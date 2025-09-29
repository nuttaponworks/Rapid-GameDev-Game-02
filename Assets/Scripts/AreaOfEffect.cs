using System;
using System.Collections;
using UnityEngine;

public class AreaOfEffect : MonoBehaviour
{
    [SerializeField] private GameObject parentObject;
    [SerializeField] private Animator anim;

    [Header("Attack settings")]
    [SerializeField] private GameObject attackParticle;
    [SerializeField] private float particleDamageDelay = 0.1f;

    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private int damageHit = 1;

    public int attackFx=28;
    [Header("Repeating Damage")]
    [Tooltip("ถ้าเปิด จะทำดาเมจซ้ำทุก interval จนหมด duration")]
    [SerializeField] private bool isRepeatingDamage = false;
    [SerializeField] private float repeatingDamageInterval = 0.5f;
    [SerializeField] private float repeatingDamageDuration = 2f;

    private PlayerStat currentPlayer;
    private Coroutine _repeatCo;

    private void Start()
    {
        AudioManager.instance.PlaySFX(22);
        anim.speed *= speedMultiplier;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStat player = other.GetComponent<PlayerStat>();
        if (player != null) currentPlayer = player;
        // Debug.Log("Player Enter");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerStat player = other.GetComponent<PlayerStat>();
        if (player != null) currentPlayer = null;
        // Debug.Log("Player Exit");
    }

    public void TriggerAttack()
    {
        StartCoroutine(PlaySFXDelay());
        // VFX
        if (attackParticle != null)
            Instantiate(attackParticle, transform.position, parentObject != null ? parentObject.transform.rotation : Quaternion.identity);

        if (isRepeatingDamage)
        {
            if (_repeatCo != null) StopCoroutine(_repeatCo);
            _repeatCo = StartCoroutine(RepeatingDamageRoutine());
        }
        else
        {
            if (currentPlayer == null) return;

            if (particleDamageDelay > 0f)
            {
                StartCoroutine(SingleHitDelay());
            }
            else
            {
                currentPlayer.TakeDamage(damageHit);
            }
        }

    }

    IEnumerator PlaySFXDelay()
    {
        yield return new WaitForSeconds(particleDamageDelay);
        AudioManager.instance.PlaySFX(attackFx);
    }
    private IEnumerator SingleHitDelay()
    {
        yield return new WaitForSeconds(particleDamageDelay);
        if (currentPlayer != null) currentPlayer.TakeDamage(damageHit);
    }

    private IEnumerator RepeatingDamageRoutine()
    {
        // ดีเลย์ก่อนไล่ยิงรอบแรก (ถ้ามี)
        if (particleDamageDelay > 0f)
            yield return new WaitForSeconds(particleDamageDelay);

        float interval = Mathf.Max(0.01f, repeatingDamageInterval);
        float duration = Mathf.Max(0f, repeatingDamageDuration);

        float elapsed = 0f;
        // ยิงครั้งแรกทันทีเมื่อเข้า loop (หลัง delay)
        while (elapsed <= duration)
        {
            if (currentPlayer != null)
                currentPlayer.TakeDamage(damageHit);

            // รอ interval แล้วเดินเวลา
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        _repeatCo = null;
    }

    public void TriggerDestroy()
    {
        if (_repeatCo != null) StopCoroutine(_repeatCo);
        _repeatCo = null;
        if (parentObject != null) Destroy(parentObject.gameObject);
        else Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (_repeatCo != null) StopCoroutine(_repeatCo);
        _repeatCo = null;
    }
}
