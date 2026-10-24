using CardRogue.StateEngine;
using UnityEngine;

public class RunManager
{
    private readonly Player _selectedCharacter;
    private readonly bool _hasSelectedCharacter;
    private readonly StateEngine _stateEngine;
    private BattleSceneController _battleSceneController;
    private bool _isEndingFight;

    public Player SelectedCharacter => _selectedCharacter;
    public bool HasSelectedCharacter => _hasSelectedCharacter;
    public StateEngine StateEngine => _stateEngine;
    public bool IsFightInProgress { get; private set; }
    public bool? LastFightWasWon { get; private set; }

    public RunManager(Player selectedCharacter, StateEngine stateEngine)
    {
        _selectedCharacter = selectedCharacter;
        _hasSelectedCharacter = selectedCharacter != null;
        _stateEngine = stateEngine;
    }

    public void StartRun()
    {
        if (IsFightInProgress)
        {
            Debug.LogWarning("Cannot start a run while a fight is in progress.");
            return;
        }

        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager == null)
        {
            Debug.LogError("Cannot start a run because SceneTransitionManager is unavailable.");
            return;
        }

        if (!transitionManager.LoadSceneLocalWithAdditiveScene(
                GameScene.GAME,
                GameScene.MAP,
                "Entering run..."))
        {
            Debug.LogError("The game and map scenes were not loaded.");
        }
    }

    public void StartFight()
    {
        if (IsFightInProgress)
        {
            Debug.LogWarning("A fight is already in progress.");
            return;
        }

        if (_hasSelectedCharacter && _selectedCharacter == null)
        {
            Debug.LogError(
                "The selected player was destroyed before the fight. " +
                "Keep the player in a loaded scene or create it from persistent run data.");
            return;
        }

        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager == null)
        {
            Debug.LogError("Cannot start a fight because SceneTransitionManager is unavailable.");
            return;
        }

        IsFightInProgress = true;
        LastFightWasWon = null;

        if (!transitionManager.ReplaceSceneAdditive(
                GameScene.MAP,
                GameScene.BATTLESCENE,
                message: "Entering battle..."))
        {
            IsFightInProgress = false;
            Debug.LogError("A battle scene was not loaded.");
        }
    }

    public bool RegisterBattleSceneController(BattleSceneController controller)
    {
        if (!IsFightInProgress)
        {
            Debug.LogError("A battle scene was loaded without an active fight.");
            return false;
        }
        if (_battleSceneController != null && _battleSceneController != controller)
        {
            Debug.LogError("A different battle scene controller is already registered.");
            return false;
        }

        _battleSceneController = controller;
        return true;
    }

    public bool EndFight(BattleSceneController controller, bool wasWon)
    {
        if (!IsFightInProgress || _isEndingFight || controller != _battleSceneController)
        {
            Debug.LogWarning("This battle scene cannot end the current fight.");
            return false;
        }

        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager == null)
        {
            Debug.LogError("Cannot end the fight because SceneTransitionManager is unavailable.");
            return false;
        }

        _isEndingFight = true;
        bool started = transitionManager.ReplaceSceneAdditive(
            GameScene.BATTLESCENE,
            GameScene.MAP,
            onReplaced: () =>
            {
                LastFightWasWon = wasWon;
                IsFightInProgress = false;
                _isEndingFight = false;
                _battleSceneController = null;
                Debug.Log(wasWon ? "Fight ended: win." : "Fight ended: lose.");
            },
            message: wasWon ? "Victory!" : "Defeat!");

        if (!started)
        {
            _isEndingFight = false;
        }

        return started;
    }

    // current run stats and methods will go here
}