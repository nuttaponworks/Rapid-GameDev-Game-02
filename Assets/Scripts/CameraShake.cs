using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance { get; private set; }

    [Header("Shake Defaults")]
    [SerializeField] private float defaultDuration = 0.5f;
    [SerializeField] private float defaultAmplitude = 0.7f;
    [SerializeField] private float decreaseFactor = 1.0f; // ยิ่งมากยิ่งดับไว
    [SerializeField] private float frequency = 25f;       // ความถี่ความเปลี่ยนของ noise

    private Vector3 originLocalPos;
    private float timeLeft = 0f;
    private float amplitude = 0f;
    private float jitterTimer = 0f;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        originLocalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        // กันกรณี enable กลับมาแล้วยังค้าง offset
        transform.localPosition = originLocalPos;
    }

    private void LateUpdate()
    {
        if (timeLeft > 0f)
        {
            // อัพเดทความถี่ของการสั่น
            jitterTimer += Time.deltaTime * frequency;

            // ใช้ insideUnitCircle สำหรับ 2D และไม่ยุ่งกับ Z
            Vector2 rand = Random.insideUnitCircle;
            float falloff = Mathf.Clamp01(timeLeft / Mathf.Max(0.0001f, defaultDuration));
            Vector3 offset = new Vector3(rand.x, rand.y, 0f) * (amplitude * falloff);

            transform.localPosition = originLocalPos + offset;

            // ลดเวลา
            timeLeft -= Time.deltaTime * decreaseFactor;

            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                amplitude = 0f;
                transform.localPosition = originLocalPos;
            }
        }
        else
        {
            // คงตำแหน่งฐานไว้ (ไม่มีการสั่น)
            if (transform.localPosition != originLocalPos)
                transform.localPosition = originLocalPos;
        }
    }

    /// <summary>
    /// สั่งสั่น (ซ้อนทับแบบ "บวกต่อ" ถ้าเรียกซ้ำ)
    /// </summary>
    public void TriggerShake(float duration = -1f, float amp = -1f)
    {
        float d = duration > 0f ? duration : defaultDuration;
        float a = amp > 0f ? amp : defaultAmplitude;

        // ถ้าเรียกซ้ำ ให้ยืดเวลาหรือเพิ่มแอมพลิจูดขึ้น (ไม่รีเซ็ตให้หายฮวบ)
        timeLeft = Mathf.Max(timeLeft, d);
        amplitude = Mathf.Max(amplitude, a);
    }

    /// <summary>
    /// หยุดสั่นทันที
    /// </summary>
    public void StopShake()
    {
        timeLeft = 0f;
        amplitude = 0f;
        transform.localPosition = originLocalPos;
    }
}