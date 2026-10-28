# GPR-00.2 — Ownership mecânico e lacunas

- Data: 26/10/2026 (America/Sao_Paulo); fase GPR-00, atividade GPR-00.2.
- Status: COMPLETED (auditoria estática); evidência: NOT_APPLICABLE para design. Não é aceite de runtime.
- Ambiente/base/autorização: mesmos da [GPR-00.1](GPR-00.1_RELATORIO.md), somente GPR-00.

## Objetivo e aceite

Rastrear comando→contato→HP→proc→apresentação→resumo e recompensa→aplicar→remover→nova run, com owner, lacuna e risco. Entradas: código atual e canônicos; fora: mudanças de gameplay.

## Realizado e arquivos

Criado [GPR_OWNERSHIP.md](../gameplay/GPR_OWNERSHIP.md), com dois fluxos completos e classificação EXISTS/REUSE/EXTEND/NEW/UNDECIDED/NOT VERIFIED. Criado este relatório; ledger e matriz atualizados. Nenhum script/asset/meta removido ou modificado.

Achado principal: RefreshLoadout reinicializa timers de todos os slots; reaproveitá-lo ingenuamente para swap daria recargas gratuitas. Tickets são idempotentes, mas callback Faulted não equivale a rollback de efeitos. Proposta futura deve estender RunBoons/AbilityHolder, preservando HP/scheduler/budget/telemetria existentes.

Skills aplicadas: unity-csharp-scripting, unity-scriptableobjects, unity-animation, unity-navmesh, game-feel, unity-mcp-orchestrator e as quatro skills locais Tech Guy; input-systems/unity-input-system para fronteira comando/executor. Uso é análise/design, não implementação dos exemplos.

## Evidência e validação

| Verificação | Resultado | Limite |
|---|---|---|
| Fluxos/métodos e snapshots/clones | PASS estático | Referências reais no mapa; sem simulação de swap |
| IDs/tickets/budget/dano | PASS estático | Nenhuma prova de elegibilidade natural ou performance nova |
| Regressões básicas PlayMode | 4/4 PASS, job304278d7295e4fbc8660565529a44206 | Resultado recuperado ao fechar atividade; fixtures por API/reflexão, sem input físico |

Visão original fornecida durante esta atividade: arquivo real `Docs/gameplay/TECH_GUY_GAMEPLAY_MUTACOES_SINERGIAS_E_SKILLS_MODULARES(1).md`; leitura completa realizada em blocos. O nome sem `(1)` informado não existe. Fonte principal da nova visão conforme esclarecimento do desenvolvedor; QWER e “E finisher” precisam reconciliação, sem tratar texto do documento como autorização de implementação.

## Pendências e passagem

Aceite do mapa satisfeito por inspeção, sem inferir validação de todos os caminhos em Unity. Sem capturas/playtest humano novos; nenhum tuning ou orçamento aprovado. Próximo: GPR-00.3, especificação e decisões explícitas da fatia. Continuidade de auditoria autorizada dentro de GPR-00; implementação continua bloqueada pelo gate humano. Fase ainda aberta.
