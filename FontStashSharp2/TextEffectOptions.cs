namespace FontStashSharp2;

/// <summary>
/// Configures a stroke or shadow effect for a text draw call.
/// </summary>
public readonly struct TextEffectOptions
{
	/// <summary>
	/// Creates text effect options.
	/// </summary>
	/// <param name="effect">The effect to apply. Must be <see cref="TextEffect.Stroke"/> or
	/// <see cref="TextEffect.Shadow"/>.</param>
	/// <param name="amount">The thickness or offset in layout pixels. Must be non-negative.</param>
	/// <param name="color">The effect color. <see langword="null"/> or the default color selects white.</param>
	public TextEffectOptions(TextEffect effect, int amount = 1, Color? color = null)
	{
		if (effect is not (TextEffect.Stroke or TextEffect.Shadow))
		{
			throw new ArgumentOutOfRangeException(
				nameof(effect),
				effect,
				"Effect must be Stroke or Shadow.");
		}

		if (amount < 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(amount),
				amount,
				"Effect amount must be non-negative.");
		}

		var resolvedColor = color.GetValueOrDefault();
		if (resolvedColor == default)
		{
			resolvedColor = Color.White;
		}

		Effect = effect;
		Amount = amount;
		Color = resolvedColor;
	}

	/// <summary>
	/// Gets the effect to apply.
	/// </summary>
	public TextEffect Effect { get; }

	/// <summary>
	/// Gets the effect thickness or offset, in layout pixels.
	/// </summary>
	public int Amount { get; }

	/// <summary>
	/// Gets the effect color.
	/// </summary>
	public Color Color { get; }

	/// <summary>
	/// Creates stroke options.
	/// </summary>
	public static TextEffectOptions Stroke(int amount = 1, Color? color = null)
	{
		return new TextEffectOptions(TextEffect.Stroke, amount, color);
	}

	/// <summary>
	/// Creates shadow options.
	/// </summary>
	public static TextEffectOptions Shadow(int amount = 1, Color? color = null)
	{
		return new TextEffectOptions(TextEffect.Shadow, amount, color);
	}
}
