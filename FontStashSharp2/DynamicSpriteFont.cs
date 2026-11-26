using System.Text;
using FontStashSharp2.Internal;
#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FontStashSharp2;

/// <summary>
/// A font of a fixed pixel size whose glyphs are rasterized into a shared atlas on first use.
/// </summary>
/// <remarks>
/// Measurement never rasterizes glyphs. Glyphs enter the atlas only when text is drawn or preloaded.
/// Instances are cached by <see cref="FontSystem.GetFont(float)"/>.
/// </remarks>
public sealed class DynamicSpriteFont : SpriteFontBase
{
	private readonly FontSystem _fontSystem;
	private readonly GlyphCache _glyphCache;
	private readonly float _inverseSupersamplingFactor;
	private long _atlasGeneration;
	private float _lineSpacing;

	internal DynamicSpriteFont(FontSystem fontSystem, FontSource[] sources, float size)
	{
		ArgumentNullException.ThrowIfNull(fontSystem);
		ArgumentNullException.ThrowIfNull(sources);

		if (sources.Length == 0)
		{
			throw new ArgumentException("At least one font source is required.", nameof(sources));
		}

		_fontSystem = fontSystem;
		_atlasGeneration = fontSystem.AtlasGeneration;
		FontSize = size;

		var scales = new float[sources.Length];
		for (var i = 0; i < sources.Length; i++)
		{
			scales[i] = sources[i].GetScale(size);
		}

		var primaryScale = scales[0];
		Ascent = sources[0].Ascent * primaryScale;
		Descent = sources[0].Descent * primaryScale;
		LineGap = sources[0].LineGap * primaryScale;

		_lineSpacing = Ascent - Descent + LineGap;
		if (!float.IsFinite(_lineSpacing) || _lineSpacing <= 0f)
		{
			_lineSpacing = size * 1.2f;
		}

		SupersamplingFactor = fontSystem.SupersamplingFactor;
		_inverseSupersamplingFactor = 1f / SupersamplingFactor;
		_glyphCache = new GlyphCache(sources, scales, fontSystem.UseKerning, SupersamplingFactor);
	}

	/// <summary>
	/// Gets the font size in pixels (the em size).
	/// </summary>
	public override float FontSize { get; }

	/// <summary>
	/// Gets or sets the distance between consecutive baselines, in pixels.
	/// </summary>
	/// <remarks>
	/// Defaults to ascent - descent + line gap for the size. Changing it affects both drawing and
	/// measurement immediately.
	/// </remarks>
	public override float LineSpacing
	{
		get => _lineSpacing;
		set
		{
			if (!float.IsFinite(value) || value < 0f)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "LineSpacing must be finite and non-negative.");
			}

			_lineSpacing = value;
		}
	}

	/// <summary>
	/// Gets the resolution multiplier used to rasterize glyphs.
	/// </summary>
	/// <remarks>
	/// Glyph bitmaps are rasterized at <c>FontSize * SupersamplingFactor</c> and drawn scaled down by
	/// the inverse factor. A value of 1 disables supersampling.
	/// </remarks>
	public float SupersamplingFactor { get; }

	/// <summary>
	/// Gets the scaled ascent above the baseline, in pixels.
	/// </summary>
	public float Ascent { get; }

	/// <summary>
	/// Gets the scaled descent below the baseline, in pixels (negative).
	/// </summary>
	public float Descent { get; }

	/// <summary>
	/// Gets the scaled line gap, in pixels.
	/// </summary>
	public float LineGap { get; }

	/// <summary>
	/// Gets the number of glyphs this font has rasterized into the atlas.
	/// </summary>
	public int GlyphCount
	{
		get
		{
			EnsureAtlasGeneration();
			return _glyphCache.RasterizedGlyphCount;
		}
	}

	/// <summary>
	/// Gets the number of code points whose metrics have been resolved (including measurement-only use).
	/// </summary>
	public int CachedGlyphCount => _glyphCache.MetricsCount;

	/// <summary>
	/// Measures a string, including line breaks, kerning, and <see cref="LineSpacing"/>.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	/// <returns>The width of the widest line and the total line height.</returns>
	public override Vector2 MeasureString(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		return MeasureString(text.AsSpan());
	}

	/// <summary>
	/// Measures the contents of a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public override Vector2 MeasureString(StringBuilder text)
	{
		ArgumentNullException.ThrowIfNull(text);
		return MeasureString(text.ToString().AsSpan());
	}

	/// <summary>
	/// Measures a span of characters, including line breaks, kerning, and <see cref="LineSpacing"/>.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public override Vector2 MeasureString(ReadOnlySpan<char> text)
	{
		if (text.IsEmpty)
		{
			return Vector2.Zero;
		}

		var maxLineWidth = 0f;
		var lineWidth = 0f;
		var lineCount = 1;
		var hasPrevious = false;
		var previous = default(GlyphMetrics);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					if (lineWidth > maxLineWidth)
					{
						maxLineWidth = lineWidth;
					}

					lineWidth = 0f;
					lineCount++;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			var metrics = _glyphCache.GetMetrics(codepoint);
			if (hasPrevious)
			{
				lineWidth += _glyphCache.GetKerning(previous, metrics);
			}

			lineWidth += metrics.Advance;
			if (lineWidth > maxLineWidth)
			{
				maxLineWidth = lineWidth;
			}

			hasPrevious = true;
			previous = metrics;
		}

		return new Vector2(maxLineWidth, lineCount * LineSpacing);
	}

#if MONOGAME
	/// <summary>
	/// Rasterizes all glyphs in a string into the atlas ahead of time, avoiding a first-draw hitch.
	/// </summary>
	/// <param name="spriteBatch">Provides the graphics device used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(SpriteBatch spriteBatch, string text)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);
		ArgumentNullException.ThrowIfNull(text);
		return PreloadGlyphs(spriteBatch.GraphicsDevice, text.AsSpan());
	}

	/// <summary>
	/// Rasterizes all glyphs in a span into the atlas ahead of time, avoiding a first-draw hitch.
	/// </summary>
	/// <param name="spriteBatch">Provides the graphics device used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(SpriteBatch spriteBatch, ReadOnlySpan<char> text)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);
		return PreloadGlyphs(spriteBatch.GraphicsDevice, text);
	}
#endif

	/// <summary>
	/// Rasterizes all glyphs in a string through a custom renderer's device ahead of time.
	/// </summary>
	/// <param name="renderer">Provides the graphics device used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(IFontStashRenderer renderer, string text)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(text);
		return PreloadGlyphs(GetDevice(renderer), text.AsSpan());
	}

	/// <summary>
	/// Rasterizes all glyphs in a span through a custom renderer's device ahead of time.
	/// </summary>
	/// <param name="renderer">Provides the graphics device used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(IFontStashRenderer renderer, ReadOnlySpan<char> text)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		return PreloadGlyphs(GetDevice(renderer), text);
	}

	/// <summary>
	/// Rasterizes all glyphs in a string through a vertex renderer's device ahead of time.
	/// </summary>
	/// <param name="renderer">Provides the graphics device used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(IFontStashRenderer2 renderer, string text)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(text);
		return PreloadGlyphs(GetDevice(renderer), text.AsSpan());
	}

	/// <summary>
	/// Rasterizes all glyphs in a span through a vertex renderer's device ahead of time.
	/// </summary>
	/// <param name="renderer">Provides the graphics device used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(IFontStashRenderer2 renderer, ReadOnlySpan<char> text)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		return PreloadGlyphs(GetDevice(renderer), text);
	}

	/// <summary>
	/// Rasterizes all glyphs in a string into the atlas ahead of time, avoiding a first-draw hitch.
	/// </summary>
	/// <param name="device">The device or texture manager used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(AtlasDevice device, string text)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(text);
		return PreloadGlyphs(device, text.AsSpan());
	}

	/// <summary>
	/// Rasterizes all glyphs in a span into the atlas ahead of time, avoiding a first-draw hitch.
	/// </summary>
	/// <param name="device">The device or texture manager used to allocate/upload atlas pages.</param>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(AtlasDevice device, ReadOnlySpan<char> text)
	{
		ArgumentNullException.ThrowIfNull(device);

		if (text.IsEmpty)
		{
			return 0;
		}

		return PreloadGlyphs(_fontSystem.GetAtlas(device), device, text);
	}

	internal int PreloadGlyphs(IGlyphAtlas atlas, string text)
	{
		ArgumentNullException.ThrowIfNull(atlas);
		ArgumentNullException.ThrowIfNull(text);
		return PreloadGlyphs(atlas, null, text.AsSpan());
	}

	internal int PreloadGlyphs(IGlyphAtlas atlas, ReadOnlySpan<char> text)
	{
		return PreloadGlyphs(atlas, null, text);
	}

	internal int PreloadGlyphs(IGlyphAtlas atlas, AtlasDevice? device, ReadOnlySpan<char> text)
	{
		ArgumentNullException.ThrowIfNull(atlas);

		if (text.IsEmpty)
		{
			return 0;
		}

		EnsureAtlasGeneration();
		var before = _glyphCache.RasterizedGlyphCount;

		for (var i = 0; i < text.Length;)
		{
			var codepoint = ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
				case '\n':
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			_glyphCache.GetRenderedGlyph(codepoint, atlas, device);
		}

		return _glyphCache.RasterizedGlyphCount - before;
	}

#if MONOGAME
	/// <inheritdoc />
	public override void DrawText(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);

		if (text.IsEmpty)
		{
			return;
		}

		EnsureAtlasGeneration();

		if (rotation == 0f
			&& origin == Vector2.Zero
			&& scale == Vector2.One
			&& effects == SpriteEffects.None
			&& layerDepth == 0f)
		{
			DrawTextDefault(spriteBatch, text, position, color);
			return;
		}

		var device = spriteBatch.GraphicsDevice;
		var atlas = _fontSystem.GetAtlas(device);

		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically ? MeasureString(text) : Vector2.Zero;

		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = default(GlyphMetrics);
		Texture2D? texture = null;
		var textureIndex = -1;
		var drawScale = new Vector2(
			scale.X * _inverseSupersamplingFactor,
			scale.Y * _inverseSupersamplingFactor);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			var glyph = _glyphCache.TryGetRenderedGlyph(codepoint, out var cachedGlyph)
				? cachedGlyph
				: _glyphCache.GetRenderedGlyph(codepoint, atlas, device);

			if (hasPrevious)
			{
				penX += _glyphCache.GetKerning(previous, glyph.Metrics);
			}

			if (glyph.HasTexture)
			{
				var glyphX = penX + glyph.OffsetX;
				var glyphY = lineTop + Ascent + glyph.OffsetY;
				var glyphWidth = glyph.SourceRectangle.Width * _inverseSupersamplingFactor;
				var glyphHeight = glyph.SourceRectangle.Height * _inverseSupersamplingFactor;

				// SpriteEffects mirror the whole text block, not each glyph independently.
				if (flipHorizontally)
				{
					glyphX = blockSize.X - glyphX - glyphWidth;
				}

				if (flipVertically)
				{
					glyphY = blockSize.Y - glyphY - glyphHeight;
				}

				if (glyph.TextureIndex != textureIndex)
				{
					texture = _fontSystem.GetTextureOrThrow(glyph.TextureIndex);
					textureIndex = glyph.TextureIndex;
				}

				var localX = (glyphX - origin.X) * scale.X;
				var localY = (glyphY - origin.Y) * scale.Y;

				Vector2 screenPosition;
				if (rotation == 0f)
				{
					screenPosition = new Vector2(position.X + localX, position.Y + localY);
				}
				else
				{
					var cos = MathF.Cos(rotation);
					var sin = MathF.Sin(rotation);
					screenPosition = new Vector2(
						position.X + (localX * cos) - (localY * sin),
						position.Y + (localX * sin) + (localY * cos));
				}

				spriteBatch.Draw(
					texture!,
					screenPosition,
					glyph.SourceRectangle,
					color,
					rotation,
					Vector2.Zero,
					drawScale,
					effects,
					layerDepth);
			}

			penX += glyph.Metrics.Advance;
			hasPrevious = true;
			previous = glyph.Metrics;
		}
	}

#endif

	/// <inheritdoc />
	public override void DrawText(
		IFontStashRenderer renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
		ArgumentNullException.ThrowIfNull(renderer);

		if (text.IsEmpty)
		{
			return;
		}

		EnsureAtlasGeneration();

		var device = GetDevice(renderer);
		var atlas = _fontSystem.GetAtlas(device);

		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically ? MeasureString(text) : Vector2.Zero;

		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = default(GlyphMetrics);
		AtlasTexture? texture = null;
		var textureIndex = -1;
		var drawScale = new Vector2(
			scale.X * _inverseSupersamplingFactor,
			scale.Y * _inverseSupersamplingFactor);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			var glyph = _glyphCache.TryGetRenderedGlyph(codepoint, out var cachedGlyph)
				? cachedGlyph
				: _glyphCache.GetRenderedGlyph(codepoint, atlas, device);

			if (hasPrevious)
			{
				penX += _glyphCache.GetKerning(previous, glyph.Metrics);
			}

			if (glyph.HasTexture)
			{
				var glyphX = penX + glyph.OffsetX;
				var glyphY = lineTop + Ascent + glyph.OffsetY;
				var glyphWidth = glyph.SourceRectangle.Width * _inverseSupersamplingFactor;
				var glyphHeight = glyph.SourceRectangle.Height * _inverseSupersamplingFactor;

				if (flipHorizontally)
				{
					glyphX = blockSize.X - glyphX - glyphWidth;
				}

				if (flipVertically)
				{
					glyphY = blockSize.Y - glyphY - glyphHeight;
				}

				if (glyph.TextureIndex != textureIndex)
				{
					texture = _fontSystem.GetTextureOrThrow(glyph.TextureIndex);
					textureIndex = glyph.TextureIndex;
				}

				var localX = (glyphX - origin.X) * scale.X;
				var localY = (glyphY - origin.Y) * scale.Y;

				Vector2 screenPosition;
				if (rotation == 0f)
				{
					screenPosition = new Vector2(position.X + localX, position.Y + localY);
				}
				else
				{
					var cos = MathF.Cos(rotation);
					var sin = MathF.Sin(rotation);
					screenPosition = new Vector2(
						position.X + (localX * cos) - (localY * sin),
						position.Y + (localX * sin) + (localY * cos));
				}

				DrawGlyph(
					renderer,
					texture!,
					screenPosition,
					glyph.SourceRectangle,
					color,
					rotation,
					drawScale,
					effects,
					layerDepth);
			}

			penX += glyph.Metrics.Advance;
			hasPrevious = true;
			previous = glyph.Metrics;
		}
	}

	/// <inheritdoc />
	public override void DrawText(
		IFontStashRenderer2 renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
		ArgumentNullException.ThrowIfNull(renderer);

		if (text.IsEmpty)
		{
			return;
		}

		EnsureAtlasGeneration();

		var device = GetDevice(renderer);
		var atlas = _fontSystem.GetAtlas(device);

		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically ? MeasureString(text) : Vector2.Zero;

		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = default(GlyphMetrics);
		AtlasTexture? texture = null;
		var textureIndex = -1;
		var textureWidth = 0;
		var textureHeight = 0;
		var drawScale = new Vector2(
			scale.X * _inverseSupersamplingFactor,
			scale.Y * _inverseSupersamplingFactor);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			var glyph = _glyphCache.TryGetRenderedGlyph(codepoint, out var cachedGlyph)
				? cachedGlyph
				: _glyphCache.GetRenderedGlyph(codepoint, atlas, device);

			if (hasPrevious)
			{
				penX += _glyphCache.GetKerning(previous, glyph.Metrics);
			}

			if (glyph.HasTexture)
			{
				var glyphX = penX + glyph.OffsetX;
				var glyphY = lineTop + Ascent + glyph.OffsetY;
				var glyphWidth = glyph.SourceRectangle.Width * _inverseSupersamplingFactor;
				var glyphHeight = glyph.SourceRectangle.Height * _inverseSupersamplingFactor;

				if (flipHorizontally)
				{
					glyphX = blockSize.X - glyphX - glyphWidth;
				}

				if (flipVertically)
				{
					glyphY = blockSize.Y - glyphY - glyphHeight;
				}

				if (glyph.TextureIndex != textureIndex)
				{
					texture = _fontSystem.GetTextureOrThrow(glyph.TextureIndex);
					var textureSize = GetTextureSize(renderer, texture!);
					textureWidth = textureSize.X;
					textureHeight = textureSize.Y;
					textureIndex = glyph.TextureIndex;
				}

				var localX = (glyphX - origin.X) * scale.X;
				var localY = (glyphY - origin.Y) * scale.Y;

				Vector2 screenPosition;
				if (rotation == 0f)
				{
					screenPosition = new Vector2(position.X + localX, position.Y + localY);
				}
				else
				{
					var rotationCos = MathF.Cos(rotation);
					var rotationSin = MathF.Sin(rotation);
					screenPosition = new Vector2(
						position.X + (localX * rotationCos) - (localY * rotationSin),
						position.Y + (localX * rotationSin) + (localY * rotationCos));
				}

				var cos = rotation == 0f ? 1f : MathF.Cos(rotation);
				var sin = rotation == 0f ? 0f : MathF.Sin(rotation);
				var quadWidth = glyph.SourceRectangle.Width * drawScale.X;
				var quadHeight = glyph.SourceRectangle.Height * drawScale.Y;
				var right = new Vector2(quadWidth * cos, quadWidth * sin);
				var down = new Vector2(-quadHeight * sin, quadHeight * cos);

				var source = glyph.SourceRectangle;
				var u0 = source.X / (float)textureWidth;
				var v0 = source.Y / (float)textureHeight;
				var u1 = (source.X + source.Width) / (float)textureWidth;
				var v1 = (source.Y + source.Height) / (float)textureHeight;

				if (flipHorizontally)
				{
					(u0, u1) = (u1, u0);
				}

				if (flipVertically)
				{
					(v0, v1) = (v1, v0);
				}

				var topLeft = new VertexPositionColorTexture(
					new Vector3(screenPosition.X, screenPosition.Y, layerDepth),
					color,
					new Vector2(u0, v0));
				var topRight = new VertexPositionColorTexture(
					new Vector3(screenPosition.X + right.X, screenPosition.Y + right.Y, layerDepth),
					color,
					new Vector2(u1, v0));
				var bottomLeft = new VertexPositionColorTexture(
					new Vector3(screenPosition.X + down.X, screenPosition.Y + down.Y, layerDepth),
					color,
					new Vector2(u0, v1));
				var bottomRight = new VertexPositionColorTexture(
					new Vector3(screenPosition.X + right.X + down.X, screenPosition.Y + right.Y + down.Y, layerDepth),
					color,
					new Vector2(u1, v1));

				renderer.DrawQuad(texture!, ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
			}

			penX += glyph.Metrics.Advance;
			hasPrevious = true;
			previous = glyph.Metrics;
		}
	}

#if MONOGAME
	private void DrawTextDefault(SpriteBatch spriteBatch, ReadOnlySpan<char> text, Vector2 position, Color color)
	{
		var device = spriteBatch.GraphicsDevice;
		var atlas = _fontSystem.GetAtlas(device);
		var baselineY = position.Y + Ascent;
		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = default(GlyphMetrics);
		Texture2D? texture = null;
		var textureIndex = -1;
		var needsScaling = SupersamplingFactor != 1f;
		var inverseScale = new Vector2(_inverseSupersamplingFactor);

		for (var i = 0; i < text.Length;)
		{
			var current = text[i];
			int codepoint;
			if (current < 128)
			{
				codepoint = current;
				i++;
			}
			else
			{
				codepoint = ReadCodepoint(text, ref i);
			}

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			var glyph = _glyphCache.TryGetRenderedGlyph(codepoint, out var cachedGlyph)
				? cachedGlyph
				: _glyphCache.GetRenderedGlyph(codepoint, atlas, device);

			if (hasPrevious)
			{
				penX += _glyphCache.GetKerning(previous, glyph.Metrics);
			}

			if (glyph.HasTexture)
			{
				if (glyph.TextureIndex != textureIndex)
				{
					texture = _fontSystem.GetTextureOrThrow(glyph.TextureIndex);
					textureIndex = glyph.TextureIndex;
				}

				var glyphPosition = new Vector2(
					position.X + penX + glyph.OffsetX,
					baselineY + lineTop + glyph.OffsetY);

				if (needsScaling)
				{
					spriteBatch.Draw(
						texture!,
						glyphPosition,
						glyph.SourceRectangle,
						color,
						0f,
						Vector2.Zero,
						inverseScale,
						SpriteEffects.None,
						0f);
				}
				else
				{
					spriteBatch.Draw(texture!, glyphPosition, glyph.SourceRectangle, color);
				}
			}

			penX += glyph.Metrics.Advance;
			hasPrevious = true;
			previous = glyph.Metrics;
		}
	}

#endif

	internal void ClearRenderedGlyphs()
	{
		_glyphCache.ClearRenderedGlyphs();
		_atlasGeneration = _fontSystem.AtlasGeneration;
	}

	private void EnsureAtlasGeneration()
	{
		if (_atlasGeneration != _fontSystem.AtlasGeneration)
		{
			ClearRenderedGlyphs();
		}
	}

	private static AtlasDevice GetDevice(IFontStashRenderer renderer)
	{
#if MONOGAME
		return renderer.GraphicsDevice;
#else
		return renderer.TextureManager;
#endif
	}

	private static AtlasDevice GetDevice(IFontStashRenderer2 renderer)
	{
#if MONOGAME
		return renderer.GraphicsDevice;
#else
		return renderer.TextureManager;
#endif
	}

	private static Point GetTextureSize(IFontStashRenderer2 renderer, AtlasTexture texture)
	{
#if MONOGAME
		return new Point(texture.Width, texture.Height);
#else
		return renderer.TextureManager.GetTextureSize(texture);
#endif
	}

	private static void DrawGlyph(
		IFontStashRenderer renderer,
		AtlasTexture texture,
		Vector2 position,
		Rectangle? sourceRectangle,
		Color color,
		float rotation,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
#if MONOGAME
		renderer.Draw(texture, position, sourceRectangle, color, rotation, scale, effects, layerDepth);
#else
		renderer.DrawText(texture, position, sourceRectangle, color, rotation, scale, effects, layerDepth);
#endif
	}

	private static int ReadCodepoint(ReadOnlySpan<char> text, ref int index)
	{
		var current = text[index];

		if (!char.IsSurrogate(current))
		{
			index++;
			return current;
		}

		if (char.IsHighSurrogate(current)
			&& index + 1 < text.Length
			&& char.IsLowSurrogate(text[index + 1]))
		{
			var codepoint = char.ConvertToUtf32(current, text[index + 1]);
			index += 2;
			return codepoint;
		}

		// A lone surrogate cannot be encoded; use U+FFFD so the font's fallback/notdef path handles it.
		index++;
		return 0xFFFD;
	}
}
