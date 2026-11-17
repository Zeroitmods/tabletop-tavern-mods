// Nucleo.cs
// Estado de los módulos en la sesión (0.7.0): al cargar el plugin (antes de que el juego lea sus mods) sincroniza los
// módulos que deja un gestor en BepInEx\plugins hacia la carpeta Mods del juego, apunta desactivados en modlist.json los
// que vienen así por defecto (escuadras +100 % y +150 %), lee qué módulos están activos y suma lo que declaran
// (Modulos.Resolver). Después reparte el resultado a las piezas de siempre: la fábrica de unidades, las facciones, los
// objetos (UnidadGuardia.CargarManifiesto) y los efectos de escuadras, munición y stats a distancia (EjercitoReforzado).
// Nucleo.cs
// Module state in the session (0.7.0): when the plugin loads (before the game reads its mods) it syncs the
// modules a manager leaves in BepInEx\plugins to the game's Mods folder, marks as disabled in modlist.json those
// that ship that way by default (squads +100 % and +150 %), reads which modules are active and adds up what they declare
// (Modulos.Resolver). Then it distributes the result to the usual pieces: the unit factory, the factions, the
// items (UnidadGuardia.CargarManifiesto) and the effects on squads, ammo and ranged stats (EjercitoReforzado).
//
// El juego lee modlist.json al construir el catálogo (y en cada ReloadData); el menú de mods guarda los cambios para el
// siguiente arranque. El núcleo fija el conjunto activo al cargar y avisa si en una recarga el juego ve otro distinto.
// Para la autoprueba puede aplicar en memoria otro conjunto de módulos (Simular) y volver al real (Restaurar).
// Autor: ZeroitMods.
// The game reads modlist.json when building the catalog (and on every ReloadData); the mods menu saves changes for the
// next launch. The core fixes the active set on load and warns if on a reload the game sees a different one.
// For the self-test it can apply another set of modules in memory (Simular) and go back to the real one (Restaurar).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TJ;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: módulos de la sesión
    // Section: session modules
    // -----------------------------------------------------------------------
    public static class Nucleo
    {
        /// <summary>Nombre de la carpeta del mod monolítico antiguo (0.6.1); si sigue activo se avisa.</summary>
        /// Folder name of the old monolithic mod (0.6.1); if it is still active a warning is shown.
        public const string CarpetaMonolitoViejo = "ZeroGuardiaImperial";

        internal static string CarpetaMods { get; private set; }
        internal static SincronizarModulos.Informe Sincronizacion { get; private set; } = new SincronizarModulos.Informe();
        internal static List<string> DesactivadosPuestos { get; private set; } = new List<string>();
        /// <summary>Todos los módulos de la carpeta Mods (activos o no) al arrancar.</summary>
        /// All modules in the Mods folder (active or not) at startup.
        internal static List<ModuloEncontrado> Encontrados { get; private set; } = new List<ModuloEncontrado>();
        /// <summary>Carpetas de mods activas al arrancar (réplica del orden de carga del juego).</summary>
        /// Mod folders active at startup (replica of the game's load order).
        internal static List<string> CarpetasActivas { get; private set; } = new List<string>();
        /// <summary>Lo que se suma con los módulos activos de verdad.</summary>
        /// What is added up with the truly active modules.
        internal static ResultadoModulos Real { get; private set; } = new ResultadoModulos();
        /// <summary>Lo que se está aplicando ahora (el real o, durante la autoprueba, uno simulado).</summary>
        /// What is being applied right now (the real one or, during the self-test, a simulated one).
        internal static ResultadoModulos Actual { get; private set; } = new ResultadoModulos();
        internal static bool Simulando { get; private set; }
        private static bool _avisoIncoherencia;

        /// <summary>
        /// Sincroniza, apunta los desactivados por defecto, lee los activos y suma sus declaraciones. No llama al juego
        /// (vale en Plugin.Load). Cualquier fallo se registra y deja el resultado vacío (juego de serie).
        /// Syncs, records the ones disabled by default, reads the active ones and adds up their declarations. Does not call the game
        /// (valid in Plugin.Load). Any failure is logged and leaves the result empty (vanilla game).
        /// </summary>
        public static void Iniciar(string carpetaDatosJuego, string carpetaPlugins, string carpetaCopias)
        {
            CarpetaMods = Path.Combine(carpetaDatosJuego, "Mods");
            try
            {
                Sincronizacion = SincronizarModulos.Sincronizar(carpetaPlugins, CarpetaMods);
                Plugin.Info("Sincronización de módulos: " + Sincronizacion);
                foreach (string a in Sincronizacion.Avisos) Plugin.Aviso("Sincronización: " + a);
            }
            catch (Exception e) { Plugin.Error("Sincronización de módulos: " + e); }

            var avisos = new List<string>();
            try
            {
                // Primero las declaraciones (para saber cuáles vienen desactivados), luego el estado activo real
                // Declarations first (to know which ones come disabled), then the real active state
                var todos = Modulos.Buscar(CarpetaMods, null, avisos);
                DesactivadosPuestos = OrdenMods.ApuntarDesactivados(CarpetaMods, todos, carpetaCopias, avisos);
                if (DesactivadosPuestos.Count > 0)
                    Plugin.Info($"Módulos nuevos apuntados DESACTIVADOS en modlist.json (vienen así por defecto): {string.Join(", ", DesactivadosPuestos)}. Se activan desde el menú de mods del juego.");
                CarpetasActivas = OrdenMods.CarpetasActivas(CarpetaMods);
                Encontrados = Modulos.Buscar(CarpetaMods, CarpetasActivas, avisos);
                Real = Modulos.Resolver(Encontrados, Plugin.VERSION);
            }
            catch (Exception e)
            {
                Plugin.Error("Lectura de los módulos: " + e + ". Se sigue sin módulos (juego de serie).");
                Real = new ResultadoModulos();
                // 0.7.5: se apunta para PartidaModificada (bloqueos en línea si hay carpetas de módulo activas)
                // 0.7.5: recorded for PartidaModificada (online locks if there are active module folders)
                LecturaFallida = true;
                if (CarpetasActivas.Count == 0)
                    try { CarpetasActivas = OrdenMods.CarpetasActivas(CarpetaMods); } catch { }
            }
            foreach (string a in avisos.Concat(Real.Avisos)) Plugin.Aviso("Módulos: " + a);
            foreach (string r in Real.Rechazados) Plugin.Error("Módulo NO aplicado: " + r);
            Plugin.Info($"Módulos encontrados en {CarpetaMods}: {(Encontrados.Count == 0 ? "ninguno" : string.Join("; ", Encontrados))}.");
            Plugin.Info("Módulos aplicados en esta sesión: " + Describir(Real));
            if (CarpetasActivas.Contains(CarpetaMonolitoViejo, StringComparer.OrdinalIgnoreCase))
                Plugin.Aviso($"El mod antiguo «{CarpetaMonolitoViejo}» (todo en uno, 0.6.1) sigue ACTIVO en Mods: duplica los datos de los módulos. Desactívalo o quítalo.");
            Actual = Real;
        }

        /// <summary>Resumen legible de un resultado (módulos, contenido y efectos).</summary>
        /// Readable summary of a result (modules, content and effects).
        public static string Describir(ResultadoModulos r)
        {
            if (r.Aplicados.Count == 0) return "ninguno (el núcleo no cambia nada: juego de serie)";
            var m = r.Manifiesto;
            string escuadras = r.ModuloEscuadras == null ? "de serie" : $"x{r.FactorEscuadras:0.##} ({r.ModuloEscuadras})";
            string municion = r.Municion == null ? "no" : $"x{r.Municion.Multiplicador}";
            string distancia = r.Distancia == null ? "no" : $"+{r.Distancia.Alcance} alcance, +{r.Distancia.Precision} precisión, +{r.Distancia.Dano} daño a {string.Join("/", r.Distancia.Tipos)}";
            string moral = r.Moral == null ? "de serie" : $"{(r.Moral.SinHuida ? "sin huida" : "")}{(r.Moral.SinHuida && r.Moral.SinVacilar ? " y " : "")}{(r.Moral.SinVacilar ? "sin flaquear" : "")} ({r.ModuloMoral})";
            return $"{string.Join(", ", r.Aplicados.Select(a => a.Id))}; contenido: {m.Unidades.Count} unidades, {m.Razas.Count} razas, {m.Heroes.Count} héroes, " +
                   $"{m.Objetos.Count} objetos, {m.Hechizos.Count} hechizos propios; escuadras {escuadras}; munición del jugador {municion}; stats a distancia {distancia}; moral del jugador {moral}";
        }

        /// <summary>Reparte un resultado a la fábrica, las facciones, los objetos y los efectos.</summary>
        /// Dispatches a result to the factory, the factions, the gear and the effects.
        public static void Aplicar(ResultadoModulos r)
        {
            Actual = r ?? new ResultadoModulos();
            UnidadGuardia.CargarManifiesto(Actual.Manifiesto);
            EjercitoReforzado.Configurar(Actual);
            MoralJugador.Configurar(Actual);
            Estandarte.Configurar(Actual);
        }

        /// <summary>¿Algún módulo instalado (activo o no) declara un efecto de escuadras, munición, distancia o moral (0.7.6)?</summary>
        /// Does any installed module (active or not) declare a squad, ammo, range or morale effect (0.7.6)?
        public static bool HayEfectosInstalados =>
            Encontrados.Any(m => m.Declaracion?.Efectos != null &&
                                 (m.Declaracion.Efectos.Escuadras != null || m.Declaracion.Efectos.MunicionJugador != null ||
                                  m.Declaracion.Efectos.DistanciaJugador != null || m.Declaracion.Efectos.MoralJugador != null));

        /// <summary>¿Este módulo (por id) se está aplicando ahora?</summary>
        /// Is this module (by id) being applied right now?
        public static bool Aplicado(string id) => Actual.Aplicado(id);

        /// <summary>
        /// 0.7.1: ¿la sesión aplica algún módulo de Zero? (con efectos, contenido o datos del juego, como Economía). Con
        /// alguno, las partidas son modificadas y no se envían a las clasificaciones online.
        /// 0.7.5 (revisión total, hallazgo 35): también si hay un módulo de Zero ACTIVO en el menú de mods que el núcleo ha
        /// rechazado (núcleo mínimo mayor, dependencia que falta, id repetido): el juego carga igual sus datos oficiales, así
        /// que la partida está modificada aunque el núcleo no lo aplique. Y si la lectura de los módulos falló, cualquier
        /// carpeta activa con zero_modulo.json. Es sobre los módulos REALES: la simulación de la autoprueba no lo cambia.
        /// 0.7.1: does the session apply any Zero module? (with effects, content or game data, like Economía). With
        /// any, the runs are modified and are not sent to the online leaderboards.
        /// 0.7.5 (full review, finding 35): also if there is an ACTIVE Zero module in the mods menu that the core has
        /// rejected (higher minimum core, missing dependency, duplicate id): the game still loads its official data, so
        /// the run is modified even if the core does not apply it. And if reading the modules failed, any active
        /// folder with zero_modulo.json. It concerns the REAL modules: the self-test simulation does not change it.
        /// </summary>
        public static bool PartidaModificada => Real.Aplicados.Count > 0 || ModulosActivosSinAplicar;

        /// <summary>0.7.5: ¿hay módulos de Zero activos en el menú que no se aplican (rechazados o lectura fallida)?</summary>
        /// 0.7.5: are there active Zero modules in the menu that are not applied (rejected or failed to read)?
        internal static bool ModulosActivosSinAplicar
        {
            get
            {
                if (Encontrados.Any(m => m.Activo)) return true;
                if (!LecturaFallida || string.IsNullOrEmpty(CarpetaMods)) return false;
                try { return CarpetasActivas.Any(n => File.Exists(Path.Combine(CarpetaMods, n, "zero_modulo.json"))); }
                catch { return true; }   // ante la duda, como modificada (bloquear en línea no rompe nada)
                                         // when in doubt, treat as modified (blocking online breaks nothing)
            }
        }

        /// <summary>0.7.5: módulos de Zero activos en el menú de mods (aplicados o rechazados), para los avisos.</summary>
        /// 0.7.5: active Zero modules in the mods menu (applied or rejected), for the warnings.
        internal static string ModulosActivosTexto()
        {
            var ids = Encontrados.Where(m => m.Activo).Select(m => m.Id).ToList();
            return ids.Count > 0 ? string.Join(", ", ids) : LecturaFallida ? "lectura de módulos fallida" : "ninguno";
        }

        /// <summary>0.7.5: true si falló la lectura de los módulos al arrancar (el resultado se quedó vacío).</summary>
        /// 0.7.5: true if reading the modules failed at startup (the result was left empty).
        internal static bool LecturaFallida { get; private set; }

        /// <summary>
        /// 0.7.1: carpetas de mods activas AL ARRANCAR, como rutas completas y en una lista del juego. Para reconstruir las
        /// reglas de héroe con el mismo conjunto de mods que leyó el núcleo (no con el modlist.json del momento).
        /// 0.7.1: mod folders active AT STARTUP, as full paths and in a game list. To rebuild the
        /// hero rules with the same set of mods the core read (not with the modlist.json of the moment).
        /// </summary>
        internal static Il2CppSystem.Collections.Generic.List<string> CarpetasParaJuego()
        {
            var lista = new Il2CppSystem.Collections.Generic.List<string>();
            foreach (string n in CarpetasActivas) lista.Add(Path.Combine(CarpetaMods, n));
            return lista;
        }

        /// <summary>0.7.5 (ronda 2): razas nuevas de todos los módulos instalados y su raza sustituta (nombre del enum).</summary>
        /// 0.7.5 (round 2): new races from all installed modules and their replacement race (enum name).
        internal static Dictionary<int, string> RazasInstaladas()
        {
            // 0.7.5, ronda 2 de la revisión: razas nuevas de TODOS los módulos instalados (activos o no) y el nombre de su raza
            // sustituta de serie. Para sanear el historial aunque el módulo de esa facción esté apagado
            // 0.7.5, review round 2: new races from ALL installed modules (active or not) and the name of their vanilla
            // replacement race. To sanitize the history even if that faction's module is turned off
            var r = new Dictionary<int, string>();
            try
            {
                var todos = Encontrados.Select(m => new ModuloEncontrado { Declaracion = m.Declaracion, Carpeta = m.Carpeta, NombreCarpeta = m.NombreCarpeta, Activo = true });
                foreach (var z in Modulos.Resolver(todos, Plugin.VERSION).Manifiesto.Razas)
                    if (z.Id >= 40 && !r.ContainsKey(z.Id)) r[z.Id] = z.RazaSustituta;
            }
            catch (Exception e) { Plugin.Aviso("No se pudieron leer las razas de los módulos instalados: " + e.Message); }
            return r;
        }

        /// <summary>
        /// 0.7.1: héroes nuevos de TODOS los módulos instalados (activos o no) y su héroe prestado de serie (Edric, 1, si el
        /// manifiesto no dice ninguno). Para el saneado de saves: un héroe de un módulo apagado se carga como su prestado.
        /// 0.7.1: new heroes from ALL installed modules (active or not) and their vanilla borrowed hero (Edric, 1, if the
        /// manifest specifies none). For save sanitizing: a hero from a disabled module is loaded as its borrowed hero.
        /// </summary>
        internal static Dictionary<int, int> HeroesInstalados()
        {
            var r = new Dictionary<int, int>();
            try
            {
                var todos = Encontrados.Select(m => new ModuloEncontrado { Declaracion = m.Declaracion, Carpeta = m.Carpeta, NombreCarpeta = m.NombreCarpeta, Activo = true });
                foreach (var h in Modulos.Resolver(todos, Plugin.VERSION).Manifiesto.Heroes)
                    if (!r.ContainsKey(h.Id)) r[h.Id] = h.PrestadoDe > 0 ? h.PrestadoDe : 1;
            }
            catch (Exception e) { Plugin.Aviso("No se pudieron leer los héroes de los módulos instalados: " + e.Message); }
            return r;
        }

        // ---------------------------------------------------------------
        // Simulación para la autoprueba
        // Simulation for the self-test
        // ---------------------------------------------------------------
        /// <summary>
        /// Suma un conjunto de módulos por id como si solo esos estuvieran activos. Los de solo efectos valen aunque estén
        /// desactivados en el menú (su declaración basta); los de contenido solo si el juego los cargó (si no, sus stats y
        /// textos no estarían en el catálogo): esos se devuelven en <paramref name="omitidos"/>.
        /// Adds up a set of modules by id as if only those were active. Effect-only ones count even if they are
        /// disabled in the menu (their declaration is enough); content ones only if the game loaded them (otherwise their stats and
        /// texts would not be in the catalog): those are returned in <paramref name="omitidos"/>.
        /// </summary>
        public static ResultadoModulos ResolverConjunto(IEnumerable<string> ids, out List<string> omitidos)
        {
            omitidos = new List<string>();
            var pedidos = new HashSet<string>(ids ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var lista = new List<ModuloEncontrado>();
            foreach (var m in Encontrados)
            {
                if (!pedidos.Contains(m.Id)) continue;
                bool soloEfectos = string.IsNullOrWhiteSpace(m.Declaracion.Contenido);
                if (!soloEfectos && !m.Activo) { omitidos.Add(m.Id); continue; }
                lista.Add(new ModuloEncontrado { Declaracion = m.Declaracion, Carpeta = m.Carpeta, NombreCarpeta = m.NombreCarpeta, Activo = true });
            }
            foreach (string id in pedidos) if (!Encontrados.Any(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase))) omitidos.Add(id);
            return Modulos.Resolver(lista, Plugin.VERSION);
        }

        /// <summary>Aplica un conjunto simulado y reconstruye el catálogo del juego (solo autoprueba, en el menú).</summary>
        /// Applies a simulated set and rebuilds the game catalog (self-test only, in the menu).
        public static ResultadoModulos Simular(TabletopTavernData data, IEnumerable<string> ids, out List<string> omitidos)
        {
            var r = ResolverConjunto(ids, out omitidos);
            Simulando = true;
            Saves.PausarVigilante = true;
            Aplicar(r);
            data.ReloadData();
            return r;
        }

        /// <summary>Vuelve a los módulos reales y reconstruye el catálogo.</summary>
        /// Goes back to the real modules and rebuilds the catalog.
        public static void Restaurar(TabletopTavernData data)
        {
            try
            {
                Aplicar(Real);
                data.ReloadData();
            }
            finally
            {
                Simulando = false;
                Saves.PausarVigilante = false;
            }
        }

        // ---------------------------------------------------------------
        // Coherencia con lo que carga el juego
        // Consistency with what the game loads
        // ---------------------------------------------------------------
        /// <summary>
        /// Tras cada recarga: si el juego ha cargado otro conjunto de módulos distinto del leído al arrancar (por ejemplo,
        /// se cambió el menú de mods y algo recargó el catálogo), se avisa una vez: el núcleo sigue con el de arranque.
        /// After each reload: if the game has loaded a different set of modules from the one read at startup (for example,
        /// the mods menu was changed and something reloaded the catalog), a warning is shown once: the core keeps the startup one.
        /// </summary>
        public static void ComprobarCoherencia()
        {
            if (_avisoIncoherencia || Simulando) return;
            try
            {
                // Orden de carga que ve ahora el juego (lee modlist.json; es lo que usa ReloadData)
                // Load order the game currently sees (reads modlist.json; it is what ReloadData uses)
                var cargadas = UnidadGuardia.ModsActivos();
                if (cargadas == null || cargadas.Count == 0 && Encontrados.All(m => !m.Activo)) return;
                var distintos = Encontrados.Where(m => m.Activo != cargadas.Contains(m.NombreCarpeta)).Select(m => m.NombreCarpeta).ToList();
                if (distintos.Count == 0) return;
                _avisoIncoherencia = true;
                Plugin.Aviso($"El menú de mods tiene ahora otro estado para {string.Join(", ", distintos)} que el leído al arrancar: el núcleo sigue con el de arranque. Reinicia el juego para aplicar los cambios.");
            }
            catch (Exception e) { _avisoIncoherencia = true; Plugin.Aviso("No se pudo comparar los módulos cargados por el juego: " + e.Message); }
        }
    }
}
