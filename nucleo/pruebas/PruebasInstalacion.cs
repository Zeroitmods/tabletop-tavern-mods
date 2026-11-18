// PruebasInstalacion.cs
// Prueba 33 (18-11-2026, núcleo 0.7.11): comprobación de la instalación de los módulos (LogicaInstalacion.Revisar), texto
// del aviso en pantalla (ES/EN), estado de cada módulo para el menú de mods, línea de resumen del log y lectura de las
// opciones de arranque en línea (PideEnLinea, réplica de QuickBattleQueue/VersusNet.BootFromCommandLine de la build
// 26749277, juego 1.11.17). Todo en carpetas temporales propias que se borran al terminar.
// Autor: ZeroitMods.
// PruebasInstalacion.cs
// Test 33 (18-11-2026, core 0.7.11): check of module installation (LogicaInstalacion.Revisar), on-screen warning text
// (ES/EN), module status for the mod menu, log summary line and reading of online startup options (PideEnLinea, replica of
// QuickBattleQueue/VersusNet.BootFromCommandLine from build 26749277, game 1.11.17). All in custom temporary folders
// that are deleted when finished.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroNucleo;

internal static class PruebasInstalacion
{
    // ---------------------------------------------------------------
    // Sección: entrada
    // Section: input
    // ---------------------------------------------------------------
    public static void Todas(Action<bool, string> comprobar)
    {
        string raiz = Path.Combine(Path.GetTempPath(), "zero_prueba_instalacion_" + Guid.NewGuid().ToString("N"));
        try
        {
            PruebaRevisar(comprobar, raiz);
            PruebaTextos(comprobar);
            PruebaEstadoFila(comprobar);
            PruebaNombreMenu(comprobar);
            PruebaEnLinea(comprobar);
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    // Crea un módulo de prueba (mod.json + zero_modulo.json con su id y núcleo mínimo)
    // Creates a test module (mod.json + zero_modulo.json with its id and minimum core)
    private static string Modulo(string carpeta, string id, string nucleoMinimo = "0.7.0", bool conModJson = true)
    {
        Directory.CreateDirectory(carpeta);
        if (conModJson) File.WriteAllText(Path.Combine(carpeta, "mod.json"), "{\"displayName\":\"" + id + "\"}");
        File.WriteAllText(Path.Combine(carpeta, "zero_modulo.json"), "{\"formato\":1,\"id\":\"" + id + "\",\"nucleoMinimo\":\"" + nucleoMinimo + "\"}");
        return carpeta;
    }

    // ---------------------------------------------------------------
    // Sección: detección de problemas
    // Section: problem detection
    // ---------------------------------------------------------------
    private static void PruebaRevisar(Action<bool, string> comprobar, string raiz)
    {
        string mods = Path.Combine(raiz, "LocalLow", "Mods"), juego = Path.Combine(raiz, "Juego");
        Directory.CreateDirectory(mods); Directory.CreateDirectory(juego);

        // Sin nada: solo «SinModulos»
        // Nothing: only «SinModulos»
        var h0 = LogicaInstalacion.Revisar(mods, juego, new List<ModuloEncontrado>(), new List<string>());
        comprobar(h0.Count == 1 && h0[0].Tipo == ProblemaInstalacion.SinModulos, "Instalación: sin módulos ni mal colocados avisa «SinModulos» (una vez)");

        // Bien instalado: nada que avisar
        // Correctly installed: nothing to warn about
        Modulo(Path.Combine(mods, "ZeroMunicion"), "ZeroMunicion");
        var bien = Modulos.Buscar(mods, new[] { "ZeroMunicion" });
        var hb = LogicaInstalacion.Revisar(mods, juego, bien, Modulos.Resolver(bien, "0.7.11").Rechazados);
        comprobar(hb.Count == 0, "Instalación: un módulo bien puesto y aplicado no avisa nada");

        // Carpeta del juego, carpeta de más y sin mod.json
        // Game folder, extra folder and missing mod.json
        Modulo(Path.Combine(juego, "Mods", "ZeroStatsDistancia"), "ZeroStatsDistancia");
        Modulo(Path.Combine(mods, "Zero-Escuadras-1.0.1", "ZeroEscuadras50"), "ZeroEscuadras50");
        Modulo(Path.Combine(mods, "ZeroEconomia"), "ZeroEconomia", conModJson: false);
        Directory.CreateDirectory(Path.Combine(mods, "_ZeroRetirados", "ZeroViejo-x"));   // «_»: el juego la salta, no avisa
                                                                                          // «_»: the game skips it, no warning
        Modulo(Path.Combine(mods, "_ZeroRetirados", "ZeroViejo-x", "ZeroViejo"), "ZeroViejo");
        var h = LogicaInstalacion.Revisar(mods, juego, bien, new List<string>());
        comprobar(h.Any(x => x.Tipo == ProblemaInstalacion.CarpetaDelJuego && x.Nombre == "ZeroStatsDistancia"),
            "Instalación: módulo en <juego>\\Mods se detecta como carpeta del juego");
        comprobar(h.Any(x => x.Tipo == ProblemaInstalacion.Anidada && x.Nombre == "ZeroEscuadras50"),
            "Instalación: zip descomprimido con una carpeta de más se detecta como anidado");
        comprobar(h.Any(x => x.Tipo == ProblemaInstalacion.SinModJson && x.Nombre == "ZeroEconomia"),
            "Instalación: carpeta con zero_modulo.json y sin mod.json se detecta");
        comprobar(!h.Any(x => x.Nombre == "ZeroViejo") && !h.Any(x => x.Tipo == ProblemaInstalacion.SinModulos),
            "Instalación: lo que hay bajo «_ZeroRetirados» no avisa y, con problemas, no sale «SinModulos»");

        // La carpeta del juego no cuenta si ES la de Mods (instalación rara con los datos dentro del juego)
        // The game folder does not count if it IS the Mods folder (unusual installation with data inside the game)
        var hMisma = LogicaInstalacion.Revisar(Path.Combine(juego, "Mods"), juego, new List<ModuloEncontrado>(), new List<string>());
        comprobar(!hMisma.Any(x => x.Tipo == ProblemaInstalacion.CarpetaDelJuego), "Instalación: si Mods es la carpeta del juego, no se avisa como carpeta equivocada");

        // Núcleo viejo: rechazado con su motivo
        // Old core: rejected with its reason
        string mods2 = Path.Combine(raiz, "Mods2");
        Modulo(Path.Combine(mods2, "ZeroFuturo"), "ZeroFuturo", "9.9.9");
        var enc2 = Modulos.Buscar(mods2, new[] { "ZeroFuturo" });
        var res2 = Modulos.Resolver(enc2, "0.7.11");
        var h2 = LogicaInstalacion.Revisar(mods2, juego, enc2, res2.Rechazados);
        comprobar(h2.Count(x => x.Tipo == ProblemaInstalacion.Rechazado && x.Nombre == "ZeroFuturo") == 1,
            "Instalación: módulo que pide un núcleo más nuevo sale como rechazado (con su id)");

        // Ilegible repetido (Nucleo.Iniciar llama dos veces a Modulos.Buscar con la misma lista): una sola vez
        // Repeated unreadable entry (Nucleo.Iniciar calls Modulos.Buscar twice with the same list): only once
        string roto = "ZeroRoto: zero_modulo.json ilegible (JsonException: x); no se usa";
        var hRep = LogicaInstalacion.Revisar(mods2, juego, enc2, new List<string>(), new[] { roto, roto });
        comprobar(hRep.Count(x => x.Tipo == ProblemaInstalacion.Ilegible) == 1, "Instalación: un zero_modulo.json ilegible avisado dos veces cuenta una");
        // Lectura fallida: sin módulos vistos no se dice «no hay ningún módulo»
        // Failed read: without any modules seen, «no modules found» is not reported
        var hFallo = LogicaInstalacion.Revisar(Path.Combine(raiz, "vacia"), juego, null, null, null, true);
        comprobar(!hFallo.Any(x => x.Tipo == ProblemaInstalacion.SinModulos), "Instalación: con la lectura de módulos fallida no sale «SinModulos»");
        // Ilegible: el aviso de Modulos.Buscar entra
        // Unreadable: the Modulos.Buscar warning enters
        var h3 = LogicaInstalacion.Revisar(mods2, juego, enc2, new List<string>(), new[] { "ZeroRoto: zero_modulo.json ilegible (JsonException: x); no se usa" });
        comprobar(h3.Any(x => x.Tipo == ProblemaInstalacion.Ilegible && x.Nombre == "ZeroRoto"), "Instalación: zero_modulo.json ilegible sale con el nombre de la carpeta");

        // Rutas que no existen o null: no lanza
        // Paths that do not exist or null: does not throw
        bool lanza = false;
        try { LogicaInstalacion.Revisar(null, null, null, null, null); LogicaInstalacion.Revisar(Path.Combine(raiz, "no"), Path.Combine(raiz, "no2"), null, null); }
        catch { lanza = true; }
        comprobar(!lanza, "Instalación: rutas nulas o inexistentes no lanzan");
    }

    // ---------------------------------------------------------------
    // Sección: textos del aviso y del log
    // Section: warning and log texts
    // ---------------------------------------------------------------
    private static void PruebaTextos(Action<bool, string> comprobar)
    {
        var h = new List<HallazgoInstalacion>
        {
            new HallazgoInstalacion { Tipo = ProblemaInstalacion.Rechazado, Nombre = "ZeroFuturo", Detalle = "x" },
            new HallazgoInstalacion { Tipo = ProblemaInstalacion.CarpetaDelJuego, Nombre = "ZeroMunicion", Detalle = "y" },
        };
        string unidad = System.IO.Path.GetPathRoot(Environment.SystemDirectory) ?? "";
        string datosMods = System.IO.Path.Combine(unidad, "Datos", "Mods");
        string es = LogicaInstalacion.TextoAviso(h, "0.7.11", datosMods, true);
        string en = LogicaInstalacion.TextoAviso(h, "0.7.11", datosMods, false);
        comprobar(es != null && es.StartsWith("Zero - Núcleo 0.7.11: ZeroMunicion") && es.Contains(datosMods.Replace('\\', '/')) && es.Contains("+1"),
            "Aviso: en español, primero lo más grave (carpeta del juego), con el destino y cuántos avisos más hay");
        comprobar(en != null && en.StartsWith("Zero - Core 0.7.11: ZeroMunicion") && en.Contains("game install folder"), "Aviso: en inglés con el mismo orden");
        comprobar(LogicaInstalacion.TextoAviso(new List<HallazgoInstalacion>(), "0.7.11", "m", true) == null &&
                  LogicaInstalacion.TextoAviso(null, "0.7.11", "m", true) == null, "Aviso: sin hallazgos no hay aviso");
        var muchos = Enumerable.Range(1, 5).Select(i => new HallazgoInstalacion { Tipo = ProblemaInstalacion.Anidada, Nombre = "Zero" + i }).ToList();
        string m = LogicaInstalacion.TextoAviso(muchos, "0.7.11", "m", true);
        comprobar(m.Contains("Zero1, Zero2, Zero3…") && !m.Contains("Zero4"), "Aviso: lista como mucho 3 nombres y corta con «…»");
        comprobar(m.Length < 260, "Aviso: el texto es corto (cabe en la notificación del juego)");
        string conRuta = LogicaInstalacion.TextoAviso(h, "0.7.11", System.IO.Path.Combine(unidad, "Users", "jugador", "AppData", "LocalLow", "MemoriStudios", "TabletopTavern", "Mods"), true);
        string rech = LogicaInstalacion.TextoAviso(new List<HallazgoInstalacion> { h[0] }, "0.7.11", "m", true);
        comprobar(!conRuta.Contains("\\") && !rech.Contains("\\") && conRuta.Contains("AppData/LocalLow/MemoriStudios/TabletopTavern/Mods") && !conRuta.Contains("nadia"),
            "Aviso: sin «\\» (TextMeshPro convertiría «\\n», «\\t», «\\u»), ruta desde AppData con «/» y sin el nombre del usuario");

        string l = LogicaInstalacion.LineaListo("0.7.11", new[] { "ZeroMunicion", "ZeroEconomia" }, 0);
        comprobar(l == "Zero - Núcleo 0.7.11 listo: 2 módulos activos (ZeroMunicion, ZeroEconomia); instalación correcta.", "Log: línea de resumen con los módulos activos");
        comprobar(LogicaInstalacion.LineaListo("0.7.11", null, 3).Contains("0 módulos activos (ninguno); 3 problemas"), "Log: resumen sin módulos y con problemas");
    }

    // ---------------------------------------------------------------
    // Sección: nombre corto de la fila del menú de mods (0.7.12)
    // Section: short name of the mod menu row (0.7.12)
    // ---------------------------------------------------------------
    // Nombre corto del menú de mods por idioma (LogicaInstalacion.NombreMenu) y lectura del campo «nombreMenu»
    // Short name for the mod menu per language (LogicaInstalacion.NombreMenu) and reading of the «nombreMenu» field
    private static void PruebaNombreMenu(Action<bool, string> comprobar)
    {
        var n = new Dictionary<string, string>
        {
            ["es"] = "Zero · Más oro", ["en"] = "Zero · More Gold", ["zh"] = "Zero · 更多金币", ["zh-Hant"] = "Zero · 更多金幣", ["de"] = "",
        };
        comprobar(LogicaInstalacion.NombreMenu(n, "es") == "Zero · Más oro", "Nombre del menú: idioma exacto");
        comprobar(LogicaInstalacion.NombreMenu(n, "ES") == "Zero · Más oro", "Nombre del menú: sin distinguir mayúsculas");
        comprobar(LogicaInstalacion.NombreMenu(n, "es-ES") == "Zero · Más oro" && LogicaInstalacion.NombreMenu(n, "es_MX") == "Zero · Más oro",
                  "Nombre del menú: «es-ES» y «es_MX» van a «es»");
        comprobar(LogicaInstalacion.NombreMenu(n, "zh-Hant") == "Zero · 更多金幣" && LogicaInstalacion.NombreMenu(n, "zh-TW") == "Zero · 更多金幣",
                  "Nombre del menú: chino tradicional");
        comprobar(LogicaInstalacion.NombreMenu(n, "zh-Hans") == "Zero · 更多金币" && LogicaInstalacion.NombreMenu(n, "zh-CN") == "Zero · 更多金币",
                  "Nombre del menú: chino simplificado");
        comprobar(LogicaInstalacion.NombreMenu(n, "ja") == "Zero · More Gold", "Nombre del menú: idioma sin nombre: inglés");
        comprobar(LogicaInstalacion.NombreMenu(n, "de") == "Zero · More Gold", "Nombre del menú: nombre vacío: inglés");
        comprobar(LogicaInstalacion.NombreMenu(n, null) == "Zero · More Gold", "Nombre del menú: sin idioma: inglés");
        comprobar(LogicaInstalacion.NombreMenu(null, "es") == null && LogicaInstalacion.NombreMenu(new Dictionary<string, string>(), "es") == null,
                  "Nombre del menú: módulo sin nombres: null (se queda el de mod.json)");
        comprobar(LogicaInstalacion.NombreMenu(new Dictionary<string, string> { ["fr"] = "Zero · Plus d'or" }, "es") == null,
                  "Nombre del menú: sin el idioma ni inglés: null");
        // El campo se lee del zero_modulo.json; sin él (módulo anterior) queda null
        // The field is read from zero_modulo.json; without it (older module) it stays null
        string dir = Path.Combine(Path.GetTempPath(), "zero_prueba_nombre_menu_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            string ruta = Path.Combine(dir, DeclaracionModulo.NombreFichero);
            File.WriteAllText(ruta, "{\"formato\": 1, \"id\": \"ZeroEconomia\", \"version\": \"1.2.2\", \"nucleoMinimo\": \"0.7.7\", " +
                                    "\"nombreMenu\": {\"es\": \"Zero · Más oro\", \"en\": \"Zero · More Gold\"}}");
            var d = Modulos.LeerDeclaracion(ruta, out string motivo);
            comprobar(d != null && motivo == null && d.NombreMenu != null && d.NombreMenu.Count == 2 && d.NombreMenu["es"] == "Zero · Más oro",
                      "Nombre del menú: zero_modulo.json con «nombreMenu» se lee");
            File.WriteAllText(ruta, "{\"formato\": 1, \"id\": \"ZeroEconomia\", \"version\": \"1.2.1\", \"nucleoMinimo\": \"0.7.7\"}");
            var v = Modulos.LeerDeclaracion(ruta, out string motivo2);
            comprobar(v != null && motivo2 == null && v.NombreMenu == null, "Nombre del menú: zero_modulo.json sin el campo (módulo anterior): null");
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    // ---------------------------------------------------------------
    // Sección: estado de la fila del menú de mods
    // Section: mod menu row status
    // ---------------------------------------------------------------
    private static void PruebaEstadoFila(Action<bool, string> comprobar)
    {
        ModuloEncontrado Mod(string id, bool activo, string min = "0.7.0") =>
            new ModuloEncontrado { Declaracion = new DeclaracionModulo { Id = id, NucleoMinimo = min }, NombreCarpeta = id, Carpeta = id, Activo = activo };
        var aplicado = Mod("ZeroMunicion", true);
        var apagado = Mod("ZeroMunicion5", false);
        var futuro = Mod("ZeroFuturo", true, "9.9.9");
        var res = Modulos.Resolver(new[] { aplicado, apagado, futuro }, "0.7.11");
        comprobar(LogicaInstalacion.EstadoFila(aplicado, res, "0.7.11", true) == "OK", "Fila: módulo aplicado");
        comprobar(LogicaInstalacion.EstadoFila(apagado, res, "0.7.11", false) == null, "Fila: módulo apagado, sin texto (lo dice el interruptor)");
        comprobar(LogicaInstalacion.EstadoFila(futuro, res, "0.7.11", true) == "NO: núcleo 9.9.9+" && LogicaInstalacion.EstadoFila(futuro, res, "0.7.11", false) == "NO: core 9.9.9+",
            "Fila: módulo que pide un núcleo más nuevo, con la versión que pide");
        comprobar(LogicaInstalacion.EstadoFila(null, res, "0.7.11", true) == null, "Fila: sin módulo, sin texto");
    }

    // ---------------------------------------------------------------
    // Sección: opciones de arranque en línea
    // Section: online startup options
    // ---------------------------------------------------------------
    private static void PruebaEnLinea(Action<bool, string> comprobar)
    {
        string[] normal = { "TabletopTavern.exe" };
        string[] cola = { "x.exe", "-versus-queue", "ranked" };
        string[] host = { "x.exe", "-versus-host" };
        comprobar(!LogicaInstalacion.PideEnLinea(normal, LogicaInstalacion.OpcionCola) &&
                  !LogicaInstalacion.PideEnLinea(normal, LogicaInstalacion.OpcionHost, LogicaInstalacion.OpcionJoin),
            "En línea: un arranque normal (sin opciones) no pide ninguna entrada: no se bloquea ni se avisa");
        comprobar(LogicaInstalacion.PideEnLinea(cola, LogicaInstalacion.OpcionCola) && !LogicaInstalacion.PideEnLinea(cola, LogicaInstalacion.OpcionHost, LogicaInstalacion.OpcionJoin),
            "En línea: «-versus-queue» pide la cola y no el enlace");
        comprobar(LogicaInstalacion.PideEnLinea(host, LogicaInstalacion.OpcionHost, LogicaInstalacion.OpcionJoin) &&
                  LogicaInstalacion.PideEnLinea(new[] { "-versus-join", "steam:1" }, LogicaInstalacion.OpcionHost, LogicaInstalacion.OpcionJoin),
            "En línea: «-versus-host» y «-versus-join» piden el enlace");
        comprobar(!LogicaInstalacion.PideEnLinea(new[] { "-Versus-Queue" }, LogicaInstalacion.OpcionCola) && !LogicaInstalacion.PideEnLinea(null, LogicaInstalacion.OpcionCola),
            "En línea: comparación exacta como el juego (mayúsculas distintas no cuentan) y null no lanza");
    }
}
