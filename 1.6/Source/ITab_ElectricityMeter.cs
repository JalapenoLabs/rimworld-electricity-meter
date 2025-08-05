/*************************************************************************
*
 * JALAPENO LABS
 * __________________
 *
 * [2025] Jalapeno Labs LLC
 * MIT License
 */

using RimWorld;
using UnityEngine;
using Verse;

namespace ElectricityMeter;

public class ITab_ElectricityMeter : ITab {
  private static readonly Vector2 WinSize = new Vector2(420f, 480f);
  private Vector2 scrollPosition;
  private float viewHeight = 1500f;

  public ITab_ElectricityMeter() {
    size = WinSize;
    labelKey = "ElectricityMeter_Power";
    tutorTag = "ElectricityMeter_Power";
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
    Building_ElectricityMeter meter = SelThing as Building_ElectricityMeter;
    if (meter == null) {
      return;
    }

    Text.Font = GameFont.Small;

    // outer rect with 10px padding on all sides
    Rect outerRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

    // how many text lines we'll draw:
    //   one per breakdown line, plus an optional gap, plus the total line
    int totalLines = 0;

    // ————— Consumption block —————
    if (meter.costBreakdown.Count > 0) {
      totalLines += meter.costBreakdown.Count; // each item
      totalLines += 4; // header, GapLine, total label, Gap()
    }

    // ————— Generation block —————
    if (meter.generationBreakdown.Count > 0) {
      totalLines += meter.generationBreakdown.Count; // each item
      totalLines += 4; // header, GapLine, total label, Gap()
    }

    // grab standard line height + spacing
    float lineHeight = Text.LineHeight;
    var listing = new Listing_Standard();
    float verticalSpacing = listing.verticalSpacing;

    // compute how tall our content area needs to be
    viewHeight = totalLines * (lineHeight + verticalSpacing);

    // inner viewRect: same X/Y origin, full width minus scrollbar, full content height
    Rect viewRect = new Rect(0f, 0f, WinSize.x - 40, viewHeight);

    // begin the scrollable area
    Widgets.BeginScrollView(outerRect, ref scrollPosition, viewRect);

    // start listing inside the viewRect
    listing.Begin(viewRect);

    if (!meter.connected) {
      listing.Label("ElectricityMeter_NotConnected".Translate());
      listing.End();
      Widgets.EndScrollView();
      return;
    }

    if (meter.costBreakdown.Count > 0) {
      Text.Font = GameFont.Medium;
      listing.Label(
        "ElectricityMeter_PowerConsumption".Translate()
      );
      Text.Font = GameFont.Small;
      // write each breakdown line
      foreach (string line in meter.costBreakdown) {
        listing.Label(line);
      }
      listing.GapLine();
      // always draw the total at the end
      listing.Label(
        "ElectricityMeter_TotalPowerConsumption".Translate(
          meter.totalPowerCost.ToString("F0")
        )
      );
      listing.Gap();
    }

    if (meter.generationBreakdown.Count > 0) {
      Text.Font = GameFont.Medium;
      listing.Label(
        "ElectricityMeter_PowerProducers".Translate()
      );
      Text.Font = GameFont.Small;
      // write each breakdown line
      foreach (string line in meter.generationBreakdown) {
        listing.Label(line);
      }
      listing.GapLine();
      // always draw the total at the end
      listing.Label(
        "ElectricityMeter_TotalPowerProduction".Translate(
          meter.totalPowerGeneration.ToString("F0")
        )
      );
      listing.Gap();
    }

    // finish up
    listing.End();
    Widgets.EndScrollView();
  }
}
