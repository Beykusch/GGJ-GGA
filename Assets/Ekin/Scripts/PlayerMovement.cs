using UnityEngine;
using System.Collections; // Required for Coroutines

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public Transform visualObj;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;      // How fast you dash
    public float dashDuration = 0.2f;  // How long the dash lasts (short is snappy)
    public float dashCooldown = 1f;    // Time before you can dash again

    private Rigidbody rb;
    private Transform camTransform;    // Optimization: Cached Camera
    
    // Movement Variables
    private Vector3 movementInput;
    private Vector3 moveVelocity;
    private Vector3 lastMoveDir;       // Stores the last direction we moved (for dashing while standing still)
    
    // State Variables
    private bool isDashing = false;
    private bool canDash = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Cache Camera for performance
        if (Camera.main != null) camTransform = Camera.main.transform;
    }

    void Update()
    {
        // 1. IF DASHING, IGNORE INPUTS
        if (isDashing) return;

        // 2. MOVEMENT INPUTS
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        // Safety check for camera
        if (camTransform == null) return;

        // 3. CALCULATE DIRECTION
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 targetDirection = (camForward * inputZ + camRight * inputX).normalized;

        // Save direction for dashing (if we are moving)
        if (targetDirection != Vector3.zero)
        {
            lastMoveDir = targetDirection;
        }

        moveVelocity = targetDirection * moveSpeed;

        // 4. VISUAL FLIP
        if (visualObj != null)
        {
            float sizeX = Mathf.Abs(visualObj.localScale.x);
            float sizeY = visualObj.localScale.y;
            float sizeZ = visualObj.localScale.z;

            if (inputX > 0)
                visualObj.localScale = new Vector3(sizeX, sizeY, sizeZ);
            else if (inputX < 0)
                visualObj.localScale = new Vector3(-sizeX, sizeY, sizeZ);
        }

        // 5. DASH INPUT (Left Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }
    }

    void FixedUpdate()
    {
        // If dashing, we don't apply normal movement code here
        // The Dash coroutine handles the velocity
        if (isDashing) return;

        // Apply Normal Movement
        // Note: keeping your 'linearVelocity' syntax for Unity 6
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }

    // --- DASH LOGIC ---
    IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;

        // determine dash direction
        // If we are moving, dash that way. If standing still, dash in the direction we last faced.
        Vector3 dashDir = lastMoveDir;
        
        // Fallback: If game just started and we haven't moved yet, dash forward
        if (dashDir == Vector3.zero) dashDir = visualObj.forward; 

        // Apply instant dash velocity
        // We preserve Y velocity so gravity still works (you don't fly if you dash off a cliff)
        rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, rb.linearVelocity.y, dashDir.z * dashSpeed);

        // Wait for the dash to finish
        yield return new WaitForSeconds(dashDuration);

        isDashing = false;

        // Wait for cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}