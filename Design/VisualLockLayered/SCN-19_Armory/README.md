# SCN-19 Armory VisualLockLayered

Date: 2026-05-23

## Purpose

`SCN-19 Armory` is the config-backed roster inspection screen reached from `SCN-03 Commander Profile` through `Open Armory`. It lets the player inspect units, vehicles, aircraft, buildings, support abilities, upgrade tracks, parts, and gear modules before loadout or store follow-through.

## Current Target

- Reference target: `reference/SCN-19_Armory_Landscape_Target.png`
- Source prompt: `prompts/SCN-19_Armory_TargetLock_V02.md`
- Status: target-lock reference generated; separated implementation layers not requested yet.

## Header Rule

Use the `SCN-13 Skirmish Setup` secondary-screen header pattern:

- Global top header has the brand zone, Credits, Supplies, Command, inbox, and settings.
- The Back control is a square arrow button beside the local screen title block on the left.
- Do not add a far-right global `BACK` button on this screen.

## UI Content

- Left category rail: Units, Vehicles, Aircraft, Buildings, Support, Upgrades.
- Center roster cards: display prefab-backed examples such as Rifleman Male II, Marksman Male I, Assault Breacher Female II, Field Commander, Cargo Truck, Canopy Truck, Attack Helicopter, Transport Helicopter, Oil Pump, Oil Refinery, Guard Tower, and Ammunition Depot.
- Right inspection panel: selected item identity, description, stats, abilities, upgrade progress, unlock/source, and CTAs.
- Bottom tabs: Owned, Upgrade Tracks, Parts, Gear Modules.
- Route breadcrumb: Main Menu > Commander Profile > Armory.

## Layering Notes For Next Pass

The target-lock is not an implementation layer pack. When layers are requested, regenerate clean layers through the active V15 workflow. Do not cut this reference target into parts. Text must remain live in Unity, and icons, frames, bars, roster art, selected-item art, category buttons, CTA buttons, and progress components must be separate.
