#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FontStashSharp2;

#if MONOGAME
/// <summary>
/// Receives glyph quads when text is drawn without <see cref="SpriteBatch"/>.
/// </summary>
/// <remarks>
/// Implement this interface to feed FontStashSharp2 glyphs into another renderer. The font supplies
/// atlas textures, source rectangles, colors, transforms, and sprite effects. Implementations should
/// draw the source rectangle at the top-left position, rotating and scaling around that point, and
/// honor the sprite effects and layer depth.
/// </remarks>
public interface IFontStashRenderer
{
	/// <summary>
	/// Gets the graphics device associated with this renderer.
	/// </summary>
	GraphicsDevice GraphicsDevice { get; }

	/// <summary>
	/// Draws one glyph quad.
	/// </summary>
	/// <param name="texture">The font atlas texture that owns <paramref name="sourceRectangle"/>.</param>
	/// <param name="position">The screen-space top-left of the destination glyph quad.</param>
	/// <param name="sourceRectangle">The glyph region inside <paramref name="texture"/>.</param>
	/// <param name="color">The color mask.</param>
	/// <param name="rotation">Rotation, in radians, around the quad's top-left corner.</param>
	/// <param name="scale">Horizontal and vertical quad scale.</param>
	/// <param name="effects">UV mirroring flags for the glyph.</param>
	/// <param name="layerDepth">The layer depth for the draw.</param>
	void Draw(
		Texture2D texture,
		Vector2 position,
		Rectangle? sourceRectangle,
		Color color,
		float rotation,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth);
}
#else
/// <summary>
/// Receives glyph quads for an engine-neutral renderer.
/// </summary>
/// <remarks>
/// The font creates its atlas through <see cref="TextureManager"/> and submits one call per visible
/// glyph. Implementations should draw <c>sourceRectangle</c> at the top-left position,
/// rotating and scaling around that point, and honor the sprite effects and layer depth.
/// </remarks>
public interface IFontStashRenderer
{
	/// <summary>
	/// Gets the texture manager used to create and upload font atlas textures.
	/// </summary>
	ITexture2DManager TextureManager { get; }

	/// <summary>
	/// Draws one glyph quad.
	/// </summary>
	/// <param name="texture">The font atlas texture that owns <paramref name="sourceRectangle"/>.</param>
	/// <param name="position">The screen-space top-left of the destination glyph quad.</param>
	/// <param name="sourceRectangle">The glyph region inside <paramref name="texture"/>.</param>
	/// <param name="color">The color mask.</param>
	/// <param name="rotation">Rotation, in radians, around the quad's top-left corner.</param>
	/// <param name="scale">Horizontal and vertical quad scale.</param>
	/// <param name="effects">UV mirroring flags for the glyph.</param>
	/// <param name="layerDepth">The layer depth for the draw.</param>
	void DrawText(
		object texture,
		Vector2 position,
		Rectangle? sourceRectangle,
		Color color,
		float rotation,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth);
}
#endif
