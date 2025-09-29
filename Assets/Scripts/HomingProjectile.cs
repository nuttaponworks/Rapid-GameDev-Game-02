using System.Collections;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("Refs (optional)")]
    [SerializeField] private Rigidbody2D _rb;

    public BossElementType elements;

    // -------- Homing core --------
    private Transform _target;
    private float _turnRateDeg;
    private float _accel;
    private float _maxSpeed;
    private float _homingDelay;
    private bool _homing;

    // -------- Separation (avoid other projectiles) --------
    [Header("Separation")]
    [Tooltip("รัศมีตรวจจับเพื่อนบ้าน (หน่วย world)")]
    [SerializeField] private float _separationRadius = 1.25f;
    [Tooltip("น้ำหนักผลัก (0=ไม่ใช้)")]
    [SerializeField] private float _separationWeight = 1.0f;
    [Tooltip("เลเยอร์ของกระสุน (ตั้งให้ตรงกับพรีแฟบกระสุน)")]
    [SerializeField] private LayerMask _separationMask;
    [Tooltip("จำกัดจำนวนข้างบ้านที่คิด (กันหนักเครื่อง)")]
    [SerializeField] private int _maxNeighbors = 12;

    // non-alloc buffer
    private static readonly Collider2D[] _sepHits = new Collider2D[32];

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
        
        
        AudioManager.instance.PlaySFX(14);
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
            // 1) ทิศไปหาเป้าหมาย (normalized)
            Vector2 toTarget = ((Vector2)_target.position - (Vector2)transform.position);
            Vector2 desiredDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : curDir;

            // 2) เวกเตอร์ผลักจากเพื่อนบ้าน (Separation)
            Vector2 sepDir = ComputeSeparationDir();
            if (sepDir.sqrMagnitude > 0f && _separationWeight > 0f)
            {
                // ผสม: อย่าให้ผลักลบล้างเป้า 100% => normalize หลัง blend
                desiredDir = (desiredDir + sepDir * _separationWeight).normalized;
            }

            // 3) หมุนเข้าหาทิศที่ต้องการแบบจำกัดอัตราเลี้ยว
            float maxRad = _turnRateDeg * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector3 newDir3 = Vector3.RotateTowards(curDir, desiredDir, maxRad, float.MaxValue);
            Vector2 newDir = new Vector2(newDir3.x, newDir3.y);

            // 4) เร่งและคุมความเร็วเพดาน
            speed = Mathf.Min(_maxSpeed, speed + _accel * Time.fixedDeltaTime);
            vel = newDir * speed;

            _rb.linearVelocity = vel;
            transform.right = newDir; // forward = right
        }
        else
        {
            // ยังไม่โฮมมิ่ง: หันตามความเร็ว
            if (speed > 0.0001f) transform.right = curDir;
        }
    }

    /// <summary>
    /// คำนวณเวกเตอร์ผลักจากเพื่อนบ้านภายในรัศมี:
    /// sum( (self - other).normalized * weight ), weight ~ 1/dist^2
    /// </summary>
    private Vector2 ComputeSeparationDir()
    {
        if (_separationRadius <= 0f || _separationWeight <= 0f) return Vector2.zero;

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, _separationRadius, _sepHits, _separationMask);
        if (count <= 0) return Vector2.zero;

        Vector2 sum = Vector2.zero;
        int used = 0;
        Vector2 self = transform.position;

        for (int i = 0; i < count && used < _maxNeighbors; i++)
        {
            var col = _sepHits[i];
            if (col == null) continue;
            if (col.attachedRigidbody == _rb) continue;          // ตัวเอง
            if (col.gameObject == gameObject) continue;

            // กรองให้เฉพาะกระสุนประเภทเดียวกัน (ถ้าต้องการ)
            if (!col.TryGetComponent<HomingProjectile>(out var _)) continue;

            Vector2 otherPos = col.transform.position;
            Vector2 away = self - otherPos;
            float dist = away.magnitude;
            if (dist < 0.0001f) continue;

            // น้ำหนักตามระยะ: ใกล้มากผลักแรงกว่า (1/r^2) และผ่อนลงเมื่อเข้าใกล้รัศมีขอบ
            float w = 1f / (dist * dist);
            sum += away / dist * w; // normalized * weight
            used++;
        }

        if (sum.sqrMagnitude <= 0f) return Vector2.zero;
        return sum.normalized;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_separationRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _separationRadius);
        }
    }
#endif
}
