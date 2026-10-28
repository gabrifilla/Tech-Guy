# Tech Guy — GPR-00: consolidado e gate de aprovação

26/10/2026, America/Sao_Paulo. **Atualização posterior: GPR-00 APROVADA DOCUMENTALMENTE COM AJUSTES.** O desenvolvedor autorizou somenteC01.1a/b; sua direçãoLMB clique/RMB especialAsura supera propostas antigas deste consolidado. Ver [ADR vigente](ADR-0006-GPR-DIRECAO-E-GATES.md) e [prontidãoC01.1](GPR-C01.1_BASELINE_E_PRONTIDAO.md). O conteúdo abaixo preserva o fechamento histórico e seus números; não é autorização paraC01.2–5/GPR-01.

**Fechamento original:** AWAITING_HUMAN_APPROVAL, cinco atividades de audit/design, nenhuma implementação. As pendências visuais/físicas abaixo eram o estado naquele momento;C01.1a acrescentou baseline virtual/visual, sem parecer humano novo.

## Entregas

| Atividade | Resultado | Relatório |
|---|---|---|
| GPR-00.1 | Projeto/instruções/skills/versão/baseline e inventário; visão inicialmente ausente, depois fornecida e lida integralmente | [00.1](GPR-00.1_RELATORIO.md) |
| GPR-00.2 | Caminhos comando→contato→HP→proc→visual→resumo e tickets→inventário→cleanup; mapa de reutilização/lacunas | [00.2](GPR-00.2_RELATORIO.md) |
| GPR-00.3 | Quatorze decisões com alternativas; conteúdo mínimo e ações protegidas propostos; divergências reconciliadas | [00.3](GPR-00.3_RELATORIO.md) |
| GPR-00.4 | Requisitos GPR-R01–17, desenho, tarefas granulares, riscos K01–10 e validação V00–16 | [00.4](GPR-00.4_RELATORIO.md) |
| GPR-00.5 | Matriz ações/bindings/slots/conflitos; arquitetura input/movimento/rebind e roteiro de migração C01 | [00.5](GPR-00.5_RELATORIO.md) |

Navegação: [pacote e fontes](../gameplay/README.md), [ledger](GAMEPLAY_REWORK_PROGRESS.md), [ADR para decisão](ADR-0006-GPR-DIRECAO-E-GATES.md). Não houve ausência de skills locais aplicáveis: quatro lidas e aplicadas, mais dez especialistas/router instalados conforme relatório00.1. Skills genéricas Unity6000.3 foram adaptadas ao projeto6000.5.10f1/pacotes reais; não houve migração de versão.

## Achados que mudam o plano de implementação

- O checkout aberto no Editor é `D:/Dev/Personal/My_games/Tech-Guy`, branch gameplayOverhaul, HEAD3a3f0ee + alterações locais. O worktree C: do chat é anterior (Unity6000.3.3f1), sem documentação/skills atuais. Todo resultado desta fase está no projeto D:, sem misturar checkouts.
- Quatro habilidades continuam index0–3. Na sessão atual: Q=Avanço, W=Punhos, E=Choque e F11=Asura; F é ação separada de finalização/retorno. F11 é remapeamento salvo de Skill4. Proposta Q/E/R/X muda o display/input dos índices, mantendo IDs históricos QWER de boons/dano. Não criar WeaponSpecial=C adicional só porque aparece como recomendação no plano.
- Skill3 também interage no Nexus; arsenal compacto usa Skill4. A migração deve separar Interact, não só mudar a tecla e manter consumo duplo.
- `AbilityHolder.RefreshLoadout` recria timers/estados de todos os slots; não serve diretamente a troca modular preservando recargas. `RunRewardPolicy` evita duplicata de concessão, mas não garante rollback de todo callback parcial. Swap exige operação seletiva no owner atual.
- GI1.0–1.8 têm dependências concretas de scheduler/ImpactEvents, dedupe, snapshot, HP confirmado, pose e reação. Básico usa Push/overrides reais; Choque/Stagger, Asura e FinishSequence não são a mesma ação. Sem reescrita do pipeline para facilitar modularidade.
- ComboNova dispara por emissão a cada terceiro básico, inclusive whiff comprometido; não é a onda por terminal confirmado proposta na visão. Conductor/ProtocolBudget e causalidade existentes devem ser reutilizados com autorização de filhos explícita.

## Proposta de GPR-C01, antes de mutações

Um backend de ações Input System1.20 com fachada GamePreferences e painel de Settings ampliados, sem dois listeners. WASD normalizado relativo à câmera, usando agente/colisão existentes como único motor, sem destinos de clique/autoaproximação. Mira independente; direção do swing fixada no commit; root/cancel/tempo atuais preservados.

Gauntlet: LMB mantém tap/hold/combos atuais; RMB propõe ramo terminal de varredura mais curto/largo, um único commit, mesmos dano/timing/reação; fatores1,25×largura/0,8×alcance são hipóteses de tuning para aprovação. Bow/Spear conservam primário e fallback secundário básico por press até recorte próprio; também precisa aprovação. Dash proposto usa WASD senão mira/facing. Nenhum desses detalhes foi implementado.

Defaults propostos: Skill1Q, Skill2E, Skill3R, Skill4X, DashSpace, InteractG, FinishSequenceF, PauseEsc. Alternativa do plano F-interação/V-finalização está registrada; proposta G/F preserva a ação F atual. C/Asura separado não recomendado por duplicar index3; dados customizados como F11 não serão apagados silenciosamente. Migração versionada/draft resolve conflitos, preserva IDs/chaves e atualiza todos os prompts. Skills protegidas/slot substituível e modo compacto têm decisão pendente explícita.

## Fatia GPR-01 posterior proposta

A=**Punhos Titânicos** novo: terminal com HP confirmado gera uma onda de geometria ampliada sem duplicar a vítima do soco, com visual de manopla maior. B=**Conductor existente** autorizado a compor com essa onda, mesma raiz/budget e sem cadeias recursivas/recursos extras. Swap mínimo=**Avanço→Uppercut Eclipse existente no index0**, por adaptador de variante/clones, mantendo Choque/Asura/FinishSequence e demais timers. Não é sistema modular universal já pronto.

Na primeira fatia, investimento específico incompatível impede troca; sem reembolso/economia nova. Recarga conservadora max(restante antigo, recarga nova), sem alterações dos outros slots. Ofertas usam tickets/ordinais/Mutation gates existentes; validar uma sequência real legal antes de autoria extra. Sem novo mapa, nova sala obrigatória ou concessão fora de ticket. C/eco/três vias fica GPR-02; duas builds completas GPR-03; meta GPR-04; expansão de armas GPR-05.

## Evidência efetivamente obtida

| Verificação | Resultado e limite |
|---|---|
| Editor/MCP | Instância Tech-Guy@fccafb2b47cd7671, Unity6000.5.10f1, NexusBlockout limpa, início/fim fora de PlayMode; sem run humana ativa no Editor |
| EditMode | AsuraFinishControlTests15/15 PASS, jobd7511749935c444badec8449dc1c0170 |
| PlayMode | Quatro GIBasic* por cancel pré-contato, reentrância comprometida, whiff móvel e troca de alvo:4/4 PASS, job304278d7295e4fbc8660565529a44206 |
| Console | Consulta final de erros vazia; warnings das fixtures preservados (keys, camera/dash/efeito ausentes, Animator), além do warning inicial Input Manager |
| Integridade |1597 arquivos próprios/config/skills/metas com SHA256 iguais; status Git fora de Docs sem delta;510 exclusões Git preexistentes preservadas conforme snapshot atual |
| Preferências | Moedas2597, loadout0, unlocks consultados e tutorial iguais; RunSummary42387 caracteres/SHA256 igual antes/depois dos testes. Sem restore manual de snapshot humano antigo |
| Capturas/input físico | **Não executados nesta fase**; nenhuma imagem ou vídeo novo alegado como inspecionado;19 testes focados não provam feel/arte |
| Histórico de falhas | Mutation/BurnTick/hold não reexecutados; reprodutibilidade atual não aferida. Resultados GI antigos não foram promovidos a aprovação atual |

Artefatos em `Docs/validation/artifacts/gameplay-rework/gpr00`: baseline/status, hashes, leituras Editor/bindings/campos, resultados detalhados dos dois jobs, preferências e QA documental. Build, importação deliberada, suite completa, standalone/relaunch, gamepad e benchmark de swarm não foram executados. Test runner entrou/saiu de Play e fixtures existentes preservaram/restauraram temporariamente a chave de resumo; isso não equivale a ausência de toda escrita transitória de teste, mas o estado final consultado permaneceu igual.

## Arquivos e QA documental

Criados os seis documentos `Docs/gameplay/GPR_OWNERSHIP.md`, `GPR_REQUIREMENTS.md`, `GPR_TECHNICAL_DESIGN.md`, `GPR_TASKS.md`, `GPR_VALIDATION_PLAN.md`, `GPR_CONTROL_MIGRATION.md`, o índice `Docs/gameplay/README.md`, os cinco relatórios `Docs/decisions/GPR-00.1_RELATORIO.md` a `GPR-00.5_RELATORIO.md`, `ADR-0006-GPR-DIRECAO-E-GATES.md`, `GAMEPLAY_REWORK_PROGRESS.md` e este consolidado. Copiado integralmente o plano para `Docs/gameplay/TECH_GUY_GAMEPLAY_REWORK_EXECUTION_PLAN_v1.1_WASD.md`, SHA256688C2935F88BC5C64DE8ABD2BD8C2D05C779D5B99F0D188A4F34E1750848727F, igual ao anexo de Downloads. A visão original foi adicionada pelo desenvolvedor e preservada.

Modificados somente os canônicos `Docs/README.md`, `Docs/03_PRIORIDADES.md` e `Docs/validation/01_MATRIZ_DE_VALIDACAO.md`, por acréscimos delimitados que apontam para este pacote. Evidências novas em `Docs/validation/artifacts/gameplay-rework/gpr00/`: `baseline.json`, `git-status-inicial.txt`, `integridade-inicial.json`, `integridade-final.json`, `editor-inicial.json`, `editmode-asura.json`, `playmode-basicos.json`, `campos-movimento.json`, `preferencias-iniciais.json`, `preferencias-finais.json`, `console-final.json` e `qa-documental.json`. Nenhum .meta criado/modificado.

QA final:15 documentos do pacote e55 links locais verificados, nenhum link ausente; cinco relatórios presentes; comparação programática das preferências consultadas sem diferença. Hashes não cobrem binários de terceiros nem preferências não consultadas; a comparação do status fora de Docs complementa essa cobertura. Não é validação de API futura ou de gameplay inexistente.

## Pendências e autorização necessária

Revisar [D01–D14](ADR-0006-GPR-DIRECAO-E-GATES.md), principalmente:

1. Núcleo protegido, quatro skills iniciais e permissão de swap somente index0/Uppercut.
2. A/B exatos, geometria e política de investimentos/cooldown/derivados.
3. Arquitetura InputAction/fachada, referência de câmera, motor único e direção de dash.
4. RMB terminal, seus fatores geométricos propostos e fallback Bow/Spear.
5. QERX/G/F/Space/Esc, tratamento de F11/custom e inspeção compacta.
6. Delta mínimo por owner, rebind Settings e prova visual suficiente para a fatia.

Baseline visual/física pré-mudança, solicitada pelo roteiro, foi **planejada e permanece pendente de execução** em C01.1; isso limita o fechamento da evidência e não deve ser apagado por aprovação do documento. Deve ocorrer antes de qualquer código e pode motivar revisão do desenho. Nenhuma aceitação humana foi registrada nesta fase. A elaboração documental está concluída; o aceite e validações futuras permanecem em aberto.

**Próxima atividade recomendada: GPR-C01.1a, apenas após autorização explícita para GPR-C01 e confirmação das decisões acima.** Uma aprovação deste design não autoriza GPR-01: primeiro migrar/testar/aceitar controles. Recomenda-se STOP agora, como solicitado. Nenhum código de implementação, prefab/cena, save/schema, balanceamento ou conteúdo de mapa foi modificado. Rollback desta fase é somente remover/reverter os documentos produzidos especificamente, preservando a visão recebida e alterações preexistentes; runtime não precisa rollback.
