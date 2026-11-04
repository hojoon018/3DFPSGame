using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string LoadingScene = "LoadingScene";

    public static string NextSceneName { get; private set; }

    public static void Load(string sceneName)
    {
        NextSceneName = sceneName;

        SceneManager.LoadScene(LoadingScene);
    }
}