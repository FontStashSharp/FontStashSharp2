namespace FontStashSharp2;

/// <summary>
/// Configures a <see cref="FontSystem"/>: the size of its texture atlas pages, how glyphs are
/// rasterized, and how glyphs are packed.
/// </summary>
/// <remarks>
/// The settings are copied by <see cref="FontSystem"/> when it is constructed, so changing this
/// object afterwards has no effect on an existing <see cref="FontSystem"/>.
/// </remarks>
public sealed class FontSystemSettings
{
	private int _textureWidth = 1024;
	private int _textureHeight = 1024;
	private int _maxTextureCount = 8;
	private int _glyphPadding = 1;
	private float _supersamplingFactor = 2f;

	/// <summary>
	/// Gets or sets the width, in pixels, of a single atlas page. Defaults to 1024.
	/// </summary>
	public int TextureWidth
	{
		get => _textureWidth;
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "Texture width must be positive.");
			}

			_textureWidth = value;
		}
	}

	/// <summary>
	/// Gets or sets the height, in pixels, of a single atlas page. Defaults to 1024.
	/// </summary>
	public int TextureHeight
	{
		get => _textureHeight;
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "Texture height must be positive.");
			}

			_textureHeight = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum number of atlas pages that can be allocated. Defaults to 8.
	/// </summary>
	/// <remarks>
	/// Pages are created lazily. When the limit is reached, adding another glyph throws an
	/// <see cref="InvalidOperationException"/> instead of growing the atlas without bound.
	/// </remarks>
	public int MaxTextureCount
	{
		get => _maxTextureCount;
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "Max texture count must be positive.");
			}

			_maxTextureCount = value;
		}
	}

	/// <summary>
	/// Gets or sets the number of transparent pixels placed around every glyph in the atlas.
	/// Defaults to 1.
	/// </summary>
	/// <remarks>
	/// Padding is filled by extending the glyph's edge pixels, which prevents dark seams when a
	/// glyph is sampled with a filtering sampler state.
	/// </remarks>
	public int GlyphPadding
	{
		get => _glyphPadding;
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "Glyph padding cannot be negative.");
			}

			_glyphPadding = value;
		}
	}

	/// <summary>
	/// Gets or sets the resolution multiplier used when rasterizing glyphs. Defaults to 2.
	/// </summary>
	/// <remarks>
	/// A glyph is rasterized at <c>FontSize * SupersamplingFactor</c> and drawn at the inverse scale.
	/// This keeps scaled text sharp while preserving the nominal font metrics. Values below 1 are
	/// rejected; use 1 to disable supersampling. Atlas memory grows roughly with the square of this
	/// factor, so increase it only when text is drawn at a larger scale.
	/// </remarks>
	public float SupersamplingFactor
	{
		get => _supersamplingFactor;
		set
		{
			if (!float.IsFinite(value) || value < 1f)
			{
				throw new ArgumentOutOfRangeException(
					nameof(value),
					value,
					"Supersampling factor must be finite and at least 1.0.");
			}

			_supersamplingFactor = value;
		}
	}

	/// <summary>
	/// Gets or sets whether kerning pairs from the font are applied. Defaults to <see langword="true"/>.
	/// </summary>
	public bool UseKerning { get; set; } = true;

	/// <summary>
	/// Gets or sets whether glyph alpha is premultiplied into RGB. Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// The default <see cref="Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend"/> mode expects
	/// premultiplied textures. Set this to <see langword="false"/> only when drawing with a
	/// non-premultiplied blend state.
	/// </remarks>
	public bool PremultiplyAlpha { get; set; } = true;

	internal FontSystemSettings Clone()
	{
		return new FontSystemSettings
		{
			TextureWidth = _textureWidth,
			TextureHeight = _textureHeight,
			MaxTextureCount = _maxTextureCount,
			GlyphPadding = _glyphPadding,
			SupersamplingFactor = _supersamplingFactor,
			UseKerning = UseKerning,
			PremultiplyAlpha = PremultiplyAlpha,
		};
	}

	internal void Validate()
	{
		if (_textureWidth <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(TextureWidth), _textureWidth, "Texture width must be positive.");
		}

		if (_textureHeight <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(TextureHeight), _textureHeight, "Texture height must be positive.");
		}

		if (_maxTextureCount <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(MaxTextureCount), _maxTextureCount, "Max texture count must be positive.");
		}

		if (_glyphPadding < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(GlyphPadding), _glyphPadding, "Glyph padding cannot be negative.");
		}

		if (!float.IsFinite(_supersamplingFactor) || _supersamplingFactor < 1f)
		{
			throw new ArgumentOutOfRangeException(
				nameof(SupersamplingFactor),
				_supersamplingFactor,
				"Supersampling factor must be finite and at least 1.0.");
		}
	}
}
