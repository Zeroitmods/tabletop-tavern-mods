// LogicaInstalacion.cs
// 0.7.11 (juego 1.11.17.2): comprobación pura (System.IO, sin tipos del juego ni de Unity) de cómo están instalados los
// módulos de Zero, para el resumen del log al cargar y para el aviso en pantalla del menú principal (AvisoInstalacion.cs).
// Se puede probar fuera del juego (proyecto pruebas\, PruebasInstalacion.cs).
// LogicaInstalacion.cs
// 0.7.11 (game 1.11.17.2): pure check (System.IO, no game or Unity types) of how Zero modules are installed,
// for the log summary on load and for the on-screen warning in the main menu (AvisoInstalacion.cs).
// Can be tested outside the game (pruebas\ project, PruebasInstalacion.cs).
//
// Qué detecta (solo lo que el jugador NO ha elegido; un módulo apagado en el menú de mods es una decisión suya y no avisa):
//   - CarpetaDelJuego: módulos en <instalación del juego>\Mods. El juego solo lee la carpeta Mods de los datos del jugador
//     (%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods, ModLoadOrder.ModsRootPath).
//   - Anidada: el zip se descomprimió con una carpeta de más (Mods\Zero-Municion-1.0.1\ZeroMunicion\mod.json): el juego
//     solo mira el primer nivel y no ve el mod.json.
//   - SinModJson: una carpeta de Mods con zero_modulo.json pero sin mod.json (copia incompleta): el juego no la carga.
//   - Rechazado: un módulo activo que el núcleo no aplica (necesita un núcleo más nuevo, dependencia que falta, id repetido).
//   - Ilegible: zero_modulo.json roto.
//   - SinModulos: el núcleo está instalado y no hay ningún módulo en Mods (ni mal colocado).
// BepInEx\plugins NO es una carpeta equivocada: SincronizarModulos (OrdenMods.cs) copia de ahí a Mods.
// También: la línea de resumen «Zero - Núcleo X listo: N módulos activos (...)» y PideEnLinea, que replica cómo el juego
// lee sus opciones de arranque en línea (QuickBattleQueue/VersusNet.BootFromCommandLine, desensamblado de la build 26749277, juego 1.11.17, 18-11-2026).
// Autor: ZeroitMods.
// What it detects (only what the player did NOT choose; a module disabled in the mod menu is their decision and triggers no warning):
//   - CarpetaDelJuego: modules in <game installation>\Mods. The game only reads the Mods folder from player data
//     (%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods, ModLoadOrder.ModsRootPath).
//   - Anidada: the zip was extracted with an extra folder (Mods\Zero-Municion-1.0.1\ZeroMunicion\mod.json): the game
//     only looks at the first level and does not see the mod.json.
//   - SinModJson: a Mods folder with zero_modulo.json but no mod.json (incomplete copy): the game does not load it.
//   - Rechazado: an active module that the core does not apply (requires a newer core, missing dependency, repeated id).
//   - Ilegible: broken zero_modulo.json.
//   - SinModulos: the core is installed and there are no modules in Mods (not even misplaced ones).
// BepInEx\plugins is NOT a wrong folder: SincronizarModulos (OrdenMods.cs) copies from there to Mods.
// Also: the log summary line «Zero - Núcleo X listo: N módulos activos (...)» and PideEnLinea, which replicates how the game
// reads its online startup options (QuickBattleQueue/VersusNet.BootFromCommandLine, disassembled from build 26749277, game 1.11.17, 18-11-2026).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: tipos de problema de instalación y hallazgo
    // Section: installation problem types and finding
    // -----------------------------------------------------------------------
    public enum ProblemaInstalacion { CarpetaDelJuego, Anidada, SinModJson, Rechazado, Ilegible, SinModulos }

    /// <summary>Un problema de instalación encontrado: tipo, módulo o carpeta afectada y detalle para el log.</summary>
    /// An installation problem found: type, affected module or folder and detail for the log.
    public sealed class HallazgoInstalacion
    {
        public ProblemaInstalacion Tipo;
        public string Nombre;     // id del módulo o nombre de la carpeta
                                  // module id or folder name
        public string Detalle;    // ruta o motivo (solo para el log)
                                  // path or reason (log only)
        public override string ToString() => $"{Tipo}: {Nombre} ({Detalle})";
    }

    // -----------------------------------------------------------------------
    // Sección: comprobación y textos
    // Section: check and texts
    // -----------------------------------------------------------------------
    public static class LogicaInstalacion
    {
        /// <summary>
        /// Revisa la instalación. <paramref name="modsRoot"/>: carpeta Mods que lee el juego; <paramref name="raizJuego"/>:
        /// carpeta de instalación del juego (Paths.GameRootPath); <paramref name="encontrados"/>: módulos que vio el núcleo;
        /// <paramref name="rechazados"/>: Modulos.Resolver(...).Rechazados («id: motivo»); <paramref name="ilegibles"/>: avisos
        /// «carpeta: zero_modulo.json ...» de Modulos.Buscar. Nunca lanza: lo que no se pueda leer se salta.
        /// Checks the installation. <paramref name="modsRoot"/>: Mods folder read by the game; <paramref name="raizJuego"/>:
        /// game installation folder (Paths.GameRootPath); <paramref name="encontrados"/>: modules seen by the core;
        /// <paramref name="rechazados"/>: Modulos.Resolver(...).Rechazados («id: reason»); <paramref name="ilegibles"/>: warnings
        /// «folder: zero_modulo.json ...» from Modulos.Buscar. Never throws: what cannot be read is skipped.
        /// </summary>
        public static List<HallazgoInstalacion> Revisar(string modsRoot, string raizJuego, IEnumerable<ModuloEncontrado> encontrados,
                                                        IEnumerable<string> rechazados, IEnumerable<string> ilegibles = null,
                                                        bool lecturaFallida = false)
        {
            var r = new List<HallazgoInstalacion>();
            var vistos = (encontrados ?? Enumerable.Empty<ModuloEncontrado>()).ToList();

            // 1) Módulos en la carpeta Mods de la instalación del juego (el juego no la lee)
            // 1) Modules in the game installation's Mods folder (the game does not read it)
            try
            {
                string modsJuego = string.IsNullOrEmpty(raizJuego) ? null : Path.Combine(raizJuego, "Mods");
                if (modsJuego != null && Directory.Exists(modsJuego) && !MismaRuta(modsJuego, modsRoot))
                    foreach (string dir in ModulosBajo(modsJuego, 2))
                        r.Add(new HallazgoInstalacion { Tipo = ProblemaInstalacion.CarpetaDelJuego, Nombre = Path.GetFileName(dir), Detalle = dir });
            }
            catch { }

            // 2) y 3) Dentro de Mods: carpeta de más (anidada) o sin mod.json
            // 2) and 3) Inside Mods: extra folder (nested) or missing mod.json
            try
            {
                if (!string.IsNullOrEmpty(modsRoot) && Directory.Exists(modsRoot))
                    foreach (string dir in Directory.GetDirectories(modsRoot).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                    {
                        string nombre = Path.GetFileName(dir);
                        if (!OrdenMods.EsCarpetaDeMod(nombre)) continue;
                        bool conDecl = File.Exists(Path.Combine(dir, DeclaracionModulo.NombreFichero));
                        bool conMod = File.Exists(Path.Combine(dir, OrdenMods.ManifiestoMod));
                        if (conDecl && !conMod)
                            r.Add(new HallazgoInstalacion { Tipo = ProblemaInstalacion.SinModJson, Nombre = nombre, Detalle = dir });
                        else if (!conDecl && !conMod)
                            foreach (string dentro in ModulosBajo(dir, 2))
                                r.Add(new HallazgoInstalacion { Tipo = ProblemaInstalacion.Anidada, Nombre = Path.GetFileName(dentro), Detalle = dentro });
                    }
            }
            catch { }

            // 4) y 5) Lo que el núcleo leyó y no aplica
            // 4) and 5) What the core read but does not apply
            foreach (string x in rechazados ?? Enumerable.Empty<string>())
                r.Add(new HallazgoInstalacion { Tipo = ProblemaInstalacion.Rechazado, Nombre = AntesDeDosPuntos(x), Detalle = x });
            // Nucleo.Iniciar llama dos veces a Modulos.Buscar con la misma lista de avisos: sin repetir
            // Nucleo.Iniciar calls Modulos.Buscar twice with the same warning list: no duplicates
            foreach (string x in (ilegibles ?? Enumerable.Empty<string>()).Distinct())
                if (x != null && x.Contains(DeclaracionModulo.NombreFichero))
                    r.Add(new HallazgoInstalacion { Tipo = ProblemaInstalacion.Ilegible, Nombre = AntesDeDosPuntos(x), Detalle = x });

            // 6) Ningún módulo en Mods ni mal colocado: el núcleo solo no cambia nada (si la lectura falló, eso ya sale como
            //    error en el log y no se puede decir que no haya módulos)
            // 6) No modules in Mods or misplaced: the core alone does not change anything (if reading failed, that already appears as
            //    an error in the log and it cannot be said that there are no modules)
            if (vistos.Count == 0 && r.Count == 0 && !lecturaFallida)
                r.Add(new HallazgoInstalacion { Tipo = ProblemaInstalacion.SinModulos, Nombre = "", Detalle = modsRoot ?? "" });
            return r;
        }

        /// <summary>Carpetas con mod.json y zero_modulo.json en <paramref name="raiz"/> hasta <paramref name="niveles"/> de profundidad.</summary>
        /// Folders with mod.json and zero_modulo.json in <paramref name="raiz"/> up to <paramref name="niveles"/> levels deep.
        private static IEnumerable<string> ModulosBajo(string raiz, int niveles)
        {
            var r = new List<string>();
            void Mirar(string d, int n)
            {
                if (n <= 0) return;
                string[] hijos;
                try { hijos = Directory.GetDirectories(d); } catch { return; }
                foreach (string h in hijos.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    if (File.Exists(Path.Combine(h, DeclaracionModulo.NombreFichero)) && File.Exists(Path.Combine(h, OrdenMods.ManifiestoMod))) r.Add(h);
                    else Mirar(h, n - 1);
                }
            }
            Mirar(raiz, niveles);
            return r;
        }

        // «ZeroMunicion: necesita el núcleo ...» -> «ZeroMunicion» (también «Id (Carpeta): ...» -> «Id (Carpeta)»)
        // «ZeroMunicion: necesita el núcleo ...» -> «ZeroMunicion» (also «Id (Carpeta): ...» -> «Id (Carpeta)»)
        private static string AntesDeDosPuntos(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int i = s.IndexOf(':');
            return (i > 0 ? s.Substring(0, i) : s).Trim();
        }

        // Compara dos rutas sin distinguir mayúsculas ni la barra final (null nunca es igual)
        // Compares two paths ignoring case and trailing slash (null is never equal)
        private static bool MismaRuta(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            try { return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        /// <summary>
        /// Texto corto del aviso en pantalla (uno solo, con el problema más importante y cuántos hay), en español o inglés.
        /// null si no hay nada que avisar. <paramref name="version"/>: versión del núcleo; <paramref name="modsRoot"/>: dónde van.
        /// Short on-screen warning text (one only, with the most important problem and how many there are), in Spanish or English.
        /// null if there is nothing to warn about. <paramref name="version"/>: core version; <paramref name="modsRoot"/>: where they go.
        /// </summary>
        public static string TextoAviso(IList<HallazgoInstalacion> h, string version, string modsRoot, bool espanol)
        {
            if (h == null || h.Count == 0) return null;
            // Orden de importancia: lo que deja módulos sin cargar primero
            // Importance order: what leaves modules unloaded first
            var orden = new[] { ProblemaInstalacion.CarpetaDelJuego, ProblemaInstalacion.Anidada, ProblemaInstalacion.SinModJson,
                                ProblemaInstalacion.Rechazado, ProblemaInstalacion.Ilegible, ProblemaInstalacion.SinModulos };
            var tipo = orden.First(t => h.Any(x => x.Tipo == t));
            var de = h.Where(x => x.Tipo == tipo).Select(x => x.Nombre).Where(n => n.Length > 0).Distinct().ToList();
            string lista = string.Join(", ", de.Take(3)) + (de.Count > 3 ? "…" : "");
            int otros = h.Count(x => x.Tipo != tipo);
            string cab = $"Zero - {(espanol ? "Núcleo" : "Core")} {version}: ";
            string dest = string.IsNullOrEmpty(modsRoot) ? (espanol ? "la carpeta Mods del juego" : "the game's Mods folder") : RutaParaPantalla(modsRoot);
            string cuerpo = tipo switch
            {
                ProblemaInstalacion.CarpetaDelJuego => espanol
                    ? $"{lista} está en la carpeta del juego y el juego no la lee. Muévelo a {dest}."
                    : $"{lista} is in the game install folder, which the game does not read. Move it to {dest}.",
                ProblemaInstalacion.Anidada => espanol
                    ? $"{lista} tiene una carpeta de más (el zip se descomprimió dentro de otra). Deja la carpeta del módulo directamente en {dest}."
                    : $"{lista} is one folder too deep (the zip was extracted into another folder). Put the module folder directly in {dest}.",
                ProblemaInstalacion.SinModJson => espanol
                    ? $"{lista} está incompleto (falta mod.json). Vuelve a copiarlo entero."
                    : $"{lista} is incomplete (mod.json is missing). Copy it again in full.",
                ProblemaInstalacion.Rechazado => espanol
                    ? $"{lista} no se aplica (necesita otro núcleo u otro módulo). Detalle en BepInEx/LogOutput.log."
                    : $"{lista} is not applied (it needs another core or module). Details in BepInEx/LogOutput.log.",
                ProblemaInstalacion.Ilegible => espanol
                    ? $"{lista} tiene el zero_modulo.json roto. Vuelve a instalarlo."
                    : $"{lista} has a broken zero_modulo.json. Reinstall it.",
                _ => espanol
                    ? $"no hay ningún módulo de Zero instalado; el núcleo solo no cambia nada. Los módulos van en {dest}."
                    : $"no Zero module is installed; the core alone changes nothing. Modules go in {dest}.",
            };
            if (otros > 0) cuerpo += espanol ? $" (+{otros} avisos más en el log)" : $" (+{otros} more in the log)";
            return cab + cuerpo;
        }

        /// <summary>
        /// Ruta para el texto en pantalla: desde «AppData» si la lleva (sin el nombre del usuario) y con «/». El texto del
        /// aviso lo pinta TextMeshPro, que convierte «\n», «\t» o «\u...» escritos en el texto: una ruta de Windows con
        /// una carpeta que empiece por «n», «t» o «u» tras la barra saldría rota. Ejemplo: «AppData/LocalLow/MemoriStudios/TabletopTavern/Mods».
        /// Path for the on-screen text: from «AppData» if it contains it (without the username) and with «/». The warning
        /// text is rendered by TextMeshPro, which converts «\n», «\t» or «\u...» written in the text: a Windows path with
        /// a folder starting with «n», «t» or «u» after the slash would appear broken. Example: «AppData/LocalLow/MemoriStudios/TabletopTavern/Mods».
        /// </summary>
        public static string RutaParaPantalla(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return "";
            string r = ruta.Replace('\\', '/').TrimEnd('/');
            int i = r.IndexOf("/AppData/", StringComparison.OrdinalIgnoreCase);
            return i >= 0 ? r.Substring(i + 1) : r;
        }

        /// <summary>
        /// Estado corto de un módulo para su fila del menú de mods (va detrás de «versión - autor»; la fila es estrecha):
        /// «OK» si el núcleo lo aplicó, «NO: núcleo 0.9.0+» si pide un núcleo más nuevo, «NO: ver log» si no se aplicó por
        /// otra cosa y nada si estaba apagado al arrancar (el interruptor del juego ya lo dice). Refleja lo que leyó el núcleo
        /// AL ARRANCAR (los cambios del menú valen al reiniciar). Sin núcleo no sale nada: así se ve que falta.
        /// Short status of a module for its mod menu row (goes after «version - author»; the row is narrow):
        /// «OK» if the core applied it, «NO: núcleo 0.9.0+» if it requires a newer core, «NO: ver log» if it was not applied for
        /// some other reason, and nothing if it was off at startup (the game's toggle already says so). Reflects what the core read
        /// AT STARTUP (menu changes take effect on restart). Without the core, nothing appears: so the absence is visible.
        /// </summary>
        public static string EstadoFila(ModuloEncontrado m, ResultadoModulos real, string version, bool espanol)
        {
            if (m == null) return null;
            if (real != null && real.Aplicado(m.Id)) return "OK";
            if (!m.Activo) return null;
            string motivo = real?.Rechazados?.FirstOrDefault(x => string.Equals(AntesDeDosPuntos(x).Split(' ')[0], m.Id, StringComparison.OrdinalIgnoreCase));
            if (motivo != null && motivo.Contains("necesita el núcleo"))
            {
                int i = motivo.IndexOf("necesita el núcleo", StringComparison.Ordinal);
                string min = motivo.Substring(i + "necesita el núcleo".Length).Trim().Split(' ')[0];
                return espanol ? $"NO: núcleo {min}+" : $"NO: core {min}+";
            }
            return espanol ? "NO: ver log" : "NO: see log";
        }

        /// <summary>Línea de resumen del log: «Zero - Núcleo X listo: N módulos activos (a, b); M problemas de instalación».</summary>
        /// Log summary line: «Zero - Núcleo X listo: N módulos activos (a, b); M problemas de instalación».
        public static string LineaListo(string version, IEnumerable<string> aplicados, int problemas)
        {
            var l = (aplicados ?? Enumerable.Empty<string>()).ToList();
            return $"Zero - Núcleo {version} listo: {l.Count} módulos activos ({(l.Count == 0 ? "ninguno" : string.Join(", ", l))}); " +
                   (problemas == 0 ? "instalación correcta." : $"{problemas} problemas de instalación (ver «Instalación:» en este log).");
        }

        /// <summary>
        /// ¿Pide el arranque una entrada en línea? Replica al juego (build 26749277, juego 1.11.17): QuickBattleQueue.BootFromCommandLine solo
        /// actúa si hay «-versus-queue» y VersusNet.BootFromCommandLine solo con «-versus-host» o «-versus-join»; los dos
        /// comparan con String.Equals (exacto, distinguiendo mayúsculas). Sin esas opciones los dos métodos no hacen nada.
        /// Does the startup request an online entry? Replicates the game (build 26749277, game 1.11.17): QuickBattleQueue.BootFromCommandLine only
        /// acts if «-versus-queue» is present and VersusNet.BootFromCommandLine only with «-versus-host» or «-versus-join»; both
        /// compare with String.Equals (exact, case-sensitive). Without those options both methods do nothing.
        /// </summary>
        public static bool PideEnLinea(string[] argumentos, params string[] opciones)
        {
            if (argumentos == null || opciones == null) return false;
            foreach (string a in argumentos)
                foreach (string o in opciones)
                    if (string.Equals(a, o, StringComparison.Ordinal)) return true;
            return false;
        }

        // Opciones de arranque en línea del juego (literales del desensamblado de la build 26749277, juego 1.11.17, 18-11-2026)
        // Game online startup options (literals from the disassembly of build 26749277, game 1.11.17, 18-11-2026)
        public const string OpcionCola = "-versus-queue", OpcionHost = "-versus-host", OpcionJoin = "-versus-join";
    }
}
