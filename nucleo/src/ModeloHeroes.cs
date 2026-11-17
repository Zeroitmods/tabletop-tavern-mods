// ModeloHeroes.cs
// Fase F5: los héroes nuevos (901, 902, 903, 904) con SU modelo 3D en lugar de la figura de
// su héroe de serie prestado (901<-Edric, 902<-Rhydan, 903<-Draven, 904<-Tokugawa).
// ModeloHeroes.cs
// Phase F5: the new heroes (901, 902, 903, 904) with THEIR 3D model instead of the figure of
// their borrowed hero (901<-Edric, 902<-Rhydan, 903<-Draven, 904<-Tokugawa).
//
// Cómo: el juego sigue cargando e instanciando el prefab del héroe prestado (la entrada de assets de HeroesNuevos), con
// su Animator y sus animaciones de taberna. Sobre ESA instancia (nunca sobre el prefab, que es el asset de Addressables
// del héroe de serie) se cuelga un hijo «ZeroModeloHeroe» con un SkinnedMeshRenderer cuya malla (formato ZHM1,
// FormatoModeloHeroe.cs) va ligada a los huesos del propio prefab, por ruta; así la animan las animaciones de serie. Los
// renderers del prestado se apagan (renderer.enabled = false; nunca se desactivan nodos: algunos son huesos). Su Animator
// se pasa a cullingMode = AlwaysAnimate: con los renderers prestados apagados se creía invisible y no movía los huesos
// (pose T, arreglado el 18-10); el log dice una vez por figura el estado del Animator y si los huesos se mueven.
// How: the game keeps loading and instantiating the borrowed hero's prefab (the HeroesNuevos asset entry), with
// its Animator and its tavern animations. On THAT instance (never on the prefab, which is the Addressables asset
// of the stock hero) a child «ZeroModeloHeroe» is attached with a SkinnedMeshRenderer whose mesh (ZHM1 format,
// FormatoModeloHeroe.cs) is bound to the prefab's own bones, by path; this way the stock animations animate it. The
// borrowed hero's renderers are turned off (renderer.enabled = false; nodes are never deactivated: some are bones). Its Animator
// is set to cullingMode = AlwaysAnimate: with the borrowed renderers off it was considered invisible and did not move the bones
// (T-pose, fixed on 18-10); the log states once per figure the Animator's state and whether the bones move.
//
// Dónde: un componente propio (VigiaModeloHeroes) mira en LateUpdate las instancias que crea el juego y les cambia la
// malla UNA vez. No se engancha a los métodos que instancian (son async void y PlayerToken.LoadHero está inlineado en
// el código nativo): se lee el resultado ya creado, que no depende del inlining.
//   - Pantalla «Seleccionar al héroe» (taberna): PlayPanel.heroObject con PlayPanel.hero.HeroID.
//   - Colección, pestaña Héroes: CollectionPreviewRig._model con _shownId «hero:9xx».
//   - Taberna de la partida: GamePlayerLoader.playerHeroGameObject con el héroe activo (HeroBonusManager).
//   - Mapa de campaña: la figura encendida de PlayerToken.heroPrefabs con el héroe activo (el hueco del héroe nuevo lo
//     prepara CampanaHeroes.AsegurarFiguraEnMapa; aquí solo se le cambia la malla). Esas figuras NO son el prefab
//     animado sino estatuas estáticas (MeshFilter «<Prefab> Statue» con el material liso PlayerToken): se cambia la
//     malla del MeshFilter por la estatua del héroe nuevo (heroes\<id>_ficha.bin) y se deja su material.
// Todo opcional (Visual.ModeloHeroes) y en blando: si falta el fichero, no cuadra un hueso o falla algo, se escribe una
// vez en el log y se queda la figura prestada como hasta ahora.
// Autor: ZeroitMods.
// Where: a custom component (VigiaModeloHeroes) watches in LateUpdate the instances the game creates and changes their
// mesh ONCE. It does not hook into the methods that instantiate them (they are async void and PlayerToken.LoadHero is inlined in
// the native code): the already created result is read, which does not depend on inlining.
//   - «Select your hero» screen (tavern): PlayPanel.heroObject with PlayPanel.hero.HeroID.
//   - Collection, Heroes tab: CollectionPreviewRig._model with _shownId «hero:9xx».
//   - Tavern of the run: GamePlayerLoader.playerHeroGameObject with the active hero (HeroBonusManager).
//   - Campaign map: the lit figure of PlayerToken.heroPrefabs with the active hero (the new hero's slot is
//     prepared by CampanaHeroes.AsegurarFiguraEnMapa; here only its mesh is changed). Those figures are NOT the animated
//     prefab but static statues (MeshFilter «<Prefab> Statue» with the plain PlayerToken material): the
//     MeshFilter's mesh is replaced with the new hero's statue (heroes\<id>_ficha.bin) and its material is kept.
// Everything optional (Visual.ModeloHeroes) and soft-failing: if the file is missing, a bone doesn't match or something fails, it is written once
// to the log and the borrowed figure stays as before.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;
using TJ.MainMenu;
using PlayerToken = TJ.Map.PlayerToken;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: modelos cargados y cambio de malla en una instancia
    // Section: loaded models and mesh swap on an instance
    // -----------------------------------------------------------------------
    public static class ModeloHeroes
    {
        /// <summary>Nombre del hijo que se cuelga de la instancia con el modelo propio (marca de «ya hecho»).</summary>
        /// Name of the child attached to the instance with the custom model ("already done" marker).
        public const string NombreHijo = "ZeroModeloHeroe";

        // Modelo de un héroe ya construido en Unity (se reutiliza en todas las instancias)
        // Model of a hero already built in Unity (reused in all instances)
        private sealed class Cargado
        {
            public DatosModeloHeroe Datos;
            public Mesh Malla;
            public Texture2D Textura;
            public Material Material;       // se crea con la primera instancia (copia de un material URP del juego)
                                            // created with the first instance (copy of a game URP material)
        }

        private static readonly Dictionary<int, Cargado> _cargados = new Dictionary<int, Cargado>();
        // Estatuas del mapa de campaña (malla estática <id>_ficha.bin) ya construidas, y las que no se pudieron leer
        // Campaign map statues (static mesh <id>_ficha.bin) already built, and those that could not be read
        private static readonly Dictionary<int, Mesh> _estatuas = new Dictionary<int, Mesh>();
        private static readonly HashSet<int> _estatuasFallidas = new HashSet<int>();
        private static readonly HashSet<int> _fallidos = new HashSet<int>();
        private static readonly HashSet<string> _avisos = new HashSet<string>();
        private static int _aplicados;

        /// <summary>Instancias a las que se ha puesto el modelo propio en esta sesión (para el log y la autoprueba).</summary>
        /// Instances that have had the custom model applied in this session (for the log and the self-test).
        public static int Aplicados => _aplicados;

        /// <summary>¿Está encendido el cambio de modelo? (opción Visual.ModeloHeroes y facciones nuevas activas)</summary>
        /// Is the model swap enabled? (option Visual.ModeloHeroes and new factions active)
        public static bool Activo => Plugin.CfgModeloHeroes != null && Plugin.CfgModeloHeroes.Value && FaccionesNuevas.Activas;

        private static void AvisoUnaVez(string clave, string texto)
        {
            if (_avisos.Add(clave)) Plugin.Aviso(texto);
        }

        /// <summary>
        /// Ruta del modelo propio de un héroe: la del manifiesto de su módulo («modelo») o, si el manifiesto es de antes de
        /// la F5, heroes\&lt;id&gt;_modelo.bin junto a su retrato. null si no hay fichero.
        /// Path of a hero's custom model: the one from its module manifest («modelo») or, if the manifest predates
        /// F5, heroes\&lt;id&gt;_modelo.bin next to its portrait. null if there is no file.
        /// </summary>
        public static string RutaModelo(int heroID)
        {
            if (!FaccionesNuevas.Heroes.TryGetValue(heroID, out var h)) return null;
            if (!string.IsNullOrEmpty(h.Modelo)) return File.Exists(h.Modelo) ? h.Modelo : null;
            if (string.IsNullOrEmpty(h.Retrato)) return null;
            string junto = Path.Combine(Path.GetDirectoryName(h.Retrato) ?? "", heroID + "_modelo.bin");
            return File.Exists(junto) ? junto : null;
        }

        /// <summary>¿Tiene este héroe un modelo propio en su módulo?</summary>
        /// Does this hero have a custom model in its module?
        public static bool TieneModelo(int heroID) => RutaModelo(heroID) != null;

        // ---------------------------------------------------------------
        // Carga del fichero y construcción de la malla y la textura
        // File loading and construction of the mesh and the texture
        // ---------------------------------------------------------------
        private static Cargado Cargar(int heroID)
        {
            if (_cargados.TryGetValue(heroID, out var c) && c.Malla != null && c.Textura != null) return c;
            if (_fallidos.Contains(heroID)) return null;
            string ruta = RutaModelo(heroID);
            if (ruta == null) return null;
            string rutaTex = Path.ChangeExtension(ruta, ".png");
            try
            {
                var datos = FormatoModeloHeroe.Leer(ruta);
                var errores = datos.Validar();
                if (errores.Count > 0) throw new InvalidDataException(string.Join("; ", errores));
                if (!File.Exists(rutaTex)) throw new FileNotFoundException("falta la textura", rutaTex);
                // Textura primero (si falla, no queda ninguna malla huérfana)
                // Texture first (if it fails, no orphaned mesh is left)
                var textura = CargarTextura(rutaTex, "zeroModeloTex" + heroID);
                c = new Cargado { Datos = datos, Textura = textura, Malla = ConstruirMalla(datos, "zeroModelo" + heroID) };
                _cargados[heroID] = c;
                Plugin.Info($"Modelo propio del héroe {heroID} cargado: {datos.Vertices} vértices, {datos.Triangulos.Length / 3} triángulos, " +
                            $"{datos.Huesos} huesos del prefab «{datos.Prefab}» (héroe {datos.Prestado}); textura {c.Textura.width}x{c.Textura.height}.");
                return c;
            }
            catch (Exception e)
            {
                _fallidos.Add(heroID);
                Plugin.Aviso($"Modelo propio del héroe {heroID}: no se pudo cargar {ruta} ({e.GetType().Name}: {e.Message}); se ve la figura prestada.");
                return null;
            }
        }

        // Estatua del mapa: <id>_ficha.bin junto al modelo (malla estática del héroe con los brazos abajo, a la escala de
        // las estatuas del juego). null si no hay o no se puede leer (se queda la estatua prestada)
        // Map statue: <id>_ficha.bin next to the model (static mesh of the hero with arms down, at the scale of
        // the game's statues). null if missing or unreadable (the borrowed statue stays)
        private static Mesh CargarEstatua(int heroID)
        {
            if (_estatuas.TryGetValue(heroID, out var e) && e != null) return e;
            if (_estatuasFallidas.Contains(heroID)) return null;
            string modelo = RutaModelo(heroID);
            string ruta = modelo == null ? null : Path.Combine(Path.GetDirectoryName(modelo) ?? "", heroID + "_ficha.bin");
            try
            {
                if (ruta == null || !File.Exists(ruta)) throw new FileNotFoundException("no hay estatua del mapa", ruta ?? heroID + "_ficha.bin");
                var datos = FormatoModeloHeroe.Leer(ruta);
                var errores = datos.Validar();
                if (errores.Count > 0) throw new InvalidDataException(string.Join("; ", errores));
                e = ConstruirMalla(datos, "zeroEstatua" + heroID, false);
                _estatuas[heroID] = e;
                Plugin.Info($"Estatua del mapa del héroe {heroID} cargada: {datos.Vertices} vértices ({datos.Prefab}).");
                return e;
            }
            catch (Exception ex)
            {
                _estatuasFallidas.Add(heroID);
                Plugin.Aviso($"Estatua del mapa del héroe {heroID}: {ex.GetType().Name}: {ex.Message}; se ve la estatua prestada.");
                return null;
            }
        }

        // UnityEngine.Mesh con vértices, normales, UV y triángulos del fichero y, si es una malla con piel, pesos y bindposes
        // UnityEngine.Mesh with the file's vertices, normals, UVs and triangles and, if it is a skinned mesh, weights and bindposes
        private static Mesh ConstruirMalla(DatosModeloHeroe d, string nombre, bool conPiel = true)
        {
            int n = d.Vertices, h = d.Huesos;
            var pos = new Vector3[n];
            var nrm = new Vector3[n];
            var uv = new Vector2[n];
            var pesos = new BoneWeight[n];
            for (int i = 0; i < n; i++)
            {
                pos[i] = new Vector3(d.Posiciones[i * 3], d.Posiciones[i * 3 + 1], d.Posiciones[i * 3 + 2]);
                nrm[i] = new Vector3(d.Normales[i * 3], d.Normales[i * 3 + 1], d.Normales[i * 3 + 2]);
                uv[i] = new Vector2(d.Uv[i * 2], d.Uv[i * 2 + 1]);
                var bw = new BoneWeight();
                bw.boneIndex0 = d.IndicesHueso[i * 4]; bw.weight0 = d.Pesos[i * 4];
                bw.boneIndex1 = d.IndicesHueso[i * 4 + 1]; bw.weight1 = d.Pesos[i * 4 + 1];
                bw.boneIndex2 = d.IndicesHueso[i * 4 + 2]; bw.weight2 = d.Pesos[i * 4 + 2];
                bw.boneIndex3 = d.IndicesHueso[i * 4 + 3]; bw.weight3 = d.Pesos[i * 4 + 3];
                pesos[i] = bw;
            }
            var bind = new Matrix4x4[h];
            for (int b = 0; b < h; b++)
            {
                int o = b * 16;
                var m = new Matrix4x4();
                m.m00 = d.Bindposes[o]; m.m01 = d.Bindposes[o + 1]; m.m02 = d.Bindposes[o + 2]; m.m03 = d.Bindposes[o + 3];
                m.m10 = d.Bindposes[o + 4]; m.m11 = d.Bindposes[o + 5]; m.m12 = d.Bindposes[o + 6]; m.m13 = d.Bindposes[o + 7];
                m.m20 = d.Bindposes[o + 8]; m.m21 = d.Bindposes[o + 9]; m.m22 = d.Bindposes[o + 10]; m.m23 = d.Bindposes[o + 11];
                m.m30 = d.Bindposes[o + 12]; m.m31 = d.Bindposes[o + 13]; m.m32 = d.Bindposes[o + 14]; m.m33 = d.Bindposes[o + 15];
                bind[b] = m;
            }
            var malla = new Mesh();
            malla.name = nombre;
            malla.hideFlags = HideFlags.HideAndDontSave;
            if (n > 65535) malla.indexFormat = IndexFormat.UInt32;          // antes de los triángulos
                                                                            // before the triangles
            malla.vertices = pos;
            malla.normals = nrm;
            malla.uv = uv;
            malla.triangles = d.Triangulos;
            if (conPiel)
            {
                malla.boneWeights = pesos;
                malla.bindposes = bind;
            }
            malla.RecalculateBounds();
            return malla;
        }

        // Textura del PNG del modelo, con mipmaps y comprimida en la GPU (la copia en memoria se libera)
        // Texture from the model's PNG, with mipmaps and GPU-compressed (the in-memory copy is released)
        private static Texture2D CargarTextura(string ruta, string nombre)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            bool leida = false;
            try { leida = ImageConversion.LoadImage(tex, File.ReadAllBytes(ruta), false); }
            finally { if (!leida) UnityEngine.Object.Destroy(tex); }                    // sin textura huérfana si falla
                                                                                        // no orphaned texture if it fails
            if (!leida) throw new InvalidDataException($"{ruta}: no es un PNG válido");
            tex.name = nombre;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 4;
            tex.Apply(true, false);                                          // mipmaps
            try { tex.Compress(true); } catch (Exception e) { AvisoUnaVez("comprimir", "Modelos de héroe: la textura se queda sin comprimir (" + e.Message + ")."); }
            tex.Apply(false, true);                                          // sube a la GPU y libera la copia
                                                                             // uploads to the GPU and releases the copy
            return tex;
        }

        // ---------------------------------------------------------------
        // Material: copia de un material URP Lit del juego con la textura del modelo
        // Material: copy of a game URP Lit material with the model's texture
        // ---------------------------------------------------------------
        private static readonly string[] ShadersPreferidos = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit" };

        // ¿Sirve este shader para pintar? (P-D) No si no existe (destruido con su bundle), si la tarjeta no lo soporta o si
        // Unity ya lo ha cambiado por el de error (el rosa)
        // Is this shader usable for rendering? (P-D) No if it doesn't exist (destroyed with its bundle), if the graphics card doesn't support it or if
        // Unity has already replaced it with the error one (the pink one)
        private static bool ShaderValido(Shader sh) =>
            sh != null && sh.isSupported && sh.name != "Hidden/InternalErrorShader";

        private static Material CrearMaterial(Cargado c, Il2CppArrayBase<Renderer> renderers, SkinnedMeshRenderer principal, int heroID)
        {
            Material plantilla = null;
            // 1) un material URP Lit de la propia figura (Tokugawa lo usa en el sombrero y el pelo)
            // 1) a URP Lit material from the figure itself (Tokugawa uses it on the hat and the hair)
            for (int i = 0; plantilla == null && i < renderers.Length; i++)
            {
                var mats = renderers[i]?.sharedMaterials;
                for (int j = 0; mats != null && j < mats.Length && plantilla == null; j++)
                    if (mats[j] != null && ShaderValido(mats[j].shader) && Array.IndexOf(ShadersPreferidos, mats[j].shader.name) >= 0) plantilla = mats[j];
            }
            Material mat;
            if (plantilla != null) mat = new Material(plantilla);
            else
            {
                // 2) el shader URP Lit por nombre; 3) si no está cargado, una copia del material de la figura prestada
                // 2) the URP Lit shader by name; 3) if it isn't loaded, a copy of the borrowed figure's material
                Shader sh = null;
                foreach (string n in ShadersPreferidos) { sh = Shader.Find(n); if (ShaderValido(sh)) break; sh = null; }
                if (sh != null) mat = new Material(sh);
                else if (principal != null && principal.sharedMaterial != null && ShaderValido(principal.sharedMaterial.shader))
                {
                    mat = new Material(principal.sharedMaterial);
                    AvisoUnaVez("shader" + heroID, $"Modelo del héroe {heroID}: no hay shader URP Lit cargado; se usa una copia del material de la figura prestada ({principal.sharedMaterial.shader?.name}) y los colores pueden salir raros.");
                }
                // Revisión del 18-10, ronda 2 (P-D): sin ningún shader válido no se pone el modelo (saldría rosa); Aplicar
                // lo recoge, avisa una vez y deja la figura prestada
                // Review of 18-10, round 2 (P-D): without any valid shader the model is not applied (it would show up pink); Aplicar
                // picks it up, warns once and leaves the borrowed figure
                else throw new InvalidOperationException("no hay ningún shader válido (URP Lit ni el de la figura prestada); se deja la figura prestada");
            }
            // Comprobación final (P-D): si la copia hubiera quedado sin shader usable, fuera el material y figura prestada
            // Final check (P-D): if the copy ended up without a usable shader, drop the material and keep the borrowed figure
            if (!ShaderValido(mat.shader))
            {
                UnityEngine.Object.Destroy(mat);
                throw new InvalidOperationException("el material creado no tiene un shader válido; se deja la figura prestada");
            }
            mat.name = "zeroModeloMat" + heroID;
            mat.hideFlags = HideFlags.HideAndDontSave;
            // Textura de color en todas las ranuras de color conocidas; fuera los mapas de la figura prestada (otras UV)
            // Color texture in all known color slots; the borrowed figure's maps are removed (different UVs)
            foreach (string p in new[] { "_BaseMap", "_MainTex", "_Texture", "_ColorMap", "_Albedo_Map" })
                if (mat.HasProperty(p))
                {
                    mat.SetTexture(p, c.Textura);
                    // Revisión del 18-10 (color del héroe 904): sin el mosaico/desplazamiento que pudiera traer la plantilla
                    // Review of 18-10 (hero 904 color): without the tiling/offset the template might carry
                    mat.SetTextureScale(p, Vector2.one);
                    mat.SetTextureOffset(p, Vector2.zero);
                }
            foreach (string p in new[] { "_BumpMap", "_MetallicGlossMap", "_SpecGlossMap", "_OcclusionMap", "_EmissionMap", "_DetailAlbedoMap", "_DetailNormalMap", "_ParallaxMap", "_DetailMask" })
                if (mat.HasProperty(p)) mat.SetTexture(p, null);
            foreach (string p in new[] { "_BaseColor", "_Color" })
                if (mat.HasProperty(p)) mat.SetColor(p, Color.white);
            // Revisión del 18-10 (color del héroe 904): la plantilla de Tokugawa trae _EmissionColor casi blanco (0,988); con
            // _EMISSION apagado no emite, pero se pone negro por si algo volviera a encender la emisión
            // Review of 18-10 (hero 904 color): Tokugawa's template has an almost white _EmissionColor (0.988); with
            // _EMISSION off it doesn't emit, but it is set to black in case something turns emission back on
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            // Mate como los materiales de los héroes de serie (Tokugawa lleva 0): con brillo, el reflejo del entorno de la
            // taberna tiñe la piel (antes 0,25)
            // Matte like the stock heroes' materials (Tokugawa has 0): with gloss, the reflection of the tavern's
            // environment tints the skin (previously 0.25)
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
            // Opaco aunque la plantilla fuera transparente (cola de render, mezcla y escritura de profundidad)
            // Opaque even if the template was transparent (render queue, blending and depth write)
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", 1f);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 2000;
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            foreach (string k in new[] { "_NORMALMAP", "_EMISSION", "_METALLICSPECGLOSSMAP", "_SPECGLOSSMAP", "_OCCLUSIONMAP", "_ALPHATEST_ON", "_PARALLAXMAP", "_DETAIL_MULX2" })
                mat.DisableKeyword(k);
            Plugin.Info($"Modelo del héroe {heroID}: material «{mat.shader?.name}» ({(plantilla != null ? "copia de " + plantilla.name : "nuevo")}).");
            return mat;
        }

        // ---------------------------------------------------------------
        // Cambio de malla en una instancia del prefab prestado
        // Mesh swap on an instance of the borrowed prefab
        // ---------------------------------------------------------------
        /// <summary>
        /// Pone el modelo propio del héroe <paramref name="heroID"/> en una instancia del prefab de su héroe prestado (la
        /// que ha creado el juego). true si queda puesto (o ya lo estaba). Nunca lanza: si algo no cuadra, deja la figura
        /// prestada tal cual y lo escribe una vez en el log. <paramref name="sitio"/> solo sirve para el log.
        /// Applies hero <paramref name="heroID"/>'s custom model to an instance of its borrowed hero's prefab (the
        /// one the game created). true if it ends up applied (or already was). Never throws: if something doesn't match, it leaves the borrowed
        /// figure as is and writes it once to the log. <paramref name="sitio"/> is only used for the log.
        /// </summary>
        public static bool Aplicar(GameObject instancia, int heroID, string sitio)
        {
            if (instancia == null || !Activo || !FaccionesNuevas.EsHeroeNuevo(heroID)) return false;
            // 0.7.5 (revisión total, hallazgo 10): el hijo y los renderers apagados se recuerdan para deshacerlos si algo falla
            // a medias; si no, el hijo se quedaba colgado y la siguiente llamada lo daba por «puesto» con la figura prestada
            // 0.7.5 (full review, finding 10): the child and the turned-off renderers are remembered so they can be undone if something fails
            // halfway; otherwise the child was left hanging and the next call considered it "applied" with the borrowed figure
            GameObject go = null;
            Renderer[] apagados = null;
            try
            {
                var raiz = instancia.transform;
                if (raiz.Find(NombreHijo) != null) return true;
                // Figura estática (las estatuas del mapa de campaña): se cambia su malla y se deja su material liso
                // Static figure (the campaign map statues): its mesh is replaced and its plain material is kept
                var filtro = instancia.GetComponent<MeshFilter>();
                if (filtro != null && instancia.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                    return AplicarEstatua(filtro, heroID, sitio);
                var c = Cargar(heroID);
                if (c == null) return false;
                int prestado = CampanaHeroes.Prestado(heroID);
                if (prestado > 0 && c.Datos.Prestado != prestado)
                {
                    AvisoUnaVez("prestado" + heroID, $"Modelo del héroe {heroID}: está ligado al esqueleto del héroe {c.Datos.Prestado} pero su figura prestada es la del {prestado}; se ve la figura prestada (hay que volver a ligar el modelo con ligar_heroe.py).");
                    return false;
                }

                // Huesos del prefab por ruta relativa a la raíz de la instancia
                // Prefab bones by path relative to the instance root
                var transforms = instancia.GetComponentsInChildren<Transform>(true);
                var rutas = new List<string>(transforms.Length);
                for (int i = 0; i < transforms.Length; i++) rutas.Add(Ruta(transforms[i], raiz));
                int[] indices = FormatoModeloHeroe.Resolver(c.Datos, rutas, out var faltan);
                if (faltan.Count > 0)
                {
                    AvisoUnaVez("huesos" + heroID + sitio, $"Modelo del héroe {heroID} ({sitio}): la figura «{instancia.name}» no tiene {faltan.Count} huesos del modelo " +
                                $"({string.Join(", ", faltan.GetRange(0, Math.Min(4, faltan.Count)))}...); se ve la figura prestada.");
                    return false;
                }
                var huesos = new Transform[indices.Length];
                for (int b = 0; b < indices.Length; b++) huesos[b] = transforms[indices[b]];

                // Renderer principal de la figura prestada: el SkinnedMeshRenderer visible con más huesos (capa y sombras)
                // Main renderer of the borrowed figure: the visible SkinnedMeshRenderer with the most bones (layer and shadows)
                var renderers = instancia.GetComponentsInChildren<Renderer>(true);
                SkinnedMeshRenderer principal = null;
                int mejor = -1;
                for (int i = 0; i < renderers.Length; i++)
                {
                    var s = renderers[i]?.TryCast<SkinnedMeshRenderer>();
                    if (s == null || !s.enabled || !s.gameObject.activeInHierarchy) continue;
                    int nb = s.bones == null ? 0 : s.bones.Length;
                    if (nb > mejor) { mejor = nb; principal = s; }
                }
                // Revisión del 18-10, ronda 2 (P-D): el material cacheado puede venir de una plantilla del bundle del héroe
                // prestado; si ese bundle se liberó, su shader ya no existe (saldría rosa). Se vuelve a crear con los
                // renderers de esta figura, que vienen de un bundle recién cargado
                // Review of 18-10, round 2 (P-D): the cached material may come from a template in the borrowed hero's
                // bundle; if that bundle was released, its shader no longer exists (it would render pink). It is recreated with the
                // renderers of this figure, which come from a freshly loaded bundle
                if (c.Material != null && !ShaderValido(c.Material.shader))
                {
                    AvisoUnaVez("rehacerMat" + heroID, $"Modelo del héroe {heroID}: el material guardado se quedó sin shader válido; se vuelve a crear.");
                    c.Material = null;
                }
                if (c.Material == null) c.Material = CrearMaterial(c, renderers, principal, heroID);

                // Revisión del 18-10 (pose T): los Animator de los cuatro prefabs prestados vienen con cullingMode =
                // CullUpdateTransforms (leído del bundle con UnityPy). Con ese modo solo escriben los huesos si alguno de
                // SUS renderers se ve, y esa lista la cogen al enlazarse (antes de que exista nuestro hijo). Al apagar los
                // renderers prestados, el Animator se cree invisible y deja los huesos en la pose del prefab (la T). Se
                // pasa a AlwaysAnimate ANTES de tocar nada más (si algo falla después, la figura prestada se queda como
                // estaba salvo ese modo, que con sus renderers encendidos no cambia nada de lo que se ve). Sin Rebind ni
                // apagar/encender el Animator: reiniciaría la máquina de estados (la animación «HeroPopIn» de la pantalla)
                // Review of 18-10 (T-pose): the Animators of the four borrowed prefabs come with cullingMode =
                // CullUpdateTransforms (read from the bundle with UnityPy). In that mode they only write the bones if any of
                // THEIR renderers is visible, and they take that list when they link up (before our child exists). When the
                // borrowed renderers are turned off, the Animator thinks it is invisible and leaves the bones in the prefab pose (the T). It is
                // switched to AlwaysAnimate BEFORE touching anything else (if something fails afterwards, the borrowed figure stays as
                // it was except for that mode, which with its renderers on changes nothing of what is visible). No Rebind or
                // turning the Animator off/on: it would restart the state machine (the "HeroPopIn" animation of the screen)
                string estadoAnimador = AnimarSiempre(instancia);

                // Hijo nuevo con el renderer del modelo propio (se monta apagado y se enciende al final)
                // New child with the renderer of our own model (it is mounted disabled and enabled at the end)
                go = new GameObject(NombreHijo);
                go.SetActive(false);
                go.layer = principal != null ? principal.gameObject.layer : instancia.layer;
                go.transform.SetParent(raiz, false);
                var smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.bones = huesos;
                smr.rootBone = huesos[c.Datos.Raiz];
                smr.sharedMesh = c.Malla;
                smr.sharedMaterial = c.Material;
                smr.updateWhenOffscreen = true;                              // sin recortes por los límites del reposo
                                                                             // no clipping from the rest-pose bounds
                smr.quality = SkinQuality.Bone4;                             // las 4 influencias aunque el juego use 2
                                                                             // all 4 influences even if the game uses 2
                if (principal != null)
                {
                    smr.shadowCastingMode = principal.shadowCastingMode;
                    smr.receiveShadows = principal.receiveShadows;
                }
                go.SetActive(true);

                // Fuera la figura prestada: se apagan sus renderers (no los nodos)
                // Out with the borrowed figure: its renderers are turned off (not the nodes)
                apagados = renderers;
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = false;
                _aplicados++;
                Plugin.Info($"Modelo propio del héroe {heroID} puesto en {sitio} (figura «{instancia.name}», {renderers.Length} renderers prestados apagados).");
                // Diagnóstico (una vez por figura): estado del Animator y si los primeros huesos son de esta instancia;
                // RevisarDiagnosticos dirá después si los huesos se mueven de verdad
                // (en su propio try: el modelo ya está puesto y un fallo aquí no debe contarse como «no se pudo poner»)
                // Diagnostics (once per figure): Animator state and whether the first bones belong to this instance;
                // RevisarDiagnosticos will say later whether the bones really move
                // (in its own try: the model is already in place and a failure here must not count as "could not be set")
                try
                {
                    var deLaInstancia = new List<string>();
                    for (int b = 0; b < Math.Min(4, huesos.Length); b++)
                        deLaInstancia.Add($"{huesos[b].name} {(huesos[b].IsChildOf(raiz) ? "sí" : "NO")}");
                    Plugin.Info($"Modelo del héroe {heroID} ({sitio}): {estadoAnimador}; huesos 0-3 de la instancia: {string.Join(", ", deLaInstancia)}.");
                    EmpezarDiagnostico(heroID, sitio, huesos);
                }
                catch (Exception e) { AvisoUnaVez("diag" + heroID + sitio, $"Modelo del héroe {heroID} ({sitio}): diagnóstico sin terminar ({e.GetType().Name}: {e.Message})."); }
                return true;
            }
            catch (Exception e)
            {
                AvisoUnaVez("aplicar" + heroID + sitio, $"Modelo del héroe {heroID} ({sitio}): {e.GetType().Name}: {e.Message}; se ve la figura prestada.");
                // 0.7.5: se deshace lo puesto a medias (el hijo se saca de la figura antes de destruirlo, para que Find no lo
                // encuentre en este mismo fotograma) y se vuelven a encender los renderers prestados
                // 0.7.5: whatever was half-applied is undone (the child is removed from the figure before destroying it, so that Find does not
                // find it in this same frame) and the borrowed renderers are turned back on
                try
                {
                    if (go != null) { go.SetActive(false); go.transform.SetParent(null, false); UnityEngine.Object.Destroy(go); }
                    if (apagados != null) for (int i = 0; i < apagados.Length; i++) if (apagados[i] != null) apagados[i].enabled = true;
                }
                catch { }
                return false;
            }
        }

        // Estatua del mapa: la malla estática del héroe nuevo en el MeshFilter de la figura (mismo material del juego)
        // Map statue: the static mesh of the new hero in the figure's MeshFilter (same material as the game)
        private static bool AplicarEstatua(MeshFilter filtro, int heroID, string sitio)
        {
            var estatua = CargarEstatua(heroID);
            if (estatua == null) return false;
            if (filtro.sharedMesh != null && filtro.sharedMesh.Pointer == estatua.Pointer) return true;
            filtro.sharedMesh = estatua;
            _aplicados++;
            Plugin.Info($"Estatua propia del héroe {heroID} puesta en {sitio} (figura «{filtro.gameObject.name}»).");
            return true;
        }

        // ---------------------------------------------------------------
        // Animator de la figura prestada: animar siempre y diagnóstico
        // Animator of the borrowed figure: always animate and diagnostics
        // ---------------------------------------------------------------
        // Pone cullingMode = AlwaysAnimate en todos los Animator de la figura (los apagados también, por si el juego los
        // enciende luego) y devuelve su estado para el log: encendido, modo antes -> después, controlador, humanoide y
        // playables enlazados
        // Sets cullingMode = AlwaysAnimate on all the figure's Animators (the disabled ones too, in case the game
        // enables them later) and returns their state for the log: enabled, mode before -> after, controller, humanoid and
        // linked playables
        private static string AnimarSiempre(GameObject instancia)
        {
            var animadores = instancia.GetComponentsInChildren<Animator>(true);
            if (animadores == null || animadores.Length == 0) return "sin Animator (no se anima)";
            var partes = new List<string>();
            for (int i = 0; i < animadores.Length; i++)
            {
                var a = animadores[i];
                if (a == null) continue;
                var antes = a.cullingMode;
                if (antes != AnimatorCullingMode.AlwaysAnimate) a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var ctrl = a.runtimeAnimatorController;
                partes.Add($"Animator «{a.gameObject.name}» encendido {(a.isActiveAndEnabled ? "sí" : "NO")}, cullingMode {antes} -> {a.cullingMode}, " +
                           $"controlador «{(ctrl != null ? ctrl.name : "ninguno")}», humanoide {(a.isHuman ? "sí" : "no")}, " +
                           $"avatar «{(a.avatar != null ? a.avatar.name : "ninguno")}», playables enlazados {(a.hasBoundPlayables ? "sí" : "no")}");
            }
            return string.Join(" | ", partes);
        }

        // Prueba de «¿se mueven los huesos?» de una figura recién cambiada: dos fotos de las rotaciones locales separadas
        // medio segundo (la primera un rato después de poner el modelo, para no contar el salto de la T a la primera pose)
        // Test of "are the bones moving?" for a freshly swapped figure: two snapshots of the local rotations half a
        // second apart (the first a while after setting the model, so as not to count the jump from the T-pose to the first pose)
        private sealed class Diagnostico
        {
            public int HeroID;
            public string Sitio;
            public Transform[] Huesos;
            public Quaternion[] Foto;          // null hasta la primera foto
                                               // null until the first snapshot
            public Vector3[] FotoPos;          // posiciones locales de la primera foto (la cadera también se desplaza)
                                               // local positions of the first snapshot (the hip also moves)
            public float Hora;                 // cuándo toca la siguiente foto (tiempo sin escalar)
                                               // when the next snapshot is due (unscaled time)
        }

        private static readonly List<Diagnostico> _diagnosticos = new List<Diagnostico>();
        private const float EsperaPrimeraFoto = 0.3f;
        private const float EsperaSegundaFoto = 0.5f;
        private const float AnguloMinimo = 0.1f;   // grados: menos que esto en todos los huesos es «quieto»...
                                                   // degrees: less than this on all bones means "still"...
        private const float DesplazamientoMinimo = 0.0005f;   // ...y menos que esto (unidades locales) en su posición
                                                              // ...and less than this (local units) in their position

        private static void EmpezarDiagnostico(int heroID, string sitio, Transform[] huesos)
        {
            if (_diagnosticos.Count >= 16) return;  // tope: nunca se acumulan
                                                    // cap: they never pile up
            _diagnosticos.Add(new Diagnostico { HeroID = heroID, Sitio = sitio, Huesos = huesos, Hora = Time.unscaledTime + EsperaPrimeraFoto });
        }

        /// <summary>
        /// Lo llama el vigilante en cada LateUpdate (después de que el Animator haya escrito los huesos): toma las fotos de
        /// los diagnósticos pendientes y escribe una vez por figura «huesos animándose: sí/no» con el giro máximo.
        /// Called by the watcher on every LateUpdate (after the Animator has written the bones): takes the snapshots of
        /// the pending diagnostics and writes once per figure "bones animating: yes/no" with the maximum rotation.
        /// </summary>
        public static void RevisarDiagnosticos()
        {
            for (int i = _diagnosticos.Count - 1; i >= 0; i--)
            {
                var d = _diagnosticos[i];
                if (Time.unscaledTime < d.Hora) continue;
                try
                {
                    if (d.Foto == null)
                    {
                        d.Foto = new Quaternion[d.Huesos.Length];
                        d.FotoPos = new Vector3[d.Huesos.Length];
                        for (int b = 0; b < d.Huesos.Length; b++)
                        {
                            if (d.Huesos[b] == null) { d.Foto[b] = Quaternion.identity; continue; }
                            d.Foto[b] = d.Huesos[b].localRotation;
                            d.FotoPos[b] = d.Huesos[b].localPosition;
                        }
                        d.Hora = Time.unscaledTime + EsperaSegundaFoto;
                        continue;
                    }
                    // Giro y desplazamiento máximos entre las dos fotos (en cualquier hueso)
                    // Maximum rotation and displacement between the two snapshots (in any bone)
                    float maximo = 0f, maxPos = 0f;
                    string cual = "";
                    for (int b = 0; b < d.Huesos.Length; b++)
                    {
                        if (d.Huesos[b] == null) continue;
                        float ang = Quaternion.Angle(d.Foto[b], d.Huesos[b].localRotation);
                        if (ang > maximo) { maximo = ang; cual = d.Huesos[b].name; }
                        maxPos = Math.Max(maxPos, Vector3.Distance(d.FotoPos[b], d.Huesos[b].localPosition));
                    }
                    bool mueve = maximo > AnguloMinimo || maxPos > DesplazamientoMinimo;
                    Plugin.Info($"Modelo del héroe {d.HeroID} ({d.Sitio}): huesos animándose: {(mueve ? "sí" : "NO")} " +
                                $"(giro máximo {maximo:0.##}° en «{cual}», desplazamiento máximo {maxPos:0.####} en {EsperaSegundaFoto:0.#} s).");
                }
                catch (Exception e)
                {
                    // Figura destruida entre fotos (cambio de pantalla): no hay nada que medir
                    // Figure destroyed between snapshots (screen change): there is nothing to measure
                    AvisoUnaVez("diagFoto" + d.HeroID + d.Sitio, $"Modelo del héroe {d.HeroID} ({d.Sitio}): diagnóstico de huesos sin terminar ({e.GetType().Name}).");
                }
                _diagnosticos.RemoveAt(i);
            }
        }

        // Ruta de un Transform relativa a la raíz («Root/Hips/Spine_01»; la raíz es «»). null si no cuelga de la raíz
        // Path of a Transform relative to the root ("Root/Hips/Spine_01"; the root is ""). null if it does not hang from the root
        private static string Ruta(Transform t, Transform raiz)
        {
            var partes = new List<string>();
            while (t != null && t.Pointer != raiz.Pointer)
            {
                partes.Add(t.name);
                t = t.parent;
            }
            if (t == null) return null;
            partes.Reverse();
            return string.Join("/", partes);
        }

        // ---------------------------------------------------------------
        // Registro del vigilante (lo llama Plugin.cs con las facciones nuevas activas)
        // Watcher registration (called by Plugin.cs with the active new factions)
        // ---------------------------------------------------------------
        /// <summary>Registra el componente vigilante en IL2CPP (una vez).</summary>
        /// Registers the watcher component in IL2CPP (once).
        public static void RegistrarTipos()
        {
            if (!Il2CppInterop.Runtime.Injection.ClassInjector.IsTypeRegisteredInIl2Cpp<VigiaModeloHeroes>())
                Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<VigiaModeloHeroes>();
        }
    }

    // -----------------------------------------------------------------------
    // Sección: componente que vigila las figuras de héroe que crea el juego
    // Section: component that watches the hero figures the game creates
    // -----------------------------------------------------------------------
    /// <summary>
    /// En LateUpdate (después de que el juego haya instanciado la figura en su Update o en la continuación de su carga
    /// asíncrona) mira las figuras de héroe de la pantalla de héroes, la Colección, la taberna de la partida y el mapa, y
    /// a cada figura nueva de un héroe 9xx le pone su modelo una vez. Las pantallas se buscan cada medio segundo mientras
    /// no se tienen; mirar sus campos en cada fotograma es barato.
    /// In LateUpdate (after the game has instantiated the figure in its Update or in the continuation of its async
    /// loading) it looks at the hero figures on the hero screen, the Collection, the run's tavern and the map, and
    /// puts its model once on each new figure of a 9xx hero. The screens are searched every half second while
    /// they are not held; looking at their fields every frame is cheap.
    /// </summary>
    public class VigiaModeloHeroes : MonoBehaviour
    {
        public VigiaModeloHeroes(IntPtr ptr) : base(ptr) { }

        private PlayPanel _panel;
        private CollectionPreviewRig _coleccion;
        private GamePlayerLoader _taberna;
        private PlayerToken _ficha;
        private float _siguienteBusqueda;
        private int _fallosSeguidos;
        private bool _parado;
        // Figuras ya miradas (id de instancia de Unity), con o sin cambio: no se vuelven a mirar
        // Figures already examined (Unity instance id), changed or not: they are not examined again
        private readonly HashSet<int> _vistas = new HashSet<int>();

        private void LateUpdate()
        {
            if (_parado || !ModeloHeroes.Activo) return;
            try
            {
                // Diagnóstico de animación de las figuras ya cambiadas (nunca lanza: cada uno lleva su try)
                // Animation diagnostics of the figures already changed (never throws: each one has its own try)
                ModeloHeroes.RevisarDiagnosticos();
                if (Time.unscaledTime >= _siguienteBusqueda)
                {
                    _siguienteBusqueda = Time.unscaledTime + 0.5f;
                    if (_panel == null) _panel = Buscar<PlayPanel>();
                    if (_coleccion == null) _coleccion = Buscar<CollectionPreviewRig>();
                    if (_taberna == null) _taberna = Buscar<GamePlayerLoader>();
                    if (_ficha == null) _ficha = Buscar<PlayerToken>();
                }
                // 1) Pantalla «Seleccionar al héroe»
                // 1) "Seleccionar al héroe" screen
                if (_panel != null) Mirar(_panel.heroObject, _panel.hero == null ? -1 : _panel.hero.HeroID, "la pantalla de héroes");
                // 2) Colección, pestaña Héroes («hero:<id>»)
                // 2) Collection, Heroes tab ("hero:<id>")
                if (_coleccion != null)
                {
                    var modelo = _coleccion._model;
                    string id = _coleccion._shownId;
                    // Mientras carga, _shownId ya es el nuevo pero _model sigue siendo la figura anterior: se espera.
                    // En la 1.11.1 el id es «hero:<id>:<bloqueado>» (prueba de Zero del 03-11-2026: con el int.TryParse de
                    // todo lo de detrás de «hero:» no se cambiaba nunca la figura de la Colección)
                    // While loading, _shownId is already the new one but _model is still the previous figure: wait.
                    // In 1.11.1 the id is «hero:<id>:<bloqueado>» (Zero's test of 03-11-2026: with the int.TryParse of
                    // everything after «hero:», the Collection figure was never changed)
                    if (!_coleccion._loading && modelo != null && LogicaCampanaHeroes.HeroeDeColeccion(id, out int h))
                        Mirar(modelo.gameObject, h, "la Colección");
                }
                // 3) Taberna de la partida: GamePlayerLoader instancia el héroe del save de campaña (o Edric si no hay
                // campaña), así que el id sale de ahí y solo cuando aparece una figura nueva (leer el save cuesta)
                // Run tavern: GamePlayerLoader instantiates the hero from the campaign save (or Edric if there is no
                // campaign), so the id comes from there and only when a new figure appears (reading the save is costly)
                if (_taberna != null)
                {
                    var figura = _taberna.playerHeroGameObject;
                    if (figura != null && !_vistas.Contains(figura.GetInstanceID())) Mirar(figura, HeroeDelSave(), "la taberna de la partida");
                }
                // 4) Mapa de campaña: la figura encendida del héroe activo (es lo que lee PlayerToken.LoadHero)
                // Campaign map: the enabled figure of the active hero (it is what PlayerToken.LoadHero reads)
                if (_ficha != null)
                {
                    var gestor = HeroBonusManager.InstanceIfExists;
                    int activo = gestor == null ? -1 : gestor.ActiveHeroID;
                    if (FaccionesNuevas.EsHeroeNuevo(activo))
                    {
                        // Revisión del 18-10, ronda 2 (B-5): solo la figura del héroe ACTIVO (el hueco heroPrefabs[id - 1]
                        // que enciende PlayerToken.LoadHero). Antes se miraban todas las encendidas y otra figura por
                        // defecto del escenario podía quedarse con la estatua del 9xx
                        // Review of 18-10, round 2 (B-5): only the ACTIVE hero's figure (the heroPrefabs[id - 1] slot
                        // that PlayerToken.LoadHero enables). Before, all enabled ones were examined and another default
                        // figure in the scene could end up with the 9xx statue
                        var figuras = _ficha.heroPrefabs;
                        int hueco = activo - 1;
                        if (figuras != null && hueco >= 0 && hueco < figuras.Length && figuras[hueco] != null && figuras[hueco].activeSelf)
                            Mirar(figuras[hueco], activo, "el mapa de campaña");
                    }
                }
                if (_vistas.Count > 2000) _vistas.Clear();             // las figuras viejas ya no existen
                                                                       // the old figures no longer exist
                _fallosSeguidos = 0;
            }
            catch (Exception e)
            {
                // Un fallo suelto (escena cambiando) se ignora; muchos seguidos paran el vigilante
                // A single failure (scene changing) is ignored; many in a row stop the watcher
                if (++_fallosSeguidos >= 50)
                {
                    _parado = true;
                    Plugin.Aviso("Modelos de héroe: el vigilante falla una y otra vez y se para (" + e.GetType().Name + ": " + e.Message + "); se ven las figuras prestadas.");
                }
            }
        }

        // Pone el modelo en una figura si es nueva y de un héroe 9xx (una sola vez por figura)
        // Puts the model on a figure if it is new and belongs to a 9xx hero (only once per figure)
        private void Mirar(GameObject figura, int heroID, string sitio)
        {
            if (figura == null) return;
            int id = figura.GetInstanceID();
            if (_vistas.Contains(id)) return;
            _vistas.Add(id);
            if (!FaccionesNuevas.EsHeroeNuevo(heroID)) return;
            ModeloHeroes.Aplicar(figura, heroID, sitio);
        }

        // Héroe de la partida de campaña como lo elige GamePlayerLoader: la partida en memoria o, si no, el save de
        // campaña (ya saneado por el núcleo: un 9xx con su módulo activo vuelve a ser él); -1 si no hay campaña
        // Hero of the campaign run as GamePlayerLoader chooses it: the run in memory or, failing that, the campaign
        // save (already sanitized by the core: a 9xx with its module active becomes itself again); -1 if there is no campaign
        private static int HeroeDelSave()
        {
            try
            {
                var datos = CampaignManager.InstanceIfExists?.CampaignSaveManager?.SaveData;
                if (datos != null) return datos.heroID;
                if (!Memori.SaveData.SaveDataHandler.CampaignSaveExists()) return -1;
                var guardado = Memori.SaveData.SaveDataHandler.Load();
                return guardado == null ? -1 : guardado.heroID;
            }
            catch { return -1; }
        }

        // Primer objeto activo de un tipo en las escenas cargadas (null si no hay)
        // First active object of a type in the loaded scenes (null if none)
        private static T Buscar<T>() where T : UnityEngine.Object
        {
            var o = UnityEngine.Object.FindFirstObjectByType(Il2CppType.Of<T>());
            return o == null ? null : o.TryCast<T>();
        }
    }
}
