# World Context Manager — V1 Planning

## Purpose

This document defines the first-pass design for the World Context Manager, which provides a unified simulation layer for time and environmental context.

The purpose of this system is to create a single source of truth for world state over time so that future systems such as pawn AI, tasking, survival mechanics, and environmental systems can operate consistently.

This system does not implement gameplay behavior.
It only provides context that other systems will read from.

---

## High-Level Goal

The World Context Manager should:

- advance time continuously during play
- track date and calendar progression
- determine current season
- determine day/night state
- calculate temperature
- expose this data to other systems
- support time scaling and pause controls
- allow player interaction while simulation is paused

---

## Core Design Rule

The World Context Manager is the only system that owns time.

- other systems must not calculate or store their own time
- all simulation systems read from World Context
- World Context is authoritative and deterministic

---

## System Responsibilities

### Time Progression

- track minutes, hours, days, months, and years
- advance time based on a configurable real-time scale
- correctly roll over time units

---

### Calendar

- 60 minutes per hour
- 24 hours per day
- 30 days per month
- 12 months per year

---

### Season

- Spring → months 3, 4, 5
- Summer → months 6, 7, 8
- Fall → months 9, 10, 11
- Winter → months 12, 1, 2

---

### Day/Night State

- Day → 06:00 to 19:59
- Night → 20:00 to 05:59

---

### Temperature

Derived from:

- season
- time of day

---

### Time Controls

- Pause
- Play
- 1x speed
- 2x speed
- 3x speed

---

### World Pause Behavior

Simulation systems stop:

- time progression
- pawn AI
- task evaluation
- movement
- work progress

Player interaction continues:

- camera movement
- selection
- issuing commands
- UI/debug tools

---

### Command Behavior While Paused

- commands can be issued
- commands are stored
- execution happens when unpaused

---

## Visual System Integration

Separate system: WorldVisualController

Responsibilities:

- lighting updates
- ambient color
- global tint
- day/night transitions

---

## Time Scale

- 1 day = 20 minutes (normal)
- Debug: 2 minutes
- Stress: 30 seconds

---

## Included in V1

- time progression
- calendar
- season
- day/night
- temperature
- pause/play
- time scaling

---

## Out of Scope

- weather
- pawn needs
- farming systems
- advanced lighting
- events

---

## Architecture

World Layer:

- Grid
- World Generator
- World Context Manager

Gameplay Layer:

- Work Orders
- Tasks
- Pawn Brain
