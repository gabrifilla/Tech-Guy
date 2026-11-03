using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace TechGuy.Tests
{
    public sealed class RunExp05ArtTests
    {
        [Test] public void DccCandidatesKeepGameplayReferencesAndPivotsWithFiniteMappedGeometry()
        {
            foreach(string name in new[]{"Fragmento","Emissor"})
            {
                var before=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp04/"+name+".prefab");
                var after=AssetDatabase.LoadAssetAtPath<GameObject>(RunExp05EnemyArtAuthoring.Prefabs+"/"+name+".prefab");Assert.IsNotNull(after);
                Assert.AreSame(before.GetComponent<EnemyVariant>().Archetype,after.GetComponent<EnemyVariant>().Archetype);
                Assert.AreEqual(before.GetComponent<Actor>().health,after.GetComponent<Actor>().health);
                var oldAI=before.GetComponent<EnemyAI>();var ai=after.GetComponent<EnemyAI>();Assert.AreEqual(oldAI.attackDamage,ai.attackDamage);Assert.AreEqual(oldAI.timeBetweenAttacks,ai.timeBetweenAttacks);Assert.AreEqual(oldAI.AttackWindup,ai.AttackWindup);
                var oldCapsule=before.GetComponent<CapsuleCollider>();var capsule=after.GetComponent<CapsuleCollider>();
                Assert.AreEqual(oldCapsule.center,capsule.center);Assert.AreEqual(oldCapsule.radius,capsule.radius);Assert.AreEqual(oldCapsule.height,capsule.height);Assert.AreEqual(oldCapsule.direction,capsule.direction);Assert.AreEqual(oldCapsule.isTrigger,capsule.isTrigger);
                Assert.AreEqual(before.GetComponent<NavMeshAgent>().speed,after.GetComponent<NavMeshAgent>().speed);Assert.AreEqual(before.GetComponent<NavMeshAgent>().radius,after.GetComponent<NavMeshAgent>().radius);
                var oldParts=before.GetComponentsInChildren<MeshFilter>(true);var parts=after.GetComponentsInChildren<MeshFilter>(true);Assert.AreEqual(oldParts.Length,parts.Length);
                for(int i=0;i<parts.Length;i++)
                {
                    Assert.AreEqual(oldParts[i].name,parts[i].name);Assert.AreEqual(oldParts[i].transform.localPosition,parts[i].transform.localPosition);Assert.AreEqual(oldParts[i].transform.localRotation,parts[i].transform.localRotation);Assert.AreEqual(oldParts[i].transform.localScale,parts[i].transform.localScale);
                    var mesh=parts[i].sharedMesh;Assert.AreNotSame(oldParts[i].sharedMesh,mesh);Assert.AreEqual(1,mesh.subMeshCount);Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);Assert.AreEqual(mesh.vertexCount,mesh.tangents.Length);
                    Assert.IsTrue(mesh.uv.All(v=>v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1));
                    Assert.IsTrue(mesh.vertices.All(v=>Finite(v.x)&&Finite(v.y)&&Finite(v.z)));
                    var vertices=mesh.vertices;var indices=mesh.triangles;double volume=0;for(int n=0;n<indices.Length;n+=3)volume+=Vector3.Dot(vertices[indices[n]],Vector3.Cross(vertices[indices[n+1]],vertices[indices[n+2]]))/6.0;
                    Assert.Greater(volume,0,mesh.name);
                }
                Assert.AreEqual(name=="Fragmento"?18:7,after.GetComponentsInChildren<MeshRenderer>(true).Length);
                Assert.Less(parts.Sum(f=>f.sharedMesh.triangles.Length/3),8000);
                Assert.IsTrue(new SerializedObject(after.GetComponent<FirstSectorEnemyPose>()).FindProperty("_laminatedPresentation").boolValue);
                Assert.IsFalse(new SerializedObject(before.GetComponent<FirstSectorEnemyPose>()).FindProperty("_laminatedPresentation").boolValue);
                Assert.IsFalse(after.GetComponentsInChildren<Component>(true).Any(c=>c==null));
            }
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
