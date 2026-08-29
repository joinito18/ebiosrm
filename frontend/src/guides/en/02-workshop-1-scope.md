# Workshop 1 — Scope and security baseline

## What the method requires

Workshop 1 sets the frame of the study:

- define the **business and technical scope** and the **participants**;
- identify the **business values** (information or processes whose compromise
  has an impact) and their **supporting assets** (what they rely on:
  applications, networks, people, premises);
- identify the **feared events** (harm to a business value) and assign a
  **severity** on the 1 to 4 scale;
- assess the **security baseline**: the gap between measures already in place
  and a reference framework (ANSSI hygiene rules, ISO 27002...).

## In the tool

### Start the workshop

From the study dashboard, open **Workshop 1** then **Start workshop**.

### Business values

**Business values** section → **Add a business value**:

- **Description**: the information or process ("Payroll process", "Customer
  master data").
- **Owning entity**: the responsible department (in the ISO 27005 *risk owner*
  sense).

The **From the library** button pre-fills these fields from a catalogue of
typical business values.

Method recommendation: aim for **5 to 10 business values**. The rule is
flexible, the tool does not block.

### Supporting assets

**Supporting assets** section → **Add a supporting asset**:

- **Associated business value**: a supporting asset serves at least one
  business value.
- **Description** and **type**: Information system, Network, Human resources,
  Premises.
- **Owning entity**.

**From the library** offers typical supporting assets (AD directory, mail, ERP,
server room...), filterable by type.

### Feared events

**Feared events** section → **Add a feared event**:

- **Associated business value**.
- **Description** of the harm ("Prolonged unavailability of the production IS",
  "Disclosure of the customer file").
- **Severity** from 1 (minor) to 4 (critical).

Severity can be **re-rated** later (dependent scenarios are then recomputed in
Workshop 5). **From the library** offers typical feared events with an
indicative severity to adjust.

### Security baseline

**Security baseline** section → **Create baseline**. Two ways to add a control:

- **ISO/IEC 27001:2022 Annex A**: pick a control from the catalogue, state its
  **status** (Compliant / Non-compliant / Not applicable) and, where relevant,
  the **current state** (what is actually done).
- **Custom framework**: enter the wording, status and theme yourself.

The baseline feeds the **compliance table** (*Compliance* guide) and appears in
the Workshop 1 report.

## Validate

**Validate workshop** requires at least one business value and one feared
event. Validation generates the **Workshop 1 PDF report** (study identity,
business values, supporting assets, rated feared events, baseline gap).

## Common mistakes

- Confusing **business value** (the *what*, business-side) and **supporting
  asset** (the *support*, technical).
- Rating severity based on probability: at this stage severity depends **only
  on impact**, not on likelihood (which comes in Workshop 4).
- Over-fragmenting supporting assets: stay at a level useful for what follows
  (a supporting asset = something an attack can target).
