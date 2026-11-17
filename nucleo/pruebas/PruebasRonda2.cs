// PruebasRonda2.cs
// Prueba 20 (18-10-2026, noche): arreglos de la segunda revisión de código del 18-10, fuera del juego:
//   - M-A: un registro bloqueado al arrancar NO se trata como corrupto. Se reintenta la lectura; si sigue bloqueado, la
//     sesión queda en solo lectura (no se guarda, el fichero bueno no se toca ni se aparta). Si el bloqueo se suelta a
//     mitad de los reintentos, carga normal. Solo un JSON roto se aparta como «.corrupto-<fecha>».
//   - B-9: un registro escrito por una versión más nueva conserva sus campos desconocidos al volver a guardarse.
//   - Parte B: encaje del icon.png de 256 x 256 en el lienzo 2:3 de 256 x 384 (LogicaIconos): medidas, icono arriba y
//     centrado, resto transparente, y validación de medidas del retrato.png (512 x 768) y del icono.
// Lo que depende de Unity (crear la textura y el sprite, meterlo en la caché del juego) solo se puede ver dentro de él.
// Autor: ZeroitMods.
// PruebasRonda2.cs
// Test 20 (18-10-2026, night): fixes from the second code review of 18-10, outside the game:
//   - M-A: a registry that is locked at startup is NOT treated as corrupt. The read is retried; if it is still locked, the
//     session becomes read-only (nothing is saved, the good file is neither touched nor set aside). If the lock is released
//     midway through the retries, it loads normally. Only a broken JSON is set aside as «.corrupto-<fecha>».
//   - B-9: a registry written by a newer version keeps its unknown fields when it is saved again.
//   - Part B: fitting the 256 x 256 icon.png into the 2:3 256 x 384 canvas (LogicaIconos): dimensions, icon at the top and
//     centered, the rest transparent, and validation of the dimensions of retrato.png (512 x 768) and of the icon.
// Whatever depends on Unity (creating the texture and the sprite, putting it in the game's cache) can only be seen inside the game.
// Author: ZeroitMods.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using ZeroNucleo;

internal static class PruebasRonda2
{
    // Carpeta de trabajo fija en el temporal del sistema (los ficheros se sobrescriben en cada pasada)
    // Fixed working folder in the system temp directory (files are overwritten on every pass)
    private static string Carpeta()
    {
        string c = Path.Combine(Path.GetTempPath(), "zgi_ronda2");
        Directory.CreateDirectory(c);
        foreach (string f in Directory.GetFiles(c)) File.Delete(f);   // solo ficheros de esta prueba
                                                                      // only files from this test
        return c;
    }

    public static void Todas(Action<bool, string> comprobar)
    {
        PruebaRegistroBloqueado(comprobar);
        PruebaCamposDesconocidos(comprobar);
        PruebaEncajeIconos(comprobar);
    }

    // Registro válido con una escuadra apuntada (lo que habría en disco de una sesión anterior)
    // Valid registry with one squad noted (what a previous session would have left on disk)
    private static string RegistroBueno(string carpeta)
    {
        string ruta = Path.Combine(carpeta, "registro.json");
        var r = new RegistroGuardias(ruta);
        r.ApuntarGuid("guid-bueno", 901);
        return ruta;
    }

    // ---------------------------------------------------------------
    // M-A: lectura bloqueada frente a JSON roto
    // M-A: locked read vs. broken JSON
    // ---------------------------------------------------------------
    private static void PruebaRegistroBloqueado(Action<bool, string> comprobar)
    {
        int reintentos = RegistroGuardias.ReintentosLectura, pausa = RegistroGuardias.PausaLecturaMs;
        try
        {
            string carpeta = Carpeta();
            string ruta = RegistroBueno(carpeta);
            byte[] original = File.ReadAllBytes(ruta);

            // 1) Bloqueado durante todos los reintentos: solo lectura, nada se escribe ni se aparta
            // 1) Locked during all the retries: read-only, nothing is written or set aside
            RegistroGuardias.ReintentosLectura = 3;
            RegistroGuardias.PausaLecturaMs = 20;
            string avisos = "";
            var bloqueado = new RegistroGuardias(ruta) { Avisar = a => avisos += a + "\n" };
            using (new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bloqueado.Cargar();
                bloqueado.ApuntarGuid("guid-nuevo", 902);   // intentaría guardar (pisaría el bueno con uno casi vacío)
                                                            // it would try to save (it would overwrite the good one with an almost empty one)
            }
            bool apartado = Directory.GetFiles(carpeta, "registro.json.corrupto-*").Length > 0;
            comprobar(bloqueado.SoloLectura && !apartado && File.ReadAllBytes(ruta).SequenceEqual(original),
                "M-A: registro bloqueado tras los reintentos -> solo lectura, el fichero bueno queda intacto y no se aparta como corrupto");
            comprobar(avisos.Contains("solo lectura"), "M-A: el bloqueo se avisa en el log (modo solo lectura)");
            var despues = new RegistroGuardias(ruta);
            despues.Cargar();
            comprobar(despues.IdDeGuid("guid-bueno") == 901 && despues.IdDeGuid("guid-nuevo") < 0 && !despues.SoloLectura,
                "M-A: en la sesión siguiente (fichero libre) el registro bueno carga entero y vuelve a guardarse con normalidad");

            // 2) El bloqueo se suelta a mitad de los reintentos: carga normal, sin solo lectura
            // 2) The lock is released midway through the retries: normal load, not read-only
            RegistroGuardias.ReintentosLectura = 20;
            RegistroGuardias.PausaLecturaMs = 25;
            var bloqueo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.None);
            var soltar = new Thread(() => { Thread.Sleep(120); bloqueo.Dispose(); });
            soltar.Start();
            var reintentado = new RegistroGuardias(ruta);
            reintentado.Cargar();
            soltar.Join();
            comprobar(!reintentado.SoloLectura && reintentado.IdDeGuid("guid-bueno") == 901,
                "M-A: si el fichero se libera durante los reintentos, el registro carga normal (sin solo lectura)");

            // 3) JSON realmente roto: se aparta como corrupto, se empieza vacío y SÍ se guarda
            // 3) Truly broken JSON: it is set aside as corrupt, it starts empty and it DOES save
            RegistroGuardias.ReintentosLectura = 3;
            RegistroGuardias.PausaLecturaMs = 20;
            File.WriteAllText(ruta, "{ esto no es JSON");
            var roto = new RegistroGuardias(ruta);
            roto.Cargar();
            bool copia = Directory.GetFiles(carpeta, "registro.json.corrupto-*").Length > 0;
            roto.ApuntarGuid("guid-tras-corrupto", 903);
            var relectura = new RegistroGuardias(ruta);
            relectura.Cargar();
            comprobar(copia && !roto.SoloLectura && relectura.IdDeGuid("guid-tras-corrupto") == 903,
                "M-A: un JSON roto sí se aparta como «.corrupto-…», se empieza vacío y se sigue guardando");
        }
        finally
        {
            RegistroGuardias.ReintentosLectura = reintentos;
            RegistroGuardias.PausaLecturaMs = pausa;
        }
    }

    // ---------------------------------------------------------------
    // B-9: campos de una versión más nueva
    // B-9: fields from a newer version
    // ---------------------------------------------------------------
    private static void PruebaCamposDesconocidos(Action<bool, string> comprobar)
    {
        string carpeta = Carpeta();
        string ruta = RegistroBueno(carpeta);
        // Se añade a mano un campo que esta versión no conoce, como lo escribiría una versión futura
        // A field this version does not know is added by hand, as a future version would write it
        string texto = File.ReadAllText(ruta);
        int llave = texto.LastIndexOf('}');
        texto = texto.Substring(0, llave).TrimEnd() + ",\n  \"CampoDelFuturo\": { \"a\": [1, 2, 3], \"b\": \"x\" }\n}";
        File.WriteAllText(ruta, texto);
        var r = new RegistroGuardias(ruta);
        r.Cargar();
        r.ApuntarGuid("guid-otro", 904);                                   // obliga a guardar
                                                                           // forces a save
        using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
        bool sigue = doc.RootElement.TryGetProperty("CampoDelFuturo", out var campo) &&
                     campo.TryGetProperty("a", out var a) && a.GetArrayLength() == 3 && campo.GetProperty("b").GetString() == "x";
        var otra = new RegistroGuardias(ruta);
        otra.Cargar();
        comprobar(sigue && otra.IdDeGuid("guid-otro") == 904 && otra.IdDeGuid("guid-bueno") == 901,
            "B-9: un campo desconocido (registro de una versión más nueva) se conserva al volver a guardar");
    }

    // ---------------------------------------------------------------
    // Parte B: encaje del icono en el lienzo 2:3
    // Part B: fitting the icon into the 2:3 canvas
    // ---------------------------------------------------------------
    private static void PruebaEncajeIconos(Action<bool, string> comprobar)
    {
        // Medidas
        // Dimensions
        LogicaIconos.MedidasLienzo(256, 256, out int w, out int h);
        comprobar(w == 256 && h == 384, $"iconos 2:3: el icono de 256 x 256 va en un lienzo de 256 x 384 (sale {w} x {h})");
        LogicaIconos.MedidasLienzo(256, 384, out int w2, out int h2);
        LogicaIconos.MedidasLienzo(300, 100, out int w3, out int h3);
        comprobar(w2 == 256 && h2 == 384 && w3 == 304 && h3 == 456 && h3 * 2 == w3 * 3,
            "iconos 2:3: una imagen ya 2:3 no crece y una apaisada cabe a lo ancho (múltiplos de 8, proporción exacta)");

        // Icono sintético: cada píxel lleva su posición (x, y) y alfa 255; la fila y = 255 es la de ARRIBA
        // Synthetic icon: each pixel carries its position (x, y) and alpha 255; row y = 255 is the TOP one
        const int L = 256;
        var icono = new byte[L * L * 4];
        for (int y = 0; y < L; y++)
            for (int x = 0; x < L; x++)
            {
                int i = (y * L + x) * 4;
                icono[i] = (byte)x; icono[i + 1] = (byte)y; icono[i + 2] = 7; icono[i + 3] = 255;
            }
        var copiaEntrada = (byte[])icono.Clone();
        byte[] lienzo = LogicaIconos.Encajar(icono, L, L, out int wl, out int hl);
        comprobar(lienzo.Length == wl * hl * 4 && wl == 256 && hl == 384 && icono.SequenceEqual(copiaEntrada),
            "iconos 2:3: el lienzo tiene el tamaño justo y la entrada no se modifica");

        // Filas 0-127 (abajo, el tercio que tapan las máscaras) transparentes; 128-383 = el icono tal cual
        // Rows 0-127 (bottom, the third covered by the masks) transparent; 128-383 = the icon as is
        bool abajoVacio = true;
        for (int i = 0; i < 128 * wl * 4 && abajoVacio; i++) abajoVacio = lienzo[i] == 0;
        bool iconoArriba = true;
        for (int y = 0; y < L && iconoArriba; y++)
            for (int x = 0; x < L && iconoArriba; x++)
            {
                int d = ((y + 128) * wl + x) * 4;
                iconoArriba = lienzo[d] == (byte)x && lienzo[d + 1] == (byte)y && lienzo[d + 2] == 7 && lienzo[d + 3] == 255;
            }
        comprobar(abajoVacio && iconoArriba,
            "iconos 2:3: el icono queda entero en los dos tercios de ARRIBA (filas 128-383) y el tercio de abajo es transparente");
        // Esquina de arriba a la izquierda del icono (x 0, y 255) en la última fila del lienzo
        // Top-left corner of the icon (x 0, y 255) in the last row of the canvas
        int esquina = ((hl - 1) * wl) * 4;
        comprobar(lienzo[esquina] == 0 && lienzo[esquina + 1] == 255 && lienzo[esquina + 3] == 255,
            "iconos 2:3: la fila de arriba del icono es la de arriba del lienzo (sin voltear)");

        // Centrado en horizontal con una imagen más estrecha que el lienzo (100 x 300 -> lienzo 200 x 300)
        // Horizontally centered with an image narrower than the canvas (100 x 300 -> 200 x 300 canvas)
        var estrecha = Enumerable.Repeat((byte)200, 100 * 300 * 4).ToArray();
        byte[] l2 = LogicaIconos.Encajar(estrecha, 100, 300, out int we, out int he);
        int filaMedia = 150 * we * 4;
        bool centrada = we == 200 && he == 300 && l2[filaMedia + 49 * 4 + 3] == 0 && l2[filaMedia + 50 * 4 + 3] == 200
                        && l2[filaMedia + 149 * 4 + 3] == 200 && l2[filaMedia + 150 * 4 + 3] == 0;
        comprobar(centrada, "iconos 2:3: una imagen más estrecha que el lienzo queda centrada en horizontal (márgenes de 50 px)");

        // Validaciones
        // Validations
        bool lanza = false;
        try { LogicaIconos.Encajar(new byte[10], L, L, out _, out _); } catch (ArgumentException) { lanza = true; }
        comprobar(lanza, "iconos 2:3: un búfer que no casa con las medidas se rechaza");
        comprobar(LogicaIconos.EsRetratoValido(512, 768) && !LogicaIconos.EsRetratoValido(256, 384) && !LogicaIconos.EsRetratoValido(768, 512)
                  && LogicaIconos.EsIconoValido(256, 256) && LogicaIconos.EsIconoValido(512, 768) && !LogicaIconos.EsIconoValido(512, 512),
            "iconos 2:3: el retrato.png solo vale de 512 x 768 y el icon.png de 256 x 256 o de 512 x 768 (juego 1.11.1)");
        comprobar(LogicaIconos.EsCuadrado(256, 256) && !LogicaIconos.EsCuadrado(256, 384),
            "iconos 2:3: se distingue el sprite cuadrado del juego del nuestro 2:3");
    }
}
