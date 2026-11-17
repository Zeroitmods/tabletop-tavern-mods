// LogicaVersion.cs
// Comparación pura (sin tipos del juego ni de Unity) de la versión del juego instalada (Application.version) con la
// versión con la que se probó el núcleo (Plugin.VersionJuegoProbada), para poder probarla fuera del juego (proyecto
// pruebas\, PruebasRevisionExterna.cs). Revisión externa del 18-10-2026: la actualización grande del 27-10 puede cambiar
// firmas y nombres; el núcleo solo avisa en el log, no desactiva nada por su cuenta.
// Autor: ZeroitMods.
// LogicaVersion.cs
// Pure comparison (no game or Unity types) of the installed game version (Application.version) with the
// version the core was tested with (Plugin.VersionJuegoProbada), so it can be tested outside the game (project
// pruebas\, PruebasRevisionExterna.cs). External review of 18-10-2026: the big update of 27-10 may change
// signatures and names; the core only warns in the log, it does not disable anything on its own.
// Author: ZeroitMods.

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: aviso de versión del juego
    // Section: game version warning
    // -----------------------------------------------------------------------
    public static class LogicaVersion
    {
        /// <summary>
        /// Texto del aviso si <paramref name="actual"/> no es <paramref name="probada"/> (se comparan sin espacios a los
        /// lados y distinguiendo mayúsculas); null si coinciden. Una versión actual vacía o null también avisa (no se sabe
        /// con qué versión se está jugando).
        /// Warning text if <paramref name="actual"/> is not <paramref name="probada"/> (compared without surrounding
        /// spaces and case-sensitively); null if they match. An empty or null current version also warns (it is not known
        /// which version is being played).
        /// </summary>
        public static string Aviso(string probada, string actual)
        {
            string p = (probada ?? "").Trim();
            string a = (actual ?? "").Trim();
            if (a.Length > 0 && a == p) return null;
            string cual = a.Length == 0 ? "una versión desconocida" : "la versión " + a;
            return $"El juego es {cual} y el núcleo se probó con la {p}: NO está probado con esta versión. Revisa en este log " +
                   "los avisos de «Parche no aplicado» y «Parche crítico FALLIDO» (y que la línea «Núcleo ... listo» no traiga fallidos) " +
                   "antes de jugar; si los hay, espera a una versión del núcleo para este juego. No se ha desactivado nada.";
        }
    }
}
