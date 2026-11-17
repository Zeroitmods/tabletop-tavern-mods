// RejillaDesplazable.cs
// Cálculos puros (sin tipos de Unity ni del juego) para las rejillas con desplazamiento de la interfaz
// (InterfazColeccion.cs): cuántas columnas caben, cuántas filas hacen falta, cuántas se ven en la ventana y qué altura
// ocupa un número de filas. Va aparte para poder probarlo fuera del juego (pruebas\PruebasInterfaz.cs).
// Autor: ZeroitMods.
// RejillaDesplazable.cs
// Pure calculations (no Unity or game types) for the scrolling grids of the interface
// (InterfazColeccion.cs): how many columns fit, how many rows are needed, how many are visible in the window and what height
// a number of rows takes up. Kept separate so it can be tested outside the game (pruebas\PruebasInterfaz.cs).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: medidas de una rejilla (GridLayoutGroup) con ventana de desplazamiento
    // Section: measurements of a grid (GridLayoutGroup) with a scroll window
    // -----------------------------------------------------------------------
    public static class RejillaDesplazable
    {
        /// <summary>
        /// Columnas de la rejilla: las fijadas si la rejilla tiene «FixedColumnCount» (y son más de 0); si no, las que
        /// caben en el ancho (como hace GridLayoutGroup con «Flexible»): (ancho - relleno + espacio) / (celda + espacio),
        /// como mínimo 1.
        /// Grid columns: the fixed ones if the grid has «FixedColumnCount» (and it is greater than 0); otherwise, those that
        /// fit in the width (as GridLayoutGroup does with «Flexible»): (width - padding + spacing) / (cell + spacing),
        /// with a minimum of 1.
        /// </summary>
        public static int Columnas(bool columnasFijas, int numeroFijo, float ancho, float rellenoHorizontal, float celdaAncho, float espacioX)
        {
            if (columnasFijas && numeroFijo > 0) return numeroFijo;
            float paso = celdaAncho + espacioX;
            if (paso <= 0f || ancho <= 0f) return 1;
            int n = (int)Math.Floor((ancho - rellenoHorizontal + espacioX) / paso + 0.001f);
            return Math.Max(1, n);
        }

        /// <summary>Filas que ocupan <paramref name="elementos"/> en <paramref name="columnas"/> columnas (0 si no hay ninguno).</summary>
        /// Rows taken up by <paramref name="elementos"/> in <paramref name="columnas"/> columns (0 if there are none).
        public static int FilasNecesarias(int elementos, int columnas)
        {
            if (elementos <= 0) return 0;
            int c = Math.Max(1, columnas);
            return (elementos + c - 1) / c;
        }

        /// <summary>
        /// Filas que enseña la ventana: las que hacen falta, sin pasar de <paramref name="maxFilas"/> (lo que cabe en la
        /// pantalla de serie) y como mínimo 1, para que la ventana nunca se quede a 0 de alto.
        /// Rows the window shows: those needed, not exceeding <paramref name="maxFilas"/> (what fits on the
        /// vanilla screen) and at least 1, so the window never ends up with 0 height.
        /// </summary>
        public static int FilasVisibles(int elementos, int columnas, int maxFilas)
        {
            int filas = FilasNecesarias(elementos, columnas);
            return Math.Max(1, Math.Min(filas, Math.Max(1, maxFilas)));
        }

        /// <summary>Altura de <paramref name="filas"/> filas: relleno de arriba y abajo, celdas y espacios entre filas.</summary>
        /// Height of <paramref name="filas"/> rows: top and bottom padding, cells and spacing between rows.
        public static float AlturaFilas(int filas, float celdaAlto, float espacioY, float rellenoArriba, float rellenoAbajo)
        {
            int f = Math.Max(0, filas);
            return rellenoArriba + rellenoAbajo + f * celdaAlto + Math.Max(0, f - 1) * espacioY;
        }

        /// <summary>true si hace falta desplazamiento: hay más filas de las que caben en la ventana.</summary>
        /// true if scrolling is needed: there are more rows than fit in the window.
        public static bool NecesitaDesplazamiento(int elementos, int columnas, int maxFilas) =>
            FilasNecesarias(elementos, columnas) > Math.Max(1, maxFilas);

        /// <summary>
        /// Mayor número de unidades de serie de una facción (las de id menor que <paramref name="primerIdNuevo"/>): es lo
        /// que la pantalla de serie está pensada para enseñar sin desplazamiento. Listas vacías o nulas cuentan 0.
        /// Highest number of vanilla units of a faction (those with id lower than <paramref name="primerIdNuevo"/>): this is what
        /// the vanilla screen is designed to show without scrolling. Empty or null lists count as 0.
        /// </summary>
        public static int MaximoDeSerie(IEnumerable<IEnumerable<int>> listas, int primerIdNuevo = 900)
        {
            int maximo = 0;
            if (listas == null) return 0;
            foreach (var lista in listas)
            {
                if (lista == null) continue;
                int n = 0;
                foreach (int id in lista) if (id < primerIdNuevo) n++;
                if (n > maximo) maximo = n;
            }
            return maximo;
        }
    }
}
