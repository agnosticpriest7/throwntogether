using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class KitchenTicketTests
    {
        readonly List<RecipeDefinition> recipes=new List<RecipeDefinition>();
        RecipeDefinition Dish(string id)
        {
            var recipe=ScriptableObject.CreateInstance<RecipeDefinition>();recipe.name=id;recipes.Add(recipe);return recipe;
        }
        [TearDown] public void TearDown()
        {
            foreach(var recipe in recipes)if(recipe!=null)UnityEngine.Object.DestroyImmediate(recipe);
            recipes.Clear();
        }
        [Test] public void NewTicketWaitsOutsideTheActiveQueue()
        {
            var board=new KitchenTicketBoard();var ticket=board.Create(Dish("fries"),0);
            Assert.That(ticket.State,Is.EqualTo(KitchenTicketState.Waiting));
            Assert.That(ticket.FiredAt,Is.Null);Assert.That(ticket.FireSequence,Is.Zero);
            Assert.That(ticket.Id,Is.Not.EqualTo(Guid.Empty));
            Assert.That(board.WaitingTickets,Is.EquivalentTo(new[]{ticket}));
            Assert.That(board.ActiveTickets,Is.Empty);Assert.That(board.ActiveCount,Is.Zero);
            Assert.That(board.Tickets,Is.EquivalentTo(new[]{ticket}));
        }
        [Test] public void CapacityDefaultsToTwoIsEnforcedAndRejectsInvalidConstruction()
        {
            Assert.That(new KitchenTicketBoard().Capacity,Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new KitchenTicketBoard(0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new KitchenTicketBoard(-3));
            var board=new KitchenTicketBoard(2);var recipe=Dish("fries");
            var a=board.Create(recipe,0);var b=board.Create(recipe,1);var c=board.Create(recipe,2);
            Assert.That(board.TryFire(a,3),Is.True);Assert.That(board.TryFire(b,3),Is.True);
            Assert.That(board.TryFire(c,3),Is.False,"A third order must not enter a two-slot kitchen");
            Assert.That(c.State,Is.EqualTo(KitchenTicketState.Waiting));Assert.That(board.ActiveCount,Is.EqualTo(2));
            Assert.That(board.TryServe(a),Is.True);
            Assert.That(board.TryFire(c,4),Is.True,"Serving frees the slot");
            Assert.That(board.ActiveCount,Is.EqualTo(2));
        }
        [Test] public void IdenticalRecipesStayIndependentTickets()
        {
            var board=new KitchenTicketBoard(3);var recipe=Dish("hash-browns");
            var a=board.Create(recipe,0);var b=board.Create(recipe,0);
            Assert.That(a.Id,Is.Not.EqualTo(b.Id));Assert.That(a,Is.Not.SameAs(b));
            Assert.That(board.TryFire(a,1),Is.True);
            Assert.That(b.State,Is.EqualTo(KitchenTicketState.Waiting),"Firing one diner's order must not fire another's");
            Assert.That(board.TryServe(a),Is.True);
            Assert.That(b.State,Is.EqualTo(KitchenTicketState.Waiting));Assert.That(board.ActiveCount,Is.Zero);
        }
        [Test] public void NullForeignAndDuplicateOperationsFailWithoutSideEffects()
        {
            var board=new KitchenTicketBoard(2);var other=new KitchenTicketBoard(2);var recipe=Dish("garden-salad");
            var mine=board.Create(recipe,0);var theirs=other.Create(recipe,0);
            Assert.Throws<ArgumentNullException>(()=>board.Create(null,0));
            Assert.That(board.TryFire(null,1),Is.False);Assert.That(board.TryMarkReady(null),Is.False);
            Assert.That(board.TryInvalidateReady(null),Is.False);Assert.That(board.TryServe(null),Is.False);Assert.That(board.TryCancel(null),Is.False);
            Assert.That(board.TryFire(theirs,1),Is.False,"A matching recipe does not grant membership");
            Assert.That(board.TryServe(theirs),Is.False);Assert.That(board.TryCancel(theirs),Is.False);
            Assert.That(theirs.State,Is.EqualTo(KitchenTicketState.Waiting));
            Assert.That(board.Tickets.Count,Is.EqualTo(1));Assert.That(board.ActiveCount,Is.Zero);
            Assert.That(board.TryFire(mine,5),Is.True);
            float? fired=mine.FiredAt;long sequence=mine.FireSequence;
            Assert.That(board.TryFire(mine,9),Is.False,"A second fire must be inert");
            Assert.That(mine.FiredAt,Is.EqualTo(fired));Assert.That(mine.FireSequence,Is.EqualTo(sequence));
            Assert.That(board.ActiveCount,Is.EqualTo(1));
        }
        [Test] public void FireOrderFollowsCallOrderEvenWithIdenticalTimestamps()
        {
            var board=new KitchenTicketBoard(3);var recipe=Dish("fries");
            var a=board.Create(recipe,0);var b=board.Create(recipe,0);var c=board.Create(recipe,0);
            Assert.That(board.TryFire(c,10),Is.True);Assert.That(board.TryFire(a,10),Is.True);Assert.That(board.TryFire(b,10),Is.True);
            Assert.That(board.ActiveTickets,Is.EqualTo(new[]{c,a,b}),"Equal fire times must not collapse the queue order");
            Assert.That(c.FireSequence,Is.LessThan(a.FireSequence));Assert.That(a.FireSequence,Is.LessThan(b.FireSequence));
            Assert.That(board.Tickets,Is.EqualTo(new[]{a,b,c}),"All tickets stay in creation order");
        }
        [Test] public void ReadyHoldsCapacityAndInvalidatingItKeepsFireOrder()
        {
            var board=new KitchenTicketBoard(1);var recipe=Dish("fries");
            var a=board.Create(recipe,0);var b=board.Create(recipe,0);
            Assert.That(board.TryFire(a,1),Is.True);
            Assert.That(board.TryMarkReady(a),Is.True);Assert.That(board.TryMarkReady(a),Is.False,"Ready is not repeatable");
            Assert.That(board.ActiveCount,Is.EqualTo(1),"A plated dish still occupies its slot until it is served");
            Assert.That(board.TryFire(b,2),Is.False);
            float? fired=a.FiredAt;long sequence=a.FireSequence;
            Assert.That(board.TryInvalidateReady(a),Is.True);
            Assert.That(a.State,Is.EqualTo(KitchenTicketState.Active));
            Assert.That(a.FiredAt,Is.EqualTo(fired));Assert.That(a.FireSequence,Is.EqualTo(sequence),"A lost dish must not reorder the queue");
            Assert.That(board.ActiveCount,Is.EqualTo(1));Assert.That(board.TryFire(b,2),Is.False);
            Assert.That(board.TryInvalidateReady(a),Is.False,"Only a Ready ticket can be invalidated");
            Assert.That(board.TryMarkReady(b),Is.False,"An unfired order cannot be plated");
        }
        [Test] public void ServeSucceedsFromWaitingActiveOrReadyExactlyOnce()
        {
            var board=new KitchenTicketBoard(2);var recipe=Dish("fries");
            var direct=board.Create(recipe,0);var staged=board.Create(recipe,0);
            var held=board.Create(recipe,0);Assert.That(board.TryServe(held),Is.True,"Hold is only a kitchen instruction");
            Assert.That(board.TryServe(held),Is.False);
            Assert.That(board.TryFire(direct,1),Is.True);
            Assert.That(board.TryServe(direct),Is.True,"A carried dish may be delivered without staging");
            Assert.That(board.TryServe(direct),Is.False);Assert.That(board.ActiveCount,Is.Zero);
            Assert.That(board.TryFire(staged,2),Is.True);Assert.That(board.TryMarkReady(staged),Is.True);
            Assert.That(board.TryServe(staged),Is.True);
            Assert.That(staged.State,Is.EqualTo(KitchenTicketState.Served));Assert.That(board.ActiveCount,Is.Zero);
        }
        [Test] public void PriorityPromotesAndRenumbersWithoutMovingPhysicalBayOrder()
        {
            var board=new KitchenTicketBoard(3);var recipe=Dish("compound");
            var a=board.Create(recipe,0);var b=board.Create(recipe,0);var c=board.Create(recipe,0);
            Assert.That(board.TryPromote(a),Is.False);
            board.TryFire(a,1);board.TryFire(b,1);board.TryFire(c,1);
            CollectionAssert.AreEqual(new[]{1,2,3},new[]{a.Priority,b.Priority,c.Priority});
            Assert.That(board.TryPromote(c),Is.True);Assert.That(board.TryPromote(c),Is.True);
            CollectionAssert.AreEqual(new[]{c,a,b},board.PrioritizedTickets);
            CollectionAssert.AreEqual(new[]{a,b,c},board.ActiveTickets,"Priority must not change bay assignment");
            Assert.That(board.TryPromote(c),Is.False);
            board.TryHold(a);Assert.That(a.Priority,Is.Zero);Assert.That(b.Priority,Is.EqualTo(2));
            board.TryFire(a,2);Assert.That(a.Priority,Is.EqualTo(3));
            board.TryMarkReady(c);Assert.That(c.Priority,Is.EqualTo(1));
            board.TryServe(c);Assert.That(c.Priority,Is.Zero);Assert.That(b.Priority,Is.EqualTo(1));
            board.TryCancel(b);Assert.That(a.Priority,Is.EqualTo(1));
            var other=new KitchenTicketBoard();Assert.That(other.TryPromote(a),Is.False);
            Assert.That(other.PrioritizedTickets,Is.Empty);
        }
        [Test] public void HoldFreesKitchenCapacityWithoutReorderingTicketsOrRevivingTerminalOrders()
        {
            var board=new KitchenTicketBoard(1);var recipe=Dish("fries");
            var a=board.Create(recipe,0);var b=board.Create(recipe,1);
            Assert.That(board.TryFire(a,2),Is.True);Assert.That(board.TryMarkReady(a),Is.True);
            Assert.That(board.TryHold(a),Is.True);Assert.That(board.ActiveCount,Is.Zero);
            Assert.That(board.TryHold(a),Is.False);Assert.That(board.TryFire(b,3),Is.True);
            Assert.That(board.Tickets,Is.EqualTo(new[]{a,b}));
            Assert.That(board.TryServe(a),Is.True);Assert.That(board.TryHold(a),Is.False);
            Assert.That(board.TryCancel(b),Is.True);Assert.That(board.TryHold(b),Is.False);
            Assert.That(board.TryHold(null),Is.False);
            var foreign=new KitchenTicketBoard().Create(recipe,0);Assert.That(board.TryHold(foreign),Is.False);
        }
        [Test] public void CancellationFromEveryLiveStateFreesCapacityAppropriately()
        {
            var board=new KitchenTicketBoard(2);var recipe=Dish("fries");
            var waiting=board.Create(recipe,0);var active=board.Create(recipe,0);var ready=board.Create(recipe,0);
            Assert.That(board.TryCancel(waiting),Is.True);
            Assert.That(board.ActiveCount,Is.Zero,"Cancelling an unfired order never held a slot");
            Assert.That(board.WaitingTickets,Is.EquivalentTo(new[]{active,ready}));
            Assert.That(board.TryFire(active,1),Is.True);Assert.That(board.TryFire(ready,1),Is.True);
            Assert.That(board.TryMarkReady(ready),Is.True);Assert.That(board.ActiveCount,Is.EqualTo(2));
            Assert.That(board.TryCancel(active),Is.True);Assert.That(board.ActiveCount,Is.EqualTo(1));
            Assert.That(board.TryCancel(ready),Is.True);Assert.That(board.ActiveCount,Is.Zero);
            Assert.That(board.ActiveTickets,Is.Empty);Assert.That(board.WaitingTickets,Is.Empty);
            Assert.That(board.Tickets.Count,Is.EqualTo(3),"Terminal tickets are retained for the day");
        }
        [Test] public void TerminalTicketsCannotBeResurrectedOrRewritten()
        {
            var board=new KitchenTicketBoard(2);var recipe=Dish("fries");
            var served=board.Create(recipe,0);var cancelled=board.Create(recipe,0);
            Assert.That(board.TryFire(served,1),Is.True);Assert.That(board.TryServe(served),Is.True);
            Assert.That(board.TryCancel(served),Is.False,"A diner leaving after their meal must not overwrite Served");
            Assert.That(served.State,Is.EqualTo(KitchenTicketState.Served));
            Assert.That(board.TryFire(served,2),Is.False);Assert.That(board.TryMarkReady(served),Is.False);
            Assert.That(board.TryInvalidateReady(served),Is.False);Assert.That(board.TryServe(served),Is.False);
            Assert.That(served.FiredAt,Is.EqualTo(1f),"Fire history survives the terminal transition");
            Assert.That(board.TryFire(cancelled,1),Is.True);Assert.That(board.TryCancel(cancelled),Is.True);
            Assert.That(board.TryServe(cancelled),Is.False);Assert.That(board.TryFire(cancelled,3),Is.False);
            Assert.That(board.TryCancel(cancelled),Is.False);
            Assert.That(cancelled.State,Is.EqualTo(KitchenTicketState.Cancelled));Assert.That(board.ActiveCount,Is.Zero);
        }
        [Test] public void RepeatedServiceCyclesDoNotDriftCountsOrOrder()
        {
            var board=new KitchenTicketBoard(2);var recipe=Dish("fries");long lastSequence=0;
            for(int i=0;i<50;i++)
            {
                var a=board.Create(recipe,i);var b=board.Create(recipe,i);var spare=board.Create(recipe,i);
                Assert.That(board.TryFire(a,i),Is.True);Assert.That(board.TryFire(b,i),Is.True);
                Assert.That(board.TryFire(spare,i),Is.False,"Capacity must hold on every cycle");
                Assert.That(a.FireSequence,Is.GreaterThan(lastSequence));lastSequence=b.FireSequence;
                Assert.That(board.TryMarkReady(a),Is.True);Assert.That(board.TryInvalidateReady(a),Is.True);
                Assert.That(board.TryMarkReady(a),Is.True);Assert.That(board.TryServe(a),Is.True);
                Assert.That(board.TryCancel(b),Is.True);Assert.That(board.TryCancel(spare),Is.True);
                Assert.That(board.ActiveCount,Is.Zero);Assert.That(board.ActiveTickets,Is.Empty);Assert.That(board.WaitingTickets,Is.Empty);
            }
            Assert.That(board.Tickets.Count,Is.EqualTo(150));
        }
        [Test] public void NewBoardRejectsTicketsFromAPreviousService()
        {
            var recipe=Dish("fries");
            var yesterday=new KitchenTicketBoard(2);var stale=yesterday.Create(recipe,0);
            Assert.That(yesterday.TryFire(stale,1),Is.True);
            var today=new KitchenTicketBoard(2);var fresh=today.Create(recipe,0);
            Assert.That(today.TryServe(stale),Is.False);Assert.That(today.TryCancel(stale),Is.False);
            Assert.That(today.TryMarkReady(stale),Is.False);Assert.That(today.TryInvalidateReady(stale),Is.False);
            Assert.That(today.TryFire(stale,1),Is.False);
            Assert.That(stale.State,Is.EqualTo(KitchenTicketState.Active),"A stale reference must not be mutated by the new day");
            Assert.That(today.Tickets,Is.EquivalentTo(new[]{fresh}));Assert.That(today.ActiveCount,Is.Zero);
        }
        [Test] public void InvalidTimestampsAreRejectedWithoutConsumingFireOrder()
        {
            var board=new KitchenTicketBoard(3);var recipe=Dish("fries");
            Assert.Throws<ArgumentOutOfRangeException>(()=>board.Create(recipe,-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>board.Create(recipe,float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(()=>board.Create(recipe,float.PositiveInfinity));
            Assert.That(board.Tickets,Is.Empty,"A rejected create must not register a ticket");
            var first=board.Create(recipe,0);var second=board.Create(recipe,20);
            Assert.That(board.TryFire(first,5),Is.True);Assert.That(first.FireSequence,Is.EqualTo(1));
            Assert.That(board.TryFire(second,float.NaN),Is.False);
            Assert.That(board.TryFire(second,float.NegativeInfinity),Is.False);
            Assert.That(board.TryFire(second,-2),Is.False);
            Assert.That(board.TryFire(second,19),Is.False,"An order cannot be fired before the diner ordered it");
            Assert.That(second.State,Is.EqualTo(KitchenTicketState.Waiting));
            Assert.That(second.FiredAt,Is.Null);Assert.That(second.FireSequence,Is.Zero);
            Assert.That(board.TryFire(second,20),Is.True);
            Assert.That(second.FireSequence,Is.EqualTo(2),"Failed attempts must not burn a queue position");
        }
        [Test] public void ReturnedCollectionsAreSnapshotsThatCannotChangeTheBoard()
        {
            var board=new KitchenTicketBoard(2);var recipe=Dish("fries");
            var first=board.Create(recipe,0);Assert.That(board.TryFire(first,1),Is.True);
            var allBefore=board.Tickets;var activeBefore=board.ActiveTickets;var waitingBefore=board.WaitingTickets;
            Assert.Throws<NotSupportedException>(()=>((IList<KitchenTicket>)allBefore).Add(first));
            Assert.Throws<NotSupportedException>(()=>((IList<KitchenTicket>)allBefore).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<KitchenTicket>)activeBefore).Clear());
            var second=board.Create(recipe,2);
            Assert.That(allBefore.Count,Is.EqualTo(1),"An old snapshot must not grow with the board");
            Assert.That(board.Tickets.Count,Is.EqualTo(2));
            Assert.That(waitingBefore,Is.Empty);Assert.That(board.WaitingTickets,Is.EquivalentTo(new[]{second}));
            Assert.That(board.TryServe(first),Is.True);
            Assert.That(activeBefore,Is.EqualTo(new[]{first}),"A historical snapshot keeps its own contents");
            Assert.That(board.ActiveTickets,Is.Empty);Assert.That(board.ActiveCount,Is.Zero);
            Assert.That(board.Tickets,Is.Not.SameAs(board.Tickets),"Each read returns its own snapshot");
        }
    }
}
