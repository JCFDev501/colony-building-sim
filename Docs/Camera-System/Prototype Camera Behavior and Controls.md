## Goal

Define how the prototype camera should behave for movement, zoom, rotation, and viewing angle so it supports readable colony management on the grid-based prototype map. This should stay aligned with the prototype’s focus on simple gameplay foundations over advanced presentation.

## Scope

- decide base camera perspective
- decide movement inputs
- decide zoom behavior
- decide rotation behavior
- define simple prototype control rules
- define what is fixed versus tunable

## Camera Design Decisions

### 1. Base camera perspective

The prototype camera will use a **perspective camera** with a **fixed downward tilt**.

The camera should feel like a tactical colony-sim camera, not a first-person or character-follow camera. A perspective view gives better depth and world readability for a 3D colony prototype while still keeping the map easy to manage.

#### Decision

- camera type: **Perspective**
- view style: **angled top-down**
- pitch: **fixed**
- starting pitch target: **around 50 degrees**
- camera looks down at the colony from an elevated angle
- pitch is **not player-controlled** in the first version

#### Why

This supports the prototype goal of managing pawns, work, and environmental interaction on a grid while keeping implementation simple. The prototype is focused on gameplay foundations, not advanced presentation systems.

### 2. Movement inputs

The player will move the camera using **WASD**.

Movement should slide the camera across the world on the ground plane so the player can inspect different parts of the colony area.

#### Decision

- input: **WASD**
- optional support: **Arrow Keys** can mirror WASD if desired
- movement happens on the **horizontal world plane**
- movement is relative to the camera’s facing direction
- no vertical free-fly movement

#### Control rule

- **W** moves forward relative to camera facing
- **S** moves backward relative to camera facing
- **A** moves left relative to camera facing
- **D** moves right relative to camera facing

#### Why

This is simple, familiar, and enough for the prototype. It supports grid navigation and future interaction work without adding unnecessary complexity.

### 3. Zoom behavior

The player will zoom using the **mouse wheel**.

Zoom should change the camera’s distance from its pivot rather than relying only on field of view changes.

#### Decision

- input: **Mouse Scroll Wheel**
- zoom method: **move camera closer to or farther from pivot**
- zoom is **smooth**
- zoom has **minimum and maximum limits**
- field of view stays fixed in the first version

#### Control rule

- scroll up: zoom in
- scroll down: zoom out
- camera cannot zoom past defined min/max range

#### Why

Distance-based zoom gives a stronger colony-management feel and keeps the scene readable at different scales. It is also easier to reason about during grid interaction testing.

### 4. Rotation behavior

The player will rotate the camera using **Q** and **E**.

Rotation should happen around the camera rig’s pivot/root, not by rolling or changing pitch.

#### Decision

- input: **Q / E**
- **Q** rotates left
- **E** rotates right
- rotation affects **yaw only**
- pitch remains fixed
- roll is never used
- rotation is **smooth** in the first prototype

#### Why

This gives the player enough control to improve readability around buildings and world layout without making the camera feel complex. The prototype only needs enough control to support management gameplay and future tile interaction.

### 5. Simple prototype control rules

The camera should stay easy to learn and easy to test.

#### Rules

- the camera always stays in an angled top-down presentation
- the player can move, zoom, and rotate at any time
- pitch does not change during normal play
- the camera does not tilt, roll, or enter cinematic modes
- the camera is meant for colony overview and local inspection, not for action gameplay
- controls should remain consistent and predictable while interacting with the grid

#### Interaction expectation

The camera should remain readable enough that hovered tiles, selected tiles, building placement, and future command interactions can all be tested cleanly on the grid foundation already completed.

### 6. Fixed vs tunable values

## Fixed for prototype v1

These should be treated as design constraints for the first version:

- perspective projection
- angled top-down style
- fixed pitch
- yaw-only rotation
- keyboard movement
- mouse-wheel zoom
- no free-look
- no character-follow mode
- no cinematic camera behavior

## Tunable in Inspector

These should be exposed for iteration:

- move speed
- zoom speed
- rotate speed
- min zoom distance
- max zoom distance
- starting zoom distance
- starting pitch value
- map bounds values
- smoothing values if used

#### Why

The prototype is meant to be tunable and easy to iterate on in Unity, just like the grid system. Inspector tuning supports quick testing and keeps implementation practical.

## Prototype Constraints

To keep this ticket aligned with the prototype boundaries, the following are intentionally out of scope for this first camera version:

- edge scrolling
- drag-to-pan
- middle-mouse orbit
- dynamic occlusion handling
- wall fade/transparency
- camera collision handling
- multiple camera presets
- cinematic transitions
- follow camera modes
- pitch adjustment by the player
- advanced smoothing polish

## Implementation Notes

The recommended rig structure is:

- **CameraRoot**  
  handles movement and rotation

- **CameraPivot**  
  holds the angled camera relationship if needed

- **Main Camera**  
  child object used for actual rendering and zoom distance

This keeps the system modular and easy to expand later.

## Expected Prototype Feel

The camera should feel:

- readable
- stable
- smooth
- responsive
- simple to control
- appropriate for managing a small colony

It should support the player’s role as a colony manager making decisions about work, needs, and space rather than directly controlling a character.

## Acceptance Criteria

- camera behavior is clearly documented
- movement input is decided as **WASD**
- zoom input is decided as **mouse wheel**
- rotation input is decided as **Q / E**
- base perspective is decided as **perspective angled top-down**
- prototype constraints are clearly defined
- fixed versus tunable values are clearly defined
- enough detail exists to implement the camera cleanly

## Short decision summary

For prototype v1, the camera will be:

- **Perspective**
- **Fixed angled top-down**
- **WASD movement**
- **Mouse-wheel zoom**
- **Q/E yaw rotation**
- **Fixed pitch**
- **Inspector-tunable speeds and limits**
