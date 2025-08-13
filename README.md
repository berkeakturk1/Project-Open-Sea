# Seabound

Procedural ocean exploration and adventure game built in Unity. Sail a vast, ever-changing sea, dive into procedurally generated shipwrecks, and explore unique islands shaped by advanced procedural algorithms.

## Overview

Seabound focuses on three main technical pillars:

1. **Gerstner Wave Simulation** – a physically inspired ocean surface model.
2. **Perlin Noise Terrain Generation** – procedural island and seabed creation.
3. **3D Wave Function Collapse** – modular, rule-based sunken structure generation.

Each run offers a different layout, resource distribution, and environmental conditions.

## Core Features

- **Procedural World**: Infinite variation in islands, seabeds, and wrecks.
- **Dynamic Ocean**: Real-time wave movement reacting to wind and light.
- **First-Person Exploration**: On-foot and underwater traversal with resource gathering.
- **Naval Travel**: Sail control with helm and sail adjustments based on wind direction/speed.

## Key Algorithms in Detail

### 1. Gerstner Wave Simulation

The ocean surface is generated using Gerstner wave equations, which model realistic wave shapes by combining horizontal and vertical displacements:

- **Multiple Wave Layers**: Several wave sets with different amplitudes, wavelengths, and directions are summed.
- **Trigonometric Functions**: Position offsets are calculated using sine and cosine, giving rolling, cresting waves.
- **Dynamic Lighting Interaction**: Normal vectors from wave equations are used to render realistic highlights and reflections.

### 2. Perlin Noise Terrain Generation

Perlin noise drives the shape and height of islands and seabeds:

- **Heightmaps**: 2D noise maps define terrain elevation, controlling beach, cliff, and mountain placement.
- **Octaves & Persistence**: Multiple layers of noise are blended for natural variation.
- **Biome Control**: Thresholds determine vegetation zones and seabed materials.

### 3. 3D Wave Function Collapse (WFC)

A fully custom 3D WFC pipeline is used for shipwreck interiors:

- **Module Library**: Structure pieces are created as modular 3D assets.
- **Adjacency Rules**: A preprocessing Python script analyzes meshes to generate neighbor compatibility tables.
- **Entropy-Based Selection**: At runtime, the algorithm picks the lowest-entropy cell and assigns a compatible module, ensuring consistent and varied wreck layouts.

## Getting Started

1. Switch to Berke Branch
2. Clone the repository.
3. Open in the correct Unity version.
4. Install any required Unity packages.
5. Open the `Main` scene and press **Play**.

## Build & Run

- Open **File → Build Settings** in Unity.
- Choose your target platform and build.
- Adjust graphics settings to suit performance needs.

## Honors

- First place in Başkent University graduation project exhibition (10.06.2025).
