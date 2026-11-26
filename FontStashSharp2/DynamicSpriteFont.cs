using System.Text;
using FontStashSharp2.Internal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FontStashSharp2;

/// <summary>
/// A font of a fixed pixel size whose glyphs are rasterized into a shared atlas on first use.
/// </summary>
/// <remarks>
/// Measurement never rasterizes glyphs. Glyphs enter the atlas only when the text is drawn (or when
/// <see cref="PreloadGlyphs(string)"/> is called). Instances are cached by <see cref="FontSystem.GetFont(float)"/>.
/// </remarks>
public sealed class DynamicSpriteFont : SpriteFontBase
{
	private readonly FontSystem _fontSystem;
	private readonly GlyphCache _glyphCache;
	private readonly float _inverseSupersamplingFactor;
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
	public int GlyphCount => _glyphCache.RasterizedGlyphCount;

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

	/// <summary>
	/// Rasterizes all glyphs in a string into the atlas ahead of time, avoiding a first-draw hitch.
	/// </summary>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	/// <exception cref="InvalidOperationException">
	/// The owning <see cref="FontSystem"/> has no graphics device.
	/// </exception>
	public int PreloadGlyphs(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		return PreloadGlyphs(text.AsSpan());
	}

	/// <summary>
	/// Rasterizes all glyphs in a span into the atlas ahead of time, avoiding a first-draw hitch.
	/// </summary>
	/// <param name="text">The text whose glyphs should be cached.</param>
	/// <returns>The number of newly rasterized glyphs.</returns>
	public int PreloadGlyphs(ReadOnlySpan<char> text)
	{
		if (text.IsEmpty)
		{
			return 0;
		}

		var atlas = _fontSystem.GetAtlasOrThrow();
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

			_glyphCache.GetRenderedGlyph(codepoint, atlas);
		}

		return _glyphCache.RasterizedGlyphCount - before;
	}

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
		if (text.IsEmpty)
		{
			return;
		}

		if (rotation == 0f
			&& origin == Vector2.Zero
			&& scale == Vector2.One
			&& effects == SpriteEffects.None
			&& layerDepth == 0f)
		{
			DrawTextDefault(spriteBatch, text, position, color);
			return;
		}

		var atlas = _fontSystem.GetAtlasOrThrow();

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
				: _glyphCache.GetRenderedGlyph(codepoint, atlas);

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

	private void DrawTextDefault(SpriteBatch spriteBatch, ReadOnlySpan<char> text, Vector2 position, Color color)
	{
		var atlas = _fontSystem.GetAtlasOrThrow();
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
				: _glyphCache.GetRenderedGlyph(codepoint, atlas);

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
