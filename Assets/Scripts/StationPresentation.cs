using UnityEngine;
namespace ThrownTogether
{
    // Three cached meshes per station; visuals never move food, colliders or timers.
    public sealed class StationPresentation : MonoBehaviour
    {
        private ProcessingStation station;
        private Transform[] pieces;
        private Material material;
        public void Initialize(ProcessingStation target,Carryable assets)
        {
            station=target; material=new Material(assets.visualShader);
            bool chopping=station.requiresAttendance;
            pieces=new Transform[chopping ? 1:3];
            for(int i=0;i<pieces.Length;i++)
            {
                var go=new GameObject(chopping ? "Prep blade feedback":"Fryer steam feedback");
                pieces[i]=go.transform; go.transform.SetParent(transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=chopping ? assets.cubeMesh:assets.sphereMesh;
                var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
                var block=new MaterialPropertyBlock(); block.SetColor("_BaseColor",chopping ? new Color(.8f,.85f,.9f):new Color(.87f,.93f,.89f)); renderer.SetPropertyBlock(block);
                go.transform.localScale=chopping ? new Vector3(.55f,.2f,.05f):Vector3.one*.13f;
                go.SetActive(false);
            }
        }
        private void Update()
        {
            if(station==null || pieces==null) return;
            bool visible=station.Working && !RestaurantMenu.Display.reducedEffects;
            for(int i=0;i<pieces.Length;i++)
            {
                pieces[i].gameObject.SetActive(visible);
                if(!visible) continue;
                pieces[i].localPosition=pieces.Length==1 ? new Vector3(.2f,1.55f+Mathf.Abs(Mathf.Sin(Time.time*12))*.25f,0) :
                    new Vector3((i-1)*.3f,1.5f+Mathf.Repeat(Time.time*.65f+i*.3f,.65f),.1f);
            }
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
