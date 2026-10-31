using System;
using System.Collections.Generic;

public sealed class PresentationEventQueue
{
    private readonly Queue<PresentationEvent> _events = new Queue<PresentationEvent>();

    public int Count => _events.Count;
    public bool IsEmpty => _events.Count == 0;

    public void EnqueueBatch(IReadOnlyList<PresentationEvent> events)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        for (int i = 0; i < events.Count; i++)
        {
            if (events[i] == null)
            {
                throw new ArgumentException("Queue batches must not contain null events.", nameof(events));
            }
        }

        for (int i = 0; i < events.Count; i++)
        {
            _events.Enqueue(events[i]);
        }
    }

    public bool TryPeek(out PresentationEvent presentationEvent)
    {
        if (_events.Count == 0)
        {
            presentationEvent = null;
            return false;
        }

        presentationEvent = _events.Peek();
        return true;
    }

    public bool TryDequeue(out PresentationEvent presentationEvent)
    {
        if (_events.Count == 0)
        {
            presentationEvent = null;
            return false;
        }

        presentationEvent = _events.Dequeue();
        return true;
    }

    public bool HasNextForSnapshot(string snapshotId)
    {
        return _events.Count > 0 && _events.Peek().SnapshotId == snapshotId;
    }
}
