import pandas as pd
import numpy as np
import time
import pulp
import warnings

warnings.filterwarnings("ignore")

# ======================================================================
#  SECTION 1 — CONFIGURATION
# ======================================================================

XLSX = "Food_Data_GP_v3_modified.xlsx"

SHEET_CAT = {
    "Vegetables (4)": "V",
    "Rice & pasta (4)": "C",
    "Nuts & Seeds (4)": "S",
    "Meats (4)": "P",
    "Fruits (4)": "F",
    "Fats & Oils (4)": "Fat",
    "Dairy___Eggs__3": None,
}

MEALS = ["Breakfast", "Lunch", "Dinner", "Snack"]

MEAL_SLOTS = {
    "Breakfast": [
        ("P",   True,  80, 130),   # slot 0 — egg        (RS: Bernoulli-activated, §2.5a)
        ("D",   True, 120, 180),   # slot 1 — dairy       (FIX-2: raised min 80→120 so 1 cup minimum)
        ("F",   True, 100, 150),   # slot 2 — fruit       (always active)
        ("C",   False, 60,  80),   # slot 3 — oats/carb   (FIX-2: raised min 40→60 / max 70→80)
        ("S",   False, 15,  25),   # slot 4 — seed        (optional, always eligible)
    ],
    "Lunch": [
        ("P",   True,  150, 300),  # slot 0 — protein
        ("V",   True,   80, 150),  # slot 1 — vegetable   (FIX-4: raised min 70→80)
        ("C",   True,  100, 250),  # slot 2 — carb        (FIX-4: lowered max 300→250 to help C13)
        ("Fat", False,   5,  15),  # slot 3 — fat/oil
    ],
    "Dinner": [
        ("P",   True,  150, 200),  # slot 0 — protein
        ("V",   True,   80, 150),  # slot 1 — vegetable   (FIX-4: raised min 70→80)
        ("C",   True,  150, 220),  # slot 2 — carb        (FIX-4: lowered max 250→220 to help C13)
    ],
    "Snack": [
        ("F",   True,  100, 150),  # slot 0 — fruit
        ("S",   False,  15,  25),  # slot 1 — seed        (FIX-3: max stays 25g — already fine)
    ],
}

# ── §2.5a  Randomly-Activated Slots RS = {(Breakfast,0), (Breakfast,3)} ──────
# Each entry: (meal, slot_idx, p_s)  where p_s is the Bernoulli probability.
# δ_{s,d} ~ Bernoulli(p_s) is drawn BEFORE each day's LP is built (C20).
RANDOM_SLOTS = [
    ("Breakfast", 0, 0.60),   # egg  slot — appears on ~60 % of days
    ("Breakfast", 3, 0.60),   # oats slot — appears on ~60 % of days
]
# Convenience set for quick membership tests
RS_SET = {(m, si) for m, si, _ in RANDOM_SLOTS}

# Slots whose gram bounds are NOT scaled by user.portion_scale (§2.3 NO_SCALE)
NO_SCALE_SLOTS = {("Breakfast", 0), ("Breakfast", 1), ("Breakfast", 3)}

# ── FIX-3: Per-food gram overrides — caps foods that are nutritionally dense
# enough that their slot max is unrealistic (e.g., dried coconut at 275g).
# Applies BEFORE the LP is built; overrides Portion_max for that food only.
# Format: {food_name_substring (lowercase): max_grams}
MAX_FOOD_GRAMS_OVERRIDE = {
    "coconut meat dried": 30,    # ~165 kcal at 25g — keep as small treat
    "coconut meat":       120,   # fresh coconut flesh — 120g is ~1 cup, fine
    "dates deglet":       100,   # ~270 kcal at 100g — realistic portion
    "dates medjool":       80,   # medjool are bigger & denser; 2 dates ≈ 50g
    "olive black":        100,   # olives as fruit slot: 100g = realistic
    "olive green":        100,
}

FAT_FILTER = {
    "P":   0.20,
    "D":   0.28,
    "F":   0.20,
    "V":   0.35,
    "C":   0.15,
}

FAMILY_PLAN_LIMITS = {
    "poultry":       8,
    "red_meat_fatty": 4,
    "red_meat_lean":  4,
    "offal":          1,
    "fish_fatty":     1,
    "fish_lean":      1,
    "seafood":        1,
}

# ── §2.5b  Tracked slots for cross-day food exclusion (C21) ──────────────────
VARY_ACROSS_DAYS = {
    "P":   [("Lunch", 0), ("Dinner", 0), ("Breakfast", 0)],
    "C":   [("Lunch", 2), ("Dinner", 2), ("Breakfast", 3)],
    "V":   [("Lunch", 1), ("Dinner", 1)],
    "F":   [("Breakfast", 2), ("Snack", 0)],
    "Fat": [("Lunch", 3)],
    "D":   [("Breakfast", 1)],
}

# §2.5b  MIN_ALT: minimum alternatives that must remain before banning a food
MIN_ALTERNATIVES_TO_BAN = 5   # = MIN_ALT in the model document

MEAL_CAL_FRAC = {
    "Breakfast": 0.25,
    "Lunch":     0.35,
    "Dinner":    0.28,
    "Snack":     0.12,
}
MEAL_CAL_TOL    = 0.15   # FIX-3: tightened ±15 % per-meal calorie band (was ±25 %) — reduces breakfast underdelivery and lunch/dinner overflow
NETCARB_MEAL_TOL = 0.30  # +30 % per-meal NC tolerance    (ε_NC)

GP_WEIGHTS_BY_GOAL = {
    "lose":     {"cost": 0.5},
    "maintain": {"cost": 0.5},
    "gain":     {"cost": 0.5},
}

# §2.3  Diabetes / W_diab / W_health  (see §4 note on w_health)
DIABETES_PARAMS = {
    "none":       {"nc_max_day": None, "w_diab": 0.0,  "w_health": 0.0},  # FIX-1: was 0.3 — healthy users must not have net-carb minimization
    "prediabetic":{"nc_max_day": 150,  "w_diab": 2.0,  "w_health": 1.0},
    "type2":      {"nc_max_day": 130,  "w_diab": 5.0,  "w_health": 1.0},
}

# ── Cardiovascular / Heart-Healthy mode (مرض الضغط — partial intervention) ──
# Without sodium/potassium data, the only evidence-based lever available is
# saturated fat reduction, which lowers LDL and reduces arterial stiffness
# over time (cardiovascular risk reduction, not acute BP control).
# This activates w_health = 1.0, identical to the diabetes sat-fat penalty.
# Labeled "cardiovascular" rather than "hypertension" to reflect what the
# data actually supports.
HYPERTENSION_PARAMS = {
    "none":           {"w_health": 0.0},
    "cardiovascular": {"w_health": 1.0},
}

# ── FIX-2: Single shared rounding tolerance used by BOTH the LP post-processor
# and plan_violates() so validation and optimization never disagree.
# 1 % covers the maximum error from rounding grams to the nearest 5g.
# (5g trim on a 100g item with 0.3 g/g fat = 1.5g fat error < 1% of 150g ceiling)
ROUNDING_TOL = 0.01   # 1 % — applied uniformly to all macro ceiling checks

MACRO_SPLITS = {
    "lose":     (0.25, 0.40),
    "maintain": (0.30, 0.40),
    "gain":     (0.25, 0.50),
}

BASE_CAL = 2000

ALLERGY_COLS = {
    "lactose": "Lactose Allergy",
    "nuts":    "Nuts Allergy",
    "gluten":  "Gluten Allergy",
}


# ======================================================================
#  SECTION 2 — DATA LOADING
# ======================================================================

def load_foods(allergies=None):
    allergies = [a.lower() for a in (allergies or [])]
    xl = pd.ExcelFile(XLSX)
    frames = []

    for sheet in xl.sheet_names:
        if sheet not in SHEET_CAT:
            continue
        df = xl.parse(sheet)
        df.columns = df.columns.str.strip()
        df = df.rename(columns={df.columns[0]: "Food"})
        df["Food"] = df["Food"].astype(str).str.strip()
        df = df[df["Food"].notna() & (df["Food"] != "") & (df["Food"] != "nan")].copy()

        if SHEET_CAT[sheet] is not None:
            df["Category"] = SHEET_CAT[sheet]
        else:
            if "Category" not in df.columns:
                df["Category"] = "D"

        for allergy in allergies:
            col = ALLERGY_COLS.get(allergy)
            if col and col in df.columns:
                df = df[df[col] != 1]

        MAP = {
            "Calories (kCal)":      "cal",
            "Protein (g)":          "prot",
            "Fats (g)":             "fat",
            "Saturated (g)":        "sat_fat",
            "Carbohydrates (g)":    "carbs",
            "Fiber (g)":            "fiber",
            "Net Carbs (g)":        "net_carbs",
            "Cost":                 "cost",
        }
        for src, dst in MAP.items():
            col_data = df[src] if src in df.columns else pd.Series(0, index=df.index)
            df[dst] = pd.to_numeric(col_data, errors="coerce").fillna(0)

        if "Net Carbs (g)" not in df.columns:
            df["net_carbs"] = (df["carbs"] - df["fiber"]).clip(lower=0)

        if "Food_Family" in df.columns:
            df["Food_Family"] = df["Food_Family"].astype(str).str.strip()
        else:
            df["Food_Family"] = "other"

        for meal in MEALS:
            if meal not in df.columns:
                df[meal] = 0
            df[meal] = pd.to_numeric(df[meal], errors="coerce").fillna(0).astype(int)

        if sheet == "Fats & Oils (4)":
            for meal in ["Breakfast", "Lunch", "Dinner"]:
                df[meal] = 1
            df["Snack"] = 0

        frames.append(df[["Food", "Category", "Food_Family",
                           "cal", "prot", "fat", "sat_fat",
                           "carbs", "fiber", "net_carbs", "cost"] + MEALS])

    return pd.concat(frames, ignore_index=True).reset_index(drop=True)


# ======================================================================
#  SECTION 3 — SLOT TRIPLES  (incorporates C20 Bernoulli draw)
# ======================================================================

def build_slot_triples(foods, excluded=None, family_banned=None, user=None, rng=None):
    """
    Build (food_idx, meal, slot_idx, min_g, max_g, required) triples for one day.

    C20 — Probabilistic slot activation (§2.5a, §5.8):
        For each (m,s) ∈ RS, draw δ_{s,d} ~ Bernoulli(p_s) using the day-seeded
        RNG.  If δ = 0 → slot is structurally omitted from the LP (no variables
        created for it).  If δ = 1 → slot is treated as optional (C2 upper bound ≤ 1).

    C21 — Cross-day food exclusion (§2.5b, §5.8):
        Foods in excluded[(meal, slot_idx)] are omitted from the eligible set for
        that slot.  The safety check (MIN_ALT) is applied in build_exclusions after
        the solve, not here — here we just filter out what H_{m,s,d} already contains.
    """
    excluded     = excluded     or {}
    family_banned = family_banned or set()
    scale        = user.portion_scale if user is not None else 1.0

    # ── C20: draw δ_{s,d} for every slot in RS ───────────────────────────────
    # seed is already set on rng before this call (master_seed + day + slot_off)
    active_random_slots: set = set()
    for meal, s_idx, p_s in RANDOM_SLOTS:
        delta = 1 if (rng is None or rng.random() < p_s) else 0  # Bernoulli(p_s)
        if delta == 1:
            active_random_slots.add((meal, s_idx))
    # ─────────────────────────────────────────────────────────────────────────

    triples = []

    for meal, slots in MEAL_SLOTS.items():
        for s_idx, (cat, required, mn, mx) in enumerate(slots):

            # ── C20: if this slot is in RS and δ_{s,d} = 0, omit it entirely ─
            if (meal, s_idx) in RS_SET:
                if (meal, s_idx) not in active_random_slots:
                    continue   # slot structurally removed from today's LP
            # ─────────────────────────────────────────────────────────────────

            # Portion scaling (NO_SCALE_SLOTS are exempt — §2.3)
            if cat in ("S", "Fat") or (meal, s_idx) in NO_SCALE_SLOTS:
                scaled_mn, scaled_mx = mn, mx
            else:
                scaled_mn = round(mn * scale / 5) * 5
                scaled_mx = round(mx * scale / 5) * 5

            # ── C21: apply cross-day exclusion set H_{m,s,d} ─────────────────
            banned = excluded.get((meal, s_idx), set())
            # ─────────────────────────────────────────────────────────────────

            fat_thresh = FAT_FILTER.get(cat, 99)

            eligible = foods[
                (foods["Category"] == cat) &
                (foods[meal] == 1) &
                (foods["fat"] <= fat_thresh) &
                (~foods.index.isin(banned)) &
                (~foods["Food_Family"].isin(family_banned))
            ].index

            for j in eligible:
                food_name = str(foods.loc[j, "Food"]).lower()
                # FIX-3: apply per-food gram cap overrides before LP build
                effective_mx = scaled_mx
                for substr, cap in MAX_FOOD_GRAMS_OVERRIDE.items():
                    if substr in food_name:
                        effective_mx = min(scaled_mx, cap)
                        break
                triples.append((j, meal, s_idx, scaled_mn, effective_mx, required))

    return triples


# ======================================================================
#  SECTION 4 — USER BIOMETRICS
# ======================================================================

ACTIVITY_MULTIPLIERS = {
    "sedentary":   1.2,
    "light":       1.375,
    "moderate":    1.55,
    "active":      1.725,
    "very_active": 1.9,
}
GOAL_DELTA = {"lose": -500, "maintain": 0, "gain": 500}


class UserBiometrics:
    """
    Computes all personalized targets from biometrics (§2.2).
    Also stores the objective weights derived from diabetes status (§2.3, §4).

    diabetes_status : 'none' | 'prediabetic' | 'type2'
      — none       : w_health = 0.0, w_diab = 0.3  (§4 table)
      — prediabetic: w_health = 1.0, w_diab = 2.0
      — type2      : w_health = 1.0, w_diab = 5.0
    """

    def __init__(self, age, gender, height_cm, weight_kg,
                 activity_level="moderate", goal="maintain",
                 diabetes_status="none", hypertension_status="none",
                 allergies=None, budget=None):

        self.age           = age
        self.gender        = gender.lower()
        self.height_cm     = height_cm
        self.weight_kg     = weight_kg
        self.activity_level = activity_level.lower()
        self.goal          = goal.lower()

        # Normalise diabetes status string
        ds = diabetes_status.lower().replace(" ", "").replace("-", "")
        if ds in ("typeii", "type2diabetes", "t2"):
            ds = "type2"
        elif ds in ("pre", "pre-diabetes", "prediabetes"):
            ds = "prediabetic"
        self.diabetes_status = ds

        self.allergies = [a.lower() for a in (allergies or [])]
        self.budget    = budget

        # ── §2.3 / §4 — diabetes-derived weights and NC cap ──────────────────
        diab = DIABETES_PARAMS.get(self.diabetes_status, DIABETES_PARAMS["none"])
        self.nc_max_day = diab["nc_max_day"]   # None when status = 'none'
        self.w_diab     = diab["w_diab"]
        self.w_health   = diab["w_health"]     # 0.0 for non-diabetic (§4)
        # ─────────────────────────────────────────────────────────────────────

        # ── Cardiovascular mode (مرض الضغط — sat-fat reduction only) ──────────
        hs = hypertension_status.lower().replace(" ", "").replace("-", "")
        if hs in ("high", "hypertension", "htn", "yes", "true", "1",
                  "standard", "strict", "cardiovascular"):
            hs = "cardiovascular"
        else:
            hs = "none"
        self.hypertension_status = hs

        htn = HYPERTENSION_PARAMS[hs]
        # w_health is activated if EITHER diabetes OR cardiovascular mode is on
        if htn["w_health"] > 0.0:
            self.w_health = max(self.w_health, htn["w_health"])
        # ─────────────────────────────────────────────────────────────────────

        # ── §2.2 — Mifflin-St Jeor BMR + TDEE ───────────────────────────────
        if self.gender == "male":
            bmr = 10 * weight_kg + 6.25 * height_cm - 5 * age + 5
        else:
            bmr = 10 * weight_kg + 6.25 * height_cm - 5 * age - 161

        mult            = ACTIVITY_MULTIPLIERS.get(self.activity_level, 1.55)
        self.tdee       = bmr * mult
        self.cal_target = self.tdee + GOAL_DELTA.get(self.goal, 0)  # E^u_req
        # ─────────────────────────────────────────────────────────────────────

        # ── §2.2 — Macro targets ─────────────────────────────────────────────
        prot_per_kg          = 2.0 if goal == "gain" else 1.6
        self.protein_target  = round(weight_kg * prot_per_kg, 1)

        fat_pct, carb_pct    = MACRO_SPLITS.get(self.goal, (0.27, 0.40))
        self.fat_pct         = fat_pct
        self.carb_pct        = carb_pct
        self.fat_target      = round(self.cal_target * fat_pct / 9, 1)
        self.carbs_target    = round(self.cal_target * carb_pct / 4, 1)

        self.fiber_target    = 38 if self.gender == "male" else 25

        # ── Macro upper bounds (new hard ceilings) ────────────────────────────
        #
        # Protein ceiling: ISSN (2017) position stand states that intakes up to
        # 2.2 g/kg/day are well-supported for muscle gain; beyond that, no
        # additional anabolic benefit is observed and renal load increases.
        # We use 2.5 g/kg as a generous but firm daily ceiling, which is
        # ~25 % above the most aggressive evidence-based target.
        # For a 71 kg "gain" user: target=142 g, ceiling=178 g.
        self.protein_max = round(weight_kg * 2.5, 1)

        # Fiber ceiling: IOM sets no formal Tolerable Upper Intake Level for
        # dietary fiber from whole food sources, but clinical practice typically
        # caps at 2× the sex-specific AI to prevent GI distress (bloating,
        # cramping, mineral absorption interference at very high intakes).
        # Reference: Dahl & Stewart (2015), Nutr Rev — "Total fiber intake".
        # Male AI = 38 g → cap = 76 g;  Female AI = 25 g → cap = 50 g.
        self.fiber_max = self.fiber_target * 2
        # ─────────────────────────────────────────────────────────────────────

        self.portion_scale   = max(1.0, self.cal_target / BASE_CAL)
        self.meal_size_max   = max(700, round(self.cal_target * 0.40))
        # ─────────────────────────────────────────────────────────────────────

    @property
    def bmi(self):
        h = self.height_cm / 100
        return round(self.weight_kg / (h * h), 1)

    @property
    def bmi_category(self):
        b = self.bmi
        if b < 18.5: return "Underweight"
        if b < 25.0: return "Normal weight"
        if b < 30.0: return "Overweight"
        return "Obese"

    def summary(self):
        act = self.activity_level.replace("_", " ").title()
        return "\n".join([
            f"\n  {'_' * 60}",
            f"  USER",
            f"  {'_' * 60}",
            f"  {self.age} yrs  |  {self.gender.title()}  |  "
            f"{self.height_cm} cm  |  {self.weight_kg} kg  |  BMI {self.bmi} ({self.bmi_category})",
            f"  Activity: {act}  |  Goal: {self.goal.upper()}",
            f"  Diabetes: {self.diabetes_status}  |  W_diab: {self.w_diab}  |  W_health: {self.w_health}"
            + (f"  |  NC_max/day: {self.nc_max_day} g" if self.nc_max_day else "  |  NC_max/day: none"),
            f"  Cardiovascular mode: {self.hypertension_status}"
            + ("  (sat-fat minimized via W_health)" if self.hypertension_status != "none" else ""),
            *([f"  Allergies: {', '.join(self.allergies)}"] if self.allergies else []),
            f"  {'_' * 60}",
            f"  DAILY TARGETS",
            f"  {'_' * 60}",
            f"  Calories : {self.cal_target:.0f} kcal  (TDEE {self.tdee:.0f})",
            f"  Protein  : {self.protein_target} – {self.protein_max} g  "
            f"({'2.0' if self.goal == 'gain' else '1.6'}–2.5 g/kg)",
            f"  Fat      : <= {self.fat_target} g  ({self.fat_pct * 100:.0f}% of kcal)",
            f"  Carbs    : <= {self.carbs_target} g  ({self.carb_pct * 100:.0f}% of kcal)",
            f"  Fiber    : {self.fiber_target} – {self.fiber_max} g",
            *([f"  Budget   : {self.budget} EGP/day"] if self.budget else []),
            f"  {'_' * 60}",
        ])


# ======================================================================
#  SECTION 5 — MILP MODEL  (single day)
# ======================================================================

def _compute_reference_values(user, foods, triples):
    """
    Compute R_cost, R_SF, R_NC — §2.4 Reference-Value Normalization.
    R_cal removed: calorie target is now enforced via hard C11 ±3% band.

    R_cost  = Σ_{m,s} max_{j ∈ J_{m,s}} Cost_j × Portion_max_{m,s}
    R_SF    = 0.10 × E^u_req / 9  (grams) — WHO/AHA SatFat limit
    R_NC    = NC^u_max if diabetic, else Carb^u_max
    """
    # R_cost: sum over every (meal, slot) of [max cost food × that slot's Portion_max]
    r_cost = 0.0
    seen_slots = set()
    for j, meal, s_idx, mn, mx, req in triples:
        key = (meal, s_idx)
        if key in seen_slots:
            continue
        seen_slots.add(key)
        slot_foods = [
            foods.loc[jj, "cost"] * float(mx_)
            for jj, m2, s2, mn_, mx_, _ in triples
            if m2 == meal and s2 == s_idx
        ]
        if slot_foods:
            r_cost += max(slot_foods)

    # Safety guard: avoid division by zero if foods table has zero costs
    r_cost = max(r_cost, 1.0)

    # R_SF = 10 % of energy ÷ 9 kcal/g  →  grams of saturated fat
    r_sf = max(0.10 * user.cal_target / 9.0, 1.0)

    # R_NC: use diabetes NC ceiling for diabetic users, carb ceiling otherwise
    if user.nc_max_day is not None:
        r_nc = float(user.nc_max_day)
    else:
        r_nc = float(user.carbs_target)
    r_nc = max(r_nc, 1.0)

    return r_cost, r_sf, r_nc


def solve_one_day(user, foods, triples, day_label="", seed=None, perturb_cost=True):
    """
    Build and solve the MILP for one day using the v5 normalized objective.

    Objective (§4 — Normalized, calorie term removed — enforced by hard C11 ±3% band):
        min Z = W_cost  × (Σ Cost_j·x  / R_cost)
              + W_health× (Σ SatFat_j·x / R_SF)
              + W_diab  × (Σ NetCarb_j·x/ R_NC)
              + proximity penalties on protein, fat, carb usage

    C20 has already been applied in build_slot_triples (δ controls which triples exist).
    C21 has already been applied in build_slot_triples (H_{m,s,d} filters the eligible set).
    """
    rng = np.random.default_rng(seed)

    # ── §2.5b — Cost perturbation (tie-breaking; disabled on retry) ───────────
    perturbed_cost = {}
    if perturb_cost:
        PERTURB_CATS = {"P": 1.00, "V": 0.50, "F": 0.50, "C": 1.00}
        for cat, pscale in PERTURB_CATS.items():
            cat_foods = foods[foods["Category"] == cat]
            if cat_foods.empty:
                continue
            max_cost = float(cat_foods["cost"].max())
            for j in cat_foods.index:
                noise = rng.uniform(0, max_cost * pscale)
                perturbed_cost[j] = float(foods.loc[j, "cost"]) + noise
    # ─────────────────────────────────────────────────────────────────────────

    prob = pulp.LpProblem(f"Diet_{day_label}", pulp.LpMinimize)

    # Decision variables (§3)
    x, y = {}, {}
    for j, meal, s_idx, mn, mx, req in triples:
        k = (j, meal, s_idx)
        x[k] = pulp.LpVariable(f"x_{j}_{meal}_{s_idx}", lowBound=0, upBound=float(mx))
        y[k] = pulp.LpVariable(f"y_{j}_{meal}_{s_idx}", cat="Binary")

    # ── §2.4 — Compute normalization reference values ─────────────────────────
    R_cost, R_SF, R_NC = _compute_reference_values(user, foods, triples)
    # ─────────────────────────────────────────────────────────────────────────

    # Objective term helpers
    W = GP_WEIGHTS_BY_GOAL.get(user.goal, GP_WEIGHTS_BY_GOAL["maintain"])

    # f₁ / R_cost  — daily cost fraction of worst-case feasible cost
    f1_cost = pulp.lpSum(
        perturbed_cost.get(j, float(foods.loc[j, "cost"])) * x[j, m, si]
        for j, m, si, *_ in triples
    )

    # f₃ / R_SF    — saturated fat fraction of WHO/AHA SatFat limit
    f3_satfat = pulp.lpSum(
        foods.loc[j, "sat_fat"] * x[j, m, si]
        for j, m, si, *_ in triples
    )

    # f₄ / R_NC    — net carb fraction of the diabetes/carb ceiling
    f4_netcarb = pulp.lpSum(
        foods.loc[j, "net_carbs"] * x[j, m, si]
        for j, m, si, *_ in triples
    )

    # ── FIX-4: Macro proximity penalty terms ──────────────────────────────────
    # Penalizes proximity to macro ceilings to reduce constraint saturation.
    # Weights are small so they act as tie-breakers only — the hard calorie
    # band (C11) guarantees calorie delivery regardless of these penalties.
    R_prot_obj  = max(user.protein_max,   1.0)
    R_fat_obj   = max(user.fat_target,    1.0)
    R_carb_obj  = max(user.carbs_target,  1.0)

    f_prot_use  = pulp.lpSum(foods.loc[j, "prot"]  * x[j, m, si] for j, m, si, *_ in triples)
    f_fat_use   = pulp.lpSum(foods.loc[j, "fat"]   * x[j, m, si] for j, m, si, *_ in triples)
    f_carb_use  = pulp.lpSum(foods.loc[j, "carbs"] * x[j, m, si] for j, m, si, *_ in triples)

    W_PROT_PROX = 0.4
    W_FAT_PROX  = 0.3
    W_CARB_PROX = 0.3
    # ─────────────────────────────────────────────────────────────────────────

    # ── §4 — Normalized objective (calorie term removed — enforced by hard C11 band)
    prob += (
        W["cost"]        * (f1_cost    / R_cost)          # dimensionless cost term
        + user.w_health  * (f3_satfat  / R_SF)            # sat-fat term (diabetes OR cardiovascular)
        + user.w_diab    * (f4_netcarb / R_NC)            # net-carb term (diabetes only; 0 for healthy)
        + W_PROT_PROX    * (f_prot_use / R_prot_obj)      # proximity penalty — protein
        + W_FAT_PROX     * (f_fat_use  / R_fat_obj)       # proximity penalty — fat
        + W_CARB_PROX    * (f_carb_use / R_carb_obj)      # proximity penalty — carbs
    ), "Obj_Normalized"
    # ─────────────────────────────────────────────────────────────────────────

    # Utility helpers
    def sum_all(nut):
        return pulp.lpSum(foods.loc[j, nut] * x[j, m, si]
                          for j, m, si, *_ in triples)

    def sum_n(meal_name, nut):
        return pulp.lpSum(foods.loc[j, nut] * x[j, m, si]
                          for j, m, si, *_ in triples if m == meal_name)

    # ── C11 — Hard daily calorie band ±3 % ───────────────────────────────────
    # Replaces the GP soft equality. ±3 % = ±~110 kcal at 3692 kcal target,
    # which absorbs gram-rounding without allowing large underdelivery.
    # The proximity penalty terms in the objective cannot push calories below
    # this floor because C11_lo is a hard constraint the solver must satisfy.
    prob += sum_all("cal") >= user.cal_target * 0.97, "C11_cal_lo"
    prob += sum_all("cal") <= user.cal_target * 1.03, "C11_cal_hi"

    # ── C7–C10 — Hard macro constraints (§5.3) + upper bounds ───────────────
    prob += sum_all("prot")  >= user.protein_target, "C7_prot_min"
    prob += sum_all("fat")   <= user.fat_target,     "C8_fat_max"
    prob += sum_all("carbs") <= user.carbs_target,   "C9_carbs_max"
    prob += sum_all("fiber") >= user.fiber_target,   "C10_fiber_min"

    # ── C7b — Protein daily ceiling ───────────────────────────────────────────
    # ISSN (2017): intakes > 2.2 g/kg/day yield no additional anabolic benefit.
    # Ceiling = 2.5 g/kg (generous upper safety margin).
    # Prevents the solver from stuffing every protein slot to its gram maximum
    # just to fill calorie targets cheaply.
    prob += sum_all("prot") <= user.protein_max, "C7b_prot_max"

    # ── C10b — Fiber daily ceiling ────────────────────────────────────────────
    # IOM has no formal UL for dietary fiber from whole foods, but clinical
    # practice caps at 2x the sex-specific AI to avoid GI distress and
    # mineral absorption interference (Dahl & Stewart, Nutr Rev 2015).
    # Male: 38 g x 2 = 76 g;  Female: 25 g x 2 = 50 g.
    prob += sum_all("fiber") <= user.fiber_max, "C10b_fiber_max"

    # ── C1 — Portion bounds linking x to y (Big-M, §5.1) ────────────────────
    for j, meal, s_idx, mn, mx, req in triples:
        k = (j, meal, s_idx)
        prob += x[k] <= float(mx) * y[k], f"C1_ub_{j}_{meal}_{s_idx}"
        prob += x[k] >= float(mn) * y[k], f"C1_lb_{j}_{meal}_{s_idx}"

    # ── C2 — Slot selection: required =1, optional ≤1  (§5.1) ───────────────
    # NOTE: RS slots (Breakfast egg & oats) are governed by C20 (already
    # handled in build_slot_triples by omitting the slot when δ=0).
    # When δ=1 those slots are included and treated as optional here (≤1).
    for meal, slots in MEAL_SLOTS.items():
        for s_idx, (cat, required, mn, mx) in enumerate(slots):
            sy = [y[j, m, si] for j, m, si, *_ in triples
                  if m == meal and si == s_idx]
            if not sy:
                continue

            if (meal, s_idx) in RS_SET:
                # C20 slot: always optional (≤1) when active; omitted when δ=0
                prob += pulp.lpSum(sy) <= 1, f"C20_opt_{meal}_{s_idx}"
            elif required:
                prob += pulp.lpSum(sy) == 1, f"C2_req_{meal}_{s_idx}"
            else:
                prob += pulp.lpSum(sy) <= 1, f"C2_opt_{meal}_{s_idx}"

    # ── C13 — Meal physical size cap (§5.5) ──────────────────────────────────
    # FIX-4: Hard per-meal gram caps to prevent 1kg+ single meals.
    # Lunch/Dinner capped at 900g (physically realistic full-plate limit).
    # Breakfast capped at 650g. Snack at 300g.
    MEAL_GRAM_CAPS = {
        "Breakfast": 650,
        "Lunch":     900,
        "Dinner":    900,
        "Snack":     300,
    }
    for meal in MEALS:
        cap = min(user.meal_size_max, MEAL_GRAM_CAPS[meal])
        prob += (pulp.lpSum(x[j, m, si] for j, m, si, *_ in triples if m == meal)
                 <= cap), f"C13_size_{meal}"

    # ── C14 — Each food appears in at most 1 slot per day (§5.5) ─────────────
    for j in foods.index:
        jy = [y[j2, m, si] for j2, m, si, *_ in triples if j2 == j]
        if len(jy) > 1:
            prob += pulp.lpSum(jy) <= 1, f"C14_var_{j}"

    # ── C15 — Different protein & carb between Lunch and Dinner (§5.5) ───────
    lunch_slots  = {cat: si for si, (cat, *_) in enumerate(MEAL_SLOTS["Lunch"])}
    dinner_slots = {cat: si for si, (cat, *_) in enumerate(MEAL_SLOTS["Dinner"])}
    for cat in ["P", "C"]:
        l_si = lunch_slots.get(cat)
        d_si = dinner_slots.get(cat)
        if l_si is None or d_si is None:
            continue
        for j in foods[foods["Category"] == cat].index:
            ly = y.get((j, "Lunch",  l_si))
            dy = y.get((j, "Dinner", d_si))
            if ly is not None and dy is not None:
                prob += ly + dy <= 1, f"C15_diff_{cat}_{j}"

    # ── C17 — Optional budget constraint (§5.6) ───────────────────────────────
    if user.budget is not None:
        prob += (pulp.lpSum(foods.loc[j, "cost"] * x[j, m, si]
                            for j, m, si, *_ in triples)
                 <= user.budget), "C17_budget"

    # ── C18–C19 — Diabetes net-carb caps (§5.7) ───────────────────────────────
    if user.nc_max_day is not None:
        prob += sum_all("net_carbs") <= user.nc_max_day, "C18_nc_day"
        for meal in MEALS:
            meal_cap = user.nc_max_day * MEAL_CAL_FRAC[meal] * (1 + NETCARB_MEAL_TOL)
            prob += sum_n(meal, "net_carbs") <= meal_cap, f"C19_nc_{meal}"

    # ── C16 — Calorie ordering Lunch ≥ Dinner ≥ Breakfast ≥ Snack (§5.5) ─────
    prob += sum_n("Lunch",     "cal") >= sum_n("Dinner",    "cal"), "C16_ord_LD"
    prob += sum_n("Dinner",    "cal") >= sum_n("Breakfast", "cal"), "C16_ord_DB"
    prob += sum_n("Breakfast", "cal") >= sum_n("Snack",     "cal"), "C16_ord_BS"

    # ── C12 — Per-meal calorie band ±τ (§5.4) ────────────────────────────────
    for meal in MEALS:
        tgt = user.cal_target * MEAL_CAL_FRAC[meal]
        prob += sum_n(meal, "cal") <= tgt * (1 + MEAL_CAL_TOL), f"C12_hi_{meal}"
        prob += sum_n(meal, "cal") >= tgt * (1 - MEAL_CAL_TOL), f"C12_lo_{meal}"

    solver = pulp.PULP_CBC_CMD(msg=0, timeLimit=120, gapRel=0.02)
    status = prob.solve(solver)
    sol    = pulp.LpStatus[status]

    if pulp.value(prob.objective) is None:
        return None, sol

    plan = {m: [] for m in MEALS}
    for j, meal, s_idx, mn, mx, req in triples:
        k = (j, meal, s_idx)
        if pulp.value(y[k]) is None or pulp.value(y[k]) < 0.5:
            continue
        grams = pulp.value(x[k])
        if grams is None or grams < 10:
            continue
        g = max(round(grams / 5) * 5, 10)
        plan[meal].append({
            "food":        foods.loc[j, "Food"],
            "food_idx":    j,
            "category":    foods.loc[j, "Category"],
            "food_family": foods.loc[j, "Food_Family"],
            "slot":        s_idx,
            "grams":       g,
            "calories":    round(g * foods.loc[j, "cal"],      1),
            "protein":     round(g * foods.loc[j, "prot"],     1),
            "fat":         round(g * foods.loc[j, "fat"],      1),
            "sat_fat":     round(g * foods.loc[j, "sat_fat"],  1),
            "carbs":       round(g * foods.loc[j, "carbs"],    1),
            "fiber":       round(g * foods.loc[j, "fiber"],    1),
            "net_carbs":   round(g * foods.loc[j, "net_carbs"],1),
            "cost":        round(g * foods.loc[j, "cost"],     2),
        })

    # ── FIX: Post-rounding micro-adjustment (C8/C9/C10b) ────────────────────
    # Rounding to nearest 5g can push fat, carbs, or fiber above their hard
    # ceilings. Trim 5g at a time from the highest-contributing item until
    # every ceiling is met or the item hits its minimum (10g).
    def _recompute(plan_, nut):
        return sum(it[nut] for m in MEALS for it in plan_.get(m, []))

    def _trim_for(plan_, nut, ceiling, foods_):
        for _ in range(20):
            if _recompute(plan_, nut) <= ceiling:
                break
            best_meal, best_idx, best_val = None, None, 0.0
            for m in MEALS:
                for idx, it in enumerate(plan_.get(m, [])):
                    if it[nut] > best_val and it["grams"] > 10:
                        best_meal, best_idx, best_val = m, idx, it[nut]
            if best_meal is None:
                break
            it  = plan_[best_meal][best_idx]
            j_  = it["food_idx"]
            ng  = max(it["grams"] - 5, 10)
            it["grams"]     = ng
            it["calories"]  = round(ng * foods_.loc[j_, "cal"],       1)
            it["protein"]   = round(ng * foods_.loc[j_, "prot"],      1)
            it["fat"]       = round(ng * foods_.loc[j_, "fat"],       1)
            it["sat_fat"]   = round(ng * foods_.loc[j_, "sat_fat"],   1)
            it["carbs"]     = round(ng * foods_.loc[j_, "carbs"],     1)
            it["fiber"]     = round(ng * foods_.loc[j_, "fiber"],     1)
            it["net_carbs"] = round(ng * foods_.loc[j_, "net_carbs"], 1)
            it["cost"]      = round(ng * foods_.loc[j_, "cost"],      2)

    _trim_for(plan, "fat",   user.fat_target,   foods)
    _trim_for(plan, "carbs", user.carbs_target, foods)
    _trim_for(plan, "fiber", user.fiber_max,    foods)
    if user.nc_max_day is not None:
        _trim_for(plan, "net_carbs", user.nc_max_day, foods)

    # ── Calorie boost: push rounded plan back up to the ±3 % floor ───────────
    # The LP guarantees calories ≥ cal_target × 0.97 at the continuous level,
    # but rounding every food to the nearest 5g can drop totals by 50–200 kcal.
    # This loop adds 5g at a time to the highest-calorie-density item that still
    # has headroom under its slot maximum, until the calorie floor is met or no
    # item can be increased without violating fat/carbs/protein ceilings.
    cal_floor = user.cal_target * 0.97
    for _ in range(60):   # max 60 × 5g = 300g added; enough for any gap
        total_cal = _recompute(plan, "calories")
        if total_cal >= cal_floor:
            break

        # Find the item with highest kcal/g that can be increased without
        # pushing fat, carbs, or protein over their respective ceilings
        best_meal, best_idx, best_density = None, None, 0.0
        for m in MEALS:
            for idx, it in enumerate(plan.get(m, [])):
                j_ = it["food_idx"]
                density = float(foods.loc[j_, "cal"])
                if density <= best_density:
                    continue
                # Check whether adding 5g would breach any ceiling
                delta_fat   = 5 * float(foods.loc[j_, "fat"])
                delta_carbs = 5 * float(foods.loc[j_, "carbs"])
                delta_prot  = 5 * float(foods.loc[j_, "prot"])
                if (_recompute(plan, "fat")   + delta_fat   > user.fat_target   * 1.01 or
                    _recompute(plan, "carbs") + delta_carbs > user.carbs_target * 1.01 or
                    _recompute(plan, "protein") + delta_prot > user.protein_max * 1.01):
                    continue
                best_meal, best_idx, best_density = m, idx, density

        if best_meal is None:
            break   # no item can be increased without breaching a ceiling

        it  = plan[best_meal][best_idx]
        j_  = it["food_idx"]
        ng  = it["grams"] + 5
        it["grams"]     = ng
        it["calories"]  = round(ng * foods.loc[j_, "cal"],       1)
        it["protein"]   = round(ng * foods.loc[j_, "prot"],      1)
        it["fat"]       = round(ng * foods.loc[j_, "fat"],       1)
        it["sat_fat"]   = round(ng * foods.loc[j_, "sat_fat"],   1)
        it["carbs"]     = round(ng * foods.loc[j_, "carbs"],     1)
        it["fiber"]     = round(ng * foods.loc[j_, "fiber"],     1)
        it["net_carbs"] = round(ng * foods.loc[j_, "net_carbs"], 1)
        it["cost"]      = round(ng * foods.loc[j_, "cost"],      2)
    # ─────────────────────────────────────────────────────────────────────────
    # ─────────────────────────────────────────────────────────────────────────

    return plan, sol


# ======================================================================
#  SECTION 6 — MULTI-DAY PLAN GENERATOR
# ======================================================================

def day_totals(plan):
    totals = {k: 0.0 for k in
              ["calories", "protein", "fat", "sat_fat",
               "carbs", "fiber", "net_carbs", "cost"]}
    if plan is None:
        return totals
    for meal in MEALS:
        for it in plan.get(meal, []):
            for k in totals:
                totals[k] += it[k]
    return totals


def plan_violates(plan, user):
    """
    True if the rounded plan misses any macro target.

    FIX-2: All checks now use the shared ROUNDING_TOL constant (1 %) so
    validation and the LP post-processor (_trim_for) agree exactly.
    Previous code mixed hardcoded 0.95/1.01 literals inconsistently.
    """
    if plan is None:
        return True
    t = day_totals(plan)
    tol = ROUNDING_TOL
    violates = (
        t["calories"] < user.cal_target * 0.97
        or t["calories"] > user.cal_target * 1.03
        or t["protein"] < user.protein_target * (1 - tol)
        or t["protein"] > user.protein_max  * (1 + tol)
        or t["fiber"]  < user.fiber_target  * (1 - tol)
        or t["fiber"]  > user.fiber_max     * (1 + tol)
        or t["fat"]    > user.fat_target    * (1 + tol)
        or t["carbs"]  > user.carbs_target  * (1 + tol)
    )
    if user.nc_max_day is not None and t["net_carbs"] > user.nc_max_day * (1 + tol):
        violates = True
    return violates


def build_exclusions(plan, prev_excluded, foods, banned_families=None):
    """
    C21 — Cross-day food exclusion update rule (§2.5b, §5.8).

    After day d's solve:
      For each (m,s) ∈ Ω̃ (tracked slots):
        j* = food selected in slot (m,s) on day d
        Alt_{m,s,d}(j*) = eligible foods for that slot NOT in H_{m,s,d} and ≠ j*
        If |Alt| ≥ MIN_ALT → H_{m,s,d+1} = H_{m,s,d} ∪ {j*}  (ban j* tomorrow)
        Else               → H_{m,s,d+1} = H_{m,s,d}           (safety: too few alts)

    FIX: banned_families is now passed in so the alternatives count only includes
    foods that are actually reachable on future days (i.e. whose family is not banned).
    Without this, a slot with 6 alternatives but 5 from a banned family would still
    get excluded — leaving only 1 real option and risking infeasibility.
    """
    excluded       = {k: set(v) for k, v in prev_excluded.items()}
    banned_families = banned_families or set()

    for cat, slot_list in VARY_ACROSS_DAYS.items():
        for meal, s_idx in slot_list:

            # Find the food chosen in this (meal, slot) today
            items = [it for it in plan.get(meal, [])
                     if it["category"] == cat and it["slot"] == s_idx]
            if not items:
                continue

            chosen_idx     = items[0]["food_idx"]
            key            = (meal, s_idx)
            already_banned = excluded.get(key, set())
            fat_thresh     = FAT_FILTER.get(cat, 99)

            # Alt_{m,s,d}(j*): eligible foods for this slot that are not
            # already in H, not j* itself, and not from a banned family (FIX)
            alternatives = foods[
                (foods["Category"] == cat) &
                (foods[meal] == 1) &
                (foods["fat"] <= fat_thresh) &
                (~foods.index.isin(already_banned | {chosen_idx})) &
                (~foods["Food_Family"].isin(banned_families))          # FIX
            ]

            # ── MIN_ALT safety check (§2.5b) ─────────────────────────────────
            if len(alternatives) >= MIN_ALTERNATIVES_TO_BAN:
                excluded.setdefault(key, set()).add(chosen_idx)
            # else: do NOT add j* — too few alternatives remain
            # ─────────────────────────────────────────────────────────────────

    return excluded


def update_family_counts(plan, family_counts):
    counts = dict(family_counts)
    for meal in MEALS:
        for it in plan.get(meal, []):
            fam = it.get("food_family", "other")
            if fam and fam != "other":
                counts[fam] = counts.get(fam, 0) + 1
    return counts


def get_banned_families(family_counts, n_days_remaining):
    """C6 — Return food families that have hit their plan-wide limit."""
    banned = set()
    for family, limit in FAMILY_PLAN_LIMITS.items():
        if family_counts.get(family, 0) >= limit:
            banned.add(family)
    return banned


def _partial_excluded(excluded, keep_last_n=2):
    """
    FIX-5: Partial C21 relaxation — keep only the most recently banned food
    per slot (up to keep_last_n entries) instead of the full accumulation.

    When later days become infeasible because too many foods are banned across
    tracked slots, this intermediate relaxation clears old bans while keeping
    very recent ones (to preserve at least some cross-day variety). It sits
    between full-exclusions and no-exclusions in the retry ladder.
    """
    partial = {}
    for key, banned_set in excluded.items():
        # Keep only the last keep_last_n items added (sets are unordered,
        # so we use the size heuristic: if ≤ keep_last_n items, keep all)
        if len(banned_set) <= keep_last_n:
            partial[key] = set(banned_set)
        else:
            # Convert to list and keep last keep_last_n (arbitrary but consistent)
            partial[key] = set(list(banned_set)[-keep_last_n:])
    return partial


def _day_solve_attempts(master_seed, day, excluded, banned_families):
    """
    Progressive relaxation ladder (§5.8 relaxation note):
      1. Normal: C21 (full exclusions) + perturbed cost + C6 (family bans)
      2. No perturbation: C21 (full) + C6, exact costs
      3. Relaxed family: C21 (full), no C6
      4. Partial exclusions: C21 (last 2 bans only), no C6  [FIX-5: new step]
      5. No exclusions: neither C21 nor C6  (guaranteed feasible fallback)
    slot_off shifts the RNG seed so δ_{s,d} remains consistent with the
    ladder position but differs from the primary attempt.
    """
    partial_excl = _partial_excluded(excluded, keep_last_n=2)
    return [
        {"label": None,                 "perturb": True,  "excluded": excluded,      "family_banned": banned_families, "slot_off": 0,    "solve_off": 0},
        {"label": "no perturb",         "perturb": False, "excluded": excluded,      "family_banned": banned_families, "slot_off": 100,  "solve_off": 1},
        {"label": "relaxed family",     "perturb": False, "excluded": excluded,      "family_banned": set(),           "slot_off": 1000, "solve_off": 2},
        {"label": "partial exclusions", "perturb": False, "excluded": partial_excl,  "family_banned": set(),           "slot_off": 1500, "solve_off": 3},  # FIX-5
        {"label": "no exclusions",      "perturb": False, "excluded": {},            "family_banned": set(),           "slot_off": 2000, "solve_off": 4},
    ]


def solve_day_robust(user, foods, day, master_seed, excluded, banned_families):
    """Solve one day; retry with relaxations until macros pass or attempts exhausted."""
    plan, sol = None, "Infeasible"
    attempts  = _day_solve_attempts(master_seed, day, excluded, banned_families)

    for i, att in enumerate(attempts):
        # ── C20: seed = master_seed + day + slot_off  (§2.5a) ─────────────
        slot_rng = np.random.default_rng(master_seed + day + att["slot_off"])
        # ─────────────────────────────────────────────────────────────────

        triples = build_slot_triples(
            foods,
            excluded=att["excluded"],
            family_banned=att["family_banned"],
            user=user,
            rng=slot_rng,          # carries the Bernoulli draw for C20
        )
        plan, sol = solve_one_day(
            user, foods, triples,
            day_label=f"Day{day}" + (f"_{att['solve_off']}" if att["solve_off"] else ""),
            seed=master_seed + day * 17 + att["solve_off"],
            perturb_cost=att["perturb"],
        )
        if plan is not None and not plan_violates(plan, user):
            return plan, sol, att["label"]

        if i < len(attempts) - 1:
            next_label = attempts[i + 1]["label"]
            reason = next_label if plan is None else f"{next_label} (macro miss)"
            print(f"retry ({reason})... ", end="", flush=True)

    return plan, sol, None


def generate_multiday_plan(user, n_days=3, master_seed=None):
    print(user.summary())
    print(f"\n  Generating {n_days}-day plan...")
    print(f"  {'_' * 60}\n")

    foods        = load_foods(allergies=user.allergies)
    excluded     = {}     # H_{m,s,d}: starts empty on day 1 (§2.5b)
    family_counts = {}
    results      = []

    if master_seed is None:
        master_seed = int(time.time() * 1000) % 100000
    print(f"  Slot variety seed: {master_seed}  (pass master_seed=N to reproduce)\n")

    for day in range(1, n_days + 1):
        print(f"  Solving Day {day}...", end=" ", flush=True)

        days_remaining  = n_days - day
        banned_families = get_banned_families(family_counts, days_remaining)

        plan, sol, used = solve_day_robust(
            user, foods, day, master_seed, excluded, banned_families,
        )

        if used:
            print(f"[{used}] ", end="", flush=True)
        print(sol)
        results.append((day, plan, sol))

        if plan is not None and not plan_violates(plan, user):
            # C21 update: build H_{m,s,d+1} from today's solution
            # FIX: pass banned_families so alternatives count excludes unreachable foods
            excluded      = build_exclusions(plan, excluded, foods, banned_families)
            family_counts = update_family_counts(plan, family_counts)

    return results, foods


# ======================================================================
#  SECTION 7 — DISPLAY
# ======================================================================

CAT_LABEL = {
    "V":   "Vegetable",
    "C":   "Carb",
    "P":   "Protein",
    "F":   "Fruit",
    "S":   "Seeds/Nuts",
    "D":   "Dairy / Egg",
    "Fat": "Fat / Oil",
}
W = 70


def print_day(day_num, plan, sol, user):
    if plan is None:
        print(f"\n  DAY {day_num} — No solution [{sol}]\n")
        return

    totals   = {k: 0.0 for k in
                ["calories", "protein", "fat", "sat_fat",
                 "carbs", "fiber", "net_carbs", "cost"]}
    meal_cals = {}

    print(f"\n{'=' * W}")
    print(f"  DAY {day_num}  [{sol}]  —  Target: {user.cal_target:.0f} kcal")
    print(f"{'=' * W}")

    for meal in MEALS:
        items  = plan.get(meal, [])
        m_cal  = sum(it["calories"] for it in items)
        tgt    = user.cal_target * MEAL_CAL_FRAC[meal]
        diff   = m_cal - tgt
        meal_cals[meal] = m_cal

        print(f"\n  {meal.upper():<12}  "
              f"{m_cal:>5.0f} kcal  (target ~{tgt:.0f},  {diff:+.0f})")
        print(f"  {'-' * 66}")
        if not items:
            print("    (no items)")
            continue
        print(f"  {'Food':<35} {'g':>4}  {'kcal':>5}  "
              f"{'P':>5}  {'F':>5}  {'C':>5}  {'Fib':>4}  Type")
        print(f"  {'-' * 66}")
        for it in sorted(items, key=lambda x: -x["calories"]):
            cat = CAT_LABEL.get(it["category"], it["category"])
            fam = f" [{it['food_family']}]" if it["food_family"] not in ("other", "") else ""
            print(f"  {it['food']:<35} {it['grams']:>4}  "
                  f"{it['calories']:>5.0f}  {it['protein']:>5.1f}  "
                  f"{it['fat']:>5.1f}  {it['carbs']:>5.1f}  "
                  f"{it['fiber']:>4.1f}  {cat}{fam}")
        for k in totals:
            totals[k] += sum(it[k] for it in items)

    print(f"\n{'=' * W}")
    print(f"  TOTALS  DAY {day_num}")
    print(f"  {'-' * 66}")
    cal_d = totals["calories"] - user.cal_target
    for label, val, note in [
        ("Calories",  f"{totals['calories']:>7.0f} kcal",
         f"target {user.cal_target:.0f}  ({cal_d:+.0f})"),
        ("Protein",   f"{totals['protein']:>7.1f} g",  f"{user.protein_target} – {user.protein_max} g"),
        ("Fat",       f"{totals['fat']:>7.1f} g",      f"<= {user.fat_target} g"),
        ("Sat. Fat",  f"{totals['sat_fat']:>7.1f} g",
         f"({totals['sat_fat'] * 9 / max(1, totals['calories']) * 100:.1f}% of kcal)"),
        ("Carbs",     f"{totals['carbs']:>7.1f} g",    f"<= {user.carbs_target} g"),
        ("Net carbs", f"{totals['net_carbs']:>7.1f} g",
         f"<= {user.nc_max_day} g" if user.nc_max_day else "no daily cap"),
        ("Fiber",     f"{totals['fiber']:>7.1f} g",    f"{user.fiber_target} – {user.fiber_max} g"),
    ]:
        print(f"  {label:<10}  {val}    {note}")

    print()
    for name, ok, detail in [
        ("Calories",   user.cal_target * 0.97 <= totals["calories"] <= user.cal_target * 1.03,
         f"{totals['calories']:.0f} kcal  ({cal_d:+.0f},  band [{user.cal_target*0.97:.0f}–{user.cal_target*1.03:.0f}])"),
        ("Protein >=", totals["protein"] >= user.protein_target * (1 - ROUNDING_TOL),
         f"{totals['protein']:.1f}/{user.protein_target} g <= {user.protein_max} g" ),
        ("Protein <=", totals["protein"] <= user.protein_max * (1 + ROUNDING_TOL),
         f"{totals['protein']:.1f}/{user.protein_max} g"),
        ("Fat <=",     totals["fat"] <= user.fat_target * (1 + ROUNDING_TOL),
         f"{totals['fat']:.1f}/{user.fat_target} g"),
        ("Carbs <=",   totals["carbs"] <= user.carbs_target * (1 + ROUNDING_TOL),
         f"{totals['carbs']:.1f}/{user.carbs_target} g"),
        ("Fiber >=",   totals["fiber"] >= user.fiber_target * (1 - ROUNDING_TOL),
         f"{totals['fiber']:.1f}/{user.fiber_target} g <= {user.fiber_max} g"),
        ("Fiber <=",   totals["fiber"] <= user.fiber_max * (1 + ROUNDING_TOL),
         f"{totals['fiber']:.1f}/{user.fiber_max} g"),
        *([("Net carbs <=", totals["net_carbs"] <= user.nc_max_day * (1 + ROUNDING_TOL),
            f"{totals['net_carbs']:.1f}/{user.nc_max_day} g")]
          if user.nc_max_day else []),
    ]:
        print(f"  {'OK  ' if ok else 'WARN'}  {name:<12}  {detail}")
    print(f"{'=' * W}\n")


def print_multiday_summary(results, user):
    W2 = 70
    n  = len(results)
    print(f"\n{'#' * W2}")
    print(f"  {n}-DAY PLAN SUMMARY — {user.goal.upper()} — {user.cal_target:.0f} kcal/day")
    print(f"{'#' * W2}")
    for day, plan, sol in results:
        if plan is None:
            continue
        print(f"\n  Day {day}:")
        for meal in MEALS:
            items = ", ".join(it["food"] for it in plan.get(meal, []))
            print(f"    {meal:<12}: {items or '-'}")
    print(f"\n{'#' * W2}\n")


# ======================================================================
#  SECTION 8 — MAIN
# ======================================================================

if __name__ == "__main__":

    user = UserBiometrics(
        age=22,
        gender="male",
        height_cm=172,
        weight_kg=71,
        activity_level="very_Active",
        goal="gain",
        diabetes_status="none",
        hypertension_status="none",   # options: 'none' | 'cardiovascular'
        allergies=[],
    )

    N_DAYS = 6

    results, foods = generate_multiday_plan(user, n_days=N_DAYS)

    for day, plan, sol in results:
        print_day(day, plan, sol, user)

    print_multiday_summary(results, user)