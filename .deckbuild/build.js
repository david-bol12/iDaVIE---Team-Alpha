const pptxgen = require("pptxgenjs");

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.3 x 7.5
pres.author = "Sub-Team 5";
pres.title = "Sub-Team 5 — Refactoring the Feature System";

// ---- Palette (clean / minimal, teal accent) ----
const INK = "1E293B";
const MUTE = "64748B";
const FAINT = "94A3B8";
const ACCENT = "0D9488";
const ACCENT_D = "0F766E";
const PANEL = "F1F5F9";
const PANEL2 = "E2E8F0";
const WHITE = "FFFFFF";

const HFONT = "Georgia";
const BFONT = "Calibri";
const MONO = "Consolas";

const makeShadow = () => ({ type: "outer", color: "000000", blur: 7, offset: 2, angle: 135, opacity: 0.10 });

// Shared header for content slides
function header(slide, kicker, title, subtitle) {
  slide.addText(kicker, { x: 0.7, y: 0.45, w: 12, h: 0.35, fontFace: BFONT, fontSize: 13, bold: true, color: ACCENT_D, charSpacing: 3, margin: 0 });
  slide.addText(title, { x: 0.68, y: 0.78, w: 12, h: 0.7, fontFace: HFONT, fontSize: 30, bold: true, color: INK, margin: 0 });
  slide.addText(subtitle, { x: 0.72, y: 1.46, w: 12, h: 0.4, fontFace: BFONT, fontSize: 14.5, color: MUTE, margin: 0 });
}

// =====================================================================
// SLIDE 1 — Title
// =====================================================================
let s1 = pres.addSlide();
s1.background = { color: WHITE };
s1.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.28, h: 7.5, fill: { color: ACCENT } });
s1.addText("iDaVIE  ·  SUB-TEAM 5", { x: 0.9, y: 1.15, w: 11, h: 0.4, fontFace: BFONT, fontSize: 15, bold: true, color: ACCENT_D, charSpacing: 3, margin: 0 });
s1.addText("Refactoring the\nFeature System", { x: 0.85, y: 1.7, w: 11.5, h: 2.4, fontFace: HFONT, fontSize: 54, bold: true, color: INK, lineSpacingMultiple: 1.0, margin: 0 });
s1.addText("Untangling iDaVIE's feature domain from Unity — turning one tangled “god class” into small, layered, testable pieces, with the quality gains measured.", { x: 0.9, y: 4.05, w: 10.6, h: 1.0, fontFace: BFONT, fontSize: 18, color: MUTE, lineSpacingMultiple: 1.15, margin: 0 });
s1.addShape(pres.shapes.LINE, { x: 0.92, y: 5.45, w: 4.2, h: 0, line: { color: PANEL2, width: 1.5 } });
s1.addText([{ text: "Team   ", options: { bold: true, color: INK } }, { text: "Fergus O’Flynn  ·  Harry Kennedy  ·  Mark Mannion  ·  Aaron Byrne", options: { color: MUTE } }], { x: 0.9, y: 5.65, w: 11, h: 0.4, fontFace: BFONT, fontSize: 15, margin: 0 });
s1.addText("Team Alpha · Software Engineering Project", { x: 0.9, y: 6.1, w: 11, h: 0.35, fontFace: BFONT, fontSize: 13, color: FAINT, margin: 0 });

// =====================================================================
// SLIDE 2 — Problem & Approach
// =====================================================================
let s2 = pres.addSlide();
s2.background = { color: WHITE };
s2.addText("From one god class to a layered design", { x: 0.7, y: 0.5, w: 12, h: 0.7, fontFace: HFONT, fontSize: 30, bold: true, color: INK, margin: 0 });
s2.addText("The problem we owned, and how we restructured it", { x: 0.72, y: 1.18, w: 12, h: 0.4, fontFace: BFONT, fontSize: 15, color: MUTE, margin: 0 });

const bx = 0.7, by = 1.95, bw = 4.1, bh = 4.05;
s2.addShape(pres.shapes.RECTANGLE, { x: bx, y: by, w: bw, h: bh, fill: { color: PANEL }, line: { color: PANEL2, width: 1 } });
s2.addText("BEFORE", { x: bx + 0.3, y: by + 0.25, w: bw - 0.6, h: 0.35, fontFace: BFONT, fontSize: 13, bold: true, color: MUTE, charSpacing: 2, margin: 0 });
s2.addText("FeatureSetManager", { x: bx + 0.3, y: by + 0.62, w: bw - 0.6, h: 0.4, fontFace: MONO, fontSize: 17, bold: true, color: INK, margin: 0 });
s2.addText([
  { text: "One class doing everything:", options: { color: INK, bold: true, breakLine: true, paraSpaceAfter: 6 } },
  { text: "Domain rules + selection logic", options: { bullet: true, color: MUTE, breakLine: true } },
  { text: "Unity lifecycle & rendering", options: { bullet: true, color: MUTE, breakLine: true } },
  { text: "File import / export (I/O)", options: { bullet: true, color: MUTE, breakLine: true } },
  { text: "UI menu state", options: { bullet: true, color: MUTE } },
], { x: bx + 0.3, y: by + 1.15, w: bw - 0.6, h: 2.2, fontFace: BFONT, fontSize: 14.5, lineSpacingMultiple: 1.05, margin: 0 });
s2.addText("Hard to test, hard to change — coupling concentrated in one place.", { x: bx + 0.3, y: by + 3.35, w: bw - 0.6, h: 0.6, fontFace: BFONT, fontSize: 12.5, italic: true, color: FAINT, margin: 0 });

s2.addText("→", { x: bx + bw - 0.1, y: by + 1.6, w: 0.9, h: 0.8, fontFace: BFONT, fontSize: 40, bold: true, color: ACCENT, align: "center", margin: 0 });

const ax = bx + bw + 0.8, aw = 7.0, ah = bh;
s2.addShape(pres.shapes.RECTANGLE, { x: ax, y: by, w: aw, h: ah, fill: { color: WHITE }, line: { color: ACCENT, width: 1.5 }, shadow: makeShadow() });
s2.addText("AFTER", { x: ax + 0.35, y: by + 0.25, w: aw - 0.7, h: 0.35, fontFace: BFONT, fontSize: 13, bold: true, color: ACCENT_D, charSpacing: 2, margin: 0 });
s2.addText("Three focused layers behind interfaces  (ADR 008)", { x: ax + 0.35, y: by + 0.6, w: aw - 0.7, h: 0.4, fontFace: BFONT, fontSize: 14, bold: true, color: INK, margin: 0 });
const layers = [
  { tag: "DOMAIN", name: "FeatureCatalog", desc: "Pure data & rules — no Unity" },
  { tag: "APPLICATION", name: "FeatureSetService", desc: "Selection & orchestration logic" },
  { tag: "INFRASTRUCTURE", name: "FeatureVisualiser", desc: "Unity rendering adapter" },
];
let ly = by + 1.15;
layers.forEach((L) => {
  s2.addShape(pres.shapes.RECTANGLE, { x: ax + 0.35, y: ly, w: aw - 0.7, h: 0.78, fill: { color: PANEL } });
  s2.addShape(pres.shapes.RECTANGLE, { x: ax + 0.35, y: ly, w: 0.08, h: 0.78, fill: { color: ACCENT } });
  s2.addText(L.tag, { x: ax + 0.55, y: ly + 0.1, w: 2.0, h: 0.3, fontFace: BFONT, fontSize: 10.5, bold: true, color: ACCENT_D, charSpacing: 1.5, margin: 0 });
  s2.addText(L.name, { x: ax + 0.55, y: ly + 0.36, w: 3.0, h: 0.35, fontFace: MONO, fontSize: 14.5, bold: true, color: INK, margin: 0 });
  s2.addText(L.desc, { x: ax + 3.5, y: ly + 0.23, w: aw - 3.9, h: 0.35, fontFace: BFONT, fontSize: 12.5, color: MUTE, align: "right", margin: 0 });
  ly += 0.92;
});
s2.addText([
  { text: "Also delivered:   ", options: { color: ACCENT_D, bold: true } },
  { text: "migrated ", options: { color: MUTE } },
  { text: "8 caller files", options: { color: INK, bold: true } },
  { text: " off the old class   ·   added the ", options: { color: MUTE } },
  { text: "IFeature", options: { color: INK, bold: true, fontFace: MONO } },
  { text: " interface + a contract for Team 4   ·   ", options: { color: MUTE } },
  { text: "2 worked examples", options: { color: INK, bold: true } },
  { text: "  (Moment Maps, VOTable Export)", options: { color: MUTE } },
], { x: 0.7, y: 6.35, w: 11.9, h: 0.5, fontFace: BFONT, fontSize: 13, margin: 0, align: "center" });

// =====================================================================
// Reusable "After classes" table builder for example slides
// =====================================================================
// Hand-built table (addTable renders unreliably through the export path).
// Region spans x 5.25 → 12.6.  Returns y just below the last row.
function afterTable(slide, _x, y, _w, rows) {
  const RX = 5.25, RW = 7.35;
  const cols = [
    { x: 5.42, w: 2.65, align: "left" },   // Class
    { x: 8.12, w: 1.95, align: "left" },   // Layer
    { x: 10.05, w: 0.82, align: "center" },// WMC
    { x: 10.9, w: 0.82, align: "center" }, // CBO
    { x: 11.75, w: 0.82, align: "center" },// LCOM
  ];
  const labels = ["Class", "Layer", "WMC", "CBO", "LCOM"];
  const hh = 0.42, rh = 0.5;
  // header
  slide.addShape(pres.shapes.RECTANGLE, { x: RX, y, w: RW, h: hh, fill: { color: ACCENT_D } });
  labels.forEach((t, c) => slide.addText(t, { x: cols[c].x, y, w: cols[c].w, h: hh, fontFace: BFONT, fontSize: 12, bold: true, color: WHITE, align: cols[c].align, valign: "middle", margin: 0 }));
  // rows
  rows.forEach((r, i) => {
    const ry = y + hh + i * rh;
    slide.addShape(pres.shapes.RECTANGLE, { x: RX, y: ry, w: RW, h: rh, fill: { color: i % 2 === 0 ? WHITE : PANEL }, line: { color: PANEL2, width: 0.5 } });
    r.forEach((cell, c) => {
      const isClass = c === 0;
      slide.addText(cell, {
        x: cols[c].x, y: ry, w: cols[c].w, h: rh, fontFace: isClass ? MONO : BFONT,
        fontSize: isClass ? 11.5 : 12, bold: isClass, color: c === 1 ? MUTE : INK,
        align: cols[c].align, valign: "middle", margin: 0,
      });
    });
  });
  return y + hh + rows.length * rh;
}

// Before callout box
function beforeBox(slide, x, y, w, h, className, lines, metricLine) {
  slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h, fill: { color: PANEL }, line: { color: PANEL2, width: 1 } });
  slide.addShape(pres.shapes.RECTANGLE, { x, y, w: 0.1, h, fill: { color: FAINT } });
  slide.addText("BEFORE  ·  ONE CLASS", { x: x + 0.32, y: y + 0.22, w: w - 0.6, h: 0.3, fontFace: BFONT, fontSize: 11.5, bold: true, color: MUTE, charSpacing: 1.5, margin: 0 });
  slide.addText(className, { x: x + 0.32, y: y + 0.52, w: w - 0.55, h: 0.38, fontFace: MONO, fontSize: 14.5, bold: true, color: INK, margin: 0 });
  slide.addText(lines, { x: x + 0.32, y: y + 1.0, w: w - 0.6, h: h - 1.55, fontFace: BFONT, fontSize: 13, color: MUTE, lineSpacingMultiple: 1.12, margin: 0, valign: "top" });
  slide.addText(metricLine, { x: x + 0.32, y: y + h - 0.5, w: w - 0.6, h: 0.4, fontFace: BFONT, fontSize: 12, italic: true, color: FAINT, margin: 0 });
}

// Takeaway strip
function takeaway(slide, runs) {
  slide.addShape(pres.shapes.RECTANGLE, { x: 0.7, y: 6.55, w: 11.9, h: 0.62, fill: { color: WHITE }, line: { color: ACCENT, width: 1 } });
  slide.addShape(pres.shapes.RECTANGLE, { x: 0.7, y: 6.55, w: 0.1, h: 0.62, fill: { color: ACCENT } });
  slide.addText(runs, { x: 0.95, y: 6.55, w: 11.5, h: 0.62, fontFace: BFONT, fontSize: 13, color: MUTE, valign: "middle", margin: 0 });
}

// =====================================================================
// SLIDE 3 — Example 1: Moment Maps
// =====================================================================
let s3 = pres.addSlide();
s3.background = { color: WHITE };
header(s3, "REFACTORING EXAMPLE 1", "Moment Maps", "A Unity MonoBehaviour that mixed calculation, rendering and UI — split along clean layer lines");

beforeBox(s3, 0.7, 2.0, 4.1, 4.3, "MomentMapRenderer", [
  { text: "A 391-line MonoBehaviour combining:", options: { color: INK, bold: true, breakLine: true, paraSpaceAfter: 6 } },
  { text: "Moment computation (the maths)", options: { bullet: true, breakLine: true } },
  { text: "Unity rendering", options: { bullet: true, breakLine: true } },
  { text: "UI / plot drawing", options: { bullet: true } },
], "WMC 27   ·   CBO 17   ·   LCOM 0.48 (borderline)");

s3.addText("AFTER  ·  focused classes behind interfaces", { x: 5.25, y: 2.0, w: 7.3, h: 0.32, fontFace: BFONT, fontSize: 12.5, bold: true, color: ACCENT_D, charSpacing: 1, margin: 0 });
afterTable(s3, 5.25, 2.42, 7.35, [
  ["MomentMapCalculator", "Domain · pure", "22", "2", "0.00"],
  ["MomentMapService", "Application", "4", "5", "0.00"],
  ["MomentMapRequest / Result", "Domain DTOs", "6 / 5", "2", "0.00"],
  ["MomentMapRendererAdapter", "Infra · Unity", "19", "14", "0.75"],
  ["IMomentMap{Adapter,Service}", "Interfaces", "–", "1–2", "–"],
]);
s3.addText("The maths now lives in a pure calculator (CBO 17 → 2, perfectly cohesive); Unity work sits behind an adapter + interface. Outliers — calculator WMC 22 and the Unity adapter’s LCOM — are expected and isolated.", {
  x: 5.27, y: 5.45, w: 7.3, h: 0.85, fontFace: BFONT, fontSize: 12.5, color: MUTE, italic: true, lineSpacingMultiple: 1.1, margin: 0, valign: "top",
});

takeaway(s3, [
  { text: "Takeaway   ", options: { color: ACCENT_D, bold: true } },
  { text: "Coupling that was concentrated in one class drops from ", options: {} },
  { text: "17 to 2", options: { color: INK, bold: true } },
  { text: " in the core logic — and the moment maths can now be unit-tested without Unity.", options: {} },
]);

// =====================================================================
// SLIDE 4 — Example 2: VOTable Export
// =====================================================================
let s4 = pres.addSlide();
s4.background = { color: WHITE };
header(s4, "REFACTORING EXAMPLE 2", "VOTable Export", "A “clean-looking” class hiding a real smell — fixed with ports, not by chasing metrics");

beforeBox(s4, 0.7, 2.0, 4.1, 4.3, "VoTableSaver", [
  { text: "One 90-line static method", options: { color: INK, bold: true, breakLine: true } },
  { text: "(SaveFeatureSetAsVoTable) doing it all:", options: { color: INK, bold: true, breakLine: true, paraSpaceAfter: 6 } },
  { text: "Document building", options: { bullet: true, breakLine: true } },
  { text: "Headers + rows", options: { bullet: true, breakLine: true } },
  { text: "File I/O", options: { bullet: true } },
], "WMC 7 · CBO 6 · LCOM 0 — passes every gate");

s4.addText("AFTER  ·  one method becomes four, behind a port", { x: 5.25, y: 2.0, w: 7.3, h: 0.32, fontFace: BFONT, fontSize: 12.5, bold: true, color: ACCENT_D, charSpacing: 1, margin: 0 });
afterTable(s4, 5.25, 2.42, 7.35, [
  ["FeatureCatalog", "Domain", "4", "3", "0.00"],
  ["VoTableExportService", "Infra · Persistence", "14", "8", "0.00"],
  ["IVoTableExporter", "Domain · port", "–", "2", "–"],
  ["ICoordinateTransformer / IAstFrame", "Domain · ports", "–", "0–1", "–"],
]);
s4.addText("CK metrics already “passed”, so they didn’t flag the real problem: one method doing four jobs. The fix is structural — BuildDocument / BuildHeaders / BuildRow / Export, with export behind the IVoTableExporter port so the domain depends only on an abstraction.", {
  x: 5.27, y: 5.1, w: 7.3, h: 1.1, fontFace: BFONT, fontSize: 12.5, color: MUTE, italic: true, lineSpacingMultiple: 1.1, margin: 0, valign: "top",
});

takeaway(s4, [
  { text: "Takeaway   ", options: { color: ACCENT_D, bold: true } },
  { text: "Not every smell shows up in the numbers — ", options: {} },
  { text: "single-responsibility & testability", options: { color: INK, bold: true } },
  { text: " drove this one. The exporter can now be tested with no Unity and no file system.", options: {} },
]);

// =====================================================================
// SLIDE 5 — Results & Deliverables
// =====================================================================
let s5 = pres.addSlide();
s5.background = { color: WHITE };
s5.addText("Outcomes & deliverables", { x: 0.7, y: 0.5, w: 12, h: 0.7, fontFace: HFONT, fontSize: 30, bold: true, color: INK, margin: 0 });
s5.addText("Across both examples, every extracted class lands within the agreed CK acceptance gates", { x: 0.72, y: 1.18, w: 12, h: 0.4, fontFace: BFONT, fontSize: 15, color: MUTE, margin: 0 });

s5.addText("Coupling of the core logic  ·  Moment Maps", { x: 0.7, y: 1.85, w: 6, h: 0.35, fontFace: BFONT, fontSize: 13, bold: true, color: INK, margin: 0 });
s5.addChart(pres.charts.BAR, [
  { name: "Before (god class)", labels: ["Coupling (CBO)", "Complexity (WMC)"], values: [17, 27] },
  { name: "After (domain class)", labels: ["Coupling (CBO)", "Complexity (WMC)"], values: [2, 22] },
], {
  x: 0.5, y: 2.2, w: 6.6, h: 3.4, barDir: "col", chartColors: [PANEL2, ACCENT], chartArea: { fill: { color: WHITE } },
  catAxisLabelColor: MUTE, catAxisLabelFontFace: BFONT, catAxisLabelFontSize: 12,
  valAxisLabelColor: FAINT, valGridLine: { color: PANEL2, size: 0.5 }, catGridLine: { style: "none" },
  showValue: true, dataLabelPosition: "outEnd", dataLabelColor: INK, dataLabelFontSize: 12, dataLabelFontBold: true,
  showLegend: true, legendPos: "b", legendColor: MUTE, legendFontFace: BFONT, legendFontSize: 11, barGapWidthPct: 60,
});
s5.addText("Lower is better — coupling drops sharply once Unity & I/O move out.", { x: 0.7, y: 5.65, w: 6.4, h: 0.4, fontFace: BFONT, fontSize: 11.5, italic: true, color: FAINT, margin: 0 });

const rx = 7.7, rw = 5.0;
s5.addShape(pres.shapes.RECTANGLE, { x: rx, y: 1.95, w: rw, h: 1.35, fill: { color: PANEL } });
s5.addShape(pres.shapes.RECTANGLE, { x: rx, y: 1.95, w: 0.1, h: 1.35, fill: { color: ACCENT } });
s5.addText([{ text: "0.48 ", options: { fontSize: 40, bold: true, color: INK } }, { text: "→ ", options: { fontSize: 30, color: FAINT } }, { text: "0.00", options: { fontSize: 40, bold: true, color: ACCENT_D } }], { x: rx + 0.35, y: 2.05, w: rw - 0.5, h: 0.8, fontFace: HFONT, valign: "middle", margin: 0 });
s5.addText("Lack-of-cohesion (LCOM) in the domain class — now perfectly cohesive", { x: rx + 0.37, y: 2.82, w: rw - 0.6, h: 0.42, fontFace: BFONT, fontSize: 12, color: MUTE, margin: 0 });

s5.addShape(pres.shapes.RECTANGLE, { x: rx, y: 3.5, w: rw, h: 1.35, fill: { color: PANEL } });
s5.addShape(pres.shapes.RECTANGLE, { x: rx, y: 3.5, w: 0.1, h: 1.35, fill: { color: ACCENT } });
s5.addText([{ text: "All ", options: { fontSize: 30, bold: true, color: INK } }, { text: "CK gates", options: { fontSize: 30, bold: true, color: ACCENT_D } }], { x: rx + 0.35, y: 3.6, w: rw - 0.5, h: 0.7, fontFace: HFONT, valign: "middle", margin: 0 });
s5.addText("Verified by an automated metrics checker we built (tools/check_ck_metrics.py).", { x: rx + 0.37, y: 4.28, w: rw - 0.6, h: 0.5, fontFace: BFONT, fontSize: 12, color: MUTE, margin: 0 });

s5.addText("DELIVERABLES", { x: rx, y: 5.05, w: rw, h: 0.3, fontFace: BFONT, fontSize: 11, bold: true, color: MUTE, charSpacing: 2, margin: 0 });
s5.addText([
  { text: "Requirements + design document & UML", options: { bullet: true, color: INK, breakLine: true } },
  { text: "3-level test suite (unit · property-based · scenario)", options: { bullet: true, color: INK, breakLine: true } },
  { text: "Testing strategy + CK-metrics report & checker", options: { bullet: true, color: INK } },
], { x: rx + 0.1, y: 5.35, w: rw - 0.1, h: 1.6, fontFace: BFONT, fontSize: 12.5, color: INK, lineSpacingMultiple: 1.05, margin: 0 });

pres.writeFile({ fileName: "SubTeam5_Overview.pptx" }).then((f) => console.log("WROTE " + f));
