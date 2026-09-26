# Changelog

## [0.8.1] - 2026-09-26

### Fixed

- Keys follow the mouse from UI Toolkit windows too. Unity 6's Hierarchy gets keys straight from its panel, not through the Shortcut Manager's handler, so End there still jumped to its last item instead of reaching the Scene view.
- The Game view keeps its keys even with the mouse over the Scene view.
- No more UAC0005 warning: the Hierarchy types are found by assembly-qualified name instead of scanning `AppDomain.GetAssemblies()`.

## [0.8.0] - 2026-09-26

### Changed

- Keys follow the mouse for every key, not just this package's shortcuts: a key pressed over a Scene view goes there, so Unity's and the project's Scene view shortcuts answer too, and the window that had the focus no longer acts on it (the Hierarchy's End jumped to its last item and lost the selection).

## [0.7.1] - 2026-09-26

### Fixed

- Shift during R turned the object back and forth instead of slowly around the pivot. Precision now scales the angle the cursor turns around the pivot, as in Blender, instead of slowing the cursor point down, which shrank and shifted the circle it drew.

## [0.7.0] - 2026-09-26

### Added

- Continuous grab during G / R / S: the cursor jumps to the opposite edge of the Scene view instead of leaving it, and the transform goes on (Windows and macOS).
- The cursor shows a move, rotate or scale arrow during G / R / S.

## [0.6.0] - 2026-09-26

### Added

- Middle mouse navigation, as in Blender: middle drag orbits around the pivot and Shift + middle drag pans, instead of Unity's middle drag pan. Toggle in Preferences > Blender Like.

## [0.5.0] - 2026-09-26

### Added

- Keys follow the mouse: with the mouse over a Scene view, the package's shortcuts act there even while another window (the Hierarchy, say) has the keyboard focus. Text fields keep their keys.
- Shift during G / R / S: precision mode, the mouse counts a tenth as much.

## [0.4.1] - 2026-09-26

### Fixed

- The Scene view could stop taking clicks, and G / R / S stop starting, after a modal transform was confirmed or cancelled with a click whose release never reached the Scene view. The click is now held until its release, a new click or one second, whichever comes first.

## [0.4.0] - 2026-09-25

### Added

- Hierarchy: press an expand arrow and drag, and every item the mouse passes over opens or closes with it, like Blender's Outliner.

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
