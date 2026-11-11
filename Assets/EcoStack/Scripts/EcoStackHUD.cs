using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace EcoStack
{
    public partial class EcoStackGame
    {
        void Bar(Rect area,float value,float max,Color color,string caption)
        {
            GUI.color=new Color(.05f,.08f,.1f,.94f); GUI.DrawTexture(area,Texture2D.whiteTexture);
            GUI.color=color; GUI.DrawTexture(new Rect(area.x+2,area.y+2,(area.width-4)*Mathf.Clamp01(value/max),area.height-4),Texture2D.whiteTexture);
            GUI.color=Color.white; GUI.Label(new Rect(area.x+8,area.y+1,area.width-12,area.height),caption,text);
        }
        void CombatGUI()
        {
            if(data==null || player==null) return;
            if(heading==null)
            {
                heading=new GUIStyle(GUI.skin.label) {fontSize=25,fontStyle=FontStyle.Bold,wordWrap=true};
                text=new GUIStyle(GUI.skin.label) {fontSize=16,wordWrap=true};
                button=new GUIStyle(GUI.skin.button) {fontSize=16,wordWrap=true};
            }
            Matrix4x4 oldMatrix=GUI.matrix;
            float scale=Mathf.Max(.1f,Mathf.Min(Screen.width/1100f,Screen.height/700f));
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            GUI.Box(new Rect(15,15,355,187),"");
            GUI.Label(new Rect(28,22,330,35),"ECO-STACK / RESCATE",heading);
            Bar(new Rect(28,62,325,26),health,100,new Color(.18f,.66f,.35f),"VIDA  "+Mathf.CeilToInt(health)+" / 100");
            Bar(new Rect(28,94,325,24),energy,100,new Color(.1f,.47f,.75f),"PULSO  "+Mathf.FloorToInt(energy)+" / 100");
            GUI.Label(new Rect(28,123,325,24),WeaponName+"  "+ammunition[weapon]+" / "+Magazine,text);
            GUI.Label(new Rect(28,147,325,24),reloadAt>0?"RECARGANDO…":"Monedas: "+data.coins+"   Escudos: "+data.shields,text);
            GUI.Label(new Rect(28,172,325,24),"Animales: "+stack.Count+" / "+Capacity+"   |   Bajas: "+destroyed,text);
            GUI.Box(new Rect(725,15,360,125),"");
            GUI.Label(new Rect(740,23,330,30),"MISIÓN DE ESTA SESIÓN",heading);
            GUI.Label(new Rect(740,58,325,70),"Rescatados: "+sessionRescues+" / 8\nNúcleo: "+(bossDefeated?"destruido":"activo")+"\nEntrega y termina en el santuario.",text);
            foreach(Enemy boss in enemies)
                if(boss.kind==3 && !boss.dead && Vector3.Distance(player.position,boss.view.position)<32)
                    Bar(new Rect(385,23,325,28),boss.health,boss.maximum,new Color(.8f,.2f,.15f),boss.health<boss.maximum/2?"NÚCLEO · FASE 2":"NÚCLEO · FASE 1");
            GUI.Label(new Rect(20,213,345,65),InSanctuary?"SANTUARIO · Curación automática\nPisa los círculos amarillos para cobrar.":"FÁBRICA · Zona hostil\nLuz roja sobre enemigo = ataque inminente.",text);
            GUI.Box(new Rect(15,644,1070,43),"");
            GUI.Label(new Rect(26,649,1050,40),"WASD: mover   Espacio: salto   Ratón: apuntar   Clic: disparar   1/2: arma   R: recarga\nQ / clic derecho: pulso   Shift: esquiva (3 s)   Esc: pausa",text);
            if(gameTime<messageUntil) { GUI.Box(new Rect(225,575,650,58),""); GUI.Label(new Rect(237,583,626,45),message,text); }
            if(!SimulationStopped)
            {
                Camera cam=cameraTransform.GetComponent<Camera>();
                foreach(Enemy e in enemies)
                {
                    if(e.dead || Vector3.Distance(player.position,e.view.position)>30) continue;
                    Vector3 screen=cam.WorldToScreenPoint(e.view.position+Vector3.up*(e.kind==3?2.7f:1.2f));
                    if(screen.z<=0) continue;
                    float x=screen.x/scale,y=(Screen.height-screen.y)/scale;
                    if(y<150 || y>560 || x<50 || x>1050) continue;
                    Bar(new Rect(x-60,y,120,23),e.health,e.maximum,e.stunUntil>gameTime?Color.cyan:new Color(.8f,.2f,.18f),e.kind==3?"NÚCLEO":e.kind==2?"TORRETA":e.kind==1?"DRON":"ROBOT");
                }
                Vector3 aim=cam.WorldToScreenPoint(aimPoint);
                if(aim.z>0) GUI.Label(new Rect(aim.x/scale-10,(Screen.height-aim.y)/scale-15,30,30),"+",heading);
            }
            if(InSanctuary && !SimulationStopped)
            {
                GUILayout.BeginArea(new Rect(725,152,360,408),GUI.skin.box);
                GUILayout.Label("TALLER DEL SANTUARIO",heading);
                if(GUILayout.Button(data.capacityLevel>=5?"Carga al máximo":"Carga +1: "+((data.capacityLevel+1)*40)+" monedas",button,GUILayout.Height(44))) Buy(0);
                if(GUILayout.Button(data.speedLevel>=5?"Velocidad al máximo":"Velocidad: "+((data.speedLevel+1)*40)+" monedas",button,GUILayout.Height(44))) Buy(1);
                if(GUILayout.Button("Escudo: 30 monedas (máximo 3)",button,GUILayout.Height(44))) Buy(2);
                if(GUILayout.Button(data.weaponLevel>=3?"Potencia al máximo":"Potencia +6: "+((data.weaponLevel+1)*60)+" monedas",button,GUILayout.Height(44)))
                {
                    int price=(data.weaponLevel+1)*60;
                    if(data.weaponLevel<3 && data.coins>=price) { data.coins-=price; data.weaponLevel++; Save(); Notify("Potencia de ambas armas mejorada."); }
                    else Notify("Potencia al máximo o monedas insuficientes.");
                }
                if(GUILayout.Button("Volver a la entrada de la fábrica",button,GUILayout.Height(44)))
                {
                    controller.enabled=false; player.position=playerStart; controller.enabled=true; verticalSpeed=0;
                    damageUntil=gameTime+2; ammunition[0]=12; ammunition[1]=24; reloadAt=0;
                }
                GUILayout.Label("Entregas: +8 monedas por animal. Robots: +12. Jefe: +100. Recintos: producción pasiva mientras juegas.",text);
                GUILayout.EndArea();
            }
            if(SimulationStopped)
            {
                GUI.color=new Color(0,0,0,.65f); GUI.DrawTexture(new Rect(0,0,Screen.width/scale,Screen.height/scale),Texture2D.whiteTexture); GUI.color=Color.white;
                GUILayout.BeginArea(new Rect(310,170,480,440),GUI.skin.box);
                if(briefing)
                {
                    GUILayout.Label("OPERACIÓN ECO-STACK",heading);
                    GUILayout.Label("Entra en la fábrica, rescata 8 animales y destruye el Núcleo de seguridad. Termina en el santuario. Los animales se recogen al tocarlos y se entregan automáticamente.",text);
                    GUILayout.Space(12);
                    GUILayout.Label("Dificultad (modifica el daño recibido)",text);
                    difficulty=GUILayout.Toolbar(difficulty,new string[]{"Fácil","Normal","Difícil"},GUILayout.Height(38));
                    GUILayout.Label("Apunta con el ratón. Clic dispara. 1/2 cambia de arma. R recarga. Q lanza un pulso de área. Shift esquiva. El santuario cura y guarda el punto de control.",text);
                    GUILayout.Space(12);
                    if(GUILayout.Button("INICIAR MISIÓN",button,GUILayout.Height(55))) { briefing=false; damageUntil=gameTime+2; }
                }
                else if(defeated)
                {
                    GUILayout.Label("RESCATISTA FUERA DE COMBATE",heading);
                    GUILayout.Label(defeatCause+". Penalización: hasta 10 monedas. Conservas tus rescates, mejoras y monedas restantes.",text);
                    if(GUILayout.Button(checkpoint?"Reintentar desde el santuario":"Reintentar desde la entrada",button,GUILayout.Height(55))) RetryCombat();
                }
                else if(victoryScreen)
                {
                    GUILayout.Label("¡MISIÓN COMPLETADA!",heading);
                    GUILayout.Label("Rescataste "+sessionRescues+" animales y destruiste el Núcleo. La primera victoria otorga 250 monedas. Puedes seguir ampliando tu santuario.",text);
                    if(GUILayout.Button("Continuar jugando",button,GUILayout.Height(55))) victoryScreen=false;
                }
                else
                {
                    GUILayout.Label("PAUSA",heading);
                    if(GUILayout.Button("Continuar",button,GUILayout.Height(45))) paused=false;
                    if(GUILayout.Button("Guardar progreso",button,GUILayout.Height(45))) { Save(); Notify("Progreso guardado."); }
                    if(GUILayout.Button("Borrar progreso y empezar de cero",button,GUILayout.Height(45))) confirmReset=true;
                    if(confirmReset && GUILayout.Button("Confirmar borrado",button,GUILayout.Height(45))) ResetProgress();
                    GUILayout.Label("Volumen",text);
                    soundVolume=GUILayout.HorizontalSlider(soundVolume,0,1); audioSource.volume=soundVolume;
                    GUILayout.Label("Se guardan monedas, mejoras y animales entregados. Los combates y la misión se reinician al abrir la escena.",text);
                }
                GUILayout.EndArea();
            }
            GUI.matrix=oldMatrix; GUI.color=Color.white;
        }
    }
}
