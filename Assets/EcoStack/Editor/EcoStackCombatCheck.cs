using UnityEditor;
using UnityEngine;
using System;
namespace EcoStack.Editor
{
    public static class EcoStackCombatCheck
    {
        [MenuItem("Eco-Stack/Verificar recursos y cálculo de impactos V5")]
        public static void Run()
        {
            Require(Mathf.Abs(EcoStackGame.SegmentHit(Vector3.zero,Vector3.right*10,Vector3.right*5,1)-.4f)<.0001f,"Impacto frontal");
            Require(EcoStackGame.SegmentHit(Vector3.zero,Vector3.right*10,new Vector3(5,2,0),1)<0,"Proyectil que pasa al lado");
            Require(EcoStackGame.SegmentHit(Vector3.zero,Vector3.right*10,Vector3.left*5,1)<0,"Objetivo detrás");
            Require(EcoStackGame.SegmentHit(Vector3.zero,Vector3.right,Vector3.right*5,1)<0,"Objetivo fuera del segmento");
            Require(EcoStackGame.SegmentHit(Vector3.zero,Vector3.zero,Vector3.zero,1)==0,"Origen dentro del objetivo");
            Require(EcoStackGame.SegmentHit(Vector3.zero,Vector3.zero,Vector3.right*5,1)<0,"Segmento sin movimiento");
            Require(Mathf.Abs(EcoStackGame.SegmentHit(Vector3.zero,Vector3.right*10,new Vector3(5,1,0),1)-.5f)<.0001f,"Impacto tangente");
            foreach(string asset in new string[]{"RoundedBox","Rock","Canopy","Puddle","Leaf","Ring"})
                Require(Resources.Load<GameObject>("Models/"+asset)!=null,"Modelo "+asset);
            foreach(string asset in new string[]{"MetalPlates","RustSurface","StonePath","MetalPlatesNormal","RustSurfaceNormal","StonePathNormal"})
                Require(Resources.Load<Texture2D>(asset)!=null,"Textura "+asset);
            Require(Resources.Load<Material>("EcoStackLit")!=null,"Material URP");
            EditorUtility.DisplayDialog("Comprobación V5","7 cálculos de impacto y los recursos principales están correctos. Esto no sustituye jugar y comprobar combate, guardado y rendimiento.","Aceptar");
        }
        static void Require(bool condition,string check)
        {
            if(!condition) throw new Exception("EcoStack V5: falla "+check);
        }
    }
}
