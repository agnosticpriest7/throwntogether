using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // Read-only physical allocation. A plate belongs to one order; a loose portion
    // (including one still cooking or in player hands) satisfies one component.
    // Reservations never lock players out of their normal CarrySlots.
    public sealed class CookProduction
    {
        public sealed class Component
        {
            public IngredientDefinition Ingredient;
            public FoodState State;
            public ProcessingRecipe Hot;
            public Carryable Supply;
            public bool OnPlate;
        }
        public sealed class Order
        {
            public KitchenTicket Ticket;
            public Carryable Plate;
            public Component[] Components;
        }
        public static IngredientPortion[] Portions(RecipeDefinition recipe)=>new[]{new IngredientPortion{ingredient=recipe.ingredient,state=recipe.requiredState}}.Concat(recipe.additionalIngredients).ToArray();
        public static ProcessingRecipe HotFor(RecipeDefinition recipe,IngredientDefinition food,FoodState state)=>
            state==FoodState.Cut || state==FoodState.Raw?null:recipe.steps.LastOrDefault(s=>s!=null && s.ingredient==food && s.output==state);
        public static bool Plain(Carryable item,IngredientDefinition food,FoodState state)=>item!=null && item.Payload!=null && !item.Payload.isPlate && !item.Payload.dirty && item.Payload.additions.Count==0 && item.Payload.ingredient==food && item.Payload.state==state;
        public static Interactable Location(Carryable item)
        {
            if(item?.Owner==null)return null;
            var bin=item.Owner.GetComponentInParent<PrepBin>();if(bin!=null)return bin;
            var counter=item.Owner.GetComponentInParent<CounterStation>();
            return counter!=null && counter.slot==item.Owner?counter:null;
        }
        public static Order[] Snapshot(RestaurantDay day,Carryable excluded=null)
        {
            var orders=(day.Expo?.OpenTickets??new KitchenTicket[0]).Select(t=>new Order{Ticket=t,Components=Portions(t.Recipe).Select(p=>new Component{Ingredient=p.ingredient,State=p.state,Hot=HotFor(t.Recipe,p.ingredient,p.state)}).ToArray()}).ToArray();
            var food=Object.FindObjectsByType<Carryable>(FindObjectsSortMode.InstanceID).Where(i=>i!=excluded && i.gameObject.scene==day.gameObject.scene && i.Owner!=null && i.Payload!=null && !i.Payload.dirty && i.Owner.GetComponentInParent<DiningTable>()==null).ToArray();
            var used=new HashSet<Carryable>();
            bool Available(Carryable i,Order o)=>!used.Contains(i) && (day.Expo.BoundTicket(i)==null || day.Expo.BoundTicket(i)==o.Ticket);
            // Full dishes first, across ALL orders: never dismantle their accounting
            // to cover an earlier simple dish with a component of a compound meal.
            foreach(var o in orders)
            {
                o.Plate=food.FirstOrDefault(i=>Available(i,o) && o.Ticket.Recipe.Matches(i.Payload));
                if(o.Plate!=null)used.Add(o.Plate);
            }
            foreach(var o in orders.Where(o=>o.Plate==null))
            {
                o.Plate=food.Where(i=>Available(i,o) && i.Payload.isPlate && i.Payload.IngredientCount>0 && o.Ticket.Recipe.AcceptsPortions(i.Payload)).OrderByDescending(i=>i.Payload.IngredientCount).FirstOrDefault();
                if(o.Plate!=null)used.Add(o.Plate);
            }
            foreach(var o in orders)foreach(var c in o.Components)
            {
                c.OnPlate=o.Plate!=null && o.Plate.Payload.Contains(c.Ingredient,c.State);
                if(c.OnPlate)continue;
                c.Supply=food.FirstOrDefault(i=>Available(i,o) && Plain(i,c.Ingredient,c.State) && !(Location(i) is ProcessingStation p && p.Busy));
                if(c.Supply==null && c.Hot!=null)c.Supply=food.FirstOrDefault(i=>Available(i,o) && Location(i) is ProcessingStation p && p.CurrentProcess==c.Hot);
                if(c.Supply!=null)used.Add(c.Supply);
            }
            return orders;
        }
    }
}
