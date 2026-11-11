using UnityEngine;
namespace EcoStack
{
    public partial class EcoStackGame
    {
        [SerializeField,HideInInspector] bool actorsPrepared;
        [SerializeField,HideInInspector] Transform actorRoot;
        Vector3 playerStart, cameraOffset, cameraAimOffset, gunBaseScale;
        public bool ActorsPrepared { get { return actorsPrepared; } }
        public Transform ActorRoot { get { return actorRoot; } }
        public void PrepareEditableActors()
        {
            if(Application.isPlaying || actorsPrepared) return;
            BuildWorld();
            StartCombat();
            CreateActorMarkers();
            // Associate existing V5 collection pads without rebuilding the edited environment.
            foreach(Transform child in environmentRoot.GetComponentsInChildren<Transform>(true))
                if(child.name=="Círculo de cobro")
                    for(int k=0;k<4;k++)
                        if(collectionPads[k]==null && Vector3.Distance(child.position,pads[k]+Vector3.back*2)<1) collectionPads[k]=child;
            actorsPrepared=true;
        }
        Transform ActorGroup(string label)
        {
            Transform group=new GameObject(label).transform;
            group.SetParent(actorRoot,false);
            return group;
        }
        EcoStackActor Mark(Transform view,EcoStackActor.Role role,Transform parent)
        {
            view.SetParent(parent,true);
            EcoStackActor marker=view.GetComponent<EcoStackActor>();
            if(marker==null) marker=view.gameObject.AddComponent<EcoStackActor>();
            marker.role=role;
            return marker;
        }
        void CreateActorMarkers()
        {
            actorRoot=new GameObject("Actores editables").transform;
            actorRoot.SetParent(transform,false);
            Transform fauna=ActorGroup("Animales"), foes=ActorGroup("Enemigos"), traps=ActorGroup("Peligros"), kits=ActorGroup("Botiquines");
            foreach(Animal a in animals) Mark(a.view,EcoStackActor.Role.Animal,fauna).species=a.kind;
            foreach(Enemy e in enemies)
            {
                var role=e.kind==0?EcoStackActor.Role.Robot:e.kind==1?EcoStackActor.Role.Drone:e.kind==2?EcoStackActor.Role.Turret:EcoStackActor.Role.Boss;
                EcoStackActor marker=Mark(e.view,role,foes);
                marker.maximumHealth=e.maximum; marker.warningLight=e.warningLight;
                marker.hitRadius=e.kind==3?2.1f:.85f;
                e.view.name=e.kind==0?"Robot perseguidor":e.kind==1?"Dron artillero":e.kind==2?"Torreta":"Jefe - Núcleo";
            }
            foreach(Hazard h in hazards)
                if(h.kind>=2) Mark(h.view,h.kind==2?EcoStackActor.Role.Chemical:EcoStackActor.Role.Conveyor,traps);
            foreach(Transform kit in medicalPickups) Mark(kit,EcoStackActor.Role.Medkit,kits);
        }
        void ReadEditableActors()
        {
            animals.Clear(); stack.Clear(); hazards.Clear(); enemies.Clear(); medicalPickups.Clear();
            foreach(EcoStackActor marker in GetComponentsInChildren<EcoStackActor>(false))
            {
                if(!marker.enabled) continue;
                Transform view=marker.transform;
                if(marker.role==EcoStackActor.Role.Animal)
                    animals.Add(new Animal {view=view,home=view.position,kind=Mathf.Clamp(marker.species,0,3)});
                else if(marker.role==EcoStackActor.Role.Medkit) medicalPickups.Add(view);
                else if(marker.role==EcoStackActor.Role.Chemical || marker.role==EcoStackActor.Role.Conveyor)
                    hazards.Add(new Hazard {view=view,home=view.position,kind=marker.role==EcoStackActor.Role.Chemical?2:3});
                else
                {
                    int kind=marker.role==EcoStackActor.Role.Robot?0:marker.role==EcoStackActor.Role.Drone?1:marker.role==EcoStackActor.Role.Turret?2:3;
                    Transform lamp=marker.warningLight;
                    if(lamp==null) lamp=Shape("Luz de ataque",PrimitiveType.Sphere,Vector3.up*.8f,Vector3.one*.28f,Color.red,view);
                    lamp.gameObject.SetActive(false);
                    int hp=Mathf.Max(1,marker.maximumHealth);
                    enemies.Add(new Enemy {view=view,home=view.position,kind=kind,health=hp,maximum=hp,attackAt=1,warningLight=lamp,radius=Mathf.Max(.1f,marker.hitRadius)});
                }
            }
            playerStart=player.position;
            cameraOffset=cameraTransform.position-player.position;
            float lookDistance=Vector3.Distance(cameraTransform.position,player.position+Vector3.up*2);
            cameraAimOffset=cameraOffset+cameraTransform.forward*lookDistance;
            gunBaseScale=gun.localScale;
            aimPoint=player.position+Vector3.forward*20+Vector3.up;
        }
    }
}
