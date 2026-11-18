// PruebasHechizos.cs
// Prueba 26 (02-11-2026, núcleo 0.7.4): hechizos propios de los héroes nuevos, munición de las híbridas y sorteo por rareza,
// fuera del juego (lógica pura y textos de save sintéticos con el formato real de la 1.11.1).
//   - LogicaHechizos: rango 1900-1999, Validar (base registrada, raza de serie, maná, estadísticas que el juego aplica...),
//     Elegir con plan B (propio registrado, propio sin registrar, plan B sin registrar, registro desconocido) y el recuento de
//     «Grand Grimoire» solo con los hechizos de serie.
//   - SaneadoHechizos: ida al disco sin ningún 19xx (selectedSpells, spellsCast, spellsEverCast, historial y batalla
//     personalizada), vuelta solo de los hechizos disponibles, instantánea aparte, doble pasada, firma distinta y módulo
//     apagado.
//   - SorteoObjetos: una carta solo se cambia por un objeto nuevo de su misma rareza, sin dato de rareza no se toca y la
//     última carta de un tesoro de tres queda protegida (en la tienda no).
//   - Con la carpeta de módulos generada (args[1]): los 4 hechizos propios en el manifiesto de Astra e Imperio, válidos y
//     con su héroe; textos «19xx» y «19xx_Desc» en es/en; el munición x3 sigue pidiendo las híbridas.
// Autor: ZeroitMods.
// PruebasHechizos.cs
// Test 26 (11-02-2026, core 0.7.4): the new heroes' own spells, ammo for the hybrids and rarity-based draws,
// outside the game (pure logic and synthetic save texts with the real 1.11.1 format).
//   - LogicaHechizos: range 1900-1999, Validar (registered base, default race, mana, stats the game applies...),
//     Elegir with fallback (own registered, own unregistered, unregistered fallback, unknown registry) and the count of
//     «Grand Grimoire» with default spells only.
//   - SaneadoHechizos: write to disk without any 19xx (selectedSpells, spellsCast, spellsEverCast, history and custom
//     battle), read back only the available spells, separate snapshot, double pass, different signature and module
//     turned off.
//   - SorteoObjetos: a card is only swapped for new gear of the same rarity, with no rarity data it is left alone and the
//     last card of a three-card treasure is protected (not in the shop).
//   - With the generated modules folder (args[1]): the 4 own spells in the Astra and Imperio manifest, valid and
//     with their hero; «19xx» and «19xx_Desc» texts in es/en; ammo x3 still requires the hybrids.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ZeroNucleo;

internal static class PruebasHechizos
{
    public static void Todas(Action<bool, string> comprobar, string carpetaModulos)
    {
        PruebaLogica(comprobar);
        PruebaElegir(comprobar);
        PruebaGrimorio(comprobar);
        PruebaSaneadoPartida(comprobar);
        PruebaSaneadoJugador(comprobar);
        PruebaSorteoRareza(comprobar);
        if (carpetaModulos != null && Directory.Exists(carpetaModulos)) PruebaModulos(comprobar, carpetaModulos);
    }

    // Hechizos registrados del juego en pequeño (nombres del enum Spell)
    // Game's registered spells in miniature (names from the Spell enum)
    private static readonly HashSet<string> Registrados = new HashSet<string>
    {
        "RallyTheBanners", "IronRain", "HuntersMark", "Cyclone", "Dread", "Sunder", "LesserDamageSpell",
    };

    // Definición válida de partida (la del héroe 904)
    // Valid run definition (the one for hero 904)
    private static Manifiesto.Hechizo Valido() => new Manifiesto.Hechizo
    {
        Id = 1904, Heroe = 904, Base = "Sunder", Raza = "RavenHost", Mana = 3, Enfriamiento = 10, Valor = -12, Radio = 7,
        Preparacion = 1, Duracion = 18, Equipo = "Enemy", TipoBonus = "None",
        Stats = new List<Manifiesto.StatHechizo>
        {
            new Manifiesto.StatHechizo { Stat = "MeleeDefense", Valor = -12 },
            new Manifiesto.StatHechizo { Stat = "Armor", Valor = -0.35f },
            new Manifiesto.StatHechizo { Stat = "Accuracy", Valor = -20 },
        },
        NombreEs = "Análisis táctico", NombreEn = "Tactical Analysis",
    };

    // ---------------------------------------------------------------
    // Rango y validación de la definición
    // Range and validation of the definition
    // ---------------------------------------------------------------
    private static void PruebaLogica(Action<bool, string> comprobar)
    {
        comprobar(LogicaHechizos.EsIdPropio(1900) && LogicaHechizos.EsIdPropio(1999) && !LogicaHechizos.EsIdPropio(1899)
                  && !LogicaHechizos.EsIdPropio(2000) && !LogicaHechizos.EsIdPropio(901),
            "hechizos propios: el rango es 1900-1999 (901, 1899 y 2000 fuera)");
        comprobar(LogicaHechizos.IdPropio("1901") == 1901 && LogicaHechizos.IdPropio(" 1904 ") == 1904 && LogicaHechizos.IdPropio("901") == -1
                  && LogicaHechizos.IdPropio("RallyTheBanners") == -1 && LogicaHechizos.IdPropio("-1901") == -1 && LogicaHechizos.IdPropio(null) == -1,
            "hechizos propios: IdPropio reconoce «1901» y rechaza 901, nombres del enum, negativos y null");

        Func<string, bool> reg = n => Registrados.Contains(n);
        comprobar(LogicaHechizos.Validar(Valido(), reg) == null, "validar: la definición de Thrawn vale");
        // Cada caso cambia una cosa de la válida y tiene que salir un motivo
        // Each case changes one thing from the valid one and a reason must come out
        var casos = new (string que, Action<Manifiesto.Hechizo> cambio)[]
        {
            ("id 1899", h => h.Id = 1899),
            ("base sin registrar (ArtilleryBombardment)", h => h.Base = "ArtilleryBombardment"),
            ("efecto sin registrar", h => h.EfectoDe = "Fireball"),
            ("raza 45 (número que no es de serie ni 40/41)", h => h.Raza = "45"),
            ("sin raza", h => h.Raza = null),
            ("maná 8", h => h.Mana = 8),
            ("radio 0", h => h.Radio = 0),
            ("fuerza 50", h => h.Fuerza = 50),
            ("equipo raro", h => h.Equipo = "Todos"),
            ("tipo de bonus no admitido (RallyTheBanners)", h => h.TipoBonus = "RallyTheBanners"),
            ("estadística Leadership", h => h.Stats.Add(new Manifiesto.StatHechizo { Stat = "Leadership", Valor = 5 })),
            ("5 estadísticas", h => { h.Stats.Add(new Manifiesto.StatHechizo { Stat = "Speed", Valor = -1 }); h.Stats.Add(new Manifiesto.StatHechizo { Stat = "Range", Valor = -1 }); }),
            ("sin nombre en inglés", h => h.NombreEn = ""),
        };
        foreach (var (que, cambio) in casos)
        {
            var h = Valido();
            cambio(h);
            string motivo = LogicaHechizos.Validar(h, reg);
            comprobar(motivo != null, $"validar: se rechaza con {que} ({motivo ?? "lo daba por bueno"})");
        }
        // 03-11-2026: banda propia. Raza 40/41 vale; el orden de bandas se amplía detrás, sin repetir ni tocar el de serie
        // 11-03-2026: own band. Race 40/41 is valid; the band order is extended after it, without repeating or touching the default one
        var con41 = Valido(); con41.Raza = "41";
        var con40 = Valido(); con40.Raza = "40";
        comprobar(LogicaHechizos.Validar(con41, reg) == null && LogicaHechizos.Validar(con40, reg) == null,
            "validar: la carta en la banda propia (40 o 41) vale");
        var num3 = Valido(); num3.Raza = "3";
        var num45 = Valido(); num45.Raza = "45";
        var numNeg = Valido(); numNeg.Raza = "-1";
        comprobar(LogicaHechizos.Validar(num3, reg) == null && LogicaHechizos.Validar(num45, reg) != null && LogicaHechizos.Validar(numNeg, reg) != null,
            "validar: una raza de serie en número (3) vale; 45 y -1 no");
        var serie = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var amp = LogicaHechizos.OrdenBandasAmpliado(serie, new[] { 41, 40, 41, 1, 39 });
        comprobar(amp != null && amp.SequenceEqual(serie.Concat(new[] { 40, 41 })),
            "bandas: se añaden 40 y 41 detrás del orden de serie, en orden y sin repetir (ni la 1 de serie ni la 39)");
        comprobar(LogicaHechizos.OrdenBandasAmpliado(amp, new[] { 40, 41 }) == null && LogicaHechizos.OrdenBandasAmpliado(serie, new[] { 1, 2 }) == null
                  && LogicaHechizos.OrdenBandasAmpliado(null, new[] { 40 }) == null && LogicaHechizos.OrdenBandasAmpliado(serie, null) == null,
            "bandas: si ya están, sin cartas nuevas o sin datos, no se toca nada (null)");
        var solo41 = LogicaHechizos.OrdenBandasAmpliado(serie.Concat(new[] { 40 }).ToList(), new[] { 40, 41 });
        comprobar(solo41 != null && solo41.Length == 12 && solo41[11] == 41, "bandas: si la 40 ya está, solo se añade la 41");
        comprobar(LogicaHechizos.Validar(Valido(), n => throw new InvalidOperationException("registro roto")) != null,
            "validar: si el registro lanza al consultar, la base cuenta como no registrada");
    }

    // ---------------------------------------------------------------
    // Elección del hechizo del héroe con plan B
    // Choosing the hero's spell with fallback
    // ---------------------------------------------------------------
    private static void PruebaElegir(Action<bool, string> comprobar)
    {
        var enumSpell = new HashSet<string>(Registrados) { "None", "ArtilleryBombardment" };
        Func<string, bool> enEnum = n => enumSpell.Contains(n) || LogicaHechizos.IdPropio(n) >= 0;
        // Registro con los de serie y, según el caso, el propio inyectado
        // Registry with the default ones and, depending on the case, the own one injected
        Func<string, LogicaHechizos.Estado> Registro(params string[] propios) =>
            n => Registrados.Contains(n) || propios.Contains(n) ? LogicaHechizos.Estado.Registrado : LogicaHechizos.Estado.NoRegistrado;

        string e1 = LogicaHechizos.Elegir(903, "1903", enEnum, Registro("1903"), "Dread", "Cyclone", out string a1);
        comprobar(e1 == "1903" && a1 == null, $"elegir: el propio registrado se usa sin aviso (sale {e1})");

        string e2 = LogicaHechizos.Elegir(903, "1903", enEnum, Registro(), "Dread", "Cyclone", out string a2);
        comprobar(e2 == "Dread" && a2 != null && a2.Contains("plan B") && a2.Contains("1903"),
            $"elegir: el propio sin registrar pasa a su plan B con aviso (sale {e2}: {a2})");

        string e3 = LogicaHechizos.Elegir(903, "1903", enEnum, Registro(), "ArtilleryBombardment", "Cyclone", out string a3);
        comprobar(e3 == "Cyclone" && a3 != null, $"elegir: con el plan B sin registrar, el del héroe prestado (sale {e3})");

        string e4 = LogicaHechizos.Elegir(903, "1903", enEnum, Registro(), "1904", "ArtilleryBombardment", out string a4);
        comprobar(e4 == LogicaHechizos.HechizoSeguro && a4 != null,
            $"elegir: un plan B que es otro id propio no vale; sin prestado registrado, RallyTheBanners (sale {e4})");

        // Registro que no se puede consultar: un nombre del enum se usa igual, pero un propio NO (su carta saldría vacía)
        // Registry that cannot be queried: an enum name is still used, but an own one is NOT (its card would come out empty)
        Func<string, LogicaHechizos.Estado> desconocido = n => LogicaHechizos.Estado.Desconocido;
        string e5 = LogicaHechizos.Elegir(901, "1901", enEnum, desconocido, "RallyTheBanners", "LesserMoraleSpell", out string a5);
        comprobar(e5 == "RallyTheBanners" && a5 != null, $"elegir: con el registro desconocido, el propio no se acepta y va el plan B (sale {e5})");
        string e6 = LogicaHechizos.Elegir(902, "IronRain", enEnum, desconocido, null, "HuntersMark", out string a6);
        comprobar(e6 == "IronRain" && a6 != null && a6.Contains("no se ha podido comprobar"),
            $"elegir: con el registro desconocido, un hechizo del enum se usa con aviso, como en la 0.7.3 (sale {e6})");

        // La sobrecarga de la 0.7.3 (sin plan B) sigue igual
        // The 0.7.3 overload (without fallback) stays the same
        string e7 = LogicaHechizos.Elegir(902, "ArtilleryBombardment", enEnum, Registro(), "HuntersMark", out _);
        comprobar(e7 == "HuntersMark", $"elegir: la sobrecarga sin plan B pasa al del prestado como antes (sale {e7})");
        // El plan B igual que el pedido no cuenta como sustituto
        // A fallback identical to the requested one does not count as a substitute
        string e8 = LogicaHechizos.Elegir(902, "ArtilleryBombardment", enEnum, Registro(), "ArtilleryBombardment", "IronRain", out _);
        comprobar(e8 == "IronRain", $"elegir: un plan B igual al pedido se salta (sale {e8})");
    }

    // ---------------------------------------------------------------
    // «Grand Grimoire» solo con los hechizos de serie
    // «Grand Grimoire» with default spells only
    // ---------------------------------------------------------------
    private static void PruebaGrimorio(Action<bool, string> comprobar)
    {
        var registro = new List<int> { 1, 2, 3, 1901, 1902 };
        comprobar(LogicaHechizos.GrimorioDeSerieCompleto(registro, new HashSet<int> { 1, 2, 3 }),
            "Grand Grimoire: con los 3 de serie lanzados se completa aunque falten los propios");
        comprobar(!LogicaHechizos.GrimorioDeSerieCompleto(registro, new HashSet<int> { 1, 2, 1901, 1902 }),
            "Grand Grimoire: los propios no sustituyen a uno de serie que falta");
        comprobar(!LogicaHechizos.GrimorioDeSerieCompleto(new List<int> { 1901 }, new HashSet<int> { 1901 }),
            "Grand Grimoire: un registro sin hechizos de serie no lo completa");
        comprobar(!LogicaHechizos.GrimorioDeSerieCompleto(null, new HashSet<int>()) && !LogicaHechizos.GrimorioDeSerieCompleto(registro, null),
            "Grand Grimoire: sin datos, no");
        // Ronda 2: pendientes del juego (SpellCastRecord.Remaining) sin los propios
        // Round 2: pending game entries (SpellCastRecord.Remaining) without the own ones
        comprobar(LogicaHechizos.SinPropios(new[] { 1901, 5, 1904 }).SequenceEqual(new[] { 5 }) && LogicaHechizos.SinPropios(new[] { 1902 }).Count == 0
                  && LogicaHechizos.SinPropios(null) == null,
            "Grand Grimoire (ronda 2): los pendientes del juego sin los propios (solo 1901/1904 pendientes = completo)");
        comprobar(LogicaHechizos.HayPropios(registro) && !LogicaHechizos.HayPropios(new List<int> { 1, 2, 3 }) && !LogicaHechizos.HayPropios(null),
            "Grand Grimoire: solo se recuenta si el registro lleva propios");
    }

    // ---------------------------------------------------------------
    // Saneado: save de campaña (y su instantánea)
    // Sanitizing: campaign save (and its snapshot)
    // ---------------------------------------------------------------
    // Save de campaña con el formato de JsonUtility de la 1.11.1 (claves y sangría de los saves reales del 01-11)
    // Campaign save in the 1.11.1 JsonUtility format (keys and indentation from the real saves of 01-11)
    private static string Campana(string ranura0, string runId, bool instantanea, string lanzamientos) =>
        "{\n    \"enemyWarlordHeroID\": 0,\n    \"selectedSpells\": [\n        " + ranura0 + ",\n        4,\n        0\n    ],\n" +
        "    \"runId\": \"" + runId + "\",\n    \"RunStats\": {\n        \"spellsCast\": [" + lanzamientos + "],\n" +
        "        \"startingSquadIds\": []\n    },\n    \"snapShot\": " + (instantanea ? "true" : "false") + "\n}";

    private const string LanzamientosConPropio =
        "\n            {\n                \"Spell\": 1901,\n                \"Casts\": 3\n            },\n" +
        "            {\n                \"Spell\": 4,\n                \"Casts\": 1\n            }\n        ";

    // Lee una lista de ints de un texto JSON (ruta de claves separadas por «/»)
    // Reads a list of ints from a JSON text (key path separated by «/»)
    private static List<int> Lista(string json, string ruta)
    {
        using var doc = JsonDocument.Parse(json);
        var e = doc.RootElement;
        foreach (string k in ruta.Split('/')) e = int.TryParse(k, out int i) ? e[i] : e.GetProperty(k);
        return e.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.Object ? x.GetProperty("Spell").GetInt32() : x.GetInt32()).ToList();
    }

    private static void PruebaSaneadoPartida(Action<bool, string> comprobar)
    {
        var reg = new RegistroGuardias(null);
        string original = Campana("1901", "run-A", false, LanzamientosConPropio);
        string disco = SaneadoHechizos.ParaDisco(original, reg);
        comprobar(SaneadoHechizos.ContarResiduos(original) == 2 && SaneadoHechizos.ContarResiduos(disco) == 0,
            $"saneado campaña: el original lleva 2 hechizos propios y el del disco ninguno ({SaneadoHechizos.ContarResiduos(disco)})");
        comprobar(Lista(disco, "selectedSpells").SequenceEqual(new[] { 0, 4, 0 }) && Lista(disco, "RunStats/spellsCast").SequenceEqual(new[] { 4 }),
            $"saneado campaña: ranura 1 a 0 y el lanzamiento propio fuera; el resto igual ({string.Join(",", Lista(disco, "selectedSpells"))} | {string.Join(",", Lista(disco, "RunStats/spellsCast"))})");
        comprobar(reg.HechizosDe(Program.R(original, "seleccion"), out string firma, out var pos, out _) && firma == "run-A" && pos.Count == 1 && pos[0] == 1901
                  && reg.HechizosDe(Program.R(original, "lanzamientos"), out _, out _, out var ent) && ent.ContainsKey(1901),
            "saneado campaña: el registro apunta la ranura 0 = 1901 con la firma run-A y la entrada de lanzamientos");

        // Doble pasada: el texto ya saneado vuelve igual y lo apuntado sigue
        // Double pass: the already sanitized text comes back the same and what was recorded remains
        string otra = SaneadoHechizos.ParaDisco(disco, reg);
        comprobar(otra == disco && reg.HechizosDe(Program.R(original, "seleccion"), out _, out _, out _), "saneado campaña: la segunda pasada no cambia nada ni olvida lo apuntado");

        // Vuelta con el módulo activo: vuelve el hechizo a su ranura y su lanzamiento
        // Read back with the module active: the spell returns to its slot and its cast record
        string vuelta = SaneadoHechizos.DesdeDisco(disco, reg, id => id == 1901);
        comprobar(Lista(vuelta, "selectedSpells").SequenceEqual(new[] { 1901, 4, 0 })
                  && Lista(vuelta, "RunStats/spellsCast").OrderBy(x => x).SequenceEqual(new[] { 4, 1901 }),
            $"saneado campaña: al leer con el módulo activo vuelven la ranura y el lanzamiento ({string.Join(",", Lista(vuelta, "selectedSpells"))})");
        // Módulo apagado: no vuelve nada y se queda apuntado
        // Module turned off: nothing comes back and it stays recorded
        comprobar(SaneadoHechizos.DesdeDisco(disco, reg, id => false) == disco && reg.HechizosDe(Program.R(original, "seleccion"), out _, out _, out _),
            "saneado campaña: con el módulo apagado el texto se queda con el de serie y lo apuntado sigue");

        // Instantánea: ranura aparte (la normal no se pisa)
        // Snapshot: separate slot (the normal one is not overwritten)
        string inst = Campana("1901", "run-A", true, "");
        string instDisco = SaneadoHechizos.ParaDisco(inst, reg);
        comprobar(SaneadoHechizos.ContarResiduos(instDisco) == 0 && reg.HechizosDe(Program.R(inst, "seleccion"), out _, out _, out _)
                  && reg.HechizosDe(Program.R(original, "seleccion"), out string f2, out _, out _) && f2 == "run-A",
            "saneado campaña: la instantánea va a su ranura y no pisa la del save normal");

        // Partida nueva (otra firma) sin hechizos propios: no recibe nada y (0.7.5) no borra lo apuntado de la otra partida
        // New run (different signature) without own spells: it receives nothing and (0.7.5) does not erase what was recorded for the other run
        string nueva = Campana("12", "run-B", false, "");
        string nuevaDisco = SaneadoHechizos.ParaDisco(nueva, reg);
        comprobar(nuevaDisco == nueva && !reg.HechizosDe(Program.R(nueva, "seleccion"), out _, out _, out _) && reg.HechizosDe(Program.R(original, "seleccion"), out _, out _, out _),
            "saneado campaña: otra partida sin propios no recibe nada y no olvida lo apuntado de la otra (0.7.5)");
        string nuevaCero = Campana("0", "run-C", false, "");
        comprobar(SaneadoHechizos.DesdeDisco(nuevaCero, reg, id => true) == nuevaCero,
            "saneado campaña: un save con la ranura a 0 de otra partida no recibe hechizos ajenos");

        // Ronda 2 de la revisión: lo apuntado se FUSIONA. Partida con los dos hechizos propios; vuelve solo 1901 (Imperio apagado)
        // Review round 2: what was recorded is MERGED. Run with both own spells; only 1901 comes back (Imperio turned off)
        var regF = new RegistroGuardias(null);
        string dos = "{\n    \"enemyWarlordHeroID\": 0,\n    \"selectedSpells\": [\n        1901,\n        1903,\n        0\n    ],\n    \"runId\": \"run-F\",\n" +
                     "    \"RunStats\": {\n        \"spellsCast\": [\n            {\n                \"Spell\": 1901,\n                \"Casts\": 1\n            },\n" +
                     "            {\n                \"Spell\": 1903,\n                \"Casts\": 2\n            }\n        ]\n    },\n    \"snapShot\": false\n}";
        string dosDisco = SaneadoHechizos.ParaDisco(dos, regF);
        string soloAstra = SaneadoHechizos.DesdeDisco(dosDisco, regF, id => id == 1901);
        string otraVez = SaneadoHechizos.ParaDisco(soloAstra, regF);
        string todoOtraVez = SaneadoHechizos.DesdeDisco(otraVez, regF, id => true);
        comprobar(Lista(soloAstra, "selectedSpells").SequenceEqual(new[] { 1901, 0, 0 }) && SaneadoHechizos.ContarResiduos(otraVez) == 0
                  && Lista(todoOtraVez, "selectedSpells").SequenceEqual(new[] { 1901, 1903, 0 })
                  && Lista(todoOtraVez, "RunStats/spellsCast").OrderBy(x => x).SequenceEqual(new[] { 1901, 1903 }),
            $"saneado (ronda 2): con un módulo apagado, guardar otra vez no olvida el hechizo que no volvió (ranura y lanzamiento: {string.Join(",", Lista(todoOtraVez, "selectedSpells"))})");
        // Límite documentado: si con el módulo apagado el juego pone otro hechizo en esa ranura, el propio se olvida
        // Documented limitation: if the game puts another spell in that slot while the module is off, the own one is forgotten
        string reescrito = soloAstra.Replace("1901,\n        0,", "1901,\n        4,");
        string reescritoDisco = SaneadoHechizos.ParaDisco(reescrito, regF);
        comprobar(Lista(SaneadoHechizos.DesdeDisco(reescritoDisco, regF, id => true), "selectedSpells").SequenceEqual(new[] { 1901, 4, 0 }),
            "saneado (ronda 2): si la ranura se reescribe con el módulo apagado, se queda lo que puso el juego (límite documentado)");

        // Sin registro o sin claves de hechizos: no se toca
        // No registry or no spell keys: left untouched
        comprobar(SaneadoHechizos.ParaDisco(original, null) == original && SaneadoHechizos.ParaDisco("{\"a\": 1}", reg) == "{\"a\": 1}",
            "saneado: sin registro o sin claves de hechizos el texto no se toca");
    }

    // ---------------------------------------------------------------
    // Saneado: save del jugador (lanzados alguna vez, historial y batalla personalizada)
    // Sanitizing: player's save (ever cast, history and custom battle)
    // ---------------------------------------------------------------
    private static void PruebaSaneadoJugador(Action<bool, string> comprobar)
    {
        string original =
            "{\n    \"spellsEverCast\": [\n        4,\n        1901,\n        1903\n    ],\n" +
            "    \"runHistory\": [\n" +
            "        {\n            \"runUUID\": \"uuid-a\",\n            \"heroID\": 901,\n            \"spells\": [\n                1901,\n                4,\n                0\n            ],\n" +
            "            \"spellsCast\": [\n                {\n                    \"Spell\": 1901,\n                    \"Casts\": 2\n                }\n            ],\n            \"fallen\": []\n        },\n" +
            "        {\n            \"runUUID\": \"uuid-b\",\n            \"heroID\": 1,\n            \"spells\": [\n                12,\n                0,\n                0\n            ],\n" +
            "            \"spellsCast\": [],\n            \"fallen\": []\n        }\n    ],\n" +
            "    \"customBattle\": {\n        \"playerCustomBattleSpells\": [\n            1903,\n            4\n        ]\n    }\n}";
        var reg = new RegistroGuardias(null);
        string disco = SaneadoHechizos.ParaDisco(original, reg);
        bool json = true;
        try { using var d = JsonDocument.Parse(disco); } catch { json = false; }
        comprobar(json && SaneadoHechizos.ContarResiduos(original) == 5 && SaneadoHechizos.ContarResiduos(disco) == 0,
            $"saneado jugador: JSON válido y sin ningún propio (antes {SaneadoHechizos.ContarResiduos(original)}, después {SaneadoHechizos.ContarResiduos(disco)})");
        comprobar(Lista(disco, "spellsEverCast").SequenceEqual(new[] { 4 }) && Lista(disco, "runHistory/0/spells").SequenceEqual(new[] { 0, 4, 0 })
                  && Lista(disco, "runHistory/0/spellsCast").Count == 0 && Lista(disco, "runHistory/1/spells").SequenceEqual(new[] { 12, 0, 0 })
                  && Lista(disco, "customBattle/playerCustomBattleSpells").SequenceEqual(new[] { 0, 4 }),
            "saneado jugador: lanzados alguna vez sin propios, partida A con la carta a 0 y sin lanzamientos, partida B intacta, batalla personalizada a 0");
        comprobar(reg.HechizosLanzados.SequenceEqual(new[] { 1901, 1903 }), $"saneado jugador: lanzados apuntados {string.Join(",", reg.HechizosLanzados)}");

        // Vuelta solo con el hechizo del héroe 901 disponible (Imperio apagado)
        // Return with only the spell of hero 901 available (Empire turned off)
        string vuelta = SaneadoHechizos.DesdeDisco(disco, reg, id => id == 1901);
        comprobar(Lista(vuelta, "spellsEverCast").OrderBy(x => x).SequenceEqual(new[] { 4, 1901 })
                  && Lista(vuelta, "runHistory/0/spells").SequenceEqual(new[] { 1901, 4, 0 }) && Lista(vuelta, "runHistory/0/spellsCast").SequenceEqual(new[] { 1901 })
                  && Lista(vuelta, "customBattle/playerCustomBattleSpells").SequenceEqual(new[] { 0, 4 }),
            "saneado jugador: con solo Astra activa vuelven 1901 (lanzados, historial) y 1903 se queda fuera (también en la batalla personalizada)");
        // Con todo disponible vuelve también 1903
        // With everything available, 1903 comes back too
        string todo = SaneadoHechizos.DesdeDisco(disco, reg, id => true);
        comprobar(Lista(todo, "spellsEverCast").OrderBy(x => x).SequenceEqual(new[] { 4, 1901, 1903 })
                  && Lista(todo, "customBattle/playerCustomBattleSpells").SequenceEqual(new[] { 1903, 4 }),
            "saneado jugador: con los dos módulos activos vuelve todo");
        // Y otra vez al disco: limpio de nuevo
        // And to disk again: clean once more
        comprobar(SaneadoHechizos.ContarResiduos(SaneadoHechizos.ParaDisco(todo, reg)) == 0, "saneado jugador: ida, vuelta e ida deja el disco sin propios");
    }

    // ---------------------------------------------------------------
    // Sorteo de objetos por rareza (I8)
    // Gear draw by rarity (I8)
    // ---------------------------------------------------------------
    private static void PruebaSorteoRareza(Action<bool, string> comprobar)
    {
        // Objetos nuevos: 700 y 701 comunes, 702 poco común, 704 raro
        // New gear: 700 and 701 common, 702 uncommon, 704 rare
        var objetos = new List<ObjetoSorteo>
        {
            new ObjetoSorteo { Id = 700, Afinidad = "40k", Rareza = 0 }, new ObjetoSorteo { Id = 701, Afinidad = "40k", Rareza = 0 },
            new ObjetoSorteo { Id = 702, Afinidad = "40k", Rareza = 1 }, new ObjetoSorteo { Id = 704, Afinidad = "40k", Rareza = 2 },
        };
        // Cartas de serie: 10 común, 20 poco común, 30 rara, 99 sin dato
        // Default cards: 10 common, 20 uncommon, 30 rare, 99 no data
        var rarezas = new Dictionary<int, int> { [10] = 0, [11] = 0, [20] = 1, [30] = 2 };
        int Rareza(int id) => rarezas.TryGetValue(id, out int r) ? r : -1;
        var vacio = new HashSet<int>();
        var afines = new HashSet<string> { "40k" };

        bool siempreBien = true;
        for (int semilla = 1; semilla <= 40; semilla++)
        {
            var cartas = new List<int> { 10, 20, 30 };
            SorteoObjetos.Sustituir(cartas, objetos, afines, vacio, 901, semilla, 1.0, 1.0, true, Rareza, SorteoObjetos.Protegidas(3, true));
            siempreBien &= (cartas[0] == 700 || cartas[0] == 701) && cartas[1] == 702 && cartas[2] == 704;
        }
        comprobar(siempreBien, "sorteo: en 40 semillas, la común solo pasa a 700/701, la poco común a 702 y la rara a 704 (tienda)");

        var conRara = new List<int> { 30, 11, 30 };
        SorteoObjetos.Sustituir(conRara, objetos.Where(o => o.Rareza != 2).ToList(), afines, vacio, 901, 7, 1.0, 1.0, true, Rareza, null);
        comprobar(conRara[0] == 30 && conRara[2] == 30 && (conRara[1] == 700 || conRara[1] == 701),
            "sorteo: sin objetos nuevos de su rareza la carta se queda (rara), y la común sí cambia");

        var sinDato = new List<int> { 99, 10 };
        SorteoObjetos.Sustituir(sinDato, objetos, afines, vacio, 901, 3, 1.0, 1.0, true, id => id == 99 ? throw new Exception("roto") : Rareza(id), null);
        comprobar(sinDato[0] == 99 && sinDato[1] != 10, "sorteo: una carta sin rareza (o que lanza al leerla) no se toca");

        var cofre = new List<int> { 10, 11, 20 };
        SorteoObjetos.Sustituir(cofre, objetos, afines, vacio, 901, 5, 1.0, 1.0, true, Rareza, SorteoObjetos.Protegidas(3, false));
        comprobar(cofre[2] == 20 && cofre[0] != 10 && cofre[1] != 11,
            $"sorteo: en un tesoro de tres la última carta (la que encaja con el ejército) no se toca ({string.Join(",", cofre)})");
        comprobar(SorteoObjetos.Protegidas(3, true).Length == 0 && SorteoObjetos.Protegidas(1, false).Length == 0 && SorteoObjetos.Protegidas(3, false).SequenceEqual(new[] { 2 }),
            "sorteo: solo se protege la tercera de un sorteo de tres que no es de tienda");

        // Sin función de rareza (antes de la 0.7.4) cambia como siempre
        // Without a rarity function (before 0.7.4) it swaps as usual
        var antes = new List<int> { 30 };
        SorteoObjetos.Sustituir(antes, objetos, afines, vacio, 901, 2, 1.0, 1.0, true);
        comprobar(antes[0] != 30, "sorteo: sin función de rareza se comporta como la 0.7.3");
    }

    // ---------------------------------------------------------------
    // Módulos generados: hechizos en el manifiesto y textos
    // Generated modules: spells in the manifest and texts
    // ---------------------------------------------------------------
    private static void PruebaModulos(Action<bool, string> comprobar, string modulos)
    {
        string lista = Path.GetFullPath(Path.Combine(modulos, "..", "herramientas", "csharp", "volcado", "hechizos_registrados_1.11.12.txt"));
        var registrados = File.Exists(lista)
            ? new HashSet<string>(File.ReadAllLines(lista).Select(l => l.Split('#')[0].Trim()).Where(l => l.Length > 0))
            : new HashSet<string>();
        var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var hechizos = new List<Manifiesto.Hechizo>();
        var heroes = new List<Manifiesto.Heroe>();
        var textos = new List<(string loc, string key, string text)>();
        foreach (string mid in new[] { "ZeroAstraMilitarum", "ZeroImperioGalactico" })
        {
            string ruta = Path.Combine(modulos, mid, "facciones_manifiesto.json");
            if (!File.Exists(ruta)) continue;
            var m = JsonSerializer.Deserialize<Manifiesto>(File.ReadAllText(ruta), opciones);
            hechizos.AddRange(m.Hechizos ?? new List<Manifiesto.Hechizo>());
            heroes.AddRange(m.Heroes);
            string lo = Path.Combine(modulos, mid, "localization_overrides.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(lo));
            foreach (var t in doc.RootElement.GetProperty("overrides").EnumerateArray())
                textos.Add((t.GetProperty("locale").GetString(), t.GetProperty("key").GetString(), t.GetProperty("text").GetString()));
        }
        var motivos = hechizos.Select(h => (h.Id, motivo: LogicaHechizos.Validar(h, n => registrados.Contains(n)))).Where(x => x.motivo != null).ToList();
        comprobar(hechizos.Count == 4 && hechizos.Select(h => h.Id).OrderBy(x => x).SequenceEqual(new[] { 1901, 1902, 1903, 1904 }) && motivos.Count == 0,
            $"módulos 0.7.4: los 4 hechizos propios en los manifiestos y válidos para el núcleo ({hechizos.Count}; {string.Join("; ", motivos.Select(x => $"{x.Id}: {x.motivo}"))})");
        comprobar(heroes.Count == 4 && heroes.All(h => h.Hechizo == (h.Id + 1000).ToString() && registrados.Contains(h.PlanB ?? ""))
                  && hechizos.All(x => heroes.Any(h => h.Id == x.Heroe)),
            $"módulos 0.7.4: cada héroe lleva su hechizo propio (id + 1000) y un plan B registrado: {string.Join(", ", heroes.Select(h => $"{h.Id} {h.Hechizo}/{h.PlanB}"))}");
        var porId = hechizos.ToDictionary(h => h.Id);
        bool datos = porId.Count == 4
                     && porId[1902].Equipo == "Enemy" && porId[1903].Equipo == "Enemy" && porId[1901].Equipo == "Player"
                     && porId[1901].Mana == 4 && porId[1902].Mana == 5 && porId[1903].Mana == 3 && porId[1904].Mana == 3
                     && porId.Values.All(h => h.Enfriamiento == 10)
                     && porId[1901].Raza == "40" && porId[1902].Raza == "40" && porId[1903].Raza == "41" && porId[1904].Raza == "41";
        comprobar(datos, "módulos 0.7.6: Creed y Vader solo a enemigos, Yarrick a aliados, maná 4/5/3/3, enfriamiento 10 y carta en la banda de su facción (40/41)");
        var faltan = new List<string>();
        foreach (var h in hechizos)
            foreach (string clave in new[] { h.Id.ToString(), h.Id + "_Desc" })
                foreach (string loc in new[] { "es", "en" })
                    if (!textos.Any(t => t.key == clave && t.loc == loc && !string.IsNullOrWhiteSpace(t.text))) faltan.Add($"{clave} ({loc})");
        comprobar(faltan.Count == 0, $"módulos 0.7.4: textos de nombre y descripción de los 4 hechizos en es/en (faltan: {string.Join(", ", faltan)})");

        // Munición x3: los tipos siguen incluyendo las híbridas (el núcleo 0.7.4 las aplica aunque el juego no)
        // Ammo x3: the types still include the hybrids (core 0.7.4 applies them even though the game does not)
        string zm = Path.Combine(modulos, "ZeroMunicion", "zero_modulo.json");
        bool hibridas = false;
        if (File.Exists(zm))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(zm));
            hibridas = doc.RootElement.GetProperty("efectos").GetProperty("municionJugador").GetProperty("tipos").EnumerateArray().Any(t => t.GetString() == "Hybrid");
        }
        comprobar(hibridas, "módulos 0.7.4: Munición x3 declara las híbridas");
    }
}
