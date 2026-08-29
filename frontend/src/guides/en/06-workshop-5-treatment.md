# Workshop 5 — Risk treatment

## What the method requires

Workshop 5 concludes the analysis:

- **assemble the risk scenarios**: severity (of the feared event) × likelihood
  (of the operational scenario) → **initial risk level**;
- decide on **treatment** (reduce, transfer, avoid, accept) and formalise a
  **risk treatment plan**: measures, owners, deadlines, cost / complexity;
- **re-assess** the risk after measures → **residual risk**;
- **formally accept** the residual risks (with sponsor and justification when
  the residual stays high).

## In the tool

### Start the workshop

**Workshop 5** → **Start workshop**. Risk scenarios are assembled automatically
from the previous workshops.

### Risk scenarios

**Risk scenarios** section: each row crosses a strategic / operational scenario
with its severity and likelihood. The **initial risk level** is computed; an
expert judgement is possible, with justification.

### Treatment plan

**Risk treatment plan** section → **Create plan**, then **Add a treatment
measure**:

- **Wording** of the measure and **covered scenarios**.
- **Treatment axis**, **owner**, **deadline** (free text: `MM/YYYY`,
  `DD/MM/YYYY`...).
- **Cost / complexity** and **status** (to do / in progress / done).
- **Compliance codes** (ISO 27001 / NIS2) associated — see the *Compliance*
  guide.

**From the library** offers measures (ISO 27002, ANSSI hygiene, your measures).
The **→ library** button capitalises a study measure into your library. A
**library suggestions** panel also proposes measures matching the study's
content.

### Residual risk

For each scenario, **Assess residual risk**: re-rate the likelihood (and
possibly the severity) taking the measures into account. The **residual level**
is recomputed.

### Formal acceptance

**Formal acceptance** section: for each residual risk, record the **decision**
(accepted / not accepted) and its **class** (acceptable as is, tolerable under
control, unacceptable). When the residual is **high**, the tool requires a
**sponsor** and a **justification**.

## Validate

**Validate workshop** requires a treatment plan and an acceptance decision for
each high residual risk. Validation:

- generates the **PDF reports** (treatment plan, risk grid, residual mapping);
- creates a **version** (*snapshot*) — you can give it a **campaign label**
  ("2026 annual review") for the N / N-1 change tracking.

## Tips

- A measure can cover **several scenarios**; a scenario can be covered by
  **several measures**.
- Residual risk is re-rated on **likelihood** first: measures act mostly on how
  feasible the attack is, rarely on severity.
- An **accepted** risk must stay **recorded and reviewed**: see the *Tracking
  and portfolio* guide.
