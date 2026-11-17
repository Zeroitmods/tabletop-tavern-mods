// ReglasObjetos.cs
// Convierte los objetos nuevos que lleva el jugador en reglas de bonus de héroe (objetos.md §3.2, ruta c), en el formato
// oficial de hero_bonus_rules.json, para que las cree el propio cargador del juego (HeroBonusRuleOverrideLoader).
//   - heroID = el héroe de la partida; localizationKey = «<id>Name», así la ficha de la unidad enseña el nombre del objeto.
//   - «ListaCalculada» se convierte en una lista de unidades (unitNames numéricos, el cargador los acepta) con la función
//     que le pasa el plugin (que mira el catálogo real del juego en ese momento).
//   - 0.7.2: «UnitName» con su lista de unidades del manifiesto (por ejemplo, el 721 Grog de Hongo también para la 946).
//   - La regla «principal» de cada objeto usa el valor de gear_overrides.json si el mod lo cambia (GearModifierValue).
// .NET puro (System.Text.Json) para poder probarlo fuera del juego.
// ReglasObjetos.cs
// Converts the new gear the player carries into hero bonus rules (objetos.md §3.2, route c), in the official
// format of hero_bonus_rules.json, so that the game's own loader creates them (HeroBonusRuleOverrideLoader).
//   - heroID = the run's hero; localizationKey = «<id>Name», so the unit sheet shows the item's name.
//   - «ListaCalculada» becomes a list of units (numeric unitNames, the loader accepts them) using the function
//     passed by the plugin (which looks at the game's real catalog at that moment).
//   - 0.7.2: «UnitName» with its unit list from the manifest (for example, Hongo's 721 Grog also for 946).
//   - Each item's «main» rule uses the value from gear_overrides.json if the mod changes it (GearModifierValue).
// Pure .NET (System.Text.Json) so it can be tested outside the game.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: JSON de reglas de los objetos que se llevan
    // Section: rules JSON for the carried gear
    // -----------------------------------------------------------------------
    public static class ReglasObjetos
    {
        public const string FiltroListaCalculada = "ListaCalculada";

        /// <summary>
        /// JSON de hero_bonus_rules.json con las reglas de <paramref name="objetos"/> para el héroe <paramref name="heroe"/>.
        /// <paramref name="expandir"/> devuelve las unidades (ids) de una condición ListaCalculada; <paramref name="valorPrincipal"/>
        /// devuelve el valor de la regla principal de un objeto si un mod lo cambia (null = el del manifiesto).
        /// Devuelve también cuántas reglas de stats y de atributo lleva (las que no tienen unidades se omiten).
        /// hero_bonus_rules.json JSON with the rules of <paramref name="objetos"/> for the hero <paramref name="heroe"/>.
        /// <paramref name="expandir"/> returns the units (ids) of a ListaCalculada condition; <paramref name="valorPrincipal"/>
        /// returns the value of an item's main rule if a mod changes it (null = the manifest's).
        /// Also returns how many stats and attribute rules it carries (those without units are omitted).
        /// </summary>
        public static string Json(IEnumerable<Manifiesto.Objeto> objetos, int heroe, Func<Manifiesto.CondicionObjeto, IEnumerable<int>> expandir,
                                  Func<int, int?> valorPrincipal, out int nStats, out int nAtributos)
        {
            var stats = new List<string>();
            var atributos = new List<string>();
            foreach (var o in objetos)
            {
                foreach (var r in o.Reglas)
                {
                    string condicion = Condicion(r.Condicion, expandir);
                    if (condicion == null) continue;      // lista vacía: el cargador la rechazaría
                                                          // empty list: the loader would reject it
                    string comun = "\"heroID\":\"" + heroe.ToString(CultureInfo.InvariantCulture) + "\",\"localizationKey\":" + Texto(o.Id + "Name") + ",\"condition\":" + condicion;
                    if (string.Equals(r.Tipo, "atributo", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(r.Atributo)) continue;
                        atributos.Add("{" + comun + ",\"grantedAttribute\":" + Texto(r.Atributo) + "}");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(r.Stat)) continue;
                        string valor = r.Valor ?? "0";
                        if (r.Principal && valorPrincipal?.Invoke(o.Id) is int v) valor = v.ToString(CultureInfo.InvariantCulture);
                        stats.Add("{" + comun + ",\"stat\":" + Texto(r.Stat) + ",\"magnitudeKind\":" + Texto(string.IsNullOrEmpty(r.Magnitud) ? "Flat" : r.Magnitud) +
                                  ",\"value\":" + Texto(valor) + "}");
                    }
                }
            }
            nStats = stats.Count;
            nAtributos = atributos.Count;
            return "{\"statRules\":[\n" + string.Join(",\n", stats) + "\n],\"attributeRules\":[\n" + string.Join(",\n", atributos) + "\n],\"factionRules\":[]}";
        }

        // Condición en formato del cargador; ListaCalculada pasa a UnitName con la lista expandida (null si queda vacía)
        // Condition in the loader's format; ListaCalculada becomes UnitName with the expanded list (null if it ends up empty)
        private static string Condicion(Manifiesto.CondicionObjeto c, Func<Manifiesto.CondicionObjeto, IEnumerable<int>> expandir)
        {
            string fk = c.FilterKind ?? "Unconditional";
            if (string.Equals(fk, FiltroListaCalculada, StringComparison.OrdinalIgnoreCase))
            {
                var ids = (expandir?.Invoke(c) ?? Enumerable.Empty<int>()).Distinct().OrderBy(i => i).ToList();
                if (ids.Count == 0) return null;
                return "{\"filterKind\":\"UnitName\",\"unitNames\":[" + string.Join(",", ids.Select(i => "\"" + i.ToString(CultureInfo.InvariantCulture) + "\"")) + "]}";
            }
            // 0.7.2: UnitName con la lista del manifiesto, como lo escribe el juego («unitNames»: nombres del enum o números,
            // HeroBonusRuleOverrideLoader los lee con Enum.TryParse). Sin unidades el cargador rechazaría la regla: se omite.
            // 0.7.2: UnitName with the manifest list, as the game writes it («unitNames»: enum names or numbers,
            // HeroBonusRuleOverrideLoader reads them with Enum.TryParse). Without units the loader would reject the rule: it is skipped.
            if (string.Equals(fk, "UnitName", StringComparison.OrdinalIgnoreCase))
            {
                var nombres = (c.UnitNames ?? new List<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct().ToList();
                if (nombres.Count == 0) return null;
                return "{\"filterKind\":\"UnitName\",\"unitNames\":[" + string.Join(",", nombres.Select(Texto)) + "]}";
            }
            var partes = new List<string> { "\"filterKind\":" + Texto(fk) };
            if (!string.IsNullOrEmpty(c.RequiredRarityTier)) partes.Add("\"requiredRarityTier\":" + Texto(c.RequiredRarityTier));
            if (c.UnitTypes != null && c.UnitTypes.Count > 0) partes.Add("\"unitTypes\":[" + string.Join(",", c.UnitTypes.Select(Texto)) + "]");
            if (c.UnitSizes != null && c.UnitSizes.Count > 0) partes.Add("\"unitSizes\":[" + string.Join(",", c.UnitSizes.Select(Texto)) + "]");
            if (!string.IsNullOrEmpty(c.Tag)) partes.Add("\"tag\":" + Texto(c.Tag));
            return "{" + string.Join(",", partes) + "}";
        }

        // Cadena JSON con escapes
        // JSON string with escapes
        private static string Texto(string s) => JsonSerializer.Serialize(s ?? "");

        /// <summary>
        /// ¿Cumple una unidad la condición ListaCalculada? (rareza, tamaño y tipo como texto de los enums del juego;
        /// Y entre campos, O dentro de cada uno; un campo vacío no filtra).
        /// Does a unit meet the ListaCalculada condition? (rarity, size and type as the text of the game's enums;
        /// AND between fields, OR within each one; an empty field does not filter).
        /// </summary>
        public static bool CumpleLista(Manifiesto.CondicionObjeto c, string rareza, string tamano, string tipo)
        {
            bool Dentro(List<string> lista, string v) => lista == null || lista.Count == 0 || lista.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));
            return Dentro(c.Rarezas, rareza) && Dentro(c.Tamanos, tamano) && Dentro(c.Tipos, tipo);
        }
    }
}
