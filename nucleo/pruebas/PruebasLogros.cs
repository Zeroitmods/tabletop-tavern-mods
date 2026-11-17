// PruebasLogros.cs
// Prueba 14 (0.7.2, 17-10-2026): logros de Steam de serie solo con contenido de serie (LogicaLogros.cs) y condición
// UnitName de las reglas de objetos (ReglasObjetos.cs, M1 de la revisión de datos: el 721 Grog de Hongo también para la 946).
//   - Colección de raza: lista de serie completa con sueltas 9xx sin reclutar -> se concede; falta una de serie -> no;
//     lista vacía tras filtrar -> no (misma guarda que el juego).
//   - Héroes: 16 de serie + 904 -> se concede; 15 de serie + 901 -> se bloquea; 15 de serie sin nuevos -> decide el juego.
//   - Ejército: razas, legendarias y rasgos sin las unidades 9xx (y sin las razas 40/41, que solo llegan con 9xx).
//   - Reglas: UnitName con «946» sale tal cual en el JSON del cargador; sin unidades se omite.
//   - Manifiesto real (si se pasa la carpeta de módulos): el 721 lleva sus reglas Goblin y dos más con UnitName 946.
// Autor: ZeroitMods.
// PruebasLogros.cs
// Test 14 (0.7.2, 17-10-2026): stock Steam achievements with stock content only (LogicaLogros.cs) and the
// UnitName condition of the gear rules (ReglasObjetos.cs, M1 of the data review: the Hongo's 721 Grog also for 946).
//   - Race Collection: full stock list with unrecruited loose 9xx -> granted; one stock missing -> not granted;
//     empty list after filtering -> not granted (same guard as the game).
//   - Heroes: 16 stock + 904 -> granted; 15 stock + 901 -> blocked; 15 stock with no new ones -> the game decides.
//   - Army: races, legendaries and traits without the 9xx units (and without races 40/41, which only come with 9xx).
//   - Rules: UnitName with «946» comes out as is in the loader JSON; with no units it is omitted.
//   - Real manifest (if the modules folder is passed): 721 carries its Goblin rules and two more with UnitName 946.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using ZeroNucleo;

internal static class PruebasLogros
{
    public static void Todas(Action<bool, string> comprobar, Manifiesto manifiesto)
    {
        // -------- Unidades y héroes de serie
        // -------- Stock units and heroes
        comprobar(LogicaLogros.EsUnidadDeSerie(0) && LogicaLogros.EsUnidadDeSerie(899) && !LogicaLogros.EsUnidadDeSerie(900) && !LogicaLogros.EsUnidadDeSerie(-1),
            "logros: unidades de serie 0-899; 900 en adelante y negativas no");
        comprobar(LogicaLogros.EsHeroeDeSerie(1) && LogicaLogros.EsHeroeDeSerie(16) && !LogicaLogros.EsHeroeDeSerie(901) && !LogicaLogros.EsHeroeDeSerie(0),
            "logros: héroes de serie 1-899; 0 y 901 no");

        // -------- Colección de raza (las 9xx bloquean en el juego)
        // -------- Race Collection (9xx block in the game)
        var lista = new[] { 1, 2, 3, 944, 960 };                  // 3 de serie y 2 sueltas
                                                                  // 3 stock and 2 loose
        comprobar(LogicaLogros.TieneUnidadesNuevas(lista) && !LogicaLogros.TieneUnidadesNuevas(new[] { 1, 2, 3 }),
            "colección: detecta sueltas 9xx en la lista de una raza de serie");
        comprobar(LogicaLogros.ColeccionDeSerieCompleta(lista, new HashSet<int> { 1, 2, 3 }),
            "colección: las 3 de serie reclutadas y las sueltas sin reclutar -> completa (se concede)");
        comprobar(!LogicaLogros.ColeccionDeSerieCompleta(lista, new HashSet<int> { 1, 2, 944, 960 }),
            "colección: falta una de serie aunque estén las sueltas -> incompleta");
        comprobar(!LogicaLogros.ColeccionDeSerieCompleta(new[] { 944, 960 }, new HashSet<int> { 944, 960 }),
            "colección: lista solo con 9xx (vacía tras filtrar) -> no se concede (guarda del juego)");
        comprobar(!LogicaLogros.ColeccionDeSerieCompleta(Array.Empty<int>(), new HashSet<int>()) && !LogicaLogros.ColeccionDeSerieCompleta(null, new HashSet<int>()),
            "colección: lista vacía o nula -> no se concede");

        // -------- Héroes (los 9xx suman en el juego)
        // -------- Heroes (9xx count in the game)
        const int godking = 4;
        List<(int, IReadOnlyCollection<int>)> Heroes(int deSerie, params int[] nuevos)
        {
            var l = new List<(int, IReadOnlyCollection<int>)>();
            for (int i = 1; i <= deSerie; i++) l.Add((i, new List<int> { godking }));
            foreach (int n in nuevos) l.Add((n, new List<int> { godking }));
            return l;
        }
        var (t1, s1) = LogicaLogros.ContarHeroes(Heroes(16, 904), LogicaLogros.AlgunaDificultad);
        comprobar(t1 == 17 && s1 == 16 && !LogicaLogros.Bloquear(s1, t1, LogicaLogros.UmbralHeroesRoster),
            "héroes: 16 de serie + 904 -> Roster Complete se concede");
        var (t2, s2) = LogicaLogros.ContarHeroes(Heroes(15, 901), LogicaLogros.AlgunaDificultad);
        comprobar(t2 == 16 && s2 == 15 && LogicaLogros.Bloquear(s2, t2, LogicaLogros.UmbralHeroesRoster),
            "héroes: 15 de serie + 901 -> Roster Complete se bloquea");
        var (t3, s3) = LogicaLogros.ContarHeroes(Heroes(15), LogicaLogros.AlgunaDificultad);
        comprobar(!LogicaLogros.Bloquear(s3, t3, LogicaLogros.UmbralHeroesRoster),
            "héroes: 15 de serie sin nuevos -> no se bloquea (decide el juego)");
        var mezcla = new List<(int, IReadOnlyCollection<int>)> { (1, new List<int> { godking }), (2, new List<int> { 1, 2 }), (901, new List<int> { godking }), (3, new List<int>()) };
        var (tg, sg) = LogicaLogros.ContarHeroes(mezcla, LogicaLogros.ConDificultad(godking));
        var (ta, sa) = LogicaLogros.ContarHeroes(mezcla, LogicaLogros.AlgunaDificultad);
        comprobar(tg == 2 && sg == 1 && ta == 3 && sa == 2,
            "héroes: Godking cuenta solo la dificultad máxima; Roster cuenta cualquier victoria y no las listas vacías");
        var (tgb, sgb) = LogicaLogros.ContarHeroes(Heroes(15, 901, 902), LogicaLogros.ConDificultad(godking));
        comprobar(LogicaLogros.Bloquear(sgb, tgb, LogicaLogros.UmbralHeroesGodking) && LogicaLogros.UmbralHeroesGodking == 16,
            "héroes: 15 de serie + 901 + 902 en Godking -> Max Difficulty All Heroes se bloquea (umbral 16 del binario)");

        // -------- Ejército (razas, legendarias y rasgos)
        // -------- Army (races, legendaries and traits)
        const int especial = 0;
        var ejercito = new List<(int, int)> { (1, 1), (2, 2), (3, 3), (900, 40), (944, 4) };   // 3 razas de serie + 40 y una suelta de raza 4
                                                                                               // 3 stock races + 40 and one loose from race 4
        var (rt, rs) = LogicaLogros.ContarRazas(ejercito, true, especial);
        comprobar(rt == 5 && rs == 3 && LogicaLogros.Bloquear(rs, rt, LogicaLogros.UmbralCuatroFacciones),
            "ejército: 3 razas de serie + raza 40 + suelta de otra raza -> Four Unique Factions se bloquea");
        var (rt2, rs2) = LogicaLogros.ContarRazas(new List<(int, int)> { (1, 1), (2, 2), (3, 3), (4, 4), (900, 40) }, true, especial);
        comprobar(!LogicaLogros.Bloquear(rs2, rt2, LogicaLogros.UmbralCuatroFacciones),
            "ejército: 4 razas de serie (+ la 40) -> Four Unique Factions se concede");
        var (rt3, rs3) = LogicaLogros.ContarRazas(new List<(int, int)> { (5, especial), (1, 1) }, false, especial);
        var (rt4, rs4) = LogicaLogros.ContarRazas(new List<(int, int)> { (5, especial), (1, 1) }, true, especial);
        comprobar(rt3 == 1 && rs3 == 1 && rt4 == 2 && rs4 == 2, "ejército: Grand Alliance no cuenta la raza Special; Four Unique Factions sí (como el juego)");
        var (lt, ls) = LogicaLogros.ContarEscuadras(new List<(int, bool)> { (10, true), (11, true), (970, true), (12, false) });
        comprobar(lt == 3 && ls == 2 && LogicaLogros.Bloquear(ls, lt, LogicaLogros.UmbralLegendariasPorDefecto),
            "ejército: 2 legendarias de serie + 1 nueva -> Legends Assembled se bloquea");
        var (gt, gs) = LogicaLogros.ContarRasgos(new List<(int, int)> { (1, 7), (2, 8), (3, 9), (4, 10), (950, 11), (5, 0) }, 0);
        comprobar(gt == 5 && gs == 4 && LogicaLogros.Bloquear(gs, gt, LogicaLogros.UmbralRasgosPorDefecto),
            "ejército: 4 rasgos en escuadras de serie + 1 en una nueva -> Master of Arms se bloquea");
        comprobar(!LogicaLogros.Bloquear(3, 3, 8) && !LogicaLogros.Bloquear(8, 9, 8),
            "regla común: sin contenido nuevo o llegando sin él, decide el juego");

        // -------- Condición UnitName de las reglas de objetos (M1)
        // -------- UnitName condition of the gear rules (M1)
        var o721 = new Manifiesto.Objeto { Id = 721, Reglas = new List<Manifiesto.ReglaObjeto> {
            new Manifiesto.ReglaObjeto { Tipo = "stat", Stat = "MeleeAttack", Magnitud = "Flat", Valor = "4", Principal = true,
                Condicion = new Manifiesto.CondicionObjeto { FilterKind = "UnitTag", Tag = "Goblin" } },
            new Manifiesto.ReglaObjeto { Tipo = "stat", Stat = "MeleeAttack", Magnitud = "Flat", Valor = "4",
                Condicion = new Manifiesto.CondicionObjeto { FilterKind = "UnitName", UnitNames = new List<string> { "946" } } },
            new Manifiesto.ReglaObjeto { Tipo = "stat", Stat = "Leadership", Magnitud = "Flat", Valor = "8",
                Condicion = new Manifiesto.CondicionObjeto { FilterKind = "UnitName", UnitNames = new List<string>() } } } };
        string json = ReglasObjetos.Json(new[] { o721 }, 7, c => Array.Empty<int>(), id => null, out int nS, out int nA);
        comprobar(nS == 2 && nA == 0 && json.Contains("{\"filterKind\":\"UnitName\",\"unitNames\":[\"946\"]}") && json.Contains("\"tag\":\"Goblin\""),
            "reglas: UnitName con la 946 sale como la escribe el juego, junto a la de la etiqueta Goblin; sin unidades se omite");
        var mixta = new Manifiesto.CondicionObjeto { FilterKind = "UnitName", UnitNames = new List<string> { "GoblinRabble", " 946 ", "946" } };
        var o2 = new Manifiesto.Objeto { Id = 799, Reglas = new List<Manifiesto.ReglaObjeto> { new Manifiesto.ReglaObjeto { Tipo = "atributo", Atributo = "Stalwart", Condicion = mixta } } };
        string json2 = ReglasObjetos.Json(new[] { o2 }, 7, c => Array.Empty<int>(), id => null, out _, out int nA2);
        comprobar(nA2 == 1 && json2.Contains("\"unitNames\":[\"GoblinRabble\",\"946\"]"), "reglas: UnitName admite nombres del enum y números, sin espacios ni repetidos");

        // -------- Manifiesto real: el 721 con sus reglas Goblin y las de la 946
        // -------- Real manifest: 721 with its Goblin rules and those of 946
        if (manifiesto != null && manifiesto.Objetos != null && manifiesto.Objetos.Count > 0)
        {
            var m721 = manifiesto.Objetos?.Find(o => o.Id == 721);
            bool goblin = m721 != null && m721.Reglas.Count(r => r.Condicion?.FilterKind == "UnitTag" && r.Condicion.Tag == "Goblin") == 2;
            bool de946 = m721 != null && m721.Reglas.Count(r => r.Condicion?.FilterKind == "UnitName" && r.Condicion.UnitNames != null && r.Condicion.UnitNames.SequenceEqual(new[] { "946" })) == 2;
            comprobar(goblin && de946, "manifiesto: el 721 Grog de Hongo lleva 2 reglas Goblin y 2 con UnitName 946");
        }
    }
}
