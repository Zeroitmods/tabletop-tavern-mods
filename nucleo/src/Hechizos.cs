// Hechizos.cs
// Hechizos propios de los héroes nuevos (0.7.4, decisión de Zero 01-11-2026; diseño en
// notas internas (no publicadas), §5 y §7): 1901 «Mantened la línea», 1902 «Bombardeo»,
// 1903 «Empuje» y 1904 «Análisis táctico». Fase 1: solo datos, sin efectos compuestos.
// Cómo:
//   - Un hechizo del juego es un asset de datos (TJ.Spells.SpellData) y todo lo que hace sale de sus campos. Se clona uno
//     REGISTRADO (Object.Instantiate, como las copias de RaceData de FaccionesNuevas), se le pone el id propio (1900-1999,
//     un int que no está en el enum Spell: el save lo guarda como número) y se cambian los campos que dice el manifiesto
//     del módulo (Manifiesto.Hechizo). El icono se hereda de la base (decisión de Zero); el efecto visual y los sonidos,
//     también, salvo que el manifiesto diga «EfectoDe» (héroe 903 toma la onda de «Resquebrajar»).
//   - Registro: SpellRegistry.Load VACÍA _bySpell y _inAuthoredOrder cada vez que se llama, así que el punto de inyección es
//     un POSTFIX de SpellRegistry.Load (cada recarga), con respaldo antes de montar los héroes (HeroesNuevos) por si el
//     juego tuviera Load copiado dentro de EnsureLoaded (inlining): entonces se fuerza la carga y se inyecta a mano. Los dos
//     son idempotentes: si el id ya está con un clon vivo, no se hace nada. Los clones se guardan entre recargas
//     (HideAndDontSave: Unity no los descarga).
//   - Solo se registra el hechizo cuyo héroe está activo (módulo de su facción activo): sin dueño, el grimorio no lo
//     enseña, pero el menú de la batalla personalizada (SpellRegistry.All) sí lo haría.
//   - Raza de la carta: de serie o, desde el 03-11-2026 (decisión de Zero), la de su facción nueva (40 facción A,
//     41 facción B) con su banda propia en el grimorio. El juego agrupa por listas cerradas de razas
//     (SpellLoadout.GrimoireGroupOrder en el grimorio de la preparación y SpellBrowseMenu.GROUP_ORDER en la batalla): sin
//     tocarlas, una carta 40/41 no salía. AmpliarBandas les añade al final las razas nuevas que tengan carta (setter
//     estático del interop; el nombre de la banda sale de SpellRaceLabel.Get -> texto «40»/«41» de los módulos y el color,
//     del postfix de GetRaceDisplayColor de FaccionesNuevas). Si la facción no está activa, la carta se queda con la raza
//     de su base. El RaceData (tinte de la carta en el menú) es el de la facción nueva o el de un hechizo de esa raza.
//   - Si la definición no vale (base no registrada, maná fuera de rango...) o el clon falla, no se registra y el héroe
//     lleva su plan B (LogicaHechizos.Elegir), con aviso en el log. Todo en blando: nunca tumba la carga.
// Saves (SaneadoHechizos.cs), logro «Grand Grimoire» (LogrosFiltrados.cs) y autoprueba (AutoPruebaNuevas.cs, línea 33)
// van aparte. Multijugador (cooperativo y versus): sin soporte; el bloqueo online de siempre cubre las clasificaciones.
// Autor: ZeroitMods.
// Hechizos.cs
// Spells for the new heroes (0.7.4, Zero's decision 01-11-2026; design in
// internal notes (unpublished), §5 and §7): 1901 «Mantened la línea», 1902 «Bombardeo»,
// 1903 «Empuje» and 1904 «Análisis táctico». Phase 1: data only, no compound effects.
// How:
//   - A game spell is a data asset (TJ.Spells.SpellData) and everything it does comes from its fields. A REGISTERED one is cloned
//     (Object.Instantiate, like the RaceData copies in FaccionesNuevas), given its own id (1900-1999,
//     an int that is not in the Spell enum: the save stores it as a number) and the fields named by the module manifest
//     (Manifiesto.Hechizo) are changed. The icon is inherited from the base (Zero's decision); the visual effect and the sounds are too,
//     unless the manifest says «EfectoDe» (hero 903 takes the wave from «Resquebrajar»).
//   - Registry: SpellRegistry.Load CLEARS _bySpell and _inAuthoredOrder every time it is called, so the injection point is
//     a POSTFIX of SpellRegistry.Load (on every reload), with a fallback before the heroes are built (HeroesNuevos) in case the
//     game had Load copied inside EnsureLoaded (inlining): then the load is forced and injection is done by hand. Both
//     are idempotent: if the id already exists with a live clone, nothing is done. The clones are kept between reloads
//     (HideAndDontSave: Unity does not unload them).
//   - Only the spell whose hero is active is registered (its faction's module active): with no owner, the grimoire does not
//     show it, but the custom battle menu (SpellRegistry.All) would.
//   - Card race: default or, since 03-11-2026 (Zero's decision), that of its new faction (40 faction A,
//     41 faction B) with its own band in the grimoire. The game groups by closed lists of races
//     (SpellLoadout.GrimoireGroupOrder in the preparation grimoire and SpellBrowseMenu.GROUP_ORDER in battle): without
//     changing them, a 40/41 card did not appear. AmpliarBandas appends the new races that have a card (static
//     interop setter; the band name comes from SpellRaceLabel.Get -> text «40»/«41» of the modules and the color
//     from the GetRaceDisplayColor postfix of FaccionesNuevas). If the faction is not active, the card keeps the race
//     of its base. The RaceData (card tint in the menu) is that of the new faction or that of a spell of that race.
//   - If the definition is invalid (unregistered base, mana out of range...) or the clone fails, it is not registered and the hero
//     uses its plan B (LogicaHechizos.Elegir), with a warning in the log. All soft-fail: it never breaks loading.
// Saves (SaneadoHechizos.cs), achievement «Grand Grimoire» (LogrosFiltrados.cs) and self-test (AutoPruebaNuevas.cs, line 33)
// are handled separately. Multiplayer (co-op and versus): unsupported; the usual online block covers the leaderboards.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using TJ.Spells;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: hechizos propios en el registro del juego
    // Section: custom spells in the game registry
    // -----------------------------------------------------------------------
    public static class Hechizos
    {
        // Hechizos propios de los módulos activos cuyo héroe está activo (id -> definición)
        // Custom spells of the active modules whose hero is active (id -> definition)
        private static readonly Dictionary<int, Manifiesto.Hechizo> _definidos = new Dictionary<int, Manifiesto.Hechizo>();
        // Clones ya hechos (id -> asset), para no crear uno nuevo en cada recarga del registro
        // Clones already made (id -> asset), to avoid creating a new one on every registry reload
        private static readonly Dictionary<int, SpellData> _clones = new Dictionary<int, SpellData>();
        private static readonly HashSet<string> _avisos = new HashSet<string>();
        private static int _inyecciones;

        /// <summary>Hechizos propios de esta sesión (id -> definición del manifiesto).</summary>
        /// Custom spells of this session (id -> manifest definition).
        public static IReadOnlyDictionary<int, Manifiesto.Hechizo> Definidos => _definidos;
        /// <summary>Veces que se han inyectado hechizos en el registro del juego en esta sesión (para la autoprueba).</summary>
        /// Number of times spells have been injected into the game registry in this session (for the self-test).
        public static int Inyecciones => _inyecciones;

        // Aviso una sola vez por clave y sesión
        // Warn only once per key and session
        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        /// <summary>
        /// Lee los hechizos propios del manifiesto fusionado: solo los de héroes nuevos activos. Lo llama
        /// UnidadGuardia.CargarManifiesto tras FaccionesNuevas.Configurar. Si el registro del juego ya está cargado (por
        /// ejemplo, al simular otro conjunto de módulos en la autoprueba), lo pone al día en el momento.
        /// Reads the custom spells from the merged manifest: only those of active new heroes. Called by
        /// UnidadGuardia.CargarManifiesto after FaccionesNuevas.Configurar. If the game registry is already loaded (for
        /// example, when simulating another set of modules in the self-test), it updates it immediately.
        /// </summary>
        public static void Configurar(Manifiesto m)
        {
            _definidos.Clear();
            if (m?.Hechizos != null && FaccionesNuevas.Activas)
                foreach (var h in m.Hechizos)
                    if (FaccionesNuevas.Heroes.ContainsKey(h.Heroe)) _definidos[h.Id] = h;
            if (_definidos.Count > 0)
                Plugin.Info($"Hechizos propios: {string.Join(", ", _definidos.Values.OrderBy(h => h.Id).Select(h => $"{h.Id} «{h.NombreEs}» (héroe {h.Heroe}, base {h.Base})"))}.");
            // Al cargar el plugin no se llama al juego: solo cuando los héroes ya se dieron de alta (datos del juego cargados)
            // The game is not called when the plugin loads: only once the heroes have been registered (game data loaded)
            if (HeroesNuevos.AltasSesion == 0) return;
            try { if (RegistroCargado()) Inyectar("módulos"); }
            catch (Exception e) { AvisoUnaVez("configurar", "Hechizos propios: no se pudo poner al día el registro del juego (" + e.Message + ")."); }
        }

        // ¿Está ya cargado el registro de hechizos del juego? Aparte y sin inlining: si SpellRegistry se moviera, el fallo de
        // JIT sale en esta llamada y lo recoge el try del llamador
        // Is the game's spell registry already loaded? Separate and not inlined: if SpellRegistry were moved, the JIT
        // failure surfaces in this call and is caught by the caller's try
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool RegistroCargado() => SpellRegistry._loaded;

        /// <summary>Postfix de SpellRegistry.Load: el juego acaba de vaciar y rellenar el registro; se vuelven a poner los nuestros.</summary>
        /// Postfix of SpellRegistry.Load: the game has just emptied and refilled the registry; ours are put back.
        public static void TrasLoad()
        {
            try { Inyectar("SpellRegistry.Load"); }
            catch (Exception e) { AvisoUnaVez("trasload", "Hechizos propios tras SpellRegistry.Load (los héroes llevarán su plan B): " + e.Message); }
        }

        // 0.7.5, ronda 2 de la revisión: ¿ya falló en esta sesión la inyección pedida desde la lectura de un save? (no se
        // reintenta en cada lectura)
        // 0.7.5, round 2 of the review: did the injection requested from reading a save already fail in this session? (it is not
        // retried on every read)
        private static bool _lecturaFallida;

        /// <summary>
        /// 0.7.5, ronda 2 (revisión independiente): para la lectura de un save. Pone los hechizos propios en el registro del
        /// juego SOLO si ya se puede sin forzar nada: los héroes nuevos ya se dieron de alta en esta sesión y el registro de
        /// hechizos ya está cargado (las mismas condiciones que Configurar). Nunca fuerza la carga del registro desde la
        /// lectura de un save (eso sería tocar el juego en mitad de su carga). Si la inyección falla, no se reintenta en esta
        /// sesión. Devuelve si <paramref name="id"/> está registrado al terminar. Lo no devuelto sigue apuntado en el registro.
        /// 0.7.5, round 2 (independent review): for reading a save. Puts the custom spells in the game's registry
        /// ONLY if that can already be done without forcing anything: the new heroes have already been registered in this session and the spell
        /// registry is already loaded (the same conditions as Configurar). Never forces the registry to load from
        /// reading a save (that would mean touching the game in the middle of its loading). If the injection fails, it is not retried in this
        /// session. Returns whether <paramref name="id"/> is registered at the end. Anything not returned stays noted in the registry.
        /// </summary>
        public static bool RegistradoParaLectura(int id)
        {
            if (Registrado(id)) return true;
            if (_lecturaFallida || !_definidos.ContainsKey(id) || HeroesNuevos.AltasSesion == 0) return false;
            try
            {
                if (!RegistroCargado()) return false;
                Inyectar("lectura de save");
                if (Registrado(id)) return true;
                _lecturaFallida = true;
            }
            catch (Exception e)
            {
                _lecturaFallida = true;
                AvisoUnaVez("lectura", "Hechizos propios: no se pudieron poner al leer un save (" + e.Message + "); se quedan apuntados aparte.");
            }
            return false;
        }

        /// <summary>
        /// Respaldo antes de montar los héroes nuevos: fuerza la carga del registro (Exists de un hechizo común) y pone los
        /// nuestros si faltan. Cubre el caso en que el postfix de Load no salte (Load copiado dentro de EnsureLoaded).
        /// Fallback before mounting the new heroes: forces the registry to load (Exists of a common spell) and puts
        /// ours in if they are missing. Covers the case where the Load postfix does not fire (Load copied inside EnsureLoaded).
        /// </summary>
        public static void Asegurar(string origen)
        {
            if (_definidos.Count == 0) return;
            try
            {
                ForzarCarga();
                bool faltaba = _definidos.Keys.Any(id => !Registrado(id));
                int puestos = Inyectar(origen);
                if (faltaba && puestos > 0)
                    Plugin.Info($"Hechizos propios: {puestos} puesto(s) a mano desde {origen} (el postfix de SpellRegistry.Load no había saltado).");
            }
            catch (Exception e) { AvisoUnaVez("asegurar", "Hechizos propios: respaldo de la inyección fallido (" + e.Message + "); los héroes llevarán su plan B."); }
        }

        // Carga el registro si aún no lo está (Exists llama a EnsureLoaded). Aparte y sin inlining, como RegistroCargado
        // Loads the registry if it is not loaded yet (Exists calls EnsureLoaded). Separate and without inlining, like RegistroCargado
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ForzarCarga() => SpellRegistry.Exists(Spell.RallyTheBanners);

        /// <summary>¿Está el id en el registro del juego ahora? (false si no se puede consultar)</summary>
        /// Is the id in the game's registry right now? (false if it cannot be queried)
        public static bool Registrado(int id)
        {
            try
            {
                var dic = SpellRegistry._bySpell;
                return dic != null && dic.ContainsKey((Spell)id) && dic[(Spell)id] != null;
            }
            catch { return false; }
        }

        /// <summary>Ids de todos los hechizos del registro del juego en su orden (vacío si no se puede leer).</summary>
        /// Ids of all the spells in the game's registry in their order (empty if it cannot be read).
        public static List<int> IdsRegistrados()
        {
            var r = new List<int>();
            try
            {
                var lista = SpellRegistry._inAuthoredOrder;
                if (lista == null) return r;
                for (int i = 0; i < lista.Count; i++) if (lista[i] != null) r.Add((int)lista[i].Spell);
            }
            catch { }
            return r;
        }

        /// <summary>
        /// Pone los hechizos propios en el registro del juego (diccionario y lista en orden, al final: detrás de los de serie)
        /// y quita los propios que ya no toquen (módulo apagado al simular). Devuelve cuántos ha puesto.
        /// Puts the custom spells in the game's registry (dictionary and ordered list, at the end: after the vanilla ones)
        /// and removes the custom ones that no longer apply (module turned off when simulating). Returns how many it has added.
        /// </summary>
        public static int Inyectar(string origen)
        {
            var dic = SpellRegistry._bySpell;
            var lista = SpellRegistry._inAuthoredOrder;
            if (dic == null || lista == null) return 0;
            // Fuera los propios que no estén definidos ahora (de atrás adelante para no saltarse ninguno)
            // Remove the custom ones that are not defined now (back to front so none is skipped)
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                var s = lista[i];
                if (s == null || !LogicaHechizos.EsIdPropio((int)s.Spell) || _definidos.ContainsKey((int)s.Spell)) continue;
                dic.Remove(s.Spell);
                lista.RemoveAt(i);
            }
            int puestos = 0;
            foreach (var h in _definidos.Values.OrderBy(x => x.Id))
            {
                var clave = (Spell)h.Id;
                SpellData actual = dic.ContainsKey(clave) ? dic[clave] : null;
                if (actual != null && Vivo(actual))
                {
                    if (!lista.Contains(actual)) lista.Add(actual);
                    continue;
                }
                SpellData clon = ClonVivo(h.Id) ?? Clonar(dic, h);
                if (clon == null) continue;
                dic[clave] = clon;
                if (!lista.Contains(clon)) lista.Add(clon);
                puestos++;
            }
            // Bandas propias del grimorio para las cartas 40/41 que estén en el registro (idempotente; nunca lanza)
            // Custom grimoire bands for cards 40/41 that are in the registry (idempotent; never throws)
            AmpliarBandas(origen);
            if (puestos > 0)
            {
                _inyecciones++;
                if (_inyecciones <= 5 || _inyecciones % 20 == 0)
                    Plugin.Info($"[{origen}] Hechizos propios registrados: {string.Join(", ", _definidos.Keys.Where(Registrado).OrderBy(x => x))} " +
                                $"(registro del juego: {lista.Count} hechizos; inyección {_inyecciones} de la sesión).");
            }
            return puestos;
        }

        // ---------------------------------------------------------------
        // Bandas propias del grimorio (03-11-2026): «facción A» (40) e «facción B» (41)
        // Grimoire's own bands (03-11-2026): «facción A» (40) and «facción B» (41)
        // ---------------------------------------------------------------
        /// <summary>Orden de bandas del grimorio de la preparación tal como está ahora (para la autoprueba; vacío si no se puede leer).</summary>
        /// Order of the grimoire bands in the preparation screen as it is now (for the self-test; empty if it cannot be read).
        public static List<int> OrdenGrimorio()
        {
            // Revisión externa del 03-11 (hallazgo 4): el constructor estático del juego tiene que haber corrido antes de
            // leer o escribir el campo (si no, el array que se pone lo pisaría después)
            // External review of 03-11 (finding 4): the game's static constructor must have run before
            // reading or writing the field (otherwise, the array being set would be overwritten afterwards)
            InicializarClase(typeof(SpellLoadout));
            return LeerOrden(SpellLoadout.GrimoireGroupOrder);
        }
        /// <summary>Orden de bandas del menú de hechizos de la batalla (para la autoprueba).</summary>
        /// Order of the bands in the battle spell menu (for the self-test).
        public static List<int> OrdenBatalla()
        {
            InicializarClase(typeof(SpellBrowseMenu));
            return LeerOrden(SpellBrowseMenu.GROUP_ORDER);
        }

        // Corre el constructor estático de una clase del juego (si el interop no da su puntero, no se llama: un puntero nulo
        // cerraría el juego)
        // Runs the static constructor of a game class (if interop does not give its pointer, it is not called: a null pointer
        // would crash the game)
        private static void InicializarClase(Type tipo)
        {
            IntPtr clase = Il2CppInterop.Runtime.Il2CppClassPointerStore.GetNativeClassPointer(tipo);
            if (clase != IntPtr.Zero) Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(clase);
        }

        /// <summary>Prefix de WarbandPanel.BuildGrimoire: segunda oportunidad de las bandas propias justo antes de construirlas.</summary>
        /// Prefix of WarbandPanel.BuildGrimoire: second chance for the custom bands right before building them.
        public static void AntesDeBuildGrimoire() => AmpliarBandas("WarbandPanel.BuildGrimoire");

        // Copia de un orden de bandas del juego como lista de enteros
        // Copy of a game band order as a list of integers
        private static List<int> LeerOrden(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Race> orden)
        {
            var r = new List<int>();
            if (orden == null) return r;
            for (int i = 0; i < orden.Length; i++) r.Add((int)orden[i]);
            return r;
        }

        /// <summary>
        /// Añade al final de los dos órdenes de bandas del juego (grimorio de la preparación y menú de la batalla) las razas
        /// nuevas 40/41 que tengan alguna carta propia en el registro. Nunca quita nada ni cambia el orden de serie, y no
        /// toca nada si no hay cartas 40/41. Si el juego cambia o falla, aviso una vez: esas cartas no salen en su banda.
        /// Appends to the end of the game's two band orders (preparation grimoire and battle menu) the new races
        /// 40/41 that have any custom card in the registry. Never removes anything or changes the vanilla order, and does not
        /// touch anything if there are no 40/41 cards. If the game changes or fails, I warn once: those cards do not appear in their band.
        /// </summary>
        public static void AmpliarBandas(string origen)
        {
            try
            {
                var razas = new List<int>();
                foreach (var c in _clones.Values)
                    if (Vivo(c) && Registrado((int)c.Spell) && LogicaHechizos.EsRazaBandaPropia((int)c.Race)) razas.Add((int)c.Race);
                if (razas.Count == 0) return;
                AmpliarUno("grimorio", OrdenGrimorio(), razas, o => SpellLoadout.GrimoireGroupOrder = o, origen);
                AmpliarUno("batalla", OrdenBatalla(), razas, o => SpellBrowseMenu.GROUP_ORDER = o, origen);
            }
            catch (Exception e) { AvisoUnaVez("bandas", "Hechizos propios: no se pudieron añadir las bandas Astra Militarum / Imperio Galáctico (" + e.Message + "); esas cartas no salen en el grimorio."); }
        }

        // Un orden: si le faltan razas nuevas, se pone un array nuevo del juego con las de serie y las nuevas detrás
        // An order: if it is missing new races, a new game array is set with the vanilla ones and the new ones after them
        private static void AmpliarUno(string que, List<int> actual, List<int> razas,
                                       Action<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Race>> poner, string origen)
        {
            if (actual.Count == 0) { AvisoUnaVez("bandasVacio" + que, $"Hechizos propios: el orden de bandas del {que} sale vacío; no se toca."); return; }
            var nuevo = LogicaHechizos.OrdenBandasAmpliado(actual, razas);
            if (nuevo == null) return;
            var arr = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Race>(nuevo.Length);
            for (int i = 0; i < nuevo.Length; i++) arr[i] = (Race)nuevo[i];
            poner(arr);
            Plugin.Info($"[{origen}] Bandas del {que}: {actual.Count} de serie + {string.Join(", ", nuevo.Skip(actual.Count))} (cartas propias de las facciones nuevas).");
        }

        // Clon guardado de una recarga anterior, si Unity no lo ha destruido
        // Saved clone from a previous reload, if Unity has not destroyed it
        private static SpellData ClonVivo(int id) => _clones.TryGetValue(id, out var c) && Vivo(c) ? c : null;

        // ¿Sigue vivo el objeto de Unity? (no recogido por el interop ni destruido por Unity)
        // Is the Unity object still alive? (not collected by interop nor destroyed by Unity)
        private static bool Vivo(UnityEngine.Object o)
        {
            if (o == null) return false;
            try { return !o.WasCollected && o.GetInstanceID() != 0 && o.name != null; }
            catch { return false; }
        }

        // ¿Tiene asset en el registro un nombre del enum Spell? (para validar base y efecto)
        // Does a name from the Spell enum have an asset in the registry? (to validate base and effect)
        private static bool BaseRegistrada(Il2CppSystem.Collections.Generic.Dictionary<Spell, SpellData> dic, string nombre) =>
            Enum.TryParse(nombre, out Spell s) && Enum.IsDefined(typeof(Spell), s) && dic.ContainsKey(s) && dic[s] != null;

        // Crea el clon de un hechizo propio con los campos del manifiesto (null si la definición no vale o algo falla)
        // Creates the clone of a custom spell with the manifest fields (null if the definition is invalid or something fails)
        private static SpellData Clonar(Il2CppSystem.Collections.Generic.Dictionary<Spell, SpellData> dic, Manifiesto.Hechizo h)
        {
            string motivo = LogicaHechizos.Validar(h, n => BaseRegistrada(dic, n));
            if (motivo != null)
            {
                AvisoUnaVez("valida" + h.Id, $"Hechizo propio {h.Id} («{h.NombreEs}», módulo {h.Modulo}): {motivo}. No se registra; su héroe lleva el plan B.");
                return null;
            }
            try
            {
                Enum.TryParse(h.Base, out Spell deBase);
                SpellData plantilla = dic[deBase];
                var clon = UnityEngine.Object.Instantiate(plantilla.Cast<UnityEngine.Object>()).Cast<SpellData>();
                clon.name = $"Zero Hechizo {h.Id} ({h.NombreEn})";
                // Que Unity no lo descargue al cambiar de escena (no está en ningún asset)
                // So that Unity does not unload it when changing scenes (it is not in any asset)
                clon.hideFlags = HideFlags.HideAndDontSave;
                clon.Spell = (Spell)h.Id;
                // Raza de la carta y su tinte. De serie: el RaceData de un hechizo registrado de esa raza si cambia respecto a
                // la base. Facción nueva 40/41 activa (03-11-2026): su RaceData propio (colores de la facción) y su banda
                // Card race and its tint. Vanilla: the RaceData of a registered spell of that race if it differs from
                // the base. New faction 40/41 active (03-11-2026): its own RaceData (faction colors) and its band
                if (int.TryParse(h.Raza, out int razaNueva) && LogicaHechizos.EsRazaBandaPropia(razaNueva))
                {
                    // Revisión externa del 03-11 (hallazgo 8): la banda tiene que ser la de la facción de su héroe
                    // External review of 03-11 (finding 8): the band must be that of its hero's faction
                    bool deSuHeroe = FaccionesNuevas.Heroes.TryGetValue(h.Heroe, out var dueno) && dueno.Raza == razaNueva;
                    if (!deSuHeroe)
                        AvisoUnaVez("razaHeroe" + h.Id, $"Hechizo propio {h.Id}: la raza {razaNueva} no es la de su héroe {h.Heroe}; la carta se queda con la raza de su base ({clon.Race}).");
                    else if (FaccionesNuevas.EsNueva(razaNueva))
                    {
                        var datos = FaccionesNuevas.DatosDe((Race)razaNueva);
                        if (datos != null) clon.RaceData = datos;
                        clon.Race = (Race)razaNueva;
                    }
                    else AvisoUnaVez("razaInactiva" + h.Id, $"Hechizo propio {h.Id}: su facción {razaNueva} no está activa; la carta se queda con la raza de su base ({clon.Race}).");
                }
                else if (Enum.TryParse(h.Raza, out Race raza) && Enum.IsDefined(typeof(Race), raza) && (int)raza < 40)
                {
                    if (clon.Race != raza)
                    {
                        var tinte = RaceDataDe(raza);
                        if (tinte != null) clon.RaceData = tinte;
                    }
                    clon.Race = raza;
                }
                if (h.Mana.HasValue) clon.SpellManaCost = h.Mana.Value;
                if (h.Enfriamiento.HasValue) clon.SpellCooldown = h.Enfriamiento.Value;
                if (h.Valor.HasValue) clon.SpellModifierValue = h.Valor.Value;
                if (h.Radio.HasValue) clon.SpellRadius = h.Radio.Value;
                if (h.Preparacion.HasValue) clon.SpellWarmUpDuration = h.Preparacion.Value;
                if (h.Duracion.HasValue) clon.SpellDuration = h.Duracion.Value;
                if (h.Fuerza.HasValue) clon.SpellForce = h.Fuerza.Value;
                if (h.UnSoloGolpe.HasValue) clon.IsOneOff = h.UnSoloGolpe.Value;
                if (h.Sacudida.HasValue) clon.ImpactShake = h.Sacudida.Value;
                if (h.IconoEstado.HasValue) clon.ShowsStatusIcon = h.IconoEstado.Value;
                if (!string.IsNullOrWhiteSpace(h.Equipo) && Enum.TryParse(h.Equipo, out Team equipo)) clon.TargetTeam = equipo;
                if (!string.IsNullOrWhiteSpace(h.TipoBonus) && Enum.TryParse(h.TipoBonus, out BattlefieldBonusEnum tipo)) clon.BonusType = tipo;
                if (!string.IsNullOrWhiteSpace(h.StatBonus) && Enum.TryParse(h.StatBonus, out UnitStat statBonus)) clon.BonusUnitStat = statBonus;
                if (h.Stats != null)
                {
                    // Lista NUEVA del juego (la del original es compartida: cambiarla tocaría también el hechizo de serie)
                    // NEW game list (the original's is shared: changing it would also affect the vanilla spell)
                    var stats = new Il2CppSystem.Collections.Generic.List<SpellBonusStat>();
                    foreach (var s in h.Stats)
                    {
                        if (!Enum.TryParse(s.Stat, out UnitStat st)) continue;
                        stats.Add(new SpellBonusStat { UnitStat = st, Value = s.Valor });
                    }
                    clon.BonusStats = stats;
                }
                // Efecto visual y sonidos de otro hechizo de serie (opcional)
                // Visual effect and sounds from another vanilla spell (optional)
                if (!string.IsNullOrWhiteSpace(h.EfectoDe) && Enum.TryParse(h.EfectoDe, out Spell deEfecto) && dic.ContainsKey(deEfecto) && dic[deEfecto] != null)
                {
                    var efecto = dic[deEfecto];
                    clon.SpellVisualPrefab = efecto.SpellVisualPrefab;
                    clon.warmupSound = efecto.warmupSound;
                    clon.hitSound = efecto.hitSound;
                }
                _clones[h.Id] = clon;
                Plugin.Info($"Hechizo propio {h.Id} «{h.NombreEs}» creado desde {h.Base}: raza {clon.Race}, maná {clon.SpellManaCost}, " +
                            $"valor {clon.SpellModifierValue}, radio {clon.SpellRadius:0.#}, duración {clon.SpellDuration:0.#} s, equipo {clon.TargetTeam}" +
                            (string.IsNullOrWhiteSpace(h.EfectoDe) ? "" : $", efecto de {h.EfectoDe}") + ".");
                return clon;
            }
            catch (Exception e)
            {
                AvisoUnaVez("clon" + h.Id, $"Hechizo propio {h.Id} («{h.NombreEs}»): no se pudo crear ({e.GetType().Name}: {e.Message}). Su héroe lleva el plan B.");
                return null;
            }
        }

        // RaceData (tinte del menú) de algún hechizo registrado de esa raza, o null
        // RaceData (menu tint) of some registered spell of that race, or null
        private static RaceData RaceDataDe(Race raza)
        {
            var lista = SpellRegistry._inAuthoredOrder;
            if (lista == null) return null;
            for (int i = 0; i < lista.Count; i++)
            {
                var s = lista[i];
                if (s != null && s.Race == raza && !LogicaHechizos.EsIdPropio((int)s.Spell) && s.RaceData != null) return s.RaceData;
            }
            return null;
        }

        /// <summary>Datos de un hechizo del registro para la autoprueba (null si no está).</summary>
        /// Data of a registry spell for the self-test (null if it is not there).
        internal static SpellData Datos(int id)
        {
            try
            {
                var dic = SpellRegistry._bySpell;
                return dic != null && dic.ContainsKey((Spell)id) ? dic[(Spell)id] : null;
            }
            catch { return null; }
        }
    }
}
