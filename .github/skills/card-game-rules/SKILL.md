---
name: card-game-rules
description: "Use when implementing or changing card-game rules, cards, decks, turns, combat, or status effects. Follow the existing gameplay and StateEngine boundaries, preserve established behavior, and validate legal state transitions."
---

# Card Game Rules

Use this workflow for gameplay-rule changes, not for presentation-only work.

## Procedure

1. Inspect the relevant gameplay scripts and the existing state engine before editing. The current state engine is under `Assets/Scripts/StateEngine`; combat orchestration is in `Assets/Scripts/FightManager.cs`; card data begins in `Assets/Scripts/Card.cs`. Treat these as orientation, not as proof that a feature is complete.
2. Identify the authoritative owner of the rule and state being changed. Keep rules out of visual/UI scripts. Reuse the current state engine and gameplay flow where they fit; do not create a parallel rules engine or duplicate state.
3. Check how the behavior currently works, including turn ownership, status durations and hooks, effect processing, events, and invalid actions. Preserve existing behavior unless the request explicitly changes it.
4. Do not invent card costs, draw/discard behavior, deck composition, targeting rules, or other game design decisions. If a necessary rule is unspecified and has multiple reasonable interpretations, ask the user before choosing.
5. Make the smallest complete change. Validate inputs and illegal state transitions using the conventions of the affected code. Keep gameplay code independent of scene objects and visual presentation where the existing architecture allows it.
6. Add or update focused tests when a suitable test setup exists. Otherwise, run the narrowest available build or validation for the changed code and state clearly what could not be verified.
7. Review all affected call sites and events so the gameplay behavior remains consistent across player and enemy turns and fight completion.

## Project-specific guidance

- `ANTK.Game.StateEngine` owns state operations such as status application, turn hooks, and damage pipeline processing. `FightManager` currently coordinates combat and calls into it. Preserve this division unless the task specifically requires an architectural change.
- Explain any necessary architectural deviation and report Unity Editor setup or asset changes the user must perform manually.
