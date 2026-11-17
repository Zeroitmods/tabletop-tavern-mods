// PruebasRevisionTotal.cs
// Prueba 27 (02-11-2026, núcleo 0.7.5): arreglos de la revisión total del proyecto (notas internas (no publicadas)).
//   - Ranuras del registro por partida y slot (hallazgo 1): dos partidas de runId distinto (principal y diaria) guardadas
//     alternadas no se borran lo apuntado; la misma partida con otro valor sí lo olvida; las ranuras viejas sin partida se
//     leen y se pasan a la nueva.
//   - Arrays nuevos de la 1.11.1 (hallazgo 4): battleRecruitOffer, mercenariesLeft y coopStartArmy se sanean y vuelven;
//     coopStartGear se sanea.
//   - Historial y save del jugador (hallazgos 24 y 29): runHistory (héroe, raza y señor de las batallas), battleLog,
//     lifetime.heroes y heroLooks; con el save real del 02-11 (una COPIA en la carpeta temporal).
//   - Logros de ejército por partida (hallazgo 25), omisiones protegidas (hallazgo 37), fusión de hechizos sin propios.
// Autor: ZeroitMods.
// PruebasRevisionTotal.cs
// Test 27 (02-11-2026, core 0.7.5): fixes from the full project review (internal notes (not published)).
//   - Registry slots per run and slot (finding 1): two runs with different runId (main and daily) saved
//     alternately do not erase what was recorded; the same run with a different value does forget it; old slots without a run are
//     read and migrated to the new one.
//   - New arrays from 1.11.1 (finding 4): battleRecruitOffer, mercenariesLeft and coopStartArmy are sanitized and come back;
//     coopStartGear is sanitized.
//   - Player history and save (findings 24 and 29): runHistory (hero, race and battle lord), battleLog,
//     lifetime.heroes and heroLooks; with the real save from 02-11 (a COPY in the temp folder).
//   - Army achievements per run (finding 25), protected omissions (finding 37), spell merge without own spells.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZeroNucleo;

internal static class PruebasRevisionTotal
{
    // -----------------------------------------------------------------------
    // Sección: tablas y textos de prueba
    // Section: test tables and texts
    // -----------------------------------------------------------------------
    // Unidades 900 (base 70), 941 (base 31) y 944 (base 95); razas 40 y 41 (sustituta 0 y 4); héroes 901-904 (prestados 6, 5, 7
    // y 8); objetos 700 (respaldo 17) y 715 (respaldo 4)
    // Units 900 (base 70), 941 (base 31) and 944 (base 95); races 40 and 41 (substitute 0 and 4); heroes 901-904 (borrowed 6, 5, 7
    // and 8); gear 700 (fallback 17) and 715 (fallback 4)
    private static TablaUnidades Unidades() => new TablaUnidades(new Dictionary<int, int> { { 900, 70 }, { 941, 31 }, { 944, 95 } });
    private static TablaRazas Razas() => new TablaRazas(new Dictionary<int, int> { { 40, 0 }, { 41, 4 } });
    private static TablaHeroes Heroes(Func<int, bool> activo = null) =>
        new TablaHeroes(new Dictionary<int, int> { { 901, 6 }, { 902, 5 }, { 903, 7 }, { 904, 8 } }) { Disponible = activo };
    private static TablaObjetos Objetos() => new TablaObjetos(new Dictionary<int, int> { { 700, 17 }, { 715, 4 } }, Enumerable.Range(1, 40));

    // Save de campaña con todo lo que el saneado toca (formato de JsonUtility «pretty»)
    // Campaign save with everything the sanitizing touches (JsonUtility «pretty» format)
    private static string Campana(string runId, string diaria, int heroe, string gear, string ofertas, string ciudad, int raza,
                                  string cartas, string ofertaBatalla, string mercenarios, string batallas, bool coop = false)
    {
        return "{\n" +
               "    \"seed\": 77,\n" +
               $"    \"Gear\": [{gear}],\n" +
               "    \"SoldGear\": [],\n" +
               $"    \"heroID\": {heroe},\n" +
               $"    \"townData\": {{\n        \"townName\": \"{ciudad}\",\n        \"townSize\": 2,\n        \"townRace\": {raza}\n    }},\n" +
               $"    \"recruitableUnits\": [{ofertas}],\n" +
               "    \"recruitableGear\": [3],\n" +
               "    \"RunStats\": {\n        \"spellsCast\": []\n    },\n" +
               "    \"snapShot\": false,\n" +
               $"    \"battleRecruitOffer\": [{ofertaBatalla}],\n" +
               $"    \"mercenariesLeft\": [{mercenarios}],\n" +
               $"    \"selectedSpells\": [{cartas}],\n" +
               $"    \"runId\": \"{runId}\",\n" +
               "    \"enemyWarlordHeroID\": 0,\n" +
               "    \"brokenGear\": [],\n" +
               $"    \"battleLog\": [{batallas}],\n" +
               $"    \"dailyDay\": \"{diaria}\",\n" +
               $"    \"coop\": {(coop ? "true" : "false")},\n" +
               "    \"coopStartArmy\": [],\n" +
               "    \"coopStartGear\": 0\n" +
               "}";
    }

    // Una batalla del registro de batallas
    // One battle from the battle log
    private static string Batalla(int raza, int senor) =>
        $"\n        {{\n            \"act\": 1,\n            \"race\": {raza},\n            \"warlord\": {senor},\n            \"won\": true\n        }}\n    ";

    // Tubería completa de escritura y de lectura (las mismas etapas y el mismo orden que Saves.SanearSalida y AntesDeFromJson)
    // Full write and read pipeline (the same stages and the same order as Saves.SanearSalida and AntesDeFromJson)
    private static string Ida(string json, RegistroGuardias reg, Func<int, bool> heroeActivo = null)
    {
        string r = SaneadoSaves.ParaDisco(json, Unidades(), reg);
        r = SaneadoSaves.RazasParaDisco(r, Razas(), reg);
        r = SaneadoObjetos.ParaDisco(r, Objetos(), reg, true);
        r = SaneadoSaves.HeroesParaDisco(r, Heroes(heroeActivo), reg);
        r = SaneadoSaves.HistorialParaDisco(r, Heroes(heroeActivo), Razas(), reg);
        return SaneadoHechizos.ParaDisco(r, reg);
    }

    private static string Vuelta(string json, RegistroGuardias reg, Func<int, bool> heroeActivo = null)
    {
        string r = SaneadoSaves.DesdeDisco(json, Unidades(), reg);
        r = SaneadoSaves.RazasDesdeDisco(r, Razas(), reg);
        r = SaneadoObjetos.DesdeDisco(r, Objetos(), reg);
        r = SaneadoSaves.HeroesDesdeDisco(r, Heroes(heroeActivo), reg);
        r = SaneadoSaves.HistorialDesdeDisco(r, Heroes(heroeActivo), Razas(), reg);
        return SaneadoHechizos.DesdeDisco(r, reg, id => id >= 1901 && id <= 1904);
    }

    // Ids nuevos que quedan en un texto (todas las etapas)
    // New ids left in a text (all stages)
    private static int Residuos(string json) =>
        SaneadoSaves.ContarResiduos(json, Unidades()) + SaneadoSaves.ContarResiduosRazas(json, Razas()) + SaneadoObjetos.ContarResiduos(json, Objetos())
        + SaneadoSaves.ContarResiduosHeroes(json, Heroes()) + SaneadoSaves.ContarResiduosHistorial(json, Heroes(), Razas()) + SaneadoHechizos.ContarResiduos(json);

    public static void Todas(Action<bool, string> comprobar, string carpetaModulos)
    {
        PruebaContexto(comprobar);
        PruebaRanurasAlternas(comprobar);
        PruebaMismaPartida(comprobar);
        PruebaLegado(comprobar);
        PruebaArraysNuevos(comprobar);
        PruebaHistorial(comprobar);
        PruebaSaveRealJugador(comprobar, carpetaModulos);
        PruebaLogros(comprobar);
        PruebaOmisiones(comprobar);
        PruebaFusionHechizos(comprobar);
        // Ronda 2 de la revisión independiente
        // Round 2 of the independent review
        PruebaPartidasSinMarcas(comprobar, carpetaModulos);
        PruebaHistorialModuloApagado(comprobar, carpetaModulos);
        PruebaSavesReales(comprobar, carpetaModulos);
        PruebaRazaViejaSoloPrincipal(comprobar);
    }

    // ---------------------------------------------------------------
    // Contexto de un texto: partida, slot e instantánea
    // Context of a text: run, slot and snapshot
    // ---------------------------------------------------------------
    private static void PruebaContexto(Action<bool, string> comprobar)
    {
        var p = ContextoSave.De(Campana("M", "", 3, "1", "3", "A", 0, "4,0,0", "", "", ""));
        var d = ContextoSave.De(Campana("D", "2026-11-02", 3, "1", "3", "A", 0, "4,0,0", "", "", ""));
        var c = ContextoSave.De(Campana("C", "", 3, "1", "3", "A", 0, "4,0,0", "", "", "", coop: true));
        var j = ContextoSave.De("{\"lastHeroID\": 3}");
        comprobar(p.Partida && p.EsPrincipal && p.Firma == "M" && p.Ranura("heroe") == "heroe@principal@M"
                  && d.Slot == ContextoSave.SlotDiaria && d.Ranura("Gear") == "Gear@diaria@D" && c.Slot == ContextoSave.SlotCoop
                  && !j.Partida && j.Ranura("ultimoHeroe") == "ultimoHeroe",
            "0.7.5 contexto: principal, diaria (dailyDay relleno) y cooperativo (coop: true) llevan su slot y su partida en la ranura; el save del jugador, la de siempre");
    }

    // ---------------------------------------------------------------
    // Hallazgo 1: principal y diaria escritas alternadas
    // Finding 1: main and daily runs written alternately
    // ---------------------------------------------------------------
    private static void PruebaRanurasAlternas(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        // Principal a medias: héroe 901, el 700 en el Gear, oferta 900, ciudad 40, hechizo 1901, oferta de batalla 941,
        // mercenario 944 y una batalla contra la 41 con el señor 903
        // Main run half done: hero 901, the 700 in the Gear, offer 900, city 40, spell 1901, battle offer 941,
        // mercenary 944 and a battle against 41 with lord 903
        string principal = Campana("M", "", 901, "700,1", "900,5,6", "Cadia", 40, "1901,4,0", "941,3", "944", Batalla(41, 903));
        // Diaria de serie: héroe 11, sin nada nuevo
        // Stock daily run: hero 11, nothing new
        string diaria = Campana("D", "2026-11-02", 11, "1,2", "3,5,6", "Altdorf", 0, "4,0,0", "3,4", "5", Batalla(1, 0));

        string dP = Ida(principal, reg);
        string dD = Ida(diaria, reg);
        reg.OlvidarSalidas();   // otra sesión: sin memoria de textos saneados
                                // another session: no memory of sanitized texts
        // Se juega la diaria y se guarda otra vez (la diaria lee y escribe varias veces)
        // The daily run is played and saved again (the daily run reads and writes several times)
        string dD2 = Ida(Vuelta(dD, reg), reg);
        reg.OlvidarSalidas();
        string vP = Vuelta(dP, reg);
        comprobar(Residuos(dP) == 0 && Residuos(dD2) == 0 && dD2 == diaria && vP == principal,
            "0.7.5 ranuras: la principal y la diaria se guardan alternadas; en disco ninguna lleva ids nuevos y la principal vuelve ENTERA (héroe, Gear, ofertas, ciudad, hechizo, oferta de batalla, mercenario y batalla)");

        // Y al revés: se vuelve a la principal, se guarda, y la diaria sigue intacta
        // And the other way around: back to the main run, save, and the daily run stays intact
        string dP2 = Ida(vP, reg);
        reg.OlvidarSalidas();
        comprobar(dP2 == dP && Vuelta(dD2, reg) == diaria && Vuelta(dP2, reg) == principal,
            "0.7.5 ranuras: guardar la principal no toca la diaria y la principal sigue volviendo igual");
        comprobar(reg.HeroeDe("heroe@principal@M", out int h, out _, out _) && h == 901 && !reg.HeroeDe("heroe@diaria@D", out _, out _, out _)
                  && reg.Ofertas("ofertas@principal@M").Count == 1 && reg.Ofertas("ofertas@diaria@D").Count == 0,
            "0.7.5 ranuras: lo apuntado está en la ranura de la principal y la diaria no tiene nada");
    }

    // ---------------------------------------------------------------
    // Hallazgo 1: en la MISMA partida, un valor nuevo sí olvida lo apuntado
    // Finding 1: in the SAME run, a new value does forget what was recorded
    // ---------------------------------------------------------------
    private static void PruebaMismaPartida(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        Ida(Campana("M", "", 901, "700,1", "900,5,6", "Cadia", 40, "1901,4,0", "", "", ""), reg);
        reg.OlvidarSalidas();
        // Misma partida M: el jugador cambia de héroe (de serie), vende el 700, la oferta es de serie y entra en otra ciudad de serie
        // Same run M: the player switches to a stock hero, sells 700, the offer is stock and enters another stock city
        string sinNada = Campana("M", "", 3, "1,2", "3,5,6", "Altdorf", 0, "4,0,0", "", "", "");
        Ida(sinNada, reg);
        comprobar(!reg.HeroeDe("heroe@principal@M", out _, out _, out _) && reg.Ofertas("ofertas@principal@M").Count == 0
                  && reg.ObjetosPosicion("Gear@principal@M").Count == 0 && !reg.RazaDe("ciudad@principal@M", out _, out _)
                  && !reg.HechizosDe("seleccion@principal@M", out _, out _, out _),
            "0.7.5 ranuras: en la misma partida, si el valor cambia, lo apuntado de esa partida se olvida como siempre");
    }

    // ---------------------------------------------------------------
    // Hallazgo 1: ranuras viejas sin partida (registros de antes de la 0.7.5)
    // Finding 1: old slots without a run (registries from before 0.7.5)
    // ---------------------------------------------------------------
    private static void PruebaLegado(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        // Lo que dejó la 0.7.4 para la principal M: héroe 901 escrito como 6, oferta 900 en la posición 0, 700 en el Gear
        // What 0.7.4 left for main run M: hero 901 written as 6, offer 900 at position 0, 700 in the Gear
        reg.FijarHeroe("heroe", 901, 6, "M");
        reg.FijarOfertas(new Dictionary<int, int> { { 0, 900 } });
        reg.FijarObjetosPosicion("Gear", new Dictionary<int, (int, int)> { { 0, (700, 17) } }, "M");
        reg.FijarHechizos("seleccion", "M", new Dictionary<int, int> { { 0, 1901 } }, null);
        string disco = Campana("M", "", 6, "17,1", "70,5,6", "Cadia", 0, "0,4,0", "", "", "");
        string esperado = Campana("M", "", 901, "700,1", "900,5,6", "Cadia", 0, "1901,4,0", "", "", "");
        // Una diaria guardada antes de volver a la principal no toca lo viejo de la principal
        // A daily run saved before going back to the main run does not touch the old data of the main run
        Ida(Campana("D", "2026-11-02", 11, "1,2", "3,5,6", "Altdorf", 0, "4,0,0", "", "", ""), reg);
        string leido = Vuelta(disco, reg);
        comprobar(leido == esperado, "0.7.5 legado: lo apuntado por la 0.7.4 (sin partida en la clave) se sigue leyendo para su partida, aunque antes se guarde una diaria");
        Ida(leido, reg);
        comprobar(!reg.HeroeDe("heroe", out _, out _, out _) && reg.Ofertas(false).Count == 0 && reg.ObjetosPosicion("Gear").Count == 0
                  && !reg.HechizosDe("seleccion", out _, out _, out _)
                  && reg.HeroeDe("heroe@principal@M", out int h, out _, out _) && h == 901 && reg.Ofertas("ofertas@principal@M").Count == 1
                  && reg.ObjetosPosicion("Gear@principal@M").Count == 1 && reg.HechizosDe("seleccion@principal@M", out _, out _, out _),
            "0.7.5 legado: al guardar esa partida, lo viejo pasa a las ranuras nuevas y las viejas se vacían");
        // Lo viejo de OTRA partida no se aplica a una diaria
        // Old data from ANOTHER run is not applied to a daily run
        var reg2 = new RegistroGuardias(null);
        reg2.FijarHeroe("heroe", 901, 11, "M");
        reg2.FijarOfertas(new Dictionary<int, int> { { 0, 900 } });
        string diariaDisco = Campana("D", "2026-11-02", 11, "1,2", "70,5,6", "Altdorf", 0, "4,0,0", "", "", "");
        comprobar(Vuelta(diariaDisco, reg2) == diariaDisco, "0.7.5 legado: lo viejo de la principal (otra firma, ofertas sin firma) no se aplica a la diaria");
    }

    // ---------------------------------------------------------------
    // Hallazgo 4: arrays nuevos de la 1.11.1 y coopStartGear
    // Finding 4: new 1.11.1 arrays and coopStartGear
    // ---------------------------------------------------------------
    private static void PruebaArraysNuevos(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        string t = Campana("M", "", 3, "1", "3,5,6", "A", 0, "4,0,0", "900,941,3", "944,5", "");
        t = t.Replace("\"coopStartArmy\": []", "\"coopStartArmy\": [900,2]").Replace("\"coopStartGear\": 0", "\"coopStartGear\": 715");
        comprobar(SaneadoSaves.ContarResiduos(t, Unidades()) == 4 && SaneadoObjetos.ContarResiduos(t, Objetos()) == 1,
            "0.7.5 arrays nuevos: battleRecruitOffer (2), mercenariesLeft (1), coopStartArmy (1) y coopStartGear cuentan como residuos");
        string d = Ida(t, reg);
        comprobar(Residuos(d) == 0 && d.Contains("\"battleRecruitOffer\": [70,31,3]") && d.Contains("\"mercenariesLeft\": [95,5]")
                  && d.Contains("\"coopStartArmy\": [70,2]") && d.Contains("\"coopStartGear\": 4"),
            "0.7.5 arrays nuevos: en disco van las bases (y el respaldo del objeto del cooperativo)");
        string v = Vuelta(d, reg);
        comprobar(v == t.Replace("\"coopStartGear\": 715", "\"coopStartGear\": 4"),
            "0.7.5 arrays nuevos: al leer vuelven 900/941 de la oferta de batalla, el 944 de mercenarios y el 900 del cooperativo (el objeto del cooperativo se queda con su respaldo)");
    }

    // ---------------------------------------------------------------
    // Hallazgos 24 y 29: registro de batallas e historial
    // Findings 24 and 29: battle log and history
    // ---------------------------------------------------------------
    private static void PruebaHistorial(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        string jugador =
            "{\n    \"lastHeroID\": 3,\n    \"runHistory\": [\n" +
            "        {\n            \"runUUID\": \"u-1\",\n            \"heroID\": 903,\n            \"squads\": [],\n            \"battles\": [" +
            Batalla(41, 0) + "," + Batalla(2, 901) + "]\n        },\n" +
            "        {\n            \"runUUID\": \"u-2\",\n            \"heroID\": 7,\n            \"battles\": [" + Batalla(1, 0) + "]\n        }\n    ],\n" +
            "    \"lifetime\": {\n        \"runs\": 3,\n        \"heroes\": [\n            {\n                \"heroID\": 7,\n                \"runs\": 1\n            },\n" +
            "            {\n                \"heroID\": 903,\n                \"runs\": 2\n            }\n        ]\n    },\n" +
            "    \"heroLooks\": [\n        {\n            \"HeroID\": 901,\n            \"Rank\": 2\n        }\n    ]\n}";
        comprobar(SaneadoSaves.ContarResiduosHistorial(jugador, Heroes(), Razas()) == 3 && SaneadoSaves.ContarResiduosHeroes(jugador, Heroes()) == 2,
            "0.7.5 historial: el héroe 903 y la raza 41 y el señor 901 de sus batallas cuentan; lifetime.heroes y heroLooks también");
        string d = Ida(jugador, reg);
        comprobar(Residuos(d) == 0 && Regex.IsMatch(d, "\"runUUID\": \"u-1\",\\s*\"heroID\": 7") && !d.Contains("\"heroID\": 903") && !d.Contains("\"HeroID\": 901")
                  && Regex.Matches(d, "\"heroID\": 7").Count == 3,
            "0.7.5 historial: en disco el 903 de la partida pasa a su prestado (7), la 41 a su sustituta, el señor 901 al 6 y las entradas 9xx de lifetime.heroes y heroLooks se apartan (sin fusionarse con la del 7)");
        comprobar(reg.PartidaAnclada("u-1") && !reg.PartidaAnclada("u-2"),
            "0.7.5 historial: la partida con contenido nuevo queda anclada (los logros la siguen reconociendo aunque el save ya no lleve el 903)");
        comprobar(Vuelta(d, reg) == jugador, "0.7.5 historial: al leer con los módulos activos todo vuelve (texto idéntico)");
        // Módulo de los héroes apagado: el historial se queda con el prestado y lo apuntado no se pierde al guardar
        // Heroes module off: the history keeps the borrowed hero and what was recorded is not lost on save
        string apagado = Vuelta(d, reg, id => false);
        string guardado = Ida(apagado, reg, id => false);
        reg.OlvidarSalidas();
        comprobar(Regex.IsMatch(apagado, "\"runUUID\": \"u-1\",\\s*\"heroID\": 7") && Vuelta(guardado, reg) == jugador,
            "0.7.5 historial: con el módulo apagado se lee el prestado, se guarda sin perder lo apuntado y al activarlo vuelve todo");
        // Registro de batallas de la partida en curso (save de campaña)
        // Battle log of the current run (campaign save)
        var regC = new RegistroGuardias(null);
        string camp = Campana("M", "", 3, "1", "3", "A", 0, "4,0,0", "", "", Batalla(40, 902) + "," + Batalla(3, 0));
        string dc = Ida(camp, regC);
        comprobar(Residuos(dc) == 0 && dc.Contains("\"race\": 0") && dc.Contains("\"warlord\": 5") && Vuelta(dc, regC) == camp,
            "0.7.5 battleLog: la raza 40 y el señor 902 de la partida en curso se guardan como su sustituta y su prestado y vuelven al leer");
    }

    // ---------------------------------------------------------------
    // Hallazgos 24 y 29: el save real del jugador del 02-11 (una copia)
    // Findings 24 and 29: the player's real save from 02-11 (a copy)
    // ---------------------------------------------------------------
    private static void PruebaSaveRealJugador(Action<bool, string> comprobar, string carpetaModulos)
    {
        if (string.IsNullOrEmpty(carpetaModulos)) return;
        string original = Path.GetFullPath(Path.Combine(carpetaModulos, "..", "backups", "20261102-saves_antes_074", "playerSaveData.json"));
        if (!File.Exists(original)) { comprobar(false, "0.7.5 save real: no está " + original); return; }
        // Siempre sobre una COPIA (el original no se abre para escribir nunca)
        // Always on a COPY (the original is never opened for writing)
        string copia = Path.Combine(Path.GetTempPath(), "zero_075_playerSaveData_" + Guid.NewGuid().ToString("N") + ".json");
        File.Copy(original, copia);
        try
        {
            string texto = File.ReadAllText(copia);
            int antes = IdsNuevos(texto);
            var reg = new RegistroGuardias(null);
            string disco = Ida(texto, reg);
            int despues = IdsNuevos(disco);
            comprobar(antes == 6 && despues == 0 && Residuos(disco) == 0,
                $"0.7.5 save real del jugador (02-11): {antes} ids nuevos antes (3 héroes del historial, 2 de lifetime.heroes, la raza 41) y {despues} tras sanear");
            reg.OlvidarSalidas();
            comprobar(Vuelta(disco, reg) == texto, "0.7.5 save real del jugador: al leer con los módulos activos vuelve todo (texto idéntico)");
            // El registro sobrevive a guardar y cargar (fichero temporal)
            // The registry survives saving and loading (temp file)
            string ruta = Path.Combine(Path.GetTempPath(), "zero_075_registro_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var enDisco = new RegistroGuardias(ruta);
                string d2 = Ida(texto, enDisco);
                var recargado = new RegistroGuardias(ruta);
                recargado.Cargar();
                comprobar(Vuelta(d2, recargado) == texto, "0.7.5 save real del jugador: con el registro guardado y vuelto a cargar también vuelve todo");
            }
            finally { try { File.Delete(ruta); } catch { } }
        }
        finally { try { File.Delete(copia); } catch { } }
    }

    // Ids nuevos de un save del jugador: héroes 900-999, razas 40/41, unidades 900-999, hechizos 1900-1999, objetos 700-799
    // New ids in a player save: heroes 900-999, races 40/41, units 900-999, spells 1900-1999, gear 700-799
    private static int IdsNuevos(string json)
    {
        int n = 0;
        void Andar(JsonElement e, string clave)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Object: foreach (var p in e.EnumerateObject()) Andar(p.Value, p.Name); break;
                case JsonValueKind.Array: foreach (var x in e.EnumerateArray()) Andar(x, clave); break;
                case JsonValueKind.Number:
                    if (!e.TryGetInt32(out int v)) break;
                    if ((clave == "heroID" || clave == "HeroID" || clave == "warlord" || clave == "lastHeroID") && v >= 900 && v <= 999) n++;
                    if ((clave == "race" || clave == "activeTavernThemeRace" || clave == "unlockedTavernThemes") && (v == 40 || v == 41)) n++;
                    if ((clave == "unit" || clave == "UnitName" || clave == "troopsRecruited" || clave == "troopsAcknowledged") && v >= 900 && v <= 999) n++;
                    if ((clave == "Spell" || clave == "spells" || clave == "spellsEverCast") && v >= 1900 && v <= 1999) n++;
                    if ((clave == "gear" || clave == "gearIdsCollected" || clave == "gearIdsAcknowledged" || clave == "lastStartingGearId"
                         || clave == "Gear" || clave == "SoldGear" || clave == "brokenGear" || clave == "recruitableGear" || clave == "townLootGearIDs"
                         || clave == "coopStartGear") && v >= 700 && v <= 799) n++;
                    if ((clave == "enemyWarlordHeroID") && v >= 900 && v <= 999) n++;
                    if ((clave == "townRace") && (v == 40 || v == 41)) n++;
                    if ((clave == "recruitableUnits" || clave == "battleRecruitOffer" || clave == "mercenariesLeft" || clave == "coopStartArmy"
                         || clave == "SquadsInArmy") && v >= 900 && v <= 999) n++;
                    if ((clave == "selectedSpells" || clave == "playerCustomBattleSpells") && v >= 1900 && v <= 1999) n++;
                    break;
            }
        }
        using var doc = JsonDocument.Parse(json);
        Andar(doc.RootElement, "");
        return n;
    }

    // ---------------------------------------------------------------
    // Hallazgo 25: logros de ejército por partida
    // Finding 25: army achievements per run
    // ---------------------------------------------------------------
    private static void PruebaLogros(Action<bool, string> comprobar)
    {
        const int Goblin = 50, Kobold = 51;
        var limpios = new HashSet<int> { Goblin };
        comprobar(!LogicaLogros.BloquearDesdeHistorial(Kobold, false, false, null),
            "0.7.5 logros: historial sin contenido nuevo, decide el juego");
        comprobar(!LogicaLogros.BloquearDesdeHistorial(Goblin, true, true, limpios) && LogicaLogros.BloquearDesdeHistorial(Kobold, true, true, limpios),
            "0.7.5 logros: con contenido nuevo en el historial, se concede el que da una partida de serie y se bloquea el que solo dan partidas con contenido nuevo");
        comprobar(LogicaLogros.BloquearDesdeHistorial(Goblin, true, false, limpios),
            "0.7.5 logros: si no se ha podido recontar por partida, se bloquea (regla vieja, ante la duda)");
        comprobar(LogicaLogros.BloquearSinContexto(true, false) && LogicaLogros.BloquearSinContexto(false, true) && !LogicaLogros.BloquearSinContexto(false, false),
            "0.7.5 logros: fuera de contexto se bloquea con módulos en la sesión o con la partida en curso anclada; el historial ya no cuenta ahí");
    }

    // ---------------------------------------------------------------
    // Hallazgo 37: OmitirParches no quita los bloqueos ni el filtro de logros
    // Finding 37: OmitirParches does not remove the blocks or the achievements filter
    // ---------------------------------------------------------------
    private static void PruebaOmisiones(Action<bool, string> comprobar)
    {
        comprobar(!Omisiones.SePuedeOmitir(Omisiones.Proteccion.EnLinea, true, true, true) && Omisiones.SePuedeOmitir(Omisiones.Proteccion.EnLinea, false, true, true)
                  && Omisiones.SePuedeOmitir(Omisiones.Proteccion.EnLinea, true, false, true),
            "0.7.5 omisiones: un bloqueo en línea no se omite con módulos aplicados y el bloqueo encendido (sí sin módulos o con el bloqueo apagado)");
        comprobar(!Omisiones.SePuedeOmitir(Omisiones.Proteccion.Logros, false, false, true) && Omisiones.SePuedeOmitir(Omisiones.Proteccion.Logros, true, true, false)
                  && Omisiones.SePuedeOmitir(Omisiones.Proteccion.Ninguna, true, true, true),
            "0.7.5 omisiones: el filtro de logros no se omite con Logros.Filtrar = true; un parche sin protección, siempre");
    }

    // ---------------------------------------------------------------
    // Hallazgo 9 (lista de bajos): fusión de los hechizos cuando el texto no trae ninguno propio
    // Finding 9 (low list): spell merging when the text has no own spells
    // ---------------------------------------------------------------
    private static void PruebaFusionHechizos(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        string dos = Campana("F", "", 3, "1", "3", "A", 0, "1901,1903,0", "", "", "");
        string d = SaneadoHechizos.ParaDisco(dos, reg);
        reg.OlvidarSalidas();
        // Con los dos módulos apagados el juego reescribe la ranura 1 (la 0 sigue a 0): solo se olvida la 1903
        // With both modules off the game rewrites slot 1 (0 stays 0): only 1903 is forgotten
        string reescrito = d.Replace("\"selectedSpells\": [0,0,0]", "\"selectedSpells\": [0,4,0]");
        SaneadoHechizos.ParaDisco(reescrito, reg);
        comprobar(reg.HechizosDe(Program.R(dos, "seleccion"), out _, out var pos, out _) && pos.Count == 1 && pos[0] == 1901,
            "0.7.5 hechizos: sin propios en el texto, se conservan las posiciones que siguen a 0 (1901) y solo se olvida la reescrita (antes se olvidaban todas)");
    }

    // ---------------------------------------------------------------
    // Ronda 2: carpeta de los saves reales y tablas «de la sesión» (héroes 901-904 y razas 40/41 instalados)
    // Round 2: real saves folder and «session» tables (heroes 901-904 and races 40/41 installed)
    // ---------------------------------------------------------------
    private static string CarpetaBackups(string carpetaModulos) =>
        string.IsNullOrEmpty(carpetaModulos) ? null : Path.GetFullPath(Path.Combine(carpetaModulos, "..", "backups"));

    // Copia un save real a la carpeta temporal y devuelve su texto (el original no se abre para escribir nunca)
    // Copies a real save to the temp folder and returns its text (the original is never opened for writing)
    private static string LeerCopia(string ruta)
    {
        string copia = Path.Combine(Path.GetTempPath(), "zero_075r2_" + Guid.NewGuid().ToString("N") + ".json");
        File.Copy(ruta, copia);
        try { return File.ReadAllText(copia); }
        finally { try { File.Delete(copia); } catch { } }
    }

    private static TablasSaneado Tablas(Func<int, bool> heroeActivo = null, bool razasActivas = true) => new TablasSaneado
    {
        Unidades = Unidades(), Razas = razasActivas ? Razas() : null, RazasHistorial = Razas(), Objetos = Objetos(), ObjetosActivos = true,
        Heroes = Heroes(heroeActivo), HechizoDisponible = id => id >= 1901 && id <= 1904,
    };

    // ---------------------------------------------------------------
    // Ronda 2 (alto): partidas del historial de antes de que el registro marcara las partidas.
    // Solo corre con un save propio del autor, que no se publica (sin él, falla con «no está»). En la copia pública,
    // los UUID de las partidas son inventados.
    // Round 2 (high): history runs from before the registry marked runs.
    // Only runs with the author's own save, which is not published (without it, it fails with «no está»). In the public copy,
    // the run UUIDs are made up.
    // ---------------------------------------------------------------
    private static void PruebaPartidasSinMarcas(Action<bool, string> comprobar, string carpetaModulos)
    {
        long Ticks(int d, int h) => new DateTime(2026, 10, d, h, 0, 0, DateTimeKind.Utc).Ticks;
        comprobar(!LogicaLogros.SinMarcasPosibles(Ticks(6, 11)) && LogicaLogros.SinMarcasPosibles(Ticks(16, 8)) && LogicaLogros.SinMarcasPosibles(Ticks(18, 23))
                  && !LogicaLogros.SinMarcasPosibles(Ticks(19, 1)) && !LogicaLogros.SinMarcasPosibles(0),
            "0.7.5 r2 logros: entre el 16-10 y el 19-10 (UTC) una partida cuenta como jugada con contenido nuevo aunque no tenga marca; antes y después, no");
        string ruta = CarpetaBackups(carpetaModulos) == null ? null : Path.Combine(CarpetaBackups(carpetaModulos), "20261102-saves_antes_074", "playerSaveData.json");
        if (ruta == null || !File.Exists(ruta)) { comprobar(false, "0.7.5 r2 logros: no está " + ruta); return; }
        using var doc = JsonDocument.Parse(LeerCopia(ruta));
        var modificadas = new List<string>();
        var anclada = new HashSet<string> { "331db69a-42f2-45ab-88a3-69576fdaceb5", "f9f1033b-8634-4913-8f50-3b06e981d196", "8ee76f00-2835-4b06-8b6b-f06a72bcd963" };
        foreach (var r in doc.RootElement.GetProperty("runHistory").EnumerateArray())
        {
            string uuid = r.GetProperty("runUUID").GetString();
            long fin = r.GetProperty("endedAtUtcTicks").GetInt64();
            var unidades = r.GetProperty("squads").EnumerateArray().Select(x => x.GetProperty("unit").GetInt32()).ToList();
            bool nueva = LogicaLogros.SinMarcasPosibles(fin)
                         || LogicaLogros.HistorialConContenidoNuevo(new[] { (uuid, r.GetProperty("heroID").GetInt32(), (IEnumerable<int>)unidades) }, anclada.Contains);
            if (nueva) modificadas.Add(uuid.Substring(0, 8));
        }
        comprobar(modificadas.Contains("ba32f363") && !modificadas.Contains("536d85d2") && modificadas.Count == 7,
            $"0.7.5 r2 logros, save real: la ba32f363 (17-10, solo con la base de la 900) sale como partida con contenido nuevo; la 536d85d2 (06-10) no ({string.Join(",", modificadas)})");
    }

    // ---------------------------------------------------------------
    // Ronda 2: historial saneado aunque el módulo de la facción esté apagado
    // Round 2: history sanitized even if the faction module is off
    // ---------------------------------------------------------------
    private static void PruebaHistorialModuloApagado(Action<bool, string> comprobar, string carpetaModulos)
    {
        string ruta = CarpetaBackups(carpetaModulos) == null ? null : Path.Combine(CarpetaBackups(carpetaModulos), "20261102-saves_antes_074", "playerSaveData.json");
        if (ruta == null || !File.Exists(ruta)) return;
        string texto = LeerCopia(ruta);
        var reg = new RegistroGuardias(null);
        // Astra e Imperio apagados: los héroes no están activos y no hay tabla de razas activas, pero sí la de los instalados
        // Astra and Imperio off: the heroes are not active and there is no active races table, but there is the installed one
        var apagados = Tablas(id => false, razasActivas: false);
        string disco = TuberiaSaneado.ParaDisco(texto, apagados, reg);
        reg.OlvidarSalidas();
        comprobar(IdsNuevos(texto) == 6 && IdsNuevos(disco) == 0 && TuberiaSaneado.DesdeDisco(disco, Tablas(), reg) == texto,
            "0.7.5 r2 historial: con los módulos de las facciones apagados no queda ningún 901/903/41 en disco, y al activarlos vuelve todo");
    }

    // ---------------------------------------------------------------
    // Ronda 2: todos los saves reales copiados por la tubería completa de escritura
    // Round 2: all real saves copied through the full write pipeline
    // ---------------------------------------------------------------
    private static void PruebaSavesReales(Action<bool, string> comprobar, string carpetaModulos)
    {
        string backups = CarpetaBackups(carpetaModulos);
        if (backups == null || !Directory.Exists(backups)) return;
        var rutas = Directory.GetDirectories(backups, "*saves*")
            .SelectMany(d => Directory.GetFiles(d, "*SaveData*.json", SearchOption.AllDirectories))
            .OrderBy(x => x).ToList();
        int bien = 0;
        var mal = new List<string>();
        foreach (string r in rutas)
        {
            string texto = LeerCopia(r);
            var reg = new RegistroGuardias(null);
            string disco = TuberiaSaneado.ParaDisco(texto, Tablas(), reg);
            reg.OlvidarSalidas();
            int quedan = IdsNuevos(disco) + TuberiaSaneado.ResiduosPorTipo(disco, Tablas()).Values.Sum();
            bool vuelta = TuberiaSaneado.DesdeDisco(disco, Tablas(), reg) == texto;
            if (quedan == 0 && vuelta) bien++; else mal.Add($"{Path.GetFileName(Path.GetDirectoryName(r))}\\{Path.GetFileName(r)} (quedan {quedan}, vuelta {(vuelta ? "sí" : "NO")})");
        }
        comprobar(rutas.Count >= 10 && mal.Count == 0,
            $"0.7.5 r2 saves reales: {bien}/{rutas.Count} saves copiados de backups pasan por la tubería completa sin ids nuevos y vuelven idénticos" + (mal.Count > 0 ? ": " + string.Join("; ", mal) : ""));
    }

    // ---------------------------------------------------------------
    // Ronda 2: la raza de una ranura vieja (sin partida) solo vale para la partida principal
    // Round 2: the race of an old slot (no run) is only valid for the main run
    // ---------------------------------------------------------------
    private static void PruebaRazaViejaSoloPrincipal(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        reg.FijarRaza("ciudad", 40, "Cadia|2");
        string principal = Campana("M", "", 3, "1", "3", "Cadia", 0, "4,0,0", "", "", "");
        string diaria = Campana("D", "2026-11-02", 3, "1", "3", "Cadia", 0, "4,0,0", "", "", "");
        comprobar(SaneadoSaves.RazasDesdeDisco(principal, Razas(), reg).Contains("\"townRace\": 40") && SaneadoSaves.RazasDesdeDisco(diaria, Razas(), reg) == diaria,
            "0.7.5 r2 razas: la ciudad apuntada por la 0.7.4 (ranura vieja) vuelve en la principal y no en una diaria con una ciudad del mismo nombre");
    }
}
