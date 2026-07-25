using UnityEngine;

public class MayoStandController : MonoBehaviour
{
    void Start() => ApplySelection();

    public void ApplySelection()
    {
        var mayo = GameObject.Find("mayo-stand");
        if (mayo == null) { Debug.LogWarning("[MayoStand] mayo-stand not found"); return; }

        var tray = mayo.transform.Find("Tray");
        if (tray == null) { Debug.LogWarning("[MayoStand] Tray not found"); return; }

        var mgr = PatientCaseManager.Instance;

        for (int i = 0; i < tray.childCount; i++)
        {
            var child = tray.GetChild(i);
            var mt = child.GetComponent<MedicalTool>();
            if (mt == null) continue;

            // Use the toolTag.toolId instead of the GameObject name,
            // because MedicalTool.Awake() renames objects to toolTag.toolName.
            string toolId = mt.GetToolId();
            bool show = mgr != null && mgr.IsToolSelected(toolId);
            child.gameObject.SetActive(show);
            Debug.Log($"[MayoStand] {child.name} (id={toolId}) → {(show ? "VISIBLE" : "OCULTO")}");
        }
    }
}
