namespace FontStashSharp2;

/// <summary>
/// A visual effect applied when drawing text.
/// </summary>
public enum TextEffect
{
	/// <summary>Draw an outline around every glyph using the effect color.</summary>
	Stroke = 1,

	/// <summary>Draw a shadow behind the text using the effect color.</summary>
	Shadow = 2,
}
