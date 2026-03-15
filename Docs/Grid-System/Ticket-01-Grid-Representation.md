# Ticket 1: Grid Representation Decisions

## Epic

Grid System Foundation

## Goal

Define how the prototype grid will be represented and how each tile will be identified within the system.

## Grid Representation Decisions

### 1. Grid Layout

**Decision:** Use a **square grid**.

**Reasoning:**  
A square grid is the simplest fit for the prototype and keeps movement, placement, lookup, and debug visualization straightforward.

---

### 2. Tile Coordinate System

**Decision:** Identify each tile by integer grid coordinates:

- `x` = horizontal column
- `y` = vertical row

**Recommended rule:**  
Use **zero-based coordinates** for storage and lookup.

**Examples:**

- `(0, 0)` = first tile
- `(1, 0)` = one tile to the right
- `(0, 1)` = one tile forward/up in grid space

---

### 3. Minimum Tile Data

**Decision:** Each tile should at minimum store:

- `x` coordinate
- `y` coordinate
- `worldPosition`
- `isOccupied`

**Optional later data:**

- `tileType`
- `isWalkable`
- placed object reference
- highlight state

---

### 4. Grid Origin Behavior

**Decision:** The grid should start from a **chosen origin transform/reference point**, not be hardcoded to world `(0,0,0)`.

**Working behavior:**

- the Grid component is attached to a scene object
- that object’s transform acts as the grid origin

**Recommended interpretation:**

- the origin represents the **bottom-left corner** of the grid
- tiles extend positively across width and height from that point

---

### 5. Cell Size Behavior

**Decision:** `cellSize` defines the width and depth of each tile in world units.

**Working behavior:**

- larger cell size = tiles are spaced farther apart and cover more world space
- smaller cell size = tiles are tighter and cover less world space
- world-to-tile conversion is based on dividing by `cellSize`
- tile-to-world conversion is based on multiplying by `cellSize`

**Recommended rule:**  
A tile’s world position should represent the **center point** of the cell.

---

## Acceptance Criteria Coverage

- grid layout is documented as square
- tile coordinates are clearly defined
- minimum tile data is identified
- grid origin behavior is decided
- cell size behavior is decided

## Ticket Status

**Status:** Done

**Deliverable:**  
Grid representation is defined and documented for implementation.
