using UnityEngine;

public class MaskManager : MonoBehaviour
{
    // Define the IDs for your layers (matches the numbers in Edit Layers)
    const int LAYER_PLAYER = 3; 
    const int LAYER_RED = 6;
    const int LAYER_BLUE = 7;
    const int LAYER_GREEN = 8;
    const int LAYER_YELLOW = 9;

    void EquipRedMask()
    {
        // 1. VISUALS: Show Red, Hide others (Using Camera Culling)
        // This bitwise operation says: Show Default (1) + Red Layer.
        Camera.main.cullingMask = (1 << 0) | (1 << LAYER_RED); 
        
        // 2. PHYSICS: Enable collision with Red, Disable others
        // false = DO NOT IGNORE (aka: Collide!)
        Physics2D.IgnoreLayerCollision(LAYER_PLAYER, LAYER_RED, false); 
        
        // true = IGNORE (aka: Pass through!)
        Physics2D.IgnoreLayerCollision(LAYER_PLAYER, LAYER_BLUE, true);
        
        // Add post-processing color filter code here...
    }
}
