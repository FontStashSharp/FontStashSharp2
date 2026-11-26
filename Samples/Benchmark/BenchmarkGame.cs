using System.Diagnostics;
using System.Globalization;
using FontStashSharp2;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BenchmarkSample;

/// <summary>
/// Compares steady-state <c>SpriteBatch.DrawString</c> throughput between FontStashSharp2 and a
/// regular MonoGame <see cref="SpriteFont"/> built from the same TTF and pixel size.
/// </summary>
internal sealed class BenchmarkGame : Game
{
	private const int Width = 1280;
	private const int Height = 720;
	private const string SampleText = "The quick brown fox jumps over the lazy dog. 0123456789 AV To WA.";

	private static readonly string GlyphSet = new(
		Enumerable.Range(32, 95).Select(static c => (char)c).ToArray());

	private static readonly Color Background = new(16, 18, 24);
	private static readonly Color Panel = new(24, 27, 36);
	private static readonly Color Line = new(48, 53, 66);
	private static readonly Color Text = new(238, 241, 247);
	private static readonly Color Muted = new(146, 154, 171);
	private static readonly Color Accent = new(108, 214, 190);
	private static readonly Color Warm = new(255, 190, 120);
	private static readonly Color SpriteFontColor = new(140, 200, 255);

	private readonly BenchmarkOptions _options;
	private readonly GraphicsDeviceManager _graphics;

	private SpriteBatch _batch = null!;
	private Texture2D _pixel = null!;
	private FontSystem _fontSystem = null!;
	private DynamicSpriteFont _ourFont = null!;
	private SpriteFont _spriteFont = null!;
	private RenderTarget2D _cpuTarget = null!;
	private RenderTarget2D _fullTarget = null!;
	private Vector2[] _positions = Array.Empty<Vector2>();

	private ModeResults _ours = new();
	private ModeResults _spriteFontResults = new();
	private bool _benchmarkComplete;
	private int _screenshotFrames;

	public BenchmarkGame(BenchmarkOptions options)
	{
		_options = options;

		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = Width,
			PreferredBackBufferHeight = Height,
			SynchronizeWithVerticalRetrace = false,
		};

		IsFixedTimeStep = true;
		TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
		IsMouseVisible = true;
		Window.AllowUserResizing = false;
		Window.Title = $"FontStashSharp2 DrawString benchmark - {options.FontSize:0.#}px";
	}

	protected override void LoadContent()
	{
		_batch = new SpriteBatch(
			GraphicsDevice,
			Math.Max(2048, (_options.Texts * SampleText.Length) + 512));

		_pixel = new Texture2D(GraphicsDevice, 1, 1);
		_pixel.SetData(new[] { Color.White });

		_fontSystem = new FontSystem(
			new FontSystemSettings
			{
				TextureWidth = 1024,
				TextureHeight = 1024,
				GlyphPadding = 1,
				SupersamplingFactor = 1f,
				// Match the runtime SpriteFont, which uses em-to-pixel scaling.
				ScaleMode = FontScaleMode.ScaleForMappingEmToPixels,
				UseKerning = !_options.DisableKerning,
			});

		var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "ABeeZee-Regular.ttf");
		var fontData = File.ReadAllBytes(fontPath);

		_fontSystem.AddFont(fontData, name: "ABeeZee");
		_ourFont = _fontSystem.GetFont(_options.FontSize);
		_spriteFont = RuntimeSpriteFontBuilder.Build(GraphicsDevice, fontData, _options.FontSize, GlyphSet);

		_cpuTarget = new RenderTarget2D(
			GraphicsDevice,
			64,
			64,
			mipMap: false,
			SurfaceFormat.Color,
			DepthFormat.None);

		_fullTarget = new RenderTarget2D(
			GraphicsDevice,
			Width,
			Height,
			mipMap: false,
			SurfaceFormat.Color,
			DepthFormat.None);

		_positions = CreatePositions(_options.Texts);

		base.LoadContent();
	}

	protected override void Update(GameTime gameTime)
	{
		if (Keyboard.GetState().IsKeyDown(Keys.Escape))
		{
			Exit();
		}

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		if (!_benchmarkComplete)
		{
			_benchmarkComplete = true;
			RunBenchmark();
			PrintResults();

			if (_options.ScreenshotPath is null && _options.AutoExit)
			{
				Exit();
				return;
			}
		}

		if (_options.ScreenshotPath is not null)
		{
			DrawScreenshotFrame();
			return;
		}

		GraphicsDevice.SetRenderTarget(null);
		GraphicsDevice.Clear(Background);

		_batch.Begin(
			sortMode: SpriteSortMode.Deferred,
			blendState: BlendState.AlphaBlend,
			samplerState: SamplerState.LinearClamp);

		DrawResults(_batch);

		_batch.End();
		base.Draw(gameTime);
	}

	private void RunBenchmark()
	{
		Console.WriteLine(
			$"Running DrawString benchmark: {_options.Texts} strings/frame, " +
			$"{SampleText.Length} chars/string, {_options.Rounds} rounds x {_options.Iterations} frames...");

		Warmup(FontKind.FontStashSharp2, BenchmarkTarget.Cpu);
		Warmup(FontKind.SpriteFont, BenchmarkTarget.Cpu);
		Warmup(FontKind.FontStashSharp2, BenchmarkTarget.Full);
		Warmup(FontKind.SpriteFont, BenchmarkTarget.Full);

		var ours = new ModeResults();
		var spriteFont = new ModeResults();

		for (var round = 0; round < _options.Rounds; round++)
		{
			// Alternate the order so thermal/cache effects do not consistently favor one path.
			var order = (round & 1) == 0
				? new[] { FontKind.FontStashSharp2, FontKind.SpriteFont }
				: new[] { FontKind.SpriteFont, FontKind.FontStashSharp2 };

			foreach (var kind in order)
			{
				foreach (var target in new[] { BenchmarkTarget.Cpu, BenchmarkTarget.Full })
				{
					var result = Measure(kind, target, _options.Iterations);
					(kind == FontKind.FontStashSharp2 ? ours : spriteFont)[target].Add(result);
				}
			}
		}

		_ours = ours;
		_spriteFontResults = spriteFont;
		GraphicsDevice.SetRenderTarget(null);
	}

	private void Warmup(FontKind kind, BenchmarkTarget target)
	{
		GraphicsDevice.SetRenderTarget(GetTarget(target));
		GraphicsDevice.Clear(Color.Transparent);

		for (var i = 0; i < _options.WarmupFrames; i++)
		{
			_batch.Begin(
				sortMode: SpriteSortMode.Deferred,
				blendState: BlendState.AlphaBlend,
				samplerState: SamplerState.LinearClamp);
			DrawBenchmarkText(kind);
			_batch.End();
		}
	}

	private PhaseResult Measure(FontKind kind, BenchmarkTarget target, int iterations)
	{
		GraphicsDevice.SetRenderTarget(GetTarget(target));
		GraphicsDevice.Clear(Color.Transparent);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		var started = Stopwatch.GetTimestamp();

		for (var i = 0; i < iterations; i++)
		{
			_batch.Begin(
				sortMode: SpriteSortMode.Deferred,
				blendState: BlendState.AlphaBlend,
				samplerState: SamplerState.LinearClamp);
			DrawBenchmarkText(kind);
			_batch.End();
		}

		var elapsed = Stopwatch.GetElapsedTime(started);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		return new PhaseResult(elapsed.TotalMilliseconds, iterations, allocated);
	}

	private void DrawBenchmarkText(FontKind kind)
	{
		if (kind == FontKind.FontStashSharp2)
		{
			foreach (var position in _positions)
			{
				_batch.DrawString(_ourFont, SampleText, position, Color.White);
			}
		}
		else
		{
			foreach (var position in _positions)
			{
				_batch.DrawString(_spriteFont, SampleText, position, Color.White);
			}
		}
	}

	private RenderTarget2D GetTarget(BenchmarkTarget target)
	{
		return target == BenchmarkTarget.Cpu ? _cpuTarget : _fullTarget;
	}

	private void PrintResults()
	{
		var textLength = SampleText.Length;

		Console.WriteLine();
		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"Font: ABeeZee, {_options.FontSize:0.##}px; kerning {(_options.DisableKerning ? "off" : "on")}; supersampling 1x; {_options.Texts} DrawString calls/frame; {textLength} chars/string."));
		Console.WriteLine(FormattableString.Invariant(
			$"Frames measured: {_options.Rounds * _options.Iterations} per mode/target; warmup: {_options.WarmupFrames}."));
		Console.WriteLine();
		Console.WriteLine("Path                  CPU ms/frame   Full ms/frame   CPU strings/s    CPU glyphs/s   Alloc B/frame");
		PrintRow("FontStashSharp2", _ours);
		PrintRow("SpriteFont", _spriteFontResults);
		Console.WriteLine();
		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"SpriteFont / FontStashSharp2 (higher means FontStashSharp2 is faster): CPU {Ratio(_ours.Cpu, _spriteFontResults.Cpu):0.00}x, Full {Ratio(_ours.Full, _spriteFontResults.Full):0.00}x"));
		Console.WriteLine();
		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"MeasureString: FontStashSharp2 {_ourFont.MeasureString(SampleText).X:0.0}px wide, SpriteFont {_spriteFont.MeasureString(SampleText).X:0.0}px wide."));
		Console.WriteLine("CPU target is 64x64 to isolate submission; Full target is 1280x720 and includes fill.");
		Console.WriteLine("Warmup excludes first-use glyph rasterization and JIT. Vsync is disabled.");
		Console.WriteLine();

		void PrintRow(string name, ModeResults results)
		{
			var stringsPerSecond = results.Cpu.MsPerFrame > 0
				? _options.Texts * 1000.0 / results.Cpu.MsPerFrame
				: 0;
			var glyphsPerSecond = stringsPerSecond * textLength;

			Console.WriteLine(string.Create(
				CultureInfo.InvariantCulture,
				$"{name,-20} {results.Cpu.MsPerFrame,13:0.000} {results.Full.MsPerFrame,15:0.000} {stringsPerSecond,16:0} {glyphsPerSecond,15:0} {results.Cpu.AllocatedBytesPerFrame,15:0}"));
		}
	}

	private void DrawResults(SpriteBatch batch)
	{
		var title = _fontSystem.GetFont(38);
		var section = _fontSystem.GetFont(14);
		var body = _fontSystem.GetFont(15);
		var table = _fontSystem.GetFont(18);

		batch.DrawString(title, "DrawString benchmark", new Vector2(56f, 28f), Text);
		batch.DrawString(
			body,
			string.Create(
				CultureInfo.InvariantCulture,
				$"{_options.FontSize:0.##}px ABeeZee  |  kerning {(_options.DisableKerning ? "off" : "on")}  |  supersampling 1x  |  {_options.Texts} strings/frame  |  {SampleText.Length} chars/string  |  {_options.Rounds} rounds x {_options.Iterations} frames"),
			new Vector2(58f, 80f),
			Muted);
		batch.Draw(_pixel, new Rectangle(56, 112, Width - 112, 1), Line);

		batch.DrawString(section, "RESULTS", new Vector2(56f, 132f), Accent);

		var cpuX = 700f;
		var fullX = 920f;
		var rowY = 166f;

		batch.DrawString(body, "Path", new Vector2(56f, rowY), Muted);
		batch.DrawString(body, "CPU 64x64", new Vector2(cpuX, rowY), Muted);
		batch.DrawString(body, "Full 1280x720", new Vector2(fullX, rowY), Muted);

		rowY += 34f;
		batch.DrawString(table, "FontStashSharp2", new Vector2(56f, rowY), Text);
		batch.DrawString(
			table,
			string.Create(CultureInfo.InvariantCulture, $"{_ours.Cpu.MsPerFrame:0.000} ms"),
			new Vector2(cpuX, rowY),
			Accent);
		batch.DrawString(
			table,
			string.Create(CultureInfo.InvariantCulture, $"{_ours.Full.MsPerFrame:0.000} ms"),
			new Vector2(fullX, rowY),
			Accent);

		rowY += 36f;
		batch.DrawString(table, "SpriteFont", new Vector2(56f, rowY), Text);
		batch.DrawString(
			table,
			string.Create(CultureInfo.InvariantCulture, $"{_spriteFontResults.Cpu.MsPerFrame:0.000} ms"),
			new Vector2(cpuX, rowY),
			SpriteFontColor);
		batch.DrawString(
			table,
			string.Create(CultureInfo.InvariantCulture, $"{_spriteFontResults.Full.MsPerFrame:0.000} ms"),
			new Vector2(fullX, rowY),
			SpriteFontColor);

		rowY += 42f;
		batch.DrawString(
			body,
			string.Create(
				CultureInfo.InvariantCulture,
				$"SpriteFont / FontStashSharp2: CPU {Ratio(_ours.Cpu, _spriteFontResults.Cpu):0.00}x   Full {Ratio(_ours.Full, _spriteFontResults.Full):0.00}x   (higher = FontStashSharp2 faster)"),
			new Vector2(56f, rowY),
			Warm);

		rowY += 34f;
		var stringsPerSecond = _ours.Cpu.MsPerFrame > 0
			? _options.Texts * 1000.0 / _ours.Cpu.MsPerFrame
			: 0;
		batch.DrawString(
			body,
			string.Create(
				CultureInfo.InvariantCulture,
				$"FontStashSharp2 CPU throughput: {stringsPerSecond:0} strings/s  ({stringsPerSecond * SampleText.Length:0} glyphs/s)  |  Allocated: {_ours.Cpu.AllocatedBytesPerFrame:0} B/frame"),
			new Vector2(56f, rowY),
			Muted);

		batch.Draw(_pixel, new Rectangle(56, 342, Width - 112, 1), Line);

		batch.DrawString(section, "METHOD", new Vector2(56f, 362f), Accent);
		var kerningNote = _options.DisableKerning
			? "Kerning is disabled for this FontStashSharp2 run."
			: "FontStashSharp2 applies the font's kerning pairs; SpriteFont uses per-glyph bearings only.";

		batch.DrawString(
			body,
			"Both engines parse the same TTF at the same pixel size and use a pre-baked atlas; only DrawString is timed.\n" +
			"FontStashSharp2 supersampling is set to 1x so both paths draw 1:1 glyph atlases; use the HelloWorld sample for 2x quality.\n" +
			"The 64x64 target isolates SpriteBatch submission. The 1280x720 target includes GPU fill/overdraw.\n" +
			"Warmup frames exclude first-use glyph rasterization, JIT, and SpriteBatch growth. Vsync is disabled.\n" +
			kerningNote,
			new Vector2(56f, 388f),
			Muted);

		batch.Draw(_pixel, new Rectangle(56, 512, Width - 112, 1), Line);

		batch.DrawString(section, "VISUAL CHECK", new Vector2(56f, 532f), Accent);
		batch.DrawString(body, "FontStashSharp2", new Vector2(56f, 562f), Muted);
		batch.DrawString(_ourFont, "The quick brown fox jumps.", new Vector2(208f, 558f), Text);
		batch.DrawString(body, "SpriteFont", new Vector2(56f, 596f), Muted);
		batch.DrawString(_spriteFont, "The quick brown fox jumps.", new Vector2(208f, 592f), SpriteFontColor);

		batch.Draw(_pixel, new Rectangle(0, Height - 36, Width, 36), Panel);
		batch.DrawString(
			body,
			"Press Esc to exit.   --benchmark prints results and exits.   --screenshot <path> saves this screen.",
			new Vector2(56f, Height - 24),
			Muted);
	}

	private void DrawScreenshotFrame()
	{
		if (_options.ScreenshotPath is null)
		{
			return;
		}

		GraphicsDevice.SetRenderTarget(_fullTarget);
		GraphicsDevice.Clear(Background);

		_batch.Begin(
			sortMode: SpriteSortMode.Deferred,
			blendState: BlendState.AlphaBlend,
			samplerState: SamplerState.LinearClamp);
		DrawResults(_batch);
		_batch.End();

		GraphicsDevice.SetRenderTarget(null);

		_screenshotFrames++;
		if (_screenshotFrames < 2)
		{
			return;
		}

		var directory = Path.GetDirectoryName(_options.ScreenshotPath);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		using (var stream = File.Create(_options.ScreenshotPath))
		{
			_fullTarget.SaveAsPng(stream, Width, Height);
		}

		Console.WriteLine($"Screenshot written to {_options.ScreenshotPath}");
		Exit();
	}

	protected override void UnloadContent()
	{
		_spriteFont?.Texture?.Dispose();
		_fontSystem?.Dispose();
		_cpuTarget?.Dispose();
		_fullTarget?.Dispose();
		_pixel?.Dispose();
		_batch?.Dispose();

		base.UnloadContent();
	}

	private static Vector2[] CreatePositions(int count)
	{
		const int columns = 4;
		const float columnStep = 310f;
		const float rowStep = 22f;

		var positions = new Vector2[count];
		for (var i = 0; i < positions.Length; i++)
		{
			positions[i] = new Vector2(
				8f + ((i % columns) * columnStep),
				8f + ((i / columns) * rowStep));
		}

		return positions;
	}

	private static double Ratio(Aggregate ours, Aggregate spriteFont)
	{
		return ours.MsPerFrame > 0 ? spriteFont.MsPerFrame / ours.MsPerFrame : 0;
	}

	private enum FontKind
	{
		FontStashSharp2,
		SpriteFont,
	}

	private enum BenchmarkTarget
	{
		Cpu,
		Full,
	}

	private readonly record struct PhaseResult(double Milliseconds, int Frames, long AllocatedBytes);

	private sealed class Aggregate
	{
		public int Frames { get; private set; }

		public double Milliseconds { get; private set; }

		public long AllocatedBytes { get; private set; }

		public double MsPerFrame => Frames > 0 ? Milliseconds / Frames : 0;

		public double AllocatedBytesPerFrame => Frames > 0 ? AllocatedBytes / (double)Frames : 0;

		public void Add(PhaseResult result)
		{
			Frames += result.Frames;
			Milliseconds += result.Milliseconds;
			AllocatedBytes += result.AllocatedBytes;
		}
	}

	private sealed class ModeResults
	{
		public Aggregate Cpu { get; } = new();

		public Aggregate Full { get; } = new();

		public Aggregate this[BenchmarkTarget target] => target == BenchmarkTarget.Cpu ? Cpu : Full;
	}
}
