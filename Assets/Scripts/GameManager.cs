using ANTK.Game.StateEngine;
using ANTK.Game.StateEngine.Pipeline;
using UnityEngine;

// main class for managing the game state and flow
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public RunManager runManager { get; private set; }
    [SerializeField] private CardDefinition _starterAttackCardDefinition;
    private StateEngine _stateEngine;
    private EffectPipeline pipeline;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void StartGameInitialization()
    {
        pipeline = new EffectPipeline();
        _stateEngine = new StateEngine(pipeline);

        SceneTransitionManager.Instance.LoadSceneLocal(GameScene.MENU, "Welcome!");
    }

    
    public void StartRun()
    {
        StartRun(null);
    }

    public void StartRun(Player selectedCharacter)
    {
        if (!_starterAttackCardDefinition)
        {
            Debug.LogError("GameManager requires a starter attack card definition.", this);
            return;
        }
        if (_stateEngine == null)
        {
            Debug.LogError("Game state must be initialized before starting a run.", this);
            return;
        }

        runManager = new RunManager(selectedCharacter, _stateEngine, _starterAttackCardDefinition);
        runManager.StartRun();
    }

    [ContextMenu("End Run")]
    public void EndRun()
    {
        runManager = null;
        SceneTransitionManager.Instance.LoadSceneLocal(GameScene.MENU, "Game Over!");
    }
}
