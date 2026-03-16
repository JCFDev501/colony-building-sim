# Player Controller Hover Flow

## Overview

The player controller should determine the hovered tile by converting mouse position into a world position and then asking the GridManager for the tile at that location.

## Hover Flow

Each frame, the controller should:

1. read the current mouse position
2. create a ray from the camera through the mouse position
3. determine where that ray hits the game world or grid plane
4. pass the world hit position to the GridManager
5. receive the tile coordinate for that position
6. determine whether the returned tile is valid
7. update hovered tile state only if the hovered tile changed

## Valid Hover Behavior

If the mouse points to a valid tile:

- store that tile as the hovered tile
- update hover feedback only if the tile changed

## Same-Tile Behavior

If the mouse remains over the same tile:

- do not reprocess hover state
- do not repeat hover feedback unnecessarily

## Invalid Hover Behavior

If the mouse is not over a valid tile:

- clear hovered tile state
- remove hover feedback

## Responsibility Split

- PlayerController reads input and manages hover state
- GridManager converts world position into tile coordinates

## Summary

The hover system should follow this path:
mouse position -> world hit position -> grid tile lookup -> hover state update
