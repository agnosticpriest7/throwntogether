using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ThrownTogether.Editor
{
 public static class MoveDiningEntrance
 {
  public static void ApplyAndCapture()
  {
   if(!Application.isBatchMode)throw new Exception("Use isolated batch authoring");
   foreach(var name in new[]{"RestaurantDevelopment","RestaurantShift"})
   {
    var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
    var room=GameObject.Find("Restaurant room art").transform;
    var entrance=room.Find("EntranceCutaway");
    if(Mathf.Abs(entrance.position.x+1.7f)>.01f)throw new Exception("Entrance has changed; inspect before rerunning");
    // Replace only the visual front infill. Preserve the existing perimeter collision.
    foreach(var t in room.Cast<Transform>().Where(t=>t.name=="WallLow"&&Mathf.Abs(t.localPosition.z+5.6f)<.01f&&Mathf.Abs(t.localPosition.x)<8.3f).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
    foreach(var segment in new[]{new Vector2(-8.4f,5.2f),new Vector2(7.4f,8.4f)})
    for(float x=segment.x;x<segment.y-.001f;x+=1.9f)
    {
     float width=Mathf.Min(1.9f,segment.y-x);
     var part=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RestaurantRoom/Prefabs/WallLow.prefab"));
     part.transform.SetParent(room,false);part.transform.localPosition=new Vector3(x+width/2,0,-5.6f);part.transform.localScale=new Vector3(width/1.9f,.32f,1);
    }
    entrance.localPosition=new Vector3(6.3f,0,-5.6f);
    var details=GameObject.Find("Restaurant finishing details").transform;
    var exterior=details.Find("Exterior framing");
    foreach(var module in new[]{"EntranceMat","EntranceAwning"}){var t=exterior.Find(module);var p=t.localPosition;p.x=6.3f;t.localPosition=p;}
    var plant=exterior.Find("PottedPlant");var plantPosition=plant.localPosition;plantPosition.x=4.4f;plant.localPosition=plantPosition;
    foreach(Transform t in exterior)if(t.name=="LowPlanter"&&t.localPosition.x>0){var p=t.localPosition;p.x=1.7f;t.localPosition=p;}
    foreach(var lamp in details.Find("Perimeter decoration").Cast<Transform>().Where(t=>t.name=="WallSconce")){var p=lamp.localPosition;p.x+=8;lamp.localPosition=p;}
    EditorSceneManager.SaveScene(scene);
   }
   AuthorRestaurantDetails.Capture();
  }
 }
}
