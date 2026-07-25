using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ConfiguracionSceneSetup
{
    [MenuItem("Tools/Setup Configuracion Scene")]
    public static void SetupScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Configuracion.unity", OpenSceneMode.Single);
        if (!scene.IsValid()) { Debug.LogError("Could not open Configuracion scene"); return; }

        if (GameObject.Find("Main Camera") == null)
        {
            var cam = new GameObject("Main Camera");
            cam.AddComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            cam.GetComponent<Camera>().backgroundColor = new Color(0.04f, 0.08f, 0.10f);
            cam.tag = "MainCamera";
        }

        var cGO = new GameObject("Canvas_Configuracion");
        cGO.AddComponent<RectTransform>();
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();
        var cfg = cGO.AddComponent<ConfiguracionController>();

        var col_bg = new Color(0.04f, 0.08f, 0.10f, 1f);
        var col_green = new Color(0f, 1f, 0.25f, 1f);
        var col_dark = new Color(0.08f, 0.12f, 0.15f, 1f);

        var bg = new GameObject("Background");
        bg.transform.SetParent(cGO.transform, false);
        StretchRect(bg.AddComponent<RectTransform>());
        bg.AddComponent<Image>().color = col_bg;

        MakeTMP("Titulo", cGO.transform, "CONFIGURACION", 72, col_green, 0.05f, 0.87f, 0.95f, 0.98f);

        // AUDIO
        MakeTMP("Sec_Audio", cGO.transform, "AUDIO", 36, col_green, 0.05f, 0.78f, 0.30f, 0.86f);
        MakeTMP("Lbl_Vol", cGO.transform, "Volumen General", 28, Color.white, 0.05f, 0.70f, 0.30f, 0.77f);
        cfg.sliderVolumenGeneral = MakeSlider("Slider_VolGeneral", cGO.transform, 0.31f, 0.71f, 0.65f, 0.76f, 0.8f);
        cfg.volGeneralLabel = MakeTMP("Lbl_VolVal", cGO.transform, "80%", 28, Color.white, 0.66f, 0.70f, 0.75f, 0.77f);

        MakeTMP("Lbl_FX", cGO.transform, "Efectos de Sonido", 28, Color.white, 0.05f, 0.62f, 0.30f, 0.69f);
        cfg.sliderEfectosSonido = MakeSlider("Slider_FX", cGO.transform, 0.31f, 0.63f, 0.65f, 0.68f, 0.8f);
        cfg.volFxLabel = MakeTMP("Lbl_FXVal", cGO.transform, "80%", 28, Color.white, 0.66f, 0.62f, 0.75f, 0.69f);

        MakeTMP("Lbl_Music", cGO.transform, "Musica Ambiental", 28, Color.white, 0.05f, 0.54f, 0.30f, 0.61f);
        cfg.sliderMusicaAmbiental = MakeSlider("Slider_Music", cGO.transform, 0.31f, 0.55f, 0.65f, 0.60f, 0.5f);
        cfg.volMusicLabel = MakeTMP("Lbl_MusicVal", cGO.transform, "50%", 28, Color.white, 0.66f, 0.54f, 0.75f, 0.61f);

        // CALIDAD
        MakeTMP("Sec_Graphics", cGO.transform, "CALIDAD GRAFICA", 36, col_green, 0.05f, 0.44f, 0.45f, 0.52f);
        string[] quals = { "Baja", "Media", "Alta" };
        var qToggles = new Toggle[3];
        for (int i = 0; i < 3; i++)
        {
            qToggles[i] = MakeToggle("Toggle_Cal_" + quals[i], quals[i], cGO.transform, 0.05f + i * 0.12f, 0.36f, 0.16f + i * 0.12f, 0.43f, col_dark);
            qToggles[i].isOn = (i == 2);
        }
        cfg.togglesCalidad = qToggles;

        // VR COMFORT
        MakeTMP("Sec_VR", cGO.transform, "CONFORT VR", 36, col_green, 0.55f, 0.78f, 0.95f, 0.86f);
        MakeTMP("Lbl_Vig", cGO.transform, "Vigneado:", 26, Color.white, 0.55f, 0.70f, 0.95f, 0.77f);
        cfg.toggleVigneadoOn = MakeToggle("Toggle_VigOn", "Activo", cGO.transform, 0.55f, 0.62f, 0.72f, 0.69f, col_dark);
        cfg.toggleVigneadoOff = MakeToggle("Toggle_VigOff", "Inactivo", cGO.transform, 0.74f, 0.62f, 0.95f, 0.69f, col_dark);
        cfg.toggleVigneadoOff.isOn = true;

        MakeTMP("Lbl_Hand", cGO.transform, "Mano dominante:", 26, Color.white, 0.55f, 0.54f, 0.95f, 0.61f);
        cfg.toggleManoDerecha = MakeToggle("Toggle_ManoDer", "Derecha", cGO.transform, 0.55f, 0.46f, 0.72f, 0.53f, col_dark);
        cfg.toggleManoIzquierda = MakeToggle("Toggle_ManoIzq", "Izquierda", cGO.transform, 0.74f, 0.46f, 0.95f, 0.53f, col_dark);
        cfg.toggleManoDerecha.isOn = true;

        MakeTMP("Lbl_Mov", cGO.transform, "Movimiento:", 26, Color.white, 0.55f, 0.38f, 0.95f, 0.45f);
        cfg.toggleMovContinuo = MakeToggle("Toggle_MovCont", "Continuo", cGO.transform, 0.55f, 0.30f, 0.72f, 0.37f, col_dark);
        cfg.toggleMovTeleport = MakeToggle("Toggle_MovTel", "Teletransporte", cGO.transform, 0.74f, 0.30f, 0.95f, 0.37f, col_dark);
        cfg.toggleMovContinuo.isOn = true;

        cfg.btnVolver = MakeButton("Btn_Volver", cGO.transform, "VOLVER AL MENU", 0.05f, 0.04f, 0.30f, 0.14f, new Color(0.1f, 0.2f, 0.35f), 34);
        cfg.btnRestaurar = MakeButton("Btn_Restaurar", cGO.transform, "VALORES POR DEFECTO", 0.35f, 0.04f, 0.65f, 0.14f, new Color(0.25f, 0.15f, 0.05f), 28);

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] Configuracion scene ready.");
    }

    static Toggle MakeToggle(string name, string label, Transform parent, float x0, float y0, float x1, float y1, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRect(go.AddComponent<RectTransform>(), x0, y0, x1, y1);
        go.AddComponent<Image>().color = bg;
        var t = go.AddComponent<Toggle>();
        var lGO = new GameObject("Label");
        lGO.transform.SetParent(go.transform, false);
        StretchRect(lGO.AddComponent<RectTransform>());
        var tmp = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 24; tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
        return t;
    }

    static Slider MakeSlider(string name, Transform parent, float x0, float y0, float x1, float y1, float defaultVal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRect(go.AddComponent<RectTransform>(), x0, y0, x1, y1);
        go.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.25f);
        var s = go.AddComponent<Slider>();
        s.minValue = 0f; s.maxValue = 1f; s.value = defaultVal;
        return s;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text, int size, Color col, float x0, float y0, float x1, float y1, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRect(go.AddComponent<RectTransform>(), x0, y0, x1, y1);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col; t.alignment = align; t.enableWordWrapping = true;
        return t;
    }

    static Button MakeButton(string name, Transform parent, string label, float x0, float y0, float x1, float y1, Color col, int size = 32)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetRect(go.AddComponent<RectTransform>(), x0, y0, x1, y1);
        go.AddComponent<Image>().color = col;
        var btn = go.AddComponent<Button>();
        var lGO = new GameObject("Label");
        lGO.transform.SetParent(go.transform, false);
        StretchRect(lGO.AddComponent<RectTransform>());
        var t = lGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = size; t.color = Color.white; t.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
