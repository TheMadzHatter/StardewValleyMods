using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WorkingPets.UnitTests
{
    [TestClass]
    public class BasicTest 
    {
        [TestMethod]
        [Ignore]
        public void TestWorkingPets()
        {
            WorkingPets wp = new WorkingPets();
            Assert.AreEqual("Hi mom", wp.testFunction("Hi", "mom"));
        }

        [TestMethod]
        public void TestPetInvetoryItem()
        {
            PetInventoryItem item = new PetInventoryItem(1, "testItem", 1);
            item.AddToStack(5);
            Assert.AreEqual(6, item.stack);
        }
        
    }
}
