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

namespace Objects
{
    public struct Field
    {
        /*
         * Player 1
         */
        public static int player1scrollsInDeck;
        public static int player1scrollsInHand;
        public static int player1scrollsInVoid;
        public static Scroll[] player1deck;
        public static Scroll[] player1hand;
        public static Scroll[] player1void;
        public static Scroll player1battlefield;

        public static Scroll player1frontLine1;
        public static Scroll player1frontLine2;
        public static Scroll player1frontLine3;
        public static Scroll player1frontLine4;
        public static Scroll player1frontLine5;
        public static Scroll player1frontLine6;

        public static Scroll player1forwardLine1;
        public static Scroll player1forwardLine2;
        public static Scroll player1forwardLine3;
        public static Scroll player1forwardLine4;
        public static Scroll player1forwardLine5;
        public static Scroll player1forwardLine6;

        public static Scroll player1rearLine1;
        public static Scroll player1rearLine2;
        public static Scroll player1rearLine3;
        public static Scroll player1rearLine4;
        public static Scroll player1rearLine5;
        public static Scroll player1rearLine6;

        /*
         * Player 2
         */

        public static int player2scrollsInDeck;
        public static int player2scrollsInHand;
        public static int player2scrollsInVoid;
        public static Scroll[] player2deck;
        public static Scroll[] player2hand;
        public static Scroll[] player2void;
        public static Scroll player2battlefield;

        public static Scroll player2frontLine1;
        public static Scroll player2frontLine2;
        public static Scroll player2frontLine3;
        public static Scroll player2frontLine4;
        public static Scroll player2frontLine5;
        public static Scroll player2frontLine6;

        public static Scroll player2forwardLine1;
        public static Scroll player2forwardLine2;
        public static Scroll player2forwardLine3;
        public static Scroll player2forwardLine4;
        public static Scroll player2forwardLine5;
        public static Scroll player2forwardLine6;

        public static Scroll player2rearLine1;
        public static Scroll player2rearLine2;
        public static Scroll player2rearLine3;
        public static Scroll player2rearLine4;
        public static Scroll player2rearLine5;
        public static Scroll player2rearLine6;
    }

    public class Deck : Object
    {
        public string name;
        public int scrollsInDeck;
        public string[] scrolls;
    }

    public class Scroll : Object
    {
        // 1 for Front Line, 2 for Back Line, 3 for Either, and 4 for Equipment
        public int line;
        public string nameAbb;
        public string typeAbb;

        public string name;
        public string stance;
        public string[] types;
        public string[] attacks;
        public int endurance;
        public int armor;
        public int accuracy;
        public int intelligence;
        public string resistence;
        public string weakness;

        // Only for Equipment scrolls
        public string effect;

        // Hidden
        public string creator;
        public int date;

        /*
         * Constructors
         */

        public Scroll()
        {
            this.line = -1;
            this.nameAbb = "    ";
            this.name = "";
            this.stance = "";
            string[] nulltypes = { };
            this.types = nulltypes;
            string[] nullAttacks = { };
            this.attacks = nullAttacks;
            this.endurance = 0;
            this.armor = 0;
            this.accuracy = 0;
            this.intelligence = 0;
            this.resistence = "";
            this.weakness = "";
        }

        public Scroll(int id ,
						int line,
                        string nameAbb,
                        string typeAbb,
                        string name,
                        string[] types,
                        string[] attacks,
                        int endurance,
                        int armor,
                        int accuracy,
                        int intelligence,
                        string resistence,
                        string weakness,
						string effect)
        {
            this.line = line;
            this.nameAbb = nameAbb;
			this.typeAbb = typeAbb;

            this.name = name;
            this.types = types;
            this.attacks = attacks;
            this.endurance = endurance;
            this.armor = armor;
            this.accuracy = accuracy;
            this.intelligence = intelligence;
            this.resistence = resistence;
            this.weakness = weakness;
        }

        public Scroll(int line, string nameAbb, string name, string effect)
        {
            this.line = line;
            this.nameAbb = nameAbb;
            this.name = name;
            this.effect = effect;
        }

        /* 
         * Methods
         */
        public String getNameAbb()
        {
            return this.nameAbb;
        }

        public override String ToString()
        {
            if (line < 3)
            {
                String returnString = "Entity";
                returnString += "\nName         : " + this.name;
                //returnString += "\nStance       : " + this.stance;
                returnString += "\nTypes        : ";
                try
                {
                    foreach (string type in types)
                    {
						returnString += "\"" + type + "\" ";
					}
				} catch (NullReferenceException) { }
				returnString += "\nAttacks      : ";
                try
                {
                    foreach (string attack in attacks)
                    {
                        returnString += "\"" + attack + "\" ";
                    }
                }
                catch (NullReferenceException) { }
                returnString += "\nEndurance    : " + this.endurance;
                returnString += "\nArmor        : " + this.armor;
                returnString += "\nAccuracy     : " + this.accuracy;
                returnString += "\nIntelligence : " + this.intelligence;
                returnString += "\nResistence   : " + this.resistence;
                returnString += "\nWeakness     : " + this.weakness;
                return returnString;
            }
            else
            {
                String returnString = "Equipment";
                returnString += "\nName: " + this.name;
                returnString += "\nEffect: " + this.effect;
                return returnString;
            }
        }
    }
}