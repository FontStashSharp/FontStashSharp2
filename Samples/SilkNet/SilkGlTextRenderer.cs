using System.Numerics;
using FontStashSharp2;
using Silk.NET.OpenGL;

namespace SilkNetSample;

internal sealed class SilkGlTextRenderer : IFontStashRenderer, IDisposable
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

	private readonly GL _gl;
	private readonly SilkGlTextureManager _textureManager;
	private readonly uint _program;
	private readonly uint _vao;
	private readonly uint _vbo;
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

	public SilkGlTextRenderer(GL gl, SilkGlTextureManager textureManager)
	{
		_gl = gl;
		_textureManager = textureManager;

		_program = CreateProgram();
		_viewportLocation = _gl.GetUniformLocation(_program, "uViewport");
		_positionLocation = _gl.GetUniformLocation(_program, "uPosition");
		_sizeLocation = _gl.GetUniformLocation(_program, "uSize");
		_scaleLocation = _gl.GetUniformLocation(_program, "uScale");
		_rotationLocation = _gl.GetUniformLocation(_program, "uRotation");
		_uvMinLocation = _gl.GetUniformLocation(_program, "uUvMin");
		_uvMaxLocation = _gl.GetUniformLocation(_program, "uUvMax");
		_textureLocation = _gl.GetUniformLocation(_program, "uTexture");
		_colorLocation = _gl.GetUniformLocation(_program, "uColor");

		float[] corners =
		{
			0f, 0f,
			1f, 0f,
			0f, 1f,
			1f, 1f,
		};

		_vao = _gl.GenVertexArray();
		_vbo = _gl.GenBuffer();

		_gl.BindVertexArray(_vao);
		_gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
		unsafe
		{
			fixed (float* vertices = corners)
			{
				_gl.BufferData(
					BufferTargetARB.ArrayBuffer,
					(nuint)(corners.Length * sizeof(float)),
					vertices,
					BufferUsageARB.StaticDraw);
			}
		}

		_gl.EnableVertexAttribArray(0);
		unsafe
		{
			_gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
		}

		_gl.BindVertexArray(0);
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

		var silkTexture = (SilkGlTexture)texture;
		var u0 = source.X / (float)silkTexture.Width;
		var v0 = source.Y / (float)silkTexture.Height;
		var u1 = (source.X + source.Width) / (float)silkTexture.Width;
		var v1 = (source.Y + source.Height) / (float)silkTexture.Height;

		if ((effects & SpriteEffects.FlipHorizontally) != 0)
		{
			(u0, u1) = (u1, u0);
		}

		if ((effects & SpriteEffects.FlipVertically) != 0)
		{
			(v0, v1) = (v1, v0);
		}

		_gl.UseProgram(_program);
		_gl.BindVertexArray(_vao);
		_gl.ActiveTexture(TextureUnit.Texture0);
		_gl.BindTexture(TextureTarget.Texture2D, silkTexture.Handle);

		_gl.Uniform1(_textureLocation, 0);
		_gl.Uniform2(_viewportLocation, _viewportWidth, _viewportHeight);
		_gl.Uniform2(_positionLocation, position.X, position.Y);
		_gl.Uniform2(_sizeLocation, (float)source.Width, source.Height);
		_gl.Uniform2(_scaleLocation, scale.X, scale.Y);
		_gl.Uniform1(_rotationLocation, rotation);
		_gl.Uniform2(_uvMinLocation, u0, v0);
		_gl.Uniform2(_uvMaxLocation, u1, v1);
		_gl.Uniform4(_colorLocation, color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

		_gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
	}

	public void Dispose()
	{
		_gl.DeleteBuffer(_vbo);
		_gl.DeleteVertexArray(_vao);
		_gl.DeleteProgram(_program);
	}

	private uint CreateProgram()
	{
		var vertexShader = CompileShader(ShaderType.VertexShader, VertexShaderSource);
		var fragmentShader = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);

		var program = _gl.CreateProgram();
		_gl.AttachShader(program, vertexShader);
		_gl.AttachShader(program, fragmentShader);
		_gl.LinkProgram(program);
		_gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var linked);
		if (linked == 0)
		{
			throw new InvalidOperationException(_gl.GetProgramInfoLog(program));
		}

		_gl.DetachShader(program, vertexShader);
		_gl.DetachShader(program, fragmentShader);
		_gl.DeleteShader(vertexShader);
		_gl.DeleteShader(fragmentShader);
		return program;
	}

	private uint CompileShader(ShaderType type, string source)
	{
		var shader = _gl.CreateShader(type);
		_gl.ShaderSource(shader, source);
		_gl.CompileShader(shader);
		_gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);
		if (compiled == 0)
		{
			throw new InvalidOperationException(_gl.GetShaderInfoLog(shader));
		}

		return shader;
	}
}
