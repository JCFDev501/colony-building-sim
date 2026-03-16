# Player Controller Behavior Breakdown

## Overview

The player controller handles simple mouse-based interaction with the grid. Its main responsibilities are hover detection and tile selection.

## Hover Behavior

When the mouse moves, the controller should:

1. read the mouse position
2. convert it to a world position
3. ask the grid for the tile at that position
4. store that tile as the hovered tile if valid
5. update hover feedback only when the hovered tile changes
6. clear hover feedback when the mouse is not over a valid tile

## Selection Behavior

When the player left-clicks, the controller should:

1. check whether a valid tile is currently hovered
2. store the hovered tile as the selected tile
3. update selection feedback

If no valid tile is hovered, no selection change should happen.

## Hover vs Selected Tile

- Hovered tile is the tile currently under the mouse
- Selected tile is the tile the player clicked
- Hover is temporary
- Selection remains until changed by another valid click

## Feedback Expectations

Hover feedback should:

- identify the currently hovered tile
- show basic tile data
- provide simple visual feedback

Selection feedback should:

- identify the selected tile
- provide simple debug or visual confirmation

## Invalid Space Behavior

If the mouse is not over a valid tile:

- hovered tile should be cleared
- hover feedback should be removed
- selected tile should remain unchanged

## State

The controller only needs to track:

- hovered tile
- selected tile

## Summary

The controller should continuously update hover state based on mouse position and allow left-click selection of valid tiles.
