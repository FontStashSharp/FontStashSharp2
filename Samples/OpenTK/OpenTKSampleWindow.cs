using System.Numerics;
using FontStashSharp2;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace OpenTKSample;

internal sealed class OpenTKSampleWindow : GameWindow
{
	private const int Width = 1280;
	private const int Height = 720;

	private static readonly Color Background = new(16, 18, 24);
	private static readonly Color Muted = new(146, 154, 171);
	private static readonly Color Accent = new(108, 214, 190);
	private static readonly Color Warm = new(255, 190, 120);
	private static readonly Color Cool = new(140, 200, 255);

	private readonly int _framesToRun;
	private FontSystem _fontSystem = null!;
	private OpenGlTextureManager _textureManager = null!;
	private OpenGlTextRenderer _renderer = null!;
	private int _renderedFrames;

	public OpenTKSampleWindow(int framesToRun)
		: base(
			new GameWindowSettings
			{
				UpdateFrequency = 60.0,
			},
			new NativeWindowSettings
			{
				ClientSize = new Vector2i(Width, Height),
				Title = "FontStashSharp2 - PlatformAgnostic + OpenTK",
				APIVersion = new Version(3, 3),
				Profile = ContextProfile.Core,
				Flags = ContextFlags.ForwardCompatible,
			})
	{
		_framesToRun = framesToRun;
	}

	protected override void OnLoad()
	{
		base.OnLoad();

		GL.ClearColor(Background.R / 255f, Background.G / 255f, Background.B / 255f, 1f);
		GL.Enable(EnableCap.Blend);
		GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		GL.Disable(EnableCap.DepthTest);
		GL.Disable(EnableCap.CullFace);
		GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

		_textureManager = new OpenGlTextureManager();
		_renderer = new OpenGlTextRenderer(_textureManager);
		_renderer.Resize(ClientSize.X, ClientSize.Y);

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

	protected override void OnUpdateFrame(FrameEventArgs args)
	{
		if (KeyboardState.IsKeyDown(Keys.Escape))
		{
			Close();
		}

		base.OnUpdateFrame(args);
	}

	protected override void OnRenderFrame(FrameEventArgs args)
	{
		base.OnRenderFrame(args);

		GL.Clear(ClearBufferMask.ColorBufferBit);
		DrawText();
		SwapBuffers();

		_renderedFrames++;
		if (_framesToRun > 0 && _renderedFrames >= _framesToRun)
		{
			Close();
		}
	}

	protected override void OnResize(ResizeEventArgs e)
	{
		GL.Viewport(0, 0, e.Width, e.Height);
		_renderer?.Resize(e.Width, e.Height);
		base.OnResize(e);
	}

	protected override void OnUnload()
	{
		_fontSystem?.Dispose();
		_renderer?.Dispose();
		base.OnUnload();
	}

	private void DrawText()
	{
		var title = _fontSystem.GetFont(44f);
		var body = _fontSystem.GetFont(34f);
		var small = _fontSystem.GetFont(18f);

		title.DrawText(_renderer, "FontStashSharp2 + OpenTK", new Vector2(64f, 52f), Color.White);
		small.DrawText(
			_renderer,
			"Platform-agnostic build: IFontStashRenderer + ITexture2DManager + OpenGL 3.3",
			new Vector2(66f, 112f),
			Muted);

		body.DrawText(_renderer, "Hello from a TTF loaded at runtime.", new Vector2(64f, 184f), Accent);
		body.DrawText(_renderer, "No MonoGame, no SpriteBatch, no content pipeline.", new Vector2(64f, 238f), Color.White);

		const string rotated = "Rotated and scaled";
		body.DrawText(
			_renderer,
			rotated,
			new Vector2(400f, 330f),
			Warm,
			-0.08f,
			body.MeasureString(rotated) * 0.5f,
			new Vector2(1.15f),
			SpriteEffects.None,
			0f);

		body.DrawText(
			_renderer,
			"Flipped",
			new Vector2(80f, 430f),
			Cool,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.FlipHorizontally,
			0f);

		body.DrawText(
			_renderer,
			"Stroke effect",
			new Vector2(64f, 500f),
			Accent,
			effect: TextEffect.Stroke,
			effectAmount: 1,
			effectColor: Color.White);
		body.DrawText(
			_renderer,
			"Shadow effect",
			new Vector2(64f, 560f),
			Warm,
			effect: TextEffect.Shadow,
			effectAmount: 4,
			effectColor: new Color(120, 70, 30));

		small.DrawText(
			_renderer,
			"Resize the window. Press Esc to exit. --frames <n> runs a bounded smoke test.",
			new Vector2(64f, Height - 52f),
			Muted);
	}
}
