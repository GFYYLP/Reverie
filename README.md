# Reverie

A first-person dreamscape where the player reconstructs fragmented memories from abstract geometry, not by solving, but by looking carefully and choosing what to keep.

![Unity](https://img.shields.io/badge/Unity-black?logo=unity)
![Status](https://img.shields.io/badge/status-in%20development-yellow)
![Platform](https://img.shields.io/badge/platform-3D-blue)
<p align="center">
  <img src="demo.gif" width="850" alt="Gameplay">
</p>

---


## Overview

Each floor (level) presents the player with a memory fragment (a reference image) to reconstruct. The player explores procedurally generated rooms, photographs abstract geometry, and arranges those captures into a composite that matches the reference.

The world is your canvas. Rooms respond to your (the character's) emotional state as you look forward and choose what is worth remembering. Projecting captures onto the world leaves a mark on it, a personal layer that accumulates across rooms, turning the dreamscape into somewhere that was actually yours



## Gameplay Features

- **Photography**: capture the main render texture (blitting to a preallocated texture, downsampled to save memory) clipped to a specified zoom level.
- **Flipnotes recording**: enabled via holding the capture button in form of cycling multi-frame captures. Smooth video recording is not adopted due to memory bandwidth constraint.
- **Collage canvas**: captured photos and flipnotes populate a draggable grid. Slots can be rearranged freely to build the composite, to be matched with the reference memory image/flipnote for level completion.
- **Decal projection**: stamp a capture onto the world's surfaces (via URP's Decal Renderer). Projected captures reference the same material as  their source snapshot, keeping them in sync.
- **Emotion System**: tracks three emotional states (content, unease, awe) derived from parsing captures and the current main camera's view (once every few seconds), each with their own associated spectrum of grammar parameters.
- **Render texture parser**: converts texture to emotion parameter values by evaluating its visual trait (symmetry, color variance, silhouette complexity, etc...) via a compute pass. Reference image matching also adopts this.
- **Procedural room**: generated via 2 compute shader passes: SDF evaluation on a voxel grid followed by marching cubes mesh extraction. The scene is composed of dozens of objects formed by combinations of primitive shapes, domain warped via FBM noise. Generation grammar is derived from the current emotion parameters.
- **Post processing chain**: individual post processes have their parameters tuned correspondingly to the character's current emotion (via URP's Volume manager).


## Tech Stack

- **Engine:** Unity 6 (URP)
- **Language:** C#, HLSL
- **Rendering:** GPU compute shaders (HLSL) for SDF generation, marching cubes, and emotion grammar parsing. URP Volume for post-processing. URP DecalProjector for world-space photo stamping.
- **UI:** Unity UGUI + RawImage for the collage canvas


## Project Structure

```
Assets/Scripts/
  RoomSpace/
    RoomSpace.cs          # Mesh generation orchestration (SDF → marching cubes → mesh)
    RoomManager.cs        # Room lifecycle and player progression
    RoomConfig.cs         # Grammar parameter container (drives SDF shader)
    Door.cs               # Trigger-based room transition
    NoiseSDF.compute      # SDF evaluation: object placement, archetypes, domain warp
    MarchingCubes.compute # Mesh extraction from density field
  Emotion/
    Emotion.cs            # Singleton emotional state (content, unease, awe)
    EmotionManager.cs     # Visual grammar analysis and emotion mapping
    VolumeManager.cs      # Post-processing driven by emotional state
    EmotionParser.compute # Per-capture grammar metric extraction (GPU)
  Canvas/
    CanvasManager.cs      # Camera mode input and vignette UI
    Composite.cs          # Snapshot orchestration, capture pipeline, collage assembly
    Snapshot.cs           # Individual capture: frame storage, flipnote playback, drag/drop
    ShotProjector.cs      # DecalProjector pool and world-space projection
    Movement.cs           # First-person controller with bob and sway
```

## Controls

| Input | Action |
|---|---|
| WASD | Move |
| Mouse | Look |
| Q | Sprint |
| Space | Jump |
| F | Display/Hide composite collage |
| Right-click (hold) | Enter camera mode |
| Scroll | Adjust capture zoom |
| Left-click (tap) | Take photo |
| Left-click (hold) | Record flipnote |
| Middle-click | Project last capture onto world |
| Escape | Toggle cursor lock |


## Status

Core systems are implemented and running: procedural room generation, emotion parsing, post-processing feedback, snapshot capture, animated flipnotes, and decal projection. The remaining work is improving visual presentation and closing the feedback loops: wiring emotional state into room grammar generation, building the composite scoring pipeline against reference images, and tuning the experience end-to-end.
