using System.Linq;
using UnityEngine;
namespace ThrownTogether
{
    // Optional, state-driven guidance uses the same stations and recipes as ordinary play.
    public sealed class PracticeGuide : MonoBehaviour
    {
        private RestaurantHud hud;
        private SourceStation source, plates;
        private ProcessingStation prep, fryer;
        private CounterStation[] counters;
        private ServiceStation service;
        public bool Active {get;private set;}
        public string Instruction {get;private set;}="";
        public Interactable SuggestedTarget {get;private set;}
        private void Start()
        {
            hud=GetComponent<RestaurantHud>();
            if(hud.shift!=null || SessionOptions.Training=="Free practice") return;
            var stations=Interactable.Active.Where(s=>s.gameObject.scene==gameObject.scene).ToArray();
            source=stations.OfType<SourceStation>().First(s=>!s.plates && s.Offers(hud.order.recipe.ingredient)); plates=stations.OfType<SourceStation>().First(s=>s.plates);
            prep=stations.OfType<ProcessingStation>().First(s=>s.recipe.input==FoodState.Raw);
            fryer=stations.OfType<ProcessingStation>().First(s=>s.recipe.input==FoodState.Cut);
            counters=stations.OfType<CounterStation>().Where(s=>!(s is ProcessingStation)).ToArray(); service=stations.OfType<ServiceStation>().First();
            Active=true;
            if(SessionOptions.Training!="Guided full loop" && SessionOptions.Training!="Garden salad")
            {
                Carryable item;
                if(SessionOptions.Training=="Serving") { plates.Interact(hud.chef); item=hud.chef.Hands.Item; }
                else item=Instantiate(source.itemPrefab);
                var payload=ItemPayload.Food(hud.order.recipe.ingredient);
                if(SessionOptions.Training=="Frying") payload.state=FoodState.Cut;
                if(SessionOptions.Training=="Plating" || SessionOptions.Training=="Serving") payload.state=FoodState.Cooked;
                if(SessionOptions.Training=="Serving") payload.isPlate=true;
                item.Configure(payload); hud.chef.Hands.TryTake(item);
            }
        }
        private void LateUpdate()
        {
            if(!Active) return;
            SuggestedTarget=null;
            if(hud.order.Phase!=OrderPhase.Waiting)
            { Instruction=hud.order.Phase==OrderPhase.Complete ? "First service complete! Y / Esc opens your results and practice choices." : "Your dish is on its way to the customer."; return; }
            if(hud.order.recipe.additionalIngredients.Length>0) {GardenAdvice();return;}
            var item=hud.chef.Hands.Item;
            if(item!=null)
            {
                var p=item.Payload;
                if(p.isPlate && !p.EmptyPlate) Hint(service,"Serve: take your plated dish to the pass with the bell. Press A / E.");
                else if(p.EmptyPlate)
                {
                    var cooked=counters.FirstOrDefault(c=>c.slot.Item!=null && ItemPayload.CanPlate(p,c.slot.Item.Payload));
                    Hint(cooked ?? counters.FirstOrDefault(c=>c.slot.Item==null),"Plate: put the plate on an ordinary counter with prepared food. Either order works.");
                }
                else if(p.state==FoodState.Raw) Hint(prep,"Prep: face the wooden board, press A / E, stay still while cutting, then collect it when ready.");
                else if(p.state==FoodState.Cut && p.ingredient.platingState==FoodState.Cooked) Hint(fryer,"Fry: put the cut ingredient in the basket. Wait for the green ready light, then collect it.");
                else Hint(counters.FirstOrDefault(c=>c.slot.Item!=null && c.slot.Item.Payload.EmptyPlate) ?? counters.FirstOrDefault(c=>c.slot.Item==null),"Plate: put prepared food on any empty counter, then add a clean plate. Garden salad needs no frying.");
                return;
            }
            foreach(var c in counters)
                if(c.slot.Item!=null && c.slot.Item.Payload.isPlate && !c.slot.Item.Payload.EmptyPlate) { Hint(c,"Pick up your finished plate from the counter with A / E, then serve it."); return; }
            if(fryer.slot.Item!=null) { Hint(fryer,fryer.Busy ? "Frying... the green light means ready. There is no burn timer." : "Collect the cooked food with A / E."); return; }
            if(prep.slot.Item!=null) { Hint(prep,prep.Busy ? (prep.Working ? "Chopping... stay still until ready." : "Press A / E to resume chopping, then stay still.") : "Collect the cut ingredient with A / E."); return; }
            foreach(var c in counters)
                if(c.slot.Item!=null && !c.slot.Item.Payload.EmptyPlate)
                {
                    if(ItemPayload.CanPlate(ItemPayload.Plate(),c.slot.Item.Payload)) Hint(plates,"Take a clean plate from the stack, then return to your prepared food.");
                    else Hint(c,"Collect the ingredient you left on this counter to continue."); return;
                }
            Hint(source,"Choose "+hud.order.recipe.ingredient.displayName+" at Produce Rack. Face the mint border, then press A / E.");
        }
        private void GardenAdvice()
        {
            var recipe=hud.order.recipe;var chef=hud.chef;var held=chef.Hands.Item;
            var plated=counters.FirstOrDefault(c=>c.slot.Item!=null && c.slot.Item.Payload.isPlate);
            if(held!=null)
            {
                var p=held.Payload;
                if(recipe.Matches(p)) {Hint(service,"Serve the Garden Salad at the bell pass.");return;}
                if(p.isPlate) {Hint(counters.FirstOrDefault(c=>c.slot.Item==null),"Put this plate on an empty counter. Add both sliced tomato and chopped lettuce.");return;}
                if(p.state==FoodState.Raw) {Hint(prep,"Press A / E at the prep board, stay still while chopping, then pick up the ingredient.");return;}
                Hint(plated ?? counters.FirstOrDefault(c=>c.slot.Item==null),"Add the prepared ingredient to the plate on the counter. Garden Salad needs tomato AND lettuce.");return;
            }
            if(prep.slot.Item!=null) {Hint(prep,prep.Busy ? (prep.Working ? "Chopping... stay still." : "Press A / E to resume chopping; progress is saved.") : "Pick up the chopped ingredient.");return;}
            if(plated!=null && recipe.Matches(plated.slot.Item.Payload)) {Hint(plated,"Pick up your finished plate, then serve Garden Salad.");return;}
            var loose=counters.FirstOrDefault(c=>c.slot.Item!=null && !c.slot.Item.Payload.isPlate);
            if(loose!=null) {Hint(plated==null ? (Interactable)plates:loose,plated==null ? "Take a clean plate and add it to the prepared food." : "Pick up the prepared food and add it to the plate.");return;}
            var needed=recipe.ingredient;
            if(plated!=null && plated.slot.Item.Payload.Contains(recipe.ingredient,recipe.requiredState)) needed=recipe.additionalIngredients[0].ingredient;
            var next=Interactable.Active.OfType<SourceStation>().First(s=>s.gameObject.scene==gameObject.scene && !s.plates && s.Offers(needed));
            Hint(next,"Take "+needed.displayName+", chop it, then add it to the salad plate. No frying.");
        }
        private void Hint(Interactable target,string instruction) { SuggestedTarget=target; Instruction=instruction; }
    }
}
