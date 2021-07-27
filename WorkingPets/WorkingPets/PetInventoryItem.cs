namespace WorkingPets
{
    public class PetInventoryItem
	{
		public int ID;
		public string Name;
		public int stack;
		public PetInventoryItem(int id, string name, int count)
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
}
