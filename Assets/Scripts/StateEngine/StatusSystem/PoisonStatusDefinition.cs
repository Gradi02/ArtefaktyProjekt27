using ANTK.Game.StateEngine.Pipeline;
using UnityEngine;

namespace ANTK.Game.StateEngine.StatusSystem
{
    public sealed class PoisonStatusDefinition : StatusDefinition, IOnTurnEnd
    {
        [SerializeField, Min(1)] private int damagePerStack = 1;
        [SerializeField, Min(1)] private int durationTurns = 2;

        public int DurationTurns => Mathf.Max(1, durationTurns);

        public void OnTurnEnd(StateEngine engine, IStateEntity owner, StatusInstance status)
        {
            int damage = damagePerStack * status.Stacks;
            engine.ProcessDamage(owner, owner, damage);
            Debug.Log($"Poison deals {damage} damage to its owner.");
        }
    }
}
