
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldMapLocation : MonoBehaviour
{
    [SerializeField] private string sceneName;

    private void OnMouseDown()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Chua nhap ten Scene cho " + gameObject.name);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
