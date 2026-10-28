using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TechGuy.Tests.CharacterIntegration
{
    public sealed class TGCharacterIntegrationTests
    {
        private const string ModelPath = "Assets/_Project/Art/Characters/TG/TG_Rigged.fbx";
        private const string ControllerPath = "Assets/_Project/Art/Character/Animator Controller Character.controller";

        [Test]
        public void ImportedAvatarRetargetsRequiredBodyAndFingerBones()
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
            Assert.That(avatar.isValid && avatar.isHuman, Is.True);
            var names = avatar.humanDescription.human.Select(b => b.humanName).ToArray();
            var required = new[] { "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
                "LeftShoulder", "RightShoulder", "LeftUpperArm", "RightUpperArm",
                "LeftLowerArm", "RightLowerArm", "LeftHand", "RightHand",
                "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg",
                "LeftFoot", "RightFoot", "LeftToes", "RightToes" };
            CollectionAssert.IsSubsetOf(required, names);
            foreach (var side in new[] { "Left", "Right" })
                foreach (var finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
                    foreach (var segment in new[] { "Proximal", "Intermediate", "Distal" })
                        CollectionAssert.Contains(names, side + " " + finger + " " + segment);
        }

        [TestCase("TG_GameMesh")]
        [TestCase("TG_ExposedSkin")]
        [TestCase("TG_Outfit")]
        [TestCase("TG_Hair")]
        public void SkinWeightsRemainFiniteNormalizedAndBoundToExistingBones(string skinName)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var skin = GetSkin(model, skinName);
            Assert.That(skin.sharedMesh, Is.Not.Null);
            Assert.That(skin.bones.All(b => b), Is.True);
            using (var counts = skin.sharedMesh.GetBonesPerVertex())
            using (var weights = skin.sharedMesh.GetAllBoneWeights())
            {
                Assert.That(counts.Length, Is.EqualTo(skin.sharedMesh.vertexCount));
                int offset = 0;
                for (int vertex = 0; vertex < counts.Length; vertex++)
                {
                    Assert.That(counts[vertex], Is.InRange(1, 4), "Vertex " + vertex);
                    float total = 0f;
                    for (int influence = 0; influence < counts[vertex]; influence++)
                    {
                        var weight = weights[offset++];
                        Assert.That(float.IsNaN(weight.weight) || float.IsInfinity(weight.weight), Is.False);
                        Assert.That(weight.weight, Is.GreaterThan(0f));
                        Assert.That(weight.boneIndex, Is.InRange(0, skin.bones.Length - 1));
                        total += weight.weight;
                    }
                    Assert.That(total, Is.EqualTo(1f).Within(.002f), "Vertex " + vertex);
                }
                Assert.That(offset, Is.EqualTo(weights.Length));
            }
        }

        [TestCase("TG_Outfit", "TG_OutfitGray")]
        [TestCase("TG_Hair", "TG_HairGray")]
        public void ImportedClothesAndHairUseTheBodyRigAndOriginalGrayMaterials(string skinName, string materialName)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var body = GetSkin(model, "TG_GameMesh");
            var skin = GetSkin(model, skinName);
            Assert.That(body.bones.Length, Is.EqualTo(53));
            AssertVisibleInsideVisual(skin, model.transform);
            Assert.That(skin.sharedMesh, Is.Not.Null);
            Assert.That(skin.sharedMesh.vertexCount, Is.GreaterThan(0));
            AssertBonesOnBodyRig(skin, body);
            AssertGrayMaterial(skin, materialName);
        }

        [Test]
        public void FullBodyRemainsHiddenWhileVisibleSkinPreservesOriginalGeometryAndRig()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var body = GetSkin(model, "TG_GameMesh");
            var exposed = GetSkin(model, "TG_ExposedSkin");
            CollectionAssert.AreEquivalent(new[] { "TG_GameMesh", "TG_ExposedSkin", "TG_Outfit", "TG_Hair" },
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(skin => skin.name).ToArray());
            Assert.That(model.GetComponentsInChildren<MeshRenderer>(true), Is.Empty,
                "The game export must exclude static authoring archive meshes.");
            AssertExposedSkin(exposed, body, model.transform);
            Assert.That(exposed.sharedMesh.vertexCount, Is.InRange(1, body.sharedMesh.vertexCount - 1));

            const float tolerance = .00001f;
            var originalPositions = new Dictionary<Vector3Int, List<Vector3>>();
            foreach (var vertex in body.sharedMesh.vertices)
            {
                var point = body.transform.TransformPoint(vertex);
                var cell = Vector3Int.FloorToInt(point / tolerance);
                if (!originalPositions.TryGetValue(cell, out var points))
                    originalPositions.Add(cell, points = new List<Vector3>());
                points.Add(point);
            }
            int exposedVertex = 0;
            foreach (var vertex in exposed.sharedMesh.vertices)
            {
                var point = exposed.transform.TransformPoint(vertex);
                var cell = Vector3Int.FloorToInt(point / tolerance);
                bool matches = false;
                for (int x = -1; x <= 1 && !matches; x++)
                    for (int y = -1; y <= 1 && !matches; y++)
                        for (int z = -1; z <= 1 && !matches; z++)
                            if (originalPositions.TryGetValue(cell + new Vector3Int(x, y, z), out var points))
                                matches = points.Any(original => (original - point).sqrMagnitude <= tolerance * tolerance);
                Assert.That(matches, Is.True, "Exposed vertex " + exposedVertex++ + " must preserve its original body position.");
            }
            using (var weights = exposed.sharedMesh.GetAllBoneWeights())
            {
                var influencedBones = new HashSet<string>();
                foreach (var weight in weights)
                    if (weight.weight > .1f) influencedBones.Add(exposed.bones[weight.boneIndex].name);
                CollectionAssert.IsSubsetOf(new[] { "Head", "LeftHand", "RightHand" }, influencedBones);
            }
        }

        [Test]
        public void ImportedHairFollowsOnlyTheExistingHeadBone()
        {
            var skin = GetSkin(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), "TG_Hair");
            int headIndex = Array.FindIndex(skin.bones, bone => bone && bone.name == "Head");
            Assert.That(headIndex, Is.GreaterThanOrEqualTo(0));
            using (var counts = skin.sharedMesh.GetBonesPerVertex())
            using (var weights = skin.sharedMesh.GetAllBoneWeights())
            {
                Assert.That(counts.Length, Is.EqualTo(skin.sharedMesh.vertexCount));
                Assert.That(weights.Length, Is.EqualTo(skin.sharedMesh.vertexCount));
                for (int vertex = 0; vertex < counts.Length; vertex++)
                {
                    Assert.That(counts[vertex], Is.EqualTo(1), "Hair vertex " + vertex);
                    Assert.That(weights[vertex].boneIndex, Is.EqualTo(headIndex), "Hair vertex " + vertex);
                    Assert.That(weights[vertex].weight, Is.EqualTo(1f).Within(.0001f), "Hair vertex " + vertex);
                }
            }
        }

        [Test]
        public void IllustratedMaterialsKeepAllSevenExportedTextureReferences()
        {
            var skin = GetSkin(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), "TG_GameMesh");
            Assert.That(skin.sharedMaterials.Length, Is.EqualTo(8));
            foreach (var material in skin.sharedMaterials)
            {
                Assert.That(material, Is.Not.Null);
                Assert.That(material.shader.name, Is.EqualTo("Tech Guy/Illustrated Toon"));
                Assert.That(AssetDatabase.GetAssetPath(material), Does.StartWith("Assets/_Project/Art/Characters/TG/Materials/"));
                if (material.name.StartsWith("TG_ShortsRepair", StringComparison.Ordinal)) continue;
                Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")),
                    Does.StartWith("Assets/_Project/Art/Characters/TG/Textures/"));
            }
        }

        [TestCase("TG_LeftLeg")]
        [TestCase("TG_RightLeg")]
        public void NeutralFootSurfacePointsForwardInsteadOfKeepingTheOriginalSplay(string legMaterial)
        {
            var skin = GetSkin(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), "TG_GameMesh");
            int submesh = Array.FindIndex(skin.sharedMaterials, m => m.name.StartsWith(legMaterial, StringComparison.Ordinal));
            Assert.That(submesh, Is.GreaterThanOrEqualTo(0));
            var vertices = skin.sharedMesh.vertices;
            var points = skin.sharedMesh.GetTriangles(submesh).Distinct()
                .Select(i => skin.transform.TransformPoint(vertices[i])).Where(v => v.y < .14f).ToArray();
            Assert.That(points.Length, Is.GreaterThan(100), "The authored foot surface must be present.");
            float meanX = points.Average(v => v.x), meanZ = points.Average(v => v.z);
            double xx = 0, zz = 0, xz = 0;
            foreach (var v in points) { double x = v.x - meanX, z = v.z - meanZ; xx += x*x; zz += z*z; xz += x*z; }
            double axis = .5 * Math.Atan2(2*xz, xx-zz);
            float yaw = Mathf.DeltaAngle(0f, 90f - (float)(axis*180/Math.PI));
            if (yaw > 90f) yaw -= 180f;
            if (yaw < -90f) yaw += 180f;
            Assert.That(Mathf.Abs(yaw), Is.LessThan(3f), "Neutral foot surface must face the model's forward axis.");
        }

        [TestCase("NexusLobby", true)]
        [TestCase("FirstSector", true)]
        [TestCase("PrologueTutorial", true)]
        [TestCase("Playground", true)]
        [TestCase("CombatStudy", true)]
        [TestCase("ProceduralStageAuthoring", false)]
        public void SavedPlayerKeepsControllerAndWeaponHandOnTheImportedRig(string sceneName, bool hasController)
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/" + sceneName + ".unity");
            try
            {
                var actor = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerActor>(true)).Single();
                var animator = actor.GetComponent<Animator>();
                var visual = actor.transform.Find("TG_Rigged_Visual");
                Assert.That(visual, Is.Not.Null);
                Assert.That(actor.transform.Find("TechGuy_HD_Visual"), Is.Null);
                Assert.That(AssetDatabase.GetAssetPath(animator.avatar), Is.EqualTo(ModelPath));
                Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
                Assert.That(animator.applyRootMotion, Is.False);
                if (hasController)
                {
                    Assert.That(actor.GetComponent<CharControlScript>(), Is.Not.Null);
                    Assert.That(actor.GetComponent<AbilityHolder>(), Is.Not.Null);
                    Assert.That(actor.GetComponent<UnityEngine.AI.NavMeshAgent>(), Is.Not.Null);
                }
                Assert.That(actor.HandTransform, Is.Not.Null);
                Assert.That(actor.HandTransform.IsChildOf(visual), Is.True);
                Assert.That(actor.HandTransform.name, Is.EqualTo("RightHand"));
                Assert.That(visual.GetComponentsInChildren<Animator>(true), Is.Empty);
                var body = GetSkin(visual.gameObject, "TG_GameMesh");
                AssertExposedSkin(GetSkin(visual.gameObject, "TG_ExposedSkin"), body, visual);
                foreach (var attachment in new[] { "TG_Outfit", "TG_Hair" })
                {
                    var skin = GetSkin(visual.gameObject, attachment);
                    AssertVisibleInsideVisual(skin, visual);
                    AssertBonesOnBodyRig(skin, body);
                    AssertGrayMaterial(skin, attachment == "TG_Outfit" ? "TG_OutfitGray" : "TG_HairGray");
                }
                Assert.That(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
                    Is.EqualTo(hasController ? ControllerPath : string.Empty));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static SkinnedMeshRenderer GetSkin(GameObject model, string skinName)
        {
            Assert.That(model, Is.Not.Null);
            return model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(skin => skin.name == skinName);
        }

        private static void AssertVisibleInsideVisual(SkinnedMeshRenderer skin, Transform visualRoot)
        {
            Assert.That(skin.enabled, Is.True, skin.name + " must have its renderer enabled.");
            Assert.That(visualRoot, Is.Not.Null);
            for (var current = skin.transform; current != null; current = current.parent)
            {
                Assert.That(current.gameObject.activeSelf, Is.True,
                    skin.name + " must have an active local visual chain; " + current.name + " is inactive.");
                if (current == visualRoot)
                    return;
            }
            Assert.Fail(skin.name + " must belong to the imported model or saved player visual root.");
        }

        private static void AssertExposedSkin(SkinnedMeshRenderer exposed, SkinnedMeshRenderer body, Transform visualRoot)
        {
            Assert.That(body.enabled, Is.False,
                "The importer must disable the preserved full anatomy underneath the clothes.");
            AssertVisibleInsideVisual(exposed, visualRoot);
            Assert.That(exposed.sharedMesh, Is.Not.Null);
            Assert.That(exposed.sharedMesh.vertexCount, Is.GreaterThan(0));
            AssertBonesOnBodyRig(exposed, body);
            var usedMaterialNames = new[] { "TG_LeftArm_Illustrated", "TG_Torso_Illustrated",
                "TG_Head_Illustrated", "TG_RightArm_Illustrated" };
            var usedOriginalMaterials = usedMaterialNames
                .Select(name => body.sharedMaterials.Single(material => material && material.name == name)).ToArray();
            CollectionAssert.AreEqual(usedOriginalMaterials, exposed.sharedMaterials);
        }

        private static void AssertBonesOnBodyRig(SkinnedMeshRenderer skin, SkinnedMeshRenderer body)
        {
            Assert.That(skin.bones, Is.Not.Empty);
            Assert.That(skin.bones.All(bone => bone), Is.True);
            CollectionAssert.IsSubsetOf(skin.bones, body.bones, skin.name + " must reference the existing body rig.");
            Assert.That(skin.rootBone, Is.Not.Null);
            CollectionAssert.Contains(body.bones, skin.rootBone);
        }

        private static void AssertGrayMaterial(SkinnedMeshRenderer skin, string materialName)
        {
            Assert.That(skin.sharedMaterials.Length, Is.EqualTo(1));
            var material = skin.sharedMaterials[0];
            Assert.That(material, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(material),
                Is.EqualTo("Assets/_Project/Art/Characters/TG/Materials/" + materialName + ".mat"));
            Assert.That(material.shader.name, Is.EqualTo("Tech Guy/Illustrated Toon"));
            var color = material.GetColor("_BaseColor");
            Assert.That(color.r, Is.EqualTo(.8f).Within(.0001f));
            Assert.That(color.g, Is.EqualTo(.8f).Within(.0001f));
            Assert.That(color.b, Is.EqualTo(.8f).Within(.0001f));
            Assert.That(color.a, Is.EqualTo(1f).Within(.0001f));
        }
    }
}
