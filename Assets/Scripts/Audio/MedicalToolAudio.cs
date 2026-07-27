using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(XRGrabInteractable))]
public class MedicalToolAudio : MonoBehaviour
{
    [Range(0f, 1f)] public float grabVolume    = 0.7f;
    [Range(0f, 1f)] public float releaseVolume = 0.4f;

    AudioSource          _src;
    XRGrabInteractable   _grab;
    AudioClip            _grabClip;
    AudioClip            _releaseClip;

    const int SR = 44100;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.spatialBlend = 1f;
        _src.playOnAwake  = false;
        _src.loop         = false;
        _src.maxDistance  = 3f;

        _grabClip    = GenerateGrabClick();
        _releaseClip = GenerateReleaseClick();
    }

    void Start()
    {
        _grab = GetComponent<XRGrabInteractable>();
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(_ => _src.PlayOneShot(_grabClip,    grabVolume));
            _grab.selectExited.AddListener( _ => _src.PlayOneShot(_releaseClip, releaseVolume));
        }
    }

    void OnDestroy()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveAllListeners();
            _grab.selectExited.RemoveAllListeners();
        }
    }

    // Click corto + cuerpo de pick-up (plástico/metal)
    static AudioClip GenerateGrabClick()
    {
        float dur = 0.09f;
        int   n   = Mathf.RoundToInt(SR * dur);
        float[] d = new float[n];
        var rng   = new System.Random(42);

        for (int i = 0; i < n; i++)
        {
            float t   = i / (float)n;
            // Transiente inicial + cuerpo de ruido filtrado
            float env = Mathf.Exp(-t * 30f) * 0.9f + Mathf.Exp(-t * 8f) * 0.3f;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            // Añadir tono de "plástico" ~1200 Hz
            float tone  = Mathf.Sin(2f * Mathf.PI * 1200f * (i / (float)SR)) * 0.3f;
            d[i] = (noise * 0.7f + tone) * env;
        }
        var c = AudioClip.Create("Tool_Grab", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // Soltar: más suave y corto
    static AudioClip GenerateReleaseClick()
    {
        float dur = 0.05f;
        int   n   = Mathf.RoundToInt(SR * dur);
        float[] d = new float[n];
        var rng   = new System.Random(7);

        for (int i = 0; i < n; i++)
        {
            float t   = i / (float)n;
            float env = Mathf.Exp(-t * 40f) * 0.5f;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            d[i] = noise * env;
        }
        var c = AudioClip.Create("Tool_Release", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
