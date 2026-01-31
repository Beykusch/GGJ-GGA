using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System; // --- YENİ EKLENDİ (Action için gerekli) ---

public class MaskController : MonoBehaviour
{
    // --- YENİ EKLENDİ (Singleton: Düşmanların bu scripte ulaşması için) ---
    public static MaskController Instance;

    // --- YENİ EKLENDİ (Olay Sistemi: Maske değişince yayın yapacağız) ---
    // Bu olay bize "Aktif olan Layer ID'sini" verecek.
    public event Action<int> OnMaskChanged;

    [Header("Layer Setup")]
    public int playerLayer;
    public int baseWorldLayer;
    public int groundLayer;

    public List<int> maskLayers = new List<int>();

    [Header("UI Setup")]
    public Image screenFilter;
    public List<Color> maskColors;

    [Header("Debug")]
    public int currentMaskIndex = -1;

    void Awake() // --- YENİ EKLENDİ (Instance'ı tanımlamak için) ---
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentMaskIndex = -1;
        ApplyMaskLogic();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchToNextMask();
        }
    }

    void SwitchToNextMask()
    {
        currentMaskIndex++;
        if (currentMaskIndex >= maskLayers.Count)
        {
            currentMaskIndex = -1;
        }
        ApplyMaskLogic();
    }

    void ApplyMaskLogic()
    {
        int visibilityMask = (1 << baseWorldLayer) | (1 << groundLayer);
        int currentActiveLayerID = -1; // Varsayılan: Hiçbiri

        if (currentMaskIndex == -1)
        {
            // --- STATE: NO MASK ---
            Camera.main.cullingMask = visibilityMask;

            if (screenFilter != null)
                screenFilter.color = Color.clear;

            foreach (int layerID in maskLayers)
            {
                Physics.IgnoreLayerCollision(playerLayer, layerID, true);
            }
            // currentActiveLayerID zaten -1 kaldı.
        }
        else
        {
            // --- STATE: WEARING A MASK ---
            int activeLayer = maskLayers[currentMaskIndex];
            currentActiveLayerID = activeLayer; // Aktif layerı kaydet

            visibilityMask |= (1 << activeLayer);
            Camera.main.cullingMask = visibilityMask;

            if (screenFilter != null && currentMaskIndex < maskColors.Count)
                screenFilter.color = maskColors[currentMaskIndex];

            foreach (int layerID in maskLayers)
            {
                bool isTargetLayer = (layerID == activeLayer);
                Physics.IgnoreLayerCollision(playerLayer, layerID, !isTargetLayer);
            }
        }

        Physics.IgnoreLayerCollision(playerLayer, groundLayer, false);
        Physics.IgnoreLayerCollision(playerLayer, baseWorldLayer, false);

        // --- YENİ EKLENDİ (Duyuruyu Yapıyoruz) ---
        // Tüm düşmanlara "Hey, şu an aktif olan Layer ID budur!" diye bağırıyoruz.
        OnMaskChanged?.Invoke(currentActiveLayerID);
    }
}