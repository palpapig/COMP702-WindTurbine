import pandas as pd

# load file
df = pd.read_csv("trainingReady.csv")

# clean column names
df.columns = df.columns.str.strip()

# drop time column
df = df.drop(columns=["Timestamp"], errors="ignore")

# convert all remaining columns to numeric
df = df.apply(pd.to_numeric, errors="coerce")

# drop rows with missing values
df = df.dropna()

# compute correlation
corr_matrix = df.corr()

print(corr_matrix)

# correlation with target
print("\nCorrelation with GearboxOilTemp:")
print(corr_matrix["GearboxOilTemp"].sort_values(ascending=False))