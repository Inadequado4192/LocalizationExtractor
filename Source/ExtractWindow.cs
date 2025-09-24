using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace LocalizationExtractor
{
    public class ExtractWindow : Window
    {
        public List<ModMetaData> activeMods = new List<ModMetaData>();

        public Vector2 scrollViewPos = Vector2.zero;

        public ExtractWindow()
        {
            doCloseX = true; // Draw close button
            closeOnCancel = true;
            activeMods = ModsConfig.ActiveModsInLoadOrder.ToList();
        }

        // Draw content per frame
        public override void DoWindowContents(Rect inRect)
        {
            float num = 5f;

            // Draw Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(5f, num, inRect.width, 35f), "SelectMod".Translate().Colorize(ColorLibrary.Green));
            Text.Font = GameFont.Small;
            num += 45f;


            // Draw list of mods
            float buttonHeight = 25f;
            Widgets.BeginScrollView(
                new Rect(0f, 40f, inRect.width, inRect.height),
                ref scrollViewPos,
                new Rect(0f, 40f, inRect.width, activeMods.Count * buttonHeight + 10f)
            );
            foreach (ModMetaData modMetaData in activeMods)
            {
                if (Widgets.ButtonText(new Rect(5f, num, inRect.width, buttonHeight), modMetaData.Name, drawBackground: false, doMouseoverSound: false, active: true, null))
                {
                    LongEventHandler.QueueLongEvent(
                        delegate
                        {
                            Extractor extractor = new Extractor(modMetaData);
                            extractor.ExtractTranslate();
                        },
                        "LocalizationExtractor".Translate(),
                        doAsynchronously: true,
                        delegate (Exception e) { Log.Message(e.ToString()); }
                    );
                    Messages.Message("SuccessfullyExtracted".Translate(), MessageTypeDefOf.PositiveEvent);
                }
                num += 30f;
            }
            Widgets.EndScrollView();
        }
    }
}