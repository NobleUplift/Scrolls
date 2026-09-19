/**
 * Default
 */
using System;

/**
 * Custom
 */
using System.Collections; // ArrayList

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
	 * Stage
	 * Which question the cursor is currently asking.
	 *
	 * None means the player is typing at the prompt. The other six run in three
	 * pairs: pick a scroll then pick where it goes, pick an attacker then pick what
	 * it hits, or pick an entity then pick where it steps.
	 */
	public enum Stage { None, Hand, Place, Attacker, Target, Mover, Destination }

	/**
	 * Selection
	 * The modal cursor.
	 *
	 * This holds only where the cursor is and how it got there. Whether a cell is
	 * a legal destination or a reachable target is PlayerCommands' business, and is
	 * asked rather than duplicated, so the cursor and a typed command can never
	 * disagree about the rules.
	 *
	 * Every position the cursor can occupy is one the action would succeed from,
	 * because movement is built by filtering cells through those same predicates.
	 * That is what lets Enter always mean yes.
	 */
	public class Selection {
		private static Stage stage = Stage.None;
		private static short player = 0;

		// Live in the Hand stage, then the confirmed card through the Place stage
		private static int handIndex = 0;

		// The live cursor while a field stage is running
		private static int line = 0;
		private static int slot = 0;

		// The confirmed attacker or mover, held through the Target and Destination stages
		private static int fromLine = 0;
		private static int fromSlot = 0;

		public static Stage Current { get { return stage; } }
		public static bool Active { get { return stage != Stage.None; } }
		public static short Player { get { return player; } }
		public static int HandIndex { get { return handIndex; } }
		public static int Line { get { return line; } }
		public static int Slot { get { return slot; } }
		public static int FromLine { get { return fromLine; } }
		public static int FromSlot { get { return fromSlot; } }

		/**
		 * CursorPlayer
		 * Whose field the live cursor is standing on.
		 *
		 * Only the Target stage crosses to the other side of the board.
		 */
		public static short CursorPlayer {
			get { return stage == Stage.Target ? (short) (1 - player) : player; }
		}

		public static void Reset() {
			stage = Stage.None;
		}

		/**
		 * BeginPlace
		 * Opens the cursor on the first hand scroll that has somewhere to go.
		 *
		 * @return false if no scroll in hand can be played anywhere
		 */
		public static bool BeginPlace(short acting) {
			player = acting;
			stage = Stage.Hand;
			handIndex = 0;

			if (!MoveToFirstLegalHand()) {
				stage = Stage.None;
				return false;
			}
			return true;
		}

		/**
		 * BeginAttack
		 * Opens the cursor on the first of the player's scrolls that can attack.
		 *
		 * @return false if nothing on the player's field can attack
		 */
		public static bool BeginAttack(short acting) {
			player = acting;
			stage = Stage.Attacker;

			ArrayList cells = LegalCells();
			if (cells.Count == 0) {
				stage = Stage.None;
				return false;
			}
			SetCell(cells, 0);
			return true;
		}

		/**
		 * BeginMove
		 * Opens the cursor on the first of the player's entities with a step to take.
		 *
		 * @return false if nothing on the player's field can reposition
		 */
		public static bool BeginMove(short acting) {
			player = acting;
			stage = Stage.Mover;

			ArrayList cells = LegalCells();
			if (cells.Count == 0) {
				stage = Stage.None;
				return false;
			}
			SetCell(cells, 0);
			return true;
		}

		/**
		 * Back
		 * Steps one stage towards the prompt.
		 */
		public static void Back() {
			switch (stage) {
				case Stage.Place:
					stage = Stage.Hand;
					break;
				case Stage.Target:
					stage = Stage.Attacker;
					line = fromLine;
					slot = fromSlot;
					break;
				case Stage.Destination:
					stage = Stage.Mover;
					line = fromLine;
					slot = fromSlot;
					break;
				default:
					stage = Stage.None;
					break;
			}
		}

		/**
		 * Confirm
		 * Accepts the current position and moves to the next stage, running the
		 * command itself on the last one.
		 */
		public static void Confirm() {
			switch (stage) {
				case Stage.Hand: {
					stage = Stage.Place;
					ArrayList cells = LegalCells();
					if (cells.Count == 0) {
						// Cannot normally happen: the Hand stage only offers playable scrolls
						stage = Stage.Hand;
						MessageLog.Add("That scroll has nowhere to go.");
						return;
					}
					SetCell(cells, 0);
					return;
				}

				case Stage.Place:
					PlayerCommands.Place(player, handIndex, line, slot);
					stage = Stage.None;
					return;

				case Stage.Attacker: {
					fromLine = line;
					fromSlot = slot;
					stage = Stage.Target;
					ArrayList cells = LegalCells();
					if (cells.Count == 0) {
						stage = Stage.Attacker;
						MessageLog.Add("Nothing on the other side can be reached.");
						return;
					}
					SetCell(cells, 0);
					return;
				}

				case Stage.Target:
					PlayerCommands.Attack(player, fromLine, fromSlot, line, slot);
					stage = Stage.None;
					return;

				case Stage.Mover: {
					fromLine = line;
					fromSlot = slot;
					stage = Stage.Destination;
					ArrayList cells = LegalCells();
					if (cells.Count == 0) {
						// Cannot normally happen: the Mover stage only offers entities with a step
						stage = Stage.Mover;
						MessageLog.Add("That entity has nowhere to step.");
						return;
					}
					SetCell(cells, 0);
					return;
				}

				case Stage.Destination:
					PlayerCommands.Move(player, fromLine, fromSlot, line, slot);
					stage = Stage.None;
					return;
			}
		}

		/**
		 * Move
		 * Steps the cursor. dx is one column of travel, dy one row on screen.
		 */
		public static void Move(int dx, int dy) {
			if (stage == Stage.None)
				return;

			if (stage == Stage.Hand) {
				MoveHand(dx);
				return;
			}

			ArrayList cells = LegalCells();
			if (cells.Count == 0)
				return;

			if (dy != 0)
				MoveLine(cells, dy);
			else if (dx != 0)
				MoveSlot(cells, dx);
		}

		/**
		 * MoveHand
		 * Walks to the next hand scroll that can actually be played.
		 */
		private static void MoveHand(int dx) {
			int count = Field.scrollLists[player, 1].Count;
			if (count == 0) {
				stage = Stage.None;
				return;
			}
			if (dx == 0)
				return;

			int step = dx > 0 ? 1 : -1;
			for (int tried = 0; tried < count; tried++) {
				handIndex = ((handIndex + step) % count + count) % count;
				if (HandPlayable(handIndex))
					return;
			}
		}

		/**
		 * MoveToFirstLegalHand
		 * Parks the cursor on the lowest hand position that has a destination.
		 */
		private static bool MoveToFirstLegalHand() {
			int count = Field.scrollLists[player, 1].Count;
			for (int i = 0; i < count; i++) {
				if (HandPlayable(i)) {
					handIndex = i;
					return true;
				}
			}
			return false;
		}

		/**
		 * HandPlayable
		 * Whether a held scroll has at least one cell it could go to.
		 */
		private static bool HandPlayable(int index) {
			ArrayList hand = Field.scrollLists[player, 1];
			if (index < 0 || index >= hand.Count)
				return false;

			return PlayerCommands.CanPlaceAnywhere(player, (Scroll) hand[index]);
		}

		/**
		 * MoveSlot
		 * Left and right, wrapping through every legal cell in board order.
		 */
		private static void MoveSlot(ArrayList cells, int dx) {
			int at = IndexOfCurrent(cells);
			int step = dx > 0 ? 1 : -1;
			int next = ((at + step) % cells.Count + cells.Count) % cells.Count;
			SetCell(cells, next);
		}

		/**
		 * MoveLine
		 * Up and down, to the nearest slot on the next line that has one.
		 *
		 * Screen direction is not line direction. DrawField draws player 1's lines
		 * in reverse so that both players' front lines meet in the middle, so moving
		 * up the screen walks the line indices one way on the near field and the
		 * other way on the far one.
		 */
		private static void MoveLine(ArrayList cells, int dy) {
			int towards = dy < 0 ? ScreenUpStep(CursorPlayer) : -ScreenUpStep(CursorPlayer);

			for (int tried = 0; tried < BasicBoard.Lines; tried++) {
				int candidate = line + towards * (tried + 1);
				if (candidate < 0 || candidate >= BasicBoard.Lines)
					continue;

				int best = NearestOnLine(cells, candidate);
				if (best >= 0) {
					SetCell(cells, best);
					return;
				}
			}
		}

		/**
		 * ScreenUpStep
		 * How a line index changes when the cursor moves one row up the screen.
		 */
		private static int ScreenUpStep(int fieldPlayer) {
			return fieldPlayer == 1 ? 1 : -1;
		}

		/**
		 * NearestOnLine
		 * The legal cell on a given line closest to the column already held, or -1.
		 */
		private static int NearestOnLine(ArrayList cells, int wanted) {
			int best = -1;
			int bestDistance = int.MaxValue;

			for (int i = 0; i < cells.Count; i++) {
				int[] cell = (int[]) cells[i];
				if (cell[0] != wanted)
					continue;

				int distance = Math.Abs(cell[1] - slot);
				if (distance < bestDistance) {
					bestDistance = distance;
					best = i;
				}
			}
			return best;
		}

		private static int IndexOfCurrent(ArrayList cells) {
			for (int i = 0; i < cells.Count; i++) {
				int[] cell = (int[]) cells[i];
				if (cell[0] == line && cell[1] == slot)
					return i;
			}
			return 0;
		}

		private static void SetCell(ArrayList cells, int index) {
			int[] cell = (int[]) cells[index];
			line = cell[0];
			slot = cell[1];
		}

		/**
		 * LegalCells
		 * Every cell the current stage may land on, in board order.
		 *
		 * Rebuilt on each move rather than cached, because placing or destroying a
		 * scroll changes the answer and a stale list would let the cursor sit
		 * somewhere the command would then refuse.
		 */
		private static ArrayList LegalCells() {
			ArrayList cells = new ArrayList();

			if (stage == Stage.Place) {
				ArrayList hand = Field.scrollLists[player, 1];
				if (handIndex < 0 || handIndex >= hand.Count)
					return cells;

				Scroll scroll = (Scroll) hand[handIndex];
				for (short l = 0; l < BasicBoard.Lines; l++)
					for (short s = 0; s < BasicBoard.Slots; s++)
						if (PlayerCommands.CanPlace(player, scroll, l, s))
							cells.Add(new int[] { l, s });
				return cells;
			}

			if (stage == Stage.Attacker) {
				for (short l = 0; l < BasicBoard.Lines; l++)
					for (short s = 0; s < BasicBoard.Slots; s++)
						if (PlayerCommands.CanAttackFrom(player, l, s))
							cells.Add(new int[] { l, s });
				return cells;
			}

			if (stage == Stage.Target) {
				short defender = (short) (1 - player);
				for (short l = 0; l < BasicBoard.Lines; l++)
					for (short s = 0; s < BasicBoard.Slots; s++)
						if (PlayerCommands.CanTarget(defender, l, s))
							cells.Add(new int[] { l, s });
				return cells;
			}

			if (stage == Stage.Mover) {
				for (short l = 0; l < BasicBoard.Lines; l++)
					for (short s = 0; s < BasicBoard.Slots; s++)
						if (PlayerCommands.CanMoveFrom(player, l, s))
							cells.Add(new int[] { l, s });
				return cells;
			}

			if (stage == Stage.Destination) {
				for (short l = 0; l < BasicBoard.Lines; l++)
					for (short s = 0; s < BasicBoard.Slots; s++)
						if (PlayerCommands.CanMoveTo(player, fromLine, fromSlot, l, s))
							cells.Add(new int[] { l, s });
				return cells;
			}

			return cells;
		}

		/**
		 * Revalidate
		 * Drops the cursor if the state it was standing on has gone.
		 *
		 * The hand shrinks when a scroll is played and the field changes when one
		 * dies, and nothing guarantees the cursor was not sitting on the part that
		 * moved.
		 */
		public static void Revalidate() {
			if (stage == Stage.None)
				return;

			if (stage == Stage.Hand) {
				// Only relocate when the held position has actually gone, or the
				// cursor would be dragged back to the first card on every keystroke
				if (!HandPlayable(handIndex) && !MoveToFirstLegalHand())
					stage = Stage.None;
				return;
			}

			ArrayList cells = LegalCells();
			if (cells.Count == 0) {
				stage = Stage.None;
				return;
			}
			SetCell(cells, IndexOfCurrent(cells));
		}

		/**
		 * StatusText
		 * The key hints for the status row, which is the only place the controls
		 * are advertised while a stage is running.
		 */
		public static string StatusText() {
			switch (stage) {
				case Stage.Hand:
					return " Choose a scroll  |  ← → move  |  Enter select  |  Esc cancel ";
				case Stage.Place:
					return " Choose a destination  |  ← → ↑ ↓ move  |  Enter place  |  Esc back ";
				case Stage.Attacker:
					return " Choose an attacker  |  ← → ↑ ↓ move  |  Enter select  |  Esc cancel ";
				case Stage.Target:
					return " Choose a target  |  ← → ↑ ↓ move  |  Enter attack  |  Esc back ";
				case Stage.Mover:
					return " Choose an entity  |  ← → ↑ ↓ move  |  Enter select  |  Esc cancel ";
				case Stage.Destination:
					return " Choose where it steps  |  ← → ↑ ↓ move  |  Enter move  |  Esc back ";
				default:
					return "";
			}
		}
	}
}
