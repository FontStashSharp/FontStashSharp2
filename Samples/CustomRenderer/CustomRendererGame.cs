using System.Globalization;
using System.Text;
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CustomRendererSample;

/// <summary>
/// Demonstrates both custom renderer interfaces: glyph quads through <see cref="IFontStashRenderer"/>
/// and vertex quads through <see cref="IFontStashRenderer2"/>.
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
	private BasicEffect _vertexEffect = null!;
	private VertexFontRenderer _vertexRenderer = null!;
	private RenderTarget2D? _renderTarget;
	private int _screenshotFrames;
	private int _lastRenderer1Calls;
	private int _lastRenderer2Calls;

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
		Window.Title = "FontStashSharp2 - Custom renderers";
	}

	protected override void LoadContent()
	{
		_batch = new SpriteBatch(GraphicsDevice);

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

		_vertexEffect = new BasicEffect(GraphicsDevice)
		{
			World = Matrix.Identity,
			View = Matrix.Identity,
			Projection = Matrix.CreateOrthographicOffCenter(0, Width, Height, 0, 0, 1),
			VertexColorEnabled = true,
			TextureEnabled = true,
		};
		_vertexRenderer = new VertexFontRenderer(GraphicsDevice, _vertexEffect);

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

		DrawFrame();

		base.Draw(gameTime);
	}

	private void DrawFrame()
	{
		_lastRenderer1Calls = _shadowRenderer.DrawCalls + _plainRenderer.DrawCalls;
		_lastRenderer2Calls = _vertexRenderer.QuadCount;
		_shadowRenderer.ResetFrame();
		_plainRenderer.ResetFrame();
		_vertexRenderer.ResetFrame();

		// Vertex text is drawn directly to the device first; SpriteBatch text then layers on top.
		_vertexRenderer.Prepare();
		DrawVertexText();

		_batch.Begin(
			sortMode: SpriteSortMode.Deferred,
			blendState: BlendState.AlphaBlend,
			samplerState: SamplerState.LinearClamp);

		DrawSpriteBatchText(_batch);

		_batch.End();
	}

	private void DrawVertexText()
	{
		var font = _fontSystem.GetFont(28);

		font.DrawText(_vertexRenderer, "Vertex quads", new Vector2(860f, 174f), Text);

		const string transformText = "Rotated + scaled";
		font.DrawText(
			_vertexRenderer,
			transformText,
			new Vector2(1010f, 260f),
			HighColor,
			-0.08f,
			font.MeasureString(transformText) * 0.5f,
			new Vector2(1.2f),
			SpriteEffects.None,
			0f);

		font.DrawText(
			_vertexRenderer,
			"Flipped",
			new Vector2(860f, 354f),
			Warm,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.FlipHorizontally,
			0f);
	}

	private void DrawSpriteBatchText(SpriteBatch batch)
	{
		var title = _fontSystem.GetFont(44);
		var subtitle = _fontSystem.GetFont(18);
		var section = _fontSystem.GetFont(14);
		var label = _fontSystem.GetFont(15);
		var demo = _fontSystem.GetFont(28);
		var footer = _fontSystem.GetFont(15);

		title.DrawText(_shadowRenderer, "Custom renderers", new Vector2(56f, 28f), Text);
		subtitle.DrawText(
			_plainRenderer,
			"Glyph quads through IFontStashRenderer and four-vertex quads through IFontStashRenderer2.",
			new Vector2(58f, 86f),
			Muted);
		batch.Draw(_pixel, new Rectangle(56, 122, Width - 112, 1), Line);

		section.DrawText(_plainRenderer, "IFONTSTASHRENDERER (GLYPH QUADS)", new Vector2(56f, 146f), Accent);
		section.DrawText(_plainRenderer, "IFONTSTASHRENDERER2 (VERTEX QUADS)", new Vector2(680f, 146f), Accent);

		label.DrawText(_plainRenderer, "string", new Vector2(56f, 183f), Muted);
		demo.DrawText(_shadowRenderer, "Simple string overload", new Vector2(220f, 174f), Text);

		label.DrawText(_plainRenderer, "StringBuilder", new Vector2(56f, 243f), Muted);
		demo.DrawText(_shadowRenderer, _builderText, new Vector2(220f, 234f), Text);

		label.DrawText(_plainRenderer, "ReadOnlySpan<char>", new Vector2(56f, 303f), Muted);
		demo.DrawText(_shadowRenderer, SpanText.AsSpan(), new Vector2(220f, 294f), Text);

		label.DrawText(_plainRenderer, "full transform", new Vector2(56f, 363f), Muted);
		const string transformText = "Rotation + scale";
		demo.DrawText(
			_shadowRenderer,
			transformText,
			new Vector2(400f, 370f),
			Warm,
			-0.05f,
			demo.MeasureString(transformText) * 0.5f,
			new Vector2(1.1f),
			SpriteEffects.None,
			0f);

		label.DrawText(_plainRenderer, "string", new Vector2(680f, 183f), Muted);
		label.DrawText(_plainRenderer, "full transform", new Vector2(680f, 243f), Muted);
		label.DrawText(_plainRenderer, "SpriteEffects", new Vector2(680f, 363f), Muted);

		batch.Draw(_pixel, new Rectangle(56, 430, Width - 112, 1), Line);

		section.DrawText(_plainRenderer, "RENDERER CONTRACT", new Vector2(56f, 450f), Accent);
		label.DrawText(
			_plainRenderer,
			"IFontStashRenderer receives one pre-transformed glyph quad per call; this renderer draws each glyph twice to add a drop shadow.",
			new Vector2(56f, 478f),
			Muted);
		label.DrawText(
			_plainRenderer,
			"IFontStashRenderer2 receives four screen-space vertices with layer depth in Z and SpriteEffects already baked into the UVs.",
			new Vector2(56f, 500f),
			Muted);
		label.DrawText(
			_plainRenderer,
			"Both paths share layout, kerning, fallback, supersampling, rotation, scale, origin, and effects data.",
			new Vector2(56f, 522f),
			Muted);

		batch.Draw(_pixel, new Rectangle(0, Height - 40, Width, 40), Panel);
		footer.DrawText(
			_plainRenderer,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Renderer1 quads: {_lastRenderer1Calls}   |   Renderer2 quads: {_lastRenderer2Calls}   |   Supersampling: 2x   |   Press Esc to exit   |   --screenshot <file.png>"),
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

		DrawFrame();

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
		_vertexEffect?.Dispose();
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

	private sealed class VertexFontRenderer : IFontStashRenderer2
	{
		private static readonly short[] Indices = { 0, 1, 2, 1, 3, 2 };

		private readonly BasicEffect _effect;

		public VertexFontRenderer(GraphicsDevice graphicsDevice, BasicEffect effect)
		{
			GraphicsDevice = graphicsDevice;
			_effect = effect;
		}

		public GraphicsDevice GraphicsDevice { get; }

		public int QuadCount { get; private set; }

		public void ResetFrame()
		{
			QuadCount = 0;
		}

		public void Prepare()
		{
			GraphicsDevice.BlendState = BlendState.AlphaBlend;
			GraphicsDevice.DepthStencilState = DepthStencilState.None;
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
		}

		public void DrawQuad(
			Texture2D texture,
			ref VertexPositionColorTexture topLeft,
			ref VertexPositionColorTexture topRight,
			ref VertexPositionColorTexture bottomLeft,
			ref VertexPositionColorTexture bottomRight)
		{
			var vertices = new[]
			{
				topLeft,
				topRight,
				bottomLeft,
				bottomRight,
			};

			_effect.Texture = texture;
			_effect.CurrentTechnique.Passes[0].Apply();
			GraphicsDevice.DrawUserIndexedPrimitives(
				PrimitiveType.TriangleList,
				vertices,
				0,
				4,
				Indices,
				0,
				2);
			QuadCount++;
		}
	}
}
