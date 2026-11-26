using System.Globalization;
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StaticFontSample;

/// <summary>
/// Demonstrates <see cref="StaticSpriteFont"/> with an AngelCode BMFont atlas loaded from a text
/// descriptor and a PNG page image.
/// </summary>
public sealed class StaticFontGame : Game
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
	private StaticSpriteFont _bodyFont = null!;
	private StaticSpriteFont _displayFont = null!;
	private Texture2D _pixel = null!;
	private RenderTarget2D? _renderTarget;
	private int _preloadedPages;
	private int _frames;

	public StaticFontGame(string? screenshotPath)
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
		Window.Title = "FontStashSharp2 - Static BMFont";
	}

	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);

		_pixel = new Texture2D(GraphicsDevice, 1, 1);
		_pixel.SetData(new[] { Color.White });

		var content = Path.Combine(AppContext.BaseDirectory, "Content");
		_bodyFont = new StaticSpriteFont(
			Path.Combine(content, "ABeeZee-18.fnt"),
			GlyphAlphaMode.Premultiplied);
		_displayFont = new StaticSpriteFont(
			Path.Combine(content, "ABeeZee-40.fnt"),
			GlyphAlphaMode.Premultiplied);

		// Upload both page images now instead of during the first visible frame.
		_preloadedPages = _bodyFont.PreloadTextures(GraphicsDevice)
			+ _displayFont.PreloadTextures(GraphicsDevice);

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
		var measured = _displayFont.MeasureString("BMFont");

		batch.DrawString(
			_displayFont,
			"Static BMFont",
			new Vector2(56f, 22f),
			Text,
			0f,
			Vector2.Zero,
			1.1f,
			SpriteEffects.None,
			0f);
		batch.DrawString(
			_bodyFont,
			"AngelCode BMFont descriptor + PNG page, parsed with SharpFNT and StbImageNext",
			new Vector2(58f, 94f),
			Muted);
		batch.Draw(_pixel, new Rectangle(56, 126, Width - 112, 1), Line);

		batch.DrawString(_bodyFont, "DESCRIPTOR", new Vector2(56f, 148f), Accent);
		DrawBodyLine(batch, $"Face \"{_displayFont.Name}\"", 176f);
		DrawBodyLine(
			batch,
			string.Create(
				CultureInfo.InvariantCulture,
				$"FontSize {_displayFont.FontSize:0} px / body {_bodyFont.FontSize:0} px"),
			202f);
		DrawBodyLine(
			batch,
			string.Create(
				CultureInfo.InvariantCulture,
				$"LineSpacing {_displayFont.LineSpacing:0} px / body {_bodyFont.LineSpacing:0} px"),
			228f);
		DrawBodyLine(
			batch,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Ascent {_displayFont.Ascent:0} px   Descent {_displayFont.Descent:0} px"),
			254f);
		DrawBodyLine(
			batch,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Measured \"BMFont\": {measured.X:0.0} x {measured.Y:0.0} px"),
			280f);
		DrawBodyLine(
			batch,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Page textures preloaded: {_preloadedPages}   |   Alpha: {_displayFont.AlphaMode}"),
			306f);

		batch.DrawString(_bodyFont, "BASE SIZE AND KERNING", new Vector2(56f, 348f), Accent);
		batch.DrawString(_displayFont, "Handgloves & kerning", new Vector2(56f, 376f), Text);
		batch.DrawString(_displayFont, "AV To WA LT 123", new Vector2(56f, 426f), Warm);

		batch.DrawString(_bodyFont, "MULTILINE + SPACING", new Vector2(56f, 500f), Accent);
		batch.DrawString(
			_bodyFont,
			"Two lines with\nextra line spacing.",
			new Vector2(56f, 528f),
			Muted,
			characterSpacing: 0.5f,
			lineSpacing: 8f);

		batch.DrawString(_bodyFont, "TRANSFORMS", new Vector2(700f, 148f), Accent);
		batch.Draw(_pixel, new Rectangle(700, 172, Width - 756, 1), Line);

		batch.DrawString(
			_displayFont,
			"Scaled 1.4x",
			new Vector2(700f, 186f),
			Text,
			0f,
			Vector2.Zero,
			1.4f,
			SpriteEffects.None,
			0f);

		batch.DrawString(
			_displayFont,
			"Stroke 2 px",
			new Vector2(700f, 258f),
			Accent,
			effect: TextEffectOptions.Stroke(2, Text));

		batch.DrawString(
			_displayFont,
			"Underlined",
			new Vector2(700f, 322f),
			Text,
			decoration: TextDecorationOptions.Underline(2f, Warm));

		batch.DrawString(
			_displayFont,
			"Struck through",
			new Vector2(700f, 386f),
			Warm,
			decoration: TextDecorationOptions.Strikethrough(2f, Accent));

		var rotatedText = "Rotated";
		var rotatedSize = _displayFont.MeasureString(rotatedText);
		batch.DrawString(
			_displayFont,
			rotatedText,
			new Vector2(930f, 500f),
			Text,
			-0.15f,
			rotatedSize * 0.5f,
			1f,
			SpriteEffects.None,
			0f,
			effect: TextEffectOptions.Shadow(3, new Color(20, 90, 80)));

		batch.DrawString(
			_displayFont,
			"Flipped",
			new Vector2(700f, 552f),
			Accent,
			0f,
			Vector2.Zero,
			1f,
			SpriteEffects.FlipHorizontally,
			0f,
			decoration: TextDecorationOptions.Underline(2f, Text));

		batch.Draw(_pixel, new Rectangle(56, 618, Width - 112, 1), Line);
		batch.DrawString(
			_bodyFont,
			"MeasureString is device-free; PreloadTextures uploads the BMFont page images ahead of time.",
			new Vector2(56f, 638f),
			Muted);

		batch.Draw(_pixel, new Rectangle(0, Height - 40, Width, 40), Panel);
		batch.DrawString(
			_bodyFont,
			"StaticSpriteFont implements the same SpriteBatch and custom renderer draw hooks as the dynamic font.",
			new Vector2(56f, Height - 27f),
			Muted);
	}

	private void DrawBodyLine(SpriteBatch batch, string text, float y)
	{
		batch.DrawString(_bodyFont, text, new Vector2(80f, y), Muted);
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
		_displayFont?.Dispose();
		_bodyFont?.Dispose();
		_pixel?.Dispose();
		_spriteBatch?.Dispose();

		base.UnloadContent();
	}
}
