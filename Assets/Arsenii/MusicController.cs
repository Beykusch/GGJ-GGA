using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicController : MonoBehaviour
{
    public static MusicController _mc;

    [Header("FMOD Music Event")]
    [SerializeField] private EventReference musicEvent;

    [Header("Params")]
    [SerializeField] private string actParam = "ActChange";
    [SerializeField] private string proximityParam = "Proximity";

    [Header("Proximity smoothing")]
    [SerializeField] private float proximitySmoothSpeed = 4f;

    [SerializeField] private int redMaskLayerId;

    private EventInstance inst;
    private float currentProximity = 1f;
    private bool started;

    private void OnMaskChanged(int activeLayer)
    {
        Debug.Log($"[MusicController] OnMaskChanged: activeLayer={activeLayer}, redMaskLayerId={redMaskLayerId}");
        if (activeLayer == redMaskLayerId)
            SetActRedMaskOn();
        else
            SetActBasic();
    }

    void Awake()
    {
        Debug.Log("MusicController Awake");

        if (_mc != null)
        {
            Destroy(gameObject);
            return;
        }

        _mc = this;
        DontDestroyOnLoad(gameObject);

        inst = RuntimeManager.CreateInstance(musicEvent);
        inst.start();
        started = true;

        inst.setParameterByNameWithLabel(actParam, "Basic");
        inst.setParameterByName(proximityParam, 100f);
    }

    void Start()
    {
        Debug.Log("[MusicController] Start called");

        if (MaskController.Instance != null)
        {
            MaskController.Instance.OnMaskChanged += OnMaskChanged;
            Debug.Log("[MusicController] Subscribed to OnMaskChanged");
        }
        else
        {
            Debug.LogError("[MusicController] MaskController.Instance is NULL in Start!");
        }
    }

    void OnDestroy()
    {
        if (MaskController.Instance != null)
        {
            MaskController.Instance.OnMaskChanged -= OnMaskChanged;
        }

        if (!started) return;

        inst.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        inst.release();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            Debug.Log("P pressed, MusicController Update works");
    }

    // ===== ACT API =====

    public void SetActBasic()
        => inst.setParameterByNameWithLabel(actParam, "Basic");

    public void SetActRedMaskOn()
        => inst.setParameterByNameWithLabel(actParam, "Red Mask On");

    public void SetActFight()
        => inst.setParameterByNameWithLabel(actParam, "Fighting");

    // ===== PROXIMITY API =====
    // 100 (far) -> 1 (close)

    public void SetProximity(float targetProximity)
    {
        // Fixed: Clamp signature is (value, min, max), not (value, max, min)
        targetProximity = Mathf.Clamp(targetProximity, 1f, 100f);

        float k = 1f - Mathf.Exp(-proximitySmoothSpeed * Time.deltaTime);
        currentProximity = Mathf.Lerp(currentProximity, targetProximity, k);

        inst.setParameterByName(proximityParam, currentProximity);
        
        // Для отладки
        Debug.LogWarning($"[MusicController] SetProximity: {currentProximity:F2}");
    }

    public void ResetProximityFar()
    {
        currentProximity = 100f;
        inst.setParameterByName(proximityParam, 100f);
    }
}
