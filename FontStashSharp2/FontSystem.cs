using System.Collections.ObjectModel;
using FontStashSharp2.Internal;
using Microsoft.Xna.Framework.Graphics;

namespace FontStashSharp2;

/// <summary>
/// Owns font sources and the shared glyph texture atlas.
/// </summary>
/// <remarks>
/// <para>
/// A font system can be created without a <see cref="GraphicsDevice"/> for measurement-only use.
/// Attempting to draw or preload glyphs on such an instance throws <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// Instances are not thread-safe. A single <see cref="GraphicsDevice"/> should be used from one thread,
/// as required by MonoGame.
/// </para>
/// </remarks>
public sealed class FontSystem : IDisposable
{
	private readonly IGlyphAtlas? _atlas;
	private readonly FontSystemSettings _settings;
	private readonly List<FontSource> _fontSources = new();
	private readonly Dictionary<float, DynamicSpriteFont> _fonts = new();
	private ReadOnlyCollection<FontSource>? _fontSourcesView;
	private bool _disposed;

	/// <summary>
	/// Creates a measurement-only font system. No texture atlas is allocated.
	/// </summary>
	public FontSystem()
		: this((GraphicsDevice?)null, null)
	{
	}

	/// <summary>
	/// Creates a font system.
	/// </summary>
	/// <param name="graphicsDevice">
	/// The device used to allocate atlas textures. Pass <see langword="null"/> to measure text only.
	/// </param>
	/// <param name="settings">Atlas settings, or <see langword="null"/> for defaults.</param>
	public FontSystem(GraphicsDevice? graphicsDevice, FontSystemSettings? settings = null)
	{
		_settings = (settings ?? new FontSystemSettings()).Clone();
		_settings.Validate();

		if (graphicsDevice is not null)
		{
			_atlas = new TextureAtlas(graphicsDevice, _settings);
		}
	}

	private FontSystem(FontSystemSettings settings, IGlyphAtlas? atlas)
	{
		_settings = settings;
		_settings.Validate();
		_atlas = atlas;
	}

	internal static FontSystem CreateWithAtlas(IGlyphAtlas atlas, FontSystemSettings? settings = null)
	{
		ArgumentNullException.ThrowIfNull(atlas);
		return new FontSystem((settings ?? new FontSystemSettings()).Clone(), atlas);
	}

	/// <summary>
	/// Gets the font sources in fallback order. The first source is the primary font.
	/// </summary>
	public IReadOnlyList<FontSource> FontSources
	{
		get
		{
			ThrowIfDisposed();
			return _fontSourcesView ??= _fontSources.AsReadOnly();
		}
	}

	/// <summary>
	/// Gets the number of atlas pages allocated so far. Measurement alone never allocates pages.
	/// </summary>
	public int TextureCount
	{
		get
		{
			ThrowIfDisposed();
			return _atlas?.TextureCount ?? 0;
		}
	}

	internal FontSystemSettings Settings => _settings;

	internal bool UseKerning => _settings.UseKerning;

	internal IGlyphAtlas GetAtlasOrThrow()
	{
		ThrowIfDisposed();
		return _atlas ?? throw new InvalidOperationException(
			"This FontSystem was created without a GraphicsDevice. Create it with a GraphicsDevice to draw or preload glyphs.");
	}

	/// <summary>
	/// Adds a TrueType or OpenType font from its raw bytes.
	/// </summary>
	/// <param name="data">
	/// Complete font file contents. The array is read in place by StbTrueTypeNext and must not be mutated
	/// while the font is in use.
	/// </param>
	/// <param name="fontIndex">Zero-based face index inside a TrueType/OpenType collection (.ttc/.otc).</param>
	/// <param name="name">Optional display name; when omitted, the font's name table is used.</param>
	/// <returns>The added source.</returns>
	/// <exception cref="InvalidDataException">The data is not a supported font.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="fontIndex"/> is out of range.</exception>
	public FontSource AddFont(byte[] data, int fontIndex = 0, string? name = null)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(data);

		var source = new FontSource(data, fontIndex, name, _fontSources.Count);
		_fontSources.Add(source);

		// New fonts become part of the fallback chain for fonts created from now on.
		_fonts.Clear();
		return source;
	}

	/// <summary>
	/// Adds a TrueType or OpenType font read from a stream. The stream is read from its current
	/// position to the end and is not disposed.
	/// </summary>
	/// <param name="stream">The stream containing the font data.</param>
	/// <param name="fontIndex">Zero-based face index inside a font collection.</param>
	/// <param name="name">Optional display name.</param>
	public FontSource AddFont(Stream stream, int fontIndex = 0, string? name = null)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(stream);

		using var memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return AddFont(memoryStream.ToArray(), fontIndex, name);
	}

	/// <summary>
	/// Adds a TrueType or OpenType font from a file.
	/// </summary>
	/// <param name="path">Path to a .ttf, .otf, .ttc, or .otc file.</param>
	/// <param name="fontIndex">Zero-based face index inside a font collection.</param>
	/// <param name="name">Optional display name.</param>
	public FontSource AddFont(string path, int fontIndex = 0, string? name = null)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(path);

		return AddFont(File.ReadAllBytes(path), fontIndex, name);
	}

	/// <summary>
	/// Gets a font of a given size in pixels. Sizes are cached: repeated calls with the same size
	/// return the same instance and share its glyph cache.
	/// </summary>
	/// <param name="size">The font size in pixels (the em size). Must be positive and finite.</param>
	public DynamicSpriteFont GetFont(float size)
	{
		ThrowIfDisposed();

		if (!float.IsFinite(size) || size <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(size), size, "Font size must be a positive, finite number.");
		}

		if (_fontSources.Count == 0)
		{
			throw new InvalidOperationException("Add at least one font with AddFont before calling GetFont.");
		}

		if (_fonts.TryGetValue(size, out var cached))
		{
			return cached;
		}

		var font = new DynamicSpriteFont(this, _fontSources.ToArray(), size);
		_fonts.Add(size, font);
		return font;
	}

	/// <summary>
	/// Gets an allocated atlas texture by index. The returned texture is owned by this system;
	/// do not dispose it.
	/// </summary>
	/// <param name="index">Zero-based atlas page index.</param>
	/// <exception cref="InvalidOperationException">
	/// This system was created without a graphics device.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
	public Texture2D GetTexture(int index)
	{
		ThrowIfDisposed();

		if (_atlas is not TextureAtlas textureAtlas)
		{
			throw new InvalidOperationException(
				"This FontSystem was created without a GraphicsDevice, so it has no atlas textures.");
		}

		return textureAtlas.GetTexture(index);
	}

	/// <summary>
	/// Disposes all atlas textures. The object cannot be used afterwards.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		(_atlas as IDisposable)?.Dispose();
		_fonts.Clear();
		_fontSources.Clear();
	}

	internal Texture2D GetTextureOrThrow(int index)
	{
		ThrowIfDisposed();

		if (_atlas is not TextureAtlas textureAtlas)
		{
			throw new InvalidOperationException(
				"Text rendering requires a FontSystem created with a GraphicsDevice.");
		}

		return textureAtlas.GetTexture(index);
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}
}
