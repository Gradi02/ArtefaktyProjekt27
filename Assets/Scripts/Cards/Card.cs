using System;

/// <summary>
/// Represents a card instance in a run's persistent collection or a combat deck.
/// </summary>
public class Card
{
    public readonly CardDefinition Definition;
    public string InstanceId { get; }

    public Card(CardDefinition definition)
    {
        if (!definition)
        {
            throw new ArgumentNullException(nameof(definition), "Card definition can't be null.");
        }

        this.Definition = definition;
        InstanceId = Guid.NewGuid().ToString("N");
    }
}
