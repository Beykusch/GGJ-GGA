using UnityEngine;
using UnityEngine.UI; // Required for the UI Overlay
using System.Collections.Generic;

public class MaskController : MonoBehaviour
{
    [Header("Layer Setup")]
    public int playerLayer;      // e.g. 3
    public int baseWorldLayer;   // e.g. 0 (Default)
    public int groundLayer;      // e.g. 6 (The floor)
    
    // The list of colored world layers (Red, Blue, etc.)
    public List<int> maskLayers = new List<int>();

    [Header("UI Setup")]
    public Image screenFilter;   // Drag your full-screen UI Image here
    public List<Color> maskColors; // Define colors here (Alpha ~50)

    [Header("Debug")]
    // -1 means "No Mask / Default World"
    // 0 means "First Mask in list", 1 means "Second Mask", etc.
    public int currentMaskIndex = -1; 

    void Start()
    {
        // FORCE the index to -1 (No Mask) explicitly
        currentMaskIndex = -1;
        // Start in the Default World (No Mask)
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
        // Increment index
        currentMaskIndex++;

        // If we reach the end of the list, reset to -1 (Default World)
        if (currentMaskIndex >= maskLayers.Count)
        {
            currentMaskIndex = -1;
        }

        ApplyMaskLogic();
    }

    void ApplyMaskLogic()
    {
        // 1. BASE VISIBILITY
        // Always show Base World + Ground
        int visibilityMask = (1 << baseWorldLayer) | (1 << groundLayer);

        // 2. CHECK IF WE ARE IN "DEFAULT WORLD" OR "MASKED WORLD"
        if (currentMaskIndex == -1)
        {
            // --- STATE: NO MASK ---
            
            // Camera: Only show Base + Ground (already set in visibilityMask)
            Camera.main.cullingMask = visibilityMask;

            // UI: clear/invisible color
            if (screenFilter != null) 
                screenFilter.color = Color.clear;

            // Physics: Disable collision with ALL colored worlds
            foreach (int layerID in maskLayers)
            {
                Physics.IgnoreLayerCollision(playerLayer, layerID, true);
            }
        }
        else
        {
            // --- STATE: WEARING A MASK ---
            
            int activeLayer = maskLayers[currentMaskIndex];

            // Camera: Add the active mask layer to the visibility
            visibilityMask |= (1 << activeLayer);
            Camera.main.cullingMask = visibilityMask;

            // UI: Set the color matching this mask
            if (screenFilter != null && currentMaskIndex < maskColors.Count)
                screenFilter.color = maskColors[currentMaskIndex];

            // Physics: Loop through all mask layers
            foreach (int layerID in maskLayers)
            {
                // If it's the active layer -> ENABLE Collision (Ignore = false)
                // If it's any other color -> DISABLE Collision (Ignore = true)
                bool isTargetLayer = (layerID == activeLayer);
                Physics.IgnoreLayerCollision(playerLayer, layerID, !isTargetLayer);
            }
        }

        // Always ensure Player collides with Ground & Base
        Physics.IgnoreLayerCollision(playerLayer, groundLayer, false);
        Physics.IgnoreLayerCollision(playerLayer, baseWorldLayer, false);
    }
}