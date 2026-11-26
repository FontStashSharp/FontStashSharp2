# FontStashSharp2

**On-demand TrueType text rendering for MonoGame, drawn through `SpriteBatch`.**

![Hello World sample](docfx/images/hello-world.png)

FontStashSharp2 parses TTF/OTF files with [StbTrueTypeNext](https://www.nuget.org/packages/StbTrueTypeNext)
and rasterizes each glyph into a shared texture atlas the first time it is needed. Text is drawn with
extension methods on `SpriteBatch` whose signatures mirror the built-in `SpriteFont` overloads.

## Why another text library?

- **No content pipeline.** Ship `.ttf`/`.otf` files and load them at runtime.
- **On demand.** Measuring a string does not touch the GPU; only glyphs that are actually drawn are
  rasterized and uploaded.
- **Any size, one atlas.** Different font sizes and fallback fonts share one growable atlas.
- **SpriteBatch native.** `spriteBatch.DrawString(font, text, position, color)` with the familiar
  `SpriteFont` parameter list, including rotation, origin, scale, effects, and layer depth.
- **Real metrics.** Font units, scaling, kerning (GPOS/kern via StbTrueTypeNext), line spacing, and
  `MeasureString` are handled by the library.
- **Sharper scaling.** Glyphs are supersampled 2× by default and drawn at the inverse scale, with a
  configurable factor for heavier zoom.

## Quick start

Add a project reference (or, once published, the `FontStashSharp2` package) and copy a font next to
your executable:

```xml
<ItemGroup>
  <None Update="Content\MyFont.ttf" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

```csharp
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class MyGame : Game
{
    private SpriteBatch _spriteBatch = null!;
    private FontSystem _fonts = null!;
    private DynamicSpriteFont _title = null!;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _fonts = new FontSystem(new FontSystemSettings
        {
            TextureWidth = 1024,
            TextureHeight = 1024,
            SupersamplingFactor = 2f, // default; use 1 to disable
        });

        _fonts.AddFont(File.ReadAllBytes("Content/MyFont.ttf"));
        _title = _fonts.GetFont(32f);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();
        _spriteBatch.DrawString(_title, "Hello, MonoGame!", new Vector2(40, 40), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _fonts.Dispose();
        _spriteBatch.Dispose();
        base.UnloadContent();
    }
}
```

The first time `"Hello, MonoGame!"` is drawn, only the distinct glyphs in that string are rasterized.
Later draws reuse them from the atlas.

## Drawing API

The extension methods accept `SpriteFontBase` (the type returned by `FontSystem.GetFont`) and mirror
`SpriteBatch.DrawString`:

```csharp
spriteBatch.DrawString(font, text, position, color);
spriteBatch.DrawString(font, text, position, color, rotation, origin, scale, effects, layerDepth);
spriteBatch.DrawString(font, text, position, color, rotation, origin, new Vector2(sx, sy), effects, layerDepth);
```

`StringBuilder` overloads are available as well.

### Custom renderers

Drawing is not limited to `SpriteBatch`. Implement `IFontStashRenderer` and use the matching
`DrawText` overloads:

```csharp
public sealed class MyRenderer : IFontStashRenderer
{
    public MyRenderer(GraphicsDevice device) => GraphicsDevice = device;

    public GraphicsDevice GraphicsDevice { get; }

    public void Draw(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        // Submit this glyph quad to your renderer.
    }
}

font.DrawText(renderer, "Hello", position, color);
```

The font pre-transforms glyph positions, so the renderer receives the top-left of each glyph quad and
rotates/scales around that point (origin zero). `SpriteEffects` is included so flips work without a
second source-rectangle convention. `Samples/CustomRenderer` uses a `SpriteBatch`-backed
implementation of this interface for its left column and a vertex-based `IFontStashRenderer2` for the
right column. See the [custom renderer article](docfx/articles/custom-renderers.md) for complete
adapters and implementation guidance.

For vertex-level pipelines, implement `IFontStashRenderer2` instead:

```csharp
public interface IFontStashRenderer2
{
    GraphicsDevice GraphicsDevice { get; }

    void DrawQuad(
        Texture2D texture,
        ref VertexPositionColorTexture topLeft,
        ref VertexPositionColorTexture topRight,
        ref VertexPositionColorTexture bottomLeft,
        ref VertexPositionColorTexture bottomRight);
}
```

Vertex positions are in screen space, Z is the layer depth, and texture coordinates already include
`SpriteEffects` mirroring. Submit the quad as two triangles, for example
`top-left, top-right, bottom-left` and `top-right, bottom-right, bottom-left`. The same `DrawText`
overloads are available for string, `StringBuilder`, spans, and full transforms.

## Benchmarking against SpriteFont

`Samples/Benchmark` runs the same TTF at the same pixel size through both FontStashSharp2 and a
runtime-baked MonoGame `SpriteFont`, warms both to a pre-baked steady state, and measures
`DrawString` throughput. It reports CPU submission timings (64×64 offscreen target), full
1280×720 timings, throughput, and managed allocations.

```bash
dotnet run --project Samples/Benchmark
dotnet run --project Samples/Benchmark -- --benchmark
dotnet run --project Samples/Benchmark -- --benchmark --texts 256 --font-size 24 --iterations 200
dotnet run --project Samples/Benchmark -- --screenshot benchmark.png
```

The benchmark requires a graphics device/display. Use `--rounds`, `--warmup`, `--no-kerning`, and `--help` for more control. The benchmark sets
supersampling to 1× so both engines draw 1:1 atlases. Ratios greater than 1
mean FontStashSharp2 is faster. Numbers vary by CPU/GPU; the sample is intended for comparisons on
the same machine.

## Supersampled text scaling

Glyphs are rasterized at `FontSize * SupersamplingFactor` and drawn at the inverse scale, while all
layout metrics (advances, kerning, ascent, line spacing) stay at the nominal `FontSize`. The default
factor is **2×**, so text scaled up to roughly 2× remains sharp without extra draw calls.

```csharp
var fonts = new FontSystem(new FontSystemSettings
{
    SupersamplingFactor = 2f, // 1 = off, 3-4 for heavy camera zoom
});
```

- Atlas memory grows roughly with the square of the factor.
- Set it to `1` to rasterize at the nominal size.
- Use `SamplerState.LinearClamp` (the default) when drawing; point sampling will alias the downscale.
- The factor is fixed per `FontSystem`; scaling beyond it still softens, so increase it or request a
  larger font size for extreme zoom.

See the [supersampling article](docfx/articles/supersampling.md) for the full explanation, tradeoffs,
and the dedicated `Samples/Supersampling` comparison.

## Measurement without a graphics device

`MeasureString` only needs parsed metrics, so it works on a background thread or a server before any
atlas exists:

```csharp
using var fonts = new FontSystem();
fonts.AddFont(File.ReadAllBytes("Content/MyFont.ttf"));
var font = fonts.GetFont(24f);

Vector2 size = font.MeasureString("Measure me");
Vector2 centered = new(
    (viewportWidth - size.X) / 2f,
    (viewportHeight - size.Y) / 2f);
```

The atlas is created lazily from the `GraphicsDevice` on the first `SpriteBatch`/renderer draw or
preload. If you only measure, no GPU resources are touched.

## On-demand atlas behavior

- `MeasureString`, line breaking, fallback resolution, and advance widths are CPU-only.
- `DynamicSpriteFont.GlyphCount` counts glyphs that have been rasterized; measuring leaves it at 0.
- First draw of a glyph rasterizes it, uploads it to the atlas, and caches its atlas rectangle.
- The atlas is bound to the first `GraphicsDevice` supplied by a `SpriteBatch` or renderer; use one
  `FontSystem` per device.
- `PreloadGlyphs(spriteBatch, ...)` (or the renderer/device overloads) rasterizes ahead of time to
  avoid a first-use hitch during gameplay.
- Atlas pages are allocated lazily; glyph padding is filled by extending edge pixels to avoid seams.

## Font fallback

Add multiple sources in priority order. A code point missing from the first source is looked up in the
next one; when no source has it, the first font's `.notdef` glyph is used (usually a box).

```csharp
fonts.AddFont(latinBytes);
fonts.AddFont(symbolsBytes);
```

## Requirements

- .NET 8 (`net8.0`)
- MonoGame 3.8.1.303 for the library (compiled against DesktopGL with `PrivateAssets="All"`; add the
  MonoGame backend package for your platform). Samples reference latest MonoGame 3.8.6.1.
- C# 12 or newer
- StbTrueTypeNext 1.0.3 (restored automatically)

## Building and testing

```bash
dotnet build FontStashSharp2.MonoGame.sln
dotnet test FontStashSharp2.Tests/FontStashSharp2.Tests.csproj
```

Run the samples:

```bash
dotnet run --project Samples/HelloWorld
dotnet run --project Samples/MeasureText
dotnet run --project Samples/MeasureText -- 48 "Custom text"
dotnet run --project Samples/Supersampling
dotnet run --project Samples/Supersampling -- --screenshot supersampling.png
dotnet run --project Samples/CustomRenderer
dotnet run --project Samples/CustomRenderer -- --screenshot custom-renderer.png
dotnet run --project Samples/Benchmark
dotnet run --project Samples/Benchmark -- --benchmark
```

Render a deterministic screenshot without keeping a window open:

```bash
dotnet run --project Samples/HelloWorld -- --screenshot out.png
```

Build the documentation:

```bash
dotnet tool restore
dotnet docfx docfx/docfx.json
```

## Project layout

```
FontStashSharp2/           Library (net8.0)
FontStashSharp2.Tests/     xUnit tests and test font (ABeeZee)
Samples/HelloWorld/        DesktopGL SpriteBatch sample (supports --screenshot)
Samples/MeasureText/       Console measurement sample (no graphics device)
Samples/Supersampling/     1x/2x/4x atlas comparison at multiple draw scales
Samples/CustomRenderer/    IFontStashRenderer sample with drop shadow
Samples/Benchmark/         DrawString benchmark vs runtime-baked SpriteFont
docfx/                     docfx configuration and articles
```

## Behavior notes and limitations

- `FontSize` is the em size in pixels. Default `LineSpacing` is
  `(ascent - descent + lineGap) * size / unitsPerEm`; set `LineSpacing` to override it.
- `\n` and `\r\n` start new lines. Tabs advance like a single space. Other control characters are
  resolved through the font fallback chain.
- Flips mirror the whole text block; rotation, origin, and scale behave like `SpriteFont` drawing.
- Glyphs are rasterized at `FontSize * SupersamplingFactor` (default 2×) and drawn at the inverse
  scale. This keeps moderate scaling sharp; scaling beyond the factor still softens, so raise the
  factor or request a larger font size for extreme zoom. Set `SupersamplingFactor = 1` to disable.
- No complex text shaping: Arabic/Indic joining, bidirectional layout, ligature substitution, color
  emoji, and variable-font axes are not implemented. Simple fallback by code point is supported.
- The atlas is premultiplied alpha by default, matching `BlendState.AlphaBlend`. Pass premultiplied
  colors (opaque colors are already premultiplied) or disable `PremultiplyAlpha` for non-premultiplied
  blending.
- `FontSystem` and fonts are not thread-safe. Measure-only systems can be used off the render thread,
  but shared glyph caches should not be used concurrently.
- Add all font sources before creating fonts. `AddFont` invalidates the size cache; existing font
  instances keep their original fallback snapshot.

## License

FontStashSharp2 is MIT licensed. The sample/test font **ABeeZee** is licensed under the SIL Open Font
License 1.1; see `THIRD-PARTY-NOTICES.md`.
