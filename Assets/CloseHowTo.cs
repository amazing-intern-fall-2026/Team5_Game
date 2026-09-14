using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Drag your "HowTo" parent GameObject here in the Inspector
    public GameObject howToPanel;

    // The Close Button will call this method
    public void CloseHowToUI()
    {
        if (howToPanel != null)
        {
            howToPanel.SetActive(false);
        }
    }

    // The Sign will call this method
    public void OpenHowToUI()
    {
        if (howToPanel != null)
        {
            howToPanel.SetActive(true);
        }
    }
}