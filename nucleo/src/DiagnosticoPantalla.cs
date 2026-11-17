// DiagnosticoPantalla.cs
// Prueba de la pantalla de héroes SIN jugar (opción Diagnostico.ProbarPantallaHeroes, false por defecto): un componente
// de Unity que, ya en el menú principal y unos segundos después de la autoprueba:
//   1) abre la pantalla de héroes de la nueva partida (PlayPanel.OpenPanel, lo mismo que el botón «Jugar»),
//   2) escribe en el log cuántas casillas hay, cuáles están activas y qué héroe tiene cada una de las nuevas,
//   3) hace una captura (ScreenCapture), pulsa la casilla de héroe 901 como si fuera un clic, escribe lo que enseña
//      el panel del héroe (nombre, facción, región, pasiva, bonus de campaña, oro) y otra captura,
//   4) baja el desplazamiento hasta el final y captura, y pulsa la casilla de héroe 903 y captura.
// No pulsa «Crear ejército» ni empieza ninguna campaña; abrir la pantalla solo lee los saves. Las capturas van a
// BepInEx\config\ZeroNucleo_capturas\. Cada paso va en try/catch y el resultado sale con la etiqueta
// [diagnostico].
// DiagnosticoPantalla.cs
// Hero screen test WITHOUT playing (option Diagnostico.ProbarPantallaHeroes, false by default): a Unity component
// that, already in the main menu and a few seconds after the self-test:
//   1) opens the new run hero screen (PlayPanel.OpenPanel, the same as the «Jugar» button),
//   2) writes to the log how many slots there are, which are active and which hero each of the new ones has,
//   3) takes a screenshot (ScreenCapture), presses the hero 901 slot as if it were a click, writes what the
//      hero panel shows (name, faction, region, passive, campaign bonus, gold) and another screenshot,
//   4) scrolls down to the end and captures, and presses the hero 903 slot and captures.
// It does not press «Crear ejército» nor start any campaign; opening the screen only reads the saves. The screenshots go to
// BepInEx\config\ZeroNucleo_capturas\. Each step is wrapped in try/catch and the result is output with the tag
// [diagnostico].

using System;
using System.IO;
using BepInEx;
using Il2CppInterop.Runtime;
using TJ;
using TJ.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: componente de prueba de la pantalla de héroes
    // Section: heroes screen test component
    // -----------------------------------------------------------------------
    public class DiagnosticoPantalla : MonoBehaviour
    {
        public DiagnosticoPantalla(IntPtr ptr) : base(ptr) { }

        private int _paso;
        private float _siguiente = -1f;
        private PlayPanel _panel;
        private string _carpeta;

        // Momento (tiempo real) en que terminó la autoprueba; lo pone AutoPrueba al acabar
        // Moment (real time) when the self-test finished; AutoPrueba sets it when it ends
        internal static float FinAutoprueba = -1f;

        // Espera entre pasos y tope para encontrar el menú
        // Wait between steps and limit for finding the menu
        private const float EsperaInicial = 12f, Tope = 240f;

        private void Update()
        {
            try
            {
                float ahora = Time.realtimeSinceStartup;
                if (_paso < 0 || FinAutoprueba < 0f || ahora < _siguiente) return;
                switch (_paso)
                {
                    case 0:   // esperar al menú principal con la pantalla de héroes cargada
                              // wait for the main menu with the heroes screen loaded
                        if (ahora < FinAutoprueba + EsperaInicial) return;
                        _panel = BuscarPanel();
                        if (_panel == null)
                        {
                            if (ahora > FinAutoprueba + Tope) { Plugin.Aviso("[diagnostico] No se encontró la pantalla de héroes (PlayPanel) en el menú; se abandona la prueba."); _paso = -1; }
                            else _siguiente = ahora + 2f;
                            return;
                        }
                        _carpeta = Path.Combine(Paths.ConfigPath, Plugin.Prefijo + "_capturas");
                        Directory.CreateDirectory(_carpeta);
                        Plugin.Info("[diagnostico] Abriendo la pantalla de héroes (sin empezar partida).");
                        _panel.OpenPanel();
                        Avanzar(9f);
                        break;
                    case 1:
                        Informar("al abrir");
                        Capturar("1_pantalla_heroes");
                        Avanzar(2f);
                        break;
                    case 2:
                        Pulsar(901);
                        Avanzar(5f);
                        break;
                    case 3:
                        Informar("con Yarrick elegido");
                        Capturar("2_yarrick");
                        Desplazar();
                        Avanzar(1.5f);
                        break;
                    case 4:
                        Capturar("3_desplazada_abajo");
                        Pulsar(903);
                        Avanzar(5f);
                        break;
                    case 5:
                        Informar("con Vader elegido");
                        Capturar("4_vader");
                        Avanzar(2f);
                        break;
                    default:
                        Plugin.Info($"[diagnostico] Pantalla de héroes: prueba terminada. Capturas en {_carpeta}.");
                        _paso = -1;
                        break;
                }
            }
            catch (Exception e)
            {
                Plugin.Error($"[diagnostico] Paso {_paso} FALLO: {e}");
                _paso = -1;
            }
        }

        private void Avanzar(float segundos) { _paso++; _siguiente = Time.realtimeSinceStartup + segundos; }

        // PlayPanel de la escena del menú (también si está desactivado)
        // PlayPanel of the menu scene (even if it is disabled)
        private static PlayPanel BuscarPanel()
        {
            var todos = Resources.FindObjectsOfTypeAll(Il2CppType.Of<PlayPanel>());
            for (int i = 0; todos != null && i < todos.Length; i++)
            {
                var p = todos[i]?.TryCast<PlayPanel>();
                if (p != null && p.gameObject.scene.IsValid()) return p;
            }
            return null;
        }

        // Estado de la pantalla: casillas, héroes de las nuevas y lo que enseña el panel del héroe
        // Screen state: slots, heroes of the new ones and what the hero panel shows
        private void Informar(string momento)
        {
            var v = _panel.commanderView;
            var tiles = v.tiles;
            int activas = 0;
            var nuevas = new System.Text.StringBuilder();
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                activas++;
                int id = t.Hero.HeroID;
                if (id >= 900)
                    nuevas.Append($" [{i}: héroe {id} «{t.nameText?.text}», retrato {(t.portrait?.sprite == null ? "null" : t.portrait.sprite.name)}]");
            }
            var desplazamiento = v.transform.GetComponentInChildren<ScrollRect>(true);
            Plugin.Info($"[diagnostico] Pantalla de héroes {momento}: {tiles.Length} casillas, {activas} visibles, títulos {v.factionNames.Length}; " +
                        $"desplazamiento {(desplazamiento == null ? "no" : desplazamiento.gameObject.name)}; nuevas:{nuevas}");
            Plugin.Info($"[diagnostico] Panel del héroe {momento}: héroe {_panel.hero.HeroID}, nombre «{v.heroNameText?.text}», facción «{v.heroFactionText?.text}», " +
                        $"región «{v.heroRegionText?.text}», pasiva «{v.battleTitle?.text}»: «{v.battleBody?.text}», campaña «{v.campaignTitle?.text}»: «{v.campaignBody?.text}», " +
                        $"emblema «{v.unitNameText?.text}», tesoro «{v.treasuryText?.text?.Replace("\n", " ")}»");
        }

        // Pulsa la casilla del héroe como un clic (el mismo onClick que dispara el ratón)
        // Presses the hero's slot like a click (the same onClick the mouse triggers)
        private void Pulsar(int heroID)
        {
            var tiles = _panel.commanderView.tiles;
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                if (t == null || !t.gameObject.activeInHierarchy || t.Hero.HeroID != heroID) continue;
                Plugin.Info($"[diagnostico] Clic en la casilla {i} (héroe {heroID}).");
                t.button.onClick.Invoke();
                return;
            }
            Plugin.Aviso($"[diagnostico] No hay casilla visible con el héroe {heroID}.");
        }

        // Lleva el desplazamiento de la rejilla de facciones hasta abajo
        // Scrolls the faction grid all the way down
        private void Desplazar()
        {
            var sr = _panel.commanderView.transform.GetComponentInChildren<ScrollRect>(true);
            if (sr == null || sr.gameObject.name != PantallaHeroes.NombreDesplazamiento) { Plugin.Info("[diagnostico] Sin desplazamiento propio en la pantalla."); return; }
            sr.verticalNormalizedPosition = 0f;
            Plugin.Info($"[diagnostico] Desplazamiento abajo: contenido {sr.content.rect.height:0} px, ventana {sr.viewport.rect.height:0} px.");
        }

        // Captura de pantalla (Unity la escribe al final del fotograma)
        // Screenshot (Unity writes it at the end of the frame)
        private void Capturar(string nombre)
        {
            string ruta = Path.Combine(_carpeta, $"{DateTime.Now:yyyyMMdd-HHmmss}_{nombre}.png");
            ScreenCapture.CaptureScreenshot(ruta);
            Plugin.Info($"[diagnostico] Captura: {ruta}");
        }
    }
}
