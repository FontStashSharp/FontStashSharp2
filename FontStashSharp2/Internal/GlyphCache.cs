using System.Runtime.CompilerServices;
using StbTrueTypeNext;

namespace FontStashSharp2.Internal;

/// <summary>
/// Resolves code points through the font fallback chain and caches advance widths, kerning pairs,
/// and rasterized glyphs for one font size.
/// </summary>
/// <remarks>
/// ASCII code points are stored in small arrays because they dominate typical UI text. Non-ASCII
/// code points use dictionaries. The ASCII kerning table is a 128x128 row-major array initialized
/// to <see cref="float.NaN"/>; pairs are computed lazily. Bitmaps are rasterized at
/// <c>fontScale * SupersamplingFactor</c>; their offsets are converted back to logical pixels.
/// </remarks>
internal sealed class GlyphCache
{
	private const int AsciiCount = 128;
	private const int AsciiKernelsLength = AsciiCount * AsciiCount;

	private readonly FontSource[] _sources;
	private readonly float[] _scales;
	private readonly bool _useKerning;
	private readonly float _supersamplingFactor;
	private readonly Dictionary<int, GlyphMetrics> _metrics = new();
	private readonly Dictionary<int, RenderedGlyph> _rendered = new();
	private readonly Dictionary<long, float> _kernings = new();
	private readonly GlyphMetrics[] _asciiMetrics = new GlyphMetrics[AsciiCount];
	private readonly bool[] _asciiMetricsCached = new bool[AsciiCount];
	private readonly RenderedGlyph[] _asciiRendered = new RenderedGlyph[AsciiCount];
	private readonly bool[] _asciiRenderedCached = new bool[AsciiCount];
	private readonly float[]? _asciiKernings;
	private int _metricsCount;
	private int _rasterizedGlyphCount;

	public GlyphCache(FontSource[] sources, float[] scales, bool useKerning, float supersamplingFactor)
	{
		ArgumentNullException.ThrowIfNull(sources);
		ArgumentNullException.ThrowIfNull(scales);

		if (sources.Length == 0)
		{
			throw new ArgumentException("At least one font source is required.", nameof(sources));
		}

		if (sources.Length != scales.Length)
		{
			throw new ArgumentException("A scale is required for every font source.", nameof(scales));
		}

		if (!float.IsFinite(supersamplingFactor) || supersamplingFactor < 1f)
		{
			throw new ArgumentOutOfRangeException(
				nameof(supersamplingFactor),
				supersamplingFactor,
				"Supersampling factor must be finite and at least 1.0.");
		}

		_sources = sources;
		_scales = scales;
		_useKerning = useKerning;
		_supersamplingFactor = supersamplingFactor;

		if (useKerning)
		{
			_asciiKernings = new float[AsciiKernelsLength];
			Array.Fill(_asciiKernings, float.NaN);
		}
	}

	/// <summary>Gets the number of code points whose advance/fallback resolution has been cached.</summary>
	public int MetricsCount => _metricsCount;

	/// <summary>Gets the number of glyphs that have been uploaded to the atlas.</summary>
	public int RasterizedGlyphCount => _rasterizedGlyphCount;

	/// <summary>Drops every rasterized glyph while keeping metrics and kerning caches.</summary>
	public void ClearRenderedGlyphs()
	{
		Array.Clear(_asciiRendered);
		Array.Clear(_asciiRenderedCached);
		_rendered.Clear();
		_rasterizedGlyphCount = 0;
	}

	public GlyphMetrics GetMetrics(int codepoint)
	{
		if ((uint)codepoint < AsciiCount)
		{
			if (_asciiMetricsCached[codepoint])
			{
				return _asciiMetrics[codepoint];
			}

			var metrics = Resolve(codepoint);
			_asciiMetrics[codepoint] = metrics;
			_asciiMetricsCached[codepoint] = true;
			_metricsCount++;
			return metrics;
		}

		if (_metrics.TryGetValue(codepoint, out var cached))
		{
			return cached;
		}

		var resolved = Resolve(codepoint);
		_metrics.Add(codepoint, resolved);
		_metricsCount++;
		return resolved;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetRenderedGlyph(int codepoint, out RenderedGlyph glyph)
	{
		if ((uint)codepoint < AsciiCount)
		{
			if (_asciiRenderedCached[codepoint])
			{
				glyph = _asciiRendered[codepoint];
				return true;
			}

			glyph = default;
			return false;
		}

		return _rendered.TryGetValue(codepoint, out glyph);
	}

	public RenderedGlyph GetRenderedGlyph(int codepoint, IGlyphAtlas atlas, AtlasDevice? device)
	{
		ArgumentNullException.ThrowIfNull(atlas);

		if (TryGetRenderedGlyph(codepoint, out var glyph))
		{
			return glyph;
		}

		var metrics = GetMetrics(codepoint);
		var source = _sources[metrics.SourceIndex];
		var scale = _scales[metrics.SourceIndex];
		var rasterScale = scale * _supersamplingFactor;

		var bitmap = rasterScale > 0f && float.IsFinite(rasterScale)
			? source.Font.CreateGlyphBitmap(
				metrics.GlyphIndex,
				rasterScale,
				rasterScale,
				0f,
				0f,
				STBRasterizerVersion.V2)
			: null;

		if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
		{
			glyph = new RenderedGlyph(metrics, -1, Rectangle.Empty, 0, 0);
		}
		else
		{
			var length = checked(bitmap.Width * bitmap.Height);
			if (bitmap.Bitmap is null || bitmap.BitmapOffset < 0 || bitmap.BitmapOffset + length > bitmap.Bitmap.Length)
			{
				throw new InvalidOperationException("The rasterizer returned an invalid glyph bitmap.");
			}

			var alpha = bitmap.Bitmap.AsSpan(bitmap.BitmapOffset, length);
			var placement = atlas.Add(device, alpha, bitmap.Width, bitmap.Height);
			glyph = new RenderedGlyph(
				metrics,
				placement.TextureIndex,
				placement.Bounds,
				bitmap.XOffset / _supersamplingFactor,
				bitmap.YOffset / _supersamplingFactor);
			_rasterizedGlyphCount++;
		}

		if ((uint)codepoint < AsciiCount)
		{
			_asciiRendered[codepoint] = glyph;
			_asciiRenderedCached[codepoint] = true;
		}
		else
		{
			_rendered.Add(codepoint, glyph);
		}

		return glyph;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetKerning(GlyphMetrics left, GlyphMetrics right)
	{
		if (!_useKerning || left.SourceIndex != right.SourceIndex)
		{
			return 0f;
		}

		if ((uint)left.Codepoint < AsciiCount && (uint)right.Codepoint < AsciiCount)
		{
			var index = (left.Codepoint << 7) | right.Codepoint;
			var value = _asciiKernings![index];
			return float.IsNaN(value) ? CacheAsciiKerning(left, right, index) : value;
		}

		return GetKerningSlow(left, right);
	}

	private float CacheAsciiKerning(GlyphMetrics left, GlyphMetrics right, int index)
	{
		var value = ComputeKerning(left, right);
		_asciiKernings![index] = value;
		return value;
	}

	private float GetKerningSlow(GlyphMetrics left, GlyphMetrics right)
	{
		var key = ((long)left.GlyphIndex << 32) | (uint)right.GlyphIndex;
		if (_kernings.TryGetValue(key, out var kerning))
		{
			return kerning;
		}

		kerning = ComputeKerning(left, right);
		_kernings.Add(key, kerning);
		return kerning;
	}

	private float ComputeKerning(GlyphMetrics left, GlyphMetrics right)
	{
		var fontUnits = _sources[left.SourceIndex].GetKerningAdvance(left.GlyphIndex, right.GlyphIndex);
		return fontUnits * _scales[left.SourceIndex];
	}

	private GlyphMetrics Resolve(int codepoint)
	{
		for (var i = 0; i < _sources.Length; i++)
		{
			var glyphIndex = _sources[i].GetGlyphIndex(codepoint);
			if (glyphIndex != 0)
			{
				return CreateMetrics(i, glyphIndex, codepoint);
			}
		}

		// No font has the code point: render the first font's .notdef glyph, which is normally a box.
		return CreateMetrics(0, 0, codepoint);
	}

	private GlyphMetrics CreateMetrics(int sourceIndex, int glyphIndex, int codepoint)
	{
		var advance = _sources[sourceIndex].GetAdvanceWidth(glyphIndex) * _scales[sourceIndex];
		return new GlyphMetrics(codepoint, sourceIndex, glyphIndex, advance);
	}
}
