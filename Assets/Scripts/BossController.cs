using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum BossElementType
{
    None = 0,
    Fire = 1,
    Water = 2,
    Grass = 3
}

[Serializable]
public struct ElementAttackPattern
{
    public GameObject prefab;                   // พรีแฟบที่จะ spawn (มี Animator + AOE อยู่ข้างใน)
    public float attackInterval;                // คาบการโจมตีแต่ละครั้ง (ทำซ้ำ)
    public float minAttackDuration, maxAttackDuration;
    public float minAttackPauseDuration, maxAttackPauseDuration;
    public float initialDelay;                  // หน่วงก่อนเริ่มทำงานครั้งแรก
    public bool defaultRotateToBoss;            // ถ้าเป็น beam ให้ติ๊กไว้
}

class ElementAttackState
{
    public ElementAttackPattern data;
    public float intervalTimer;
    public float modeTimer;
    public bool isAttacking;
    public bool initialized;

    public ElementAttackState(ElementAttackPattern pattern)
    {
        data = pattern;
        intervalTimer = data.attackInterval;
        modeTimer = data.initialDelay > 0 ? data.initialDelay : 0f;
        isAttacking = false;   // รอ initialDelay ก่อน
        initialized = false;
    }

    public void Tick(float dt, out bool fireNow)
    {
        fireNow = false;

        // Initial delay ครั้งเดียว
        if (!initialized)
        {
            if (modeTimer > 0f)
            {
                modeTimer -= dt;
                return;
            }
            initialized = true;
            isAttacking = true;
            modeTimer = UnityEngine.Random.Range(data.minAttackDuration, data.maxAttackDuration);
            intervalTimer = Mathf.Max(0.01f, data.attackInterval);
        }

        // เดินโหมด โจมตี/พัก
        if (modeTimer > 0f)
        {
            modeTimer -= dt;
        }
        else
        {
            isAttacking = !isAttacking;
            modeTimer = isAttacking
                ? UnityEngine.Random.Range(data.minAttackDuration, data.maxAttackDuration)
                : UnityEngine.Random.Range(data.minAttackPauseDuration, data.maxAttackPauseDuration);

            if (isAttacking)
                intervalTimer = Mathf.Max(0.01f, data.attackInterval);
        }

        // ถ้าอยู่ในโหมดโจมตี เดิน interval และยิงเมื่อครบ
        if (isAttacking)
        {
            intervalTimer -= dt;
            if (intervalTimer <= 0f)
            {
                fireNow = true;
                intervalTimer += Mathf.Max(0.01f, data.attackInterval); // คงที่ทุกนัด
            }
        }
    }
}

public class BossController : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 300f;
    private float currentHP;
    public bool bossIsDead = false;

    [Header("Attack Settings")]
    public Transform[] firePoints;

    [Header("Normal Attack Settings")]
    public GameObject bossBulletPrefab;
    public bool canNormalAttack = false;
    [Space]
    [SerializeField] float normalAttackInterval = 0.5f;
    [SerializeField] float normalAttackTimer;
    [SerializeField] float minNormalAttackDuration = 5f, maxNormalAttackDuration = 10f;
    [SerializeField] private float minNormalAttackPauseDuration = 2f, maxNormalAttackPauseDuration = 5f;

    private Coroutine canNormalAttackRoutine;

    [Header("Elemental Attack")]
    [SerializeField] private List<ElementAttackPattern> fireTypeAttackPattern;
    [SerializeField] private List<ElementAttackPattern> waterTypeAttackPattern;
    [SerializeField] private List<ElementAttackPattern> grassTypeAttackPattern;

    private readonly List<ElementAttackState> _activeElementStates = new List<ElementAttackState>();
    private Coroutine _elementLoop;

    [Header("Element State")]
    public BossElementType currentElement = BossElementType.None;

    [Header("Phase")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.3f;
    private int currentPhase = 1;

    public StatsManager1 stats;
    private Coroutine attackRoutine;

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

        if (_elementLoop == null)
            _elementLoop = StartCoroutine(ElementalAttackLoop());

        Debug.Log("Boss StartProcess called");
    }

    private void StopProcess()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        if (_elementLoop != null)
        {
            StopCoroutine(_elementLoop);
            _elementLoop = null;
        }
        _activeElementStates.Clear();
    }

    void HandleCanNormalAttack()
    {
        if (!canNormalAttack) normalAttackTimer -= Time.deltaTime;
        if (normalAttackTimer <= 0)
        {
            canNormalAttack = true;
            normalAttackTimer = Random.Range(minNormalAttackPauseDuration, maxNormalAttackPauseDuration);

            if (canNormalAttackRoutine != null) StopCoroutine(canNormalAttackRoutine);
            canNormalAttackRoutine = StartCoroutine(CanNormalAttackRoutine());
        }
    }

    IEnumerator CanNormalAttackRoutine()
    {
        canNormalAttack = true;
        yield return new WaitForSeconds(Random.Range(minNormalAttackDuration, maxNormalAttackDuration));
        canNormalAttack = false;
    }

    IEnumerator AttackRoutine()
    {
        while (true)
        {
            float randomInterval = Random.Range(normalAttackInterval * 0.1f, normalAttackInterval);
            yield return new WaitForSeconds(randomInterval);
            Attack(); // Normal attack
        }
    }

    // ===== Normal Attack: ยิงกระสุนแบบสุ่มรอบตำแหน่งผู้เล่น =====
    void Attack()
    {
        if (!canNormalAttack) return;
        if (GameStateManager.Instance.currentState != GameState.Process) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (var fp in firePoints)
        {
            GameObject bullet = Instantiate(bossBulletPrefab, fp.position, Quaternion.identity);
            BossBullet bp = bullet.GetComponent<BossBullet>();
            if (bp != null)
            {
                var p = player.transform.position;
                Vector3 target = new Vector3(
                    Random.Range(-p.x, p.x),
                    Random.Range(-p.y, p.y),
                    p.z
                );
                bp.SetTarget(target);
            }
        }
    }

    // ===== Elemental Attack Overload: แค่ spawn พรีแฟบ แล้วให้ Animator จัดการ TriggerAttack/Destroy =====
    void Attack(in ElementAttackPattern pattern, Vector3 spawnPos, bool rotateToBoss = false)
    {
        if (GameStateManager.Instance.currentState != GameState.Process) return;
        if (pattern.prefab == null) return;

        GameObject obj = Instantiate(pattern.prefab, spawnPos, Quaternion.identity);

        bool doRotate = rotateToBoss || pattern.defaultRotateToBoss;
        if (doRotate)
        {
            Vector3 dir = (transform.position - spawnPos).normalized;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // 2D: ให้แกน up หันเข้าหาบอส
            obj.transform.rotation = Quaternion.AngleAxis(angleDeg, Vector3.forward);
        }

        // ไม่ต้องเรียกอะไรบน AreaOfEffect: Animator ในพรีแฟบจะ trigger เอง
    }

    // ===== Elemental control =====
    public void SetElement(BossElementType newElement)
    {
        if (currentElement == newElement) return;
        currentElement = newElement;

        _activeElementStates.Clear();

        List<ElementAttackPattern> src = null;
        switch (currentElement)
        {
            case BossElementType.Fire:  src = fireTypeAttackPattern;  break;
            case BossElementType.Water: src = waterTypeAttackPattern; break;
            case BossElementType.Grass: src = grassTypeAttackPattern; break;
        }

        if (src != null)
        {
            foreach (var pat in src)
                _activeElementStates.Add(new ElementAttackState(pat));
        }
    }

    IEnumerator ElementalAttackLoop()
    {
        var wait = new WaitForEndOfFrame();

        while (true)
        {
            if (GameStateManager.Instance.currentState == GameState.Process && _activeElementStates.Count > 0)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Vector3 playerPos = player ? player.transform.position : transform.position;

                for (int i = 0; i < _activeElementStates.Count; i++)
                {
                    _activeElementStates[i].Tick(Time.deltaTime, out bool fireNow);
                    if (fireNow)
                    {
                        Attack(_activeElementStates[i].data, playerPos, _activeElementStates[i].data.defaultRotateToBoss);
                    }
                }
            }

            yield return wait;
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
