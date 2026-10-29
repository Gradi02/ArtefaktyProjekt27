using System.Collections.Generic;
using ANTK.Game.StateEngine.StatusSystem;

namespace ANTK.Game.StateEngine
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
