using System;
using System.Collections.Generic;
using ANTK.Game.StateEngine.StatSystem;
using ANTK.Game.StateEngine.StatusSystem;

namespace ANTK.Game.StateEngine.Pipeline
{
    public sealed class EffectPipeline
    {
        public EffectContext ProcessDamage(
            StateEngine engine,
            IStateEntity source,
            IStateEntity target,
            float baseDamage)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            EffectContext context = new EffectContext(source, target, baseDamage);

            // Each phase can adjust the context before the final value is applied.
            RunPhase(engine, context, EffectPhase.PreCalculate);
            RunPhase(engine, context, EffectPhase.Calculate);
            Calculate(context);

            RunPhase(engine, context, EffectPhase.Apply);
            context.FinalValue = (float)Math.Min(
                target.Health,
                Math.Ceiling(Math.Max(0f, context.FinalValue)));
            target.ApplyDamage((int)context.FinalValue);

            RunPhase(engine, context, EffectPhase.PostApply);
            return context;
        }

        private static void Calculate(EffectContext context)
        {
            context.FinalValue = StatModifierCalculator.Calculate(
                context.BaseValue,
                context.ModifiersToProcess,
                context.RecordProcessedModifier);
        }

        private static void RunPhase(
            StateEngine engine,
            EffectContext context,
            EffectPhase phase)
        {
            InvokeStatusHooks(engine, context.Source, context, phase);

            if (!ReferenceEquals(context.Source, context.Target))
            {
                InvokeStatusHooks(engine, context.Target, context, phase);
            }
        }

        private static void InvokeStatusHooks(
            StateEngine engine,
            IStateEntity owner,
            EffectContext context,
            EffectPhase phase)
        {
            List<StatusInstance> snapshot = new List<StatusInstance>(owner.ActiveStatuses);

            foreach (StatusInstance status in snapshot)
            {
                if (!owner.HasStatus(status))
                {
                    continue;
                }

                IOnDamagePipeline hook = status.Definition as IOnDamagePipeline;
                if (hook != null)
                {
                    hook.OnDamagePipeline(engine, status, context, phase);
                }
            }
        }
    }
}
