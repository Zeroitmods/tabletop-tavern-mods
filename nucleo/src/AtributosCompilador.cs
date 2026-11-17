// AtributosCompilador.cs
// Atributos que el compilador de C# necesita y que no encuentra utilizables entre las referencias del juego
// (algún ensamblado de interop declara un NullableAttribute sin constructores y tapa el del runtime).
// Declararlos aquí como internos resuelve el conflicto sin efecto alguno en tiempo de ejecución.
// AtributosCompilador.cs
// Attributes that the C# compiler needs and cannot find usable among the game's references
// (some interop assembly declares a NullableAttribute without constructors and hides the runtime's one).
// Declaring them here as internal resolves the conflict with no effect at runtime.

namespace System.Runtime.CompilerServices
{
    // Sección: atributos de anotaciones de nulabilidad generados por el compilador
    // Section: nullability annotation attributes produced by the compiler
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(byte b) { }
        public NullableAttribute(byte[] b) { }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        public NullableContextAttribute(byte b) { }
    }
}
