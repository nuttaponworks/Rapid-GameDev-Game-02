using System;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum GameState
{
    Warmup,
    Start,
    Process,
    End,
    Summary
}
public class GameStateManager : MonoBehaviour
{
    public  GameObject gameWinPanel;
    public  GameObject gameOverPanel;
    public  GameObject WarmUpPanel;
    public static GameStateManager Instance;
    public PlayerStat playerStat;
    public BossController BossController;
    public GameState currentState { get; private set; }

    public GameObject currentBossPrefab;
    public event Action<GameState> OnStateChanged;
    public GameObject bossPrefab;
    

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ChangeState(GameState.Warmup);
        
        WarmUpPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        gameWinPanel.SetActive(false);
        Time.timeScale = 1;
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Game State: " + currentState);
        OnStateChanged?.Invoke(newState);
        
        if (newState == GameState.Process)
        {
            if (bossPrefab != null && currentBossPrefab == null)
                currentBossPrefab = Instantiate(bossPrefab, Vector3.zero, Quaternion.identity);
            gameOverPanel.SetActive(false);
            WarmUpPanel.SetActive(false);
        }
        
        if (newState == GameState.End)
        {
            if (playerStat.PlayerIsDead)
            {
                gameOverPanel.SetActive(true);
            }
            else if (BossController.bossIsDead)
            {
                gameWinPanel.SetActive(true);
            }
        }

        if (newState == GameState.Summary)
        {
            Invoke(nameof(ReturnToWarmup), 3f);
        }
    }

    void ReturnToWarmup()
    {
        ChangeState(GameState.Warmup);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
