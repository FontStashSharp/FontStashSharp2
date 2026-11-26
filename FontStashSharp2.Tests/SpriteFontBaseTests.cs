using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FontStashSharp2.Tests;

public sealed class SpriteFontBaseTests
{
	[Fact]
	public void ExternalFont_CanOverrideDrawText()
	{
		SpriteFontBase font = new StubFont();
		font.LineSpacing = 20f;

		var measured = font.MeasureString("any text");

		Assert.Equal(10f, measured.X);
		Assert.Equal(20f, measured.Y);
		Assert.Equal(20f, font.LineSpacing);
	}

	[Fact]
	public void StringAndStringBuilderOverloads_DispatchToSpanHook()
	{
		var font = new StubFont();

		font.DrawText((SpriteBatch)null!, "alpha", Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
		Assert.Equal("alpha", font.LastDrawnText);

		font.DrawText(
			(SpriteBatch)null!,
			new StringBuilder("beta"),
			Vector2.Zero,
			Color.White,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.None,
			0f);
		Assert.Equal("beta", font.LastDrawnText);

		font.DrawText((SpriteBatch)null!, "gamma".AsSpan(), Vector2.Zero, Color.White);
		Assert.Equal("gamma", font.LastDrawnText);

		font.DrawText((SpriteBatch)null!, "delta", Vector2.Zero, Color.White);
		Assert.Equal("delta", font.LastDrawnText);

		font.DrawText((SpriteBatch)null!, new StringBuilder("epsilon"), Vector2.Zero, Color.White);
		Assert.Equal("epsilon", font.LastDrawnText);
	}

	[Fact]
	public void CustomRendererOverloads_DispatchToRendererHook()
	{
		var font = new StubFont();
		var renderer = new StubRenderer();

		font.DrawText(
			renderer,
			"alpha",
			Vector2.Zero,
			Color.White,
			0.5f,
			Vector2.Zero,
			new Vector2(2f),
			SpriteEffects.FlipHorizontally,
			0.25f);

		Assert.Equal("alpha", font.LastDrawnText);
		Assert.Equal(0.5f, font.LastRotation);
		Assert.Equal(new Vector2(2f), font.LastScale);
		Assert.Equal(SpriteEffects.FlipHorizontally, font.LastEffects);
		Assert.Equal(0.25f, font.LastLayerDepth);

		font.DrawText(renderer, "beta", Vector2.Zero, Color.White);
		Assert.Equal("beta", font.LastDrawnText);
		Assert.Equal(0f, font.LastRotation);
		Assert.Equal(Vector2.One, font.LastScale);
		Assert.Equal(SpriteEffects.None, font.LastEffects);
		Assert.Equal(0f, font.LastLayerDepth);

		font.DrawText(renderer, new StringBuilder("gamma"), Vector2.Zero, Color.White);
		Assert.Equal("gamma", font.LastDrawnText);
	}

	[Fact]
	public void EffectArguments_AreForwardedToRendererHook()
	{
		var font = new StubFont();
		var renderer = new StubRenderer();

		font.DrawText(
			renderer,
			"alpha",
			Vector2.Zero,
			Color.White,
			effect: TextEffect.Shadow,
			effectAmount: 3);

		Assert.Equal(TextEffect.Shadow, font.LastEffect);
		Assert.Equal(3, font.LastEffectAmount);

		font.DrawText((SpriteBatch)null!, "beta", Vector2.Zero, Color.White);
		Assert.Equal(TextEffect.None, font.LastEffect);
		Assert.Equal(1, font.LastEffectAmount);
	}

	[Fact]
	public void VertexRendererOverloads_DispatchToQuadHook()
	{
		var font = new StubFont();
		var renderer = new StubVertexRenderer();

		font.DrawText(
			renderer,
			"alpha",
			Vector2.Zero,
			Color.White,
			0.5f,
			Vector2.Zero,
			new Vector2(2f),
			SpriteEffects.FlipVertically,
			0.25f);

		Assert.Equal("alpha", font.LastDrawnText);
		Assert.Equal(0.5f, font.LastRotation);
		Assert.Equal(new Vector2(2f), font.LastScale);
		Assert.Equal(SpriteEffects.FlipVertically, font.LastEffects);
		Assert.Equal(0.25f, font.LastLayerDepth);

		font.DrawText(renderer, "beta".AsSpan(), Vector2.Zero, Color.White);
		Assert.Equal("beta", font.LastDrawnText);
		Assert.Equal(0f, font.LastRotation);
		Assert.Equal(Vector2.One, font.LastScale);
		Assert.Equal(SpriteEffects.None, font.LastEffects);

		font.DrawText(renderer, new StringBuilder("gamma"), Vector2.Zero, Color.White);
		Assert.Equal("gamma", font.LastDrawnText);
	}

	[Fact]
	public void DrawTextOverloads_RejectNullText()
	{
		var font = new StubFont();
		var renderer = new StubRenderer();
		var vertexRenderer = new StubVertexRenderer();

		Assert.Throws<ArgumentNullException>(
			() => font.DrawText((SpriteBatch)null!, (string)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText((SpriteBatch)null!, (StringBuilder)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText((SpriteBatch)null!, (string)null!, Vector2.Zero, Color.White));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText((SpriteBatch)null!, (StringBuilder)null!, Vector2.Zero, Color.White));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(renderer, (string)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(renderer, (StringBuilder)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(renderer, (string)null!, Vector2.Zero, Color.White));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(renderer, (StringBuilder)null!, Vector2.Zero, Color.White));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(vertexRenderer, (string)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(vertexRenderer, (StringBuilder)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(vertexRenderer, (string)null!, Vector2.Zero, Color.White));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(vertexRenderer, (StringBuilder)null!, Vector2.Zero, Color.White));
	}

	private sealed class StubFont : SpriteFontBase
	{
		public override float FontSize => 16f;

		public override float LineSpacing { get; set; } = 16f;

		public string? LastDrawnText { get; private set; }

		public float LastRotation { get; private set; }

		public Vector2 LastScale { get; private set; }

		public SpriteEffects LastEffects { get; private set; }

		public float LastLayerDepth { get; private set; }

		public TextEffect LastEffect { get; private set; }

		public int LastEffectAmount { get; private set; }

		public override Vector2 MeasureString(string text)
		{
			return new Vector2(10f, LineSpacing);
		}

		public override Vector2 MeasureString(ReadOnlySpan<char> text)
		{
			return new Vector2(10f, LineSpacing);
		}

		public override Vector2 MeasureString(StringBuilder text)
		{
			return new Vector2(10f, LineSpacing);
		}

		public override void DrawText(
			SpriteBatch spriteBatch,
			ReadOnlySpan<char> text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth,
			TextEffect effect,
			int effectAmount)
		{
			LastDrawnText = text.ToString();
			LastEffect = effect;
			LastEffectAmount = effectAmount;
		}

		public override void DrawText(
			IFontStashRenderer renderer,
			ReadOnlySpan<char> text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth,
			TextEffect effect,
			int effectAmount)
		{
			LastDrawnText = text.ToString();
			LastRotation = rotation;
			LastScale = scale;
			LastEffects = effects;
			LastLayerDepth = layerDepth;
			LastEffect = effect;
			LastEffectAmount = effectAmount;
		}

		public override void DrawText(
			IFontStashRenderer2 renderer,
			ReadOnlySpan<char> text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth,
			TextEffect effect,
			int effectAmount)
		{
			LastDrawnText = text.ToString();
			LastRotation = rotation;
			LastScale = scale;
			LastEffects = effects;
			LastLayerDepth = layerDepth;
			LastEffect = effect;
			LastEffectAmount = effectAmount;
		}
	}

	private sealed class StubRenderer : IFontStashRenderer
	{
		public GraphicsDevice GraphicsDevice => null!;

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
		}
	}

	private sealed class StubVertexRenderer : IFontStashRenderer2
	{
		public GraphicsDevice GraphicsDevice => null!;

		public void DrawQuad(
			Texture2D texture,
			ref VertexPositionColorTexture topLeft,
			ref VertexPositionColorTexture topRight,
			ref VertexPositionColorTexture bottomLeft,
			ref VertexPositionColorTexture bottomRight)
		{
		}
	}
}
