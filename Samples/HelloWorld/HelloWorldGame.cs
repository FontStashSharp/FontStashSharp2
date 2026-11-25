using System.Globalization;
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HelloWorldSample;

/// <summary>
/// Demonstrates on-demand TrueType rendering through SpriteBatch: sizes, measurement, centered
/// rotation, scaling, flipping, and atlas statistics.
/// </summary>
public sealed class HelloWorldGame : Game
{
	private const int Width = 1280;
	private const int Height = 720;

	private static readonly Color Background = new(16, 18, 24);
	private static readonly Color Panel = new(24, 27, 36);
	private static readonly Color Line = new(48, 53, 66);
	private static readonly Color Text = new(238, 241, 247);
	private static readonly Color Muted = new(146, 154, 171);
	private static readonly Color Accent = new(108, 214, 190);
	private static readonly Color Warm = new(255, 190, 120);

	private readonly GraphicsDeviceManager _graphics;
	private readonly string? _screenshotPath;

	private SpriteBatch _spriteBatch = null!;
	private FontSystem _fontSystem = null!;
	private Texture2D _pixel = null!;
	private RenderTarget2D? _renderTarget;
	private int _frames;

	public HelloWorldGame(string[] args)
	{
		_screenshotPath = ParseScreenshotPath(args);

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
		Window.Title = "FontStashSharp2 - Hello World";
	}

	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);

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
		using (var stream = File.OpenRead(fontPath))
		{
			_fontSystem.AddFont(stream, name: "ABeeZee");
		}

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

		_spriteBatch.Begin(
			sortMode: SpriteSortMode.Deferred,
			blendState: BlendState.AlphaBlend,
			samplerState: SamplerState.LinearClamp);

		DrawScene(_spriteBatch);

		_spriteBatch.End();
		base.Draw(gameTime);
	}

	private void DrawScene(SpriteBatch batch)
	{
		var title = _fontSystem.GetFont(48);
		var subtitle = _fontSystem.GetFont(18);
		var section = _fontSystem.GetFont(14);
		var body = _fontSystem.GetFont(16);
		var measureFont = _fontSystem.GetFont(22);

		batch.DrawString(title, "FontStashSharp2", new Vector2(56f, 32f), Text);
		batch.DrawString(
			subtitle,
			"On-demand TrueType text for MonoGame, drawn with SpriteBatch",
			new Vector2(58f, 94f),
			Muted);
		batch.Draw(_pixel, new Rectangle(56, 128, Width - 112, 1), Line);

		batch.DrawString(section, "ONE FONT SYSTEM, MANY SIZES", new Vector2(56f, 148f), Accent);
		DrawSizeSample(batch, 18, "The quick brown fox jumps over the lazy dog", 176f);
		DrawSizeSample(batch, 24, "Sphinx of black quartz, judge my vow", 204f);
		DrawSizeSample(batch, 32, "Handgloves & kerning: AV To WA LT", 238f);
		DrawSizeSample(batch, 40, "On-demand glyph atlas", 282f);
		DrawSizeSample(batch, 56, "Hello, MonoGame!", 334f);

		var paragraph = _fontSystem.GetFont(20);
		batch.DrawString(section, "MULTILINE TEXT", new Vector2(56f, 428f), Accent);
		batch.DrawString(
			paragraph,
			"Line breaks use LineSpacing.\nGlyphs are shared by every size.\nMissing code points use font fallback.",
			new Vector2(56f, 452f),
			Muted);

		batch.DrawString(section, "MEASURE, ROTATE, SCALE, FLIP", new Vector2(720f, 148f), Accent);
		batch.Draw(_pixel, new Rectangle(720, 172, Width - 776, 1), Line);
		batch.DrawString(body, "MeasureString() uses the same advances and kerning as drawing.", new Vector2(720f, 186f), Muted);

		var measuredText = "Right-aligned with MeasureString";
		var measuredSize = measureFont.MeasureString(measuredText);
		batch.DrawString(
			measureFont,
			measuredText,
			new Vector2(Width - 56f - measuredSize.X, 218f),
			Text);

		batch.DrawString(
			body,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Measured: {measuredSize.X:0.0} x {measuredSize.Y:0.0} px"),
			new Vector2(720f, 252f),
			Warm);

		batch.Draw(_pixel, new Rectangle(720, 286, Width - 776, 1), Line);

		var rotatedFont = _fontSystem.GetFont(30);
		const string rotatedText = "Rotated around its center";
		var rotatedSize = rotatedFont.MeasureString(rotatedText);
		var rotatedOrigin = rotatedSize * 0.5f;
		var rotatedCenter = new Vector2(980f, 340f);
		batch.DrawString(
			rotatedFont,
			rotatedText,
			rotatedCenter,
			Text,
			-0.14f,
			rotatedOrigin,
			1f,
			SpriteEffects.None,
			0f);

		var scaledFont = _fontSystem.GetFont(24);
		batch.DrawString(
			scaledFont,
			"Scaled 1.4x",
			new Vector2(720f, 398f),
			Warm,
			0f,
			Vector2.Zero,
			1.4f,
			SpriteEffects.None,
			0f);

		var flipFont = _fontSystem.GetFont(24);
		batch.DrawString(
			flipFont,
			"Flipped horizontally",
			new Vector2(720f, 450f),
			Text,
			0f,
			Vector2.Zero,
			1f,
			SpriteEffects.FlipHorizontally,
			0f);

		var scaledMeasured = scaledFont.MeasureString("Scaled 1.4x");
		batch.DrawString(
			section,
			string.Create(
				CultureInfo.InvariantCulture,
				$"SCALED WIDTH {scaledMeasured.X * 1.4f:0.0} PX  /  UNSCALED {scaledMeasured.X:0.0} PX"),
			new Vector2(720f, 492f),
			Muted);

		batch.Draw(_pixel, new Rectangle(56, 590, Width - 112, 1), Line);

		var fontsUsed = new HashSet<DynamicSpriteFont>
		{
			title,
			subtitle,
			section,
			body,
			paragraph,
			measureFont,
			rotatedFont,
			scaledFont,
			flipFont,
			_fontSystem.GetFont(18),
			_fontSystem.GetFont(24),
			_fontSystem.GetFont(32),
			_fontSystem.GetFont(40),
			_fontSystem.GetFont(56),
		};

		var glyphs = 0;
		foreach (var font in fontsUsed)
		{
			glyphs += font.GlyphCount;
		}

		var status = string.Create(
			CultureInfo.InvariantCulture,
			$"Atlas pages: {_fontSystem.TextureCount}   |   Rasterized glyphs: {glyphs}   |   Supersampling: {title.SupersamplingFactor:0.#}x   |   Press Esc to exit   |   --screenshot <file.png>");

		batch.DrawString(body, status, new Vector2(56f, 616f), Muted);

		// A small footer panel makes the bottom edge feel intentional.
		batch.Draw(_pixel, new Rectangle(0, Height - 40, Width, 40), Panel);
		batch.DrawString(
			section,
			"Glyphs are added to the atlas the first time they are drawn, then reused for every later draw.",
			new Vector2(56f, Height - 26),
			Muted);
	}

	private void DrawSizeSample(SpriteBatch batch, float size, string text, float y)
	{
		var font = _fontSystem.GetFont(size);
		batch.DrawString(font, text, new Vector2(56f, y), Text);
	}

	private void DrawScreenshotFrame()
	{
		if (_renderTarget is null || _screenshotPath is null)
		{
			return;
		}

		GraphicsDevice.SetRenderTarget(_renderTarget);
		GraphicsDevice.Clear(Background);

		_spriteBatch.Begin(
			sortMode: SpriteSortMode.Deferred,
			blendState: BlendState.AlphaBlend,
			samplerState: SamplerState.LinearClamp);

		DrawScene(_spriteBatch);

		_spriteBatch.End();
		GraphicsDevice.SetRenderTarget(null);

		_frames++;
		if (_frames < 2)
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
		_renderTarget?.Dispose();
		_fontSystem?.Dispose();
		_pixel?.Dispose();
		_spriteBatch?.Dispose();

		base.UnloadContent();
	}

	private static string? ParseScreenshotPath(string[] args)
	{
		const string prefix = "--screenshot=";

		for (var i = 0; i < args.Length; i++)
		{
			if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				return Path.GetFullPath(args[i][prefix.Length..]);
			}

			if (string.Equals(args[i], "--screenshot", StringComparison.OrdinalIgnoreCase)
				&& i + 1 < args.Length)
			{
				return Path.GetFullPath(args[++i]);
			}
		}

		return null;
	}
}
