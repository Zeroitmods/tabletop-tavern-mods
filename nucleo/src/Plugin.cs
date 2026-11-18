// Plugin.cs
// Punto de entrada del plugin «Zero - Núcleo» para Tabletop Tavern (BepInEx 6 IL2CPP). Hasta la 0.6.1 era el plugin
// monolítico «Zero - facción A» (todo en uno); desde la 0.7.0 el núcleo NO aplica nada por sí mismo: detecta los
// módulos instalados y activos en el menú de mods del juego (carpetas de mod oficial con un zero_modulo.json, Nucleo.cs y
// Modulos.cs) y aplica lo que declara cada uno:
//   - módulos de contenido (unidades sueltas, facción A, facción B): la «fábrica» de unidades 9xx dirigida
//     por manifiesto (UnidadGuardia.cs), las razas 40/41 y sus héroes 901-904 (FaccionesNuevas.cs, HeroesNuevos.cs,
//     PantallaHeroes.cs) y los objetos 7xx (Objetos.cs), con el saneado de saves de siempre (Saves.cs);
//   - módulos de efectos (escuadras +50/+100/+150 %, munición x3, stats a distancia): EjercitoReforzado.cs.
// Sin ningún módulo, el juego queda de serie.
// Este fichero migra la configuración y el registro de la 0.6.1, prepara los módulos y aplica los parches de Harmony uno
// a uno, de forma defensiva:
//   - si falla un parche CRÍTICO (registro de las unidades, blob de stats, visual, saneado de saves) se quitan todos
//     los parches de la fábrica y queda desactivada, con el motivo en el log; la partida sigue como si no existiera.
//   - si falla un parche OPCIONAL (reclutamiento, proyectil, estela, sonido, reglas del jugador, clasificaciones)
//     solo se avisa.
// 0.7.1 (revisión independiente del 17-10-2026): el bloqueo de las clasificaciones va en su propia instancia de Harmony y
// se aplica con cualquier módulo instalado (también solo de efectos, o si falla la fábrica); la carpeta de datos del juego
// sale de Application.persistentDataPath (con la ruta fija de respaldo); los héroes nuevos de los módulos instalados
// entran en el saneado de saves y se avisa al cargar si la partida en curso usa un héroe nuevo cuyo módulo no está activo.
// 0.7.2 (17-10-2026): pantalla de héroes ampliada con todas las facciones, Colección y Batalla personalizada con las
// facciones nuevas (InterfazColeccion.cs), objetos nuevos en la armería, héroes nuevos en el mapa de campaña y con su
// módulo apagado (CampanaHeroes.cs) y logros de Steam de serie solo con contenido de serie (LogrosFiltrados.cs, con su
// propia instancia de Harmony, como las clasificaciones).
// Autor: ZeroitMods.
// Plugin.cs
// Entry point of the «Zero - Núcleo» plugin for Tabletop Tavern (BepInEx 6 IL2CPP). Up to 0.6.1 it was the
// monolithic plugin «Zero - facción A» (all in one); since 0.7.0 the core does NOT apply anything by itself: it detects the
// modules installed and enabled in the game's mod menu (official mod folders with a zero_modulo.json, Nucleo.cs and
// Modulos.cs) and applies what each one declares:
//   - content modules (standalone units, faction A, faction B): the manifest-driven 9xx unit "factory"
//     (UnidadGuardia.cs), races 40/41 and their heroes 901-904 (FaccionesNuevas.cs, HeroesNuevos.cs,
//     PantallaHeroes.cs) and the 7xx gear (Objetos.cs), with the usual save sanitizing (Saves.cs);
//   - effects modules (squads +50/+100/+150 %, ammo x3, ranged stats): EjercitoReforzado.cs.
// With no module, the game stays vanilla.
// This file migrates the configuration and the registry from 0.6.1, prepares the modules and applies the Harmony patches one
// by one, defensively:
//   - if a CRITICAL patch fails (unit registry, stats blob, visual, save sanitizing) all the factory's
//     patches are removed and it is disabled, with the reason in the log; the run continues as if it did not exist.
//   - if an OPTIONAL patch fails (recruitment, projectile, trail, sound, player rules, rankings)
//     only a warning is issued.
// 0.7.1 (independent review of 17-10-2026): the rankings block lives in its own Harmony instance and
// is applied with any module installed (even effects-only, or if the factory fails); the game's data folder
// comes from Application.persistentDataPath (with the fixed path as fallback); the new heroes of installed modules
// are included in save sanitizing and a warning is shown on load if the current run uses a new hero whose module is not enabled.
// 0.7.2 (17-10-2026): heroes screen extended with all factions, Collection and Custom Battle with the
// new factions (InterfazColeccion.cs), new gear in the armory, new heroes on the campaign map and with their
// module turned off (CampanaHeroes.cs) and vanilla Steam achievements only with vanilla content (LogrosFiltrados.cs, with its
// own Harmony instance, like the rankings).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: plugin, configuración y aplicación de parches
    // Section: plugin, configuration and patch application
    // -----------------------------------------------------------------------
    [BepInPlugin(GUID, "Zero - Núcleo", VERSION)]
    public class Plugin : BasePlugin
    {
        public const string GUID = "zero.tabletoptavern.nucleo";
        public const string VERSION = "0.7.13";   // 0.7.13 (18-11-2026): Batalla.JineteCaballeria encendida por defecto (las 7 caballerías con jinete de los módulos 1.1.4 se reclutan; LogicaJinete.cs pasa una vez a true el false de serie de un .cfg anterior); 0.7.12 (18-11-2026): nombre corto de cada módulo en el menú de mods, en el idioma del juego (zero_modulo.json, «nombreMenu»); 0.7.11 (18-11-2026): juego 1.11.17 (build 26749277); aviso de instalación en el menú y estado de los módulos en el menú de mods; las entradas en línea por línea de comandos siguen bloqueadas pero solo avisan si el arranque las pide; 0.7.10 (17-11-2026): adaptado al juego 1.11.12 (build 26621364): raceRules en GenerateEndlessRace/GenerateRaceForMap/GetEnemyHeroForCampaign y sin aviso en pantalla en las entradas en línea por línea de comandos (colgaban el arranque); 0.7.9 (17-11-2026): los módulos ya no traen ficheros del juego (anim.bin, anchors.bin, marcadores): se reconstruyen desde la instalación del jugador (FicherosJuego.cs); 0.7.8 (03-11-2026): textos propios del estandarte al idioma nuevo al cambiarlo en Ajustes; 0.7.7 (03-11-2026): estandarte (colores de las facciones A y B, rueda en «Colores de facción», iconos de los módulos con selector, en la vista previa y en las banderas del jugador en batalla); 0.7.6 (03-11-2026): prueba de Zero (Colección 3D, historias, bandas del grimorio, Refuerzos, razas enemigas en los tres actos, módulo Impertérritos) y arreglo del cierre de la autoprueba; 0.7.5 (02-11-2026): revisión total (ranuras por partida y slot, historial saneado, logros por partida, multijugador); 0.7.4: hechizos propios, munición x3 de las híbridas y sorteo por rareza
                                                  // 0.7.13 (18-11-2026): Batalla.JineteCaballeria on by default (the 7 mounted cavalry units of the 1.1.4 modules can be recruited; LogicaJinete.cs switches the old default false of an earlier .cfg to true once); 0.7.12 (18-11-2026): short name of each module in the mod menu, in the game's language (zero_modulo.json, «nombreMenu»); 0.7.11 (18-11-2026): game 1.11.17 (build 26749277); installation warning in the menu and module status in the mod menu; online command-line entries remain blocked but only warn if the startup requests them; 0.7.10 (17-11-2026): adapted to game 1.11.12 (build 26621364): raceRules in GenerateEndlessRace/GenerateRaceForMap/GetEnemyHeroForCampaign and no on-screen warning for online command-line entries (they hung the startup); 0.7.9 (17-11-2026): modules no longer carry game files (anim.bin, anchors.bin, markers): rebuilt from the player's installation (FicherosJuego.cs); 0.7.8 (03-11-2026): custom banner texts updated to the new language when changed in Settings; 0.7.7 (03-11-2026): banner (faction A and B colors, wheel in «Colores de facción», module icons with selector, in preview and in player flags in battle); 0.7.6 (03-11-2026): Zero test (Collection 3D, stories, grimoire bands, Reinforcements, enemy races in all three acts, Impertérritos module) and fix for self-test shutdown; 0.7.5 (02-11-2026): full review (slots per run and save slot, sanitized history, achievements per run, multiplayer); 0.7.4: custom spells, x3 ammo for hybrids and rarity-based drop roll
        // Versión del juego (Application.version) con la que se probó el núcleo. Fuente: PlayerSettings de
        // TabletopTavern_Data\globalgamemanagers del juego instalado, leída el 18-10-2026 (Unity 6000.3.23f1; build de
        // Steam 26006870, solo como dato). Si el juego arranca con otra versión, Load lo avisa en el log (ComprobarVersionJuego).
        // Al probar el núcleo con una versión nueva del juego, actualizar aquí.
        // Game version (Application.version) the core was tested with. Source: PlayerSettings in
        // TabletopTavern_Data\globalgamemanagers of the installed game, read on 18-10-2026 (Unity 6000.3.23f1; Steam
        // build 26006870, for reference only). If the game starts with another version, Load warns about it in the log (ComprobarVersionJuego).
        // When testing the core with a new game version, update here.
        public const string VersionJuegoProbada = "1.11.17.2";   // 18-11-2026: núcleo 0.7.11 en la 1.11.17.2 (build 26749277): arranques automáticos hasta el menú, autoprueba 64/64 con los módulos 1.1.1 (prueba 2) , 62/65 en la instalación limpia forzada con un módulo de prueba que pide el núcleo 9.9.9 (prueba 3a: los 3 fallos son los esperados) y 64/64 con los módulos 1.1.2 (prueba 3b: los 60 modelos exactos). Antes: 1.11.12 (17-11-2026, 0.7.10, autoprueba 55/55) y 1.11.1 (03-11-2026)
                                                                 // 18-11-2026: core 0.7.11 on 1.11.17.2 (build 26749277): automatic startups to the menu, self-test 64/64 with modules 1.1.1 (test 2) , 62/65 on the forced clean install with a test module requiring core 9.9.9 (test 3a: the 3 failures are expected) and 64/64 with modules 1.1.2 (test 3b: 60 exact models). Before: 1.11.12 (17-11-2026, 0.7.10, self-test 55/55) and 1.11.1 (03-11-2026)
        // GUID y nombres de la versión monolítica (0.6.1), para migrar su configuración y su registro
        // GUID and names of the monolithic version (0.6.1), to migrate its configuration and its registry
        private const string GuidViejo = "zero.tabletoptavern.guardiaimperial";
        private const string PrefijoViejo = "ZeroGuardiaImperial";
        internal const string Prefijo = "ZeroNucleo";

        // Log compartido por todos los parches (prefijo [ZeroNucleo] para buscarlo fácil)
        // Log shared by all the patches (prefix [ZeroNucleo] to find it easily)
        internal static ManualLogSource Registro;

        // Opciones de BepInEx\config\zero.tabletoptavern.nucleo.cfg
        // BepInEx\config\zero.tabletoptavern.nucleo.cfg options
        internal static ConfigEntry<bool> CfgActivado;
        internal static ConfigEntry<float> CfgProbabilidad;
        internal static ConfigEntry<bool> CfgSiempreEnOfertas;
        internal static ConfigEntry<bool> CfgEstelaRoja;
        internal static ConfigEntry<bool> CfgModeloPropio;
        internal static ConfigEntry<string> CfgModeloBaseEn;
        internal static ConfigEntry<bool> CfgModeloHeroes;     // F5: modelo 3D propio de los héroes 901-904 (ModeloHeroes.cs)
                                                               // F5: custom 3D model for heroes 901-904 (ModeloHeroes.cs)
        internal static ConfigEntry<bool> CfgSonidoPropio;
        internal static ConfigEntry<bool> CfgImpactoPropio;
        internal static ConfigEntry<float> CfgGananciaDb;
        internal static ConfigEntry<int> CfgRevisionSonido;    // 18-10-2026: migración única de SonidoPropio a true
                                                               // 18-10-2026: one-time migration of SonidoPropio to true
        internal static ConfigEntry<bool> CfgBloquearClasificaciones;
        // 0.7.2: bloqueo de la telemetría del juego con módulos aplicados (Online.BloquearTelemetria)
        // 0.7.2: blocking of the game's telemetry with modules applied (Online.BloquearTelemetria)
        internal static ConfigEntry<bool> CfgBloquearTelemetria;
        // 0.7.4: sin partidas en línea (cooperativo y versus) con módulos aplicados (Multijugador.Bloquear)
        // 0.7.4: no online runs (co-op and versus) with modules applied (Multijugador.Bloquear)
        internal static ConfigEntry<bool> CfgBloquearMultijugador;
        internal static ConfigEntry<bool> CfgTrazas;
        internal static ConfigEntry<bool> CfgSinSonido;   // 0.7.11: solo para pruebas (Silencio.cs)
                                                          // 0.7.11: for tests only (Silencio.cs)
        internal static ConfigEntry<bool> CfgProbarMenuMods;   // 0.7.11: solo para pruebas (DiagnosticoMenuMods.cs)
                                                               // 0.7.11: for tests only (DiagnosticoMenuMods.cs)
        internal static ConfigEntry<bool> CfgAutoPrueba;
        internal static ConfigEntry<bool> CfgRespaldo;
        internal static ConfigEntry<string> CfgOmitir;
        internal static ConfigEntry<bool> CfgJineteCaballeria;
        internal static ConfigEntry<bool> CfgJineteRevisado;   // 0.7.13: marcador de la migración del jinete (LogicaJinete.cs)
                                                               // 0.7.13: marker of the rider migration (LogicaJinete.cs)
        internal static ConfigEntry<bool> CfgProyectilPorUnidad;
        internal static ConfigEntry<bool> CfgGuardiaEnAstra;
        internal static ConfigEntry<bool> CfgPantallaHeroes;
        internal static ConfigEntry<bool> CfgProbarPantalla;
        internal static ConfigEntry<float> CfgObjetosProbAfin;
        internal static ConfigEntry<float> CfgObjetosProbGeneral;
        internal static ConfigEntry<bool> CfgObjetosModoPrueba;
        // Interfaz (InterfazColeccion.cs): Colección y Batalla personalizada con facciones nuevas y desplazamiento
        // Interface (InterfazColeccion.cs): Collection and Custom Battle with new factions and scrolling
        internal static ConfigEntry<bool> CfgInterfazColeccion;
        internal static ConfigEntry<bool> CfgInterfazBatalla;
        // 0.7.2: logros de Steam de serie solo con unidades y héroes de serie (LogrosFiltrados.cs)
        // 0.7.2: stock Steam achievements only with stock units and heroes (LogrosFiltrados.cs)
        internal static ConfigEntry<bool> CfgLogrosFiltrar;

        // Estado global: true solo si todos los parches críticos se aplicaron
        // Global state: true only if all critical patches were applied
        internal static bool Operativo;

        /// <summary>Carpeta de datos del juego (saves y Mods): Application.persistentDataPath o, si falla, la ruta fija.</summary>
        /// Game data folder (saves and Mods): Application.persistentDataPath or, if that fails, the fixed path.
        internal static string CarpetaDatosJuego { get; private set; } = RutaDatosFija;
        private static string RutaDatosFija => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "MemoriStudios", "TabletopTavern");

        private Harmony _harmony;
        // Instancia aparte para «Ejército reforzado»: no se quita si fallan las firmas críticas de la fábrica
        // Separate instance for «Ejército reforzado»: it is not removed if the factory's critical signatures fail
        private Harmony _harmonyReforzado;
        // Instancia aparte para los objetos nuevos (0.6.0): si falla una firma suya se quitan solo los objetos
        // Separate instance for the new gear (0.6.0): if one of its signatures fails, only the gear is removed
        private Harmony _harmonyObjetos;
        // 0.7.1: instancia aparte para el bloqueo de las clasificaciones (no se quita nunca con las demás)
        // 0.7.1: separate instance for the leaderboard block (never removed along with the others)
        private Harmony _harmonyClasificaciones;
        // Instancia aparte para los héroes nuevos fuera de su pantalla (CampanaHeroes.cs: figura del mapa de campaña y
        // assets de héroe sin entrada); se aplica con cualquier módulo instalado y no se quita con las demás
        // Separate instance for new heroes outside their screen (CampanaHeroes.cs: campaign map figure and
        // hero assets without an entry); applied with any module installed and not removed along with the others
        private Harmony _harmonyCampana;
        // 0.7.2: instancia aparte para el filtrado de los logros de Steam; se aplica con cualquier módulo instalado y no se
        // quita con las demás (un save puede traer héroes o unidades 9xx aunque hoy su módulo esté apagado)
        // 0.7.2: separate instance for filtering Steam achievements; applied with any module installed and not
        // removed along with the others (a save may contain 9xx heroes or units even if their module is turned off today)
        private Harmony _harmonyLogros;
        private readonly List<string> _fallosCriticos = new List<string>();
        private readonly List<string> _fallosOpcionales = new List<string>();
        // Parches omitidos por Diagnostico.OmitirParches (nombre del parche): Parchear los cuenta como 1, pero no están puestos
        // Patches skipped by Diagnostico.OmitirParches (patch name): Parchear counts them as 1, but they are not applied
        private readonly HashSet<string> _omitidos = new HashSet<string>();
        // 0.7.5: nombres de método omitidos (para que la autoprueba 34 los distinga de un parche que no engancha)
        // 0.7.5: method names omitted (so that self-test 34 can tell them apart from a patch that doesn't hook)
        internal static readonly HashSet<string> MetodosOmitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _aplicados;

        // ---------------------------------------------------------------
        // Carga del plugin
        // Plugin loading
        // ---------------------------------------------------------------
        public override void Load()
        {
            Registro = Log;
            // Juego 1.11.1 (revisión del 01-11-2026): red final. Todo lo que puede depender del juego va en fases dentro de
            // CargarInterno; si aun así algo no previsto lanza, se apunta en el log y BepInEx no descarta el plugin entero (lo
            // que ya se aplicó se queda puesto: cada pieza con instancia propia ya se retira sola si falla a medias)
            // Game 1.11.1 (review of 01-11-2026): final safety net. Everything that may depend on the game goes in phases inside
            // CargarInterno; if something unexpected still throws, it is noted in the log and BepInEx does not discard the whole plugin (what
            // was already applied stays in place: each piece with its own instance already removes itself if it fails halfway)
            try { CargarInterno(); }
            catch (Exception e)
            {
                try { _fallosCriticos.Add("carga interrumpida: " + LogicaCarga.Describir(e)); } catch { }
                Error("Parche crítico FALLIDO (carga del núcleo interrumpida por un error no previsto: " + LogicaCarga.Describir(e) +
                      "). Lo que ya estaba aplicado se queda; el resto del núcleo no se carga en esta sesión.");
            }
        }

        // ¿Hay contenido de módulos aplicable? (false si Nucleo.Aplicar falló o no hay datos; nunca lanza)
        // Is there applicable module content? (false if Nucleo.Aplicar failed or there is no data; never throws)
        private bool _contenidoAplicado;
        private bool HayContenido()
        {
            try { return _contenidoAplicado && Nucleo.Real != null && Nucleo.Real.HayContenido; }
            catch { return false; }
        }

        // Cuerpo de la carga (antes era Load). Juego 1.11.1: separado para que Load lleve la red final de arriba
        // Body of the loading (it used to be Load). Game 1.11.1: split so that Load carries the final safety net above
        private void CargarInterno()
        {
            // 0.7.0: la configuración de la 0.6.1 (otro GUID) se copia una vez al fichero nuevo antes de leerla
            // 0.7.0: the 0.6.1 configuration (another GUID) is copied once to the new file before reading it
            MigrarConfiguracion();
            LeerConfiguracion();
            // 0.7.11: silencio de las pruebas, lo primero (antes de que el juego cargue el menú y su música)
            // 0.7.11: test silence, first thing (before the game loads the menu and its music)
            if (CfgSinSonido != null && CfgSinSonido.Value && Silencio.Aplicar())
            {
                try
                {
                    Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<VigiaSilencio>();
                    AddComponent<VigiaSilencio>();
                    Info("Diagnostico.SinSonido = true: juego sin sonido (volumen global de Unity a 0).");
                }
                catch (Exception e) { Aviso("Diagnostico.SinSonido: " + e.Message); }
            }
            if (!CfgActivado.Value)
            {
                Info("Desactivado en la configuración (General.Activado = false). No se aplica nada.");
                return;
            }

            // Revisión externa del 18-10: aviso si el juego no es la versión con la que se probó el núcleo (va antes de mirar los
            // módulos para que salga también sin ninguno instalado). Solo avisa; no desactiva nada
            // External review of 18-10: warning if the game is not the version the core was tested with (it goes before checking the
            // modules so it also shows up with none installed). It only warns; it disables nothing
            ComprobarVersionJuego();

            // Carpeta de datos del juego: la de Unity (la misma que usa el juego para saves y Mods) o, si no se puede leer
            // todavía, la ruta fija de siempre (0.7.1)
            // Game data folder: Unity's (the same one the game uses for saves and Mods) or, if it cannot be read
            // yet, the usual fixed path (0.7.1)
            CarpetaDatosJuego = CalcularCarpetaDatos();
            // 0.7.5 (hallazgo 5): la partida en curso es la del slot de campaña activo (principal, diaria, cooperativo...)
            // 0.7.5 (finding 5): the current run is the one in the active campaign slot (main, daily, co-op...)
            HiloPrincipal = Environment.CurrentManagedThreadId;
            Saves.SlotActivo = LeerSlotActivo;
            string datosJuego = CarpetaDatosJuego;
            string copias = Path.Combine(Paths.ConfigPath, Prefijo + "_copias");

            // Módulos: sincroniza los del gestor, apunta los desactivados por defecto y suma los activos.
            // Juego 1.11.1 (revisión): en su fase; si falla, se sigue como sin módulos (solo logros y bloqueo online si queda
            // el registro), sin fábrica
            // Modules: syncs the manager's ones, notes the ones disabled by default and adds the active ones.
            // Game 1.11.1 (review): in its own phase; if it fails, continues as with no modules (only achievements and online block if the
            // registry remains), without factory
            bool modulosLeidos = Fase("Módulos instalados (Nucleo.Iniciar)", () => Nucleo.Iniciar(datosJuego, Paths.PluginPath, copias));
            // 0.7.11: instalación de los módulos (carpeta equivocada, carpeta de más, núcleo viejo...): resumen en el log y aviso
            // en pantalla cuando el menú principal ya está montado. Va ANTES del retorno de «no hay ningún módulo», que es
            // justo el caso de los módulos mal colocados, y con su propia instancia de Harmony
            // 0.7.11: module installation (wrong folder, extra folder, old core...): log summary and on-screen warning
            // when the main menu is fully set up. Runs BEFORE the «no modules found» early return, which is
            // precisely the case of misplaced modules, and with its own Harmony instance
            Fase("Aviso de instalación", () => AvisoInstalacion.Preparar(Nucleo.AvisosLectura));
            Fase("Aviso de instalación (parches y vigilante)", AplicarParchesInstalacion);
            int encontrados = 0;
            if (modulosLeidos) { try { encontrados = Nucleo.Encontrados?.Count ?? 0; } catch { encontrados = 0; } }
            if (encontrados == 0)
            {
                Info($"Núcleo {VERSION}: no hay ningún módulo instalado en {Nucleo.CarpetaMods}. No se aplica ningún parche de contenido (juego de serie).");
                // 0.7.2 (revisión): si alguna vez hubo módulos (queda el registro de saves), los logros de serie se siguen
                // filtrando: un save de entonces puede traer unidades o victorias de héroes 9xx que el juego contaría
                // 0.7.2 (review): if there were ever modules (the saves registry remains), vanilla achievements are still
                // filtered: a save from back then may carry units or victories of 9xx heroes that the game would count
                if (File.Exists(Path.Combine(Paths.ConfigPath, Prefijo + "_registro.json")) || File.Exists(Path.Combine(Paths.ConfigPath, PrefijoViejo + "_registro.json")))
                {
                    // Juego 1.11.1: cada bloque de parches va en su fase (un fallo no impide los demás)
                    // Game 1.11.1: each patch block goes in its own phase (one failure does not prevent the others)
                    Fase("Logros de Steam filtrados", AplicarParchesLogros);
                    // Revisión del 18-10 (M-2): también se carga el registro y se pone el bloqueo de clasificaciones y
                    // telemetría, que en esta sesión solo bloquea las partidas apuntadas como jugadas con módulos
                    // Review of 18-10 (M-2): the registry is also loaded and the leaderboard and telemetry block is set,
                    // which in this session only blocks runs recorded as played with modules
                    string registroSinModulos = Path.Combine(Paths.ConfigPath, Prefijo + "_registro.json");
                    MigrarRegistro(registroSinModulos, Path.Combine(Paths.ConfigPath, PrefijoViejo + "_registro.json"));
                    Fase("Registro de saves (Saves.Iniciar)", () => Saves.Iniciar(registroSinModulos, copias));
                    Fase("Clasificaciones y telemetría", AplicarParcheClasificaciones);
                }
                AvisarHeroeDeLaPartida(datosJuego, false);
                return;
            }

            // Registro de GUID de escuadras (saves), junto a la configuración de BepInEx; se migra el de la 0.6.1
            // Squad GUID registry (saves), next to the BepInEx configuration; the one from 0.6.1 is migrated
            string registro = Path.Combine(Paths.ConfigPath, Prefijo + "_registro.json");
            MigrarRegistro(registro, Path.Combine(Paths.ConfigPath, PrefijoViejo + "_registro.json"));
            // Juego 1.11.1 (revisión): sin el registro de saves no hay saneado seguro de las 9xx, así que si falla no se pone la
            // fábrica (se trata como «sin contenido»)
            // Game 1.11.1 (review): without the saves registry there is no safe sanitizing of the 9xx ids, so if it fails the
            // factory is not applied (it is treated as "no content")
            bool savesListos = Fase("Registro de saves (Saves.Iniciar)", () => Saves.Iniciar(registro, copias));

            // Contenido y efectos de los módulos activos (manifiesto fusionado, razas, héroes, objetos y efectos).
            // Juego 1.11.1 (revisión): en su fase; si falla, «sin contenido» (no se aplica la fábrica)
            // Content and effects of the active modules (merged manifest, races, heroes, gear and effects).
            // Game 1.11.1 (review): in its own phase; if it fails, "no content" (the factory is not applied)
            _contenidoAplicado = Fase("Contenido de los módulos (Nucleo.Aplicar)", () => Nucleo.Aplicar(Nucleo.Real)) && savesListos;
            if (!_contenidoAplicado) Aviso("Sin contenido de módulos en esta sesión (falló Nucleo.Aplicar o el registro de saves): no se aplica la fábrica de unidades.");
            // 0.7.1: héroes nuevos de TODOS los módulos instalados para el saneado de saves (los de módulos apagados se
            // cargan como su héroe prestado); solo se restauran los activos. Juego 1.11.1: en su fase
            // 0.7.1: new heroes from ALL installed modules for save sanitizing (those from disabled modules are
            // loaded as their borrowed hero); only the active ones are restored. Game 1.11.1: in its phase
            Fase("Tabla de héroes nuevos para el saneado", () =>
            {
                var heroesInstalados = Nucleo.HeroesInstalados();
                Saves.TablaHeroes = heroesInstalados.Count == 0 ? null : new TablaHeroes(heroesInstalados) { Disponible = FaccionesNuevas.EsHeroeNuevo };
                // 0.7.5 (ronda 2 de la revisión): razas de todos los módulos instalados, para sanear el historial aunque su
                // facción esté apagada (la tabla de razas activas solo existe con las facciones nuevas activas)
                // 0.7.5 (review round 2): races from all installed modules, to sanitize the history even if their
                // faction is off (the active races table only exists when the new factions are active)
                var razasInstaladas = Nucleo.RazasInstaladas();
                var sustitutas = new Dictionary<int, int>();
                foreach (var kv in razasInstaladas) sustitutas[kv.Key] = (int)FaccionesNuevas.RazaSerie(kv.Value, Race.IronLegion);
                Saves.TablaRazasInstaladas = sustitutas.Count == 0 ? null : new TablaRazas(sustitutas);
                // 0.7.4: hechizos propios que se devuelven al leer un save. Revisión (ronda 2): solo los que están REGISTRADOS en el
                // juego en ese momento (definido por un módulo activo no basta: si el clon falló, el save no debe recibir un id
                // que el juego no conoce; lo apuntado se queda en el registro y vuelve cuando sí esté)
                // 0.7.5 (revisión total, hallazgo 30): si el save se lee antes de que el hechizo esté en el registro del juego,
                // se intenta ponerlo. Ronda 2 de la revisión: SIN forzar la carga del registro desde la lectura (solo si los
                // héroes ya se dieron de alta y el registro ya está cargado, Hechizos.RegistradoParaLectura); si no se puede,
                // false y lo apuntado se queda en el registro aparte para la siguiente lectura. Límite que queda: si el juego
                // lee el save antes de eso y SpellLoadout.Sanitize rellena la ranura, ese 19xx se olvida (SaneadoHechizos.cs)
                // 0.7.4: custom spells that are returned when reading a save. Review (round 2): only those REGISTERED in the
                // game at that moment (being defined by an active module is not enough: if the clone failed, the save must not receive an id
                // the game does not know; what was noted stays in the registry and comes back when it is)
                // 0.7.5 (full review, finding 30): if the save is read before the spell is in the game's registry,
                // an attempt is made to add it. Round 2 of the review: WITHOUT forcing the registry load from the read (only if the
                // heroes have already been registered and the registry is already loaded, Hechizos.RegistradoParaLectura); if not possible,
                // false and what was noted stays in the separate registry for the next read. Remaining limitation: if the game
                // reads the save before that and SpellLoadout.Sanitize fills the slot, that 19xx is forgotten (SaneadoHechizos.cs)
                Saves.HechizoDisponible = id => Hechizos.Definidos.ContainsKey(id) && Hechizos.RegistradoParaLectura(id);
            });

            // Juego 1.11.1: desde aquí cada bloque de parches va en su fase (LogicaCarga.Fase): si el juego quita o mueve un tipo
            // o un método, falla ese bloque (o solo ese parche, con los tipos diferidos de Parchear) y lo demás sigue cargando.
            // 0.7.1: clasificaciones online, con cualquier módulo instalado (también solo de efectos o si falla la fábrica)
            // Game 1.11.1: from here on each block of patches goes in its own phase (LogicaCarga.Fase): if the game removes or moves a type
            // or a method, that block fails (or only that patch, with the deferred types of Parchear) and everything else keeps loading.
            // 0.7.1: online leaderboards, with any module installed (also with effects-only modules or if the factory fails)
            Fase("Clasificaciones y telemetría", AplicarParcheClasificaciones);
            // Héroes nuevos en el mapa de campaña y en los assets de héroe (CampanaHeroes.cs), también con su módulo apagado
            // o la fábrica caída: un save puede traer un 9xx y el juego se saldría de sus arrays (opcionales)
            // New heroes in the campaign map and in the hero assets (CampanaHeroes.cs), also with their module turned off
            // or the factory down: a save may contain a 9xx and the game would go out of its arrays (optional)
            _harmonyCampana = new Harmony(GUID + ".campana");
            Fase("Héroes nuevos en el mapa de campaña", () =>
            {
                foreach (var p in CampanaHeroes.Parches())
                    Parchear(p.nombre, p.tipo, p.metodo, p.args, p.prefix, p.postfix, p.finalizer, false, typeof(CampanaHeroes), _harmonyCampana);
            });
            // Acción propia de los héroes nuevos en «Elige uno» tras la batalla (AccionesHeroes.cs; opcional, solo actúa con un
            // héroe nuevo de un módulo activo)
            // Custom action for the new heroes in "Elige uno" after the battle (AccionesHeroes.cs; optional, only acts with a
            // new hero from an active module)
            Fase("Acción de los héroes nuevos tras la batalla", () =>
            {
                foreach (var p in AccionesHeroes.Parches())
                    Parchear(p.nombre, p.tipo, p.metodo, p.args, p.prefix, p.postfix, p.finalizer, false, typeof(AccionesHeroes), _harmonyCampana);
            });
            // 0.7.2: logros de Steam de serie sin contar unidades ni héroes 9xx (opcionales; con Logros.Filtrar = false no se ponen)
            // 0.7.2: default Steam achievements without counting 9xx units or heroes (optional; with Logros.Filtrar = false they are not applied)
            Fase("Logros de Steam filtrados", AplicarParchesLogros);

            _harmony = new Harmony(GUID);
            if (HayContenido())
            {
                // Crítica: si la fase entera falla (por ejemplo, un tipo de la fábrica que ya no existe), el estado de la
                // fábrica queda a medias; se apunta en _fallosCriticos y la retirada de abajo quita sus parches
                // Critical: if the whole phase fails (for example, a factory type that no longer exists), the factory state
                // is left half-done; it is recorded in _fallosCriticos and the rollback below removes its patches
                Fase("Fábrica de unidades y facciones", AplicarParches, critico: true);
                // F2: si no engancha el alta de los héroes nuevos, las razas 40/41 se quedarían sin héroes (general rival y
                // señor de la guerra caerían en Edric): se desactivan las facciones nuevas y todo vuelve a la F1
                // Juego 1.11.1 (revisión): en su fase (FaccionesNuevas tiene inicializadores estáticos con tipos del juego)
                // F2: if the hook for registering the new heroes doesn't attach, races 40/41 would be left without heroes (rival general and
                // warlord would fall back to Edric): the new factions are disabled and everything goes back to F1
                // Game 1.11.1 (review): in its own phase (FaccionesNuevas has static initializers with game types)
                Fase("Comprobación del alta de los héroes nuevos", () =>
                {
                    if (_fallosCriticos.Count == 0 && FaccionesNuevas.Activas && !_heroesEnganchados)
                    {
                        Error("Facciones nuevas DESACTIVADAS en esta sesión: no engancha el alta de los héroes (HeroData). Todo queda como en la F1.");
                        FaccionesNuevas.ForzarDesactivadas = true;
                        Nucleo.Aplicar(Nucleo.Real);
                    }
                });
            }
            else Info("Ningún módulo de contenido activo: no se aplican los parches de la fábrica de unidades (catálogo de serie).");

            if (_fallosCriticos.Count > 0)
            {
                // Algo esencial no engancha (lo normal: un parche del juego cambió una firma). Se quita todo.
                // Something essential doesn't hook (usual cause: a game patch changed a signature). Everything is removed.
                try { _harmony.UnpatchSelf(); } catch (Exception e) { Error("Error al quitar parches: " + e.Message); }
                Operativo = false;
                Error("Fábrica DESACTIVADA: fallaron parches críticos -> " + string.Join(" | ", _fallosCriticos));
                Error("El juego sigue sin unidades nuevas. Las escuadras guardadas de unidades nuevas cargarán como su unidad base.");
                // Juego 1.11.1 (revisión): sin fábrica, las razas 40/41 y sus héroes tampoco tienen sentido: se desactivan y se
                // rehace el contenido de los módulos (efectos sí), dentro de su fase
                // Game 1.11.1 (review): without the factory, races 40/41 and their heroes make no sense either: they are disabled and the
                // modules' content is rebuilt (effects yes), within its phase
                Fase("Facciones nuevas desactivadas por fallo de la fábrica", () =>
                {
                    FaccionesNuevas.ForzarDesactivadas = true;
                    Nucleo.Aplicar(Nucleo.Real);
                });
                Fase("Ejército reforzado", AplicarParchesReforzadoSeguro);
                AvisarHeroeDeLaPartida(datosJuego, false);
                return;
            }

            Operativo = HayContenido();
            Fase("Ejército reforzado", AplicarParchesReforzadoSeguro);
            if (Operativo) Fase("Objetos nuevos", AplicarParchesObjetosSeguro);
            // Juego 1.11.1 (revisión): el resumen lee FaccionesNuevas y Objetos (inicializadores estáticos): en su fase
            // Game 1.11.1 (review): the summary reads FaccionesNuevas and Objetos (static initializers): in its phase
            Fase("Resumen de la carga en el log", EscribirResumen);
            AvisarHeroeDeLaPartida(datosJuego, Operativo);

            // Diagnóstico opcional: abre la pantalla de héroes en el menú y hace capturas (sin empezar partida)
            // Optional diagnostic: opens the heroes screen in the menu and takes screenshots (without starting a run)
            Fase("Diagnóstico de la pantalla de héroes", PrepararDiagnosticoPantalla);

            if (!Operativo) return;
            // Vigilante de saves sobre la carpeta de datos del juego (ruta calculada sin llamar al juego, así arranca
            // antes de que exista nada del catálogo)
            // Save watcher on the game's data folder (path computed without calling the game, so it starts
            // before anything from the catalog exists)
            Fase("Vigilante de saves", () => Saves.ArrancarVigilante(datosJuego));

            // Si el catálogo del juego ya se construyó antes de cargar el plugin, se registran las unidades ahora
            // If the game's catalog was already built before the plugin loaded, the units are registered now
            Fase("Alta de las unidades con el catálogo ya cargado", UnidadGuardia.RegistrarSiYaHayCatalogo);
        }

        // 0.7.11: parches del aviso de instalación (instancia propia, opcionales: si no enganchan queda el log) y su vigilante
        // 0.7.11: installation warning patches (own instance, optional: if they don't hook the log remains) and its watcher
        private Harmony _harmonyInstalacion;
        private void AplicarParchesInstalacion()
        {
            _harmonyInstalacion = new Harmony(GUID + ".instalacion");
            Parchear("Aviso de instalación: menú principal montado (SceneHandler.AlertOfSceneSetUpComlete)", () => typeof(Memori.Scenes.SceneHandler),
                "AlertOfSceneSetUpComlete", () => new[] { typeof(bool) }, null, nameof(AvisoInstalacion.TrasAlertaEscena), null, false,
                typeof(AvisoInstalacion), _harmonyInstalacion);
            Parchear("Menú de mods: estado de los módulos de Zero (ModListRow.Setup)", () => typeof(TJ.ModListRow), "Setup", null,
                null, nameof(AvisoInstalacion.TrasModListRowSetup), null, false, typeof(AvisoInstalacion), _harmonyInstalacion);
            if (!Il2CppInterop.Runtime.Injection.ClassInjector.IsTypeRegisteredInIl2Cpp<VigiaAvisoInstalacion>())
                Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<VigiaAvisoInstalacion>();
            AddComponent<VigiaAvisoInstalacion>();
        }

        // Juego 1.11.1 (revisión): líneas de resumen de la carga (antes en Load), aparte para ir en su fase
        // Game 1.11.1 (review): load summary lines (previously in Load), separated so they go in their own phase
        private void EscribirResumen()
        {
            Info($"Núcleo {VERSION} listo (juego {VersionJuego ?? "versión desconocida"}, probado con {VersionJuegoProbada}): {_aplicados} parches aplicados" +
                 (_fallosOpcionales.Count > 0 ? $", {_fallosOpcionales.Count} opcionales fallidos ({string.Join(" | ", _fallosOpcionales)})" : ", 0 fallidos") +
                 $"; fábrica de unidades {(Operativo ? "operativa" : "sin usar")}.");
            Info($"Config: probabilidad de oferta {CfgProbabilidad.Value:0.00}, siempre en ofertas {CfgSiempreEnOfertas.Value}, " +
                 $"estela roja {CfgEstelaRoja.Value}, sonido propio {CfgSonidoPropio.Value} (impacto {CfgImpactoPropio.Value}, ganancia {CfgGananciaDb.Value:0.#} dB), bloquear clasificaciones {CfgBloquearClasificaciones.Value}, bloquear telemetría {CfgBloquearTelemetria.Value}, bloquear multijugador {CfgBloquearMultijugador.Value}");
            Info($"Config: proyectil por unidad {CfgProyectilPorUnidad.Value}, jinete de caballería {CfgJineteCaballeria.Value}; facciones nuevas en esta sesión {FaccionesNuevas.Activas}, " +
                 $"Guardia Imperial en Astra Militarum {CfgGuardiaEnAstra.Value} (en esta sesión: {FaccionesNuevas.GuardiaEnAstra}), pantalla de héroes ampliada {CfgPantallaHeroes.Value}, prueba de pantalla {CfgProbarPantalla.Value}, " +
                 $"interfaz: Colección {CfgInterfazColeccion.Value}, batalla personalizada {CfgInterfazBatalla.Value}; logros filtrados {CfgLogrosFiltrar.Value}");
            Info($"Config: objetos nuevos operativos {Objetos.Operativo} ({Objetos.Numero} en los módulos activos), probabilidad afín " +
                 $"{CfgObjetosProbAfin.Value:0.00}, general {CfgObjetosProbGeneral.Value:0.00}, modo prueba {CfgObjetosModoPrueba.Value}");
        }

        // Juego 1.11.1 (revisión): diagnóstico opcional de la pantalla de héroes (antes en Load), aparte para ir en su fase
        // Game 1.11.1 (review): optional heroes screen diagnostic (previously in Load), separated so it goes in its own phase
        private void PrepararDiagnosticoPantalla()
        {
            if (CfgProbarPantalla.Value && FaccionesNuevas.Activas)
            {
                try
                {
                    if (!Il2CppInterop.Runtime.Injection.ClassInjector.IsTypeRegisteredInIl2Cpp<DiagnosticoPantalla>())
                        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<DiagnosticoPantalla>();
                    AddComponent<DiagnosticoPantalla>();
                    Info("Diagnóstico de la pantalla de héroes preparado (Diagnostico.ProbarPantallaHeroes = true).");
                }
                catch (Exception e) { Error("No se pudo preparar el diagnóstico de la pantalla de héroes: " + e.Message); }
            }
            // 0.7.11: prueba del menú de mods (estado de los módulos en cada fila), solo con Diagnostico.ProbarMenuMods
            // 0.7.11: mod menu test (module status in each row), only with Diagnostico.ProbarMenuMods
            if (CfgProbarMenuMods != null && CfgProbarMenuMods.Value)
            {
                try
                {
                    if (!Il2CppInterop.Runtime.Injection.ClassInjector.IsTypeRegisteredInIl2Cpp<DiagnosticoMenuMods>())
                        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<DiagnosticoMenuMods>();
                    AddComponent<DiagnosticoMenuMods>();
                    Info("Diagnóstico del menú de mods preparado (Diagnostico.ProbarMenuMods = true).");
                }
                catch (Exception e) { Error("No se pudo preparar el diagnóstico del menú de mods: " + e.Message); }
            }
        }

        // ---------------------------------------------------------------
        // Juego 1.11.1: envoltorios de las fases con instancia propia. Si la fase entera falla a medias (excepción fuera de
        // Parchear), se quitan los parches que su instancia llegara a poner y la pieza queda desactivada, en vez de dejar
        // medio enganche puesto. La excepción sigue hacia Fase, que la apunta en el log.
        // Game 1.11.1: phase wrappers with their own instance. If the whole phase fails halfway (exception outside
        // Parchear), the patches its instance managed to apply are removed and the piece is disabled, instead of leaving
        // a half-applied hook. The exception continues to Fase, which records it in the log.
        // ---------------------------------------------------------------
        private void AplicarParchesReforzadoSeguro()
        {
            try { AplicarParchesReforzado(); }
            catch
            {
                try { _harmonyReforzado?.UnpatchSelf(); } catch { }
                EjercitoReforzado.Operativo = false;
                throw;
            }
        }

        private void AplicarParchesObjetosSeguro()
        {
            try { AplicarParchesObjetos(); }
            catch
            {
                try { _harmonyObjetos?.UnpatchSelf(); } catch { }
                Objetos.Operativo = false;
                Objetos.ActivosEnCache = false;
                throw;
            }
        }

        // ---------------------------------------------------------------
        // Logros de Steam filtrados (0.7.2): instancia propia que no se quita con las demás. Se aplica con cualquier módulo
        // instalado y, desde la revisión, también sin módulos si queda el registro de saves de una sesión con módulos. Solo
        // usa el save del jugador y la regla «id 900 o más = nuevo» (LogicaLogros), nada del resto del núcleo
        // Filtered Steam achievements (0.7.2): its own instance, which is not removed with the others. It is applied with any installed
        // module and, since the review, also without modules if the saves registry from a session with modules remains. It only
        // uses the player's save and the rule "id 900 or more = new" (LogicaLogros), nothing from the rest of the core
        // ---------------------------------------------------------------
        private void AplicarParchesLogros()
        {
            if (!CfgLogrosFiltrar.Value)
            {
                Info("Logros de Steam: sin filtrar (Logros.Filtrar = false); las unidades y héroes nuevos cuentan como el juego los ve.");
                return;
            }
            if (_harmonyLogros != null) return;
            _harmonyLogros = new Harmony(GUID + ".logros");
            foreach (var p in LogrosFiltrados.Parches())
                Parchear(p.nombre, p.tipo, p.metodo, p.args, p.prefix, p.postfix, p.finalizer, false, typeof(LogrosFiltrados), _harmonyLogros);
            // Arreglos de inlining de los logros: AquiredGear, embudo UnlockAchievement y UnitKills sin 9xx (ParchesInlineados.cs)
            // Inlining fixes for achievements: AquiredGear, UnlockAchievement funnel and UnitKills without 9xx (ParchesInlineados.cs)
            foreach (var p in ParchesInlineados.ParchesLogros())
                Parchear(p.nombre, p.tipo, p.metodo, p.args, p.prefix, p.postfix, p.finalizer, false, typeof(ParchesInlineados), _harmonyLogros);
        }

        // ---------------------------------------------------------------
        // Versión del juego frente a la probada (revisión externa del 18-10, antes de la actualización del 27-10)
        // Game version versus the tested one (external review of 18-10, before the 27-10 update)
        // ---------------------------------------------------------------
        /// <summary>Application.version del juego en esta sesión (null si no se pudo leer).</summary>
        /// Game's Application.version in this session (null if it could not be read).
        internal static string VersionJuego { get; private set; }

        // Lee Application.version (y buildGUID, solo para el log) y avisa si no es VersionJuegoProbada. Solo log: el núcleo
        // no tiene aviso en pantalla propio ni se desactiva solo; los parches que no enganchen ya avisan uno a uno
        // («Parche no aplicado» / «Parche crítico FALLIDO») y la fábrica se quita sola si falla uno crítico.
        // Reads Application.version (and buildGUID, for the log only) and warns if it is not VersionJuegoProbada. Log only: the core
        // has no on-screen warning of its own and does not disable itself; patches that fail to hook already warn one by one
        // ("Parche no aplicado" / "Parche crítico FALLIDO") and the factory removes itself if a critical one fails.
        private static void ComprobarVersionJuego()
        {
            string guid = null;
            try
            {
                VersionJuego = UnityEngine.Application.version;
                try { guid = UnityEngine.Application.buildGUID; } catch { }
            }
            catch (Exception e)
            {
                Aviso($"No se pudo leer la versión del juego ({e.GetType().Name}: {e.Message}); no se comprueba si el núcleo está probado con ella (probado con {VersionJuegoProbada}).");
                return;
            }
            string aviso = LogicaVersion.Aviso(VersionJuegoProbada, VersionJuego);
            if (aviso == null) Info($"Versión del juego {VersionJuego} (build {guid ?? "?"}): la misma con la que se probó el núcleo.");
            else Aviso(aviso + $" (build {guid ?? "?"})");
        }

        // ---------------------------------------------------------------
        // Carpeta de datos del juego y héroe de la partida en curso (0.7.1)
        // Game data folder and hero of the current run (0.7.1)
        // ---------------------------------------------------------------
        // Application.persistentDataPath (la que usa el juego para saves y Mods); si no se puede leer o no existe, la ruta
        // fija de siempre (%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern)
        // Application.persistentDataPath (the one the game uses for saves and Mods); if it cannot be read or does not exist, the usual
        // fixed path (%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern)
        private static string CalcularCarpetaDatos()
        {
            try
            {
                string p = UnityEngine.Application.persistentDataPath;
                if (!string.IsNullOrEmpty(p))
                {
                    p = Path.GetFullPath(p);
                    if (Directory.Exists(p)) return p;
                    Aviso($"Application.persistentDataPath ({p}) no existe; se usa la ruta fija {RutaDatosFija}.");
                }
            }
            catch (Exception e) { Aviso($"No se pudo leer Application.persistentDataPath ({e.GetType().Name}: {e.Message}); se usa la ruta fija {RutaDatosFija}."); }
            return RutaDatosFija;
        }

        // Una línea en el log al cargar si la partida en curso usa un héroe nuevo (9xx) cuyo módulo no está activo: sin el
        // saneado (ningún módulo de contenido activo) el juego lo cambia por Edric y su retrato falla; con el saneado se
        // carga como su héroe prestado. También si el registro guarda un héroe nuevo de esta partida que ahora no vuelve.
        // A log line on load if the current run uses a new hero (9xx) whose module is not active: without
        // sanitizing (no content module active) the game replaces it with Edric and its portrait fails; with sanitizing it is
        // loaded as its borrowed hero. Also if the registry holds a new hero from this run that now does not come back.
        private static void AvisarHeroeDeLaPartida(string datosJuego, bool conSaneado)
        {
            try
            {
                string ruta = Path.Combine(datosJuego, "campaignSaveData.json");
                if (!File.Exists(ruta)) return;
                string texto;
                using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var sr = new StreamReader(fs)) texto = sr.ReadToEnd();
                var m = System.Text.RegularExpressions.Regex.Match(texto, "\"heroID\"\\s*:\\s*(\\d+)");
                if (!m.Success || !int.TryParse(m.Groups[1].Value, out int heroe)) return;
                if (heroe >= 900 && heroe <= 999 && !FaccionesNuevas.EsHeroeNuevo(heroe))
                {
                    var tabla = Saves.TablaHeroes;
                    if (conSaneado && tabla != null && tabla.EsNuevo(heroe))
                        Aviso($"La partida en curso usa el héroe nuevo {heroe} y su módulo no está activo: se carga con su héroe prestado ({tabla.PrestadoDe(heroe)}). Al activar el módulo otra vez vuelve el {heroe}.");
                    else
                        Aviso($"La partida en curso usa el héroe nuevo {heroe} y su módulo no está activo: el juego lo cambiará por Edric y su retrato puede fallar. Activa su módulo (Astra Militarum o Imperio Galáctico) y reinicia, o termina esa partida con el módulo activo.");
                    return;
                }
                if (Saves.Registro != null && Saves.Registro.HeroeDe("heroe", out int real, out int escrito, out _) && escrito == heroe && !FaccionesNuevas.EsHeroeNuevo(real))
                    Info($"La partida en curso es del héroe nuevo {real}, que ahora no está activo: sigue con su héroe prestado ({heroe}). Al activar su módulo otra vez vuelve el {real}.");
            }
            catch (Exception e) { Aviso("No se pudo revisar el héroe de la partida en curso: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // Migración desde la 0.6.1 (plugin monolítico, GUID zero.tabletoptavern.guardiaimperial)
        // Migration from 0.6.1 (monolithic plugin, GUID zero.tabletoptavern.guardiaimperial)
        // ---------------------------------------------------------------
        // Si no existe el .cfg nuevo y sí el viejo, se copia (el viejo se queda como estaba) y se recarga. Las opciones
        // que ya no existen (EjercitoReforzado.*, Facciones.Activadas, Objetos.Activados: ahora mandan los módulos) se
        // quedan en el fichero sin efecto.
        // If the new .cfg does not exist but the old one does, it is copied (the old one is left as it was) and reloaded. Options
        // that no longer exist (EjercitoReforzado.*, Facciones.Activadas, Objetos.Activados: the modules are now in charge) are
        // left in the file with no effect.
        private void MigrarConfiguracion()
        {
            try
            {
                string nuevo = Path.Combine(Paths.ConfigPath, GUID + ".cfg");
                string viejo = Path.Combine(Paths.ConfigPath, GuidViejo + ".cfg");
                if (File.Exists(nuevo) || !File.Exists(viejo)) return;
                File.Copy(viejo, nuevo);
                Config.Reload();
                Info($"Configuración de la 0.6.1 copiada a {nuevo} (la vieja se conserva).");
            }
            catch (Exception e) { Aviso("No se pudo copiar la configuración de la 0.6.1: " + e.Message); }
        }

        // El registro de escuadras de la 0.6.1 se COPIA al nombre nuevo (el viejo se conserva intacto como respaldo)
        // The squad registry from 0.6.1 is COPIED to the new name (the old one is kept intact as a backup)
        private static void MigrarRegistro(string nuevo, string viejo)
        {
            try
            {
                if (File.Exists(nuevo) || !File.Exists(viejo)) return;
                File.Copy(viejo, nuevo);
                Info($"Registro de escuadras de la 0.6.1 copiado a {nuevo} (el viejo se conserva en {viejo}).");
            }
            catch (Exception e) { Error("No se pudo copiar el registro de escuadras de la 0.6.1 (las escuadras de unidades nuevas cargarían como su base): " + e.Message); }
        }

        // Crea/lee las opciones del fichero .cfg (con descripción en español)
        // Creates/reads the options of the .cfg file (with descriptions in Spanish)
        private void LeerConfiguracion()
        {
            CfgActivado = Config.Bind("General", "Activado", true,
                "Interruptor general del plugin. false = no aplica ningún parche (la DLL se carga pero no hace nada).");
            CfgProbabilidad = Config.Bind("Reclutamiento", "Probabilidad", 0.25f,
                "Probabilidad (0 a 1) de que una oferta de reclutamiento del jugador (tienda, ciudades, recompensas, eventos) " +
                "incluya una Guardia Imperial sustituyendo a una de las cartas, si su rango lo permite.");
            CfgSiempreEnOfertas = Config.Bind("Reclutamiento", "SiempreEnOfertas", false,
                "Modo prueba: true = la Guardia Imperial aparece en TODAS las ofertas del jugador donde su rango encaje. " +
                "Sigue siendo solo para el jugador.");
            CfgEstelaRoja = Config.Bind("Visual", "EstelaRoja", true,
                "Pinta de rojo la estela de los disparos de la Guardia Imperial.");
            CfgModeloPropio = Config.Bind("Visual", "ModeloPropio", true,
                "Usar el modelo 3D propio de la Guardia Imperial (Mods\\ZeroAstraMilitarum\\unit_visuals\\_900). " +
                "Si se desactiva o falta la carpeta, la unidad usa el modelo de EisenmannRegiment. Con el importador general vale para " +
                "todas las unidades nuevas con modelo propio (unit_visuals\\_<id> de su módulo): false = todas con el de su base.");
            CfgModeloBaseEn = Config.Bind("Visual", "ModeloBaseEn", "",
                "Ids de unidades nuevas que deben usar el modelo de su unidad base aunque traigan modelo propio, separados " +
                "por comas (por ejemplo: 901, 906). Vacío = todas con su modelo propio. Se aplica al recargar los datos.");
            CfgModeloHeroes = Config.Bind("Visual", "ModeloHeroes", true,
                "Fase F5: los héroes nuevos (Yarrick, Creed, Vader, Thrawn) salen con su propio modelo 3D en la pantalla de héroes, " +
                "la Colección, la taberna de la partida y el mapa de campaña (heroes\\9xx_modelo.bin de su módulo), animado con las " +
                "animaciones de su héroe de serie prestado. false = la figura del héroe prestado, como antes. Si falta el modelo o algo " +
                "no cuadra, también se ve la prestada.");
            CfgSonidoPropio = Config.Bind("Audio", "SonidoPropio", true,
                "Sonido de energía para las armas láser y bláster de las unidades nuevas (Astra Militarum e Imperio Galáctico; qué unidades, " +
                "lo dice el campo «sonidos» de su módulo). Por orden: 1) láser propio desde los WAV de la carpeta audio\\ del módulo, " +
                "2) si no se pueden crear, un sonido de hechizo del juego ya cargado (Smite Charge o LightningStrike), 3) si tampoco, el disparo " +
                "de la unidad base. false = todas con el disparo de su unidad base, como el juego.");
            CfgImpactoPropio = Config.Bind("Audio", "ImpactoPropio", false,
                "Sonido de impacto láser (laser_impacto_*.wav) cuando acierta un disparo de energía; solo vale con SonidoPropio = true y WAV propios. " +
                "false = impacto mudo, como las armas de fuego del juego.");
            CfgGananciaDb = Config.Bind("Audio", "GananciaDb", 0f,
                "Ajuste de volumen de los sonidos propios en decibelios (de -24 a 12; -6 = la mitad de fuerte). " +
                "Se aplica al cargar los WAV, al arrancar el juego.");
            // 18-10-2026: hasta la 0.7.2 SonidoPropio venía en false (se creía que en Unity 6 no se podían crear sonidos). Una sola
            // vez, una configuración anterior en false pasa a true; después manda lo que elija el usuario
            // 18-10-2026: up to 0.7.2 SonidoPropio defaulted to false (it was believed that sounds could not be created in Unity 6). Only once,
            // an earlier setting of false becomes true; after that, whatever the user chooses applies
            CfgRevisionSonido = Config.Bind("Audio", "RevisionSonido", 0,
                "Uso interno: revisión de las opciones de audio ya migradas (no hace falta tocarla).");
            if (CfgRevisionSonido.Value < LogicaSonidos.RevisionSonidoActual)
            {
                CfgSonidoPropio.Value = LogicaSonidos.MigrarSonidoPropio(CfgSonidoPropio.Value, CfgRevisionSonido.Value, out bool migrado);
                CfgRevisionSonido.Value = LogicaSonidos.RevisionSonidoActual;
                Config.Save();
                if (migrado) Info("Audio: SonidoPropio pasa a true (migración única del 18-10-2026: el sonido láser ya funciona). Se puede volver a poner en false.");
            }
            CfgBloquearClasificaciones = Config.Bind("Clasificaciones", "Bloquear", true,
                "No envía partidas a las clasificaciones online mientras haya algún módulo de Zero aplicado (efectos, contenido o " +
                "Economía): son partidas modificadas.");
            CfgBloquearTelemetria = Config.Bind("Online", "BloquearTelemetria", true,
                "No apunta ni envía la telemetría del juego (eventos de partida, nodo, batalla y excepciones que el juego manda a su " +
                "servidor de estadísticas) mientras haya algún módulo de Zero aplicado: llevarían ids de unidades, héroes y objetos " +
                "nuevos. false = el juego la envía como siempre.");
            CfgBloquearMultijugador = Config.Bind("Multijugador", "Bloquear", true,
                "No deja abrir ni unirse a partidas en línea (cooperativo y versus) mientras haya algún módulo de Zero aplicado: el " +
                "otro jugador no tendría las mismas unidades, héroes, hechizos ni efectos. false = el juego conecta como siempre " +
                "(sin soporte: puede fallar o desincronizarse).");
            CfgTrazas = Config.Bind("Diagnostico", "Trazas", false,
                "Escribe en el log cada paso del alta de la unidad (para localizar un cierre del juego).");
            CfgSinSonido = Config.Bind("Diagnostico", "SinSonido", false,
                "Solo para pruebas: el juego arranca y sigue sin ningún sonido (volumen global de Unity a 0 desde la carga). " +
                "No cambia tus ajustes de volumen: con false suena como siempre.");
            CfgProbarMenuMods = Config.Bind("Diagnostico", "ProbarMenuMods", false,
                "Solo para pruebas: ya en el menú abre el menú de mods, escribe en el log cada fila (con el estado de los módulos de Zero) " +
                "y hace una captura en BepInEx\\config\\ZeroNucleo_capturas; no toca ningún interruptor.");
            CfgOmitir = Config.Bind("Diagnostico", "OmitirParches", "",
                "Solo para diagnosticar cierres: nombres de método del juego separados por comas que NO se parchean (p. ej. ToJson,WriteAllText). " +
                "Submit (bloqueo de las clasificaciones online) no se omite mientras haya módulos de Zero aplicados y Clasificaciones.Bloquear = true.");
            CfgRespaldo = Config.Bind("Diagnostico", "RespaldoAssets", true,
                "Red de seguridad: si un getter de assets (icono de tipo, bandera, icono, prefab, voces) falla con la 900, " +
                "devuelve lo de EisenmannRegiment en vez de dejar la carta bocabajo.");
            CfgAutoPrueba = Config.Bind("Diagnostico", "AutoPrueba", false,
                "Solo para pruebas: al arrancar, comprueba una vez en el menú los módulos (detección y combinaciones simuladas, que " +
                "recargan el catálogo del juego varias veces) y lo que aplican: unidades nuevas, reclutamiento, saneado de saves, " +
                "escuadras y bonus del jugador (resultado en el log con la etiqueta [autoprueba]). No toca ninguna partida. Tarda unos " +
                "segundos más en el arranque.");
            CfgProyectilPorUnidad = Config.Bind("Batalla", "ProyectilPorUnidad", true,
                "Cambia la flecha por defecto de las unidades nuevas por el proyectil del manifiesto (bala, bala de cañón, hacha, virote...).");
            CfgGuardiaEnAstra = Config.Bind("Facciones", "GuardiaEnAstra", true,
                "Fase F3: la Guardia Imperial (900) es la unidad común a distancia de Astra Militarum: va en la lista de la raza 40 " +
                "(también para la IA de esa raza) y deja de ofrecerse a las facciones de serie. false = como antes (solo el jugador, " +
                "cualquier facción, 25 % de las ofertas). Solo vale con el módulo Astra Militarum activo. Se lee al arrancar el juego.");
            CfgPantallaHeroes = Config.Bind("Facciones", "PantallaHeroes", true,
                "Añade a la pantalla de héroes de la nueva partida las tarjetas de las facciones nuevas (con desplazamiento vertical). " +
                "false = la pantalla de serie: enseña lo que cabe en sus 18 casillas (Astra Militarum sale en la 9.ª tarjeta, que en " +
                "la pantalla de serie no cabe entera en el panel; el Imperio no sale). Los héroes nuevos siguen como enemigos.");
            CfgProbarPantalla = Config.Bind("Diagnostico", "ProbarPantallaHeroes", false,
                "Solo para pruebas: tras la autoprueba abre la pantalla de héroes en el menú, elige Yarrick y Vader y hace capturas en " +
                "BepInEx\\config\\ZeroNucleo_capturas (sin empezar ninguna partida).");
            CfgObjetosProbAfin = Config.Bind("Objetos", "ProbabilidadAfin", 0.25f,
                "Probabilidad (0 a 1) de que cada carta de gear se cambie por un objeto nuevo AFÍN (de tu facción, de una facción con 3 o " +
                "más escuadras en tu ejército o de la del enemigo o la ciudad de donde sale el botín).");
            CfgObjetosProbGeneral = Config.Bind("Objetos", "ProbabilidadGeneral", 0.05f,
                "Si no sale el afín: probabilidad (0 a 1) de que la carta se cambie por cualquier objeto nuevo.");
            CfgObjetosModoPrueba = Config.Bind("Objetos", "ModoPrueba", false,
                "Solo para probar: true = TODAS las cartas de gear se cambian por objetos nuevos (afines primero) mientras queden sin tener.");
            // 0.7.13 (18-11-2026): encendida por defecto (probada en batalla con las 7 caballerías el 18-11-2026)
            // 0.7.13 (18-11-2026): on by default (tested in battle with the 7 cavalry units on 18-11-2026)
            CfgJineteCaballeria = Config.Bind("Batalla", "JineteCaballeria", true,
                "Añade el componente de caballería con jinete a las unidades nuevas montadas (902, 923, 926, 947, 950, 963, 967) " +
                "para que se vea el jinete sobre la montura, como en la caballería de serie. Con false luchan igual pero la montura " +
                "sale sin jinete.");
            CfgJineteRevisado = Config.Bind("Batalla", "JineteCaballeriaRevisado", false,
                "No tocar: lo pone el núcleo 0.7.13 al pasar JineteCaballeria al nuevo valor por defecto (true) una sola vez. Con " +
                "true se respeta lo que pongas en JineteCaballeria.");
            // Migración (LogicaJinete): el false de serie de los núcleos anteriores pasa a true una vez; después manda el jugador
            // Migration (LogicaJinete): the default false of the older cores becomes true once; after that the player decides
            var jinete = LogicaJinete.Migrar(CfgJineteCaballeria.Value, CfgJineteRevisado.Value);
            if (jinete.Valor != CfgJineteCaballeria.Value) CfgJineteCaballeria.Value = jinete.Valor;
            if (jinete.Marcador != CfgJineteRevisado.Value) CfgJineteRevisado.Value = jinete.Marcador;
            if (jinete.Encendida)
                Info("Config: Batalla.JineteCaballeria estaba en false (el valor de serie de los núcleos anteriores) y pasa a true, " +
                     "el nuevo valor por defecto; se hace una sola vez (Batalla.JineteCaballeriaRevisado = true). Si la quieres apagada, " +
                     "pon false: ya no se vuelve a cambiar.");
            CfgInterfazColeccion = Config.Bind("Interfaz", "Coleccion", true,
                "Colección del menú principal: las facciones nuevas (Astra Militarum, Imperio Galáctico) salen en la lista con sus " +
                "unidades y héroes, los objetos nuevos en Equipamiento, y la rejilla de unidades se baja con la rueda del ratón si una " +
                "facción pasa de 16 unidades. No cuenta para los logros ni envía nada online. false = la Colección de serie.");
            CfgLogrosFiltrar = Config.Bind("Logros", "Filtrar", true,
                "Logros de Steam de serie solo con contenido de serie: las unidades 9xx (también las sueltas de las facciones de serie), " +
                "los héroes 901-904 y los hechizos 1901-1904 no suman para los logros de colección por facción, Grand Collector, Max " +
                "Difficulty All Heroes, Roster Complete y Grand Grimoire (y no los bloquean). Ocho logros de composición del ejército " +
                "(Four Unique Factions, Grand Alliance, Legends Assembled, Master of Arms, Hired Swords, Goblin Horde, Kobold Swarm y " +
                "Shield Sisters) SÍ se bloquean cuando no se pueden recontar sin lo nuevo: en una partida jugada con módulos, y desde " +
                "el historial solo por las partidas jugadas con módulos (las de serie los conceden): en la práctica, esos 8 solo se " +
                "consiguen en sesiones SIN módulos de Zero activos. Los ya ganados se reenvían. El resto " +
                "de logros de batalla no se filtran. false = como el juego (lo nuevo cuenta y no se bloquea nada). Se lee al arrancar el juego.");
            CfgInterfazBatalla = Config.Bind("Interfaz", "BatallaPersonalizada", true,
                "Batalla personalizada: rombos y cartas de las facciones nuevas en «Generar unidades», y la rejilla de cartas se baja " +
                "con la rueda del ratón si una raza tiene más cartas de las que caben. false = el panel de serie.");
        }

        // ---------------------------------------------------------------
        // Lista de parches: (tipo del juego, método, argumentos, prefix, postfix, finalizer, crítico)
        // List of patches: (game type, method, arguments, prefix, postfix, finalizer, critical)
        // ---------------------------------------------------------------
        // Juego 1.11.1 (revisión del 01-11-2026): todos los Parchear de este método y de AplicarParchesFacciones pasan el tipo y
        // los argumentos diferidos («() => typeof(X)»): un tipo opcional que el juego quite o mueva (TJBakePreviewPlayer,
        // EntityWatcher, TrailRendererSetup, IAudioRequester...) solo hace fallar su parche, no la fábrica ni las facciones.
        // 0.7.5 (hallazgo 38): también AplicarParchesReforzado y AplicarParchesObjetos.
        // Game 1.11.1 (review of 01-11-2026): all the Parchear calls in this method and in AplicarParchesFacciones pass the type and
        // the arguments deferred ("() => typeof(X)"): an optional type that the game removes or moves (TJBakePreviewPlayer,
        // EntityWatcher, TrailRendererSetup, IAudioRequester...) only makes its own patch fail, not the factory or the factions.
        // 0.7.5 (finding 38): also AplicarParchesReforzado and AplicarParchesObjetos.
        private void AplicarParches()
        {
            Func<Type> tData = () => typeof(TJ.TabletopTavernData);

            // --- Registro de la unidad (crítico: al menos uno de los tres enganches de alta debe aplicarse) ---
            // --- Unit registration (critical: at least one of the three registration hooks must be applied) ---
            int altas = 0;
            altas += Parchear("Alta tras LoadStatsFromSOs", tData, "LoadStatsFromSOs", () => Type.EmptyTypes,
                null, nameof(UnidadGuardia.TrasLoadStatsFromSOs), null, false, typeof(UnidadGuardia));
            altas += Parchear("Alta antes de SquadStatsOverrideLoader.ApplyFile", () => typeof(TJ.SquadStatsOverrideLoader), "ApplyFile", null,
                nameof(UnidadGuardia.AntesDeApplyFile), null, null, false, typeof(UnidadGuardia));
            altas += Parchear("Alta antes de SquadStatsOverrideLoader.ApplyOverridesFromModFolder", () => typeof(TJ.SquadStatsOverrideLoader), "ApplyOverridesFromModFolder", null,
                nameof(UnidadGuardia.AntesDeApplyOverridesFromModFolder), null, null, false, typeof(UnidadGuardia));
            if (altas == 0) _fallosCriticos.Add("ningún enganche de alta de la unidad");

            // --- Visual (crítico: sin él la unidad saldría invisible) ---
            // El de ReloadData lleva además un prefix (rearma el escalado de Ejército reforzado en cada recarga) y el
            // postfix hace alta, listas de raza, escalado, visual y autoprueba
            // --- Visual (critical: without it the unit would be invisible) ---
            // The ReloadData one also has a prefix (re-arms the Ejército reforzado scaling on every reload) and the
            // postfix does registration, race lists, scaling, visual and self-test
            int visuales = 0;
            visuales += Parchear("Visual tras UnitVisualOverrideLoader.ApplyOverridesFromModFolder", () => typeof(TJ.UnitVisualOverrideLoader), "ApplyOverridesFromModFolder", null,
                null, nameof(UnidadGuardia.AsegurarVisualPostfix), null, false, typeof(UnidadGuardia));
            visuales += Parchear("Visual y listas de raza tras ReloadData", tData, "ReloadData", () => Type.EmptyTypes,
                null, nameof(UnidadGuardia.TrasReloadData), null, false, typeof(UnidadGuardia));
            if (visuales == 0) _fallosCriticos.Add("ningún enganche de registro del visual");

            // --- Retrato 3D (Colección, reclutamiento, prestigio): sin arma de melé en reposo (opcional) ---
            // El juego monta el retrato de un modelo de mod con todos sus adjuntos; este postfix esconde el 'sword' de las
            // unidades que cambian arco por espada, igual que la batalla (ver ArmasPrevia.cs)
            // --- 3D portrait (Collection, recruitment, prestige): no melee weapon at rest (optional) ---
            // The game builds the portrait of a mod model with all its attachments; this postfix hides the 'sword' of the
            // units that swap bow for sword, just like the battle does (see ArmasPrevia.cs)
            Parchear("Retrato 3D sin arma de melé en reposo (TJBakePreviewPlayer.Show)", () => typeof(MemoriStudios.TJBake.TJBakePreviewPlayer), "Show",
                () => new[] { typeof(MemoriStudios.TJBake.TJBakeVisual), typeof(int) },
                null, nameof(ArmasPrevia.TrasShow), null, false, typeof(ArmasPrevia));

            // --- Blob de stats para Burst (crítico: sin él Burst leería fuera del array) ---
            // --- Stats blob for Burst (critical: without it Burst would read out of bounds of the array) ---
            Parchear("Blob de stats (LoadAndInjectData)", tData, "LoadAndInjectData", () => Type.EmptyTypes,
                nameof(UnidadGuardia.AntesDeLoadAndInjectData), nameof(UnidadGuardia.TrasLoadAndInjectData),
                nameof(UnidadGuardia.FinalizarLoadAndInjectData), true, typeof(UnidadGuardia));

            // --- Saves: escritura (crítico: al menos un enganche) y lectura (crítico) ---
            // --- Saves: write (critical: at least one hook) and read (critical) ---
            Func<Type> tJson = () => typeof(UnityEngine.JsonUtility);
            int escrituras = 0;
            escrituras += Parchear("Saneado al escribir (JsonUtility.ToJson)", tJson, "ToJson",
                () => new[] { typeof(Il2CppSystem.Object), typeof(bool) }, null, nameof(Saves.TrasToJson), null, false, typeof(Saves));
            escrituras += Parchear("Saneado al escribir (File.WriteAllText)", () => typeof(Il2CppSystem.IO.File), "WriteAllText",
                () => new[] { typeof(string), typeof(string) }, nameof(Saves.AntesDeWriteAllText), null, null, false, typeof(Saves));
            if (escrituras == 0) _fallosCriticos.Add("ningún enganche de saneado al escribir saves");
            int lecturas = 0;
            lecturas += Parchear("Restaurar al leer (JsonUtility.FromJson)", tJson, "FromJson",
                () => new[] { typeof(string), typeof(Il2CppSystem.Type) }, nameof(Saves.AntesDeFromJson), null, null, false, typeof(Saves));
            lecturas += Parchear("Restaurar al leer (JsonUtility.FromJsonInternal)", tJson, "FromJsonInternal",
                () => new[] { typeof(string), typeof(Il2CppSystem.Object), typeof(Il2CppSystem.Type) }, nameof(Saves.AntesDeFromJson), null, null, false, typeof(Saves));
            if (lecturas == 0) _fallosCriticos.Add("ningún enganche de restauración al leer saves");

            // --- Reclutamiento del jugador (opcional: sin él la unidad simplemente no se ofrece) ---
            // --- Player recruitment (optional: without it the unit is simply not offered) ---
            Parchear("Oferta por reputación (recompensas/eventos/consumibles)", tData, "GetSquadsToRecruitBasedOnReputation", null,
                null, nameof(Reclutamiento.TrasReputacion), null, false, typeof(Reclutamiento));
            Parchear("Oferta de packs de la tienda", tData, "GetSquadsToRecruitForPack", null,
                null, nameof(Reclutamiento.TrasPack), null, false, typeof(Reclutamiento));
            Parchear("Oferta de ciudades", tData, "GetSquadsToRecruitForRaceTown", null,
                null, nameof(Reclutamiento.TrasCiudad), null, false, typeof(Reclutamiento));
            Parchear("Oferta tras batalla", tData, "GetSquadsToRecruitForRaceBattle", null,
                null, nameof(Reclutamiento.TrasBatalla), null, false, typeof(Reclutamiento));

            // --- Red de seguridad de la interfaz: lecturas de assets por unidad (opcionales) ---
            // --- UI safety net: per-unit asset reads (optional) ---
            foreach (var (metodo, finalizer) in (Plugin.CfgRespaldo.Value ? Redireccion.Parches : System.Array.Empty<(string, string)>()))
                Parchear("Respaldo de assets " + metodo, tData, metodo, null,
                    null, null, finalizer, false, typeof(Redireccion));

            // --- Batalla: proyectil de fusil y estela roja (opcionales) ---
            // --- Battle: rifle projectile and red trail (optional) ---
            Parchear("Latido de batalla (EntityWatcher.Update)", () => typeof(TJ.EntityWatcher), "Update", () => Type.EmptyTypes,
                null, nameof(Batalla.TrasEntityWatcherUpdate), null, false, typeof(Batalla));
            Parchear("Reinicio de batalla (EntityWatcher.SetUp)", () => typeof(TJ.EntityWatcher), "SetUp", () => Type.EmptyTypes,
                null, nameof(Batalla.TrasEntityWatcherSetUp), null, false, typeof(Batalla));
            Parchear("Estela de disparo (TrailRendererSetup.SetupTrail)", () => typeof(TrailRendererSetup), "SetupTrail", null,
                null, nameof(Batalla.TrasSetupTrail), null, false, typeof(Batalla));

            // --- Batalla: sonido propio de disparo e impacto (opcional; sin él suena el de Eisenmann) ---
            // --- Battle: custom firing and impact sound (optional; without it Eisenmann's plays) ---
            if (CfgSonidoPropio.Value)
                Parchear("Sonido propio (TabletopTavernData.GetBattlefieldAudio)", tData, "GetBattlefieldAudio", null,
                    null, nameof(AudioGuardia.TrasGetBattlefieldAudio), null, false, typeof(AudioGuardia));

            // --- F2: razas y héroes nuevos (solo si las facciones nuevas están activas en esta sesión) ---
            // Juego 1.11.1: en su propia fase opcional. Si falla entera, _heroesEnganchados se queda en false y Load() desactiva
            // las facciones nuevas como siempre (todo vuelve a la F1), sin contar como fallo crítico de la fábrica
            // --- F2: new races and heroes (only if the new factions are active in this session) ---
            // Game 1.11.1: in its own optional phase. If it fails entirely, _heroesEnganchados stays false and Load() disables
            // the new factions as usual (everything goes back to F1), without counting as a critical factory failure
            if (FaccionesNuevas.Activas) Fase("Facciones nuevas (razas 40/41 y héroes 901-904)", () => AplicarParchesFacciones(tData));

            // --- 0.7.7: estandarte (Estandarte.cs). Presets 40/41 con las facciones; selector e icono en batalla con algún
            // módulo con iconos. Solo métodos sin argumentos y NO plegados en GameAssembly (EnsureBuilt, Refresh, ApplyTeamColors;
            // xref del 03-11): nada de structs por el trampolín y nunca Start/Awake/Update vacíos (plegados con miles de métodos) ---
            // --- 0.7.7: banner (Estandarte.cs). Presets 40/41 with the factions; selector and icon in battle with any
            // module that has icons. Only parameterless methods that are NOT folded in GameAssembly (EnsureBuilt, Refresh, ApplyTeamColors;
            // xref from 03-11): no structs through the trampoline and never empty Start/Awake/Update (folded with thousands of methods) ---
            if (FaccionesNuevas.Activas || Estandarte.HayIconos)
                Fase("Estandarte (colores de facción e iconos)", () =>
                {
                    var tE = typeof(Estandarte);
                    Parchear("Estandarte: colores, rueda y selector (WarbandBannerPanel.EnsureBuilt)", () => typeof(TJ.MainMenu.WarbandBannerPanel), "EnsureBuilt", () => Type.EmptyTypes, null, nameof(Estandarte.TrasEnsureBuilt), null, false, tE);
                    if (Estandarte.HayIconos)
                    {
                        Parchear("Estandarte: icono en la vista previa (WarbandBannerPanel.Refresh)", () => typeof(TJ.MainMenu.WarbandBannerPanel), "Refresh", () => Type.EmptyTypes, null, nameof(Estandarte.TrasRefresh), null, false, tE);
                        // Arreglo del 03-11: ApplyTeamColors (no plegado, 0x270 bytes, lo llama solo SetUp); NUNCA Start, que en
                        // GameAssembly es una función plegada con miles de métodos vacíos
                        // Fix from 03-11: ApplyTeamColors (not folded, 0x270 bytes, called only by SetUp); NEVER Start, which in
                        // GameAssembly is a folded function with thousands of empty methods
                        Parchear("Estandarte: icono en las banderas del jugador (SquadFlagGameObject.ApplyTeamColors)", () => typeof(TJ.SquadFlagGameObject), "ApplyTeamColors", () => Type.EmptyTypes, null, nameof(Estandarte.TrasApplyTeamColors), null, false, tE);
                    }
                    // Revisión del arreglo, hallazgo 2: si los datos del juego ya estaban cargados (sin ReloadData posterior),
                    // las cachés se preparan aquí (carga del plugin, fuera de los enganches de ciclo de vida de Unity)
                    // Fix review, finding 2: if the game data was already loaded (with no later ReloadData),
                    // the caches are prepared here (plugin load, outside Unity's lifecycle hooks)
                    try { var datos = TJ.TabletopTavernData.InstanceIfExists; if (datos != null && datos.SquadAssetsDictionary != null && datos.SquadAssetsDictionary.Count > 0) Estandarte.Preparar(datos); }
                    catch (Exception e) { Aviso("Estandarte: no se pudieron preparar los iconos al cargar: " + e.Message); }
                });

            // --- Interfaz: Colección y Batalla personalizada (facciones nuevas, objetos nuevos y rejillas con desplazamiento;
            // opcionales, InterfazColeccion.cs; cada uno según su interruptor Interfaz.*) ---
            // --- UI: Collection and custom Battle (new factions, new gear and scrolling grids;
            // optional, InterfazColeccion.cs; each one according to its Interfaz.* switch) ---
            Fase("Interfaz de Colección y Batalla personalizada", () =>
            {
                foreach (var p in InterfazColeccion.Parches())
                    Parchear(p.nombre, p.tipo, p.metodo, p.args, p.prefix, p.postfix, p.finalizer, false, typeof(InterfazColeccion));
            });
        }

        // ---------------------------------------------------------------
        // 0.7.5: slot de campaña activo del juego (SaveDataHandler.ActiveSlot: 0 principal, 1 diaria, 2 cooperativo, 3 vista del
        // invitado). Solo se lee en el hilo principal (fuera de él, Saves usa el último leído); aparte y sin inlining: si el
        // juego quita la propiedad, el fallo de JIT sale aquí y lo recoge el try de Saves
        // 0.7.5: the game's active campaign slot (SaveDataHandler.ActiveSlot: 0 main, 1 daily, 2 co-op, 3 guest
        // view). Only read on the main thread (off it, Saves uses the last value read); separate and without inlining: if the
        // game removes the property, the JIT failure surfaces here and is caught by the try in Saves
        // ---------------------------------------------------------------
        internal static int HiloPrincipal = -1;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static int LeerSlotActivo()
        {
            if (Environment.CurrentManagedThreadId != HiloPrincipal) return -2;   // -2 = otro hilo: usar el último leído
                                                                                  // -2 = another thread: use the last value read
            return (int)Memori.SaveData.SaveDataHandler.ActiveSlot;
        }

        // F2: true si se aplicó el alta de los héroes nuevos (sin ella se desactivan las facciones nuevas)
        // F2: true if the registration of the new heroes was applied (without it the new factions are disabled)
        private bool _heroesEnganchados;

        // ---------------------------------------------------------------
        // F2: parches de las razas 40/41 y de los héroes 901-904. El alta de los héroes es imprescindible para las
        // facciones nuevas; el resto son opcionales (si uno falla, se avisa y esa pieza queda como la de serie).
        // F2: patches for races 40/41 and heroes 901-904. Hero registration is essential for the
        // new factions; the rest are optional (if one fails, a warning is logged and that piece stays as in the base game).
        // ---------------------------------------------------------------
        private void AplicarParchesFacciones(Func<Type> tData)
        {
            var tF = typeof(FaccionesNuevas);
            _heroesEnganchados = Parchear("Héroes nuevos (HeroData.LoadFromResourcesAndOverrides)", () => typeof(TJ.HeroData), "LoadFromResourcesAndOverrides", null,
                null, nameof(HeroesNuevos.TrasCargarHeroes), null, false, typeof(HeroesNuevos)) == 1;
            Parchear("Retrato propio de los héroes nuevos (LoadHeroSpriteAsync)", tData, "LoadHeroSpriteAsync", null,
                nameof(HeroesNuevos.AntesDeLoadHeroSpriteAsync), null, null, false, typeof(HeroesNuevos));
            // 0.7.4: hechizos propios de los héroes nuevos en el registro de hechizos del juego tras cada recarga (Load lo
            // vacía). Opcional: si no engancha, el respaldo de HeroesNuevos los pone antes de montar los héroes
            // 0.7.4: custom spells of the new heroes in the game's spell registry after each reload (Load
            // clears it). Optional: if it doesn't hook, the HeroesNuevos fallback sets them before building the heroes
            Parchear("Hechizos propios tras SpellRegistry.Load", () => typeof(TJ.Spells.SpellRegistry), nameof(TJ.Spells.SpellRegistry.Load), () => Type.EmptyTypes,
                null, nameof(Hechizos.TrasLoad), null, false, typeof(Hechizos));
            // 03-11-2026 (revisión externa, hallazgo 4): las bandas propias se vuelven a mirar antes de construir el grimorio
            // 03-11-2026 (external review, finding 4): custom bands are checked again before building the grimoire
            Parchear("Bandas propias del grimorio (WarbandPanel.BuildGrimoire)", () => typeof(TJ.MainMenu.WarbandPanel), "BuildGrimoire", () => Type.EmptyTypes,
                nameof(Hechizos.AntesDeBuildGrimoire), null, null, false, typeof(Hechizos));
            Parchear("Respaldo de GetRaceData para 40/41", tData, "GetRaceData", null, null, nameof(FaccionesNuevas.TrasGetRaceData), null, false, tF);
            Parchear("Razas nuevas en los actos 1-3 (GenerateRaceForMap)", tData, "GenerateRaceForMap", null, null, nameof(FaccionesNuevas.TrasGenerateRaceForMap), null, false, tF);
            Parchear("Razas nuevas en los actos infinitos (GenerateEndlessRace)", tData, "GenerateEndlessRace", null, nameof(FaccionesNuevas.AntesDeGenerateEndlessRace), null, null, false, tF);
            Parchear("Ciudades de las razas nuevas (GenerateTownRace)", () => typeof(TJ.CampaignSaveManager), "GenerateTownRace", null, null, nameof(FaccionesNuevas.TrasGenerateTownRace), null, false, tF);
            Parchear("Región del mapa prestada (GetMapRegion)", () => typeof(TJ.Map.MapThemeManager), "GetMapRegion", null, nameof(FaccionesNuevas.AntesDeGetMapRegion), null, null, false, tF);
            // Arreglos de inlining: ciudad (GenerateTownRace en GenerateTown) y región del nodo (GetMapRegion en MapNode.SetUp)
            // Inlining fixes: town (GenerateTownRace in GenerateTown) and node region (GetMapRegion in MapNode.SetUp)
            foreach (var p in ParchesInlineados.ParchesFacciones())
                Parchear(p.nombre, p.tipo, p.metodo, p.args, p.prefix, p.postfix, p.finalizer, false, typeof(ParchesInlineados), null, p.prioridad);
            foreach (string m in new[] { "SetAudioThemePack", "PlayBattleTheme", "SetGarrisonBattleTheme" })
                Parchear("Música prestada (" + m + ")", () => typeof(Memori.Audio.IAudioRequester), m, null, nameof(FaccionesNuevas.AntesDeTemaMusical), null, null, false, tF);
            // 03-11-2026 (punto 4 de la prueba): diagnóstico del icono del estandarte (solo log). Solo con Diagnostico.AutoPrueba
            // (revisión externa, hallazgo 7): es un parche de diagnóstico en el menú principal
            // 03-11-2026 (test point 4): banner icon diagnostics (log only). Only with Diagnostico.AutoPrueba
            // (external review, finding 7): it is a diagnostic patch on the main menu
            if (CfgAutoPrueba != null && CfgAutoPrueba.Value)
            Parchear("Estandarte: icono en el log (WarbandBannerPanel.Refresh)", () => typeof(TJ.MainMenu.WarbandBannerPanel), "Refresh", () => Type.EmptyTypes, null, nameof(FaccionesNuevas.TrasBannerRefresh), null, false, tF);
            Parchear("Color de las facciones nuevas (GetRaceDisplayColor)", () => typeof(TJ.ColorData), "GetRaceDisplayColor", null, null, nameof(FaccionesNuevas.TrasGetRaceDisplayColor), null, false, tF);
            Parchear("Fondo de carta prestado (SetBackgroundImage)", () => typeof(TJ.SquadDisplayCardPresenter), "SetBackgroundImage", null, null, nameof(FaccionesNuevas.TrasSetBackgroundImage), null, false, tF);
            Parchear("Diezmo del Munitorum (TownPanel.OnEnterTown)", () => typeof(TJ.Town.TownPanel), "OnEnterTown", null, null, nameof(FaccionesNuevas.TrasOnEnterTown), null, false, tF);
            Parchear("Texto del Diezmo en la ciudad (TownPanel.RecruitLimitNote)", () => typeof(TJ.Town.TownPanel), "RecruitLimitNote", null, null, nameof(FaccionesNuevas.TrasRecruitLimitNote), null, false, tF);
            Parchear("Tesorería Imperial (GoldManager.GetTotalInterest)", () => typeof(TJ.GoldManager), "GetTotalInterest", null, null, nameof(FaccionesNuevas.TrasGetTotalInterest), null, false, tF);
            Parchear("Sin temas de taberna para 40/41 (RefreshTavernThemeUnlocks)", () => typeof(Memori.SaveData.SaveDataHandler), "RefreshTavernThemeUnlocks", null, null, nameof(FaccionesNuevas.TrasRefreshTavernThemeUnlocks), null, false, tF);
            if (CfgPantallaHeroes.Value)
            {
                try { PantallaHeroes.RegistrarTipos(); }
                catch (Exception e) { Aviso("No se pudo registrar el componente de recorte de la pantalla de héroes: " + e.Message); }
            }
            // F5: modelo 3D propio de los héroes nuevos (componente vigilante, sin parches: ver ModeloHeroes.cs)
            // F5: custom 3D model for the new heroes (watcher component, no patches: see ModeloHeroes.cs)
            if (CfgModeloHeroes.Value)
            {
                try
                {
                    ModeloHeroes.RegistrarTipos();
                    AddComponent<VigiaModeloHeroes>();
                    int conModelo = 0;
                    foreach (int id in FaccionesNuevas.Heroes.Keys) if (ModeloHeroes.TieneModelo(id)) conModelo++;
                    Info($"Modelos propios de los héroes nuevos: vigilante preparado ({conModelo}/{FaccionesNuevas.Heroes.Count} héroes con modelo en su módulo).");
                }
                catch (Exception e) { Aviso("Modelos propios de los héroes nuevos: no se pudo preparar el vigilante (" + e.Message + "); se ven las figuras prestadas."); }
            }
            if (CfgPantallaHeroes.Value)
                Parchear("Pantalla de héroes ampliada antes de abrirla (PlayPanel.OpenPanel)", () => typeof(TJ.MainMenu.PlayPanel), "OpenPanel", () => Type.EmptyTypes,
                    nameof(PantallaHeroes.AntesDeOpenPanel), null, null, false, typeof(PantallaHeroes));
            // Títulos de facción que caben en su tarjeta (también los de serie; en español uno se montaba sobre otro)
            // Faction titles that fit on their card (including the base ones; in Spanish one overlapped another)
            Parchear("Títulos de facción que caben (CommanderScreenView.LoadRoster)", () => typeof(TJ.MainMenu.CommanderScreenView), "LoadRoster", null,
                null, nameof(PantallaHeroes.TrasLoadRoster), null, false, typeof(PantallaHeroes));
        }

        // ---------------------------------------------------------------
        // Efectos de los módulos (escuadras, munición, stats a distancia; clase EjercitoReforzado): instancia de Harmony
        // propia, independiente de la fábrica de unidades. Se aplica si hay algún módulo instalado (los efectos los decide
        // cada recarga según los módulos activos; así la autoprueba puede simular los desactivados). Solo se quita si
        // fallan SUS firmas: el escalado (prefix y postfix de ReloadData) es imprescindible; las reglas del jugador
        // (LoadRulesFromResourcesAndOverrides) son opcionales. El postfix de ReloadData también lanza la autoprueba.
        // Module effects (squads, ammo, ranged stats; class EjercitoReforzado): own Harmony instance,
        // independent of the unit factory. It is applied if any module is installed (the effects are decided on
        // each reload according to the active modules; this way the self-test can simulate the disabled ones). It is only removed if
        // ITS signatures fail: scaling (prefix and postfix of ReloadData) is essential; the player rules
        // (LoadRulesFromResourcesAndOverrides) are optional. The ReloadData postfix also launches the self-test.
        // ---------------------------------------------------------------
        private void AplicarParchesReforzado()
        {
            _harmonyReforzado = new Harmony(GUID + ".ejercitoreforzado");
            // 0.7.5 (hallazgo 38): tipos y argumentos DIFERIDOS, como en AplicarParches: si una actualización mueve uno de estos
            // tipos, falla solo su parche y no la fase entera
            // 0.7.5 (finding 38): DEFERRED types and arguments, as in AplicarParches: if an update moves one of these
            // types, only its patch fails and not the whole phase
            Func<Type> tData = () => typeof(TJ.TabletopTavernData);
            Func<Type> tBonus = () => typeof(TJ.HeroBonusManager);
            // Postfix con prioridad baja: se ejecuta después del de la fábrica (que da de alta las unidades nuevas)
            // Postfix with low priority: runs after the factory's (which registers the new units)
            int escalado = Parchear("Ejército reforzado: escalado tras ReloadData", tData, "ReloadData", () => Type.EmptyTypes,
                nameof(EjercitoReforzado.AntesDeRecarga), nameof(EjercitoReforzado.TrasRecarga), null, false, typeof(EjercitoReforzado),
                _harmonyReforzado, Priority.Last);
            Parchear("Ejército reforzado: reglas del jugador (HeroBonusManager.LoadRulesFromResourcesAndOverrides)", tBonus,
                    "LoadRulesFromResourcesAndOverrides", null, null, nameof(EjercitoReforzado.TrasCargarReglas), null, false, typeof(EjercitoReforzado),
                    _harmonyReforzado, Priority.Normal);
            // 0.7.1: las reglas del jugador son solo para el héroe activo; se rehacen al cambiar de escena (héroe nuevo) y al
            // decidir el juego el señor de la guerra del enfrentamiento (si usa el mismo héroe, no se ponen)
            // 0.7.1: player rules apply only to the active hero; they are rebuilt on scene change (new hero) and when the
            // game decides the warlord of the matchup (if it uses the same hero, they are not applied)
            Parchear("Ejército reforzado: héroe de la escena (HeroBonusManager.OnGameStateChanged)", tBonus, "OnGameStateChanged", null,
                    null, nameof(EjercitoReforzado.TrasOnGameStateChanged), null, false, typeof(EjercitoReforzado), _harmonyReforzado, Priority.Normal);
            Parchear("Ejército reforzado: señor de la guerra (CampaignSaveManager.SaveEnemyArmy)", () => typeof(TJ.CampaignSaveManager), "SaveEnemyArmy", null,
                    null, nameof(EjercitoReforzado.TrasSaveEnemyArmy), null, false, typeof(EjercitoReforzado), _harmonyReforzado, Priority.Normal);
            // 0.7.4: munición x3 también para las híbridas (el juego no les da bonus de munición por su cuenta). Las dos
            // sobrecargas estáticas: la de 5 argumentos la usa la batalla (EntityWatcher, SquadManager). 0.7.5 (hallazgo 31): la
            // ficha (UnitStatUI) llama a la de INSTANCIA de 3 argumentos, que llama a la de 4 y esta a la de 5; se parchea la de
            // 4 por si la de 5 quedara copiada dentro de ella (inlining). La de instancia no se parchea: si en la prueba la ficha
            // de una híbrida no enseña el x3, añadir aquí el mismo postfix a esa sobrecarga
            // 0.7.4: ammo x3 also for hybrids (the game gives them no ammo bonus on its own). The two
            // static overloads: the 5-argument one is used by battle (EntityWatcher, SquadManager). 0.7.5 (finding 31): the
            // card (UnitStatUI) calls the 3-argument INSTANCE one, which calls the 4-argument one and that calls the 5-argument one; the 4-argument one is patched
            // in case the 5-argument one were copied inside it (inlining). The instance one is not patched: if in testing the hybrid's
            // card doesn't show the x3, add the same postfix to that overload here
            Parchear("Munición de las híbridas (HeroBonusManager.GetHeroStatBonus, 5 argumentos)", tBonus, nameof(TJ.HeroBonusManager.GetHeroStatBonus),
                    () => new[] { typeof(UnitStat), typeof(UnitName), typeof(int), typeof(float), typeof(Race) },
                    null, nameof(EjercitoReforzado.TrasGetHeroStatBonus), null, false, typeof(EjercitoReforzado), _harmonyReforzado, Priority.Normal);
            Parchear("Munición de las híbridas (HeroBonusManager.GetHeroStatBonus, 4 argumentos)", tBonus, nameof(TJ.HeroBonusManager.GetHeroStatBonus),
                    () => new[] { typeof(UnitStat), typeof(UnitName), typeof(int), typeof(float) },
                    null, nameof(EjercitoReforzado.TrasGetHeroStatBonus4), null, false, typeof(EjercitoReforzado), _harmonyReforzado, Priority.Normal);
            // 0.7.6: módulo «Impertérritos» (MoralJugador.cs): moral de las escuadras del jugador en batalla
            // (EntityWatcher.Update, mensaje de Unity) y en la resolución automática (AutoResolveSimulation.UpdateMorale,
            // privado, dos llamadas reales desde Tick según el xref del 03-11). Cada parche mira además en tiempo de ejecución si
            // el efecto está activo. La autoprueba 36 solo los comprueba con el módulo aplicado.
            // 2.ª ronda de la revisión externa: SOLO si Impertérritos está aplicado al arrancar (sin el módulo, ningún parche
            // nuevo de moral) y UpdateMorale con su firma exacta del volcado 1.11.1 (dos arrays, punteros, y un bool)
            // Revisión externa del 03-11 (hallazgo 3): en su propia instancia de Harmony, para que no se quiten con el resto
            // de Ejército reforzado si falla el escalado de ReloadData
            // 0.7.6: module «Impertérritos» (MoralJugador.cs): morale of the player's squads in battle
            // (EntityWatcher.Update, Unity message) and in auto-resolve (AutoResolveSimulation.UpdateMorale,
            // private, two real calls from Tick according to the 03-11 xref). Each patch also checks at runtime whether
            // the effect is active. Self-test 36 only checks them with the module applied.
            // 2nd round of the external review: ONLY if Impertérritos is applied at startup (without the module, no new
            // morale patches) and UpdateMorale with its exact signature from the 1.11.1 dump (two arrays, pointers, and a bool)
            // External review of 03-11 (finding 3): in its own Harmony instance, so they are not removed along with the rest
            // of Ejército reforzado if the ReloadData scaling fails
            if (MoralJugador.Activo)
            {
                var harmonyMoral = new Harmony(GUID + ".moral");
                Parchear("Moral del jugador: batalla (EntityWatcher.Update)", () => typeof(TJ.EntityWatcher), "Update", () => Type.EmptyTypes,
                        null, nameof(MoralJugador.TrasEntityWatcherUpdate), null, false, typeof(MoralJugador), harmonyMoral, Priority.Normal);
                Parchear("Moral del jugador: batalla nueva (EntityWatcher.SetUp)", () => typeof(TJ.EntityWatcher), "SetUp", () => Type.EmptyTypes,
                        null, nameof(MoralJugador.TrasEntityWatcherSetUp), null, false, typeof(MoralJugador), harmonyMoral, Priority.Normal);
                Parchear("Moral del jugador: resolución automática (AutoResolveSimulation.UpdateMorale)",
                        () => TipoPorNombre("TJ.Engagement.AutoResolveSimulation, TabletopTavern.Core"), "UpdateMorale", () =>
                        {
                            var arr = typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<>).MakeGenericType(TipoPorNombre("TJ.Engagement.AutoResolveSquad, TabletopTavern.Core"));
                            return new[] { arr, arr, typeof(bool) };
                        },
                        nameof(MoralJugador.AntesDeUpdateMorale), null, null, false, typeof(MoralJugador), harmonyMoral, Priority.Normal);
            }
            if (escalado == 0)
            {
                try { _harmonyReforzado.UnpatchSelf(); } catch (Exception e) { Error("Error al quitar los parches de Ejército reforzado: " + e.Message); }
                EjercitoReforzado.Operativo = false;
                Error("Ejército reforzado DESACTIVADO: no engancha el escalado de ReloadData (¿cambió la firma tras un parche del juego?).");
                return;
            }
            EjercitoReforzado.Operativo = true;
            Info($"Ejército reforzado operativo (parches propios, independientes de la fábrica de unidades{(Operativo ? "" : ", que está desactivada")}).");
        }

        // ---------------------------------------------------------------
        // Clasificaciones online (0.7.1): instancia de Harmony propia, que se aplica con cualquier módulo instalado y no se
        // quita con las demás. El prefix (Saves.AntesDeSubmit) decide en cada envío: con algún módulo de Zero aplicado en la
        // sesión, no se envía. Si no engancha, se avisa (las partidas modificadas podrían llegar a las clasificaciones).
        // Revisión del 18-10 (M-2): también se aplica sin módulos instalados si queda el registro de saves, y el prefix
        // bloquea además las partidas que el registro apunta como jugadas con módulos (RegistroGuardias.BloquearOnline).
        // Online leaderboards (0.7.1): own Harmony instance, applied with any module installed and not
        // removed along with the others. The prefix (Saves.AntesDeSubmit) decides on each submission: with any Zero module applied in the
        // session, it is not sent. If it doesn't hook, a warning is logged (modified runs could reach the leaderboards).
        // Review of 18-10 (M-2): also applied without installed modules if the saves registry remains, and the prefix
        // also blocks the runs that the registry marks as played with modules (RegistroGuardias.BloquearOnline).
        // ---------------------------------------------------------------
        private void AplicarParcheClasificaciones()
        {
            _harmonyClasificaciones = new Harmony(GUID + ".clasificaciones");
            const string nombre = "Bloqueo de clasificaciones (LeaderboardClient.Submit)";
            // Juego 1.11.1: tipo diferido (se resuelve dentro del try de Parchear). Así, si una actualización mueve
            // LeaderboardClient, falla solo este parche (con su aviso) y la telemetría de abajo se intenta igual
            // Game 1.11.1: deferred type (resolved inside the try of Parchear). This way, if an update moves
            // LeaderboardClient, only this patch fails (with its warning) and the telemetry below is still attempted
            int ok = Parchear(nombre, () => typeof(TabletopTavern.Leaderboards.LeaderboardClient), Omisiones.MetodoClasificaciones, null,
                nameof(Saves.AntesDeSubmit), null, null, false, typeof(Saves), _harmonyClasificaciones);
            // Omitido no es aplicado: sin el prefix no hay bloqueo. Solo se puede omitir sin módulos aplicados o con
            // Clasificaciones.Bloquear = false (Omisiones.SePuedeOmitir), pero se trata igual que un fallo y se dice claro.
            // Skipped is not applied: without the prefix there is no blocking. It can only be skipped with no modules applied or with
            // Clasificaciones.Bloquear = false (Omisiones.SePuedeOmitir), but it is treated the same as a failure and reported clearly.
            if (_omitidos.Contains(nombre))
            {
                string porque = Nucleo.PartidaModificada ? "hay módulos de Zero aplicados y Clasificaciones.Bloquear = false" : "no hay módulos de Zero aplicados";
                string texto = $"Bloqueo de las clasificaciones online OMITIDO por Diagnostico.OmitirParches ({porque}): en esta sesión no hay bloqueo, las partidas se envían.";
                _fallosOpcionales.Add(nombre + ": omitido por configuración (sin bloqueo de clasificaciones)");
                Aviso(texto);
            }
            else if (ok == 0) Error("No engancha el bloqueo de las clasificaciones: con módulos activos, las partidas modificadas podrían enviarse a las clasificaciones online.");
            else if (Nucleo.PartidaModificada && CfgBloquearClasificaciones.Value) Info("Clasificaciones online: en esta sesión no se envían partidas (hay módulos de Zero aplicados).");
            // 0.7.4 (decisión de Zero, 01-11-2026): sin soporte en cooperativo ni versus (hechizos propios 19xx, unidades 9xx y
            // efectos que el otro jugador no tiene). Revisión de la 0.7.4 (ronda 2): se corta en la ENTRADA de cada camino
            // (firmas del interop 1.11.1, TJ.Versus.Net): la cola de la partida rápida y clasificatoria, las salas propia y de
            // cooperativo, unirse a una sala, la invitación de Steam y volver a una partida. VersusLobby.StartLink (donde se abre
            // el enlace con el otro jugador) queda como última red y, si bloquea, sale de la sala. Lo que enseña la interfaz con
            // cada bloqueo no se ha visto en el juego (no hay código público del versus): por probar. Todos opcionales: si alguno
            // no engancha, se avisa y lo demás sigue. Las condiciones están en Saves.BloquearOnline
            // 0.7.4 (Zero's decision, 01-11-2026): no support in co-op or versus (custom spells 19xx, units 9xx and
            // effects that the other player doesn't have). Review of 0.7.4 (round 2): it is cut off at the ENTRY of each path
            // (interop 1.11.1 signatures, TJ.Versus.Net): the quick match and ranked queue, the own and co-op
            // rooms, joining a room, the Steam invitation and returning to a run. VersusLobby.StartLink (where the link
            // with the other player is opened) remains as a last safety net and, if it blocks, leaves the room. What the interface shows with
            // each block has not been seen in the game (there is no public versus code): to be tested. All optional: if one
            // doesn't hook, a warning is given and the rest continues. The conditions are in Saves.BloquearOnline
            Parchear("Bloqueo de multijugador: partida rápida y clasificatoria (QuickBattleQueue.Start)", () => typeof(TJ.Versus.Net.QuickBattleQueue), "Start",
                () => new[] { typeof(bool) }, nameof(Saves.AntesDeQuickBattleStart), null, null, false, typeof(Saves), _harmonyClasificaciones);
            Parchear("Bloqueo de multijugador: sala propia (VersusLobby.HostCustomAsync)", () => typeof(TJ.Versus.Net.VersusLobby), "HostCustomAsync", null,
                nameof(Saves.AntesDeHostCustom), null, null, false, typeof(Saves), _harmonyClasificaciones);
            // 0.7.5 (hallazgo 33): HostCoopAsync está copiado (inlining) dentro de CoopSession.<InviteFriend>d__72.MoveNext en el
            // juego 1.11.1 (comprobado en GameAssembly.dll), así que su prefix no salta al invitar. Se corta antes, en
            // CoopSession.InviteFriend (estático, void, sin argumentos); el de HostCoopAsync se mantiene por si una versión
            // futura deja de copiarlo
            // 0.7.5 (finding 33): HostCoopAsync is copied (inlined) inside CoopSession.<InviteFriend>d__72.MoveNext in game
            // 1.11.1 (checked in GameAssembly.dll), so its prefix doesn't trigger when inviting. It is cut off earlier, in
            // CoopSession.InviteFriend (static, void, no arguments); the one on HostCoopAsync is kept in case a future
            // version stops inlining it
            Parchear("Bloqueo de multijugador: invitar al cooperativo (CoopSession.InviteFriend)", () => typeof(TJ.Versus.Net.CoopSession), "InviteFriend", () => Type.EmptyTypes,
                nameof(Saves.AntesDeInviteFriend), null, null, false, typeof(Saves), _harmonyClasificaciones);
            Parchear("Bloqueo de multijugador: sala de cooperativo (VersusLobby.HostCoopAsync)", () => typeof(TJ.Versus.Net.VersusLobby), "HostCoopAsync", null,
                nameof(Saves.AntesDeHostCoop), null, null, false, typeof(Saves), _harmonyClasificaciones);
            Parchear("Bloqueo de multijugador: unirse a una sala (VersusLobby.JoinAsync)", () => typeof(TJ.Versus.Net.VersusLobby), "JoinAsync", null,
                nameof(Saves.AntesDeJoin), null, null, false, typeof(Saves), _harmonyClasificaciones);
            Parchear("Bloqueo de multijugador: invitación de Steam (VersusLobby.OnJoinRequested)", () => typeof(TJ.Versus.Net.VersusLobby), "OnJoinRequested", null,
                nameof(Saves.AntesDeOnJoinRequested), null, null, false, typeof(Saves), _harmonyClasificaciones);
            Parchear("Bloqueo de multijugador: volver a una partida (VersusRejoin.Resume)", () => typeof(TJ.Versus.Net.VersusRejoin), "Resume", null,
                nameof(Saves.AntesDeRejoinResume), null, null, false, typeof(Saves), _harmonyClasificaciones);
            Parchear("Bloqueo de multijugador: última red (VersusLobby.StartLink)", () => typeof(TJ.Versus.Net.VersusLobby), "StartLink", null,
                nameof(Saves.AntesDeStartLink), null, null, false, typeof(Saves), _harmonyClasificaciones);
            // 0.7.5 (hallazgo 36): las dos rutas de arranque por línea de comandos (pruebas de desarrollo del juego) abren la cola
            // o el enlace sin pasar por las entradas de arriba (VersusNet.BootFromCommandLine llama a VersusNet.Begin directamente)
            // 0.7.5 (finding 36): the two command-line startup routes (game development tests) open the queue
            // or the link without going through the entries above (VersusNet.BootFromCommandLine calls VersusNet.Begin directly)
            Parchear("Bloqueo de multijugador: cola por línea de comandos (QuickBattleQueue.BootFromCommandLine)", () => typeof(TJ.Versus.Net.QuickBattleQueue), "BootFromCommandLine", () => Type.EmptyTypes,
                nameof(Saves.AntesDeBootColaRapida), null, null, false, typeof(Saves), _harmonyClasificaciones);
            Parchear("Bloqueo de multijugador: enlace por línea de comandos (VersusNet.BootFromCommandLine)", () => typeof(TJ.Versus.Net.VersusNet), "BootFromCommandLine", () => Type.EmptyTypes,
                nameof(Saves.AntesDeBootVersusNet), null, null, false, typeof(Saves), _harmonyClasificaciones);
            // Juego 1.11.1: la telemetría va en su propia fase. Con la 0.7.2, un tipo movido (HttpAnalyticsBackend) hizo que el
            // JIT de AplicarParchesTelemetria lanzara TypeLoadException al entrar, por encima de su try, y cayó Load() entero
            // Game 1.11.1: telemetry goes in its own phase. With 0.7.2, a moved type (HttpAnalyticsBackend) made the
            // JIT of AplicarParchesTelemetria throw TypeLoadException on entry, above its try, and the whole Load() failed
            Fase("Telemetría", AplicarParchesTelemetria);
        }

        // ---------------------------------------------------------------
        // Telemetría del juego (0.7.2, decisión de Zero): misma instancia que las clasificaciones (se aplica con cualquier
        // módulo instalado y no se quita con las demás). Métodos que de verdad apuntan o envían datos, comprobados en el
        // interop (Memori.Analytics.dll; hasta el juego 1.10.17, HttpAnalyticsBackend estaba en TabletopTavern.Core.dll):
        //   - AnalyticsService.Record (estático): punto único de todos los eventos (GameEventTracker y ExceptionTracker).
        //   - HttpAnalyticsBackend.Record: el backend real, por si algo lo llama sin pasar por AnalyticsService.
        //   - HttpAnalyticsBackend.Send: el POST al servidor (la cola guardada en disco de sesiones anteriores).
        // Los dos Record llevan Saves.AntesDeEventoTelemetria (recibe el nombre del evento) y Send, Saves.AntesDeEnvioTelemetria.
        // El informe de «Report a bug» en sí (va a Discord, no a la telemetría) no se toca nunca. Su evento de telemetría
        // (bugReportSubmitted) y el de excepciones (exceptionSeen) solo se bloquean con módulos aplicados; sin módulos pasan
        // aunque la partida en curso se jugara con ellos (revisión externa del 18-10, P-B). Ninguno de los tres objetivos se pierde
        // por inlining (revision_inlining.md, sección online). Cada enganche es opcional y, si algo falla, se avisa y el juego
        // sigue.
        // Juego 1.11.1 (01-11-2026): HttpAnalyticsBackend pasó de TabletopTavern.Analytics (TabletopTavern.Core.dll) a
        // Memori.Analytics (Memori.Analytics.dll), con los mismos Record y Send. Ya no se nombra con typeof: los tipos se
        // buscan por nombre (TipoPorNombre, con la ubicación nueva primero y la vieja de respaldo) dentro del try de cada
        // parche, así que si se vuelve a mover sale un «Parche no aplicado» y lo demás sigue. Los métodos siguen con nameof
        // (comprobado al compilar contra el interop; no crea ninguna referencia al tipo en ejecución).
        // Game telemetry (0.7.2, Zero's decision): same instance as the leaderboards (applied with any
        // module installed and not removed along with the others). Methods that really target or send data, checked in the
        // interop (Memori.Analytics.dll; up to game 1.10.17, HttpAnalyticsBackend was in TabletopTavern.Core.dll):
        //   - AnalyticsService.Record (static): single point for all events (GameEventTracker and ExceptionTracker).
        //   - HttpAnalyticsBackend.Record: the real backend, in case something calls it without going through AnalyticsService.
        //   - HttpAnalyticsBackend.Send: the POST to the server (the queue saved on disk from earlier sessions).
        // Both Record methods carry Saves.AntesDeEventoTelemetria (it receives the event name) and Send carries Saves.AntesDeEnvioTelemetria.
        // The «Report a bug» report itself (it goes to Discord, not to telemetry) is never touched. Its telemetry event
        // (bugReportSubmitted) and the exceptions one (exceptionSeen) are only blocked with modules applied; without modules they pass
        // even if the current run was played with them (external review of 18-10, P-B). None of the three targets is lost
        // to inlining (revision_inlining.md, online section). Each hook is optional and, if something fails, a warning is logged and the game
        // continues.
        // Game 1.11.1 (01-11-2026): HttpAnalyticsBackend moved from TabletopTavern.Analytics (TabletopTavern.Core.dll) to
        // Memori.Analytics (Memori.Analytics.dll), with the same Record and Send. It is no longer named with typeof: the types are
        // looked up by name (TipoPorNombre, with the new location first and the old one as a fallback) inside the try of each
        // patch, so if it moves again a «Parche no aplicado» appears and the rest continues. The methods still use nameof
        // (checked when compiling against the interop; it creates no reference to the type at runtime).
        // ---------------------------------------------------------------
        // Nombres candidatos (con su ensamblado) de los tipos de la telemetría: primero el del juego 1.11.1, después el de 1.10.17
        // Candidate names (with their assembly) of the telemetry types: first the one from game 1.11.1, then the one from 1.10.17
        private const string TipoAnalyticsService = "Memori.Analytics.AnalyticsService, Memori.Analytics";
        private const string TipoHttpAnalyticsBackend = "Memori.Analytics.HttpAnalyticsBackend, Memori.Analytics|TabletopTavern.Analytics.HttpAnalyticsBackend, TabletopTavern.Core";

        // Tipo del juego por nombre (LogicaCarga.ResolverTipo): Type.GetType con el ensamblado (dispara el resolvedor del
        // interop de BepInEx aunque esa DLL aún no esté cargada) y, si no, AccessTools.TypeByName (ensamblados ya cargados)
        // Game type by name (LogicaCarga.ResolverTipo): Type.GetType with the assembly (triggers the BepInEx interop
        // resolver even if that DLL is not yet loaded) and, failing that, AccessTools.TypeByName (already loaded assemblies)
        internal static Type TipoPorNombre(string candidatos) =>
            LogicaCarga.ResolverTipo(candidatos, n => Type.GetType(n, false), n => AccessTools.TypeByName(n));

        private void AplicarParchesTelemetria()
        {
            try
            {
                var objetivos = new (string nombre, string tipo, string metodo, string prefix)[]
                {
                    ("Telemetría: eventos (AnalyticsService.Record)", TipoAnalyticsService, nameof(Memori.Analytics.AnalyticsService.Record), nameof(Saves.AntesDeEventoTelemetria)),
                    ("Telemetría: backend (HttpAnalyticsBackend.Record)", TipoHttpAnalyticsBackend, nameof(Memori.Analytics.HttpAnalyticsBackend.Record), nameof(Saves.AntesDeEventoTelemetria)),
                    ("Telemetría: envío al servidor (HttpAnalyticsBackend.Send)", TipoHttpAnalyticsBackend, nameof(Memori.Analytics.HttpAnalyticsBackend.Send), nameof(Saves.AntesDeEnvioTelemetria)),
                };
                int puestos = 0, eventos = 0;
                foreach (var (nombre, tipo, metodo, prefix) in objetivos)
                {
                    // Un omitido por Diagnostico.OmitirParches cuenta como puesto en Parchear, pero no filtra nada.
                    // El tipo se resuelve dentro del try de Parchear (tipo diferido)
                    // One skipped by Diagnostico.OmitirParches counts as applied in Parchear, but filters nothing.
                    // The type is resolved inside the try of Parchear (deferred type)
                    string candidatos = tipo;
                    int ok = Parchear(nombre, () => TipoPorNombre(candidatos), metodo, null, prefix, null, null, false, typeof(Saves), _harmonyClasificaciones);
                    puestos += ok;
                    if (ok == 1 && prefix == nameof(Saves.AntesDeEventoTelemetria) && !_omitidos.Contains(nombre)) eventos++;
                }
                // El envío de la cola sin módulos solo se suelta si están puestos LOS DOS filtros de apuntar (revisión externa
                // del diff): con uno solo, lo que entrara por el otro camino llegaría a la cola sin filtrar. Si falta alguno,
                // el envío vuelve a la regla anterior (RegistroGuardias.BloquearEnvioTelemetria)
                // The queue submission without modules is only released if BOTH aiming filters are in place (external review
                // of the diff): with only one, anything coming in through the other path would reach the queue unfiltered. If either is missing,
                // the submission goes back to the previous rule (RegistroGuardias.BloquearEnvioTelemetria)
                Saves.FiltroDeEventosTelemetria = eventos == 2;
                if (puestos == 0) Error("No engancha ningún bloqueo de la telemetría: con módulos activos, el juego podría enviar eventos con contenido nuevo.");
                else if (Nucleo.PartidaModificada && CfgBloquearTelemetria.Value) Info($"Telemetría del juego: bloqueada en esta sesión ({puestos} de {objetivos.Length} enganches; hay módulos de Zero aplicados).");
            }
            catch (Exception e)
            {
                // Falla en blando: sin bloqueo de la telemetría, pero el juego y el resto del plugin siguen
                // Fails softly: no telemetry blocking, but the game and the rest of the plugin continue
                _fallosOpcionales.Add("Telemetría: " + e.Message);
                Aviso("No se pudo preparar el bloqueo de la telemetría: " + e.Message);
            }
        }

        // ---------------------------------------------------------------
        // Objetos nuevos (0.6.0): instancia de Harmony propia. Imprescindibles para los objetos: GetGear (sin él saldrían como
        // None) y la carga de reglas de héroe (sin ella no darían bonus); si falla uno, se quitan todos los de los objetos y el
        // resto del plugin sigue. Los demás son opcionales (sorteo, contexto, puntos de sincronización).
        // New gear (0.6.0): own Harmony instance. Essential for the gear: GetGear (without it they would show up as
        // None) and the loading of hero rules (without it they wouldn't give a bonus); if one fails, all the gear ones are removed and the
        // rest of the plugin continues. The others are optional (draw, context, sync points).
        // ---------------------------------------------------------------
        private void AplicarParchesObjetos()
        {
            if (Objetos.Numero == 0) { Info("Objetos nuevos: ningún módulo activo trae objetos; no se aplican sus parches."); Objetos.ActivosEnCache = false; return; }
            _harmonyObjetos = new Harmony(GUID + ".objetos");
            var tO = typeof(Objetos);
            // 0.7.5 (hallazgo 38): tipos y argumentos del juego DIFERIDOS (si una actualización mueve uno, falla solo su parche)
            // 0.7.5 (finding 38): DEFERRED game types and arguments (if an update moves one, only its patch fails)
            Func<Type> tGearData = () => typeof(TJ.GearData);
            Func<Type> tGearManager = () => typeof(TJ.GearManager);
            int imprescindibles = 0;
            imprescindibles += Parchear("Objetos: datos (GearData.GetGear)", tGearData, "GetGear", () => new[] { typeof(TJ.GearID) },
                null, nameof(Objetos.TrasGetGear), null, false, tO, _harmonyObjetos);
            imprescindibles += Parchear("Objetos: efectos (HeroBonusManager.LoadRulesFromResourcesAndOverrides)", () => typeof(TJ.HeroBonusManager),
                "LoadRulesFromResourcesAndOverrides", null, null, nameof(Objetos.TrasCargarReglas), null, false, tO, _harmonyObjetos);
            if (imprescindibles < 2)
            {
                try { _harmonyObjetos.UnpatchSelf(); } catch (Exception e) { Error("Error al quitar los parches de los objetos: " + e.Message); }
                Objetos.Operativo = false;
                Objetos.ActivosEnCache = false;
                Error("Objetos nuevos DESACTIVADOS: no engancha GetGear o la carga de reglas de héroe. El resto del plugin sigue; los saves los guardan como su respaldo.");
                return;
            }
            Objetos.Operativo = true;
            Func<Type> tCsm = () => typeof(TJ.CampaignSaveManager);
            Parchear("Objetos: sorteo (CampaignSaveManager.DrawRandomGear)", tCsm, "DrawRandomGear", null, null, nameof(Objetos.TrasDrawRandomGear), null, false, tO, _harmonyObjetos);
            Parchear("Objetos: contexto del saqueo del campo (EngagementPanel.AddLootBattlefield)", () => typeof(TJ.Engagement.EngagementPanel), "AddLootBattlefield", () => Type.EmptyTypes,
                nameof(Objetos.AntesDeBotinEnemigo), null, nameof(Objetos.FinBotinEnemigo), false, tO, _harmonyObjetos);
            Parchear("Objetos: contexto de Grave Robbers (CampaignSaveManager.GraveRobbers)", tCsm, "GraveRobbers", () => Type.EmptyTypes,
                nameof(Objetos.AntesDeBotinEnemigo), null, nameof(Objetos.FinBotinEnemigo), false, tO, _harmonyObjetos);
            Parchear("Objetos: botín de la ciudad con su raza (CampaignSaveManager.GenerateTown)", tCsm, "GenerateTown", null,
                nameof(Objetos.AntesDeGenerateTown), nameof(Objetos.TrasGenerateTown), nameof(Objetos.FinGenerateTown), false, tO, _harmonyObjetos);
            Parchear("Objetos: héroe y escena (HeroBonusManager.OnGameStateChanged)", () => typeof(TJ.HeroBonusManager), "OnGameStateChanged", null,
                null, nameof(Objetos.TrasOnGameStateChanged), null, false, tO, _harmonyObjetos);
            Parchear("Objetos: gear cargado (GearManager.LoadAllGear)", tGearManager, "LoadAllGear", () => Type.EmptyTypes,
                null, nameof(Objetos.TrasLoadAllGear), null, false, tO, _harmonyObjetos);
            Parchear("Objetos: gear ganado (GearManager.AquireGear)", tGearManager, "AquireGear", null,
                null, nameof(Objetos.TrasCambioGear), null, false, tO, _harmonyObjetos);
            Parchear("Objetos: gear perdido (GearManager.UnAquireGear)", tGearManager, "UnAquireGear", null,
                null, nameof(Objetos.TrasCambioGear), null, false, tO, _harmonyObjetos);
            Parchear("Objetos: herrería (CampaignSaveManager.ReforgeGear)", tCsm, "ReforgeGear", null,
                null, nameof(Objetos.TrasReforgeGear), null, false, tO, _harmonyObjetos);
            // 0.7.2: armería del equipo inicial. GetGearIDs solo cambia dentro de un contexto de lista ampliada: el que abre Build
            // y los de la Colección y su contador del menú (InterfazColeccion.cs). El logro de objetos sigue con los 40 de serie
            // (no se evalúa dentro de esos contextos y LogrosFiltrados suspende cualquiera que quede abierto). nameof comprueba
            // al compilar que Build existe en el interop.
            // 0.7.2: starting gear armory. GetGearIDs only changes within an expanded list context: the one opened by Build
            // and those of the Collection and its menu counter (InterfazColeccion.cs). The gear achievement stays with the base 40
            // (it is not evaluated within those contexts and LogrosFiltrados suspends any that remains open). nameof checks
            // at compile time that Build exists in the interop.
            Parchear("Objetos: lista de gear ampliada (GearData.GetGearIDs)", tGearData, nameof(TJ.GearData.GetGearIDs), () => Type.EmptyTypes,
                null, nameof(Objetos.TrasGetGearIDs), null, false, tO, _harmonyObjetos);
            Parchear("Objetos: armería del equipo inicial (WarbandGearArmory.Build)", () => typeof(TJ.MainMenu.WarbandGearArmory), nameof(TJ.MainMenu.WarbandGearArmory.Build), () => Type.EmptyTypes,
                nameof(Objetos.AntesDeArmeria), nameof(Objetos.TrasArmeria), nameof(Objetos.FinArmeria), false, tO, _harmonyObjetos);
            // 0.7.2 (revisión): contador del botón Colección del menú con la lista de objetos ampliada, con o sin
            // Interfaz.Coleccion (antes dependía de ese interruptor y, apagado, marcaba más recogidos que objetos: 45/40).
            // Las unidades de las razas nuevas se suman en el postfix de la interfaz (InterfazColeccion.Parches)
            // 0.7.2 (review): Collection button counter in the menu with the expanded gear list, with or without
            // Interfaz.Coleccion (before it depended on that switch and, when off, showed more collected than gear: 45/40).
            // The units of the new races are added in the interface postfix (InterfazColeccion.Parches)
            Parchear("Objetos: lista ampliada en el contador del botón Colección (MainMenu.RefreshCollectionButtonState)", () => typeof(TJ.MainMenu.MainMenu),
                nameof(TJ.MainMenu.MainMenu.RefreshCollectionButtonState), () => Type.EmptyTypes,
                nameof(InterfazColeccion.AntesDeListaAmpliada), null, nameof(InterfazColeccion.FinListaAmpliada), false, typeof(InterfazColeccion), _harmonyObjetos);
            Info($"Objetos nuevos operativos: {Objetos.Numero} objetos de los módulos activos (parches propios).");
        }

        // ---------------------------------------------------------------
        // Aplica un parche y clasifica el resultado. Devuelve 1 si se aplicó, 0 si no.
        // (Por defecto con la instancia principal; «Ejército reforzado» pasa la suya y una prioridad.)
        // Esta sobrecarga recibe el tipo ya resuelto (typeof en el llamador): se mantiene para los parches directos de este
        // fichero y pasa a la de tipo diferido.
        // Applies a patch and classifies the result. Returns 1 if it was applied, 0 if not.
        // (By default with the main instance; «Ejército reforzado» passes its own and a priority.)
        // This overload receives the already resolved type (typeof in the caller): it is kept for the direct patches in this
        // file and delegates to the deferred-type one.
        // ---------------------------------------------------------------
        private int Parchear(string nombre, Type tipo, string metodo, Type[] argumentos,
                             string prefix, string postfix, string finalizer, bool critico, Type claseParches,
                             Harmony instancia = null, int prioridad = -1)
            => Parchear(nombre, () => tipo, metodo, argumentos == null ? (Func<Type[]>)null : () => argumentos,
                        prefix, postfix, finalizer, critico, claseParches, instancia, prioridad);

        // ---------------------------------------------------------------
        // Juego 1.11.1: misma función con el tipo y los argumentos DIFERIDOS (Func). Las listas de parches de los demás
        // ficheros (LogrosFiltrados, ParchesInlineados, CampanaHeroes, AccionesHeroes, InterfazColeccion y la telemetría) los
        // pasan así: cada «() => typeof(X)» es un método aparte que el JIT solo compila al llamarlo aquí, dentro del try. Si
        // el juego quita o mueve X, falla ESE parche («Parche no aplicado», o «Parche crítico FALLIDO» si es crítico) y no la
        // lista entera ni Load(). nameof y typeof siguen comprobándose al compilar contra el interop.
        // Game 1.11.1: same function with the type and arguments DEFERRED (Func). The patch lists in the other
        // files (LogrosFiltrados, ParchesInlineados, CampanaHeroes, AccionesHeroes, InterfazColeccion and the telemetry)
        // pass them this way: each «() => typeof(X)» is a separate method that the JIT only compiles when it is called here, inside the try. If
        // the game removes or moves X, THAT patch fails («Parche no aplicado», or «Parche crítico FALLIDO» if it is critical) and not the
        // whole list or Load(). nameof and typeof are still checked at compile time against the interop.
        // ---------------------------------------------------------------
        private int Parchear(string nombre, Func<Type> tipoDiferido, string metodo, Func<Type[]> argumentosDiferidos,
                             string prefix, string postfix, string finalizer, bool critico, Type claseParches,
                             Harmony instancia = null, int prioridad = -1)
        {
            Type tipo = null;
            // Diagnóstico: parches omitidos a mano (Diagnostico.OmitirParches, nombres de método separados por comas). Un
            // omitido cuenta como 1 para no disparar la retirada de los demás, pero se apunta en _omitidos para quien necesite
            // distinguirlo de uno aplicado (el bloqueo de clasificaciones). Ese bloqueo no se omite con módulos aplicados.
            // 0.7.5 (hallazgo 37): tampoco se omiten los bloqueos en línea (clasificaciones, multijugador, telemetría) con
            // módulos aplicados y su bloqueo encendido, ni el filtro de logros con Logros.Filtrar = true
            // Diagnostics: patches skipped by hand (Diagnostico.OmitirParches, method names separated by commas). A
            // skipped one counts as 1 so it does not trigger the removal of the others, but it is recorded in _omitidos for whoever needs to
            // tell it apart from an applied one (the leaderboard block). That block is not skipped when modules are applied.
            // 0.7.5 (finding 37): the inline blocks (leaderboards, multiplayer, telemetry) are not skipped either when
            // modules are applied and their block is enabled, nor is the achievements filter with Logros.Filtrar = true
            if (Omisiones.EnLista(CfgOmitir?.Value, metodo))
            {
                var proteccion = instancia != null && instancia == _harmonyClasificaciones ? Omisiones.Proteccion.EnLinea
                               : instancia != null && instancia == _harmonyLogros ? Omisiones.Proteccion.Logros
                               : Omisiones.Proteccion.Ninguna;
                // Qué bloqueo protege este parche en línea: el de las clasificaciones, el de la telemetría o el del multijugador
                // Which online block this patch protects: the leaderboards one, the telemetry one or the multiplayer one
                bool bloqueo = metodo == Omisiones.MetodoClasificaciones ? CfgBloquearClasificaciones?.Value ?? true
                             : metodo == "Record" || metodo == "Send" ? CfgBloquearTelemetria?.Value ?? true
                             : CfgBloquearMultijugador?.Value ?? true;
                if (!Omisiones.SePuedeOmitir(proteccion, Nucleo.PartidaModificada, bloqueo, CfgLogrosFiltrar?.Value ?? true))
                    Aviso($"Diagnostico.OmitirParches pide omitir «{metodo}» ({nombre}), pero NO se omite: " +
                          (proteccion == Omisiones.Proteccion.Logros
                              ? "es del filtro de logros (un logro de Steam concedido en falso no se puede quitar). Para quitarlo, pon Logros.Filtrar = false."
                              : "es un bloqueo en línea y hay módulos de Zero aplicados. Para quitarlo, apaga su bloqueo en la configuración (sin soporte)."));
                else
                {
                    _omitidos.Add(nombre);
                    MetodosOmitidos.Add(metodo);
                    if (proteccion == Omisiones.Proteccion.Ninguna) Aviso($"Parche omitido por configuración: {nombre}");
                    else Error($"Parche de PROTECCIÓN omitido por configuración: {nombre} (sin módulos aplicados o con su bloqueo apagado; revisa Diagnostico.OmitirParches).");
                    return 1;
                }
            }
            // Juego 1.11.1 (revisión): resolver tipo, argumentos y método y aplicar el parche lo hace LogicaCarga.AplicarUno (el
            // mismo código que prueba pruebas\PruebasCarga.cs); aquí solo se le pasa cómo buscar con Harmony y cómo parchear
            // Game 1.11.1 (review): resolving the type, arguments and method and applying the patch is done by LogicaCarga.AplicarUno (the
            // same code that pruebas\PruebasCarga.cs tests); here it is only passed how to search with Harmony and how to patch
            HarmonyMethod Hm(string n) => n == null ? null
                : prioridad < 0 ? new HarmonyMethod(AccessTools.Method(claseParches, n))
                : new HarmonyMethod(AccessTools.Method(claseParches, n), prioridad);
            Exception error = LogicaCarga.AplicarUno(tipoDiferido, metodo, argumentosDiferidos,
                (t, m, a) => a == null ? AccessTools.Method(t, m) : AccessTools.Method(t, m, a),
                original => (instancia ?? _harmony).Patch(original, prefix: Hm(prefix), postfix: Hm(postfix), finalizer: Hm(finalizer)),
                out tipo);
            if (error == null)
            {
                _aplicados++;
                Info($"Parche aplicado: {nombre} -> {tipo.Name}.{metodo}");
                return 1;
            }
            string texto = $"{nombre} ({tipo?.Name ?? "?"}.{metodo}): {error.GetType().Name} {error.Message}";
            if (critico) { _fallosCriticos.Add(texto); Error("Parche crítico FALLIDO: " + texto); }
            else { _fallosOpcionales.Add(texto); Aviso("Parche no aplicado: " + texto); }
            return 0;
        }

        // ---------------------------------------------------------------
        // Juego 1.11.1: fases de la carga (LogicaCarga.Fase). La acción se llama desde dentro de un try que está FUERA del
        // método que nombra los tipos del juego, así que también se recoge el TypeLoadException que lanza el JIT al entrar
        // en ese método (lo que tumbó Load() con la 0.7.2). Una fase crítica que falla se apunta en _fallosCriticos (y corre
        // la retirada de la fábrica de siempre); una opcional, en _fallosOpcionales. Los textos llevan «Parche crítico
        // FALLIDO» / «Parche no aplicado» para que salgan en la misma búsqueda del log que los parches sueltos.
        // Game 1.11.1: load phases (LogicaCarga.Fase). The action is called from inside a try that is OUTSIDE the
        // method that names the game types, so the TypeLoadException thrown by the JIT when entering
        // that method is also caught (what brought down Load() in 0.7.2). A critical phase that fails is recorded in _fallosCriticos (and the
        // usual factory rollback runs); an optional one, in _fallosOpcionales. The texts carry «Parche crítico
        // FALLIDO» / «Parche no aplicado» so they show up in the same log search as the individual patches.
        // ---------------------------------------------------------------
        private bool Fase(string nombre, Action accion, bool critico = false)
        {
            return LogicaCarga.Fase(nombre, accion, (n, e) =>
            {
                string texto = $"fase «{n}» entera: {LogicaCarga.Describir(e)}";
                if (critico) { _fallosCriticos.Add(texto); Error("Parche crítico FALLIDO (" + texto + ")"); }
                else { _fallosOpcionales.Add(texto); Aviso("Parche no aplicado (" + texto + "); el resto del núcleo sigue."); }
            });
        }

        // ---------------------------------------------------------------
        // Utilidades de log con prefijo común
        // Log utilities with a common prefix
        // ---------------------------------------------------------------
        internal static void Info(string t) => Registro?.LogInfo("[ZeroNucleo] " + t);

        // Diagnóstico: escribe la primera entrada en cada enganche (solo con Diagnostico.Trazas = true)
        // Diagnostics: writes the first entry on each hook (only with Diagnostico.Trazas = true)
        private static readonly HashSet<string> _entradas = new HashSet<string>();
        internal static void TrazaUnaVez(string enganche)
        {
            if (CfgTrazas == null || !CfgTrazas.Value) return;
            lock (_entradas) { if (!_entradas.Add(enganche)) return; }
            Info("[traza] primera entrada en " + enganche);
        }
        internal static void Aviso(string t) => Registro?.LogWarning("[ZeroNucleo] " + t);
        internal static void Error(string t) => Registro?.LogError("[ZeroNucleo] " + t);
    }
}
