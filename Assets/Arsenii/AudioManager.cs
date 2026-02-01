using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Footsteps")]
    [SerializeField] private EventReference footstepEvent;

    [Header("Other OneShots")]
    [SerializeField] private EventReference jumpEvent;
    [SerializeField] private EventReference landEvent;
    [SerializeField] private EventReference shootEvent;
    [SerializeField] private EventReference dashEvent;
    
    [Header("Enemy Sounds")]
    [SerializeField] private EventReference enemyAttackEvent;
    
    
    

    void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayFootstep(Vector3 position)
    {
        Debug.Log("footstep sound played");
        if (!footstepEvent.IsNull)
            RuntimeManager.PlayOneShot(footstepEvent, position);
    }

    public void PlayJump(Vector3 position)
    {
        if (!jumpEvent.IsNull)
            RuntimeManager.PlayOneShot(jumpEvent, position);
    }

    public void PlayLand(Vector3 position)
    {
        if (!landEvent.IsNull)
            RuntimeManager.PlayOneShot(landEvent, position);
    }

    public void PlayShoot(Vector3 position)
    {
        if (!shootEvent.IsNull)
            RuntimeManager.PlayOneShot(shootEvent, position);
    }

    public void DashShot(Vector3 position)
    {
        if (!dashEvent.IsNull)
            RuntimeManager.PlayOneShot(dashEvent, position);
    }
    public void PlayEnemyAttack(Vector3 position)
    {
        if (!enemyAttackEvent.IsNull)
            RuntimeManager.PlayOneShot(enemyAttackEvent, position);
    }
}