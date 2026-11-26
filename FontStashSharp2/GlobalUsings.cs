#if MONOGAME
global using Vector2 = Microsoft.Xna.Framework.Vector2;
global using Vector3 = Microsoft.Xna.Framework.Vector3;
global using Color = Microsoft.Xna.Framework.Color;
global using Rectangle = Microsoft.Xna.Framework.Rectangle;
global using Point = Microsoft.Xna.Framework.Point;
global using SpriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects;
global using VertexPositionColorTexture = Microsoft.Xna.Framework.Graphics.VertexPositionColorTexture;
global using AtlasTexture = Microsoft.Xna.Framework.Graphics.Texture2D;
global using AtlasDevice = Microsoft.Xna.Framework.Graphics.GraphicsDevice;
#else
global using Vector2 = System.Numerics.Vector2;
global using Vector3 = System.Numerics.Vector3;
global using Color = FontStashSharp2.Color;
global using Rectangle = FontStashSharp2.Rectangle;
global using Point = FontStashSharp2.Point;
global using SpriteEffects = FontStashSharp2.SpriteEffects;
global using VertexPositionColorTexture = FontStashSharp2.VertexPositionColorTexture;
global using AtlasTexture = object;
global using AtlasDevice = FontStashSharp2.ITexture2DManager;
#endif
