using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    // Run once in an isolated authoring workspace. Outputs are normal reusable assets.
    public static class AuthorMenuExpansion
    {
        const string Folder="Assets/Data/MenuExpansion";
        static T Save<T>(T value,string name) where T:UnityEngine.Object {AssetDatabase.CreateAsset(value,Folder+"/"+name+".asset");return value;}
        static T Old<T>(string name) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>("Assets/Data/VerticalSlice/"+name+".asset");
        static Material steel, dark, wood, green, red, gold, white;
        static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;return Save(m,name);}
        static GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material m,PrimitiveType type=PrimitiveType.Cube)
        {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        static GameObject Station(string name)
        {
            var g=new GameObject(name);var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,.6f,0);c.size=new Vector3(1.8f,1.2f,1.5f);
            var visual=new GameObject("Authored category art");visual.transform.SetParent(g.transform,false);g.AddComponent<StationArt>().visual=visual;
            Part(visual.transform,"Cabinet",new Vector3(0,.62f,0),new Vector3(1.72f,1.05f,1.42f),steel);
            Part(visual.transform,"Toe recess",new Vector3(0,.12f,0),new Vector3(1.55f,.15f,1.3f),dark);
            return g;
        }
        static GameObject Appliance(string title,ApplianceDefinition appliance,bool grill)
        {
            var g=Station(title);var t=g.GetComponent<StationArt>().visual.transform;
            Part(t,"Hot surface",new Vector3(0,1.2f,0),new Vector3(1.65f,.10f,1.34f),dark);
            for(int side=-1;side<=1;side+=2)Part(t,"Steel rim",new Vector3(side*.84f,1.27f,0),new Vector3(.07f,.15f,1.42f),steel);
            Part(t,"Back splash",new Vector3(0,1.34f,.69f),new Vector3(1.75f,.3f,.08f),steel);
            Part(t,"Control fascia",new Vector3(0,1,-.74f),new Vector3(1.72f,.28f,.08f),steel);
            for(int i=-1;i<=1;i++)Part(t,"Dial",new Vector3(i*.52f,1,-.81f),new Vector3(.14f,.14f,.08f),dark,PrimitiveType.Sphere);
            if(grill)for(int i=-4;i<=4;i++)Part(t,"Grill bar",new Vector3(i*.16f,1.29f,0),new Vector3(.055f,.07f,1.14f),steel);
            else Part(t,"Griddle grease channel",new Vector3(0,1.27f,-.55f),new Vector3(1.5f,.025f,.06f),gold);
            var slot=new GameObject("Food slot");slot.transform.SetParent(g.transform,false);slot.transform.localPosition=new Vector3(0,1.34f,0);
            var s=g.AddComponent<ProcessingStation>();s.stationName=title;s.appliance=appliance;s.recipe=appliance.supportedProcesses[0];s.slot=slot.AddComponent<CarrySlot>();
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,Folder+"/"+(grill?"Grill":"Griddle")+".prefab");UnityEngine.Object.DestroyImmediate(g);return prefab;
        }
        static GameObject Storage(IngredientStorageDefinition definition,Carryable item,bool fridge)
        {
            var g=Station(definition.displayName);var t=g.GetComponent<StationArt>().visual.transform;
            if(fridge)
            {
                Part(t,"Refrigerator upper door",new Vector3(0,1.36f,0),new Vector3(1.7f,.46f,1.4f),steel);
                Part(t,"Insulated door",new Vector3(0,.83f,-.74f),new Vector3(1.54f,1.30f,.08f),white);
                Part(t,"Door seal",new Vector3(0,1.17f,-.79f),new Vector3(1.48f,.025f,.015f),dark);
                Part(t,"Door handle",new Vector3(.59f,.91f,-.83f),new Vector3(.045f,.36f,.06f),dark);
                Part(t,"Cold indicator",new Vector3(-.57f,1.34f,-.80f),new Vector3(.09f,.06f,.02f),green);
            }
            else
            {
                Part(t,"Produce worktop",new Vector3(0,1.2f,0),new Vector3(1.82f,.12f,1.48f),wood);
                for(int i=0;i<4;i++)
                {
                    var p=new Vector3((i%2-.5f)*.84f,1.31f,(i/2-.5f)*.67f);
                    Part(t,"Produce bin",p,new Vector3(.79f,.16f,.62f),wood);
                    for(int j=0;j<3;j++)Part(t,definition.ingredients[i].displayName,p+new Vector3((j-1)*.2f,.14f,(j%2-.5f)*.16f),new Vector3(.24f,.20f,.26f),new[]{gold,red,green,white}[i],PrimitiveType.Sphere);
                }
            }
            g.GetComponent<StationArt>().includesPantryDisplay=true;
            var source=g.AddComponent<SourceStation>();source.stationName=definition.displayName;source.storage=definition;source.ingredient=definition.ingredients[0];source.itemPrefab=item;
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,Folder+"/"+(fridge?"Refrigerator":"ProduceRack")+".prefab");UnityEngine.Object.DestroyImmediate(g);return prefab;
        }
        public static void Run()
        {
            if(!Application.isBatchMode || Directory.Exists(Folder))throw new InvalidOperationException("Author once in the isolated workspace; preserve reviewed outputs.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            steel=Mat("Steel",new Color(.48f,.62f,.65f));dark=Mat("HotIron",new Color(.13f,.17f,.18f));wood=Mat("ProduceWood",new Color(.59f,.36f,.17f));green=Mat("LeafGreen",new Color(.36f,.66f,.18f));red=Mat("TomatoRed",new Color(.86f,.15f,.055f));gold=Mat("Golden",new Color(.86f,.62f,.22f));white=Mat("Ivory",new Color(.87f,.87f,.75f));
            var potato=Old<IngredientDefinition>("Potato");var mushroom=Old<IngredientDefinition>("Mushrooms");var tomato=Old<IngredientDefinition>("Tomato");var lettuce=Old<IngredientDefinition>("Lettuce");
            IngredientDefinition Ingredient(string key,string title,IngredientVisualKind kind,string purchase,FoodState ready,string cooked,Color raw,Color done)
            {
                var v=ScriptableObject.CreateInstance<IngredientDefinition>();v.id="ingredient."+key;v.displayName=title;v.visualKind=kind;v.requiredPurchase=purchase;v.platingState=ready;
                v.stateNames=new[]{"Raw "+key,"Cut "+key,cooked,cooked,cooked};v.stateColors=new[]{raw,raw,done,done,done};return Save(v,title);
            }
            var egg=Ingredient("egg","Egg",IngredientVisualKind.Egg,"griddle",FoodState.Griddled,"Fried egg",new Color(.94f,.89f,.77f),new Color(1,.75f,.10f));
            var chicken=Ingredient("chicken","Chicken",IngredientVisualKind.Chicken,"grill",FoodState.Grilled,"Grilled chicken",new Color(.94f,.58f,.49f),new Color(.83f,.47f,.17f));
            void Extend(IngredientDefinition v,string griddled,string grilled,params FoodState[] ready)
            {v.stateNames=v.stateNames.Take(3).Concat(new[]{griddled,grilled}).ToArray();v.stateColors=v.stateColors.Take(3).Concat(new[]{new Color(.77f,.49f,.19f),new Color(.61f,.34f,.13f)}).ToArray();v.additionalPlatingStates=ready;EditorUtility.SetDirty(v);}
            Extend(potato,"Hash browns","Potato",FoodState.Griddled);Extend(mushroom,"Griddled mushrooms","Grilled mushrooms",FoodState.Griddled,FoodState.Grilled);Extend(tomato,"Griddled tomato","Grilled tomato",FoodState.Griddled,FoodState.Grilled);
            ProcessingRecipe Process(string name,IngredientDefinition food,FoodState input,FoodState output,float duration,string label)
            {var v=ScriptableObject.CreateInstance<ProcessingRecipe>();v.id="process."+name;v.displayName=name;v.ingredient=food;v.input=input;v.output=output;v.duration=duration;v.stationLabel=label;return Save(v,name);}
            var eg=Process("GriddleEgg",egg,FoodState.Raw,FoodState.Griddled,4,"Griddle");var po=Process("GriddlePotato",potato,FoodState.Cut,FoodState.Griddled,3,"Griddle");
            var mu=Process("GriddleMushroom",mushroom,FoodState.Cut,FoodState.Griddled,3,"Griddle");var to=Process("GriddleTomato",tomato,FoodState.Cut,FoodState.Griddled,3,"Griddle");
            var ch=Process("GrillChicken",chicken,FoodState.Raw,FoodState.Grilled,6,"Grill");var gm=Process("GrillMushroom",mushroom,FoodState.Cut,FoodState.Grilled,4,"Grill");var gt=Process("GrillTomato",tomato,FoodState.Cut,FoodState.Grilled,3.5f,"Grill");
            ApplianceDefinition Machine(string name,params ProcessingRecipe[] processes){var a=ScriptableObject.CreateInstance<ApplianceDefinition>();a.id="appliance."+name.ToLowerInvariant();a.displayName=name;a.supportedProcesses=processes;return Save(a,name+"Definition");}
            var griddle=Machine("Griddle",eg,po,mu,to);var grill=Machine("Grill",ch,gm,gt);
            var pp=Old<ProcessingRecipe>("PrepPotato");var pm=Old<ProcessingRecipe>("PrepMushrooms");var pt=Old<ProcessingRecipe>("PrepTomato");var pl=Old<ProcessingRecipe>("PrepLettuce");var fp=Old<ProcessingRecipe>("FryPotato");
            RecipeDefinition Dish(string key,string name,int price,string purchase,ProcessingRecipe[] steps,params IngredientPortion[] parts)
            {var r=ScriptableObject.CreateInstance<RecipeDefinition>();r.id="dish."+key;r.displayName=name;r.salePrice=price;r.requiredPurchases=new[]{purchase};r.steps=steps;r.ingredient=parts[0].ingredient;r.requiredState=parts[0].state;r.additionalIngredients=parts.Skip(1).ToArray();return Save(r,key);}
            IngredientPortion P(IngredientDefinition i,FoodState s)=>new IngredientPortion{ingredient=i,state=s};
            var book=Resources.Load<RecipeBook>("RecipeBook");book.recipes=book.recipes.Concat(new[]{
                Dish("fried-egg","Fried Egg",12,"griddle",new[]{eg},P(egg,FoodState.Griddled)),
                Dish("hash-browns","Hash Browns",12,"griddle",new[]{pp,po},P(potato,FoodState.Griddled)),
                Dish("mushroom-omelet","Mushroom Omelet",18,"griddle",new[]{eg,pm,mu},P(egg,FoodState.Griddled),P(mushroom,FoodState.Griddled)),
                Dish("garden-omelet","Garden Omelet",26,"griddle",new[]{eg,pt,to,pm,mu},P(egg,FoodState.Griddled),P(tomato,FoodState.Griddled),P(mushroom,FoodState.Griddled)),
                Dish("grilled-chicken","Grilled Chicken",14,"grill",new[]{ch},P(chicken,FoodState.Grilled)),
                Dish("grilled-mushrooms","Grilled Mushrooms",12,"grill",new[]{pm,gm},P(mushroom,FoodState.Grilled)),
                Dish("grilled-tomato","Grilled Tomato",10,"grill",new[]{pt,gt},P(tomato,FoodState.Grilled)),
                Dish("chicken-fries","Chicken & Fries",24,"grill",new[]{ch,pp,fp},P(chicken,FoodState.Grilled),P(potato,FoodState.Cooked)),
                Dish("chicken-salad","Chicken Salad",26,"grill",new[]{ch,pl,pt},P(chicken,FoodState.Grilled),P(lettuce,FoodState.Cut),P(tomato,FoodState.Cut)),
                Dish("chicken-mushroom","Chicken Mushroom Plate",22,"grill",new[]{ch,pm,gm},P(chicken,FoodState.Grilled),P(mushroom,FoodState.Grilled))}).ToArray();EditorUtility.SetDirty(book);
            var settings=Resources.Load<DayServiceDefinition>("ServiceDay");
            RestaurantUpgradeDefinition Offer(string key,string title,int price,GameObject prefab,params Vector3[] positions)
            {var o=ScriptableObject.CreateInstance<RestaurantUpgradeDefinition>();o.id=key;o.displayName=title;o.cost=price;o.kind=RestaurantPurchaseKind.ApplianceBay;o.stationPrefab=prefab;o.layoutPositions=positions;return Save(o,key+"-offer");}
            settings.purchases=settings.purchases.Concat(new[]{Offer("griddle","Flat-Top Griddle",125,Appliance("Flat-Top Griddle",griddle,false),new Vector3(-6.4f,0,3.55f),new Vector3(.6f,0,5),new Vector3(-6,0,-1.4f)),Offer("grill","Grill",150,Appliance("Grill",grill,true),new Vector3(-4.1f,0,3.55f),new Vector3(-6,0,1),new Vector3(-2.5f,0,-4.3f))}).ToArray();EditorUtility.SetDirty(settings);
            IngredientStorageDefinition Category(string name,params IngredientDefinition[] foods){var c=ScriptableObject.CreateInstance<IngredientStorageDefinition>();c.id="storage."+name;c.displayName=name;c.ingredients=foods;return Save(c,name.Replace(" ",""));}
            var produce=Category("Produce Rack",potato,tomato,lettuce,mushroom);var fridge=Category("Refrigerator",egg,chicken);
            GameObject producePrefab=null,fridgePrefab=null;
            foreach(string name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var sources=UnityEngine.Object.FindObjectsByType<SourceStation>(FindObjectsSortMode.None).Where(s=>!s.plates).ToArray();
                if(producePrefab==null){producePrefab=Storage(produce,sources[0].itemPrefab,false);fridgePrefab=Storage(fridge,sources[0].itemPrefab,true);}
                foreach(var source in sources)
                {
                    var root=source.gameObject;var kind=source.ingredient.visualKind;
                    // Preserve layout anchor transforms; remove the old crate's interaction and geometry.
                    foreach(Transform child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                    foreach(var component in root.GetComponents<Component>())if(!(component is Transform))UnityEngine.Object.DestroyImmediate(component);
                    if(kind!=IngredientVisualKind.Potato && kind!=IngredientVisualKind.Mushroom){root.name="Vacant ingredient bay";continue;}
                    var prefab=kind==IngredientVisualKind.Potato?producePrefab:fridgePrefab;
                    var copy=UnityEngine.Object.Instantiate(prefab,root.transform);copy.transform.localPosition=Vector3.zero;copy.transform.localRotation=Quaternion.identity;
                    root.name=kind==IngredientVisualKind.Potato?"Produce storage anchor":"Refrigerator storage anchor";
                }
                EditorSceneManager.SaveScene(scene);
            }
            var island=Old<KitchenLayoutDefinition>("Kitchen1");island.stations[4].x=.4f;EditorUtility.SetDirty(island);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("Menu expansion authored: 13 recipes, 2 appliance offers and 2 reusable category sources.");
        }
    }
}
