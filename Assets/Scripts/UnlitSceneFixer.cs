using UnityEngine;

/// Cambia todos los materiales de la escena a Unlit al iniciar,
/// para que las texturas se vean sin afectarse por la iluminación.
public class UnlitSceneFixer : MonoBehaviour
{
    // Start en lugar de Awake para que TMP haya inicializado sus renderers primero
    void Start()
    {
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
        {
            Debug.LogWarning("[UnlitSceneFixer] Shader URP/Unlit no encontrado.");
            return;
        }

        int count = 0;
        var renderers = (Renderer[])GameObject.FindObjectsOfType(typeof(Renderer));

        foreach (var rend in renderers)
        {
            // Saltar cualquier renderer que sea parte de TextMeshPro o TextMesh
            if (rend.GetComponent<TMPro.TMP_Text>() != null) continue;
            if (rend.GetComponent<TextMesh>() != null) continue;

            var mats = rend.materials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;
                if (mat.shader == unlitShader) continue;

                var shaderName = mat.shader != null ? mat.shader.name : "";
                // Proteger shaders de TMP, UI y cualquier Distance Field
                if (shaderName.Contains("TextMeshPro") ||
                    shaderName.Contains("Distance Field") ||
                    shaderName.Contains("UI/") ||
                    shaderName.Contains("Sprite")) continue;

                Texture tex = null;
                Color col = Color.white;
                if (mat.HasProperty("_BaseMap"))  tex = mat.GetTexture("_BaseMap");
                if (tex == null && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");

                mat.shader = unlitShader;
                if (tex != null) mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", col);
                changed = true;
                count++;
            }
            if (changed) rend.materials = mats;
        }

        Debug.Log($"[UnlitSceneFixer] {count} materiales cambiados a Unlit.");
    }
}
