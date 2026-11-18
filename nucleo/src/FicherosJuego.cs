// FicherosJuego.cs
// Núcleo 0.7.9 (17-11-2026): ficheros del JUEGO que necesitan los modelos propios y que los módulos ya no distribuyen.
// Los visuales de las unidades nuevas (Mods\<módulo>\unit_visuals\_<id>\) usan la animación (anim.bin), las anclas
// (anchors.bin) y algún marcador de props (marcador_*.mesh.bin) de su unidad base. El cargador del juego
// (TJBakeVisualLoader.Load) los lee de la MISMA carpeta que el unit.json, así que no basta con apuntarle a la instalación:
// antes de registrar el modelo, el núcleo los reconstruye en esa carpeta desde la instalación del jugador
// (StreamingAssets\UnitVisuals\<Base>\...), siguiendo la declaración zero_ficheros_juego.json que deja el generador
// (bepinex\ZeroNucleo\modelo\ficheros_juego.py, mismo algoritmo).
//   - Entrada «copia»: el fichero es el del juego tal cual.
//   - Entrada «parche»: el fichero es un derivado (esqueleto a escala, reposo enderezado...): fichero del juego XOR el
//     parche «ZPR1» que trae el módulo (se aplica sobre el fichero del juego del jugador; no es una copia de él).
//   - Cada entrada lleva el SHA-256 del fichero del juego (shaOrigen) y del resultado (shaDestino): lo reconstruido es
//     byte a byte lo que se probó. Si el juego cambia esa base (actualización), no se reconstruye nada de esa carpeta
//     (los slots del unit.json y los pesos del cuerpo se hicieron contra el fichero viejo): la unidad usa el modelo de su
//     base hasta que se regenere el módulo.
//   - Nunca lanza: cualquier fallo devuelve Ok = false y el motivo (ModeloGuardia deja la unidad con su modelo base).
//   - 0.7.11 (18-11-2026, juego 1.11.17.2: el estudio volvió a hornear 27 de esos ficheros): si la base cambió, antes
//     de rendirse se usan los ficheros que ya estaban en la carpeta (Previos) o, si todo son copias y la base conserva
//     su esqueleto, se adapta el visual a la base actual (sus ficheros y los slots de su unit.json) en una carpeta
//     aparte de BepInEx\config (Adaptado). Ver Modo.
// No usa tipos del juego (se compila también en las pruebas, PruebasFicherosJuego.cs).
// Autor: ZeroitMods.
// FicherosJuego.cs
// Core 0.7.9 (17-11-2026): GAME files needed by custom models that modules no longer distribute.
// The visuals of new units (Mods\<module>\unit_visuals\_<id>\) use the animation (anim.bin), anchors
// (anchors.bin) and some prop markers (marcador_*.mesh.bin) of their base unit. The game's loader
// (TJBakeVisualLoader.Load) reads them from the SAME folder as the unit.json, so pointing it at the installation is not enough:
// before registering the model, the core rebuilds them in that folder from the player's installation
// (StreamingAssets\UnitVisuals\<Base>\...), following the zero_ficheros_juego.json declaration left by the generator
// (bepinex\ZeroNucleo\modelo\ficheros_juego.py, same algorithm).
//   - «copia» entry: the file is the game file as-is.
//   - «parche» entry: the file is a derivative (scaled skeleton, straightened rest pose...): game file XOR the
//     «ZPR1» patch the module provides (applied on the player's game file; it is not a copy of it).
//   - Each entry carries the SHA-256 of the game file (shaOrigen) and of the result (shaDestino): what is rebuilt is
//     byte-for-byte what was tested. If the game changes that base (update), nothing in that folder is rebuilt
//     (the unit.json slots and body weights were made against the old
//     file): the unit uses its base model until the module is regenerated.
//   - Never throws: any failure returns Ok = false and the reason (ModeloGuardia leaves the unit with its base model).
//   - 0.7.11 (18-11-2026, game 1.11.17.2: the studio re-baked 27 of those files): if the base changed, before
//     giving up it tries, in this order, the files already in the folder (Previos) or, if all entries are copies and the base keeps
//     its skeleton, adapts the visual to the current base (its files and its unit.json slots) in a separate folder
//     under BepInEx\config (Adaptado). See Modo.
// Does not use game types (also compiled in the tests, PruebasFicherosJuego.cs).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: reconstrucción de los ficheros del juego de una carpeta de visual
    // Section: rebuilding the game files of a visual folder
    // -----------------------------------------------------------------------
    public static class FicherosJuego
    {
        /// <summary>Nombre de la declaración dentro de cada carpeta de visual.</summary>
        /// Name of the declaration inside each visual folder.
        public const string NombreDeclaracion = "zero_ficheros_juego.json";
        // Cabecera de los parches: «ZPR1» + u32 largo del resultado + u32 largo del fichero del juego
        // Patch header: «ZPR1» + u32 result length + u32 game file length
        private static readonly byte[] MagiaParche = { (byte)'Z', (byte)'P', (byte)'R', (byte)'1' };
        // Tope de lectura (el anim.bin más grande del juego ronda los 3 MB): un fichero absurdo no se carga en memoria
        // Read limit (the largest anim.bin in the game is around 3 MB): an absurd file is not loaded into memory
        private const long TopeFichero = 64L * 1024 * 1024;

        /// <summary>Una entrada de la declaración (rutas relativas con «/»).</summary>
        /// One entry of the declaration (relative paths with «/»).
        public sealed class Entrada
        {
            public string Destino { get; set; }      // relativo a la carpeta del visual («anim.bin», «rider/anim.bin»)
                                                     // relative to the visual folder («anim.bin», «rider/anim.bin»)
            public string Origen { get; set; }       // relativo a StreamingAssets\UnitVisuals («EisenmannRegiment/anim.bin»)
                                                     // relative to StreamingAssets\UnitVisuals («EisenmannRegiment/anim.bin»)
            public string ShaOrigen { get; set; }
            public string ShaDestino { get; set; }
            public string Cabecera { get; set; }     // 16 primeros bytes del fichero del juego, en hexadecimal (informativo)
                                                     // First 16 bytes of the game file, in hexadecimal (informational)
            public string Parche { get; set; }       // relativo a la carpeta del visual, o null si es una copia
                                                     // relative to the visual folder, or null if it is a copy
        }

        private sealed class Declaracion
        {
            public int Formato { get; set; }
            public List<Entrada> Ficheros { get; set; }
        }

        /// <summary>
        /// 0.7.11: cómo se dejó la carpeta. Exacto = ficheros del juego idénticos a los de cuando se hizo el módulo;
        /// Previos = el juego cambió esa base, pero en la carpeta ya estaban los ficheros de cuando se hizo el módulo
        /// (los reconstruyó un núcleo anterior) y se usan tal cual; Adaptado = el juego cambió esa base y la carpeta no
        /// los tenía: si todo son copias y la base conserva su esqueleto, se monta el visual con los ficheros ACTUALES de
        /// la base y los slots de su unit.json actual, en una carpeta aparte (el módulo no se toca).
        /// 0.7.11: how the folder was left. Exacto = game files identical to those when the module was made;
        /// Previos = the game changed that base, but the files from when the module was made were already in the folder
        /// (rebuilt by an earlier core) and are used as-is; Adaptado = the game changed that base and the folder did not
        /// have them: if all entries are copies and the base keeps its skeleton, the visual is assembled with the CURRENT files of
        /// the base and its unit.json slots, in a separate folder (the module is not touched).
        /// </summary>
        public enum Modo { Ninguno, Exacto, Previos, Adaptado }

        /// <summary>Resultado de preparar una carpeta.</summary>
        /// Result of preparing a folder.
        public sealed class Resultado
        {
            public bool Ok;
            public string Motivo;
            public readonly List<string> Avisos = new List<string>();
            public int Escritos, Iguales, Declarados;
            /// <summary>0.7.11: cómo quedó (Ninguno si no hay declaración o si falló).</summary>
            /// 0.7.11: how it ended up (Ninguno if there is no declaration or if it failed).
            public Modo Modo;
            /// <summary>0.7.11: carpeta que hay que registrar en el juego (la del módulo o, si se adaptó, la adaptada).</summary>
            /// 0.7.11: folder to register in the game (the module's folder or, if adapted, the adapted one).
            public string CarpetaFinal;
        }

        private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        // Una vez por sesión y carpeta (ModeloGuardia.Asegurar se llama en cada ReloadData y en cada mod cargado)
        // Once per session and folder (ModeloGuardia.Asegurar is called on every ReloadData and on every loaded mod)
        private static readonly Dictionary<string, Resultado> _hechas = new Dictionary<string, Resultado>(StringComparer.OrdinalIgnoreCase);

        /// <summary>¿Tiene la carpeta una declaración de ficheros del juego?</summary>
        /// Does the folder have a declaration of game files?
        public static bool TieneDeclaracion(string carpetaVisual) =>
            !string.IsNullOrEmpty(carpetaVisual) && File.Exists(Path.Combine(carpetaVisual, NombreDeclaracion));

        /// <summary>Como Preparar, pero una sola vez por carpeta en la sesión. 'nueva' = true si se ha hecho ahora.</summary>
        /// Like Preparar, but only once per folder per session. 'nueva' = true if it was done just now.
        public static Resultado PrepararUnaVez(string carpetaVisual, string raizUnitVisuals, out bool nueva, string raizAdaptados = null)
        {
            nueva = false;
            string clave = carpetaVisual ?? "";
            if (_hechas.TryGetValue(clave, out var r)) return r;
            r = Preparar(carpetaVisual, raizUnitVisuals, raizAdaptados);
            _hechas[clave] = r;
            nueva = true;
            return r;
        }

        /// <summary>Olvida lo hecho en la sesión (pruebas).</summary>
        /// Forgets what was done in the session (tests).
        public static void Olvidar() => _hechas.Clear();

        /// <summary>
        /// Deja listos los ficheros del juego que lista la declaración de <paramref name="carpetaVisual"/>, desde
        /// <paramref name="raizUnitVisuals"/> (StreamingAssets\UnitVisuals del juego). Solo escribe lo que falta o no
        /// coincide (temporal + mover, para no dejar un fichero a medias). Sin declaración: Ok y nada que hacer.
        /// 0.7.11: si el juego cambió una base (actualización), en vez de rendirse prueba, por este orden, los ficheros que
        /// ya estaban en la carpeta (Previos) y la adaptación a la base actual en <paramref name="raizAdaptados"/>
        /// (Adaptado; solo con copias y la misma estructura). Si nada vale, la unidad usa su modelo base, como antes.
        /// Prepares the game files listed in the declaration of <paramref name="carpetaVisual"/> from
        /// <paramref name="raizUnitVisuals"/> (StreamingAssets\UnitVisuals of the game). Only writes what is missing or does not
        /// match (temp + move, to avoid leaving a half-written file). Without a declaration: Ok and nothing to do.
        /// 0.7.11: if the game changed a base (update), instead of giving up it tries, in this order, the files already
        /// in the folder (Previos) and adaptation to the current base in <paramref name="raizAdaptados"/>
        /// (Adaptado; copies only and same structure). If nothing works, the unit uses its base model, as before.
        /// </summary>
        public static Resultado Preparar(string carpetaVisual, string raizUnitVisuals, string raizAdaptados = null)
        {
            var r = new Resultado { CarpetaFinal = carpetaVisual };
            try
            {
                if (!TieneDeclaracion(carpetaVisual)) { r.Ok = true; return r; }
                if (string.IsNullOrEmpty(raizUnitVisuals) || !Directory.Exists(raizUnitVisuals))
                    return Fallo(r, $"no se encuentra la carpeta de modelos del juego ({raizUnitVisuals ?? "sin ruta"})");
                Declaracion d;
                try { d = JsonSerializer.Deserialize<Declaracion>(File.ReadAllText(Path.Combine(carpetaVisual, NombreDeclaracion)), Opciones); }
                catch (Exception e) { return Fallo(r, $"{NombreDeclaracion} ilegible ({e.GetType().Name}: {e.Message})"); }
                if (d == null || d.Formato != 1 || d.Ficheros == null || d.Ficheros.Count == 0)
                    return Fallo(r, $"{NombreDeclaracion} no válido (formato {d?.Formato}, {d?.Ficheros?.Count ?? 0} ficheros)");
                r.Declarados = d.Ficheros.Count;

                // 0) Entradas válidas y sin destinos repetidos (sin distinguir mayúsculas: en Windows «anim.bin» y
                //    «ANIM.bin» son el mismo fichero)
                // 0) Valid entries and no repeated destinations (case-insensitive: on Windows «anim.bin» and
                //    «ANIM.bin» are the same file)
                var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in d.Ficheros)
                {
                    if (e == null || !RutaValida(e.Destino) || !RutaValida(e.Origen) || (e.Parche != null && !RutaValida(e.Parche))
                        || !EsSha(e.ShaOrigen) || !EsSha(e.ShaDestino))
                        return Fallo(r, $"entrada no válida en {NombreDeclaracion} ({e?.Destino ?? "sin destino"})");
                    if (!vistos.Add(e.Destino)) return Fallo(r, $"destino repetido en {NombreDeclaracion}: {e.Destino}");
                    if (Dentro(raizUnitVisuals, e.Origen) == null || Dentro(carpetaVisual, e.Destino) == null)
                        return Fallo(r, $"{e.Destino}: ruta fuera de su carpeta");
                }

                // 1) Exacto: todo se comprueba y se reconstruye en memoria antes de escribir nada (si una entrada falla, la
                //    carpeta no queda a medias con unas sí y otras no)
                // 1) Exacto: everything is checked and rebuilt in memory before writing anything (if one entry fails, the
                //    folder is not left half-done with some entries done and others not)
                string motivoExacto = ReconstruirEnMemoria(d, carpetaVisual, raizUnitVisuals, out var listos);
                if (motivoExacto == null)
                {
                    Escribir(listos, r);
                    QuitarAdaptada(carpetaVisual, raizAdaptados);
                    r.Modo = Modo.Exacto;
                    r.Ok = true;
                    return r;
                }

                // 2) Previos (0.7.11): el juego cambió la base, pero en la carpeta ya están los ficheros de cuando se hizo el
                //    módulo (los dejó un núcleo anterior). Son justo aquellos contra los que se hicieron los slots y los pesos
                // 2) Previos (0.7.11): the game changed the base, but the files from when the module was made are already in the folder
                //    (left by an earlier core). They are exactly those against which the slots and weights were made
                if (d.Ficheros.All(e => DestinoYaBueno(Dentro(carpetaVisual, e.Destino), e.ShaDestino)))
                {
                    r.Iguales = d.Ficheros.Count;
                    QuitarAdaptada(carpetaVisual, raizAdaptados);
                    r.Modo = Modo.Previos;
                    r.Avisos.Add($"{motivoExacto}; se usan los ficheros que ya había en la carpeta (los de cuando se hizo el módulo)");
                    r.Ok = true;
                    return r;
                }

                // 3) Adaptado (0.7.11): solo copias de una misma base que conserva su esqueleto; carpeta aparte
                // 3) Adaptado (0.7.11): copies only from the same base that keeps its skeleton; separate folder
                if (!string.IsNullOrEmpty(raizAdaptados))
                {
                    string motivoAdaptar;
                    try { motivoAdaptar = Adaptar(d, carpetaVisual, raizUnitVisuals, raizAdaptados, r); }
                    catch (Exception e) { motivoAdaptar = $"{e.GetType().Name}: {e.Message}"; }
                    if (motivoAdaptar == null)
                    {
                        r.Modo = Modo.Adaptado;
                        r.Avisos.Add($"{motivoExacto}; se adapta a la base actual del juego (sus slots y sus ficheros) en {r.CarpetaFinal}");
                        r.Ok = true;
                        return r;
                    }
                    return Fallo(r, $"{motivoExacto}; tampoco se puede adaptar: {motivoAdaptar}");
                }
                return Fallo(r, motivoExacto);
            }
            catch (Exception e)
            {
                return Fallo(r, $"{e.GetType().Name}: {e.Message}");
            }
        }

        // Reconstruye en memoria lo declarado con los ficheros del juego exactos (los de cuando se hizo el módulo). null si
        // todo cuadra; si no, el motivo
        // Rebuilds in memory what is declared using the exact game files (those from when the module was made). null if
        // everything matches; otherwise, the reason
        private static string ReconstruirEnMemoria(Declaracion d, string carpetaVisual, string raizUnitVisuals, out List<(string destino, byte[] datos)> listos)
        {
            listos = new List<(string destino, byte[] datos)>();
            foreach (var e in d.Ficheros)
            {
                string origen = Dentro(raizUnitVisuals, e.Origen);
                string destino = Dentro(carpetaVisual, e.Destino);
                if (!File.Exists(origen)) return $"falta {e.Origen} en la instalación del juego";
                byte[] original = Leer(origen);
                // El fichero del juego tiene que ser exactamente el de cuando se hizo el módulo: los slots del unit.json y
                // los pesos del cuerpo se hicieron contra él
                // The game file must be exactly the one used when the module was made: the unit.json slots and
                // body weights were made against it
                if (!Igual(Sha(original), e.ShaOrigen))
                    return $"{e.Origen} del juego no es el de cuando se hizo el módulo (¿actualización del juego?)";
                byte[] datos;
                if (e.Parche == null) datos = original;
                else
                {
                    string rutaParche = Dentro(carpetaVisual, e.Parche);
                    if (rutaParche == null || !File.Exists(rutaParche)) return $"falta el parche {e.Parche}";
                    datos = AplicarParche(Leer(rutaParche), original, out string error);
                    if (datos == null) return $"{e.Parche}: {error}";
                }
                if (!Igual(Sha(datos), e.ShaDestino)) return $"{e.Destino}: lo reconstruido no coincide con su SHA-256";
                listos.Add((destino, datos));
            }
            return null;
        }

        // Escribe solo lo que falta o no coincide (temporal + mover) y lo cuenta en r
        // Writes only what is missing or does not match (temp + move) and counts it in r
        private static void Escribir(List<(string destino, byte[] datos)> listos, Resultado r)
        {
            foreach (var (destino, datos) in listos)
            {
                if (File.Exists(destino) && new FileInfo(destino).Length == datos.Length && Igual(Sha(Leer(destino)), Sha(datos)))
                {
                    r.Iguales++;
                    continue;
                }
                EscribirFichero(destino, datos);
                r.Escritos++;
            }
        }

        private static void EscribirFichero(string destino, byte[] datos)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            string temporal = destino + ".zero_tmp";
            try
            {
                File.WriteAllBytes(temporal, datos);
                File.Move(temporal, destino, true);
            }
            finally
            {
                if (File.Exists(temporal)) { try { File.Delete(temporal); } catch { /* se reintenta en la siguiente sesión */ } }
                                                                                    // it is retried in the next session
            }
        }

        // ¿Está ya el destino y es el de cuando se hizo el módulo?
        // Is the destination already there and is it the one from when the module was made?
        private static bool DestinoYaBueno(string destino, string shaDestino)
        {
            try { return destino != null && File.Exists(destino) && Igual(Sha(Leer(destino)), shaDestino); }
            catch { return false; }
        }

        // -------------------------------------------------------------------
        // Sección: adaptación a la base actual del juego (0.7.11)
        // Section: adaptation to the current game base (0.7.11)
        // -------------------------------------------------------------------
        /// <summary>
        /// Copia de una malla TJBM con su caja (6 float32 en el byte 20: mínimo xyz y máximo xyz, como
        /// modelo\formato_tjbake.leer_malla) agrandada por cada eje en max(la mitad de su tamaño, 1 unidad). Si no es una
        /// malla TJBM o la caja no es válida, la devuelve igual.
        /// Copy of a TJBM mesh with its bounding box (6 float32 at byte 20: min xyz and max xyz, as
        /// modelo\formato_tjbake.leer_malla) enlarged per axis by max(half its size, 1 unit). If it is not a
        /// TJBM mesh or the box is not valid, it is returned unchanged.
        /// </summary>
        public static byte[] AgrandarCaja(byte[] malla)
        {
            if (malla == null || malla.Length < 48 || System.Text.Encoding.ASCII.GetString(malla, 0, 4) != "TJBM") return malla;
            var copia = (byte[])malla.Clone();
            for (int eje = 0; eje < 3; eje++)
            {
                float lo = BitConverter.ToSingle(copia, 20 + 4 * eje), hi = BitConverter.ToSingle(copia, 32 + 4 * eje);
                if (float.IsNaN(lo) || float.IsNaN(hi) || float.IsInfinity(lo) || float.IsInfinity(hi) || hi < lo) return malla;
                float crece = Math.Max((hi - lo) * 0.5f, 1f);
                BitConverter.GetBytes(lo - crece).CopyTo(copia, 20 + 4 * eje);
                BitConverter.GetBytes(hi + crece).CopyTo(copia, 32 + 4 * eje);
            }
            return copia;
        }

        // Copia profunda de un nodo JSON (en .NET 6 JsonNode no trae DeepClone): se serializa y se vuelve a leer
        // Deep copy of a JSON node (in .NET 6 JsonNode does not have DeepClone): serialized and read back
        private static System.Text.Json.Nodes.JsonNode Clonar(System.Text.Json.Nodes.JsonNode n) =>
            n == null ? null : System.Text.Json.Nodes.JsonNode.Parse(n.ToJsonString());

        /// <summary>Cabecera de anim.bin (TJBA) y anchors.bin (TJBK): «magia» + u32 versión + u32 huesos o anclas + u32
        /// fotogramas + 16 bytes de relleno; luego, por fotograma y hueso/ancla, una matriz 3x4 (anim en float16, anclas en
        /// float32). Mismo formato que modelo\formato_tjbake.py.</summary>
        /// Header of anim.bin (TJBA) and anchors.bin (TJBK): «magic» + u32 version + u32 bones or anchors + u32
        /// frames + 16 padding bytes; then, per frame and bone/anchor, a 3x4 matrix (anim in float16, anchors in
        /// float32). Same format as modelo\formato_tjbake.py.
        public static bool LeerCabecera(byte[] datos, string magia, int bytesPorValor, out int elementos, out int fotogramas)
        {
            elementos = fotogramas = 0;
            if (datos == null || datos.Length < 32 || System.Text.Encoding.ASCII.GetString(datos, 0, 4) != magia) return false;
            if (BitConverter.ToInt32(datos, 4) != 1) return false;
            elementos = BitConverter.ToInt32(datos, 8);
            fotogramas = BitConverter.ToInt32(datos, 12);
            if (elementos <= 0 || fotogramas <= 0) return false;
            return datos.LongLength == 32L + (long)fotogramas * elementos * 12 * bytesPorValor;
        }

        // Monta en raizAdaptados\<carpeta> el visual con los ficheros ACTUALES de la base y los slots de su unit.json actual.
        // null si se pudo; si no, el motivo. Condiciones (si no, es más seguro el modelo base):
        //   - todas las entradas son copias (sin parche), de la misma carpeta base y sin subcarpetas (el jinete no);
        //   - hay anim.bin; si la carpeta del módulo trae un anchors.bin propio sin declarar (anclas calculadas para los
        //     fotogramas viejos, como la 952), no se adapta;
        //   - el unit.json de la base tiene los mismos huesos (número y nombres) que el nuestro; la cabecera de anim.bin
        //     cuadra con ese número de huesos y con el último fotograma de sus slots, y la de anchors.bin con sus anclas.
        // Assembles in raizAdaptados\<folder> the visual with the CURRENT files of the base and its unit.json slots.
        // null if successful; otherwise, the reason. Conditions (otherwise the base model is safer):
        //   - all entries are copies (no patch), from the same base folder and without subfolders (the rider does not qualify);
        //   - there is an anim.bin; if the module folder contains its own anchors.bin without declaring it (anchors calculated for the
        //     old frames, like the 952), it is not adapted;
        //   - the base's unit.json has the same bones (count and names) as ours; the anim.bin header
        //     matches that bone count and the last frame of its slots, and the anchors.bin header matches its anchors.
        private static string Adaptar(Declaracion d, string carpetaVisual, string raizUnitVisuals, string raizAdaptados, Resultado r)
        {
            if (d.Ficheros.Any(e => e.Parche != null)) return "lleva ficheros derivados (parche), que no se pueden rehacer sobre la base nueva";
            if (d.Ficheros.Any(e => e.Destino.Contains('/')) || Directory.GetDirectories(carpetaVisual).Length > 0)
                return "lleva subcarpetas (jinete)";
            if (NombreAdaptada(carpetaVisual) == null) return "la carpeta del visual no está en <módulo>\\unit_visuals\\";
            var bases = d.Ficheros.Select(e => e.Origen.Split('/')[0]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (bases.Count != 1 || d.Ficheros.Any(e => e.Origen.Split('/').Length != 2)) return "los ficheros no salen de una sola carpeta base";
            var nombres = new HashSet<string>(d.Ficheros.Select(e => e.Destino), StringComparer.OrdinalIgnoreCase);
            if (!nombres.Contains("anim.bin")) return "no declara anim.bin";
            if (!nombres.Contains("anchors.bin") && File.Exists(Path.Combine(carpetaVisual, "anchors.bin")))
                return "trae anclas propias (anchors.bin), hechas para los fotogramas de la base vieja";
            string carpetaBase = Dentro(raizUnitVisuals, bases[0]);
            string jsonBase = carpetaBase == null ? null : Path.Combine(carpetaBase, "unit.json");
            string jsonPropio = Path.Combine(carpetaVisual, "unit.json");
            if (jsonBase == null || !File.Exists(jsonBase)) return $"falta {bases[0]}/unit.json en el juego";
            if (!File.Exists(jsonPropio)) return "falta el unit.json del módulo";

            var propio = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(jsonPropio))?.AsObject();
            var deBase = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(jsonBase))?.AsObject();
            if (propio == null || deBase == null) return "unit.json ilegible";
            if (!string.IsNullOrEmpty((string)propio["rider"])) return "lleva jinete (unit.json)";
            // Mismo esqueleto: número y nombres de huesos
            // Same skeleton: bone count and names
            var huesosPropios = propio["skeleton"]?["names"]?.AsArray().Select(n => (string)n).ToList();
            var huesosBase = deBase["skeleton"]?["names"]?.AsArray().Select(n => (string)n).ToList();
            if (huesosPropios == null || huesosBase == null || !huesosPropios.SequenceEqual(huesosBase))
                return $"el esqueleto de {bases[0]} ya no es el del modelo ({huesosBase?.Count ?? 0} huesos frente a {huesosPropios?.Count ?? 0}, o con otros nombres)";
            var slots = deBase["slots"]?.AsArray();
            if (slots == null || slots.Count == 0) return $"{bases[0]}/unit.json sin slots";
            int ultimo = slots.Max(s => (int?)s?["start"] + (int?)s?["count"] ?? 0);

            // Ficheros actuales de la base, comprobados por estructura (no por SHA)
            // Current files of the base, checked by structure (not by SHA)
            var actuales = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in d.Ficheros)
            {
                string origen = Dentro(raizUnitVisuals, e.Origen);
                if (origen == null || !File.Exists(origen)) return $"falta {e.Origen} en la instalación del juego";
                actuales[e.Destino] = Leer(origen);
            }
            if (!LeerCabecera(actuales["anim.bin"], "TJBA", 2, out int huesos, out int fotogramas))
                return $"{bases[0]}/anim.bin con un formato que no se conoce";
            if (huesos != huesosBase.Count) return $"{bases[0]}/anim.bin tiene {huesos} huesos y su unit.json {huesosBase.Count}";
            if (fotogramas != ultimo) return $"{bases[0]}/anim.bin tiene {fotogramas} fotogramas y sus slots llegan al {ultimo}";
            if (actuales.TryGetValue("anchors.bin", out var anclasBin))
            {
                if (!LeerCabecera(anclasBin, "TJBK", 4, out int anclas, out int fotogramasAnclas))
                    return $"{bases[0]}/anchors.bin con un formato que no se conoce";
                int anclasJson = deBase["anchors"]?.AsArray().Count ?? -1;
                if (anclas != anclasJson || fotogramasAnclas != fotogramas)
                    return $"{bases[0]}/anchors.bin no cuadra con su unit.json ({anclas} anclas y {fotogramasAnclas} fotogramas; esperaba {anclasJson} y {fotogramas})";
            }

            // unit.json adaptado: el nuestro con los slots, el ritmo y (si las anclas son las de la base) las anclas de la base
            // Adapted unit.json: ours with the slots, the pace and (if the anchors match the base) the base's anchors
            propio["slots"] = Clonar(deBase["slots"]);
            if (deBase["frameRate"] != null) propio["frameRate"] = Clonar(deBase["frameRate"]);
            if (nombres.Contains("anchors.bin") && deBase["anchors"] != null) propio["anchors"] = Clonar(deBase["anchors"]);
            byte[] jsonAdaptado = System.Text.Encoding.UTF8.GetBytes(propio.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            // Carpeta adaptada: <raizAdaptados>\<módulo>_<carpeta del visual> (nombres de carpeta, sin rutas). Se monta en
            // «.nuevo» y se cambia de golpe: nunca queda una carpeta mezclada con restos de una adaptación anterior
            // Adapted folder: <raizAdaptados>\<module>_<visual folder> (folder names, no paths). Assembled in
            // «.nuevo» and swapped atomically: a folder is never left mixed with leftovers from a previous adaptation
            string destinoFinal = Path.Combine(raizAdaptados, NombreAdaptada(carpetaVisual));
            if (YaAdaptada(destinoFinal, carpetaVisual, actuales, jsonAdaptado))
            {
                r.Iguales = d.Ficheros.Count;
                r.CarpetaFinal = destinoFinal;
                return null;
            }
            string destinoRaiz = destinoFinal + ".nuevo";
            if (Directory.Exists(destinoRaiz)) Directory.Delete(destinoRaiz, true);
            // Lo de la carpeta del módulo (mallas, texturas, icono...) menos la declaración, los parches y lo declarado
            // What is in the module folder (meshes, textures, icon...) minus the declaration, patches and declared files
            var listos = new List<(string destino, byte[] datos)>();
            foreach (string f in Directory.GetFiles(carpetaVisual))
            {
                string n = Path.GetFileName(f);
                if (n.Equals(NombreDeclaracion, StringComparison.OrdinalIgnoreCase) || n.EndsWith(".zpar", StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith(".zero_tmp", StringComparison.OrdinalIgnoreCase) || nombres.Contains(n)
                    || n.Equals("unit.json", StringComparison.OrdinalIgnoreCase)) continue;
                byte[] datos = Leer(f);
                // Las mallas propias llevan la caja (bounds) calculada con TODOS los fotogramas de la animación vieja; con
                // la nueva la figura puede salirse de ella (ataques más largos) y Unity la recortaría al borde de la
                // pantalla. Se agranda con margen (copia: el fichero del módulo no se toca)
                // Custom meshes carry the bounding box (bounds) calculated with ALL frames of the old animation; with
                // the new one the figure may exceed it (longer attacks) and Unity would clip it at the edge of the
                // screen. It is enlarged with a margin (copy: the module file is not touched)
                if (n.EndsWith(".mesh.bin", StringComparison.OrdinalIgnoreCase)) datos = AgrandarCaja(datos);
                listos.Add((Path.Combine(destinoRaiz, n), datos));
            }
            foreach (var kv in actuales) listos.Add((Path.Combine(destinoRaiz, kv.Key), kv.Value));
            listos.Add((Path.Combine(destinoRaiz, "unit.json"), jsonAdaptado));
            Escribir(listos, r);
            if (Directory.Exists(destinoFinal)) Directory.Delete(destinoFinal, true);
            Directory.Move(destinoRaiz, destinoFinal);
            r.CarpetaFinal = destinoFinal;
            return null;
        }

        // Nombre de la carpeta adaptada de un visual: «<módulo>_<id>» si el visual está en <módulo>\unit_visuals\_<id>;
        // null si la carpeta no tiene esa forma
        // Name of the adapted folder for a visual: «<module>_<id>» if the visual is in <module>\unit_visuals\_<id>;
        // null if the folder does not have that form
        private static string NombreAdaptada(string carpetaVisual)
        {
            try
            {
                var visual = new DirectoryInfo(Path.GetFullPath(carpetaVisual).TrimEnd('\\', '/'));
                var uv = visual.Parent;
                var modulo = uv?.Parent;
                if (uv == null || modulo == null || !uv.Name.Equals("unit_visuals", StringComparison.OrdinalIgnoreCase)
                    || !visual.Name.StartsWith("_") || string.IsNullOrEmpty(modulo.Name)) return null;
                return modulo.Name + visual.Name;
            }
            catch { return null; }
        }

        // ¿La carpeta adaptada ya tiene exactamente lo que se montaría? (ficheros del juego, unit.json y lo del módulo)
        // Does the adapted folder already contain exactly what would be assembled? (game files, unit.json and the module's files)
        private static bool YaAdaptada(string destinoFinal, string carpetaVisual, Dictionary<string, byte[]> actuales, byte[] jsonAdaptado)
        {
            try
            {
                if (!Directory.Exists(destinoFinal)) return false;
                if (!DestinoYaBueno(Path.Combine(destinoFinal, "unit.json"), Sha(jsonAdaptado))) return false;
                foreach (var kv in actuales)
                    if (!DestinoYaBueno(Path.Combine(destinoFinal, kv.Key), Sha(kv.Value))) return false;
                foreach (string f in Directory.GetFiles(carpetaVisual))
                {
                    string n = Path.GetFileName(f);
                    if (n.Equals(NombreDeclaracion, StringComparison.OrdinalIgnoreCase) || n.EndsWith(".zpar", StringComparison.OrdinalIgnoreCase)
                        || n.EndsWith(".zero_tmp", StringComparison.OrdinalIgnoreCase) || actuales.ContainsKey(n)
                        || n.Equals("unit.json", StringComparison.OrdinalIgnoreCase)) continue;
                    byte[] datos = Leer(f);
                    if (n.EndsWith(".mesh.bin", StringComparison.OrdinalIgnoreCase)) datos = AgrandarCaja(datos);
                    if (!DestinoYaBueno(Path.Combine(destinoFinal, n), Sha(datos))) return false;
                }
                return true;
            }
            catch { return false; }
        }

        // Quita la carpeta adaptada de un visual que ya no la necesita (vuelve a Exacto o Previos). Es caché del núcleo,
        // dentro de raizAdaptados; si no se puede, se deja (no estorba: no se registra)
        // Removes the adapted folder for a visual that no longer needs it (back to Exacto or Previos). It is core cache,
        // inside raizAdaptados; if it cannot be removed, it is left (harmless: it is not registered)
        private static void QuitarAdaptada(string carpetaVisual, string raizAdaptados)
        {
            if (string.IsNullOrEmpty(raizAdaptados)) return;
            string nombre = NombreAdaptada(carpetaVisual);
            if (nombre == null) return;
            foreach (string c in new[] { Path.Combine(raizAdaptados, nombre), Path.Combine(raizAdaptados, nombre + ".nuevo") })
                try { if (Directory.Exists(c)) Directory.Delete(c, true); } catch { }
        }

        /// <summary>
        /// Rutas (relativas al módulo, con «/») de los ficheros que reconstruye el núcleo según las declaraciones de
        /// unit_visuals\*\ de un módulo. La sincronización desde BepInEx\plugins no los toma por ficheros del usuario.
        /// Paths (relative to the module, with «/») of the files the core rebuilds according to the declarations in
        /// unit_visuals\*\ of a module. Syncing from BepInEx\plugins does not treat them as user files.
        /// </summary>
        public static HashSet<string> DestinosDeclarados(string carpetaModulo)
        {
            var r = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string uv = Path.Combine(carpetaModulo ?? "", "unit_visuals");
                if (!Directory.Exists(uv)) return r;
                foreach (string dir in Directory.GetDirectories(uv))
                {
                    string ruta = Path.Combine(dir, NombreDeclaracion);
                    if (!File.Exists(ruta)) continue;
                    try
                    {
                        var d = JsonSerializer.Deserialize<Declaracion>(File.ReadAllText(ruta), Opciones);
                        foreach (var e in d?.Ficheros ?? new List<Entrada>())
                            if (e != null && RutaValida(e.Destino))
                                r.Add("unit_visuals/" + Path.GetFileName(dir) + "/" + e.Destino);
                    }
                    catch { /* una declaración rota no protege nada: la sincronización hará lo de siempre */ }
                            // a broken declaration protects nothing: the sync will do what it always does
                }
            }
            catch { /* sin acceso a la carpeta: conjunto vacío */ }
                    // no access to the folder: empty set
            return r;
        }

        // -------------------------------------------------------------------
        // Sección: parche ZPR1 (mismo algoritmo que ficheros_juego.aplicar_parche)
        // Section: ZPR1 patch (same algorithm as ficheros_juego.aplicar_parche)
        // -------------------------------------------------------------------
        /// <summary>Resultado = parche XOR fichero del juego en el tramo común + el resto del parche tal cual. null y el
        /// error si el parche no es válido o no es para ese fichero (por largo).</summary>
        /// Result = patch XOR game file over the common span + the rest of the patch as is. null and the
        /// error if the patch is not valid or is not for that file (by length).
        public static byte[] AplicarParche(byte[] parche, byte[] original, out string error)
        {
            error = null;
            if (parche == null || original == null || parche.Length < 12 || !parche.Take(4).SequenceEqual(MagiaParche))
            {
                error = "no es un parche ZPR1";
                return null;
            }
            int largo = BitConverter.ToInt32(parche, 4);
            int largoOriginal = BitConverter.ToInt32(parche, 8);
            if (largo < 0 || parche.Length - 12 != largo || largoOriginal != original.Length)
            {
                error = $"parche de {parche.Length - 12} bytes para {largo}; fichero del juego de {original.Length} y esperaba {largoOriginal}";
                return null;
            }
            var salida = new byte[largo];
            int comun = Math.Min(largo, original.Length);
            for (int i = 0; i < comun; i++) salida[i] = (byte)(parche[12 + i] ^ original[i]);
            if (largo > comun) Buffer.BlockCopy(parche, 12 + comun, salida, comun, largo - comun);
            return salida;
        }

        // -------------------------------------------------------------------
        // Sección: utilidades
        // Section: utilities
        // -------------------------------------------------------------------
        private static Resultado Fallo(Resultado r, string motivo)
        {
            r.Ok = false;
            r.Motivo = motivo;
            return r;
        }

        /// <summary>Ruta relativa segura con «/»: sin unidad ni raíz, sin «\», sin «..», «.» ni partes vacías.</summary>
        /// Safe relative path with «/»: no drive or root, no «\», no «..», «.» or empty parts.
        public static bool RutaValida(string rel)
        {
            if (string.IsNullOrEmpty(rel) || rel.Contains('\\') || rel.Contains(':') || rel.StartsWith("/")) return false;
            return rel.Split('/').All(p => p.Length > 0 && p != "." && p != "..");
        }

        // Ruta completa de 'rel' dentro de 'raiz', o null si se saliera de ella (defensa extra a RutaValida)
        // Full path of 'rel' inside 'raiz', or null if it would escape it (extra defense on top of RutaValida)
        private static string Dentro(string raiz, string rel)
        {
            string baseCompleta = Path.GetFullPath(raiz).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string completa = Path.GetFullPath(Path.Combine(baseCompleta, rel.Replace('/', Path.DirectorySeparatorChar)));
            return completa.StartsWith(baseCompleta, StringComparison.OrdinalIgnoreCase) ? completa : null;
        }

        private static byte[] Leer(string ruta)
        {
            long largo = new FileInfo(ruta).Length;
            if (largo > TopeFichero) throw new InvalidDataException($"{Path.GetFileName(ruta)} mide {largo} bytes (tope {TopeFichero})");
            return File.ReadAllBytes(ruta);
        }

        private static string Sha(byte[] datos) => Convert.ToHexString(SHA256.HashData(datos));

        private static bool Igual(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool EsSha(string s) => s != null && s.Length == 64 && s.All(Uri.IsHexDigit);
    }
}
