using System.Collections.Generic;
using UnityEngine;

namespace ThrownTogether
{
    // Visual-only component: never scales the motor, carry slot, or interaction reach.
    public sealed class ChefAppearance : MonoBehaviour
    {
        public int playerIndex;
        public bool usePlayerSelection=true;
        public ChefAppearanceData appearance=new ChefAppearanceData();
        private readonly Dictionary<string,Renderer> parts=new Dictionary<string,Renderer>();
        private readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        private readonly Dictionary<string,Quaternion> restRotations=new Dictionary<string,Quaternion>();
        private ChefController chef;
        private Vector3 lastPosition;
        private float stride;
        public float CarryPose { get; private set; }
        private static readonly Color[] bodyColors={new Color(.22f,.66f,.64f),new Color(.98f,.62f,.47f),new Color(.66f,.53f,.79f),new Color(.43f,.68f,.89f)};
        private static readonly Color[] clothColors={new Color(.96f,.87f,.68f),new Color(.14f,.20f,.34f),new Color(1,.81f,.40f),new Color(.69f,.30f,.15f)};
        private void Awake() { Cache(); chef=GetComponentInParent<ChefController>(); lastPosition=transform.position; }
        private void Start() => Apply(usePlayerSelection ? ChefWardrobe.ForPlayer(playerIndex) : appearance);
        private void Cache()
        {
            if(parts.Count>0) return;
            foreach(var r in GetComponentsInChildren<Renderer>(true)) if(r.name.StartsWith("C_")) parts[r.name]=r;
            foreach(var t in GetComponentsInChildren<Transform>(true)) if(t.name.StartsWith("B_"))
            { bones[t.name]=t; restRotations[t.name]=t.localRotation; }
        }
        public void Apply(ChefAppearanceData value)
        {
            Cache(); appearance=value.Copy(); appearance.Normalize(); var a=appearance;
            foreach(var entry in parts)
            {
                string n=entry.Key; bool visible=true;
                if(n.StartsWith("C_Torso")) visible=n=="C_Torso"+a.build;
                else if(n.StartsWith("C_Waist")) visible=a.clothing==1 && n.EndsWith(a.build.ToString());
                else if(n.StartsWith("C_BibStrap")) visible=a.clothing==2 && n.StartsWith("C_BibStrap"+a.build);
                else if(n.StartsWith("C_Bib")) visible=a.clothing==2 && n.EndsWith(a.build.ToString());
                else if(n.StartsWith("C_Eyes")) visible=n=="C_Eyes"+a.eyes;
                else if(n.StartsWith("C_Mouth")) visible=n=="C_Mouth"+a.mouth;
                else if(n=="C_Tooth3") visible=a.mouth==3;
                else if(n.StartsWith("C_Cap")) visible=a.headwear==1;
                else if(n.StartsWith("C_Beanie")) visible=a.headwear==2;
                else if(n.StartsWith("C_Headband")) visible=a.headwear==3;
                else if(n.StartsWith("C_Hair")) visible=a.hair==1 && a.headwear==0;
                else if(n.StartsWith("C_Glasses")) visible=a.glasses==1;
                entry.Value.gameObject.SetActive(visible);
                var block=new MaterialPropertyBlock();
                bool body=n=="C_Head" || n.StartsWith("C_Torso") || n.StartsWith("C_Arm") || n.StartsWith("C_Leg") || n.StartsWith("C_Foot");
                bool clothing=n.StartsWith("C_Waist") || n.StartsWith("C_Bib");
                bool headwear=n.StartsWith("C_Cap") || n.StartsWith("C_Beanie") || n.StartsWith("C_Headband");
                if(body || clothing || headwear)
                {
                    Color hatColor=a.headwear==2 ? new Color(.23f,.39f,.65f):a.headwear==3 ? new Color(.91f,.38f,.33f):new Color(.91f,.61f,.21f);
                    block.SetColor("_BaseColor",body ? bodyColors[a.bodyColor]:clothing ? clothColors[a.clothingColor]:hatColor);
                    entry.Value.SetPropertyBlock(block);
                }
            }
            Pose(CarryPose,0,0);
        }
        private void LateUpdate()
        {
            float distance=Vector3.Distance(transform.position,lastPosition); lastPosition=transform.position;
            if(Time.deltaTime<=0) return;
            bool moving=distance>.0001f && distance<1;
            if(moving) stride+=distance*10;
            Pose(chef!=null && chef.Hands.Item!=null ? 1:0,moving ? Mathf.Sin(stride)*18:0,Time.deltaTime);
        }
        public void Pose(float carrying,float swing,float delta)
        {
            Cache(); CarryPose=Mathf.MoveTowards(CarryPose,carrying,delta*7);
            foreach(string side in new[]{"L","R"})
            {
                float sign=side=="L" ? 1:-1;
                string arm="B_Arm_"+side,leg="B_Leg_"+side;
                if(bones.TryGetValue(arm,out var t))
                {
                    // Imported Blender bones have local Y down the limb; rotate in character space.
                    var axis=t.parent.InverseTransformDirection(transform.right);
                    t.localRotation=Quaternion.AngleAxis(-CarryPose*92+(1-CarryPose)*swing*sign,axis)*restRotations[arm];
                    t.position=transform.TransformPoint(new Vector3(sign*(.34f+(appearance.build-1)*.035f),1.02f,0));
                }
                if(bones.TryGetValue(leg,out var l)) l.localRotation=Quaternion.AngleAxis(-swing*sign,l.parent.InverseTransformDirection(transform.right))*restRotations[leg];
            }
        }
        public bool IsVisible(string name) { Cache(); return parts.TryGetValue(name,out var r) && r.gameObject.activeSelf; }
    }
}
