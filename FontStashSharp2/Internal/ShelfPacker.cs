namespace FontStashSharp2.Internal;

/// <summary>
/// A minimal shelf (row) packer for one atlas page. It never reuses space and is deterministic:
/// glyphs are placed left-to-right in rows whose height is the tallest glyph seen in that row.
/// </summary>
internal sealed class ShelfPacker
{
	private readonly int _width;
	private readonly int _height;
	private int _x;
	private int _y;
	private int _rowHeight;

	public ShelfPacker(int width, int height)
	{
		if (width <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(width), width, "Page width must be positive.");
		}

		if (height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(height), height, "Page height must be positive.");
		}

		_width = width;
		_height = height;
	}

	/// <summary>
	/// Attempts to reserve a rectangle. The packer's state is unchanged when the rectangle does not fit.
	/// </summary>
	/// <remarks>
	/// Placement is a single pass: rows are consumed top to bottom and no previously used space is reused.
	/// </remarks>
	public bool TryPack(int width, int height, out int x, out int y)
	{
		x = 0;
		y = 0;

		if (width <= 0 || height <= 0 || width > _width || height > _height)
		{
			return false;
		}

		// Start a new shelf when the current one is too narrow.
		var candidateX = _x;
		var candidateY = _y;
		var candidateRowHeight = _rowHeight;

		if (candidateX + width > _width)
		{
			candidateX = 0;
			candidateY += candidateRowHeight;
			candidateRowHeight = 0;
		}

		if (candidateY + height > _height)
		{
			return false;
		}

		x = candidateX;
		y = candidateY;

		_x = candidateX + width;
		_y = candidateY;
		_rowHeight = Math.Max(candidateRowHeight, height);

		return true;
	}
}
