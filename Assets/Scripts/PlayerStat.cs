using Unity.VisualScripting;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    public event System.Action<int> HpChanged;
    private int _playerHP = 5;
    public int playerHP
    {
        get => _playerHP;
        set { if (_playerHP != value) { _playerHP = value; HpChanged?.Invoke(_playerHP); } }
    }

    public bool playerIsDead = false;

    [Header("I-Frame")]
    [SerializeField] private float iFrameDuration = 2f;      // เวลาล่องหนหลังโดนตี
    [SerializeField] private float blinkFrequency = 12f;     // Hz กระพริบ
    [SerializeField] private float blinkMinAlpha = 0.3f;     // ความโปร่งใสตอนจาง
    [SerializeField] private SpriteRenderer[] spriteRenderers; // ถ้าเว้นว่าง จะ auto-find ทั้งลูกหลาน

    private bool _invulnerable;
    private Coroutine _iFrameCo;

    private void Awake()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void TakeDamage(int damage = 1)
    {
        // กันดาเมจระหว่าง i-frame
        if (_invulnerable || playerIsDead) return;

        playerHP -= damage;
        if (playerHP < 0) playerHP = 0;

        Debug.Log(playerHP);

        CameraShake.instance?.TriggerShake();
        GameStateManager.Instance?.TriggerHurt();

        // เริ่ม I-frames (กระพริบ + กันดาเมจ)
        if (_iFrameCo != null) StopCoroutine(_iFrameCo);
        _iFrameCo = StartCoroutine(IFrameBlink(iFrameDuration));

        if (playerHP <= 0)
        {
            Debug.Log("You Lose!");
            Time.timeScale = 0;
            playerIsDead = true;
            GameStateManager.Instance?.ChangeState(GameState.End);
        }
    }

    private System.Collections.IEnumerator IFrameBlink(float duration)
    {
        _invulnerable = true;

        float t = 0f;
        // backup สีเดิม
        Color[] original = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i]) original[i] = spriteRenderers[i].color;

        while (t < duration)
        {
            t += Time.deltaTime;
            // สร้างอัลฟาแบบกระพริบด้วย sine 0..1
            float s = (Mathf.Sin(2f * Mathf.PI * blinkFrequency * t) * 0.5f) + 0.5f;
            float a = Mathf.Lerp(blinkMinAlpha, 1f, s);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (!spriteRenderers[i]) continue;
                var c = spriteRenderers[i].color;
                c.a = a;
                spriteRenderers[i].color = c;
            }

            yield return null;
        }

        // คืนอัลฟาเดิมทั้งหมด
        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i]) spriteRenderers[i].color = original[i];

        _invulnerable = false;
        _iFrameCo = null;
    }
}
