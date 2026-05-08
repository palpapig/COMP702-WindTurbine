import sys
import json
import pandas as pd
from sklearn.svm import SVR
from sklearn.model_selection import train_test_split
from sklearn.metrics import PredictionErrorDisplay
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType


data_path = sys.argv[1]
model_path = sys.argv[2]
dataset = pd.read_csv(data_path)

#extract datasets from training data csv
xvar = dataset.columns[0] # = "inputVal"
yvar = dataset.columns[1] # = "power"

X = dataset[xvar]
y = dataset[yvar]

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.33, random_state=42)

#create and train SVR model on training data
svr = SVR(kernel="rbf", C=1, gamma=0.1, epsilon=0.01, tol=0.1)
svr.fit(X_train, y_train,)

#get predictions using test data
y_pred = svr.predict(X_test)

#TODO graphing or save to csv here for validaton purposes
display = PredictionErrorDisplay.from_predictions(y_test, y_pred)
display.plot()

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
with open("Models/.onnx", "wb") as f:
    f.write(onnx_model.SerializeToString())

#return expected deviation to C#
response = {
    "success": True,
    "expected_deviation": expected_deviation_percent,
    "message": "Model trained without issue"
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