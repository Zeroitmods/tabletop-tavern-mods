// PruebasCicloVida.cs
// Prueba 31 (03-11-2026, arreglo de la 0.7.7): ningún enganche de un método de ciclo de vida de Unity (Start, Awake,
// OnEnable, OnDisable, Update, LateUpdate, FixedUpdate, OnDestroy) llama, directa o indirectamente, a
// FindObjectsOfType*/FindObjectOfType*/FindObjectsByType/FindAnyObjectByType/FindFirstObjectByType. La 0.7.7 no cargaba
// («Stack overflow»): Resources.FindObjectsOfTypeAll dentro del postfix de SquadFlagGameObject.Start hacía que Unity
// llamara a Start de otras banderas, que volvían al postfix, sin fin.
// Cómo: lee el IL de la DLL compilada (System.Reflection.Metadata). En cada método, los textos (ldstr) que preceden a una
// llamada a Plugin.Parchear dan el método parcheado («Start»…) y los nombres de los enganches (nameof); desde cada
// enganche se sigue el grafo de llamadas dentro de la DLL y se busca una llamada a Find*.
// Autor: ZeroitMods.
// PruebasCicloVida.cs
// Test 31 (03-11-2026, fix for 0.7.7): no hook of a Unity lifecycle method (Start, Awake,
// OnEnable, OnDisable, Update, LateUpdate, FixedUpdate, OnDestroy) calls, directly or indirectly,
// FindObjectsOfType*/FindObjectOfType*/FindObjectsByType/FindAnyObjectByType/FindFirstObjectByType. 0.7.7 would not load
// («Stack overflow»): Resources.FindObjectsOfTypeAll inside the postfix of SquadFlagGameObject.Start made Unity
// call Start of other banners, which went back to the postfix, endlessly.
// How: it reads the IL of the compiled DLL (System.Reflection.Metadata). In each method, the strings (ldstr) preceding a
// call to Plugin.Parchear give the patched method («Start»…) and the names of the hooks (nameof); from each
// hook the call graph is followed within the DLL and a call to Find* is searched for.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class PruebasCicloVida
{
    private static readonly HashSet<string> CicloVida = new HashSet<string>
        { "Start", "Awake", "OnEnable", "OnDisable", "Update", "LateUpdate", "FixedUpdate", "OnDestroy", "OnValidate", "Reset",
          "OnGUI", "OnBecameVisible", "OnBecameInvisible", "OnRectTransformDimensionsChange", "OnTransformParentChanged",
          "OnApplicationFocus", "OnApplicationPause" };
    // Revisión del arreglo, hallazgo 4: los enganches del estandarte se vigilan siempre por nombre, aunque su método no sea
    // de ciclo de vida (EnsureBuilt y Refresh los llama el juego desde OnEnable del panel), y Preparar también
    // Review of the fix, finding 4: the banner hooks are always watched by name, even if their method is not
    // a lifecycle one (EnsureBuilt and Refresh are called by the game from the panel's OnEnable), and so is Preparar
    private static readonly string[] SiempreVigilados = { "TrasEnsureBuilt", "TrasRefresh", "TrasApplyTeamColors", "Preparar" };

    // -----------------------------------------------------------------------
    // Sección: entrada de la prueba
    // Section: test input
    // -----------------------------------------------------------------------
    public static void Todas(Action<bool, string> comprobar)
    {
        Console.WriteLine("--- Prueba 31: ningún enganche de ciclo de vida de Unity llama a FindObjectsOfType* ---");
        try { Ejecutar(comprobar); }
        catch (Exception e) { comprobar(false, "prueba 31 sin errores al leer el IL: " + e); }
    }

    private static void Ejecutar(Action<bool, string> comprobar)
    {
        string dll = RutaDll();
        if (dll == null) { comprobar(false, "prueba 31: DLL compilada del núcleo encontrada (compila en Release antes de las pruebas)"); return; }
        using var pe = new PEReader(File.OpenRead(dll));
        var md = pe.GetMetadataReader();
        var tamOperando = TablaOperandos();

        // Métodos de la DLL: nombre -> definiciones; e IL decodificado (llamadas y textos) de cada uno
        // DLL methods: name -> definitions; and decoded IL (calls and strings) of each one
        var porNombre = new Dictionary<string, List<MethodDefinitionHandle>>();
        var cuerpos = new Dictionary<MethodDefinitionHandle, List<(string op, int token, string texto)>>();
        foreach (var th in md.TypeDefinitions)
            foreach (var mh in md.GetTypeDefinition(th).GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                string n = md.GetString(m.Name);
                if (!porNombre.TryGetValue(n, out var l)) porNombre[n] = l = new List<MethodDefinitionHandle>();
                l.Add(mh);
                if (m.RelativeVirtualAddress != 0) cuerpos[mh] = Decodificar(pe.GetMethodBody(m.RelativeVirtualAddress), md, tamOperando);
            }

        // Enganches de ciclo de vida: textos antes de cada llamada a Parchear
        // Lifecycle hooks: strings before each call to Parchear
        var enganches = new HashSet<string>();
        foreach (var c in cuerpos.Values)
        {
            var ventana = new List<string>();
            foreach (var (op, token, texto) in c)
            {
                if (op == "ldstr") { ventana.Add(texto); continue; }
                if ((op == "call" || op == "callvirt") && NombreMetodo(md, token) == "Parchear")
                {
                    if (ventana.Any(CicloVida.Contains))
                        foreach (var t in ventana) if (!CicloVida.Contains(t) && porNombre.ContainsKey(t)) enganches.Add(t);
                    ventana.Clear();
                }
            }
        }
        comprobar(enganches.Contains("TrasEntityWatcherUpdate") && !enganches.Contains("TrasFlagStart"), $"prueba 31: se reconocen los enganches de ciclo de vida ({string.Join(", ", enganches.OrderBy(x => x))})");
        var faltan = SiempreVigilados.Where(n => !porNombre.ContainsKey(n)).ToList();
        comprobar(faltan.Count == 0, $"prueba 31: los enganches del estandarte existen en la DLL ({(faltan.Count == 0 ? "todos" : "faltan " + string.Join(", ", faltan))})");
        foreach (var n in SiempreVigilados) if (porNombre.ContainsKey(n)) enganches.Add(n);

        // La detección funciona: en la DLL hay alguna llamada a Find* (AudioGuardia busca clips por nombre fuera de ellos)
        // Detection works: the DLL contains some call to Find* (AudioGuardia looks up clips by name outside of them)
        bool hayFind = cuerpos.Values.Any(c => c.Any(i => EsLlamada(i.op) && EsFind(NombreMetodo(md, i.token))));
        comprobar(hayFind, "prueba 31: el escáner detecta llamadas a FindObjectsOfType* en la DLL (control)");

        // Desde cada enganche, grafo de llamadas dentro de la DLL
        // From each hook, call graph within the DLL
        var malos = new List<string>();
        foreach (var e in enganches)
        {
            var pendientes = new Stack<(MethodDefinitionHandle h, string ruta)>();
            var vistos = new HashSet<MethodDefinitionHandle>();
            foreach (var h in porNombre[e]) pendientes.Push((h, e));
            while (pendientes.Count > 0)
            {
                var (h, ruta) = pendientes.Pop();
                if (!vistos.Add(h) || !cuerpos.TryGetValue(h, out var c)) continue;
                foreach (var (op, token, _) in c)
                {
                    if (!EsLlamada(op) && op != "ldftn" && op != "ldvirtftn") continue;
                    string nombre = NombreMetodo(md, token);
                    if (EsFind(nombre)) { malos.Add($"{ruta} -> {nombre}"); continue; }
                    if (vistos.Count >= 20000) { malos.Add(ruta + " -> grafo truncado (más de 20000 métodos)"); break; }
                    foreach (var d in Destinos(md, token, porNombre, cuerpos))
                        pendientes.Push((d, ruta + " -> " + nombre));
                }
            }
        }
        comprobar(malos.Count == 0, $"prueba 31: ningún enganche de ciclo de vida de Unity llama a FindObjectsOfType* ({(malos.Count == 0 ? "ninguno" : string.Join("; ", malos.Distinct().Take(6)))})");
    }

    // -----------------------------------------------------------------------
    // Sección: ayudas de lectura del IL
    // Section: IL reading helpers
    // -----------------------------------------------------------------------
    // Métodos de la DLL a los que puede llevar una llamada (revisión del arreglo, hallazgo 3), de forma conservadora:
    //  - definición directa; si no tiene cuerpo (abstracta o de interfaz), todas las de la DLL con ese nombre;
    //  - especificación genérica: su método de base;
    //  - referencia a miembro de un tipo genérico propio (TypeSpec) o de la DLL: todas las de la DLL con ese nombre;
    //  - constructor de un tipo generado por el compilador («<...>»: iteradores, async, cierres): todos sus métodos
    //    (MoveNext de corrutinas y lambdas de los cierres); y los tipos anidados «<Nombre>…» de cada método visitado.
    // Límites (ronda 2, hallazgo 7): delegados guardados en campos de otros tipos; y solo se buscan Find* — lo que hagan
    // por dentro métodos del juego o de Unity (Instance, GetSquadTypeFlagSprite…) no se ve: por eso el código usa
    // InstanceIfExists y lee datos ya cargados.
    // DLL methods a call can lead to (review of the fix, finding 3), conservatively:
    //  - direct definition; if it has no body (abstract or interface), all those in the DLL with that name;
    //  - generic specification: its base method;
    //  - member reference of one's own generic type (TypeSpec) or of the DLL: all those in the DLL with that name;
    //  - constructor of a compiler-generated type («<...>»: iterators, async, closures): all its methods
    //    (MoveNext of coroutines and lambdas of closures); and the nested types «<Nombre>…» of each visited method.
    // Limits (round 2, finding 7): delegates stored in fields of other types; and only Find* is searched for — what
    // game or Unity methods do internally (Instance, GetSquadTypeFlagSprite…) is not seen: that is why the code uses
    // InstanceIfExists and reads already loaded data.
    private static IEnumerable<MethodDefinitionHandle> Destinos(MetadataReader md, int token,
        Dictionary<string, List<MethodDefinitionHandle>> porNombre, Dictionary<MethodDefinitionHandle, List<(string, int, string)>> cuerpos)
    {
        var h = MetadataTokens.EntityHandle(token);
        var res = new List<MethodDefinitionHandle>();
        if (h.Kind == HandleKind.MethodSpecification)
        {
            res.AddRange(Destinos(md, MetadataTokens.GetToken(md.GetMethodSpecification((MethodSpecificationHandle)h).Method), porNombre, cuerpos));
            return res;
        }
        string nombre = NombreMetodo(md, token);
        if (h.Kind == HandleKind.MethodDefinition)
        {
            var mh = (MethodDefinitionHandle)h;
            res.Add(mh);
            // Sin cuerpo o virtual: también las redefiniciones de la DLL con ese nombre (ronda 2, hallazgo 7)
            // No body or virtual: also the overrides in the DLL with that name (round 2, finding 7)
            var def = md.GetMethodDefinition(mh);
            bool virtualM = (def.Attributes & MethodAttributes.Virtual) != 0;
            if ((!cuerpos.ContainsKey(mh) || virtualM) && nombre != null && porNombre.TryGetValue(nombre, out var mismos)) res.AddRange(mismos);
            var tipo = md.GetTypeDefinition(def.GetDeclaringType());
            if (nombre == ".ctor" && md.GetString(tipo.Name).StartsWith("<")) res.AddRange(tipo.GetMethods());
            // Tipos generados por el compilador para este método («<Nombre>d__» iteradores y async en struct, «<Nombre>g__»
            // funciones locales, cierres): todos sus métodos (ronda 2, hallazgo 7)
            // Compiler-generated types for this method («<Nombre>d__» iterators and async in struct, «<Nombre>g__»
            // local functions, closures): all their methods (round 2, finding 7)
            foreach (var anidado in tipo.GetNestedTypes())
            {
                var ta = md.GetTypeDefinition(anidado);
                if (nombre != null && md.GetString(ta.Name).StartsWith("<" + nombre + ">")) res.AddRange(ta.GetMethods());
            }
        }
        else if (h.Kind == HandleKind.MemberReference)
        {
            var mr = md.GetMemberReference((MemberReferenceHandle)h);
            bool propio = mr.Parent.Kind == HandleKind.TypeSpecification || mr.Parent.Kind == HandleKind.TypeDefinition;
            if (propio && nombre != null && nombre != ".ctor" && porNombre.TryGetValue(nombre, out var mismos)) res.AddRange(mismos);
        }
        return res;
    }

    private static bool EsLlamada(string op) => op == "call" || op == "callvirt" || op == "newobj";

    private static bool EsFind(string n) => n != null && (n.StartsWith("FindObjectsOfType") || n.StartsWith("FindObjectOfType")
        || n == "FindObjectsByType" || n == "FindAnyObjectByType" || n == "FindFirstObjectByType");

    // Tamaño del operando de cada código de operación (de System.Reflection.Emit.OpCodes)
    // Operand size of each opcode (from System.Reflection.Emit.OpCodes)
    private static Dictionary<short, (string nombre, OperandType tipo)> TablaOperandos()
    {
        var t = new Dictionary<short, (string, OperandType)>();
        foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (f.GetValue(null) is OpCode oc) t[oc.Value] = (oc.Name, oc.OperandType);
        return t;
    }

    // Instrucciones de un cuerpo: (nombre, token si lo hay, texto si es ldstr)
    // Instructions of a body: (name, token if any, string if ldstr)
    private static List<(string, int, string)> Decodificar(MethodBodyBlock cuerpo, MetadataReader md, Dictionary<short, (string nombre, OperandType tipo)> tabla)
    {
        var res = new List<(string, int, string)>();
        var r = cuerpo.GetILReader();
        while (r.RemainingBytes > 0)
        {
            short v = r.ReadByte();
            if (v == 0xFE) v = unchecked((short)(0xFE00 | r.ReadByte()));
            if (!tabla.TryGetValue(v, out var oc)) break;   // código desconocido: se para (no inventa)
                                                            // unknown opcode: stops (does not guess)
            int token = 0; string texto = null;
            switch (oc.tipo)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: r.ReadByte(); break;
                case OperandType.InlineVar: r.ReadInt16(); break;
                case OperandType.InlineI8: case OperandType.InlineR: r.ReadInt64(); break;
                case OperandType.ShortInlineR: r.ReadSingle(); break;
                case OperandType.InlineSwitch: { int n = r.ReadInt32(); for (int i = 0; i < n; i++) r.ReadInt32(); break; }
                case OperandType.InlineString: token = r.ReadInt32(); texto = md.GetUserString(MetadataTokens.UserStringHandle(token & 0xFFFFFF)); break;
                default: token = r.ReadInt32(); break;   // InlineMethod, InlineField, InlineType, InlineTok, InlineSig, InlineBrTarget, InlineI
            }
            res.Add((oc.nombre, token, texto));
        }
        return res;
    }

    // Nombre del método de un token (definición, referencia o especificación genérica)
    // Method name from a token (definition, reference or generic specification)
    private static string NombreMetodo(MetadataReader md, int token)
    {
        try
        {
            var h = MetadataTokens.EntityHandle(token);
            switch (h.Kind)
            {
                case HandleKind.MethodDefinition: return md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)h).Name);
                case HandleKind.MemberReference: return md.GetString(md.GetMemberReference((MemberReferenceHandle)h).Name);
                case HandleKind.MethodSpecification: return NombreMetodo(md, MetadataTokens.GetToken(md.GetMethodSpecification((MethodSpecificationHandle)h).Method));
            }
        }
        catch { }
        return null;
    }

    private static string RutaDll()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            string csproj = Path.Combine(d.FullName, "ZeroNucleo.csproj");
            if (File.Exists(csproj))
            {
                string dll = Path.Combine(d.FullName, "bin", "Release", "net6.0", "ZeroNucleo.dll");
                return File.Exists(dll) ? dll : null;
            }
        }
        return null;
    }
}
