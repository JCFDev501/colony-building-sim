# Player Controller Hover and Selection Rules

## Overview

This document defines how hover and selection behave together in the prototype player controller.

## Hovered Tile

- The hovered tile is the tile currently under the mouse cursor.
- Hover is temporary and changes as the mouse moves.
- If the mouse is not over a valid tile, there is no hovered tile.

## Selected Tile

- The selected tile is the last valid hovered tile that was left-clicked.
- Selection persists until another valid tile is selected.
- Only one tile can be selected at a time.

## Feedback Rules

### Hover Feedback

- Hovered tile debug text is shown during testing.
- Hovered tile highlight is shown only while a valid tile is hovered.

### Selection Feedback

- Selected tile debug text is shown during testing.
- Selected tile highlight remains visible on the selected tile until selection changes.

## Click Behavior

- Left click on a valid hovered tile selects that tile.
- Left click on invalid or empty space does nothing.
- Clicking a different valid tile updates the current selection.

## Testing Expectations

- Hover can change without changing selection.
- Selection remains after the mouse moves away.
- Hover and selection may refer to different tiles at the same time.

## Summary

Hover is temporary.
Selection is persistent.
Empty-space clicks do not change selection.
