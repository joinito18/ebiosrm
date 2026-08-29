# Getting started

This guide explains how to run an **EBIOS Risk Manager** risk assessment with
the tool, workshop by workshop. Each article first recalls what the ANSSI
method requires, then walks through the matching screens.

## What EBIOS Risk Manager is

EBIOS RM is the digital-risk assessment and treatment method published by
France's ANSSI (version 1.5, March 2024, aligned with ISO/IEC 27005:2022). It
runs in **five workshops** that build on one another:

| Workshop | Purpose | Main output |
|---|---|---|
| 1 — Scope and security baseline | Frame the study, list business values and supporting assets, identify feared events, assess the baseline | Scope, feared events rated by severity |
| 2 — Risk origins | Identify the relevant "risk origin / target objective" pairs | Selected RO/TO pairs |
| 3 — Strategic scenarios | Map the ecosystem, build the high-level scenarios and their attack paths | Strategic scenarios, ecosystem measures |
| 4 — Operational scenarios | Describe the attack paths technically, rate likelihood | Operational scenarios, operating modes |
| 5 — Risk treatment | Assess the risk, decide on treatment, formalise the plan and acceptance | Treatment plan, accepted residual risks |

The method is **iterative**: you can go back, refine, then re-validate a
workshop.

## Creating a study

1. **Studies** menu → **New study**.
2. Fill in the **name**, the **mission** of the object under study and the
   **scope** (what is in the analysis and what is excluded).
3. The study is created as a *draft*, all five workshops empty.

You can also **import** a study (JSON export file) or **duplicate** an existing
study to reuse it as a template (Studies menu, row actions).

## Roles and sharing

A study has an **owner** and can be shared by e-mail with other accounts, with
three roles:

- **Reader**: view only, including reports.
- **Editor**: can edit workshop content.
- **Owner**: additionally can share, change roles and delete the study.

Every action is recorded in an **audit log** the owner can review.

## Navigating the tool

- Study **dashboard**: summary of key figures and access to the five
  workshops.
- **Sidebar**: workshop progress (draft / in progress / validated), access to
  the library, portfolio, reports and settings.
- Each workshop is **started**, filled in, then **validated**: validation
  freezes a version (*snapshot*) that feeds the reports and the change
  tracking.

## Validating a workshop

The **Validate workshop** button checks minimal completeness (for example: at
least one business value and one feared event in Workshop 1) then records a
dated version. You can reopen a validated workshop to correct it: a new
validation will create a new version, the old one stays available for
comparison.

## Going further

- **Library**: capitalise measures, risk origins, stakeholders, business
  values, supporting assets, feared events and operating modes from one study
  to the next — see the *Library* guide.
- **Compliance**: cross-reference your baseline and treatment plan with
  ISO 27001 or NIS2 — see the *Compliance* guide.
- **Portfolio and tracking**: steer several studies and follow how risk
  evolves over time — see the *Tracking and portfolio* guide.
