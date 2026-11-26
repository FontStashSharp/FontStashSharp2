using System.Numerics;
using FontStashSharp2;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace SilkNetSample;

internal sealed class SilkSample : IDisposable
{
	private const int Width = 1280;
	private const int Height = 720;

	private static readonly Color Background = new(16, 18, 24);
	private static readonly Color Muted = new(146, 154, 171);
	private static readonly Color Accent = new(108, 214, 190);
	private static readonly Color Warm = new(255, 190, 120);
	private static readonly Color Cool = new(140, 200, 255);

	private readonly IWindow _window;
	private readonly int _framesToRun;
	private GL? _gl;
	private FontSystem? _fontSystem;
	private SilkGlTextureManager? _textureManager;
	private SilkGlTextRenderer? _renderer;
	private int _renderedFrames;

	public SilkSample(int framesToRun)
	{
		_framesToRun = framesToRun;

		var options = WindowOptions.Default with
		{
			Size = new Vector2D<int>(Width, Height),
			Title = "FontStashSharp2 - PlatformAgnostic + Silk.NET",
			API = new GraphicsAPI(
				ContextAPI.OpenGL,
				ContextProfile.Core,
				ContextFlags.ForwardCompatible,
				new APIVersion(3, 3)),
			VSync = true,
		};

		_window = Window.Create(options);
		_window.Load += OnLoad;
		_window.Render += OnRender;
		_window.Resize += OnResize;
		_window.Closing += OnClosing;
	}

	public void Run()
	{
		_window.Run();
	}

	public void Dispose()
	{
		_window.Dispose();
	}

	private void OnLoad()
	{
		_gl = GL.GetApi(_window);
		_gl.ClearColor(Background.R / 255f, Background.G / 255f, Background.B / 255f, 1f);
		_gl.Enable(EnableCap.Blend);
		_gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		_gl.Disable(EnableCap.DepthTest);
		_gl.Disable(EnableCap.CullFace);
		_gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);

		_textureManager = new SilkGlTextureManager(_gl);
		_renderer = new SilkGlTextRenderer(_gl, _textureManager);
		_renderer.Resize(_window.Size.X, _window.Size.Y);

		_fontSystem = new FontSystem(new FontSystemSettings
		{
			TextureWidth = 1024,
			TextureHeight = 1024,
			GlyphPadding = 1,
			SupersamplingFactor = 2f,
			UseKerning = true,
			// Straight RGBA + SrcAlpha blending keeps the sample easy to compare with engine defaults.
			AlphaMode = GlyphAlphaMode.NonPremultiplied,
		});

		var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "ABeeZee-Regular.ttf");
		_fontSystem.AddFont(File.ReadAllBytes(fontPath), name: "ABeeZee");
	}

	private void OnRender(double delta)
	{
		if (_gl is null || _fontSystem is null || _renderer is null)
		{
			return;
		}

		_gl.Clear(ClearBufferMask.ColorBufferBit);
		DrawText();

		_renderedFrames++;
		if (_framesToRun > 0 && _renderedFrames >= _framesToRun)
		{
			_window.Close();
		}
	}

	private void OnResize(Vector2D<int> size)
	{
		_gl?.Viewport(0, 0, (uint)Math.Max(1, size.X), (uint)Math.Max(1, size.Y));
		_renderer?.Resize(size.X, size.Y);
	}

	private void OnClosing()
	{
		_fontSystem?.Dispose();
		_renderer?.Dispose();
		_gl?.Dispose();

		_fontSystem = null;
		_renderer = null;
		_textureManager = null;
		_gl = null;
	}

	private void DrawText()
	{
		var fontSystem = _fontSystem!;
		var renderer = _renderer!;
		var title = fontSystem.GetFont(44f);
		var body = fontSystem.GetFont(34f);
		var small = fontSystem.GetFont(18f);

		title.DrawText(renderer, "FontStashSharp2 + Silk.NET", new Vector2(64f, 52f), Color.White);
		small.DrawText(
			renderer,
			"Platform-agnostic build: IFontStashRenderer + ITexture2DManager + OpenGL 3.3",
			new Vector2(66f, 112f),
			Muted);

		body.DrawText(renderer, "Hello from a TTF loaded at runtime.", new Vector2(64f, 184f), Accent);
		body.DrawText(renderer, "No MonoGame, no SpriteBatch, no content pipeline.", new Vector2(64f, 238f), Color.White);

		const string rotated = "Rotated and scaled";
		body.DrawText(
			renderer,
			rotated,
			new Vector2(400f, 330f),
			Warm,
			-0.08f,
			body.MeasureString(rotated) * 0.5f,
			new Vector2(1.15f),
			SpriteEffects.None,
			0f);

		body.DrawText(
			renderer,
			"Flipped",
			new Vector2(80f, 430f),
			Cool,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.FlipHorizontally,
			0f);

		body.DrawText(
			renderer,
			"Stroke effect",
			new Vector2(64f, 500f),
			Accent,
			effect: TextEffectOptions.Stroke(1, Color.White));
		body.DrawText(
			renderer,
			"Shadow effect",
			new Vector2(64f, 560f),
			Warm,
			effect: TextEffectOptions.Shadow(4, new Color(120, 70, 30)));

		small.DrawText(
			renderer,
			"Resize the window. Close it to exit. --frames <n> runs a bounded smoke test.",
			new Vector2(64f, Height - 52f),
			Muted);
	}
}
