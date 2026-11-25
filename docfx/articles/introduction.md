# Introduction

FontStashSharp2 is a small text rendering library for MonoGame. It parses TrueType/OpenType fonts
with StbTrueTypeNext and rasterizes glyphs into a texture atlas only when they are needed. Text is
submitted to `SpriteBatch` using extension methods that look and feel like `SpriteFont.DrawString`.

```csharp
spriteBatch.DrawString(font, "Hello, MonoGame!", new Vector2(40, 40), Color.White);
spriteBatch.DrawString(
    font,
    "Rotated",
    new Vector2(400, 200),
    Color.White,
    -0.1f,
    font.MeasureString("Rotated") / 2f,
    1f,
    SpriteEffects.None,
    0f);
```

## Design principles

### Lazy by default

`MeasureString` parses the font, resolves fallback, applies kerning, and computes advances without
allocating a texture. Glyphs are rasterized and uploaded only when a draw call (or `PreloadGlyphs`)
asks for them. This keeps startup fast and avoids baking glyphs you never show.

### One atlas for everything

All font sizes and all fallback sources share the same growing atlas. Pages are created only when the
current pages are full. Each page uses shelf packing, and each glyph is surrounded by padding whose
pixels replicate the glyph edge to avoid filtering seams.

### SpriteBatch first

The drawing API is intentionally boring:

```csharp
spriteBatch.DrawString(font, text, position, color);
spriteBatch.DrawString(font, text, position, color, rotation, origin, scale, effects, layerDepth);
spriteBatch.DrawString(font, text, position, color, rotation, origin, scaleVector, effects, layerDepth);
```

The `font` parameter is `SpriteFontBase` (usually a `DynamicSpriteFont` from
`FontSystem.GetFont`). The remaining parameters match `SpriteBatch.DrawString`.

### Measurement anywhere

A `FontSystem` can be created without a `GraphicsDevice`. Parsing, fallback, measurement, and line
layout work on a background thread. Drawing and preloading require a device and fail with a clear
exception otherwise.

## Feature summary

| Feature | Supported |
| --- | --- |
| TTF/OTF parsing | Yes, via StbTrueTypeNext |
| Font collections (`.ttc`/`.otc`) | Yes, by face index |
| Dynamic font sizes | Yes, cached per float size |
| On-demand glyph atlas | Yes |
| Multiple atlas pages | Yes, bounded by settings |
| Kerning | Yes, via StbTrueTypeNext GPOS/kern |
| Newlines (`\n`, `\r\n`) | Yes |
| Tabs | Rendered as a single space |
| Surrogate pairs | Yes (UTF-16), no shaping |
| Font fallback | Yes, by code point |
| Measurement without GPU | Yes |
| Preloading glyphs | Yes |
| Rotation, origin, scale, flips | Yes |
| Complex shaping / bidi / ligatures | No |
| Color emoji / variable axes | No |
| SDF/MSDF rasterization | No |

## Requirements

- .NET 8 (`net8.0`)
- C# 12
- MonoGame 3.8.6.1
- StbTrueTypeNext 1.0.3

Continue with [Getting started](getting-started.md).
