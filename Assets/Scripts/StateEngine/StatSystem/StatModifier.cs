using System;
using System.Collections.Generic;

namespace CardRogue.StateEngine.StatSystem
{
    public enum StatModifierType
    {
        Flat,
        PercentAdd
    }

    public sealed class StatModifier
    {
        public float Value { get; }
        public StatModifierType Type { get; }
        public string Source { get; }

        public StatModifier(float value, StatModifierType type, string source)
        {
            if (type != StatModifierType.Flat && type != StatModifierType.PercentAdd)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported stat modifier type.");
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("A modifier source is required.", nameof(source));
            }

            Value = value;
            Type = type;
            Source = source;
        }
    }

    internal static class StatModifierCalculator
    {
        public static float Calculate(
            float baseValue,
            IEnumerable<StatModifier> modifiers,
            Action<StatModifier> onProcessed = null)
        {
            float flatTotal = 0f;
            float percentAddTotal = 0f;

            foreach (StatModifier modifier in modifiers)
            {
                onProcessed?.Invoke(modifier);

                switch (modifier.Type)
                {
                    case StatModifierType.Flat:
                        flatTotal += modifier.Value;
                        break;
                    case StatModifierType.PercentAdd:
                        percentAddTotal += modifier.Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(modifier.Type),
                            modifier.Type,
                            "Unsupported stat modifier type.");
                }
            }

            // Additive percentages are summed, then applied after flat modifiers.
            return (baseValue + flatTotal) * (1f + percentAddTotal);
        }
    }
}
