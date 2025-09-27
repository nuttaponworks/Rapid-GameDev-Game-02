using Unity.VisualScripting;
using UnityEngine;


public class PlayerStat : MonoBehaviour
{
    [SerializeField] private float playerHP = 100f;
    public bool PlayerIsDead = false;
    
    
    public void TakeDamage(float damage)
    {
        playerHP -= damage;
        Debug.Log(playerHP);

        CameraShake.instance.TriggerShake();
        GameStateManager.Instance.TriggerHurt();
        
        if (playerHP <= 0)
        {
            Debug.Log("You Lose!");
            Time.timeScale = 0;
            PlayerIsDead = true;
            GameStateManager.Instance.ChangeState(GameState.End);
            
            //Destroy(gameObject);
        }
    }
}
