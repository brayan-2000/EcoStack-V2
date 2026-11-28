using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoStack
{
    // A self-contained prototype. Only the scene containing this component starts the game.
    public partial class EcoStackGame : MonoBehaviour
    {
        const string SaveKey = "EcoStack.Prototype.v1";
        readonly string[] species = { "Ajolote", "Tortuga marina", "Gorila de montaña", "Pangolín" };
        readonly int[] rates = { 10, 25, 60, 120 };
        readonly Color[] colors = { new Color(1f,.57f,.68f), new Color(.37f,.72f,.28f), new Color(.48f,.53f,.58f), new Color(.72f,.49f,.28f) };
        readonly List<Animal> animals = new List<Animal>();
        readonly List<Animal> stack = new List<Animal>();
        readonly List<Hazard> hazards = new List<Hazard>();
        readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
        readonly List<Material> materials = new List<Material>();
        readonly Vector3[] pads = { new Vector3(-10,0,197), new Vector3(10,0,197), new Vector3(-10,0,220), new Vector3(10,0,220) };
        [SerializeField] float sanctuaryEntryZ=180;
        [SerializeField] Vector3 sanctuaryRespawn=new Vector3(0,.1f,187);
        [SerializeField] Transform[] collectionPads=new Transform[4];
        [SerializeField, HideInInspector] TextMesh[] padLabels = new TextMesh[4];
        [SerializeField, HideInInspector] Transform environmentRoot;
        public Transform EnvironmentRoot { get { return environmentRoot; } }
        public void CreateEditableEnvironment() { if (environmentRoot == null) BuildEnvironment(); }
        [SerializeField] Transform player, body, cameraTransform;
        [SerializeField,HideInInspector] CharacterController controller;
        SaveData data;
        float verticalSpeed, damageUntil, pickupAfter, saveTimer, gameTime;
        bool paused, confirmReset;
        string message = "Recoge animales en la fábrica y cruza la puerta verde.";
        float messageUntil = 10;
        AudioSource audioSource;
        AudioClip pickupSound, deliverySound, hitSound;
        GUIStyle heading, text, button;

        [Serializable]
        class SaveData
        {
            public int coins;
            public int capacityLevel;
            public int speedLevel;
            public int shields;
            public int weaponLevel;
            public bool missionCompleted;
            public int[] rescued = new int[4];
            public float[] pending = new float[4];
        }
        class Animal
        {
            public Transform view;
            public int kind;
            public Vector3 home;
            public bool carried;
            public float respawnAt;
        }
        class Hazard
        {
            public Transform view;
            public Vector3 home;
            public int kind; // 0 ground robot, 1 drone, 2 chemical puddle, 3 inverse belt
            public float phase;
        }
        int Capacity { get { return 3 + data.capacityLevel; } }
        bool InSanctuary { get { return player.position.z > sanctuaryEntryZ; } }

        [SerializeField] bool dynamicAmbience=true;
        public void ApplyDefaultLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f,.76f,.88f);
            RenderSettings.ambientEquatorColor = new Color(.46f,.58f,.66f);
            RenderSettings.ambientGroundColor = new Color(.25f,.31f,.29f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(.35f,.5f,.58f);
            RenderSettings.fogDensity = .0025f;
        }

        void Start()
        {
            surfaces.Clear();
            materialCache.Clear();
            materials.Clear();
            domeMeshes.Clear();
            Load();
            BuildWorld();
            RefreshAnimalModels();
            ApplyDefaultLighting();
            audioSource = gameObject.AddComponent<AudioSource>();
            pickupSound = Tone(660, .09f);
            deliverySound = Tone(880, .22f);
            hitSound = Tone(140, .18f);
            audioSource.volume=soundVolume;
            shotSound=Tone(420,.06f); blastSound=Tone(70,.3f);
            StartCombat();
            if(!actorsPrepared) { CreateActorMarkers(); actorsPrepared=true; }
            ReadEditableActors();
            Physics.SyncTransforms();
            BrightenExistingMaterials();
        }

        Material MakeMaterial(Color color)
        {
            Material existing;
            if (materialCache.TryGetValue(color, out existing)) return existing;
            Material template = LitTemplate();
            Material material = template != null ? new Material(template) : new Material(Resources.Load<Shader>("EcoStackColor"));
            material.color = color;
            materials.Add(material);
            materialCache[color] = material;
            return material;
        }

        Transform Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent, bool solid = false)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            RefineMesh(obj,name,type);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = SurfaceFor(name, color);
            Collider collider = obj.GetComponent<Collider>();
            collider.enabled = solid;
            return obj.transform;
        }

        TextMesh Label(string value, Vector3 position, Transform parent, float size = .14f)
        {
            GameObject obj = new GameObject("Texto " + value);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(25, 0, 0);
            TextMesh label = obj.AddComponent<TextMesh>();
            label.text = value;
            label.fontSize = 48;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }

        Transform AnimalModel(int kind, Vector3 position, Transform parent)
        {
            Transform root = new GameObject(species[kind]).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            if(kind==2) root.localScale=Vector3.one*1.35f;
            RefreshAnimalVisual(root,kind);
            return root;
        }

        void RefreshAnimalModels()
        {
            Transform[] transforms=GetComponentsInChildren<Transform>(true);
            for(int i=0;i<transforms.Length;i++)
                for(int kind=0;kind<species.Length;kind++)
                    if(transforms[i].name==species[kind])
                    {
                        RefreshAnimalVisual(transforms[i],kind);
                        break;
                    }
        }

        void RefreshAnimalVisual(Transform root,int kind)
        {
            for(int i=root.childCount-1;i>=0;i--)
            {
                GameObject oldPart=root.GetChild(i).gameObject;
                if(Application.isPlaying) Destroy(oldPart);
                else DestroyImmediate(oldPart);
            }
            Color color=colors[kind];
            Color light=Color.Lerp(color,Color.white,.3f);
            Color dark=Color.Lerp(color,new Color(.12f,.16f,.1f),.42f);
            if(kind==0)
            {
                Shape("Cuerpo",PrimitiveType.Sphere,Vector3.zero,new Vector3(.66f,.36f,.78f),color,root);
                Shape("Vientre",PrimitiveType.Sphere,new Vector3(0,-.09f,.12f),new Vector3(.48f,.19f,.56f),light,root);
                Shape("Cabeza",PrimitiveType.Sphere,new Vector3(0,.1f,.5f),new Vector3(.47f,.39f,.42f),color,root);
                for(int side=-1;side<=1;side+=2)
                {
                    Shape("Ojo",PrimitiveType.Sphere,new Vector3(side*.14f,.22f,.83f),new Vector3(.075f,.085f,.045f),new Color(.1f,.07f,.1f),root);
                    Shape("Brillo del ojo",PrimitiveType.Sphere,new Vector3(side*.155f,.25f,.866f),Vector3.one*.025f,Color.white,root);
                    for(int branch=0;branch<3;branch++)
                    {
                        Transform gill=Shape("Branquia plumosa",PrimitiveType.Capsule,new Vector3(side*(.34f+branch*.025f),.24f+branch*.07f,.43f-branch*.07f),new Vector3(.045f,.16f,.045f),new Color(1f,.35f,.49f),root);
                        gill.localRotation=Quaternion.Euler(0,0,side*(48+branch*13));
                    }
                    for(int foot=-1;foot<=1;foot+=2)
                        Shape("Pata",PrimitiveType.Sphere,new Vector3(side*.42f,-.15f,foot*.34f),new Vector3(.13f,.11f,.2f),color,root);
                }
                Transform tail=Shape("Cola",PrimitiveType.Capsule,new Vector3(0,-.02f,-.72f),new Vector3(.14f,.4f,.14f),color,root);
                tail.localRotation=Quaternion.Euler(90,0,0);
                Shape("Aleta caudal",PrimitiveType.Sphere,new Vector3(0,0,-1.01f),new Vector3(.3f,.12f,.22f),light,root);
            }
            else if(kind==1)
            {
                Shape("Cuerpo",PrimitiveType.Sphere,Vector3.zero,new Vector3(.62f,.27f,.72f),color,root);
                Shape("Borde del caparazón",PrimitiveType.Sphere,new Vector3(0,.13f,-.06f),new Vector3(.61f,.14f,.66f),dark,root);
                Shape("Caparazón",PrimitiveType.Sphere,new Vector3(0,.2f,-.06f),new Vector3(.56f,.34f,.6f),Color.Lerp(color,new Color(.18f,.39f,.16f),.35f),root);
                for(int plate=0;plate<5;plate++)
                {
                    float angle=plate*Mathf.PI*2f/5f;
                    Shape("Placa del caparazón",PrimitiveType.Sphere,new Vector3(Mathf.Sin(angle)*.25f,.43f,Mathf.Cos(angle)*.25f-.06f),new Vector3(.19f,.07f,.2f),light,root);
                }
                Shape("Cabeza",PrimitiveType.Sphere,new Vector3(0,.09f,.59f),new Vector3(.3f,.26f,.34f),light,root);
                for(int side=-1;side<=1;side+=2)
                {
                    Shape("Ojo",PrimitiveType.Sphere,new Vector3(side*.1f,.18f,.86f),new Vector3(.045f,.05f,.03f),new Color(.08f,.07f,.04f),root);
                    Transform flipper=Shape("Aleta",PrimitiveType.Capsule,new Vector3(side*.48f,-.06f,.34f),new Vector3(.13f,.12f,.27f),color,root);
                    flipper.localRotation=Quaternion.Euler(side*12f,0,side*18f);
                    Transform rearFlipper=Shape("Aleta trasera",PrimitiveType.Capsule,new Vector3(side*.43f,-.08f,-.39f),new Vector3(.12f,.1f,.22f),color,root);
                    rearFlipper.localRotation=Quaternion.Euler(0,0,side*16f);
                }
            }
            else if(kind==2)
            {
                Shape("Torso",PrimitiveType.Sphere,new Vector3(0,.03f,0),new Vector3(.57f,.55f,.52f),dark,root);
                Shape("Pecho",PrimitiveType.Sphere,new Vector3(0,.04f,.35f),new Vector3(.42f,.39f,.22f),light,root);
                Shape("Cabeza",PrimitiveType.Sphere,new Vector3(0,.49f,.28f),new Vector3(.39f,.38f,.38f),dark,root);
                Shape("Cara",PrimitiveType.Sphere,new Vector3(0,.46f,.58f),new Vector3(.29f,.24f,.16f),new Color(.4f,.42f,.4f),root);
                Shape("Hocico",PrimitiveType.Sphere,new Vector3(0,.34f,.69f),new Vector3(.18f,.13f,.11f),new Color(.27f,.29f,.28f),root);
                Shape("Nariz",PrimitiveType.Sphere,new Vector3(0,.49f,.72f),new Vector3(.09f,.06f,.045f),new Color(.12f,.14f,.13f),root);
                for(int side=-1;side<=1;side+=2)
                {
                    Shape("Oreja",PrimitiveType.Sphere,new Vector3(side*.38f,.49f,.28f),new Vector3(.13f,.16f,.12f),dark,root);
                    Shape("Ojo",PrimitiveType.Sphere,new Vector3(side*.12f,.57f,.7f),new Vector3(.045f,.05f,.025f),new Color(.12f,.08f,.05f),root);
                    Transform arm=Shape("Brazo",PrimitiveType.Capsule,new Vector3(side*.53f,-.02f,.06f),new Vector3(.2f,.38f,.2f),dark,root);
                    arm.localRotation=Quaternion.Euler(0,0,-side*9f);
                    Shape("Mano",PrimitiveType.Sphere,new Vector3(side*.64f,-.31f,.18f),new Vector3(.19f,.15f,.2f),dark,root);
                    Shape("Pierna",PrimitiveType.Capsule,new Vector3(side*.22f,-.43f,.01f),new Vector3(.21f,.28f,.22f),dark,root);
                    Shape("Pie",PrimitiveType.Sphere,new Vector3(side*.22f,-.66f,.12f),new Vector3(.22f,.13f,.3f),dark,root);
                }
            }
            else
            {
                Shape("Cuerpo",PrimitiveType.Sphere,new Vector3(0,.02f,0),new Vector3(.58f,.35f,.76f),color,root);
                Shape("Cabeza",PrimitiveType.Sphere,new Vector3(0,.1f,.59f),new Vector3(.28f,.24f,.34f),light,root);
                Transform snout=Shape("Hocico alargado",PrimitiveType.Capsule,new Vector3(0,.02f,.83f),new Vector3(.085f,.3f,.085f),light,root);
                snout.localRotation=Quaternion.Euler(90,0,0);
                Shape("Nariz",PrimitiveType.Sphere,new Vector3(0,.02f,1.1f),new Vector3(.07f,.06f,.07f),dark,root);
                for(int side=-1;side<=1;side+=2)
                {
                    Shape("Ojo",PrimitiveType.Sphere,new Vector3(side*.17f,.18f,.75f),new Vector3(.04f,.045f,.03f),new Color(.1f,.07f,.04f),root);
                    for(int row=0;row<4;row++)
                    {
                        float z=.36f-row*.23f;
                        float x=side*(.2f-row*.018f);
                        Transform scale=Shape("Escama",PrimitiveType.Sphere,new Vector3(x,.3f-Mathf.Abs(side)*.015f,z),new Vector3(.17f,.085f,.16f),row%2==0?light:color,root);
                        scale.localRotation=Quaternion.Euler(12,0,side*14f);
                        Shape("Pata",PrimitiveType.Capsule,new Vector3(side*.39f,-.14f,z),new Vector3(.13f,.11f,.15f),dark,root);
                    }
                }
                Transform tail=Shape("Cola",PrimitiveType.Capsule,new Vector3(0,-.02f,-.68f),new Vector3(.12f,.4f,.12f),dark,root);
                tail.localRotation=Quaternion.Euler(90,0,0);
            }
        }

        void BuildWorld()
        {
            if (environmentRoot == null) BuildEnvironment();
            if(player!=null)
            {
                if(body==null)
                {
                    body=new GameObject("Modelo rescatista").transform;
                    body.SetParent(player,false);
                }
                if(Application.isPlaying) RefreshCharacterVisual();
                return;
            }
            player = new GameObject("Rescatista").transform;
            player.SetParent(transform);
            player.position = new Vector3(0,.1f,5);
            controller = player.gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .4f;
            controller.center = Vector3.up*.9f;
            body = new GameObject("Modelo rescatista").transform;
            body.SetParent(player, false);
            RefreshCharacterVisual();
            Camera cam = new GameObject("EcoStack Camera").AddComponent<Camera>();
            cam.transform.SetParent(transform);
            cam.tag = "MainCamera";
            cam.fieldOfView = 60;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var cameraData = cam.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            cam.backgroundColor = new Color(.07f,.15f,.18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.gameObject.AddComponent<AudioListener>();
            cameraTransform = cam.transform;
            cameraTransform.position = player.position + new Vector3(0,4,-5.5f);
            cameraTransform.LookAt(player.position+Vector3.up*2+Vector3.forward*2);
            for (int i = 0; i < 40; i++)
            {
                int kind = i % 4;
                Vector3 home = new Vector3((i%3-1)*8, .55f, 9+i*4.1f);
                animals.Add(new Animal {kind=kind, home=home, view=AnimalModel(kind,home,transform)});
            }
            for (int i = 0; i < 24; i++)
            {
                int kind = i%4;
                Vector3 home = new Vector3(i%2==0 ? -5 : 5, kind==1 ? 3.1f : .45f, 18+i*6.5f);
                Transform hazard = BuildHazardModel(kind,home);
                hazards.Add(new Hazard {kind=kind,home=home,view=hazard,phase=i});
            }
        }

        void RefreshCharacterVisual()
        {
            for(int i=body.childCount-1;i>=0;i--)
            {
                GameObject oldPart=body.GetChild(i).gameObject;
                if(Application.isPlaying) Destroy(oldPart);
                else DestroyImmediate(oldPart);
            }
            Color suit = new Color(.23f,.32f,.13f);
            Color pants = new Color(.18f,.24f,.11f);
            Color leather = new Color(.28f,.19f,.12f);
            Color skin = new Color(.68f,.42f,.29f);
            Color hair = new Color(.16f,.075f,.035f);
            Color trim = new Color(.62f,.48f,.2f);
            CreateOrganicTorso(suit);
            Shape("Cadera del uniforme",PrimitiveType.Sphere,new Vector3(0,.58f,0),new Vector3(.48f,.32f,.34f),pants,body);
            Shape("Cuello",PrimitiveType.Capsule,new Vector3(0,1.39f,0),new Vector3(.15f,.14f,.15f),skin,body);
            Shape("Arnés izquierdo",PrimitiveType.Capsule,new Vector3(-.14f,1.08f,.17f),new Vector3(.045f,.25f,.035f),trim,body);
            Shape("Arnés derecho",PrimitiveType.Capsule,new Vector3(.14f,1.08f,.17f),new Vector3(.045f,.25f,.035f),trim,body);
            Shape("Bolsillo de pecho",PrimitiveType.Capsule,new Vector3(-.11f,.98f,.17f),new Vector3(.085f,.08f,.035f),new Color(.29f,.39f,.17f),body);
            Shape("Insignia de hoja",PrimitiveType.Sphere,new Vector3(-.11f,.99f,.2f),new Vector3(.045f,.05f,.02f),new Color(.62f,.76f,.27f),body);
            Shape("Cinturón",PrimitiveType.Capsule,new Vector3(0,.62f,0),new Vector3(.53f,.09f,.38f),leather,body);
            Shape("Hebilla",PrimitiveType.Cylinder,new Vector3(0,.62f,.2f),new Vector3(.09f,.025f,.07f),trim,body);
            Shape("Mochila de campo",PrimitiveType.Capsule,new Vector3(0,.99f,-.25f),new Vector3(.42f,.46f,.23f),new Color(.17f,.23f,.1f),body);
            Transform bedroll=Shape("Manta enrollada",PrimitiveType.Cylinder,new Vector3(0,1.3f,-.39f),new Vector3(.13f,.4f,.13f),new Color(.39f,.29f,.16f),body);
            bedroll.localRotation=Quaternion.Euler(0,0,90);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Pierna",PrimitiveType.Capsule,new Vector3(side*.15f,.38f,0),new Vector3(.18f,.29f,.18f),pants,body);
                Shape("Rodilla",PrimitiveType.Sphere,new Vector3(side*.15f,.25f,.02f),new Vector3(.17f,.16f,.17f),pants,body);
                Shape("Pantorrilla",PrimitiveType.Capsule,new Vector3(side*.16f,.17f,.01f),new Vector3(.145f,.2f,.15f),pants,body);
                Shape("Bota",PrimitiveType.Capsule,new Vector3(side*.16f,.11f,.07f),new Vector3(.23f,.15f,.34f),leather,body);
                Shape("Hombro",PrimitiveType.Sphere,new Vector3(side*.34f,1.24f,0),new Vector3(.27f,.25f,.24f),suit,body);
                Transform upperArm=Shape("Brazo",PrimitiveType.Capsule,new Vector3(side*.4f,1.01f,.01f),new Vector3(.17f,.27f,.17f),suit,body);
                upperArm.localRotation=Quaternion.Euler(0,0,-side*7f);
                Shape("Codo",PrimitiveType.Sphere,new Vector3(side*.43f,.77f,.02f),new Vector3(.155f,.155f,.155f),suit,body);
                Transform forearm=Shape("Antebrazo",PrimitiveType.Capsule,new Vector3(side*.45f,.61f,.035f),new Vector3(.14f,.22f,.14f),new Color(.27f,.36f,.16f),body);
                forearm.localRotation=Quaternion.Euler(0,0,-side*5f);
                Shape("Guante",PrimitiveType.Sphere,new Vector3(side*.46f,.4f,.05f),new Vector3(.14f,.15f,.14f),leather,body);
            }
            Shape("Cabeza",PrimitiveType.Sphere,new Vector3(0,1.66f,.015f),new Vector3(.4f,.47f,.38f),skin,body);
            Shape("Cabello",PrimitiveType.Sphere,new Vector3(0,1.76f,-.1f),new Vector3(.44f,.31f,.38f),hair,body);
            Shape("Cola de cabello",PrimitiveType.Capsule,new Vector3(0,1.49f,-.33f),new Vector3(.15f,.26f,.15f),hair,body);
            Shape("Gorra",PrimitiveType.Sphere,new Vector3(0,1.91f,0),new Vector3(.5f,.22f,.5f),new Color(.17f,.27f,.1f),body);
            Shape("Visera de gorra",PrimitiveType.Capsule,new Vector3(0,1.84f,.19f),new Vector3(.42f,.055f,.26f),new Color(.17f,.27f,.1f),body);
            Shape("Parche de gorra",PrimitiveType.Sphere,new Vector3(0,1.93f,.25f),new Vector3(.1f,.08f,.035f),new Color(.62f,.75f,.25f),body);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Oreja",PrimitiveType.Sphere,new Vector3(side*.2f,1.64f,.01f),new Vector3(.075f,.11f,.07f),skin,body);
                Shape("Ojo",PrimitiveType.Sphere,new Vector3(side*.095f,1.68f,.17f),new Vector3(.045f,.055f,.026f),new Color(.9f,.83f,.7f),body);
                Shape("Pupila",PrimitiveType.Sphere,new Vector3(side*.095f,1.68f,.193f),new Vector3(.022f,.032f,.015f),new Color(.09f,.055f,.03f),body);
                Transform brow=Shape("Ceja",PrimitiveType.Capsule,new Vector3(side*.095f,1.74f,.17f),new Vector3(.075f,.018f,.018f),hair,body);
                brow.localRotation=Quaternion.Euler(0,0,side*8f);
            }
            Shape("Nariz",PrimitiveType.Sphere,new Vector3(0,1.59f,.19f),new Vector3(.045f,.06f,.045f),new Color(.62f,.36f,.25f),body);
            Shape("Boca",PrimitiveType.Capsule,new Vector3(0,1.52f,.185f),new Vector3(.055f,.015f,.015f),new Color(.29f,.1f,.07f),body);
        }

        void CreateOrganicTorso(Color color)
        {
            Vector3[] profile =
            {
                new Vector3(.19f,.46f,.14f),
                new Vector3(.24f,.54f,.16f),
                new Vector3(.25f,.72f,.16f),
                new Vector3(.27f,.96f,.17f),
                new Vector3(.31f,1.16f,.18f),
                new Vector3(.34f,1.28f,.18f),
                new Vector3(.29f,1.36f,.15f),
                new Vector3(.17f,1.41f,.12f)
            };
            const int segments=20;
            int ringVertices=profile.Length*segments;
            Vector3[] vertices=new Vector3[ringVertices+2];
            int[] triangles=new int[(profile.Length-1)*segments*6+segments*6];
            for(int ring=0;ring<profile.Length;ring++)
                for(int segment=0;segment<segments;segment++)
                {
                    float angle=2f*Mathf.PI*segment/segments;
                    vertices[ring*segments+segment]=new Vector3(
                        profile[ring].x*Mathf.Cos(angle),
                        profile[ring].y,
                        profile[ring].z*Mathf.Sin(angle));
                }
            int triangle=0;
            for(int ring=0;ring<profile.Length-1;ring++)
                for(int segment=0;segment<segments;segment++)
                {
                    int next=(segment+1)%segments;
                    int lower=ring*segments+segment;
                    int lowerNext=ring*segments+next;
                    int upper=(ring+1)*segments+segment;
                    int upperNext=(ring+1)*segments+next;
                    triangles[triangle++]=lower;
                    triangles[triangle++]=upper;
                    triangles[triangle++]=lowerNext;
                    triangles[triangle++]=lowerNext;
                    triangles[triangle++]=upper;
                    triangles[triangle++]=upperNext;
                }
            int bottomCenter=ringVertices;
            int topCenter=ringVertices+1;
            vertices[bottomCenter]=new Vector3(0,profile[0].y,0);
            vertices[topCenter]=new Vector3(0,profile[profile.Length-1].y,0);
            int topRing=(profile.Length-1)*segments;
            for(int segment=0;segment<segments;segment++)
            {
                int next=(segment+1)%segments;
                triangles[triangle++]=bottomCenter;
                triangles[triangle++]=segment;
                triangles[triangle++]=next;
                triangles[triangle++]=topCenter;
                triangles[triangle++]=topRing+next;
                triangles[triangle++]=topRing+segment;
            }
            Mesh mesh=new Mesh();
            mesh.name="Malla orgánica del torso";
            mesh.vertices=vertices;
            mesh.triangles=triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject torso=new GameObject("Torso anatómico");
            torso.transform.SetParent(body,false);
            torso.AddComponent<MeshFilter>().sharedMesh=mesh;
            torso.AddComponent<MeshRenderer>().sharedMaterial=MakeMaterial(color);
        }

        Vector2 Movement(out bool jump, out bool pause)
        {
            Vector2 direction = Vector2.zero;
            jump = false; pause = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keys = Keyboard.current;
            if (keys == null) return direction;
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) direction.y++;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) direction.y--;
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) direction.x++;
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) direction.x--;
            jump = keys.spaceKey.wasPressedThisFrame;
            pause = keys.escapeKey.wasPressedThisFrame;
#else
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) direction.y++;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) direction.y--;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direction.x++;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) direction.x--;
            jump = Input.GetKeyDown(KeyCode.Space);
            pause = Input.GetKeyDown(KeyCode.Escape);
#endif
            return Vector2.ClampMagnitude(direction,1);
        }

        void Update()
        {
            bool jump, pause;
            Vector2 input = Movement(out jump,out pause);
            if (pause && !briefing && !defeated && !victoryScreen) { paused = !paused; confirmReset = false; }
            if (SimulationStopped) return;
            float delta = Time.deltaTime;
            gameTime += delta;
            float speed = 6 + data.speedLevel*.8f;
            Vector3 push = Vector3.zero;
            foreach (Hazard hazard in hazards)
            {
                if (hazard.kind < 2) continue; // Robots and drones now use combat AI.
                Vector3 relative = hazard.view.InverseTransformPoint(player.position);
                if (Mathf.Abs(relative.x)<2.8f && Mathf.Abs(relative.z)<1.8f && relative.y<.35f)
                {
                    if (hazard.kind==2) { speed *= .45f; TakeDamage(10,"Residuos químicos"); }
                    else push = -hazard.view.forward*3.5f;
                }
            }
            if (defeated) return;
            CombatInput();
            if (gameTime < dashUntil) speed *= 2.2f;
            if (controller.isGrounded && verticalSpeed<0) verticalSpeed = -2;
            if (jump && controller.isGrounded) verticalSpeed = 8;
            verticalSpeed -= 22*delta;
            Vector3 motion = new Vector3(input.x,0,input.y)*speed + push;
            controller.Move((motion+Vector3.up*verticalSpeed)*delta);
            if (input.sqrMagnitude>.01f)
                body.rotation = Quaternion.Slerp(body.rotation,Quaternion.LookRotation(new Vector3(input.x,0,input.y)),12*delta);
            if (player.position.y < -8)
            {
                controller.enabled=false;
                player.position=playerStart+Vector3.up;
                controller.enabled=true;
                verticalSpeed=0;
                Hit();
            }
            if (defeated) return;
            foreach (Animal animal in animals)
            {
                if (animal.respawnAt>0 && gameTime>=animal.respawnAt)
                {
                    animal.respawnAt=0;
                    animal.view.position=animal.home;
                    animal.view.gameObject.SetActive(true);
                }
                if (animal.carried || animal.respawnAt>0 || gameTime<pickupAfter) continue;
                if (Vector3.Distance(player.position+Vector3.up*.5f,animal.view.position)<1.5f && stack.Count<Capacity)
                {
                    animal.carried=true;
                    stack.Add(animal);
                    audioSource.PlayOneShot(pickupSound,.2f);
                }
            }
            if (InSanctuary && stack.Count>0) Deliver();
            TickCombat(delta);
            if (defeated || victoryScreen) return;
            float height=2.2f;
            for (int i=0;i<stack.Count;i++)
            {
                Animal animal=stack[i];
                float sway=Mathf.Sin(gameTime*4+i*.55f)*.025f*(i+1)*input.magnitude;
                animal.view.position=player.position+new Vector3(sway,height,0);
                animal.view.rotation=Quaternion.Euler(0,body.eulerAngles.y,sway*25);
                height+=animal.kind==2 ? 1.15f : .65f;
            }
            bool collected=false;
            for (int k=0;k<4;k++)
            {
                data.pending[k]+=data.rescued[k]*rates[k]*delta/60f;
                Vector3 pad=collectionPads[k]!=null?collectionPads[k].position:pads[k]+new Vector3(0,0,-2);
                if (Vector3.Distance(player.position,pad)<1.6f)
                {
                    int amount=Mathf.FloorToInt(data.pending[k]);
                    data.coins+=amount;
                    data.pending[k]-=amount;
                    collected |= amount>0;
                }
                if (padLabels[k] != null) padLabels[k].text=species[k]+" x"+data.rescued[k]+"\n"+Mathf.FloorToInt(data.pending[k])+" Eco-Coins";
            }
            saveTimer+=delta;
            if (collected || saveTimer>5) Save();
        }

        void LateUpdate()
        {
            if (cameraTransform==null || SimulationStopped) return;
            float rise=stack.Count*.3f;
            cameraTransform.position=Vector3.Lerp(cameraTransform.position,player.position+cameraOffset+new Vector3(0,rise,-rise),8*Time.deltaTime);
            cameraTransform.LookAt(player.position+cameraAimOffset);
            if(!dynamicAmbience) return;
            Camera camera = cameraTransform.GetComponent<Camera>();
            RenderSettings.fogColor = camera.backgroundColor;
            camera.backgroundColor = Color.Lerp(camera.backgroundColor,
                InSanctuary ? new Color(.62f,.84f,.9f) : new Color(.2f,.31f,.4f), Time.deltaTime*2);

        }

        void BrightenExistingMaterials()
        {
            Renderer[] renderers=GetComponentsInChildren<Renderer>(true);
            MaterialPropertyBlock block=new MaterialPropertyBlock();
            Color lift=new Color(.9f,.93f,.98f,1);
            foreach(Renderer renderer in renderers)
            {
                Material[] sharedMaterials=renderer.sharedMaterials;
                for(int index=0;index<sharedMaterials.Length;index++)
                {
                    Material material=sharedMaterials[index];
                    if(material==null) continue;
                    renderer.GetPropertyBlock(block,index);
                    bool changed=false;
                    if(material.HasProperty("_BaseColor"))
                    {
                        block.SetColor("_BaseColor",Color.Lerp(material.GetColor("_BaseColor"),lift,.2f));
                        changed=true;
                    }
                    if(material.HasProperty("_Color"))
                    {
                        block.SetColor("_Color",Color.Lerp(material.GetColor("_Color"),lift,.2f));
                        changed=true;
                    }
                    if(changed) renderer.SetPropertyBlock(block,index);
                    block.Clear();
                }
            }
        }

        void Hit() { TakeDamage(25,"Caída"); }

        void ScatterAnimals()
        {
                        pickupAfter=gameTime+2;
            for (int i=0;i<stack.Count;i++)
            {
                Animal animal=stack[i];
                animal.carried=false;
                animal.view.rotation=Quaternion.identity;
                Vector3 landing=player.position+new Vector3(Mathf.Cos(i*2.4f)*2.4f,.55f,Mathf.Sin(i*2.4f)*2.4f);
                landing.x=Mathf.Clamp(landing.x,-15,15);
                landing.z=Mathf.Clamp(landing.z,2,177);
                landing.y=.55f;
                animal.view.position=landing;
            }
            stack.Clear();

        }

        void Deliver()
        {
            int count=stack.Count;
            foreach (Animal animal in stack)
            {
                data.rescued[animal.kind]++;
                animal.carried=false;
                animal.respawnAt=gameTime+12;
                animal.view.rotation=Quaternion.identity;
                animal.view.gameObject.SetActive(false);
                // Each habitat uses a representative model and a persistent population counter.
            }
            stack.Clear();
            sessionRescues += count;
            data.coins += count*8;
            audioSource.PlayOneShot(deliverySound,.25f);
            Notify("Rescate completado: "+count+" animales. +"+(count*8)+" monedas. Cobra también en los recintos.");
            Save();
        }

        void Notify(string value) { message=value; messageUntil=gameTime+5; }

        void Buy(int type)
        {
            if (!InSanctuary || paused) return;
            int level=type==0 ? data.capacityLevel : data.speedLevel;
            if (type<2 && level>=5) return;
            if (type==2 && data.shields>=3) return;
            int price=type==2 ? 30 : (level+1)*40;
            if (data.coins<price) { Notify("Faltan Eco-Coins. Recógelas en los recintos."); return; }
            data.coins-=price;
            if (type==0) data.capacityLevel++;
            else if (type==1) data.speedLevel++;
            else data.shields++;
            Notify("Mejora comprada.");
            Save();
        }

        void OnGUI() { CombatGUI(); }
        void ResetProgress()
        {
            data = new SaveData();
            stack.Clear();
            foreach (Animal animal in animals)
            {
                animal.carried = false;
                animal.respawnAt = 0;
                animal.view.position = animal.home;
                animal.view.rotation = Quaternion.identity;
                animal.view.gameObject.SetActive(true);
            }
            controller.enabled = false;
            player.position = playerStart;
            controller.enabled = true;
            verticalSpeed = 0;
            damageUntil = 0;
            pickupAfter = 0;
            confirmReset = false;
            paused = false;
            ResetCombat(true);
            Save();
            Notify("Partida nueva. Recoge animales y llévalos al santuario.");
        }

        int TotalRescued() { int total=0; foreach (int count in data.rescued) total+=count; return total; }
        void Load()
        {
            try { data=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey,"")); }
            catch (Exception) { data=null; }
            if (data==null) data=new SaveData();
            if (data.rescued==null || data.rescued.Length!=4) data.rescued=new int[4];
            if (data.pending==null || data.pending.Length!=4) data.pending=new float[4];
            data.coins=Mathf.Clamp(data.coins,0,100000000);
            data.capacityLevel=Mathf.Clamp(data.capacityLevel,0,5);
            data.speedLevel=Mathf.Clamp(data.speedLevel,0,5);
            data.shields=Mathf.Clamp(data.shields,0,3);
            data.weaponLevel=Mathf.Clamp(data.weaponLevel,0,3);
            for (int k=0;k<4;k++)
            {
                data.rescued[k]=Mathf.Clamp(data.rescued[k],0,100000);
                if (float.IsNaN(data.pending[k]) || float.IsInfinity(data.pending[k])) data.pending[k]=0;
                data.pending[k]=Mathf.Clamp(data.pending[k],0,1000000);
            }
        }
        void Save()
        {
            if (data==null) return;
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));
            PlayerPrefs.Save(); saveTimer=0;
        }
        AudioClip Tone(float frequency,float duration)
        {
            const int sampleRate=22050;
            float[] samples=new float[(int)(sampleRate*duration)];
            for (int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(2*Mathf.PI*frequency*i/sampleRate)*(1f-(float)i/samples.Length)*.4f;
            AudioClip clip=AudioClip.Create("EcoStack efecto",samples.Length,1,sampleRate,false);
            clip.SetData(samples,0);
            return clip;
        }
        void OnApplicationPause(bool value) { if (value) Save(); }
        void OnApplicationQuit() { Save(); }
        void OnDestroy()
        {
            Save();
            if (!Application.isPlaying) return;
            foreach (Mesh mesh in domeMeshes) if (mesh!=null) Destroy(mesh);
            foreach (Material material in materials) if (material!=null) Destroy(material);
            if (pickupSound!=null) Destroy(pickupSound);
            if (deliverySound!=null) Destroy(deliverySound);
            if (hitSound!=null) Destroy(hitSound);
            if (shotSound!=null) Destroy(shotSound);
            if (blastSound!=null) Destroy(blastSound);
        }
    }
}
