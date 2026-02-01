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

    private EventInstance inst;
    private float currentProximity = 100f;
    private bool started;

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

        // Safe defaults
        inst.setParameterByNameWithLabel(actParam, "Basic");
        inst.setParameterByName(proximityParam, 100f);
    }

    void OnDestroy()
    {
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
        => inst.setParameterByNameWithLabel(actParam, "RedMaskON");

    public void SetActFight()
        => inst.setParameterByNameWithLabel(actParam, "Fighting");

    // ===== PROXIMITY API =====
    // 100 (far) -> 1 (close)

    public void SetProximity(float targetProximity)
    {
        // Исправлен порядок аргументов: min=1, max=100
        targetProximity = Mathf.Clamp(targetProximity, 1f, 100f);

        float k = 1f - Mathf.Exp(-proximitySmoothSpeed * Time.deltaTime);
        currentProximity = Mathf.Lerp(currentProximity, targetProximity, k);

        inst.setParameterByName(proximityParam, currentProximity);
        
        // Для отладки
        Debug.Log($"[MusicController] SetProximity: {currentProximity:F2}");
    }

    public void ResetProximityFar()
    {
        currentProximity = 100f;
        inst.setParameterByName(proximityParam, 100f);
    }
}
