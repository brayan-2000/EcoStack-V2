using UnityEditor;

namespace EcoStack.Editor
{
    public class EcoStackImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/EcoStack/Resources/")) return;
            TextureImporter importer=(TextureImporter)assetImporter;
            importer.mipmapEnabled=true;
            importer.anisoLevel=4;
            if(assetPath.EndsWith("Normal.png")) importer.textureType=TextureImporterType.NormalMap;
        }
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/EcoStack/Resources/Models/")) return;
            ModelImporter importer=(ModelImporter)assetImporter;
            importer.globalScale=1;
            importer.importNormals=ModelImporterNormals.Calculate;
            importer.normalSmoothingAngle=80;
            importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.importAnimation=false;
        }
    }
}
