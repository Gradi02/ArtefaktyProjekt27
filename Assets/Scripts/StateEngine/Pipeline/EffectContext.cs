using System;
using System.Collections.Generic;
using CardRogue.StateEngine.StatSystem;

namespace CardRogue.StateEngine.Pipeline
{
    public enum EffectPhase
    {
        PreCalculate,
        Calculate,
        Apply,
        PostApply
    }

    public sealed class EffectContext
    {
        private readonly List<StatModifier> _modifiersToProcess = new List<StatModifier>();
        private readonly List<StatModifier> _processedModifiers = new List<StatModifier>();

        public IStateEntity Source { get; }
        public IStateEntity Target { get; }
        public float BaseValue { get; set; }
        public float FinalValue { get; set; }
        public IReadOnlyList<StatModifier> ProcessedModifiers => _processedModifiers;

        public EffectContext(IStateEntity source, IStateEntity target, float baseValue)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Source = source;
            Target = target;
            BaseValue = baseValue;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            _modifiersToProcess.Add(modifier);
        }

        internal void RecordProcessedModifier(StatModifier modifier)
        {
            _processedModifiers.Add(modifier);
        }

        internal IReadOnlyList<StatModifier> ModifiersToProcess => _modifiersToProcess;
    }
}
