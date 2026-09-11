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
            if(plates && ItemPayload.CanPlate(ItemPayload.Plate(),chef.Hands.Item?.Payload)) return CleanPlatesRemaining>0 ? "Plate food in hands" : "No clean plates — wash a dirty plate";
            if(chef.Hands.Item!=null) return "Hands full — use a counter";
            if(!plates) return "Take "+ingredient.displayName.ToLowerInvariant();
            return CleanPlatesRemaining>0 ? "Take clean plate ("+CleanPlatesRemaining+" / 5)" : "No clean plates — collect and wash a dirty plate";
        }
        public override bool Interact(ChefController chef)
        {
            if(chef.Hands.Item!=null)
            {
                var held=chef.Hands.Item;
                if(plates && ItemPayload.CanPlate(ItemPayload.Plate(),held.Payload))
                {
                    var plate=TakeCleanPlate();if(plate==null)return false;
                    plate.Payload.AddFood(held.Payload);chef.Hands.Release();Destroy(held.gameObject);
                    plate.RefreshVisual();return chef.Hands.TryTake(plate);
                }
                return ReturnCleanPlate(held);
            }
            Carryable item;
            if(plates)
            {
                item=TakeCleanPlate();
            }
            else {item=Instantiate(itemPrefab);item.Configure(ItemPayload.Food(ingredient));}
            return chef.Hands.TryTake(item);
        }
        public Carryable TakeCleanPlate()
        {
            if(!plates)return null;
            if(returned.Count>0){var item=returned.Pop();item.gameObject.SetActive(true);return item;}
            if(issued>=PlateCapacity)return null;
            var created=Instantiate(itemPrefab);created.Configure(ItemPayload.Plate());issued++;pool.Add(created);return created;
        }
        public bool ReturnCleanPlate(Carryable item)
        {
            if(!plates || item==null || !item.Payload.EmptyPlate || item.Payload.dirty || !pool.Contains(item) || returned.Contains(item))return false;
            item.Owner?.Release();item.transform.SetParent(transform,false);item.gameObject.SetActive(false);returned.Push(item);return true;
        }
    }
}
