using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArenaAsset))]
public class ArenaAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Print JSON to log"))
        {
            ArenaAsset asset = (ArenaAsset)target;
            string json = JsonUtility.ToJson(asset.arenaData, true);
            Debug.Log(json);
        }

        if (GUILayout.Button("Copy JSON to clipboard"))
        {
            ArenaAsset asset = (ArenaAsset)target;
            string json = JsonUtility.ToJson(asset.arenaData, true);
            EditorGUIUtility.systemCopyBuffer = json;
        }

        if (GUILayout.Button("Save JSON to file"))
        {
            ArenaAsset asset = (ArenaAsset)target;
            string json = JsonUtility.ToJson(asset.arenaData, true);

            string path = EditorUtility.SaveFilePanel(
                "Save Arena JSON",
                "",
                asset.name + ".json",
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
                Debug.Log("Saved to: " + path);
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Load JSON from file"))
        {
            ArenaAsset asset = (ArenaAsset)target;

            string path = EditorUtility.OpenFilePanel(
                "Load Arena JSON",
                "",
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                string json = System.IO.File.ReadAllText(path);

                Undo.RecordObject(asset, "Load Arena JSON");

                JsonUtility.FromJsonOverwrite(json, asset.arenaData);

                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                Debug.Log("Loaded from: " + path);
            }
        }
    }
}