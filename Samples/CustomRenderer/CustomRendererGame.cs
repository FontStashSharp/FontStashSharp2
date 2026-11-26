using System.Globalization;
using System.Text;
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CustomRendererSample;

/// <summary>
/// Demonstrates drawing text exclusively through <see cref="IFontStashRenderer"/> overloads of
/// <c>DrawText</c>, using a custom renderer that adds a drop shadow to every glyph.
/// </summary>
internal sealed class CustomRendererGame : Game
{
	private const int Width = 1280;
	private const int Height = 720;
	private const string SpanText = "ReadOnlySpan<char> overload";

	private static readonly Color Background = new(16, 18, 24);
	private static readonly Color Panel = new(24, 27, 36);
	private static readonly Color Line = new(48, 53, 66);
	private static readonly Color Text = new(238, 241, 247);
	private static readonly Color Muted = new(146, 154, 171);
	private static readonly Color Accent = new(108, 214, 190);
	private static readonly Color Warm = new(255, 190, 120);
	private static readonly Color HighColor = new(140, 200, 255);

	private readonly GraphicsDeviceManager _graphics;
	private readonly string? _screenshotPath;
	private readonly StringBuilder _builderText = new("StringBuilder overload");

	private SpriteBatch _batch = null!;
	private Texture2D _pixel = null!;
	private FontSystem _fontSystem = null!;
	private SpriteBatchFontRenderer _shadowRenderer = null!;
	private SpriteBatchFontRenderer _plainRenderer = null!;
	private RenderTarget2D? _renderTarget;
	private int _screenshotFrames;
	private int _lastDrawCalls;

	public CustomRendererGame(string? screenshotPath)
	{
		_screenshotPath = screenshotPath;

		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = Width,
			PreferredBackBufferHeight = Height,
			SynchronizeWithVerticalRetrace = true,
		};

		IsFixedTimeStep = true;
		TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
		IsMouseVisible = true;
		Window.AllowUserResizing = false;
		Window.Title = "FontStashSharp2 - Custom renderer";
	}

	protected override void LoadContent()
	{
		_batch = new SpriteBatch(GraphicsDevice);

		_pixel = new Texture2D(GraphicsDevice, 1, 1);
		_pixel.SetData(new[] { Color.White });

		_fontSystem = new FontSystem(
			GraphicsDevice,
			new FontSystemSettings
			{
				TextureWidth = 1024,
				TextureHeight = 1024,
				GlyphPadding = 1,
				SupersamplingFactor = 2f,
				UseKerning = true,
			});

		var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "ABeeZee-Regular.ttf");
		_fontSystem.AddFont(File.ReadAllBytes(fontPath), name: "ABeeZee");

		_shadowRenderer = new SpriteBatchFontRenderer(
			GraphicsDevice,
			_batch,
			new Color(4, 6, 10, 190),
			new Vector2(3f, 3f));

		_plainRenderer = new SpriteBatchFontRenderer(
			GraphicsDevice,
			_batch,
			Color.Transparent,
			Vector2.Zero);

		if (_screenshotPath is not null)
		{
			_renderTarget = new RenderTarget2D(
				GraphicsDevice,
				Width,
				Height,
				mipMap: false,
				SurfaceFormat.Color,
				DepthFormat.None);
		}

		base.LoadContent();
	}

	protected override void Update(GameTime gameTime)
	{
		if (Keyboard.GetState().IsKeyDown(Keys.Escape))
		{
			Exit();
		}

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		if (_screenshotPath is not null)
		{
			DrawScreenshotFrame();
			return;
		}

		GraphicsDevice.SetRenderTarget(null);
		GraphicsDevice.Clear(Background);

		_batch.Begin(
			sortMode: SpriteSortMode.Deferred,
			blendState: BlendState.AlphaBlend,
			samplerState: SamplerState.LinearClamp);

		DrawScene(_batch);

		_batch.End();
		base.Draw(gameTime);
	}

	private void DrawScene(SpriteBatch batch)
	{
		var title = _fontSystem.GetFont(44);
		var subtitle = _fontSystem.GetFont(18);
		var section = _fontSystem.GetFont(14);
		var label = _fontSystem.GetFont(15);
		var demo = _fontSystem.GetFont(30);
		var footer = _fontSystem.GetFont(15);

		_lastDrawCalls = _shadowRenderer.DrawCalls + _plainRenderer.DrawCalls;
		_shadowRenderer.ResetFrame();
		_plainRenderer.ResetFrame();

		title.DrawText(_shadowRenderer, "Custom renderer", new Vector2(56f, 28f), Text);
		subtitle.DrawText(
			_plainRenderer,
			"Every line below is drawn through DrawText(renderer, ...) with a custom IFontStashRenderer.",
			new Vector2(58f, 86f),
			Muted);
		batch.Draw(_pixel, new Rectangle(56, 122, Width - 112, 1), Line);

		section.DrawText(_plainRenderer, "DRAWTEXT OVERLOADS", new Vector2(56f, 146f), Accent);

		label.DrawText(_plainRenderer, "string", new Vector2(56f, 185f), Muted);
		demo.DrawText(_shadowRenderer, "Simple string overload", new Vector2(270f, 176f), Text);

		label.DrawText(_plainRenderer, "StringBuilder", new Vector2(56f, 255f), Muted);
		demo.DrawText(_shadowRenderer, _builderText, new Vector2(270f, 246f), Text);

		label.DrawText(_plainRenderer, "ReadOnlySpan<char>", new Vector2(56f, 325f), Muted);
		demo.DrawText(_shadowRenderer, SpanText.AsSpan(), new Vector2(270f, 316f), Text);

		label.DrawText(_plainRenderer, "full transform", new Vector2(56f, 405f), Muted);
		const string transformText = "Rotation + scale + origin";
		demo.DrawText(
			_shadowRenderer,
			transformText,
			new Vector2(600f, 420f),
			Warm,
			-0.07f,
			demo.MeasureString(transformText) * 0.5f,
			new Vector2(1.15f),
			SpriteEffects.None,
			0f);

		label.DrawText(_plainRenderer, "SpriteEffects", new Vector2(56f, 490f), Muted);
		demo.DrawText(
			_shadowRenderer,
			"Flipped text",
			new Vector2(270f, 481f),
			HighColor,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.FlipHorizontally,
			0f);

		batch.Draw(_pixel, new Rectangle(56, 566, Width - 112, 1), Line);

		section.DrawText(_plainRenderer, "RENDERER CONTRACT", new Vector2(56f, 586f), Accent);
		label.DrawText(
			_plainRenderer,
			"Draw receives the glyph texture, pre-transformed top-left position, source rectangle, color, rotation,",
			new Vector2(56f, 610f),
			Muted);
		label.DrawText(
			_plainRenderer,
			"scale, SpriteEffects, and layer depth. This sample draws each glyph twice to add a drop shadow.",
			new Vector2(56f, 630f),
			Muted);

		batch.Draw(_pixel, new Rectangle(0, Height - 40, Width, 40), Panel);
		footer.DrawText(
			_plainRenderer,
			string.Create(
				CultureInfo.InvariantCulture,
				$"IFontStashRenderer.Draw calls last frame: {_lastDrawCalls}   |   Supersampling: 2x   |   Press Esc to exit   |   --screenshot <file.png>"),
			new Vector2(56f, Height - 27f),
			Muted);
	}

	private void DrawScreenshotFrame()
	{
		if (_renderTarget is null || _screenshotPath is null)
		{
			return;
		}

		GraphicsDevice.SetRenderTarget(_renderTarget);
		GraphicsDevice.Clear(Background);

		_batch.Begin(
			sortMode: SpriteSortMode.Deferred,
			blendState: BlendState.AlphaBlend,
			samplerState: SamplerState.LinearClamp);

		DrawScene(_batch);

		_batch.End();
		GraphicsDevice.SetRenderTarget(null);

		_screenshotFrames++;
		if (_screenshotFrames < 2)
		{
			return;
		}

		var directory = Path.GetDirectoryName(_screenshotPath);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		using (var stream = File.Create(_screenshotPath))
		{
			_renderTarget.SaveAsPng(stream, Width, Height);
		}

		Console.WriteLine($"Screenshot written to {_screenshotPath}");
		Exit();
	}

	protected override void UnloadContent()
	{
		_fontSystem?.Dispose();
		_renderTarget?.Dispose();
		_pixel?.Dispose();
		_batch?.Dispose();

		base.UnloadContent();
	}

	private sealed class SpriteBatchFontRenderer : IFontStashRenderer
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly Color _shadowColor;
		private readonly Vector2 _shadowOffset;

		public SpriteBatchFontRenderer(
			GraphicsDevice graphicsDevice,
			SpriteBatch spriteBatch,
			Color shadowColor,
			Vector2 shadowOffset)
		{
			GraphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_shadowColor = shadowColor;
			_shadowOffset = shadowOffset;
		}

		public GraphicsDevice GraphicsDevice { get; }

		public int DrawCalls { get; private set; }

		public void ResetFrame()
		{
			DrawCalls = 0;
		}

		public void Draw(
			Texture2D texture,
			Vector2 position,
			Rectangle? sourceRectangle,
			Color color,
			float rotation,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth)
		{
			if (_shadowColor.A > 0)
			{
				_spriteBatch.Draw(
					texture,
					position + _shadowOffset,
					sourceRectangle,
					_shadowColor,
					rotation,
					Vector2.Zero,
					scale,
					effects,
					layerDepth);
				DrawCalls++;
			}

			_spriteBatch.Draw(
				texture,
				position,
				sourceRectangle,
				color,
				rotation,
				Vector2.Zero,
				scale,
				effects,
				layerDepth);
			DrawCalls++;
		}
	}
}
