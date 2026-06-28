import contextlib
import importlib.util
import io
import os
from pathlib import Path
from typing import Any

from fastapi import FastAPI


BASE_DIR = Path(__file__).resolve().parent
MODEL_PATH = Path(os.getenv("FITMATE_MODEL_PATH", BASE_DIR / "optimizer_model.py"))
XLSX_PATH = Path(os.getenv("FITMATE_FOOD_XLSX", BASE_DIR / "Food_Data_GP_v3_.xlsx"))


def _load_optimizer_module():
    if not MODEL_PATH.exists():
        raise FileNotFoundError(f"Optimizer model was not found: {MODEL_PATH}")

    spec = importlib.util.spec_from_file_location("fitmate_optimizer_model", MODEL_PATH)
    if spec is None or spec.loader is None:
        raise ImportError(f"Could not load optimizer model from {MODEL_PATH}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    if not XLSX_PATH.exists():
        raise FileNotFoundError(f"Food data Excel was not found: {XLSX_PATH}")

    module.XLSX = str(XLSX_PATH)
    return module


optimizer = _load_optimizer_module()
app = FastAPI(title="FitMate Optimizer API")


def _get(payload: dict[str, Any], key: str, default: Any = None) -> Any:
    if key in payload:
        return payload[key]

    pascal_key = key[:1].upper() + key[1:]
    if pascal_key in payload:
        return payload[pascal_key]

    return default


def _text(value: Any, default: str = "") -> str:
    if value is None:
        return default
    return str(value).strip()


def _build_user(payload: dict[str, Any]):
    user = optimizer.UserBiometrics(
        age=int(_get(payload, "age", 0)),
        gender=_text(_get(payload, "gender", "female"), "female"),
        height_cm=float(_get(payload, "heightCm", 0)),
        weight_kg=float(_get(payload, "weightKg", 0)),
        activity_level=_text(_get(payload, "activityLevel", "moderate"), "moderate"),
        goal=_text(_get(payload, "goal", "maintain"), "maintain"),
        diabetes_status=_text(_get(payload, "diabetesStatus", "none"), "none"),
        hypertension_status=_text(_get(payload, "hypertensionStatus", "none"), "none"),
        allergies=_get(payload, "allergies", []) or [],
        budget=_get(payload, "budget", None),
    )

    calorie_adjustment = float(_get(payload, "calorieAdjustment", 0) or 0)
    if calorie_adjustment:
        user.cal_target = max(800, user.cal_target + calorie_adjustment)
        user.fat_target = round(user.cal_target * user.fat_pct / 9, 1)
        user.carbs_target = round(user.cal_target * user.carb_pct / 4, 1)
        user.portion_scale = max(1.0, user.cal_target / optimizer.BASE_CAL)
        user.meal_size_max = max(700, round(user.cal_target * 0.40))

    return user


def _build_exclusion_map(
    slot_exclusions: list[dict[str, Any]],
    foods,
) -> dict[tuple[str, int], set[int]]:
    if not slot_exclusions:
        return {}

    foods_by_name = {
        str(row["Food"]).strip().lower(): idx
        for idx, row in foods.iterrows()
    }

    excluded: dict[tuple[str, int], set[int]] = {}
    for exclusion in slot_exclusions:
        meal = _text(exclusion.get("meal") or exclusion.get("Meal"))
        slot = exclusion.get("slot", exclusion.get("Slot"))
        names = exclusion.get("excludedFoods") or exclusion.get("ExcludedFoods") or []

        if not meal or slot is None:
            continue

        key = (meal, int(slot))
        excluded.setdefault(key, set())
        for name in names:
            food_idx = foods_by_name.get(str(name).strip().lower())
            if food_idx is not None:
                excluded[key].add(food_idx)

    return excluded


def _analysis(user) -> dict[str, Any]:
    return {
        "bmi": user.bmi,
        "bmiCategory": user.bmi_category,
        "tdee": round(user.tdee, 2),
        "calTarget": round(user.cal_target, 2),
        "proteinTarget": user.protein_target,
        "fatTarget": user.fat_target,
        "carbsTarget": user.carbs_target,
        "fiberTarget": user.fiber_target,
        "netCarbsMaxDay": user.nc_max_day,
    }


def _map_item(item: dict[str, Any]) -> dict[str, Any]:
    return {
        "food": item.get("food", ""),
        "slot": item.get("slot", 0),
        "category": item.get("category", ""),
        "foodFamily": item.get("food_family", ""),
        "grams": item.get("grams", 0),
        "calories": item.get("calories", 0),
        "protein": item.get("protein", 0),
        "fat": item.get("fat", 0),
        "carbs": item.get("carbs", 0),
        "fiber": item.get("fiber", 0),
        "netCarbs": item.get("net_carbs", 0),
        "satFat": item.get("sat_fat", 0),
    }


def _map_plan(plan: dict[str, list[dict[str, Any]]]) -> dict[str, list[dict[str, Any]]]:
    return {
        meal: [_map_item(item) for item in plan.get(meal, [])]
        for meal in optimizer.MEALS
    }


@app.get("/health")
def health() -> dict[str, str]:
    return {
        "status": "ok",
        "modelPath": str(MODEL_PATH),
        "xlsxPath": str(XLSX_PATH),
    }


@app.post("/optimize")
def optimize(payload: dict[str, Any]) -> dict[str, Any]:
    user = _build_user(payload)

    with contextlib.redirect_stdout(io.StringIO()):
        foods = optimizer.load_foods(allergies=user.allergies)
        excluded = _build_exclusion_map(_get(payload, "slotExclusions", []) or [], foods)
        master_seed = _get(payload, "masterSeed", None)
        if master_seed is None:
            master_seed = int(optimizer.time.time() * 1000) % 100000

        day_number = int(_get(payload, "dayNumber", 1) or 1)
        plan, solver_status, relaxation = optimizer.solve_day_robust(
            user=user,
            foods=foods,
            day=day_number,
            master_seed=int(master_seed),
            excluded=excluded,
            banned_families=set(),
        )

    if plan is None or optimizer.plan_violates(plan, user):
        return {
            "success": False,
            "analysis": _analysis(user),
            "plan": None,
            "status": solver_status,
            "message": "Optimizer could not generate a feasible meal plan.",
            "relaxationApplied": relaxation is not None,
            "relaxedConstraints": [relaxation] if relaxation else [],
        }

    return {
        "success": True,
        "analysis": _analysis(user),
        "plan": _map_plan(plan),
        "status": solver_status,
        "message": "Meal plan generated successfully.",
        "relaxationApplied": relaxation is not None,
        "relaxedConstraints": [relaxation] if relaxation else [],
    }
