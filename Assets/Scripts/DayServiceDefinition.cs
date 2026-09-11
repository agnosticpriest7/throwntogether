using UnityEngine;
namespace ThrownTogether
{
    [CreateAssetMenu(menuName="Thrown Together/Service day")]
    public sealed class DayServiceDefinition : ContentDefinition
    {
        public float durationSeconds=300;
        public int arrivals=10;
        public int wasteCost=1;
        public Vector3 serverIdle=new Vector3(10.2f,0,-5.1f), busserIdle=new Vector3(11.5f,0,-5.1f);
        public int additionalCustomersPerDay=2;
        public int CustomersForDay(int day)=>Mathf.Clamp(arrivals+Mathf.Clamp(day-1,0,10000)*additionalCustomersPerDay,1,120);
        public float IntervalForDay(int day)=>arrivalInterval*Mathf.Max(1,arrivals-1)/Mathf.Max(1,CustomersForDay(day)-1);
        public float firstArrival=4, arrivalInterval=24, outsidePatience=45, eatingSeconds=8;
        public float seatedPatience=90;
        public float bonusWindow=60;
        public int maximumBonus=5;
        public float walkingSpeed=2.4f;
        public Vector3 entrance=new Vector3(6.3f,0,-6.1f);
        public float diningAisleX=5.25f;
        public RestaurantUpgradeDefinition[] purchases;
        public GameObject walkingVisual;
        public EmployeeRoleDefinition serverRole;
        public EmployeeRoleDefinition dishwasherRole;
        public EmployeeRoleDefinition busserRole;
        public Vector3 sidewalkStart=new Vector3(-14,0,-7.1f), sidewalkExit=new Vector3(17,0,-7.1f);
    }
}
