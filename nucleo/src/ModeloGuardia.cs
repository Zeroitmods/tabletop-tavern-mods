// ModeloGuardia.cs
// Registra los modelos 3D PROPIOS de las unidades nuevas como visual «de mod» y su icon.png como icono de carta.
// Empezó (F2) solo para la facción A (900, carpeta unit_visuals\_900 del módulo facción A); con el importador general
// vale para cualquier unidad 9xx cuyo manifiesto diga «modelo»: «unit_visuals\_<id>» (lo pone generar_mod_facciones.py
// cuando el importador por lotes, modelo\importar_lote.py, ha dejado esa carpeta en el módulo).
//   - Con el registro de mod (UnitVisualOverrides.Register), el juego construye la unidad con el modelo propio en
//     batalla (UnitGPUAnimLoader → UnitVisualOverrideRuntime.TryBuild) y lo enseña también en el retrato 3D de
//     reclutamiento, prestigio y colección (ModUnitPreview.TryCreate usa el mismo diccionario).
//   - El registro «de serie» con la carpeta de la unidad base (UnidadGuardia.AsegurarVisual) se mantiene: el juego lo
//     necesita como respaldo, para la regla arco/espada y para precargar texturas.
//   - Las carpetas empiezan por «_» para que el cargador oficial las salte en silencio (no conoce las unidades 9xx y,
//     además, rechaza visuales de mod para la artillería; el registro directo no pasa por esa comprobación).
//   - El icono se apunta en el diccionario privado UnitVisualOverrideLoader.s_iconPathByUnit (enum → texto, sin
//     structs con referencias), y el juego lo carga con su propio código (TryGetIcon).
//   - Revisión del 18-10, ronda 2 (parte B): el juego pinta los retratos de unidad en marcos 2:3 sin conservar la
//     proporción y el icon.png (entonces 256 x 256 obligatorio; desde la 1.11.1 es el heredado) salía estirado. Ahora, en la caché de iconos del juego
//     (UnitVisualOverrideLoader.s_iconsByPath, la que TryGetIcon mira ANTES de cargar el PNG; ClearOverrides no la
//     vacía) se mete un sprite 2:3 propio con la misma clave (la ruta del icon.png): el retrato.png opcional de 512 x 768
//     de la carpeta del visual o, si no hay, el icon.png encajado arriba en un lienzo 256 x 384 transparente
//     (LogicaIconos.cs). Sin parches de Harmony; cada sprite se crea una vez por sesión y se reutiliza.
//   - Juego 1.11.1 (01-11-2026): el icon.png oficial pasa a ser de 512 x 768 y generar_mod_facciones.py ya lo escribe así
//     (los píxeles del retrato.png). Si el icon.png de una unidad ya mide 512 x 768, PonerSprite2x3 no hace nada: el juego
//     crea el sprite 2:3 y no se estira ni se duplica. Con el icon.png de 256 x 256 de antes sigue como en la ronda 2. Se
//     decide si se retira del todo después de la prueba de Zero.
// Opciones: Visual.ModeloPropio = false vuelve TODAS al modelo de su base; Visual.ModeloBaseEn = "901,906" vuelve solo
// esas (tras recargar los datos del juego, p. ej. al volver al menú principal o reiniciar).
// Si la carpeta de una unidad no existe, esa unidad sigue con el modelo de su base (aviso una vez).
// 0.7.9 (17-11-2026): los módulos ya no traen los ficheros del juego (anim.bin, anchors.bin, marcadores de props). Antes de
// registrar cada modelo, FicherosJuego los reconstruye en su carpeta desde la instalación del jugador
// (StreamingAssets\UnitVisuals); si no puede (falta la base, el juego la cambió...), la unidad usa el modelo de su base.
// ModeloGuardia.cs
// Registers the new units' OWN 3D models as a «mod» visual and their icon.png as the card icon.
// It started (F2) only for faction A (900, folder unit_visuals\_900 of the faction A module); with the general importer
// it works for any 9xx unit whose manifest says «modelo»: «unit_visuals\_<id>» (generar_mod_facciones.py sets it
// when the batch importer, modelo\importar_lote.py, has left that folder in the module).
//   - With the mod registry (UnitVisualOverrides.Register), the game builds the unit with its own model in
//     battle (UnitGPUAnimLoader → UnitVisualOverrideRuntime.TryBuild) and also shows it in the 3D portrait for
//     recruitment, prestige and Collection (ModUnitPreview.TryCreate uses the same dictionary).
//   - The default registration with the base unit's folder (UnidadGuardia.AsegurarVisual) is kept: the game
//     needs it as a fallback, for the bow/sword rule and to preload textures.
//   - The folders start with «_» so the official loader skips them silently (it doesn't know the 9xx units and
//     also rejects mod visuals for artillery; direct registration doesn't go through that check).
//   - The icon is stored in the private dictionary UnitVisualOverrideLoader.s_iconPathByUnit (enum → text, without
//     structs with references), and the game loads it with its own code (TryGetIcon).
//   - Review of 18-10, round 2 (part B): the game draws unit portraits in 2:3 frames without preserving the
//     aspect ratio and the icon.png (then 256 x 256 required; since 1.11.1 it is the legacy one) came out stretched. Now, in the game's icon cache
//     (UnitVisualOverrideLoader.s_iconsByPath, the one TryGetIcon checks BEFORE loading the PNG; ClearOverrides doesn't
//     clear it) a 2:3 sprite of our own is inserted with the same key (the icon.png path): the optional retrato.png of 512 x 768
//     from the visual's folder or, if there is none, the icon.png fitted at the top of a transparent 256 x 384 canvas
//     (LogicaIconos.cs). No Harmony patches; each sprite is created once per session and reused.
//   - Game 1.11.1 (01-11-2026): the official icon.png becomes 512 x 768 and generar_mod_facciones.py already writes it that way
//     (the pixels of retrato.png). If a unit's icon.png is already 512 x 768, PonerSprite2x3 does nothing: the game
//     creates the 2:3 sprite and it is neither stretched nor duplicated. With the old 256 x 256 icon.png it works as in round 2. It will
//     be decided whether to remove it entirely after Zero's test.
// Options: Visual.ModeloPropio = false reverts ALL to their base's model; Visual.ModeloBaseEn = "901,906" reverts only
// those (after reloading the game data, e.g. when returning to the main menu or restarting).
// If a unit's folder doesn't exist, that unit keeps its base's model (warning once).
// 0.7.9 (17-11-2026): the modules no longer ship the game's files (anim.bin, anchors.bin, prop markers). Before
// registering each model, FicherosJuego rebuilds them in its folder from the player's installation
// (StreamingAssets\UnitVisuals); if it can't (base missing, the game changed it...), the unit uses its base's model.

using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TabletopTavern.GpuAnim;
using TJ;
using UnityEngine;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: registro de los modelos propios y de sus iconos
    // Section: registration of the custom models and their icons
    // -----------------------------------------------------------------------
    public static class ModeloGuardia
    {
        // Subcarpeta del modelo de la 900 dentro de su módulo (si el manifiesto no dice otra)
        // Model subfolder of unit 900 inside its module (if the manifest doesn't say otherwise)
        public const string Subcarpeta = "unit_visuals\\_900";

        // Para no repetir mensajes en cada ReloadData
        // To avoid repeating messages on every ReloadData
        private static string _ultimoRegistro;
        private static readonly HashSet<int> _avisadoSinCarpeta = new HashSet<int>();
        private static bool _avisadoIcono;
        // Ronda 2 (parte B): sprites 2:3 ya creados (clave: ruta del icon.png) y fallos por ruta. Un fallo se reintenta en
        // las siguientes recargas (puede ser pasajero: fichero a medio copiar) hasta IntentosSprite2x3 veces
        // Round 2 (part B): 2:3 sprites already created (key: icon.png path) and failures per path. A failure is retried on
        // the following reloads (it may be transient: file half copied) up to IntentosSprite2x3 times
        private static readonly Dictionary<string, Sprite> _sprites2x3 = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _fallos2x3 = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private const int IntentosSprite2x3 = 3;
        private static readonly HashSet<string> _avisos2x3 = new HashSet<string>();
        // Juego 1.11.1: rutas de icon.png que ya son 2:3 oficiales (512 x 768), en las que el núcleo no mete sprite propio
        // Game 1.11.1: icon.png paths that are already official 2:3 (512 x 768), for which the core doesn't add its own sprite
        private static readonly HashSet<string> _iconos2x3Oficiales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _avisadoIconoOficial;
        // 0.7.9: unidades ya avisadas de que no se pudieron reconstruir sus ficheros del juego, y carpeta de modelos del juego
        // 0.7.9: units already warned that their game files could not be rebuilt, and the game's models folder
        private static readonly HashSet<int> _avisadoFicherosJuego = new HashSet<int>();
        private static string _raizUnitVisuals;

        /// <summary>
        /// 0.7.9: StreamingAssets\UnitVisuals de la instalación (Application.streamingAssetsPath; si Unity no la diera, la
        /// carpeta «&lt;juego&gt;_Data\StreamingAssets» junto al ejecutable). Se calcula una vez.
        /// 0.7.9: StreamingAssets\UnitVisuals of the installation (Application.streamingAssetsPath; if Unity doesn't provide it, the
        /// folder «&lt;game&gt;_Data\StreamingAssets» next to the executable). Computed once.
        /// </summary>
        internal static string RaizUnitVisuals()
        {
            if (_raizUnitVisuals != null) return _raizUnitVisuals;
            string sa = null;
            try { sa = Application.streamingAssetsPath; } catch (Exception e) { Plugin.Aviso("streamingAssetsPath no disponible: " + e.Message); }
            if (string.IsNullOrEmpty(sa))
                sa = Path.Combine(BepInEx.Paths.GameRootPath, BepInEx.Paths.ProcessName + "_Data", "StreamingAssets");
            return _raizUnitVisuals = Path.Combine(sa, "UnitVisuals");
        }

        /// <summary>
        /// Carpeta del módulo que trae la 900 (facción A) si está aplicado, o null. 0.7.0: antes era la carpeta del
        /// mod oficial único; ahora sale del manifiesto fusionado (cada unidad apunta la carpeta de su módulo).
        /// Folder of the module that ships 900 (faction A) if applied, or null. 0.7.0: it used to be the folder of the
        /// single official mod; now it comes from the merged manifest (each unit points to its module's folder).
        /// </summary>
        internal static string CarpetaModActivo(string candidata)
        {
            var g = UnidadGuardia.Datos(UnidadGuardia.Guardia);
            return g?.Datos?.Carpeta;
        }

        /// <summary>Ids que deben quedarse con el modelo de su base según Visual.ModeloBaseEn ("901, 906"...).</summary>
        /// Ids that must keep their base's model according to Visual.ModeloBaseEn ("901, 906"...).
        internal static HashSet<int> IdsConModeloBase()
        {
            var ids = new HashSet<int>();
            string texto = Plugin.CfgModeloBaseEn?.Value ?? "";
            foreach (string trozo in texto.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(trozo.Trim(), out int id)) ids.Add(id);
            return ids;
        }

        /// <summary>Carpeta del modelo propio de una unidad (completa) o null si su manifiesto dice «base».</summary>
        /// Folder of a unit's custom model (full path) or null if its manifest says «base».
        internal static string CarpetaModelo(UnidadNueva u)
        {
            string mod = u?.Datos?.Carpeta;
            if (string.IsNullOrEmpty(mod)) return null;
            string sub = u.Datos.Modelo;
            if (string.IsNullOrEmpty(sub) || sub == "base")
            {
                if (u.Id != UnidadGuardia.Guardia) return null;
                sub = Subcarpeta;                   // la 900 siempre tuvo su carpeta, aunque el manifiesto no la diga
                                                    // unit 900 always had its folder, even if the manifest doesn't say so
            }
            return Path.Combine(mod, sub.Replace('\\', Path.DirectorySeparatorChar));
        }

        /// <summary>Registra los modelos propios de todas las unidades nuevas que lo tengan (idempotente).
        /// 'modFolderPath' puede ser null (no se usa: cada unidad lleva la carpeta de su módulo).</summary>
        /// Registers the custom models of all new units that have one (idempotent).
        /// 'modFolderPath' may be null (it isn't used: each unit carries its module's folder).
        public static void Asegurar(string origen, string modFolderPath)
        {
            if (!Plugin.Operativo || Plugin.CfgModeloPropio == null || !Plugin.CfgModeloPropio.Value) return;
            var aBase = IdsConModeloBase();
            var hechas = new List<string>();
            int fjCarpetas = 0, fjEscritos = 0, fjIguales = 0;   // 0.7.9: resumen de la reconstrucción de esta pasada
                                                                 // 0.7.9: summary of this pass's rebuild
            foreach (var u in UnidadGuardia.Todas)
            {
                string carpeta = CarpetaModelo(u);
                if (carpeta == null || aBase.Contains(u.Numero) || !UnidadGuardia.Registrada(u.Numero)) continue;
                if (!File.Exists(Path.Combine(carpeta, "unit.json")))
                {
                    if (_avisadoSinCarpeta.Add(u.Numero))
                        Plugin.Aviso($"[{origen}] No hay modelo propio en {carpeta}: la {u.Numero} usa el modelo de su base ({u.Base}).");
                    continue;
                }
                // 0.7.9: ficheros del juego del modelo, reconstruidos desde la instalación del jugador (una vez por sesión).
                // Si no se puede, la unidad se queda con el modelo de su base: aviso una vez y no se registra
                // 0.7.9: the model's game files, rebuilt from the player's installation (once per session).
                // If it can't be done, the unit keeps its base's model: warning once and it is not registered
                var fj = FicherosJuego.PrepararUnaVez(carpeta, RaizUnitVisuals(), out bool fjNueva);
                if (fjNueva && fj.Declarados > 0)
                {
                    fjCarpetas++; fjEscritos += fj.Escritos; fjIguales += fj.Iguales;
                    foreach (string a in fj.Avisos) Plugin.Aviso($"[{origen}] Ficheros del juego de la {u.Numero}: {a}");
                }
                if (!fj.Ok)
                {
                    if (_avisadoFicherosJuego.Add(u.Numero))
                        Plugin.Aviso($"[{origen}] La {u.Numero} usa el modelo de su base ({u.Base}): no se pudieron reconstruir los " +
                                     $"ficheros del juego de su modelo propio ({fj.Motivo}). Si el juego se ha actualizado, hace falta una versión nueva del módulo.");
                    continue;
                }
                string clave = u.Id.ToString();      // "901": es lo que busca el juego con unitName.ToString()
                                                     // "901": that's what the game looks for with unitName.ToString()
                bool yaEstaba = UnitVisualOverrides.TryGet(clave, out string actual) &&
                                string.Equals(actual, carpeta, StringComparison.OrdinalIgnoreCase);
                if (!yaEstaba) UnitVisualOverrides.Register(clave, carpeta);
                bool icono = RegistrarIcono(u.Id, Path.Combine(carpeta, UnitVisualOverrideLoader.IconFileName));
                hechas.Add(u.Numero + (icono ? "" : " (sin icono)"));
            }
            if (fjCarpetas > 0)
                Plugin.Info($"[{origen}] Ficheros del juego de los modelos propios: {fjCarpetas} carpetas comprobadas contra la instalación " +
                            $"({RaizUnitVisuals()}); {fjEscritos} reconstruidos y {fjIguales} ya estaban bien.");
            string resumen = string.Join(", ", hechas);
            if (resumen != _ultimoRegistro)
            {
                _ultimoRegistro = resumen;
                if (hechas.Count > 0)
                    Plugin.Info($"[{origen}] Modelos propios registrados: {resumen}" +
                                (aBase.Count > 0 ? $" (con el de su base por Visual.ModeloBaseEn: {string.Join(", ", aBase)})" : ""));
            }
        }

        // Apunta el icon.png en el diccionario privado del cargador oficial (lo vacía ClearOverrides en cada ReloadData)
        // Records the icon.png in the official loader's private dictionary (ClearOverrides empties it on every ReloadData)
        private static bool RegistrarIcono(UnitName unidad, string ruta)
        {
            if (!File.Exists(ruta)) return false;
            try
            {
                var dic = UnitVisualOverrideLoader.s_iconPathByUnit;
                if (dic == null) return false;
                dic[unidad] = ruta;
                // Ronda 2 (parte B): sprite 2:3 en la caché del juego con la misma ruta (si falla, queda el cuadrado de serie)
                // Round 2 (part B): 2:3 sprite in the game's cache with the same path (if it fails, the default square one remains)
                PonerSprite2x3(unidad, ruta);
                return true;
            }
            catch (Exception e)
            {
                if (!_avisadoIcono)
                {
                    _avisadoIcono = true;
                    Plugin.Aviso("No se pudo registrar el icono propio de una unidad nueva (" + e.GetType().Name + ": " + e.Message + "); queda el de su base.");
                }
                return false;
            }
        }

        // ---------------------------------------------------------------
        // Ronda 2 (parte B): sprite 2:3 del icono en la caché del juego
        // Round 2 (part B): 2:3 icon sprite in the game's cache
        // ---------------------------------------------------------------
        // Mete en UnitVisualOverrideLoader.s_iconsByPath[rutaIcono] el sprite 2:3 de la unidad. La clave es la ruta del
        // icon.png (la de s_iconPathByUnit) aunque el sprite salga del retrato.png: así, si algún día no estuviera, el juego
        // cargaría su icono cuadrado de siempre en vez de dejar la unidad sin icono. Solo se cambia una entrada que no
        // exista o sea cuadrada (la que crea el juego); el sprite cuadrado sustituido no se destruye (alguna carta podría
        // tenerlo puesto): es una sola vez por unidad y sesión.
        // Puts the unit's 2:3 sprite into UnitVisualOverrideLoader.s_iconsByPath[rutaIcono]. The key is the icon.png
        // path (the one from s_iconPathByUnit) even though the sprite comes from retrato.png: this way, if it were ever missing, the game
        // would load its usual square icon instead of leaving the unit without an icon. Only an entry that doesn't
        // exist or is square (the one the game creates) is replaced; the replaced square sprite is not destroyed (some card might
        // have it set): it happens once per unit and session.
        private static void PonerSprite2x3(UnitName unidad, string rutaIcono)
        {
            try
            {
                // Juego 1.11.1: con un icon.png de 512 x 768 el juego crea él mismo el sprite 2:3; el núcleo no toca la caché
                // Game 1.11.1: with a 512 x 768 icon.png the game creates the 2:3 sprite itself; the core doesn't touch the cache
                if (EsIconoOficial2x3(rutaIcono)) return;
                var cache = UnitVisualOverrideLoader.s_iconsByPath;
                if (cache == null) return;
                if (!_sprites2x3.TryGetValue(rutaIcono, out Sprite propio) || propio == null)
                {
                    if (_fallos2x3.TryGetValue(rutaIcono, out int fallos) && fallos >= IntentosSprite2x3) return;
                    propio = CrearSprite2x3(unidad, rutaIcono);
                    if (propio == null) { _fallos2x3[rutaIcono] = fallos + 1; return; }
                    _sprites2x3[rutaIcono] = propio;
                }
                if (cache.TryGetValue(rutaIcono, out Sprite actual) && actual != null)
                {
                    if (actual.Pointer == propio.Pointer) return;                         // ya está puesto
                                                                                          // already set
                    if (!LogicaIconos.EsCuadrado(actual.rect.width, actual.rect.height)) return;   // uno 2:3 que no es nuestro: se respeta
                                                                                                   // a 2:3 one that isn't ours: it is respected
                }
                cache[rutaIcono] = propio;
            }
            catch (Exception e)
            {
                _fallos2x3[rutaIcono] = IntentosSprite2x3;                              // error inesperado: no se reintenta
                                                                                        // unexpected error: not retried
                Aviso2x3("poner", $"No se pudo poner el retrato 2:3 de la unidad {unidad} ({e.GetType().Name}: {e.Message}); se ve su icono cuadrado (estirado).");
            }
        }

        // ¿El icon.png ya mide 512 x 768 (formato oficial del juego 1.11.1)? Lee solo los 24 primeros bytes. Se recuerda por
        // ruta (lo regenera el generador, no cambia durante la sesión). Si no se puede leer, false (sigue lo de la ronda 2)
        // Is the icon.png already 512 x 768 (official format of game 1.11.1)? It reads only the first 24 bytes. It is remembered per
        // path (the generator regenerates it, it doesn't change during the session). If it can't be read, false (falls back to round 2)
        private static bool EsIconoOficial2x3(string rutaIcono)
        {
            if (_iconos2x3Oficiales.Contains(rutaIcono)) return true;
            try
            {
                var cabecera = new byte[24];
                int leidos;
                using (var f = File.OpenRead(rutaIcono)) leidos = f.Read(cabecera, 0, cabecera.Length);
                if (leidos < cabecera.Length || !LogicaIconos.MedidasPng(cabecera, out int w, out int h) || !LogicaIconos.EsIcono2x3Oficial(w, h))
                    return false;
            }
            catch { return false; }
            _iconos2x3Oficiales.Add(rutaIcono);
            if (!_avisadoIconoOficial)
            {
                _avisadoIconoOficial = true;
                Plugin.Info("Iconos de unidad de 512x768 (formato del juego 1.11.1): los carga el juego; el núcleo no crea retratos 2:3 para ellos.");
            }
            return true;
        }

        // Crea el sprite 2:3: a) retrato.png de 512 x 768 de la carpeta del visual; b) si no hay o no vale, el icon.png
        // encajado arriba en un lienzo 256 x 384 transparente. null si no se puede (queda el icono cuadrado del juego)
        // Creates the 2:3 sprite: a) the 512 x 768 retrato.png from the visual's folder; b) if it's missing or invalid, the icon.png
        // fitted at the top of a transparent 256 x 384 canvas. null if not possible (the game's square icon remains)
        private static Sprite CrearSprite2x3(UnitName unidad, string rutaIcono)
        {
            string nombre = unidad + " mod icon";                                  // el mismo nombre que le pone el juego
                                                                                   // the same name the game gives it
            string retrato = Path.Combine(Path.GetDirectoryName(rutaIcono) ?? "", LogicaIconos.NombreRetrato);
            Texture2D tex = null;
            if (File.Exists(retrato))
            {
                tex = LeerPng(retrato, LogicaIconos.TopeRetratoBytes, nombre);
                if (tex != null && !LogicaIconos.EsRetratoValido(tex.width, tex.height))
                {
                    Aviso2x3("medida" + unidad, $"El retrato.png de la unidad {unidad} mide {tex.width}x{tex.height} y debe medir " +
                             $"{LogicaIconos.AnchoRetrato}x{LogicaIconos.AltoRetrato}; se usa el icon.png encajado en 2:3.");
                    UnityEngine.Object.Destroy(tex);
                    tex = null;
                }
            }
            if (tex == null)
            {
                // b) icon.png encajado: se lee legible, se encaja con la lógica pura y se escribe en una textura nueva
                // b) fitted icon.png: it is read in a readable form, fitted with the pure logic and written into a new texture
                var icono = LeerPng(rutaIcono, LogicaIconos.TopeIconoBytes, nombre);
                if (icono == null) return null;
                try
                {
                    // Solo se encaja un icon.png que el juego acepte (256 x 256 o, desde la 1.11.1, 512 x 768; este último ya no
                    // llega aquí porque PonerSprite2x3 sale antes)
                    // Only an icon.png that the game accepts is fitted (256 x 256 or, since 1.11.1, 512 x 768; the latter no longer
                    // reaches here because PonerSprite2x3 returns earlier)
                    if (!LogicaIconos.EsIconoValido(icono.width, icono.height)) return null;
                    var px = icono.GetPixels32();                                  // RGBA, filas de abajo a arriba
                                                                                   // RGBA, rows from bottom to top
                    int w = icono.width, h = icono.height;
                    var rgba = new byte[w * h * 4];
                    for (int i = 0; i < px.Length; i++)
                    {
                        Color32 c = px[i];
                        rgba[i * 4] = c.r; rgba[i * 4 + 1] = c.g; rgba[i * 4 + 2] = c.b; rgba[i * 4 + 3] = c.a;
                    }
                    byte[] lienzo = LogicaIconos.Encajar(rgba, w, h, out int wl, out int hl);
                    var colores = new Il2CppStructArray<Color32>(wl * hl);
                    for (int i = 0; i < colores.Length; i++)
                        colores[i] = new Color32(lienzo[i * 4], lienzo[i * 4 + 1], lienzo[i * 4 + 2], lienzo[i * 4 + 3]);
                    tex = new Texture2D(wl, hl, TextureFormat.RGBA32, true) { name = nombre };
                    tex.SetPixels32(colores);                                      // se escriben TODOS los píxeles (los nuevos no vienen limpios)
                                                                                   // ALL pixels are written (new ones don't come clean)
                }
                finally { UnityEngine.Object.Destroy(icono); }                     // la textura de lectura no se queda
                                                                                   // the read texture is not kept
            }
            // Textura final: sin repetir bordes, mipmaps (se pinta a menos de la mitad), comprimida y sin copia en memoria
            // Final texture: no repeated edges, mipmaps (it is drawn at less than half size), compressed and with no copy in memory
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(true, false);
            try { tex.Compress(true); } catch (Exception e) { Aviso2x3("comprimir", "Retratos 2:3: la textura se queda sin comprimir (" + e.Message + ")."); }
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = nombre;
            sprite.hideFlags = HideFlags.HideAndDontSave;                          // que no lo libere UnloadUnusedAssets
                                                                                   // so UnloadUnusedAssets doesn't free it
            return sprite;
        }

        // PNG a una textura legible RGBA (null y aviso una vez si no se puede: grande, ilegible, no PNG...)
        // PNG into a readable RGBA texture (null, with a one-time warning, if it can't be done: too large, unreadable, not a PNG...)
        private static Texture2D LeerPng(string ruta, long tope, string nombre)
        {
            Texture2D tex = null;
            bool bien = false;
            try
            {
                if (new FileInfo(ruta).Length > tope) throw new InvalidDataException($"pesa más de {tope / (1024 * 1024)} MB");
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = nombre };
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(ruta), false)) throw new InvalidDataException("no es un PNG válido");
                bien = true;
                return tex;
            }
            catch (Exception e)
            {
                Aviso2x3("leer" + ruta, $"Retratos 2:3: no se pudo leer {ruta} ({e.Message}).");
                return null;
            }
            finally { if (!bien && tex != null) UnityEngine.Object.Destroy(tex); }      // sin textura huérfana si falla
                                                                                        // no orphaned texture if it fails
        }

        private static void Aviso2x3(string clave, string texto)
        {
            if (_avisos2x3.Add(clave)) Plugin.Aviso(texto);
        }
    }
}
