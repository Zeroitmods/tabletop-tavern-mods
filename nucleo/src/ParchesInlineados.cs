// ParchesInlineados.cs
// Arreglos de los parches que no saltaban porque el compilador IL2CPP copió el método parcheado dentro de su llamador
// (revisión del 17-10-2026: notas internas (no publicadas), sección «Arreglos aplicados»). Harmony desvía la
// entrada nativa del método; la copia inlineada no pasa por ella. La forma común: se parchea el llamador (que sí entra
// por su dirección), su prefix abre un contexto y su finalizer lo cierra pase lo que pase; dentro, los métodos que el
// llamador llama de verdad leen ese contexto y corrigen lo que la copia hizo de serie. Los parches viejos se quedan: en
// sus otras rutas de llamada directa siguen saltando. La lógica pura está en LogicaInlineados.cs (probada fuera del juego).
//   1) Ciudades de las razas nuevas: GenerateTownRace va copiado dentro de CampaignSaveManager.GenerateTown. Prefix de
//      GenerateTown: calcula la raza con GenerateTownRace llamado desde aquí (pasa por FaccionesNuevas.TrasGenerateTownRace)
//      y, si es nueva, abre el contexto. Prefix de TabletopTavernData.GetSquadsWithTiersFromRace: dentro del contexto cambia
//      la raza (guarnición de la ciudad). Postfix de GenerateTown con prioridad alta (antes de Objetos.TrasGenerateTown, que
//      lee townRace para el botín afín): pone townData.townRace a la raza nueva.
//   2) Región prestada en los nodos del mapa: GetMapRegion va copiado dentro de MapNode.SetUp. Prefix de SetUp: guarda la
//      raza del nodo (sin cambiarla: también da las banderas y la ventana de la Marcha). Prefix de
//      CampaignSaveManager.GenerateNodeWeather y GenerateNodeBiome: dentro del contexto y con raza nueva, la región pasa a
//      MapThemeManager.GetMapRegion(raza), que entra por FaccionesNuevas.AntesDeGetMapRegion y da la región prestada.
//   3) Logro de objetos: EvaluateGearCollection va copiado dentro de SaveDataHandler.AquiredGear. Mismo par de
//      LogrosFiltrados (suspende la lista de gear ampliada mientras corre).
//   4) Logros por estadísticas. SteamAchievements.AddStat/SetStat llaman a EvaluateStatAchievements, que desbloquea con
//      SteamStatic.UnlockAchievement sin pasar por SteamAchievements.Unlock (donde está el filtro de LogrosFiltrados).
//      Juego 1.11.1: SteamAchievements ya no existe. PlayerAchievements.AddStat(PlayerStatId, int) llama a
//      EvaluateStatAchievements y este desbloquea por el almacén (IAchievementStore.Unlock -> SteamAchievementStore.Unlock,
//      que salta con un jmp a SteamStatic.UnlockAchievement, comprobado en el binario): el embudo sigue valiendo.
//      - Prefix de SteamStatic.UnlockAchievement(string): el embudo de TODOS los desbloqueos (Unlock salta aquí con un jmp;
//        TriggerAchievement va copiado dentro, por eso no se parchea). Traduce el nombre de Steam a AchievementId y aplica
//        el mismo filtro (LogrosFiltrados.AntesDeUnlock). En la ruta de Unlock se evalúa dos veces: da lo mismo.
//      - La regla vigente (LogicaLogros.Bloquear: recontar sin lo nuevo) no se puede aplicar en el embudo a un logro por
//        estadística: el valor es un acumulado de Steam y no dice qué unidad lo generó. Se aplica donde está ese dato, al
//        sumar: prefix de ArmySpawnManager.MapSquadsToKillsAndWithdrwanSquads (cuenta las bajas de las escuadras 9xx del
//        jugador) y prefix de SteamAchievements.AddStat (dentro de ese contexto descuenta esas bajas de UnitKills). Así el
//        acumulado ya es «sin 9xx» y el umbral que aplica el juego es el mismo que daría el recuento.
//      - No se descuentan: las bajas por hechizos (la regla cuenta héroes y unidades, no lo que hacen los hechizos del
//        héroe, que son los del héroe de serie que lo presta), ni ShopPurchases ni CitiesSacked (acciones del jugador
//        que ninguna unidad 9xx genera). El acumulado anterior a esta versión no se corrige hacia atrás.
// Todo falla en blando: si algo falla, decide el juego como siempre. Los finalizers devuelven la excepción tal cual.
// Autor: ZeroitMods.
// ParchesInlineados.cs
// Fixes for the patches that did not fire because the IL2CPP compiler copied the patched method inside its caller
// (review of 17-10-2026: internal notes (unpublished), section «Arreglos aplicados»). Harmony diverts the
// native entry of the method; the inlined copy does not go through it. The common approach: patch the caller (which does enter
// through its address), its prefix opens a context and its finalizer closes it no matter what; inside, the methods that the
// caller actually calls read that context and correct what the copy did by default. The old patches stay: on
// their other direct-call paths they still fire. The pure logic is in LogicaInlineados.cs (tested outside the game).
//   1) Cities of the new races: GenerateTownRace is copied inside CampaignSaveManager.GenerateTown. Prefix of
//      GenerateTown: computes the race with GenerateTownRace called from here (it goes through FaccionesNuevas.TrasGenerateTownRace)
//      and, if it is new, opens the context. Prefix of TabletopTavernData.GetSquadsWithTiersFromRace: inside the context it changes
//      the race (city garrison). Postfix of GenerateTown with high priority (before Objetos.TrasGenerateTown, which
//      reads townRace for the matching loot): sets townData.townRace to the new race.
//   2) Borrowed region on the map nodes: GetMapRegion is copied inside MapNode.SetUp. Prefix of SetUp: saves the
//      race of the node (without changing it: it also provides the banners and the March window). Prefix of
//      CampaignSaveManager.GenerateNodeWeather and GenerateNodeBiome: inside the context and with a new race, the region becomes
//      MapThemeManager.GetMapRegion(race), which enters through FaccionesNuevas.AntesDeGetMapRegion and gives the borrowed region.
//   3) Gear achievement: EvaluateGearCollection is copied inside SaveDataHandler.AquiredGear. Same pair as
//      LogrosFiltrados (it suspends the expanded gear list while it runs).
//   4) Stat achievements. SteamAchievements.AddStat/SetStat call EvaluateStatAchievements, which unlocks with
//      SteamStatic.UnlockAchievement without going through SteamAchievements.Unlock (where the LogrosFiltrados filter is).
//      Game 1.11.1: SteamAchievements no longer exists. PlayerAchievements.AddStat(PlayerStatId, int) calls
//      EvaluateStatAchievements and this unlocks through the store (IAchievementStore.Unlock -> SteamAchievementStore.Unlock,
//      which jumps with a jmp to SteamStatic.UnlockAchievement, verified in the binary): the funnel is still valid.
//      - Prefix of SteamStatic.UnlockAchievement(string): the funnel for ALL unlocks (Unlock jumps here with a jmp;
//        TriggerAchievement is copied inside, so it is not patched). It translates the Steam name to AchievementId and applies
//        the same filter (LogrosFiltrados.AntesDeUnlock). On the Unlock path it is evaluated twice: it makes no difference.
//      - The current rule (LogicaLogros.Bloquear: recount without the new ones) cannot be applied in the funnel to a stat
//        achievement: the value is a Steam accumulated total and does not say which unit generated it. It is applied where that data is, when
//        adding: prefix of ArmySpawnManager.MapSquadsToKillsAndWithdrwanSquads (counts the kills of the player's 9xx
//        squads) and prefix of SteamAchievements.AddStat (inside that context it subtracts those kills from UnitKills). This way the
//        accumulated total is already «without 9xx» and the threshold the game applies is the same one the recount would give.
//      - Not subtracted: kills by spells (the rule counts heroes and units, not what the hero's spells do,
//        which are those of the default hero that lends it), nor ShopPurchases nor CitiesSacked (player actions
//        that no 9xx unit generates). The accumulated total prior to this version is not corrected retroactively.
// Everything fails softly: if something fails, the game decides as usual. The finalizers return the exception as is.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using Memori.SaveData;
using Memori.Steamworks;
using TJ;
// Juego 1.11.1: AchievementId, PlayerStatId (antes SteamStatId), AchievementRegistry y PlayerAchievements (antes
// SteamAchievements) están en TJ.Achievements (TabletopTavern.Platform.dll)
// Game 1.11.1: AchievementId, PlayerStatId (formerly SteamStatId), AchievementRegistry and PlayerAchievements (formerly
// SteamAchievements) are in TJ.Achievements (TabletopTavern.Platform.dll)
using TJ.Achievements;
using TJ.Map;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: parches de los métodos inlineados
    // Section: patches for the inlined methods
    // -----------------------------------------------------------------------
    public static class ParchesInlineados
    {
        private static readonly HashSet<string> _avisos = new HashSet<string>();

        // Contextos de los llamadores (solo hilo principal)
        // Caller contexts (main thread only)
        private static readonly ContextoAnidado<int> _ciudad = new ContextoAnidado<int>();   // GenerateTown: raza nueva o SinRaza
                                                                                             // GenerateTown: new race or SinRaza
        private static readonly ContextoAnidado<int> _nodo = new ContextoAnidado<int>();     // MapNode.SetUp: raza del nodo
                                                                                             // MapNode.SetUp: node race
        private static readonly ContextoAnidado<Pendiente> _bajas = new ContextoAnidado<Pendiente>();   // fin de batalla
                                                                                                        // end of battle
        private static int _ciudadesCambiadas, _batallasDescontadas;

        // Bajas de unidades nuevas que aún quedan por descontar de UnitKills en la batalla que se está cerrando
        // Kills by new units that are still to be subtracted from UnitKills in the battle being closed
        private sealed class Pendiente { public int Bajas; public int Total; }

        // Nombre de Steam de cada logro -> AchievementId (null = aún no o no se pudo; se reintenta hasta tener alguno)
        // Steam name of each achievement -> AchievementId (null = not yet or could not be obtained; retried until there is at least one)
        private static Dictionary<string, AchievementId> _porNombre;

        /// <summary>
        /// Parches de las facciones nuevas para Plugin.AplicarParchesFacciones: (nombre, tipo, método, argumentos, prefix,
        /// postfix, finalizer, prioridad; -1 = la normal). nameof comprueba al compilar que existen en el interop.
        /// Patches of the new factions for Plugin.AplicarParchesFacciones: (name, type, method, arguments, prefix,
        /// postfix, finalizer, priority; -1 = the normal one). nameof checks at compile time that they exist in the interop.
        /// </summary>
        // Juego 1.11.1: tipo y argumentos DIFERIDOS (Func): cada «() => typeof(X)» se evalúa dentro del try de
        // Plugin.Parchear, así que un tipo que el juego quite o mueva solo hace fallar su parche, no la lista ni Load().
        // Game 1.11.1: DEFERRED type and arguments (Func): each «() => typeof(X)» is evaluated inside the try of
        // Plugin.Parchear, so a type that the game removes or moves only makes its patch fail, not the list or Load().
        public static List<(string nombre, Func<Type> tipo, string metodo, Func<Type[]> args, string prefix, string postfix, string finalizer, int prioridad)> ParchesFacciones()
        {
            Func<Type> tC = () => typeof(CampaignSaveManager);
            return new List<(string, Func<Type>, string, Func<Type[]>, string, string, string, int)>
            {
                // Prioridad alta: el postfix va antes que Objetos.TrasGenerateTown (botín afín con la raza de la ciudad)
                // High priority: the postfix goes before Objetos.TrasGenerateTown (matching loot with the city's race)
                ("Inlining: ciudad de raza nueva (CampaignSaveManager.GenerateTown)", tC, nameof(CampaignSaveManager.GenerateTown), null,
                    nameof(AntesDeGenerateTown), nameof(TrasGenerateTown), nameof(FinGenerateTown), HarmonyLib.Priority.High),
                ("Inlining: guarnición de la ciudad de raza nueva (TabletopTavernData.GetSquadsWithTiersFromRace)", () => typeof(TabletopTavernData),
                    nameof(TabletopTavernData.GetSquadsWithTiersFromRace), null, nameof(AntesDeGetSquadsWithTiersFromRace), null, null, -1),
                ("Inlining: raza del nodo del mapa (MapNode.SetUp)", () => typeof(MapNode), nameof(MapNode.SetUp), null,
                    nameof(AntesDeSetUpNodo), null, nameof(FinSetUpNodo), -1),
                ("Inlining: tiempo del nodo con la región prestada (CampaignSaveManager.GenerateNodeWeather)", tC, nameof(CampaignSaveManager.GenerateNodeWeather), null,
                    nameof(AntesDeGenerateNodeWeather), null, null, -1),
                ("Inlining: bioma del nodo con la región prestada (CampaignSaveManager.GenerateNodeBiome)", tC, nameof(CampaignSaveManager.GenerateNodeBiome), null,
                    nameof(AntesDeGenerateNodeBiome), null, null, -1),
            };
        }

        /// <summary>Parches de los logros para Plugin.AplicarParchesLogros (mismo formato que LogrosFiltrados.Parches()).</summary>
        /// Achievement patches for Plugin.AplicarParchesLogros (same format as LogrosFiltrados.Parches()).
        // Juego 1.11.1: tipo y argumentos DIFERIDOS (Func): cada «() => typeof(X)» se evalúa dentro del try de
        // Plugin.Parchear, así que un tipo que el juego quite o mueva solo hace fallar su parche, no la lista ni Load().
        // Game 1.11.1: DEFERRED type and arguments (Func): each «() => typeof(X)» is evaluated inside the try of
        // Plugin.Parchear, so a type that the game removes or moves only makes its patch fail, not the list or Load().
        public static List<(string nombre, Func<Type> tipo, string metodo, Func<Type[]> args, string prefix, string postfix, string finalizer)> ParchesLogros()
        {
            return new List<(string, Func<Type>, string, Func<Type[]>, string, string, string)>
            {
                ("Inlining: logro de objetos al recoger un objeto (SaveDataHandler.AquiredGear)", () => typeof(SaveDataHandler), nameof(SaveDataHandler.AquiredGear),
                    null, nameof(AntesDeAquiredGear), null, nameof(FinAquiredGear)),
                ("Logros: filtro en el embudo de todos los desbloqueos (SteamStatic.UnlockAchievement)", () => typeof(SteamStatic), nameof(SteamStatic.UnlockAchievement),
                    () => new[] { typeof(string) }, nameof(AntesDeUnlockAchievement), null, null),
                ("Logros: bajas de las unidades nuevas al cerrar la batalla (ArmySpawnManager.MapSquadsToKillsAndWithdrwanSquads)", () => typeof(ArmySpawnManager),
                    nameof(ArmySpawnManager.MapSquadsToKillsAndWithdrwanSquads), null, nameof(AntesDeMapSquads), null, nameof(FinMapSquads)),
                // Juego 1.11.1: SteamAchievements.AddStat(SteamStatId, int) pasó a PlayerAchievements.AddStat(PlayerStatId, int);
                // MapSquadsToKillsAndWithdrwanSquads la llama directamente (call, sin inlining; comprobado en el binario)
                // Game 1.11.1: SteamAchievements.AddStat(SteamStatId, int) became PlayerAchievements.AddStat(PlayerStatId, int);
                // MapSquadsToKillsAndWithdrwanSquads calls it directly (call, no inlining; verified in the binary)
                ("Logros: UnitKills sin las bajas de las unidades nuevas (PlayerAchievements.AddStat)", () => typeof(PlayerAchievements), nameof(PlayerAchievements.AddStat),
                    null, nameof(AntesDeAddStat), null, null),
            };
        }

        // Aviso una sola vez por clave y sesión (estos parches pueden correr a menudo)
        // Warn only once per key and session (these patches may run often)
        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        // ---------------------------------------------------------------
        // 1) Ciudades de las razas nuevas (GenerateTownRace copiado en GenerateTown)
        // 1) Cities of the new races (GenerateTownRace copied into GenerateTown)
        // ---------------------------------------------------------------
        /// <summary>
        /// Prefix de CampaignSaveManager.GenerateTown(int nodo, int nivel): abre el contexto con la raza nueva de la ciudad
        /// (o SinRaza). La raza sale de GenerateTownRace llamado desde aquí, que entra por su dirección y pasa por el
        /// parche de las ciudades: la misma que ven la bandera del mapa (MapNode.UpdateIcon) y TownFlag.
        /// Prefix of CampaignSaveManager.GenerateTown(int nodo, int nivel): opens the context with the city's new race
        /// (or SinRaza). The race comes from GenerateTownRace called from here, which enters through its address and goes through the
        /// city patch: the same one seen by the map banner (MapNode.UpdateIcon) and TownFlag.
        /// </summary>
        public static void AntesDeGenerateTown(CampaignSaveManager __instance, int __0)
        {
            int raza = LogicaInlineados.SinRaza;
            try
            {
                if (FaccionesNuevas.Activas)
                {
                    // Juego 1.11.1: GenerateTownRace(nodo, libro) pasó a GenerateTownRace(nodo, semilla de la campaña, libro).
                    // GenerateTown, MapNode y TownFlag le pasan SaveData.seed (comprobado en el binario: el mismo hash en la copia
                    // de GenerateTown y en TownFlag.SetUp), así que aquí también, para que la raza coincida con la bandera
                    // Game 1.11.1: GenerateTownRace(nodo, libro) became GenerateTownRace(nodo, campaign seed, libro).
                    // GenerateTown, MapNode and TownFlag pass it SaveData.seed (verified in the binary: the same hash in the copy
                    // in GenerateTown and in TownFlag.SetUp), so here too, so that the race matches the banner
                    var datos = __instance.SaveData;
                    raza = LogicaInlineados.RazaCiudadNueva((int)CampaignSaveManager.GenerateTownRace(__0, datos.seed, datos.bookNumber), FaccionesNuevas.EsNueva);
                }
            }
            catch (Exception e) { AvisoUnaVez("ciudad", "Ciudad de raza nueva (se queda la de serie): " + e.Message); raza = LogicaInlineados.SinRaza; }
            _ciudad.Abrir(raza);   // siempre, para que el finalizer cierre exactamente lo que se abrió
                                   // always, so that the finalizer closes exactly what was opened
        }

        /// <summary>Prefix de TabletopTavernData.GetSquadsWithTiersFromRace(Race): dentro de GenerateTown, la raza nueva de la ciudad.</summary>
        /// Prefix of TabletopTavernData.GetSquadsWithTiersFromRace(Race): inside GenerateTown, the city's new race.
        public static void AntesDeGetSquadsWithTiersFromRace(ref Race __0)
        {
            try { __0 = (Race)LogicaInlineados.RazaGuarnicion((int)__0, _ciudad.Abierto, _ciudad.Valor); }
            catch (Exception e) { AvisoUnaVez("guarnicion", "Guarnición de la ciudad de raza nueva (se queda la de serie): " + e.Message); }
        }

        /// <summary>
        /// Postfix de GenerateTown (prioridad alta): pone townData.townRace a la raza nueva. TownPanel lee ese campo para
        /// el nombre, el color, los reclutas («Oferta de ciudades») y la raza del héroe; Objetos.TrasGenerateTown, para el
        /// botín afín (40 -> facción A, 41 -> facción B).
        /// Postfix of GenerateTown (high priority): sets townData.townRace to the new race. TownPanel reads that field for
        /// the name, the color, the recruits («Oferta de ciudades») and the hero's race; Objetos.TrasGenerateTown, for the
        /// matching loot (40 -> faction A, 41 -> faction B).
        /// </summary>
        public static void TrasGenerateTown(CampaignSaveManager __instance, int __0)
        {
            if (!_ciudad.Abierto || _ciudad.Valor == LogicaInlineados.SinRaza) return;
            try
            {
                var ciudad = __instance.SaveData?.townData;
                if (ciudad == null) return;
                Race deSerie = ciudad.townRace;
                ciudad.townRace = (Race)_ciudad.Valor;
                _ciudadesCambiadas++;
                if (_ciudadesCambiadas <= 10 || _ciudadesCambiadas % 20 == 0)
                    Plugin.Info($"Ciudad del nodo {__0}: raza {_ciudad.Valor} con su guarnición (la copia del juego daba {deSerie}; {_ciudadesCambiadas} en la sesión).");
            }
            catch (Exception e) { AvisoUnaVez("ciudadraza", "Raza de la ciudad (se queda la de serie): " + e.Message); }
        }

        /// <summary>Finalizer de GenerateTown: cierra el contexto pase lo que pase y deja pasar la excepción del juego.</summary>
        /// Finalizer of GenerateTown: closes the context no matter what and lets the game's exception through.
        public static Exception FinGenerateTown(Exception __exception) { _ciudad.Cerrar(); return __exception; }

        // ---------------------------------------------------------------
        // 2) Región prestada en los nodos del mapa (GetMapRegion copiado en MapNode.SetUp)
        // 2) Borrowed region on the map nodes (GetMapRegion copied into MapNode.SetUp)
        // ---------------------------------------------------------------
        /// <summary>Prefix de MapNode.SetUp(MapNodeData, Camera, bool, Race): guarda la raza del nodo sin cambiarla.</summary>
        /// Prefix of MapNode.SetUp(MapNodeData, Camera, bool, Race): stores the node's race without changing it.
        public static void AntesDeSetUpNodo(Race __3) => _nodo.Abrir((int)__3);

        /// <summary>Finalizer de MapNode.SetUp: cierra el contexto pase lo que pase.</summary>
        /// Finalizer of MapNode.SetUp: closes the context no matter what.
        public static Exception FinSetUpNodo(Exception __exception) { _nodo.Cerrar(); return __exception; }

        /// <summary>Prefix de CampaignSaveManager.GenerateNodeWeather: dentro de SetUp y con raza nueva, la región prestada.</summary>
        /// Prefix of CampaignSaveManager.GenerateNodeWeather: inside SetUp and with a new race, the borrowed region.
        public static void AntesDeGenerateNodeWeather(ref MapRegion __3) => RegionDelNodo(ref __3, "tiempo");

        /// <summary>Prefix de CampaignSaveManager.GenerateNodeBiome: dentro de SetUp y con raza nueva, la región prestada.</summary>
        /// Prefix of CampaignSaveManager.GenerateNodeBiome: inside SetUp and with a new race, the borrowed region.
        public static void AntesDeGenerateNodeBiome(ref MapRegion __3) => RegionDelNodo(ref __3, "bioma");

        // Cambia la región por la de MapThemeManager.GetMapRegion(raza del nodo), que pasa por el parche de la región prestada
        // Replaces the region with the one from MapThemeManager.GetMapRegion(node race), which goes through the borrowed region patch
        private static void RegionDelNodo(ref MapRegion region, string que)
        {
            try
            {
                if (!LogicaInlineados.CambiarRegion(_nodo.Abierto, _nodo.Valor, FaccionesNuevas.EsNueva)) return;
                var temas = MapThemeManager.Instance;
                var prestada = temas == null ? null : temas.GetMapRegion((Race)_nodo.Valor);
                if (prestada != null) region = prestada;
            }
            catch (Exception e) { AvisoUnaVez("region" + que, $"Región prestada del nodo para el {que} (se queda la del juego): " + e.Message); }
        }

        // ---------------------------------------------------------------
        // 3) Logro de objetos (EvaluateGearCollection copiado en AquiredGear)
        // 3) Gear achievement (EvaluateGearCollection copied into AquiredGear)
        // ---------------------------------------------------------------
        /// <summary>Prefix de SaveDataHandler.AquiredGear: el mismo de EvaluateGearCollection (suspende la lista de gear ampliada).</summary>
        /// Prefix of SaveDataHandler.AquiredGear: the same as EvaluateGearCollection (suspends the extended gear list).
        public static void AntesDeAquiredGear() => LogrosFiltrados.AntesDeEvaluateGearCollection();

        /// <summary>Finalizer de SaveDataHandler.AquiredGear: devuelve la lista ampliada como estaba.</summary>
        /// Finalizer of SaveDataHandler.AquiredGear: restores the extended list as it was.
        public static Exception FinAquiredGear(Exception __exception) => LogrosFiltrados.FinEvaluateGearCollection(__exception);

        // ---------------------------------------------------------------
        // 4) Logros: embudo SteamStatic.UnlockAchievement y bajas de las 9xx en UnitKills
        // 4) Achievements: SteamStatic.UnlockAchievement funnel and the 9xx kills in UnitKills
        // ---------------------------------------------------------------
        /// <summary>
        /// Prefix de SteamStatic.UnlockAchievement(string): traduce el nombre de Steam a AchievementId y aplica el filtro de
        /// LogrosFiltrados. Si el nombre no se reconoce, decide el juego.
        /// Prefix of SteamStatic.UnlockAchievement(string): translates the Steam name to AchievementId and applies the
        /// LogrosFiltrados filter. If the name is not recognized, the game decides.
        /// </summary>
        public static bool AntesDeUnlockAchievement(string __0)
        {
            try
            {
                if (string.IsNullOrEmpty(__0)) return true;
                var mapa = PorNombre();
                return !mapa.TryGetValue(__0, out var id) || LogrosFiltrados.AntesDeUnlock(id);
            }
            catch (Exception e)
            {
                AvisoUnaVez("embudo", "Logros: no se pudo filtrar en UnlockAchievement (decide el juego): " + e.Message);
                return true;
            }
        }

        // Tabla nombre de Steam -> AchievementId con AchievementRegistry.ApiName (la misma traducción que hace Unlock)
        // Steam name -> AchievementId table using AchievementRegistry.ApiName (the same translation that Unlock does)
        private static Dictionary<string, AchievementId> PorNombre()
        {
            if (_porNombre != null) return _porNombre;
            var r = new Dictionary<string, AchievementId>(StringComparer.Ordinal);
            foreach (AchievementId id in Enum.GetValues(typeof(AchievementId)))
            {
                try
                {
                    string nombre = AchievementRegistry.ApiName(id);
                    if (!string.IsNullOrEmpty(nombre)) r[nombre] = id;
                }
                catch { }
            }
            // Revisión del 18-10 (L-5): un resultado vacío (ApiName falló) no se guarda; se reintenta en la siguiente llamada
            // en vez de dejar el embudo sin filtrar toda la sesión
            // Review of 18-10 (L-5): an empty result (ApiName failed) is not saved; it is retried on the next call
            // instead of leaving the funnel unfiltered for the whole session
            if (r.Count == 0)
            {
                AvisoUnaVez("nombresSteam", "Logros: no se pudo traducir ningún nombre de Steam (AchievementRegistry.ApiName); se reintentará en el siguiente logro.");
                return r;
            }
            _porNombre = r;
            Plugin.Info($"Logros: {r.Count} nombres de Steam traducidos para el filtro de UnlockAchievement.");
            return r;
        }

        /// <summary>
        /// Prefix de ArmySpawnManager.MapSquadsToKillsAndWithdrwanSquads(bool): abre el contexto con las bajas que han hecho
        /// las escuadras 9xx del jugador (squadIdKillCounter por UniqueID del ejército guardado), para descontarlas de UnitKills.
        /// Prefix of ArmySpawnManager.MapSquadsToKillsAndWithdrwanSquads(bool): opens the context with the kills made by
        /// the player's 9xx squads (squadIdKillCounter by UniqueID of the saved army), to subtract them from UnitKills.
        /// </summary>
        public static void AntesDeMapSquads(ArmySpawnManager __instance)
        {
            var p = new Pendiente();
            try
            {
                if (Plugin.CfgLogrosFiltrar == null || Plugin.CfgLogrosFiltrar.Value)
                {
                    var ids = __instance.uniqueIDToSquadId;
                    var bajas = __instance.squadIdKillCounter;
                    var ejercito = BattleManager.Instance.BattleSaveManager.GetArmyFromSaveData(true).Item1;
                    var escuadras = new List<(int, int)>();
                    for (int i = 0; ids != null && bajas != null && ejercito != null && i < ejercito.Length; i++)
                    {
                        var s = ejercito[i];
                        if (s == null || s.UniqueID == null || !ids.ContainsKey(s.UniqueID)) continue;
                        int id = ids[s.UniqueID];
                        escuadras.Add(((int)s.UnitName, bajas.ContainsKey(id) ? bajas[id] : 0));
                    }
                    p.Bajas = p.Total = LogicaInlineados.BajasNuevas(escuadras);
                }
            }
            catch (Exception e) { AvisoUnaVez("bajas", "Logros: no se pudieron contar las bajas de las unidades nuevas (suman como en el juego): " + e.Message); p.Bajas = p.Total = 0; }
            _bajas.Abrir(p);
        }

        /// <summary>Finalizer de MapSquadsToKillsAndWithdrwanSquads: cierra el contexto pase lo que pase.</summary>
        /// Finalizer of MapSquadsToKillsAndWithdrwanSquads: closes the context no matter what.
        public static Exception FinMapSquads(Exception __exception)
        {
            try
            {
                var p = _bajas.Valor;
                if (_bajas.Abierto && p != null && p.Total > 0)
                {
                    _batallasDescontadas++;
                    if (_batallasDescontadas <= 10 || _batallasDescontadas % 20 == 0)
                        Plugin.Info($"Logros: {p.Total - p.Bajas} de {p.Total} bajas de unidades nuevas no suman a la estadística UnitKills de Steam ({_batallasDescontadas} batallas en la sesión).");
                }
            }
            catch { }
            _bajas.Cerrar();
            return __exception;
        }

        /// <summary>Prefix de PlayerAchievements.AddStat(PlayerStatId, int) (SteamAchievements.AddStat(SteamStatId, int) hasta el
        /// juego 1.10.17): dentro del cierre de batalla, UnitKills sin las bajas 9xx.</summary>
        /// Prefix of PlayerAchievements.AddStat(PlayerStatId, int) (SteamAchievements.AddStat(SteamStatId, int) up to
        /// game 1.10.17): inside the battle wrap-up, UnitKills without the 9xx kills.
        public static void AntesDeAddStat(PlayerStatId __0, ref int __1)
        {
            try
            {
                if (!_bajas.Abierto || __0 != PlayerStatId.UnitKills) return;
                var p = _bajas.Valor;
                if (p == null || p.Bajas <= 0) return;
                __1 = LogicaInlineados.Descontar(__1, ref p.Bajas);
            }
            catch (Exception e) { AvisoUnaVez("addstat", "Logros: no se pudo descontar de UnitKills (suma como en el juego): " + e.Message); }
        }
    }
}
