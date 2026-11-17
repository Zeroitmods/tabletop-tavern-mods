// ArmeriaObjetos.cs
// Lista de gear ampliada con los objetos nuevos 7xx para las pantallas que enseñan «todo el gear» (0.7.2).
// El juego saca esa lista de GearData.GetGearIDs(), que recorre el enum cerrado GearID (40 objetos de serie hasta el juego
// 1.10.17, 56 en la 1.11.1,
// ECS/Components/GearData.cs:64-74): la armería de la preparación de partida (WarbandGearArmory.Build) nunca ve un 7xx.
// El plugin amplía esa lista SOLO mientras dura una pantalla concreta (contexto con contador): el logro de colección
// (SaveDataHandler.EvaluateGearCollection), el contador del menú, la plantilla de gear_overrides.json y las herramientas
// de desarrollo siguen viendo solo los de serie.
// Aquí va la parte pura (sin tipos del juego, se prueba en pruebas\): el contador del contexto y la unión de ids.
// Autor: ZeroitMods.
// ArmeriaObjetos.cs
// Extended gear list with the new 7xx new gear for the screens that show "all the gear" (0.7.2).
// The game gets that list from GearData.GetGearIDs(), which iterates over the closed GearID enum (40 base items up to game
// 1.10.17, 56 in 1.11.1,
// ECS/Components/GearData.cs:64-74): the armory on the run setup screen (WarbandGearArmory.Build) never sees a 7xx.
// The plugin extends that list ONLY for the duration of a specific screen (context with a counter): the Collection achievement
// (SaveDataHandler.EvaluateGearCollection), the menu counter, the gear_overrides.json template and the development
// tools still see only the base ones.
// Here is the pure part (no game types, tested in pruebas\): the context counter and the union of ids.
// Author: ZeroitMods.

using System.Collections.Generic;
using System.Linq;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: contexto «lista de gear ampliada» y unión de ids
    // Section: "extended gear list" context and union of ids
    // -----------------------------------------------------------------------
    public static class ArmeriaObjetos
    {
        // Pantallas abiertas que piden la lista ampliada (solo hilo principal: lo llaman parches de la interfaz)
        // Open screens that request the extended list (main thread only: called by the UI patches)
        private static int _abiertos;

        /// <summary>¿Hay ahora alguna pantalla que pida la lista de gear ampliada?</summary>
        /// Is there currently any screen requesting the extended gear list?
        public static bool Ampliando => _abiertos > 0;

        /// <summary>Entra en el contexto (prefix del método de la pantalla). Llamar SIEMPRE emparejado con <see cref="Salir"/>.</summary>
        /// Enters the context (prefix of the screen method). ALWAYS call paired with <see cref="Salir"/>.
        public static void Entrar() => _abiertos++;

        /// <summary>Sale del contexto (finalizer del método de la pantalla: se ejecuta también si el juego lanza una excepción).</summary>
        /// Exits the context (finalizer of the screen method: it also runs if the game throws an exception).
        public static void Salir() { if (_abiertos > 0) _abiertos--; }

        /// <summary>
        /// 0.7.2: suspende el contexto (el contador pasa a 0) y devuelve el valor que tenía, para volver a ponerlo con
        /// <see cref="Reanudar"/>. Lo usa el logro de objetos (LogrosFiltrados.cs), que debe contar siempre solo los de serie.
        /// 0.7.2: suspends the context (the counter goes to 0) and returns the value it had, so it can be restored with
        /// <see cref="Reanudar"/>. Used by the gear achievement (LogrosFiltrados.cs), which must always count only the base ones.
        /// </summary>
        public static int Suspender() { int antes = _abiertos; _abiertos = 0; return antes; }

        /// <summary>0.7.2: vuelve a poner el contador que devolvió <see cref="Suspender"/> (nunca negativo).</summary>
        /// 0.7.2: restores the counter returned by <see cref="Suspender"/> (never negative).
        public static void Reanudar(int antes) => _abiertos = antes < 0 ? 0 : antes;

        /// <summary>
        /// Vuelve a cero el contador. Lo usan las pruebas y, desde la revisión de la 0.7.2, el cambio de escena
        /// (Objetos.TrasOnGameStateChanged) como red por si un finalizer no llegara a correr.
        /// Resets the counter to zero. Used by the tests and, since the 0.7.2 review, by the scene change
        /// (Objetos.TrasOnGameStateChanged) as a safety net in case a finalizer never runs.
        /// </summary>
        internal static void Reiniciar() => _abiertos = 0;

        /// <summary>
        /// Lista de serie seguida de los ids nuevos que aún no estén en ella (en el orden dado y sin repetir). Al ir al final,
        /// el OrderByDescending estable del juego los deja al final de su rareza. Devuelve null si no hay nada que añadir.
        /// Base list followed by the new ids that are not yet in it (in the given order and without repeats). Since they go at the end,
        /// the game's stable OrderByDescending leaves them at the end of their rarity. Returns null if there is nothing to add.
        /// </summary>
        public static int[] Unir(IReadOnlyList<int> deSerie, IEnumerable<int> nuevos)
        {
            var ya = new HashSet<int>(deSerie ?? new int[0]);
            var extra = new List<int>();
            foreach (int id in nuevos ?? Enumerable.Empty<int>())
                if (id > 0 && ya.Add(id)) extra.Add(id);
            if (extra.Count == 0) return null;
            var r = new int[(deSerie?.Count ?? 0) + extra.Count];
            for (int i = 0; i < (deSerie?.Count ?? 0); i++) r[i] = deSerie[i];
            for (int i = 0; i < extra.Count; i++) r[(deSerie?.Count ?? 0) + i] = extra[i];
            return r;
        }
    }
}
