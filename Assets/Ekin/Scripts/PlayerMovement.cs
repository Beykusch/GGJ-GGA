using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public Transform visualObj;
    public Animator anim; // BURAYA ANIMATOR'I SÜRÜKLE

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Dash FX & UI")]
    public TrailRenderer dashTrail;
    public Image dashCooldownImage;

    [Header("Knockback & Recovery (HYBRID SYSTEM)")]
    public float knockbackStunTime = 0.5f;
    public float knockbackFriction = 5f;
    public float recoveryDuration = 1.5f;

    [Header("Sound")] 
    [SerializeField] private float footstepsTimer = 0f;
    [SerializeField] private float footstepsInterval = 0.4f; // interval between footsteps sounds
    
    private bool isMoving = false;
    

    private Rigidbody rb;
    private Transform camTransform;

    private Vector3 moveVelocity;
    private Vector3 lastMoveDir;
    private Vector3 knockbackVelocity;

    private bool isDashing = false;
    private bool canDash = true;
    private bool isInputLocked = false;

    private float defaultMoveSpeed;
    
    // Animasyon Hafızası için
    private float lastInputX;
    private float lastInputY;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (Camera.main != null) camTransform = Camera.main.transform;
        
        // Animator otomatik bulunsun
        if (anim == null && visualObj != null) anim = visualObj.GetComponent<Animator>();

        if (dashTrail != null) dashTrail.emitting = false;
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 1;

        defaultMoveSpeed = moveSpeed;
    }

    void Update()
    {
        // 1. SİSTEM: KNOCKBACK FADE OUT
        if (knockbackVelocity.magnitude > 0.2f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackFriction * Time.deltaTime);
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        if (isDashing) return;

        // 2. INPUTLARI OKU
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        // 3. ANIMASYON KONTROLÜ (Input Lock durumuna göre)
        UpdateAnimations(inputX, inputZ);

        // 4. HAREKET MANTIĞI
        if (isInputLocked)
        {
            moveVelocity = Vector3.zero; // Stun yemişse kendi iradesiyle hareket edemez
        }
        else
        {
            if (camTransform != null)
            {
                
                // Kamera açısına göre yön belirleme
                Vector3 camForward = camTransform.forward;
                Vector3 camRight = camTransform.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 targetDirection = (camForward * inputZ + camRight * inputX).normalized;

                if (targetDirection != Vector3.zero) 
                {
                    lastMoveDir = targetDirection;
                }

                moveVelocity = targetDirection * moveSpeed;

                // --- VISUAL FLIP ---
                // Sadece input varken ve kilitli değilken dön
                if (visualObj != null && inputX != 0)
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
            // Dash atarken de yön verisi gönderiyoruz
            StartCoroutine(DashRoutine(inputX, inputZ));
        }
        
        // -- Footstep sound logic -- 
        if (isMoving)
        {
            footstepsTimer += Time.deltaTime;
            if (footstepsTimer >= footstepsInterval)
            {
                Debug.Log("Footstep sound triggered");
                AudioManager.I.PlayFootstep(transform.position);
                footstepsTimer = 0f;
            }
        }
        else
        {
            footstepsTimer = footstepsInterval;
        }
    }

    void UpdateAnimations(float x, float y)
    {
        if (anim == null) return;

        // Hareket ediyor mu? (Input var mı VE Stun durumu yok mu?)
        bool hasInput = new Vector2(x, y).sqrMagnitude > 0.1f;
        isMoving = hasInput && !isInputLocked;

        anim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            // Hareket varken hafızayı güncelle
            lastInputX = Mathf.Abs(x); // Flip yaptığımız için X hep pozitif
            lastInputY = y;
        }

        // Animatör'e her zaman HAFIZADAKİ son yönü gönderiyoruz.
        // Böylece durduğunda veya stun yediğinde (0,0) değil, son baktığı yön gider.
        anim.SetFloat("InputX", lastInputX);
        anim.SetFloat("InputY", lastInputY);
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        // BÜYÜK BİRLEŞME: İrade Hızı + Savrulma Hızı
        Vector3 finalVelocity = moveVelocity + knockbackVelocity;

        rb.linearVelocity = new Vector3(finalVelocity.x, rb.linearVelocity.y, finalVelocity.z);
    }

    public void GetKnockedBack(Vector3 direction, float force)
    {
        StopAllCoroutines(); 

        moveSpeed = defaultMoveSpeed;
        isDashing = false;
        
        // Dash animasyonunu kapat
        if (anim != null) anim.SetBool("IsDashing", false);

        knockbackVelocity = direction * force;

        StartCoroutine(KnockbackAndRecoveryRoutine());
    }

    IEnumerator KnockbackAndRecoveryRoutine()
    {
        // AŞAMA 1: STUN
        isInputLocked = true;
        // Not: UpdateAnimations fonksiyonu isInputLocked true olduğu için
        // IsMoving'i false yapacak ve karakter Idle animasyonuna (son baktığı yönde) geçecek.
        
        yield return new WaitForSeconds(knockbackStunTime);

        // AŞAMA 2: RECOVERY
        isInputLocked = false;
        float startSpeed = defaultMoveSpeed * 0.2f;
        moveSpeed = startSpeed;

        float timer = 0f;
        while (timer < recoveryDuration)
        {
            timer += Time.deltaTime;
            moveSpeed = Mathf.Lerp(startSpeed, defaultMoveSpeed, timer / recoveryDuration);
            yield return null;
        }

        // AŞAMA 3: NORMAL
        moveSpeed = defaultMoveSpeed;
    }

    IEnumerator DashRoutine(float inX, float inY)
    {
        isDashing = true;
        canDash = false;
        knockbackVelocity = Vector3.zero;
        
        //--Dash sound--
        AudioManager.I.DashShot(transform.position);

        // 1. ANIMASYON AYARLARI (4 YÖNLÜ SNAP)
        if (anim != null)
        {
            anim.SetBool("IsDashing", true);
            
            float dashAnimX = 0;
            float dashAnimY = 0;

            if (Mathf.Abs(inY) > Mathf.Abs(inX)) // Dikey hareket baskın
            {
                dashAnimX = 0;
                dashAnimY = inY > 0 ? 1 : -1;
            }
            else // Yatay hareket baskın (veya duruyor ama dash attı)
            {
                dashAnimX = 1; 
                dashAnimY = 0;
            }
            // Dash yönünü anlık olarak overwrite ediyoruz
            anim.SetFloat("InputX", dashAnimX);
            anim.SetFloat("InputY", dashAnimY);
        }

        // 2. GÖRSEL VE FİZİK
        if (dashTrail != null) dashTrail.emitting = true;
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 0;

        Vector3 dashDir = lastMoveDir;
        if (dashDir == Vector3.zero) dashDir = visualObj.forward;

        rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, rb.linearVelocity.y, dashDir.z * dashSpeed);

        yield return new WaitForSeconds(dashDuration);

        // 3. BİTİŞ
        isDashing = false;
        if (anim != null) anim.SetBool("IsDashing", false);
        
        if (dashTrail != null) dashTrail.emitting = false;

        float timer = 0f;
        while (timer < dashCooldown)
        {
            timer += Time.deltaTime;
            if (dashCooldownImage != null) dashCooldownImage.fillAmount = timer / dashCooldown;
            yield return null;
        }

        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 1;
        canDash = true;
    }
}