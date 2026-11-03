using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class RunExp04ArtTests
    {
        [Test] public void RevisionChangesGeometryWithoutChangingGameplayFootprintOrNativeExecutors()
        {
            foreach(string name in new[]{"Fragmento","Emissor"})
            {
                var old=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp03/"+name+".prefab");
                var current=AssetDatabase.LoadAssetAtPath<GameObject>(RunExp04EnemyArtAuthoring.Prefabs+"/"+name+".prefab");Assert.IsNotNull(current);
                Assert.AreSame(old.GetComponent<EnemyVariant>().Archetype,current.GetComponent<EnemyVariant>().Archetype);
                Assert.AreEqual(old.GetComponent<Actor>().health,current.GetComponent<Actor>().health);
                Assert.AreEqual(old.GetComponent<EnemyAI>().attackDamage,current.GetComponent<EnemyAI>().attackDamage);
                Assert.AreEqual(old.GetComponent<EnemyAI>().timeBetweenAttacks,current.GetComponent<EnemyAI>().timeBetweenAttacks);
                Assert.AreEqual(old.GetComponent<EnemyAI>().AttackWindup,current.GetComponent<EnemyAI>().AttackWindup);
                var beforeCapsule=old.GetComponent<CapsuleCollider>();var afterCapsule=current.GetComponent<CapsuleCollider>();
                Assert.AreEqual(beforeCapsule.center,afterCapsule.center);Assert.AreEqual(beforeCapsule.radius,afterCapsule.radius);Assert.AreEqual(beforeCapsule.height,afterCapsule.height);Assert.AreEqual(beforeCapsule.direction,afterCapsule.direction);Assert.AreEqual(beforeCapsule.isTrigger,afterCapsule.isTrigger);
                Assert.AreEqual(old.GetComponentsInChildren<MeshRenderer>(true).Length,current.GetComponentsInChildren<MeshRenderer>(true).Length);
                Assert.IsTrue(new SerializedObject(current.GetComponent<FirstSectorEnemyPose>()).FindProperty("_weightedLocomotion").boolValue);
                var before=old.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();var after=current.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();Assert.AreEqual(before.Length,after.Length);Assert.IsTrue(before.Where((m,i)=>m!=after[i]).Any());
                Assert.IsFalse(current.GetComponentsInChildren<Component>(true).Any(c=>c==null));
            }
            foreach(string id in AssetDatabase.FindAssets("t:Mesh",new[]{RunExp04EnemyArtAuthoring.Root}))
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(id));Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);
                Assert.IsTrue(mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z)));
                double volume=0;var vertices=mesh.vertices;var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3)volume+=Vector3.Dot(vertices[indices[i]],Vector3.Cross(vertices[indices[i+1]],vertices[indices[i+2]]))/6.0;Assert.Greater(volume,0,mesh.name);
            }
        }
    }
}
