---
name: presentation-flow
description: "Use when adding or changing gameplay effects, action snapshots, Director rules, presentation events, or animations in the card game's backend-to-frontend flow. Preserve the separation between authoritative game state and asynchronous presentation."
---

# Game Action and Presentation Flow

Use this workflow when a gameplay change needs to be represented visually, or when adding/changing an effect, a Director projection rule, a presentation event, or an animation.

## Stable architecture

Keep the responsibilities separate:

1. **Gameplay / StateEngine** owns rules and authoritative state. An effect changes state synchronously and records the facts that actually occurred as typed actions. Do not make gameplay wait for an animation.
2. **Command boundary** groups actions caused by one accepted command in their execution order and publishes at most one immutable snapshot for that command. Do not publish attempted, rejected, or merely planned actions.
3. **Presentation Director** reads only the snapshot. Its rules project or group actions into presentation events; they must not inspect live gameplay state. Preserve event order, and only merge actions when their data and position make the grouping valid.
4. **Presentation queue/player** consumes events sequentially. An event is complete only after its animation finishes. The presentation layer must not read or mutate combat statistics.
5. **Animation components** own visual behavior and may use the playback context for supported pause/speed behavior. They receive presentation data, not StateEngine objects.

Treat current class names and files as implementation examples, not permanent API promises. Start by locating the current command boundary, action/snapshot types, Director registration point, animation base/interface, and playback host in the repository.

## Procedure

1. Trace the relevant gameplay command from its caller through the authoritative effect/state operation. Confirm where validation occurs and what constitutes a successful state change.
2. Implement the effect in the gameplay/state layer using the established rule and pipeline patterns. Do not put game rules in a UI, Director rule, or animation.
3. Define or reuse a typed action containing the identifiers and outcome data required to explain what happened. Include a reason/source identifier when it affects presentation or rules. Avoid passing scene objects, mutable state objects, or presentation-only guesses.
4. Record the action only after the operation succeeds. Ensure the command boundary includes all resulting actions in their actual order and emits one snapshot, not one snapshot per low-level effect.
5. If presentation needs different treatment, add a focused Director rule and register it at the project's central rule-composition point. Rules should have explicit applicability, preserve unrelated actions, and not silently consume actions they do not handle. Keep grouping local to the rule rather than adding global special cases.
6. Add an animation component using the current presentation animation contract/base. Map it to the projected event type, read only the event payload, and complete its asynchronous task only when the visual work is finished. Respect playback controls if the animation API exposes them.
7. Wire the animation through the existing presentation host/registry. Prefer the project's serializable animation-component type over interface arrays or untyped `MonoBehaviour` arrays when configuring Inspector references.
8. Validate the complete path: successful command → ordered action(s) → one snapshot → expected Director event(s) → sequential animation completion → playback acknowledgement. Check rejected/no-op commands do not produce success-shaped actions or snapshots.
9. Report any Unity Editor setup that cannot be represented in code.

## Design guidance

- Keep action and event identifiers stable and intentional; do not make visuals depend on incidental class names or localized display text.
- Preserve every action's backend facts when grouping. A combined animation must still be able to identify all affected cards/entities and relevant reasons.
- Keep Director composition discoverable in one place. New rules should be easy to add without registering overlapping behavior across unrelated scripts.
- Missing handlers, invalid snapshots, and failed animations should remain visible errors; do not silently drop events or unblock gameplay as if playback succeeded.
- Avoid predicting future architecture in this skill. Re-check the current code and adapt names, registration APIs, tests, and Unity wiring as they evolve.

## Current code orientation

The flow is currently represented by `Assets/Scripts/StateEngine`, `Assets/Scripts/FightManager.cs`, `Assets/Scripts/Presentation`, and `Assets/Scripts/Presentation/Animations`. These paths are orientation only; search the project for current implementations before editing.
