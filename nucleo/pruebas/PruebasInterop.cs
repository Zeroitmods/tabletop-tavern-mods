// PruebasInterop.cs
// Prueba 28 (03-11-2026, crash al arrancar con la autoprueba; notas internas (no publicadas)): ningún método del juego con un parámetro
// ref/out de un struct NO blittable (en el interop, un tipo que hereda de Il2CppSystem.ValueType, como TJ.Hero) se llama por
// su envoltorio generado. Ese envoltorio pasa al juego la dirección de un IntPtr local de 8 bytes y el juego escribe ahí el
// struct entero: desborda la pila y el proceso cae después en cualquier sitio (03-11: HashSet<int>.Contains de la autoprueba).
// Se mira la DLL compilada (bin\Release\net6.0\ZeroNucleo.dll) con System.Reflection.Metadata, sin cargarla:
//   - la DLL está al día (no es más vieja que ningún .cs de src\ ni que el csproj; si no, la prueba miraría otra cosa);
//   - el detector reconoce el caso del crash (TJ.Spells.SpellLoadout.TryGetSignatureHero(Spell, out Hero)) en el interop, y
//     el campo del interop que usa AutoPrueba.DuenoDelHechizo existe con ese nombre exacto;
//   - la sustitución de parámetros genéricos funciona (caso sintético: Dictionary<int, Hero>.TryGetValue(!0, out !1));
//   - control positivo de la rama que vigila el núcleo: sus referencias a TJ.Hero se nombran igual que en el interop y,
//     con la DLL del crash guardada en una carpeta de copias del autor que no se publica, el detector da justo la llamada
//     de entonces;
//   - ninguna referencia a método de la DLL apunta a un método así (los que hagan falta se llaman a mano con una caja del
//     tamaño real, como AutoPrueba.DuenoDelHechizo). Con métodos de tipos genéricos instanciados (p. ej.
//     Dictionary<int, Hero>.TryGetValue) se sustituyen los !i por los argumentos del tipo, y en los métodos genéricos
//     (MethodSpecification) los !!i por los del método; un struct genérico se compara por su nombre sin argumentos.
// Solo cuentan los métodos de tipos definidos en el interop (los del BCL gestionado no van a código nativo).
// Alcance: solo llamadas directas desde ZeroNucleo.dll (no los módulos, ni reflexión, ni parches de Harmony: el 03-11 se
// comprobó a mano que ningún parche del núcleo toca un método con ref/out de struct no blittable). Necesita el juego instalado:
// sin la DLL compilada o sin el interop, es un fallo.
// Autor: ZeroitMods.
// PruebasInterop.cs
// Test 28 (03-11-2026, crash on startup with the self-test; internal notes (unpublished)): no game method with a
// ref/out parameter of a NON-blittable struct (in the interop, a type that inherits from Il2CppSystem.ValueType, like TJ.Hero) is called through
// its generated wrapper. That wrapper passes the game the address of an 8-byte local IntPtr and the game writes the
// whole struct there: it overflows the stack and the process later crashes anywhere (03-11: HashSet<int>.Contains in the self-test).
// The compiled DLL (bin\Release\net6.0\ZeroNucleo.dll) is inspected with System.Reflection.Metadata, without loading it:
//   - the DLL is up to date (not older than any .cs in src\ or the csproj; otherwise the test would look at something else);
//   - the detector recognizes the crash case (TJ.Spells.SpellLoadout.TryGetSignatureHero(Spell, out Hero)) in the interop, and
//     the interop field used by AutoPrueba.DuenoDelHechizo exists with that exact name;
//   - generic parameter substitution works (synthetic case: Dictionary<int, Hero>.TryGetValue(!0, out !1));
//   - positive control of the branch the core guards: its references to TJ.Hero are named the same as in the interop and,
//     with the crash DLL kept in an unpublished folder of the author's copies, the detector yields exactly the call
//     from back then;
//   - no method reference in the DLL points to such a method (the ones needed are called by hand with a box of the
//     real size, like AutoPrueba.DuenoDelHechizo). With methods of instantiated generic types (e.g.
//     Dictionary<int, Hero>.TryGetValue) the !i are replaced by the type's arguments, and in generic methods
//     (MethodSpecification) the !!i by the method's; a generic struct is compared by its name without arguments.
// Only methods of types defined in the interop count (those of the managed BCL don't go to native code).
// Scope: only direct calls from ZeroNucleo.dll (not the modules, nor reflection, nor Harmony patches: on 03-11 it was
// checked by hand that no core patch touches a method with a ref/out of a non-blittable struct). Requires the game installed:
// without the compiled DLL or without the interop, it is a failure.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

internal static class PruebasInterop
{
    // Campo del envoltorio del interop que lee AutoPrueba.DuenoDelHechizo (src\AutoPruebaNuevas.cs): mismo texto exacto
    // Interop wrapper field read by AutoPrueba.DuenoDelHechizo (src\AutoPruebaNuevas.cs): exact same text
    private const string CampoTryGetSignatureHero = "NativeMethodInfoPtr_TryGetSignatureHero_Public_Static_Boolean_Spell_byref_Hero_0";

    // -----------------------------------------------------------------------
    // Sección: entrada de la prueba
    // Section: test input
    // -----------------------------------------------------------------------
    public static void Todas(Action<bool, string> comprobar)
    {
        Console.WriteLine("--- Prueba 28: ref/out de structs no blittables del juego por el envoltorio del interop ---");
        try { Ejecutar(comprobar); }
        catch (Exception e) { comprobar(false, "prueba 28 sin errores al leer los metadatos: " + e); }   // una firma rara no tumba el resto
                                                                                                         // one odd signature does not bring down the rest
    }

    private static void Ejecutar(Action<bool, string> comprobar)
    {
        string raiz = RaizProyecto();
        string dll = raiz == null ? null : Path.Combine(raiz, "bin", "Release", "net6.0", "ZeroNucleo.dll");
        if (dll == null || !File.Exists(dll))
        {
            comprobar(false, $"DLL compilada del núcleo encontrada ({dll ?? "sin ZeroNucleo.csproj por encima"}; compila en Release antes de las pruebas)");
            return;
        }
        string interop = CarpetaInterop(Path.Combine(raiz, "ZeroNucleo.csproj"));
        if (interop == null || !Directory.Exists(interop))
        {
            comprobar(false, $"interop del juego encontrado ({interop ?? "sin <GameDir> en el csproj"})");
            return;
        }

        // La DLL tiene que ser la de los fuentes actuales (todo src\ y el csproj)
        // The DLL must be built from the current sources (all of src\ and the csproj)
        var fuentes = Directory.GetFiles(Path.Combine(raiz, "src"), "*.cs", SearchOption.AllDirectories).Append(Path.Combine(raiz, "ZeroNucleo.csproj"));
        var masNuevo = fuentes.Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();
        comprobar(File.GetLastWriteTimeUtc(dll) >= masNuevo,
                  $"la DLL compilada está al día con src\\ y el csproj ({File.GetLastWriteTime(dll):dd-MM HH:mm} frente a {masNuevo.ToLocalTime():dd-MM HH:mm}; si no, recompila en Release)");

        // Structs no blittables del interop (nombre completo, con '/' para los anidados y `n en los genéricos) y todos los
        // tipos que define el interop (solo sus métodos van a código nativo: un Dictionary gestionado de .NET no cuenta)
        // Non-blittable structs of the interop (full name, with '/' for nested ones and `n for generics) and all the
        // types defined by the interop (only their methods go to native code: a managed .NET Dictionary does not count)
        var noBlittables = new HashSet<string>(StringComparer.Ordinal);
        var tiposInterop = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in Directory.GetFiles(interop, "*.dll")) StructsNoBlittables(f, noBlittables, tiposInterop);
        comprobar(noBlittables.Contains("TJ.Hero"), $"interop: TJ.Hero se reconoce como struct no blittable ({noBlittables.Count} en total)");

        // El caso del crash tiene que salir con el detector (si no, el detector no vale) y el campo que lee el núcleo existe
        // The crash case must come out of the detector (otherwise the detector is worthless) and the field read by the core exists
        string core = Path.Combine(interop, "TabletopTavern.Core.dll");
        var delJuego = File.Exists(core) ? DefinicionesConByrefNoBlittable(core, noBlittables) : new List<string>();
        comprobar(delJuego.Any(m => m.Contains("TJ.Spells.SpellLoadout::TryGetSignatureHero")),
                  $"interop: el detector marca SpellLoadout.TryGetSignatureHero (out Hero) entre {delJuego.Count} métodos del juego con ref/out de struct no blittable");
        comprobar(File.Exists(core) && TieneCampo(core, "TJ.Spells.SpellLoadout", CampoTryGetSignatureHero),
                  $"interop: SpellLoadout tiene el campo {CampoTryGetSignatureHero} que usa la autoprueba");

        // Sustitución genérica (caso sintético): (!0, !1&) con el tipo Dictionary<System.Int32, TJ.Hero> da TJ.Hero&
        // Generic substitution (synthetic case): (!0, !1&) with the type Dictionary<System.Int32, TJ.Hero> gives TJ.Hero&
        var contexto = new Contexto(ImmutableArray.Create("System.Int32", "TJ.Hero"), ImmutableArray<string>.Empty);
        var p = new Nombres();
        string sustituido = p.GetByReferenceType(p.GetGenericTypeParameter(contexto, 1));
        string sustituidoMetodo = p.GetByReferenceType(p.GetGenericMethodParameter(new Contexto(ImmutableArray<string>.Empty, ImmutableArray.Create("TJ.Hero")), 0));
        comprobar(sustituido == "TJ.Hero&" && EsByrefNoBlittable(sustituido, noBlittables) && sustituidoMetodo == "TJ.Hero&"
                  && EsByrefNoBlittable("Il2CppSystem.Collections.Generic.KeyValuePair`2<System.Int32,TJ.Hero>&", noBlittables) == noBlittables.Contains("Il2CppSystem.Collections.Generic.KeyValuePair`2"),
                  $"detector: los parámetros genéricos se sustituyen (!1 de Dictionary<int, Hero> -> {sustituido}; !!0 de un método <Hero> -> {sustituidoMetodo})");

        // La DLL del núcleo no llama a ninguno por su envoltorio
        // The core DLL calls none of them through its wrapper
        var llamadas = ReferenciasConByrefNoBlittable(dll, noBlittables, tiposInterop, out var padres, out var devuelven);
        devuelven = devuelven.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
        // 0.7.7 (arreglo del 03-11): structs no blittables DEVUELTOS por valor. Solo los ya revisados (lista de abajo); uno nuevo
        // hace fallar la prueba hasta que se revise y se añada (o se cambie el código para no usarlo)
        // Los de antes de la 0.7.7 (código ya probado en el juego). En los MÉTODOS el interop recibe el valor ya empaquetado
        // por il2cpp_runtime_invoke; en los accesores de CAMPO (minúscula, p. ej. PlayPanel.get_hero) lee la memoria del
        // campo y la empaqueta: ninguno de los dos escribe en un IntPtr de la pila como el out de TryGetSignatureHero. El
        // estandarte dejó de leer WarbandBannerPanel._hero por prudencia, no porque ese acceso haya fallado
        // 0.7.7 (fix of 03-11): non-blittable structs RETURNED by value. Only those already reviewed (list below); a new one
        // makes the test fail until it is reviewed and added (or the code is changed so as not to use it)
        // Those from before 0.7.7 (code already tested in the game). In METHODS the interop receives the value already packed
        // by il2cpp_runtime_invoke; in FIELD accessors (lowercase, e.g. PlayPanel.get_hero) it reads the field's memory
        // and packs it: neither writes to a stack IntPtr like the out of TryGetSignatureHero. The
        // banner stopped reading WarbandBannerPanel._hero out of caution, not because that access had failed
        var revisados = new HashSet<string>(StringComparer.Ordinal)
        {
            "HeroStatBonusRule::get_Condition -> BonusCondition",
            "Il2CppSystem.Collections.Generic.Dictionary`2/KeyCollection<UnitName,SquadStats>::GetEnumerator -> Il2CppSystem.Collections.Generic.Dictionary`2/KeyCollection/Enumerator<UnitName,SquadStats>",
            "Il2CppSystem.Collections.Generic.List`1<Memori.SaveData.HeroDifficultiesCompleted>::get_Item -> Memori.SaveData.HeroDifficultiesCompleted",
            "Il2CppSystem.Collections.Generic.List`1<TJ.Hero>::get_Item -> TJ.Hero",
            "TJ.Battle.BattleSaveManager::GetArmyFromSaveData -> Il2CppSystem.ValueTuple`2<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray`1<Memori.SaveData.SquadToLoad>,Il2CppSystem.Collections.Generic.Dictionary`2<System.String,Memori.SaveData.SquadBattlePosition>>",
            "TJ.GearData::GetGear -> TJ.Gear",
            "TJ.HeroData::GetHeroByID -> TJ.Hero",
            "TJ.MainMenu.HeroRosterTile::get_Hero -> TJ.Hero",
            "TJ.MainMenu.PlayPanel::get_hero -> TJ.Hero",
            "TJ.TabletopTavernData::GetEnemyHeroForCampaign -> TJ.Hero",
            "TJ.TabletopTavernData::GetHeroLeadingRace -> TJ.Hero",
            "Unity.Entities.EntityQuery::ToEntityArray -> Unity.Collections.NativeArray`1<Unity.Entities.Entity>",
            "UnityEngine.JsonUtility::FromJson -> Memori.SaveData.SquadToLoad"
        };
        var nuevos = devuelven.Where(d => !revisados.Contains(d)).ToList();
        // Control positivo (revisión del segundo arreglo, hallazgo 6): el detector de retornos ve una llamada conocida
        // Positive control (review of the second fix, finding 6): the return detector sees a known call
        comprobar(devuelven.Contains("TJ.HeroData::GetHeroByID -> TJ.Hero"), "detector de retornos: ve HeroData.GetHeroByID -> TJ.Hero (control)");
        Console.WriteLine("INFO  getters/métodos del interop que devuelven struct no blittable: " + (devuelven.Count == 0 ? "ninguno" : string.Join("; ", devuelven)));
        comprobar(nuevos.Count == 0, "núcleo: ninguna llamada nueva a getters o métodos del interop que devuelvan un struct no blittable por valor" +
                  (nuevos.Count == 0 ? "" : ": " + string.Join("; ", nuevos)));
        // Control positivo de esta rama: el núcleo usa TJ.Hero (new Hero(), HeroID) y su nombre por referencia tiene que
        // coincidir con el del interop; si no, la búsqueda de abajo nunca encontraría nada
        // Positive control of this branch: the core uses TJ.Hero (new Hero(), HeroID) and its name by reference must
        // match the one in the interop; otherwise the search below would never find anything
        comprobar(padres.Contains("TJ.Hero") && noBlittables.Contains("TJ.Hero"),
                  $"núcleo: las referencias a TJ.Hero se nombran igual que en el interop ({padres.Count} tipos referenciados)");
        // Control positivo con la DLL del crash del 03-11 (copia en una carpeta de copias del autor que no se publica): el detector tiene que dar justo
        // la llamada a TryGetSignatureHero por su envoltorio
        // Positive control with the DLL from the 03-11 crash (copy in an unpublished folder of the author's copies): the detector must yield exactly
        // the call to TryGetSignatureHero through its wrapper
        string vieja = Path.GetFullPath(Path.Combine(raiz, "..", "..", "backups", "20261103_crash", "ZeroNucleo_instalada_0.7.5.dll"));
        if (File.Exists(vieja))
        {
            var antes = ReferenciasConByrefNoBlittable(vieja, noBlittables, tiposInterop, out _);
            comprobar(antes.Count == 1 && antes[0].Contains("TJ.Spells.SpellLoadout::TryGetSignatureHero"),
                      $"detector sobre la DLL del crash del 03-11: da la llamada de entonces ({string.Join("; ", antes)})");
        }
        else comprobar(false, $"DLL del crash del 03-11 para el control positivo encontrada ({vieja})");
        comprobar(llamadas.Count == 0,
                  "núcleo: ninguna llamada por el envoltorio del interop a métodos con ref/out de struct no blittable" +
                  (llamadas.Count == 0 ? "" : ": " + string.Join("; ", llamadas)));
    }

    // -----------------------------------------------------------------------
    // Sección: rutas (proyecto del núcleo e interop del juego según su csproj)
    // Section: paths (core project and game interop according to its csproj)
    // -----------------------------------------------------------------------
    // Sube desde la carpeta de salida hasta la que tiene ZeroNucleo.csproj
    // Goes up from the output folder to the one containing ZeroNucleo.csproj
    private static string RaizProyecto()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "ZeroNucleo.csproj"))) return d.FullName;
        return null;
    }

    // <GameDir> del csproj + \BepInEx\interop (la misma carpeta contra la que compila el núcleo)
    // <GameDir> from the csproj + \BepInEx\interop (the same folder the core compiles against)
    private static string CarpetaInterop(string csproj)
    {
        var m = Regex.Match(File.ReadAllText(csproj), @"<GameDir>([^<]+)</GameDir>");
        return m.Success ? Path.Combine(m.Groups[1].Value.Trim(), "BepInEx", "interop") : null;
    }

    // -----------------------------------------------------------------------
    // Sección: lectura de metadatos
    // Section: metadata reading
    // -----------------------------------------------------------------------
    // Añade los tipos del ensamblado que heredan directamente de Il2CppSystem.ValueType (structs de il2cpp con referencias:
    // los blittables heredan de System.ValueType y los enums de System.Enum) y, en «todos», cada tipo que define
    // Adds the assembly's types that inherit directly from Il2CppSystem.ValueType (il2cpp structs with references:
    // blittable ones inherit from System.ValueType and enums from System.Enum) and, in "all", every type that defines
    private static void StructsNoBlittables(string ruta, HashSet<string> destino, HashSet<string> todos)
    {
        try
        {
            using var pe = new PEReader(File.OpenRead(ruta));
            if (!pe.HasMetadata) return;
            var md = pe.GetMetadataReader();
            foreach (var h in md.TypeDefinitions)
            {
                var t = md.GetTypeDefinition(h);
                todos.Add(NombreDefinicion(md, h));
                if (t.BaseType.IsNil) continue;
                if (NombreTipo(md, t.BaseType) == "Il2CppSystem.ValueType") destino.Add(NombreDefinicion(md, h));
            }
        }
        catch (BadImageFormatException) { }   // DLL nativa u otra cosa sin metadatos: no cuenta
                                              // Native DLL or something else without metadata: does not count
    }

    // ¿Define el tipo un campo con este nombre exacto? (el puntero de método que el núcleo lee por reflexión)
    // Does the type define a field with this exact name? (the method pointer that the core reads via reflection)
    private static bool TieneCampo(string ruta, string tipo, string campo)
    {
        using var pe = new PEReader(File.OpenRead(ruta));
        var md = pe.GetMetadataReader();
        foreach (var h in md.TypeDefinitions)
        {
            if (NombreDefinicion(md, h) != tipo) continue;
            return md.GetTypeDefinition(h).GetFields().Any(f => md.GetString(md.GetFieldDefinition(f).Name) == campo);
        }
        return false;
    }

    // ¿Es este parámetro un byref de struct no blittable? (un struct genérico se compara sin sus argumentos)
    // Is this parameter a byref of a non-blittable struct? (a generic struct is compared without its arguments)
    private static bool EsByrefNoBlittable(string p, HashSet<string> noBlittables)
    {
        if (!p.EndsWith("&")) return false;
        string tipo = p.Substring(0, p.Length - 1);
        int menor = tipo.IndexOf('<');
        if (menor >= 0) tipo = tipo.Substring(0, menor);
        return noBlittables.Contains(tipo);
    }

    // Definiciones de métodos de un ensamblado del juego con algún parámetro byref de struct no blittable
    // Method definitions of a game assembly with some byref parameter of a non-blittable struct
    private static List<string> DefinicionesConByrefNoBlittable(string ruta, HashSet<string> noBlittables)
    {
        var r = new List<string>();
        using var pe = new PEReader(File.OpenRead(ruta));
        var md = pe.GetMetadataReader();
        var proveedor = new Nombres();
        foreach (var th in md.TypeDefinitions)
        {
            var t = md.GetTypeDefinition(th);
            foreach (var mh in t.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                var firma = m.DecodeSignature(proveedor, null);
                foreach (var p in firma.ParameterTypes)
                    if (EsByrefNoBlittable(p, noBlittables)) r.Add($"{NombreDefinicion(md, th)}::{md.GetString(m.Name)}({p})");
            }
        }
        return r;
    }

    // Referencias del núcleo a métodos de tipos del interop (lo que se llama por el envoltorio) con algún parámetro byref de
    // struct no blittable. En un método de un tipo genérico instanciado, los !i se sustituyen por los argumentos del tipo; en
    // las instanciaciones de métodos genéricos (MethodSpecification), además los !!i por los del método.
    // padres: nombres (sin argumentos genéricos) de los tipos dueños de esas referencias, para el control positivo
    // Core references to methods of interop types (what is called through the wrapper) with some byref parameter of a
    // non-blittable struct. In a method of an instantiated generic type, the !i are replaced by the type arguments; in
    // instantiations of generic methods (MethodSpecification), the !!i are also replaced by the method's.
    // parents: names (without generic arguments) of the types owning those references, for the positive control
    private static List<string> ReferenciasConByrefNoBlittable(string ruta, HashSet<string> noBlittables, HashSet<string> tiposInterop,
                                                               out HashSet<string> padres)
        => ReferenciasConByrefNoBlittable(ruta, noBlittables, tiposInterop, out padres, out _);

    // 0.7.7 (arreglo del 03-11): además, en «devuelven», las llamadas a métodos y getters del interop que DEVUELVEN un struct
    // no blittable por valor (como el getter del campo _hero de WarbandBannerPanel)
    // 0.7.7 (fix of 03-11): in addition, in "returns", the calls to interop methods and getters that RETURN a non-blittable
    // struct by value (like the getter of the _hero field of WarbandBannerPanel)
    private static List<string> ReferenciasConByrefNoBlittable(string ruta, HashSet<string> noBlittables, HashSet<string> tiposInterop,
                                                               out HashSet<string> padres, out List<string> devuelven)
    {
        var r = new List<string>();
        var dev = new List<string>();
        devuelven = dev;
        padres = new HashSet<string>(StringComparer.Ordinal);
        using var pe = new PEReader(File.OpenRead(ruta));
        var md = pe.GetMetadataReader();

        // Argumentos de método de cada instanciación genérica de una MemberReference (puede haber varias)
        // Method arguments of each generic instantiation of a MemberReference (there may be several)
        var instancias = new Dictionary<MemberReferenceHandle, List<ImmutableArray<string>>>();
        for (int fila = 1; fila <= md.GetTableRowCount(TableIndex.MethodSpec); fila++)
        {
            var spec = md.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(fila));
            if (spec.Method.Kind != HandleKind.MemberReference) continue;
            var args = spec.DecodeSignature(new Nombres(), null);
            var clave = (MemberReferenceHandle)spec.Method;
            if (!instancias.TryGetValue(clave, out var lista)) instancias[clave] = lista = new List<ImmutableArray<string>>();
            lista.Add(args);
        }

        foreach (var h in md.MemberReferences)
        {
            var mr = md.GetMemberReference(h);
            if (mr.GetKind() != MemberReferenceKind.Method) continue;
            ImmutableArray<string> argsTipo = ImmutableArray<string>.Empty;
            string padre = "?";
            if (mr.Parent.Kind == HandleKind.TypeSpecification)
            {
                // Tipo genérico instanciado: nombre y argumentos (los del tipo más externo, que se decodifica el último)
                // Instantiated generic type: name and arguments (those of the outermost type, which is decoded last)
                var ts = md.GetTypeSpecification((TypeSpecificationHandle)mr.Parent);
                var lector = new Nombres();
                padre = ts.DecodeSignature(lector, null);
                argsTipo = lector.UltimosArgumentos;
            }
            else if (mr.Parent.Kind == HandleKind.TypeReference || mr.Parent.Kind == HandleKind.TypeDefinition)
                padre = NombreTipo(md, (EntityHandle)mr.Parent);
            int menor = padre.IndexOf('<');
            string padreBase = menor >= 0 ? padre.Substring(0, menor) : padre;
            padres.Add(padreBase);
            if (!tiposInterop.Contains(padreBase)) continue;   // método gestionado (.NET, BepInEx, el propio núcleo): no va a nativo
                                                               // managed method (.NET, BepInEx, the core itself): does not go to native
            // Sin instanciar (o no genérico) y una vez por cada instanciación de método genérico
            // Uninstantiated (or non-generic) and once for each generic method instantiation
            var contextos = new List<Contexto> { new Contexto(argsTipo, ImmutableArray<string>.Empty) };
            if (instancias.TryGetValue(h, out var deMetodo)) contextos.AddRange(deMetodo.Select(a => new Contexto(argsTipo, a)));
            foreach (var c in contextos)
            {
                var firma = mr.DecodeMethodSignature(new Nombres(), c);
                foreach (var p in firma.ParameterTypes)
                    if (EsByrefNoBlittable(p, noBlittables)) r.Add($"{padre}::{md.GetString(mr.Name)}({p})");
                string ret = firma.ReturnType ?? "";
                int lt = ret.IndexOf('<');
                if (noBlittables.Contains(lt >= 0 ? ret.Substring(0, lt) : ret)) dev.Add($"{padre}::{md.GetString(mr.Name)} -> {ret}");
            }
        }
        return r.Distinct().ToList();
    }

    // Nombre completo de una definición de tipo (anidados como Externo/Interno)
    // Full name of a type definition (nested ones as Outer/Inner)
    private static string NombreDefinicion(MetadataReader md, TypeDefinitionHandle h)
    {
        var t = md.GetTypeDefinition(h);
        var declarante = t.GetDeclaringType();
        if (!declarante.IsNil) return NombreDefinicion(md, declarante) + "/" + md.GetString(t.Name);
        string ns = md.GetString(t.Namespace);
        return ns.Length == 0 ? md.GetString(t.Name) : ns + "." + md.GetString(t.Name);
    }

    // Nombre completo de una referencia de tipo (anidados como Externo/Interno)
    // Full name of a type reference (nested ones as Outer/Inner)
    private static string NombreReferencia(MetadataReader md, TypeReferenceHandle h)
    {
        var t = md.GetTypeReference(h);
        if (t.ResolutionScope.Kind == HandleKind.TypeReference)
            return NombreReferencia(md, (TypeReferenceHandle)t.ResolutionScope) + "/" + md.GetString(t.Name);
        string ns = md.GetString(t.Namespace);
        return ns.Length == 0 ? md.GetString(t.Name) : ns + "." + md.GetString(t.Name);
    }

    // Nombre de un tipo base o padre de miembro (definición, referencia o especificación genérica)
    // Name of a base type or member parent (definition, reference or generic specification)
    private static string NombreTipo(MetadataReader md, EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.TypeDefinition: return NombreDefinicion(md, (TypeDefinitionHandle)h);
            case HandleKind.TypeReference: return NombreReferencia(md, (TypeReferenceHandle)h);
            case HandleKind.TypeSpecification:
                return md.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(new Nombres(), null);
            default: return "?";
        }
    }

    // -----------------------------------------------------------------------
    // Sección: proveedor de firmas que da el nombre de cada tipo (byref acaba en '&')
    // Section: signature provider that gives the name of each type (byref ends in '&')
    // -----------------------------------------------------------------------
    // Contexto genérico: argumentos del tipo dueño (!i) y del método instanciado (!!i)
    // Generic context: arguments of the owner type (!i) and of the instantiated method (!!i)
    private sealed class Contexto
    {
        public readonly ImmutableArray<string> Tipo, Metodo;
        public Contexto(ImmutableArray<string> tipo, ImmutableArray<string> metodo) { Tipo = tipo; Metodo = metodo; }
    }

    private sealed class Nombres : ISignatureTypeProvider<string, object>
    {
        // Argumentos de la última instanciación genérica decodificada (la más externa se decodifica la última)
        // Arguments of the last decoded generic instantiation (the outermost one is decoded last)
        public ImmutableArray<string> UltimosArgumentos { get; private set; } = ImmutableArray<string>.Empty;
        public string GetPrimitiveType(PrimitiveTypeCode c) => "System." + c;
        public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) => NombreDefinicion(r, h);
        public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) => NombreReferencia(r, h);
        public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k) =>
            r.GetTypeSpecification(h).DecodeSignature(this, c);
        public string GetByReferenceType(string e) => e + "&";
        public string GetPointerType(string e) => e + "*";
        public string GetSZArrayType(string e) => e + "[]";
        public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
        public string GetGenericInstantiation(string g, ImmutableArray<string> a)
        {
            UltimosArgumentos = a;
            return g + "<" + string.Join(",", a) + ">";
        }
        public string GetGenericMethodParameter(object c, int i) =>
            c is Contexto x && !x.Metodo.IsDefault && i < x.Metodo.Length ? x.Metodo[i] : "!!" + i;
        public string GetGenericTypeParameter(object c, int i) =>
            c is Contexto x && !x.Tipo.IsDefault && i < x.Tipo.Length ? x.Tipo[i] : "!" + i;
        public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr";
        public string GetModifiedType(string m, string u, bool req) => u;
        public string GetPinnedType(string e) => e;
    }
}
