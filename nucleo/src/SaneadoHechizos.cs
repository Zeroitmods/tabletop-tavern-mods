// SaneadoHechizos.cs
// Saneado de los hechizos propios (0.7.4, ids 1900-1999) en los saves, con la regla de siempre: los saves nunca llevan ids
// nuevos. Mismo patrón que las victorias de los héroes nuevos (SaneadoSaves.EntradasHeroesParaDisco): al escribir se quitan
// y se apuntan en el registro aparte (RegistroGuardias); al leer vuelven solo los de los hechizos que el núcleo tiene
// definidos ahora (módulo activo).
// Dónde acaba el id de un hechizo (código del juego, Map/SaveData/SaveDataHandler.cs y RunRecord.cs; saves reales 1.11.1):
//   - Save de campaña (y su instantánea): «selectedSpells» (3 ranuras; la 1 es la del héroe) y «spellsCast» de RunStats
//     ({"Spell": n, "Casts": c}). Las ranuras se escriben como 0 (Spell.None) y se apunta la posición con la firma de la
//     partida (el juego rehace la ranura 1 con el hechizo del héroe y rellena las otras: SpellLoadout.Sanitize). Los
//     lanzamientos se quitan y se apuntan con la firma.
//   - Save del jugador: «spellsEverCast» (para el logro «Grand Grimoire») se quita y se apunta (solo crece); en cada partida
//     del historial («runHistory», por su runUUID) las cartas «spells» van a 0 con su posición y los lanzamientos «spellsCast»
//     se quitan. Sin el núcleo, el historial no enseña claves crudas ni el juego apunta errores de texto en el log.
//   - Batalla personalizada: «playerCustomBattleSpells» a 0 con su posición (el juego deja la ranura por defecto).
// Sin el núcleo (o sin el módulo) la partida carga igual: el juego tolera ids desconocidos, pero así ni lo necesita.
// Revisión de la 0.7.4 (ronda 2): lo apuntado se FUSIONA con lo nuevo (antes se sustituía): con un módulo apagado, un
// hechizo que no ha vuelto al texto se conserva mientras su posición siga a 0 (cartas) o no esté en la lista (lanzamientos)
// y la partida sea la misma. Límite conocido: si con el módulo apagado el juego reescribe esa ranura (por ejemplo, el
// jugador pone otro hechizo en la ranura 2 o 3, o SpellLoadout.Sanitize rellena la ranura con uno común), la posición ya
// no está a 0 y ese hechizo propio se olvida: al volver a activar el módulo, la ranura se queda con lo que puso el juego.
// 0.7.5 (revisión total del 02-11): las ranuras del save de campaña llevan la partida y el slot (SaneadoSaves.ContextoSave):
// guardar la partida diaria ya no olvida los hechizos apuntados de la principal. Las ranuras viejas sin partida en la clave
// se leen si son de la misma partida (firma) y se pasan a la nueva al guardar. Y la rama sin hechizos propios en el texto
// FUSIONA: se conservan las posiciones apuntadas que siguen a 0 (antes, si una sola había cambiado, se olvidaban todas).
// .NET puro (sin tipos del juego ni de Unity) para poder probarlo fuera del juego (pruebas\PruebasHechizos.cs).
// Autor: ZeroitMods.
// SaneadoHechizos.cs
// Sanitizing of custom spells (0.7.4, ids 1900-1999) in saves, with the usual rule: saves never carry new
// ids. Same pattern as the victories of the new heroes (SaneadoSaves.EntradasHeroesParaDisco): when writing they are removed
// and noted in the separate registry (RegistroGuardias); when reading, only those of the spells that the core has
// currently defined come back (active module).
// Where a spell id ends up (game code, Map/SaveData/SaveDataHandler.cs and RunRecord.cs; real saves 1.11.1):
//   - Campaign save (and its snapshot): «selectedSpells» (3 slots; slot 1 is the hero's) and «spellsCast» of RunStats
//     ({"Spell": n, "Casts": c}). Slots are written as 0 (Spell.None) and the position is noted with the run's
//     signature (the game rebuilds slot 1 with the hero's spell and fills the others: SpellLoadout.Sanitize). The
//     casts are removed and noted with the signature.
//   - Player save: «spellsEverCast» (for the «Grand Grimoire» achievement) is removed and noted (it only grows); in each run
//     of the history («runHistory», by its runUUID) the «spells» cards go to 0 with their position and the «spellsCast» casts
//     are removed. Without the core, the history shows no raw keys and the game logs no text errors in the log.
//   - Custom battle: «playerCustomBattleSpells» to 0 with its position (the game leaves the default slot).
// Without the core (or without the module) the run loads anyway: the game tolerates unknown ids, but this way it doesn't even need to.
// Review of 0.7.4 (round 2): what was noted is MERGED with the new (before it was replaced): with a module turned off, a
// spell that has not returned to the text is kept as long as its position is still 0 (cards) or not in the list (casts)
// and the run is the same. Known limitation: if, with the module turned off, the game rewrites that slot (for example, the
// player puts another spell in slot 2 or 3, or SpellLoadout.Sanitize fills the slot with a common one), the position is no
// longer 0 and that custom spell is forgotten: when the module is turned on again, the slot keeps what the game put there.
// 0.7.5 (full review of 02-11): the campaign save slots carry the run and the slot (SaneadoSaves.ContextoSave):
// saving the daily run no longer forgets the noted spells of the main one. Old slots without a run in the key
// are read if they belong to the same run (signature) and are moved to the new one when saving. And the branch without custom spells in the text
// MERGES: noted positions that are still 0 are kept (before, if just one had changed, all were forgotten).
// Pure .NET (no game or Unity types) so it can be tested outside the game (pruebas\PruebasHechizos.cs).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: saneado de los hechizos propios (ida al disco y vuelta)
    // Section: sanitizing of custom spells (to disk and back)
    // -----------------------------------------------------------------------
    public static class SaneadoHechizos
    {
        // Claves del save que llevan ids de hechizo
        // Save keys that carry spell ids
        internal const string ClaveSeleccion = "selectedSpells";
        internal const string ClaveBatalla = "playerCustomBattleSpells";
        internal const string ClaveLanzadosSiempre = "spellsEverCast";
        internal const string ClaveHistorial = "runHistory";
        internal const string ClaveCartas = "spells";
        internal const string ClaveLanzamientos = "spellsCast";

        // Ranuras del registro
        // Registry slots
        internal const string RanuraSeleccion = "seleccion";
        internal const string RanuraLanzamientos = "lanzamientos";
        internal const string RanuraBatalla = "batalla";
        internal const string PrefijoHistorial = "historial:";

        private static readonly Regex ReSpell = new Regex(@"""Spell""\s*:\s*(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ReRunUuid = new Regex(@"""runUUID""\s*:\s*""([^""]*)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ReIdPropio = new Regex(@"(?<![\w.])19\d\d(?![\w.])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // ¿Trae el texto alguna clave de hechizos? (atajo)
        // Does the text contain any spell key? (shortcut)
        private static bool TieneClaves(string json) =>
            json.IndexOf("\"" + ClaveSeleccion + "\"", StringComparison.Ordinal) >= 0
            || json.IndexOf("\"" + ClaveBatalla + "\"", StringComparison.Ordinal) >= 0
            || json.IndexOf("\"" + ClaveLanzadosSiempre + "\"", StringComparison.Ordinal) >= 0
            || json.IndexOf("\"" + ClaveLanzamientos + "\"", StringComparison.Ordinal) >= 0;

        // 0.7.5: lo apuntado de una ranura para esta partida: la ranura nueva (con partida y slot) o, si no tiene nada, la vieja
        // sin partida en la clave (antes de la 0.7.5) cuando es de esta misma partida. <leida> = la ranura de donde salió
        // 0.7.5: what was recorded for a slot for this run: the new slot (with run and slot) or, if it has nothing, the old one
        // without the run in the key (before 0.7.5) when it belongs to this same run. <leida> = the slot it came from
        private static bool Apuntado(RegistroGuardias registro, string ranura, string legado, string firma, out string f,
                                     out Dictionary<int, int> posiciones, out Dictionary<int, string> entradas, out string leida)
        {
            leida = ranura;
            if (registro.HechizosDe(ranura, out f, out posiciones, out entradas)) return true;
            if (legado != null && legado != ranura && registro.HechizosDe(legado, out f, out posiciones, out entradas) && f == firma) { leida = legado; return true; }
            f = null;
            return false;
        }

        // 0.7.5: tras escribir la ranura nueva, la vieja de esta misma partida ya no hace falta
        // 0.7.5: after writing the new slot, the old one for this same run is no longer needed
        private static void OlvidarLegado(RegistroGuardias registro, string ranura, string legado, string firma)
        {
            if (legado != null && legado != ranura && registro.HechizosDe(legado, out string f, out _, out _) && f == firma)
                registro.FijarHechizos(legado, null, null, null);
        }

        // Id de hechizo de un elemento de lista («1901») o -1
        // Spell id of a list element («1901») or -1
        private static int Id(string elemento) => int.TryParse(elemento, out int v) ? v : -1;

        // ---------------------------------------------------------------
        // Ida al disco
        // Going to disk
        // ---------------------------------------------------------------
        /// <summary>
        /// Devuelve el texto sin ningún hechizo propio (1900-1999) y apunta en el registro lo quitado. Mismo texto si no hay
        /// nada que cambiar. Un texto que ya salió saneado por esta etapa se devuelve tal cual (segunda pasada).
        /// Returns the text without any custom spell (1900-1999) and records what was removed in the registry. Same text if there is
        /// nothing to change. A text that already came out sanitized by this stage is returned as is (second pass).
        /// </summary>
        public static string ParaDisco(string json, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || registro == null || !TieneClaves(json)) return json;
            int previas = registro.EtapasDe(json);
            if ((previas & RegistroGuardias.EtapaHechizos) != 0) return json;
            registro.IniciarLote();
            try
            {
                string res;
                if (SaneadoSaves.EsPartida(json))
                {
                    // 0.7.5: ranuras con partida y slot (y la vieja sin ellos, de respaldo)
                    // 0.7.5: slots with run and slot (and the old one without them, as a fallback)
                    var ctx = ContextoSave.De(json);
                    res = PosicionesParaDisco(json, ClaveSeleccion, ctx.Ranura(RanuraSeleccion), ctx.Legado(RanuraSeleccion), ctx.Firma, registro);
                    res = EntradasParaDisco(res, ClaveLanzamientos, ctx.Ranura(RanuraLanzamientos), ctx.Legado(RanuraLanzamientos), ctx.Firma, registro);
                }
                else
                {
                    res = LanzadosParaDisco(json, registro);
                    res = HistorialParaDisco(res, registro);
                    res = PosicionesParaDisco(res, ClaveBatalla, RanuraBatalla, null, null, registro);
                }
                registro.RecordarSalida(res, previas | RegistroGuardias.EtapaHechizos);
                return res;
            }
            finally { registro.TerminarLote(); }
        }

        // Lista de cartas («clave»: [..]): los ids propios pasan a 0 y su posición se apunta en la ranura. Si no queda
        // ninguno, se conservan de lo apuntado (misma firma) las posiciones que siguen a 0 (texto ya saneado o módulo apagado)
        // y se olvidan las demás (0.7.5: antes, si una sola había cambiado, se olvidaban todas). Solo la primera aparición
        // de la clave en el texto. <legado>: ranura vieja sin partida (0.7.5), o null
        // Card list («key»: [..]): custom ids become 0 and their position is recorded in the slot. If none is
        // left, the positions that are still 0 are kept from what was recorded (same signature) (already sanitized text or module off)
        // and the rest are forgotten (0.7.5: before, if just one had changed, all were forgotten). Only the first occurrence
        // of the key in the text. <legado>: old slot without run (0.7.5), or null
        private static string PosicionesParaDisco(string json, string clave, string ranura, string legado, string firma, RegistroGuardias registro)
        {
            var re = SaneadoSaves.Array(clave);
            var m = re.Match(json);
            if (!m.Success) return json;
            var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
            var posiciones = new Dictionary<int, int>();
            for (int i = 0; i < elementos.Count; i++)
            {
                int v = Id(elementos[i]);
                if (!LogicaHechizos.EsIdPropio(v)) continue;
                posiciones[i] = v;
                elementos[i] = "0";
            }
            bool hayPrev = Apuntado(registro, ranura, legado, firma, out string fPrev, out var prev, out _, out _);
            if (posiciones.Count > 0)
            {
                // Ronda 2: se conservan los apuntados de la misma partida que no han vuelto (módulo apagado) y siguen a 0
                // Round 2: the recorded ones from the same run that have not come back (module off) and are still 0 are kept
                if (hayPrev && fPrev == firma)
                    foreach (var kv in prev)
                        if (!posiciones.ContainsKey(kv.Key) && kv.Key < elementos.Count && elementos[kv.Key] == "0" && !posiciones.ContainsValue(kv.Value))
                            posiciones[kv.Key] = kv.Value;
                registro.FijarHechizos(ranura, firma, posiciones, null);
                OlvidarLegado(registro, ranura, legado, firma);
                return json.Substring(0, m.Groups[2].Index) + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + json.Substring(m.Groups[2].Index + m.Groups[2].Length);
            }
            if (hayPrev)
            {
                // 0.7.5: fusión también sin hechizos propios en el texto: quedan las posiciones de la misma partida que siguen a 0
                // 0.7.5: merge also without custom spells in the text: the positions from the same run that are still 0 remain
                var siguen = new Dictionary<int, int>();
                if (fPrev == firma)
                    foreach (var kv in prev)
                        if (kv.Key < elementos.Count && elementos[kv.Key] == "0") siguen[kv.Key] = kv.Value;
                registro.FijarHechizos(ranura, siguen.Count > 0 ? firma : null, siguen, null);
                OlvidarLegado(registro, ranura, legado, firma);
            }
            return json;
        }

        // Lista de lanzamientos («clave»: [{"Spell": n, "Casts": c}, ...]): las entradas propias se quitan y se apuntan en la
        // ranura. Si no queda ninguna, se conserva lo apuntado solo con la misma firma. Solo la primera aparición.
        // <legado>: ranura vieja sin partida (0.7.5), o null
        // Cast list («key»: [{"Spell": n, "Casts": c}, ...]): custom entries are removed and recorded in the
        // slot. If none is left, what was recorded is kept only with the same signature. Only the first occurrence.
        // <legado>: old slot without run (0.7.5), or null
        private static string EntradasParaDisco(string json, string clave, string ranura, string legado, string firma, RegistroGuardias registro)
        {
            if (!SaneadoSaves.PartirArrayObjetos(json, clave, out int abre, out int cierra, out var objetos)) return json;
            var quedan = new List<string>();
            var quitadas = new Dictionary<int, string>();
            foreach (var (ini, fin) in objetos)
            {
                string objeto = json.Substring(ini, fin - ini);
                int id = HechizoDeEntrada(objeto);
                if (LogicaHechizos.EsIdPropio(id)) quitadas[id] = objeto;
                else quedan.Add(objeto);
            }
            if (quitadas.Count > 0)
            {
                // Ronda 2: se conservan los apuntados de la misma partida que no han vuelto a la lista (módulo apagado)
                // Round 2: the recorded ones from the same run that have not returned to the list (module off) are kept
                if (Apuntado(registro, ranura, legado, firma, out string fPrev, out _, out var prev, out _) && fPrev == firma)
                {
                    var presentes = new HashSet<int>(quedan.Select(HechizoDeEntrada));
                    foreach (var kv in prev)
                        if (!quitadas.ContainsKey(kv.Key) && !presentes.Contains(kv.Key)) quitadas[kv.Key] = kv.Value;
                }
                registro.FijarHechizos(ranura, firma, null, quitadas);
                OlvidarLegado(registro, ranura, legado, firma);
                return json.Substring(0, abre) + SaneadoSaves.UnirObjetos(json, clave, abre, cierra, objetos, quedan) + json.Substring(cierra + 1);
            }
            // Sin propios en el texto: lo de la misma partida se conserva (pasado a la ranura nueva si venía de la vieja);
            // lo de otra partida en ESTA ranura se olvida (con la ranura por partida no debería pasar)
            // No custom ones in the text: what belongs to the same run is kept (moved to the new slot if it came from the old one);
            // what belongs to another run in THIS slot is forgotten (with a per-run slot this should not happen)
            if (Apuntado(registro, ranura, legado, firma, out string f, out _, out var ya, out string leida))
            {
                if (f != firma) registro.FijarHechizos(ranura, null, null, null);
                else if (leida != ranura) { registro.FijarHechizos(ranura, firma, null, ya); OlvidarLegado(registro, ranura, legado, firma); }
            }
            return json;
        }

        // Id de hechizo de una entrada {"Spell": n, ...} o -1
        // Spell id of an entry {"Spell": n, ...} or -1
        private static int HechizoDeEntrada(string objeto)
        {
            var m = ReSpell.Match(objeto);
            return m.Success && int.TryParse(m.Groups[1].Value, out int id) ? id : -1;
        }

        // «spellsEverCast» del save del jugador: los propios se quitan y se apuntan (la lista apuntada solo crece)
        // «spellsEverCast» from the player's save: custom ones are removed and recorded (the recorded list only grows)
        private static string LanzadosParaDisco(string json, RegistroGuardias registro)
        {
            var m = SaneadoSaves.Array(ClaveLanzadosSiempre).Match(json);
            if (!m.Success) return json;
            var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
            var propios = elementos.Select(Id).Where(LogicaHechizos.EsIdPropio).ToList();
            if (propios.Count == 0) return json;
            registro.AnadirHechizosLanzados(propios);
            var quedan = elementos.Where(e => !LogicaHechizos.EsIdPropio(Id(e))).ToList();
            return json.Substring(0, m.Groups[2].Index) + SaneadoSaves.UnirArray(quedan, m.Groups[2].Value) + json.Substring(m.Groups[2].Index + m.Groups[2].Length);
        }

        // Historial de partidas del save del jugador: en cada partida (por su runUUID), cartas a 0 y lanzamientos fuera
        // Run history from the player's save: in each run (by its runUUID), cards set to 0 and casts removed
        private static string HistorialParaDisco(string json, RegistroGuardias registro)
        {
            if (!SaneadoSaves.PartirArrayObjetos(json, ClaveHistorial, out _, out _, out var partidas) || partidas.Count == 0) return json;
            string res = json;
            // De la última a la primera: así las posiciones de las anteriores no se mueven al cambiar una
            // From last to first: this way the positions of the earlier ones do not shift when one changes
            for (int i = partidas.Count - 1; i >= 0; i--)
            {
                var (ini, fin) = partidas[i];
                string objeto = res.Substring(ini, fin - ini);
                string uuid = Uuid(objeto);
                if (uuid == null) continue;
                string ranura = PrefijoHistorial + uuid;
                string nuevo = PosicionesParaDisco(objeto, ClaveCartas, ranura + ":cartas", null, null, registro);
                nuevo = EntradasParaDisco(nuevo, ClaveLanzamientos, ranura + ":lanzamientos", null, null, registro);
                if (!ReferenceEquals(nuevo, objeto) && nuevo != objeto) res = res.Substring(0, ini) + nuevo + res.Substring(fin);
            }
            return res;
        }

        // runUUID de una partida del historial (null si no lo trae)
        // runUUID of a run in the history (null if it has none)
        private static string Uuid(string objeto)
        {
            var m = ReRunUuid.Match(objeto);
            return m.Success && m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : null;
        }

        // ---------------------------------------------------------------
        // Vuelta del disco
        // Return from disk
        // ---------------------------------------------------------------
        /// <summary>
        /// Devuelve los hechizos apuntados a su sitio, solo los que <paramref name="disponible"/> acepta (definidos en un
        /// módulo activo). Mismo texto si no hay nada que cambiar.
        /// Returns the recorded spells to their place, only those that <paramref name="disponible"/> accepts (defined in an
        /// active module). Same text if there is nothing to change.
        /// </summary>
        public static string DesdeDisco(string json, RegistroGuardias registro, Func<int, bool> disponible)
        {
            if (string.IsNullOrEmpty(json) || registro == null || disponible == null || !registro.TieneHechizos || !TieneClaves(json)) return json;
            if (SaneadoSaves.EsPartida(json))
            {
                var ctx = ContextoSave.De(json);
                string res = PosicionesDesdeDisco(json, ClaveSeleccion, ctx.Ranura(RanuraSeleccion), ctx.Legado(RanuraSeleccion), ctx.Firma, registro, disponible);
                return EntradasDesdeDisco(res, ClaveLanzamientos, ctx.Ranura(RanuraLanzamientos), ctx.Legado(RanuraLanzamientos), ctx.Firma, registro, disponible);
            }
            string r = LanzadosDesdeDisco(json, registro, disponible);
            r = HistorialDesdeDisco(r, registro, disponible);
            return PosicionesDesdeDisco(r, ClaveBatalla, RanuraBatalla, null, null, registro, disponible);
        }

        // Cartas: en cada posición apuntada que siga a 0, el hechizo vuelve si está disponible (misma firma)
        // Cards: at each recorded position that is still 0, the spell comes back if it is available (same signature)
        private static string PosicionesDesdeDisco(string json, string clave, string ranura, string legado, string firma, RegistroGuardias registro, Func<int, bool> disponible)
        {
            if (!Apuntado(registro, ranura, legado, firma, out string f, out var posiciones, out _, out _) || f != firma || posiciones.Count == 0) return json;
            var m = SaneadoSaves.Array(clave).Match(json);
            if (!m.Success) return json;
            var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
            bool cambio = false;
            foreach (var kv in posiciones)
            {
                if (kv.Key >= elementos.Count || elementos[kv.Key] != "0" || !disponible(kv.Value)) continue;
                // El mismo hechizo no se repite en otra ranura (el juego no admite duplicados)
                // The same spell is not repeated in another slot (the game does not allow duplicates)
                if (elementos.Contains(kv.Value.ToString())) continue;
                elementos[kv.Key] = kv.Value.ToString();
                cambio = true;
            }
            if (!cambio) return json;
            return json.Substring(0, m.Groups[2].Index) + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + json.Substring(m.Groups[2].Index + m.Groups[2].Length);
        }

        // Lanzamientos: se añaden al final las entradas apuntadas de los hechizos disponibles que falten (misma firma)
        // Casts: the recorded entries for available spells that are missing are appended at the end (same signature)
        private static string EntradasDesdeDisco(string json, string clave, string ranura, string legado, string firma, RegistroGuardias registro, Func<int, bool> disponible)
        {
            if (!Apuntado(registro, ranura, legado, firma, out string f, out _, out var entradas, out _) || f != firma || entradas.Count == 0) return json;
            if (!SaneadoSaves.PartirArrayObjetos(json, clave, out int abre, out int cierra, out var objetos)) return json;
            var presentes = new HashSet<int>();
            var todos = new List<string>();
            foreach (var (ini, fin) in objetos)
            {
                string objeto = json.Substring(ini, fin - ini);
                presentes.Add(HechizoDeEntrada(objeto));
                todos.Add(objeto);
            }
            bool anadida = false;
            foreach (var kv in entradas.OrderBy(x => x.Key))
                if (disponible(kv.Key) && !presentes.Contains(kv.Key)) { todos.Add(kv.Value); anadida = true; }
            if (!anadida) return json;
            return json.Substring(0, abre) + SaneadoSaves.UnirObjetos(json, clave, abre, cierra, objetos, todos) + json.Substring(cierra + 1);
        }

        // «spellsEverCast»: se añaden al final los apuntados disponibles que falten
        // «spellsEverCast»: the available recorded ones that are missing are appended at the end
        private static string LanzadosDesdeDisco(string json, RegistroGuardias registro, Func<int, bool> disponible)
        {
            var apuntados = registro.HechizosLanzados.Where(disponible).ToList();
            if (apuntados.Count == 0) return json;
            var m = SaneadoSaves.Array(ClaveLanzadosSiempre).Match(json);
            if (!m.Success) return json;
            var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
            var faltan = apuntados.Where(id => !elementos.Contains(id.ToString())).ToList();
            if (faltan.Count == 0) return json;
            elementos.AddRange(faltan.Select(id => id.ToString()));
            return json.Substring(0, m.Groups[2].Index) + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + json.Substring(m.Groups[2].Index + m.Groups[2].Length);
        }

        // Historial: cada partida recupera sus cartas y lanzamientos apuntados
        // History: each run gets its recorded cards and casts back
        private static string HistorialDesdeDisco(string json, RegistroGuardias registro, Func<int, bool> disponible)
        {
            if (!SaneadoSaves.PartirArrayObjetos(json, ClaveHistorial, out _, out _, out var partidas) || partidas.Count == 0) return json;
            string res = json;
            for (int i = partidas.Count - 1; i >= 0; i--)
            {
                var (ini, fin) = partidas[i];
                string objeto = res.Substring(ini, fin - ini);
                string uuid = Uuid(objeto);
                if (uuid == null) continue;
                string ranura = PrefijoHistorial + uuid;
                string nuevo = PosicionesDesdeDisco(objeto, ClaveCartas, ranura + ":cartas", null, null, registro, disponible);
                nuevo = EntradasDesdeDisco(nuevo, ClaveLanzamientos, ranura + ":lanzamientos", null, null, registro, disponible);
                if (!ReferenceEquals(nuevo, objeto) && nuevo != objeto) res = res.Substring(0, ini) + nuevo + res.Substring(fin);
            }
            return res;
        }

        // ---------------------------------------------------------------
        // Verificación
        // Verification
        // ---------------------------------------------------------------
        /// <summary>Cuenta los hechizos propios que quedan en las claves de hechizos del texto (debe ser 0 tras ParaDisco).</summary>
        /// Counts the custom spells remaining in the text's spell keys (must be 0 after ParaDisco).
        public static int ContarResiduos(string json)
        {
            if (string.IsNullOrEmpty(json) || !TieneClaves(json) || !ReIdPropio.IsMatch(json)) return 0;
            int n = 0;
            foreach (string clave in new[] { ClaveSeleccion, ClaveBatalla, ClaveLanzadosSiempre, ClaveCartas })
                foreach (Match m in SaneadoSaves.Array(clave).Matches(json))
                    foreach (string e in SaneadoSaves.PartirArray(m.Groups[2].Value))
                        if (LogicaHechizos.EsIdPropio(Id(e))) n++;
            // Lanzamientos {"Spell": n}: todas las apariciones (campaña e historial)
            // Casts {"Spell": n}: all occurrences (campaign and history)
            foreach (Match m in ReSpell.Matches(json))
                if (int.TryParse(m.Groups[1].Value, out int id) && LogicaHechizos.EsIdPropio(id)) n++;
            return n;
        }
    }
}
