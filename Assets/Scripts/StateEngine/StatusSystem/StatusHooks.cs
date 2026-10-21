using CardRogue.StateEngine.Pipeline;

namespace CardRogue.StateEngine.StatusSystem
{
    public interface IOnTurnStart
    {
        void OnTurnStart(StateEngine engine, IStateEntity owner, StatusInstance status);
    }

    public interface IOnTurnEnd
    {
        void OnTurnEnd(StateEngine engine, IStateEntity owner, StatusInstance status);
    }

    public interface IOnDamagePipeline
    {
        void OnDamagePipeline(
            StateEngine engine,
            StatusInstance status,
            EffectContext context,
            EffectPhase phase);
    }
}
