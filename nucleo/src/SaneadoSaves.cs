// SaneadoSaves.cs
// Transformaciones de texto JSON para que los ficheros de guardado NUNCA contengan unidades nuevas (ids 9xx).
// SaneadoSaves.cs
// JSON text transformations so that save files NEVER contain new units (ids 9xx).
//
// Al escribir: cada escuadra de una unidad nueva ("UnitName": 9xx + "UniqueID") se guarda como su UNIDAD BASE (la
// 900 como EisenmannRegiment, la 944 como Oni...) y su GUID se apunta en el registro aparte con el id real
// (RegistroGuardias). Las listas sin GUID se limpian: las ofertas de reclutamiento se cambian por la base y se
// recuerda la posición y el id; las listas de colección quitan la unidad y anotan cuál era.
// Al leer: se hace lo inverso con lo apuntado en el registro, por GUID (nunca por posición), y solo para las
// unidades que existen ahora en el catálogo y cuya base coincide con la del save.
// Resultado: sin el plugin la partida carga y cada unidad nueva aparece como su unidad base.
// Vale para todas las listas de escuadras del save: ejército propio, ejército enemigo, guarniciones y retiradas.
// Las ofertas se apuntan por separado para el save normal y la instantánea del nodo ("snapShot": true).
// Si un GUID apuntado no se puede restaurar (la base del save no cuadra o la unidad ya no existe), se avisa una vez por
// GUID y sesión (AvisarNoRestaurable) y la entrada del registro se conserva.
// On write: each squad of a new unit ("UnitName": 9xx + "UniqueID") is saved as its BASE UNIT (the
// 900 as EisenmannRegiment, the 944 as Oni...) and its GUID is recorded in the separate registry with the real id
// (RegistroGuardias). Lists without a GUID are cleaned: recruitment offers are replaced by the base and the
// position and id are remembered; collection lists remove the unit and record which one it was.
// On read: the reverse is done with what was recorded in the registry, by GUID (never by position), and only for the
// units that currently exist in the catalog and whose base matches the one in the save.
// Result: without the plugin the run loads and each new unit appears as its base unit.
// Applies to all squad lists in the save: own army, enemy army, garrisons and retreated.
// Offers are recorded separately for the normal save and the node snapshot ("snapShot": true).
// If a recorded GUID cannot be restored (the save's base does not match or the unit no longer exists), a warning is issued once per
// GUID and session (AvisarNoRestaurable) and the registry entry is kept.
//
// 0.5.0 (F2): las razas nuevas 40/41 tampoco llegan al disco donde romperían el juego sin plugin (GetRaceData(40)
// devuelve null): la raza de la ciudad actual (townData.townRace) y la del campo de batalla preparado
// (battleFieldPreset.race) se guardan como su raza sustituta de serie y la real se apunta en el registro con una firma;
// de los temas de taberna desbloqueados (playerSaveData) se quitan 40/41, que no tienen tema. Hasta la 0.7.4 el historial
// de batallas («race» de battleLog y de los registros de partidas) se dejaba tal cual; desde la 0.7.5 también se sanea
// (sección «Historial de partidas y registro de batallas», más abajo).
// 0.5.0 (F2): the new races 40/41 also do not reach the disk, where they would break the game without the plugin (GetRaceData(40)
// returns null): the race of the current town (townData.townRace) and that of the prepared battlefield
// (battleFieldPreset.race) are saved as their stock replacement race and the real one is recorded in the registry with a signature;
// from the unlocked tavern themes (playerSaveData) 40/41 are removed, as they have no theme. Up to 0.7.4 the battle
// history («race» of battleLog and of the run records) was left as is; since 0.7.5 it is also sanitized
// (section «Historial de partidas y registro de batallas», below).
//
// 0.7.1: los héroes nuevos 901-904 tampoco llegan al disco en el save de campaña (héroe del jugador «heroID» y señor de la
// guerra enemigo «enemyWarlordHeroID»): se guardan como su héroe prestado de serie y el real se apunta en el registro con
// la firma de la partida (su runId). Sin su módulo (o sin el plugin) la partida carga con el héroe prestado en vez de caer
// en Edric con el retrato roto; al activar el módulo otra vez vuelve el héroe nuevo. El historial de partidas del save del
// jugador («heroID» de cada registro) se dejaba tal cual hasta la 0.7.4 (desde la 0.7.5 se sanea, ver más abajo).
// 0.7.5 (revisión total del 02-11): las ranuras de los saves de campaña llevan la partida y el slot (ContextoSave): la
// partida diaria o cooperativa ya no borra lo apuntado de la principal. Los arrays de unidades nuevos de la 1.11.1
// (battleRecruitOffer, mercenariesLeft, coopStartArmy) se tratan como las ofertas, y las listas por héroe del save del
// jugador incluyen heroLooks y lifetime.heroes.
// 0.7.2: el último héroe usado del save del jugador («lastHeroID», escalar) también se guarda como su prestado y el real
// se apunta en el registro (ranura «ultimoHeroe», sin firma de partida); al leer vuelve si su módulo está activo.
// 0.7.2 (revisión): las victorias de los héroes nuevos del save del jugador (entradas 9xx de HeroDifficultiesCompleted y
// HeroLastDifficulties) se quitan al escribir y se apuntan en el registro; al leer vuelven las de los héroes activos.
// Así sin el plugin no cuentan para los logros de serie. Además, cada etapa de ida al disco (unidades, razas, héroes; los
// objetos en SaneadoObjetos.cs) devuelve tal cual un texto que ya salió saneado por ella (memoria de textos saneados de
// RegistroGuardias): el juego pasa cada save dos veces por el saneado y la segunda pasada olvidaba lo apuntado.
// Revisión del 18-10 (M-1): las bajas históricas de las unidades nuevas (entradas 9xx de UnitNameHistoricalKillStore del
// save del jugador, {"UnitName": n, "Kills": k} sin GUID) ya no se cambian por su base (eso duplicaba la entrada de la base
// en cada sesión y la 9xx perdía sus bajas): se quitan al escribir y se apuntan en el registro; al leer vuelven las de las
// unidades disponibles.
// 0.7.1: the new heroes 901-904 also do not reach the disk in the campaign save (player hero «heroID» and enemy warlord
// «enemyWarlordHeroID»): they are saved as their stock borrowed hero and the real one is recorded in the registry with
// the run's signature (its runId). Without its module (or without the plugin) the run loads with the borrowed hero instead of falling
// back to Edric with a broken portrait; when the module is activated again the new hero returns. The run history of the player's
// save («heroID» of each record) was left as is until 0.7.4 (since 0.7.5 it is sanitized, see below).
// 0.7.5 (full review of 02-11): the slots of campaign saves carry the run and the slot (ContextoSave): the
// daily or co-op run no longer erases what was recorded for the main one. The new unit arrays from 1.11.1
// (battleRecruitOffer, mercenariesLeft, coopStartArmy) are handled like the offers, and the per-hero lists of the player's
// save include heroLooks and lifetime.heroes.
// 0.7.2: the last hero used in the player's save («lastHeroID», scalar) is also saved as its borrowed one and the real one
// is recorded in the registry (slot «ultimoHeroe», without run signature); on read it returns if its module is active.
// 0.7.2 (review): the victories of the new heroes in the player's save (9xx entries of HeroDifficultiesCompleted and
// HeroLastDifficulties) are removed on write and recorded in the registry; on read those of the active heroes return.
// This way, without the plugin they do not count towards the stock achievements. Also, each write-to-disk stage (units, races, heroes; the
// gear in SaneadoObjetos.cs) returns as is a text that already came out sanitized by it (memory of sanitized texts of
// RegistroGuardias): the game passes each save through sanitizing twice and the second pass forgot what was recorded.
// Review of 18-10 (M-1): the historical kills of new units (9xx entries of UnitNameHistoricalKillStore in the
// player's save, {"UnitName": n, "Kills": k} without GUID) are no longer replaced by their base (that duplicated the base's entry
// in every session and the 9xx lost its kills): they are removed on write and recorded in the registry; on read those of the
// available units return.
//
// Este fichero es .NET puro (sin tipos del juego ni de Unity) para poder probarlo fuera del juego
// (proyecto pruebas\).
// This file is pure .NET (no game or Unity types) so it can be tested outside the game
// (proyecto pruebas\).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: tabla de unidades nuevas (id -> número de su unidad base)
    // Section: new units table (id -> number of its base unit)
    // -----------------------------------------------------------------------
    /// <summary>Ids nuevos y el número (ordinal del enum UnitName) de su unidad base, más el filtro de disponibilidad.</summary>
    /// New ids and the number (ordinal of the UnitName enum) of their base unit, plus the availability filter.
    public sealed class TablaUnidades
    {
        private readonly Dictionary<int, int> _basePorId;
        public IReadOnlyDictionary<int, int> BasePorId => _basePorId;

        /// <summary>¿Existe ahora esta unidad en el catálogo? (solo se restauran al leer las que existen). null = todas.</summary>
        /// Does this unit currently exist in the catalog? (only those that exist are restored on read). null = all.
        public Func<int, bool> Disponible;

        // Detector rápido: algún id nuevo aparece como número suelto en el texto
        // Quick detector: does any new id appear as a standalone number in the text
        internal readonly Regex ReIds;

        public TablaUnidades(IDictionary<int, int> basePorId)
        {
            _basePorId = new Dictionary<int, int>(basePorId);
            string alternativas = _basePorId.Count == 0 ? "(?!)" : string.Join("|", _basePorId.Keys.OrderByDescending(k => k));
            ReIds = new Regex("(?<![\\w.])(?:" + alternativas + ")(?![\\w.])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public bool Contiene(int id) => _basePorId.ContainsKey(id);
        public bool Contiene(string texto) => int.TryParse(texto, out int id) && _basePorId.ContainsKey(id);
        public int BaseDe(int id) => _basePorId.TryGetValue(id, out int b) ? b : -1;
        internal bool Restaurable(int id, int numeroEnSave) =>
            _basePorId.TryGetValue(id, out int b) && b == numeroEnSave && (Disponible == null || Disponible(id));
    }

    // -----------------------------------------------------------------------
    // Sección: tabla de razas nuevas (raza nueva -> raza sustituta de serie que se escribe en disco)
    // Section: new races table (new race -> stock replacement race that is written to disk)
    // -----------------------------------------------------------------------
    /// <summary>Razas nuevas (40/41) y la raza de serie que se guarda en el disco en su lugar.</summary>
    /// New races (40/41) and the stock race that is saved to disk in their place.
    public sealed class TablaRazas
    {
        private readonly Dictionary<int, int> _sustituta;
        public TablaRazas(IDictionary<int, int> sustitutaPorRaza) { _sustituta = new Dictionary<int, int>(sustitutaPorRaza); }
        public bool EsNueva(int raza) => _sustituta.ContainsKey(raza);
        public int SustitutaDe(int raza) => _sustituta.TryGetValue(raza, out int s) ? s : raza;
        public int Numero => _sustituta.Count;
        /// <summary>Tema de taberna que se guarda si el elegido fuera de una raza nueva: Special (8), el tema por defecto.</summary>
        /// Tavern theme saved if the chosen one belonged to a new race: Special (8), the default theme.
        public int TemaRespaldo { get; set; } = 8;
    }

    // -----------------------------------------------------------------------
    // Sección: tabla de héroes nuevos (héroe nuevo -> héroe de serie prestado que se escribe en disco) (0.7.1)
    // Section: new heroes table (new hero -> stock borrowed hero that is written to disk) (0.7.1)
    // -----------------------------------------------------------------------
    /// <summary>
    /// Héroes nuevos (901-904) de los módulos INSTALADOS (activos o no) y el héroe de serie que se guarda en el disco en su
    /// lugar (su «PrestadoDe»). <see cref="Disponible"/> dice si el héroe está activo ahora (su módulo aplicado): solo
    /// esos se restauran al leer; los demás se cargan como su prestado.
    /// New heroes (901-904) from INSTALLED modules (active or not) and the stock hero that is saved to disk in their
    /// place (their "PrestadoDe"). <see cref="Disponible"/> says whether the hero is currently active (its module applied): only
    /// those are restored on read; the rest are loaded as their borrowed hero.
    /// </summary>
    public sealed class TablaHeroes
    {
        private readonly Dictionary<int, int> _prestado;
        public TablaHeroes(IDictionary<int, int> prestadoPorId) { _prestado = new Dictionary<int, int>(prestadoPorId); }

        /// <summary>¿Está activo ahora este héroe nuevo? null = todos los de la tabla.</summary>
        /// Is this new hero currently active? null = all those in the table.
        public Func<int, bool> Disponible;

        public int Numero => _prestado.Count;
        public bool EsNuevo(int heroe) => _prestado.ContainsKey(heroe);
        public int PrestadoDe(int heroe) => _prestado.TryGetValue(heroe, out int p) ? p : heroe;
        public bool Activo(int heroe) => EsNuevo(heroe) && (Disponible == null || Disponible(heroe));
    }

    // -----------------------------------------------------------------------
    // Sección: contexto de un texto de save (0.7.5, revisión total del 02-11): partida, slot e instantánea
    // Section: context of a save text (0.7.5, full review of 02-11): run, slot and snapshot
    // -----------------------------------------------------------------------
    /// <summary>
    /// De qué es un texto de save, para la clave de sus ranuras en el registro. El juego 1.11.1 tiene cuatro saves de
    /// campaña (SaveDataHandler.CampaignSlot: Main, Daily, Coop y CoopView, cada uno con su instantánea) con el mismo tipo
    /// CampaignSaveData. Hasta la 0.7.4 todos compartían las ranuras («heroe», «Gear», «ciudad»...) y guardar la partida
    /// diaria borraba lo apuntado de la principal. Ahora la clave de un save de campaña lleva el slot y la firma de la
    /// partida («heroe@principal@&lt;runId&gt;»): guardar una partida nunca toca lo de otra. El slot sale SOLO del texto
    /// (la lectura no sabe de qué fichero viene): «dailyDay» no vacío = diaria, «coop»: true = cooperativo. El
    /// cooperativo del invitado (CoopView) no se distingue del del anfitrión por el texto y comparte su slot; su firma
    /// (runId) es la de la partida del anfitrión, así que tampoco se mezcla con la principal.
    /// Los textos que no son de campaña (save del jugador, batalla personalizada, piezas sueltas) usan la ranura de siempre.
    /// What a save text belongs to, for the key of its slots in the core registry. Game 1.11.1 has four campaign saves
    /// (SaveDataHandler.CampaignSlot: Main, Daily, Coop and CoopView, each with its snapshot) with the same type
    /// CampaignSaveData. Up to 0.7.4 they all shared the slots («heroe», «Gear», «ciudad»...) and saving the daily
    /// run wiped what was recorded for the main one. Now the key of a campaign save carries the slot and the signature of the
    /// run («heroe@principal@&lt;runId&gt;»): saving one run never touches another's data. The slot comes ONLY from the text
    /// (the read doesn't know which file it comes from): non-empty «dailyDay» = daily, «coop»: true = co-op. The
    /// guest's co-op (CoopView) can't be told apart from the host's by the text and shares its slot; its signature
    /// (runId) is that of the host's run, so it doesn't get mixed with the main one either.
    /// Texts that aren't campaign saves (player save, custom battle, loose pieces) use the usual slot.
    /// </summary>
    public sealed class ContextoSave
    {
        public const string SlotPrincipal = "principal", SlotDiaria = "diaria", SlotCoop = "coop";
        // Firma de las partidas sin runId ni semilla (no debería pasar; así nunca se mezcla con una partida real)
        // Signature of runs with no runId or seed (shouldn't happen; this way it never gets mixed with a real run)
        public const string SinFirma = "sin-firma";
        private static readonly Regex ReDiaria = new Regex(@"""dailyDay""\s*:\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ReCoop = new Regex(@"""coop""\s*:\s*true", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public bool Partida { get; private set; }       // ¿save de campaña (o su instantánea)?
                                                        // campaign save (or its snapshot)?
        public bool Instantanea { get; private set; }   // ¿instantánea del nodo («snapShot»: true)?
                                                        // node snapshot («snapShot»: true)?
        public string Firma { get; private set; }       // runId o «semilla:N» (null fuera de campaña)
                                                        // runId or «seed:N» (null outside a campaign)
        public string Slot { get; private set; }        // principal, diaria o coop (null fuera de campaña)
                                                        // main, daily or co-op (null outside a campaign)
        public bool EsPrincipal => Slot == SlotPrincipal;

        /// <summary>Contexto de un texto (nunca null).</summary>
        /// Context of a text (never null).
        public static ContextoSave De(string json)
        {
            var c = new ContextoSave { Instantanea = !string.IsNullOrEmpty(json) && SaneadoSaves.EsInstantanea(json) };
            if (!SaneadoSaves.EsPartida(json)) return c;
            c.Partida = true;
            c.Firma = SaneadoSaves.FirmaPartida(json);
            c.Slot = ReDiaria.IsMatch(json) ? SlotDiaria : ReCoop.IsMatch(json) ? SlotCoop : SlotPrincipal;
            return c;
        }

        /// <summary>Ranura del registro: en campaña «base[_instantanea]@slot@firma»; fuera, «base[_instantanea]».</summary>
        /// Registry slot: in a campaign «base[_instantanea]@slot@firma»; outside, «base[_instantanea]».
        public string Ranura(string baseRanura) =>
            Partida ? Legado(baseRanura) + "@" + Slot + "@" + (Firma ?? SinFirma) : Legado(baseRanura);

        /// <summary>Ranura de antes de la 0.7.5 (sin partida ni slot): solo se lee para no perder lo apuntado por ella.</summary>
        /// Slot from before 0.7.5 (no run or slot): only read so that what it recorded is not lost.
        public string Legado(string baseRanura) => Instantanea ? baseRanura + "_instantanea" : baseRanura;

        /// <summary>¿Tiene la ranura nueva una vieja distinta que mirar? (solo en campaña)</summary>
        /// Does the new slot have a different old one to look at? (campaign only)
        public bool ConLegado => Partida;
    }

    // -----------------------------------------------------------------------
    // Sección: transformaciones de saneado (ida al disco y vuelta)
    // Section: sanitizing transformations (to disk and back)
    // -----------------------------------------------------------------------
    public static class SaneadoSaves
    {
        // Claves de listas de colección (List<UnitName>) que se guardan en playerSaveData.json
        // Collection list keys (List<UnitName>) that are saved in playerSaveData.json
        private static readonly string[] ListasColeccion = { "troopsRecruited", "troopsAcknowledged" };

        // Clave del array fijo de ofertas de reclutamiento (UnitName[]) de campaignSaveData.json
        // Key of the fixed array of recruitment offers (UnitName[]) in campaignSaveData.json
        private const string ClaveOfertas = "recruitableUnits";

        // 0.7.5 (revisión total del 02-11): arrays de unidades sin GUID del save de campaña que se tratan como las ofertas
        // (unidad nueva -> base y se apunta posición e id; al leer vuelve si la posición sigue con la base). El primero es el
        // de siempre; los otros tres los trajo el juego 1.11.1 (volcado del interop, notas internas (no publicadas)):
        // la oferta de recompensa tras una batalla, los mercenarios que quedan por contratar y el ejército inicial del
        // cooperativo. Clave del save -> nombre base de su ranura en el registro
        // 0.7.5 (full review of 02-11): arrays of units without a GUID in the campaign save that are treated like the offers
        // (new unit -> base, and the position and id are recorded; on read it comes back if the position still holds the base). The first one is the
        // usual one; the other three were introduced by game 1.11.1 (interop dump, internal (unpublished) notes):
        // the reward offer after a battle, the mercenaries left to hire and the starting army of the
        // co-op. Save key -> base name of its slot in the registry
        internal static readonly (string clave, string ranura)[] ArraysConPosicion =
        {
            (ClaveOfertas, "ofertas"), ("battleRecruitOffer", "ofertasBatalla"), ("mercenariesLeft", "mercenarios"), ("coopStartArmy", "ejercitoCoop"),
        };

        // Arrays de unidades sin GUID donde la unidad nueva se cambia por su base sin poder recuperarla al leer
        // Arrays of units without a GUID where the new unit is swapped for its base with no way to recover it on read
        private static readonly string[] ArraysSinRecuperacion = { "SquadsInArmy" };

        // "UnitName": <número> seguido (con cualquier sangría) de su "UniqueID": "<guid>"
        // "UnitName": <número> followed (with any indentation) by its "UniqueID": "<guid>"
        private static readonly Regex ReEscuadra = new Regex(
            "(\"UnitName\"\\s*:\\s*)(\\d+)(\\s*,\\s*\"UniqueID\"\\s*:\\s*\")([^\"]*)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Campos sueltos de tipo UnitName (historial de partidas, etc.). Las bajas históricas ya no pasan por aquí desde la
        // revisión del 18-10 (M-1): BajasParaDisco las quita antes
        // Loose fields of type UnitName (run history, etc.). Historical kills no longer go through here since the
        // review of 18-10 (M-1): BajasParaDisco removes them beforehand
        private static readonly Regex ReCampoSuelto = new Regex(
            "(\"(?:UnitName|unit|Unit)\"\\s*:\\s*)(\\d+)(?=\\s*[,}\\]\\r\\n])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Marca de instantánea del nodo en campaignSaveDataSnapshot.json
        // Snapshot marker of the node in campaignSaveDataSnapshot.json
        private static readonly Regex ReInstantanea = new Regex("\"snapShot\"\\s*:\\s*true", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        internal static bool EsInstantanea(string json) => json.IndexOf("\"snapShot\"", StringComparison.Ordinal) >= 0 && ReInstantanea.IsMatch(json);

        /// <summary>Aviso hacia el log cuando un GUID apuntado no se puede restaurar (null en pruebas = sin aviso).</summary>
        /// Warning to the log when a recorded GUID cannot be restored (null in tests = no warning).
        public static Action<string> AvisarNoRestaurable;
        private static readonly HashSet<string> _avisados = new HashSet<string>();

        // Avisa (una vez por GUID y sesión) de que una escuadra apuntada se queda como su base al leer
        // Warns (once per GUID and session) that a recorded squad stays as its base on read
        private static void AvisoNoRestaurable(TablaUnidades tabla, string guid, int id, int numero)
        {
            if (AvisarNoRestaurable == null) return;
            lock (_avisados) { if (!_avisados.Add(guid)) return; }
            string motivo = !tabla.Contiene(id) ? "la unidad ya no está en el manifiesto"
                          : tabla.BaseDe(id) != numero ? $"en el save figura la unidad {numero} y su base es la {tabla.BaseDe(id)}"
                          : "la unidad no está ahora en el catálogo (¿mod desactivado?)";
            AvisarNoRestaurable($"La escuadra {guid} está apuntada como unidad {id}, pero no se restaura: {motivo}. Se queda como la unidad {numero} del save; la entrada del registro se conserva.");
        }

        // Array JSON de una clave concreta: "clave": [ ... ]
        // JSON array of a specific key: "key": [ ... ]
        private static Regex ReArray(string clave) => new Regex(
            "(\"" + Regex.Escape(clave) + "\"\\s*:\\s*\\[)([^\\]]*)(\\])",
            RegexOptions.CultureInvariant);

        // Cache de las expresiones de arrays (se crean una vez)
        // Cache of the array expressions (created once)
        private static readonly Dictionary<string, Regex> CacheArrays = new Dictionary<string, Regex>();
        internal static Regex Array(string clave)
        {
            lock (CacheArrays)
            {
                if (!CacheArrays.TryGetValue(clave, out var re)) { re = ReArray(clave); CacheArrays[clave] = re; }
                return re;
            }
        }

        // ---------------------------------------------------------------
        // Ida al disco: quita toda unidad nueva del texto y apunta lo necesario
        // Trip to disk: removes every new unit from the text and records what is needed
        // ---------------------------------------------------------------
        /// <summary>Devuelve el JSON sin ninguna unidad nueva. Devuelve el mismo texto si no hay nada que cambiar.</summary>
        /// Returns the JSON without any new unit. Returns the same text if there is nothing to change.
        public static string ParaDisco(string json, TablaUnidades tabla, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || tabla == null) return json;
            // 0.7.2 (revisión): un texto que ya salió saneado por esta etapa se devuelve tal cual (segunda pasada del mismo
            // texto: sin esto se olvidarían las ofertas apuntadas)
            // 0.7.2 (review): a text that already came out sanitized by this stage is returned as is (second pass over the same
            // text: without this, the recorded offers would be forgotten)
            int previas = registro.EtapasDe(json);
            if ((previas & RegistroGuardias.EtapaUnidades) != 0) return json;
            // 0.7.5: partida, slot e instantánea del texto (las ofertas se apuntan solo en la ranura de esa partida)
            // 0.7.5: run, slot and snapshot of the text (offers are recorded only in the slot of that run)
            var ctx = ContextoSave.De(json);
            // Atajo barato: sin ningún id nuevo en el texto no hay nada que cambiar. Si el texto trae ofertas (o los otros
            // arrays con posición) sin unidades nuevas, se olvidan las posiciones apuntadas de ESA ranura (0.7.5: solo de
            // esa partida y slot; antes, de cualquier partida)
            // Cheap shortcut: with no new id in the text there is nothing to change. If the text contains offers (or the other
            // arrays with position) without new units, the recorded positions of THAT slot are forgotten (0.7.5: only of
            // that run and slot; before, of any run)
            if (!tabla.ReIds.IsMatch(json))
            {
                bool alguno = false;
                registro.IniciarLote();
                try
                {
                    foreach (var (clave, baseRanura) in ArraysConPosicion)
                    {
                        if (json.IndexOf("\"" + clave + "\"", StringComparison.Ordinal) < 0) continue;
                        FijarPosiciones(registro, ctx, clave, baseRanura, new Dictionary<int, int>());
                        alguno = true;
                    }
                }
                finally { registro.TerminarLote(); }
                if (alguno) registro.RecordarSalida(json, previas | RegistroGuardias.EtapaUnidades);
                return json;
            }
            // Todo el saneado de este texto es un lote: el registro se escribe una sola vez al final
            // All the sanitizing of this text is one batch: the registry is written only once at the end
            registro.IniciarLote();
            try
            {
                string res = ParaDiscoLote(json, tabla, registro, ctx);
                registro.RecordarSalida(res, previas | RegistroGuardias.EtapaUnidades);
                return res;
            }
            finally { registro.TerminarLote(); }
        }

        // Cuerpo de ParaDisco (dentro del lote del registro)
        // Body of ParaDisco (inside the registry batch)
        private static string ParaDiscoLote(string json, TablaUnidades tabla, RegistroGuardias registro, ContextoSave ctx)
        {
            string res = json;

            // 1) Escuadras con GUID: se guardan como su base y el GUID va al registro con el id real
            // 1) Squads with a GUID: they are saved as their base and the GUID goes to the registry with the real id
            res = ReEscuadra.Replace(res, m =>
            {
                if (!int.TryParse(m.Groups[2].Value, out int id) || !tabla.Contiene(id)) return m.Value;
                string guid = m.Groups[4].Value;
                if (!string.IsNullOrEmpty(guid)) registro.ApuntarGuid(guid, id);
                return m.Groups[1].Value + tabla.BaseDe(id) + m.Groups[3].Value + guid + "\"";
            });

            // 1b) Bajas históricas (revisión del 18-10, M-1): ANTES de los campos sueltos, que cambiarían el 9xx por su base
            // 1b) Historical kills (review of 18-10, M-1): BEFORE the loose fields, which would swap the 9xx for its base
            res = BajasParaDisco(res, tabla, registro);

            // 2) Campos sueltos sin GUID: se cambian por su base (no se pueden recuperar al leer)
            // 2) Loose fields without a GUID: they are swapped for their base (they cannot be recovered on read)
            res = ReCampoSuelto.Replace(res, m =>
                int.TryParse(m.Groups[2].Value, out int id) && tabla.Contiene(id) ? m.Groups[1].Value + tabla.BaseDe(id) : m.Value);

            // 3) Ofertas de reclutamiento y los demás arrays con posición (0.7.5: oferta tras batalla, mercenarios y ejército
            //    inicial del cooperativo): unidad nueva -> base, recordando posición e id en la ranura de esa partida
            // 3) Recruitment offers and the other arrays with position (0.7.5: offer after battle, mercenaries and starting
            //    army of the co-op): new unit -> base, remembering position and id in the slot of that run
            foreach (var (clave, baseRanura) in ArraysConPosicion)
            {
                if (res.IndexOf("\"" + clave + "\"", StringComparison.Ordinal) < 0) continue;
                res = Array(clave).Replace(res, m =>
                {
                    var elementos = PartirArray(m.Groups[2].Value);
                    var apuntadas = new Dictionary<int, int>();
                    for (int i = 0; i < elementos.Count; i++)
                    {
                        if (int.TryParse(elementos[i], out int id) && tabla.Contiene(id)) { elementos[i] = tabla.BaseDe(id).ToString(); apuntadas[i] = id; }
                    }
                    FijarPosiciones(registro, ctx, clave, baseRanura, apuntadas);
                    return apuntadas.Count == 0 ? m.Value : m.Groups[1].Value + UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value;
                });
            }

            // 3b) Otros arrays de unidades sin GUID (ArmySaveData.SquadsInArmy): unidad nueva -> base, sin recuperación
            // 3b) Other arrays of units without a GUID (ArmySaveData.SquadsInArmy): new unit -> base, no recovery
            foreach (string clave in ArraysSinRecuperacion)
            {
                res = Array(clave).Replace(res, m =>
                {
                    var elementos = PartirArray(m.Groups[2].Value);
                    bool cambiado = false;
                    for (int i = 0; i < elementos.Count; i++)
                        if (int.TryParse(elementos[i], out int id) && tabla.Contiene(id)) { elementos[i] = tabla.BaseDe(id).ToString(); cambiado = true; }
                    return cambiado ? m.Groups[1].Value + UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value : m.Value;
                });
            }

            // 4) Listas de colección: se quitan las unidades nuevas y se anota cuáles estaban
            // 4) Collection lists: the new units are removed and which ones were present is noted
            foreach (string clave in ListasColeccion)
            {
                res = Array(clave).Replace(res, m =>
                {
                    var elementos = PartirArray(m.Groups[2].Value);
                    var quitados = elementos.Where(tabla.Contiene).Select(int.Parse).ToList();
                    if (quitados.Count == 0) return m.Value;
                    foreach (int id in quitados) registro.MarcarLista(clave, id);
                    elementos.RemoveAll(tabla.Contiene);
                    return m.Groups[1].Value + UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value;
                });
            }
            return res;
        }

        // ---------------------------------------------------------------
        // Vuelta del disco: devuelve las unidades nuevas apuntadas en el registro
        // Trip back from disk: restores the new units recorded in the registry
        // ---------------------------------------------------------------
        /// <summary>Devuelve el JSON con las unidades nuevas restauradas según el registro. Mismo texto si no hay nada que cambiar.</summary>
        /// Returns the JSON with the new units restored according to the registry. Same text if there is nothing to change.
        public static string DesdeDisco(string json, TablaUnidades tabla, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || tabla == null || registro.EstaVacio) return json;
            string res = json;

            // 1) Escuadras: una base con un GUID apuntado vuelve a ser su unidad nueva (si existe y la base cuadra)
            // 1) Squads: a base with a recorded GUID becomes its new unit again (if it exists and the base matches)
            if (registro.NumeroGuids > 0)
            {
                res = ReEscuadra.Replace(res, m =>
                {
                    string guid = m.Groups[4].Value;
                    if (guid.Length == 0 || !int.TryParse(m.Groups[2].Value, out int numero)) return m.Value;
                    int id = registro.IdDeGuid(guid);
                    // Sin apuntar, o ya es la unidad nueva (el prefix de lectura corre dos veces: FromJson llama a FromJsonInternal)
                    // Not recorded, or already the new unit (the read prefix runs twice: FromJson calls FromJsonInternal)
                    if (id < 0 || id == numero) return m.Value;
                    if (tabla.Restaurable(id, numero)) return m.Groups[1].Value + id + m.Groups[3].Value + guid + "\"";
                    AvisoNoRestaurable(tabla, guid, id, numero);
                    return m.Value;
                });
            }

            // 2) Ofertas de reclutamiento (y los demás arrays con posición, 0.7.5): posiciones apuntadas que sigan siendo su
            //    base vuelven a ser la unidad nueva (solo lo apuntado de esta partida y slot)
            // 2) Recruitment offers (and the other arrays with position, 0.7.5): recorded positions that are still their
            //    base become the new unit again (only what was recorded for this run and slot)
            var ctx = ContextoSave.De(json);
            foreach (var (clave, baseRanura) in ArraysConPosicion)
            {
                if (res.IndexOf("\"" + clave + "\"", StringComparison.Ordinal) < 0) continue;
                var ofertas = LeerPosiciones(registro, ctx, clave, baseRanura);
                if (ofertas.Count == 0) continue;
                res = Array(clave).Replace(res, m =>
                {
                    var elementos = PartirArray(m.Groups[2].Value);
                    bool cambiado = false;
                    foreach (var kv in ofertas)
                    {
                        if (kv.Key < elementos.Count && int.TryParse(elementos[kv.Key], out int numero) && tabla.Restaurable(kv.Value, numero))
                        { elementos[kv.Key] = kv.Value.ToString(); cambiado = true; }
                    }
                    return cambiado ? m.Groups[1].Value + UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value : m.Value;
                });
            }

            // 3) Listas de colección marcadas: se vuelven a añadir las unidades nuevas que falten (y existan)
            // 3) Marked collection lists: the missing new units are added back (if they exist)
            if (registro.TieneListasMarcadas)
            {
                foreach (string clave in ListasColeccion)
                {
                    var ids = registro.IdsDeLista(clave).Where(id => tabla.Contiene(id) && (tabla.Disponible == null || tabla.Disponible(id))).ToList();
                    if (ids.Count == 0) continue;
                    res = Array(clave).Replace(res, m =>
                    {
                        var elementos = PartirArray(m.Groups[2].Value);
                        bool cambiado = false;
                        foreach (int id in ids)
                            if (!elementos.Contains(id.ToString())) { elementos.Add(id.ToString()); cambiado = true; }
                        return cambiado ? m.Groups[1].Value + UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value : m.Value;
                    });
                }
            }

            // 4) Bajas históricas apuntadas (revisión del 18-10, M-1)
            // 4) Recorded historical kills (review of 18-10, M-1)
            res = BajasDesdeDisco(res, tabla, registro);
            return res;
        }

        // ---------------------------------------------------------------
        // Posiciones apuntadas de los arrays con posición (0.7.5)
        // Recorded positions of the arrays with position (0.7.5)
        // ---------------------------------------------------------------
        // Apunta (o, vacío, olvida) las posiciones de un array en la ranura de esta partida y slot. Las ofertas de un texto
        // que no es de campaña siguen en las ranuras de siempre (registro.Ofertas(bool)). Al guardar la partida principal se
        // olvidan las ofertas viejas sin partida (de antes de la 0.7.5): esta escritura ya las ha pasado a su ranura nueva
        // Records (or, if empty, forgets) the positions of an array in the slot of this run and slot. The offers of a text
        // that is not a campaign one stay in the usual slots (registro.Ofertas(bool)). When saving the main run, the
        // old offers without a run (from before 0.7.5) are forgotten: this write has already moved them to their new slot
        private static void FijarPosiciones(RegistroGuardias registro, ContextoSave ctx, string clave, string baseRanura, Dictionary<int, int> apuntadas)
        {
            bool ofertas = clave == ClaveOfertas;
            if (ofertas && !ctx.Partida) { registro.FijarOfertas(apuntadas, ctx.Instantanea); return; }
            registro.FijarOfertas(ctx.Ranura(baseRanura), apuntadas);
            if (ofertas && ctx.EsPrincipal) registro.FijarOfertas(new Dictionary<int, int>(), ctx.Instantanea);
        }

        // Posiciones apuntadas de un array para este texto. Las ofertas viejas sin partida (de antes de la 0.7.5) solo valen
        // para la partida principal, que es la única que existía entonces
        // Recorded positions of an array for this text. The old offers without a run (from before 0.7.5) are only valid
        // for the main run, which is the only one that existed then
        private static Dictionary<int, int> LeerPosiciones(RegistroGuardias registro, ContextoSave ctx, string clave, string baseRanura)
        {
            bool ofertas = clave == ClaveOfertas;
            if (ofertas && !ctx.Partida) return registro.Ofertas(ctx.Instantanea);
            var r = registro.Ofertas(ctx.Ranura(baseRanura));
            if (r.Count == 0 && ofertas && ctx.EsPrincipal) r = registro.Ofertas(ctx.Instantanea);
            return r;
        }

        // ---------------------------------------------------------------
        // Bajas históricas de las unidades nuevas (revisión del 18-10, M-1): UnitNameHistoricalKillStore del save del
        // jugador. El juego busca la primera entrada de cada unidad y suma ahí; si la de la 900 se guardara como su base,
        // al recargar la 900 tendría 0 bajas y en cada sesión aparecería otra entrada duplicada de la base. Se tratan como
        // las victorias de los héroes nuevos: fuera del save y al registro al escribir; de vuelta al leer.
        // Historical kills of the new units (review of 18-10, M-1): UnitNameHistoricalKillStore in the player's save.
        // The game looks for the first entry of each unit and adds there; if the one for 900 were saved as its base,
        // on reloading 900 would have 0 kills and in every session another duplicated entry of the base would appear. They are treated like
        // the victories of the new heroes: out of the save and into the registry on write; back on read.
        // ---------------------------------------------------------------
        internal const string ClaveBajas = "UnitNameHistoricalKillStore";
        private static readonly Regex ReUnidadEntrada = new Regex(@"""UnitName""\s*:\s*(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Unidad de una entrada (objeto JSON) o -1
        // Unit of an entry (JSON object) or -1
        private static int UnidadDeEntrada(string objeto)
        {
            var m = ReUnidadEntrada.Match(objeto);
            return m.Success && int.TryParse(m.Groups[1].Value, out int u) ? u : -1;
        }

        // Ida al disco: quita las entradas de unidades nuevas y las apunta enteras (dentro del lote de ParaDisco)
        // Write to disk: removes the new units' entries and records them whole (inside the ParaDisco batch)
        private static string BajasParaDisco(string json, TablaUnidades tabla, RegistroGuardias registro)
        {
            if (json.IndexOf("\"" + ClaveBajas + "\"", StringComparison.Ordinal) < 0
                || !PartirArrayObjetos(json, ClaveBajas, out int abre, out int cierra, out var objetos)) return json;
            var quedan = new List<string>();
            bool quitada = false;
            foreach (var (ini, fin) in objetos)
            {
                string objeto = json.Substring(ini, fin - ini);
                int u = UnidadDeEntrada(objeto);
                if (u >= 0 && tabla.Contiene(u)) { registro.FijarEntradaUnidad(ClaveBajas, u, objeto); quitada = true; }
                else quedan.Add(objeto);
            }
            if (!quitada) return json;
            return json.Substring(0, abre) + UnirObjetos(json, ClaveBajas, abre, cierra, objetos, quedan) + json.Substring(cierra + 1);
        }

        // Vuelta del disco: añade al final las entradas apuntadas de las unidades disponibles que falten. Si ya hay una de
        // esa unidad (el prefix de lectura corre dos veces) no se añade otra: nunca se duplica
        // Read from disk: appends the recorded entries of the available units that are missing. If there is already one for
        // that unit (the read prefix runs twice) another is not added: it is never duplicated
        private static string BajasDesdeDisco(string json, TablaUnidades tabla, RegistroGuardias registro)
        {
            if (json.IndexOf("\"" + ClaveBajas + "\"", StringComparison.Ordinal) < 0) return json;
            var apuntadas = registro.EntradasUnidades(ClaveBajas);
            if (apuntadas.Count == 0 || !PartirArrayObjetos(json, ClaveBajas, out int abre, out int cierra, out var objetos)) return json;
            var presentes = new HashSet<int>();
            var todos = new List<string>();
            foreach (var (ini, fin) in objetos)
            {
                string objeto = json.Substring(ini, fin - ini);
                presentes.Add(UnidadDeEntrada(objeto));
                todos.Add(objeto);
            }
            bool anadida = false;
            foreach (var kv in apuntadas.OrderBy(x => x.Key))
                if (tabla.Contiene(kv.Key) && (tabla.Disponible == null || tabla.Disponible(kv.Key)) && !presentes.Contains(kv.Key))
                { todos.Add(kv.Value); anadida = true; }
            if (!anadida) return json;
            return json.Substring(0, abre) + UnirObjetos(json, ClaveBajas, abre, cierra, objetos, todos) + json.Substring(cierra + 1);
        }

        /// <summary>
        /// ¿Trae el texto alguna escuadra («UnitName» + «UniqueID») cuyo GUID esté apuntado en el registro? (revisión del
        /// 18-10, M-2: así se reconoce una partida con unidades 9xx empezada antes de que el registro marcara partidas).
        /// Does the text contain any squad («UnitName» + «UniqueID») whose GUID is recorded in the core registry? (review of
        /// 18-10, M-2: this is how a run with 9xx units started before the registry marked runs is recognized).
        /// </summary>
        public static bool TieneGuidApuntado(string json, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || registro == null || registro.NumeroGuids == 0) return false;
            foreach (Match m in ReEscuadra.Matches(json))
                if (m.Groups[4].Length > 0 && registro.ContieneGuid(m.Groups[4].Value)) return true;
            return false;
        }

        // ---------------------------------------------------------------
        // Razas nuevas (F2): ciudad actual, campo de batalla preparado y temas de taberna
        // New races (F2): current town, prepared battlefield and tavern themes
        // ---------------------------------------------------------------
        // townData (TownSaveData): "townName", "townSize" y "townRace" van seguidos en el orden de declaración
        // townData (TownSaveData): "townName", "townSize" and "townRace" come one after another in declaration order
        private static readonly Regex ReCiudad = new Regex(
            @"(""townName""\s*:\s*""((?:[^""\\]|\\.)*)""\s*,\s*""townSize""\s*:\s*(-?\d+)\s*,\s*""townRace""\s*:\s*)(-?\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        // battleFieldPreset: "mapRegion" (objeto), "race" y, más adelante en el mismo objeto, "seed"
        // battleFieldPreset: "mapRegion" (object), "race" and, later in the same object, "seed"
        private static readonly Regex ReCampo = new Regex(
            @"(""battleFieldPreset""\s*:\s*\{\s*""mapRegion""\s*:\s*\{[^{}]*\}\s*,\s*""race""\s*:\s*)(-?\d+)([^{}]*?""seed""\s*:\s*(-?\d+))",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        // Tema de taberna elegido (playerSaveData)
        // Chosen tavern theme (playerSaveData)
        private static readonly Regex ReTemaActivo = new Regex(@"(""activeTavernThemeRace""\s*:\s*)(-?\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private const string ClaveTemas = "unlockedTavernThemes";

        // Raza apuntada para este texto: la de la ranura de esta partida y slot (0.7.5) o, si no hay, la de la ranura vieja
        // sin partida (antes de la 0.7.5), que lleva su propia firma (ciudad y tamaño o semilla del campo) y se comprueba igual
        // Race recorded for this text: the one in the slot of this run and slot (0.7.5) or, if none, the one in the old slot
        // without a run (before 0.7.5), which carries its own signature (town and size or field seed) and is checked the same way
        private static bool RazaApuntada(RegistroGuardias registro, ContextoSave ctx, string objeto, out int raza, out string firma)
        {
            if (registro.RazaDe(ctx.Ranura(objeto), out raza, out firma)) return true;
            // Ronda 2 de la revisión: la ranura vieja solo para la partida principal (la única que existía antes de la 0.7.5)
            // Review round 2: the old slot only for the main run (the only one that existed before 0.7.5)
            return ctx.ConLegado && ctx.EsPrincipal && registro.RazaDe(ctx.Legado(objeto), out raza, out firma);
        }

        // Apunta (raza >= 0) u olvida (raza < 0) la raza de una ranura de esta partida; si la ranura vieja sin partida tenía
        // la raza de este mismo objeto (misma firma), se olvida: lo que valga ya está en la nueva
        // Records (race >= 0) or forgets (race < 0) the race of a slot of this run; if the old slot without a run had
        // the race of this same object (same signature), it is forgotten: whatever is valid is already in the new one
        private static void FijarRazaPartida(RegistroGuardias registro, ContextoSave ctx, string objeto, int raza, string firma)
        {
            registro.FijarRaza(ctx.Ranura(objeto), raza, firma);
            if (ctx.ConLegado && registro.RazaDe(ctx.Legado(objeto), out _, out string fv) && fv == firma)
                registro.FijarRaza(ctx.Legado(objeto), -1, null);
        }

        /// <summary>
        /// Ida al disco de las razas nuevas: ciudad y campo de batalla con raza 40/41 se guardan con su sustituta y la
        /// real se apunta (con firma); los temas de taberna 40/41 se quitan. Mismo texto si no hay nada que cambiar.
        /// Write to disk for the new races: town and battlefield with race 40/41 are saved with their replacement and the
        /// real one is recorded (with signature); tavern themes 40/41 are removed. Same text if there is nothing to change.
        /// </summary>
        public static string RazasParaDisco(string json, TablaRazas razas, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || razas == null || razas.Numero == 0 || registro == null) return json;
            bool hayCiudad = json.IndexOf("\"townRace\"", StringComparison.Ordinal) >= 0;
            bool hayCampo = json.IndexOf("\"battleFieldPreset\"", StringComparison.Ordinal) >= 0;
            bool hayTemas = json.IndexOf("TavernTheme", StringComparison.Ordinal) >= 0;
            if (!hayCiudad && !hayCampo && !hayTemas) return json;
            // 0.7.2 (revisión): un texto que ya salió saneado por esta etapa se devuelve tal cual (en la segunda pasada la
            // ciudad y el campo traen ya la sustituta y se olvidaría la raza nueva apuntada)
            // 0.7.2 (review): a text that already came out sanitized by this stage is returned as is (on the second pass the
            // town and the field already carry the replacement and the recorded new race would be forgotten)
            int previas = registro.EtapasDe(json);
            if ((previas & RegistroGuardias.EtapaRazas) != 0) return json;
            var ctx = ContextoSave.De(json);
            string res = json;
            registro.IniciarLote();
            try
            {
                if (hayCiudad)
                    res = ReCiudad.Replace(res, m =>
                    {
                        if (!int.TryParse(m.Groups[4].Value, out int raza)) return m.Value;
                        string firma = m.Groups[2].Value + "|" + m.Groups[3].Value;
                        if (!razas.EsNueva(raza)) { FijarRazaPartida(registro, ctx, "ciudad", -1, firma); return m.Value; }
                        FijarRazaPartida(registro, ctx, "ciudad", raza, firma);
                        return m.Groups[1].Value + razas.SustitutaDe(raza);
                    });
                if (hayCampo)
                    res = ReCampo.Replace(res, m =>
                    {
                        if (!int.TryParse(m.Groups[2].Value, out int raza)) return m.Value;
                        string firma = m.Groups[4].Value;
                        if (!razas.EsNueva(raza)) { FijarRazaPartida(registro, ctx, "campo", -1, firma); return m.Value; }
                        FijarRazaPartida(registro, ctx, "campo", raza, firma);
                        return m.Groups[1].Value + razas.SustitutaDe(raza) + m.Groups[3].Value;
                    });
                if (hayTemas)
                {
                    // Temas desbloqueados: fuera las razas nuevas (no tienen tema propio)
                    // Unlocked themes: new races are removed (they have no theme of their own)
                    res = Array(ClaveTemas).Replace(res, m =>
                    {
                        var elementos = PartirArray(m.Groups[2].Value);
                        int antes = elementos.Count;
                        elementos.RemoveAll(e => int.TryParse(e, out int r) && razas.EsNueva(r));
                        return elementos.Count == antes ? m.Value : m.Groups[1].Value + UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value;
                    });
                    // Tema elegido: si fuera una raza nueva, el de respaldo (Special, el tema por defecto)
                    // Chosen theme: if it were a new race, the fallback one (Special, the default theme)
                    res = ReTemaActivo.Replace(res, m =>
                        int.TryParse(m.Groups[2].Value, out int r) && razas.EsNueva(r) ? m.Groups[1].Value + razas.TemaRespaldo : m.Value);
                }
                registro.RecordarSalida(res, previas | RegistroGuardias.EtapaRazas);
            }
            finally { registro.TerminarLote(); }
            return res;
        }

        /// <summary>
        /// Vuelta del disco de las razas nuevas: la ciudad o el campo que figuren con la sustituta y la misma firma que
        /// la apuntada vuelven a su raza nueva. Mismo texto si no hay nada que cambiar.
        /// Read from disk for the new races: the town or field that appear with the replacement and the same signature as
        /// the recorded one go back to their new race. Same text if there is nothing to change.
        /// </summary>
        public static string RazasDesdeDisco(string json, TablaRazas razas, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || razas == null || razas.Numero == 0 || registro == null || !registro.TieneRazas) return json;
            var ctx = ContextoSave.De(json);
            string res = json;
            if (json.IndexOf("\"townRace\"", StringComparison.Ordinal) >= 0
                && RazaApuntada(registro, ctx, "ciudad", out int rc, out string fc))
                res = ReCiudad.Replace(res, m =>
                    int.TryParse(m.Groups[4].Value, out int v) && razas.EsNueva(rc) && v == razas.SustitutaDe(rc)
                    && fc == m.Groups[2].Value + "|" + m.Groups[3].Value ? m.Groups[1].Value + rc : m.Value);
            if (json.IndexOf("\"battleFieldPreset\"", StringComparison.Ordinal) >= 0
                && RazaApuntada(registro, ctx, "campo", out int rk, out string fk))
                res = ReCampo.Replace(res, m =>
                    int.TryParse(m.Groups[2].Value, out int v) && razas.EsNueva(rk) && v == razas.SustitutaDe(rk)
                    && fk == m.Groups[4].Value ? m.Groups[1].Value + rk + m.Groups[3].Value : m.Value);
            return res;
        }

        /// <summary>
        /// Razas nuevas apuntadas en el registro para las ranuras que trae este texto (ciudad, campo) que NO se han podido
        /// restaurar (la tabla de razas no está, la raza ya no es nueva o la firma no cuadra). El registro las conserva para
        /// una lectura posterior; esto solo las describe para el log (0.6.1). Vacío si no hay ninguna pendiente.
        /// New races recorded in the core registry for the slots this text carries (town, field) that could NOT be
        /// restored (the races table is missing, the race is no longer new or the signature doesn't match). The registry keeps them for
        /// a later read; this only describes them for the log (0.6.1). Empty if none are pending.
        /// </summary>
        public static List<string> RazasPendientes(string textoLeido, TablaRazas razas, RegistroGuardias registro)
        {
            var r = new List<string>();
            if (string.IsNullOrEmpty(textoLeido) || registro == null || !registro.TieneRazas) return r;
            var ctx = ContextoSave.De(textoLeido);
            void Mirar(string objeto, string clave, Regex re, int grupoRaza, Func<Match, string> firma)
            {
                if (textoLeido.IndexOf(clave, StringComparison.Ordinal) < 0 || !RazaApuntada(registro, ctx, objeto, out int apuntada, out string f)) return;
                foreach (Match m in re.Matches(textoLeido))
                {
                    if (int.TryParse(m.Groups[grupoRaza].Value, out int v) && v == apuntada) return;   // ya restaurada
                                                                                                       // already restored
                    string motivo = razas == null ? "facciones nuevas desactivadas"
                                  : !razas.EsNueva(apuntada) ? "la raza ya no es nueva"
                                  : firma(m) != f ? "la firma no cuadra (otra ciudad o campo)" : "la raza del save no es su sustituta";
                    r.Add($"{objeto} {apuntada} ({motivo})");
                    return;
                }
            }
            Mirar("ciudad", "\"townRace\"", ReCiudad, 4, m => m.Groups[2].Value + "|" + m.Groups[3].Value);
            Mirar("campo", "\"battleFieldPreset\"", ReCampo, 2, m => m.Groups[4].Value);
            return r;
        }

        /// <summary>Cuenta las razas nuevas que quedan donde no deben llegar al disco (debe ser 0 tras RazasParaDisco).</summary>
        /// Counts the new races left where they must not reach the disk (must be 0 after RazasParaDisco).
        public static int ContarResiduosRazas(string json, TablaRazas razas)
        {
            if (string.IsNullOrEmpty(json) || razas == null || razas.Numero == 0) return 0;
            int n = 0;
            foreach (Match m in ReCiudad.Matches(json)) if (int.TryParse(m.Groups[4].Value, out int r) && razas.EsNueva(r)) n++;
            foreach (Match m in ReCampo.Matches(json)) if (int.TryParse(m.Groups[2].Value, out int r) && razas.EsNueva(r)) n++;
            foreach (Match m in ReTemaActivo.Matches(json)) if (int.TryParse(m.Groups[2].Value, out int r) && razas.EsNueva(r)) n++;
            foreach (Match m in Array(ClaveTemas).Matches(json))
                foreach (string e in PartirArray(m.Groups[2].Value)) if (int.TryParse(e, out int r) && razas.EsNueva(r)) n++;
            return n;
        }

        // ---------------------------------------------------------------
        // Partida a la que pertenece un save (0.7.1): ancla de lo apuntado en el registro
        // Run a save belongs to (0.7.1): anchor of what is recorded in the core registry
        // ---------------------------------------------------------------
        private static readonly Regex ReRunId = new Regex(@"""runId""\s*:\s*""([^""]*)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ReSemilla = new Regex(@"""seed""\s*:\s*(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>¿Es el texto un save de campaña (campaignSaveData o su instantánea)? Solo ellos llevan «enemyWarlordHeroID».</summary>
        /// Is the text a campaign save (campaignSaveData or its snapshot)? Only those contain «enemyWarlordHeroID».
        public static bool EsPartida(string json) =>
            !string.IsNullOrEmpty(json) && json.IndexOf("\"enemyWarlordHeroID\"", StringComparison.Ordinal) >= 0;

        /// <summary>
        /// Firma de la partida de un save de campaña: su «runId» (el mismo en el save normal y en la instantánea) o, en las
        /// partidas antiguas sin runId, «semilla:» y la primera semilla del texto (la de la partida). null si no es un save
        /// de campaña o no trae ninguna de las dos.
        /// Run signature of a campaign save: its «runId» (the same in the normal save and in the snapshot) or, in old
        /// runs without runId, «semilla:» and the first seed in the text (the run's one). null if it is not a campaign
        /// save or contains neither.
        /// </summary>
        public static string FirmaPartida(string json)
        {
            if (!EsPartida(json)) return null;
            var m = ReRunId.Match(json);
            if (m.Success && m.Groups[1].Value.Length > 0) return m.Groups[1].Value;
            var s = ReSemilla.Match(json);
            return s.Success ? "semilla:" + s.Groups[1].Value : null;
        }

        // ---------------------------------------------------------------
        // Héroes nuevos (0.7.1): héroe del jugador y señor de la guerra enemigo del save de campaña
        // New heroes (0.7.1): player hero and enemy warlord of the campaign save
        // ---------------------------------------------------------------
        // Campos de héroe del save de campaña y su ranura en el registro (solo la primera aparición de cada uno: en
        // CampaignSaveData son campos de primer nivel y aparecen una vez)
        // Hero fields of the campaign save and their slot in the registry (only the first occurrence of each one: in
        // CampaignSaveData they are top-level fields and appear once)
        private static readonly (string campo, string ranura, Regex re)[] CamposHeroe =
        {
            ("heroID", "heroe", new Regex(@"(""heroID""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
            ("enemyWarlordHeroID", "senor", new Regex(@"(""enemyWarlordHeroID""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        };

        /// <summary>
        /// Ida al disco de los héroes nuevos: si el héroe del jugador o el señor de la guerra del save de campaña es un héroe
        /// nuevo, se escribe su héroe prestado de serie y el real se apunta en el registro con la firma de la partida. Si el
        /// campo trae un héroe de serie, se olvida lo apuntado salvo que sea la misma partida y siga el héroe escrito (así es
        /// idempotente y, con el módulo apagado, el héroe vuelve al activarlo). Límite conocido: si en la misma partida el
        /// siguiente señor de la guerra es justo el héroe prestado del anterior (9xx), al leer vuelve a salir el 9xx en ese
        /// enfrentamiento. Mismo texto si no hay nada que cambiar.
        /// Trip to disk for the new heroes: if the player's hero or the campaign save's warlord is a new
        /// hero, its stock borrowed hero is written and the real one is recorded in the registry with the run signature. If the
        /// field holds a stock hero, whatever was recorded is forgotten unless it is the same run and the written hero is still there (so it is
        /// idempotent and, with the module off, the hero comes back when enabled). Known limitation: if in the same run the
        /// next warlord is exactly the previous one's borrowed hero (9xx), on read the 9xx comes back in that
        /// encounter. Same text if there is nothing to change.
        /// </summary>
        public static string HeroesParaDisco(string json, TablaHeroes tabla, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || registro == null) return json;
            bool hayTabla = tabla != null && tabla.Numero > 0;
            bool partida = EsPartida(json);
            // Sin tabla solo se tratan las victorias 9xx del save del jugador (un save antiguo puede traerlas aunque hoy no
            // haya ningún módulo con héroes); el resto necesita la tabla
            // Without the table only the 9xx victories in the player's save are handled (an old save may contain them even if there is
            // no module with heroes today); the rest needs the table
            if (partida ? !hayTabla : !hayTabla && !TieneListasHeroesJugador(json)) return json;
            if (!partida && json.IndexOf("\"lastHeroID\"", StringComparison.Ordinal) < 0 && !TieneListasHeroesJugador(json)) return json;
            // 0.7.2 (revisión): un texto que ya salió saneado por esta etapa se devuelve tal cual (segunda pasada)
            // 0.7.2 (review): a text that has already been sanitized by this stage is returned as is (second pass)
            int previas = registro.EtapasDe(json);
            if ((previas & RegistroGuardias.EtapaHeroes) != 0) return json;
            registro.IniciarLote();
            try
            {
                // Save del jugador: su «lastHeroID» y las listas por héroe (el historial de partidas va en HistorialParaDisco)
                // Player save: its «lastHeroID» and the per-hero lists (the run history goes in HistorialParaDisco)
                string res = partida ? HeroesPartidaParaDisco(json, tabla, registro)
                                     : EntradasHeroesParaDisco(hayTabla ? UltimoHeroeParaDisco(json, tabla, registro) : json, tabla, registro);
                registro.RecordarSalida(res, previas | RegistroGuardias.EtapaHeroes);
                return res;
            }
            finally { registro.TerminarLote(); }
        }

        // Héroe del jugador y señor de la guerra del save de campaña (cuerpo de HeroesParaDisco, dentro del lote).
        // 0.7.5: en la ranura de ESTA partida y slot (ContextoSave). Lo apuntado en la ranura vieja sin partida (antes de la
        // 0.7.5) se mira si es de esta misma partida (firma) y, como esta escritura ya lo deja en la nueva, se olvida
        // Player hero and warlord of the campaign save (body of HeroesParaDisco, inside the batch).
        // 0.7.5: in the slot of THIS run and slot (ContextoSave). What was recorded in the old slot without a run (before
        // 0.7.5) is checked to see if it belongs to this same run (signature) and, since this write already leaves it in the new one, it is forgotten
        private static string HeroesPartidaParaDisco(string json, TablaHeroes tabla, RegistroGuardias registro)
        {
            var ctx = ContextoSave.De(json);
            string firma = ctx.Firma;
            string res = json;
            foreach (var (_, ranuraBase, re) in CamposHeroe)
            {
                string ranura = ctx.Ranura(ranuraBase);
                // Lo apuntado antes para esta partida: la ranura nueva o, si no hay, la vieja con la misma firma
                // What was recorded earlier for this run: the new slot or, if there is none, the old one with the same signature
                bool hayAntes = HeroeApuntado(registro, ctx, ranuraBase, out int realAntes, out int escAntes, out string fAntes);
                res = re.Replace(res, m =>
                {
                    if (!int.TryParse(m.Groups[2].Value, out int v)) return m.Value;
                    if (tabla.EsNuevo(v))
                    {
                        int escrito = tabla.PrestadoDe(v);
                        registro.FijarHeroe(ranura, v, escrito, firma);
                        return m.Groups[1].Value + escrito;
                    }
                    // Héroe de serie: se conserva lo apuntado si es la misma partida y sigue el héroe escrito (un texto ya
                    // saneado que vuelve a pasar, p. ej. ToJson y luego WriteAllText, o el módulo del real apagado); si
                    // cambió el valor o la partida, se olvida (solo el de ESTA partida: 0.7.5)
                    // Default hero: what was recorded is kept if it is the same run and the written hero still applies (an already
                    // sanitized text passing through again, e.g. ToJson and then WriteAllText, or the real one's module turned off); if
                    // the value or the run changed, it is forgotten (only that of THIS run: 0.7.5)
                    if (hayAntes && fAntes == firma && escAntes == v)
                    {
                        registro.FijarHeroe(ranura, realAntes, escAntes, firma);
                        return m.Value;
                    }
                    registro.FijarHeroe(ranura, -1, 0, null);
                    return m.Value;
                }, 1);
                // La ranura vieja de esta misma partida ya no hace falta (su contenido, si vale, está en la nueva)
                // The old slot of this same run is no longer needed (its content, if valid, is in the new one)
                if (ctx.ConLegado && registro.HeroeDe(ctx.Legado(ranuraBase), out _, out _, out string fv) && fv == firma)
                    registro.FijarHeroe(ctx.Legado(ranuraBase), -1, 0, null);
            }
            return res;
        }

        /// <summary>
        /// 0.7.5: héroe apuntado para el campo <paramref name="ranuraBase"/> («heroe», «senor») de este save de campaña: el de
        /// la ranura de su partida y slot o, si no hay, el de la ranura vieja sin partida (antes de la 0.7.5) si es de la
        /// misma partida (firma). false si no hay ninguno.
        /// 0.7.5: hero recorded for the field <paramref name="ranuraBase"/> («heroe», «senor») of this campaign save: the one in
        /// the slot of its run and slot or, if there is none, the one in the old slot without a run (before 0.7.5) if it belongs to the
        /// same run (signature). false if there is none.
        /// </summary>
        public static bool HeroeApuntado(RegistroGuardias registro, ContextoSave ctx, string ranuraBase, out int real, out int escrito, out string firma)
        {
            if (registro.HeroeDe(ctx.Ranura(ranuraBase), out real, out escrito, out firma)) return true;
            if (ctx.ConLegado && registro.HeroeDe(ctx.Legado(ranuraBase), out real, out escrito, out firma) && firma == ctx.Firma) return true;
            real = -1; escrito = -1; firma = null;
            return false;
        }

        /// <summary>
        /// Vuelta del disco de los héroes nuevos: el héroe escrito con la misma firma de partida vuelve a ser el nuevo si
        /// está activo. Un héroe nuevo que llegue en el disco con su módulo apagado (save de antes de la 0.7.1) se carga como
        /// su prestado y se apunta, para recuperarlo al activar el módulo otra vez. Mismo texto si no hay nada que cambiar.
        /// Return from disk of the new heroes: the hero written with the same run signature becomes the new one again if
        /// it is active. A new hero that arrives from disk with its module turned off (save from before 0.7.1) is loaded as
        /// its borrowed hero and recorded, to recover it when the module is activated again. Same text if there is nothing to change.
        /// </summary>
        public static string HeroesDesdeDisco(string json, TablaHeroes tabla, RegistroGuardias registro)
        {
            if (tabla == null || tabla.Numero == 0 || registro == null || string.IsNullOrEmpty(json)) return json;
            // Save del jugador: su «lastHeroID» y las victorias de los héroes nuevos activos
            // Player save: its «lastHeroID» and the victories of the active new heroes
            if (!EsPartida(json)) return EntradasHeroesDesdeDisco(UltimoHeroeDesdeDisco(json, tabla, registro), tabla, registro);
            // 0.7.5: lo apuntado de ESTA partida y slot (o, de antes de la 0.7.5, la ranura vieja con la misma firma)
            // 0.7.5: what was recorded for THIS run and slot (or, from before 0.7.5, the old slot with the same signature)
            var ctx = ContextoSave.De(json);
            string firma = ctx.Firma;
            string res = json;
            foreach (var (_, ranuraBase, re) in CamposHeroe)
            {
                string ranura = ctx.Ranura(ranuraBase);
                res = re.Replace(res, m =>
                {
                    if (!int.TryParse(m.Groups[2].Value, out int v)) return m.Value;
                    if (HeroeApuntado(registro, ctx, ranuraBase, out int real, out int esc, out string f) && esc == v && f == firma && tabla.Activo(real))
                        return m.Groups[1].Value + real;
                    if (tabla.EsNuevo(v) && !tabla.Activo(v))
                    {
                        int escrito = tabla.PrestadoDe(v);
                        registro.FijarHeroe(ranura, v, escrito, firma);
                        return m.Groups[1].Value + escrito;
                    }
                    return m.Value;
                }, 1);
            }
            return res;
        }

        // ---------------------------------------------------------------
        // Último héroe usado del save del jugador (0.7.2): «lastHeroID», escalar de playerSaveData
        // Last hero used in the player save (0.7.2): «lastHeroID», scalar of playerSaveData
        // ---------------------------------------------------------------
        // Ranura del registro (sin firma de partida: es del jugador, no de una partida) y su expresión
        // Registry slot (without run signature: it belongs to the player, not to a run) and its expression
        internal const string RanuraUltimoHeroe = "ultimoHeroe";
        private static readonly Regex ReUltimoHeroe = new Regex(@"(""lastHeroID""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Ida al disco: un héroe nuevo se escribe como su prestado de serie y el real se apunta. Con un héroe de serie se
        // conserva lo apuntado solo si sigue el héroe escrito Y el real no está activo (módulo apagado: así vuelve al
        // activarlo); con el real activo se olvida, porque el jugador empezó a propósito con otro (también con el prestado).
        // 0.7.2 (revisión): antes se conservaba también con el real activo y el reinicio rápido, que usa lastHeroID para
        // crear la partida, arrancaba con el héroe nuevo aunque el jugador hubiera elegido el prestado. La segunda pasada
        // del mismo texto (ToJson y luego WriteAllText) ya no llega aquí: la corta la memoria de textos saneados.
        // Trip to disk: a new hero is written as its default borrowed hero and the real one is recorded. With a default hero, what
        // was recorded is kept only if the written hero still applies AND the real one is not active (module turned off: that way it comes back when
        // it is activated); with the real one active it is forgotten, because the player deliberately started with another (also with the borrowed one).
        // 0.7.2 (review): before, it was also kept with the real one active and quick restart, which uses lastHeroID to
        // create the run, started with the new hero even if the player had chosen the borrowed one. The second pass
        // of the same text (ToJson and then WriteAllText) no longer gets here: the memory of sanitized texts cuts it off.
        private static string UltimoHeroeParaDisco(string json, TablaHeroes tabla, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || json.IndexOf("\"lastHeroID\"", StringComparison.Ordinal) < 0) return json;
            return ReUltimoHeroe.Replace(json, m =>
            {
                if (!int.TryParse(m.Groups[2].Value, out int v)) return m.Value;
                if (tabla.EsNuevo(v))
                {
                    int escrito = tabla.PrestadoDe(v);
                    registro.FijarHeroe(RanuraUltimoHeroe, v, escrito, null);
                    return m.Groups[1].Value + escrito;
                }
                if (registro.HeroeDe(RanuraUltimoHeroe, out int real, out int esc, out _) && esc == v && !tabla.Activo(real)) return m.Value;
                registro.FijarHeroe(RanuraUltimoHeroe, -1, 0, null);
                return m.Value;
            }, 1);
        }

        // Vuelta del disco: el héroe escrito vuelve a ser el nuevo si está activo; un héroe nuevo en el disco con su
        // módulo apagado (save de antes de la 0.7.2) se carga como su prestado y se apunta para recuperarlo después
        // Return from disk: the written hero becomes the new one again if it is active; a new hero on disk with its
        // module turned off (save from before 0.7.2) is loaded as its borrowed hero and recorded to recover it later
        private static string UltimoHeroeDesdeDisco(string json, TablaHeroes tabla, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || json.IndexOf("\"lastHeroID\"", StringComparison.Ordinal) < 0) return json;
            return ReUltimoHeroe.Replace(json, m =>
            {
                if (!int.TryParse(m.Groups[2].Value, out int v)) return m.Value;
                if (registro.HeroeDe(RanuraUltimoHeroe, out int real, out int esc, out _) && esc == v && tabla.Activo(real))
                    return m.Groups[1].Value + real;
                if (tabla.EsNuevo(v) && !tabla.Activo(v))
                {
                    int escrito = tabla.PrestadoDe(v);
                    registro.FijarHeroe(RanuraUltimoHeroe, v, escrito, null);
                    return m.Groups[1].Value + escrito;
                }
                return m.Value;
            }, 1);
        }

        // ---------------------------------------------------------------
        // Victorias de los héroes nuevos en el save del jugador (0.7.2, revisión): HeroDifficultiesCompleted y
        // HeroLastDifficulties. Sin el plugin (o sin módulos, o con Logros.Filtrar = false) el juego contaría esas entradas
        // 9xx para «Roster Complete» y «Max Difficulty All Heroes». Al escribir se quitan del save y se apuntan en el
        // registro (objeto JSON tal cual, solo crece); al leer vuelven las de los héroes activos.
        // Victories of the new heroes in the player save (0.7.2, review): HeroDifficultiesCompleted and
        // HeroLastDifficulties. Without the plugin (or without modules, or with Logros.Filtrar = false) the game would count those
        // 9xx entries for «Roster Complete» and «Max Difficulty All Heroes». When writing, they are removed from the save and recorded in the
        // registry (JSON object as is, it only grows); when reading, those of the active heroes come back.
        // ---------------------------------------------------------------
        // Listas de objetos {"HeroID": n, ...} del save del jugador
        // 0.7.5 (revisión total del 02-11): también las dos listas por héroe que trajo el juego 1.11.1: «heroLooks» (aspecto
        // elegido, {"HeroID", "Rank"}) y «heroes» de «lifetime» (partidas y victorias por héroe, {"heroID", "runs", "wins"}).
        // La entrada 9xx se APARTA entera (no se fusiona con la del héroe prestado: el juego coge la primera entrada de cada
        // héroe y la sumaría a la de serie) y vuelve al leer si el héroe está activo. «heroes» solo aparece en «lifetime»
        // (comprobado en el save real del 02-11)
        // Lists of {"HeroID": n, ...} objects of the player save
        // 0.7.5 (full review of 02-11): also the two per-hero lists that game 1.11.1 brought: «heroLooks» (chosen
        // look, {"HeroID", "Rank"}) and «heroes» of «lifetime» (runs and victories per hero, {"heroID", "runs", "wins"}).
        // The 9xx entry is SET ASIDE whole (not merged with the borrowed hero's: the game takes the first entry of each
        // hero and would add it to the default one) and comes back on read if the hero is active. «heroes» only appears in «lifetime»
        // (verified in the real save of 02-11)
        internal static readonly string[] ListasHeroesJugador = { "HeroDifficultiesCompleted", "HeroLastDifficulties", "heroLooks", "heroes" };
        // Primer id de héroe nuevo: los de serie son menores (los de los módulos van de 901 en adelante)
        // First new hero id: the default ones are lower (those of the modules start at 901 and go up)
        internal const int PrimerHeroeNuevo = 900;
        // «HeroID» en las listas de victorias y de aspecto; «heroID» en las de «lifetime»
        // «HeroID» in the victory and look lists; «heroID» in the «lifetime» ones
        private static readonly Regex ReHeroIdEntrada = new Regex(@"""[Hh]eroID""\s*:\s*(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // ¿Trae el texto alguna de las listas de victorias por héroe?
        // Does the text contain any of the per-hero victory lists?
        private static bool TieneListasHeroesJugador(string json)
        {
            foreach (string clave in ListasHeroesJugador)
                if (json.IndexOf("\"" + clave + "\"", StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        // ¿Es un héroe nuevo? Los de la tabla o, aunque su módulo no esté instalado, cualquiera desde el 900
        // Is it a new hero? Those in the table or, even if their module isn't installed, any from 900 on
        private static bool EsHeroeNuevoId(TablaHeroes tabla, int id) => id >= PrimerHeroeNuevo || (tabla != null && tabla.EsNuevo(id));

        // Héroe de una entrada (objeto JSON) o -1
        // Hero of an entry (JSON object) or -1
        private static int HeroeDeEntrada(string objeto)
        {
            var m = ReHeroIdEntrada.Match(objeto);
            return m.Success && int.TryParse(m.Groups[1].Value, out int h) ? h : -1;
        }

        /// <summary>
        /// Localiza el array de objetos de <paramref name="clave"/> («"clave": [ {...}, {...} ]», con arrays anidados dentro
        /// de cada objeto): posición del «[» y del «]» y, de cada objeto de primer nivel, su inicio y su fin (exclusivo).
        /// false si la clave no está o el array no se puede leer.
        /// Locates the array of objects of <paramref name="clave"/> («"clave": [ {...}, {...} ]», with nested arrays inside
        /// each object): position of the «[» and the «]» and, for each top-level object, its start and end (exclusive).
        /// false if the key is missing or the array can't be read.
        /// </summary>
        internal static bool PartirArrayObjetos(string json, string clave, out int abre, out int cierra, out List<(int ini, int fin)> objetos)
        {
            abre = cierra = -1;
            objetos = new List<(int, int)>();
            int k = json.IndexOf("\"" + clave + "\"", StringComparison.Ordinal);
            if (k < 0) return false;
            int i = k + clave.Length + 2;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length || json[i] != ':') return false;
            i++;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length || json[i] != '[') return false;
            abre = i;
            int prof = 0, iniObjeto = -1;
            bool enTexto = false;
            for (int j = i + 1; j < json.Length; j++)
            {
                char c = json[j];
                // Dentro de una cadena no cuentan las llaves ni los corchetes
                // Inside a string, braces and brackets don't count
                if (enTexto) { if (c == '\\') j++; else if (c == '"') enTexto = false; continue; }
                if (c == '"') { enTexto = true; continue; }
                if (c == '{' || c == '[')
                {
                    if (prof == 0 && c == '{') iniObjeto = j;
                    prof++;
                }
                else if (c == '}' || c == ']')
                {
                    if (prof == 0)
                    {
                        if (c != ']') return false;
                        cierra = j;
                        return true;
                    }
                    prof--;
                    if (prof == 0 && c == '}' && iniObjeto >= 0) { objetos.Add((iniObjeto, j + 1)); iniObjeto = -1; }
                }
            }
            return false;
        }

        // Rehace el contenido de un array de objetos con el estilo del original («[]» si queda vacío). Sin objetos en el
        // original, la sangría sale de la línea de la clave (estilo «pretty» de JsonUtility) o va sin saltos (compacto)
        // Rebuilds the content of an array of objects in the original's style («[]» if it ends up empty). With no objects in the
        // original, the indentation comes from the key's line (JsonUtility «pretty» style) or has no line breaks (compact)
        internal static string UnirObjetos(string json, string clave, int abre, int cierra, List<(int ini, int fin)> originales, List<string> objetos)
        {
            if (objetos.Count == 0) return "[]";
            string antes, despues;
            if (originales.Count > 0)
            {
                antes = json.Substring(abre + 1, originales[0].ini - abre - 1);
                despues = json.Substring(originales[originales.Count - 1].fin, cierra - originales[originales.Count - 1].fin);
            }
            else
            {
                int k = json.IndexOf("\"" + clave + "\"", StringComparison.Ordinal);
                int inicioLinea = json.LastIndexOf('\n', Math.Max(0, k)) + 1;
                bool pretty = inicioLinea > 0;
                string sangria = pretty ? json.Substring(inicioLinea, k - inicioLinea) : "";
                antes = pretty ? "\n" + sangria + "    " : "";
                despues = pretty ? "\n" + sangria : "";
            }
            return "[" + antes + string.Join("," + antes, objetos) + despues + "]";
        }

        // Ida al disco: quita las entradas de héroes nuevos y las apunta (dentro del lote de HeroesParaDisco)
        // Write to disk: removes the new heroes' entries and records them (inside the HeroesParaDisco batch)
        private static string EntradasHeroesParaDisco(string json, TablaHeroes tabla, RegistroGuardias registro)
        {
            string res = json;
            foreach (string clave in ListasHeroesJugador)
            {
                if (!PartirArrayObjetos(res, clave, out int abre, out int cierra, out var objetos)) continue;
                var quedan = new List<string>();
                bool quitada = false;
                foreach (var (ini, fin) in objetos)
                {
                    string objeto = res.Substring(ini, fin - ini);
                    int h = HeroeDeEntrada(objeto);
                    if (h >= 0 && EsHeroeNuevoId(tabla, h)) { registro.FijarEntradaHeroe(clave, h, objeto); quitada = true; }
                    else quedan.Add(objeto);
                }
                if (!quitada) continue;
                res = res.Substring(0, abre) + UnirObjetos(res, clave, abre, cierra, objetos, quedan) + res.Substring(cierra + 1);
            }
            return res;
        }

        // Vuelta del disco: añade al final las entradas apuntadas de los héroes nuevos activos que falten
        // Read from disk: appends the recorded entries of the active new heroes that are missing
        private static string EntradasHeroesDesdeDisco(string json, TablaHeroes tabla, RegistroGuardias registro)
        {
            if (!registro.TieneEntradasHeroes) return json;
            string res = json;
            foreach (string clave in ListasHeroesJugador)
            {
                var apuntadas = registro.EntradasHeroes(clave);
                if (apuntadas.Count == 0 || !PartirArrayObjetos(res, clave, out int abre, out int cierra, out var objetos)) continue;
                var presentes = new HashSet<int>();
                var todos = new List<string>();
                foreach (var (ini, fin) in objetos)
                {
                    string objeto = res.Substring(ini, fin - ini);
                    presentes.Add(HeroeDeEntrada(objeto));
                    todos.Add(objeto);
                }
                bool anadida = false;
                foreach (var kv in apuntadas.OrderBy(x => x.Key))
                    if (tabla.Activo(kv.Key) && !presentes.Contains(kv.Key)) { todos.Add(kv.Value); anadida = true; }
                if (!anadida) continue;
                res = res.Substring(0, abre) + UnirObjetos(res, clave, abre, cierra, objetos, todos) + res.Substring(cierra + 1);
            }
            return res;
        }

        // Entradas de héroes nuevos que quedan en las listas de victorias del save del jugador
        // New hero entries left in the victory lists of the player save
        private static int ContarEntradasHeroes(string json, TablaHeroes tabla)
        {
            int n = 0;
            foreach (string clave in ListasHeroesJugador)
            {
                if (!PartirArrayObjetos(json, clave, out _, out _, out var objetos)) continue;
                foreach (var (ini, fin) in objetos)
                {
                    int h = HeroeDeEntrada(json.Substring(ini, fin - ini));
                    if (h >= 0 && EsHeroeNuevoId(tabla, h)) n++;
                }
            }
            return n;
        }

        /// <summary>Cuenta los héroes nuevos que quedan en el save de campaña o en el save del jugador («lastHeroID» y, desde la
        /// revisión de la 0.7.2, las victorias por héroe) (debe ser 0 tras HeroesParaDisco).</summary>
        /// Counts the new heroes left in the campaign save or the player save («lastHeroID» and, since the
        /// 0.7.2 review, wins per hero) (must be 0 after HeroesParaDisco).
        public static int ContarResiduosHeroes(string json, TablaHeroes tabla)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            bool hayTabla = tabla != null && tabla.Numero > 0;
            if (!EsPartida(json))
            {
                // Save del jugador: su «lastHeroID» (necesita la tabla) y sus victorias por héroe (9xx aunque no haya tabla)
                // Player save: its «lastHeroID» (needs the table) and its wins per hero (9xx even if there is no table)
                int r = TieneListasHeroesJugador(json) ? ContarEntradasHeroes(json, tabla) : 0;
                if (!hayTabla) return r;
                var u = ReUltimoHeroe.Match(json);
                return r + (u.Success && int.TryParse(u.Groups[2].Value, out int h) && tabla.EsNuevo(h) ? 1 : 0);
            }
            if (!hayTabla) return 0;
            int n = 0;
            foreach (var (_, _, re) in CamposHeroe)
            {
                var m = re.Match(json);
                if (m.Success && int.TryParse(m.Groups[2].Value, out int v) && tabla.EsNuevo(v)) n++;
            }
            return n;
        }

        // ---------------------------------------------------------------
        // Historial de partidas y registro de batallas (0.7.5, revisión total del 02-11). Hasta la 0.7.4 se dejaban tal
        // cual («solo es texto»), pero el juego 1.11.1 pinta el detalle de cada partida del historial (detailVersion 1): sin
        // el núcleo, la raza 41 o el héroe 901 de una partida no tienen datos. Ahora:
        //   - save del jugador, «runHistory»: el «heroID» de cada partida (9xx -> prestado) y, en sus «battles», la «race»
        //     (40/41 -> sustituta) y el «warlord» (9xx -> prestado). Se apunta por el runUUID de la partida y la partida se
        //     marca como jugada con módulos (así los logros la siguen reconociendo aunque el save ya no lleve el 9xx);
        //   - save de campaña, «battleLog» (batallas de la partida en curso): «race» y «warlord» igual, por la ranura de su
        //     partida y slot.
        // Al leer vuelven los de los módulos activos si en el save sigue lo escrito. Lo apuntado nunca se olvida (el
        // historial de una partida terminada no cambia). Sin tabla de héroes o de razas, esa parte no se toca (no se sabe el
        // prestado ni la sustituta).
        // Run history and battle log (0.7.5, full review from 02-11). Up to 0.7.4 they were left as
        // is («it's just text»), but game 1.11.1 renders the detail of each run in the history (detailVersion 1): without
        // the core, race 41 or hero 901 of a run have no data. Now:
        //   - player save, «runHistory»: the «heroID» of each run (9xx -> borrowed) and, in its «battles», the «race»
        //     (40/41 -> substitute) and the «warlord» (9xx -> borrowed). It is recorded by the run's runUUID and the run is
        //     marked as played with modules (so the achievements still recognize it even if the save no longer carries the 9xx);
        //   - campaign save, «battleLog» (battles of the current run): «race» and «warlord» likewise, by the slot of its
        //     run and slot.
        // On read, those of the active modules are restored if the save still holds what was written. What is recorded is never forgotten (the
        // history of a finished run does not change). Without a hero or race table, that part is left untouched (the
        // borrowed one and the substitute are unknown).
        // ---------------------------------------------------------------
        internal const string ClaveRuns = "runHistory", ClaveBatallasRun = "battles", ClaveBatallasPartida = "battleLog";
        private const string RanuraBatallas = "batallas";
        private static readonly Regex ReHeroIdRun = new Regex(@"(""heroID""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ReRazaBatalla = new Regex(@"(""race""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ReSenorBatalla = new Regex(@"(""warlord""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ReRunUuid = new Regex(@"""runUUID""\s*:\s*""([^""]*)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Ida al disco del historial de partidas (save del jugador) y del registro de batallas (save de campaña): héroes y
        /// razas nuevos a su prestado y sustituta, apuntando lo real en el registro. Mismo texto si no hay nada que cambiar.
        /// Trip to disk of the run history (player save) and the battle log (campaign save): new heroes and
        /// races become their borrowed hero and substitute, recording the real ones in the registry. Same text if there is nothing to change.
        /// </summary>
        public static string HistorialParaDisco(string json, TablaHeroes heroes, TablaRazas razas, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || registro == null) return json;
            bool hayHeroes = heroes != null && heroes.Numero > 0, hayRazas = razas != null && razas.Numero > 0;
            if (!hayHeroes && !hayRazas) return json;
            bool runs = json.IndexOf("\"" + ClaveRuns + "\"", StringComparison.Ordinal) >= 0;
            bool log = json.IndexOf("\"" + ClaveBatallasPartida + "\"", StringComparison.Ordinal) >= 0 && EsPartida(json);
            if (!runs && !log) return json;
            // Un texto que ya salió saneado por esta etapa se devuelve tal cual (segunda pasada)
            // A text that already came out sanitized by this stage is returned as is (second pass)
            int previas = registro.EtapasDe(json);
            if ((previas & RegistroGuardias.EtapaHistorial) != 0) return json;
            registro.IniciarLote();
            try
            {
                string res = json;
                if (log)
                {
                    var ctx = ContextoSave.De(json);
                    var rz = new Dictionary<int, int>();
                    var sn = new Dictionary<int, int>();
                    res = BatallasParaDisco(res, ClaveBatallasPartida, heroes, razas, rz, sn);
                    registro.FundirPartida(true, ctx.Ranura(RanuraBatallas), ctx.Firma, 0, 0, rz, sn);
                }
                if (runs) res = RunsParaDisco(res, heroes, razas, registro);
                registro.RecordarSalida(res, previas | RegistroGuardias.EtapaHistorial);
                return res;
            }
            finally { registro.TerminarLote(); }
        }

        // Batallas de una lista (battleLog o battles de una partida): raza nueva -> sustituta y señor de la guerra nuevo ->
        // prestado, apuntando la posición en rz y sn. De la última a la primera: las posiciones de las anteriores no se mueven
        // Battles of a list (battleLog or battles of a run): new race -> substitute and new warlord ->
        // borrowed, recording the position in rz and sn. From last to first: the positions of the earlier ones do not shift
        private static string BatallasParaDisco(string json, string clave, TablaHeroes heroes, TablaRazas razas, Dictionary<int, int> rz, Dictionary<int, int> sn)
        {
            if (!PartirArrayObjetos(json, clave, out _, out _, out var objetos) || objetos.Count == 0) return json;
            string res = json;
            for (int i = objetos.Count - 1; i >= 0; i--)
            {
                var (ini, fin) = objetos[i];
                string objeto = res.Substring(ini, fin - ini);
                string nuevo = objeto;
                int pos = i;
                if (razas != null && razas.Numero > 0)
                    nuevo = ReRazaBatalla.Replace(nuevo, m =>
                    {
                        if (!int.TryParse(m.Groups[2].Value, out int r) || !razas.EsNueva(r)) return m.Value;
                        rz[pos] = r;
                        return m.Groups[1].Value + razas.SustitutaDe(r);
                    }, 1);
                if (heroes != null && heroes.Numero > 0)
                    nuevo = ReSenorBatalla.Replace(nuevo, m =>
                    {
                        if (!int.TryParse(m.Groups[2].Value, out int w) || !heroes.EsNuevo(w)) return m.Value;
                        sn[pos] = w;
                        return m.Groups[1].Value + heroes.PrestadoDe(w);
                    }, 1);
                if (nuevo != objeto) res = res.Substring(0, ini) + nuevo + res.Substring(fin);
            }
            return res;
        }

        // Partidas del historial: héroe del jugador y batallas de cada una, apuntadas por su runUUID
        // Runs in the history: player's hero and battles of each one, recorded by its runUUID
        private static string RunsParaDisco(string json, TablaHeroes heroes, TablaRazas razas, RegistroGuardias registro)
        {
            if (!PartirArrayObjetos(json, ClaveRuns, out _, out _, out var partidas) || partidas.Count == 0) return json;
            string res = json;
            for (int i = partidas.Count - 1; i >= 0; i--)
            {
                var (ini, fin) = partidas[i];
                string objeto = res.Substring(ini, fin - ini);
                var mu = ReRunUuid.Match(objeto);
                string uuid = mu.Success && mu.Groups[1].Value.Length > 0 ? mu.Groups[1].Value : null;
                int heroe = 0, escrito = 0;
                // «heroID» de la partida: el primero del objeto (sus listas anidadas no llevan ese campo)
                // «heroID» of the run: the first one in the object (its nested lists do not carry that field)
                string nuevo = heroes == null || heroes.Numero == 0 ? objeto : ReHeroIdRun.Replace(objeto, m =>
                {
                    if (!int.TryParse(m.Groups[2].Value, out int h) || !heroes.EsNuevo(h)) return m.Value;
                    heroe = h; escrito = heroes.PrestadoDe(h);
                    return m.Groups[1].Value + escrito;
                }, 1);
                var rz = new Dictionary<int, int>();
                var sn = new Dictionary<int, int>();
                nuevo = BatallasParaDisco(nuevo, ClaveBatallasRun, heroes, razas, rz, sn);
                if (nuevo == objeto) continue;
                res = res.Substring(0, ini) + nuevo + res.Substring(fin);
                if (uuid == null) continue;   // sin runUUID no se puede devolver al leer (no debería pasar)
                                              // without runUUID it cannot be restored on read (should not happen)
                registro.FundirPartida(false, uuid, uuid, heroe, escrito, rz, sn);
                registro.MarcarPartidaModificada(uuid);
            }
            return res;
        }

        /// <summary>
        /// Vuelta del disco del historial y del registro de batallas: lo apuntado de los módulos activos vuelve si en el save
        /// sigue lo escrito (prestado o sustituta). Mismo texto si no hay nada que cambiar.
        /// Return from disk of the history and the battle log: what was recorded for active modules is restored if the save
        /// still holds what was written (borrowed or substitute). Same text if there is nothing to change.
        /// </summary>
        public static string HistorialDesdeDisco(string json, TablaHeroes heroes, TablaRazas razas, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || registro == null || !registro.TieneHistorial) return json;
            string res = json;
            if (EsPartida(json) && json.IndexOf("\"" + ClaveBatallasPartida + "\"", StringComparison.Ordinal) >= 0
                && registro.PartidaDe(true, ContextoSave.De(json).Ranura(RanuraBatallas), out _, out _, out var rz, out var sn))
                res = BatallasDesdeDisco(res, ClaveBatallasPartida, heroes, razas, rz, sn);
            if (json.IndexOf("\"" + ClaveRuns + "\"", StringComparison.Ordinal) >= 0
                && PartirArrayObjetos(res, ClaveRuns, out _, out _, out var partidas))
            {
                for (int i = partidas.Count - 1; i >= 0; i--)
                {
                    var (ini, fin) = partidas[i];
                    string objeto = res.Substring(ini, fin - ini);
                    var mu = ReRunUuid.Match(objeto);
                    if (!mu.Success || !registro.PartidaDe(false, mu.Groups[1].Value, out int heroe, out int escrito, out var rzr, out var snr)) continue;
                    string nuevo = objeto;
                    if (heroe > 0 && heroes != null && heroes.Activo(heroe))
                        nuevo = ReHeroIdRun.Replace(nuevo, m => int.TryParse(m.Groups[2].Value, out int h) && h == escrito ? m.Groups[1].Value + heroe : m.Value, 1);
                    nuevo = BatallasDesdeDisco(nuevo, ClaveBatallasRun, heroes, razas, rzr, snr);
                    if (nuevo != objeto) res = res.Substring(0, ini) + nuevo + res.Substring(fin);
                }
            }
            return res;
        }

        // Batallas de una lista: cada posición apuntada vuelve a su raza o señor real si sigue la sustituta o el prestado
        // Battles of a list: each recorded position is restored to its real race or warlord if the substitute or the borrowed one is still there
        private static string BatallasDesdeDisco(string json, string clave, TablaHeroes heroes, TablaRazas razas, Dictionary<int, int> rz, Dictionary<int, int> sn)
        {
            if ((rz.Count == 0 && sn.Count == 0) || !PartirArrayObjetos(json, clave, out _, out _, out var objetos)) return json;
            string res = json;
            for (int i = objetos.Count - 1; i >= 0; i--)
            {
                var (ini, fin) = objetos[i];
                string objeto = res.Substring(ini, fin - ini);
                string nuevo = objeto;
                if (razas != null && razas.Numero > 0 && rz.TryGetValue(i, out int real) && razas.EsNueva(real))
                    nuevo = ReRazaBatalla.Replace(nuevo, m => int.TryParse(m.Groups[2].Value, out int v) && v == razas.SustitutaDe(real) ? m.Groups[1].Value + real : m.Value, 1);
                if (heroes != null && heroes.Numero > 0 && sn.TryGetValue(i, out int senor) && heroes.Activo(senor))
                    nuevo = ReSenorBatalla.Replace(nuevo, m => int.TryParse(m.Groups[2].Value, out int v) && v == heroes.PrestadoDe(senor) ? m.Groups[1].Value + senor : m.Value, 1);
                if (nuevo != objeto) res = res.Substring(0, ini) + nuevo + res.Substring(fin);
            }
            return res;
        }

        /// <summary>
        /// Cuenta los héroes y razas nuevos que quedan en el historial de partidas y en el registro de batallas (debe ser 0
        /// tras HistorialParaDisco). Solo los que las tablas conocen (sin tabla no hay con qué sanearlos).
        /// Counts the new heroes and races left in the run history and the battle log (must be 0
        /// after HistorialParaDisco). Only those the tables know (without a table there is nothing to sanitize them with).
        /// </summary>
        public static int ContarResiduosHistorial(string json, TablaHeroes heroes, TablaRazas razas)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            bool hayHeroes = heroes != null && heroes.Numero > 0, hayRazas = razas != null && razas.Numero > 0;
            if (!hayHeroes && !hayRazas) return 0;
            int n = 0;
            // Razas y señores nuevos de las batallas de una lista
            // New races and warlords of the battles of a list
            int Batallas(string texto, string clave)
            {
                int k = 0;
                if (!PartirArrayObjetos(texto, clave, out _, out _, out var objetos)) return 0;
                foreach (var (ini, fin) in objetos)
                {
                    string o = texto.Substring(ini, fin - ini);
                    var r = ReRazaBatalla.Match(o);
                    if (hayRazas && r.Success && int.TryParse(r.Groups[2].Value, out int rv) && razas.EsNueva(rv)) k++;
                    var w = ReSenorBatalla.Match(o);
                    if (hayHeroes && w.Success && int.TryParse(w.Groups[2].Value, out int wv) && heroes.EsNuevo(wv)) k++;
                }
                return k;
            }
            if (EsPartida(json) && json.IndexOf("\"" + ClaveBatallasPartida + "\"", StringComparison.Ordinal) >= 0) n += Batallas(json, ClaveBatallasPartida);
            if (json.IndexOf("\"" + ClaveRuns + "\"", StringComparison.Ordinal) >= 0 && PartirArrayObjetos(json, ClaveRuns, out _, out _, out var partidas))
                foreach (var (ini, fin) in partidas)
                {
                    string o = json.Substring(ini, fin - ini);
                    var h = ReHeroIdRun.Match(o);
                    if (hayHeroes && h.Success && int.TryParse(h.Groups[2].Value, out int hv) && heroes.EsNuevo(hv)) n++;
                    n += Batallas(o, ClaveBatallasRun);
                }
            return n;
        }

        // ---------------------------------------------------------------
        // Verificación: ¿queda alguna unidad nueva en las rutas conocidas?
        // Check: is any new unit left in the known paths?
        // ---------------------------------------------------------------
        /// <summary>Cuenta las unidades nuevas que quedan en claves de unidad conocidas (debe ser 0 tras ParaDisco).</summary>
        /// Counts the new units left in known unit keys (must be 0 after ParaDisco).
        public static int ContarResiduos(string json, TablaUnidades tabla)
        {
            if (string.IsNullOrEmpty(json) || tabla == null || !tabla.ReIds.IsMatch(json)) return 0;
            int n = 0;
            foreach (Match m in ReCampoSuelto.Matches(json)) if (tabla.Contiene(m.Groups[2].Value)) n++;
            // 0.7.5: también los arrays con posición nuevos de la 1.11.1 (battleRecruitOffer, mercenariesLeft, coopStartArmy)
            // 0.7.5: also the new position arrays from 1.11.1 (battleRecruitOffer, mercenariesLeft, coopStartArmy)
            foreach (string clave in ArraysConPosicion.Select(a => a.clave).Concat(ListasColeccion).Concat(ArraysSinRecuperacion))
            {
                foreach (Match m in Array(clave).Matches(json))
                    foreach (string e in PartirArray(m.Groups[2].Value)) if (tabla.Contiene(e)) n++;
            }
            return n;
        }

        // ---------------------------------------------------------------
        // Utilidades de arrays de números en texto JSON
        // Utilities for number arrays in JSON text
        // ---------------------------------------------------------------
        // Parte el contenido de un array JSON de números en elementos ya recortados
        // Splits the content of a JSON number array into already trimmed elements
        internal static List<string> PartirArray(string contenido)
        {
            var lista = new List<string>();
            if (string.IsNullOrWhiteSpace(contenido)) return lista;
            foreach (string trozo in contenido.Split(','))
            {
                string t = trozo.Trim();
                if (t.Length > 0) lista.Add(t);
            }
            return lista;
        }

        // Vuelve a unir los elementos respetando el estilo (en líneas con sangría si el original las tenía)
        // Joins the elements back together preserving the style (on indented lines if the original had them)
        internal static string UnirArray(List<string> elementos, string original)
        {
            if (elementos.Count == 0) return original.Contains("\n") ? "\n" + SangriaCierre(original) : "";
            int salto = original.IndexOf('\n');
            if (salto < 0) return string.Join(",", elementos);
            // Estilo "pretty": cada elemento en su línea con la sangría del primero
            // "pretty" style: each element on its own line with the indentation of the first
            string sangria = SangriaPrimerElemento(original);
            var sb = new StringBuilder("\n");
            for (int i = 0; i < elementos.Count; i++)
            {
                sb.Append(sangria).Append(elementos[i]);
                sb.Append(i < elementos.Count - 1 ? ",\n" : "\n");
            }
            sb.Append(SangriaCierre(original));
            return sb.ToString();
        }

        // Sangría que precede al primer elemento del array original
        // Indentation preceding the first element of the original array
        private static string SangriaPrimerElemento(string original)
        {
            int salto = original.IndexOf('\n');
            int i = salto + 1;
            int ini = i;
            while (i < original.Length && (original[i] == ' ' || original[i] == '\t')) i++;
            return original.Substring(ini, i - ini);
        }

        // Sangría que precede al corchete de cierre (lo que hay tras el último salto de línea)
        // Indentation preceding the closing bracket (what comes after the last line break)
        private static string SangriaCierre(string original)
        {
            int salto = original.LastIndexOf('\n');
            return salto < 0 ? "" : original.Substring(salto + 1);
        }
    }
}
