# GPR-01.5a — comportamento integrado da build

29/10/2026 · **AUDITORIA MECÂNICA CONCLUÍDA NO ESCOPO TESTADO: 98/98 PlayMode, zero ignorados.** Aceite físico das camadas01.4 e da composição tripla permanece pendente. O pedido atual aceita01.4 tecnicamente, não fisicamente; gameplay01.3 já foi aceito anteriormente.

## Objetivo, preparação e trabalho

Revalidar Base/A/B/C/AB/AC/BC/ABC na implementação existente, sem alterar combate, economia ou aparência. Lidos AGENTS, seis relatórios01.4, visão, requisitos/desenho/tarefas/validação/fatia/ledger/matriz e skills locais com referências. Skills aplicadas: tech-guy-game-vision, tech-guy-combat-unity, tech-guy-boon-design, tech-guy-unity-acceptance; instaladas router, unity-csharp-scripting, unity-mcp-orchestrator e performance-optimization. Nenhuma delegação.

Unity MCP confirmou D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, Tech-Guy@fccafb2b47cd7671, branch gameplayOverhaul, HEAD653db8ba0874f53141c70e88227af76ba0dd07ae com alterações anteriores preservadas. NexusBlockout limpa/EditMode, compilação pronta e Console inicial sem entradas. Baseline2819 arquivos, diff anterior,36 chaves de preferências e5 arquivos do diretório persistente inventariados em gpr-01-5. Não houve reset, commit ou push.

## Matriz observada

| Build | Mecânica validada | Identidade |
|---|---|---|
| Base | Básico nativo, sem tornado | Sem novo VFX |
| A | Tornado viajante, dano0,60× capturado | Vento |
| B | Conductor nativo; nenhum tornado | Feedback nativo |
| C | Detonation nativa em morte; nenhum tornado | Feedback nativo |
| A+B | Impacto de vento pode conduzir uma vez | Vento + azul elétrico |
| A+C | Morte confirmada por vento pode explodir uma vez | Vento + fogo |
| B+C | Caminho nativo preservado, sem whitelist global nova | Feedback nativo |
| A+B+C | Vento→Conductor letal→Detonation | Três camadas |

Dois produtores no mesmo terminal continuam legítimos: soco20 + cascata nativa9/4,05 + vento12 + condução derivada5,4 =50,45HP, cinco transações e uma raiz terminal. Alvos já reclamados pela cascata nativa não voltam a ser elegíveis só para garantir uma segunda cadeia. Quota de um Conductor e uma Detonation pertence ao tornado, não suprime o soco.

Triple causal: vento12 não letal, Conductor mata vizinho5 e Detonation causa3,75 no terceiro. Também conferidos overkill/HP efetivo, mortes creditadas, parent único, ausência de recurso extra, orçamento32, último slot, colliders/gerações, reservas revalidadas, reentrância, whiff/imunidade/LOS, pause/hitstop, morte/disable/swap/run/cena, clear e próximo encontro. Cascatas nativas permanecem limitadas pelos owners/dedupe/budgets existentes; não foram globalmente reescritas.

## Execução e evidência

Unity Test Framework via `run_tests` com [a-request.json](../validation/artifacts/gameplay-rework/gpr-01-5/a-request.json), `mode=PlayMode`, `include_details=true`, `init_timeout=120000`. Job **52409195165a4379ad5bce80fd673203**, **98/98**,167,0076961s. Resultado completo [a-final.json](../validation/artifacts/gameplay-rework/gpr-01-5/a-final.json). Nenhuma falha nesta seleção; não é suíte global.

Recibos e capturas novos em `gpr-01-5/a`, inclusive natural tickets AB/AC na rota antiga de fixture, HP/lineage e crowd8/32/64. As comparações mecânicas usam grants assistidos identificados; não demonstram disponibilidade natural. A auditoria da produção real está separada em01.5b, e a câmera normal/telegraphs em01.5c.

Arquivos alterados somente para redirecionar evidência: TitanicCompositionPlayModeTests.cs e TitanicElementalVisualPlayModeTests.cs agora respeitam TechGuy.Tests.GprPhase, conservando defaults anteriores. Nenhum owner/asset de produção mudou. Relatórios históricos e saídas anteriores preservados. Os novos caminhos não implicam revalidação humana.

## Limites e próximo gate

98/98 não converte os199/203 históricos de01.4 em203/203. A estabilidade ampliada é tratada em01.5d; a acessibilidade e leitura em01.5b/c. Desempenho é medição de Editor, sem isolamentoCPU/GPU/GC de release; saves completos não são objeto de certificação. Próximas subatividades01.5b–d já autorizadas; nenhuma01.6 ou expansão autorizada.
