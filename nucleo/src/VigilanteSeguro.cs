// VigilanteSeguro.cs
// Piezas .NET puras del vigilante de saves (Saves.cs) que se pueden probar fuera del juego (proyecto pruebas\):
//   - PausaVigilante: pausa con contador que, además de impedir las vueltas siguientes, ESPERA a que acabe la vuelta en
//     curso. La usa la autoprueba (revisión del 18-10, A-1): mientras cambia el registro de saves por uno temporal, el
//     vigilante (otro hilo) no puede estar saneando, así que ningún GUID real acaba en el registro temporal.
//   - EscrituraSegura: reescritura de un save por el vigilante solo si el juego no lo ha vuelto a guardar mientras tanto
//     (revisión del 18-10, M-3). Comprueba fecha y tamaño justo antes del reemplazo; si cambiaron, no pisa nada.
// Autor: ZeroitMods.
// VigilanteSeguro.cs
// Pure .NET pieces of the saves watcher (Saves.cs) that can be tested outside the game (project pruebas\):
//   - PausaVigilante: counter-based pause that, besides preventing subsequent passes, WAITS for the current pass to
//     finish. Used by the self-test (review of 18-10, A-1): while the saves registry is swapped for a temporary one, the
//     watcher (another thread) cannot be sanitizing, so no real GUID ends up in the temporary registry.
//   - EscrituraSegura: rewrite of a save by the watcher only if the game has not saved it again in the meantime
//     (review of 18-10, M-3). Checks date and size right before the replacement; if they changed, it overwrites nothing.
// Author: ZeroitMods.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: pausa del vigilante con espera a la vuelta en curso
    // Section: watcher pause that waits for the current pass
    // -----------------------------------------------------------------------
    /// <summary>
    /// Pausa del vigilante de saves entre hilos. El vigilante llama a <see cref="Entrar"/> al empezar cada vuelta y a
    /// <see cref="Salir"/> al acabarla; quien necesite que no haya ninguna vuelta en marcha llama a <see cref="Pausar"/>
    /// (y siempre después a <see cref="Reanudar"/>). Las pausas se pueden anidar (contador).
    /// Saves watcher pause across threads. The watcher calls <see cref="Entrar"/> at the start of each pass and
    /// <see cref="Salir"/> when it finishes; anyone who needs no pass to be running calls <see cref="Pausar"/>
    /// (and always <see cref="Reanudar"/> afterwards). Pauses can be nested (counter).
    /// </summary>
    public sealed class PausaVigilante
    {
        private int _pausas;     // > 0 = en pausa
                                 // > 0 = paused
        private int _ocupado;    // 1 = hay una vuelta del vigilante en marcha
                                 // 1 = a watcher pass is running

        /// <summary>¿Está en pausa ahora?</summary>
        /// Is it paused right now?
        public bool EnPausa => Volatile.Read(ref _pausas) > 0;

        /// <summary>¿Hay una vuelta del vigilante en marcha?</summary>
        /// Is a watcher pass running?
        public bool Ocupado => Volatile.Read(ref _ocupado) == 1;

        /// <summary>
        /// El vigilante intenta empezar una vuelta: false si está en pausa o ya hay otra en marcha. Se vuelve a mirar la
        /// pausa DESPUÉS de marcarse ocupado (las dos operaciones son barreras completas): así, o el vigilante ve la pausa
        /// y no entra, o quien pausa ve la vuelta en marcha y la espera. Nunca pasan las dos cosas a la vez.
        /// The watcher tries to start a pass: false if paused or another is already running. The
        /// pause is checked again AFTER marking itself busy (both operations are full barriers): this way, either the watcher sees the pause
        /// and does not enter, or the pauser sees the running pass and waits for it. Both things never happen at once.
        /// </summary>
        public bool Entrar()
        {
            if (EnPausa) return false;
            if (Interlocked.Exchange(ref _ocupado, 1) == 1) return false;
            if (EnPausa) { Interlocked.Exchange(ref _ocupado, 0); return false; }
            return true;
        }

        /// <summary>El vigilante termina su vuelta.</summary>
        /// The watcher finishes its pass.
        public void Salir() => Interlocked.Exchange(ref _ocupado, 0);

        /// <summary>
        /// Pone la pausa y espera (como mucho <paramref name="esperaMs"/> ms) a que acabe la vuelta en curso. true si ya no
        /// hay ninguna vuelta en marcha; false si se agotó la espera (la pausa queda puesta igual: hay que llamar a
        /// <see cref="Reanudar"/> siempre).
        /// Sets the pause and waits (at most <paramref name="esperaMs"/> ms) for the current pass to finish. true if no
        /// pass is running anymore; false if the wait timed out (the pause stays set anyway: you must always call
        /// <see cref="Reanudar"/>).
        /// </summary>
        public bool Pausar(int esperaMs)
        {
            Interlocked.Increment(ref _pausas);
            var reloj = Stopwatch.StartNew();
            while (Volatile.Read(ref _ocupado) == 1)
            {
                if (reloj.ElapsedMilliseconds >= esperaMs) return false;
                Thread.Sleep(5);
            }
            return true;
        }

        /// <summary>Quita una pausa (nunca baja de cero).</summary>
        /// Removes one pause (never goes below zero).
        public void Reanudar()
        {
            if (Interlocked.Decrement(ref _pausas) < 0) Interlocked.Exchange(ref _pausas, 0);
        }
    }

    // -----------------------------------------------------------------------
    // Sección: reescritura de un save solo si nadie lo ha cambiado
    // Section: rewrite of a save only if nobody has changed it
    // -----------------------------------------------------------------------
    public static class EscrituraSegura
    {
        /// <summary>Sufijo del fichero temporal de la reescritura (el mismo que usaba el vigilante).</summary>
        /// Suffix of the rewrite's temporary file (the same one the watcher used).
        public const string SufijoTemporal = ".zgi.tmp";

        /// <summary>¿Ha cambiado el fichero (fecha o tamaño) desde que se leyó? También true si ya no existe.</summary>
        /// Has the file changed (date or size) since it was read? Also true if it no longer exists.
        public static bool HaCambiado(string ruta, DateTime fechaLeida, long tamanoLeido)
        {
            var info = new FileInfo(ruta);
            if (!info.Exists) return true;
            return info.LastWriteTimeUtc != fechaLeida || info.Length != tamanoLeido;
        }

        /// <summary>
        /// Reescribe <paramref name="ruta"/> con <paramref name="contenido"/> de forma atómica (temporal + File.Replace),
        /// pero solo si el fichero sigue con la fecha y el tamaño leídos. Se comprueba antes de empezar y otra vez justo
        /// antes del reemplazo (después de escribir el temporal, que es lo que más tarda). Si cambió, devuelve false y no
        /// pisa la versión nueva del juego; el temporal se queda (se sobrescribe en el siguiente intento, no se borra
        /// nada). <paramref name="antesDeEscribir"/> (copia de seguridad) solo corre si se va a escribir.
        /// <paramref name="trasTemporal"/> es solo para las pruebas: simula que el juego guarda en ese momento.
        /// Límite: entre la última comprobación y File.Replace queda una ventana de microsegundos que el sistema de ficheros
        /// no permite cerrar sin bloquear el save al juego.
        /// Rewrites <paramref name="ruta"/> with <paramref name="contenido"/> atomically (temp file + File.Replace),
        /// but only if the file still has the date and size that were read. It is checked before starting and again right
        /// before the replacement (after writing the temp file, which takes the longest). If it changed, it returns false and does not
        /// overwrite the game's new version; the temp file stays (it is overwritten on the next attempt, nothing
        /// is deleted). <paramref name="antesDeEscribir"/> (backup) only runs if a write is going to happen.
        /// <paramref name="trasTemporal"/> is only for tests: it simulates the game saving at that moment.
        /// Limit: between the last check and File.Replace there is a window of microseconds that the file system
        /// does not allow closing without locking the save away from the game.
        /// </summary>
        public static bool ReemplazarSiNoCambio(string ruta, string contenido, DateTime fechaLeida, long tamanoLeido,
                                                Action antesDeEscribir = null, Action trasTemporal = null)
        {
            if (HaCambiado(ruta, fechaLeida, tamanoLeido)) return false;
            antesDeEscribir?.Invoke();
            string tmp = ruta + SufijoTemporal;
            File.WriteAllText(tmp, contenido);
            trasTemporal?.Invoke();
            // Última comprobación justo antes del reemplazo: si el juego ha guardado mientras tanto, su versión manda
            // Last check right before the replacement: if the game has saved in the meantime, its version wins
            if (HaCambiado(ruta, fechaLeida, tamanoLeido)) return false;
            File.Replace(tmp, ruta, null);
            return true;
        }
    }
}
