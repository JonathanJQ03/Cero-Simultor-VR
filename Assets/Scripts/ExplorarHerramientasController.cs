using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ExplorarHerramientasController : MonoBehaviour
{
    public TextMeshProUGUI nombreHerramienta;
    public TextMeshProUGUI descripcionHerramienta;
    public Image iconoImagen;

    private string[] nombres = new string[] {
        "Torniquete", "Epinefrina", "Vendas",
        "Desfibrilador", "Laringoscopio", "Monitor Portatil",
        "Estetoscopio", "Canula de Guedel", "Tijeras de Trauma"
    };

    private string[] descripciones = new string[] {
        "Controla hemorragias externas en extremidades",
        "Reanima en paro cardiaco — ultimo recurso",
        "Controla hemorragias en tronco y cabeza",
        "Corrige arritmias letales — requiere dos manos",
        "Intubacion endotraqueal para asegurar via aerea",
        "Ver signos vitales detallados en tiempo real",
        "Auscultacion cardiaca y pulmonar",
        "Via aerea basica temporal",
        "Cortar ropa para exponer heridas"
    };

    private string[] btnNombres = new string[] {
        "Btn_Torniquete", "Btn_Epinefrina", "Btn_Vendas",
        "Btn_Desfibrilador", "Btn_Laringoscopio", "Btn_MonitorPortatil",
        "Btn_Estetoscopio", "Btn_CanuladeGuedel", "Btn_TijerasdeTrauma"
    };

    private Sprite[] iconos;

    void Awake()
    {
        iconos = new Sprite[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            var btn = GameObject.Find(btnNombres[i]);
            if (btn != null)
            {
                var icon = btn.transform.Find("Icon");
                if (icon != null)
                {
                    var img = icon.GetComponent<Image>();
                    if (img != null) iconos[i] = img.sprite;
                }
            }
        }
    }

    void Start()
    {
        var buttons = FindObjectsOfType<Button>();
        foreach (var btn in buttons)
        {
            if (btn.name.StartsWith("Btn_"))
            {
                string cleanName = btn.name.Substring(4).Replace(" ", "");
                for (int i = 0; i < nombres.Length; i++)
                {
                    if (nombres[i].Replace(" ", "") == cleanName)
                    {
                        int idx = i;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => MostrarInformacion(idx));
                        break;
                    }
                }
            }
        }
    }

    public void MostrarInformacion(int index)
    {
        if (index >= 0 && index < nombres.Length)
        {
            nombreHerramienta.text = nombres[index];
            descripcionHerramienta.text = descripciones[index];
            if (iconoImagen != null && index < iconos.Length && iconos[index] != null)
            {
                iconoImagen.sprite = iconos[index];
                iconoImagen.color = Color.white;
            }
        }
    }
}
