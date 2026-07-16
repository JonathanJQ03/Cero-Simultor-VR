using UnityEngine;

public enum ToolCategory
{
    Tratamiento,
    Diagnostico,
    Preparacion
}

[CreateAssetMenu(fileName = "NewMedicalTool", menuName = "Pulso Cero VR/Medical Tool Tag")]
public class MedicalToolTag : ScriptableObject
{
    public string toolId;
    public string toolName;
    public ToolCategory category;
    [TextArea] public string description;
    public Sprite icon;

    public static class IDs
    {
        public const string TORNIQUETE = "Torniquete";
        public const string EPINEFRINA = "Epinefrina";
        public const string VENDAS_HEMO = "VendasHemo";
        public const string GASAS = "Gasas";
        public const string DESFIBRILADOR = "Desfibrilador";
        public const string LARINGOSCOPIO = "Laringoscopio";
        public const string MONITOR_PORTATIL = "MonitorPortatil";
        public const string ESTETOSCOPIO = "Estetoscopio";
        public const string CANULA_GUEDEL = "CanulaDeGuedel";
        public const string TIJERAS_TRAUMA = "TijerasDeTrauma";
        public const string BISTURI = "Bisturi";
    }
}
