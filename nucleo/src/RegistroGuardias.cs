// RegistroGuardias.cs
// Registro aparte (fuera de los saves del juego) de qué escuadras guardadas son en realidad unidades nuevas 9xx.
// Se guarda en BepInEx\config\ZeroNucleo_registro.json (hasta la 0.6.1, ZeroGuardiaImperial_registro.json, que el
// núcleo copia la primera vez). Contiene (versión 2):
//   - GUID (UniqueID) de cada escuadra -> id real (900, 941...); en el save figura como su unidad base
//   - posición de cada oferta de reclutamiento que era una unidad nueva -> id real, por separado para el save normal
//     (campaignSaveData.json, "snapShot": false) y para la instantánea del nodo (campaignSaveDataSnapshot.json, true)
//   - por cada lista de colección, qué ids nuevos contenía
//   - (0.5.0, F2) la raza nueva real (40/41) de la ciudad actual y del campo de batalla preparado, que en el save
//     figuran con su raza sustituta de serie; por ranura («ciudad», «campo», con «_instantanea» para la instantánea) y
//     con una firma (nombre y tamaño de la ciudad, semilla del campo) para no restaurarla sobre otra ciudad o campo
//   - (0.6.0) objetos nuevos 700-729: en las listas con posición (Gear, recruitableGear, townLootGearIDs) la posición,
//     el id real y el objeto de serie con el que se escribió; en las demás (SoldGear, brokenGear y las de colección
//     gearIdsCollected/gearIdsAcknowledged) qué ids nuevos se quitaron. Por ranura («_instantanea» para la instantánea)
//   - (0.7.1) la partida (runId del save de campaña) de cada ranura de objetos con o sin posición, para no restaurar
//     marcas de una partida en otra, y los héroes nuevos 901-904 del save de campaña (héroe del jugador y señor de la
//     guerra) guardados como su héroe prestado: ranura -> héroe real, prestado escrito y partida
//   - (0.7.2, revisión) las victorias de los héroes nuevos del save del jugador (entradas 9xx de
//     HeroDifficultiesCompleted y HeroLastDifficulties), que se quitan del save al escribir
//   - (revisión del 18-10, M-1) las bajas históricas de las unidades nuevas del save del jugador (entradas 9xx de
//     UnitNameHistoricalKillStore), que se quitan del save al escribir igual que las victorias de los héroes
//   - (revisión del 18-10, M-2) las partidas (runId) que se han jugado con módulos de Zero: no van a clasificaciones ni
//     telemetría aunque se terminen en una sesión sin módulos
//   - (0.7.4) los hechizos propios 1900-1999 que se quitan de los saves (SaneadoHechizos.cs): por ranura, la posición de
//     cada uno en las listas de cartas (selectedSpells, playerCustomBattleSpells, spells del historial) y los objetos
//     {Spell, Casts} de las listas de lanzamientos (spellsCast), con la firma de la partida; y la lista de los lanzados
//     alguna vez (spellsEverCast del save del jugador), que solo crece
//   - (0.7.5, revisión total del 02-11) las ranuras de los saves de campaña llevan la partida y el slot en la clave
//     («heroe@principal@<runId>», «Gear_instantanea@diaria@<runId>»...; SaneadoSaves.ContextoSave). Antes una sola ranura
//     servía para la campaña principal, la diaria y la cooperativa, y guardar una borraba lo apuntado de la otra. Las
//     ofertas de reclutamiento van ya por ranura (OfertasPorRanura). Las ranuras viejas sin partida en la clave se siguen
//     leyendo (con su comprobación de firma de siempre) hasta que esa misma partida se vuelve a guardar
//   - (0.7.5) el historial de partidas del save del jugador y el registro de batallas de la partida: héroe del jugador,
//     raza y señor de la guerra de cada batalla que eran nuevos (HistorialPartidas por runUUID, BatallasPartida por ranura)
// Las entradas de partidas ya terminadas no se borran (como los GUID): son pocas e inofensivas.
// Además, solo en memoria (no va al fichero), los últimos textos saneados: así el saneado no olvida lo apuntado cuando
// el mismo texto pasa dos veces (ver «Memoria de textos ya saneados» más abajo).
// La versión 1 (plugin 0.1-0.3, solo la 900) se lee y se convierte sola: sus GUID, ofertas y listas pasan a ser de
// la 900. Antes de reescribirlo por primera vez en formato nuevo se deja una copia «.v1-<fecha>» al lado.
// Si este fichero se pierde, la partida sigue cargando: las unidades nuevas quedan como su unidad base.
// Guardado: cada cambio se escribe al momento, salvo dentro de un lote (IniciarLote/TerminarLote), que escribe una sola
// vez al final si hubo cambios (el saneado de un save entero es un lote).
// Limpieza de GUID huérfanos (escuadras que ya no existen): NO se hace. Para saber que un GUID ya no se usa habría que
// mirar todos los saves a la vez (normal, instantánea, historial) y el juego los reescribe por separado; borrar uno
// que aún se usara haría que esa escuadra volviera para siempre como su base. Las entradas sobrantes son inofensivas.
// .NET puro (System.Text.Json) para poder probarlo fuera del juego.
// RegistroGuardias.cs
// Separate registry (outside the game's saves) of which saved squads are actually new 9xx units.
// It is saved in BepInEx\config\ZeroNucleo_registro.json (up to 0.6.1, ZeroGuardiaImperial_registro.json, which the
// core copies the first time). It contains (version 2):
//   - GUID (UniqueID) of each squad -> real id (900, 941...); in the save it appears as its base unit
//   - position of each recruitment offer that was a new unit -> real id, separately for the normal save
//     (campaignSaveData.json, "snapShot": false) and for the node snapshot (campaignSaveDataSnapshot.json, true)
//   - for each collection list, which new ids it contained
//   - (0.5.0, F2) the real new race (40/41) of the current city and the prepared battlefield, which in the save
//     appear with their default substitute race; by slot ("ciudad", "campo", with "_instantanea" for the snapshot) and
//     with a signature (city name and size, battlefield seed) so it is not restored onto a different city or battlefield
//   - (0.6.0) new gear 700-729: in the lists with positions (Gear, recruitableGear, townLootGearIDs) the position,
//     the real id and the default item it was written with; in the others (SoldGear, brokenGear and the collection ones
//     gearIdsCollected/gearIdsAcknowledged) which new ids were removed. By slot ("_instantanea" for the snapshot)
//   - (0.7.1) the run (campaign save runId) of each gear slot, with or without position, so marks from one run are not restored
//     into another, and the new heroes 901-904 of the campaign save (player hero and warlord)
//     saved as their borrowed hero: slot -> real hero, borrowed hero written and run
//   - (0.7.2, review) the victories of the new heroes in the player save (9xx entries of
//     HeroDifficultiesCompleted and HeroLastDifficulties), which are removed from the save when writing
//   - (review of 18-10, M-1) the historical kills of the new units in the player save (9xx entries of
//     UnitNameHistoricalKillStore), which are removed from the save when writing, just like the heroes' victories
//   - (review of 18-10, M-2) the runs (runId) that were played with Zero modules: they do not go to leaderboards or
//     telemetry even if they end in a session without modules
//   - (0.7.4) the custom spells 1900-1999 that are removed from the saves (SaneadoHechizos.cs): by slot, the position of
//     each one in the card lists (selectedSpells, playerCustomBattleSpells, history spells) and the objects
//     {Spell, Casts} of the cast lists (spellsCast), with the run signature; and the list of those ever
//     cast (spellsEverCast in the player save), which only grows
//   - (0.7.5, full review of 02-11) the campaign save slots carry the run and the slot in the key
//     ("heroe@principal@<runId>", "Gear_instantanea@diaria@<runId>"...; SaneadoSaves.ContextoSave). Before, a single slot
//     served the main, daily and co-op campaigns, and saving one erased what was recorded for the other. The
//     recruitment offers now go by slot (OfertasPorRanura). Old slots without a run in the key are still
//     read (with their usual signature check) until that same run is saved again
//   - (0.7.5) the run history of the player save and the battle log of the run: player hero,
//     race and warlord of each battle that were new (HistorialPartidas by runUUID, BatallasPartida by slot)
// Entries for runs that have already ended are not deleted (like the GUIDs): they are few and harmless.
// Also, in memory only (not written to the file), the latest sanitized texts: so the sanitizing does not forget what was recorded when
// the same text passes through twice (see "Memoria de textos ya saneados" below).
// Version 1 (plugin 0.1-0.3, only 900) is read and converted automatically: its GUIDs, offers and lists become those of
// 900. Before rewriting it for the first time in the new format, a copy ".v1-<fecha>" is left next to it.
// If this file is lost, the run still loads: the new units remain as their base unit.
// Saving: each change is written immediately, except inside a batch (IniciarLote/TerminarLote), which writes only once
// at the end if there were changes (sanitizing a whole save is a batch).
// Cleanup of orphan GUIDs (squads that no longer exist): NOT done. To know that a GUID is no longer used, you would have to
// look at all the saves at once (normal, snapshot, history) and the game rewrites them separately; deleting one
// that was still in use would make that squad come back forever as its base. The leftover entries are harmless.
// Pure .NET (System.Text.Json) so it can be tested outside the game.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: datos persistidos y acceso seguro entre hilos
    // Section: persisted data and thread-safe access
    // -----------------------------------------------------------------------
    public class RegistroGuardias
    {
        // Id que reciben las entradas antiguas (versión 1), que solo podían ser facción A
        // Id given to old entries (version 1), which could only be faction A
        public const int IdLegado = 900;

        // Forma en disco del registro (propiedades públicas para System.Text.Json)
        // On-disk form of the registry (public properties for System.Text.Json)
        public class Datos
        {
            public int Version { get; set; } = 2;
            // Versión 1 (solo se leen; al guardar quedan vacías)
            // Version 1 (read-only; empty when saved)
            public List<string> Guids { get; set; } = new List<string>();
            public List<int> IndicesOfertas { get; set; } = new List<int>();
            public List<string> ListasMarcadas { get; set; } = new List<string>();
            // Versión 2
            // Version 2
            public Dictionary<string, int> GuidsPorId { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, int> Ofertas { get; set; } = new Dictionary<string, int>();          // "posición" -> id
                                                                                                           // "position" -> id
            public Dictionary<string, int> OfertasInstantanea { get; set; } = new Dictionary<string, int>(); // lo mismo en la instantánea
                                                                                                             // same in the snapshot
            public Dictionary<string, List<int>> ListasPorId { get; set; } = new Dictionary<string, List<int>>();
            // 0.5.0 (F2): razas nuevas guardadas en el save como su sustituta (ranura -> raza real y firma)
            // 0.5.0 (F2): new races stored in the save as their stand-in (slot -> real race and signature)
            public Dictionary<string, RazaApuntada> RazasDisco { get; set; } = new Dictionary<string, RazaApuntada>();
            // 0.6.0: objetos nuevos. Ranura -> posición -> (id real, objeto escrito) y ranura -> ids quitados
            // 0.6.0: new gear. Slot -> position -> (real id, written item) and slot -> removed ids
            public Dictionary<string, Dictionary<string, ObjetoApuntado>> ObjetosPosicion { get; set; } = new Dictionary<string, Dictionary<string, ObjetoApuntado>>();
            public Dictionary<string, List<int>> ObjetosListas { get; set; } = new Dictionary<string, List<int>>();
            // 0.7.1: partida (runId) a la que pertenece lo apuntado de cada ranura de objetos con posición o sin posición
            // 0.7.1: run (runId) that the entries of each gear slot, with or without position, belong to
            public Dictionary<string, string> ObjetosPartida { get; set; } = new Dictionary<string, string>();
            // 0.7.1: héroes nuevos guardados en el save como su héroe prestado (ranura -> real, escrito y partida)
            // 0.7.1: new heroes stored in the save as their borrowed hero (slot -> real, written and run)
            public Dictionary<string, HeroeApuntado> HeroesDisco { get; set; } = new Dictionary<string, HeroeApuntado>();
            // 0.7.2 (revisión): victorias de héroes nuevos del save del jugador ("lista:héroe" -> objeto JSON tal cual)
            // 0.7.2 (review): victories of new heroes from the player's save ("list:hero" -> JSON object as is)
            public Dictionary<string, string> EntradasHeroes { get; set; } = new Dictionary<string, string>();
            // Revisión del 18-10 (M-1): bajas históricas de unidades nuevas ("lista:unidad" -> objeto JSON tal cual)
            // Review of 18-10 (M-1): historical casualties of new units ("list:unit" -> JSON object as is)
            public Dictionary<string, string> EntradasUnidades { get; set; } = new Dictionary<string, string>();
            // Revisión del 18-10 (M-2): partidas (runId o firma por semilla) jugadas con módulos de Zero
            // Review of 18-10 (M-2): runs (runId or seed signature) played with Zero's modules
            public List<string> PartidasModificadas { get; set; } = new List<string>();
            // 0.7.6: regla del sorteo de facciones enemigas por partida (runId -> 1 la de antes, 2 la de la 0.7.6)
            // 0.7.6: rule for drawing enemy factions per run (runId -> 1 the old one, 2 the 0.7.6 one)
            public Dictionary<string, int> ReglaActos { get; set; } = new Dictionary<string, int>();
            // 0.7.6: logros que no se concedieron por batallas con «Impertérritos» (no se reenvían nunca a Steam)
            // 0.7.6: achievements not granted due to battles with «Impertérritos» (never resent to Steam)
            public List<int> LogrosBloqueadosMoral { get; set; } = new List<int>();
            // 0.7.4: hechizos propios quitados de los saves (ranura -> firma, posiciones y entradas) y los lanzados alguna vez
            // 0.7.4: custom spells removed from saves (slot -> signature, positions and entries) and the ones ever cast
            public Dictionary<string, HechizosApuntados> HechizosDisco { get; set; } = new Dictionary<string, HechizosApuntados>();
            public List<int> HechizosLanzados { get; set; } = new List<int>();
            // 0.7.5: ofertas de reclutamiento por ranura con partida y slot ("ofertas@principal@<runId>" -> posición -> id)
            // 0.7.5: recruitment offers per slot with run and slot ("ofertas@principal@<runId>" -> position -> id)
            public Dictionary<string, Dictionary<string, int>> OfertasPorRanura { get; set; } = new Dictionary<string, Dictionary<string, int>>();
            // 0.7.5: historial de partidas del save del jugador (runUUID -> héroe, razas y señores nuevos de sus batallas)
            // 0.7.5: run history from the player's save (runUUID -> hero, new races and lords from its battles)
            public Dictionary<string, PartidaApuntada> HistorialPartidas { get; set; } = new Dictionary<string, PartidaApuntada>();
            // 0.7.5: registro de batallas de la partida en curso (battleLog del save de campaña), por ranura con partida y slot
            // 0.7.5: battle log of the current run (battleLog of the campaign save), per slot with run and slot
            public Dictionary<string, PartidaApuntada> BatallasPartida { get; set; } = new Dictionary<string, PartidaApuntada>();
            // 0.7.7: icono elegido para el estandarte del jugador («juego:<textura>» o «<módulo>:<id>»; null = el de serie).
            // Global, como el armyBanner del save del jugador (Estandarte.cs)
            // 0.7.7: icon chosen for the player's banner ("juego:<textura>" or "<módulo>:<id>"; null = the default one).
            // Global, like the armyBanner of the player save (Estandarte.cs)
            public string IconoEstandarte { get; set; }
            // Revisión del 18-10, ronda 2 (B-9): campos que esta versión no conoce (registro escrito por una versión más
            // nueva). Se conservan tal cual y vuelven al fichero en cada Guardar, en vez de perderse
            // Review of 18-10, round 2 (B-9): fields this version does not know (registry written by a newer
            // version). They are kept as is and written back to the file on every Save, instead of being lost
            [System.Text.Json.Serialization.JsonExtensionData]
            public Dictionary<string, JsonElement> Resto { get; set; }
        }

        /// <summary>Héroe nuevo de una ranura del save (héroe del jugador o señor de la guerra), el prestado escrito y la partida.</summary>
        /// New hero of a save slot (player hero or warlord), the borrowed one written and the run.
        public class HeroeApuntado
        {
            public int Real { get; set; }
            public int Escrito { get; set; }
            public string Firma { get; set; }
        }

        /// <summary>
        /// Hechizos propios de una ranura del save (0.7.4): posición -> id en una lista de cartas (en el save va 0) y id ->
        /// objeto JSON tal cual en una lista de lanzamientos (en el save se quita), con la firma de la partida (null en el
        /// save del jugador y en la batalla personalizada).
        /// Custom spells of a save slot (0.7.4): position -> id in a card list (0 in the save) and id ->
        /// JSON object as is in a cast list (removed in the save), with the run signature (null in the
        /// player's save and in the custom battle).
        /// </summary>
        public class HechizosApuntados
        {
            public string Firma { get; set; }
            public Dictionary<string, int> Posiciones { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, string> Entradas { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Lo nuevo de una partida del historial o del registro de batallas (0.7.5): el héroe del jugador (real y prestado
        /// escrito; 0 = ninguno) y, por posición de la batalla en su lista, la raza enemiga real (40/41) y el señor de la
        /// guerra real (901-904). En el save van la raza sustituta y el héroe prestado. Firma: la partida (runUUID o runId).
        /// What is new in a run of the history or battle log (0.7.5): the player's hero (real and borrowed
        /// written; 0 = none) and, by battle position in its list, the real enemy race (40/41) and the real
        /// warlord (901-904). The save holds the stand-in race and the borrowed hero. Signature: the run (runUUID or runId).
        /// </summary>
        public class PartidaApuntada
        {
            public string Firma { get; set; }
            public int Heroe { get; set; }
            public int HeroeEscrito { get; set; }
            public Dictionary<string, int> Razas { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, int> Senores { get; set; } = new Dictionary<string, int>();
        }

        /// <summary>Objeto nuevo de una posición de una lista del save: su id real y el de serie que se escribió en su lugar.</summary>
        /// New item at a position of a save list: its real id and the default one written in its place.
        public class ObjetoApuntado
        {
            public int Real { get; set; }
            public int Escrito { get; set; }
        }

        /// <summary>Raza nueva real (40/41) de una ranura del save y la firma del objeto donde estaba.</summary>
        /// Real new race (40/41) of a save slot and the signature of the object where it was.
        public class RazaApuntada
        {
            public int Raza { get; set; }
            public string Firma { get; set; }
        }

        private readonly object _cerrojo = new object();
        private readonly string _ruta;           // null = solo en memoria (pruebas)
                                                 // null = in memory only (tests)
        private Datos _datos = new Datos();
        private Dictionary<string, int> _guids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private int _lote;           // > 0 = dentro de un lote: los cambios se guardan al terminarlo
                                     // > 0 = inside a batch: changes are saved when it ends
        private bool _sucio;         // hubo cambios dentro del lote
                                     // there were changes inside the batch
        // Revisión del 18-10 (L-18): el fichero era ilegible y no se pudo apartar; en esta sesión no se escribe para no
        // pisar el original (que quizá solo es de una versión más nueva) con un registro vacío
        // Review of 18-10 (L-18): the file was unreadable and could not be set aside; in this session nothing is written so as not to
        // overwrite the original (which may just be from a newer version) with an empty registry
        private bool _noGuardar;

        // Aviso opcional hacia el log del plugin (en pruebas queda a null)
        // Optional warning to the plugin log (null in tests)
        public Action<string> Avisar;

        public RegistroGuardias(string ruta) { _ruta = ruta; }

        /// <summary>
        /// ¿Es un registro solo en memoria (sin fichero)? Los de la autoprueba y las pruebas lo son. El vigilante de saves
        /// nunca sanea con uno así (revisión del 18-10, A-1): un GUID apuntado ahí se perdería al tirarlo.
        /// Is this an in-memory-only registry (no file)? Those of the self-test and the tests are. The save watcher
        /// never sanitizes with one like this (review of 18-10, A-1): a GUID recorded there would be lost when discarded.
        /// </summary>
        public bool SoloEnMemoria => _ruta == null;

        /// <summary>
        /// ¿Está el registro en solo lectura en esta sesión? (revisión del 18-10, ronda 2, M-A) Pasa si el fichero no se
        /// pudo leer tras los reintentos o si era ilegible y no se pudo apartar: nada se escribe hasta reiniciar.
        /// Is the registry read-only in this session? (review of 18-10, round 2, M-A) Happens if the file could not be
        /// read after the retries or if it was unreadable and could not be set aside: nothing is written until restart.
        /// </summary>
        public bool SoloLectura { get { lock (_cerrojo) return _noGuardar; } }

        // Reintentos y pausa (ms) de la lectura del registro cuando el fichero está en uso (M-A). Las pruebas los bajan
        // Retries and pause (ms) for reading the registry when the file is in use (M-A). Tests lower them
        public static int ReintentosLectura = 5;
        public static int PausaLecturaMs = 100;

        // ---------------------------------------------------------------
        // Carga y guardado del fichero de registro
        // Loading and saving the registry file
        // ---------------------------------------------------------------
        /// <summary>
        /// Lee el registro del disco (convierte la versión 1); si no existe empieza vacío. Revisión del 18-10, ronda 2 (M-A):
        /// un fallo de LECTURA (fichero en uso por el antivirus o una copia de seguridad) se reintenta unas veces con una
        /// pausa corta; si sigue sin poder leerse, la sesión empieza vacía pero en solo lectura (no se guarda nada y el
        /// fichero bueno no se toca ni se aparta). Solo un JSON realmente roto se aparta como «.corrupto-<fecha>» y se
        /// empieza de cero con guardado normal.
        /// Reads the registry from disk (converts version 1); if it does not exist, it starts empty. Review of 18-10, round 2 (M-A):
        /// a READ failure (file in use by the antivirus or a backup) is retried a few times with a
        /// short pause; if it still cannot be read, the session starts empty but read-only (nothing is saved and the
        /// good file is neither touched nor set aside). Only a truly broken JSON is set aside as ".corrupto-<fecha>" and
        /// it starts from scratch with normal saving.
        /// </summary>
        public void Cargar()
        {
            lock (_cerrojo)
            {
                if (_ruta == null || !File.Exists(_ruta)) return;
                // 1) Lectura con reintentos: un fichero bloqueado NO es un registro corrupto
                // 1) Reading with retries: a locked file is NOT a corrupt registry
                string texto = null;
                Exception fallo = null;
                int intentos = Math.Max(1, ReintentosLectura);
                for (int i = 0; i < intentos && texto == null; i++)
                {
                    try { texto = File.ReadAllText(_ruta); }
                    catch (FileNotFoundException) { return; }                 // desapareció entre medias: como si no existiera
                                                                              // it disappeared in the meantime: treat as if it did not exist
                    catch (DirectoryNotFoundException) { return; }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        fallo = e;
                        if (i + 1 < intentos) System.Threading.Thread.Sleep(Math.Max(0, PausaLecturaMs));
                    }
                    // Cualquier otro fallo de lectura (ruta no admitida, seguridad...): no se reintenta, pero tampoco sale
                    // de Cargar (nunca lanza) y se trata igual que el bloqueo: solo lectura, sin tocar el fichero
                    // Any other read failure (unsupported path, security...): not retried, but it does not escape
                    // from Cargar either (never throws) and is treated like the lock: read-only, file untouched
                    catch (Exception e) { fallo = e; break; }
                }
                if (texto == null)
                {
                    // Sigue bloqueado: modo solo lectura. Registro vacío en memoria, pero sin escribir en esta sesión
                    // Still locked: read-only mode. Empty registry in memory, but nothing written in this session
                    _noGuardar = true;
                    _datos = new Datos();
                    _guids.Clear();
                    Avisar?.Invoke($"No se pudo leer el registro de unidades nuevas tras {intentos} intentos ({fallo?.Message}): " +
                                   "en esta sesión se empieza vacío y NO se guarda (solo lectura), para no pisar el fichero bueno. " +
                                   "Las unidades nuevas de saves anteriores se verán como su base hasta reiniciar el juego.");
                    return;
                }
                // 2) Interpretación del JSON: solo aquí un fallo significa registro corrupto
                // 2) JSON parsing: only here does a failure mean a corrupt registry
                try
                {
                    _datos = JsonSerializer.Deserialize<Datos>(texto) ?? new Datos();
                    Normalizar(_datos);
                    bool eraV1 = _datos.Version < 2;
                    // Versión 1 -> 2: todo lo antiguo era facción A (900)
                    // Version 1 -> 2: everything old was faction A (900)
                    foreach (string g in _datos.Guids) if (!string.IsNullOrEmpty(g)) _datos.GuidsPorId[g] = IdLegado;
                    foreach (int i in _datos.IndicesOfertas) { _datos.Ofertas[i.ToString()] = IdLegado; _datos.OfertasInstantanea[i.ToString()] = IdLegado; }
                    foreach (string l in _datos.ListasMarcadas) AnadirALista(_datos, l, IdLegado);
                    _datos.Guids.Clear(); _datos.IndicesOfertas.Clear(); _datos.ListasMarcadas.Clear();
                    _guids = new Dictionary<string, int>(_datos.GuidsPorId, StringComparer.OrdinalIgnoreCase);
                    if (eraV1)
                    {
                        // Copia del formato antiguo (no se borra nunca) y se reescribe ya en versión 2
                        // Copy of the old format (never deleted) and it is rewritten as version 2
                        try { File.Copy(_ruta, _ruta + ".v1-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), false); } catch { }
                        _datos.Version = 2;
                        Guardar();
                    }
                }
                catch (Exception e)
                {
                    // Registro ilegible: se aparta con otro nombre (no se borra) y se empieza de cero. Revisión del 18-10
                    // (L-18): si ni siquiera se puede apartar, en esta sesión no se guarda (no se pisa el original)
                    // Unreadable registry: set aside under another name (not deleted) and start from scratch. Review of 18-10
                    // (L-18): if it cannot even be set aside, nothing is saved in this session (the original is not overwritten)
                    try { File.Copy(_ruta, _ruta + ".corrupto-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true); }
                    catch (Exception c)
                    {
                        _noGuardar = true;
                        Avisar?.Invoke("No se pudo apartar el registro ilegible (" + c.Message + "): en esta sesión no se guardará para no pisarlo.");
                    }
                    Avisar?.Invoke("Registro de unidades nuevas ilegible, se empieza vacío: " + e.Message);
                    _datos = new Datos();
                    _guids.Clear();
                }
            }
        }

        // Rellena las colecciones que falten (JSON antiguo o editado a mano)
        // Fills in any missing collections (old or hand-edited JSON)
        private static void Normalizar(Datos d)
        {
            d.Guids ??= new List<string>();
            d.IndicesOfertas ??= new List<int>();
            d.ListasMarcadas ??= new List<string>();
            d.GuidsPorId ??= new Dictionary<string, int>();
            d.Ofertas ??= new Dictionary<string, int>();
            d.OfertasInstantanea ??= new Dictionary<string, int>();
            d.ListasPorId ??= new Dictionary<string, List<int>>();
            d.RazasDisco ??= new Dictionary<string, RazaApuntada>();
            d.ObjetosPosicion ??= new Dictionary<string, Dictionary<string, ObjetoApuntado>>();
            d.ObjetosListas ??= new Dictionary<string, List<int>>();
            d.ObjetosPartida ??= new Dictionary<string, string>();
            d.HeroesDisco ??= new Dictionary<string, HeroeApuntado>();
            d.EntradasHeroes ??= new Dictionary<string, string>();
            d.EntradasUnidades ??= new Dictionary<string, string>();
            d.PartidasModificadas ??= new List<string>();
            d.ReglaActos ??= new Dictionary<string, int>();
            d.LogrosBloqueadosMoral ??= new List<int>();
            d.HechizosDisco ??= new Dictionary<string, HechizosApuntados>();
            d.HechizosLanzados ??= new List<int>();
            foreach (var h in d.HechizosDisco.Values)
            {
                if (h == null) continue;
                h.Posiciones ??= new Dictionary<string, int>();
                h.Entradas ??= new Dictionary<string, string>();
            }
            // 0.7.5
            d.OfertasPorRanura ??= new Dictionary<string, Dictionary<string, int>>();
            d.HistorialPartidas ??= new Dictionary<string, PartidaApuntada>();
            d.BatallasPartida ??= new Dictionary<string, PartidaApuntada>();
            foreach (var p in d.HistorialPartidas.Values.Concat(d.BatallasPartida.Values))
            {
                if (p == null) continue;
                p.Razas ??= new Dictionary<string, int>();
                p.Senores ??= new Dictionary<string, int>();
            }
        }

        private static bool AnadirALista(Datos d, string clave, int id)
        {
            if (!d.ListasPorId.TryGetValue(clave, out var lista)) { lista = new List<int>(); d.ListasPorId[clave] = lista; }
            if (lista.Contains(id)) return false;
            lista.Add(id);
            return true;
        }

        // ---------------------------------------------------------------
        // Lotes: varios cambios, una sola escritura
        // Batches: several changes, a single write
        // ---------------------------------------------------------------
        /// <summary>Empieza un lote: los cambios no se escriben hasta TerminarLote (se pueden anidar).</summary>
        /// Starts a batch: changes are not written until TerminarLote (they can be nested).
        public void IniciarLote() { lock (_cerrojo) _lote++; }

        /// <summary>Termina un lote: si hubo cambios y era el último anidado, escribe el registro una vez.</summary>
        /// Ends a batch: if there were changes and it was the last nested one, writes the registry once.
        public void TerminarLote()
        {
            lock (_cerrojo)
            {
                if (_lote > 0) _lote--;
                if (_lote == 0 && _sucio) { _sucio = false; Guardar(); }
            }
        }

        /// <summary>Número de escrituras del fichero hechas (para las pruebas).</summary>
        /// Number of file writes done (for tests).
        public int Escrituras { get; private set; }

        // Un cambio: se guarda ya o, dentro de un lote, al terminarlo (llamar con el cerrojo tomado)
        // A change: saved immediately or, inside a batch, when it ends (call with the lock held)
        private void Cambiado()
        {
            if (_lote > 0) _sucio = true; else Guardar();
        }

        // Escribe el registro de forma atómica (fichero temporal + reemplazo)
        // Writes the registry atomically (temporary file + replace)
        private void Guardar()
        {
            Escrituras++;
            if (_ruta == null || _noGuardar) return;
            try
            {
                _datos.GuidsPorId = new Dictionary<string, int>(_guids);
                string tmp = _ruta + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(_datos, new JsonSerializerOptions { WriteIndented = true }));
                if (File.Exists(_ruta)) File.Replace(tmp, _ruta, null); else File.Move(tmp, _ruta);
            }
            catch (Exception e) { Avisar?.Invoke("No se pudo guardar el registro de unidades nuevas: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // Consultas y cambios (todas con cerrojo)
        // Queries and changes (all with lock)
        // ---------------------------------------------------------------
        /// <summary>True si no hay nada apuntado (atajo para no tocar el JSON al leer).</summary>
        /// True if nothing is recorded (shortcut to avoid touching the JSON when reading).
        // Revisión del 18-10 (M-1): también cuentan las bajas históricas apuntadas (si no, DesdeDisco no las devolvería)
        // 0.7.5: y las ofertas apuntadas por ranura con partida
        // Review of 18-10 (M-1): recorded historical casualties also count (otherwise DesdeDisco would not return them)
        // 0.7.5: and so do the offers recorded per slot with run
        public bool EstaVacio { get { lock (_cerrojo) return _guids.Count == 0 && _datos.Ofertas.Count == 0 && _datos.OfertasInstantanea.Count == 0 && _datos.ListasPorId.Count == 0 && _datos.EntradasUnidades.Count == 0 && _datos.OfertasPorRanura.Count == 0; } }

        public bool TieneListasMarcadas { get { lock (_cerrojo) return _datos.ListasPorId.Count > 0; } }

        public int NumeroGuids { get { lock (_cerrojo) return _guids.Count; } }

        /// <summary>Apunta que la escuadra <paramref name="guid"/> es la unidad <paramref name="id"/> (guarda solo si cambia).</summary>
        /// Records that the squad <paramref name="guid"/> is the unit <paramref name="id"/> (saves only if it changes).
        public void ApuntarGuid(string guid, int id)
        {
            lock (_cerrojo)
            {
                if (_guids.TryGetValue(guid, out int ya) && ya == id) return;
                _guids[guid] = id;
                Cambiado();
            }
        }

        /// <summary>Id real apuntado para una escuadra, o -1 si no está.</summary>
        /// Real id recorded for a squad, or -1 if there is none.
        public int IdDeGuid(string guid)
        {
            lock (_cerrojo) return guid != null && _guids.TryGetValue(guid, out int id) ? id : -1;
        }

        public bool ContieneGuid(string guid) => IdDeGuid(guid) >= 0;

        /// <summary>Quita una escuadra apuntada (solo para limpiezas a mano o de pruebas). true si estaba.</summary>
        /// Removes a tracked squad (only for manual or test cleanups). true if it was there.
        public bool QuitarGuid(string guid)
        {
            lock (_cerrojo)
            {
                if (guid == null || !_guids.Remove(guid)) return false;
                Cambiado();
                return true;
            }
        }

        /// <summary>
        /// Sustituye las posiciones de las ofertas que eran unidades nuevas (guarda solo si cambia). El save normal y la
        /// instantánea del nodo llevan cada uno las suyas (<paramref name="instantanea"/>).
        /// Replaces the positions of the offers that were new units (saves only if something changes). The normal save and the
        /// node snapshot each carry their own (<paramref name="instantanea"/>).
        /// </summary>
        public void FijarOfertas(Dictionary<int, int> porPosicion, bool instantanea = false)
        {
            lock (_cerrojo)
            {
                var nuevo = new Dictionary<string, int>();
                foreach (var kv in porPosicion) nuevo[kv.Key.ToString()] = kv.Value;
                if (DiccionariosIguales(instantanea ? _datos.OfertasInstantanea : _datos.Ofertas, nuevo)) return;
                if (instantanea) _datos.OfertasInstantanea = nuevo; else _datos.Ofertas = nuevo;
                Cambiado();
            }
        }

        /// <summary>Posición -> id de las ofertas apuntadas del save normal o de la instantánea.</summary>
        /// Position -> id of the tracked offers from the normal save or the snapshot.
        public Dictionary<int, int> Ofertas(bool instantanea = false)
        {
            lock (_cerrojo)
            {
                var r = new Dictionary<int, int>();
                foreach (var kv in instantanea ? _datos.OfertasInstantanea : _datos.Ofertas) if (int.TryParse(kv.Key, out int i)) r[i] = kv.Value;
                return r;
            }
        }

        /// <summary>
        /// 0.7.5: sustituye las ofertas apuntadas de una ranura con partida y slot (SaneadoSaves.ContextoSave). Vacío = se
        /// olvida esa ranura (solo esa: las de otras partidas y slots no se tocan). Guarda solo si cambia.
        /// 0.7.5: replaces the tracked offers of a slot with run and slot (SaneadoSaves.ContextoSave). Empty = that slot is
        /// forgotten (only that one: those of other runs and slots are not touched). Saves only if something changes.
        /// </summary>
        public void FijarOfertas(string ranura, Dictionary<int, int> porPosicion)
        {
            if (string.IsNullOrEmpty(ranura)) return;
            lock (_cerrojo)
            {
                if (porPosicion == null || porPosicion.Count == 0)
                {
                    if (_datos.OfertasPorRanura.Remove(ranura)) Cambiado();
                    return;
                }
                var nuevo = new Dictionary<string, int>();
                foreach (var kv in porPosicion) nuevo[kv.Key.ToString()] = kv.Value;
                if (_datos.OfertasPorRanura.TryGetValue(ranura, out var ya) && ya != null && DiccionariosIguales(ya, nuevo)) return;
                _datos.OfertasPorRanura[ranura] = nuevo;
                Cambiado();
            }
        }

        /// <summary>0.7.5: posición -> id de las ofertas apuntadas en una ranura con partida y slot (vacío si nada).</summary>
        /// 0.7.5: position -> id of the tracked offers in a slot with run and slot (empty if none).
        public Dictionary<int, int> Ofertas(string ranura)
        {
            lock (_cerrojo)
            {
                var r = new Dictionary<int, int>();
                if (ranura != null && _datos.OfertasPorRanura.TryGetValue(ranura, out var d) && d != null)
                    foreach (var kv in d) if (int.TryParse(kv.Key, out int i)) r[i] = kv.Value;
                return r;
            }
        }

        /// <summary>Anota que una lista de colección contenía la unidad <paramref name="id"/>.</summary>
        /// Records that a collection list contained the unit <paramref name="id"/>.
        public void MarcarLista(string clave, int id)
        {
            lock (_cerrojo) { if (AnadirALista(_datos, clave, id)) Cambiado(); }
        }

        /// <summary>Ids nuevos que contenía una lista de colección (vacío si ninguno).</summary>
        /// New ids that a collection list contained (empty if none).
        public List<int> IdsDeLista(string clave)
        {
            lock (_cerrojo) return _datos.ListasPorId.TryGetValue(clave, out var l) ? new List<int>(l) : new List<int>();
        }

        // ---------------------------------------------------------------
        // Razas nuevas (F2): ciudad actual y campo de batalla preparado
        // New races (F2): current city and prepared battlefield
        // ---------------------------------------------------------------
        /// <summary>True si hay alguna raza nueva apuntada (atajo para no tocar el JSON al leer).</summary>
        /// True if there is any tracked new race (shortcut to avoid touching the JSON when reading).
        public bool TieneRazas { get { lock (_cerrojo) return _datos.RazasDisco.Count > 0; } }

        /// <summary>
        /// Apunta la raza nueva real de una ranura con su firma; con <paramref name="raza"/> &lt; 0 la olvida (el save ya
        /// no tiene una raza nueva en esa ranura). Guarda solo si cambia.
        /// Tracks the actual new race of a slot with its signature; with <paramref name="raza"/> &lt; 0 it forgets it (the save no
        /// longer has a new race in that slot). Saves only if something changes.
        /// </summary>
        public void FijarRaza(string ranura, int raza, string firma)
        {
            lock (_cerrojo)
            {
                if (raza < 0)
                {
                    if (_datos.RazasDisco.Remove(ranura)) Cambiado();
                    return;
                }
                if (_datos.RazasDisco.TryGetValue(ranura, out var ya) && ya.Raza == raza && ya.Firma == firma) return;
                _datos.RazasDisco[ranura] = new RazaApuntada { Raza = raza, Firma = firma ?? "" };
                Cambiado();
            }
        }

        /// <summary>Raza nueva apuntada en una ranura y su firma (false si no hay ninguna).</summary>
        /// New race tracked in a slot and its signature (false if there is none).
        public bool RazaDe(string ranura, out int raza, out string firma)
        {
            lock (_cerrojo)
            {
                raza = -1; firma = null;
                if (!_datos.RazasDisco.TryGetValue(ranura, out var r) || r == null) return false;
                raza = r.Raza; firma = r.Firma ?? "";
                return true;
            }
        }

        // ---------------------------------------------------------------
        // Objetos nuevos (0.6.0)
        // New gear (0.6.0)
        // ---------------------------------------------------------------
        /// <summary>True si hay algún objeto nuevo apuntado (atajo para no tocar el JSON al leer).</summary>
        /// True if there is any tracked new gear (shortcut to avoid touching the JSON when reading).
        public bool TieneObjetos { get { lock (_cerrojo) return _datos.ObjetosPosicion.Count > 0 || _datos.ObjetosListas.Count > 0; } }

        /// <summary>
        /// Sustituye los objetos nuevos apuntados de una lista con posición (posición -> real y escrito). Vacío = la lista ya
        /// no tiene objetos nuevos: se olvida la ranura. Guarda solo si cambia.
        /// Replaces the tracked new gear of a list with position (position -> actual and written). Empty = the list no
        /// longer has new gear: the slot is forgotten. Saves only if something changes.
        /// </summary>
        public void FijarObjetosPosicion(string ranura, Dictionary<int, (int real, int escrito)> porPosicion, string partida = null)
        {
            lock (_cerrojo)
            {
                _datos.ObjetosPosicion.TryGetValue(ranura, out var ya);
                if (porPosicion.Count == 0)
                {
                    bool quitado = ya != null && _datos.ObjetosPosicion.Remove(ranura);
                    if (_datos.ObjetosPartida.Remove(ranura) || quitado) Cambiado();
                    return;
                }
                // 0.7.1: la partida a la que pertenece lo apuntado (si cambia, también hay que guardar)
                // 0.7.1: the run the tracked data belongs to (if it changes, it must also be saved)
                bool partidaCambia = FijarPartidaObjetos(ranura, partida);
                bool igual = !partidaCambia && ya != null && ya.Count == porPosicion.Count;
                if (igual)
                    foreach (var kv in porPosicion)
                        if (!ya.TryGetValue(kv.Key.ToString(), out var o) || o == null || o.Real != kv.Value.real || o.Escrito != kv.Value.escrito) { igual = false; break; }
                if (igual) return;
                var nuevo = new Dictionary<string, ObjetoApuntado>();
                foreach (var kv in porPosicion) nuevo[kv.Key.ToString()] = new ObjetoApuntado { Real = kv.Value.real, Escrito = kv.Value.escrito };
                _datos.ObjetosPosicion[ranura] = nuevo;
                Cambiado();
            }
        }

        /// <summary>Posición -> (real, escrito) de una lista con posición (vacío si no hay nada apuntado).</summary>
        /// Position -> (actual, written) of a list with position (empty if nothing is tracked).
        public Dictionary<int, (int real, int escrito)> ObjetosPosicion(string ranura)
        {
            lock (_cerrojo)
            {
                var r = new Dictionary<int, (int, int)>();
                if (_datos.ObjetosPosicion.TryGetValue(ranura, out var d) && d != null)
                    foreach (var kv in d) if (kv.Value != null && int.TryParse(kv.Key, out int i)) r[i] = (kv.Value.Real, kv.Value.Escrito);
                return r;
            }
        }

        /// <summary>Sustituye los ids nuevos quitados de una lista sin posición (SoldGear, brokenGear). Vacío = se olvida.</summary>
        /// Replaces the removed new ids of a list without position (SoldGear, brokenGear). Empty = forgotten.
        public void FijarObjetosLista(string ranura, List<int> ids, string partida = null)
        {
            lock (_cerrojo)
            {
                _datos.ObjetosListas.TryGetValue(ranura, out var ya);
                if (ids.Count == 0)
                {
                    bool quitado = ya != null && _datos.ObjetosListas.Remove(ranura);
                    if (_datos.ObjetosPartida.Remove(ranura) || quitado) Cambiado();
                    return;
                }
                bool partidaCambia = FijarPartidaObjetos(ranura, partida);
                if (!partidaCambia && ya != null && ya.Count == ids.Count && new HashSet<int>(ya).SetEquals(ids)) return;
                _datos.ObjetosListas[ranura] = new List<int>(ids);
                Cambiado();
            }
        }

        /// <summary>Añade ids nuevos quitados de una lista de colección (solo crece, como las de unidades).</summary>
        /// Adds removed new ids of a collection list (only grows, like those of units).
        public void AnadirObjetosLista(string ranura, IEnumerable<int> ids)
        {
            lock (_cerrojo)
            {
                if (!_datos.ObjetosListas.TryGetValue(ranura, out var lista) || lista == null) { lista = new List<int>(); }
                bool cambio = false;
                foreach (int id in ids) if (!lista.Contains(id)) { lista.Add(id); cambio = true; }
                if (!cambio) return;
                _datos.ObjetosListas[ranura] = lista;
                Cambiado();
            }
        }

        /// <summary>Ids nuevos quitados de una lista sin posición o de colección (vacío si ninguno).</summary>
        /// Removed new ids of a list without position or of a collection (empty if none).
        public List<int> ObjetosLista(string ranura)
        {
            lock (_cerrojo) return _datos.ObjetosListas.TryGetValue(ranura, out var l) && l != null ? new List<int>(l) : new List<int>();
        }

        // Pone la partida de una ranura de objetos (null = sin ancla: se quita). true si cambió (llamar con el cerrojo tomado)
        // Sets the run of a gear slot (null = no anchor: it is removed). true if it changed (call with the lock held)
        private bool FijarPartidaObjetos(string ranura, string partida)
        {
            if (string.IsNullOrEmpty(partida)) return _datos.ObjetosPartida.Remove(ranura);
            if (_datos.ObjetosPartida.TryGetValue(ranura, out string ya) && ya == partida) return false;
            _datos.ObjetosPartida[ranura] = partida;
            return true;
        }

        /// <summary>
        /// Partida (runId) a la que pertenece lo apuntado de una ranura de objetos; null si no tiene ancla (registros de la
        /// 0.7.0 o textos sin partida).
        /// Run (runId) the tracked data of a gear slot belongs to; null if it has no anchor (records from
        /// 0.7.0 or texts without a run).
        /// </summary>
        public string PartidaObjetos(string ranura)
        {
            lock (_cerrojo) return _datos.ObjetosPartida.TryGetValue(ranura, out string p) && !string.IsNullOrEmpty(p) ? p : null;
        }

        // ---------------------------------------------------------------
        // Héroes nuevos (0.7.1): héroe del jugador y señor de la guerra del save de campaña
        // New heroes (0.7.1): player hero and warlord of the campaign save
        // ---------------------------------------------------------------
        /// <summary>True si hay algún héroe nuevo apuntado.</summary>
        /// True if there is any tracked new hero.
        public bool TieneHeroes { get { lock (_cerrojo) return _datos.HeroesDisco.Count > 0; } }

        /// <summary>
        /// Apunta el héroe nuevo real de una ranura, el prestado que se escribió y la partida; con <paramref name="real"/>
        /// &lt; 0 lo olvida. Guarda solo si cambia.
        /// Tracks the actual new hero of a slot, the borrowed hero that was written and the run; with <paramref name="real"/>
        /// &lt; 0 it forgets it. Saves only if something changes.
        /// </summary>
        public void FijarHeroe(string ranura, int real, int escrito, string firma)
        {
            lock (_cerrojo)
            {
                if (real < 0)
                {
                    if (_datos.HeroesDisco.Remove(ranura)) Cambiado();
                    return;
                }
                if (_datos.HeroesDisco.TryGetValue(ranura, out var ya) && ya != null && ya.Real == real && ya.Escrito == escrito && ya.Firma == (firma ?? "")) return;
                _datos.HeroesDisco[ranura] = new HeroeApuntado { Real = real, Escrito = escrito, Firma = firma ?? "" };
                Cambiado();
            }
        }

        /// <summary>Héroe nuevo apuntado en una ranura, el prestado escrito y su partida (false si no hay ninguno).</summary>
        /// New hero tracked in a slot, the borrowed hero written and its run (false if there is none).
        public bool HeroeDe(string ranura, out int real, out int escrito, out string firma)
        {
            lock (_cerrojo)
            {
                real = -1; escrito = -1; firma = null;
                if (!_datos.HeroesDisco.TryGetValue(ranura, out var h) || h == null) return false;
                real = h.Real; escrito = h.Escrito; firma = string.IsNullOrEmpty(h.Firma) ? null : h.Firma;
                return true;
            }
        }

        // ---------------------------------------------------------------
        // Victorias de héroes nuevos en el save del jugador (0.7.2, revisión): entradas de HeroDifficultiesCompleted y
        // HeroLastDifficulties con un héroe 9xx. Se guarda el objeto JSON tal cual, por lista y héroe; solo crece (una
        // entrada nueva del mismo héroe sustituye a la anterior y nunca se borra por no venir en un texto)
        // Victories of new heroes in the player's save (0.7.2, review): entries of HeroDifficultiesCompleted and
        // HeroLastDifficulties with a 9xx hero. The JSON object is stored as is, per list and hero; it only grows (a new
        // entry for the same hero replaces the previous one and is never deleted for being absent from a text)
        // ---------------------------------------------------------------
        /// <summary>True si hay alguna entrada de héroe nuevo apuntada.</summary>
        /// True if there is any tracked new hero entry.
        public bool TieneEntradasHeroes { get { lock (_cerrojo) return _datos.EntradasHeroes.Count > 0; } }

        /// <summary>Apunta (o sustituye) el objeto JSON de la entrada de <paramref name="heroe"/> en la lista <paramref name="lista"/>.</summary>
        /// Tracks (or replaces) the JSON object of the entry of <paramref name="heroe"/> in the list <paramref name="lista"/>.
        public void FijarEntradaHeroe(string lista, int heroe, string objeto)
        {
            if (string.IsNullOrEmpty(lista) || string.IsNullOrEmpty(objeto)) return;
            lock (_cerrojo)
            {
                string clave = lista + ":" + heroe;
                if (_datos.EntradasHeroes.TryGetValue(clave, out string ya) && ya == objeto) return;
                _datos.EntradasHeroes[clave] = objeto;
                Cambiado();
            }
        }

        /// <summary>Entradas apuntadas de una lista: héroe -> objeto JSON (vacío si no hay ninguna).</summary>
        /// Tracked entries of a list: hero -> JSON object (empty if there are none).
        public Dictionary<int, string> EntradasHeroes(string lista)
        {
            var r = new Dictionary<int, string>();
            lock (_cerrojo)
            {
                string prefijo = lista + ":";
                foreach (var kv in _datos.EntradasHeroes)
                    if (kv.Key.StartsWith(prefijo, StringComparison.Ordinal) && int.TryParse(kv.Key.Substring(prefijo.Length), out int h) && !string.IsNullOrEmpty(kv.Value))
                        r[h] = kv.Value;
            }
            return r;
        }

        // ---------------------------------------------------------------
        // Hechizos propios quitados de los saves (0.7.4): por ranura (posiciones y entradas con su firma) y los lanzados
        // alguna vez (solo crece)
        // Custom spells removed from the saves (0.7.4): per slot (positions and entries with their signature) and those ever
        // cast (only grows)
        // ---------------------------------------------------------------
        /// <summary>True si hay algún hechizo propio apuntado (atajo para no tocar el JSON al leer).</summary>
        /// True if there is any tracked custom spell (shortcut to avoid touching the JSON when reading).
        public bool TieneHechizos { get { lock (_cerrojo) return _datos.HechizosDisco.Count > 0 || _datos.HechizosLanzados.Count > 0; } }

        /// <summary>
        /// Lo apuntado en una ranura: firma, posición -> id y id -> objeto JSON (copias). false si la ranura no tiene nada.
        /// What is tracked in a slot: signature, position -> id and id -> JSON object (copies). false if the slot has nothing.
        /// </summary>
        public bool HechizosDe(string ranura, out string firma, out Dictionary<int, int> posiciones, out Dictionary<int, string> entradas)
        {
            firma = null;
            posiciones = new Dictionary<int, int>();
            entradas = new Dictionary<int, string>();
            lock (_cerrojo)
            {
                if (string.IsNullOrEmpty(ranura) || !_datos.HechizosDisco.TryGetValue(ranura, out var h) || h == null) return false;
                firma = string.IsNullOrEmpty(h.Firma) ? null : h.Firma;
                foreach (var kv in h.Posiciones) if (int.TryParse(kv.Key, out int p)) posiciones[p] = kv.Value;
                foreach (var kv in h.Entradas) if (int.TryParse(kv.Key, out int id) && !string.IsNullOrEmpty(kv.Value)) entradas[id] = kv.Value;
                return posiciones.Count > 0 || entradas.Count > 0;
            }
        }

        /// <summary>
        /// Fija (sustituye) lo apuntado en una ranura. Sin posiciones ni entradas, la ranura se olvida. Solo guarda si cambia.
        /// Sets (replaces) what is tracked in a slot. Without positions or entries, the slot is forgotten. Saves only if something changes.
        /// </summary>
        public void FijarHechizos(string ranura, string firma, IDictionary<int, int> posiciones, IDictionary<int, string> entradas)
        {
            if (string.IsNullOrEmpty(ranura)) return;
            lock (_cerrojo)
            {
                bool vacio = (posiciones == null || posiciones.Count == 0) && (entradas == null || entradas.Count == 0);
                if (vacio)
                {
                    if (_datos.HechizosDisco.Remove(ranura)) Cambiado();
                    return;
                }
                var nuevo = new HechizosApuntados { Firma = firma };
                if (posiciones != null) foreach (var kv in posiciones) nuevo.Posiciones[kv.Key.ToString()] = kv.Value;
                if (entradas != null) foreach (var kv in entradas) nuevo.Entradas[kv.Key.ToString()] = kv.Value;
                if (_datos.HechizosDisco.TryGetValue(ranura, out var ya) && ya != null && ya.Firma == nuevo.Firma
                    && DiccionariosIguales(ya.Posiciones, nuevo.Posiciones) && TextosIguales(ya.Entradas, nuevo.Entradas)) return;
                _datos.HechizosDisco[ranura] = nuevo;
                Cambiado();
            }
        }

        /// <summary>Hechizos propios lanzados alguna vez (spellsEverCast) quitados del save del jugador (copia ordenada).</summary>
        /// Custom spells ever cast (spellsEverCast) removed from the player's save (sorted copy).
        public List<int> HechizosLanzados { get { lock (_cerrojo) return new List<int>(_datos.HechizosLanzados); } }

        /// <summary>Añade hechizos a la lista de los lanzados alguna vez (solo crece; guarda si cambia).</summary>
        /// Adds spells to the list of those ever cast (only grows; saves if something changes).
        public void AnadirHechizosLanzados(IEnumerable<int> ids)
        {
            if (ids == null) return;
            lock (_cerrojo)
            {
                bool cambio = false;
                foreach (int id in ids) if (!_datos.HechizosLanzados.Contains(id)) { _datos.HechizosLanzados.Add(id); cambio = true; }
                if (!cambio) return;
                _datos.HechizosLanzados.Sort();
                Cambiado();
            }
        }

        // ---------------------------------------------------------------
        // Historial de partidas y registro de batallas (0.7.5): héroe del jugador, raza y señor de la guerra de cada batalla
        // que eran nuevos. Lo apuntado de una partida se FUSIONA con lo nuevo (una posición nueva sustituye a la anterior) y
        // nunca se borra por no venir en un texto: el historial de una partida terminada no cambia
        // Run history and battle log (0.7.5): player hero, race and warlord of each battle
        // that were new. What is tracked for a run is MERGED with the new data (a new position replaces the previous one) and
        // is never deleted for being absent from a text: the history of a finished run does not change
        // ---------------------------------------------------------------
        /// <summary>True si hay algo apuntado del historial o del registro de batallas.</summary>
        /// True if anything is tracked from the history or the battle log.
        public bool TieneHistorial { get { lock (_cerrojo) return _datos.HistorialPartidas.Count > 0 || _datos.BatallasPartida.Count > 0; } }

        // Diccionario de una de las dos clases de apuntes (historial del jugador o batallas de la partida)
        // Dictionary for one of the two kinds of notes (player history or battles of the run)
        private Dictionary<string, PartidaApuntada> DePartidas(bool batallas) => batallas ? _datos.BatallasPartida : _datos.HistorialPartidas;

        /// <summary>
        /// Funde en la clave <paramref name="clave"/> (runUUID del historial o ranura del registro de batallas) lo nuevo de
        /// una partida: héroe real y escrito (si <paramref name="heroe"/> &gt; 0) y razas y señores por posición. Guarda solo
        /// si cambia algo.
        /// Merges into the key <paramref name="clave"/> (runUUID of the history or slot of the battle log) the new data of
        /// a run: actual and written hero (if <paramref name="heroe"/> &gt; 0) and races and warlords by position. Saves only
        /// if something changes.
        /// </summary>
        public void FundirPartida(bool batallas, string clave, string firma, int heroe, int heroeEscrito,
                                  IDictionary<int, int> razas, IDictionary<int, int> senores)
        {
            if (string.IsNullOrEmpty(clave)) return;
            bool hay = heroe > 0 || (razas != null && razas.Count > 0) || (senores != null && senores.Count > 0);
            if (!hay) return;
            lock (_cerrojo)
            {
                var dic = DePartidas(batallas);
                bool cambio = false;
                if (!dic.TryGetValue(clave, out var p) || p == null) { p = new PartidaApuntada { Firma = firma }; dic[clave] = p; cambio = true; }
                p.Razas ??= new Dictionary<string, int>();
                p.Senores ??= new Dictionary<string, int>();
                if (heroe > 0 && (p.Heroe != heroe || p.HeroeEscrito != heroeEscrito)) { p.Heroe = heroe; p.HeroeEscrito = heroeEscrito; cambio = true; }
                if (razas != null) foreach (var kv in razas)
                    if (!p.Razas.TryGetValue(kv.Key.ToString(), out int ya) || ya != kv.Value) { p.Razas[kv.Key.ToString()] = kv.Value; cambio = true; }
                if (senores != null) foreach (var kv in senores)
                    if (!p.Senores.TryGetValue(kv.Key.ToString(), out int ya) || ya != kv.Value) { p.Senores[kv.Key.ToString()] = kv.Value; cambio = true; }
                if (cambio) Cambiado();
            }
        }

        /// <summary>Lo apuntado de una partida (copia): héroe real y escrito, razas y señores por posición. false si nada.</summary>
        /// What is tracked for a run (copy): actual and written hero, races and warlords by position. false if nothing.
        public bool PartidaDe(bool batallas, string clave, out int heroe, out int heroeEscrito, out Dictionary<int, int> razas, out Dictionary<int, int> senores)
        {
            heroe = 0; heroeEscrito = 0;
            razas = new Dictionary<int, int>();
            senores = new Dictionary<int, int>();
            if (string.IsNullOrEmpty(clave)) return false;
            lock (_cerrojo)
            {
                if (!DePartidas(batallas).TryGetValue(clave, out var p) || p == null) return false;
                heroe = p.Heroe; heroeEscrito = p.HeroeEscrito;
                foreach (var kv in p.Razas ?? new Dictionary<string, int>()) if (int.TryParse(kv.Key, out int i)) razas[i] = kv.Value;
                foreach (var kv in p.Senores ?? new Dictionary<string, int>()) if (int.TryParse(kv.Key, out int i)) senores[i] = kv.Value;
                return true;
            }
        }

        // Dos diccionarios de textos con las mismas claves y valores
        // Two text dictionaries with the same keys and values
        private static bool TextosIguales(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a) if (!b.TryGetValue(kv.Key, out string v) || v != kv.Value) return false;
            return true;
        }

        // ---------------------------------------------------------------
        // Bajas históricas de unidades nuevas en el save del jugador (revisión del 18-10, M-1): entradas de
        // UnitNameHistoricalKillStore con una unidad 9xx. Igual que las victorias de los héroes: el objeto JSON tal cual, por
        // lista y unidad; una entrada nueva de la misma unidad sustituye a la anterior y nunca se borra por no venir
        // Historical kills of new units in the player's save (review of 18-10, M-1): entries of
        // UnitNameHistoricalKillStore with a 9xx unit. Same as hero victories: the JSON object as is, per
        // list and unit; a new entry for the same unit replaces the previous one and is never deleted for being absent
        // ---------------------------------------------------------------
        /// <summary>Apunta (o sustituye) el objeto JSON de la entrada de la unidad <paramref name="unidad"/> en <paramref name="lista"/>.</summary>
        /// Tracks (or replaces) the JSON object of the entry of the unit <paramref name="unidad"/> in <paramref name="lista"/>.
        public void FijarEntradaUnidad(string lista, int unidad, string objeto)
        {
            if (string.IsNullOrEmpty(lista) || string.IsNullOrEmpty(objeto)) return;
            lock (_cerrojo)
            {
                string clave = lista + ":" + unidad;
                if (_datos.EntradasUnidades.TryGetValue(clave, out string ya) && ya == objeto) return;
                _datos.EntradasUnidades[clave] = objeto;
                Cambiado();
            }
        }

        /// <summary>Entradas apuntadas de una lista: unidad -> objeto JSON (vacío si no hay ninguna).</summary>
        /// Tracked entries of a list: unit -> JSON object (empty if there are none).
        public Dictionary<int, string> EntradasUnidades(string lista)
        {
            var r = new Dictionary<int, string>();
            lock (_cerrojo)
            {
                string prefijo = lista + ":";
                foreach (var kv in _datos.EntradasUnidades)
                    if (kv.Key.StartsWith(prefijo, StringComparison.Ordinal) && int.TryParse(kv.Key.Substring(prefijo.Length), out int u) && !string.IsNullOrEmpty(kv.Value))
                        r[u] = kv.Value;
            }
            return r;
        }

        // ---------------------------------------------------------------
        // Partidas jugadas con módulos de Zero (revisión del 18-10, M-2): bloqueo de clasificaciones y telemetría por
        // partida, no solo por sesión
        // Runs played with Zero modules (review of 18-10, M-2): leaderboard lock and telemetry per
        // run, not just per session
        // ---------------------------------------------------------------
        /// <summary>0.7.6: apunta un logro que no se concedió por una batalla con «Impertérritos». Solo crece.</summary>
        /// 0.7.6: tracks an achievement that was not granted because of a battle with «Impertérritos». Only grows.
        public void ApuntarLogroBloqueadoMoral(int logro)
        {
            lock (_cerrojo)
            {
                if (_datos.LogrosBloqueadosMoral.Contains(logro)) return;
                _datos.LogrosBloqueadosMoral.Add(logro);
                Cambiado();
            }
        }

        /// <summary>0.7.7: icono elegido para el estandarte (null = el de serie).</summary>
        /// 0.7.7: icon chosen for the banner (null = the default one).
        public string IconoEstandarte { get { lock (_cerrojo) return _datos.IconoEstandarte; } }

        /// <summary>0.7.7: guarda el icono elegido para el estandarte (null = volver al de serie).</summary>
        /// 0.7.7: saves the icon chosen for the banner (null = back to the default one).
        public void FijarIconoEstandarte(string codigo)
        {
            lock (_cerrojo)
            {
                if (string.Equals(_datos.IconoEstandarte, codigo, StringComparison.Ordinal)) return;
                _datos.IconoEstandarte = codigo;
                Cambiado();
            }
        }

        /// <summary>0.7.6: ¿se bloqueó este logro por una batalla con «Impertérritos»?</summary>
        /// 0.7.6: was this achievement blocked because of a battle with «Impertérritos»?
        public bool LogroBloqueadoMoral(int logro) { lock (_cerrojo) return _datos.LogrosBloqueadosMoral.Contains(logro); }

        /// <summary>0.7.6: regla del sorteo de facciones enemigas apuntada para la partida (0 si no hay).</summary>
        /// 0.7.6: enemy faction draw rule tracked for the run (0 if none).
        public int ReglaActosDe(string partida)
        {
            if (string.IsNullOrEmpty(partida)) return 0;
            lock (_cerrojo) return _datos.ReglaActos.TryGetValue(partida, out int r) ? r : 0;
        }

        /// <summary>0.7.6: apunta la regla del sorteo de facciones enemigas de la partida (solo la primera vez).</summary>
        /// 0.7.6: tracks the enemy faction draw rule of the run (only the first time).
        public void FijarReglaActos(string partida, int regla)
        {
            if (string.IsNullOrEmpty(partida) || regla <= 0) return;
            lock (_cerrojo)
            {
                if (_datos.ReglaActos.ContainsKey(partida)) return;
                _datos.ReglaActos[partida] = regla;
                Cambiado();
            }
        }

        /// <summary>Apunta que la partida <paramref name="partida"/> (runId o firma por semilla) se jugó con módulos. Solo crece.</summary>
        /// Tracks that the run <paramref name="partida"/> (runId or seed signature) was played with modules. Only grows.
        public void MarcarPartidaModificada(string partida)
        {
            if (string.IsNullOrEmpty(partida)) return;
            lock (_cerrojo)
            {
                if (_datos.PartidasModificadas.Contains(partida)) return;
                _datos.PartidasModificadas.Add(partida);
                Cambiado();
            }
        }

        /// <summary>
        /// ¿Hay algo en el registro que ate la partida <paramref name="partida"/> a los módulos? Sí si está marcada como
        /// modificada, o si un héroe nuevo o unos objetos nuevos apuntados son de esa partida (registros de antes de esta
        /// versión, que aún no marcaban partidas). false con una partida vacía.
        /// Does the registry contain anything that ties the run <paramref name="partida"/> to the modules? Yes if it is marked as
        /// modified, or if a new hero or some new gear on record belong to that run (registries from before this
        /// version, which did not mark runs yet). false with an empty run.
        /// </summary>
        public bool PartidaAnclada(string partida)
        {
            if (string.IsNullOrEmpty(partida)) return false;
            lock (_cerrojo)
            {
                if (_datos.PartidasModificadas.Contains(partida)) return true;
                foreach (var h in _datos.HeroesDisco.Values) if (h != null && h.Firma == partida) return true;
                foreach (string p in _datos.ObjetosPartida.Values) if (p == partida) return true;
                // 0.7.5: hechizos propios de esa partida, su entrada del historial y su registro de batallas
                // 0.7.5: spells specific to that run, its history entry and its battle log
                foreach (var h in _datos.HechizosDisco.Values) if (h != null && h.Firma == partida) return true;
                if (_datos.HistorialPartidas.ContainsKey(partida)) return true;
                foreach (var b in _datos.BatallasPartida.Values) if (b != null && b.Firma == partida) return true;
                return false;
            }
        }

        /// <summary>
        /// Decisión del bloqueo online (clasificaciones y telemetría): con la opción encendida, se bloquea si la sesión
        /// aplica algún módulo o si el registro ata la partida <paramref name="partida"/> a los módulos. Función pura para
        /// las pruebas; la usa Saves.AntesDeSubmit (la telemetría usa BloquearTelemetria y BloquearEnvioTelemetria).
        /// Online block decision (leaderboards and telemetry): with the option on, it blocks if the session
        /// applies any module or if the registry ties the run <paramref name="partida"/> to the modules. Pure function for
        /// tests; used by Saves.AntesDeSubmit (telemetry uses BloquearTelemetria and BloquearEnvioTelemetria).
        /// </summary>
        public static bool BloquearOnline(bool opcionBloquear, bool modulosAplicados, RegistroGuardias registro, string partida) =>
            opcionBloquear && (modulosAplicados || (registro != null && registro.PartidaAnclada(partida)));

        // ---------------------------------------------------------------
        // Telemetría: informes de fallo frente a eventos de partida (revisión externa del 18-10, P-B)
        // Telemetry: bug reports versus run events (external review of 18-10, P-B)
        // ---------------------------------------------------------------
        /// <summary>
        /// Eventos de telemetría del juego que son informes de fallo y no datos de la partida: «exceptionSeen» (la primera
        /// vez por sesión que sale una excepción nueva, ExceptionTracker.SendPending -> GameEventTracker.ExceptionSeen) y
        /// «bugReportSubmitted» (aviso de que el jugador mandó un informe desde «Report a bug», BugReportBridge.OnReportSent
        /// -> GameEventTracker.BugReportSubmitted). Nombres exactos, comprobados en el código del juego
        /// (Analytics\GameEventTracker.cs) el 18-10-2026. Lista cerrada: un evento nuevo de una actualización cuenta como
        /// de partida hasta que se añada aquí.
        /// Game telemetry events that are crash reports and not run data: «exceptionSeen» (the first
        /// time per session that a new exception occurs, ExceptionTracker.SendPending -> GameEventTracker.ExceptionSeen) and
        /// «bugReportSubmitted» (notice that the player sent a report from «Report a bug», BugReportBridge.OnReportSent
        /// -> GameEventTracker.BugReportSubmitted). Exact names, checked in the game code
        /// (Analytics\GameEventTracker.cs) on 18-10-2026. Closed list: a new event from an update counts as
        /// a run event until it is added here.
        /// </summary>
        public static readonly string[] EventosDeFallo = { "exceptionSeen", "bugReportSubmitted" };

        /// <summary>¿Es <paramref name="evento"/> un informe de fallo del juego? Nombre exacto (distingue mayúsculas); null o vacío = no.</summary>
        /// Is <paramref name="evento"/> a game crash report? Exact name (case-sensitive); null or empty = no.
        public static bool EsInformeDeFallo(string evento) =>
            !string.IsNullOrEmpty(evento) && Array.IndexOf(EventosDeFallo, evento) >= 0;

        /// <summary>
        /// Decisión de la telemetría al APUNTAR un evento (AnalyticsService.Record y HttpAnalyticsBackend.Record). Con la
        /// opción encendida:
        ///   - con algún módulo de Zero aplicado en la sesión se bloquea todo, también los informes de fallo (podrían ser
        ///     fallos del mod);
        ///   - sin módulos aplicados solo se bloquea si la partida en curso está atada a los módulos en el registro
        ///     (<see cref="PartidaAnclada"/>), y aun así los informes de fallo del juego (<see cref="EsInformeDeFallo"/>)
        ///     pasan: el núcleo no tapa los fallos del juego. Un evento sin nombre cuenta como de partida (fallo cerrado).
        /// Función pura para las pruebas; la usa Saves.AntesDeEventoTelemetria. <paramref name="partida"/> se pide solo si
        /// hace falta (leerla supone mirar el save del disco).
        /// Telemetry decision when RECORDING an event (AnalyticsService.Record and HttpAnalyticsBackend.Record). With the
        /// option on:
        ///   - with any Zero module applied in the session, everything is blocked, including crash reports (they could be
        ///     mod failures);
        ///   - with no modules applied, it only blocks if the current run is tied to the modules in the registry
        ///     (<see cref="PartidaAnclada"/>), and even then the game's crash reports (<see cref="EsInformeDeFallo"/>)
        ///     go through: the core does not hide the game's crashes. An event with no name counts as a run event (fail closed).
        /// Pure function for tests; used by Saves.AntesDeEventoTelemetria. <paramref name="partida"/> is requested only if
        /// needed (reading it means looking at the save on disk).
        /// </summary>
        public static bool BloquearTelemetria(bool opcionBloquear, bool modulosAplicados, RegistroGuardias registro, Func<string> partida, string evento)
        {
            if (!opcionBloquear) return false;
            if (modulosAplicados) return true;
            if (EsInformeDeFallo(evento)) return false;
            return registro != null && partida != null && registro.PartidaAnclada(partida());
        }

        /// <summary>
        /// Decisión de la telemetría al ENVIAR la cola al servidor (HttpAnalyticsBackend.Send): solo se bloquea con la opción
        /// encendida y algún módulo aplicado. Sin módulos la cola solo tiene lo que dejó pasar <see cref="BloquearTelemetria"/>
        /// (y lo de sesiones anteriores, que una sesión sin partida anclada ya enviaba igual); bloquearla retendría los
        /// informes de fallo hasta que acabara la partida. Si no están puestos los dos enganches de apuntar
        /// (<paramref name="filtroDeEventos"/> = false) el filtro previo no es seguro y se vuelve a la regla anterior: se
        /// bloquea mientras la partida en curso esté anclada.
        /// Telemetry decision when SENDING the queue to the server (HttpAnalyticsBackend.Send): it only blocks with the option
        /// on and some module applied. Without modules the queue only holds what <see cref="BloquearTelemetria"/> let through
        /// (and items from earlier sessions, which a session with no anchored run already sent anyway); blocking it would hold back the
        /// crash reports until the run ended. If the two recording hooks are not both in place
        /// (<paramref name="filtroDeEventos"/> = false) the pre-filter is not safe and it falls back to the previous rule: it
        /// blocks while the current run is anchored.
        /// </summary>
        public static bool BloquearEnvioTelemetria(bool opcionBloquear, bool modulosAplicados, bool filtroDeEventos, RegistroGuardias registro, Func<string> partida)
        {
            if (!opcionBloquear) return false;
            if (modulosAplicados) return true;
            if (filtroDeEventos) return false;
            return registro != null && partida != null && registro.PartidaAnclada(partida());
        }

        // ---------------------------------------------------------------
        // Memoria de textos ya saneados (0.7.2, revisión). Solo en memoria: no va al fichero del registro.
        // El juego escribe cada save dos veces por el saneado (postfix de JsonUtility.ToJson y prefix de File.WriteAllText
        // con el mismo texto). En la segunda pasada el texto ya no trae nada nuevo y cada etapa olvidaría lo apuntado
        // (objetos, ofertas de unidades, raza de la ciudad...). Cada etapa del saneado apunta aquí el texto que devuelve y
        // con qué etapas está ya saneado; si le llega exactamente un texto saneado por ella, lo devuelve sin tocar el
        // registro. Así ParaDisco(ParaDisco(x)) deja el registro igual, también en la tubería completa (la marca de etapas
        // pasa de una etapa a la siguiente).
        // Límite: solo vale si un texto saneado no puede volver idéntico byte a byte con otro significado. Con los saves
        // del juego (que llevan oro, ejército, estadísticas...) eso no pasa; un valor elegido a propósito (por ejemplo el
        // héroe prestado) siempre llega en un texto distinto y se trata con normalidad.
        // Memory of already sanitized texts (0.7.2, review). In memory only: it does not go to the registry file.
        // The game writes each save twice because of the sanitizing (postfix of JsonUtility.ToJson and prefix of File.WriteAllText
        // with the same text). On the second pass the text brings nothing new and each stage would forget what was recorded
        // (gear, unit offers, the city's race...). Each sanitizing stage records here the text it returns and
        // which stages it has already been sanitized with; if it receives exactly a text sanitized by it, it returns it without touching the
        // registry. This way ParaDisco(ParaDisco(x)) leaves the registry unchanged, also in the full pipeline (the stage mark
        // passes from one stage to the next).
        // Limit: it only works if a sanitized text cannot come back byte-for-byte identical with a different meaning. With the game's
        // saves (which carry gold, army, stats...) that does not happen; a deliberately chosen value (for example the
        // borrowed hero) always arrives in a different text and is handled normally.
        // ---------------------------------------------------------------
        /// <summary>Etapas del saneado (bits) con las que se marca un texto saneado. 0.7.4: hechizos propios (16). 0.7.5: historial (32).</summary>
        /// Sanitizing stages (bits) used to mark a sanitized text. 0.7.4: own spells (16). 0.7.5: history (32).
        public const int EtapaUnidades = 1, EtapaRazas = 2, EtapaObjetos = 4, EtapaHeroes = 8, EtapaHechizos = 16, EtapaHistorial = 32;
        /// <summary>Todas las etapas: lo que marca Saves.SanearSalida al terminar la tubería.</summary>
        /// All stages: what Saves.SanearSalida marks at the end of the pipeline.
        public const int EtapasTodas = EtapaUnidades | EtapaRazas | EtapaObjetos | EtapaHeroes | EtapaHechizos | EtapaHistorial;
        // Textos recientes (el más reciente primero) y sus etapas; pocos y pequeños (saves de decenas de KB)
        // Recent texts (most recent first) and their stages; few and small (saves of tens of KB)
        private const int MaxSalidas = 16;
        private readonly List<KeyValuePair<string, int>> _salidas = new List<KeyValuePair<string, int>>();

        /// <summary>Etapas con las que <paramref name="texto"/> salió saneado hace poco (0 si no es una salida reciente).</summary>
        /// Stages with which <paramref name="texto"/> came out sanitized recently (0 if it is not a recent output).
        public int EtapasDe(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return 0;
            lock (_cerrojo)
            {
                foreach (var s in _salidas)
                    if (s.Key.Length == texto.Length && string.Equals(s.Key, texto, StringComparison.Ordinal)) return s.Value;
                return 0;
            }
        }

        /// <summary>¿Es <paramref name="texto"/> una salida reciente de la tubería completa (todas las etapas)?</summary>
        /// Is <paramref name="texto"/> a recent output of the full pipeline (all stages)?
        public bool EsSalidaCompleta(string texto) => (EtapasDe(texto) & EtapasTodas) == EtapasTodas;

        /// <summary>Apunta <paramref name="texto"/> como saneado con <paramref name="etapas"/> (se suman a las que ya tuviera).</summary>
        /// Records <paramref name="texto"/> as sanitized with <paramref name="etapas"/> (added to those it already had).
        public void RecordarSalida(string texto, int etapas)
        {
            if (string.IsNullOrEmpty(texto) || etapas == 0) return;
            lock (_cerrojo)
            {
                for (int i = 0; i < _salidas.Count; i++)
                {
                    var s = _salidas[i];
                    if (s.Key.Length != texto.Length || !string.Equals(s.Key, texto, StringComparison.Ordinal)) continue;
                    _salidas.RemoveAt(i);
                    etapas |= s.Value;
                    break;
                }
                _salidas.Insert(0, new KeyValuePair<string, int>(texto, etapas));
                if (_salidas.Count > MaxSalidas) _salidas.RemoveAt(_salidas.Count - 1);
            }
        }

        /// <summary>Vacía la memoria de textos saneados (como al empezar otra sesión del juego; lo usan las pruebas).</summary>
        /// Clears the memory of sanitized texts (as when starting another game session; used by the tests).
        public void OlvidarSalidas() { lock (_cerrojo) _salidas.Clear(); }

        private static bool DiccionariosIguales(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a) if (!b.TryGetValue(kv.Key, out int v) || v != kv.Value) return false;
            return true;
        }
    }
}
