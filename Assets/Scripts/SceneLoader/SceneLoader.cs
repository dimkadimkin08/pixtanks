using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    public string targetSceneName;
    public bool loadOnStart;
    private void Start()
    {
        if (loadOnStart)
        {
            LoadTargetScene();
        }
    }

    public void LoadTargetScene()
    {
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}