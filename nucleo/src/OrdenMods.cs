// OrdenMods.cs
// Lectura y escritura del orden de mods del juego (Mods\modlist.json) sin llamar al juego, para que el núcleo sepa en
// Plugin.Load (antes de TabletopTavernData.Awake) qué módulos están activos, y sincronización de los módulos que deja un
// gestor de mods (Thunderstore / r2modman) dentro de BepInEx\plugins\<Autor-Paquete>\ hacia la carpeta Mods del juego.
// OrdenMods.cs
// Reading and writing the game's mod order (Mods\modlist.json) without calling the game, so the core knows in
// Plugin.Load (before TabletopTavernData.Awake) which modules are active, and synchronizing the modules that a
// mod manager (Thunderstore / r2modman) leaves inside BepInEx\plugins\<Autor-Paquete>\ into the game's Mods folder.
//
// Por qué sincronizar y no parchear el cargador: el juego solo busca mods en UNA carpeta (ModLoadOrder.ModsRootPath) y
// modlist.json guarda solo el NOMBRE de cada carpeta, que resuelve con Path.Combine(Mods, nombre). Una carpeta fuera de
// Mods no podría activarse ni desactivarse desde el menú de mods del juego. Copiándola a Mods, el menú funciona igual
// que con un mod puesto a mano.
// Why synchronize instead of patching the loader: the game only looks for mods in ONE folder (ModLoadOrder.ModsRootPath) and
// modlist.json stores only the NAME of each folder, which it resolves with Path.Combine(Mods, nombre). A folder outside
// Mods could not be enabled or disabled from the game's mod menu. By copying it to Mods, the menu works the same
// as with a mod placed by hand.
//
// Réplica de ModLoadOrder.GetEnabledModFolderPathsInOrder (Modding/ModLoadOrder.cs del juego): primero las entradas de
// modlist.json activas y que existen, en su orden; después las carpetas de Mods que no están en la lista (instaladas
// nuevas), en orden alfabético y ACTIVAS por defecto. Las carpetas que empiezan por «_» no son mods. Hace falta mod.json.
// Política de actualización de una copia sincronizada (desde la 0.7.0, revisión de la sincronización): el marcador guarda la
// huella SHA-256 de cada fichero tal como se copió. Al actualizar, lo que el núcleo copió y el paquete ya no trae se aparta a
// Mods\_ZeroRetirados\<módulo>-<fecha>-actualizacion\ (no se queda cargándose); lo editado a mano en la copia se respalda ahí
// mismo, con aviso en el log, antes de poner la versión nueva; lo que añadió el usuario y el paquete no trae se deja. Nunca
// se borra nada, con UNA excepción (revisión del 18-10, L-25): al retirar una copia cuyo origen ya no está, si en
// _ZeroRetirados ya hay una copia retirada de ese módulo con la misma huella (contenido idéntico byte a byte), la de Mods
// se borra en vez de duplicarse; su contenido sigue guardado en la retirada igual. Mientras el paquete no cambie, las
// ediciones locales se conservan (la copia no se toca).
// .NET puro (System.IO y System.Text.Json) para poder probarlo fuera del juego (proyecto pruebas\).
// Autor: ZeroitMods.
// Replica of ModLoadOrder.GetEnabledModFolderPathsInOrder (Modding/ModLoadOrder.cs in the game): first the entries of
// modlist.json that are active and exist, in their order; then the Mods folders that are not in the list (newly installed),
// in alphabetical order and ACTIVE by default. Folders starting with «_» are not mods. mod.json is required.
// Update policy for a synced copy (since 0.7.0, sync review): the marker stores the
// SHA-256 hash of each file as it was copied. On update, what the core copied and the package no longer ships is moved aside to
// Mods\_ZeroRetirados\<módulo>-<fecha>-actualizacion\ (so it stops being loaded); what was hand-edited in the copy is backed up there
// as well, with a warning in the log, before the new version is put in place; what the user added and the package does not ship is left alone. Nothing
// is ever deleted, with ONE exception (review of 18-10, L-25): when retiring a copy whose source no longer exists, if
// _ZeroRetirados already holds a retired copy of that module with the same hash (byte-for-byte identical content), the one in Mods
// is deleted instead of being duplicated; its content stays stored in the retired copy all the same. As long as the package does not change, local
// edits are kept (the copy is not touched).
// Pure .NET (System.IO and System.Text.Json) so it can be tested outside the game (pruebas\ project).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: modlist.json (orden y estado activo de los mods del juego)
    // Section: modlist.json (order and active state of the game's mods)
    // -----------------------------------------------------------------------
    public static class OrdenMods
    {
        public const string ManifiestoMod = "mod.json";
        public const string ListaMods = "modlist.json";

        /// <summary>Una entrada de modlist.json.</summary>
        /// An entry of modlist.json.
        public sealed class Entrada
        {
            public string Folder;
            public bool Enabled;
        }

        /// <summary>Lo mismo que ModLoadOrder.IsRealModFolder: las carpetas con «_» delante están reservadas.</summary>
        /// Same as ModLoadOrder.IsRealModFolder: folders with a leading «_» are reserved.
        public static bool EsCarpetaDeMod(string nombre) => !string.IsNullOrEmpty(nombre) && !nombre.StartsWith("_");

        /// <summary>
        /// Entradas de modlist.json en su orden (lista vacía si no existe; null y motivo si está roto). Se lee igual de
        /// estricto que el juego (JsonUtility no acepta comentarios ni comas finales): si el juego no puede leerla, sigue
        /// como si no hubiera lista y activa todas las carpetas de mods; el núcleo debe deducir lo mismo, no otra cosa.
        /// Entries of modlist.json in order (empty list if it does not exist; null and reason if it is broken). It is read as
        /// strictly as the game does (JsonUtility accepts no comments or trailing commas): if the game cannot read it, it continues
        /// as if there were no list and enables all mod folders; the core must deduce the same, nothing else.
        /// </summary>
        public static List<Entrada> LeerLista(string modsRoot, out string motivo)
        {
            motivo = null;
            var r = new List<Entrada>();
            string ruta = Path.Combine(modsRoot ?? "", ListaMods);
            if (!File.Exists(ruta)) return r;
            try
            {
                // JSON estricto (opciones por defecto: sin comentarios ni comas finales), como JsonUtility.FromJson del juego
                // Strict JSON (default options: no comments or trailing commas), like the game's JsonUtility.FromJson
                using var doc = JsonDocument.Parse(File.ReadAllText(ruta), new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
                if (!doc.RootElement.TryGetProperty("loadOrder", out var lista) || lista.ValueKind != JsonValueKind.Array) return r;
                foreach (var e in lista.EnumerateArray())
                {
                    string folder = e.TryGetProperty("folder", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString() : null;
                    bool enabled = e.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.True;
                    if (string.IsNullOrWhiteSpace(folder)) continue;
                    r.Add(new Entrada { Folder = folder, Enabled = enabled });
                }
                return r;
            }
            catch (Exception e)
            {
                motivo = $"modlist.json ilegible ({e.GetType().Name}: {e.Message})";
                return null;
            }
        }

        /// <summary>Nombres de las carpetas de mods activas, en el orden de carga del juego (réplica de ModLoadOrder).</summary>
        /// Names of the active mod folders, in the game's load order (replica of ModLoadOrder).
        public static List<string> CarpetasActivas(string modsRoot)
        {
            var r = new List<string>();
            if (string.IsNullOrEmpty(modsRoot) || !Directory.Exists(modsRoot)) return r;
            var conocidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Con modlist.json roto el juego avisa y sigue como si no hubiera lista: aquí igual
            // With a broken modlist.json the game warns and continues as if there were no list: same here
            foreach (var e in LeerLista(modsRoot, out _) ?? new List<Entrada>())
            {
                conocidas.Add(e.Folder);
                if (e.Enabled && Directory.Exists(Path.Combine(modsRoot, e.Folder))) r.Add(e.Folder);
            }
            var nuevas = Directory.GetDirectories(modsRoot).Select(Path.GetFileName)
                .Where(EsCarpetaDeMod)
                .Where(n => !conocidas.Contains(n))
                .Where(n => File.Exists(Path.Combine(modsRoot, n, ManifiestoMod)))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
            r.AddRange(nuevas);
            return r;
        }

        /// <summary>
        /// Apunta DESACTIVADOS en modlist.json los módulos con activoPorDefecto = false que el juego aún no conoce (si no,
        /// el juego los activaría solo al verlos). Conserva el orden y las demás entradas; copia antes la lista a
        /// <paramref name="carpetaCopias"/>. Devuelve los nombres apuntados.
        /// Marks as DISABLED in modlist.json the modules with activoPorDefecto = false that the game does not know yet (otherwise,
        /// the game would enable them on its own when it sees them). Keeps the order and the other entries; first copies the list to
        /// <paramref name="carpetaCopias"/>. Returns the names it marked.
        /// </summary>
        public static List<string> ApuntarDesactivados(string modsRoot, IEnumerable<ModuloEncontrado> modulos, string carpetaCopias, List<string> avisos = null)
        {
            var puestos = new List<string>();
            var lista = LeerLista(modsRoot, out string motivo);
            if (lista == null) { avisos?.Add(motivo + ": no se apuntan los módulos desactivados por defecto"); return puestos; }
            var conocidas = new HashSet<string>(lista.Select(e => e.Folder), StringComparer.OrdinalIgnoreCase);
            foreach (var m in modulos)
            {
                if (m.Declaracion == null || m.Declaracion.ActivoPorDefecto || conocidas.Contains(m.NombreCarpeta)) continue;
                lista.Add(new Entrada { Folder = m.NombreCarpeta, Enabled = false });
                conocidas.Add(m.NombreCarpeta);
                puestos.Add(m.NombreCarpeta);
            }
            if (puestos.Count == 0) return puestos;
            CopiarLista(modsRoot, carpetaCopias);
            EscribirLista(modsRoot, lista);
            return puestos;
        }

        /// <summary>Copia modlist.json (si existe) a la carpeta de copias con la fecha en el nombre.</summary>
        /// Copies modlist.json (if it exists) to the backups folder with the date in the name.
        public static string CopiarLista(string modsRoot, string carpetaCopias)
        {
            string ruta = Path.Combine(modsRoot, ListaMods);
            if (!File.Exists(ruta) || string.IsNullOrEmpty(carpetaCopias)) return null;
            Directory.CreateDirectory(carpetaCopias);
            string copia = Path.Combine(carpetaCopias, $"modlist-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(ruta, copia, true);
            return copia;
        }

        /// <summary>Escribe modlist.json con el mismo formato que JsonUtility.ToJson(…, true) del juego (sangría de 4).</summary>
        /// Writes modlist.json with the same format as the game's JsonUtility.ToJson(…, true) (indent of 4).
        public static void EscribirLista(string modsRoot, List<Entrada> lista)
        {
            var sb = new StringBuilder();
            sb.Append("{\n    \"loadOrder\": [");
            for (int i = 0; i < lista.Count; i++)
            {
                sb.Append(i == 0 ? "\n" : ",\n");
                sb.Append("        {\n            \"folder\": ").Append(JsonSerializer.Serialize(lista[i].Folder))
                  .Append(",\n            \"enabled\": ").Append(lista[i].Enabled ? "true" : "false").Append("\n        }");
            }
            sb.Append(lista.Count == 0 ? "]\n}" : "\n    ]\n}");
            Directory.CreateDirectory(modsRoot);
            File.WriteAllText(Path.Combine(modsRoot, ListaMods), sb.ToString(), new UTF8Encoding(false));
        }
    }

    // -----------------------------------------------------------------------
    // Sección: sincronización de módulos instalados por un gestor (BepInEx\plugins\...) hacia Mods
    // Section: synchronization of modules installed by a manager (BepInEx\plugins\...) into Mods
    // -----------------------------------------------------------------------
    public static class SincronizarModulos
    {
        /// <summary>Marca que deja el núcleo en cada copia que hace él (sin ella, la carpeta se puso a mano y no se toca).</summary>
        /// Marker the core leaves in every copy it makes (without it, the folder was placed by hand and is not touched).
        public const string Marcador = "zero_sincronizado.json";
        /// <summary>Carpeta (ignorada por el juego, empieza por «_») a la que se mueven las copias cuyo origen ya no existe.</summary>
        /// Folder (ignored by the game, starts with «_») to which copies whose source no longer exists are moved.
        public const string CarpetaRetirados = "_ZeroRetirados";

        /// <summary>Qué ha hecho la sincronización (para el log y la autoprueba).</summary>
        /// What the synchronization did (for the log and the self-test).
        public sealed class Informe
        {
            public List<string> Copiados = new List<string>(), Actualizados = new List<string>(), Iguales = new List<string>(),
                                AMano = new List<string>(), Retirados = new List<string>(), Avisos = new List<string>();
            /// <summary>Ficheros obsoletos (el núcleo los copió y el paquete ya no los trae) apartados al actualizar, como «módulo/ruta».</summary>
            /// Obsolete files (the core copied them and the package no longer ships them) set aside on update, as «módulo/ruta».
            public List<string> Obsoletos = new List<string>();
            /// <summary>Ficheros editados a mano en la copia, respaldados antes de poner la versión nueva, como «módulo/ruta».</summary>
            /// Files edited by hand in the copy, backed up before the new version is put in place, as «módulo/ruta».
            public List<string> Respaldados = new List<string>();
            public int Fuentes;
            public override string ToString() =>
                $"{Fuentes} módulo(s) en BepInEx\\plugins; copiados [{string.Join(", ", Copiados)}], actualizados [{string.Join(", ", Actualizados)}], " +
                $"iguales [{string.Join(", ", Iguales)}], puestos a mano (no se tocan) [{string.Join(", ", AMano)}], retirados [{string.Join(", ", Retirados)}], " +
                $"ficheros obsoletos apartados [{string.Join(", ", Obsoletos)}], ediciones locales respaldadas [{string.Join(", ", Respaldados)}]";
        }

        // Contenido del marcador. Ficheros: ruta relativa (con «/») -> huella SHA-256 de cada fichero tal como se copió.
        // Los marcadores anteriores no lo tienen (null): se completa en cuanto se puede (ver CopiarSiHaceFalta).
        // Marker contents. Ficheros: relative path (with «/») -> SHA-256 hash of each file as it was copied.
        // Previous markers do not have it (null): it is filled in as soon as possible (see CopiarSiHaceFalta).
        private sealed class DatosMarcador
        {
            public string Origen { get; set; }
            public string Huella { get; set; }
            public string Version { get; set; }
            public string Fecha { get; set; }
            public Dictionary<string, string> Ficheros { get; set; }
        }

        /// <summary>
        /// Copia a <paramref name="modsRoot"/> cada módulo (carpeta con zero_modulo.json y mod.json) que haya bajo
        /// <paramref name="pluginRoot"/>, si falta o si cambió (huella SHA-256 de sus ficheros). Nunca pierde nada: las copias
        /// propias cuyo origen desapareció (paquete desinstalado o de otro perfil del gestor) se mueven a Mods\_ZeroRetirados,
        /// salvo que allí ya haya una retirada del mismo módulo con idéntico contenido (cambios de perfil): entonces la copia
        /// sale de Mods sin duplicarse.
        /// Al actualizar, los ficheros obsoletos y las ediciones locales también se apartan allí (ver Actualizar).
        /// Una carpeta de Mods sin marcador es una instalación a mano y no se toca.
        /// Copies to <paramref name="modsRoot"/> each module (folder with zero_modulo.json and mod.json) found under
        /// <paramref name="pluginRoot"/>, if it is missing or has changed (SHA-256 fingerprint of its files). Never loses anything: own copies
        /// whose source disappeared (package uninstalled or from another manager profile) are moved to Mods\_ZeroRetirados,
        /// unless a retired copy of the same module with identical content is already there (profile changes): then the copy
        /// leaves Mods without being duplicated.
        /// When updating, obsolete files and local edits are also set aside there (see Actualizar).
        /// A Mods folder without a marker is a manual install and is left untouched.
        /// </summary>
        public static Informe Sincronizar(string pluginRoot, string modsRoot)
        {
            var inf = new Informe();
            if (string.IsNullOrEmpty(modsRoot)) return inf;
            string raizPlugins = string.IsNullOrEmpty(pluginRoot) ? null : Path.GetFullPath(pluginRoot);
            var origenesVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Solo se retiran huérfanas si BepInEx\plugins se pudo recorrer entero (si no, se tomaría todo por huérfano)
            // Orphans are only retired if BepInEx\plugins could be traversed completely (otherwise, everything would be taken as an orphan)
            bool recorrido = false;

            // 1) Fuentes: zero_modulo.json bajo BepInEx\plugins (cualquier profundidad)
            // 1) Sources: zero_modulo.json under BepInEx\plugins (any depth)
            if (raizPlugins != null && Directory.Exists(raizPlugins))
            {
                IEnumerable<string> decls;
                try { decls = Directory.EnumerateFiles(raizPlugins, DeclaracionModulo.NombreFichero, SearchOption.AllDirectories).ToList(); recorrido = true; }
                catch (Exception e) { inf.Avisos.Add("no se pudo recorrer BepInEx\\plugins: " + e.Message); decls = Array.Empty<string>(); }
                foreach (string decl in decls.OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                {
                    string origen = Path.GetDirectoryName(decl);
                    if (!File.Exists(Path.Combine(origen, OrdenMods.ManifiestoMod))) { inf.Avisos.Add($"{origen}: tiene zero_modulo.json pero no mod.json; no se copia"); continue; }
                    var d = Modulos.LeerDeclaracion(decl, out string motivo);
                    if (d == null) { inf.Avisos.Add($"{origen}: zero_modulo.json {motivo}; no se copia"); continue; }
                    string nombre = NombreValido(d.Id) ? d.Id : Path.GetFileName(origen);
                    if (!NombreValido(nombre)) { inf.Avisos.Add($"{origen}: nombre de carpeta no válido '{nombre}'; no se copia"); continue; }
                    inf.Fuentes++;
                    origenesVistos.Add(Path.GetFullPath(origen));
                    try { CopiarSiHaceFalta(origen, Path.Combine(modsRoot, nombre), nombre, d.Version, inf); }
                    catch (Exception e) { inf.Avisos.Add($"{nombre}: no se pudo copiar a Mods ({e.GetType().Name}: {e.Message})"); }
                }
            }

            // 2) Copias propias huérfanas: el origen ya no existe o no está en este BepInEx\plugins -> a _ZeroRetirados
            // 2) Orphaned own copies: the source no longer exists or is not in this BepInEx\plugins -> to _ZeroRetirados
            if (recorrido && Directory.Exists(modsRoot))
            {
                foreach (string dir in Directory.GetDirectories(modsRoot))
                {
                    string nombre = Path.GetFileName(dir);
                    if (!OrdenMods.EsCarpetaDeMod(nombre)) continue;
                    var marca = LeerMarcador(dir);
                    if (marca == null || string.IsNullOrEmpty(marca.Origen)) continue;
                    string origen = SafeFull(marca.Origen);
                    bool vivo = origen != null && origenesVistos.Contains(origen);
                    if (vivo) continue;
                    try
                    {
                        // Al cambiar de perfil en el gestor, la misma copia se retiraría una y otra vez: si ya hay una retirada
                        // de este módulo con el mismo contenido (misma huella), no se duplica. La copia sale igual de Mods (si
                        // no, el juego la seguiría cargando) y no se pierde nada: su contenido ya está guardado byte a byte.
                        // When switching profiles in the manager, the same copy would be retired over and over: if there is already a retired copy
                        // of this module with the same content (same fingerprint), it is not duplicated. The copy is still removed from Mods (otherwise
                        // the game would keep loading it) and nothing is lost: its content is already saved byte for byte.
                        string igual = RetiradaIgual(modsRoot, nombre, Huella(dir));
                        if (igual != null)
                        {
                            Directory.Delete(dir, true);
                            inf.Retirados.Add(nombre);
                            inf.Avisos.Add($"{nombre}: su origen ya no está ({marca.Origen}); sale de Mods sin duplicarse (ya hay una copia igual en {igual})");
                            continue;
                        }
                        string destino = CarpetaUnica(Path.Combine(modsRoot, CarpetaRetirados, $"{nombre}-{DateTime.Now:yyyyMMdd-HHmmss}"));
                        Directory.CreateDirectory(Path.GetDirectoryName(destino));
                        Directory.Move(dir, destino);
                        inf.Retirados.Add(nombre);
                    }
                    catch (Exception e) { inf.Avisos.Add($"{nombre}: su origen ya no está ({marca.Origen}) y no se pudo apartar ({e.Message})"); }
                }
            }
            return inf;
        }

        // Copia el módulo si falta o cambió; respeta las carpetas puestas a mano (sin marcador)
        // Copies the module if it is missing or changed; respects hand-placed folders (without a marker)
        private static void CopiarSiHaceFalta(string origen, string destino, string nombre, string version, Informe inf)
        {
            string huella = Huella(origen);
            Dictionary<string, string> ficheros;
            if (Directory.Exists(destino))
            {
                var marca = LeerMarcador(destino);
                if (marca == null)
                {
                    inf.AMano.Add(nombre);
                    string vMano = Modulos.LeerDeclaracion(Path.Combine(destino, DeclaracionModulo.NombreFichero), out _)?.Version;
                    if (vMano != null && vMano != version)
                        inf.Avisos.Add($"{nombre}: hay una copia puesta a mano en Mods (versión {vMano}) y el gestor trae la {version}; se deja la de Mods (bórrala a mano si quieres la del gestor)");
                    return;
                }
                if (marca.Huella == huella && string.Equals(SafeFull(marca.Origen), Path.GetFullPath(origen), StringComparison.OrdinalIgnoreCase))
                {
                    inf.Iguales.Add(nombre);
                    // Marcador de una versión anterior (sin lista de ficheros): el origen no cambió desde que se copió, así que
                    // sus huellas son justo las de lo copiado. Se completa ya para que la próxima actualización sepa qué puso
                    // el núcleo y qué editó el usuario. La copia no se toca (las ediciones locales se conservan).
                    // Marker from an earlier version (without a file list): the source has not changed since it was copied, so
                    // its fingerprints are exactly those of what was copied. It is completed now so the next update knows what the
                    // core put and what the user edited. The copy is left untouched (local edits are preserved).
                    if (marca.Ficheros == null)
                    {
                        marca.Ficheros = HuellasFicheros(origen);
                        EscribirMarcador(destino, marca);
                    }
                    return;
                }
                ficheros = Actualizar(origen, destino, nombre, marca, inf);
                inf.Actualizados.Add(nombre);
            }
            else
            {
                ficheros = HuellasFicheros(origen);
                CopiarCarpeta(origen, destino);
                inf.Copiados.Add(nombre);
            }
            // El marcador se escribe lo último: si algo falla a medias, se queda el viejo y el siguiente arranque repite la
            // actualización (los ficheros ya puestos coinciden con el paquete y se saltan)
            // The marker is written last: if something fails halfway, the old one stays and the next startup repeats the
            // update (files already in place match the package and are skipped)
            EscribirMarcador(destino, new DatosMarcador
            {
                Origen = Path.GetFullPath(origen), Huella = huella, Version = version,
                Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Ficheros = ficheros
            });
        }

        /// <summary>
        /// Actualiza una copia sincronizada fichero a fichero y devuelve las huellas de lo que queda copiado (las del origen).
        /// - Fichero que el núcleo copió y el paquete ya no trae: se aparta (se mueve) a la carpeta de respaldo.
        /// - Fichero que el paquete trae con otro contenido: si en la copia estaba editado a mano (su huella no es la apuntada
        ///   en el marcador, o el núcleo no lo puso), se guarda antes en la carpeta de respaldo con aviso; luego se pone el nuevo.
        /// - Fichero que añadió el usuario y el paquete no trae: se deja (con aviso).
        /// - Fichero nuevo del paquete: se copia.
        /// La carpeta de respaldo es Mods\_ZeroRetirados\&lt;módulo&gt;-&lt;fecha&gt;-actualizacion\ (con las rutas relativas) y solo se
        /// crea si hace falta. Nunca se borra nada.
        /// Updates a synchronized copy file by file and returns the fingerprints of what remains copied (those of the source).
        /// - File the core copied and the package no longer includes: it is set aside (moved) to the backup folder.
        /// - File the package includes with different content: if in the copy it had been edited by hand (its fingerprint is not the one noted
        ///   in the marker, or the core did not put it there), it is first saved to the backup folder with a warning; then the new one is put in place.
        /// - File added by the user and not included in the package: it is left (with a warning).
        /// - New file from the package: it is copied.
        /// The backup folder is Mods\_ZeroRetirados\&lt;módulo&gt;-&lt;fecha&gt;-actualizacion\ (with relative paths) and is only
        /// created if needed. Nothing is ever deleted.
        /// </summary>
        private static Dictionary<string, string> Actualizar(string origen, string destino, string nombre, DatosMarcador marca, Informe inf)
        {
            var nuevos = HuellasFicheros(origen);

            // Qué copió el núcleo la última vez (sin distinguir mayúsculas, como el sistema de ficheros de Windows)
            // What the core copied last time (case-insensitive, like the Windows file system)
            Dictionary<string, string> gestionados = null;
            if (marca.Ficheros != null)
            {
                gestionados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in marca.Ficheros) gestionados[kv.Key] = kv.Value;
            }
            // Marcador de una versión anterior (sin lista): si la copia sigue idéntica a lo que se copió (misma huella de
            // carpeta), todo es del núcleo y nada está editado; si no, no se puede saber qué tocó el usuario y se respalda todo
            // lo que se vaya a pisar o apartar (respaldar de más no pierde nada)
            // Marker from an earlier version (without a list): if the copy is still identical to what was copied (same folder
            // fingerprint), everything belongs to the core and nothing is edited; otherwise, there is no way to know what the user touched, so everything
            // that is going to be overwritten or moved aside is backed up (backing up too much loses nothing)
            bool antiguoIntacto = gestionados == null && marca.Huella == Huella(destino);
            if (gestionados == null && !antiguoIntacto)
                inf.Avisos.Add($"{nombre}: la copia de Mods tiene un marcador de una versión anterior y no coincide con lo que se copió; se respaldan todos los ficheros que se cambien o aparten");

            // Carpeta de respaldo: se elige y crea solo la primera vez que hace falta
            // Backup folder: chosen and created only the first time it is needed
            string respaldo = null;
            string RutaRespaldo(string rel)
            {
                respaldo ??= CarpetaUnica(Path.Combine(Path.GetDirectoryName(destino), CarpetaRetirados, $"{nombre}-{DateTime.Now:yyyyMMdd-HHmmss}-actualizacion"));
                string r = Path.Combine(respaldo, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(r));
                return r;
            }

            // 0.7.9: ficheros del juego que el núcleo reconstruye en la copia (FicherosJuego) según las declaraciones del
            // paquete nuevo: no son del usuario, así que no se avisa por ellos
            // 0.7.9: game files that the core rebuilds in the copy (FicherosJuego) according to the declarations of the
            // new package: they are not the user's, so no warning is given for them
            var reconstruidos = FicherosJuego.DestinosDeclarados(origen);

            // 1) Ficheros que ya hay en la copia
            // 1) Files already in the copy
            foreach (string f in Directory.GetFiles(destino, "*", SearchOption.AllDirectories))
            {
                if (EsMarcador(f)) continue;
                string rel = RutaRelativa(destino, f);
                bool delNucleo = gestionados == null || gestionados.ContainsKey(rel);

                // 1a) El paquete ya no lo trae
                // 1a) The package no longer includes it
                if (!nuevos.TryGetValue(rel, out string hNuevo))
                {
                    if (!delNucleo)
                    {
                        // 0.7.9: lo reconstruyó el núcleo desde el juego (lo comprueba en cada sesión): se deja sin aviso
                        // 0.7.9: the core rebuilt it from the game (it checks this every session): it is left in place with no warning
                        if (reconstruidos.Contains(rel)) continue;
                        inf.Avisos.Add($"{nombre}: '{rel}' no lo puso el núcleo y el paquete no lo trae; se deja en Mods");
                        continue;
                    }
                    // (si lo copió el núcleo de un paquete anterior, el de la 1.1.0 con los ficheros del juego dentro, se
                    // aparta como cualquier obsoleto: el núcleo lo vuelve a poner desde la instalación al registrar el modelo)
                    // (if the core copied it from an earlier package, the 1.1.0 one with the game files inside, it is
                    // set aside like any obsolete file: the core puts it back from the installation when it registers the model)
                    bool editado = gestionados != null ? HuellaFichero(f) != gestionados[rel] : !antiguoIntacto;
                    string r = RutaRespaldo(rel);
                    File.Move(f, r);
                    inf.Obsoletos.Add($"{nombre}/{rel}");
                    if (editado)
                        inf.Avisos.Add($"{nombre}: '{rel}' ya no viene en el paquete y {(gestionados != null ? "estaba editado" : "podía estar editado")} en la copia de Mods; se aparta a {r}");
                    continue;
                }

                // 1b) El paquete lo trae: si ya es igual, nada; si no, respaldo si estaba editado y se pone el nuevo
                // 1b) The package includes it: if it is already the same, nothing; if not, back it up if it was edited and put the new one in place
                string hDest = HuellaFichero(f);
                if (hDest == hNuevo) continue;
                bool editadoLocal = gestionados == null ? !antiguoIntacto : !gestionados.TryGetValue(rel, out string hCopiado) || hCopiado != hDest;
                if (editadoLocal)
                {
                    string r = RutaRespaldo(rel);
                    File.Copy(f, r, false);
                    inf.Respaldados.Add($"{nombre}/{rel}");
                    inf.Avisos.Add($"{nombre}: '{rel}' {(gestionados != null ? "estaba editado" : "podía estar editado")} en la copia de Mods; tu versión se guarda en {r} y se pone la del paquete");
                }
                File.Copy(Path.Combine(origen, rel), f, true);
            }

            // 2) Ficheros nuevos del paquete que aún no están en la copia
            // 2) New files in the package that are not yet in the copy
            foreach (string rel in nuevos.Keys)
            {
                string d = Path.Combine(destino, rel);
                if (File.Exists(d)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(d));
                File.Copy(Path.Combine(origen, rel), d, false);
            }
            return nuevos;
        }

        /// <summary>Huella SHA-256 de cada fichero de una carpeta (ruta relativa con «/» -> huella), sin el marcador.</summary>
        /// SHA-256 fingerprint of each file in a folder (relative path with «/» -> fingerprint), without the marker.
        private static Dictionary<string, string> HuellasFicheros(string carpeta)
        {
            var r = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories))
                if (!EsMarcador(f)) r[RutaRelativa(carpeta, f)] = HuellaFichero(f);
            return r;
        }

        // Huella SHA-256 (hexadecimal) de un fichero, leído por flujo para no cargar audios grandes enteros en memoria
        // SHA-256 fingerprint (hexadecimal) of a file, read as a stream so large audio files are not loaded entirely into memory
        private static string HuellaFichero(string ruta)
        {
            using var sha = SHA256.Create();
            using var s = File.OpenRead(ruta);
            return Convert.ToHexString(sha.ComputeHash(s));
        }

        // Ruta relativa normalizada con «/» (la misma forma que usa Huella)
        // Relative path normalized with «/» (the same form used by Huella)
        private static string RutaRelativa(string carpeta, string fichero) => Path.GetRelativePath(carpeta, fichero).Replace('\\', '/');

        // ¿Es el marcador del núcleo? (mismo criterio que Huella: por nombre, a cualquier profundidad)
        // Is it the core marker? (same criterion as Huella: by name, at any depth)
        private static bool EsMarcador(string ruta) => string.Equals(Path.GetFileName(ruta), Marcador, StringComparison.OrdinalIgnoreCase);

        // Escribe el marcador de una copia sincronizada (UTF-8 sin BOM, con sangría)
        // Writes the marker of a synchronized copy (UTF-8 without BOM, indented)
        private static void EscribirMarcador(string carpeta, DatosMarcador datos) =>
            File.WriteAllText(Path.Combine(carpeta, Marcador), JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));

        /// <summary>
        /// Carpeta de Mods\_ZeroRetirados con una retirada entera de <paramref name="nombre"/> («&lt;módulo&gt;-aaaammdd-hhmmss»,
        /// con o sin «-N»; no los respaldos parciales «-actualizacion») cuyo contenido tenga esa huella; null si no hay.
        /// Mods\_ZeroRetirados folder with a full retirement of <paramref name="nombre"/> («&lt;módulo&gt;-aaaammdd-hhmmss»,
        /// with or without «-N»; not the partial backups «-actualizacion») whose content has that fingerprint; null if there is none.
        /// </summary>
        private static string RetiradaIgual(string modsRoot, string nombre, string huella)
        {
            string raiz = Path.Combine(modsRoot, CarpetaRetirados);
            if (!Directory.Exists(raiz)) return null;
            var patron = new Regex("^" + Regex.Escape(nombre) + @"-\d{8}-\d{6}(-\d+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            foreach (string d in Directory.GetDirectories(raiz))
            {
                if (!patron.IsMatch(Path.GetFileName(d))) continue;
                try { if (Huella(d) == huella) return d; }
                catch { /* una retirada ilegible no cuenta como igual: se guarda otra */ }
                        // an unreadable retirement does not count as equal: another one is saved
            }
            return null;
        }

        // Ruta libre a partir de una base: la base si no existe; si no, base-2, base-3… (varias pasadas en el mismo segundo)
        // Free path based on a base: the base if it does not exist; otherwise, base-2, base-3… (several passes in the same second)
        private static string CarpetaUnica(string baseRuta)
        {
            string r = baseRuta;
            for (int i = 2; Directory.Exists(r) || File.Exists(r); i++) r = $"{baseRuta}-{i}";
            return r;
        }

        /// <summary>Huella SHA-256 de una carpeta: rutas relativas y contenido de todos sus ficheros (sin el marcador).</summary>
        /// SHA-256 fingerprint of a folder: relative paths and contents of all its files (without the marker).
        public static string Huella(string carpeta)
        {
            using var sha = SHA256.Create();
            var ficheros = Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories)
                .Where(f => !string.Equals(Path.GetFileName(f), Marcador, StringComparison.OrdinalIgnoreCase))
                .Select(f => (rel: Path.GetRelativePath(carpeta, f).Replace('\\', '/'), ruta: f))
                .OrderBy(x => x.rel, StringComparer.Ordinal).ToList();
            foreach (var (rel, ruta) in ficheros)
            {
                byte[] nombre = Encoding.UTF8.GetBytes(rel + "\n");
                sha.TransformBlock(nombre, 0, nombre.Length, null, 0);
                byte[] datos = File.ReadAllBytes(ruta);
                sha.TransformBlock(datos, 0, datos.Length, null, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha.Hash);
        }

        // Copia recursiva que sobrescribe (no borra lo que sobre en el destino)
        // Recursive copy that overwrites (does not delete what is left over in the destination)
        private static void CopiarCarpeta(string origen, string destino)
        {
            Directory.CreateDirectory(destino);
            foreach (string f in Directory.GetFiles(origen, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(origen, f);
                string d = Path.Combine(destino, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(d));
                File.Copy(f, d, true);
            }
        }

        // Marcador de una carpeta de Mods (null si no lo tiene o está roto)
        // Marker of a Mods folder (null if it has none or it is broken)
        private static DatosMarcador LeerMarcador(string carpeta)
        {
            string ruta = Path.Combine(carpeta, Marcador);
            if (!File.Exists(ruta)) return null;
            try { return JsonSerializer.Deserialize<DatosMarcador>(File.ReadAllText(ruta), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { return null; }
        }

        // Nombre de carpeta aceptable para Mods (sin separadores ni «_» delante)
        // Acceptable folder name for Mods (no separators or leading «_»)
        private static bool NombreValido(string n) =>
            OrdenMods.EsCarpetaDeMod(n) && n.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && n.Trim() == n && n != "." && n != "..";

        private static string SafeFull(string p) { try { return string.IsNullOrEmpty(p) ? null : Path.GetFullPath(p); } catch { return null; } }
    }
}
