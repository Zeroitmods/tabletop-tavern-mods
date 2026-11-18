// LogicaJinete.cs
// Núcleo 0.7.13 (18-11-2026): la opción Batalla.JineteCaballeria (jinete visible en las caballerías nuevas 902, 923,
// 926, 947, 950, 963 y 967) pasa a venir ENCENDIDA. BepInEx guarda en el .cfg el valor de cada opción y, si el fichero
// ya existe, manda lo escrito en él aunque cambie el valor por defecto: quien tuviera el núcleo 0.7.12 o anterior tiene
// «JineteCaballeria = false» (el valor de serie de entonces) y se quedaría sin jinete. Decisión pura (sin tipos del
// juego ni de BepInEx, para probarla fuera del juego en pruebas\PruebasJinete.cs) de qué valor queda y si hay que
// escribirlo, con una clave marcador (Batalla.JineteCaballeriaRevisado):
//   - sin marcador (núcleo anterior o .cfg nuevo): un «false» se toma por el valor de serie antiguo y pasa a true; se
//     pone el marcador. Un «false» puesto a mano antes de la 0.7.13 no se distingue del de serie: se enciende UNA vez.
//   - con marcador: se respeta lo que haya (si el jugador lo apaga después, sigue apagado).
// Autor: ZeroitMods.
// LogicaJinete.cs
// Core 0.7.13 (18-11-2026): the Batalla.JineteCaballeria setting (visible rider on the new cavalry 902, 923,
// 926, 947, 950, 963 and 967) is now ON by default. BepInEx stores each setting's value in the .cfg and, if the file
// already exists, what is written there wins even when the default changes: anyone who had core 0.7.12 or older has
// «JineteCaballeria = false» (the default back then) and would be left without a rider. Pure decision (no game or
// BepInEx types, so it can be tested outside the game in pruebas\PruebasJinete.cs) of which value stays and whether it
// has to be written, using a marker key (Batalla.JineteCaballeriaRevisado):
//   - no marker (older core or new .cfg): a «false» is taken as the old default and becomes true; the
//     marker is set. A «false» set by hand before 0.7.13 cannot be told apart from the default: it is switched on ONCE.
//   - with the marker: whatever is there is kept (if the player turns it off afterwards, it stays off).
// Author: ZeroitMods.

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: migración de la opción del jinete
    // Section: migration of the rider setting
    // -----------------------------------------------------------------------
    public static class LogicaJinete
    {
        /// <summary>Resultado de la migración: valor final, marcador final y si cambió el valor (para el log).</summary>
        /// Result of the migration: final value, final marker and whether the value changed (for the log).
        public struct Resultado
        {
            public bool Valor;        // valor de Batalla.JineteCaballeria que queda
                                      // value of Batalla.JineteCaballeria that stays
            public bool Marcador;     // valor de Batalla.JineteCaballeriaRevisado que queda (siempre true tras migrar)
                                      // value of Batalla.JineteCaballeriaRevisado that stays (always true after migrating)
            public bool Encendida;    // true = era el false antiguo y se ha encendido ahora
                                      // true = it was the old false and has just been switched on
        }

        /// <summary>
        /// Decide el valor de la opción del jinete a partir del valor leído del .cfg (o el de serie, true, si no estaba)
        /// y del marcador (false si no estaba).
        /// Decides the value of the rider setting from the value read from the .cfg (or the default, true, if it was not there)
        /// and from the marker (false if it was not there).
        /// </summary>
        public static Resultado Migrar(bool valorLeido, bool marcadorLeido)
        {
            // Con el marcador ya puesto, la opción es la que eligió el jugador (o la que dejó la migración): no se toca
            // With the marker already set, the setting is the one the player chose (or the one the migration left): it is not touched
            if (marcadorLeido) return new Resultado { Valor = valorLeido, Marcador = true, Encendida = false };
            // Sin marcador: un false es el valor de serie de los núcleos anteriores; pasa al nuevo (true)
            // No marker: a false is the default of the older cores; it becomes the new one (true)
            return new Resultado { Valor = true, Marcador = true, Encendida = !valorLeido };
        }
    }
}
