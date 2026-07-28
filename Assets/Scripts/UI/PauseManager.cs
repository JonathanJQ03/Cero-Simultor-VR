using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// Gestiona la pausa del juego solo en la escena Piso Quirofano.
/// El componente ES el Canvas raíz: al destruirse lleva todo el UI consigo.
[DefaultExecutionOrder(-90)]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
public class PauseManager : MonoBehaviour
{
    static Color C(int h) => new Color(((h>>16)&0xFF)/255f,((h>>8)&0xFF)/255f,(h&0xFF)/255f);

    static readonly Color BG_OVERLAY = new Color(0.04f, 0.09f, 0.14f, 0.88f);
    static readonly Color TEAL       = C(0x1DC9B7);
    static readonly Color TEAL_BTN   = C(0x1BAF9F);
    static readonly Color RED_BTN    = C(0xBB2020);
    static readonly Color PANEL_BG   = C(0x0D1B26);

    bool        _isPaused;
    GameObject  _pausePanel;
    GameObject  _pauseBtn;

    // ── Awake: configurar Canvas y construir UI ───────────────────────────────
    void Awake()
    {
        // Configurar el Canvas que vive en este mismo GO
        var cv = GetComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;

        var sc = GetComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight  = 0.5f;

        // EventSystem — necesario para que los botones reciban clics
        EnsureEventSystem();

        BuildPauseButton();
        BuildPausePanel();
        _pausePanel.SetActive(false);
    }

    // ESC como atajo; Update funciona incluso con timeScale=0
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    // ── EventSystem ───────────────────────────────────────────────────────────
    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    // ── Botón de pausa (esquina superior derecha) ─────────────────────────────
    void BuildPauseButton()
    {
        _pauseBtn = new GameObject("PauseButton");
        _pauseBtn.transform.SetParent(transform, false);

        var rt = _pauseBtn.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);
        rt.sizeDelta        = new Vector2(72f, 72f);

        var img    = _pauseBtn.AddComponent<Image>();
        img.sprite = MakeRoundedSprite(64, 64, 14);
        img.type   = Image.Type.Sliced;
        img.color  = new Color(0.08f, 0.18f, 0.28f, 0.88f);

        // Icono "||"
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(_pauseBtn.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        AnchorFill(iconRT);
        var icon        = iconGO.AddComponent<TextMeshProUGUI>();
        icon.text       = "| |";
        icon.fontSize   = 26;
        icon.fontStyle  = FontStyles.Bold;
        icon.color      = TEAL;
        icon.alignment  = TextAlignmentOptions.Center;

        var btn                    = _pauseBtn.AddComponent<Button>();
        btn.targetGraphic          = img;
        var cols                   = btn.colors;
        cols.normalColor           = new Color(0.08f, 0.18f, 0.28f, 0.88f);
        cols.highlightedColor      = new Color(0.14f, 0.28f, 0.40f, 1.00f);
        cols.pressedColor          = new Color(0.04f, 0.10f, 0.18f, 1.00f);
        cols.selectedColor         = cols.normalColor;
        btn.colors                 = cols;
        btn.onClick.AddListener(TogglePause);
    }

    // ── Panel de pausa (oculto por defecto) ───────────────────────────────────
    void BuildPausePanel()
    {
        // Overlay oscuro full-screen
        _pausePanel = new GameObject("PausePanel");
        _pausePanel.transform.SetParent(transform, false);

        var overlayRT = _pausePanel.AddComponent<RectTransform>();
        AnchorFill(overlayRT);

        var overlay   = _pausePanel.AddComponent<Image>();
        overlay.color = BG_OVERLAY;

        // Tarjeta central
        var card   = new GameObject("Card");
        card.transform.SetParent(_pausePanel.transform, false);

        var cardRT              = card.AddComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRT.pivot            = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta        = new Vector2(560f, 420f);
        cardRT.anchoredPosition = Vector2.zero;

        var cardImg    = card.AddComponent<Image>();
        cardImg.color  = PANEL_BG;
        cardImg.sprite = MakeRoundedSprite(256, 256, 28);
        cardImg.type   = Image.Type.Sliced;

        // Franja de acento teal en la parte superior
        var accentGO  = new GameObject("Accent");
        accentGO.transform.SetParent(card.transform, false);
        var accentRT  = accentGO.AddComponent<RectTransform>();
        Anchor(accentRT, 0.06f, 0.84f, 0.94f, 0.88f);
        accentGO.AddComponent<Image>().color = TEAL;

        // Título
        var titleGO        = new GameObject("Title");
        titleGO.transform.SetParent(card.transform, false);
        var titleRT        = titleGO.AddComponent<RectTransform>();
        Anchor(titleRT, 0.05f, 0.64f, 0.95f, 0.84f);
        var title          = titleGO.AddComponent<TextMeshProUGUI>();
        title.text         = "JUEGO EN PAUSA";
        title.fontSize     = 42;
        title.fontStyle    = FontStyles.Bold;
        title.color        = Color.white;
        title.alignment    = TextAlignmentOptions.Center;

        // Botón CONTINUAR
        BuildModalButton(card.transform, "CONTINUAR",       TEAL_BTN, 0.38f, OnContinuar);

        // Botón MENU PRINCIPAL
        BuildModalButton(card.transform, "MENU PRINCIPAL",  RED_BTN,  0.14f, OnMenuPrincipal);

        // Hint ESC
        var hintGO     = new GameObject("Hint");
        hintGO.transform.SetParent(card.transform, false);
        var hintRT     = hintGO.AddComponent<RectTransform>();
        Anchor(hintRT, 0.05f, 0.03f, 0.95f, 0.10f);
        var hint       = hintGO.AddComponent<TextMeshProUGUI>();
        hint.text      = "Pulsa ESC para continuar";
        hint.fontSize  = 18;
        hint.color     = new Color(1f, 1f, 1f, 0.30f);
        hint.alignment = TextAlignmentOptions.Center;
    }

    void BuildModalButton(Transform parent, string label, Color col,
                          float anchorMinY, UnityEngine.Events.UnityAction action)
    {
        float btnW = 430f, btnH = 80f;

        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var rt              = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, anchorMinY);
        rt.anchorMax        = new Vector2(0.5f, anchorMinY);
        rt.pivot            = new Vector2(0.5f, 0.0f);
        rt.sizeDelta        = new Vector2(btnW, btnH);
        rt.anchoredPosition = Vector2.zero;

        var img    = go.AddComponent<Image>();
        img.color  = col;
        img.sprite = MakeRoundedSprite(256, 80, 18);
        img.type   = Image.Type.Sliced;

        var txtGO      = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT      = txtGO.AddComponent<RectTransform>();
        AnchorFill(txtRT);
        var txt        = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text       = label;
        txt.fontSize   = 28;
        txt.fontStyle  = FontStyles.Bold;
        txt.color      = Color.white;
        txt.alignment  = TextAlignmentOptions.Center;

        var btn               = go.AddComponent<Button>();
        btn.targetGraphic     = img;
        var colors            = btn.colors;
        colors.normalColor    = col;
        colors.highlightedColor = col * 1.2f;
        colors.pressedColor   = col * 0.75f;
        colors.selectedColor  = col;
        btn.colors            = colors;
        btn.onClick.AddListener(action);

        // Brillo superior
        var shineGO   = new GameObject("Shine");
        shineGO.transform.SetParent(go.transform, false);
        var shineRT   = shineGO.AddComponent<RectTransform>();
        Anchor(shineRT, 0.005f, 0.52f, 0.995f, 0.995f);
        shineGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
    }

    // ── Lógica de pausa ───────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (_isPaused) Resume(); else Pause();
    }

    void Pause()
    {
        _isPaused      = true;
        Time.timeScale = 0f;
        _pausePanel.SetActive(true);
        _pauseBtn.SetActive(false);
    }

    void Resume()
    {
        _isPaused      = false;
        Time.timeScale = 1f;
        _pausePanel.SetActive(false);
        _pauseBtn.SetActive(true);
    }

    void OnContinuar()     => Resume();
    void OnMenuPrincipal() { Time.timeScale = 1f; SceneManager.LoadScene("MenuPrincipal"); }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void AnchorFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite MakeRoundedSprite(int w, int h, int r)
    {
        var tex        = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px         = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = Mathf.Max(0, Mathf.Max(r - x, x - (w - 1 - r)));
            float dy = Mathf.Max(0, Mathf.Max(r - y, y - (h - 1 - r)));
            float a  = Mathf.Clamp01(r + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
            px[y * w + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight,
            new Vector4(r, r, r, r));
    }
}
