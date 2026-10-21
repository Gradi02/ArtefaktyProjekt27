using CardRogue.StateEngine;
using UnityEngine;

// main class for managing the game state and flow
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private RunManager _runManager;
    private StateEngine _stateEngine;

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

        _stateEngine = new StateEngine();
    }

    [ContextMenu("Start Game")]
    public void StartRun()
    {
        // get selected player character
        Player selectedCharacter = null;

        _runManager = new RunManager(selectedCharacter, _stateEngine);
    }
}
