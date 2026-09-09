using UnityEngine;
namespace ThrownTogether
{
    // Presentation observes order phases; it never accepts dishes or changes customer timing.
    public sealed class CustomerPresentation : MonoBehaviour
    {
        private CustomerOrder order;
        private Transform left, right;
        private Material material;
        public void Initialize(CustomerOrder target,Carryable assets)
        {
            order=target; if(order.customerVisual==null) return;
            material=new Material(assets.visualShader);
            var root=order.customerVisual;
            for(int side=-1;side<=1;side+=2)
            {
                Piece(root,"Eyes",assets.sphereMesh,new Vector3(side*.105f,1.48f,-.226f),Vector3.one*.065f,Color.black);
                var arm=new GameObject("Arm pivot").transform; arm.SetParent(root,false); arm.localPosition=new Vector3(side*.36f,1.08f,-.03f);
                Piece(arm,"Sleeve",assets.cubeMesh,new Vector3(0,-.13f,0),new Vector3(.15f,.32f,.17f),new Color(.75f,.34f,.26f));
                Piece(arm,"Hand",assets.sphereMesh,new Vector3(0,-.33f,-.01f),Vector3.one*.15f,new Color(.93f,.77f,.57f));
                if(side<0) left=arm; else right=arm;
            }
        }
        private Transform Piece(Transform parent,string name,Mesh mesh,Vector3 position,Vector3 scale,Color color)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh; var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            var block=new MaterialPropertyBlock(); block.SetColor("_BaseColor",color); renderer.SetPropertyBlock(block); return go.transform;
        }
        private void Update()
        {
            if(left==null || right==null) return;
            float angle=0;
            if(!RestaurantMenu.Display.reducedEffects)
                angle=order.Phase==OrderPhase.Delivering ? 25 : order.Phase==OrderPhase.Eating ? 15+Mathf.Sin(Time.time*5)*8:0;
            left.localRotation=Quaternion.Euler(-angle,0,-angle*.5f); right.localRotation=Quaternion.Euler(-angle,0,angle*.5f);
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
