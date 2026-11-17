// PruebasArreglos18.cs
// Prueba 18 (18-10-2026): arreglos de la revisión de código del 18-10, fuera del juego:
//   - A-1: el vigilante de saves (otro hilo) nunca usa el registro temporal de la autoprueba. Se prueba la pausa con
//     espera (PausaVigilante, VigilanteSeguro.cs) con un hilo que imita al vigilante mientras el hilo principal cambia el
//     registro compartido muchas veces, y que un registro sin fichero se reconoce (RegistroGuardias.SoloEnMemoria). El
//     vigilante real (Saves.cs) usa tipos del juego y solo se puede ver dentro de él.
//   - M-1: bajas históricas de las unidades nuevas (UnitNameHistoricalKillStore) con el save real del jugador: fuera del
//     save y al registro al escribir, de vuelta al leer, sin duplicar la base ni la 9xx en ninguna sesión.
//   - M-2: bloqueo de clasificaciones y telemetría por partida (RegistroGuardias.BloquearOnline y PartidaAnclada), que
//     sobrevive a guardar y cargar el registro, y reconocimiento de una partida con escuadras 9xx apuntadas.
//   - M-3: el vigilante no pisa un save que el juego ha vuelto a guardar (EscrituraSegura), tampoco si lo guarda justo
//     entre el fichero temporal y el reemplazo.
// Autor: ZeroitMods.
// PruebasArreglos18.cs
// Test 18 (18-10-2026): fixes from the code review of 18-10, outside the game:
//   - A-1: the save watcher (another thread) never uses the self-test's temporary registry. The pause with
//     wait (PausaVigilante, VigilanteSeguro.cs) is tested with a thread that imitates the watcher while the main thread swaps the
//     shared registry many times, and that a registry without a file is recognized (RegistroGuardias.SoloEnMemoria). The real
//     watcher (Saves.cs) uses game types and can only be seen inside the game.
//   - M-1: historical kills of the new units (UnitNameHistoricalKillStore) with the player's real save: out of the
//     save and into the registry on write, back on read, without duplicating the base or the 9xx in any session.
//   - M-2: lock of leaderboards and telemetry per run (RegistroGuardias.BloquearOnline and PartidaAnclada), which
//     survives saving and loading the registry, and recognition of a run with 9xx squads recorded.
//   - M-3: the watcher does not overwrite a save that the game has saved again (EscrituraSegura), even if it saves right
//     between the temporary file and the replacement.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ZeroNucleo;

internal static class PruebasArreglos18
{
    // Carpeta de trabajo fija en el temporal del sistema (los ficheros se sobrescriben en cada pasada)
    // Fixed working folder in the system temp directory (files are overwritten on every pass)
    private static string Carpeta()
    {
        string c = Path.Combine(Path.GetTempPath(), "zgi_arreglos18");
        Directory.CreateDirectory(c);
        return c;
    }

    public static void Todas(Action<bool, string> comprobar, string jugadorReal)
    {
        PruebaPausaVigilante(comprobar);
        PruebaBajasHistoricas(comprobar, jugadorReal);
        PruebaBloqueoOnline(comprobar);
        PruebaEscrituraSegura(comprobar);
    }

    // ---------------------------------------------------------------
    // A-1: pausa del vigilante y registro solo en memoria
    // A-1: watcher pause and in-memory-only registry
    // ---------------------------------------------------------------
    private static void PruebaPausaVigilante(Action<bool, string> comprobar)
    {
        comprobar(new RegistroGuardias(null).SoloEnMemoria && !new RegistroGuardias(Path.Combine(Carpeta(), "r.json")).SoloEnMemoria,
            "A-1: un registro sin fichero (el temporal de la autoprueba) se reconoce como solo en memoria; uno con fichero no");

        // Pausar espera a que acabe la vuelta en curso
        // Pausar waits for the current loop to finish
        var pausa = new PausaVigilante();
        var dentro = new ManualResetEventSlim(false);
        var hilo = new Thread(() => { if (pausa.Entrar()) { dentro.Set(); Thread.Sleep(200); pausa.Salir(); } });
        hilo.Start();
        dentro.Wait(2000);
        var reloj = Stopwatch.StartNew();
        bool libre = pausa.Pausar(3000);
        long espera = reloj.ElapsedMilliseconds;
        hilo.Join();
        comprobar(libre && espera >= 100 && !pausa.Ocupado, $"A-1: Pausar espera a que el vigilante acabe su vuelta en curso ({espera} ms)");
        comprobar(!pausa.Entrar(), "A-1: con la pausa puesta el vigilante no empieza otra vuelta");
        pausa.Pausar(10);   // anidada
                            // nested
        pausa.Reanudar();
        bool siguePausado = !pausa.Entrar();
        pausa.Reanudar();
        bool entra = pausa.Entrar();
        pausa.Salir();
        pausa.Reanudar();   // de más: no baja de cero
                            // extra: does not go below zero
        bool entraOtra = pausa.Entrar();
        pausa.Salir();
        comprobar(siguePausado && entra && entraOtra, "A-1: las pausas se anidan (hasta la última Reanudar no vuelve) y una Reanudar de más no rompe nada");

        // Se agota la espera si la vuelta no acaba: Pausar devuelve false (la autoprueba no se hace)
        // The wait times out if the loop does not finish: Pausar returns false (the self-test is not run)
        var larga = new PausaVigilante();
        var dentro2 = new ManualResetEventSlim(false);
        var hilo2 = new Thread(() => { if (larga.Entrar()) { dentro2.Set(); Thread.Sleep(600); larga.Salir(); } });
        hilo2.Start();
        dentro2.Wait(2000);
        bool agotada = !larga.Pausar(50);
        hilo2.Join();
        larga.Reanudar();
        comprobar(agotada, "A-1: si la vuelta en curso no acaba a tiempo, Pausar devuelve false");

        // Carrera: un hilo imita al vigilante (lee el registro compartido y apunta un GUID) mientras el hilo principal hace
        // 200 veces lo de la autoprueba (pausa, cambia el registro por uno temporal, lo devuelve, reanuda)
        // Race: a thread imitates the watcher (reads the shared registry and records a GUID) while the main thread does
        // the self-test steps 200 times (pause, swap the registry for a temporary one, restore it, resume)
        var real = new RegistroGuardias(null);
        RegistroGuardias compartido = real;
        var p = new PausaVigilante();
        bool parar = false;
        int vueltas = 0;
        var vigilante = new Thread(() =>
        {
            int n = 0;
            while (!Volatile.Read(ref parar))
            {
                if (!p.Entrar()) { Thread.Yield(); continue; }
                try
                {
                    var reg = Volatile.Read(ref compartido);
                    Thread.SpinWait(200);
                    reg.ApuntarGuid("guid-" + (n++), 900);
                    Interlocked.Increment(ref vueltas);
                }
                finally { p.Salir(); }
            }
        });
        vigilante.Start();
        int contaminados = 0;
        for (int i = 0; i < 200; i++)
        {
            if (!p.Pausar(2000)) { contaminados++; p.Reanudar(); continue; }
            var temporal = new RegistroGuardias(null);
            Volatile.Write(ref compartido, temporal);
            Thread.SpinWait(500);
            Volatile.Write(ref compartido, real);
            if (temporal.NumeroGuids > 0) contaminados++;
            p.Reanudar();
            Thread.Yield();
        }
        Volatile.Write(ref parar, true);
        vigilante.Join();
        comprobar(contaminados == 0 && vueltas > 0 && real.NumeroGuids == vueltas,
            $"A-1: en 200 cambios de registro con el vigilante en marcha, ningún GUID cae en el temporal ({vueltas} vueltas, todas al registro real)");
    }

    // ---------------------------------------------------------------
    // M-1: bajas históricas de las unidades nuevas
    // M-1: historical kills of the new units
    // ---------------------------------------------------------------
    // Número de entradas de la unidad <u> en UnitNameHistoricalKillStore (y sus bajas, de la última)
    // Number of entries for unit <u> in UnitNameHistoricalKillStore (and its kills, from the last one)
    private static int Entradas(string json, int u, out int bajas)
    {
        bajas = -1;
        int n = 0;
        if (!SaneadoSaves.PartirArrayObjetos(json, SaneadoSaves.ClaveBajas, out _, out _, out var objetos)) return -1;
        foreach (var (ini, fin) in objetos)
        {
            string o = json.Substring(ini, fin - ini);
            var m = Regex.Match(o, "\"UnitName\"\\s*:\\s*(\\d+)");
            if (!m.Success || int.Parse(m.Groups[1].Value) != u) continue;
            n++;
            var k = Regex.Match(o, "\"Kills\"\\s*:\\s*(\\d+)");
            bajas = k.Success ? int.Parse(k.Groups[1].Value) : -1;
        }
        return n;
    }

    private static void PruebaBajasHistoricas(Action<bool, string> comprobar, string jugadorReal)
    {
        if (string.IsNullOrEmpty(jugadorReal) || !SaneadoSaves.PartirArrayObjetos(jugadorReal, SaneadoSaves.ClaveBajas, out _, out int cierra, out var objetos) || objetos.Count == 0)
        {
            comprobar(false, "M-1: el save del jugador de prueba trae UnitNameHistoricalKillStore con entradas");
            return;
        }
        // La 900 tiene de base la 61, que ya tiene su propia entrada en el save real (el caso del duplicado)
        // 900 has 61 as its base, which already has its own entry in the real save (the duplicate case)
        int base900 = 61;
        int basesAntes = Entradas(jugadorReal, base900, out int bajasBase);
        // Se añade al final del array una entrada de la 900 con el mismo estilo que las demás
        // An entry for 900 is added at the end of the array in the same style as the others
        int finUltimo = objetos[objetos.Count - 1].fin;
        string sangria = jugadorReal.Substring(objetos[0].ini - 8 < 0 ? 0 : objetos[0].ini - 8, Math.Min(8, objetos[0].ini));
        string entrada900 = ",\n" + sangria + "{\n" + sangria + "    \"UnitName\": 900,\n" + sangria + "    \"Kills\": 37\n" + sangria + "}";
        string conBajas = jugadorReal.Substring(0, finUltimo) + entrada900 + jugadorReal.Substring(finUltimo);
        comprobar(Entradas(conBajas, 900, out _) == 1 && basesAntes == 1, "M-1: save del jugador real con una entrada de la 900 (37 bajas) y una de su base 61");

        var disponibles = new HashSet<int> { 900 };
        var tabla = new TablaUnidades(new Dictionary<int, int> { { 900, base900 }, { 944, 30 } }) { Disponible = disponibles.Contains };
        var reg = new RegistroGuardias(null);

        // Ida al disco: la entrada de la 900 sale del save y va al registro; la base no se duplica ni cambia
        // Trip to disk: the 900 entry leaves the save and goes to the registry; the base is neither duplicated nor changed
        string disco = SaneadoSaves.ParaDisco(conBajas, tabla, reg);
        int basesDisco = Entradas(disco, base900, out int bajasBaseDisco);
        comprobar(Entradas(disco, 900, out _) == 0 && basesDisco == 1 && bajasBaseDisco == bajasBase && SaneadoSaves.ContarResiduos(disco, tabla) == 0
                  && reg.EntradasUnidades(SaneadoSaves.ClaveBajas).TryGetValue(900, out string apuntada) && apuntada.Contains("37"),
            "M-1: al escribir, la entrada de la 900 sale del save y se apunta en el registro; la de la base 61 sigue siendo una y con sus bajas");
        comprobar(disco == jugadorReal, "M-1: el save escrito es exactamente el original sin la entrada de la 900");

        // Vuelta: con la 900 disponible vuelve entera, en su sitio (texto idéntico al de antes de escribir)
        // Return trip: with 900 available it comes back whole, in its place (text identical to before writing)
        string leido = SaneadoSaves.DesdeDisco(disco, tabla, reg);
        comprobar(leido == conBajas, "M-1: al leer con la 900 disponible vuelve su entrada con sus 37 bajas (texto idéntico)");
        string leidoDos = SaneadoSaves.DesdeDisco(leido, tabla, reg);
        comprobar(leidoDos == leido && Entradas(leidoDos, 900, out _) == 1, "M-1: leer dos veces (el prefix de lectura corre dos veces) no duplica la entrada");

        // Sesión siguiente: la 900 mata más (37 -> 50); se guarda y se lee otra vez: una sola entrada, con 50
        // Next session: 900 kills more (37 -> 50); it is saved and read again: a single entry, with 50
        reg.OlvidarSalidas();
        string sesion2 = Regex.Replace(leido, "(\"UnitName\": 900,\\s*\"Kills\": )37", "${1}50");
        string disco2 = SaneadoSaves.ParaDisco(sesion2, tabla, reg);
        string leido2 = SaneadoSaves.DesdeDisco(disco2, tabla, reg);
        comprobar(Entradas(disco2, base900, out _) == 1 && Entradas(leido2, 900, out int bajas2) == 1 && bajas2 == 50 && Entradas(leido2, base900, out _) == 1,
            "M-1: en la sesión siguiente se suma en la misma entrada (50 bajas) y ni la base ni la 900 se duplican");

        // Módulo apagado: la 900 no vuelve; guardar sin ella no borra lo apuntado; al encenderlo vuelve con 50
        // Module off: 900 does not come back; saving without it does not erase what was recorded; when turned on it comes back with 50
        disponibles.Clear();
        reg.OlvidarSalidas();
        string sinModulo = SaneadoSaves.DesdeDisco(disco2, tabla, reg);
        string discoSinModulo = SaneadoSaves.ParaDisco(sinModulo, tabla, reg);
        disponibles.Add(900);
        string vuelve = SaneadoSaves.DesdeDisco(discoSinModulo, tabla, reg);
        comprobar(Entradas(sinModulo, 900, out _) == 0 && Entradas(vuelve, 900, out int bajas3) == 1 && bajas3 == 50,
            "M-1: con el módulo apagado la 900 no aparece y lo apuntado se conserva; al encenderlo vuelve con sus 50 bajas");

        // Un registro con solo bajas apuntadas no cuenta como vacío (si no, DesdeDisco no las devolvería)
        // A registry with only recorded kills does not count as empty (otherwise DesdeDisco would not return them)
        var soloBajas = new RegistroGuardias(null);
        soloBajas.FijarEntradaUnidad(SaneadoSaves.ClaveBajas, 900, "{\"UnitName\": 900, \"Kills\": 3}");
        comprobar(!soloBajas.EstaVacio, "M-1: un registro con solo bajas históricas apuntadas no está vacío");

        // Y sobrevive a guardar y cargar
        // And it survives saving and loading
        string ruta = Path.Combine(Carpeta(), "registro_bajas.json");
        var enDisco = new RegistroGuardias(ruta);
        enDisco.FijarEntradaUnidad(SaneadoSaves.ClaveBajas, 900, "{\"UnitName\": 900, \"Kills\": 12}");
        var cargado = new RegistroGuardias(ruta);
        cargado.Cargar();
        comprobar(cargado.EntradasUnidades(SaneadoSaves.ClaveBajas).TryGetValue(900, out string e12) && e12.Contains("12"),
            "M-1: las bajas apuntadas sobreviven a guardar y cargar el registro");
    }

    // ---------------------------------------------------------------
    // M-2: bloqueo de clasificaciones y telemetría por partida
    // M-2: lock of leaderboards and telemetry per run
    // ---------------------------------------------------------------
    private static void PruebaBloqueoOnline(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        comprobar(!RegistroGuardias.BloquearOnline(false, true, reg, "r1"), "M-2: con la opción apagada no se bloquea nunca");
        comprobar(RegistroGuardias.BloquearOnline(true, true, null, null), "M-2: con módulos aplicados en la sesión se bloquea (como antes)");
        comprobar(!RegistroGuardias.BloquearOnline(true, false, null, "r1") && !RegistroGuardias.BloquearOnline(true, false, reg, "r1"),
            "M-2: sin módulos y sin nada apuntado de esa partida no se bloquea (partida de serie)");
        reg.MarcarPartidaModificada("r1");
        comprobar(RegistroGuardias.BloquearOnline(true, false, reg, "r1") && !RegistroGuardias.BloquearOnline(true, false, reg, "r2")
                  && !RegistroGuardias.BloquearOnline(true, false, reg, "") && !RegistroGuardias.BloquearOnline(true, false, reg, null),
            "M-2: sin módulos, la partida marcada como jugada con módulos se bloquea; otra partida o una sin runId no");
        reg.FijarHeroe("heroe", 901, 6, "r3");
        reg.FijarObjetosPosicion("Gear", new Dictionary<int, (int, int)> { { 0, (700, 5) } }, "r4");
        comprobar(RegistroGuardias.BloquearOnline(true, false, reg, "r3") && RegistroGuardias.BloquearOnline(true, false, reg, "r4"),
            "M-2: también se bloquea una partida con un héroe nuevo u objetos nuevos apuntados (registros de antes de marcar partidas)");

        // Partida con escuadras 9xx empezada antes de esta versión: se reconoce por un GUID apuntado
        // Run with 9xx squads started before this version: recognized by a recorded GUID
        reg.ApuntarGuid("aaaa-1", 900);
        string conEscuadra = "{\"enemyWarlordHeroID\": 3, \"runId\": \"r5\", \"playerArmy\": [{\"UnitName\": 61, \"UniqueID\": \"aaaa-1\"}]}";
        string sinEscuadra = "{\"enemyWarlordHeroID\": 3, \"runId\": \"r6\", \"playerArmy\": [{\"UnitName\": 61, \"UniqueID\": \"bbbb-2\"}]}";
        comprobar(SaneadoSaves.TieneGuidApuntado(conEscuadra, reg) && !SaneadoSaves.TieneGuidApuntado(sinEscuadra, reg)
                  && !SaneadoSaves.TieneGuidApuntado(conEscuadra, new RegistroGuardias(null)),
            "M-2: un save de campaña con una escuadra apuntada se reconoce como partida modificada; uno sin ellas no");

        // Sobrevive a guardar y cargar; un registro de antes (sin el campo) carga sin fallar
        // Survives saving and loading; an older registry (without the field) loads without failing
        string ruta = Path.Combine(Carpeta(), "registro_partidas.json");
        var enDisco = new RegistroGuardias(ruta);
        enDisco.MarcarPartidaModificada("r7");
        enDisco.MarcarPartidaModificada("r7");   // repetida: no se duplica
                                                 // repeated: not duplicated
        var cargado = new RegistroGuardias(ruta);
        cargado.Cargar();
        comprobar(cargado.PartidaAnclada("r7") && File.ReadAllText(ruta).Split("r7").Length == 2,
            "M-2: la partida marcada sobrevive a guardar y cargar el registro (una sola vez)");
        string viejo = Path.Combine(Carpeta(), "registro_viejo.json");
        File.WriteAllText(viejo, "{\"Version\": 2, \"GuidsPorId\": {\"cccc-3\": 900}}");
        var cargadoViejo = new RegistroGuardias(viejo);
        cargadoViejo.Cargar();
        cargadoViejo.MarcarPartidaModificada("r8");
        comprobar(cargadoViejo.IdDeGuid("cccc-3") == 900 && cargadoViejo.PartidaAnclada("r8"), "M-2: un registro de antes, sin la lista de partidas, carga y la apunta");
    }

    // ---------------------------------------------------------------
    // M-3: reescritura del vigilante solo si el juego no ha vuelto a guardar
    // M-3: watcher rewrite only if the game has not saved again
    // ---------------------------------------------------------------
    private static void PruebaEscrituraSegura(Action<bool, string> comprobar)
    {
        string ruta = Path.Combine(Carpeta(), "campaignSaveData.json");

        // Sin cambios: se reescribe y corre la copia de seguridad
        // No changes: it is rewritten and the backup runs
        File.WriteAllText(ruta, "viejo con residuos");
        var info = new FileInfo(ruta);
        int copias = 0;
        bool hecho = EscrituraSegura.ReemplazarSiNoCambio(ruta, "saneado", info.LastWriteTimeUtc, info.Length, () => copias++);
        comprobar(hecho && File.ReadAllText(ruta) == "saneado" && copias == 1, "M-3: sin cambios del juego, el save se reescribe saneado (con copia de seguridad)");

        // El juego guarda antes de empezar: no se toca y no se hace copia
        // The game saves before starting: it is not touched and no backup is made
        File.WriteAllText(ruta, "viejo con residuos");
        info = new FileInfo(ruta);
        DateTime fecha = info.LastWriteTimeUtc; long tam = info.Length;
        File.WriteAllText(ruta, "nuevo del juego, más largo");
        copias = 0;
        hecho = EscrituraSegura.ReemplazarSiNoCambio(ruta, "saneado", fecha, tam, () => copias++);
        comprobar(!hecho && File.ReadAllText(ruta) == "nuevo del juego, más largo" && copias == 0, "M-3: si el juego ya guardó otra versión, no se pisa");

        // El juego guarda justo entre el temporal y el reemplazo (tamaño distinto)
        // The game saves right between the temp file and the replacement (different size)
        File.WriteAllText(ruta, "viejo con residuos");
        info = new FileInfo(ruta);
        hecho = EscrituraSegura.ReemplazarSiNoCambio(ruta, "saneado", info.LastWriteTimeUtc, info.Length, null,
            () => File.WriteAllText(ruta, "guardado del juego en medio"));
        comprobar(!hecho && File.ReadAllText(ruta) == "guardado del juego en medio", "M-3: si el juego guarda justo antes del reemplazo, su versión manda");

        // Mismo tamaño pero otra fecha (el juego reescribe con la misma longitud)
        // Same size but different date (the game rewrites with the same length)
        File.WriteAllText(ruta, "viejo con residuos");
        info = new FileInfo(ruta);
        hecho = EscrituraSegura.ReemplazarSiNoCambio(ruta, "saneado", info.LastWriteTimeUtc, info.Length, null,
            () => { File.WriteAllText(ruta, "viejo CON residuos"); File.SetLastWriteTimeUtc(ruta, DateTime.UtcNow.AddSeconds(5)); });
        comprobar(!hecho && File.ReadAllText(ruta) == "viejo CON residuos", "M-3: con el mismo tamaño pero otra fecha tampoco se pisa");
    }
}
