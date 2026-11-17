// LogicaInteres.cs
// Cálculo puro (sin tipos del juego ni de Unity) de la «Tesorería imperial», el bonus de campaña de la raza 41 (héroes
// 903 y 904), para poder probarlo fuera del juego (proyecto pruebas\, PruebasInteres.cs).
// Decisión de Zero (18-10-2026, revision_18oct_equilibrio.md §2.1): el interés doble solo vale para los primeros 5 de
// oro de interés del turno: interés = base + mín(base, 5). Con el tope 30 de «Zero - Economía» y sin objetos de
// interés da como mucho 35 por turno (antes, con el x2 entero, 60), y en el juego de serie (tope 5) da 10, lo mismo que
// Tokugawa. Lo usa el postfix FaccionesNuevas.TrasGetTotalInterest sobre el interés TOTAL que ya calculó el juego
// (GoldManager.GetTotalInterest), que incluye los objetos de interés: con Iron Bank (x2) y tope 30 sale 30 -> 60 -> 65
// (revisión del 18-10, ronda 2, B-4; si el x2 debe ir solo sobre el interés base es la pregunta P-A, decisión de Zero).
// Autor: ZeroitMods.
// LogicaInteres.cs
// Pure calculation (no game or Unity types) of the «Tesorería imperial», the campaign bonus of race 41 (heroes
// 903 and 904), so it can be tested outside the game (project pruebas\, PruebasInteres.cs).
// Zero's decision (18-10-2026, revision_18oct_equilibrio.md §2.1): the double interest only applies to the first 5 of
// gold interest per turn: interest = base + min(base, 5). With the cap of 30 from «Zero - Economía» and no interest
// items it gives at most 35 per turn (previously, with the full x2, 60), and in the stock game (cap 5) it gives 10, the same as
// Tokugawa. It is used by the postfix FaccionesNuevas.TrasGetTotalInterest on the TOTAL interest the game already calculated
// (GoldManager.GetTotalInterest), which includes interest items: with Iron Bank (x2) and a cap of 30 it goes 30 -> 60 -> 65
// (review of 18-10, round 2, B-4; whether the x2 should apply only to the base interest is question P-A, Zero's decision).
// Author: ZeroitMods.

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: lógica pura del interés de la Tesorería imperial
    // Section: pure logic of the Tesorería imperial interest
    // -----------------------------------------------------------------------
    public static class LogicaInteres
    {
        // Parte del interés que se duplica: el tope de interés del juego de serie (GoldManager, maxInterest = 5)
        // Part of the interest that is doubled: the interest cap of the stock game (GoldManager, maxInterest = 5)
        public const int TramoDoble = 5;

        /// <summary>
        /// Interés del turno con la Tesorería imperial: el interés del juego más lo que tenga de sus primeros
        /// <see cref="TramoDoble"/> de oro. Un interés de 0 o negativo se devuelve tal cual (no se toca).
        /// Turn interest with the Tesorería imperial: the game's interest plus whatever it has from its first
        /// <see cref="TramoDoble"/> gold. An interest of 0 or negative is returned as is (left untouched).
        /// </summary>
        public static int TesoreriaImperial(int interes)
        {
            if (interes <= 0) return interes;
            return interes + System.Math.Min(interes, TramoDoble);
        }
    }
}
