# Registro da autorização GPR-C01.3a —27/10/2026

Transcrição do texto fornecido pelo desenvolvedor nesta conversa. Não é auditoria independente do código. As restrições originais abaixo foram alteradas apenas pelas decisões posteriores explícitas:

- Resposta sobre Arco: **Preservar hold somente com ChargedShot**.
- Ampliação: **“Continue, e se possivel, aplique também a alteração referente ao asura no RMB.”** Autoriza o adapter RMB/Asura; não autoriza skill X, especiais novos de Arco/Lança ou fase3b.
- Entrega: commit e push de todas as alterações acumuladas, inclusive anteriores; depois solicitar desligamento do computador.

## Texto original recebido

# TECH GUY — GPR-C01.2c Review and GPR-C01.3a Authorization

## 1. Review result

The developer has reviewed `GPR-C01.2c_RELATORIO.md`.

GPR-C01.2c is accepted as technically completed within its documented scope.

This is NOT final human acceptance of WASD movement feel or authorization for the entire GPR-C01.3 phase.

The developer's physical observations must remain tracked:

1. Walking animation feels unnatural when the character continuously faces the mouse cursor.
2. Basic attacks currently require clicking an enemy, which is incompatible with the new combat model.
3. Dash should eventually support a configurable direction policy: mouse cursor or WASD movement input.

Do not silently implement all three changes in this activity.

## 2. Mandatory preparation

Before changing code:

- Read AGENTS.md and the current canonical Tech Guy documents.
- Read the existing GPR requirements, technical design, tasks, ADR-0006, validation matrix and progress ledger.
- Discover and apply relevant installed custom Tech Guy skills and gamedev agent skills.
- Verify the real Unity Editor and project through Unity MCP.
- Record the current Git worktree, Editor state, relevant baseline tests and protected assets.
- Respect any active human play session, saves and prior development changes.

## 3. Current authorization — GPR-C01.3a ONLY

Implement the new Left Mouse Button basic attack input contract.

**Approved gameplay behavior:**

- Every deliberate LMB press requests one individual basic attack.
- Consecutive clicks can progress the existing weapon-specific basic combo.
- The attack must NOT require clicking directly on an enemy.
- Mouse cursor position defines the intended aim direction.
- An attack can execute and miss when no enemy is in range.
- Attack hit confirmation must come from the existing authored combat geometry and impact pipeline, not from cursor target selection.
- One click must not automatically execute the entire combo.
- Do not add unintended automatic repeat or unlimited buffering.
- Preserve responsive combo continuation within the existing valid windows.

## 4. Combat preservation

For Gauntlet:

- Preserve the current basic attack cadence and individual combo stages.
- Preserve the existing attack animation ownership and authored impact timing.
- Preserve contact-driven damage, Push/flinch, Stagger rules, recovery, hitstop and cancellation.
- Preserve skill-driven movement and current character-facing commitments.
- Do not automatically pursue or navigate toward the clicked enemy.
- Do not introduce aim snapping or lock-on behavior as a substitute for cursor-directed attacks.

For Bow:

- Audit existing tap/hold/ChargedShot contracts.
- Do not globally replace input release/hold semantics with press-only behavior.
- Preserve charged attacks and report any weapon-specific exception needed.

For Spear:

- Preserve the existing basic execution behavior while adapting its input to the new targeting contract.

Inspect the current attack and aim code first. Reuse existing attack requests, executors and hit resolution.

## 5. Facing and animation

The developer has reported unnatural locomotion animation while the character faces the mouse.

During this activity:

- Identify the existing movement-facing and aim-facing responsibilities.
- Verify that basic attack direction and animation agree with the intended cursor aim.
- Do not globally force character rotation toward movement direction while attacking.
- Preserve attack-specific facing locks and animation commitments.
- Document the smallest appropriate follow-up for directional locomotion, strafing, backpedaling and animation blending.

Do not replace rigs, animation controllers, clips or artwork without a separate validated design and authorization.

## 6. Scope restrictions

Do NOT implement:

- RMB Asura activation.
- A new Asura executor.
- A new Bow or Spear special.
- Fourth modular skill X.
- Full Q/E/R/X and Settings migration.
- Dash direction configuration.
- Unrelated enemy, boon, map or combat balance changes.
- A general rewrite of the input or animation systems.

The current RMB and Asura bindings must remain intact.

## 7. Required tests

Create or adapt focused regression tests proving:

1. A single LMB press produces at most one accepted basic attack.
2. One click does not automatically play the whole combo.
3. Consecutive clicks advance combo stages according to existing timing rules.
4. Clicking empty ground can produce a valid attack animation and impact attempt with no forced target.
5. Hit/miss results are determined by actual attack contact.
6. Existing cooldown, cadence, recovery and cancel contracts are preserved.
7. Mouse aim and WASD movement do not create unwanted click-to-move behavior.
8. Existing Bow tap/hold/charge and Spear basic behavior are not silently broken.
9. No extra attack fires after pause, UI interaction, lost focus or context release.
10. No double-counted damage or extra attack events are introduced.

Investigate ambiguous hold or buffering behavior instead of inventing new gameplay rules.

Use actual Unity MCP, PlayMode/EditMode tests, Game View captures and structured observations. Record exactly what was run.

Do not claim automated input is equivalent to physical human playtesting.

## 8. Documentation and reporting

Update the canonical design documents and tasks as needed.

At the end of GPR-C01.3a, produce a complete Markdown activity report in Brazilian Portuguese containing:

- Exact activity ID and completion status.
- Implemented behavior.
- Modified files.
- Skills actually used.
- Tests run, with exact results.
- Unity Console findings and captured evidence.
- Known regressions and limitations.
- Remaining movement/animation issues.
- Pending Asura/RMB migration decisions.
- Proposed next activity GPR-C01.3b and prerequisites.

Update the master progress ledger and canonical validation matrix.

**STOP after GPR-C01.3a. Do not implement GPR-C01.3b until the developer reviews this report and approves the next activity.**