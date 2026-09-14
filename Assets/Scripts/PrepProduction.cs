using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // A service-local projection, not another ticket authority. Each real portion is
    // allocated at most once, including food in human hands and on attended boards.
    public sealed class PrepProduction
    {
        readonly RestaurantDay day;
        public PrepWorkLedger Ledger {get;}=new PrepWorkLedger();
        public IngredientDefinition[] Needed {get;private set;}=new IngredientDefinition[0];
        readonly Dictionary<string,int> deficits=new Dictionary<string,int>();
        public PrepProduction(RestaurantDay day){this.day=day;}
        public int Deficit(string key)=>deficits.TryGetValue(key,out var n)?n:0;
        public void Refresh()
        {
            deficits.Clear();var ingredients=new List<IngredientDefinition>();
            var allocated=new HashSet<(Carryable,IngredientDefinition)>();
            var food=Object.FindObjectsByType<Carryable>(FindObjectsSortMode.None)
                .Where(i=>i.gameObject.scene==day.gameObject.scene && i.Owner!=null && i.Payload!=null && !i.Payload.dirty &&
                    i.Owner.GetComponentInParent<DiningTable>()==null &&
                    !(day.PrepCook!=null && day.PrepCook.ReservedItem==i)).ToArray();
            foreach(var ticket in day.Expo?.OpenTickets??new KitchenTicket[0])
            {
                if(ticket.State!=KitchenTicketState.Active && ticket.State!=KitchenTicketState.Ready)continue;
                var recipe=ticket.Recipe;
                foreach(var ingredient in recipe.steps.Where(p=>p!=null && p.input==FoodState.Raw && p.output==FoodState.Cut && p.ingredient!=null).Select(p=>p.ingredient).Distinct())
                {
                    if(string.IsNullOrWhiteSpace(ingredient.id))continue;
                    if(!ingredients.Contains(ingredient)){ingredients.Add(ingredient);deficits[ingredient.id]=0;}
                    bool supplied=false;
                    var finalState=recipe.ingredient==ingredient?recipe.requiredState:recipe.additionalIngredients.First(p=>p.ingredient==ingredient).state;
                    // Use dish-specific finished food before flexible chopped/raw stock.
                    // Otherwise fries could consume the only cut potato while ignoring
                    // existing fries, causing needless work for a hash-brown ticket.
                    foreach(var item in food.OrderBy(i=>i.Payload.Contains(ingredient,finalState)?0:i.Payload.Contains(ingredient,FoodState.Cut)?1:2))
                    {
                        var identity=(item,ingredient);
                        if(allocated.Contains(identity))continue;
                        var bound=day.Expo.BoundTicket(item);
                        if(bound!=null && bound!=ticket)continue;
                        var payload=item.Payload;
                        if(payload.isPlate && !recipe.AcceptsPortions(payload))continue;
                        bool compatible=payload.Contains(ingredient,FoodState.Cut) || payload.Contains(ingredient,FoodState.Raw) ||
                            (payload.ingredient==ingredient && recipe.HasPortion(ingredient,payload.state)) ||
                            payload.additions.Any(p=>p.ingredient==ingredient && recipe.HasPortion(ingredient,p.state));
                        if(!compatible)continue;
                        allocated.Add(identity);supplied=true;break;
                    }
                    if(!supplied)deficits[ingredient.id]++;
                }
            }
            Needed=ingredients.ToArray();Ledger.SetDeficits(deficits);
        }
    }
}
