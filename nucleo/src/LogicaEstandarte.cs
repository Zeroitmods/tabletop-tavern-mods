// LogicaEstandarte.cs
// Núcleo 0.7.7 (03-11-2026): parte pura (sin Unity ni tipos del juego) de los iconos del estandarte (Estandarte.cs):
// declaración de iconos de un módulo (efectos.iconosEstandarte de zero_modulo.json), validación de ids y rutas, códigos
// que se guardan en el registro del núcleo («juego:<textura>» o «<módulo>:<id>»), claves de texto de los iconos de serie
// y la regla de qué banderas llevan el icono elegido. Va aparte para probarlo fuera del juego (pruebas\PruebasEstandarte.cs).
// Autor: ZeroitMods.
// LogicaEstandarte.cs
// Core 0.7.7 (03-11-2026): pure part (no Unity or game types) of the banner icons (Estandarte.cs):
// declaration of a module's icons (efectos.iconosEstandarte in zero_modulo.json), validation of ids and paths, codes
// saved in the core registry («juego:<textura>» or «<módulo>:<id>»), text keys for the stock icons
// and the rule for which flags carry the chosen icon. Kept separate to test it outside the game (pruebas\PruebasEstandarte.cs).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: datos
    // Section: data
    // -----------------------------------------------------------------------
    /// <summary>Un icono tal como lo declara un módulo en zero_modulo.json (efectos.iconosEstandarte).</summary>
    /// An icon as declared by a module in zero_modulo.json (efectos.iconosEstandarte).
    public class IconoEstandarteDecl
    {
        public string Id { get; set; }          // id dentro del módulo (minúsculas, cifras y «_»)
                                                // id within the module (lowercase, digits and "_")
        public string Fichero { get; set; }     // PNG relativo a la carpeta del módulo
                                                // PNG relative to the module folder
        public string Clave { get; set; }       // clave de texto (localization_overrides) con el nombre visible
                                                // text key (localization_overrides) with the display name
        public string NombreEs { get; set; }    // respaldo si falta la clave
                                                // fallback if the key is missing
        public string NombreEn { get; set; }
    }

    /// <summary>Un icono ya resuelto: de qué módulo es y su ruta completa.</summary>
    /// An already resolved icon: which module it belongs to and its full path.
    public class IconoEstandarte
    {
        public string Modulo;
        public string Id;
        public string Ruta;
        public string Clave;
        public string NombreEs;
        public string NombreEn;
        public string Codigo => LogicaEstandarte.Codigo(Modulo, Id);
    }

    // -----------------------------------------------------------------------
    // Sección: reglas
    // Section: rules
    // -----------------------------------------------------------------------
    public static class LogicaEstandarte
    {
        /// <summary>Iconos como mucho por módulo (los que pasen se ignoran con aviso).</summary>
        /// Maximum icons per module (any beyond that are ignored with a warning).
        public const int MaximoPorModulo = 64;
        /// <summary>Prefijo de los códigos de los iconos de serie (banderas de escuadra del juego).</summary>
        /// Prefix of the codes of the built-in icons (the game's squad flags).
        public const string PrefijoJuego = "juego:";

        private static readonly Regex _id = new Regex("^[a-z0-9_]{1,40}$", RegexOptions.CultureInvariant);

        /// <summary>Código que se guarda en el registro para un icono de módulo.</summary>
        /// Code saved in the registry for a module icon.
        public static string Codigo(string modulo, string id) => modulo + ":" + id;

        /// <summary>Código que se guarda en el registro para un icono de serie (por nombre de textura).</summary>
        /// Code saved in the registry for a built-in icon (by texture name).
        public static string CodigoJuego(string textura) => PrefijoJuego + textura;

        /// <summary>¿Es un id de icono válido? (minúsculas, cifras y «_», 1-40).</summary>
        /// Is it a valid icon id? (lowercase, digits and "_", 1-40).
        public static bool IdValido(string id) => !string.IsNullOrEmpty(id) && _id.IsMatch(id);

        /// <summary>
        /// Ruta completa de un fichero de icono dentro de la carpeta del módulo, o null (con el motivo) si no vale: vacía,
        /// absoluta, con «..», que no sea .png o que se salga de la carpeta.
        /// Full path of an icon file inside the module folder, or null (with the reason) if it is not valid: empty,
        /// absolute, containing "..", not a .png, or escaping the folder.
        /// </summary>
        public static string RutaSegura(string carpeta, string fichero, out string motivo)
        {
            motivo = null;
            if (string.IsNullOrWhiteSpace(fichero)) { motivo = "sin fichero"; return null; }
            string f = fichero.Replace('\\', '/');
            if (Path.IsPathRooted(fichero) || f.StartsWith("/") || f.Contains(":")) { motivo = "ruta absoluta"; return null; }
            if (f.Split('/').Any(p => p == "..")) { motivo = "ruta con «..»"; return null; }
            if (!f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) { motivo = "no es .png"; return null; }
            string baseCompleta = Path.GetFullPath(carpeta ?? ".");
            string completa = Path.GetFullPath(Path.Combine(baseCompleta, f.Replace('/', Path.DirectorySeparatorChar)));
            string conBarra = baseCompleta.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!completa.StartsWith(conBarra, StringComparison.OrdinalIgnoreCase)) { motivo = "se sale de la carpeta del módulo"; return null; }
            return completa;
        }

        /// <summary>
        /// Resuelve los iconos que declara un módulo: valida id y fichero, quita repetidos y recorta al máximo. Los
        /// descartes van a <paramref name="avisos"/> con el id del módulo. No comprueba que el PNG exista (lo hace la carga).
        /// Resolves the icons that a module declares: validates id and file, removes duplicates and trims to the maximum. The
        /// discarded ones go to <paramref name="avisos"/> with the module id. It does not check that the PNG exists (loading does that).
        /// </summary>
        public static List<IconoEstandarte> Resolver(string modulo, string carpeta, IEnumerable<IconoEstandarteDecl> decl, List<string> avisos)
        {
            var res = new List<IconoEstandarte>();
            if (decl == null) return res;
            // Revisión externa 0.7.7, hallazgo 9: «juego» es el prefijo de los iconos de serie
            // External review 0.7.7, finding 9: «juego» is the prefix of the stock icons
            if (string.Equals(modulo, PrefijoJuego.TrimEnd(':'), StringComparison.OrdinalIgnoreCase))
            {
                avisos?.Add($"{modulo}: un módulo no puede llamarse «juego» (choca con los iconos de serie); sus iconos se ignoran");
                return res;
            }
            var vistos = new HashSet<string>(StringComparer.Ordinal);
            foreach (var d in decl)
            {
                if (d == null) continue;
                if (!IdValido(d.Id)) { avisos?.Add($"{modulo}: icono de estandarte con id no válido «{d.Id}» (minúsculas, cifras y _); se ignora"); continue; }
                if (!vistos.Add(d.Id)) { avisos?.Add($"{modulo}: icono de estandarte «{d.Id}» repetido; se usa el primero"); continue; }
                string ruta = RutaSegura(carpeta, d.Fichero, out string motivo);
                if (ruta == null) { avisos?.Add($"{modulo}: icono de estandarte «{d.Id}»: {motivo}; se ignora"); continue; }
                if (res.Count >= MaximoPorModulo) { avisos?.Add($"{modulo}: más de {MaximoPorModulo} iconos de estandarte; se ignoran los demás"); break; }
                res.Add(new IconoEstandarte { Modulo = modulo, Id = d.Id, Ruta = ruta, Clave = d.Clave, NombreEs = d.NombreEs, NombreEn = d.NombreEn });
            }
            return res;
        }

        /// <summary>
        /// Nombre legible de una textura de bandera del juego: sin «Outline», sin espacios ni signos y con mayúscula
        /// inicial («Spears Outline» → «Spears», «raptorsOutline» → «Raptors»).
        /// Readable name of a game flag texture: without "Outline", without spaces or symbols and with an initial
        /// capital letter ("Spears Outline" → "Spears", "raptorsOutline" → "Raptors").
        /// </summary>
        public static string NombreLegible(string textura)
        {
            if (string.IsNullOrEmpty(textura)) return "?";
            string s = Regex.Replace(textura, "outline", "", RegexOptions.IgnoreCase);
            var sb = new StringBuilder();
            foreach (char c in s) if (char.IsLetterOrDigit(c)) sb.Append(c);
            if (sb.Length == 0) return textura;
            sb[0] = char.ToUpperInvariant(sb[0]);
            return sb.ToString();
        }

        /// <summary>Clave de texto del nombre de un icono de serie («zeroIconoJuego» + nombre legible).</summary>
        /// Text key for the name of a built-in icon ("zeroIconoJuego" + readable name).
        public static string ClaveTexturaJuego(string textura) => "zeroIconoJuego" + NombreLegible(textura);

        /// <summary>
        /// Código elegido que se puede usar ahora: el guardado si está entre los disponibles; si no (módulo apagado, icono
        /// quitado), null = el icono de serie. La elección no se borra: vuelve al reactivar el módulo.
        /// Chosen code that can be used now: the saved one if it is among the available ones; otherwise (module off, icon
        /// removed), null = the built-in icon. The choice is not deleted: it comes back when the module is re-enabled.
        /// </summary>
        public static string ElegidoDisponible(string guardado, ICollection<string> disponibles) =>
            !string.IsNullOrEmpty(guardado) && disponibles != null && disponibles.Contains(guardado) ? guardado : null;

        /// <summary>
        /// ¿Lleva esta bandera de escuadra el icono elegido? Solo las del jugador (SquadId &gt; 0, como en
        /// SquadFollower.PlayerSquads del juego y en «Impertérritos») y solo si hay un icono elegido y disponible.
        /// Does this squad flag carry the chosen icon? Only the player's (SquadId &gt; 0, as in
        /// SquadFollower.PlayerSquads in the game and in "Impertérritos") and only if there is a chosen, available icon.
        /// </summary>
        public static bool BanderaLlevaIcono(int squadId, bool hayIconoElegido) => hayIconoElegido && squadId > 0;
    }
}
