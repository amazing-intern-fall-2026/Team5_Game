using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OverlaySceneBootstrapper : MonoBehaviour
{
    [System.Serializable]
    public struct Overlay
    {
        public string sceneName;
        public string panelObjectName;
    }

    [SerializeField]
    private Overlay[] overlays = new Overlay[]
    {
        new Overlay { sceneName = "PauseMenu", panelObjectName = "PauseMenu" },
        new Overlay { sceneName = "OptionsMenu", panelObjectName = "Options" },
        new Overlay { sceneName = "HowToPlay", panelObjectName = "HowTo" },
    };

    private IEnumerator Start()
    {
        foreach (var overlay in overlays)
        {
            if (SceneManager.GetSceneByName(overlay.sceneName).isLoaded)
                continue;

            AsyncOperation load = SceneManager.LoadSceneAsync(overlay.sceneName, LoadSceneMode.Additive);
            yield return load;

            HidePanel(overlay.sceneName, overlay.panelObjectName);
        }
    }

    private void HidePanel(string sceneName, string panelName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeep(root.transform, panelName);
            if (found != null)
            {
                found.gameObject.SetActive(false);
                return;
            }
        }
    }

    private Transform FindDeep(Transform parent, string name)
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
