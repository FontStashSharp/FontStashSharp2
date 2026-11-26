# Getting started

## 1. Reference the library

Add a project reference (or the `FontStashSharp2` NuGet package once published):

```xml
<ItemGroup>
  <ProjectReference Include="..\FontStashSharp2\FontStashSharp2.MonoGame.csproj" />
</ItemGroup>
```

The library references `MonoGame.Framework.DesktopGL` 3.8.1.303 with `PrivateAssets="All"`, so it does
not impose a backend on consumers. Add the MonoGame backend package for your target (DesktopGL,
WindowsDX, Android, iOS, etc.); the sample projects reference DesktopGL 3.8.6.1 directly.

## 2. Ship a font with your game

There is no content pipeline step. Copy the font next to the executable:

```xml
<ItemGroup>
  <None Update="Content\MyFont.ttf" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## 3. Create a font system and load a font

Create a `FontSystem` and add fonts:

```csharp
using FontStashSharp2;

private FontSystem _fonts = null!;
private DynamicSpriteFont _font = null!;

protected override void LoadContent()
{
    _fonts = new FontSystem(new FontSystemSettings
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

### Text effects

Every draw overload accepts an optional `TextEffectOptions? effect` argument:

```csharp
_spriteBatch.DrawString(_font, "Stroked", new Vector2(40, 120), Color.White,
    effect: TextEffectOptions.Stroke(2, Color.Black));

_spriteBatch.DrawString(_font, "Shadowed", new Vector2(40, 180), Color.White,
    effect: TextEffectOptions.Shadow(3, new Color(80, 90, 120)));
```

- `null` means no effect.
- `TextEffectOptions.Stroke` draws the text at surrounding offsets before the main text.
- `TextEffectOptions.Shadow` draws the text offset down and right before the main text.
- `Amount` is the thickness/offset in layout pixels; `0` disables the effect.
- `Color` selects the stroke/shadow color; omitting it selects white.
- Negative amounts and invalid effect kinds throw.

The same arguments work with `font.DrawText(renderer, ...)`, `font.DrawText(renderer2, ...)`, and
`SpriteBatch.DrawString`. See [Text effects](articles/text-effects.md) for the full guide.

### Text decorations

Underlines and strikethroughs use an independent optional `TextDecorationOptions?` argument:

```csharp
_spriteBatch.DrawString(_font, "Underlined", position, Color.White,
    decoration: TextDecorationOptions.Underline(2));

_spriteBatch.DrawString(_font, "Struck through", position, Color.White,
    decoration: TextDecorationOptions.Strikethrough(1.5f, Color.OrangeRed));
```

- `null` or `TextDecoration.None` draws no line.
- `DecorationWidth` is in layout pixels and scales with the draw.
- `Color` defaults to `null`, meaning the main text color.
- Decorations are drawn after the effects and text.
- The same argument works with both custom renderers.

### Character and line spacing

Every draw overload also accepts additional spacing, in layout pixels. Both values are scaled with the
draw scale:

```csharp
_spriteBatch.DrawString(_font, "Wide text", position, Color.White,
    characterSpacing: 4);

_spriteBatch.DrawString(_font, "First\nSecond", position, Color.White,
    lineSpacing: 8);
```

- `characterSpacing` is added after each glyph advance and scales with the horizontal text scale.
- `lineSpacing` is added after each line break and scales with the vertical text scale.
- Both default to 0 and affect drawing only; `MeasureString` continues to use the font's metrics.

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

## Static BMFont fonts

`StaticSpriteFont` loads a pre-generated AngelCode BMFont atlas instead of rasterizing outlines:

```csharp
using var staticFont = new StaticSpriteFont("Content/MyFont.fnt");

_spriteBatch.DrawString(staticFont, "Static BMFont", position, Color.White);
```

The descriptor may be text, XML, or binary. Relative page images resolve against the descriptor's
directory, and page textures upload on the first draw or through
`PreloadTextures(GraphicsDevice)`. Page images are treated as straight alpha and premultiplied by
default, matching dynamic fonts and `BlendState.AlphaBlend`; pass `GlyphAlphaMode.NonPremultiplied`
for source-alpha blending. Measurement works before the device is known. See
[Static bitmap fonts](articles/static-fonts.md) for loading, metrics, multi-page atlases, and
lifetime details.

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
int added = _font.PreloadGlyphs(_spriteBatch, "Score: 0000  Lives: 3");
```

The `SpriteBatch`, `IFontStashRenderer`, `IFontStashRenderer2`, or `GraphicsDevice` overload supplies
the device used to allocate and upload the atlas. This uses the same cache as drawing, so the first
real draw is upload-free.

## 8. Inspect the atlas

```csharp
Console.WriteLine($"Pages: {_fonts.TextureCount}");
Console.WriteLine($"Rasterized glyphs: {_font.GlyphCount}");
Console.WriteLine($"Resolved code points: {_font.CachedGlyphCount}");
```

`GlyphCount` counts glyphs uploaded to the atlas. Measurement leaves it at zero, which is a useful
sanity check for the on-demand behavior. `TextureCount` is 0 until the first draw/preload creates the
atlas. `FontSystem.GetTexture(index)` exposes atlas pages for debugging (do not dispose them).

When no existing page can fit the next glyph, the atlas creates another page. Observe that transition:

```csharp
fonts.TextureAtlasFull += (_, e) =>
    Console.WriteLine($"Creating page {e.CurrentTextureCount} of up to {e.MaxTextureCount}");
```

To drop every atlas page and force glyphs to be uploaded again on the next draw or preload:

```csharp
fonts.ClearTextures();
```

`ClearTextures` keeps font sources, metrics, and kerning, resets `TextureCount`/`GlyphCount`, and
invalidates textures previously returned by `GetTexture`.

## 9. Measurement and device binding

Measurement works before any GPU resources are created:

```csharp
using var fonts = new FontSystem();
fonts.AddFont(File.ReadAllBytes("Content/MyFont.ttf"));
var font = fonts.GetFont(24f);
Vector2 size = font.MeasureString("Measured without a GPU");
```

The atlas is created from the active `SpriteBatch` or renderer on the first draw or preload. Use one
`FontSystem` per device.

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
| `ScaleMode` | `ScaleForPixelHeight` | Map font units by pixel height or by em square. |
| `MaxTextureCount` | 8 | Maximum number of pages before adding a glyph throws. |
| `GlyphPadding` | 1 | Transparent/extruded border around each glyph. |
| `SupersamplingFactor` | 2 | Glyphs rasterize at `FontSize * factor`; atlas memory grows ~factor². |
| `UseKerning` | `true` | Apply kerning pairs. |
| `AlphaMode` | `Premultiplied` | How coverage becomes RGBA: `Premultiplied`, `NonPremultiplied`, or `NoAntialiasing`. |

Settings are copied when the `FontSystem` is constructed; reuse or mutate the settings object freely
afterwards.

## Behavior details

- `FontSize` is measured in pixels. By default it maps the font's ascent-to-descent height
  (`FontScaleMode.ScaleForPixelHeight`); set `ScaleMode = FontScaleMode.ScaleForMappingEmToPixels`
  to map the em square instead.
- `\n` and `\r\n` break lines. Tab advances by one space.
- `SpriteEffects.FlipHorizontally` / `FlipVertically` mirror the whole text block.
- Glyphs are rasterized at `FontSize * SupersamplingFactor` (default 2×) and drawn at the inverse
  scale. This keeps text sharp up to roughly the configured factor; raise it for extreme zoom or use a
  larger font size. Set `SupersamplingFactor = 1` to disable the extra atlas resolution.
- Supersampling increases atlas memory roughly quadratically. Use `SamplerState.LinearClamp` so the
  downscale is filtered; point sampling will alias. See [Supersampling](articles/supersampling.md) for the full
  explanation and factor guidance.
- The dynamic `FontSystem` path requires TTF/OTF outlines. Use `StaticSpriteFont` for AngelCode
  BMFont atlases.
- Text colors should be premultiplied when using the default `GlyphAlphaMode.Premultiplied` atlas mode
  and `BlendState.AlphaBlend`. Opaque colors such as `Color.White` and `Color.Red` are already
  premultiplied.
- Use `GlyphAlphaMode.NonPremultiplied` with standard source-alpha blending, or
  `GlyphAlphaMode.NoAntialiasing` to threshold coverage to fully opaque or fully transparent pixels.

## Platform-agnostic package

For engines other than MonoGame, use `FontStashSharp2.PlatformAgnostic`. It has no MonoGame
dependency and draws only through `IFontStashRenderer`/`IFontStashRenderer2` plus an engine-supplied
`ITexture2DManager`. See [Platform-agnostic rendering](articles/platform-agnostic.md).
