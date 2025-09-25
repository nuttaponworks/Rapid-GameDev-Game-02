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

    public StatsManager1 stats;
    private Coroutine attackRoutine;

    void Start()
    {
        currentHP = maxHP;


        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
        if (GameStateManager.Instance.currentState == GameState.Process)
            StartProcess();
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

    IEnumerator AttackRoutine()
    {
        Debug.Log("Boss is attacking...");
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);
            Attack();
        }
    }

    void Attack()
    {
        if (GameStateManager.Instance.currentState != GameState.Process) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (var fp in firePoints)
        {
            GameObject bullet = Instantiate(bossBulletPrefab, fp.position, Quaternion.identity);
            BossBullet bp = bullet.GetComponent<BossBullet>();
            Debug.Log("Bullet spawned at ");
            if (bp != null)
                bp.SetTarget(player.transform.position);
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
        if (stats != null) stats.StopAndRecord();
        Debug.Log("Boss Died!");

        GameStateManager.Instance.ChangeState(GameState.End);

        Destroy(gameObject);
    }

}


