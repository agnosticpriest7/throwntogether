using UnityEngine;

namespace ThrownTogether
{
    public sealed class Carryable : MonoBehaviour
    {
        public ItemPayload Payload { get; private set; }
        public CarrySlot Owner { get; internal set; }
        public Mesh sphereMesh, cubeMesh, cylinderMesh;
        public Mesh plateMesh;
        public Mesh mushroomCapMesh;
        public Shader visualShader;
        private Transform visuals;
        private Material material;
        private Vector3 ingredientOffset;
        private float ingredientScale=1;
        public void Configure(ItemPayload payload) { Payload = payload; RefreshVisual(); }
        public void RefreshVisual()
        {
            if (visuals != null) { visuals.gameObject.SetActive(false); Destroy(visuals.gameObject); }
            if (material != null) Destroy(material);
            visuals = new GameObject("Item visual").transform;
            visuals.SetParent(transform, false);
            // Presentation scale only: ownership, carry anchors and interaction reach are unchanged.
            visuals.localScale=Vector3.one*1.12f;
            material = new Material(visualShader);
            ingredientOffset=Vector3.zero;
            ingredientScale=1;
            if (Payload.isPlate)
            {
                if(plateMesh!=null) MeshPiece(plateMesh,Vector3.zero,Vector3.one,new Color(.91f,.94f,.90f));
                else
                {
                    Piece(PrimitiveType.Cylinder, Vector3.zero, new Vector3(.82f,.035f,.82f), new Color(.78f,.84f,.88f));
                    Piece(PrimitiveType.Cylinder, new Vector3(0,.025f,0), new Vector3(.71f,.026f,.71f), Color.white);
                }
            }
            if(Payload.dirty)
            {
                for(int i=0;i<3;i++) Piece(PrimitiveType.Sphere,new Vector3((i-1)*.18f,.065f,(i%2-.5f)*.2f),new Vector3(.23f,.018f,.17f),new Color(.37f,.23f,.09f));
                return;
            }
            if (Payload.ingredient == null) return;
            bool salad=Payload.IngredientCount==2 && Payload.state==FoodState.Cut && Payload.additions[0].state==FoodState.Cut &&
                ((Payload.ingredient.visualKind==IngredientVisualKind.Tomato && Payload.additions[0].ingredient.visualKind==IngredientVisualKind.Lettuce) ||
                 (Payload.ingredient.visualKind==IngredientVisualKind.Lettuce && Payload.additions[0].ingredient.visualKind==IngredientVisualKind.Tomato));
            if(Payload.additions.Count>0 && !salad)
            {
                var portions=new System.Collections.Generic.List<IngredientPortion>(Payload.additions){new IngredientPortion{ingredient=Payload.ingredient,state=Payload.state}};
                portions.Sort((a,b)=>a.ingredient.visualKind.CompareTo(b.ingredient.visualKind));
                ingredientScale=.65f;
                for(int i=0;i<portions.Count;i++)
                {float angle=i*Mathf.PI*2/portions.Count;ingredientOffset=new Vector3(Mathf.Cos(angle)*.2f,.04f,Mathf.Sin(angle)*.2f);DrawIngredient(portions[i].ingredient,portions[i].state);}
                return;
            }
            if(Payload.additions.Count==1 && Payload.ingredient.visualKind==IngredientVisualKind.Tomato && Payload.additions[0].ingredient.visualKind==IngredientVisualKind.Lettuce)
            {
                DrawIngredient(Payload.additions[0].ingredient,Payload.additions[0].state);
                ingredientOffset=new Vector3(.03f,.085f,.02f);DrawIngredient(Payload.ingredient,Payload.state);return;
            }
            DrawIngredient(Payload.ingredient,Payload.state);
            foreach(var part in Payload.additions) {ingredientOffset=new Vector3(.03f,.085f,.02f);DrawIngredient(part.ingredient,part.state);}
        }
        private void DrawIngredient(IngredientDefinition ingredient,FoodState state)
        {
            var color = ingredient.ColorFor(state);
            var y = Payload.isPlate ? .10f : .08f;
            if(ingredient.visualKind==IngredientVisualKind.Egg)
            {
                if(state==FoodState.Raw)Piece(PrimitiveType.Sphere,new Vector3(0,y+.1f,0),new Vector3(.33f,.43f,.33f),new Color(.95f,.90f,.78f));
                else
                {Piece(PrimitiveType.Sphere,new Vector3(0,y,0),new Vector3(.6f,.06f,.5f),Color.white);Piece(PrimitiveType.Sphere,new Vector3(.02f,y+.06f,0),new Vector3(.25f,.12f,.25f),new Color(1,.68f,.04f));}
            }
            else if(ingredient.visualKind==IngredientVisualKind.Chicken)
            {
                Piece(PrimitiveType.Sphere,new Vector3(0,y+.04f,0),new Vector3(.49f,.22f,.57f),color).localRotation=Quaternion.Euler(0,-20,0);
                Piece(PrimitiveType.Sphere,new Vector3(.12f,y+.03f,-.19f),new Vector3(.24f,.14f,.3f),color);
                if(state==FoodState.Grilled)for(int i=-1;i<=1;i++)Piece(PrimitiveType.Cube,new Vector3(0,y+.153f,i*.12f),new Vector3(.32f,.012f,.035f),new Color(.24f,.12f,.06f)).localRotation=Quaternion.Euler(0,-20,0);
            }
            else if(ingredient.visualKind==IngredientVisualKind.Potato && state==FoodState.Griddled)
            {
                for(int i=-1;i<=1;i++)Piece(PrimitiveType.Sphere,new Vector3(i*.14f,y+i*.016f,0),new Vector3(.26f,.085f,.46f),color);
            }
            else if(ingredient.visualKind==IngredientVisualKind.Lettuce)
            {
                for(int i=0;i<5;i++)
                {
                    float a=i*Mathf.PI*2/5;var pos=new Vector3(Mathf.Cos(a)*.18f,y+(i%2)*.025f,Mathf.Sin(a)*.18f);
                    Piece(PrimitiveType.Sphere,pos,state==FoodState.Raw ? new Vector3(.44f,.27f,.42f):new Vector3(.33f,.06f,.27f),i%2==0 ? color:new Color(.45f,.78f,.17f)).localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                    if(state!=FoodState.Raw) Piece(PrimitiveType.Cube,pos+Vector3.up*.031f,new Vector3(.18f,.008f,.014f),new Color(.55f,.75f,.29f)).localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                }
            }
            else if(ingredient.visualKind==IngredientVisualKind.Tomato)
            {
                if(state==FoodState.Raw)
                {
                    Piece(PrimitiveType.Sphere,new Vector3(0,y+.08f,0),new Vector3(.52f,.43f,.52f),color);
                    Piece(PrimitiveType.Cube,new Vector3(0,y+.30f,0),new Vector3(.32f,.035f,.075f),new Color(.15f,.48f,.12f));
                    Piece(PrimitiveType.Cube,new Vector3(0,y+.30f,0),new Vector3(.075f,.035f,.32f),new Color(.15f,.48f,.12f));
                }
                else for(int i=0;i<3;i++)
                {
                    float size=Payload.IngredientCount>1?.82f:1;
                    var p=new Vector3((i-1)*.18f*size,y+i*.018f,(i%2-.5f)*.16f*size);
                    Piece(PrimitiveType.Cylinder,p,new Vector3(.39f*size,.025f,.39f*size),color);
                    Piece(PrimitiveType.Cylinder,p+Vector3.up*.027f,new Vector3(.29f*size,.007f,.29f*size),new Color(1,.48f,.27f));
                    for(int seed=0;seed<3;seed++)
                    {
                        float a=seed*Mathf.PI*2/3;
                        Piece(PrimitiveType.Sphere,p+new Vector3(Mathf.Cos(a)*.078f*size,.038f,Mathf.Sin(a)*.078f*size),new Vector3(.035f*size,.014f,.05f*size),new Color(1,.85f,.47f));
                    }
                }
            }
            else if(ingredient.visualKind==IngredientVisualKind.Mushroom)
            {
                if(state==FoodState.Raw)
                {
                    Piece(PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(.16f,.18f,.16f),new Color(.9f,.84f,.7f));
                    Piece(PrimitiveType.Sphere,new Vector3(0,y+.16f,0),new Vector3(.52f,.065f,.52f),new Color(.46f,.34f,.22f));
                    Piece(PrimitiveType.Sphere,new Vector3(0,y+.25f,0),new Vector3(.58f,.28f,.58f),color);
                }
                else for(int i=0;i<4;i++)
                {
                    var p=new Vector3((i%2-.5f)*.28f,y+(i%2)*.012f,(i/2-.5f)*.29f);
                    Piece(PrimitiveType.Cube,p+new Vector3(0,0,-.025f),new Vector3(.09f,.065f,.13f),state==FoodState.Cooked?new Color(.68f,.47f,.25f):new Color(.9f,.84f,.7f));
                    MeshPiece(mushroomCapMesh!=null?mushroomCapMesh:sphereMesh,p+new Vector3(0,.018f,.015f),new Vector3(.26f,.075f,.38f),color);
                    MeshPiece(mushroomCapMesh!=null?mushroomCapMesh:sphereMesh,p+new Vector3(0,.054f,.03f),new Vector3(.18f,.014f,.25f),state==FoodState.Cooked?new Color(.51f,.32f,.16f):new Color(.76f,.64f,.46f));
                }
            }
            else if (state == FoodState.Raw)
            {
                Piece(PrimitiveType.Sphere, new Vector3(0,y,0), new Vector3(.43f,.32f,.55f), color);
                for(int i=0;i<3;i++) Piece(PrimitiveType.Sphere,new Vector3((i-1)*.1f,y+.155f,(i%2-.5f)*.18f),new Vector3(.04f,.02f,.04f),new Color(.28f,.16f,.06f));
            }
            else
                for (int i=0;i<5;i++)
                {
                    var p=new Vector3((i-2)*.10f,y+(i%2)*.024f,(i%2)*.04f);
                    var tint=i%2==0?color:Color.Lerp(color,new Color(1,.89f,.48f),.2f);
                    Piece(PrimitiveType.Cube,p,new Vector3(.073f,.085f,.39f+(i%3)*.035f),tint).localRotation=Quaternion.Euler(0,(i-2)*7,0);
                }
        }
        private Transform Piece(PrimitiveType type, Vector3 position, Vector3 scale, Color color)
            => MeshPiece(type==PrimitiveType.Sphere?sphereMesh:type==PrimitiveType.Cylinder?cylinderMesh:cubeMesh,position,scale,color);
        private Transform MeshPiece(Mesh mesh, Vector3 position, Vector3 scale, Color color)
        {
            // Visual-only meshes: avoid CreatePrimitive's implicit collider types,
            // which can be stripped from players when no authored object uses them.
            var part = new GameObject("Food part");
            part.AddComponent<MeshFilter>().sharedMesh=mesh;
            part.transform.SetParent(visuals, false); part.transform.localPosition=position*ingredientScale+ingredientOffset; part.transform.localScale=scale*ingredientScale;
            var renderer=part.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            var properties=new MaterialPropertyBlock(); properties.SetColor("_BaseColor",color); renderer.SetPropertyBlock(properties);
            return part.transform;
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
