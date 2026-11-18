// DiagnosticoMenuMods.cs
// Núcleo 0.7.11 (18-11-2026): prueba del menú de mods SIN jugar (opción Diagnostico.ProbarMenuMods, false por defecto).
// Ya en el menú principal y unos segundos después de la autoprueba: abre el panel de mods (ModListPanel.OpenPanel, lo
// mismo que el botón «Mods»), escribe en el log el nombre y el texto de versión/autor de cada fila (con el estado que
// añade AvisoInstalacion), hace una captura y vuelve al menú principal. No pulsa ningún interruptor ni «Publicar».
// Capturas en BepInEx\config\ZeroNucleo_capturas\. Cada paso va en try/catch; el resultado sale con [diagnostico].
// Autor: ZeroitMods.
// DiagnosticoMenuMods.cs
// Core 0.7.11 (18-11-2026): mod menu test WITHOUT playing (option Diagnostico.ProbarMenuMods, false by default).
// Already in the main menu and a few seconds after the self-test: opens the mod panel (ModListPanel.OpenPanel, the
// same as the «Mods» button), writes to the log the name and version/author text of each row (with the status added
// by AvisoInstalacion), takes a screenshot and returns to the main menu. Does not press any toggle or «Publicar».
// Screenshots in BepInEx\config\ZeroNucleo_capturas\. Each step is in try/catch; the result appears with [diagnostico].
// Author: ZeroitMods.

using System;
using System.IO;
using BepInEx;
using Il2CppInterop.Runtime;
using TJ.MainMenu;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: componente de prueba del menú de mods
    // Section: mod menu test component
    // -----------------------------------------------------------------------
    public class DiagnosticoMenuMods : MonoBehaviour
    {
        public DiagnosticoMenuMods(IntPtr ptr) : base(ptr) { }

        private int _paso;
        private float _siguiente = -1f;
        private ModListPanel _panel;
        private string _carpeta;
        // Espera tras la autoprueba (si la hay) y tope para encontrar el menú
        // Wait after the self-test (if any) and timeout to find the menu
        private const float EsperaInicial = 15f, Tope = 240f;
        private float _inicio = -1f;

        private void Update()
        {
            try
            {
                float ahora = Time.realtimeSinceStartup;
                if (_paso < 0 || ahora < _siguiente) return;
                // Referencia de tiempo: el fin de la autoprueba o, sin autoprueba, el primer fotograma
                // Time reference: the end of the self-test or, without a self-test, the first frame
                if (_inicio < 0f) _inicio = DiagnosticoPantalla.FinAutoprueba >= 0f ? DiagnosticoPantalla.FinAutoprueba : ahora;
                switch (_paso)
                {
                    case 0:   // esperar al menú principal con el panel de mods cargado
                              // wait for the main menu with the mod panel loaded
                        if (DiagnosticoPantalla.FinAutoprueba >= 0f) _inicio = DiagnosticoPantalla.FinAutoprueba;
                        if (ahora < _inicio + EsperaInicial) return;
                        _panel = BuscarPanel();
                        if (_panel == null)
                        {
                            if (ahora > _inicio + Tope) { Plugin.Aviso("[diagnostico] No se encontró el panel de mods (ModListPanel); se abandona la prueba."); _paso = -1; }
                            else _siguiente = ahora + 2f;
                            return;
                        }
                        _carpeta = Path.Combine(Paths.ConfigPath, Plugin.Prefijo + "_capturas");
                        Directory.CreateDirectory(_carpeta);
                        Plugin.Info("[diagnostico] Abriendo el menú de mods (sin tocar nada).");
                        _panel.OpenPanel();
                        Avanzar(4f);
                        break;
                    case 1:
                        Informar();
                        Capturar("menu_mods");
                        Avanzar(2f);
                        break;
                    default:
                        try { _panel.ReturnToMainMenu(); } catch (Exception e) { Plugin.Aviso("[diagnostico] Volver al menú: " + e.Message); }
                        Plugin.Info($"[diagnostico] Menú de mods: prueba terminada. Capturas en {_carpeta}.");
                        _paso = -1;
                        enabled = false;
                        break;
                }
            }
            catch (Exception e)
            {
                Plugin.Error($"[diagnostico] Menú de mods, paso {_paso} FALLO: {e}");
                _paso = -1;
            }
        }

        private void Avanzar(float segundos) { _paso++; _siguiente = Time.realtimeSinceStartup + segundos; }

        // ModListPanel de la escena del menú (también si está desactivado)
        // ModListPanel from the menu scene (also if disabled)
        private static ModListPanel BuscarPanel()
        {
            var todos = Resources.FindObjectsOfTypeAll(Il2CppType.Of<ModListPanel>());
            for (int i = 0; todos != null && i < todos.Length; i++)
            {
                var p = todos[i]?.TryCast<ModListPanel>();
                if (p != null && p.gameObject.scene.IsValid()) return p;
            }
            return null;
        }

        // Una línea por fila: nombre y texto de versión/autor (ahí va el estado del núcleo en las filas de Zero)
        // One line per row: name and version/author text (the core status for Zero rows goes there)
        private void Informar()
        {
            var filas = _panel._spawnedRows;
            int n = filas?.Count ?? 0;
            Plugin.Info($"[diagnostico] Menú de mods: {n} filas.");
            for (int i = 0; i < n; i++)
            {
                var f = filas[i];
                if (f == null) continue;
                string nombre = f.nameText != null ? f.nameText.text : "(sin nombre)";
                string version = f.versionAuthorText != null ? f.versionAuthorText.text : "(sin texto)";
                Plugin.Info($"[diagnostico] Fila {i + 1}: «{nombre}» | «{version}»");
            }
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
