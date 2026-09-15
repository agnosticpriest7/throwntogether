using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class ExpoPanelTests
    {
        RecipeDefinition recipe;
        [SetUp] public void Setup(){recipe=ScriptableObject.CreateInstance<RecipeDefinition>();}
        [TearDown] public void Cleanup(){Object.DestroyImmediate(recipe);}
        [Test] public void SlotsRemainStableWhenEarlierOrderIsHeldAndRefired()
        {
            var b=new KitchenTicketBoard(3);var a=b.Create(recipe,0);var c=b.Create(recipe,0);var d=b.Create(recipe,0);
            b.TryFire(a,1);b.TryFire(c,1);b.TryFire(d,1);var m=new ExpoPanelModel();m.Refresh(b.Tickets,b.Capacity,a);
            b.TryHold(a);m.Refresh(b.Tickets,b.Capacity,a);
            Assert.That(m.Slots[0],Is.Null);Assert.That(m.Slots[1],Is.SameAs(c));Assert.That(m.Waiting,Is.EqualTo(new[]{a}));
            b.TryFire(a,2);b.TryMarkReady(c);m.Refresh(b.Tickets,b.Capacity,a);
            Assert.That(m.Slots,Is.EqualTo(new[]{a,c,d}));Assert.That(c.State,Is.EqualTo(KitchenTicketState.Ready));
            Assert.That(m.Number(c),Is.EqualTo(2));
        }
        [Test] public void WaitingWindowFollowsFocusAndClampsAfterDepartures()
        {
            var b=new KitchenTicketBoard();for(int i=0;i<8;i++)b.Create(recipe,0);var m=new ExpoPanelModel();
            m.Refresh(b.Tickets,b.Capacity,b.Tickets[7]);Assert.That(m.WaitingTop,Is.EqualTo(3));
            Assert.That(m.Waiting[m.WaitingTop+4],Is.SameAs(b.Tickets[7]));
            b.TryCancel(b.Tickets[7]);b.TryServe(b.Tickets[6]);m.Refresh(b.Tickets,b.Capacity,b.Tickets[5]);
            Assert.That(m.WaitingTop,Is.EqualTo(1));Assert.That(m.Number(b.Tickets[5]),Is.EqualTo(6));
            m.Refresh(b.Tickets,b.Capacity,b.Tickets[0]);Assert.That(m.WaitingTop,Is.Zero);
        }
        [Test] public void PresentationNeverChangesQueueOrTicketState()
        {
            var b=new KitchenTicketBoard();var a=b.Create(recipe,0);b.TryFire(a,1);var m=new ExpoPanelModel();
            for(int i=0;i<20;i++)m.Refresh(b.Tickets,b.Capacity,a);
            Assert.That(b.ActiveCount,Is.EqualTo(1));Assert.That(b.Capacity,Is.EqualTo(2));Assert.That(a.State,Is.EqualTo(KitchenTicketState.Active));
            Assert.That(m.Slots.Length,Is.EqualTo(2));b.TryServe(a);m.Refresh(b.Tickets,b.Capacity,null);Assert.That(m.Slots[0],Is.Null);
        }
    }
}
