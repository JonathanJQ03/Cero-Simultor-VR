using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;

public static class PisoQuirofanoSetup
{
    [MenuItem("Tools/Setup Piso Quirofano Canvas")]
    public static void SetupScene()
    {
        foreach (var n in new[] { "Canvas_GameFlow", "Canvas_Flash", "EventSystem" })
        {
            var f = GameObject.Find(n);
            if (f != null) Object.DestroyImmediate(f);
        }

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var col_bg = new Color(0.04f, 0.08f, 0.10f, 0.96f);
        var col_green = new Color(0f, 1f, 0.25f, 1f);
        var col_orange = new Color(1f, 0.6f, 0f, 1f);
        var col_red = new Color(0.7f, 0f, 0f, 1f);

        // ===== MAIN CANVAS =====
        var cGO = new GameObject("Canvas_GameFlow");
        var cRT = cGO.AddComponent<RectTransform>();
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        cGO.AddComponent<CanvasScaler>();
        cGO.AddComponent<GraphicRaycaster>();
        cRT.sizeDelta = new Vector2(1920, 1080);
        cGO.transform.position = new Vector3(0, 1.5f, 2.5f);
        cGO.transform.localScale = Vector3.one * 0.001f;
        var flow = cGO.AddComponent<GameFlowController>();

        // ===== PANEL 1: ESCENA ACCIDENTE =====
        var p1 = MakePanel("Panel_EscenaAccidente", cGO.transform, col_bg, true);
        MakeTMP("Titulo_Accidente", p1.transform, "ESCENA DE ACCIDENTE", 68, col_green, 0.08f, 0.77f, 0.92f, 0.96f);
        MakeTMP("Desc_Accidente", p1.transform,
            "Accidente de transito - trauma multiple.\nPaciente masculino, 34 anos, 78 kg.\nHemorragia activa en extremidad inferior derecha.\nEs necesario actuar de inmediato.",
            34, Color.white, 0.08f, 0.35f, 0.92f, 0.76f);
        var b1 = MakeButton("Btn_Siguiente", p1.transform, "SIGUIENTE", 0.35f, 0.10f, 0.65f, 0.22f, new Color(0f, 0.55f, 0.28f), 40);
        b1.onClick.AddListener(() => { if (GameFlowController.Instance != null) GameFlowController.Instance.BtnSiguiente(); });

        // ===== PANEL 2: REPORTE PACIENTE =====
        var p2 = MakePanel("Panel_ReportePaciente", cGO.transform, col_bg, false);
        var rCtrl = p2.AddComponent<PatientReportController>();
        MakeTMP("Titulo_Reporte", p2.transform, "REPORTE DE INGRESO DEL PACIENTE", 54, col_green, 0.04f, 0.87f, 0.96f, 0.98f);
        MakeTMP("Info_Paciente_Header", p2.transform, "Paciente: M | 34 anos | 78 kg   Causa: Accidente de transito", 28, new Color(0.75f,0.75f,0.75f), 0.04f, 0.79f, 0.96f, 0.87f);

        TextMeshProUGUI fcV, fcS, s2V, s2S, paV, paS, shV, shS;
        MakeVitalBox(p2.transform, "FC", "128 bpm", "TAQUICARDIA", col_orange, 0.03f, 0.60f, 0.26f, 0.77f, out fcV, out fcS);
        MakeVitalBox(p2.transform, "SpO2", "88 %", "HIPOXIA CRITICA", col_red, 0.27f, 0.60f, 0.50f, 0.77f, out s2V, out s2S);
        MakeVitalBox(p2.transform, "PA", "90/60", "HIPOTENSION", col_orange, 0.51f, 0.60f, 0.74f, 0.77f, out paV, out paS);
        MakeVitalBox(p2.transform, "Shock", "III", "SEVERO", col_red, 0.75f, 0.60f, 0.97f, 0.77f, out shV, out shS);
        rCtrl.fcValueText = fcV; rCtrl.fcStatusText = fcS;
        rCtrl.spo2ValueText = s2V; rCtrl.spo2StatusText = s2S;
        rCtrl.paValueText = paV; rCtrl.paStatusText = paS;
        rCtrl.shockValueText = shV; rCtrl.shockStatusText = shS;

        rCtrl.patientInfoText = MakeTMP("Info_Paciente_Full", p2.transform, "", 28, new Color(0.7f,0.7f,0.7f), 0.04f, 0.35f, 0.96f, 0.59f, TextAlignmentOptions.TopLeft);
        var bProc = MakeButton("Btn_Proceder", p2.transform, "PROCEDER A SELECCION DE HERRAMIENTAS", 0.15f, 0.07f, 0.85f, 0.20f, new Color(0f, 0.45f, 0.22f), 30);
        rCtrl.btnProceder = bProc;

        // ===== PANEL 3: SELECCION HERRAMIENTAS =====
        var p3 = MakePanel("Panel_SeleccionHerramientas", cGO.transform, col_bg, false);
        var tMgr = p3.AddComponent<ToolSelectionManager>();
        MakeTMP("Titulo_Herramientas", p3.transform, "SELECCION DE HERRAMIENTAS", 50, col_green, 0.04f, 0.88f, 0.96f, 0.99f);
        var cLbl = MakeTMP("Counter_Herramientas", p3.transform, "Selecciona 5 de 9 herramientas (0/5)", 26, col_orange, 0.04f, 0.81f, 0.96f, 0.88f);
        tMgr.counterText = cLbl;

        string[] tIds = { "Bisturi", "TijerasDeTrauma", "VendasHemo", "Gasas", "Torniquete", "Epinefrina", "Desfibrilador", "Laringoscopio", "CanulaDeGuedel" };
        string[] tNames = { "Bisturi", "Tijeras Trauma", "Vendas Hemo", "Gasas", "Torniquete", "Epinefrina", "Desfibrilador", "Laringoscopio", "Canula Guedel" };
        string[] tDescs = {
            "Realiza incision quirurgica para exponer la herida de la extremidad.",
            "Corta ropa y tejido para exponer zona de trauma rapidamente.",
            "Vendas con agentes hemostaticos para control de hemorragia.",
            "Gasas estandar para limpieza del campo operatorio.",
            "Torniquete para comprimir proximal a la herida y detener hemorragia.",
            "Epinefrina 1mg IV en bolo para paro cardiaco.",
            "Descarga electrica bifasica para restaurar ritmo sinusal.",
            "Permite visualizar y manejar la via aerea superior.",
            "Mantiene la via aerea permeable en paciente inconsciente."
        };

        var entries = new List<ToolSelectionManager.ToolEntry>();
        float gx0 = 0.02f, gx1 = 0.65f, gy0 = 0.21f, gy1 = 0.80f;
        float cw = (gx1 - gx0) / 3f, ch = (gy1 - gy0) / 3f;
        for (int i = 0; i < tIds.Length; i++)
        {
            int col = i % 3, row = 2 - (i / 3);
            var btn = MakeButton("Btn_" + tIds[i], p3.transform, tNames[i],
                gx0 + col * cw + 0.004f, gy0 + row * ch + 0.004f,
                gx0 + (col + 1) * cw - 0.004f, gy0 + (row + 1) * ch - 0.004f,
                new Color(0.07f, 0.11f, 0.14f), 24);
            var e = new ToolSelectionManager.ToolEntry();
            e.toolId = tIds[i]; e.displayName = tNames[i]; e.description = tDescs[i]; e.button = btn;
            entries.Add(e);
        }
        tMgr.tools = entries;

        var iBox = new GameObject("ToolInfoBox");
        iBox.transform.SetParent(p3.transform, false);
        SetRect(iBox.AddComponent<RectTransform>(), 0.67f, 0.21f, 0.98f, 0.80f);
        iBox.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.16f, 0.96f);
        iBox.SetActive(false);
        tMgr.toolInfoBox = iBox;
        tMgr.toolInfoNameText = MakeTMP("ToolInfoName", iBox.transform, "Herramienta", 36, col_green, 0.05f, 0.76f, 0.95f, 0.98f);
        tMgr.toolInfoDescText = MakeTMP("ToolInfoDesc", iBox.transform, "Pasa el cursor sobre una herramienta.", 24, Color.white, 0.05f, 0.05f, 0.95f, 0.75f, TextAlignmentOptions.TopLeft);

        var bIng = MakeButton("Btn_Ingresar", p3.transform, "INGRESAR AL QUIROFANO", 0.20f, 0.06f, 0.80f, 0.19f, new Color(0.22f, 0.22f, 0.22f, 0.85f), 34);
        bIng.interactable = false;
        tMgr.btnIngresar = bIng;
        tMgr.btnIngresarLabel = bIng.GetComponentInChildren<TextMeshProUGUI>();

        // ===== PANEL 4: RESULTADO =====
        // ResultsController lives on _GameManagers and builds its own overlay canvas procedurally.
        var p4 = MakePanel("Panel_Resultado", cGO.transform, col_bg, false);

        // Wire GameFlowController
        flow.panelEscenaAccidente = p1;
        flow.panelReportePaciente = p2;
        flow.panelSeleccionHerramientas = p3;
        flow.panelResultado = p4;

        // ===== FLASH CANVAS =====
        var fGO = new GameObject("Canvas_Flash");
        var fRT = fGO.AddComponent<RectTransform>();
        var fCv = fGO.AddComponent<Canvas>();
        fCv.renderMode = RenderMode.WorldSpace;
        fCv.sortingOrder = 10;
        fGO.AddComponent<GraphicRaycaster>();
        fRT.sizeDelta = new Vector2(1920, 1080);
        fGO.transform.position = new Vector3(0, 1.5f, 2.49f);
        fGO.transform.localScale = Vector3.one * 0.001f;
        var fCtrl = fGO.AddComponent<FeedbackFlashController>();
        var oGO = new GameObject("FlashOverlay");
        oGO.transform.SetParent(fGO.transform, false);
        StretchRect(oGO.AddComponent<RectTransform>());
        var oImg = oGO.AddComponent<Image>();
        oImg.color = Color.clear; oImg.raycastTarget = false;
        fCtrl.flashOverlay = oImg;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] Canvas_GameFlow + Canvas_Flash created and wired.");
    }

    static GameObject MakePanel(string name, Transform parent, Color bg, bool active)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        StretchRect(go.AddComponent<RectTransform>());
        go.AddComponent<Image>().color = bg;
        go.SetActive(active);
        return go;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text, int size, Color col, float x0, float y0, float x1, float y1, TextAlignmentOptions align = TextAlignmentOptions.Center)
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
        t.text = label; t.fontSize = size; t.color = Color.white; t.alignment = TextAlignmentOptions.Center; t.enableWordWrapping = true;
        return btn;
    }

    static void MakeVitalBox(Transform parent, string boxName, string val, string status, Color col, float x0, float y0, float x1, float y1, out TextMeshProUGUI valT, out TextMeshProUGUI stT)
    {
        var box = new GameObject("Box_" + boxName);
        box.transform.SetParent(parent, false);
        SetRect(box.AddComponent<RectTransform>(), x0, y0, x1, y1);
        box.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.05f, 0.9f);
        valT = MakeTMP("Val_" + boxName, box.transform, val, 44, col, 0f, 0.45f, 1f, 1f);
        stT = MakeTMP("St_" + boxName, box.transform, status, 22, col, 0.05f, 0f, 0.95f, 0.45f);
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
