using UnityEngine;

public class GameStateTest : MonoBehaviour
{
    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            GameStateManager.Instance.ChangeState(GameState.Warmup);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            GameStateManager.Instance.ChangeState(GameState.Start);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            GameStateManager.Instance.ChangeState(GameState.Process);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            GameStateManager.Instance.ChangeState(GameState.End);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            GameStateManager.Instance.ChangeState(GameState.Summary);
    }
}
