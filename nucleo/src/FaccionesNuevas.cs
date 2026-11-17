// FaccionesNuevas.cs
// Fase F2 del diseño (notas internas (no publicadas) §6.7): las razas nuevas 40 (facción A) y 41 (Imperio
// Galáctico), jugables y enemigas. El enum Race del juego solo tiene sus 10 valores; un número fuera de él vale como
// raza en los diccionarios del juego, pero hay que cubrir todo lo que recorre el enum o espera datos por raza (§2.5):
//   - RaceData (colores, peana de las miniaturas y región): copia de una raza de serie con los colores propios,
//     registrada justo después de que el juego cargue los suyos (LoadStatsFromSOs) y en cada recarga. Sin ella
//     GetRaceData(40) devuelve null y las banderas de batalla y de ciudad, el mapa y la colección cierran el juego.
//     Además, un postfix de GetRaceData devuelve la copia si alguien la pidiera antes (red de seguridad).
//   - Enemigos: GenerateRaceForMap (actos 1-3: la raza entra en el sorteo de su acto), GenerateEndlessRace (actos
//     infinitos, reescrito con las razas nuevas en el bombo), MarchRules.HostRaces (anfitriones de la Marcha: se
//     sobrescribe el campo, que el juego calcula una sola vez) y GenerateTownRace (1 de cada 10 ciudades).
//   - Lo que no tienen propio y se presta de una raza de serie: región del mapa (GetMapRegion), música (temas por
//     número de raza en IAudioRequester), fondo de la carta de unidad, pasiva de batalla (Batalla.cs) y bonus de
//     campaña (Diezmo del Munitorum = Edicto Imperial en TownPanel; Tesorería Imperial = interés x2 en GoldManager).
//   - Color de la facción en la interfaz (GetRaceDisplayColor), que de serie cae en el gris de «Special».
//   - Temas de taberna: 40/41 no tienen; se quitan si el juego los añade al ganar en la máxima dificultad.
// Todo depende de la opción Facciones.Activadas (false = todo como en la F1: las razas no existen y sus unidades se
// quedan solo en datos). La F3 (la unidad 900 pasa a la facción A) va con Facciones.GuardiaEnAstra.
// 0.5.1: una raza solo entra en los sorteos de enemigos, ciudades y Marcha si es JUGABLE en esta recarga (Jugable):
// facciones activas, mod oficial activo y unidades reclutables de su raza en los 4 rangos. Se recalcula en cada
// recarga de datos. La F3 también depende de que la 40 sea jugable: si no, la 900 vuelve a las ofertas del jugador.
// Los héroes 901-904 están en HeroesNuevos.cs y la pantalla de héroes en PantallaHeroes.cs.
// FaccionesNuevas.cs
// Design phase F2 (internal notes (unpublished) §6.7): the new races 40 (faction A) and 41 (Galactic
// Empire), both playable and as enemies. The game's Race enum only has its 10 values; a number outside it works as a
// race in the game's dictionaries, but we must cover everything that iterates over the enum or expects per-race data (§2.5):
//   - RaceData (colors, miniature base and region): a copy of a stock race with its own colors,
//     registered right after the game loads its own (LoadStatsFromSOs) and on every reload. Without it
//     GetRaceData(40) returns null and the battle and city banners, the map and the Collection crash the game.
//     Also, a GetRaceData postfix returns the copy if anyone requests it earlier (safety net).
//   - Enemies: GenerateRaceForMap (acts 1-3: the race enters the draw for its act), GenerateEndlessRace (endless
//     acts, rewritten with the new races in the pool), MarchRules.HostRaces (March hosts: the field is
//     overwritten, since the game computes it only once) and GenerateTownRace (1 in every 10 cities).
//   - What they don't have of their own is borrowed from a stock race: map region (GetMapRegion), music (themes by
//     race number in IAudioRequester), unit card background, battle passive (Batalla.cs) and campaign
//     bonus (Munitorum Tithe = Imperial Edict in TownPanel; Imperial Treasury = interest x2 in GoldManager).
//   - Faction color in the interface (GetRaceDisplayColor), which by default falls back to the gray of «Special».
//   - Tavern themes: 40/41 have none; they are removed if the game adds them when winning on the highest difficulty.
// Everything depends on the Facciones.Activadas option (false = everything as in F1: the races don't exist and their units
// stay as data only). F3 (unit 900 moves to faction A) goes with Facciones.GuardiaEnAstra.
// 0.5.1: a race only enters the draws for enemies, cities and March if it is PLAYABLE in this reload (Jugable):
// factions enabled, official mod enabled and recruitable units of its race in all 4 ranks. It is recomputed on every
// data reload. F3 also depends on 40 being playable: if not, 900 goes back to the player's offers.
// Heroes 901-904 are in HeroesNuevos.cs and the heroes screen in PantallaHeroes.cs.

using System;
using System.Collections.Generic;
using System.Linq;
using TJ;
using UnityEngine;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: estado de las facciones nuevas (lo que dice el manifiesto y la configuración)
    // Section: state of the new factions (what the manifest and the configuration say)
    // -----------------------------------------------------------------------
    public static class FaccionesNuevas
    {
        // Razas nuevas activas del manifiesto (número -> datos) y héroes nuevos activos
        // Active new races from the manifest (number -> data) and active new heroes
        private static readonly Dictionary<int, Manifiesto.Raza> _razas = new Dictionary<int, Manifiesto.Raza>();
        private static readonly Dictionary<int, Manifiesto.Heroe> _heroes = new Dictionary<int, Manifiesto.Heroe>();
        // Copia de RaceData por raza nueva (se crea una vez por sesión y se vuelve a registrar en cada recarga)
        // RaceData copy per new race (created once per session and registered again on every reload)
        private static readonly Dictionary<int, RaceData> _datosRaza = new Dictionary<int, RaceData>();
        private static readonly HashSet<string> _avisos = new HashSet<string>();
        // Razas nuevas jugables en esta recarga (activas, mod oficial activo y sus 4 rangos con unidades)
        // New races playable in this reload (active, official mod active and their 4 ranks with units)
        private static readonly HashSet<int> _jugables = new HashSet<int>();

        /// <summary>true = las razas 40/41 y sus héroes están dados de alta en esta sesión (configuración y manifiesto).</summary>
        /// true = races 40/41 and their heroes are registered in this session (configuration and manifest).
        public static bool Activas { get; private set; }
        /// <summary>La configuración pide la F3 (Facciones.GuardiaEnAstra) y la raza 40 está activa en el manifiesto.</summary>
        /// The configuration requests F3 (Facciones.GuardiaEnAstra) and race 40 is active in the manifest.
        public static bool GuardiaEnAstraPedida { get; private set; }
        /// <summary>
        /// true = F3 en vigor: la 900 es de la facción A (lista de la raza 40, también para la IA) y ya no va en ofertas.
        /// Solo si la raza 40 es jugable en esta recarga (diseño §4.6: sin facción, modo anterior).
        /// true = F3 in effect: 900 belongs to faction A (race 40 list, also for the AI) and is no longer in offers.
        /// Only if race 40 is playable in this reload (design §4.6: no faction, previous mode).
        /// </summary>
        public static bool GuardiaEnAstra => GuardiaEnAstraPedida && Jugable((Race)40);

        /// <summary>
        /// Raza nueva jugable en esta recarga: facciones activas, mod oficial activo y unidades reclutables de su raza en
        /// los 4 rangos (lo calcula ActualizarJugables en cada recarga). Solo las jugables salen en sorteos y Marcha.
        /// New race playable in this reload: factions active, official mod active and recruitable units of its race in
        /// all 4 ranks (computed by ActualizarJugables on every reload). Only playable ones appear in draws and the March.
        /// </summary>
        public static bool Jugable(Race r) => Activas && _jugables.Contains((int)r);
        public static IEnumerable<Race> RazasJugables => _jugables.Where(k => Activas && _razas.ContainsKey(k)).Select(k => (Race)k);
        /// <summary>0.7.7: nombre de respaldo (manifiesto) de una raza nueva en español o inglés (null si no existe).</summary>
        /// 0.7.7: fallback name (manifest) of a new race in Spanish or English (null if it doesn't exist).
        public static string NombreEs(int raza) => _razas.TryGetValue(raza, out var r) ? r.NombreEs : null;
        public static string NombreEn(int raza) => _razas.TryGetValue(raza, out var r) ? r.NombreEn : null;
        /// <summary>Lo pone el plugin si no engancha un parche imprescindible de la F2: las facciones nuevas no se activan.</summary>
        /// Set by the plugin if an essential F2 patch fails to hook: the new factions are not activated.
        internal static bool ForzarDesactivadas;

        public static IReadOnlyDictionary<int, Manifiesto.Raza> Razas => _razas;
        public static IReadOnlyDictionary<int, Manifiesto.Heroe> Heroes => _heroes;
        public static bool EsNueva(Race r) => Activas && _razas.ContainsKey((int)r);
        public static bool EsNueva(int r) => Activas && _razas.ContainsKey(r);
        public static IEnumerable<Race> RazasNuevas => _razas.Keys.Select(k => (Race)k);
        public static bool EsHeroeNuevo(int heroID) => Activas && _heroes.ContainsKey(heroID);

        /// <summary>
        /// Lee razas y héroes del manifiesto según la configuración. Se llama al cargar el plugin, antes de resolver
        /// las unidades (que necesitan saber si las razas 40/41 existen) y prepara la tabla de razas del saneado.
        /// Reads races and heroes from the manifest according to the configuration. Called when the plugin loads, before resolving
        /// the units (which need to know whether races 40/41 exist) and prepares the sanitizing race table.
        /// </summary>
        public static void Configurar(Manifiesto m)
        {
            _razas.Clear();
            _heroes.Clear();
            Activas = false;
            GuardiaEnAstraPedida = false;
            _jugables.Clear();
            Saves.TablaRazas = null;
            if (ForzarDesactivadas) return;
            // 0.7.0: ya no hay opción Facciones.Activadas: las razas llegan solo de los módulos de facción activos
            // 0.7.0: the Facciones.Activadas option no longer exists: races come only from the active faction modules
            foreach (var r in m.Razas)
                if (r.Activa && r.Id >= 40 && !Enum.IsDefined(typeof(Race), r.Id)) _razas[r.Id] = r;
            foreach (var h in m.Heroes)
                if (h.Activa && _razas.ContainsKey(h.Raza)) _heroes[h.Id] = h;
            // Riesgo ALTO del diseño: con un solo héroe en una raza se cierra la tienda de emblemas. Una raza sin sus 2
            // héroes no se da de alta.
            // HIGH risk in the design: with a single hero in a race the emblem shop crashes. A race without its 2
            // heroes is not registered.
            foreach (var r in _razas.Values.ToList())
            {
                int n = _heroes.Values.Count(h => h.Raza == r.Id);
                if (n < 2)
                {
                    Plugin.Error($"Facciones nuevas: la raza {r.Id} tiene {n} héroe(s) activos en el manifiesto y necesita 2; no se da de alta.");
                    _razas.Remove(r.Id);
                    foreach (var h in _heroes.Values.Where(x => x.Raza == r.Id).ToList()) _heroes.Remove(h.Id);
                }
            }
            Activas = _razas.Count > 0;
            if (!Activas) { Plugin.Info("Facciones nuevas: ningún módulo de facción activo (Astra Militarum, Imperio Galáctico)."); return; }
            GuardiaEnAstraPedida = Plugin.CfgGuardiaEnAstra.Value && _razas.ContainsKey(40);
            // Tabla del saneado: raza nueva -> número de su raza sustituta de serie
            // Sanitizing table: new race -> number of its stock replacement race
            var sustitutas = new Dictionary<int, int>();
            foreach (var r in _razas.Values) sustitutas[r.Id] = (int)RazaSerie(r.RazaSustituta, Race.IronLegion);
            Saves.TablaRazas = new TablaRazas(sustitutas);
            Plugin.Info($"Facciones nuevas: razas {string.Join(", ", _razas.Values.Select(r => $"{r.Id} ({r.NombreEs})"))}; " +
                        $"héroes {string.Join(", ", _heroes.Keys)}; Guardia Imperial en Astra Militarum (F3) pedida: {GuardiaEnAstraPedida} (en vigor si la raza 40 es jugable).");
        }

        /// <summary>Raza de serie por su nombre del enum (o la de respaldo si no existe).</summary>
        /// Stock race by its enum name (or the fallback one if it doesn't exist).
        public static Race RazaSerie(string nombre, Race respaldo)
        {
            if (!string.IsNullOrEmpty(nombre) && Enum.TryParse(nombre, out Race r) && Enum.IsDefined(typeof(Race), r)) return r;
            return respaldo;
        }

        // Raza de serie que presta algo a una raza nueva (región, música, pasiva...) o la propia si no es nueva
        // Stock race that lends something to a new race (region, music, passive...) or the race itself if it is not new
        internal static Race Prestada(Race r, Func<Manifiesto.Raza, string> campo, Race respaldo)
        {
            if (!Activas || !_razas.TryGetValue((int)r, out var datos)) return r;
            return RazaSerie(campo(datos), respaldo);
        }

        // Aviso una sola vez por clave y sesión (para fallos dentro de parches que se ejecutan a menudo)
        // Warns only once per key and session (for failures inside patches that run often)
        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        // ---------------------------------------------------------------
        // RaceData de las razas nuevas (riesgo ALTO: GetRaceData nulo cierra el juego)
        // RaceData of the new races (HIGH risk: a null GetRaceData crashes the game)
        // ---------------------------------------------------------------
        /// <summary>Registra el RaceData de cada raza nueva si falta en el diccionario del juego. Devuelve cuántas faltaban.</summary>
        /// Registers the RaceData of each new race if missing from the game's dictionary. Returns how many were missing.
        public static int AsegurarRaceData(TabletopTavernData data, string origen)
        {
            if (!Activas || data == null) return 0;
            var dic = data.RaceDataDictionary;
            if (dic == null) return 0;
            int puestas = 0;
            foreach (var r in _razas.Values)
            {
                var raza = (Race)r.Id;
                if (dic.ContainsKey(raza) && dic[raza] != null) continue;
                RaceData copia = CopiaRaceData(dic, r);
                if (copia == null) { AvisoUnaVez("racedata" + r.Id, $"[{origen}] No hay ningún RaceData de serie que copiar para la raza {r.Id} todavía."); continue; }
                dic[raza] = copia;
                puestas++;
            }
            if (puestas > 0 && origen != "LoadStatsFromSOs")
                Plugin.Info($"[{origen}] RaceData de las razas nuevas registrado ({puestas}).");
            return puestas;
        }

        // Copia (una vez por sesión) del RaceData de su raza de serie, con los colores propios y la región prestada.
        // Si la copia falla, se usa el RaceData de serie tal cual: nunca null.
        // Copy (once per session) of the RaceData of its stock race, with its own colors and the borrowed region.
        // If the copy fails, the stock RaceData is used as is: never null.
        private static RaceData CopiaRaceData(Il2CppGeneric.Dictionary<Race, RaceData> dic, Manifiesto.Raza r)
        {
            if (_datosRaza.TryGetValue(r.Id, out var hecha) && hecha != null) return hecha;
            Race deQuien = RazaSerie(r.DatosDe, Race.IronLegion);
            RaceData plantilla = dic.ContainsKey(deQuien) ? dic[deQuien] : null;
            if (plantilla == null)
                foreach (Race x in Enum.GetValues(typeof(Race)))
                    if (dic.ContainsKey(x) && dic[x] != null) { plantilla = dic[x]; break; }
            if (plantilla == null) return null;
            try
            {
                var copia = UnityEngine.Object.Instantiate(plantilla.Cast<UnityEngine.Object>()).Cast<RaceData>();
                copia.name = "ZeroRaceData_" + r.Id;
                // Que Unity no la descargue al cambiar de escena (no está en ningún asset)
                // So that Unity doesn't unload it when changing scenes (it is not in any asset)
                copia.hideFlags = HideFlags.HideAndDontSave;
                copia.Race = (Race)r.Id;
                if (r.Colores != null && r.Colores.Count >= 3)
                {
                    copia.PrimaryColor = Color(r.Colores[0], copia.PrimaryColor);
                    copia.SecondaryColor = Color(r.Colores[1], copia.SecondaryColor);
                    copia.AccentColor = Color(r.Colores[2], copia.AccentColor);
                }
                // Región del mapa: la de la raza de serie dueña de la región prestada
                // Map region: that of the base-game faction that owns the borrowed region
                Race deRegion = RazaSerie(r.RazaRegion, deQuien);
                if (dic.ContainsKey(deRegion) && dic[deRegion] != null) copia.MapRegion = dic[deRegion].MapRegion;
                _datosRaza[r.Id] = copia;
                Plugin.Info($"RaceData de la raza {r.Id} ({r.NombreEs}): copia de {deQuien} con colores {string.Join(" ", r.Colores ?? new List<string>())} y región de {deRegion}.");
                return copia;
            }
            catch (Exception e)
            {
                Plugin.Error($"RaceData de la raza {r.Id}: no se pudo copiar ({e.Message}); se usa el de {deQuien} tal cual.");
                _datosRaza[r.Id] = plantilla;
                return plantilla;
            }
        }

        // Color desde «#RRGGBB» (o el de respaldo si el texto no vale)
        // Color from «#RRGGBB» (or the fallback color if the text is not valid)
        internal static Color Color(string hex, Color respaldo)
        {
            if (string.IsNullOrEmpty(hex)) return respaldo;
            string h = hex.TrimStart('#');
            if (h.Length != 6 || !int.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out int v)) return respaldo;
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
        }

        /// <summary>RaceData de una raza nueva (la copia ya hecha), o null si no es nueva o aún no existe.</summary>
        /// RaceData of a new race (the copy already made), or null if it is not new or does not exist yet.
        public static RaceData DatosDe(Race r) => EsNueva(r) && _datosRaza.TryGetValue((int)r, out var d) ? d : null;

        // Postfix de TabletopTavernData.GetRaceData: red de seguridad si alguien la pide antes de registrarla
        // Postfix of TabletopTavernData.GetRaceData: safety net in case someone requests it before it is registered
        public static void TrasGetRaceData(Race __0, ref RaceData __result)
        {
            try
            {
                if (__result != null || !EsNueva(__0)) return;
                var data = TabletopTavernData.InstanceIfExists;
                if (data != null) AsegurarRaceData(data, "GetRaceData");
                __result = DatosDe(__0);
                if (__result == null && data != null && data.RaceDataDictionary.ContainsKey(Race.IronLegion)) __result = data.RaceDataDictionary[Race.IronLegion];
                AvisoUnaVez("getrace" + (int)__0, $"GetRaceData({(int)__0}) se pidió antes de registrar la raza; se devuelve la copia (o la de Iron Legion).");
            }
            catch (Exception e) { AvisoUnaVez("getraceerr", "TrasGetRaceData: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // Enemigos: actos de campaña, actos infinitos y Marcha
        // Enemies: campaign acts, endless acts and March
        // ---------------------------------------------------------------
        // Razas de cada acto de serie (TabletopTavernData.GenerateRaceForMap); solo se usan para el peso del sorteo
        // Races of each base-game act (TabletopTavernData.GenerateRaceForMap); only used for the draw weight
        private static readonly Dictionary<int, Race[]> ActosSerie = new Dictionary<int, Race[]>
        {
            { 1, new[] { Race.Gruntkin, Race.SanguineCourt } },
            { 2, new[] { Race.IronLegion, Race.SakuraDynasty, Race.RavenHost } },
            { 3, new[] { Race.DrakosaurBrood, Race.DeepstoneHold, Race.TaelindorForest } },
        };

        // Postfix de GenerateRaceForMap: en los actos 1-3, las razas nuevas de ese acto entran en el sorteo con el mismo peso
        // que cada raza de serie (0.7.6: las dos en los tres actos, «actosCampana» del manifiesto; antes una por acto). El sorteo es determinista (misma semilla, mismo resultado) y aparte del del juego: si
        // no toca la raza nueva, se queda lo que eligió el juego.
        // Postfix of GenerateRaceForMap: in acts 1-3, the new races of that act enter the draw with the same weight
        // as each base-game race (0.7.6: both in all three acts, «actosCampana» from the manifest; before, one per act). The draw is deterministic (same seed, same result) and separate from the game's own: if
        // it does not pick the new race, whatever the game chose stays.
        public static void TrasGenerateRaceForMap(int __0, int __1, Race __2, ref Race __result)
        {
            try
            {
                int acto = __0, semilla = __1;
                Race propia = __2;
                if (!Activas || acto < 1 || acto > 3) return;
                // 2.ª ronda de la revisión: la regla de la 0.7.6 (las dos en los tres actos) solo en partidas empezadas con ella
                // 2nd round of the review: the 0.7.6 rule (both in all three acts) only in runs started with it
                bool reglaNueva = ReglaActosNueva();
                var candidatas = _razas.Values.Where(r => (reglaNueva ? LogicaCampanaHeroes.SaleEnActo(acto, r.ActoCampana, r.ActosCampana) : r.ActoCampana == acto)
                                                          && (Race)r.Id != propia && Jugable((Race)r.Id))
                                              .OrderBy(r => r.Id).Select(r => (Race)r.Id).ToList();
                if (candidatas.Count == 0) return;
                int deSerie = ActosSerie.TryGetValue(acto, out var lista) ? lista.Count(x => x != propia) : 3;
                var azar = new System.Random(unchecked(semilla * 31 + acto * 7919 + 4040));
                double p = LogicaCampanaHeroes.ProbabilidadNuevas(deSerie, candidatas.Count);
                if (azar.NextDouble() >= p) return;
                __result = candidatas[azar.Next(candidatas.Count)];
            }
            catch (Exception e) { AvisoUnaVez("acto", "TrasGenerateRaceForMap: " + e.Message); }
        }

        // ¿La partida en curso usa la regla de la 0.7.6? Sin partida (menú, autoprueba), sí. La decisión se apunta en el registro
        // por runId para que no cambie nunca. Si algo falla, la nueva (como en una partida nueva)
        // Regla ya decidida en esta sesión por partida (3.ª pasada de la revisión, hallazgo 2: sin registro no cambia a mitad)
        // Does the current run use the 0.7.6 rule? With no run (menu, self-test), yes. The decision is recorded in the registry
        // by runId so that it never changes. If anything fails, the new one (as in a new run)
        // Rule already decided in this session per run (3rd pass of the review, finding 2: without a registry it does not change midway)
        private static readonly Dictionary<string, int> _reglaActosSesion = new Dictionary<string, int>();
        private static bool ReglaActosNueva()
        {
            try
            {
                var datos = CampaignManager.InstanceIfExists?.CampaignSaveManager?.SaveData;
                if (datos == null) return true;
                string partida = datos.runId;   // id de la partida en el save (el mismo que usa el registro)
                                                // id of the run in the save (the same one the registry uses)
                if (string.IsNullOrEmpty(partida)) partida = "semilla:" + datos.seed;   // save sin id: por su semilla
                                                                                        // save without an id: by its seed
                var reg = Saves.Registro;
                int apuntada = reg?.ReglaActosDe(partida) ?? 0;
                if (apuntada == 0) _reglaActosSesion.TryGetValue(partida, out apuntada);
                int regla = LogicaCampanaHeroes.ReglaActos(apuntada, datos.bookNumber, datos.BattlesFought);
                _reglaActosSesion[partida] = regla;
                reg?.FijarReglaActos(partida, regla);
                return regla == LogicaCampanaHeroes.ReglaNueva;
            }
            catch (Exception e) { AvisoUnaVez("reglaActos", "Regla del sorteo por acto: " + e.Message + " (se usa la de la 0.7.6)."); return true; }
        }

        // Prefix de GenerateEndlessRace: misma lógica que el juego (todas las razas que la partida aún no ha visto, en
        // orden sorteado con la semilla) pero con las razas nuevas en el bombo (Enum.GetValues no las ve)
        // Prefix of GenerateEndlessRace: same logic as the game (all the races the run has not seen yet, in
        // order shuffled with the seed) but with the new races in the pool (Enum.GetValues does not see them)
        public static bool AntesDeGenerateEndlessRace(TabletopTavernData __instance, int __0, int __1, Race __2, ref Race __result)
        {
            if (!Activas || !RazasJugables.Any()) return true;
            try
            {
                int acto = __0, semilla = __1;
                Race propia = __2;
                var sinVer = BomboInfinito(propia);
                int ultimoActo = TabletopTavernConstants.FINAL_STORY_ACT;
                for (int a = 1; a <= ultimoActo; a++) sinVer.Remove(__instance.GenerateRaceForMap(a, semilla, propia));
                Race eleccion = propia;
                for (int a = ultimoActo + 1; a <= acto; a++)
                {
                    if (sinVer.Count == 0) sinVer = BomboInfinito(propia);
                    eleccion = SorteoConSemilla(sinVer, semilla + a * 13);
                    sinVer.Remove(eleccion);
                }
                __result = eleccion;
                return false;
            }
            catch (Exception e)
            {
                AvisoUnaVez("infinito", "AntesDeGenerateEndlessRace (se usa el del juego, sin razas nuevas): " + e.Message);
                return true;
            }
        }

        // Bombo de los actos infinitos: las razas del juego que salen en este build (menos Special y la propia) y las nuevas
        // JUGABLES. Se usa al empezar y cada vez que se reinicia el bombo (0.6.1: un solo sitio, siempre filtrado)
        // Pool for endless acts: the game's races that appear in this build (except Special and the race itself) and the new
        // PLAYABLE ones. Used at the start and every time the pool is reset (0.6.1: a single place, always filtered)
        private static List<Race> BomboInfinito(Race propia)
        {
            var bombo = new List<Race>();
            foreach (Race r in Enum.GetValues(typeof(Race)))
                if (r != Race.Special && r != propia && TabletopTavernConstants.IsRaceInBuild(r)) bombo.Add(r);
            foreach (Race r in RazasJugables) if (r != propia) bombo.Add(r);
            return bombo;
        }

        // Como TabletopTavernData.PickSeededRace: sorteo con semilla que deja el estado de UnityEngine.Random como estaba
        // Like TabletopTavernData.PickSeededRace: seeded draw that leaves the UnityEngine.Random state as it was
        private static Race SorteoConSemilla(List<Race> bombo, int semilla)
        {
            var antes = UnityEngine.Random.state;
            UnityEngine.Random.InitState(semilla);
            int i = UnityEngine.Random.Range(0, bombo.Count);
            UnityEngine.Random.state = antes;
            return bombo[i];
        }

        /// <summary>
        /// Rehace MarchRules.HostRaces (el juego lo calcula una vez recorriendo el enum): quita las razas nuevas que no
        /// sean jugables y añade las que sí, mirando cada vez el array actual. Se llama en cada recarga.
        /// Rebuilds MarchRules.HostRaces (the game computes it once by iterating over the enum): removes the new races that
        /// are not playable and adds those that are, checking the current array each time. Called on every reload.
        /// </summary>
        public static void AsegurarAnfitrionesMarcha(string origen)
        {
            // 0.6.1: también con las facciones desactivadas (quita las razas fuera del enum que hubiera puesto antes)
            // 0.6.1: also with factions disabled (removes the races outside the enum that it may have added before)
            try
            {
                // Llamar antes a un método estático fuerza el constructor estático de MarchRules: si corriera después,
                // volvería a calcular HostRaces sin las razas nuevas
                // Calling a static method first forces the static constructor of MarchRules: if it ran afterwards,
                // it would recompute HostRaces without the new races
                MarchRules.InMarch(1);
                var actuales = MarchRules.HostRaces;
                var actual = new List<Race>();
                for (int i = 0; actuales != null && i < actuales.Length; i++) actual.Add(actuales[i]);
                // Las de serie tal cual (las del enum del juego); de las nuevas, solo las jugables ahora
                // Base-game ones as they are (those in the game's enum); of the new ones, only those playable now
                var lista = actual.Where(r => Enum.IsDefined(typeof(Race), r)).ToList();
                int deSerie = lista.Count;
                foreach (Race r in RazasJugables.OrderBy(x => (int)x)) lista.Add(r);
                if (!lista.SequenceEqual(actual))
                {
                    var nuevo = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Race>(lista.Count);
                    for (int i = 0; i < lista.Count; i++) nuevo[i] = lista[i];
                    MarchRules.HostRaces = nuevo;
                    Plugin.Info($"[{origen}] Anfitriones de la Marcha: {deSerie} razas de serie + {lista.Count - deSerie} nuevas jugables ({string.Join(", ", lista.Select(x => (int)x))}).");
                }
            }
            catch (Exception e) { Plugin.Error($"[{origen}] No se pudo ampliar MarchRules.HostRaces: " + e.Message); }
        }

        /// <summary>Número de anfitriones de la Marcha que son razas nuevas (para la autoprueba).</summary>
        /// Number of March hosts that are new races (for the self-test).
        public static int AnfitrionesNuevos()
        {
            var a = MarchRules.HostRaces;
            int n = 0;
            for (int i = 0; a != null && i < a.Length; i++) if (EsNueva(a[i])) n++;
            return n;
        }

        // Postfix de CampaignSaveManager.GenerateTownRace (estático): unas ciudades pasan a ser de las razas nuevas. El
        // sorteo depende solo de los argumentos, igual que el del juego, así que la bandera del mapa, el nombre y la
        // guarnición (que llaman cada uno a este método) ven siempre la misma raza.
        // Juego 1.11.1: la firma pasó de (nodo, acto) a (nodo, semilla de la campaña, acto). __0 sigue siendo el nodo, __1 es
        // ahora la semilla y __2 el acto: el sorteo usa los tres (como el juego, que ya varía las ciudades por campaña).
        // Postfix of CampaignSaveManager.GenerateTownRace (static): some towns become towns of the new races. The
        // draw depends only on the arguments, like the game's own, so the map banner, the name and the
        // garrison (which each call this method) always see the same race.
        // Game 1.11.1: the signature changed from (node, act) to (node, campaign seed, act). __0 is still the node, __1 is
        // now the seed and __2 the act: the draw uses all three (like the game, which already varies towns per campaign).
        public static void TrasGenerateTownRace(int __0, int __1, int __2, ref Race __result)
        {
            try
            {
                if (!Activas) return;
                int tirada = new System.Random(unchecked(__0 * 7 + __2 * 131 + __1 * 977 + 40417)).Next(0, 100);
                int acumulado = 0;
                foreach (var r in _razas.Values.OrderBy(x => x.Id))
                {
                    // Una raza no jugable no se lleva su porcentaje (las ciudades se quedan con la raza de serie)
                    // A non-playable race does not get its percentage (towns keep the base-game race)
                    if (!Jugable((Race)r.Id)) continue;
                    acumulado += Math.Max(0, r.PorcentajeCiudades);
                    if (tirada < acumulado) { __result = (Race)r.Id; return; }
                }
            }
            catch (Exception e) { AvisoUnaVez("ciudad", "TrasGenerateTownRace: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // Prestados: región del mapa, música, color y fondo de carta
        // Borrowed: map region, music, color and card background
        // ---------------------------------------------------------------
        // Prefix de MapThemeManager.GetMapRegion: la raza nueva usa la región de su raza de serie (si no, Goblin Hills)
        // Prefix of MapThemeManager.GetMapRegion: the new race uses the region of its base-game race (otherwise, Goblin Hills)
        public static void AntesDeGetMapRegion(ref Race __0)
        {
            try { if (EsNueva(__0)) __0 = Prestada(__0, r => r.RazaRegion, Race.IronLegion); }
            catch (Exception e) { AvisoUnaVez("region", "AntesDeGetMapRegion: " + e.Message); }
        }

        // Prefix de IAudioRequester.SetAudioThemePack / PlayBattleTheme / SetGarrisonBattleTheme: los temas van por
        // número de raza y 40/41 se saldrían de la lista; se traducen a la raza de serie que presta la música
        // Prefix of IAudioRequester.SetAudioThemePack / PlayBattleTheme / SetGarrisonBattleTheme: themes go by
        // race number and 40/41 would fall outside the list; they are translated to the base-game race that lends the music
        public static void AntesDeTemaMusical(ref int __0)
        {
            try { if (EsNueva(__0)) __0 = (int)Prestada((Race)__0, r => r.Musica, Race.IronLegion); }
            catch (Exception e) { AvisoUnaVez("musica", "AntesDeTemaMusical: " + e.Message); }
        }

        // Postfix de TJ.MainMenu.WarbandBannerPanel.Refresh() (03-11-2026, punto 4 de la prueba: estandarte): solo apunta en
        // el log, una vez por icono, qué textura tiene el material de la vista previa del estandarte, para saber de dónde sale
        // el «fusil» que ve Zero con héroe 902. Refresh no lleva argumentos (2.ª ronda de la revisión: antes iba en
        // BannerPreview.Apply, cuyo struct de colores pasaba igualmente por el trampolín)
        // Postfix of TJ.MainMenu.WarbandBannerPanel.Refresh() (03-11-2026, test point 4: banner): it only logs,
        // once per icon, which texture the banner preview material has, to find out where the
        // «rifle» that Zero sees with hero 902 comes from. Refresh takes no arguments (2nd round of the review: it used to be in
        // BannerPreview.Apply, whose color struct went through the trampoline anyway)
        private static readonly HashSet<string> _iconosEstandarte = new HashSet<string>();
        public static void TrasBannerRefresh(TJ.MainMenu.WarbandBannerPanel __instance)
        {
            try
            {
                var material = __instance?._preview?._material;
                if (material == null) return;
                var icono = material.GetTexture(TJ.MainMenu.BannerPreview.IconID);
                string nombre = icono == null ? "ninguno" : (icono.name ?? "?");
                if (_iconosEstandarte.Count < 40 && _iconosEstandarte.Add(nombre))
                    Plugin.Info($"Estandarte: el juego pone el icono «{nombre}» ({(icono == null ? "-" : $"{icono.width}x{icono.height}")}) en la vista previa.");
            }
            catch { }
        }

        // Postfix de ColorData.GetRaceDisplayColor: color propio de la facción (aclarado si es muy oscuro para leerse
        // sobre la interfaz) en vez del gris de «Special»
        // Postfix of ColorData.GetRaceDisplayColor: the faction's own color (lightened if it is too dark to read
        // on the interface) instead of the gray of «Special»
        public static void TrasGetRaceDisplayColor(Race __0, ref Color __result)
        {
            try
            {
                if (!EsNueva(__0) || !_razas.TryGetValue((int)__0, out var r) || r.Colores == null || r.Colores.Count == 0) return;
                Color c = Color(r.Colores[0], __result);
                float luz = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                if (luz < 0.45f) c = UnityEngine.Color.Lerp(c, UnityEngine.Color.white, 0.45f);
                __result = c;
            }
            catch (Exception e) { AvisoUnaVez("color", "TrasGetRaceDisplayColor: " + e.Message); }
        }

        // Postfix de SquadDisplayCardPresenter.SetBackgroundImage: con una raza nueva no se enciende ningún fondo; se
        // enciende el de la raza de serie que presta la música (Iron Legion para 40, Raven Host para 41)
        // Postfix of SquadDisplayCardPresenter.SetBackgroundImage: with a new race no background is turned on; the
        // background of the base-game race that lends the music is turned on (Iron Legion for 40, Raven Host for 41)
        public static void TrasSetBackgroundImage(SquadDisplayCardPresenter __instance, Race __0)
        {
            try
            {
                if (!EsNueva(__0)) return;
                Race fondo = Prestada(__0, r => r.Musica, Race.IronLegion);
                var imagen = fondo == Race.RavenHost ? __instance.ravenhostBackground : __instance.ironLegionBackground;
                if (imagen != null) imagen.enabled = true;
            }
            catch (Exception e) { AvisoUnaVez("fondo", "TrasSetBackgroundImage: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // Bonus de campaña prestados (van cableados por id de héroe en el juego)
        // Borrowed campaign bonuses (hardwired by hero id in the game)
        // ---------------------------------------------------------------
        // Héroe activo de la partida (0 si no hay gestor de bonus todavía)
        // Active hero of the run (0 if there is no bonus manager yet)
        private static int HeroeActivo()
        {
            var g = HeroBonusManager.InstanceIfExists;
            return g == null ? 0 : g.ActiveHeroID;
        }

        // ¿El héroe activo es de una raza nueva cuyo bonus de campaña es este?
        // Is the active hero of a new race whose campaign bonus is this one?
        private static bool HeroeConBonus(string bonus)
        {
            int h = HeroeActivo();
            return EsHeroeNuevo(h) && _razas.TryGetValue(_heroes[h].Raza, out var r)
                   && string.Equals(r.BonusCampanaPrestado, bonus, StringComparison.OrdinalIgnoreCase);
        }

        // Postfix de TownPanel.OnEnterTown: Diezmo del Munitorum = el Edicto Imperial de Iron Legion (primer pack de
        // reclutamiento gratis en cada ciudad) para héroe 901 y héroe 902. Se respeta el bloqueo de pasiva de las Pruebas
        // (Ordalías) que anulan el de Iron Legion, igual que el juego.
        // Postfix of TownPanel.OnEnterTown: Munitorum Tithe = Iron Legion's Imperial Edict (first free recruitment
        // pack in each city) for hero 901 and hero 902. The passive lock of the Trials
        // (Ordeals) that nullify Iron Legion's is respected, just like the game does.
        public static void TrasOnEnterTown(TJ.Town.TownPanel __instance)
        {
            try
            {
                if (!HeroeConBonus("ImperialEdict") || __instance.imperialEdictActive) return;
                var datos = __instance.campaignSaveManager?.SaveData;
                if (datos == null || datos.IsFactionPassiveBlocked(Race.IronLegion)) return;
                __instance.imperialEdictActive = true;
                __instance.UpdateAffordability(datos.goldAmount);
                Plugin.Info("Diezmo del Munitorum: primer pack de reclutamiento gratis en esta ciudad.");
            }
            catch (Exception e) { AvisoUnaVez("edicto", "TrasOnEnterTown: " + e.Message); }
        }

        // Postfix de TownPanel.RecruitLimitNote: la nota de la ciudad enseña el texto del bonus de la raza nueva
        // Postfix of TownPanel.RecruitLimitNote: the town note shows the bonus text of the new race
        public static void TrasRecruitLimitNote(ref string __result)
        {
            try
            {
                if (!HeroeConBonus("ImperialEdict")) return;
                int h = HeroeActivo();
                string texto = Memori.Localization.LocalizationManager.Instance.GetText(_heroes[h].Raza + "BonusDescription");
                __result += $"\n<color={ColorData.Gold}>{texto}</color>";
            }
            catch (Exception e) { AvisoUnaVez("nota", "TrasRecruitLimitNote: " + e.Message); }
        }

        // Postfix de GoldManager.GetTotalInterest: Tesorería imperial del héroe 903 y héroe 904. Desde el 18-10-2026 (decisión de
        // Zero, equilibrio §2.1) el x2 solo vale para los primeros 5 de interés (LogicaInteres.TesoreriaImperial): con el
        // tope 30 de «Zero - Economía» y sin objetos de interés, como mucho 35 por turno en lugar de 60 (va sobre el total
        // del juego, que ya incluye Iron Bank y compañía: con Iron Bank, 30 -> 60 -> 65; ronda 2, B-4)
        // Postfix of GoldManager.GetTotalInterest: Imperial Treasury of hero 903 and hero 904. Since 18-10-2026 (decision by
        // Zero, balance §2.1) the x2 only applies to the first 5 of interest (LogicaInteres.TesoreriaImperial): with the
        // 30 cap of «Zero - Economía» and no interest items, at most 35 per turn instead of 60 (it applies to the game's
        // total, which already includes Iron Bank and co.: with Iron Bank, 30 -> 60 -> 65; round 2, B-4)
        public static void TrasGetTotalInterest(ref int __result)
        {
            try { if (HeroeConBonus("TesoreriaDoble")) __result = LogicaInteres.TesoreriaImperial(__result); }
            catch (Exception e) { AvisoUnaVez("interes", "TrasGetTotalInterest: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // Temas de taberna: las razas nuevas no tienen (diseño §6.4)
        // Tavern themes: the new races have none (design §6.4)
        // ---------------------------------------------------------------
        // Postfix de SaveDataHandler.RefreshTavernThemeUnlocks: si el juego añadió 40/41 (victoria en la máxima
        // dificultad con un héroe nuevo), se quitan y se guarda (solo si había algo que quitar)
        // Postfix of SaveDataHandler.RefreshTavernThemeUnlocks: if the game added 40/41 (victory on the highest
        // difficulty with a new hero), they are removed and the save is written (only if there was something to remove)
        public static void TrasRefreshTavernThemeUnlocks()
        {
            try
            {
                if (!Activas) return;
                var datos = Memori.SaveData.SaveDataHandler.LoadPlayerSaveData();
                var temas = datos?.unlockedTavernThemes;
                if (temas == null) return;
                bool cambiado = false;
                foreach (Race r in RazasNuevas) while (temas.Contains(r)) { temas.Remove(r); cambiado = true; }
                if (EsNueva(datos.activeTavernThemeRace)) { datos.activeTavernThemeRace = Race.Special; cambiado = true; }
                if (!cambiado) return;
                Memori.SaveData.SaveDataHandler.SavePlayerSaveData(datos);
                Plugin.Info("Temas de taberna: se quitan los de las razas nuevas (no tienen tema propio).");
            }
            catch (Exception e) { AvisoUnaVez("temas", "TrasRefreshTavernThemeUnlocks: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // Recarga de datos: lo que hay que rehacer cada vez que el juego reconstruye el catálogo
        // Data reload: what has to be redone every time the game rebuilds the catalog
        // ---------------------------------------------------------------
        /// <summary>
        /// Calcula qué razas nuevas son jugables en esta recarga (mod oficial activo y unidades de su raza en los 4
        /// rangos). Va después de las listas de raza. Devuelve un resumen para el log.
        /// Computes which new races are playable in this reload (official mod active and units of its race in all 4
        /// ranks). Runs after the race lists. Returns a summary for the log.
        /// </summary>
        public static string ActualizarJugables(TabletopTavernData data, bool modActivo)
        {
            if (!Activas) { _jugables.Clear(); return "facciones nuevas desactivadas"; }
            // 0.6.1: sin catálogo (recarga a medias) no se toca lo que había: no se convierte en estado definitivo
            // 0.6.1: with no catalog (partial reload) what was there is left untouched: it does not become the final state
            var dic = data?.UnitsOfRaceDictionary;
            if (dic == null) return "sin catálogo de razas todavía (se mantiene lo anterior)";
            var nuevas = new HashSet<int>();
            var partes = new List<string>();
            foreach (Race r in RazasNuevas)
            {
                var rangos = new SortedSet<int>();
                int n = 0;
                var l = dic.ContainsKey(r) ? dic[r] : null;
                for (int i = 0; l != null && i < l.Count; i++) { rangos.Add(data.GetUnitTierFromUnitName(l[i])); n++; }
                bool completa = rangos.Contains(1) && rangos.Contains(2) && rangos.Contains(3) && rangos.Contains(4);
                if (modActivo && completa) nuevas.Add((int)r);
                partes.Add($"raza {(int)r}: {n} unidades, rangos {string.Join(",", rangos)}, jugable {modActivo && completa}");
            }
            _jugables.Clear();
            foreach (int r in nuevas) _jugables.Add(r);
            return string.Join("; ", partes) + (modActivo ? "" : " (mod oficial no activo)");
        }

        /// <summary>Solo para la autoprueba: quita o devuelve una raza de las jugables sin recargar datos.</summary>
        /// For the self-test only: removes or restores a race from the playable ones without reloading data.
        internal static void ForzarJugable(Race r, bool jugable)
        {
            if (jugable) { if (Activas && _razas.ContainsKey((int)r)) _jugables.Add((int)r); }
            else _jugables.Remove((int)r);
        }

        /// <summary>RaceData (siempre que estén activas) y anfitriones de la Marcha según las jugables (postfix de ReloadData).</summary>
        /// RaceData (whenever they are active) and Marcha hosts according to the playable ones (postfix of ReloadData).
        public static void TrasRecarga(TabletopTavernData data, string origen)
        {
            if (Activas) AsegurarRaceData(data, origen);
            // Anfitriones siempre: con las facciones apagadas se quitan las nuevas que quedaran de antes
            // Hosts always: with the factions turned off, any new ones left over from before are removed
            AsegurarAnfitrionesMarcha(origen);
        }
    }
}
