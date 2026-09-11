using UnityEngine;
namespace ThrownTogether
{
    // Presentation observes order phases; it never accepts dishes or changes customer timing.
    public sealed class CustomerPresentation : MonoBehaviour
    {
        private CustomerOrder order;
        private Transform left, right;
        private Material material;
        public ChefAppearance seatedVisual;
        public int variant;
        private Transform legLeft, legRight, armLeft, armRight, head;
        private Quaternion legLeftRest,legRightRest,armLeftRest,armRightRest,headRest;
        public void Initialize(CustomerOrder target,Carryable assets)
        {
            order=target; if(order.customerVisual==null) return;
            if(seatedVisual!=null)
            {
                seatedVisual.enabled=false;seatedVisual.usePlayerSelection=false;
                ApplyCustomerLook(seatedVisual,variant);
                foreach(var bone in seatedVisual.GetComponentsInChildren<Transform>(true))
                {
                    if(bone.name=="B_Leg_L"){legLeft=bone;legLeftRest=bone.localRotation;}
                    if(bone.name=="B_Leg_R"){legRight=bone;legRightRest=bone.localRotation;}
                    if(bone.name=="B_Arm_L"){armLeft=bone;armLeftRest=bone.localRotation;}
                    if(bone.name=="B_Arm_R"){armRight=bone;armRightRest=bone.localRotation;}
                    if(bone.name=="B_Head"){head=bone;headRest=bone.localRotation;}
                }
                ApplyPose(order.Phase,0,true);return;
            }
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
        public static void ApplyCustomerLook(ChefAppearance appearance,int variant)
        {
            var random=new System.Random(variant);
            var look=new ChefAppearanceData {build=random.Next(3),bodyColor=random.Next(ChefWardrobe.Colors.Length),clothing=0,headwear=0,
                hair=random.Next(1,ChefWardrobe.Hair.Length),hairColor=random.Next(ChefWardrobe.HairColors.Length),eyes=random.Next(ChefWardrobe.Eyes.Length),mouth=random.Next(ChefWardrobe.Mouths.Length),glasses=random.Next(2)};
            appearance.Apply(look);
            // Civilian palette/outfits are independent from both players' wardrobe choices.
            Color shirt=Color.HSVToRGB((float)random.NextDouble(),.48f,.68f);
            bool scarf=random.Next(2)==0;
            foreach(var renderer in appearance.GetComponentsInChildren<Renderer>(true))
            {
                bool torso=renderer.name.StartsWith("C_Torso"),trousers=renderer.name.StartsWith("C_Leg"),shoe=renderer.name.StartsWith("C_Foot");
                bool detail=renderer.name.StartsWith(scarf?"C_CustomerScarf":"C_CustomerVest") && renderer.name.EndsWith(look.build.ToString());
                if(detail)renderer.gameObject.SetActive(true);
                if(!torso&&!trousers&&!shoe&&!detail)continue;
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor",torso?shirt:shoe?new Color(.18f,.16f,.15f):detail?Color.Lerp(shirt,new Color(.98f,.76f,.35f),.65f):new Color(.20f,.25f,.31f));renderer.SetPropertyBlock(block);
            }
        }

        private void Update()
        {
            if(seatedVisual!=null){if(order!=null)ApplyPose(order.Phase,Time.time,RestaurantMenu.Display.reducedEffects);return;}
            if(left==null || right==null) return;
            float angle=0;
            if(!RestaurantMenu.Display.reducedEffects)
                angle=order.Phase==OrderPhase.Delivering ? 25 : order.Phase==OrderPhase.Eating ? 15+Mathf.Sin(Time.time*5)*8:0;
            left.localRotation=Quaternion.Euler(-angle,0,-angle*.5f); right.localRotation=Quaternion.Euler(-angle,0,angle*.5f);
        }
        // Observe state only. Chairs, colliders, order timers and food ownership never move here.
        public void ApplyPose(OrderPhase phase,float time,bool reducedEffects)
        {
            if(seatedVisual==null)return;
            float eating=phase==OrderPhase.Eating&&!reducedEffects?Mathf.Sin(time*5):0;
            Rotate(legLeft,legLeftRest,-78);Rotate(legRight,legRightRest,-78);
            Rotate(armLeft,armLeftRest,-48);
            Rotate(armRight,armRightRest,phase==OrderPhase.Eating?-65-eating*12:phase==OrderPhase.Delivering?-70:-48);
            Rotate(head,headRest,eating*3);
        }
        private void Rotate(Transform bone,Quaternion rest,float angle)
        {
            if(bone!=null)bone.localRotation=Quaternion.AngleAxis(angle,bone.parent.InverseTransformDirection(seatedVisual.transform.right))*rest;
        }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
