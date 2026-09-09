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
        public void Configure(ItemPayload payload) { Payload = payload; RefreshVisual(); }
        public void RefreshVisual()
        {
            if (visuals != null) { visuals.gameObject.SetActive(false); Destroy(visuals.gameObject); }
            if (material != null) Destroy(material);
            visuals = new GameObject("Item visual").transform;
            visuals.SetParent(transform, false);
            material = new Material(visualShader);
            if (Payload.isPlate) Piece(PrimitiveType.Cylinder, Vector3.zero, new Vector3(.65f,.045f,.65f), Color.white);
            if (Payload.ingredient == null) return;
            var color = Payload.ingredient.ColorFor(Payload.state);
            var y = Payload.isPlate ? .13f : .08f;
            if(Payload.ingredient.visualKind==IngredientVisualKind.Mushroom)
            {
                if(Payload.state==FoodState.Raw)
                {
                    Piece(PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(.16f,.15f,.16f),new Color(.9f,.84f,.7f));
                    Piece(PrimitiveType.Sphere,new Vector3(0,y+.18f,0),new Vector3(.58f,.24f,.58f),color);
                }
                else for(int i=0;i<4;i++)
                {
                    var p=new Vector3((i%2-.5f)*.26f,y,(i/2-.5f)*.25f);
                    Piece(PrimitiveType.Cube,p,new Vector3(.09f,.1f,.2f),color);
                    Piece(PrimitiveType.Sphere,p+Vector3.up*.08f,new Vector3(.24f,.11f,.24f),color);
                }
            }
            else if (Payload.state == FoodState.Raw)
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
            part.transform.SetParent(visuals, false); part.transform.localPosition=position; part.transform.localScale=scale;
            var renderer=part.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            var properties=new MaterialPropertyBlock(); properties.SetColor("_BaseColor",color); renderer.SetPropertyBlock(properties);
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
