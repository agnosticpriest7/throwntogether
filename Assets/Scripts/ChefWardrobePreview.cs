using UnityEngine;

namespace ThrownTogether
{
    public sealed class ChefWardrobePreview : MonoBehaviour
    {
        private GameObject stage;
        private Camera previewCamera;
        private RenderTexture texture;
        private ChefAppearance model;
        public Texture Image => texture;
        public void Show(ChefAppearanceData data)
        {
            if(stage==null)
            {
                var asset=Resources.Load<GameObject>("ChefVisual"); if(asset==null) return;
                stage=new GameObject("Wardrobe preview"); stage.transform.position=new Vector3(1000,-1000,1000);
                var visual=Instantiate(asset,stage.transform); model=visual.GetComponent<ChefAppearance>(); model.usePlayerSelection=false;
                foreach(var t in stage.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
                var cam=new GameObject("Wardrobe camera"); cam.transform.SetParent(stage.transform,false);
                cam.transform.localPosition=new Vector3(2,1.9f,4); cam.transform.LookAt(stage.transform.position+Vector3.up*.95f);
                previewCamera=cam.AddComponent<Camera>(); previewCamera.enabled=false; previewCamera.cullingMask=1<<31;
                previewCamera.orthographic=true; previewCamera.orthographicSize=1.22f; previewCamera.nearClipPlane=.1f; previewCamera.farClipPlane=10;
                previewCamera.clearFlags=CameraClearFlags.SolidColor; previewCamera.backgroundColor=new Color(.12f,.17f,.19f);
                texture=new RenderTexture(512,640,24); texture.Create(); previewCamera.targetTexture=texture;
            }
            model.Apply(data); previewCamera.Render();
        }
        private void OnDestroy()
        {
            if(stage!=null) Destroy(stage);
            if(texture!=null) {texture.Release(); Destroy(texture);}
        }
    }
}
