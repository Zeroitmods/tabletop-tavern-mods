// PruebasRevisionExterna.cs
// Prueba 21 (18-10-2026, noche): mejoras de la revisión externa, fuera del juego:
//   - P-B: decisión de la telemetría por evento (RegistroGuardias.BloquearTelemetria y BloquearEnvioTelemetria). Con
//     módulos aplicados se bloquea todo, también los informes de fallo; sin módulos solo se bloquean los eventos de la
//     partida anclada a los módulos en el registro, y los informes de fallo del juego (exceptionSeen, bugReportSubmitted)
//     pasan; el envío de la cola sin módulos pasa si el filtro de eventos está puesto. La partida en curso (lectura del
//     save en disco) solo se pide cuando hace falta.
//   - Aviso de versión del juego (LogicaVersion.Aviso): misma versión sin aviso; distinta, vacía o null con aviso.
// Los enganches de Harmony (Saves.AntesDeEventoTelemetria y AntesDeEnvioTelemetria) y Application.version solo se pueden
// ver dentro del juego.
// Autor: ZeroitMods.
// PruebasRevisionExterna.cs
// Test 21 (18-10-2026, night): improvements from the external review, outside the game:
//   - P-B: per-event telemetry decision (RegistroGuardias.BloquearTelemetria and BloquearEnvioTelemetria). With
//     modules applied everything is blocked, including crash reports; without modules only the events of the
//     run anchored to the modules in the registry are blocked, and the game's crash reports (exceptionSeen, bugReportSubmitted)
//     go through; sending the queue without modules goes through if the event filter is set. The current run (reading the
//     save on disk) is only requested when needed.
//   - Game version warning (LogicaVersion.Aviso): same version no warning; different, empty or null with a warning.
// The Harmony hooks (Saves.AntesDeEventoTelemetria and AntesDeEnvioTelemetria) and Application.version can only be
// seen inside the game.
// Author: ZeroitMods.

using System;
using ZeroNucleo;

internal static class PruebasRevisionExterna
{
    public static void Todas(Action<bool, string> comprobar)
    {
        PruebaTelemetriaPorEvento(comprobar);
        PruebaEnvioTelemetria(comprobar);
        PruebaAvisoVersion(comprobar);
    }

    // Registro en memoria con la partida «r-mod» marcada como jugada con módulos
    // In-memory registry with the run «r-mod» marked as played with modules
    private static RegistroGuardias RegistroConPartida()
    {
        var reg = new RegistroGuardias(null);
        reg.MarcarPartidaModificada("r-mod");
        return reg;
    }

    // ---------------------------------------------------------------
    // P-B: apuntar eventos (AnalyticsService.Record y HttpAnalyticsBackend.Record)
    // P-B: recording events (AnalyticsService.Record and HttpAnalyticsBackend.Record)
    // ---------------------------------------------------------------
    private static void PruebaTelemetriaPorEvento(Action<bool, string> comprobar)
    {
        var reg = RegistroConPartida();
        string[] eventos = { "runEnded", "battleEnded", "exceptionSeen", "bugReportSubmitted", null, "" };

        // Lista cerrada y nombres exactos
        // Closed list and exact names
        comprobar(RegistroGuardias.EsInformeDeFallo("exceptionSeen") && RegistroGuardias.EsInformeDeFallo("bugReportSubmitted"),
            "P-B: exceptionSeen y bugReportSubmitted son informes de fallo");
        comprobar(!RegistroGuardias.EsInformeDeFallo("runEnded") && !RegistroGuardias.EsInformeDeFallo("ExceptionSeen") &&
                  !RegistroGuardias.EsInformeDeFallo("exceptionSeenX") && !RegistroGuardias.EsInformeDeFallo(null) && !RegistroGuardias.EsInformeDeFallo(""),
            "P-B: eventos de partida, mayúsculas distintas, prefijos, null y vacío no son informes de fallo (nombre exacto)");

        // Matriz opción x módulos x partida anclada x evento
        // Matrix option x modules x anchored run x event
        bool todoBien = true;
        string primerFallo = null;
        foreach (bool opcion in new[] { false, true })
            foreach (bool modulos in new[] { false, true })
                foreach (string partida in new[] { "r-mod", "r-serie", null })
                    foreach (string evento in eventos)
                    {
                        bool anclada = partida == "r-mod";
                        bool esperado = opcion && (modulos || (anclada && !RegistroGuardias.EsInformeDeFallo(evento)));
                        bool real = RegistroGuardias.BloquearTelemetria(opcion, modulos, reg, () => partida, evento);
                        if (real != esperado && todoBien)
                        {
                            todoBien = false;
                            primerFallo = $"opción {opcion}, módulos {modulos}, partida {partida ?? "null"}, evento {evento ?? "null"}: {real}";
                        }
                    }
        comprobar(todoBien, "P-B: matriz completa (opción x módulos x partida x evento) = opción && (módulos || (anclada && !informe de fallo))" +
                            (primerFallo == null ? "" : " -> falla en " + primerFallo));

        // Casos clave, uno a uno, para que el log diga quién pasa y quién no
        // Key cases, one by one, so the log says what passes and what doesn't
        comprobar(RegistroGuardias.BloquearTelemetria(true, true, reg, () => "r-serie", "exceptionSeen") &&
                  RegistroGuardias.BloquearTelemetria(true, true, reg, () => "r-serie", "bugReportSubmitted"),
            "P-B: CON módulos aplicados los informes de fallo se bloquean (podrían ser fallos del mod)");
        comprobar(RegistroGuardias.BloquearTelemetria(true, false, reg, () => "r-mod", "runEnded") &&
                  RegistroGuardias.BloquearTelemetria(true, false, reg, () => "r-mod", "battleEnded"),
            "P-B: SIN módulos, los eventos de una partida anclada siguen bloqueados (runEnded, battleEnded)");
        comprobar(!RegistroGuardias.BloquearTelemetria(true, false, reg, () => "r-mod", "exceptionSeen") &&
                  !RegistroGuardias.BloquearTelemetria(true, false, reg, () => "r-mod", "bugReportSubmitted"),
            "P-B: SIN módulos, los informes de fallo del juego pasan aunque la partida en curso esté anclada");
        comprobar(RegistroGuardias.BloquearTelemetria(true, false, reg, () => "r-mod", null) &&
                  RegistroGuardias.BloquearTelemetria(true, false, reg, () => "r-mod", "eventoNuevoDelJuego"),
            "P-B: SIN módulos y partida anclada, un evento sin nombre o desconocido se bloquea (fallo cerrado)");
        comprobar(!RegistroGuardias.BloquearTelemetria(true, false, reg, () => "r-serie", "runEnded") &&
                  !RegistroGuardias.BloquearTelemetria(true, false, null, () => "r-mod", "runEnded") &&
                  !RegistroGuardias.BloquearTelemetria(true, false, reg, null, "runEnded"),
            "P-B: SIN módulos, una partida de serie, sin registro o sin forma de leer la partida no se bloquea");

        // La partida en curso (lectura del save en disco) solo se pide cuando decide algo
        // The current run (reading the save on disk) is only requested when it decides something
        int lecturas = 0;
        Func<string> contar = () => { lecturas++; return "r-mod"; };
        RegistroGuardias.BloquearTelemetria(false, false, reg, contar, "runEnded");
        RegistroGuardias.BloquearTelemetria(true, true, reg, contar, "runEnded");
        RegistroGuardias.BloquearTelemetria(true, false, reg, contar, "exceptionSeen");
        comprobar(lecturas == 0, "P-B: con la opción apagada, con módulos o con un informe de fallo no se lee la partida en curso");
        RegistroGuardias.BloquearTelemetria(true, false, reg, contar, "runEnded");
        comprobar(lecturas == 1, "P-B: sin módulos y evento de partida se lee la partida en curso una vez");
    }

    // ---------------------------------------------------------------
    // P-B: envío de la cola al servidor (HttpAnalyticsBackend.Send)
    // P-B: sending the queue to the server (HttpAnalyticsBackend.Send)
    // ---------------------------------------------------------------
    private static void PruebaEnvioTelemetria(Action<bool, string> comprobar)
    {
        var reg = RegistroConPartida();
        comprobar(!RegistroGuardias.BloquearEnvioTelemetria(false, true, true, reg, () => "r-mod"),
            "P-B envío: con la opción apagada no se bloquea nunca");
        comprobar(RegistroGuardias.BloquearEnvioTelemetria(true, true, true, reg, () => "r-serie") &&
                  RegistroGuardias.BloquearEnvioTelemetria(true, true, false, reg, () => "r-serie"),
            "P-B envío: CON módulos aplicados la cola no se envía");
        comprobar(!RegistroGuardias.BloquearEnvioTelemetria(true, false, true, reg, () => "r-mod"),
            "P-B envío: SIN módulos y con el filtro de eventos puesto la cola se envía aunque la partida esté anclada (los informes de fallo no se retienen)");
        comprobar(RegistroGuardias.BloquearEnvioTelemetria(true, false, false, reg, () => "r-mod") &&
                  !RegistroGuardias.BloquearEnvioTelemetria(true, false, false, reg, () => "r-serie"),
            "P-B envío: SIN módulos y SIN filtro de eventos seguro (no engancharon los dos Record) vuelve la regla anterior: se retiene solo con la partida anclada");
        int lecturas = 0;
        RegistroGuardias.BloquearEnvioTelemetria(true, false, true, reg, () => { lecturas++; return "r-mod"; });
        comprobar(lecturas == 0, "P-B envío: con el filtro de eventos puesto no se lee la partida en curso");
    }

    // ---------------------------------------------------------------
    // Aviso de versión del juego
    // Game version notice
    // ---------------------------------------------------------------
    private static void PruebaAvisoVersion(Action<bool, string> comprobar)
    {
        comprobar(LogicaVersion.Aviso("1.10.17", "1.10.17") == null && LogicaVersion.Aviso("1.10.17", " 1.10.17 ") == null,
            "Versión: la misma versión (también con espacios a los lados) no avisa");
        string distinta = LogicaVersion.Aviso("1.10.17", "1.11.0");
        comprobar(distinta != null && distinta.Contains("1.11.0") && distinta.Contains("1.10.17") && distinta.Contains("NO está probado") &&
                  distinta.Contains("Parche no aplicado") && distinta.Contains("No se ha desactivado nada"),
            "Versión: otra versión avisa con las dos versiones, que no está probado, que revise «Parche no aplicado» y que no se desactiva nada");
        comprobar(LogicaVersion.Aviso("1.10.17", "1.10.170") != null && LogicaVersion.Aviso("1.10.17", "1.10.1") != null,
            "Versión: la comparación es exacta (1.10.170 y 1.10.1 no son 1.10.17)");
        string vacia = LogicaVersion.Aviso("1.10.17", "");
        comprobar(vacia != null && vacia.Contains("desconocida") && LogicaVersion.Aviso("1.10.17", null) != null,
            "Versión: una versión vacía o null avisa como desconocida");
    }
}
