using FontStashSharp2;
using OpenTK.Graphics.OpenGL4;

namespace OpenTKSample;

internal sealed class OpenGlTexture : IDisposable
{
	public OpenGlTexture(int handle, int width, int height)
	{
		Handle = handle;
		Width = width;
		Height = height;
	}

	public int Handle { get; }

	public int Width { get; }

	public int Height { get; }

	public void Dispose()
	{
		GL.DeleteTexture(Handle);
	}
}

internal sealed class OpenGlTextureManager : ITexture2DManager
{
	public object CreateTexture(int width, int height)
	{
		var handle = GL.GenTexture();

		GL.BindTexture(TextureTarget.Texture2D, handle);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		GL.TexImage2D(
			TextureTarget.Texture2D,
			0,
			PixelInternalFormat.Rgba8,
			width,
			height,
			0,
			PixelFormat.Rgba,
			PixelType.UnsignedByte,
			IntPtr.Zero);

		return new OpenGlTexture(handle, width, height);
	}

	public Point GetTextureSize(object texture)
	{
		var glTexture = (OpenGlTexture)texture;
		return new Point(glTexture.Width, glTexture.Height);
	}

	public void SetTextureData(object texture, Rectangle bounds, ReadOnlySpan<byte> data)
	{
		var glTexture = (OpenGlTexture)texture;

		GL.BindTexture(TextureTarget.Texture2D, glTexture.Handle);
		unsafe
		{
			fixed (byte* pixels = data)
			{
				GL.TexSubImage2D(
					TextureTarget.Texture2D,
					0,
					bounds.X,
					bounds.Y,
					bounds.Width,
					bounds.Height,
					PixelFormat.Rgba,
					PixelType.UnsignedByte,
					(IntPtr)pixels);
			}
		}
	}

	public void DestroyTexture(object texture)
	{
		((OpenGlTexture)texture).Dispose();
	}
}
