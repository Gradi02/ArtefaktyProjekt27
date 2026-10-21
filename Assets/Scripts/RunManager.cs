using UnityEngine;

public class RunManager
{
    private Player _selectedCharacter;
    private FightManager _fightManager;

    public RunManager(Player selectedCharacter)
    {
        _selectedCharacter = selectedCharacter;
    }

    public void StartFight()
    {
        // logic to start the fight with the selected character
        _fightManager = new FightManager(_selectedCharacter);

        // add SO for fight settings and pass them to the FightManager (difficulty, enemy type, etc.)
    }
    // current run stats and methods will go here
}