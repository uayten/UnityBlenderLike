# Changelog

## [0.3.0] - 2026-09-25

### Added

- Z-up Transform Inspector: with Blender axes, Location, Rotation (Blender's XYZ Euler) and Scale show and edit Blender's values. Toggle in Preferences > Blender Like or the Transform header's context menu.

### Changed

- With Blender axes, local X / Y / Z in the modal transforms use Blender's names too, matching the Z-up Inspector.

## [0.2.0] - 2026-09-25

### Added

- Modal G (move) and S (scale) next to R, with G / R / S switching mode without confirming.
- H / Shift+H / Alt+H hide selected, hide unselected and reveal all, with Scene view visibility.
- Alt+G / Alt+R / Alt+S clear location, rotation and scale.
- Shift+D duplicates and starts moving the copy, as one undo step.
- Ctrl+P / Alt+P parent to the active object and clear parent, keeping world placement.
- A / Alt+A select all and select none.
- Numpad . frames the selection; Numpad / toggles local view and restores the view on leaving it.
- Auto Perspective: views made orthographic by Numpad 1 / 3 / 7 go back to perspective when orbited, with a setting in Preferences > Blender Like.

### Changed

- X / Y / Z in the modal transforms use Blender's axis names with the Blender axis setting (Z up).

## [0.1.0] - 2026-09-25

### Added

- Modal rotation with R in the Scene view: follows the mouse around the pivot, X / Y / Z axis locks (global, then local), typed angles, Ctrl snapping, one undo step.
- Numpad views: 1 / 3 / 7 front, right and top, Ctrl for the opposite side, 5 to toggle orthographic.
- Preferences > Blender Like: Blender or Unity axis convention for the views and axis locks, per project.
