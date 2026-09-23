using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneSwitcherWindow : EditorWindow
{
    private List<string> scenePaths = new List<string>();
    private Vector2 scrollPosition;

    [MenuItem("CustomTools/Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcherWindow>("Scene Switcher");
    }

    private void OnEnable()
    {
        LoadScenes();
    }

    private void LoadScenes()
    {
        scenePaths.Clear();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenePaths.Add(scene.path);
            }
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Update scenes"))
        {
            LoadScenes();
        }

        EditorGUILayout.Space();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (string scenePath in scenePaths)
        {
            if (GUILayout.Button(System.IO.Path.GetFileNameWithoutExtension(scenePath)))
            {
                OpenScene(scenePath);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void OpenScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}