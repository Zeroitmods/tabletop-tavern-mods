// Estandarte.cs
// Núcleo 0.7.7 (03-11-2026, petición de Zero con captura de la pestaña «Estandarte» de la preparación):
//   1) Colores de facción: presets de las facciones A y B (razas 40/41) en la fila «Colores de facción»
//      de TJ.MainMenu.WarbandBannerPanel, y la fila dentro de una ventana con desplazamiento horizontal por la rueda.
//   2) Iconos del estandarte: los declaran los módulos (efectos.iconosEstandarte; LogicaEstandarte.cs). Fila nueva con
//      el selector (icono del héroe, iconos de serie y los de los módulos activos); la elección va al registro del núcleo
//      (nunca al save) y se pinta en la vista previa (postfix de Refresh) y en las banderas de las escuadras del jugador
//      en batalla (postfix de SquadFlagGameObject.ApplyTeamColors, campo squadId > 0).
// Segundo cierre del 03-11 (AccessViolation en get_SquadId): SquadFlagGameObject.Start y get_SquadId son funciones
// PLEGADAS en GameAssembly (el compilador junta en una sola dirección miles de métodos con el mismo código: Start vacío con
// 3446 Dispose de iteradores; get_SquadId con otros getters): parchear Start desviaba todas esas funciones al postfix, con
// objetos que no eran banderas. Ahora el enganche es ApplyTeamColors (0x270 bytes, solo lo llama SetUp al final; xref) y
// el squadId se lee del campo (acceso directo a memoria, sin llamar al juego). Nada de structs il2cpp por valor ni out.
// Evidencia del juego 1.11.1 y diseño: notas internas (no publicadas) (el mapa de campaña no pinta el estandarte).
// Reglas de seguridad (notas internas (no publicadas) y §7 de notas internas (no publicadas)):
//   - solo se parchean métodos sin argumentos o con objetos (EnsureBuilt, Refresh, ApplyTeamColors); los que llevan el
//     struct Colours (blittable) se LLAMAN (Apply, PickPreset), nunca se parchean;
//   - NUNCA Start/Awake/OnEnable/Update vacíos ni getters o setters triviales: en GameAssembly pueden estar PLEGADOS con
//     miles de métodos; todo método parcheado tiene que tener un xref que pruebe que no lo está (los tres de aquí, 03-11:
//     EnsureBuilt 0xB39 bytes, Refresh 0x315, ApplyTeamColors 0x270, ninguno plegado);
//   - además, cada enganche comprueba que __instance es de verdad de su clase (red por si una versión nueva lo plegara).
// Autor: ZeroitMods.
// Estandarte.cs
// Core 0.7.7 (03-11-2026, request from Zero with a screenshot of the «Estandarte» tab of the setup screen):
//   1) Faction colors: presets for factions A and B (races 40/41) in the «Colores de facción» row
//      of TJ.MainMenu.WarbandBannerPanel, and the row inside a window with horizontal scrolling via the mouse wheel.
//   2) Banner icons: declared by the modules (efectos.iconosEstandarte; LogicaEstandarte.cs). New row with
//      the selector (hero icon, built-in icons and those of the active modules); the choice goes to the core registry
//      (never to the save) and is drawn in the preview (Refresh postfix) and on the flags of the player's squads
//      in battle (postfix of SquadFlagGameObject.ApplyTeamColors, field squadId > 0).
// Second fix of 03-11 (AccessViolation in get_SquadId): SquadFlagGameObject.Start and get_SquadId are
// FOLDED functions in GameAssembly (the compiler merges thousands of methods with the same code into a single address: empty Start with
// 3446 iterator Dispose methods; get_SquadId with other getters): patching Start redirected all those functions to the postfix, with
// objects that were not flags. Now the hook is ApplyTeamColors (0x270 bytes, only called by SetUp at the end; xref)
// and squadId is read from the field (direct memory access, without calling the game). No il2cpp structs by value or out.
// Evidence from game 1.11.1 and design: internal notes (unpublished) (the campaign map does not draw the banner).
// Safety rules (internal notes (unpublished) and §7 of internal notes (unpublished)):
//   - only methods with no arguments or with objects are patched (EnsureBuilt, Refresh, ApplyTeamColors); those that take the
//     Colours struct (blittable) are CALLED (Apply, PickPreset), never patched;
//   - NEVER empty Start/Awake/OnEnable/Update or trivial getters or setters: in GameAssembly they may be FOLDED with
//     thousands of methods; every patched method must have an xref proving it is not folded (the three here, 03-11:
//     EnsureBuilt 0xB39 bytes, Refresh 0x315, ApplyTeamColors 0x270, none folded);
//   - in addition, each hook checks that __instance really is of its class (a safety net in case a new version folds it).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppInterop.Runtime;
using TJ;
using TJ.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace ZeroNucleo
{
    public static class Estandarte
    {
        // -------------------------------------------------------------------
        // Sección: estado
        // Section: state
        // -------------------------------------------------------------------
        private const string VentanaPresets = "zeroVentanaPresets", VentanaIconos = "zeroVentanaIconos", FilaIconos = "zeroFilaIconos";
        private const string PrefijoPreset = "zeroPreset", PrefijoIcono = "zeroIcono", HijoImagen = "zeroIconoImagen", Separador = "zeroSeparador";
        private const string CodigoHeroe = "";
        private const string PropiedadIcono = "_IconSprite";
        private const string ModuloHeraldica = "ZeroHeraldica";   // su pack va detrás de los iconos de facción
                                                                  // its pack goes after the faction icons
        private const string TituloIconos = "zeroTituloIconos";   // propiedad del icono en el material de las banderas (GetFlagMaterial)   // botón «Icono del héroe» (= sin elección)
                                                                  // icon property in the flags' material (GetFlagMaterial)   // «Icono del héroe» button (= no choice)

        private static List<IconoEstandarte> _modulos = new List<IconoEstandarte>();      // iconos de los módulos activos
                                                                                          // icons of the active modules
        private static readonly Dictionary<string, Texture2D> _texturas = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> _fallidos = new HashSet<string>();
        private static List<(string codigo, Texture textura, string clave, string respaldo)> _juego;   // iconos de serie
                                                                                                       // built-in icons
        private static readonly HashSet<string> _avisos = new HashSet<string>();
        private static string _memoria;                 // elección si no hay registro del núcleo
                                                        // choice if there is no core registry
        private static Texture _iconoHeroe;             // último icono que puso el juego (unidad emblema del héroe)
                                                        // last icon set by the game (the hero's emblem unit)
        private static readonly List<Boton> _botones = new List<Boton>();

        // Textos propios ya puestos en la interfaz (títulos y avisos emergentes) con su clave, para rehacerlos al cambiar el
        // idioma del juego (petición de Zero del 03-11: «según cambie el idioma, el mod también»). Los clones se crean sin el
        // componente de localización del juego, así que sin esto se quedarían en el idioma del momento del montaje
        // Custom texts already placed in the interface (titles and tooltips) along with their key, to rebuild them when the game
        // language changes (request from Zero on 03-11: «when the language changes, the mod should too»). The clones are created without the
        // game's localization component, so without this they would stay in the language in use at build time
        private sealed class TextoVivo
        {
            public TMPro.TMP_Text Texto;                              // título (o null)
                                                                      // title (or null)
            public Memori.Tooltip.MemoriTooltipTrigger Aviso;         // aviso emergente de un botón (o null)
                                                                      // tooltip of a button (or null)
            public string Clave, Es, En;
            public float Retardo;
            public string Idioma;                                     // código de idioma con el que se puso el texto
                                                                      // language code the text was set with
        }
        // Una entrada por objeto (clave: puntero IL2CPP del texto o del aviso), así un montaje repetido no la duplica
        // One entry per object (key: IL2CPP pointer of the text or the tooltip), so a repeated build does not duplicate it
        private static readonly Dictionary<IntPtr, TextoVivo> _textosVivos = new Dictionary<IntPtr, TextoVivo>();

        /// <summary>Banderas del jugador con el icono elegido en esta sesión (autoprueba y log).</summary>
        /// Player flags with the icon chosen in this session (self-test and log).
        public static int BanderasSesion { get; private set; }
        /// <summary>Veces que se ha puesto el icono elegido en la vista previa en esta sesión.</summary>
        /// Number of times the chosen icon has been set in the preview in this session.
        public static int VistasSesion { get; private set; }

        // Un botón del selector: su código, sus imágenes teñibles (con el hueco del estandarte que copian) y su anillo
        // A selector button: its code, its tintable images (with the banner slot they copy) and its ring
        private sealed class Boton
        {
            public string Codigo;
            public GameObject Raiz;
            public GameObject Anillo;
            public Image Icono;
            public List<(Graphic g, BannerSlot hueco)> Teñibles = new List<(Graphic, BannerSlot)>();
        }

        // Red contra el plegado (revisión del segundo arreglo, hallazgo 1): ¿el objeto nativo es de verdad de la clase T?
        // Safety net against folding (review of the second fix, finding 1): is the native object really of class T?
        private static bool EsDe<T>(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase o) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
        {
            try
            {
                IntPtr clase = IL2CPP.il2cpp_object_get_class(o.Pointer);
                IntPtr esperada = Il2CppClassPointerStore<T>.NativeClassPtr;
                if (clase == esperada) return true;
                AvisoUnaVez("clase" + typeof(T).Name, $"un enganche recibió un objeto que no es {typeof(T).Name}; se ignora (¿método plegado en esta versión del juego?).");
                return false;
            }
            catch { return false; }
        }

        /// <summary>¿Están preparadas las cachés? (autoprueba y Plugin)</summary>
        /// Are the caches ready? (self-test and Plugin)
        public static bool Preparado => _preparado;

        /// <summary>Iconos de los módulos activos (lo llama Nucleo.Aplicar).</summary>
        /// Icons of the active modules (called by Nucleo.Aplicar).
        public static void Configurar(ResultadoModulos r)
        {
            _modulos = r?.IconosEstandarte?.ToList() ?? new List<IconoEstandarte>();
            _fallidos.Clear();   // revisión externa 0.7.7, hallazgo 10
                                 // external review 0.7.7, finding 10
            // Ronda 2 del arreglo, hallazgo 1: módulos nuevos = cachés por rehacer; si los datos ya están, ahora mismo
            // Round 2 of the fix, finding 1: new modules = caches to rebuild; if the data is already there, right now
            _preparado = false;
            try
            {
                var datos = TabletopTavernData.InstanceIfExists;
                if (datos != null && datos.SquadAssetsDictionary != null && datos.SquadAssetsDictionary.Count > 0) Preparar(datos);
            }
            catch (Exception e) { AvisoUnaVez("prepararConfigurar", e.Message); }
        }

        /// <summary>¿Hay algún icono de módulo? (sin ninguno no hay selector; los presets 40/41 van con las facciones).</summary>
        /// Is there any module icon? (with none there is no selector; presets 40/41 go with the factions).
        public static bool HayIconos => _modulos.Count > 0;
        /// <summary>Número de iconos de módulo activos.</summary>
        /// Number of active module icons.
        public static int IconosModulos => _modulos.Count;

        private static void AvisoUnaVez(string clave, string texto)
        {
            if (_avisos.Add(clave)) Plugin.Aviso("Estandarte: " + texto);
        }

        // -------------------------------------------------------------------
        // Sección: elección guardada y texturas
        // Section: saved choice and textures
        // -------------------------------------------------------------------
        /// <summary>Código guardado (registro del núcleo; sin registro, en memoria). Null o vacío = el de serie.</summary>
        /// Saved code (core registry; with no registry, in memory). Null or empty = the built-in one.
        public static string Guardado => Saves.Registro != null ? Saves.Registro.IconoEstandarte : _memoria;

        private static void Elegir(string codigo)
        {
            string c = string.IsNullOrEmpty(codigo) ? null : codigo;
            _memoria = c;
            try { Saves.Registro?.FijarIconoEstandarte(c); }
            catch (Exception e) { AvisoUnaVez("guardar", "no se pudo guardar el icono elegido en el registro: " + e.Message); }
            Plugin.Info($"Estandarte: icono elegido «{c ?? "del héroe"}».");
        }

        // Arreglos del 03-11 (la 0.7.7 no cargaba): la causa de fondo era que SquadFlagGameObject.Start está PLEGADO con miles
        // de métodos vacíos, así que el postfix corría en todos ellos (con FindObjectsOfTypeAll dentro, recursión y Stack
        // overflow; sin él, AccessViolation leyendo SquadId de objetos que no eran banderas). Ahora el enganche de batalla es
        // ApplyTeamColors (no plegado), las cachés se preparan en Preparar() (ReloadData, al cargar el plugin, al cambiar los
        // módulos y, si aún faltan, en EnsureBuilt, que no es de ciclo de vida) y los enganches de vista previa y bandera solo
        // leen las cachés.
        // Fixes from 03-11 (0.7.7 did not load): the root cause was that SquadFlagGameObject.Start is FOLDED with thousands
        // of empty methods, so the postfix ran on all of them (with FindObjectsOfTypeAll inside, recursion and Stack
        // overflow; without it, AccessViolation reading SquadId from objects that were not flags). Now the battle hook is
        // ApplyTeamColors (not folded), the caches are prepared in Preparar() (ReloadData, when the plugin loads, when the
        // modules change and, if still missing, in EnsureBuilt, which is not a lifecycle method) and the preview and flag hooks only
        // read the caches.
        private static bool _preparado;
        // Guardas de reentrada, una por enganche (revisión del arreglo, ronda 2, hallazgo 3)
        // Reentry guards, one per hook (review of the fix, round 2, finding 3)
        private static bool _dentroEnsure, _dentroRefresh, _dentroFlag;
        private static string _ultimoResumen;   // para no repetir el log de Preparar (hallazgo 6)
                                                // so the Preparar log is not repeated (finding 6)

        /// <summary>Códigos que se pueden elegir ahora (serie + módulos activos con su PNG cargado). Solo cachés.</summary>
        /// Codes that can be chosen now (built-in + active modules with their PNG loaded). Caches only.
        private static HashSet<string> Disponibles()
        {
            var d = new HashSet<string>(StringComparer.Ordinal);
            if (_juego != null) foreach (var j in _juego) if (j.textura != null) d.Add(j.codigo);
            foreach (var m in _modulos) if (_texturas.TryGetValue(m.Codigo, out var t) && t != null) d.Add(m.Codigo);
            return d;
        }

        /// <summary>Textura del icono elegido y disponible, o null (= el de serie). Solo cachés: vale en cualquier enganche.</summary>
        /// Texture of the chosen icon if available, or null (= the built-in one). Caches only: valid in any hook.
        public static Texture TexturaElegida()
        {
            string c = LogicaEstandarte.ElegidoDisponible(Guardado, Disponibles());
            return c == null ? null : TexturaDe(c);
        }

        // Textura de un código desde las cachés (nunca carga ni busca)
        // Texture for a code from the caches (never loads or searches)
        private static Texture TexturaDe(string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return null;
            if (codigo.StartsWith(LogicaEstandarte.PrefijoJuego, StringComparison.Ordinal))
            {
                if (_juego != null) foreach (var j in _juego) if (j.codigo == codigo && j.textura != null) return j.textura;
                return null;
            }
            return _texturas.TryGetValue(codigo, out var t) && t != null ? t : null;
        }

        /// <summary>
        /// Prepara las cachés una vez por recarga de datos: iconos de serie desde SquadAssetsDictionary (la misma fuente que
        /// usa el juego: GetSquadTypeFlagSprite = SquadAssetsDictionary[unidad].squadIcon.FlagSprite) y los PNG de los
        /// módulos. La llama UnidadGuardia.TrasReloadData; el postfix de EnsureBuilt solo si aún no se hizo. Sin
        /// FindObjectsOfType* (prueba 31).
        /// Prepares the caches once per data reload: built-in icons from SquadAssetsDictionary (the same source the game
        /// uses: GetSquadTypeFlagSprite = SquadAssetsDictionary[unit].squadIcon.FlagSprite) and the PNGs of the
        /// modules. Called by UnidadGuardia.TrasReloadData; the EnsureBuilt postfix only if it has not been done yet. Without
        /// FindObjectsOfType* (test 31).
        /// </summary>
        public static void Preparar(TabletopTavernData data)
        {
            if (data == null) return;   // ronda 2, hallazgo 4: sin depender de módulos ni facciones
                                        // round 2, finding 4: without depending on modules or factions
            // Iconos de serie: unidades de serie (por debajo de 900) que están en el diccionario de assets
            // Built-in icons: built-in units (below 900) that are in the assets dictionary
            var lista = new List<(string, Texture, string, string)>();
            try
            {
                var dic = data.SquadAssetsDictionary;
                var vistos = new HashSet<string>(StringComparer.Ordinal);
                foreach (UnitName u in Enum.GetValues(typeof(UnitName)))
                {
                    if ((int)u >= 900 || dic == null || !dic.ContainsKey(u)) continue;
                    Texture tex = null;
                    try { tex = data.GetSquadTypeFlagSprite(u)?.texture; } catch { }
                    if (tex == null || string.IsNullOrEmpty(tex.name) || !vistos.Add(tex.name)) continue;
                    lista.Add((LogicaEstandarte.CodigoJuego(tex.name), tex, LogicaEstandarte.ClaveTexturaJuego(tex.name), LogicaEstandarte.NombreLegible(tex.name)));
                }
            }
            catch (Exception e) { AvisoUnaVez("juego", "no se pudieron leer los iconos de serie: " + e.Message); }
            if (lista.Count > 0 || _juego == null || _juego.Count == 0)   // ronda 2, hallazgo 5: una recarga vacía no borra la buena
                                                                          // round 2, finding 5: an empty reload does not overwrite the good one
                _juego = lista.OrderBy(x => x.Item4, StringComparer.Ordinal).ToList();
            // PNG de los módulos
            // Module PNGs
            int cargados = 0;
            foreach (var m in _modulos) if (Cargar(m) != null) cargados++;
            // Revisión del arreglo, hallazgo 1: «preparado» solo con los iconos de serie leídos y cada PNG de módulo intentado
            // (cargado o fallido); si no, EnsureBuilt (no es de ciclo de vida) y la siguiente recarga lo vuelven a intentar
            // Review of the fix, finding 1: «ready» only with the built-in icons read and each module PNG attempted
            // (loaded or failed); otherwise, EnsureBuilt (not a lifecycle method) and the next reload try again
            _preparado = _juego.Count > 0 && _modulos.All(m => _texturas.ContainsKey(m.Codigo) || _fallidos.Contains(m.Codigo));
            string resumen = $"{_juego.Count} iconos de serie y {cargados} de {_modulos.Count} de módulos preparados";
            if (resumen != _ultimoResumen) { _ultimoResumen = resumen; Plugin.Info("Estandarte: " + resumen + "."); }
        }

        // PNG del módulo a textura (como los retratos: HideAndDontSave para que Unity no la descargue). Solo desde Preparar
        // Module PNG to texture (like the portraits: HideAndDontSave so Unity does not unload it). Only from Preparar
        private static Texture2D Cargar(IconoEstandarte ic)
        {
            if (_texturas.TryGetValue(ic.Codigo, out var t) && t != null) return t;
            if (_fallidos.Contains(ic.Codigo)) return null;
            try
            {
                if (!File.Exists(ic.Ruta)) throw new FileNotFoundException("no existe", ic.Ruta);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(ic.Ruta), false))
                {
                    UnityEngine.Object.Destroy(tex);
                    throw new InvalidDataException("no es un PNG válido");
                }
                tex.name = "zeroEstandarte_" + ic.Modulo + "_" + ic.Id;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.hideFlags = HideFlags.HideAndDontSave;
                _texturas[ic.Codigo] = tex;
                return tex;
            }
            catch (Exception e)
            {
                _fallidos.Add(ic.Codigo);
                AvisoUnaVez("png" + ic.Codigo, $"no se pudo cargar el icono {ic.Codigo} ({ic.Ruta}): {e.Message}; no sale en el selector.");
                return null;
            }
        }

        // Sprite (para el botón del selector) de una textura cualquiera, cacheado por código
        // Sprite (for the selector button) from any texture, cached by code
        private static Sprite SpriteDe(string codigo, Texture textura)
        {
            if (textura == null) return null;
            if (_sprites.TryGetValue(codigo, out var s) && s != null && s.texture != null && s.texture.Pointer == textura.Pointer) return s;
            var t2 = textura.TryCast<Texture2D>();
            if (t2 == null) return null;
            if (s != null) UnityEngine.Object.Destroy(s);   // revisión externa 0.7.7, hallazgo 10: el sustituido, fuera
                                                            // external review 0.7.7, finding 10: the replaced one, out
            s = Sprite.Create(t2, new Rect(0, 0, t2.width, t2.height), new Vector2(0.5f, 0.5f), 100f);
            s.name = "zeroSpriteEstandarte_" + codigo;
            s.hideFlags = HideFlags.HideAndDontSave;
            _sprites[codigo] = s;
            return s;
        }

        // Pone un título propio y lo apunta para rehacerlo al cambiar de idioma
        // Sets a custom title and records it to rebuild it when the language changes
        private static void PonerTitulo(TMPro.TMP_Text t, string clave, string es, string en)
        {
            if (t == null) return;
            t.text = Texto(clave, es, en);
            _textosVivos[t.Pointer] = new TextoVivo { Texto = t, Clave = clave, Es = es, En = en, Idioma = AccionesHeroes.CodigoIdioma() };
        }

        /// <summary>
        /// Quita los textos cuyo objeto ya no existe (panel destruido) y rehace los que se pusieron en otro idioma. Cada
        /// entrada guarda su idioma y solo se marca como hecha si el texto se pudo poner (revisión externa del 03-11,
        /// hallazgos 1-3). Ojo: «v.Texto != null» usa el operador de UnityEngine.Object (Il2CppInterop), que da nulo para un
        /// objeto destruido; no cambiarlo por «is null», «?.» ni ReferenceEquals.
        /// Removes the texts whose object no longer exists (destroyed panel) and redoes those that were set in another language. Each
        /// entry stores its language and is only marked as done if the text could be set (external review of 03-11,
        /// findings 1-3). Careful: «v.Texto != null» uses the UnityEngine.Object operator (Il2CppInterop), which gives null for a
        /// destroyed object; do not change it to «is null», «?.» or ReferenceEquals.
        /// </summary>
        private static void RefrescarTextos()
        {
            if (_textosVivos.Count == 0) return;
            string codigo = AccionesHeroes.CodigoIdioma();
            int hechos = 0;
            foreach (var par in _textosVivos.ToList())
            {
                var v = par.Value;
                bool vivo;
                try { vivo = v.Texto != null || v.Aviso != null; } catch { vivo = false; }
                if (!vivo) { _textosVivos.Remove(par.Key); continue; }
                if (v.Idioma == codigo) continue;
                try
                {
                    string texto = Texto(v.Clave, v.Es, v.En);
                    if (v.Texto != null) v.Texto.text = texto;
                    else v.Aviso.SetUpToolTip(texto, "", "", v.Retardo);
                    v.Idioma = codigo;
                    hechos++;
                }
                catch (Exception e) { AvisoUnaVez("texto" + v.Clave, $"no se pudo rehacer el texto «{v.Clave}» en el idioma nuevo: {e.Message}"); }
            }
            if (hechos > 0) Plugin.Info($"Estandarte: {hechos} textos propios puestos en el idioma «{codigo}».");
        }

        // Texto localizado (clave del juego o de un módulo) con respaldo en español o inglés según el idioma del juego
        // Localized text (game or module key) with a Spanish or English fallback depending on the game's language
        private static string Texto(string clave, string es, string en)
        {
            try
            {
                var loc = Memori.Localization.LocalizationManager.Instance;
                if (loc != null && !string.IsNullOrEmpty(clave))
                {
                    string t = loc.GetText(clave);
                    if (!string.IsNullOrWhiteSpace(t) && t.Trim() != clave) return t;
                }
            }
            catch { }
            bool espanol = false;
            try { espanol = AccionesHeroes.JuegoEnEspanol(); } catch { }
            return (espanol ? es : en) ?? es ?? en ?? clave;
        }

        // -------------------------------------------------------------------
        // Sección: parches
        // Section: patches
        // -------------------------------------------------------------------
        /// <summary>Postfix de WarbandBannerPanel.EnsureBuilt: presets 40/41, ventana con rueda y selector de iconos.</summary>
        /// Postfix of WarbandBannerPanel.EnsureBuilt: presets 40/41, window with wheel and icon selector.
        public static void TrasEnsureBuilt(WarbandBannerPanel __instance)
        {
            // Guarda de reentrada (arreglo del Stack overflow del 03-11): si Unity vuelve a entrar en un enganche del
            // estandarte mientras otro corre, no se hace nada
            // Reentry guard (fix for the Stack overflow of 03-11): if Unity re-enters a banner hook
            // while another is running, nothing is done
            if (_dentroEnsure) return;
            _dentroEnsure = true;
            try
            {
                var p = __instance;
                if (p == null || !EsDe<WarbandBannerPanel>(p)) return;
                if (p == null || p.presetParent == null) return;
                // Ronda 2, hallazgo 2: InstanceIfExists (Instance podría buscar o crear el objeto dentro de OnEnable)
                // Round 2, finding 2: InstanceIfExists (Instance might search for or create the object inside OnEnable)
            if (!_preparado) { try { Preparar(TabletopTavernData.InstanceIfExists); } catch (Exception e) { AvisoUnaVez("preparar", e.Message); } }
                RectTransform fila = p.presetParent.TryCast<RectTransform>();
                if (fila == null) return;
                // Plantilla (un preset de serie, antes de tocar nada) y su raza
                // Template (a built-in preset, before touching anything) and its race
                Button plantilla = Plantilla(fila);
                if (plantilla == null) { AvisoUnaVez("plantilla", "la fila «Colores de facción» no tiene botones de serie; no se añade nada."); return; }
                Race razaPlantilla = RazaDe(plantilla);
                try { Envolver(fila, VentanaPresets); }
                catch (Exception e) { AvisoUnaVez("rueda", "no se pudo poner la rueda en «Colores de facción»: " + e.Message); }
                try { if (FaccionesNuevas.Activas) AnadirPresets(p, fila, plantilla, razaPlantilla); }
                catch (Exception e) { AvisoUnaVez("presets", "no se pudieron añadir los colores de las facciones nuevas: " + e.Message); }
                try { if (HayIconos) MontarSelector(p, fila, plantilla, razaPlantilla); }
                catch (Exception e) { AvisoUnaVez("selector", "no se pudo montar el selector de iconos: " + e.Message); }
                // Textos propios en el idioma actual (por si se cambió el idioma con el panel ya montado)
                // Custom texts in the current language (in case the language was changed with the panel already built)
                try { RefrescarTextos(); }
                catch (Exception e) { AvisoUnaVez("textos", "no se pudieron rehacer los textos al cambiar de idioma: " + e.Message); }
            }
            finally { _dentroEnsure = false; }
        }

        /// <summary>
        /// Postfix de WarbandBannerPanel.Refresh (lo llaman Commit, LoadForHero y ResetBanner justo después de
        /// BannerPreview.Apply): pone el icono elegido en la vista previa y repinta el selector.
        /// Postfix of WarbandBannerPanel.Refresh (called by Commit, LoadForHero and ResetBanner right after
        /// BannerPreview.Apply): sets the chosen icon in the preview and repaints the selector.
        /// </summary>
        public static void TrasRefresh(WarbandBannerPanel __instance)
        {
            // Guarda de reentrada (arreglo del Stack overflow del 03-11): si Unity vuelve a entrar en un enganche del
            // estandarte mientras otro corre, no se hace nada
            // Reentry guard (fix for the Stack overflow of 03-11): if Unity re-enters a banner hook
            // while another is running, nothing is done
            if (_dentroRefresh) return;
            _dentroRefresh = true;
            try
            {
                try
                {
                    if (__instance == null || !EsDe<WarbandBannerPanel>(__instance)) return;
                    // Idioma cambiado con el panel montado: títulos y avisos propios al idioma nuevo
                    // Language changed with the panel built: custom titles and tooltips to the new language
                    try { RefrescarTextos(); }
                    catch (Exception e) { AvisoUnaVez("textos", "no se pudieron rehacer los textos al cambiar de idioma: " + e.Message); }
                    var prev = __instance._preview;
                    var mat = prev?._material;
                    if (mat == null) return;
                    var actual = mat.GetTexture(BannerPreview.IconID);
                    var elegida = TexturaElegida();
                    // Ronda 2 de la revisión, hallazgos 3 y 4: el del héroe, del dato del juego; si no se puede leer, del material
                    // (solo si no es nuestro ni nulo)
                    // Sin leer _hero (struct Hero por valor, no blittable; prueba 28): el del héroe es lo que pone el juego
                    // Round 2 of the review, findings 3 and 4: the hero's, from the game data; if it cannot be read, from the material
                    // (only if it is neither ours nor null)
                    // Without reading _hero (Hero struct by value, not blittable; test 28): the hero's is whatever the game sets
                    if (actual != null && !EsNuestra(actual) && (elegida == null || actual.Pointer != elegida.Pointer)) _iconoHeroe = actual;
                    if (elegida != null && (actual == null || actual.Pointer != elegida.Pointer))
                    {
                        _ultimaAplicada = elegida;
                        try { prev.Apply(__instance._colours, elegida); }
                        catch { mat.SetTexture(BannerPreview.IconID, elegida); }
                        VistasSesion++;
                    }
                    RepintarSelector(__instance);
                }
                catch (Exception e) { AvisoUnaVez("refresh", "no se pudo poner el icono en la vista previa: " + e.Message); }
            }
            finally { _dentroRefresh = false; }
        }

        // Revisión externa 0.7.7, hallazgo 1: ¿es una textura de un módulo o la última que se aplicó desde el selector?
        // External review 0.7.7, finding 1: is it a texture from a module or the last one applied from the selector?
        private static Texture _ultimaAplicada;
        private static bool EsNuestra(Texture t)
        {
            if (t == null) return false;
            if (_ultimaAplicada != null && _ultimaAplicada.Pointer == t.Pointer) return true;
            foreach (var x in _texturas.Values) if (x != null && x.Pointer == t.Pointer) return true;
            return false;
        }

        /// <summary>
        /// Postfix de SquadFlagGameObject.ApplyTeamColors (privado, sin argumentos; lo llama SetUp al final, con el material y
        /// la escuadra ya puestos): en las banderas del jugador (campo squadId &gt; 0),
        /// el icono elegido en vez del de su tipo de unidad. El material es propio de esa bandera (GetFlagMaterial).
        /// Postfix of SquadFlagGameObject.ApplyTeamColors (private, no arguments; SetUp calls it at the end, with the material and
        /// the squad already set): on the player's banners (field squadId &gt; 0),
        /// the chosen icon instead of the one for its unit type. The material belongs to that banner alone (GetFlagMaterial).
        /// </summary>
        public static void TrasApplyTeamColors(SquadFlagGameObject __instance)
        {
            // Guarda de reentrada (arreglo del Stack overflow del 03-11): si Unity vuelve a entrar en un enganche del
            // estandarte mientras otro corre, no se hace nada
            // Reentry guard (fix for the Stack overflow of 03-11): if Unity re-enters a banner hook
            // while another is running, nothing is done
            if (_dentroFlag) return;
            _dentroFlag = true;
            try
            {
                try
                {
                    if (__instance == null || !EsDe<SquadFlagGameObject>(__instance)) return;
                    if (!_preparado && !string.IsNullOrEmpty(Guardado))
                        AvisoUnaVez("sinPreparar", "hay un icono elegido pero los iconos no están preparados (sin ReloadData tras cargar los módulos); las banderas llevan el de serie.");
                    var tex = TexturaElegida();
                    // Campo squadId (lectura directa), no la propiedad SquadId (método plegado del juego)
                    // Field squadId (direct read), not the SquadId property (the game's folded method)
                    if (!LogicaEstandarte.BanderaLlevaIcono(__instance.squadId, tex != null)) return;
                    var mat = __instance.flagMeshRenderer?.sharedMaterial;
                    if (mat == null) return;
                    // El material es propio de cada bandera: SquadManager.GetFlagMaterial hace «new Material(raceFlagBaseMaterial)»
                    // por escuadra (código público y xref de la 1.11.1). No se usa MaterialPropertyBlock: la bandera pone el suyo
                    // (escuadra rota, selección) con SetPropertyBlock y lo pisaría. Revisión externa 0.7.7, hallazgos 2 y 8
                    // The material is unique to each flag: SquadManager.GetFlagMaterial does «new Material(raceFlagBaseMaterial)»
                    // per squad (public code and xref of 1.11.1). MaterialPropertyBlock is not used: the flag sets its own
                    // (broken squad, selection) with SetPropertyBlock and would overwrite it. External review 0.7.7, findings 2 and 8
                    if (!mat.HasProperty(PropiedadIcono))
                    {
                        AvisoUnaVez("sinPropiedad", $"el material de la bandera «{mat.name}» no tiene {PropiedadIcono}; no se cambia el icono en batalla.");
                        return;
                    }
                    mat.SetTexture(PropiedadIcono, tex);
                    if (BanderasSesion++ == 0) Plugin.Info($"Estandarte: icono «{tex.name}» puesto en las banderas del jugador en batalla.");
                }
                catch (Exception e) { AvisoUnaVez("bandera", "no se pudo poner el icono en una bandera de batalla: " + e.Message); }
            }
            finally { _dentroFlag = false; }
        }

        // -------------------------------------------------------------------
        // Sección: fila «Colores de facción»
        // Section: row «Colores de facción»
        // -------------------------------------------------------------------
        // Último botón de serie activo de la fila (los nuestros llevan prefijo)
        // Last active stock button in the row (ours carry a prefix)
        private static Button Plantilla(RectTransform fila)
        {
            Button b = null;
            for (int i = 0; i < fila.childCount; i++)
            {
                var h = fila.GetChild(i);
                if (h == null || h.name.StartsWith("zero", StringComparison.Ordinal) || !h.gameObject.activeSelf) continue;
                var bt = h.GetComponent<Button>();
                if (bt != null) b = bt;
            }
            return b;
        }

        // Raza de un preset de serie: la de serie cuyos colores casan con más imágenes del botón
        // Race of a stock preset: the stock one whose colors match the most button images
        private static Race RazaDe(Button preset)
        {
            var graficos = preset.GetComponentsInChildren<Graphic>(true);
            Race mejor = Race.IronLegion; int max = -1;
            foreach (Race r in Enum.GetValues(typeof(Race)))
            {
                // Solo razas de serie jugables (sin «Especial» ni la Liga Olímpica «próximamente», como en InterfazColeccion)
                // Only playable stock races (without «Especial» or the Olympic League «próximamente», as in InterfazColeccion)
                if ((int)r >= 40 || r == Race.Special || r == Race.OlympianLeague) continue;
                ArmyBanners.Colours c;
                try { c = ArmyBanners.FactionColours(r); } catch { continue; }
                int n = 0;
                for (int i = 0; i < graficos.Length; i++)
                    if (graficos[i] != null && Hueco(graficos[i].color, c) != null) n++;
                if (n > max) { max = n; mejor = r; }
            }
            if (max <= 0)
            {
                var colores = string.Join(", ", graficos.Where(g => g != null).Select(g => $"{g.name} #{ColorUtility.ToHtmlStringRGB(g.color)}"));
                AvisoUnaVez("raza", $"no se reconoce la raza del preset «{preset.name}» por sus colores ({colores}); los nuevos se ven como él.");
            }
            return mejor;
        }

        // Hueco del estandarte (Principal, Secundario, Ribete) al que corresponde un color, o null
        // Banner slot (Principal, Secundario, Ribete) that a color corresponds to, or null
        private static BannerSlot? Hueco(Color x, ArmyBanners.Colours c)
        {
            if (Parecido(x, c.Main)) return BannerSlot.Main;
            if (Parecido(x, c.Second)) return BannerSlot.Second;
            if (Parecido(x, c.Trim)) return BannerSlot.Trim;
            return null;
        }

        private static bool Parecido(Color a, Color b) =>
            Math.Abs(a.r - b.r) < 0.06f && Math.Abs(a.g - b.g) < 0.06f && Math.Abs(a.b - b.b) < 0.06f;

        private static Color DeHueco(ArmyBanners.Colours c, BannerSlot h) => h == BannerSlot.Main ? c.Main : h == BannerSlot.Second ? c.Second : c.Trim;

        // Imágenes del clon que copian un hueco del estandarte (según los colores de la raza de la plantilla)
        // Clone images that copy a banner slot (according to the template race's colors)
        private static List<(Graphic, BannerSlot)> Teñibles(GameObject clon, ArmyBanners.Colours plantilla)
        {
            var res = new List<(Graphic, BannerSlot)>();
            var graficos = clon.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; graficos != null && i < graficos.Length; i++)
            {
                var g = graficos[i];
                if (g == null) continue;
                var h = Hueco(g.color, plantilla);
                if (h.HasValue) res.Add((g, h.Value));
            }
            return res;
        }

        private static void Teñir(List<(Graphic g, BannerSlot h)> teñibles, ArmyBanners.Colours c)
        {
            foreach (var (g, h) in teñibles)
            {
                if (g == null) continue;
                var nuevo = DeHueco(c, h);
                g.color = new Color(nuevo.r, nuevo.g, nuevo.b, g.color.a);
            }
        }

        // Apaga el anillo de «elegido» que el clon pueda haber copiado (nombre WarbandBannerPanel.RingName); lo devuelve
        // Turns off the «chosen» ring that the clone may have copied (name WarbandBannerPanel.RingName); returns it
        private static GameObject Anillo(GameObject clon)
        {
            string nombre = null;
            try { nombre = WarbandBannerPanel.RingName; } catch { }
            if (string.IsNullOrEmpty(nombre)) return null;
            var ts = clon.GetComponentsInChildren<Transform>(true);
            for (int i = 0; ts != null && i < ts.Length; i++)
                if (ts[i] != null && ts[i].name == nombre) { ts[i].gameObject.SetActive(false); return ts[i].gameObject; }
            return null;
        }

        // Tooltip del clon (MemoriTooltipTrigger, como los de serie) con un título
        // Clone tooltip (MemoriTooltipTrigger, like the stock ones) with a title
        private static void Tooltip(GameObject clon, string clave, string es, string en, float retardo)
        {
            var tt = clon.GetComponentInChildren<Memori.Tooltip.MemoriTooltipTrigger>(true);
            if (tt == null) return;
            tt.SetUpToolTip(Texto(clave, es, en), "", "", retardo);
            _textosVivos[tt.Pointer] = new TextoVivo { Aviso = tt, Clave = clave, Es = es, En = en, Retardo = retardo, Idioma = AccionesHeroes.CodigoIdioma() };   // ver RefrescarTextos
                                                                                                                                                                   // see RefrescarTextos
        }

        private static float Retardo(Button plantilla)
        {
            try { var tt = plantilla.GetComponentInChildren<Memori.Tooltip.MemoriTooltipTrigger>(true); return tt != null ? tt.Delay : 0.3f; }
            catch { return 0.3f; }
        }

        // Clon de la plantilla, sin oyentes en tiempo de ejecución, sin anillo y listo para usar
        // Clone of the template, with no runtime listeners, no ring and ready to use
        private static GameObject Clonar(Button plantilla, Transform padre, string nombre)
        {
            var clon = UnityEngine.Object.Instantiate(plantilla.gameObject, padre, false);
            clon.name = nombre;
            clon.SetActive(true);
            var b = clon.GetComponent<Button>();
            // Revisión externa 0.7.7, hallazgo 5: evento nuevo (RemoveAllListeners no quita los persistentes del prefab)
            // External review 0.7.7, finding 5: new event (RemoveAllListeners does not remove the prefab's persistent ones)
            if (b != null) { b.onClick = new Button.ButtonClickedEvent(); b.interactable = true; }
            return clon;
        }

        // Presets 40/41: clic = PickPreset del juego (lee los colores del RaceData propio que registra el núcleo)
        // Presets 40/41: click = the game's PickPreset (reads the colors from the custom RaceData that the core registers)
        private static void AnadirPresets(WarbandBannerPanel p, RectTransform fila, Button plantilla, Race razaPlantilla)
        {
            var cPlantilla = ArmyBanners.FactionColours(razaPlantilla);
            float retardo = Retardo(plantilla);
            int creados = 0;
            foreach (Race r in FaccionesNuevas.RazasJugables)
            {
                string nombre = PrefijoPreset + (int)r;
                if (fila.Find(nombre) != null) continue;
                var clon = Clonar(plantilla, fila, nombre);
                Teñir(Teñibles(clon, cPlantilla), ArmyBanners.FactionColours(r));
                Anillo(clon);
                Tooltip(clon, ((int)r).ToString(), FaccionesNuevas.NombreEs((int)r), FaccionesNuevas.NombreEn((int)r), retardo);
                var boton = clon.GetComponent<Button>();
                var panel = p; var raza = r;
                boton.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() =>
                {
                    try { if (panel != null) panel.PickPreset(boton, raza); }
                    catch (Exception e) { AvisoUnaVez("pick" + (int)raza, $"preset {(int)raza}: {e.Message}"); }
                })));
                creados++;
            }
            if (creados > 0)
            {
                Plugin.Info($"Estandarte: {creados} colores de facción nuevos (clonados de {razaPlantilla}); fila: {Describir(fila)}.");
                Reajustar(fila);
            }
        }

        // -------------------------------------------------------------------
        // Sección: ventana con desplazamiento horizontal (rueda del ratón)
        // Section: window with horizontal scrolling (mouse wheel)
        // -------------------------------------------------------------------
        /// <summary>
        /// Mete la fila en una ventana del mismo tamaño y sitio con ScrollRect horizontal + RectMask2D. Unity pasa la rueda
        /// a desplazamiento horizontal cuando el vertical está apagado. La fila crece con su contenido (ContentSizeFitter) y
        /// queda centrada: si cabe, se ve como antes. Solo con HorizontalLayoutGroup; si no, se deja como está.
        /// Puts the row in a window of the same size and place with a horizontal ScrollRect + RectMask2D. Unity turns the wheel
        /// into horizontal scrolling when vertical is turned off. The row grows with its content (ContentSizeFitter) and
        /// is centered: if it fits, it looks as before. Only with HorizontalLayoutGroup; otherwise, it is left as is.
        /// </summary>
        private static RectTransform Envolver(RectTransform fila, string nombre)
        {
            if (fila.parent != null && fila.parent.name == nombre) return fila.parent.TryCast<RectTransform>();
            if (fila.GetComponent<HorizontalLayoutGroup>() == null)
            {
                AvisoUnaVez("sinlayout" + nombre, $"la fila «{fila.name}» no tiene HorizontalLayoutGroup; sin rueda ({Describir(fila)}).");
                return null;
            }
            var padre = fila.parent;
            if (padre == null) return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(fila);
            float ancho = fila.rect.width, alto = fila.rect.height;
            // Revisión externa 0.7.7, hallazgo 6: cada botón conserva su ancho (sin LayoutElement, el preferido podría ser el
            // del sprite o 0 al ajustar la fila a su contenido)
            // External review 0.7.7, finding 6: each button keeps its width (without LayoutElement, the preferred width could be that
            // of the sprite or 0 when fitting the row to its content)
            for (int i = 0; i < fila.childCount; i++)
            {
                var h = fila.GetChild(i)?.TryCast<RectTransform>();
                if (h == null || h.GetComponent<LayoutElement>() != null) continue;
                var leh = h.gameObject.AddComponent<LayoutElement>();
                leh.preferredWidth = h.rect.width; leh.minWidth = h.rect.width;
                leh.preferredHeight = h.rect.height;
            }
            var go = new GameObject(nombre);
            go.layer = fila.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(padre, false);
            rt.SetSiblingIndex(fila.GetSiblingIndex());
            rt.anchorMin = fila.anchorMin; rt.anchorMax = fila.anchorMax; rt.pivot = fila.pivot;
            rt.anchoredPosition = fila.anchoredPosition; rt.sizeDelta = fila.sizeDelta; rt.localScale = fila.localScale;
            // En un padre con LayoutGroup ocupa lo mismo que la fila
            // In a parent with LayoutGroup it takes up the same as the row
            var le = go.AddComponent<LayoutElement>();
            var leFila = fila.GetComponent<LayoutElement>();
            le.preferredWidth = ancho; le.preferredHeight = alto; le.minHeight = alto;
            if (leFila != null)
            {
                le.ignoreLayout = leFila.ignoreLayout; le.flexibleWidth = leFila.flexibleWidth; le.flexibleHeight = leFila.flexibleHeight; le.layoutPriority = leFila.layoutPriority;
                leFila.preferredWidth = -1f; leFila.minWidth = -1f; leFila.flexibleWidth = -1f;   // ronda 2, hallazgo 2
                                                                                                  // round 2, finding 2
            }
            // Fondo transparente que recoge la rueda también entre botones
            // Transparent background that also catches the wheel between buttons
            var fondo = go.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0f);
            fondo.raycastTarget = true;
            go.AddComponent<RectMask2D>();
            var sr = go.AddComponent<ScrollRect>();
            sr.horizontal = true; sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.inertia = true; sr.scrollSensitivity = 30f;
            sr.viewport = rt;
            // La fila dentro: centrada, de la altura de la ventana y del ancho de su contenido
            // The row inside: centered, the height of the window and the width of its content
            fila.SetParent(rt, false);
            fila.localScale = Vector3.one;
            fila.anchorMin = new Vector2(0.5f, 0f); fila.anchorMax = new Vector2(0.5f, 1f); fila.pivot = new Vector2(0.5f, 0.5f);
            fila.anchoredPosition = Vector2.zero; fila.sizeDelta = new Vector2(ancho, 0f);
            var csf = fila.GetComponent<ContentSizeFitter>() ?? fila.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            sr.content = fila;
            Plugin.Info($"Estandarte: ventana con rueda «{nombre}» de {ancho:0}x{alto:0} en «{padre.name}» ({(padre.GetComponent<LayoutGroup>() != null ? "con" : "sin")} LayoutGroup).");
            return rt;
        }

        // Tras añadir botones: rehace la maquetación y deja la vista al principio
        // After adding buttons: rebuilds the layout and leaves the view at the start
        private static void Reajustar(RectTransform fila)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(fila);
            var sr = fila.parent?.GetComponent<ScrollRect>();
            // Revisión externa 0.7.7, hallazgo 6: al principio solo si no cabe (si cabe, se queda centrada como antes)
            // External review 0.7.7, finding 6: at the start only if it does not fit (if it fits, it stays centered as before)
            var vista = fila.parent?.TryCast<RectTransform>();
            if (sr != null && vista != null && fila.name == FilaIconos && fila.rect.width > vista.rect.width + 0.5f)
            {
                // Prueba de Zero del 03-11 (la vista salía al final de la fila): contenido anclado a la izquierda, al principio
                // Zero's test of 03-11 (the view appeared at the end of the row): content anchored to the left, at the start
                fila.anchorMin = new Vector2(0f, fila.anchorMin.y); fila.anchorMax = new Vector2(0f, fila.anchorMax.y);
                fila.pivot = new Vector2(0f, fila.pivot.y);
                fila.anchoredPosition = new Vector2(0f, fila.anchoredPosition.y);
                sr.velocity = Vector2.zero;
            }
        }

        private static string Describir(RectTransform rt)
        {
            if (rt == null) return "-";
            return $"«{rt.name}» {rt.rect.width:0}x{rt.rect.height:0}, {rt.childCount} hijos, padre «{rt.parent?.name}»";
        }

        // -------------------------------------------------------------------
        // Sección: selector de iconos
        // Section: icon selector
        // -------------------------------------------------------------------
        /// <summary>
        /// Fila de iconos: con el padre de la ventana en un VerticalLayoutGroup, una fila nueva igual debajo de «Colores de
        /// facción» (con su título clonado si lo hay); si no, los iconos van en la misma fila tras un hueco. Botones clonados
        /// del preset de serie, teñidos con los colores actuales del estandarte y con el icono encima.
        /// Icon row: with the window's parent in a VerticalLayoutGroup, a new identical row below «Colores de
        /// facción» (with its cloned title if any); otherwise, the icons go in the same row after a gap. Buttons cloned
        /// from the stock preset, tinted with the banner's current colors and with the icon on top.
        /// </summary>
        private static void MontarSelector(WarbandBannerPanel p, RectTransform fila, Button plantilla, Race razaPlantilla)
        {
            _botones.RemoveAll(b => b.Raiz == null);
            // Ronda 3 del arreglo: sin cachés aún, no se monta nada (el siguiente EnsureBuilt lo vuelve a intentar)
            // Round 3 of the fix: no caches yet, nothing is built (the next EnsureBuilt tries again)
            if (!_preparado || _juego == null) { AvisoUnaVez("selectorEspera", "iconos aún sin preparar; el selector se montará al volver a abrir la pestaña."); return; }
            var ventana = fila.parent != null && fila.parent.name == VentanaPresets ? fila.parent.TryCast<RectTransform>() : null;
            var contenedor = ventana != null ? ventana.parent : null;
            RectTransform destino;
            bool filaPropia = contenedor != null && contenedor.GetComponent<VerticalLayoutGroup>() != null;
            if (filaPropia)
            {
                var existente = contenedor.Find(VentanaIconos);
                if (existente != null && _botones.Count > 0) return;   // ya montado
                                                                       // already built
                if (existente != null) { existente.name = "zeroVieja"; UnityEngine.Object.DestroyImmediate(existente.gameObject); }   // hallazgo 7
                                                                                                                                      // finding 7
                destino = FilaNueva(fila, contenedor, ventana);
            }
            else if (contenedor != null && BuscarHijo(contenedor, VentanaIconos) != null && _botones.Count > 0) return;   // ya montada
                                                                                                                          // already built
            else if (contenedor != null && (destino = SeccionPropia(p, fila, ventana, contenedor)) != null) { filaPropia = true; }
            else
            {
                if (fila.GetComponent<HorizontalLayoutGroup>() == null)
                {
                    AvisoUnaVez("sinSelector", $"ni fila propia (sin VerticalLayoutGroup en «{contenedor?.name}») ni HorizontalLayoutGroup en la fila de colores: no hay selector ({Describir(fila)}).");
                    return;
                }
                if (fila.Find(PrefijoIcono + "0") != null && _botones.Count > 0) return;
                destino = fila;
                var hueco = new GameObject(Separador);
                hueco.AddComponent<RectTransform>().SetParent(fila, false);
                var le = hueco.AddComponent<LayoutElement>();
                le.preferredWidth = 24f; le.minWidth = 24f;
                AvisoUnaVez("mismaFila", $"el panel no tiene VerticalLayoutGroup ({contenedor?.name}); los iconos van en la fila de colores, tras un hueco.");
            }

            var cPlantilla = ArmyBanners.FactionColours(razaPlantilla);
            float retardo = Retardo(plantilla);
            // Lista: héroe + serie + módulos
            // List: hero + stock + modules
            var entradas = new List<(string codigo, Texture tex, string clave, string es, string en)>();
            // Icono del héroe: el último que puso el juego en la vista previa (sin leer el struct Hero)
            // Hero icon: the last one the game put in the preview (without reading the Hero struct)
            var deEste = _iconoHeroe;
            if (deEste != null) _iconoHeroe = deEste;
            entradas.Add((CodigoHeroe, deEste, "zeroIconoEstandarteHeroe", "Icono del héroe", "Hero's icon"));
            // Orden (prueba de Zero del 03-11): héroe, los de las facciones activas, los de Heráldica y los del juego
            // Order (Zero's test from 03-11): hero, those of the active factions, those of Heráldica and those of the game
            foreach (var m in _modulos.Where(x => x.Modulo != ModuloHeraldica).Concat(_modulos.Where(x => x.Modulo == ModuloHeraldica)))
            {
                var t = TexturaDe(m.Codigo);   // solo la caché (Preparar)
                                               // cache only (Preparar)
                if (t != null) entradas.Add((m.Codigo, t, m.Clave, m.NombreEs ?? m.Id, m.NombreEn ?? m.Id));
            }
            foreach (var j in _juego) entradas.Add((j.codigo, j.textura, j.clave, j.respaldo, j.respaldo));
            var panel = p;
            int i = 0;
            foreach (var (codigo, tex, clave, tEs, tEn) in entradas)
            {
                var clon = Clonar(plantilla, destino, PrefijoIcono + i++);
                var b = new Boton { Codigo = codigo, Raiz = clon, Anillo = Anillo(clon) };
                // Prueba de Zero del 03-11: sin el escudo de colores de fondo (el icono se leía mezclado). Se quedan el gráfico
                // del botón, el mayor de sus hijos (el rombo) y el anillo; lo demás (escudo y sus cuarteles) se apaga
                // Zero's test from 03-11: without the colored shield in the background (the icon read as blended). The button's graphic,
                // the largest of its children (the diamond) and the ring stay; everything else (shield and its quarters) is turned off
                QuitarEscudo(clon, b.Anillo, cPlantilla);
                b.Teñibles = new List<(Graphic, BannerSlot)>();
                // Icono encima, al 76 % del botón, sin recoger clics
                // Icon on top, at 76% of the button, without picking up clicks
                var ico = new GameObject(HijoImagen);
                var irt = ico.AddComponent<RectTransform>();
                irt.SetParent(clon.transform, false);
                irt.anchorMin = new Vector2(0.12f, 0.12f); irt.anchorMax = new Vector2(0.88f, 0.88f);
                irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
                var img = ico.AddComponent<Image>();
                img.preserveAspect = true; img.raycastTarget = false;
                img.sprite = SpriteDe(codigo.Length == 0 ? "heroe" : codigo, tex);
                img.enabled = img.sprite != null;
                b.Icono = img;
                Tooltip(clon, clave, tEs, tEn, retardo);
                var boton = clon.GetComponent<Button>();
                string elegido = codigo;
                boton.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() =>
                {
                    try
                    {
                        Elegir(elegido);
                        if (panel == null) return;
                        // Revisión externa 0.7.7, hallazgo 1: Refresh no aplica; se pone primero el icono del héroe (como haría el
                        // juego) y el postfix de Refresh pone encima el elegido, si lo hay
                        // External review 0.7.7, finding 1: Refresh does not apply; the hero icon is set first (as the
                        // game would) and the Refresh postfix puts the chosen one on top, if there is one
                        var heroe = _iconoHeroe;   // el último que puso el juego (sin leer el struct Hero)
                                                   // the last one the game set (without reading the Hero struct)
                        if (heroe != null && panel._preview != null) { _ultimaAplicada = null; _iconoHeroe = heroe; panel._preview.Apply(panel._colours, heroe); }
                        panel.Refresh();
                    }
                    catch (Exception e) { AvisoUnaVez("clicIcono", "al elegir un icono: " + e.Message); }
                })));
                _botones.Add(b);
            }
            Reajustar(destino);
            Plugin.Info($"Estandarte: selector con {entradas.Count} iconos ({_juego.Count} de serie, {_modulos.Count} de módulos) en {(filaPropia ? "una fila propia" : "la fila de colores")}.");
            RepintarSelector(p);
        }

        // Sitio original del botón «Restablecer» de cada panel (para no bajarlo dos veces al rehacer la sección)
        // Original position of the "Restablecer" button of each panel (so it is not lowered twice when the section is rebuilt)
        private static readonly Dictionary<IntPtr, Vector3> _posResetOriginal = new Dictionary<IntPtr, Vector3>();

        // Quita la sección «Iconos» de un montaje anterior (título y ventana con su fila)
        // Removes the «Iconos» section from a previous setup (title and window with its row)
        private static void Limpiar(Transform contenedor)
        {
            var zona = BuscarHijo(contenedor, ZonaVerticalNombre);
            if (zona != null)
            {
                var sr = zona.GetComponent<ScrollRect>();
                if (sr != null) sr.verticalNormalizedPosition = 1f;   // arriba: las piezas vuelven a su sitio de pantalla
                                                                      // above: the pieces go back to their place on screen
                var cont = sr != null && sr.content != null ? (Transform)sr.content : BuscarHijo(zona, ZonaVerticalNombre + "Contenido");
                if (cont != null)
                    for (int i = cont.childCount - 1; i >= 0; i--)
                    {
                        var h = cont.GetChild(i)?.TryCast<RectTransform>();
                        if (h == null || h.name == VentanaIconos || h.name == TituloIconos) continue;
                        if (_origenPiezas.TryGetValue(h.Pointer, out var o) && o.padre != null)
                        {
                            CambiarPadreFijo(h, o.padre);
                            h.SetSiblingIndex(Math.Min(o.orden, Math.Max(0, o.padre.childCount - 1)));
                        }
                        else CambiarPadreFijo(h, contenedor);
                    }
                zona.name = "zeroVieja";
                UnityEngine.Object.DestroyImmediate(zona.gameObject);
            }
            foreach (var n in new[] { VentanaIconos, TituloIconos })
                for (var t = BuscarHijo(contenedor, n); t != null; t = BuscarHijo(contenedor, n))
                { t.name = "zeroVieja"; UnityEngine.Object.DestroyImmediate(t.gameObject); }
            _botones.RemoveAll(b => b.Raiz == null);
        }

        // Hijo (a cualquier profundidad) con ese nombre, o null
        // Child (at any depth) with that name, or null
        private static Transform BuscarHijo(Transform raiz, string nombre)
        {
            var ts = raiz.GetComponentsInChildren<Transform>(true);
            for (int i = 0; ts != null && i < ts.Length; i++) if (ts[i] != null && ts[i].name == nombre) return ts[i];
            return null;
        }

        // Mueve un RectTransform un vector medido en el espacio de «espacio» (convertido al espacio de su padre)
        // Moves a RectTransform by a vector measured in the space of «espacio» (converted to its parent's space)
        private static void MoverEn(RectTransform rt, Transform espacio, Vector3 delta)
        {
            var mundo = espacio.TransformVector(delta);
            var local = rt.parent != null ? rt.parent.InverseTransformVector(mundo) : mundo;
            rt.localPosition = rt.localPosition + local;
        }

        // Caja de un RectTransform en el espacio local de otro (abajo, arriba, izquierda, derecha)
        // Box of one RectTransform in the local space of another (bottom, top, left, right)
        private static (float abajo, float arriba, float izq, float der) Caja(RectTransform rt, Transform espacio)
        {
            var esquinas = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
            rt.GetWorldCorners(esquinas);
            float ab = float.MaxValue, ar = float.MinValue, iz = float.MaxValue, de = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var l = espacio.InverseTransformPoint(esquinas[i]);
                ab = Math.Min(ab, l.y); ar = Math.Max(ar, l.y); iz = Math.Min(iz, l.x); de = Math.Max(de, l.x);
            }
            return (ab, ar, iz, de);
        }

        /// <summary>
        /// Sección propia «Iconos» en un panel SIN LayoutGroup (pruebas de Zero del 03-11). Bajo la ventana de «Colores de
        /// facción» van el título (clon del de los colores, texto «Iconos») y una ventana con rueda del mismo ancho, con el MISMO
        /// hueco título→fila que tienen los colores (medido). Nunca se encoge nada ni se solapa: «Restablecer colores de facción»
        /// baja debajo de la sección y, si así se saldría del panel, la zona desde el título de los colores hasta ese botón va
        /// dentro de una ventana con desplazamiento VERTICAL por la rueda (decisión de Zero: mejor desplazar que encoger).
        /// Devuelve la fila donde van los botones (o null: plan B, misma fila de colores).
        /// Own «Iconos» section in a panel WITHOUT LayoutGroup (Zero's tests from 03-11). Under the «Colores de
        /// facción» window go the title (clone of the colors one, text «Iconos») and a window with a wheel of the same width, with the SAME
        /// title→row gap that the colors have (measured). Nothing is ever shrunk or overlapped: «Restablecer colores de facción»
        /// moves below the section and, if that would take it out of the panel, the area from the colors title down to that button goes
        /// inside a window with VERTICAL wheel scrolling (Zero's decision: better to scroll than to shrink).
        /// Returns the row where the buttons go (or null: plan B, same colors row).
        /// </summary>
        private static RectTransform SeccionPropia(WarbandBannerPanel p, RectTransform fila, RectTransform ventana, Transform contenedor)
        {
            if (ventana == null || fila.GetComponent<HorizontalLayoutGroup>() == null) return null;
            var panel = contenedor.TryCast<RectTransform>();
            if (panel == null) return null;
            const float hueco = 8f;
            // Restos de un montaje anterior fuera y «Restablecer» en su sitio original antes de medir
            // Leftovers from a previous setup removed and «Restablecer» in its original place before measuring
            Limpiar(contenedor);
            var reset = p.resetButton != null ? p.resetButton.transform.TryCast<RectTransform>() : null;
            if (reset != null && _posResetOriginal.TryGetValue(reset.Pointer, out var orig)) reset.localPosition = orig;
            Canvas.ForceUpdateCanvases();
            var cajaPanel = Caja(panel, contenedor);
            var cajaColores = Caja(ventana, contenedor);
            if (cajaColores.arriba - cajaColores.abajo < 1f || cajaColores.der - cajaColores.izq < 1f) return null;
            float altoFila = cajaColores.arriba - cajaColores.abajo;

            // Título de los colores: el texto (sin botón) justo encima de la ventana de colores (a menos de 40 de ella)
            // Colors title: the text (no button) right above the colors window (less than 40 from it)
            RectTransform tituloColores = null; float mejor = float.MaxValue;
            var textos = contenedor.GetComponentsInChildren<TMPro.TMP_Text>(true);
            for (int i = 0; textos != null && i < textos.Length; i++)
            {
                var t = textos[i];
                if (t == null || t.GetComponentInParent<Button>() != null || t.name.StartsWith("zero")) continue;
                var rt = t.transform.TryCast<RectTransform>();
                if (rt == null) continue;
                var c = Caja(rt, contenedor);
                float d = c.abajo - cajaColores.arriba;
                if (d >= -4f && d <= 40f && d < mejor && c.izq < cajaColores.der && c.der > cajaColores.izq) { mejor = d; tituloColores = rt; }
            }
            // Relación título→fila de los colores (de la parte de arriba del título a la de arriba de la fila): la misma abajo
            // Colors title→row relationship (from the top of the title to the top of the row): the same below
            var cajaTit = tituloColores != null ? Caja(tituloColores, contenedor) : cajaColores;
            float relTitulo = tituloColores != null ? cajaTit.arriba - cajaColores.arriba : 0f;

            // Posiciones (espacio del panel): título arriba, fila debajo con el mismo hueco, «Restablecer» debajo de la fila
            // Positions (panel space): title on top, row below with the same gap, «Restablecer» below the row
            float topeTitulo = cajaColores.abajo - hueco;                 // arriba del título nuevo
                                                                          // top of the new title
            float topeFila = topeTitulo - relTitulo;                      // arriba de la fila nueva
                                                                          // top of the new row
            float bajoFila = topeFila - altoFila;
            (float abajo, float arriba, float izq, float der) cajaReset = reset != null ? Caja(reset, contenedor) : (bajoFila, bajoFila, 0f, 0f);
            float bajar = reset != null ? Math.Max(0f, cajaReset.arriba - (bajoFila - hueco)) : 0f;   // lo que baja el botón
                                                                                                      // how far the button moves down

            // Título «Iconos»
            // Title «Iconos»
            if (tituloColores != null)
            {
                var padreTit = tituloColores.parent != null && tituloColores.parent.GetComponent<LayoutGroup>() == null ? tituloColores.parent : contenedor;
                var tit = UnityEngine.Object.Instantiate(tituloColores.gameObject, padreTit, true);
                tit.name = TituloIconos;
                var comps = tit.GetComponentsInChildren<Component>(true);
                for (int i = 0; comps != null && i < comps.Length; i++)
                    if (comps[i] != null && comps[i].TryCast<TMPro.TMP_Text>() == null
                        && comps[i].GetIl2CppType().Name.IndexOf("Locali", StringComparison.OrdinalIgnoreCase) >= 0)
                        UnityEngine.Object.DestroyImmediate(comps[i]);
                var txt = tit.GetComponentInChildren<TMPro.TMP_Text>(true);
                PonerTitulo(txt, "zeroIconosEstandarteSeccion", "Iconos", "Icons");
                var trt = tit.transform.TryCast<RectTransform>();
                MoverEn(trt, contenedor, new Vector3(0f, topeTitulo - Caja(trt, contenedor).arriba, 0f));
            }
            // Fila de iconos en su ventana con rueda (mismo ancho y alto que la de colores, sin encoger)
            // Icon row in its window with a wheel (same width and height as the colors one, without shrinking)
            var nueva = new GameObject(FilaIconos);
            nueva.layer = fila.gameObject.layer;
            var rtF = nueva.AddComponent<RectTransform>();
            rtF.SetParent(contenedor, false);
            rtF.anchorMin = rtF.anchorMax = rtF.pivot = new Vector2(0.5f, 0.5f);
            float ancho = cajaColores.der - cajaColores.izq;
            rtF.sizeDelta = new Vector2(ancho, altoFila);
            var hlg = fila.GetComponent<HorizontalLayoutGroup>();
            var h2 = nueva.AddComponent<HorizontalLayoutGroup>();
            h2.spacing = hlg.spacing; h2.padding = hlg.padding; h2.childAlignment = hlg.childAlignment;
            h2.childControlWidth = hlg.childControlWidth; h2.childControlHeight = hlg.childControlHeight;
            h2.childForceExpandWidth = false; h2.childForceExpandHeight = hlg.childForceExpandHeight;
            var v = Envolver(rtF, VentanaIconos);
            if (v == null) { UnityEngine.Object.Destroy(nueva); Limpiar(contenedor); return null; }
            var cv = Caja(v, contenedor);
            float cx = (cajaColores.izq + cajaColores.der) / 2f;
            MoverEn(v, contenedor, new Vector3(cx - (cv.izq + cv.der) / 2f, topeFila - cv.arriba, 0f));
            // «Restablecer» debajo de la fila (se guarda su sitio original para rehacer sin bajarlo dos veces)
            // «Restablecer» below the row (its original place is saved to redo without moving it down twice)
            if (bajar > 0f && reset != null)
            {
                if (!_posResetOriginal.ContainsKey(reset.Pointer)) _posResetOriginal[reset.Pointer] = reset.localPosition;
                MoverEn(reset, contenedor, new Vector3(0f, -bajar, 0f));
            }
            var cajaFinal = Caja(v, contenedor);
            (float abajo, float arriba, float izq, float der) cajaResetFinal = reset != null ? Caja(reset, contenedor) : cajaReset;
            // Si «Restablecer» se sale del panel: desplazamiento vertical de la zona colores + iconos + botón
            // If «Restablecer» goes outside the panel: vertical scrolling of the colors + icons + button area
            float abajoTodo = Math.Min(cajaFinal.abajo, cajaResetFinal.abajo);
            bool vertical = abajoTodo < cajaPanel.abajo + 2f;
            if (vertical)
            {
                float arribaZona = (tituloColores != null ? cajaTit.arriba : cajaColores.arriba) + 4f;
                var piezas = new List<RectTransform> { ventana, v };
                if (tituloColores != null) piezas.Add(tituloColores);
                var titNuevo = BuscarHijo(contenedor, TituloIconos)?.TryCast<RectTransform>();
                if (titNuevo != null) piezas.Add(titNuevo);
                if (reset != null) piezas.Add(reset);
                // Revisión rápida, hallazgo 1: cualquier otro hijo del panel en la franja de la zona entra también (si no, la
                // zona taparía sus clics)
                // Quick review, finding 1: any other child of the panel in the area's strip is included too (otherwise the
                // area would block its clicks)
                for (int i = 0; i < contenedor.childCount; i++)
                {
                    var h = contenedor.GetChild(i)?.TryCast<RectTransform>();
                    if (h == null || !h.gameObject.activeSelf || piezas.Any(x => x != null && x.Pointer == h.Pointer) || h.name.StartsWith("zero")) continue;
                    var ch = Caja(h, contenedor);
                    if (ch.arriba > arribaZona || ch.der - ch.izq >= (cajaPanel.der - cajaPanel.izq) - 1f) continue;   // fuera, o fondo del panel
                                                                                                                       // outside, or panel background
                    piezas.Add(h);
                }
                if (!ZonaVertical(contenedor, piezas, cajaPanel, arribaZona, abajoTodo - hueco))
                    AvisoUnaVez("sinVertical", "no se pudo montar el desplazamiento vertical; «Restablecer» puede quedar fuera del panel.");
            }
            Plugin.Info($"Estandarte: sección «Iconos» en «{contenedor.name}» (panel {cajaPanel.abajo:0}..{cajaPanel.arriba:0}): colores " +
                        $"{cajaColores.abajo:0}..{cajaColores.arriba:0}, título {(tituloColores != null ? $"{cajaTit.abajo:0}..{cajaTit.arriba:0} (hueco título-fila {relTitulo:0})" : "no hallado")}, " +
                        $"iconos {cajaFinal.abajo:0}..{cajaFinal.arriba:0}, «Restablecer» {cajaResetFinal.abajo:0}..{cajaResetFinal.arriba:0} (bajado {bajar:0}); " +
                        $"desplazamiento vertical {(vertical ? "sí" : "no hace falta")}.");
            return rtF;
        }

        // Zona con desplazamiento VERTICAL por la rueda: una ventana del ancho del panel, de «arriba» al fondo del panel, cuyo
        // contenido (de «arriba» a «abajo») lleva las piezas dadas, que conservan su sitio en pantalla al cambiarlas de padre
        // Area with VERTICAL wheel scrolling: a window the width of the panel, from «arriba» to the panel bottom, whose
        // content (from «arriba» to «abajo») holds the given pieces, which keep their place on screen when reparented
        private const string ZonaVerticalNombre = "zeroZonaVertical";
        // Padre y orden originales de cada pieza metida en la zona (para devolverla igual al deshacerla; hallazgo 2)
        // Original parent and order of each piece put in the area (to restore it identically when undoing; finding 2)
        private static readonly Dictionary<IntPtr, (Transform padre, int orden)> _origenPiezas = new Dictionary<IntPtr, (Transform, int)>();

        // Cambia de padre conservando sitio en pantalla Y tamaño (con anclas estiradas, el tamaño cambiaría; hallazgo 3)
        // Reparents keeping screen position AND size (with stretched anchors, the size would change; finding 3)
        private static void CambiarPadreFijo(RectTransform rt, Transform padre)
        {
            var tam = rt.rect.size;
            var pos = rt.position;
            bool estirada = rt.anchorMin != rt.anchorMax;
            rt.SetParent(padre, true);
            if (estirada)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = tam;
                rt.position = pos;
            }
        }
        private static bool ZonaVertical(Transform contenedor, List<RectTransform> piezas, (float abajo, float arriba, float izq, float der) cajaPanel, float arriba, float abajo)
        {
            try
            {
                var go = new GameObject(ZonaVerticalNombre);
                var vista = go.AddComponent<RectTransform>();
                vista.SetParent(contenedor, false);
                vista.anchorMin = vista.anchorMax = vista.pivot = new Vector2(0.5f, 0.5f);
                float izq = float.MaxValue, der = float.MinValue;
                foreach (var pz in piezas) if (pz != null) { var c = Caja(pz, contenedor); izq = Math.Min(izq, c.izq); der = Math.Max(der, c.der); }
                izq -= 4f; der += 4f;
                vista.sizeDelta = new Vector2(der - izq, Math.Max(10f, arriba - (cajaPanel.abajo + 2f)));
                var cvis = Caja(vista, contenedor);
                MoverEn(vista, contenedor, new Vector3((izq + der) / 2f - (cvis.izq + cvis.der) / 2f, arriba - cvis.arriba, 0f));
                var fondo = go.AddComponent<Image>(); fondo.color = new Color(0f, 0f, 0f, 0f); fondo.raycastTarget = true;
                go.AddComponent<RectMask2D>();
                var sr = go.AddComponent<ScrollRect>();
                sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
                sr.inertia = true; sr.scrollSensitivity = 30f; sr.viewport = vista;
                // Contenido: arriba pegado a la ventana, alto = toda la zona
                // Content: top flush against the window, height = the whole area
                var goC = new GameObject(ZonaVerticalNombre + "Contenido");
                var cont = goC.AddComponent<RectTransform>();
                cont.SetParent(vista, false);
                cont.anchorMin = new Vector2(0f, 1f); cont.anchorMax = new Vector2(1f, 1f); cont.pivot = new Vector2(0.5f, 1f);
                cont.anchoredPosition = Vector2.zero;
                cont.sizeDelta = new Vector2(0f, arriba - abajo);
                sr.content = cont;   // hallazgo 5: antes de mover nada
                                     // finding 5: before moving anything
                foreach (var pz in piezas)
                {
                    if (pz == null) continue;
                    _origenPiezas[pz.Pointer] = (pz.parent, pz.GetSiblingIndex());
                    CambiarPadreFijo(pz, cont);
                }
                sr.verticalNormalizedPosition = 1f;
                Plugin.Info($"Estandarte: zona con desplazamiento vertical de {arriba - abajo:0} de alto en una ventana de {vista.rect.height:0}.");
                return true;
            }
            catch (Exception e) { AvisoUnaVez("zonaVertical", e.Message); return false; }
        }

        // Apaga en un botón de icono los gráficos del escudo de colores: deja el gráfico raíz, el hijo de mayor área (el rombo)
        // y el anillo de elegido. Apunta en el log, una vez, qué gráficos había y cuáles se apagan
        // Turns off the colored shield graphics on an icon button: keeps the root graphic, the child with the largest area (the diamond)
        // and the chosen ring. Logs once which graphics there were and which ones get turned off
        private static void QuitarEscudo(GameObject clon, GameObject anillo, ArmyBanners.Colours cPlantilla)
        {
            // Cuarteles del escudo: los gráficos con los colores de la facción de la plantilla (los mismos que se teñían)
            // Shield quarters: the graphics with the template's faction colors (the same ones that used to be tinted)
            var cuarteles = Teñibles(clon, cPlantilla).Select(x => x.Item1).Where(g => g != null).ToList();
            if (cuarteles.Count == 0) { AvisoUnaVez("sinEscudo", "no se reconoce el escudo en el botón de icono (sin colores de la plantilla); se queda como está."); return; }
            // Nodo del escudo: el antepasado común de los cuarteles por debajo de la raíz del botón
            // Shield node: the common ancestor of the quarters below the button root
            Transform comun = cuarteles[0].transform;
            while (comun != null && comun != clon.transform && !cuarteles.All(g => g.transform.IsChildOf(comun))) comun = comun.parent;
            string quitado;
            if (comun != null && comun != clon.transform && (anillo == null || !anillo.transform.IsChildOf(comun)))
            {
                comun.gameObject.SetActive(false);   // el escudo entero (cuarteles y su borde)
                                                     // the whole shield (quarters and their border)
                quitado = "el nodo «" + comun.name + "»";
            }
            else
            {
                foreach (var g in cuarteles) g.enabled = false;   // cuelgan de la raíz: solo los cuarteles
                                                                  // hang from the root: only the quarters
                quitado = cuarteles.Count + " cuarteles sueltos";
            }
            if (_avisos.Add("escudoLog"))
                Plugin.Info($"Estandarte: botón de icono sin escudo: apagado {quitado} ({string.Join(", ", cuarteles.Select(g => g.name))}).");
        }

        // Fila nueva bajo la ventana de colores: título clonado (si lo hay) + ventana con rueda y su propia fila
        // New row below the colors window: cloned title (if any) + window with wheel and its own row
        private static RectTransform FilaNueva(RectTransform fila, Transform contenedor, RectTransform ventana)
        {
            int indice = ventana.GetSiblingIndex();
            // Revisión externa 0.7.7, hallazgo 7: si el título ya existe (montaje anterior cortado), la fila va tras él
            // External review 0.7.7, finding 7: if the title already exists (previous build cut short), the row goes after it
            var tituloViejo = contenedor.Find("zeroTituloIconos");
            if (tituloViejo != null) indice = tituloViejo.GetSiblingIndex();
            // Título: el hermano anterior con texto y sin botón (el «Colores de facción»); se clona sin su localización
            // Title: the previous sibling with text and no button (the «Colores de facción»); it is cloned without its localization
            if (indice > 0)
            {
                var previo = contenedor.GetChild(indice - 1);
                var texto = previo?.GetComponent<TMPro.TMP_Text>();
                if (texto != null && previo.GetComponent<Button>() == null && contenedor.Find("zeroTituloIconos") == null)
                {
                    var titulo = UnityEngine.Object.Instantiate(previo.gameObject, contenedor, false);
                    titulo.name = "zeroTituloIconos";
                    var comps = titulo.GetComponents<Component>();
                    for (int i = 0; comps != null && i < comps.Length; i++)
                        if (comps[i] != null && comps[i].GetIl2CppType().Name.IndexOf("Locali", StringComparison.OrdinalIgnoreCase) >= 0)
                            UnityEngine.Object.DestroyImmediate(comps[i]);
                    titulo.transform.SetSiblingIndex(indice + 1);
                    var t = titulo.GetComponent<TMPro.TMP_Text>();
                    PonerTitulo(t, "zeroIconoEstandarteTitulo", "Icono del estandarte", "Banner icon");
                    indice++;
                }
            }
            // Fila: copia vacía de la de colores (mismo HorizontalLayoutGroup y tamaño)
            // Row: empty copy of the colors one (same HorizontalLayoutGroup and size)
            var nueva = new GameObject(FilaIconos);
            nueva.layer = fila.gameObject.layer;
            var rt = nueva.AddComponent<RectTransform>();
            rt.SetParent(contenedor, false);
            rt.SetSiblingIndex(indice + 1);
            rt.sizeDelta = new Vector2(ventana.rect.width, ventana.rect.height);   // ronda 2, hallazgo 5: lo visible, no el contenido
                                                                                   // round 2, finding 5: what is visible, not the content
            var hlg = fila.GetComponent<HorizontalLayoutGroup>();
            var h2 = nueva.AddComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                h2.spacing = hlg.spacing; h2.padding = hlg.padding; h2.childAlignment = hlg.childAlignment;
                h2.childControlWidth = hlg.childControlWidth; h2.childControlHeight = hlg.childControlHeight;
                h2.childForceExpandWidth = false; h2.childForceExpandHeight = hlg.childForceExpandHeight;
            }
            // Ronda 2 de la revisión, hallazgo 1: sin LayoutElement en la fila (con prioridad 1 fijaría su ancho y el
            // ContentSizeFitter no la dejaría crecer); el tamaño en el panel lo lleva la ventana que crea Envolver
            // Round 2 of the review, finding 1: no LayoutElement on the row (with priority 1 it would fix its width and the
            // ContentSizeFitter would not let it grow); the size in the panel is handled by the window that Envolver creates
            var leV = ventana.GetComponent<LayoutElement>();
            var v = Envolver(rt, VentanaIconos);
            if (v != null && v.GetComponent<LayoutElement>() is LayoutElement lv && leV != null)
            { lv.preferredWidth = leV.preferredWidth; lv.preferredHeight = leV.preferredHeight; lv.minHeight = leV.minHeight; }
            return rt;
        }

        // Colores del estandarte en los botones, icono del héroe al día y anillo en el elegido
        // Banner colors on the buttons, hero icon up to date and ring on the chosen one
        private static void RepintarSelector(WarbandBannerPanel p)
        {
            _botones.RemoveAll(b => b.Raiz == null);
            if (_botones.Count == 0) return;
            ArmyBanners.Colours c = p._colours;
            string elegido = LogicaEstandarte.ElegidoDisponible(Guardado, Disponibles()) ?? CodigoHeroe;
            foreach (var b in _botones)
            {
                Teñir(b.Teñibles, c);
                if (b.Codigo.Length == 0 && b.Icono != null && _iconoHeroe != null)
                {
                    b.Icono.sprite = SpriteDe("heroe", _iconoHeroe);
                    b.Icono.enabled = b.Icono.sprite != null;
                }
                if (b.Anillo != null) b.Anillo.SetActive(b.Codigo == elegido);
            }
        }
    }
}
