// Saves.cs
// Enganches que aplican el saneado de SaneadoSaves.cs a todo lo que el juego serializa con JsonUtility:
//   - Al escribir: postfix de JsonUtility.ToJson(object, bool) (lo que usa SaveDataHandler.SaveToJSON) y, como
//     segunda red, prefix de File.WriteAllText. SaveToJSON pasa el MISMO texto por los dos: el texto que sale de la
//     primera se recuerda (memoria de textos saneados del registro) y la segunda red, si le llega ese texto, solo hace la
//     copia de seguridad y no vuelve a sanear. Hasta la revisión de la 0.7.2 se decía aquí que aplicarlos dos veces no
//     cambiaba nada, y no era cierto: la segunda pasada olvidaba los objetos, las razas y las ofertas apuntadas.
//   - Al leer: prefix de JsonUtility.FromJson(string, Type) y de FromJsonInternal (el genérico FromJson<T> que usa
//     ReadListFromJSON acaba en uno de los dos).
//   - Copia de seguridad: la primera vez que en esta sesión se reescribe cada fichero de save, se copia antes la
//     versión anterior a BepInEx\config\ZeroNucleo_copias\<fecha>\ (hasta la 0.6.1, ZeroGuardiaImperial_copias).
//   - Verificación: si tras sanear quedara alguna unidad nueva (9xx) en una clave de unidad, se avisa en el log.
//   - Vigilante: un temporizador revisa cada 2 s los ficheros de save y sanea cualquier unidad nueva que hubiera
//     llegado al disco (última red por si el compilador del juego hubiera inlineado alguna llamada enganchada).
// Qué es «unidad nueva» y cuál es su base lo dice la tabla del manifiesto (UnidadGuardia.CargarManifiesto).
// 0.5.0 (F2): lo mismo con las razas nuevas 40/41 (raza de la ciudad y del campo de batalla, temas de taberna), con la
// tabla de razas que prepara FaccionesNuevas (SaneadoSaves.RazasParaDisco / RazasDesdeDisco).
// 0.6.0: lo mismo con los objetos nuevos 700-729 (SaneadoObjetos.cs, tabla que prepara Objetos.Configurar). Se restauran
// al leer solo si los objetos están activos; al escribir siempre se sanean.
// 0.7.1: lo mismo con los héroes nuevos 901-904 del save de campaña (SaneadoSaves.HeroesParaDisco / HeroesDesdeDisco).
// 0.7.4: lo mismo con los hechizos propios 1900-1999 (SaneadoHechizos.cs): ranuras, lanzamientos, historial y los lanzados
// alguna vez. Se sanean siempre al escribir; al leer vuelven los que tiene definidos un módulo activo (HechizoDisponible).
// 0.7.5 (revisión total del 02-11): los saves de los cuatro slots de campaña del juego 1.11.1 (principal, diaria,
// cooperativo y la vista del invitado, cada uno con su instantánea) entran en la copia previa, el vigilante y la partida en
// curso (la del slot activo). Nueva etapa del saneado: historial de partidas y registro de batallas
// (SaneadoSaves.HistorialParaDisco). Todas las etapas se aplican solo a los textos de save (EsTextoDeSave). La primera copia
// de seguridad de la sesión lleva también el registro aparte (no viaja con Steam Cloud: así se puede recuperar a mano).
// También aquí: bloqueo del envío a clasificaciones online (partidas con cualquier módulo de Zero aplicado).
// Saves.cs
// Hooks that apply the sanitizing from SaneadoSaves.cs to everything the game serializes with JsonUtility:
//   - On write: postfix of JsonUtility.ToJson(object, bool) (what SaveDataHandler.SaveToJSON uses) and, as a
//     second safety net, prefix of File.WriteAllText. SaveToJSON passes the SAME text through both: the text that comes out of the
//     first one is remembered (memory of sanitized texts in the registry) and the second net, if that text reaches it, only makes the
//     backup copy and does not sanitize again. Until the review of 0.7.2 it said here that applying them twice
//     changed nothing, and that was not true: the second pass forgot the gear, the races and the recorded offers.
//   - On read: prefix of JsonUtility.FromJson(string, Type) and of FromJsonInternal (the generic FromJson<T> used by
//     ReadListFromJSON ends up in one of the two).
//   - Backup: the first time in this session that each save file is rewritten, the previous
//     version is copied first to BepInEx\config\ZeroNucleo_copias\<fecha>\ (until 0.6.1, ZeroGuardiaImperial_copias).
//   - Verification: if after sanitizing any new unit (9xx) remains in a unit key, a warning is written to the log.
//   - Watcher: a timer checks the save files every 2 s and sanitizes any new unit that had
//     reached the disk (last safety net in case the game's compiler had inlined some hooked call).
// What a "new unit" is and what its base is are given by the manifest table (UnidadGuardia.CargarManifiesto).
// 0.5.0 (F2): the same with the new races 40/41 (city and battlefield race, tavern themes), with the
// race table prepared by FaccionesNuevas (SaneadoSaves.RazasParaDisco / RazasDesdeDisco).
// 0.6.0: the same with the new gear 700-729 (SaneadoObjetos.cs, table prepared by Objetos.Configurar). They are restored
// on read only if the gear is active; on write they are always sanitized.
// 0.7.1: the same with the new heroes 901-904 of the campaign save (SaneadoSaves.HeroesParaDisco / HeroesDesdeDisco).
// 0.7.4: the same with the custom spells 1900-1999 (SaneadoHechizos.cs): slots, casts, history and the ones cast
// at some point. They are always sanitized on write; on read, those defined by an active module come back (HechizoDisponible).
// 0.7.5 (full review of 02-11): the saves of the four campaign slots of game 1.11.1 (main, daily,
// co-op and the guest view, each with its snapshot) go into the prior backup, the watcher and the current
// run (the one in the active slot). New sanitizing stage: run history and battle log
// (SaneadoSaves.HistorialParaDisco). All stages are applied only to save texts (EsTextoDeSave). The first backup
// of the session also includes the separate registry (it does not travel with Steam Cloud, so it can be recovered by hand).
// Also here: blocking of submission to online leaderboards (runs with any Zero module applied).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: estado del saneado y arranque
    // Section: sanitizing state and startup
    // -----------------------------------------------------------------------
    public static class Saves
    {
        internal static RegistroGuardias Registro;
        // Ids nuevos -> número de su unidad base (la pone UnidadGuardia al leer el manifiesto; null = aún no)
        // New ids -> number of their base unit (set by UnidadGuardia when reading the manifest; null = not yet)
        internal static TablaUnidades Tabla;
        // Razas nuevas -> raza sustituta de serie (la pone FaccionesNuevas; null = facciones nuevas desactivadas)
        // New races -> default replacement race (set by FaccionesNuevas; null = new factions disabled)
        internal static TablaRazas TablaRazas;
        // 0.7.5 (ronda 2 de la revisión): razas nuevas de TODOS los módulos instalados (activos o no) -> sustituta. Solo para
        // la ida al disco del historial: así un 40/41 de una partida vieja no llega al disco con su facción apagada
        // 0.7.5 (review round 2): new races from ALL installed modules (active or not) -> replacement. Only for
        // the history's trip to disk: this way a 40/41 from an old run does not reach the disk with its faction turned off
        internal static TablaRazas TablaRazasInstaladas;
        private static TablaRazas RazasHistorial => TablaRazasInstaladas ?? TablaRazas;
        // Objetos nuevos -> objeto de serie de respaldo (la pone Objetos.Configurar; null = no hay objetos)
        // New gear -> default fallback item (set by Objetos.Configurar; null = no gear)
        internal static TablaObjetos TablaObjetos;
        // 0.7.1: héroes nuevos de los módulos instalados -> héroe prestado de serie (la pone Plugin.Load; null = ninguno)
        // 0.7.1: new heroes from installed modules -> default borrowed hero (set by Plugin.Load; null = none)
        internal static TablaHeroes TablaHeroes;
        private static int _heroesSaneados, _heroesRestaurados;
        // 0.7.4: ¿se puede devolver al leer este hechizo propio? (lo pone Plugin con Hechizos.Definidos; null = ninguno)
        // 0.7.4: can this custom spell be returned when reading? (set by Plugin with Hechizos.Definidos; null = none)
        internal static Func<int, bool> HechizoDisponible;
        private static int _hechizosSaneados, _hechizosRestaurados;
        private static int _objetosSaneados, _objetosRestaurados;
        private static string _carpetaCopias;
        private static string _rutaRegistro;   // 0.7.5: para copiarlo con la primera copia de saves de la sesión
                                               // 0.7.5: to copy it with the session's first save backup
        private static string _sesion;
        private static readonly HashSet<string> _copiados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static int _escriturasSaneadas, _lecturasRestauradas, _razasSaneadas, _razasRestauradas, _historialSaneado, _historialRestaurado;
        // Solo para la autoprueba: la lectura se comporta como si no hubiera unidades 9xx registradas
        // Only for the self-test: reading behaves as if there were no 9xx units registered
        internal static bool SimularSinUnidades;
        // 0.7.0, solo para la autoprueba: mientras se simulan otros conjuntos de módulos el vigilante no revisa el disco
        // (bandera simple: Nucleo.Simular la pone en cada simulación y Nucleo.Restaurar la quita una vez al final)
        // 0.7.0, only for the self-test: while other sets of modules are simulated the watcher does not check the disk
        // (simple flag: Nucleo.Simular sets it on each simulation and Nucleo.Restaurar clears it once at the end)
        internal static volatile bool PausarVigilante;
        // Revisión del 18-10 (A-1): pausa con contador y espera a la vuelta en curso. La autoprueba la mantiene puesta de
        // principio a fin, porque cambia Saves.Registro por un registro temporal y el vigilante corre en otro hilo
        // Review of 18-10 (A-1): pause with counter and wait for the current loop to finish. The self-test keeps it set from
        // start to finish, because it swaps Saves.Registro for a temporary registry and the watcher runs on another thread
        internal static readonly PausaVigilante Pausa = new PausaVigilante();
        // Razas apuntadas sin restaurar de las que ya se ha avisado en esta sesión
        // Recorded races left unrestored that have already been warned about in this session
        private static readonly HashSet<string> _pendientesAvisadas = new HashSet<string>();
        private static bool _clasificacionAvisada;

        // Ficheros de save del juego (los que se copian antes de la primera reescritura de la sesión y revisa el vigilante).
        // 0.7.5: los de los cuatro slots de campaña de la 1.11.1 (nombres de global-metadata.dat, 02-11-2026) y, por si una
        // actualización añade otro, cualquier «*SaveData*.json» de la carpeta (EsFicheroSave)
        // Game save files (the ones copied before the session's first rewrite and checked by the watcher).
        // 0.7.5: those of the four campaign slots of 1.11.1 (names from global-metadata.dat, 02-11-2026) and, in case an
        // update adds another, any «*SaveData*.json» in the folder (EsFicheroSave)
        internal static readonly string[] FicherosSave =
        {
            "campaignSaveData.json", "campaignSaveDataSnapshot.json", "dailySaveData.json", "dailySaveDataSnapshot.json",
            "coopSaveData.json", "coopSaveDataSnapshot.json", "coopViewSaveData.json", "coopViewSaveDataSnapshot.json",
            "playerSaveData.json", "customBattleSaveData.json",
        };

        /// <summary>¿Es un fichero de save del juego? Los conocidos o cualquier «*SaveData*.json» (0.7.5).</summary>
        /// Is it a game save file? The known ones or any «*SaveData*.json» (0.7.5).
        internal static bool EsFicheroSave(string nombre) =>
            !string.IsNullOrEmpty(nombre) && (Array.IndexOf(FicherosSave, nombre) >= 0
            || (nombre.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && nombre.IndexOf("SaveData", StringComparison.OrdinalIgnoreCase) >= 0));

        // Ficheros que revisa el vigilante: los conocidos y los «*SaveData*.json» que haya en la carpeta
        // Files checked by the watcher: the known ones and any «*SaveData*.json» in the folder
        private static IEnumerable<string> FicherosAVigilar()
        {
            var nombres = new List<string>(FicherosSave);
            try
            {
                foreach (string ruta in Directory.GetFiles(_carpetaSaves, "*SaveData*.json"))
                {
                    string n = Path.GetFileName(ruta);
                    if (!nombres.Contains(n, StringComparer.OrdinalIgnoreCase)) nombres.Add(n);
                }
            }
            catch { }
            return nombres;
        }

        /// <summary>Carga el registro de GUID y prepara la carpeta de copias de esta sesión.</summary>
        /// Loads the GUID registry and prepares this session's backup folder.
        public static void Iniciar(string rutaRegistro, string carpetaCopias)
        {
            Registro = new RegistroGuardias(rutaRegistro) { Avisar = Plugin.Aviso };
            _rutaRegistro = rutaRegistro;
            SaneadoSaves.AvisarNoRestaurable = Plugin.Aviso;
            Registro.Cargar();
            _carpetaCopias = carpetaCopias;
            _sesion = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            // Revisión del 18-10, ronda 2 (M-A): si el registro quedó en solo lectura (bloqueado o ilegible), se dice aquí
            // Review of 18-10, round 2 (M-A): if the registry ended up read-only (locked or unreadable), it is reported here
            Plugin.Info($"Registro de unidades nuevas cargado: {Registro.NumeroGuids} escuadra(s) apuntada(s) ({rutaRegistro})" +
                        (Registro.SoloLectura ? " (SOLO LECTURA en esta sesión: no se guardará)." : "."));
        }

        // ---------------------------------------------------------------
        // Escritura
        // Writing
        // ---------------------------------------------------------------
        // Postfix de JsonUtility.ToJson(object, bool): el texto que sale nunca lleva unidades nuevas
        // Postfix of JsonUtility.ToJson(object, bool): the resulting text never contains new units
        public static void TrasToJson(ref string __result)
        {
            Plugin.TrazaUnaVez("TrasToJson");
            try
            {
                // Diagnóstico: confirma una vez que el enganche ve los saves reales del juego (no solo la autoprueba)
                // Diagnostic: confirms once that the hook sees the game's real saves (not just the self-test)
                if (!_toJsonSaveVisto && __result != null && (__result.Contains("\"playerArmy\"") || __result.Contains("\"troopsRecruited\"")))
                {
                    _toJsonSaveVisto = true;
                    Plugin.Info("Saneado: JsonUtility.ToJson intercepta un save real del juego (enganche de escritura efectivo).");
                }
                __result = SanearSalida(__result, "ToJson");
            }
            catch (Exception e) { Plugin.Error("TrasToJson: " + e.Message); }
        }
        private static bool _toJsonSaveVisto, _writeAllTextVisto, _segundaPasadaVista;

        // ---------------------------------------------------------------
        // Vigilante de ficheros: última red, independiente de los enganches
        // File watcher: last safety net, independent of the hooks
        // ---------------------------------------------------------------
        // Cada 2 s mira si algún save cambió en disco y, si contiene una unidad nueva en una clave de unidad (porque algún
        // enganche de escritura no se ejecutara, p. ej. por inlining del compilador), lo sanea y lo reescribe de forma
        // atómica. Solo usa System.IO y el saneado (.NET puro): seguro en un hilo aparte.
        // Every 2 s it checks whether any save changed on disk and, if it contains a new unit in a unit key (because some
        // write hook did not run, e.g. due to compiler inlining), it sanitizes it and rewrites it
        // atomically. It only uses System.IO and the sanitizing (pure .NET): safe on a separate thread.
        private static System.Threading.Timer _vigilante;
        private static string _carpetaSaves;
        private static readonly Dictionary<string, DateTime> _ultimaEscritura = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        // Ficheros de save con un fallo del vigilante ya avisado en esta sesión (B-2: un aviso por fichero, no cada 2 s)
        // Save files with a watcher failure already warned about in this session (B-2: one warning per file, not every 2 s)
        private static readonly HashSet<string> _vigilanteAvisados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Arranca el vigilante sobre la carpeta de saves (llamar desde el hilo principal).</summary>
        /// Starts the watcher on the saves folder (call from the main thread).
        public static void ArrancarVigilante(string carpetaSaves)
        {
            if (_vigilante != null || string.IsNullOrEmpty(carpetaSaves) || !Directory.Exists(carpetaSaves)) return;
            _carpetaSaves = carpetaSaves;
            _vigilante = new System.Threading.Timer(_ => RevisarSaves(), null, 2000, 2000);
            Plugin.Info($"Vigilante de saves activo en {carpetaSaves} (revisa cada 2 s que no quede ninguna unidad nueva en disco).");
        }

        // Revisa los ficheros de save que hayan cambiado desde la última vez
        // Checks the save files that have changed since the last time
        private static void RevisarSaves()
        {
            // Revisión del 18-10 (A-1): Pausa.Entrar marca la vuelta en marcha, así la autoprueba puede esperarla antes de
            // cambiar el registro
            // Review of 18-10 (A-1): Pausa.Entrar marks the loop as running, so the self-test can wait for it before
            // swapping the registry
            if (Tabla == null || PausarVigilante || !Pausa.Entrar()) return;
            try
            {
                // El registro se lee UNA vez por vuelta y nunca se usa uno solo en memoria (el temporal de la autoprueba):
                // aunque la autoprueba lo cambiara a mitad de vuelta, los GUID irían siempre al registro real
                // The registry is read ONCE per pass and a standalone one is never used in memory (the self-test's temporary one):
                // even if the self-test changed it mid-pass, the GUIDs would always go to the real registry
                RegistroGuardias registro = Registro;
                if (registro == null || registro.SoloEnMemoria) return;
                foreach (string nombre in FicherosAVigilar())
                {
                    // Revisión del 18-10, ronda 2 (B-2): cada fichero va en su propio try. Un fallo en uno (p. ej. la copia
                    // de seguridad con el disco lleno) no corta la revisión de los demás, y su aviso sale una sola vez por
                    // fichero y sesión (antes se repetía cada 2 s). Ese fichero se vuelve a intentar en la siguiente vuelta
                    // Review of 18-10, round 2 (B-2): each file goes in its own try. A failure in one (e.g. the backup
                    // with a full disk) does not cut off the review of the others, and its warning is shown only once per
                    // file and session (it used to repeat every 2 s). That file is retried on the next pass
                    try
                    {
                        string ruta = Path.Combine(_carpetaSaves, nombre);
                        var info = new FileInfo(ruta);
                        if (!info.Exists) continue;
                        // Fecha y tamaño leídos ANTES del contenido: si luego cambian, el juego ha vuelto a guardar (M-3)
                        // Date and size read BEFORE the contents: if they change afterwards, the game has saved again (M-3)
                        DateTime fecha = info.LastWriteTimeUtc;
                        long tamano = info.Length;
                        if (_ultimaEscritura.TryGetValue(nombre, out DateTime antes) && antes == fecha) continue;
                        string texto;
                        try
                        {
                            using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                            using var sr = new StreamReader(fs);
                            texto = sr.ReadToEnd();
                        }
                        catch (IOException) { continue; }   // el juego lo está escribiendo: siguiente vuelta
                                                            // the game is writing it: next pass
                        // Revisión del 18-10, ronda 2 (B-1): se apunta como revisada la fecha de la versión LEÍDA (no la que
                        // tenga el fichero al final de la vuelta, que podría ser una versión nueva sin revisar); tras una
                        // reescritura propia, la del fichero recién reemplazado
                        // Review of 18-10, round 2 (B-1): the date of the version that was READ is recorded as reviewed (not the one
                        // the file has at the end of the pass, which could be a new unreviewed version); after our own
                        // rewrite, the date of the freshly replaced file
                        DateTime revisada = fecha;
                        // 0.7.5: recuento por tipo (el aviso dice qué era) y con la etapa del historial
                        // 0.7.5: count by type (the warning says what it was) and with the history stage
                        var porTipo = ContarResiduosPorTipo(texto);
                        int residuos = porTipo.Values.Sum();
                        if (residuos > 0)
                        {
                            // Ronda 2 de la revisión: la tubería pura (TuberiaSaneado.cs), la misma que pasan las pruebas
                            // Round 2 of the review: the pure pipeline (TuberiaSaneado.cs), the same one the tests run
                            string limpio = TuberiaSaneado.ParaDisco(texto, TablasActuales(), registro);
                            if (ContarResiduosPorTipo(limpio).Values.Sum() == 0)
                            {
                                // 0.6.0 (revisión) y revisión del 18-10 (M-3): si el juego ha vuelto a guardar mientras se
                                // saneaba (fecha o tamaño distintos), no se pisa su versión nueva; se revisa en la siguiente
                                // vuelta. Se comprueba al empezar y otra vez justo antes de File.Replace (EscrituraSegura).
                                // Sin copia de seguridad no se reescribe (si CopiarAntesDeReescribir lanza, sale al catch de
                                // este fichero y se reintenta en la siguiente vuelta)
                                // 0.6.0 (review) and review of 18-10 (M-3): if the game has saved again while it was
                                // being sanitized (different date or size), its new version is not overwritten; it is reviewed on the next
                                // pass. It is checked at the start and again right before File.Replace (EscrituraSegura).
                                // Without a backup it is not rewritten (if CopiarAntesDeReescribir throws, it goes to this file's catch
                                // and is retried on the next pass)
                                if (!EscrituraSegura.ReemplazarSiNoCambio(ruta, limpio, fecha, tamano, () => CopiarAntesDeReescribir(ruta))) continue;
                                revisada = File.GetLastWriteTimeUtc(ruta);
                                // 0.7.5: desglose por tipo (antes no decía que podían ser hechizos)
                                // 0.7.5: breakdown by type (it used to not say they could be spells)
                                string desglose = string.Join(", ", porTipo.Where(kv => kv.Value > 0).Select(kv => kv.Value + " " + kv.Key));
                                Plugin.Aviso($"Vigilante: {nombre} tenía {residuos} id(s) nuevo(s) en disco ({desglose}); saneado y reescrito (los enganches de escritura no lo cubrieron).");
                            }
                            else Plugin.Error($"Vigilante: no se pudo sanear {nombre} ({residuos} residuos).");
                        }
                        _ultimaEscritura[nombre] = revisada;
                    }
                    catch (Exception e)
                    {
                        if (_vigilanteAvisados.Add(nombre))
                            Plugin.Aviso($"Vigilante de saves ({nombre}): {e.Message}. Se reintenta cada vuelta sin repetir este aviso; los demás saves se siguen revisando.");
                    }
                }
            }
            catch (Exception e) { Plugin.Aviso("Vigilante de saves: " + e.Message); }
            finally { Pausa.Salir(); }
        }

        // 0.7.5: ids nuevos que quedan en un texto, por tipo (para el vigilante y su aviso)
        // 0.7.5: new ids left in a text, by type (for the watcher and its warning)
        private static Dictionary<string, int> ContarResiduosPorTipo(string texto) => TuberiaSaneado.ResiduosPorTipo(texto, TablasActuales());

        // Ronda 2 de la revisión: las tablas de ahora para la tubería pura (TuberiaSaneado.cs)
        // Round 2 of the review: the current tables for the pure pipeline (TuberiaSaneado.cs)
        private static TablasSaneado TablasActuales() => new TablasSaneado
        {
            Unidades = Tabla, Razas = TablaRazas, RazasHistorial = TablaRazasInstaladas, Objetos = TablaObjetos,
            ObjetosActivos = Objetos.ActivosEnCache, Heroes = TablaHeroes, HechizoDisponible = HechizoDisponible,
        };

        // Prefix de File.WriteAllText(path, contents): segunda red y copia de seguridad del save anterior
        // Prefix of File.WriteAllText(path, contents): second safety net and backup of the previous save
        public static void AntesDeWriteAllText(string path, ref string contents)
        {
            Plugin.TrazaUnaVez("AntesDeWriteAllText");
            try
            {
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".json.tmp", StringComparison.OrdinalIgnoreCase)) return;
                string destino = path.Substring(0, path.Length - 4);   // quita ".tmp"
                                                                       // removes ".tmp"
                if (!_writeAllTextVisto) { _writeAllTextVisto = true; Plugin.Info("Saneado: File.WriteAllText intercepta la escritura de un save real (" + Path.GetFileName(destino) + ")."); }
                // Revisión del 18-10 (L-14): la copia de seguridad va en su propio try; si falla (fichero bloqueado, disco
                // lleno) el texto se sanea igual
                // Review of 18-10 (L-14): the backup goes in its own try; if it fails (locked file, full
                // disk) the text is sanitized anyway
                try { CopiarAntesDeReescribir(destino); }
                catch (Exception e) { Plugin.Aviso("Copia de seguridad del save previo fallida (" + Path.GetFileName(destino) + "): " + e.Message + ". Se sanea igual."); }
                // 0.7.2 (revisión): el texto que acaba de salir saneado de TrasToJson (o cualquier otro ya saneado entero)
                // no se vuelve a sanear: la segunda pasada olvidaba lo apuntado
                // 0.7.2 (review): the text that has just come out sanitized from TrasToJson (or any other already fully sanitized one)
                // is not sanitized again: the second pass forgot what had been recorded
                if (Registro != null && Registro.EsSalidaCompleta(contents))
                {
                    if (!_segundaPasadaVista) { _segundaPasadaVista = true; Plugin.Info("Saneado: File.WriteAllText recibe el texto ya saneado por JsonUtility.ToJson; solo se hace la copia de seguridad (sin segunda pasada)."); }
                    return;
                }
                contents = SanearSalida(contents, "WriteAllText " + Path.GetFileName(destino));
            }
            catch (Exception e) { Plugin.Error("AntesDeWriteAllText: " + e.Message); }
        }

        // Aplica el saneado de ida y verifica que no queda ninguna unidad nueva
        // Applies the outbound sanitizing and verifies that no new unit remains
        private static string SanearSalida(string json, string origen)
        {
            if (Registro == null || string.IsNullOrEmpty(json)) return json;
            // 0.7.2 (revisión): un texto que ya salió de aquí no se vuelve a sanear (ni a verificar)
            // 0.7.2 (review): a text that has already come out of here is not sanitized (or verified) again
            if (Registro.EsSalidaCompleta(json)) return json;
            // 0.7.5 (revisión total, hallazgo 27): TODAS las etapas solo en los textos de save (antes solo la de hechizos). Un
            // JSON que no es save (ajustes, registros de partida sueltos que el juego pasa por ToJson...) no se toca: así el
            // registro tampoco apunta nada de un texto que no es la partida
            // 0.7.5 (full review, finding 27): ALL stages only on save texts (before, only the spells one). A
            // JSON that is not a save (settings, loose run records that the game passes through ToJson...) is not touched: this way the
            // registry doesn't record anything from a text that is not the run either
            if (!EsTextoDeSave(json)) return json;
            string limpio = Tabla == null ? json : SaneadoSaves.ParaDisco(json, Tabla, Registro);
            // F2: razas nuevas (ciudad, campo de batalla, temas de taberna)
            // F2: new races (city, battlefield, tavern themes)
            string sinRazas = SaneadoSaves.RazasParaDisco(limpio, TablaRazas, Registro);
            if (!ReferenceEquals(sinRazas, limpio) && sinRazas != limpio)
            {
                _razasSaneadas++;
                if (_razasSaneadas <= 5 || _razasSaneadas % 50 == 0)
                    Plugin.Info($"Saneado al escribir ({origen}): raza nueva de ciudad o campo de batalla guardada como su raza sustituta + registro aparte ({_razasSaneadas} textos en la sesión).");
                limpio = sinRazas;
            }
            if (!ReferenceEquals(limpio, json) && limpio != json)
            {
                _escriturasSaneadas++;
                if (_escriturasSaneadas <= 5 || _escriturasSaneadas % 50 == 0)
                    Plugin.Info($"Saneado al escribir ({origen}): unidades nuevas guardadas como su unidad base + registro aparte " +
                                $"({Registro.NumeroGuids} escuadras apuntadas, {_escriturasSaneadas} textos saneados en la sesión).");
            }
            // 0.6.0: objetos nuevos (Gear, ofertas, botín de ciudad, vendidos, rotos, colección e historial)
            // 0.6.0: new gear (Gear, offers, city loot, sold, broken, Collection and history)
            string sinObjetos = SaneadoObjetos.ParaDisco(limpio, TablaObjetos, Registro, Objetos.ActivosEnCache);
            if (!ReferenceEquals(sinObjetos, limpio) && sinObjetos != limpio)
            {
                _objetosSaneados++;
                if (_objetosSaneados <= 5 || _objetosSaneados % 50 == 0)
                    Plugin.Info($"Saneado al escribir ({origen}): objetos nuevos guardados como su objeto de serie de respaldo + registro aparte ({_objetosSaneados} textos en la sesión).");
                limpio = sinObjetos;
            }
            // 0.7.1: héroes nuevos del save de campaña (héroe del jugador y señor de la guerra)
            // 0.7.1: new heroes from the campaign save (player hero and warlord)
            string sinHeroes = SaneadoSaves.HeroesParaDisco(limpio, TablaHeroes, Registro);
            if (!ReferenceEquals(sinHeroes, limpio) && sinHeroes != limpio)
            {
                _heroesSaneados++;
                if (_heroesSaneados <= 5 || _heroesSaneados % 50 == 0)
                    Plugin.Info($"Saneado al escribir ({origen}): héroe nuevo guardado como su héroe prestado de serie + registro aparte ({_heroesSaneados} textos en la sesión).");
                limpio = sinHeroes;
            }
            // 0.7.5: historial de partidas (save del jugador) y registro de batallas (save de campaña)
            // 0.7.5: run history (player save) and battle log (campaign save)
            string sinHistorial = SaneadoSaves.HistorialParaDisco(limpio, TablaHeroes, RazasHistorial, Registro);
            if (!ReferenceEquals(sinHistorial, limpio) && sinHistorial != limpio)
            {
                _historialSaneado++;
                if (_historialSaneado <= 5 || _historialSaneado % 50 == 0)
                    Plugin.Info($"Saneado al escribir ({origen}): héroes y razas nuevos del historial de partidas guardados como su prestado o sustituta + registro aparte ({_historialSaneado} textos en la sesión).");
                limpio = sinHistorial;
            }
            int residuosHistorial = SaneadoSaves.ContarResiduosHistorial(limpio, TablaHeroes, RazasHistorial);
            if (residuosHistorial > 0) Plugin.Error($"VERIFICACIÓN: quedan {residuosHistorial} héroe(s) o raza(s) nuevo(s) en el historial de un texto guardado ({origen}). Revisa el formato del save.");
            // 0.7.4: hechizos propios (ranuras, lanzamientos, historial y lanzados alguna vez)
            // 0.7.4: custom spells (slots, casts, history and ever cast)
            string sinHechizos = SaneadoHechizos.ParaDisco(limpio, Registro);
            if (!ReferenceEquals(sinHechizos, limpio) && sinHechizos != limpio)
            {
                _hechizosSaneados++;
                if (_hechizosSaneados <= 5 || _hechizosSaneados % 50 == 0)
                    Plugin.Info($"Saneado al escribir ({origen}): hechizos propios quitados del save + registro aparte ({_hechizosSaneados} textos en la sesión).");
                limpio = sinHechizos;
            }
            int residuosHechizos = SaneadoHechizos.ContarResiduos(limpio);
            if (residuosHechizos > 0) Plugin.Error($"VERIFICACIÓN: quedan {residuosHechizos} hechizo(s) propio(s) en un texto guardado ({origen}). Revisa el formato del save.");
            int residuosHeroes = SaneadoSaves.ContarResiduosHeroes(limpio, TablaHeroes);
            if (residuosHeroes > 0) Plugin.Error($"VERIFICACIÓN: quedan {residuosHeroes} héroe(s) nuevo(s) en un texto guardado ({origen}). Revisa el formato del save.");
            int residuosObjetos = SaneadoObjetos.ContarResiduos(limpio, TablaObjetos);
            if (residuosObjetos > 0) Plugin.Error($"VERIFICACIÓN: quedan {residuosObjetos} objeto(s) nuevo(s) en un texto guardado ({origen}). Revisa el formato del save.");
            int residuos = SaneadoSaves.ContarResiduos(limpio, Tabla);
            if (residuos > 0) Plugin.Error($"VERIFICACIÓN: quedan {residuos} unidad(es) nueva(s) en un texto guardado ({origen}). Revisa el formato del save.");
            int residuosRazas = SaneadoSaves.ContarResiduosRazas(limpio, TablaRazas);
            if (residuosRazas > 0) Plugin.Error($"VERIFICACIÓN: quedan {residuosRazas} raza(s) nueva(s) en un texto guardado ({origen}). Revisa el formato del save.");
            // Revisión del 18-10 (M-2): un save de campaña escrito con módulos aplicados marca su partida (runId) como
            // modificada en el registro; aunque se termine en una sesión sin módulos, no va a clasificaciones ni telemetría
            // Review of 18-10 (M-2): a campaign save written with modules applied marks its run (runId) as
            // modified in the registry; even if it ends in a session without modules, it does not go to leaderboards or telemetry
            if (Nucleo.PartidaModificada && SaneadoSaves.EsPartida(limpio)) Registro.MarcarPartidaModificada(SaneadoSaves.FirmaPartida(limpio));
            // 0.7.2 (revisión): se recuerda el texto saneado entero (todas las etapas) para que la segunda red de escritura
            // (File.WriteAllText con este mismo texto) no lo vuelva a sanear. Solo los saves (los demás ya han salido arriba)
            // 0.7.2 (review): the whole sanitized text (all stages) is remembered so that the second write safety net
            // (File.WriteAllText with this same text) does not sanitize it again. Only saves (the others have already come out above)
            Registro.RecordarSalida(limpio, RegistroGuardias.EtapasTodas);
            return limpio;
        }

        // Claves que delatan un texto de save (campaña, instantánea, jugador o batalla personalizada) frente al resto de JSON
        // que el juego serializa con JsonUtility (ajustes, mods...). 0.7.5: también las piezas de save que la autoprueba
        // pasa sueltas por ToJson (TownSaveData: «townRace», «townLootGearIDs») y las claves del jugador de la 1.11.1
        // Keys that reveal a save text (campaign, snapshot, player or custom battle) as opposed to the rest of the JSON
        // that the game serializes with JsonUtility (settings, mods...). 0.7.5: also the save pieces that the self-test
        // passes loose through ToJson (TownSaveData: «townRace», «townLootGearIDs») and the player keys from 1.11.1
        private static readonly string[] ClavesDeSave =
        {
            "\"UnitName\"", "\"Gear\"", "\"recruitableUnits\"", "\"townRace\"", "\"townLootGearIDs\"", "\"battleFieldPreset\"",
            "\"lastHeroID\"", "\"enemyWarlordHeroID\"", "\"troopsRecruited\"", "\"HeroDifficultiesCompleted\"", "\"gearIdsCollected\"",
            "\"selectedSpells\"", "\"spellsEverCast\"", "\"playerCustomBattleSpells\"", "\"runHistory\"", "\"battleLog\"",
            "\"lastStartingGearId\"", "\"UnitNameHistoricalKillStore\"", "\"SquadsInArmy\"",
        };
        // (Comprobado en el interop 1.11.1: CustomBattleSaveData lleva playerCustomBattleSpells, así que la batalla
        // personalizada entra; «SquadsInArmy» va por si algún ejército guardado suelto no trae otra clave de la lista)
        // (Checked in interop 1.11.1: CustomBattleSaveData carries playerCustomBattleSpells, so the custom
        // battle is included; «SquadsInArmy» is there in case some loose saved army has no other key from the list)

        // ¿Es un texto de save? Solo esos se sanean (0.7.5) y se recuerdan en la memoria de textos saneados
        // Is it a save text? Only those are sanitized (0.7.5) and remembered in the sanitized texts memory
        internal static bool EsTextoDeSave(string json)
        {
            foreach (string c in ClavesDeSave) if (json.IndexOf(c, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        // Copia el save que va a reescribirse (solo la primera vez por fichero y sesión)
        // Copies the save that is about to be rewritten (only the first time per file and session)
        private static void CopiarAntesDeReescribir(string destino)
        {
            string nombre = Path.GetFileName(destino);
            lock (_copiados)   // también lo usa el vigilante desde su hilo
                               // the watcher also uses it from its thread
            {
                if (!EsFicheroSave(nombre) || _copiados.Contains(nombre) || !File.Exists(destino)) return;
                string carpeta = Path.Combine(_carpetaCopias, _sesion);
                Directory.CreateDirectory(carpeta);
                File.Copy(destino, Path.Combine(carpeta, nombre), true);
                _copiados.Add(nombre);
                Plugin.Info($"Copia de seguridad del save previo: {Path.Combine(carpeta, nombre)}");
                // 0.7.5 (hallazgo 26): con la primera copia de la sesión va también el registro aparte (no viaja con Steam
                // Cloud; si la nube trae otro save, con esta copia se puede volver a mano). Si falla, solo se avisa
                // 0.7.5 (finding 26): the separate registry also goes along with the first copy of the session (it does not travel with Steam
                // Cloud; if the cloud brings another save, this copy can be used to roll back by hand). If it fails, only a warning is shown
                if (_copiados.Count == 1 && !string.IsNullOrEmpty(_rutaRegistro) && File.Exists(_rutaRegistro))
                {
                    try { File.Copy(_rutaRegistro, Path.Combine(carpeta, Path.GetFileName(_rutaRegistro)), true); }
                    catch (Exception e) { Plugin.Aviso("No se pudo copiar el registro aparte junto a la copia de los saves: " + e.Message); }
                }
            }
        }

        // ---------------------------------------------------------------
        // Lectura
        // Reading
        // ---------------------------------------------------------------
        // Prefix de JsonUtility.FromJson(string, Type) y FromJsonInternal(string, object, Type)
        // Prefix of JsonUtility.FromJson(string, Type) and FromJsonInternal(string, object, Type)
        public static void AntesDeFromJson(ref string json)
        {
            Plugin.TrazaUnaVez("AntesDeFromJson");
            try
            {
                if (Registro == null || string.IsNullOrEmpty(json)) return;
                // Revisión del 18-10 (M-2): un save de campaña con alguna escuadra apuntada (unidad 9xx guardada como su
                // base) es de una partida modificada aunque se empezara antes de esta versión; se marca su runId.
                // Ronda 2 (B-3): ojo, este prefix solo está enganchado con contenido aplicado (Plugin.AplicarParches), así
                // que aquí solo cubre ese caso. Las sesiones sin módulos activos las cubre Saves.PartidaEnCurso, que hace
                // la misma comprobación al leer el save de campaña del disco para la telemetría
                // Review of 18-10 (M-2): a campaign save with any squad recorded (unit 9xx saved as its
                // base) belongs to a modified run even if it was started before this version; its runId is marked.
                // Round 2 (B-3): note, this prefix is only hooked with applied content (Plugin.AplicarParches), so
                // here it only covers that case. Sessions without active modules are covered by Saves.PartidaEnCurso, which does
                // the same check when reading the campaign save from disk for telemetry
                if (SaneadoSaves.EsPartida(json) && SaneadoSaves.TieneGuidApuntado(json, Registro))
                    Registro.MarcarPartidaModificada(SaneadoSaves.FirmaPartida(json));
                if (!Plugin.Operativo) return;
                string restaurado = json;
                // Unidades 9xx: sin ninguna registrada (mod oficial desactivado) se quedan como su unidad base
                // 9xx units: with none registered (official mod disabled) they stay as their base unit
                if (Tabla != null && UnidadGuardia.AlgunaRegistrada() && !SimularSinUnidades)
                    restaurado = SaneadoSaves.DesdeDisco(json, Tabla, Registro);
                // F2: raza nueva de la ciudad o del campo de batalla (si se apuntó al escribir y la firma cuadra). Va aparte de
                // las unidades (0.5.1): solo necesita la tabla de razas (facciones activas) y el registro
                // F2: new city or battlefield race (if it was recorded on write and the signature matches). It goes apart from
                // the units (0.5.1): it only needs the race table (active factions) and the registry
                string conRazas = TablaRazas == null ? restaurado : SaneadoSaves.RazasDesdeDisco(restaurado, TablaRazas, Registro);
                if (conRazas != restaurado)
                {
                    restaurado = conRazas;
                    _razasRestauradas++;
                    if (_razasRestauradas <= 5 || _razasRestauradas % 50 == 0)
                        Plugin.Info($"Raza nueva de ciudad o campo de batalla restaurada al leer ({_razasRestauradas} textos en la sesión).");
                }
                // 0.6.1: lo apuntado que no se ha podido restaurar se conserva en el registro (otra lectura lo restaurará
                // cuando se pueda) y se avisa una vez por caso
                // 0.6.1: whatever was recorded but could not be restored is kept in the registry (another read will restore it
                // when possible) and a warning is shown once per case
                foreach (string pendiente in SaneadoSaves.RazasPendientes(restaurado, TablaRazas, Registro))
                    lock (_pendientesAvisadas)
                        if (_pendientesAvisadas.Add(pendiente))
                            Plugin.Aviso($"Raza nueva apuntada sin restaurar al leer: {pendiente}. Se conserva en el registro para restaurarla después.");
                // 0.6.0: objetos nuevos (solo si están activos: si no, se quedan como su respaldo y el registro no se toca)
                // 0.6.0: new gear (only if active: otherwise they stay as their fallback and the registry is not touched)
                if (TablaObjetos != null && Objetos.ActivosEnCache)
                {
                    string conObjetos = SaneadoObjetos.DesdeDisco(restaurado, TablaObjetos, Registro);
                    if (conObjetos != restaurado)
                    {
                        restaurado = conObjetos;
                        _objetosRestaurados++;
                        if (_objetosRestaurados <= 5 || _objetosRestaurados % 50 == 0)
                            Plugin.Info($"Objetos nuevos restaurados al leer ({_objetosRestaurados} textos en la sesión).");
                    }
                }
                // 0.7.1: héroes nuevos del save de campaña (el activo vuelve; el de un módulo apagado se carga como su prestado)
                // 0.7.1: new heroes from the campaign save (the active one comes back; one from a disabled module is loaded as its borrowed hero)
                string conHeroes = SaneadoSaves.HeroesDesdeDisco(restaurado, TablaHeroes, Registro);
                if (conHeroes != restaurado)
                {
                    restaurado = conHeroes;
                    _heroesRestaurados++;
                    if (_heroesRestaurados <= 5 || _heroesRestaurados % 50 == 0)
                        Plugin.Info($"Héroe nuevo del save de campaña restaurado (o cargado como su prestado si su módulo está apagado) al leer ({_heroesRestaurados} textos en la sesión).");
                }
                // 0.7.5: historial de partidas y registro de batallas (los de módulos activos vuelven)
                // 0.7.5: run history and battle log (those from active modules come back)
                string conHistorial = SaneadoSaves.HistorialDesdeDisco(restaurado, TablaHeroes, TablaRazas, Registro);
                if (conHistorial != restaurado)
                {
                    restaurado = conHistorial;
                    _historialRestaurado++;
                    if (_historialRestaurado <= 5 || _historialRestaurado % 50 == 0)
                        Plugin.Info($"Héroes y razas nuevos del historial de partidas restaurados al leer ({_historialRestaurado} textos en la sesión).");
                }
                // 0.7.4: hechizos propios de los módulos activos (los de un módulo apagado se quedan apuntados)
                // 0.7.4: custom spells from active modules (those from a disabled module stay recorded)
                if (HechizoDisponible != null)
                {
                    string conHechizos = SaneadoHechizos.DesdeDisco(restaurado, Registro, HechizoDisponible);
                    if (conHechizos != restaurado)
                    {
                        restaurado = conHechizos;
                        _hechizosRestaurados++;
                        if (_hechizosRestaurados <= 5 || _hechizosRestaurados % 50 == 0)
                            Plugin.Info($"Hechizos propios restaurados al leer ({_hechizosRestaurados} textos en la sesión).");
                    }
                }
                if (restaurado != json)
                {
                    json = restaurado;
                    _lecturasRestauradas++;
                    if (_lecturasRestauradas <= 5 || _lecturasRestauradas % 50 == 0)
                        Plugin.Info($"Unidades nuevas restauradas al leer ({_lecturasRestauradas} textos en la sesión).");
                }
            }
            catch (Exception e) { Plugin.Error("AntesDeFromJson: " + e.Message); }
        }

        // -------------------------------------------------------------------
        // Sección: clasificaciones online
        // Section: online leaderboards
        // -------------------------------------------------------------------
        // Prefix de LeaderboardClient.Submit (instancia de Harmony propia, que se aplica con cualquier módulo instalado):
        // 0.7.1: con cualquier módulo de Zero aplicado en la sesión (efectos, contenido o Economía) la partida es modificada
        // y no se envía (false = no ejecutar). Ya no depende de que la fábrica esté operativa ni de las unidades registradas.
        // Revisión del 18-10 (M-2): también se bloquea, sin módulos aplicados en esta sesión, si el registro dice que esa
        // partida (runId) se jugó con módulos (RegistroGuardias.BloquearOnline). Con módulos instalados el parche ya estaba;
        // sin ninguno instalado, Plugin.Load lo pone si queda el registro de saves.
        // Prefix of LeaderboardClient.Submit (own Harmony instance, applied with any module installed):
        // 0.7.1: with any Zero module applied in the session (effects, content or Economía) the run is modified
        // and is not submitted (false = do not execute). It no longer depends on the factory being operational or on the registered units.
        // Review of 18-10 (M-2): it is also blocked, with no modules applied in this session, if the registry says that
        // run (runId) was played with modules (RegistroGuardias.BloquearOnline). With modules installed the patch was already there;
        // with none installed, Plugin.Load sets it if the saves registry remains.
        public static bool AntesDeSubmit(string runId, string kind)
        {
            try
            {
                bool bloquear = Plugin.CfgBloquearClasificaciones != null && Plugin.CfgBloquearClasificaciones.Value;
                if (!RegistroGuardias.BloquearOnline(bloquear, Nucleo.PartidaModificada, Registro, runId)) return true;
                if (!_clasificacionAvisada)
                {
                    _clasificacionAvisada = true;
                    if (Nucleo.PartidaModificada) Plugin.Info($"Clasificaciones: envío bloqueado ({kind}) porque la partida usa módulos de Zero ({Nucleo.ModulosActivosTexto()}).");
                    else Plugin.Info($"Clasificaciones: envío bloqueado ({kind}) porque la partida {runId} se jugó con módulos de Zero (apuntada en el registro de saves).");
                }
                return false;
            }
            catch { return false; }
        }

        // -------------------------------------------------------------------
        // Sección: partidas en línea (0.7.4, decisión de Zero 01-11-2026: sin soporte en cooperativo ni versus)
        // Section: online runs (0.7.4, Zero's decision 01-11-2026: no support in co-op or versus)
        // -------------------------------------------------------------------
        // Revisión de la 0.7.4 (ronda 2): el bloqueo solo en StartLink llegaba tarde (la sala ya existía y, en la partida rápida
        // o clasificatoria, un desconocido sin mods se emparejaba y se quedaba colgado). Ahora se corta en la ENTRADA de cada
        // camino (interop 1.11.1, TJ.Versus.Net): QuickBattleQueue.Start (rápida y clasificatoria), VersusLobby.HostCustomAsync
        // (sala propia), HostCoopAsync (cooperativo), JoinAsync (unirse a una sala), OnJoinRequested (invitación de Steam) y
        // VersusRejoin.Resume (volver a una partida). StartLink queda como última red y, si bloquea ahí, sale de la sala
        // (VersusLobby.Leave). El rechazo propio del juego en el saludo (NetRefusal.Mods / ModsInRanked) no se toca.
        // 0.7.5 (revisión total del 02-11): HostCoopAsync va copiado (inlining) dentro de CoopSession.InviteFriend en la 1.11.1,
        // así que el cooperativo como anfitrión se corta en InviteFriend (y, si no, en StartLink). VersusRejoin.Resume lo llama
        // el menú principal en CADA carga: se devuelve false (nada que retomar) sin contarlo ni avisar como bloqueo. Y las dos
        // rutas de línea de comandos (QuickBattleQueue/VersusNet.BootFromCommandLine) también se cortan.
        // Texto del log (BepInEx), siempre en español como el resto del log; lo que ve el jugador es TextoAvisoOnline()
        // 0.7.4 review (round 2): blocking only in StartLink came too late (the lobby already existed and, in quick or ranked
        // matches, a stranger without mods got paired and was left hanging). Now it is cut at the ENTRY of each
        // path (interop 1.11.1, TJ.Versus.Net): QuickBattleQueue.Start (quick and ranked), VersusLobby.HostCustomAsync
        // (own lobby), HostCoopAsync (co-op), JoinAsync (join a lobby), OnJoinRequested (Steam invite) and
        // VersusRejoin.Resume (return to a run). StartLink remains as the last safety net and, if it blocks there, leaves the lobby
        // (VersusLobby.Leave). The game's own rejection in the handshake (NetRefusal.Mods / ModsInRanked) is not touched.
        // 0.7.5 (full review of 02-11): HostCoopAsync is inlined inside CoopSession.InviteFriend in 1.11.1,
        // so co-op as host is cut in InviteFriend (and, failing that, in StartLink). VersusRejoin.Resume is called
        // by the main menu on EVERY load: it returns false (nothing to resume) without counting it or warning as a block. And the two
        // command-line routes (QuickBattleQueue/VersusNet.BootFromCommandLine) are also cut.
        // Log text (BepInEx), always in Spanish like the rest of the log; what the player sees is TextoAvisoOnline()
        private const string TextoBloqueoOnline = "Con mods de Zero activos no se puede jugar en línea";
        // 03-11-2026 (revisión de idiomas, hallazgo 1): aviso en pantalla, traducido. Clave que el generador escribe en los 9
        // módulos (es y en; con el juego en otro idioma el juego cae al «en»). Si el módulo instalado es anterior y no la
        // trae, se usa el respaldo de aquí en el idioma del juego (español si el juego está en español, inglés si no)
        // 03-11-2026 (language review, finding 1): on-screen notice, translated. Key that the generator writes in the 9
        // modules (es and en; with the game in another language the game falls back to «en»). If the installed module is older and does not
        // include it, the fallback here is used in the game's language (Spanish if the game is in Spanish, English otherwise)
        internal const string ClaveAvisoOnline = "zeroMultijugadorBloqueado";
        private const string AvisoOnlineEs = "Con mods de Zero activos no se puede jugar en línea. Desactívalos en el menú de mods y reinicia el juego.";
        private const string AvisoOnlineEn = "Online play is disabled while Zero's mods are enabled. Disable them in the mod menu and restart the game.";
        // Hora del último aviso en pantalla: uno por intento (si un mismo clic pasa por dos entradas, no salen dos avisos)
        // (Environment.TickCount64: reloj monótono en milisegundos; el valor inicial deja pasar el primer aviso)
        // Time of the last on-screen notice: one per attempt (if a single click goes through two entries, two notices do not appear)
        // (Environment.TickCount64: monotonic clock in milliseconds; the initial value lets the first notice through)
        private static long _ultimoAvisoPantalla = long.MinValue / 2;
        // 03-11-2026 (revisión del 03-11, hallazgo 6): momento del último aviso de CADA entrada (las claves son los textos
        // fijos de BloquearOnline: «partida rápida», «sala propia de versus»...). Si el juego reintenta la misma entrada en
        // bucle (StartLink, unirse a una sala...), el aviso sale una vez cada 15 s y no cada 2 s; un clic en otra entrada
        // sigue avisando enseguida. Decisión: volver a pulsar la misma entrada antes de 15 s no repite el aviso (queda el log)
        // 03-11-2026 (review of 03-11, finding 6): time of the last notice for EACH entry (the keys are the fixed
        // texts of BloquearOnline: «partida rápida», «sala propia de versus»...). If the game retries the same entry in a
        // loop (StartLink, joining a lobby...), the notice appears once every 15 s and not every 2 s; a click on another entry
        // still warns immediately. Decision: pressing the same entry again within 15 s does not repeat the notice (the log remains)
        private static readonly Dictionary<string, long> _ultimoAvisoPorEntrada = new Dictionary<string, long>();
        private const long AvisoMismaEntradaMs = 15000;
        private static readonly HashSet<string> _bloqueosAvisados = new HashSet<string>();
        /// <summary>Veces que se ha bloqueado una entrada en línea en esta sesión (para la autoprueba).</summary>
        /// Number of times an online entry has been blocked in this session (for the self-test).
        public static int BloqueosOnline { get; private set; }

        /// <summary>
        /// ¿Hay que bloquear el juego en línea? Con Multijugador.Bloquear = true y módulos de Zero aplicados. Si no se puede
        /// saber si hay módulos aplicados, se deja pasar (sin módulos no hay nada que proteger; ronda 2: antes fallaba cerrado).
        /// Should online play be blocked? With Multijugador.Bloquear = true and Zero modules applied. If it cannot be
        /// known whether modules are applied, it is let through (without modules there is nothing to protect; round 2: it used to fail closed).
        /// </summary>
        internal static bool BloquearOnline(string donde) => BloquearOnline(donde, true);

        // <contar> = false (0.7.5): decide igual, pero no suma al contador ni avisa (VersusRejoin.Resume, que el menú llama siempre)
        // <pantalla> = false (0.7.10, 17-11-2026): sin aviso en pantalla, solo el log. Para las dos entradas por línea de
        // comandos (BootFromCommandLine), que el juego 1.11.12 llama en el arranque, cuando NotificationManager ya existe pero
        // el juego aún no ha cargado (tablas de texto sin cargar): el aviso en ese momento dejaba el juego colgado al
        // arrancar (3 de 3 arranques con Multijugador.Bloquear = true; con false arrancaba). En el arranque no hay ningún clic
        // del jugador al que contestar, así que basta con el log
        // <contar> = false (0.7.5): decides the same, but does not add to the counter or notify (VersusRejoin.Resume, which the menu always calls)
        // <pantalla> = false (0.7.10, 17-11-2026): no on-screen notice, only the log. For the two command-line
        // entry points (BootFromCommandLine), which game 1.11.12 calls at startup, when NotificationManager already exists but
        // the game has not finished loading (text tables not loaded): the notice at that moment left the game hanging on
        // startup (3 of 3 startups with Multijugador.Bloquear = true; with false it started). At startup there is no player
        // click to respond to, so the log is enough
        internal static bool BloquearOnline(string donde, bool contar, bool pantalla = true)
        {
            bool modificada;
            try { modificada = Nucleo.PartidaModificada; }
            catch { return false; }
            if (!modificada) return false;
            bool bloquear;
            try { bloquear = Plugin.CfgBloquearMultijugador == null || Plugin.CfgBloquearMultijugador.Value; }
            catch { bloquear = true; }
            if (!bloquear) return false;
            if (!contar) return true;
            BloqueosOnline++;
            try
            {
                bool primera;
                lock (_bloqueosAvisados) primera = _bloqueosAvisados.Add(donde);
                if (primera || BloqueosOnline % 20 == 0)
                    Plugin.Aviso($"Multijugador: {TextoBloqueoOnline} ({donde} bloqueado; módulos activos: " +
                                 $"{Nucleo.ModulosActivosTexto()}). Para jugar en línea, desactiva los módulos en el " +
                                 "menú de mods y reinicia (o pon Multijugador.Bloquear = false, sin soporte).");
            }
            catch { }
            // Aparte y dentro de un try: si el tipo de los avisos cambia, el fallo de JIT sale aquí y el bloqueo sigue
            // Separate and inside a try: if the type of the notices changes, the JIT failure surfaces here and the block continues
            if (pantalla) { try { AvisarEnPantalla(donde); } catch { } }
            return true;
        }

        /// <summary>
        /// Texto traducido del aviso del bloqueo en línea: el del módulo (clave zeroMultijugadorBloqueado, en el idioma del
        /// juego, o en inglés si el idioma no es es/en) o, si el módulo no la trae, el respaldo del núcleo. Nunca lanza.
        /// Translated text of the online block notice: the module's (key zeroMultijugadorBloqueado, in the game's
        /// language, or in English if the language is not es/en) or, if the module does not include it, the core fallback. Never throws.
        /// </summary>
        internal static string TextoAvisoOnline()
        {
            string t = null;
            try { t = Memori.Localization.LocalizationManager.Instance?.GetText(ClaveAvisoOnline); } catch { }
            bool espanol = false;
            if (!LogicaAccionesHeroes.TextoValido(t, ClaveAvisoOnline))
                try { espanol = AccionesHeroes.JuegoEnEspanol(); } catch { }
            return LogicaAccionesHeroes.Texto(t, ClaveAvisoOnline, AvisoOnlineEs, AvisoOnlineEn, espanol);
        }

        // Enseña el aviso con el aviso de error del juego (NotificationManager.ErrorNotification: el mismo que usan sus
        // paneles del menú principal, p. ej. PlayPanel, ModListPanel y WarbandPanel, xref del 03-11-2026; también
        // AccionesHeroes.Notificar). Uno por intento (2 s entre avisos) y, por entrada, uno cada AvisoMismaEntradaMs. Si
        // el gestor de avisos no existe todavía (arranque por línea de comandos) o falla, solo queda el log. Solo desde el
        // hilo principal (la interfaz de Unity no se toca desde otro hilo); 03-11-2026 (revisión del 03-11, hallazgo 5): si
        // el hilo principal aún no está apuntado (HiloPrincipal = -1, antes de Load), tampoco: solo el log. Nunca lanza
        // Shows the notice with the game's error notification (NotificationManager.ErrorNotification: the same one its main menu
        // panels use, e.g. PlayPanel, ModListPanel and WarbandPanel, xref of 03-11-2026; also
        // AccionesHeroes.Notificar). One per attempt (2 s between notices) and, per entry, one every AvisoMismaEntradaMs. If
        // the notification manager does not exist yet (command-line startup) or fails, only the log remains. Only from the
        // main thread (Unity's UI must not be touched from another thread); 03-11-2026 (review of 03-11, finding 5): if
        // the main thread is not yet recorded (HiloPrincipal = -1, before Load), same: only the log. Never throws
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void AvisarEnPantalla(string donde)
        {
            try
            {
                if (Plugin.HiloPrincipal == -1 || Environment.CurrentManagedThreadId != Plugin.HiloPrincipal) return;
                // reloj monótono (milisegundos desde el arranque): un cambio de hora del sistema no deja los avisos mudos
                // monotonic clock (milliseconds since startup): a system time change does not leave the notices silent
                long ahora = Environment.TickCount64;
                if (ahora - _ultimoAvisoPantalla < 2000) return;
                lock (_ultimoAvisoPorEntrada)
                    if (_ultimoAvisoPorEntrada.TryGetValue(donde ?? "", out var previo) && ahora - previo < AvisoMismaEntradaMs)
                        return;
                var gestor = Memori.Notifications.NotificationManager.Instance;
                if (gestor == null) return;
                gestor.ErrorNotification(TextoAvisoOnline());
                // las marcas, solo si el aviso salió (si ErrorNotification lanza, el siguiente intento vuelve a probar)
                // the marks, only if the notice was shown (if ErrorNotification throws, the next attempt tries again)
                _ultimoAvisoPantalla = ahora;
                lock (_ultimoAvisoPorEntrada) _ultimoAvisoPorEntrada[donde ?? ""] = ahora;
            }
            catch (Exception e)
            {
                bool primera;
                lock (_bloqueosAvisados) primera = _bloqueosAvisados.Add("aviso en pantalla");
                if (primera) Plugin.Aviso("Multijugador: no se pudo enseñar el aviso del juego (" + e.Message + "); queda en el log.");
            }
        }

        /// <summary>Prefix de QuickBattleQueue.Start(ranked): no entra en la cola; la cola queda en «Failed» con el motivo.</summary>
        /// Prefix of QuickBattleQueue.Start(ranked): does not enter the queue; the queue is left in «Failed» with the reason.
        public static bool AntesDeQuickBattleStart(bool __0)
        {
            if (!BloquearOnline(__0 ? "partida clasificatoria" : "partida rápida")) return true;
            try { FallarColaRapida(); } catch (Exception e) { Plugin.Aviso("Multijugador: no se pudo marcar la cola como fallida (" + e.Message + ")."); }
            return false;
        }

        // Pone la cola rápida en su estado de fallo del juego, con el texto traducido como motivo (el panel de 1.11.1 enseña el
        // texto de su Problem, «La búsqueda se detuvo…»; el motivo queda por si el juego lo enseña en otra versión). Aparte y
        // sin inlining: si el tipo cambia, el fallo de JIT sale aquí y lo recoge el try del llamador
        // Puts the quick queue in the game's failure state, with the translated text as the reason (the 1.11.1 panel shows the
        // text of its Problem, «La búsqueda se detuvo…»; the reason is kept in case the game shows it in another version). Separate and
        // without inlining: if the type changes, the JIT failure surfaces here and is caught by the caller's try
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void FallarColaRapida() =>
            TJ.Versus.Net.QuickBattleQueue.Set(TJ.Versus.Net.QuickBattleQueue.State.Failed, TJ.Versus.Net.QuickBattleQueue.Problem.Stopped, TextoAvisoOnline());

        /// <summary>Prefix de VersusLobby.HostCustomAsync: sin sala propia (devuelve una tarea ya terminada con false).</summary>
        /// Prefix of VersusLobby.HostCustomAsync: no own lobby (returns an already completed task with false).
        public static bool AntesDeHostCustom(ref Il2CppSystem.Threading.Tasks.Task<bool> __result) => TareaFalsa("sala propia de versus", ref __result);

        /// <summary>Prefix de VersusLobby.HostCoopAsync: sin sala de cooperativo.</summary>
        /// Prefix of VersusLobby.HostCoopAsync: no co-op lobby.
        public static bool AntesDeHostCoop(ref Il2CppSystem.Threading.Tasks.Task<bool> __result) => TareaFalsa("sala de cooperativo", ref __result);

        /// <summary>Prefix de VersusLobby.JoinAsync(lobbyId): no se une a la sala.</summary>
        /// Prefix of VersusLobby.JoinAsync(lobbyId): does not join the lobby.
        public static bool AntesDeJoin(ref Il2CppSystem.Threading.Tasks.Task<bool> __result) => TareaFalsa("unirse a una sala", ref __result);

        // Bloqueo de un método async que devuelve Task<bool>: sin llamar al original, una tarea terminada con false (lo mismo que
        // si la sala no se hubiera podido crear o unir). Si no se puede crear esa tarea, se deja pasar: StartLink es la red
        // Block of an async method that returns Task<bool>: without calling the original, a completed task with false (the same as
        // if the lobby could not have been created or joined). If that task cannot be created, it is let through: StartLink is the safety net
        private static bool TareaFalsa(string donde, ref Il2CppSystem.Threading.Tasks.Task<bool> __result)
        {
            if (!BloquearOnline(donde)) return true;
            try
            {
                var tarea = TareaTerminada();
                if (tarea == null) return true;
                __result = tarea;
                return false;
            }
            catch (Exception e)
            {
                bool primera;
                lock (_bloqueosAvisados) primera = _bloqueosAvisados.Add("tarea");
                if (primera)
                    Plugin.Aviso($"Multijugador: no se pudo cortar «{donde}» en la entrada ({e.GetType().Name}: {e.Message}); queda el bloqueo de StartLink.");
                return true;
            }
        }

        /// <summary>Tarea del juego ya terminada con false (Task.FromResult). Aparte y sin inlining. La usa también la autoprueba.</summary>
        /// Game task already completed with false (Task.FromResult). Separate and without inlining. The self-test also uses it.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static Il2CppSystem.Threading.Tasks.Task<bool> TareaTerminada() => Il2CppSystem.Threading.Tasks.Task.FromResult<bool>(false);

        /// <summary>Prefix de VersusLobby.OnJoinRequested(lobbyId) (invitación aceptada desde Steam): no se entra.</summary>
        /// Prefix of VersusLobby.OnJoinRequested(lobbyId) (invite accepted from Steam): it does not enter.
        public static bool AntesDeOnJoinRequested() => !BloquearOnline("invitación de Steam");

        /// <summary>
        /// Prefix de VersusRejoin.Resume(): no se vuelve a una partida en línea a medias (devuelve false). 0.7.5 (hallazgo 34):
        /// el menú principal lo llama en cada carga (MainMenu.AddMultiplayerButton), así que NO cuenta como bloqueo ni avisa:
        /// devolver false es lo mismo que «no hay nada que retomar». Solo una línea informativa por sesión.
        /// Prefix of VersusRejoin.Resume(): an online run in progress is not resumed (returns false). 0.7.5 (finding 34):
        /// the main menu calls it on every load (MainMenu.AddMultiplayerButton), so it does NOT count as a block or warn:
        /// returning false is the same as "nothing to resume". Only one informational line per session.
        /// </summary>
        public static bool AntesDeRejoinResume(ref bool __result)
        {
            if (!BloquearOnline("volver a una partida en línea", false)) return true;
            if (!_rejoinInformado)
            {
                _rejoinInformado = true;
                Plugin.Info("Multijugador: con módulos de Zero no se retoma ninguna partida en línea a medias (el menú lo consulta al cargar; no es un intento de jugar en línea).");
            }
            __result = false;
            return false;
        }
        private static bool _rejoinInformado;

        /// <summary>Prefix de CoopSession.InviteFriend() (0.7.5, hallazgo 33): sin sala de cooperativo (HostCoopAsync va copiado dentro).</summary>
        /// Prefix of CoopSession.InviteFriend() (0.7.5, finding 33): no co-op room (HostCoopAsync is copied inside).
        public static bool AntesDeInviteFriend() => !BloquearOnline("sala de cooperativo (invitar a un amigo)");

        /// <summary>Prefix de QuickBattleQueue.BootFromCommandLine() (0.7.5, hallazgo 36): sin cola por línea de comandos.</summary>
        /// Prefix of QuickBattleQueue.BootFromCommandLine() (0.7.5, finding 36): no command-line queue.
        // 0.7.10: sin aviso en pantalla (se llama en el arranque; ver BloquearOnline)
        // 0.7.11: se sigue bloqueando SIEMPRE (como la 0.7.10: si una build nueva añade otra opción de arranque, no entra sin
        // bloqueo), pero el aviso del log y el contador solo cuando el arranque pide la cola («-versus-queue»). El juego llama
        // a este método en CADA arranque y, sin esa opción, no hace nada (desensamblado de la build 26749277, juego 1.11.17):
        // hasta la 0.7.10 eso dejaba en cada arranque un aviso de «bloqueado» sin que nadie hubiera pedido nada
        // 0.7.10: no on-screen warning (called at startup; see BloquearOnline)
        // 0.7.11: still always blocked (like 0.7.10: if a new build adds another startup option, it does not get through without
        // blocking), but the log warning and counter only when the startup requests the queue («-versus-queue»). The game calls
        // this method on EVERY startup and, without that option, does nothing (disassembled from build 26749277, game 1.11.17):
        // up to 0.7.10 that left a «blocked» warning on every startup even though nobody had requested anything
        public static bool AntesDeBootColaRapida()
        {
            bool pide = LogicaInstalacion.PideEnLinea(ArgumentosArranque(), LogicaInstalacion.OpcionCola);
            return !BloquearOnline("cola en línea por línea de comandos", pide, false);
        }

        /// <summary>Prefix de VersusNet.BootFromCommandLine() (0.7.5, hallazgo 36): sin enlace por línea de comandos.</summary>
        /// Prefix of VersusNet.BootFromCommandLine() (0.7.5, finding 36): no command-line link.
        // 0.7.10: sin aviso en pantalla (se llama en el arranque; ver BloquearOnline)
        // 0.7.11: bloqueado siempre; aviso y contador solo si el arranque pide el enlace («-versus-host» o «-versus-join»)
        // 0.7.10: no on-screen warning (called at startup; see BloquearOnline)
        // 0.7.11: always blocked; warning and counter only if the startup requests the link («-versus-host» or «-versus-join»)
        public static bool AntesDeBootVersusNet()
        {
            bool pide = LogicaInstalacion.PideEnLinea(ArgumentosArranque(), LogicaInstalacion.OpcionHost, LogicaInstalacion.OpcionJoin);
            return !BloquearOnline("enlace en línea por línea de comandos", pide, false);
        }

        // Argumentos del proceso (los mismos que lee el juego con Environment.GetCommandLineArgs). Si no se pueden leer, se
        // devuelven las opciones en línea para que salga el aviso (el bloqueo va siempre)
        // Process arguments (the same ones the game reads with Environment.GetCommandLineArgs). If they cannot be read, the
        // online options are returned so that the warning appears (the block is always applied)
        private static string[] ArgumentosArranque()
        {
            try { return Environment.GetCommandLineArgs() ?? Array.Empty<string>(); }
            catch { return new[] { LogicaInstalacion.OpcionCola, LogicaInstalacion.OpcionHost }; }
        }

        /// <summary>
        /// Prefix de TJ.Versus.Net.VersusLobby.StartLink(host, ownerId, ranked, coop): última red. Si se llega aquí con módulos
        /// aplicados (un camino que no se cortó en la entrada), no se abre el enlace (false) y se sale de la sala.
        /// Prefix of TJ.Versus.Net.VersusLobby.StartLink(host, ownerId, ranked, coop): last safety net. If execution gets here with modules
        /// applied (a path that was not cut off at the entry), the link is not opened (false) and the room is left.
        /// </summary>
        public static bool AntesDeStartLink(bool __0, bool __3, ref bool __result)
        {
            if (!BloquearOnline($"enlace de {(__3 ? "cooperativo" : "versus")} como {(__0 ? "anfitrión" : "invitado")}")) return true;
            __result = false;
            try { SalirDeSala(); } catch (Exception e) { Plugin.Aviso("Multijugador: no se pudo salir de la sala (" + e.Message + ")."); }
            return false;
        }

        // Sale de la sala del juego (VersusLobby.Leave). Aparte y sin inlining, como FallarColaRapida
        // Leaves the game's room (VersusLobby.Leave). Separate and without inlining, like FallarColaRapida
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void SalirDeSala() => TJ.Versus.Net.VersusLobby.Leave();

        // -------------------------------------------------------------------
        // Sección: telemetría del juego (0.7.2, decisión de Zero)
        // Section: game telemetry (0.7.2, Zero's decision)
        // -------------------------------------------------------------------
        // Prefixes de Memori.Analytics.AnalyticsService.Record (por ahí pasan todos los eventos: GameEventTracker con
        // runStarted, runEnded, nodeCompleted, battleEnded..., y las excepciones de ExceptionTracker), de
        // HttpAnalyticsBackend.Record (por si algo llama al backend directamente) y de HttpAnalyticsBackend.Send (el envío
        // real al servidor; también la cola guardada en disco de sesiones anteriores). false = no se ejecuta el original.
        // Con algún módulo de Zero aplicado y Online.BloquearTelemetria = true se bloquea todo (también los informes de
        // fallo: podrían ser fallos del mod). Los eventos que no se apuntan no se recuperan.
        // Revisión del 18-10 (M-2): sin módulos aplicados también se bloquean los eventos de la partida en curso (runId del
        // campaignSaveData.json en disco) si está apuntada en el registro como jugada con módulos.
        // Revisión externa del 18-10 (P-B): en ese caso sin módulos, los informes de fallo del juego (exceptionSeen y
        // bugReportSubmitted, RegistroGuardias.EsInformeDeFallo) pasan, y el envío de la cola no se corta (solo tiene lo
        // que dejó pasar el filtro de eventos). Así el núcleo no tapa los fallos del juego.
        // Fallan en blando: ante cualquier error dejan hacer al juego (true).
        // Prefixes of Memori.Analytics.AnalyticsService.Record (all events go through there: GameEventTracker with
        // runStarted, runEnded, nodeCompleted, battleEnded..., and the exceptions of ExceptionTracker), of
        // HttpAnalyticsBackend.Record (in case something calls the backend directly) and of HttpAnalyticsBackend.Send (the real
        // sending to the server; also the queue saved on disk from previous sessions). false = the original is not executed.
        // With any Zero module applied and Online.BloquearTelemetria = true, everything is blocked (also the crash
        // reports: they could be mod failures). Events that are not recorded cannot be recovered.
        // Review of 18-10 (M-2): without modules applied, events of the current run (runId from
        // campaignSaveData.json on disk) are also blocked if it is recorded in the registry as played with modules.
        // External review of 18-10 (P-B): in that case without modules, the game's crash reports (exceptionSeen and
        // bugReportSubmitted, RegistroGuardias.EsInformeDeFallo) pass through, and sending the queue is not cut off (it only has what
        // the event filter let through). That way the core does not hide the game's failures.
        // They fail softly: on any error they let the game proceed (true).

        /// <summary>true si enganchan los dos prefixes de apuntar eventos (lo pone Plugin.AplicarParchesTelemetria).</summary>
        /// true if both event-recording prefixes hook (set by Plugin.AplicarParchesTelemetria).
        internal static bool FiltroDeEventosTelemetria;

        // Prefix de AnalyticsService.Record(eventName, parameters) y HttpAnalyticsBackend.Record(eventName, parameters).
        // El nombre del parámetro (eventName) es el del interop, comprobado en Memori.Analytics.dll y TabletopTavern.Core.dll
        // Prefix of AnalyticsService.Record(eventName, parameters) and HttpAnalyticsBackend.Record(eventName, parameters).
        // The parameter name (eventName) is the interop one, checked in Memori.Analytics.dll and TabletopTavern.Core.dll
        public static bool AntesDeEventoTelemetria(string eventName)
        {
            try
            {
                if (Plugin.CfgBloquearTelemetria == null || !Plugin.CfgBloquearTelemetria.Value) return true;
                bool modulos = Nucleo.PartidaModificada;
                // Solo se mira el save en disco si hace falta (sin módulos, evento de partida y con registro)
                // The save on disk is only inspected if needed (without modules, run event and with registry)
                if (!RegistroGuardias.BloquearTelemetria(true, modulos, Registro, PartidaEnCurso, eventName))
                {
                    if (!modulos && RegistroGuardias.EsInformeDeFallo(eventName) && !_falloAvisado && Registro != null && Registro.PartidaAnclada(PartidaEnCurso()))
                    {
                        _falloAvisado = true;
                        Plugin.Info($"Telemetría: el informe de fallo del juego «{eventName}» pasa aunque la partida en curso se jugó con módulos de Zero (sin módulos cargados en esta sesión los fallos son del juego).");
                    }
                    return true;
                }
                AvisarTelemetria(modulos);
                return false;
            }
            catch { return true; }
        }

        // Prefix de HttpAnalyticsBackend.Send(batch): el POST de la cola al servidor
        // Prefix of HttpAnalyticsBackend.Send(batch): the POST of the queue to the server
        public static bool AntesDeEnvioTelemetria()
        {
            try
            {
                if (Plugin.CfgBloquearTelemetria == null || !Plugin.CfgBloquearTelemetria.Value) return true;
                bool modulos = Nucleo.PartidaModificada;
                if (!RegistroGuardias.BloquearEnvioTelemetria(true, modulos, FiltroDeEventosTelemetria, Registro, PartidaEnCurso)) return true;
                AvisarTelemetria(modulos);
                return false;
            }
            catch { return true; }
        }

        // Una línea en el log la primera vez que se bloquea algo de la telemetría en la sesión
        // One log line the first time any telemetry is blocked in the session
        private static void AvisarTelemetria(bool modulos)
        {
            if (_telemetriaAvisada) return;
            _telemetriaAvisada = true;
            Plugin.Info(modulos
                ? "Telemetría del juego bloqueada en esta sesión (Online.BloquearTelemetria = true y hay módulos de Zero aplicados): no se apunta ni se envía ningún evento, tampoco los informes de fallo."
                : "Telemetría del juego bloqueada: la partida en curso se jugó con módulos de Zero (apuntada en el registro de saves); no se apuntan sus eventos mientras siga siendo la partida en curso. Los informes de fallo del juego (exceptionSeen, bugReportSubmitted) sí se apuntan" +
                  (FiltroDeEventosTelemetria ? " y la cola se envía." : "; la cola se retiene mientras siga esta partida porque no engancharon los dos filtros de eventos."));
        }
        private static bool _telemetriaAvisada;
        private static bool _falloAvisado;

        // Partida en curso para la telemetría sin módulos (M-2) y los logros de ejército: runId (o firma por semilla) del save
        // de campaña del disco. 0.7.5 (hallazgo 5): del slot ACTIVO del juego (principal, diaria, cooperativo o vista del
        // invitado), no siempre de campaignSaveData.json. Cada fichero se mira como mucho una vez por segundo y solo se vuelve
        // a leer si cambia la fecha o el tamaño. null si no hay save de ese slot o no se puede leer.
        // Current run for telemetry without modules (M-2) and army achievements: runId (or seed signature) of the campaign save
        // on disk. 0.7.5 (finding 5): from the game's ACTIVE slot (main, daily, co-op or guest
        // view), not always from campaignSaveData.json. Each file is checked at most once per second and is only re-read
        // if the date or size changes. null if there is no save for that slot or it cannot be read.
        private static readonly object _cerrojoPartida = new object();
        private sealed class PartidaFichero { public string Firma; public DateTime Fecha; public long Tamano = -1; public long Mirada; }
        private static readonly Dictionary<string, PartidaFichero> _partidas = new Dictionary<string, PartidaFichero>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 0.7.5: slot de campaña activo del juego (0 principal, 1 diaria, 2 cooperativo, 3 vista del invitado; -2 = no se
        /// puede leer en este hilo). Lo pone Plugin; null o un fallo = no se sabe.
        /// 0.7.5: the game's active campaign slot (0 main, 1 daily, 2 co-op, 3 guest view; -2 = cannot
        /// be read on this thread). Set by Plugin; null or a failure = unknown.
        /// </summary>
        internal static Func<int> SlotActivo;
        private static int _ultimoSlot = -1;   // último slot leído en el hilo principal (-1 = nunca)
                                               // last slot read on the main thread (-1 = never)
        internal static readonly string[] FicheroPorSlot = { "campaignSaveData.json", "dailySaveData.json", "coopSaveData.json", "coopViewSaveData.json" };

        // Fichero del save de campaña del slot activo; null si no se sabe
        // Campaign save file of the active slot; null if unknown
        private static string FicheroSlotActivo()
        {
            int slot = -1;
            try
            {
                int leido = SlotActivo?.Invoke() ?? -1;
                if (leido >= 0) _ultimoSlot = leido;
                slot = leido == -2 ? _ultimoSlot : leido;
            }
            catch { slot = _ultimoSlot; }
            return slot >= 0 && slot < FicheroPorSlot.Length ? FicheroPorSlot[slot] : null;
        }

        // Juego 1.11.1 (revisión): internal para LogrosFiltrados (logros de ejército sin contexto: ¿la partida en curso está
        // anclada a los módulos?). Si no se sabe el slot activo, la principal (como hasta la 0.7.4)
        // Game 1.11.1 (review): internal for LogrosFiltrados (army achievements without context: is the current run
        // anchored to the modules?). If the active slot is unknown, the main one (as up to 0.7.4)
        internal static string PartidaEnCurso() => FirmaDeFichero(FicheroSlotActivo() ?? FicheroPorSlot[0]);

        /// <summary>¿Se sabe el slot de campaña activo? (si no, LogrosFiltrados mira todas las partidas en curso)</summary>
        /// Is the active campaign slot known? (if not, LogrosFiltrados checks all runs in progress)
        internal static bool SlotActivoConocido => FicheroSlotActivo() != null;

        /// <summary>0.7.5: firmas de las partidas de los cuatro saves de campaña que haya en disco (para decidir ante la duda).</summary>
        /// 0.7.5: signatures of the runs from the four campaign saves on disk (to decide when in doubt).
        internal static List<string> PartidasEnCurso()
        {
            var r = new List<string>();
            foreach (string f in FicheroPorSlot)
            {
                string firma = FirmaDeFichero(f);
                if (firma != null && !r.Contains(firma)) r.Add(firma);
            }
            return r;
        }

        // Firma de la partida de un save de campaña del disco (con caché por fichero)
        // Signature of the run from a campaign save on disk (cached per file)
        private static string FirmaDeFichero(string nombre)
        {
            string carpeta = Plugin.CarpetaDatosJuego;
            if (string.IsNullOrEmpty(carpeta) || string.IsNullOrEmpty(nombre)) return null;
            lock (_cerrojoPartida)
            {
                if (!_partidas.TryGetValue(nombre, out var p)) { p = new PartidaFichero(); _partidas[nombre] = p; }
                long ahora = Environment.TickCount64;
                if (p.Mirada != 0 && ahora - p.Mirada < 1000) return p.Firma;
                p.Mirada = ahora;
                try
                {
                    string ruta = Path.Combine(carpeta, nombre);
                    var info = new FileInfo(ruta);
                    if (!info.Exists) { p.Firma = null; p.Fecha = default; p.Tamano = -1; return null; }
                    // Fecha Y tamaño: si el juego reescribe el save con la misma marca de tiempo, el tamaño suele delatarlo
                    // Date AND size: if the game rewrites the save with the same timestamp, the size usually gives it away
                    DateTime fecha = info.LastWriteTimeUtc;
                    long tamano = info.Length;
                    if (fecha == p.Fecha && tamano == p.Tamano) return p.Firma;
                    using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var sr = new StreamReader(fs);
                    string texto = sr.ReadToEnd();
                    p.Firma = SaneadoSaves.FirmaPartida(texto);
                    p.Fecha = fecha;
                    p.Tamano = tamano;
                    // Revisión del 18-10, ronda 2 (B-3): cierra el hueco de M-2 en sesiones sin módulos activos (el prefix
                    // de lectura no está enganchado entonces). Si el save de campaña tiene alguna escuadra 9xx apuntada en el
                    // registro, su partida se marca como modificada aquí mismo. El juego apunta runEnded (telemetría, que
                    // pasa por aquí) antes de llamar a Submit, así que la marca llega antes del envío a clasificaciones
                    // Review of 18-10, round 2 (B-3): closes the M-2 gap in sessions without active modules (the read
                    // prefix is not hooked then). If the campaign save has any 9xx squad recorded in the
                    // registry, its run is marked as modified right here. The game records runEnded (telemetry, which
                    // goes through here) before calling Submit, so the mark arrives before the submission to leaderboards
                    var registro = Registro;
                    if (registro != null && SaneadoSaves.EsPartida(texto) && SaneadoSaves.TieneGuidApuntado(texto, registro))
                        registro.MarcarPartidaModificada(p.Firma);
                }
                catch (IOException) { }   // el juego lo está escribiendo: vale lo de la última vez
                                          // the game is writing it: use the last value
                return p.Firma;
            }
        }
    }
}
