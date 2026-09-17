/**
 * Default
 */
using System;

/**
 * Custom
 */
using System.Text;

/**
 * Native Namespaces
 */
using Scrolls;
using Commands;
using Objects;
using Board;
using ArtificialIntelligence;

namespace Board {
	/**
	 * Cell
	 * One character of the screen together with its colours.
	 */
	public struct Cell {
		public char ch;
		public ConsoleColor fg;
		public ConsoleColor bg;
	}

	/**
	 * Rect
	 * A rectangular region of the screen. Returned by the board layout so that
	 * callers can address an individual card slot rather than a whole line of text.
	 */
	public struct Rect {
		public int x;
		public int y;
		public int width;
		public int height;

		public Rect(int x, int y, int width, int height) {
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		public bool Contains(int px, int py) {
			return px >= this.x && px < this.x + this.width
				&& py >= this.y && py < this.y + this.height;
		}
	}

	/**
	 * Screen
	 * A double-buffered character display.
	 *
	 * Drawing composes into the back buffer; Flush compares it against the front
	 * buffer and rewrites only the cells that actually changed. Nothing else in the
	 * program may call Console.Write directly, or the two buffers fall out of step
	 * with what the terminal is showing.
	 */
	public class Screen {
		public const ConsoleColor DefaultFg = ConsoleColor.Gray;
		public const ConsoleColor DefaultBg = ConsoleColor.Black;

		public static int Width = 0;
		public static int Height = 0;

		// front is what the terminal currently shows, back is what the next Flush will make it show
		private static Cell[,] front;
		private static Cell[,] back;

		// Set when the buffers are reallocated, to repaint every cell instead of diffing against garbage
		private static bool forceFull = true;

		/**
		 * Init
		 * Prepares the console for full-screen drawing.
		 */
		public static void Init(string title) {
			try {
				Console.Title = title;
			} catch (Exception) { }

			/*
			 * UTF8Encoding(false) rather than Encoding.UTF8: the latter carries a
			 * byte-order-mark preamble which .NET Framework emits into the output
			 * stream, printing stray bytes before the first frame.
			 */
			try {
				Console.OutputEncoding = new UTF8Encoding(false);
			} catch (Exception) { }

			try {
				Console.CursorVisible = false;
			} catch (Exception) { }

			EnsureSize();
		}

		/**
		 * Shutdown
		 * Restores the console to a state a shell can keep using.
		 */
		public static void Shutdown() {
			try {
				Console.ResetColor();
				Console.CursorVisible = true;
				Console.SetCursorPosition(0, Math.Max(0, Height - 1));
				Console.WriteLine();
			} catch (Exception) { }
		}

		/**
		 * EnsureSize
		 * Re-reads the terminal size and reallocates if it changed.
		 *
		 * @return true if the size changed, meaning the caller must re-lay out
		 */
		public static bool EnsureSize() {
			int w;
			int h;
			GetConsoleSize(out w, out h);

			if (w == Width && h == Height && back != null)
				return false;

			Width = w;
			Height = h;
			front = new Cell[Height, Width];
			back = new Cell[Height, Width];
			forceFull = true;
			Clear();
			return true;
		}

		/**
		 * GetConsoleSize
		 * Reads the window size, falling back to 80x25 when there is no real console
		 * (output redirected to a file or a pipe, where the window APIs throw).
		 */
		private static void GetConsoleSize(out int w, out int h) {
			try {
				w = Console.WindowWidth;
				h = Console.WindowHeight;
				if (w > 0 && h > 0)
					return;
			} catch (Exception) { }
			w = 80;
			h = 25;
		}

		/**
		 * Clear
		 * Blanks the back buffer. Does not touch the terminal; Flush does that.
		 */
		public static void Clear() {
			Fill(new Rect(0, 0, Width, Height), ' ', DefaultFg, DefaultBg);
		}

		public static void Fill(Rect area, char ch, ConsoleColor fg, ConsoleColor bg) {
			for (int y = area.y; y < area.y + area.height; y++)
				for (int x = area.x; x < area.x + area.width; x++)
					Put(x, y, ch, fg, bg);
		}

		/**
		 * Put
		 * Writes one character. Coordinates outside the screen are dropped rather
		 * than throwing, so a layout that overruns a small terminal clips instead
		 * of crashing.
		 */
		public static void Put(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg) {
			if (x < 0 || y < 0 || x >= Width || y >= Height)
				return;
			back[y, x].ch = ch;
			back[y, x].fg = fg;
			back[y, x].bg = bg;
		}

		public static void Write(int x, int y, string text, ConsoleColor fg, ConsoleColor bg) {
			if (text == null)
				return;
			for (int i = 0; i < text.Length; i++)
				Put(x + i, y, text[i], fg, bg);
		}

		public static void Write(int x, int y, string text) {
			Write(x, y, text, DefaultFg, DefaultBg);
		}

		/**
		 * WriteCentred
		 * Horizontally centres text across the full screen width.
		 */
		public static void WriteCentred(int y, string text, ConsoleColor fg, ConsoleColor bg) {
			if (text == null)
				return;
			Write((Width - text.Length) / 2, y, text, fg, bg);
		}

		/**
		 * Recolour
		 * Repaints the colours of a region without disturbing its characters.
		 * This is what lets a later selection cursor highlight a slot that the
		 * board renderer has already drawn.
		 */
		public static void Recolour(Rect area, ConsoleColor fg, ConsoleColor bg) {
			for (int y = area.y; y < area.y + area.height; y++) {
				for (int x = area.x; x < area.x + area.width; x++) {
					if (x < 0 || y < 0 || x >= Width || y >= Height)
						continue;
					back[y, x].fg = fg;
					back[y, x].bg = bg;
				}
			}
		}

		/**
		 * Flush
		 * Pushes the back buffer to the terminal, writing only changed cells.
		 */
		public static void Flush() {
			try {
				Console.CursorVisible = false;
			} catch (Exception) { }

			StringBuilder run = new StringBuilder();

			for (int y = 0; y < Height; y++) {
				int x = 0;
				while (x < Width) {
					if (!Changed(x, y)) {
						x++;
						continue;
					}

					ConsoleColor fg = back[y, x].fg;
					ConsoleColor bg = back[y, x].bg;
					int start = x;
					run.Length = 0;

					// Gather a run of changed cells sharing one colour pair
					while (x < Width && Changed(x, y)
						   && back[y, x].fg == fg && back[y, x].bg == bg) {
						run.Append(back[y, x].ch);
						front[y, x] = back[y, x];
						x++;
					}

					try {
						Console.SetCursorPosition(start, y);
						Console.ForegroundColor = fg;
						Console.BackgroundColor = bg;
						Console.Write(run.ToString());
					} catch (Exception) {
						// Terminal shrank underneath us; the next EnsureSize will re-lay out
						forceFull = true;
						return;
					}
				}
			}

			forceFull = false;
		}

		/**
		 * Changed
		 * Whether a cell needs rewriting.
		 *
		 * Writing the bottom-right cell scrolls the console buffer up by one line,
		 * which would shift the whole frame, so that cell is never drawn.
		 */
		private static bool Changed(int x, int y) {
			if (y == Height - 1 && x == Width - 1)
				return false;
			if (forceFull)
				return true;
			return back[y, x].ch != front[y, x].ch
				|| back[y, x].fg != front[y, x].fg
				|| back[y, x].bg != front[y, x].bg;
		}

		/**
		 * PlaceCursor
		 * Parks the hardware cursor, used to keep it sitting in the command prompt.
		 */
		public static void PlaceCursor(int x, int y, bool visible) {
			try {
				if (x >= 0 && y >= 0 && x < Width && y < Height)
					Console.SetCursorPosition(x, y);
				Console.CursorVisible = visible;
			} catch (Exception) { }
		}
	}
}
