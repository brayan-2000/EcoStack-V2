using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoStack
{
    public partial class EcoStackGame
    {
        class Enemy
        {
            public Transform view;
            public Vector3 home, target;
            public int kind, health, maximum;
            public float radius=.85f;
            public float attackAt, fireAt, stunUntil;
            public bool warning, dead;
            public Transform warningLight;
        }
        class Bolt
        {
            public Transform view;
            public Vector3 velocity;
            public int damage;
            public bool hostile;
            public float expires;
        }
        class Flash
        {
            public Transform view;
            public float expires;
        }
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Bolt> bolts = new List<Bolt>();
        readonly List<Flash> flashes = new List<Flash>();
        readonly List<Transform> medicalPickups = new List<Transform>();
        float health=100, energy=100, nextShot, reloadAt, nextDash, dashUntil;
        [SerializeField, Range(0,2)] int difficulty=1;
        bool briefing=true;
        float soundVolume=.65f;
        int weapon, sessionRescues, destroyed;
        int[] ammunition = {12,24};
        bool defeated, victoryScreen, bossDefeated, checkpoint, missionAwarded;
        Vector3 aimPoint;
        [SerializeField] Transform gun, muzzle;
        AudioClip shotSound, blastSound;
        string defeatCause="";
        public bool SimulationStopped { get { return paused || defeated || victoryScreen || briefing; } }
        int Magazine { get { return weapon==0?12:24; } }
        string WeaponName { get { return weapon==0?"Pistola de pulsos":"Rifle de energía"; } }

        void StartCombat()
        {
            if(actorsPrepared) return;
            gun=new GameObject("Arma de energía").transform;
            gun.SetParent(player,false);
            gun.localPosition=new Vector3(.48f,1.18f,.22f);
            Shape("Carcasa arma",PrimitiveType.Cube,Vector3.zero,new Vector3(.27f,.3f,.7f),steel,gun);
            Shape("Empuñadura",PrimitiveType.Cube,new Vector3(0,-.2f,-.15f),new Vector3(.17f,.28f,.22f),Color.black,gun);
            Shape("Luz energía",PrimitiveType.Cube,new Vector3(0,.16f,0),new Vector3(.17f,.03f,.4f),Color.cyan,gun);
            muzzle=new GameObject("Boca del arma").transform;
            muzzle.SetParent(gun,false); muzzle.localPosition=Vector3.forward*.48f;

            foreach(Hazard h in hazards)
                if(h.kind<2) AddEnemy(h.view,h.home,h.kind);
            for(int i=0;i<4;i++)
            {
                Vector3 home=new Vector3(i%2==0?-9:9,.9f,42+i*30);
                Transform t=BuildHazardModel(0,home);
                t.name="Torreta centinela";
                Shape("Torreta cañón",PrimitiveType.Cube,new Vector3(0,.6f,-.6f),new Vector3(.35f,.35f,1.5f),steel,t);
                AddEnemy(t,home,2);
            }
            Vector3 bossHome=new Vector3(0,1.5f,164);
            Transform boss=BuildHazardModel(0,bossHome);
            boss.localScale=Vector3.one*3;
            boss.name="Jefe: Núcleo de seguridad";
            Shape("Luz núcleo",PrimitiveType.Sphere,new Vector3(0,.5f,0),Vector3.one*.6f,Color.red,boss);
            AddEnemy(boss,bossHome,3);
            for(int i=0;i<4;i++)
            {
                Transform kit=Shape("Botiquín",PrimitiveType.Cube,new Vector3(i%2==0?-8:8,.6f,32+i*36),new Vector3(.65f,.5f,.65f),Color.white,transform);
                Shape("Cruz médica",PrimitiveType.Cube,new Vector3(0,.27f,0),new Vector3(.4f,.04f,.12f),Color.red,kit);
                Shape("Cruz médica",PrimitiveType.Cube,new Vector3(0,.27f,0),new Vector3(.12f,.04f,.4f),Color.red,kit);
                medicalPickups.Add(kit);
            }
            aimPoint=player.position+Vector3.forward*20+Vector3.up;
            Notify("Misión: rescata 8 animales y destruye el Núcleo de seguridad. Clic: disparar.");
        }
        void AddEnemy(Transform view,Vector3 home,int kind)
        {
            int hp=kind==3?700:kind==2?130:kind==1?65:95;
            Transform indicator=Shape("Luz de ataque",PrimitiveType.Sphere,new Vector3(0,.8f,0),Vector3.one*.28f,Color.red,view);
            indicator.gameObject.SetActive(false);
            enemies.Add(new Enemy {view=view,home=home,kind=kind,health=hp,maximum=hp,attackAt=gameTime+1,warningLight=indicator});
        }

        void CombatInput()
        {
            bool fire=false, emp=false, reload=false, dash=false, one=false, two=false;
            Vector2 pointer=new Vector2(Screen.width*.5f,Screen.height*.5f);
#if ENABLE_INPUT_SYSTEM
            if(Mouse.current!=null)
            {
                pointer=Mouse.current.position.ReadValue();
                fire=Mouse.current.leftButton.isPressed;
                emp=Mouse.current.rightButton.wasPressedThisFrame;
            }
            if(Keyboard.current!=null)
            {
                reload=Keyboard.current.rKey.wasPressedThisFrame;
                dash=Keyboard.current.leftShiftKey.wasPressedThisFrame;
                one=Keyboard.current.digit1Key.wasPressedThisFrame;
                two=Keyboard.current.digit2Key.wasPressedThisFrame;
                emp |= Keyboard.current.qKey.wasPressedThisFrame;
            }
#else
            pointer=Input.mousePosition; fire=Input.GetMouseButton(0); emp=Input.GetMouseButtonDown(1)||Input.GetKeyDown(KeyCode.Q);
            reload=Input.GetKeyDown(KeyCode.R); dash=Input.GetKeyDown(KeyCode.LeftShift);
            one=Input.GetKeyDown(KeyCode.Alpha1); two=Input.GetKeyDown(KeyCode.Alpha2);
#endif
            if(one||two) { int choice=two?1:0; if(choice!=weapon) { weapon=choice; reloadAt=0; } }
            Ray ray=cameraTransform.GetComponent<Camera>().ScreenPointToRay(pointer);
            float distance;
            Plane plane=new Plane(Vector3.up,player.position+Vector3.up*1.2f);
            aimPoint=plane.Raycast(ray,out distance)?ray.GetPoint(Mathf.Min(distance,90)):player.position+body.forward*20+Vector3.up;
            // Aim assist only near the cursor; height is taken from the selected drone/robot.
            float best=60f;
            foreach(Enemy e in enemies)
            {
                if(e.dead) continue;
                Vector3 relative=e.view.position-ray.origin;
                float along=Vector3.Dot(relative,ray.direction);
                float error=Vector3.Distance(e.view.position,ray.GetPoint(along));
                if(along>0 && along<best && error<e.radius+.25f) { best=along; aimPoint=e.view.position; }
            }
            gun.localScale=Vector3.Scale(gunBaseScale,new Vector3(1,1,weapon==0?1:1.55f));
            Vector3 gunDirection=aimPoint-gun.position;
            if(gunDirection.sqrMagnitude>.001f) gun.rotation=Quaternion.LookRotation(gunDirection);
            if(reloadAt>0 && gameTime>=reloadAt) { ammunition[weapon]=Magazine; reloadAt=0; }
            if(reload && ammunition[weapon]<Magazine && reloadAt==0) reloadAt=gameTime+1.4f;
            if(dash && gameTime>=nextDash) { dashUntil=gameTime+.28f; nextDash=gameTime+3; }
            if(InSanctuary) return;
            if(emp && energy>=40)
            {
                energy-=40;
                foreach(Enemy e in enemies)
                    if(!e.dead && Vector3.Distance(e.view.position,player.position)<10 && VisibleFrom(player.position+Vector3.up,e.view.position))
                    { e.stunUntil=gameTime+3; e.warning=false; e.warningLight.gameObject.SetActive(false); DamageEnemy(e,55); }
                Pulse(player.position+Vector3.up,Color.cyan,4);
                audioSource.PlayOneShot(blastSound,.25f);
            }
            if(fire && gameTime>=nextShot && reloadAt==0)
            {
                if(ammunition[weapon]==0) { reloadAt=gameTime+1.4f; return; }
                ammunition[weapon]--;
                nextShot=gameTime+(weapon==0?.32f:.11f);
                int damage=(weapon==0?32:18)+data.weaponLevel*6;
                Launch(muzzle.position,(aimPoint-muzzle.position).normalized*42,damage,false);
                Pulse(muzzle.position,Color.cyan,.24f);
                audioSource.PlayOneShot(shotSound,.12f);
            }
        }

        bool VisibleFrom(Vector3 origin,Vector3 target)
        {
            Vector3 d=target-origin;
            foreach(RaycastHit hit in Physics.RaycastAll(origin,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(player) && hit.transform!=player) return false;
            return true;
        }
        void MoveEnemy(Enemy e,Vector3 destination,float speed,float dt)
        {
            Vector3 offset=destination-e.view.position; offset.y=0;
            float step=Mathf.Min(speed*dt,offset.magnitude);
            if(step<.001f) return;
            Vector3 direction=offset.normalized;
            float radius=e.kind==3?1.5f:.55f;
            // Keep solid cover between the pursuer and player; try side-stepping when blocked.
            if(!Physics.SphereCast(e.view.position,radius,direction,out RaycastHit hit,step+.15f,~0,QueryTriggerInteraction.Ignore))
                e.view.position+=direction*step;
            else
            {
                Vector3 side=Vector3.Cross(Vector3.up,direction);
                if(!Physics.SphereCast(e.view.position,radius,side,out hit,step+.15f,~0,QueryTriggerInteraction.Ignore)) e.view.position+=side*step;
            }
            Vector3 p=e.view.position; p.x=Mathf.Clamp(p.x,-11,11); p.z=Mathf.Clamp(p.z,12,177); e.view.position=p;
        }
        void TickCombat(float dt)
        {
            energy=Mathf.Min(100,energy+12*dt);
            if(InSanctuary)
            {
                if(!checkpoint) { checkpoint=true; Notify("Punto de control activado. Aquí recuperas vida y puedes comprar mejoras."); }
                health=Mathf.Min(100,health+18*dt);
            }
            foreach(Transform kit in medicalPickups)
                if(kit.gameObject.activeSelf && health<100 && Vector3.Distance(player.position+Vector3.up*.6f,kit.position)<1.4f)
                { health=Mathf.Min(100,health+35); kit.gameObject.SetActive(false); Notify("Botiquín: +35 de vida."); }
            foreach(Enemy e in enemies)
            {
                if(e.dead) continue;
                if(e.stunUntil>gameTime) continue;
                float range=Vector3.Distance(player.position,e.view.position);
                if(InSanctuary || range>30)
                {
                    e.warning=false; e.warningLight.gameObject.SetActive(false);
                    if(e.kind<2) MoveEnemy(e,e.home,2,dt);
                    continue;
                }
                Vector3 target=player.position+Vector3.up;
                Vector3 facing=target-e.view.position; facing.y=0;
                if(facing.sqrMagnitude>.001f) e.view.rotation=Quaternion.LookRotation(-facing);
                if(e.kind==0 && range>1.25f) MoveEnemy(e,player.position,2.8f,dt);
                if(e.kind==1)
                {
                    if(range>10) MoveEnemy(e,player.position,3.2f,dt);
                    Vector3 p=e.view.position; p.y=e.home.y+Mathf.Sin(gameTime*2+e.home.z)*.35f; e.view.position=p;
                }
                if(e.kind==3 && range>9) MoveEnemy(e,player.position,1.5f,dt);
                if(range<(e.kind==3?3.1f:1.7f) && e.kind!=1) TakeDamage(e.kind==3?30:18,"Golpe de robot");
                float attackRange=e.kind==0?8:25;
                if(range<attackRange && gameTime>=e.attackAt && !e.warning && VisibleFrom(e.view.position,target))
                {
                    e.warning=true; e.target=target; e.fireAt=gameTime+(e.kind==3?.9f:.7f);
                    e.warningLight.gameObject.SetActive(true);
                }
                if(e.warning && gameTime>=e.fireAt)
                {
                    e.warning=false; e.warningLight.gameObject.SetActive(false);
                    e.attackAt=gameTime+(e.kind==3 && e.health<e.maximum/2?1.1f:2.2f);
                    Vector3 direction=(e.target-e.view.position).normalized;
                    int spread=e.kind==3?1:0;
                    for(int n=-spread;n<=spread;n++)
                        Launch(e.view.position+direction*(e.kind==3?2:.8f),Quaternion.Euler(0,n*13,0)*direction*(e.kind==3?13:10),e.kind==3?25:14,true);
                }
            }
            UpdateBolts(dt);
            for(int i=flashes.Count-1;i>=0;i--)
            {
                flashes[i].view.localScale*=1+dt*3;
                if(gameTime>=flashes[i].expires) { Destroy(flashes[i].view.gameObject); flashes.RemoveAt(i); }
            }
            if(!defeated && !missionAwarded && bossDefeated && sessionRescues>=8 && InSanctuary)
            {
                missionAwarded=true; victoryScreen=true;
                if(!data.missionCompleted) { data.coins+=250; data.missionCompleted=true; }
                Save();
            }
        }
        void Launch(Vector3 origin,Vector3 velocity,int damage,bool hostile)
        {
            Transform view=Shape(hostile?"Luz proyectil enemigo":"Luz pulso del jugador",PrimitiveType.Sphere,origin,new Vector3(.17f,.17f,.45f),hostile?new Color(1,.2f,.05f):Color.cyan,transform);
            view.rotation=Quaternion.LookRotation(velocity);
            bolts.Add(new Bolt {view=view,velocity=velocity,damage=damage,hostile=hostile,expires=gameTime+4});
        }
        // Segment/sphere intersection prevents fast projectiles skipping small targets.
        public static float SegmentHit(Vector3 start,Vector3 end,Vector3 center,float radius)
        {
            Vector3 d=end-start, m=start-center;
            float a=Vector3.Dot(d,d), c=Vector3.Dot(m,m)-radius*radius;
            if(c<=0) return 0;
            if(a<.000001f) return -1;
            float b=Vector3.Dot(m,d), discriminant=b*b-a*c;
            if(discriminant<0) return -1;
            float t=(-b-Mathf.Sqrt(discriminant))/a;
            return t>=0 && t<=1?t:-1;
        }
        void UpdateBolts(float dt)
        {
            for(int i=bolts.Count-1;i>=0;i--)
            {
                Bolt b=bolts[i]; Vector3 a=b.view.position, end=a+b.velocity*dt;
                float length=Vector3.Distance(a,end), first=2; Enemy victim=null; bool playerHit=false;
                if(gameTime>=b.expires) { Destroy(b.view.gameObject); bolts.RemoveAt(i); continue; }
                foreach(RaycastHit hit in Physics.RaycastAll(a,b.velocity.normalized,length,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.transform==player || hit.transform.IsChildOf(player)) continue;
                    float t=hit.distance/Mathf.Max(length,.0001f);
                    if(t<first) first=t;
                }
                if(b.hostile && !InSanctuary && !defeated)
                {
                    float t=SegmentHit(a,end,player.position+Vector3.up*.9f,.6f);
                    if(t>=0 && t<first) { first=t; playerHit=true; }
                }
                else if(!b.hostile)
                {
                    foreach(Enemy e in enemies)
                    {
                        if(e.dead) continue;
                        float t=SegmentHit(a,end,e.view.position,e.radius);
                        if(t>=0 && t<first) { first=t; victim=e; }
                    }
                }
                if(first<=1)
                {
                    if(playerHit) TakeDamage(b.damage,"Proyectil enemigo");
                    if(victim!=null) DamageEnemy(victim,b.damage);
                    Pulse(Vector3.Lerp(a,end,first),b.hostile?Color.yellow:Color.cyan,.25f);
                    Destroy(b.view.gameObject); bolts.RemoveAt(i);
                }
                else b.view.position=end;
            }
        }
        void DamageEnemy(Enemy e,int amount)
        {
            if(e.dead) return;
            e.health=Mathf.Max(0,e.health-amount);
            if(e.health>0) return;
            e.dead=true; e.view.gameObject.SetActive(false); destroyed++;
            data.coins+=e.kind==3?100:12;
            Pulse(e.view.position,new Color(1,.45f,.1f),e.kind==3?2:.8f);
            if(e.kind==3) { bossDefeated=true; foreach(Enemy other in enemies) if(other.kind==3 && !other.dead) bossDefeated=false; Notify("Núcleo destruido. Rescata 8 animales y regresa al santuario para completar la misión."); }
            Save();
        }
        void Pulse(Vector3 position,Color color,float size)
        {
            if(flashes.Count>=40) return;
            Transform view=Shape("Luz impacto",PrimitiveType.Sphere,position,Vector3.one*size,color,transform);
            flashes.Add(new Flash {view=view,expires=gameTime+.16f});
        }
        void TakeDamage(int amount,string cause)
        {
            if(defeated || InSanctuary || gameTime<damageUntil || gameTime<dashUntil) return;
            damageUntil=gameTime+.85f;
            audioSource.PlayOneShot(hitSound,.2f);
            if(data.shields>0) { data.shields--; Notify("Escudo consumido: impacto bloqueado."); Save(); return; }
            amount=Mathf.RoundToInt(amount*(difficulty==0?.65f:difficulty==2?1.35f:1));
            health=Mathf.Max(0,health-amount);
            ScatterAnimals();
            Notify(cause+": -"+amount+" de vida. Recupera los animales caídos.");
            if(health<=0)
            {
                defeated=true; defeatCause=cause; data.coins=Mathf.Max(0,data.coins-10); Save();
            }
        }
        void RetryCombat()
        {
            ScatterAnimals(); ResetCombat(false);
            controller.enabled=false;
            player.position=checkpoint?sanctuaryRespawn:playerStart;
            controller.enabled=true; verticalSpeed=0;
            Notify("Reintento: animales rescatados y mejoras conservados. Enemigos reiniciados.");
        }
        void ResetCombat(bool fresh)
        {
            health=100; energy=100; ammunition[0]=12; ammunition[1]=24;
            defeated=false; victoryScreen=false; paused=false; reloadAt=0; nextShot=0; nextDash=0; dashUntil=0;
            damageUntil=gameTime+2; bossDefeated=false; missionAwarded=false;
            foreach(Bolt b in bolts) Destroy(b.view.gameObject); bolts.Clear();
            foreach(Flash f in flashes) Destroy(f.view.gameObject); flashes.Clear();
            foreach(Enemy e in enemies)
            { e.health=e.maximum; e.dead=false; e.warning=false; e.stunUntil=0; e.attackAt=gameTime+2; e.view.position=e.home; e.view.gameObject.SetActive(true); e.warningLight.gameObject.SetActive(false); }
            foreach(Transform kit in medicalPickups) kit.gameObject.SetActive(true);
            if(fresh) { sessionRescues=0; destroyed=0; checkpoint=false; briefing=true; }
        }
    }
}
