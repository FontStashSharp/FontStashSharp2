#if PLATFORM_AGNOSTIC
namespace FontStashSharp2;

/// <summary>
/// Creates and updates the texture objects used by a platform-agnostic font system.
/// </summary>
/// <remarks>
/// The engine supplies its own texture type as <see langword="object"/>. Glyph uploads are RGBA
/// bytes in row-major order, matching the bounds passed to <see cref="SetTextureData"/>.
/// </remarks>
public interface ITexture2DManager
{
	/// <summary>
	/// Creates an empty texture with the requested dimensions.
	/// </summary>
	object CreateTexture(int width, int height);

	/// <summary>
	/// Gets the dimensions of a texture created by this manager.
	/// </summary>
	Point GetTextureSize(object texture);

	/// <summary>
	/// Uploads RGBA pixels into a region of a texture created by this manager.
	/// </summary>
	/// <param name="texture">The destination texture.</param>
	/// <param name="bounds">The destination region in pixels.</param>
	/// <param name="data">Row-major RGBA bytes; the length is <c>bounds.Width * bounds.Height * 4</c>.</param>
	void SetTextureData(object texture, Rectangle bounds, ReadOnlySpan<byte> data);

	/// <summary>
	/// Releases a texture previously created by this manager.
	/// </summary>
	void DestroyTexture(object texture);
}

/// <summary>
/// A 32-bit RGBA color. Channel order matches the byte layout of common game-engine colors.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
	/// <summary>
	/// Creates a color from its channels.
	/// </summary>
	public Color(byte r, byte g, byte b, byte a = 255)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	/// <summary>Gets the red channel.</summary>
	public byte R { get; }

	/// <summary>Gets the green channel.</summary>
	public byte G { get; }

	/// <summary>Gets the blue channel.</summary>
	public byte B { get; }

	/// <summary>Gets the alpha channel.</summary>
	public byte A { get; }

	/// <summary>Gets all channels packed as R | G &lt;&lt; 8 | B &lt;&lt; 16 | A &lt;&lt; 24.</summary>
	public uint PackedValue => R | ((uint)G << 8) | ((uint)B << 16) | ((uint)A << 24);

	/// <summary>Gets a fully transparent color.</summary>
	public static Color Transparent => new(0, 0, 0, 0);

	/// <summary>Gets opaque white.</summary>
	public static Color White => new(255, 255, 255, 255);

	/// <summary>Gets opaque black.</summary>
	public static Color Black => new(0, 0, 0, 255);

	/// <summary>Gets opaque red.</summary>
	public static Color Red => new(255, 0, 0, 255);

	/// <summary>Gets opaque green.</summary>
	public static Color Green => new(0, 255, 0, 255);

	/// <summary>Gets opaque blue.</summary>
	public static Color Blue => new(0, 0, 255, 255);

	/// <inheritdoc />
	public bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Color other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => (int)PackedValue;

	/// <inheritdoc />
	public override string ToString() => $"{{R:{R} G:{G} B:{B} A:{A}}}";

	/// <summary>Tests two colors for equality.</summary>
	public static bool operator ==(Color left, Color right) => left.Equals(right);

	/// <summary>Tests two colors for inequality.</summary>
	public static bool operator !=(Color left, Color right) => !left.Equals(right);
}

/// <summary>
/// An integer rectangle in pixel coordinates.
/// </summary>
public readonly struct Rectangle : IEquatable<Rectangle>
{
	/// <summary>
	/// Creates a rectangle.
	/// </summary>
	public Rectangle(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	/// <summary>Gets the left coordinate.</summary>
	public int X { get; }

	/// <summary>Gets the top coordinate.</summary>
	public int Y { get; }

	/// <summary>Gets the width.</summary>
	public int Width { get; }

	/// <summary>Gets the height.</summary>
	public int Height { get; }

	/// <summary>Gets the left coordinate.</summary>
	public int Left => X;

	/// <summary>Gets the top coordinate.</summary>
	public int Top => Y;

	/// <summary>Gets the right coordinate.</summary>
	public int Right => X + Width;

	/// <summary>Gets the bottom coordinate.</summary>
	public int Bottom => Y + Height;

	/// <summary>Gets whether the rectangle has no area.</summary>
	public bool IsEmpty => Width == 0 || Height == 0;

	/// <summary>Gets an empty rectangle.</summary>
	public static Rectangle Empty => new(0, 0, 0, 0);

	/// <inheritdoc />
	public bool Equals(Rectangle other) =>
		X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

	/// <inheritdoc />
	public override string ToString() => $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";

	/// <summary>Tests two rectangles for equality.</summary>
	public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);

	/// <summary>Tests two rectangles for inequality.</summary>
	public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);
}

/// <summary>
/// An integer point in pixel coordinates.
/// </summary>
public readonly struct Point : IEquatable<Point>
{
	/// <summary>
	/// Creates a point.
	/// </summary>
	public Point(int x, int y)
	{
		X = x;
		Y = y;
	}

	/// <summary>Gets the X coordinate.</summary>
	public int X { get; }

	/// <summary>Gets the Y coordinate.</summary>
	public int Y { get; }

	/// <inheritdoc />
	public bool Equals(Point other) => X == other.X && Y == other.Y;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Point other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(X, Y);

	/// <inheritdoc />
	public override string ToString() => $"{{X:{X} Y:{Y}}}";

	/// <summary>Tests two points for equality.</summary>
	public static bool operator ==(Point left, Point right) => left.Equals(right);

	/// <summary>Tests two points for inequality.</summary>
	public static bool operator !=(Point left, Point right) => !left.Equals(right);
}

/// <summary>
/// Mirroring flags applied to a glyph quad.
/// </summary>
[Flags]
public enum SpriteEffects
{
	/// <summary>No mirroring.</summary>
	None = 0,

	/// <summary>Mirror the text block horizontally.</summary>
	FlipHorizontally = 1,

	/// <summary>Mirror the text block vertically.</summary>
	FlipVertically = 2,
}

/// <summary>
/// A vertex with a screen-space position, color, and normalized texture coordinate.
/// </summary>
public struct VertexPositionColorTexture
{
	/// <summary>
	/// Creates a vertex.
	/// </summary>
	public VertexPositionColorTexture(Vector3 position, Color color, Vector2 textureCoordinate)
	{
		Position = position;
		Color = color;
		TextureCoordinate = textureCoordinate;
	}

	/// <summary>Gets or sets the position. Z is the layer depth.</summary>
	public Vector3 Position { get; set; }

	/// <summary>Gets or sets the color.</summary>
	public Color Color { get; set; }

	/// <summary>Gets or sets the normalized texture coordinate.</summary>
	public Vector2 TextureCoordinate { get; set; }
}
#endif
