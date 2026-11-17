// Batalla.cs
// Ajustes de las unidades nuevas dentro de la batalla, todos desde código gestionado (nada de Burst):
//   1) Proyectil: el juego asigna el proyectil por nombre de unidad y una id desconocida dispara FLECHAS. Un
//      «latido» (postfix de EntityWatcher.Update, cada pocos frames) cambia ShootAttack.ProjectileEntity de las
//      unidades nuevas al proyectil del manifiesto (bala de fusil, bala de cañón, hacha, virote...: el que el juego da
//      a su unidad base o el que pida la ficha). Solo se cambia la flecha por defecto (respeta flechas incendiarias).
//      El sistema Burst solo instancia lo que haya en ese dato. Vale para los dos bandos (jugador e IA).
//   2) Caballería con jinete: el juego solo pone el componente Cavalry (que monta el jinete en la silla) a una lista
//      fija de unidades (UnitSetUpSystem.cs:337-354). En el mismo latido se añade a las unidades nuevas marcadas
//      «caballeria» en el manifiesto; SaddleSetUpSystem monta el jinete en cuanto lo ve. Opción Batalla.JineteCaballeria.
//   3) Estela roja (solo la 900): TrailRendererSetup.SetupTrail asigna una estela de un pool a cada bala nueva. Si la
//      bala es de un guardia, se pinta de rojo la estela asignada; al reutilizarla para otra unidad recupera su color.
//   4) Prestigio: en el mismo latido se aplica el extra de prestigio propio de la Guardia (solo la 900, Prestigio.cs).
//   5) Pasiva de batalla prestada (F2): el juego pone la pasiva de cada raza con un switch al crear la escuadra
//      (SquadManager) y una raza nueva cae fuera. En el latido, a cada escuadra de las razas 40/41 se le añade la
//      etiqueta y el componente de su raza prestada (facción A: Iron Resolve de Iron Legion; facción B:
//      Hunter's Patience de Taelindor); los sistemas Burst de esas pasivas solo miran la etiqueta. Se añaden unos
//      fotogramas después de desplegar (inofensivo). No cuenta en la resolución automática de batallas.
// Batalla.cs
// Adjustments for the new units within battle, all from managed code (no Burst):
//   1) Projectile: the game assigns the projectile by unit name and an unknown id fires ARROWS. A
//      «heartbeat» (postfix of EntityWatcher.Update, every few frames) changes ShootAttack.ProjectileEntity of the
//      new units to the manifest projectile (rifle bullet, cannonball, axe, bolt...: the one the game gives
//      to its base unit or the one the sheet asks for). Only the default arrow is replaced (respects fire arrows).
//      The Burst system only instantiates what is in that data. Works for both sides (player and AI).
//   2) Cavalry with rider: the game only adds the Cavalry component (which mounts the rider in the saddle) to a fixed
//      list of units (UnitSetUpSystem.cs:337-354). In the same heartbeat it is added to the new units marked
//      «caballeria» in the manifest; SaddleSetUpSystem mounts the rider as soon as it sees it. Option Batalla.JineteCaballeria.
//   3) Red trail (only 900): TrailRendererSetup.SetupTrail assigns a trail from a pool to each new
//      bullet. If the bullet belongs to a guard, the assigned trail is painted red; when reused for another unit it recovers its color.
//   4) Prestige: in the same heartbeat the Guard's own prestige bonus is applied (only 900, Prestigio.cs).
//   5) Borrowed battle passive (F2): the game sets each race's passive with a switch when creating the squad
//      (SquadManager) and a new race falls outside it. In the heartbeat, each squad of races 40/41 gets the
//      tag and component of its borrowed race (faction A: Iron Resolve from Iron Legion; faction B:
//      Hunter's Patience from Taelindor); the Burst systems for those passives only look at the tag. They are added a few
//      frames after deploying (harmless). Not counted in automatic battle resolution.

using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: latido de batalla, proyectil y caballería
    // Section: battle heartbeat, projectile and cavalry
    // -----------------------------------------------------------------------
    public static class Batalla
    {
        private const int CadaFrames = 5;                       // frecuencia del latido
                                                                // heartbeat frequency
        private static int _frames;
        private static bool _consultasCreadas;
        private static EntityQuery _consultaTiradores;          // entidades con Unit + RangedUnitTag
                                                                // entities with Unit + RangedUnitTag
        private static EntityQuery _consultaCuerpo;             // entidades con Unit + MeleeUnitTag (caballería)
                                                                // entities with Unit + MeleeUnitTag (cavalry)
        private static EntityQuery _consultaReferencias;        // singleton EntitiesReferences
                                                                // EntitiesReferences singleton
        private static bool _referenciasLeidas;
        private static Entity _flecha;
        private static readonly Dictionary<string, Entity> _proyectiles = new Dictionary<string, Entity>();
        private static readonly HashSet<long> _vistas = new HashSet<long>();   // entidades ya revisadas (índice+versión)
                                                                               // entities already reviewed (index+version)
        private static int _corregidasBatalla, _jinetesBatalla;
        private static bool _errorLatidoAvisado, _jineteDesactivado;
        // F2: pasiva prestada de las razas nuevas
        // F2: borrowed passive of the new races
        private static EntityQuery _consultaUnidades;                        // todas las entidades con Unit
                                                                             // all entities with Unit
        private static readonly HashSet<long> _vistasPasiva = new HashSet<long>();      // unidades ya revisadas
                                                                                        // units already reviewed
        private static readonly HashSet<long> _escuadrasPasiva = new HashSet<long>();   // escuadras con la pasiva puesta
                                                                                        // squads with the passive applied
        private static int _pasivasBatalla, _ultimasUnidades = -1, _latidosPasiva;
        private static bool _pasivaDesactivada;

        // Clave única de una entidad (índice y versión)
        // Unique key of an entity (index and version)
        private static long Clave(Entity e) => ((long)e.Index << 32) | (uint)e.Version;
        private static bool Iguales(Entity a, Entity b) => a.Index == b.Index && a.Version == b.Version;

        // Postfix de EntityWatcher.SetUp: empieza batalla nueva, se olvida lo de la anterior
        // Postfix of EntityWatcher.SetUp: a new battle starts, what was from the previous one is forgotten
        public static void TrasEntityWatcherSetUp()
        {
            _vistas.Clear();
            _consultasCreadas = false;
            _referenciasLeidas = false;
            _proyectiles.Clear();
            _corregidasBatalla = 0;
            _jinetesBatalla = 0;
            _errorLatidoAvisado = false;
            _rojas.Clear();
            _vistasPasiva.Clear();
            _escuadrasPasiva.Clear();
            _pasivasBatalla = 0;
            _ultimasUnidades = -1;
            _latidosPasiva = 0;
            Prestigio.Reiniciar();
        }

        // Lee del singleton EntitiesReferences las entidades prefabricadas de cada proyectil (nombres del manifiesto)
        // Reads from the EntitiesReferences singleton the prefab entities of each projectile (manifest names)
        private static void LeerProyectiles(EntitiesReferences refs)
        {
            _flecha = refs.arrowPrefabEntity;
            _proyectiles["flecha"] = refs.arrowPrefabEntity;
            _proyectiles["bala"] = refs.bulletPrefabEntity;
            _proyectiles["bala_canon"] = refs.cannonBallPrefabEntity;
            _proyectiles["catapulta"] = refs.gobberLobberProjectilePrefabEntity;
            _proyectiles["virote"] = refs.ballistaBoltPrefabEntity;
            _proyectiles["flecha_fuego"] = refs.flamingArrowPrefabEntity;
            _proyectiles["hacha"] = refs.throwingAxePrefabEntity;
            _proyectiles["garra"] = refs.siegeclawProjectilePrefabEntity;
            _proyectiles["estrella"] = refs.starHurlerProjectilePrefabEntity;
        }

        // Postfix de EntityWatcher.Update: corrige proyectil (y caballería) de las unidades nuevas recién desplegadas
        // Postfix of EntityWatcher.Update: fixes the projectile (and cavalry) of newly deployed new units
        public static void TrasEntityWatcherUpdate(EntityWatcher __instance)
        {
            if (!Plugin.Operativo) return;
            if (++_frames % CadaFrames != 0) return;
            try
            {
                if (!__instance.setup || !UnidadGuardia.AlgunaRegistrada()) return;
                var mundo = World.DefaultGameObjectInjectionWorld;
                if (mundo == null) return;
                EntityManager em = mundo.EntityManager;

                if (!_consultasCreadas)
                {
                    _consultaTiradores = em.CreateEntityQuery(new Il2CppStructArray<ComponentType>(new[]
                    {
                        ComponentType.ReadOnly(Il2CppType.Of<Unit>()),
                        ComponentType.ReadOnly(Il2CppType.Of<RangedUnitTag>()),
                    }));
                    _consultaCuerpo = em.CreateEntityQuery(new Il2CppStructArray<ComponentType>(new[]
                    {
                        ComponentType.ReadOnly(Il2CppType.Of<Unit>()),
                        ComponentType.ReadOnly(Il2CppType.Of<MeleeUnitTag>()),
                    }));
                    _consultaReferencias = em.CreateEntityQuery(new Il2CppStructArray<ComponentType>(new[]
                    {
                        ComponentType.ReadOnly(Il2CppType.Of<EntitiesReferences>()),
                    }));
                    _consultaUnidades = em.CreateEntityQuery(new Il2CppStructArray<ComponentType>(new[]
                    {
                        ComponentType.ReadOnly(Il2CppType.Of<Unit>()),
                    }));
                    _consultasCreadas = true;
                }
                if (!_referenciasLeidas)
                {
                    if (_consultaReferencias.CalculateEntityCount() == 0) return;
                    LeerProyectiles(_consultaReferencias.GetSingleton<EntitiesReferences>());
                    _referenciasLeidas = true;
                    Plugin.Info($"Batalla: referencias de proyectiles leídas (flecha {_flecha.Index}, bala {_proyectiles["bala"].Index}, " +
                                $"cañón {_proyectiles["bala_canon"].Index}, hacha {_proyectiles["hacha"].Index}, virote {_proyectiles["virote"].Index}).");
                }

                RevisarTiradores(em);
                if (UnidadGuardia.HayCaballeria && Plugin.CfgJineteCaballeria.Value && !_jineteDesactivado) RevisarCaballeria(em);
                if (FaccionesNuevas.Activas && !_pasivaDesactivada) RevisarPasivas(em);
                Prestigio.ReintentarPendientes(em);
            }
            catch (Exception ex)
            {
                if (!_errorLatidoAvisado) { Plugin.Error("Latido de batalla: " + ex); _errorLatidoAvisado = true; }
            }
        }

        // Unidades de tiro nuevas: proyectil del manifiesto y extra de prestigio de la Guardia
        // New ranged units: manifest projectile and the Guard's prestige extra
        private static void RevisarTiradores(EntityManager em)
        {
            ComponentType tipoDisparo = ComponentType.ReadOnly(Il2CppType.Of<ShootAttack>());
            NativeArray<Entity> entidades = _consultaTiradores.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entidades.Length; i++)
                {
                    Entity e = entidades[i];
                    long k = Clave(e);
                    if (_vistas.Contains(k)) continue;
                    Unit u = em.GetComponentData<Unit>(e);
                    var nueva = UnidadGuardia.Datos(u.unitName);
                    if (nueva == null) { _vistas.Add(k); continue; }
                    if (!em.HasComponent(e, tipoDisparo)) continue;          // aún sin ShootAttack: siguiente latido
                                                                             // no ShootAttack yet: next heartbeat
                    ShootAttack disparo = em.GetComponentData<ShootAttack>(e);
                    bool cambiado = false;
                    // Solo se cambia la flecha por defecto (respeta flechas incendiarias u otros efectos del juego)
                    // Only the default arrow is changed (respects fire arrows and other game effects)
                    string nombre = nueva.Datos.Proyectil ?? "flecha";
                    if (Plugin.CfgProyectilPorUnidad.Value && _proyectiles.TryGetValue(nombre, out Entity objetivo) && objetivo != Entity.Null
                        && Iguales(disparo.ProjectileEntity, _flecha) && !Iguales(objetivo, _flecha))
                    {
                        disparo.ProjectileEntity = objetivo;
                        cambiado = true;
                        _corregidasBatalla++;
                        if (_corregidasBatalla == 1 || _corregidasBatalla % 50 == 0)
                            Plugin.Info($"Batalla: proyectil de las unidades nuevas corregido ({nueva.Numero} -> {nombre}; {_corregidasBatalla} soldados en esta batalla).");
                    }
                    // Extra de prestigio propio de la Guardia (una sola vez por soldado: la entidad queda en _vistas)
                    // The Guard's own prestige extra (only once per soldier: the entity is left in _vistas)
                    int prestigio = nueva.Datos.PrestigioPropio ? Prestigio.NivelDeEscuadra(u.squadId) : 0;
                    if (prestigio > 0)
                    {
                        disparo.damageAmount += Prestigio.DanoExtra(prestigio);
                        disparo.Range += Prestigio.AlcanceExtra(prestigio);
                        cambiado = true;
                    }
                    if (cambiado) em.SetComponentData(e, disparo);
                    if (nueva.Datos.PrestigioPropio) Prestigio.AplicarMoral(em, u.squadEntity, u.squadId, prestigio);
                    _vistas.Add(k);
                }
            }
            finally { entidades.Dispose(); }
        }

        // Caballería nueva con jinete: se le añade el componente Cavalry si no lo tiene (lo usa SaddleSetUpSystem)
        // New cavalry with a rider: the Cavalry component is added if it doesn't have it (used by SaddleSetUpSystem)
        private static void RevisarCaballeria(EntityManager em)
        {
            ComponentType tipoCaballeria = ComponentType.ReadOnly(Il2CppType.Of<Cavalry>());
            NativeArray<Entity> entidades = _consultaCuerpo.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entidades.Length; i++)
                {
                    Entity e = entidades[i];
                    long k = Clave(e);
                    if (_vistas.Contains(k)) continue;
                    Unit u = em.GetComponentData<Unit>(e);
                    var nueva = UnidadGuardia.Datos(u.unitName);
                    if (nueva == null || !nueva.Datos.Caballeria || em.HasComponent(e, tipoCaballeria)) { _vistas.Add(k); continue; }
                    try
                    {
                        em.AddComponentData(e, new Cavalry { unitName = u.unitName });
                        _jinetesBatalla++;
                        if (_jinetesBatalla == 1 || _jinetesBatalla % 50 == 0)
                            Plugin.Info($"Batalla: componente de caballería con jinete añadido ({nueva.Numero}; {_jinetesBatalla} jinetes en esta batalla).");
                    }
                    catch (Exception ex)
                    {
                        // Si el juego no deja añadirlo, se desactiva para toda la sesión (la unidad lucha igual, sin jinete visible)
                        // If the game won't let it be added, it is disabled for the whole session (the unit still fights, with no visible rider)
                        _jineteDesactivado = true;
                        Plugin.Aviso("Batalla: no se pudo añadir el componente de caballería (" + ex.GetType().Name + ": " + ex.Message +
                                     "); se deja de intentar en esta sesión. La unidad lucha igual, sin jinete visible.");
                        return;
                    }
                    _vistas.Add(k);
                }
            }
            finally { entidades.Dispose(); }
        }

        // -------------------------------------------------------------------
        // Sección: pasiva de batalla prestada de las razas nuevas (F2)
        // Section: borrowed battle passive of the new races (F2)
        // -------------------------------------------------------------------
        // A cada escuadra de una raza nueva (40/41) le pone la etiqueta y el componente de la pasiva de su raza prestada.
        // Con AddComponent no genérico (componente a cero, que es como lo crea el juego) y, para Hunter's Patience, el
        // dato «a distancia» después; si el juego no deja, se avisa y se deja de intentar en la sesión.
        // Gives each squad of a new race (40/41) the tag and the component of the passive of its borrowed race.
        // With non-generic AddComponent (zeroed component, which is how the game creates it) and, for Hunter's Patience, the
        // «ranged» data afterwards; if the game doesn't allow it, a warning is logged and it stops trying for the session.
        private static void RevisarPasivas(EntityManager em)
        {
            // Cuando cambia el número de unidades (despliegue, refuerzos o bajas) y, aunque no cambie, cada 60 latidos
            // (unos 5 s): una unidad que muere y otra que aparece dejan el mismo recuento (0.6.1)
            // When the number of units changes (deployment, reinforcements or casualties) and, even if it doesn't change, every 60 heartbeats
            // (about 5 s): a unit that dies and another that appears leave the same count (0.6.1)
            int cuantas = _consultaUnidades.CalculateEntityCount();
            _latidosPasiva++;
            if (cuantas == _ultimasUnidades && _latidosPasiva < 60) return;
            _ultimasUnidades = cuantas;
            _latidosPasiva = 0;
            NativeArray<Entity> entidades = _consultaUnidades.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entidades.Length; i++)
                {
                    Entity e = entidades[i];
                    long k = Clave(e);
                    if (_vistasPasiva.Contains(k)) continue;
                    Unit u = em.GetComponentData<Unit>(e);
                    var nueva = UnidadGuardia.Datos(u.unitName);
                    if (nueva == null || !nueva.Raza.HasValue || !FaccionesNuevas.EsNueva(nueva.Raza.Value)) { _vistasPasiva.Add(k); continue; }
                    Entity escuadra = u.squadEntity;
                    long ke = Clave(escuadra);
                    if (_escuadrasPasiva.Contains(ke)) { _vistasPasiva.Add(k); continue; }
                    // Escuadra aún sin montar (sin entidad o sin su componente SquadEntity, que el juego pone al reproducir su
                    // búfer de comandos): la unidad NO se marca vista y se vuelve a mirar en el siguiente latido (0.6.1)
                    // Squad not yet assembled (no entity or no SquadEntity component, which the game adds when replaying its
                    // command buffer): the unit is NOT marked as seen and is checked again on the next heartbeat (0.6.1)
                    if (escuadra == Entity.Null || !em.Exists(escuadra) || !em.HasComponent(escuadra, ComponentType.ReadOnly(Il2CppType.Of<SquadEntity>())))
                    { _ultimasUnidades = -1; continue; }
                    Race prestada = FaccionesNuevas.Prestada(nueva.Raza.Value, r => r.PasivaPrestada, Race.IronLegion);
                    try
                    {
                        if (prestada == Race.IronLegion)
                        {
                            Anadir(em, escuadra, ComponentType.ReadWrite(Il2CppType.Of<IronLegionRaceTag>()));
                            Anadir(em, escuadra, ComponentType.ReadWrite(Il2CppType.Of<IronResolveComponent>()));
                        }
                        else if (prestada == Race.TaelindorForest)
                        {
                            Anadir(em, escuadra, ComponentType.ReadWrite(Il2CppType.Of<TaelindorForestRaceTag>()));
                            bool nuevoComp = Anadir(em, escuadra, ComponentType.ReadWrite(Il2CppType.Of<HuntersPatienceComponent>()));
                            // Como el juego: «a distancia» si la escuadra es de tipo Ranged (si no, la variante de cuerpo a cuerpo)
                            // Like the game: «ranged» if the squad is of type Ranged (otherwise, the melee variant)
                            if (nuevoComp && u.unitType == UnitType.Ranged)
                                em.SetComponentData(escuadra, new HuntersPatienceComponent { IsRanged = true });
                        }
                        else { _vistasPasiva.Add(k); continue; }   // pasiva prestada sin implementar: nada que poner
                                                                   // borrowed passive not implemented: nothing to set
                    }
                    catch (Exception ex)
                    {
                        _pasivaDesactivada = true;
                        Plugin.Aviso($"Batalla: no se pudo poner la pasiva prestada a una escuadra de la raza {(int)nueva.Raza.Value} ({ex.GetType().Name}: {ex.Message}); " +
                                     "se deja de intentar en esta sesión. Las escuadras luchan igual, sin pasiva.");
                        return;
                    }
                    _escuadrasPasiva.Add(ke);
                    _vistasPasiva.Add(k);       // vista solo cuando su escuadra ya tiene la pasiva
                                                // seen only when its squad already has the passive
                    _pasivasBatalla++;
                    if (_pasivasBatalla == 1 || _pasivasBatalla % 10 == 0)
                        Plugin.Info($"Batalla: pasiva prestada de {prestada} puesta a escuadras de las razas nuevas ({nueva.Numero}; {_pasivasBatalla} escuadras en esta batalla).");
                }
            }
            finally { entidades.Dispose(); }
        }

        // Añade un componente si la entidad no lo tiene. Devuelve true si lo ha añadido ahora.
        // Adds a component if the entity doesn't have it. Returns true if it was added now.
        private static bool Anadir(EntityManager em, Entity e, ComponentType tipo)
        {
            if (em.HasComponent(e, tipo)) return false;
            em.AddComponent(e, tipo);
            return true;
        }

        // -------------------------------------------------------------------
        // Sección: estela roja de los disparos
        // Section: red trail of the shots
        // -------------------------------------------------------------------
        private static readonly Color RojoInicio = new Color(1f, 0.12f, 0.05f, 1f);
        private static readonly Color RojoFin = new Color(1f, 0f, 0f, 0f);
        private static bool _colorOriginalLeido;
        private static Color _inicioOriginal, _finOriginal;
        private static readonly List<TrailRendererFollowerComponent> _rojas = new List<TrailRendererFollowerComponent>();
        private static bool _balaDeGuardia;              // la bala que se está montando ahora es de un guardia
                                                         // the bullet being assembled right now belongs to a guard
        private static bool _errorEstelaAvisado;

        // ¿La bala la disparó un guardia?
        // Was the bullet fired by a guard?
        private static bool EsDeGuardia(Entity bala, EntityManager em)
        {
            if (!em.HasComponent(bala, ComponentType.ReadOnly(Il2CppType.Of<Bullet>()))) return false;
            return em.GetComponentData<Bullet>(bala).shooterUnitName == UnidadGuardia.Guardia;
        }

        // Postfix de SetupTrail: pinta de rojo la estela del guardia o devuelve el color a una estela reutilizada.
        // Solo postfix: quitar el humo de pólvora exigiría un prefix que cambie un parámetro bool por referencia; se
        // deja para la F3, cuando se monten la estela y el fogonazo propios.
        // Postfix of SetupTrail: paints the guard's trail red or restores the color of a reused trail.
        // Postfix only: removing the gunpowder smoke would require a prefix that changes a bool parameter by reference; it is
        // left for F3, when the custom trail and muzzle flash are built.
        public static void TrasSetupTrail(TrailRendererSetup __instance, Entity entity, bool flamingAmmo)
        {
            if (!Plugin.Operativo || !Plugin.CfgEstelaRoja.Value || flamingAmmo) return;
            try
            {
                var mundo = World.DefaultGameObjectInjectionWorld;
                _balaDeGuardia = mundo != null && EsDeGuardia(entity, mundo.EntityManager);
                if (_balaDeGuardia)
                {
                    var lista = __instance.trailRendererFollowers;
                    for (int i = lista.Count - 1; i >= 0; i--)
                    {
                        var f = lista[i];
                        if (f == null || !Iguales(f.EntityToFollow, entity)) continue;
                        var trail = Estela(f);
                        if (trail == null) break;
                        if (!_colorOriginalLeido) { _inicioOriginal = trail.startColor; _finOriginal = trail.endColor; _colorOriginalLeido = true; }
                        trail.startColor = RojoInicio;
                        trail.endColor = RojoFin;
                        if (!_rojas.Contains(f)) _rojas.Add(f);
                        break;
                    }
                }
                else if (_rojas.Count > 0 && _colorOriginalLeido)
                {
                    // Una estela que fue roja y ahora sigue a una bala de otra unidad recupera su color
                    // A trail that was red and now follows a bullet from another unit gets its color back
                    for (int i = _rojas.Count - 1; i >= 0; i--)
                    {
                        var f = _rojas[i];
                        if (f == null) { _rojas.RemoveAt(i); continue; }
                        if (!Iguales(f.EntityToFollow, entity)) continue;
                        var trail = Estela(f);
                        if (trail != null) { trail.startColor = _inicioOriginal; trail.endColor = _finOriginal; }
                        _rojas.RemoveAt(i);
                        break;
                    }
                }
            }
            catch (Exception e) { AvisoEstela(e); }
            finally { _balaDeGuardia = false; }
        }

        // TrailRenderer de un seguidor (campo privado del juego, o el componente si no estuviera asignado)
        // TrailRenderer of a follower (private game field, or the component if it wasn't assigned)
        private static TrailRenderer Estela(TrailRendererFollowerComponent f) => f.trailRenderer != null ? f.trailRenderer : f.GetComponent<TrailRenderer>();

        private static void AvisoEstela(Exception e)
        {
            if (_errorEstelaAvisado) return;
            _errorEstelaAvisado = true;
            Plugin.Error("Estela roja (se sigue sin ella): " + e);
        }
    }
}
