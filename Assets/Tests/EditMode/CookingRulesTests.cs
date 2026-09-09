using NUnit.Framework;
using UnityEditor;

namespace ThrownTogether.Tests
{
    public class CookingRulesTests
    {
        [TestCase(FoodState.Raw,false)]
        [TestCase(FoodState.Cut,true)]
        [TestCase(FoodState.Cooked,false)]
        public void GardenSaladRequiresBothPreparedIngredients(FoodState state,bool accepted)
        {
            var tomato=Data<IngredientDefinition>("Tomato");var item=ItemPayload.Food(tomato);item.state=state;
            Assert.That(ItemPayload.CanPlate(ItemPayload.Plate(),item),Is.EqualTo(accepted));
            item.isPlate=true;Assert.That(Data<RecipeDefinition>("TomatoSalad").Matches(item),Is.False,"Tomato alone is incomplete");
            item.additions.Add(new IngredientPortion{ingredient=Data<IngredientDefinition>("Lettuce"),state=FoodState.Cut});
            Assert.That(Data<RecipeDefinition>("TomatoSalad").Matches(item),Is.EqualTo(accepted));
            item.isPlate=false;
            Assert.That(Data<ProcessingRecipe>("FryPotato").Accepts(item),Is.False);
            Assert.That(Data<ProcessingRecipe>("FryMushrooms").Accepts(item),Is.False);
        }
        [Test]
        public void CarryablePrefabReferencesEveryRuntimeVisualDependency()
        {
            var item=AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/VerticalSlice/Carryable.prefab").GetComponent<Carryable>();
            Assert.That(item.sphereMesh,Is.Not.Null);
            Assert.That(item.cubeMesh,Is.Not.Null);
            Assert.That(item.cylinderMesh,Is.Not.Null);
            Assert.That(item.visualShader,Is.Not.Null);
        }
        [Test] public void GardenSaladAcceptsEitherAssemblyOrderAndRejectsDuplicatesAndDirtyPlates()
        {
            var tomato=Data<IngredientDefinition>("Tomato");var lettuce=Data<IngredientDefinition>("Lettuce");var recipe=Data<RecipeDefinition>("TomatoSalad");
            foreach(bool reverse in new[]{false,true})
            {
                var first=ItemPayload.Food(reverse ? lettuce:tomato);first.state=FoodState.Cut;
                var second=ItemPayload.Food(reverse ? tomato:lettuce);second.state=FoodState.Cut;
                var plate=ItemPayload.Plate();Assert.That(ItemPayload.CanPlate(plate,first),Is.True);plate.AddFood(first);
                Assert.That(recipe.Matches(plate),Is.False);Assert.That(ItemPayload.CanPlate(plate,first),Is.False,"No duplicate ingredient");
                second.state=FoodState.Raw;Assert.That(ItemPayload.CanPlate(plate,second),Is.False);second.state=FoodState.Cut;
                Assert.That(ItemPayload.CanPlate(plate,second),Is.True);plate.AddFood(second);Assert.That(recipe.Matches(plate),Is.True);
                plate.MakeDirty();Assert.That(recipe.Matches(plate),Is.False);Assert.That(ItemPayload.CanPlate(plate,first),Is.False);Assert.That(plate.IngredientCount,Is.Zero);
            }
        }
        private T Data<T>(string name) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/Data/VerticalSlice/"+name+".asset");
        [Test]
        public void CatalogMigrationPreservesRecipeAndApplianceCompatibility()
        {
            var fries=Data<RecipeDefinition>("Fries"); var prep=Data<ProcessingRecipe>("PrepPotato"); var fry=Data<ProcessingRecipe>("FryPotato");
            Assert.That(fries.id,Is.EqualTo("recipe.fries"));
            Assert.That(fries.displayName,Is.EqualTo("Fries"));
            Assert.That(fries.steps,Is.EqualTo(new[] { prep,fry }));
            Assert.That(Data<ApplianceDefinition>("Fryer").Supports(fry),Is.True);
            Assert.That(Data<ApplianceDefinition>("Fryer").Supports(prep),Is.False);
            Assert.That(Data<ApplianceDefinition>("PrepStation").Supports(prep),Is.True);
            Assert.That(prep.duration,Is.EqualTo(1.5f)); Assert.That(fry.duration,Is.EqualTo(5f));
        }
        [TestCase(FoodState.Raw,false)]
        [TestCase(FoodState.Cut,false)]
        [TestCase(FoodState.Cooked,true)]
        public void PlatingAndOrderRequireCookedPotato(FoodState state,bool accepted)
        {
            var food=ItemPayload.Food(Data<IngredientDefinition>("Potato")); food.state=state;
            Assert.That(ItemPayload.CanPlate(ItemPayload.Plate(),food),Is.EqualTo(accepted));
            food.isPlate=true;
            Assert.That(Data<RecipeDefinition>("Fries").Matches(food),Is.EqualTo(accepted));
        }
        [Test]
        public void RecipeRejectsAnotherIngredientAndMissingDefinition()
        {
            var other=UnityEngine.ScriptableObject.CreateInstance<IngredientDefinition>();
            var empty=UnityEngine.ScriptableObject.CreateInstance<RecipeDefinition>();
            try
            {
                var item=ItemPayload.Food(other); item.state=FoodState.Cooked; item.isPlate=true;
                Assert.That(Data<RecipeDefinition>("Fries").Matches(item),Is.False);
                Assert.That(empty.Matches(ItemPayload.Plate()),Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(empty); }
        }
        [Test]
        public void AuthoredPotatoProgressionUsesTwoDistinctTimedRecipes()
        {
            var potato=Data<IngredientDefinition>("Potato"); var prep=Data<ProcessingRecipe>("PrepPotato"); var fryer=Data<ProcessingRecipe>("FryPotato");
            Assert.That(potato,Is.Not.Null); Assert.That(prep,Is.Not.Null); Assert.That(fryer,Is.Not.Null);
            var item=ItemPayload.Food(potato);
            Assert.That(prep.Accepts(item),Is.True); Assert.That(fryer.Accepts(item),Is.False,"Raw potato must not enter fryer");
            item.state=prep.output; Assert.That(item.state,Is.EqualTo(FoodState.Cut)); Assert.That(fryer.Accepts(item),Is.True);
            item.state=fryer.output; Assert.That(item.state,Is.EqualTo(FoodState.Cooked)); Assert.That(fryer.Accepts(item),Is.False);
            Assert.That(prep.duration,Is.InRange(1f,2f)); Assert.That(fryer.duration,Is.InRange(4f,6f));
        }
        [Test]
        public void OrderRequiresCorrectCookedFoodOnPlate()
        {
            var dish=Data<DishRecipe>("Fries"); var food=ItemPayload.Food(Data<IngredientDefinition>("Potato")); var plate=ItemPayload.Plate();
            Assert.That(ItemPayload.CanPlate(plate,food),Is.False);
            Assert.That(dish.Matches(plate),Is.False);
            food.state=FoodState.Cooked; Assert.That(dish.Matches(food),Is.False); Assert.That(ItemPayload.CanPlate(plate,food),Is.True);
            plate.ingredient=food.ingredient; plate.state=food.state;
            Assert.That(dish.Matches(plate),Is.True); Assert.That(ItemPayload.CanPlate(plate,food),Is.False,"No double plating");
            plate.state=FoodState.Raw; Assert.That(dish.Matches(plate),Is.False);
            plate.ingredient=null; Assert.That(dish.Matches(plate),Is.False);
        }
    }
}
