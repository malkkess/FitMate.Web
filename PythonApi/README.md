# FitMate Python Optimizer API

Run the Python optimizer service before generating a meal plan from the .NET backend.

```powershell
cd C:\Users\hp\source\repos\FitMate.Web\PythonApi
py -m uvicorn app:app --host 127.0.0.1 --port 8001
```

The .NET backend is already configured to call:

```text
http://localhost:8001/optimize
```

The wrapper uses:

- `optimizer_model.py`
- `Food_Data_GP_v3_modified.xlsx`

You can override paths with `FITMATE_MODEL_PATH` and `FITMATE_FOOD_XLSX`.
