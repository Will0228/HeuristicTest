import pandas as pd
import matplotlib.pyplot as plt
import glob

# 1. 読み込むCSVファイルのリストを取得（例：dataフォルダ内の全てのcsv）
file_list_experiment = glob.glob("Assets/DataLog/ScoreData/*.csv") 
file_list_greedy = glob.glob("Assets/DataLog/ScoreData/Greedy/*.csv") 

plt.figure(figsize=(10, 6))

for file in file_list_experiment:
    df = pd.read_csv(file)
    plt.plot(df.iloc[:, 1], df.iloc[:, 0], color='black', alpha=0.5)

for file in file_list_greedy:
    df = pd.read_csv(file)
    plt.plot(df.iloc[:, 1], df.iloc[:, 0], color='red', alpha=1.0)

# 3. グラフの装飾
plt.title("Score Over Time")
plt.xlabel("Time")
plt.ylabel("Score")
plt.legend() # 凡例を表示
plt.grid(True) # グリッド線を表示

plt.show()