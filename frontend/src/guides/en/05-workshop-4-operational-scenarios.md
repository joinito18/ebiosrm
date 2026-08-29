# Workshop 4 — Operational scenarios

## What the method requires

Workshop 4 goes down to the **technical level**:

- for each attack path from Workshop 3, describe an **operational scenario**;
- break it into one or more **operating modes** (possible technical variants);
- each operating mode breaks into **elementary actions** spread over the
  typical sequence **KNOW / GET IN / FIND / EXPLOIT**;
- each elementary action **targets a specific supporting asset** from
  Workshop 1;
- **rate the likelihood** of each operating mode (probability of success ×
  technical difficulty), which gives the scenario's overall likelihood.

## In the tool

### Start and create the scenarios

**Workshop 4** → **Start workshop**. Each attack path from Workshop 3 appears;
for each one, **Create operational scenario**.

### Operating modes

For a scenario, **Add an operating mode**:

- **Description** of the mode.
- **Elementary actions**: one line per action, with its **phase** (KNOW / GET
  IN / FIND / EXPLOIT), its **description**, the **targeted supporting asset**
  and, optionally, a **MITRE ATT&CK technique**.
- **Probability of success** (1 to 4) and **technical difficulty** (1 to 4).
  The official grid derives the **likelihood**; the matrix is shown live.

The **From the library** button offers typical operating modes (ransomware via
phishing, intrusion through an exposed remote access, bounce via a provider,
exploitation of a web vulnerability, Active Directory domination). A **library
suggestions** panel additionally proposes those whose keywords (name, actions,
MITRE techniques) match the attack path and the RO/TO pair. Either way, the
tool pre-fills the description, the ratings and the actions; **then remember to
map each action to the right supporting asset in your study** (the imported
target label is only a hint).

### MITRE ATT&CK techniques

The **technique** field offers an ATT&CK Enterprise catalogue filtered by EBIOS
RM phase. It helps describe the action and objectivise the likelihood. The
selected technique is included in the Workshop 4 report.

### Overall likelihood

The likelihood of an operational scenario is that of its **most likely**
operating mode (the most favourable to the attacker). A different expert
judgement is possible per mode, with justification.

## Validate

**Validate workshop** requires at least one operational scenario with a
complete operating mode. The PDF report details the operating modes, their
actions per phase and the likelihood.

## Tips

- Adjust the **granularity**: no need to describe 20 actions if 4 are enough to
  reason about likelihood.
- An elementary action always ends up **touching a concrete supporting asset**
  identified in Workshop 1 — this closes the chain.
- Two operating modes of the same scenario can have very different
  likelihoods: that is normal, the scenario keeps the worst.
