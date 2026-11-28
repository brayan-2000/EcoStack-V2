using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace EcoStack
{
    public partial class EcoStackGame
    {
        private GUIStyle panelStyle;
        private GUIStyle titleCenterStyle;
        private GUIStyle richTextStyle;
        private GUIStyle richButtonStyle;
        private Texture2D panelBgTex;
        private Texture2D darkOverlayTex;

        void InitCustomStyles()
        {
            if (panelStyle != null) return;

            panelBgTex = MakeSolidTex(new Color(0.06f, 0.09f, 0.13f, 0.92f));
            darkOverlayTex = MakeSolidTex(new Color(0.02f, 0.04f, 0.06f, 0.78f));

            heading = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                richText = true
            };

            titleCenterStyle = new GUIStyle(heading)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26
            };

            text = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                richText = true
            };

            richTextStyle = new GUIStyle(text)
            {
                alignment = TextAnchor.MiddleCenter
            };

            button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                richText = true,
                padding = new RectOffset(12, 12, 8, 8)
            };

            richButtonStyle = new GUIStyle(button)
            {
                fontSize = 18
            };

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(18, 18, 16, 16)
            };
            panelStyle.normal.background = panelBgTex;
        }

        Texture2D MakeSolidTex(Color col)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        void DrawStyledPanel(Rect r, Color borderColor)
        {
            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(r.x - 1.5f, r.y - 1.5f, r.width + 3f, r.height + 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Box(r, "", panelStyle);
        }

        void Bar(Rect area, float value, float max, Color color, string caption)
        {
            GUI.color = new Color(0.04f, 0.06f, 0.09f, 0.96f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(area.x + 2, area.y + 2, (area.width - 4) * Mathf.Clamp01(value / max), area.height - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 10, area.y + 1, area.width - 16, area.height), "<b>" + caption + "</b>", text);
        }

        void CombatGUI()
        {
            if (data == null || player == null) return;
            InitCustomStyles();

            Matrix4x4 oldMatrix = GUI.matrix;
            float scale = Mathf.Max(.1f, Mathf.Min(Screen.width / 1100f, Screen.height / 700f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

            // Panel Superior Izquierdo: Estado del Rescatista
            DrawStyledPanel(new Rect(15, 15, 360, 192), new Color(0.18f, 0.75f, 0.45f, 0.55f));
            GUI.Label(new Rect(28, 22, 335, 34), "<color=#2ECC71>ECO-STACK</color> <color=#95A5A6>/</color> <color=#ECF0F1>RESCATE</color>", heading);
            Bar(new Rect(28, 62, 330, 26), health, 100, new Color(0.16f, 0.68f, 0.36f), "VIDA   " + Mathf.CeilToInt(health) + " / 100");
            Bar(new Rect(28, 94, 330, 24), energy, 100, new Color(0.12f, 0.55f, 0.85f), "PULSO  " + Mathf.FloorToInt(energy) + " / 100");
            GUI.Label(new Rect(28, 124, 330, 24), "<color=#BDC3C7>Arma:</color> <b>" + WeaponName + "</b>  <color=#00E5FF>" + ammunition[weapon] + " / " + Magazine + "</color>", text);
            GUI.Label(new Rect(28, 149, 330, 24), reloadAt > 0
                ? "<color=#FF9F43><b>RECARGANDO ARMA…</b></color>"
                : "<color=#F1C40F><b>Monedas: " + data.coins + "</b></color>    <color=#00E5FF><b>Escudos: " + data.shields + "</b></color>", text);
            GUI.Label(new Rect(28, 174, 330, 24), "<color=#2ECC71>Animales: <b>" + stack.Count + " / " + Capacity + "</b></color>   <color=#7F8C8D>|</color>   <color=#E74C3C>Bajas: <b>" + destroyed + "</b></color>", text);

            // Panel Superior Derecho: Objetivos de Misión
            DrawStyledPanel(new Rect(720, 15, 365, 130), new Color(0.18f, 0.75f, 0.45f, 0.55f));
            GUI.Label(new Rect(735, 22, 340, 32), "<color=#2ECC71>OBJETIVOS DE MISIÓN</color>", heading);
            string nucleoEstado = bossDefeated ? "<color=#2ECC71><b>DESTRUIDO</b></color>" : "<color=#FF5733><b>ACTIVO</b></color>";
            GUI.Label(new Rect(735, 56, 340, 82),
                "• Rescatados: <color=#F1C40F><b>" + sessionRescues + " / 8</b></color>\n" +
                "• Núcleo central: " + nucleoEstado + "\n" +
                "<color=#BDC3C7>Entrega en el santuario para finalizar.</color>", text);

            // Barra de Vida del Jefe (Núcleo)
            foreach (Enemy boss in enemies)
                if (boss.kind == 3 && !boss.dead && Vector3.Distance(player.position, boss.view.position) < 32)
                    Bar(new Rect(390, 22, 315, 28), boss.health, boss.maximum, new Color(0.85f, 0.2f, 0.16f), boss.health < boss.maximum / 2 ? "NÚCLEO · FASE 2" : "NÚCLEO · FASE 1");

            // Indicador de Zona (oculto durante menús centrales para evitar superposición)
            if (!SimulationStopped)
            {
                DrawStyledPanel(new Rect(15, 216, 360, 56), InSanctuary ? new Color(0.18f, 0.8f, 0.44f, 0.5f) : new Color(0.9f, 0.3f, 0.23f, 0.5f));
                GUI.Label(new Rect(26, 222, 340, 48), InSanctuary
                    ? "<color=#2ECC71><b>SANTUARIO</b></color> · Curación automática\n<color=#F1C40F>Pisa los círculos amarillos para cobrar.</color>"
                    : "<color=#FF6B6B><b>FÁBRICA</b></color> · Zona hostil\n<color=#BDC3C7>Luz roja sobre enemigo = ataque inminente.</color>", text);
            }

            // Barra Inferior de Controles
            DrawStyledPanel(new Rect(15, 642, 1070, 46), new Color(0.3f, 0.45f, 0.55f, 0.45f));
            GUI.Label(new Rect(26, 646, 1050, 40),
                "<color=#00E5FF><b>WASD</b></color>: mover   <color=#00E5FF><b>Espacio</b></color>: salto   <color=#00E5FF><b>Ratón</b></color>: apuntar   <color=#00E5FF><b>Clic</b></color>: disparar   <color=#00E5FF><b>1/2</b></color>: arma   <color=#00E5FF><b>R</b></color>: recarga\n" +
                "<color=#00E5FF><b>Q / Clic der.</b></color>: pulso   <color=#00E5FF><b>Shift</b></color>: esquiva (3 s)   <color=#F1C40F><b>Esc</b></color>: pausa", text);

            // Notificaciones Flotantes
            if (gameTime < messageUntil)
            {
                DrawStyledPanel(new Rect(225, 575, 650, 54), new Color(0.95f, 0.77f, 0.06f, 0.7f));
                GUI.Label(new Rect(237, 581, 626, 42), "<color=#F1C40F><b>" + message + "</b></color>", richTextStyle);
            }

            // Barras de Enemigos y Mira en Combate
            if (!SimulationStopped)
            {
                Camera cam = cameraTransform.GetComponent<Camera>();
                foreach (Enemy e in enemies)
                {
                    if (e.dead || Vector3.Distance(player.position, e.view.position) > 30) continue;
                    Vector3 screen = cam.WorldToScreenPoint(e.view.position + Vector3.up * (e.kind == 3 ? 2.7f : 1.2f));
                    if (screen.z <= 0) continue;
                    float x = screen.x / scale, y = (Screen.height - screen.y) / scale;
                    if (y < 150 || y > 560 || x < 50 || x > 1050) continue;
                    Bar(new Rect(x - 60, y, 120, 23), e.health, e.maximum, e.stunUntil > gameTime ? Color.cyan : new Color(0.85f, 0.22f, 0.18f), e.kind == 3 ? "NÚCLEO" : e.kind == 2 ? "TORRETA" : e.kind == 1 ? "DRON" : "ROBOT");
                }
                Vector3 aim = cam.WorldToScreenPoint(aimPoint);
                if (aim.z > 0) GUI.Label(new Rect(aim.x / scale - 10, (Screen.height - aim.y) / scale - 15, 30, 30), "<color=#00E5FF>+</color>", heading);
            }

            // Panel Taller del Santuario (Mejoras Tycoon)
            if (InSanctuary && !SimulationStopped)
            {
                DrawStyledPanel(new Rect(720, 155, 365, 415), new Color(0.95f, 0.77f, 0.06f, 0.65f));
                GUILayout.BeginArea(new Rect(734, 165, 338, 395));
                GUILayout.Label("<color=#F1C40F>TALLER DEL SANTUARIO</color>", heading);
                GUILayout.Space(6);
                if (GUILayout.Button(data.capacityLevel >= 5 ? "<color=#95A5A6>Carga al máximo</color>" : "Carga +1   <color=#F1C40F>(" + ((data.capacityLevel + 1) * 40) + " monedas)</color>", button, GUILayout.Height(44))) Buy(0);
                GUILayout.Space(4);
                if (GUILayout.Button(data.speedLevel >= 5 ? "<color=#95A5A6>Velocidad al máximo</color>" : "Velocidad   <color=#F1C40F>(" + ((data.speedLevel + 1) * 40) + " monedas)</color>", button, GUILayout.Height(44))) Buy(1);
                GUILayout.Space(4);
                if (GUILayout.Button("Escudo extra   <color=#00E5FF>(30 monedas · máx. 3)</color>", button, GUILayout.Height(44))) Buy(2);
                GUILayout.Space(4);
                if (GUILayout.Button(data.weaponLevel >= 3 ? "<color=#95A5A6>Potencia al máximo</color>" : "Potencia +6   <color=#F1C40F>(" + ((data.weaponLevel + 1) * 60) + " monedas)</color>", button, GUILayout.Height(44)))
                {
                    int price = (data.weaponLevel + 1) * 60;
                    if (data.weaponLevel < 3 && data.coins >= price) { data.coins -= price; data.weaponLevel++; Save(); Notify("Potencia de ambas armas mejorada."); }
                    else Notify("Potencia al máximo o monedas insuficientes.");
                }
                GUILayout.Space(6);
                if (GUILayout.Button("<color=#2ECC71>Volver a la entrada de la fábrica</color>", button, GUILayout.Height(44)))
                {
                    controller.enabled = false; player.position = playerStart; controller.enabled = true; verticalSpeed = 0;
                    damageUntil = gameTime + 2; ammunition[0] = 12; ammunition[1] = 24; reloadAt = 0;
                }
                GUILayout.Space(6);
                GUILayout.Label("<color=#BDC3C7>Entregas: +8 monedas/animal · Robots: +12 · Jefe: +100 · Recintos: ingreso pasivo.</color>", text);
                GUILayout.EndArea();
            }

            // Pantallas Modales (Briefing, Derrota, Victoria, Pausa)
            if (SimulationStopped)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width / scale, Screen.height / scale), darkOverlayTex);

                Rect modalRect = new Rect(290, 155, 520, 455);
                Color modalBorder = defeated ? new Color(0.9f, 0.25f, 0.2f, 0.8f) : new Color(0.18f, 0.8f, 0.44f, 0.8f);
                DrawStyledPanel(modalRect, modalBorder);

                GUILayout.BeginArea(new Rect(modalRect.x + 22, modalRect.y + 18, modalRect.width - 44, modalRect.height - 36));
                if (briefing)
                {
                    GUILayout.Label("<color=#2ECC71>OPERACIÓN ECO-STACK</color>", titleCenterStyle);
                    GUILayout.Space(10);
                    GUILayout.Label("Entra en la fábrica, rescata <color=#F1C40F><b>8 animales</b></color> y destruye el <color=#FF6B6B><b>Núcleo de seguridad</b></color>. Termina en el santuario. Los animales se recogen al tocarlos y se entregan automáticamente.", text);
                    GUILayout.Space(14);
                    GUILayout.Label("<color=#00E5FF><b>Dificultad</b></color> <color=#95A5A6>(modifica el daño recibido)</color>", text);
                    GUILayout.Space(4);
                    difficulty = GUILayout.Toolbar(difficulty, new string[] { "<color=#2ECC71>Fácil</color>", "<color=#F1C40F>Normal</color>", "<color=#FF6B6B>Difícil</color>" }, button, GUILayout.Height(40));
                    GUILayout.Space(14);
                    GUILayout.Label("<color=#BDC3C7>Apunta con el ratón · Clic dispara · 1/2 cambia arma · R recarga · Q pulso de área · Shift esquiva. El santuario cura y guarda tu punto de control.</color>", text);
                    GUILayout.Space(16);
                    if (GUILayout.Button("<color=#2ECC71>▶   INICIAR MISIÓN</color>", richButtonStyle, GUILayout.Height(56))) { briefing = false; damageUntil = gameTime + 2; }
                }
                else if (defeated)
                {
                    GUILayout.Label("<color=#FF6B6B>RESCATISTA FUERA DE COMBATE</color>", titleCenterStyle);
                    GUILayout.Space(12);
                    GUILayout.Label(defeatCause + ".\n<color=#F1C40F>Penalización: hasta 10 monedas.</color> Conservas tus rescates, mejoras y monedas restantes.", text);
                    GUILayout.Space(20);
                    if (GUILayout.Button(checkpoint ? "<color=#2ECC71>Reintentar desde el santuario</color>" : "<color=#2ECC71>Reintentar desde la entrada</color>", richButtonStyle, GUILayout.Height(56))) RetryCombat();
                }
                else if (victoryScreen)
                {
                    GUILayout.Label("<color=#2ECC71>¡MISIÓN COMPLETADA!</color>", titleCenterStyle);
                    GUILayout.Space(12);
                    GUILayout.Label("Rescataste <color=#F1C40F><b>" + sessionRescues + " animales</b></color> y destruiste el Núcleo.\nLa primera victoria otorga <color=#F1C40F><b>250 monedas</b></color>. Puedes seguir ampliando tu santuario.", text);
                    GUILayout.Space(20);
                    if (GUILayout.Button("<color=#2ECC71>Continuar jugando</color>", richButtonStyle, GUILayout.Height(56))) victoryScreen = false;
                }
                else
                {
                    GUILayout.Label("<color=#00E5FF>PAUSA</color>", titleCenterStyle);
                    GUILayout.Space(10);
                    if (GUILayout.Button("<color=#2ECC71>Continuar</color>", button, GUILayout.Height(44))) paused = false;
                    GUILayout.Space(4);
                    if (GUILayout.Button("Guardar progreso", button, GUILayout.Height(44))) { Save(); Notify("Progreso guardado."); }
                    GUILayout.Space(4);
                    if (GUILayout.Button("<color=#FF9F43>Borrar progreso y empezar de cero</color>", button, GUILayout.Height(44))) confirmReset = true;
                    if (confirmReset && GUILayout.Button("<color=#FF5733>Confirmar borrado definitivo</color>", button, GUILayout.Height(44))) ResetProgress();
                    GUILayout.Space(10);
                    GUILayout.Label("<b>Volumen general:</b> <color=#00E5FF>" + Mathf.RoundToInt(soundVolume * 100f) + "%</color>", text);
                    soundVolume = GUILayout.HorizontalSlider(soundVolume, 0, 1); audioSource.volume = soundVolume;
                    GUILayout.Space(8);
                    GUILayout.Label("<color=#95A5A6>Se guardan monedas, mejoras y animales entregados. Los combates se reinician al abrir la escena.</color>", text);
                }
                GUILayout.EndArea();
            }

            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
        }
    }
}
