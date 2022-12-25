﻿/*
 * This file contains substantial portions of code adapted from Thomas O'Sullivan's "AutoToot" 
 * mod under the MIT license. Below is the text of the copyright notice and permission notice.
 */

/*
    COPYRIGHT NOTICE:
	© 2022 Thomas O'Sullivan - All rights reserved.
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:
	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	SOFTWARE.
	FILE INFORMATION:
	Name: PointSceneControllerPatch.cs
	Project: AutoToot
	Author: Tom
	Created: 16th October 2022
*/

using HoverToot;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HoverToot
{
    internal class AntiCheatingPatches
    {
        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.Start))]
        internal class PointSceneControllerStartPatch
        {
            static void Prefix()
            {
                if (Plugin.DidToggleThisSong)
                    --GlobalVariables.localsave.tracks_played;
                Plugin.Logger.LogInfo($"Did toggle: {Plugin.DidToggleThisSong}");
            }
        }

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.updateSave))]
        internal class PointSceneControllerUpdateSavePatch
        {
            static bool Prefix()
            {
                Plugin.Logger.LogInfo($"Did toggle: {Plugin.DidToggleThisSong}");
                return !Plugin.DidToggleThisSong;
            }
        }

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.checkScoreCheevos))]
        internal class PointSceneControllerAchievementsCheckPatch
        {
            static bool Prefix()
            {
                Plugin.Logger.LogInfo($"Did toggle: {Plugin.DidToggleThisSong}");
                return !Plugin.DidToggleThisSong;
            }
        }

        [HarmonyPatch(typeof(PointSceneController), nameof(PointSceneController.doCoins))]
        internal class PointSceneControllerDoCoinsPatch
        {
            static void Postfix(PointSceneController __instance)
            {
                if (!Plugin.DidToggleThisSong)
                    return;

                GameObject coin = GameObject.Find(CoinPath);
                if (coin == null)
                {
                    Plugin.Logger.LogError("Unable to find toots coin, it may be visible still.");
                }
                else
                {
                    coin.SetActive(false);
                }

                GameObject tootsObject = GameObject.Find(TootsTextPath);
                if (tootsObject == null)
                {
                    Plugin.Logger.LogError("Unable to find toots text, HoverToot indicator will not be present.");
                }
                else
                {
                    __instance.tootstext.text = "Used HoverToot";

                    Vector3 textPosition = tootsObject.transform.position;
                    textPosition.x = TootsTextXPosition;
                    tootsObject.transform.position = textPosition;
                }

                __instance.Invoke(nameof(PointSceneController.showContinue), 0.75f);
            }

            private const string CoinPath = "Canvas/buttons/coingroup/coin";
            private const string TootsTextPath = "Canvas/buttons/coingroup/Text";
            private const float TootsTextXPosition = -0.246f;
        }
    }
}
