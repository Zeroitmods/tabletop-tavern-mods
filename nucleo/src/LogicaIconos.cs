// LogicaIconos.cs
// Revisión del 18-10, ronda 2 (parte B): lógica pura (sin tipos de Unity, se prueba fuera del juego) de los iconos 2:3
// de las unidades nuevas.
// Problema: todos los marcos de retrato de unidad del juego (casilla y mini de la Colección, ejército de la pantalla de
// héroes, carta de unidad insignia...) son rectángulos 2:3 sin «preserveAspect», pensados para los retratos de serie de
// 512 x 768. Hasta el juego 1.10.x el icon.png de un mod tenía que medir 256 x 256 (lo exigía
// UnitVisualOverrideLoader.LoadIcon), así que el juego lo estiraba 1,5 veces a lo alto (desde la 1.11.1, ver más abajo, el
// icon.png oficial ya es 2:3 y esto solo vale para un icon.png de 256 x 256).
// Arreglo (ModeloGuardia.RegistrarIcono): se mete en la caché de iconos del juego un sprite 2:3 propio:
//   a) el «retrato.png» de la carpeta del visual si existe y mide exactamente 512 x 768;
//   b) si no, el icon.png encajado en un lienzo 2:3 transparente con este código: centrado en horizontal y pegado
//      ARRIBA. Motivo: todas las máscaras del juego enseñan solo la parte de arriba del retrato (la casilla de la
//      Colección el 70 % de arriba, el ejército de la pantalla de héroes un cuadrado de 92 x 92 sobre un retrato de
//      92 x 138, es decir, los dos tercios de arriba; la mini de 64 x 72 sobre 64 x 96, el 75 %). Con el icono en los dos
//      tercios de arriba se ve entero en todas; centrado en vertical perdería su sexto de abajo en las de 2/3.
// Formato de los píxeles: RGBA de 8 bits por canal, filas de ABAJO a ARRIBA (el orden de Texture2D.GetPixels32).
// Juego 1.11.1 (01-11-2026): el icon.png oficial es ya de 512 x 768 (UnitVisualOverrideLoader.IconWidth/IconHeight; el de
// 256 x 256 queda como LegacyIconSize, con un aviso). Con un icon.png de 512 x 768 el juego crea él mismo el sprite 2:3, así
// que el núcleo no hace nada (EsIcono2x3Oficial, leyendo solo la cabecera del PNG con MedidasPng).
// Autor: ZeroitMods.
// LogicaIconos.cs
// Review of 18-10, round 2 (part B): pure logic (no Unity types, tested outside the game) for the 2:3 icons
// of the new units.
// Problem: all of the game's unit portrait frames (Collection slot and mini, army on the heroes screen,
// flagship unit card...) are 2:3 rectangles without «preserveAspect», designed for the stock portraits of
// 512 x 768. Up to game 1.10.x a mod's icon.png had to be 256 x 256 (required by
// UnitVisualOverrideLoader.LoadIcon), so the game stretched it 1.5 times in height (since 1.11.1, see below, the
// official icon.png is already 2:3 and this only applies to a 256 x 256 icon.png).
// Fix (ModeloGuardia.RegistrarIcono): a custom 2:3 sprite is put into the game's icon cache:
//   a) the «retrato.png» in the visual's folder if it exists and measures exactly 512 x 768;
//   b) otherwise, the icon.png fitted onto a transparent 2:3 canvas with this code: horizontally centered and pinned to the
//      TOP. Reason: all the game's masks show only the top part of the portrait (the Collection slot
//      the top 70%, the army on the heroes screen a 92 x 92 square over a 92 x 138 portrait, that is, the top two thirds; the 64 x 72 mini over 64 x 96, 75%). With the icon in the top two
//      thirds it is fully visible in all of them; vertically centered it would lose its bottom sixth in the 2/3 ones.
//      Pixel format: RGBA, 8 bits per channel, rows from BOTTOM to TOP (the order of Texture2D.GetPixels32).
// Game 1.11.1 (01-11-2026): the official icon.png is now 512 x 768 (UnitVisualOverrideLoader.IconWidth/IconHeight; the
// 256 x 256 one remains as LegacyIconSize, with a warning). With a 512 x 768 icon.png the game creates the 2:3 sprite itself, so
// the core does nothing (EsIcono2x3Oficial, reading only the PNG header with MedidasPng).
// While loading, _shownId is already the new one but _model is still the previous figure: wait.
// Author: ZeroitMods.

using System;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: medidas y encaje del icono en el lienzo 2:3
    // Section: icon dimensions and fitting it onto the 2:3 canvas
    // -----------------------------------------------------------------------
    public static class LogicaIconos
    {
        /// <summary>Nombre del retrato 2:3 opcional en la carpeta del visual de la unidad (junto a icon.png).</summary>
        /// Name of the optional 2:3 portrait in the unit's visual folder (next to icon.png).
        public const string NombreRetrato = "retrato.png";
        /// <summary>Medidas exactas del retrato opcional (las de los retratos de unidad de serie).</summary>
        /// Exact dimensions of the optional portrait (those of the stock unit portraits).
        public const int AnchoRetrato = 512, AltoRetrato = 768;
        /// <summary>
        /// Lado del icon.png CUADRADO heredado (256 x 256, UnitVisualOverrideLoader.LegacyIconSize en el juego 1.11.1). Desde
        /// la 1.11.1 el oficial es el de 512 x 768 (IconWidth x IconHeight); el de 256 se sigue aceptando (0.7.5: comentario
        /// al día, antes citaba un «IconSize» que ya no existe).
        /// Side of the legacy SQUARE icon.png (256 x 256, UnitVisualOverrideLoader.LegacyIconSize in game 1.11.1). Since
        /// 1.11.1 the official one is 512 x 768 (IconWidth x IconHeight); the 256 one is still accepted (0.7.5: comment
        /// updated, it used to cite an «IconSize» that no longer exists).
        /// </summary>
        public const int LadoIcono = 256;
        /// <summary>Tope de tamaño de fichero para leer un PNG (el juego pone 4 MB al icono; el retrato es mayor).</summary>
        /// File size cap for reading a PNG (the game sets 4 MB for the icon; the portrait is larger).
        public const long TopeIconoBytes = 4L * 1024 * 1024, TopeRetratoBytes = 8L * 1024 * 1024;

        /// <summary>¿Mide el retrato opcional exactamente 512 x 768?</summary>
        /// Is the optional portrait exactly 512 x 768?
        public static bool EsRetratoValido(int ancho, int alto) => ancho == AnchoRetrato && alto == AltoRetrato;

        /// <summary>¿Es un icon.png que acepta el juego? (512 x 768 desde la 1.11.1, o el heredado de 256 x 256)</summary>
        /// Is it an icon.png the game accepts? (512 x 768 since 1.11.1, or the legacy 256 x 256)
        public static bool EsIconoValido(int ancho, int alto) => (ancho == LadoIcono && alto == LadoIcono) || EsIcono2x3Oficial(ancho, alto);

        /// <summary>¿Es el icon.png 2:3 oficial del juego 1.11.1 (512 x 768)? Entonces el núcleo no crea sprite propio.</summary>
        /// Is it the official 2:3 icon.png of game 1.11.1 (512 x 768)? Then the core does not create its own sprite.
        public static bool EsIcono2x3Oficial(int ancho, int alto) => ancho == AnchoRetrato && alto == AltoRetrato;

        /// <summary>
        /// Medidas de un PNG a partir de su cabecera (firma de 8 bytes y bloque IHDR: ancho y alto en big-endian en los bytes
        /// 16-23). false si no es un PNG o la cabecera está corta. No lee el resto del fichero.
        /// Dimensions of a PNG from its header (8-byte signature and IHDR chunk: width and height in big-endian at bytes
        /// 16-23). false if it is not a PNG or the header is too short. Does not read the rest of the file.
        /// </summary>
        public static bool MedidasPng(byte[] cabecera, out int ancho, out int alto)
        {
            ancho = alto = 0;
            byte[] firma = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (cabecera == null || cabecera.Length < 24) return false;
            for (int i = 0; i < firma.Length; i++) if (cabecera[i] != firma[i]) return false;
            if (cabecera[12] != 'I' || cabecera[13] != 'H' || cabecera[14] != 'D' || cabecera[15] != 'R') return false;
            ancho = (cabecera[16] << 24) | (cabecera[17] << 16) | (cabecera[18] << 8) | cabecera[19];
            alto = (cabecera[20] << 24) | (cabecera[21] << 16) | (cabecera[22] << 8) | cabecera[23];
            return ancho > 0 && alto > 0;
        }

        /// <summary>¿Es un sprite cuadrado? (el que crea el juego con el icon.png; el nuestro es 2:3)</summary>
        /// Is it a square sprite? (the one the game creates with icon.png; ours is 2:3)
        public static bool EsCuadrado(float ancho, float alto) => Math.Abs(ancho - alto) < 0.5f;

        /// <summary>
        /// Medidas del lienzo 2:3 para una imagen de <paramref name="ancho"/> x <paramref name="alto"/>: el ancho más
        /// pequeño que la contiene a la vez a lo ancho y a lo alto, redondeado a múltiplo de 8 (así el alto, 3/2 del
        /// ancho, es entero y múltiplo de 4, lo que pide la compresión de la GPU). Para el icono de 256: 256 x 384.
        /// Dimensions of the 2:3 canvas for an image of <paramref name="ancho"/> x <paramref name="alto"/>: the smallest
        /// width that contains it both in width and height, rounded up to a multiple of 8 (so the height, 3/2 of the
        /// width, is an integer and a multiple of 4, as required by GPU compression). For the 256 icon: 256 x 384.
        /// </summary>
        public static void MedidasLienzo(int ancho, int alto, out int anchoLienzo, out int altoLienzo)
        {
            if (ancho <= 0 || alto <= 0) throw new ArgumentOutOfRangeException(nameof(ancho), "medidas no válidas");
            int minimo = Math.Max(ancho, (alto * 2 + 2) / 3);          // ancho tal que 3/2 del ancho >= alto
                                                                       // width such that 3/2 of the width >= height
            anchoLienzo = (minimo + 7) / 8 * 8;
            altoLienzo = anchoLienzo * 3 / 2;
        }

        /// <summary>
        /// Encaja una imagen RGBA (filas de abajo a arriba) en un lienzo 2:3 transparente: centrada en horizontal y pegada
        /// arriba (ver la cabecera). Devuelve los píxeles nuevos y sus medidas. No modifica la entrada.
        /// Fits an RGBA image (rows from bottom to top) onto a transparent 2:3 canvas: horizontally centered and pinned to the
        /// top (see the header). Returns the new pixels and their dimensions. Does not modify the input.
        /// </summary>
        public static byte[] Encajar(byte[] rgba, int ancho, int alto, out int anchoLienzo, out int altoLienzo)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            MedidasLienzo(ancho, alto, out anchoLienzo, out altoLienzo);
            if (rgba.Length != ancho * alto * 4) throw new ArgumentException($"se esperaban {ancho * alto * 4} bytes y hay {rgba.Length}", nameof(rgba));
            var lienzo = new byte[anchoLienzo * altoLienzo * 4];         // todo a 0: transparente
                                                                         // all 0: transparent
            int x0 = (anchoLienzo - ancho) / 2;                          // centrado en horizontal
                                                                         // horizontally centered
            int y0 = altoLienzo - alto;                                  // filas de abajo a arriba: las últimas son las de arriba
                                                                         // rows from bottom to top: the last ones are the top ones
            for (int y = 0; y < alto; y++)
                Buffer.BlockCopy(rgba, y * ancho * 4, lienzo, ((y0 + y) * anchoLienzo + x0) * 4, ancho * 4);
            return lienzo;
        }
    }
}
