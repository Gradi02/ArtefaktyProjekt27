# Card authoring

Open **Tools > ANTK > Cards > Card Authoring** in the Unity Editor.

The window creates a `CardDefinition` and its data-driven behavior asset using text fields, dropdowns, and buttons. Choose an existing folder under `Assets`, add one or more effects, and generate the assets. The generated card type uses the existing `Attack` or `Defense` values.

The first version supports:

- Fixed damage targeted at the card's owner (the player for current player cards) or the opponent.
- Applying an existing status definition to either target, with configured stacks and duration. A duration of `-1` is permanent.

To add the generated card to the starting run deck, select `StarterDeck` and edit its card list and copy quantities in the Inspector. The `GameManager` prefab uses this asset when creating a run. 
Create additional deck assets from **Assets > Create > ANTK > Cards > Starter Deck**.
