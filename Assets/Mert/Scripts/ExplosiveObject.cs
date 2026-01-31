using UnityEngine;

public class ExplosiveObject : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 5f; // How big is the explosion?
    public int playerDamage = 20;      // How much damage to deal to the player?
    
    [Header("Filters")]
    public LayerMask groundLayer;      // Assign "Ground" layer here to prevent destroying the floor
    public GameObject explosionVFX;    // Optional: Drag a particle effect prefab here

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Check if the colliding object is the Sion Enemy
        SionEnemyAI enemy = collision.gameObject.GetComponent<SionEnemyAI>();

        if (enemy != null)
        {
            // 2. Check if the enemy is strictly in the CHARGING state
            if (enemy.currentState == SionEnemyAI.EnemyState.Charging)
            {
                Explode();
            }
        }
    }

    void Explode()
    {
        // A. Visual Effect (Optional)
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        // B. Create the Logic "Trigger" Sphere
        // This gets every collider inside the radius instantly
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        Debug.Log("Explosion hit " + hitColliders.Length + " objects.");

        foreach (Collider hit in hitColliders)
        {
            GameObject target = hit.gameObject;

            // --- FILTER 1: Skip the Ground ---
            // Checks if the object's layer is included in the Ground LayerMask
            if (((1 << target.layer) & groundLayer) != 0) 
                continue;

            // --- FILTER 2: Skip the Enemy itself ---
            // We don't want to delete the enemy, just let it get stunned by its own script
            if (target.GetComponent<SionEnemyAI>() != null) 
                continue;

            // --- LOGIC: Damage Player ---
            if (target.CompareTag("Player"))
            {
                Debug.Log($"<color=red>EXPLOSION! Player took {playerDamage} damage.</color>");
                
                // UNCOMMENT and adapt this line to your specific Player Health script:
                // target.GetComponent<PlayerHealth>().TakeDamage(playerDamage);
            }
            // --- LOGIC: Destroy Everything Else ---
            else
            {
                // Destroy other objects (crates, props, etc.)
                // This prevents the barrel from trying to destroy itself twice
                if (target != this.gameObject)
                {
                    Destroy(target);
                }
            }
        }

        // C. Destroy the Barrel
        Destroy(this.gameObject);
    }

    // Visualization to see the radius in the Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}