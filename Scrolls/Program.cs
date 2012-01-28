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
		public bool quit = false;

		public static void Main(string[] args) {
			// Setup console window
			Console.Title = "Scrolls";
			Console.SetWindowSize(82,52);

			// Welcome the user, setup the field, print the board, make the deck, and wait
            String introduction = "Welcome to Scrolls!";
            String margin = "";
            for (int i = 0; i < (81 - introduction.Length) / 2; i++)
                margin += " ";
            BasicBoard.title = margin + introduction + margin;
		    //Console.WriteLine(BasicBoard.title);
			new Field(40,40);
			BasicBoard.PrintBoard();
			int[,] scroll1 = { { 10000000, 1 },
							   { 10000001, 3 },
							   { 10000002, 2 },
							   { 10000003, 3 } };
			int[,] scroll2 = { { 20000000, 1 },
							   { 20000001, 2 } };
			using (Deck player1 = new Deck(scroll1, 40)) {
				player1.Make();
			}
			using (Deck player2 = new Deck(scroll2, 40)) {
				player2.Make();
			}
			Thread.Sleep(1000);

			// Draw the hands and ask for a command
			SystemCommands.DrawHand();
			InputCommand();
			Console.ReadLine();
		}

		/* act - shift */
		public static void InputCommand() {
			string newCommand = Console.ReadLine();
			do {
				// Check to make sure the command is valid
				bool validCommand = false;
				foreach (string command in PlayerCommands.allCommands)
					if (command == newCommand)
						validCommand = true;

				// If command is valid, run it and refresh the board, otherwise 
				if (validCommand == true) {
					PlayerCommands.runCommand(newCommand);
					Console.Clear();
					BasicBoard.PrintBoard();
					newCommand = Console.ReadLine();
				} else {
					if (newCommand == "") {
						PlayerCommands.help(false);
						newCommand = Console.ReadLine();
						Console.Clear();
						BasicBoard.PrintBoard();
					} else if (newCommand == "help") {
						PlayerCommands.help(true);
						newCommand = Console.ReadLine();
						Console.Clear();
						BasicBoard.PrintBoard();
					} else if (newCommand == "quit") {
					} else {
						Console.WriteLine(newCommand + " is not a valid command!");
						newCommand = Console.ReadLine();
						Console.Clear();
						BasicBoard.PrintBoard();
					}
				}
			} while (newCommand != "quit");
		}
	}
}
/**
 * CHANGELOG
 * 2010-07-19T08:11 - Saved code for complete rewrite
 * 
 */