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

    [Header("Knockback Settings (YENİ)")]
    public float knockbackStunTime = 0.5f; // Havada kontrolsüz kalma süresi
    public float recoveryDuration = 1.0f;  // Yere indikten sonra hızlanma süresi

    private Rigidbody rb;
    private Transform camTransform;

    private Vector3 moveVelocity;
    private Vector3 lastMoveDir;

    private bool isDashing = false;
    private bool canDash = true;
    private bool isKnockedBack = false;

    // Orijinal hızı hafızada tutmak için
    private float defaultMoveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (Camera.main != null) camTransform = Camera.main.transform;

        if (dashTrail != null) dashTrail.emitting = false;
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 1;

        // Başlangıç hızını kaydet (Ayılınca bu hıza döneceğiz)
        defaultMoveSpeed = moveSpeed;
    }

    void Update()
    {
        if (isDashing || isKnockedBack) return;

        // --- INPUTS ---
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        if (camTransform == null) return;

        // --- CALCULATE DIRECTION ---
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 targetDirection = (camForward * inputZ + camRight * inputX).normalized;

        if (targetDirection != Vector3.zero) lastMoveDir = targetDirection;

        moveVelocity = targetDirection * moveSpeed;

        // --- VISUAL FLIP ---
        if (visualObj != null)
        {
            float sizeX = Mathf.Abs(visualObj.localScale.x);
            if (inputX > 0) visualObj.localScale = new Vector3(sizeX, visualObj.localScale.y, visualObj.localScale.z);
            else if (inputX < 0) visualObj.localScale = new Vector3(-sizeX, visualObj.localScale.y, visualObj.localScale.z);
        }

        // --- DASH ---
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(DashRoutine());
        }
    }

    void FixedUpdate()
    {
        if (isDashing || isKnockedBack) return;

        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }

    public void GetKnockedBack(Vector3 direction, float force)
    {
        // Eğer zaten havadaysak tekrar vurulunca bug olmasın, mevcut coroutine'i durdur
        StopAllCoroutines();

        // Hızı ve Inputu sıfırla
        isKnockedBack = true;
        moveSpeed = defaultMoveSpeed; // Hız bozulmuşsa düzelt

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction * force, ForceMode.Impulse);

        StartCoroutine(RecoverFromKnockback());
    }

    // --- BU KISIM GÜNCELLENDİ: YAVAŞ TOPARLANMA ---
    IEnumerator RecoverFromKnockback()
    {
        // 1. AŞAMA: TAM KİLİT (Havada uçma evresi)
        yield return new WaitForSeconds(knockbackStunTime);

        // Input kilidini aç
        isKnockedBack = false;

        // 2. AŞAMA: YAVAŞ ÇEKİM (Ayılma evresi)
        // Hızı çok düşür (Örn: Normalin %20'si)
        float startSpeed = defaultMoveSpeed * 0.2f;
        moveSpeed = startSpeed;

        float timer = 0f;

        // Belirlenen süre boyunca hızı yavaş yavaş artır (Lerp)
        while (timer < recoveryDuration)
        {
            timer += Time.deltaTime;

            // Hızı zamanla %20'den %100'e çek
            moveSpeed = Mathf.Lerp(startSpeed, defaultMoveSpeed, timer / recoveryDuration);

            yield return null; // Bir sonraki kareyi bekle
        }

        // 3. AŞAMA: NORMALE DÖNÜŞ
        moveSpeed = defaultMoveSpeed;
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;

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