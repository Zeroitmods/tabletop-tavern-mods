// HeroesNuevos.cs
// Fase F2 (diseño §4.2, §5.2 y §6.3): los héroes nuevos 901, 902 (facción A, raza 40), 903 y
// 904 (facción B, raza 41). El sistema oficial de mods solo puede modificar héroes que ya existen, así
// que el plugin los da de alta:
//   - En HeroData: postfix de HeroData.LoadFromResourcesAndOverrides, que VACÍA y recarga los héroes en cada recarga de
//     mods (riesgo ALTO del diseño: un alta única al arrancar desaparecería en la primera recarga). Cada héroe se monta
//     de cero con sus textos (claves zeroHeroe9xx del mod oficial), su raza, su unidad emblema, su hechizo emblema (desde
//     el juego 1.11.1 tiene que estar registrado en SpellRegistry; si no, sustituto seguro con aviso: LogicaHechizos.cs;
//     desde la 0.7.4 puede ser un hechizo PROPIO 1900-1999 que inyecta Hechizos.cs, con «PlanB» oficial si falla), el
//     oro inicial y la condición de desbloqueo de un héroe de serie y su ejército inicial («muestra» del manifiesto: el
//     menú lo carga en la partida nueva y lo cobra del tesoro; 3 unidades comunes desde el 17-10-2026).
//     Hero es un struct con referencias: se mete en el diccionario y en la lista del juego llamando a set_Item/Add
//     directamente en IL2CPP con el struct desempaquetado (como AssetsGuardia), nunca por los genéricos de interop.
//   - En los assets de héroe (HeroAssetsData): una entrada por héroe con la figura 3D y el retrato PRESTADOS de un héroe
//     de serie (nivel a de §6.3; la figura propia es la F5). Así LoadHeroSpriteAsync y LoadHeroPrefabAsync nunca
//     encuentran la entrada vacía (riesgo ALTO: no comprueban nulos).
//   - Retrato propio: PNG del mod (heroes\9xx.png) convertido en Sprite; un prefix de LoadHeroSpriteAsync lo devuelve
//     ya cargado. Si el prefix no se pudiera usar, sale el retrato prestado (nunca vacío).
// Los bonus de batalla de los cuatro van por la vía oficial (hero_bonus_rules.json del mod, que escribe
// generar_mod_facciones.py) y los bonus del jugador de «Ejército reforzado» los añade EjercitoReforzado.cs.
// HeroesNuevos.cs
// Phase F2 (design §4.2, §5.2 and §6.3): the new heroes 901, 902 (faction A, race 40), 903 and
// 904 (faction B, race 41). The official mod system can only modify heroes that already exist, so
// the plugin registers them:
//   - In HeroData: postfix of HeroData.LoadFromResourcesAndOverrides, which EMPTIES and reloads the heroes on every mod
//     reload (design HIGH risk: a single registration at startup would vanish on the first reload). Each hero is built
//     from scratch with its texts (zeroHeroe9xx keys from the official mod), its race, its emblem unit, its emblem spell (since
//     game 1.11.1 it must be registered in SpellRegistry; if not, safe substitute with a warning: LogicaHechizos.cs;
//     since 0.7.4 it can be a CUSTOM spell 1900-1999 injected by Hechizos.cs, with an official «PlanB» if it fails), the
//     starting gold and the unlock condition of a stock hero, and its starting army (manifest «sample»: the
//     menu loads it in the new run and charges it to the treasury; 3 common units since 17-10-2026).
//     Hero is a struct with references: it is put into the dictionary and the game's list by calling set_Item/Add
//     directly in IL2CPP with the unboxed struct (like AssetsGuardia), never through the interop generics.
//   - In the hero assets (HeroAssetsData): one entry per hero with the 3D figure and portrait BORROWED from a stock hero
//     (level a of §6.3; the custom figure is F5). This way LoadHeroSpriteAsync and LoadHeroPrefabAsync never
//     find the entry empty (HIGH risk: they don't check for nulls).
//   - Custom portrait: mod PNG (heroes\9xx.png) converted into a Sprite; a prefix of LoadHeroSpriteAsync returns it
//     already loaded. If the prefix could not be used, the borrowed portrait is used (never empty).
// The battle bonuses of all four go through the official route (the mod's hero_bonus_rules.json, written by
// generar_mod_facciones.py) and the player bonuses of «Ejército reforzado» are added by EjercitoReforzado.cs.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: alta de los héroes nuevos en HeroData
    // Section: registration of the new heroes in HeroData
    // -----------------------------------------------------------------------
    public static unsafe class HeroesNuevos
    {
        private static bool _dentro;                                   // evita reentradas (GetHeroByID llama a EnsureLoaded)
                                                                       // avoids re-entrancy (GetHeroByID calls EnsureLoaded)
        private static readonly HashSet<string> _avisos = new HashSet<string>();
        private static readonly Dictionary<int, Sprite> _retratos = new Dictionary<int, Sprite>();
        private static readonly HashSet<int> _retratosFallidos = new HashSet<int>();
        private static int _altasSesion;

        /// <summary>Veces que se han dado de alta los héroes en esta sesión (una por recarga de HeroData).</summary>
        /// Number of times the heroes have been registered in this session (once per HeroData reload).
        public static int AltasSesion => _altasSesion;
        /// <summary>Cómo se entrega el retrato propio: «Task(ctor)», «Task.FromResult» o null si aún no se ha pedido.</summary>
        /// How the custom portrait is delivered: «Task(ctor)», «Task.FromResult» or null if it has not been requested yet.
        public static string ViaRetrato { get; private set; }

        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        // Postfix de HeroData.LoadFromResourcesAndOverrides: los héroes de serie ya están; se añaden los nuevos
        // Postfix of HeroData.LoadFromResourcesAndOverrides: the stock heroes are already there; the new ones get added
        public static void TrasCargarHeroes()
        {
            if (!Plugin.Operativo || !FaccionesNuevas.Activas || _dentro) return;
            if (!UnidadGuardia.ContenidoActivo()) return;      // sin módulos de contenido no hay unidades emblema ni textos
                                                               // without content modules there are no emblem units or texts
            _dentro = true;
            try
            {
                // 0.7.4: los hechizos propios tienen que estar en el registro ANTES de elegir el hechizo de cada héroe
                // (respaldo del postfix de SpellRegistry.Load por si el juego no pasa por él)
                // 0.7.4: custom spells must be in the registry BEFORE choosing each hero's spell
                // (fallback for the SpellRegistry.Load postfix in case the game doesn't go through it)
                Hechizos.Asegurar("HeroData");
                int puestos = AsegurarHeroes("HeroData");
                if (puestos > 0) _altasSesion++;
                var data = TabletopTavernData.InstanceIfExists;
                if (data != null) AsegurarAssetsHeroes(data, "HeroData");
            }
            catch (Exception e) { Plugin.Error("TrasCargarHeroes: " + e); }
            finally { _dentro = false; }
        }

        /// <summary>Da de alta en HeroData los héroes nuevos que falten. Devuelve cuántos ha añadido.</summary>
        /// Registers the missing new heroes in HeroData. Returns how many it added.
        public static int AsegurarHeroes(string origen)
        {
            var dic = HeroData._heroesByID;
            var lista = HeroData._heroesSorted;
            if (dic == null || lista == null || dic.Count == 0) return 0;
            int puestos = 0;
            // En orden de id: la lista del juego va ordenada por HeroID y los nuevos (9xx) son los más altos
            // In id order: the game's list is sorted by HeroID and the new ones (9xx) are the highest
            foreach (var h in FaccionesNuevas.Heroes.Values.OrderBy(x => x.Id))
            {
                if (dic.ContainsKey(h.Id)) continue;
                Hero nuevo = Montar(h);
                IntPtr datos = IL2CPP.il2cpp_object_unbox(nuevo.Pointer);
                InvocarDiccionario(dic.Pointer, "set_Item", h.Id, datos);
                try { InvocarLista(lista.Pointer, "Add", datos); }
                catch
                {
                    // Sin lista no sale en la pantalla de héroes: se deshace el alta en el diccionario para reintentar limpio
                    // Without a list it doesn't show up on the heroes screen: the dictionary registration is undone to retry cleanly
                    try { dic.Remove(h.Id); } catch { }
                    throw;
                }
                puestos++;
            }
            if (puestos > 0)
            {
                var comprobado = FaccionesNuevas.Heroes.Keys.Where(id => HeroData.GetHeroByID(id).HeroID == id).ToList();
                if (comprobado.Count == FaccionesNuevas.Heroes.Count)
                    Plugin.Info($"[{origen}] Héroes nuevos dados de alta: {string.Join(", ", comprobado)} (HeroData: {dic.Count} héroes, lista {lista.Count}).");
                else
                    Plugin.Error($"[{origen}] Héroes nuevos: solo {comprobado.Count}/{FaccionesNuevas.Heroes.Count} responden en GetHeroByID tras el alta.");
            }
            return puestos;
        }

        // ¿El módulo del héroe trae la historia larga «zeroHeroe<id>Lore»? Se mira en su localization_overrides.json (la carpeta
        // del módulo es la de encima de «heroes\», donde está su retrato). Si no se puede saber, la corta (2.ª ronda de la
        // revisión externa, hallazgo 10: así nunca sale la clave en bruto)
        // Does the hero's module include the long story «zeroHeroe<id>Lore»? It is checked in its localization_overrides.json (the module
        // folder is the one above «heroes\», where its portrait is). If it can't be determined, use the short one (2nd round of the
        // external review, finding 10: this way the raw key never shows up)
        private static bool ModuloTraeHistoriaLarga(Manifiesto.Heroe h)
        {
            try
            {
                string carpeta = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(h.Retrato ?? ""));
                string textos = string.IsNullOrEmpty(carpeta) ? null : System.IO.Path.Combine(carpeta, "localization_overrides.json");
                if (textos == null || !System.IO.File.Exists(textos)) return false;
                return System.IO.File.ReadAllText(textos).Contains("\"zeroHeroe" + h.Id + "Lore\"");
            }
            catch { return false; }
        }

        // Monta un héroe nuevo desde cero (struct empaquetado del juego) con los datos del manifiesto
        // Builds a new hero from scratch (the game's packed struct) with the manifest data
        private static Hero Montar(Manifiesto.Heroe h)
        {
            Hero prestado = HeroData.GetHeroByID(h.PrestadoDe > 0 ? h.PrestadoDe : HeroData.DefaultHeroID);
            Hero oro = HeroData.GetHeroByID(h.OroComo > 0 ? h.OroComo : HeroData.DefaultHeroID);
            Hero desbloqueo = HeroData.GetHeroByID(HeroData.DefaultHeroID);    // Edric: siempre desbloqueado
                                                                               // Edric: always unlocked
            var nuevo = new Hero();
            nuevo.HeroName = "zeroHeroe" + h.Id;
            nuevo.HeroDescription = "zeroHeroe" + h.Id + "Desc";
            nuevo.HeroBonusDescription = new Il2CppStringArray(new[] { $"zeroH{h.Id}heroBonusDescription1", $"zeroH{h.Id}heroBonusDescription2" });
            // Texto de historia bajo el modelo (pantalla de héroes y Colección: GetLoreString con esta clave). Desde el
            // 03-11-2026 es la historia larga «zeroHeroe<id>Lore», como las de serie; la corta «...Desc» queda para el
            // cuadro emergente. Si el módulo instalado no la trae, la Colección cae en la corta (InterfazColeccion.RellenarHistoria)
            // Revisión externa del 03-11 (hallazgo 6): con un módulo anterior (sin la clave larga), la corta, también en la
            // pantalla de selección
            // Story text under the model (heroes screen and Collection: GetLoreString with this key). Since
            // 03-11-2026 it is the long story «zeroHeroe<id>Lore», like the stock ones; the short «...Desc» is kept for the
            // popup. If the installed module doesn't include it, the Collection falls back to the short one (InterfazColeccion.RellenarHistoria)
            // External review of 03-11 (finding 6): with an older module (without the long key), the short one, also on the
            // selection screen
            nuevo.HeroPrefabName = "zeroHeroe" + h.Id + (ModuloTraeHistoriaLarga(h) ? "Lore" : "Desc");
            nuevo.UnlockCondition = desbloqueo.UnlockCondition;
            nuevo.DemoUnlockCondition = desbloqueo.DemoUnlockCondition;
            nuevo.StartingGold = oro.StartingGold;
            nuevo.HeroID = h.Id;
            nuevo.Race = (Race)h.Raza;
            nuevo.SignatureUnit = (UnitName)h.Emblema;
            // Juego 1.11.1 (01-11-2026): el hechizo tiene que estar REGISTRADO (SpellRegistry), no solo en el enum; si no, se
            // avisa y lleva un sustituto seguro (el del prestado o RallyTheBanners) en vez de quedarse con la ranura vacía.
            // 0.7.4: el pedido puede ser un id propio (1901...) y antes del prestado va el «PlanB» del manifiesto
            // Game 1.11.1 (01-11-2026): the spell must be REGISTERED (SpellRegistry), not just in the enum; otherwise a
            // warning is issued and it gets a safe substitute (the borrowed hero's or RallyTheBanners) instead of leaving the slot empty.
            // 0.7.4: the requested one can be a custom id (1901...) and before the borrowed hero's comes the manifest's «PlanB»
            string elegido = LogicaHechizos.Elegir(h.Id, h.Hechizo, HechizoEnEnum, EstadoEnRegistro, h.PlanB,
                                                    prestado.SignatureSpell.ToString(), out string avisoHechizo);
            if (avisoHechizo != null) AvisoUnaVez("hechizo" + h.Id, avisoHechizo);
            nuevo.SignatureSpell = AHechizo(elegido, out TJ.Spells.Spell hechizo) ? hechizo : prestado.SignatureSpell;
            var muestra = (h.Muestra ?? new List<int>()).Select(x => (UnitName)x).ToArray();
            // Revisión del 18-10 (L-10): decisión de Zero, 3 unidades iniciales por héroe. Si el manifiesto trae otra cosa
            // se avisa (el héroe se da de alta igual con lo que declare)
            // Review of 18-10 (L-10): Zero's decision, 3 starting units per hero. If the manifest has something else,
            // a warning is issued (the hero is still registered with whatever it declares)
            if (muestra.Length != 3) AvisoUnaVez("muestra" + h.Id, $"Héroe nuevo {h.Id}: su ejército inicial trae {muestra.Length} unidades y lo acordado son 3. Revisa «Muestra» en el manifiesto de su módulo.");
            nuevo.StartingArmyUnits = new Il2CppStructArray<UnitName>(muestra);
            return nuevo;
        }

        // ¿Existe el hechizo? Un nombre del enum Spell del juego (los números sueltos no cuentan: IsDefined) o, desde la 0.7.4,
        // un id propio del rango 1900-1999 (no está en el enum: lo inyecta Hechizos.cs)
        // Does the spell exist? A name from the game's Spell enum (bare numbers don't count: IsDefined) or, since 0.7.4,
        // a custom id in the 1900-1999 range (not in the enum: Hechizos.cs injects it)
        private static bool HechizoEnEnum(string nombre) => AHechizo(nombre, out _);

        // Texto del manifiesto -> valor del enum Spell: nombre del enum o id propio (1900-1999). false si no es ninguno
        // Manifest text -> Spell enum value: enum name or custom id (1900-1999). false if it's neither
        private static bool AHechizo(string texto, out TJ.Spells.Spell s)
        {
            int propio = LogicaHechizos.IdPropio(texto);
            if (propio >= 0) { s = (TJ.Spells.Spell)propio; return true; }
            return Enum.TryParse(texto, out s) && Enum.IsDefined(typeof(TJ.Spells.Spell), s);
        }

        // Lo que dice el registro de hechizos del juego de un nombre. Si no se puede consultar (tipo movido en una
        // actualización: el fallo de JIT sale en ConsultarRegistro, fuera de aquí), «desconocido» con un aviso
        // What the game's spell registry says about a name. If it can't be queried (type moved in an
        // update: the JIT failure surfaces in ConsultarRegistro, outside here), «desconocido» with a warning
        internal static LogicaHechizos.Estado EstadoEnRegistro(string nombre)
        {
            if (!AHechizo(nombre, out TJ.Spells.Spell s))
                return LogicaHechizos.Estado.NoRegistrado;
            try { return ConsultarRegistro(s) ? LogicaHechizos.Estado.Registrado : LogicaHechizos.Estado.NoRegistrado; }
            catch (Exception e)
            {
                AvisoUnaVez("registroHechizos", "No se pudo consultar el registro de hechizos del juego (" + e.GetType().Name + ": " +
                            e.Message + "); los hechizos de los héroes nuevos se aceptan si están en el enum, como antes.");
                return LogicaHechizos.Estado.Desconocido;
            }
        }

        // Llamada al registro aparte y sin inlining: si SpellRegistry se moviera, el TypeLoadException lo recoge el try de arriba
        // Separate registry call without inlining: if SpellRegistry were moved, the TypeLoadException is caught by the try above
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool ConsultarRegistro(TJ.Spells.Spell s) => TJ.Spells.SpellRegistry.Exists(s);

        // set_Item(int, Hero) del Dictionary<int, Hero> del juego, con el struct desempaquetado
        // set_Item(int, Hero) of the game's Dictionary<int, Hero>, with the struct unpacked
        private static void InvocarDiccionario(IntPtr dic, string metodo, int clave, IntPtr valor)
        {
            IntPtr clase = IL2CPP.il2cpp_object_get_class(dic);
            IntPtr m = IL2CPP.il2cpp_class_get_method_from_name(clase, metodo, 2);
            if (m == IntPtr.Zero) throw new MissingMethodException("Dictionary<int,Hero>", metodo);
            void** args = stackalloc void*[2];
            int k = clave;
            args[0] = &k;
            args[1] = (void*)valor;
            IntPtr excepcion = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(m, dic, args, ref excepcion);
            if (excepcion != IntPtr.Zero) throw new InvalidOperationException($"{metodo} lanzó una excepción dentro del juego");
        }

        // Add(Hero) de la List<Hero> del juego, con el struct desempaquetado. 0.7.4: también lo usa EjercitoReforzado para
        // añadir un UnitStatBonus (struct con referencias) a la lista de bonus que devuelve el juego
        // Add(Hero) of the game's List<Hero>, with the struct unpacked. 0.7.4: also used by EjercitoReforzado to
        // add a UnitStatBonus (struct with references) to the bonus list the game returns
        internal static void InvocarLista(IntPtr lista, string metodo, IntPtr valor)
        {
            IntPtr clase = IL2CPP.il2cpp_object_get_class(lista);
            IntPtr m = IL2CPP.il2cpp_class_get_method_from_name(clase, metodo, 1);
            if (m == IntPtr.Zero) throw new MissingMethodException("List<Hero>", metodo);
            void** args = stackalloc void*[1];
            args[0] = (void*)valor;
            IntPtr excepcion = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(m, lista, args, ref excepcion);
            if (excepcion != IntPtr.Zero) throw new InvalidOperationException($"{metodo} lanzó una excepción dentro del juego");
        }

        // ---------------------------------------------------------------
        // Assets de héroe: figura 3D y retrato prestados (nunca entrada vacía)
        // Hero assets: 3D figure and portrait borrowed (never an empty entry)
        // ---------------------------------------------------------------
        /// <summary>Añade a HeroAssetsData una entrada por héroe nuevo con figura y retrato de su héroe prestado.</summary>
        /// Adds to HeroAssetsData one entry per new hero with the figure and portrait of its borrowed hero.
        public static int AsegurarAssetsHeroes(TabletopTavernData data, string origen)
        {
            if (!FaccionesNuevas.Activas || data == null) return 0;
            var assets = data._heroAssetsData;
            if (assets == null || assets.Heroes == null || assets.Heroes.Count == 0) return 0;
            int puestos = 0;
            foreach (var h in FaccionesNuevas.Heroes.Values.OrderBy(x => x.Id))
            {
                if (assets.GetByID(h.Id) != null) continue;
                var prestada = assets.GetByID(h.PrestadoDe) ?? assets.GetByID(HeroData.DefaultHeroID) ?? assets.Heroes[0];
                var entrada = new HeroAssetEntry();
                entrada.HeroID = h.Id;
                entrada.Sprite = prestada.Sprite;     // misma referencia de Addressables: el retrato prestado de respaldo
                                                      // same Addressables reference: the borrowed fallback portrait
                entrada.Prefab = prestada.Prefab;     // la figura de la taberna (el gestor de Addressables cuenta referencias por clave)
                                                      // the tavern figure (the Addressables manager counts references per key)
                assets.Heroes.Add(entrada);
                puestos++;
            }
            if (puestos > 0)
                Plugin.Info($"[{origen}] Assets de héroe: {puestos} entradas nuevas (figura 3D y retrato de respaldo prestados: " +
                            $"{string.Join(", ", FaccionesNuevas.Heroes.Values.Select(x => $"{x.Id}<-{x.PrestadoDe}"))}).");
            return puestos;
        }

        // ---------------------------------------------------------------
        // Retratos propios (PNG del mod)
        // Custom portraits (mod PNG)
        // ---------------------------------------------------------------
        /// <summary>Sprite del retrato propio de un héroe nuevo (se carga del PNG la primera vez; null si falla).</summary>
        /// Sprite of a new hero's custom portrait (loaded from the PNG the first time; null on failure).
        public static Sprite Retrato(int heroID)
        {
            if (_retratos.TryGetValue(heroID, out var s) && s != null) return s;
            if (_retratosFallidos.Contains(heroID) || !FaccionesNuevas.Heroes.TryGetValue(heroID, out var h) || string.IsNullOrEmpty(h.Retrato)) return null;
            // 0.7.0: el retrato ya viene como ruta completa dentro de la carpeta de su módulo (Modulos.Fusionar)
            // 0.7.0: the portrait now comes as a full path inside its module's folder (Modulos.Fusionar)
            string ruta = h.Retrato.Replace('\\', Path.DirectorySeparatorChar);
            try
            {
                if (!File.Exists(ruta)) throw new FileNotFoundException("no existe", ruta);
                byte[] bytes = File.ReadAllBytes(ruta);
                var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                // 0.7.5 (revisión total, hallazgo 11): si el PNG no vale, la textura de 2x2 se destruye antes de salir
                // 0.7.5 (full review, finding 11): if the PNG is invalid, the 2x2 texture is destroyed before returning
                if (!ImageConversion.LoadImage(textura, bytes, false))
                {
                    UnityEngine.Object.Destroy(textura);
                    throw new InvalidDataException("no es un PNG válido");
                }
                textura.name = "zeroRetratoTex" + heroID;
                textura.wrapMode = TextureWrapMode.Clamp;
                textura.hideFlags = HideFlags.HideAndDontSave;
                var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = "zeroRetrato" + heroID;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                _retratos[heroID] = sprite;
                Plugin.Info($"Retrato del héroe {heroID} cargado: {ruta} ({textura.width}x{textura.height}).");
                return sprite;
            }
            catch (Exception e)
            {
                _retratosFallidos.Add(heroID);
                Plugin.Aviso($"Retrato del héroe {heroID}: no se pudo cargar {ruta} ({e.Message}); se usa el retrato prestado.");
                return null;
            }
        }

        /// <summary>Carga ya los retratos de todos los héroes nuevos (en el menú, desde el hilo principal).</summary>
        /// Loads the portraits of all new heroes right away (in the menu, from the main thread).
        public static int CargarRetratos()
        {
            int n = 0;
            foreach (int id in FaccionesNuevas.Heroes.Keys) if (Retrato(id) != null) n++;
            return n;
        }

        // Prefix de TabletopTavernData.LoadHeroSpriteAsync: para un héroe nuevo devuelve su retrato propio ya cargado (una
        // tarea terminada). Si no hay retrato o no se puede crear la tarea, sigue el juego con el retrato prestado.
        // Prefix of TabletopTavernData.LoadHeroSpriteAsync: for a new hero it returns its already loaded custom portrait (a
        // completed task). If there is no portrait or the task can't be created, the game continues with the borrowed portrait.
        public static bool AntesDeLoadHeroSpriteAsync(int __0, ref Il2CppSystem.Threading.Tasks.Task<Sprite> __result)
        {
            try
            {
                if (!FaccionesNuevas.EsHeroeNuevo(__0)) return true;
                var sprite = Retrato(__0);
                if (sprite == null) return true;
                var tarea = TareaTerminada(sprite);
                if (tarea == null) return true;
                __result = tarea;
                return false;
            }
            catch (Exception e)
            {
                AvisoUnaVez("retrato", "AntesDeLoadHeroSpriteAsync (se usa el retrato prestado): " + e.Message);
                return true;
            }
        }

        // Tarea ya terminada con el sprite: constructor interno Task<T>(T) o, si no existe en el juego, Task.FromResult
        // Already completed task with the sprite: internal constructor Task<T>(T) or, if it doesn't exist in the game, Task.FromResult
        private static Il2CppSystem.Threading.Tasks.Task<Sprite> TareaTerminada(Sprite sprite)
        {
            if (ViaRetrato != "Task.FromResult")
            {
                try
                {
                    var t = new Il2CppSystem.Threading.Tasks.Task<Sprite>(sprite);
                    if (ViaRetrato == null) { ViaRetrato = "Task(ctor)"; Plugin.Info("Retratos propios: tarea terminada con el constructor Task<Sprite>(sprite)."); }
                    return t;
                }
                catch (Exception e) { AvisoUnaVez("ctor", "Task<Sprite>(sprite) no disponible (" + e.Message + "); se prueba Task.FromResult."); }
            }
            try
            {
                var t = Il2CppSystem.Threading.Tasks.Task.FromResult<Sprite>(sprite);
                if (ViaRetrato != "Task.FromResult") { ViaRetrato = "Task.FromResult"; Plugin.Info("Retratos propios: tarea terminada con Task.FromResult."); }
                return t;
            }
            catch (Exception e)
            {
                AvisoUnaVez("fromresult", "Task.FromResult<Sprite> no disponible (" + e.Message + "); los héroes nuevos usan el retrato prestado.");
                return null;
            }
        }
    }
}
