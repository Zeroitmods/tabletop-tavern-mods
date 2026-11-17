// Omisiones.cs
// Parches omitidos a mano para diagnosticar cierres (Diagnostico.OmitirParches, nombres de método separados por comas) y
// sus excepciones. Hasta la 0.7.4 solo se protegía el bloqueo de las clasificaciones online (LeaderboardClient.Submit).
// 0.7.5 (revisión total del 02-11, hallazgo 37): OmitirParches casa por NOMBRE de método en cualquier tipo, así que un
// «Start» o un «Unlock» puesto para diagnosticar quitaba sin aviso el bloqueo del multijugador o el filtro de logros (un
// logro de Steam concedido en falso no se puede quitar). Ahora tampoco se omiten:
//   - los parches de protección online (clasificaciones, multijugador y telemetría) mientras haya módulos de Zero aplicados
//     y su bloqueo esté encendido en la configuración;
//   - los del filtro de logros mientras Logros.Filtrar = true (para quitarlos, se apaga el filtro: decisión explícita).
// .NET puro para poder probarlo fuera del juego (proyecto pruebas\).
// Autor: ZeroitMods.
// Omisiones.cs
// Patches skipped by hand to diagnose crashes (Diagnostico.OmitirParches, method names separated by commas) and
// their exceptions. Up to 0.7.4 only the online leaderboards lock (LeaderboardClient.Submit) was protected.
// 0.7.5 (full review of 02-11, finding 37): OmitirParches matches by method NAME in any type, so a
// «Start» or an «Unlock» added for diagnosis silently removed the multiplayer lock or the achievements filter (a
// Steam achievement granted by mistake cannot be removed). Now the following are not skipped either:
//   - the online protection patches (leaderboards, multiplayer and telemetry) while Zero modules are applied
//     and their lock is enabled in the configuration;
//   - those of the achievements filter while Logros.Filtrar = true (to remove them, turn the filter off: an explicit decision).
// Pure .NET so it can be tested outside the game (pruebas\ project).
// Author: ZeroitMods.

using System;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: decisión de omitir un parche por configuración
    // Section: decision to skip a patch by configuration
    // -----------------------------------------------------------------------
    public static class Omisiones
    {
        /// <summary>Método del juego que parchea el bloqueo de las clasificaciones online.</summary>
        /// Game method that patches the online leaderboards lock.
        public const string MetodoClasificaciones = "Submit";

        /// <summary>Qué protege un parche (0.7.5): nada, el juego en línea (clasificaciones, multijugador, telemetría) o los logros.</summary>
        /// What a patch protects (0.7.5): nothing, online play (leaderboards, multiplayer, telemetry) or achievements.
        public enum Proteccion { Ninguna, EnLinea, Logros }

        /// <summary>¿Está <paramref name="metodo"/> en la lista de OmitirParches? (sin distinguir mayúsculas, con espacios)</summary>
        /// Is <paramref name="metodo"/> in the OmitirParches list? (case-insensitive, with spaces)
        public static bool EnLista(string lista, string metodo)
        {
            if (string.IsNullOrEmpty(metodo)) return false;
            foreach (string t in (lista ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (string.Equals(t, metodo, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// ¿Se puede omitir el parche de <paramref name="metodo"/>? (forma de la 0.7.1-0.7.4, solo para el bloqueo de
        /// clasificaciones) Todos sí, salvo Submit mientras haya módulos aplicados y el bloqueo esté activado.
        /// Can the patch for <paramref name="metodo"/> be skipped? (0.7.1-0.7.4 form, only for the leaderboards
        /// lock) All yes, except Submit while modules are applied and the lock is enabled.
        /// </summary>
        public static bool SePuedeOmitir(string metodo, bool partidaModificada, bool bloquearClasificaciones) =>
            !(string.Equals(metodo, MetodoClasificaciones, StringComparison.OrdinalIgnoreCase) && partidaModificada && bloquearClasificaciones);

        /// <summary>
        /// 0.7.5: ¿se puede omitir un parche según lo que protege? Los de protección en línea no, mientras haya módulos
        /// aplicados (<paramref name="partidaModificada"/>) y su bloqueo esté encendido (<paramref name="bloqueoActivo"/>); los
        /// del filtro de logros no, mientras el filtro esté encendido (<paramref name="filtrarLogros"/>). El resto, sí.
        /// 0.7.5: can a patch be skipped based on what it protects? Online protection ones cannot, while modules are
        /// applied (<paramref name="partidaModificada"/>) and their lock is on (<paramref name="bloqueoActivo"/>); those
        /// of the achievements filter cannot, while the filter is on (<paramref name="filtrarLogros"/>). The rest can.
        /// </summary>
        public static bool SePuedeOmitir(Proteccion proteccion, bool partidaModificada, bool bloqueoActivo, bool filtrarLogros) =>
            proteccion switch
            {
                Proteccion.EnLinea => !(partidaModificada && bloqueoActivo),
                Proteccion.Logros => !filtrarLogros,
                _ => true,
            };
    }
}
