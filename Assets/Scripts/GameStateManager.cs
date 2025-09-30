using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
    [Header("UI")]
    [SerializeField] GameObject gameWinPanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject warmUpPanel;
    [SerializeField] GameObject hurtPanel;
    [SerializeField] GameObject bossStatPanel;
    [SerializeField] GameObject playerStatPanel;
    private Coroutine hurtCoroutine;
    [Space]
    public PlayerStat playerStat;
    public BossController bossController;
    public int playerDamage = 5;
    public GameState currentState { get; private set; }

    public GameObject currentBossPrefab;
    public event Action<GameState> OnStateChanged;
    public GameObject bossPrefab;
    
    public static GameStateManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ChangeState(GameState.Warmup);
        
        
        warmUpPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        gameWinPanel.SetActive(false);
        bossStatPanel.SetActive(false);
        playerStatPanel.SetActive(false);
        Time.timeScale = 1;
    }

    public void StartGameProcess()
    {
        ChangeState(GameState.Process);
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
            warmUpPanel.SetActive(false);
            
            bossStatPanel.SetActive(true);
            playerStatPanel.SetActive(true);
        }
        
        if (newState == GameState.End)
        {
            if (playerStat.playerIsDead)
            {
                gameOverPanel.SetActive(true);
            }
            else if (bossController.bossIsDead)
            {
                gameWinPanel.SetActive(true);
            }
            bossStatPanel.SetActive(false);
            playerStatPanel.SetActive(false);
            Time.timeScale = 0f;
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

    public void ReturnToMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TriggerHurt()
    {
        if(hurtCoroutine!=null) StopCoroutine(hurtCoroutine);
        hurtCoroutine= StartCoroutine(HurtCoroutine());
    }

    IEnumerator HurtCoroutine()
    {
        hurtPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        hurtPanel.SetActive(false);
    }

    public void OpenVolumeUI()
    {
        AudioManager.instance.OpenVolumeUI();
    }
}
