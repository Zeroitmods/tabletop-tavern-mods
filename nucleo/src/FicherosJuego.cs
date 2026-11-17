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
// No usa tipos del juego (se compila también en las pruebas, PruebasFicherosJuego.cs).
// Autor: ZeroitMods.
// FicherosJuego.cs
// Core 0.7.9 (17-11-2026): GAME files that the custom models need and that the modules no longer distribute.
// The visuals of the new units (Mods\<módulo>\unit_visuals\_<id>\) use the animation (anim.bin), the anchors
// (anchors.bin) and some props marker (marcador_*.mesh.bin) of their base unit. The game's loader
// (TJBakeVisualLoader.Load) reads them from the SAME folder as unit.json, so pointing it at the installation is not enough:
// before registering the model, the core rebuilds them in that folder from the player's installation
// (StreamingAssets\UnitVisuals\<Base>\...), following the zero_ficheros_juego.json declaration left by the generator
// (bepinex\ZeroNucleo\modelo\ficheros_juego.py, same algorithm).
//   - «copia» entry: the file is the game's file as is.
//   - «parche» entry: the file is a derivative (scaled skeleton, straightened rest pose...): game file XOR the
//     «ZPR1» patch that the module provides (it is applied on top of the player's game file; it is not a copy of it).
//   - Each entry carries the SHA-256 of the game file (shaOrigen) and of the result (shaDestino): what is rebuilt is
//     byte for byte what was tested. If the game changes that base (update), nothing in that folder is rebuilt
//     (the unit.json slots and the body weights were made against the old file): the unit uses its base model
//     until the module is regenerated.
//   - Never throws: any failure returns Ok = false and the reason (ModeloGuardia leaves the unit with its base model).
// It does not use game types (it is also compiled in the tests, PruebasFicherosJuego.cs).
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

        /// <summary>Resultado de preparar una carpeta.</summary>
        /// Result of preparing a folder.
        public sealed class Resultado
        {
            public bool Ok;
            public string Motivo;
            public readonly List<string> Avisos = new List<string>();
            public int Escritos, Iguales, Declarados;
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
        public static Resultado PrepararUnaVez(string carpetaVisual, string raizUnitVisuals, out bool nueva)
        {
            nueva = false;
            string clave = carpetaVisual ?? "";
            if (_hechas.TryGetValue(clave, out var r)) return r;
            r = Preparar(carpetaVisual, raizUnitVisuals);
            _hechas[clave] = r;
            nueva = true;
            return r;
        }

        /// <summary>Olvida lo hecho en la sesión (pruebas).</summary>
        /// Forgets what was done in the session (tests).
        public static void Olvidar() => _hechas.Clear();

        /// <summary>
        /// Deja en <paramref name="carpetaVisual"/> los ficheros que lista su declaración, reconstruidos desde
        /// <paramref name="raizUnitVisuals"/> (StreamingAssets\UnitVisuals del juego). Solo escribe lo que falta o no
        /// coincide (temporal + mover, para no dejar un fichero a medias). Sin declaración: Ok y nada que hacer.
        /// Leaves in <paramref name="carpetaVisual"/> the files listed in its declaration, rebuilt from
        /// <paramref name="raizUnitVisuals"/> (the game's StreamingAssets\UnitVisuals). It only writes what is missing or does not
        /// match (temporary + move, so a file is never left half-written). No declaration: Ok and nothing to do.
        /// </summary>
        public static Resultado Preparar(string carpetaVisual, string raizUnitVisuals)
        {
            var r = new Resultado();
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

                // 1) Todo se comprueba y se reconstruye en memoria antes de escribir nada: si una entrada falla, la carpeta
                //    no queda a medias con unas sí y otras no
                // 1) Everything is checked and rebuilt in memory before anything is written: if one entry fails, the folder
                //    is not left half-done with some entries applied and others not
                var listos = new List<(string destino, byte[] datos)>();
                // Destinos ya vistos, sin distinguir mayúsculas (en Windows «anim.bin» y «ANIM.bin» son el mismo fichero)
                // Destinations already seen, case-insensitive (on Windows «anim.bin» and «ANIM.bin» are the same file)
                var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in d.Ficheros)
                {
                    if (e == null || !RutaValida(e.Destino) || !RutaValida(e.Origen) || (e.Parche != null && !RutaValida(e.Parche))
                        || !EsSha(e.ShaOrigen) || !EsSha(e.ShaDestino))
                        return Fallo(r, $"entrada no válida en {NombreDeclaracion} ({e?.Destino ?? "sin destino"})");
                    if (!vistos.Add(e.Destino)) return Fallo(r, $"destino repetido en {NombreDeclaracion}: {e.Destino}");
                    string origen = Dentro(raizUnitVisuals, e.Origen);
                    string destino = Dentro(carpetaVisual, e.Destino);
                    if (origen == null || destino == null) return Fallo(r, $"{e.Destino}: ruta fuera de su carpeta");
                    if (!File.Exists(origen)) return Fallo(r, $"falta {e.Origen} en la instalación del juego");
                    byte[] original = Leer(origen);
                    // El fichero del juego tiene que ser exactamente el de cuando se hizo el módulo: los slots del unit.json y
                    // los pesos del cuerpo se hicieron contra él. Si una actualización lo cambia, la unidad usa su modelo base
                    // The game file must be exactly the one from when the module was made: the unit.json slots and
                    // the body weights were made against it. If an update changes it, the unit uses its base model
                    if (!Igual(Sha(original), e.ShaOrigen))
                        return Fallo(r, $"{e.Origen} del juego no es el de cuando se hizo el módulo (¿actualización del juego?): {e.Destino} no se puede reconstruir");
                    byte[] datos;
                    if (e.Parche == null) datos = original;
                    else
                    {
                        string rutaParche = Dentro(carpetaVisual, e.Parche);
                        if (rutaParche == null || !File.Exists(rutaParche)) return Fallo(r, $"falta el parche {e.Parche}");
                        datos = AplicarParche(Leer(rutaParche), original, out string error);
                        if (datos == null) return Fallo(r, $"{e.Parche}: {error}");
                    }
                    if (!Igual(Sha(datos), e.ShaDestino)) return Fallo(r, $"{e.Destino}: lo reconstruido no coincide con su SHA-256");
                    listos.Add((destino, datos));
                }

                // 2) Escritura: solo lo que falta o no coincide
                // 2) Writing: only what is missing or does not match
                foreach (var (destino, datos) in listos)
                {
                    if (File.Exists(destino) && new FileInfo(destino).Length == datos.Length && Igual(Sha(Leer(destino)), Sha(datos)))
                    {
                        r.Iguales++;
                        continue;
                    }
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
                    r.Escritos++;
                }
                r.Ok = true;
                return r;
            }
            catch (Exception e)
            {
                return Fallo(r, $"{e.GetType().Name}: {e.Message}");
            }
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
