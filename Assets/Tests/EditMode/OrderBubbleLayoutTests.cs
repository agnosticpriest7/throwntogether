using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class OrderBubbleLayoutTests
    {
        [Test] public void EveryRecipeHasItsOwnRenderedCompletedDishIcon()
        {
            foreach(var recipe in Resources.Load<RecipeBook>("RecipeBook").recipes){var image=FoodIcon.DishImage(recipe);Assert.That(image,Is.Not.Null,recipe.displayName);Assert.That(image.width,Is.EqualTo(192));}
            var produce=UnityEditor.AssetDatabase.LoadAssetAtPath<IngredientStorageDefinition>("Assets/Data/MenuExpansion/ProduceRack.asset");
            CollectionAssert.AreEqual(new[]{IngredientVisualKind.Lettuce,IngredientVisualKind.Mushroom,IngredientVisualKind.Potato,IngredientVisualKind.Tomato},System.Array.ConvertAll(produce.ingredients,i=>i.visualKind));
        }
        [Test] public void CardsStayInsideHudSafeAreaAtEveryTextSize()
        {
            foreach(float scale in new[]{1f,1.15f,1.3f})foreach(var p in new[]{new Vector2(-100,-100),new Vector2(940,120),new Vector2(940,430),new Vector2(1600,900)})
            {
                var r=OrderBubbleLayout.ForSeat(p,scale);Assert.That(r.xMin,Is.GreaterThanOrEqualTo(12));Assert.That(r.xMax,Is.LessThanOrEqualTo(1268));Assert.That(r.yMin,Is.GreaterThanOrEqualTo(76));Assert.That(r.yMax,Is.LessThanOrEqualTo(612));
            }
        }
        [Test] public void ServedOrdersHideImmediatelyAndNewOrdersReappear()
        {
            var root=new GameObject("Order");var dish=new GameObject("Dish");var recipe=ScriptableObject.CreateInstance<RecipeDefinition>();
            try
            {
                var ticket=root.AddComponent<CustomerOrder>();ticket.tableSlot=root.AddComponent<CarrySlot>();ticket.ResetOrder(recipe);
                Assert.That(OrderBubbleLayout.Visible(ticket),Is.True);
                ticket.Receive(dish.AddComponent<Carryable>());
                Assert.That(ticket.Phase,Is.EqualTo(OrderPhase.Eating));Assert.That(OrderBubbleLayout.Visible(ticket),Is.False);
                ticket.Advance(3);Assert.That(OrderBubbleLayout.Visible(ticket),Is.False);
                ticket.ResetOrder(recipe);Assert.That(OrderBubbleLayout.Visible(ticket),Is.True);
                ticket.ResetOrder(null);Assert.That(OrderBubbleLayout.Visible(ticket),Is.False);
                var size=OrderBubbleLayout.ForSeat(Vector2.zero,1).size;Assert.That(size.x*size.y,Is.LessThan(206*109*.25f));
            }
            finally{Object.DestroyImmediate(dish);Object.DestroyImmediate(root);Object.DestroyImmediate(recipe);}
        }
    }
}
