using FontStashSharp2;
using Silk.NET.OpenGL;

namespace SilkNetSample;

internal sealed class SilkGlTexture : IDisposable
{
	private readonly GL _gl;

	public SilkGlTexture(GL gl, uint handle, int width, int height)
	{
		_gl = gl;
		Handle = handle;
		Width = width;
		Height = height;
	}

	public uint Handle { get; }

	public int Width { get; }

	public int Height { get; }

	public void Dispose()
	{
		_gl.DeleteTexture(Handle);
	}
}

internal sealed class SilkGlTextureManager : ITexture2DManager
{
	private readonly GL _gl;

	public SilkGlTextureManager(GL gl)
	{
		_gl = gl;
	}

	public unsafe object CreateTexture(int width, int height)
	{
		var handle = _gl.GenTexture();

		_gl.BindTexture(TextureTarget.Texture2D, handle);
		_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
		_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
		_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		_gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		_gl.TexImage2D(
			TextureTarget.Texture2D,
			0,
			(int)InternalFormat.Rgba8,
			(uint)width,
			(uint)height,
			0,
			PixelFormat.Rgba,
			PixelType.UnsignedByte,
			(void*)0);

		return new SilkGlTexture(_gl, handle, width, height);
	}

	public Point GetTextureSize(object texture)
	{
		var silkTexture = (SilkGlTexture)texture;
		return new Point(silkTexture.Width, silkTexture.Height);
	}

	public unsafe void SetTextureData(object texture, Rectangle bounds, ReadOnlySpan<byte> data)
	{
		var silkTexture = (SilkGlTexture)texture;

		_gl.BindTexture(TextureTarget.Texture2D, silkTexture.Handle);
		fixed (byte* pixels = data)
		{
			_gl.TexSubImage2D(
				TextureTarget.Texture2D,
				0,
				bounds.X,
				bounds.Y,
				(uint)bounds.Width,
				(uint)bounds.Height,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				pixels);
		}
	}

	public void DestroyTexture(object texture)
	{
		((SilkGlTexture)texture).Dispose();
	}
}
