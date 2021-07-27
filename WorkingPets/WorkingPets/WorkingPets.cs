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
		//private bool petToday = false;
		private bool petYesterday = false;
		private int petStreak;
		private const int MAXSTACK = 999;

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
            try
            {
				savedData = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json");
			}
            catch (Exception ex)
            {
				savedData = new ModData();
			}
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
			int lastDayPet = workingPet.lastPetDay.Values.First();
			bool petted = Game1.Date.TotalDays - lastDayPet == 1; // Current day minus last day pet should be exactly one
			this.petYesterday = petted;
            if (petted)
            {
				this.petStreak += 1;
            }
			SaveData();
		}

		private void DayStarted(object sender, DayStartedEventArgs e)
		{
			if (Context.IsWorldReady)
			{
				GetPetInventory();
				PetWork(petYesterday);
				//petToday = false;
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
				workableLocations[index].digUpArtifactSpot(0, 0, Game1.player);
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

		/// <summary>
		/// This function is based off GameLocation.digUpArtifactSpot(int xLocation, int yLocation, Farmer who)
		/// </summary>
		/// <param name="who"></param>
		/// <param name="here"></param>
		public void DigDirt(SFarmer who, GameLocation here)
		{
			int xLocation = Game1.random.Next(here.map.DisplayWidth / 64);
			int yLocation = Game1.random.Next(here.map.DisplayHeight / 64);
			Random random = new Random(xLocation * 2000 + yLocation + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
			int toDigUp = -1;
			string[] split = null;
			foreach (KeyValuePair<int, string> v in Game1.objectInformation)
			{
				split = v.Value.Split('/');
				if (split[3].Contains("Arch"))
				{
					string[] archSplit = split[6].Split(' ');
					for (int j = 0; j < archSplit.Length; j += 2)
					{
						if (archSplit[j].Equals(here.name) && random.NextDouble() < 2 * Convert.ToDouble(archSplit[j + 1], CultureInfo.InvariantCulture))
						{
							toDigUp = v.Key;
							break;
						}
						j += 2;
					}
				}
				if (toDigUp != -1)
					break;
			}
			if (random.NextDouble() < 0.2 && !(here is Farm))
				toDigUp = 102;
			if (toDigUp == 102 && (int)Game1.netWorldState.Value.LostBooksFound >= 21)
				toDigUp = 770;
			if (toDigUp != -1)
			{
				AddToPetInventory(new StardewValley.Object(toDigUp, 1), 1);
				who.gainExperience(5, 25);
				return;
			}
			if (Game1.GetSeasonForLocation(here).Equals("winter") && random.NextDouble() < 0.5 && !(here is Desert))
			{
				if (random.NextDouble() < 0.4)
				{
					AddToPetInventory(new StardewValley.Object(416, 1), 1);
				}
				else
				{
					AddToPetInventory(new StardewValley.Object(412, 1), 1);
				}
				return;
			}
			if (Game1.random.NextDouble() <= 0.25 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
				AddToPetInventory(new StardewValley.Object(890, 1), random.Next(2 - 6));
			if (Game1.GetSeasonForLocation(here).Equals("spring") && random.NextDouble() < 0.0625 && !(this is Desert) && !(this is Beach))
			{
				AddToPetInventory(new StardewValley.Object(273, 1), random.Next(2, 6));
				return;
			}
			if (Game1.random.NextDouble() <= 0.2 && (Game1.MasterPlayer.mailReceived.Contains("guntherBones") 
				|| (Game1.player.team.specialOrders.Where((SpecialOrder x) 
				=> (string)x.questKey == "Gunther") != null && Game1.player.team.specialOrders.Where((SpecialOrder x) => (string)x.questKey == "Gunther").Count() > 0)))
			{
				AddToPetInventory(new StardewValley.Object(881, 1), random.Next(2, 6));
			}

			Dictionary<string, string> locationData = Game1.content.Load<Dictionary<string, string>>("Data\\Locations");
			if (!locationData.ContainsKey(here.name))
				return;
			string[] rawData = locationData[here.name].Split('/')[8].Split(' ');
			if (rawData.Length == 0 || rawData[0].Equals("-1"))
				return;
			for (int i = 0; i < rawData.Length; i += 2)
			{
				if (random.NextDouble() <= Convert.ToDouble(rawData[i + 1]))
				{
					if (!(random.NextDouble() <= Convert.ToDouble(rawData[i + 1])))
					{
						continue;
					}
					toDigUp = Convert.ToInt32(rawData[i]);
					if (Game1.objectInformation.ContainsKey(toDigUp) && (Game1.objectInformation[toDigUp].Split('/')[3].Contains("Arch") || toDigUp == 102))
					{
						if (toDigUp == 102 && (int)Game1.netWorldState.Value.LostBooksFound >= 21)
						{
							toDigUp = 770;
						}
						AddToPetInventory(new StardewValley.Object(toDigUp, 1), 1);
						break;
					}
					if (toDigUp == 330 && here.HasUnlockedAreaSecretNotes(who) && Game1.random.NextDouble() < 0.11)
					{
						StardewValley.Object o = here.tryToCreateUnseenSecretNote(who);
						if (o != null)
						{
							AddToPetInventory(o, 1);
							break;
						}
					}
					else if (toDigUp == 330 && Game1.stats.DaysPlayed > 28 && Game1.random.NextDouble() < 0.1)
					{
						AddToPetInventory(new StardewValley.Object(688 + Game1.random.Next(3), 1),1);
					}
					AddToPetInventory(new StardewValley.Object(toDigUp, 1), random.Next(1, 4));

					break;
				}
			}
			
		}

		private void AddToPetInventory(StardewValley.Object obj, int count = 1)
		{
			if(petInventory.ContainsKey(obj.parentSheetIndex))
			{
				if(petInventory[obj.parentSheetIndex].stack + count <= MAXSTACK)
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