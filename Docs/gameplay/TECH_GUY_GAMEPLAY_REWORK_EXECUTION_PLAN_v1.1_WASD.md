# TECH GUY — Gameplay Rework: Execution Plan, Approval Gates & Activity Reports

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



**Document type:** Execution directive and staged implementation plan for Codex (Unity 3D / C#).  
**Version:** 1.1 — 2026-10-26 (new WASD + mouse-combo control direction).  
**Decision status:** **New control direction approved in principle by developer; GPR-00 audit/design only authorized.** Control implementation and later phases require explicit developer approval.  
**Primary product-vision reference:** `TECH_GUY_GAMEPLAY_MUTACOES_SINERGIAS_E_SKILLS_MODULARES(1).md` (the supplied design proposal; locate its actual repository path or request it to be placed in the repository—never assume that this filename is already in the repo).  
**Developer-facing language:** Brazilian Portuguese in reports, decisions, and human-playtest instructions. Technical identifiers and code may remain in English.

> **Core design intent**: The weapon defines how the player *starts* fighting; the acquisitions, modular skills, and choices made **during a run** define what that fighting style *becomes*. The result must change **mechanics, tactical decisions, and visual identity** while preserving responsive action combat.

---

## 1. Mission and non-negotiable rules

Evolve the existing Tech Guy ARPG/roguelite toward:

1. **A compelling weapon core from minute one.** Gauntlet remains responsive and enjoyable without requiring an RNG jackpot.
2. **Mutable basic attacks.** A basic-attack-focused build is a viable playstyle, not merely a resource generator.
3. **Compositional boons and emergent synergies.** Two or three compatible acquisitions can produce a meaningful *combined behavior*, not only summed percentage bonuses.
4. **Modular active skills acquired and replaced during the run**, with an explicit policy for protected/inherent moves, investments, cooldowns, and dependent boons.
5. **Mechanical and visual transformations coupled to the actual run build**, using modular and reversible presentation, not one character prefab for each combination.
6. **Weapon identity maintained.** Gauntlet = close-range commitment/boxing/impact; Bow and Spear later demonstrate their own semantics rather than copies of Gauntlet effects.
7. **Fair, readable encounters.** Player effects never obscure essential enemy tells, the player silhouette, HP/resources, or boss mechanics.
8. **A traceable run.** Reward decisions, skill changes, damage provenance and derived effects remain consistent in the existing run and results systems.
9. **Direct action-game controls:** WASD movement, mouse aiming and two mouse attack channels, with exactly three remappable modular skills on Q/E/R and a permanent RMB weapon special. This replaces click-to-move as the intended gameplay interaction; specific secondary combo semantics and legacy action remapping require audit and explicit design sign-off.

**Never** sacrifice the validated input/contact/reaction improvements of Gameplay Intelligence 1.0–1.8 to make modularity easier. No basic-hit stun-locking of common/Elite/Boss AI, no duplicate damage, no ghost hits, no unbounded proc chains, and no hidden changes to attack cadence or cancel windows.

**Do not** treat the supplied proposal's sample class names, boons, tags, YAML or tier values as existing Unity APIs/assets or approved tuning. **The WASD + mouse + Q/E/R/X direction in this version is an explicit newer developer decision and supersedes legacy control examples**, but must not be presumed implemented.

## 2. Mandatory discovery and evidence discipline

Before any task:

1. Read the **actual** `AGENTS.md` and repository instructions, and discover/read/apply the project's **existing design skills** (combat, game vision, boons, enemy encounters, art, UX/HUD, Unity acceptance as appropriate). **Never invent skill names or paths.**
2. Read the product-vision document **in full**. Read the current canonical docs and especially the actual equivalents of `02_ESTADO_E_LACUNAS`, `03_PRIORIDADES`, weapon/combat/boon/run/HUD documentation, the latest validation matrix and playtest checklist. **Discover real paths**; the titles are search hints, not permission to manufacture files.
3. Verify the correct project and active Editor via **Unity MCP**. Historical project information points to `D:/Dev/Personal/My_games/Tech-Guy` and Unity `6000.5.10f1`, but **verify the live state** rather than relying on history. Do not operate on a stale clone/worktree.
4. Record `git status`, current revision plus dirty-file state, scene/PlayMode state, available disk space, Console baseline, and any active human run. Preserve intentional Git deletions, existing `.meta`/GUIDs, uncommitted changes, and player currency/unlocks/loadout/RunSummary. Do not automatically restore old snapshots over newer player progress.
5. Inspect existing systems before suggesting new ones. Known documented candidates include the basic-attack profiles/ImpactEvents, `AbilityHolder`, weapon executors, `RunBoons`/`HookBus`, Protocol/Mutation execution, `ProtocolBudget`, damage transaction/causal attribution, reward tickets, and `RunSummary`. Names are research leads, not proof of current signatures or ownership.
6. Use Unity MCP for import/compilation, EditMode/PlayMode regressions, **real Game View screenshots**, and preservation of Editor state when available. If MCP or scene execution is unavailable, mark validation **BLOCKED**, do not invent green results.
7. For feel/UX, distinguish **automated tests**, **automated gameplay**, **screenshots/video actually inspected**, and **human physical playtests**. Only the developer can approve subjective game feel and visual identity.
8. Prefer modifying the established owners to adding managers, event buses, attack clocks, damage pipelines, duplicate inventory stores or new persistent schema.

### Existing facts and known caution areas

- Gameplay Intelligence 1.0–1.8 and the run analytics/results vertical slice have documented technical gains. They are **regression baselines**, not blanket human acceptance.
- The GI1.9 human feedback describes **boons that do not transform play enough**, repetitive/basic enemies, limited spatial decisions, similar-looking enemy archetypes and apparently ineffective overhead/ranged area feedback. These issues inform design; they are **not** permission to rebalance enemy AI or assume a specific broken attack executor in this rework.
- The real Gauntlet controls/finisher semantics need reconciliation. Older descriptions protect an “E finisher”; more recent playtest docs distinguish the E skill, an Asura action bound to F11 in that tested configuration and a contextual finisher on F. The developer has now selected **WASD movement, left/right mouse as basic-combo attack inputs, and Q/E/R/X remappable skills**. **Audit the current build, ability ownership and inputs before designing replaceable slots or migrating any legacy ability binding.** Do not arbitrarily remove or overwrite characteristic actions.
- The existing route uses authored reward ordinals, Mutation/Elite and boss gates, and randomizes FirstSector layouts/rosters. Modular rewards must not silently violate those contracts.
- Some historical tests fail due to retired boon fixtures, order/timing sensitivity and a BurnTick saturation case. Record their **current reproducibility and scope**; do not revive retired content, weaken assertions, or falsely claim a global green suite.
- Existing enemy-impact fixes, Sniper warning/shot consistency, projectile pooling across repeated runs, and run-summary no-double-counting are specific regressions to preserve.

---

## 3. Governance: activity -> report -> review -> next task

**An activity is the smallest planned, verifiable unit of work** (e.g. `GPR-C01.2` or `GPR-01.2`, not an entire phase). **At the end of EVERY activity, Codex MUST generate a `.md` activity report**—even if the task fails, is blocked, or was an audit with no code changes. A short factual report is preferable to a long redundant narrative.

### Required workflow for every activity

1. **Open:** state its ID, goal, input artifacts, scope, exact acceptance checks and dependencies. Verify baseline; explicitly list any known limitations.
2. **Execute within approved scope:** audit/design or implement. For behavior changes, establish reproducer/failing regression before a fix when feasible.
3. **Validate:** run relevant Unity tests and visual/gameplay comparisons. Do not equate compile success or a video frame with feel approval.
4. **Close:** write the activity report using the template in §9, update a concise master progress ledger and the canonical validation matrix; use the repository's **existing documentation conventions** instead of assuming a new directory. Add a new `reports/` location **only if no suitable convention exists and document why**.
5. **Gate:** state `COMPLETED`, `PARTIAL`, `BLOCKED`, `FAILED`, or `AWAITING_HUMAN_APPROVAL`, and separately state evidence classification (`IMPLEMENTADO_NAO_VALIDADO`, `VALIDADO_TECNICAMENTE`, `VALIDADO_EM_GAMEPLAY`, `APROVADO_PARA_ENTREGA`, as applicable). Do not confuse task completeness with human acceptance.
6. **Stop at the approval boundary.** Ask the developer to review a design decision or phase completion. **Do not automatically start the next phase.** Within an approved phase, Codex may proceed to the next clearly pre-approved, dependency-safe activity, but must report each activity individually; stop for conflicts, regressions, ambiguous gameplay decisions or architectural rewrites.

### Master progress ledger (maintain throughout)

Locate/reuse the current project progress tracker if one exists. Otherwise create a minimal `GAMEPLAY_REWORK_PROGRESS.md` beside the execution plan in a suitable discovered docs location. Required columns:

| Activity ID | Title | Status | Last updated | Changed files | Tests/evidence | Activity report | Approval/next gate |
|---|---|---|---|---|---|---|---|

Do **not** merge all activity reports into an ever-growing validation matrix. Maintain short per-activity reports and update the matrix with a compact evidence summary and link. Avoid duplicating whole technical sections in several documents.

### Approval gates and authority

- **This handoff authorizes GPR-00.1 through GPR-00.5: read-only audit and design artifacts only.** No gameplay runtime, prefab, scene, save or authored-balance changes.
- After Phase 0: **STOP** and present the audited architecture, unanswered decisions, phase-1 scope, trade-offs and approval options.
- A later explicit approval of a phase authorizes only its stated activities, acceptance checks and scoped changes. A discovered bug does not expand scope automatically.
- No mass migration, all-weapons rewrite, rework of enemies/maps/Nexus, new meta tree or large content batch without separate approval.

---

## 4. Proposed staged roadmap

This reorganizes—but does **not** replace—the original vision's proposed phases 0–6. Original “engine -> visuals -> skills” order is intentionally changed to **prove an end-to-end playable transformation early**. The original vision remains the source of product goals; conflicts require explicit decisions, not silent edits.

| Phase | ID | Purpose | Output / approval gate |
|---|---|---|---|
| 0 | **GPR-00** | Baseline audit, controls redesign and minimal vertical-slice design | Reuse map, exact Gauntlet slot/finisher contract, requirements/design/tasks and risk register; **human approval required** |
| C | **GPR-C01** | **WASD + mouse action controls foundation, before gameplay transformations** | Playable and remappable movement/attacks/skills; preserved Gauntlet feel and UI safety; **human control approval** |
| 1 | **GPR-01** | **Playable thin vertical slice** of mutated Gauntlet basic + small boon synergy + one safe modular skill swap + minimal visual manifestation | Functional loop in one small representative run flow; pre/post regressions and **human feel review** |
| 2 | **GPR-02** | Robust modifier composition, conflict/priority, three-way synergy and reversible cumulative visual layers | Deterministic composition, proc budget, distinct visuals and cleanup; **human approval** |
| 3 | **GPR-03** | Reward economy, modular skills, specializations and distinct builds through a real run | Two strategically distinct Gauntlet builds; valid skill offers; repeated runs; **human playtest** |
| 4 | **GPR-04** | Limited sideways metaprogression in the lobby | Meaningful starter choice without mandatory power grind; **human UX approval** |
| 5 | **GPR-05** | Apply validated model to Bow and Spear | Same semantics, different weapon identity; **human feel approval by weapon** |
| 6 | **GPR-06** | Content expansion, balance, accessibility, art/performance polish | Scalable content and measurable budgets; **release criteria approval** |

### Phase GPR-00 — Audit and design only (AUTHORIZED NOW)

**GPR-00.1 — Read, inventory, baseline.** Inspect AGENTS, actual design skills, product vision, canonical docs, current code/assets and the current Unity project. Map actual weapons, basic, skills, boons/Mutations/Protocols, damage/proc budgets, run, rewards, HUD, appearance and tests. Record tested vs historical vs unknown facts. **Output:** inventory + risk/status baseline + activity report.

**GPR-00.2 — Mechanical ownership and gap map.** Trace Gauntlet command -> commit -> ImpactEvent -> confirmed damage -> secondary proc -> animation/VFX -> RunSummary. Trace acquisition -> reward ticket -> equip/replace -> removal -> cleanup on new run. Classify each required capability **EXISTS / REUSE / EXTEND / NEW / UNDECIDED / NOT VERIFIED**, with code/asset owner, risk, regression. **Output:** dependency/ownership map + activity report.

**GPR-00.3 — Product decisions and thin-slice specification.** Audit the legacy Q/W/E/R slot setup and design the new Q/E/R/X defaults, Asura and finisher ownership; nominate one safe, replaceable skill for the slice or propose a compatibility adapter without touching protected actions. Define how skill acquisition/replacement affects spent investments, modifiers, cooldowns, charges, HUD and save/run state. Describe 2-item and later 3-item synergy semantics, what can proc from derived damage, modifier conflicts and visual priority. Compare at least two approaches when structural changes would be significant. **Output:** decision record with `APPROVED_EXISTING / PROPOSED / NEEDS_DEVELOPER_DECISION`, detailed proposed GPR-01 acceptance conditions + activity report.

**GPR-00.4 — Implementation-ready planning package.** Produce repository-aligned **requirements**, **technical design**, and **granular tasks**, with traceable IDs, risks, test plan, current integration points and rollback. Select a *small* representative Gauntlet slice: one basic mutation with visible mechanical change, one second compatible boon, one safely replaceable active skill, minimal non-final visual change, one meaningful room/reward acquisition route. Include one optional three-way synergy for GPR-02, not all 12 illustrative boons. Establish pre-change baseline videos/screenshots, test filters and acceptance gates. **Output:** preliminary phase-0 planning package + activity report + updated ledger. Continue to GPR-00.5, then finalize the package and STOP for approval.

**GPR-00.5 — New input/movement design and migration planning (MANDATORY).** Inspect real legacy click-to-move, click-to-attack, mouse pointer aiming, NavMeshAgent, camera, facing, dash, skills, hold attack, interaction prompts, Asura, finishers, UI focus/input gating, pause, reward choices, rebinding and any controller support. Define stable semantic actions and Q/E/R/X display mapping without changing underlying skill slot identity. Write testable specifications and migration tasks for **GPR-C01**. Identify all default binding conflicts (notably E-interaction versus E-skill and F-finisher versus F-interact) and propose solutions for explicit approval. **Do not implement controls in Phase 0.** Output action/binding/ownership/conflict matrix, input architecture selection, user playtest, regression matrix and activity report. Update GPR-00.4 plan package to reference GPR-C01.

**GPR-00 exit gate:** Developer explicitly approves (a) protected/inherent action policy, (b) allowed replaceable slots, (c) the precise first two boons/skills and interaction, (d) skill-investment/cooldown handling, (e) minimal runtime change list, and (f) what visual proof counts as sufficient, **plus (g) WASD input architecture, mouse-button combo policy, default binding conflict resolution and rebinding UX**. If undecided, list concrete alternatives, trade-offs and recommended default; **do not silently decide**.

### Phase GPR-C01 — Direct control and movement foundation (BEFORE GPR-01; NOT YET AUTHORIZED)

**This is a gameplay-input migration, not a request to replace the combat engine.** Preserve stable action identities, serialized assets, visual hit timing and actual gameplay behavior. Authorize this phase only after GPR-00 audit and design approval.

- **GPR-C01.1 — Input audit + regression baselines.** Record current movement click path, agent/camera/rotation ownership, target raycasting, mouse attacks, ability key indexes, dash, interact and finisher bindings, HUD/menu prompts and automated tests. Capture mouse/keyboard physical playtest baseline and current Unity MCP tests. Define an action-centric binding table; report.
- **GPR-C01.2 — Direct WASD movement.** Implement responsive, camera-relative or agreed world-relative planar input (choose from actual camera contract), normalized diagonals, collision/NavMesh/world limits, input disabled during menus/CC/locked states, motion/animation and facing coherence. No click-to-move in gameplay; cursor remains for aiming/targeting and menu selection. Avoid physics/agent competing controllers, path warping, root-motion desync and movement during pause. Record numerical baselines instead of inventing speed/acceleration.
- **GPR-C01.3 — Mouse attack/combo channel.** LMB = primary basic combo; RMB = secondary basic/combo command with meaningful differentiated behavior, designed for Gauntlet first. Basic sequences may mix LMB/RMB, with explicit timing/cancel/hold/targeting rules and no duplicate hits/costs. Aim by cursor, independently from movement; never let RMB or LMB accidentally issue a movement destination during gameplay. Define what RMB does for Bow/Spear before enabling their control route, ensuring no weapon becomes nonfunctional.
- **GPR-C01.4 — Q/E/R/X skill binding migration + complete rebind UI.** Keep exactly four logical skill slots, with defaults Q,E,R,X but without changing skill identities/scheduler/cooldowns. Move legacy E-interaction to a distinct semantic `Interact` action; preserve weapon special/Asura and contextual finisher using explicit approved mappings, not silent deletions. Implement action-based rebinding/persistence/reset/conflict detection, keyboard/mouse primary and optional secondary bindings, correct icon/text prompts and UI focus gates, rejection of unusable/duplicate assignments. Verify repeated run/scene sessions and no impact on player prefs. Update HUD/UI to reflect currently bound actions.
- **GPR-C01.5 — Integrated feel and regression gate.** Real developer physical mouse+keyboard encounter with strafing while attacking, diagonals, dash direction, moving/aiming independently, primary/secondary combos, Q/E/R, menus, rewards, interactions, pause, transition, death, Nexus and FirstSector. Run Unity focused tests for hit timing, mana, Asura, cancel, proc ownership, multi-weapon attack/skill slots, InputSystem device hotplug/rebinding, pointer over UI and Editor PlayMode lifecycle. Compare baseline captures and record human verdict. **STOP for developer approval before GPR-01.**

**GPR-C01 exit gate:** The developer can fight intuitively using WASD + LMB basic + RMB special + Q/E/R, modify bindings in Settings, interact with hub/rewards without attacks leaking through UI, retain expected attack impact/cancel/ability identity, and explain the assigned role of every remaining special move. Human feedback and technical proof are both required.

### GPR-01 — direção experimental aceita / fundação01.1 concluída / conteúdo aguardando aprovação

Plano vigente detalhado em [GPR_01_VERTICAL_SLICE.md](GPR_01_VERTICAL_SLICE.md). C01 está completa;01.P1 aceita;01.1 autorizada exclusivamente e concluída tecnicamente. O pedido mais recente traz três modificadores à primeira fatia, antes previstos para GPR-02.

- **01.1 CONCLUÍDA:** Basic no grafo/restore, receipt→reserva de filho, permissões opt-in da linhagem e aquisição sem reset;90 EditMode+120 PlayMode selecionados. STOP para revisão.
- **01.2:** A/Punhos Titânicos: terminal LMB confirmado gera onda frontal limitada e attachment visual da run, sem alterar timings/cancel/base.
- **01.3:** B/Conductor existente compõe com a onda, mesma raiz/budget, ordem equivalente.
- **01.4:** C/Detonation existente cria consequência on-kill em posição distinta, caps específicos do novo ramo.
- **01.5:** oferta/tickets/seed legal, seis ordens, duas runs/Nexus/reset, telemetria e visual/performance proporcionais.
- **01.6:** relatório e aceite humano apenas da nova mutação/sinergia/visual; sem reabrir aceite completo dos controles.

**Saída proposta:** base/A/A+B/A+B+C demonstrados; Q nativo integra aproximação; visual reversível; custos/recursos/cancel/reação/HP legítimos; tickets e cooldowns preservados. SwapAvanço→Uppercut é adiado para manter o menor recorte e depende de concordância nesta revisão. Somente01.1 autorizada e executada. STOP para revisão da fundação e nova autorização01.2; nenhum conteúdoA/visual implementado.

### Phase GPR-02 — composição geral e visuais cumulativos (FUTURA)

A única combinação de três vias da proposta01 não autoriza antecipar o framework geral, ecos ou demais conteúdos desta fase.

- **GPR-02.1:** formalize modifier priorities, additive/transformative/exclusive resolution, tags/capabilities compatible with existing types; tests for ordering, exclusivity, acquisition order and deterministic choice.
- **GPR-02.2:** demonstrate **one** three-way synergy whose result is recognizable, with strict primary/secondary/proc eligibility and bounded depth/work. Record exactly which child effect can inherit which modifier.
- **GPR-02.3:** two simultaneous visual changes on the actual character/attack; resolver derives output idempotently from current run state, cleans up on skill swap, death, disable, end-run and next run. Shader/material/rig budgets and fallback behavior.
- **GPR-02.4:** test extreme proc cascades (8/32/64 targets if relevant), pause/hitstop/pooling, performance and overlapping elite/boss telegraphs. Human check of advanced build readability. **STOP for approval.**

### Phase GPR-03 — Reward ecosystem + modular skills (FUTURE)

- **GPR-03.1:** extend from one safe skill swap to authored modular choices, replacing/upgrading a skill, with explicit refund/retention/conversion policy for prior investment and dependent boons.
- **GPR-03.2:** reward planner that filters incompatible/dead choices and provides appropriate opportunities to shape basic-focused vs skill-focused builds, without forcing one synergy or breaking existing acquisition ordinals, Mutation/Elite/boss gates.
- **GPR-03.3:** UI: equip/replace/cancel, before/after behavior, compatibility and what happens to investments; support currently active real input modes and HUD responsibilities.
- **GPR-03.4:** play **two** contrasted Gauntlet builds (e.g. heavy control/impact vs high-frequency mark-and-consume), confirming different decisions, pacing and visuals, not only DPS/color. Validate repeated Nexus→run→Nexus cycles and real summary provenance. **STOP for approval.**

### Phase GPR-04 — Sideways metaprogression (FUTURE)

- **GPR-04.1:** choose a *small* subset of skill/starting-option unlocks; don't build a complete tree.
- **GPR-04.2:** prove an equipped starter option is a sidegrade with no required grind to make the first run fun.
- **GPR-04.3:** test persistence, unlocked pools and lobby UI, without requiring Nexus spatial/art redesign. **STOP for approval.**

### Phase GPR-05 — Bow and Spear identity (FUTURE)

- **GPR-05.1:** inspect Bow/Spear actual core; design one mutation per weapon sharing an abstract property but using weapon-specific mechanics/geometry/presentation.
- **GPR-05.2:** test at least one cross-weapon modifier such as compatible fragmentation/echo with proper eligibility and documented exclusions.
- **GPR-05.3:** validate technical correctness and human feel separately, preserving attack patterns, projectile trajectories, sweet spots, ranges and constraints. **STOP for approval.**

### Phase GPR-06 — Expansion and polish (FUTURE)

- **GPR-06.1:** content authoring workflow and catalogue curation, add only mechanics that diversify decisions.
- **GPR-06.2:** balance offer probabilities, run duration, proc thresholds and performance using actual measured data.
- **GPR-06.3:** final visuals/SFX, accessibility and enemy telegraph priority; large mixed encounters.
- **GPR-06.4:** review end-of-run UI, causal stats, persistence, content validity and release test matrix. **STOP for approval.**

---

## 5. Required cross-cutting technical design questions (resolve in GPR-00)

### 5.0 Developer-approved control direction and required input contracts

**Authoritative interaction goals (not statements about existing code):**

| Semantic player action | New default | Contract and caveat |
|---|---|---|
| Move | W/A/S/D | Direct directional movement, independent of pointer; normalize diagonal speed; honor collisions, CC, pauses and current animation/cancel constraints. |
| Aim / select target | Mouse position | Cursor raycast/project to gameplay plane, rotation/facing policy verified per weapon; movement never auto-selects an enemy. |
| Primary basic / combo | Left Mouse Button | Basic combo from the **current weapon's existing attack system**; preserve authored contact and response. |
| Especial permanente da arma | RMB | Especial atual, Asura na Manopla; executor/recursos existentes, sem básico concorrente. |
| Skill slot 1 | Q | Skill logical index 0, after verifying the current actual slot mapping. |
| Skill slot 2 | E | Skill logical index 1, after verifying the current actual slot mapping. |
| Skill slot 3 | R | Skill logical index 2, after verifying the current actual slot mapping. |
| Quarta modular / X | Nenhuma | Não existe; index3 legado é o especialRMB e não um slot modular. |
| Dash/dodge | Space | Provisional existing default; agree on movement-vector direction versus cursor/facing fallback and test priority. |
| Interact | G, configurável | Ação própria; finisherF separado, sem conflito com skills. |
| Weapon special / Asura | C (RECOMMENDED) | Migrates inaccessible F11 default while preserving the mechanic; exact action must be audited and approved. |
| Contextual finisher | V (RECOMMENDED) | Prevents ambiguity with Interact on F; only bind if separate action exists; confirm whether any execution belongs to the weapon's E-skill instead. |
| Pause / menu | Escape | Preserve current menu/state ownership, with UI gate and release barrier. |

**Approval distinction:** WASD, mouse clicks for basics/combos, Q/E/R/X for four skills and customizable controls are explicit developer requests. RMB chain behavior, Interact=F, Asura=C, Finisher=V, Space dash aiming, camera-relative motion and controller bindings are **recommendations to validate**, not approved facts. Never claim those defaults already exist or are chosen irrevocably.

**Mandatory technical specifications:**

- Preserve **semantic action IDs** independent from defaults: e.g., `SkillSlot1..4`, `PrimaryBasic`, `SecondaryBasic`, `Move`, `Aim`, `Dash`, `Interact`, `WeaponSpecial`, `ContextFinisher`. These are descriptive names, not requests to introduce duplicate classes or rename existing serialized enums.
- The Input System configuration/legacy compatibility (`Active Input Handling`), existing rebind facilities and ownership of `InputAction` maps must be audited. Prefer extending the project's existing input abstraction; do not create competing input listeners. If actions are currently polled through legacy methods, propose a controlled migration with parity tests.
- Preserve independent movement and aim: while holding WASD, mouse clicks should perform attacks toward the current cursor/eligible target without turning into path destinations. When cursor is over gameplay UI, world attacks should be gated consistently; UI clicks remain operational.
- Define light/heavy/alternate-chain **state transitions**, command buffering/cancellation and hold/repeat behavior without widening windows or allowing attacks during protected states automatically. Secondary basic must not bypass damage/Asura accounting.
- Movement and combat must agree on locomotion animation, body rotation, hitstop, dash root/aim priority and attack recovery; no unapproved ability to kite during root-locked actions. Reuse existing NavMesh/collision world and author directional locomotion as needed.
- Rebinding: changes take effect without reopening the game; persist via existing approved settings source; display live current mappings in HUD, ability slots, interactive prompts and tutorials. Conflict warnings, reset defaults, inaccessible keys, reserved OS inputs, left/right mouse swap and keyboard layouts must be covered. Do not make a setting-only UI that still invokes hardcoded Q/E/R/X input checks.
- Player input and UI interaction must not fire simultaneously; add explicit gating for reward cards, menus, pause, typing, dialogs, drag events, Nexus pedestals, scene transitions, hover and input-release across open/close. Keep single EventSystem and avoid concurrent InputSystem/legacy consumption.
- Gamepad parity is a separate compatibility scope unless the active build already supports it. Never silently regress an already supported device; define action semantics without tying game logic permanently to specific keyboard keys.
- Record input acceptance and attack commit in existing observation channels; test physical mouse+keyboard as well as virtual device fixtures. Save schema migration must be backwards-compatible and preserve user profiles.

### 5.1 Identity and move ownership

Create a definitive **action ownership table** for Gauntlet: input/binding -> action -> owner -> always available or replaceable -> cancel rules -> resource source -> animation/ImpactEvent -> consequence of replacement. Include old and new move keys, LMB/RMB as separately identified basic-combo inputs, Q/E/R/X as the proposed four default keys, dash, Asura, interaction and contextual finisher. Explain apparent historic mismatch of “E finisher” vs F/F11; trust live source, do not guess.

### 5.2 Basic-mutation composition and AttackContext

Use existing combat contexts and authored events. Specify when the snapshot is taken (commit versus impact), which changes can affect the current execution versus only the next, ordering of shape/quantity/trajectory/status, owner/generation IDs, primary/secondary/proc sources, cancellation semantics, duplicate prevention, and what happens when an item is acquired/removed mid-action.

### 5.3 Proc policy and causal analytics

Reuse `ProtocolBudget`/existing ledger and source attribution. Prevent repeated procs on the same source/target unless explicitly authored; define `can_trigger_from_primary`, `can_trigger_from_secondary`, `can_trigger_from_proc`, depth/budget/ICD and cleanup. Children must link to the originating skill while summaries remain **mutually exclusive in totals**. Specify reproducibility strategy based on actual RNG/seeds; avoid global RNG-order assumptions.

### 5.4 Safe modular skill replacement

Define atomic transaction boundaries, replacement of an actively casting/queued/recovering ability, cooldown/charges/resources/Asura compatibility, effects persisting after the old skill is removed, modifiers referencing removed skill, refresh of HUD, rollback on failure, undo/removal at run end and persistent starter choices. No mutation of shared `ScriptableObject` definitions to store current-run state.

### 5.5 Visual-state reconciliation

Derive visual state from authoritative run state, not additive side effects that accumulate forever. Define layered attachments/material properties/VFX and priority between temporary status, permanent-for-run mutations and exclusive transformations. Reconcile after equip, removal, scene load, pooling, death, restart; cover clipping and multiple simultaneous modifications. Critical enemy/boss warnings win visual priority over player spectacle.

### 5.6 Reward eligibility and run flow

Assess the current actual reward tickets, cardinality/ordinals, slot conflicts, Mutations and route gates. Show how a skill-required boon becomes eligible; how invalid options are filtered and replaced; how essential early *opportunities* can be offered without hardcoded room numbers or guaranteed legendary builds; and how input-release/focus/navigation are preserved in reward screens.

### 5.7 Observability and acceptance

Prefer existing GameplayObservation + RunSummary/RunStatistics. Distinguish actual HP damage from overlaps in `HookHit`, `BasicHit`, `ProtocolImpact` and damage-transaction receipts. Provide before/after combat clips and screenshots with scene/seed/weapon/camera/resolution. Measure baseline before choosing numerical frame-time/proc budgets. Do not promise full human visual approval from telemetry.

---

## 6. Vertical-slice acceptance matrix (proposed, to confirm in Phase 0)

| ID | Minimum observable acceptance condition | Required evidence |
|---|---|---|
| VS-CTRL | Approved GPR-C01 input system works: WASD move, cursor aim, LMB/RMB basic combo, four remappable Q/E/R/X skills | Physical playtest + remapping persistence, UI gating and Unity regression tests |
| VS-A | Gauntlet base usable in first encounter with no boon | Playable scene, screenshot/video, human input feel |
| VS-B | One acquired boon changes basic geometry/behavior meaningfully | Authoritative event timeline + real combat capture |
| VS-C | Second modifier composes with the altered action | Output effect and damage/context assertions; screenshot |
| VS-D | One active skill is acquired/replaced in approved slot at safe reward boundary | UI choice, atomic state tests, cooldown/resource audit |
| VS-E | Real model/VFX changes visibly and reversibly | Character/attack before/after and run-reset screenshot |
| VS-F | No uncontrolled proc loops or duplicate HP damage | Ledger/budget regression including cancel/invalid target |
| VS-G | Existing hit reactions, resistance, cancels and enemy telegraphs preserved | Focused Unity test filter + actual encounter observation |
| VS-H | Correctly returns to Nexus and second run is clean | Repeated-run tests, preference and RunSummary integrity |
| VS-I | Developer judges feel and transformation acceptable | Explicit recorded human verdict, not automated inference |

The **GPR-01 slice** requires one two-item synergy, not the entire twelve-boon example catalogue or three-item composition. Three-item distinct synergy and multiple visual layers belong to **GPR-02**. Full modular ecosystem and two thoroughly contrasted builds belong to **GPR-03**. Each step must remain enjoyable on its own; avoid building a generic abstraction without gameplay proof.

---

## 7. Test and regression policy

1. Add focused tests before runtime changes when feasible. Preserve authored profiles/cooldowns/damage, committed attacks, valid cancel windows, specific target eligibility and hit reactions.
2. Test invalid and legitimate combinations; replacement with invested skill, absent slots, disabled items, session restart, interrupted casts, pause/hitstop, death/disable and repeated Nexus→FirstSector→Nexus loops.
3. Include normal, Elite/Boss control-protection regressions and at least one mixed enemy encounter after relevant mechanical changes. Visual overload must not hide warnings.
4. Use finite budgets + explicit ancestry to stress legal chaining and ensure suppressed children do not generate damage/resource/reward side effects.
5. Regression-check Bow/Spear even when changing Gauntlet-specific code shared by common systems.
6. Verify stats through **confirmed HP transactions** and explicit attribution, not sum of overlapping combat callbacks.
7. Report filters, job IDs, individual failing tests, green counts, Console errors, changed assets and source revision. An aggregate across several filters is **not** one globally passing suite.
8. Capture and **actually inspect** targeted Game View PNG/video; distinguish real production runs, controlled fixtures, API-assisted gameplay and human keyboard/mouse playtest.
9. Preservation: `.meta`/GUIDs, serialized assets, third-party files, intentionally deleted files, progression, run summaries and user session state. Avoid destructive builders, unsolicited massive snapshots and low-disk capture storms.

---

## 8. Decision register required at Phase 0 gate

For each issue below, mark the developer decision, recommended option, alternatives, impact and owning activity. Do **not** quietly hardcode a default into the production build.

1. Which Gauntlet moves are permanent/inherent vs exchangeable, including old E/F/F11/Asura/finisher realities and the new Q/E/R/X mapping.
2. How many active slots are initially populated; minimum first-run capability.
3. Whether modular skills are weapon-exclusive by default, and which limited universals are actually designed.
4. What happens to skill-specific upgrades and dependent boons when replacing a skill (suspend, convert, refund, disallow; decide per type).
5. How cooldown, resource, charge, cast and proc ownership behave across skill replacement.
6. Exactly which first basic mutation, companion boon and replaceable skill comprise GPR-01.
7. Acquisition opportunities and rescue mechanisms that prevent unusable/dead reward selections without eliminating RNG variety.
8. What secondary/proc effects may trigger downstream modifiers and the measured safety limits.
9. Visual transformation minimum, attachment/material/VFX priorities and what counts as legible in swarm.
10. What must be fixed before this experiment if a blocking enemy attack or run-reward defect invalidates its tests; keep independent bugs separate from architecture work.
11. Mouse secondary basic: heavy attack, alternative combo branch or contextual follow-up? Choose a testable policy for Gauntlet and separately for Bow/Spear.
12. Directional locomotion implementation, movement/camera reference frame, facing/aim, dash direction and attack-rooting.
13. Default interaction, Asura and contextual finisher bindings, removing E and F11 conflicts without changing the actual mechanics.
14. Settings/rebind architecture: UI, persistence, duplicate/conflicting mappings, reserved bindings, HUD prompts, active device support and regression coverage.

---

## 9. REQUIRED ACTIVITY REPORT TEMPLATE (one `.md` per activity)

Create and fill the following report **at the end of EVERY activity (`GPR-00.Y`, `GPR-C01.Y`, or `GPR-XX.Y`)**, including design-only activities and blockers. Use the repo's real docs convention, link the report from the master ledger, and update the canonical validation matrix compactly.

```markdown
# Tech Guy — Activity Report: <activity ID> — <activity title>

- Date/time (America/Sao_Paulo):
- Phase / activity ID:
- Status: COMPLETED | PARTIAL | BLOCKED | FAILED | AWAITING_HUMAN_APPROVAL
- Evidence classification: IMPLEMENTADO_NAO_VALIDADO | VALIDADO_TECNICAMENTE | VALIDADO_EM_GAMEPLAY | APROVADO_PARA_ENTREGA | NOT_APPLICABLE
- Project path / Unity version / MCP instance:
- Git revision / branch / uncommitted state:
- Developer approval authorizing scope:

## 1. Objective and scope
- Intended outcome:
- In scope / out of scope:
- Acceptance criteria:

## 2. What was actually done
- Inspections/design decisions:
- Implementation, if any:
- Exact files created/modified/deleted (including .meta):
- Existing systems reused and why:
- Any deviation from the approved design:

## 3. Evidence and validation
| Test / scenario / capture | Actual result | Evidence path/job ID | Limitations |
|---|---|---|---|
| ... | PASS/FAIL/BLOCKED/NOT RUN | ... | ... |

- Before/after behavior and metrics (only if comparable):
- Real Unity screenshots/video actually opened and inspected:
- Actual human playtest and direct developer statements, if any:
- Console before/after; regressions; known historical failures:
- Security/integrity: scenes, GUIDs, Git deletions, user prefs/RunSummary, cleanup:

## 4. Outcome and open issues
- Acceptance criteria satisfied:
- Acceptance criteria NOT satisfied:
- Bugs/regressions/risks, with impact and reproduction:
- Explicit decisions still awaiting developer approval:
- What was NOT tested or implemented:

## 5. Stage handoff
- Activity completed: <activity ID> (yes/no)
- Phase completed: <phase ID> (yes/no; explain)
- Next recommended activity ID and reason:
- Is moving forward authorized? yes/no; cite exact gate
- One-paragraph summary in Brazilian Portuguese for the developer:
```

**Strict honesty rule:** A file created or a compile success alone cannot yield `VALIDADO_EM_GAMEPLAY`. A green automated suite alone cannot yield `APROVADO_PARA_ENTREGA`. A failed or blocked task **still receives a report** with verified facts and the exact missing gate.

### End-of-phase summary (in addition to activity reports)

At each phase gate, write a short `<PHASE_ID>_PHASE_SUMMARY.md` according to discovered repository conventions, containing:

- Activities finished / partial / blocked, with links to each report;
- Demonstrated player-facing behavior and inspected captures;
- Actual test coverage, failures and regression risks;
- Approved and pending product decisions;
- Scope delta and rollback path;
- **Exact next-phase permission required** and a clear **STOP / GO recommendation**;
- Developer's acceptance recorded only after explicitly received.

---

## 10. Exact instructions for the FIRST Codex execution

**Execute ONLY Phase GPR-00 (GPR-00.1–GPR-00.5), in order.**

1. Discover real project instructions, skills, paths and files.
2. Verify actual Unity project and record baseline without disrupting current Play Mode or saves.
3. Produce the code-informed inventory and architecture/ownership map.
4. Reconcile finisher/Asura/bindings, ability swap and boon compatibility against the live implementation. **Treat WASD + LMB/RMB combos + remappable Q/E/R/X as the intended new control model and audit the existing click-to-move system to replace it safely.**
5. **Design GPR-C01 as the prerequisite migration phase**, with ownership, movement, mouse-combo, skill binding/settings UI, tests and human approval gate. Then design a small playable GPR-01 mutation slice on top of that input foundation.
6. Write repository-aligned **requirements**, **technical design**, **tasks**, **risk/decision register** and **test plan**; do not create duplicate canons.
7. **After each activity**, create its own report and update the master ledger and validation matrix.
8. Write `GPR-00_PHASE_SUMMARY.md` and explain in Brazilian Portuguese **what was completed, what remains, exactly which activity/phase is next, and the developer decisions required**.
9. **STOP. Do not implement GPR-C01 or GPR-01, do not edit gameplay runtime/assets/scenes, and do not treat this document as authorization for the full rework.**

**The measure of success for Phase 0 is not the amount of code produced. It is a credible, minimal, testable **input migration** and subsequent mechanical/visual transformation design that preserves the combat we already enjoy.**
