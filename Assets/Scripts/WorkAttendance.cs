using UnityEngine;
namespace ThrownTogether
{
    // An explicit Use claims one job. Movement or another job releases that claim.
    public sealed class WorkAttendance
    {
        private ChefController worker;
        private int revision;
        private Vector3 position;
        public bool Running
        {
            get
            {
                if(worker==null || !worker.gameObject.activeInHierarchy || worker.WorkRevision!=revision) return false;
                var delta=worker.transform.position-position;delta.y=0;
                if(delta.sqrMagnitude>.0025f) {worker.CancelWork();return false;}
                var input=worker.GetComponent<ChefInput>();
                return input==null || input.InputFocused;
            }
        }
        public void Release() {worker=null;}
        public bool Begin(ChefController chef)
        {
            if(Running) return false;
            worker=chef;chef.CancelWork();revision=chef.WorkRevision;position=chef.transform.position;return true;
        }
    }
}
