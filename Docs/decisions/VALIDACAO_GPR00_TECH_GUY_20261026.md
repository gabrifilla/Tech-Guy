# Tech Guy — Revisão independente da GPR-00

**Base examinada:** `Docs.zip` enviado em 26/10/2026. **Natureza:** revisão documental de artefatos e JSONs anexados; sem acesso direto ao Unity Editor, código-fonte integral ou vídeos do checkout local. **Decisão:** aceitar GPR-00 como **entrega documental tecnicamente coerente**, mas **não aprovar GPR-C01 inteiro** nem os parâmetros de GPR-01 neste momento. Autorizar somente C01.1a/b (baseline e provas sem alteração de gameplay), seguido de revisão pontual das decisões abaixo.

## Inventário e cobertura

O ZIP contém 73 entradas, incluindo diretórios, canônicos, 5 relatórios `GPR-00.1` a `00.5`, resumo de fase, ADR-0006, ledger, 6 especificações (`OWNERSHIP`, `REQUIREMENTS`, `TECHNICAL_DESIGN`, `TASKS`, `VALIDATION_PLAN`, `CONTROL_MIGRATION`), documentos de visão/plano e arquivos brutos de evidência. Os arquivos de teste anexados confirmam 15/15 EditMode e 4/4 PlayMode, mas não são suíte global ou teste dos controles novos. `integridade-final.json`: 1597 arquivos conferidos, nenhum alterado; status Git fora de Docs sem delta; 510 exclusões preexistentes antes/depois. `qa-documental.json`: 15 documentos, 55 links, sem destinos ausentes e preferências consultadas iguais. Não há baseline físico ou capturas novas da migração.

## Pontos aprováveis

- Auditoria separou corretamente checkout D: aberto no Unity do worktree anterior de C:.
- Leitura de instruções locais, skills instaladas e restrições de compatibilidade de versão foi reportada de forma verificável documentalmente.
- Mapeamento existente de hit/HP/ProtocolBudget/RunBoons/RunSummary evita pipelines paralelos.
- Mantém IDs dos slots e de telemetria, embora as teclas mudem para Q/E/R/X.
- Preserva contrato de ataques básicos, cancelamento, GetHit, GI1.6–1.8, run e economia.
- Identificou que `RefreshLoadout` geral recria cooldowns e que troca exige operação seletiva e rollback.
- V00–V16 e tarefas C01.1a–C01.5b distinguem provas técnicas, captura e aceitação humana.
- Não houve implementação prematura, e cada atividade possui relatório Markdown e ledger.

## Bloqueios de design e ajustes necessários

1. **D11: RMB não aprovado como apenas substituto de terminal.** A proposta original requer clique esquerdo/direito para combos e ataques básicos. Implementar um protótipo comparável ou decidir explicitamente se RMB será golpe alternativo independente, follow-up, heavy ou apenas terminal. Não adotar proporções 1,25x/0,8x sem teste autoral. O fallback Bow/Spear como alias do mesmo ataque pode ser aceitável *somente* como transição técnica documentada, não como controle definitivo.
2. **D12: NavMeshAgent.Move é hipótese de engenharia, não prova.** Exigir protótipo de cantos, paredes, borda de NavMesh, obstáculos, agent avoidance, root motion, dash, pull e ataque em movimento sem dupla autoridade. Se falhar, reabrir escolha do motor antes de forçar arquitetura. WASD relativo à câmera é bom padrão, mas dash orientado por movimento/mira requer teste humano.
3. **D13: teclas auxiliares e conflito de F/G/V.** Q/E/R/X, WASD, LMB/RMB e remapeamento são direção aprovada; `Interact=G`, `Finish=F`, Asura index3=X são propostas coerentes, mas devem receber aceite explícito. Preservar F11 salvo/custom até migração voluntária. A inspeção compacta do arsenal deve possuir ação/UI independente e prova real de uso.
4. **D14: rebind robusto é grande superfície.** Favor Input Actions + facade única, com persistência versionada e draft/Apply/Cancel/Reset, mas exigir piloto EditMode para migração de overrides reais antes de substituir o backend; recuperação de inputs essenciais e ausência de listeners duplicados.
5. **D01/D02: proteção das skills na fase inicial não pode virar regra permanente.** Manter o kit atual na migração evita perdas, porém a visão de longo prazo prevê loadout progressivamente modular. GPR-01 com um swap é prova de conceito, não conclusão de skills modulares.
6. **D04/D05: impedir substituição de skill investida e cooldown=max(...)** são escolhas de design razoáveis para o protótipo, não políticas finais. Mostrar efeitos nas ofertas e medir se bloqueios tornam recompensas inúteis.
7. **D06/D07: fatia A + Conductor + Uppercut** é verificável, mas precisa prova de oferta legal pela rota/ordinal reais. Caso contrário não forçar Mutation por via artificial, nem substituir E/Stagger ou Asura.
8. **D08: composição filha.** Persistir ancestry/dedupe/budgets, proibir dupla contagem/duplicação de energia. Validar child suppression mesmo quando não há HP causado.
9. **D09: visual de manopla e onda** pode ser mínimo, mas precisa parecer transformação e ser legível. Uma nova regra visual não deve usar tamanho da malha como hitbox sem geometria compatível.
10. **GI1.9/AoE e 510 vs. 546 exclusões históricas:** investigar separadamente só se necessário. A diferença de contagem de exclusões não indica modificação nesta tarefa, mas deve ser documentada como baseline atual, não apagada ou interpretada como restauração sem diff comparável.

## Gate recomendado

- **GPR-00:** `CONCLUIDA_DOCUMENTALMENTE / APROVACAO_CONDICIONAL`.
- **GPR-C01.1a/b:** autorizável como captura/baseline, sem alteração de runtime ou bindings.
- **GPR-C01.2–C01.5:** aguardar D11–D14 decididos, provas da baseline e eventual ajuste de arquitetura.
- **GPR-01 e posteriores:** não autorizados.

## Handoff sugerido

Peça ao Codex para registrar esta revisão como feedback humano de aprovação condicional, atualizar ADR-0006 e tasks sem adulterar históricos, executar somente C01.1a/b, produzir relatórios individuais (`GPR-C01.1a_RELATORIO.md` e `GPR-C01.1b_RELATORIO.md`), atualizar ledger/matriz e parar com propostas comparáveis de RMB e dash antes de implementar movimento.
