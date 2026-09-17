using UnityEngine;

// Attach this to the GameManager object inside PauseMenu.unity (the same
// object PauseMenu.cs already lives on). Wire your new "Options" button's
// OnClick() to this component's OpenOptions() method.
public class OptionsButtonHandler : MonoBehaviour
{
    public void OpenOptions()
    {
        OverlayPanel.Show("OptionsMenu", "Options");
    }
}
