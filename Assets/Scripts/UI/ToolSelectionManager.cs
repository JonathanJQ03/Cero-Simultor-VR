using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ToolSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class ToolEntry
    {
        public string toolId;
        public string displayName;
        [TextArea] public string description;
        public Button button;
    }

    [Header("Tools")]
    public List<ToolEntry> tools = new List<ToolEntry>();

    [Header("UI References")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI toolInfoNameText;
    public TextMeshProUGUI toolInfoDescText;
    public GameObject toolInfoBox;
    public Button btnIngresar;
    public TextMeshProUGUI btnIngresarLabel;

    [Header("Colors")]
    public Color selectedColor = new Color(0f, 1f, 0.25f);
    public Color deselectedColor = new Color(1f, 1f, 1f, 0.5f);
    public Color btnReadyColor = new Color(1f, 0.55f, 0f);
    public Color btnNotReadyColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);

    void OnEnable()
    {
        SetupButtons();
        if (toolInfoBox != null) toolInfoBox.SetActive(false);
        RefreshUI();
    }

    void SetupButtons()
    {
        foreach (var tool in tools)
        {
            var t = tool;
            if (t.button == null) continue;

            t.button.onClick.RemoveAllListeners();
            t.button.onClick.AddListener(() => OnToolClicked(t));

            var trigger = t.button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = t.button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((_) => ShowToolInfo(t));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => HideToolInfo());
            trigger.triggers.Add(exit);
        }

        if (btnIngresar != null)
        {
            btnIngresar.onClick.RemoveAllListeners();
            btnIngresar.onClick.AddListener(OnIngresarClicked);
        }
    }

    void OnToolClicked(ToolEntry tool)
    {
        if (GameFlowController.Instance == null) return;
        GameFlowController.Instance.ToggleTool(tool.toolId);
        ShowToolInfo(tool);
        RefreshUI();
    }

    void ShowToolInfo(ToolEntry tool)
    {
        if (toolInfoBox != null) toolInfoBox.SetActive(true);
        if (toolInfoNameText != null) toolInfoNameText.text = tool.displayName;
        if (toolInfoDescText != null) toolInfoDescText.text = tool.description;
    }

    void HideToolInfo()
    {
        if (toolInfoBox != null) toolInfoBox.SetActive(false);
    }

    void RefreshUI()
    {
        var flow = GameFlowController.Instance;
        int count = flow != null ? flow.SelectedTools.Count : 0;
        int required = flow != null ? flow.requiredToolCount : 5;

        if (counterText != null)
            counterText.text = count < required
                ? $"⚠ SELECCIONA {required} DE {tools.Count} HERRAMIENTAS  ({count}/{required})"
                : $"✓ SELECCION COMPLETA ({count}/{required})";

        foreach (var tool in tools)
        {
            if (tool.button == null) continue;
            bool selected = flow != null && flow.IsToolSelected(tool.toolId);

            var img = tool.button.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                img.color = selected ? new Color(0f, 0.25f, 0.1f, c.a) : new Color(0.1f, 0.1f, 0.1f, c.a);
            }

            var outline = tool.button.GetComponent<Outline>();
            if (outline == null) outline = tool.button.gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? selectedColor : deselectedColor;
            outline.effectDistance = selected ? new Vector2(3, 3) : new Vector2(1, 1);

            foreach (var t in tool.button.GetComponentsInChildren<TextMeshProUGUI>())
                t.color = selected ? selectedColor : Color.white;
        }

        if (btnIngresar != null)
        {
            bool ready = flow != null && flow.ReadyToSimulate();
            btnIngresar.interactable = ready;
            var img = btnIngresar.GetComponent<Image>();
            if (img != null) img.color = ready ? btnReadyColor : btnNotReadyColor;
            if (btnIngresarLabel != null)
                btnIngresarLabel.color = ready ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
    }

    void OnIngresarClicked()
    {
        if (GameFlowController.Instance != null)
            GameFlowController.Instance.BtnIngresarQuirofano();
    }
}
