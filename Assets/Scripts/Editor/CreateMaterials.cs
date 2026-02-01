using UnityEngine;
using UnityEditor;

public class MaterialGenerator
{
    [MenuItem("Tools/Create Materials")]
    public static void CreateMaterials()
    {
        // 保存先のフォルダ（あらかじめ作っておいてください）
        string folderPath = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        int count = 100;
        for (int i = 0; i < count; i++)
        {
            // 0.0(黒) から 1.0(白) までの値を計算
            float t = i / (float)(count - 1);
            Color color = new Color(t, t, t);

            // マテリアル作成（Standardシェーダーを使用）
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;

            // アセットとして保存
            string assetPath = $"{folderPath}/Material_{i:D3}.mat";
            AssetDatabase.CreateAsset(mat, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("100個のマテリアルを作成しました！");
    }
}