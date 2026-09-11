using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    [Serializable] public sealed class FurniturePlacement { public int kitchen,slot,turns; public string id; }

    // Stable numbered bays follow Kyle's red-numbered plan. Gameplay geometry stays authoritative.
    public sealed class KitchenFurniture : MonoBehaviour
    {
        public static readonly Vector3[] Slots={
            new Vector3(2.3f,0,5.8f),new Vector3(.4f,0,5.8f),new Vector3(-1.5f,0,5.8f),new Vector3(-3.4f,0,5.8f),new Vector3(-5.3f,0,5.8f),new Vector3(-7.2f,0,5.8f),
            new Vector3(-7.2f,0,3.2f),new Vector3(-7.2f,0,.65f),new Vector3(-7.2f,0,-1.9f),new Vector3(-7.2f,0,-4.4f),
            new Vector3(-5.3f,0,-4.4f),new Vector3(-3.4f,0,-4.4f),
            new Vector3(-.8f,0,-1.4f),new Vector3(-.8f,0,.55f),new Vector3(-.8f,0,2.5f),
            new Vector3(-2.75f,0,2.5f),new Vector3(-2.75f,0,.55f),new Vector3(-2.75f,0,-1.4f),
            new Vector3(3.6f,0,.75f),new Vector3(3.6f,0,-1.2f)
        };
        sealed class Piece { public string id; public Transform root; public Vector3 before; public Quaternion rotation; public int slot=-1,turns; }
        readonly List<Piece> pieces=new List<Piece>();
        KitchenLayout layout; RestaurantDay day;
        public bool Editing {get;private set;}
        public int Selected {get;private set;}
        public int Held {get;private set;}=-1;
        int pendingTurns;
        public string Message {get;private set;}="";
        public int Count=>pieces.Count;
        public int MoveFee=>RestaurantAccounts.Current.ArrangementFee;
        public int AssignedSlot(string id)=>pieces.First(p=>p.id==id).slot;
        public bool CanEdit=>day!=null && (day.AwaitingMenu || day.Closed && day.Paid) && GetComponent<DayPresentation>()?.Transitioning!=true;
        public void Initialize(KitchenLayout value)
        {
            layout=value;day=GetComponent<RestaurantDay>();
            foreach(int index in new[]{0,1,3,4,5,6,7,8,10,11})
                if(layout!=null && index<layout.anchors.Length && layout.anchors[index].GetComponentInChildren<Interactable>()!=null)Register("base:"+index,layout.anchors[index]);
        }
        public void Register(string id,Transform root)
        {
            if(pieces.Any(p=>p.id==id))return;
            var piece=new Piece{id=id,root=root,before=root.position,rotation=root.rotation,slot=-1};
            int[] anchors={0,1,3,4,5,6,7,8,10,11}, bays={5,4,7,2,14,12,13,18,8,19};
            int preferred=-1;
            for(int i=0;i<anchors.Length;i++)if(id=="base:"+anchors[i])preferred=bays[i];
            if(preferred<0 || At(preferred)!=null)preferred=Enumerable.Range(0,Slots.Length).Where(i=>At(i)==null).OrderBy(i=>Vector3.SqrMagnitude(Slots[i]-root.position)).First();
            pieces.Add(piece);Place(piece,preferred,0);piece.before=root.position;piece.rotation=root.rotation;
        }
        static int Nearest(Vector3 position)
        {for(int i=0;i<Slots.Length;i++)if(Vector3.Distance(position,Slots[i])<.15f)return i;return -1;}
        Piece At(int slot)=>pieces.FirstOrDefault(p=>p.slot==slot);
        public Transform Find(string id)=>pieces.FirstOrDefault(p=>p.id==id)?.root;
        public void ApplySaved()
        {
            var records=RestaurantAccounts.Current.Data.furniture??new FurniturePlacement[0];
            var applicable=records.Where(p=>p!=null && p.kitchen==SessionOptions.Kitchen).ToArray();
            if(applicable.Length==0)return;
            var old=pieces.Select(p=>(p.root.position,p.root.rotation,p.slot,p.turns)).ToArray();
            // Legacy partial saves must not collide with newly snapped default equipment.
            foreach(var saved in applicable.Where(x=>x.slot>=0 && x.slot<Slots.Length)){var owner=pieces.FirstOrDefault(p=>p.id==saved.id);var occupant=At(saved.slot);if(owner!=null && occupant!=null && occupant!=owner && !applicable.Any(x=>x.id==occupant.id)){int free=Enumerable.Range(0,Slots.Length).First(i=>At(i)==null && !applicable.Any(x=>x.slot==i));Place(occupant,free,0);}}

            bool valid=applicable.Select(p=>p.id).Distinct().Count()==applicable.Length && applicable.Select(p=>p.slot).Distinct().Count()==applicable.Length;
            foreach(var saved in applicable)
            {
                var piece=pieces.FirstOrDefault(p=>p.id==saved.id);
                if(piece==null)continue; // Unknown/removed ownership never spawns free equipment.
                if(saved.slot<0 || saved.slot>=Slots.Length || saved.turns<0 || saved.turns>3){valid=false;break;}
                Place(piece,saved.slot,saved.turns);
            }
            if(!valid || !Validate(out var unused))
            {for(int i=0;i<pieces.Count;i++){var p=pieces[i];p.root.SetPositionAndRotation(old[i].position,old[i].rotation);p.slot=old[i].slot;p.turns=old[i].turns;}Physics.SyncTransforms();Message="Saved layout no longer fits; original positions restored. Your save is preserved.";}
        }
        void IncludeNewPurchases()
        {
            foreach(var offer in day.Settings.purchases)
                if(offer!=null && offer.stationPrefab!=null && RestaurantAccounts.Current.Owns(offer.id) && Find("purchase:"+offer.id)==null)
                {
                    var instance=Instantiate(offer.stationPrefab,offer.layoutPositions[Mathf.Clamp(SessionOptions.Kitchen,0,offer.layoutPositions.Length-1)],Quaternion.identity);instance.name=offer.displayName;Register("purchase:"+offer.id,instance.transform);
                }
        }
        public bool Begin()
        {
            if(!CanEdit)return false;
            IncludeNewPurchases();
            foreach(var p in pieces){p.before=p.root.position;p.rotation=p.root.rotation;}
            Editing=true;Held=-1;Selected=0;Message="A: pick/place • X: rotate • RB: equipment • Y: save • B: cancel | $"+MoveFee+" per break, only for saved changes";return true;
        }
        static void Place(Piece p,int slot,int turns)
        {p.slot=slot;p.turns=turns;p.root.SetPositionAndRotation(Slots[slot],Quaternion.Euler(0,turns*90,0));Physics.SyncTransforms();}
        public bool TryMove(string id,int slot,int turns)
        {
            if(!Editing || !CanEdit || slot<0 || slot>=Slots.Length || turns<0 || turns>3)return false;
            var p=pieces.FirstOrDefault(v=>v.id==id);if(p==null)return false;
            var other=At(slot);if(other==p)other=null;
            var oldPosition=p.root.position;var oldRotation=p.root.rotation;int oldSlot=p.slot,oldTurns=p.turns;
            var otherPosition=other!=null?other.root.position:Vector3.zero;var otherRotation=other!=null?other.root.rotation:Quaternion.identity;int otherTurns=other!=null?other.turns:0;
            if(other!=null && oldSlot<0){Message="Move this equipment to an empty slot before swapping.";return false;}
            Place(p,slot,turns);if(other!=null)Place(other,oldSlot,other.turns);
            if(Validate(out var problem)){Message="Placed. Save layout when finished.";return true;}
            p.root.SetPositionAndRotation(oldPosition,oldRotation);p.slot=oldSlot;p.turns=oldTurns;
            if(other!=null){other.root.SetPositionAndRotation(otherPosition,otherRotation);other.slot=slot;other.turns=otherTurns;}
            Physics.SyncTransforms();Message=problem;return false;
        }
        public bool Validate(out string problem)
        {
            Physics.SyncTransforms();
            foreach(var p in pieces)
            foreach(var box in p.root.GetComponentsInChildren<BoxCollider>())
            {
                if(!box.enabled || box.isTrigger)continue;
                var half=Vector3.Scale(box.size,box.transform.lossyScale)*.5f-Vector3.one*.025f;
                half=Vector3.Max(half,Vector3.one*.01f);
                foreach(var hit in Physics.OverlapBox(box.transform.TransformPoint(box.center),half,box.transform.rotation,~0,QueryTriggerInteraction.Ignore))
                    if(hit.gameObject.scene==gameObject.scene && !hit.transform.IsChildOf(p.root) && hit.GetComponentInParent<ChefController>()==null && !(hit is CharacterController))
                    {problem="That position overlaps "+hit.name+" ("+p.root.name+").";return false;}
            }
            foreach(var spawn in new[]{layout.choices[SessionOptions.Kitchen].playerOneSpawn,layout.choices[SessionOptions.Kitchen].playerTwoSpawn})
                if(!KitchenStaffRoute.Clear(spawn)){problem="Keep both player starting positions clear.";return false;}
            var start=layout.choices[SessionOptions.Kitchen].playerOneSpawn;
            foreach(var target in Interactable.Active.Where(s=>s.gameObject.scene==gameObject.scene && !(s is DiningTable)))
                if(KitchenStaffRoute.ToStation(start,target.transform)==null){problem="Keep a clear walking route to every station and serving area.";return false;}
            problem="";return true;
        }
        public bool Save()
        {
            if(Held>=0){Message="Place or cancel the selected equipment before saving.";return false;}
            if(!Editing || !CanEdit || !Validate(out var problem)){Message="Layout is not available or blocked; keep routes and player spawns clear.";return false;}
            var records=pieces.Where(p=>p.slot>=0).Select(p=>new FurniturePlacement{kitchen=SessionOptions.Kitchen,id=p.id,slot=p.slot,turns=p.turns}).ToArray();
            bool changed=pieces.Any(p=>Vector3.Distance(p.before,p.root.position)>.01f || Quaternion.Angle(p.rotation,p.root.rotation)>.1f);
            if(!RestaurantAccounts.Current.SetFurniture(SessionOptions.Kitchen,records,changed)){Message=RestaurantAccounts.Current.Problem;return false;}
            Editing=false;Held=-1;return true;
        }
        public void Cancel()
        {
            if(!Editing)return;
            foreach(var p in pieces){if(p.root==null)continue;p.root.SetPositionAndRotation(p.before,p.rotation);p.slot=Nearest(p.before);p.turns=(Mathf.RoundToInt(p.rotation.eulerAngles.y/90)%4+4)%4;}
            Physics.SyncTransforms();Editing=false;Held=-1;
        }
        public void Select(int index){Selected=Mathf.Clamp(index,0,Slots.Length+1);}
        public void Confirm()
        {
            if(Selected==Slots.Length){Save();return;}
            if(Selected==Slots.Length+1){Cancel();return;}
            if(Held<0){var p=At(Selected);if(p!=null){Held=pieces.IndexOf(p);pendingTurns=p.turns;}else Message="Empty slot. Select equipment first.";}
            else if(TryMove(pieces[Held].id,Selected,pendingTurns))Held=-1;
        }
        public void Rotate(){if(Held>=0)pendingTurns=(pendingTurns+1)%4;else Message="Select equipment first to rotate it.";}
        public void CyclePiece(){if(pieces.Count==0)return;Held=(Held+1)%pieces.Count;pendingTurns=pieces[Held].turns;if(pieces[Held].slot>=0)Selected=pieces[Held].slot;}
        public bool Back(){if(Held>=0){Held=-1;return false;}Cancel();return true;}
        public void Navigate(Vector2 direction)
        {
            Vector2 Point(int i){if(i==Slots.Length)return new Vector2(1060,670);if(i==Slots.Length+1)return new Vector2(1210,670);var p=GetComponent<RestaurantHud>().gameplayCamera.WorldToViewportPoint(Slots[i]);return new Vector2(p.x*1280,p.y*720);}
            var origin=Point(Selected);float best=float.MaxValue;int chosen=Selected;
            for(int i=0;i<Slots.Length+2;i++)
            {var delta=Point(i)-origin;float along=Vector2.Dot(delta,direction.normalized);if(along<5)continue;float side=Mathf.Abs(delta.x*direction.y-delta.y*direction.x);float score=delta.magnitude+side*2;if(score<best){best=score;chosen=i;}}
            Selected=chosen;
        }

        public void Draw(Action saved,Action cancelled)
        {
            var camera=GetComponent<RestaurantHud>().gameplayCamera;
            var prior=GUI.color;var matrix=GUI.matrix;GUI.depth=-150;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            var style=new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter};style.normal.textColor=Color.white;
            GUI.Box(new Rect(15,10,1280-30,76),"KITCHEN LAYOUT — Move / D-pad   A: select/place   X / R: rotate   RB / Tab: equipment   Y: save   B: back");
            GUI.Label(new Rect(30,40,1280-360,40),Message);
            GUI.backgroundColor=Selected==Slots.Length?Color.yellow:Color.white;
            if(GUI.Button(new Rect(1280-315,44,140,30),"Save ($"+MoveFee+")") && Save())saved();
            GUI.backgroundColor=Selected==Slots.Length+1?Color.yellow:Color.white;
            if(GUI.Button(new Rect(1280-165,44,140,30),"Cancel")){Cancel();cancelled();}
            GUI.backgroundColor=Color.white;
            for(int i=0;i<Slots.Length;i++)
            {
                var screen=camera.WorldToScreenPoint(Slots[i]+Vector3.up*1.15f);var occupant=At(i);
                var rect=new Rect(screen.x*1280f/Screen.width-29,720-screen.y*720f/Screen.height-25,58,50);
                var tint=i==Selected?new Color(1,.85f,.2f):occupant==null?new Color(.15f,.95f,.3f):new Color(.25f,.7f,1);
                GUI.color=tint;GUI.DrawTexture(rect,Texture2D.whiteTexture);
                GUI.color=new Color(.02f,.07f,.09f,.9f);GUI.DrawTexture(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),Texture2D.whiteTexture);GUI.color=Color.white;
                if(GUI.Button(rect,(i+1).ToString(),style)){Selected=i;Confirm();}
            }
            GUI.color=Color.white;
            string name=Held>=0?pieces[Held].root.name:At(Selected)?.root.name??"Empty";
            GUI.Box(new Rect(1280*.2f,720-74,1280*.6f,48),(Selected>=Slots.Length?(Selected==Slots.Length?"A: save layout":"A: cancel changes"):"Slot "+(Selected+1)+" — "+name)+(Held>=0?" | Rotation "+(pendingTurns*90)+"°":""));
            // Out-of-plan equipment in alternate/older layouts remains selectable for its first move.
            int row=0;foreach(var p in pieces.Where(p=>p.slot<0))
                if(GUI.Button(new Rect(1280-245,100+row++*38,225,34),"Move "+p.root.name)){Held=pieces.IndexOf(p);pendingTurns=p.turns;}
            GUI.color=prior;GUI.matrix=matrix;GUI.depth=0;
        }
    }
}
