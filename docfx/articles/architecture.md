# Architecture

This page describes the pipeline from font bytes to `SpriteBatch` quads, and the invariants that keep
the on-demand atlas consistent.

The same rasterization, packing, measurement, and caching pipeline backs
`FontStashSharp2.PlatformAgnostic`. That build swaps the graphics device for `ITexture2DManager` and
exposes only the renderer draw hooks; see [Platform-agnostic rendering](platform-agnostic.md).

## Object graph

```
FontSystem ── owns ──> FontSource[] (fallback order)
    │                        │
    │                        └── StbTrueTypeNext.STBFont (parsed tables, read-only)
    │
    ├── lazily owns ──> TextureAtlas (shared pages, lock-protected)
    │                       │  created from the first SpriteBatch/renderer device
    │                       ├── ShelfPacker per page
    │                       └── Texture2D per page
    │
    └── caches ──> DynamicSpriteFont per float size
                        ├── FontSize, Ascent, Descent, LineGap, LineSpacing
                        └── GlyphCache
                              ├── Dictionary<codepoint, GlyphMetrics>
                              ├── Dictionary<codepoint, RenderedGlyph>
                              └── Dictionary<kerning pair, float>
```

The atlas is created on the first draw or preload from the `SpriteBatch`, `IFontStashRenderer`, or
`IFontStashRenderer2` device. Use one `FontSystem` per device.

## Loading

`FontSystem.AddFont` validates the bytes with `STBFontCollection`, resolves the requested face offset,
constructs an `STBFont`, and reads vertical metrics and a display name. The source array is kept alive
by the `FontSource`; StbTrueTypeNext reads the bytes in place.

`FontSystem.GetFont(size)` creates one `DynamicSpriteFont` per exact float size and caches it. All
callers of the same size share one glyph cache and atlas entries.

## On-demand glyph pipeline

```
DrawString/PreloadGlyphs
        │
        ▼
GlyphCache.GetRenderedGlyph(codepoint)
        │
        ├── GetMetrics: resolve fallback source + glyph index, cache advance
        │
        ├── STBFont.CreateGlyphBitmap(index, scale * SupersamplingFactor, ...)
        │
        ├── TextureAtlas.Add(coverage, width, height)
        │       ├── pack a padded slot (new page if necessary)
        │       ├── convert 8-bit coverage to RGBA (AlphaMode)
        │       ├── extrude edges into padding
        │       └── Texture2D.SetData(slot rectangle)
        │
        └── cache RenderedGlyph { texture index, source rectangle, offsets }
```

`MeasureString` stops at `GetMetrics`; it never calls `CreateGlyphBitmap` or the atlas. This is what
makes measurement possible before an atlas exists and keeps `GlyphCount` at zero until drawing.

## Text layout

For each code point:

1. Resolve the source and glyph index through the fallback chain.
2. Look up kerning for the previous glyph **if it came from the same source**.
3. Advance the pen by `advance * scale + kerning`.
4. Place the glyph bitmap at `pen + (bitmap.XOffset / SupersamplingFactor)`, and
   `baseline + (bitmap.YOffset / SupersamplingFactor)`.

Font units are converted with `ScaleForMappingEmToPixels(FontSize)`, so `FontSize` is an em size.
Vertical metrics give `Ascent`, `Descent` (negative), and `LineGap`; the default line spacing is
`Ascent - Descent + LineGap`.

Glyph bitmaps are rasterized at `FontSize * SupersamplingFactor`; offsets are divided back into
logical layout pixels so advances, kerning, ascent, and line spacing remain based on the nominal
`FontSize`. This is what makes scaled text sharper without changing layout. See
[Supersampling](supersampling.md) for the quality/memory tradeoffs.

The first baseline is `position.Y + Ascent`; new lines add `LineSpacing`. StbTrueTypeNext bitmap Y
offsets are baseline-relative with positive Y downward, so no sign flip is needed.

## Drawing transform

For each glyph, the library computes the glyph's top-left in layout space, then applies:

```
local  = (glyphTopLeft - origin) * scale
screen = position + Rotate(local, rotation)
```

It then calls `SpriteBatch.Draw` with the glyph's atlas source rectangle, `origin = Vector2.Zero`,
rotation/effects, and a scale of `requestedScale / SupersamplingFactor`. The source rectangle holds
the higher-resolution bitmap, so the draw ends up at the logical size while retaining the extra
detail. This reproduces whole-text rotation and scaling while letting `SpriteBatch` batch glyphs by
texture.

Horizontal/vertical flips mirror the whole block around `MeasureString`'s size before the transform.
The flip math uses logical glyph size (`source width / SupersamplingFactor`). `SpriteEffects` is
passed to `SpriteBatch.Draw` so each glyph's texture is flipped to match.

## Custom renderers

`DrawText` overloads that accept `IFontStashRenderer` use the same layout and transform pipeline, but
submit each glyph through `renderer.Draw` instead of `SpriteBatch.Draw`. The interface includes sprite
effects and layer depth, and receives already-transformed positions with an implicit origin of zero.
The CustomRenderer sample implements the interface with a `SpriteBatch` adapter for all of its text,
including rotated, scaled, and flipped lines, so that path runs in the visual integration check.
`IFontStashRenderer2` exposes the same pipeline as four screen-space vertices with layer depth in Z
and flips baked into the texture coordinates. See [Custom renderers](custom-renderers.md) for both
contracts and a complete example.

## Atlas details

- **Packing.** A shelf packer places glyphs left to right, starting a new row when the current row is
  too narrow and a new page when the page is full. Packing is deterministic.
- **Padding.** Each glyph occupies `width + 2 * GlyphPadding` by `height + 2 * GlyphPadding` in
  physical pixels. The padding is filled with the nearest edge pixel; the source rectangle returned
  to the renderer covers only the glyph itself.
- **Supersampling.** Glyphs are rasterized at `FontSize * SupersamplingFactor`; atlas usage therefore
  grows roughly with the square of the factor.
- **Alpha modes.** `AlphaMode` controls how coverage becomes RGBA. `Premultiplied` (default) stores
  `(a, a, a, a)` and matches `BlendState.AlphaBlend`; `NonPremultiplied` stores `(255, 255, 255, a)`
  for source-alpha blending; `NoAntialiasing` stores `(255, 255, 255, 255)` for non-zero coverage and
  transparent black otherwise.
- **Pages.** Pages are created lazily. `MaxTextureCount` bounds growth and produces a descriptive
  exception when exceeded. A glyph larger than a page also throws a descriptive exception.
- **Growth notification.** When no existing page can fit the next glyph, a new page is created.
  `FontSystem.TextureAtlasFull` fires immediately before that allocation; the first page does not fire
  it. Event data includes the current/max page counts, page size, glyph size, and padded slot size.
- **Clearing.** New pages are cleared once so untouched pixels are transparent.
- **Reset.** `FontSystem.ClearTextures` disposes every page and drops rendered glyph entries while
  keeping metrics and kerning. A generation counter makes even font instances removed from the size
  cache re-rasterize on their next draw/preload. Textures returned by `GetTexture` become invalid.

## Text effects

`TextEffect.Stroke` and `TextEffect.Shadow` are draw-time effects. The font first submits the effect
passes, then the main text pass, so effect pixels never cover later glyphs:

- **Stroke** submits the glyph at every offset in a square ring up to `effectAmount` (8 passes for
  amount 1). The stroke uses `effectColor`.
- **Shadow** submits one offset by `(effectAmount, effectAmount)` using `effectColor`.
- `effectColor` defaults to white. Use a contrasting color for visible outlines or shadows.
- `effectAmount = 0` is equivalent to `None`; negative values throw.

The fast `SpriteBatch` path is used only when the effect is `None`. Effects reuse the same atlas
glyphs and layout; they only multiply draw submissions.

## Lifetime, disposal, and threading

- `FontSystem.Dispose` disposes every atlas page and clears cached fonts. It is idempotent. Further
  calls on the system throw `ObjectDisposedException`.
- `TextureAtlas` guards mutations with a lock because different glyph caches may upload concurrently.
- `FontSystem`, `FontSource`, `DynamicSpriteFont`, and `GlyphCache` are not thread-safe. MonoGame
  graphics devices are not thread-safe either.
- Existing font instances keep a snapshot of the fallback list. `AddFont` clears the size cache, so
  future `GetFont` calls see the new chain.

## Performance notes

The common `DrawString(font, text, position, color)` call takes a dedicated fast path in
`DynamicSpriteFont.DrawText`:

- rotation, origin, scale, effects, and layer depth are checked once per call;
- each glyph is submitted with the four-argument `SpriteBatch.Draw` when `SupersamplingFactor` is 1,
  or one inverse-scaled draw otherwise;
- the current atlas texture is cached across glyphs instead of being looked up per glyph;
- ASCII metrics, rendered glyphs, and kerning pairs use fixed arrays, while non-ASCII uses
  dictionaries;
- the ASCII kerning table is a lazily filled 128×128 array (`NaN` means not yet computed).

All other combinations (rotation, origin, scale, flips, layer depth) use the general path with
per-glyph transforms. The `Samples/Benchmark` project measures this path against a runtime-baked
MonoGame `SpriteFont`; rerun it after changing layout or draw code.

## Testing seam

`IGlyphAtlas` abstracts texture upload. Unit tests use a recording implementation to assert that:

- measurement does not rasterize,
- each unique code point is rasterized once,
- repeated preloads are no-ops,
- whitespace does not allocate atlas entries,
- bitmap coverage is non-empty.

The `HelloWorld --screenshot` sample is the end-to-end visual check with a real graphics device.
