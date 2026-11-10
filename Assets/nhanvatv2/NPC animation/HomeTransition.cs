
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeTransition : MonoBehaviour
{
    private bool playerNear = false;

    [SerializeField] private string targetScene = "demo1";

    private void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            Debug.Log("Nhấn E để trở về nông trại!");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}
