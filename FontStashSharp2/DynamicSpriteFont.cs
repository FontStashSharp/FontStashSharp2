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
			scales[i] = sources[i].GetScale(size, fontSystem.ScaleMode);
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
	/// Gets the font size in pixels. The mapping to font units depends on
	/// <see cref="FontSystemSettings.ScaleMode"/>.
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
	public override float Ascent { get; }

	/// <summary>
	/// Gets the scaled descent below the baseline, in pixels (negative).
	/// </summary>
	public override float Descent { get; }

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
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

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

	/// <inheritdoc />
	protected override float SupersamplingScale => _inverseSupersamplingFactor;

	/// <inheritdoc />
	protected override void PrepareForDraw()
	{
		EnsureAtlasGeneration();
	}

	/// <inheritdoc />
	protected override float GetAdvance(int codepoint)
	{
		return _glyphCache.GetMetrics(codepoint).Advance;
	}

	/// <inheritdoc />
	protected override float GetKerning(int left, int right)
	{
		return _glyphCache.GetKerning(
			_glyphCache.GetMetrics(left),
			_glyphCache.GetMetrics(right));
	}

	/// <inheritdoc />
	protected override bool TryGetRenderedGlyph(
		int codepoint,
		AtlasDevice device,
		out SpriteGlyph glyph)
	{
		if (!_glyphCache.TryGetRenderedGlyph(codepoint, out var rendered))
		{
			var atlas = _fontSystem.GetAtlas(device);
			rendered = _glyphCache.GetRenderedGlyph(codepoint, atlas, device);
		}

		glyph = new SpriteGlyph(
			rendered.TextureIndex,
			rendered.SourceRectangle,
			rendered.OffsetX,
			rendered.OffsetY,
			rendered.Metrics.Advance);
		return true;
	}

	/// <inheritdoc />
	protected override AtlasTexture GetTexture(AtlasDevice device, int textureIndex)
	{
		return _fontSystem.GetTextureOrThrow(textureIndex);
	}

	/// <inheritdoc />
	protected override AtlasTexture GetWhiteTexture(AtlasDevice device)
	{
		return _fontSystem.GetWhiteTexture(device);
	}
}
