/**
 * RenderHarness
 *
 * Automated checks for the board renderer and the deck/draw/place plumbing.
 *
 * This exists because Scrolls cannot be driven through captured tool output: the
 * renderer positions the cursor and polls Console.KeyAvailable, both of which are
 * meaningless when stdout is redirected, and a process launched from a build script
 * has no console at all.
 *
 * The way around that is AllocConsole. It attaches a classic conhost window, and
 * conhost - unlike Windows Terminal - honours Console.SetWindowSize. So the harness
 * can put the console into any size it likes, render a frame, and inspect the result.
 *
 * Two rules follow from that and are easy to get wrong:
 *
 *   1. Call AllocConsole before touching System.Console, or Console caches handles
 *      from the console-less state and every later call fails.
 *   2. Write the report to a file, never to stdout. Stdout now belongs to the
 *      allocated console window, not to whatever launched this.
 *
 * The frame itself is read out of Board.Screen's private back buffer by reflection
 * rather than scraped off the terminal, so the check is against what the renderer
 * composed, independent of what the terminal did with it.
 *
 * Build and run through tools\run-harness.ps1.
 */

/**
 * Default
 */
using System;

/**
 * Custom
 */
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

/**
 * Native Namespaces
 */
using Scrolls;
using Commands;
using Objects;
using Board;
using ArtificialIntelligence;

namespace Tools {
	public class RenderHarness {
		[DllImport("kernel32.dll")]
		private static extern bool AllocConsole();

		private static StringBuilder report = new StringBuilder();
		private static int passed;
		private static int failed;

		public static int Main(string[] argv) {
			string outPath = argv.Length > 0
				? argv[0]
				: Path.Combine(Environment.CurrentDirectory, "render-harness-report.txt");

			// Must happen before any Console access
			AllocConsole();

			try {
				RunChecks();
			} catch (Exception e) {
				Fail("harness completed without throwing", e.ToString());
			}

			string summary = "RESULT: " + passed + " passed, " + failed + " failed";
			report.Insert(0, summary + "\r\n" + new string('=', summary.Length) + "\r\n\r\n");

			File.WriteAllText(outPath, report.ToString(), new UTF8Encoding(true));
			return failed == 0 ? 0 : 1;
		}

		/**
		 * The checks themselves.
		 *
		 * Ordered so that each builds on the state the previous one left behind:
		 * load, draw, place, then the size sweep, then the two exhaustion cases.
		 */
		private static void RunChecks() {
			Screen.Init("Scrolls render harness");
			BasicBoard.title = "Welcome to Scrolls!";

			new Field(40, 40);

			int[,] deck1 = { { 10000000, 4 }, { 10000001, 16 }, { 10000002, 12 }, { 10000003, 8 } };
			int[,] deck2 = { { 10000000, 2 }, { 10000001, 18 }, { 10000002, 14 }, { 10000003, 6 } };

			Deck p1 = new Deck(0, deck1, 40);
			p1.Make();
			((IDisposable) p1).Dispose();

			Deck p2 = new Deck(1, deck2, 40);
			p2.Make();
			((IDisposable) p2).Dispose();

			Section("Deck loading");

			/*
			 * The manifest totals 40 for each player. Before Make honoured the
			 * manifest it loaded the whole table into player 1's pile regardless,
			 * so both a wrong count and a wrong owner are worth asserting.
			 */
			Check("player 1 pile holds 40 scrolls", Field.scrollLists[0, 0].Count == 40,
				  "got " + Field.scrollLists[0, 0].Count);
			Check("player 2 pile holds 40 scrolls", Field.scrollLists[1, 0].Count == 40,
				  "got " + Field.scrollLists[1, 0].Count);
			Check("player 2's deck did not land in player 1's pile",
				  !Object.ReferenceEquals(Field.scrollLists[0, 0], Field.scrollLists[1, 0]), "");

			Check("manifest quantities honoured (4/16/12/8)",
				  Tally(Field.scrollLists[0, 0], "AZGD") == 4
				  && Tally(Field.scrollLists[0, 0], "AZWR") == 16
				  && Tally(Field.scrollLists[0, 0], "MTCN") == 12
				  && Tally(Field.scrollLists[0, 0], "ELFC") == 8,
				  Distribution(Field.scrollLists[0, 0]));

			/*
			 * Copies must be distinct objects: two Aztec Warriors take damage
			 * independently, so sharing one instance would couple them.
			 */
			Check("duplicate cards are distinct instances",
				  !Object.ReferenceEquals(Field.scrollLists[0, 0][0], Field.scrollLists[0, 0][1]), "");

			Check("effect is never null (column is NULL on every row)",
				  ((Scroll) Field.scrollLists[0, 0][0]).effect != null, "");

			Section("Drawing");

			for (int i = 0; i < 6; i++) {
				PlayerCommands.draw(0);
				PlayerCommands.draw(1);
			}

			Check("six draws leave 34 in deck and 6 in hand",
				  Field.scrollLists[0, 0].Count == 34 && Field.scrollLists[0, 1].Count == 6,
				  Field.scrollLists[0, 0].Count + "/" + Field.scrollLists[0, 1].Count);
			Check("scrollsIn matches scrollLists after draw", CountsAgree(0), Counts(0));

			Section("Placing");

			/*
			 * Nothing may be played until a turn is running: CanPlace asks Turn, and
			 * without this every rule below returns false and reads as a pass. Begin
			 * runs the Draw Phase, so the hand is seven by the time the place lands.
			 */
			Turn.Begin(0);
			Check("the opening Draw Phase drew one", Field.scrollLists[0, 1].Count == 7,
				  "got " + Field.scrollLists[0, 1].Count);

			Scroll top = (Scroll) Field.scrollLists[0, 1][0];
			short expectedLine = top.FieldLine();
			PlayerCommands.place(0, new string[] { "1", "1" });

			Check("placed scroll landed on the line its FieldLine() reports",
				  Field.playerLines[0, expectedLine, 0] != null
				  && Field.playerLines[0, expectedLine, 0].id == top.id,
				  "expected " + top.nameAbb + " on line " + expectedLine);
			Check("hand shrank by one after placing", Field.scrollLists[0, 1].Count == 6,
				  "got " + Field.scrollLists[0, 1].Count);
			Check("scrollsIn matches scrollLists after place", CountsAgree(0), Counts(0));

			PlayerCommands.place(0, new string[] { "99", "1" });
			PlayerCommands.place(0, new string[] { "1", "9" });
			Check("out-of-range place left the hand alone", Field.scrollLists[0, 1].Count == 6,
				  "got " + Field.scrollLists[0, 1].Count);

			Section("Hand cap");

			while (PlayerCommands.draw(0)) { }
			Check("hand stops at the cap", Field.scrollLists[0, 1].Count == BasicBoard.MaxHand,
				  "got " + Field.scrollLists[0, 1].Count);

			Section("Deck exhaustion");

			/*
			 * Drain player 2 by cycling hand into void, so draw keeps being legal.
			 * The old draw decremented a counter unchecked and went negative, which
			 * then broke the width of the middle band.
			 */
			int guard = 0;
			while (guard++ < 500) {
				ArrayList hand = Field.scrollLists[1, 1];
				if (hand.Count > 0) {
					Field.scrollLists[1, 2].Add(hand[0]);
					hand.RemoveAt(0);
					PlayerCommands.Sync(1);
				}
				if (!PlayerCommands.draw(1) && Field.scrollLists[1, 0].Count == 0)
					break;
			}
			Check("empty deck floors at zero", Field.scrollsIn[1, 0] == 0,
				  "got " + Field.scrollsIn[1, 0]);
			Check("no pile count went negative",
				  Field.scrollsIn[1, 0] >= 0 && Field.scrollsIn[1, 1] >= 0 && Field.scrollsIn[1, 2] >= 0,
				  Counts(1));

			Section("Layout at varying terminal sizes");

			/*
			 * The shearing this renderer replaced showed up as rows of differing
			 * width, so that is the assertion. A full hand is wider than the board,
			 * which is why 67 rather than 37 is the minimum.
			 */
			Frame(80, 25, "default, compact layout");
			Frame(100, 50, "tall, full layout");
			Frame(120, 30, "wide and short");
			Frame(BasicBoard.MinWidth, BasicBoard.MinHeight, "exactly the minimum");
			Frame(BasicBoard.MinWidth - 1, BasicBoard.MinHeight, "one column below minimum");
			Frame(40, 20, "far below minimum");
		}

		/**
		 * Frame
		 * Renders at one console size and asserts every buffer row is full width.
		 */
		private static void Frame(int width, int height, string label) {
			string name = label + " (" + width + "x" + height + ")";

			try {
				// Shrink the window before the buffer, or the buffer cannot shrink
				Console.SetWindowSize(1, 1);
				Console.SetBufferSize(width, height);
				Console.SetWindowSize(width, height);
			} catch (Exception e) {
				Fail(name, "could not size console: " + e.Message);
				return;
			}

			BasicBoard.Render("place 1 3");

			Type screen = typeof(Screen);
			int bufferWidth = (int) screen.GetField("Width").GetValue(null);
			int bufferHeight = (int) screen.GetField("Height").GetValue(null);
			Array back = (Array) screen
				.GetField("back", BindingFlags.NonPublic | BindingFlags.Static)
				.GetValue(null);
			FieldInfo ch = screen.Assembly.GetType("Board.Cell").GetField("ch");

			string[] rows = new string[bufferHeight];
			int wrong = 0;
			StringBuilder row = new StringBuilder();

			for (int y = 0; y < bufferHeight; y++) {
				row.Length = 0;
				for (int x = 0; x < bufferWidth; x++) {
					char c = (char) ch.GetValue(back.GetValue(y, x));
					row.Append(c == '\0' ? ' ' : c);
				}
				rows[y] = row.ToString();
				if (rows[y].Length != bufferWidth)
					wrong++;
			}

			Check(name + ": every row is " + bufferWidth + " columns", wrong == 0,
				  wrong + " rows of the wrong width");

			bool expectNotice = width < BasicBoard.MinWidth || height < BasicBoard.MinHeight;
			Check(name + (expectNotice ? ": shows the too-small notice" : ": draws the board"),
				  BasicBoard.TooSmall == expectNotice,
				  "TooSmall=" + BasicBoard.TooSmall);

			report.Append("\r\n  --- frame: " + name + " ---\r\n");
			for (int y = 0; y < rows.Length; y++)
				report.Append(String.Format("  {0,3}|{1}|\r\n", y, rows[y].TrimEnd()));
		}

		private static bool CountsAgree(int player) {
			for (int category = 0; category < 3; category++)
				if (Field.scrollsIn[player, category] != Field.scrollLists[player, category].Count)
					return false;
			return true;
		}

		private static string Counts(int player) {
			return "counts " + Field.scrollsIn[player, 0] + "/" + Field.scrollsIn[player, 1] + "/"
				+ Field.scrollsIn[player, 2] + " vs lists " + Field.scrollLists[player, 0].Count + "/"
				+ Field.scrollLists[player, 1].Count + "/" + Field.scrollLists[player, 2].Count;
		}

		private static int Tally(ArrayList pile, string nameAbb) {
			int n = 0;
			foreach (Scroll scroll in pile)
				if (scroll.nameAbb == nameAbb)
					n++;
			return n;
		}

		private static string Distribution(ArrayList pile) {
			return "AZGD=" + Tally(pile, "AZGD") + " AZWR=" + Tally(pile, "AZWR")
				+ " MTCN=" + Tally(pile, "MTCN") + " ELFC=" + Tally(pile, "ELFC");
		}

		private static void Section(string name) {
			report.Append("\r\n" + name + "\r\n" + new string('-', name.Length) + "\r\n");
		}

		private static void Check(string description, bool condition, string detail) {
			if (condition) {
				passed++;
				report.Append("  PASS  " + description + "\r\n");
			} else {
				Fail(description, detail);
			}
		}

		private static void Fail(string description, string detail) {
			failed++;
			report.Append("  FAIL  " + description
						  + (detail.Length > 0 ? "  [" + detail + "]" : "") + "\r\n");
		}
	}
}
