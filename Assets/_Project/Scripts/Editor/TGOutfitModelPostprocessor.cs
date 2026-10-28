using UnityEditor;
using UnityEngine;

namespace TechGuy.EditorTools
{
    /// <summary>Retains the full anatomical reference without rendering skin under the fitted outfit.</summary>
    public sealed class TGOutfitModelPostprocessor : AssetPostprocessor
    {
        private const string ModelPath = "Assets/_Project/Art/Characters/TG/TG_Rigged.fbx";

        public override uint GetVersion() => 1;

        private void OnPostprocessModel(GameObject model)
        {
            if (assetPath != ModelPath)
                return;

            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool hasOutfit = false, hasHair = false, hasExposedSkin = false;
            foreach (var skin in skins)
            {
                hasOutfit |= skin.name == "TG_Outfit";
                hasHair |= skin.name == "TG_Hair";
                hasExposedSkin |= skin.name == "TG_ExposedSkin";
            }

            if (!hasOutfit || !hasHair || !hasExposedSkin)
                return;

            foreach (var skin in skins)
                if (skin.name == "TG_GameMesh")
                    skin.enabled = false;
        }
    }
}
