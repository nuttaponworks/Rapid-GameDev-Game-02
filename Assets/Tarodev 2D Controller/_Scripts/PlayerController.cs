using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private GameObject parryParticle;
        [SerializeField] private Animator _anim;

        private bool _facingRight = true;
// แปลงชื่อพารามิเตอร์ใน Animator เป็น hash เพื่อประสิทธิภาพ
        private static readonly int HashIsRunning = Animator.StringToHash("isRunning");
        private static readonly int HashIsFalling = Animator.StringToHash("isFalling");
        private static readonly int HashJump      = Animator.StringToHash("Jump");
        private static readonly int HashIsDashing  = Animator.StringToHash("isDashing");
        private static readonly int HashDash       = Animator.StringToHash("Dash");   
        
        [Header("Dash Trail")]
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private float _dashingStartWidth = 0.65f;
        [SerializeField] private float _idleStartWidth = 0f;

        [Header("Gravity Suspend")]
        [SerializeField] private float _gravitySuspendDurationAfterBounce = 0.5f;
        private float _gravitySuspendedUntil = -999f;
        private bool GravitySuspended => Time.time < _gravitySuspendedUntil;

        
        private bool _lastDashingState;
        
        public bool IsDashing => _isDashing;
        public Vector2 DashDirection => _dashDir;
        public float DashSpeed => _dashSpeed;
        
        [SerializeField] private ScriptableStats _stats;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        #region Interface
        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        #endregion

        private float _time;

        // ------------ DASH & UI ------------
        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 22f;
        [SerializeField] private float _dashDuration = 0.15f;
        
// Dash: คง _dashSpeed, _dashDuration ไว้ได้
        [Header("Stamina")]
        public float stamina;
        public float maxStamina = 100f;
        [SerializeField] private float _dashStaminaCost = 30f;
        [SerializeField] private float _staminaRegenTime = 3f;
        [SerializeField] private float _staminaOnResetDash = 25f;
        [SerializeField] private float _minStaminaToAct = 10f; // 👈 สแตมินาขั้นต่ำเพื่อ Dash/Glide


        [Header("Glide (RMB Hold)")]
        [SerializeField] private float _glideDrainPerSec = 10f;
        [SerializeField] private float _glideGravityMultiplier = 0.3f;
        [SerializeField] private float _glideMaxFallSpeedMultiplier = 0.4f;

// runtime
        private bool _isGliding;
        private float StaminaRegenPerSec => maxStamina / Mathf.Max(0.01f, _staminaRegenTime);
        private bool HasMinStamina => stamina >= _minStaminaToAct; // 👈 helper

        [Header("UI")]
        [Header("Direction Indicator")]
        [Tooltip("ใส่ LineRenderer ที่เป็นเส้นชี้ทิศ")]
        [SerializeField] private LineRenderer _directionLine;
        [SerializeField] private float _directionLineLength = 1.5f;

        private bool _isDashing;
        private float _dashEndTime;
        private float _dashCooldownUntil;
        private Vector2 _dashDir;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;

            // เตรียม LineRenderer ให้เป็น world-space
            if (_directionLine != null) _directionLine.useWorldSpace = true;

            // ซ่อนหลอดตอนเริ่ม
           
            if (_trail == null) _trail = GetComponent<TrailRenderer>() ?? GetComponentInChildren<TrailRenderer>();
            if (_trail != null) SetTrailStartWidth(_idleStartWidth); // เริ่มต้นปิดเส้น

            stamina = maxStamina;
        }

        private void Update()
        {
            if (GameStateManager.Instance.currentState != GameState.Process) return;
            
            _time += Time.deltaTime;
            GatherInput();
            
            HandleDashInput();   // ใช้สแตมินาแทนคูลดาวน์
            HandleGlideInput();  // คลิกขวาค้าง
            TickStamina();       // ฟื้น/ดูด
            UpdateDirectionLine();
            
            UpdateTrailByDashState();
        }
        // แก้เฉพาะ startWidth ไม่แตะ endWidth/widthCurve
        private void SetTrailStartWidth(float w) {
            if (_trail == null) return;
            _trail.startWidth = w;
        }

        // เรียกทุกเฟรมให้ตรงกับสถานะ dash
        private void UpdateTrailByDashState() {
            if (_trail == null) return;
            float desired = _isDashing ? _dashingStartWidth : _idleStartWidth;
            if (!Mathf.Approximately(_trail.startWidth, desired)) _trail.startWidth = desired;
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            if (GameStateManager.Instance.currentState != GameState.Process) return;
            UpdateTrailByDashState();
            CheckCollisions();

            if (_isDashing)
            {
                // ระหว่างดาช: ล็อกความเร็ว ไม่สนแรงโน้มถ่วง/คอนโทรล
                _frameVelocity = _dashDir * _dashSpeed;

                if (Time.time >= _dashEndTime) _isDashing = false;
            }
            else
            {
                HandleJump();
                HandleDirection();
                HandleGravity();
            }

            UpdateFacing();
            UpdateAnimator();
            ApplyMovement();
        }

        private void UpdateFacing()
        {
            float lookX = 0f;

            if (_isDashing) lookX = _dashDir.x;
            else if (Mathf.Abs(_frameVelocity.x) > 0.01f) lookX = _frameVelocity.x;
            else if (Mathf.Abs(_frameInput.Move.x) > 0.01f) lookX = _frameInput.Move.x;

            if (Mathf.Abs(lookX) <= 0.0001f) return; // ไม่เปลี่ยนถ้าไม่มีทิศที่ชัด

            bool faceRight = lookX > 0f;
            if (faceRight == _facingRight) return;

            _facingRight = faceRight;

            if (_anim != null)
            {
                _anim.GetComponent<SpriteRenderer>().flipX = !_facingRight; // ขวา = ไม่ flip
            }
            else
            {
                // fallback ถ้าไม่มี SpriteRenderer
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (_facingRight ? 1f : -1f);
                transform.localScale = s;
            }

        }

        private void UpdateAnimator()
        {
            if (_anim == null) return;

            bool running = Mathf.Abs(_frameVelocity.x) > 0.01f;
            _anim.SetBool(HashIsRunning, running);

            _anim.SetBool(HashIsFalling, !_grounded);

            // ใหม่: isDashing
            _anim.SetBool(HashIsDashing, _isDashing);
        }


        #region Dash Impl

        private void HandleDashInput()
        {
            // คลิกซ้ายเพื่อ Dash ถ้าพร้อม (คูลดาวน์หมด)
            if (Input.GetMouseButtonDown(0) && HasMinStamina && stamina >= _dashStaminaCost)
            {
                Vector3 mouseWorld = Camera.main != null
                    ? Camera.main.ScreenToWorldPoint(Input.mousePosition)
                    : (Vector3)Input.mousePosition;

                mouseWorld.z = transform.position.z;

                Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right; // กันกรณีเมาส์ทับตัวเอง

                _dashDir = dir;
                _isDashing = true;
                _dashEndTime = Time.time + _dashDuration;
                SpendStamina(_dashStaminaCost);
                SetTrailStartWidth(_dashingStartWidth);
                _lastDashingState = true;
                
                AudioManager.instance.PlaySFX(32);
                if (_anim != null) _anim.SetTrigger(HashDash);
            }
        }
        /// <summary>รีเซ็ตคูลดาวน์ดาชทันที</summary>
        public void ResetDashCooldown() {
            GainStamina(_staminaOnResetDash);
        }

        private void HandleGlideInput() {
            _isGliding = Input.GetMouseButtonDown(1) && HasMinStamina;
            if (_isGliding)
            {
                Instantiate(parryParticle, this.transform.position, Quaternion.identity);
            }
            if (_isGliding && stamina < _minStaminaToAct)
            {
                _isGliding = false; // 👈 ดับเมื่อหล่นต่ำกว่า min
            }
        }

        private void TickStamina()
        {
            if (_isGliding)
            {
                SpendStamina(_glideDrainPerSec * Time.deltaTime);
                if (stamina < _minStaminaToAct) _isGliding = false; // 👈 ดับเมื่อหล่นต่ำกว่า min
            }
            else
            {
                GainStamina(StaminaRegenPerSec * Time.deltaTime);
            }
        }


        private void SpendStamina(float amt) => stamina = Mathf.Max(0f, stamina - amt);
        private void GainStamina(float amt)  => stamina = Mathf.Min(maxStamina, stamina + amt);



        /// <summary>ยกเลิกดาชทันที แล้วเด้งด้วยความเร็วตามที่ส่งเข้าไป</summary>
        public void CancelDashAndBounce(Vector2 bounceVelocity)
        {
            _isDashing = false;

            // ใช้ระบบความเร็วของสคริปต์ (ไม่ยุ่ง Rigidbody โดยตรง)
            _frameVelocity = bounceVelocity;
            // _rb.velocity = bounceVelocity; // ❌ ไม่ใช้

            // ระงับแรงโน้มถ่วงชั่วคราว 0.5 วินาที
            _gravitySuspendedUntil = Time.time + _gravitySuspendDurationAfterBounce;

            // รีเซ็ตเอฟเฟกต์/แฟล็กที่เกี่ยวข้อง
            SetTrailStartWidth(_idleStartWidth);
            _lastDashingState = false;

            // กัน jump-cut ทำให้แรงโน้มถ่วงแรงขึ้นในทันที
            _endedJumpEarly = false;
            _coyoteUsable = false;
            
        }
        

        private void UpdateDirectionLine()
        {
            if (_directionLine == null) return;
            if (Camera.main == null) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = transform.position.z;

            Vector2 origin = transform.position;
            Vector2 dir = ((Vector2)mouseWorld - origin).normalized;

            _directionLine.positionCount = 2;
            _directionLine.SetPosition(0, origin);
            _directionLine.SetPosition(1, origin + dir * _directionLineLength);
        }

        #endregion

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

            if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;

            // Trigger jump animation
            if (_anim != null) _anim.SetTrigger(HashJump);

            Jumped?.Invoke(); // ✅ เรียกครั้งเดียว
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (GravitySuspended) return;

            bool applyGlide = _isGliding && !_grounded && !_isDashing;

            // on ground: กดไว้ที่ค่า grounding แล้วจบ
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
                return;
            }

            float fallAcc = _stats.FallAcceleration;
            float maxFall = _stats.MaxFallSpeed;

            if (_endedJumpEarly && _frameVelocity.y > 0)
                fallAcc *= _stats.JumpEndEarlyGravityModifier;

            if (applyGlide)
            {
                fallAcc *= Mathf.Clamp01(_glideGravityMultiplier);       // < 1 = ตกช้าลง
                maxFall *= Mathf.Clamp01(_glideMaxFallSpeedMultiplier);  // < 1 = เพดานตกช้าลง
            }

            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -maxFall, fallAcc * Time.fixedDeltaTime);
        }



        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}
