// Silencio.cs
// Núcleo 0.7.11 (18-11-2026): opción SOLO para pruebas, Diagnostico.SinSonido (false por defecto). Con ella el juego
// arranca y sigue sin ningún sonido: AudioListener.volume = 0 desde la carga del plugin (antes de que suene nada) y un
// componente que lo vuelve a poner a 0 en cada fotograma por si el juego lo cambia. No toca los ajustes de volumen del
// jugador (PlayerPrefs) ni el mezclador de Windows: al quitar la opción todo suena como antes.
// Autor: ZeroitMods.
// Silencio.cs
// Core 0.7.11 (18-11-2026): option FOR TESTS ONLY, Diagnostico.SinSonido (false by default). With it the game
// starts and runs with no sound at all: AudioListener.volume = 0 from the plugin load (before anything plays) and a
// component that resets it to 0 every frame in case the game changes it. Does not touch the player's volume settings
// (PlayerPrefs) or the Windows mixer: when the option is removed everything sounds as before.
// Author: ZeroitMods.

using System;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: silencio para las pruebas automáticas
    // Section: silence for automatic tests
    // -----------------------------------------------------------------------
    public static class Silencio
    {
        /// <summary>Pone el volumen global de Unity a 0 (nunca lanza). true si se pudo.</summary>
        /// Sets Unity's global volume to 0 (never throws). true if successful.
        public static bool Aplicar()
        {
            try
            {
                AudioListener.volume = 0f;
                return true;
            }
            catch (Exception e)
            {
                Plugin.Aviso("Diagnostico.SinSonido: no se pudo poner el volumen a 0 (" + e.Message + ").");
                return false;
            }
        }
    }

    /// <summary>Componente propio (BasePlugin.AddComponent): mantiene AudioListener.volume a 0 mientras viva el juego.</summary>
    /// Custom component (BasePlugin.AddComponent): keeps AudioListener.volume at 0 while the game runs.
    public class VigiaSilencio : MonoBehaviour
    {
        public VigiaSilencio(IntPtr ptr) : base(ptr) { }

        // Un acceso a una propiedad por fotograma: si el juego lo sube, vuelve a 0 en el mismo fotograma
        // One property access per frame: if the game raises it, it goes back to 0 in the same frame
        private void Update()
        {
            try { if (AudioListener.volume != 0f) AudioListener.volume = 0f; } catch { }
        }
    }
}
