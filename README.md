# FontStashSharp2

**On-demand TrueType text rendering for MonoGame, drawn through `SpriteBatch`.**

![Hello World sample](docfx/images/hello-world.png)

## Overview

FontStashSharp2 parses TTF/OTF fonts with StbTrueTypeNext and rasterizes glyphs into a shared texture
atlas only when they are first needed. Text is drawn with `SpriteBatch` extension methods that mirror
the `SpriteFont` API, with configurable supersampling, kerning, font fallback, and custom renderer
support.

## Why another text library?

- **No content pipeline.** Ship `.ttf`/`.otf` files and load them at runtime.
- **On demand.** Measuring text does not touch the GPU; only glyphs that are drawn are rasterized and
  uploaded.
- **Any size, one atlas.** Different font sizes and fallback fonts share one growable atlas.
- **SpriteBatch native.** The familiar `SpriteFont` parameter list, including rotation, origin, scale,
  effects, and layer depth.
- **Sharper scaling.** Glyphs are supersampled 2× by default and drawn at the inverse scale, with a
  configurable factor for heavier zoom.

## Documentation

https://fontstashsharp.github.io/FontStashSharp2

## License

FontStashSharp2 is MIT licensed. The sample/test font **ABeeZee** is licensed under the SIL Open Font
License 1.1; see `THIRD-PARTY-NOTICES.md`.
