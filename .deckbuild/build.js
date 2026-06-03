const pptxgen = require("pptxgenjs");

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.3 x 7.5
pres.author = "Sub-Team 5";
pres.title = "Sub-Team 5 — Feature System";

// ---- Plain, Office-style palette ----
const NAVY = "1F3864";
const TEXT = "262626";
const MUTE = "595959";
const BORDER = "BFBFBF";
const ALT = "F2F2F2";
const WHITE = "FFFFFF";
const CAL = "Calibri";

function title(slide, t, sub) {
  slide.addText(t, { x: 0.6, y: 0.45, w: 12.1, h: 0.7, fontFace: CAL, fontSize: 34, bold: true, color: NAVY, margin: 0 });
  if (sub) slide.addText(sub, { x: 0.62, y: 1.18, w: 12.1, h: 0.45, fontFace: CAL, fontSize: 17, color: MUTE, margin: 0 });
}

// Simple 2-column table built from shapes (renders reliably)
function table2(slide, x, y, w, c1, header, rows, rh) {
  rh = rh || 0.58;
  const hh = 0.5;
  const c2 = w - c1;
  slide.addShape(pres.shapes.RECTANGLE, { x, y, w, h: hh, fill: { color: NAVY }, line: { color: NAVY, width: 1 } });
  slide.addText(header[0], { x: x + 0.18, y, w: c1 - 0.25, h: hh, color: WHITE, bold: true, fontFace: CAL, fontSize: 15, valign: "middle", margin: 0 });
  slide.addText(header[1], { x: x + c1 + 0.18, y, w: c2 - 0.35, h: hh, color: WHITE, bold: true, fontFace: CAL, fontSize: 15, valign: "middle", margin: 0 });
  rows.forEach((r, i) => {
    const ry = y + hh + i * rh;
    slide.addShape(pres.shapes.RECTANGLE, { x, y: ry, w, h: rh, fill: { color: i % 2 ? ALT : WHITE }, line: { color: BORDER, width: 0.75 } });
    slide.addText(r[0], { x: x + 0.18, y: ry, w: c1 - 0.25, h: rh, color: TEXT, bold: true, fontFace: CAL, fontSize: 14.5, valign: "middle", margin: 0 });
    slide.addText(r[1], { x: x + c1 + 0.18, y: ry, w: c2 - 0.35, h: rh, color: TEXT, fontFace: CAL, fontSize: 14.5, valign: "middle", margin: 0 });
  });
  return y + hh + rows.length * rh;
}

function bullets(slide, x, y, w, items, fs) {
  slide.addText(
    items.map((it, i) => ({ text: it, options: { bullet: { indent: 18 }, breakLine: true, paraSpaceAfter: 14, color: TEXT } })),
    { x, y, w, h: 5, fontFace: CAL, fontSize: fs || 19, color: TEXT, valign: "top", margin: 0 }
  );
}

// =====================================================================
// SLIDE 1 — Title
// =====================================================================
let s1 = pres.addSlide();
s1.background = { color: WHITE };
s1.addText("Sub-Team 5", { x: 0.9, y: 2.0, w: 11, h: 0.5, fontFace: CAL, fontSize: 20, bold: true, color: MUTE, margin: 0 });
s1.addText("Refactoring the Feature System", { x: 0.88, y: 2.55, w: 11.6, h: 1.0, fontFace: CAL, fontSize: 44, bold: true, color: NAVY, margin: 0 });
s1.addText("Making iDaVIE's feature code simpler, safer, and easier to test.", { x: 0.9, y: 3.6, w: 11, h: 0.5, fontFace: CAL, fontSize: 20, color: TEXT, margin: 0 });
s1.addText("Fergus O'Flynn   |   Harry Kennedy   |   Mark Mannion   |   Aaron Byrne", { x: 0.9, y: 4.9, w: 11.5, h: 0.4, fontFace: CAL, fontSize: 16, color: MUTE, margin: 0 });

// =====================================================================
// SLIDE 2 — The problem
// =====================================================================
let s2 = pres.addSlide();
s2.background = { color: WHITE };
title(s2, "The problem we set out to fix");
bullets(s2, 0.8, 1.6, 11.6, [
  "The feature system was built around one very large class that did almost everything.",
  "It handled the data, the on-screen display, saving files, and the menus all at the same time.",
  "Because everything was bundled together, the code was hard to test, risky to change, and easy to break by accident.",
  "Our goal: break it apart into smaller classes that each do one job well.",
]);

// =====================================================================
// SLIDE 3 — Example 1: Moment Maps (table)
// =====================================================================
let s3 = pres.addSlide();
s3.background = { color: WHITE };
title(s3, "Example 1: Moment Maps");
s3.addText("Before, a single class did the maths, the on-screen display, and the plotting all together. We split it into focused classes:", {
  x: 0.8, y: 1.55, w: 11.7, h: 0.6, fontFace: CAL, fontSize: 16, color: TEXT, margin: 0,
});
table2(s3, 0.8, 2.3, 11.7, 4.3, ["New class", "What it does"], [
  ["MomentMapCalculator", "Does the moment-map maths, and nothing else"],
  ["MomentMapService", "Takes a request and hands back the result"],
  ["MomentMapRequest / Result", "Simple holders for the inputs and the outputs"],
  ["MomentMapRendererAdapter", "Draws the result on screen"],
  ["Two boundary pieces", "Keep the parts cleanly separated"],
]);
s3.addText("The maths can now be checked on its own, without having to run the 3D display.", {
  x: 0.8, y: 5.7, w: 11.7, h: 0.5, fontFace: CAL, fontSize: 16, italic: true, color: MUTE, margin: 0,
});

// =====================================================================
// SLIDE 4 — Example 2: VOTable export (table)
// =====================================================================
let s4 = pres.addSlide();
s4.background = { color: WHITE };
title(s4, "Example 2: Saving feature files");
s4.addText("Before, one long method built the document, the headers, the rows, and wrote the file all at once. We split it up:", {
  x: 0.8, y: 1.55, w: 11.7, h: 0.6, fontFace: CAL, fontSize: 16, color: TEXT, margin: 0,
});
table2(s4, 0.8, 2.3, 11.7, 4.3, ["New class", "What it does"], [
  ["FeatureCatalog", "Holds the feature data"],
  ["VoTableExportService", "Builds the file and writes it out"],
  ["IVoTableExporter", "A clean boundary between the data and the file"],
  ["Coordinate helpers", "Handle the coordinate conversions"],
]);
s4.addText("The file saving can now be tested without writing real files to disk.", {
  x: 0.8, y: 5.15, w: 11.7, h: 0.5, fontFace: CAL, fontSize: 16, italic: true, color: MUTE, margin: 0,
});

// =====================================================================
// SLIDE 5 — Improvements: code quality & health
// =====================================================================
let s5 = pres.addSlide();
s5.background = { color: WHITE };
title(s5, "Improvements to code quality and health");
bullets(s5, 0.8, 1.55, 11.7, [
  "Each class now has one clear job, so the code is much easier to read and change.",
  "The core logic is separated from the display, so it can be tested on its own.",
  "The code is far less tangled, so changing one part is far less likely to break another.",
  "We added automated tests covering worked examples, tricky edge cases, and full user scenarios.",
  "We measured code quality before and after, and every part now meets the team's targets.",
  "Clear, documented boundaries make it easier for other sub-teams to build on our work.",
], 18);

pres.writeFile({ fileName: "SubTeam5_Overview.pptx" }).then((f) => console.log("WROTE " + f));
