using System.Globalization;
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SupersamplingSample;

/// <summary>
/// Compares glyph atlases rasterized at 1x, 2x, and 4x while drawing the same 24px font at
/// 1.0x, 1.5x, 2.0x, and 3.0x display scales.
/// </summary>
internal sealed class SupersamplingGame : Game
{
	private const int Width = 1280;
	private const int Height = 720;
	private const float SampleFontSize = 24f;
	private const string SampleText = "Sharp";

	private static readonly float[] Factors = { 1f, 2f, 4f };
	private static readonly float[] DrawScales = { 1f, 1.5f, 2f, 3f };
	private static readonly Color Background = new(16, 18, 24);
	private static readonly Color Panel = new(24, 27, 36);
	private static readonly Color Line = new(48, 53, 66);
	private static readonly Color Text = new(238, 241, 247);
	private static readonly Color Muted = new(146, 154, 171);
	private static readonly Color Accent = new(108, 214, 190);
	private static readonly Color Warm = new(255, 190, 120);
	private static readonly Color HighFactor = new(140, 200, 255);

	private readonly GraphicsDeviceManager _graphics;
	private readonly string? _screenshotPath;
	private readonly List<DemoFont> _demoFonts = new();

	private SpriteBatch _batch = null!;
	private Texture2D _pixel = null!;
	private FontSystem _uiFonts = null!;
	private RenderTarget2D? _renderTarget;
	private int _screenshotFrames;

	public SupersamplingGame(string? screenshotPath)
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
		Window.Title = "FontStashSharp2 - Supersampling";
	}

	protected override void LoadContent()
	{
		_batch = new SpriteBatch(GraphicsDevice);

		_pixel = new Texture2D(GraphicsDevice, 1, 1);
		_pixel.SetData(new[] { Color.White });

		var fontData = ReadFontData();

		_uiFonts = CreateFontSystem(2f);
		_uiFonts.AddFont(fontData, name: "ABeeZee");

		foreach (var factor in Factors)
		{
			var fontSystem = CreateFontSystem(factor);
			fontSystem.AddFont(fontData, name: "ABeeZee");
			_demoFonts.Add(new DemoFont(factor, fontSystem, fontSystem.GetFont(SampleFontSize)));
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
		var title = _uiFonts.GetFont(40);
		var subtitle = _uiFonts.GetFont(18);
		var section = _uiFonts.GetFont(14);
		var body = _uiFonts.GetFont(16);
		var tableHeader = _uiFonts.GetFont(18);
		var small = _uiFonts.GetFont(13);

		batch.DrawString(title, "Supersampled text scaling", new Vector2(56f, 28f), Text);
		batch.DrawString(
			subtitle,
			"Same 24px font in every cell. Rows use different atlas resolutions; columns draw at different scales.",
			new Vector2(58f, 82f),
			Muted);
		batch.Draw(_pixel, new Rectangle(56, 116, Width - 112, 1), Line);

		batch.DrawString(section, "DRAW SCALE", new Vector2(56f, 144f), Accent);
		batch.DrawString(
			body,
			"Glyphs are rasterized at FontSize x SupersamplingFactor and drawn at scale / factor.",
			new Vector2(56f, 168f),
			Muted);

		var columns = new[] { 240f, 490f, 740f, 990f };
		for (var i = 0; i < DrawScales.Length; i++)
		{
			batch.DrawString(
				tableHeader,
				string.Create(CultureInfo.InvariantCulture, $"{DrawScales[i]:0.0}x"),
				new Vector2(columns[i], 196f),
				Muted);
		}

		batch.Draw(_pixel, new Rectangle(56, 228, Width - 112, 1), Line);

		var rowY = 250f;
		foreach (var demo in _demoFonts)
		{
			var isDefault = MathF.Abs(demo.Factor - 2f) < 0.001f;
			var labelColor = isDefault ? Accent : demo.Factor > 2f ? HighFactor : Text;

			batch.DrawString(
				body,
				string.Create(
					CultureInfo.InvariantCulture,
					$"{demo.Factor:0}x atlas{(isDefault ? " (default)" : string.Empty)}"),
				new Vector2(56f, rowY + 3f),
				labelColor);

			for (var i = 0; i < DrawScales.Length; i++)
			{
				batch.DrawString(
					demo.Font,
					SampleText,
					new Vector2(columns[i], rowY),
					Text,
					0f,
					Vector2.Zero,
					DrawScales[i],
					SpriteEffects.None,
					0f);
			}

			rowY += 94f;
		}

		batch.Draw(_pixel, new Rectangle(56, 548, Width - 112, 1), Line);

		batch.DrawString(section, "ATLAS USAGE", new Vector2(56f, 568f), Accent);
		var statsY = 596f;
		foreach (var demo in _demoFonts)
		{
			batch.DrawString(
				small,
				string.Create(
					CultureInfo.InvariantCulture,
					$"{demo.Factor:0}x: {demo.FontSystem.TextureCount} page(s), {demo.Font.GlyphCount} glyphs, ~{demo.Factor * demo.Factor:0}x atlas pixels"),
				new Vector2(56f, statsY),
				Muted);
			statsY += 20f;
		}

		batch.DrawString(
			small,
			"Atlas memory grows roughly with factor squared. Use LinearClamp; point sampling aliases the downscale.",
			new Vector2(400f, 596f),
			Warm);

		batch.Draw(_pixel, new Rectangle(0, Height - 40, Width, 40), Panel);
		batch.DrawString(
			body,
			"Press Esc to exit.   --screenshot <path> saves this screen.",
			new Vector2(56f, Height - 26),
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
		foreach (var demo in _demoFonts)
		{
			demo.FontSystem.Dispose();
		}

		_uiFonts?.Dispose();
		_renderTarget?.Dispose();
		_pixel?.Dispose();
		_batch?.Dispose();

		base.UnloadContent();
	}

	private FontSystem CreateFontSystem(float supersamplingFactor)
	{
		return new FontSystem(
			new FontSystemSettings
			{
				TextureWidth = 1024,
				TextureHeight = 1024,
				GlyphPadding = 1,
				SupersamplingFactor = supersamplingFactor,
				UseKerning = true,
			});
	}

	private static byte[] ReadFontData()
	{
		var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "ABeeZee-Regular.ttf");
		return File.ReadAllBytes(fontPath);
	}

	private sealed record DemoFont(
		float Factor,
		FontSystem FontSystem,
		DynamicSpriteFont Font);
}
