using System;
using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class CookCareerTests
    {
        sealed class Memory:ISettingsStorage{public string json="";public bool fail;public string Read()=>json;public void Write(string s){if(fail)throw new Exception();json=s;}}
        [Test] public void NewCareerHasNoExpoAndOldSaveRetainsItsDeskAndPosition()
        {
            var fresh=new RestaurantAccount(new Memory());Assert.IsFalse(fresh.Owns("expo-desk"));
            var store=new Memory{json="{\"schemaVersion\":1,\"cash\":250,\"nextDay\":2,\"activeDay\":1,\"settledDay\":1,\"completedDays\":1,\"purchases\":[],\"furniture\":[{\"kitchen\":0,\"slot\":16,\"turns\":1,\"id\":\"expo\"}]}"};
            var old=new RestaurantAccount(store);Assert.IsTrue(old.Writable);Assert.IsTrue(old.Owns("expo-desk"));Assert.AreEqual(250,old.Data.cash);
            Assert.AreEqual("purchase:legacy-expo",old.Data.furniture[0].id);Assert.AreEqual(16,old.Data.furniture[0].slot);
            Assert.IsTrue(old.PrepareCareer(1));old=new RestaurantAccount(store);Assert.AreEqual(1,old.Quantity("expo-desk"));
            Assert.IsTrue(old.ResetCareer());Assert.IsFalse(old.Owns("expo-desk"));
        }
        [Test] public void ResumePreservesMoneyAndReopensUnfinishedDayWithoutAwardingIncome()
        {
            var store=new Memory();var a=new RestaurantAccount(store);int first=a.StartDay();a.Settle(first,500,0);int unfinished=a.StartDay();
            Assert.IsFalse(a.Buy("cook",200));Assert.IsTrue(a.PrepareCareer(2));Assert.AreEqual(500,a.Data.cash);Assert.AreEqual(1,a.Data.completedDays);
            Assert.IsTrue(a.BuyEquipment("expo-desk",100));Assert.IsTrue(a.Buy("cook",200));
            a=new RestaurantAccount(store);Assert.AreEqual(2,a.Data.careerKitchen);Assert.AreEqual(unfinished,a.StartDay());
            store.fail=true;Assert.IsFalse(a.PrepareCareer(0));Assert.AreEqual(2,a.Data.careerKitchen);Assert.AreEqual(unfinished,a.Data.activeDay);
        }
    }
}
