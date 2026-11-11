using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace EcoStack
{
    public partial class EcoStackGame
    {
        readonly Dictionary<string,Material> surfaces = new Dictionary<string,Material>();

        Material SurfaceFor(string objectName, Color tint)
        {
            string texture = "";
            if (objectName.Contains("Placa") || objectName.Contains("Máquina") || objectName.Contains("Muro") || objectName.Contains("Pared")) texture="MetalPlates";
            if (objectName.Contains("Tubería") || objectName.Contains("Barril") || objectName.Contains("Columna") || objectName.Contains("Tanque")) texture="RustSurface";
            if (objectName.Contains("Baldosa") || objectName.Contains("Camino")) texture="StonePath";
            if (objectName.Contains("seguridad") || objectName.Contains("Señal")) texture="HazardStripes";
            bool glow=objectName.Contains("Luz") || objectName.Contains("Lámpara") || objectName.Contains("Pantalla") || objectName.Contains("Ventana tóxica");
            bool water=objectName.Contains("Agua") || (objectName.StartsWith("Hábitat") && tint.b>.6f);
            if (texture=="" && !glow && !water) return MakeMaterial(tint);
            string key=texture+"/"+ColorUtility.ToHtmlStringRGBA(tint)+"/"+glow+"/"+water;
            Material material;
            if(surfaces.TryGetValue(key,out material)) return material;
            if(water)
            {
                material=new Material(Resources.Load<Shader>("EcoStackWater"));
                material.color=new Color(.04f,.62f,.71f,1);
            }
            else
            {
                Material template=LitTemplate();
                material=template!=null ? new Material(template) : new Material(Resources.Load<Shader>("EcoStackColor"));
                material.color=tint;
                if(texture!="")
                {
                    Texture2D map=Resources.Load<Texture2D>(texture);
                    Texture2D normal=Resources.Load<Texture2D>(texture+"Normal");
                    if(normal!=null && material.HasProperty("_BumpMap"))
                    {
                        material.SetTexture("_BumpMap",normal);
                        material.EnableKeyword("_NORMALMAP");
                        material.SetFloat("_BumpScale",.7f);
                    }
                    if(material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap",map);
                    if(material.HasProperty("_MainTex")) material.SetTexture("_MainTex",map);
                    material.color=Color.Lerp(tint,Color.white,.3f);
                    if(texture=="HazardStripes") material.color=Color.white;
                    if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",texture=="StonePath" ? 0 : .4f);
                    if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.32f);
                }
                if(glow)
                {
                    material.EnableKeyword("_EMISSION");
                    if(material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",tint*2);
                }
            }
            materials.Add(material);
            surfaces[key]=material;
            return material;
        }

        Material LitTemplate()
        {
            return Resources.Load<Material>("EcoStackLit");
        }

        void Lamp(Vector3 position,Color color)
        {
            Light lamp=new GameObject("Iluminación local").AddComponent<Light>();
            lamp.transform.SetParent(environmentRoot,false);
            lamp.transform.localPosition=position;
            lamp.type=LightType.Point;
            lamp.color=color;
            lamp.intensity=5;
            lamp.range=18;
            lamp.shadows=LightShadows.None;
        }

        void Steam(Vector3 position)
        {
            GameObject emitter=new GameObject("Fuga de vapor");
            emitter.transform.SetParent(environmentRoot,false);
            emitter.transform.localPosition=position;
            emitter.transform.localRotation=Quaternion.Euler(-90,0,0);
            ParticleSystem particles=emitter.AddComponent<ParticleSystem>();
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;
            main.duration=6;
            main.loop=true;
            main.startLifetime=3;
            main.startSpeed=.9f;
            main.startSize=new ParticleSystem.MinMaxCurve(.5f,1.8f);
            main.startColor=new Color(.8f,.83f,.85f,.6f);
            main.maxParticles=40;
            var emission=particles.emission;
            emission.rateOverTime=7;
            var shape=particles.shape;
            shape.shapeType=ParticleSystemShapeType.Cone;
            shape.angle=15;
            shape.radius=.12f;
            var size=particles.sizeOverLifetime;
            size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,1.6f));
            Material smoke=new Material(Resources.Load<Shader>("EcoStackSteam"));
            smoke.color=new Color(.7f,.75f,.78f,.22f);
            materials.Add(smoke);
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=smoke;
            particles.Play();
        }

        void AddVisualDetail()
        {
            Volume volume=new GameObject("Color y brillo V4").AddComponent<Volume>();
            volume.transform.SetParent(environmentRoot,false);
            volume.isGlobal=true;
            volume.priority=20;
            volume.sharedProfile=Resources.Load<VolumeProfile>("EcoStackProfileV4");
            for(int z=25;z<180;z+=40) Steam(new Vector3(-14,2,z));
            for(int z=12;z<180;z+=30)
            {
                Lamp(new Vector3(-9,5,z),new Color(1,.43f,.18f));
                Lamp(new Vector3(9,6,z+8),new Color(.3f,.67f,.85f));
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 pos=new Vector3(side*10,3,z);
                    Block("Carcasa alarma",pos,new Vector3(.6f,1.1f,.6f),steel);
                    Shape("Luz roja",PrimitiveType.Capsule,pos+new Vector3(0,0,-.3f),new Vector3(.4f,.4f,.4f),new Color(1,.08f,.03f),environmentRoot);
                    for(int bar=-1;bar<=1;bar++) Block("Reja alarma",pos+new Vector3(bar*.16f,0,-.5f),new Vector3(.045f,.85f,.06f),steel);
                    Block("Consola control",new Vector3(side*11,1,z+4),new Vector3(2,2,1),steel);
                    for(int b=0;b<3;b++) Shape("Botón consola",PrimitiveType.Sphere,new Vector3(side*11-.5f+b*.5f,1.4f,z+3.45f),Vector3.one*.17f,b==0?Color.red:Color.green,environmentRoot);
                }
            }
            // A decorated service platform outside the central playable lane.
            for(int z=35;z<170;z+=55)
            {
                Block("Pasarela elevada",new Vector3(15,2.8f,z),new Vector3(6,.35f,10),steel);
                Fence(new Vector3(12,2.8f,z-5),new Vector3(12,2.8f,z+5));
                for(int step=0;step<8;step++) Block("Escalón",new Vector3(15,step*.35f,z-10+step*.65f),new Vector3(3,.3f,.7f),brass);
            }
            for(int z=185;z<240;z+=5)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    for(int leaf=0;leaf<3;leaf++)
                    {
                        Transform plant=Shape("Hoja",PrimitiveType.Cube,new Vector3(side*18+leaf*.3f,.7f,z),new Vector3(.18f,1.7f,.5f),new Color(.25f+leaf*.12f,.65f,.12f),environmentRoot);
                        plant.rotation=Quaternion.Euler(leaf*20,leaf*50,side*(20+leaf*12));
                    }
                    Shape("Flor",PrimitiveType.Sphere,new Vector3(side*17.5f,1.2f,z),new Vector3(.65f,.3f,.65f),new Color(1,.38f,.65f),environmentRoot);
                }
            }
            for(int x=-12;x<=12;x+=6)
            {
                Transform panel=Block("Panel solar",new Vector3(x,7,183),new Vector3(5,.16f,3),new Color(.09f,.18f,.38f));
                panel.rotation=Quaternion.Euler(22,0,0);
                for(int i=-2;i<=2;i++) Beam("Marco solar",new Vector3(x+i,7.65f,181.6f),new Vector3(x+i,6.5f,184.4f),.04f,Color.gray);
            }
        }
    }
}
