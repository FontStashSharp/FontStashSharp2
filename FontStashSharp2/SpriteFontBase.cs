using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FontStashSharp2;

/// <summary>
/// Base type for fonts that can be measured and drawn through <see cref="SpriteBatch"/>.
/// </summary>
/// <remarks>
/// Instances are created by <see cref="FontSystem.GetFont(float)"/>. The drawing entry points live in
/// <see cref="SpriteBatchExtensions"/> and mirror the <see cref="SpriteBatch.DrawString(SpriteFont, string, Vector2, Color)"/>
/// overload set.
/// </remarks>
public abstract class SpriteFontBase
{
	/// <summary>
	/// Gets the font size in pixels (the em size).
	/// </summary>
	public abstract float FontSize { get; }

	/// <summary>
	/// Gets or sets the distance between consecutive baselines, in pixels.
	/// </summary>
	public abstract float LineSpacing { get; set; }

	/// <summary>
	/// Measures a string, including line breaks, kerning, and <see cref="LineSpacing"/>.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public abstract Vector2 MeasureString(string text);

	/// <summary>
	/// Measures a span of characters.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public abstract Vector2 MeasureString(ReadOnlySpan<char> text);

	/// <summary>
	/// Measures the contents of a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public abstract Vector2 MeasureString(StringBuilder text);

	internal abstract void DrawText(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth);
}
