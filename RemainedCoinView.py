import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import glob
import os


def draw_density_map_with_points(folder_path, highlight_points, lowScore_points, size=20):
    file_list = glob.glob(os.path.join(folder_path, "*.csv"))
    num_files = len(file_list)
    if num_files == 0: return
    grid_data = np.zeros((size, size))
    step_value = 1.0 / num_files
    
    for file in file_list:
        df = pd.read_csv(file)
        unique_coords = df.iloc[:, [0, 1]].drop_duplicates()
        for _, row in unique_coords.iterrows():
            x, y = int(row.iloc[0]), int(row.iloc[1])
            if 0 <= x < size and 0 <= y < size:
                grid_data[y, x] += step_value

    fig, ax = plt.subplots(figsize=(10, 8))

    # 1. ヒートマップを描画 (背景)
    im = ax.imshow(grid_data, cmap="Greys", origin='lower', 
                   extent=[0, size, 0, size], vmin=0, vmax=1.0)

    # 2. 配列内の座標に赤い円を描画 (前面)
    if highlight_points:
        hx, hy = zip(*highlight_points)
        
        ax.scatter([x + 0.5 for x in hx], [y + 0.5 for y in hy], 
                   color='red', s=50, marker='o', label='High Score Coin Position', zorder=3)
        
    if lowScore_points:
        hx, hy = zip(*lowScore_points)
        
        ax.scatter([x + 0.5 for x in hx], [y + 0.5 for y in hy], 
                   color='blue', s=50, marker='*', label='Low Score Coin Position', zorder=3)

    # 装飾
    plt.xlabel("X Coordinate")
    plt.ylabel("Y Coordinate")
    plt.title("Locations of Remaining Coins")
    ax.set_xticks(np.arange(0, size + 1, 1))
    ax.set_yticks(np.arange(0, size + 1, 1))
    ax.grid(which='both', color='gray', linestyle='-', linewidth=0.5)
    plt.colorbar(im).set_label('Overlap Density (1.0 = Not collected in any of the patterns)')
    plt.legend()
    ax.set_aspect('equal')
    plt.show()

df_highScore_points = pd.read_csv("Assets/DataLog/HighScoreCoinData.csv")
highScore_points = list(zip(df_highScore_points.iloc[:, 0], df_highScore_points.iloc[:, 1]))
df_lowScore_points = pd.read_csv("Assets/DataLog/LowScoreCoinData.csv")
lowScore_points = list(zip(df_lowScore_points.iloc[:, 0], df_lowScore_points.iloc[:, 1]))
draw_density_map_with_points("Assets/DataLog/RemainedCoinPosData/Greedy", highScore_points, lowScore_points)
