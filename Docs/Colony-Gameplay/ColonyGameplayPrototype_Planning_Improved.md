# Colony Gameplay Prototype Planning Notes

## Purpose
This document captures the current planning decisions for the next prototype phase after the world-generator foundation is in place.

The next phase is focused on proving that pawns are no longer just movable units, but autonomous workers that can respond to colony work through a tasking system.

This pass assumes the world already supports generated terrain, generated water, and generated world content such as trees.

---

## What the next pass should prove

The next pass should prove:

- the player can create work in the world
- work can exist as persistent colony goals
- pawns can evaluate available work autonomously
- pawns can choose what to do based on per-pawn priorities
- pawns can move to work and perform it
- if no valid work exists, pawns can wander locally instead of freezing

This is the first real step from movement prototype into colony gameplay prototype.

---

## High-level behavior rules

### Autonomous vs deputized
- Pawns are autonomous by default.
- If a pawn is deputized, the brain is disabled.
- Deputized pawns ignore autonomous task selection and wait for player commands.
- Non-deputized pawns evaluate available tasks and choose work.

### No work fallback
- If a non-deputized pawn has no valid task, it should wander.
- Wandering should stay within a local tunable range.
- Wandering should use the normal pathfinding system.
- Pawns should reevaluate for tasks when they become idle again, such as after reaching a wander destination.

---

## Work priority system

### Per-pawn priorities
Work priorities are stored per pawn.

### V1 work types
Work types for the first pass:

0. Harvest
1. Cut
2. Construct
3. Grow
4. Cook

### V1 work-type definitions
The first pass needs clear separation between similar world-interaction jobs.

#### Harvest
Harvest means collecting from a world target that is meant to yield resources without being treated as a tree-cutting job.

Examples:
- harvest a crop
- harvest a berry bush
- gather from a harvestable plant node

Harvest should be used for gatherable or collectible world targets.

#### Cut
Cut means removing a tree or other cuttable natural world object that primarily represents wood-producing clearing work.

Examples:
- cut a tree
- cut a dead log later if supported

For the first pass, Cut should primarily represent tree removal.

#### Construct
Construct means building or advancing a player-placed structure work order on a valid buildable tile.

#### Grow
Grow means farming or plant-growth-related work when that exists.

#### Cook
Cook means processing ingredients or food at a valid cooking target.

### Core work-type rule
Harvest is for harvestable gather targets.
Cut is for cuttable tree-style targets.
These should not be treated as interchangeable labels.

### Default priorities
All five work types start at priority 4 for every pawn.

### Priority rules
- Lower number is better.
- Priority 1 is highest.
- Priority 4 is lowest.
- If two work types share the same priority value, the lower index wins.

### Default fallback order
Because all work types start at 4, the fallback order for an untouched pawn is:

1. Harvest
2. Cut
3. Construct
4. Grow
5. Cook

### Future expansion
Later, work types may be disabled or restricted based on things like:
- backstories
- traits
- injuries
- other pawn-specific rules

That is out of scope for the first pass.

---

## Work orders vs tasks

### Work order
A work order is the colony goal or player request.

Examples:
- build a wall here
- harvest this crop
- cut this tree

A work order represents the requested outcome.

### Task
A task is the immediate actionable step a pawn can perform right now.

Examples:
- bring wood to a wall site
- build the wall now that materials are ready
- harvest the crop

A task represents the immediate action.

### Core rule
Work orders represent what the player wants completed.
Tasks represent what a pawn can actually do right now.

### Shared completion
One work order may be advanced by multiple tasks and potentially multiple pawns over time.

Example:
- one pawn delivers some wood
- another pawn delivers the remaining wood
- another pawn constructs the wall

---

## Work order model

### Generic work order object
Use one generic work-order data object for all tile-based work.

This generic work order should hold the core data needed for any player-marked job on a tile.

### Suggested work-order data
At a planning level, a work order should know:

- work type
- target tile
- current state
- required materials
- delivered materials
- progress
- claimed or assigned data as needed later

### Material data
A work order should track:
- required materials
- delivered materials

### Material commitment rule
Once materials are delivered to a work order, they are committed to that order.

For the first pass:
- no taking delivered materials back out
- no reclaim logic yet

---

## Work-order states

First-pass work-order states:

- Planned
- WaitingForMaterials
- Ready
- InProgress
- Complete
- Cancelled

### State transition rules

#### Planned
- If the order needs no materials, it becomes Ready.
- If the order has unmet material requirements, it becomes WaitingForMaterials.

#### WaitingForMaterials
- Once all required materials are delivered, it becomes Ready.

#### Ready
- When a pawn begins working on it, it becomes InProgress.

#### InProgress
- When work finishes, it becomes Complete.

#### Any state
- If the player removes it or it becomes invalid, it becomes Cancelled.

---

## Task model

### Who creates tasks
The task system is responsible for creating and updating tasks from work orders.

The pawn brain does not create tasks.
The pawn brain only reads available tasks and chooses among them.

### V1 task types
Task types for the first pass:

- Harvest
- Cut
- Construct
- Grow
- Cook
- DeliverMaterials

### DeliverMaterials rule
DeliverMaterials does not get its own separate pawn priority yet.

Instead, DeliverMaterials inherits the priority of the linked work order's work type.

Examples:
- delivering wood for a wall uses Construct priority
- delivering ingredients for cooking uses Cook priority

### Suggested task data
At a planning level, a task should know:

- task type
- linked work order
- target tile
- state
- claimed pawn
- action amount or duration as needed

### Task availability rules
A task should be considered available only if:

- it is not complete or cancelled
- it is not already claimed by another pawn
- the parent work order is in the correct state for that task
- the task still has a valid target
- the pawn is allowed to consider that work type
- it is valid enough for the pawn to attempt

### Task claim rule
A pawn claims a task as soon as it chooses it.

This avoids multiple pawns trying to take the same task at once.

### Task release conditions
A claimed task is released if:

- the parent work order is cancelled
- the task becomes invalid
- the pawn can no longer reach or perform it
- the pawn becomes deputized
- the pawn is interrupted by a higher-level mode change
- the task completes

---

## World target validity rules

The colony gameplay pass now depends on generated terrain and generated world content.
Because of that, task and work-order validity need clearer target rules.

### Core target rule
A task target is not valid just because a tile exists.
A task target is valid only if the tile and the world state match the work being requested.

### Cut target validity
A Cut target is valid only if:
- the target tile contains cuttable world content
- the content is currently treated as a tree or other cuttable natural object
- the target has not already been removed or invalidated
- the target tile is reachable enough for the pawn to attempt the work

### Harvest target validity
A Harvest target is valid only if:
- the target tile contains harvestable world content
- the content is currently in a harvestable state
- the target has not already been consumed, removed, or invalidated
- the target tile is reachable enough for the pawn to attempt the work

### Construct target validity
A Construct target is valid only if:
- the target tile is build-valid for the requested structure
- the tile is not water
- the tile is not blocked by incompatible world content
- the work order is still active
- the target is reachable enough for the pawn to attempt the work

### Future target expansion
Later work types may define more specific target rules, but the first pass should already separate target validity by work type rather than treating all tile targets the same.

---

## Build-valid terrain rules

Now that terrain exists, construction needs a clear first-pass terrain rule.

### First-pass build-valid rule
For the first pass, structures should only be placeable on valid land tiles.

### V1 build-valid terrain
By default, these terrain types should be considered build-valid:
- Grass
- Dirt
- ForestFloor

### V1 build-invalid terrain
By default, this terrain type should be considered build-invalid:
- Water

### Additional build constraints
Even on valid terrain, a tile may still be build-invalid if:
- it is occupied by a pawn
- it contains blocking world content that has not been cleared
- it is reserved by incompatible work as defined later
- the target structure has additional placement rules later

### Why this stays simple
The first pass should keep construction rules readable.
It only needs enough build-validity structure to support prototype placement and future work-order/task checks.

---

## World content specificity direction

The current world model separates terrain, content, and gameplay state.
That is the correct direction, but the colony gameplay phase needs slightly more specificity when evaluating work.

### Current issue
A broad content label such as Resource is not enough by itself to answer:
- whether the target is a tree
- whether the target is harvestable
- whether the target is cuttable
- whether the target has already been depleted
- whether the target should generate a Cut task or a Harvest task

### V1 planning direction
The first colony gameplay pass should introduce enough world-target specificity to let task generation and target validation reason about actual world objects.

This does not require a giant simulation model.
It only requires enough data to distinguish task-relevant target kinds.

### Minimum future-facing need
The system should be able to distinguish at least:
- tree-style cut targets
- harvestable gather targets
- construction targets

### Practical rule
Broad tile content categories may remain for high-level state, but task generation should be able to inspect a more specific target identity than ContentType alone when needed.

This can be implemented later using a lightweight world object, resource descriptor, or similar world-content data layer.
The exact implementation does not need to be locked in by this planning document.

---

## Brain evaluation rule

### Core brain rule
The pawn brain chooses a work type first, then chooses a specific task inside that work type.

It does not compare every individual task in the world all at once for the first pass.

### First-pass decision flow
For a non-deputized pawn:

1. Gather all valid available tasks
2. Group tasks by work type
3. Compare work types using the pawn's per-pawn priorities
4. Lower priority number wins
5. If priority ties, lower work-type index wins
6. Choose the winning work type
7. Choose a specific task within that work type
8. Claim the task
9. Execute it

### If no valid task exists
If no valid task exists:
- choose a local wander destination
- pathfind to it
- reevaluate when idle again

---

## Recommended future structure

### Responsibility split
A clean long-term split should be:

- Work order system: stores colony goals
- Task system: generates and updates tasks from work orders
- Pawn brain: chooses among available tasks
- Pawn execution: moves and performs the chosen task

### Execution flow
A simple first-pass pawn state flow may later look like:

- Idle
- ChoosingTask
- MovingToTask
- PerformingTask
- Wandering

This is planning only and not yet an implementation commitment.

---

## Pre-tasking cleanup work

Before adding the pawn brain and full tasking logic, the Pawn file should be cleaned up.

The current Pawn script already carries several responsibilities, including:
- selection state
- deputization state
- grid position
- movement
- occupancy behavior
- preview visuals
- state indicator visuals

It should be cleaned up before adding brain/state-machine logic.

---

## Current locked clarifications

The current clarified planning decisions are:

- Harvest and Cut are separate work types with different target meanings
- Construct should use simple build-valid terrain rules
- Water is build-invalid in V1
- Task and work-order validity must check world-target type, not just tile existence
- The colony gameplay pass will eventually need more specific world-target identity than ContentType alone
- The exact implementation for richer world-target specificity is still open

---

## Recommended next implementation step
Create a cleanup/refactor ticket for the Pawn architecture before beginning the autonomous tasking system.
