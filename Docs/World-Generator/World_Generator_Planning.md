# World Generator Planning

## Purpose
This document captures the current planning decisions for the world generator that will come before the full colony gameplay prototype.

The goal of this pass is to define a believable, tunable, and scalable forest world generator that works with the current grid foundation and can support later colony systems.

This planning pass is focused on the generator itself, not pawn spawning, colony bootstrap logic, or later gameplay systems.

---

## High-Level Goal
The world generator should create a believable forest map that:

- works on top of the existing grid system
- is tunable from the Inspector
- is easy to debug and improve later
- supports future resource gathering and colony interaction
- keeps the full map broadly playable
- avoids overcommitting to overly advanced simulation too early

The generator should feel natural without requiring full biome simulation, elevation systems, or advanced hydrology.

---

## Core World Model
The current planning direction separates world concerns into three layers:

### Terrain
Terrain describes what the ground is.

Planned terrain types:
- Grass
- Dirt
- ForestFloor
- Water

### Content
Content describes what is placed on top of the ground.

Current broad content categories:
- Empty
- NaturalBlocker
- Resource
- Structure

### Gameplay State
Gameplay state describes how the tile behaves during play.

Current gameplay state includes:
- walkable / non-walkable
- occupied / unoccupied
- reserved / unreserved

### Core Rule
Terrain describes the base ground.
Content describes what is on the tile.
Gameplay state describes how the tile behaves during play.

This keeps the system scalable and avoids overloading one field with too much meaning.

---

## Generator Scope
This generator pass should focus on:

- generating believable water features
- generating believable land terrain around water
- assigning terrain types across the grid
- placing trees after terrain generation
- exposing tuning values for testing and iteration
- supporting future improvements without requiring a rewrite

This pass is not focused on:

- pawn spawn placement
- colony start placement
- movement cost / terrain weights
- full hydrology simulation
- elevation-based river flow
- complex water-network simulation
- advanced biome/ecosystem simulation

---

## Forest Definition
For this prototype, a believable forest does not mean realistic ecological simulation.
It means a map that reads naturally and supports gameplay.

A believable forest should have:

- clear wooded regions
- clear open land
- water that feels naturally placed
- non-uniform tree density
- natural transitions between open and wooded areas
- enough open land for colony interaction and movement

The forest should not feel like evenly scattered random trees.
It should feel like broad natural regions shaped by water, terrain, and tree clustering.

---

## Water-First Generation Direction
The current planning decision is that water should generate first.

### Why Water Comes First
Water is one of the strongest map-shaping features.
It affects:

- where land masses exist
- where forest can grow
- how open areas form
- how movement routes feel
- how believable the overall map reads

If water is generated first, the terrain and forest can react to it.
This should produce a more natural result than generating forest first and carving water through it afterward.

---

## V1 Water Direction
The water system should usually feel internal and basin-like.

The generator should not require every map to have an edge-to-edge river.
Water may remain internal, connected, and natural-looking without crossing the full map.

### Planned V1 Water Feature Types
- Basin
- Channel
- Partial river
- Widened sections

### Basin
A basin is a larger internal water body.
It may read as a pond, wetland pocket, or small lake-like feature.

### Channel
A channel is a narrower winding water feature that can extend from or around a basin.

### Partial River
A partial river is a stronger directional water feature, but it does not need to cross the whole map.

### Widened Sections
A widened section is a part of a channel or partial river that opens into a broader pond-like area.
This helps the water system feel less uniform.

### Water Design Goals
The water system should:

- feel natural and irregular
- support internal basin-like layouts
- allow some branching behavior
- avoid dominating the whole map
- avoid fragmenting land too aggressively
- leave broad areas of usable land

### Out of Scope for V1 Water
- full hydrology simulation
- elevation-aware water flow
- pond-to-pond connection logic using pathfinding
- complex tributary hierarchies
- guaranteed map-wide river systems on every seed

---

## Terrain Generation After Water
After water is generated, the remaining land should be assigned terrain types.

This should use a hybrid model:

- distance from water gives broad environmental tendency
- noise adds natural variation and prevents artificial banding

This is intended to produce terrain that feels connected to nearby water without looking too mechanical.

---

## Terrain Influence Bands
The current V1 plan uses three water-distance influence bands:

- Near water
- Mid distance
- Far from water

### Near Water
Near-water land should favor:
- Dirt
- Grass

Near-water land should be less likely to become:
- ForestFloor

This area should read more open and exposed.

### Mid Distance
Mid-distance land should favor:
- Grass

It may also include:
- Dirt
- ForestFloor

This area should act as a transition band between open land and deeper forest.

### Far From Water
Farther land should favor:
- ForestFloor
- Grass

It should be less likely to become:
- Dirt

This area should read as deeper forest territory.

---

## Terrain Types
### Grass
Grass represents open land or open natural field space.

### Dirt
Dirt represents exposed ground, clearings, or worn natural earth.
It can help break up the map visually and support future colony-used space.

### ForestFloor
ForestFloor represents ground that belongs to a forested region.
It is not the tree itself.
It tells the generator and the player that the tile belongs to wooded terrain.

### Water
Water represents blocked natural water terrain.
Water is terrain, not content.

---

## Tree Placement Rules
Trees should be placed after terrain assignment.

For V1:
- trees strongly prefer ForestFloor
- trees may rarely appear on Grass
- trees do not appear on Dirt
- trees do not appear on Water

This allows the map to have:
- dense forest interiors
- softer forest edges
- open terrain around water and dirt areas

The forest should be shaped by terrain rather than relying on random tree scatter alone.

---

## Tunability Goals
The generator should be designed to expose tuning values in the Inspector.

Examples of tunable areas include:

### Global
- seed
- auto-generate on start
- regenerate controls

### Water
- water coverage
- basin count or likelihood
- channel count or likelihood
- width ranges
- branch amount
- irregularity strength

### Terrain
- near / mid / far band thresholds
- terrain tendency strength
- noise scale
- noise influence strength

### Trees
- forest-floor tree chance
- grass tree chance
- clustering strength
- density tuning

### Validation
- maximum blocked percentage
- minimum open land percentage
- minimum tree/resource coverage

The exact tuning set can be refined during implementation, but the generator should be structured so these values can be changed without rewriting the system.

---

## Scalability Goals
The generator should be built in a way that can improve over time.

This planning direction should scale into future additions such as:

- more terrain types
- more world content types
- better tree/resource placement rules
- special shoreline behavior
- mud, roads, or biome variants
- better river logic
- richer water-network generation
- future movement weights if needed

The current planning direction avoids locking the project into one overly rigid technique.

---

## Algorithm Direction
The current planning direction is hybrid rather than single-method.

### Broad Direction
- water features establish the major natural shape first
- land terrain reacts to water distance
- noise is used to break up and naturalize terrain transitions
- trees are placed after terrain is assigned

### Notes on Techniques
Possible techniques discussed during planning include:
- Perlin noise for natural variation
- cellular automata or blob-like generation for basin shaping
- structured region logic where useful

The important planning decision is not to commit the entire system to one single algorithm too early.
Instead, the generator should be structured as a tunable pipeline that can evolve over time.

---

## Current V1 Generator Order
The current planned order is:

1. Generate water features
2. Compute water-distance influence bands
3. Assign terrain types using water distance plus noise
4. Place trees and other content after terrain assignment
5. Validate broad map playability
6. Visualize and debug the result

---

## Playability Direction
The entire map should remain broadly playable.

This does not mean every tile must be walkable.
It means the generated result should not become over-fragmented, over-blocked, or unusable for future colony play.

The generator should aim for:

- broad usable land areas
- natural blocked areas from water and trees
- believable movement space
- enough resources for early colony interaction

Exact pawn spawn placement is intentionally out of scope for this planning pass.
That should be handled later by a separate system.

---

## Current Locked Planning Decisions
The current planning decisions are:

- the generator should create a believable forest map
- the system should be tunable and scalable
- terrain and content should stay separate
- water should generate first
- water should often feel internal and basin-like
- V1 water features are basin, channel, partial river, and widened sections
- land terrain should be assigned using three water-distance bands
- terrain assignment should use water distance plus noise
- terrain types are Grass, Dirt, ForestFloor, and Water
- trees should be placed after terrain generation
- trees strongly prefer ForestFloor
- trees do not appear on Dirt or Water
- pawn spawn and colony-start logic are out of scope for this pass
- terrain movement weights are out of scope for this pass

---

## Recommended Next Steps
The next planning and implementation steps should be:

1. Create a separate ticket for tile-data updates needed to support terrain type.
2. Define the exact `TileTerrainType` enum and decide what needs to be added to `GridTile`.
3. Break the world generator into smaller implementation tickets.
4. Build the first generator pass in a staged, tunable way.
5. Add debug visualization to inspect terrain and water output.

