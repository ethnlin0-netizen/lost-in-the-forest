using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class MenuButtonSfx : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private UnityEngine.UI.Button button;
    private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); button.onClick.AddListener(Click); }
    public void OnPointerEnter(PointerEventData data) => Focus();
    public void OnSelect(BaseEventData data) => Focus();
    private void Focus() { if (button != null && button.IsInteractable()) GameAudioController.Instance?.Play(GameSound.MenuFocus, this, 1f, true); }
    private void Click() { GameAudioController.Instance?.Play(GameSound.MenuClick, this, 1f, true); }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Click); }
}
