using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class RunExp02GeometryTests
    {
        [Test] public void AuthoredClosedMeshesFaceOutwardAndHaveFiniteUvNormals()
        {
            var ids=AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/_Project/Prototypes/RunExp02/Meshes"});
            Assert.Greater(ids.Length,10);
            foreach(var id in ids)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(id));
                var vertices=mesh.vertices;var triangles=mesh.triangles;double volume=0;
                for(int i=0;i<triangles.Length;i+=3)volume+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]))/6.0;
                Assert.Greater(volume,0,mesh.name+" must expose outward faces rather than hollow inverted surfaces.");
                Assert.AreEqual(vertices.Length,mesh.uv.Length);Assert.AreEqual(vertices.Length,mesh.normals.Length);
                foreach(var normal in mesh.normals)Assert.IsFalse(float.IsNaN(normal.x)||float.IsNaN(normal.y)||float.IsNaN(normal.z));
            }
        }
    }
}
