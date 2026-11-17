// LogicaLogros.cs
// Cálculos puros (sin tipos del juego ni de Unity) de LogrosFiltrados.cs, para poder probarlos fuera del juego
// (proyecto pruebas\, PruebasLogros.cs). Decisión de Zero (17-10-2026): los logros de Steam de serie cuentan solo
// unidades y héroes de serie. Las unidades 9xx (también las «sueltas» que van en listas de razas de serie) y los
// héroes 9xx (901-904) ni suman ni bloquean ningún logro de colección, de héroes o de ejército.
// Dos casos:
//   - BLOQUEAN (falso negativo): la colección de una raza de serie y «Grand Collector» piden todas las unidades de la
//     lista de la raza, y las sueltas están en esa lista. Se recalcula con la lista sin 9xx y se concede si está completa.
//   - SUMAN (falso positivo): «Max Difficulty All Heroes», «Roster Complete», «Four Unique Factions», «Grand Alliance»,
//     «Legends Assembled» y «Master of Arms» llegan a su umbral contando héroes o unidades 9xx (o las razas 40/41). Se
//     recuenta sin 9xx y, si solo llegan gracias a ellos, no se conceden.
// 0.7.5 (revisión total del 02-11): los 8 logros de composición del ejército ya no se bloquean para siempre por tener una
// partida con contenido nuevo en el historial: el historial se recuenta por partida (BloquearDesdeHistorial) y fuera de
// contexto solo cuenta la sesión y la partida en curso (BloquearSinContexto).
// Autor: ZeroitMods.
// LogicaLogros.cs
// Pure calculations (no game or Unity types) from LogrosFiltrados.cs, so they can be tested outside the game
// (project pruebas\, PruebasLogros.cs). Decision by Zero (17-10-2026): stock Steam achievements count only
// stock units and heroes. 9xx units (including the «loose» ones in stock race lists) and 9xx
// heroes (901-904) neither add to nor block any Collection, hero or army achievement.
// Two cases:
//   - BLOCK (false negative): the Collection of a stock race and «Grand Collector» require all the units in the
//     race list, and the loose ones are in that list. It is recalculated with the list without 9xx and granted if complete.
//   - ADD (false positive): «Max Difficulty All Heroes», «Roster Complete», «Four Unique Factions», «Grand Alliance»,
//     «Legends Assembled» and «Master of Arms» reach their threshold by counting 9xx heroes or units (or races 40/41). They are
//     recounted without 9xx and, if they only reach it thanks to them, they are not granted.
// 0.7.5 (full review of 02-11): the 8 army composition achievements are no longer blocked forever because of having a
// run with new content in the history: the history is recounted per run (BloquearDesdeHistorial) and out of
// context only the session and the current run are counted (BloquearSinContexto).
// Author: ZeroitMods.

using System.Collections.Generic;
using System.Linq;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: lógica pura del filtrado de logros
    // Section: pure logic of achievement filtering
    // -----------------------------------------------------------------------
    public static class LogicaLogros
    {
        // Primer id de los contenidos de Zero: unidades 900-999 y héroes 901-999 (los de serie son menores)
        // First id of Zero's content: units 900-999 and heroes 901-999 (stock ones are lower)
        public const int PrimerIdNuevo = 900;
        // Umbrales leídos del binario del juego instalado (17-10-2026, desensamblado de GameAssembly.dll):
        //   CampaignSaveManager.CheckGodkingCompletionAchievements: «cmp edi,10h» (16 héroes en Godking)
        //   SaveDataHandler.ApplyVictoryUnlocks: «cmp esi,10h» (16 héroes con alguna victoria, Roster Complete)
        //   CampaignSaveManager.CheckForFourFactions: «cmp dword ptr [rdi+18h],4» (4 razas en el ejército)
        // Thresholds read from the installed game binary (17-10-2026, disassembly of GameAssembly.dll):
        //   CampaignSaveManager.CheckGodkingCompletionAchievements: «cmp edi,10h» (16 heroes in Godking)
        //   SaveDataHandler.ApplyVictoryUnlocks: «cmp esi,10h» (16 heroes with some victory, Roster Complete)
        //   CampaignSaveManager.CheckForFourFactions: «cmp dword ptr [rdi+18h],4» (4 races in the army)
        public const int UmbralHeroesGodking = 16;
        public const int UmbralHeroesRoster = 16;
        public const int UmbralCuatroFacciones = 4;
        // Por defecto de AchievementRules (si no se pueden leer del juego): Legends Assembled, Master of Arms, Grand Alliance
        // AchievementRules defaults (if they cannot be read from the game): Legends Assembled, Master of Arms, Grand Alliance
        public const int UmbralLegendariasPorDefecto = 3;
        public const int UmbralRasgosPorDefecto = 5;
        public const int UmbralFaccionesPorDefecto = 8;

        /// <summary>¿Es una unidad de serie? (UnitName de 0 a 899; las 9xx son de los módulos de Zero)</summary>
        /// Is it a stock unit? (UnitName from 0 to 899; the 9xx are from Zero's modules)
        public static bool EsUnidadDeSerie(int unidad) => unidad >= 0 && unidad < PrimerIdNuevo;

        /// <summary>¿Es un héroe de serie? (HeroID de 1 a 899; 901-904 y demás 9xx son de los módulos de Zero)</summary>
        /// Is it a stock hero? (HeroID from 1 to 899; 901-904 and other 9xx are from Zero's modules)
        public static bool EsHeroeDeSerie(int heroe) => heroe > 0 && heroe < PrimerIdNuevo;

        /// <summary>¿Lleva la lista alguna unidad nueva (9xx o fuera de rango)? Si no, el juego ya decide bien solo.</summary>
        /// Does the list contain any new unit (9xx or out of range)? If not, the game already decides correctly on its own.
        public static bool TieneUnidadesNuevas(IEnumerable<int> lista) => lista != null && lista.Any(u => !EsUnidadDeSerie(u));

        /// <summary>
        /// Colección de una raza con la lista sin unidades nuevas: true si quedan unidades de serie y están todas
        /// reclutadas. Lista vacía tras filtrar = false (misma guarda del juego: un mod que vacía la lista no regala el logro).
        /// Collection of a race with the list without new units: true if stock units remain and all of them are
        /// recruited. Empty list after filtering = false (same guard as the game: a mod that empties the list does not hand out the achievement).
        /// </summary>
        public static bool ColeccionDeSerieCompleta(IEnumerable<int> lista, ICollection<int> reclutadas)
        {
            if (lista == null || reclutadas == null) return false;
            var deSerie = lista.Where(EsUnidadDeSerie).ToList();
            return deSerie.Count > 0 && deSerie.All(reclutadas.Contains);
        }

        /// <summary>
        /// Héroes con victoria contados como el juego (todos) y solo los de serie. <paramref name="cuenta"/> dice si una
        /// entrada del save cuenta (alguna dificultad para Roster Complete; Godking para Max Difficulty All Heroes).
        /// Heroes with a victory counted like the game does (all) and only the stock ones. <paramref name="cuenta"/> says whether a
        /// save entry counts (any difficulty for Roster Complete; Godking for Max Difficulty All Heroes).
        /// </summary>
        public static (int todos, int deSerie) ContarHeroes(IEnumerable<(int heroe, IReadOnlyCollection<int> dificultades)> entradas,
                                                           System.Func<IReadOnlyCollection<int>, bool> cuenta)
        {
            int todos = 0, deSerie = 0;
            foreach (var (heroe, dificultades) in entradas ?? Enumerable.Empty<(int, IReadOnlyCollection<int>)>())
            {
                if (dificultades == null || !cuenta(dificultades)) continue;
                todos++;
                if (EsHeroeDeSerie(heroe)) deSerie++;
            }
            return (todos, deSerie);
        }

        /// <summary>Entrada que cuenta para Roster Complete: alguna dificultad completada.</summary>
        /// Entry that counts for Roster Complete: any difficulty completed.
        public static bool AlgunaDificultad(IReadOnlyCollection<int> dificultades) => dificultades != null && dificultades.Count > 0;

        /// <summary>Entrada que cuenta para Max Difficulty All Heroes: la dificultad Godking completada.</summary>
        /// Entry that counts for Max Difficulty All Heroes: Godking difficulty completed.
        public static System.Func<IReadOnlyCollection<int>, bool> ConDificultad(int godking) => d => d != null && d.Contains(godking);

        /// <summary>
        /// Regla común de los logros que SUMAN: se bloquea si el recuento sin contenidos nuevos no llega al umbral y el
        /// recuento del juego es mayor (es decir, solo llega gracias a lo nuevo). Si lo nuevo no cambia nada, decide el juego.
        /// Common rule for the ADD achievements: it is blocked if the count without new content does not reach the threshold and the
        /// game's count is higher (that is, it only reaches it thanks to the new content). If the new content changes nothing, the game decides.
        /// </summary>
        public static bool Bloquear(int sinNuevos, int conTodo, int umbral) => sinNuevos < umbral && conTodo > sinNuevos;

        /// <summary>
        /// Razas distintas en el ejército, con todas las unidades y solo con las de serie. <paramref name="contarEspecial"/>
        /// = false deja fuera la raza Special (Grand Alliance); Four Unique Factions la cuenta, como el juego.
        /// Las razas nuevas (40/41) solo pueden llegar con unidades 9xx, así que quedan fuera al filtrar unidades.
        /// Distinct races in the army, with all units and only with stock ones. <paramref name="contarEspecial"/>
        /// = false leaves out the Special race (Grand Alliance); Four Unique Factions counts it, like the game.
        /// New races (40/41) can only come with 9xx units, so they are left out when filtering units.
        /// </summary>
        public static (int todas, int deSerie) ContarRazas(IEnumerable<(int unidad, int raza)> escuadras, bool contarEspecial, int especial)
        {
            var todas = new HashSet<int>();
            var deSerie = new HashSet<int>();
            foreach (var (unidad, raza) in escuadras ?? Enumerable.Empty<(int, int)>())
            {
                if (!contarEspecial && raza == especial) continue;
                todas.Add(raza);
                if (EsUnidadDeSerie(unidad)) deSerie.Add(raza);
            }
            return (todas.Count, deSerie.Count);
        }

        /// <summary>Escuadras que cumplen algo, con todas las unidades y solo con las de serie (p. ej. legendarias).</summary>
        /// Squads that meet a condition, with all units and only with stock ones (e.g. legendary).
        public static (int todas, int deSerie) ContarEscuadras(IEnumerable<(int unidad, bool cumple)> escuadras)
        {
            int todas = 0, deSerie = 0;
            foreach (var (unidad, cumple) in escuadras ?? Enumerable.Empty<(int, bool)>())
            {
                if (!cumple) continue;
                todas++;
                if (EsUnidadDeSerie(unidad)) deSerie++;
            }
            return (todas, deSerie);
        }

        /// <summary>Rasgos de prestigio distintos (sin «ninguno»), con todas las unidades y solo con las de serie.</summary>
        /// Distinct prestige traits (without «none»), with all units and only with stock ones.
        public static (int todos, int deSerie) ContarRasgos(IEnumerable<(int unidad, int rasgo)> escuadras, int ninguno)
        {
            var todos = new HashSet<int>();
            var deSerie = new HashSet<int>();
            foreach (var (unidad, rasgo) in escuadras ?? Enumerable.Empty<(int, int)>())
            {
                if (rasgo == ninguno) continue;
                todos.Add(rasgo);
                if (EsUnidadDeSerie(unidad)) deSerie.Add(rasgo);
            }
            return (todos.Count, deSerie.Count);
        }

        // ---------------------------------------------------------------
        // Juego 1.11.1 (revisión del 01-11-2026): logros de «ejército» sin contexto de recuento
        // Game 1.11.1 (review of 01-11-2026): «army» achievements without a count context
        // ---------------------------------------------------------------
        /// <summary>
        /// ¿Hay en el historial de partidas alguna jugada con contenido nuevo? Sí si alguna partida está anclada a los módulos
        /// en el registro (<paramref name="anclada"/> con su runUUID), si su héroe es 9xx o si alguna de sus unidades es 9xx.
        /// Los saves guardan las 9xx como su unidad base (saneado), así que las unidades casi nunca lo delatan: lo que manda es
        /// el registro y el héroe. Sirve para AchievementSync.AwardFromRunHistory, que concede HiredSwords, GoblinHorde,
        /// KoboldSwarm y ShieldSisters (AchievementRules.ForFinalArmy) recorriendo TODAS las partidas del historial sin decir
        /// cuál cumplió: si alguna tiene contenido nuevo, no se puede recontar con seguridad y se bloquea.
        /// Does the run history contain any run with new content? Yes if any run is anchored to the modules
        /// in the registry (<paramref name="anclada"/> with its runUUID), if its hero is 9xx or if any of its units is 9xx.
        /// The saves store the 9xx as their base unit (sanitizing), so the units almost never give it away: what matters is
        /// the registry and the hero. It is used by AchievementSync.AwardFromRunHistory, which grants HiredSwords, GoblinHorde,
        /// KoboldSwarm and ShieldSisters (AchievementRules.ForFinalArmy) by going through ALL the runs in the history without saying
        /// which one met the requirement: if any has new content, it cannot be safely recounted and it is blocked.
        /// </summary>
        // 0.7.5, ronda 2 de la revisión independiente (hallazgo alto): las partidas que terminaron entre el primer módulo de
        // Zero (16-10-2026; el plugin «facción A» con la 900 es de ese día) y el día en que el registro ya marcaba
        // todas las partidas jugadas con módulos (núcleo 0.7.2 con la marca M-2, instalado el 18-10-2026 a las 20:11 hora
        // de España; se toma el 19-10-2026 00:00 UTC para cubrir ese día entero) no tienen marca y el historial guarda sus
        // 9xx ya cambiadas por su base (caso real: ba32f363, 17-10, solo con la unidad 70, base de la 900). Ante la duda,
        // cuentan como partidas con contenido nuevo. Ticks de .NET en UTC (como RunRecord.endedAtUtcTicks)
        // 0.7.5, round 2 of the independent review (high-severity finding): runs that ended between Zero's first module
        // (16-10-2026; the «faction A» plugin with 900 dates from that day) and the day the registry already marked
        // all runs played with modules (core 0.7.2 with the M-2 mark, installed on 18-10-2026 at 20:11 Spain
        // time; 19-10-2026 00:00 UTC is used to cover that entire day) have no mark and the history stores their
        // 9xx already replaced by their base (real case: ba32f363, 17-10, only with unit 70, base of 900). When in doubt,
        // they count as runs with new content. .NET ticks in UTC (like RunRecord.endedAtUtcTicks)
        public static readonly long InicioModulosTicks = new System.DateTime(2026, 10, 16, 0, 0, 0, System.DateTimeKind.Utc).Ticks;
        public static readonly long InicioMarcasTicks = new System.DateTime(2026, 10, 19, 0, 0, 0, System.DateTimeKind.Utc).Ticks;

        /// <summary>¿Terminó la partida cuando ya había módulos de Zero pero el registro aún no marcaba las partidas? (0 = sin fecha: no)</summary>
        /// Did the run end when Zero's modules already existed but the registry did not yet mark runs? (0 = no date: no)
        public static bool SinMarcasPosibles(long finTicks) => finTicks >= InicioModulosTicks && finTicks < InicioMarcasTicks;

        public static bool HistorialConContenidoNuevo(IEnumerable<(string partida, int heroe, IEnumerable<int> unidades)> partidas,
                                                      System.Func<string, bool> anclada)
        {
            if (partidas == null) return false;
            foreach (var (partida, heroe, unidades) in partidas)
            {
                if (heroe >= PrimerIdNuevo) return true;
                if (!string.IsNullOrEmpty(partida) && anclada != null && anclada(partida)) return true;
                if (TieneUnidadesNuevas(unidades)) return true;
            }
            return false;
        }

        /// <summary>
        /// Decisión de un logro de ejército fuera de un contexto que permita recontar sin 9xx (regla «ante la duda, bloquear»:
        /// un logro no concedido se puede ganar después; uno concedido en falso no se quita de Steam). 0.7.5 (revisión total
        /// del 02-11): se bloquea si la sesión tiene módulos de Zero activos o si la partida en curso está anclada a los módulos.
        /// El historial ya no cuenta aquí (antes bloqueaba para siempre): lo que viene del historial se decide por partida
        /// (BloquearDesdeHistorial).
        /// Decision for an army achievement outside a context that allows recounting without 9xx (rule "when in doubt, block":
        /// an achievement not granted can be earned later; one granted wrongly cannot be removed from Steam). 0.7.5 (full review
        /// of 02-11): it is blocked if the session has active Zero modules or if the current run is anchored to the modules.
        /// The history no longer counts here (it used to block forever): what comes from the history is decided per run
        /// (BloquearDesdeHistorial).
        /// </summary>
        public static bool BloquearSinContexto(bool sesionConModulos, bool partidaEnCursoAnclada) => sesionConModulos || partidaEnCursoAnclada;

        /// <summary>
        /// 0.7.5: decisión de un logro de ejército que concede AchievementSync.AwardFromRunHistory. Sin contenido nuevo en el
        /// historial, decide el juego. Con él: si se ha podido recontar por partida (<paramref name="recuentoPorPartida"/>), se
        /// concede solo si alguna partida de serie lo da (<paramref name="limpios"/>); si no se ha podido, se bloquea (regla
        /// vieja, ante la duda).
        /// 0.7.5: decision for an army achievement granted by AchievementSync.AwardFromRunHistory. With no new content in the
        /// history, the game decides. With it: if it could be recounted per run (<paramref name="recuentoPorPartida"/>), it is
        /// granted only if some stock run gives it (<paramref name="limpios"/>); if it could not, it is blocked (old
        /// rule, when in doubt).
        /// </summary>
        public static bool BloquearDesdeHistorial(int logro, bool historialConNuevo, bool recuentoPorPartida, ICollection<int> limpios)
        {
            if (!historialConNuevo) return false;
            if (!recuentoPorPartida) return true;
            return limpios == null || !limpios.Contains(logro);
        }
    }
}
