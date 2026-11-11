using UnityEngine;

namespace EcoStack
{
    public partial class EcoStackGame
    {
        readonly Color steel = new Color(.19f,.24f,.27f);
        readonly Color rust = new Color(.56f,.25f,.10f);
        readonly Color brass = new Color(.91f,.58f,.14f);
        readonly Color foliage = new Color(.22f,.53f,.12f);

        Transform Block(string name, Vector3 position, Vector3 size, Color color, bool solid=false)
        {
            return Shape(name,PrimitiveType.Cube,position,size,color,environmentRoot,solid);
        }

        void Beam(string name, Vector3 start, Vector3 end, float width, Color color)
        {
            Transform beam=Block(name,(start+end)*.5f,new Vector3(width,(end-start).magnitude,width),color);
            beam.rotation=Quaternion.FromToRotation(Vector3.up,end-start);
        }

        void Pipe(Vector3 start,Vector3 end,float diameter)
        {
            Transform pipe=Shape("Tubería industrial",PrimitiveType.Cylinder,(start+end)*.5f,
                new Vector3(diameter,(end-start).magnitude*.5f,diameter),rust,environmentRoot);
            pipe.rotation=Quaternion.FromToRotation(Vector3.up,end-start);
            for(int i=0;i<=4;i++)
            {
                Transform joint=Shape("Junta",PrimitiveType.Cylinder,Vector3.Lerp(start,end,i/4f),new Vector3(diameter*1.2f,.12f,diameter*1.2f),steel,environmentRoot);
                joint.rotation=pipe.rotation;
            }
        }

        void Barrel(Vector3 position)
        {
            Shape("Barril de residuos",PrimitiveType.Cylinder,position+Vector3.up*.8f,new Vector3(1.1f,.8f,1.1f),brass,environmentRoot,true);
            foreach(float y in new float[]{.15f,.8f,1.45f})
                Shape("Aro del barril",PrimitiveType.Cylinder,position+Vector3.up*y,new Vector3(1.15f,.045f,1.15f),steel,environmentRoot);
            Block("Señal de peligro",position+new Vector3(0,.8f,-.56f),new Vector3(.36f,.36f,.02f),Color.black);
        }

        void Tree(Vector3 position,float scale)
        {
            Shape("Tronco",PrimitiveType.Cylinder,position+Vector3.up*scale,new Vector3(.5f*scale,scale,.5f*scale),new Color(.36f,.24f,.12f),environmentRoot);
            for(int i=0;i<3;i++)
            {
                Vector3 offset=new Vector3(Mathf.Sin(i*2.1f)*scale*.5f,scale*(2+i*.32f),Mathf.Cos(i*2.1f)*scale*.5f);
                Shape("Copa verde",PrimitiveType.Sphere,position+offset,Vector3.one*scale*1.8f,foliage*(1+i*.16f),environmentRoot);
            }
        }

        void Fence(Vector3 start,Vector3 end)
        {
            int steps=Mathf.CeilToInt(Vector3.Distance(start,end)/2);
            for(int i=0;i<=steps;i++)
                Block("Poste",Vector3.Lerp(start,end,(float)i/steps)+Vector3.up*.65f,new Vector3(.15f,1.3f,.15f),brass);
            Beam("Baranda",start+Vector3.up*1.2f,end+Vector3.up*1.2f,.12f,brass);
            Beam("Baranda",start+Vector3.up*.55f,end+Vector3.up*.55f,.10f,brass);
        }

        Material domeGlass;
        readonly System.Collections.Generic.List<Mesh> domeMeshes = new System.Collections.Generic.List<Mesh>();

        void GlassPanel(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            if (domeGlass == null)
            {
                domeGlass = new Material(Resources.Load<Shader>("EcoStackGlass"));
                domeGlass.color = new Color(.4f,.85f,.95f,.10f);
                materials.Add(domeGlass);
            }
            GameObject panel = new GameObject("Vidrio del domo");
            panel.transform.SetParent(environmentRoot,false);
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] {a,b,c,d};
            mesh.triangles = new int[] {0,1,2,0,2,3};
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            domeMeshes.Add(mesh);
            panel.AddComponent<MeshFilter>().sharedMesh = mesh;
            panel.AddComponent<MeshRenderer>().sharedMaterial = domeGlass;
        }

        void BuildEnvironment()
        {
            environmentRoot = new GameObject("Entorno editable").transform;
            environmentRoot.SetParent(transform, false);
            RenderSettings.ambientLight=new Color(.65f,.72f,.75f);
            RenderSettings.fog=false;
            Light sun=new GameObject("Luz del mundo").AddComponent<Light>();
            sun.transform.SetParent(environmentRoot);
            sun.type=LightType.Directional;
            sun.intensity=1.7f;
            sun.shadows=LightShadows.Soft;
            sun.shadowStrength=.85f;
            sun.shadowBias=.04f;
            sun.transform.rotation=Quaternion.Euler(45,-25,0);

            // Continuous walkable collision surface. All decorative floor plates have no collider.
            Block("Terreno fábrica 40 x 184",new Vector3(0,-.5f,88),new Vector3(40,1,184),steel,true);
            Block("Terreno santuario 40 x 64",new Vector3(0,-.5f,212),new Vector3(40,1,64),new Color(.28f,.45f,.20f),true);
            Block("Muro de entrada",new Vector3(0,6,-4),new Vector3(40,12,.5f),steel,true);
            Block("Muro final",new Vector3(0,3,244),new Vector3(40,6,.5f),foliage,true);
            for(int z=0;z<180;z+=4)
            {
                for(int x=-16;x<=16;x+=4)
                {
                    Color plate=((x/4+z/4)%2==0) ? new Color(.26f,.29f,.29f) : new Color(.34f,.31f,.26f);
                    Block("Placa metálica",new Vector3(x,.015f,z),new Vector3(3.92f,.03f,3.92f),plate);
                }
                // Narrow crossbars form a grated central walkway.
                for(int j=0;j<4;j++) Block("Rejilla",new Vector3(0,.05f,z+j*.9f),new Vector3(5,.035f,.075f),rust);
                foreach(int side in new int[]{-1,1})
                {
                    Transform stripe=Block("Franja de seguridad",new Vector3(side*12,.055f,z),new Vector3(.8f,.04f,1.5f),brass);
                    stripe.rotation=Quaternion.Euler(0,35,0);
                }
            }
            foreach(int side in new int[]{-1,1})
            {
                Block("Pared industrial",new Vector3(side*20,6,88),new Vector3(.5f,12,184),steel,true);
                Block("Límite santuario",new Vector3(side*20,2,212),new Vector3(.4f,4,64),foliage,true);
                Pipe(new Vector3(side*17,9,0),new Vector3(side*17,9,178),1.6f);
                for(int z=8;z<180;z+=12)
                {
                    Block("Columna",new Vector3(side*18,6,z),new Vector3(.7f,12,.7f),rust);
                    Beam("Refuerzo diagonal",new Vector3(side*18,10,z),new Vector3(side*13,13,z),.45f,rust);
                    Pipe(new Vector3(side*16,1,z),new Vector3(side*16,9,z),.8f);
                    Block("Lámpara alarma",new Vector3(side*17,4,z),new Vector3(.4f,.85f,.4f),new Color(1,.12f,.05f));
                    for(int bar=0;bar<3;bar++) Barrel(new Vector3(side*(14+bar*1.2f),0,z+3));
                    Block("Máquina",new Vector3(side*17,1.4f,z+7),new Vector3(3,2.8f,3),steel,true);
                    Block("Panel máquina",new Vector3(side*17,1.8f,z+5.48f),new Vector3(1.8f,1,.04f),new Color(.08f,.72f,.7f));
                    Fence(new Vector3(side*13,0,z-3),new Vector3(side*13,0,z+2));
                }
            }
            for(int z=8;z<180;z+=12)
            {
                Beam("Viga superior",new Vector3(-18,13,z),new Vector3(18,13,z),.6f,rust);
                Pipe(new Vector3(-16,10.5f,z+3),new Vector3(16,10.5f,z+3),.75f);
                Block("Techo",new Vector3(0,14,z),new Vector3(40,.35f,8),steel);
                Block("Luz techo",new Vector3(0,13.6f,z),new Vector3(3,.1f,.35f),new Color(1,.81f,.42f));
            }
            for(int z=18;z<160;z+=45)
            {
                Shape("Tanque industrial",PrimitiveType.Cylinder,new Vector3(-16,4,z),new Vector3(5,4,5),rust,environmentRoot);
                Block("Ventana tóxica",new Vector3(-16,4,z-2.52f),new Vector3(1.8f,4,.06f),new Color(.4f,1,.05f));
            }
            foreach(float x in new float[]{-18,18})
            {
                Block("Portal de rescate",new Vector3(x,5.5f,180),new Vector3(1.4f,11,1.6f),steel);
                Block("Luz del portal",new Vector3(x,5.5f,179),new Vector3(.35f,10,.2f),Color.green);
            }
            Block("Arco del santuario",new Vector3(0,11,180),new Vector3(37,1,1.6f),steel);
            Block("Luz arco",new Vector3(0,10.8f,179),new Vector3(35,.3f,.2f),Color.green);
            Label("SANTUARIO  /  ENTREGA AUTOMÁTICA",new Vector3(0,12,180),environmentRoot,.23f);
            Label("ZONA DE RESCATE",new Vector3(-12,3,9),environmentRoot,.06f);

            AddVisualDetail();
            AddCombatLayout();

            // Broad stone paths and visible collection circles around the four habitats.
            for(int z=182;z<244;z+=3)
                for(int x=-3;x<=3;x+=3)
                    Block("Baldosa de piedra",new Vector3(x,.04f,z),new Vector3(2.9f,.08f,2.9f),new Color(.65f,.64f,.51f));
            for(int k=0;k<4;k++)
            {
                Vector3 center=pads[k];
                Block("Hábitat "+species[k],center+new Vector3(0,.06f,3),new Vector3(12,.12f,12),k<2 ? new Color(.06f,.7f,.77f) : new Color(.4f,.6f,.22f));
                Block("Camino al recinto",center+new Vector3(0,.07f,-2),new Vector3(14,.14f,2.4f),new Color(.65f,.64f,.51f));
                collectionPads[k]=Shape("Círculo de cobro",PrimitiveType.Cylinder,center+new Vector3(0,.18f,-2),new Vector3(2,.16f,2),brass,environmentRoot);
                padLabels[k]=Label("",center+new Vector3(0,3,-2),environmentRoot,.13f);
                AnimalModel(k,center+new Vector3(0,.55f,3),environmentRoot);
                Fence(center+new Vector3(-6,0,9),center+new Vector3(6,0,9));
                Fence(center+new Vector3(-6,0,-1),center+new Vector3(-6,0,9));
                Fence(center+new Vector3(6,0,-1),center+new Vector3(6,0,9));
                for(int i=0;i<4;i++)
                {
                    Vector3 rock=center+new Vector3(-4+i*2.6f,.4f,7);
                    Transform stone=Shape("Roca",PrimitiveType.Cube,rock,new Vector3(1.8f,.8f,1.4f),new Color(.5f,.57f,.48f),environmentRoot);
                    stone.rotation=Quaternion.Euler(0,i*31,12);
                }
            }
            for(int z=187;z<241;z+=9)
                foreach(int side in new int[]{-1,1}) Tree(new Vector3(side*18,0,z),2.2f);
            // Faceted vaulted canopy, crossed ribs evoke the glass dome in the reference.
            for(int z=181;z<=241;z+=10)
            {
                for(int segment=0;segment<8;segment++)
                {
                    float a=segment*Mathf.PI/8;
                    float b=(segment+1)*Mathf.PI/8;
                    Vector3 first=new Vector3(Mathf.Cos(a)*20,5+Mathf.Sin(a)*15,z);
                    Vector3 second=new Vector3(Mathf.Cos(b)*20,5+Mathf.Sin(b)*15,z);
                    Beam("Arco domo",first,second,.18f,new Color(.72f,.85f,.80f));
                    if(z<241)
                    {
                        GlassPanel(first,second,second+Vector3.forward*10,first+Vector3.forward*10);
                        Beam("Costilla domo",first,first+Vector3.forward*10,.14f,new Color(.72f,.85f,.80f));
                        Beam("Diagonal domo",first,second+Vector3.forward*10,.10f,new Color(.72f,.85f,.80f));
                    }
                }
            }
            Block("Panel de mejoras",new Vector3(0,1.7f,239),new Vector3(5,3.4f,1.5f),steel);
            Block("Pantalla mejoras",new Vector3(0,2,238.2f),new Vector3(4,1.6f,.05f),new Color(.1f,.8f,.5f));
            Label("TIENDA DE MEJORAS",new Vector3(0,4,239),environmentRoot,.16f);
            for(int x=-15;x<=15;x+=5)
            {
                Block("Roca de cascada",new Vector3(x,2,242),new Vector3(4,4,3),new Color(.47f,.53f,.43f));
                Block("Agua de cascada",new Vector3(x,2,240.45f),new Vector3(1.8f,3.8f,.09f),new Color(.18f,.73f,.85f));
            }
        }
    }
}
