// PruebasJinete.cs
// Prueba 34 (18-11-2026, núcleo 0.7.13): migración de la opción Batalla.JineteCaballeria (LogicaJinete.Migrar), fuera
// del juego. La opción pasa a venir encendida; quien tenga el .cfg de un núcleo anterior la tiene en false (el valor de
// serie de entonces) y BepInEx respeta lo escrito en el fichero. Casos: sin .cfg (valor de serie true, sin marcador),
// .cfg antiguo con false y sin marcador, .cfg antiguo con true, y false puesto por el jugador DESPUÉS de la migración.
// Autor: ZeroitMods.
// PruebasJinete.cs
// Test 34 (18-11-2026, core 0.7.13): migration of the Batalla.JineteCaballeria setting (LogicaJinete.Migrar), outside
// the game. The setting is now on by default; anyone with the .cfg of an older core has it at false (the default
// back then) and BepInEx keeps what is written in the file. Cases: no .cfg (default true, no marker),
// old .cfg with false and no marker, old .cfg with true, and false set by the player AFTER the migration.
// Author: ZeroitMods.

using System;
using ZeroNucleo;

internal static class PruebasJinete
{
    public static void Todas(Action<bool, string> comprobar)
    {
        // Sección: sin .cfg (primera instalación): BepInEx da el valor de serie (true) y el marcador a false
        // Section: no .cfg (first install): BepInEx gives the default (true) and the marker at false
        var nuevo = LogicaJinete.Migrar(true, false);
        comprobar(nuevo.Valor && nuevo.Marcador && !nuevo.Encendida,
                  "jinete: sin .cfg queda encendida, se pone el marcador y no se avisa de ningún cambio");

        // Sección: .cfg de un núcleo anterior con el false de serie y sin marcador: pasa a true una vez
        // Section: .cfg from an older core with the default false and no marker: becomes true once
        var antiguo = LogicaJinete.Migrar(false, false);
        comprobar(antiguo.Valor && antiguo.Marcador && antiguo.Encendida,
                  "jinete: el false antiguo sin marcador pasa a true (una vez) y se avisa en el log");

        // Sección: .cfg anterior con true (puesto a mano): se queda en true, con marcador
        // Section: older .cfg with true (set by hand): stays true, with the marker
        var antiguoTrue = LogicaJinete.Migrar(true, false);
        comprobar(antiguoTrue.Valor && antiguoTrue.Marcador && !antiguoTrue.Encendida,
                  "jinete: un true anterior se queda como está");

        // Sección: después de migrar, el jugador la apaga: se respeta en este y en los siguientes arranques
        // Section: after migrating, the player turns it off: it is kept in this and the following startups
        var apagada = LogicaJinete.Migrar(false, true);
        var otraVez = LogicaJinete.Migrar(apagada.Valor, apagada.Marcador);
        comprobar(!apagada.Valor && apagada.Marcador && !apagada.Encendida && !otraVez.Valor && !otraVez.Encendida,
                  "jinete: con el marcador puesto, un false del jugador se respeta siempre");

        // Sección: con el marcador y true, nada cambia
        // Section: with the marker and true, nothing changes
        var encendida = LogicaJinete.Migrar(true, true);
        comprobar(encendida.Valor && encendida.Marcador && !encendida.Encendida, "jinete: con marcador y true no cambia nada");
    }
}
