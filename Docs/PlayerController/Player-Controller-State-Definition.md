# Player Controller State Definition

## Overview

The PlayerController should only store the minimum data needed to interact with the grid. Its state should stay small and focused on hover and selection.

## Required References

The PlayerController should store:

- a reference to the GridManager
- a reference to the camera used for mouse-to-world lookup

## Hover State

The PlayerController should store:

- the currently hovered tile coordinates
- a flag indicating whether a valid hovered tile exists

This is needed so hover feedback can be updated only when the hovered tile changes.

## Selection State

The PlayerController should store:

- the currently selected tile coordinates
- a flag indicating whether a valid selected tile exists

This is needed so tile selection can persist after hover changes.

## Optional Prototype Fields

Later, the PlayerController may also store:

- debug toggles
- hover visual references
- selection visual references

These are optional and should only be added when needed.

## Out of Scope State

The PlayerController should not store:

- full grid data
- pathfinding data
- occupancy data for all tiles
- building data
- movement paths
- job or AI data

These belong to other systems.

## Notes

Tile coordinates such as (0, 0) may be valid, so the controller should not use coordinate values alone to represent “no tile.” Separate validity flags should be used for hover and selection state.

## Summary

The PlayerController only needs references, hovered tile state, and selected tile state.
