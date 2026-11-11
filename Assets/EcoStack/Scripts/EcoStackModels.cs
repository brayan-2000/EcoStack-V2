using UnityEngine;
using System.Collections.Generic;

namespace EcoStack
{
    public partial class EcoStackGame
    {
        readonly Dictionary<string,Mesh> modelCache = new Dictionary<string,Mesh>();

        Mesh ModelMesh(string model)
        {
            Mesh mesh;
            if(modelCache.TryGetValue(model,out mesh)) return mesh;
            GameObject resource=Resources.Load<GameObject>("Models/"+model);
            if(resource==null) return null;
            MeshFilter filter=resource.GetComponentInChildren<MeshFilter>();
            if(filter==null) return null;
            mesh=filter.sharedMesh;
            modelCache[model]=mesh;
            return mesh;
        }

        void RefineMesh(GameObject obj,string name,PrimitiveType type)
        {
            string model="";
            if(type==PrimitiveType.Cube && !name.Contains("Rejilla") && !name.Contains("Baldosa") && !name.Contains("Placa") && !name.Contains("Terreno")) model="RoundedBox";
            if(name.Contains("Roca")) model="Rock";
            if(name.Contains("Copa")) model="Canopy";
            if(name=="Hoja") model="Leaf";
            if(name=="Charco químico") model="Puddle";
            if(name=="Aro hélice") model="Ring";
            if(model=="") return;
            Mesh mesh=ModelMesh(model);
            if(mesh!=null) obj.GetComponent<MeshFilter>().sharedMesh=mesh;
        }

        Transform BuildHazardModel(int kind,Vector3 position)
        {
            Transform root=new GameObject("Obstáculo "+kind).transform;
            root.SetParent(transform,false);
            root.localPosition=position;
            Color dark=new Color(.09f,.12f,.14f);
            if(kind==2)
            {
                Shape("Charco químico",PrimitiveType.Sphere,Vector3.down*.3f,new Vector3(5.4f,.12f,3.4f),new Color(.42f,.85f,.08f),root);
                for(int i=0;i<5;i++) Shape("Burbuja",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*2)*1.7f,-.22f,Mathf.Cos(i*2)),Vector3.one*.16f,new Color(.7f,1,.15f),root);
                return root;
            }
            if(kind==3)
            {
                Shape("Banda transportadora",PrimitiveType.Cube,Vector3.down*.3f,new Vector3(5,.22f,3),dark,root);
                for(int i=0;i<9;i++) Shape("Banda",PrimitiveType.Cube,new Vector3(0,-.15f,-1.3f+i*.32f),new Vector3(4.7f,.08f,.13f),new Color(.32f,.35f,.36f),root);
                Shape("Franja de seguridad",PrimitiveType.Cube,new Vector3(-2.5f,-.1f,0),new Vector3(.18f,.3f,3.2f),Color.white,root);
                Shape("Franja de seguridad",PrimitiveType.Cube,new Vector3(2.5f,-.1f,0),new Vector3(.18f,.3f,3.2f),Color.white,root);
                return root;
            }
            Shape("Carcasa máquina",PrimitiveType.Cube,Vector3.zero,new Vector3(1.5f,.65f,1.2f),kind==1?new Color(.7f,.23f,.12f):brass,root);
            Shape("Visor",PrimitiveType.Cube,new Vector3(0,.05f,-.62f),new Vector3(.9f,.28f,.08f),dark,root);
            Shape("Luz del sensor",PrimitiveType.Sphere,new Vector3(0,.06f,-.7f),new Vector3(.22f,.22f,.1f),kind==1?Color.red:Color.cyan,root);
            for(int side=-1;side<=1;side+=2)
            {
                for(int end=-1;end<=1;end+=2)
                {
                    if(kind==1)
                    {
                        Shape("Brazo dron",PrimitiveType.Cube,new Vector3(side*.85f,.1f,end*.5f),new Vector3(.8f,.16f,.14f),dark,root);
                        Transform ring=Shape("Aro hélice",PrimitiveType.Cylinder,new Vector3(side*1.2f,.18f,end*.6f),new Vector3(.85f,.85f,.85f),dark,root);
                        Transform rotor=Shape("Hélice",PrimitiveType.Cube,ring.localPosition+Vector3.up*.02f,new Vector3(.7f,.045f,.09f),new Color(.65f,.68f,.7f),root);
                        rotor.gameObject.AddComponent<EcoRotor>();
                    }
                    else
                    {
                        Transform wheel=Shape("Rueda",PrimitiveType.Cylinder,new Vector3(side*.8f,-.17f,end*.36f),new Vector3(.5f,.13f,.5f),dark,root);
                        wheel.localRotation=Quaternion.Euler(0,0,90);
                    }
                }
            }
            if(kind==0)
                for(int i=-1;i<=1;i++) Shape("Cepillo",PrimitiveType.Cylinder,new Vector3(i*.4f,-.28f,-.85f),new Vector3(.48f,.12f,.48f),brass,root);
            return root;
        }
    }
}
