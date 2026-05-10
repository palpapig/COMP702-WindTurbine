import sys
import json
import pandas as pd
from sklearn.svm import SVR
from sklearn.model_selection import train_test_split, GridSearchCV
from sklearn.metrics import PredictionErrorDisplay
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

import matplotlib.pyplot as plt


data_path = sys.argv[1]
model_path = sys.argv[2]
model_name = sys.argv[3]
dataset = pd.read_csv(data_path)

#take only first 5000 rows for faster grid searching
#dataset = dataset.head(4000)

#extract datasets from training data csv
xvar = dataset.columns[0] # = "inputVal"
yvar = dataset.columns[1] # = "power"

X = dataset[[xvar]]
y = dataset[yvar]



X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.33, random_state=42)

######## normal model training ######
svr = SVR(kernel="rbf", C=1, gamma="scale")
svr.fit(X_train, y_train,)

#get predictions using test data
y_pred = svr.predict(X_test)
####### end of normal model training ######


###### grid search ###### 
# search_params = {
#     "C": [0.01, 0.1, 1, 10, 100],
#     "gamma": [0.01, 0.1, 1, 10, 100],
#     #"epsilon": [0.01, 0.1, 1, 10, 100],
# }

# base_svr = SVR(kernel="rbf")

# grid_search = GridSearchCV(base_svr, search_params)
# svr = grid_search.fit(X_train, y_train)

# best_params = grid_search.get_params()

# with open('params.txt', 'w') as f:
#     for key, value in best_params.items():
#         try:
#             f.write(f"{key}: {value}\n")
#         except:
#             try:
#                 f.write(f"failed to serialize {key}\n")
#             except:
#                 pass
####### end of grid search #######


y_pred = svr.predict(X_test)


#TODO graphing or save to csv here for validaton purposes
display = PredictionErrorDisplay.from_predictions(y_test, y_pred, kind="actual_vs_predicted")
display.plot()
plt.savefig(f"PythonDegradationTraining/outputs/scilearn_{model_name}")
plt.show()

#combine test data and predictions into a csv for validation
results_df = pd.DataFrame({
    'PitchAngle': X_test[xvar].values,
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
    "ExpectedDeviation": expected_deviation_percent,
    "Message": "Model trained without issue"
}

print(json.dumps(response))





#code outline:

#do a train-test split?

#normalize training data?

#TODO
#do grid search for best hyperprams using training data
    #either have them be tightly bunched assumed-correct params,
    #OR, do a double grid search where you take the best params and do a tighter search around that. 

#TODO (try out the current graph function)
#validation, not for production: also have the testing data passed and do this:
    #also test data to get RMSE, R2, MAE values
    #put test/actual data into grapher


#python creates ONNX file

#calculate expected degradation percentage.

#return to C#