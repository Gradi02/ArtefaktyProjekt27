using System;

public class Card
{
    public readonly CardDefinition Definition;
    
    public Card(CardDefinition definition)
    {
        if (!definition)
            throw new ArgumentNullException("Card can't have null definition!");
        
        this.Definition = definition;
    }
}
