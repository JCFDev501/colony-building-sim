# Player Controller Foundation Scope

## Goal

Create a simple player controller that allows the player to interact with the grid for testing and future gameplay systems.

## Purpose

Handle basic mouse-based interaction with the grid so the player can inspect and select tiles during testing.

## Why It Matters

Now that the grid system is complete, the prototype needs a basic way for the player to interact with tiles. This creates a foundation for inspection, selection, and later gameplay features without adding unnecessary complexity too early.

## Responsibilities

- detect the tile currently under the mouse
- provide simple hovered tile feedback
- allow tile selection with left click
- provide simple selected tile feedback

## Supported Inputs

- mouse movement
- left click

## Expected Feedback

- hovered tile can be identified
- basic tile info can be viewed during testing
- hovered tile has clear visual feedback
- selected tile can be identified through simple visual or debug feedback

## In Scope

- reading hovered tile from the existing grid system
- displaying basic tile information
- highlighting the hovered tile
- selecting a tile with left click
- keeping the system simple and easy to expand later

## Out of Scope

- pathfinding
- player movement between tiles
- job systems
- building placement
- combat
- advanced UI
- drag selection
- multi-unit control

## Prototype Notes

This controller is not meant to be a full gameplay controller. Its purpose is to support simple grid interaction during prototype development. The focus should remain on inspection and selection only.

## Summary

The player controller foundation should do one thing well: let the player hover and select tiles in a clear, testable way.
