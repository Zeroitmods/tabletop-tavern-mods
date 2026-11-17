// LogicaSonidos.cs
// Lógica pura del sonido láser de las unidades nuevas (sin tipos de Unity ni del juego: se compila también en pruebas\).
// LogicaSonidos.cs
// Pure logic for the laser sound of the new units (no Unity or game types: it is also compiled in pruebas\).
//
// Qué decide este fichero:
//   - Qué tipo de sonido de disparo lleva cada unidad, leído del campo «sonidos» de su entrada en el manifiesto
//     (facciones_manifiesto.json, lo escribe generar_mod_facciones.py): «base» (el de su unidad base, sin tocar),
//     «laser» (láser seco de la facción A), «blaster» (disparo de energía agudo de la facción B) o «pesado» (disparo de energía pesado: arma pesada fija, andador pesado...).
//     «propios_guardia» es el valor antiguo de la 900 y vale como «laser» (manifiestos instalados antes del 18-10-2026).
//   - Con qué prefijo se buscan sus WAV en la carpeta audio\ del módulo (laser_disparo_*, blaster_disparo_*,
//     pesado_disparo_*; el impacto es común: laser_impacto_*).
//   - Qué clip de serie del juego se presta si no hay WAV propios (hechizos de héroe que suenan a energía).
//   - Qué capa se usa al final: 1) láser propio desde WAV, 2) clip prestado del juego, 3) el de la unidad base.
// What this file decides:
//   - Which type of firing sound each unit uses, read from the «sonidos» field of its entry in the manifest
//     (facciones_manifiesto.json, written by generar_mod_facciones.py): «base» (that of its base unit, untouched),
//     «laser» (dry laser of faction A), «blaster» (sharp energy shot of faction B) or «pesado» (heavy energy shot: fixed heavy weapon, heavy walker...).
//     «propios_guardia» is the old value of 900 and counts as «laser» (manifests installed before 18-10-2026).
//   - Which prefix is used to look for its WAVs in the module's audio\ folder (laser_disparo_*, blaster_disparo_*,
//     pesado_disparo_*; the impact is shared: laser_impacto_*).
//   - Which stock game clip is borrowed if there are no custom WAVs (hero spells that sound like energy).
//   - Which layer is used in the end: 1) custom laser from WAV, 2) clip borrowed from the game, 3) that of the base unit.

using System;
using System.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: tipos de sonido y capas
    // Section: sound types and layers
    // -----------------------------------------------------------------------
    /// <summary>Tipo de sonido de disparo de una unidad nueva.</summary>
    /// Firing sound type of a new unit.
    public enum TipoSonido { Base, Laser, Blaster, Pesado }

    /// <summary>De dónde sale el sonido de un disparo: WAV propio, clip prestado del juego o el de la unidad base.</summary>
    /// Where the sound of a shot comes from: custom WAV, borrowed game clip or that of the base unit.
    public enum CapaSonido { Propio, Prestado, Base }

    public static class LogicaSonidos
    {
        // Valores del campo «sonidos» del manifiesto
        // Values of the manifest's "sonidos" field
        public const string ValorBase = "base";
        public const string ValorLaser = "laser";
        public const string ValorBlaster = "blaster";
        public const string ValorPesado = "pesado";
        public const string ValorAntiguoGuardia = "propios_guardia";   // alias de «laser» (manifiesto anterior al 18-10-2026)
                                                                       // alias of "laser" (manifest prior to 18-10-2026)

        // Prefijo común de los impactos (un solo juego de WAV para todas las armas de energía)
        // Common prefix for impacts (a single set of WAVs for all energy weapons)
        public const string PrefijoImpacto = "laser_impacto_";

        // ---------------------------------------------------------------
        // Lectura del campo del manifiesto
        // Reading the manifest field
        // ---------------------------------------------------------------
        /// <summary>Tipo de sonido según el campo «sonidos» del manifiesto. Vacío o desconocido = Base (sin tocar).</summary>
        /// Sound type according to the manifest's "sonidos" field. Empty or unknown = Base (untouched).
        public static TipoSonido DeManifiesto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return TipoSonido.Base;
            switch (valor.Trim().ToLowerInvariant())
            {
                case ValorLaser:
                case ValorAntiguoGuardia: return TipoSonido.Laser;
                case ValorBlaster: return TipoSonido.Blaster;
                case ValorPesado: return TipoSonido.Pesado;
                default: return TipoSonido.Base;
            }
        }

        /// <summary>Nombre corto del tipo para el log («laser», «blaster»...).</summary>
        /// Short name of the type for the log ("laser", "blaster"...).
        public static string Nombre(TipoSonido t)
        {
            switch (t)
            {
                case TipoSonido.Laser: return ValorLaser;
                case TipoSonido.Blaster: return ValorBlaster;
                case TipoSonido.Pesado: return ValorPesado;
                default: return ValorBase;
            }
        }

        // ---------------------------------------------------------------
        // WAV propios y clips prestados por tipo
        // Custom WAVs and borrowed clips by type
        // ---------------------------------------------------------------
        /// <summary>Prefijo de los WAV de disparo de un tipo (null para Base: no lleva WAV).</summary>
        /// Prefix of the firing WAVs of a type (null for Base: it has no WAVs).
        public static string PrefijoDisparo(TipoSonido t)
        {
            switch (t)
            {
                case TipoSonido.Laser: return "laser_disparo_";
                case TipoSonido.Blaster: return "blaster_disparo_";
                case TipoSonido.Pesado: return "pesado_disparo_";
                default: return null;
            }
        }

        /// <summary>Tipos con WAV propio, en el orden en que se cargan.</summary>
        /// Types with custom WAVs, in the order they are loaded.
        public static readonly TipoSonido[] TiposConWav = { TipoSonido.Laser, TipoSonido.Blaster, TipoSonido.Pesado };

        // Clips de serie que se prestan (nombres exactos de AudioClip del juego; notas internas (no publicadas) §3):
        // fusiles y carabinas (láser y energía) → «Smite Charge», el barrido descendente más parecido a un «pew»;
        // armas pesadas → «LightningStrike» (arco eléctrico largo), y si no está, «Smite Charge».
        // Stock clips that are borrowed (exact AudioClip names from the game; internal (unpublished) notes §3):
        // rifles and carbines (laser and energy) → «Smite Charge», the descending sweep closest to a «pew»;
        // heavy weapons → «LightningStrike» (long electric arc), and if it is not available, «Smite Charge».
        private static readonly string[] _prestadosFusil = { "Smite Charge" };
        private static readonly string[] _prestadosPesado = { "LightningStrike", "Smite Charge" };

        /// <summary>Nombres de clips del juego que se pueden prestar a un tipo, por orden de preferencia (vacío para Base).</summary>
        /// Names of game clips that can be borrowed for a type, in order of preference (empty for Base).
        public static IReadOnlyList<string> Prestados(TipoSonido t)
        {
            switch (t)
            {
                case TipoSonido.Laser:
                case TipoSonido.Blaster: return _prestadosFusil;
                case TipoSonido.Pesado: return _prestadosPesado;
                default: return Array.Empty<string>();
            }
        }

        /// <summary>Todos los nombres de clips prestables (para buscarlos de una vez en el juego).</summary>
        /// All the names of borrowable clips (to look them up in the game at once).
        public static IEnumerable<string> TodosLosPrestados()
        {
            var vistos = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in TiposConWav)
                foreach (var n in Prestados(t))
                    if (vistos.Add(n)) yield return n;
        }

        /// <summary>
        /// Primer nombre de la lista de preferencia de <paramref name="t"/> que está entre los cargados (null si ninguno).
        /// First name in the preference list of <paramref name="t"/> that is among the loaded ones (null if none).
        /// </summary>
        public static string ElegirPrestado(TipoSonido t, ICollection<string> cargados)
        {
            if (cargados == null) return null;
            foreach (var n in Prestados(t))
                if (cargados.Contains(n)) return n;
            return null;
        }

        // ---------------------------------------------------------------
        // Elección de capa
        // Layer choice
        // ---------------------------------------------------------------
        /// <summary>
        /// Capa del disparo: WAV propio si hay clips propios de su tipo, si no un clip prestado del juego si está cargado,
        /// y si no el de la unidad base. Un tipo Base siempre es Base.
        /// Shot layer: own WAV if there are own clips of its type, otherwise a borrowed clip from the game if it is loaded,
        /// and otherwise the base unit's. A Base type is always Base.
        /// </summary>
        public static CapaSonido ElegirCapa(TipoSonido t, int clipsPropios, bool hayPrestado)
        {
            if (t == TipoSonido.Base) return CapaSonido.Base;
            if (clipsPropios > 0) return CapaSonido.Propio;
            return hayPrestado ? CapaSonido.Prestado : CapaSonido.Base;
        }

        /// <summary>Texto de la capa para el log: «láser propio desde WAV», «prestado: Smite Charge» o «base».</summary>
        /// Layer text for the log: «láser propio desde WAV», «prestado: Smite Charge» or «base».
        public static string DescribirCapa(CapaSonido capa, string prestado = null)
        {
            switch (capa)
            {
                case CapaSonido.Propio: return "láser propio desde WAV";
                case CapaSonido.Prestado: return "prestado: " + (prestado ?? "?");
                default: return "base";
            }
        }

        /// <summary>
        /// Migración única de la opción Audio.SonidoPropio (18-10-2026): hasta la 0.7.2 venía en false porque se creía que el
        /// sonido propio no podía funcionar en Unity 6. Si la configuración es anterior (revisión menor que 1) y está en
        /// false, pasa a true una sola vez; después manda lo que elija el usuario. Devuelve el valor que debe quedar.
        /// One-time migration of the Audio.SonidoPropio option (2026-10-18): up to 0.7.2 it defaulted to false because it was believed that custom
        /// sound could not work in Unity 6. If the configuration is older (review lower than 1) and is set to
        /// false, it switches to true just once; after that, whatever the user chooses takes precedence. Returns the value that should remain.
        /// </summary>
        public static bool MigrarSonidoPropio(bool valorActual, int revision, out bool migrado)
        {
            migrado = revision < 1 && !valorActual;
            return migrado || valorActual;
        }

        public const int RevisionSonidoActual = 1;
    }
}
