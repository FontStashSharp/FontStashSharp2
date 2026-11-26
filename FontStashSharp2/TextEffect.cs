namespace FontStashSharp2;

/// <summary>
/// A visual effect applied when drawing text.
/// </summary>
public enum TextEffect
{
	/// <summary>Draw the text normally.</summary>
	None = 0,

	/// <summary>Draw an outline around every glyph using <c>effectColor</c>.</summary>
	Stroke = 1,

	/// <summary>Draw a shadow behind the text using <c>effectColor</c>.</summary>
	Shadow = 2,
}
