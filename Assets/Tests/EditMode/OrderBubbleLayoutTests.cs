using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class OrderBubbleLayoutTests
    {
        [Test] public void CardsStayInsideHudSafeAreaAtEveryTextSize()
        {
            foreach(float scale in new[]{1f,1.15f,1.3f})foreach(var p in new[]{new Vector2(-100,-100),new Vector2(940,120),new Vector2(940,430),new Vector2(1600,900)})
            {
                var r=OrderBubbleLayout.ForSeat(p,scale);Assert.That(r.xMin,Is.GreaterThanOrEqualTo(12));Assert.That(r.xMax,Is.LessThanOrEqualTo(1268));Assert.That(r.yMin,Is.GreaterThanOrEqualTo(76));Assert.That(r.yMax,Is.LessThanOrEqualTo(612));
            }
        }
        [Test] public void OrderStatesDescribePhasesWithoutInventingPatience()
        {
            Assert.That(OrderBubbleLayout.State(OrderPhase.Waiting),Is.EqualTo("TO COOK"));Assert.That(OrderBubbleLayout.State(OrderPhase.Delivering),Is.EqualTo("ON THE WAY"));Assert.That(OrderBubbleLayout.State(OrderPhase.Eating),Is.EqualTo("ENJOYING"));Assert.That(OrderBubbleLayout.State(OrderPhase.Complete),Is.EqualTo("SERVED"));
        }
    }
}
