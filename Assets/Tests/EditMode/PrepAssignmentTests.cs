using System;
using NUnit.Framework;
namespace ThrownTogether.Tests
{
    public sealed class PrepAssignmentTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public bool fail;public string Read()=>json;public void Write(string value){if(fail)throw new Exception("disk");json=value;}}
        [Test] public void AssignmentHireTrainingAndBinsPersistAndWritesAreAtomic()
        {
            var storage=new Memory();var account=new RestaurantAccount(storage);int day=account.StartDay();account.Settle(day,500,0);
            Assert.IsFalse(account.AssignPrep(0,"base:3","potato"));
            Assert.IsTrue(account.Buy("prep-cook",150));Assert.IsTrue(account.Train("prep-cook"));
            Assert.IsTrue(account.BuyEquipment("prep-bin",60));Assert.IsTrue(account.BuyEquipment("prep-bin",60));
            Assert.IsTrue(account.AssignPrep(0,"base:3","potato"));
            storage.fail=true;Assert.IsFalse(account.AssignPrep(0,"other","tomato"));Assert.AreEqual("potato",account.PrepAssignment(0).ingredient);storage.fail=false;
            account=new RestaurantAccount(storage);Assert.AreEqual(2,account.Quantity("prep-bin"));Assert.AreEqual(1.1f,account.StaffSpeed("prep-cook"));Assert.AreEqual("base:3",account.PrepAssignment(0).station);
            var snapshot=account.PrepAssignment(0);snapshot.station="mutated";Assert.AreEqual("base:3",account.PrepAssignment(0).station);
            Assert.AreEqual("",account.PrepAssignment(1).station);Assert.IsTrue(account.AssignPrep(0,"",""));
            account.StartDay();Assert.IsFalse(account.AssignPrep(0,"base:3","potato"));
        }
    }
}
