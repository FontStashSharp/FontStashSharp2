# FontStashSharp2

**On-demand TrueType text rendering for MonoGame and other C# game engines.**

![Hello World sample](docfx/images/hello-world.png)

## Overview

FontStashSharp2 parses TTF/OTF fonts with StbTrueTypeNext and rasterizes glyphs into a shared texture
atlas only when they are first needed. The MonoGame package draws with `SpriteBatch` extension methods
that mirror the `SpriteFont` API. The `FontStashSharp2.PlatformAgnostic` package uses renderer
interfaces and an engine-supplied `ITexture2DManager`, so it works without any MonoGame dependency.

`StaticSpriteFont` loads pre-generated AngelCode BMFont atlases through SharpFNT and decodes their page
images with StbImageNext. Static and dynamic fonts share the same draw overloads, effects,
decorations, spacing, and renderer interfaces.

## Why another text library?

- **No content pipeline.** Ship `.ttf`/`.otf` files and load them at runtime, or load AngelCode
  BMFont atlases with `StaticSpriteFont`.
- **On demand.** Measuring text does not touch the GPU; only glyphs that are drawn are rasterized and
  uploaded.
- **Any size, one atlas.** Different font sizes and fallback fonts share one growable atlas.
- **SpriteBatch native.** The familiar `SpriteFont` parameter list, including rotation, origin, scale,
  effects, and layer depth.
- **Engine neutral.** `FontStashSharp2.PlatformAgnostic` exposes renderer interfaces and RGBA texture
  uploads instead of MonoGame types.
- **AOT friendly.** Both class libraries are annotated `IsAotCompatible` and avoid reflection and
  runtime code generation.
- **Sharper scaling.** Glyphs are supersampled 2× by default and drawn at the inverse scale, with a
  configurable factor for heavier zoom.

## Code metrics

Counts exclude `bin/`, `obj/`, generated docfx output, and binary assets. The shared library is
compiled by both packages and is counted once. Counts were measured on 2026-11-27. `Code lines`
excludes blank and comment-only lines but includes braces, directives, and `using` lines.

| Area | Files | Physical lines | Non-blank lines | Code lines | Size |
| --- | ---: | ---: | ---: | ---: | ---: |
| Shared library (`FontStashSharp2/`) | 25 | 5,715 | 5,048 | 3,958 | 159.1 KB |
| MonoGame tests | 11 | 1,299 | 1,066 | 1,063 | 36.4 KB |
| Platform-agnostic tests | 4 | 1,302 | 1,075 | 1,074 | 34.1 KB |
| Samples | 23 | 3,896 | 3,305 | 3,262 | 102.2 KB |
| **Total C#** | **63** | **12,212** | **10,494** | **9,357** | **331.8 KB** |
| docfx Markdown | 8 | 1,586 | 1,195 | n/a | 59.5 KB |

## Documentation

https://fontstashsharp.github.io/FontStashSharp2

## License

FontStashSharp2 is MIT licensed. The sample/test font **ABeeZee** is licensed under the SIL Open Font
License 1.1; see `THIRD-PARTY-NOTICES.md`.
