# Workshop 3 — Strategic scenarios

## What the method requires

Workshop 3 builds a **high-level** view of the attack paths:

- **map the ecosystem**: the stakeholders (customers, partners, suppliers...)
  and their **threat level** — dependence, penetration, cyber maturity, trust —
  which yields a **zone** (watch, control, danger);
- **build the strategic scenarios**: from a selected RO/TO pair, towards a
  feared event, possibly through stakeholders;
- describe the **attack paths** of each scenario;
- propose **ecosystem security measures** to reduce the threat level of
  critical stakeholders, then **re-assess** the residual threat level.

## In the tool

### Stakeholders

**Key stakeholders** section → **Add a stakeholder**: name, roles and
expectations, representative, category (Customer / Partner / Supplier / Other).
**From the library** offers typical stakeholders with indicative levels.

### Threat-level assessment

**Threat-level assessment** section: for each stakeholder, rate **dependence**,
**penetration**, **cyber maturity** and **trust** (1 to 4). The tool computes a
level and derives the **zone**. A different expert judgement is possible, with
justification.

Stakeholders in the **Control** or **Danger** zone are *critical* and define
the real perimeter of the ecosystem.

### Mapping

The **Mapping** section shows, as server-generated SVG:

- the ecosystem **threat radar** (concentric watch / control / danger circles,
  initial / residual toggle);
- the **tree** of strategic scenarios and their attack paths.

These diagrams are included in the **Workshop 3 PDF report**.

### Strategic scenarios and attack paths

**Strategic scenarios** section → **Add a strategic scenario**: choose the
**RO/TO pair**, the targeted **feared event** and describe the scenario.
**Severity** is inherited from the feared event.

For each scenario, **Attack paths** section → add one or more paths (for
example "direct attack" and "bounce via the outsourcing provider"). A path can
involve a stakeholder.

### Ecosystem measures

**Ecosystem security measures** section: add measures on critical stakeholders
(contractualisation, audit, access segmentation...). Then re-assess the
**residual threat level** (same four criteria): the residual radar updates.

## Validate

**Validate workshop** requires at least one strategic scenario with an attack
path. The PDF report includes the radar, the tree and the scenarios.

## Tips

- A strategic scenario stays **macro**: "the attacker compromises the
  outsourcing provider then reaches the production IS". Technical detail is the
  subject of Workshop 4.
- Threat level is not an accusation: a merely *negligent* stakeholder can be in
  the danger zone without hostile intent.
