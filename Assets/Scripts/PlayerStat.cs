using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    [SerializeField] private float playerHP = 100f;
    
    public void takeDamage(float damage)
    {
        playerHP -= damage;
        Debug.Log(playerHP);

        if (playerHP <= 0)
        {
            Debug.Log("You Lose!");
            GameStateManager.Instance.ChangeState(GameState.End);
            Destroy(gameObject);
        }
    }
}
