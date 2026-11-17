// Objetos.cs
// Objetos nuevos (gear) 700-729 del plugin 0.6.0 (notas internas (no publicadas)). El juego tiene una lista cerrada de
// objetos escrita en código (enum GearID y GearData.GetBaseGear: 40 hasta el juego 1.10.17; 56 en la 1.11.1, de ellos 5
// retirados del sorteo en la 1.10.19); el mod oficial no puede añadir objetos. Aquí:
//   - Datos: postfix de GearData.GetGear. Para un id nuevo devuelve su Gear (nombre interno «zeroObjeto7xx», que es la
//     clave de su icono, rareza y número) en vez de None. El Gear es un struct con una cadena: el trampolín de interop lo
//     copia en el búfer de retorno del llamador; se devuelve un Gear CACHEADO por id, así el objeto empaquetado (y la
//     cadena a la que apunta la copia) sigue vivo para siempre. Respeta gear_overrides.json (GearModifierValue).
//   - Icono: el PNG del mod (objetos\7xx.png, 512x512 RGBA) convertido en Sprite y metido en la caché del juego
//     (SpriteData._icons), así SpriteData.GetSprite lo encuentra sin parchear nada más (también si estuviera inlineado).
//   - Textos: claves 7xxName/7xxDesc/7xxFlavor del mod oficial (localization_overrides.json, las escribe el generador).
//   - Efectos (decisión por defecto, ruta c de §3.2): mientras se lleva el objeto, sus reglas de bonus de héroe (con el
//     héroe de la partida y localizationKey «7xxName») se añaden a las listas del juego, las mismas que leen la batalla,
//     la resolución automática y la ficha de la unidad. Las crea el cargador oficial a partir de un hero_bonus_rules.json
//     que escribe el plugin en BepInEx\config\ZeroNucleo_objetos\ y se añaden en bloque (AddRange), como
//     «Ejército reforzado». Cuando cambia el gear (o el héroe) se reconstruyen todas las reglas con el propio juego
//     (HeroBonusManager.LoadRulesFromResourcesAndOverrides) y el postfix vuelve a añadir las de los objetos que se llevan:
//     nunca se quitan elementos de una lista de structs con referencias desde el interop.
//     Fuga al enemigo: en una batalla con señor de la guerra del mismo héroe que el jugador no se añaden (se avisa). El
//     señor de la guerra se mira también cuando el juego guarda el ejército enemigo (EjercitoReforzado.TrasSaveEnemyArmy),
//     que es cuando lo decide, para que la resolución automática (sin cambio de escena) tampoco se los dé.
//   - Sorteo (§5): postfix de CampaignSaveManager.DrawRandomGear (todas las fuentes de gear pasan por él) que cambia
//     cartas por objetos nuevos según la afinidad (SorteoObjetos.cs). Contexto: prefix en AddLootBattlefield y
//     GraveRobbers (raza del enemigo) y en GenerateTown (la ciudad se sortea antes de saber su raza: se rehace después).
//   - Saves: SaneadoObjetos.cs (respaldo de serie en disco + registro aparte); este fichero prepara su tabla.
//   - Armería (0.7.2): mientras se construye la armería del equipo inicial, GearData.GetGearIDs devuelve también los
//     objetos activos (ArmeriaObjetos.cs); salen en su rareza, con candado hasta cogerlos en una partida.
// Opciones: Objetos.ProbabilidadAfin, Objetos.ProbabilidadGeneral, Objetos.ModoPrueba (desde la 0.7.0 los objetos llegan
// solo de los módulos activos; ya no hay Objetos.Activados).
// Los parches van en una instancia de Harmony propia: si falla GetGear o la carga de reglas se quitan solo los objetos.
// Autor: ZeroitMods.
// Objetos.cs
// New gear 700-729 of plugin 0.6.0 (internal notes (unpublished)). The game has a closed list of
// items written in code (enum GearID and GearData.GetBaseGear: 40 up to game 1.10.17; 56 in 1.11.1, 5 of them
// removed from the draw in 1.10.19); the official mod cannot add items. Here:
//   - Data: postfix of GearData.GetGear. For a new id it returns its Gear (internal name «zeroObjeto7xx», which is the
//     key for its icon, rarity and number) instead of None. The Gear is a struct with a string: the interop trampoline
//     copies it into the caller's return buffer; a CACHED Gear is returned per id, so the boxed object (and the
//     string the copy points to) stays alive forever. Respects gear_overrides.json (GearModifierValue).
//   - Icon: the mod's PNG (objetos\7xx.png, 512x512 RGBA) converted into a Sprite and put into the game's cache
//     (SpriteData._icons), so SpriteData.GetSprite finds it without patching anything else (even if it were inlined).
//   - Texts: keys 7xxName/7xxDesc/7xxFlavor of the official mod (localization_overrides.json, written by the generator).
//   - Effects (default decision, route c of §3.2): while the item is worn, its hero bonus rules (with the
//     run's hero and localizationKey «7xxName») are added to the game's lists, the same ones read by the battle,
//     auto-resolve and the unit sheet. The official loader creates them from a hero_bonus_rules.json
//     written by the plugin in BepInEx\config\ZeroNucleo_objetos\ and they are added in bulk (AddRange), like
//     «Ejército reforzado». When the gear (or the hero) changes, all rules are rebuilt with the game itself
//     (HeroBonusManager.LoadRulesFromResourcesAndOverrides) and the postfix adds back those of the items being worn:
//     elements are never removed from a list of structs with references from interop.
//     Leak to the enemy: in a battle with a warlord of the same hero as the player they are not added (a warning is logged). The
//     warlord is also checked when the game saves the enemy army (EjercitoReforzado.TrasSaveEnemyArmy),
//     which is when it is decided, so that auto-resolve (with no scene change) does not grant them either.
//   - Draw (§5): postfix of CampaignSaveManager.DrawRandomGear (all gear sources go through it) that swaps
//     cards for new gear according to affinity (SorteoObjetos.cs). Context: prefix on AddLootBattlefield and
//     GraveRobbers (enemy race) and on GenerateTown (the town is drawn before its race is known: it is redone afterwards).
//   - Saves: SaneadoObjetos.cs (stock backup on disk + separate registry); this file prepares its table.
//   - Armory (0.7.2): while the starting team's armory is being built, GearData.GetGearIDs also returns the
//     active items (ArmeriaObjetos.cs); they appear at their rarity, locked until picked up in a run.
// Options: Objetos.ProbabilidadAfin, Objetos.ProbabilidadGeneral, Objetos.ModoPrueba (since 0.7.0 items come
// only from active modules; there is no Objetos.Activados anymore).
// The patches go in their own Harmony instance: if GetGear or the rules loading fails, only the items are removed.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using Memori.SaveData;
using TJ;
using UnityEngine;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: datos de los objetos, GetGear e iconos
    // Section: gear data, GetGear and icons
    // -----------------------------------------------------------------------
    public static class Objetos
    {
        public const string PrefijoIcono = "zeroObjeto";

        // Juego 1.11.1 (revisión del 01-11-2026): objetos que el juego retiró del sorteo en la 1.10.19 («leave the gear pool»).
        // Siguen en el enum GearID, pero el saneado no los usa como sustituto. Por nombre (no por número) por si el enum se
        // reordena; la misma lista que GEAR_RETIRADO de generar_mod_facciones.py
        // Game 1.11.1 (review of 01-11-2026): gear that the game removed from the draw in 1.10.19 («leave the gear pool»).
        // They are still in the GearID enum, but sanitizing does not use them as a replacement. By name (not by number) in case the enum is
        // reordered; the same list as GEAR_RETIRADO in generar_mod_facciones.py
        private static readonly HashSet<string> Retirados = new HashSet<string>(StringComparer.Ordinal)
            { "RiverTrout", "MichaelsSecretStuff", "OrnateRing", "Cauldron", "CookieAndFowlCard" };

        /// <summary>¿Es uno de los objetos de serie retirados del sorteo en la 1.10.19?</summary>
        /// Is it one of the base gear removed from the draw in 1.10.19?
        private static bool EsRetirado(GearID g) => Retirados.Contains(g.ToString());

        /// <summary>true si los parches imprescindibles de los objetos (GetGear y carga de reglas) se aplicaron.</summary>
        /// true if the essential gear patches (GetGear and rules loading) were applied.
        public static bool Operativo { get; internal set; }

        // Objetos activos del manifiesto (id -> datos) y su versión para el sorteo
        // Active gear from the manifest (id -> data) and its version for the draw
        private static readonly Dictionary<int, Manifiesto.Objeto> _porId = new Dictionary<int, Manifiesto.Objeto>();
        private static List<ObjetoSorteo> _sorteo = new List<ObjetoSorteo>();
        private static readonly Dictionary<int, GearRarity> _rareza = new Dictionary<int, GearRarity>();
        private static readonly List<string> _desactivados = new List<string>();
        // Gear cacheados por id (el objeto empaquetado mantiene viva la cadena GearName) y sprites de los iconos
        // Gear cached by id (the boxed object keeps the GearName string alive) and icon sprites
        private static readonly Dictionary<int, Gear> _gears = new Dictionary<int, Gear>();
        private static readonly Dictionary<int, Sprite> _iconos = new Dictionary<int, Sprite>();
        private static readonly HashSet<int> _iconosFallidos = new HashSet<int>();
        private static readonly HashSet<string> _avisos = new HashSet<string>();

        /// <summary>Ids de los objetos activos (en orden).</summary>
        /// Ids of the active gear (in order).
        public static IReadOnlyList<int> Ids => _porId.Keys.OrderBy(i => i).ToList();
        public static int Numero => _porId.Count;
        public static IReadOnlyList<string> Desactivados => _desactivados;
        internal static IReadOnlyList<ObjetoSorteo> ParaSorteo => _sorteo;
        public static Manifiesto.Objeto Datos(int id) => _porId.TryGetValue(id, out var o) ? o : null;
        public static GearRarity RarezaDe(int id) => _rareza.TryGetValue(id, out var r) ? r : GearRarity.Common;
        public static string NombreInterno(int id) => PrefijoIcono + id;

        /// <summary>
        /// Objetos activos en esta sesión, en caché (se puede leer desde cualquier hilo: el vigilante de saves y el saneado no
        /// llaman al juego). La actualiza ActualizarActivos en el hilo principal.
        /// Active gear in this session, cached (can be read from any thread: the save watcher and sanitizing do not
        /// call the game). Updated by ActualizarActivos on the main thread.
        /// </summary>
        public static volatile bool ActivosEnCache;

        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        // ---------------------------------------------------------------
        // Configuración (al leer el manifiesto, antes de los parches)
        // Configuration (when reading the manifest, before the patches)
        // ---------------------------------------------------------------
        /// <summary>Prepara los objetos activos del manifiesto y la tabla del saneado de saves. Idempotente.</summary>
        /// Prepares the active gear from the manifest and the save sanitizing table. Idempotent.
        public static void Configurar(Manifiesto m)
        {
            _porId.Clear();
            _rareza.Clear();
            _desactivados.Clear();
            var respaldos = new Dictionary<int, int>();
            foreach (var o in m?.Objetos ?? new List<Manifiesto.Objeto>())
            {
                if (!o.Activo) { _desactivados.Add($"{o.Id} (desactivado en el manifiesto)"); continue; }
                if (!Enum.TryParse(o.Respaldo, out GearID respaldo) || !Enum.IsDefined(typeof(GearID), respaldo) || respaldo == GearID.None)
                { _desactivados.Add($"{o.Id} (respaldo desconocido '{o.Respaldo}')"); continue; }
                if (!Enum.TryParse(o.Rareza, out GearRarity rareza) || !Enum.IsDefined(typeof(GearRarity), rareza))
                { _desactivados.Add($"{o.Id} (rareza desconocida '{o.Rareza}')"); continue; }
                if (o.Reglas == null || o.Reglas.Count == 0) { _desactivados.Add($"{o.Id} (sin reglas)"); continue; }
                _porId[o.Id] = o;
                _rareza[o.Id] = rareza;
                respaldos[o.Id] = (int)respaldo;
            }
            // 0.7.4: con su rareza, para cambiar una carta solo por un objeto nuevo de la misma rareza (I8)
            // 0.7.4: with its rarity, to swap a card only for a new gear of the same rarity (I8)
            _sorteo = _porId.Values.OrderBy(o => o.Id).Select(o => new ObjetoSorteo { Id = o.Id, Afinidad = o.Afinidad, Comandante = o.Comandante, Rareza = (int)RarezaDe(o.Id) }).ToList();
            // Juego 1.11.1 (revisión): sin los 5 objetos que el juego retiró del sorteo en la 1.10.19 (siguen en el enum). Así el
            // saneado nunca cambia un 7xx por uno de ellos cuando su respaldo ya se lleva (el save daría un objeto «muerto»)
            // Game 1.11.1 (review): without the 5 gear items the game removed from the draw in 1.10.19 (they are still in the enum). This way
            // sanitizing never swaps a 7xx for one of them when its backup is already being worn (the save would give a «dead» item)
            var deSerie = Enum.GetValues(typeof(GearID)).Cast<GearID>().Where(g => g != GearID.None && !EsRetirado(g)).Select(g => (int)g).ToList();
            Saves.TablaObjetos = respaldos.Count == 0 ? null : new TablaObjetos(respaldos, deSerie);
            // 0.7.0: solo llegan los objetos de los módulos activos; sin ninguno, no hay objetos nuevos
            // 0.7.0: only the gear from active modules arrives; with none, there is no new gear
            ActivosEnCache = _porId.Count > 0;
            Plugin.Info($"Objetos nuevos del manifiesto: {_porId.Count} activos ({string.Join(",", _porId.Keys.OrderBy(i => i))})" +
                        (_desactivados.Count > 0 ? $"; desactivados: {string.Join("; ", _desactivados)}" : "") + ".");
        }

        /// <summary>¿Están los objetos activos ahora? (parches, fábrica operativa y objetos en los módulos aplicados).</summary>
        /// Is the active gear present now? (patches, working factory and gear in the applied modules).
        public static bool ActualizarActivos()
        {
            bool activos = Operativo && Plugin.Operativo && _porId.Count > 0 && UnidadGuardia.ContenidoActivo();
            if (activos != ActivosEnCache)
                Plugin.Info(activos ? "Objetos nuevos ACTIVOS (sorteo, efectos y restauración de saves)." : "Objetos nuevos NO activos en este momento (el saneado de saves sigue).");
            ActivosEnCache = activos;
            return activos;
        }

        // ---------------------------------------------------------------
        // GetGear: datos del objeto nuevo
        // GetGear: new gear data
        // ---------------------------------------------------------------
        /// <summary>Postfix de GearData.GetGear(GearID): para un objeto nuevo devuelve su Gear en vez de None.</summary>
        /// Postfix of GearData.GetGear(GearID): for a new gear it returns its Gear instead of None.
        public static void TrasGetGear(GearID __0, ref Gear __result)
        {
            try
            {
                int id = (int)__0;
                if (id < 700 || id > 729 || !_porId.TryGetValue(id, out var o)) return;
                Gear g = GearDe(id, o);
                if (g == null) return;
                // gear_overrides.json: el original ya puso el valor del mod en el None que devuelve (de serie None vale 0)
                // gear_overrides.json: the original already put the mod's value into the None it returns (by default None is worth 0)
                int valor = __result != null && __result.GearModifierValue != 0 ? __result.GearModifierValue : o.Valor;
                if (g.GearModifierValue != valor) g.GearModifierValue = valor;
                __result = g;
            }
            catch (Exception e) { AvisoUnaVez("getgear", "GetGear de un objeto nuevo: " + e.Message); }
        }

        // Gear cacheado de un objeto (se crea la primera vez que se pide)
        // Cached Gear of an item (created the first time it is requested)
        private static Gear GearDe(int id, Manifiesto.Objeto o)
        {
            if (_gears.TryGetValue(id, out var g) && g != null) return g;
            g = new Gear();
            g.GearName = NombreInterno(id);
            g.GearRarity = RarezaDe(id);
            g.GearModifierValue = o.Valor;
            g.BanFromShop = false;
            _gears[id] = g;
            return g;
        }

        // ---------------------------------------------------------------
        // Iconos
        // Icons
        // ---------------------------------------------------------------
        /// <summary>Sprite del icono de un objeto (se carga del PNG del mod la primera vez; null si falla).</summary>
        /// Sprite of an item's icon (loaded from the mod's PNG the first time; null if it fails).
        public static Sprite Icono(int id)
        {
            if (_iconos.TryGetValue(id, out var s) && s != null) return s;
            if (_iconosFallidos.Contains(id) || !_porId.TryGetValue(id, out var o) || string.IsNullOrEmpty(o.Icono)) return null;
            // 0.7.0: el icono ya viene como ruta completa dentro de la carpeta de su módulo (Modulos.Fusionar)
            // 0.7.0: the icon now comes as a full path inside its module's folder (Modulos.Fusionar)
            string ruta = o.Icono.Replace('\\', Path.DirectorySeparatorChar);
            try
            {
                if (!File.Exists(ruta)) throw new FileNotFoundException("no existe", ruta);
                var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(textura, File.ReadAllBytes(ruta), false)) throw new InvalidDataException("no es un PNG válido");
                textura.name = "zeroObjetoTex" + id;
                textura.wrapMode = TextureWrapMode.Clamp;
                textura.hideFlags = HideFlags.HideAndDontSave;
                var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = NombreInterno(id);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                _iconos[id] = sprite;
                return sprite;
            }
            catch (Exception e)
            {
                _iconosFallidos.Add(id);
                Plugin.Aviso($"Icono del objeto {id}: no se pudo cargar {ruta} ({e.Message}).");
                return null;
            }
        }

        /// <summary>Carga los iconos y los mete en la caché del juego (SpriteData._icons). Devuelve cuántos hay en ella.</summary>
        /// Loads the icons and puts them into the game's cache (SpriteData._icons). Returns how many are in it.
        public static int AsegurarIconos(string origen)
        {
            if (_porId.Count == 0) return 0;
            var dic = SpriteData._icons;
            if (dic == null)
            {
                // La caché se crea en la primera petición: se pide un icono de serie para que cargue la biblioteca
                // The cache is created on the first request: a base icon is requested so that the library loads
                try { SpriteData.GetSprite("Arming Swords"); } catch (Exception e) { Plugin.Aviso("No se pudo cargar la biblioteca de iconos del juego: " + e.Message); }
                dic = SpriteData._icons;
            }
            if (dic == null) { AvisoUnaVez("iconos", "La caché de iconos del juego (SpriteData._icons) no existe: los objetos nuevos saldrán sin icono."); return 0; }
            int puestos = 0, presentes = 0;
            foreach (int id in _porId.Keys)
            {
                var sprite = Icono(id);
                if (sprite == null) continue;
                string clave = NombreInterno(id);
                if (!dic.ContainsKey(clave)) { dic[clave] = sprite; puestos++; }
                presentes++;
            }
            if (puestos > 0) Plugin.Info($"[{origen}] Iconos de objetos nuevos en la caché del juego: {puestos} añadidos ({presentes}/{_porId.Count} presentes).");
            return presentes;
        }

        // ---------------------------------------------------------------
        // Recarga de datos (postfix de ReloadData, vía UnidadGuardia.TrasReloadData)
        // Data reload (postfix of ReloadData, via UnidadGuardia.TrasReloadData)
        // ---------------------------------------------------------------
        public static void TrasRecarga(string origen)
        {
            if (!Operativo) return;
            ActualizarActivos();
            AsegurarIconos(origen);
        }

        // -------------------------------------------------------------------
        // Sección: armería de la preparación de partida (lista de gear ampliada)
        // Section: run setup armory (extended gear list)
        // -------------------------------------------------------------------
        // WarbandGearArmory.Build (UI/Main Menu/RunSetup/WarbandGearArmory.cs:55-82) pinta una casilla por cada id de
        // GearData.GetGearIDs() (enum cerrado: 40 de serie hasta el juego 1.10.17, 56 en la 1.11.1) y la marca como encontrada si está en gearIdsCollected del save
        // del jugador. Mientras dura Build, GetGearIDs devuelve además los objetos de los módulos activos: el propio juego
        // crea sus casillas en su rareza (GetGear parcheado), con candado hasta que se cogen en una partida (misma regla que
        // los de serie: SaveDataHandler.AquiredGear los apunta en la colección y el saneado los conserva en el registro).
        // WarbandGearArmory.Build (UI/Main Menu/RunSetup/WarbandGearArmory.cs:55-82) draws one slot for each id in
        // GearData.GetGearIDs() (closed enum: 40 by default up to game 1.10.17, 56 in 1.11.1) and marks it as found if it is in gearIdsCollected in the player's
        // save. While Build runs, GetGearIDs also returns the gear from the active modules: the game itself
        // creates their slots with their rarity (GetGear patched), locked until they are picked up in a run (same rule as
        // the default ones: SaveDataHandler.AquiredGear records them in the collection and sanitizing keeps them in the registry).
        private static int _anadidosEnContexto;       // ids nuevos añadidos por GetGearIDs dentro del Build en curso
                                                      // new ids added by GetGearIDs within the current Build
        private static bool _armeriaComprobada;

        /// <summary>
        /// API para otras pantallas (la Colección): envolver el método que llama a GearData.GetGearIDs con
        /// EntrarListaAmpliada (prefix) y SalirListaAmpliada (finalizer). Fuera de ese contexto la lista sigue siendo la de serie.
        /// API for other screens (the Collection): wrap the method that calls GearData.GetGearIDs with
        /// EntrarListaAmpliada (prefix) and SalirListaAmpliada (finalizer). Outside that context the list remains the default one.
        /// </summary>
        public static void EntrarListaAmpliada() => ArmeriaObjetos.Entrar();
        public static void SalirListaAmpliada() => ArmeriaObjetos.Salir();

        /// <summary>Postfix de GearData.GetGearIDs(): dentro de una pantalla con lista ampliada añade los objetos activos al final.</summary>
        /// Postfix of GearData.GetGearIDs(): inside a screen with an expanded list, it appends the active gear at the end.
        public static void TrasGetGearIDs(ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<GearID> __result)
        {
            if (!ArmeriaObjetos.Ampliando || !ActivosEnCache || _porId.Count == 0) return;
            try
            {
                var deSerie = new List<int>();
                for (int i = 0; __result != null && i < __result.Length; i++) deSerie.Add((int)__result[i]);
                int[] unidos = ArmeriaObjetos.Unir(deSerie, Ids);
                if (unidos == null) return;
                var nuevo = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<GearID>(unidos.Length);
                for (int i = 0; i < unidos.Length; i++) nuevo[i] = (GearID)unidos[i];
                _anadidosEnContexto += unidos.Length - deSerie.Count;
                __result = nuevo;
            }
            catch (Exception e) { AvisoUnaVez("getgearids", "Lista de gear ampliada (se queda la de serie): " + e.Message); }
        }

        /// <summary>Prefix de WarbandGearArmory.Build: abre el contexto de lista ampliada y asegura los iconos en la caché.</summary>
        /// Prefix of WarbandGearArmory.Build: opens the expanded list context and ensures the icons are in the cache.
        public static void AntesDeArmeria()
        {
            ArmeriaObjetos.Entrar();
            _anadidosEnContexto = 0;
            if (!ActivosEnCache) return;
            try { AsegurarIconos("armería"); } catch (Exception e) { AvisoUnaVez("iconosarmeria", "Iconos para la armería: " + e.Message); }
        }

        /// <summary>
        /// Postfix de WarbandGearArmory.Build: comprueba (una vez por sesión) que las casillas de los objetos nuevos se crearon.
        /// Build sale sin hacer nada si ya tiene casillas (segunda vez que se enseña): entonces no se comprueba.
        /// Postfix of WarbandGearArmory.Build: checks (once per session) that the slots for the new gear were created.
        /// Build exits without doing anything if it already has slots (second time it is shown): in that case no check is done.
        /// </summary>
        public static void TrasArmeria(TJ.MainMenu.WarbandGearArmory __instance)
        {
            if (_armeriaComprobada || !ActivosEnCache || _porId.Count == 0) return;
            try
            {
                int casillas = __instance?.tiles?.Count ?? -1;
                if (casillas <= 0) return;   // aún sin construir (padre inactivo): se comprueba en el siguiente Build
                                             // not built yet (inactive parent): it is checked on the next Build
                _armeriaComprobada = true;
                int deSerie = Enum.GetValues(typeof(GearID)).Length - 1;
                if (_anadidosEnContexto > 0 && casillas >= deSerie + _porId.Count)
                    Plugin.Info($"Armería: {_porId.Count} objetos nuevos en la lista del equipo inicial ({casillas} casillas; con candado hasta cogerlos en una partida).");
                else
                    AvisoUnaVez("armeria", $"Armería: los objetos nuevos no han entrado en la lista del equipo inicial ({casillas} casillas, {_anadidosEnContexto} añadidos; " +
                                           "puede que el compilador del juego haya integrado GetGearIDs dentro de la pantalla).");
            }
            catch (Exception e) { AvisoUnaVez("armeriacomprobar", "Armería (comprobación): " + e.Message); }
        }

        /// <summary>Finalizer de WarbandGearArmory.Build: cierra el contexto pase lo que pase.</summary>
        /// Finalizer of WarbandGearArmory.Build: closes the context no matter what.
        public static Exception FinArmeria(Exception __exception) { ArmeriaObjetos.Salir(); return __exception; }

        // -------------------------------------------------------------------
        // Sección: efectos (reglas de bonus de héroe mientras se lleva el objeto)
        // Section: effects (hero bonus rules while the gear is equipped)
        // -------------------------------------------------------------------
        // Estado de las reglas aplicadas: héroe, objetos y si se omitieron (señor de la guerra del mismo héroe)
        // State of the applied rules: hero, gear, and whether they were skipped (warlord of the same hero)
        private sealed class EstadoReglas
        {
            public int Heroe = -1;
            public List<int> Ids = new List<int>();
            public bool OmitidoPorSenor;
            public string Firma => $"{Heroe}|{string.Join(",", Ids)}|{OmitidoPorSenor}";
        }

        private static EstadoReglas _pendiente;       // lo que pide Sincronizar a la recarga de reglas que provoca
                                                      // what Sincronizar requests from the rules reload it triggers
        private static EstadoReglas _forzado;         // solo para la autoprueba
                                                      // for the self-test only
        private static string _firmaAplicada = "";
        private static bool _dentroReglas;
        private static int _heroe = -1;               // héroe activo según el juego (-1 = menú o batalla personalizada)
                                                      // active hero according to the game (-1 = menu or custom battle)
        private static GearManager _gestorGear;       // último gestor de gear visto (vive en la escena del mapa)
                                                      // last gear manager seen (lives in the map scene)

        /// <summary>Reglas de objetos añadidas en la última carga (stats + rasgos) y los objetos que las dieron.</summary>
        /// Gear rules added in the last load (stats + traits) and the gear that provided them.
        public static int ReglasAplicadas { get; private set; }
        public static string UltimoEstado => _firmaAplicada;

        private static string CarpetaReglas => Path.Combine(Paths.ConfigPath, Plugin.Prefijo + "_objetos");

        // ¿Sigue vivo el gestor de gear del mapa? (en la batalla se destruye)
        // Is the map's gear manager still alive? (it is destroyed in battle)
        private static bool GestorVivo()
        {
            try { return _gestorGear != null && !_gestorGear.WasCollected && _gestorGear.Pointer != IntPtr.Zero; }
            catch { return false; }
        }

        // Objetos nuevos que lleva el jugador y están activos (ni rotos ni anulados por una ordalía)
        // New gear the player is carrying that is active (neither broken nor nullified by an ordeal)
        private static List<int> ObjetosQueSeLlevan()
        {
            var r = new SortedSet<int>();
            if (GestorVivo())
            {
                var lista = _gestorGear.activeGearItems;
                for (int i = 0; lista != null && i < lista.Count; i++) { int id = (int)lista[i]; if (_porId.ContainsKey(id)) r.Add(id); }
                return r.ToList();
            }
            var run = SaveDataHandler.Load();
            var gear = run?.Gear;
            for (int i = 0; gear != null && i < gear.Count; i++)
            {
                var g = gear[i];
                if (_porId.ContainsKey((int)g) && !run.IsGearInactive(g)) r.Add((int)g);
            }
            return r.ToList();
        }

        // Estado que toca ahora según el juego
        // State that applies now according to the game
        private static EstadoReglas CalcularEstado()
        {
            if (_forzado != null) return _forzado;
            var e = new EstadoReglas { Heroe = _heroe };
            if (_heroe <= 0) return e;
            e.Ids = ObjetosQueSeLlevan();
            if (e.Ids.Count > 0)
            {
                // Señor de la guerra con el mismo héroe: sus escuadras recibirían también los bonus del objeto
                // Warlord with the same hero: their squads would also receive the gear's bonuses
                int senor = SaveDataHandler.Load()?.enemyWarlordHeroID ?? 0;
                if (senor > 0 && senor == _heroe)
                {
                    e.OmitidoPorSenor = true;
                    e.Ids = new List<int>();
                    AvisoUnaVez("senor" + _heroe, $"Objetos: el señor de la guerra enemigo usa el mismo héroe ({_heroe}); mientras dure esa batalla los objetos nuevos no dan bonus (si no, también se los daría al enemigo).");
                }
            }
            return e;
        }

        /// <summary>
        /// Pone las reglas de los objetos que se llevan ahora: si cambió algo, reconstruye todas las reglas de héroe con el
        /// propio juego y el postfix (TrasCargarReglas) añade las de los objetos.
        /// Sets the rules for the gear currently carried: if anything changed, it rebuilds all hero rules with the
        /// game itself and the postfix (TrasCargarReglas) adds those of the gear.
        /// </summary>
        public static void Sincronizar(string motivo)
        {
            if (!Operativo || _dentroReglas) return;
            try
            {
                if (!ActualizarActivos())
                {
                    if (_firmaAplicada.Length > 0 && ReglasAplicadas > 0) RecargarReglas(new EstadoReglas());   // quitar las que hubiera
                                                                                                                // remove any that existed
                    return;
                }
                var e = CalcularEstado();
                if (e.Firma == _firmaAplicada) return;
                RecargarReglas(e);
                Plugin.TrazaUnaVez("Objetos.Sincronizar " + motivo);
            }
            catch (Exception ex) { Plugin.Error($"Objetos (sincronizar, {motivo}): {ex}"); }
        }

        // Reconstruye las reglas del juego pidiendo el estado dado a nuestro postfix
        // Rebuilds the game's rules by passing the given state to our postfix
        private static void RecargarReglas(EstadoReglas e)
        {
            _pendiente = e;
            // 0.7.1: con las carpetas de mods activas al arrancar (las que leyó el núcleo), no con el modlist.json del momento
            // 0.7.1: with the mod folders active at startup (the ones the core read), not with the modlist.json of the moment
            try { HeroBonusManager.LoadRulesFromResourcesAndOverrides(Nucleo.CarpetasParaJuego()); }
            finally { _pendiente = null; }
        }

        /// <summary>Postfix de HeroBonusManager.LoadRulesFromResourcesAndOverrides: añade las reglas de los objetos que se llevan.</summary>
        /// Postfix of HeroBonusManager.LoadRulesFromResourcesAndOverrides: adds the rules of the gear being carried.
        public static void TrasCargarReglas()
        {
            if (_dentroReglas) return;
            _dentroReglas = true;
            try
            {
                ReglasAplicadas = 0;
                var e = _pendiente ?? (ActivosEnCache ? CalcularEstado() : new EstadoReglas());
                if (!ActivosEnCache && _pendiente == null) e = new EstadoReglas();
                Aplicar(e);
            }
            catch (Exception ex) { Plugin.Error("Objetos (reglas): " + ex); }
            finally { _dentroReglas = false; }
        }

        // Añade a las listas del juego las reglas de los objetos del estado (las listas acaban de rehacerse desde cero)
        // Adds the state's gear rules to the game's lists (the lists have just been rebuilt from scratch)
        private static void Aplicar(EstadoReglas e)
        {
            _firmaAplicada = e.Firma;
            if (e.Heroe <= 0 || e.Ids.Count == 0) return;
            var reales = HeroBonusManager._statRules;
            var realesAtributos = HeroBonusManager._attributeRules;
            if (reales == null || realesAtributos == null) { AvisoUnaVez("listas", "Objetos: las listas de reglas de héroe aún no existen."); _firmaAplicada = ""; return; }

            var objetos = e.Ids.Where(_porId.ContainsKey).Select(id => _porId[id]).ToList();
            var catalogo = Catalogo();
            string json = ReglasObjetos.Json(objetos, e.Heroe,
                c => catalogo.Where(u => ReglasObjetos.CumpleLista(c, u.rareza, u.tamano, u.tipo)).Select(u => u.id),
                ValorDeMod, out int nStats, out int nAtributos);
            if (nStats + nAtributos == 0) return;

            Directory.CreateDirectory(CarpetaReglas);
            string ruta = Path.Combine(CarpetaReglas, HeroBonusRuleOverrideLoader.FileName);
            if (!File.Exists(ruta) || File.ReadAllText(ruta) != json) File.WriteAllText(ruta, json, new UTF8Encoding(false));

            // El cargador del juego crea las reglas en listas temporales y se añaden en bloque a las reales
            // The game's loader creates the rules in temporary lists and they are added in bulk to the real ones
            var temporal = new Il2CppGeneric.List<HeroStatBonusRule>();
            var atributos = new Il2CppGeneric.List<HeroAttributeBonusRule>();
            var faccion = new Il2CppGeneric.List<FactionBonusRule>();
            HeroBonusRuleOverrideLoader.ApplyOverridesFromModFolder(CarpetaReglas, temporal, atributos, faccion);
            if (temporal.Count > 0) reales.AddRange(temporal.Cast<Il2CppGeneric.IEnumerable<HeroStatBonusRule>>());
            if (atributos.Count > 0) realesAtributos.AddRange(atributos.Cast<Il2CppGeneric.IEnumerable<HeroAttributeBonusRule>>());
            ReglasAplicadas = temporal.Count + atributos.Count;
            if (ReglasAplicadas != nStats + nAtributos)
                Plugin.Aviso($"Objetos: el cargador del juego creó {temporal.Count}/{nStats} reglas de stats y {atributos.Count}/{nAtributos} de rasgos (ver Player.log).");
            if (_forzado == null)
                Plugin.Info($"Objetos: {temporal.Count} reglas de stats y {atributos.Count} de rasgos para el héroe {e.Heroe} (objetos {string.Join(", ", e.Ids)}).");
        }

        // Valor de un objeto si gear_overrides.json lo cambia (para su regla principal); null = el del manifiesto
        // Value of a gear item if gear_overrides.json changes it (for its main rule); null = the manifest's value
        private static int? ValorDeMod(int id)
        {
            try
            {
                var dic = GearData.ModifierOverrides;
                if (dic != null && dic.ContainsKey((GearID)id)) return dic[(GearID)id];
            }
            catch { }
            return null;
        }

        // Unidades del catálogo con su rareza, tamaño y tipo (para las condiciones ListaCalculada)
        // Catalog units with their rarity, size and type (for ListaCalculada conditions)
        private static List<(int id, string rareza, string tamano, string tipo)> Catalogo()
        {
            var r = new List<(int, string, string, string)>();
            var data = TabletopTavernData.InstanceIfExists;
            var dic = data?.SquadStatsDictionary;
            if (dic == null) return r;
            var claves = new List<UnitName>();
            foreach (UnitName k in dic.Keys) claves.Add(k);
            foreach (var k in claves)
            {
                SquadStats s = dic[k];
                r.Add(((int)k, s.RarityTier.ToString(), s.unitSize.ToString(), s.unitType.ToString()));
            }
            return r;
        }

        // ---------------------------------------------------------------
        // Puntos de sincronización (postfix)
        // Synchronization points (postfix)
        // ---------------------------------------------------------------
        /// <summary>Postfix de HeroBonusManager.OnGameStateChanged: héroe activo (o -1) y estado de la escena (entra en batalla).</summary>
        /// Postfix of HeroBonusManager.OnGameStateChanged: active hero (or -1) and scene state (entering battle).
        public static void TrasOnGameStateChanged(HeroBonusManager __instance)
        {
            try { _heroe = __instance.ActiveHeroID; } catch { }
            // 0.7.2 (revisión): red del contexto de lista ampliada. Si una excepción nativa se saltara el finalizer de una
            // pantalla, el contador se quedaría abierto y GetGearIDs devolvería los 7xx en todo el juego; en un cambio de
            // escena ninguna pantalla está construyendo su lista, así que se vuelve a cero
            // 0.7.2 (review): safety net for the expanded list context. If a native exception skipped a screen's finalizer,
            // the counter would stay open and GetGearIDs would return the 7xx ids throughout the game; during a scene
            // change no screen is building its list, so it is reset to zero
            try { ArmeriaObjetos.Reiniciar(); } catch { }
            Sincronizar("cambio de escena");
        }

        // El héroe se conoce por la escena; en el mapa, si aún no se sabe, se lee del save
        // The hero is known from the scene; on the map, if still unknown, it is read from the save
        private static void HeroeDelMapa()
        {
            if (_heroe > 0) return;
            try { _heroe = SaveDataHandler.IsCustomBattle() ? -1 : SaveDataHandler.GetActiveHeroID(); } catch { }
        }

        /// <summary>Postfix de GearManager.LoadAllGear: gear cargado del save (al entrar al mapa).</summary>
        /// Postfix of GearManager.LoadAllGear: gear loaded from the save (when entering the map).
        public static void TrasLoadAllGear(GearManager __instance)
        {
            _gestorGear = __instance;
            HeroeDelMapa();
            if (Operativo) { try { AsegurarIconos("LoadAllGear"); } catch { } }
            Sincronizar("gear cargado");
        }

        /// <summary>Postfix de GearManager.AquireGear y UnAquireGear: se gana o se pierde un objeto.</summary>
        /// Postfix of GearManager.AquireGear and UnAquireGear: a gear item is gained or lost.
        public static void TrasCambioGear(GearManager __instance)
        {
            _gestorGear = __instance;
            HeroeDelMapa();
            Sincronizar("cambio de gear");
        }

        /// <summary>Postfix de CampaignSaveManager.ReforgeGear (herrería): un objeto se cambia por otro.</summary>
        /// Postfix of CampaignSaveManager.ReforgeGear (smithy): one gear item is swapped for another.
        public static void TrasReforgeGear() => Sincronizar("herrería");

        // -------------------------------------------------------------------
        // Sección: sorteo según la partida
        // Section: draw depending on the run
        // -------------------------------------------------------------------
        private static int _contextoRaza = -1;        // raza del contexto del sorteo en curso (-1 = ninguno)
                                                      // race of the current draw context (-1 = none)
        private static int _enCiudad;                 // > 0 = sorteo dentro de GenerateTown (se rehace en su postfix)
                                                      // > 0 = draw inside GenerateTown (redone in its postfix)
        private static int _sorteosCambiados, _cartasCambiadas;

        /// <summary>Probabilidades de la configuración (0-1).</summary>
        /// Probabilities from the configuration (0-1).
        private static double ProbAfin => Math.Clamp(Plugin.CfgObjetosProbAfin?.Value ?? 0.25f, 0f, 1f);
        private static double ProbGeneral => Math.Clamp(Plugin.CfgObjetosProbGeneral?.Value ?? 0.05f, 0f, 1f);
        private static bool ModoPrueba => Plugin.CfgObjetosModoPrueba != null && Plugin.CfgObjetosModoPrueba.Value;

        // Nombre de una raza del juego (lo que lleva la afinidad del manifiesto)
        // Name of a game race (what the manifest's affinity carries)
        private static string NombreRaza(int r) => ((Race)r).ToString();

        // Grupos afines: raza del héroe, facciones con 3 o más escuadras en el ejército y la raza del contexto
        // Related groups: the hero's race, factions with 3 or more squads in the army, and the context's race
        private static HashSet<string> Afinidades(int heroe, int razaContexto, out string detalle)
        {
            var afines = new HashSet<string>();
            var partes = new List<string>();
            try
            {
                string g = SorteoObjetos.GrupoDeRaza((int)HeroData.GetRaceFromHero(heroe), NombreRaza);
                if (g != null) { afines.Add(g); partes.Add("héroe " + g); }
            }
            catch { }
            try
            {
                var data = TabletopTavernData.InstanceIfExists;
                var unidades = UnidadesDelEjercitoGuardado();
                if (data != null && unidades.Count > 0)
                {
                    int Raza(int u) { try { return (int)data.GetRaceFromUnitName((UnitName)u); } catch { return -1; } }
                    foreach (string g in SorteoObjetos.GruposDelEjercito(unidades, Raza, NombreRaza)) if (afines.Add(g)) partes.Add("ejército " + g);
                }
            }
            catch (Exception e) { AvisoUnaVez("ejercito", "Sorteo de objetos: no se pudo leer el ejército del save (" + e.Message + "); se usa solo el héroe y el contexto."); }
            string gc = razaContexto >= 0 ? SorteoObjetos.GrupoDeRaza(razaContexto, NombreRaza) : null;
            if (gc != null && afines.Add(gc)) partes.Add("contexto " + gc);
            detalle = partes.Count == 0 ? "sin afinidad" : string.Join(", ", partes);
            return afines;
        }

        // Unidades del ejército del jugador según el save de campaña del disco, con las 9xx restauradas por el registro
        // Player's army units according to the campaign save on disk, with the 9xx ones restored by the registry
        private static List<int> UnidadesDelEjercitoGuardado()
        {
            // 0.7.1: carpeta de datos del juego que calculó el plugin al arrancar (Application.persistentDataPath o la fija)
            // 0.7.1: game data folder computed by the plugin at startup (Application.persistentDataPath or the fixed one)
            string ruta = Path.Combine(Plugin.CarpetaDatosJuego, "campaignSaveData.json");
            if (!File.Exists(ruta)) return new List<int>();
            string texto;
            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var sr = new StreamReader(fs)) texto = sr.ReadToEnd();
            if (Saves.Tabla != null && Saves.Registro != null) texto = SaneadoSaves.DesdeDisco(texto, Saves.Tabla, Saves.Registro);
            return SorteoObjetos.UnidadesDelEjercito(texto);
        }

        // Cambia las cartas de una lista de GearID del juego; devuelve cuántas cambió
        // Swaps the cards in a list of the game's GearID; returns how many it swapped
        private static int SustituirEn(CampaignSaveManager gestor, Il2CppGeneric.List<GearID> lista, int razaContexto, string fuente, bool tienda = true)
        {
            if (lista == null || lista.Count == 0) return 0;
            var datos = gestor.SaveData;
            if (datos == null) return 0;
            int heroe = datos.heroID;
            var excluidos = new HashSet<int>();
            for (int i = 0; datos.Gear != null && i < datos.Gear.Count; i++) excluidos.Add((int)datos.Gear[i]);
            for (int i = 0; datos.SoldGear != null && i < datos.SoldGear.Count; i++) excluidos.Add((int)datos.SoldGear[i]);
            var cartas = new List<int>();
            for (int i = 0; i < lista.Count; i++) cartas.Add((int)lista[i]);
            var afines = Afinidades(heroe, razaContexto, out string detalle);
            // 0.7.4: misma rareza que la carta de serie y sin tocar la última carta de un cofre de tres (I8)
            // 0.7.4: same rarity as the default card and without touching the last card of a three-card chest (I8)
            int cambios = SorteoObjetos.Sustituir(cartas, _sorteo, afines, excluidos, heroe, gestor.GetSeededRandom(), ProbAfin, ProbGeneral, ModoPrueba,
                                                  RarezaDeCarta, SorteoObjetos.Protegidas(cartas.Count, tienda));
            if (cambios == 0) return 0;
            for (int i = 0; i < cartas.Count; i++) if ((int)lista[i] != cartas[i]) lista[i] = (GearID)cartas[i];
            _sorteosCambiados++;
            _cartasCambiadas += cambios;
            if (_sorteosCambiados <= 10 || _sorteosCambiados % 20 == 0)
                Plugin.Info($"Sorteo de objetos ({fuente}; {detalle}): {cambios} de {cartas.Count} carta(s) cambiadas -> [{string.Join(", ", cartas)}] " +
                            $"({_cartasCambiadas} cartas en {_sorteosCambiados} sorteos en la sesión).");
            return cambios;
        }

        /// <summary>Postfix de CampaignSaveManager.DrawRandomGear(int, bool): cambia cartas por objetos nuevos según la afinidad.</summary>
        /// Postfix of CampaignSaveManager.DrawRandomGear(int, bool): swaps cards for new gear according to affinity.
        public static void TrasDrawRandomGear(CampaignSaveManager __instance, bool __1, ref Il2CppGeneric.List<GearID> __result)
        {
            if (!ActivosEnCache || _enCiudad > 0) return;
            try { SustituirEn(__instance, __result, _contextoRaza, __1 ? "tienda" : (_contextoRaza >= 0 ? "botín con contexto" : "cofre, evento o botín"), __1); }
            catch (Exception e) { AvisoUnaVez("sorteo", "Sorteo de objetos (se queda el de serie): " + e); }
        }

        // Rareza (número del enum GearRarity) de una carta de serie, o -1 si no se puede leer (entonces no se cambia)
        // Rarity (GearRarity enum number) of a default card, or -1 if it cannot be read (then it is not swapped)
        private static int RarezaDeCarta(int id)
        {
            try
            {
                if (_porId.ContainsKey(id)) return (int)RarezaDe(id);
                var g = GearData.GetGear((GearID)id);
                return g == null ? -1 : (int)g.GearRarity;
            }
            catch { return -1; }
        }

        // Raza del ejército enemigo del save (Special si no hay)
        // Race of the enemy army in the save (Special if none)
        private static int RazaEnemiga()
        {
            try { return (int)SaveDataHandler.GetEnemyRace(); } catch { return -1; }
        }

        /// <summary>Prefix de EngagementPanel.AddLootBattlefield y CampaignSaveManager.GraveRobbers: contexto = raza enemiga.</summary>
        /// Prefix of EngagementPanel.AddLootBattlefield and CampaignSaveManager.GraveRobbers: context = enemy race.
        public static void AntesDeBotinEnemigo() { if (ActivosEnCache) _contextoRaza = RazaEnemiga(); }

        /// <summary>Finalizer de los dos anteriores: se olvida el contexto (también si el juego lanza una excepción).</summary>
        /// Finalizer of the previous two: the context is cleared (also if the game throws an exception).
        public static Exception FinBotinEnemigo(Exception __exception) { _contextoRaza = -1; return __exception; }

        /// <summary>Prefix de CampaignSaveManager.GenerateTown: el sorteo de su botín se rehace en el postfix.</summary>
        /// Prefix of CampaignSaveManager.GenerateTown: the draw of its loot is redone in the postfix.
        public static void AntesDeGenerateTown() => _enCiudad++;

        /// <summary>Postfix de GenerateTown: con la raza de la ciudad ya decidida, cambia las cartas de su botín.</summary>
        /// Postfix of GenerateTown: with the town's race already decided, swaps the cards of its loot.
        public static void TrasGenerateTown(CampaignSaveManager __instance)
        {
            if (!ActivosEnCache) return;
            try
            {
                var ciudad = __instance.SaveData?.townData;
                if (ciudad == null) return;
                // Revisión 0.7.4 (ronda 2): el botín de ciudad no es tienda, así que la tercera carta de un botín de tres queda protegida
                // Review 0.7.4 (round 2): town loot is not a shop, so the third card of a three-card loot is protected
                SustituirEn(__instance, ciudad.townLootGearIDs, (int)ciudad.townRace, "saqueo de ciudad", false);
            }
            catch (Exception e) { AvisoUnaVez("ciudad", "Sorteo de objetos de la ciudad (se queda el de serie): " + e.Message); }
        }

        /// <summary>Finalizer de GenerateTown: termina el modo ciudad pase lo que pase.</summary>
        /// Finalizer of GenerateTown: ends town mode no matter what.
        public static Exception FinGenerateTown(Exception __exception) { if (_enCiudad > 0) _enCiudad--; return __exception; }

        // -------------------------------------------------------------------
        // Sección: apoyo a la autoprueba (estado forzado, sin partida)
        // Section: self-test support (forced state, no run)
        // -------------------------------------------------------------------
        /// <summary>Solo autoprueba: pone las reglas de estos objetos para este héroe (null = vuelve al estado real del juego).</summary>
        /// Self-test only: sets the rules of these gear items for this hero (null = returns to the game's real state).
        internal static void ForzarParaPrueba(int heroe, IEnumerable<int> ids)
        {
            _forzado = ids == null ? null : new EstadoReglas { Heroe = heroe, Ids = ids.OrderBy(i => i).ToList() };
            var e = _forzado ?? (ActivosEnCache ? CalcularEstado() : new EstadoReglas());
            RecargarReglas(e);
        }
    }
}
