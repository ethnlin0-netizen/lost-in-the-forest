using UnityEngine;
using UnityEngine.UI;

public sealed class StoryDialogue : MonoBehaviour
{
    public Canvas canvas;
    public Image backdrop;
    public GameObject dialoguePanel, celebrationStage;
    public Text speaker, message, hint;
    public RectTransform sleepingTK, celebratingLenny;

    public void ShowIntroduction(string text)
    {
        canvas.enabled = true;
        PositionDialogue(.74f, .96f);
        backdrop.color = new Color(.025f, .04f, .055f, .25f);
        celebrationStage.SetActive(false);
        ShowText("Lenny", text, "Enter / Space: continue");
    }

    public void ShowCelebration()
    {
        canvas.enabled = true;
        PositionDialogue(.04f, .26f);
        backdrop.color = new Color(.025f, .04f, .055f, .94f);
        dialoguePanel.SetActive(false);
        celebrationStage.SetActive(true);
        sleepingTK.localRotation = Quaternion.Euler(0, 0, 90);
    }

    public void ShowText(string name, string text, string instructions)
    {
        dialoguePanel.SetActive(true);
        speaker.text = name;
        message.text = text;
        hint.text = instructions;
    }

    public void Hide() { canvas.enabled = false; }

    private void PositionDialogue(float bottom, float top)
    {
        var panel = (RectTransform)dialoguePanel.transform;
        panel.anchorMin = new Vector2(.06f, bottom);
        panel.anchorMax = new Vector2(.94f, top);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
    }
}
