using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using SFarmer = StardewValley.Farmer;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using StardewValley.Locations;

namespace WorkingPets
{
	public class PetInvetoryItem
	{
		public int ID;
		public string Name;
		public int stack;
		public PetInvetoryItem(int id, string name, int count)
		{
			this.ID = id;
			this.Name = name;
			this.stack = count;
		}
		public void AddToStack(int count)
		{
			this.stack += count;
		}
	}
	public class WorkingPets : Mod
	{
		private Pet workingPet;
		private static SFarmer thisFarmer;
		public Pet WorkingPet { get => workingPet; set => workingPet = value; }
		public int hayAmount;
		public FarmAnimal animal = new FarmAnimal();
		private ModConfig Config;
		private bool petToday = false;
		private bool petYesterday = false;
		private bool notifiedFarmer = false;

		private bool huntVermin;
		private bool digArtifacts;
		private Dictionary<int, PetInvetoryItem> petInventory = new Dictionary<int, PetInvetoryItem>();

		GameLocation[] petLocations = new GameLocation[2];

		ModData savedData; 

		public override void Entry(IModHelper helper)
		{
			this.Configure();
			InputEvents.ButtonReleased += this.InputEvents_ButtonReleased;
			TimeEvents.AfterDayStarted += new EventHandler(this.TimeEvents_AfterDayStarted);
			SaveEvents.BeforeSave += new EventHandler(this.SaveEvents_BeforeSave);
			SaveEvents.AfterLoad += new EventHandler(this.SaveEvents_AfterLoad);
		}

		private void Configure()
		{
			this.Config = this.Helper.ReadConfig<ModConfig>();
			this.huntVermin = this.Config.HuntVermin;
			this.digArtifacts = this.Config.DigArtifacts;
		}

		private void SaveEvents_AfterLoad(object sender, EventArgs e)
		{
			savedData = this.Helper.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
			this.petYesterday = savedData.PetYesterday;
			this.petInventory = savedData.PetInventory ?? new Dictionary<int, PetInvetoryItem>();
			petLocations[0] = Game1.getLocationFromName("FarmHouse");
			petLocations[1] = Game1.getLocationFromName("Farm");
			if (workingPet == null)
			{
				List<NPC> farmCharacters = new List<NPC>();
				farmCharacters.AddRange(petLocations[0].getCharacters());
				farmCharacters.AddRange(petLocations[1].getCharacters());
				foreach (NPC character in farmCharacters)
				{
					if (character is Dog)
					{
						workingPet = (Dog)character;
					}
				}
			}
		}

		private void SaveEvents_BeforeSave(object sender, EventArgs e)
		{
			this.petYesterday = this.petToday;
			SaveData();
		}

		private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
		{
			if (Context.IsWorldReady)
			{
				PetWork(petYesterday);
				petToday = false;
				notifiedFarmer = false;
			}
		}

		private void InputEvents_ButtonReleased(object sender, EventArgsInput e)
		{
			//this.Monitor.Log($"{Game1.player.name} pressed {e.Button}.");
			if (Context.IsWorldReady && WorkingPet != null) // save is loaded
			{
				if(workingPet.getTileLocation() == e.Cursor.GrabTile)
				{
					this.Monitor.Log($"{WorkingPet.name} was pet. ");
					petToday = true;
					if(petInventory.Count > 0)
					{
						this.Monitor.Log($"{WorkingPet.name}'s inventory is not empty.");
					}
				}
				bool wasPet = this.Helper.Reflection.GetField<bool>(WorkingPet, "wasPetToday").GetValue();
				if (wasPet && !petToday)
				{
					//this.Monitor.Log($"{WorkingPet.name} was pet. ");
					petToday = true;
				}
				//if(wasPet && petYesterday && !notifiedFarmer)
				//{
				//	this.Monitor.Log($"Magic number of vermin were scared off by {WorkingPet.name}.");
				//	notifiedFarmer = true;
				//}
				//
				//We h
				if(wasPet && petInventory.Count > 0)
				{

				}
			}
		}

		public void PetWork(bool worked)
		{
			//if (huntVermin)
			//	HuntVermin(worked);
			if (digArtifacts && worked)
				DigArtifacts();
		}

		//public void HuntVermin(bool worked)
		//{
		//	Farm thisFarm = Game1.getFarm();
		//	int currentHayCount = thisFarm.piecesOfHay;
		//	double hayPercent = Utility.numSilos() * (0.1);
		//	if (hayPercent > 1)
		//		hayPercent = 1;
		//	int verminCount = (int)Math.Ceiling(currentHayCount * hayPercent * Math.Abs(Game1.dailyLuck));
		//	this.Monitor.Log($"Vermin Count: {verminCount}.");

		//	if (worked)
		//	{
		//		Game1.drawObjectDialogue($"{verminCount} vermin were scared away from your silos by {workingPet.name}.");
		//	}
		//	else
		//	{
		//		thisFarm.piecesOfHay = thisFarm.piecesOfHay - Math.Abs(verminCount);
		//		Game1.drawObjectDialogue($"{verminCount} pieces of hay were eaten by vermin. {workingPet.name} isn't feeling loved enough to work.");
		//	}
		//}

		public void DigArtifacts()
		{
			int numberOfArtifacts = 2;

			List<GameLocation> locations = Game1.locations.Where(lc => lc.isOutdoors == true).ToList();
			Random random = new Random(2000 + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);

			GameLocation farm = Game1.getLocationFromName("Farm");
			int x = 50;
			int y = 45;
			for (int i = 0; i < numberOfArtifacts; i++)
			{
				DigDirt(x, y, Game1.player, farm);
			}
		}

		public void DigDirt(int xLocation, int yLocation, SFarmer who, GameLocation here)
		{
			Random random = new Random(xLocation * 2000 + yLocation + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
			int objectIndex = -1;
			foreach (KeyValuePair<int, string> keyValuePair in Game1.objectInformation)
			{
				string[] strArray1 = keyValuePair.Value.Split('/');
				if (strArray1[3].Contains("Arch"))
				{
					string[] strArray2 = strArray1[6].Split(' ');
					int index = 0;
					while (index < strArray2.Length)
					{
						if (strArray2[index].Equals(here.name) && random.NextDouble() < Convert.ToDouble(strArray2[index + 1], (IFormatProvider)CultureInfo.InvariantCulture))
						{
							objectIndex = keyValuePair.Key;
							break;
						}
						index += 2;
					}
				}
				if (objectIndex != -1)
					break;
			}
			if (random.NextDouble() < 0.2 && !(here is Farm))
				objectIndex = 102;
			if (objectIndex == 102 && who.archaeologyFound.ContainsKey(102) && who.archaeologyFound[102][0] >= 21)
				objectIndex = 770;
			if (objectIndex != -1)
			{
				AddToPetInventory(new StardewValley.Object(objectIndex, 1), 1);
				//who.gainExperience(5, 25);
			}
			else if (Game1.currentSeason.Equals("winter") && random.NextDouble() < 0.5 && !(here is Desert))
			{
				if (random.NextDouble() < 0.4)
				{
					AddToPetInventory(new StardewValley.Object(416, 1), 1);
				}
				else
				{
					AddToPetInventory(new StardewValley.Object(412, 1), 1);
				}
			}
			else
			{
				Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\Locations");
				if (!dictionary.ContainsKey(here.name))
					return;
				string[] strArray = dictionary[here.name].Split('/')[8].Split(' ');
				if (strArray.Length == 0 || strArray[0].Equals("-1"))
					return;
				int index1 = 0;
				while (index1 < strArray.Length)
				{
					if (random.NextDouble() <= Convert.ToDouble(strArray[index1 + 1]))
					{
						int index2 = Convert.ToInt32(strArray[index1]);
						if (Game1.objectInformation.ContainsKey(index2))
						{
							if (Game1.objectInformation[index2].Split('/')[3].Contains("Arch") || index2 == 102)
							{
								if (index2 == 102 && who.archaeologyFound.ContainsKey(102) && who.archaeologyFound[102][0] >= 21)
									index2 = 770;
								AddToPetInventory(new StardewValley.Object(index2, 1), 1);
								break;
							}
						}
						AddToPetInventory(new StardewValley.Object(412, 1), random.Next(1, 4));
						break;
					}
					index1 += 2;
				}
			}
		}

		private void AddToPetInventory(StardewValley.Object obj, int count = 1)
		{
			if(petInventory.ContainsKey(obj.parentSheetIndex))
			{

				petInventory[obj.parentSheetIndex].AddToStack(count);
			}
			else
			{
				petInventory.Add(obj.parentSheetIndex, new PetInvetoryItem(obj.parentSheetIndex, obj.name, count));
			}
		}

		public void SaveData()
		{
			savedData.PetYesterday = this.petYesterday;
			savedData.PetInventory = this.petInventory;
			this.Helper.WriteJsonFile($"data/{Constants.SaveFolderName}.json", savedData);
		}

		
	}
}