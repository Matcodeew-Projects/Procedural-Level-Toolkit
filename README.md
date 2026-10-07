# Procedural Level Toolkit

A modular Unity Editor toolkit for designing rooms, assembling level graphs, solving spatial layouts, previewing generated levels in 2D/3D, validating them, and building final level prefabs.

The toolkit is designed around a clear authoring pipeline:

```text
RoomDefinition
      ↓
Room Editor
      ↓
Room Prefab
      ↓
RoomModuleDefinition
      ↓
Level Graph
      ↓
Socket Connections
      ↓
Layout Solver
      ↓
SpatialLayoutData
      ↓
2D / 3D Preview
      ↓
Validation
      ↓
Level Prefab
```

The current V1 focuses on **manual room authoring + manual logical graph editing + automatic spatial assembly**.

---

## Features

### Room Editor

Create reusable logical rooms directly inside Unity.

- Grid-based room authoring
- Multiple room layers
- Custom cell types
- Cell grouping
- Room sockets
- Socket direction, role, type and width
- 2D editing workflow
- 3D room preview
- Resizable Editor panels
- Room prefab generation
- Generated / Manual prefab workflow

### Room Modules

`RoomModuleDefinition` connects the logical room data to the final 3D prefab used by levels.

A module contains:

```text
RoomModuleDefinition
├── RoomDefinition
├── 3D Prefab
├── World Units / Cell
└── Metadata
```

This keeps the logical room representation separate from its final visual prefab.

### Level Editor

Assemble rooms into complete levels through a logical graph.

- Room Module library
- Infinite graph canvas
- Infinite pan and zoom
- Resizable side panels
- Node selection and movement
- Logical room connections
- Socket assignment
- Connection validation
- Delete selected node/connection with `Delete`
- Directional keyboard navigation
- Undo support

### Layout Solver

Convert a logical graph into a real spatial layout.

The solver:

- Selects a root room
- Places connected rooms
- Rotates rooms in 90° increments
- Aligns connected sockets
- Detects impossible layouts
- Detects overlaps
- Supports branches
- Produces persistent `SpatialLayoutData`

Logical graph positions are intentionally separate from physical level positions.

```text
LevelNodeData.GraphPosition
        ≠
LevelModuleInstanceData.Position
```

### 2D Layout Preview

Inspect the actual solved spatial layout.

- Infinite canvas
- Pan
- Zoom
- Fit complete layout
- Room footprints
- Connection visualization

### 3D Level Preview

Preview the final room assembly without building the final prefab.

- Uses the same `LevelBuilder` as the final build
- Orbit camera
- Pan camera
- Zoom
- Fit complete level
- Uses Unity `PreviewRenderUtility`

The 3D preview and final prefab therefore share the same placement logic.

### Validation

The Level Editor includes a dedicated validation/build workflow.

Validation checks include:

- Missing graph nodes
- Missing Room Modules
- Missing Room Definitions
- Missing room prefabs
- Invalid room dimensions
- Invalid sockets
- Unassigned sockets
- Socket reuse
- Socket type compatibility
- Socket role compatibility
- Socket width compatibility
- Disconnected graphs
- Unsolved layouts
- Missing placements
- Duplicate placements
- Room overlaps
- Unresolved logical connections
- Invalid socket world alignment
- Invalid socket directions
- Inconsistent `World Units / Cell`

Errors block the final build.

Warnings are displayed but do not block the build.

### Level Build

Build a solved and validated level into a Unity prefab.

```text
LevelDefinition
      ↓
Validate
      ↓
LevelBuilder
      ↓
Generated Level Prefab
```

The generated prefab is stored back inside the `LevelDefinition`, allowing later rebuilds without selecting the path again.

---

# Installation

## From the repository

Clone or copy the toolkit into your Unity project's `Assets` folder:

```text
Assets/
└── ProceduralLevelToolkit/
```

Open the project and let Unity compile the assemblies.

The toolkit is currently developed with Unity 6.

---

# Quick Start

## 1. Create a Room Definition

Create a new room asset:

```text
Create
→ Procedural Level Toolkit
→ Room
→ Room Definition
```

Open the Procedural Level Toolkit window and go to the Room Editor.

Define the room dimensions and paint the desired cells.

---

## 2. Add Room Sockets

Sockets represent physical connection points between rooms.

A socket must be placed on the corresponding room boundary:

```text
North → top row
South → bottom row
West  → left column
East  → right column
```

The Room Editor grid uses:

```text
X increases from left to right.
Y increases from top to bottom.
```

Therefore:

```text
North → y = 0
South → y = Height - 1
West  → x = 0
East  → x = Width - 1
```

A socket can define:

- Direction
- Role
- Type
- Width
- Custom display name

### Socket Roles

```text
Any
Entry
Exit
```

Typical compatible pairs:

```text
Entry ↔ Exit
Exit  ↔ Entry
Any   ↔ Any / Entry / Exit
```

---

## 3. Build the Room Prefab

Generate the room prefab from the Room Editor.

Generated prefabs use two important roots:

```text
Room Prefab
├── Generated
└── Manual
```

### Generated

Managed by the toolkit.

It may be deleted and regenerated when rebuilding the room.

### Manual

Owned by the designer.

Use this section for:

- Decorations
- Lights
- Props
- Custom scripts
- Hand-placed objects

The toolkit preserves this hierarchy when the generated room content is rebuilt.

Do not manually place persistent custom content inside `Generated`.

---

## 4. Create a Room Module

In the Level Editor, open:

```text
Structure
→ Room Modules
→ Create / Update Module
```

Assign:

```text
Room Definition
3D Prefab
World Units / Cell
```

Then create or update the module.

`World Units / Cell` defines how one logical room cell maps to Unity world space.

All modules used by the same level currently need to use the same value.

Example:

```text
1 logical cell = 2 Unity meters

World Units / Cell = 2
```

---

## 5. Create a Level

Create a `LevelDefinition` from the Level Editor.

Then add Room Modules to the Structure graph.

```text
Room Module Library
        ↓
        +
        ↓
Graph Node
```

The same Room Module can be used multiple times in one level.

---

## 6. Connect Rooms

Select a node, then use `Shift + Click` on another node to create a logical connection.

Select the connection and assign the physical sockets.

Connection colors provide immediate feedback about socket validity.

---

## 7. Solve the Layout

Open the `Layout` tab.

Choose the root room and click:

```text
Solve Layout
```

The solver calculates:

- Room positions
- Room rotations
- Socket alignment
- Spatial connections

If a layout cannot be solved, the solver reports an explicit error instead of silently generating an invalid level.

---

## 8. Inspect the Layout

The Layout page contains both:

```text
2D Layout
3D Preview
```

The two views represent the same `SpatialLayoutData`.

### 2D controls

- Mouse wheel: zoom
- Middle mouse / configured pan input: move view
- `Fit`: frame the complete layout

### 3D controls

- Left mouse drag: orbit
- Middle mouse drag: pan
- Mouse wheel: zoom
- `Fit`: frame the complete level

---

## 9. Validate the Level

Open the `Build` tab.

The validator runs automatically and can also be executed manually with:

```text
Validate Level
```

A valid level displays:

```text
READY TO BUILD

Errors: 0
Warnings: ...
```

Errors must be fixed before a level can be built.

---

## 10. Build the Final Level Prefab

Use:

```text
Build Level Prefab
```

The first build asks for an output location.

The generated prefab is then stored in the `LevelDefinition`.

Future builds can use:

```text
Rebuild Level Prefab
```

to update the same output.

Use:

```text
Build As...
```

to select a different prefab output.

---

# Editor Controls

## Level Structure

```text
Left Click Node      Select node
Drag Node            Move node
Shift + Click Node   Connect from selected node
Click Connection     Select connection
Shift + Click Link   Delete connection
Delete / Backspace   Delete current selection
Arrow Keys           Navigate Node → Connection → Node
Mouse Wheel          Zoom graph
Middle Mouse         Pan graph
Alt + Left Mouse     Pan graph
Fit                   Frame graph
```

The Structure workspace is intentionally unbounded.

Graph coordinates are purely organizational and do not affect the physical level layout.

---

# Core Data Model

## Room

```text
RoomDefinition
├── Width / Height
├── RoomLayerData
├── CellData
├── CellGroupData
└── RoomSocketData
```

## Module

```text
RoomModuleDefinition
├── RoomDefinition
├── Prefab
├── World Units / Cell
└── Tags
```

## Level

```text
LevelDefinition
├── Metadata
├── Layout Root
├── LevelGraphData
├── SpatialLayoutData
├── Generated Prefab
└── Last Build Information
```

## Logical Graph

```text
LevelGraphData
├── LevelNodeData
└── LevelConnectionData
```

## Spatial Layout

```text
SpatialLayoutData
├── LevelModuleInstanceData
└── LevelSocketConnectionData
```

---

# Coordinate Conventions

## Room Editor Grid

```text
(0,0) ─────────────→ X
  │
  │
  │
  ↓
  Y
```

The top-left grid cell is `(0, 0)`.

## Unity World

The generated level maps logical coordinates into Unity as:

```text
Grid X → Unity X
Grid Y → Unity Z
```

Room rotation is performed around Unity's Y axis.

Spatial room rotations use 90° increments.

---

# Generated vs Manual Content

The toolkit deliberately separates generated content from designer-owned content.

```text
Prefab
├── Generated
└── Manual
```

This allows procedural regeneration without destroying manual edits.

Recommended workflow:

```text
Room Editor
    ↓
Generate Room Prefab
    ↓
Open Prefab
    ↓
Add props / decorations / lights under Manual
    ↓
Return to Room Editor
    ↓
Regenerate
    ↓
Manual content remains intact
```

---

# Tests

The toolkit includes EditMode regression tests covering critical systems.

Current coverage includes:

### Rooms

- Room resize
- Cell persistence
- Cell groups
- Socket boundaries
- Socket directions
- Room validation
- Save / reload persistence
- Generated / Manual prefab preservation

### Level Graph

- Node removal
- Incident connection removal
- Duplicate connection rejection

### Layout Solver

- Two-room placement
- Socket alignment
- Rotation
- Overlap prevention

### Level Validator

- Valid solved level
- Disconnected graph rejection
- Missing socket assignment rejection

### Level Builder

- Module instance count
- Position conversion
- Rotation conversion

Run tests from:

```text
Window
→ General
→ Test Runner
→ EditMode
→ Run All
```

These tests are intended to act as regression protection when modifying the toolkit.

They do not replace manual UX testing.

---

# Recommended Project Workflow

```text
dev
 ↓
Feature / fix commits
 ↓
EditMode tests
 ↓
Manual toolkit test
 ↓
Pull Request
 ↓
Squash merge
 ↓
main
 ↓
Release
```

Before merging a release into `main`, verify the complete workflow:

```text
Create Room
→ Save
→ Add sockets
→ Build Room prefab
→ Add Manual content
→ Rebuild Room
→ Verify Manual content survived
→ Create Room Module
→ Create Level
→ Connect modules
→ Assign sockets
→ Solve Layout
→ Inspect 2D
→ Inspect 3D
→ Validate
→ Build Level
→ Close Unity
→ Reopen Unity
→ Verify all data persisted
→ Run EditMode tests
```

---

# V1 Scope

The current V1 is focused on a stable authoring workflow.

Included:

- Room authoring
- Room prefab generation
- Room Modules
- Manual level graph editing
- Socket assignment
- Spatial layout solving
- Overlap detection
- 2D preview
- 3D preview
- Validation
- Final prefab generation
- Persistence
- EditMode regression tests

Not currently part of the V1 scope:

- Runtime procedural graph generation
- Wave Function Collapse
- Automatic dungeon generation rules
- Multi-floor generation
- Advanced solver backtracking
- Automatic corridor synthesis
- Reference / Hybrid build modes
- Multiplayer-specific workflows

These systems can be layered on top of the current architecture later without replacing the core Room → Graph → Layout → Build pipeline.

---

# Sample Content

A minimal sample is recommended for the repository.

Suggested structure:

```text
Assets/
└── ProceduralLevelToolkit/
    └── Samples/
        └── BasicDungeon/
            ├── Rooms/
            ├── Modules/
            ├── Prefabs/
            ├── Levels/
            └── Materials/
```

Suggested sample rooms:

```text
Entrance
Corridor
Turn
Combat Room
Boss Room
```

The sample does not need production-quality art.

Simple graybox meshes are sufficient to demonstrate:

- Room dimensions
- Socket placement
- Rotation
- Branching
- Layout solving
- 3D level assembly
- Final prefab generation

---

# Sample Asset Recommendation

For the V1 repository, prefer assets created directly inside Unity:

- Cubes
- Planes
- Simple walls
- Basic materials
- Primitive props

This keeps the sample:

- Lightweight
- Fully redistributable
- Dependency-free
- Easy to understand
- Safe from third-party licensing issues

External environment packs can still be used in screenshots or optional showcase projects, provided their license explicitly allows redistribution.

Do not require a paid Asset Store package to use the official sample.

---

# Project Status

The toolkit currently supports the full core pipeline:

```text
Room Authoring
✓

Room Prefab Generation
✓

Room Modules
✓

Level Graph
✓

Socket Connections
✓

Spatial Layout Solver
✓

2D Layout Preview
✓

3D Level Preview
✓

Validation
✓

Level Prefab Build
✓

EditMode Regression Tests
✓
```

The remaining V1 work is primarily:

```text
Sample content
Documentation polish
Final persistence test
Final regression test pass
Release preparation
```

---

# License

Add the repository license here before public distribution.

If the project is intended to be open source, include a `LICENSE` file at the repository root and reference it from this section.
