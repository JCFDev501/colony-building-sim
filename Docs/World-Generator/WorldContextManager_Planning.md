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
- track date and simplified calendar progression
- determine current season
- determine time-of-day phase
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

V1 uses a simplified, game-friendly calendar:

- 60 minutes per hour
- 24 hours per day
- 15 days per month
- 4 months per year
- 60 days per year

Each month represents a season.

---

### Month & Season

The world uses named months that represent seasonal cycles:

- **Bloomtide** → Spring
- **Suncrest** → Summer
- **Harvestfall** → Fall
- **Frostwane** → Winter

Month progression represents seasonal progression.

Example:

- Day 3, Suncrest → Early Summer
- Day 14, Frostwane → Late Winter

Season is derived from the current month and must not be set independently.

---

### Time of Day Phase

Time is divided into phases for better AI and visual control:

- EarlyMorning → 05:00 - 07:59
- Morning → 08:00 - 11:59
- Afternoon → 12:00 - 16:59
- Evening → 17:00 - 20:59
- Night → 21:00 - 23:59
- LateNight → 00:00 - 04:59

This replaces simple day/night state.

---

### Temperature

Temperature is derived from:

- current season
- time of day phase

General rules:

- warmer during day phases
- cooler during night phases
- each season has its own baseline range

---

### Time Controls

- Pause
- Play
- 1x speed
- 2x speed
- 3x speed

---

### World Pause Behavior

World pause affects simulation only.

Simulation systems stop:

- time progression
- pawn AI
- task evaluation
- movement
- work progress
- environmental updates

Player interaction continues:

- camera movement
- selection
- issuing commands
- UI/debug tools

---

### Command Behavior While Paused

- commands can be issued while paused
- commands are stored
- execution happens when unpaused

This allows pause to function as a planning mode.

---

## Visual System Integration

Separate system: **WorldVisualController**

Responsibilities:

- update directional lighting
- update ambient lighting
- apply global color tint
- respond to time-of-day phase changes
- handle smooth transitions between phases

---

## Time Scale

**Normal:**

- 1 in-game day = 20 real minutes

**Debug:**

- 1 day = 2 minutes

**Stress Test:**

- 1 day = 30 seconds

---

## Included in V1

- time progression
- simplified calendar
- season calculation
- time-of-day phase system
- temperature calculation
- pause/play system
- time scaling
- visual hooks

---

## Out of Scope

- weather systems
- pawn needs (hunger, tiredness, health)
- farming systems
- AI scheduling/sleep systems
- advanced lighting systems
- biome simulation
- event systems

---

## Architecture

**World Layer**

- Grid System
- World Generator
- World Context Manager

**Gameplay Layer**

- Work Orders
- Tasks
- Pawn Brain

---

## Why This System Matters

This system becomes the foundation for all time-based simulation.

It enables:

- consistent AI decision-making
- scalable environmental systems
- proper colony simulation pacing
- future survival and seasonal systems

Without it, time logic becomes fragmented and difficult to maintain.
