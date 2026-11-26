# Platform-agnostic rendering

`FontStashSharp2.PlatformAgnostic` is the engine-neutral build of the library. It has no MonoGame
dependency: there is no `SpriteBatch`, `Texture2D`, or `GraphicsDevice`. Instead, the engine supplies a
texture manager and implements one of the renderer interfaces.

Use this package when integrating FontStashSharp2 with a custom engine, FNA, Stride, raylib, or any
other C# renderer. The MonoGame package remains the right choice for MonoGame projects because it adds
the `SpriteBatch.DrawString` extension methods.

Both packages use the `FontStashSharp2` namespace and type names. Reference one or the other in a
project, not both.

## Texture management

Implement `ITexture2DManager` once per engine. Atlas textures are passed around as `object`, so the
manager can use the engine's native texture type internally.

```csharp
using FontStashSharp2;

public sealed class MyTextureManager : ITexture2DManager
{
    public object CreateTexture(int width, int height)
    {
        return new MyTexture(width, height);
    }

    public Point GetTextureSize(object texture)
    {
        var myTexture = (MyTexture)texture;
        return new Point(myTexture.Width, myTexture.Height);
    }

    public void SetTextureData(object texture, Rectangle bounds, ReadOnlySpan<byte> data)
    {
        var myTexture = (MyTexture)texture;

        // data contains bounds.Width * bounds.Height * 4 RGBA bytes in row-major order.
        myTexture.Upload(bounds, data);
    }

    public void DestroyTexture(object texture)
    {
        ((MyTexture)texture).Dispose();
    }
}
```

The atlas uses RGBA bytes. `PremultiplyAlpha` defaults to `true`, so RGB already equals alpha; use a
premultiplied alpha blend state, or set `PremultiplyAlpha = false` and upload straight alpha. The
`bounds` argument identifies the dirty region, so the engine can update a sub-rectangle instead of
re-uploading the whole page.

`FontSystem.Dispose` calls `DestroyTexture` for every atlas page it created.

## Draw through a renderer

Implement `IFontStashRenderer` to receive one call per visible glyph. The font has already resolved
layout, rotation, scaling, sprite effects, and layer depth.

```csharp
using System.Numerics;
using FontStashSharp2;

public sealed class MyTextRenderer : IFontStashRenderer
{
    public MyTextRenderer(MyTextureManager textureManager)
    {
        TextureManager = textureManager;
    }

    public ITexture2DManager TextureManager { get; }

    public void DrawText(
        object texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        // Draw sourceRectangle from texture at position.
        // Rotate/scale around the top-left corner, mirror according to effects,
        // and submit with the requested color and layer depth.
    }
}
```

The vertex-level interface is available when the engine prefers to build quads itself:

```csharp
public interface IFontStashRenderer2
{
    ITexture2DManager TextureManager { get; }

    void DrawQuad(
        object texture,
        ref VertexPositionColorTexture topLeft,
        ref VertexPositionColorTexture topRight,
        ref VertexPositionColorTexture bottomLeft,
        ref VertexPositionColorTexture bottomRight);
}
```

`VertexPositionColorTexture.Position` is in screen space, Z is the layer depth, `TextureCoordinate` is
normalized, and the color is the requested mask. Submit the four vertices as two triangles.

## Load, measure, and draw

```csharp
using System.Numerics;
using FontStashSharp2;

var textureManager = new MyTextureManager();
var renderer = new MyTextRenderer(textureManager);

using var fonts = new FontSystem(new FontSystemSettings
{
    TextureWidth = 1024,
    TextureHeight = 1024,
    SupersamplingFactor = 2f,
});

fonts.AddFont(File.ReadAllBytes("Content/MyFont.ttf"));
var font = fonts.GetFont(32f);

Vector2 size = font.MeasureString("Hello, engine!");
font.DrawText(renderer, "Hello, engine!", new Vector2(40f, 40f), Color.White);
```

The platform-agnostic build intentionally has no draw methods that take a `SpriteBatch` or graphics
device. Its `SpriteFontBase.DrawText` overloads accept `IFontStashRenderer` or `IFontStashRenderer2`.
The usual `string`, `StringBuilder`, rotation, origin, scale, effects, and layer-depth overloads are
available for both renderer interfaces.

## Preloading and device lifetime

`MeasureString` never creates a texture. Glyphs are uploaded on the first draw or when preloading:

```csharp
int added = font.PreloadGlyphs(textureManager, "Score: 0000  Lives: 3");
```

Use one `FontSystem` per `ITexture2DManager`. A second manager throws instead of silently mixing atlas
pages. The atlas is created lazily, so measurement works before any engine texture exists.

Complete OpenGL implementations are available in [Samples](samples.md): `Samples/OpenTK` uses OpenTK
and `Samples/SilkNet` uses Silk.NET. Both implement `ITexture2DManager` and `IFontStashRenderer` and
render the same runtime-loaded font without MonoGame.

## AOT and trimming

Both class libraries set `IsAotCompatible`. The font pipeline uses no reflection, runtime code
generation, or dynamic assembly loading, so it can be linked into Native AOT and trimmed applications.
The samples are ordinary executable projects and are intentionally not marked AOT-compatible.

## Repository build

The two builds share the same sources. The MonoGame project defines `MONOGAME`; the platform-agnostic
project defines `PLATFORM_AGNOSTIC`. Divergent APIs and types are selected with `#if` in the shared
files, which keeps glyph rasterization, measurement, packing, and caching identical between builds.
