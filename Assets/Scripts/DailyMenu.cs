using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    public static class DailyMenu
    {
        public const int Minimum=3;
        public static RecipeDefinition[] Catalog => Resources.Load<RecipeBook>("RecipeBook").recipes;
        public static RecipeDefinition[] Resolve(RestaurantAccount account) => Catalog.Where(r=>account.Data.selectedMenu.Contains(r.id) && r.Unlocked(account)).ToArray();
        public static bool CanStart(RestaurantAccount account) => Resolve(account).Length>=Minimum;
        public static string StartProblem(RestaurantAccount account) => CanStart(account)?"Ready to start":"Select at least 3 unlocked dishes ("+Resolve(account).Length+" / 3)";
        public static int VarietyRevenue(int menuCount,int distinctServed,int baseIncome) => menuCount>=4 && distinctServed>=4 ? Mathf.Min(15,Mathf.Max(0,baseIncome)/20):0;
        public static bool Toggle(RestaurantAccount account,RecipeDefinition recipe)
        {
            if(recipe==null || !recipe.Unlocked(account))return false;
            var selected=account.Data.selectedMenu.ToList();if(!selected.Remove(recipe.id))selected.Add(recipe.id);
            return account.SetMenu(selected.ToArray());
        }
        public static bool CanAssemble(ItemPayload plate,ItemPayload food)
        {
            if(plate.ContainsIngredient(food.ingredient))return false;
            return Catalog.Any(r=>r.AcceptsPortions(plate) && r.HasPortion(food.ingredient,food.state));
        }
    }
}
