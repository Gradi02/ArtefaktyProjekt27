using System.Collections.Generic;
using CardRogue.StateEngine.StatusSystem;

namespace CardRogue.StateEngine
{
    public interface IStateEntity
    {
        int Health { get; }
        IReadOnlyList<StatusInstance> ActiveStatuses { get; }

        void ApplyDamage(int amount);
        void AddStatus(StatusInstance status);
        bool RemoveStatus(StatusInstance status);
        bool HasStatus(StatusInstance status);
    }
}
