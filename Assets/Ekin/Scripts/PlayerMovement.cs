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
            // Sağa gidiyorsak
            if (inputX > 0)
                visualObj.localScale = new Vector3(1, 1, 1);
            // Sola gidiyorsak (X'i -1 yaparak aynalarız)
            else if (inputX < 0)
                visualObj.localScale = new Vector3(-1, 1, 1);
        }
    }

    void FixedUpdate()
    {
        // 4. FİZİKSEL HAREKETİ UYGULA
        // Mevcut Y hızını koru (yerçekimi bozulmasın), X ve Z'yi değiştir.
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }
}