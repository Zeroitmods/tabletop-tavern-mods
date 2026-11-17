// AssetsGuardia.cs
// Entrada de assets (iconos, voces, sonidos, prefab de reclutamiento, raza) de cada unidad nueva en
// SquadAssetsDictionary: copia exacta de la de su unidad base, salvo la raza, que puede cambiar (sueltas de una
// facción clonadas de una unidad de otra).
// AssetsGuardia.cs
// Assets entry (icons, voices, sounds, recruitment prefab, race) for each new unit in
// SquadAssetsDictionary: an exact copy of that of its base unit, except the race, which may change (drops from a
// faction cloned from a unit of another).
//
// Problema encontrado (16-10-2026): SquadAssets es un struct con referencias y, a través del indexador genérico de
// interop (Dictionary<UnitName, SquadAssets>.set_Item), lo que se guardaba no era el struct sino basura (la cabecera
// del objeto empaquetado): la carta de reclutamiento se quedaba bocabajo (NullReferenceException en
// GetSquadTypeIcon) y, al leer esa entrada desde el plugin, el juego se cerraba.
// Solución: se llama a set_Item/get_Item del diccionario directamente en IL2CPP (il2cpp_runtime_invoke) pasando el
// struct desempaquetado, que es lo que espera el método. La comprobación se hace comparando los bytes del struct de
// la unidad nueva con los de su base (todos menos los 4 de la raza, que se comprueba aparte), sin crear envoltorios
// sobre datos que podrían estar rotos.
// La raza (enum Race) es el PRIMER campo de SquadAssets (Data/Core/TabletopTavernDataComponents.cs:7): su
// desplazamiento se lee del propio IL2CPP (il2cpp_field_get_offset) y se escribe como entero en la copia.
// Problem found (16-10-2026): SquadAssets is a struct with references and, through the generic interop indexer
// (Dictionary<UnitName, SquadAssets>.set_Item), what was stored was not the struct but garbage (the header
// of the boxed object): the recruitment card stayed face down (NullReferenceException in
// GetSquadTypeIcon) and, when reading that entry from the plugin, the game crashed.
// Solution: set_Item/get_Item of the dictionary are called directly in IL2CPP (il2cpp_runtime_invoke) passing the
// unboxed struct, which is what the method expects. The check is done by comparing the bytes of the new unit's struct with
// those of its base (all except the 4 of the race, which is checked separately), without creating wrappers
// over data that could be broken.
// The race (enum Race) is the FIRST field of SquadAssets (Data/Core/TabletopTavernDataComponents.cs:7): its
// offset is read from IL2CPP itself (il2cpp_field_get_offset) and written as an integer in the copy.

using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using TJ;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: copia y verificación de los assets de las unidades nuevas
    // Section: copy and verification of the assets of the new units
    // -----------------------------------------------------------------------
    public static unsafe class AssetsGuardia
    {
        // Unidades cuya entrada de assets está verificada (copia de la base con la raza correcta)
        // Units whose assets entry is verified (copy of the base with the correct race)
        private static readonly HashSet<int> _validas = new HashSet<int>();
        // Unidades de las que ya se escribió en el log la primera copia (para no repetir en cada recarga)
        // Units whose first copy has already been written to the log (to avoid repeating it on every reload)
        private static readonly HashSet<int> _informadas = new HashSet<int>();
        // Desplazamiento del campo «race» dentro del struct desempaquetado (-1 = aún sin calcular)
        // Offset of the "race" field within the unboxed struct (-1 = not yet computed)
        private static int _desplRaza = -1;

        /// <summary>true si la entrada de assets de la 900 es una copia exacta de la de Eisenmann (lo usa la autoprueba).</summary>
        /// true if the assets entry of 900 is an exact copy of that of Eisenmann (used by the self-test).
        public static bool AssetsValidos => _validas.Contains(UnidadGuardia.ID);
        public static bool Valida(UnitName u) => _validas.Contains((int)u);
        public static int NumeroValidas => _validas.Count;

        /// <summary>Garantiza que assets[id] existe y es copia de assets[base] con la raza del manifiesto.</summary>
        /// Ensures that assets[id] exists and is a copy of assets[base] with the race from the manifest.
        internal static void AsegurarAssets(Il2CppGeneric.Dictionary<UnitName, SquadAssets> assets, UnidadNueva u, string origen)
        {
            Plugin.TrazaUnaVez("AsegurarAssets");
            int id = u.Numero;
            try
            {
                if (assets == null || !assets.ContainsKey(u.Base)) { _validas.Remove(id); return; }
                if (assets.ContainsKey(u.Id) && IgualQueBase(assets, u)) { _validas.Add(id); return; }

                // get_Item(base) devuelve una copia empaquetada; se ajusta la raza y se guarda desempaquetada en el id nuevo
                // get_Item(base) returns a boxed copy; the race is adjusted and it is stored unboxed under the new id
                IntPtr cajaBase = Invocar(assets, "get_Item", 1, (int)u.Base, IntPtr.Zero);
                if (cajaBase == IntPtr.Zero) throw new InvalidOperationException($"get_Item({u.Base}) devolvió null");
                byte* datos = (byte*)IL2CPP.il2cpp_object_unbox(cajaBase);
                if (u.Raza.HasValue) *(int*)(datos + DesplazamientoRaza(cajaBase)) = (int)u.Raza.Value;
                Invocar(assets, "set_Item", 2, id, (IntPtr)datos);

                bool ok = IgualQueBase(assets, u);
                if (ok) _validas.Add(id); else _validas.Remove(id);
                if (!ok) Plugin.Error($"[{origen}] Los assets de la {id} no coinciden con los de {u.Base} tras copiarlos.");
                else if (_informadas.Add(id) && (id == UnidadGuardia.ID || (CfgTrazas())))
                    Plugin.Info($"[{origen}] Assets de la {id} copiados de {u.Base} (copia verificada): {Describir(LeerEntrada(assets, u.Id))}");
            }
            catch (Exception e)
            {
                _validas.Remove(id);
                Plugin.Error($"[{origen}] AsegurarAssets({id}): " + e);
            }
        }

        private static bool CfgTrazas() => Plugin.CfgTrazas != null && Plugin.CfgTrazas.Value;

        // Desplazamiento del campo «race» respecto al inicio de los datos desempaquetados (se calcula una vez)
        // Offset of the "race" field relative to the start of the unboxed data (computed once)
        private static int DesplazamientoRaza(IntPtr caja)
        {
            if (_desplRaza >= 0) return _desplRaza;
            IntPtr clase = IL2CPP.il2cpp_object_get_class(caja);
            IntPtr campo = IL2CPP.il2cpp_class_get_field_from_name(clase, "race");
            if (campo == IntPtr.Zero) throw new MissingFieldException("SquadAssets", "race");
            // il2cpp_field_get_offset cuenta desde el inicio del objeto empaquetado (con su cabecera)
            // il2cpp_field_get_offset counts from the start of the boxed object (including its header)
            int cabecera = (int)((long)IL2CPP.il2cpp_object_unbox(caja) - (long)caja);
            _desplRaza = (int)IL2CPP.il2cpp_field_get_offset(campo) - cabecera;
            if (_desplRaza < 0) throw new InvalidOperationException($"desplazamiento de race negativo ({_desplRaza})");
            return _desplRaza;
        }

        // Llama a un método del diccionario en IL2CPP con la clave (UnitName como int) y, si hace falta, un valor ya
        // desempaquetado. Devuelve el resultado empaquetado (o IntPtr.Zero si el método no devuelve nada).
        // Calls a dictionary method in IL2CPP with the key (UnitName as int) and, if needed, an already
        // unboxed value. Returns the boxed result (or IntPtr.Zero if the method returns nothing).
        private static IntPtr Invocar(Il2CppGeneric.Dictionary<UnitName, SquadAssets> dic, string metodo, int nArgs, int clave, IntPtr valorDesempaquetado)
        {
            IntPtr clase = IL2CPP.il2cpp_object_get_class(dic.Pointer);
            IntPtr m = IL2CPP.il2cpp_class_get_method_from_name(clase, metodo, nArgs);
            if (m == IntPtr.Zero) throw new MissingMethodException("Dictionary<UnitName,SquadAssets>", metodo);
            void** args = stackalloc void*[2];
            int k = clave;
            args[0] = &k;
            args[1] = (void*)valorDesempaquetado;
            IntPtr excepcion = IntPtr.Zero;
            IntPtr res = IL2CPP.il2cpp_runtime_invoke(m, dic.Pointer, args, ref excepcion);
            if (excepcion != IntPtr.Zero) throw new InvalidOperationException($"{metodo} lanzó una excepción dentro del juego");
            return res;
        }

        /// <summary>Lee una entrada del diccionario de assets por la vía segura (copia empaquetada de IL2CPP).</summary>
        /// Reads an entry from the assets dictionary the safe way (boxed IL2CPP copy).
        public static SquadAssets LeerEntrada(Il2CppGeneric.Dictionary<UnitName, SquadAssets> dic, UnitName clave)
        {
            if (dic == null || !dic.ContainsKey(clave)) return null;
            IntPtr caja = Invocar(dic, "get_Item", 1, (int)clave, IntPtr.Zero);
            return caja == IntPtr.Zero ? null : new SquadAssets(caja);
        }

        // Compara byte a byte el struct de la unidad nueva con el de su base, salvo la raza, que debe ser la del
        // manifiesto (o la de la base si el manifiesto no pone ninguna). Sin crear envoltorios sobre datos dudosos.
        // Compares byte by byte the new unit's struct with that of its base, except the race, which must be the one from the
        // manifest (or the base's if the manifest specifies none). Without creating wrappers over doubtful data.
        private static bool IgualQueBase(Il2CppGeneric.Dictionary<UnitName, SquadAssets> dic, UnidadNueva u)
        {
            IntPtr cajaN = Invocar(dic, "get_Item", 1, u.Numero, IntPtr.Zero);
            IntPtr cajaB = Invocar(dic, "get_Item", 1, (int)u.Base, IntPtr.Zero);
            if (cajaN == IntPtr.Zero || cajaB == IntPtr.Zero) return false;
            uint alin = 0;
            int tam = IL2CPP.il2cpp_class_value_size(IL2CPP.il2cpp_object_get_class(cajaB), ref alin);
            byte* n = (byte*)IL2CPP.il2cpp_object_unbox(cajaN);
            byte* b = (byte*)IL2CPP.il2cpp_object_unbox(cajaB);
            int dr = DesplazamientoRaza(cajaB);
            int razaEsperada = u.Raza.HasValue ? (int)u.Raza.Value : *(int*)(b + dr);
            if (*(int*)(n + dr) != razaEsperada) return false;
            for (int i = 0; i < tam; i++)
            {
                if (i >= dr && i < dr + sizeof(int)) continue;   // la raza ya se ha comprobado
                                                                 // the race has already been checked
                if (n[i] != b[i]) return false;
            }
            return tam > 0;
        }

        // Resumen legible de una entrada de assets (para el log; solo sobre entradas ya verificadas)
        // Readable summary of an assets entry (for the log; only for already verified entries)
        public static string Describir(SquadAssets a)
        {
            if (a == null) return "(sin entrada)";
            string N(UnityEngine.Object o) => o == null ? "null" : o.name;
            return $"raza {a.race}, icono {N(a.unitIcon)}, icono de tipo {(a.squadIcon == null ? "null" : "ok")}, " +
                   $"voces {N(a.voiceSFX)}, disparo {N(a.fireProjectileSFX)}, formación {N(a.formationDiscipline)}, " +
                   $"prefab {(a.unitRecruitmentPrefab == null ? "null" : a.unitRecruitmentPrefab.AssetGUID)}";
        }
    }
}
