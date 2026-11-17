// AudioGuardia.cs
// Sonido láser de las unidades nuevas con armas de energía (las facciones A y B): disparo e impacto.
// AudioGuardia.cs
// Laser sound for the new units with energy weapons (factions A and B): shot and impact.
//
// Cómo suena un disparo en el juego (código publicado de Tabletop Tavern):
//   - Burst solo encola eventos SFXBufferElement { UnitName, tipo, distancia }: disparo en
//     RangedUnitAttackSystem (50 % de los disparos, 60 m) e impacto en BulletMoverSystem (35 % de los aciertos).
//   - EntityWatcher (gestionado) pide el clip a TabletopTavernData.GetBattlefieldAudio(unidad, tipo), que saca uno al
//     azar del FireProjectileSFX de la unidad (fireProjectileSFX = disparo, hitSFX = impacto; las armas de fuego
//     tienen hitSFX vacío, impacto mudo), y lo reproduce con SFXManager en el canal Effects.
// How a shot sounds in the game (published Tabletop Tavern code):
//   - Burst only enqueues SFXBufferElement events { UnitName, tipo, distancia }: shot in
//     RangedUnitAttackSystem (50% of shots, 60 m) and impact in BulletMoverSystem (35% of hits).
//   - EntityWatcher (managed) requests the clip from TabletopTavernData.GetBattlefieldAudio(unit, type), which picks one at
//     random from the unit's FireProjectileSFX (fireProjectileSFX = shot, hitSFX = impact; firearms
//     have an empty hitSFX, a silent impact), and plays it with SFXManager on the Effects channel.
//
// Qué unidades suenan a láser: las que el manifiesto marca en su campo «sonidos» (LogicaSonidos.cs): «laser» (facción A),
// «blaster» (facción B) o «pesado» (arma pesada fija, andador pesado...). «base» = el sonido de su unidad base, sin tocar.
// Which units sound like lasers: those the manifest marks in its «sonidos» field (LogicaSonidos.cs): «laser» (faction A),
// «blaster» (faction B) or «pesado» (fixed heavy weapon, heavy walker...). «base» = the sound of its base unit, untouched.
//
// Tres capas por disparo (de la mejor a la más segura; cada una con try/catch y caída a la siguiente):
//   1) Láser propio desde WAV: <prefijo>*.wav de la carpeta audio\ del módulo de la unidad (laser_disparo_*,
//      blaster_disparo_*, pesado_disparo_*; impacto común laser_impacto_*). El AudioClip se crea llamando directamente
//      a las funciones internas de Unity (AudioClip::Construct_Internal_Injected y CreateUserSound_Injected) y las
//      muestras se escriben con SetData_Injected, resueltas con il2cpp_resolve_icall. Así no se pasa por el
//      AudioClip.Create del interop de BepInEx, que en Unity 6 falla con «Method not found:
//      ReadOnlySpan`1.GetPinnableReference()» (Il2CppInterop, issue #202). Investigación: notas internas (no publicadas).
//   2) Prestado: un clip de serie del juego que ya esté cargado (hechizos de héroe): «Smite Charge» para fusiles y
//      carabinas, «LightningStrike» para las armas pesadas. Se busca por nombre con Resources.FindObjectsOfTypeAll
//      (solo encuentra clips en memoria; si no está, se reintenta como mucho cada 30 s, nunca en cada disparo).
//   3) Base: el disparo de la unidad base (lo que hace el juego sin el plugin).
// Three layers per shot (from best to safest; each with try/catch and falling back to the next):
//   1) Custom laser from WAV: <prefijo>*.wav in the audio\ folder of the unit's module (laser_disparo_*,
//      blaster_disparo_*, pesado_disparo_*; shared impact laser_impacto_*). The AudioClip is created by calling directly
//      the internal Unity functions (AudioClip::Construct_Internal_Injected and CreateUserSound_Injected) and the
//      samples are written with SetData_Injected, resolved with il2cpp_resolve_icall. This way it bypasses the
//      BepInEx interop's AudioClip.Create, which in Unity 6 fails with «Method not found:
//      ReadOnlySpan`1.GetPinnableReference()» (Il2CppInterop, issue #202). Research: internal notes (unpublished).
//   2) Borrowed: a vanilla game clip that is already loaded (hero spells): «Smite Charge» for rifles and
//      carbines, «LightningStrike» for heavy weapons. It is looked up by name with Resources.FindObjectsOfTypeAll
//      (it only finds clips in memory; if it is not there, it retries at most every 30 s, never on every shot).
//   3) Base: the base unit's shot (what the game does without the plugin).
//
// Cuándo se crea cada cosa:
//   - Los WAV se convierten en clips SOLO en el postfix de TabletopTavernData.ReloadData (hilo principal, menú;
//     UnidadGuardia.TrasReloadData). Si Unity destruyera los clips, se recargan en el siguiente ReloadData (con tope).
//   - El postfix de GetBattlefieldAudio NUNCA crea clips: solo elige entre los que ya hay.
//   - No se toca la entrada de assets de ninguna unidad (la de la 900 sigue siendo copia exacta de Eisenmann, lo que
//     vigila AssetsGuardia): escribir structs con referencias por interop fue lo que dejó la carta bocabajo.
// When each thing is created:
//   - WAVs are converted into clips ONLY in the TabletopTavernData.ReloadData postfix (main thread, menu;
//     UnidadGuardia.TrasReloadData). If Unity destroys the clips, they are reloaded on the next ReloadData (with a cap).
//   - The GetBattlefieldAudio postfix NEVER creates clips: it only picks among the existing ones.
//   - No unit's asset entry is touched (the one for 900 is still an exact copy of Eisenmann, which
//     AssetsGuardia watches): writing structs with references via interop is what left the card face down.
//
// Centinela anti-cierre: una función interna con un puntero mal pasado puede cerrar el juego, y eso no lo para un
// try/catch. Antes del primer intento nativo se escribe BepInEx\config\zero.tabletoptavern.nucleo.audio_en_curso.txt y
// se borra al terminar la carga. Si al arrancar el fichero sigue ahí, la carga anterior no terminó (el juego se cerró en
// medio): se salta la capa 1 (quedan la 2 y la 3) y se avisa en el log. Para reintentar, borrar ese fichero.
// Anti-crash sentinel: an internal function with a badly passed pointer can crash the game, and a
// try/catch cannot stop that. Before the first native attempt, BepInEx\config\zero.tabletoptavern.nucleo.audio_en_curso.txt is written and
// it is deleted when loading finishes. If the file is still there at startup, the previous load did not finish (the game crashed
// midway): layer 1 is skipped (layers 2 and 3 remain) and a warning is logged. To retry, delete that file.
//
// Opciones: Audio.SonidoPropio (true por defecto desde el 18-10-2026; migración única en Plugin.cs), Audio.ImpactoPropio,
// Audio.GananciaDb.
// Historia: hasta la 0.7.2 se creía que en Unity 6 no se podían crear sonidos desde el plugin (la comprobación buscaba el
// AudioClip.Create gestionado, que el compilador del juego recortó). Era un diagnóstico equivocado: las funciones
// internas sí están en UnityPlayer.dll; lo que fallaba era la copia del interop (issue #202).
// Options: Audio.SonidoPropio (true by default since 18-10-2026; one-time migration in Plugin.cs), Audio.ImpactoPropio,
// Audio.GananciaDb.
// History: up to 0.7.2 it was believed that in Unity 6 sounds could not be created from the plugin (the check looked for the managed
// AudioClip.Create, which the game's compiler stripped). That was a wrong diagnosis: the internal
// functions are in UnityPlayer.dll; what failed was the interop copy (issue #202).

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: carga de clips propios y sustitución del sonido de las armas de energía
    // Section: loading custom clips and replacing the sound of energy weapons
    // -----------------------------------------------------------------------
    public static class AudioGuardia
    {
        public const string Subcarpeta = "audio";
        public static string PrefijoDisparo => LogicaSonidos.PrefijoDisparo(TipoSonido.Laser);   // el de la 900 (autoprueba)
                                                                                                 // the one for 900 (self-test)
        public const string PrefijoImpacto = LogicaSonidos.PrefijoImpacto;
        private const int MaxClipsPorTipo = 8;                   // tope de variantes por tipo
                                                                 // cap on variants per type
        private const long MaxBytesFichero = 4 * 1024 * 1024;    // un disparo ocupa ~25 KB; más de 4 MB es un error
                                                                 // one shot takes ~25 KB; more than 4 MB is an error
        private const int MaxRecargas = 3;                       // reintentos si Unity destruye los clips (cambio de escena)
                                                                 // retries if Unity destroys the clips (scene change)
        private const float SegundosEntreBusquedas = 30f;        // búsqueda de clips prestados: como mucho cada 30 s
                                                                 // borrowed clip lookup: at most every 30 s
        private const string FicheroCentinela = "zero.tabletoptavern.nucleo.audio_en_curso.txt";

        // Clips propios cargados por tipo (se guardan las referencias para que no los recoja el recolector)
        // Custom clips loaded per type (references are kept so the garbage collector does not collect them)
        private static readonly Dictionary<TipoSonido, List<AudioClip>> _propios = new Dictionary<TipoSonido, List<AudioClip>>
        {
            { TipoSonido.Laser, new List<AudioClip>() },
            { TipoSonido.Blaster, new List<AudioClip>() },
            { TipoSonido.Pesado, new List<AudioClip>() },
        };
        private static readonly List<AudioClip> _impactos = new List<AudioClip>();
        // Clips del juego prestados (nombre -> clip). Son del juego: NUNCA se destruyen desde aquí
        // Borrowed game clips (name -> clip). They belong to the game: NEVER destroyed from here
        private static readonly Dictionary<string, AudioClip> _prestados = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private static float _proximaBusqueda;                   // Time.realtimeSinceStartup de la próxima búsqueda permitida
                                                                 // Time.realtimeSinceStartup of the next allowed lookup
        private static readonly System.Random _azar = new System.Random();   // azar propio: no toca el de Unity
                                                                             // own randomness: does not touch Unity's
        private static bool _intentado;          // ya se intentó cargar (no se reintenta en cada ReloadData)
                                                 // loading was already attempted (not retried on every ReloadData)
        private static bool _destruidos;         // el postfix vio un clip destruido: recargar en el próximo ReloadData
                                                 // the postfix saw a destroyed clip: reload on the next ReloadData
        private static int _recargas;
        private static bool _errorAvisado;
        // Última capa escrita en el log por tipo (solo se escribe cuando cambia)
        // Last layer written to the log per type (only written when it changes)
        private static readonly Dictionary<TipoSonido, string> _capaAvisada = new Dictionary<TipoSonido, string>();

        // Tipo de sonido por unidad (id -> tipo), rehecho si cambia la lista de unidades del manifiesto
        // Sound type per unit (id -> type), rebuilt if the manifest's unit list changes
        private static object _fuenteTipos;
        private static readonly Dictionary<int, TipoSonido> _tipos = new Dictionary<int, TipoSonido>();

        /// <summary>Carpetas de donde se leyeron WAV (para el log y la autoprueba; «(ninguna)» si no hubo).</summary>
        /// Folders WAVs were read from (for the log and the self-test; «(ninguna)» if there were none).
        public static string CarpetaUsada { get; private set; }
        /// <summary>Motivo por el que la capa 1 (clips desde WAV) no funcionó; null si funcionó o no se intentó.</summary>
        /// Reason why layer 1 (clips from WAV) did not work; null if it worked or was not attempted.
        public static string MotivoSinPropios { get; private set; }
        public static int NumDisparos => _propios[TipoSonido.Laser].Count;
        public static int NumImpactos => _impactos.Count;
        public static IReadOnlyList<AudioClip> Disparos => _propios[TipoSonido.Laser];
        internal static int NumPropios(TipoSonido t) => _propios.TryGetValue(t, out var l) ? l.Count : 0;
        internal static IReadOnlyList<AudioClip> Propios(TipoSonido t) => _propios.TryGetValue(t, out var l) ? l : (IReadOnlyList<AudioClip>)Array.Empty<AudioClip>();

        private static bool Activado => Plugin.CfgSonidoPropio != null && Plugin.CfgSonidoPropio.Value;

        // ---------------------------------------------------------------
        // Tipo de sonido de una unidad (campo «sonidos» del manifiesto)
        // Sound type of a unit («sonidos» field of the manifest)
        // ---------------------------------------------------------------
        /// <summary>Tipo de sonido de una unidad nueva (Base si no es nueva o su manifiesto dice «base»).</summary>
        /// Sound type of a new unit (Base if it is not new or its manifest says «base»).
        internal static TipoSonido TipoDe(UnitName u)
        {
            var todas = UnidadGuardia.Todas;
            if (!ReferenceEquals(todas, _fuenteTipos))
            {
                // El manifiesto se cargó otra vez (o es la primera consulta): se rehace la tabla
                // The manifest was loaded again (or this is the first query): the table is rebuilt
                _tipos.Clear();
                foreach (var n in todas) _tipos[n.Numero] = LogicaSonidos.DeManifiesto(n.Datos?.Sonidos);
                _fuenteTipos = todas;
            }
            return _tipos.TryGetValue((int)u, out var t) ? t : TipoSonido.Base;
        }

        // ---------------------------------------------------------------
        // Postfix de TabletopTavernData.GetBattlefieldAudio(UnitName, SFXEntityType)
        // (__0 = unidad, __1 = tipo de sonido; se usan posiciones por si el interop cambia los nombres)
        // Juego 1.11.1: el enum Memori.Audio.SFXEntityType sigue igual (Idle, Death, FireProjectile, MeleeAttack, Explosion,
        // ProjectileHit) pero ahora vive en TabletopTavern.Audio.dll (referencia nueva en el csproj). GetBattlefieldAudio
        // mantiene la firma y lo siguen llamando EntityWatcher.Update y EntityWatcher.ReplayUnitSound (comprobado en el binario)
        // Postfix of TabletopTavernData.GetBattlefieldAudio(UnitName, SFXEntityType)
        // (__0 = unit, __1 = sound type; positions are used in case interop changes the names)
        // Game 1.11.1: the enum Memori.Audio.SFXEntityType is unchanged (Idle, Death, FireProjectile, MeleeAttack, Explosion,
        // ProjectileHit) but now lives in TabletopTavern.Audio.dll (new reference in the csproj). GetBattlefieldAudio
        // keeps its signature and is still called by EntityWatcher.Update and EntityWatcher.ReplayUnitSound (checked in the binary)
        // ---------------------------------------------------------------
        public static void TrasGetBattlefieldAudio(UnitName __0, Memori.Audio.SFXEntityType __1, ref AudioClip __result)
        {
            if (!UnidadGuardia.EsNueva(__0)) return;              // lo más frecuente: unidad de serie, salida inmediata
                                                                  // the most frequent case: stock unit, immediate return
            try
            {
                if (!Plugin.Operativo || !Activado) return;
                TipoSonido tipo = TipoDe(__0);
                if (tipo == TipoSonido.Base) return;              // su manifiesto dice «base»: el de su unidad base
                                                                  // its manifest says «base»: that of its base unit

                if (__1 == Memori.Audio.SFXEntityType.ProjectileHit)
                {
                    // Impacto: solo con WAV propios y la opción activada; si no, como el juego (mudo en armas de fuego)
                    // Impact: only with custom WAVs and the option enabled; otherwise, like the game (silent on firearms)
                    if (Plugin.CfgImpactoPropio == null || !Plugin.CfgImpactoPropio.Value) return;
                    AudioClip imp = ElegirVivo(_impactos);
                    if (imp != null) __result = imp;
                    return;
                }
                if (__1 != Memori.Audio.SFXEntityType.FireProjectile) return;   // voces, muerte, cuerpo a cuerpo: los de su base
                                                                                // voices, death, melee: those of its base

                // Capa 1: láser propio desde WAV
                // Layer 1: own laser from WAV
                AudioClip clip = ElegirVivo(_propios[tipo]);
                // Capa 2: clip prestado del juego (solo si no hay propio)
                // Layer 2: borrowed game clip (only if there is no custom one)
                string prestado = null;
                if (clip == null) clip = BuscarPrestado(tipo, out prestado);
                CapaSonido capa = LogicaSonidos.ElegirCapa(tipo, clip != null && prestado == null ? 1 : 0, prestado != null);
                AvisarCapa(tipo, capa, prestado);
                if (clip != null) __result = clip;                // capa 3 (base): se deja el resultado original
                                                                  // layer 3 (base): the original result is left as is
            }
            catch (Exception e)
            {
                if (!_errorAvisado) { _errorAvisado = true; Plugin.Error("Audio: fallo al elegir el sonido láser (se sigue con el de la unidad base): " + e); }
            }
        }

        // Un clip al azar de la lista; null si está vacía o si Unity lo destruyó (entonces se recarga en el próximo ReloadData)
        // A random clip from the list; null if it is empty or if Unity destroyed it (then it is reloaded on the next ReloadData)
        private static AudioClip ElegirVivo(List<AudioClip> lista)
        {
            if (lista.Count == 0) return null;
            AudioClip clip = lista[_azar.Next(lista.Count)];
            if (clip == null)
            {
                if (!_destruidos) { _destruidos = true; Plugin.Aviso("Audio: un clip propio ya no existe (¿cambio de escena?); se recargarán en la próxima recarga de datos."); }
                return null;
            }
            return clip;
        }

        // Escribe en el log la capa de un tipo cuando cambia («Audio [blaster]: prestado: Smite Charge»)
        // Writes a type's layer to the log when it changes («Audio [blaster]: prestado: Smite Charge»)
        private static void AvisarCapa(TipoSonido tipo, CapaSonido capa, string prestado)
        {
            string texto = LogicaSonidos.DescribirCapa(capa, prestado);
            if (capa == CapaSonido.Base)
                texto += $" ({string.Join(" / ", LogicaSonidos.Prestados(tipo))} no cargado en esta batalla y sin WAV propios)";
            if (_capaAvisada.TryGetValue(tipo, out var antes) && antes == texto) return;
            _capaAvisada[tipo] = texto;
            Plugin.Info($"Audio [{LogicaSonidos.Nombre(tipo)}]: {texto}");
        }

        // ---------------------------------------------------------------
        // Capa 2: clips del juego prestados (hechizos de héroe)
        // Layer 2: borrowed game clips (hero spells)
        // ---------------------------------------------------------------
        // Clip prestado para un tipo (null si ninguno está cargado). La búsqueda en Unity va con límite de frecuencia
        // Borrowed clip for a type (null if none is loaded). The Unity lookup is rate-limited
        private static AudioClip BuscarPrestado(TipoSonido tipo, out string nombre)
        {
            nombre = null;
            AudioClip c = PrestadoEnCache(tipo, out nombre);
            if (c != null) return c;
            if (Time.realtimeSinceStartup < _proximaBusqueda) return null;
            _proximaBusqueda = Time.realtimeSinceStartup + SegundosEntreBusquedas;
            RefrescarPrestados();
            return PrestadoEnCache(tipo, out nombre);
        }

        // El primer prestado de la lista de preferencia del tipo que sigue vivo en la caché
        // The first borrowed clip in the type's preference list that is still alive in the cache
        private static AudioClip PrestadoEnCache(TipoSonido tipo, out string nombre)
        {
            nombre = null;
            foreach (string n in LogicaSonidos.Prestados(tipo))
            {
                if (!_prestados.TryGetValue(n, out var c)) continue;
                if (c == null) { _prestados.Remove(n); continue; }   // Unity lo descargó: se buscará otra vez
                                                                     // Unity unloaded it: it will be looked up again
                nombre = n;
                return c;
            }
            return null;
        }

        /// <summary>Busca en memoria los clips prestables por nombre (Resources.FindObjectsOfTypeAll). Devuelve cuántos hay.</summary>
        /// Searches memory for borrowable clips by name (Resources.FindObjectsOfTypeAll). Returns how many there are.
        internal static int RefrescarPrestados()
        {
            try
            {
                var buscados = new HashSet<string>(LogicaSonidos.TodosLosPrestados(), StringComparer.Ordinal);
                var todos = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
                for (int i = 0; todos != null && i < todos.Length; i++)
                {
                    var c = todos[i]?.TryCast<AudioClip>();
                    if (c == null) continue;
                    string n = c.name;
                    if (n == null || !buscados.Contains(n) || _prestados.ContainsKey(n)) continue;
                    // Si el clip no tiene los datos cargados, se piden ya (en la primera reproducción ya estarán)
                    // If the clip does not have its data loaded, it is requested now (by the first playback it will be ready)
                    try { if (c.loadState != AudioDataLoadState.Loaded) c.LoadAudioData(); } catch { }
                    _prestados[n] = c;
                }
            }
            catch (Exception e) { Plugin.Aviso("Audio: no se pudieron buscar los clips prestados del juego: " + e.Message); }
            return _prestados.Count;
        }

        /// <summary>Nombres de los clips prestados que hay ahora en la caché (para la autoprueba y el log).</summary>
        /// Names of the borrowed clips currently in the cache (for the self-test and the log).
        internal static string ResumenPrestados()
        {
            var vivos = new List<string>();
            foreach (var kv in _prestados) if (kv.Value != null) vivos.Add(kv.Key);
            return vivos.Count == 0 ? "ninguno" : string.Join(", ", vivos);
        }

        // ---------------------------------------------------------------
        // Carga: solo desde el postfix de ReloadData (hilo principal). Una vez, más las recargas por clips destruidos
        // Loading: only from the ReloadData postfix (main thread). Once, plus reloads for destroyed clips
        // ---------------------------------------------------------------
        /// <summary>Carga los WAV si aún no se intentó (o si se destruyeron, con tope). Idempotente y sin excepciones.</summary>
        /// Loads the WAVs if not yet attempted (or if they were destroyed, with a cap). Idempotent and exception-free.
        public static void AsegurarCargado(string origen)
        {
            if (!Activado) return;
            if (_intentado)
            {
                if (!_destruidos && !HayDestruidos()) return;
                _destruidos = false;
                if (_recargas >= MaxRecargas) return;
                _recargas++;
                Plugin.Aviso($"[{origen}] Audio: clips propios destruidos por Unity; recarga {_recargas}/{MaxRecargas}.");
            }
            _intentado = true;
            MotivoSinPropios = null;
            string centinela = null;
            try
            {
                Liberar();
                _capaAvisada.Clear();
                var carpetas = BuscarCarpetas();
                CarpetaUsada = carpetas.Count == 0 ? null : string.Join("; ", carpetas);
                if (carpetas.Count == 0)
                {
                    MotivoSinPropios = "no hay carpeta audio\\ en los módulos con armas de energía";
                    Plugin.Aviso($"[{origen}] Audio: no hay carpeta {Subcarpeta}\\ en ningún módulo con unidades láser; se usarán clips prestados del juego o el sonido base.");
                    return;
                }
                // Centinela anti-cierre (ver cabecera): si quedó de la sesión anterior, no se llama a ninguna función interna
                // Anti-crash sentinel (see header): if left over from the previous session, no internal function is called
                centinela = Path.Combine(Paths.ConfigPath, FicheroCentinela);
                if (File.Exists(centinela))
                {
                    MotivoSinPropios = "centinela: la carga anterior no terminó (el juego se cerró al crear los clips)";
                    Plugin.Aviso($"[{origen}] Audio: la carga de sonidos de la sesión anterior no terminó (¿se cerró el juego al crear los clips?). " +
                                 $"Se salta el láser propio desde WAV; quedan los clips prestados del juego y el sonido base. Para reintentarlo, borra {centinela}.");
                    centinela = null;                             // no se borra: sigue bloqueando hasta que lo quite el usuario
                                                                  // it is not deleted: it keeps blocking until the user removes it
                    return;
                }
                File.WriteAllText(centinela, "Carga de sonidos propios en curso (Zero - Núcleo). Si el juego se cerró y este fichero sigue aquí, " +
                                             "el plugin no volverá a crear clips desde WAV hasta que lo borres.");
                if (!ResolverCreacion(out string motivo))
                {
                    MotivoSinPropios = motivo;
                    Plugin.Aviso($"[{origen}] Audio: {motivo}; se usarán clips prestados del juego o el sonido base.");
                    return;
                }
                float ganancia = Plugin.CfgGananciaDb == null ? 0f : Math.Clamp(Plugin.CfgGananciaDb.Value, -24f, 12f);
                var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // un mismo WAV en dos módulos: una vez
                                                                                      // the same WAV in two modules: once
                foreach (string carpeta in carpetas)
                {
                    foreach (var t in LogicaSonidos.TiposConWav) CargarTipo(carpeta, LogicaSonidos.PrefijoDisparo(t), _propios[t], ganancia, vistos);
                    CargarTipo(carpeta, PrefijoImpacto, _impactos, ganancia, vistos);
                }
                foreach (var t in LogicaSonidos.TiposConWav)
                    Plugin.Info($"[{origen}] Audio [{LogicaSonidos.Nombre(t)}]: {_propios[t].Count} disparos propios desde WAV" +
                                (_propios[t].Count == 0 ? $" (ninguno: se usará «{LogicaSonidos.Prestados(t)[0]}» prestado si está cargado, o el sonido base)" : ""));
                Plugin.Info($"[{origen}] Audio: {_impactos.Count} impactos propios (se oyen con ImpactoPropio = {Plugin.CfgImpactoPropio?.Value}); carpetas: {CarpetaUsada}; ganancia {ganancia:0.#} dB.");
                if (TotalPropios() == 0 && MotivoSinPropios == null) MotivoSinPropios = "ningún WAV de disparo válido";
            }
            catch (Exception e)
            {
                Liberar();
                MotivoSinPropios = "error al cargar: " + e.Message;
                Plugin.Error($"[{origen}] Audio: no se pudieron cargar los sonidos propios (se usarán clips prestados o el sonido base): " + e);
            }
            finally
            {
                // La carga terminó sin cerrar el juego: se quita el centinela
                // Loading finished without crashing the game: the sentinel is removed
                if (centinela != null) { try { File.Delete(centinela); } catch (Exception e) { Plugin.Aviso("Audio: no se pudo quitar el centinela: " + e.Message); } }
                // Información para el log: qué clips prestados hay ya en memoria (en el menú lo normal es que ninguno)
                // Info for the log: which borrowed clips are already in memory (in the menu normally none)
                try { RefrescarPrestados(); Plugin.Info($"[{origen}] Audio: clips prestados del juego en memoria ahora: {ResumenPrestados()}."); } catch { }
            }
        }

        private static int TotalPropios()
        {
            int n = 0;
            foreach (var l in _propios.Values) n += l.Count;
            return n;
        }

        // Carpetas audio\ de los módulos que traen unidades con sonido no «base» (más el respaldo antiguo de BepInEx\plugins)
        // audio\ folders of the modules that bring units with non-«base» sound (plus the old BepInEx\plugins fallback)
        private static List<string> BuscarCarpetas()
        {
            var salida = new List<string>();
            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var u in UnidadGuardia.Todas)
                {
                    if (LogicaSonidos.DeManifiesto(u.Datos?.Sonidos) == TipoSonido.Base) continue;
                    string mod = u.Datos?.Carpeta;
                    if (string.IsNullOrEmpty(mod)) continue;
                    string c = Path.Combine(mod, Subcarpeta);
                    if (vistas.Add(c) && Directory.Exists(c)) salida.Add(c);
                }
            }
            catch (Exception e) { Plugin.Aviso("Audio: no se pudieron consultar las carpetas de los módulos: " + e.Message); }
            string respaldo = Path.Combine(Paths.PluginPath, "ZeroAstraMilitarum", Subcarpeta);
            if (vistas.Add(respaldo) && Directory.Exists(respaldo)) salida.Add(respaldo);
            return salida;
        }

        // Carga todos los <prefijo>*.wav de la carpeta (orden alfabético, con tope) en la lista
        // Loads all the <prefijo>*.wav files from the folder (alphabetical order, with a cap) into the list
        private static void CargarTipo(string carpeta, string prefijo, List<AudioClip> destino, float gananciaDb, HashSet<string> vistos)
        {
            string[] ficheros = Directory.GetFiles(carpeta, prefijo + "*.wav");
            Array.Sort(ficheros, StringComparer.OrdinalIgnoreCase);
            foreach (string f in ficheros)
            {
                if (destino.Count >= MaxClipsPorTipo) break;
                if (!vistos.Add(Path.GetFileName(f))) continue;
                try
                {
                    AudioClip clip = CrearClip(f, gananciaDb);
                    if (clip != null) destino.Add(clip);
                }
                catch (Exception e)
                {
                    if (MotivoSinPropios == null) MotivoSinPropios = $"{Path.GetFileName(f)}: {e.Message}";
                    Plugin.Aviso($"Audio: {Path.GetFileName(f)} descartado: {e.Message}");
                }
            }
        }

        // Lee un WAV y crea el AudioClip de Unity (mono; si el WAV es estéreo se mezcla a mono)
        // Reads a WAV and creates the Unity AudioClip (mono; if the WAV is stereo it is mixed down to mono)
        private static AudioClip CrearClip(string ruta, float gananciaDb)
        {
            var info = new FileInfo(ruta);
            if (info.Length > MaxBytesFichero) throw new InvalidDataException($"demasiado grande ({info.Length} bytes)");
            float[] muestras = LectorWav.LeerPcm16(File.ReadAllBytes(ruta), out int frecuencia);
            float factor = (float)Math.Pow(10.0, gananciaDb / 20.0);
            if (factor != 1f)
                for (int i = 0; i < muestras.Length; i++) muestras[i] = Math.Clamp(muestras[i] * factor, -1f, 1f);

            string nombre = Path.GetFileNameWithoutExtension(ruta);
            if (string.IsNullOrEmpty(nombre)) nombre = "zero_laser";
            // Vía principal: funciones internas de Unity llamadas a mano; respaldo: el Create compilado (si existiera)
            // Main path: Unity internal functions called by hand; fallback: the compiled Create (if it exists)
            AudioClip clip = CrearClipIcall(nombre, muestras.Length, frecuencia) ?? CrearClipNativo(nombre, muestras.Length, frecuencia);
            if (clip == null) throw new InvalidOperationException("no se pudo crear el AudioClip (ver avisos anteriores)");
            try
            {
                if (!EscribirMuestras(clip, muestras)) throw new InvalidOperationException("SetData_Injected devolvió false");
                // Comprobación de vuelta: el clip tiene las muestras pedidas y no están a cero
                // Round-trip check: the clip has the requested samples and they are not all zero
                if (clip.samples != muestras.Length) throw new InvalidOperationException($"el clip tiene {clip.samples} muestras y se pidieron {muestras.Length}");
                var prueba = new float[Math.Min(256, muestras.Length)];
                int desde = Math.Max(0, Math.Min(muestras.Length / 8, muestras.Length - prueba.Length));
                if (!LeerMuestras(clip, prueba, desde)) throw new InvalidOperationException("GetData_Injected devolvió false");
                bool hayDatos = false;
                foreach (float v in prueba) if (v != 0f) { hayDatos = true; break; }
                bool esperaDatos = false;
                for (int i = 0; i < prueba.Length; i++) if (muestras[desde + i] != 0f) { esperaDatos = true; break; }
                if (esperaDatos && !hayDatos) throw new InvalidOperationException("las muestras leídas de vuelta están a cero");
                clip.hideFlags = HideFlags.HideAndDontSave;      // que no lo descargue Resources.UnloadUnusedAssets
                                                                 // so that Resources.UnloadUnusedAssets does not unload it
                return clip;
            }
            catch
            {
                // Si falla tras crearlo, se destruye para no dejar un clip vacío huérfano en Unity
                // If it fails after creating it, it is destroyed so as not to leave an orphaned empty clip in Unity
                try { UnityEngine.Object.Destroy(clip); } catch { }
                throw;
            }
        }

        // ¿Algún clip propio ha sido destruido por Unity?
        // Has Unity destroyed any of our own clips?
        private static bool HayDestruidos()
        {
            foreach (var l in _propios.Values) foreach (var c in l) if (c == null) return true;
            foreach (var c in _impactos) if (c == null) return true;
            return false;
        }

        // Destruye los clips CREADOS por el plugin (antes de recargar). Los prestados son del juego y no se tocan
        // Destroys the clips CREATED by the plugin (before reloading). Borrowed ones belong to the game and are not touched
        private static void Liberar()
        {
            var listas = new List<List<AudioClip>>(_propios.Values) { _impactos };
            foreach (var lista in listas)
            {
                foreach (var c in lista) { try { if (c != null) UnityEngine.Object.Destroy(c); } catch { } }
                lista.Clear();
            }
        }

        // ---------------------------------------------------------------
        // Funciones internas de Unity (AudioClip::*_Injected), resueltas con il2cpp_resolve_icall
        // Unity internal functions (AudioClip::*_Injected), resolved with il2cpp_resolve_icall
        // ---------------------------------------------------------------
        // ManagedSpanWrapper de Unity 6 (UnityEngine.Bindings): puntero al primer elemento y número de elementos
        // Unity 6 ManagedSpanWrapper (UnityEngine.Bindings): pointer to the first element and number of elements
        [StructLayout(LayoutKind.Sequential)]
        private struct EnvolturaSpan { public IntPtr Inicio; public int Longitud; }

        // bool de Unity = 1 byte: por eso byte (un bool de .NET se pasaría como BOOL de 4 bytes). En x64 solo hay una
        // convención de llamada, así que Cdecl no cambia nada; las firmas son las que declara el propio interop para sus
        // delegados *_InjectedDelegate (IL leído el 18-10-2026, notas internas (no publicadas) §2.2)
        // Unity bool = 1 byte: hence byte (a .NET bool would be passed as a 4-byte BOOL). On x64 there is only one
        // calling convention, so Cdecl changes nothing; the signatures are those declared by the interop itself for its
        // *_InjectedDelegate delegates (IL read on 18-10-2026, internal notes (unpublished) §2.2)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte FuncionMuestras(IntPtr clipNativo, ref EnvolturaSpan datos, int desplazamiento);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr FuncionConstruir();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FuncionCrearSonido(IntPtr clipNativo, ref EnvolturaSpan nombre, int muestras, int canales, int hz, byte flujo);

        private static FuncionMuestras _setData, _getData;
        private static FuncionConstruir _construir;
        private static FuncionCrearSonido _crearSonido;
        private static bool _icallsResueltas;
        private static bool _construccionRota;   // Construct_Internal no dio un AudioClip usable: no se reintenta en esta sesión
                                                 // Construct_Internal did not return a usable AudioClip: it is not retried in this session
        private static string _faltan;           // funciones internas que no se encontraron (para el log)
                                                 // internal functions that were not found (for the log)

        // Resuelve una función interna: primero el nombre sin firma (el que usa el interop), después con firma
        // Resolves an internal function: first the name without signature (the one the interop uses), then with signature
        private static IntPtr Resolver(string nombre, string firma)
        {
            IntPtr p = IL2CPP.il2cpp_resolve_icall(nombre);
            if (p == IntPtr.Zero && firma != null) p = IL2CPP.il2cpp_resolve_icall(nombre + firma);
            return p;
        }

        // Resuelve una vez las cinco funciones internas (las que no existan quedan en null)
        // Resolves the five internal functions once (those that do not exist stay null)
        private static void ResolverIcalls()
        {
            if (_icallsResueltas) return;
            _icallsResueltas = true;
            const string firmaDatos = "(System.IntPtr,UnityEngine.Bindings.ManagedSpanWrapper&,System.Int32)";
            IntPtr s = Resolver("UnityEngine.AudioClip::SetData_Injected", firmaDatos);
            IntPtr g = Resolver("UnityEngine.AudioClip::GetData_Injected", firmaDatos);
            IntPtr c = Resolver("UnityEngine.AudioClip::Construct_Internal_Injected", "()");
            IntPtr u = Resolver("UnityEngine.AudioClip::CreateUserSound_Injected",
                                "(System.IntPtr,UnityEngine.Bindings.ManagedSpanWrapper&,System.Int32,System.Int32,System.Int32,System.Boolean)");
            if (s != IntPtr.Zero) _setData = Marshal.GetDelegateForFunctionPointer<FuncionMuestras>(s);
            if (g != IntPtr.Zero) _getData = Marshal.GetDelegateForFunctionPointer<FuncionMuestras>(g);
            if (c != IntPtr.Zero) _construir = Marshal.GetDelegateForFunctionPointer<FuncionConstruir>(c);
            if (u != IntPtr.Zero) _crearSonido = Marshal.GetDelegateForFunctionPointer<FuncionCrearSonido>(u);
            var faltan = new List<string>();
            if (_construir == null) faltan.Add("Construct_Internal_Injected");
            if (_crearSonido == null) faltan.Add("CreateUserSound_Injected");
            if (_setData == null) faltan.Add("SetData_Injected");
            if (_getData == null) faltan.Add("GetData_Injected");
            _faltan = faltan.Count == 0 ? null : string.Join(", ", faltan);
            Plugin.Info("Audio: funciones internas de AudioClip " + (_faltan == null ? "resueltas (las 4)." : "sin resolver: " + _faltan + "."));
        }

        // ¿Se pueden crear clips desde WAV? (las 4 funciones internas resueltas). Sin llamar a ninguna
        // Can clips be created from WAV? (the 4 internal functions resolved). Without calling any
        private static bool ResolverCreacion(out string motivo)
        {
            motivo = null;
            ResolverIcalls();
            if (_faltan == null) return true;
            motivo = "funciones internas de AudioClip sin resolver (" + _faltan + ")";
            return false;
        }

        /// <summary>
        /// Capa 1: crea un AudioClip vacío con las funciones internas de Unity, como hace AudioClip.Create por dentro
        /// (Construct_Internal + CreateUserSound), sin el Span del interop. Null si algo no cuadra (sin llamar a nada más).
        /// Layer 1: creates an empty AudioClip with Unity's internal functions, as AudioClip.Create does internally
        /// (Construct_Internal + CreateUserSound), without the interop Span. Null if something does not add up (without calling anything else).
        /// </summary>
        private static unsafe AudioClip CrearClipIcall(string nombre, int muestras, int frecuencia)
        {
            // Si la construcción ya falló una vez, no se repite con cada WAV (menos llamadas nativas a ciegas)
            // If construction has already failed once, it is not repeated for each WAV (fewer blind native calls)
            if (_construir == null || _crearSonido == null || _construccionRota) return null;
            // Construct_Internal_Injected devuelve un GCHandle del objeto gestionado (BindingsHelpers.cs de Unity 6000.3);
            // el handle no se libera (Unity tampoco lo hace al desempaquetarlo, y Il2CppInterop hace lo mismo en su
            // AssetBundle.LoadFromFile de Unity 6: il2cpp_gchandle_get_target sin liberar). Como mucho queda un handle por
            // clip creado (8 por tipo como máximo), y Destroy sigue destruyendo el objeto nativo
            // Construct_Internal_Injected returns a GCHandle of the managed object (Unity 6000.3 BindingsHelpers.cs);
            // the handle is not freed (Unity does not do so when unpacking it either, and Il2CppInterop does the same in its
            // AssetBundle.LoadFromFile on Unity 6: il2cpp_gchandle_get_target without freeing). At most one handle remains per
            // clip created (8 per type at most), and Destroy still destroys the native object
            IntPtr h = _construir();
            if (h == IntPtr.Zero) { MotivoSinPropios ??= "Construct_Internal_Injected devolvió 0"; _construccionRota = true; return null; }
            IntPtr obj = IL2CPP.il2cpp_gchandle_get_target(h);
            if (obj == IntPtr.Zero) { MotivoSinPropios ??= "el GCHandle de Construct_Internal no da ningún objeto"; _construccionRota = true; return null; }
            // Comprobación de tipo antes de usarlo: debe ser un AudioClip
            // Type check before using it: it must be an AudioClip
            IntPtr claseClip = Il2CppClassPointerStore<AudioClip>.NativeClassPtr;
            if (claseClip == IntPtr.Zero || IL2CPP.il2cpp_object_get_class(obj) != claseClip)
            { MotivoSinPropios ??= "Construct_Internal no devolvió un AudioClip"; _construccionRota = true; return null; }
            var clip = new AudioClip(obj);
            IntPtr nativo = clip.m_CachedPtr;
            if (nativo == IntPtr.Zero)
            {
                // No dejar el objeto gestionado a medias (si Unity no lo deja destruir, no pasa nada más)
                // Do not leave the managed object half-done (if Unity does not let it be destroyed, nothing else happens)
                try { UnityEngine.Object.Destroy(clip); } catch { }
                MotivoSinPropios ??= "el AudioClip construido no tiene objeto nativo";
                _construccionRota = true;
                return null;
            }
            // El nombre va como ManagedSpanWrapper sobre los char UTF-16 de la cadena, fijada durante toda la llamada
            // The name is passed as a ManagedSpanWrapper over the UTF-16 chars of the string, pinned for the whole call
            fixed (char* p = nombre)
            {
                var span = new EnvolturaSpan { Inicio = (IntPtr)p, Longitud = nombre.Length };
                _crearSonido(nativo, ref span, muestras, 1, frecuencia, 0);
            }
            return clip;
        }

        private static bool _avisoCreate;

        /// <summary>
        /// Respaldo de la capa 1: AudioClip.Create(string, int, int, int, bool) compilado en el juego, llamado con
        /// il2cpp_runtime_invoke. En esta versión el compilador lo recortó y devuelve null sin llamar a nada.
        /// (El AudioClip.Create del interop NO se usa: en Unity 6 lanza MissingMethodException, issue #202.)
        /// Fallback for layer 1: AudioClip.Create(string, int, int, int, bool) compiled in the game, called with
        /// il2cpp_runtime_invoke. In this version the compiler stripped it and it returns null without calling anything.
        /// (The interop's AudioClip.Create is NOT used: in Unity 6 it throws MissingMethodException, issue #202.)
        /// </summary>
        private static unsafe AudioClip CrearClipNativo(string nombre, int muestras, int frecuencia)
        {
            IntPtr clase = Il2CppClassPointerStore<AudioClip>.NativeClassPtr;
            IntPtr m = clase == IntPtr.Zero ? IntPtr.Zero : IL2CPP.il2cpp_class_get_method_from_name(clase, "Create", 5);
            if (m == IntPtr.Zero) return null;
            IntPtr textoNombre = IL2CPP.ManagedStringToIl2Cpp(nombre);
            int longitud = muestras, canales = 1, hz = frecuencia;
            byte flujo = 0;   // bool de IL2CPP: 1 byte
                              // IL2CPP bool: 1 byte
            void** args = stackalloc void*[5];
            args[0] = (void*)textoNombre;
            args[1] = &longitud;
            args[2] = &canales;
            args[3] = &hz;
            args[4] = &flujo;
            IntPtr excepcion = IntPtr.Zero;
            IntPtr res = IL2CPP.il2cpp_runtime_invoke(m, IntPtr.Zero, args, ref excepcion);
            if (excepcion != IntPtr.Zero || res == IntPtr.Zero)
            {
                if (!_avisoCreate) { _avisoCreate = true; Plugin.Aviso("Audio: AudioClip.Create compilado en el juego falló."); }
                return null;
            }
            return new AudioClip(res);
        }

        /// <summary>Escribe las muestras (mono) en el clip. true si Unity las acepta.</summary>
        /// Writes the (mono) samples into the clip. true if Unity accepts them.
        internal static unsafe bool EscribirMuestras(AudioClip clip, float[] muestras)
        {
            ResolverIcalls();
            if (_setData == null) { Il2CppStructArray<float> datos = muestras; return clip.SetData(datos, 0); }
            IntPtr nativo = clip.m_CachedPtr;
            if (nativo == IntPtr.Zero || muestras.Length == 0) return false;
            fixed (float* p = muestras)
            {
                var span = new EnvolturaSpan { Inicio = (IntPtr)p, Longitud = muestras.Length };
                return _setData(nativo, ref span, 0) != 0;
            }
        }

        /// <summary>Lee muestras del clip en <paramref name="destino"/> desde <paramref name="desplazamiento"/>. true si Unity las da.</summary>
        /// Reads samples from the clip into <paramref name="destino"/> starting at <paramref name="desplazamiento"/>. true if Unity provides them.
        internal static unsafe bool LeerMuestras(AudioClip clip, float[] destino, int desplazamiento)
        {
            ResolverIcalls();
            if (_getData == null)
            {
                var buf = new Il2CppStructArray<float>(destino.Length);
                if (!clip.GetData(buf, desplazamiento)) return false;
                for (int i = 0; i < destino.Length; i++) destino[i] = buf[i];
                return true;
            }
            IntPtr nativo = clip.m_CachedPtr;
            if (nativo == IntPtr.Zero || destino.Length == 0) return false;
            fixed (float* p = destino)
            {
                var span = new EnvolturaSpan { Inicio = (IntPtr)p, Longitud = destino.Length };
                return _getData(nativo, ref span, desplazamiento) != 0;
            }
        }

        // ---------------------------------------------------------------
        // Medida de nivel (para la autoprueba): pico y RMS en dBFS de un clip, si Unity deja leer sus muestras
        // Level measurement (for the self-test): peak and RMS in dBFS of a clip, if Unity lets its samples be read
        // ---------------------------------------------------------------
        internal static string Medir(AudioClip c)
        {
            if (c == null) return "null";
            string cabecera = $"{c.name} ({c.length:0.000} s, {c.frequency} Hz, {c.channels} canal(es), {c.samples} muestras, carga {c.loadType})";
            try
            {
                long total = (long)c.samples * c.channels;
                if (total <= 0 || total > 5_000_000) return cabecera + ", nivel sin medir (tamaño)";
                if (c.loadState != AudioDataLoadState.Loaded) c.LoadAudioData();
                var buf = new float[total];
                if (!LeerMuestras(c, buf, 0)) return cabecera + ", nivel sin medir (GetData = false)";
                double pico = 0, suma = 0;
                for (long i = 0; i < total; i++) { double v = Math.Abs(buf[(int)i]); if (v > pico) pico = v; suma += v * v; }
                if (pico <= 0) return cabecera + ", nivel sin medir (muestras a cero: clip comprimido o sin cargar)";
                double rms = Math.Sqrt(suma / total);
                return cabecera + $", pico {20 * Math.Log10(pico):0.0} dBFS, RMS {20 * Math.Log10(rms):0.0} dBFS";
            }
            catch (Exception e) { return cabecera + ", nivel sin medir (" + e.Message + ")"; }
        }
    }
}
