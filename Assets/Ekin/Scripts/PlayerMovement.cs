using UnityEngine;
using UnityEngine.UI; // Required for UI
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
    public TrailRenderer dashTrail; // Drag your Trail Renderer here
    public Image dashCooldownImage; // Drag your "DashFill" UI Image here

    private Rigidbody rb;
    private Transform camTransform;
    
    private Vector3 moveVelocity;
    private Vector3 lastMoveDir;
    
    private bool isDashing = false;
    private bool canDash = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (Camera.main != null) camTransform = Camera.main.transform;

        // Ensure trail is off at start
        if (dashTrail != null) dashTrail.emitting = false;
        
        // Ensure UI is full (ready to dash)
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 1;
    }

    void Update()
    {
        if (isDashing) return;

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
        if (isDashing) return;
        // Apply normal movement
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;

        // 1. START VISUALS
        if (dashTrail != null) dashTrail.emitting = true;
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 0; // Empty the UI icon

        // 2. APPLY VELOCITY
        Vector3 dashDir = lastMoveDir;
        if (dashDir == Vector3.zero) dashDir = visualObj.forward;
        
        rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, rb.linearVelocity.y, dashDir.z * dashSpeed);

        // 3. WAIT FOR DASH DURATION
        yield return new WaitForSeconds(dashDuration);

        // 4. STOP DASHING & TRAIL
        isDashing = false;
        if (dashTrail != null) dashTrail.emitting = false;

        // 5. HANDLE COOLDOWN (Animation)
        float timer = 0f;
        while (timer < dashCooldown)
        {
            timer += Time.deltaTime;
            // Update UI smoothly
            if (dashCooldownImage != null)
            {
                dashCooldownImage.fillAmount = timer / dashCooldown;
            }
            yield return null; // Wait for next frame
        }

        // 6. COOLDOWN FINISHED
        if (dashCooldownImage != null) dashCooldownImage.fillAmount = 1;
        canDash = true;
    }
}