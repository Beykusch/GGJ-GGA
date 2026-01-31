using UnityEngine;
using System.Collections;

public class SionEnemyAI : MonoBehaviour
{
    public enum EnemyState { Passive, Preparing, Chasing, Charging }

    [Header("Basic Settings")]
    public EnemyState currentState = EnemyState.Passive;
    public int myFactionLayer = 8; // Layer ID for Red/Blue etc.
    public Transform playerTarget;

    [Header("Timings")]
    public float aggressionDelay = 2.0f; // Delay before attacking when mask is equipped

    [Header("Dash (Sion R) Settings")]
    public float dashDuration = 4.0f; // Total duration of the dash
    public float dashMaxSpeed = 20f;  // Maximum speed to reach

    [Header("Speed Curve (Very Important)")]
    [Tooltip("Draw this curve in the Inspector like a bell curve/trapezoid. Low at start, high in middle, low at end.")]
    public AnimationCurve speedCurve; // Controls acceleration and deceleration

    [Header("Movement Settings")]
    public float moveSpeed = 3f;      // Normal chasing speed
    public float turnSpeed = 200f;    // Normal turning speed
    public float attackTriggerDistance = 8f; // Distance to trigger dash
    [Tooltip("Low value = Low Steer (Straight line), High value = High Steer (Homing missile)")]
    public float dashTurnRate = 30f;

    private Rigidbody rb;
    private Coroutine aggressionCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

        // Listen for mask changes
        if (MaskController.Instance != null)
        {
            MaskController.Instance.OnMaskChanged += HandleMaskChange;
        }

        // Create a default curve if none is assigned to prevent errors
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

    // Handles logic when the player changes masks
    void HandleMaskChange(int activeLayerID)
    {
        StopAllCoroutines();

        if (activeLayerID == myFactionLayer)
        {
            // Correct mask, start the aggression countdown
            if (currentState == EnemyState.Passive)
            {
                aggressionCoroutine = StartCoroutine(StartAggressionCooldown());
            }
        }
        else
        {
            // Wrong mask, calm down
            currentState = EnemyState.Passive;
            rb.linearVelocity = Vector3.zero; // Stop movement
        }
    }

    // Cooldown before becoming aggressive
    IEnumerator StartAggressionCooldown()
    {
        currentState = EnemyState.Preparing;
        // Optional: Play alert sound or effect here
        yield return new WaitForSeconds(aggressionDelay);
        currentState = EnemyState.Chasing;
    }

    void FixedUpdate()
    {
        if (playerTarget == null) return;

        switch (currentState)
        {
            case EnemyState.Chasing:
                HandleChasing();
                break;
                // Charging state is handled within the Coroutine
        }
    }

    // 1. Normal Chasing Logic
    void HandleChasing()
    {
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Within attack range?
        if (distance <= attackTriggerDistance)
        {
            StartCoroutine(PerformDashAttack());
            return;
        }

        // Move and Rotate towards player normally
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0; // Ignore height difference

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

        rb.MovePosition(transform.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
    }

    // 2. Dash Attack Logic (Sion R)
    IEnumerator PerformDashAttack()
    {
        currentState = EnemyState.Charging;
        Debug.Log("DASH STARTED: Accelerating...");

        float timer = 0f;

        while (timer < dashDuration && currentState == EnemyState.Charging)
        {
            timer += Time.fixedDeltaTime;

            // 1. Get Speed Multiplier from the Curve (Value between 0 and 1)
            float speedMultiplier = speedCurve.Evaluate(timer / dashDuration);

            // 2. Calculate Current Speed
            float currentSpeed = speedMultiplier * dashMaxSpeed;

            // 3. MOVE FORWARD (With calculated speed)
            rb.MovePosition(transform.position + transform.forward * currentSpeed * Time.fixedDeltaTime);

            // 4. STEERING (Limited turning ability)
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                // Rotate slowly towards player to allow dodging
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, dashTurnRate * Time.fixedDeltaTime);
            }

            yield return new WaitForFixedUpdate();
        }

        // Dash finished naturally
        if (currentState == EnemyState.Charging)
        {
            currentState = EnemyState.Chasing;
            rb.linearVelocity = Vector3.zero; // Stop sliding
            Debug.Log("Dash Finished, returning to Chase.");
        }
    }

    // Collision Handling
    void OnCollisionEnter(Collision collision)
    {
        if (currentState == EnemyState.Charging)
        {
            // If hitting own faction object or a destructible prop
            if (collision.gameObject.layer == myFactionLayer || collision.gameObject.CompareTag("DestructibleProp"))
            {
                Debug.Log("SMASH! Prop destroyed.");
                Destroy(collision.gameObject);

                StopAllCoroutines();
                currentState = EnemyState.Chasing;
            }
            // If hitting the Player
            else if (collision.gameObject.CompareTag("Player"))
            {
                Debug.Log("Hit the Player!");
                // Apply damage logic here

                StopAllCoroutines();
                currentState = EnemyState.Chasing;
            }
            // If hitting a Wall (that is not ground)
            else if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
            {
                Debug.Log("Hit a wall!");
                StopAllCoroutines();
                currentState = EnemyState.Chasing;
            }
        }
    }
}