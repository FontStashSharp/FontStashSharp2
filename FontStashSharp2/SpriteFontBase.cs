using System.Text;
#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FontStashSharp2;

#if MONOGAME
/// <summary>
/// Base type for fonts that can be measured and drawn through <see cref="SpriteBatch"/> or a custom
/// renderer.
/// </summary>
/// <remarks>
/// Instances are usually created by <see cref="FontSystem.GetFont(float)"/>. Custom fonts can derive
/// this type and implement <c>DrawText</c>. The extension entry points live in
/// <see cref="SpriteBatchExtensions"/> and mirror the
/// <see cref="SpriteBatch.DrawString(SpriteFont, string, Vector2, Color)"/> overload set.
/// </remarks>
#else
/// <summary>
/// Base type for fonts that can be measured and drawn through a custom renderer.
/// </summary>
/// <remarks>
/// Instances are usually created by <see cref="FontSystem.GetFont(float)"/>. Custom fonts can derive
/// this type and implement the <see cref="IFontStashRenderer"/> and <see cref="IFontStashRenderer2"/>
/// draw hooks.
/// </remarks>
#endif
public abstract class SpriteFontBase
{
	/// <summary>
	/// Gets the font size in pixels.
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

#if MONOGAME
	/// <summary>
	/// Draws a span of characters into a <see cref="SpriteBatch"/>.
	/// </summary>
	/// <param name="spriteBatch">The batch to submit glyph quads to.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph quads.</param>
	/// <param name="layerDepth">The layer depth passed to <c>SpriteBatch.Draw</c>.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	/// <remarks>
	/// This is the low-level rendering hook used by <see cref="SpriteBatchExtensions"/>.
	/// Custom fonts derive <see cref="SpriteFontBase"/> and implement this method.
	/// </remarks>
	public abstract void DrawText(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null);

	/// <summary>
	/// Draws a span of characters at a position with no rotation or scaling.
	/// </summary>
	/// <param name="spriteBatch">The batch to submit glyph quads to.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		DrawText(
			spriteBatch,
			text,
			position,
			color,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.None,
			0f,
			characterSpacing,
			lineSpacing,
			effect);
	}

	/// <summary>
	/// Draws a string into a <see cref="SpriteBatch"/>.
	/// </summary>
	/// <param name="spriteBatch">The batch to submit glyph quads to.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph quads.</param>
	/// <param name="layerDepth">The layer depth passed to <c>SpriteBatch.Draw</c>.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		string text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws a string at a position with no rotation or scaling.
	/// </summary>
	/// <param name="spriteBatch">The batch to submit glyph quads to.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		string text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.AsSpan(), position, color, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> into a <see cref="SpriteBatch"/>.
	/// </summary>
	/// <param name="spriteBatch">The batch to submit glyph quads to.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph quads.</param>
	/// <param name="layerDepth">The layer depth passed to <c>SpriteBatch.Draw</c>.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		StringBuilder text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.ToString().AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> at a position with no rotation or scaling.
	/// </summary>
	/// <param name="spriteBatch">The batch to submit glyph quads to.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		StringBuilder text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.ToString().AsSpan(), position, color, characterSpacing, lineSpacing, effect);
	}

#endif

	/// <summary>
	/// Draws a span of characters through a custom <see cref="IFontStashRenderer"/>.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph quads.</param>
	/// <param name="layerDepth">The layer depth passed to the renderer.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public abstract void DrawText(
		IFontStashRenderer renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null);

	/// <summary>
	/// Draws a span of characters through a custom renderer at a position with no rotation or scaling.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		DrawText(
			renderer,
			text,
			position,
			color,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.None,
			0f,
			characterSpacing,
			lineSpacing,
			effect);
	}

	/// <summary>
	/// Draws a string through a custom <see cref="IFontStashRenderer"/>.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph quads.</param>
	/// <param name="layerDepth">The layer depth passed to the renderer.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		string text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws a string through a custom renderer at a position with no rotation or scaling.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		string text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> through a custom <see cref="IFontStashRenderer"/>.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph quads.</param>
	/// <param name="layerDepth">The layer depth passed to the renderer.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		StringBuilder text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> through a custom renderer at a position with
	/// no rotation or scaling.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		StringBuilder text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws a span of characters through a vertex-level <see cref="IFontStashRenderer2"/>.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph texture coordinates.</param>
	/// <param name="layerDepth">The layer depth written to each vertex Z.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public abstract void DrawText(
		IFontStashRenderer2 renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null);

	/// <summary>
	/// Draws a span of characters through a vertex-level renderer at a position with no rotation or
	/// scaling.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		DrawText(
			renderer,
			text,
			position,
			color,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.None,
			0f,
			characterSpacing,
			lineSpacing,
			effect);
	}

	/// <summary>
	/// Draws a string through a vertex-level <see cref="IFontStashRenderer2"/>.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph texture coordinates.</param>
	/// <param name="layerDepth">The layer depth written to each vertex Z.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		string text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws a string through a vertex-level renderer at a position with no rotation or scaling.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		string text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> through a vertex-level
	/// <see cref="IFontStashRenderer2"/>.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="rotation">Rotation, in radians, around <paramref name="origin"/>.</param>
	/// <param name="origin">Rotation/scaling origin in text layout pixels.</param>
	/// <param name="scale">Horizontal and vertical text scale.</param>
	/// <param name="effects">Sprite effects applied to glyph texture coordinates.</param>
	/// <param name="layerDepth">The layer depth written to each vertex Z.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		StringBuilder text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect);
	}

	/// <summary>
	/// Draws the contents of a <see cref="StringBuilder"/> through a vertex-level renderer at a position
	/// with no rotation or scaling.
	/// </summary>
	/// <param name="renderer">The renderer that receives glyph quads.</param>
	/// <param name="text">The text to draw.</param>
	/// <param name="position">The top-left position of the text block.</param>
	/// <param name="color">The color mask applied to every glyph.</param>
	/// <param name="characterSpacing">Additional spacing between characters, in layout pixels, scaled with the text.</param>
	/// <param name="lineSpacing">Additional spacing between lines, in layout pixels, scaled with the text.</param>
	/// <param name="effect">The optional stroke or shadow effect; <see langword="null"/> draws normally.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		StringBuilder text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, characterSpacing, lineSpacing, effect);
	}
}
