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

		font.DrawText(null!, "alpha", Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
		Assert.Equal("alpha", font.LastDrawnText);

		font.DrawText(
			null!,
			new StringBuilder("beta"),
			Vector2.Zero,
			Color.White,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.None,
			0f);
		Assert.Equal("beta", font.LastDrawnText);

		font.DrawText(null!, "gamma".AsSpan(), Vector2.Zero, Color.White);
		Assert.Equal("gamma", font.LastDrawnText);

		font.DrawText(null!, "delta", Vector2.Zero, Color.White);
		Assert.Equal("delta", font.LastDrawnText);

		font.DrawText(null!, new StringBuilder("epsilon"), Vector2.Zero, Color.White);
		Assert.Equal("epsilon", font.LastDrawnText);
	}

	[Fact]
	public void DrawTextOverloads_RejectNullText()
	{
		var font = new StubFont();

		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(null!, (string)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(null!, (StringBuilder)null!, Vector2.Zero, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(null!, (string)null!, Vector2.Zero, Color.White));
		Assert.Throws<ArgumentNullException>(
			() => font.DrawText(null!, (StringBuilder)null!, Vector2.Zero, Color.White));
	}

	private sealed class StubFont : SpriteFontBase
	{
		public override float FontSize => 16f;

		public override float LineSpacing { get; set; } = 16f;

		public string? LastDrawnText { get; private set; }

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
			float layerDepth)
		{
			LastDrawnText = text.ToString();
		}
	}
}
