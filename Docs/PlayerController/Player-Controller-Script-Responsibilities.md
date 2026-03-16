# Player Controller Script Responsibilities

## Overview

The player controller should use the existing grid system rather than replacing or duplicating its logic. Responsibilities should stay split between grid data and player interaction.

## GridManager Responsibilities

The GridManager should be responsible for:

- storing grid settings
- storing tile data
- converting between world positions and tile coordinates
- validating tile coordinates
- returning tile data when requested
- handling grid debug drawing if needed

The GridManager should not handle player input, hover state, or tile selection.

## PlayerController Responsibilities

The PlayerController should be responsible for:

- reading mouse input
- determining the tile currently under the mouse
- storing the hovered tile
- storing the selected tile
- detecting when hover changes
- handling tile selection on click
- providing simple debug or visual feedback

## State Ownership

The PlayerController should own:

- hovered tile state
- selected tile state

The GridManager should own:

- grid data
- tile data
- tile lookup logic

## Communication Flow

The PlayerController should ask the GridManager for tile information.

Flow:

1. read player input
2. determine mouse world position
3. ask GridManager for the tile at that position
4. update hover or selection state based on the result

## Recommended Prototype Setup

Use only:

- GridManager
- PlayerController

This keeps the prototype simple and avoids unnecessary systems too early.

## Summary

GridManager owns the grid.
PlayerController owns player interaction with the grid.
