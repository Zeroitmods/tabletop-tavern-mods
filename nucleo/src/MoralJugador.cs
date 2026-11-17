// MoralJugador.cs
// Módulo «Impertérritos» (núcleo 0.7.6, petición de Zero del 03-11-2026): las escuadras del JUGADOR no flaquean ni huyen
// por moral; la IA enemiga sigue como siempre. Lo declara el módulo ZeroImperterritos con «efectos.moralJugador» (Modulos.cs).
// Por qué con código y no con datos: el juego no tiene ningún atributo que lo haga. «Impávido» (Stalwart) solo da
// inmunidad al terror; la escuadra sigue flaqueando y rompiéndose por bajas, flanqueo, aliados en huida, pérdidas del
// ejército o fuego (MoraleSystem). Y la moral la decide un sistema Burst, que no se puede parchear.
// Cómo:
//   1) Batallas en tiempo real (campaña y batalla personalizada): postfix de EntityWatcher.Update (mensaje de Unity: se
//      llama siempre, no se inlinea). En cada fotograma, a las escuadras con PlayerSquad (el juego se la pone a las del
//      jugador, SquadId > 0) se les deja el umbral de huida inalcanzable y la moral llena (LogicaMoral). El sistema del
//      juego corre después en el mismo fotograma y ya no puede romperlas. La retirada que ordena el jugador no se toca
//      (va por otra etiqueta). Las escuadras enemigas no llevan PlayerSquad: no se tocan.
//   2) Resolución automática: prefix de AutoResolveSimulation.UpdateMorale. El juego la llama dos veces por paso, una por
//      bando (aiSide = false es el jugador); para el jugador no se ejecuta, así su moral no baja y no se rompe. Comprobado
//      con el xref del binario 1.11.1 (03-11-2026): dos llamadas reales desde Tick, sin copias inlineadas.
// Multijugador: con cualquier módulo de Zero activo el núcleo bloquea cooperativo, versus y clasificaciones (Saves.cs). Ese
// bloqueo se puede apagar (Multijugador.Bloquear): por eso los dos parches se apartan además si hay una partida en línea
// (VersusArmies.Active o CoopSession.Active del juego, leídos por nombre): solo un cliente con la moral cambiada se
// desincronizaría (revisión externa del 03-11, hallazgo 1).
// Componentes: MoraleComponent son tres float y un byte (blittable): nada de ref/out de structs no blittables.
// Autor: ZeroitMods.
// MoralJugador.cs
// Module «Impertérritos» (core 0.7.6, request from Zero dated 03-11-2026): the PLAYER's squads neither waver nor flee
// from morale; the enemy AI works as usual. The module ZeroImperterritos declares it with «efectos.moralJugador» (Modulos.cs).
// Why with code and not with data: the game has no attribute that does this. «Impávido» (Stalwart) only grants
// immunity to terror; the squad still wavers and breaks from casualties, flanking, fleeing allies, army losses
// or fire (MoraleSystem). And morale is decided by a Burst system, which cannot be patched.
// How:
//   1) Real-time battles (campaign and custom battle): postfix of EntityWatcher.Update (Unity message: it is
//      always called, never inlined). On every frame, squads with PlayerSquad (the game sets it on the player's
//      squads, SquadId > 0) get an unreachable flee threshold and full morale (LogicaMoral). The game's system
//      then runs later in the same frame and can no longer break them. The retreat ordered by the player is not touched
//      (it goes through a different tag). Enemy squads do not have PlayerSquad: they are not touched.
//   2) Auto-resolve: prefix of AutoResolveSimulation.UpdateMorale. The game calls it twice per step, once per
//      side (aiSide = false is the player); for the player it does not run, so their morale does not drop and they do not break. Verified
//      with the xref of the 1.11.1 binary (03-11-2026): two real calls from Tick, no inlined copies.
// Multiplayer: with any Zero module active the core blocks co-op, versus and rankings (Saves.cs). That
// block can be turned off (Multijugador.Bloquear): that is why both patches also step aside if there is an online run
// (the game's VersusArmies.Active or CoopSession.Active, read by name): only a client with altered morale
// would desync (external review of 03-11, finding 1).
// Components: MoraleComponent is three floats and a byte (blittable): no ref/out of non-blittable structs.
// Author: ZeroitMods.

using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;
using TJ.Morale;
using Unity.Collections;
using Unity.Entities;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: estado del efecto y parches
    // Section: effect state and patches
    // -----------------------------------------------------------------------
    public static class MoralJugador
    {
        private static EfectoMoral _efecto;
        private static EntityQuery _consulta;
        private static bool _consultaCreada, _errorAvisado, _autoAvisado, _avisoHilo, _avisoTipos, _cuentaHecha;
        // Mundo ECS en el que se creó la consulta (puntero nativo). Revisión externa del 03-11 (2.ª ronda, hallazgo 1): una
        // consulta de un mundo destruido lee memoria liberada (fallo nativo, no se puede capturar); se rehace si cambia
        // ECS world in which the query was created (native pointer). External review of 03-11 (2nd round, finding 1): a
        // query from a destroyed world reads freed memory (native crash, cannot be caught); it is rebuilt if it changes
        private static IntPtr _mundoConsulta = IntPtr.Zero;
        // EntityWatcher de la batalla en curso (puntero): si Update ve otro, es una batalla nueva aunque el postfix de SetUp
        // no haya saltado (3.ª pasada de la revisión, hallazgo 1)
        // EntityWatcher of the current battle (pointer): if Update sees a different one, it is a new battle even if the SetUp
        // postfix did not fire (3rd pass of the review, finding 1)
        private static IntPtr _vigilante = IntPtr.Zero;
        private static int _escuadrasBatalla;
        // ¿Hay partida en línea? (se mira al empezar cada batalla y, en la resolución automática, una vez por fotograma)
        // Is there an online run? (checked at the start of each battle and, in auto-resolve, once per frame)
        private static bool _enLinea;
        private static int _fotogramaEnLinea = -1;
        private static readonly System.Collections.Generic.HashSet<long> _vistas = new System.Collections.Generic.HashSet<long>();

        /// <summary>¿Está activo algún módulo con «moralJugador» en esta sesión?</summary>
        /// Is any module with «moralJugador» active in this session?
        public static bool Activo => _efecto != null && (_efecto.SinHuida || _efecto.SinVacilar);
        /// <summary>Efecto activo (null = de serie).</summary>
        /// Active effect (null = default).
        public static EfectoMoral Efecto => _efecto;
        /// <summary>Escuadras del jugador distintas ajustadas en la sesión (para la autoprueba y el log).</summary>
        /// Distinct player squads adjusted in the session (for the self-test and the log).
        public static int EscuadrasSesion { get; private set; }
        /// <summary>Pasos de la resolución automática en los que se saltó la moral del jugador en la sesión.</summary>
        /// Auto-resolve steps in which the player's morale was skipped in the session.
        public static int PasosAutoSesion { get; private set; }

        /// <summary>Recoge el efecto del resultado de los módulos (Nucleo.Aplicar).</summary>
        /// Picks up the effect from the modules' result (Nucleo.Aplicar).
        public static void Configurar(ResultadoModulos r)
        {
            _efecto = r?.Moral;
            if (Activo) Plugin.Info($"Moral del jugador: {(_efecto.SinHuida ? "sin huida" : "")}{(_efecto.SinHuida && _efecto.SinVacilar ? " y " : "")}{(_efecto.SinVacilar ? "sin flaquear" : "")} (módulo {r.ModuloMoral}); la IA, de serie.");
        }

        // Postfix de EntityWatcher.SetUp: batalla nueva, consulta y cuentas de cero
        // Postfix of EntityWatcher.SetUp: new battle, query and counters reset to zero
        public static void TrasEntityWatcherSetUp()
        {
            _consultaCreada = false;
            _mundoConsulta = IntPtr.Zero;
            _cuentaHecha = false;
            _escuadrasBatalla = 0;
            _vistas.Clear();
            _enLinea = PartidaEnLinea();
            if (_enLinea && Activo) Plugin.Aviso("Moral del jugador: partida en línea; en esta batalla la moral queda de serie.");
        }

        // Postfix de EntityWatcher.Update: moral de las escuadras del jugador (cada fotograma; son pocas entidades)
        // Postfix of EntityWatcher.Update: morale of the player's squads (every frame; there are few entities)
        public static void TrasEntityWatcherUpdate(EntityWatcher __instance)
        {
            if (!Activo || _errorAvisado) return;
            try
            {
                if (__instance == null || !__instance.setup) return;
                if (__instance.Pointer != _vigilante) { TrasEntityWatcherSetUp(); _vigilante = __instance.Pointer; }
                if (_enLinea) return;
                var mundo = World.DefaultGameObjectInjectionWorld;
                // Solo un mundo vivo; la consulta se rehace si el mundo no es en el que se creó
                // Only a live world; the query is rebuilt if the world is not the one it was created in
                if (mundo == null || !mundo.IsCreated) { _consultaCreada = false; return; }
                EntityManager em = mundo.EntityManager;
                if (!_consultaCreada || mundo.Pointer != _mundoConsulta)
                {
                    _consulta = em.CreateEntityQuery(new Il2CppStructArray<ComponentType>(new[]
                    {
                        ComponentType.ReadOnly(Il2CppType.Of<PlayerSquad>()),
                        ComponentType.ReadWrite(Il2CppType.Of<MoraleComponent>()),
                    }));
                    _consultaCreada = true;
                    _mundoConsulta = mundo.Pointer;
                }
                NativeArray<Entity> escuadras = _consulta.ToEntityArray(Allocator.Temp);
                try
                {
                    for (int i = 0; i < escuadras.Length; i++)
                    {
                        Entity e = escuadras[i];
                        MoraleComponent m = em.GetComponentData<MoraleComponent>(e);
                        float actual = m.CurrentMorale, umbral = m.MoraleThreshold;
                        byte estado = m.MoraleState;
                        if (!LogicaMoral.Ajustar(ref actual, m.MaxMorale, ref umbral, ref estado, _efecto.SinHuida, _efecto.SinVacilar)) continue;
                        m.CurrentMorale = actual;
                        m.MoraleThreshold = umbral;
                        m.MoraleState = estado;
                        em.SetComponentData(e, m);
                        if (_vistas.Add(((long)e.Index << 32) | (uint)e.Version))
                        {
                            _escuadrasBatalla++;
                            EscuadrasSesion++;
                            if (_escuadrasBatalla == 1) Plugin.Info("Moral del jugador: escuadras del jugador impertérritas en esta batalla (la IA, de serie).");
                            CuentaDeLaBatalla(em, escuadras.Length);
                        }
                    }
                }
                finally { escuadras.Dispose(); }
            }
            catch (Exception e)
            {
                // Un fallo aquí se avisa una vez y el efecto se para en la sesión: la batalla sigue de serie
                // A failure here is reported once and the effect stops for the session: the battle continues as default
                _errorAvisado = true;
                Plugin.Aviso("Moral del jugador: no se pudo ajustar la moral (" + e.GetType().Name + ": " + e.Message + "); el resto de la sesión, moral de serie.");
            }
        }

        // Prefix de AutoResolveSimulation.UpdateMorale(own, foes, aiSide): para el bando del jugador (aiSide = false) no se
        // ejecuta, así su moral no baja ni se rompe. Para la IA, el original tal cual. Nunca lanza.
        // Por qué no afecta a la IA (código público, AutoResolveSimulation.cs:895-959, y firma de la 1.11.1 en el volcado:
        // UpdateMorale(Il2CppReferenceArray<AutoResolveSquad> own, Il2CppReferenceArray<AutoResolveSquad> foes, bool aiSide),
        // dos arrays, es decir, punteros, y un bool): el método solo ESCRIBE en «own» (moral, Broken, ArmyLosses, anillos de
        // bajas y el aviso de aliados en huida de su propio bando); de «foes» solo LEE si causan terror. Tick lo llama una vez
        // por bando y paso (aiSide = true para la IA). La moral inicial se pone fuera de él. La resolución automática es la de
        // las batallas del jugador; no hay combates IA contra IA en campaña
        // Prefix of AutoResolveSimulation.UpdateMorale(own, foes, aiSide): for the player's side (aiSide = false) it does not
        // run, so their morale neither drops nor breaks. For the AI, the original as is. Never throws.
        // Why it does not affect the AI (public code, AutoResolveSimulation.cs:895-959, and the 1.11.1 signature in the dump:
        // UpdateMorale(Il2CppReferenceArray<AutoResolveSquad> own, Il2CppReferenceArray<AutoResolveSquad> foes, bool aiSide),
        // two arrays, that is, pointers, and a bool): the method only WRITES to «own» (morale, Broken, ArmyLosses, casualty
        // rings and the fleeing-allies warning of its own side); from «foes» it only READS whether they cause terror. Tick calls it once
        // per side and step (aiSide = true for the AI). The initial morale is set outside of it. Auto-resolve is the one used for
        // the player's battles; there are no AI vs AI fights in the campaign
        public static bool AntesDeUpdateMorale(bool __2)
        {
            try
            {
                if (!Activo || __2) return true;
                // Partida en línea: de serie (se mira como mucho una vez por fotograma)
                // Online run: default (checked at most once per frame)
                int f = UnityEngine.Time.frameCount;
                if (f != _fotogramaEnLinea) { _fotogramaEnLinea = f; _enLinea = PartidaEnLinea(); }
                if (_enLinea) return true;
                PasosAutoSesion++;
                if (!_autoAvisado) { _autoAvisado = true; Plugin.Info("Moral del jugador: resolución automática sin pérdida de moral para el jugador (la IA, de serie)."); }
                return false;
            }
            catch (Exception e)
            {
                // Revisión externa (2.ª ronda, hallazgo 11): avisar una vez si esto falla (p. ej. fuera del hilo principal)
                // External review (2nd round, finding 11): warn once if this fails (e.g. off the main thread)
                if (!_avisoHilo) { _avisoHilo = true; Plugin.Aviso("Moral del jugador: la resolución automática queda de serie (" + e.GetType().Name + ": " + e.Message + ")."); }
                return true;
            }
        }

        // Una vez por batalla: escuadras del jugador frente a escuadras con moral que no son del jugador (las que no se tocan),
        // para comprobar en el log que la IA queda de serie (revisión externa, 2.ª ronda, hallazgo 8)
        // Once per battle: player squads versus squads with morale that are not the player's (the ones that are not touched),
        // to check in the log that the AI stays as stock (external review, 2nd round, finding 8)
        private static void CuentaDeLaBatalla(EntityManager em, int delJugador)
        {
            if (_cuentaHecha) return;
            _cuentaHecha = true;
            try
            {
                var todas = em.CreateEntityQuery(new Il2CppStructArray<ComponentType>(new[] { ComponentType.ReadOnly(Il2CppType.Of<MoraleComponent>()) }));
                int total = todas.CalculateEntityCount();
                todas.Dispose();
                Plugin.Info($"Moral del jugador: {delJugador} escuadras del jugador con moral impertérrita; {Math.Max(0, total - delJugador)} escuadras con moral que no son del jugador, sin tocar.");
            }
            catch { }
        }

        // ¿Hay una partida en línea en curso? (versus o cooperativo). Por nombre y por reflexión: si el juego mueve los
        // tipos, se da por que no (el bloqueo del multijugador sigue siendo la protección principal)
        // Is there an online run in progress? (versus or co-op). By name and by reflection: if the game moves the
        // types, assume there is not (the multiplayer block is still the main protection)
        private static bool PartidaEnLinea()
        {
            int vistos = 0;
            foreach (string tipo in new[] { "TJ.Versus.VersusArmies, TabletopTavern.Core", "TJ.Versus.Net.CoopSession, TabletopTavern.Core" })
            {
                try
                {
                    var t = Plugin.TipoPorNombre(tipo);
                    if (t != null) vistos++;
                    var p = t?.GetProperty("Active", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (p != null && p.GetValue(null) is bool b && b) return true;
                }
                catch { }
            }
            // Revisión externa (2.ª ronda, hallazgo 12): si el juego movió los dos tipos, se dice una vez
            // External review (2nd round, finding 12): if the game moved the two types, it is reported once
            if (vistos == 0 && !_avisoTipos) { _avisoTipos = true; Plugin.Aviso("Moral del jugador: no se encuentran VersusArmies ni CoopSession; no se puede saber si hay partida en línea (sigue el bloqueo del multijugador)."); }
            return false;
        }
    }
}
