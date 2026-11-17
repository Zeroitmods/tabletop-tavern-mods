// PruebasSonidos.cs
// Prueba 23 (18-10-2026): sonido láser de las unidades nuevas (LogicaSonidos.cs y LectorWav.cs), fuera del juego:
//   - lectura del campo «sonidos» del manifiesto (base, laser, blaster, pesado y el alias antiguo propios_guardia);
//   - prefijo de los WAV por tipo y clips del juego que se prestan (fusiles: Smite Charge; pesadas: LightningStrike);
//   - elección de capa (WAV propio > prestado > base) y migración única de Audio.SonidoPropio;
//   - con la carpeta maestra de módulos: qué unidades llevan cada sonido y que cada una tiene en la carpeta audio\ de
//     su módulo WAV válidos de su tipo (los lee el mismo LectorWav que el plugin).
// La creación del AudioClip (funciones internas de Unity) y el postfix de GetBattlefieldAudio solo se pueden ver en el juego.
// Autor: ZeroitMods.
// PruebasSonidos.cs
// Test 23 (18-10-2026): laser sound of the new units (LogicaSonidos.cs and LectorWav.cs), outside the game:
//   - reading the «sonidos» field of the manifest (base, laser, blaster, pesado and the old alias propios_guardia);
//   - WAV prefix by type and borrowed game clips (rifles: Smite Charge; heavy ones: LightningStrike);
//   - layer choice (own WAV > borrowed > base) and one-time migration of Audio.SonidoPropio;
//   - with the master modules folder: which units carry each sound and that each one has valid WAVs of its type in the audio\ folder of
//     its module (read by the same LectorWav as the plugin).
// Creating the AudioClip (internal Unity functions) and the GetBattlefieldAudio postfix can only be seen in the game.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroNucleo;

internal static class PruebasSonidos
{
    public static void Todas(Action<bool, string> comprobar, Manifiesto manifiesto)
    {
        PruebaValores(comprobar);
        PruebaCapas(comprobar);
        PruebaMigracion(comprobar);
        if (manifiesto != null) PruebaUnidades(comprobar, manifiesto);
    }

    // ---------------------------------------------------------------
    // Valores del manifiesto, prefijos y prestados
    // Manifest values, prefixes and borrowed ones
    // ---------------------------------------------------------------
    private static void PruebaValores(Action<bool, string> comprobar)
    {
        comprobar(LogicaSonidos.DeManifiesto("laser") == TipoSonido.Laser && LogicaSonidos.DeManifiesto("propios_guardia") == TipoSonido.Laser
                  && LogicaSonidos.DeManifiesto(" Blaster ") == TipoSonido.Blaster && LogicaSonidos.DeManifiesto("PESADO") == TipoSonido.Pesado,
            "sonido: laser/propios_guardia = láser 40k, blaster y pesado (sin distinguir mayúsculas ni espacios)");
        comprobar(LogicaSonidos.DeManifiesto(null) == TipoSonido.Base && LogicaSonidos.DeManifiesto("") == TipoSonido.Base
                  && LogicaSonidos.DeManifiesto("base") == TipoSonido.Base && LogicaSonidos.DeManifiesto("cañón") == TipoSonido.Base,
            "sonido: vacío, «base» o un valor desconocido = sonido de la unidad base (sin tocar)");
        comprobar(LogicaSonidos.PrefijoDisparo(TipoSonido.Laser) == "laser_disparo_" && LogicaSonidos.PrefijoDisparo(TipoSonido.Blaster) == "blaster_disparo_"
                  && LogicaSonidos.PrefijoDisparo(TipoSonido.Pesado) == "pesado_disparo_" && LogicaSonidos.PrefijoDisparo(TipoSonido.Base) == null,
            "sonido: prefijo de WAV por tipo (Base no lleva WAV)");
        comprobar(LogicaSonidos.Prestados(TipoSonido.Laser).SequenceEqual(new[] { "Smite Charge" })
                  && LogicaSonidos.Prestados(TipoSonido.Blaster).SequenceEqual(new[] { "Smite Charge" })
                  && LogicaSonidos.Prestados(TipoSonido.Pesado).SequenceEqual(new[] { "LightningStrike", "Smite Charge" })
                  && LogicaSonidos.Prestados(TipoSonido.Base).Count == 0,
            "sonido: prestados = Smite Charge para fusiles y carabinas, LightningStrike (y si no Smite Charge) para las pesadas");
        comprobar(LogicaSonidos.TodosLosPrestados().OrderBy(n => n).SequenceEqual(new[] { "LightningStrike", "Smite Charge" }),
            "sonido: la búsqueda en el juego pide cada clip prestado una sola vez");
        var soloSmite = new HashSet<string> { "Smite Charge" };
        comprobar(LogicaSonidos.ElegirPrestado(TipoSonido.Pesado, soloSmite) == "Smite Charge"
                  && LogicaSonidos.ElegirPrestado(TipoSonido.Pesado, new HashSet<string> { "LightningStrike", "Smite Charge" }) == "LightningStrike"
                  && LogicaSonidos.ElegirPrestado(TipoSonido.Blaster, new HashSet<string> { "LightningStrike" }) == null
                  && LogicaSonidos.ElegirPrestado(TipoSonido.Laser, null) == null,
            "sonido: el prestado sale por orden de preferencia y solo si está cargado");
    }

    // ---------------------------------------------------------------
    // Elección de capa y texto del log
    // Layer choice and log text
    // ---------------------------------------------------------------
    private static void PruebaCapas(Action<bool, string> comprobar)
    {
        comprobar(LogicaSonidos.ElegirCapa(TipoSonido.Laser, 3, true) == CapaSonido.Propio
                  && LogicaSonidos.ElegirCapa(TipoSonido.Blaster, 0, true) == CapaSonido.Prestado
                  && LogicaSonidos.ElegirCapa(TipoSonido.Pesado, 0, false) == CapaSonido.Base
                  && LogicaSonidos.ElegirCapa(TipoSonido.Base, 5, true) == CapaSonido.Base,
            "sonido: capa = WAV propio si hay, si no prestado si está cargado, si no base; un tipo Base siempre es base");
        comprobar(LogicaSonidos.DescribirCapa(CapaSonido.Propio) == "láser propio desde WAV"
                  && LogicaSonidos.DescribirCapa(CapaSonido.Prestado, "Smite Charge") == "prestado: Smite Charge"
                  && LogicaSonidos.DescribirCapa(CapaSonido.Base) == "base",
            "sonido: textos del log por capa («láser propio desde WAV», «prestado: …», «base»)");
    }

    // ---------------------------------------------------------------
    // Migración única de Audio.SonidoPropio
    // One-time migration of Audio.SonidoPropio
    // ---------------------------------------------------------------
    private static void PruebaMigracion(Action<bool, string> comprobar)
    {
        bool v1 = LogicaSonidos.MigrarSonidoPropio(false, 0, out bool m1);
        bool v2 = LogicaSonidos.MigrarSonidoPropio(true, 0, out bool m2);
        bool v3 = LogicaSonidos.MigrarSonidoPropio(false, LogicaSonidos.RevisionSonidoActual, out bool m3);
        comprobar(v1 && m1 && v2 && !m2 && !v3 && !m3,
            "sonido: la migración pasa a true una configuración antigua en false, una sola vez (después manda el usuario)");
    }

    // ---------------------------------------------------------------
    // Unidades reales del manifiesto y WAV de sus módulos
    // Real manifest units and the WAVs of their modules
    // ---------------------------------------------------------------
    private static void PruebaUnidades(Action<bool, string> comprobar, Manifiesto m)
    {
        var tipo = m.Unidades.ToDictionary(u => u.Id, u => LogicaSonidos.DeManifiesto(u.Sonidos));
        int[] laser = { 900, 903, 905, 907, 912 };
        int[] blaster = { 921, 924, 928, 933 };
        int[] pesado = { 927, 930, 934 };
        bool Es(int id, TipoSonido t) => tipo.TryGetValue(id, out var x) && x == t;
        comprobar(laser.All(i => Es(i, TipoSonido.Laser)), "sonido: láser 40k en 900, 903, 905, 907 y 912 (rifles y carabinas láser)");
        comprobar(blaster.All(i => Es(i, TipoSonido.Blaster)), "sonido: bláster en 921, 924, 928 y 933 (E-11, E-11D y DC-15A)");
        comprobar(pesado.All(i => Es(i, TipoSonido.Pesado)), "sonido: bláster pesado en 927, 930 y 934 (E-Web, AT-ST y AT-AT)");
        var conEnergia = new HashSet<int>(laser.Concat(blaster).Concat(pesado).Append(901));
        var resto = tipo.Where(kv => !conEnergia.Contains(kv.Key) && kv.Value != TipoSonido.Base).Select(kv => kv.Key).ToList();
        comprobar(resto.Count == 0 && Es(906, TipoSonido.Base) && Es(911, TipoSonido.Base) && Es(929, TipoSonido.Base),
            "sonido: morteros, cañones, detonadores y unidades sueltas se quedan con el sonido base" +
            (resto.Count == 0 ? "" : " (con sonido de energía: " + string.Join(", ", resto) + ")"));

        // Cada unidad con sonido de energía tiene en audio\ de su módulo al menos un WAV válido de su tipo
        // Each unit with an energy sound has at least one valid WAV of its type in the audio\ folder of its module
        var faltan = new List<string>();
        int leidos = 0;
        foreach (var u in m.Unidades)
        {
            var t = LogicaSonidos.DeManifiesto(u.Sonidos);
            if (t == TipoSonido.Base) continue;
            string carpeta = string.IsNullOrEmpty(u.Carpeta) ? null : Path.Combine(u.Carpeta, "audio");
            string[] wavs = carpeta != null && Directory.Exists(carpeta)
                ? Directory.GetFiles(carpeta, LogicaSonidos.PrefijoDisparo(t) + "*.wav") : new string[0];
            int validos = 0;
            foreach (string w in wavs)
            {
                try { LectorWav.LeerPcm16(File.ReadAllBytes(w), out int hz); if (hz == 44100) validos++; leidos++; }
                catch (Exception e) { faltan.Add($"{Path.GetFileName(w)}: {e.Message}"); }
            }
            if (validos == 0) faltan.Add($"{u.Id} ({LogicaSonidos.Nombre(t)}) sin WAV en {carpeta ?? "(sin carpeta)"}");
        }
        comprobar(faltan.Count == 0, "sonido: cada unidad de energía tiene WAV PCM 16 bits a 44,1 kHz de su tipo en su módulo " +
                                      $"({leidos} lecturas)" + (faltan.Count == 0 ? "" : ": " + string.Join("; ", faltan.Take(5))));

        // Impactos: cada módulo con unidades de energía trae los suyos
        // Impacts: each module with energy units brings its own
        var modulos = m.Unidades.Where(u => LogicaSonidos.DeManifiesto(u.Sonidos) != TipoSonido.Base && !string.IsNullOrEmpty(u.Carpeta))
                                .Select(u => u.Carpeta).Distinct().ToList();
        comprobar(modulos.Count > 0 && modulos.All(c => Directory.Exists(Path.Combine(c, "audio"))
                  && Directory.GetFiles(Path.Combine(c, "audio"), LogicaSonidos.PrefijoImpacto + "*.wav").Length >= 2),
            $"sonido: los {modulos.Count} módulos con armas de energía traen al menos 2 WAV de impacto");
    }
}
