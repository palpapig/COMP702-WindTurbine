import sys
import json
import pandas as pd
from sklearn.svm import SVR
from sklearn.model_selection import train_test_split, GridSearchCV
from sklearn.metrics import PredictionErrorDisplay
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

import matplotlib.pyplot as plt

do_grid_search = True

data_path = sys.argv[1]
model_path = sys.argv[2]
model_name = sys.argv[3]
dataset = pd.read_csv(data_path)

is_region_2p5 = model_name[-1] == "5" #if region is 2p5, not 2...
region = "2p5" if is_region_2p5 else "2"


#take only first 4000 rows for faster grid searching
if do_grid_search:
    dataset = dataset.head(4000)

#extract datasets from training data csv
xvar = dataset.columns[0] # = "inputVal"
yvar = dataset.columns[1] # = "power"

X = dataset[[xvar]]
y = dataset[yvar]



X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.33, random_state=42)

#calculate Epsilon from IQR of y_train
Q1 = y_train.quantile(0.25)
Q3 = y_train.quantile(0.75)
IQR = Q3 - Q1
epsilon = IQR / 13.49

if not do_grid_search:
    ######## normal model training ######
    #create pipeline to include scaling
    svr = Pipeline([
        ('scaler', StandardScaler()),
        ('svr', SVR(kernel="rbf", C=1, gamma="scale"))
    ])
    #svr = SVR(kernel="rbf", C=1, gamma="scale", epsilon=epsilon)

    svr.fit(X_train, y_train,)

    #get predictions using test data
    y_pred = svr.predict(X_test)
    ####### end of normal model training ######

else:
    ###### grid search ###### 
    search_params = {
        "svr__C": [0.01, 0.1, 1, 10, 100],
        "svr__gamma": [0.01, 0.1, 1, 10, 100],
        #"svr__epsilon": [0.01, 0.1, 1, 10, 100],
    }

    #base_svr = SVR(kernel="rbf", epsilon=epsilon)
    base_svr = Pipeline([
        ('scaler', StandardScaler()),
        ('svr', SVR(kernel="rbf", epsilon=epsilon))
    ])

    grid_search = GridSearchCV(base_svr, search_params)
    svr = grid_search.fit(X_train, y_train)
    

    best_params = grid_search.best_params_

    with open(f'PythonDegradationTraining/outputs/params-r{region}.txt', 'w') as f:
        for key, value in best_params.items():
            try:
                f.write(f"{key}: {value}\n")
            except:
                try:
                    f.write(f"failed to serialize {key}\n")
                except:
                    pass
    ####### end of grid search #######


y_pred = svr.predict(X_test)


#graphing or save to csv here for manual validaton purposes
display = PredictionErrorDisplay.from_predictions(y_test, y_pred, kind="actual_vs_predicted")
display.plot()
plt.savefig(f"PythonDegradationTraining/outputs/scilearn_{model_name}")
#plt.show()

#combine test data and predictions into a csv for later manual validation
x_var_name = 'GeneratorSpeed'
if (is_region_2p5): 
    x_var_name = "PitchAngle"

results_df = pd.DataFrame({
    x_var_name: X_test[xvar].values,
    'Power': y_test.values,
    'PredictedPower': y_pred
})
results_df.to_csv(f"PythonDegradationTraining/outputs/python_{model_name}.csv", index=False)

#get expected deviation value of the model
residuals = y_test - y_pred #subtract every value in list y_test by the corresponding value in y_pred 
expected_deviation = sum(residuals) / sum(y_test)
expected_deviation_percent = expected_deviation * 100

#save trained SVR to file as ONNX
onnx_model = convert_sklearn(
    svr,
    initial_types=[
        (xvar, FloatTensorType([None, 1]))
    ]
)

# Save model
with open(model_path, "wb") as f:
    f.write(onnx_model.SerializeToString())

#return expected deviation to C#
response = {
    "Success": True,
    "ExpectedDeviationPercentage": expected_deviation_percent,
    "Message": "Model trained without issue"
}

print(json.dumps(response))




