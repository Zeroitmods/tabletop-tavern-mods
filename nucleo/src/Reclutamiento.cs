// Reclutamiento.cs
// Hace que la facción A aparezca en las ofertas de reclutamiento DEL JUGADOR, juegue la facción que juegue.
// Reclutamiento.cs
// Makes faction A appear in the PLAYER's recruitment offers, whatever faction they play.
//
// Por qué así: todos los pools del juego (jugador y enemigo) salen de UnitsOfRaceDictionary por raza, así que meter
// la unidad ahí la daría también al enemigo. En su lugar, la 900 se queda fuera de ese diccionario y se inyecta
// con un postfix en los cuatro métodos que SOLO usa el jugador (comprobado en el código del juego):
//   - GetSquadsToRecruitBasedOnReputation: recompensas de eventos, encuentros y consumibles (raza del héroe)
//   - GetSquadsToRecruitForPack: packs de cartas de la tienda (por rango; el pack 4 de unidad emblema se respeta)
//   - GetSquadsToRecruitForRaceTown: reclutar en ciudades (limitado por tamaño de ciudad)
//   - GetSquadsToRecruitForRaceBattle: carta de recompensa tras batalla (limitado por rareza)
// La generación de ejércitos enemigos, guarniciones y señores de la guerra no pasa por estos métodos.
// Se sustituye una carta (no se añade) porque el panel crea tantas cartas como elementos tenga la oferta.
// El sorteo usa la semilla de la propia oferta: la misma oferta da siempre el mismo resultado (sin trampas al recargar).
// Solo para la 900 (manifiesto: reclutamiento «ofertas_jugador»). Las unidades sueltas no pasan por aquí: están en
// la lista de su raza (UnidadGuardia.AsegurarListas) y salen por las tiradas normales del juego, también para la IA.
// Why this way: all the game's pools (player and enemy) come from UnitsOfRaceDictionary by race, so putting
// the unit there would give it to the enemy too. Instead, unit 900 stays out of that dictionary and is injected
// with a postfix in the four methods that ONLY the player uses (checked in the game code):
//   - GetSquadsToRecruitBasedOnReputation: rewards from events, encounters and consumables (hero's race)
//   - GetSquadsToRecruitForPack: tavern card packs (by rank; the emblem unit pack 4 is respected)
//   - GetSquadsToRecruitForRaceTown: recruiting in towns (limited by town size)
//   - GetSquadsToRecruitForRaceBattle: reward card after battle (limited by rarity)
// The generation of enemy armies, garrisons and warlords does not go through these methods.
// A card is replaced (not added) because the panel creates as many cards as the offer has elements.
// The draw uses the offer's own seed: the same offer always gives the same result (no cheating by reloading).
// Only for unit 900 (manifest: recruitment «ofertas_jugador»). Individual units do not go through here: they are in
// their race's list (UnidadGuardia.AsegurarListas) and come out through the game's normal rolls, for the AI too.

using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: inyección en ofertas del jugador
    // Section: injection into player offers
    // -----------------------------------------------------------------------
    public static class Reclutamiento
    {
        // Sales distintas por tipo de oferta para que no coincidan los sorteos entre métodos con la misma semilla
        // Different salts per offer type so draws do not coincide between methods with the same seed
        private const int SalReputacion = 9001, SalPack = 9002, SalCiudad = 9003, SalBatalla = 9004;

        // Rango (1-4) de la facción A según su rareza actual en el catálogo (lo que diga unit_overrides.json)
        // Rank (1-4) of faction A according to its current rarity in the catalog (whatever unit_overrides.json says)
        private static int RangoGuardia(TabletopTavernData data) => data.GetUnitTierFromUnitName(UnidadGuardia.Guardia);

        // Sustituye una carta de la oferta por la facción A si toca por probabilidad
        // Replaces a card in the offer with faction A if the probability roll says so
        private static void Inyectar(Il2CppStructArray<UnitName> oferta, int semilla, int sal, string donde)
        {
            if (!Plugin.Operativo || oferta == null || oferta.Length == 0) return;
            // Solo mientras el manifiesto diga que la 900 va en las ofertas del jugador (F1; en la F3 pasa a su raza)
            // Only while the manifest says unit 900 goes in the player's offers (F1; in F3 it moves to its race)
            if (!UnidadGuardia.GuardiaEnOfertas || !UnidadGuardia.RegistradaEnCatalogo()) return;
            for (int i = 0; i < oferta.Length; i++) if (oferta[i] == UnidadGuardia.Guardia) return;

            var azar = new Random(unchecked(semilla * 7919 + sal));
            double prob = Plugin.CfgSiempreEnOfertas.Value || AutoPrueba.ForzarOferta ? 1.0 : Math.Clamp(Plugin.CfgProbabilidad.Value, 0f, 1f);
            if (azar.NextDouble() >= prob) return;

            int pos = azar.Next(oferta.Length);
            UnitName antes = oferta[pos];
            oferta[pos] = UnidadGuardia.Guardia;
            Plugin.Info($"Oferta de {donde}: Guardia Imperial en la carta {pos + 1}/{oferta.Length} (sustituye a {antes}).");
        }

        // Postfix: recompensas por reputación (eventos, encuentros, consumibles). Cualquier rango.
        // Postfix: reputation rewards (events, encounters, consumables). Any rank.
        public static void TrasReputacion(Il2CppStructArray<UnitName> __result, int _seed)
        {
            try { Inyectar(__result, _seed, SalReputacion, "recompensa"); }
            catch (Exception e) { Plugin.Error("TrasReputacion: " + e.Message); }
        }

        // Postfix: packs de la tienda. Solo en el pack de su mismo rango y nunca en el de unidad emblema (4).
        // Postfix: tavern packs. Only in the pack of its own rank and never in the emblem unit pack (4).
        public static void TrasPack(TabletopTavernData __instance, Il2CppStructArray<UnitName> __result, int _seed, int _cardPackID)
        {
            try
            {
                if (_cardPackID == 4 || _cardPackID != RangoGuardia(__instance)) return;
                Inyectar(__result, _seed, SalPack, $"pack de tienda {_cardPackID}");
            }
            catch (Exception e) { Plugin.Error("TrasPack: " + e.Message); }
        }

        // Postfix: ciudades. Mismos límites que el juego: aldea solo rango 1, castillo hasta 2, ciudad hasta 3.
        // Postfix: towns. Same limits as the game: village only rank 1, castle up to 2, city up to 3.
        public static void TrasCiudad(TabletopTavernData __instance, Il2CppStructArray<UnitName> __result, int _seed, TownSize _townsize)
        {
            try
            {
                int rango = RangoGuardia(__instance);
                if (_townsize == TownSize.Village && rango >= 2) return;
                if (_townsize == TownSize.Castle && rango >= 3) return;
                if (_townsize == TownSize.City && rango >= 4) return;
                Inyectar(__result, _seed, SalCiudad, $"ciudad ({_townsize})");
            }
            catch (Exception e) { Plugin.Error("TrasCiudad: " + e.Message); }
        }

        // Postfix: recompensa tras batalla. Común: rangos 1-2; poco común: 2-3; rara: solo 3.
        // Postfix: post-battle reward. Common: ranks 1-2; uncommon: 2-3; rare: 3 only.
        public static void TrasBatalla(TabletopTavernData __instance, Il2CppStructArray<UnitName> __result, int _seed, UnitRarity _unitRarity)
        {
            try
            {
                int rango = RangoGuardia(__instance);
                if (_unitRarity == UnitRarity.Common && rango > 2) return;
                if (_unitRarity == UnitRarity.Uncommon && (rango < 2 || rango > 3)) return;
                if (_unitRarity == UnitRarity.Rare && rango != 3) return;
                Inyectar(__result, _seed, SalBatalla, $"recompensa de batalla ({_unitRarity})");
            }
            catch (Exception e) { Plugin.Error("TrasBatalla: " + e.Message); }
        }
    }
}
