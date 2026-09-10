using System.Collections.Generic;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class SourceStation : Interactable
    {
        public IngredientDefinition ingredient;
        public Carryable itemPrefab;
        public bool plates;
        public const int PlateCapacity=5;
        private int issued;
        private readonly HashSet<Carryable> pool=new HashSet<Carryable>();
        private readonly Stack<Carryable> returned=new Stack<Carryable>();
        public int CleanPlatesRemaining => PlateCapacity-issued+returned.Count;
        public override string Prompt(ChefController chef)
        {
            if(plates && chef.Hands.Item!=null && chef.Hands.Item.Payload.EmptyPlate && !chef.Hands.Item.Payload.dirty) return "Return clean plate to stack";
            if(chef.Hands.Item!=null) return "Hands full — use a counter";
            if(!plates) return "Take "+ingredient.displayName.ToLowerInvariant();
            return CleanPlatesRemaining>0 ? "Take clean plate ("+CleanPlatesRemaining+" / 5)" : "No clean plates — collect and wash a dirty plate";
        }
        public override bool Interact(ChefController chef)
        {
            if(chef.Hands.Item!=null)
            {
                var held=chef.Hands.Item;
                if(!plates || !held.Payload.EmptyPlate || held.Payload.dirty || !pool.Contains(held))return false;
                chef.Hands.Release();held.transform.SetParent(transform,false);held.gameObject.SetActive(false);returned.Push(held);return true;
            }
            Carryable item;
            if(plates)
            {
                if(returned.Count>0){item=returned.Pop();item.gameObject.SetActive(true);}
                else
                {
                    if(issued>=PlateCapacity)return false;
                    item=Instantiate(itemPrefab);item.Configure(ItemPayload.Plate());issued++;pool.Add(item);
                }
            }
            else {item=Instantiate(itemPrefab);item.Configure(ItemPayload.Food(ingredient));}
            return chef.Hands.TryTake(item);
        }
    }
}
