// Default
using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

// Custom
//using System.IO;
//using System.Reflection;

// Native Namespaces
using Scrolls;
using Decks;
using Commands;
using Objects;
using Board;

namespace Scrolls
{
    public class Program
    {
        public bool quit = false;

        public static void Main(string[] args) {
            Console.Title = "Scrolls";
            Console.SetWindowSize(72,52);
            Console.WriteLine("                Welcome to Scrolls!                ");
            BasicBoard.createSlots();
            BasicBoard.printBoard();
            //Aztec.make();
            SystemCommands.drawHand();
            inputCommand();
			Console.ReadLine();
        }

        /* act - shift */
        public static void inputCommand()
        {
            string newCommand = Console.ReadLine();
            do
            {
                bool validCommand = false;
                foreach (string command in PlayerCommands.allCommands)
                {
                    if (command == newCommand)
                    {
                        validCommand = true;
                    }
                }
                if (validCommand == true)
                {
                    PlayerCommands.runCommand(newCommand);
                    Console.Clear();
                    BasicBoard.printBoard();
                    newCommand = Console.ReadLine();
                }
                else
                {
                    if (newCommand == "")
                    {
                        PlayerCommands.help(false);
                        newCommand = Console.ReadLine();
                        Console.Clear();
                        BasicBoard.printBoard();
                    }
                    else if (newCommand == "help")
                    {
                        PlayerCommands.help(true);
                        newCommand = Console.ReadLine();
                        Console.Clear();
                        BasicBoard.printBoard();
                    }
                    else if (newCommand == "quit") { }
                    else
                    {
                        Console.WriteLine(newCommand + " is not a valid command!");
                        newCommand = Console.ReadLine();
                        Console.Clear();
                        BasicBoard.printBoard();
                    }
                }
            } while (newCommand != "quit");
        }
    }
}