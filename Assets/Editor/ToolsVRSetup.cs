using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class ToolsVRSetup
{
    struct ToolDef
    {
        public string goPath;   // path in Hierarchy
        public string toolId;
        public string toolName;
        public ToolCategory category;
    }

    static readonly ToolDef[] Tools = new ToolDef[]
    {
        new ToolDef { goPath = "mayo-stand/Tray/Bisturi",        toolId = "Bisturi",        toolName = "Bisturi",              category = ToolCategory.Tratamiento },
        new ToolDef { goPath = "mayo-stand/Tray/TijerasTrauma",  toolId = "TijerasDeTrauma",toolName = "Tijeras de Trauma",     category = ToolCategory.Tratamiento },
        new ToolDef { goPath = "mayo-stand/Tray/Vendas",         toolId = "VendasHemo",     toolName = "Vendas Hemostaticas",   category = ToolCategory.Tratamiento },
        new ToolDef { goPath = "mayo-stand/Tray/Torniquete",     toolId = "Torniquete",     toolName = "Torniquete",            category = ToolCategory.Tratamiento },
        new ToolDef { goPath = "mayo-stand/Tray/Jeringa",        toolId = "Epinefrina",     toolName = "Epinefrina",            category = ToolCategory.Tratamiento },
        new ToolDef { goPath = "mayo-stand/Tray/Desfibrilador",  toolId = "Desfibrilador",  toolName = "Desfibrilador",         category = ToolCategory.Tratamiento },
        new ToolDef { goPath = "mayo-stand/Tray/Laringoscopio",  toolId = "Laringoscopio",  toolName = "Laringoscopio",         category = ToolCategory.Diagnostico },
        new ToolDef { goPath = "mayo-stand/Tray/Canula",         toolId = "CanulaDeGuedel", toolName = "Canula de Guedel",      category = ToolCategory.Preparacion },
        new ToolDef { goPath = "mayo-stand/Tray/Estetoscopio",   toolId = "Estetoscopio",   toolName = "Estetoscopio",          category = ToolCategory.Diagnostico },
    };

    [MenuItem("Tools/Setup VR Tools (mayo-stand)")]
    public static void SetupVRTools()
    {
        // 1. Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/ToolTags"))
            AssetDatabase.CreateFolder("Assets", "ToolTags");

        int done = 0;

        foreach (var def in Tools)
        {
            // 2. Find the GameObject
            var go = GameObject.Find(def.goPath);
            if (go == null)
            {
                Debug.LogWarning($"[ToolsVRSetup] No encontrado: {def.goPath}");
                continue;
            }

            // 3. Create or load the ScriptableObject
            string assetPath = $"Assets/ToolTags/Tag_{def.toolId}.asset";
            var tag = AssetDatabase.LoadAssetAtPath<MedicalToolTag>(assetPath);
            if (tag == null)
            {
                tag = ScriptableObject.CreateInstance<MedicalToolTag>();
                AssetDatabase.CreateAsset(tag, assetPath);
            }
            tag.toolId   = def.toolId;
            tag.toolName = def.toolName;
            tag.category = def.category;
            EditorUtility.SetDirty(tag);

            // 4. Add Rigidbody (XRGrabInteractable also adds one, but we set it first)
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.useGravity  = true;
            rb.isKinematic = false;

            // 5. Add a Box Collider if none exists anywhere on the object
            if (go.GetComponentInChildren<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                // Try to auto-size from child renderers
                var renderers = go.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    col.center = go.transform.InverseTransformPoint(b.center);
                    col.size   = go.transform.InverseTransformVector(b.size);
                    // Clamp minimum size so it's always grabbable
                    var s = col.size;
                    s.x = Mathf.Max(s.x, 0.03f);
                    s.y = Mathf.Max(s.y, 0.03f);
                    s.z = Mathf.Max(s.z, 0.03f);
                    col.size = s;
                }
                else
                {
                    col.size = new Vector3(0.05f, 0.05f, 0.15f);
                }
            }

            // 6. Add XRGrabInteractable
            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null) grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;

            // 7. Add MedicalTool and assign tag
            var tool = go.GetComponent<MedicalTool>();
            if (tool == null) tool = go.AddComponent<MedicalTool>();
            tool.toolTag = tag;
            EditorUtility.SetDirty(go);

            done++;
            Debug.Log($"[ToolsVRSetup] Configurado: {go.name} → toolId={def.toolId}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[ToolsVRSetup] Listo. {done}/{Tools.Length} herramientas configuradas.");
    }
}
