using NUnit.Framework;
using UnityEngine;
namespace ThrownTogether.Tests
{
    public sealed class RecipeBookTests
    {
        [Test] public void BookIncludesEveryPlayableDishAndCounterPickupInstructions()
        {
            var book=Resources.Load<RecipeBook>("RecipeBook");Assert.That(book,Is.Not.Null);Assert.That(book.recipes.Length,Is.EqualTo(13));
            foreach(var recipe in book.recipes)
            {
                Assert.That(recipe,Is.Not.Null);var text=RecipeBook.Instructions(recipe);
                Assert.That(text,Does.Contain("ordinary counter"));Assert.That(text,Does.Contain("Pick up the finished plate"));Assert.That(text,Does.Contain(recipe.displayName));
            }
        }
        [Test] public void BookReflectsActualHotAndColdRecipeSteps()
        {
            var book=Resources.Load<RecipeBook>("RecipeBook");
            foreach(var recipe in book.recipes)
            {
                var text=RecipeBook.Instructions(recipe);if(recipe.requiredPurchases.Length>0){foreach(var step in recipe.steps)Assert.That(text,Does.Contain(step.duration.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" seconds"));continue;}Assert.That(text,Does.Contain("1.5 seconds"));
                if(recipe.requiredState==FoodState.Cooked) {Assert.That(text,Does.Contain("Fryer:"));Assert.That(text,Does.Contain("5 seconds"));}
                else Assert.That(text,Does.Not.Contain("Fryer:"));
            }
            Assert.That(RecipeBook.Instructions(null),Is.EqualTo("Recipe unavailable."));
        }
    }
}
