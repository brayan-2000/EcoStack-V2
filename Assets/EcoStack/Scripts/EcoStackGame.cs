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
        readonly Color[] colors = { new Color(1,.4f,.65f), new Color(.3f,.7f,.3f), new Color(.3f,.33f,.4f), new Color(.65f,.4f,.2f) };
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
            RenderSettings.ambientSkyColor = new Color(.34f,.4f,.48f);
            RenderSettings.ambientEquatorColor = new Color(.18f,.23f,.28f);
            RenderSettings.ambientGroundColor = new Color(.1f,.12f,.13f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = .004f;
        }

        void Start()
        {
            surfaces.Clear();
            materialCache.Clear();
            materials.Clear();
            domeMeshes.Clear();
            Load();
            BuildWorld();
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
            Color color = colors[kind];
            Shape("Cuerpo", PrimitiveType.Sphere, Vector3.zero, new Vector3(.85f,.5f,1), color, root);
            Shape("Cabeza", PrimitiveType.Sphere, new Vector3(0,.12f,.55f), Vector3.one * .55f, color, root);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Ojo", PrimitiveType.Sphere, new Vector3(side*.17f,.23f,.77f), Vector3.one*.09f, Color.black, root);
                Shape("Pata", PrimitiveType.Cube, new Vector3(side*.45f,-.15f,.1f), new Vector3(.35f,.15f,.5f), color, root);
                if (kind == 0)
                {
                    for (int j = 0; j < 3; j++)
                        Shape("Branquia", PrimitiveType.Cube, new Vector3(side*.42f,.15f+j*.13f,.48f-j*.1f), new Vector3(.38f,.07f,.07f), new Color(1,.2f,.45f), root);
                }
            }
            if (kind == 1 || kind == 3)
                Shape("Caparazón", PrimitiveType.Sphere, new Vector3(0,.2f,-.1f), new Vector3(.9f,.5f,.9f), color * .65f, root);
            if (kind == 0)
            {
                Transform tail=Shape("Cola",PrimitiveType.Capsule,new Vector3(0,0,-.72f),new Vector3(.22f,.48f,.17f),color,root);
                tail.localRotation=Quaternion.Euler(70,0,0);
            }
            if (kind == 1)
            {
                for(int i=0;i<5;i++) Shape("Placa caparazón",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*1.25f)*.23f,.39f,Mathf.Cos(i*1.25f)*.23f-.1f),new Vector3(.3f,.1f,.3f),new Color(.35f,.4f,.15f),root);
            }
            if (kind == 2)
            {
                root.localScale = Vector3.one * 1.35f;
                Shape("Pecho",PrimitiveType.Sphere,new Vector3(0,.23f,.05f),new Vector3(.8f,.85f,.65f),color,root);
                for(int side=-1;side<=1;side+=2)
                {
                    Shape("Brazo gorila",PrimitiveType.Capsule,new Vector3(side*.5f,.02f,.2f),new Vector3(.3f,.42f,.35f),color,root);
                    Shape("Oreja",PrimitiveType.Sphere,new Vector3(side*.29f,.2f,.55f),Vector3.one*.17f,color,root);
                }
                Shape("Hocico",PrimitiveType.Sphere,new Vector3(0,.07f,.8f),new Vector3(.32f,.2f,.18f),color*.6f,root);
            }
            if (kind == 3)
            {
                for(int row=0;row<4;row++)
                    for(int side=-1;side<=1;side++)
                    {
                        Transform plate=Shape("Escama",PrimitiveType.Cube,new Vector3(side*.22f,.38f-Mathf.Abs(side)*.08f,.25f-row*.22f),new Vector3(.3f,.12f,.3f),color*(.8f+row*.06f),root);
                        plate.localRotation=Quaternion.Euler(15,0,side*25);
                    }
            }
            return root;
        }

        void BuildWorld()
        {
            if (environmentRoot == null) BuildEnvironment();
            if(player!=null) return;
            player = new GameObject("Rescatista").transform;
            player.SetParent(transform);
            player.position = new Vector3(0,.1f,5);
            controller = player.gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .4f;
            controller.center = Vector3.up*.9f;
            body = new GameObject("Modelo rescatista").transform;
            body.SetParent(player, false);
            Shape("Traje", PrimitiveType.Capsule, new Vector3(0,.85f,0), new Vector3(.7f,.6f,.6f), new Color(.2f,.6f,.3f), body);
            Shape("Cabeza", PrimitiveType.Sphere, new Vector3(0,1.65f,0), Vector3.one*.55f, new Color(.85f,.62f,.4f), body);
            Shape("Mochila", PrimitiveType.Cube, new Vector3(0,1,-.35f), new Vector3(.6f,.7f,.3f), new Color(.3f,.35f,.15f), body);
            for (int arm=-1;arm<=1;arm+=2)
            {
                Shape("Manga",PrimitiveType.Capsule,new Vector3(arm*.48f,1,0),new Vector3(.24f,.32f,.24f),new Color(.2f,.5f,.25f),body);
                Shape("Guante",PrimitiveType.Sphere,new Vector3(arm*.48f,.65f,0),Vector3.one*.25f,new Color(.2f,.17f,.13f),body);
                Shape("Ojo",PrimitiveType.Sphere,new Vector3(arm*.12f,1.7f,.25f),Vector3.one*.07f,Color.black,body);
            }
            Shape("Visera",PrimitiveType.Cube,new Vector3(0,1.9f,.3f),new Vector3(.55f,.08f,.35f),new Color(.2f,.5f,.1f),body);
            Shape("Gorra", PrimitiveType.Cylinder, new Vector3(0,1.9f,0), new Vector3(.65f,.07f,.65f), new Color(.2f,.5f,.1f), body);
            for (int s = -1; s <= 1; s += 2)
                Shape("Bota", PrimitiveType.Cube, new Vector3(s*.22f,.15f,0), new Vector3(.25f,.3f,.45f), new Color(.2f,.15f,.1f), body);
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
                InSanctuary ? new Color(.48f,.77f,.86f) : new Color(.07f,.10f,.13f), Time.deltaTime*2);

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
