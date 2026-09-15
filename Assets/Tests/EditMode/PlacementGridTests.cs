using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class PlacementGridTests
    {
        [Test] public void EveryLegacySaveIndexKeepsItsWorldPosition()
        {
            for(int i=0;i<KitchenFurniture.ExpandedSlots.Length;i++)Assert.That(KitchenFurniture.GridSlots[i],Is.EqualTo(KitchenFurniture.ExpandedSlots[i]));
        }
        [Test] public void FullGridCoversEveryApplianceCellAndGatesWestExtension()
        {
            for(int row=0;row<6;row++)for(int col=0;col<8;col++)
            {
                var p=new Vector3(-11+col*1.9f,0,5.8f-row*2.04f);
                int i=System.Array.FindIndex(KitchenFurniture.GridSlots,s=>Vector3.Distance(s,p)<.02f);
                Assert.That(i,Is.GreaterThanOrEqualTo(0));Assert.That(KitchenFurniture.SlotAvailable(i,true),Is.True);
                Assert.That(KitchenFurniture.SlotAvailable(i,false),Is.EqualTo(col>=2));
            }
            Assert.That(KitchenFurniture.SlotAvailable(-1,true),Is.False);
            Assert.That(KitchenFurniture.SlotAvailable(KitchenFurniture.GridSlots.Length,true),Is.False);
        }
    }
}
