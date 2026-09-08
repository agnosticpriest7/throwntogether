using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThrownTogether.Editor
{
    public static class CreateRestaurantScene
    {
        public const string ScenePath="Assets/Scenes/RestaurantDevelopment.unity";
        [MenuItem("Thrown Together/Create Vertical Slice Scene")]
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("Restaurant scene already exists; edit the authored scene instead of regenerating it.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save your current scene before creating the restaurant.");
            Directory.CreateDirectory("Assets/Data/VerticalSlice"); Directory.CreateDirectory("Assets/Prefabs/VerticalSlice"); Directory.CreateDirectory("Assets/Materials/VerticalSlice");
            AssetDatabase.Refresh();
            var potato=ScriptableObject.CreateInstance<IngredientDefinition>();
            AssetDatabase.CreateAsset(potato,"Assets/Data/VerticalSlice/Potato.asset");
            var prep=ScriptableObject.CreateInstance<ProcessingRecipe>(); prep.ingredient=potato; prep.input=FoodState.Raw; prep.output=FoodState.Cut; prep.duration=1.5f;
            AssetDatabase.CreateAsset(prep,"Assets/Data/VerticalSlice/PrepPotato.asset");
            var fry=ScriptableObject.CreateInstance<ProcessingRecipe>(); fry.ingredient=potato; fry.input=FoodState.Cut; fry.output=FoodState.Cooked; fry.duration=5;
            AssetDatabase.CreateAsset(fry,"Assets/Data/VerticalSlice/FryPotato.asset");
            var dish=ScriptableObject.CreateInstance<DishRecipe>(); dish.ingredient=potato;
            AssetDatabase.CreateAsset(dish,"Assets/Data/VerticalSlice/Fries.asset");

            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cream=Material("Cream",new Color(.88f,.85f,.72f));
            var floor=Material("KitchenFloor",new Color(.33f,.43f,.44f));
            var dining=Material("DiningFloor",new Color(.6f,.5f,.4f));
            var dark=Material("Charcoal",new Color(.13f,.2f,.24f));
            var mint=Material("Mint",new Color(.3f,.72f,.58f));
            var amber=Material("Amber",new Color(.95f,.6f,.16f));
            var coral=Material("Coral",new Color(.8f,.36f,.27f));
            var white=Material("White",new Color(.97f,.97f,.92f));
            Box("Kitchen floor",null,new Vector3(-2, -.15f,1),new Vector3(10,.3f,11),floor);
            Box("Dining floor",null,new Vector3(5,-.15f,1),new Vector3(4,.3f,11),dining);
            Box("Back wall",null,new Vector3(0,.4f,6.5f),new Vector3(14,.8f,.25f),cream);
            Box("Left wall",null,new Vector3(-7,.4f,1),new Vector3(.25f,.8f,11),cream);
            Box("Right wall",null,new Vector3(7,.4f,1),new Vector3(.25f,.8f,11),cream);
            Box("Front boundary",null,new Vector3(0,.12f,-4.5f),new Vector3(14,.24f,.25f),cream);

            var item=new GameObject("Carryable item").AddComponent<Carryable>();
            var itemPrefab=PrefabUtility.SaveAsPrefabAsset(item.gameObject,"Assets/Prefabs/VerticalSlice/Carryable.prefab").GetComponent<Carryable>(); Object.DestroyImmediate(item.gameObject);
            var source=Station<SourceStation>("1  POTATOES",new Vector3(-5,0,4),mint,dark); source.ingredient=potato; source.itemPrefab=itemPrefab;
            Box("Potato crate",source.transform,new Vector3(0,1.35f,0),new Vector3(1.2f,.3f,.8f),amber);
            var chop=Station<ProcessingStation>("2  PREP",new Vector3(-1,0,1),cream,dark); chop.recipe=prep; chop.slot=Slot(chop.transform,new Vector3(0,1.3f,0));
            Box("Chopping board",chop.transform,new Vector3(0,1.2f,0),new Vector3(1.2f,.05f,.8f),mint);
            var fryer=Station<ProcessingStation>("3  FRYER",new Vector3(1,0,4),amber,dark); fryer.recipe=fry; fryer.slot=Slot(fryer.transform,new Vector3(0,1.35f,0));
            Box("Fryer basket",fryer.transform,new Vector3(0,1.22f,0),new Vector3(1.25f,.08f,.9f),dark);
            var plates=Station<SourceStation>("4  PLATES",new Vector3(-5,0,-2),mint,dark); plates.plates=true; plates.itemPrefab=itemPrefab;
            for(int i=0;i<3;i++) Shape(PrimitiveType.Cylinder,"Plate stack",plates.transform,new Vector3(0,1.24f+i*.08f,0),new Vector3(.8f,.035f,.8f),white,false);
            var plating=Station<CounterStation>("PLATING COUNTER",new Vector3(-1,0,-2),cream,dark); plating.slot=Slot(plating.transform,new Vector3(0,1.3f,0));
            var spare=Station<CounterStation>("SPARE COUNTER",new Vector3(-5,0,1),cream,dark); spare.slot=Slot(spare.transform,new Vector3(0,1.3f,0));

            var table=new GameObject("Customer table"); table.transform.position=new Vector3(5,0,4);
            Box("Table",table.transform,new Vector3(0,.65f,0),new Vector3(1.6f,1.3f,1.4f),cream);
            var order=new GameObject("Fries customer").AddComponent<CustomerOrder>(); order.recipe=dish; order.tableSlot=Slot(table.transform,new Vector3(0,1.4f,-.1f));
            var customer=new GameObject("Seated customer").transform; customer.position=new Vector3(5,0,5.35f); order.customerVisual=customer;
            Shape(PrimitiveType.Capsule,"Customer body",customer,new Vector3(0,.8f,0),new Vector3(.65f,.55f,.65f),coral,false);
            Shape(PrimitiveType.Sphere,"Customer head",customer,new Vector3(0,1.45f,0),Vector3.one*.5f,cream,false);
            Box("Chair",customer,new Vector3(0,.35f,.2f),new Vector3(.85f,.65f,.8f),dark);
            var pickup=Station<ServiceStation>("5  SERVICE PICKUP",new Vector3(4,0,1),coral,dark); pickup.order=order; pickup.pickupSlot=Slot(pickup.transform,new Vector3(0,1.3f,0));

            var chef=new GameObject("Chef").AddComponent<ChefController>();
            var motor=chef.GetComponent<CharacterController>(); motor.height=1.85f; motor.center=new Vector3(0,.93f,0); motor.radius=.3f; motor.stepOffset=.15f; motor.skinWidth=.035f;
            chef.Hands=Slot(chef.transform,new Vector3(0,1.1f,.65f));
            Shape(PrimitiveType.Capsule,"Apron",chef.transform,new Vector3(0,.85f,0),new Vector3(.62f,.45f,.62f),mint,false);
            Shape(PrimitiveType.Sphere,"Head",chef.transform,new Vector3(0,1.4f,0),Vector3.one*.45f,cream,false);
            Shape(PrimitiveType.Cylinder,"Chef hat",chef.transform,new Vector3(0,1.72f,0),new Vector3(.55f,.15f,.55f),white,false);
            Box("Facing marker",chef.transform,new Vector3(0,1.3f,.25f),new Vector3(.2f,.14f,.13f),dark,false);
            chef.gameObject.AddComponent<ChefInput>();
            var chefPrefab=PrefabUtility.SaveAsPrefabAsset(chef.gameObject,"Assets/Prefabs/VerticalSlice/Chef.prefab"); Object.DestroyImmediate(chef.gameObject);
            chef=((GameObject)PrefabUtility.InstantiatePrefab(chefPrefab)).GetComponent<ChefController>(); chef.transform.position=new Vector3(-3.2f,.03f,-.3f);
            var camera=new GameObject("Gameplay Camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.transform.position=new Vector3(0,16,-12); camera.transform.LookAt(new Vector3(0,0,1)); camera.orthographic=true; camera.orthographicSize=6.5f; camera.nearClipPlane=.1f; camera.farClipPlane=80; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.07f,.12f,.16f); camera.gameObject.AddComponent<AudioListener>();
            var light=new GameObject("Key light").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f; light.transform.rotation=Quaternion.Euler(50,-30,0); light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.65f,.7f,.75f);
            var hud=new GameObject("Service HUD").AddComponent<RestaurantHud>(); hud.chef=chef; hud.order=order; hud.gameplayCamera=camera;
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("Created authored RestaurantDevelopment scene and vertical-slice data/prefabs.");
        }
        private static Material Material(string name,Color color) { var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color=color; AssetDatabase.CreateAsset(m,"Assets/Materials/VerticalSlice/"+name+".mat"); return m; }
        private static CarrySlot Slot(Transform parent,Vector3 position) { var s=new GameObject("Carry slot").AddComponent<CarrySlot>(); s.transform.SetParent(parent,false); s.transform.localPosition=position; return s; }
        private static T Station<T>(string label,Vector3 position,Material top,Material baseMaterial) where T:Interactable
        {
            var s=new GameObject(label).AddComponent<T>(); s.stationName=label; s.transform.position=position;
            Box("Cabinet",s.transform,new Vector3(0,.55f,0),new Vector3(1.8f,1.1f,1.5f),baseMaterial);
            Box("Worktop",s.transform,new Vector3(0,1.13f,0),new Vector3(1.9f,.16f,1.6f),top);
            return s;
        }
        private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collision=true) => Shape(PrimitiveType.Cube,name,parent,position,scale,material,collision);
        private static GameObject Shape(PrimitiveType type,string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collision)
        {
            var obj=GameObject.CreatePrimitive(type); obj.name=name; if(parent!=null)obj.transform.SetParent(parent,false); obj.transform.localPosition=position; obj.transform.localScale=scale; obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision) Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
        }
    }
}
