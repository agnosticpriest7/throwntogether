using UnityEngine;

namespace ThrownTogether
{
    public sealed class Carryable : MonoBehaviour
    {
        public ItemPayload Payload { get; private set; }
        public CarrySlot Owner { get; internal set; }
        public Mesh sphereMesh, cubeMesh, cylinderMesh;
        public Shader visualShader;
        private Transform visuals;
        private Material material;
        private Vector3 ingredientOffset;
        public void Configure(ItemPayload payload) { Payload = payload; RefreshVisual(); }
        public void RefreshVisual()
        {
            if (visuals != null) { visuals.gameObject.SetActive(false); Destroy(visuals.gameObject); }
            if (material != null) Destroy(material);
            visuals = new GameObject("Item visual").transform;
            visuals.SetParent(transform, false);
            visuals.localScale=Vector3.one*1.35f;
            material = new Material(visualShader);
            ingredientOffset=Vector3.zero;
            if (Payload.isPlate)
            {
                Piece(PrimitiveType.Cylinder, Vector3.zero, new Vector3(.82f,.035f,.82f), new Color(.78f,.84f,.88f));
                Piece(PrimitiveType.Cylinder, new Vector3(0,.025f,0), new Vector3(.71f,.026f,.71f), Color.white);
            }
            if(Payload.dirty)
            {
                for(int i=0;i<3;i++) Piece(PrimitiveType.Sphere,new Vector3((i-1)*.18f,.065f,(i%2-.5f)*.2f),new Vector3(.23f,.018f,.17f),new Color(.37f,.23f,.09f));
                return;
            }
            if (Payload.ingredient == null) return;
            if(Payload.additions.Count==1 && Payload.ingredient.visualKind==IngredientVisualKind.Tomato && Payload.additions[0].ingredient.visualKind==IngredientVisualKind.Lettuce)
            {
                DrawIngredient(Payload.additions[0].ingredient,Payload.additions[0].state);
                ingredientOffset=new Vector3(.06f,.14f,.03f);DrawIngredient(Payload.ingredient,Payload.state);return;
            }
            DrawIngredient(Payload.ingredient,Payload.state);
            foreach(var part in Payload.additions) {ingredientOffset=new Vector3(.06f,.14f,.03f);DrawIngredient(part.ingredient,part.state);}
        }
        private void DrawIngredient(IngredientDefinition ingredient,FoodState state)
        {
            var color = ingredient.ColorFor(state);
            var y = Payload.isPlate ? .13f : .08f;
            if(ingredient.visualKind==IngredientVisualKind.Lettuce)
            {
                for(int i=0;i<5;i++)
                {
                    float a=i*Mathf.PI*2/5;var pos=new Vector3(Mathf.Cos(a)*.18f,y+(i%2)*.025f,Mathf.Sin(a)*.18f);
                    Piece(PrimitiveType.Sphere,pos,state==FoodState.Raw ? new Vector3(.44f,.27f,.42f):new Vector3(.35f,.055f,.3f),i%2==0 ? color:new Color(.45f,.78f,.17f));
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
                    var p=new Vector3((i-1)*.18f,y+i*.035f,(i%2-.5f)*.16f);
                    Piece(PrimitiveType.Cylinder,p,new Vector3(.39f,.035f,.39f),color);
                    Piece(PrimitiveType.Cylinder,p+Vector3.up*.037f,new Vector3(.29f,.007f,.29f),new Color(1,.48f,.27f));
                    Piece(PrimitiveType.Sphere,p+Vector3.up*.048f,new Vector3(.055f,.014f,.055f),new Color(1,.85f,.47f));
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
                    var p=new Vector3((i%2-.5f)*.26f,y,(i/2-.5f)*.25f);
                    Piece(PrimitiveType.Cube,p,new Vector3(.09f,.1f,.2f),color);
                    Piece(PrimitiveType.Sphere,p+Vector3.up*.08f,new Vector3(.24f,.11f,.24f),color);
                }
            }
            else if (state == FoodState.Raw)
            {
                Piece(PrimitiveType.Sphere, new Vector3(0,y,0), new Vector3(.43f,.32f,.55f), color);
                for(int i=0;i<3;i++) Piece(PrimitiveType.Sphere,new Vector3((i-1)*.1f,y+.155f,(i%2-.5f)*.18f),new Vector3(.04f,.02f,.04f),new Color(.28f,.16f,.06f));
            }
            else
                for (int i=0;i<5;i++) Piece(PrimitiveType.Cube, new Vector3((i-2)*.095f,y+(i%2)*.025f,0), new Vector3(.075f,.12f,.42f), color);
        }
        private void Piece(PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            // Visual-only meshes: avoid CreatePrimitive's implicit collider types,
            // which can be stripped from players when no authored object uses them.
            var part = new GameObject(type.ToString());
            part.AddComponent<MeshFilter>().sharedMesh=type==PrimitiveType.Sphere ? sphereMesh : type==PrimitiveType.Cylinder ? cylinderMesh : cubeMesh;
            part.transform.SetParent(visuals, false); part.transform.localPosition=position+ingredientOffset; part.transform.localScale=scale;
            var renderer=part.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            var properties=new MaterialPropertyBlock(); properties.SetColor("_BaseColor",color); renderer.SetPropertyBlock(properties);
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
