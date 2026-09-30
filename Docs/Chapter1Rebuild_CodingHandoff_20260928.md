# Chapter 1 Rebuild Coding Handoff — 2026-09-28

## Boundary

The resource-production task imported the approved art only. Do not regenerate or replace the
PNG files. Implement runtime wiring in the coding task and preserve the current dirty worktree.

Unity project: `C:\Users\hdnwl\OneDrive\Desktop\Project\escape`

Resource root: `Assets/Resources/ChannelZero/Chapter1Rebuild`

## Imported sets

- `TVReusable`: 10 layered closeup sprites
- `TVGlitchCloseup`: 6 transparent closeup glitch overlays
- `TVGlitchRoom`: 6 full-room glitch frames
- `Sofa`: 8 full-room child/sofa frames
- `TimeSlip`: 7 full-room 2001-to-1961 frames

## Required integration

1. For `LIV-Z01`, build a reusable stack in this order:
   `ScreenContent -> BasePlate -> GlitchOverlay -> GlassReflection -> Scanline -> ScreenGlow`.
2. Keep the original artwork fallback for non-TV closeups and missing resources.
3. Support at least `off`, `noise`, and `glitch` TV states. `off` must use only
   `screen_off + base_plate` so it reconstructs the approved original.
4. Play closeup and room glitch frames with timings `95, 70, 120, 80, 145, 420 ms`.
5. On the first unplugged-TV event, finish the room glitch before revealing the sofa child.
6. Wire the eight sofa frames and seven time-slip frames without changing their order.
7. Preserve reduced-flashing and reduced-motion accessibility behavior.

Likely integration points:

- `Assets/ChannelZero/Runtime/Presentation/ChannelZeroCloseupCanvasController.cs`
- `Assets/ChannelZero/Runtime/Presentation/Chapter1GlitchFrameSequencePlayer.cs`
- `Assets/ChannelZero/Runtime/Presentation/ChannelZeroVerticalSliceController.cs`
- `Assets/ChannelZero/Tests/EditMode/ChannelZeroChapter1RuntimeTests.cs`
- `Assets/ChannelZero/Tests/PlayMode/ChannelZeroVerticalSlicePlayModeTests.cs`

## Completion gates

- Verify all 37 imported assets load as Sprites.
- Add focused EditMode coverage for frame counts and layered TV resource loading.
- Keep the existing vertical-slice PlayMode suite passing.
- Manually check the first TV glitch, sofa reveal/disappearance, time slip, closeup layering,
  reduced flashing, and 1280x720 / 1920x1080 rendering.
- Report automated evidence separately from manual Play Mode evidence.
