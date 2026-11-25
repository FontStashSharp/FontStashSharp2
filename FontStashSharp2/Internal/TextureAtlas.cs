using System.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FontStashSharp2.Internal;

/// <summary>
/// A lazily grown, shelf-packed texture atlas of glyph alpha bitmaps.
/// </summary>
internal sealed class TextureAtlas : IGlyphAtlas, IDisposable
{
	private readonly GraphicsDevice _graphicsDevice;
	private readonly int _width;
	private readonly int _height;
	private readonly int _padding;
	private readonly bool _premultiplyAlpha;
	private readonly int _maxTextureCount;
	private readonly List<Page> _pages = new();
	private readonly object _sync = new();
	private bool _disposed;

	public TextureAtlas(GraphicsDevice graphicsDevice, FontSystemSettings settings)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);
		ArgumentNullException.ThrowIfNull(settings);

		_graphicsDevice = graphicsDevice;
		_width = settings.TextureWidth;
		_height = settings.TextureHeight;
		_padding = settings.GlyphPadding;
		_premultiplyAlpha = settings.PremultiplyAlpha;
		_maxTextureCount = settings.MaxTextureCount;
	}

	public int TextureCount
	{
		get
		{
			lock (_sync)
			{
				return _pages.Count;
			}
		}
	}

	public GlyphPlacement Add(ReadOnlySpan<byte> alphaBitmap, int width, int height)
	{
		if (width <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(width), width, "Bitmap width must be positive.");
		}

		if (height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(height), height, "Bitmap height must be positive.");
		}

		if (alphaBitmap.Length < (long)width * height)
		{
			throw new ArgumentException("The bitmap buffer is smaller than width * height.", nameof(alphaBitmap));
		}

		lock (_sync)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			var slotWidth = checked(width + (_padding * 2));
			var slotHeight = checked(height + (_padding * 2));

			if (slotWidth > _width || slotHeight > _height)
			{
				throw new InvalidOperationException(
					$"A {width}x{height} glyph (plus {_padding}px padding) does not fit in a " +
					$"{_width}x{_height} atlas page. Increase FontSystemSettings.TextureWidth/TextureHeight.");
			}

			Page? page = null;
			var slotX = 0;
			var slotY = 0;

			foreach (var candidate in _pages)
			{
				if (candidate.Packer.TryPack(slotWidth, slotHeight, out slotX, out slotY))
				{
					page = candidate;
					break;
				}
			}

			if (page is null)
			{
				if (_pages.Count >= _maxTextureCount)
				{
					throw new InvalidOperationException(
						$"The glyph atlas reached its limit of {_maxTextureCount} texture page(s). " +
						"Increase FontSystemSettings.MaxTextureCount or use fewer font sizes.");
				}

				page = CreatePage();
				_pages.Add(page);

				if (!page.Packer.TryPack(slotWidth, slotHeight, out slotX, out slotY))
				{
					throw new InvalidOperationException(
						$"A {width}x{height} glyph (plus {_padding}px padding) does not fit in a " +
						$"{_width}x{_height} atlas page. Increase FontSystemSettings.TextureWidth/TextureHeight.");
				}
			}

			var dataLength = checked(slotWidth * slotHeight * 4);
			var data = ArrayPool<byte>.Shared.Rent(dataLength);
			try
			{
				BuildPaddedRgba(alphaBitmap, width, height, slotWidth, slotHeight, data);
				page.Texture.SetData(
					0,
					new Rectangle(slotX, slotY, slotWidth, slotHeight),
					data,
					0,
					dataLength);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(data);
			}

			return new GlyphPlacement(
				page.Index,
				new Rectangle(slotX + _padding, slotY + _padding, width, height));
		}
	}

	public Texture2D GetTexture(int index)
	{
		lock (_sync)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			if (index < 0 || index >= _pages.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, "Atlas texture index is out of range.");
			}

			return _pages[index].Texture;
		}
	}

	public void Dispose()
	{
		lock (_sync)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			foreach (var page in _pages)
			{
				page.Texture.Dispose();
			}

			_pages.Clear();
		}
	}

	private Page CreatePage()
	{
		var texture = new Texture2D(_graphicsDevice, _width, _height, false, SurfaceFormat.Color)
		{
			Name = $"FontStashSharp2.Atlas{_pages.Count}",
		};

		// New texture memory is undefined; clear it once so untouched pixels are transparent.
		texture.SetData(new byte[_width * _height * 4]);
		return new Page(texture, _pages.Count, _width, _height);
	}

	private void BuildPaddedRgba(
		ReadOnlySpan<byte> alphaBitmap,
		int width,
		int height,
		int slotWidth,
		int slotHeight,
		byte[] destination)
	{
		// Extend edge pixels into the padding. This avoids linear-filtering seams between glyphs.
		for (var y = 0; y < slotHeight; y++)
		{
			var sourceY = Math.Clamp(y - _padding, 0, height - 1);
			var sourceRow = sourceY * width;
			var destinationRow = y * slotWidth;

			for (var x = 0; x < slotWidth; x++)
			{
				var sourceX = Math.Clamp(x - _padding, 0, width - 1);
				var alpha = alphaBitmap[sourceRow + sourceX];
				var offset = (destinationRow + x) * 4;

				if (alpha == 0)
				{
					destination[offset] = 0;
					destination[offset + 1] = 0;
					destination[offset + 2] = 0;
					destination[offset + 3] = 0;
				}
				else if (_premultiplyAlpha)
				{
					destination[offset] = alpha;
					destination[offset + 1] = alpha;
					destination[offset + 2] = alpha;
					destination[offset + 3] = alpha;
				}
				else
				{
					destination[offset] = 255;
					destination[offset + 1] = 255;
					destination[offset + 2] = 255;
					destination[offset + 3] = alpha;
				}
			}
		}
	}

	private sealed class Page
	{
		public Page(Texture2D texture, int index, int width, int height)
		{
			Texture = texture;
			Index = index;
			Packer = new ShelfPacker(width, height);
		}

		public Texture2D Texture { get; }

		public int Index { get; }

		public ShelfPacker Packer { get; }
	}
}
