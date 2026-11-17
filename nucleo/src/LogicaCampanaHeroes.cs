// LogicaCampanaHeroes.cs
// Cálculos puros (sin tipos del juego ni de Unity) de CampanaHeroes.cs, para poder probarlos fuera del juego
// (proyecto pruebas\, PruebasCampana.cs):
//   - qué figura del mapa de campaña se enciende para un héroe (PlayerToken.heroPrefabs va de 1 a N por id de héroe y
//     los héroes nuevos 901-904 se salen del array: se usa su héroe prestado o, si tampoco cabe, el de por defecto);
//   - hasta dónde se amplía heroPrefabs y qué figura se copia en cada hueco nuevo (LongitudNecesaria, OrigenFigura: el
//     arreglo que funciona aunque LoadHero esté inlineado dentro de MapSceneManager.CompleteLoad);
//   - qué id de héroe se pide a los assets cuando el pedido no tiene entrada (héroe nuevo con su módulo apagado);
//   - cómo se suma al contador «hechas/total» del botón Colección lo de las razas nuevas;
//   - qué héroe enseña la vista 3D de la Colección (id «hero:<id>:<bloqueado>» de la 1.11.1, HeroeDeColeccion).
// Autor: ZeroitMods.
// LogicaCampanaHeroes.cs
// Pure calculations (no game or Unity types) from CampanaHeroes.cs, so they can be tested outside the game
// (proyecto pruebas\, PruebasCampana.cs):
//   - which campaign map figure lights up for a hero (PlayerToken.heroPrefabs goes from 1 to N by hero id and
//     the new heroes 901-904 fall outside the array: their borrowed hero is used or, if that does not fit either, the default one);
//   - how far heroPrefabs is extended and which figure is copied into each new slot (LongitudNecesaria, OrigenFigura: the
//     fix that works even if LoadHero is inlined inside MapSceneManager.CompleteLoad);
//   - which hero id is requested from the assets when the request has no entry (new hero with its module turned off);
//   - how the new races are added to the «done/total» counter of the Collection button;
//   - which hero the Collection 3D view shows (id «hero:<id>:<bloqueado>» from 1.11.1, HeroeDeColeccion).
// Author: ZeroitMods.

using System;
using System.Text.RegularExpressions;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: lógica pura de los héroes nuevos fuera de su pantalla
    // Section: pure logic of the new heroes outside their screen
    // -----------------------------------------------------------------------
    public static class LogicaCampanaHeroes
    {
        // ¿Cabe el id (empieza en 1) en un array de «longitud» figuras?
        // Does the id (starting at 1) fit in an array of «longitud» figures?
        private static bool EnRango(int id, int longitud) => id >= 1 && id <= longitud;

        // Héroe prestado sin dejar que un fallo de la tabla tumbe el cálculo (-1 = sin prestado)
        // Borrowed hero without letting a table failure bring down the calculation (-1 = no borrowed hero)
        private static int PrestadoSeguro(Func<int, int> prestadoDe, int heroID)
        {
            if (prestadoDe == null) return -1;
            try { return prestadoDe(heroID); }
            catch { return -1; }
        }

        /// <summary>
        /// Índice (desde 0) de la figura del mapa para un héroe, como PlayerToken.LoadHero (heroPrefabs[heroID - 1], con
        /// -1 = héroe 1). Si el héroe no cabe en el array: su prestado; si tampoco cabe: <paramref name="porDefecto"/>; si
        /// tampoco: la primera figura. -1 solo si no hay ninguna figura.
        /// Index (from 0) of the map figure for a hero, like PlayerToken.LoadHero (heroPrefabs[heroID - 1], with
        /// -1 = hero 1). If the hero does not fit in the array: its borrowed hero; if that does not fit either: <paramref name="porDefecto"/>; if
        /// not, the first figure. -1 only if there are no figures.
        /// </summary>
        public static int IndiceFigura(int heroID, int longitud, Func<int, int> prestadoDe, int porDefecto)
        {
            if (longitud <= 0) return -1;
            if (heroID == -1) heroID = 1;                       // mismo respaldo que el juego
                                                                // same fallback as the game
            if (EnRango(heroID, longitud)) return heroID - 1;
            int p = PrestadoSeguro(prestadoDe, heroID);
            if (EnRango(p, longitud)) return p - 1;
            if (EnRango(porDefecto, longitud)) return porDefecto - 1;
            return 0;
        }

        /// <summary>¿Haría falta cambiar de figura? (el juego, tal cual, se saldría del array con este héroe).</summary>
        /// Would the figure need to be changed? (the game, as is, would go out of the array with this hero).
        public static bool FueraDeRango(int heroID, int longitud)
        {
            if (heroID == -1) heroID = 1;
            return !EnRango(heroID, longitud);
        }

        // ---------------------------------------------------------------
        // Array de figuras ampliado (0.7.2, LoadHero inlineado en MapSceneManager.CompleteLoad)
        // Extended figure array (0.7.2, LoadHero inlined in MapSceneManager.CompleteLoad)
        // ---------------------------------------------------------------
        /// <summary>Id de héroe más alto para el que se amplía heroPrefabs (los módulos de Zero usan el rango 9xx).</summary>
        /// Highest hero id for which heroPrefabs is extended (Zero's modules use the 9xx range).
        public const int TopeHeroeMapa = 999;

        /// <summary>
        /// Longitud que necesita heroPrefabs para que el código de LoadHero (heroPrefabs[heroID - 1], con -1 = héroe 1) no se
        /// salga: la misma si ya cabe; el id si no cabe. -1 si el id no se puede arreglar ampliando (0, menor que -1 o por
        /// encima de <see cref="TopeHeroeMapa"/>, para que un save roto no pida un array enorme).
        /// Length heroPrefabs needs so that the LoadHero code (heroPrefabs[heroID - 1], with -1 = hero 1) does not
        /// go out of bounds: the same if it already fits; the id if it does not fit. -1 if the id cannot be fixed by extending (0, less than -1 or
        /// above <see cref="TopeHeroeMapa"/>, so a broken save does not request a huge array).
        /// </summary>
        public static int LongitudNecesaria(int heroID, int longitud)
        {
            if (heroID == -1) heroID = 1;                       // mismo respaldo que el juego
                                                                // same fallback as the game
            if (heroID < 1 || heroID > TopeHeroeMapa) return -1;
            return Math.Max(Math.Max(longitud, 0), heroID);
        }

        /// <summary>
        /// Índice (desde 0) de la figura que se copia en el hueco del héroe <paramref name="heroID"/> de un array de
        /// <paramref name="longitud"/> figuras: la de su prestado si está llena; si no, la de <paramref name="porDefecto"/>; si
        /// no, la primera llena que no sea su propio hueco. -1 si no hay ninguna llena. <paramref name="llena"/> dice si la
        /// posición (desde 0) tiene figura; si falla, cuenta como vacía.
        /// Index (from 0) of the figure copied into the slot of hero <paramref name="heroID"/> in an array of
        /// <paramref name="longitud"/> figures: that of its borrowed hero if filled; otherwise, that of <paramref name="porDefecto"/>; otherwise,
        /// the first filled one that is not its own slot. -1 if none is filled. <paramref name="llena"/> says whether the
        /// position (from 0) has a figure; if it fails, it counts as empty.
        /// </summary>
        public static int OrigenFigura(int heroID, int longitud, Func<int, bool> llena, Func<int, int> prestadoDe, int porDefecto)
        {
            if (longitud <= 0 || llena == null) return -1;
            if (heroID == -1) heroID = 1;
            bool Llena(int i) { try { return llena(i); } catch { return false; } }
            bool Vale(int id) => EnRango(id, longitud) && id != heroID && Llena(id - 1);
            int p = PrestadoSeguro(prestadoDe, heroID);
            if (Vale(p)) return p - 1;
            if (Vale(porDefecto)) return porDefecto - 1;
            for (int i = 0; i < longitud; i++)
                if (i != heroID - 1 && Llena(i)) return i;
            return -1;
        }

        /// <summary>
        /// Id de héroe que se pide a los assets (retrato y figura): el mismo si tiene entrada; si no, su prestado si ese sí
        /// la tiene; si no, el de por defecto si la tiene; y si nada tiene entrada, el mismo (el juego decide).
        /// Hero id requested from the assets (portrait and figure): the same one if it has an entry; otherwise, its borrowed hero if that one
        /// has it; otherwise, the default one if it has it; and if nothing has an entry, the same one (the game decides).
        /// </summary>
        public static int HeroeConAssets(int heroID, Func<int, bool> tieneEntrada, Func<int, int> prestadoDe, int porDefecto)
        {
            if (tieneEntrada == null) return heroID;
            bool Tiene(int id) { try { return tieneEntrada(id); } catch { return false; } }
            if (Tiene(heroID)) return heroID;
            int p = PrestadoSeguro(prestadoDe, heroID);
            if (p > 0 && p != heroID && Tiene(p)) return p;
            if (porDefecto != heroID && Tiene(porDefecto)) return porDefecto;
            return heroID;
        }

        // Texto del contador que escribe el juego: «hechas/total» (MainMenu.SetCompletionCounter)
        // Counter text written by the game: «done/total» (MainMenu.SetCompletionCounter)
        private static readonly Regex ReContador = new Regex(@"^\s*(\d+)\s*/\s*(\d+)\s*$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Suma al contador «hechas/total» lo de las razas nuevas. false (y sin tocar nada) si el texto no tiene esa forma o
        /// no hay nada que sumar.
        /// Adds the new races to the «done/total» counter. false (and touches nothing) if the text does not have that form or
        /// there is nothing to add.
        /// </summary>
        public static bool SumarAContador(string texto, int masHechas, int masTotal, out int hechas, out int total)
        {
            hechas = 0; total = 0;
            if (string.IsNullOrEmpty(texto) || masTotal <= 0 || masHechas < 0 || masHechas > masTotal) return false;
            var m = ReContador.Match(texto);
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int h) || !int.TryParse(m.Groups[2].Value, out int t)) return false;
            if (h > int.MaxValue - masHechas || t > int.MaxValue - masTotal) return false;     // sin desbordar el contador
                                                                                               // without overflowing the counter
            hechas = h + masHechas;
            total = t + masTotal;
            return true;
        }

        // Actos en los que una raza nueva sale como ejército enemigo (0.7.6, prueba de Zero del 03-11-2026): la lista
        // «actosCampana» del manifiesto si trae alguno válido (1-3); si no, el «actoCampana» de siempre (uno solo)
        // Acts in which a new race appears as an enemy army (0.7.6, Zero's test of 03-11-2026): the manifest's
        // «actosCampana» list if it has any valid one (1-3); otherwise, the usual «actoCampana» (just one)
        /// <summary>¿Sale la raza nueva como enemiga en el acto <paramref name="acto"/>?</summary>
        /// Does the new race appear as an enemy in act <paramref name="acto"/>?
        public static bool SaleEnActo(int acto, int actoCampana, System.Collections.Generic.IEnumerable<int> actosCampana)
        {
            if (acto < 1 || acto > 3) return false;
            bool hayLista = false;
            if (actosCampana != null)
                foreach (int a in actosCampana)
                {
                    if (a < 1 || a > 3) continue;
                    hayLista = true;
                    if (a == acto) return true;
                }
            return !hayLista && actoCampana == acto;
        }

        // Regla del sorteo de facciones enemigas por partida (0.7.6, 2.ª ronda de la revisión): el juego vuelve a sortear la
        // raza enemiga con la semilla en cada batalla (EngagementPanel.PrepareBattle), así que una partida empezada con la
        // 0.7.5 tiene que seguir con la regla de antes (una raza nueva por acto) o cambiaría de enemigo a mitad de acto
        // Rule for drawing enemy factions per run (0.7.6, 2nd round of the review): the game draws the enemy
        // race again with the seed in every battle (EngagementPanel.PrepareBattle), so a run started with
        // 0.7.5 has to keep the previous rule (one new race per act) or the enemy would change mid-act
        public const int ReglaAntigua = 1, ReglaNueva = 2;

        /// <summary>
        /// Regla de una partida: la apuntada si la hay; si no, la nueva solo si la partida acaba de empezar (acto 1 y ninguna
        /// batalla jugada) y la de antes si ya iba empezada cuando llegó la 0.7.6.
        /// Rule of a run: the recorded one if there is one; otherwise, the new one only if the run has just started (act 1 and no
        /// battle played) and the previous one if it was already underway when 0.7.6 arrived.
        /// </summary>
        public static int ReglaActos(int apuntada, int libro, int batallas)
        {
            if (apuntada == ReglaAntigua || apuntada == ReglaNueva) return apuntada;
            return libro <= 1 && batallas <= 0 ? ReglaNueva : ReglaAntigua;
        }

        /// <summary>
        /// Probabilidad de que el mapa de un acto sea de una raza nueva: cada raza nueva pesa lo mismo que cada raza de
        /// serie del acto (sin contar la del jugador). 0 si no hay nuevas.
        /// Probability that an act's map is of a new race: each new race weighs the same as each
        /// standard race of the act (not counting the player's). 0 if there are no new ones.
        /// </summary>
        public static double ProbabilidadNuevas(int deSerie, int nuevas)
        {
            if (nuevas <= 0) return 0;
            if (deSerie < 0) deSerie = 0;
            return nuevas / (double)(deSerie + nuevas);
        }

        // Id del héroe que enseña la vista 3D de la Colección (CollectionPreviewRig._shownId). Prueba de Zero del
        // 03-11-2026: en la 1.11.1 el id es «hero:{0}:{1}» (literal de los metadatos del juego: id del héroe y, detrás,
        // si está bloqueado), no «hero:{0}» como en el código público; el vigilante pedía un número entero tras «hero:»,
        // no lo encontraba y la figura prestada se quedaba sin cambiar. Ahora vale el número hasta los dos puntos
        // Id of the hero shown by the Collection 3D view (CollectionPreviewRig._shownId). Zero's test of
        // 03-11-2026: in 1.11.1 the id is «hero:{0}:{1}» (literal from the game's metadata: hero id and, after it,
        // whether it is locked), not «hero:{0}» as in the public code; the watcher expected an integer after «hero:»,
        // did not find it and the borrowed figure stayed unchanged. Now the number up to the colon is used
        private static readonly Regex ReHeroeColeccion = new Regex(@"^hero:(\d+)(?::.*)?$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Saca el id del héroe de «hero:903», «hero:903:False» o «hero:903:True». false si no es un héroe (unidades,
        /// vacío o null) o el número no cabe en un int.
        /// Extracts the hero id from «hero:903», «hero:903:False» or «hero:903:True». false if it is not a hero (units,
        /// empty or null) or the number does not fit in an int.
        /// </summary>
        public static bool HeroeDeColeccion(string shownId, out int heroID)
        {
            heroID = -1;
            if (string.IsNullOrEmpty(shownId)) return false;
            var m = ReHeroeColeccion.Match(shownId);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int id)) { heroID = id; return true; }
            return false;
        }
    }
}
