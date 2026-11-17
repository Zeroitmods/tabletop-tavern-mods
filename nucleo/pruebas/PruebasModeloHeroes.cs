// PruebasModeloHeroes.cs
// Prueba 16 (F5): modelo 3D propio de los héroes nuevos (FormatoModeloHeroe.cs).
//   - Fichero ZHM1 sintético: se lee con los mismos valores, valida, y se rechazan la magia mala, la versión mala, el
//     fichero truncado o con bytes de más y los índices fuera de rango.
//   - Resolución de huesos por ruta contra una lista de rutas de instancia (encontrados y faltantes).
//   - Modelos reales (si se pasa la carpeta de módulos): los cuatro 9xx_modelo.bin se leen y validan, están ligados a su
//     héroe prestado (901->1, 902->2, 903->10, 904->12), tienen textura y TODAS sus rutas de hueso existen en el
//     esqueleto extraído del prefab prestado (modelo\fuentes\heroes_prestados\<Prefab>_esqueleto.json); y su estatua del
//     mapa (9xx_ficha.bin, malla estática) es válida y es la misma malla.
// Autor: ZeroitMods.
// PruebasModeloHeroes.cs
// Test 16 (F5): custom 3D model of the new heroes (FormatoModeloHeroe.cs).
//   - Synthetic ZHM1 file: it is read with the same values, validates, and bad magic, bad version, truncated
//     file or file with extra bytes and out-of-range indices are rejected.
//   - Bone resolution by path against a list of instance paths (found and missing).
//   - Real models (if the modules folder is passed): the four 9xx_modelo.bin are read and validated, are linked to their
//     borrowed hero (901->1, 902->2, 903->10, 904->12), have a texture and ALL their bone paths exist in the
//     skeleton extracted from the borrowed prefab (modelo\fuentes\heroes_prestados\<Prefab>_esqueleto.json); and their map
//     statue (9xx_ficha.bin, static mesh) is valid and is the same mesh.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ZeroNucleo;

internal static class PruebasModeloHeroes
{
    // Héroe nuevo -> (héroe prestado, prefab del prestado, módulo)
    // New hero -> (borrowed hero, borrowed prefab, module)
    private static readonly Dictionary<int, (int prestado, string prefab, string modulo)> Heroes = new Dictionary<int, (int, string, string)>
    {
        { 901, (1, "EdricValeward", "ZeroAstraMilitarum") }, { 902, (2, "RhydanGreythorne", "ZeroAstraMilitarum") },
        { 903, (10, "LordDravenBloodreaver", "ZeroImperioGalactico") }, { 904, (12, "TokugawaHarunobu", "ZeroImperioGalactico") },
    };

    // Escribe un ZHM1 mínimo (un triángulo con dos huesos) como lo haría formato_heroe.py
    // Writes a minimal ZHM1 (a triangle with two bones) the way formato_heroe.py would
    private static byte[] Sintetico(uint version = 1, string magia = "ZHM1", int extra = 0, int indiceMalo = -1)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8);
        void Texto(string s) { var b = Encoding.UTF8.GetBytes(s); bw.Write((ushort)b.Length); bw.Write(b); }
        bw.Write(Encoding.ASCII.GetBytes(magia));
        bw.Write(version); bw.Write(1); bw.Write(3u); bw.Write(3u); bw.Write(2u); bw.Write(0);
        Texto("Prueba");
        Texto("Root/Hips"); Texto("Root/Hips/Spine_01");
        for (int h = 0; h < 2; h++) for (int i = 0; i < 16; i++) bw.Write(i % 5 == 0 ? 1f : 0f);      // identidades
                                                                                                      // identities
        float[] pos = { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
        foreach (float f in pos) bw.Write(f);
        for (int v = 0; v < 3; v++) { bw.Write(0f); bw.Write(0f); bw.Write(1f); }                      // normales
                                                                                                       // normals
        for (int v = 0; v < 3; v++) { bw.Write(v * 0.5f); bw.Write(0.25f); }                         // uv
        for (int v = 0; v < 3; v++) { bw.Write((ushort)(v == 2 ? 1 : 0)); bw.Write((ushort)1); bw.Write((ushort)0); bw.Write((ushort)0); }
        for (int v = 0; v < 3; v++) { bw.Write(0.75f); bw.Write(0.25f); bw.Write(0f); bw.Write(0f); }
        bw.Write(0u); bw.Write(indiceMalo >= 0 ? (uint)indiceMalo : 2u); bw.Write(1u);
        for (int i = 0; i < extra; i++) bw.Write((byte)0);
        bw.Flush();
        return ms.ToArray();
    }

    private static bool Lanza(Func<object> f)
    {
        try { f(); return false; }
        catch (InvalidDataException) { return true; }
        catch (EndOfStreamException) { return true; }
    }

    public static void Todas(Action<bool, string> comprobar, string carpetaModulos)
    {
        // -------- Fichero sintético
        // -------- Synthetic file
        var m = FormatoModeloHeroe.Leer(Sintetico());
        comprobar(m.Prestado == 1 && m.Prefab == "Prueba" && m.Vertices == 3 && m.Huesos == 2 && m.Raiz == 0
                  && m.Rutas[1] == "Root/Hips/Spine_01" && m.Triangulos.SequenceEqual(new[] { 0, 2, 1 }),
            "modelo de héroe: el ZHM1 sintético se lee con sus datos (3 vértices, 2 huesos, triángulo 0-2-1)");
        comprobar(Math.Abs(m.Posiciones[3] - 1f) < 1e-6 && Math.Abs(m.Uv[2] - 0.5f) < 1e-6 && m.IndicesHueso[8] == 1 && Math.Abs(m.Pesos[0] - 0.75f) < 1e-6
                  && Math.Abs(m.Bindposes[0] - 1f) < 1e-6 && Math.Abs(m.Bindposes[5] - 1f) < 1e-6 && m.Validar().Count == 0,
            "modelo de héroe: posiciones, uv, índices de hueso, pesos y bindposes en su sitio; valida sin errores");
        comprobar(Lanza(() => FormatoModeloHeroe.Leer(Sintetico(magia: "ZHMX"))), "modelo de héroe: magia mala -> se rechaza");
        comprobar(Lanza(() => FormatoModeloHeroe.Leer(Sintetico(version: 2))), "modelo de héroe: versión 2 -> se rechaza");
        comprobar(Lanza(() => FormatoModeloHeroe.Leer(Sintetico(extra: 3))), "modelo de héroe: bytes de más al final -> se rechaza");
        var corto = Sintetico();
        comprobar(Lanza(() => FormatoModeloHeroe.Leer(corto.Take(corto.Length - 4).ToArray())), "modelo de héroe: fichero truncado -> se rechaza");
        comprobar(FormatoModeloHeroe.Leer(Sintetico(indiceMalo: 7)).Validar().Any(e => e.Contains("triángulo")),
            "modelo de héroe: índice de triángulo fuera de la malla -> lo detecta Validar");

        // -------- Resolución de huesos por ruta
        // -------- Bone resolution by path
        var idx = FormatoModeloHeroe.Resolver(m, new List<string> { "", "Root", "Root/Hips/Spine_01", "Root/Hips" }, out var faltan);
        comprobar(idx[0] == 3 && idx[1] == 2 && faltan.Count == 0, "modelo de héroe: huesos resueltos por ruta en la instancia");
        FormatoModeloHeroe.Resolver(m, new List<string> { "", "Root", "Root/Hips" }, out faltan);
        comprobar(faltan.Count == 1 && faltan[0] == "Root/Hips/Spine_01", "modelo de héroe: el hueso que no está en la instancia sale en «faltan»");

        // -------- Modelos reales de los módulos
        // -------- Real models from the modules
        if (carpetaModulos == null || !Directory.Exists(carpetaModulos)) return;
        string esqueletos = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "modelo", "fuentes", "heroes_prestados"));
        foreach (var (id, (prestado, prefab, modulo)) in Heroes.Select(k => (k.Key, k.Value)))
        {
            string ruta = Path.Combine(carpetaModulos, modulo, "heroes", $"{id}_modelo.bin");
            if (!File.Exists(ruta)) { comprobar(false, $"modelo de héroe {id}: falta {ruta}"); continue; }
            var real = FormatoModeloHeroe.Leer(ruta);
            var errores = real.Validar();
            comprobar(errores.Count == 0 && real.Prestado == prestado && File.Exists(Path.ChangeExtension(ruta, ".png")),
                $"modelo de héroe {id}: {real.Vertices} vértices, {real.Triangulos.Length / 3} triángulos y {real.Huesos} huesos de «{real.Prefab}», " +
                $"ligado al héroe {real.Prestado}, con textura{(errores.Count > 0 ? " -> " + string.Join("; ", errores) : "")}");
            // Estatua del mapa de campaña: misma malla (vértices y triángulos), un solo «hueso» y del mismo prestado
            // Campaign map statue: same mesh (vertices and triangles), a single «bone» and from the same borrowed hero
            string ficha = Path.Combine(carpetaModulos, modulo, "heroes", $"{id}_ficha.bin");
            if (File.Exists(ficha))
            {
                var est = FormatoModeloHeroe.Leer(ficha);
                comprobar(est.Validar().Count == 0 && est.Huesos == 1 && est.Prestado == prestado && est.Vertices == real.Vertices
                          && est.Triangulos.SequenceEqual(real.Triangulos),   // revisión del 18-10 (L-12): mismos índices, no solo el mismo número
                                                                              // review of 18-10 (L-12): same indices, not just the same count
                    $"modelo de héroe {id}: estatua del mapa «{est.Prefab}» válida ({est.Vertices} vértices, misma malla que el modelo)");
            }
            else comprobar(false, $"modelo de héroe {id}: falta la estatua del mapa {ficha}");
            string json = Path.Combine(esqueletos, prefab + "_esqueleto.json");
            if (!File.Exists(json)) { comprobar(false, $"modelo de héroe {id}: falta el esqueleto extraído {json}"); continue; }
            using var doc = JsonDocument.Parse(File.ReadAllText(json));
            var rutas = doc.RootElement.GetProperty("nodos").EnumerateArray().Select(n => n.GetProperty("ruta").GetString()).ToList();
            FormatoModeloHeroe.Resolver(real, rutas, out var sinHueso);
            bool unicas = real.Rutas.All(r => rutas.Count(x => x == r) == 1);
            comprobar(sinHueso.Count == 0 && unicas && real.Rutas[real.Raiz].EndsWith(id == 903 ? "pelvis" : "Hips"),
                $"modelo de héroe {id}: sus {real.Huesos} rutas de hueso existen (una sola vez) en el prefab {prefab} y la raíz es la cadera" +
                (sinHueso.Count > 0 ? $" (faltan {string.Join(", ", sinHueso.Take(3))})" : ""));
        }
    }
}
