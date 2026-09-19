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

namespace Commands {
	/**
	 * Phase
	 * The six phases of a turn, in the order they run.
	 *
	 * Regroup and Retreat are one phase under two names rather than two values:
	 * which one is running depends on whether the player destroyed anything this
	 * turn, and everything else about the phase is identical. Turn.IsRegroup tells
	 * them apart. docs/GAMEPLAY.md is the rules reference.
	 */
	public enum Phase { Draw, Interlude, Rallying, Skirmish, Regroup, End }

	/**
	 * Turn
	 * Whose turn it is, which phase is running, and what the player has left to
	 * spend in it.
	 *
	 * The rules the rest of the code asks about are the CanX methods here. Nothing
	 * copies them: PlayerCommands.CanPlace and CanAttackFrom call into this, and the
	 * selection cursor filters through those in turn, so a spent allowance removes
	 * the action from the cursor and from the typed command at the same moment.
	 *
	 * State is static in the style of Objects.Field, for the same reason: there is
	 * exactly one game.
	 */
	public class Turn {
		// The player who took the first turn, which is what closes a round
		private static short starter = 0;
		private static short active = 0;

		private static int number = 0;
		private static int round = 0;

		private static Phase phase = Phase.Draw;
		private static bool running = false;

		/*
		 * Everything below is one turn's worth of allowance and is cleared by
		 * PassTurn. Rallying grants an Entity, an Equipment and a state change; a
		 * Regroup grants a second Equipment; both Regroup and Retreat grant a
		 * reposition.
		 */
		private static bool entityPlayed;
		private static bool equipmentPlayed;
		private static bool stateChanged;
		private static bool regroupPlayed;
		private static bool repositioned;
		private static short attacksMade;
		private static short kills;
		private static bool skirmishSkipped;

		public static bool Running { get { return running; } }
		public static short Active { get { return active; } }
		public static Phase Current { get { return phase; } }
		public static int Number { get { return number; } }
		public static int Round { get { return round; } }

		/**
		 * IsRegroup
		 * Whether the phase after Skirmish is a Regroup rather than a Retreat.
		 *
		 * A Regroup is earned by destroying at least one entity on the other side of
		 * the field, and is the only one of the two that allows an Equipment card to
		 * be played.
		 */
		public static bool IsRegroup { get { return kills > 0; } }

		/**
		 * Begin
		 * Opens the game on a player's Draw Phase.
		 *
		 * @param first The player taking the first turn
		 */
		public static void Begin(short first) {
			starter = first;
			active = first;
			number = 1;
			round = 1;
			running = true;

			ResetAllowances();
			ClearAttacked(active);

			phase = Phase.Draw;
			MessageLog.Add("Turn " + number + ", round " + round + ": " + Name(active) + ".");
			Settle();
		}

		/**
		 * Pass
		 * The pass verb: give up the rest of this phase.
		 *
		 * Skipping the Skirmish Phase also skips the Regroup or Retreat that would
		 * follow it, so the decision is taken here rather than in Step: a phase in
		 * which an attack was made was not skipped, however early it is left.
		 */
		public static void Pass() {
			if (!running)
				return;

			if (phase == Phase.Skirmish && attacksMade == 0)
				skirmishSkipped = true;

			Advance();
		}

		/**
		 * Advance
		 * Leaves the current phase and settles on the next one that wants the player.
		 */
		public static void Advance() {
			if (!running)
				return;
			Step();
			Settle();
		}

		/**
		 * Settle
		 * Runs each phase's entry until one of them has something to wait for.
		 *
		 * This cannot cycle: Enter returns false for the Rallying Phase whatever the
		 * state of the board, so every lap through the six phases stops there.
		 */
		private static void Settle() {
			while (Enter())
				Step();
		}

		/**
		 * Step
		 * Moves to the next phase without entering it.
		 *
		 * Stepping off the End Phase is what passes the turn, so the phase after End
		 * is the next player's Draw.
		 */
		private static void Step() {
			switch (phase) {
				case Phase.Draw:
					phase = Phase.Interlude;
					break;
				case Phase.Interlude:
					phase = Phase.Rallying;
					break;
				case Phase.Rallying:
					phase = Phase.Skirmish;
					break;
				case Phase.Skirmish:
					phase = skirmishSkipped ? Phase.End : Phase.Regroup;
					break;
				case Phase.Regroup:
					phase = Phase.End;
					break;
				case Phase.End:
					PassTurn();
					break;
			}
		}

		/**
		 * Enter
		 * Runs whatever a phase does on arrival.
		 *
		 * @return true if the phase has nothing to wait for and the turn should carry
		 *         straight on through it
		 */
		private static bool Enter() {
			switch (phase) {
				case Phase.Draw:
					if (PlayerCommands.draw(active))
						MessageLog.Add(Name(active) + " draws.");
					return true;

				case Phase.Interlude:
					/*
					 * No card carries an effect yet, so there is never anything to
					 * resolve. This is the line an effect system replaces.
					 */
					return true;

				case Phase.Skirmish:
					if (PlayerCommands.HasAttacker(active))
						return false;
					skirmishSkipped = true;
					MessageLog.Add(Name(active) + " has nothing able to attack.");
					return true;

				case Phase.Regroup:
					MessageLog.Add(IsRegroup
						? "Regroup: " + Name(active) + " may play an equipment and reposition an entity."
						: "Retreat: " + Name(active) + " may reposition an entity.");
					return false;

				case Phase.End:
					return true;

				default:
					// Rallying waits for the player, always
					return false;
			}
		}

		/**
		 * PassTurn
		 * Hands the turn to the opponent and clears everything it spent.
		 */
		private static void PassTurn() {
			active = (short) (1 - active);
			number++;
			if (active == starter)
				round++;

			ResetAllowances();
			ClearAttacked(active);

			phase = Phase.Draw;
			MessageLog.Add("Turn " + number + ", round " + round + ": " + Name(active) + ".");
		}

		private static void ResetAllowances() {
			entityPlayed = false;
			equipmentPlayed = false;
			stateChanged = false;
			regroupPlayed = false;
			repositioned = false;
			attacksMade = 0;
			kills = 0;
			skirmishSkipped = false;
		}

		/**
		 * ClearAttacked
		 * Gives a player's entities their attack back for the turn beginning now.
		 *
		 * Empty cells hold placeholder scrolls rather than nulls, so this can clear
		 * every cell without testing what is in it.
		 */
		private static void ClearAttacked(short player) {
			for (short line = 0; line < BasicBoard.Lines; line++) {
				for (short slot = 0; slot < BasicBoard.Slots; slot++) {
					Scroll scroll = Field.playerLines[player, line, slot];
					if (scroll != null)
						scroll.attacked = false;
				}
			}
		}

		/**
		 * CanPlay
		 * Whether a scroll may be played from the hand right now.
		 *
		 * The Rallying Phase allows one Entity and one Equipment, counted separately.
		 * A Regroup allows one Equipment and no Entity; a Retreat allows nothing. No
		 * entity reaches the field outside the Rallying Phase.
		 *
		 * The Regroup Equipment is its own allowance, not the Rallying one carried
		 * forward, in the same way the two reposition allowances are separate.
		 *
		 * @param player The player trying to play it
		 * @param scroll The scroll in hand
		 */
		public static bool CanPlay(short player, Scroll scroll) {
			if (!running || player != active || scroll == null)
				return false;

			if (phase == Phase.Rallying)
				return scroll.IsEntity() ? !entityPlayed : !equipmentPlayed;

			if (phase == Phase.Regroup)
				return IsRegroup && !scroll.IsEntity() && !regroupPlayed;

			return false;
		}

		/**
		 * RecordPlay
		 * Spends whichever allowance the scroll just played came out of.
		 */
		public static void RecordPlay(Scroll scroll) {
			if (!running)
				return;

			if (phase == Phase.Rallying) {
				if (scroll != null && scroll.IsEntity())
					entityPlayed = true;
				else
					equipmentPlayed = true;
			} else if (phase == Phase.Regroup) {
				regroupPlayed = true;
			}
		}

		/**
		 * CanChangeState
		 * Whether an entity may change position right now.
		 *
		 * The Rallying Phase's state change and the Regroup or Retreat Phase's
		 * position change are the same move with two separate allowances, so a player
		 * who repositions at base camp may still reposition after a skirmish.
		 */
		public static bool CanChangeState(short player) {
			if (!running || player != active)
				return false;

			if (phase == Phase.Rallying)
				return !stateChanged;

			if (phase == Phase.Regroup)
				return !repositioned;

			return false;
		}

		public static void RecordStateChange() {
			if (phase == Phase.Rallying)
				stateChanged = true;
			else if (phase == Phase.Regroup)
				repositioned = true;
		}

		/**
		 * CanAttackNow
		 * Whether attacks may be declared at all. Which entities still have their
		 * attack is PlayerCommands.CanAttackFrom's business.
		 */
		public static bool CanAttackNow(short player) {
			return running && player == active && phase == Phase.Skirmish;
		}

		public static void RecordAttack() {
			attacksMade++;
		}

		/**
		 * RecordKill
		 * One entity destroyed on the other side of the field, which is what turns
		 * the phase after Skirmish into a Regroup rather than a Retreat.
		 */
		public static void RecordKill() {
			kills++;
		}

		/**
		 * PhaseName
		 * The phase as the player sees it, which is the only place the Regroup and
		 * Retreat distinction is spelled out.
		 */
		public static string PhaseName() {
			switch (phase) {
				case Phase.Draw: return "Draw Phase";
				case Phase.Interlude: return "Interlude Phase";
				case Phase.Rallying: return "Rallying Phase";
				case Phase.Skirmish: return "Skirmish Phase";
				case Phase.Regroup: return IsRegroup ? "Regroup Phase" : "Retreat Phase";
				case Phase.End: return "End Phase";
				default: return "";
			}
		}

		/**
		 * Banner
		 * The title row: who is acting, where the game is, and what is running.
		 */
		public static string Banner() {
			return Name(active) + "  |  Turn " + number + ", Round " + round + "  |  " + PhaseName();
		}

		public static string Name(int player) {
			return "Player " + (player + 1);
		}
	}
}
