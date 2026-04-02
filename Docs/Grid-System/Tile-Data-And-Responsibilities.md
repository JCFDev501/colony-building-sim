# Tile Data and Responsibilities

## Core Design Direction

Tiles must be **dynamic**, not static.

A tile should not represent only one permanent thing. Instead, it should act as a container of gameplay state that can change during play.

Examples:

- an empty tile can later hold a stone deposit
- a resource tile can later become depleted
- a walkable tile can later become blocked
- a free tile can later become occupied
- a tile can be reserved temporarily for work
- a tile can later contain a built structure

Because of that, tile design should focus on **state** rather than hardcoded one-purpose tile types.

## Tile Responsibilities

A tile is responsible for holding **per-tile gameplay state**.

For the prototype, a tile should answer questions like:

- where is this tile?
- can something move through this tile?
- is something currently occupying this tile?
- is this tile reserved?
- does this tile currently contain natural content or resource content?
- does this tile currently contain built content?
- can this tile change into something else later?

A tile should not be responsible for running high-level systems like pathfinding or spawning logic. It should only hold state and provide simple tile-level information.

## What Belongs on a Tile

For the prototype, each tile should store at least the following categories of data.

### 1. Identity data

This is the base information that defines the tile.

- grid coordinates
- world position

These should remain permanent for the lifetime of the tile.

### 2. Traversal state

This controls whether the tile can be moved through.

- walkable
- blocked
- optional movement cost later

For the first version, `walkable` is enough. Movement cost can be added later when pathfinding becomes more advanced.

### 3. Occupancy state

This tracks whether something is currently using the tile.

- occupied
- optional occupant reference later

For prototype v1, a simple boolean is enough.

### 4. Reservation state

This tracks whether the tile has been claimed by a future action.

- reserved
- optional reserver reference later

For prototype v1, a simple boolean is enough.

### 5. Content state

This tracks what kind of thing the tile currently contains.

The tile should be able to represent:

- empty content
- natural blocker content
- resource content
- built content

For the first version, this should be kept simple and generic.

A good early direction is to use a lightweight content/category value rather than hardcoding many separate tile classes.

### 6. Runtime mutability

Tile state must be allowed to change during play.

Examples of mutable tile data:

- walkable
- occupied
- reserved
- content/category
- resource presence or removal later

## What Should Stay in `GridManager`

`GridManager` should remain responsible for **grid-level structure and queries**, not for storing all high-level game logic.

`GridManager` should continue to handle:

- grid generation
- storing tiles in a lookup structure
- coordinate bounds checks
- coordinate-to-world conversion
- world-to-coordinate conversion
- neighbor lookup
- helper methods for querying and updating tile state

In other words:

- `GridTile` holds the state
- `GridManager` helps other systems find and interact with tiles safely

## What Should Not Belong Directly on a Tile

To keep the first version simple, a tile should not directly own:

- full pathfinding logic
- colonist behavior logic
- spawning systems
- build system orchestration
- rendering logic
- advanced simulation logic

Tiles are data/state objects first.

## Prototype Tile Rules

To keep this system usable for future features, the following rules should guide the prototype:

### Rule 1

A tile must support changing state during runtime.

### Rule 2

A tile must remain generic enough to support multiple future systems.

### Rule 3

A tile should store state, not system-level behavior.

### Rule 4

The first version should prefer simple booleans and simple categories over overly complex object references.

### Rule 5

The tile system should support future use by:

- colonist movement
- A\*
- build placement
- environment spawning
- resource placement
- PCG

## Recommended Prototype Tile Data

For prototype v1, the recommended tile data is:

- coordinates
- world position
- walkable
- occupied
- reserved
- content type/category

That is enough to support the next layers of the prototype without overbuilding.

## Recommended Content Direction

For the first version, tile content should be treated as a simple category.

Example early categories:

- Empty
- NaturalBlocker
- Resource
- Structure

This keeps the tile system flexible while staying easy to understand.

Specific resource or structure details can be expanded later once the first version is stable.

## Future-Friendly Notes

The design should leave room for later additions such as:

- movement cost
- occupant reference
- reserver reference
- resource amount
- structure reference
- terrain type
- tile modifiers

These are not required for the first prototype version, but the initial design should not block them.

## Acceptance Criteria

- tile responsibilities are clearly documented
- required tile state fields are decided
- tile vs `GridManager` responsibilities are clearly separated
- dynamic tile-state requirements are defined
- enough detail exists to begin implementing `GridTile` cleanly
- design remains simple enough for prototype development
- design is flexible enough to support colonists, building, environment spawning, and PCG

## Short Summary

For prototype v1, a tile should be a dynamic container of gameplay state rather than a static fixed type. It should store identity, traversal, occupancy, reservation, and simple content-category data, while `GridManager` remains responsible for grid-level lookup, conversion, and helper queries.
