using System.Numerics;
using FontStashSharp2;
using OpenTK.Graphics.OpenGL4;

namespace OpenTKSample;

internal sealed class OpenGlTextRenderer : IFontStashRenderer, IDisposable
{
	private const string VertexShaderSource = """
		#version 330 core
		layout (location = 0) in vec2 aCorner;

		uniform vec2 uViewport;
		uniform vec2 uPosition;
		uniform vec2 uSize;
		uniform vec2 uScale;
		uniform float uRotation;
		uniform vec2 uUvMin;
		uniform vec2 uUvMax;

		out vec2 vUv;

		void main()
		{
			vec2 local = aCorner * uSize * uScale;
			float c = cos(uRotation);
			float s = sin(uRotation);
			vec2 rotated = vec2(local.x * c - local.y * s, local.x * s + local.y * c);
			vec2 screen = uPosition + rotated;
			gl_Position = vec4(
				screen.x / uViewport.x * 2.0 - 1.0,
				1.0 - screen.y / uViewport.y * 2.0,
				0.0,
				1.0);
			vUv = mix(uUvMin, uUvMax, aCorner);
		}
		""";

	private const string FragmentShaderSource = """
		#version 330 core
		in vec2 vUv;

		uniform sampler2D uTexture;
		uniform vec4 uColor;

		out vec4 FragColor;

		void main()
		{
			FragColor = texture(uTexture, vUv) * uColor;
		}
		""";

	private readonly OpenGlTextureManager _textureManager;
	private readonly int _program;
	private readonly int _vao;
	private readonly int _vbo;
	private readonly int _viewportLocation;
	private readonly int _positionLocation;
	private readonly int _sizeLocation;
	private readonly int _scaleLocation;
	private readonly int _rotationLocation;
	private readonly int _uvMinLocation;
	private readonly int _uvMaxLocation;
	private readonly int _textureLocation;
	private readonly int _colorLocation;
	private float _viewportWidth = 1f;
	private float _viewportHeight = 1f;

	public OpenGlTextRenderer(OpenGlTextureManager textureManager)
	{
		_textureManager = textureManager;

		_program = CreateProgram();
		_viewportLocation = GL.GetUniformLocation(_program, "uViewport");
		_positionLocation = GL.GetUniformLocation(_program, "uPosition");
		_sizeLocation = GL.GetUniformLocation(_program, "uSize");
		_scaleLocation = GL.GetUniformLocation(_program, "uScale");
		_rotationLocation = GL.GetUniformLocation(_program, "uRotation");
		_uvMinLocation = GL.GetUniformLocation(_program, "uUvMin");
		_uvMaxLocation = GL.GetUniformLocation(_program, "uUvMax");
		_textureLocation = GL.GetUniformLocation(_program, "uTexture");
		_colorLocation = GL.GetUniformLocation(_program, "uColor");

		float[] corners =
		{
			0f, 0f,
			1f, 0f,
			0f, 1f,
			1f, 1f,
		};

		_vao = GL.GenVertexArray();
		_vbo = GL.GenBuffer();

		GL.BindVertexArray(_vao);
		GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
		GL.BufferData(BufferTarget.ArrayBuffer, corners.Length * sizeof(float), corners, BufferUsageHint.StaticDraw);
		GL.EnableVertexAttribArray(0);
		GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
		GL.BindVertexArray(0);
	}

	public ITexture2DManager TextureManager => _textureManager;

	public void Resize(int width, int height)
	{
		_viewportWidth = Math.Max(1, width);
		_viewportHeight = Math.Max(1, height);
	}

	public void DrawText(
		object texture,
		Vector2 position,
		Rectangle? sourceRectangle,
		Color color,
		float rotation,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
		if (sourceRectangle is not Rectangle source || source.Width <= 0 || source.Height <= 0)
		{
			return;
		}

		var glTexture = (OpenGlTexture)texture;
		var u0 = source.X / (float)glTexture.Width;
		var v0 = source.Y / (float)glTexture.Height;
		var u1 = (source.X + source.Width) / (float)glTexture.Width;
		var v1 = (source.Y + source.Height) / (float)glTexture.Height;

		if ((effects & SpriteEffects.FlipHorizontally) != 0)
		{
			(u0, u1) = (u1, u0);
		}

		if ((effects & SpriteEffects.FlipVertically) != 0)
		{
			(v0, v1) = (v1, v0);
		}

		GL.UseProgram(_program);
		GL.BindVertexArray(_vao);
		GL.ActiveTexture(TextureUnit.Texture0);
		GL.BindTexture(TextureTarget.Texture2D, glTexture.Handle);

		GL.Uniform1(_textureLocation, 0);
		GL.Uniform2(_viewportLocation, _viewportWidth, _viewportHeight);
		GL.Uniform2(_positionLocation, position.X, position.Y);
		GL.Uniform2(_sizeLocation, (float)source.Width, source.Height);
		GL.Uniform2(_scaleLocation, scale.X, scale.Y);
		GL.Uniform1(_rotationLocation, rotation);
		GL.Uniform2(_uvMinLocation, u0, v0);
		GL.Uniform2(_uvMaxLocation, u1, v1);
		GL.Uniform4(_colorLocation, color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

		GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
	}

	public void Dispose()
	{
		GL.DeleteBuffer(_vbo);
		GL.DeleteVertexArray(_vao);
		GL.DeleteProgram(_program);
	}

	private static int CreateProgram()
	{
		var vertexShader = CompileShader(ShaderType.VertexShader, VertexShaderSource);
		var fragmentShader = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);

		var program = GL.CreateProgram();
		GL.AttachShader(program, vertexShader);
		GL.AttachShader(program, fragmentShader);
		GL.LinkProgram(program);
		GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var linked);
		if (linked == 0)
		{
			throw new InvalidOperationException(GL.GetProgramInfoLog(program));
		}

		GL.DetachShader(program, vertexShader);
		GL.DetachShader(program, fragmentShader);
		GL.DeleteShader(vertexShader);
		GL.DeleteShader(fragmentShader);
		return program;
	}

	private static int CompileShader(ShaderType type, string source)
	{
		var shader = GL.CreateShader(type);
		GL.ShaderSource(shader, source);
		GL.CompileShader(shader);
		GL.GetShader(shader, ShaderParameter.CompileStatus, out var compiled);
		if (compiled == 0)
		{
			throw new InvalidOperationException(GL.GetShaderInfoLog(shader));
		}

		return shader;
	}
}
