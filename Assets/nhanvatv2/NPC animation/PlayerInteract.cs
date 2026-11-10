
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private NPCDialogue nearbyNPC;

    void Update()
    {
        if (nearbyNPC == null)
            return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            nearbyNPC.Talk();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        NPCDialogue npc = other.GetComponent<NPCDialogue>();

        if (npc != null)
        {
            nearbyNPC = npc;
            Debug.Log("Nhấn F để trò chuyện với " + npc.npcName);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        NPCDialogue npc = other.GetComponent<NPCDialogue>();

        if (npc != null && nearbyNPC == npc)
        {
            nearbyNPC = null;
        }
    }
}
