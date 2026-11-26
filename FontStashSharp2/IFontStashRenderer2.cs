#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FontStashSharp2;

#if MONOGAME
/// <summary>
/// Receives fully formed glyph quads when text is drawn through a vertex-level renderer.
/// </summary>
/// <remarks>
/// This is the lowest-level text drawing path. The font supplies screen-space positions, texture
/// coordinates, colors, and layer depth already applied. Implementations are expected to submit the
/// four vertices as two triangles (for example top-left, top-right, bottom-left and top-right,
/// bottom-right, bottom-left).
/// </remarks>
public interface IFontStashRenderer2
{
	/// <summary>
	/// Gets the graphics device associated with this renderer.
	/// </summary>
	GraphicsDevice GraphicsDevice { get; }

	/// <summary>
	/// Draws one glyph quad.
	/// </summary>
	/// <param name="texture">The atlas texture that owns the quad's texture coordinates.</param>
	/// <param name="topLeft">Top-left vertex.</param>
	/// <param name="topRight">Top-right vertex.</param>
	/// <param name="bottomLeft">Bottom-left vertex.</param>
	/// <param name="bottomRight">Bottom-right vertex.</param>
	/// <remarks>
	/// <see cref="VertexPositionColorTexture.Position"/> is in screen space; Z is the layer depth.
	/// Texture coordinates already include <see cref="SpriteEffects"/> mirroring. Color is the
	/// requested color mask.
	/// </remarks>
	void DrawQuad(
		Texture2D texture,
		ref VertexPositionColorTexture topLeft,
		ref VertexPositionColorTexture topRight,
		ref VertexPositionColorTexture bottomLeft,
		ref VertexPositionColorTexture bottomRight);
}
#else
/// <summary>
/// Receives fully formed glyph quads for an engine-neutral vertex renderer.
/// </summary>
/// <remarks>
/// The font supplies screen-space positions, texture coordinates, colors, and layer depth already
/// applied. Implementations are expected to submit the four vertices as two triangles (for example
/// top-left, top-right, bottom-left and top-right, bottom-right, bottom-left).
/// </remarks>
public interface IFontStashRenderer2
{
	/// <summary>
	/// Gets the texture manager used to create and upload font atlas textures.
	/// </summary>
	ITexture2DManager TextureManager { get; }

	/// <summary>
	/// Draws one glyph quad.
	/// </summary>
	/// <param name="texture">The atlas texture that owns the quad's texture coordinates.</param>
	/// <param name="topLeft">Top-left vertex.</param>
	/// <param name="topRight">Top-right vertex.</param>
	/// <param name="bottomLeft">Bottom-left vertex.</param>
	/// <param name="bottomRight">Bottom-right vertex.</param>
	/// <remarks>
	/// <see cref="VertexPositionColorTexture.Position"/> is in screen space; Z is the layer depth.
	/// Texture coordinates already include <see cref="SpriteEffects"/> mirroring. Color is the
	/// requested color mask.
	/// </remarks>
	void DrawQuad(
		object texture,
		ref VertexPositionColorTexture topLeft,
		ref VertexPositionColorTexture topRight,
		ref VertexPositionColorTexture bottomLeft,
		ref VertexPositionColorTexture bottomRight);
}
#endif
