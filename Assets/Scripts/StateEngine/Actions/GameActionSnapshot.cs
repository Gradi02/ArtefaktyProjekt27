using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ANTK.Game.StateEngine.Actions
{
    public sealed class GameActionSnapshot
    {
        private readonly ReadOnlyCollection<GameAction> _actions;

        public string SnapshotId { get; }
        public string CommandId { get; }
        public string CommandTypeId { get; }
        public IReadOnlyList<GameAction> Actions => _actions;

        public GameActionSnapshot(
            string snapshotId,
            string commandId,
            string commandTypeId,
            IEnumerable<GameAction> actions)
        {
            if (string.IsNullOrWhiteSpace(snapshotId))
            {
                throw new ArgumentException("Snapshot ID must not be empty.", nameof(snapshotId));
            }
            if (string.IsNullOrWhiteSpace(commandId))
            {
                throw new ArgumentException("Command ID must not be empty.", nameof(commandId));
            }
            if (string.IsNullOrWhiteSpace(commandTypeId))
            {
                throw new ArgumentException("Command type ID must not be empty.", nameof(commandTypeId));
            }
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            List<GameAction> copiedActions = new List<GameAction>(actions);
            if (copiedActions.Count == 0)
            {
                throw new ArgumentException("A snapshot must contain at least one executed action.", nameof(actions));
            }
            if (copiedActions.Contains(null))
            {
                throw new ArgumentException("Snapshot actions must not contain null values.", nameof(actions));
            }

            SnapshotId = snapshotId;
            CommandId = commandId;
            CommandTypeId = commandTypeId;
            _actions = copiedActions.AsReadOnly();
        }
    }
}
