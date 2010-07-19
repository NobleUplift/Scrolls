// Default
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

namespace Board
{
    public class BasicBoard
    {
        public static void createSlots()
        {
            /**
             * Player 1
             */
            Field.player1scrollsInDeck = 40;
            Field.player1scrollsInHand = 0;
            Field.player1scrollsInVoid = 0;
            //Field.player1deck = {new Scroll()};
            //Field.player1hand = {new Scroll()};
            //Field.player1void = {new Scroll()};
            Field.player1battlefield = new Scroll();

            Field.player1frontLine1 = new Scroll();
            Field.player1frontLine2 = new Scroll();
            Field.player1frontLine3 = new Scroll();
            Field.player1frontLine4 = new Scroll();
            Field.player1frontLine5 = new Scroll();
            Field.player1frontLine6 = new Scroll();

            Field.player1forwardLine1 = new Scroll();
            Field.player1forwardLine2 = new Scroll();
            Field.player1forwardLine3 = new Scroll();
            Field.player1forwardLine4 = new Scroll();
            Field.player1forwardLine5 = new Scroll();
            Field.player1forwardLine6 = new Scroll();

            Field.player1rearLine1 = new Scroll();
            Field.player1rearLine2 = new Scroll();
            Field.player1rearLine3 = new Scroll();
            Field.player1rearLine4 = new Scroll();
            Field.player1rearLine5 = new Scroll();
            Field.player1rearLine6 = new Scroll();


            /*
             * Player 2
             */
            Field.player2scrollsInDeck = 40;
            Field.player2scrollsInHand = 0;
            Field.player2scrollsInVoid = 0;
            //Field.player2deck = {new Scroll()};
            //Field.player2hand = {new Scroll()};
            //Field.player2void = {new Scroll()};
            Field.player2battlefield = new Scroll();

            Field.player2frontLine1 = new Scroll();
            Field.player2frontLine2 = new Scroll();
            Field.player2frontLine3 = new Scroll();
            Field.player2frontLine4 = new Scroll();
            Field.player2frontLine5 = new Scroll();
            Field.player2frontLine6 = new Scroll();

            Field.player2forwardLine1 = new Scroll();
            Field.player2forwardLine2 = new Scroll();
            Field.player2forwardLine3 = new Scroll();
            Field.player2forwardLine4 = new Scroll();
            Field.player2forwardLine5 = new Scroll();
            Field.player2forwardLine6 = new Scroll();

            Field.player2rearLine1 = new Scroll();
            Field.player2rearLine2 = new Scroll();
            Field.player2rearLine3 = new Scroll();
            Field.player2rearLine4 = new Scroll();
            Field.player2rearLine5 = new Scroll();
            Field.player2rearLine6 = new Scroll();
        }

        public static void printBoard()
        {
            int player1tens;
            int player1ones;
            if (Field.player1scrollsInDeck != 0)
            {
                player1tens = (int)(Field.player1scrollsInDeck / 10);
                if (player1tens != 0)
                {
                    player1ones = Field.player1scrollsInDeck % (player1tens * 10);
                }
                else
                {
                    player1ones = Field.player1scrollsInDeck;
                }
            }
            else
            {
                player1tens = 0;
                player1ones = 0;
            }

            int player2tens;
            int player2ones;
            if (Field.player2scrollsInDeck != 0)
            {
                player2tens = (int)(Field.player2scrollsInDeck / 10);
                if (player2tens != 0)
                {
                    player2ones = Field.player2scrollsInDeck % (player2tens * 10);
                }
                else
                {
                    player2ones = Field.player2scrollsInDeck;
                }
            }
            else
            {
                player2tens = 0;
                player2ones = 0;
            }

            String[] player1handGUI = new String[6];
            player1handGUI[0] = "";
            player1handGUI[1] = "";
            player1handGUI[2] = "";
            player1handGUI[3] = "";
            player1handGUI[4] = "";
            player1handGUI[5] = "";

            int spaces = 71 - (6 * Field.player2scrollsInHand) - (int) (1 * ((uint) Field.player2scrollsInHand - 1));

            for (int lines = 0; lines < player1handGUI.Length; lines++) {
                for (int counter = 0; counter <= spaces; counter++)
                {
                    player1handGUI[lines] += " ";
                }
                if (lines == 0) {

                }
            }

            Console.WriteLine(player1handGUI[0]);
            Console.WriteLine(player1handGUI[1]);
            Console.WriteLine(player1handGUI[2]);
            Console.WriteLine(player1handGUI[3]);
            Console.WriteLine(player1handGUI[4]);
            Console.WriteLine(player1handGUI[5]);

            Console.ReadLine();

            /* if (Field.player2scrollsInHand == 0)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 1)
            {
                Console.WriteLine("                                 ┌────┐                                ");
                Console.WriteLine("                                 │    │                                ");
                Console.WriteLine("                                 │    │                                ");
                Console.WriteLine("                                 │    │                                ");
                Console.WriteLine("                                 └────┘                                ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 2)
            {
                Console.WriteLine("                             ┌────┐ ┌────┐                             ");
                Console.WriteLine("                             │    │ │    │                             ");
                Console.WriteLine("                             │    │ │    │                             ");
                Console.WriteLine("                             │    │ │    │                             ");
                Console.WriteLine("                             └────┘ └────┘                             ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 3)
            {
                Console.WriteLine("                         ┌────┐ ┌────┐ ┌────┐                          ");
                Console.WriteLine("                         │    │ │    │ │    │                          ");
                Console.WriteLine("                         │    │ │    │ │    │                          ");
                Console.WriteLine("                         │    │ │    │ │    │                          ");
                Console.WriteLine("                         └────┘ └────┘ └────┘                          ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 4)
            {
                Console.WriteLine("                      ┌────┐ ┌────┐ ┌────┐ ┌────┐                      ");
                Console.WriteLine("                      │    │ │    │ │    │ │    │                      ");
                Console.WriteLine("                      │    │ │    │ │    │ │    │                      ");
                Console.WriteLine("                      │    │ │    │ │    │ │    │                      ");
                Console.WriteLine("                      └────┘ └────┘ └────┘ └────┘                      ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 5)
            {
                Console.WriteLine("                   ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐                  ");
                Console.WriteLine("                   │    │ │    │ │    │ │    │ │    │                  ");
                Console.WriteLine("                   │    │ │    │ │    │ │    │ │    │                  ");
                Console.WriteLine("                   │    │ │    │ │    │ │    │ │    │                  ");
                Console.WriteLine("                   └────┘ └────┘ └────┘ └────┘ └────┘                  ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 6)
            {
                Console.WriteLine("               ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐               ");
                Console.WriteLine("               │    │ │    │ │    │ │    │ │    │ │    │               ");
                Console.WriteLine("               │    │ │    │ │    │ │    │ │    │ │    │               ");
                Console.WriteLine("               │    │ │    │ │    │ │    │ │    │ │    │               ");
                Console.WriteLine("               └────┘ └────┘ └────┘ └────┘ └────┘ └────┘               ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 7)
            {
                Console.WriteLine("           ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐            ");
                Console.WriteLine("           │    │ │    │ │    │ │    │ │    │ │    │ │    │            ");
                Console.WriteLine("           │    │ │    │ │    │ │    │ │    │ │    │ │    │            ");
                Console.WriteLine("           │    │ │    │ │    │ │    │ │    │ │    │ │    │            ");
                Console.WriteLine("           └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘            ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 8)
            {
                Console.WriteLine("        ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐        ");
                Console.WriteLine("        │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │        ");
                Console.WriteLine("        │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │        ");
                Console.WriteLine("        │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │        ");
                Console.WriteLine("        └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘        ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 9)
            {
                Console.WriteLine("    ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐     ");
                Console.WriteLine("    │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │     ");
                Console.WriteLine("    │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │     ");
                Console.WriteLine("    │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │     ");
                Console.WriteLine("    └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘     ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player2scrollsInHand == 10)
            {
                Console.WriteLine(" ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ");
                Console.WriteLine(" │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ ");
                Console.WriteLine(" │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ ");
                Console.WriteLine(" │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ ");
                Console.WriteLine(" └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ ");
                Console.WriteLine("                                                                       ");
            } */
            Console.WriteLine("                    ┌────┬────┬────┬────┬────┬────┐                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    ├────┼────┼────┼────┼────┼────│                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    ├────┼────┼────┼────┼────┼────│                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    └────┴────┴────┴────┴────┴────┘                    ");
            Console.WriteLine("                                                                       ");
            Console.WriteLine("                    ┌────┐                   ┌────┐                    ");
            Console.WriteLine("                    │    ├─────────┬─────────┤    │                    ");
            Console.WriteLine("                    │    │   " + player2tens + "     │   " + player1tens + "     │    │                    ");
            Console.WriteLine("                    │    │     " + player2ones + "   │     " + player1ones + "   │    │                    ");
            Console.WriteLine("                    │    ├─────────┴─────────┤    │                    ");
            Console.WriteLine("                    └────┘                   └────┘                    ");
            Console.WriteLine("                                                                       ");
            Console.WriteLine("                    ┌────┬────┬────┬────┬────┬────┐                    ");
            Console.WriteLine("                    │" + Field.player1frontLine1.nameAbb +
                              "│" + Field.player1frontLine2.nameAbb +
                              "│" + Field.player1frontLine3.nameAbb +
                              "│" + Field.player1frontLine4.nameAbb +
                              "│" + Field.player1frontLine5.nameAbb +
                              "│" + Field.player1frontLine6.nameAbb +
                              "│                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    ├──" +
                            "──┼──" +
                            "──┼──" +
                            "──┼──" +
                            "──┼──" +
                            "──┼──" +
                            "──│                    ");
            Console.WriteLine("                    │" + Field.player1forwardLine1.nameAbb +
                              "│" + Field.player1forwardLine2.nameAbb +
                              "│" + Field.player1forwardLine3.nameAbb +
                              "│" + Field.player1forwardLine4.nameAbb +
                              "│" + Field.player1forwardLine5.nameAbb +
                              "│" + Field.player1forwardLine6.nameAbb +
                              "│                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    ├──" +
                            "──┼──" +
                            "──┼──" +
                            "──┼──" +
                            "──┼──" +
                            "──┼──" +
                            "──│                    ");
            Console.WriteLine("                    │" + Field.player1rearLine1.nameAbb +
                              "│" + Field.player1rearLine2.nameAbb +
                              "│" + Field.player1rearLine3.nameAbb +
                              "│" + Field.player1rearLine4.nameAbb +
                              "│" + Field.player1rearLine5.nameAbb +
                              "│" + Field.player1rearLine6.nameAbb +
                              "│                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    │    │    │    │    │    │    │                    ");
            Console.WriteLine("                    └────┴────┴────┴────┴────┴────┘                    ");
            if (Field.player1scrollsInHand == 0)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                                                       ");
            }
            else if (Field.player1scrollsInHand == 1)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                                 ┌────┐                                ");
                Console.WriteLine("                                 │    │                                ");
                Console.WriteLine("                                 │    │                                ");
                Console.WriteLine("                                 │    │                                ");
                Console.WriteLine("                                 └────┘                                ");
            }
            else if (Field.player1scrollsInHand == 2)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                             ┌────┐ ┌────┐                             ");
                Console.WriteLine("                             │    │ │    │                             ");
                Console.WriteLine("                             │    │ │    │                             ");
                Console.WriteLine("                             │    │ │    │                             ");
                Console.WriteLine("                             └────┘ └────┘                             ");
            }
            else if (Field.player1scrollsInHand == 3)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                         ┌────┐ ┌────┐ ┌────┐                          ");
                Console.WriteLine("                         │    │ │    │ │    │                          ");
                Console.WriteLine("                         │    │ │    │ │    │                          ");
                Console.WriteLine("                         │    │ │    │ │    │                          ");
                Console.WriteLine("                         └────┘ └────┘ └────┘                          ");
            }
            else if (Field.player1scrollsInHand == 4)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                      ┌────┐ ┌────┐ ┌────┐ ┌────┐                      ");
                Console.WriteLine("                      │    │ │    │ │    │ │    │                      ");
                Console.WriteLine("                      │    │ │    │ │    │ │    │                      ");
                Console.WriteLine("                      │    │ │    │ │    │ │    │                      ");
                Console.WriteLine("                      └────┘ └────┘ └────┘ └────┘                      ");
            }
            else if (Field.player1scrollsInHand == 5)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("                   ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐                  ");
                Console.WriteLine("                   │    │ │    │ │    │ │    │ │    │                  ");
                Console.WriteLine("                   │    │ │    │ │    │ │    │ │    │                  ");
                Console.WriteLine("                   │    │ │    │ │    │ │    │ │    │                  ");
                Console.WriteLine("                   └────┘ └────┘ └────┘ └────┘ └────┘                  ");
            }
            else if (Field.player1scrollsInHand == 6)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("               ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐               ");
                Console.WriteLine("               │    │ │    │ │    │ │    │ │    │ │    │               ");
                Console.WriteLine("               │    │ │    │ │    │ │    │ │    │ │    │               ");
                Console.WriteLine("               │    │ │    │ │    │ │    │ │    │ │    │               ");
                Console.WriteLine("               └────┘ └────┘ └────┘ └────┘ └────┘ └────┘               ");
            }
            else if (Field.player1scrollsInHand == 7)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("           ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐            ");
                Console.WriteLine("           │    │ │    │ │    │ │    │ │    │ │    │ │    │            ");
                Console.WriteLine("           │    │ │    │ │    │ │    │ │    │ │    │ │    │            ");
                Console.WriteLine("           │    │ │    │ │    │ │    │ │    │ │    │ │    │            ");
                Console.WriteLine("           └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘            ");
            }
            else if (Field.player1scrollsInHand == 8)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("        ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐        ");
                Console.WriteLine("        │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │        ");
                Console.WriteLine("        │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │        ");
                Console.WriteLine("        │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │        ");
                Console.WriteLine("        └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘        ");
            }
            else if (Field.player1scrollsInHand == 9)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine("    ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐     ");
                Console.WriteLine("    │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │     ");
                Console.WriteLine("    │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │     ");
                Console.WriteLine("    │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │     ");
                Console.WriteLine("    └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘     ");
            }
            else if (Field.player1scrollsInHand == 10)
            {
                Console.WriteLine("                                                                       ");
                Console.WriteLine(" ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ");
                Console.WriteLine(" │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ ");
                Console.WriteLine(" │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ ");
                Console.WriteLine(" │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ │    │ ");
                Console.WriteLine(" └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ ");
            }
        }
    }
}