using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class RunExp03ArtTests
    {
        [Test] public void RefinedGeometryIsFiniteOutwardAndCandidatesKeepTheirNativeContracts()
        {
            foreach(string name in new[]{"Fragmento","Emissor"})
            {
                var old=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prototypes/RunExp02/"+name+".prefab");
                var current=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp03/"+name+".prefab");
                Assert.IsNotNull(current.GetComponent<FirstSectorEnemyPose>());Assert.IsNull(current.GetComponent<RunExp02EnemyPose>());
                Assert.AreEqual(old.GetComponent<EnemyVariant>().Archetype,current.GetComponent<EnemyVariant>().Archetype);
                Assert.AreEqual(old.GetComponent<Actor>().health,current.GetComponent<Actor>().health);
                Assert.AreEqual(old.GetComponent<EnemyAI>().attackDamage,current.GetComponent<EnemyAI>().attackDamage);
                Assert.AreEqual(old.GetComponent<EnemyAI>().timeBetweenAttacks,current.GetComponent<EnemyAI>().timeBetweenAttacks);
                Assert.AreEqual(old.GetComponent<CapsuleCollider>().radius,current.GetComponent<CapsuleCollider>().radius);
                Assert.IsTrue(current.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial&&r.sharedMaterial.shader));
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/_Project/Art/RunExp03/Enemies"}))
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));var v=mesh.vertices;var t=mesh.triangles;double volume=0;
                for(int i=0;i<t.Length;i+=3)volume+=Vector3.Dot(v[t[i]],Vector3.Cross(v[t[i+1]],v[t[i+2]]))/6.0;
                Assert.Greater(volume,0,mesh.name);Assert.AreEqual(v.Length,mesh.uv.Length);Assert.IsTrue(v.All(p=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)));
            }
        }
    }
}
