using CardRogue.StateEngine;
using UnityEngine;

public class FightManager
{
    private Player _selectedCharacter;
    private StateEngine _stateEngine;

    public FightManager(Player selectedCharacter, StateEngine stateEngine)
    {
        _selectedCharacter = selectedCharacter;
        _stateEngine = stateEngine;
    }
}
