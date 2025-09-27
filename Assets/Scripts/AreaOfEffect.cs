using System;
using System.Collections;
using UnityEngine;

public class AreaOfEffect : MonoBehaviour
{
    [SerializeField] private GameObject parentObject;
    [SerializeField] private Animator anim;

    [Header("Attack settings")] [SerializeField]
    private GameObject attackParticle;

    [SerializeField] private float particleDamageDelay = 0.1f;

    [SerializeField] private float damage, speedMultiplier = 1f;

    private PlayerStat currentPlayer;

    private void Start()
    {
        anim.speed *= speedMultiplier;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStat player = other.gameObject.GetComponent<PlayerStat>();
        if (player != null) currentPlayer = player;

        Debug.Log("Player Enter");
    }

    // private void OnTriggerStay2D(Collider2D other)
    // {
    //     
    //     PlayerStat player = other.gameObject.GetComponent<PlayerStat>();
    //     if (player != null) currentPlayer = player;
    //     
    //     
    //     Debug.Log("Player Stay");
    // }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerStat player = other.gameObject.GetComponent<PlayerStat>();
        if (player != null) currentPlayer = null;


        Debug.Log("Player Exit");
    }

    public void TriggerAttack()
    {
        Instantiate(attackParticle, this.transform.position, parentObject.transform.rotation);
        if (currentPlayer != null)
        {
            
            if (particleDamageDelay > 0)
                StartCoroutine(AttackDelay());
            else currentPlayer.TakeDamage(damage);
        }
    }

    IEnumerator AttackDelay()
    {
        Debug.Log($"Attack delayed by {particleDamageDelay} sec");
        yield return new WaitForSeconds(particleDamageDelay);
        Debug.Log($"Deal {damage} to the player delayed");
        if (currentPlayer != null) currentPlayer.TakeDamage(damage);
    }

    public void TriggerDestroy()
    {
        Destroy(parentObject.gameObject);
    }
}
