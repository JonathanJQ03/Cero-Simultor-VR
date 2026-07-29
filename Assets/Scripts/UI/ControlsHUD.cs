using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Panel compacto en la esquina superior izquierda que muestra los controles del juego.
[DefaultExecutionOrder(-85)]
public class ControlsHUD : MonoBehaviour
{
    static readonly Color PANEL_BG  = new Color(0.04f, 0.09f, 0.14f, 0.82f);
    static readonly Color TEAL      = new Color(0.114f, 0.788f, 0.718f, 1f);
    static readonly Color HEADER_BG = new Color(0.07f, 0.15f, 0.22f, 1f);
    static readonly Color TEXT_COL  = new Color(0.92f, 0.95f, 0.97f, 1f);

    Canvas     _canvas;
    GameObject _body;
    bool       _expanded = true;

    void Awake()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 90;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        BuildHUD();
    }

    void BuildHUD()
    {
        // Contenedor raíz anclado arriba-izquierda
        var root = MakeRT("HUD_Root", transform);
        root.anchorMin        = new Vector2(0, 1);
        root.anchorMax        = new Vector2(0, 1);
        root.pivot            = new Vector2(0, 1);
        root.anchoredPosition = new Vector2(18, -18);
        root.sizeDelta        = new Vector2(310, 0);

        root.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var vlRoot = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlRoot.childControlWidth      = true;
        vlRoot.childControlHeight     = true;
        vlRoot.childForceExpandWidth  = true;
        vlRoot.childForceExpandHeight = false;
        vlRoot.spacing = 0;

        // ── Cabecera ──────────────────────────────────────────────────────────
        var header = MakeRT("Header", root);
        AddBG(header, HEADER_BG);
        AddMinHeight(header, 36);

        var hlHeader = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlHeader.childControlWidth      = true;
        hlHeader.childControlHeight     = true;
        hlHeader.childForceExpandWidth  = true;
        hlHeader.childForceExpandHeight = false;
        hlHeader.padding = new RectOffset(10, 6, 6, 6);
        hlHeader.spacing = 4;

        var titleRT  = MakeRT("Title", header);
        var titleTMP = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "CONTROLES";
        titleTMP.fontSize  = 18;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = TEAL;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddFlexibleWidth(titleRT);

        // Botón minimizar
        var btnRT  = MakeRT("ToggleBtn", header);
        AddMinWidth(btnRT, 28);
        AddMinHeight(btnRT, 24);
        var btnImg = btnRT.gameObject.AddComponent<Image>();
        btnImg.color = new Color(1, 1, 1, 0);
        var btn = btnRT.gameObject.AddComponent<Button>();

        var lblRT  = MakeRT("Lbl", btnRT);
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP = lblRT.gameObject.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = "▼";
        lblTMP.fontSize  = 13;
        lblTMP.color     = TEAL;
        lblTMP.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(() => {
            _expanded = !_expanded;
            _body.SetActive(_expanded);
            lblTMP.text = _expanded ? "▼" : "▶";
        });

        // ── Cuerpo ────────────────────────────────────────────────────────────
        _body = MakeRT("Body", root).gameObject;
        var bodyRT = (RectTransform)_body.transform;
        AddBG(bodyRT, PANEL_BG);
        _body.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var vlBody = _body.AddComponent<VerticalLayoutGroup>();
        vlBody.childControlWidth      = true;
        vlBody.childControlHeight     = true;
        vlBody.childForceExpandWidth  = true;
        vlBody.childForceExpandHeight = false;
        vlBody.padding = new RectOffset(10, 10, 6, 8);
        vlBody.spacing = 3;

        BuildSection(bodyRT, "CONTROLES");
        BuildRow(bodyRT, "[ W ][ A ][ S ][ D ]", "Moverse");
        BuildRow(bodyRT, "[ Flechas ]",           "Girar");

        BuildSpacer(bodyRT);

        BuildSection(bodyRT, "MANO IZQUIERDA");
        BuildRow(bodyRT, "[ 1 ]", "Agarrar");
        BuildRow(bodyRT, "[ 2 ]", "Soltar");

        BuildSpacer(bodyRT);

        BuildSection(bodyRT, "MANO DERECHA");
        BuildRow(bodyRT, "[ 4 ]", "Agarrar");
        BuildRow(bodyRT, "[ 5 ]", "Soltar");
    }

    void BuildRow(RectTransform parent, string key, string desc)
    {
        var row = MakeRT("Row", parent);
        AddMinHeight(row, 22);

        var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.childControlWidth      = true;
        hl.childControlHeight     = true;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;
        hl.spacing = 8;

        var keyRT  = MakeRT("Key", row);
        AddMinWidth(keyRT, 148);
        var keyTMP = keyRT.gameObject.AddComponent<TextMeshProUGUI>();
        keyTMP.text      = key;
        keyTMP.fontSize  = 14f;
        keyTMP.fontStyle = FontStyles.Bold;
        keyTMP.color     = TEAL;
        keyTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var descRT  = MakeRT("Desc", row);
        AddFlexibleWidth(descRT);
        var descTMP = descRT.gameObject.AddComponent<TextMeshProUGUI>();
        descTMP.text      = desc;
        descTMP.fontSize  = 14f;
        descTMP.color     = TEXT_COL;
        descTMP.alignment = TextAlignmentOptions.MidlineLeft;
    }

    void BuildSection(RectTransform parent, string title)
    {
        var rt  = MakeRT("Section_" + title, parent);
        AddMinHeight(rt, 20);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text      = title;
        tmp.fontSize  = 13f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(1f, 1f, 1f, 0.45f);
        tmp.alignment = TextAlignmentOptions.BottomLeft;
    }

    void BuildSpacer(RectTransform parent)
    {
        var rt = MakeRT("Spacer", parent);
        AddMinHeight(rt, 4);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static RectTransform MakeRT(string n, Transform parent)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void AddBG(RectTransform rt, Color color)
    {
        rt.gameObject.AddComponent<Image>().color = color;
    }

    static void AddMinHeight(RectTransform rt, float h)
    {
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minHeight = h;
    }

    static void AddMinWidth(RectTransform rt, float w)
    {
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minWidth = w;
    }

    static void AddFlexibleWidth(RectTransform rt)
    {
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
    }
}
