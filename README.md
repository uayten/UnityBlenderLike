# UnityBlenderLike

Makes the Unity Scene view feel like Blender, as an editor-only package you can keep to yourself in a shared project:

- **Modal rotation with R**: the selection follows the mouse around the pivot, full turns included, instead of Unity's gizmo that stops following once you circle the pivot
- **Axis locks and typed angles**: X / Y / Z lock an axis (again for local), type `90` for an exact angle, Ctrl snaps to 5°, click to confirm, right click to cancel
- **Numpad views**: 1 / 3 / 7 for front, right and top, Ctrl for the opposite side, 5 to toggle orthographic
- **Blender's axes**: views and axis locks match models exported from Blender with the default FBX settings, so Numpad 7 shows what Blender's top view shows and R Z turns around the vertical
- **Nothing in the project**: no scene, asset or setting changes, and it can live outside version control, so your teammates keep stock Unity

## Contents

- [Install](#install)
- [Uninstall](#uninstall)
- [Shortcuts](#shortcuts)
- [Modal rotation](#modal-rotation)
- [Numpad views](#numpad-views)
- [Axes](#axes)
- [Conflicts with existing shortcuts](#conflicts-with-existing-shortcuts)
- [How it works](#how-it-works)
- [Compatibility](#compatibility)
- [Why](#why)

## Install

### Just for you, in a shared project

Clone the repository into the project's `Assets` folder and keep it out of the project's repository:

```
git clone https://github.com/uayten/UnityBlenderLike.git Assets/UnityBlenderLike
```

Then add these two lines to `.git/info/exclude` in the project. It works like `.gitignore`, but it stays on your machine, so the project's shared files don't change at all:

```
/Assets/UnityBlenderLike/
/Assets/UnityBlenderLike.meta
```

A line in the project's `.gitignore` works too, if the team doesn't mind seeing it.

Nothing else in the project references the package, so teammates who don't have it get stock Unity, with no missing scripts or errors.

### In your own projects

**Window → Package Manager → + → Install package from git URL**:

```
https://github.com/uayten/UnityBlenderLike.git
```

This adds a line to `Packages/manifest.json`, so everyone who opens the project gets the package. Use the first method in projects you share.

## Uninstall

Delete the `Assets/UnityBlenderLike` folder, or remove the package in the Package Manager. The shortcuts go with it. Bindings you changed in **Edit → Shortcuts** stay in your shortcut profile.

## Shortcuts

All shortcuts work with the Scene view focused and are listed under **Blender Like** in **Edit → Shortcuts**, where they can be rebound.

| Key | Action |
|---|---|
| R | [Modal rotation](#modal-rotation) of the selection |
| Numpad 1 / Ctrl + Numpad 1 | Front / back view |
| Numpad 3 / Ctrl + Numpad 3 | Right / left view |
| Numpad 7 / Ctrl + Numpad 7 | Top / bottom view |
| Numpad 5 | Toggle orthographic / perspective |

On macOS, Ctrl is Cmd (Unity's Action modifier).

## Modal rotation

Select something, point at the Scene view and press R:

| Input | Effect |
|---|---|
| Move the mouse | Rotate around the view axis, following the cursor angle around the pivot, full turns included |
| X / Y / Z | Lock to the global axis; press again for the local axis (of the active object), again to unlock. A line through the pivot, in Unity's color for the axis it runs along, shows the lock |
| Digits, `.`, `-`, Backspace | Type an exact angle in degrees |
| Ctrl | Snap to 5° steps |
| Left click / Enter | Confirm, as one undo step |
| Right click / Esc | Cancel and go back to where it started |

The pivot follows Unity's **Pivot / Center** toggle in the toolbar. With Center, several objects swing around their common center; with Pivot, each turns around its own origin. A child whose parent is also selected turns once, with its parent.

While rotating, Unity's move / rotate / scale gizmo hides, and every other key is held back, so X doesn't delete and digits don't switch views. The current angle and axis show at the bottom of the Scene view.

Global axes follow the [axis convention](#axes). Local axes are the object's own, named as Unity names them (Y up, Z forward).

## Numpad views

| Key | View |
|---|---|
| Numpad 1 | Front (orthographic) |
| Ctrl + Numpad 1 | Back |
| Numpad 3 | Right |
| Ctrl + Numpad 3 | Left |
| Numpad 7 | Top |
| Ctrl + Numpad 7 | Bottom |
| Numpad 5 | Toggle orthographic / perspective |

The views keep the current pivot and zoom, and turn to face the [axis convention](#axes).

## Axes

**Edit → Preferences → Blender Like → Axes**:

| Setting | Views | X / Y / Z in modal rotation |
|---|---|---|
| **Blender** (default) | Match Blender for models exported with the default FBX settings: Numpad 7 shows the model the way Blender's top view does | Blender's names: Z is up, Y runs back to front. R Z 90 turns the same way as in Blender |
| **Unity** | Unity's own orientation, the same as the Scene view gizmo | Unity's names: Y is up, Z forward |

Why Blender needs its own setting: a default Blender FBX export brings Blender's +Y to Unity's -Z. Unity's top view puts +Z at the top of the screen, so a plan drawn in Blender shows up upside down in it.

The setting is saved per project, on your machine only (in `UserSettings/`, which Unity projects keep out of version control).

## Conflicts with existing shortcuts

The package binds R and the numpad keys. If other shortcuts in your profile already use them, change one side in **Edit → Shortcuts**:

- Unity's default profile uses R for the Scale tool. The package's R only acts in the Scene view.
- If you bound Unity's own **Scene View → Set Orthographic ... View** or **Toggle Orthographic Projection** to the numpad, clear those bindings, or both would answer the same key.

## How it works

- The package is one editor assembly (`UnityBlenderLike.Editor`). Nothing runs in builds or at runtime, and no scene or asset refers to it.
- The keys are regular Shortcut Manager shortcuts in the Scene view context, so they show and rebind in **Edit → Shortcuts**.
- While the modal rotation runs, a handler placed ahead of the Shortcut Manager reads the keys, so the modal gets them before any other shortcut. It's removed when the rotation ends.
- Confirming puts every object back where it started, records it for Undo, then applies the final rotation, so the whole drag undoes in one step.

## Compatibility

Written and tested on Unity 6 (6000.6) on Windows. It only uses editor APIs that exist since Unity 2021.3, the minimum declared in `package.json`, but older versions haven't been tested.

The key handler reads `EditorApplication.globalEventHandler`, an internal Unity field. If a future version removes it, rotating with the mouse and confirming or cancelling with a click still work, but the keys (axis locks, typed angles, Enter, Esc) go to Unity's own shortcuts instead.

## Why

Coming from Blender, two things in Unity's Scene view got in the way. Unity's rotate gizmo stops following the mouse when you circle the pivot, which makes a plain 180° turn awkward. And plans modeled in Blender showed up upside down in Unity's top view. This package fixes both, and keeps them out of the shared project, the same way [MayaBlenderLike](https://github.com/uayten/MayaBlenderLike) does for Maya.
