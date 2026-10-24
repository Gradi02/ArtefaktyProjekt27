using System;

public sealed class FightEndedEventArgs : EventArgs
{
    public bool WasWon { get; }
    public LivingMixin DefeatedEntity { get; }

    public FightEndedEventArgs(bool wasWon, LivingMixin defeatedEntity)
    {
        WasWon = wasWon;
        DefeatedEntity = defeatedEntity;
    }
}
