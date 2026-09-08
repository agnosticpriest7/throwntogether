using UnityEngine;

namespace ThrownTogether
{
    public sealed class RestaurantAudioFeedback : MonoBehaviour
    {
        public AudioPlayback playback;
        public ChefController chef;
        public ProcessingStation prep, fryer;
        public CustomerOrder order;
        public AudioCue pickup, place, chop, fryerStart, sizzle, complete, plate, success, uiClick;
        private bool prepBusy, fryerBusy;
        private OrderPhase phase;
        private AudioSource frying;
        private void OnEnable() { if(chef != null) chef.InteractionSucceeded+=Interaction; }
        private void OnDisable()
        {
            if(chef != null) chef.InteractionSucceeded-=Interaction;
            if(frying != null) frying.Stop();
            prepBusy=false; fryerBusy=false;
        }
        private void Interaction(Interactable station, bool hadItem, bool plated)
        {
            if(playback == null) return;
            if(station == prep && prep.Busy || station == fryer && fryer.Busy) return;
            playback.Play(plated ? plate : hadItem ? place : pickup);
        }
        private void Update()
        {
            if(playback == null) return;
            if(prep != null && prep.Busy != prepBusy)
            { prepBusy=prep.Busy; playback.Play(prepBusy ? chop : complete); }
            if(fryer != null && fryer.Busy != fryerBusy)
            {
                fryerBusy=fryer.Busy;
                if(fryerBusy) { playback.Play(fryerStart); frying=playback.Play(sizzle); }
                else { if(frying != null) frying.Stop(); frying=null; playback.Play(complete); }
            }
            if(order != null && phase != order.Phase)
            { phase=order.Phase; if(phase == OrderPhase.Complete) playback.Play(success); }
        }
        public void Click() { if(playback != null) playback.Play(uiClick); }
    }
}
