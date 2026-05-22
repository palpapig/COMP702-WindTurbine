import matplotlib.pyplot as plt
import pandas as pd

###
### Input a dataframe with the 2 columns into blue_, red_, or green_data If one isnt included, it isnt shown
### If filename isn't included, the graph it isnt saved
###

def One_Curve(ax, blue_data, red_data, x_label, title):

    y_label = 'Power'
    
    #'GeneratorSpeed'
    #x_label = 'GeneratorSpeed or PitchAngle'
    #for ax, y_label in zip(axes, y_labels):
    if red_data is not None:
        ax.scatter(red_data[x_label], red_data[y_label],color='red', marker='o', s=5, label='Actual')
    ax.scatter(blue_data[x_label], blue_data[y_label],color='blue', marker='o', s=5, label='Predicted')
    ax.set_xlabel(x_label)
    ax.set_ylabel(y_label)
    #ax.set_xlim(left=0)

    ax.title.set_text(title)
    ax.legend()

    #uncomment this have graphs fit together smoothly
    ax.set_ylim(bottom=0, top=2100)
    
    

append = "grid-normalized-epsilon-"
filename = f"{append}csharp_WT-004"
#filename = f"{append}python_WT-004"
title = append


fig, axes = plt.subplots(1, 2, figsize=(10, 5)) 


### Read csvs and rename columns, create two graphs, one for each region ###

r2_df = pd.read_csv(f'region_testing/data/{filename}-region2.csv')

r2_actual = r2_df[["GeneratorSpeed", "Power"]]
r2_predicted = r2_df[["GeneratorSpeed", "PredictedPower"]]

r2_predicted.rename(columns={"PredictedPower":"Power"}, inplace=True)

One_Curve(ax=axes[0], blue_data=r2_predicted, red_data=r2_actual, x_label="GeneratorSpeed", title="Region 2\nWind Speed 3.5 - 9 m/s")



r2p5_df = pd.read_csv(f'region_testing/data/{filename}-region2p5.csv')

r2p5_actual = r2p5_df[["PitchAngle", "Power"]]
r2p5_predicted = r2p5_df[["PitchAngle", "PredictedPower"]]

r2p5_predicted.rename(columns={"PredictedPower":"Power"}, inplace=True)

One_Curve(ax=axes[1], blue_data=r2p5_predicted, red_data=r2p5_actual, x_label="PitchAngle", title="Region 2.5\nWindSpeed 9 - 12.5 m/s")






if title is not None:
    plt.suptitle(title)
if filename is not None:
    plt.savefig(f"region_testing/graphs/{filename}-scaled.png")
    plt.show()