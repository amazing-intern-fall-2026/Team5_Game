using UnityEngine;
using UnityEngine.SceneManagement;

// Shared by any button or trigger that needs to reveal an overlay panel
// (Options, HowTo, etc). If the panel's scene is already loaded (the usual
// case, since OverlaySceneBootstrapper preloads them) it just re-enables
// the panel. If the scene was fully unloaded (e.g. OptionsMenu.CloseOptions
// unloads itself on Back), it loads the scene again first.
public static class OverlayPanel
{
    public static void Show(string sceneName, string panelName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (scene.isLoaded)
        {
            ShowPanel(scene, panelName);
            return;
        }

        void OnLoaded(Scene loadedScene, LoadSceneMode mode)
        {
            if (loadedScene.name != sceneName) return;
            SceneManager.sceneLoaded -= OnLoaded;
            ShowPanel(loadedScene, panelName);
        }

        SceneManager.sceneLoaded += OnLoaded;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    private static void ShowPanel(Scene scene, string panelName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeep(root.transform, panelName);
            if (found != null)
            {
                found.gameObject.SetActive(true);
                return;
            }
        }
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }

        return null;
    }
}
