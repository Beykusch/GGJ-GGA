using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform camTransform;

    void Start()
    {
        camTransform = Camera.main.transform;
    }
    // Unreal'daki "Event Tick"
    void LateUpdate()
    {
        // Kameranın rotasyonunu al, ama sadece Y ekseninde dönmesin istiyorsan
        // (DST'de genelde karakter kameraya tam paralel bakar)
        transform.rotation = Camera.main.transform.rotation;

        // Eğer karakterin yere "basıyor" gibi değil de havada uçuyor gibi duruyorsa
        // Sadece görsel objeye (Child object) bu scripti at.
    }
}