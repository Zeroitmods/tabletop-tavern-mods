// Redireccion.cs
// Red de seguridad para la interfaz: si al pedir un asset de una unidad nueva (icono de tipo, bandera, icono, prefab
// de reclutamiento, voces) la función del catálogo lanza una excepción (entrada de assets rota), un FINALIZER se traga
// la excepción y devuelve lo mismo de su UNIDAD BASE (según el manifiesto: la 900 -> Eisenmann, la 944 -> Oni...).
// Así una entrada rota no deja una carta bocabajo ni bloquea la pantalla. Con la entrada válida no hace nada.
// Redireccion.cs
// Safety net for the UI: if, when requesting an asset for a new unit (type icon, flag, icon, recruitment prefab,
// voices), the catalog function throws an exception (broken asset entry), a FINALIZER swallows the
// exception and returns the same thing from its BASE UNIT (according to the manifest: 900 -> Eisenmann, 944 -> Oni...).
// This way a broken entry doesn't leave a card face down or block the screen. With a valid entry it does nothing.
//
// Se usan finalizers (no prefix) para no tocar la llamada normal: solo actúan si el original ya ha fallado, y solo
// sobre funciones que devuelven referencias (Sprite, AudioClip, string).
// Finalizers are used (not prefix) so the normal call isn't touched: they only act if the original has already failed, and only
// on functions that return references (Sprite, AudioClip, string).

using System;
using TJ;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: finalizers de respaldo por tipo de resultado
    // Section: fallback finalizers by result type
    // -----------------------------------------------------------------------
    public static class Redireccion
    {
        private static bool _avisado;

        // Avisa una sola vez de que se ha usado el respaldo
        // Warns only once that the fallback has been used
        private static void Avisar(string metodo, UnitName unidad, Exception e)
        {
            if (_avisado) return;
            _avisado = true;
            Plugin.Aviso($"Respaldo de assets: {metodo}({(int)unidad}) falló ({e.GetType().Name}); se devuelve lo de {UnidadGuardia.BaseDe(unidad)}.");
        }

        // ¿Hay que rescatar esta llamada? Solo si es una unidad nueva y la función original lanzó una excepción
        // Does this call need to be rescued? Only if it is a new unit and the original function threw an exception
        private static bool Rescatar(Exception ex, UnitName unidad) => ex != null && UnidadGuardia.EsNueva(unidad);

        // GetSquadTypeIcon (lo usa RecruitCard.SetUp: era el fallo de la carta bocabajo)
        // GetSquadTypeIcon (used by RecruitCard.SetUp: it was the cause of the face-down card bug)
        public static Exception FinGetSquadTypeIcon(Exception __exception, UnitName _unitName, ref Sprite __result)
        {
            if (!Rescatar(__exception, _unitName)) return __exception;
            try { __result = TabletopTavernData.Instance.GetSquadTypeIcon(UnidadGuardia.BaseDe(_unitName)); Avisar("GetSquadTypeIcon", _unitName, __exception); return null; }
            catch { return __exception; }
        }

        // GetSquadTypeFlagSprite
        public static Exception FinGetSquadTypeFlagSprite(Exception __exception, UnitName _unitName, ref Sprite __result)
        {
            if (!Rescatar(__exception, _unitName)) return __exception;
            try { __result = TabletopTavernData.Instance.GetSquadTypeFlagSprite(UnidadGuardia.BaseDe(_unitName)); Avisar("GetSquadTypeFlagSprite", _unitName, __exception); return null; }
            catch { return __exception; }
        }

        // GetUnitIcon
        public static Exception FinGetUnitIcon(Exception __exception, UnitName _unitName, ref Sprite __result)
        {
            if (!Rescatar(__exception, _unitName)) return __exception;
            try { __result = TabletopTavernData.Instance.GetUnitIcon(UnidadGuardia.BaseDe(_unitName)); Avisar("GetUnitIcon", _unitName, __exception); return null; }
            catch { return __exception; }
        }

        // GetRecruitmentPrefabKey
        public static Exception FinGetRecruitmentPrefabKey(Exception __exception, UnitName _unitName, ref string __result)
        {
            if (!Rescatar(__exception, _unitName)) return __exception;
            try { __result = TabletopTavernData.Instance.GetRecruitmentPrefabKey(UnidadGuardia.BaseDe(_unitName)); Avisar("GetRecruitmentPrefabKey", _unitName, __exception); return null; }
            catch { return __exception; }
        }

        // GetRandomBarkSFX / GetRandomChargeSFX / GetRandomRetreatSFX (voces)
        // GetRandomBarkSFX / GetRandomChargeSFX / GetRandomRetreatSFX (voices)
        public static Exception FinGetRandomBarkSFX(Exception __exception, UnitName _unitName, ref AudioClip __result)
        {
            if (!Rescatar(__exception, _unitName)) return __exception;
            try { __result = TabletopTavernData.Instance.GetRandomBarkSFX(UnidadGuardia.BaseDe(_unitName)); Avisar("GetRandomBarkSFX", _unitName, __exception); return null; }
            catch { return __exception; }
        }
        public static Exception FinGetRandomChargeSFX(Exception __exception, UnitName _unitName, ref AudioClip __result)
        {
            if (!Rescatar(__exception, _unitName)) return __exception;
            try { __result = TabletopTavernData.Instance.GetRandomChargeSFX(UnidadGuardia.BaseDe(_unitName)); Avisar("GetRandomChargeSFX", _unitName, __exception); return null; }
            catch { return __exception; }
        }
        public static Exception FinGetRandomRetreatSFX(Exception __exception, UnitName _unitName, ref AudioClip __result)
        {
            if (!Rescatar(__exception, _unitName)) return __exception;
            try { __result = TabletopTavernData.Instance.GetRandomRetreatSFX(UnidadGuardia.BaseDe(_unitName)); Avisar("GetRandomRetreatSFX", _unitName, __exception); return null; }
            catch { return __exception; }
        }

        // Lista (método del juego, finalizer) que aplica Plugin.cs
        // List (game method, finalizer) that Plugin.cs applies
        public static readonly (string metodo, string finalizer)[] Parches =
        {
            ("GetSquadTypeIcon", nameof(FinGetSquadTypeIcon)),
            ("GetSquadTypeFlagSprite", nameof(FinGetSquadTypeFlagSprite)),
            ("GetUnitIcon", nameof(FinGetUnitIcon)),
            ("GetRecruitmentPrefabKey", nameof(FinGetRecruitmentPrefabKey)),
            ("GetRandomBarkSFX", nameof(FinGetRandomBarkSFX)),
            ("GetRandomChargeSFX", nameof(FinGetRandomChargeSFX)),
            ("GetRandomRetreatSFX", nameof(FinGetRandomRetreatSFX)),
        };
    }
}
