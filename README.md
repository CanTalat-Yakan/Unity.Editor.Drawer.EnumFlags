# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Enum Flags Drawer

> Quick overview: A foldout with checkboxes, “All” and “None” controls for bitwise flag enums. Annotate an enum field with `[EnumFlags]` (and mark the enum with `[Flags]`) to select multiple values at once.

This drawer turns a flagged enum into a compact, readable list of toggles. Use it for any enum designed for bitwise combinations.

![screenshot](Documentation/Screenshot.png)

## Features
- Checkbox list for each enum value under a single foldout
- Quick actions: “All” to set every bit, “None” to clear all
- Supports any integral underlying enum type (byte, int, long, etc.)
- Works best with enums marked `[Flags]` and power‑of‑two values
- Editor‑only; zero runtime overhead

## Requirements
- Unity Editor 6000.0+ (Editor‑only; attribute lives in Runtime for convenience)
- No external dependencies

Tip: Define a zero value (None = 0) in your enum for a predictable “no flags” state; consider adding an explicit “All” member if useful.

## Usage
Define a flagged enum and use `[EnumFlags]` on the field

```csharp
using System;
using UnityEngine;
using UnityEssentials;

[Flags]
public enum Abilities
{
    None   = 0,
    Jump   = 1 << 0,
    Dash   = 1 << 1,
    Shoot  = 1 << 2,
    Glide  = 1 << 3,
    All    = ~0
}

public class PlayerConfig : MonoBehaviour
{
    [EnumFlags]
    public Abilities unlockedAbilities;
}
```

Result: In the Inspector, expand the foldout to toggle individual flags, or click All/None.

## How It Works
- The property drawer activates on enum fields annotated with `[EnumFlags]`
- Renders a single foldout line; when expanded:
  - Two small buttons: All (sets value to bitwise NOT of 0), None (sets value to 0)
  - A checkbox per enum name; toggling sets/clears the corresponding bit using bitwise OR/AND/NOT
- Special cases when your enum defines members with values 0 and ~0:
  - A “None” member (value 0) is considered checked only when the combined value is 0
  - An “All” member (value ~0 for the underlying type) is considered checked when all other visible members are set
- Height expands dynamically based on the number of enum names

## Notes and Limitations
- Best with `[Flags]` enums whose members are power‑of‑two bit values
- All/None buttons work even if the enum does not declare explicit `None`/`All` members; they directly set 0 or ~0
- If your enum doesn’t cover all bits (gaps), “All” sets every bit of the underlying type; visually, all listed checkboxes appear checked
- The drawer operates on the current target object; standard multi‑object editing rules apply
- The field must be an enum; applying `[EnumFlags]` to non‑enum fields shows an inline error

## Files in This Package
- `Runtime/EnumFlagsAttribute.cs` – `[EnumFlags]` attribute marker
- `Editor/EnumFlagsDrawer.cs` – PropertyDrawer (foldout UI, All/None buttons, bitwise toggle logic)
- `Runtime/UnityEssentials.EnumFlagsDrawer.asmdef` – Runtime assembly definition
- `Editor/UnityEssentials.EnumFlagsDrawer.Editor.asmdef` – Editor assembly definition

## Tags
unity, unity-editor, attribute, propertydrawer, enum, flags, mask, bitwise, checkbox, inspector, ui, tools, workflow
