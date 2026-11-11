using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EcoStack.Editor
{
    [InitializeOnLoad]
    public static class EcoStackVisualSetup
    {
        static EcoStackVisualSetup() { EditorApplication.delayCall += EnsureMaterial; }

        public static void EnsureMaterial()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureMaterial;
                return;
            }
            const string path="Assets/EcoStack/Resources/EcoStackLit.mat";
            Shader shader=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null
                ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
            if (shader==null) return;
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material==null)
            {
                material=new Material(shader);
                material.name="EcoStack Lit";
                AssetDatabase.CreateAsset(material,path);
            }
            if(material.shader!=shader) material.shader=shader;
            if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.28f);
            if(material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",.28f);
            EditorUtility.SetDirty(material);
            const string profilePath="Assets/EcoStack/Resources/EcoStackProfileV4.asset";
            VolumeProfile profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile,profilePath);
                Bloom bloom=profile.Add<Bloom>(true);
                bloom.intensity.Override(.35f);
                bloom.threshold.Override(1.1f);
                Tonemapping tone=profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);
                ColorAdjustments grade=profile.Add<ColorAdjustments>(true);
                grade.postExposure.Override(.35f);
                grade.contrast.Override(8);
                grade.saturation.Override(8);
                foreach(VolumeComponent component in profile.components)
                    AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Eco-Stack/Crear nueva escena editable V6")]
        public static void CreateVisualScene()
        {
            if(EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Eco-Stack","Detén Play primero.","Aceptar");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureMaterial();
            if(Resources.Load<Material>("EcoStackLit")==null)
            {
                EditorUtility.DisplayDialog("Eco-Stack","Esta versión está preparada para URP. No se pudo crear el material. Espera a que termine la importación y vuelve a intentar.","Aceptar");
                return;
            }
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("EcoStack V6").AddComponent<EcoStackGame>();
            EcoStackSetup.CreateEditable();
            EcoStackEditableSetup.Prepare();
            Object.FindObjectOfType<EcoStackGame>().ApplyDefaultLighting();
            if(!AssetDatabase.IsValidFolder("Assets/EcoStack/Scenes")) AssetDatabase.CreateFolder("Assets/EcoStack","Scenes");
            string path=AssetDatabase.GenerateUniqueAssetPath("Assets/EcoStack/Scenes/EcoStack_Editable_V6.unity");
            EditorSceneManager.SaveScene(scene,path);
        }
    }
}
