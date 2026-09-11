using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    public static class AuthorServiceGrowth
    {
        const string Folder="Assets/Data/ServiceDay/";
        static void Module(Transform parent,string path,Vector3 position,Vector3 scale)
        {var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;}
        static AudioCue Cue(string name,float volume,bool loop,params string[] clips)
        {
            var cue=ScriptableObject.CreateInstance<AudioCue>();cue.volume=volume;cue.loop=loop;cue.group=AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/Audio/place.asset").group;
            cue.clips=clips.Select(c=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/WorkSounds/"+c+".wav")).ToArray();
            if(cue.clips.Any(c=>c==null))throw new Exception("Missing work audio");
            AssetDatabase.CreateAsset(cue,"Assets/Audio/"+name+".asset");return cue;
        }
        public static void CaptureExpanded()
        {
            ApplyCameraTrial.Capture();
            var shift=UnityEngine.Object.FindAnyObjectByType<RestaurantShift>();shift.diningExpansion.SetActive(true);
            foreach(var offer in shift.dayDefinition.purchases)if(offer.stationPrefab!=null)UnityEngine.Object.Instantiate(offer.stationPrefab,offer.layoutPositions[0],Quaternion.identity);
            var food=AssetDatabase.LoadAssetAtPath<Carryable>("Assets/Prefabs/VerticalSlice/Carryable.prefab");
            foreach(var order in shift.expansionSeats)order.GetComponent<CustomerPresentation>().Initialize(order,food);
            var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;
            var target=RenderTexture.GetTemporary(1600,1000,24);var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("Builds/CameraTrialReview/four-tables-equipment.png",image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
        }
        public static void Run()
        {
            if(!Application.isBatchMode || File.Exists(Folder+"Busser.asset"))throw new Exception("Use a fresh isolated candidate; service growth already authored.");
            var day=AssetDatabase.LoadAssetAtPath<DayServiceDefinition>("Assets/Resources/ServiceDay.asset");
            var busser=ScriptableObject.CreateInstance<EmployeeRoleDefinition>();busser.id="hire-busser";busser.displayName="Hire busser";busser.description="Carries dirty table plates to the return rack.";busser.hireCost=100;AssetDatabase.CreateAsset(busser,Folder+"Busser.asset");day.busserRole=busser;
            day.dishwasherRole.description="Collects dirty rack plates, washes them, and returns clean plates to stock.";EditorUtility.SetDirty(day.dishwasherRole);
            day.additionalCustomersPerDay=2;day.firstArrival=1;day.sidewalkStart=new Vector3(-14,0,-7.1f);day.sidewalkExit=new Vector3(17,0,-7.1f);
            var tables=ScriptableObject.CreateInstance<RestaurantUpgradeDefinition>();tables.id="dining-tables";tables.displayName="Two more dining tables";tables.description="Opens the second dining column: four tables total.";tables.kind=RestaurantPurchaseKind.DiningTables;tables.cost=150;AssetDatabase.CreateAsset(tables,Folder+"dining-tables.asset");day.purchases=day.purchases.Concat(new[]{tables}).ToArray();EditorUtility.SetDirty(day);
            var counter=day.purchases.First(p=>p.kind==RestaurantPurchaseKind.CounterBay);counter.layoutPositions[0]=new Vector3(-6.4f,0,-4.3f);EditorUtility.SetDirty(counter);
            var wash=Cue("washing",.25f,true,"washing");var steps=Cue("footsteps",.18f,false,"footstep0","footstep1");
            var place=AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/Audio/place.asset");place.clips=new[]{"plate0","plate1"}.Select(c=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/WorkSounds/"+c+".wav")).ToArray();place.volume=.28f;EditorUtility.SetDirty(place);
            foreach(var name in new[]{"RestaurantDevelopment","RestaurantShift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var shift=UnityEngine.Object.FindAnyObjectByType<RestaurantShift>();
                var floor=GameObject.Find("Dining floor").transform;floor.position+=Vector3.right*2.1f;var scale=floor.localScale;scale.x+=4.2f;floor.localScale=scale;Physics.SyncTransforms();AuthorFirstServiceFloor.Floor(name,true);
                foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>())
                {
                    if(box.gameObject.name=="Dining floor")continue;
                    var bounds=box.bounds;
                    if(bounds.size.z>10 && bounds.size.x<1 && bounds.center.x>8){box.transform.position+=Vector3.right*4.2f;Debug.Log("Moved boundary "+box.name);}
                    else if(bounds.size.x>16 && bounds.size.z<1){box.transform.position+=Vector3.right*2.1f;var s=box.transform.localScale;s.x*= (bounds.size.x+4.2f)/bounds.size.x;box.transform.localScale=s;Debug.Log("Extended boundary "+box.name);}
                }
                var room=GameObject.Find("Restaurant room art").transform;
                foreach(Transform part in room)if(part.localPosition.x>=8.2f){part.localPosition+=Vector3.right*4.2f;}
                foreach(float z in new[]{7.6f,-5.6f})for(float x=8.4f;x<12.59f;x+=1.9f)
                {float width=Mathf.Min(1.9f,12.6f-x);Module(room,"Assets/Art/RestaurantRoom/Prefabs/"+(z>0?"WallPanel":"WallLow")+".prefab",new Vector3(x+width/2,0,z),new Vector3(width/1.9f,z>0?1:.32f,1));}
                var exterior=GameObject.Find("Restaurant finishing details").transform.Find("Exterior framing");
                foreach(float x in new[]{-15.2f,-13.3f,-11.4f,-9.5f,9.5f,11.4f,13.3f,15.2f,17.1f})Module(exterior,"Assets/Art/RestaurantDetails/Prefabs/SidewalkTile.prefab",new Vector3(x,0,-6.55f),Vector3.one);
                foreach(Transform part in exterior)if(part.name=="LowPlanter"){var p=part.localPosition;p.z=-6.6f;part.localPosition=p;}
                if(shift!=null)
                {var expansion=new GameObject("Dining expansion — two purchased tables");shift.diningExpansion=expansion;shift.expansionSeats=new CustomerOrder[shift.seats.Length];
                for(int i=0;i<shift.seats.Length;i++)
                {
                    var original=shift.seats[i];var table=UnityEngine.Object.Instantiate(original.tableSlot.transform.parent.gameObject,expansion.transform);table.name="Expansion table "+(i+3);table.transform.position+=Vector3.right*4.2f;
                    var visual=UnityEngine.Object.Instantiate(original.customerVisual.gameObject,expansion.transform);visual.transform.position+=Vector3.right*4.2f;
                    var logic=UnityEngine.Object.Instantiate(original.gameObject,expansion.transform);logic.name="Expansion order "+(i+3);
                    var order=logic.GetComponent<CustomerOrder>();order.tableSlot=table.GetComponentInChildren<CarrySlot>();order.customerVisual=visual.transform;
                    var presentation=logic.GetComponent<CustomerPresentation>();presentation.seatedVisual=visual.GetComponentInChildren<ChefAppearance>(true);shift.expansionSeats[i]=order;
                }
                expansion.SetActive(false);}
                var audio=UnityEngine.Object.FindAnyObjectByType<RestaurantAudioFeedback>();audio.wash=wash;audio.footstep=steps;
                var camera=UnityEngine.Object.FindAnyObjectByType<RestaurantHud>().gameplayCamera;camera.orthographicSize=7.5f;camera.transform.position=new Vector3(2.1f,.3f,1)-camera.transform.forward*20;
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();ApplyCameraTrial.Capture();Debug.Log("Service growth authored");
        }
    }
}
