using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public Transform visualObj;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Dash FX & UI")]
    public TrailRenderer dashTrail;
    public Image dashCooldownImage;

    [Header("Knockback & Recovery (HİBRİT SİSTEM)")]
    public float knockbackStunTime = 0.5f; // Tuşların kilitli kaldığı süre
    public float knockbackFriction = 5f;   // Savrulmanın azalma hızı (Fade out)
    public float recoveryDuration = 1.5f;  // Stun bittikten sonra hızın normale dönme süresi

    private Rigidbody rb;
    private Transform camTransform;

    private Vector3 moveVelocity;
    private Vector3 lastMoveDir;
    private Vector3 knockbackVelocity; // Savrulma gücü

    private bool isDashing = false;
    private bool canDash = true;
    private bool isInputLocked = false;

    private float defaultMoveSpeed; // Orijinal hızı hafızada tutmak için

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (Camera.main != null) camTransform = Camera.main.transform;

        if (dashTrail != null) dashTrail.emitting = false;
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 1;

        // Başlangıç hızını kaydet
        defaultMoveSpeed = moveSpeed;
    }

    void Update()
    {
        // 1. SİSTEM A: KNOCKBACK FADE OUT (Her karede savrulma azalır)
        if (knockbackVelocity.magnitude > 0.2f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackFriction * Time.deltaTime);
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        if (isDashing) return;

        // 2. INPUT KONTROLÜ
        if (isInputLocked)
        {
            // Stun yemişsek inputtan gelen hız SIFIRDIR.
            // Ama karakter knockbackVelocity sayesinde kaymaya devam eder.
            moveVelocity = Vector3.zero;
        }
        else
        {
            // --- NORMAL HAREKET ---
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            if (camTransform != null)
            {
                Vector3 camForward = camTransform.forward;
                Vector3 camRight = camTransform.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 targetDirection = (camForward * inputZ + camRight * inputX).normalized;

                if (targetDirection != Vector3.zero) lastMoveDir = targetDirection;

                // moveSpeed burada recovery süresince yavaş yavaş artacak
                moveVelocity = targetDirection * moveSpeed;

                // --- VISUAL FLIP ---
                if (visualObj != null)
                {
                    float sizeX = Mathf.Abs(visualObj.localScale.x);
                    if (inputX > 0) visualObj.localScale = new Vector3(sizeX, visualObj.localScale.y, visualObj.localScale.z);
                    else if (inputX < 0) visualObj.localScale = new Vector3(-sizeX, visualObj.localScale.y, visualObj.localScale.z);
                }
            }
        }

        // --- DASH ---
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isInputLocked)
        {
            StartCoroutine(DashRoutine());
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        // --- BÜYÜK BİRLEŞME ---
        // (Yavaş yavaş artan Yürüme Hızı) + (Yavaş yavaş azalan Savrulma Hızı)
        Vector3 finalVelocity = moveVelocity + knockbackVelocity;

        rb.linearVelocity = new Vector3(finalVelocity.x, rb.linearVelocity.y, finalVelocity.z);
    }

    public void GetKnockedBack(Vector3 direction, float force)
    {
        StopAllCoroutines(); // Eski recovery veya dash varsa iptal et

        // Değerleri sıfırla
        moveSpeed = defaultMoveSpeed;
        isDashing = false;

        // 1. Darbeyi ver
        knockbackVelocity = direction * force;

        // 2. Rutini başlat
        StartCoroutine(KnockbackAndRecoveryRoutine());
    }

    // --- HEPSİNİ YÖNETEN COROUTINE ---
    IEnumerator KnockbackAndRecoveryRoutine()
    {
        // AŞAMA 1: STUN (Tuşlar Kilitli)
        isInputLocked = true;

        // Bu süre boyunca knockbackVelocity Update'de azalmaya devam ediyor...
        yield return new WaitForSeconds(knockbackStunTime);

        // AŞAMA 2: RECOVERY (Tuşlar Açık ama Hız Düşük)
        isInputLocked = false;

        // Hızı %20'ye düşür
        float startSpeed = defaultMoveSpeed * 0.2f;
        moveSpeed = startSpeed;

        float timer = 0f;

        // Belirlenen süre boyunca hızı yavaş yavaş artır
        while (timer < recoveryDuration)
        {
            timer += Time.deltaTime;

            // Lerp ile hızı artır
            moveSpeed = Mathf.Lerp(startSpeed, defaultMoveSpeed, timer / recoveryDuration);

            yield return null;
        }

        // AŞAMA 3: NORMALE DÖNÜŞ
        moveSpeed = defaultMoveSpeed;
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;
        knockbackVelocity = Vector3.zero; // Dash atarsan savrulma iptal olsun (Opsiyonel)

        if (dashTrail != null) dashTrail.emitting = true;
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 0;

        Vector3 dashDir = lastMoveDir;
        if (dashDir == Vector3.zero) dashDir = visualObj.forward;

        rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, rb.linearVelocity.y, dashDir.z * dashSpeed);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        if (dashTrail != null) dashTrail.emitting = false;

        float timer = 0f;
        while (timer < dashCooldown)
        {
            timer += Time.deltaTime;
            if (dashCooldownImage != null)
                dashCooldownImage.fillAmount = timer / dashCooldown;
            yield return null;
        }

        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 1;
        canDash = true;
    }
}