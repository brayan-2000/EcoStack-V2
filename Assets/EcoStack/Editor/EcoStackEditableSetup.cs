using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EcoStack.Editor
{
    public static class EcoStackEditableSetup
    {
        [MenuItem("Eco-Stack/Preparar TODO para editar sin Play")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorUtility.DisplayDialog("Eco-Stack","Detén Play primero.","Aceptar"); return; }
            EcoStackGame game=Object.FindObjectOfType<EcoStackGame>();
            if(game==null) { EditorUtility.DisplayDialog("Eco-Stack","Abre tu escena EcoStack primero.","Aceptar"); return; }
            if(game.ActorsPrepared)
            { Selection.activeGameObject=game.gameObject; EditorUtility.DisplayDialog("Eco-Stack","Ya está preparado. Despliega Rescatista y Actores editables. No se han creado duplicados.","Aceptar"); return; }
            EcoStackVisualSetup.EnsureMaterial();
            Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Preparar actores editables");
            game.PrepareEditableActors();
            const string folder="Assets/EcoStack/EntornosGuardados";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/EcoStack","EntornosGuardados");
            string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/RecursosActores.asset");
            Material container=new Material(Resources.Load<Shader>("EcoStackColor"));
            AssetDatabase.CreateAsset(container,path);
            foreach(Renderer renderer in game.GetComponentsInChildren<Renderer>(true))
                foreach(Material material in renderer.sharedMaterials)
                    if(material!=null && !EditorUtility.IsPersistent(material)) AssetDatabase.AddObjectToAsset(material,path);
            foreach(MeshFilter filter in game.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null && !EditorUtility.IsPersistent(filter.sharedMesh)) AssetDatabase.AddObjectToAsset(filter.sharedMesh,path);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Transform player=game.transform.Find("Rescatista");
            Selection.activeGameObject=player!=null?player.gameObject:game.gameObject;
            if(player!=null && SceneView.lastActiveSceneView!=null)
                SceneView.lastActiveSceneView.LookAt(player.position+Vector3.up,Quaternion.Euler(12,155,0),5);
            EditorUtility.DisplayDialog("Todo preparado","Rescatista, arma, cámara, animales, enemigos y botiquines ya están en la escena. Guarda con Ctrl+S. Edita fuera de Play.","Aceptar");
        }
    }
}
