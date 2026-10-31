using System;
using System.Collections.Generic;
using ANTK.Game.StateEngine.Actions;

public interface IActionPresentationRule
{
    bool TryProject(
        GameActionSnapshot snapshot,
        int actionIndex,
        out PresentationEvent presentationEvent,
        out int consumedActionCount);
}

public sealed class PresentationDirector
{
    private readonly PresentationEventQueue _queue;
    private readonly List<IActionPresentationRule> _rules = new List<IActionPresentationRule>();
    private readonly HashSet<string> _processedSnapshotIds = new HashSet<string>();

    public PresentationDirector(
        PresentationEventQueue queue,
        IEnumerable<IActionPresentationRule> rules = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        if (rules != null)
        {
            foreach (IActionPresentationRule rule in rules)
            {
                if (rule == null)
                {
                    throw new ArgumentException("Presentation rules must not contain null values.", nameof(rules));
                }

                _rules.Add(rule);
            }
        }

        // Always include the default single-action rule to ensure all actions can be projected.
        // If you want to add new rules, add them in BattleSceneController
        _rules.Add(new SingleActionPresentationRule());
    }

    public IReadOnlyList<PresentationEvent> ProcessSnapshot(GameActionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }
        if (_processedSnapshotIds.Contains(snapshot.SnapshotId))
        {
            throw new InvalidOperationException(
                $"Snapshot '{snapshot.SnapshotId}' has already been processed.");
        }

        List<PresentationEvent> projectedEvents = new List<PresentationEvent>();
        int actionIndex = 0;

        while (actionIndex < snapshot.Actions.Count)
        {
            PresentationEvent presentationEvent = null;
            int consumedActionCount = 0;

            foreach (IActionPresentationRule rule in _rules)
            {
                if (!rule.TryProject(snapshot, actionIndex, out presentationEvent, out consumedActionCount))
                {
                    continue;
                }

                if (presentationEvent == null ||
                    presentationEvent.SnapshotId != snapshot.SnapshotId ||
                    consumedActionCount <= 0 ||
                    actionIndex + consumedActionCount > snapshot.Actions.Count)
                {
                    throw new InvalidOperationException(
                        $"A Director rule returned an invalid projection for snapshot '{snapshot.SnapshotId}'.");
                }

                break;
            }

            if (presentationEvent == null)
            {
                throw new InvalidOperationException(
                    $"No Director rule projected action {actionIndex} in snapshot '{snapshot.SnapshotId}'.");
            }

            projectedEvents.Add(presentationEvent);
            actionIndex += consumedActionCount;
        }

        _processedSnapshotIds.Add(snapshot.SnapshotId);
        _queue.EnqueueBatch(projectedEvents);
        return projectedEvents.AsReadOnly();
    }

    private sealed class SingleActionPresentationRule : IActionPresentationRule
    {
        public bool TryProject(
            GameActionSnapshot snapshot,
            int actionIndex,
            out PresentationEvent presentationEvent,
            out int consumedActionCount)
        {
            GameAction action = snapshot.Actions[actionIndex];
            presentationEvent = new PresentationEvent(
                snapshot.SnapshotId,
                action.TypeId,
                new[] { action });
            consumedActionCount = 1;
            return true;
        }
    }
}

public sealed class ConsecutiveCardDrawsRule : IActionPresentationRule
{
    public bool TryProject(
        GameActionSnapshot snapshot,
        int actionIndex,
        out PresentationEvent presentationEvent,
        out int consumedActionCount)
    {
        CardDrawnAction firstDraw = snapshot.Actions[actionIndex] as CardDrawnAction;
        if (firstDraw == null)
        {
            presentationEvent = null;
            consumedActionCount = 0;
            return false;
        }

        List<GameAction> groupedActions = new List<GameAction> { firstDraw };
        int nextIndex = actionIndex + 1;
        while (nextIndex < snapshot.Actions.Count)
        {
            CardDrawnAction nextDraw = snapshot.Actions[nextIndex] as CardDrawnAction;
            if (nextDraw == null || !CanMerge(firstDraw, nextDraw))
            {
                break;
            }

            groupedActions.Add(nextDraw);
            nextIndex++;
        }

        presentationEvent = new PresentationEvent(
            snapshot.SnapshotId,
            "cards.drawn",
            groupedActions);
        consumedActionCount = groupedActions.Count;
        return true;
    }

    private static bool CanMerge(CardDrawnAction first, CardDrawnAction next)
    {
        return first.RecipientEntityId == next.RecipientEntityId &&
            first.SourceZoneId == next.SourceZoneId &&
            first.DestinationZoneId == next.DestinationZoneId &&
            first.ReasonId == next.ReasonId;
    }
}
