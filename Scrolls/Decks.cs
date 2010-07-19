// Default
using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

// Custom
using System.Data.SqlServerCe; // Compact Edition
//using System.Data.SqlClient; // Full Edition
using System.Collections;  // ArrayList

// Native Namespaces
using Scrolls;
using Decks;
using Commands;
using Objects;
using Board;

namespace Decks
{
	public class Aztec
	{
		public int[,] deck = { { 10000000, 1 }, { 10000001, 3 }, { 10000002, 2 }, { 10000003, 3 } };
		public static ArrayList scrolls = new ArrayList();
		public int capacity;

		public static void make()
		{
			string myConnectionString = "Data Source=|DataDirectory|\\BoosterPacks.sdf"/* + 
										 "Persist Security Info=false;" +
										 "Initial Catalog=Aztec;" +
										 "Integrated Security=SSPI;" +
										 "Application Name=Scrolls"*/
																	 ;
			SqlCeConnection myConnection = new SqlCeConnection(myConnectionString);
			SqlCeDataReader myReader = null;
			SqlCeCommand myCommand = null;
			Scroll row = null;
			string[] types;
			string[] attacks;
			try
			{
				myConnection.Open();
				myCommand = new SqlCeCommand("SELECT * FROM Aztec", myConnection);
				myReader = myCommand.ExecuteReader();
				while (myReader.Read())
				{
					types = myReader["types"].ToString().Split(',');
					attacks = myReader["attacks"].ToString().Split(',');
					row = new Scroll(Convert.ToInt32(myReader["id"]),
									 Convert.ToInt32(myReader["line"]),
									 myReader["nameAbb"].ToString(),
									 myReader["typeAbb"].ToString(),
									 myReader["name"].ToString(),
									 types,
									 attacks,
									 Convert.ToInt32(myReader["endurance"]),
									 Convert.ToInt32(myReader["armor"]),
									 Convert.ToInt32(myReader["accuracy"]),
									 Convert.ToInt32(myReader["intelligence"]),
									 myReader["resistence"].ToString(),
									 myReader["weakness"].ToString(),
									 myReader["effect"].ToString());
					Console.WriteLine(row);
					Aztec.scrolls.Add(row);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Database Connection Failed!");
				Console.WriteLine(e.ToString());
			}

			try
			{
				myConnection.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine("Database Connection Could Not Close!");
				Console.WriteLine(e.ToString());
			}
		}
	}

	public class Celtic
	{
		public Scroll[] deck;
		public int scrolls;

		public static void make()
		{

		}
	}
}