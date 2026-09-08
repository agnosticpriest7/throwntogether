using UnityEngine;

namespace ThrownTogether
{
    public sealed class CarrySlot : MonoBehaviour
    {
        public Carryable Item { get; private set; }
        public bool TryTake(Carryable item)
        {
            if (Item != null || item == null) return false;
            if (item.Owner != null) item.Owner.Release();
            Item=item; item.Owner=this;
            item.transform.SetParent(transform,false); item.transform.localPosition=Vector3.zero; item.transform.localRotation=Quaternion.identity;
            return true;
        }
        public Carryable Release()
        {
            var item=Item; Item=null;
            if (item != null) { item.Owner=null; item.transform.SetParent(null,true); }
            return item;
        }
    }
}
