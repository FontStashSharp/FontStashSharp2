using System.Text;
using FontStashSharp2.Internal;
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
	/// Gets the distance from the top of the line to the baseline, in pixels.
	/// </summary>
	/// <remarks>Derived fonts may override this for decoration placement. The default is 0.</remarks>
	public virtual float Ascent => 0f;

	/// <summary>
	/// Gets the distance from the baseline to the bottom of the line, in pixels (negative).
	/// </summary>
	/// <remarks>Derived fonts may override this for decoration placement. The default is 0.</remarks>
	public virtual float Descent => 0f;

	/// <summary>
	/// Measures a string, including line breaks, kerning, and <see cref="LineSpacing"/>.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public virtual Vector2 MeasureString(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		return MeasureString(text.AsSpan());
	}

	/// <summary>
	/// Measures a span of characters.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public virtual Vector2 MeasureString(ReadOnlySpan<char> text)
	{
		PrepareForMeasure();

		if (text.IsEmpty)
		{
			return Vector2.Zero;
		}

		var maxLineWidth = 0f;
		var lineWidth = 0f;
		var lineCount = 1;
		var hasPrevious = false;
		var previous = 0;

		for (var i = 0; i < text.Length;)
		{
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					if (lineWidth > maxLineWidth)
					{
						maxLineWidth = lineWidth;
					}

					lineWidth = 0f;
					lineCount++;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			if (hasPrevious)
			{
				lineWidth += GetKerning(previous, codepoint);
			}

			lineWidth += GetAdvance(codepoint);

			if (lineWidth > maxLineWidth)
			{
				maxLineWidth = lineWidth;
			}

			hasPrevious = true;
			previous = codepoint;
		}

		return new Vector2(maxLineWidth, lineCount * LineSpacing);
	}

	/// <summary>
	/// Measures the contents of a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="text">The text to measure.</param>
	public virtual Vector2 MeasureString(StringBuilder text)
	{
		ArgumentNullException.ThrowIfNull(text);
		return MeasureString(text.ToString().AsSpan());
	}

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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	/// <remarks>
	/// This is the low-level rendering hook used by <see cref="SpriteBatchExtensions"/>.
	/// Custom fonts derive <see cref="SpriteFontBase"/> and implement this method.
	/// </remarks>
	public virtual void DrawText(
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(spriteBatch);
		DrawTextWithEffects(
			spriteBatch,
			text,
			position,
			color,
			rotation,
			origin,
			scale,
			effects,
			layerDepth,
			characterSpacing,
			lineSpacing,
			effect,
			decoration);
	}

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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
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
			effect,
			decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		string text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.AsSpan(), position, color, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.ToString().AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		SpriteBatch spriteBatch,
		StringBuilder text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(spriteBatch, text.ToString().AsSpan(), position, color, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public virtual void DrawText(
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		DrawTextWithEffects(
			renderer,
			text,
			position,
			color,
			rotation,
			origin,
			scale,
			effects,
			layerDepth,
			characterSpacing,
			lineSpacing,
			effect,
			decoration);
	}

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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
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
			effect,
			decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		string text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		IFontStashRenderer renderer,
		StringBuilder text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public virtual void DrawText(
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		DrawTextWithEffects(
			renderer,
			text,
			position,
			color,
			rotation,
			origin,
			scale,
			effects,
			layerDepth,
			characterSpacing,
			lineSpacing,
			effect,
			decoration);
	}

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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
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
			effect,
			decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		string text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.AsSpan(), position, color, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
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
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, rotation, origin, scale, effects, layerDepth, characterSpacing, lineSpacing, effect, decoration);
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
	/// <param name="decoration">The optional underline or strikethrough; <see langword="null"/> draws no decoration.</param>
	public void DrawText(
		IFontStashRenderer2 renderer,
		StringBuilder text,
		Vector2 position,
		Color color,
		float characterSpacing = 0f,
		float lineSpacing = 0f,
		TextEffectOptions? effect = null,
		TextDecorationOptions? decoration = null)
	{
		ArgumentNullException.ThrowIfNull(text);
		DrawText(renderer, text.ToString().AsSpan(), position, color, characterSpacing, lineSpacing, effect, decoration);
	}

	/// <summary>
	/// Gets the multiplier applied to draw scales for fonts with supersampled glyphs. Defaults to 1.
	/// </summary>
	protected virtual float SupersamplingScale => 1f;

	/// <summary>
	/// Called before measurement. Derived fonts can validate state or prepare caches.
	/// </summary>
	protected virtual void PrepareForMeasure()
	{
	}

	/// <summary>
	/// Called before drawing. Derived fonts can validate state or refresh caches.
	/// </summary>
	protected virtual void PrepareForDraw()
	{
	}

	/// <summary>
	/// Gets the advance of a code point, in layout pixels.
	/// </summary>
	/// <param name="codepoint">The code point to look up.</param>
	protected virtual float GetAdvance(int codepoint)
	{
		return 0f;
	}

	/// <summary>
	/// Gets the kerning adjustment between two code points, in layout pixels.
	/// </summary>
	/// <param name="left">The previous code point.</param>
	/// <param name="right">The current code point.</param>
	protected virtual float GetKerning(int left, int right)
	{
		return 0f;
	}

	/// <summary>
	/// Gets the rendered glyph for a code point, rasterizing or resolving it if necessary.
	/// </summary>
	/// <param name="codepoint">The code point to look up.</param>
	/// <param name="device">The device used by the current draw.</param>
	/// <param name="glyph">The rendered glyph when available.</param>
	/// <returns><see langword="true"/> when the font has a glyph for the code point.</returns>
	protected virtual bool TryGetRenderedGlyph(
		int codepoint,
		AtlasDevice device,
		out SpriteGlyph glyph)
	{
		glyph = default;
		return false;
	}

	/// <summary>
	/// Gets the texture for a rendered glyph.
	/// </summary>
	/// <param name="device">The device used by the current draw.</param>
	/// <param name="textureIndex">The texture index supplied by <see cref="TryGetRenderedGlyph"/>.</param>
	protected virtual AtlasTexture GetTexture(AtlasDevice device, int textureIndex)
	{
		throw new NotSupportedException("This font does not provide glyph textures.");
	}

	/// <summary>
	/// Gets the 1x1 white texture used for text decorations.
	/// </summary>
	/// <param name="device">The device used by the current draw.</param>
	protected virtual AtlasTexture GetWhiteTexture(AtlasDevice device)
	{
		throw new NotSupportedException("This font does not provide a decoration texture.");
	}

	/// <summary>
	/// A resolved glyph used by the shared draw pipeline.
	/// </summary>
	protected readonly struct SpriteGlyph
	{
		/// <summary>
		/// Creates a rendered glyph.
		/// </summary>
		/// <param name="textureIndex">The texture index, or -1 when the glyph has no pixels.</param>
		/// <param name="sourceRectangle">The glyph rectangle in the texture.</param>
		/// <param name="offsetX">The horizontal offset from the pen position, in layout pixels.</param>
		/// <param name="offsetY">The vertical offset from the baseline, in layout pixels.</param>
		/// <param name="advance">The advance in layout pixels.</param>
		public SpriteGlyph(
			int textureIndex,
			Rectangle sourceRectangle,
			float offsetX,
			float offsetY,
			float advance)
		{
			TextureIndex = textureIndex;
			SourceRectangle = sourceRectangle;
			OffsetX = offsetX;
			OffsetY = offsetY;
			Advance = advance;
		}

		/// <summary>Gets the texture index, or -1 when the glyph has no pixels.</summary>
		public int TextureIndex { get; }

		/// <summary>Gets the glyph rectangle in the texture.</summary>
		public Rectangle SourceRectangle { get; }

		/// <summary>Gets the horizontal offset from the pen position, in layout pixels.</summary>
		public float OffsetX { get; }

		/// <summary>Gets the vertical offset from the baseline, in layout pixels.</summary>
		public float OffsetY { get; }

		/// <summary>Gets the advance in layout pixels.</summary>
		public float Advance { get; }

		/// <summary>Gets whether the glyph has pixels to draw.</summary>
		public bool HasTexture => TextureIndex >= 0;
	}

	private bool PrepareDraw(
		TextEffectOptions? effect,
		TextDecorationOptions? decoration,
		out TextEffectOptions activeEffect,
		out TextDecorationOptions decorationValue,
		out bool hasDecoration)
	{
		if (effect is { } options)
		{
			TextRenderHelpers.ValidateTextEffect(options);
		}

		if (decoration is { } decorationOptions)
		{
			TextRenderHelpers.ValidateTextDecoration(decorationOptions);
		}

		activeEffect = effect.GetValueOrDefault();
		decorationValue = decoration.GetValueOrDefault();
		hasDecoration = decorationValue.Decoration != TextDecoration.None
			&& decorationValue.DecorationWidth > 0f;

		return effect is { Amount: > 0 };
	}

#if MONOGAME
	private void DrawTextWithEffects(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		TextEffectOptions? effect,
		TextDecorationOptions? decoration)
	{
		PrepareForDraw();

		if (text.IsEmpty)
		{
			return;
		}

		var hasEffect = PrepareDraw(effect, decoration, out var activeEffect, out var decorationValue, out var hasDecoration);

		if (!hasEffect)
		{
			if (rotation == 0f
				&& origin == Vector2.Zero
				&& scale == Vector2.One
				&& effects == SpriteEffects.None
				&& layerDepth == 0f
				&& characterSpacing == 0f
				&& lineSpacing == 0f)
			{
				DrawTextDefault(spriteBatch, text, position, color);
			}
			else
			{
				DrawTextCore(
					spriteBatch,
					text,
					position,
					color,
					rotation,
					origin,
					scale,
					effects,
					layerDepth,
					characterSpacing,
					lineSpacing,
					Vector2.Zero);
			}

			if (hasDecoration)
			{
				DrawDecorations(
					spriteBatch,
					text,
					position,
					color,
					rotation,
					origin,
					scale,
					effects,
					layerDepth,
					characterSpacing,
					lineSpacing,
					decorationValue);
			}

			return;
		}

		foreach (var offset in TextRenderHelpers.EnumerateEffectOffsets(activeEffect.Effect, activeEffect.Amount))
		{
			DrawTextCore(
				spriteBatch,
				text,
				position,
				activeEffect.Color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				offset);
		}

		DrawTextCore(
			spriteBatch,
			text,
			position,
			color,
			rotation,
			origin,
			scale,
			effects,
			layerDepth,
			characterSpacing,
			lineSpacing,
			Vector2.Zero);

		if (hasDecoration)
		{
			DrawDecorations(
				spriteBatch,
				text,
				position,
				color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				decorationValue);
		}
	}

	private void DrawTextDefault(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color)
	{
		var device = spriteBatch.GraphicsDevice;
		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = 0;
		AtlasTexture? texture = null;
		var textureIndex = -1;
		var inverseScale = new Vector2(SupersamplingScale);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			if (hasPrevious)
			{
				penX += GetKerning(previous, codepoint);
			}

			var hasGlyph = TryGetRenderedGlyph(codepoint, device, out var glyph);

			if (hasGlyph && glyph.HasTexture)
			{
				if (glyph.TextureIndex != textureIndex)
				{
					texture = GetTexture(device, glyph.TextureIndex);
					textureIndex = glyph.TextureIndex;
				}

				var glyphPosition = new Vector2(
					position.X + penX + glyph.OffsetX,
					position.Y + Ascent + lineTop + glyph.OffsetY);

				if (SupersamplingScale != 1f)
				{
					spriteBatch.Draw(
						texture!,
						glyphPosition,
						glyph.SourceRectangle,
						color,
						0f,
						Vector2.Zero,
						inverseScale,
						SpriteEffects.None,
						0f);
				}
				else
				{
					spriteBatch.Draw(texture!, glyphPosition, glyph.SourceRectangle, color);
				}
			}

			penX += hasGlyph ? glyph.Advance : GetAdvance(codepoint);
			hasPrevious = true;
			previous = codepoint;
		}
	}

	private void DrawTextCore(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		Vector2 effectOffset)
	{
		var device = spriteBatch.GraphicsDevice;
		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically
			? MeasureStringWithSpacing(text, characterSpacing, lineSpacing)
			: Vector2.Zero;

		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = 0;
		AtlasTexture? texture = null;
		var textureIndex = -1;
		var drawScale = new Vector2(
			scale.X * SupersamplingScale,
			scale.Y * SupersamplingScale);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing + lineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			if (hasPrevious)
			{
				penX += GetKerning(previous, codepoint);
			}

			var hasGlyph = TryGetRenderedGlyph(codepoint, device, out var glyph);

			if (hasGlyph && glyph.HasTexture)
			{
				var glyphX = penX + glyph.OffsetX;
				var glyphY = lineTop + Ascent + glyph.OffsetY;
				var glyphWidth = glyph.SourceRectangle.Width * SupersamplingScale;
				var glyphHeight = glyph.SourceRectangle.Height * SupersamplingScale;

				if (flipHorizontally)
				{
					glyphX = blockSize.X - glyphX - glyphWidth;
				}

				if (flipVertically)
				{
					glyphY = blockSize.Y - glyphY - glyphHeight;
				}

				glyphX += effectOffset.X;
				glyphY += effectOffset.Y;

				if (glyph.TextureIndex != textureIndex)
				{
					texture = GetTexture(device, glyph.TextureIndex);
					textureIndex = glyph.TextureIndex;
				}

				var localX = (glyphX - origin.X) * scale.X;
				var localY = (glyphY - origin.Y) * scale.Y;

				Vector2 screenPosition;
				if (rotation == 0f)
				{
					screenPosition = new Vector2(position.X + localX, position.Y + localY);
				}
				else
				{
					var cos = MathF.Cos(rotation);
					var sin = MathF.Sin(rotation);
					screenPosition = new Vector2(
						position.X + (localX * cos) - (localY * sin),
						position.Y + (localX * sin) + (localY * cos));
				}

				spriteBatch.Draw(
					texture!,
					screenPosition,
					glyph.SourceRectangle,
					color,
					rotation,
					Vector2.Zero,
					drawScale,
					effects,
					layerDepth);
			}

			penX += hasGlyph ? glyph.Advance : GetAdvance(codepoint);
			penX += characterSpacing;
			hasPrevious = true;
			previous = codepoint;
		}
	}

	private void DrawDecorations(
		SpriteBatch spriteBatch,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		TextDecorationOptions decoration)
	{
		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically
			? MeasureStringWithSpacing(text, characterSpacing, lineSpacing)
			: Vector2.Zero;
		var lines = GetDecorationLines(text, characterSpacing, lineSpacing);
		if (lines.Count == 0)
		{
			return;
		}

		var white = GetWhiteTexture(spriteBatch.GraphicsDevice);
		var lineColor = decoration.Color ?? color;

		foreach (var line in lines)
		{
			if (!TextRenderHelpers.TryGetDecorationGeometry(
				decoration,
				line,
				Ascent,
				Descent,
				blockSize,
				effects,
				out var x,
				out var y,
				out var width,
				out var thickness))
			{
				continue;
			}

			var localX = (x - origin.X) * scale.X;
			var localY = (y - origin.Y) * scale.Y;
			Vector2 screenPosition;
			if (rotation == 0f)
			{
				screenPosition = new Vector2(position.X + localX, position.Y + localY);
			}
			else
			{
				var cos = MathF.Cos(rotation);
				var sin = MathF.Sin(rotation);
				screenPosition = new Vector2(
					position.X + (localX * cos) - (localY * sin),
					position.Y + (localX * sin) + (localY * cos));
			}

			spriteBatch.Draw(
				white,
				screenPosition,
				null,
				lineColor,
				rotation,
				Vector2.Zero,
				new Vector2(width * scale.X, thickness * scale.Y),
				effects,
				layerDepth);
		}
	}
#endif

	private void DrawTextWithEffects(
		IFontStashRenderer renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		TextEffectOptions? effect,
		TextDecorationOptions? decoration)
	{
		PrepareForDraw();

		if (text.IsEmpty)
		{
			return;
		}

		var hasEffect = PrepareDraw(effect, decoration, out var activeEffect, out var decorationValue, out var hasDecoration);

		if (!hasEffect)
		{
			DrawTextCore(
				renderer,
				text,
				position,
				color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				Vector2.Zero);

			if (hasDecoration)
			{
				DrawDecorations(
					renderer,
					text,
					position,
					color,
					rotation,
					origin,
					scale,
					effects,
					layerDepth,
					characterSpacing,
					lineSpacing,
					decorationValue);
			}

			return;
		}

		foreach (var offset in TextRenderHelpers.EnumerateEffectOffsets(activeEffect.Effect, activeEffect.Amount))
		{
			DrawTextCore(
				renderer,
				text,
				position,
				activeEffect.Color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				offset);
		}

		DrawTextCore(
			renderer,
			text,
			position,
			color,
			rotation,
			origin,
			scale,
			effects,
			layerDepth,
			characterSpacing,
			lineSpacing,
			Vector2.Zero);

		if (hasDecoration)
		{
			DrawDecorations(
				renderer,
				text,
				position,
				color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				decorationValue);
		}
	}

	private void DrawTextCore(
		IFontStashRenderer renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		Vector2 effectOffset)
	{
		var device = GetDevice(renderer);
		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically
			? MeasureStringWithSpacing(text, characterSpacing, lineSpacing)
			: Vector2.Zero;

		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = 0;
		AtlasTexture? texture = null;
		var textureIndex = -1;
		var drawScale = new Vector2(
			scale.X * SupersamplingScale,
			scale.Y * SupersamplingScale);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing + lineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			if (hasPrevious)
			{
				penX += GetKerning(previous, codepoint);
			}

			var hasGlyph = TryGetRenderedGlyph(codepoint, device, out var glyph);

			if (hasGlyph && glyph.HasTexture)
			{
				var glyphX = penX + glyph.OffsetX;
				var glyphY = lineTop + Ascent + glyph.OffsetY;
				var glyphWidth = glyph.SourceRectangle.Width * SupersamplingScale;
				var glyphHeight = glyph.SourceRectangle.Height * SupersamplingScale;

				if (flipHorizontally)
				{
					glyphX = blockSize.X - glyphX - glyphWidth;
				}

				if (flipVertically)
				{
					glyphY = blockSize.Y - glyphY - glyphHeight;
				}

				glyphX += effectOffset.X;
				glyphY += effectOffset.Y;

				if (glyph.TextureIndex != textureIndex)
				{
					texture = GetTexture(device, glyph.TextureIndex);
					textureIndex = glyph.TextureIndex;
				}

				var localX = (glyphX - origin.X) * scale.X;
				var localY = (glyphY - origin.Y) * scale.Y;

				Vector2 screenPosition;
				if (rotation == 0f)
				{
					screenPosition = new Vector2(position.X + localX, position.Y + localY);
				}
				else
				{
					var cos = MathF.Cos(rotation);
					var sin = MathF.Sin(rotation);
					screenPosition = new Vector2(
						position.X + (localX * cos) - (localY * sin),
						position.Y + (localX * sin) + (localY * cos));
				}

				DrawGlyph(
					renderer,
					texture!,
					screenPosition,
					glyph.SourceRectangle,
					color,
					rotation,
					drawScale,
					effects,
					layerDepth);
			}

			penX += hasGlyph ? glyph.Advance : GetAdvance(codepoint);
			penX += characterSpacing;
			hasPrevious = true;
			previous = codepoint;
		}
	}

	private void DrawDecorations(
		IFontStashRenderer renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		TextDecorationOptions decoration)
	{
		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically
			? MeasureStringWithSpacing(text, characterSpacing, lineSpacing)
			: Vector2.Zero;
		var lines = GetDecorationLines(text, characterSpacing, lineSpacing);
		if (lines.Count == 0)
		{
			return;
		}

		var white = GetWhiteTexture(GetDevice(renderer));
		var lineColor = decoration.Color ?? color;

		foreach (var line in lines)
		{
			if (!TextRenderHelpers.TryGetDecorationGeometry(
				decoration,
				line,
				Ascent,
				Descent,
				blockSize,
				effects,
				out var x,
				out var y,
				out var width,
				out var thickness))
			{
				continue;
			}

			var localX = (x - origin.X) * scale.X;
			var localY = (y - origin.Y) * scale.Y;
			Vector2 screenPosition;
			if (rotation == 0f)
			{
				screenPosition = new Vector2(position.X + localX, position.Y + localY);
			}
			else
			{
				var cos = MathF.Cos(rotation);
				var sin = MathF.Sin(rotation);
				screenPosition = new Vector2(
					position.X + (localX * cos) - (localY * sin),
					position.Y + (localX * sin) + (localY * cos));
			}

			DrawGlyph(
				renderer,
				white,
				screenPosition,
				null,
				lineColor,
				rotation,
				new Vector2(width * scale.X, thickness * scale.Y),
				effects,
				layerDepth);
		}
	}

	private void DrawTextWithEffects(
		IFontStashRenderer2 renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		TextEffectOptions? effect,
		TextDecorationOptions? decoration)
	{
		PrepareForDraw();

		if (text.IsEmpty)
		{
			return;
		}

		var hasEffect = PrepareDraw(effect, decoration, out var activeEffect, out var decorationValue, out var hasDecoration);

		if (!hasEffect)
		{
			DrawTextCore(
				renderer,
				text,
				position,
				color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				Vector2.Zero);

			if (hasDecoration)
			{
				DrawDecorations(
					renderer,
					text,
					position,
					color,
					rotation,
					origin,
					scale,
					effects,
					layerDepth,
					characterSpacing,
					lineSpacing,
					decorationValue);
			}

			return;
		}

		foreach (var offset in TextRenderHelpers.EnumerateEffectOffsets(activeEffect.Effect, activeEffect.Amount))
		{
			DrawTextCore(
				renderer,
				text,
				position,
				activeEffect.Color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				offset);
		}

		DrawTextCore(
			renderer,
			text,
			position,
			color,
			rotation,
			origin,
			scale,
			effects,
			layerDepth,
			characterSpacing,
			lineSpacing,
			Vector2.Zero);

		if (hasDecoration)
		{
			DrawDecorations(
				renderer,
				text,
				position,
				color,
				rotation,
				origin,
				scale,
				effects,
				layerDepth,
				characterSpacing,
				lineSpacing,
				decorationValue);
		}
	}

	private void DrawTextCore(
		IFontStashRenderer2 renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		Vector2 effectOffset)
	{
		var device = GetDevice(renderer);
		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically
			? MeasureStringWithSpacing(text, characterSpacing, lineSpacing)
			: Vector2.Zero;

		var lineTop = 0f;
		var penX = 0f;
		var hasPrevious = false;
		var previous = 0;
		AtlasTexture? texture = null;
		var textureIndex = -1;
		var textureWidth = 0;
		var textureHeight = 0;
		var drawScale = new Vector2(
			scale.X * SupersamplingScale,
			scale.Y * SupersamplingScale);

		for (var i = 0; i < text.Length;)
		{
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					lineTop += LineSpacing + lineSpacing;
					penX = 0f;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			if (hasPrevious)
			{
				penX += GetKerning(previous, codepoint);
			}

			var hasGlyph = TryGetRenderedGlyph(codepoint, device, out var glyph);

			if (hasGlyph && glyph.HasTexture)
			{
				var glyphX = penX + glyph.OffsetX;
				var glyphY = lineTop + Ascent + glyph.OffsetY;
				var glyphWidth = glyph.SourceRectangle.Width * SupersamplingScale;
				var glyphHeight = glyph.SourceRectangle.Height * SupersamplingScale;

				if (flipHorizontally)
				{
					glyphX = blockSize.X - glyphX - glyphWidth;
				}

				if (flipVertically)
				{
					glyphY = blockSize.Y - glyphY - glyphHeight;
				}

				glyphX += effectOffset.X;
				glyphY += effectOffset.Y;

				if (glyph.TextureIndex != textureIndex)
				{
					texture = GetTexture(device, glyph.TextureIndex);
					var textureSize = GetTextureSize(device, texture);
					textureWidth = textureSize.X;
					textureHeight = textureSize.Y;
					textureIndex = glyph.TextureIndex;
				}

				var localX = (glyphX - origin.X) * scale.X;
				var localY = (glyphY - origin.Y) * scale.Y;

				Vector2 screenPosition;
				if (rotation == 0f)
				{
					screenPosition = new Vector2(position.X + localX, position.Y + localY);
				}
				else
				{
					var rotationCos = MathF.Cos(rotation);
					var rotationSin = MathF.Sin(rotation);
					screenPosition = new Vector2(
						position.X + (localX * rotationCos) - (localY * rotationSin),
						position.Y + (localX * rotationSin) + (localY * rotationCos));
				}

				var cos = rotation == 0f ? 1f : MathF.Cos(rotation);
				var sin = rotation == 0f ? 0f : MathF.Sin(rotation);
				var quadWidth = glyph.SourceRectangle.Width * drawScale.X;
				var quadHeight = glyph.SourceRectangle.Height * drawScale.Y;
				var right = new Vector2(quadWidth * cos, quadWidth * sin);
				var down = new Vector2(-quadHeight * sin, quadHeight * cos);

				var source = glyph.SourceRectangle;
				var u0 = source.X / (float)textureWidth;
				var v0 = source.Y / (float)textureHeight;
				var u1 = (source.X + source.Width) / (float)textureWidth;
				var v1 = (source.Y + source.Height) / (float)textureHeight;

				if (flipHorizontally)
				{
					(u0, u1) = (u1, u0);
				}

				if (flipVertically)
				{
					(v0, v1) = (v1, v0);
				}

				var topLeft = new VertexPositionColorTexture(
					new Vector3(screenPosition.X, screenPosition.Y, layerDepth),
					color,
					new Vector2(u0, v0));
				var topRight = new VertexPositionColorTexture(
					new Vector3(screenPosition.X + right.X, screenPosition.Y + right.Y, layerDepth),
					color,
					new Vector2(u1, v0));
				var bottomLeft = new VertexPositionColorTexture(
					new Vector3(screenPosition.X + down.X, screenPosition.Y + down.Y, layerDepth),
					color,
					new Vector2(u0, v1));
				var bottomRight = new VertexPositionColorTexture(
					new Vector3(screenPosition.X + right.X + down.X, screenPosition.Y + right.Y + down.Y, layerDepth),
					color,
					new Vector2(u1, v1));

				renderer.DrawQuad(texture!, ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
			}

			penX += hasGlyph ? glyph.Advance : GetAdvance(codepoint);
			penX += characterSpacing;
			hasPrevious = true;
			previous = codepoint;
		}
	}

	private void DrawDecorations(
		IFontStashRenderer2 renderer,
		ReadOnlySpan<char> text,
		Vector2 position,
		Color color,
		float rotation,
		Vector2 origin,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth,
		float characterSpacing,
		float lineSpacing,
		TextDecorationOptions decoration)
	{
		var flipHorizontally = (effects & SpriteEffects.FlipHorizontally) != 0;
		var flipVertically = (effects & SpriteEffects.FlipVertically) != 0;
		var blockSize = flipHorizontally || flipVertically
			? MeasureStringWithSpacing(text, characterSpacing, lineSpacing)
			: Vector2.Zero;
		var lines = GetDecorationLines(text, characterSpacing, lineSpacing);
		if (lines.Count == 0)
		{
			return;
		}

		var white = GetWhiteTexture(GetDevice(renderer));
		var lineColor = decoration.Color ?? color;

		foreach (var line in lines)
		{
			if (!TextRenderHelpers.TryGetDecorationGeometry(
				decoration,
				line,
				Ascent,
				Descent,
				blockSize,
				effects,
				out var x,
				out var y,
				out var width,
				out var thickness))
			{
				continue;
			}

			var localX = (x - origin.X) * scale.X;
			var localY = (y - origin.Y) * scale.Y;
			Vector2 screenPosition;
			if (rotation == 0f)
			{
				screenPosition = new Vector2(position.X + localX, position.Y + localY);
			}
			else
			{
				var cos = MathF.Cos(rotation);
				var sin = MathF.Sin(rotation);
				screenPosition = new Vector2(
					position.X + (localX * cos) - (localY * sin),
					position.Y + (localX * sin) + (localY * cos));
			}

			var cos2 = rotation == 0f ? 1f : MathF.Cos(rotation);
			var sin2 = rotation == 0f ? 0f : MathF.Sin(rotation);
			var quadWidth = width * scale.X;
			var quadHeight = thickness * scale.Y;
			var right = new Vector2(quadWidth * cos2, quadWidth * sin2);
			var down = new Vector2(-quadHeight * sin2, quadHeight * cos2);

			var topLeft = new VertexPositionColorTexture(
				new Vector3(screenPosition.X, screenPosition.Y, layerDepth),
				lineColor,
				new Vector2(0f, 0f));
			var topRight = new VertexPositionColorTexture(
				new Vector3(screenPosition.X + right.X, screenPosition.Y + right.Y, layerDepth),
				lineColor,
				new Vector2(1f, 0f));
			var bottomLeft = new VertexPositionColorTexture(
				new Vector3(screenPosition.X + down.X, screenPosition.Y + down.Y, layerDepth),
				lineColor,
				new Vector2(0f, 1f));
			var bottomRight = new VertexPositionColorTexture(
				new Vector3(screenPosition.X + right.X + down.X, screenPosition.Y + right.Y + down.Y, layerDepth),
				lineColor,
				new Vector2(1f, 1f));

			renderer.DrawQuad(white, ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
		}
	}

	private Vector2 MeasureStringWithSpacing(
		ReadOnlySpan<char> text,
		float characterSpacing,
		float lineSpacing)
	{
		if (text.IsEmpty)
		{
			return Vector2.Zero;
		}

		if (characterSpacing == 0f && lineSpacing == 0f)
		{
			return MeasureString(text);
		}

		var maxLineWidth = 0f;
		var lineWidth = 0f;
		var lineCount = 1;
		var hasPrevious = false;
		var previous = 0;

		for (var i = 0; i < text.Length;)
		{
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					if (lineWidth > maxLineWidth)
					{
						maxLineWidth = lineWidth;
					}

					lineWidth = 0f;
					lineCount++;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			if (hasPrevious)
			{
				lineWidth += GetKerning(previous, codepoint);
				lineWidth += characterSpacing;
			}

			lineWidth += GetAdvance(codepoint);

			if (lineWidth > maxLineWidth)
			{
				maxLineWidth = lineWidth;
			}

			hasPrevious = true;
			previous = codepoint;
		}

		return new Vector2(
			maxLineWidth,
			(lineCount * LineSpacing) + ((lineCount - 1) * lineSpacing));
	}

	private List<TextRenderHelpers.DecorationLine> GetDecorationLines(
		ReadOnlySpan<char> text,
		float characterSpacing,
		float lineSpacing)
	{
		var lines = new List<TextRenderHelpers.DecorationLine>();
		var lineTop = 0f;
		var lineWidth = 0f;
		var hasPrevious = false;
		var previous = 0;

		for (var i = 0; i < text.Length;)
		{
			var codepoint = TextRenderHelpers.ReadCodepoint(text, ref i);

			switch (codepoint)
			{
				case '\r':
					continue;
				case '\n':
					if (lineWidth > 0f)
					{
						lines.Add(new TextRenderHelpers.DecorationLine(lineWidth, lineTop));
					}

					lineWidth = 0f;
					lineTop += LineSpacing + lineSpacing;
					hasPrevious = false;
					continue;
				case '\t':
					codepoint = ' ';
					break;
			}

			if (hasPrevious)
			{
				lineWidth += GetKerning(previous, codepoint);
				lineWidth += characterSpacing;
			}

			lineWidth += GetAdvance(codepoint);
			hasPrevious = true;
			previous = codepoint;
		}

		if (lineWidth > 0f)
		{
			lines.Add(new TextRenderHelpers.DecorationLine(lineWidth, lineTop));
		}

		return lines;
	}

	private static AtlasDevice GetDevice(IFontStashRenderer renderer)
	{
#if MONOGAME
		return renderer.GraphicsDevice;
#else
		return renderer.TextureManager;
#endif
	}

	private static AtlasDevice GetDevice(IFontStashRenderer2 renderer)
	{
#if MONOGAME
		return renderer.GraphicsDevice;
#else
		return renderer.TextureManager;
#endif
	}

	private static Point GetTextureSize(AtlasDevice device, AtlasTexture texture)
	{
#if MONOGAME
		return new Point(texture.Width, texture.Height);
#else
		return device.GetTextureSize(texture);
#endif
	}

	private static void DrawGlyph(
		IFontStashRenderer renderer,
		AtlasTexture texture,
		Vector2 position,
		Rectangle? sourceRectangle,
		Color color,
		float rotation,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
#if MONOGAME
		renderer.Draw(texture, position, sourceRectangle, color, rotation, scale, effects, layerDepth);
#else
		renderer.DrawText(texture, position, sourceRectangle, color, rotation, scale, effects, layerDepth);
#endif
	}
}
