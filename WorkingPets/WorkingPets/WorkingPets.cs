using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using SFarmer = StardewValley.Farmer;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StardewValley.Locations;
using Netcode;

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
		public void SetStack(int count)
		{
			this.stack = count;
		}
	}
	public class WorkingPets : Mod
	{
		private Pet workingPet;
		public Pet WorkingPet { get => workingPet; set => workingPet = value; }
		private ModConfig Config;
		private bool petToday = false;
		private bool petYesterday = false;
		private int petStreak;
		private int maxStack = 999;

		private bool digArtifacts;
		private Dictionary<int, PetInvetoryItem> petInventory = new Dictionary<int, PetInvetoryItem>();
		List<GameLocation> workableLocations;
		ModData savedData; 

		public override void Entry(IModHelper helper)
		{
			this.Configure();
			//InputEvents.ButtonReleased += this.InputEvents_ButtonReleased;
			helper.Events.GameLoop.DayStarted += this.DayStarted;
			helper.Events.GameLoop.Saving += this.BeforeSave;
			helper.Events.GameLoop.SaveLoaded += this.AfterSaveLoaded;
		}

		private void Configure()
		{
			this.Config = this.Helper.ReadConfig<ModConfig>();
			this.digArtifacts = this.Config.DigArtifacts;
		}

		private void AfterSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			savedData = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
			this.petYesterday = savedData.PetYesterday;
			this.petStreak = savedData.petStreak;
			this.petInventory = savedData.PetInventory ?? new Dictionary<int, PetInvetoryItem>();
			workableLocations = GetWorkableLocations();
			if (workingPet == null)
			{
				List<NPC> farmCharacters = new List<NPC>();
				farmCharacters.AddRange(Game1.getLocationFromName("FarmHouse").getCharacters());
				farmCharacters.AddRange(Game1.getLocationFromName("Farm").getCharacters());
				foreach (NPC character in farmCharacters)
				{
					if (character is Dog)
					{
						workingPet = (Dog)character;
					}
				}
			}
		}

		private void BeforeSave(object sender, SavingEventArgs e)
		{
			// This needs to happen right after player goes to sleep at night. 
			this.petYesterday = this.Helper.Reflection.GetField<bool>(WorkingPet, "wasPetToday").GetValue();
			SaveData();
		}

		private void DayStarted(object sender, DayStartedEventArgs e)
		{
			if (Context.IsWorldReady)
			{
				GetPetInventory();
				PetWork(petYesterday);
				petToday = false;
				if (!petYesterday)
					petStreak = 0;
			}
		}

		public void GetPetInventory()
		{
			Game1.drawDialogue(workingPet, "I found these artifacts for you.");
			foreach (KeyValuePair<int, PetInvetoryItem> kvp in petInventory.Where(pi => pi.Value.stack > 0))
			{
				StardewValley.Object obj = new StardewValley.Object(kvp.Key, kvp.Value.stack);
				var item = Game1.player.addItemToInventory(obj);
				if (item == null) // full pet inventory added to farmer inventory
				{
					kvp.Value.SetStack(0);
				}
				else // only some of pet inventory added to the famer inventory
				{
					kvp.Value.SetStack(item.Stack);
				}
			}
		}

		//private void InputEvents_ButtonReleased(object sender, EventArgsInput e)
		//{
		//	if (Context.IsWorldReady && WorkingPet != null) // save is loaded
		//	{
		//		if(workingPet.getTileLocation() == e.Cursor.GrabTile)
		//		{
		//			this.Monitor.Log($"{WorkingPet.name} was pet. ");
		//			if (!petToday)
		//				petStreak++;
		//			petToday = true;
					
		//		}
		//	}
		//}

		public void PetWork(bool worked)
		{
			if (digArtifacts && worked)
				DigArtifacts();
		}

		public void DigArtifacts()
		{
			Random random = new Random(2000 + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
			int index = Game1.random.Next() % workableLocations.Count;
			int num = petStreak > 10 ? 10 : petStreak;
			for (int i = 0; i < num; i++)
			{
				DigDirt(Game1.player, workableLocations[index]);
			}
		}

		public List<GameLocation> GetWorkableLocations()
		{
			List<GameLocation> petLocations = new List<GameLocation>();
			petLocations.Add(Game1.getLocationFromName("Farm"));
			petLocations.Add(Game1.getLocationFromName("Town"));
			petLocations.Add(Game1.getLocationFromName("Beach"));
			petLocations.Add(Game1.getLocationFromName("Mountain"));
			petLocations.Add(Game1.getLocationFromName("Forest"));
			petLocations.Add(Game1.getLocationFromName("BusStop"));
			petLocations.Add(Game1.getLocationFromName("Woods"));
			petLocations.Add(Game1.getLocationFromName("Backwoods"));
			if (Game1.MasterPlayer.mailReceived.Contains("ccBoilerRoom"))
			{
				petLocations.Add(Game1.getLocationFromName("Desert"));
			}
			if (Game1.MasterPlayer.mailReceived.Contains("landslideDone"))
			{
				petLocations.Add(Game1.getLocationFromName("Railroad"));
			}
			return petLocations;
		}

		public void DigDirt(SFarmer who, GameLocation here)
		{
			int x = Game1.random.Next(here.map.DisplayWidth / 64);
			int y = Game1.random.Next(here.map.DisplayHeight / 64);
			Random random = new Random(x * 2000 + y + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
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
						if (strArray2[index].Equals((string)((NetFieldBase<string, NetString>)here.name)) && random.NextDouble() < Convert.ToDouble(strArray2[index + 1], (IFormatProvider)CultureInfo.InvariantCulture))
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
				if (!dictionary.ContainsKey((string)((NetFieldBase<string, NetString>)here.name)))
					return;
				string[] strArray = dictionary[(string)((NetFieldBase<string, NetString>)here.name)].Split('/')[8].Split(' ');
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
						AddToPetInventory(new StardewValley.Object(index2, 1), random.Next(1, 4));
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
				if(petInventory[obj.parentSheetIndex].stack + count <= maxStack)
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
			savedData.petStreak = this.petStreak;
			this.Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", savedData);
		}

		
	}
}