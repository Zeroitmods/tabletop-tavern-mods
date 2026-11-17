// FormatoModeloHeroe.cs
// Lector del formato «ZHM1» del modelo 3D propio de un héroe nuevo (F5): malla con pesos ligada al esqueleto del prefab
// del héroe de serie prestado. Lo escribe modelo\ligar_heroe.py (especificación en modelo\formato_heroe.py, mismo orden
// de campos) y lo usa ModeloHeroes.cs para construir el UnityEngine.Mesh. Código puro (sin tipos del juego ni de Unity):
// lo comprueban las pruebas fuera del juego (pruebas\PruebasModeloHeroes.cs).
// Autor: ZeroitMods.
// FormatoModeloHeroe.cs
// Reader for the «ZHM1» format of the custom 3D model of a new hero (F5): weighted mesh bound to the skeleton of the borrowed
// stock hero's prefab. It is written by modelo\ligar_heroe.py (specification in modelo\formato_heroe.py, same field
// order) and used by ModeloHeroes.cs to build the UnityEngine.Mesh. Pure code (no game or Unity types):
// it is checked by the out-of-game tests (pruebas\PruebasModeloHeroes.cs).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: datos leídos de un fichero ZHM1 (ejes de Unity)
    // Section: data read from a ZHM1 file (Unity axes)
    // -----------------------------------------------------------------------
    public sealed class DatosModeloHeroe
    {
        public int Prestado;                 // héroe de serie cuyo prefab da el esqueleto
                                             // stock hero whose prefab provides the skeleton
        public string Prefab;                // nombre del prefab de origen (informativo)
                                             // name of the source prefab (informational)
        public int Raiz;                     // índice del hueso raíz del renderer (la cadera)
                                             // index of the renderer's root bone (the hips)
        public string[] Rutas;               // ruta de cada hueso relativa a la raíz de la instancia («Root/Hips/...»)
                                             // path of each bone relative to the instance root («Root/Hips/...»)
        public float[] Bindposes;            // huesos x 16, Matrix4x4 por filas
                                             // bones x 16, row-major Matrix4x4
        public float[] Posiciones;           // vértices x 3
                                             // vertices x 3
        public float[] Normales;             // vértices x 3
                                             // vertices x 3
        public float[] Uv;                   // vértices x 2
                                             // vertices x 2
        public ushort[] IndicesHueso;        // vértices x 4
                                             // vertices x 4
        public float[] Pesos;                // vértices x 4
                                             // vertices x 4
        public int[] Triangulos;             // índices (triángulos x 3), orden de Unity
                                             // indices (triangles x 3), Unity order

        public int Vertices => Posiciones == null ? 0 : Posiciones.Length / 3;
        public int Huesos => Rutas == null ? 0 : Rutas.Length;

        /// <summary>Lista de problemas del modelo (vacía si está bien): rangos, pesos, índices y matrices.</summary>
        /// List of model problems (empty if it is fine): ranges, weights, indices and matrices.
        public List<string> Validar()
        {
            var err = new List<string>();
            int n = Vertices, h = Huesos;
            // Tamaños de todos los bloques antes de recorrerlos (un objeto montado a mano podría no cuadrar)
            // Sizes of all the blocks before iterating over them (a hand-built object might not add up)
            if (Posiciones == null || Posiciones.Length % 3 != 0 || Normales?.Length != n * 3 || Uv?.Length != n * 2
                || IndicesHueso?.Length != n * 4 || Pesos?.Length != n * 4)
            {
                err.Add("bloques de vértices con tamaños que no cuadran");
                return err;
            }
            if (n == 0 || Triangulos == null || Triangulos.Length == 0 || Triangulos.Length % 3 != 0)
                err.Add($"malla vacía o triángulos incompletos ({n} vértices, {Triangulos?.Length ?? 0} índices)");
            if (h == 0 || Raiz < 0 || Raiz >= h) err.Add($"hueso raíz {Raiz} fuera de la lista de {h} huesos");
            if (Rutas != null && new HashSet<string>(Rutas).Count != h) err.Add("rutas de hueso repetidas");
            if (Rutas != null) foreach (var r in Rutas) if (string.IsNullOrEmpty(r)) { err.Add("ruta de hueso vacía"); break; }
            if (Triangulos != null) foreach (int t in Triangulos) if (t < 0 || t >= n) { err.Add("índice de triángulo fuera de la malla"); break; }
            if (IndicesHueso != null) foreach (ushort b in IndicesHueso) if (b >= h) { err.Add("índice de hueso fuera de la lista"); break; }
            for (int v = 0; Pesos != null && v < n; v++)
            {
                float s = Pesos[v * 4] + Pesos[v * 4 + 1] + Pesos[v * 4 + 2] + Pesos[v * 4 + 3];
                bool negativo = Pesos[v * 4] < 0 || Pesos[v * 4 + 1] < 0 || Pesos[v * 4 + 2] < 0 || Pesos[v * 4 + 3] < 0;
                if (Math.Abs(s - 1f) > 1e-3f || negativo) { err.Add($"pesos mal normalizados en el vértice {v}"); break; }
            }
            if (!Finitos(Posiciones) || !Finitos(Normales) || !Finitos(Uv) || !Finitos(Bindposes)) err.Add("valores no finitos");
            if (Bindposes == null || Bindposes.Length != h * 16) err.Add("bindposes con tamaño distinto del número de huesos");
            return err;
        }

        private static bool Finitos(float[] a)
        {
            if (a == null) return false;
            foreach (float x in a) if (float.IsNaN(x) || float.IsInfinity(x)) return false;
            return true;
        }
    }

    // -----------------------------------------------------------------------
    // Sección: lectura
    // Section: reading
    // -----------------------------------------------------------------------
    public static class FormatoModeloHeroe
    {
        public const string Magia = "ZHM1";
        public const uint Version = 1;

        /// <summary>Lee un fichero ZHM1. Lanza InvalidDataException si no tiene el formato esperado.</summary>
        /// Reads a ZHM1 file. Throws InvalidDataException if it does not have the expected format.
        public static DatosModeloHeroe Leer(string ruta) => Leer(File.ReadAllBytes(ruta), ruta);

        /// <summary>Lee un ZHM1 de memoria (el nombre solo sirve para los mensajes de error).</summary>
        /// Reads a ZHM1 from memory (the name is only used for error messages).
        public static DatosModeloHeroe Leer(byte[] datos, string nombre = "modelo")
        {
            using var ms = new MemoryStream(datos, false);
            using var br = new BinaryReader(ms, Encoding.UTF8);
            if (datos.Length < 28 || Encoding.ASCII.GetString(br.ReadBytes(4)) != Magia)
                throw new InvalidDataException($"{nombre}: no es un modelo de héroe ZHM1");
            uint version = br.ReadUInt32();
            if (version != Version) throw new InvalidDataException($"{nombre}: versión {version} no soportada");
            var m = new DatosModeloHeroe { Prestado = br.ReadInt32() };
            int n = Contar(br.ReadUInt32(), 4_000_000, "vértices", nombre);
            int k = Contar(br.ReadUInt32(), 24_000_000, "índices", nombre);
            int h = Contar(br.ReadUInt32(), 4096, "huesos", nombre);
            m.Raiz = br.ReadInt32();
            m.Prefab = Texto(br);
            m.Rutas = new string[h];
            for (int i = 0; i < h; i++) m.Rutas[i] = Texto(br);
            // Tamaño exacto que queda: así un fichero truncado o con basura al final no se acepta
            // Exact remaining size: this way a truncated file or one with trailing garbage is not accepted
            long esperado = (long)h * 16 * 4 + (long)n * (3 + 3 + 2) * 4 + (long)n * 4 * 2 + (long)n * 4 * 4 + (long)k * 4;
            if (ms.Length - ms.Position != esperado)
                throw new InvalidDataException($"{nombre}: tamaño inesperado ({ms.Length - ms.Position} bytes de datos, se esperaban {esperado})");
            m.Bindposes = Floats(br, h * 16);
            m.Posiciones = Floats(br, n * 3);
            m.Normales = Floats(br, n * 3);
            m.Uv = Floats(br, n * 2);
            m.IndicesHueso = new ushort[n * 4];
            Buffer.BlockCopy(br.ReadBytes(n * 4 * 2), 0, m.IndicesHueso, 0, n * 4 * 2);
            m.Pesos = Floats(br, n * 4);
            m.Triangulos = new int[k];
            Buffer.BlockCopy(br.ReadBytes(k * 4), 0, m.Triangulos, 0, k * 4);     // u32 del fichero (< vértices) como int
                                                                                  // u32 from the file (< vertices) as int
            return m;
        }

        // Número de elementos con un tope razonable (un fichero corrupto no reserva memoria sin fin)
        // Number of elements with a reasonable cap (a corrupt file does not reserve endless memory)
        private static int Contar(uint v, int tope, string que, string nombre)
        {
            if (v > tope) throw new InvalidDataException($"{nombre}: demasiados {que} ({v})");
            return (int)v;
        }

        // Texto con longitud u16 + UTF-8
        // Text with u16 length + UTF-8
        private static string Texto(BinaryReader br)
        {
            int n = br.ReadUInt16();
            if (n > br.BaseStream.Length - br.BaseStream.Position) throw new InvalidDataException("texto más largo que el fichero");
            return Encoding.UTF8.GetString(br.ReadBytes(n));
        }

        // Bloque de floats little endian
        // Block of little-endian floats
        private static float[] Floats(BinaryReader br, int n)
        {
            var a = new float[n];
            Buffer.BlockCopy(br.ReadBytes(n * 4), 0, a, 0, n * 4);
            return a;
        }

        /// <summary>
        /// Resuelve las rutas de hueso contra las rutas que tiene una instancia (relativas a su raíz). Devuelve el índice de
        /// cada hueso en <paramref name="rutasInstancia"/> (-1 si falta) y la lista de las que faltan.
        /// Resolves the bone paths against the paths an instance has (relative to its root). Returns the index of
        /// each bone in <paramref name="rutasInstancia"/> (-1 if missing) and the list of the missing ones.
        /// </summary>
        public static int[] Resolver(DatosModeloHeroe m, IList<string> rutasInstancia, out List<string> faltan)
        {
            var indice = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < rutasInstancia.Count; i++)
                if (rutasInstancia[i] != null && !indice.ContainsKey(rutasInstancia[i])) indice[rutasInstancia[i]] = i;
            faltan = new List<string>();
            var res = new int[m.Huesos];
            for (int b = 0; b < m.Huesos; b++)
            {
                if (indice.TryGetValue(m.Rutas[b], out int i)) res[b] = i;
                else { res[b] = -1; faltan.Add(m.Rutas[b]); }
            }
            return res;
        }
    }
}
