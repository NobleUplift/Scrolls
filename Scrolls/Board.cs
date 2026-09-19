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
	 * MessageLog
	 * A bounded scrollback of command output.
	 *
	 * The board is repainted constantly, so anything written straight to the
	 * console is destroyed before it can be read. Messages go here instead and are
	 * drawn into their own region of every frame.
	 */
	public class MessageLog {
		private const int Capacity = 200;
		private static ArrayList lines = new ArrayList();

		public static void Add(string text) {
			if (text == null)
				text = "";
			lines.Add(text);
			while (lines.Count > Capacity)
				lines.RemoveAt(0);
		}

		public static int Count {
			get { return lines.Count; }
		}

		/**
		 * Tail
		 * The last count messages, oldest first, for drawing into a region of
		 * fixed height.
		 */
		public static string[] Tail(int count) {
			if (count < 0)
				count = 0;
			int take = Math.Min(count, lines.Count);
			string[] result = new string[take];
			for (int i = 0; i < take; i++)
				result[i] = (string) lines[lines.Count - take + i];
			return result;
		}
	}

	/**
	 * BasicBoard
	 * Composes the battlefield into the Screen back buffer.
	 *
	 * Every width here derives from SlotInterior and Slots rather than being a
	 * hand-tuned literal, because the previous renderer's field rows and hand rows
	 * disagreed by one column and sheared the grid.
	 *
	 * The whole board is drawn in single rules. Double rules are reserved, board
	 * wide, to mean "selected": the grid used to divide its own slots with ║ and ═,
	 * which left the renderer no heavier weight to mark a cursor with. Anything that
	 * adds a permanent double line here takes that vocabulary back again.
	 */
	public class BasicBoard {
		public const int Slots = 6;
		public const int Lines = 3;
		public const int SlotInterior = 5;

		// 1 leading rule, then each slot contributes its interior plus one trailing rule
		public const int BoardWidth = 1 + Slots * (SlotInterior + 1); // 37

		public const int MaxHand = 11;

		// Heights of each band, in full and compact form
		private const int FullLineRows = 3;
		private const int CompactLineRows = 1;
		private const int FullFieldRows = 1 + Lines * FullLineRows + (Lines - 1) + 1;       // 13
		private const int CompactFieldRows = 1 + Lines * CompactLineRows + (Lines - 1) + 1; //  7
		private const int FullMiddleRows = 6;
		private const int CompactMiddleRows = 3;
		private const int FullHandRows = 3;
		private const int CompactHandRows = 1;

		private const int FullTotal = 1 + FullHandRows + FullFieldRows + FullMiddleRows + FullFieldRows + FullHandRows;
		private const int CompactTotal = 1 + CompactHandRows + CompactFieldRows + CompactMiddleRows + CompactFieldRows + CompactHandRows;

		/*
		 * A full hand laid out compactly is wider than the board itself, so the
		 * minimum width is driven by the hand, not by the field. Below this the
		 * hand would be clipped at both ends and a player could not see what they
		 * were holding.
		 */
		private const int WidestRow = MaxHand * (SlotInterior + 1) - 1; // 65
		public const int MinWidth = WidestRow + 2;
		public const int MinHeight = CompactTotal + 3;

		// Shown until the first turn begins, after which Turn.Banner has the row
		public static string title = "";

		private static ConsoleColor Fg = Screen.DefaultFg;
		private static ConsoleColor Bg = Screen.DefaultBg;
		private static ConsoleColor Rule = ConsoleColor.DarkGray;
		private static ConsoleColor Card = ConsoleColor.White;
		private static ConsoleColor Dim = ConsoleColor.DarkGray;

		/*
		 * The cursor and the choice it has already made are both drawn as double
		 * rules, so they need to be told apart by colour: the live one is bright,
		 * the settled one behind it is not.
		 */
		private static ConsoleColor Cursor = ConsoleColor.Yellow;
		private static ConsoleColor Chosen = ConsoleColor.DarkYellow;

		/**
		 * Slot and hand rectangles from the most recent frame.
		 *
		 * Nothing selects a card yet, but the renderer is the only thing that knows
		 * where a slot landed on screen. Recording it here means a later cursor can
		 * highlight a slot through Screen.Recolour without the layout maths being
		 * duplicated or the board being redrawn a second way.
		 */
		private static Rect[, ,] slotRects = new Rect[2, Lines, Slots];
		private static Rect[,] handRects = new Rect[2, MaxHand];
		private static Rect[] battlefieldRects = new Rect[2];

		/*
		 * Whether the last frame drew each hand as boxes or as a bare row. A boxed
		 * card has rules to promote to double lines; a bare one has only the gap
		 * either side of it.
		 */
		private static bool[] handBoxed = new bool[2];

		public static Rect SlotRect(int player, int line, int slot) {
			return slotRects[player, line, slot];
		}

		public static Rect HandRect(int player, int index) {
			return handRects[player, index];
		}

		public static Rect BattlefieldRect(int player) {
			return battlefieldRects[player];
		}

		private static bool tooSmall;
		private static int promptRow;
		private static int promptColumn;

		/**
		 * TooSmall
		 * Whether the last frame was the "resize me" notice rather than a board.
		 * The input loop uses this to accept a bare Q, since no prompt is visible.
		 */
		public static bool TooSmall { get { return tooSmall; } }

		public static int PromptRow { get { return promptRow; } }
		public static int PromptColumn { get { return promptColumn; } }

		/**
		 * Render
		 * Draws one complete frame and pushes it to the terminal.
		 *
		 * @param input The text the player has typed so far, shown at the prompt
		 */
		public static void Render(string input) {
			Screen.EnsureSize();
			Screen.Clear();

			tooSmall = Screen.Width < MinWidth || Screen.Height < MinHeight;
			if (tooSmall) {
				DrawTooSmall();
				Screen.Flush();
				return;
			}

			bool full = Screen.Height >= FullTotal + 3;
			int left = (Screen.Width - BoardWidth) / 2;

			int fieldRows = full ? FullFieldRows : CompactFieldRows;
			int middleRows = full ? FullMiddleRows : CompactMiddleRows;
			int handRows = full ? FullHandRows : CompactHandRows;

			/*
			 * Once a turn is running the title row is the only thing on screen that
			 * says whose turn it is, which matters when both players share a console.
			 */
			int y = 0;
			Screen.WriteCentred(y, Turn.Running ? Turn.Banner() : title, ConsoleColor.Yellow, Bg);
			y += 1;

			DrawHand(1, left, y, handRows);
			y += handRows;

			DrawField(1, left, y, full);
			y += fieldRows;

			DrawMiddle(left, y, full);
			y += middleRows;

			DrawField(0, left, y, full);
			y += fieldRows;

			DrawHand(0, left, y, handRows);
			y += handRows;

			/*
			 * Last, because it overwrites rules the bands have already drawn and
			 * needs every rect of this frame to be recorded. It must also happen
			 * before the Flush below, which is why no caller outside this method
			 * could do it.
			 */
			DrawSelection();

			// Whatever is left over becomes scrollback, with the last two rows reserved
			promptRow = Screen.Height - 1;
			int statusRow = Screen.Height - 2;
			int logTop = y;
			int logRows = statusRow - logTop;

			DrawLog(logTop, logRows);
			DrawStatus(statusRow);
			DrawPrompt(promptRow, input);

			Screen.Flush();
			Screen.PlaceCursor(promptColumn, promptRow, true);
		}

		private static void DrawTooSmall() {
			string[] message = {
				"Terminal too small.",
				"",
				"Needs at least " + MinWidth + " x " + MinHeight + ".",
				"This window is " + Screen.Width + " x " + Screen.Height + ".",
				"",
				"Resize the window, or press Q to quit."
			};
			// The prompt is not drawn at this size, so Q is handled directly by the input loop
			int top = Math.Max(0, (Screen.Height - message.Length) / 2);
			for (int i = 0; i < message.Length; i++)
				Screen.WriteCentred(top + i, message[i], ConsoleColor.Red, Bg);
		}

		/**
		 * DrawField
		 * One player's three lines of six slots.
		 */
		private static void DrawField(int player, int left, int top, bool full) {
			int y = top;
			Screen.Write(left, y, Rule6('┌', '─', '┬', '┐'), Rule, Bg);
			y++;

			/*
			 * Player 1 reads bottom-up so that the line nearest the middle of the
			 * screen is the front line for both players.
			 */
			for (int step = 0; step < Lines; step++) {
				int line = (player == 1) ? (Lines - 1 - step) : step;

				if (full) {
					DrawSlotRow(player, line, left, y, SlotField.Name);
					DrawSlotRow(player, line, left, y + 1, SlotField.Type);
					DrawSlotRow(player, line, left, y + 2, SlotField.Endurance);
					for (int slot = 0; slot < Slots; slot++)
						slotRects[player, line, slot] = new Rect(SlotX(left, slot), y, SlotInterior, FullLineRows);
					y += FullLineRows;
				} else {
					DrawSlotRow(player, line, left, y, SlotField.Name);
					for (int slot = 0; slot < Slots; slot++)
						slotRects[player, line, slot] = new Rect(SlotX(left, slot), y, SlotInterior, CompactLineRows);
					y += CompactLineRows;
				}

				if (step != Lines - 1) {
					Screen.Write(left, y, Rule6('├', '─', '┼', '┤'), Rule, Bg);
					y++;
				}
			}

			Screen.Write(left, y, Rule6('└', '─', '┴', '┘'), Rule, Bg);
		}

		private enum SlotField { Name, Type, Endurance }

		/**
		 * DrawSlotRow
		 * One horizontal strip across all six slots of a line.
		 */
		private static void DrawSlotRow(int player, int line, int left, int y, SlotField which) {
			Screen.Put(left, y, '│', Rule, Bg);
			for (int slot = 0; slot < Slots; slot++) {
				Scroll scroll = Field.playerLines[player, line, slot];
				string text = SlotText(scroll, which);
				Screen.Write(SlotX(left, slot), y, Fit(text), IsEmpty(scroll) ? Dim : Card, Bg);
				Screen.Put(SlotX(left, slot) + SlotInterior, y, '│', Rule, Bg);
			}
		}

		private static string SlotText(Scroll scroll, SlotField which) {
			if (IsEmpty(scroll))
				return "";
			switch (which) {
				case SlotField.Name:
					return scroll.nameAbb;
				case SlotField.Type:
					return scroll.typeAbb;
				case SlotField.Endurance:
					return scroll.endurance.ToString();
				default:
					return "";
			}
		}

		private static bool IsEmpty(Scroll scroll) {
			return scroll == null || scroll.id == 0;
		}

		/**
		 * SlotX
		 * Left edge of a slot's interior, counted from the board's left rule.
		 */
		private static int SlotX(int left, int slot) {
			return left + 1 + slot * (SlotInterior + 1);
		}

		/**
		 * Rule6
		 * A full-width horizontal rule with a junction between each pair of slots.
		 */
		private static string Rule6(char start, char fill, char junction, char end) {
			string result = start.ToString();
			for (int slot = 0; slot < Slots; slot++) {
				result += new string(fill, SlotInterior);
				result += (slot == Slots - 1) ? end : junction;
			}
			return result;
		}

		/**
		 * DrawMiddle
		 * The band between the two fields: each player's battlefield scroll on the
		 * outside, their deck and void counts on the inside.
		 */
		private static void DrawMiddle(int left, int top, bool full) {
			int inner = BoardWidth - 2 * (SlotInterior + 2); // 23
			int half = (inner - 1) / 2;                      // 11
			int leftBoxX = left + 1;
			int rightBoxX = left + BoardWidth - SlotInterior - 1;
			int centreX = left + SlotInterior + 2;

			if (full) {
				Screen.Write(left, top, "┌" + new string('─', SlotInterior) + "┐" + new string(' ', inner) + "┌" + new string('─', SlotInterior) + "┐", Rule, Bg);
				Screen.Write(left, top + 1, "│" + new string(' ', SlotInterior) + "├" + new string('─', half) + "┬" + new string('─', half) + "┤" + new string(' ', SlotInterior) + "│", Rule, Bg);
				Screen.Write(left, top + 2, "│" + new string(' ', SlotInterior) + "│" + new string(' ', half) + "│" + new string(' ', half) + "│" + new string(' ', SlotInterior) + "│", Rule, Bg);
				Screen.Write(left, top + 3, "│" + new string(' ', SlotInterior) + "│" + new string(' ', half) + "│" + new string(' ', half) + "│" + new string(' ', SlotInterior) + "│", Rule, Bg);
				Screen.Write(left, top + 4, "│" + new string(' ', SlotInterior) + "├" + new string('─', half) + "┴" + new string('─', half) + "┤" + new string(' ', SlotInterior) + "│", Rule, Bg);
				Screen.Write(left, top + 5, "└" + new string('─', SlotInterior) + "┘" + new string(' ', inner) + "└" + new string('─', SlotInterior) + "┘", Rule, Bg);

				DrawCount(centreX + 1, top + 2, half - 2, "Deck", Field.scrollsIn[1, 0]);
				DrawCount(centreX + 1, top + 3, half - 2, "Void", Field.scrollsIn[1, 2]);
				DrawCount(centreX + half + 2, top + 2, half - 2, "Deck", Field.scrollsIn[0, 0]);
				DrawCount(centreX + half + 2, top + 3, half - 2, "Void", Field.scrollsIn[0, 2]);

				battlefieldRects[1] = new Rect(leftBoxX, top + 1, SlotInterior, 4);
				battlefieldRects[0] = new Rect(rightBoxX, top + 1, SlotInterior, 4);
				DrawBattlefield(1, leftBoxX, top + 2);
				DrawBattlefield(0, rightBoxX, top + 2);
			} else {
				Screen.Write(left, top, "┌" + new string('─', SlotInterior) + "┬" + new string('─', half) + "┬" + new string('─', half) + "┬" + new string('─', SlotInterior) + "┐", Rule, Bg);
				Screen.Write(left, top + 1, "│" + new string(' ', SlotInterior) + "│" + new string(' ', half) + "│" + new string(' ', half) + "│" + new string(' ', SlotInterior) + "│", Rule, Bg);
				Screen.Write(left, top + 2, "└" + new string('─', SlotInterior) + "┴" + new string('─', half) + "┴" + new string('─', half) + "┴" + new string('─', SlotInterior) + "┘", Rule, Bg);

				DrawCount(centreX + 1, top + 1, half - 2, "Deck", Field.scrollsIn[1, 0]);
				DrawCount(centreX + half + 2, top + 1, half - 2, "Deck", Field.scrollsIn[0, 0]);

				battlefieldRects[1] = new Rect(leftBoxX, top + 1, SlotInterior, 1);
				battlefieldRects[0] = new Rect(rightBoxX, top + 1, SlotInterior, 1);
				DrawBattlefield(1, leftBoxX, top + 1);
				DrawBattlefield(0, rightBoxX, top + 1);
			}
		}

		private static void DrawBattlefield(int player, int x, int y) {
			Scroll scroll = Field.battlefields[player];
			Screen.Write(x, y, Fit(IsEmpty(scroll) ? "" : scroll.nameAbb), IsEmpty(scroll) ? Dim : Card, Bg);
		}

		/**
		 * DrawCount
		 * A labelled number, label left and value right within the given width.
		 *
		 * The old renderer split counts into tens and ones and placed each digit by
		 * hand, which broke the row width as soon as a count went negative or above
		 * ninety-nine. Formatting into a fixed field cannot do that.
		 */
		private static void DrawCount(int x, int y, int width, string label, short value) {
			string text = value.ToString();
			Screen.Write(x, y, label, Dim, Bg);
			Screen.Write(x + width - text.Length, y, text, Card, Bg);
		}

		/**
		 * DrawHand
		 * A player's hand, as boxed cards when there is room and as a plain row of
		 * abbreviations when there is not.
		 */
		private static void DrawHand(int player, int left, int top, int rows) {
			int hand = Field.scrollsIn[player, 1];
			if (hand < 0)
				hand = 0;
			if (hand > MaxHand)
				hand = MaxHand;

			ArrayList held = Field.scrollLists[player, 1];

			if (rows >= FullHandRows) {
				int cardWidth = SlotInterior + 2; // a box around the interior
				int span = hand * cardWidth + Math.Max(0, hand - 1);
				if (span <= Screen.Width) {
					int x = (Screen.Width - span) / 2;
					for (int i = 0; i < hand; i++) {
						int cx = x + i * (cardWidth + 1);
						Screen.Write(cx, top, "┌" + new string('─', SlotInterior) + "┐", Rule, Bg);
						Screen.Write(cx, top + 1, "│", Rule, Bg);
						Screen.Write(cx + 1, top + 1, Fit(HandName(held, i)), Card, Bg);
						Screen.Write(cx + SlotInterior + 1, top + 1, "│", Rule, Bg);
						Screen.Write(cx, top + 2, "└" + new string('─', SlotInterior) + "┘", Rule, Bg);
						/*
						 * Interior only, in both axes, matching slotRects. It used to
						 * span the box's height but not its width, so a highlight
						 * covered the border rows without covering the border columns.
						 */
						handRects[player, i] = new Rect(cx + 1, top + 1, SlotInterior, 1);
					}
					handBoxed[player] = true;
					return;
				}
			}

			// Compact: one row of abbreviations, centred
			int compactWidth = SlotInterior + 1;
			int compactSpan = Math.Max(0, hand * compactWidth - 1);
			int startX = (Screen.Width - compactSpan) / 2;
			int row = top + rows - 1;
			for (int i = 0; i < hand; i++) {
				int cx = startX + i * compactWidth;
				Screen.Write(cx, row, Fit(HandName(held, i)), Card, Bg);
				handRects[player, i] = new Rect(cx, row, SlotInterior, 1);
			}
			handBoxed[player] = false;
		}

		/**
		 * DrawSelection
		 * Marks whatever the cursor is pointing at, and whatever it has already
		 * settled on.
		 *
		 * The board is drawn entirely in single rules, so a selection is shown by
		 * promoting the rules around one card to double lines. Nothing is moved or
		 * redrawn; only the boundary characters are overwritten, and the geometry
		 * comes from the rectangles the bands recorded rather than being worked out
		 * a second way.
		 */
		private static void DrawSelection() {
			if (!Selection.Active)
				return;

			short actor = Selection.Player;

			switch (Selection.Current) {
				case Stage.Hand:
					MarkHand(actor, Selection.HandIndex, Cursor);
					break;

				case Stage.Place:
					MarkHand(actor, Selection.HandIndex, Chosen);
					MarkSlot(actor, Selection.Line, Selection.Slot, Cursor);
					break;

				case Stage.Attacker:
					MarkSlot(actor, Selection.Line, Selection.Slot, Cursor);
					break;

				case Stage.Target:
					MarkSlot(actor, Selection.FromLine, Selection.FromSlot, Chosen);
					MarkSlot(Selection.CursorPlayer, Selection.Line, Selection.Slot, Cursor);
					break;

				case Stage.Mover:
					MarkSlot(actor, Selection.Line, Selection.Slot, Cursor);
					break;

				case Stage.Destination:
					MarkSlot(actor, Selection.FromLine, Selection.FromSlot, Chosen);
					MarkSlot(actor, Selection.Line, Selection.Slot, Cursor);
					break;
			}
		}

		/**
		 * MarkSlot
		 * Doubles the rules around one field cell.
		 *
		 * The cell's own horizontal edges become ═ as well as its verticals becoming
		 * ║, because inside a full grid the verticals alone would not say which of
		 * the two neighbouring cells was meant.
		 */
		private static void MarkSlot(int player, int line, int slot, ConsoleColor colour) {
			if (player < 0 || player > 1 || line < 0 || line >= Lines || slot < 0 || slot >= Slots)
				return;

			Rect area = slotRects[player, line, slot];
			MarkSides(area, colour);
			MarkRule(area, area.y - 1, '╥', colour, true);
			MarkRule(area, area.y + area.height, '╨', colour, true);
		}

		/**
		 * MarkHand
		 * Doubles the rules around one card in a hand.
		 *
		 * A boxed card has its own frame to promote. A card in the bare compact row
		 * has none, so the one column of clearance either side carries the mark
		 * instead; that costs no extra row and so leaves MinHeight alone.
		 */
		private static void MarkHand(int player, int index, ConsoleColor colour) {
			if (player < 0 || player > 1 || index < 0 || index >= MaxHand)
				return;
			if (index >= Field.scrollLists[player, 1].Count)
				return;

			Rect area = handRects[player, index];
			MarkSides(area, colour);

			if (handBoxed[player]) {
				Screen.Put(area.x - 1, area.y - 1, '╓', colour, Bg);
				Screen.Put(area.x + area.width, area.y - 1, '╖', colour, Bg);
				Screen.Put(area.x - 1, area.y + area.height, '╙', colour, Bg);
				Screen.Put(area.x + area.width, area.y + area.height, '╜', colour, Bg);
			}
		}

		/**
		 * MarkSides
		 * Turns the vertical rules either side of a region into double lines.
		 */
		private static void MarkSides(Rect area, ConsoleColor colour) {
			for (int y = area.y; y < area.y + area.height; y++) {
				Screen.Put(area.x - 1, y, '║', colour, Bg);
				Screen.Put(area.x + area.width, y, '║', colour, Bg);
			}
		}

		/**
		 * MarkRule
		 * One horizontal edge of a marked cell, with the junctions at its ends.
		 *
		 * ╥ and ╨ carry a single horizontal, so the ═ run reads as belonging to the
		 * marked cell and the surrounding grid stays unbroken either side of it.
		 */
		private static void MarkRule(Rect area, int y, char junction, ConsoleColor colour, bool fill) {
			Screen.Put(area.x - 1, y, junction, colour, Bg);
			Screen.Put(area.x + area.width, y, junction, colour, Bg);
			if (!fill)
				return;
			for (int x = area.x; x < area.x + area.width; x++)
				Screen.Put(x, y, '═', colour, Bg);
		}

		private static string HandName(ArrayList held, int index) {
			if (held == null || index >= held.Count)
				return "?";
			Scroll scroll = (Scroll) held[index];
			return scroll == null ? "?" : scroll.nameAbb;
		}

		/**
		 * DrawLog
		 * The scrollback region. This is the part that makes the board readable:
		 * command output survives the next repaint instead of being cleared away.
		 */
		private static void DrawLog(int top, int rows) {
			if (rows <= 0)
				return;
			Screen.Write(0, top, new string('─', Screen.Width), Rule, Bg);
			int textRows = rows - 1;
			if (textRows <= 0)
				return;
			string[] tail = MessageLog.Tail(textRows);
			for (int i = 0; i < tail.Length; i++)
				Screen.Write(1, top + 1 + i, Clip(tail[i], Screen.Width - 2), Fg, Bg);
		}

		/**
		 * DrawStatus
		 * The verb list, or the cursor's key hints while a selection is running.
		 *
		 * The prompt is inert during a selection, so this row is the only place the
		 * arrow keys are advertised. The verb list is built by PlayerCommands from
		 * the phase that is running rather than being a literal kept in step by hand.
		 */
		private static void DrawStatus(int row) {
			string text = Selection.Active ? Selection.StatusText() : PlayerCommands.StatusText();
			ConsoleColor colour = Selection.Active ? Cursor : ConsoleColor.DarkCyan;
			Screen.Write(0, row, Clip(text, Screen.Width), colour, Bg);
		}

		private static void DrawPrompt(int row, string input) {
			string prefix = "> ";
			Screen.Write(0, row, prefix, ConsoleColor.Green, Bg);
			string shown = input == null ? "" : input;
			int room = Screen.Width - prefix.Length - 1;
			if (shown.Length > room)
				shown = shown.Substring(shown.Length - room);
			Screen.Write(prefix.Length, row, shown, Fg, Bg);
			promptColumn = prefix.Length + shown.Length;
		}

		/**
		 * Fit
		 * Pads or truncates to the slot interior.
		 *
		 * The abbreviation columns are nvarchar(4) but empty slots defaulted to five
		 * spaces, so filled and empty cells were different widths. Forcing every
		 * value through here means no card can widen a row.
		 */
		private static string Fit(string text) {
			return Clip(text == null ? "" : text, SlotInterior).PadRight(SlotInterior);
		}

		private static string Clip(string text, int width) {
			if (text == null)
				return "";
			if (width <= 0)
				return "";
			return text.Length > width ? text.Substring(0, width) : text;
		}
	}
}
