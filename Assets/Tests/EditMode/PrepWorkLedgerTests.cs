using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
namespace ThrownTogether.Tests
{
    public sealed class PrepWorkLedgerTests
    {
        static KeyValuePair<string,int> Need(string key,int count)=>new KeyValuePair<string,int>(key,count);
        static PrepWorkLedger Ledger(params KeyValuePair<string,int>[] deficits)
        {
            var ledger=new PrepWorkLedger();ledger.SetDeficits(deficits);return ledger;
        }
        [Test] public void OnePortionIsReservedByExactlyOneWorker()
        {
            var ledger=Ledger(Need("cut-tomato",1));
            var first=Guid.NewGuid();var second=Guid.NewGuid();
            var claim=ledger.TryClaim(first,"cut-tomato");
            Assert.That(claim,Is.Not.Null);
            Assert.That(claim.WorkerId,Is.EqualTo(first));Assert.That(claim.Key,Is.EqualTo("cut-tomato"));
            Assert.That(claim.Id,Is.Not.EqualTo(Guid.Empty));
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1));
            Assert.That(ledger.Available("cut-tomato"),Is.Zero);
            Assert.That(ledger.TryClaim(second,"cut-tomato"),Is.Null,"A second worker cannot chase the same portion");
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1));
            Assert.That(ledger.Claims.Count,Is.EqualTo(1));
        }
        [Test] public void TwoPortionsSupportTwoWorkersButNotAThird()
        {
            var ledger=Ledger(Need("cut-tomato",2));
            var a=ledger.TryClaim(Guid.NewGuid(),"cut-tomato");
            Assert.That(ledger.Available("cut-tomato"),Is.EqualTo(1));
            var b=ledger.TryClaim(Guid.NewGuid(),"cut-tomato");
            Assert.That(a,Is.Not.Null);Assert.That(b,Is.Not.Null);
            Assert.That(a.Id,Is.Not.EqualTo(b.Id),"Each reservation has its own identity");
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(2));
            Assert.That(ledger.Available("cut-tomato"),Is.Zero);
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"cut-tomato"),Is.Null);
            Assert.That(ledger.Claims.Count,Is.EqualTo(2));
        }
        [Test] public void AWorkerHoldsOneClaimAtATimeAcrossEveryKey()
        {
            var ledger=Ledger(Need("cut-tomato",2),Need("cut-lettuce",2));
            var worker=Guid.NewGuid();
            var held=ledger.TryClaim(worker,"cut-tomato");
            Assert.That(held,Is.Not.Null);
            Assert.That(ledger.TryClaim(worker,"cut-lettuce"),Is.Null,"No automatic switching to another key");
            Assert.That(ledger.TryClaim(worker,"cut-tomato"),Is.Null,"No second claim on the same key either");
            Assert.That(ledger.Reserved("cut-lettuce"),Is.Zero);
            Assert.That(ledger.Available("cut-lettuce"),Is.EqualTo(2),"Another worker's key is untouched");
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"cut-lettuce"),Is.Not.Null);
            Assert.That(ledger.Release(worker,held.Id),Is.True);
            Assert.That(ledger.TryClaim(worker,"cut-lettuce"),Is.Not.Null,"Releasing frees the worker to take other work");
        }
        [Test] public void OnlyTheOwnerCanReleaseAndStaleIdentitiesChangeNothing()
        {
            var ledger=Ledger(Need("cut-tomato",1));
            var owner=Guid.NewGuid();var stranger=Guid.NewGuid();
            var claim=ledger.TryClaim(owner,"cut-tomato");
            Assert.That(ledger.Release(stranger,claim.Id),Is.False,"Another worker cannot release this claim");
            Assert.That(ledger.Release(owner,Guid.NewGuid()),Is.False,"An unknown claim id releases nothing");
            Assert.That(ledger.Release(Guid.Empty,claim.Id),Is.False);
            Assert.That(ledger.Release(owner,Guid.Empty),Is.False);
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1),"No refused release changed the ledger");
            Assert.That(ledger.Claims.Count,Is.EqualTo(1));
            Assert.That(ledger.Release(owner,claim.Id),Is.True);
            Assert.That(ledger.Release(owner,claim.Id),Is.False,"A released claim is stale and cannot be released twice");
            Assert.That(ledger.Reserved("cut-tomato"),Is.Zero);
            Assert.That(ledger.Available("cut-tomato"),Is.EqualTo(1));
            Assert.That(ledger.Claims,Is.Empty);
        }
        [Test] public void ReclaimingAfterReleaseProducesAFreshIdentity()
        {
            var ledger=Ledger(Need("fry-potato",1));
            var worker=Guid.NewGuid();
            var first=ledger.TryClaim(worker,"fry-potato");
            Assert.That(ledger.Release(worker,first.Id),Is.True);
            var second=ledger.TryClaim(worker,"fry-potato");
            Assert.That(second,Is.Not.Null);
            Assert.That(second.Id,Is.Not.EqualTo(first.Id),"The new reservation is not the old one");
            Assert.That(ledger.Release(worker,first.Id),Is.False,"The old id stays dead");
            Assert.That(ledger.Reserved("fry-potato"),Is.EqualTo(1));
            Assert.That(ledger.Release(worker,second.Id),Is.True);
        }
        [Test] public void DemandRemovalKeepsInFlightClaimsButOffersNoFreshWork()
        {
            var ledger=Ledger(Need("cut-tomato",2));
            var carrying=Guid.NewGuid();
            var claim=ledger.TryClaim(carrying,"cut-tomato");
            // The order was held or cancelled: the caller replaces the snapshot entirely.
            ledger.SetDeficits(new KeyValuePair<string,int>[0]);
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1),"A worker already carrying food keeps their claim");
            Assert.That(ledger.Available("cut-tomato"),Is.Zero);
            Assert.That(ledger.Claims.Count,Is.EqualTo(1));
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"cut-tomato"),Is.Null,"Removed demand offers no new work");
            // Demand returns; the retained claim still counts against it.
            ledger.SetDeficits(new[]{Need("cut-tomato",2)});
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1));
            Assert.That(ledger.Available("cut-tomato"),Is.EqualTo(1),"Returning demand accounts for the claim still held");
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"cut-tomato"),Is.Not.Null);
            Assert.That(ledger.Available("cut-tomato"),Is.Zero);
            Assert.That(ledger.Release(carrying,claim.Id),Is.True);
            Assert.That(ledger.Available("cut-tomato"),Is.EqualTo(1));
        }
        [Test] public void ASnapshotSmallerThanTheReservationsNeverGoesNegative()
        {
            var ledger=Ledger(Need("cut-tomato",3));
            var workers=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};
            foreach(var worker in workers)Assert.That(ledger.TryClaim(worker,"cut-tomato"),Is.Not.Null);
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(3));
            ledger.SetDeficits(new[]{Need("cut-tomato",1)});
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(3),"Shrinking demand does not discard claims");
            Assert.That(ledger.Available("cut-tomato"),Is.Zero,"Availability floors at zero rather than going negative");
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"cut-tomato"),Is.Null);
            var live=ledger.Claims;
            Assert.That(ledger.Release(workers[0],live[0].Id),Is.True);
            Assert.That(ledger.Available("cut-tomato"),Is.Zero,"Still over-reserved after one release");
            Assert.That(ledger.Release(workers[1],live[1].Id),Is.True);
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1));
            Assert.That(ledger.Available("cut-tomato"),Is.Zero);
        }
        [Test] public void KeysAreIndependentAndUnknownOrBlankKeysAreInert()
        {
            var ledger=Ledger(Need("cut-tomato",1),Need("cut-lettuce",1),Need("fry-potato",0));
            var worker=Guid.NewGuid();
            Assert.That(ledger.TryClaim(worker,"cut-tomato"),Is.Not.Null);
            Assert.That(ledger.Available("cut-lettuce"),Is.EqualTo(1),"One key's reservation does not consume another");
            Assert.That(ledger.Reserved("cut-lettuce"),Is.Zero);
            Assert.That(ledger.Available("fry-potato"),Is.Zero,"A zero deficit offers nothing");
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"fry-potato"),Is.Null);
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"grill-steak"),Is.Null,"An unknown key cannot be claimed");
            Assert.That(ledger.Available("grill-steak"),Is.Zero);
            Assert.That(ledger.Reserved("grill-steak"),Is.Zero);
            foreach(string blank in new[]{null,"","   "})
            {
                Assert.That(ledger.TryClaim(Guid.NewGuid(),blank),Is.Null);
                Assert.That(ledger.Available(blank),Is.Zero);
                Assert.That(ledger.Reserved(blank),Is.Zero);
            }
            Assert.That(ledger.TryClaim(Guid.Empty,"cut-lettuce"),Is.Null,"An anonymous worker cannot reserve work");
            Assert.That(ledger.Reserved("cut-lettuce"),Is.Zero);
            Assert.That(ledger.Claims.Count,Is.EqualTo(1));
        }
        [Test] public void InvalidSnapshotsAreRejectedWithoutDisturbingAnything()
        {
            var ledger=Ledger(Need("cut-tomato",2));
            var worker=Guid.NewGuid();
            var claim=ledger.TryClaim(worker,"cut-tomato");
            void Unchanged(string because)
            {
                Assert.That(ledger.Available("cut-tomato"),Is.EqualTo(1),because);
                Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1),because);
                Assert.That(ledger.Claims.Count,Is.EqualTo(1),because);
            }
            Assert.Throws<ArgumentNullException>(()=>ledger.SetDeficits(null));
            Unchanged("a null snapshot changes nothing");
            foreach(string blank in new[]{null,"","   "})
            {
                Assert.Throws<ArgumentException>(()=>ledger.SetDeficits(new[]{Need("cut-lettuce",1),Need(blank,1)}));
                Unchanged("a blank key rejects the whole snapshot");
            }
            Assert.Throws<ArgumentException>(()=>ledger.SetDeficits(new[]{Need("cut-lettuce",1),Need("fry-potato",-1)}));
            Unchanged("a negative count rejects the whole snapshot");
            Assert.Throws<ArgumentException>(()=>ledger.SetDeficits(new[]{Need("cut-lettuce",1),Need("cut-lettuce",2)}));
            Unchanged("duplicate keys reject the whole snapshot");
            Assert.That(ledger.Available("cut-lettuce"),Is.Zero,"No partial entry survived a rejected snapshot");
            ledger.SetDeficits(new[]{Need("cut-tomato",2),Need("cut-lettuce",1)});
            Assert.That(ledger.Available("cut-lettuce"),Is.EqualTo(1),"A valid snapshot still applies afterwards");
            Assert.That(ledger.Release(worker,claim.Id),Is.True);
        }
        [Test] public void ClaimsIsADefensiveSnapshot()
        {
            var ledger=Ledger(Need("cut-tomato",2));
            var worker=Guid.NewGuid();
            var claim=ledger.TryClaim(worker,"cut-tomato");
            var before=ledger.Claims;
            Assert.That(before.Count,Is.EqualTo(1));
            Assert.That(ledger.Claims,Is.Not.SameAs(ledger.Claims),"Each read returns its own snapshot");
            Assert.Throws<NotSupportedException>(()=>((IList<PrepWorkClaim>)before).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<PrepWorkClaim>)before).Add(claim));
            ledger.TryClaim(Guid.NewGuid(),"cut-tomato");
            Assert.That(before.Count,Is.EqualTo(1),"An old snapshot does not grow with the ledger");
            Assert.That(ledger.Claims.Count,Is.EqualTo(2));
            Assert.That(ledger.Release(worker,claim.Id),Is.True);
            Assert.That(before.Count,Is.EqualTo(1),"A historical snapshot keeps its own contents");
            Assert.That(ledger.Claims.Any(c=>c.Id==claim.Id),Is.False,"The released claim is gone from the live list");
            Assert.That(ledger.Reserved("cut-tomato"),Is.EqualTo(1));
        }
        [Test] public void CompletedWorkIsAccountedByRefreshingDeficitsBeforeRelease()
        {
            // The integration order Codex uses: the physical portion now exists, so the
            // caller lowers the deficit first and only then releases the finished claim.
            var ledger=Ledger(Need("cut-tomato",2));
            var worker=Guid.NewGuid();
            var claim=ledger.TryClaim(worker,"cut-tomato");
            Assert.That(ledger.Available("cut-tomato"),Is.EqualTo(1));
            ledger.SetDeficits(new[]{Need("cut-tomato",1)});
            Assert.That(ledger.Available("cut-tomato"),Is.Zero,"The remaining portion stays reserved until release");
            Assert.That(ledger.Release(worker,claim.Id),Is.True);
            Assert.That(ledger.Reserved("cut-tomato"),Is.Zero);
            Assert.That(ledger.Available("cut-tomato"),Is.EqualTo(1),"Exactly the still-missing portion is offered again");
            Assert.That(ledger.TryClaim(Guid.NewGuid(),"cut-tomato"),Is.Not.Null);
            Assert.That(ledger.Available("cut-tomato"),Is.Zero);
        }
    }
}
