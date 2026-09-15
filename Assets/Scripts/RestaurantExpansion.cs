using System.Linq;
using UnityEngine;

namespace ThrownTogether
{
    // Optional authored room strips. Existing stations, bays, players and entry remain fixed.
    public sealed class RestaurantExpansion : MonoBehaviour
    {
        public const string KitchenId="kitchen-extension", DiningId="dining-extension";
        public const float KitchenWidth=3.8f, DiningWidth=4.2f;
        public GameObject kitchenArea,diningArea;
        public CustomerOrder[] additionalSeats=new CustomerOrder[0];
        public Transform[] westEdge=new Transform[0],eastEdge=new Transform[0],wideBoundaries=new Transform[0];
        bool kitchenApplied,diningApplied;
        Vector3 cameraPosition;
        float cameraSize;
        bool capturedCamera;
        public CustomerOrder[] ActiveSeats=>diningApplied?additionalSeats:new CustomerOrder[0];
        public bool KitchenOpen=>kitchenApplied;
        public bool DiningOpen=>diningApplied;
        public void Apply()
        {
            var camera=GetComponent<RestaurantHud>().gameplayCamera;
            if(!capturedCamera){cameraPosition=camera.transform.position;cameraSize=GetComponent<DayPresentation>()?.ServiceCameraSize??camera.orthographicSize;capturedCamera=true;}
            var account=RestaurantAccounts.Current;
            bool changed=false;
            if(!kitchenApplied && account.Owns(KitchenId))
            {
                kitchenArea.SetActive(true);foreach(var edge in westEdge)edge.position+=Vector3.left*KitchenWidth;
                Widen(-KitchenWidth);kitchenApplied=true;changed=true;
            }
            if(!diningApplied && account.Owns(DiningId))
            {
                diningArea.SetActive(true);foreach(var edge in eastEdge)edge.position+=Vector3.right*DiningWidth;
                Widen(DiningWidth);diningApplied=true;changed=true;
            }
            if(!changed)return;
            Physics.SyncTransforms();
            float left=kitchenApplied?KitchenWidth:0,right=diningApplied?DiningWidth:0;
            camera.transform.position=cameraPosition+Vector3.right*(right-left)*.5f;
            // Keep the service camera angle; reserve a modest horizontal margin for the room edges.
            float size=Mathf.Max(cameraSize,(21+left+right+1.4f)/(2*Mathf.Max(.5f,camera.aspect)*.94f));
            var presentation=GetComponent<DayPresentation>();
            if(presentation!=null)presentation.SetServiceCameraSize(size);else camera.orthographicSize=size;
        }
        void Widen(float signedWidth)
        {
            foreach(var boundary in wideBoundaries)
            {
                boundary.position+=Vector3.right*signedWidth*.5f;
                var size=boundary.localScale;size.x+=Mathf.Abs(signedWidth);boundary.localScale=size;
            }
        }
    }
}
