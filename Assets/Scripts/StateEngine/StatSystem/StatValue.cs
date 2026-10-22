using System;
using System.Collections.Generic;

namespace CardRogue.StateEngine.StatSystem
{
    public sealed class StatValue
    {
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        public float BaseValue { get; set; }
        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        public StatValue(float baseValue)
        {
            BaseValue = baseValue;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            _modifiers.Add(modifier);
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            return _modifiers.Remove(modifier);
        }

        public float CalculateValue()
        {
            return StatModifierCalculator.Calculate(BaseValue, _modifiers);
        }
    }
}
