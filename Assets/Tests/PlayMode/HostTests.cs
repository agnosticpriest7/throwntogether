using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace ThrownTogether.Tests
{
    public sealed class HostTests
    {
        sealed class Memory:ISettingsStorage {public string json="";public string Read()=>json;public void Write(string value)=>json=value;}
        Scene original,scene;GameObject[] suspended;Memory memory;RestaurantDay day;

        [UnitySetUp] public IEnumerator Setup()
        {
            original=SceneManager.GetActiveScene();suspended=Object.FindObjectsByType<RestaurantHud>().Any(h=>h.gameObject.scene==original)?original.GetRootGameObjects().Where(g=>g.activeSelf).ToArray():new GameObject[0];foreach(var g in suspended)g.SetActive(false);
            memory=new Memory();RestaurantAccounts.UseStorage(memory);SessionOptions.Kitchen=0;SessionOptions.ShiftOrders=0;SessionOptions.ManageBeforeService=true;Time.timeScale=1;
            int n=RestaurantAccounts.Current.StartDay();RestaurantAccounts.Current.Settle(n,2000,0);RestaurantAccounts.Current.SetMenu(DailyMenu.Catalog.Where(r=>r.requiredPurchases.Length==0).Select(r=>r.id).ToArray());
            yield return Load();
        }
        IEnumerator Load()
        {
            if(scene.IsValid()&&scene.isLoaded){SceneManager.SetActiveScene(original);yield return SceneManager.UnloadSceneAsync(scene);}
            SessionOptions.ManageBeforeService=true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestaurantShift.unity",new LoadSceneParameters(LoadSceneMode.Additive));
            scene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);SceneManager.SetActiveScene(scene);yield return null;yield return null;
            day=Object.FindObjectsByType<RestaurantDay>().Single(d=>d.gameObject.scene==scene);
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);SceneManager.SetActiveScene(original);foreach(var g in suspended)if(g!=null)g.SetActive(true);
            Time.timeScale=1;SessionOptions.ManageBeforeService=false;SessionOptions.ShiftOrders=6;SessionOptions.Kitchen=0;RestaurantAccounts.ResetCache();
        }
        void HireHost()
        {
            Assert.That(day.Settings.hostRole,Is.Not.Null);Assert.That(day.Settings.hostRole.id,Is.EqualTo("host"));
            Assert.That(RestaurantAccounts.Current.Buy("host",day.Settings.hostRole.hireCost),Is.True);
        }
        void FinishArrival(){for(int i=0;i<1200&&day.StaffEntering;i++)day.Advance(.1f);Assert.That(day.StaffEntering,Is.False);}

        [UnityTest] public IEnumerator HostHireTrainingAndHomePersist()
        {
            HireHost();Assert.That(RestaurantAccounts.Current.Train("host"),Is.True);Assert.That(RestaurantAccounts.Current.TrainingLevel("host"),Is.EqualTo(1));
            var furniture=day.GetComponent<KitchenFurniture>();Assert.That(furniture.Begin(),Is.True);furniture.ToggleHomes();
            var chosen=new Vector3(8.6f,0,-4.8f);Assert.That(furniture.Homes.TrySet("host",chosen),Is.True,furniture.Homes.Message);Assert.That(furniture.Save(),Is.True,furniture.Message);
            RestaurantAccounts.UseStorage(memory);yield return Load();
            Assert.That(RestaurantAccounts.Current.Owns("host"),Is.True);Assert.That(RestaurantAccounts.Current.TrainingLevel("host"),Is.EqualTo(1));Assert.That(day.GetComponent<KitchenFurniture>().Homes.Home("host"),Is.EqualTo(chosen));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator HostPhysicallyEscortsQueueHeadAndSlowsOnlyOutsidePatience()
        {
            HireHost();Assert.That(day.StartService(),Is.True,day.ExpoProblem);FinishArrival();Assert.That(day.Host,Is.Not.Null);
            foreach(var table in day.Tables)table.ReserveSeat();
            day.Advance(day.Settings.firstArrival+14);Assert.That(day.WaitingOutside,Is.GreaterThan(0));float before=day.OldestWait;
            day.Advance(10);Assert.That(day.OldestWait-before,Is.EqualTo(7.5f).Within(.12f));Assert.That(day.OutsidePatienceRate,Is.EqualTo(.75f));
            var table0=day.Tables[0];table0.CancelArrival();
            for(int i=0;i<1200&&!table0.WaitingForMeal;i++)day.Advance(.1f);
            Assert.That(table0.WaitingForMeal,Is.True,"Host should escort the first waiting customer all the way to the chair");Assert.That(day.WaitingOutside,Is.GreaterThanOrEqualTo(0));
            float seated=table0.PatienceRemaining;day.Advance(1);Assert.That(seated-table0.PatienceRemaining,Is.EqualTo(1/day.Settings.seatedPatience).Within(.003f),"Host must not change seated patience");
            LogAssert.NoUnexpectedReceived();yield return null;
        }

        [UnityTest] public IEnumerator NoHostKeepsAutomaticSeatingAndNormalOutsideDecay()
        {
            Assert.That(day.StartService(),Is.True);foreach(var table in day.Tables)table.ReserveSeat();
            day.Advance(day.Settings.firstArrival+14);Assert.That(day.WaitingOutside,Is.GreaterThan(0));float before=day.OldestWait;day.Advance(10);
            Assert.That(day.Host,Is.Null);Assert.That(day.OutsidePatienceRate,Is.EqualTo(1));Assert.That(day.OldestWait-before,Is.EqualTo(10).Within(.12f));
            day.Tables[0].CancelArrival();for(int i=0;i<500&&!day.Tables[0].WaitingForMeal;i++)day.Advance(.1f);
            Assert.That(day.Tables[0].WaitingForMeal,Is.True,"No-host baseline still seats automatically");LogAssert.NoUnexpectedReceived();yield return null;
        }

        [UnityTest] public IEnumerator HostBreakImmediatelyRestoresNormalPatienceAndAutomaticSeating()
        {
            HireHost();Assert.That(day.StartService(),Is.True);FinishArrival();foreach(var table in day.Tables)table.ReserveSeat();
            day.Advance(day.Settings.firstArrival+14);Assert.That(day.WaitingOutside,Is.GreaterThan(0));Assert.That(day.OutsidePatienceRate,Is.EqualTo(.75f));
            var member=day.Staff.Single(s=>s.Role=="host");float before=day.OldestWait;Assert.That(member.ToggleBreak(),Is.True);Assert.That(day.HostAvailable,Is.False);day.Advance(4);
            Assert.That(day.OldestWait-before,Is.EqualTo(4).Within(.12f));day.Tables[0].CancelArrival();
            for(int i=0;i<600&&!day.Tables[0].WaitingForMeal;i++)day.Advance(.1f);
            Assert.That(day.Tables[0].WaitingForMeal,Is.True,"Customers should seat automatically while host is resting");
            Assert.That(member.ToggleBreak(),Is.True);Assert.That(day.HostAvailable,Is.True);LogAssert.NoUnexpectedReceived();yield return null;
        }

        [UnityTest] public IEnumerator ClosingReleasesHostReservationAndStaffStillLeaveLast()
        {
            HireHost();Assert.That(day.StartService(),Is.True);FinishArrival();
            day.Advance(day.Settings.firstArrival+2);for(int i=0;i<300&&!day.Tables.Any(t=>t.Arriving);i++)day.Advance(.05f);
            float duration=day.Settings.durationSeconds;day.Settings.durationSeconds=day.Elapsed;
            try
            {
                day.Advance(.1f);Assert.That(day.Tables.All(t=>!t.Arriving),Is.True,"An unadmitted host reservation must be released at closing");
                for(int i=0;i<4000&&!day.Closed;i++)day.Advance(.1f);
                Assert.That(day.Closed,Is.True);Assert.That(day.Paid,Is.True);Assert.That(day.Staff.All(s=>s.Gone),Is.True);
            }
            finally{day.Settings.durationSeconds=duration;}
            LogAssert.NoUnexpectedReceived();yield return null;
        }

        [UnityTest] public IEnumerator HostAlreadyOnSidewalkCanLeaveWithoutIndoorRoute()
        {
            HireHost();Assert.That(day.StartService(),Is.True);FinishArrival();var member=day.Staff.Single(s=>s.Role=="host");
            member.transform.position=new Vector3(day.Settings.entrance.x-1.2f,0,day.Settings.sidewalkStart.z);
            member.BeginDeparture();for(int i=0;i<500&&!member.Gone;i++)member.AdvanceDeparture(.1f);
            Assert.That(member.Gone,Is.True,member.Status);LogAssert.NoUnexpectedReceived();yield return null;
        }
        [TestCase(false)] [TestCase(true)]
        public void HostBreakDuringOutsideApproachReturnsOrResumesFromActualPosition(bool resumeOutside)
        {
            HireHost();Assert.IsTrue(day.StartService());FinishArrival();var member=day.Staff.Single(s=>s.Role=="host");
            for(int i=0;i<1200;i++){day.Advance(.05f);if(day.Host.Status=="Greeting next customer"&&member.transform.position.z< -5.6f)break;}
            Assert.AreEqual("Greeting next customer",day.Host.Status);Assert.Less(member.transform.position.z,-5.6f);
            var before=member.transform.position;Assert.IsTrue(member.ToggleBreak());Assert.AreEqual(before,member.transform.position);
            Assert.IsFalse(day.HostAvailable);Assert.IsFalse(day.Tables.Any(t=>t.Arriving),"Unstarted reservation must be released");
            if(resumeOutside)
            {
                Assert.IsTrue(member.ToggleBreak());Assert.AreEqual(before,member.transform.position);Assert.IsTrue(day.HostAvailable);
                for(int i=0;i<1200&&!day.Tables.Any(t=>t.WaitingForMeal);i++)day.Advance(.05f);
                Assert.IsTrue(day.Tables.Any(t=>t.WaitingForMeal),day.Host.Status);Assert.IsFalse(day.HostRouteBlocked);
            }
            else
            {
                for(int i=0;i<1200&&!member.OnBreak;i++)
                {
                    var previous=member.transform.position;day.Advance(.05f);
                    Assert.LessOrEqual(Vector3.Distance(previous,member.transform.position),day.Settings.walkingSpeed*.05f+.001f,"No teleport on break-home route");
                }
                Assert.IsTrue(member.OnBreak,member.DisplayStatus);Assert.Less(Vector3.Distance(member.Home,member.transform.position),.05f);
                for(int i=0;i<600&&!day.Tables.Any(t=>t.WaitingForMeal);i++)day.Advance(.05f);
                Assert.IsTrue(day.Tables.Any(t=>t.WaitingForMeal),"Automatic seating still proceeds");
            }
        }
    }
}
