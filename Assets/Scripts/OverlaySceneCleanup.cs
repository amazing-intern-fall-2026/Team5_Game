using UnityEngine;
using UnityEngine.EventSystems;

// Attach to the EventSystem and/or Main Camera object inside an overlay
// scene (PauseMenu, OptionsMenu, HowToPlay). When that scene loads
// additively on top of a base scene that already has one, this copy
// destroys itself instead of creating a duplicate.
public class OverlaySceneCleanup : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<EventSystem>() != null &&
            FindObjectsOfType<EventSystem>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        if (GetComponent<Camera>() != null &&
            FindObjectsOfType<Camera>().Length > 1)
        {
            Destroy(gameObject);
        }
    }
}
