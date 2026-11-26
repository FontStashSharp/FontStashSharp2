namespace FontStashSharp2.Internal;

/// <summary>
/// Shared helpers for the draw hooks of the built-in font types.
/// </summary>
internal static class TextRenderHelpers
{
	public static int ReadCodepoint(ReadOnlySpan<char> text, ref int index)
	{
		var current = text[index];

		if (!char.IsSurrogate(current))
		{
			index++;
			return current;
		}

		if (char.IsHighSurrogate(current)
			&& index + 1 < text.Length
			&& char.IsLowSurrogate(text[index + 1]))
		{
			var codepoint = char.ConvertToUtf32(current, text[index + 1]);
			index += 2;
			return codepoint;
		}

		// A lone surrogate cannot be encoded; use U+FFFD so the font's fallback/notdef path handles it.
		index++;
		return 0xFFFD;
	}

	public static void ValidateTextEffect(TextEffectOptions effect)
	{
		if (effect.Effect is not (TextEffect.Stroke or TextEffect.Shadow))
		{
			throw new ArgumentOutOfRangeException(
				nameof(effect),
				effect.Effect,
				"Effect must be Stroke or Shadow.");
		}

		if (effect.Amount < 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(effect),
				effect.Amount,
				"Effect amount must be non-negative.");
		}
	}

	public static void ValidateTextDecoration(TextDecorationOptions decoration)
	{
		if (decoration.Decoration is not (TextDecoration.None
			or TextDecoration.Underline
			or TextDecoration.Strikethrough))
		{
			throw new ArgumentOutOfRangeException(
				nameof(decoration),
				decoration.Decoration,
				"Decoration must be None, Underline, or Strikethrough.");
		}

		if (!float.IsFinite(decoration.DecorationWidth) || decoration.DecorationWidth < 0f)
		{
			throw new ArgumentOutOfRangeException(
				nameof(decoration),
				decoration.DecorationWidth,
				"Decoration width must be finite and non-negative.");
		}
	}

	public static IEnumerable<Vector2> EnumerateEffectOffsets(TextEffect effect, int effectAmount)
	{
		if (effect == TextEffect.Shadow)
		{
			yield return new Vector2(effectAmount, effectAmount);
			yield break;
		}

		if (effect != TextEffect.Stroke || effectAmount <= 0)
		{
			yield break;
		}

		for (var y = -effectAmount; y <= effectAmount; y++)
		{
			for (var x = -effectAmount; x <= effectAmount; x++)
			{
				if (x != 0 || y != 0)
				{
					yield return new Vector2(x, y);
				}
			}
		}
	}

	public static bool TryGetDecorationGeometry(
		TextDecorationOptions decoration,
		DecorationLine line,
		float ascent,
		float descent,
		Vector2 blockSize,
		SpriteEffects effects,
		out float x,
		out float y,
		out float width,
		out float thickness)
	{
		x = 0f;
		y = 0f;
		width = 0f;
		thickness = 0f;

		if (decoration.Decoration == TextDecoration.None || decoration.DecorationWidth <= 0f)
		{
			return false;
		}

		var baseline = line.Top + ascent;
		y = decoration.Decoration == TextDecoration.Underline
			? baseline - (descent * 0.5f)
			: baseline - (ascent * 0.45f);
		width = line.Width;
		thickness = decoration.DecorationWidth;

		if ((effects & SpriteEffects.FlipHorizontally) != 0)
		{
			x = blockSize.X - x - width;
		}

		if ((effects & SpriteEffects.FlipVertically) != 0)
		{
			y = blockSize.Y - y - thickness;
		}

		return true;
	}

	public readonly struct DecorationLine
	{
		public DecorationLine(float width, float top)
		{
			Width = width;
			Top = top;
		}

		public float Width { get; }

		public float Top { get; }
	}
}
