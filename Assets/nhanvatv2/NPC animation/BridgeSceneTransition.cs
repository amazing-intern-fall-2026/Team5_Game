
using UnityEngine;
using UnityEngine.SceneManagement;

public class BridgeSceneTransition : MonoBehaviour
{
    [Header("Scene cần chuyển đến")]
    [SerializeField] private string targetScene = "demo4";

    [Header("Phím tương tác")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool playerInRange = false;

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            GoToTargetScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Đã đến cầu! Nhấn E để đi đến chợ.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void GoToTargetScene()
    {
        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogError(
                "Không thể tải Scene: " + targetScene +
                ". Hãy kiểm tra tên Scene và Build Profiles."
            );
        }
    }
}
