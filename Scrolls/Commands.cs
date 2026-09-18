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
		public static string[] allCommands = { "draw", "place", "attack", "help", "quit" };

		private static string[,] helpText = {
			{ "draw",                     "Draw the top scroll of your deck." },
			{ "place",                    "Pick a scroll and a destination with the arrow keys." },
			{ "place <hand> <line> <slot>", "Play a scroll directly. Line is front, rear or equip." },
			{ "attack",                   "Pick an attacker and a target with the arrow keys." },
			{ "help",                     "Show this list." },
			{ "quit",                     "Leave the game." }
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
					if (args.Length == 0)
						BeginPlace(player);
					else
						place(player, args);
					return false;
				case "attack":
					BeginAttack(player);
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
				MessageLog.Add("  " + helpText[i, 0].PadRight(30) + helpText[i, 1]);
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
		 * Parses a typed placement and hands it to Place.
		 *
		 * Two arguments keep the original behaviour, taking the line from the scroll
		 * itself. Three let the player name the line, which is the whole point of
		 * the change: a scroll legal on either line could never be steered before.
		 *
		 * @param player The acting player
		 * @param args   hand position, optionally a line, then slot
		 */
		public static void place(short player, string[] args) {
			if (args.Length < 2) {
				MessageLog.Add("Usage: place <hand position> [front|rear|equip] <slot>");
				return;
			}

			int handIndex;
			int slot;
			if (!TryParse(args[0], out handIndex) || !TryParse(args[args.Length - 1], out slot)) {
				MessageLog.Add("Hand position and slot must be numbers.");
				return;
			}

			ArrayList hand = Field.scrollLists[player, 1];
			handIndex--;
			slot--;

			if (handIndex < 0 || handIndex >= hand.Count) {
				MessageLog.Add("No scroll at hand position " + (handIndex + 1) + ".");
				return;
			}

			Scroll scroll = (Scroll) hand[handIndex];
			int line;

			if (args.Length >= 3) {
				if (!ParseLine(args[1], out line)) {
					MessageLog.Add("\"" + args[1] + "\" is not a line. Use front, rear or equip.");
					return;
				}
			} else {
				line = scroll.FieldLine();
			}

			Place(player, handIndex, line, slot);
		}

		/**
		 * ParseLine
		 * Reads a line as a name or as a one-based number.
		 */
		private static bool ParseLine(string text, out int line) {
			line = 0;
			if (text == null)
				return false;

			switch (text.ToLower()) {
				case "front": line = 0; return true;
				case "rear":  line = 1; return true;
				case "back":  line = 1; return true;
				case "equip": line = 2; return true;
				case "equipment": line = 2; return true;
				case "mage": line = 2; return true;
			}

			int number;
			if (TryParse(text, out number) && number >= 1 && number <= BasicBoard.Lines) {
				line = number - 1;
				return true;
			}
			return false;
		}

		/**
		 * LineName
		 * A line index as the player sees it, for messages and the status row.
		 */
		public static string LineName(int line) {
			switch (line) {
				case 0: return "front";
				case 1: return "rear";
				case 2: return "equipment";
				default: return "line " + (line + 1);
			}
		}

		/**
		 * PlaceLines
		 * The field rows a scroll may legally occupy.
		 *
		 * Scroll.FieldLine collapses "Either" to the front row because it has to
		 * return one answer. This returns the whole set instead, so a cursor can
		 * offer the choice and a typed line can be checked against it.
		 */
		public static short[] PlaceLines(Scroll scroll) {
			if (scroll == null)
				return new short[] { 0 };

			switch (scroll.line) {
				case 1: return new short[] { 0 };
				case 2: return new short[] { 1 };
				case 3: return new short[] { 0, 1 };
				case 4: return new short[] { 2 };
				default: return new short[] { 0 };
			}
		}

		/**
		 * Occupied
		 * Whether a field cell holds a real scroll.
		 *
		 * Empty cells are placeholder Scrolls with id 0 seeded by the Field
		 * constructor, not nulls, though both are treated as empty here.
		 */
		public static bool Occupied(int player, int line, int slot) {
			if (line < 0 || line >= BasicBoard.Lines || slot < 0 || slot >= BasicBoard.Slots)
				return false;
			Scroll scroll = Field.playerLines[player, line, slot];
			return scroll != null && scroll.id != 0;
		}

		/**
		 * CanPlace
		 * Whether a scroll may be played into a given cell.
		 */
		public static bool CanPlace(short player, Scroll scroll, int line, int slot) {
			if (scroll == null || slot < 0 || slot >= BasicBoard.Slots)
				return false;
			if (Occupied(player, line, slot))
				return false;

			short[] lines = PlaceLines(scroll);
			for (int i = 0; i < lines.Length; i++)
				if (lines[i] == line)
					return true;
			return false;
		}

		/**
		 * CanAttackFrom
		 * Whether a cell holds something able to declare an attack.
		 *
		 * Equipment neither attacks nor is attacked, and a scroll whose attacks all
		 * lack a power cannot deal damage, so neither is offered to the cursor.
		 */
		public static bool CanAttackFrom(short player, int line, int slot) {
			if (!Occupied(player, line, slot))
				return false;
			Scroll scroll = Field.playerLines[player, line, slot];
			return scroll.IsEntity() && scroll.PrimaryAttack() >= 0;
		}

		/**
		 * CanTarget
		 * Whether a defending cell may be attacked.
		 *
		 * A rear scroll is shielded only by the front scroll in its own column. An
		 * empty front slot leaves the rear scroll behind it exposed, even when the
		 * rest of the front line is full.
		 */
		public static bool CanTarget(short defender, int line, int slot) {
			if (!Occupied(defender, line, slot))
				return false;
			if (line == 2)
				return false;
			if (line == 1 && Occupied(defender, 0, slot))
				return false;
			return true;
		}

		/**
		 * Place
		 * Moves a scroll from the hand into a field cell.
		 *
		 * @return true if the scroll moved
		 */
		public static bool Place(short player, int handIndex, int line, int slot) {
			ArrayList hand = Field.scrollLists[player, 1];

			if (handIndex < 0 || handIndex >= hand.Count) {
				MessageLog.Add("No scroll at hand position " + (handIndex + 1) + ".");
				return false;
			}
			if (slot < 0 || slot >= BasicBoard.Slots) {
				MessageLog.Add("Slot must be between 1 and " + BasicBoard.Slots + ".");
				return false;
			}

			Scroll scroll = (Scroll) hand[handIndex];

			if (!CanPlace(player, scroll, line, slot)) {
				if (Occupied(player, line, slot))
					MessageLog.Add("That slot is already occupied.");
				else
					MessageLog.Add(scroll.name + " cannot be played to the " + LineName(line) + " line.");
				return false;
			}

			Field.playerLines[player, line, slot] = scroll;
			hand.RemoveAt(handIndex);
			Sync(player);
			MessageLog.Add(scroll.name + " placed in " + LineName(line) + " slot " + (slot + 1) + ".");
			return true;
		}

		/**
		 * Attack
		 * Resolves one attack between two field cells.
		 *
		 * Damage is the attack's power less the defender's armor, taken off the
		 * defender's endurance. Endurance is mutated in place: every scroll on the
		 * field is a distinct object from Scroll.Copy, so no other copy of the card
		 * is affected, and there is no printed-versus-current split to maintain.
		 *
		 * @return true if the attack resolved
		 */
		public static bool Attack(short player, int line, int slot, int targetLine, int targetSlot) {
			short defender = (short) (1 - player);

			if (!CanAttackFrom(player, line, slot)) {
				MessageLog.Add("Nothing there can attack.");
				return false;
			}
			if (!CanTarget(defender, targetLine, targetSlot)) {
				MessageLog.Add("That scroll cannot be reached.");
				return false;
			}

			Scroll attacker = Field.playerLines[player, line, slot];
			Scroll target = Field.playerLines[defender, targetLine, targetSlot];

			int attack = attacker.PrimaryAttack();
			short power = attacker.AttackPower(attack);
			short damage = (short) Math.Max(0, power - target.armor);

			target.endurance = (short) (target.endurance - damage);

			MessageLog.Add(attacker.name + " hits " + target.name + " with "
						   + attacker.AttackName(attack) + " for " + damage + ".");

			if (target.endurance <= 0) {
				/*
				 * The vacated cell takes a placeholder rather than a null: the Field
				 * constructor seeds every cell that way and CanPlace tests id, not
				 * reference.
				 */
				Field.scrollLists[defender, 2].Add(target);
				Field.playerLines[defender, targetLine, targetSlot] = new Scroll();
				Sync(defender);
				MessageLog.Add(target.name + " is destroyed.");
			} else {
				MessageLog.Add(target.name + " has " + target.endurance + " endurance left.");
			}

			return true;
		}

		/**
		 * BeginPlace
		 * Opens the cursor on the hand, if there is anything to play.
		 */
		public static void BeginPlace(short player) {
			if (Field.scrollLists[player, 1].Count == 0) {
				MessageLog.Add("Your hand is empty.");
				return;
			}
			if (!Selection.BeginPlace(player))
				MessageLog.Add("Nothing in your hand has anywhere to go.");
		}

		/**
		 * BeginAttack
		 * Opens the cursor on your own field, if anything there can attack.
		 */
		public static void BeginAttack(short player) {
			if (!Selection.BeginAttack(player))
				MessageLog.Add("Nothing on your field can attack.");
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
