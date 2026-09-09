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
            if (Payload.state == FoodState.Raw)
                Piece(PrimitiveType.Sphere, new Vector3(0,y,0), new Vector3(.43f,.32f,.55f), color);
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
