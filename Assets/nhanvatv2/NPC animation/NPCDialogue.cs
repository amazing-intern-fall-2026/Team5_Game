
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Người dân";
    [TextArea(2, 4)]
    public string[] dialogueLines =
    {
        "Xin chào!",
        "Bạn đang tìm kiếm thứ gì sao?"
    };

    public DialogueManager dialogueManager;

    public void Talk()
    {
        if (dialogueManager == null)
        {
            Debug.LogWarning("Chưa gán DialogueManager cho NPC!");
            return;
        }

        if (!dialogueManager.IsDialogueOpen())
        {
            dialogueManager.StartDialogue(npcName, dialogueLines);
        }
        else
        {
            dialogueManager.NextDialogue();
        }
    }
}
