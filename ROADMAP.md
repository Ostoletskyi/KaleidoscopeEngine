# ROADMAP.md

# Kaleidoscope Production Roadmap

Work must be done stage by stage.

Do not skip stages.
Do not implement future stages early.
Do not modify systems outside the current task scope.

---

## Stage 00 — Audit

File:

`TASK_00_AUDIT.md`

Goal:

Inspect the project without code changes.

Exit criteria:

- architecture summary completed;
- risk areas identified;
- affected files listed;
- safe implementation order confirmed.

---

## Stage 01 — Input System

File:

`TASK_01_INPUT_SYSTEM.md`

Goal:

Restore and stabilize keyboard controls.

Exit criteria:

- Space shake works;
- Left/Right acceleration works;
- speed decays to baseline 5;
- Insert/Delete work;
- PageUp/PageDown work;
- Home/End work;
- Numpad controls work;
- Ctrl+A and Ctrl+F work.

---

## Stage 02 — Camera and Flight

File:

`TASK_02_CAMERA_AND_FLIGHT.md`

Goal:

Restore cinematic movement.

Exit criteria:

- W/A/S/D movement works;
- zoom is smooth;
- center offset is stable;
- movement feels like flight, not dragging.

---

## Stage 03 — Filter Pipeline

File:

`TASK_03_FILTER_PIPELINE.md`

Goal:

Make the image glossy, rich, cinematic, and premium.

Exit criteria:

- filters no longer dirty the image;
- color is richer;
- contrast is controlled;
- bloom/gloss is tasteful;
- user image remains readable.

---

## Stage 04 — Procedural Crystals

File:

`TASK_04_PROCEDURAL_CRYSTALS.md`

Goal:

Improve procedural crystal/fractal mode.

Exit criteria:

- crystals look transparent and gem-like;
- zoom reveals evolving detail;
- image does not feel static or repetitive.

---

## Stage 05 — User Image Mode

File:

`TASK_05_USER_IMAGE_MODE.md`

Goal:

Improve user image behavior and Mobius-film movement.

Exit criteria:

- user image remains recognizable;
- Up/Down create optical ribbon movement;
- distortion is controlled.

---

## Stage 06 — UI, Help, Statistics

File:

`TASK_06_UI_HELP_STATS.md`

Goal:

Update menu, help, and statistics.

Exit criteria:

- menu preserved;
- help lists all controls;
- statistics panel shows key runtime values.

---

## Stage 07 — LM Studio Panel

File:

`TASK_07_LM_STUDIO_PANEL.md`

Goal:

Isolate and improve AI features.

Exit criteria:

- LM Studio panel exists;
- unavailable server does not crash app;
- AI tools do not pollute main UI.

---

## Stage 08 — Production Build

File:

`TASK_08_PRODUCTION_BUILD.md`

Goal:

Prepare standalone export.

Exit criteria:

- no compile errors;
- build settings checked;
- standalone build validated;
- runtime controls work in build.