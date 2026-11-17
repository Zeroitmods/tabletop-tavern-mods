// CasillasHeroes.cs
// Cálculos puros (sin tipos de Unity ni del juego) de la pantalla de héroes ampliada (PantallaHeroes.cs): cuántas
// casillas hacen falta, cuántas tarjetas de facción nuevas hay que clonar y en qué orden quedan las tarjetas cuando la
// última es la «facción misteriosa» del juego. Va aparte para poder probarlo fuera del juego (pruebas\PruebasHeroes.cs).
// CasillasHeroes.cs
// Pure calculations (without Unity or game types) for the expanded heroes screen (PantallaHeroes.cs): how many
// slots are needed, how many new faction cards must be cloned and in what order the cards end up when the
// last one is the game's "mystery faction". Kept separate so it can be tested outside the game (pruebas\PruebasHeroes.cs).
//
// Cómo reparte el juego las casillas (versión instalada el 17-10-2026, leído del código nativo de GameAssembly.dll):
//   - PlayPanel.OpenPanel pasa a CommanderScreenView.LoadRoster los héroes jugables seguidos de los «próximamente»
//     (HeroData.Heroes + HeroData.ComingSoonHeroes: héroes de razas que aún no están en el juego, hoy los 2 olímpicos).
//   - LoadRoster enseña min(casillas - 2, héroes) héroes por orden, 2 por tarjeta, y deja SIEMPRE las 2 últimas casillas
//     y la última tarjeta para la facción misteriosa («??????????», ShowMysteryFaction).
// Así que caben todos si casillas - 2 >= jugables + próximamente.
// Autor: ZeroitMods.
// How the game distributes the slots (version installed on 17-10-2026, read from the native code of GameAssembly.dll):
//   - PlayPanel.OpenPanel passes to CommanderScreenView.LoadRoster the playable heroes followed by the "coming soon" ones
//     (HeroData.Heroes + HeroData.ComingSoonHeroes: heroes of races not yet in the game, currently the 2 Olympian ones).
//   - LoadRoster shows min(slots - 2, heroes) heroes in order, 2 per card, and ALWAYS leaves the last 2 slots
//     and the last card for the mystery faction ("??????????", ShowMysteryFaction).
// So all fit if slots - 2 >= playable + coming soon.
// Author: ZeroitMods.

using System;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: casillas y tarjetas de la pantalla de héroes
    // Section: slots and cards of the heroes screen
    // -----------------------------------------------------------------------
    public static class CasillasHeroes
    {
        /// <summary>Casillas que el juego reserva al final para la facción misteriosa (una tarjeta de 2).</summary>
        /// Slots the game reserves at the end for the mystery faction (one card of 2).
        public const int CasillasMisterio = 2;

        /// <summary>
        /// Casillas de la pantalla de héroes de serie en el juego instalado (actualización del 17-10-2026: 20 = 10 tarjetas).
        /// Solo es el respaldo: la autoprueba usa las que ve la pantalla al abrirse (PantallaHeroes.CasillasDeSerie).
        /// Slots of the stock heroes screen in the installed game (update of 17-10-2026: 20 = 10 cards).
        /// It is only the fallback: the self-test uses the ones the screen sees when it opens (PantallaHeroes.CasillasDeSerie).
        /// </summary>
        public const int CasillasDeSerieJuego = 20;

        /// <summary>
        /// Casillas que hacen falta para enseñar todos los héroes: jugables + próximamente + la reserva de la facción
        /// misteriosa (0 en un juego sin ella). Valores negativos cuentan como 0.
        /// Slots needed to show all heroes: playable + coming soon + the mystery faction
        /// reserve (0 in a game without it). Negative values count as 0.
        /// </summary>
        public static int Necesarias(int jugables, int proximamente, int reserva)
            => Math.Max(0, jugables) + Math.Max(0, proximamente) + Math.Max(0, reserva);

        /// <summary>Tarjetas de facción (2 casillas cada una) para un número de casillas, redondeando hacia arriba.</summary>
        /// Faction cards (2 slots each) for a number of slots, rounding up.
        public static int Tarjetas(int casillas) => casillas <= 0 ? 0 : (casillas + 1) / 2;

        /// <summary>Tarjetas que hay que clonar para llegar a las casillas necesarias (0 si ya caben).</summary>
        /// Cards that must be cloned to reach the needed slots (0 if they already fit).
        public static int TarjetasNuevas(int jugables, int proximamente, int reserva, int tarjetasActuales)
            => Math.Max(0, Tarjetas(Necesarias(jugables, proximamente, reserva)) - Math.Max(0, tarjetasActuales));

        /// <summary>
        /// Orden de las tarjetas tras ampliar: para cada posición nueva, el índice de la tarjeta original que va ahí (0 o
        /// más) o -(k+1) para el clon k. Con facción misteriosa, los clones van justo antes de la última tarjeta (la
        /// misteriosa sigue la última, que es donde la busca el juego); sin ella, al final.
        /// Order of the cards after expanding: for each new position, the index of the original card that goes there (0 or
        /// more) or -(k+1) for clone k. With a mystery faction, the clones go right before the last card (the
        /// mystery one stays last, which is where the game looks for it); without it, at the end.
        /// </summary>
        public static int[] OrdenTarjetas(int originales, int nuevas, bool misterioAlFinal)
        {
            if (originales < 0 || nuevas < 0) throw new ArgumentOutOfRangeException(nameof(originales));
            var orden = new int[originales + nuevas];
            int corte = misterioAlFinal && originales > 0 ? originales - 1 : originales;
            int j = 0;
            for (int i = 0; i < corte; i++) orden[j++] = i;
            for (int k = 0; k < nuevas; k++) orden[j++] = -(k + 1);
            for (int i = corte; i < originales; i++) orden[j++] = i;
            return orden;
        }

        /// <summary>
        /// Índice de la tarjeta que sirve de plantilla para los clones: la última tarjeta normal (la penúltima si la última
        /// es la misteriosa, que el juego pinta como bloqueada). -1 si no hay ninguna normal.
        /// Index of the card that serves as the template for the clones: the last normal card (the second to last if the last
        /// one is the mystery one, which the game draws as locked). -1 if there is no normal one.
        /// </summary>
        public static int Plantilla(int originales, bool misterioAlFinal)
            => misterioAlFinal ? originales - 2 : originales - 1;
    }
}
