using UnityEngine;

namespace ThrownTogether
{
    // Visual-only station dressing. Authored transforms, collisions and carry slots stay authoritative.
    public sealed class KitchenPresentation : MonoBehaviour
    {
        private RestaurantHud hud;
        private Material material;
        private Carryable assets;
        private Marker[] markers;
        private sealed class Marker { public Interactable station; public GameObject one, two, ready; }
        private readonly Color wood=new Color(.66f,.43f,.24f), steel=new Color(.65f,.75f,.78f);
        private void Start()
        {
            hud=GetComponent<RestaurantHud>();
            foreach(var source in FindObjectsByType<SourceStation>(FindObjectsSortMode.None))
                if(source.gameObject.scene==gameObject.scene) { assets=source.itemPrefab; break; }
            if(assets==null) return;
            material=new Material(assets.visualShader);
            var stations=System.Array.FindAll(FindObjectsByType<Interactable>(FindObjectsSortMode.None),s=>s.gameObject.scene==gameObject.scene);
            markers=new Marker[stations.Length];
            for(int i=0;i<stations.Length;i++)
            {
                var station=stations[i]; Dress(station);
                var marker=new Marker {station=station}; markers[i]=marker;
                marker.one=Border(station.transform,"P1 target",new Color(.15f,1,.68f),1.08f,.05f);
                marker.two=Border(station.transform,"P2 target",new Color(1,.38f,.3f),1.18f,.07f);
                marker.ready=Part(station.transform,"Ready indicator",assets.sphereMesh,new Vector3(.77f,1.28f,-.64f),Vector3.one*.14f,new Color(.2f,1,.5f)).gameObject;
                marker.one.SetActive(false); marker.two.SetActive(false); marker.ready.SetActive(false);
            }
            foreach(var customer in FindObjectsByType<CustomerOrder>(FindObjectsSortMode.None))
                if(customer.gameObject.scene==gameObject.scene) customer.gameObject.AddComponent<CustomerPresentation>().Initialize(customer,assets);
        }
        private Transform Part(Transform parent,string name,Mesh mesh,Vector3 position,Vector3 scale,Color color)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            var block=new MaterialPropertyBlock(); block.SetColor("_BaseColor",color); renderer.SetPropertyBlock(block);
            return go.transform;
        }
        private Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Color c) => Part(parent,name,assets.cubeMesh,p,size,c);
        private GameObject Border(Transform parent,string name,Color color,float extent,float y)
        {
            var root=new GameObject(name); root.transform.SetParent(parent,false);
            Box(root.transform,"Front",new Vector3(0,y,-extent*.82f),new Vector3(extent*2,.045f,.065f),color);
            Box(root.transform,"Back",new Vector3(0,y,extent*.82f),new Vector3(extent*2,.045f,.065f),color);
            Box(root.transform,"Left",new Vector3(-extent,y,0),new Vector3(.065f,.045f,extent*1.64f),color);
            Box(root.transform,"Right",new Vector3(extent,y,0),new Vector3(.065f,.045f,extent*1.64f),color);
            return root;
        }
        private static void Tint(Transform parent,string name,Color color)
        {
            var renderer=parent.Find(name)?.GetComponent<Renderer>(); if(renderer==null) return;
            var block=new MaterialPropertyBlock(); block.SetColor("_BaseColor",color); renderer.SetPropertyBlock(block);
        }
        private static void HideDecoration(Transform parent,string name)
        {
            var renderer=parent.Find(name)?.GetComponent<Renderer>(); if(renderer!=null) renderer.enabled=false;
        }
        private void Dress(Interactable station)
        {
            var t=station.transform;
            bool authored=station.GetComponent<StationArt>()?.visual!=null;
            if(!authored)
            {
            Tint(t,"Worktop",new Color(.77f,.81f,.78f));
            Tint(t,"Cabinet",new Color(.24f,.32f,.32f));
            // Small cabinet seams and handles supply scale without covering the work surface.
            Box(t,"Door seam",new Vector3(0,.54f,-.757f),new Vector3(.025f,.87f,.015f),new Color(.12f,.18f,.18f));
            Box(t,"Left handle",new Vector3(-.13f,.75f,-.79f),new Vector3(.045f,.19f,.045f),steel);
            Box(t,"Right handle",new Vector3(.13f,.75f,-.79f),new Vector3(.045f,.19f,.045f),steel);
            }
            if(station is WashingStation)
            {
                if(authored) return;
                Tint(t,"Worktop",steel);
                Box(t,"Sink bowl",new Vector3(0,1.23f,0),new Vector3(1.4f,.06f,1),new Color(.12f,.28f,.35f));
                for(int side=-1;side<=1;side+=2) Box(t,"Sink rim",new Vector3(side*.76f,1.29f,0),new Vector3(.09f,.13f,1.16f),steel);
                Box(t,"Tap upright",new Vector3(.42f,1.58f,.56f),new Vector3(.09f,.65f,.09f),steel);
                Box(t,"Tap spout",new Vector3(.42f,1.88f,.32f),new Vector3(.09f,.09f,.53f),steel);
                return;
            }
            if(station is DishReturnStation)
            {
                if(authored && station.GetComponent<StationArt>().includesServiceDisplay) return;
                Tint(t,"Cabinet",new Color(.35f,.29f,.24f));
                for(int side=-1;side<=1;side+=2) Box(t,"Return tray rim",new Vector3(side*.7f,1.35f,0),new Vector3(.07f,.25f,1.1f),steel);
                Box(t,"Return tray back",new Vector3(0,1.35f,.55f),new Vector3(1.45f,.25f,.07f),steel);
                return;
            }
            if(station is SourceStation source)
            {
                if(source.plates)
                {
                    Tint(t,"Cabinet",new Color(.35f,.45f,.54f));
                    for(int i=0;i<3;i++) Part(t,"Stacked ceramic",assets.cylinderMesh,new Vector3(.23f,1.48f+i*.07f,0),new Vector3(.9f,.025f,.9f),i%2==0 ? Color.white:steel);
                    return;
                }
                HideDecoration(t,"Potato crate");
                if(authored && station.GetComponent<StationArt>().includesPantryDisplay) return;
                Box(t,"Crate base",new Vector3(0,1.24f,0),new Vector3(1.42f,.08f,1.04f),wood);
                for(int side=-1;side<=1;side+=2)
                {
                    for(int level=0;level<2;level++)
                    {
                        Box(t,"Crate slat",new Vector3(0,1.31f+level*.14f,side*.51f),new Vector3(1.45f,.095f,.065f),wood);
                        Box(t,"Crate end",new Vector3(side*.69f,1.38f,0),new Vector3(.065f,.30f,1.02f),wood);
                    }
                }
                for(int i=0;i<4;i++)
                {
                    var preview=Instantiate(assets,t); preview.name="Pantry ingredient display";
                    preview.transform.localPosition=new Vector3((i%2-.5f)*.59f,1.49f,(i/2-.5f)*.42f);
                    if(source.ingredient.visualKind==IngredientVisualKind.Mushroom)
                    { preview.transform.localScale=Vector3.one*.82f; preview.transform.localRotation=Quaternion.Euler(20,15*(i-1),0); }
                    preview.Configure(ItemPayload.Food(source.ingredient));
                }
                return;
            }
            if(station is ProcessingStation process)
            {
                if(authored)
                {
                    station.gameObject.AddComponent<StationPresentation>().Initialize(process,assets);
                    return;
                }
                bool prep=process.recipe.input==FoodState.Raw;
                if(prep)
                {
                    Tint(t,"Chopping board",new Color(.72f,.48f,.23f));
                    Box(t,"Board handle",new Vector3(.69f,1.21f,0),new Vector3(.25f,.055f,.25f),wood);
                    Box(t,"Resting knife blade",new Vector3(-.68f,1.24f,.1f),new Vector3(.12f,.025f,.51f),new Color(.87f,.91f,.94f));
                    Box(t,"Knife grip",new Vector3(-.68f,1.25f,-.25f),new Vector3(.13f,.065f,.23f),new Color(.15f,.16f,.18f));
                }
                else
                {
                    Tint(t,"Cabinet",steel); Tint(t,"Fryer basket",new Color(.35f,.19f,.025f));
                    Box(t,"Control back",new Vector3(0,1.41f,.64f),new Vector3(1.8f,.45f,.19f),steel);
                    for(int i=-1;i<=1;i+=2) Part(t,"Fryer dial",assets.sphereMesh,new Vector3(i*.5f,1.48f,.52f),new Vector3(.16f,.16f,.07f),Color.black);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Box(t,"Basket rim",new Vector3(side*.65f,1.30f,0),new Vector3(.06f,.09f,1),steel);
                        Box(t,"Basket edge",new Vector3(0,1.30f,side*.48f),new Vector3(1.3f,.09f,.06f),steel);
                    }
                    Box(t,"Basket stem",new Vector3(.5f,1.37f,-.61f),new Vector3(.07f,.06f,.34f),steel);
                    Box(t,"Basket grip",new Vector3(.5f,1.4f,-.78f),new Vector3(.19f,.11f,.23f),Color.black);
                }
                station.gameObject.AddComponent<StationPresentation>().Initialize(process,assets);
                return;
            }
            if(station is ServiceStation)
            {
                if(authored && station.GetComponent<StationArt>().includesServiceDisplay) return;
                Tint(t,"Cabinet",new Color(.54f,.23f,.18f));
                for(int side=-1;side<=1;side+=2) Box(t,"Pass post",new Vector3(side*.81f,1.60f,.57f),new Vector3(.09f,.9f,.09f),steel);
                Box(t,"Pass shelf",new Vector3(0,2.04f,.51f),new Vector3(1.88f,.07f,.36f),steel);
                Part(t,"Service bell base",assets.cylinderMesh,new Vector3(.69f,1.25f,-.48f),new Vector3(.26f,.025f,.26f),Color.black);
                Part(t,"Service bell",assets.sphereMesh,new Vector3(.69f,1.30f,-.48f),new Vector3(.21f,.11f,.21f),new Color(.9f,.75f,.28f));
            }
        }
        private void LateUpdate()
        {
            if(markers==null) return;
            foreach(var m in markers)
            {
                m.one.SetActive(hud.chef!=null && hud.chef.Focus==m.station);
                m.two.SetActive(hud.coop!=null && hud.coop.PlayerTwo!=null && hud.coop.PlayerTwo.Focus==m.station);
                m.ready.SetActive(m.station is ProcessingStation p && !p.Busy && p.slot.Item!=null || m.station is WashingStation w && !w.Busy && w.slot.Item!=null);
            }
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
