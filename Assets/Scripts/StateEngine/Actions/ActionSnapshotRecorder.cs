using System;
using System.Collections.Generic;

namespace ANTK.Game.StateEngine.Actions
{
    public sealed class ActionSnapshotRecorder
    {
        private List<GameAction> _actions;
        private string _commandTypeId;
        private string _commandId;

        public bool IsRecording => _actions != null;

        public void BeginCommand(string commandTypeId)
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("An action command is already being recorded.");
            }
            if (string.IsNullOrWhiteSpace(commandTypeId))
            {
                throw new ArgumentException("Command type ID must not be empty.", nameof(commandTypeId));
            }

            _commandTypeId = commandTypeId;
            _commandId = Guid.NewGuid().ToString("N");
            _actions = new List<GameAction>();
        }

        public void Record(GameAction action)
        {
            if (!IsRecording)
            {
                throw new InvalidOperationException("Actions can only be recorded inside a command.");
            }
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _actions.Add(action);
        }

        public GameActionSnapshot CompleteCommand()
        {
            if (!IsRecording)
            {
                throw new InvalidOperationException("There is no action command to complete.");
            }

            List<GameAction> actions = _actions;
            string commandId = _commandId;
            string commandTypeId = _commandTypeId;
            _actions = null;
            _commandId = null;
            _commandTypeId = null;

            if (actions.Count == 0)
            {
                return null;
            }

            return new GameActionSnapshot(commandId, commandId, commandTypeId, actions);
        }
    }
}
