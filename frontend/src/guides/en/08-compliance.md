# Compliance

The **Compliance** module cross-references a study's content with a regulatory
framework to produce a **coverage table**.

## Principle

For each requirement of the framework, the tool looks at:

- the status of the matching controls in the **security baseline**
  (Workshop 1);
- the **treatment-plan measures** (Workshop 5) carrying the matching
  **compliance code**.

It derives a **coverage**: Compliant, Partial, Not covered, Not applicable.

## Available frameworks

- **ISO/IEC 27001:2022** — the 93 requirements of Annex A.
- **NIS2** — the 10 areas of Article 21, with an **indicative mapping** to the
  ISO controls (a NIS2 requirement is considered covered at the level of
  `max(direct measure, associated ISO controls)`).

> The ISO → NIS2 mapping is indicative and must be validated by the analyst.

## In the tool

### Associate a compliance code with a measure

In Workshop 5, on a treatment measure, the **Compliance** selector lets you
tick the ISO 27001 / NIS2 codes the measure addresses (multi-select chips).

### View the table

Study **Compliance** menu (or the link from the dashboard). Pick the framework:
the table lists each requirement, its coverage, the baseline status and the
measures that treat it. A box gives the number of applicable requirements
addressed.

### PDF annex

The **Download the compliance annex (PDF)** button produces a document with the
table, to attach to an accreditation file or an audit.

## Tips

- Filling in the compliance codes **as you go** in Workshop 5 avoids a large
  after-the-fact mapping effort.
- "Not applicable" is a legitimate answer: justify it in the baseline status or
  the measure.
- Compliance is not the goal of EBIOS RM (which targets risk) but a **useful
  by-product** to demonstrate regulatory coverage.
