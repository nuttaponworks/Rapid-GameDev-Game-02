using System;
using UnityEngine;
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
    public static GameStateManager Instance;
    public GameState currentState { get; private set; }

    public event Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ChangeState(GameState.Warmup);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Game State: " + currentState);
        OnStateChanged?.Invoke(newState);

        if (newState == GameState.Summary)
        {
            Invoke(nameof(ReturnToWarmup), 3f);
        }
    }

    void ReturnToWarmup()
    {
        ChangeState(GameState.Warmup);
    }

}
