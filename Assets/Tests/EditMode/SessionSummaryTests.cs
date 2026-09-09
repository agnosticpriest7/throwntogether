using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class SessionSummaryTests
    {
        [Test] public void ContributionsSeparatePrepFryingPlatingAndService()
        {
            var go=new GameObject("Contribution fixture"); var recipe=ScriptableObject.CreateInstance<ProcessingRecipe>();
            try
            {
                var process=go.AddComponent<ProcessingStation>(); process.recipe=recipe;
                var count=new SessionSummary.Contribution();
                SessionSummary.Record(count,process,false,false);
                Assert.That(count.prep+count.fry+count.served+count.plates,Is.Zero,"Collecting food is not a second processing start");
                var counter=go.AddComponent<CounterStation>(); SessionSummary.Record(count,counter,true,true);
                Assert.That(count.plates,Is.EqualTo(1)); Assert.That(count.served,Is.Zero);
                var service=go.AddComponent<ServiceStation>(); SessionSummary.Record(count,service,true,false);
                Assert.That(count.served,Is.EqualTo(1)); Assert.That(count.plates,Is.EqualTo(1));
            }
            finally {Object.DestroyImmediate(go); Object.DestroyImmediate(recipe);}
        }
    }
}
