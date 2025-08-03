/*************************************************************************
*
 * JALAPENO LABS
 * __________________
 *
 * [2025] Jalapeno Labs LLC
 * MIT License
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ElectricityMeter {
    // Mod entry point & definition
    public class ModEntry : Mod {
        public ModEntry(ModContentPack content) : base(content) {
            var harmony = new Harmony("jalapenolabs.rimworld.electricitymeter");
            harmony.PatchAll();

            Log.Message("⚡💥Electricity Meter mod was successfully loaded!");
        }
    }

    public class Building_ElectricityMeter : Building {
        // Stores the latest breakdown of power usage by def, and total consumption
        public List<string> breakdownLines = new List<string>();
        public float totalPower = 0f;
        public bool connected = false;  // whether the meter is connected to a power grid with other devices

        private int ticksSinceLastUpdate = 0;
        private const int UpdateIntervalTicks = 60;  // Recalculate once per second (60 ticks)

        protected override void Tick() {
            base.Tick();
            if (Spawned && Find.Selector.SingleSelectedThing == this) {
                // Only update when selected, to save performance
                ticksSinceLastUpdate++;
                if (ticksSinceLastUpdate >= UpdateIntervalTicks) {
                    ticksSinceLastUpdate = 0;
                    RecalculatePowerUsage();
                }
            }
            else {
                // Reset counter when not selected
                ticksSinceLastUpdate = 0;
            }
        }

        public void RecalculatePowerUsage() {
            breakdownLines.Clear();
            totalPower = 0f;
            connected = false;
            var net = PowerComp?.PowerNet;
    
            if (net == null || (net.transmitters.Count <= 1 && net.connectors.Count == 0)) {
                // Not connected to any meaningful power network
                breakdownLines.Add("Not connected to any power grid.");
                connected = false;
                totalPower = 0f;
                return;
            }

            connected = true;
            // Group power consumption by thing def
            var consumptionByDef = new Dictionary<ThingDef, (int count, float total, float each)>();
            foreach (var cp in net.powerComps) {
                if (cp.PowerOn && cp.PowerOutput < 0f) {
                    ThingDef def = cp.parent.def;
                    float usage = -cp.PowerOutput;  // base consumption in Watts

                    if (!consumptionByDef.ContainsKey(def)) {
                        consumptionByDef[def] = (0, 0f, usage);
                    }

                    var entry = consumptionByDef[def];
                    entry.count += 1;
                    entry.total += usage;
                    // 'each' remains the same (usage) for all in this group
                    consumptionByDef[def] = entry;
                    totalPower += usage;
                }
            }

            // Sort groups by total power descending for readability
            foreach (var kvp in consumptionByDef.OrderByDescending(e => e.Value.total)) {
                ThingDef def = kvp.Key;
                int count = kvp.Value.count;
                float total = kvp.Value.total;
                float each = kvp.Value.each;
                if (count > 1) {
                    breakdownLines.Add($"{def.label.CapitalizeFirst()} x{count} ({each:F0} W each): {total:F0} W");
                }
                else {
                    breakdownLines.Add($"{def.label.CapitalizeFirst()}: {total:F0} W");
                }
            }
            // If no consumers on the net, breakdownLines will be empty (totalPower will be 0).
            // (Total will still be displayed in the ITab even if no devices are consuming power.)
        }
    }

    public class ITab_ElectricityMeter : ITab {
        public ITab_ElectricityMeter() {
            this.size = new Vector2(300f, 400f);
            this.labelKey = "Power";    // Label for the tab button (shown as "Power")
            this.tutorTag = "Power";    // Tutor tag for tutorial system
            Log.Message("Registering ITab_ElectricityMeter");
        }

        // On opening the tab, recalc immediately for up-to-date data
        public override void OnOpen() {
            base.OnOpen();
            if (SelThing is Building_ElectricityMeter meter) {
                meter.RecalculatePowerUsage();
            }
        }

        protected override void FillTab() {
            Text.Font = GameFont.Small;
            Building_ElectricityMeter meter = SelThing as Building_ElectricityMeter;
            if (meter == null) {
                return;
            };

            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);
            if (!meter.connected) {
                // Display not-connected message
                if (meter.breakdownLines.Count > 0) {
                    listing.Label(meter.breakdownLines[0]);
                }
            }
            else {
                // List each group’s consumption
                foreach (string line in meter.breakdownLines) {
                    listing.Label(line);
                }
                // Draw a line then total consumption at the end
                if (meter.breakdownLines.Count > 0) {
                    listing.GapLine();
                }
                listing.Label($"Total: {meter.totalPower:F0} W");
            }
            listing.End();
        }
    }
}
