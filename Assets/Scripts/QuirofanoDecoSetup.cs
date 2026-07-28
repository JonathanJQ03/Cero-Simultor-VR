using UnityEngine;

/// Decoración estática del quirófano: baldosas, techo, máquina de anestesia,
/// taburetes quirúrgicos y corrección de luces de la lámpara.
[DefaultExecutionOrder(-50)]
public class QuirofanoDecoSetup : MonoBehaviour
{
    void Start()
    {
        SetupFloorTiles();
        CreateCeiling();
        CreateAnesthesiaMachine();
        CreateSurgicalStools();
        FixLampLights();
    }

    // ── 1. PISO CON BALDOSAS ─────────────────────────────────────────────────
void SetupFloorTiles()
    {
        var floorGO = GameObject.Find("Floor");
        if (floorGO == null) { Debug.LogWarning("[DecoSetup] Floor no encontrado"); return; }
        var rend = floorGO.GetComponent<MeshRenderer>();
        if (rend == null) return;
        var mat = MakeUnlitTileMat("FloorTileMat",
            new Color(0.82f, 0.84f, 0.84f),
            new Color(0.32f, 0.34f, 0.35f), 3,
            new Vector2(20f, 20f));
        rend.sharedMaterial = mat;
    }

    // ── 2. TECHO ─────────────────────────────────────────────────────────────
void CreateCeiling()
    {
        // Destruye el Plane anterior (tiene aristas de triángulo visibles) y usa Quad
        var old = GameObject.Find("_Ceiling");
        if (old != null) Destroy(old);

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "_Ceiling";
        Destroy(go.GetComponent<Collider>());
        // Quad por defecto es XY vertical — rotar 90° en X lo pone horizontal con normal hacia abajo
        go.transform.position   = new Vector3(-1.01f, 3.0f, -1.06f);
        go.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(10.2f, 8.2f, 1f); // cubre la sala entera

        go.GetComponent<MeshRenderer>().sharedMaterial = MakeUnlitTileMat("CeilTileMat",
            new Color(0.91f, 0.92f, 0.92f),
            new Color(0.50f, 0.51f, 0.52f), 3,
            new Vector2(17f, 13f));
    }

    // ── 3. MÁQUINA DE ANESTESIA ───────────────────────────────────────────────
    void CreateAnesthesiaMachine()
    {
        if (GameObject.Find("_AnesthesiaMachine") != null) return;

        var root = new GameObject("_AnesthesiaMachine");
        // Cabecera del paciente, lado izquierdo
        root.transform.position = new Vector3(-3.3f, 0f, -2.7f);

        var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Cuerpo principal
        Box(root, "Body",       P(0f, 0.62f, 0f),      S(0.55f,1.24f,0.45f), Hex(0x3A3D42), lit);
        // Panel frontal oscuro
        Box(root, "FrontPanel", P(0f, 0.85f,-0.228f),  S(0.52f,0.74f,0.015f),Hex(0x272A2F), lit);
        // Pantalla monitor
        Box(root, "Screen",     P(0f, 1.03f,-0.233f),  S(0.34f,0.22f,0.008f),Hex(0x051218), lit);
        Box(root, "ScreenGlow", P(0f, 1.03f,-0.236f),  S(0.30f,0.18f,0.005f),new Color(0f,0.65f,0.58f), lit);
        // Superficie superior
        Box(root, "TopTray",    P(0f, 1.26f,-0.04f),   S(0.56f,0.025f,0.50f),Hex(0x4A4D52), lit);
        // Vaporizadores
        Box(root, "Vap1",       P( 0.19f,0.54f,-0.228f),S(0.10f,0.32f,0.06f),Hex(0x1A5BAE), lit); // azul
        Box(root, "Vap2",       P( 0.06f,0.54f,-0.228f),S(0.10f,0.32f,0.06f),Hex(0xD4A017), lit); // amarillo
        // Cilindro O2
        Cyl(root,  "GasCylO2",  P(-0.33f,0.52f, 0.06f), S(0.08f,0.55f,0.08f),Hex(0x2D7A3A), lit);
        Box(root,  "CylValve",  P(-0.33f,1.10f, 0.06f), S(0.06f,0.06f,0.06f),Hex(0x555760), lit);
        // Brazo bolsa de respiración
        Box(root,  "BagArm",    P( 0.35f,0.22f,-0.14f), S(0.24f,0.022f,0.022f),Hex(0x606265),lit);
        // Bolsa de respiración (esfera)
        var bag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bag.name = "BreathingBag"; Destroy(bag.GetComponent<Collider>());
        bag.transform.SetParent(root.transform, false);
        bag.transform.localPosition = P(0.48f, 0.22f, -0.14f);
        bag.transform.localScale    = S(0.14f, 0.11f, 0.10f);
        SetMat(bag, new Color(0.68f, 0.84f, 0.90f), lit);
        // Base y ruedas
        Box(root, "Base", P(0f,0.025f,0f), S(0.60f,0.05f,0.50f), Hex(0x1E2025), lit);
        Wheel(root, P( 0.26f,0.055f, 0.21f), lit);
        Wheel(root, P(-0.26f,0.055f, 0.21f), lit);
        Wheel(root, P( 0.26f,0.055f,-0.21f), lit);
        Wheel(root, P(-0.26f,0.055f,-0.21f), lit);
    }

    void Wheel(GameObject parent, Vector3 localPos, Shader shader)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        w.name = "Wheel"; Destroy(w.GetComponent<Collider>());
        w.transform.SetParent(parent.transform, false);
        w.transform.localPosition = localPos;
        w.transform.localScale    = S(0.065f, 0.05f, 0.065f);
        SetMat(w, Hex(0x101215), shader);
    }

    // ── 4. TABURETES QUIRÚRGICOS ──────────────────────────────────────────────
    void CreateSurgicalStools()
    {
        // Lado derecho del paciente (cirujano)
        if (GameObject.Find("_Stool_0") == null) MakeStool("_Stool_0", new Vector3( 2.1f, 0f,-0.7f));
        if (GameObject.Find("_Stool_1") == null) MakeStool("_Stool_1", new Vector3( 2.1f, 0f, 0.6f));
        // Lado izquierdo (enfermero/instrumentista)
        if (GameObject.Find("_Stool_2") == null) MakeStool("_Stool_2", new Vector3(-3.9f, 0f, 0.5f));
    }

    void MakeStool(string n, Vector3 pos)
    {
        var root = new GameObject(n);
        root.transform.position = pos;
        var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Asiento
        Cyl(root, "Seat",     P(0f,0.67f,0f), S(0.30f,0.045f,0.30f), Hex(0x141418), lit);
        Cyl(root, "SeatPad",  P(0f,0.692f,0f),S(0.27f,0.025f,0.27f), Hex(0x1C1C21), lit);
        // Poste neumático
        Cyl(root, "Pneum",    P(0f,0.50f,0f), S(0.050f,0.28f,0.050f),Hex(0xC2C4C8), lit);
        Cyl(root, "Post",     P(0f,0.26f,0f), S(0.032f,0.52f,0.032f),Hex(0xA8AAAE), lit);
        // Placa base central
        Cyl(root, "Hub",      P(0f,0.025f,0f),S(0.10f,0.04f, 0.10f), Hex(0x888A8E), lit);

        // Estrella de 5 brazos
        for (int i = 0; i < 5; i++)
        {
            float ang = i * 72f * Mathf.Deg2Rad;
            float ax  = Mathf.Sin(ang) * 0.195f;
            float az  = Mathf.Cos(ang) * 0.195f;
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm" + i; Destroy(arm.GetComponent<Collider>());
            arm.transform.SetParent(root.transform, false);
            arm.transform.localPosition = new Vector3(ax * 0.5f, 0.022f, az * 0.5f);
            arm.transform.localScale    = new Vector3(0.022f, 0.016f, 0.40f);
            arm.transform.localRotation = Quaternion.Euler(0f, -i * 72f, 0f);
            SetMat(arm, Hex(0x888A8E), lit);
            // Ruedecilla
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.name = "Caster" + i; Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(root.transform, false);
            c.transform.localPosition = new Vector3(ax, 0.03f, az);
            c.transform.localScale    = S(0.042f, 0.038f, 0.042f);
            SetMat(c, Hex(0x0E1013), lit);
        }
    }

    // ── 5. LUCES DE LA LÁMPARA QUIRÚRGICA ────────────────────────────────────
    void FixLampLights()
    {
        var lamp = GameObject.Find("lamp");
        if (lamp == null) return;

        var lights = lamp.GetComponentsInChildren<Light>();
        foreach (var l in lights)
        {
            // Apuntar directo hacia abajo (world -Y)
            l.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            l.type      = LightType.Spot;
            l.spotAngle = 55f;
            l.range     = 5.0f;
            l.intensity = 10f;
            l.color     = new Color(1.0f, 0.97f, 0.92f); // blanco quirúrgico cálido
        }
    }

    // ── Generador de textura de baldosas ─────────────────────────────────────
Material MakeUnlitTileMat(string matName, Color tile, Color grout, int g, Vector2 tiling)
    {
        int w = 128, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
        tex.name = matName + "Tex";
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = (x < g || y < g) ? grout : tile;
        tex.SetPixels(pixels);
        tex.wrapMode   = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        var sh  = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        var mat = new Material(sh);
        mat.name = matName;
        mat.SetTexture("_BaseMap", tex);
        // _BaseMap_ST: XY = tiling, ZW = offset (método más confiable en URP)
        mat.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
        mat.SetColor("_BaseColor", Color.white);
        return mat;
    }

    static Texture2D MakeTileTex(int w, int h, Color tile, Color grout, int g)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = (x < g || y < g) ? grout : tile;
        tex.SetPixels(pixels);
        return tex;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void Box(GameObject p, string n, Vector3 pos, Vector3 sc, Color col, Shader sh)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n; Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(p.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = sc;
        SetMat(go, col, sh);
    }

    void Cyl(GameObject p, string n, Vector3 pos, Vector3 sc, Color col, Shader sh)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = n; Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(p.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = sc;
        SetMat(go, col, sh);
    }

    void SetMat(GameObject go, Color col, Shader sh)
    {
        if (sh == null) return;
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     col);
        go.GetComponent<MeshRenderer>().material = mat;
    }

    static Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
    static Vector3 S(float x, float y, float z) => new Vector3(x, y, z);
    static Color Hex(int rgb) => new Color(
        ((rgb >> 16) & 0xFF) / 255f,
        ((rgb >>  8) & 0xFF) / 255f,
        ( rgb        & 0xFF) / 255f);
}
