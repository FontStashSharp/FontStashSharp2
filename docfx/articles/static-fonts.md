# Static bitmap fonts

`StaticSpriteFont` loads a pre-generated AngelCode BMFont atlas instead of rasterizing TrueType
glyphs at runtime. It derives from `SpriteFontBase` and supplies only BMFont glyph/page data; the
shared base pipeline handles measurement, effects, decorations, spacing, rotation, scaling, flips,
and all draw overloads.

The BMFont descriptor is parsed by [SharpFNT](https://github.com/AuroraBertaOldham/SharpFNT); page
images are decoded with [StbImageNext](https://github.com/StbSharp/StbImageNext).

## Loading

Load a descriptor from a file:

```csharp
using FontStashSharp2;

using var font = new StaticSpriteFont(
    Path.Combine(AppContext.BaseDirectory, "Content", "MyFont.fnt"));
```

Relative page paths are resolved against the descriptor's directory. The constructor accepts text,
XML, and binary BMFont files.

Descriptor bytes and streams are also supported:

```csharp
using var fromBytes = new StaticSpriteFont(fontBytes, contentDirectory);
using var fromStream = new StaticSpriteFont(fontStream, contentDirectory);
```

The stream overload reads to the end and leaves the stream open. When no base directory is supplied,
relative page paths are resolved against the current working directory.

## Alpha mode

BMFont page images are treated as straight alpha, which is what BMFont tools normally export. The
constructor's `GlyphAlphaMode` argument controls the RGBA data uploaded to the texture:

```csharp
using var premultiplied = new StaticSpriteFont("Content/MyFont.fnt");
using var straight = new StaticSpriteFont(
    "Content/MyFont.fnt",
    GlyphAlphaMode.NonPremultiplied);
```

- `Premultiplied` is the default. It stores `(RGB * A, A)` and matches the dynamic font atlas and
  MonoGame's `BlendState.AlphaBlend`.
- `NonPremultiplied` keeps `(RGB, A)` for standard source-alpha blending.
- `NoAntialiasing` thresholds coverage to fully opaque or fully transparent pixels.

The selected mode is available through `AlphaMode`.

## Metrics

The BMFont `info` and `common` blocks provide the font metrics:

- `FontSize` comes from `info size`, falling back to `common lineHeight`.
- `LineSpacing` comes from `common lineHeight` and can be changed.
- `Ascent` is `common base`.
- `Descent` is `common base - common lineHeight` and is negative.

`MeasureString` uses the BMFont advances and kerning pairs. It never creates textures, so measuring is
safe before a graphics device exists.

## Drawing and texture lifetime

Page images are decoded during construction but uploaded to the graphics device or
`ITexture2DManager` lazily on the first draw. One `StaticSpriteFont` belongs to one device; passing a
different device throws. Create one instance per device.

Use `PreloadTextures` to upload every page before the first draw:

```csharp
font.PreloadTextures(GraphicsDevice); // MonoGame
font.PreloadTextures(textureManager); // platform-agnostic
```

`PreloadTextures` returns the number of page textures created by the call.

Dispose the font when it is no longer needed. This destroys all page textures and the 1x1 white
texture used for decorations:

```csharp
font.Dispose();
```

## Coordinates

BMFont `xoffset`/`yoffset` are relative to the cursor and the top of the line, not the baseline. The
draw path follows the format directly:

- Glyph top-left is `(penX + xoffset, lineTop + yoffset)`.
- `xadvance` moves the cursor.
- Multi-page atlases use the `page` field on each character.
- Missing characters fall back to the `-1` character when the descriptor provides one.

## Example

```csharp
using var font = new StaticSpriteFont("Content/MyFont.fnt");

spriteBatch.DrawString(
    font,
    "Static BMFont",
    new Vector2(40f, 40f),
    Color.White,
    decoration: TextDecorationOptions.Underline(2f, Color.OrangeRed));
```

The same font works through the custom renderers:

```csharp
font.DrawText(
    renderer,
    "Static BMFont",
    new Vector2(40f, 40f),
    Color.White,
    effect: TextEffectOptions.Shadow(3, Color.Black));
```

Effects and decorations reuse the same BMFont glyph rectangles. Effects submit extra glyph passes;
decorations submit solid quads from a dedicated white texture.

## Sample

[Samples/StaticFont](samples.md#static-bmfont-desktopgl) loads 18 px and 40 px BMFont atlases,
preloads their page textures, and demonstrates measurement, kerning, scaling, effects, decorations,
rotation, and flips through `SpriteBatch`.
