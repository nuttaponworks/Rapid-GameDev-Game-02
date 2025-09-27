using System.Collections;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("Refs (optional)")]
    [SerializeField] private Rigidbody2D _rb;

    private Transform _target;
    private float _turnRateDeg;
    private float _accel;
    private float _maxSpeed;
    private float _homingDelay;

    private bool _homing;

    public void Init(Transform target, Vector2 initialDir, float initialSpeed,
                     float turnRateDeg, float acceleration, float maxSpeed, float homingDelay)
    {
        _target      = target;
        _turnRateDeg = turnRateDeg;
        _accel       = acceleration;
        _maxSpeed    = maxSpeed;
        _homingDelay = homingDelay;

        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        Vector2 v0 = initialDir.normalized * initialSpeed;
        if (_rb != null) _rb.linearVelocity = v0;

        StartCoroutine(HomingRoutine());
    }

    private void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
    }

    private IEnumerator HomingRoutine()
    {
        if (_homingDelay > 0) yield return new WaitForSeconds(_homingDelay);
        _homing = true;
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        Vector2 vel = _rb.linearVelocity;
        float speed = vel.magnitude;
        Vector2 curDir = speed > 0.0001f ? vel / Mathf.Max(speed, 0.0001f) : (Vector2)transform.right;

        if (_homing && _target != null)
        {
            Vector2 desiredDir = ((Vector2)_target.position - (Vector2)transform.position).normalized;

            // หมุนทิศปัจจุบันเข้าหาเป้าด้วยมุมสูงสุดต่อเฟรม
            float maxRad = _turnRateDeg * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector3 newDir3 = Vector3.RotateTowards(curDir, desiredDir, maxRad, float.MaxValue);
            Vector2 newDir = new Vector2(newDir3.x, newDir3.y);

            // เร่งความเร็ว และคุมเพดาน
            speed = Mathf.Min(_maxSpeed, speed + _accel * Time.fixedDeltaTime);
            vel = newDir * speed;

            _rb.linearVelocity = vel;
            transform.right = newDir; // หันหน้าไปตามทิศ
        }
        else
        {
            // ยังไม่เริ่มโฮมมิ่ง: คงทิศเดิม แต่หันตามความเร็วให้ดูคม
            if (speed > 0.0001f) transform.right = curDir;
        }
    }
}
