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

		// Which player's pile Make fills. Without it both decks landed in player 1's.
		private short owner;

		private string database = "Data Source=|DataDirectory|\\BoosterPacks.sdf";
		/* + 
		 "Persist Security Info=false;" +
		 "Initial Catalog=Aztec;" +
		 "Integrated Security=SSPI;" + 
		 "Application Name=Scrolls"*/
		private SqlCeConnection connection;

		public Deck(short owner, int[,] deck, short capacity) {
			this.owner = owner;
			this.deck = deck;
			this.capacity = capacity;

			this.connection = new SqlCeConnection(this.database);
			try {
				this.connection.Open();
			} catch (Exception e) {
				MessageLog.Add("Database connection failed: " + e.Message);
			}
		}

		~Deck() {
			/*
			 * Runs on the finaliser thread, potentially after the screen is gone, so
			 * it reports nothing. Dispose is the path that matters.
			 */
			try {
				this.connection.Close();
			} catch (Exception) { }
		}

		void IDisposable.Dispose() {
			try {
				this.connection.Close();
			} catch (Exception e) {
				MessageLog.Add("Database connection could not close: " + e.Message);
			}
		}

		/**
		 * Make
		 * Builds this deck's pile from the card manifest.
		 *
		 * The table is read once into a lookup, then the {cardId, quantity} pairs
		 * decide what actually goes into the pile. Previously the manifest and the
		 * capacity were both ignored and every pile was just the whole table.
		 */
		public void Make() {
			Hashtable byId = new Hashtable();
			SqlCeDataReader myReader = null;
			SqlCeCommand myCommand = null;

			try {
				myCommand = new SqlCeCommand("SELECT * FROM Aztec", this.connection);
				myReader = myCommand.ExecuteReader();
				while (myReader.Read()) {
					Scroll row = ReadScroll(myReader);
					byId[row.id] = row;
				}
			} catch (Exception e) {
				MessageLog.Add("Database read failed: " + e.Message);
				return;
			} finally {
				if (myReader != null)
					myReader.Close();
			}

			ArrayList pile = Field.scrollLists[this.owner, 0];
			pile.Clear();

			for (short entry = 0; entry < this.deck.GetLength(0); entry++) {
				int id = this.deck[entry, 0];
				int quantity = this.deck[entry, 1];

				Scroll template = (Scroll) byId[id];
				if (template == null) {
					MessageLog.Add("Deck lists card " + id + ", which is not in the database.");
					continue;
				}

				for (int copy = 0; copy < quantity && pile.Count < this.capacity; copy++)
					pile.Add(template.Copy());
			}

			Shuffle(pile);

			Field.scrollsIn[this.owner, 0] = (short) pile.Count;
			Field.scrollsIn[this.owner, 1] = 0;
			Field.scrollsIn[this.owner, 2] = 0;
		}

		/**
		 * Shuffle
		 * Fisher-Yates, in place.
		 *
		 * The pile is built by walking the manifest, so without this the opening
		 * hand is simply the last entry repeated.
		 */
		private static void Shuffle(ArrayList pile) {
			for (int i = pile.Count - 1; i > 0; i--) {
				int j = random.Next(i + 1);
				object swap = pile[i];
				pile[i] = pile[j];
				pile[j] = swap;
			}
		}

		// One generator for the process: separate Random instances seeded from the
		// clock in quick succession produce identical sequences.
		private static Random random = new Random();

		/**
		 * ReadScroll
		 * Builds one Scroll from the current row of a reader.
		 */
		private static Scroll ReadScroll(SqlCeDataReader reader) {
			return new Scroll(Convert.ToInt32(reader["id"]),
							  Convert.ToInt16(reader["line"]),
							  Text(reader["nameAbb"]),
							  Text(reader["typeAbb"]),
							  Text(reader["name"]),
							  Text(reader["types"]).Split(','),
							  Text(reader["attacks"]).Split(','),
							  Convert.ToInt16(reader["endurance"]),
							  Convert.ToInt16(reader["armor"]),
							  Convert.ToInt16(reader["accuracy"]),
							  Convert.ToInt16(reader["intelligence"]),
							  Text(reader["resistence"]),
							  Text(reader["weakness"]),
							  Text(reader["effect"]));
		}

		/**
		 * Text
		 * Reads a column that may be NULL. The effect column is NULL on every row in
		 * the shipped database, so calling ToString on it directly is not safe.
		 */
		private static string Text(object value) {
			return (value == null || value == DBNull.Value) ? "" : value.ToString();
		}

		public void CreatePack(string table) {
			SqlCeCommand insert = new SqlCeCommand(
				"CREATE TABLE " + table + "", this.connection);
		}
	}

    /**
     * Alternative names: Bind, Cell, Unit (2012/01/28 02:29)
     */
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
			// The renderer pads to the slot width, so these no longer carry layout
			this.nameAbb = "";
			this.typeAbb = "";

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
			this.effect = effect;
			this.stance = "";
		}

		/**
		 * Copy
		 * A duplicate of this scroll.
		 *
		 * A deck holds several copies of the same card, and each copy takes damage
		 * independently, so they cannot share one instance.
		 */
		public Scroll Copy() {
			Scroll clone = new Scroll(this.id, this.line, this.nameAbb, this.typeAbb,
									  this.name, this.types, this.attacks,
									  this.endurance, this.armor, this.accuracy,
									  this.intelligence, this.resistence,
									  this.weakness, this.effect);
			clone.creator = this.creator;
			return clone;
		}

		/**
		 * FieldLine
		 * Translates the database's line numbering into a Field.playerLines row.
		 *
		 * The database stores 1 Front, 2 Back, 3 Either, 4 Equipment, while
		 * Field.playerLines is indexed 0 Front, 1 Forward, 2 Equipment. Nothing
		 * converted between the two before, so a card's stored line was never a
		 * usable row index. Either resolves to the front row.
		 */
		public short FieldLine() {
			switch (this.line) {
				case 1: return 0;
				case 2: return 1;
				case 3: return 0;
				case 4: return 2;
				default: return 0;
			}
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