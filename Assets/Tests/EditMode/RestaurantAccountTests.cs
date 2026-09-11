using System;
using NUnit.Framework;
namespace ThrownTogether.Tests
{
    public sealed class RestaurantAccountTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public bool fail;public string Read()=>json;public void Write(string value){if(fail)throw new Exception("Storage unavailable");json=value;}}
        [Test] public void RepeatPurchasesResaleAndTrainingPersistAtomically()
        {
            var store=new Memory();var a=new RestaurantAccount(store);int day=a.StartDay();a.Settle(day,2000,0);
            Assert.That(a.BuyEquipment("grill",150),Is.True);Assert.That(a.BuyEquipment("grill",150),Is.True);
            Assert.That(a.Quantity("grill"),Is.EqualTo(2));var first=a.Data.equipment[0];
            store.fail=true;Assert.That(a.SellEquipment(first.instanceId,"grill",150),Is.False);Assert.That(a.Data.cash,Is.EqualTo(1700));store.fail=false;
            Assert.That(a.SellEquipment(first.instanceId,"grill",150),Is.True);Assert.That(a.Data.cash,Is.EqualTo(1812));Assert.That(a.Owns("grill"),Is.True);
            a=new RestaurantAccount(store);Assert.That(a.Quantity("grill"),Is.EqualTo(1));Assert.That(a.SellEquipment(first.instanceId,"grill",150),Is.False);
            a.Buy("server",150);Assert.That(a.Train("server"),Is.True);Assert.That(a.Train("server"),Is.True);Assert.That(a.Train("server"),Is.True);Assert.That(a.Train("server"),Is.False);
            a=new RestaurantAccount(store);Assert.That(a.StaffSpeed("server"),Is.EqualTo(1.3f).Within(.001));Assert.That(a.Train("busser"),Is.False);
            day=a.StartDay();Assert.That(a.BuyEquipment("extra-plate",20),Is.False);Assert.That(a.SellEquipment(a.Data.equipment[0].instanceId,"grill",150),Is.False);
        }
        [Test] public void LegacyEquipmentCanSellAndLastCopyRelocksOwnership()
        {
            var store=new Memory();var a=new RestaurantAccount(store);int day=a.StartDay();a.Settle(day,200,0);a.Buy("grill",150);
            a=new RestaurantAccount(store);Assert.That(a.Equipment("grill",150).Length,Is.EqualTo(1));Assert.That(a.SellEquipment("grill","grill",150),Is.True);Assert.That(a.Owns("grill"),Is.False);Assert.That(a.Data.cash,Is.EqualTo(162));
        }
        [Test] public void ArrangementFeeIsOncePerPaidBreakAtomicAndFreeBeforeDayOne()
        {
            var store=new Memory();var a=new RestaurantAccount(store);Assert.That(a.ArrangementFee,Is.Zero);var records=new[]{new FurniturePlacement{id="base:3",slot=4}};
            int day=a.StartDay();a.Settle(day,100,0);Assert.That(a.ArrangementFee,Is.EqualTo(100));
            store.fail=true;Assert.That(a.SetFurniture(0,records,true),Is.False);Assert.That(a.Data.cash,Is.EqualTo(100));Assert.That(a.ArrangementFee,Is.EqualTo(100));
            store.fail=false;Assert.That(a.SetFurniture(0,records,true),Is.True);Assert.That(a.Data.cash,Is.Zero);
            a=new RestaurantAccount(store);Assert.That(a.ArrangementFee,Is.Zero);a.SetFurniture(0,records,true);Assert.That(a.Data.cash,Is.Zero);
            day=a.StartDay();a.Settle(day,0,0);Assert.That(a.ArrangementFee,Is.EqualTo(100));
        }
        [Test] public void CareerResetClearsProgressAndFailedResetPreservesIt()
        {
            var storage=new Memory();var account=new RestaurantAccount(storage);int day=account.StartDay();account.Settle(day,200,0);account.Buy("grill",50);
            account.SetMenu(new[]{"fries"});account.SetFurniture(0,new[]{new FurniturePlacement{id="base:3",slot=2}});
            string before=storage.json;storage.fail=true;Assert.That(account.ResetCareer(),Is.False);Assert.That(storage.json,Is.EqualTo(before));Assert.That(account.Data.cash,Is.EqualTo(150));
            storage.fail=false;Assert.That(account.ResetCareer(),Is.True);var loaded=new RestaurantAccount(storage);
            Assert.That(loaded.Data.nextDay,Is.EqualTo(1));Assert.That(loaded.Data.cash,Is.Zero);Assert.That(loaded.Data.completedDays,Is.Zero);
            Assert.That(loaded.Data.purchases,Is.Empty);Assert.That(loaded.Data.selectedMenu,Is.Empty);Assert.That(loaded.Data.furniture,Is.Empty);
        }
        [Test] public void FurnitureIsAtomicPerKitchenAndCannotChangeDuringService()
        {
            var storage=new Memory();var account=new RestaurantAccount(storage);
            var placement=new FurniturePlacement{id="base:3",kitchen=0,slot=2,turns=1};
            Assert.That(account.SetFurniture(0,new[]{placement}),Is.True);
            var loaded=new RestaurantAccount(storage);Assert.That(loaded.Data.furniture[0].slot,Is.EqualTo(2));
            Assert.That(account.SetFurniture(0,new[]{placement,placement}),Is.False);
            storage.fail=true;Assert.That(account.SetFurniture(0,new FurniturePlacement[0]),Is.False);Assert.That(account.Data.furniture.Length,Is.EqualTo(1));storage.fail=false;
            int day=account.StartDay();Assert.That(account.SetFurniture(0,new FurniturePlacement[0]),Is.False);account.Settle(day,0,0);
            Assert.That(account.SetFurniture(1,new[]{new FurniturePlacement{id="base:3",kitchen=1,slot=4}}),Is.True);Assert.That(account.Data.furniture.Length,Is.EqualTo(2));
        }
        [Test] public void DayPaysOnceAndPurchasesSurviveReload()
        {
            var storage=new Memory();var account=new RestaurantAccount(storage);int day=account.StartDay();
            Assert.That(account.Data.cash,Is.Zero);Assert.That(account.Buy("counter",0),Is.False,"No shopping mid-day");
            Assert.That(account.Settle(day,100,25),Is.True);Assert.That(account.Settle(day,100,25),Is.False);
            Assert.That(account.Buy("counter",50),Is.True);Assert.That(account.Buy("counter",50),Is.False);
            var loaded=new RestaurantAccount(storage);Assert.That(loaded.Data.cash,Is.EqualTo(75));Assert.That(loaded.Owns("counter"),Is.True);
            Assert.That(loaded.Buy("server",150),Is.False);Assert.That(loaded.Data.cash,Is.EqualTo(75));
        }
        [Test] public void FailedWriteDoesNotDebitOrPayAndCanRetry()
        {
            var storage=new Memory();var account=new RestaurantAccount(storage);int day=account.StartDay();storage.fail=true;
            Assert.That(account.Settle(day,100,10),Is.False);Assert.That(account.Data.cash,Is.Zero);
            storage.fail=false;Assert.That(account.Settle(day,100,10),Is.True);storage.fail=true;
            Assert.That(account.Buy("counter",50),Is.False);Assert.That(account.Data.cash,Is.EqualTo(110));Assert.That(account.Owns("counter"),Is.False);
        }
        [TestCase("{\"schemaVersion\":99,\"cash\":500}")]
        [TestCase("broken")]
        [TestCase("{\"schemaVersion\":1,\"cash\":-1}")]
        public void UnknownOrCorruptSavesArePreserved(string json)
        {
            var storage=new Memory {json=json};var account=new RestaurantAccount(storage);
            Assert.That(account.Writable,Is.False);Assert.That(account.StartDay(),Is.Zero);Assert.That(storage.json,Is.EqualTo(json));
        }
        [Test] public void DifficultyCountsCompletedDaysOnlyAndOldSavesKeepMoney()
        {
            var storage=new Memory();var account=new RestaurantAccount(storage);account.StartDay();int current=account.StartDay();
            Assert.That(account.Data.completedDays,Is.Zero);storage.fail=true;Assert.That(account.Settle(current,20,0),Is.False);Assert.That(account.Data.completedDays,Is.Zero);
            storage.fail=false;Assert.That(account.Settle(current,20,0),Is.True);Assert.That(account.Settle(current,20,0),Is.False);Assert.That(account.Data.completedDays,Is.EqualTo(1));
            var loaded=new RestaurantAccount(storage);Assert.That(loaded.Data.completedDays,Is.EqualTo(1));
            storage.json=storage.json.Replace(",\"completedDays\":1","");loaded=new RestaurantAccount(storage);Assert.That(loaded.Writable,Is.True);Assert.That(loaded.Data.cash,Is.EqualTo(20));Assert.That(loaded.Data.completedDays,Is.Zero);
            var definition=UnityEngine.ScriptableObject.CreateInstance<DayServiceDefinition>();
            try{Assert.That(definition.CustomersForDay(1),Is.EqualTo(12));Assert.That(definition.CustomersForDay(2),Is.EqualTo(14));Assert.That(definition.CustomersForDay(3),Is.EqualTo(16));Assert.That(definition.IntervalForDay(3),Is.LessThan(definition.IntervalForDay(1)));}
            finally{UnityEngine.Object.DestroyImmediate(definition);}
        }
        [Test] public void WasteFeesReduceOnlyTodaysPayoutWithoutDebtAndRemainAtomic()
        {
            var store=new Memory();var account=new RestaurantAccount(store);int day=account.StartDay();Assert.That(account.Settle(day,10,5,2),Is.True);Assert.That(account.Data.cash,Is.EqualTo(13));
            day=account.StartDay();store.fail=true;Assert.That(account.Settle(day,10,5,2),Is.False);Assert.That(account.Data.cash,Is.EqualTo(13));store.fail=false;Assert.That(account.Settle(day,10,5,2),Is.True);Assert.That(account.Data.cash,Is.EqualTo(26));
            day=account.StartDay();Assert.That(account.Settle(day,0,0,3),Is.True);Assert.That(account.Data.cash,Is.EqualTo(26));Assert.That(new RestaurantAccount(store).Data.cash,Is.EqualTo(26));
        }
        [Test] public void RestartCannotPayAnAbandonedDay()
        {
            var account=new RestaurantAccount(new Memory());int abandoned=account.StartDay();int current=account.StartDay();
            Assert.That(account.Settle(abandoned,50,5),Is.False);Assert.That(account.Settle(current,10,0),Is.True);Assert.That(account.Data.cash,Is.EqualTo(10));
        }
    }
}
