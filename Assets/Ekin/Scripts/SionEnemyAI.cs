using UnityEngine;
using System.Collections;

public class SionEnemyAI : MonoBehaviour
{
    // Düşmanın Olası Durumları
    public enum EnemyState { Passive, Preparing, Chasing, Charging, Stunned }

    [Header("Temel Ayarlar")]
    public EnemyState currentState = EnemyState.Passive;
    public int myFactionLayer = 8; // Kırmızı Layer ID (Editörden kontrol et)
    public Transform playerTarget;

    [Header("Zamanlamalar")]
    public float aggressionDelay = 2.0f; // Maske takılınca ne kadar beklesin?
    public float stunDuration = 2.0f;    // Çarpınca kaç saniye sersemlesin?

    [Header("Dash (Sion R) Ayarları")]
    public float dashDuration = 4.0f; // Dash süresi
    public float dashMaxSpeed = 20f;  // Ulaşılacak maksimum hız

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
    private Coroutine aggressionCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Görsel bileşenleri (Sprite ve Animator) alt objelerde olabilir, onları bulalım
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

        // Grafik atanmadıysa varsayılan oluştur (Hata vermesin)
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

    // --- GÖRSEL GÜNCELLEMELER (UPDATE) ---
    void Update()
    {
        // Animasyon ve Yön çevirme işlemleri her karede yapılır
        UpdateAnimationState();
        HandleSpriteFlip();
    }

    // --- FİZİKSEL HAREKETLER (FIXED UPDATE) ---
    void FixedUpdate()
    {
        if (playerTarget == null) return;

        // Sadece normal kovalama (Chasing) durumunu burada yönetiyoruz.
        // Charging ve Stunned durumları Coroutine içinde yönetiliyor.
        if (currentState == EnemyState.Chasing)
        {
            HandleChasing();
        }
    }

    // --- MANTIK: MASKE DEĞİŞİMİ ---
    void HandleMaskChange(int activeLayerID)
    {
        StopAllCoroutines(); // Saldırı veya Stun varsa iptal et

        if (activeLayerID == myFactionLayer)
        {
            // Doğru maske takıldı, eğer pasifse geri sayımı başlat
            if (currentState == EnemyState.Passive)
            {
                aggressionCoroutine = StartCoroutine(StartAggressionCooldown());
            }
        }
        else
        {
            // Yanlış maske, hemen sakinleş
            currentState = EnemyState.Passive;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    IEnumerator StartAggressionCooldown()
    {
        currentState = EnemyState.Preparing;
        // Burada "! - Fark etti" efekti oynatılabilir
        yield return new WaitForSeconds(aggressionDelay);
        currentState = EnemyState.Chasing;
    }

    // --- MANTIK: NORMAL KOVALAMA ---
    void HandleChasing()
    {
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Menzile girdiyse Dash (Ulti) başlat
        if (distance <= attackTriggerDistance)
        {
            StartCoroutine(PerformDashAttack());
            return;
        }

        // Oyuncuya dön ve yürü
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0; // Yükseklik farkını yoksay

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        rb.MovePosition(transform.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
    }

    // --- MANTIK: SION R (DASH SALDIRISI) ---
    IEnumerator PerformDashAttack()
    {
        currentState = EnemyState.Charging;
        Debug.Log("DASH BAŞLADI: İvmeleniyor...");

        float timer = 0f;

        while (timer < dashDuration && currentState == EnemyState.Charging)
        {
            timer += Time.fixedDeltaTime;

            // 1. Hızı Grafikten Al
            float speedMultiplier = speedCurve.Evaluate(timer / dashDuration);
            float currentSpeed = speedMultiplier * dashMaxSpeed;

            // 2. İleri Git
            rb.MovePosition(transform.position + transform.forward * currentSpeed * Time.fixedDeltaTime);

            // 3. Kısıtlı Dönüş (Falso)
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, dashTurnRate * Time.fixedDeltaTime);
            }

            yield return new WaitForFixedUpdate();
        }

        // Çarpışmadan süre bittiyse normale dön
        if (currentState == EnemyState.Charging)
        {
            currentState = EnemyState.Chasing;
            rb.linearVelocity = Vector3.zero;
            Debug.Log("Dash bitti, tekrar kovalıyor.");
        }
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

        // Maske değişmediyse kaldığı yerden devam et
        if (currentState == EnemyState.Stunned)
        {
            currentState = EnemyState.Chasing;
            Debug.Log("Düşman kendine geldi.");
        }
    }

    // --- ANIMASYON YÖNETİMİ ---
    void UpdateAnimationState()
    {
        if (anim == null) return;

        int stateID = 0; // 0: Idle

        switch (currentState)
        {
            case EnemyState.Passive:
            case EnemyState.Preparing:
                stateID = 0; // Idle
                break;

            case EnemyState.Chasing:
                stateID = 1; // Walk (Kovalama)
                break;

            case EnemyState.Charging:
                stateID = 2; // Run / Dash
                break;

            case EnemyState.Stunned:
                stateID = 3; // Stun
                break;
        }

        anim.SetInteger("State", stateID);
    }

    void HandleSpriteFlip()
    {
        if (playerTarget == null || spriteRenderer == null) return;
        if (currentState == EnemyState.Stunned) return; // Stun yemişken dönmesin

        Vector3 direction = playerTarget.position - transform.position;

        // Sprite sağa bakıyorsa: direction.x < 0 ise Flip yap.
        if (direction.x > 0)
            spriteRenderer.flipX = false;
        else if (direction.x < 0)
            spriteRenderer.flipX = true;
    }

    // --- ÇARPIŞMA KONTROLLERİ ---
    void OnCollisionEnter(Collision collision)
    {
        // Sadece Dash atarken çarpışmaları kontrol et
        if (currentState == EnemyState.Charging)
        {
            // 1. Kutuya veya Kendi Rengine Çarparsa
            if (collision.gameObject.CompareTag("Destructible") || collision.gameObject.layer == myFactionLayer)
            {
                Debug.Log("GÜM! Obje parçalandı.");
                Destroy(collision.gameObject);

                StopAllCoroutines();
                StartCoroutine(ApplyStun()); // STUN YER
            }
            // 2. Oyuncuya Çarparsa
            else if (collision.gameObject.CompareTag("Player"))
            {
                Debug.Log("Oyuncuya vurdu!");
                // Hasar kodu buraya...

                StopAllCoroutines();
                currentState = EnemyState.Chasing; // Oyuncuya vurunca stun yemez, kovalar
            }
            // 3. Duvara Çarparsa (Yer hariç)
            else if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
            {
                Debug.Log("Duvara tosladı!");

                StopAllCoroutines();
                StartCoroutine(ApplyStun()); // STUN YER
            }
        }
    }
}