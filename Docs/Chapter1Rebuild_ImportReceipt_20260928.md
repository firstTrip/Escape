# Chapter 1 Rebuild Art Import Receipt — 2026-09-28

## Imported Unity resources

- TV reusable closeup layers: 10 sprites
- TV closeup glitch overlays: 6 sprites
- TV room glitch frames: 6 sprites
- Sofa child sequence: 8 sprites
- 2001 to 1961 time-slip sequence: 7 sprites
- RuntimeSetV02 family-photo states: 2 sprites
- RuntimeSetV02 TV rear states: 2 sprites
- RuntimeSetV02 1961 tuner states: 3 sprites
- RuntimeSetV03 B~G state art: 24 sprites
- RuntimeSetV04 follow-up state art: 36 sprites
- Total: 104 PNG files with version-preserving Unity `.meta` files

All assets live under `Assets/Resources/ChannelZero/Chapter1Rebuild`. Existing closeup PNG,
catalog entries, and their `.meta` files were not replaced. The vertical-slice scene was
intentionally regenerated from its canonical scene creator to serialize the new TMP/localization
references and compatibility hotspots.

## Runtime wiring status

- TV layered closeup, six-frame glitch, sofa sequence, and forward/reverse time-slip are wired.
- The changed 2001 family photo, burned/repaired TV rear, and three 1961 tuner states are
  selected from runtime flags and puzzle state without replacing the original closeup catalog.
- B~G state art is routed to the TV screw/cover/tube stages, return wires/tube panel,
  stabilizer, Jinwoo treatment, and clock gears/hidden compartment.
- RuntimeSetV04 is routed to frame-back clue stages, vase causality, four-era child traces,
  alignment overlays, Jinwoo outcomes, three clock ticks, and the ending TV recognition state.
- Closeup copy is presented through serialized TextMeshProUGUI references. Puzzle title, body,
  action, and close-button copy expose stable locale keys with Korean fallback text.
- The scene-hotspot path continues through vase, 2749, Jinwoo treatment, 08:08, the workshop
  door, and all four authored Chapter 1 ending lines.

## Import policy

- All textures are single Sprites, sRGB, bilinear, Clamp, 2048 max size, with mipmaps disabled.
- Reusable TV layers are uncompressed to preserve mask and reconstruction edges.
- Full-screen animation frames use high-quality compression.

## Verification

- Runtime sprite load coverage: 104/104.
- Resource count and importer settings were checked after import.
- No existing closeup PNG, catalog asset, or `.meta` file was overwritten.
- EditMode: 68/68 passed.
- PlayMode: 6/6 passed, including the full scene-hotspot completion path.
- Windows development build: succeeded with 0 build errors; headless startup reached the
  2001 entrance state.

Automated interaction and headless startup do not replace human pointer, visual, audio, or
multi-resolution Play Mode review.
