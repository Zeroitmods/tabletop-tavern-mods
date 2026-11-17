// Prestigio.cs
// Progresión de prestigio propia de la facción A (niveles II y III del juego), encima de la de serie.
// Prestigio.cs
// Faction A's own prestige progression (levels II and III of the game), on top of the standard one.
//
// Cómo funciona el prestigio en el juego (código oficial):
//   - Nivel 0 = sin prestigio (I), nivel 1 = Prestige II (plata), nivel 2 = Prestige III (oro, máximo).
//   - Unidades a distancia, por nivel: +5 alcance y +5 precisión (UnitPrestigeSystemSetUpSystem) y +100 de munición
//     de escuadra (EntityWatcher). No reciben moral (solo cuerpo a cuerpo y magos). Al llegar a III ganan un rasgo
//     al azar de PRESTIGE_TRAIT_POOL que no tengan ya (la Guardia ya tiene perforante y antiinfantería).
//   - Es genérico por tipo de unidad: no hay forma de configurarlo por unidad sin código.
// Extra de la Guardia (solo la 900, aplicado desde el latido de batalla a cada soldado y escuadra, una sola vez):
//   - II «veteranos»: +2 daño a distancia, +10 moral.
//   - III «tropas de choque con fusil hot-shot»: +4 daño a distancia, +10 alcance, +20 moral.
// La precisión no se toca: con 80 de base, +10 de héroe y +10 de prestigio de serie ya llega a 100 en III.
// No se aplica en la resolución automática de batallas (allí solo cuenta el prestigio de serie).
// How prestige works in the game (official code):
//   - Level 0 = no prestige (I), level 1 = Prestige II (silver), level 2 = Prestige III (gold, maximum).
//   - Ranged units, per level: +5 range and +5 accuracy (UnitPrestigeSystemSetUpSystem) and +100 squad
//     ammo (EntityWatcher). They get no morale (only melee and mages). On reaching III they gain a random
//     trait from PRESTIGE_TRAIT_POOL that they do not already have (the Guard already has armor-piercing and anti-infantry).
//   - It is generic per unit type: there is no way to configure it per unit without code.
// Guard extra (only unit 900, applied from the battle heartbeat to each soldier and squad, only once):
//   - II «veterans»: +2 ranged damage, +10 morale.
//   - III «shock troops with hot-shot rifles»: +4 ranged damage, +10 range, +20 morale.
// Accuracy is left untouched: with 80 base, +10 from the hero and +10 from default prestige it already reaches 100 at III.
// It is not applied in automatic battle resolution (there only the default prestige counts).

using System;
using System.Collections.Generic;
using TJ.Morale;
using Unity.Entities;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: tabla de extras y aplicación en batalla
    // Section: extras table and application in battle
    // -----------------------------------------------------------------------
    public static class Prestigio
    {
        // Extras por nivel de prestigio (índice 0 = sin prestigio, 1 = II, 2 = III)
        // Extras per prestige level (index 0 = no prestige, 1 = II, 2 = III)
        private static readonly int[] Dano = { 0, 2, 4 };
        private static readonly float[] Alcance = { 0f, 0f, 10f };
        private static readonly float[] Moral = { 0f, 10f, 20f };

        // Escuadras con la moral ya ajustada y escuadras pendientes (su MoraleComponent aún no existía)
        // Squads with morale already adjusted and pending squads (their MoraleComponent did not exist yet)
        private static readonly HashSet<long> _moralHecha = new HashSet<long>();
        private static readonly Dictionary<long, (Entity escuadra, int nivel, int id)> _pendientes = new Dictionary<long, (Entity, int, int)>();
        private static bool _errorAvisado;

        private static int Acotar(int nivel) => Math.Clamp(nivel, 0, 2);
        public static int DanoExtra(int nivel) => Dano[Acotar(nivel)];
        public static float AlcanceExtra(int nivel) => Alcance[Acotar(nivel)];

        /// <summary>Nivel de prestigio (0-2) de la escuadra en la batalla actual, según el SquadManager del juego.</summary>
        /// Prestige level (0-2) of the squad in the current battle, according to the game's SquadManager.
        public static int NivelDeEscuadra(int squadId)
        {
            try
            {
                var bm = BattleManager.Instance;
                if (bm == null || bm.SquadManager == null) return 0;
                return Acotar(bm.SquadManager.GetSquadPrestige(squadId));
            }
            catch (Exception e)
            {
                if (!_errorAvisado) { _errorAvisado = true; Plugin.Error("Prestigio: no se pudo leer el nivel de la escuadra: " + e.Message); }
                return 0;
            }
        }

        /// <summary>Sube la moral máxima y actual de la escuadra según su prestigio (una vez por escuadra y batalla).</summary>
        /// Raises the squad's maximum and current morale according to its prestige (once per squad and battle).
        public static void AplicarMoral(EntityManager em, Entity escuadra, int squadId, int nivel)
        {
            if (nivel <= 0 || Moral[Acotar(nivel)] <= 0f) return;
            long clave = ((long)escuadra.Index << 32) | (uint)escuadra.Version;
            if (_moralHecha.Contains(clave)) return;
            if (!IntentarMoral(em, escuadra, squadId, nivel)) _pendientes[clave] = (escuadra, nivel, squadId);
            else _moralHecha.Add(clave);
        }

        /// <summary>Reintenta las escuadras cuya moral aún no existía (llamado en cada latido).</summary>
        /// Retries the squads whose morale did not exist yet (called on every heartbeat).
        public static void ReintentarPendientes(EntityManager em)
        {
            if (_pendientes.Count == 0) return;
            foreach (var kv in new List<KeyValuePair<long, (Entity escuadra, int nivel, int id)>>(_pendientes))
            {
                if (!em.Exists(kv.Value.escuadra)) { _pendientes.Remove(kv.Key); continue; }
                if (IntentarMoral(em, kv.Value.escuadra, kv.Value.id, kv.Value.nivel)) { _pendientes.Remove(kv.Key); _moralHecha.Add(kv.Key); }
            }
        }

        // Suma la moral si la escuadra ya tiene su MoraleComponent; false si aún no
        // Adds the morale if the squad already has its MoraleComponent; false if not yet
        private static bool IntentarMoral(EntityManager em, Entity escuadra, int squadId, int nivel)
        {
            var tipo = ComponentType.ReadOnly(Il2CppInterop.Runtime.Il2CppType.Of<MoraleComponent>());
            if (!em.Exists(escuadra) || !em.HasComponent(escuadra, tipo)) return false;
            MoraleComponent moral = em.GetComponentData<MoraleComponent>(escuadra);
            float extra = Moral[Acotar(nivel)];
            moral.MaxMorale += extra;
            moral.CurrentMorale += extra;
            em.SetComponentData(escuadra, moral);
            Plugin.Info($"Prestigio {(nivel == 1 ? "II" : "III")} de la Guardia (escuadra {squadId}): +{extra} moral, " +
                        $"+{DanoExtra(nivel)} daño a distancia, +{AlcanceExtra(nivel)} alcance (encima del prestigio de serie).");
            return true;
        }

        /// <summary>Batalla nueva: se olvida lo anterior.</summary>
        /// New battle: the previous state is forgotten.
        public static void Reiniciar()
        {
            _moralHecha.Clear();
            _pendientes.Clear();
        }
    }
}
