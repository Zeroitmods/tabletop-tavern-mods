// SorteoObjetos.cs
// Sorteo de los objetos nuevos según la partida (objetos.md §5), en .NET puro para poder simularlo fuera del juego y en
// la autoprueba (sin CampaignSaveManager):
//   - Afinidad: conjunto de «grupos» afines (facción A, starwars o una raza del juego): la raza del héroe, toda facción de la
//     que el ejército lleve 3 o más escuadras (para facción A y facción B cuentan también los ids 900-913 y 921-934) y la
//     facción del contexto (enemigo al saquear el campo o con Grave Robbers, raza de la ciudad al saquearla).
//   - Sustitución por carta: con ProbabilidadAfin se cambia por un objeto afín elegible; si no, con ProbabilidadGeneral
//     por cualquier objeto nuevo elegible; si no, se queda la de serie. Elegible = no lo tienes, no lo vendiste hace poco
//     y no está ya en ese mismo sorteo. Los objetos de comandante pesan el doble con su héroe.
//   - Semilla: la del juego (GetSeededRandom) más una sal propia y la posición de la carta, para que la tirada no cambie al
//     recargar la partida (igual que Grave Robbers).
//   - 0.7.4 (decisión de Zero 01-11-2026; revisión de datos I8): desde el juego 1.10.19 las ofertas siguen la rareza (común
//     60 %, poco común 30 %, rara 10 %). Una carta solo se cambia por un objeto nuevo de su MISMA rareza (si no se sabe la
//     rareza de la carta, no se toca), así el reparto del juego se mantiene. Y la última carta de un cofre de tres («la que
//     encaja con un escuadrón de tu ejército») no se toca.
// También extrae las unidades del ejército del texto del save de campaña (bloque playerArmy).
// SorteoObjetos.cs
// Draw of the new gear according to the run (objetos.md §5), in plain .NET so it can be simulated outside the game and in
// the self-test (without CampaignSaveManager):
//   - Affinity: set of related "groups" (faction A, starwars or a game race): the hero's race, every faction of which the
//     army has 3 or more squads (for faction A and faction B the ids 900-913 and 921-934 also count) and the
//     context faction (enemy when looting the field or with Grave Robbers, the city's race when looting it).
//   - Replacement per card: with ProbabilidadAfin it is swapped for an eligible related item; if not, with ProbabilidadGeneral
//     for any eligible new gear; if not, the default one stays. Eligible = you don't own it, you didn't sell it recently
//     and it is not already in that same draw. Commander items weigh double with their hero.
//   - Seed: the game's (GetSeededRandom) plus its own salt and the card's position, so the roll doesn't change when
//     reloading the run (same as Grave Robbers).
//   - 0.7.4 (Zero's decision 01-11-2026; data review I8): since game 1.10.19 the offers follow rarity (common
//     60%, uncommon 30%, rare 10%). A card is only swapped for a new item of its SAME rarity (if the card's
//     rarity is unknown, it is left alone), so the game's distribution is kept. And the last card of a chest of three ("the one that
//     fits a squad in your army") is left alone.
// It also extracts the army's units from the campaign save text (playerArmy block).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: datos mínimos de un objeto para el sorteo
    // Section: minimum data of an item for the draw
    // -----------------------------------------------------------------------
    /// <summary>Lo que el sorteo necesita de un objeto nuevo.</summary>
    /// What the draw needs from a new gear item.
    public sealed class ObjetoSorteo
    {
        public int Id;
        public string Afinidad;      // facción A, starwars o el nombre de una raza del juego
                                     // faction A, starwars or the name of a game race
        public int Comandante;       // héroe con el que pesa el doble (0 = ninguno)
                                     // hero with whom it weighs double (0 = none)
        public int Rareza = -1;      // 0.7.4: rareza (número del enum GearRarity: 0 común, 1 poco común, 2 rara); -1 = sin dato
                                     // 0.7.4: rarity (number from the GearRarity enum: 0 common, 1 uncommon, 2 rare); -1 = no data
    }

    // -----------------------------------------------------------------------
    // Sección: lógica del sorteo
    // Section: draw logic
    // -----------------------------------------------------------------------
    public static class SorteoObjetos
    {
        public const string Grupo40k = "40k";
        public const string GrupoStarWars = "starwars";
        public const int MinimoEscuadras = 3;                 // escuadras de una facción para que sea afín
                                                              // squads of a faction for it to count as affine
        private const int Sal = 70913;                        // sal propia de la semilla (no choca con las del juego)
                                                              // seed's own salt (does not clash with the game's)

        /// <summary>Grupo de afinidad de una raza: 40 -> facción A, 41 -> starwars, una de serie -> su nombre (null si no aplica).</summary>
        /// Affinity group of a race: 40 -> faction A, 41 -> starwars, a default one -> its name (null if not applicable).
        public static string GrupoDeRaza(int raza, Func<int, string> nombreRaza)
        {
            if (raza == 40) return Grupo40k;
            if (raza == 41) return GrupoStarWars;
            if (raza < 0 || raza > 7) return null;            // Special, Liga Olímpica u otras: sin objetos propios
                                                              // Special, Olympic League or others: no gear of their own
            return nombreRaza?.Invoke(raza);
        }

        /// <summary>Grupo de afinidad de una unidad: los ids 900-913 cuentan como facción A y 921-934 como facción B; si no, su raza.</summary>
        /// Affinity group of a unit: ids 900-913 count as faction A and 921-934 as faction B; otherwise, its race.
        public static string GrupoDeUnidad(int unidad, Func<int, int> razaDeUnidad, Func<int, string> nombreRaza)
        {
            if (unidad >= 900 && unidad <= 913) return Grupo40k;
            if (unidad >= 921 && unidad <= 934) return GrupoStarWars;
            return GrupoDeRaza(razaDeUnidad(unidad), nombreRaza);
        }

        /// <summary>Grupos afines por el ejército: los de toda facción con MinimoEscuadras o más escuadras.</summary>
        /// Groups affine to the army: those of every faction with MinimoEscuadras or more squads.
        public static HashSet<string> GruposDelEjercito(IEnumerable<int> unidades, Func<int, int> razaDeUnidad, Func<int, string> nombreRaza)
        {
            var cuenta = new Dictionary<string, int>();
            foreach (int u in unidades)
            {
                string g = GrupoDeUnidad(u, razaDeUnidad, nombreRaza);
                if (g == null) continue;
                cuenta[g] = cuenta.TryGetValue(g, out int n) ? n + 1 : 1;
            }
            return new HashSet<string>(cuenta.Where(kv => kv.Value >= MinimoEscuadras).Select(kv => kv.Key));
        }

        /// <summary>
        /// Generador propio de una carta (SplitMix64) a partir de la semilla del juego, la sal y la posición: determinista
        /// (la misma partida y nodo dan las mismas cartas) y sin la correlación que dan semillas seguidas en System.Random.
        /// A card's own generator (SplitMix64) based on the game seed, the salt and the position: deterministic
        /// (the same run and node give the same cards) and without the correlation that consecutive seeds give in System.Random.
        /// </summary>
        public sealed class Generador
        {
            private ulong _estado;
            public Generador(int semillaJuego, int posicion)
            {
                unchecked { _estado = ((ulong)(uint)semillaJuego << 32) ^ (uint)(Sal + posicion * 7919); Siguiente(); }
            }
            private ulong Siguiente()
            {
                unchecked
                {
                    ulong z = _estado += 0x9E3779B97F4A7C15UL;
                    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                    return z ^ (z >> 31);
                }
            }
            /// <summary>Número uniforme en [0, 1).</summary>
            /// Uniform number in [0, 1).
            public double NextDouble() => (Siguiente() >> 11) * (1.0 / (1UL << 53));
        }

        /// <summary>
        /// Cambia las cartas de un sorteo según la afinidad. Devuelve cuántas cambió. <paramref name="cartas"/> se modifica
        /// en el sitio (ids de GearID; los nuevos son 700-729). Un objeto ya presente en las cartas no se repite.
        /// 0.7.4: con <paramref name="rarezaDeCarta"/> (rareza de un objeto de serie, -1 si no se sabe) solo se cambia una carta
        /// por un objeto nuevo de su misma rareza; las posiciones de <paramref name="protegidas"/> no se tocan.
        /// Changes the cards of a draw according to affinity. Returns how many it changed. <paramref name="cartas"/> is modified
        /// in place (GearID ids; the new ones are 700-729). An item already present in the cards is not repeated.
        /// 0.7.4: with <paramref name="rarezaDeCarta"/> (rarity of a base-game item, -1 if unknown) a card is only swapped
        /// for a new gear item of the same rarity; the positions in <paramref name="protegidas"/> are not touched.
        /// </summary>
        public static int Sustituir(IList<int> cartas, IReadOnlyList<ObjetoSorteo> objetos, ISet<string> afines, ISet<int> excluidos,
                                    int heroe, int semillaJuego, double probAfin, double probGeneral, bool modoPrueba = false,
                                    Func<int, int> rarezaDeCarta = null, ICollection<int> protegidas = null)
        {
            if (cartas == null || objetos == null || objetos.Count == 0) return 0;
            int cambios = 0;
            for (int i = 0; i < cartas.Count; i++)
            {
                if (protegidas != null && protegidas.Contains(i)) continue;
                var rng = new Generador(semillaJuego, i);
                // Elegibles: ni excluidos (tenidos o vendidos) ni ya en el sorteo
                // Eligible: neither excluded (owned or sold) nor already in the draw
                var elegibles = objetos.Where(o => !excluidos.Contains(o.Id) && !cartas.Contains(o.Id)).ToList();
                if (elegibles.Count == 0) break;
                // 0.7.4: misma rareza que la carta original (sin dato de rareza, la carta se queda como está)
                // 0.7.4: same rarity as the original card (with no rarity data, the card stays as is)
                if (rarezaDeCarta != null)
                {
                    int rareza = RarezaSegura(rarezaDeCarta, cartas[i]);
                    if (rareza < 0) continue;
                    elegibles = elegibles.Where(o => o.Rareza == rareza).ToList();
                    if (elegibles.Count == 0) continue;
                }
                var afinesElegibles = afines == null || afines.Count == 0 ? new List<ObjetoSorteo>() : elegibles.Where(o => afines.Contains(o.Afinidad)).ToList();
                ObjetoSorteo elegido = null;
                double tirada = rng.NextDouble();
                if (afinesElegibles.Count > 0 && (modoPrueba || tirada < probAfin))
                    elegido = Elegir(afinesElegibles, heroe, rng);
                else if (modoPrueba || rng.NextDouble() < probGeneral)
                    elegido = Elegir(elegibles, heroe, rng);
                if (elegido == null) continue;
                cartas[i] = elegido.Id;
                cambios++;
            }
            return cambios;
        }

        /// <summary>Posiciones que no se tocan en un sorteo: la última de un cofre de tres (no tienda) encaja con tu ejército.</summary>
        /// Positions that are not touched in a draw: the last one of a three-item chest (not a shop) fits your army.
        public static int[] Protegidas(int numeroCartas, bool tienda) => !tienda && numeroCartas == 3 ? new[] { 2 } : Array.Empty<int>();

        // Rareza de una carta con red: una excepción cuenta como «sin dato»
        // Rarity of a card with a safety net: an exception counts as "no data"
        private static int RarezaSegura(Func<int, int> rarezaDeCarta, int carta)
        {
            try { return rarezaDeCarta(carta); }
            catch { return -1; }
        }

        // Elige al azar con peso 1 (2 para el objeto de comandante del héroe activo)
        // Picks at random with weight 1 (2 for the active hero's commander item)
        private static ObjetoSorteo Elegir(List<ObjetoSorteo> lista, int heroe, Generador rng)
        {
            double total = lista.Sum(o => Peso(o, heroe));
            double t = rng.NextDouble() * total;
            foreach (var o in lista)
            {
                t -= Peso(o, heroe);
                if (t < 0) return o;
            }
            return lista[lista.Count - 1];
        }

        public static double Peso(ObjetoSorteo o, int heroe) => o.Comandante > 0 && o.Comandante == heroe ? 2.0 : 1.0;

        // ---------------------------------------------------------------
        // Ejército desde el texto del save de campaña
        // Army from the campaign save text
        // ---------------------------------------------------------------
        // Una escuadra del bloque playerArmy: UnitName, UnitIndex (-1 = casilla vacía) e isEmptySquad, en ese orden
        // A squad from the playerArmy block: UnitName, UnitIndex (-1 = empty slot) and isEmptySquad, in that order
        private static readonly Regex ReEscuadra = new Regex(
            "\"UnitName\"\\s*:\\s*(\\d+)[^{}]*?\"UnitIndex\"\\s*:\\s*(-?\\d+)\\s*,\\s*\"isEmptySquad\"\\s*:\\s*(true|false)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Ids de unidad de las escuadras reales del ejército del jugador (bloque «playerArmy» del save).</summary>
        /// Unit ids of the real squads in the player's army ("playerArmy" block of the save).
        public static List<int> UnidadesDelEjercito(string jsonCampana)
        {
            var r = new List<int>();
            if (string.IsNullOrEmpty(jsonCampana)) return r;
            int ini = jsonCampana.IndexOf("\"playerArmy\"", StringComparison.Ordinal);
            if (ini < 0) return r;
            int abre = jsonCampana.IndexOf('[', ini);
            if (abre < 0) return r;
            // Cierre del array: corchetes equilibrados (los GUID y números no llevan corchetes)
            // End of the array: balanced brackets (GUIDs and numbers have no brackets)
            int nivel = 0, fin = -1;
            for (int i = abre; i < jsonCampana.Length; i++)
            {
                char c = jsonCampana[i];
                if (c == '[') nivel++;
                else if (c == ']' && --nivel == 0) { fin = i; break; }
            }
            if (fin < 0) return r;
            foreach (Match m in ReEscuadra.Matches(jsonCampana.Substring(abre, fin - abre + 1)))
            {
                if (!int.TryParse(m.Groups[1].Value, out int u) || !int.TryParse(m.Groups[2].Value, out int indice)) continue;
                if (indice < 0 || m.Groups[3].Value == "true") continue;
                r.Add(u);
            }
            return r;
        }
    }
}
