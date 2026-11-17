// PruebasInterfaz.cs
// Prueba 11: cálculos de las rejillas con desplazamiento de la interfaz (RejillaDesplazable.cs), fuera del juego:
//   - Colección: 8 columnas fijas y ventana de 2 filas; 16 unidades caben sin desplazamiento, 19 (Legión de Hierro con
//     las sueltas) no; la ventana de una facción de 8 unidades mide 1 fila;
//   - Batalla personalizada: columnas que caben en el ancho, filas de la raza de serie más grande (sin las 9xx) y
//     desplazamiento solo cuando una raza pasa de ellas;
//   - alturas con relleno y espacio entre filas;
//   - el nombre «40» de los rombos nuevos se convierte en raza con Enum.Parse (lo que hace el OnToggleChanged del juego).
// Autor: ZeroitMods.
// PruebasInterfaz.cs
// Test 11: calculations for the scrolling grids of the interface (RejillaDesplazable.cs), outside the game:
//   - Collection: 8 fixed columns and a 2-row window; 16 units fit without scrolling, 19 (Iron Legion with
//     the loose ones) do not; the window of a faction with 8 units is 1 row tall;
//   - Custom battle: columns that fit the width, rows of the largest stock race (without the 9xx) and
//     scrolling only when a race exceeds them;
//   - heights with padding and spacing between rows;
//   - the name "40" of the new diamonds is converted into a race with Enum.Parse (what the game's OnToggleChanged does).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using ZeroNucleo;

internal static class PruebasInterfaz
{
    // Enum con los mismos valores que el Race del juego (para comprobar Enum.Parse con un número fuera del enum)
    // Enum with the same values as the game's Race (to check Enum.Parse with a number outside the enum)
    private enum RazaPrueba { IronLegion, Gruntkin, RavenHost, TaelindorForest, SanguineCourt, SakuraDynasty, DeepstoneHold, DrakosaurBrood, Special, OlympianLeague }

    public static void Todas(Action<bool, string> comprobar)
    {
        // -------- Colección: 8 columnas fijas, ventana de 2 filas
        // -------- Collection: 8 fixed columns, 2-row window
        int col = RejillaDesplazable.Columnas(true, 8, 1040f, 0f, 121f, 10f);
        comprobar(col == 8, "interfaz: la Colección usa sus 8 columnas fijas");
        comprobar(!RejillaDesplazable.NecesitaDesplazamiento(16, col, 2), "interfaz: 16 unidades (2 filas de 8) caben sin desplazamiento");
        comprobar(RejillaDesplazable.NecesitaDesplazamiento(19, col, 2) && RejillaDesplazable.FilasNecesarias(19, col) == 3,
            "interfaz: 19 unidades (Legión de Hierro con sueltas) necesitan 3 filas y desplazamiento");
        comprobar(RejillaDesplazable.FilasVisibles(19, col, 2) == 2 && RejillaDesplazable.FilasVisibles(8, col, 2) == 1
                  && RejillaDesplazable.FilasVisibles(0, col, 2) == 1,
            "interfaz: la ventana enseña 2 filas con 19, 1 con 8 y nunca 0");
        float h2 = RejillaDesplazable.AlturaFilas(2, 127f, 10f, 0f, 0f);
        comprobar(Math.Abs(h2 - 264f) < 0.01f, $"interfaz: 2 filas de 127 con 10 de espacio miden 264 ({h2})");
        comprobar(Math.Abs(RejillaDesplazable.AlturaFilas(1, 127f, 10f, 4f, 6f) - 137f) < 0.01f && RejillaDesplazable.AlturaFilas(0, 127f, 10f, 0f, 0f) == 0f,
            "interfaz: 1 fila con relleno 4/6 mide 137 y 0 filas, 0");

        // -------- Batalla personalizada: columnas por ancho y filas de la raza de serie más grande
        // -------- Custom battle: columns by width and rows of the largest stock race
        int colB = RejillaDesplazable.Columnas(false, 0, 420f, 10f, 95f, 10f);
        comprobar(colB == 4, $"interfaz: en 420 px con celdas de 95, espacio 10 y relleno 10 caben 4 columnas ({colB})");
        comprobar(RejillaDesplazable.Columnas(false, 0, 0f, 0f, 95f, 10f) == 1 && RejillaDesplazable.Columnas(true, 0, 420f, 10f, 95f, 10f) == 4,
            "interfaz: sin ancho sale 1 columna; con «fijas» a 0 se calculan por ancho");
        var listas = new List<IEnumerable<int>>
        {
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 941, 944, 950 },   // Legión de Hierro: 16 de serie + 3 sueltas
                                                                                             // Iron Legion: 16 stock + 3 loose
            new[] { 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 945, 960 },
            new[] { 900, 901, 903, 904, 905, 906, 907, 908, 909, 910, 911, 912, 913 },        // facción A: todo 9xx
                                                                                              // faction A: all 9xx
            null,
        };
        int maxSerie = RejillaDesplazable.MaximoDeSerie(listas);
        comprobar(maxSerie == 16, $"interfaz: la raza de serie más grande tiene 16 unidades sin contar las 9xx ({maxSerie})");
        int filasMax = RejillaDesplazable.FilasNecesarias(maxSerie, colB);
        comprobar(filasMax == 4 && RejillaDesplazable.NecesitaDesplazamiento(19, colB, filasMax) && !RejillaDesplazable.NecesitaDesplazamiento(16, colB, filasMax)
                  && !RejillaDesplazable.NecesitaDesplazamiento(13, colB, filasMax),
            "interfaz: ventana de 4 filas; 19 cartas desplazan, 16 y las 13 de Astra Militarum no");

        // -------- Rombos nuevos: el juego convierte el nombre del rombo en raza con Enum.Parse
        // -------- New diamonds: the game converts the diamond name into a race with Enum.Parse
        bool parseOk;
        try { parseOk = (int)(RazaPrueba)Enum.Parse(typeof(RazaPrueba), "40") == 40 && (int)(RazaPrueba)Enum.Parse(typeof(RazaPrueba), "41") == 41; }
        catch { parseOk = false; }
        comprobar(parseOk, "interfaz: Enum.Parse(Race, \"40\") da la raza 40 aunque no esté en el enum (nombre de los rombos nuevos)");
    }
}
