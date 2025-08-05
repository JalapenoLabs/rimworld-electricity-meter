/*************************************************************************
*
 * JALAPENO LABS
 * __________________
 *
 * [2025] Jalapeno Labs LLC
 * MIT License
 */

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ElectricityMeter;

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
    public float totalPowerCost = 0f;
    public List<string> costBreakdown = new List<string>();

    public float totalPowerGeneration = 0f;
    public List<string> generationBreakdown = new List<string>();

    // Whether the meter is connected to a power grid with other devices
    public bool connected = false;

    // Ticks
    private int tickCount = 0;
    private int tickInterval = 60;

    protected override void Tick() {
        base.Tick();
        tickCount++;

        if (!Spawned || (tickCount % tickInterval) != 0) {
            return;
        }

        // Only update when selected, to save performance
        if (Find.Selector.SingleSelectedThing != this) {
            return;
        }

        RecalculatePowerUsage();
    }

    public void RecalculatePowerUsage() {
        costBreakdown.Clear();
        generationBreakdown.Clear();
        totalPowerCost = 0f;
        totalPowerGeneration = 0f;
        var net = PowerComp?.PowerNet;
        connected = !(net == null || (net.transmitters.Count <= 1 && net.connectors.Count == 0));

        if (!connected) {
            return;
        }

        // We must iterate twice to group items by count (to group multiple same defs)

        // Group power consumption by thing def
        var consumptionByDef = new Dictionary<ThingDef, (int count, float total, float each)>();
        var generatorsByDef = new Dictionary<ThingDef, (int count, float total, float each)>();
        foreach (var cp in net.powerComps) {
            if (!cp.PowerOn) {
                continue;
            }
            ThingDef def = cp.parent.def;
            float usage = cp.PowerOutput;  // base consumption in Watts

            if (cp.PowerOutput >= 0f) {
                if (!generatorsByDef.ContainsKey(def)) {
                    generatorsByDef[def] = (0, 0f, usage);
                }

                var entry = generatorsByDef[def];
                entry.count += 1;
                entry.total += usage;  // convert to positive for generation
                generatorsByDef[def] = entry;
                totalPowerGeneration += usage;
            }
            else {
                if (!consumptionByDef.ContainsKey(def)) {
                    consumptionByDef[def] = (0, 0f, usage);
                }

                var entry = consumptionByDef[def];
                entry.count += 1;
                entry.total += usage;
                consumptionByDef[def] = entry;
                totalPowerCost += usage;
            }
        }

        // Sort groups by total power descending for readability
        foreach (var kvp in consumptionByDef.OrderByDescending(e => e.Value.total)) {
            ThingDef def = kvp.Key;
            if (kvp.Value.count > 1) {
                costBreakdown.Add(
                    "ElectricityMeter_ConsumptionMany".Translate(
                        def.label.CapitalizeFirst(),
                        kvp.Value.total.ToString("F0"),
                        kvp.Value.count.ToString()
                    )
                );
            }
            else {
                costBreakdown.Add(
                    "ElectricityMeter_ConsumptionOne".Translate(
                        def.label.CapitalizeFirst(),
                        kvp.Value.total.ToString("F0")
                    )
                );
            }
        }

        foreach (var kvp in generatorsByDef.OrderByDescending(e => e.Value.total)) {
            ThingDef def = kvp.Key;
            if (kvp.Value.count > 1) {
                generationBreakdown.Add(
                    "ElectricityMeter_GenerationMany".Translate(
                        def.label.CapitalizeFirst(),
                        kvp.Value.total.ToString("F0"),
                        kvp.Value.count.ToString()
                    )
                );
            }
            else {
                generationBreakdown.Add(
                    "ElectricityMeter_GenerationOne".Translate(
                        def.label.CapitalizeFirst(),
                        kvp.Value.total.ToString("F0")
                    )
                );
            }
        }
    }
}
