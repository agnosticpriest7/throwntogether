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
        public string LastEvent { get; private set; }="Solo ready";
        private bool p2Connected;
        private static string DeviceName(Gamepad pad) => pad.displayName.Length>36 ? pad.displayName.Substring(0,36)+"…" : pad.displayName;
        public string DeviceSummary => "P1: "+(PlayerOnePad!=null && PlayerOnePad.added ? DeviceName(PlayerOnePad)+" + keyboard":"Keyboard (no assigned pad)")+
            "\nP2: "+(PlayerTwo==null ? "Not joined — resume and press A on an unused pad" : PlayerTwoPad!=null && PlayerTwoPad.added ? DeviceName(PlayerTwoPad) : "Disconnected — item retained")+"\n"+LastEvent;
        public void UseKeyboardPlayerOne()
        {
            if(PlayerTwo!=null) return;
            KeyboardPlayerOne=true; BindPlayerOne(null);
        }
        public string Status => PlayerTwo==null ? "Solo | Press A on a second pad to join" :
            PlayerTwoPad!=null && PlayerTwoPad.added ? "Local co-op | P1 mint / P2 coral" : "P2 disconnected — position and item retained; press A to reconnect";
        private void Start() { BindPlayerOne(Gamepad.all.FirstOrDefault()); }
        public void RefreshPlayerOneAssignment()
        {
            if(!KeyboardPlayerOne && (PlayerOnePad==null || !PlayerOnePad.added))
                BindPlayerOne(Gamepad.all.FirstOrDefault(p=>p!=PlayerTwoPad));
        }
        public void BindPlayerOne(Gamepad pad)
        {
            PlayerOnePad=pad;
            playerOne.GetComponent<ChefInput>().BindDevices(new InputDevice[]{Keyboard.current,pad}.Where(d=>d!=null).ToArray());
        }
        public bool Join(Gamepad pad)
        {
            if(RestaurantMenu.GameplayBlocked || pad==null || !pad.added || pad==PlayerOnePad || (PlayerTwoPad!=null && PlayerTwoPad.added)) return false;
            bool reconnect=PlayerTwo!=null;
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
            p2Connected=true; LastEvent=reconnect ? "P2 reconnected — chef and held item retained" : "P2 joined — A / E uses the focused station";
            return true;
        }
        public bool LeavePlayerTwo()
        {
            if(PlayerTwo==null || PlayerTwo.Hands.Item!=null) return false;
            if(audioFeedback!=null) PlayerTwo.InteractionSucceeded-=audioFeedback.ObserveInteraction;
            PlayerTwo.gameObject.SetActive(false); Destroy(PlayerTwo.gameObject);
            PlayerTwo=null; PlayerTwoPad=null; p2Connected=false; LastEvent="P2 left — solo play continues"; return true;
        }
        private void Update()
        {
            if(playerOne==null) return;
            if(PlayerTwo!=null) {
                bool connected=PlayerTwoPad!=null && PlayerTwoPad.added;
                PlayerTwo.GetComponent<ChefInput>().enabled=connected;
                if(p2Connected!=connected) LastEvent=connected ? "P2 reconnected" : "P2 disconnected — chef and held item retained";
                p2Connected=connected;
            }
            if(RestaurantMenu.GameplayBlocked || !WebInputFocus.HasFocus) return;
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
