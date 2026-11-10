
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text npcNameText;
    public TMP_Text dialogueText;

    private string[] sentences;
    private int index;

    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(string npcName, string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        sentences = lines;
        index = 0;

        npcNameText.text = npcName;
        dialoguePanel.SetActive(true);
        dialogueText.text = sentences[index];
    }

    public void NextDialogue()
    {
        if (sentences == null || sentences.Length == 0)
            return;

        index++;

        if (index >= sentences.Length)
        {
            EndDialogue();
            return;
        }

        dialogueText.text = sentences[index];
    }

    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        sentences = null;
    }

    public bool IsDialogueOpen()
    {
        return dialoguePanel.activeSelf;
    }
}
