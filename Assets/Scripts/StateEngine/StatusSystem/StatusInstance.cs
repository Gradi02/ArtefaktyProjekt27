using System;

namespace ANTK.Game.StateEngine.StatusSystem
{
    public sealed class StatusInstance
    {
        public StatusDefinition Definition { get; }
        public int Stacks { get; private set; }
        public int DurationTurns { get; private set; }
        public bool IsPermanent => DurationTurns < 0;

        public StatusInstance(StatusDefinition definition, int stacks, int durationTurns)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (stacks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stacks), "Stacks must be positive.");
            }
            if (durationTurns < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationTurns),
                    "Duration must be -1 (permanent) or a non-negative number of turns.");
            }

            Definition = definition;
            Stacks = stacks;
            DurationTurns = durationTurns;
        }

        public void AddStacks(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Added stacks must be positive.");
            }

            Stacks += amount;
        }

        public void RefreshDuration(int durationTurns)
        {
            if (durationTurns < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationTurns),
                    "Duration must be -1 (permanent) or a non-negative number of turns.");
            }

            DurationTurns = durationTurns;
        }

        internal bool TickDuration()
        {
            if (IsPermanent)
            {
                return false;
            }

            if (DurationTurns > 0)
            {
                DurationTurns--;
            }

            return DurationTurns == 0;
        }
    }
}
