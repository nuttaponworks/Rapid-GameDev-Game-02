using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public GameObject prefab;
    public float attackInterval;
    public float minAttackDuration, maxAttackDuration;
    public float minAttackPauseDuration, maxAttackPauseDuration;
    public float initialDelay;
    public bool defaultRotateToBoss;
    public bool spawnAtBoss; // 👈 NEW: false (default) = spawn ที่ player, true = spawn ที่ boss
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
    [SerializeField] private Animator anim;
    public static System.Action<BossElementType> OnElementChanged;
    
    [Header("HP")]
    public float maxHP = 300f;
    public float currentHP;
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
// ===== Element cycle =====
    private readonly List<BossElementType> _remainingElements = new(); // เก็บ 2 ธาตุที่เหลือ

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

    }

    private void Update()
    {
        HandleCanNormalAttack();
        //
        if(Input.GetKeyDown(KeyCode.Alpha6)) 
            SetElement(BossElementType.None);
        else if(Input.GetKeyDown(KeyCode.Alpha7)) 
            SetElement(BossElementType.Fire);
        else if(Input.GetKeyDown(KeyCode.Alpha8)) 
            SetElement(BossElementType.Water);
        else if(Input.GetKeyDown(KeyCode.Alpha9)) 
            SetElement(BossElementType.Grass);
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

        InitElementCycleAtStart();
    }

    // IEnumerator DelayStartElemental()
    // {
    //     yield return new WaitForSeconds(1f);
    //     //if (GameStateManager.Instance.currentState == GameState.Process) 
    //     InitElementCycleAtStart();
    //
    // }
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
    public void InitElementCycleAtStart()
    {
        // reset pool
        _remainingElements.Clear();
        var pool = new List<BossElementType> { BossElementType.Fire, BossElementType.Water, BossElementType.Grass };

        // สุ่มตัวเริ่ม
        int startIdx = Random.Range(0, pool.Count);
        var startElem = pool[startIdx];

        // ตั้งธาตุเริ่ม
        SetElement(startElem);

        // เก็บสองตัวที่เหลือ
        for (int i = 0; i < pool.Count; i++)
            if (i != startIdx) _remainingElements.Add(pool[i]);
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
            AudioManager.instance.PlaySFX(15);
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

        // 🔔 ยิงทริกเกอร์อนิเมชันทุกครั้งที่บอสโจมตีผ่าน Elemental
        if (anim == null) anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Attack"); // หรือ anim.SetTrigger("Attack");

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

        OnElementChanged?.Invoke(currentElement);
        
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
        
        Debug.Log($"Switched element to: {currentElement}");
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
                        Debug.Log("Element Attack!");
                        var pat = _activeElementStates[i].data;
                        Vector3 spawnPos = pat.spawnAtBoss ? transform.position : playerPos; // 👈 เลือกตำแหน่งตาม flag
                        Attack(pat, spawnPos, pat.defaultRotateToBoss);
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

        // (phase logic เดิมคงไว้ได้)
        float pct = currentHP / maxHP;
        if (pct <= phase3Threshold) currentPhase = 3;
        else if (pct <= phase2Threshold) currentPhase = 2;

        AudioManager.instance.PlaySFX(UnityEngine.Random.Range(27,32));
        if (currentHP > 0) return;

        // HP หมด
        if (_remainingElements.Count > 0)
        {
            // ฟื้นเต็มและเปลี่ยนเป็น 1 ใน 2 ที่เหลือ
            int idx = Random.Range(0, _remainingElements.Count);
            var next = _remainingElements[idx];
            _remainingElements.RemoveAt(idx);

            currentHP = maxHP;
            SetElement(next);

            Debug.Log($"Boss revived with new element: {next}. Remaining: {_remainingElements.Count}");
            return;
        }

        // ไม่มีธาตุเหลือแล้ว → ชนะ
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
        if (other.TryGetComponent<HomingProjectile>(out var proj))
        {
            TakeElementalDamage(other,proj.elements);
            Destroy(other.gameObject);
        }
    }

    void TakeElementalDamage(Collider2D other, BossElementType targetElement)
    {
        if (homingHitParticlePrefab != null)
            Instantiate(homingHitParticlePrefab, other.transform.position, Quaternion.identity);

        int dmg = GameStateManager.Instance.playerDamage;
        if (currentElement != targetElement) dmg /= 2;
        TakeDamage(dmg);
    }
}
