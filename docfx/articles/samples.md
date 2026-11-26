# Samples

The repository contains eight samples. The MonoGame windowed samples also support screenshot modes that
double as integration tests; the platform-agnostic samples accept `--frames <n>` for bounded smoke
tests.

## HelloWorld (DesktopGL)

A windowed MonoGame sample showing:

- one `FontSystem` shared by many font sizes,
- `MeasureString`-based right alignment and centered rotation,
- 2× supersampled glyphs for sharper scaled text,
- uniform scaling and horizontal flipping,
- live atlas page, rasterized-glyph, and supersampling counts,
- on-demand glyph caching during draw.

![Hello World sample](../images/hello-world.png)

```bash
dotnet run --project Samples/HelloWorld
```

Press `Esc` to exit.

### Screenshot mode

Render a deterministic 1280x720 frame to a PNG and exit:

```bash
dotnet run --project Samples/HelloWorld -- --screenshot out.png
```

This exercises the full path on a real GPU: font parsing, glyph rasterization, atlas upload, padding,
layout, rotation, scaling, flipping, and `SpriteBatch` drawing. It is useful for visual regression
checks on a machine with a display.

## Supersampling (DesktopGL)

A dedicated comparison of glyph-atlas resolutions:

- rows: 1×, 2× (default), and 4× supersampling;
- columns: 1.0×, 1.5×, 2.0×, and 3.0× draw scale;
- the same 24 px font, text, and colors in every cell;
- live atlas page and glyph counts per factor.

![Supersampling comparison](../images/supersampling.png)

```bash
dotnet run --project Samples/Supersampling
dotnet run --project Samples/Supersampling -- --screenshot supersampling.png
```

The 1× row shows magnification blur at 3× display scale. The 4× row stays sharp at 3× because it is
still being downsampled. The 2× row is the default tradeoff. See the
[Supersampling article](supersampling.md) for the full explanation.

## Custom renderer (DesktopGL)

A dedicated sample for both custom renderer interfaces. Every line, including the chrome, is drawn
through `font.DrawText(renderer, ...)`, not the `SpriteBatch` extension methods:

- the left column uses `IFontStashRenderer` with a `SpriteBatch`-backed renderer that adds a drop
  shadow;
- the right column uses `IFontStashRenderer2` with a `BasicEffect` vertex renderer and
  `DrawUserIndexedPrimitives`.

![Custom renderer sample](../images/custom-renderer.png)

```bash
dotnet run --project Samples/CustomRenderer
dotnet run --project Samples/CustomRenderer -- --screenshot custom-renderer.png
```

The sample exercises the `string`, `StringBuilder`, `ReadOnlySpan<char>`, full transform (rotation,
scale, origin), and `SpriteEffects` overloads on both interfaces. The footer shows glyph-quad and
vertex-quad counts from the previous frame. See [Custom renderers](custom-renderers.md) for both
interface contracts and the renderer implementation guide.

## Text effects (DesktopGL)

A dedicated `SpriteBatch` sample for the optional draw effect arguments:

- `TextEffect.None`, `TextEffect.Stroke`, and `TextEffect.Shadow`;
- different `effectAmount` and `effectColor` values;
- effects combined with rotation, scaling, and horizontal flipping;
- every line goes through `SpriteBatch.DrawString`, so it exercises the MonoGame draw path.

![Text effects sample](../images/text-effects.png)

```bash
dotnet run --project Samples/TextEffects
dotnet run --project Samples/TextEffects -- --screenshot text-effects.png
```

Press `Esc` to exit. See [Text effects](../index.md#text-effects) in the Getting Started guide for the
API.

## OpenTK (platform-agnostic)

A minimal OpenGL 3.3 text renderer built on `FontStashSharp2.PlatformAgnostic` and OpenTK:

- implements `ITexture2DManager` with OpenGL textures (`TexImage2D`/`TexSubImage2D`);
- implements `IFontStashRenderer` with a shader, a unit-quad VBO, and one draw per glyph;
- loads the TTF at runtime, draws rotated/scaled text and `SpriteEffects` flips;
- demonstrates `TextEffect.Stroke` and `TextEffect.Shadow`;
- sets `AlphaMode = GlyphAlphaMode.NonPremultiplied` and uses standard
  `SrcAlpha`/`OneMinusSrcAlpha` blending.

```bash
dotnet run --project Samples/OpenTK
dotnet run --project Samples/OpenTK -- --frames 3
```

Press `Esc` to exit. See [Platform-agnostic rendering](platform-agnostic.md) for the interface contracts.

## Silk.NET (platform-agnostic)

The same renderer shape with Silk.NET windowing and OpenGL bindings:

- `SilkGlTextureManager` creates and updates OpenGL textures;
- `SilkGlTextRenderer` implements `IFontStashRenderer` with a GLSL 330 shader;
- shares the same font/atlas pipeline with the OpenTK and MonoGame builds.

```bash
dotnet run --project Samples/SilkNet
dotnet run --project Samples/SilkNet -- --frames 3
```

Close the window to exit. See [Platform-agnostic rendering](platform-agnostic.md) for the interface
contracts.

## MeasureText (console)

A measurement-only sample that never creates a `GraphicsDevice`:

```bash
dotnet run --project Samples/MeasureText
dotnet run --project Samples/MeasureText -- 48 "Custom text"
```

It prints:

- font name and vertical metrics in font units,
- scaled ascent/descent/line spacing,
- widths and heights for single-line, multi-line, trailing-newline, kerning, and missing-glyph text,
- a centered placement calculation for a 640x360 view,
- cached metric count and the fact that zero glyphs have been rasterized.

This sample is a convenient smoke test for font loading and layout in CI environments without a GPU.

## Benchmark (DesktopGL)

`Samples/Benchmark` compares steady-state `SpriteBatch.DrawString` throughput between
FontStashSharp2 and a regular MonoGame `SpriteFont`. The SpriteFont is built at runtime from the same
TTF and pixel size, so both renderers start with a pre-baked atlas. FontStashSharp2 uses
`SupersamplingFactor = 1` here so both paths draw 1:1 atlases; the HelloWorld sample demonstrates the
default 2× mode. Warmup frames exclude first-use glyph rasterization and JIT. The sample requires a
graphics device/display.

```bash
dotnet run --project Samples/Benchmark
dotnet run --project Samples/Benchmark -- --benchmark
dotnet run --project Samples/Benchmark -- --benchmark --texts 256 --font-size 24 --iterations 200
dotnet run --project Samples/Benchmark -- --screenshot benchmark.png
```

The sample measures two targets:

- **CPU 64×64** — isolates `SpriteBatch` submission/geometry cost by clipping the resulting quads.
- **Full 1280×720** — includes GPU fill and overdraw.

It reports milliseconds per frame, strings/glyph throughput, managed allocations, and a
`SpriteFont / FontStashSharp2` ratio (greater than 1 means FontStashSharp2 is faster).

Useful options:

| Option | Meaning |
| --- | --- |
| `--rounds <n>` | Measured rounds per mode/target. |
| `--iterations <n>` | Frames per measured phase. |
| `--texts <n>` | DrawString calls per frame. |
| `--font-size <n>` | Font size in pixels. |
| `--warmup <n>` | Warmup frames per phase. |
| `--no-kerning` | Disable FontStashSharp2 kerning pairs for diagnostics. |
| `--screenshot <path>` | Save the results screen and exit. |

Numbers vary by CPU/GPU and are intended for comparisons on the same machine. The interactive
results screen includes a visual check drawn by both engines.

## Tests

```bash
dotnet test FontStashSharp2.Tests/FontStashSharp2.Tests.csproj
```

The xUnit suite uses a fake glyph atlas, so it runs without a graphics device. It covers parsing,
fallback order, ASCII coverage, advances, kerning, newlines, tabs, surrogate handling, line spacing,
on-demand caching, preloading, packer edge cases, settings validation, and disposal.
