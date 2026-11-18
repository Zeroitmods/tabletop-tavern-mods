// AvisoInstalacion.cs
// 0.7.11 (juego 1.11.17.2): aviso EN PANTALLA de problemas de instalación de los módulos de Zero y estado de cada módulo en
// el menú de mods del juego. La comprobación es pura (LogicaInstalacion.cs); aquí solo va lo que toca el juego.
// AvisoInstalacion.cs
// 0.7.11 (game 1.11.17.2): ON-SCREEN warning of installation problems with Zero modules and status of each module in
// the game's mod menu. The check is pure (LogicaInstalacion.cs); only what touches the game is here.
//
// Cuándo se enseña el aviso (lección de la 0.7.8 y la 0.7.9, arreglada en la 0.7.10: NotificationManager.ErrorNotification
// en el arranque, con las tablas de texto sin cargar, colgaba el juego): NUNCA en Plugin.Load ni en el arranque. Un postfix de
// Memori.Scenes.SceneHandler.AlertOfSceneSetUpComlete (método normal, no async: xref de la build 26749277, juego 1.11.17, 18-11-2026, lo llama
// MainMenu+<Load>d__77.MoveNext al terminar de montar el menú) apunta la hora la primera vez que el estado es MainMenu, y
// un componente propio lo enseña 3 s después con NotificationManager.DisplayNotification: el mismo camino que usa el juego
// para su aviso «modsCountChanged» (MainMenu+<CheckModCountChanged>d__80: Task.Delay(2000) y DisplayNotification). Una
// vez por sesión; si algo falla, queda el log.
// When the warning is shown (lesson from 0.7.8 and 0.7.9, fixed in 0.7.10: NotificationManager.ErrorNotification
// at startup, with text tables not yet loaded, hung the game): NEVER in Plugin.Load or at startup. A postfix of
// Memori.Scenes.SceneHandler.AlertOfSceneSetUpComlete (normal method, not async: xref from build 26749277, game 1.11.17, 18-11-2026, called by
// MainMenu+<Load>d__77.MoveNext when done setting up the menu) records the time the first time the state is MainMenu, and
// a custom component shows it 3 s later with NotificationManager.DisplayNotification: the same path the game uses
// for its «modsCountChanged» warning (MainMenu+<CheckModCountChanged>d__80: Task.Delay(2000) and DisplayNotification).
// Once per session; if anything fails, the log remains.
//
// Menú de mods: postfix de TJ.ModListRow.Setup (método real de ~0x1D7 bytes; ModListPanel.Refresh lo llama con call, sin
// copia inlineada, xref de la build 26749277, juego 1.11.17, 18-11-2026). En las filas de las carpetas de módulos de Zero añade al texto de versión y
// autor el estado que midió el núcleo al arrancar: «OK» o «NO: ...» (corto: la fila es estrecha). Las demás filas no se
// tocan. El menú de mods se abre siempre después de Plugin.Load (Nucleo.Iniciar ya ha leído los módulos).
// Autor: ZeroitMods.
// Mod menu: postfix of TJ.ModListRow.Setup (real method of ~0x1D7 bytes; ModListPanel.Refresh calls it with call, without
// an inlined copy, xref from build 26749277, game 1.11.17, 18-11-2026). On the rows of Zero module folders it appends to the version and
// author text the status measured by the core at startup: «OK» or «NO: ...» (short: the row is narrow). Other rows are not
// touched. The mod menu is always opened after Plugin.Load (Nucleo.Iniciar has already read the modules).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: preparación (desde Plugin.Load) y aviso del menú principal
    // Section: setup (from Plugin.Load) and main menu warning
    // -----------------------------------------------------------------------
    public static class AvisoInstalacion
    {
        /// <summary>Problemas encontrados al arrancar (para el log, el aviso y la autoprueba).</summary>
        /// Problems found at startup (for the log, the warning and the self-test).
        internal static List<HallazgoInstalacion> Hallazgos { get; private set; } = new List<HallazgoInstalacion>();
        // Hora (Time.realtimeSinceStartup) a partir de la que se enseña el aviso; < 0 = todavía no está el menú
        // Time (Time.realtimeSinceStartup) from which the warning is shown; < 0 = menu not ready yet
        private static float _horaAviso = -1f;
        private static bool _avisado;
        /// <summary>Segundos de espera tras montar el menú (el juego espera 2 s para su aviso de mods; el nuestro va después).</summary>
        /// Seconds to wait after the menu is set up (the game waits 2 s for its mod warning; ours comes after).
        internal const float EsperaSegundos = 3f;
        /// <summary>true cuando ya no queda nada que enseñar (aviso dado, o sin problemas y el menú ya montado).</summary>
        /// true when there is nothing left to show (warning given, or no problems and the menu already set up).
        internal static bool Hecho => _avisado || (Hallazgos.Count == 0 && _menuVisto);
        private static bool _menuVisto;
        private static bool _alertaVista;

        /// <summary>
        /// Revisa la instalación y deja el resumen en el log. La llama Plugin.CargarInterno justo después de Nucleo.Iniciar y
        /// ANTES del retorno temprano de «no hay ningún módulo» (ese es justo el caso de los módulos mal colocados). Nunca lanza.
        /// Checks the installation and leaves the summary in the log. Called by Plugin.CargarInterno right after Nucleo.Iniciar and
        /// BEFORE the early return of «no modules found» (that is precisely the case of misplaced modules). Never throws.
        /// </summary>
        internal static void Preparar(IEnumerable<string> ilegibles)
        {
            try
            {
                Hallazgos = LogicaInstalacion.Revisar(Nucleo.CarpetaMods, Paths.GameRootPath, Nucleo.Encontrados,
                                                      Nucleo.Real?.Rechazados, ilegibles, Nucleo.LecturaFallida);
                foreach (var h in Hallazgos)
                    if (h.Tipo == ProblemaInstalacion.SinModulos) Plugin.Info("Instalación: " + h);
                    else Plugin.Aviso("Instalación: " + h);
                var aplicados = Nucleo.Real?.Aplicados?.Select(m => m.Id) ?? Enumerable.Empty<string>();
                int problemas = Hallazgos.Count(h => h.Tipo != ProblemaInstalacion.SinModulos);
                Plugin.Info(LogicaInstalacion.LineaListo(Plugin.VERSION, aplicados, problemas));
            }
            catch (Exception e) { Plugin.Aviso("Instalación: no se pudo revisar (" + e.Message + ")."); }
        }

        /// <summary>
        /// Postfix de SceneHandler.AlertOfSceneSetUpComlete(bool silent): la primera vez que el menú principal termina de
        /// montarse, apunta la hora del aviso. No toca la interfaz (eso lo hace el componente, en su Update). Nunca lanza.
        /// Postfix of SceneHandler.AlertOfSceneSetUpComlete(bool silent): the first time the main menu finishes
        /// setting up, records the warning time. Does not touch the UI (that is done by the component, in its Update). Never throws.
        /// </summary>
        public static void TrasAlertaEscena()
        {
            try
            {
                if (_avisado || _horaAviso >= 0f) return;
                bool menu = EstadoEsMenu();
                if (!_alertaVista) { _alertaVista = true; Plugin.Info($"Instalación: primera escena montada (menú principal: {menu})."); }
                if (!menu) return;
                _menuVisto = true;
                if (Hallazgos.Count > 0) _horaAviso = Time.realtimeSinceStartup + EsperaSegundos;
            }
            catch { }
        }

        // Estado de la escena (aparte y sin inlining: si el tipo cambia en una actualización, el fallo sale aquí, dentro del try)
        // Scene state (separate and without inlining: if the type changes in an update, the failure appears here, inside the try)
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool EstadoEsMenu() =>
            Memori.Scenes.SceneHandler.Instance != null &&
            Memori.Scenes.SceneHandler.Instance.CurrentGameState == Memori.Scenes.GameStateEnum.MainMenu;

        /// <summary>Lo llama el componente en cada fotograma: enseña el aviso una vez cuando toca. Nunca lanza.</summary>
        /// Called by the component every frame: shows the warning once when due. Never throws.
        internal static void Tick()
        {
            if (_avisado || _horaAviso < 0f || Time.realtimeSinceStartup < _horaAviso) return;
            // Si el jugador ya entró en una partida, nada en plena carga: se vuelve a programar al volver al menú
            // If the player has already entered a run, nothing during full load: rescheduled when returning to the menu
            bool menu = false;
            try { menu = EstadoEsMenu(); } catch { }
            if (!menu) { _horaAviso = -1f; return; }
            _avisado = true;   // pase lo que pase, una sola vez por sesión
                               // whatever happens, only once per session
            try
            {
                bool es = false;
                try { es = AccionesHeroes.JuegoEnEspanol(); } catch { }
                string texto = LogicaInstalacion.TextoAviso(Hallazgos, Plugin.VERSION, Nucleo.CarpetaMods, es);
                if (texto == null) return;
                if (Mostrar(texto)) Plugin.Info("Instalación: aviso en pantalla enseñado: " + texto);
                else Plugin.Aviso("Instalación: no hay gestor de avisos del juego; el aviso queda solo en el log: " + texto);
            }
            catch (Exception e) { Plugin.Aviso("Instalación: no se pudo enseñar el aviso en pantalla (" + e.Message + "); queda en el log."); }
        }

        // Aviso del juego (el mismo camino que su aviso «modsCountChanged»). Aparte y sin inlining, como AvisarEnPantalla
        // Game warning (the same path as its «modsCountChanged» warning). Separate and without inlining, like AvisarEnPantalla
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool Mostrar(string texto)
        {
            var gestor = Memori.Notifications.NotificationManager.Instance;
            if (gestor == null) return false;
            gestor.DisplayNotification(texto);
            return true;
        }

        // ---------------------------------------------------------------
        // Menú de mods: estado de cada módulo de Zero en su fila
        // Mod menu: status of each Zero module in its row
        // ---------------------------------------------------------------
        /// <summary>Postfix de TJ.ModListRow.Setup: añade el estado medido por el núcleo a las filas de módulos de Zero. Nunca lanza.</summary>
        /// Postfix of TJ.ModListRow.Setup: appends the status measured by the core to Zero module rows. Never throws.
        public static void TrasModListRowSetup(TJ.ModListRow __instance)
        {
            try
            {
                PonerEstadoFila(__instance);
            }
            catch { }
        }

        // Cuerpo del postfix de la fila (aparte y sin inlining: los tipos del juego se resuelven aquí, dentro del try)
        // Body of the row postfix (separate and without inlining: game types are resolved here, inside the try)
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PonerEstadoFila(TJ.ModListRow fila)
        {
            string carpeta = fila?._folder;
            if (string.IsNullOrEmpty(carpeta)) return;
            var m = Nucleo.Encontrados.FirstOrDefault(x => string.Equals(x.NombreCarpeta, carpeta, StringComparison.OrdinalIgnoreCase));
            if (m == null) return;   // no es un módulo de Zero
                                     // not a Zero module
            bool es = false;
            try { es = AccionesHeroes.JuegoEnEspanol(); } catch { }
            string estado = LogicaInstalacion.EstadoFila(m, Nucleo.Real, Plugin.VERSION, es);
            var t = fila.versionAuthorText;
            if (t == null || string.IsNullOrEmpty(estado)) return;
            string sufijo = " · " + estado;
            string actual = t.text ?? "";
            if (actual.EndsWith(sufijo, StringComparison.Ordinal)) return;   // Setup otra vez sobre la misma fila
                                                                             // Setup called again on the same row
            // Sin salto de línea: si no cabe, que se salga por la derecha antes que montarse sobre el nombre
            // No line break: if it doesn't fit, let it overflow to the right rather than overlapping the name
            try { t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; } catch { }
            t.text = actual + sufijo;
        }
    }

    // -----------------------------------------------------------------------
    // Sección: componente que espera al menú para enseñar el aviso
    // Section: component that waits for the menu to show the warning
    // -----------------------------------------------------------------------
    /// <summary>Componente propio (BasePlugin.AddComponent): llama a AvisoInstalacion.Tick y se apaga tras el aviso.</summary>
    /// Custom component (BasePlugin.AddComponent): calls AvisoInstalacion.Tick and shuts itself off after the warning.
    public class VigiaAvisoInstalacion : MonoBehaviour
    {
        public VigiaAvisoInstalacion(IntPtr ptr) : base(ptr) { }

        // Una comprobación de dos números por fotograma hasta que sale el aviso; luego el componente se apaga
        // One check of two numbers per frame until the warning is shown; then the component shuts off
        private void Update()
        {
            AvisoInstalacion.Tick();
            try { if (AvisoInstalacion.Hecho) enabled = false; } catch { }
        }
    }
}
