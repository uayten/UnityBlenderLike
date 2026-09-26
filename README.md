# UnityBlenderLike

Makes the Unity Scene view feel like Blender, as an editor-only package you can keep to yourself in a shared project:

- **Modal G / R / S**: move, rotate and scale follow the mouse until you click, and rotation follows the cursor around the pivot, full turns included, instead of Unity's gizmo that stops following once you circle the pivot
- **Axis locks and typed values**: X / Y / Z lock an axis (again for local), type `90` for an exact value, Shift for precision, Ctrl snaps, G / R / S switch mode without confirming, click to confirm, right click to cancel
- **Keys follow the mouse**: with the mouse over the Scene view, every key goes there, even right after clicking in the Hierarchy, like Blender's areas
- **Blender's object keys**: H / Shift+H / Alt+H hide and reveal, Alt+G / Alt+R / Alt+S clear transforms, Shift+D duplicates and moves, Ctrl+P / Alt+P parent and unparent, A / Alt+A select all and none
- **Blender's middle mouse**: middle drag orbits around the pivot, Shift + middle drag pans
- **Numpad views**: 1 / 3 / 7 for front, right and top, Ctrl for the opposite side, 5 to toggle orthographic, `.` to frame the selection, / for local view, and Auto Perspective when orbiting
- **Drag over Hierarchy arrows**: press an expand arrow and drag up or down, and every item you pass opens (or closes) too, like Blender's Outliner
- **Z-up Transform Inspector**: Location, Rotation and Scale show and edit the values Blender's N panel shows, with Z up and Blender's XYZ Euler, while the object keeps Unity's own values underneath
- **Blender's axes**: views and axis locks match models exported from Blender with the default FBX settings, so Numpad 7 shows what Blender's top view shows and R Z turns around the vertical
- **Nothing in the project**: no scene, asset or setting changes, and it can live outside version control, so your teammates keep stock Unity

## Contents

- [Install](#install)
- [Uninstall](#uninstall)
- [Shortcuts](#shortcuts)
- [Modal transforms](#modal-transforms)
- [Object shortcuts](#object-shortcuts)
- [Middle mouse navigation](#middle-mouse-navigation)
- [Numpad views](#numpad-views)
- [Z-up Transform Inspector](#z-up-transform-inspector)
- [Hierarchy](#hierarchy)
- [Settings](#settings)
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

All shortcuts are listed under **Blender Like** in **Edit → Shortcuts**, where they can be rebound. They act in the Scene view, and in other windows the same keys keep doing what Unity does there.

Like Blender, keys go to the view under the mouse: select something in the Hierarchy, move the mouse over the Scene view and press G, and it moves, without clicking the Scene view first. This holds for every key, not just this package's: the Scene view takes the keyboard focus and gets the key, so Unity's and your project's own Scene view shortcuts answer too, and the Hierarchy doesn't act on it (End doesn't jump to its last item, for one). While you type in a text field (renaming, searching), keys stay there, and modifier keys alone (Shift, Ctrl, Alt) never move the focus.

| Key | Action | Unity default it overrides in the Scene view |
|---|---|---|
| G / R / S | [Modal move / rotate / scale](#modal-transforms) | R: Scale tool |
| H / Shift+H / Alt+H | [Hide selected / hide unselected / reveal all](#object-shortcuts) | H: toggle visibility, Shift+H: toggle isolation |
| Alt+G / Alt+R / Alt+S | Clear location / rotation / scale | |
| Shift+D | Duplicate and move | |
| Ctrl+P / Alt+P | Parent to active / clear parent | Ctrl+P: Play |
| A / Alt+A | Select all / select none | |
| Numpad 1 / Ctrl + Numpad 1 | Front / back view | |
| Numpad 3 / Ctrl + Numpad 3 | Right / left view | |
| Numpad 7 / Ctrl + Numpad 7 | Top / bottom view | |
| Numpad 5 | Toggle orthographic / perspective | |
| Numpad . | Frame selected | |
| Numpad / | Local view | |

W and E keep Unity's move and rotate gizmos. On macOS, Ctrl is Cmd (Unity's Action modifier).

## Modal transforms

Select something, point at the Scene view and press G, R or S:

| Input | Effect |
|---|---|
| Move the mouse | Move in the view plane, rotate around the view axis following the cursor angle around the pivot (full turns included), or scale by the distance from the pivot |
| X / Y / Z | Lock to the global axis; press again for the local axis (of the active object), again to unlock. A line through the pivot, in Unity's color for the axis it runs along, shows the lock |
| Digits, `.`, `-`, Backspace | Type an exact value: meters for move (along X when no axis is locked), degrees for rotate, factor for scale |
| G / R / S | Switch to another transform, keeping what the previous one did |
| Shift | Precision: while held, moving and scaling follow a tenth of the mouse movement, and rotating turns a tenth of the angle the cursor goes around the pivot |
| Ctrl | Snap: Unity's grid snap step for move, 5° for rotate, 0.1 for scale |
| Left click / Enter | Confirm, as one undo step |
| Right click / Esc | Cancel and go back to where it started |

Like Blender's continuous grab, the cursor never runs out of room: when it reaches an edge of the Scene view, it jumps to the opposite edge and the transform goes on without a jump. The dotted line to the pivot follows where the mouse would be, past the edge. While transforming, the cursor shows a move, rotate or scale arrow. The jump uses the operating system's cursor call (Windows and macOS); on Linux the cursor stops at the edge as in stock Unity.

The pivot follows Unity's **Pivot / Center** toggle in the toolbar. With Center, several objects swing around and scale from their common center; with Pivot, each turns and grows around its own origin. A child whose parent is also selected moves once, with its parent.

Unity can't scale an object along an arbitrary world axis without shearing it, so a scale locked to an axis acts on the object's own axis that runs closest to it.

While transforming, Unity's move / rotate / scale gizmo hides, and every other key is held back, so X doesn't delete and digits don't switch views. The current value and axis show at the bottom of the Scene view.

Global axes follow the [axis setting](#settings). Local axes are the object's own, named the same way: with Blender axes, local Z is the object's up, as the [Z-up Transform Inspector](#z-up-transform-inspector) shows it.

## Object shortcuts

| Key | Effect |
|---|---|
| H | Hide the selection and everything under it |
| Shift+H | Hide everything except the selection |
| Alt+H | Reveal everything |
| Alt+G / Alt+R / Alt+S | Reset local position to 0, rotation to 0, scale to 1, as one undo step |
| Shift+D | Duplicate and start moving the copy. Right click keeps the copy in place; one undo removes the copy |
| Ctrl+P | Parent the selected objects to the active one, keeping where they are. Select the children first and the parent last |
| Alt+P | Take the selected objects out of their parents, keeping where they are |
| A | Select all |
| Alt+A | Select none |

Hiding uses Unity's Scene view visibility: it only affects what you see in the Editor, isn't saved in the scene, and doesn't change the game.

Unity keeps the hierarchy inside a prefab instance fixed, so Ctrl+P / Alt+P skip objects inside one, with a warning in the Console. Open the prefab to change its hierarchy.

## Middle mouse navigation

| Gesture | Blender Like | Unity default |
|---|---|---|
| Middle mouse drag | Orbit around the pivot | Pan |
| Shift + middle mouse drag | Pan: the scene follows the mouse at the pivot's depth | Pan |

The orbit turns at the same speed and in the same directions as Unity's Alt + left drag. In a 2D or rotation-locked Scene view, a plain middle drag still pans.

Everything else stays Unity's: Alt + left drag orbit, right drag to look and fly with WASD, the wheel to zoom, and middle drags with Alt or Ctrl. During a G / R / S, the middle button cancels it, like the right button.

Turn it off with **Middle Mouse Navigation** in [Settings](#settings).

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
| Numpad . | Frame the selection, like Unity's F |
| Numpad / | Local view: only the selection shows, framed. Again to show everything and go back to the view from before |

The views keep the current pivot and zoom, and follow the [axis setting](#settings).

Like Blender's Auto Perspective, a view that went orthographic through 1, 3 or 7 returns to perspective when you orbit it. A view made orthographic with 5 stays orthographic while orbiting.

Local view uses Unity's isolation, the same as its own isolation shortcut: it isn't saved in the scene.

## Z-up Transform Inspector

With Blender axes, the Transform component in the Inspector shows Blender's values:

| Row | Shows |
|---|---|
| **Location** | Position with Z up and Y from front to back: Blender X = Unity -X, Blender Y = Unity -Z, Blender Z = Unity Y |
| **Rotation** | Blender's XYZ Euler angles, in degrees, turning the way they turn in Blender |
| **Scale** | Scale with Y and Z swapped |

The labels read Location / Rotation / Scale, like Blender's N panel, so it's clear which values you're looking at. Editing works as usual: typing, dragging the X / Y / Z labels, expressions, several objects at once (a value that differs shows as `—`), prefab overrides in bold with right-click revert, and Undo.

Only the display changes. The object keeps Unity's values, so scenes, prefabs, animation and code are untouched, and a teammate without the package sees Unity's values for the same object. When talking about positions with them, remember that your Z is their Y.

To see Unity's values, right click the Transform header → **Blender Like: Z-Up Values**, or turn it off in [Settings](#settings). The Scene view gizmos stay Unity's: the up arrow is still the green one.

Values match Blender's N panel for objects whose FBX brings no axis rotation of its own. When a model's root comes in with rotation X -90 (FBX import without **Bake Axis Conversion**), the Inspector shows that -90, because it's really there in Unity.

RectTransform (UI) keeps Unity's Inspector.

## Hierarchy

Press an item's expand arrow in the Hierarchy and drag up or down: every item the mouse passes over gets the same state as the first one, so one stroke opens (or closes) a whole column of arrows, like Blender's Outliner. Items without children are skipped.

Opening an item pushes the rows below it down, so a drag downward runs into the children it just revealed, as in Blender. To open a list of siblings in one go, start at the bottom one and drag up.

Alt+click keeps Unity's own behavior (expand or collapse everything under the item).

This works with the Hierarchy window of Unity 6 (the one built with UI Toolkit). It finds that window's internals by name, so on Unity versions with the older Hierarchy nothing changes there.

## Settings

**Edit → Preferences → Blender Like**:

| Setting | Effect |
|---|---|
| **Axes: Blender** (default) | Views match Blender for models exported with the default FBX settings: Numpad 7 shows the model the way Blender's top view does. X / Y / Z use Blender's names: Z is up, Y runs back to front, and G Z 1 or R Z 90 go the same way as in Blender |
| **Axes: Unity** | Views use Unity's own orientation, the same as the Scene view gizmo. X / Y / Z use Unity's names: Y is up, Z forward |
| **Z-Up Transform Inspector** (on by default) | With Blender axes, the [Transform Inspector](#z-up-transform-inspector) shows Blender's values |
| **Auto Perspective** (on by default) | Views made orthographic by 1, 3 or 7 go back to perspective when orbited |
| **Middle Mouse Navigation** (on by default) | [Middle drag orbits, Shift + middle drag pans](#middle-mouse-navigation) |

Why Blender needs its own axis setting: a default Blender FBX export brings Blender's +Y to Unity's -Z. Unity's top view puts +Z at the top of the screen, so a plan drawn in Blender shows up upside down in it.

Settings are saved per project, on your machine only (in `UserSettings/`, which Unity projects keep out of version control).

## Conflicts with existing shortcuts

When a Scene view shortcut and a global one share a key, Unity runs the Scene view one while the Scene view is focused. So G, R, S, Ctrl+P and the others override Unity's global keys only there; the fly mode keys (right mouse + WASD) keep working.

Two of Unity's defaults live in a context of their own, active in the Scene view too, so they do clash with the package:

- **Scene Visibility → Toggle Selection And Descendants Visibility** (H)
- **Scene Visibility → Toggle Isolation On Selection And Descendants** (Shift+H)

Clear their bindings in **Edit → Shortcuts** (the package covers both with H and Numpad /). The same goes for any of Unity's **Scene View → Set Orthographic ... View** or **Toggle Orthographic Projection** you bound to the numpad yourself.

## How it works

- The package is one editor assembly (`UnityBlenderLike.Editor`). Nothing runs in builds or at runtime, and no scene or asset refers to it.
- The keys are regular Shortcut Manager shortcuts in the Scene view context, so they show and rebind in **Edit → Shortcuts**.
- While a modal transform runs, a handler placed ahead of the Shortcut Manager reads the keys, so the modal gets them before any other shortcut. It's removed when the transform ends.
- Middle mouse navigation reads the mouse in the Scene view's `beforeSceneGui`, before the Scene view's own camera controls, and moves the view's pivot and rotation.
- Keys follow the mouse through two listeners, because keys reach windows two ways: IMGUI windows get them through the Shortcut Manager's handler (a second handler goes ahead of it), and UI Toolkit windows, like Unity 6's Hierarchy, get them straight from their panel (a listener on each panel's root takes them first). Either one gives the Scene view the focus and sends it the key. The Game view keeps its keys, so playing isn't interrupted when the mouse drifts over the Scene view.
- Confirming puts every object back where it started, records it for Undo, then applies the final transform, so the whole drag undoes in one step. Shift+D merges the duplicate into that same step.
- Auto Perspective reads the Scene view's view animation (internal) to tell it apart from an orbit.
- The Hierarchy drag listens to the Hierarchy window's pointer events before its rows see them, and opens or closes items through the Hierarchy view's expand and collapse methods.
- The Z-up Transform Inspector is a custom Inspector for Transform. It converts the values on the way in and out, and draws Unity's own Transform Inspector when it's off.

## Compatibility

Written and tested on Unity 6 (6000.6) on Windows. It only uses editor APIs that exist since Unity 2021.3, the minimum declared in `package.json`, but older versions haven't been tested.

Two features read internal Unity fields. If a future version removes them:

- `EditorApplication.globalEventHandler`: moving the mouse and confirming or cancelling with a click still work, but the keys during a modal transform (axis locks, typed values, Enter, Esc) go to Unity's own shortcuts instead, and keys stop following the mouse (click the Scene view first).
- The Scene view's view animation: Auto Perspective waits a fixed second after a view key before it starts watching for orbits.
- The Hierarchy window's row items (`Unity.Hierarchy.HierarchyViewItem`): if they change, dragging over the arrows stops doing anything and a plain click works as in stock Unity.
- `UnityEditor.TransformInspector`, Unity's own Transform Inspector: with the Z-up Inspector off, the Transform shows its raw fields instead (rotation as a quaternion).

## Why

Coming from Blender, Unity's Scene view kept getting in the way. Unity's rotate gizmo stops following the mouse when you circle the pivot, which makes a plain 180° turn awkward. Plans modeled in Blender showed up upside down in Unity's top view. And hands trained on G, R, S and H kept reaching for keys that do something else. This package brings those habits over, and keeps them out of the shared project, the same way [MayaBlenderLike](https://github.com/uayten/MayaBlenderLike) does for Maya.
