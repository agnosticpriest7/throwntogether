using NUnit.Framework;
namespace ThrownTogether.Tests
{
    public sealed class ExpansionAccountTests
    {
        sealed class Memory:ISettingsStorage{public string json="";public bool fail;public string Read()=>json;public void Write(string s){if(fail)throw new System.IO.IOException();json=s;}}
        [Test] public void NewBaysRequireOwnershipAndExpansionSurvivesReload()
        {
            var memory=new Memory();var account=new RestaurantAccount(memory);int day=account.StartDay();account.Settle(day,2000,0);
            var records=new[]{new FurniturePlacement{kitchen=0,id="base:3",slot=27}};
            Assert.IsFalse(account.SetFurniture(0,records));Assert.IsTrue(account.Buy(RestaurantExpansion.KitchenId,500));
            Assert.IsTrue(account.SetFurniture(0,records));Assert.IsTrue(account.Buy(RestaurantExpansion.DiningId,600));
            var restored=new RestaurantAccount(memory);Assert.IsTrue(restored.Owns(RestaurantExpansion.KitchenId));Assert.IsTrue(restored.Owns(RestaurantExpansion.DiningId));
            Assert.AreEqual(900,restored.Data.cash);Assert.AreEqual(27,restored.Data.furniture[0].slot);
            Assert.IsFalse(restored.Buy(RestaurantExpansion.KitchenId,500));Assert.AreEqual(900,restored.Data.cash);
            Assert.IsTrue(restored.ResetCareer());Assert.IsFalse(restored.Owns(RestaurantExpansion.KitchenId));Assert.IsEmpty(restored.Data.furniture);
        }
        [Test] public void FailedPurchaseDoesNotUnlockSpaceOrSpendMoney()
        {
            var memory=new Memory();var account=new RestaurantAccount(memory);int day=account.StartDay();account.Settle(day,1000,0);
            memory.fail=true;Assert.IsFalse(account.Buy(RestaurantExpansion.KitchenId,500));Assert.IsFalse(account.Owns(RestaurantExpansion.KitchenId));Assert.AreEqual(1000,account.Data.cash);
            memory.fail=false;account.StartDay();Assert.IsFalse(account.Buy(RestaurantExpansion.DiningId,600));
        }
    }
}
