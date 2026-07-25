using UnityEngine;

/// Construye una mano geométrica en runtime como visual del controlador VR.
[DisallowMultipleComponent]
public class HandVisualBuilder : MonoBehaviour
{
    public bool isLeftHand = true;

    [Header("Apariencia")]
    public Material handMaterial;
    public Color skinColor = new Color(0.88f, 0.74f, 0.62f);

    private Material _mat;
    private bool _runtimeMat;

    void Awake()
    {
        if (handMaterial != null)
        {
            _mat = handMaterial;
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Standard");
            _mat = new Material(shader);
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", skinColor);
            if (_mat.HasProperty("_Color"))     _mat.SetColor("_Color",     skinColor);
            _runtimeMat = true;
        }
        Build();
    }

    void OnDestroy()
    {
        if (_runtimeMat && _mat != null) Destroy(_mat);
    }

    void Build()
    {
        // s = 1 para mano izquierda (pulgar en +X local),  -1 para derecha
        float s = isLeftHand ? 1f : -1f;

        // ── Palma (caja plana) ───────────────────────────────────────────
        Box("Palm", new Vector3(0f, 0f, 0f), new Vector3(0.080f, 0.020f, 0.090f), Vector3.zero);

        // ── Nudillos (fila de protuberancias en el nacimiento de los dedos)
        Box("Knuckles", new Vector3(0f, 0.014f, -0.038f), new Vector3(0.076f, 0.014f, 0.018f), Vector3.zero);

        // ── Dedos (cápsula girada 90° en X → la cápsula apunta en -Z local)
        // index
        Cap("Index",  new Vector3( s * 0.023f, 0.012f, -0.075f), new Vector3(0.013f, 0.038f, 0.013f), new Vector3(90f, 0f, 0f));
        // medio
        Cap("Middle", new Vector3( s * 0.007f, 0.012f, -0.081f), new Vector3(0.014f, 0.043f, 0.014f), new Vector3(90f, 0f, 0f));
        // anular
        Cap("Ring",   new Vector3(-s * 0.008f, 0.012f, -0.075f), new Vector3(0.013f, 0.038f, 0.013f), new Vector3(90f, 0f, 0f));
        // meñique
        Cap("Pinky",  new Vector3(-s * 0.023f, 0.009f, -0.063f), new Vector3(0.010f, 0.030f, 0.010f), new Vector3(90f, 0f, 0f));

        // ── Pulgar (inclinado hacia el lado y ligeramente hacia adelante) ─
        Cap("Thumb",  new Vector3(s * 0.044f, 0.005f, -0.020f), new Vector3(0.015f, 0.038f, 0.015f), new Vector3(90f, 0f, s * -42f));

        // ── Muñeca (cilindro que conecta el controlador con la palma) ────
        Cap("Wrist",  new Vector3(0f, 0f, 0.028f), new Vector3(0.062f, 0.028f, 0.062f), Vector3.zero);
    }

    void Box(string n, Vector3 p, Vector3 sc, Vector3 r)   => Setup(GameObject.CreatePrimitive(PrimitiveType.Cube),    n, p, sc, r);
    void Cap(string n, Vector3 p, Vector3 sc, Vector3 r)   => Setup(GameObject.CreatePrimitive(PrimitiveType.Capsule), n, p, sc, r);

    void Setup(GameObject go, string partName, Vector3 localPos, Vector3 localScale, Vector3 localEuler)
    {
        go.name = partName;
        go.transform.SetParent(transform, false);
        go.transform.localPosition  = localPos;
        go.transform.localScale     = localScale;
        go.transform.localEulerAngles = localEuler;

        // Quitar colisores para no interferir con XR
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.GetComponent<MeshRenderer>().sharedMaterial = _mat;
    }
}
