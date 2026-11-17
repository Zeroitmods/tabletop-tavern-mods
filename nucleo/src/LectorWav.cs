// LectorWav.cs
// Lector mínimo de ficheros WAV para los sonidos propios de la facción A (F3).
// Sin tipos de Unity ni del juego: se compila también en pruebas\ para probarlo fuera del juego.
// Admite RIFF/WAVE con formato 1 (PCM), 16 bits, 1 o 2 canales (el estéreo se mezcla a mono) y salta los bloques
// que no conoce (LIST, fact, etc.). Cualquier otra cosa lanza InvalidDataException con el motivo.
// Validaciones (revisión del 17-10-2026): el tamaño RIFF de la cabecera debe caber en el fichero (si no, está
// truncado) y los bloques se recorren solo dentro de ese tamaño; un bloque que declara más bytes de los que hay se
// rechaza (no se recorta); el bloque data debe medir un múltiplo exacto de 2 × canales.
// LectorWav.cs
// Minimal WAV file reader for faction A's custom sounds (F3).
// No Unity or game types: it is also compiled in pruebas\ to test it outside the game.
// Supports RIFF/WAVE with format 1 (PCM), 16 bits, 1 or 2 channels (stereo is mixed down to mono) and skips chunks
// it doesn't know (LIST, fact, etc.). Anything else throws InvalidDataException with the reason.
// Validations (review of 17-10-2026): the RIFF size in the header must fit in the file (otherwise it is
// truncated) and chunks are traversed only within that size; a chunk that declares more bytes than there are is
// rejected (not trimmed); the data chunk must be an exact multiple of 2 × channels.

using System;
using System.IO;
using System.Text;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: lectura de WAV PCM de 16 bits a muestras float mono
    // Section: reading 16-bit PCM WAV into mono float samples
    // -----------------------------------------------------------------------
    public static class LectorWav
    {
        /// <summary>Convierte el contenido de un WAV PCM 16 bits en muestras mono de -1 a 1 y devuelve su frecuencia.</summary>
        /// Converts the contents of a 16-bit PCM WAV into mono samples from -1 to 1 and returns its sample rate.
        public static float[] LeerPcm16(byte[] b, out int frecuencia)
        {
            frecuencia = 0;
            if (b == null || b.Length < 44 || Ascii(b, 0) != "RIFF" || Ascii(b, 8) != "WAVE") throw new InvalidDataException("no es un WAV RIFF");
            // Tamaño RIFF: lo que sigue a los 8 primeros bytes. Debe caber en el fichero; si sobran bytes al final se ignoran
            // RIFF size: what follows the first 8 bytes. It must fit in the file; extra bytes at the end are ignored
            long tamRiff = BitConverter.ToUInt32(b, 4);
            if (tamRiff < 4) throw new InvalidDataException($"tamaño RIFF no válido ({tamRiff})");
            if (8 + tamRiff > b.Length) throw new InvalidDataException($"WAV truncado: la cabecera RIFF declara {8 + tamRiff} bytes y el fichero tiene {b.Length}");
            long limite = 8 + tamRiff;                               // los bloques se recorren solo hasta aquí
                                                                     // chunks are walked only up to here
            int canales = 0, bits = 0, formato = 0;
            bool hayFmt = false;
            long pos = 12;
            // Recorre los bloques RIFF: id (4 bytes) + tamaño (4 bytes) + datos
            // Walks the RIFF chunks: id (4 bytes) + size (4 bytes) + data
            while (pos + 8 <= limite)
            {
                string id = Ascii(b, (int)pos);
                long tam = BitConverter.ToUInt32(b, (int)pos + 4);
                int datos = (int)pos + 8;
                if (datos + tam > limite)
                    throw new InvalidDataException($"bloque '{id}' truncado: declara {tam} bytes y quedan {limite - datos} dentro del RIFF");
                if (id == "fmt ")
                {
                    if (tam < 16) throw new InvalidDataException("bloque fmt corto");
                    formato = BitConverter.ToUInt16(b, datos);
                    canales = BitConverter.ToUInt16(b, datos + 2);
                    frecuencia = BitConverter.ToInt32(b, datos + 4);
                    bits = BitConverter.ToUInt16(b, datos + 14);
                    hayFmt = true;
                }
                else if (id == "data")
                {
                    if (!hayFmt) throw new InvalidDataException("bloque data antes de fmt");
                    if (formato != 1 || bits != 16) throw new InvalidDataException($"solo PCM 16 bits (formato {formato}, {bits} bits)");
                    if (canales < 1 || canales > 2) throw new InvalidDataException($"canales no admitidos: {canales}");
                    if (frecuencia < 8000 || frecuencia > 96000) throw new InvalidDataException($"frecuencia no admitida: {frecuencia}");
                    int bytesMarco = 2 * canales;
                    if (tam % bytesMarco != 0) throw new InvalidDataException($"bloque data mal formado: {tam} bytes no es múltiplo de {bytesMarco}");
                    int marcos = (int)(tam / bytesMarco);
                    if (marcos == 0) throw new InvalidDataException("sin muestras");
                    var salida = new float[marcos];
                    for (int i = 0; i < marcos; i++)
                    {
                        int o = datos + i * 2 * canales;
                        float s = BitConverter.ToInt16(b, o) / 32768f;
                        if (canales == 2) s = (s + BitConverter.ToInt16(b, o + 2) / 32768f) * 0.5f;
                        salida[i] = s;
                    }
                    return salida;
                }
                pos = datos + tam + (tam & 1);                   // los bloques RIFF van alineados a 2 bytes
                                                                 // RIFF chunks are 2-byte aligned
            }
            if (pos < limite) throw new InvalidDataException($"WAV truncado: cabecera de bloque incompleta en el byte {pos} (RIFF hasta {limite})");
            throw new InvalidDataException("sin bloque data");
        }

        // Cuatro caracteres ASCII a partir de la posición o
        // Four ASCII characters starting at position o
        private static string Ascii(byte[] b, int o) => Encoding.ASCII.GetString(b, o, 4);
    }
}
