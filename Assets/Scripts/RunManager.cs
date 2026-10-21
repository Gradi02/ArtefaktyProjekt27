using CardRogue.StateEngine;
using UnityEngine;

public class RunManager
{
    private Player _selectedCharacter;
    private FightManager _fightManager;
    private StateEngine _stateEngine;

    public RunManager(Player selectedCharacter, StateEngine stateEngine)
    {
        _selectedCharacter = selectedCharacter;
        _stateEngine = stateEngine;
    }

    public void StartFight()
    {
        // logic to start the fight with the selected character
        _fightManager = new FightManager(_selectedCharacter, _stateEngine);

        // add SO for fight settings and pass them to the FightManager (difficulty, enemy type, etc.)
    }
    // current run stats and methods will go here
}