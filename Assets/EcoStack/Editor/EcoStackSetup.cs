using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoStack.Editor
{
    public static class EcoStackSetup
    {
        const string ScenePath = "Assets/EcoStack/Scenes/EcoStack.unity";

        [MenuItem("Eco-Stack/Abrir escena del juego")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string[] candidates=AssetDatabase.FindAssets("EcoStack_Editable_V6 t:Scene",new string[]{"Assets/EcoStack"});
            if(candidates.Length>0) EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(candidates[candidates.Length-1]));
            else { EcoStackVisualSetup.CreateVisualScene(); return; }
            EcoStackGame game=Object.FindObjectOfType<EcoStackGame>();
            if(game!=null) Selection.activeGameObject=game.gameObject;
        }

        [MenuItem("Eco-Stack/Crear entorno editable sin Play")]
        public static void CreateEditable()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Eco-Stack", "Detén Play antes de crear el entorno editable.", "Aceptar");
                return;
            }
            EcoStackGame game = Object.FindObjectOfType<EcoStackGame>();
            if (game == null)
            {
                EditorUtility.DisplayDialog("Eco-Stack", "Abre primero la escena EcoStack.", "Aceptar");
                return;
            }
            if (game.EnvironmentRoot != null)
            {
                Selection.activeGameObject = game.EnvironmentRoot.gameObject;
                EditorUtility.DisplayDialog("Eco-Stack", "El entorno ya está creado. Despliega Entorno editable en Hierarchy para modificarlo.", "Aceptar");
                return;
            }
            Undo.RecordObject(game,"Crear entorno editable");
            game.CreateEditableEnvironment();
            Undo.RegisterCreatedObjectUndo(game.EnvironmentRoot.gameObject,"Crear entorno editable");
            const string folder = "Assets/EcoStack/EntornosGuardados";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/EcoStack","EntornosGuardados");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder+"/RecursosEntorno.asset");
            Material container = new Material(Resources.Load<Shader>("EcoStackColor"));
            container.name = "Recursos del entorno";
            AssetDatabase.CreateAsset(container,assetPath);
            foreach (Renderer renderer in game.EnvironmentRoot.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && !EditorUtility.IsPersistent(material))
                        AssetDatabase.AddObjectToAsset(material,assetPath);
                }
            }
            foreach (MeshFilter filter in game.EnvironmentRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh != null && !EditorUtility.IsPersistent(mesh))
                    AssetDatabase.AddObjectToAsset(mesh,assetPath);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Selection.activeGameObject = game.EnvironmentRoot.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0,3,20),Quaternion.Euler(15,0,0),18);
            EditorUtility.DisplayDialog("Entorno creado", "Ya puedes editarlo en Scene. Guarda con Ctrl+S. Durante Play se usará este entorno guardado.", "Aceptar");
        }

        [MenuItem("Eco-Stack/Agregar escena a Build Settings")]
        public static void AddBuildScene()
        {
            string currentPath=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if(string.IsNullOrEmpty(currentPath)) { EditorUtility.DisplayDialog("Eco-Stack","Guarda primero tu escena.","Aceptar"); return; }
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(scene => scene.path == currentPath);
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(currentPath, true));
            else scenes[index].enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorUtility.DisplayDialog("Eco-Stack", "Escena agregada. Para exportar solo este juego, deja EcoStack como primera y única escena habilitada en tu perfil de compilación.", "Aceptar");
        }
    }
}
