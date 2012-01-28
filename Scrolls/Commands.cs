/**
 * Default
 */
using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

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
		public static void DrawHand() {
			for (short counter = 6; counter != 0; counter--) {
				// Draw for Player 1, refresh the screen, and wait
				PlayerCommands.draw(0);
				Console.Clear();
				BasicBoard.PrintBoard();
				Thread.Sleep(500);

				// Draw for Player 2, refresh the screen, and wait
				PlayerCommands.draw(1);
				Console.Clear();
				BasicBoard.PrintBoard();
				Thread.Sleep(500);
			}
		}
	}

	public class PlayerCommands {
		public static string[] allCommands = { "draw" };
		
		public static void runCommand(string command) {
			if (command == "draw")
				draw(0);
			else if (command == "place")
				Console.WriteLine();
		}

		public static void help(bool valid) {
			if (valid) {
				Console.WriteLine("Valid commands are:");
				Console.WriteLine("  *help");
				Console.WriteLine("  *draw");
				Console.WriteLine("  *quit");
				Console.Write("Please input your command: ");
			} else {
				Console.WriteLine("You have not entered a command, valid commands are:");
				Console.WriteLine("  *help");
				Console.WriteLine("  *draw");
				Console.WriteLine("  *quit");
				Console.Write("Please input your command: ");
			}
		}

		public static void draw(short player) {
			/*foreach (ArrayList scroll in Field.scrollLists)
				foreach (Scroll item in scroll)
					Console.WriteLine(item);*/

			//Console.WriteLine((Field.scrollLists[player, 0].Count - 1));
			//Field.scrollLists[player, 1].Add( Field.scrollLists[player, 0][ Field.scrollLists[player, 0].Count) ] );
			//Field.scrollLists[player, 0].RemoveAt( Field.scrollLists[player, 0].Count );
            if (Field.scrollsIn[player, 1] < 11) {
                Field.scrollsIn[player, 0]--; // Decrease player's deck
                Field.scrollsIn[player, 1]++; // Increase player's hand
            } else {
                Console.Write("");
            }
		}
	}
}