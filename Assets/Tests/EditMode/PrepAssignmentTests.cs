using System;
using NUnit.Framework;
namespace ThrownTogether.Tests
{
    public sealed class PrepAssignmentTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public bool fail;public string Read()=>json;public void Write(string value){if(fail)throw new Exception("disk");json=value;}}
        [Test] public void BinAssignmentsPersistIndependentlyAndRejectInvalidSnapshots()
        {
            var storage=new Memory();var account=new RestaurantAccount(storage);int day=account.StartDay();account.Settle(day,500,0);account.Buy("prep-cook",150);
            var bins=new[]{new PrepBinAssignment{bin="purchase:a",ingredient="potato"},new PrepBinAssignment{bin="purchase:b",ingredient="mushroom"}};
            Assert.IsTrue(account.AssignPrepBins(0,true,bins));bins[0].ingredient="changed";
            Assert.IsTrue(account.AssignPrep(0,"base:3",""));account=new RestaurantAccount(storage);
            Assert.IsTrue(account.PrepAssignment(0).restock);Assert.AreEqual("potato",account.PrepAssignment(0).bins[0].ingredient);
            var copy=account.PrepAssignment(0);copy.bins[0].ingredient="changed";Assert.AreEqual("potato",account.PrepAssignment(0).bins[0].ingredient);
            Assert.IsFalse(account.AssignPrepBins(0,true,new[]{bins[1],bins[1]}));Assert.AreEqual(2,account.PrepAssignment(0).bins.Length);
            storage.fail=true;Assert.IsFalse(account.AssignPrepBins(0,false,new PrepBinAssignment[0]));Assert.IsTrue(account.PrepAssignment(0).restock);
            storage.fail=false;account.StartDay();Assert.IsFalse(account.AssignPrepBins(0,false,new PrepBinAssignment[0]));
        }
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
