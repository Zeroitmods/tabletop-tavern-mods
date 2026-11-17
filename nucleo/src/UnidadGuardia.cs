// UnidadGuardia.cs
// «Fábrica» de unidades nuevas (diseño §6.2): da de alta en el catálogo del juego todas las unidades del manifiesto
// fusionado de los módulos de contenido ACTIVOS (0.7.0: Nucleo.cs y Modulos.cs; antes, un único mod oficial). Por unidad:
//   - Copia los SquadStats y SquadAssets de su unidad base con unitName = id nuevo. Los stats definitivos los pone el
//     mod oficial (unit_overrides.json con los 30 rasgos y los 7 datos ocultos), que el juego aplica después.
//   - Escribe en sus assets la raza del manifiesto si la unidad está activa y es una raza del juego (las sueltas de
//     una facción con base de otra, p. ej. los Ogros de Asedio de la Legión de Hierro clonan el Oni de Sakura).
//   - Registra como visual «de serie» la carpeta de su unidad base (si no, saldría invisible en batalla). La 900
//     además tiene su modelo propio (ModeloGuardia.cs).
//   - Listas de raza (UnitsOfRaceDictionary, de donde salen tienda, ciudades, recompensas y ejércitos enemigos):
//     las activas con reclutamiento «raza» entran en la de su facción, ordenadas por rango; las demás no están en
//     ninguna. La 900 sigue fuera de todas y la reparte Reclutamiento.cs solo al jugador, como en la 0.3.0.
//   - Agranda el blob de stats que lee Burst para que Stats[id] exista durante la batalla para todas.
// Desde la 0.5.0 (F2) las de la facción A (40) e facción B (41) van en la lista de su raza nueva (salvo la
// caballería con jinete, solo en datos) si las facciones nuevas están activas (FaccionesNuevas.cs); si no, se quedan solo
// en datos como en la F1. Con la F3 (Facciones.GuardiaEnAstra) la 900 pasa a la raza 40: entra en su lista (también
// para la IA) y deja de repartirse en las ofertas del jugador.
// Solo actúa con las unidades de los módulos activos en el menú de mods del juego (sin ninguno, el catálogo queda de serie).
// La 900 (facción A) viene con el módulo facción A: sin él no existe.
// El nombre del fichero y de la clase se mantiene de cuando era solo la facción A (900).
// UnidadGuardia.cs
// "Factory" of new units (design §6.2): registers in the game's catalog all the units from the merged manifest
// of the ACTIVE content modules (0.7.0: Nucleo.cs and Modulos.cs; before, a single official mod). Per unit:
//   - Copies the SquadStats and SquadAssets of its base unit with unitName = new id. The final stats are set by the
//     official mod (unit_overrides.json with the 30 traits and the 7 hidden values), which the game applies afterwards.
//   - Writes the manifest's race into its assets if the unit is active and is a game race (the loose ones of
//     a faction with another's base, e.g. the Siege Ogres of the Iron Legion, clone Sakura's Oni).
//   - Registers the folder of its base unit as the "default" visual (otherwise it would be invisible in battle). The 900
//     also has its own model (ModeloGuardia.cs).
//   - Race lists (UnitsOfRaceDictionary, where the shop, cities, rewards and enemy armies come from):
//     the active ones with "race" recruitment go into their faction's list, sorted by rank; the others are in
//     none. The 900 remains outside all of them and Reclutamiento.cs hands it out only to the player, as in 0.3.0.
//   - Enlarges the stats blob that Burst reads so that Stats[id] exists during battle for all of them.
// Since 0.5.0 (F2) those of faction A (40) and faction B (41) go into the list of their new race (except
// cavalry with rider, data only) if the new factions are active (FaccionesNuevas.cs); otherwise, they stay data-only
// as in F1. With F3 (Facciones.GuardiaEnAstra) the 900 moves to race 40: it enters its list (also
// for the AI) and is no longer handed out in the player's offers.
// It only acts on units from the modules active in the game's mod menu (with none, the catalog stays default).
// The 900 (faction A) comes with the faction A module: without it, it doesn't exist.
// The file and class name are kept from when it was only faction A (900).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TabletopTavern.GpuAnim;
using TJ;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: una unidad nueva con sus datos ya resueltos contra los enums del juego
    // Section: a new unit with its data already resolved against the game's enums
    // -----------------------------------------------------------------------
    internal sealed class UnidadNueva
    {
        public Manifiesto.Unidad Datos;
        public UnitName Id;
        public UnitName Base;
        public Race? Raza;             // raza que se escribe en sus assets (null = la de su base)
                                       // race written into its assets (null = that of its base)
        public bool EnLista;           // entra en UnitsOfRaceDictionary[Raza]
                                       // enters UnitsOfRaceDictionary[Raza]
        // Lo que dice el manifiesto sin la F3 (0.6.1): a esto vuelve la 900 si la raza 40 deja de ser jugable
        // What the manifest says without F3 (0.6.1): this is what 900 reverts to if race 40 stops being playable
        public Race? RazaManifiesto;
        public bool EnListaManifiesto;
        public int Numero => (int)Id;
    }

    // -----------------------------------------------------------------------
    // Sección: manifiesto, identidad de la 900 y comprobación del mod oficial
    // Section: manifest, identity of 900 and official mod check
    // -----------------------------------------------------------------------
    public static class UnidadGuardia
    {
        // la facción A (900): constantes que usan las piezas propias de la 900 (reclutamiento en ofertas,
        // sonido, modelo, estela, prestigio)
        // faction A (900): constants used by the 900's own pieces (recruitment in offers,
        // sound, model, trail, prestige)
        public const int ID = 900;
        public static readonly UnitName Guardia = (UnitName)ID;
        public static readonly UnitName Base = UnitName.EisenmannRegiment;

        // Mod antiguo de «Ejército reforzado» (retirado en la 0.4.0; si sigue activo, se avisa)
        // Old "Ejército reforzado" mod (removed in 0.4.0; if still active, a warning is shown)
        public const string CarpetaModViejo = "ZeroMunicionAlcance";

        // Número de EisenmannRegiment en el enum (lo que escribe JsonUtility); lo usa la autoprueba
        // EisenmannRegiment number in the enum (what JsonUtility writes); used by the self-test
        public static int OrdinalBase => (int)Base;

        // Id más alta que se recorre al reconstruir el diccionario en el plan B del finalizer
        // Highest id iterated when rebuilding the dictionary in the finalizer's plan B
        private const int MaxIdRecorrido = 4095;

        // Unidades del manifiesto ya resueltas (id -> unidad) y su tabla para el saneado de saves
        // Manifest units already resolved (id -> unit) and their table for save sanitizing
        private static readonly Dictionary<int, UnidadNueva> _porId = new Dictionary<int, UnidadNueva>();
        private static List<UnidadNueva> _todas = new List<UnidadNueva>();
        internal static Manifiesto Manifiesto { get; private set; } = Manifiesto.Vacio("sin cargar");
        internal static IReadOnlyList<UnidadNueva> Todas => _todas;
        internal static int MaxId { get; private set; } = ID;
        internal static bool HayCaballeria { get; private set; }

        // Ids de relleno metidos temporalmente en el diccionario durante LoadAndInjectData
        // (LoadAndInjectData solo se llama en el hilo principal y no se anida: un estado estático basta)
        // Filler ids temporarily inserted into the dictionary during LoadAndInjectData
        // (LoadAndInjectData is only called on the main thread and is not nested: static state is enough)
        private static readonly List<int> _rellenos = new List<int>();

        // Caché de la lista de mods activos (evita leer modlist.json en cada llamada)
        // Cache of the active mods list (avoids reading modlist.json on every call)
        private static HashSet<string> _modsActivosCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _rutasModsCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static DateTime _modsActivosHasta = DateTime.MinValue;

        /// <summary>
        /// Toma el manifiesto fusionado de los módulos activos (Nucleo.Aplicar; vale al cargar el plugin, sin llamar al
        /// juego), resuelve las unidades base contra el enum UnitName y prepara la tabla del saneado de saves. Se puede
        /// llamar otra vez con otro manifiesto (autoprueba con módulos simulados): todo se rehace desde cero.
        /// Takes the merged manifest of the active modules (Nucleo.Aplicar; valid when the plugin loads, without calling the
        /// game), resolves the base units against the UnitName enum and prepares the save sanitizing table. It can be
        /// called again with a different manifest (self-test with simulated modules): everything is rebuilt from scratch.
        /// </summary>
        public static void CargarManifiesto(Manifiesto m)
        {
            m ??= Manifiesto.Vacio("nulo");
            // F2: razas 40/41 y héroes 901-904 según la configuración (las unidades necesitan saber si sus razas existen)
            // F2: races 40/41 and heroes 901-904 according to the configuration (units need to know whether their races exist)
            FaccionesNuevas.Configurar(m);
            // 0.7.4: hechizos propios de los héroes nuevos activos (Hechizos.cs); necesita saber qué héroes hay
            // 0.7.4: custom spells of the active new heroes (Hechizos.cs); needs to know which heroes exist
            Hechizos.Configurar(m);
            // 0.6.0: objetos nuevos (gear) y su tabla del saneado de saves
            // 0.6.0: new gear and its save sanitizing table
            Objetos.Configurar(m);
            var lista = new List<UnidadNueva>();
            foreach (var u in m.Unidades)
            {
                if (u.Id < 900 || u.Id > 999) { Plugin.Aviso($"Manifiesto: id {u.Id} fuera del rango reservado 900-999; se ignora."); continue; }
                if (!Enum.TryParse(u.Base, out UnitName baseEnum) || !Enum.IsDefined(typeof(UnitName), baseEnum))
                { Plugin.Aviso($"Manifiesto: la unidad {u.Id} tiene una base desconocida '{u.Base}'; se ignora."); continue; }
                Race? raza = null;
                if (!string.IsNullOrEmpty(u.Raza) && Enum.TryParse(u.Raza, out Race r) && (Enum.IsDefined(typeof(Race), r) || FaccionesNuevas.EsNueva(r))) raza = r;
                bool porRaza = string.Equals(u.Reclutamiento, Manifiesto.ReclutaPorRaza, StringComparison.OrdinalIgnoreCase);
                Race? razaManifiesto = raza;
                bool enListaManifiesto = u.Activa && raza.HasValue && porRaza;
                // F3: la facción A (900) pasa a la facción A (raza 40, lista de la raza, también para la IA)
                // (en cada recarga AjustarGuardiaF3 la devuelve a las ofertas si la raza 40 no es jugable)
                // F3: faction A (900) moves to faction A (race 40, race list, also for the AI)
                // (on each reload AjustarGuardiaF3 returns it to the offers if race 40 is not playable)
                if (u.Id == ID && u.Activa && FaccionesNuevas.GuardiaEnAstraPedida) { raza = (Race)40; porRaza = true; }
                bool enLista = u.Activa && raza.HasValue && porRaza;
                if (u.Activa && !raza.HasValue && porRaza)
                {
                    // Raza 40/41 con las facciones nuevas desactivadas: solo en datos, como en la F1 (sin aviso por cada una)
                    // Race 40/41 with the new factions disabled: data only, as in F1 (no warning for each one)
                    if (!(int.TryParse(u.Raza, out int num) && num >= 40)) Plugin.Aviso($"Manifiesto: la unidad {u.Id} está activa pero su raza '{u.Raza}' no existe en el juego; no se podrá reclutar.");
                }
                lista.Add(new UnidadNueva { Datos = u, Id = (UnitName)u.Id, Base = baseEnum, Raza = u.Activa ? raza : null, EnLista = enLista,
                                            RazaManifiesto = u.Activa ? razaManifiesto : null, EnListaManifiesto = enListaManifiesto });
            }
            Manifiesto = m;
            _todas = lista;
            _porId.Clear();
            foreach (var u in lista) _porId[u.Numero] = u;
            MaxId = lista.Count == 0 ? 0 : lista.Max(u => u.Numero);
            HayCaballeria = lista.Any(u => u.Datos.Caballeria);
            Saves.Tabla = new TablaUnidades(lista.ToDictionary(u => u.Numero, u => (int)u.Base)) { Disponible = Registrada };
            Plugin.Info($"Manifiesto de unidades ({m.Origen}): {lista.Count} unidades, {lista.Count(u => u.Datos.Activa)} activas " +
                        $"({lista.Count(u => u.EnLista)} en listas de raza, {lista.Count(u => u.EnLista && FaccionesNuevas.EsNueva(u.Raza.Value))} de ellas en las razas nuevas), " +
                        $"{lista.Count(u => !u.EnLista)} fuera de toda lista; {m.Razas.Count} razas y {m.Heroes.Count} héroes en el manifiesto " +
                        $"(facciones nuevas {(FaccionesNuevas.Activas ? "activas" : "desactivadas")}).");
        }

        // Consultas rápidas sobre el manifiesto
        // Quick queries on the manifest
        internal static bool EsNueva(UnitName u) => _porId.ContainsKey((int)u);
        internal static UnidadNueva Datos(UnitName u) => _porId.TryGetValue((int)u, out var n) ? n : null;
        /// <summary>Unidad base de una unidad nueva (o la propia unidad si no es nueva).</summary>
        /// Base unit of a new unit (or the unit itself if it is not new).
        public static UnitName BaseDe(UnitName u) => _porId.TryGetValue((int)u, out var n) ? n.Base : u;

        /// <summary>¿La 900 está activa y se reparte en las ofertas del jugador? (modo de la 0.3.0)</summary>
        /// Is 900 active and distributed in the player's offers? (0.3.0 mode)
        internal static bool GuardiaEnOfertas =>
            !FaccionesNuevas.GuardiaEnAstra && _porId.TryGetValue(ID, out var g) && g.Datos.Activa &&
            string.Equals(g.Datos.Reclutamiento, Manifiesto.ReclutaOfertasJugador, StringComparison.OrdinalIgnoreCase);

        /// <summary>Nombres de las carpetas de mods activas en el orden de carga del juego (caché de 3 s).</summary>
        /// Names of the active mod folders in the game's load order (3 s cache).
        internal static HashSet<string> ModsActivos()
        {
            if (DateTime.UtcNow < _modsActivosHasta) return _modsActivosCache;
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rutas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                Il2CppGeneric.List<string> carpetas = ModLoadOrder.GetEnabledModFolderPathsInOrder();
                for (int i = 0; carpetas != null && i < carpetas.Count; i++)
                {
                    if (carpetas[i] == null) continue;
                    string nombre = Path.GetFileName(carpetas[i].TrimEnd('\\', '/'));
                    nombres.Add(nombre);
                    rutas[nombre] = carpetas[i];
                }
            }
            catch (Exception e) { Plugin.Aviso("No se pudo leer la lista de mods activos: " + e.Message); }
            _modsActivosCache = nombres;
            _rutasModsCache = rutas;
            _modsActivosHasta = DateTime.UtcNow.AddSeconds(3);
            return nombres;
        }

        /// <summary>Ruta completa de un mod activo por el nombre de su carpeta (null si no está activo).</summary>
        /// Full path of an active mod by its folder name (null if it is not active).
        internal static string RutaModActivo(string nombre)
        {
            ModsActivos();
            return _rutasModsCache.TryGetValue(nombre, out string r) ? r : null;
        }

        /// <summary>True si algún módulo de contenido está aplicado (0.7.0; antes, «el mod oficial está activo»).</summary>
        /// True if any content module is applied (0.7.0; previously, "the official mod is active").
        public static bool ContenidoActivo() => Nucleo.Actual.HayContenido;

        // ---------------------------------------------------------------
        // Alta en los diccionarios de stats y assets (idempotente)
        // Registration in the stats and assets dictionaries (idempotent)
        // ---------------------------------------------------------------
        /// <summary>Crea las unidades del manifiesto que falten copiando su base. Devuelve cuántas ha creado ahora.</summary>
        /// Creates the missing manifest units by copying their base. Returns how many it has created now.
        public static int AsegurarAlta(Il2CppGeneric.Dictionary<UnitName, SquadStats> stats,
                                       Il2CppGeneric.Dictionary<UnitName, SquadAssets> assets, string origen)
        {
            if (!Plugin.Operativo || stats == null || assets == null || _todas.Count == 0) return 0;
            // Sin ninguna unidad nueva aún en el catálogo, solo se crean si el mod oficial está activo
            // With no new unit in the catalog yet, they are only created if the official mod is active
            bool algunaYa = false;
            foreach (var u in _todas) if (stats.ContainsKey(u.Id)) { algunaYa = true; break; }
            if (!algunaYa && !ContenidoActivo()) return 0;
            int creadas = 0, sinBase = 0;
            foreach (var u in _todas)
            {
                // OJO: con los diccionarios genéricos del juego se usa ContainsKey + indexador, nunca TryGetValue:
                // el parámetro out de un struct en un genérico de interop cerró el juego en la primera prueba (16-10-2026)
                // WARNING: with the game's generic dictionaries use ContainsKey + indexer, never TryGetValue:
                // the out parameter of a struct in an interop generic crashed the game in the first test (16-10-2026)
                if (!stats.ContainsKey(u.Id))
                {
                    if (!stats.ContainsKey(u.Base) || !assets.ContainsKey(u.Base)) { sinBase++; continue; }
                    Traza(origen, $"clonando {u.Base} como {u.Numero}");
                    SquadStats plantilla = stats[u.Base];
                    plantilla.unitName = u.Id;
                    stats[u.Id] = plantilla;
                    creadas++;
                }
                // Los assets se aseguran SIEMPRE y por separado de los stats (si se perdieran o copiaran mal, la UI peta)
                // Assets are ALWAYS ensured, separately from stats (if they were lost or copied incorrectly, the UI breaks)
                AssetsGuardia.AsegurarAssets(assets, u, origen);
            }
            if (creadas > 0)
                Plugin.Info($"[{origen}] {creadas} unidades nuevas registradas (clon de su unidad base); en el catálogo: " +
                            $"{_todas.Count(u => stats.ContainsKey(u.Id))}/{_todas.Count}. Stats definitivos: unit_overrides.json del mod oficial.");
            if (sinBase > 0) Plugin.Aviso($"[{origen}] {sinBase} unidades nuevas esperan a que su unidad base esté en el catálogo.");
            return creadas;
        }

        // Traza de diagnóstico paso a paso (solo con Diagnostico.Trazas = true en la configuración)
        // Step-by-step diagnostic trace (only with Diagnostico.Trazas = true in the configuration)
        internal static void Traza(string origen, string paso)
        {
            if (Plugin.CfgTrazas != null && Plugin.CfgTrazas.Value) Plugin.Info($"[traza {origen}] {paso}");
        }

        // Postfix de TabletopTavernData.LoadStatsFromSOs: justo después de cargar los ScriptableObjects y antes de
        // RemoteBalance y de los mods (así unit_overrides.json puede modificar las unidades nuevas)
        // Postfix of TabletopTavernData.LoadStatsFromSOs: right after loading the ScriptableObjects and before
        // RemoteBalance and the mods (so unit_overrides.json can modify the new units)
        public static void TrasLoadStatsFromSOs(TabletopTavernData __instance)
        {
            Plugin.TrazaUnaVez("TrasLoadStatsFromSOs");
            try { AsegurarAlta(__instance.SquadStatsDictionary, __instance.SquadAssetsDictionary, "LoadStatsFromSOs"); }
            catch (Exception e) { Plugin.Error("TrasLoadStatsFromSOs: " + e); }
            // F2: RaceData de 40/41 justo después de que el juego cargue los suyos (antes de que nada lo pida)
            // F2: RaceData of 40/41 right after the game loads its own (before anything requests it)
            try { FaccionesNuevas.AsegurarRaceData(__instance, "LoadStatsFromSOs"); }
            catch (Exception e) { Plugin.Error("RaceData de las razas nuevas (LoadStatsFromSOs): " + e); }
        }

        // Prefix de SquadStatsOverrideLoader.ApplyFile (respaldo si el postfix anterior no llegara a ejecutarse)
        // Prefix of SquadStatsOverrideLoader.ApplyFile (fallback in case the previous postfix does not get to run)
        public static void AntesDeApplyFile(Il2CppGeneric.Dictionary<UnitName, SquadStats> squadStatsDictionary,
                                            Il2CppGeneric.Dictionary<UnitName, SquadAssets> squadAssetsDictionary)
        {
            Plugin.TrazaUnaVez("AntesDeApplyFile");
            try { AsegurarAlta(squadStatsDictionary, squadAssetsDictionary, "ApplyFile"); }
            catch (Exception e) { Plugin.Error("AntesDeApplyFile: " + e); }
        }

        // Prefix de SquadStatsOverrideLoader.ApplyOverridesFromModFolder (segundo respaldo, por cada mod)
        // Prefix of SquadStatsOverrideLoader.ApplyOverridesFromModFolder (second fallback, per mod)
        public static void AntesDeApplyOverridesFromModFolder(Il2CppGeneric.Dictionary<UnitName, SquadStats> squadStatsDictionary,
                                                              Il2CppGeneric.Dictionary<UnitName, SquadAssets> squadAssetsDictionary)
        {
            Plugin.TrazaUnaVez("AntesDeApplyOverridesFromModFolder");
            try { AsegurarAlta(squadStatsDictionary, squadAssetsDictionary, "ApplyOverridesFromModFolder"); }
            catch (Exception e) { Plugin.Error("AntesDeApplyOverridesFromModFolder: " + e); }
        }

        // ---------------------------------------------------------------
        // Listas de raza (UnitsOfRaceDictionary): quién se recluta y en qué facción
        // Race lists (UnitsOfRaceDictionary): who can be recruited and in which faction
        // ---------------------------------------------------------------
        /// <summary>
        /// Mete cada unidad activa «por raza» en la lista de su facción (por rango, como el juego) y saca de todas
        /// las listas a las demás (la 900 y las solo-datos). Devuelve cuántas quedan en listas.
        /// Puts each active unit "by race" into its faction's list (by rank, like the game) and removes the others from all
        /// lists (900 and the data-only ones). Returns how many remain in lists.
        /// </summary>
        public static int AsegurarListas(TabletopTavernData data, string origen)
        {
            var dic = data.UnitsOfRaceDictionary;
            if (dic == null || _todas.Count == 0) return 0;
            // Razas del juego y, si están activas, las nuevas 40/41 (Enum.GetValues no las ve)
            // Game races and, if active, the new 40/41 (Enum.GetValues does not see them)
            var razas = Enum.GetValues(typeof(Race)).Cast<Race>().Concat(FaccionesNuevas.RazasNuevas).Distinct().ToList();
            int metidas = 0, enListas = 0, quitadas = 0;
            foreach (var u in _todas)
            {
                bool registrada = data.SquadStatsDictionary.ContainsKey(u.Id);
                // Fuera de cualquier lista que no sea la suya (o de todas si no debe reclutarse)
                // Out of any list that is not its own (or of all of them if it must not be recruited)
                foreach (Race r in razas)
                {
                    if (u.EnLista && registrada && r == u.Raza.Value) continue;
                    if (!dic.ContainsKey(r)) continue;
                    var otra = dic[r];
                    while (otra.Contains(u.Id)) { otra.Remove(u.Id); quitadas++; }
                }
                if (!u.EnLista || !registrada) continue;
                Race raza = u.Raza.Value;
                if (!dic.ContainsKey(raza)) dic[raza] = new Il2CppGeneric.List<UnitName>();
                var lista = dic[raza];
                if (!lista.Contains(u.Id))
                {
                    // Inserción ordenada por rango (detrás de las de su mismo rango), sin delegados de interop
                    // Rank-ordered insertion (after those of the same rank), without interop delegates
                    int rango = data.GetUnitTierFromUnitName(u.Id);
                    int pos = lista.Count;
                    for (int i = 0; i < lista.Count; i++)
                        if (data.GetUnitTierFromUnitName(lista[i]) > rango) { pos = i; break; }
                    lista.Insert(pos, u.Id);
                    metidas++;
                }
                enListas++;
            }
            if (metidas > 0 || quitadas > 0)
                Plugin.Info($"[{origen}] Listas de raza: {metidas} unidades nuevas añadidas a su facción, {quitadas} retiradas; " +
                            $"{enListas} reclutables por raza (jugador e IA).");
            return enListas;
        }

        // ---------------------------------------------------------------
        // Visual de batalla: carpeta de serie de la unidad base registrada para cada id nuevo
        // Battle visual: stock folder of the base unit registered for each new id
        // ---------------------------------------------------------------
        /// <summary>Registra la carpeta de serie de la base como visual «de serie» de cada unidad nueva (idempotente).</summary>
        /// Registers the base's stock folder as the "stock" visual of each new unit (idempotent).
        public static void AsegurarVisual(string origen)
        {
            if (!Plugin.Operativo || !AlgunaRegistrada()) return;
            int hechas = 0, pendientes = 0;
            foreach (var u in _todas)
            {
                if (!Registrada(u.Numero)) continue;
                string clave = u.Id.ToString();   // "941": es lo que busca el juego con unitName.ToString()
                                                  // "941": it is what the game looks for with unitName.ToString()
                if (UnitVisualOverrides.TryGetBuiltIn(clave, out string ya) && !string.IsNullOrEmpty(ya)) continue;
                if (!UnitVisualOverrides.TryGetBuiltIn(u.Base.ToString(), out string carpeta) || string.IsNullOrEmpty(carpeta)) { pendientes++; continue; }
                UnitVisualOverrides.RegisterBuiltIn(clave, carpeta);
                hechas++;
            }
            if (hechas > 0) Plugin.Info($"[{origen}] Visual de serie registrado para {hechas} unidades nuevas (modelo de su unidad base).");
            if (pendientes > 0) Plugin.Aviso($"[{origen}] {pendientes} unidades nuevas sin carpeta de serie de su base todavía; se reintentará.");
        }

        // Postfix de UnitVisualOverrideLoader.ApplyOverridesFromModFolder (se ejecuta tras cada mod)
        // (__0 = modFolderPath: la carpeta del mod que el juego acaba de cargar; sirve para registrar el modelo propio)
        // Postfix of UnitVisualOverrideLoader.ApplyOverridesFromModFolder (runs after each mod)
        // (__0 = modFolderPath: the mod folder the game has just loaded; used to register the custom model)
        public static void AsegurarVisualPostfix(string __0)
        {
            Plugin.TrazaUnaVez("AsegurarVisualPostfix");
            try { AsegurarVisual("UnitVisuals"); } catch (Exception e) { Plugin.Error("AsegurarVisualPostfix: " + e); }
            try { ModeloGuardia.Asegurar("UnitVisuals", __0); } catch (Exception e) { Plugin.Error("ModeloGuardia (UnitVisuals): " + e); }
        }

        // Postfix de TabletopTavernData.ReloadData: alta, listas de raza, visual, stats y autoprueba
        // (el escalado de «Ejército reforzado» va en su propio postfix, de una instancia de Harmony aparte, que se
        // ejecuta después de este: EjercitoReforzado.TrasRecarga)
        // Postfix of TabletopTavernData.ReloadData: registration, race lists, visual, stats and self-test
        // (the "Reinforced Army" scaling goes in its own postfix, from a separate Harmony instance, which
        // runs after this one: EjercitoReforzado.TrasRecarga)
        public static void TrasReloadData(TabletopTavernData __instance)
        {
            Plugin.TrazaUnaVez("TrasReloadData");
            try
            {
                AsegurarAlta(__instance.SquadStatsDictionary, __instance.SquadAssetsDictionary, "ReloadData");
                // F2: RaceData de las razas nuevas antes de las listas (la piden banderas y mapa)
                // F2: RaceData of the new races before the lists (banners and map request it)
                try { FaccionesNuevas.AsegurarRaceData(__instance, "ReloadData"); } catch (Exception e) { Plugin.Error("RaceData de las razas nuevas (ReloadData): " + e); }
                try { AsegurarListas(__instance, "ReloadData"); } catch (Exception e) { Plugin.Error("Listas de raza: " + e); }
                // F2/F3 (0.5.1): razas jugables en esta recarga, la 900 según la raza 40 y anfitriones de la Marcha
                // F2/F3 (0.5.1): playable races in this reload, 900 according to race 40 and March hosts
                try { ComprobarRazasNuevas(__instance); } catch (Exception e) { Plugin.Error("Comprobación de las razas nuevas: " + e); }
                try { AjustarGuardiaF3(__instance); } catch (Exception e) { Plugin.Error("F3 (Guardia Imperial en Astra Militarum): " + e); }
                try { FaccionesNuevas.TrasRecarga(__instance, "ReloadData"); } catch (Exception e) { Plugin.Error("Facciones nuevas (ReloadData): " + e); }
                // F2: héroes (por si HeroData se cargó antes que el plugin), sus assets y los retratos propios
                // F2: heroes (in case HeroData loaded before the plugin), their assets and the custom portraits
                if (FaccionesNuevas.Activas && ContenidoActivo())
                {
                    try { HeroesNuevos.AsegurarHeroes("ReloadData"); } catch (Exception e) { Plugin.Error("Héroes nuevos (ReloadData): " + e); }
                    try { HeroesNuevos.AsegurarAssetsHeroes(__instance, "ReloadData"); } catch (Exception e) { Plugin.Error("Assets de héroes nuevos (ReloadData): " + e); }
                    try { HeroesNuevos.CargarRetratos(); } catch (Exception e) { Plugin.Error("Retratos de héroes nuevos: " + e); }
                }
                // 0.7.7 (arreglo del 03-11): cachés del estandarte (iconos de serie y PNG de los módulos) aquí, fuera de los
                // enganches de ciclo de vida de Unity, después de héroes y retratos (revisión del arreglo, hallazgo 1)
                // 0.7.7 (fix of 03-11): banner caches (stock icons and module PNGs) here, outside the
                // Unity lifecycle hooks, after heroes and portraits (review of the fix, finding 1)
                try { Estandarte.Preparar(__instance); } catch (Exception e) { Plugin.Error("Estandarte (ReloadData): " + e); }
                // Escalado de «Ejército reforzado» antes de la autoprueba (idempotente: si ya se hizo en esta recarga no
                // hace nada; su propio postfix lo repite por si este parche se quitara)
                // "Reinforced Army" scaling before the self-test (idempotent: if already done in this reload it
                // does nothing; its own postfix repeats it in case this patch is removed)
                try { EjercitoReforzado.EscalarCatalogo(__instance, "ReloadData"); } catch (Exception e) { Plugin.Error("Ejército reforzado: " + e); }
                AsegurarVisual("ReloadData");
                try { ModeloGuardia.Asegurar("ReloadData", null); } catch (Exception e) { Plugin.Error("ModeloGuardia (ReloadData): " + e); }
                InformarStats(__instance, "ReloadData");
                AudioGuardia.AsegurarCargado("ReloadData");     // F3: WAV propios (en el menú, antes de la autoprueba)
                                                                // F3: custom WAVs (in the menu, before the self-test)
                try { Objetos.TrasRecarga("ReloadData"); } catch (Exception e) { Plugin.Error("Objetos nuevos (ReloadData): " + e); }   // 0.6.0: iconos
                                                                                                                                        // 0.6.0: icons
                Nucleo.ComprobarCoherencia();
                AutoPrueba.EjecutarUnaVez(__instance);
            }
            catch (Exception e) { Plugin.Error("TrasReloadData: " + e); }
        }

        // F2/F3: qué razas nuevas son jugables en esta recarga (mod oficial activo y unidades en los 4 rangos); solo esas
        // salen en sorteos, ciudades y Marcha (si a una le faltan rangos, la IA bajaría de rango y la tienda quedaría coja)
        // F2/F3: which new races are playable in this reload (official mod active and units in all 4 ranks); only those
        // appear in draws, cities and March (if one lacks ranks, the AI would drop in rank and the shop would be lopsided)
        private static string _ultimoResumenJugables;
        private static void ComprobarRazasNuevas(TabletopTavernData data)
        {
            if (!FaccionesNuevas.Activas) return;
            string resumen = FaccionesNuevas.ActualizarJugables(data, ContenidoActivo());
            if (resumen == _ultimoResumenJugables) return;
            _ultimoResumenJugables = resumen;
            if (FaccionesNuevas.RazasNuevas.All(FaccionesNuevas.Jugable)) Plugin.Info("Razas nuevas jugables: " + resumen + ".");
            else Plugin.Error("Razas nuevas NO jugables (no salen en sorteos ni en la Marcha): " + resumen + ".");
        }

        // F3 (diseño §4.6): la 900 va en la facción A solo si la raza 40 es jugable; si no, vuelve a su raza de serie y a
        // las ofertas del jugador. Si cambia, se rehacen sus listas y su entrada de assets (lleva la raza).
        // F3 (design §4.6): the 900 goes in faction A only if race 40 is playable; otherwise, it returns to its default race and to
        // the player's offers. If it changes, its lists and its assets entry (which carries the race) are rebuilt.
        private static void AjustarGuardiaF3(TabletopTavernData data)
        {
            if (!FaccionesNuevas.GuardiaEnAstraPedida || !_porId.TryGetValue(ID, out var g) || !g.Datos.Activa) return;
            bool enAstra = FaccionesNuevas.GuardiaEnAstra;
            bool ahora = g.Raza.HasValue && (int)g.Raza.Value == 40;
            if (enAstra == ahora) return;
            if (enAstra) { g.Raza = (Race)40; g.EnLista = true; }
            else
            {
                // Vuelve exactamente a lo que dice el manifiesto (raza de serie y reclutamiento); si el manifiesto no trae
                // una raza de serie válida no se inventa ninguna: se deja como está y se avisa
                // Goes back exactly to what the manifest says (stock race and recruitment); if the manifest does not provide
                // a valid stock race none is invented: it is left as is and a warning is issued
                if (!g.RazaManifiesto.HasValue || !Enum.IsDefined(typeof(Race), g.RazaManifiesto.Value))
                {
                    Plugin.Error($"F3: la raza 40 no es jugable, pero el manifiesto no da una raza de serie válida para la 900 ('{g.Datos.Raza}'); se deja como estaba.");
                    return;
                }
                g.Raza = g.RazaManifiesto;
                g.EnLista = g.EnListaManifiesto;
            }
            AsegurarListas(data, "F3");
            AssetsGuardia.AsegurarAssets(data.SquadAssetsDictionary, g, "F3");
            if (enAstra) Plugin.Info("F3: la Guardia Imperial vuelve a Astra Militarum (la raza 40 es jugable).");
            else Plugin.Aviso("F3: la raza 40 no es jugable en esta recarga; la Guardia Imperial vuelve a las ofertas del jugador (como en la 0.4.1).");
        }

        // Escribe en el log los stats finales de la 900 y avisa si siguen siendo los de Eisenmann
        // Writes the final stats of 900 to the log and warns if they are still Eisenmann's
        private static void InformarStats(TabletopTavernData data, string origen)
        {
            var dic = data.SquadStatsDictionary;
            if (!dic.ContainsKey(Guardia) || !dic.ContainsKey(Base)) return;
            SquadStats g = dic[Guardia];
            SquadStats e = dic[Base];
            Plugin.Info($"[{origen}] Stats 900: soldados {g.baseUnitCount}, vida {g.HitPointsPerUnit}, ataque {g.MeleeAttack}/{g.MeleeDefense}, " +
                        $"fuerza {g.WeaponStrength}, armadura {g.Armor}, moral {g.Leadership}, alcance {g.BaseRange}, precisión {g.attackAccuracy}, " +
                        $"daño distancia {g.MissileStrength}, munición {g.Ammunition}, perforante {g.SquadAttributes.ArmorPiercing}, rareza {g.RarityTier}");
            if (g.Armor == e.Armor && g.BaseRange == e.BaseRange && g.RarityTier == e.RarityTier)
                Plugin.Aviso("Los stats de la 900 son iguales a los de Eisenmann: el unit_overrides.json del mod oficial no se ha aplicado.");
        }

        /// <summary>True si la 900 está ahora mismo en el catálogo del juego.</summary>
        /// True if 900 is currently in the game's catalog.
        public static bool RegistradaEnCatalogo() => Registrada(ID);

        /// <summary>True si la unidad (número) está ahora mismo en el catálogo del juego.</summary>
        /// True if the unit (number) is currently in the game's catalog.
        public static bool Registrada(int id)
        {
            try
            {
                var data = TabletopTavernData.InstanceIfExists;
                return data != null && data.SquadStatsDictionary != null && data.SquadStatsDictionary.ContainsKey((UnitName)id);
            }
            catch { return false; }
        }

        /// <summary>True si alguna unidad del manifiesto está en el catálogo (para no trabajar en partidas sin ellas).</summary>
        /// True if any manifest unit is in the catalog (to avoid working in runs without them).
        public static bool AlgunaRegistrada()
        {
            try
            {
                var data = TabletopTavernData.InstanceIfExists;
                if (data == null || data.SquadStatsDictionary == null) return false;
                foreach (var u in _todas) if (data.SquadStatsDictionary.ContainsKey(u.Id)) return true;
                return false;
            }
            catch { return false; }
        }

        // Llamado al cargar el plugin: si el catálogo ya existía (plugin cargado tarde), se reconstruye una vez
        // Called when the plugin loads: if the catalog already existed (plugin loaded late), it is rebuilt once
        public static void RegistrarSiYaHayCatalogo()
        {
            try
            {
                var data = TabletopTavernData.InstanceIfExists;
                if (data == null) { Plugin.Info("Catálogo del juego aún no creado: las unidades se registrarán al construirse."); return; }
                if (AlgunaRegistrada()) return;
                if (data.SquadStatsDictionary.Count == 0) return;
                Plugin.Info("El catálogo ya existía al cargar el plugin: se reconstruye una vez para registrar las unidades nuevas.");
                data.ReloadData();
            }
            catch (Exception e) { Plugin.Error("RegistrarSiYaHayCatalogo: " + e); }
        }

        // ---------------------------------------------------------------
        // Blob de stats para Burst (LoadAndInjectData)
        // Stats blob for Burst (LoadAndInjectData)
        // ---------------------------------------------------------------
        // El juego reserva el blob con SquadStatsDictionary.Count y lo rellena con (UnitName)i para i < Count.
        // Las unidades nuevas quedarían fuera y Burst leería Stats[9xx] fuera del array. Antes de construirlo se meten
        // copias válidas de relleno para todos los ids que falten por debajo de la id nueva más alta (Count pasa a
        // MaxId + 1); después se quitan.
        // The game reserves the blob with SquadStatsDictionary.Count and fills it with (UnitName)i for i < Count.
        // The new units would be left out and Burst would read Stats[9xx] outside the array. Before building it, valid
        // filler copies are inserted for all the missing ids below the highest new id (Count becomes
        // MaxId + 1); afterwards they are removed.
        public static void AntesDeLoadAndInjectData(TabletopTavernData __instance)
        {
            _rellenos.Clear();
            try
            {
                if (!Plugin.Operativo) return;
                var dic = __instance.SquadStatsDictionary;
                AsegurarAlta(dic, __instance.SquadAssetsDictionary, "LoadAndInjectData");
                AsegurarVisual("LoadAndInjectData");
                if (!AlgunaRegistrada() || !dic.ContainsKey(Base)) return;
                // Id más alta realmente registrada (no se rellena por encima de ella)
                // Highest id actually registered (nothing is filled above it)
                int maxReg = 0;
                foreach (var u in _todas) if (dic.ContainsKey(u.Id)) maxReg = Math.Max(maxReg, u.Numero);
                SquadStats plantilla = dic[Base];
                for (int i = 0; i < maxReg; i++)
                {
                    var id = (UnitName)i;
                    if (dic.ContainsKey(id)) continue;
                    var relleno = plantilla;
                    relleno.unitName = id;
                    dic[id] = relleno;
                    _rellenos.Add(i);
                }
                if (dic.Count != maxReg + 1)
                    Plugin.Aviso($"Blob: el diccionario mide {dic.Count} y debería medir {maxReg + 1} (¿hay ids por encima de {maxReg}?).");
                else
                    Plugin.Info($"Blob de stats preparado: {dic.Count} entradas ({_rellenos.Count} de relleno temporales), hasta Stats[{maxReg}].");
            }
            catch (Exception e) { Plugin.Error("AntesDeLoadAndInjectData: " + e); }
        }

        // Postfix: comprobación (la limpieza va en el finalizer, que se ejecuta aunque el original falle)
        // Postfix: check (cleanup goes in the finalizer, which runs even if the original fails)
        public static void TrasLoadAndInjectData(TabletopTavernData __instance)
        {
            if (_rellenos.Count > 0) Plugin.Info("Blob de stats inyectado en ECS con las unidades nuevas.");
        }

        // Finalizer: quita los rellenos SIEMPRE (también si LoadAndInjectData lanzó una excepción)
        // Finalizer: ALWAYS removes the fillers (also if LoadAndInjectData threw an exception)
        public static Exception FinalizarLoadAndInjectData(TabletopTavernData __instance, Exception __exception)
        {
            try
            {
                if (_rellenos.Count == 0) return __exception;
                var dic = __instance.SquadStatsDictionary;
                int quitados = 0;
                try
                {
                    foreach (int i in _rellenos) if (dic.Remove((UnitName)i)) quitados++;
                }
                catch (Exception e)
                {
                    // Plan B si Remove no existiera en este build: vaciar y volver a meter solo las entradas reales
                    // Plan B if Remove does not exist in this build: clear and re-insert only the real entries
                    Plugin.Aviso("Remove no disponible (" + e.Message + "), se reconstruye el diccionario sin rellenos.");
                    var reales = new List<KeyValuePair<int, SquadStats>>();
                    var rellenos = new HashSet<int>(_rellenos);
                    // Se recorre bastante más allá de 999 por si un parche futuro del juego añadiera ids altas
                    // It scans well beyond 999 in case a future game patch adds high ids
                    for (int i = 0; i <= MaxIdRecorrido; i++)
                        if (!rellenos.Contains(i) && dic.ContainsKey((UnitName)i)) reales.Add(new KeyValuePair<int, SquadStats>(i, dic[(UnitName)i]));
                    dic.Clear();
                    foreach (var kv in reales) dic[(UnitName)kv.Key] = kv.Value;
                    quitados = _rellenos.Count;
                }
                Plugin.Info($"Rellenos del blob retirados: {quitados}/{_rellenos.Count}. Diccionario de stats: {dic.Count} unidades.");
                _rellenos.Clear();
            }
            catch (Exception e) { Plugin.Error("FinalizarLoadAndInjectData: " + e); }
            if (__exception != null) Plugin.Error("LoadAndInjectData lanzó una excepción del propio juego: " + __exception.Message);
            return __exception;
        }
    }
}
