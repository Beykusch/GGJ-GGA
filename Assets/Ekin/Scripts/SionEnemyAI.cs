using UnityEngine;
using System.Collections;

public class SionAnimationControl : MonoBehaviour
{
    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb; // Eğer Rigidbody kullanıyorsan

    void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>(); // Görselin genelde child objededir
        rb = GetComponent<Rigidbody2D>();
    }

    [System.Obsolete]
    void Update()
    {
        // 1. Hız bilgisini al (Rigidbody veya NavMeshAgent'tan)
        // Unity 6'da linearVelocity yerine normal velocity de kullanılabilir ama garanti olsun:
        Vector2 velocity = rb.velocity;

        // 2. Hareket var mı kontrolü (0.1'den büyükse hareketli say)
        bool isMoving = velocity.magnitude > 0.1f;

        // 3. Animator parametrelerini güncelle
        anim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            // Hareket yönünü Animator'a gönder (-1 ile 1 arasında)
            anim.SetFloat("InputX", velocity.x);
            anim.SetFloat("InputY", velocity.y);

            // 4. Sprite Yönünü Çevirme (Flip)
            // Eğer sağa gidiyorsa düz, sola gidiyorsa ters çevir
            if (velocity.x > 0.1f)
            {
                sr.flipX = false; // Orijinal yön (Sağ)
            }
            else if (velocity.x < -0.1f)
            {
                sr.flipX = true; // Aynalanmış yön (Sol)
            }
        }
    }
}
public class SionEnemyAI : MonoBehaviour
{
    // Düşmanın Olası Durumları
    public enum EnemyState { Passive, Preparing, Chasing, Charging, Stunned, Dead }

    [Header("Temel Ayarlar")]
    public EnemyState currentState = EnemyState.Passive;
    public int myFactionLayer = 8; // Kırmızı Layer ID
    public Transform playerTarget;

    [Header("Zamanlamalar")]
    public float aggressionDelay = 2.0f; // Maske takılınca ne kadar beklesin?
    public float stunDuration = 2.0f;    // Oyuncuya çarpınca kaç sn sersemlesin?
    public float corpseDuration = 3.0f;  // Ölüsü ne kadar yerde kalsın?

    [Header("Dash (Sion R) Ayarları")]
    public float dashDuration = 4.0f; // Dash süresi
    public float dashMaxSpeed = 20f;  // Ulaşılacak maksimum hız

    [Header("Saldırı Gücü")]
    public float knockbackForce = 15f; // Oyuncuyu fırlatma gücü

    [Header("Hız Grafiği")]
    [Tooltip("Inspector'dan çizilecek grafik. Başta yavaş, ortada hızlı, sonda yavaş.")]
    public AnimationCurve speedCurve;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;      // Normal yürüme hızı
    public float turnSpeed = 200f;    // Dönüş hızı
    public float attackTriggerDistance = 8f; // Dash kaç metre kala başlasın?
    [Tooltip("Düşük değer = Zor döner (Araba gibi), Yüksek değer = Hemen döner")]
    public float dashTurnRate = 30f;

    // --- BİLEŞENLER ---
    private Rigidbody rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Collider myCollider;
    private Coroutine aggressionCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();

        // Görsel bileşenleri (Sprite ve Animator) alt objelerde olabilir
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Oyuncuyu otomatik bul
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

        // Maske değişim sistemine abone ol
        if (MaskController.Instance != null)
        {
            MaskController.Instance.OnMaskChanged += HandleMaskChange;
        }

        // Grafik atanmadıysa varsayılan oluştur
        if (speedCurve == null || speedCurve.length == 0)
        {
            speedCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
        }
    }

    void OnDestroy()
    {
        if (MaskController.Instance != null)
            MaskController.Instance.OnMaskChanged -= HandleMaskChange;
    }

    // --- GÖRSEL GÜNCELLEMELER ---
    void Update()
    {
        UpdateAnimationState();
        HandleSpriteFlip();
    }

    // --- FİZİKSEL HAREKETLER ---
    void FixedUpdate()
    {
        if (playerTarget == null) return;

        // Ölü, Pasif veya Stun yemişse hareket etme
        if (currentState == EnemyState.Dead || currentState == EnemyState.Passive || currentState == EnemyState.Stunned) return;

        if (currentState == EnemyState.Chasing)
        {
            HandleChasing();
        }
    }

    // --- MANTIK: MASKE DEĞİŞİMİ ---
    void HandleMaskChange(int activeLayerID)
    {
        if (currentState == EnemyState.Dead) return; // Ölüler maske dinlemez

        StopAllCoroutines(); // Saldırı veya Stun varsa iptal et

        if (activeLayerID == myFactionLayer)
        {
            if (currentState == EnemyState.Passive)
            {
                aggressionCoroutine = StartCoroutine(StartAggressionCooldown());
            }
            // Eğer Stun yemişse dokunmuyoruz, süresi bitince kendine gelir.
        }
        else
        {
            currentState = EnemyState.Passive;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    IEnumerator StartAggressionCooldown()
    {
        currentState = EnemyState.Preparing;
        yield return new WaitForSeconds(aggressionDelay);
        currentState = EnemyState.Chasing;
    }

    // --- MANTIK: NORMAL KOVALAMA ---
    void HandleChasing()
    {
        float distance = Vector3.Distance(transform.position, playerTarget.position);
        
        // Update Proximity
        if (MusicController._mc != null)
            MusicController._mc.SetProximity(distance);

        if (distance <= attackTriggerDistance)
        {
            if (MusicController._mc != null)
                MusicController._mc.SetActFight();
            
            StartCoroutine(PerformDashAttack());
            return;
        }

        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        rb.MovePosition(transform.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
    }

    // --- MANTIK: SION R (DASH SALDIRISI) ---
    IEnumerator PerformDashAttack()
    {
        //play attack sound
        AudioManager.I.PlayEnemyAttack(transform.position);

        currentState = EnemyState.Charging;

        // BUG FIX: Dash başlangıcında yüzünü oyuncuya döndür
        if (playerTarget != null)
        {
            Vector3 startDir = (playerTarget.position - transform.position).normalized;
            startDir.y = 0;
            if (startDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(startDir);
        }

        Debug.Log("DASH BAŞLADI: İvmeleniyor...");

        float timer = 0f;

        while (timer < dashDuration && currentState == EnemyState.Charging)
        {
            timer += Time.fixedDeltaTime;

            float speedMultiplier = speedCurve.Evaluate(timer / dashDuration);
            float currentSpeed = speedMultiplier * dashMaxSpeed;

            rb.MovePosition(transform.position + transform.forward * currentSpeed * Time.fixedDeltaTime);

            // Kısıtlı Dönüş (Falso)
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, dashTurnRate * Time.fixedDeltaTime);
            }

            yield return new WaitForFixedUpdate();
        }

        if (currentState == EnemyState.Charging)
        {
            currentState = EnemyState.Chasing;
            rb.linearVelocity = Vector3.zero;
        }
        
        if (MusicController._mc != null)
            MusicController._mc.ResetProximityFar();
    }

    // --- MANTIK: STUN (SERSEMLEME) ---
    IEnumerator ApplyStun()
    {
        currentState = EnemyState.Stunned;

        // Fiziği tamamen durdur
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("DÜŞMAN SERSEMLEDİ! 😵");

        yield return new WaitForSeconds(stunDuration);

        if (currentState == EnemyState.Stunned)
        {
            currentState = EnemyState.Chasing;
            Debug.Log("Düşman kendine geldi.");
        }
    }

    // --- MANTIK: ÖLÜM ---
    void Die()
    {
        currentState = EnemyState.Dead;

        StopAllCoroutines();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true; // Fizikten çıkar
        if (myCollider != null) myCollider.enabled = false; // Çarpışmayı kapat

        Debug.Log("DÜŞMAN ÖLDÜ!");
        StartCoroutine(DestroyCorpse());
    }

    IEnumerator DestroyCorpse()
    {
        yield return new WaitForSeconds(corpseDuration);
        Destroy(gameObject);
    }

    // --- ÇARPIŞMA KONTROLLERİ ---
    void OnCollisionEnter(Collision collision)
{
    if (currentState == EnemyState.Charging)
    {
        // SENARYO 1: OYUNCUYA ÇARPARSA -> KNOCKBACK + STUN
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerMovement playerScript = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerScript != null)
            {
                Vector3 knockbackDir = (collision.transform.position - transform.position).normalized;
                knockbackDir.y = 0.5f;
                playerScript.GetKnockedBack(knockbackDir, knockbackForce);
            }

            Debug.Log("Oyuncuya vurdu!");
            StopAllCoroutines();
            StartCoroutine(ApplyStun());
        }

        // SENARYO 2: KUTUYA ÇARPARSA -> GECİKMELİ ÖLÜM / STUN
        else if (collision.gameObject.CompareTag("Destructible") || collision.gameObject.layer == myFactionLayer)
        {
            Debug.Log("Kutuya çarptı! Patlama için bekleniyor...");

            // ÖNEMLİ DEĞİŞİKLİK BURADA:
            // Hemen Die() çağırma. Bir kare bekle ki Kutu scripti senin "Charging" olduğunu görebilsin.
            StartCoroutine(HandleCollisionDelay()); 
        }

        // SENARYO 3: DUVARA ÇARPARSA -> STUN
        else if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
        {
            Debug.Log("Duvara tosladı!");
            StopAllCoroutines();
            StartCoroutine(ApplyStun());
        }
    }
}

// Yeni Yardımcı Coroutine
IEnumerator HandleCollisionDelay()
{
    // Fizik hesaplamalarının bitmesi için bir kare bekle
    yield return null; 

    // ŞİMDİ ölebilir veya sersemleyebilirsin
    // Eğer patlamada zaten ölecekse Die() çağırmana gerek kalmayabilir (Barrel scripti yok edebilir).
    // Ama garanti olsun diye buraya ekliyoruz:
    Die(); 
    // Veya sersemlemesini istiyorsan: 
    // StartCoroutine(ApplyStun());
}

    // --- ANIMASYON YÖNETİMİ ---
    void UpdateAnimationState()
    {
        if (anim == null) return;

        int stateID = 0;

        switch (currentState)
        {
            case EnemyState.Passive:
            case EnemyState.Preparing:
                stateID = 0; // Idle
                break;
            case EnemyState.Chasing:
                stateID = 1; // Walk
                break;
            case EnemyState.Charging:
                stateID = 2; // Run
                break;
            case EnemyState.Stunned:
                stateID = 3; // Stun
                break;
            case EnemyState.Dead:
                stateID = 4; // Death
                break;
        }

        anim.SetInteger("State", stateID);
    }

    void HandleSpriteFlip()
    {
        if (playerTarget == null || spriteRenderer == null) return;
        // Ölü veya Stun yemişse dönemesin
        if (currentState == EnemyState.Dead || currentState == EnemyState.Stunned) return;

        Vector3 direction = playerTarget.position - transform.position;

        if (direction.x > 0)
            spriteRenderer.flipX = false;
        else if (direction.x < 0)
            spriteRenderer.flipX = true;
    }
}