# Sub-Team 5 — CK Metrics (Chidamber & Kemerer Suite)

CK metrics for the two worked refactoring examples (Moment Maps and VoTable
Export), captured before and after the refactor. Values are mapped from the
SciTools **Understand** CSV exports as follows:

| CK metric | Understand source | Notes |
|---|---|---|
| WMC | `SumCyclomatic` | Sum of method cyclomatic complexity. (Use `CountDeclMethod` if your rubric defines WMC as a plain method count.) |
| DIT | `MaxInheritanceTree` | |
| NOC | `CountClassDerived` | |
| CBO | `CountClassCoupled` | |
| LCOM | `PercentLackOfCohesionModified` ÷ 100 | Henderson–Sellers variant, expressed as a fraction. |
| RFC | *not natively exported* | Estimated from declared methods + fan-out calls. Treat as approximate. |

> ⚠️ **RFC caveat:** the Understand CSV export does not contain a native RFC
> column. The RFC figures below are estimates; method counts are small enough
> that RFC sits well under 50 in every class, but these are not measured values.
> If RFC is audited, re-run Understand with the RFC metric enabled.

---

## CK metric reference (acceptance gates)

| CK metric | Meaning | Acceptable range |
|---|---|---|
| WMC | Weighted Methods per Class | ≤ 20 domain; ≤ 40 adapters |
| DIT | Depth of Inheritance Tree | ≤ 4 |
| NOC | Number of Children | ≤ 5 |
| CBO | Coupling Between Object classes | ≤ 14 domain; ≤ 25 orchestrators; cycles forbidden |
| RFC | Response For a Class | ≤ 50 |
| LCOM | Lack of Cohesion in Methods (Henderson–Sellers) | ≤ 0.5 |

---

## Example 1 — Moment Maps

### Before — single class `VolumeData.MomentMapRenderer` (god class / MonoBehaviour)

| Metric | Value | Range | Pass? |
|---|---|---|---|
| WMC | 27 | ≤20 domain / ≤40 adapter | ✅ as adapter (❌ if judged domain) |
| DIT | 2 | ≤4 | ✅ |
| NOC | 0 | ≤5 | ✅ |
| CBO | 17 | ≤14 domain / ≤25 orch | ✅ as orchestrator (❌ if domain) |
| RFC | ~22 decl methods (+calls) | ≤50 | ✅ |
| LCOM | 0.48 | ≤0.5 | ✅ (borderline) |

This one class mixes pure calculation, Unity rendering, and UI plotting. It
passes most gates only because it is read as an adapter, and LCOM is right on
the edge.

### After — split into domain + application + infrastructure

| Class | Layer | WMC | DIT | NOC | CBO | RFC≈ | LCOM | Notes |
|---|---|---|---|---|---|---|---|---|
| `MomentMapCalculator` | Domain (static) | 22 | 1 | 0 | 2 | ~3 | 0.0 | ⚠️ WMC 22 > 20 domain gate (just over) |
| `MomentMapService` | Application | 4 | 1 | 0 | 5 | ~2 | 0.0 | ✅ all |
| `MomentMapRequest` | Domain DTO | 6 | 1 | 0 | 2 | ~9 | 0.0 | ✅ all |
| `MomentMapResult` | Domain DTO | 5 | 1 | 0 | 2 | ~9 | 0.0 | ✅ all |
| `MomentMapRendererAdapter` | Infra (Unity) | 19 | 2 | 0 | 14 | ~8 | **0.75** | ❌ LCOM 0.75 > 0.5; CBO 14 at domain limit but OK as adapter |
| `IMomentMapAdapter` | Domain (iface) | 0 | 0 | 0 | 1 | – | – | port |
| `IMomentMapService` | App (iface) | 0 | 0 | 0 | 2 | – | – | port |

**Takeaways:**
- Coupling and complexity are now distributed; the pure `MomentMapCalculator`
  has perfect cohesion (LCOM 0) and CBO 2.
- Two values to flag: `MomentMapCalculator` WMC = 22 is marginally over the ≤20
  domain gate (the three moment-computation methods carry real cyclomatic
  weight), and `MomentMapRendererAdapter` LCOM = 0.75 **fails** the ≤0.5
  cohesion gate — expected for a Unity adapter holding many independently-used
  UI/render fields, but the one remaining smell.

---

## Example 2 — VoTable Export

### Before — single class `VoTableReader.VoTableSaver`

| Metric | Value | Range | Pass? |
|---|---|---|---|
| WMC | 7 | ≤20 domain | ✅ |
| DIT | 1 | ≤4 | ✅ |
| NOC | 0 | ≤5 | ✅ |
| CBO | 6 | ≤14 domain | ✅ |
| RFC | ~1 decl method (+calls) | ≤50 | ✅ |
| LCOM | 0.0 | ≤0.5 | ✅ |

All CK metrics already pass — the smell here is **not** captured by CK: a single
90-line static method (`SaveFeatureSetAsVoTable`, cyclomatic 7) doing document
building, headers, rows, and I/O at once. CK looks clean only because there is
one method.

### After — split into domain + infrastructure with ports

| Class | Layer | WMC | DIT | NOC | CBO | RFC≈ | LCOM | Notes |
|---|---|---|---|---|---|---|---|---|
| `FeatureCatalog` | Domain | 4 | 1 | 0 | 3 | ~2 | 0.0 | ✅ all |
| `VoTableExportService` | Infra (Persistence) | 14 | 1 | 0 | 8 | ~4 | 0.0 | ✅ all (WMC 14 ≤ 40 adapter) |
| `IVoTableExporter` | Domain (iface) | 0 | 0 | 1 | 2 | – | – | port, NOC 1 ✅ |
| `ICoordinateTransformer` | Domain (iface) | 0 | 0 | 0 | 1 | – | – | port |
| `IAstFrame` | Domain (iface) | 0 | 0 | 0 | 0 | – | – | port |

**Takeaways:**
- The monolithic method is decomposed into `BuildDocument` / `BuildHeaders` /
  `BuildRow` / `Export` inside `VoTableExportService`, and the work is moved
  behind the `IVoTableExporter` port so the domain (`FeatureCatalog`) depends
  only on the abstraction.
- **Every CK metric passes in every class**, before and after. The improvement
  is structural/SRP rather than a CK-number drop — each class stays well within
  gates, with ideal DIT/NOC/LCOM throughout. No inheritance cycles in either
  example.
