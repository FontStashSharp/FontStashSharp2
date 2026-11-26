namespace FontStashSharp2;

/// <summary>
/// A visual effect applied when drawing text.
/// </summary>
public enum TextEffect
{
	/// <summary>Draw the text normally.</summary>
	None = 0,

	/// <summary>Draw an outline around every glyph using the text color.</summary>
	Stroke = 1,

	/// <summary>Draw a darkened, half-alpha shadow behind the text.</summary>
	Shadow = 2,
}
