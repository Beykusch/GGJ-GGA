using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ayarlar")]
    public float moveSpeed = 6f; // Hız
    public Transform visualObj;  // Görseli çevirmek için referans

    private Rigidbody rb;
    private Vector3 movementInput;
    private Vector3 moveVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 1. GİRDİLERİ AL (WASD)
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        // 2. KAMERAYA GÖRE YÖN HESAPLA (İzometrik Mantık)
        // Kameranın ileri ve sağ vektörlerini al
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        // Y eksenini (yukarı/aşağı) iptal et ki karakter yere paralel gitsin
        camForward.y = 0;
        camRight.y = 0;

        // Vektör boylarını 1'e eşitle (Normalize)
        camForward.Normalize();
        camRight.Normalize();

        // Girdi yönünü oluştur
        Vector3 targetDirection = (camForward * inputZ + camRight * inputX).normalized;

        // Hızı hesapla
        moveVelocity = targetDirection * moveSpeed;

        // 3. GÖRSELİ DÖNDÜR (FLIP)
        if (visualObj != null)
        {
            // First, get the current absolute size (always positive)
            // This ensures we respect whatever size you set in the Inspector (e.g., 0.2)
            float sizeX = Mathf.Abs(visualObj.localScale.x);
            float sizeY = visualObj.localScale.y;
            float sizeZ = visualObj.localScale.z;

            // Sağa gidiyorsak (Moving Right)
            if (inputX > 0)
            {
                visualObj.localScale = new Vector3(sizeX, sizeY, sizeZ);
            }
            // Sola gidiyorsak (Moving Left)
            else if (inputX < 0)
            {
                // We use negative sizeX to flip it
                visualObj.localScale = new Vector3(-sizeX, sizeY, sizeZ);
            }
        }
    }

    void FixedUpdate()
    {
        // 4. FİZİKSEL HAREKETİ UYGULA
        // Mevcut Y hızını koru (yerçekimi bozulmasın), X ve Z'yi değiştir.
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }
}