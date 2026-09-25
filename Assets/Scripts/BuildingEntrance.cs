using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildingEntrance : MonoBehaviour
{
    [SerializeField] private GameObject enterPrompt;

    // Tên Scene bên trong nhà kho
    [SerializeField] private string interiorSceneName = "Demo2";

    private bool playerInRange = false;

    private void Start()
    {
        if (enterPrompt != null)
        {
            enterPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        // Player đang đứng gần cửa và nhấn E
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            EnterBuilding();
        }
    }

    private void EnterBuilding()
    {
        SceneManager.LoadScene(interiorSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (enterPrompt != null)
        {
            enterPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (enterPrompt != null)
        {
            enterPrompt.SetActive(false);
        }
    }
}
