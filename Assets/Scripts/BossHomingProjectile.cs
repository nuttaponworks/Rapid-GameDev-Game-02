using System;
using System.Collections.Generic;
using UnityEngine;

public class BossHomingProjectile : MonoBehaviour
{

    [Header("Movement")] [SerializeField] private float speed = 10f;
    [SerializeField] private float maxTurnRateDegPerSec = 360f; // ยิ่งต่ำ โค้งยิ่งกว้าง

    [Tooltip("ใช้ Rigidbody2D ถ้ามี (แนะนำสำหรับฟิสิกส์)")] [SerializeField]
    private bool useRigidbody2D = true;

    [Header("Target Lag")] [Tooltip("เวลาหน่วงตำแหน่งเป้าหมาย (วินาที)")] [SerializeField]
    private float lagSeconds = 0.25f;

    [Tooltip("อัตราเก็บตัวอย่างตำแหน่งเป้าหมาย (Hz)")] [SerializeField]
    private float sampleRate = 30f;

    [Header("Lifetime")] [SerializeField] private float lifeTime = 8f;

    [Header("Hit")] [SerializeField] private LayerMask hitMask;
    [SerializeField] private GameObject hitVfxPrefab;

    private Transform _target;
    private readonly List<Vector3> _history = new(); // คิวตำแหน่งเป้าหมายย้อนหลัง (world)
    private float _sampleTimer;
    private float _sampleInterval;
    private int _historyCapacity;
    private Rigidbody2D _rb;

    private void Start()
    {
        SetTarget(GameStateManager.Instance.playerStat.gameObject.transform);
    }

    public void SetTarget(Transform t)
    {
        _target = t;
        _history.Clear();
        WarmStartHistory();
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sampleInterval = Mathf.Max(1f / Mathf.Max(1f, sampleRate), 0.005f);
        _historyCapacity = Mathf.CeilToInt(lagSeconds / _sampleInterval) + 1;
    }

    private void OnEnable()
    {
        // ป้องกันลืมทำลาย
        if (lifeTime > 0) Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // เก็บตำแหน่งเป้าหมายย้อนหลัง (sampling แบบคงที่)
        if (_target != null)
        {
            _sampleTimer += Time.deltaTime;
            while (_sampleTimer >= _sampleInterval)
            {
                _sampleTimer -= _sampleInterval;
                PushHistory(_target.position);
            }
        }
        else
        {
            // ไม่มีเป้า ก็ปล่อยวิ่งตรงตามทิศปัจจุบัน
        }
    }

    private void FixedUpdate()
    {
        Vector3 aimPos = GetLaggedTargetPosition();

        // ทิศที่ควรหันไป
        Vector2 toTarget = ((Vector2)aimPos - (Vector2)transform.position);
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float currentZ = transform.eulerAngles.z;
            float maxStep = maxTurnRateDegPerSec * Time.fixedDeltaTime;
            float newZ = Mathf.MoveTowardsAngle(currentZ, desiredAngle - 90f, maxStep); // ให้แกน up ชี้หน้า

            transform.rotation = Quaternion.Euler(0, 0, newZ);
        }

        // เดินหน้าไปตามหัว
        Vector2 forward = transform.up; // up คือ heading (เพราะเราหัก -90 ข้างบน)
        Vector2 vel = forward * speed;

        if (useRigidbody2D && _rb != null)
            _rb.velocity = vel;
        else
            transform.position += (Vector3)(vel * Time.fixedDeltaTime);
    }

    // ---------- History (Lag) ----------
    private void WarmStartHistory()
    {
        if (_target == null) return;
        _historyCapacity = Mathf.Max(2, Mathf.CeilToInt(lagSeconds / Mathf.Max(_sampleInterval, 0.001f)) + 1);
        _history.Clear();
        Vector3 p = _target.position;
        for (int i = 0; i < _historyCapacity; i++) _history.Add(p);
    }

    private void PushHistory(Vector3 pos)
    {
        if (_history.Count == 0)
        {
            _history.Add(pos);
            return;
        }

        _history.Add(pos);
        if (_history.Count > _historyCapacity) _history.RemoveAt(0);
    }

    private Vector3 GetLaggedTargetPosition()
    {
        if (_target == null || _history.Count == 0) return transform.position + transform.up; // ไปข้างหน้าเฉย ๆ

        // index ย้อนหลังตาม lagSeconds
        int idx = Mathf.Clamp(_history.Count - 1 - Mathf.CeilToInt(lagSeconds / Mathf.Max(_sampleInterval, 0.001f)), 0,
            _history.Count - 1);
        return _history[idx];
    }

    // ---------- Hit ----------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        if (hitVfxPrefab) Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_target != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 lagPos = Application.isPlaying ? GetLaggedTargetPosition() : _target.position;
            Gizmos.DrawWireSphere(lagPos, 0.15f);
            Gizmos.DrawLine(transform.position, lagPos);
        }
    }
#endif
}