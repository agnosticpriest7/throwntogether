using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace ThrownTogether.Tests
{
    public sealed class CongestionTests
    {
        readonly List<GameObject> objects=new List<GameObject>();
        GameObject Make(Vector3 offset){var g=new GameObject("Traffic test");g.transform.position=new Vector3(100,0,100)+offset;objects.Add(g);return g;}
        [TearDown] public void Cleanup(){foreach(var g in objects)Object.DestroyImmediate(g);objects.Clear();}
        [Test] public void PassingSlowsButNeverStacksOrTrapsAndWallsSeparatePeople()
        {
            var a=Make(Vector3.zero).AddComponent<StaffCongestion>();var b=Make(Vector3.right*.5f).AddComponent<StaffCongestion>();
            Assert.That(a.Factor(Vector3.right),Is.EqualTo(StaffCongestion.MinimumSpeed).Within(.03));
            Make(Vector3.right*.4f).AddComponent<StaffCongestion>();Assert.That(a.Factor(Vector3.right),Is.GreaterThanOrEqualTo(.6f));
            Assert.That(a.Factor(Vector3.left),Is.EqualTo(1),"Moving out of congestion must be unpenalized");
            Assert.That(a.Factor(Vector3.zero),Is.EqualTo(1));
            var wall=Make(new Vector3(.2f,1,0)).AddComponent<BoxCollider>();wall.size=new Vector3(.1f,2,2);Physics.SyncTransforms();
            Assert.That(a.Factor(Vector3.right),Is.EqualTo(1),"People behind counters/walls must not slow each other");
        }
        [Test] public void ClearAislesAndUnregisteredCustomersKeepNormalSpeed()
        {
            var a=Make(Vector3.zero).AddComponent<StaffCongestion>();Make(Vector3.right*.3f).AddComponent<DiningWalker>();
            var b=Make(Vector3.right*2).AddComponent<StaffCongestion>();Assert.That(a.Factor(Vector3.right),Is.EqualTo(1));
            b.transform.position=a.transform.position+Vector3.right*.5f;b.gameObject.SetActive(false);
            Assert.That(a.Factor(Vector3.right),Is.EqualTo(1),"Inactive or departed workers must leave the registry");
        }
        [UnityTest] public IEnumerator PlayersSlowAndCanPassInsteadOfPhysicallyBlocking()
        {
            var a=Make(Vector3.zero).AddComponent<ChefController>();var b=Make(Vector3.right*.65f).AddComponent<ChefController>();
            Physics.SyncTransforms();float x=a.transform.position.x;a.Move(Vector2.right,.05f);
            Assert.That(a.transform.position.x-x,Is.InRange(.1f,.20f));
            for(int i=0;i<60;i++)a.Move(Vector2.right,.01f);
            Assert.That(a.transform.position.x,Is.GreaterThan(b.transform.position.x+.5f));
            Assert.That(Physics.GetIgnoreCollision(a.GetComponent<CharacterController>(),b.GetComponent<CharacterController>()),Is.True);
            yield return null;
        }
        [Test] public void EmployeeWalkingSlowsButCustomerWalkingDoesNot()
        {
            var visual=Resources.Load<GameObject>("ChefVisual");Assert.That(visual,Is.Not.Null);
            var employee=Make(Vector3.zero).AddComponent<DiningWalker>();employee.Initialize(visual,0);
            Make(Vector3.right*.5f).AddComponent<StaffCongestion>();
            var start=employee.transform.position;employee.Go(start+Vector3.right*3);employee.Advance(.1f,2);
            Assert.That(employee.transform.position.x-start.x,Is.InRange(.12f,.14f));
            var customer=Make(Vector3.forward*3).AddComponent<DiningWalker>();customer.Initialize(visual,0,true);
            Make(Vector3.forward*3+Vector3.right*.5f).AddComponent<StaffCongestion>();
            start=customer.transform.position;customer.Go(start+Vector3.right*3);customer.Advance(.1f,2);
            Assert.That(customer.transform.position.x-start.x,Is.EqualTo(.2f).Within(.001f));
        }
    }
}
