# SCN-19 Armory VisualLockLayered

Date: 2026-05-23

## Purpose

`SCN-19 Armory` is the config-backed roster inspection screen reached from `SCN-03 Commander Profile` through `Open Armory`. It lets the player inspect units, vehicles, aircraft, buildings, support abilities, upgrade tracks, parts, and gear modules before loadout or store follow-through.

## Current Target

- Reference target: `reference/SCN-19_Armory_Landscape_Target.png`
- Target source prompt: `prompts/SCN-19_Armory_TargetLock_V02.md`
- Layer source prompt: `prompts/SCN-19_Armory_LayerPack_V01.md`
- Layer manifest: `layer_manifest.json`
- Contact sheet: `generated_one_go/layers_contact_sheet.png`
- Status: target-lock reference and V01 separated implementation layer pack generated.

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

## Layering Notes

The target-lock was not cut into parts. V01 layers were generated from fresh source sheets through the active V15 workflow. Text must remain live in Unity, and icons, frames, bars, roster art, selected-item art, category buttons, CTA buttons, and progress components are separate.
