using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThrownTogether
{
    public sealed class LocalCoopSession : MonoBehaviour
    {
        public ChefController playerOne;
        public ChefController chefPrefab;
        public Transform playerTwoSpawn;
        public RestaurantAudioFeedback audioFeedback;
        public ChefController PlayerTwo { get; private set; }
        public Gamepad PlayerOnePad { get; private set; }
        public Gamepad PlayerTwoPad { get; private set; }
        public bool KeyboardPlayerOne { get; private set; }
        public void UseKeyboardPlayerOne()
        {
            if(PlayerTwo!=null) return;
            KeyboardPlayerOne=true; BindPlayerOne(null);
        }
        public string Status => PlayerTwo==null ? "Solo | Press A on a second pad to join" :
            PlayerTwoPad!=null && PlayerTwoPad.added ? "Local co-op | P1 mint / P2 coral" : "P2 disconnected — position and item retained; press A to reconnect";
        private void Start() { BindPlayerOne(Gamepad.all.FirstOrDefault()); }
        public void BindPlayerOne(Gamepad pad)
        {
            PlayerOnePad=pad;
            playerOne.GetComponent<ChefInput>().BindDevices(new InputDevice[]{Keyboard.current,pad}.Where(d=>d!=null).ToArray());
        }
        public bool Join(Gamepad pad)
        {
            if(pad==null || !pad.added || pad==PlayerOnePad || (PlayerTwoPad!=null && PlayerTwoPad.added)) return false;
            if(PlayerTwo==null)
            {
                PlayerTwo=Instantiate(chefPrefab,playerTwoSpawn.position,playerTwoSpawn.rotation);
                PlayerTwo.name="Player 2";
                var block=new MaterialPropertyBlock(); block.SetColor("_BaseColor",new Color(1,.38f,.3f));
                var apron=PlayerTwo.transform.Find("Apron");
                if(apron!=null) apron.GetComponent<Renderer>().SetPropertyBlock(block);
                if(audioFeedback!=null) PlayerTwo.InteractionSucceeded+=audioFeedback.ObserveInteraction;
            }
            PlayerTwoPad=pad;
            var input=PlayerTwo.GetComponent<ChefInput>();
            input.BindDevices(pad); input.enabled=true;
            input.SetInputFocus(false); input.SetInputFocus(true);
            return true;
        }
        private void Update()
        {
            if(playerOne==null) return;
            if(PlayerTwo!=null) PlayerTwo.GetComponent<ChefInput>().enabled=PlayerTwoPad!=null && PlayerTwoPad.added;
            if(!WebInputFocus.HasFocus) return;
            foreach(var pad in Gamepad.all)
            {
                if(!pad.buttonSouth.wasPressedThisFrame || pad==PlayerOnePad || pad==PlayerTwoPad) continue;
                if(!KeyboardPlayerOne && (PlayerOnePad==null || !PlayerOnePad.added)) BindPlayerOne(pad);
                else Join(pad);
            }
        }
        private void OnDestroy()
        {
            if(PlayerTwo!=null && audioFeedback!=null) PlayerTwo.InteractionSucceeded-=audioFeedback.ObserveInteraction;
        }
    }
}
