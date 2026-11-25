using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbTrueTypeNext;

namespace BenchmarkSample;

/// <summary>
/// Bakes a regular MonoGame <see cref="SpriteFont"/> from the same TTF bytes and font size used by
/// FontStashSharp2. Both renderers therefore start with a pre-baked glyph atlas; only DrawString is
/// measured.
/// </summary>
internal static class RuntimeSpriteFontBuilder
{
	private const int PageSize = 1024;
	private const int GlyphPadding = 1;

	public static SpriteFont Build(GraphicsDevice graphicsDevice, byte[] fontData, float fontSize, string glyphSet)
	{
		ArgumentNullException.ThrowIfNull(graphicsDevice);
		ArgumentNullException.ThrowIfNull(fontData);
		ArgumentNullException.ThrowIfNull(glyphSet);

		var font = new STBFont(fontData, 0);
		font.GetFontVMetrics(out var ascent, out var descent, out var lineGap);

		var scale = font.ScaleForMappingEmToPixels(fontSize);
		var ascentPixels = (int)MathF.Round(ascent * scale);
		var lineSpacing = Math.Max(1, (int)MathF.Round((ascent - descent + lineGap) * scale));

		var characters = glyphSet
			.Where(c => c >= ' ')
			.Distinct()
			.OrderBy(c => c)
			.ToList();

		if (!characters.Contains('?'))
		{
			characters.Add('?');
			characters.Sort();
		}

		var pixels = new byte[PageSize * PageSize * 4];
		var glyphBounds = new List<Rectangle>(characters.Count);
		var cropping = new List<Rectangle>(characters.Count);
		var kerning = new List<Vector3>(characters.Count);

		var penX = GlyphPadding;
		var penY = GlyphPadding;
		var rowHeight = 0;

		foreach (var character in characters)
		{
			var glyphIndex = font.FindGlyphIndex(character);
			font.GetGlyphHMetrics(glyphIndex, out var advanceFontUnits, out _);
			var advance = advanceFontUnits * scale;

			var bitmap = glyphIndex != 0
				? font.CreateGlyphBitmap(glyphIndex, scale, scale, 0f, 0f, STBRasterizerVersion.V2)
				: null;

			if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
			{
				// Whitespace and zero-area glyphs still advance the pen. SpriteFont will submit a
				// zero-sized quad for them, exactly as it does for a content-pipeline font.
				glyphBounds.Add(Rectangle.Empty);
				cropping.Add(new Rectangle(0, ascentPixels, 0, 0));
				kerning.Add(new Vector3(0f, 0f, advance));
				continue;
			}

			var width = bitmap.Width;
			var height = bitmap.Height;
			var slotWidth = width + (GlyphPadding * 2);
			var slotHeight = height + (GlyphPadding * 2);

			if (penX + slotWidth > PageSize - GlyphPadding)
			{
				penX = GlyphPadding;
				penY += rowHeight;
				rowHeight = 0;
			}

			if (penY + slotHeight > PageSize - GlyphPadding)
			{
				throw new InvalidOperationException(
					$"The SpriteFont atlas is too small for all glyphs at {fontSize:0.##}px.");
			}

			var slotX = penX;
			var slotY = penY;
			WritePaddedGlyph(pixels, bitmap, slotX, slotY, slotWidth, slotHeight);
			penX += slotWidth;
			rowHeight = Math.Max(rowHeight, slotHeight);

			var leftSideBearing = bitmap.XOffset;
			glyphBounds.Add(new Rectangle(slotX + GlyphPadding, slotY + GlyphPadding, width, height));
			cropping.Add(new Rectangle(0, ascentPixels + bitmap.YOffset, width, height));
			kerning.Add(new Vector3(leftSideBearing, width, advance - leftSideBearing - width));
		}

		var texture = new Texture2D(graphicsDevice, PageSize, PageSize, false, SurfaceFormat.Color);
		texture.SetData(pixels);

		return new SpriteFont(
			texture,
			glyphBounds,
			cropping,
			characters,
			lineSpacing,
			0f,
			kerning,
			'?');
	}

	private static void WritePaddedGlyph(
		byte[] destination,
		STBGlyphBitmap bitmap,
		int slotX,
		int slotY,
		int slotWidth,
		int slotHeight)
	{
		for (var y = 0; y < slotHeight; y++)
		{
			var sourceY = Math.Clamp(y - GlyphPadding, 0, bitmap.Height - 1);
			var sourceRow = bitmap.BitmapOffset + (sourceY * bitmap.Width);

			for (var x = 0; x < slotWidth; x++)
			{
				var sourceX = Math.Clamp(x - GlyphPadding, 0, bitmap.Width - 1);
				var alpha = bitmap.Bitmap[sourceRow + sourceX];
				var offset = (((slotY + y) * PageSize) + slotX + x) * 4;

				destination[offset] = alpha;
				destination[offset + 1] = alpha;
				destination[offset + 2] = alpha;
				destination[offset + 3] = alpha;
			}
		}
	}
}
