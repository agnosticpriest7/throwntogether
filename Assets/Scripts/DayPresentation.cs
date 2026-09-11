using System;
using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // Scene-local presentation only; RestaurantDay and RestaurantMenu retain all economic authority.
    public sealed class DayPresentation : MonoBehaviour
    {
        public enum Phase { Service, Closing, Night, LeavingManagement, Opening }
        public const float TransitionSeconds=2.5f;
        public const float CameraPullback=1.125f;
        public Phase Current { get; private set; }
        public bool Transitioning => Current==Phase.Closing || Current==Phase.Opening || Current==Phase.LeavingManagement;
        public float NightAmount { get; private set; }
        public float ServiceOpacity => Current==Phase.Closing ? 1-NightAmount : Current==Phase.Opening ? 1-NightAmount : 1;
        public float MenuOpacity => Current==Phase.LeavingManagement ? 1-Ease(elapsed/.35f) : Current==Phase.Night ? Ease(elapsed/.35f) : 1;
        public bool NightMenu => Current==Phase.Night || Current==Phase.LeavingManagement;
        private static bool openingAfterLoad;
        private RestaurantHud hud;
        private Camera cameraView;
        private float normalSize, elapsed, savedScale=1;
        private bool ownsPause;
        private Action loadNextDay;
        private Color sky, equator, ground, background;
        private LightState[] lights;
        private sealed class LightState { public Light light; public Color color; public float intensity; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPending() => openingAfterLoad=false;
        private void Awake()
        {
            hud=GetComponent<RestaurantHud>(); cameraView=hud.gameplayCamera;
            normalSize=cameraView.orthographicSize; background=cameraView.backgroundColor;
            sky=RenderSettings.ambientSkyColor; equator=RenderSettings.ambientEquatorColor; ground=RenderSettings.ambientGroundColor;
            lights=FindObjectsByType<Light>().Where(l=>l.gameObject.scene==gameObject.scene)
                .Select(l=>new LightState {light=l,color=l.color,intensity=l.intensity}).ToArray();
            if(openingAfterLoad)
            {
                openingAfterLoad=false; Current=Phase.Opening; Pause(); Apply(1);
            }
        }
        private void Pause() { savedScale=Time.timeScale>0 ? Time.timeScale:1; Time.timeScale=0; ownsPause=true; }
        private void Resume() { if(ownsPause)Time.timeScale=savedScale; ownsPause=false; }
        private void Update() => AdvancePresentation(Time.unscaledDeltaTime);
        public void AdvancePresentation(float seconds)
        {
            if(Current==Phase.Service && hud.shift?.Day?.Closed==true)
            {
                GetComponent<RestaurantMenu>().Close(); Current=Phase.Closing; elapsed=0; Pause();
            }
            elapsed+=Mathf.Max(0,seconds);
            if(Current==Phase.Closing)
            {
                Apply(Ease(elapsed/TransitionSeconds));
                if(elapsed>=TransitionSeconds)
                {
                    Current=Phase.Night; elapsed=0; Resume(); GetComponent<RestaurantMenu>().OpenRestaurant();
                }
            }
            else if(Current==Phase.LeavingManagement && elapsed>=.35f)
            {
                var load=loadNextDay; loadNextDay=null;
                // The new scene starts at exactly this night framing before its first rendered frame.
                Current=Phase.Night; openingAfterLoad=true;
                try { load?.Invoke(); } catch { openingAfterLoad=false; throw; }
            }
            else if(Current==Phase.Opening)
            {
                Apply(1-Ease(elapsed/TransitionSeconds));
                if(elapsed>=TransitionSeconds)
                {
                    Current=Phase.Service; elapsed=0; Resume();
                    foreach(var input in FindObjectsByType<ChefInput>()) input.RequireActionRelease();
                }
            }
        }
        public bool BeginNextDay(Action load)
        {
            if(Current!=Phase.Night || hud.shift?.Day?.Paid!=true || load==null)return false;
            loadNextDay=load; elapsed=0; Current=Phase.LeavingManagement; return true;
        }
        private static float Ease(float t) { t=Mathf.Clamp01(t); return t*t*(3-2*t); }
        private void Apply(float night)
        {
            NightAmount=night;
            cameraView.orthographicSize=Mathf.Lerp(normalSize,normalSize*CameraPullback,night);
            cameraView.backgroundColor=Color.Lerp(background,new Color(.10f,.14f,.23f),night);
            RenderSettings.ambientSkyColor=Color.Lerp(sky,new Color(.34f,.39f,.53f),night);
            RenderSettings.ambientEquatorColor=Color.Lerp(equator,new Color(.34f,.34f,.39f),night);
            RenderSettings.ambientGroundColor=Color.Lerp(ground,new Color(.23f,.23f,.28f),night);
            foreach(var state in lights)
            {
                if(state.light==null)continue;
                bool sun=state.light.type==LightType.Directional;
                state.light.intensity=state.intensity*Mathf.Lerp(1,sun?.57f:1.15f,night);
                state.light.color=Color.Lerp(state.color,sun?new Color(.75f,.80f,1):new Color(1,.66f,.37f),night);
                // Brief warm dawn, returning to the exact authored service colors.
                if(sun && Current==Phase.Opening) state.light.color=Color.Lerp(state.light.color,new Color(1,.83f,.66f),Mathf.Sin(night*Mathf.PI)*.55f);
            }
        }
        private void OnDestroy() { Resume(); }
    }
}
