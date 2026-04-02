# Movement System V1

## Purpose
This document defines the first version of the pawn command and movement system for the colony-building prototype.

The purpose of V1 is to prove that pawns can be selected, deputized, given move commands, preview their intended destinations, pathfind across the grid, and move smoothly to valid assigned tiles.

## Core Command Rule
Only pawns that are both **selected** and **deputized** can execute movement commands.

Selection and deputization are separate states.

- A pawn can be selected without being deputized.
- A pawn must be selected before it can be deputized.
- A pawn can remain deputized even if it is no longer selected.
- Commands only apply to pawns that are both selected and deputized at the time of the command.

## Command Flow
The V1 movement flow is:

1. Select pawn(s)
2. Deputize pawn(s)
3. Hover a tile to preview move targets
4. Click a tile to issue the move command
5. The system assigns destinations
6. Each pawn pathfinds and moves

## Selection Rules
V1 selection behavior:

- click pawn = single select
- shift-click pawn = add or remove from selection
- click empty space = clear selection

Focus does not need its own separate system in V1.

Focus can be derived from the current selection:
- 0 selected = no focus
- 1 selected = single-pawn focus
- 2+ selected = group focus

## Group Command Rules
For group movement, the system uses an anchor-based command model.

### Anchor Pawn
The first selected pawn is treated as the **anchor pawn**.

### Anchor Destination
The hovered or clicked tile is treated as the **candidate anchor destination**.

If that tile is invalid or unreachable, the system resolves it to the **nearest valid reachable tile** during hover preview.

The preview should show the resolved destination, not just the raw hovered tile.

### Additional Group Destinations
After the anchor destination is resolved:
- the anchor pawn is assigned that destination
- the remaining selected + deputized pawns are assigned the nearest valid unique reachable tiles around the anchor destination
- destination assignment searches outward from the anchor tile

This means the player commands the group to move to an area, while the system resolves exact per-pawn destination tiles.

## Destination Validity
A tile is a valid destination only if it is:
- inside bounds
- passable / walkable
- not blocked by natural content or other blocking world content
- not already assigned to another pawn in the same command
- reachable for that pawn through pathfinding

## Failure Rules
The anchor pawn must be able to receive a valid reachable destination.

- if the anchor pawn cannot receive a valid reachable destination, the command fails
- if the anchor pawn can move, partial success is allowed for the rest of the group
- non-anchor pawns only move if they also receive valid reachable destination assignments

## Pathfinding Rules
V1 uses **A*** pathfinding.

Pathfinding rules:
- 8-directional traversal is supported
- diagonal movement is allowed only when:
  - the diagonal target tile is passable
  - both touching orthogonal side tiles are also passable
- diagonal corner-cutting is not allowed

All traversable tiles are treated equally in V1.

Terrain costs such as grass, mud, roads, or slowing effects are out of scope for this version.

## Movement Style
Movement should feel smooth.

Pawns do not teleport from tile to tile.  
Instead, they move smoothly between tile centers along their path.

## Occupancy and Movement Conflict Rules
V1 uses a simple tile occupancy rule during movement:

- a pawn occupies its current tile
- before advancing, it attempts to reserve its next tile
- once the move completes, the previous tile is released

If the next tile becomes unavailable:
- the pawn does not just wait in place
- it re-runs A* from its current tile to its assigned destination
- if a new path exists, it continues moving
- if no path exists, that pawn stops cleanly

## Preview Rules
The movement preview should:
- show the resolved anchor destination
- show assigned destination tiles for the rest of the commanded group
- make it clear when a hovered tile is invalid and no fallback destination exists

The preview should match the final command behavior.  
What the player sees before clicking should be what the system actually uses.

## V1 Scope Boundary
This version is focused on proving pawn command and movement foundations only.

Included in V1:
- selection + deputization command gate
- hover preview
- resolved destination fallback
- group destination assignment
- A* pathfinding
- smooth movement
- 8-directional traversal
- no corner-cutting
- re-pathing when blocked

Not included in V1:
- terrain movement weights
- advanced group formations
- combat movement
- task/job execution
- autonomous colonist decision-making
- advanced crowd simulation

## Summary
V1 movement is built around a simple but scalable rule set:

- players select pawn(s)
- players deputize pawn(s)
- only selected + deputized pawns receive commands
- hover preview resolves the best valid movement area
- click commits the move command
- each pawn gets a valid assigned destination
- each pawn pathfinds with A*
- each pawn moves smoothly
- if blocked, a pawn re-runs A* instead of just waiting

This creates the first playable movement foundation for future colonist, task, and colony systems.
