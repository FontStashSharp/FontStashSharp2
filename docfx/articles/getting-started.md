# Getting started

## 1. Reference the library

Add a project reference (or the `FontStashSharp2` NuGet package once published):

```xml
<ItemGroup>
  <ProjectReference Include="..\FontStashSharp2\FontStashSharp2.csproj" />
</ItemGroup>
```

The library references `MonoGame.Framework.DesktopGL` so the sample projects are self-contained. The
MonoGame assembly identity is the same across platform packages; use the MonoGame package appropriate
for your target.

## 2. Ship a font with your game

There is no content pipeline step. Copy the font next to the executable:

```xml
<ItemGroup>
  <None Update="Content\MyFont.ttf" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## 3. Create a font system and load a font

Create the `FontSystem` after the `GraphicsDevice` exists (typically in `LoadContent`):

```csharp
using FontStashSharp2;

private FontSystem _fonts = null!;
private DynamicSpriteFont _font = null!;

protected override void LoadContent()
{
    _fonts = new FontSystem(GraphicsDevice, new FontSystemSettings
    {
        TextureWidth = 1024,
        TextureHeight = 1024,
        GlyphPadding = 1,
        SupersamplingFactor = 2f,
        UseKerning = true,
    });

    var path = Path.Combine(AppContext.BaseDirectory, "Content", "MyFont.ttf");
    _fonts.AddFont(File.ReadAllBytes(path), name: "My Font");

    _font = _fonts.GetFont(32f);
}
```

`AddFont` also accepts a `Stream` or a path. For a font collection, pass the zero-based face index:

```csharp
_fonts.AddFont(bytes, fontIndex: 1);
```

## 4. Draw

Use the extension methods inside a `SpriteBatch.Begin`/`End` pair:

```csharp
protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);

    _spriteBatch.Begin(
        samplerState: SamplerState.LinearClamp,
        blendState: BlendState.AlphaBlend);

    _spriteBatch.DrawString(_font, "Hello, MonoGame!", new Vector2(40, 40), Color.White);

    _spriteBatch.End();
    base.Draw(gameTime);
}
```

The full parameter list mirrors `SpriteBatch.DrawString`:

```csharp
spriteBatch.DrawString(
    font,
    text,
    position,
    color,
    rotation,
    origin,
    scale,
    effects,
    layerDepth);
```

`origin` is measured in text layout pixels (for example `font.MeasureString(text) / 2f` to rotate
around the center). `StringBuilder` overloads and a `Vector2` scale overload are available.

## 5. Measure and align

`MeasureString` returns the width of the widest line and the total height:

```csharp
var size = _font.MeasureString("Centered text");
var position = new Vector2(
    (viewportWidth - size.X) / 2f,
    (viewportHeight - size.Y) / 2f);

_spriteBatch.DrawString(_font, "Centered text", position, Color.White);
```

Line height comes from the font's vertical metrics:

```csharp
float lineSpacing = _font.LineSpacing; // ascent - descent + line gap, scaled to FontSize
```

`LineSpacing` is settable if you want tighter or looser lines.

## 6. Fallback fonts

Sources are searched in the order they were added:

```csharp
_fonts.AddFont(latinBytes);
_fonts.AddFont(symbolBytes);
```

If no source contains a code point, the first source's `.notdef` glyph is used. Kerning is applied
only within a single source.

## 7. Avoid first-draw hitches

Rasterize the glyphs for known UI strings while loading:

```csharp
int added = _font.PreloadGlyphs("Score: 0000  Lives: 3");
```

This uses the same cache as drawing, so the first real draw is upload-free.

## 8. Inspect the atlas

```csharp
Console.WriteLine($"Pages: {_fonts.TextureCount}");
Console.WriteLine($"Rasterized glyphs: {_font.GlyphCount}");
Console.WriteLine($"Resolved code points: {_font.CachedGlyphCount}");
```

`GlyphCount` counts glyphs uploaded to the atlas. Measurement leaves it at zero, which is a useful
sanity check for the on-demand behavior. `FontSystem.GetTexture(index)` exposes atlas pages for
debugging (do not dispose them).

## 9. Measurement-only systems

For tools, server-side layout, or tests, omit the graphics device:

```csharp
using var fonts = new FontSystem();
fonts.AddFont(File.ReadAllBytes("Content/MyFont.ttf"));
var font = fonts.GetFont(24f);
Vector2 size = font.MeasureString("Measured without a GPU");
```

Drawing or preloading on such a system throws `InvalidOperationException` with a message explaining
that a `GraphicsDevice` is required.

## 10. Dispose

Dispose the font system when the game unloads. This releases all atlas textures:

```csharp
protected override void UnloadContent()
{
    _fonts.Dispose();
    _spriteBatch.Dispose();
    base.UnloadContent();
}
```

## Settings reference

| Setting | Default | Meaning |
| --- | --- | --- |
| `TextureWidth` | 1024 | Width of each atlas page. |
| `TextureHeight` | 1024 | Height of each atlas page. |
| `MaxTextureCount` | 8 | Maximum number of pages before adding a glyph throws. |
| `GlyphPadding` | 1 | Transparent/extruded border around each glyph. |
| `SupersamplingFactor` | 2 | Glyphs rasterize at `FontSize * factor`; atlas memory grows ~factor². |
| `UseKerning` | `true` | Apply kerning pairs. |
| `PremultiplyAlpha` | `true` | Store RGB = alpha in the atlas (matches `BlendState.AlphaBlend`). |

Settings are copied when the `FontSystem` is constructed; reuse or mutate the settings object freely
afterwards.

## Behavior details

- `FontSize` is the em size in pixels.
- `\n` and `\r\n` break lines. Tab advances by one space.
- `SpriteEffects.FlipHorizontally` / `FlipVertically` mirror the whole text block.
- Glyphs are rasterized at `FontSize * SupersamplingFactor` (default 2×) and drawn at the inverse
  scale. This keeps text sharp up to roughly the configured factor; raise it for extreme zoom or use a
  larger font size. Set `SupersamplingFactor = 1` to disable the extra atlas resolution.
- Supersampling increases atlas memory roughly quadratically. Use `SamplerState.LinearClamp` so the
  downscale is filtered; point sampling will alias. See [Supersampling](supersampling.md) for the full
  explanation and factor guidance.
- Bitmap fonts are not supported; a scalable TTF/OTF outline is required.
- Text colors should be premultiplied when using the default `BlendState.AlphaBlend`. Opaque colors
  such as `Color.White` and `Color.Red` are already premultiplied.
