using System.Collections.Generic;
using UnityEngine;
namespace ThrownTogether
{
    public sealed class RestaurantAudioFeedback : MonoBehaviour
    {
        public AudioPlayback playback;
        public ChefController chef;
        public ProcessingStation prep,fryer;
        public CustomerOrder order;
        public RestaurantShift shift;
        public AudioCue pickup,place,chop,fryerStart,sizzle,complete,plate,success,uiClick,wash,footstep;
        int completedOrders;OrderPhase phase;float discovery;
        sealed class StationVoice {public Interactable station;public bool busy;public float pulse;public AudioSource loop;}
        readonly List<StationVoice> stations=new List<StationVoice>();
        readonly List<ChefController> stepPositions=new List<ChefController>();
        readonly Dictionary<ChefController,Vector3> footsteps=new Dictionary<ChefController,Vector3>();
        void OnEnable(){if(chef!=null)chef.InteractionSucceeded+=ObserveInteraction;discovery=0;}
        void OnDisable(){if(chef!=null)chef.InteractionSucceeded-=ObserveInteraction;foreach(var s in stations)Stop(s);stations.Clear();footsteps.Clear();}
        void Stop(StationVoice s){if(playback!=null)playback.StopLoop(s.loop);s.loop=null;}
        public void RefreshStations()
        {
            foreach(var station in FindObjectsByType<Interactable>())
                if(station.gameObject.scene==gameObject.scene && (station is ProcessingStation || station is WashingStation) && !stations.Exists(s=>s.station==station))stations.Add(new StationVoice{station=station});
            foreach(var player in FindObjectsByType<ChefController>())if(player.gameObject.scene==gameObject.scene && !footsteps.ContainsKey(player))footsteps[player]=player.transform.position;
        }
        public void ObserveInteraction(Interactable station,bool hadItem,bool plated)
        {
            if(playback==null)return;
            if(station is ProcessingStation processing && processing.Busy)return;
            playback.Play(plated?plate:hadItem?place:pickup);
        }
        void Update()
        {
            if(playback==null)return;
            if((discovery-=Time.unscaledDeltaTime)<=0){discovery=.5f;RefreshStations();}
            foreach(var s in stations)
            {
                if(s.station==null || !s.station.isActiveAndEnabled){Stop(s);continue;}
                var processing=s.station as ProcessingStation;var washing=s.station as WashingStation;
                bool busy=processing!=null?processing.Busy:washing.Busy;
                bool working=processing!=null?processing.Working:washing.Working;
                bool automatic=processing!=null && !processing.requiresAttendance;
                if(busy!=s.busy)
                {
                    if(busy && automatic)playback.Play(fryerStart);
                    if(!busy){Stop(s);playback.Play(complete);}
                    s.busy=busy;s.pulse=0;
                }
                bool audible=working && Time.timeScale>0;
                if(automatic || washing!=null)
                {
                    if(audible && s.loop==null)s.loop=playback.Play(automatic?sizzle:wash);
                    if(s.loop!=null){if(audible)s.loop.UnPause();else s.loop.Pause();}
                }
                else if(audible && (s.pulse-=Time.deltaTime)<=0){s.pulse=.38f;playback.Play(chop);}
            }
            stepPositions.Clear();
            foreach(var entry in footsteps)
            {
                var player=entry.Key;if(player==null)continue;
                float distance=Vector3.Distance(player.transform.position,footsteps[player]);
                if(distance>=.65f){stepPositions.Add(player);if(distance<2 && Time.timeScale>0)playback.Play(footstep);}
            }
            foreach(var player in stepPositions)footsteps[player]=player.transform.position;
            if(shift!=null && completedOrders!=shift.CompletedCount){completedOrders=shift.CompletedCount;playback.Play(success);}
            if(shift==null && order!=null && phase!=order.Phase){phase=order.Phase;if(phase==OrderPhase.Complete)playback.Play(success);}
        }
        public void Click(){if(playback!=null)playback.Play(uiClick);}
    }
}
