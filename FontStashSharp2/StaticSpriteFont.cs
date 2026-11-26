using SharpFNT;
using StbImageNext;
#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FontStashSharp2;

/// <summary>
/// A font loaded from an AngelCode BMFont description and its page images.
/// </summary>
/// <remarks>
/// <para>
/// The BMFont descriptor may be in text, XML, or binary format. Page images are decoded with
/// StbImageNext and uploaded to the graphics device or texture manager on first draw.
/// </para>
/// <para>
/// Measurement never creates textures. A different device or texture manager than the one used for
/// the first draw is rejected; create one <see cref="StaticSpriteFont"/> per device.
/// </para>
/// </remarks>
public sealed class StaticSpriteFont : SpriteFontBase, IDisposable
{
	private readonly Dictionary<int, BitmapGlyph> _glyphs = new();
	private readonly Dictionary<long, int> _kerningPairs = new();
	private readonly Dictionary<int, PageSource> _pages = new();
	private string _name = string.Empty;
	private float _fontSize;
	private float _ascent;
	private float _descent;
	private float _lineSpacing;
	private AtlasDevice? _device;
	private AtlasTexture? _whiteTexture;
	private bool _disposed;

	/// <summary>
	/// Loads a BMFont descriptor from a file. Relative page image paths are resolved against the
	/// descriptor's directory.
	/// </summary>
	/// <param name="path">The path to the BMFont descriptor (text, XML, or binary).</param>
	/// <param name="alphaMode">
	/// How decoded page pixels are stored before upload. The default is
	/// <see cref="GlyphAlphaMode.Premultiplied"/>.
	/// </param>
	public StaticSpriteFont(string path, GlyphAlphaMode alphaMode = GlyphAlphaMode.Premultiplied)
	{
		ArgumentNullException.ThrowIfNull(path);
		AlphaMode = ValidateAlphaMode(alphaMode);

		var fullPath = Path.GetFullPath(path);
		var baseDirectory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
		Load(File.ReadAllBytes(fullPath), baseDirectory);
	}

	/// <summary>
	/// Loads a BMFont descriptor from bytes.
	/// </summary>
	/// <param name="fontData">The complete descriptor in text, XML, or binary format.</param>
	/// <param name="baseDirectory">
	/// The directory used to resolve relative page image paths, or <see langword="null"/> to use the
	/// current directory.
	/// </param>
	/// <param name="alphaMode">
	/// How decoded page pixels are stored before upload. The default is
	/// <see cref="GlyphAlphaMode.Premultiplied"/>.
	/// </param>
	public StaticSpriteFont(
		byte[] fontData,
		string? baseDirectory = null,
		GlyphAlphaMode alphaMode = GlyphAlphaMode.Premultiplied)
	{
		ArgumentNullException.ThrowIfNull(fontData);
		AlphaMode = ValidateAlphaMode(alphaMode);
		Load(fontData, baseDirectory ?? Directory.GetCurrentDirectory());
	}

	/// <summary>
	/// Loads a BMFont descriptor from a stream. The stream is read to its end and left open.
	/// </summary>
	/// <param name="fontStream">The stream containing the descriptor in text, XML, or binary format.</param>
	/// <param name="baseDirectory">
	/// The directory used to resolve relative page image paths, or <see langword="null"/> to use the
	/// current directory.
	/// </param>
	/// <param name="alphaMode">
	/// How decoded page pixels are stored before upload. The default is
	/// <see cref="GlyphAlphaMode.Premultiplied"/>.
	/// </param>
	public StaticSpriteFont(
		Stream fontStream,
		string? baseDirectory = null,
		GlyphAlphaMode alphaMode = GlyphAlphaMode.Premultiplied)
	{
		ArgumentNullException.ThrowIfNull(fontStream);

		if (!fontStream.CanRead)
		{
			throw new ArgumentException("The stream must be readable.", nameof(fontStream));
		}

		AlphaMode = ValidateAlphaMode(alphaMode);
		using var buffer = new MemoryStream();
		fontStream.CopyTo(buffer);
		Load(buffer.ToArray(), baseDirectory ?? Directory.GetCurrentDirectory());
	}

	/// <summary>
	/// Gets the font face name from the BMFont descriptor.
	/// </summary>
	public string Name
	{
		get
		{
			ThrowIfDisposed();
			return _name;
		}
	}

	/// <summary>
	/// Gets how decoded page pixels are stored before upload.
	/// </summary>
	public GlyphAlphaMode AlphaMode { get; }

	/// <inheritdoc />
	public override float FontSize
	{
		get
		{
			ThrowIfDisposed();
			return _fontSize;
		}
	}

	/// <summary>
	/// Gets the scaled ascent above the baseline, in pixels.
	/// </summary>
	public override float Ascent
	{
		get
		{
			ThrowIfDisposed();
			return _ascent;
		}
	}

	/// <summary>
	/// Gets the scaled descent below the baseline, in pixels (negative).
	/// </summary>
	public override float Descent
	{
		get
		{
			ThrowIfDisposed();
			return _descent;
		}
	}

	/// <inheritdoc />
	public override float LineSpacing
	{
		get
		{
			ThrowIfDisposed();
			return _lineSpacing;
		}
		set
		{
			ThrowIfDisposed();

			if (!float.IsFinite(value) || value < 0f)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "LineSpacing must be finite and non-negative.");
			}

			_lineSpacing = value;
		}
	}

	/// <summary>
	/// Uploads all BMFont page images to the device ahead of the first draw, avoiding a first-draw
	/// hitch.
	/// </summary>
	/// <param name="device">The graphics device or texture manager used to create the textures.</param>
	/// <returns>The number of page textures created by this call.</returns>
	public int PreloadTextures(AtlasDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);
		ThrowIfDisposed();
		EnsureDevice(device);

		var created = 0;
		foreach (var pageIndex in _pages.Keys)
		{
			if (_pages[pageIndex].Texture is null)
			{
				GetTexture(device, pageIndex);
				created++;
			}
		}

		return created;
	}

	/// <summary>
	/// Releases the page and decoration textures created for the device.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		foreach (var page in _pages.Values)
		{
			if (page.Texture is { } texture)
			{
				DestroyTexture(texture);
			}

			page.Texture = null;
			page.Data = null;
		}

		if (_whiteTexture is { } white)
		{
			DestroyTexture(white);
		}

		_whiteTexture = null;
		_device = null;
	}

	private bool TryGetGlyph(int codepoint, out BitmapGlyph glyph)
	{
		if (_glyphs.TryGetValue(codepoint, out glyph))
		{
			return true;
		}

		return _glyphs.TryGetValue(-1, out glyph);
	}

	/// <inheritdoc />
	protected override float GetKerning(int left, int right)
	{
		var key = ((long)left << 32) | (uint)right;
		return _kerningPairs.TryGetValue(key, out var amount) ? amount : 0f;
	}

	/// <inheritdoc />
	protected override AtlasTexture GetTexture(AtlasDevice device, int pageIndex)
	{
		EnsureDevice(device);

		if (!_pages.TryGetValue(pageIndex, out var page))
		{
			throw new InvalidOperationException($"The BMFont descriptor references missing page {pageIndex}.");
		}

		if (page.Texture is null)
		{
			if (page.Data is null)
			{
				throw new InvalidOperationException($"BMFont page {pageIndex} has no decoded pixel data.");
			}

			page.Texture = CreateTexture(device, page.Width, page.Height, page.Data);
			page.Data = null;
		}

		return page.Texture;
	}

	/// <inheritdoc />
	protected override AtlasTexture GetWhiteTexture(AtlasDevice device)
	{
		EnsureDevice(device);

		if (_whiteTexture is null)
		{
			_whiteTexture = CreateTexture(device, 1, 1, new byte[] { 255, 255, 255, 255 });
		}

		return _whiteTexture;
	}

	private void EnsureDevice(AtlasDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);

		if (_device is null)
		{
			_device = device;
			return;
		}

		if (!ReferenceEquals(_device, device))
		{
#if MONOGAME
			throw new InvalidOperationException(
				"This StaticSpriteFont already owns textures for a different GraphicsDevice. Create one instance per graphics device.");
#else
			throw new InvalidOperationException(
				"This StaticSpriteFont already owns textures for a different ITexture2DManager. Create one instance per texture manager.");
#endif
		}
	}

	private static AtlasTexture CreateTexture(AtlasDevice device, int width, int height, byte[] data)
	{
		var expectedLength = checked(width * height * 4);
		if (data.Length != expectedLength)
		{
			throw new ArgumentException(
				$"The RGBA buffer must contain exactly {expectedLength} bytes for a {width}x{height} texture.",
				nameof(data));
		}

#if MONOGAME
		var texture = new Texture2D(device, width, height, false, SurfaceFormat.Color)
		{
			Name = "FontStashSharp2.Static",
		};
		texture.SetData(data);
		return texture;
#else
		var texture = device.CreateTexture(width, height);
		device.SetTextureData(texture, new Rectangle(0, 0, width, height), data);
		return texture;
#endif
	}

	private void DestroyTexture(AtlasTexture texture)
	{
#if MONOGAME
		texture.Dispose();
#else
		_device?.DestroyTexture(texture);
#endif
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	private void Load(byte[] data, string baseDirectory)
	{
		BitmapFont font;
		var format = DetectFormat(data);

		using (var stream = new MemoryStream(data, writable: false))
		{
			font = BitmapFont.FromStream(stream, format, leaveOpen: false);
		}

		if (font.Info is null)
		{
			throw new InvalidDataException("The BMFont descriptor has no info block.");
		}

		if (font.Common is null)
		{
			throw new InvalidDataException("The BMFont descriptor has no common block.");
		}

		if (font.Characters is null || font.Characters.Count == 0)
		{
			throw new InvalidDataException("The BMFont descriptor has no characters.");
		}

		if (font.Pages is null || font.Pages.Count == 0)
		{
			throw new InvalidDataException("The BMFont descriptor has no page images.");
		}

		_lineSpacing = font.Common.LineHeight;
		if (!float.IsFinite(_lineSpacing) || _lineSpacing <= 0f)
		{
			throw new InvalidDataException("The BMFont common lineHeight must be positive.");
		}

		_name = font.Info.Face ?? string.Empty;
		_fontSize = font.Info.Size > 0 ? font.Info.Size : _lineSpacing;
		_ascent = font.Common.Base;
		_descent = font.Common.Base - font.Common.LineHeight;

		if (!float.IsFinite(_fontSize) || _fontSize <= 0f)
		{
			throw new InvalidDataException("The BMFont info size must be positive.");
		}

		foreach (var pair in font.Characters)
		{
			var character = pair.Value;
			if (character is null)
			{
				continue;
			}

			if (character.Width < 0 || character.Height < 0)
			{
				throw new InvalidDataException(
					$"BMFont character {pair.Key} has a negative size.");
			}

			_glyphs[pair.Key] = new BitmapGlyph(
				character.X,
				character.Y,
				character.Width,
				character.Height,
				character.XOffset,
				character.YOffset,
				character.XAdvance,
				character.Page);
		}

		if (font.KerningPairs is not null)
		{
			foreach (var pair in font.KerningPairs)
			{
				var key = ((long)pair.Key.First << 32) | (uint)pair.Key.Second;
				_kerningPairs[key] = pair.Value;
			}
		}

		foreach (var pair in font.Pages)
		{
			var pagePath = ResolvePagePath(baseDirectory, pair.Value);
			var image = ImageResult.FromMemory(
				File.ReadAllBytes(pagePath),
				ColorComponents.RedGreenBlueAlpha);

			if (image is null || image.Width <= 0 || image.Height <= 0 || image.Data is null)
			{
				throw new InvalidDataException($"Could not decode BMFont page image '{pagePath}'.");
			}

			var expectedLength = checked(image.Width * image.Height * 4);
			if (image.Data.Length != expectedLength)
			{
				throw new InvalidDataException(
					$"BMFont page image '{pagePath}' did not decode as RGBA.");
			}

			ApplyAlphaMode(image.Data, AlphaMode);
			_pages[pair.Key] = new PageSource(image.Width, image.Height, image.Data);
		}

		foreach (var pair in _glyphs)
		{
			if (!_pages.ContainsKey(pair.Value.Page))
			{
				throw new InvalidDataException(
					$"BMFont character {pair.Key} references missing page {pair.Value.Page}.");
			}
		}
	}

	private static string ResolvePagePath(string baseDirectory, string pageFile)
	{
		var normalized = pageFile
			.Replace('\\', Path.DirectorySeparatorChar)
			.Replace('/', Path.DirectorySeparatorChar);

		return Path.GetFullPath(Path.Combine(baseDirectory, normalized));
	}

	private static FormatHint DetectFormat(ReadOnlySpan<byte> data)
	{
		if (data.Length >= 3
			&& data[0] == (byte)'B'
			&& data[1] == (byte)'M'
			&& data[2] == (byte)'F')
		{
			return FormatHint.Binary;
		}

		var offset = 0;
		if (data.Length >= 3
			&& data[0] == 0xEF
			&& data[1] == 0xBB
			&& data[2] == 0xBF)
		{
			offset = 3;
		}

		while (offset < data.Length && data[offset] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
		{
			offset++;
		}

		return offset < data.Length && data[offset] == (byte)'<'
			? FormatHint.XML
			: FormatHint.Text;
	}

	private static GlyphAlphaMode ValidateAlphaMode(GlyphAlphaMode alphaMode)
	{
		if (alphaMode is not (GlyphAlphaMode.Premultiplied
			or GlyphAlphaMode.NonPremultiplied
			or GlyphAlphaMode.NoAntialiasing))
		{
			throw new ArgumentOutOfRangeException(
				nameof(alphaMode),
				alphaMode,
				"Alpha mode must be Premultiplied, NonPremultiplied, or NoAntialiasing.");
		}

		return alphaMode;
	}

	private static void ApplyAlphaMode(byte[] rgba, GlyphAlphaMode alphaMode)
	{
		switch (alphaMode)
		{
			case GlyphAlphaMode.Premultiplied:
				for (var i = 0; i < rgba.Length; i += 4)
				{
					var alpha = rgba[i + 3];
					if (alpha == 255)
					{
						continue;
					}

					if (alpha == 0)
					{
						rgba[i] = 0;
						rgba[i + 1] = 0;
						rgba[i + 2] = 0;
						continue;
					}

					rgba[i] = (byte)((rgba[i] * alpha + 127) / 255);
					rgba[i + 1] = (byte)((rgba[i + 1] * alpha + 127) / 255);
					rgba[i + 2] = (byte)((rgba[i + 2] * alpha + 127) / 255);
				}

				break;

			case GlyphAlphaMode.NonPremultiplied:
				break;

			case GlyphAlphaMode.NoAntialiasing:
				for (var i = 0; i < rgba.Length; i += 4)
				{
					if (rgba[i + 3] == 0)
					{
						rgba[i] = 0;
						rgba[i + 1] = 0;
						rgba[i + 2] = 0;
					}
					else
					{
						rgba[i] = 255;
						rgba[i + 1] = 255;
						rgba[i + 2] = 255;
						rgba[i + 3] = 255;
					}
				}

				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(alphaMode), alphaMode, null);
		}
	}

	private readonly struct BitmapGlyph
	{
		public BitmapGlyph(
			int x,
			int y,
			int width,
			int height,
			int xOffset,
			int yOffset,
			int advance,
			int page)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
			XOffset = xOffset;
			YOffset = yOffset;
			Advance = advance;
			Page = page;
		}

		public int X { get; }

		public int Y { get; }

		public int Width { get; }

		public int Height { get; }

		public int XOffset { get; }

		public int YOffset { get; }

		public int Advance { get; }

		public int Page { get; }
	}

	private sealed class PageSource
	{
		public PageSource(int width, int height, byte[] data)
		{
			Width = width;
			Height = height;
			Data = data;
		}

		public int Width { get; }

		public int Height { get; }

		public byte[]? Data { get; set; }

		public AtlasTexture? Texture { get; set; }
	}

	/// <inheritdoc />
	protected override void PrepareForMeasure()
	{
		ThrowIfDisposed();
	}

	/// <inheritdoc />
	protected override void PrepareForDraw()
	{
		ThrowIfDisposed();
	}

	/// <inheritdoc />
	protected override float GetAdvance(int codepoint)
	{
		return TryGetGlyph(codepoint, out var glyph) ? glyph.Advance : 0f;
	}

	/// <inheritdoc />
	protected override bool TryGetRenderedGlyph(
		int codepoint,
		AtlasDevice device,
		out SpriteGlyph glyph)
	{
		if (!TryGetGlyph(codepoint, out var bitmapGlyph))
		{
			glyph = default;
			return false;
		}

		if (bitmapGlyph.Width <= 0 || bitmapGlyph.Height <= 0)
		{
			glyph = new SpriteGlyph(-1, Rectangle.Empty, 0f, 0f, bitmapGlyph.Advance);
			return true;
		}

		glyph = new SpriteGlyph(
			bitmapGlyph.Page,
			new Rectangle(bitmapGlyph.X, bitmapGlyph.Y, bitmapGlyph.Width, bitmapGlyph.Height),
			bitmapGlyph.XOffset,
			bitmapGlyph.YOffset - Ascent,
			bitmapGlyph.Advance);
		return true;
	}
}
