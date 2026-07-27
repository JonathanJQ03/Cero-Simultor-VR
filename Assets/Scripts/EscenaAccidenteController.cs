using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class EscenaAccidenteController : MonoBehaviour
{
    void Awake()
    {
        if (PatientCaseManager.Instance == null)
            new GameObject("PatientCaseManager").AddComponent<PatientCaseManager>();
        else
            PatientCaseManager.Instance.GenerateNewCase();

        SetupVideo();
    }

    void SetupVideo()
    {
        // RenderTexture que recibe el frame del video
        var rt = new RenderTexture(1920, 1080, 0);
        rt.Create();

        // Canvas a máxima prioridad
        var canvasGO = new GameObject("VideoCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.Expand;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fondo negro para bordes si el video no cubre todo
        var bgGO  = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        StretchFull(bgGO.GetComponent<RectTransform>());

        // RawImage donde se muestra el video
        var rawGO  = new GameObject("VideoDisplay");
        rawGO.transform.SetParent(canvasGO.transform, false);
        var raw    = rawGO.AddComponent<RawImage>();
        raw.texture = rt;
        StretchFull(rawGO.GetComponent<RectTransform>());

        // VideoPlayer
        var vpGO = new GameObject("VP");
        var vp   = vpGO.AddComponent<VideoPlayer>();
        vp.playOnAwake   = false;
        vp.renderMode    = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.aspectRatio   = VideoAspectRatio.FitInside;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        var src = vpGO.AddComponent<AudioSource>();
        vp.SetTargetAudioSource(0, src);

#if UNITY_EDITOR
        vp.url = System.IO.Path.Combine(Application.dataPath, "Screenshots/EscenaAccidente.mp4");
#else
        // En build coloca el video en StreamingAssets
        vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, "EscenaAccidente.mp4");
#endif

        vp.prepareCompleted += OnPrepared;
        vp.loopPointReached += OnVideoFinished;
        vp.Prepare();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void OnPrepared(VideoPlayer vp) => vp.Play();

    void OnVideoFinished(VideoPlayer vp) => SceneManager.LoadScene("SeleccionDificultad");
}
