# PROJECT_TARGETS.md

# Project Vision

The application is a cinematic interactive kaleidoscope system intended for standalone production release.

The project should feel alive, fluid, glossy, artistic, reactive, and visually premium.

The kaleidoscope should create emotional “wow” moments rather than looking like a technical demo.

---

# Core Interaction Goals

## Space

Image shake impulse.

---

## Left / Right Arrow

Mirror rotation acceleration.

- Hold duration affects acceleration strength.
- Speed accumulates up to ±300.
- After release:
  - rotation persists;
  - after 10 seconds smoothly decays back to baseline speed 5.

Rotation should never completely stop unless explicitly requested.

---

## Up / Down Arrow

### Procedural Crystal Mode

Continuous zoom behaves like a living fractal system.

Image must continuously evolve during zoom.

---

### User Image Mode

Mobius-film behavior.

Image behaves like a moving optical ribbon or film strip.

Up:
- inward camera movement;
- outward image flow.

Down:
- reverse flow.

---

# Display Controls

## Insert / Delete

Switch display modes.

---

## Page Up / Page Down

Switch quality levels.

---

## Home / End

Switch render resolution presets.

---

# Numeric Keypad

## Plus / Minus

Double or halve mirror count.

---

## 1 / 2 / 3

Presets:
- 6 mirrors
- 12 mirrors
- 24 mirrors

---

## 4 / 5 / 6

- fast spin positive
- immediate stop
- fast spin negative

---

## 7

Cinematic pulse preset.

---

## 8

Crystal density/detail preset.

---

## 9

Random beauty-shot preset.

---

## 0

Image inversion.

---

# Ctrl Shortcuts

## Ctrl + A

Automatic mode.

---

## Ctrl + F

Auto-optimize visual parameters.

---

# Flight Controls

## W A S D

Shift kaleidoscope center to simulate cinematic flight.

---

# Procedural Crystal Mode

Transparent colored crystals should feel:

- layered
- refractive
- alive
- fractal
- deep
- evolving

Avoid static repetitive texture feeling.

---

# User Image Mode

User image should remain recognizable and visually enhanced.

Avoid destructive filtering.

---

# Menu Goals

Preserve and modernize existing menu.

---

# Help Goals

Help section must contain all current keyboard controls.

---

# Statistics Goals

Statistics panel should display:

- FPS
- mirrors
- speed
- zoom
- quality
- resolution
- source mode
- auto mode
- filter preset
- inversion state

---

# LM Studio Goals

LM Studio integration must:

- be isolated in dedicated panel;
- fail safely;
- never break visual runtime if unavailable.

---

# Production Goals

Application should become export-ready standalone software.