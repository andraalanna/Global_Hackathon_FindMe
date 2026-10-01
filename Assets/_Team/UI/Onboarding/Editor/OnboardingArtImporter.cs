using System.IO;
using UnityEditor;
using UnityEngine;

namespace FindMe.Onboarding
{
    /// <summary>
    /// Import rule for everything in Assets/_Team/UI/Art: always a crisp UI sprite (no mipmaps, real
    /// transparency), and the shape sprites get 9-slice borders so corners keep their roundness at any size.
    /// Runs automatically whenever Unity (re)imports a file there.
    /// </summary>
    public class OnboardingArtImporter : AssetPostprocessor
    {
        const string Root = "Assets/_Team/UI/Onboarding/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.spritePixelsPerUnit = 100f;

            // Border = the corner size in the source image (radius × 2 because the art is @2x, plus anti-aliasing).
            float b = Path.GetFileNameWithoutExtension(assetPath) switch
            {
                "rounded_18" => 40f,
                "rounded_28" => 58f,    // UIKit.RoundedBorder must match this
                "pill" => 115f,
                _ => 0f,
            };
            ti.spriteBorder = new Vector4(b, b, b, b);
        }
    }
}
