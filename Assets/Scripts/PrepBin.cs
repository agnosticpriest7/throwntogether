using System.Collections.Generic;
using UnityEngine;

namespace ThrownTogether
{
    // Physical portions retain their identity and CarrySlot ownership while stored.
    // No recipe/ticket binding: either chef or staff may use the same buffer.
    public sealed class PrepBin : Interactable
    {
        public const int Capacity=5;
        readonly List<CarrySlot> portions=new List<CarrySlot>();
        public int Count { get { Prune(); return portions.Count; } }
        public IngredientDefinition Ingredient => Count==0?null:portions[0].Item.Payload.ingredient;
        public FoodState State => Count==0?FoodState.Cut:portions[0].Item.Payload.state;
        void Prune()
        {
            for(int i=portions.Count-1;i>=0;i--)
                if(portions[i]==null || portions[i].Item==null)
                {if(portions[i]!=null)Destroy(portions[i].gameObject);portions.RemoveAt(i);}
            for(int i=0;i<portions.Count;i++)portions[i].transform.localPosition=Position(i);
        }
        static Vector3 Position(int index)=>new Vector3((index%3-1)*.32f,1.25f,(index/3)*.35f-.15f);
        public bool CanStore(ItemPayload food)
        {
            return food!=null && !food.isPlate && !food.dirty && food.ingredient!=null &&
                food.state==FoodState.Cut && food.additions.Count==0 && Count<Capacity &&
                (Count==0 || Ingredient==food.ingredient && State==food.state);
        }
        public bool Store(Carryable item)
        {
            if(!isActiveAndEnabled || item==null || !CanStore(item.Payload))return false;
            var holder=new GameObject("Prep portion");holder.transform.SetParent(transform,false);
            holder.transform.localPosition=Position(Count);
            var slot=holder.AddComponent<CarrySlot>();
            if(!slot.TryTake(item)){Destroy(holder);return false;}
            portions.Add(slot);return true;
        }
        public bool Take(CarrySlot hands)
        {
            if(!isActiveAndEnabled || hands==null || hands.Item!=null || Count==0)return false;
            var slot=portions[portions.Count-1];
            if(!hands.TryTake(slot.Item))return false;
            portions.RemoveAt(portions.Count-1);Destroy(slot.gameObject);return true;
        }
        public override string Status => "Prep bin — "+Count+" / "+Capacity;
        public override string Prompt(ChefController chef)
        {
            if(chef.Hands.Item==null)return Count==0?"Prep bin empty":"Take "+Ingredient.NameFor(State)+" ("+Count+" / "+Capacity+")";
            if(CanStore(chef.Hands.Item.Payload))return "Store prepared ingredient ("+Count+" / "+Capacity+")";
            return Count>=Capacity?"Prep bin full":Count>0?"One prepared ingredient type per bin":"Prep bin needs chopped ingredients";
        }
        public override bool Interact(ChefController chef)
            => chef.Hands.Item==null?Take(chef.Hands):Store(chef.Hands.Item);
    }
}
