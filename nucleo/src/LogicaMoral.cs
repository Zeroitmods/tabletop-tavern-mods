// LogicaMoral.cs
// Cálculo puro (sin tipos del juego ni de Unity) del módulo «Impertérritos» (0.7.6, 03-11-2026): cómo se deja la moral
// de una escuadra del jugador para que no flaquee ni huya. Lo aplica MoralJugador.cs en cada fotograma de batalla y se
// prueba fuera del juego (proyecto pruebas\, PruebasMoral.cs).
// Cómo decide el juego (MoraleSystem, sistema Burst que no se puede parchear): la moral actual baja o sube cada fotograma
// (acotada entre 0 y la máxima); si queda en el umbral o por debajo, la escuadra se rompe (estado 2) y otro sistema le
// pone la etiqueta de huida; si queda en el 45 % de la máxima o por debajo, flaquea (estado 1).
// Autor: ZeroitMods.
// LogicaMoral.cs
// Pure calculation (no game or Unity types) of the "Impertérritos" module (0.7.6, 03-11-2026): how the morale
// of a player squad is set so that it neither wavers nor flees. MoralJugador.cs applies it every battle frame and it is
// tested outside the game (pruebas\ project, PruebasMoral.cs).
// How the game decides (MoraleSystem, a Burst system that cannot be patched): current morale goes down or up every frame
// (clamped between 0 and the maximum); if it ends at the threshold or below, the squad breaks (state 2) and another system
// puts the flee tag on it; if it ends at 45 % of the maximum or below, it wavers (state 1).
// Author: ZeroitMods.

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: moral de una escuadra del jugador
    // Section: morale of a player squad
    // -----------------------------------------------------------------------
    public static class LogicaMoral
    {
        /// <summary>
        /// Umbral de huida que se pone con «sin huida». Tiene que ser muy negativo: la moral nunca baja de 0 en el sistema
        /// del juego, pero la carga por el flanco y la nieve restan de golpe fuera de él, sin suelo.
        /// Flee threshold set with "no flee". It has to be very negative: morale never drops below 0 in the game's
        /// system, but the flank charge and the snow subtract abruptly outside it, with no floor.
        /// </summary>
        public const float UmbralSinHuida = -1000000f;

        // Estados del juego (MoraleComponent.MoraleState)
        // Game states (MoraleComponent.MoraleState)
        public const byte Firme = 0, Flaquea = 1, Rota = 2;

        /// <summary>
        /// Ajusta la moral de una escuadra del jugador. <paramref name="sinHuida"/>: umbral de huida inalcanzable (nunca se
        /// rompe); <paramref name="sinVacilar"/>: moral actual llena (no flaquea). El estado se corrige para el fotograma en
        /// curso (el juego lo recalcula en el siguiente). Devuelve true si cambió algo (hay que escribirlo en la entidad).
        /// Adjusts the morale of a player squad. <paramref name="sinHuida"/>: unreachable flee threshold (it never
        /// breaks); <paramref name="sinVacilar"/>: full current morale (it does not waver). The state is corrected for the current
        /// frame (the game recalculates it in the next one). Returns true if anything changed (it must be written to the entity).
        /// </summary>
        public static bool Ajustar(ref float actual, float maxima, ref float umbral, ref byte estado, bool sinHuida, bool sinVacilar)
        {
            bool cambio = false;
            if (sinHuida && umbral > UmbralSinHuida / 2f)
            {
                umbral = UmbralSinHuida;
                cambio = true;
            }
            if (sinVacilar && maxima > 0f && actual < maxima)
            {
                actual = maxima;
                cambio = true;
            }
            byte nuevo = estado;
            // Revisión externa (2.ª ronda, hallazgo 9): sin «sin huida», una escuadra ya rota se queda rota (no «firme» huyendo)
            // External review (2nd round, finding 9): without «sin huida», a squad that is already broken stays broken (not «steady» while fleeing)
            if (sinVacilar && maxima > 0f && (sinHuida || estado != Rota)) nuevo = Firme;
            else if (sinHuida && estado == Rota) nuevo = Flaquea;
            if (nuevo != estado)
            {
                estado = nuevo;
                cambio = true;
            }
            return cambio;
        }
    }
}
