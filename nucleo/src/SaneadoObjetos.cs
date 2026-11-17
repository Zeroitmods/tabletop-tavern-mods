// SaneadoObjetos.cs
// Saneado de saves para los objetos nuevos (gear) 700-729 (0.6.0): los ficheros de guardado NUNCA llevan un objeto
// nuevo. Sin el plugin, GearData.GetGear(700) devuelve None, el icono «None» no existe y la casilla queda ocupada por un
// objeto muerto; con el saneado, la partida carga con un objeto de serie parecido (su «respaldo») en su lugar.
// SaneadoObjetos.cs
// Save sanitizing for the new gear 700-729 (0.6.0): save files NEVER carry a new
// item. Without the plugin, GearData.GetGear(700) returns None, the «None» icon does not exist and the slot is left occupied by a
// dead item; with sanitizing, the run loads with a similar vanilla item (its «fallback») in its place.
//
// Al escribir (texto JSON):
//   - Listas con posición estable (Gear, recruitableGear, townLootGearIDs): cada objeto nuevo se escribe como su respaldo
//     (o, si ese ya está en la misma lista, como otro objeto de serie libre) y se apunta en el registro la posición, el id
//     real y el escrito. Por separado para el save normal y la instantánea del nodo («snapShot»: true).
//   - Listas sin posición (SoldGear, brokenGear): se quitan los objetos nuevos y se apunta cuáles eran (se sustituye lo
//     apuntado en cada escritura).
//   - Listas de colección del save del jugador (gearIdsCollected, gearIdsAcknowledged): se quitan y se anotan (solo crece).
//   - Historial de partidas («gear» de cada registro): se cambian por su respaldo sin poder recuperarlos (solo es texto).
//   - 0.7.2: equipo inicial recordado del save del jugador («lastStartingGearId», escalar): un objeto nuevo se escribe
//     como None (0) y el real se apunta sin ancla de partida. None y no el respaldo: el menú lo vuelve a equipar sin mirar
//     si está encontrado (PlayPanel.cs:397), así sin plugin no sale equipado un objeto de serie que el jugador no tiene.
// Al leer: se hace lo inverso con lo apuntado; una posición solo vuelve a ser el objeto nuevo si en el save sigue el
// objeto escrito (si el jugador lo cambió sin plugin, se respeta lo que haya).
// 0.7.0 (módulos): si un módulo con objetos está desactivado, sus objetos no están en la tabla; lo apuntado de ellos en el
// registro se conserva al escribir (no se olvida) para que vuelvan al activar otra vez el módulo.
// 0.7.1: lo apuntado de las listas de la partida (Gear, ofertas, botín, vendidos, rotos) se ancla a su partida (runId del
// save de campaña): no se restaura ni se conserva en otra partida, así no aparecen objetos «fantasma» al empezar otra.
// 0.7.2 (revisión): un texto que ya salió saneado por esta etapa se devuelve sin tocar el registro (memoria de textos
// saneados de RegistroGuardias), así ParaDisco(ParaDisco(x)) no pierde lo apuntado. El equipo inicial recordado sigue
// olvidándose con un objeto de serie si el real está activo (el jugador eligió otro o ninguno): eso solo es correcto
// porque la segunda pasada del mismo texto ya no llega a InicialParaDisco.
// 0.7.5 (revisión total del 02-11): lo apuntado de las listas de la partida va en la ranura de ESA partida y slot
// (SaneadoSaves.ContextoSave: «Gear@principal@<runId>», «Gear_instantanea@diaria@<runId>»...). Guardar la partida diaria
// ya no olvida lo de la principal. Las ranuras viejas sin partida en la clave se siguen leyendo si son de la misma partida
// (o, sin ancla, solo para la principal) y se pasan a la nueva al guardar. Además, el equipo inicial del cooperativo
// («coopStartGear», escalar de la 1.11.1) se escribe como su respaldo sin recuperación (el cooperativo está bloqueado con
// módulos; es solo para que el disco nunca lleve un 7xx).
// .NET puro (sin tipos del juego) para poder probarlo fuera del juego (proyecto pruebas\).
// When writing (JSON text):
//   - Lists with stable positions (Gear, recruitableGear, townLootGearIDs): each new gear item is written as its fallback
//     (or, if that one is already in the same list, as another free stock item) and the registry records the position, the
//     real id and the written one. Separately for the normal save and the node snapshot («snapShot»: true).
//   - Lists without positions (SoldGear, brokenGear): the new gear is removed and the registry records which items they were (what was
//     recorded is replaced on each write).
//   - Collection lists in the player save (gearIdsCollected, gearIdsAcknowledged): they are removed and recorded (it only grows).
//   - Run history («gear» of each record): they are replaced by their fallback with no way to recover them (it is only text).
//   - 0.7.2: remembered starting gear in the player save («lastStartingGearId», scalar): a new gear item is written
//     as None (0) and the real one is recorded without a run anchor. None and not the fallback: the menu equips it again without checking
//     whether it has been found (PlayPanel.cs:397), so without the plugin a stock item the player does not have never shows up equipped.
// When reading: the inverse is done with what was recorded; a position only becomes the new gear again if the save still has
// the written item (if the player changed it without the plugin, whatever is there is respected).
// 0.7.0 (modules): if a module with gear is disabled, its gear is not in the table; what was recorded for it in the
// registry is kept when writing (it is not forgotten) so it comes back when the module is enabled again.
// 0.7.1: what was recorded for the run lists (Gear, offers, loot, sold, broken) is anchored to its run (runId of the
// campaign save): it is not restored or kept in another run, so no «ghost» gear appears when starting another one.
// 0.7.2 (review): a text that already came out sanitized by this stage is returned without touching the registry (memory of texts
// sanitized by RegistroGuardias), so ParaDisco(ParaDisco(x)) does not lose what was recorded. The remembered starting gear keeps
// being forgotten with a stock item if the real one is active (the player chose another or none): this is only correct
// because the second pass of the same text no longer reaches InicialParaDisco.
// 0.7.5 (full review of 02-11): what was recorded for the run lists goes in the slot of THAT run and slot
// (SaneadoSaves.ContextoSave: «Gear@principal@<runId>», «Gear_instantanea@diaria@<runId>»...). Saving the daily run
// no longer forgets the main one's. Old slots without a run in the key are still read if they belong to the same run
// (or, without an anchor, only for the main one) and are moved to the new one on save. Also, the co-op starting gear
// («coopStartGear», scalar from 1.11.1) is written as its fallback with no recovery (co-op is blocked with
// modules; it is only so the disk never carries a 7xx).
// Pure .NET (no game types) so it can be tested outside the game (tests\ project).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: tabla de objetos nuevos (id -> objeto de serie de respaldo)
    // Section: new gear table (id -> fallback stock item)
    // -----------------------------------------------------------------------
    /// <summary>Objetos nuevos (700-729), su respaldo de serie y los objetos de serie que se pueden usar si el respaldo choca.</summary>
    /// New gear (700-729), its stock fallback and the stock items that can be used if the fallback collides.
    public sealed class TablaObjetos
    {
        private readonly Dictionary<int, int> _respaldo;
        private readonly int[] _repuestos;

        /// <summary>Ids nuevos y su respaldo; <paramref name="deSerie"/> = ids del enum GearID del juego (sin None), en orden.</summary>
        /// New ids and their fallback; <paramref name="deSerie"/> = ids of the game's GearID enum (without None), in order.
        public TablaObjetos(IDictionary<int, int> respaldoPorId, IEnumerable<int> deSerie)
        {
            _respaldo = new Dictionary<int, int>(respaldoPorId);
            _repuestos = deSerie.Where(i => i > 0 && !_respaldo.ContainsKey(i)).Distinct().ToArray();
        }

        public int Numero => _respaldo.Count;
        public IEnumerable<int> Ids => _respaldo.Keys;
        public bool EsNuevo(int id) => _respaldo.ContainsKey(id);
        public bool EsNuevo(string texto) => int.TryParse(texto, out int id) && _respaldo.ContainsKey(id);
        public int RespaldoDe(int id) => _respaldo.TryGetValue(id, out int r) ? r : 0;

        /// <summary>Objeto de serie que se escribe en lugar de <paramref name="id"/>: su respaldo o el primero libre.</summary>
        /// Stock item written in place of <paramref name="id"/>: its fallback or the first free one.
        public int Escrito(int id, ICollection<int> ocupados)
        {
            int r = RespaldoDe(id);
            if (r > 0 && !ocupados.Contains(r)) return r;
            foreach (int x in _repuestos) if (!ocupados.Contains(x)) return x;
            return r > 0 ? r : (_repuestos.Length > 0 ? _repuestos[0] : 1);
        }
    }

    // -----------------------------------------------------------------------
    // Sección: transformaciones de ida al disco y vuelta
    // Section: transformations going to disk and back
    // -----------------------------------------------------------------------
    public static class SaneadoObjetos
    {
        // Listas de objetos con posición estable (en el orden en que se tratan: Gear primero, las ofertas lo miran)
        // Gear lists with stable positions (in the order they are processed: Gear first, the offers look at it)
        internal static readonly string[] ListasConPosicion = { "Gear", "recruitableGear", "townLootGearIDs" };
        // Listas sin posición de la partida (se quitan los nuevos y se apunta cuáles)
        // Run lists without positions (the new ones are removed and which ones they were is recorded)
        internal static readonly string[] ListasConjunto = { "SoldGear", "brokenGear" };
        // Listas de colección del save del jugador (se quitan y se anotan; solo crece)
        // Collection lists in the player save (removed and recorded; it only grows)
        internal static readonly string[] ListasColeccion = { "gearIdsCollected", "gearIdsAcknowledged" };
        // Historial: «gear» de cada registro de partida (respaldo sin recuperación)
        // History: «gear» of each run record (fallback with no recovery)
        internal static readonly string[] ListasHistorial = { "gear" };
        // 0.7.2: equipo inicial recordado (escalar del save del jugador); ranura del registro y valor escrito (GearID.None)
        // 0.7.2: remembered starting gear (scalar in the player save); registry slot and written value (GearID.None)
        internal const string ClaveInicial = "lastStartingGearId";
        internal const int EscritoInicial = 0;
        private static readonly Regex ReInicial = new Regex(@"(""lastStartingGearId""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static IEnumerable<string> TodasLasClaves => ListasConPosicion.Concat(ListasConjunto).Concat(ListasColeccion).Concat(ListasHistorial);

        // 0.7.5: equipo inicial del cooperativo (escalar del save de campaña de la 1.11.1): respaldo sin recuperación
        // 0.7.5: co-op starting gear (scalar in the campaign save from 1.11.1): fallback with no recovery
        internal const string ClaveCoop = "coopStartGear";
        private static readonly Regex ReCoop = new Regex(@"(""coopStartGear""\s*:\s*)(-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // 0.7.5: ranura de la que LEER lo apuntado de una lista de la partida: la de esta partida y slot o, si está vacía, la
        // vieja sin partida en la clave (antes de la 0.7.5) cuando es de esta misma partida (o, sin ancla, solo para la
        // partida principal, la única que existía entonces)
        // 0.7.5: slot to READ what was recorded for a run list from: the one for this run and slot or, if it is empty, the
        // old one without a run in the key (before 0.7.5) when it belongs to this same run (or, without an anchor, only for the
        // main run, the only one that existed then)
        private static string RanuraLectura(RegistroGuardias registro, ContextoSave ctx, string clave)
        {
            string nueva = ctx.Ranura(clave);
            if (!ctx.ConLegado || registro.ObjetosPosicion(nueva).Count > 0 || registro.ObjetosLista(nueva).Count > 0) return nueva;
            string vieja = ctx.Legado(clave);
            if (registro.ObjetosPosicion(vieja).Count == 0 && registro.ObjetosLista(vieja).Count == 0) return nueva;
            string apuntada = registro.PartidaObjetos(vieja);
            return apuntada == ctx.Firma || (apuntada == null && ctx.EsPrincipal) ? vieja : nueva;
        }

        // 0.7.1: ¿lo apuntado en la ranura es de la partida del texto? Sin partida en el texto o sin ancla en el registro
        // (registro de la 0.7.0) se acepta, como antes
        // 0.7.1: does what was recorded in the slot belong to the text's run? Without a run in the text or without an anchor in the registry
        // (0.7.0 registry) it is accepted, as before
        private static bool MismaPartida(RegistroGuardias registro, string ranura, string partida)
        {
            if (partida == null) return true;
            string apuntada = registro.PartidaObjetos(ranura);
            return apuntada == null || apuntada == partida;
        }

        // ¿Aparece la clave como array en el texto? (atajo barato antes de las expresiones regulares)
        // Does the key appear as an array in the text? (cheap shortcut before the regular expressions)
        private static bool TieneClave(string json, string clave) => json.IndexOf("\"" + clave + "\"", StringComparison.Ordinal) >= 0;

        // Elementos del primer array de una clave (vacío si no está)
        // Elements of the first array of a key (empty if it is not there)
        private static List<string> PrimerArray(string json, string clave)
        {
            var m = SaneadoSaves.Array(clave).Match(json);
            return m.Success ? SaneadoSaves.PartirArray(m.Groups[2].Value) : new List<string>();
        }

        // ---------------------------------------------------------------
        // Ida al disco
        // Going to disk
        // ---------------------------------------------------------------
        /// <summary>
        /// Devuelve el JSON sin ningún objeto nuevo y apunta en el registro lo necesario para volver. Si una lista con
        /// posición o sin posición del texto no trae objetos nuevos, se olvida lo apuntado para ella solo cuando
        /// <paramref name="olvidarSinObjetos"/> es true (objetos activos); si no, el registro no se toca.
        /// Devuelve el mismo texto si no hay nada que cambiar.
        /// Returns the JSON without any new gear and records in the registry what is needed to restore it. If a list with
        /// or without positions in the text has no new gear, what was recorded for it is forgotten only when
        /// <paramref name="olvidarSinObjetos"/> is true (gear active); otherwise, the registry is not touched.
        /// Returns the same text if there is nothing to change.
        /// </summary>
        public static string ParaDisco(string json, TablaObjetos tabla, RegistroGuardias registro, bool olvidarSinObjetos)
        {
            if (string.IsNullOrEmpty(json) || tabla == null || tabla.Numero == 0 || registro == null) return json;
            var presentes = TodasLasClaves.Where(c => TieneClave(json, c)).ToList();
            bool hayInicial = TieneClave(json, ClaveInicial);
            if (presentes.Count == 0 && !hayInicial && !TieneClave(json, ClaveCoop)) return json;
            // 0.7.2 (revisión): un texto que ya salió saneado por esta etapa se devuelve tal cual. El juego pasa cada save dos
            // veces por el saneado (ToJson y WriteAllText con el mismo texto) y en la segunda pasada ya no hay objetos nuevos:
            // sin esto se olvidaba lo apuntado (Gear, ofertas, botín, vendidos, rotos y lastStartingGearId)
            // 0.7.2 (review): a text that already came out sanitized by this stage is returned as is. The game passes each save twice
            // through the sanitizing (ToJson and WriteAllText with the same text) and on the second pass there is no new gear left:
            // without this, what was recorded was forgotten (Gear, offers, loot, sold, broken and lastStartingGearId)
            int previas = registro.EtapasDe(json);
            if ((previas & RegistroGuardias.EtapaObjetos) != 0) return json;
            // 0.7.1: partida del texto (runId del save de campaña; null en el save del jugador) para anclar lo apuntado.
            // 0.7.5: y su slot (contexto: la ranura lleva partida y slot)
            // 0.7.1: the text's run (runId of the campaign save; null in the player save) to anchor what was recorded.
            // 0.7.5: and its slot (context: the slot carries run and slot)
            var ctx = ContextoSave.De(json);
            string partida = ctx.Firma;
            string res = json;
            registro.IniciarLote();
            try
            {
                // Objetos de serie que lleva la partida (las ofertas no deben repetir uno de ellos al escribir un respaldo)
                // Stock items the run carries (offers must not repeat one of them when writing a fallback)
                var enGear = new HashSet<int>();
                foreach (string e in PrimerArray(json, "Gear")) if (int.TryParse(e, out int g) && !tabla.EsNuevo(g)) enGear.Add(g);

                // 1) Listas con posición: objeto nuevo -> respaldo libre, apuntando posición, real y escrito
                // 1) Lists with positions: new gear -> free fallback, recording position, real and written
                foreach (string clave in ListasConPosicion)
                {
                    if (!presentes.Contains(clave)) continue;
                    string ranura = ctx.Ranura(clave), lectura = RanuraLectura(registro, ctx, clave);
                    res = SaneadoSaves.Array(clave).Replace(res, m =>
                    {
                        var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
                        var ocupados = new HashSet<int>();
                        foreach (string e in elementos) if (int.TryParse(e, out int v) && !tabla.EsNuevo(v)) ocupados.Add(v);
                        if (clave != "Gear") ocupados.UnionWith(enGear);
                        var apuntados = new Dictionary<int, (int, int)>();
                        for (int i = 0; i < elementos.Count; i++)
                        {
                            if (!int.TryParse(elementos[i], out int id) || !tabla.EsNuevo(id)) continue;
                            int escrito = tabla.Escrito(id, ocupados);
                            ocupados.Add(escrito);
                            elementos[i] = escrito.ToString();
                            apuntados[i] = (id, escrito);
                        }
                        // 0.7.0: lo apuntado de objetos de módulos que ahora no están activos (no son de la tabla) se conserva
                        // mientras su posición siga con el objeto escrito, para recuperarlo al volver a activar el módulo
                        // (0.7.1: solo si lo apuntado es de esta misma partida; las marcas de otra partida se olvidan)
                        // 0.7.0: what was recorded for module gear that is not currently active (not in the table) is kept
                        // as long as its position still has the written item, to recover it when the module is enabled again
                        // (0.7.1: only if what was recorded is from this same run; marks from another run are forgotten)
                        int conservados = 0;
                        if (olvidarSinObjetos && MismaPartida(registro, lectura, partida))
                            foreach (var kv in registro.ObjetosPosicion(lectura))
                                if (!tabla.EsNuevo(kv.Value.real) && !apuntados.ContainsKey(kv.Key) && kv.Key < elementos.Count
                                    && elementos[kv.Key] == kv.Value.escrito.ToString()) { apuntados[kv.Key] = kv.Value; conservados++; }
                        if (apuntados.Count > 0 || olvidarSinObjetos)
                        {
                            registro.FijarObjetosPosicion(ranura, apuntados, partida);
                            // 0.7.5: lo leído de la ranura vieja ya está en la nueva
                            // 0.7.5: what was read from the old slot is already in the new one
                            if (lectura != ranura) registro.FijarObjetosPosicion(lectura, new Dictionary<int, (int, int)>());
                        }
                        if (apuntados.Count == conservados) return m.Value;
                        return apuntados.Count == 0 ? m.Value : m.Groups[1].Value + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value;
                    });
                }

                // 2) Listas sin posición: se quitan los nuevos y se apunta cuáles (se sustituye lo apuntado)
                // 2) Lists without positions: the new ones are removed and which ones they were is recorded (what was recorded is replaced)
                foreach (string clave in ListasConjunto)
                {
                    if (!presentes.Contains(clave)) continue;
                    string ranura = ctx.Ranura(clave), lectura = RanuraLectura(registro, ctx, clave);
                    res = SaneadoSaves.Array(clave).Replace(res, m =>
                    {
                        var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
                        var quitados = elementos.Where(tabla.EsNuevo).Select(int.Parse).Distinct().ToList();
                        // 0.7.0: se conservan los apuntados de módulos que ahora no están activos (no son de la tabla)
                        // (0.7.1: solo si son de esta misma partida)
                        // 0.7.0: the recorded entries of modules that are not currently active are kept (they are not from the table)
                        // (0.7.1: only if they are from this same run)
                        bool conservar = olvidarSinObjetos && MismaPartida(registro, lectura, partida);
                        var apuntar = quitados.Concat(conservar ? registro.ObjetosLista(lectura).Where(id => !tabla.EsNuevo(id)) : Enumerable.Empty<int>())
                                              .Distinct().ToList();
                        if (quitados.Count > 0 || olvidarSinObjetos)
                        {
                            registro.FijarObjetosLista(ranura, apuntar, partida);
                            if (lectura != ranura) registro.FijarObjetosLista(lectura, new List<int>());
                        }
                        if (quitados.Count == 0) return m.Value;
                        elementos.RemoveAll(tabla.EsNuevo);
                        return m.Groups[1].Value + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value;
                    });
                }

                // 3) Listas de colección: se quitan y se anotan (solo crece; no dependen de la instantánea)
                // 3) Collection lists: they are removed and recorded (it only grows; they do not depend on the snapshot)
                foreach (string clave in ListasColeccion)
                {
                    if (!presentes.Contains(clave)) continue;
                    res = SaneadoSaves.Array(clave).Replace(res, m =>
                    {
                        var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
                        var quitados = elementos.Where(tabla.EsNuevo).Select(int.Parse).Distinct().ToList();
                        if (quitados.Count == 0) return m.Value;
                        registro.AnadirObjetosLista(clave, quitados);
                        elementos.RemoveAll(tabla.EsNuevo);
                        return m.Groups[1].Value + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value;
                    });
                }

                // 4) Historial: respaldo sin recuperación
                // 4) History: backup without recovery
                foreach (string clave in ListasHistorial)
                {
                    if (!presentes.Contains(clave)) continue;
                    res = SaneadoSaves.Array(clave).Replace(res, m =>
                    {
                        var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
                        bool cambiado = false;
                        for (int i = 0; i < elementos.Count; i++)
                            if (int.TryParse(elementos[i], out int id) && tabla.EsNuevo(id)) { elementos[i] = tabla.RespaldoDe(id).ToString(); cambiado = true; }
                        return cambiado ? m.Groups[1].Value + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value : m.Value;
                    });
                }

                // 5) Equipo inicial recordado (0.7.2): objeto nuevo -> None y se apunta el real
                // 5) Remembered starting gear (0.7.2): new gear -> None and the real one is recorded
                if (hayInicial) res = InicialParaDisco(res, tabla, registro, olvidarSinObjetos);
                // 6) Equipo inicial del cooperativo (0.7.5): respaldo sin recuperación
                // 6) Co-op starting gear (0.7.5): backup without recovery
                if (TieneClave(res, ClaveCoop))
                    res = ReCoop.Replace(res, m => int.TryParse(m.Groups[2].Value, out int v) && tabla.EsNuevo(v) ? m.Groups[1].Value + tabla.RespaldoDe(v) : m.Value, 1);
                // Solo con los objetos activos: sin ellos esta etapa no olvida nada y repetirla no cambia el registro; así, si
                // el mismo texto llega luego con los objetos activos, se trata con normalidad (y se olvida lo que toque)
                // Only with the gear active: without it this stage forgets nothing and repeating it does not change the registry; so, if
                // the same text arrives later with the gear active, it is handled normally (and whatever applies is forgotten)
                if (olvidarSinObjetos) registro.RecordarSalida(res, previas | RegistroGuardias.EtapaObjetos);
            }
            finally { registro.TerminarLote(); }
            return res;
        }

        // Equipo inicial recordado: escribe None en lugar de un objeto nuevo y apunta el real (posición 0 de su ranura). Con
        // un objeto de serie se conserva lo apuntado si sigue None y el real no es de un módulo activo o los objetos no están
        // activos (para recuperarlo al volver a activarlos); si no, se olvida (el jugador eligió otro o ninguno).
        // Remembered starting gear: writes None instead of new gear and records the real one (position 0 of its slot). With
        // a stock item, what was recorded is kept if it is still None and the real one is not from an active module or the gear is not
        // active (to recover it when it is activated again); otherwise, it is forgotten (the player chose another or none).
        private static string InicialParaDisco(string json, TablaObjetos tabla, RegistroGuardias registro, bool olvidarSinObjetos)
        {
            return ReInicial.Replace(json, m =>
            {
                if (!int.TryParse(m.Groups[2].Value, out int v)) return m.Value;
                if (tabla.EsNuevo(v))
                {
                    registro.FijarObjetosPosicion(ClaveInicial, new Dictionary<int, (int, int)> { { 0, (v, EscritoInicial) } });
                    return m.Groups[1].Value + EscritoInicial;
                }
                var apuntado = registro.ObjetosPosicion(ClaveInicial);
                if (apuntado.Count == 0) return m.Value;
                bool sigue = apuntado.TryGetValue(0, out var a) && a.escrito == v;
                if (sigue && (!olvidarSinObjetos || !tabla.EsNuevo(a.real))) return m.Value;
                registro.FijarObjetosPosicion(ClaveInicial, new Dictionary<int, (int, int)>());
                return m.Value;
            }, 1);
        }

        // ---------------------------------------------------------------
        // Vuelta del disco
        // Return from disk
        // ---------------------------------------------------------------
        /// <summary>Devuelve el JSON con los objetos nuevos restaurados según el registro. Mismo texto si no hay nada que cambiar.</summary>
        /// Returns the JSON with the new gear restored according to the registry. Same text if there is nothing to change.
        public static string DesdeDisco(string json, TablaObjetos tabla, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || tabla == null || tabla.Numero == 0 || registro == null || !registro.TieneObjetos) return json;
            // 0.7.5: contexto del texto (partida y slot en la ranura)
            // 0.7.5: text context (run and slot in the slot)
            var ctx = ContextoSave.De(json);
            string partida = ctx.Firma;
            string res = json;

            // 1) Listas con posición: si en la posición sigue el objeto escrito, vuelve a ser el nuevo (0.7.1: y si lo
            //    apuntado es de esta misma partida)
            // 1) Lists with position: if the written item is still at the position, it becomes the new one again (0.7.1: and if what was
            //    recorded is from this same run)
            foreach (string clave in ListasConPosicion)
            {
                string lectura = RanuraLectura(registro, ctx, clave);
                if (!TieneClave(res, clave) || !MismaPartida(registro, lectura, partida)) continue;
                var apuntados = registro.ObjetosPosicion(lectura);
                if (apuntados.Count == 0) continue;
                res = SaneadoSaves.Array(clave).Replace(res, m =>
                {
                    var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
                    bool cambiado = false;
                    foreach (var kv in apuntados)
                    {
                        if (kv.Key < elementos.Count && int.TryParse(elementos[kv.Key], out int v) && v == kv.Value.escrito && tabla.EsNuevo(kv.Value.real))
                        { elementos[kv.Key] = kv.Value.real.ToString(); cambiado = true; }
                    }
                    return cambiado ? m.Groups[1].Value + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value : m.Value;
                });
            }

            // 2) Listas sin posición y de colección: se vuelven a añadir los nuevos apuntados que falten
            // 2) Lists without position and collection lists: the recorded new items that are missing are added back
            foreach (string clave in ListasConjunto.Concat(ListasColeccion))
            {
                if (!TieneClave(res, clave)) continue;
                bool coleccion = ListasColeccion.Contains(clave);
                string ranura = coleccion ? clave : RanuraLectura(registro, ctx, clave);
                // 0.7.1: las listas de la partida (vendidos, rotos) solo con lo apuntado en esta misma partida; las de
                // colección son del jugador y valen para todas
                // 0.7.1: the run's lists (sold, broken) only with what was recorded in this same run; the
                // collection ones belong to the player and apply to all
                if (!coleccion && !MismaPartida(registro, ranura, partida)) continue;
                var ids = registro.ObjetosLista(ranura).Where(tabla.EsNuevo).ToList();
                if (ids.Count == 0) continue;
                res = SaneadoSaves.Array(clave).Replace(res, m =>
                {
                    var elementos = SaneadoSaves.PartirArray(m.Groups[2].Value);
                    bool cambiado = false;
                    foreach (int id in ids)
                        if (!elementos.Contains(id.ToString())) { elementos.Add(id.ToString()); cambiado = true; }
                    return cambiado ? m.Groups[1].Value + SaneadoSaves.UnirArray(elementos, m.Groups[2].Value) + m.Groups[3].Value : m.Value;
                });
            }

            // 3) Equipo inicial recordado (0.7.2): si sigue None y lo apuntado es de un objeto activo, vuelve a ser el real
            // 3) Remembered starting gear (0.7.2): if it is still None and what was recorded is from an active item, it becomes the real one again
            if (TieneClave(res, ClaveInicial) && registro.ObjetosPosicion(ClaveInicial).TryGetValue(0, out var ini) && tabla.EsNuevo(ini.real))
                res = ReInicial.Replace(res, m => int.TryParse(m.Groups[2].Value, out int v) && v == ini.escrito ? m.Groups[1].Value + ini.real : m.Value, 1);
            return res;
        }

        // ---------------------------------------------------------------
        // Verificación
        // Verification
        // ---------------------------------------------------------------
        /// <summary>Cuenta los objetos nuevos que quedan en las listas de objetos conocidas (debe ser 0 tras ParaDisco).</summary>
        /// Counts the new gear left in the known item lists (must be 0 after ParaDisco).
        public static int ContarResiduos(string json, TablaObjetos tabla)
        {
            if (string.IsNullOrEmpty(json) || tabla == null || tabla.Numero == 0) return 0;
            int n = 0;
            foreach (string clave in TodasLasClaves)
            {
                if (!TieneClave(json, clave)) continue;
                foreach (Match m in SaneadoSaves.Array(clave).Matches(json))
                    foreach (string e in SaneadoSaves.PartirArray(m.Groups[2].Value)) if (tabla.EsNuevo(e)) n++;
            }
            // 0.7.2: equipo inicial recordado del save del jugador
            // 0.7.2: remembered starting gear from the player's save
            if (TieneClave(json, ClaveInicial))
            {
                var mi = ReInicial.Match(json);
                if (mi.Success && tabla.EsNuevo(mi.Groups[2].Value)) n++;
            }
            // 0.7.5: equipo inicial del cooperativo
            // 0.7.5: co-op starting gear
            if (TieneClave(json, ClaveCoop))
            {
                var mc = ReCoop.Match(json);
                if (mc.Success && tabla.EsNuevo(mc.Groups[2].Value)) n++;
            }
            return n;
        }
    }
}
