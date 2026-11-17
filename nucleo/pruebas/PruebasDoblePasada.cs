// PruebasDoblePasada.cs
// Prueba 15 (revisión de la 0.7.2): el juego pasa cada save DOS veces por el saneado de escritura (postfix de
// JsonUtility.ToJson y prefix de File.WriteAllText con el mismo texto). Antes, la segunda pasada olvidaba lo apuntado.
// Aquí, fuera del juego:
//   - ParaDisco(ParaDisco(x)) en cada etapa (objetos: Gear, ofertas, botín, vendidos, rotos y lastStartingGearId;
//     razas: ciudad y campo de batalla; unidades: ofertas de reclutamiento; héroes: héroe, señor de la guerra y
//     lastHeroID) deja el registro igual (sin escrituras nuevas) y la lectura devuelve el texto original;
//   - la tubería completa en el orden de Saves.SanearSalida (unidades, razas, objetos, héroes) pasada dos veces;
//   - casos negativos: un texto nuevo con el héroe prestado (o None en el equipo inicial) elegido a propósito SÍ olvida lo
//     apuntado si el real está activo (MEDIA 1); con el módulo apagado se conserva;
//   - victorias de héroes nuevos del save del jugador (HeroDifficultiesCompleted y HeroLastDifficulties, MEDIA 2): se
//     quitan al escribir (también sin tabla de héroes), vuelven al leer solo con el héroe activo, y la ida y vuelta con el
//     save real del jugador devuelve el mismo contenido.
// Autor: ZeroitMods.
// PruebasDoblePasada.cs
// Test 15 (review of 0.7.2): the game passes each save TWICE through the write sanitizing (postfix of
// JsonUtility.ToJson and prefix of File.WriteAllText with the same text). Before, the second pass forgot what had been recorded.
// Here, outside the game:
//   - ParaDisco(ParaDisco(x)) at each stage (gear: Gear, offers, loot, sold, broken and lastStartingGearId;
//     races: city and battlefield; units: recruitment offers; heroes: hero, warlord and
//     lastHeroID) leaves the registry unchanged (no new writes) and reading returns the original text;
//   - the full pipeline in the order of Saves.SanearSalida (units, races, gear, heroes) passed twice;
//   - negative cases: a new text with the borrowed hero (or None in the starting gear) chosen on purpose DOES forget what was
//     recorded if the real one is active (MEDIA 1); with the module off it is kept;
//   - new heroes' victories in the player's save (HeroDifficultiesCompleted and HeroLastDifficulties, MEDIA 2): they are
//     removed when writing (also without a hero table), come back when reading only with the hero active, and the round trip with the
//     player's real save returns the same content.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZeroNucleo;

internal static class PruebasDoblePasada
{
    // ---------------------------------------------------------------
    // Ayudantes
    // Helpers
    // ---------------------------------------------------------------
    // Texto sin espacios ni saltos (JsonUtility no los tiene en cuenta al leer)
    // Text without spaces or line breaks (JsonUtility ignores them when reading)
    private static string SinBlancos(string s) => Regex.Replace(s ?? "", @"\s+", "");

    // Valor del primer campo numérico «clave» del texto (-999 si no está)
    // Value of the first numeric field «clave» in the text (-999 if absent)
    private static int Campo(string json, string clave)
    {
        var m = Regex.Match(json, "\"" + clave + "\"\\s*:\\s*(-?\\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : -999;
    }

    // Save de campaña sintético con todo lo que miran los saneados (formato compacto en los arrays)
    // Synthetic campaign save with everything the sanitizing steps look at (compact format in the arrays)
    private static string Campana(string runId, string gear = "17,700,715", string ofertas = "715,3", string botin = "700",
                                  string vendidos = "700", string rotos = "715", int heroe = 901, int senor = 903,
                                  int raza = 40, string unidades = "900,5", int oro = 100)
    {
        return "{\n    \"saveVersion\": 1,\n    \"seed\": 7,\n" +
               $"    \"runId\": \"{runId}\",\n    \"gold\": {oro},\n" +
               "    \"playerArmy\": [\n        {\n            \"UnitName\": 900,\n            \"UniqueID\": \"g-900\"\n        }\n    ],\n" +
               $"    \"recruitableUnits\": [{unidades}],\n" +
               $"    \"Gear\": [{gear}],\n    \"recruitableGear\": [{ofertas}],\n    \"townLootGearIDs\": [{botin}],\n" +
               $"    \"SoldGear\": [{vendidos}],\n    \"brokenGear\": [{rotos}],\n" +
               $"    \"townData\": {{ \"townName\": \"Cadia\", \"townSize\": 1, \"townRace\": {raza} }},\n" +
               $"    \"battleFieldPreset\": {{ \"mapRegion\": {{ \"a\": 1 }}, \"race\": {raza}, \"seed\": 5 }},\n" +
               $"    \"heroID\": {heroe},\n    \"enemyWarlordHeroID\": {senor},\n    \"snapShot\": false\n}}";
    }

    // Save del jugador sintético (equipo inicial, último héroe, colección y victorias por héroe). Las entradas del 901 van
    // al final de cada lista porque al leer se añaden al final (el juego las busca por HeroID, el orden no importa)
    // Synthetic player save (starting gear, last hero, Collection and victories per hero). The 901 entries go
    // at the end of each list because when reading they are appended at the end (the game looks them up by HeroID, order doesn't matter)
    private static string Jugador(int ultimo = 901, int inicial = 715, int partidas = 3, bool conVictorias = true)
    {
        string victorias = !conVictorias ? "" :
            "    \"HeroDifficultiesCompleted\": [\n        {\n            \"HeroID\": 11,\n            \"DifficultiesCompleted\": [\n                11\n            ]\n        },\n" +
            "        {\n            \"HeroID\": 901,\n            \"DifficultiesCompleted\": [\n                3,\n                5\n            ]\n        }\n    ],\n" +
            "    \"HeroLastDifficulties\": [\n        {\n            \"HeroID\": 11,\n            \"LastDifficulty\": 11\n        },\n" +
            "        {\n            \"HeroID\": 901,\n            \"LastDifficulty\": 5\n        }\n    ],\n";
        return "{\n" + $"    \"totalRuns\": {partidas},\n    \"lastHeroID\": {ultimo},\n    \"lastStartingGearId\": {inicial},\n" +
               "    \"gearIdsCollected\": [1,715],\n" + victorias + "    \"activeTavernThemeRace\": 0\n}";
    }

    // Tablas de prueba: objetos 715 -> 4 y 700 -> 17; razas 40 -> 0; unidades 900 -> 70; héroes 901 -> 7 y 903 -> 10
    // Test tables: gear 715 -> 4 and 700 -> 17; races 40 -> 0; units 900 -> 70; heroes 901 -> 7 and 903 -> 10
    private static TablaObjetos Objetos() => new TablaObjetos(new Dictionary<int, int> { { 715, 4 }, { 700, 17 } }, Enumerable.Range(1, 40));
    private static TablaRazas Razas() => new TablaRazas(new Dictionary<int, int> { { 40, 0 } });
    private static TablaUnidades Unidades() => new TablaUnidades(new Dictionary<int, int> { { 900, 70 } });
    private static TablaHeroes Heroes(HashSet<int> activos) =>
        new TablaHeroes(new Dictionary<int, int> { { 901, 7 }, { 903, 10 } }) { Disponible = activos.Contains };

    // Tubería de escritura en el orden de Saves.SanearSalida (lo que hace el juego en cada pasada)
    // Write pipeline in the order of Saves.SanearSalida (what the game does on each pass)
    private static string Escribir(string json, RegistroGuardias reg, TablaUnidades u, TablaRazas r, TablaObjetos o, TablaHeroes h)
    {
        string s = SaneadoSaves.ParaDisco(json, u, reg);
        s = SaneadoSaves.RazasParaDisco(s, r, reg);
        s = SaneadoObjetos.ParaDisco(s, o, reg, true);
        return SaneadoSaves.HeroesParaDisco(s, h, reg);
    }

    // Tubería de lectura en el orden de Saves.AntesDeFromJson
    // Read pipeline in the order of Saves.AntesDeFromJson
    private static string Leer(string json, RegistroGuardias reg, TablaUnidades u, TablaRazas r, TablaObjetos o, TablaHeroes h)
    {
        string s = SaneadoSaves.DesdeDisco(json, u, reg);
        s = SaneadoSaves.RazasDesdeDisco(s, r, reg);
        s = SaneadoObjetos.DesdeDisco(s, o, reg);
        return SaneadoSaves.HeroesDesdeDisco(s, h, reg);
    }

    public static void Todas(Action<bool, string> comprobar, string jugadorReal = null)
    {
        PruebaEtapas(comprobar);
        PruebaTuberia(comprobar);
        PruebaNegativos(comprobar);
        PruebaVictorias(comprobar, jugadorReal);
    }

    // ---------------------------------------------------------------
    // Cada etapa por separado: ParaDisco(ParaDisco(x))
    // Each stage separately: ParaDisco(ParaDisco(x))
    // ---------------------------------------------------------------
    private static void PruebaEtapas(Action<bool, string> comprobar)
    {
        // Objetos del save de campaña (Gear, ofertas, botín, vendidos y rotos)
        // Gear in the campaign save (Gear, offers, loot, sold and broken)
        var to = Objetos();
        var reg = new RegistroGuardias(null);
        string c = Campana("A");
        string d1 = SaneadoObjetos.ParaDisco(c, to, reg, true);
        int escrituras = reg.Escrituras;
        string d2 = SaneadoObjetos.ParaDisco(d1, to, reg, true);
        comprobar(d2 == d1 && reg.Escrituras == escrituras, "doble pasada (objetos de campaña): la segunda pasada no cambia el texto ni escribe el registro");
        comprobar(reg.ObjetosPosicion(Program.R(c, "Gear")).Count == 2 && reg.ObjetosPosicion(Program.R(c, "recruitableGear")).Count == 1 && reg.ObjetosPosicion(Program.R(c, "townLootGearIDs")).Count == 1
                  && reg.ObjetosLista(Program.R(c, "SoldGear")).Contains(700) && reg.ObjetosLista(Program.R(c, "brokenGear")).Contains(715),
            "doble pasada (objetos de campaña): siguen apuntados 2 del Gear, 1 oferta, 1 de botín, 1 vendido y 1 roto");
        comprobar(SaneadoObjetos.DesdeDisco(d2, to, reg) == c, "doble pasada (objetos de campaña): la lectura devuelve el texto original (Gear [17,700,715], ofertas, botín, vendidos y rotos)");

        // Equipo inicial recordado del save del jugador (lastStartingGearId)
        // Remembered starting gear in the player's save (lastStartingGearId)
        var regJ = new RegistroGuardias(null);
        string j = Jugador();
        string j1 = SaneadoObjetos.ParaDisco(j, to, regJ, true);
        string j2 = SaneadoObjetos.ParaDisco(j1, to, regJ, true);
        comprobar(Campo(j1, "lastStartingGearId") == 0 && j2 == j1 && regJ.ObjetosPosicion("lastStartingGearId").Count == 1,
            "doble pasada (lastStartingGearId): se escribe None y lo apuntado sigue tras la segunda pasada");
        comprobar(Campo(SaneadoObjetos.DesdeDisco(j2, to, regJ), "lastStartingGearId") == 715, "doble pasada (lastStartingGearId): al leer vuelve el 715 (antes salía 0)");

        // Razas (ciudad y campo de batalla), la reproducción de la revisión
        // Races (city and battlefield), the review's reproduction
        var tr = Razas();
        var regR = new RegistroGuardias(null);
        string r1 = SaneadoSaves.RazasParaDisco(c, tr, regR);
        string r2 = SaneadoSaves.RazasParaDisco(r1, tr, regR);
        string rl = SaneadoSaves.RazasDesdeDisco(r2, tr, regR);
        comprobar(Campo(r1, "townRace") == 0 && Campo(rl, "townRace") == 40 && Campo(rl, "race") == 40,
            "doble pasada (razas): la ciudad y el campo de batalla vuelven a 40 al leer (antes volvían a 0)");

        // Unidades: ofertas de reclutamiento (la revisión no lo señaló: la segunda pasada también las olvidaba)
        // Units: recruitment offers (the review did not flag it: the second pass also missed them)
        var tu = Unidades();
        var regU = new RegistroGuardias(null);
        string u1 = SaneadoSaves.ParaDisco(c, tu, regU);
        string u2 = SaneadoSaves.ParaDisco(u1, tu, regU);
        comprobar(regU.Ofertas(Program.R(c, "ofertas")).Count == 1 && SaneadoSaves.DesdeDisco(u2, tu, regU).Contains("\"recruitableUnits\": [900,5]"),
            "doble pasada (unidades): la oferta 900 sigue apuntada y vuelve al leer");

        // Héroes del save de campaña y lastHeroID del save del jugador
        // Heroes from the campaign save and lastHeroID from the player's save
        var th = Heroes(new HashSet<int> { 901, 903 });
        var regH = new RegistroGuardias(null);
        string h1 = SaneadoSaves.HeroesParaDisco(c, th, regH);
        string h2 = SaneadoSaves.HeroesParaDisco(h1, th, regH);
        string hl = SaneadoSaves.HeroesDesdeDisco(h2, th, regH);
        comprobar(Campo(hl, "heroID") == 901 && Campo(hl, "enemyWarlordHeroID") == 903, "doble pasada (héroes de campaña): vuelven el 901 y el 903");
        string l1 = SaneadoSaves.HeroesParaDisco(j, th, regH);
        string l2 = SaneadoSaves.HeroesParaDisco(l1, th, regH);
        comprobar(Campo(l1, "lastHeroID") == 7 && Campo(SaneadoSaves.HeroesDesdeDisco(l2, th, regH), "lastHeroID") == 901,
            "doble pasada (lastHeroID): se escribe el 7 y al leer vuelve el 901 tras dos pasadas");
    }

    // ---------------------------------------------------------------
    // Tubería completa dos veces (lo que pasa en el juego al guardar)
    // Full pipeline twice (what happens in the game when saving)
    // ---------------------------------------------------------------
    private static void PruebaTuberia(Action<bool, string> comprobar)
    {
        var activos = new HashSet<int> { 901, 903 };
        var (tu, tr, to, th) = (Unidades(), Razas(), Objetos(), Heroes(activos));
        foreach (var (nombre, original) in new[] { ("save de campaña", Campana("A")), ("save del jugador", Jugador()) })
        {
            var reg = new RegistroGuardias(null);
            string p1 = Escribir(original, reg, tu, tr, to, th);
            int escrituras = reg.Escrituras;
            string p2 = Escribir(p1, reg, tu, tr, to, th);
            comprobar(p2 == p1 && reg.Escrituras == escrituras, $"tubería dos veces ({nombre}): la segunda pasada no cambia nada ni escribe el registro");
            comprobar(SaneadoSaves.ContarResiduos(p2, tu) + SaneadoSaves.ContarResiduosRazas(p2, tr) + SaneadoObjetos.ContarResiduos(p2, to) + SaneadoSaves.ContarResiduosHeroes(p2, th) == 0,
                $"tubería dos veces ({nombre}): no queda nada nuevo en el texto guardado");
            comprobar(SinBlancos(Leer(p2, reg, tu, tr, to, th)) == SinBlancos(original), $"tubería dos veces ({nombre}): la lectura devuelve el contenido original");
            // El texto saneado entero que recuerda Saves.SanearSalida (segunda red de escritura)
            // The entire sanitized text that Saves.SanearSalida remembers (second write safety net)
            reg.RecordarSalida(p2, RegistroGuardias.EtapasTodas);
            comprobar(reg.EsSalidaCompleta(p2) && !reg.EsSalidaCompleta(original), $"tubería ({nombre}): el texto saneado entero se reconoce como salida completa y el original no");
        }
    }

    // ---------------------------------------------------------------
    // Casos negativos: lo elegido a propósito se respeta (MEDIA 1)
    // Negative cases: what was chosen on purpose is respected (MEDIA 1)
    // ---------------------------------------------------------------
    private static void PruebaNegativos(Action<bool, string> comprobar)
    {
        var activos = new HashSet<int> { 901, 903 };
        var th = Heroes(activos);
        var to = Objetos();
        var reg = new RegistroGuardias(null);
        SaneadoSaves.HeroesParaDisco(Jugador(901), th, reg);
        SaneadoObjetos.ParaDisco(Jugador(901), to, reg, true);

        // El jugador empieza otra partida con el 7 (el prestado del 901) y sin equipo inicial: texto distinto
        // The player starts another run with 7 (the borrowed hero from 901) and without starting gear: different text
        string otra = Jugador(7, 0, partidas: 4);
        string escrita = SaneadoObjetos.ParaDisco(SaneadoSaves.HeroesParaDisco(otra, th, reg), to, reg, true);
        string leida = SaneadoObjetos.DesdeDisco(SaneadoSaves.HeroesDesdeDisco(escrita, th, reg), to, reg);
        comprobar(Campo(leida, "lastHeroID") == 7, "MEDIA 1: con el 901 activo, el héroe prestado elegido a propósito se respeta (el reinicio rápido ya no arranca con el 901)");
        comprobar(Campo(leida, "lastStartingGearId") == 0, "equipo inicial: None elegido a propósito con el 715 activo se respeta");

        // Con el módulo del 901 apagado se conserva para cuando se active
        // With the 901 module turned off, it is kept for when it is activated
        activos.Remove(901);
        var regB = new RegistroGuardias(null);
        var thTodos = Heroes(new HashSet<int> { 901, 903 });
        SaneadoSaves.HeroesParaDisco(Jugador(901), thTodos, regB);
        string apagado = Jugador(7, 715, partidas: 5);
        string e2 = SaneadoSaves.HeroesParaDisco(apagado, th, regB);
        comprobar(Campo(SaneadoSaves.HeroesDesdeDisco(e2, th, regB), "lastHeroID") == 7 && Campo(SaneadoSaves.HeroesDesdeDisco(e2, thTodos, regB), "lastHeroID") == 901,
            "lastHeroID: con el módulo del 901 apagado se lee el 7 y lo apuntado se conserva (al activarlo vuelve el 901)");
    }

    // ---------------------------------------------------------------
    // Victorias de héroes nuevos en el save del jugador (MEDIA 2)
    // New heroes' victories in the player's save (MEDIA 2)
    // ---------------------------------------------------------------
    private static void PruebaVictorias(Action<bool, string> comprobar, string jugadorReal)
    {
        var activos = new HashSet<int> { 901, 903 };
        var th = Heroes(activos);
        var reg = new RegistroGuardias(null);
        string j = Jugador();
        string e = SaneadoSaves.HeroesParaDisco(j, th, reg);
        comprobar(!Regex.IsMatch(e, "\"HeroID\"\\s*:\\s*9\\d\\d") && Regex.Matches(e, "\"HeroID\"\\s*:\\s*11").Count == 2 && SaneadoSaves.ContarResiduosHeroes(e, th) == 0,
            "MEDIA 2: las entradas del 901 se quitan de HeroDifficultiesCompleted y HeroLastDifficulties (quedan las del 11)");
        comprobar(SaneadoSaves.ContarResiduosHeroes(j, th) == 3, "MEDIA 2: el recuento de residuos ve las 2 victorias del 901 y su lastHeroID");
        string l = SaneadoSaves.HeroesDesdeDisco(e, th, reg);
        comprobar(Regex.Matches(l, "\"HeroID\"\\s*:\\s*901").Count == 2 && SinBlancos(l).Contains("{\"HeroID\":901,\"DifficultiesCompleted\":[3,5]}"),
            "MEDIA 2: con el 901 activo sus victorias vuelven al leer, tal cual");
        activos.Remove(901);
        comprobar(!SaneadoSaves.HeroesDesdeDisco(e, th, reg).Contains("\"HeroID\": 901"), "MEDIA 2: con el módulo del 901 apagado sus victorias no vuelven (el juego de serie no las cuenta)");
        activos.Add(901);

        // Sin tabla de héroes (ningún módulo con héroes instalado): las 9xx se quitan igual y se apuntan
        // No hero table (no module with heroes installed): the 9xx entries are removed all the same and recorded
        var regSin = new RegistroGuardias(null);
        string sinTabla = SaneadoSaves.HeroesParaDisco(j, null, regSin);
        comprobar(!sinTabla.Contains("\"HeroID\": 901") && regSin.EntradasHeroes("HeroDifficultiesCompleted").ContainsKey(901) && SaneadoSaves.ContarResiduosHeroes(sinTabla, null) == 0,
            "MEDIA 2: sin tabla de héroes las victorias 9xx también se quitan del save y quedan en el registro");

        // Lista que se queda vacía y vuelve a llenarse (formato «[]» de JsonUtility)
        // List that ends up empty and gets filled again (JsonUtility's «[]» format)
        var regV = new RegistroGuardias(null);
        string solo = "{\n    \"lastHeroID\": 2,\n    \"HeroLastDifficulties\": [\n        {\n            \"HeroID\": 903,\n            \"LastDifficulty\": 2\n        }\n    ]\n}";
        string vacia = SaneadoSaves.HeroesParaDisco(solo, th, regV);
        comprobar(vacia.Contains("\"HeroLastDifficulties\": []") && SinBlancos(SaneadoSaves.HeroesDesdeDisco(vacia, th, regV)) == SinBlancos(solo),
            "MEDIA 2: una lista que se queda vacía se escribe «[]» y al leer vuelve a tener la entrada del 903");

        // Save real del jugador con una victoria del 901 añadida: ida y vuelta con el mismo contenido
        // Real player save with a 901 victory added: round trip with the same content
        if (jugadorReal != null && jugadorReal.Contains("\"HeroDifficultiesCompleted\""))
        {
            var regReal = new RegistroGuardias(null);
            string conNuevo = Regex.Replace(jugadorReal, "(\"HeroDifficultiesCompleted\"\\s*:\\s*\\[)",
                "$1\n        {\n            \"HeroID\": 902,\n            \"DifficultiesCompleted\": [\n                4\n            ]\n        },", RegexOptions.None, TimeSpan.FromSeconds(5));
            var thReal = new TablaHeroes(new Dictionary<int, int> { { 902, 3 } });
            string escritoReal = SaneadoSaves.HeroesParaDisco(conNuevo, thReal, regReal);
            comprobar(!escritoReal.Contains("\"HeroID\": 902") && SinBlancos(escritoReal) == SinBlancos(SaneadoSaves.HeroesParaDisco(jugadorReal, thReal, new RegistroGuardias(null))),
                "MEDIA 2 (save real del jugador): sin la victoria del 902 el texto escrito es el del save real");
            comprobar(SaneadoSaves.HeroesDesdeDisco(escritoReal, thReal, regReal).Contains("\"HeroID\": 902"),
                "MEDIA 2 (save real del jugador): al leer vuelve la victoria del 902");
        }
    }
}
