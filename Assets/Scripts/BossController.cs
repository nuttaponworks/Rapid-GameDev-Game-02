using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 300f;   
    private float currentHP;

    [Header("Attack")]
    public GameObject bossBulletPrefab; 
    public Transform[] firePoints;          
    public float attackInterval = 2;

    [Header("Phase")]
    public float phase2Threshold = 0.6f; 
    public float phase3Threshold = 0.3f; 
    private int currentPhase = 1;

    public StatsManager stats;

    void Start()
    {
        currentHP = maxHP;
        if (stats != null) stats.StartTimer();
        StartCoroutine(AttackRoutine()); 
    }
    IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);
            Attack();
        }
    }

    void Attack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        foreach (var fp in firePoints)
        {
            GameObject bullet = Instantiate(bossBulletPrefab, fp.position, Quaternion.identity);

            BossBullet bp = bullet.GetComponent<BossBullet>();
            if (bp != null)
            {
                bp.SetTarget(player.transform.position);
            }
        }
    }
    public void TakeDamage(float dmg)
    {
        currentHP -= dmg;
        Debug.Log("Boss HP: " + currentHP + "/" + maxHP);

        float pct = currentHP / maxHP;
        if (pct <= phase3Threshold) currentPhase = 3;
        else if (pct <= phase2Threshold) currentPhase = 2;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (stats != null) stats.StopAndRecord();
        Debug.Log("Boss Died!");
        Destroy(gameObject);
    }

}


