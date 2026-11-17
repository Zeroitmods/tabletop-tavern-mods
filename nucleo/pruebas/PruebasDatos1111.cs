// PruebasDatos1111.cs
// Prueba 25 (01-11-2026, juego 1.11.1): arreglos de la revisión de datos (notas internas (no publicadas)), fuera del juego.
//   - Hechizo emblema de los héroes nuevos (LogicaHechizos.cs): un hechizo del enum que no está registrado (el caso de
//     ArtilleryBombardment) se cambia por el del héroe prestado o, si tampoco vale, por RallyTheBanners, con aviso; si el
//     registro no se puede consultar, se mantiene lo de antes.
//   - Cabecera de PNG e icono 2:3 oficial de 512 x 768 (LogicaIconos.MedidasPng y EsIcono2x3Oficial): con él, el núcleo no
//     mete sprite propio en la caché del juego.
//   - Con la carpeta de módulos generada (args[1]): las 60 unidades escriben los 15 rasgos de campaña a "False" (la 951 ya
//     no lleva earnTheirFreedom) y los 45 rasgos; los héroes llevan un hechizo registrado (héroe 902, IronRain); los icon.png
//     de las unidades miden 512 x 768 RGBA; nucleoMinimo = versión del núcleo (0.7.3; 0.7.4 desde el 02-11); y los nombres que chocaban con el juego ya no salen.
// Autor: ZeroitMods.
// PruebasDatos1111.cs
// Test 25 (01-11-2026, game 1.11.1): fixes from the data review (internal notes (unpublished)), outside the game.
//   - Emblem spell of the new heroes (LogicaHechizos.cs): an enum spell that is not registered (the case of
//     ArtilleryBombardment) is replaced by the borrowed hero's or, if that is not valid either, by RallyTheBanners, with a warning; if the
//     registry cannot be queried, the previous value is kept.
//   - PNG header and official 512 x 768 2:3 icon (LogicaIconos.MedidasPng and EsIcono2x3Oficial): with it, the core does not
//     put its own sprite into the game's cache.
//   - With the generated modules folder (args[1]): the 60 units write the 15 campaign traits as "False" (951 no
//     longer has earnTheirFreedom) and the 45 traits; heroes have a registered spell (hero 902, IronRain); the icon.png files
//     of the units are 512 x 768 RGBA; nucleoMinimo = core version (0.7.3; 0.7.4 since 02-11); and the names that clashed with the game no longer appear.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ZeroNucleo;

internal static class PruebasDatos1111
{
    // Los 15 rasgos de campaña de la guía oficial (README, tabla de unit_overrides.json), en su orden
    // The 15 campaign traits of the official guide (README, unit_overrides.json table), in their order
    private static readonly string[] RasgosCampana =
    {
        "uninvitedGuests", "restlessDead", "earnTheirFreedom", "masterlessBlade", "slowGrowth", "returnToTheGrove",
        "prospectors", "regeneration", "noblePurse", "garrisonDuty", "feast", "scavengers", "graveRobbers", "cryptKeepers",
        "luckyCharm",
    };

    public static void Todas(Action<bool, string> comprobar, string carpetaModulos)
    {
        PruebaHechizos(comprobar);
        PruebaCabeceraPng(comprobar);
        if (carpetaModulos != null && Directory.Exists(carpetaModulos)) PruebaModulos(comprobar, carpetaModulos);
    }

    // ---------------------------------------------------------------
    // Hechizo emblema: registrado, sustituto seguro y registro desconocido
    // Emblem spell: registered, safe substitute and unknown registry
    // ---------------------------------------------------------------
    private static void PruebaHechizos(Action<bool, string> comprobar)
    {
        // Enum y registro del juego 1.11.1 en pequeño: ArtilleryBombardment está en el enum pero no registrado
        // Enum and registry of game 1.11.1 in miniature: ArtilleryBombardment is in the enum but not registered
        var enumSpell = new HashSet<string> { "None", "RallyTheBanners", "ArtilleryBombardment", "IronRain", "HuntersMark", "Cyclone", "Fireball" };
        var registrados = new HashSet<string> { "RallyTheBanners", "IronRain", "HuntersMark", "Cyclone" };
        Func<string, bool> enEnum = n => enumSpell.Contains(n);
        Func<string, LogicaHechizos.Estado> registro = n => registrados.Contains(n) ? LogicaHechizos.Estado.Registrado : LogicaHechizos.Estado.NoRegistrado;

        string e1 = LogicaHechizos.Elegir(902, "IronRain", enEnum, registro, "HuntersMark", out string a1);
        comprobar(e1 == "IronRain" && a1 == null, "hechizos: un hechizo registrado se usa tal cual y sin aviso (Creed con IronRain)");

        string e2 = LogicaHechizos.Elegir(902, "ArtilleryBombardment", enEnum, registro, "HuntersMark", out string a2);
        comprobar(e2 == "HuntersMark" && a2 != null && a2.Contains("902") && a2.Contains("ArtilleryBombardment") && a2.Contains("HuntersMark"),
            $"hechizos: en el enum pero sin registrar (ArtilleryBombardment) pasa al del héroe prestado con aviso (sale {e2}: {a2})");

        string e3 = LogicaHechizos.Elegir(902, "ArtilleryBombardment", enEnum, registro, "Fireball", out string a3);
        comprobar(e3 == LogicaHechizos.HechizoSeguro && a3 != null && a3.Contains(LogicaHechizos.HechizoSeguro),
            $"hechizos: si el del prestado tampoco está registrado, lleva RallyTheBanners (sale {e3})");

        string e4 = LogicaHechizos.Elegir(901, "NoExiste", enEnum, registro, "HuntersMark", out string a4);
        comprobar(e4 == "HuntersMark" && a4 != null && a4.Contains("no existe"),
            $"hechizos: un nombre que no está en el enum pasa al del prestado, ahora con aviso (sale {e4})");

        string e5 = LogicaHechizos.Elegir(901, null, enEnum, registro, "HuntersMark", out string a5);
        comprobar(e5 == "HuntersMark" && a5 != null, "hechizos: sin hechizo en el manifiesto, el del prestado con aviso");

        string e6 = LogicaHechizos.Elegir(901, "None", enEnum, registro, "HuntersMark", out _);
        comprobar(e6 == "HuntersMark", "hechizos: «None» (está en el enum, sin asset) no se acepta: sale el del prestado");

        // Registro que no se puede consultar (tipo movido: lanza) o que no reconoce ni el hechizo común: lo de antes
        // Registry that cannot be queried (moved type: throws) or that does not recognize even the common spell: the previous behavior
        Func<string, LogicaHechizos.Estado> lanza = n => throw new TypeLoadException("SpellRegistry");
        string e7 = LogicaHechizos.Elegir(902, "ArtilleryBombardment", enEnum, lanza, "HuntersMark", out string a7);
        comprobar(e7 == "ArtilleryBombardment" && a7 != null && a7.Contains("no se ha podido comprobar"),
            $"hechizos: si el registro lanza, se acepta lo que esté en el enum (como antes) y se avisa (sale {e7})");
        Func<string, LogicaHechizos.Estado> vacio = n => LogicaHechizos.Estado.NoRegistrado;
        string e8 = LogicaHechizos.Elegir(903, "Cyclone", enEnum, vacio, "Dread", out string a8);
        comprobar(e8 == "Cyclone" && a8 != null, "hechizos: un registro vacío (ni RallyTheBanners) no es fiable: se mantiene el pedido con aviso");
        Func<string, LogicaHechizos.Estado> desconocido = n => LogicaHechizos.Estado.Desconocido;
        string e9 = LogicaHechizos.Elegir(904, "NoExiste", enEnum, desconocido, "HuntersMark", out _);
        comprobar(e9 == "HuntersMark", "hechizos: registro desconocido y nombre fuera del enum: el del prestado, como antes");

        bool nulo = false;
        try { LogicaHechizos.Elegir(1, "x", null, registro, "y", out _); } catch (ArgumentNullException) { nulo = true; }
        comprobar(nulo, "hechizos: sin función del enum se rechaza");
    }

    // ---------------------------------------------------------------
    // Cabecera de PNG e icono 2:3 oficial
    // PNG header and official 2:3 icon
    // ---------------------------------------------------------------
    private static byte[] Cabecera(int ancho, int alto)
    {
        var c = new byte[26];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(c, 0);
        c[16] = (byte)(ancho >> 24); c[17] = (byte)(ancho >> 16); c[18] = (byte)(ancho >> 8); c[19] = (byte)ancho;
        c[20] = (byte)(alto >> 24); c[21] = (byte)(alto >> 16); c[22] = (byte)(alto >> 8); c[23] = (byte)alto;
        c[24] = 8; c[25] = 6;                                             // 8 bits, RGBA
        return c;
    }

    private static void PruebaCabeceraPng(Action<bool, string> comprobar)
    {
        bool ok768 = LogicaIconos.MedidasPng(Cabecera(512, 768), out int w, out int h);
        comprobar(ok768 && w == 512 && h == 768 && LogicaIconos.EsIcono2x3Oficial(w, h),
            $"iconos 1.11.1: la cabecera de un PNG de 512 x 768 se lee y es el icono 2:3 oficial (sale {w} x {h})");
        bool ok256 = LogicaIconos.MedidasPng(Cabecera(256, 256), out int w2, out int h2);
        comprobar(ok256 && w2 == 256 && h2 == 256 && !LogicaIconos.EsIcono2x3Oficial(w2, h2) && LogicaIconos.EsIconoValido(w2, h2),
            "iconos 1.11.1: el icono heredado de 256 x 256 sigue valiendo pero no es el oficial (el núcleo le pone su retrato 2:3)");
        var malo = Cabecera(512, 768); malo[1] = 0;
        comprobar(!LogicaIconos.MedidasPng(malo, out _, out _) && !LogicaIconos.MedidasPng(new byte[10], out _, out _) && !LogicaIconos.MedidasPng(null, out _, out _),
            "iconos 1.11.1: una cabecera que no es de PNG, corta o nula no da medidas");
        comprobar(!LogicaIconos.EsIcono2x3Oficial(768, 512) && !LogicaIconos.EsIcono2x3Oficial(256, 384),
            "iconos 1.11.1: solo 512 x 768 cuenta como icono oficial (ni girado ni el lienzo de 256 x 384)");
    }

    // ---------------------------------------------------------------
    // Módulos generados
    // Generated modules
    // ---------------------------------------------------------------
    private static void PruebaModulos(Action<bool, string> comprobar, string modulos)
    {
        string[] contenido = { "ZeroUnidadesSueltas", "ZeroAstraMilitarum", "ZeroImperioGalactico" };

        // --- Rasgos de campaña: las 60 unidades, los 15 a "False"; y los 45 rasgos escritos
        // --- Campaign traits: the 60 units, the 15 set to "False"; and the 45 traits written
        int unidades = 0, conTodos = 0;
        var malos = new List<string>();
        string rasgo951 = null;
        foreach (string mid in contenido)
        {
            string ruta = Path.Combine(modulos, mid, "unit_overrides.json");
            if (!File.Exists(ruta)) { malos.Add(mid + ": falta unit_overrides.json"); continue; }
            using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
            foreach (var e in doc.RootElement.GetProperty("overrides").EnumerateArray())
            {
                unidades++;
                string uid = e.GetProperty("unitName").GetString();
                int bien = 0;
                foreach (string r in RasgosCampana)
                {
                    if (e.TryGetProperty(r, out var v) && v.GetString() == "False") bien++;
                    else malos.Add($"{uid}.{r}");
                }
                if (bien == RasgosCampana.Length) conTodos++;
                if (uid == "951" && e.TryGetProperty("earnTheirFreedom", out var etf)) rasgo951 = etf.GetString();
                int rasgos = e.EnumerateObject().Count(p => p.Value.ValueKind == JsonValueKind.String && (p.Value.GetString() == "True" || p.Value.GetString() == "False"));
                if (rasgos != 45) malos.Add($"{uid}: {rasgos} rasgos True/False (deben ser 45)");
            }
        }
        comprobar(unidades == 60 && conTodos == 60 && malos.Count == 0,
            $"datos 1.11.1: las 60 unidades escriben los 15 rasgos de campaña a False y 45 rasgos en total ({conTodos}/{unidades}; mal: {string.Join(", ", malos.Take(8))})");
        comprobar(rasgo951 == "False", $"datos 1.11.1: la 951 (base ThrallLevy) ya no se convierte en Huskarls: earnTheirFreedom = {rasgo951 ?? "(falta)"}");

        // --- Hechizos de los héroes: todos registrados en el juego 1.11.1 y héroe 902 con IronRain.
        // 0.7.4 (decisión de Zero 01-11): cada héroe lleva su hechizo PROPIO (id + 1000, prueba 26) y el oficial pasa a ser su
        // «planB»; lo registrado se mira ahora en el plan B (héroe 902 sigue con IronRain de respaldo)
        // --- Hero spells: all registered in game 1.11.1 and hero 902 with IronRain.
        // 0.7.4 (Zero's decision 01-11): each hero has their OWN spell (id + 1000, test 26) and the official one becomes their
        // «planB»; what is registered is now checked in plan B (hero 902 keeps IronRain as a fallback)
        string lista = Path.GetFullPath(Path.Combine(modulos, "..", "herramientas", "csharp", "volcado", "hechizos_registrados_1.11.1.txt"));
        var registrados = File.Exists(lista)
            ? new HashSet<string>(File.ReadAllLines(lista).Select(l => l.Split('#')[0].Trim()).Where(l => l.Length > 0))
            : new HashSet<string>();
        var hechizos = new Dictionary<int, string>();
        foreach (string mid in new[] { "ZeroAstraMilitarum", "ZeroImperioGalactico" })
        {
            string ruta = Path.Combine(modulos, mid, "facciones_manifiesto.json");
            if (!File.Exists(ruta)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
            foreach (var h in doc.RootElement.GetProperty("heroes").EnumerateArray())
            {
                string pedido = h.GetProperty("hechizo").GetString();
                // Hechizo oficial que acaba llevando si el propio fallara: el plan B si el pedido es propio, si no el pedido
                // Official spell it ends up carrying if its own were to fail: plan B if the requested one is its own, otherwise the requested one
                hechizos[h.GetProperty("id").GetInt32()] = LogicaHechizos.IdPropio(pedido) >= 0 && h.TryGetProperty("planB", out var pb) ? pb.GetString() : pedido;
            }
        }
        comprobar(registrados.Count == 23 && hechizos.Count == 4 && hechizos.Values.All(registrados.Contains)
                  && hechizos.TryGetValue(902, out string creed) && creed == "IronRain",
            $"datos 1.11.1: los 4 héroes llevan un hechizo oficial registrado (de plan B desde la 0.7.4; lista de {registrados.Count}) y Creed lleva IronRain: " +
            string.Join(", ", hechizos.OrderBy(x => x.Key).Select(x => $"{x.Key} {x.Value}")));

        // --- Iconos de unidad: 512 x 768 RGBA en las 60 carpetas unit_visuals\_9xx
        // --- Unit icons: 512 x 768 RGBA in the 60 unit_visuals\_9xx folders
        int iconos = 0, iconosBien = 0;
        var iconosMal = new List<string>();
        foreach (string mid in contenido)
        {
            string d = Path.Combine(modulos, mid, "unit_visuals");
            if (!Directory.Exists(d)) continue;
            foreach (string sub in Directory.GetDirectories(d, "_*"))
            {
                string icono = Path.Combine(sub, "icon.png");
                if (!File.Exists(icono)) continue;
                iconos++;
                var cab = new byte[26];
                using (var f = File.OpenRead(icono)) f.Read(cab, 0, cab.Length);
                if (LogicaIconos.MedidasPng(cab, out int w, out int h) && LogicaIconos.EsIcono2x3Oficial(w, h) && cab[25] == 6) iconosBien++;
                else iconosMal.Add($"{Path.GetFileName(sub)} {w}x{h} color {cab[25]}");
            }
        }
        comprobar(iconos == 60 && iconosBien == 60,
            $"datos 1.11.1: los icon.png de las 60 unidades miden 512 x 768 RGBA ({iconosBien}/{iconos}; mal: {string.Join(", ", iconosMal.Take(5))})");

        // --- nucleoMinimo 0.7.3 en todos los módulos
        // --- nucleoMinimo 0.7.3 in all modules
        var minimos = new List<string>();
        // 17-11-2026 (núcleo 0.7.9): los tres de contenido piden la 0.7.9 (sus modelos ya no traen los ficheros del juego y
        // solo la 0.7.9 los reconstruye); se comprueban aparte y los demás siguen con la 0.7.7
        // 17-11-2026 (core 0.7.9): the three content ones require 0.7.9 (their models no longer include the game files and
        // only 0.7.9 rebuilds them); they are checked separately and the others stay on 0.7.7
        var deContenido = new HashSet<string> { "ZeroUnidadesSueltas", "ZeroAstraMilitarum", "ZeroImperioGalactico" };
        var minimosContenido = new List<string>();
        foreach (string dir in Directory.GetDirectories(modulos, "Zero*"))
        {
            string zm = Path.Combine(dir, "zero_modulo.json");
            if (!File.Exists(zm)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(zm));
            string minimo = doc.RootElement.GetProperty("nucleoMinimo").GetString();
            if (deContenido.Contains(Path.GetFileName(dir))) minimosContenido.Add(minimo);
            else minimos.Add(minimo);
        }
        comprobar(minimosContenido.Count == 3 && minimosContenido.All(m => m == "0.7.9") && Modulos.CompararVersiones("0.7.9", Program.VersionNucleo) <= 0,
            $"datos 1.11.1: Más unidades, Astra Militarum e Imperio Galáctico piden el núcleo 0.7.9 ({string.Join(", ", minimosContenido)})");
        // 0.7.4: el mínimo sube con el núcleo (hechizos propios y munición de las híbridas); se compara con su VERSION
        // 0.7.6 (03-11-2026): 10 módulos (+ Impertérritos); 0.7.7: 11 (+ Heráldica)
        // 03-11 noche: 13 (+ Munición x2 y x5). El núcleo 0.7.8 solo cambia el propio núcleo (textos del estandarte al
        // cambiar de idioma), así que los módulos siguen pidiendo la 0.7.7 (el mínimo que necesitan), nunca más que la actual
        // 0.7.4: the minimum rises with the core (own spells and ammo for the hybrids); it is compared with its VERSION
        // 0.7.6 (03-11-2026): 10 modules (+ Impertérritos); 0.7.7: 11 (+ Heráldica)
        // 03-11 night: 13 (+ Munición x2 and x5). Core 0.7.8 only changes the core itself (banner texts when
        // changing language), so the modules still ask for 0.7.7 (the minimum they need), never more than the current one
        const string MinimoModulos = "0.7.7";
        comprobar(minimos.Count == 10 && minimos.All(m => m == MinimoModulos) && Modulos.CompararVersiones(MinimoModulos, Program.VersionNucleo) <= 0,
            $"datos 1.11.1: los otros 10 módulos piden el núcleo {MinimoModulos} (actual {Program.VersionNucleo}; {minimos.Count}: {string.Join(", ", minimos.Distinct())})");

        // --- Nombres: los que chocaban con el juego 1.11.1 ya no salen; los nuevos sí; y el daño de impacto oficial
        // --- Names: those that clashed with game 1.11.1 no longer appear; the new ones do; and the official impact damage
        var textos = new List<(string loc, string key, string text)>();
        foreach (string dir in Directory.GetDirectories(modulos, "Zero*"))
        {
            string lo = Path.Combine(dir, "localization_overrides.json");
            if (!File.Exists(lo)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(lo));
            foreach (var t in doc.RootElement.GetProperty("overrides").EnumerateArray())
                textos.Add((t.GetProperty("locale").GetString(), t.GetProperty("key").GetString(), t.GetProperty("text").GetString()));
        }
        string Texto(string loc, string key) => textos.FirstOrDefault(t => t.loc == loc && t.key == key).text;
        string[] viejos = { "Cuerno de guerra", "War Horn", "Pulso firme", "Ojo de halcón", "Enjambre kobold", "Kobold Swarm" };
        var siguen = textos.Where(t => viejos.Contains(t.text)).Select(t => $"{t.key}={t.text}").ToList();
        comprobar(siguen.Count == 0, $"datos 1.11.1: ningún nombre nuestro coincide con los del juego que chocaban (siguen: {string.Join(", ", siguen)})");
        comprobar(Texto("es", "722Name") == "Cuerno del fiordo" && Texto("en", "722Name") == "Fjord Horn"
                  && Texto("es", "971") == "Tropel kobold" && Texto("en", "971") == "Kobold Throng"
                  && Texto("es", "zeroAlcanceExtra") == "Vista de águila" && Texto("es", "zeroPrecisionExtra") == "Manos firmes",
            "datos 1.11.1: nombres nuevos de 722, 971 y los bonus de Stats a distancia");
        var impacto = textos.Where(t => t.text.Contains("ChargeImpactDamage|")).ToList();
        comprobar(impacto.Count == 3 && impacto.All(t => t.text.Contains("[ChargeImpactDamage|daño de impacto]")),
            $"datos 1.11.1: los 3 textos con daño de impacto usan el nombre oficial «daño de impacto» ({impacto.Count})");
    }
}
