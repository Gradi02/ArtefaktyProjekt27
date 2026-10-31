# Project Agent Instructions

## Project Overview
- Game engine: Unity 6.3
- Language: C#
- Render pipeline: URP
- Target platform: PC / Steam
- Input: Unity Input System
- Animations: DOTween / async c# Tasks

## Architecture
- Use modular, component-based architecture.
- Prefer composition over inheritance.
- Keep gameplay logic separate from presentation.
- Avoid unnecessary dependencies between systems.
- Avoid too many Singletons.

## Unity Rules
- Use ScriptableObjects for shared configuration data.
- Use SerializeField for private Inspector fields.
- Avoid GameObject.Find and FindObjectOfType in gameplay code.
- Avoid unnecessary allocations in Update.
- Use object pooling for frequently spawned objects.
- Use assembly definitions for bigger systems.
- Prefer events over direct references when decoupling systems.

## Coding Standards
- Use PascalCase for classes and public members.
- Use camelCase for private fields.
- Prefer explicit types over var when clarity improves.

## Workflow
- Inspect existing code before creating new systems.
- Reuse existing architecture and conventions.
- Do not modify unrelated files.
- Explain architectural decisions.
- Report any Unity Editor actions that must be performed manually.

## Restrictions
- Do not add packages without approval.
- Do not modify ProjectSettings without explaining why.
- Do not create duplicate systems.
