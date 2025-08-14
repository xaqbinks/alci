# Alien Civilizations - A Procedural Evolution Simulation

## Overview

This project is the complete C# codebase for a complex, procedural simulation game where players can observe and subtly influence the evolution of alien species on a dynamic planet. The simulation is built on an emergent, multi-layered system that models everything from planetary physics to species evolution, societal development, and inter-species conflict.

This document provides instructions on how to set up, compile, and test the simulation scripts in a standard Unity project on a Windows PC.

## Core Features

- **Procedural Planet Generation:** Creates a unique, tile-based planet from a sphere mesh, with varied terrain types (Ocean, Land, Mountain) and resource distribution.
- **Dynamic Climate Model:** The planet's atmosphere is composed of multiple gases. Greenhouse gases, including pollution from industrial activity, dynamically affect the global temperature.
- **Biotic Simulation:** Species live, consume resources, grow, and migrate based on their unique biological traits and environmental conditions. The simulation includes a full predation model (carnivores vs. herbivores).
- **Evolutionary Engine:** Species are under constant environmental pressure. The simulation analyzes the primary causes of death (e.g., starvation, harsh climate) and triggers mutations in a species' stats, allowing them to adapt over time.
- **Societal Development:** Species generate knowledge, research a technology tree, and form governments (Autocracy, Collective, Technocracy) that provide unique bonuses.
- **Diplomacy & Conflict:** Species form relationships based on factors like border friction. These relationships can lead to diplomatic treaties (Research Agreements, Defensive Pacts) or devolve into war. Combat is resolved based on population, combat strength, technology, and terrain.
- **Great Leaders:** Unique Great Leaders can emerge, providing powerful, temporary bonuses to their species.
- **AI Opponents:** The simulation supports multiple species, with AI controllers making strategic decisions about research, migration, and expansion.
- **Procedural Visualization:** In the absence of user-provided 3D models, the simulation will procedurally generate unique meshes for each species. These meshes visually change in real-time to reflect the species' evolution, with their shape, size, color, and texture determined by their biology, stats, and ethics.

## Setup & Compilation (VR First with PC Fallback)

This project is designed as a **VR-first experience for the Meta Quest 3**, but it maintains full testability on a standard PC. The setup process requires the Oculus Integration SDK.

### Prerequisites

- **Unity Hub** & **Unity Editor (2021.3 LTS or newer recommended)**
- **Oculus Integration SDK:** Download and import this free package from the Unity Asset Store.
- Basic knowledge of the Unity Editor interface.

### Step 1: Project and Asset Import

1.  Create a new, empty **3D (Core)** project in Unity Hub.
2.  Go to `Window > Package Manager`. Find and import the **Oculus Integration** package.
3.  Close the Unity Editor.
4.  Copy the entire `Assets` folder from this repository into your project's root folder, **replacing** the existing `Assets` folder.
5.  Re-open your project. Unity will import all scripts. If prompted by Oculus to upgrade/fix anything, accept the recommended changes.
6.  If prompted, click **Import TMP Essentials**.

### Step 2: Scene Setup

The project uses a two-scene architecture. You will need to create/verify these scenes and add them to the build settings.

#### Scene 1: `MainMenu`

1.  Create a new scene (`File > New Scene`) and save it as `MainMenu.unity` inside an `Assets/Scenes/` folder.
2.  **Create Managers:**
    -   Create an empty `GameObject` named `[Managers]`.
    -   Attach the `GameManager`, `GameSetupManager`, `InputManager`, and `PlatformRigManager` scripts to it.
3.  **Create the UI:**
    -   Create a UI Canvas. **Set its Render Mode to `World Space`** to prepare it for VR.
    -   Attach the `MainMenuController.cs` script to a child GameObject of the Canvas.
    -   Populate the `MainMenuController`'s fields with data assets (`PlanetData`, `SpeciesData`) and UI prefabs as needed.
4.  **Set up the Platform Rigs:**
    -   **PC Rig:** Create a `GameObject` named `PC_Rig`. Add the `PCCameraController.cs` script to its child Camera object.
    -   **VR Rig:** Drag the `OVRCameraRig` prefab from the Oculus SDK into your scene.
    -   On your `[Managers]` GameObject, link the `PC_Rig`, `VR_Rig`, and their respective components (`PCCameraController`'s Camera, `OVRCameraRig`'s `rightControllerAnchor`) to the fields on the `PlatformRigManager` and `InputManager`. The `PlatformRigManager` will automatically enable the correct rig at runtime.

#### Scene 2: `MainSimulationScene`

1.  Create a second scene and save it as `MainSimulationScene.unity`.
2.  Repeat the setup for the `[Managers]` and `Platform Rigs` as in the `MainMenu` scene. This is necessary because they hold scene-specific references. The singletons will handle persistence correctly.
3.  Add the `SimulationManager` and all other simulation-related managers to an empty `[Simulation]` GameObject.
4.  Add the `Planet` GameObject.
5.  Add any in-game UI Canvases, also set to **World Space**. For example, the `InterventionUIController` and `ResolutionUIController` would live here, initially disabled.

### Step 3: Build Settings

1.  Go to `File > Build Settings...`.
2.  Add both `MainMenu.unity` and `MainSimulationScene.unity` to the "Scenes In Build" list.
3.  **Ensure `MainMenu` is at index 0.**
4.  To build for VR (Meta Quest), switch the platform to **Android**. Set the texture compression to **ASTC**.
5.  To build for PC testing, keep the platform as **Windows/Mac/Linux**.

### Step 4: Run the Game

1.  Open the `MainMenu` scene.
2.  Press **Play**. The correct rig (PC or VR) will activate automatically.
3.  Interact with the UI using your mouse or VR controller and launch the simulation.

## Automated Building

This project includes an automated build script to simplify compiling.

1.  Make sure your `MainMenu` and `MainSimulationScene` are added and enabled in the Build Settings (as described in Step 3).
2.  At the top of the Unity Editor, click the new **Build** menu item.
3.  Select **Build for Windows**.
4.  The script will automatically build the project and place the output `.exe` and data files into a `Builds/Windows/` folder in your project's root directory.
5.  The output folder will open automatically upon successful completion.

## Procedural Audio System

The project has been upgraded with a powerful procedural audio engine that generates all sound effects on the fly. This system replaces the need for pre-recorded audio files and allows for highly dynamic and varied soundscapes that react to game events.

### How It Works

The system is built on two core components:
1.  **`ProceduralSoundPreset`:** A `ScriptableObject` that acts as a "recipe" for a sound. You can create and define these presets in the editor.
2.  **`SoundSynthesizer`:** A static class that reads a preset and generates a playable `AudioClip` based on its parameters.

### Setup & Workflow

1.  **Ensure `AudioManager.cs` is on the `SimulationManager` GameObject.** This script now only requires a single `AudioSource` component.
2.  **Create Sound Presets:**
    -   In the Project window, right-click and go to `Create > Alien Civilizations > Procedural Sound Preset`.
    -   Name the new asset (e.g., "VolcanoPreset"). This name is important, as it's used to trigger the sound.
    -   Select the asset and configure its properties in the Inspector. The system now supports multiple advanced synthesis techniques:
        -   **Waveform:** The base sound shape. This now includes an **`Additive`** type. If you select `Additive`, the sound will be built from the **Harmonics** list below, allowing for very complex timbres.
        -   **Harmonics List:** For `Additive` synthesis, you can define a list of sine wave components. Each has a frequency multiplier (relative to the base frequency) and an amplitude. This can be used to create bells, complex drones, or other unique sounds.
        -   **Frequency:** The base pitch of the sound (or the fundamental for additive synthesis).
        -   **Envelope (ADSR):** Shape the volume of the sound over its lifetime.
        -   **FM Synthesis:** Add metallic or complex overtones by modulating the frequency.
        -   **Filter (Biquad):** The synthesizer now uses a high-quality Biquad filter. You can select the **Filter Type** (`None`, `LowPass`, `HighPass`, `BandPass`) and set the **Filter Frequency** and **Resonance (Q)** to sculpt the sound.
        -   **Delay Effect:** Add a simple echo to the sound. You can control the `Delay Time`, `Delay Feedback` (how many echoes), and `Delay Mix`.
        -   **Reverb Effect:** Add a sense of space to the sound. You can control the `Reverb Time` (the length of the tail) and the `Reverb Mix`.
3.  **Add Presets to the AudioManager:**
    -   Select the `SimulationManager` GameObject.
    -   In the `AudioManager` component, find the `Sound Presets` list.
    -   Add your newly created preset assets to this list.

### Performance Caching

The `AudioManager` now includes a simple caching system. If the same sound (with the same dynamic parameters) is triggered multiple times in quick succession, a cached version of the `AudioClip` will be used instead of regenerating it. This improves performance during very busy moments of the simulation. The cache is automatically cleared every 10 seconds to manage memory.

### Triggering Sounds from Code

Sounds are triggered from any manager with a reference to the `SimulationManager`. The system now supports positional audio, so you must provide the sound's origin point.

The audio system is now integrated into the following core game events:
-   **Geology:** Earthquakes, Volcanoes, and Mineral Deposits.
-   **Conflict:** Battles between species.
-   **Civilization:** Technology discoveries and government formations.
-   **Evolution:** Application of a beneficial genetic mutation.
-   **Great Leaders:** The spawning and death of a leader.

#### 1. Playing a Static Preset (Positional)

To play a sound exactly as defined in the preset, at a specific location:

```csharp
// The 'worldPosition' variable should be the location of the event.
simManager.audioManager.PlaySound("MyPresetName", worldPosition);
```

#### 2. Playing a Dynamic, Modified Sound (Positional)

The real power of the system is modifying sounds at runtime. The `PlayDynamicSound()` method allows you to override preset parameters just before the clip is generated. This is perfect for making sounds reflect the magnitude of an event.

**Example (from `ConflictManager.cs`):**

This example shows how a battle's sound is made deeper and more chaotic based on the number of combatants.

```csharp
void ResolveCombat(PlanetTile tile, SpeciesData s1, SpeciesData s2)
{
    // ...
    long totalPopInvolved = tile.populations[s1] + tile.populations[s2];
    float battleScale = Mathf.Clamp01((float)totalPopInvolved / 5000f);

    // Calculate new parameters based on the battle's scale
    float frequency = Mathf.Lerp(300f, 80f, battleScale);
    float fmAmount = Mathf.Lerp(50f, 400f, battleScale);

    // Trigger the sound at the tile's position with the dynamic parameters
    simManager.audioManager.PlayDynamicSound("BattleImpact", tile.position, frequencyOverride: frequency, fmAmountOverride: fmAmount);
    //...
}
```
This approach ensures that no two events need to sound exactly the same, making the world feel much more alive and reactive.

## Procedural Music System

The game now features a procedural music system that generates a continuous, non-repetitive, and adaptive ambient soundtrack.

### How It Works

A dedicated `MusicManager` generates a sequence of musical notes based on a set of rules and the current game state. It uses a simple synthesizer to create a soft, ambient tone for each note. This music is generated in a separate `AudioSource` and plays constantly in the background.

### Adaptive States

The music will automatically change to reflect the player's diplomatic situation:
-   **Peace:** The music uses a calm, consonant Major Pentatonic scale and a slow tempo.
-   **Tension:** If the player is in a "cold war" (low disposition but not officially at war), the music shifts to a more somber Minor Pentatonic scale with a sparser, slower tempo.
-   **War:** During wartime, the music remains in the minor scale but the tempo increases significantly, and a low-frequency percussive element is added to the beat to create a sense of urgency.

No special setup is required for this system. The `MusicManager` script should be attached to the `SimulationManager` GameObject, and it will handle the rest automatically.

---

## Visualizing the Simulation

### Planet & Species Visualization

The simulation uses two main scripts for visualization:
-   **`PlanetVisualizer.cs`**: Colors the planet mesh based on terrain type. This requires the `VertexColorMat` material you created during setup.
-   **`SpeciesVisualizer.cs`**: Represents species on the planet. It operates in two modes:
    1.  **Prefab Mode:** If you assign a 3D model prefab to the `Species Prefab` field on a `SpeciesData` asset, the visualizer will use your custom model.
    2.  **Procedural Mode:** If the `Species Prefab` field is left empty, the visualizer automatically generates a unique mesh for that species.

### Advanced Procedural Generation

The procedural mesh generation is now highly advanced and deeply tied to the simulation state:
-   **Generation Method:** The system uses different algorithms based on biology.
    -   **Organic Life (`Carbon_` types):** Generated using the **Marching Cubes** algorithm. This creates highly detailed and organic "blob-like" shapes from a 3D noise field, resulting in very unique and alien forms.
    -   **Crystalline Life (`Silicon_` types):** Generated from a subdivided cube which is then "faceted" to create a sharp, geometric, crystal-like appearance.
-   **Stat-Driven Appearance:** A species' current stats dynamically drive the generation parameters.
    -   **Noise Field:** For organic life, the 3D noise field is shaped by stats like `aggressionFactor`, changing the core form of the creature.
    -   **Size:** The final size of the mesh is linked to `GetCurrentCombatStrength()`.
    -   **Color:** The mesh's color is a blend based on the species' dominant ethics (Red=Militarist, Green=Erudite, Blue=Pacifist).

Because the mesh is regenerated every tick, you will see its shape, size, and color **change in real-time** as the species evolves.

**Simple Vertex Color Shader:**
```csharp
Shader "Unlit/VertexColor"
{
    Properties { _Color ("Main Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; return o; }
            fixed4 frag (v2f i) : SV_Target { return i.color; }
            ENDCG
        }
    }
}
```
