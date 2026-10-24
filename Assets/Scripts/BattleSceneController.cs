using UnityEngine;

public sealed class BattleSceneController : MonoBehaviour
{
    private RunManager _runManager;

    private void Start()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.runManager == null)
        {
            Debug.LogError("Battle scene loaded without an active RunManager.", this);
            return;
        }

        if (gameManager.runManager.RegisterBattleSceneController(this))
        {
            _runManager = gameManager.runManager;
        }
    }

    [ContextMenu("Test/Win Fight")]
    public void WinFight()
    {
        EndFight(wasWon: true);
    }

    [ContextMenu("Test/Lose Fight")]
    public void LoseFight()
    {
        EndFight(wasWon: false);
    }

    private void EndFight(bool wasWon)
    {
        if (_runManager == null)
        {
            Debug.LogError("Battle scene controller has not been connected to a RunManager.", this);
            return;
        }

        _runManager.EndFight(this, wasWon);
    }
}
