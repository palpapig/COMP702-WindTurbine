import matplotlib.pyplot as plt
import pandas as pd

###
### Input a dataframe with the 2 columns into blue_, red_, or green_data. If one isnt included, it isnt shown
### If filename isn't included, the graph it isnt saved
###

def One_Curve( blue_data, red_data = None, green_data = None, title = None, filename = None):


    fig, ax = plt.subplots(1, 1, figsize=(5, 5)) 

    #these variable names must be the same as the ones in the .csv
    y_label = 'Power'
    x_label = 'PitchAngle'

    if red_data is not None:
        ax.scatter(red_data[x_label], red_data[y_label],color='red', marker='o', s=5)
    ax.scatter(blue_data[x_label], blue_data[y_label],color='blue', marker='o', s=5)
    if green_data is not None:
        ax.scatter(green_data[x_label], green_data[y_label],color='red', marker='o', s=5)
    ax.set_xlabel(x_label)
    ax.set_ylabel(y_label)
    #ax.set_xlim(left=0)
    #ax.set_ylim(bottom=0)
    if title is not None:
        plt.suptitle(title)
    if filename is not None:
        plt.savefig(f"{filename}.png")
    if title is not None:
        plt.show()


#The filename is the name of the input csv and the name of the output graph
filename = "csharp_results_2"

df = pd.read_csv(f'data/{filename}.csv')

#Reading 3 columns from a csv, then renaming so they both have the same x and y names for the graph
actual = df[["PitchAngle", "Power"]]
predicted = df[["PitchAngle", "PredictedPower"]]

predicted.rename(columns={"PredictedPower":"Power"}, inplace=True)

One_Curve(blue_data=predicted, red_data=actual, title=f"Python", filename=f"graphs/{filename}")
