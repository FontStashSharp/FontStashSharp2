# FontStashSharp2 - repository guide for AI agents

This file describes how the repository is organized and which invariants must be preserved when
changing code. Read it before making non-trivial edits.

## What this project is

A small text rendering library for MonoGame and other C# game engines (.NET 8 / C# 12). It parses
TrueType/OpenType fonts with **StbTrueTypeNext** and rasterizes glyphs on demand into a shared texture
atlas. The MonoGame build draws through `SpriteBatch` extension methods that mirror the `SpriteFont`
API. The platform-agnostic build has no MonoGame dependency and draws only through
`IFontStashRenderer`/`IFontStashRenderer2` using an engine-supplied `ITexture2DManager`.

Do **not** copy implementation code or architecture from the upstream FontStashSharp project. This is
an independent implementation. Familiar public API shape is fine; internals should be our own.

## Build, test, run

```bash
dotnet build FontStashSharp2.MonoGame.sln
dotnet test FontStashSharp2.Tests/FontStashSharp2.Tests.csproj
dotnet build FontStashSharp2.PlatformAgnostic.sln
dotnet test FontStashSharp2.PlatformAgnostic.Tests/FontStashSharp2.PlatformAgnostic.Tests.csproj
dotnet run --project Samples/HelloWorld
dotnet run --project Samples/MeasureText
dotnet run --project Samples/Benchmark
dotnet run --project Samples/Benchmark -- --benchmark
dotnet run --project Samples/Supersampling
dotnet run --project Samples/CustomRenderer
dotnet run --project Samples/TextEffects
dotnet run --project Samples/OpenTK
dotnet run --project Samples/SilkNet
dotnet run --project Samples/OpenTK -- --frames 3
dotnet run --project Samples/SilkNet -- --frames 3
dotnet run --project Samples/HelloWorld -- --screenshot out.png
dotnet run --project Samples/Benchmark -- --screenshot benchmark.png
dotnet run --project Samples/Supersampling -- --screenshot supersampling.png
dotnet run --project Samples/CustomRenderer -- --screenshot custom-renderer.png
```

Documentation:

```bash
dotnet tool restore
dotnet docfx docfx/docfx.json
```

The library and tests pin MonoGame 3.8.1.303 through `MonoGameVersion`. Samples use the latest
MonoGame 3.8.6.1 through `LatestMonoGameVersion`. StbTrueTypeNext is pinned through
`StbTrueTypeNextVersion`. The package/assembly version is defined once in `Directory.Build.props`; do
not add per-project `<Version>` elements.

The screenshot mode is also a useful end-to-end smoke test on a machine with a graphics device.

## Layout

```
FontStashSharp2/
  FontSystem.cs             owns sources, cached fonts, atlas; AddFont/GetFont/GetTexture
  FontSource.cs             wrapper around StbTrueTypeNext STBFont + name/metrics
  IFontStashRenderer.cs     quad renderer abstraction for drawing without SpriteBatch
  IFontStashRenderer2.cs    vertex-quad renderer abstraction
  DynamicSpriteFont.cs      per-size metadata, MeasureString, PreloadGlyphs, DrawText
  SpriteFontBase.cs         public base type + drawing hooks and convenience overloads
  SpriteBatchExtensions.cs  SpriteFont-compatible DrawString overloads
  FontSystemSettings.cs     atlas/raster settings (cloned on construction)
  Internal/
    GlyphCache.cs           fallback resolution, advances, kerning, rasterized glyph cache
    TextureAtlas.cs         RGBA atlas upload, padding extrusion, page allocation, disposal
    ShelfPacker.cs          pure row packer for one page
    IGlyphAtlas.cs          test seam; fake atlases record glyphs without a GPU
    Glyphs.cs               metrics/rendered-glyph/placement value types
FontStashSharp2.Tests/      xUnit tests; Fonts/ABeeZee-Regular.ttf is the fixture
FontStashSharp2.PlatformAgnostic/
  FontStashSharp2.PlatformAgnostic.csproj
                            links the shared sources without MonoGame; PLATFORM_AGNOSTIC defined
FontStashSharp2.PlatformAgnostic.Tests/
                            renderer/atlas tests using a fake ITexture2DManager
Samples/HelloWorld/         DesktopGL sample and screenshot mode
Samples/MeasureText/        console sample, no GraphicsDevice
Samples/Supersampling/      1x/2x/4x atlas comparison at multiple draw scales
Samples/CustomRenderer/     IFontStashRenderer + IFontStashRenderer2 sample
Samples/TextEffects/        SpriteBatch TextEffect None/Stroke/Shadow sample
Samples/OpenTK/             platform-agnostic OpenTK/OpenGL renderer sample
Samples/SilkNet/            platform-agnostic Silk.NET/OpenGL renderer sample
Samples/Benchmark/          DrawString benchmark vs runtime-baked SpriteFont
docfx/                      docfx.json, articles, API reference, screenshots
```

## Invariants

### On-demand caching

Measurement must never rasterize or upload a glyph. `DynamicSpriteFont.MeasureString` may populate
`GlyphCache` metrics/kerning, but only `GetRenderedGlyph` (called by drawing or `PreloadGlyphs`) may
touch `IGlyphAtlas`. `DynamicSpriteFont.GlyphCount` counts atlas glyphs and must stay 0 after
measurement. Tests in `MeasurementTests`/`GlyphCacheTests` enforce this.

The atlas is created on the first draw or preload from the `SpriteBatch`, `IFontStashRenderer`, or
`IFontStashRenderer2` device. A different device must use a separate `FontSystem`. Public
`PreloadGlyphs` overloads therefore require a batch/renderer/device; tests use the internal fake-atlas
overload.

`FontSystem.TextureAtlasFull` fires before a new page is allocated, but not for the first page. The
handler runs synchronously on the drawing thread. `FontSystem.ClearTextures` disposes every page and
must invalidate rendered glyph caches (metrics/kerning stay). Old `DynamicSpriteFont` instances detect
the atlas generation change and re-rasterize lazily; never serve a cached `RenderedGlyph` across a
clear.

### AOT and trimming

- Both class libraries (`FontStashSharp2.MonoGame` and `FontStashSharp2.PlatformAgnostic`) set
  `IsAotCompatible`; keep them free of reflection, runtime code generation, and trim-unsafe APIs.
- Sample and test projects are applications/test hosts, not libraries. Do not add `PublishAot` or
  `IsAotCompatible` to them.
- The shared pipeline uses only static generics, dictionaries, `ArrayPool`, `System.Numerics`, and
  StbTrueTypeNext; it contains no unsafe blocks, P/Invoke, or reflection. Keep it that way.

### Platform-agnostic build

- `FontStashSharp2.PlatformAgnostic` compiles the shared sources with `PLATFORM_AGNOSTIC`; the MonoGame
  project compiles them with `MONOGAME`. Use `#if MONOGAME` / `#else` for divergent types and APIs.
- The platform-agnostic build must not reference MonoGame. `ITexture2DManager` is its only device
  abstraction: atlas textures are `object`, and uploads are row-major RGBA bytes.
- Its public draw surface consists only of `SpriteFontBase.DrawText` overloads that accept
  `IFontStashRenderer` or `IFontStashRenderer2`. Do not add `SpriteBatch` overloads or MonoGame types.
- One `FontSystem` is bound to one `ITexture2DManager`; atlas pages remember their manager and a second
  manager throws. `ITexture2DManager.DestroyTexture` is called from `FontSystem.Dispose`.
- `ITexture2DManager.SetTextureData` receives `ReadOnlySpan<byte>` with
  `bounds.Width * bounds.Height * 4` RGBA bytes. Never change this to BGRA or skip the alpha channel.

### Coordinates and layout

- `position` is the top-left of the text block.
- The baseline of the first line is `position.Y + Ascent`; each following line adds `LineSpacing` plus
  the optional draw `lineSpacing`. Each glyph advance includes the optional draw `characterSpacing`.
  Both values are layout pixels and are transformed by the draw scale.
- StbTrueTypeNext glyph offsets are baseline-relative with Y growing downward (negative above the
  baseline). Do not flip the sign.
- Advances and kerning are converted from font units with `FontSystemSettings.ScaleMode`.
  `ScaleForPixelHeight` is the default; `ScaleForMappingEmToPixels` opts into em sizing.
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
- MonoGame texture pages are `SurfaceFormat.Color`; both builds upload RGBA. `AlphaMode` defaults to
  `Premultiplied` (`a,a,a,a`); `NonPremultiplied` stores white + coverage; `NoAntialiasing` stores
  fully opaque or fully transparent pixels.
- The atlas-full event is raised inside the atlas lock right before `CreatePage`. Do not draw or call
  `ClearTextures` from the handler; inspect the event args and return.
- `TextureAtlas` is lock-protected. `FontSystem`, fonts, and `GlyphCache` are **not** thread-safe.
- Uploading a newly rasterized glyph may happen between `SpriteBatch.Draw` calls; do not introduce
  `GraphicsDevice.SetData`/`ITexture2DManager.SetTextureData` calls in a way that requires a batch
  flush.

### Draw-path performance

- `DynamicSpriteFont.DrawText` dispatches to a dedicated default path when rotation is 0, origin is
  0, scale is 1, effects are None, layer depth is 0, and `TextEffect` is `None`. It caches the current
  atlas texture across glyphs and uses the four-argument `SpriteBatch.Draw` when `SupersamplingFactor`
  is 1, or an inverse-scaled draw otherwise. The transformed path keeps per-glyph
  origin/rotation/scale/effects handling and divides the draw scale by the supersampling factor.
- A draw call takes `TextEffectOptions? effect`; `null` means no effect. `Stroke` submits the glyph at
  every square-ring offset up to `Amount`; `Shadow` submits one `(Amount, Amount)` offset. Both use
  `Color`, which defaults to white. Effect passes run before the main pass and are draw-time only (the
  atlas glyph is unchanged). Keep the no-effect fast path.
- `GlyphCache` keeps ASCII metrics, rendered glyphs, and kerning pairs in fixed arrays (ASCII kerning
  is a lazily filled 128×128 table). Non-ASCII falls back to dictionaries. Do not remove these fast
  paths without running `Samples/Benchmark`.
- A rendered glyph is cached once per font size; measurement never populates the rendered cache.

### Public API compatibility

- Draw overload signatures must keep mirroring `SpriteBatch.DrawString` (string and StringBuilder;
  float and Vector2 scale; rotation/origin/effects/layerDepth) and must accept the trailing optional
  `float characterSpacing = 0f, float lineSpacing = 0f, TextEffectOptions? effect = null`.
  `TextEffect` has only `Stroke` and `Shadow`; no `None` value.
- `TextEffectOptions.Amount` must be non-negative; `0` disables effects. Invalid effect kinds throw.
  Effect passes run before the main text pass and use the same atlas glyphs.
- `TextEffectOptions.Color` defaults to white when omitted.
- `SpriteFontBase` is the font parameter type. In the MonoGame build its public abstract span-based
  `DrawText` hooks accept a `SpriteBatch`, an `IFontStashRenderer`, or an `IFontStashRenderer2`; the
  platform-agnostic build exposes only the two renderer hooks. `string`/`StringBuilder` and
  position/color overloads delegate to them.
- MonoGame `IFontStashRenderer.Draw` and platform-agnostic `IFontStashRenderer.DrawText` receive
  pre-transformed glyph positions (origin zero) and include `SpriteEffects` so flips are representable.
  Do not remove the effects parameter; the generic draw path relies on it.
- `IFontStashRenderer2.DrawQuad` receives four screen-space vertices. `Position.Z` is the layer depth
  and texture coordinates already include `SpriteEffects` mirroring; do not apply origin transforms or
  flip UVs again in the renderer.
- Fonts are cached per exact float size by `FontSystem.GetFont`; repeated calls return the same
  instance and glyph cache.
- `AddFont` clears future font instances (new fallback chain). Existing fonts keep their snapshot.
- Disposal is idempotent and invalidates the system (`ObjectDisposedException` on further use).

## Tests

Tests use a fake `IGlyphAtlas` so caching/rasterization can be tested without a GPU. Prefer this seam
over spinning up MonoGame in unit tests. The platform-agnostic tests use a fake `ITexture2DManager` and
recording renderers, so they also run without a GPU. `Samples/OpenTK` and `Samples/SilkNet` are the
windowed platform-agnostic integration samples; both accept `--frames <n>` for a bounded smoke test.
`Samples/HelloWorld --screenshot` is the visual integration
check; `Samples/Benchmark --screenshot` verifies the runtime-baked SpriteFont and benchmark screen;
`Samples/Supersampling --screenshot` verifies the 1x/2x/4x comparison;
`Samples/CustomRenderer --screenshot` verifies the custom renderer overloads end-to-end.
`Samples/TextEffects --screenshot` verifies the draw-time TextEffect passes. Keep tests deterministic;
do not depend on system fonts.

When adding behavior, add tests for:

- measurement math (advances, kerning, newlines, tabs, surrogates),
- unchanged on-demand behavior,
- invalid input and disposal,
- packing edge cases.

## Benchmarking

`Samples/Benchmark` is the source of truth for DrawString performance claims.

- Compare the same TTF, pixel size, text, and pre-baked atlas for both engines. The benchmark uses
  `SupersamplingFactor = 1` so SpriteFont and FontStashSharp2 both draw 1:1 atlases, and
  `ScaleMode = ScaleForMappingEmToPixels` to match the runtime SpriteFont's em scaling.
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
`docfx/articles/*.md` (including `supersampling.md`, `custom-renderers.md`, `text-effects.md`, and
`platform-agnostic.md`), and `AGENTS.md` in sync when behavior or public API changes. Regenerate API
docs with docfx; `docfx/api/` and `docfx/_site/` are generated and git-ignored.

## Style

- C# 12, file-scoped namespaces, nullable enabled, tab indentation (4-column tabs), LF line endings.
- Use `ArgumentNullException.ThrowIfNull`, `ObjectDisposedException.ThrowIf`, and explicit bounds
  checks for public entry points.
- Keep public members documented; internal members should have short explanatory comments.
- Avoid introducing new dependencies unless they are essential and cross-platform.
