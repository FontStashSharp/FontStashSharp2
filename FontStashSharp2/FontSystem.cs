using System.Collections.ObjectModel;
using FontStashSharp2.Internal;

namespace FontStashSharp2;

/// <summary>
/// Owns font sources and the shared glyph texture atlas.
/// </summary>
/// <remarks>
/// <para>
/// A font system does not require a graphics device at construction time. The atlas is created lazily
/// from the device supplied by the first renderer draw/preload call. This allows measurement without
/// a GPU.
/// </para>
/// <para>
/// One font system is bound to one graphics device or texture manager. Create a separate font system
/// per device. Instances are not thread-safe.
/// </para>
/// </remarks>
public sealed class FontSystem : IDisposable
{
	private IGlyphAtlas? _atlas;
	private readonly FontSystemSettings _settings;
	private readonly List<FontSource> _fontSources = new();
	private readonly Dictionary<float, DynamicSpriteFont> _fonts = new();
	private ReadOnlyCollection<FontSource>? _fontSourcesView;
	private long _atlasGeneration;
	private bool _disposed;

	/// <summary>
	/// Occurs when no existing atlas page can fit the next glyph and a new page is about to be
	/// created.
	/// </summary>
	/// <remarks>
	/// The event is raised synchronously from the drawing thread before the new page is allocated.
	/// The first page does not raise this event. Handlers should only inspect the supplied data; do not
	/// draw text or call <see cref="ClearTextures"/> from the handler.
	/// </remarks>
	public event EventHandler<TextureAtlasFullEventArgs>? TextureAtlasFull;

	/// <summary>
	/// Creates a font system. No texture atlas is allocated until text is drawn or preloaded.
	/// </summary>
	/// <param name="settings">Atlas settings, or <see langword="null"/> for defaults.</param>
	public FontSystem(FontSystemSettings? settings = null)
	{
		_settings = (settings ?? new FontSystemSettings()).Clone();
		_settings.Validate();
	}

	internal static FontSystem CreateWithAtlas(IGlyphAtlas atlas, FontSystemSettings? settings = null)
	{
		ArgumentNullException.ThrowIfNull(atlas);

		var fontSystem = new FontSystem(settings);
		fontSystem._atlas = atlas;
		return fontSystem;
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
	/// Gets the number of atlas pages allocated so far. This is 0 until text is drawn or preloaded.
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

	internal float SupersamplingFactor => _settings.SupersamplingFactor;

	internal FontScaleMode ScaleMode => _settings.ScaleMode;

	internal long AtlasGeneration => _atlasGeneration;

	internal IGlyphAtlas GetAtlas(AtlasDevice device)
	{
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(device);

		_atlas ??= new TextureAtlas(_settings, OnTextureAtlasFull);
		_atlas.ValidateDevice(device);
		return _atlas;
	}

	private void OnTextureAtlasFull(TextureAtlasFullEventArgs e)
	{
		TextureAtlasFull?.Invoke(this, e);
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
	/// <param name="size">The font size in pixels. Must be positive and finite.</param>
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
	/// No atlas has been created yet. Draw or preload text first.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
	public AtlasTexture GetTexture(int index)
	{
		ThrowIfDisposed();

		if (_atlas is not TextureAtlas textureAtlas)
		{
			throw new InvalidOperationException(
				"No atlas textures have been created yet. Draw or preload text through a renderer first.");
		}

		return textureAtlas.GetTexture(index);
	}

	/// <summary>
	/// Destroys all atlas textures and clears cached rasterized glyphs.
	/// </summary>
	/// <remarks>
	/// Font sources, measured metrics, and kerning caches are kept. The next draw or preload creates a
	/// new atlas and rasterizes glyphs on demand again. Textures returned by <see cref="GetTexture"/>
	/// before this call become invalid. The font system is not thread-safe; do not call this while
	/// drawing.
	/// </remarks>
	public void ClearTextures()
	{
		ThrowIfDisposed();

		_atlasGeneration++;
		(_atlas as IDisposable)?.Dispose();
		_atlas = null;

		// Existing font instances may not be in _fonts (AddFont clears the cache), so they also
		// compare AtlasGeneration before drawing.
		foreach (var font in _fonts.Values)
		{
			font.ClearRenderedGlyphs();
		}
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
		_atlas = null;
		_fonts.Clear();
		_fontSources.Clear();
	}

	internal AtlasTexture GetWhiteTexture(AtlasDevice device)
	{
		ThrowIfDisposed();

		var atlas = GetAtlas(device);
		if (atlas is not TextureAtlas textureAtlas)
		{
			throw new InvalidOperationException("Text decorations require a real texture atlas.");
		}

		return textureAtlas.GetWhiteTexture(device);
	}

	internal AtlasTexture GetTextureOrThrow(int index)
	{
		ThrowIfDisposed();

		if (_atlas is not TextureAtlas textureAtlas)
		{
			throw new InvalidOperationException(
				"No atlas textures have been created yet. Draw or preload text through a renderer first.");
		}

		return textureAtlas.GetTexture(index);
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}
}
