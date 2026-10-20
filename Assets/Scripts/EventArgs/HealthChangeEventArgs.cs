using System;

public class HealthChangeEventArgs : EventArgs
{
    public int previousValue { get; }
    public int currentValue { get; }
    public int delta => currentValue - previousValue;
        
    public HealthChangeEventArgs(int previous, int current)
    {
        previousValue = previous;
        currentValue = current;
    }
}