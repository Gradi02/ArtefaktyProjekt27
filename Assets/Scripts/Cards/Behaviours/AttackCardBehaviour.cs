using ANTK.Game.StateEngine;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack Card Behaviour", menuName = "ANTK/Cards/Attack Behaviour")]
public class AttackCardBehaviour : CardBehaviour
{
    public override void Perform(CardContext context)
    {
        if (context == null)
        {
            throw new System.ArgumentNullException(nameof(context));
        }

        context.StateEngine.ProcessDamage(
            (IStateEntity)context.Source,
            (IStateEntity)context.Target,
            context.Source.AttackPower.CalculateValue(),
            "card.attack");
    }
}