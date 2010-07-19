using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

/*
 * Native Namespaces
 */
using Scrolls;
using Decks;
using Commands;
using Objects;
using Board;

namespace Commands
{
    public class SystemCommands
    {
        public static void drawHand()
        {
            for (int counter = 6; counter != 0;counter--)
            {
                PlayerCommands.draw(1);
                Console.Clear();
                BasicBoard.printBoard();
                System.Threading.Thread.Sleep(500);
                PlayerCommands.draw(2);
                Console.Clear();
                BasicBoard.printBoard();
                System.Threading.Thread.Sleep(500);
            }
        }
    }

    public class PlayerCommands
    {
        public static string[] allCommands = { "draw" };

        public static void runCommand(string command)
        {
            if (command == "draw")
            {
                draw(1);
            }
            else if (command == "place")
            {

            }
        }

        public static void help(bool valid)
        {
            if (valid)
            {
                Console.WriteLine("Valid commands are:");
                Console.WriteLine("  *help");
                Console.WriteLine("  *draw");
                Console.WriteLine("  *quit");
                Console.Write("Please input your command: ");
            }
            else
            {
                Console.WriteLine("You have not entered a command, valid commands are:");
                Console.WriteLine("  *help");
                Console.WriteLine("  *draw");
                Console.WriteLine("  *quit");
                Console.Write("Please input your command: ");
            }
        }

        public static void draw(int player)
        {
            if (player == 1)
            {
                Field.player1scrollsInDeck--;
                Field.player1scrollsInHand++;
            }
            else if (player == 2)
            {
                Field.player2scrollsInDeck--;
                Field.player2scrollsInHand++;
            }
            else
            {
                Console.WriteLine("Error: Incorrect usage of draw().");
            }
        }
    }
}