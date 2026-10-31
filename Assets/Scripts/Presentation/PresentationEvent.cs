using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ANTK.Game.StateEngine.Actions;

public sealed class PresentationEvent
{
    private readonly ReadOnlyCollection<GameAction> _actions;

    public string SnapshotId { get; }
    public string EventTypeId { get; }
    public IReadOnlyList<GameAction> Actions => _actions;

    public PresentationEvent(string snapshotId, string eventTypeId, IEnumerable<GameAction> actions)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            throw new ArgumentException("Snapshot ID must not be empty.", nameof(snapshotId));
        }
        if (string.IsNullOrWhiteSpace(eventTypeId))
        {
            throw new ArgumentException("Event type ID must not be empty.", nameof(eventTypeId));
        }
        if (actions == null)
        {
            throw new ArgumentNullException(nameof(actions));
        }

        List<GameAction> copiedActions = new List<GameAction>(actions);
        if (copiedActions.Count == 0 || copiedActions.Contains(null))
        {
            throw new ArgumentException("A presentation event must contain valid actions.", nameof(actions));
        }

        SnapshotId = snapshotId;
        EventTypeId = eventTypeId;
        _actions = copiedActions.AsReadOnly();
    }
}
