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
using System.Data.SqlServerCe; // Compact Edition
//using System.Data.SqlClient; // Full Edition
using System.Collections;	   // ArrayList

/**
 * Native Namespaces
 */
using Scrolls;
using Commands;
using Objects;
using Board;
using ArtificialIntelligence;

namespace Objects
{
	public struct Field {
		/**
		 * Arrays containing the field's scrolls
		 * 
		 * [0, x, y] - Player 1
		 * [1, x, y] - Player 2
		 * [x, 0, y] - Front Line
		 * [x, 1, y] - Forward Line
		 * [x, 2, y] - Equipment Line
		 * [x, y, z] - Individual scroll from left to right
		 */
		public static Scroll[,,] playerLines;

		/**
		 * Array containing the number of Scrolls in each players'
		 * deck, hand, and void.
		 * 
		 * [0, x] - Player 1
		 * [1, x] - Player 2
		 * [x, 0] - Deck
		 * [x, 1] - Hand
		 * [x, 2] - Void
		 */
		public static short[,] scrollsIn;

		/**
		 * scrollLists
		 * ArrayList acting as a Stack
		 * 
		 * [0, x] - Player 1
		 * [1, x] - Player 2
		 * [x, 0] - Deck
		 * [x, 1] - Hand
		 * [x, 2] - Void
		 */
		public static ArrayList[,] scrollLists;

		// Scroll array of size 2 containing both Battlefield cards
		public static Scroll[] battlefields;

		public Field(short deck1, short deck2) {
			Field.playerLines = new Scroll[2, 3, 6];
			// Create a new scroll for every cell
			for (short player = 0; player < 2; player++)
				for (short line = 0; line < 3; line++)
					for (short scroll = 0; scroll < 6; scroll++)
						Field.playerLines[player, line, scroll] = new Scroll();
			Field.scrollsIn = new short[2, 3];
			Field.scrollLists = new ArrayList[2,3];
			for (short player = 0; player < 2; player++)
				for (short scrolls = 0; scrolls < 3; scrolls++)
					Field.scrollLists[player, scrolls] = new ArrayList();
			Field.battlefields = new Scroll[2];

			/**
			 * Player 1
			 */
			Field.scrollsIn[0, 0] = deck1;
			Field.scrollsIn[0, 1] = 0;
			Field.scrollsIn[0, 2] = 0;
			Field.battlefields[0] = new Scroll();

			/**
			 * Player 2
			 */
			Field.scrollsIn[1, 0] = deck2;
			Field.scrollsIn[1, 1] = 0;
			Field.scrollsIn[1, 2] = 0;
			Field.battlefields[1] = new Scroll();
		}
	}

	public class Deck : IDisposable {
		public int[,] deck;
		//public static ArrayList scrolls = new ArrayList();
		public short capacity;

		private string database = "Data Source=|DataDirectory|\\BoosterPacks.sdf";
		/* + 
		 "Persist Security Info=false;" +
		 "Initial Catalog=Aztec;" +
		 "Integrated Security=SSPI;" +
		 "Application Name=Scrolls"*/
		private SqlCeConnection connection;

		public Deck(int[,] deck, short capacity) {
			this.deck = deck;
			this.capacity = capacity;

			this.connection = new SqlCeConnection(this.database);
			try {
				this.connection.Open();
			} catch (Exception e) {
				Console.WriteLine("Database Connection Failed!");
				Console.WriteLine(e.ToString());
			}
		}

		~Deck() {
			try {
				this.connection.Close();
			} catch (Exception e) {
				//Console.WriteLine("Database Connection Could Not Close!");
				//Console.WriteLine(e.ToString());
			}
		}

		void IDisposable.Dispose() {
			try {
				this.connection.Close();
			} catch (Exception e) { }
		}

		public void Make() {
			SqlCeDataReader myReader = null;
			SqlCeCommand myCommand = null;
			Scroll row = null;
			string[] types;
			string[] attacks;
			try {
				myCommand = new SqlCeCommand("SELECT * FROM Aztec", this.connection);
				myReader = myCommand.ExecuteReader();
				while (myReader.Read())
				{
					types = myReader["types"].ToString().Split(',');
					attacks = myReader["attacks"].ToString().Split(',');
					row = new Scroll(Convert.ToInt32(myReader["id"]),
									 Convert.ToInt16(myReader["line"]),
									 myReader["nameAbb"].ToString(),
									 myReader["typeAbb"].ToString(),
									 myReader["name"].ToString(),
									 types,
									 attacks,
									 Convert.ToInt16(myReader["endurance"]),
									 Convert.ToInt16(myReader["armor"]),
									 Convert.ToInt16(myReader["accuracy"]),
									 Convert.ToInt16(myReader["intelligence"]),
									 myReader["resistence"].ToString(),
									 myReader["weakness"].ToString(),
									 myReader["effect"].ToString());
					//Console.WriteLine(row);
					//Deck.scrolls.Add(row);
					Field.scrollLists[0, 0].Add(row);
				}
			} catch (Exception e) {
				Console.WriteLine("Database Reader Failed!");
				Console.WriteLine(e.ToString());
			}
		}

		public void CreatePack(string table) {
			SqlCeCommand insert = new SqlCeCommand(
				"CREATE TABLE " + table + "", this.connection);
		}
	}

	public class Scroll : Object {
		public int id;
		// 1 for Front Line, 2 for Back Line, 3 for Either, and 4 for Equipment
		public short line;
		public string nameAbb;
		public string typeAbb;

		public string name;
		public string stance;
		public string[] types;
		public string[] attacks;

		public short endurance;
		public short armor;
		public short accuracy;
		public short intelligence;

		public string resistence;
		public string weakness;

		// Only for Equipment scrolls
		public string effect;

		// Hidden
		public string creator;
		public short date;

		/**
		 * 
		 * Constructors
		 * 
		 */
		public Scroll() {
			this.id = 0;
			this.line = -1;
			this.nameAbb = "     ";
			this.typeAbb = "     ";

			this.name = "";
			this.stance = "";
			this.types = null;
			this.attacks = null;
			this.endurance = 0;
			this.armor = 0;
			this.accuracy = 0;
			this.intelligence = 0;
			this.resistence = "";
			this.weakness = "";
		}

		public Scroll(int id ,
						short line,
						string nameAbb,
						string typeAbb,
						string name,
						string[] types,
						string[] attacks,
						short endurance,
						short armor,
						short accuracy,
						short intelligence,
						string resistence,
						string weakness,
						string effect) {
			this.id = id;
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

		public Scroll(short line, string nameAbb, string name, string effect) {
			this.line = line;
			this.nameAbb = nameAbb;
			this.name = name;
			this.effect = effect;
		}

		/**
		 * 
		 * Methods
		 * 
		 */
		public override String ToString() {
			if (line < 3) {
				String returnString = "Entity";
				returnString += "\nName		 : " + this.name;
				//returnString += "\nStance	   : " + this.stance;
				returnString += "\nTypes		: ";
				try
				{
					foreach (string type in types)
					{
						returnString += "\"" + type + "\" ";
					}
				} catch (NullReferenceException) { }
				returnString += "\nAttacks	  : ";
				try
				{
					foreach (string attack in attacks)
					{
						returnString += "\"" + attack + "\" ";
					}
				}
				catch (NullReferenceException) { }
				returnString += "\nEndurance	: " + this.endurance;
				returnString += "\nArmor		: " + this.armor;
				returnString += "\nAccuracy	 : " + this.accuracy;
				returnString += "\nIntelligence : " + this.intelligence;
				returnString += "\nResistence   : " + this.resistence;
				returnString += "\nWeakness	 : " + this.weakness;
				return returnString;
			} else {
				String returnString = "Equipment";
				returnString += "\nName: " + this.name;
				returnString += "\nEffect: " + this.effect;
				return returnString;
			}
		}
	}
}