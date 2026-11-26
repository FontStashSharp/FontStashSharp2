using System.Globalization;
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TextEffectsSample;

/// <summary>
/// Demonstrates <see cref="TextEffect.None"/>, <see cref="TextEffect.Stroke"/>, and
/// <see cref="TextEffect.Shadow"/> through the SpriteBatch draw overloads.
/// </summary>
public sealed class TextEffectsGame : Game
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

	public TextEffectsGame(string? screenshotPath)
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
		Window.Title = "FontStashSharp2 - Text effects";
	}

	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);

		_pixel = new Texture2D(GraphicsDevice, 1, 1);
		_pixel.SetData(new[] { Color.White });

		_fontSystem = new FontSystem(
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
		var title = _fontSystem.GetFont(48f);
		var subtitle = _fontSystem.GetFont(18f);
		var section = _fontSystem.GetFont(14f);
		var label = _fontSystem.GetFont(14f);
		var demo = _fontSystem.GetFont(42f);
		var small = _fontSystem.GetFont(16f);

		batch.DrawString(title, "Text effects", new Vector2(56f, 28f), Text);
		batch.DrawString(
			subtitle,
			"TextEffect.None, Stroke, and Shadow through SpriteBatch.DrawString",
			new Vector2(58f, 92f),
			Muted);
		batch.Draw(_pixel, new Rectangle(56, 124, Width - 112, 1), Line);

		batch.DrawString(section, "EFFECTS", new Vector2(56f, 146f), Accent);
		DrawEffectRow(batch, label, demo, 176f, "None", "Plain text", Text, TextEffect.None, 0);
		DrawEffectRow(batch, label, demo, 238f, "Stroke / 1", "Stroke 1 px", Text, TextEffect.Stroke, 1);
		DrawEffectRow(batch, label, demo, 300f, "Stroke / 2", "Stroke 2 px", Accent, TextEffect.Stroke, 2);
		DrawEffectRow(batch, label, demo, 362f, "Shadow / 2", "Shadow 2 px", Text, TextEffect.Shadow, 2);
		DrawEffectRow(batch, label, demo, 424f, "Shadow / 6", "Shadow 6 px", Warm, TextEffect.Shadow, 6);

		batch.DrawString(section, "WITH TRANSFORMS", new Vector2(720f, 146f), Accent);
		batch.Draw(_pixel, new Rectangle(720, 170, Width - 776, 1), Line);

		var rotatedFont = _fontSystem.GetFont(30f);
		const string rotatedText = "Rotated stroke";
		var rotatedSize = rotatedFont.MeasureString(rotatedText);
		batch.DrawString(
			rotatedFont,
			rotatedText,
			new Vector2(980f, 250f),
			Accent,
			-0.12f,
			rotatedSize * 0.5f,
			1f,
			SpriteEffects.None,
			0f,
			TextEffect.Stroke,
			2);

		var scaledFont = _fontSystem.GetFont(24f);
		batch.DrawString(
			scaledFont,
			"Scaled shadow",
			new Vector2(720f, 340f),
			Warm,
			0f,
			Vector2.Zero,
			1.5f,
			SpriteEffects.None,
			0f,
			TextEffect.Shadow,
			3);

		batch.DrawString(
			small,
			"Effects are submitted as extra glyph passes before the main text.",
			new Vector2(720f, 420f),
			Muted);

		var flippedFont = _fontSystem.GetFont(24f);
		batch.DrawString(
			flippedFont,
			"Flipped stroke",
			new Vector2(720f, 466f),
			Text,
			0f,
			Vector2.Zero,
			1f,
			SpriteEffects.FlipHorizontally,
			0f,
			TextEffect.Stroke,
			1);

		var largeFont = _fontSystem.GetFont(58f);
		batch.DrawString(
			largeFont,
			"Effects!",
			new Vector2(720f, 520f),
			Accent,
			0f,
			Vector2.Zero,
			1f,
			SpriteEffects.None,
			0f,
			TextEffect.Shadow,
			5);

		batch.Draw(_pixel, new Rectangle(56, 590, Width - 112, 1), Line);
		batch.DrawString(
			small,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Atlas pages: {_fontSystem.TextureCount}   |   Rasterized glyphs: {demo.GlyphCount}   |   Press Esc to exit   |   --screenshot <file.png>"),
			new Vector2(56f, 610f),
			Muted);

		batch.Draw(_pixel, new Rectangle(0, Height - 40, Width, 40), Panel);
		batch.DrawString(
			small,
			"effectAmount is measured in layout pixels; 0 disables the effect.",
			new Vector2(56f, Height - 27f),
			Muted);
	}

	private static void DrawEffectRow(
		SpriteBatch batch,
		DynamicSpriteFont labelFont,
		DynamicSpriteFont demoFont,
		float y,
		string label,
		string text,
		Color color,
		TextEffect effect,
		int effectAmount)
	{
		batch.DrawString(labelFont, label, new Vector2(56f, y + 8f), Muted);
		batch.DrawString(
			demoFont,
			text,
			new Vector2(220f, y),
			color,
			effect: effect,
			effectAmount: effectAmount);
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
}
