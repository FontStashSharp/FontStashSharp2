# FontStashSharp2 - repository guide for AI agents

This file describes how the repository is organized and which invariants must be preserved when
changing code. Read it before making non-trivial edits.

## What this project is

A small text rendering library for MonoGame (.NET 8 / C# 12). It parses TrueType/OpenType fonts with
**StbTrueTypeNext** and rasterizes glyphs on demand into a shared texture atlas. Public draw methods
are `SpriteBatch` extension methods that mirror the `SpriteFont` overload set.

Do **not** copy implementation code or architecture from the upstream FontStashSharp project. This is
an independent implementation. Familiar public API shape is fine; internals should be our own.

## Build, test, run

```bash
dotnet build FontStashSharp2.sln
dotnet test FontStashSharp2.Tests/FontStashSharp2.Tests.csproj
dotnet run --project Samples/HelloWorld
dotnet run --project Samples/MeasureText
dotnet run --project Samples/Benchmark
dotnet run --project Samples/Benchmark -- --benchmark
dotnet run --project Samples/Supersampling
dotnet run --project Samples/HelloWorld -- --screenshot out.png
dotnet run --project Samples/Benchmark -- --screenshot benchmark.png
dotnet run --project Samples/Supersampling -- --screenshot supersampling.png
```

Documentation:

```bash
dotnet tool restore
dotnet docfx docfx/docfx.json
```

The screenshot mode is also a useful end-to-end smoke test on a machine with a graphics device.

## Layout

```
FontStashSharp2/
  FontSystem.cs             owns sources, cached fonts, atlas; AddFont/GetFont/GetTexture
  FontSource.cs             wrapper around StbTrueTypeNext STBFont + name/metrics
  DynamicSpriteFont.cs      per-size metadata, MeasureString, PreloadGlyphs, DrawText
  SpriteFontBase.cs         public base type + internal drawing hook
  SpriteBatchExtensions.cs  SpriteFont-compatible DrawString overloads
  FontSystemSettings.cs     atlas/raster settings (cloned on construction)
  Internal/
    GlyphCache.cs           fallback resolution, advances, kerning, rasterized glyph cache
    TextureAtlas.cs         RGBA atlas upload, padding extrusion, page allocation, disposal
    ShelfPacker.cs          pure row packer for one page
    IGlyphAtlas.cs          test seam; fake atlases record glyphs without a GPU
    Glyphs.cs               metrics/rendered-glyph/placement value types
FontStashSharp2.Tests/      xUnit tests; Fonts/ABeeZee-Regular.ttf is the fixture
Samples/HelloWorld/         DesktopGL sample and screenshot mode
Samples/MeasureText/        console sample, no GraphicsDevice
Samples/Supersampling/      1x/2x/4x atlas comparison at multiple draw scales
Samples/Benchmark/          DrawString benchmark vs runtime-baked SpriteFont
docfx/                      docfx.json, articles, API reference, screenshots
```

## Invariants

### On-demand caching

Measurement must never rasterize or upload a glyph. `DynamicSpriteFont.MeasureString` may populate
`GlyphCache` metrics/kerning, but only `GetRenderedGlyph` (called by drawing or `PreloadGlyphs`) may
touch `IGlyphAtlas`. `DynamicSpriteFont.GlyphCount` counts atlas glyphs and must stay 0 after
measurement. Tests in `MeasurementTests`/`GlyphCacheTests` enforce this.

### Coordinates and layout

- `position` is the top-left of the text block.
- The baseline of the first line is `position.Y + Ascent`; each following line adds `LineSpacing`.
- StbTrueTypeNext glyph offsets are baseline-relative with Y growing downward (negative above the
  baseline). Do not flip the sign.
- Advances and kerning are converted from font units with
  `ScaleForMappingEmToPixels(FontSize)` (em sizing), not `ScaleForPixelHeight`.
- Glyph bitmaps are rasterized at `fontScale * SupersamplingFactor`. Bitmap offsets are divided by
  the factor before layout so advances, ascent, and line spacing stay nominal. Atlas source rectangles
  remain physical; draws use `requestedScale / SupersamplingFactor`.
- Kerning applies only when the previous and current glyphs come from the same source; never kern
  across a line break.
- Flips mirror the whole block around its measured size. Per-glyph destination positions are
  pre-transformed, and each `SpriteBatch.Draw` uses `Vector2.Zero` as its origin. Rotation is applied
  to the scaled layout offset, matching the `SpriteFont` transform.

### Atlas

- Pages are 1024x1024 by default, created lazily, capped by `MaxTextureCount`.
- Padding is included in the packed slot; glyph edge pixels are extruded into the padding. Source
  rectangles exclude the padding.
- `SupersamplingFactor` (default 2) multiplies glyph pixel dimensions; atlas usage grows roughly with
  the square of the factor. Do not apply the factor to advances, kerning, or line metrics.
- Texture pages are `SurfaceFormat.Color` and premultiplied by default (RGB = alpha).
- `TextureAtlas` is lock-protected. `FontSystem`, fonts, and `GlyphCache` are **not** thread-safe.
- Uploading a newly rasterized glyph may happen between `SpriteBatch.Draw` calls; do not introduce
  `GraphicsDevice.SetData` calls in a way that requires a batch flush.

### Draw-path performance

- `DynamicSpriteFont.DrawText` dispatches to a dedicated default path when rotation is 0, origin is
  0, scale is 1, effects are None, and layer depth is 0. It caches the current atlas texture across
  glyphs and uses the four-argument `SpriteBatch.Draw` when `SupersamplingFactor` is 1, or an
  inverse-scaled draw otherwise. The transformed path keeps per-glyph origin/rotation/scale/effects
  handling and divides the draw scale by the supersampling factor.
- `GlyphCache` keeps ASCII metrics, rendered glyphs, and kerning pairs in fixed arrays (ASCII kerning
  is a lazily filled 128×128 table). Non-ASCII falls back to dictionaries. Do not remove these fast
  paths without running `Samples/Benchmark`.
- A rendered glyph is cached once per font size; measurement never populates the rendered cache.

### Public API compatibility

- Draw overload signatures must keep mirroring `SpriteBatch.DrawString` (string and StringBuilder;
  float and Vector2 scale; rotation/origin/effects/layerDepth).
- `SpriteFontBase` is the font parameter type. Internal abstract `DrawText` intentionally prevents
  external derivation.
- Fonts are cached per exact float size by `FontSystem.GetFont`; repeated calls return the same
  instance and glyph cache.
- `AddFont` clears future font instances (new fallback chain). Existing fonts keep their snapshot.
- Disposal is idempotent and invalidates the system (`ObjectDisposedException` on further use).

## Tests

Tests use a fake `IGlyphAtlas` so caching/rasterization can be tested without a GPU. Prefer this seam
over spinning up MonoGame in unit tests. `Samples/HelloWorld --screenshot` is the visual integration
check; `Samples/Benchmark --screenshot` verifies the runtime-baked SpriteFont and benchmark screen;
`Samples/Supersampling --screenshot` verifies the 1x/2x/4x comparison. Keep tests deterministic; do
not depend on system fonts.

When adding behavior, add tests for:

- measurement math (advances, kerning, newlines, tabs, surrogates),
- unchanged on-demand behavior,
- invalid input and disposal,
- packing edge cases.

## Benchmarking

`Samples/Benchmark` is the source of truth for DrawString performance claims.

- Compare the same TTF, pixel size, text, and pre-baked atlas for both engines. The benchmark uses
  `SupersamplingFactor = 1` so SpriteFont and FontStashSharp2 both draw 1:1 atlases.
- Exclude first-use glyph rasterization, JIT, and batch growth from measured phases via warmup.
- Alternate mode order between rounds and report CPU submission (small clipped target) separately
  from full rendering (1280×720) so GPU fill does not hide CPU regressions.
- Never special-case the benchmark harness for one path. If the library draw path changes, rerun the
  benchmark and update this documentation if the methodology changes.
- The benchmark requires a graphics device/display; use `--benchmark` for console output and
  `--screenshot` for a visual check.

## Assets and licensing

- `FontStashSharp2` is MIT (see `LICENSE`).
- The bundled ABeeZee font is SIL OFL 1.1. Keep `ABeeZee-OFL.txt` next to every copy and update
  `THIRD-PARTY-NOTICES.md` if assets change.
- Do not add fonts with unclear redistribution rights.

## Docs

Public API XML documentation is required (`GenerateDocumentationFile` is on). Keep `README.md`,
`docfx/articles/*.md` (including `supersampling.md`), and `AGENTS.md` in sync when behavior or public
API changes. Regenerate API
docs with docfx; `docfx/api/` and `docfx/_site/` are generated and git-ignored.

## Style

- C# 12, file-scoped namespaces, nullable enabled, tab indentation (4-column tabs), LF line endings.
- Use `ArgumentNullException.ThrowIfNull`, `ObjectDisposedException.ThrowIf`, and explicit bounds
  checks for public entry points.
- Keep public members documented; internal members should have short explanatory comments.
- Avoid introducing new dependencies unless they are essential and cross-platform.
