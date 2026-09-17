/**
 * Default
 */
using System;

/**
 * Custom
 */
using System.Threading;
using System.Collections;

/**
 * Native Namespaces
 */
using Scrolls;
using Commands;
using Objects;
using Board;
using ArtificialIntelligence;

namespace Commands {
	public class SystemCommands {
		/**
		 * DrawHand
		 * Deals the opening hands.
		 *
		 * Still animated, but the delay is now skippable: holding a key through the
		 * deal drops straight to the finished board instead of forcing a six second
		 * wait on every launch.
		 */
		public static void DrawHand() {
			const int OpeningHand = 6;
			bool animate = true;

			for (short counter = 0; counter < OpeningHand; counter++) {
				for (short player = 0; player < 2; player++) {
					PlayerCommands.draw(player);
					if (animate) {
						BasicBoard.Render("");
						animate = !Interrupted();
						if (animate)
							Thread.Sleep(250);
					}
				}
			}

			if (!animate)
				MessageLog.Add("Deal skipped.");
		}

		/**
		 * Interrupted
		 * Whether a key is waiting, used to cut an animation short. Reading the key
		 * here stops it arriving at the command prompt afterwards.
		 */
		private static bool Interrupted() {
			try {
				if (!Console.KeyAvailable)
					return false;
				Console.ReadKey(true);
				return true;
			} catch (Exception) {
				return false;
			}
		}
	}

	public class PlayerCommands {
		public const int MaxHand = BasicBoard.MaxHand;

		/**
		 * Every verb the parser accepts, with the help text shown for it.
		 *
		 * This is the single source of truth. Previously allCommands held only
		 * "draw" while runCommand also handled "place", so a valid verb was
		 * rejected as invalid before it ever reached the dispatcher.
		 */
		public static string[] allCommands = { "draw", "place", "help", "quit" };

		private static string[,] helpText = {
			{ "draw",              "Draw the top scroll of your deck." },
			{ "place <hand> <slot>", "Play a scroll from your hand into a slot (1-6)." },
			{ "help",              "Show this list." },
			{ "quit",              "Leave the game." }
		};

		/**
		 * runCommand
		 * Parses and executes one line of input.
		 *
		 * @param  player The player the command is acting for
		 * @param  input  The raw line typed at the prompt
		 * @return true if the game should quit
		 */
		public static bool runCommand(short player, string input) {
			if (input == null)
				input = "";

			string[] parts = input.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0) {
				help();
				return false;
			}

			string verb = parts[0].ToLower();
			string[] args = new string[parts.Length - 1];
			Array.Copy(parts, 1, args, 0, args.Length);

			switch (verb) {
				case "draw":
					draw(player);
					return false;
				case "place":
					place(player, args);
					return false;
				case "help":
					help();
					return false;
				case "quit":
					return true;
				default:
					MessageLog.Add("\"" + verb + "\" is not a command. Type help.");
					return false;
			}
		}

		public static void help() {
			MessageLog.Add("Commands:");
			for (int i = 0; i < helpText.GetLength(0); i++)
				MessageLog.Add("  " + helpText[i, 0].PadRight(22) + helpText[i, 1]);
		}

		/**
		 * draw
		 * Moves the top scroll of a player's deck into their hand.
		 *
		 * This now moves the actual Scroll rather than only adjusting counts, which
		 * is what lets the board render real cards. The counts are derived from the
		 * lists afterwards so the two representations cannot drift apart.
		 *
		 * @param  player The drawing player
		 * @return true if a scroll moved
		 */
		public static bool draw(short player) {
			ArrayList deck = Field.scrollLists[player, 0];
			ArrayList hand = Field.scrollLists[player, 1];

			if (hand.Count >= MaxHand) {
				MessageLog.Add("Player " + (player + 1) + "'s hand is full.");
				return false;
			}
			if (deck.Count == 0) {
				MessageLog.Add("Player " + (player + 1) + "'s deck is empty.");
				return false;
			}

			int top = deck.Count - 1;
			Scroll scroll = (Scroll) deck[top];
			deck.RemoveAt(top);
			hand.Add(scroll);
			Sync(player);
			return true;
		}

		/**
		 * place
		 * Plays a scroll out of the hand and onto the field.
		 *
		 * The target line comes from the scroll itself rather than being chosen, so
		 * this needs no cursor; the slot is given as a number.
		 *
		 * @param player The acting player
		 * @param args   hand position then slot, both one-based
		 */
		public static void place(short player, string[] args) {
			if (args.Length < 2) {
				MessageLog.Add("Usage: place <hand position> <slot>");
				return;
			}

			int handIndex;
			int slot;
			if (!TryParse(args[0], out handIndex) || !TryParse(args[1], out slot)) {
				MessageLog.Add("Both arguments must be numbers.");
				return;
			}

			ArrayList hand = Field.scrollLists[player, 1];
			handIndex--;
			slot--;

			if (handIndex < 0 || handIndex >= hand.Count) {
				MessageLog.Add("No scroll at hand position " + (handIndex + 1) + ".");
				return;
			}
			if (slot < 0 || slot >= BasicBoard.Slots) {
				MessageLog.Add("Slot must be between 1 and " + BasicBoard.Slots + ".");
				return;
			}

			Scroll scroll = (Scroll) hand[handIndex];
			short line = scroll.FieldLine();

			if (Field.playerLines[player, line, slot] != null
				&& Field.playerLines[player, line, slot].id != 0) {
				MessageLog.Add("That slot is already occupied.");
				return;
			}

			Field.playerLines[player, line, slot] = scroll;
			hand.RemoveAt(handIndex);
			Sync(player);
			MessageLog.Add(scroll.name + " placed in slot " + (slot + 1) + ".");
		}

		/**
		 * Sync
		 * Recomputes a player's counts from their lists.
		 *
		 * scrollsIn used to be updated by hand alongside scrollLists and the two
		 * drifted apart immediately. Deriving one from the other removes the
		 * possibility.
		 */
		public static void Sync(short player) {
			for (short category = 0; category < 3; category++)
				Field.scrollsIn[player, category] = (short) Field.scrollLists[player, category].Count;
		}

		/**
		 * TryParse
		 * Int32.TryParse exists in .NET 2.0 and up; this only keeps the call sites
		 * readable.
		 */
		private static bool TryParse(string text, out int value) {
			return Int32.TryParse(text, out value);
		}
	}
}
