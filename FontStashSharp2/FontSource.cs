using System.Text;
using StbTrueTypeNext;

namespace FontStashSharp2;

/// <summary>
/// One parsed font face added to a <see cref="FontSystem"/>.
/// </summary>
/// <remarks>
/// The font bytes are parsed with StbTrueTypeNext. The byte array passed to
/// <see cref="FontSystem.AddFont(byte[], int, string?)"/> is read in place and is therefore kept
/// alive by this object; do not mutate it while the font is in use.
/// </remarks>
public sealed class FontSource
{
	private readonly STBFont _font;

	internal FontSource(byte[] data, int fontIndex, string? name, int index)
	{
		ArgumentNullException.ThrowIfNull(data);

		var fontCount = STBFontCollection.GetNumberOfFonts(data);
		if (fontCount <= 0)
		{
			throw new InvalidDataException("The data does not contain a recognized TrueType or OpenType font.");
		}

		if (fontIndex < 0 || fontIndex >= fontCount)
		{
			throw new ArgumentOutOfRangeException(
				nameof(fontIndex),
				fontIndex,
				$"The font file contains {fontCount} face(s); valid indices are 0 through {fontCount - 1}.");
		}

		var offset = STBFontCollection.GetFontOffsetForIndex(data, fontIndex);
		if (offset < 0)
		{
			throw new InvalidDataException("The requested font face could not be located in the font data.");
		}

		try
		{
			_font = new STBFont(data, offset);
		}
		catch (Exception exception) when (exception is not OutOfMemoryException)
		{
			throw new InvalidDataException("The font data could not be parsed.", exception);
		}

		_font.GetFontVMetrics(out var ascent, out var descent, out var lineGap);
		Ascent = ascent;
		Descent = descent;
		LineGap = lineGap;

		Index = index;
		Name = !string.IsNullOrWhiteSpace(name)
			? name.Trim()
			: ReadName(_font, 4) ?? ReadName(_font, 1) ?? $"Font {fontIndex}";
	}

	/// <summary>
	/// Gets the zero-based index of this source within its <see cref="FontSystem"/>.
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Gets the display name of the font face.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the font's ascent in font units (usually positive).
	/// </summary>
	public int Ascent { get; }

	/// <summary>
	/// Gets the font's descent in font units (usually negative).
	/// </summary>
	public int Descent { get; }

	/// <summary>
	/// Gets the font's line gap in font units.
	/// </summary>
	public int LineGap { get; }

	internal STBFont Font => _font;

	/// <summary>
	/// Gets the glyph index for a Unicode code point, or 0 when the font does not contain the glyph.
	/// </summary>
	/// <param name="codepoint">The Unicode code point to look up.</param>
	public int GetGlyphIndex(int codepoint)
	{
		return _font.FindGlyphIndex(codepoint);
	}

	/// <summary>
	/// Gets whether the font contains a glyph for a Unicode code point.
	/// </summary>
	/// <param name="codepoint">The Unicode code point to look up.</param>
	public bool ContainsGlyph(int codepoint)
	{
		return _font.FindGlyphIndex(codepoint) != 0;
	}

	internal float GetScale(float fontSize, FontScaleMode scaleMode)
	{
		return scaleMode == FontScaleMode.ScaleForPixelHeight
			? _font.ScaleForPixelHeight(fontSize)
			: _font.ScaleForMappingEmToPixels(fontSize);
	}

	internal int GetAdvanceWidth(int glyphIndex)
	{
		_font.GetGlyphHMetrics(glyphIndex, out var advanceWidth, out _);
		return advanceWidth;
	}

	internal int GetKerningAdvance(int leftGlyphIndex, int rightGlyphIndex)
	{
		return _font.GetGlyphKernAdvance(leftGlyphIndex, rightGlyphIndex);
	}

	private static string? ReadName(STBFont font, int nameId)
	{
		string? raw;
		try
		{
			raw = font.GetFontNameString(
				STBFont.STBTT_PLATFORM_ID_MICROSOFT,
				STBFont.STBTT_MS_EID_UNICODE_BMP,
				STBFont.STBTT_MS_LANG_ENGLISH,
				nameId);
		}
		catch (Exception exception) when (exception is not OutOfMemoryException)
		{
			return null;
		}

		if (string.IsNullOrEmpty(raw))
		{
			return null;
		}

		// StbTrueTypeNext currently exposes the UTF-16BE bytes of Microsoft name records as one
		// char per byte. That form contains NUL bytes for Latin names; already-decoded strings do
		// not contain control characters. Decode only the byte form so a future fix stays compatible.
		if (raw.Any(c => c < ' '))
		{
			var bytes = new byte[raw.Length];
			for (var i = 0; i < raw.Length; i++)
			{
				if (raw[i] > 0xFF)
				{
					return null;
				}

				bytes[i] = (byte)raw[i];
			}

			var decoded = Encoding.BigEndianUnicode.GetString(bytes).Trim('\0', ' ');
			if (decoded.Length > 0 && !decoded.Contains('\uFFFD'))
			{
				return decoded;
			}

			return null;
		}

		var trimmed = raw.Trim('\0', ' ');
		return trimmed.Length > 0 ? trimmed : null;
	}
}
