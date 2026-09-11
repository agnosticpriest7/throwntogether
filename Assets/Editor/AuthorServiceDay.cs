using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
    public static class AuthorServiceDay
    {
        public static void Apply()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use the isolated authoring workspace.");
            const string folder="Assets/Data/ServiceDay";Directory.CreateDirectory(folder);
            const string path="Assets/Resources/ServiceDay.asset";
            if(File.Exists(path))throw new InvalidOperationException("Already authored; review existing assets instead of overwriting.");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/RestaurantShift.unity");
            var shift=UnityEngine.Object.FindAnyObjectByType<RestaurantShift>();
            var counter=UnityEngine.Object.FindObjectsByType<CounterStation>().First(s=>s.GetType()==typeof(CounterStation));
            var fryer=UnityEngine.Object.FindObjectsByType<ProcessingStation>().First(s=>!s.requiresAttendance);
            GameObject SaveStation(Interactable source,string name)
            {
                var copy=UnityEngine.Object.Instantiate(source.gameObject);copy.name=name;copy.transform.position=Vector3.zero;
                var prefab=PrefabUtility.SaveAsPrefabAsset(copy,folder+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(copy);return prefab;
            }
            RestaurantUpgradeDefinition Offer(string id,string title,int cost,RestaurantPurchaseKind kind,GameObject prefab,Vector3[] positions)
            {
                var offer=ScriptableObject.CreateInstance<RestaurantUpgradeDefinition>();offer.id=id;offer.displayName=title;offer.cost=cost;offer.kind=kind;offer.stationPrefab=prefab;offer.layoutPositions=positions;
                AssetDatabase.CreateAsset(offer,folder+"/"+id+".asset");return offer;
            }
            var config=ScriptableObject.CreateInstance<DayServiceDefinition>();config.id="service-day-v1";config.displayName="Restaurant day";
            config.walkingVisual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ChefVisual.prefab");
            config.purchases=new[]{
                Offer("counter-bay","Extra counter bay",50,RestaurantPurchaseKind.CounterBay,SaveStation(counter,"ExtraCounter"),new[]{new Vector3(1.4f,0,-3.5f),new Vector3(.8f,0,3),new Vector3(-2.5f,0,2.7f)}),
                Offer("fryer-bay","Extra fryer bay",100,RestaurantPurchaseKind.FryerBay,SaveStation(fryer,"ExtraFryer"),new[]{new Vector3(-1.3f,0,5.8f),new Vector3(.8f,0,-4.5f),new Vector3(.5f,0,3)}),
                Offer("fryer-speed","Fryer upgrade (+25% speed)",80,RestaurantPurchaseKind.FasterFryers,null,new Vector3[0])};
            var role=ScriptableObject.CreateInstance<EmployeeRoleDefinition>();role.id="server";role.displayName="Hire a server (pass to table)";role.hireCost=150;
            AssetDatabase.CreateAsset(role,folder+"/Server.asset");config.serverRole=role;
            AssetDatabase.CreateAsset(config,path);shift.dayDefinition=config;EditorUtility.SetDirty(shift);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("Service-day configuration and reusable purchase stations authored.");
        }
    }
}
