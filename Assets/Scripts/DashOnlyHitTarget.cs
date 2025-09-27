using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TarodevController
{
    /// <summary>
    /// วัตถุที่ถูก "ชน" ได้เฉพาะตอนผู้เล่นกำลัง Dash เท่านั้น
    /// - เมื่อชน: ผู้เล่นเด้งกลับทิศเดิม ด้วยความเร็ว = DashSpeed * BounceMultiplier
    /// - รีเซ็ตคูลดาวน์ Dash ของผู้เล่นทันที
    /// - นับ HP (Hit Count) ลดลง 1 ต่อการชน 1 ครั้ง ครบแล้ว Destroy ตัวเอง
    /// - สปอนพาร์ติเคิลที่ตำแหน่งเป้า โดยหมุน Z หันไปทางผู้เล่น
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DashOnlyHitTarget : MonoBehaviour
    {
        [Header("On Destroy -> Shoot Projectiles")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private int   _projectileCount    = 3;
        [SerializeField] private float _projectileSpreadDeg = 10f;   // กระจายซ้าย/ขวา (องศา)
        [SerializeField] private float _projInitialSpeed    = 10f;   // ความเร็วเริ่ม (หนีออกจากบอส)
        [SerializeField] private float _projHomingDelay     = 0.15f; // หน่วงก่อนเริ่มเลี้ยวกลับ
        [SerializeField] private float _projTurnRateDeg     = 540f;  // อัตราหัน (deg/sec)
        [SerializeField] private float _projAcceleration    = 50f;   // อัตราเร่ง
        [SerializeField] private float _projMaxSpeed        = 28f;   // เพดานความเร็ว
        
        [SerializeField] private Slider healthSlider;
        [Header("Hit Settings")]
        [Tooltip("จำนวนครั้งที่โดนชนโดย Dash ก่อนจะพัง")]
        [SerializeField] private int _hitPoints = 3;

        [Tooltip("ให้ Collider เป็น Trigger (แนะนำ)")]
        [SerializeField] private bool _useTrigger = true;

        [Header("Bounce")]
        [Tooltip("กำลังเด้งกลับ = DashSpeed * BounceMultiplier")]
        [SerializeField] private float _bounceMultiplier = 3f;

        [Header("Hit Particle")]
        [Tooltip("พรีแฟบพาร์ติเคิลที่จะสปอนเมื่อโดนชน (เว้นว่างได้ถ้าไม่ใช้)")]
        [SerializeField] private GameObject _hitParticlePrefab;
        [SerializeField] private GameObject _destroyParticlePrefab;

        [Tooltip("ถ้าพรีแฟบของคุณหันหน้าคนละแกน ให้ใส่มุมชดเชย (องศา) เช่น -90 หรือ +90")]
        [SerializeField] private float _particleZRotationOffset = 0f;

        [Tooltip("ทำลายพาร์ติเคิลอัตโนมัติหลังเวลานี้ (วินาที). ถ้า <= 0 จะไม่ทำลายอัตโนมัติ")]
        [SerializeField] private float _particleAutoDestroyAfter = 2f;

        [Header("Events")]
        [Tooltip("ถูกเรียกจาก OnDestroy()")]
        public UnityEvent OnDestroyed;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = _useTrigger;
        }

        private void Start()
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = _hitPoints;
                healthSlider.value = _hitPoints;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_useTrigger) return;
            TryHandleHit(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_useTrigger) return;
            TryHandleHit(collision.rigidbody ? collision.rigidbody.gameObject : collision.gameObject);
        }

        private void TryHandleHit(GameObject otherGO)
        {
            if (otherGO == null) return;

            var player = otherGO.GetComponent<PlayerController>();
            if (player == null) return;

            // ต้องกำลัง Dash เท่านั้น
            if (!player.IsDashing) return;

            Vector2 dashDir = player.DashDirection;
            float dashSpeed = player.DashSpeed;

            // เด้งกลับแรงตาม BounceMultiplier
            Vector2 bounceVel = -dashDir * dashSpeed * _bounceMultiplier;

            // ยกเลิกดาช + เด้ง + รีเซ็ตคูลดาวน์
            player.CancelDashAndBounce(bounceVel);
            player.ResetDashCooldown();

            // ---- Spawn Particle ที่ตำแหน่งของ "เป้า" และหมุน Z ให้หันไปหา Player ----
            SpawnHitParticleTowardsPlayer(player.transform.position);

            // หัก HP
            _hitPoints = Mathf.Max(0, _hitPoints - 1);
            
            if (healthSlider != null) healthSlider.value = _hitPoints;

            if (_hitPoints <= 0)
            {
                if (_hitPoints <= 0)
                {
                    Instantiate(_destroyParticlePrefab, this.transform.position, quaternion.identity);
                    SpawnDeathProjectiles();   // <-- เพิ่มบรรทัดนี้
                    Destroy(gameObject);
                }
                
                Instantiate(_destroyParticlePrefab, this.transform.position, quaternion.identity);
                Destroy(gameObject);
            }
        }

        private void SpawnDeathProjectiles()
        {
            if (_projectilePrefab == null) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.currentBossPrefab == null) return;

            Transform boss = gsm.currentBossPrefab.transform;
            Vector2 toTarget = ((Vector2)boss.position - (Vector2)transform.position);
            if (toTarget.sqrMagnitude < 0.0001f) toTarget = Vector2.right;

            // ทิศเริ่ม: "หนีออกจากบอส"
            Vector2 baseAwayDir = (-toTarget).normalized;

            for (int i = 0; i < _projectileCount; i++)
            {
                float offset = (i - (_projectileCount - 1) * 0.5f) * _projectileSpreadDeg;
                Vector2 initialDir = Rotate2D(baseAwayDir, offset);

                Quaternion rot = Quaternion.Euler(0, 0, Mathf.Atan2(initialDir.y, initialDir.x) * Mathf.Rad2Deg);
                var go = Instantiate(_projectilePrefab, transform.position, rot);

                // ส่งพารามิเตอร์ให้กระสุน
                var proj = go.GetComponent<HomingProjectile>();
                if (proj != null)
                {
                    proj.Init(
                        boss,
                        initialDir,
                        _projInitialSpeed,
                        _projTurnRateDeg,
                        _projAcceleration,
                        _projMaxSpeed,
                        _projHomingDelay
                    );
                }
                else
                {
                    // fallback: ถ้าไม่มีสคริปต์ ให้พุ่งทิศเริ่มด้วย Rigidbody2D (ถ้ามี)
                    var rb = go.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = initialDir * _projInitialSpeed;
                }
            }
        }

        private static Vector2 Rotate2D(Vector2 v, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float ca = Mathf.Cos(rad);
            float sa = Mathf.Sin(rad);
            return new Vector2(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
        }
        
        private void SpawnHitParticleTowardsPlayer(Vector3 playerWorldPos)
        {
            if (_hitParticlePrefab == null) return;

            // ทิศจากเป้า -> ไปหา Player
            Vector2 toPlayer = (playerWorldPos - transform.position);
            float angleDeg = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // หมุนเฉพาะแกน Z + ชดเชยมุมตามที่ตั้งค่า
            Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg + _particleZRotationOffset);

            GameObject inst = Instantiate(_hitParticlePrefab, transform.position, rot);

            // ถ้ามี ParticleSystem ให้เล่น
            var ps = inst.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            // ทำลายอัตโนมัติถ้ากำหนดไว้
            if (_particleAutoDestroyAfter > 0f)
            {
                Destroy(inst, _particleAutoDestroyAfter);
            }
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}
