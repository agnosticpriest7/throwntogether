using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class MenuProgressionTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public bool fail;public string Read()=>json;public void Write(string s){if(fail)throw new System.IO.IOException();json=s;}}
        [Test] public void NewRestaurantRequiresThreeUnlockedChoicesAndPurchaseExpandsPool()
        {
            var memory=new Memory();var a=new RestaurantAccount(memory);
            Assert.That(DailyMenu.CanStart(a),Is.False);
            var basic=DailyMenu.Catalog.Where(r=>r.Unlocked(a)).ToArray();Assert.That(basic.Length,Is.EqualTo(3));
            foreach(var r in basic.Take(2))Assert.That(DailyMenu.Toggle(a,r),Is.True);
            Assert.That(DailyMenu.CanStart(a),Is.False);Assert.That(DailyMenu.StartProblem(a),Does.Contain("2 / 3"));
            Assert.That(DailyMenu.Toggle(a,basic[2]),Is.True);Assert.That(DailyMenu.CanStart(a),Is.True);
            var egg=DailyMenu.Catalog.Single(r=>r.id=="dish.fried-egg");Assert.That(DailyMenu.Toggle(a,egg),Is.False);Assert.That(egg.LockReason,Is.EqualTo("Requires Griddle"));
            int day=a.StartDay();Assert.That(a.Settle(day,500,0),Is.True);
            Assert.That(a.Buy("griddle",125),Is.True);Assert.That(DailyMenu.Catalog.Count(r=>r.Unlocked(a)),Is.EqualTo(7));
            Assert.That(DailyMenu.Toggle(a,egg),Is.True);Assert.That(a.Buy("grill",150),Is.True);
            var reload=new RestaurantAccount(memory);Assert.That(reload.Data.cash,Is.EqualTo(225));Assert.That(DailyMenu.Resolve(reload).Length,Is.EqualTo(4));
            Assert.That(DailyMenu.Catalog.All(r=>r.Unlocked(reload)),Is.True);
            var before=reload.Data.selectedMenu.ToArray();memory.fail=true;Assert.That(DailyMenu.Toggle(reload,egg),Is.False);Assert.That(reload.Data.selectedMenu,Is.EqualTo(before));
        }
        [Test] public void OldSchemaOneSaveMigratesOnlyStartingMenuAndFutureSchemaIsPreserved()
        {
            var memory=new Memory{json="{\"schemaVersion\":1,\"cash\":80,\"nextDay\":2,\"activeDay\":1,\"settledDay\":1,\"completedDays\":1,\"purchases\":[\"grill\"]}"};
            var a=new RestaurantAccount(memory);Assert.That(a.Writable,Is.True);Assert.That(a.Owns("grill"),Is.True);Assert.That(DailyMenu.Resolve(a).Length,Is.EqualTo(3));
            memory.json=memory.json.Replace("\"schemaVersion\":1","\"schemaVersion\":99");string original=memory.json;var future=new RestaurantAccount(memory);
            Assert.That(future.SetMenu(new[]{"dish.fries"}),Is.False);Assert.That(memory.json,Is.EqualTo(original));
        }
        [TestCase(3,3,200,0)][TestCase(4,3,200,0)][TestCase(4,4,200,10)][TestCase(13,4,1000,15)][TestCase(4,4,19,0)]
        public void VarietyRewardsDistinctDishesAndHasASmallCap(int menu,int served,int income,int expected)=>Assert.That(DailyMenu.VarietyRevenue(menu,served,income),Is.EqualTo(expected));
        [Test] public void EveryRecipeHasReachableProcessesAndAnyAssemblyOrderMatchesExactly()
        {
            var book=DailyMenu.Catalog;Assert.That(book.Length,Is.EqualTo(13));Assert.That(book.Select(r=>r.id).Distinct().Count(),Is.EqualTo(book.Length));
            foreach(var r in book)
            {
                var portions=new[]{new IngredientPortion{ingredient=r.ingredient,state=r.requiredState}}.Concat(r.additionalIngredients).ToArray();
                foreach(var part in portions)
                {
                    FoodState state=FoodState.Raw;
                    foreach(var step in r.steps.Where(s=>s.ingredient==part.ingredient)){Assert.That(step.input,Is.EqualTo(state),r.displayName);state=step.output;}
                    Assert.That(state,Is.EqualTo(part.state),r.displayName);Assert.That(part.ingredient.Ready(state),Is.True);
                }
                foreach(var order in new[]{portions,portions.Reverse().ToArray()})
                {
                    var plate=ItemPayload.Plate();
                    foreach(var part in order){var food=new ItemPayload{ingredient=part.ingredient,state=part.state};Assert.That(ItemPayload.CanPlate(plate,food),Is.True,r.displayName);plate.AddFood(food);}
                    Assert.That(r.Matches(plate),Is.True,r.displayName);Assert.That(book.Count(d=>d.Matches(plate)),Is.EqualTo(1),"Dish states must be unambiguous");
                    Assert.That(ItemPayload.CanPlate(plate,new ItemPayload{ingredient=order[0].ingredient,state=order[0].state}),Is.False);
                    plate.MakeDirty();Assert.That(r.Matches(plate),Is.False);
                }
            }
            var chicken=book.Single(r=>r.id=="dish.grilled-chicken");Assert.That(book.Single(r=>r.id=="dish.chicken-fries").salePrice,Is.GreaterThan(chicken.salePrice));
        }
        [Test] public void GriddleAndGrillRejectEachOthersInputAndWrongPreparation()
        {
            var offers=Resources.Load<DayServiceDefinition>("ServiceDay").purchases;
            var grill=offers.Single(o=>o.id=="grill").stationPrefab.GetComponent<ProcessingStation>().appliance;
            var griddle=offers.Single(o=>o.id=="griddle").stationPrefab.GetComponent<ProcessingStation>().appliance;
            var chicken=DailyMenu.Catalog.Single(r=>r.id=="dish.grilled-chicken").ingredient;
            var hash=DailyMenu.Catalog.Single(r=>r.id=="dish.hash-browns").ingredient;
            Assert.That(griddle.supportedProcesses.Any(p=>p.Accepts(ItemPayload.Food(chicken))),Is.False);
            Assert.That(grill.supportedProcesses.Any(p=>p.Accepts(ItemPayload.Food(chicken))),Is.True);
            Assert.That(griddle.supportedProcesses.Any(p=>p.Accepts(ItemPayload.Food(hash))),Is.False);
            Assert.That(griddle.supportedProcesses.Any(p=>p.Accepts(new ItemPayload{ingredient=hash,state=FoodState.Cut})),Is.True);
        }
    }
}
