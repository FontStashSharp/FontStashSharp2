namespace FontStashSharp2.Tests;

public sealed class ShelfPackerTests
{
	[Fact]
	public void PacksGlyphsLeftToRight()
	{
		var packer = new Internal.ShelfPacker(10, 10);

		Assert.True(packer.TryPack(4, 3, out var x1, out var y1));
		Assert.True(packer.TryPack(5, 3, out var x2, out var y2));

		Assert.Equal(0, x1);
		Assert.Equal(0, y1);
		Assert.Equal(4, x2);
		Assert.Equal(0, y2);
	}

	[Fact]
	public void StartsANewShelfWhenTheRowIsFull()
	{
		var packer = new Internal.ShelfPacker(10, 10);

		Assert.True(packer.TryPack(6, 4, out _, out _));
		Assert.True(packer.TryPack(9, 2, out var x, out var y));

		Assert.Equal(0, x);
		Assert.Equal(4, y);
	}

	[Fact]
	public void FailsWhenThePageIsFull()
	{
		var packer = new Internal.ShelfPacker(10, 10);

		Assert.True(packer.TryPack(10, 8, out _, out _));
		Assert.False(packer.TryPack(10, 3, out _, out _));
	}

	[Fact]
	public void FailedPack_DoesNotChangePackerState()
	{
		var packer = new Internal.ShelfPacker(10, 10);

		Assert.True(packer.TryPack(10, 8, out _, out _));
		Assert.False(packer.TryPack(10, 3, out _, out _));

		// A smaller rectangle still fits in the remaining two rows.
		Assert.True(packer.TryPack(10, 2, out var x, out var y));
		Assert.Equal(0, x);
		Assert.Equal(8, y);
	}

	[Fact]
	public void RejectsRectanglesLargerThanThePage()
	{
		var packer = new Internal.ShelfPacker(8, 8);

		Assert.False(packer.TryPack(9, 1, out _, out _));
		Assert.False(packer.TryPack(1, 9, out _, out _));
	}

	[Theory]
	[InlineData(0, 4)]
	[InlineData(4, 0)]
	[InlineData(-1, 4)]
	[InlineData(4, -1)]
	public void RejectsNonPositiveRectangles(int width, int height)
	{
		var packer = new Internal.ShelfPacker(8, 8);

		Assert.False(packer.TryPack(width, height, out _, out _));
	}

	[Theory]
	[InlineData(0, 4)]
	[InlineData(4, 0)]
	[InlineData(-1, 4)]
	[InlineData(4, -1)]
	public void ConstructorRejectsNonPositivePageSize(int width, int height)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Internal.ShelfPacker(width, height));
	}
}
