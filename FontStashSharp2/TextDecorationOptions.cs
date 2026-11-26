namespace FontStashSharp2;

/// <summary>
/// Configures an underline or strikethrough for a text draw call.
/// </summary>
public readonly struct TextDecorationOptions
{
	/// <summary>
	/// Creates text decoration options.
	/// </summary>
	/// <param name="decoration">The decoration to draw. Must be <see cref="TextDecoration.None"/>,
	/// <see cref="TextDecoration.Underline"/>, or <see cref="TextDecoration.Strikethrough"/>.</param>
	/// <param name="decorationWidth">The line thickness in layout pixels. Must be finite and non-negative.</param>
	/// <param name="color">The line color, or <see langword="null"/> to use the main text color.</param>
	public TextDecorationOptions(
		TextDecoration decoration,
		float decorationWidth = 1f,
		Color? color = null)
	{
		if (decoration is not (TextDecoration.None
			or TextDecoration.Underline
			or TextDecoration.Strikethrough))
		{
			throw new ArgumentOutOfRangeException(
				nameof(decoration),
				decoration,
				"Decoration must be None, Underline, or Strikethrough.");
		}

		if (!float.IsFinite(decorationWidth) || decorationWidth < 0f)
		{
			throw new ArgumentOutOfRangeException(
				nameof(decorationWidth),
				decorationWidth,
				"Decoration width must be finite and non-negative.");
		}

		Decoration = decoration;
		DecorationWidth = decorationWidth;
		Color = color;
	}

	/// <summary>
	/// Gets the decoration to draw.
	/// </summary>
	public TextDecoration Decoration { get; }

	/// <summary>
	/// Gets the line thickness in layout pixels.
	/// </summary>
	public float DecorationWidth { get; }

	/// <summary>
	/// Gets the line color, or <see langword="null"/> to use the main text color.
	/// </summary>
	public Color? Color { get; }

	/// <summary>
	/// Creates underline options.
	/// </summary>
	/// <param name="width">The line thickness in layout pixels.</param>
	/// <param name="color">The line color, or <see langword="null"/> to use the main text color.</param>
	public static TextDecorationOptions Underline(float width = 1f, Color? color = null)
	{
		return new TextDecorationOptions(TextDecoration.Underline, width, color);
	}

	/// <summary>
	/// Creates strikethrough options.
	/// </summary>
	/// <param name="width">The line thickness in layout pixels.</param>
	/// <param name="color">The line color, or <see langword="null"/> to use the main text color.</param>
	public static TextDecorationOptions Strikethrough(float width = 1f, Color? color = null)
	{
		return new TextDecorationOptions(TextDecoration.Strikethrough, width, color);
	}
}
