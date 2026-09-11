using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThrownTogether
{
    public sealed class StartupSequence : MonoBehaviour
    {
        public bool studioIntro;
        public CanvasGroup artwork;
        public Canvas canvas;
        public Texture2D publisherLogo;
        public bool ShowingPublisher { get; private set; }
        public const float FadeIn=.5f, Hold=3f, FadeOut=.75f;
        public float Elapsed { get; private set; }
        public bool Leaving { get; private set; }
        private static StartupSequence active;
        public static bool BlocksMenu => active!=null;
        private InputAction start;
        private bool released, loading, revealing;
        private float exitFrom=1, revealTime;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()=>active=null;
        private void Awake()
        {
            active=this;artwork.alpha=0;
            start=new InputAction("Start",InputActionType.Button);
            start.AddBinding("<Gamepad>/buttonSouth");start.AddBinding("<Keyboard>/enter");start.AddBinding("<Keyboard>/space");start.AddBinding("<Mouse>/leftButton");
            start.Enable();
        }
        private void Update()
        {
            // Unity may update Boot behind its native splash; count only visible intro time.
            if(studioIntro && !UnityEngine.Rendering.SplashScreen.isFinished)return;
            if(revealing)
            {
                revealTime+=Time.unscaledDeltaTime;
                if(revealTime>=.35f)Destroy(gameObject);
                return;
            }
            if(loading)return;
            foreach(var pad in Gamepad.all)if(pad.buttonSouth.wasPressedThisFrame)WebInputFocus.FocusFromGamepad();
            if(!start.IsPressed())released=true;
            if(released && start.WasPressedThisFrame() && WebInputFocus.HasFocus)RequestStart();
            Advance(Time.unscaledDeltaTime);
            if(studioIntro && !Leaving && Elapsed>=FadeIn+Hold)RequestStart();
            if(Leaving && Elapsed>=FadeOut && !TryShowPublisher()){loading=true;StartCoroutine(LoadNext());}
        }
        public void Advance(float seconds)
        {
            if(loading || revealing)return;
            Elapsed+=Mathf.Max(0,seconds);
            artwork.alpha=Leaving ? exitFrom*(1-Ease(Elapsed/FadeOut)):Ease(Elapsed/FadeIn);
        }
        public bool RequestStart()
        {
            if(Leaving || loading || revealing)return false;
            exitFrom=artwork.alpha;Elapsed=0;Leaving=true;return true;
        }
        // Switch artwork only at black, reusing the same lightweight canvas and fade.
        public bool TryShowPublisher()
        {
            if(!studioIntro || ShowingPublisher || publisherLogo==null || !Leaving || Elapsed<FadeOut)return false;
            var image=artwork.GetComponentInChildren<RawImage>();
            if(image==null)return false;
            image.texture=publisherLogo;
            var fit=image.GetComponent<AspectRatioFitter>();
            if(fit!=null)fit.aspectRatio=(float)publisherLogo.width/publisherLogo.height;
            ShowingPublisher=true;Leaving=false;Elapsed=0;artwork.alpha=0;released=false;
            return true;
        }
        private IEnumerator LoadNext()
        {
            if(studioIntro)
            {
#if UNITY_EDITOR
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
                yield return SceneManager.LoadSceneAsync("MainMenu");
#endif
                yield break;
            }
            DontDestroyOnLoad(gameObject);
            RestaurantMenu.ShowFrontEndOnNextLoad();
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantDevelopment.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("RestaurantDevelopment");
#endif
            canvas.enabled=false;revealing=true;revealTime=0;
        }
        private void OnGUI()
        {
            // IMGUI menu is drawn above uGUI; this final black veil also fades the existing menu cleanly.
            if(!revealing)return;
            var color=GUI.color;var depth=GUI.depth;GUI.depth=-10000;
            GUI.color=new Color(0,0,0,1-Ease(revealTime/.35f));GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=color;GUI.depth=depth;
        }
        private static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        private void OnDestroy(){if(active==this)active=null;start?.Dispose();}
    }
}
