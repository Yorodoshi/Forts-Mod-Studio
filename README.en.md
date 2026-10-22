# Forts Mod Studio

**[Документация на русском](README.md)** · **[Download the application](https://github.com/Yorodoshi/Forts-Mod-Studio/releases/latest)**

A desktop mod editor for **Forts**. Create materials, devices, technology buildings, weapons, projectiles and scenarios, then export or install your mod.


The primary release is **2.7.0**. At the owner's request, 2.7.1 is retained as a prerelease following a 4/71 VirusTotal report. [Rollback details and exact files](docs/releases/rollback-to-v2.7.0.md).

## Install and launch

1. Open [Releases](https://github.com/Yorodoshi/Forts-Mod-Studio/releases).
2. Download and extract the release ZIP, then run **Forts Mod Studio 2.7.exe**. A standalone EXE is also available.
3. Under **Project and installation**, select the folder containing `Forts.exe` if automatic detection did not find it.

Requires Windows x64, .NET Framework 4.x and an installed copy of Forts for game templates, Lua validation and the in-game range. FMS needs no installer or additional libraries. High Seas features, including consumable ammunition, require the corresponding game content.

Before updating, save your project and close the previous FMS version. Versions 2.5–2.7 share the same autosave.

## English and Russian

Select **English** or **Русский** in the top toolbar. The interface changes immediately and remembers your choice. Russian is the first-launch default.

The interface language is stored separately from the mod project. Switching it does not rewrite your Russian or English mod names, descriptions or other editable content. Object lists use the English name when available in English mode.

## Features

- Materials: game parameters, thickness, seamless textures, icons and full-section extrusion.
- Devices and technology buildings: parameters, unlock dependencies, construction-stage images and sounds.
- Weapons: ammunition, separate base and rotating artwork, pivot, muzzle position and recoil.
- Projectiles: flight parameters, visuals, cluster splitting and editable child projectiles.
- Hybrid anti-air: configurable long- and close-range modes with automatic switching.
- Scenarios: events, conditions, actions, variables and sounds.
- Scene editor: building, aiming, trajectory, animation, zoom, undo and redo.
- ZIP export, installation into a separate mod folder, Lua validation using the installed game and log viewing.

## Preview and physics

The built-in animation is a flight sketch. Use **Forts Range · game physics** to examine impacts, deformation, stability and destruction in the actual game. The range creates a separate map and helper mod from the current project.

Rendering uses retained layers, world tiles, projectile rotation atlases and separate sprites for stationary objects. Camera movement reuses prepared images. **Frame details** displays rendering-stage timings and cache sizes. Actual FPS depends on the scene, settings and computer; this release has not undergone an interactive FPS measurement.

## Projects

- **Save** writes a `.fms.json` project.
- **Open JSON** restores a project, including parameters and embedded custom assets.
- Autosave: `%LOCALAPPDATA%\FortsModStudio2\autosave-2.5.fms.json`.
- Interface language: `%LOCALAPPDATA%\FortsModStudio2\interface-language.txt`.

This repository contains editor source code. Personal projects, private images and installed game files are not included. The editor reads standard game resources from your local Forts installation.

## Build from source

On Windows, run from the repository folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `dist\Forts Mod Studio 2.7.exe`. The script uses the Windows .NET Framework x64 compiler. C# source and embedded Lua resources are in `src/`.

## Authenticity and security

See [SECURITY.md](SECURITY.md) for checksums, build provenance, actual signing status and antivirus-report instructions.

## Feedback

Open an [Issue](https://github.com/Yorodoshi/Forts-Mod-Studio/issues) with the FMS version, steps leading to the problem and a screenshot. For performance problems, enable **Frame details** and mention whether projectile tracking is enabled.

Forts Mod Studio is an independent community tool. Forts and game assets belong to their respective owners.
