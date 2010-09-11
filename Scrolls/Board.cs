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
	public class BasicBoard {
		public static void PrintBoard() {
			short[] two = GetDeckCount(Field.scrollsIn[1, 0]);
			short[] one = GetDeckCount(Field.scrollsIn[0, 0]);

			string margin = "                      ";
			string top    = "┌─────┬─────┬─────┬─────┬─────┬─────┐";
			string bars   = "│     │     │     │     │     │     │";
			string types  = "│";
			string names  = "│";
			string middle = "├─────┼─────┼─────┼─────┼─────┼─────│";
			string bottom = "└─────┴─────┴─────┴─────┴─────┴─────┘";
			string midtop = "┌─────┐                       ┌─────┐";
			string mid2up = "│     ├───────────┬───────────┤     │";
			string midone = "│     │    " + two[0] + "      │    " + one[0] + "      │     │";
			string midtwo = "│     │      " + two[1] + "    │      " + one[1] + "    │     │";
			string mid2dn = "│     ├───────────┴───────────┤     │";
			string midbot = "└─────┘                       └─────┘";
			
			string handTop = "┌─────┐";
			string handMid = "│     │";
			string handBot = "└─────┘";
			string handMargin = GetHandMargin(1);
			String[] handLines = new String[6];

			short handNum = Field.scrollsIn[1, 1];
			// Initialize Stings for 
			for (short counter = 0; counter < handLines.Length; counter++)
				handLines[counter] = "";

			for (short counter = 0; counter < handNum; counter++) {
				handLines[0] += handTop;
				handLines[1] += handMid;
				handLines[2] += handBot;
				if (counter != handNum - 1) {
					handLines[0] += " ";
					handLines[1] += " ";
					handLines[2] += " ";
				}
			}

			WriteMargin(handMargin, handLines[0]);
			for (short counter = 0; counter < 3; counter++)
				WriteMargin(handMargin, handLines[1]);
			WriteMargin(handMargin, handLines[2]);

			Console.WriteLine(); 
			WriteMargin(margin, top);
			for (short line = 2; line > -1; line--) {
				WriteMargin(margin, bars);
				for (short scroll = 0; scroll < 6; scroll++)
					types += Field.playerLines[1, line, scroll].typeAbb + "│";
				WriteMargin(margin, types);
				types = "│";

				for (short scroll = 0; scroll < 6; scroll++)
					names += Field.playerLines[1, line, scroll].nameAbb + "│";
				WriteMargin(margin, names);
				names = "│";

				if (line != 0)
					WriteMargin(margin, middle);
			}
			WriteMargin(margin, bottom);

			// Write the middle section of the field
			Console.WriteLine();
			WriteMargin(margin, midtop);
			WriteMargin(margin, mid2up);
			WriteMargin(margin, midone);
			WriteMargin(margin, midtwo);
			WriteMargin(margin, mid2dn);
			WriteMargin(margin, midbot);
			Console.WriteLine();

			WriteMargin(margin, top);
			for (short line = 2; line > -1; line--) {
				for (short scroll = 0; scroll < 6; scroll++)
					names += Field.playerLines[0, line, scroll].nameAbb + "│";
				WriteMargin(margin, names);
				names = "│";

				for (short scroll = 0; scroll < 6; scroll++)
					types += Field.playerLines[0, line, scroll].typeAbb + "│";
				WriteMargin(margin, types);
				types = "│";
				WriteMargin(margin, bars);
				
				if (line != 0)
					WriteMargin(margin, middle);
			}
			WriteMargin(margin, bottom);
		}

		/**
		 * GetDeckCount
		 * Converts 
		 * 
		 * @param  scrollsInDeck
		 * @return An array of 0:tens,1:ones
		 */
		private static short[] GetDeckCount(short scrollsInDeck) {
			short tens;
			short ones;
			if (scrollsInDeck != 0) {
				tens = (short) (scrollsInDeck / 10);
				if (tens != 0)
					ones = (short) (scrollsInDeck % (tens * 10));
				else
					ones = scrollsInDeck;
			} else {
				tens = 0;
				ones = 0;
			}
			short[] returnArray = {tens,ones}; // Create the array of tens and ones
			return returnArray;
		}

		/**
		 * GetHandMargin
		 * 
		 * 
		 * @param  player  
		 * @return        
		 */
		private static string GetHandMargin(short player) {
			short hand = Field.scrollsIn[player, 1];
			short spaces = (short) (81 - hand * 7);
			if (hand > 1)
				spaces -= (short) (hand - 1);
			Console.WriteLine(spaces);
			spaces = (short) ((spaces - 1) / 2);
			Console.WriteLine(spaces);
			string margin = "";
			for (short counter = 0; counter < spaces; counter++)
				margin += " ";
			return margin;
		}

		/**
		 * Prints a line with margins on both sides
		 * 
		 * @param margin   The margin variable
		 * @param variable The string to print
		 */
		private static void WriteMargin(string margin,string variable) {
			Console.WriteLine(margin + variable + margin);
		}
	}
}