using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class BossController : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 300f;
    private float currentHP;
    public bool bossIsDead = false;
    
    [Header("Normal Attack")]
    public GameObject bossBulletPrefab;
    public Transform[] firePoints;
    public bool canNormalAttack = false;
    [Space]
    [SerializeField] float normalAttackInterval = 0.5f;
    [SerializeField] float normalAttackTimer;
    [SerializeField] float minNormalAttackDuration=5f, maxNormalAttackDuration = 10f;
    [SerializeField] private float minNormalAttackPauseDuration = 2f, maxNormalAttackPauseDuration = 5f;
    
    private Coroutine canNormalAttackRoutine;

    [Header("Phase")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.3f;
    private int currentPhase = 1;

    public StatsManager1 stats;
    private Coroutine attackRoutine;
// [เพิ่มฟิลด์ไว้บนสุดของคลาส]
    [Header("Hit FX")]
    [SerializeField] private GameObject homingHitParticlePrefab;

    void Start()
    {

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
        if (GameStateManager.Instance.currentState == GameState.Process)
            StartProcess();
    }

    private void Update()
    {
        HandleCanNormalAttack();
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Process:
                StartProcess();
                break;

            case GameState.End:
            case GameState.Warmup:
            case GameState.Start:
            case GameState.Summary:
                StopProcess();
                break;
        }
    }

    private void StartProcess()
    {
        if (stats != null) stats.StartTimer();
        
        currentHP = maxHP;
        canNormalAttack = true;
        
        if (attackRoutine == null)
            attackRoutine = StartCoroutine(AttackRoutine());
        
        Debug.Log("Boss StartProcess called");
        
    }

    private void StopProcess()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    void HandleCanNormalAttack()
    {
        if(!canNormalAttack) normalAttackTimer -= Time.deltaTime;
        if (normalAttackTimer <= 0)
        {
            canNormalAttack = true;
            normalAttackTimer = Random.Range(minNormalAttackPauseDuration,maxNormalAttackPauseDuration);
            
            if(canNormalAttackRoutine!=null) StopCoroutine(canNormalAttackRoutine);
            canNormalAttackRoutine = StartCoroutine(CanNormalAttackRoutine());
        }
    }

    IEnumerator CanNormalAttackRoutine()
    {
        canNormalAttack = true;
        yield return new WaitForSeconds(Random.Range(minNormalAttackDuration,maxNormalAttackDuration));
        canNormalAttack = false;
    }

    IEnumerator AttackRoutine()
    {
        while (true)
        {
            float randomInterval = Random.Range(normalAttackInterval*0.1f, normalAttackInterval);
            yield return new WaitForSeconds(randomInterval);
            Attack();
        }
    }

    void Attack()
    {
        if (!canNormalAttack) return;
        Debug.Log("Attack!");
        
        if (GameStateManager.Instance.currentState != GameState.Process) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (var fp in firePoints)
        {
            GameObject bullet = Instantiate(bossBulletPrefab, fp.position, Quaternion.identity);
            BossBullet bp = bullet.GetComponent<BossBullet>();
            if (bp != null)
                bp.SetTarget(new Vector3(
                    Random.Range(-player.transform.position.x,player.transform.position.x),
                    Random.Range(-player.transform.position.y,player.transform.position.y),
                    player.transform.position.z));
        }
    }

    public void TakeDamage(float dmg)
    {
        if (GameStateManager.Instance.currentState != GameState.Process) return;

        currentHP -= dmg;
        Debug.Log("Boss HP: " + currentHP + "/" + maxHP);

        float pct = currentHP / maxHP;
        if (pct <= phase3Threshold) currentPhase = 3;
        else if (pct <= phase2Threshold) currentPhase = 2;

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        bossIsDead = true;
        
        if (stats != null) stats.StopAndRecord();
        Debug.Log("Boss Died!");

        GameStateManager.Instance.ChangeState(GameState.End);

        Destroy(gameObject);
    }
    
    // [เพิ่มเมธอดใหม่ในคลาส]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<HomingProjectile>(out var _))
        {
            if (homingHitParticlePrefab != null)
                Instantiate(homingHitParticlePrefab, other.transform.position, Quaternion.identity);

            Destroy(other.gameObject);
        }
    }


}


