# TouchInput

A lightweight touch gesture toolkit for Unity, built on top of the **Unity Input System**.
It recognizes taps, swipes and one/two-finger transform gestures (pan, rotate, pinch-to-scale),
and applies them to 3D objects. It also lets you simulate multi-touch with a mouse in the Editor.

The project is loosely inspired by [TouchScript](https://github.com/TouchScript/TouchScript)
but uses the modern Input System and uGUI EventSystem instead of a custom input layer.

## Requirements

| | Version |
|---|---|
| Unity | 6000.0.63f1 (Unity 6) |
| Input System | 1.16.0 |
| Render pipeline | URP 17 (only needed for the sample scene) |

The runtime assembly `TouchInput.Core` targets **Android**, **iOS** and the **Editor**.

## Features

- **Custom Input System composites** for touch input:
  - `OneFingerDrag`: a single pointer with its position and contact state.
  - `SingleFingerExclusiveDrag`: a single pointer that is active only while no second touch is down.
  - `TwoFingersDrag`: two pointers with both positions.
- **Transform gestures**:
  - `MultiTouchTransformGesture` reads input from Input System actions and needs no EventSystem or `Graphic`.
  - `MultiTouchTransformGestureUI` reads input from EventSystem drag events, for UI elements.
  - You can bind translation, rotation and scaling to one or two fingers, and lock any of them at runtime.
  - Thresholds are set in centimeters, so gestures behave the same across screen densities.
- **Discrete gestures**:
  - `TapGesture`: single/double/N-tap detection, cancelled by multi-touch.
  - `SwipeGesture`: swipes with a minimum distance and time limit, restricted to horizontal, vertical or any direction.
  - `MetaGesture`: combines all pointer press/move/release events on an object into one stream, including the active pointer count.
- **Transformer**: `ThreeDimensionalTransformation` moves, rotates and scales one or more `Transform`s from gesture output, with per-property smoothing, sensitivity and scale limits.
- **Mouse simulation of multi-touch** in the Editor and on desktop.
- **Editor tooling**: an `InputActionName` field with a custom drawer that shows a dropdown of every action in the project's Input Action assets.

## Project structure

```
Assets/TouchInput/Source/
├── Actions/
│   ├── Mappings/GeneralActions.inputactions   # "Mobile" and "UI" action maps
│   └── Scripts/
│       ├── Composites/                        # Custom InputBindingComposites
│       └── Contracts/                         # TouchResult, TwoTouchesResult, InputActionName
├── Editor/                                    # InputActionName property drawer
├── Gestures/
│   └── Scripts/
│       ├── Contracts/                         # Event data, TransformType, SwipeGestureDirection
│       ├── Dispatchers/                       # DragEventsDispatcher
│       ├── Modifiers/                         # Pointer position modifiers (mouse simulation)
│       ├── MetaGesture.cs
│       ├── MultiTouchTransformGestureBase.cs
│       ├── MultiTouchTransformGesture.cs
│       ├── MultiTouchTransformGestureUI.cs
│       ├── SwipeGesture.cs
│       └── TapGesture.cs
├── Managers/
│   ├── Prefabs/                               # InputManager, UnityEventSystem
│   └── Scripts/InputActionsManager.cs
├── Transformers/                              # ThreeDimensionalTransformation
├── Utilities/                                 # MonoSingleton, CameraCache, extensions
└── Samples/Scenes/TouchTransformations.unity  # Demo scene
```

## Getting started

1. Clone the repository and open it in Unity 6000.0.63f1 or newer.
2. Open `Assets/TouchInput/Source/Samples/Scenes/TouchTransformations.unity` and press Play.
3. Drag with the mouse to move the object. Hold **Left Ctrl** and drag to rotate and scale it (see [Mouse simulation](#mouse-simulation)).

### Using it in your own scene

1. Add the `InputManager` prefab (`Managers/Prefabs`) to the scene. It holds the `InputActionsManager` singleton, which:
   - references the `InputActionAsset` (by default `GeneralActions`);
   - defines the reference DPI that converts centimeter thresholds to pixels.
2. If you use the EventSystem-based gestures (`TapGesture`, `SwipeGesture`, `MetaGesture`,
   `MultiTouchTransformGestureUI`), also add the `UnityEventSystem` prefab and put a `Graphic`
   with `raycastTarget = true` on the gesture's GameObject. A transparent `Image` works.
3. Add the gesture component you need and subscribe to its events.

#### Transforming a 3D object

Add `ThreeDimensionalTransformation` to a GameObject. Unity also adds `MultiTouchTransformGesture`
automatically, because the transformer requires it. Then configure:

- **Gesture**:
  - `Single/Multi Touch Bindings`: which transforms one finger and two fingers drive. The defaults are translation for one finger, and rotation plus scaling for two.
  - `Locked Transforms`: transforms that are suppressed regardless of input.
  - `Screen Transform Threshold`: the minimum movement in cm before a gesture is recognized.
  - `Min Fingers Distance`: the minimum distance in cm between the two fingers.
  - Action names: the defaults are `Touch (Single)` and `Touch (Multi)`.
- **Transformer**: for each of translation, rotation and scale you can set:
  - the target `Transform`;
  - smoothing factor;
  - sensitivity;
  - snap distance.

  The transformer also has min/max scale limits.

#### Listening to gestures from code

```csharp
using TouchInput.Source.Gestures.Scripts;
using TouchInput.Source.Gestures.Scripts.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

public class GestureListener : MonoBehaviour
{
    [SerializeField] private TapGesture _tap;
    [SerializeField] private SwipeGesture _swipe;
    [SerializeField] private MultiTouchTransformGesture _transform;

    private void OnEnable()
    {
        _tap.Tapped += OnTapped;
        _swipe.Swiped += OnSwiped;
        _transform.ScalePhaseChanged += OnScale;
    }

    private void OnDisable()
    {
        _tap.Tapped -= OnTapped;
        _swipe.Swiped -= OnSwiped;
        _transform.ScalePhaseChanged -= OnScale;
    }

    private void OnTapped(TapEventData e) => Debug.Log($"{e.Count} taps at {e.Position}");

    private void OnSwiped(SwipeEventData e) => Debug.Log($"Swipe {e.DeltaPosition}");

    private void OnScale(InputActionPhase phase)
    {
        if (phase == InputActionPhase.Performed)
            Debug.Log($"Scale delta: {_transform.DeltaScale}");
    }
}
```

Every gesture exposes both a C# `event` and a serialized `UnityEvent`, so you can wire it up from
code or from the Inspector.

### Transform gesture output

`MultiTouchTransformGestureBase` raises `TranslationPhaseChanged`, `RotationPhaseChanged` and
`ScalePhaseChanged` with `Started`, `Performed` or `Canceled`. During `Performed`, read the per-frame deltas:

| Property | Meaning |
|---|---|
| `DeltaPosition` | Drag delta in screen pixels |
| `DeltaRotation` | Rotation delta in degrees (positive = counter-clockwise) |
| `DeltaScale` | Fractional change in finger distance (negative = fingers apart / zoom in) |

You can change bindings at runtime with `RebindSingleTouchTransform`, `RebindMultiTouchTransform` and
`SetTransformsLock`. Only one transform can be bound to a single finger, and a transform bound to a
single finger is removed from the two-finger bindings automatically.

## Mouse simulation

On non-handheld devices, the gestures swap in an `IDragPositionModifier` that turns mouse input into a fake second finger:

| Component | How to simulate two fingers | Modifier |
|---|---|---|
| `MultiTouchTransformGesture` | Hold **Left Ctrl** + left mouse button. The second touch mirrors the cursor around the point where Ctrl-drag started. | `SimulateSecondTouchAsPivotOffset` |
| `MultiTouchTransformGestureUI` | Use the **right mouse button** as the second pointer. | `SimulateSecondTouchAsOppositePosition` |

On handheld devices, `DragScreenPosition` passes the real touch positions through unchanged.

## Tips

- **Nested drag handlers.** The EventSystem delivers drag events only to the nearest handler.
  So a child `ScrollRect` will block a `SwipeGesture` on its parent. Add `DragEventsDispatcher` to
  the child and point it at the parent, and both will receive the drag.
- **Debug logging.** Uncomment `#define USE_DEBUG_LOGS` at the top of
  `MultiTouchTransformGestureBase.cs` to log pointer begin, move and end.
