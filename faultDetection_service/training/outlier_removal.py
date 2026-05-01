from pathlib import Path
import pandas as pd

#physical filter limits (from Outlier Detection Methodology document)
POWER_UPPER_LIMIT = 2152.0   #5% above rated power (2050 kW) to remove extreme transient spikes while preserving normal peaks. Rated power from turbine specs
PITCH_COL = "Blade angle (pitch position) A (°)" #pitch angle >20° indicates turbine is stopped or severely curtailed 
ROTOR_COL = "Rotor speed (RPM)"
PITCH_UPPER_LIMIT = 20.0 #normal operation stays below 20°
ROTOR_LOWER_LIMIT = 11.0 #rotor speed <11 RPM captures start-up / shut-down transients. Below this, SCADA data becomes unreliable (averaging over 10-min intervals)

BASE_DIR = Path(__file__).resolve().parent.parent

RAW_DIR = BASE_DIR / "data" / "raw"
CLEANED_DIR = BASE_DIR / "data" / "cleaned"
CLEANED_DIR.mkdir(parents=True, exist_ok=True)

WIND_COL = "Wind speed (m/s)"
POWER_COL = "Power (kW)"


def clean_file(file_path: Path) -> None:
    print(f"Reading: {file_path.name}")

    #load CSV and clean column names (remove leading/trailing spaces)
    df = pd.read_csv(file_path)
    df.columns = df.columns.str.strip()

    #validate required columns exist
    if WIND_COL not in df.columns:
        raise ValueError(f"Missing column: {WIND_COL}")
    if POWER_COL not in df.columns:
        raise ValueError(f"Missing column: {POWER_COL}")

    before = len(df)

    # Convert wind speed and power to numeric
    df[WIND_COL] = pd.to_numeric(df[WIND_COL], errors="coerce")
    df[POWER_COL] = pd.to_numeric(df[POWER_COL], errors="coerce")

    # Remove rows where wind speed or power is missing
    df = df.dropna(subset=[WIND_COL, POWER_COL])

    # Basic physical cleaning
    df = df[df[WIND_COL] >= 0]
    df = df[df[POWER_COL] >= 0]

    # ====== NEW PHYSICAL FILTERS  ========
    # 1. Power > 2152 kW (extreme transient spikes)
    before_power = len(df)
    df = df[df[POWER_COL] <= POWER_UPPER_LIMIT]
    removed_power = before_power - len(df)
    print(f"Removed {removed_power} rows with power > {POWER_UPPER_LIMIT} kW")

    # 2. Pitch angle > 20° (turbine stopped or severely curtailed)
    if PITCH_COL in df.columns:
        df[PITCH_COL] = pd.to_numeric(df[PITCH_COL], errors="coerce")
        before_pitch = len(df)
        df = df[df[PITCH_COL] <= PITCH_UPPER_LIMIT]
        removed_pitch = before_pitch - len(df)
        print(f"Removed {removed_pitch} rows with pitch angle > {PITCH_UPPER_LIMIT}°")
    else:
        print(f"Warning: Column '{PITCH_COL}' not found – skipping pitch filter")

    # 3. Rotor speed < 11 RPM (start-up / shut-down transients)
    if ROTOR_COL in df.columns:
        df[ROTOR_COL] = pd.to_numeric(df[ROTOR_COL], errors="coerce")
        before_rotor = len(df)
        df = df[df[ROTOR_COL] >= ROTOR_LOWER_LIMIT]
        removed_rotor = before_rotor - len(df)
        print(f"Removed {removed_rotor} rows with rotor speed < {ROTOR_LOWER_LIMIT} RPM")
    else:
        print(f"Warning: Column '{ROTOR_COL}' not found – skipping rotor speed filter")

    #remove impossible / extreme wind speed values (above cut‑out, typical 25 m/s)
    df = df[df[WIND_COL] <= 30]

    #very low wind (<3 m/s) but high power (>50 kW) – physically impossible
    df = df[~((df[WIND_COL] < 3) & (df[POWER_COL] > 50))]

    #remove high wind (>5 m/s) but near‑zero power (<10 kW) – indicates fault or downtime
    df = df[~((df[WIND_COL] > 5) & (df[POWER_COL] < 10))]

    #IQR‑based outlier removal per wind speed bin (60 bins, ~0.5 m/s each)
    df["wind_bin"] = pd.cut(df[WIND_COL], bins=60)
    cleaned_parts = []

    for _, group in df.groupby("wind_bin", observed=False):
        if len(group) < 20:
            #too few points for reliable IQR – keep all
            cleaned_parts.append(group)
            continue

        q1 = group[POWER_COL].quantile(0.25)
        q3 = group[POWER_COL].quantile(0.75)
        iqr = q3 - q1

        lower = q1 - 1.5 * iqr
        upper = q3 + 1.5 * iqr

        cleaned_group = group[
            (group[POWER_COL] >= lower) &
            (group[POWER_COL] <= upper)
        ]

        cleaned_parts.append(cleaned_group)

    cleaned_df = pd.concat(cleaned_parts, ignore_index=True)

    cleaned_df = cleaned_df.drop(columns=["wind_bin"])

    #rename columns to match model trainer expectations
    rename_map = {
        'Wind speed (m/s)': 'windSpeed',
        'Rotor speed (RPM)': 'rotorSpeed',
        'Blade angle (pitch position) A (°)': 'pitchAngle',
        'Gear oil temperature (°C)': 'GearboxOilTemp',
        'Power (kW)': 'power'
    }
    cleaned_df.rename(columns=rename_map, inplace=True)    
    #added dummy temperature column (model trainer expects it)
    cleaned_df['temperature'] = cleaned_df['GearboxOilTemp']  

    after = len(cleaned_df)
    removed = before - after

    output_path = CLEANED_DIR / f"{file_path.stem}_cleaned.csv"
    cleaned_df.to_csv(output_path, index=False)

    print(f"Saved: {output_path.name}")
    print(f"Before: {before}")
    print(f"After: {after}")
    print(f"Removed: {removed}")
    print("-" * 40)


def main() -> None:
    csv_files = list(RAW_DIR.glob("*.csv"))

    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in {RAW_DIR}")

    for file_path in csv_files:
        clean_file(file_path)


if __name__ == "__main__":
    main()