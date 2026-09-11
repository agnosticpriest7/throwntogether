using System;
using NUnit.Framework;
namespace ThrownTogether.Tests
{
    public sealed class RestaurantAccountTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public bool fail;public string Read()=>json;public void Write(string value){if(fail)throw new Exception("Storage unavailable");json=value;}}
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
        [Test] public void RestartCannotPayAnAbandonedDay()
        {
            var account=new RestaurantAccount(new Memory());int abandoned=account.StartDay();int current=account.StartDay();
            Assert.That(account.Settle(abandoned,50,5),Is.False);Assert.That(account.Settle(current,10,0),Is.True);Assert.That(account.Data.cash,Is.EqualTo(10));
        }
    }
}
