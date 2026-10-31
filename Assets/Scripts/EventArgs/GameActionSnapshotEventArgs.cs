using System;
using ANTK.Game.StateEngine.Actions;

public sealed class GameActionSnapshotEventArgs : EventArgs
{
    public GameActionSnapshot Snapshot { get; }

    public GameActionSnapshotEventArgs(GameActionSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }
}
