using CardRogue.StateEngine;
using CardRogue.StateEngine.Pipeline;
using UnityEngine;

// main class for managing the game state and flow
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private RunManager _runManager;
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
        // get selected player character
        Player selectedCharacter = null;

        _runManager = new RunManager(selectedCharacter, _stateEngine);
        SceneTransitionManager.Instance.LoadSceneLocal(GameScene.GAME, "RUN!");
    }

    [ContextMenu("End Run")]
    public void EndRun()
    {
        _runManager = null;
        SceneTransitionManager.Instance.LoadSceneLocal(GameScene.MENU, "Game Over!");
    }
}
