using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class SimulacionHUDSetup
{
    [MenuItem("Tools/Setup Simulacion HUD")]
    public static void Setup()
    {
        // 1. PatientController en medical_gurney
        var gurney = GameObject.Find("medical_gurney");
        if (gurney != null && gurney.GetComponent<PatientController>() == null)
        {
            gurney.AddComponent<PatientController>();
            EditorUtility.SetDirty(gurney);
            Debug.Log("[HUDSetup] PatientController agregado a medical_gurney");
        }

        // 2. GameTimer + MessageController en _GameManagers
        var managers = GameObject.Find("_GameManagers");
        if (managers != null)
        {
            if (managers.GetComponent<GameTimer>() == null)
                managers.AddComponent<GameTimer>();
            if (managers.GetComponent<MessageController>() == null)
                managers.AddComponent<MessageController>();
            EditorUtility.SetDirty(managers);
            Debug.Log("[HUDSetup] GameTimer + MessageController en _GameManagers");
        }

        // 3. Panel_HUD en Canvas_GameFlow
        var canvasGO = GameObject.Find("Canvas_GameFlow");
        if (canvasGO == null) { Debug.LogError("[HUDSetup] Canvas_GameFlow no encontrado"); return; }

        var oldHUD = canvasGO.transform.Find("Panel_HUD");
        if (oldHUD != null) Object.DestroyImmediate(oldHUD.gameObject);

        var colGreen  = new Color(0f, 1f, 0.25f, 1f);
        var colOrange = new Color(1f, 0.6f, 0f, 1f);
        var colRed    = new Color(0.7f, 0f, 0f, 1f);
        var colDark   = new Color(0.06f, 0.10f, 0.13f, 0.95f);

        var hudGO = MakeGO("Panel_HUD", canvasGO.transform, 0f, 0f, 1f, 1f);
        hudGO.AddComponent<Image>().color = Color.clear;
        hudGO.SetActive(false);
        var hudCtrl = hudGO.AddComponent<HealthBarController>();

        // Barra de salud (arriba izquierda)
        var healthBarGO = MakeGO("HealthBar_BG", hudGO.transform, 0.01f, 0.90f, 0.28f, 0.99f);
        healthBarGO.AddComponent<Image>().color = colDark;

        var fillBGGO = MakeGO("Fill_BG", healthBarGO.transform, 0.01f, 0.08f, 0.99f, 0.50f);
        fillBGGO.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);

        var fillGO = MakeGO("Fill", fillBGGO.transform, 0f, 0f, 1f, 1f);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = colGreen;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        hudCtrl.healthFill = fillImg;

        hudCtrl.healthPercentText = MakeTMP("Txt_Health",  healthBarGO.transform, "PHYS: 100%", 26, colGreen,  0.02f, 0.52f, 0.70f, 0.95f);
        hudCtrl.shockIndexText    = MakeTMP("Txt_Shock",   healthBarGO.transform, "SI: --",     22, colOrange, 0.70f, 0.52f, 0.99f, 0.95f);
        hudCtrl.hypoxiaAlertText  = MakeTMP("Txt_Hypoxia", healthBarGO.transform, "HIPOXIA!",   22, colRed,    0.02f, 0.02f, 0.99f, 0.50f);

        // Timer (arriba derecha)
        var timerGO = MakeGO("TimerBox", hudGO.transform, 0.72f, 0.90f, 0.99f, 0.99f);
        timerGO.AddComponent<Image>().color = colDark;
        var timerTMP = MakeTMP("Txt_Timer", timerGO.transform, "02:00", 42, colGreen, 0.05f, 0.05f, 0.95f, 0.95f, TextAlignmentOptions.Center);

        var gt = managers != null ? managers.GetComponent<GameTimer>() : null;
        if (gt != null) { gt.timerUGUIText = timerTMP; EditorUtility.SetDirty(gt); }

        // Mensajes de feedback (parte inferior)
        var msgGO = MakeGO("MsgBox", hudGO.transform, 0.20f, 0.01f, 0.80f, 0.11f);
        msgGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        var msgTMP = MakeTMP("Txt_Mensaje", msgGO.transform, "", 30, Color.white, 0.03f, 0.05f, 0.97f, 0.95f, TextAlignmentOptions.Center);

        var mc = managers != null ? managers.GetComponent<MessageController>() : null;
        if (mc != null) { mc.messageUGUIText = msgTMP; EditorUtility.SetDirty(mc); }

        // 4. Asignar simulationHUD al GameFlowController
        var flow = canvasGO.GetComponent<GameFlowController>();
        if (flow != null) { flow.simulationHUD = hudGO; EditorUtility.SetDirty(flow); }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HUDSetup] Todo listo: PatientController + GameTimer + MessageController + Panel_HUD");
    }

    static GameObject MakeGO(string name, Transform parent, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text, int size, Color col,
        float x0, float y0, float x1, float y1, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = MakeGO(name, parent, x0, y0, x1, y1);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        return t;
    }
}
