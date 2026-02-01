using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class MaskController : MonoBehaviour
{
    public static MaskController Instance;
    public event Action<int> OnMaskChanged;

    [System.Serializable]
    public struct MaskSetup
    {
        public string name;             // Editörde karışmasın diye isim (örn: "Kirmizi Maske")
        public GameObject maskObj;      // Açıp kapatacağımız ana obje (örn: "Mask1")
        public SpriteRenderer renderer; // İçindeki Sprite Renderer (örn: "MaskSpriteRed")
        
        [Header("Sprites")]
        public Sprite frontSprite;      // Aşağı bakarken
        public Sprite backSprite;       // Yukarı bakarken
        public Sprite sideSprite;       // Yana bakarken
    }

    [Header("Dependencies")]
    public Animator characterAnimator; 

    [Header("Mask Configurations")]
    public List<MaskSetup> availableMasks; // Maskeleri buraya ekle

    [Header("Visual Settings")]
    public int sortingOrderFront = 1;
    public int sortingOrderBack = -1;

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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Başlangıçta tüm maske objelerini kapat
        foreach (var mask in availableMasks)
        {
            if(mask.maskObj != null) mask.maskObj.SetActive(false);
        }

        currentMaskIndex = -1;
        ApplyMaskLogic();
    }

    void Update()
    {
        // --- KLAVYE GİRİŞLERİ (1-5) ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchMask(-1); // Maskesiz
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchMask(0);  // Liste elemanı 0
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchMask(1);  // Liste elemanı 1
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchMask(2);  // Liste elemanı 2
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchMask(3);  // Liste elemanı 3
    }

    void LateUpdate()
    {
        // Eğer maske takılı değilse görsel güncelleme yapma
        if (currentMaskIndex == -1 || currentMaskIndex >= availableMasks.Count) 
            return;

        UpdateMaskVisualDirection();
    }

    void SwitchMask(int targetIndex)
    {
        // Eğer geçerli bir index ise (veya -1 ise) değiştir
        if (targetIndex == -1 || targetIndex < availableMasks.Count)
        {
            currentMaskIndex = targetIndex;
            ApplyMaskLogic();
        }
    }

    void ApplyMaskLogic()
    {
        int visibilityMask = (1 << baseWorldLayer) | (1 << groundLayer);
        int currentActiveLayerID = -1; 

        // 1. MASKE OBJELERİNİ AÇ/KAPAT (SetActive Yönetimi)
        for (int i = 0; i < availableMasks.Count; i++)
        {
            if (availableMasks[i].maskObj != null)
            {
                // Sadece seçilen maskeyi aktif et, diğerlerini kapat
                bool isActive = (i == currentMaskIndex);
                availableMasks[i].maskObj.SetActive(isActive);
            }
        }

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
        }
        else
        {
            // --- STATE: WEARING A MASK ---
            int activeLayer = maskLayers[currentMaskIndex];
            currentActiveLayerID = activeLayer;

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

        OnMaskChanged?.Invoke(currentActiveLayerID);
    }

    void UpdateMaskVisualDirection()
    {
        if (characterAnimator == null) return;

        float inputX = characterAnimator.GetFloat("InputX");
        float inputY = characterAnimator.GetFloat("InputY");

        // Şu anki aktif maske ayarlarını al
        MaskSetup currentMask = availableMasks[currentMaskIndex];
        SpriteRenderer activeRenderer = currentMask.renderer;

        if (activeRenderer == null) return;

        // --- YÖN VE KATMAN MANTIĞI ---
        
        // 1. ARKAYA BAKMA (Yukarı)
        if (inputY > 0.1f && Mathf.Abs(inputY) > Mathf.Abs(inputX))
        {
            activeRenderer.sprite = currentMask.backSprite;
            activeRenderer.sortingOrder = sortingOrderBack; // Karakterin arkasına
            activeRenderer.flipX = false;
        }
        // 2. ÖNE BAKMA (Aşağı)
        else if (inputY < -0.1f && Mathf.Abs(inputY) > Mathf.Abs(inputX))
        {
            activeRenderer.sprite = currentMask.frontSprite;
            activeRenderer.sortingOrder = sortingOrderFront; // Karakterin önüne
            activeRenderer.flipX = false;
        }
        // 3. YANA BAKMA
        else
        {
            activeRenderer.sprite = currentMask.sideSprite;
            activeRenderer.sortingOrder = sortingOrderFront; // Karakterin önüne
            
            // Eğer maske objesi karakterin Scale'inden etkilenmiyorsa burayı aç:
            // activeRenderer.flipX = (inputX < 0);
        }
    }
}