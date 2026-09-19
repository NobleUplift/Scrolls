/**
 * Default
 */
using System;

/**
 * Custom
 */
//using System.IO;
//using System.Reflection;
using System.Threading;

/**
 * Native Namespaces
 */
using Scrolls;
using Commands;
using Objects;
using Board;
using ArtificialIntelligence;

namespace Scrolls {
	public class Program
	{
		public const short DeckSize = 40;

		public static void Main(string[] args) {
			/*
			 * No SetWindowSize. Windows Terminal ignores it, which left every frame
			 * wider than the window and sheared the grid. The renderer reads the
			 * real window size instead and lays out to whatever it finds.
			 */
			Screen.Init("Scrolls");

			// Shown through the deal; the turn banner takes the row from Turn.Begin on
			BasicBoard.title = "Welcome to Scrolls!";

			new Field(DeckSize, DeckSize);

			/*
			 * Card ids come from the Aztec table. The original player 2 manifest
			 * listed the 20000000 range, which has never existed in the shipped
			 * database, so that deck always loaded empty.
			 */
			int[,] scroll1 = { { 10000000,  4 },
							   { 10000001, 16 },
							   { 10000002, 12 },
							   { 10000003,  8 } };
			int[,] scroll2 = { { 10000000,  2 },
							   { 10000001, 18 },
							   { 10000002, 14 },
							   { 10000003,  6 } };

			using (Deck player1 = new Deck(0, scroll1, DeckSize)) {
				player1.Make();
			}
			using (Deck player2 = new Deck(1, scroll2, DeckSize)) {
				player2.Make();
			}

			MessageLog.Add("Type help for commands.");

			SystemCommands.DrawHand();
			Turn.Begin(0);
			InputCommand();

			Screen.Shutdown();
		}

		/**
		 * InputCommand
		 * The command loop: read a key, redraw when something changed, dispatch on
		 * Enter.
		 *
		 * Input is assembled a key at a time rather than through ReadLine because
		 * ReadLine moves the cursor and echoes on its own, which fights a renderer
		 * that owns the whole screen. Polling instead of blocking also lets the
		 * board re-lay out when the window is resized mid-game.
		 */
		public static void InputCommand() {
			string input = "";
			bool quit = false;
			bool dirty = true;

			while (!quit) {
				if (dirty) {
					BasicBoard.Render(input);
					dirty = false;
				}

				if (!KeyWaiting()) {
					// Nothing typed: check whether the window changed shape, then idle
					if (Screen.EnsureSize())
						dirty = true;
					Thread.Sleep(30);
					continue;
				}

				ConsoleKeyInfo key;
				try {
					key = Console.ReadKey(true);
				} catch (Exception) {
					return; // No usable console; nothing more this loop can do
				}

				/*
				 * At a size too small to draw the board there is no visible prompt,
				 * so the notice offers a bare Q instead of a typed command.
				 */
				if (BasicBoard.TooSmall) {
					if (key.Key == ConsoleKey.Q)
						return;
					dirty = true;
					continue;
				}

				/*
				 * A running selection owns the keyboard. The prompt is inert until
				 * it finishes, so that Enter and Escape mean one thing at a time.
				 */
				if (Selection.Active) {
					CursorKey(key);
					dirty = true;
					continue;
				}

				switch (key.Key) {
					case ConsoleKey.Enter:
						// Whoever's turn it is; the console is shared between the two
						quit = PlayerCommands.runCommand(Turn.Active, input);
						input = "";
						dirty = true;
						break;

					case ConsoleKey.Backspace:
						if (input.Length > 0) {
							input = input.Substring(0, input.Length - 1);
							dirty = true;
						}
						break;

					case ConsoleKey.Escape:
						if (input.Length > 0) {
							input = "";
							dirty = true;
						}
						break;

					default:
						if (!Char.IsControl(key.KeyChar)) {
							input += key.KeyChar;
							dirty = true;
						}
						break;
				}
			}
		}

		/**
		 * CursorKey
		 * One keystroke while a selection is running.
		 *
		 * Arrow keys reach here with a KeyChar of '\0', which Char.IsControl calls a
		 * control character, so the typing branch below discards them. They have to
		 * be read off key.Key instead.
		 *
		 * Movement only ever lands on a position the command would accept, so Enter
		 * never has to refuse. Revalidate runs afterwards because placing a scroll
		 * shortens the hand and destroying one empties a cell, either of which can
		 * pull the ground out from under the cursor.
		 */
		private static void CursorKey(ConsoleKeyInfo key) {
			switch (key.Key) {
				case ConsoleKey.LeftArrow:
					Selection.Move(-1, 0);
					break;
				case ConsoleKey.RightArrow:
					Selection.Move(1, 0);
					break;
				case ConsoleKey.UpArrow:
					Selection.Move(0, -1);
					break;
				case ConsoleKey.DownArrow:
					Selection.Move(0, 1);
					break;
				case ConsoleKey.Enter:
					Selection.Confirm();
					break;
				case ConsoleKey.Escape:
					Selection.Back();
					break;
			}
			Selection.Revalidate();
		}

		/**
		 * KeyWaiting
		 * Whether a keystroke is pending, false when there is no console to ask.
		 */
		private static bool KeyWaiting() {
			try {
				return Console.KeyAvailable;
			} catch (Exception) {
				return false;
			}
		}
	}
}
