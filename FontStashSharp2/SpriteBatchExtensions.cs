using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FontStashSharp2;

/// <summary>
/// <see cref="SpriteBatch"/> drawing extensions for <see cref="SpriteFontBase"/> fonts.
/// </summary>
/// <remarks>
/// The overload set mirrors <see cref="SpriteBatch"/>'s SpriteFont drawing methods, so swapping a
/// <see cref="SpriteFont"/> for a <see cref="DynamicSpriteFont"/> only requires changing the font argument.
/// </remarks>
public static class SpriteBatchExtensions
{
	/// <summary>
	/// Draws a string at a position.
	/// </summary>
	public static void DrawString(
		this SpriteBatch spriteBatch,
		SpriteFontBase font,
		string text,
		Vector2 position,
		Color color)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);
		ArgumentNullException.ThrowIfNull(font);
		ArgumentNullException.ThrowIfNull(text);

		font.DrawText(spriteBatch, text.AsSpan(), position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
	}

	/// <summary>
	/// Draws a string with rotation, origin, uniform scale, sprite effects, and layer depth.
	/// </summary>
	public static void DrawString(
		this SpriteBatch spriteBatch,
		SpriteFontBase font,
		string text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		float scale,
		SpriteEffects effects,
		float layerDepth)
	{
		DrawString(spriteBatch, font, text, position, color, rotation, origin, new Vector2(scale), effects, layerDepth);
	}

	/// <summary>
	/// Draws a string with rotation, origin, non-uniform scale, sprite effects, and layer depth.
	/// </summary>
	public static void DrawString(
		this SpriteBatch spriteBatch,
		SpriteFontBase font,
		string text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);
		ArgumentNullException.ThrowIfNull(font);
		ArgumentNullException.ThrowIfNull(text);

		font.DrawText(spriteBatch, text.AsSpan(), position, color, rotation, origin, scale, effects, layerDepth);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> at a position.
	/// </summary>
	public static void DrawString(
		this SpriteBatch spriteBatch,
		SpriteFontBase font,
		StringBuilder text,
		Vector2 position,
		Color color)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);
		ArgumentNullException.ThrowIfNull(font);
		ArgumentNullException.ThrowIfNull(text);

		font.DrawText(spriteBatch, text.ToString().AsSpan(), position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> with rotation, origin, uniform scale,
	/// sprite effects, and layer depth.
	/// </summary>
	public static void DrawString(
		this SpriteBatch spriteBatch,
		SpriteFontBase font,
		StringBuilder text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		float scale,
		SpriteEffects effects,
		float layerDepth)
	{
		DrawString(
			spriteBatch,
			font,
			text,
			position,
			color,
			rotation,
			origin,
			new Vector2(scale),
			effects,
			layerDepth);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> with rotation, origin, non-uniform scale,
	/// sprite effects, and layer depth.
	/// </summary>
	public static void DrawString(
		this SpriteBatch spriteBatch,
		SpriteFontBase font,
		StringBuilder text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);
		ArgumentNullException.ThrowIfNull(font);
		ArgumentNullException.ThrowIfNull(text);

		font.DrawText(spriteBatch, text.ToString().AsSpan(), position, color, rotation, origin, scale, effects, layerDepth);
	}
}
