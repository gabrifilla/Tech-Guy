# Gameplay Rework — tarefas propostas e gates

27/10/2026. **GPR-C01.3a concluída tecnicamente, aguardando revisão.** C01.2c aceita no escopo técnico. LMB direcional por clique implementado; hold do básico somente com ChargedShot por decisão explícita; Asura no RMB da Manopla incluída por autorização adicional.102/102 testes normais; aceite físico pendente. C01.3b/GPR-01 não iniciadas. [Relatório C01.3a](../decisions/GPR-C01.3a_RELATORIO.md).

| Tarefa | Dependência | Trabalho mínimo e owner | Aceite/rollback |
|---|---|---|---|
| C01.1a | Autorização explícita recebida | Baseline técnica, virtual/API e capturas atuais; distinguir humano ausente; inspeção três armas | COMPLETED técnico;21Play+15Edit; baseline física pendente, não inventada |
| C01.1b | 1a | Revisar ADR/requisitos/design/tasks para RMB Asura, cliqueLMB, quatro slots e compatibilidade; fechar plano/gates | COMPLETED de elaboração; STOP para decisões pendentes |
| C01.2a | Autorização explícita recebida; Asura/X/Bow-Spear não são bloqueios | CONCLUÍDA E ACEITA para implementar: motor/arena/10 testes isolados, câmera/mundo/dash comparados, Move cru falha e CC avaliado separadamente |59/59 históricos;4 PNG e CSV; autorização posterior levou a2b, sem atestar físico |
| C01.2b | Autorização direta após revisão2a; teclas incluídas por resposta explícita | IMPLEMENTADO: controlador/motor único, fontes reais, animação por deslocamento,13 testes de integração e cutover em4 cenas; facade v2 para liberarW com Q/E/R e especialF11 |66/66 finais incluindo regressões;3PNG Nexus/CSV; físico, crowd e bordas pendentes. Asura/X/Bow-Spear separados; próxima proposta2c |
| C01.2c | Autorização específica recebida após2b | CONCLUÍDA: sliding protegido de locomoção; barreira compartilhada de soltura; UI/reward/results/reload/foco/disable; R contextual; investigação de recovery |83/83 normais; hitch500 negativo reproduzido/causa histórica aberta; relatório e revisão pendente. STOP antes de3 |
| C01.3a | C01.2c aceita; LMB autorizado; RMB acrescentado diretamente; ChargedShot decidido | CONCLUÍDA: básico primário por clique/cursor/whiff; buffer único120ms; hold apenas ChargedShot; RMB Manopla no owner Asura atual, sem básico/custo duplicado; F11 compat/F prioritário |102/102 normais;4 Game View PNG/1372 amostras. X/extração completa não implementados. STOP e revisão antes3b |
| C01.3b | 3a | Testar clique/busy/hold sem repetição default, simultâneoespecial/básico, consumo/cooldown/Cadenciada/F/cancel/cleanup e sourceR | R04/05/09; hold opt-in/ChargedShot exigem decisão; sem alias de básicoBow/Spear |
| C01.4a | 3b/D13/14 | PilotoEditMode sem PlayerPrefs: importar preferências reais/draft e round-trip overrides; depois migração versionada aprovada | V04/05/06; F11 não copiado cegamente para X ouRMB; sem novo backend antes do piloto |
| C01.4b | 4a | Ampliar painel existente: bindings primário/alternativo e partes Move; Apply/Cancel/Reset/foco/validação | V06/07,1080p/720p; sem UI de skills modular ainda |
| C01.4c | 4b | Atualizar todos os prompts/HUD/tutorial/Nexus/reward/results; manter slot histórico em analytics | V05/14; auditoria de strings QWER/E/F11 |
| C01.5a | 4c | Filtros GI/básico/cancel/reação/energia/summary e smoke três armas | V08/15; falhas novas bloqueiam gate |
| C01.5b | 5a | Sessão física e antes/depois de movimento/mira/combo/dash/menus/remap | V16; STOP, aprovação humana antes GPR-01 |
| 01.1 | C01 humano aprovado + conteúdo D06/09 | Rebaseline pós-controle; validar sequência real de tickets/rota/capabilities | V13; não misturar mudança de input e mutação no comparativo |
| 01.2a | 01.1 | A no scope Basic/snapshot e filho no scheduler/contexto existente | V09; não tocar clock/cancel/tuning base |
| 01.2b | 2a | Testes whiff/LOS/immune/reentrante/swap/cleanup; visual forma+onda | V09/12; sem dano em callback cosmético |
| 01.3 | 2b | B=Conductor recebe onda autorizada, mesma raiz/budget, ordem de aquisição equivalente | V10/14; sem ampliação geral de procs |
| 01.4a | 3/D04/05/07 | Operação seletiva/rollback holder + adaptador Uppercut no RunBoons | V11, falha antes/depois commit e timers de outros slots |
| 01.4b | 4a | Reward compara antes/depois, investimento e recarga; respeita ticket/focus/release | V05/11/13; sem concessão extra |
| 01.5 | 4b | Reconciliação visual base/A/A+B/reset, pooling/disable/cena/morte | V12; sem material/attachment órfão |
| 01.6a | 5 | Percurso real e dois ciclos; HP/RunSummary, budgets/performance observados | V13/14/15; dado sintético claramente identificado |
| 01.6b | 6a | Comparação visual/feel e humano; consolidado/report/ledger/matriz | V16; STOP antes GPR-02 |

GPR-02: C/eco, sinergia três vias e camadas cumulativas; GPR-03: duas builds e ecossistema modular ampliado; GPR-04: meta; GPR-05: semânticas específicas Bow/Spear. Sem catálogo dos12 exemplos, Mythics, mapa novo ou rework inimigo nas tarefas acima.

Riscos transversais: K01 checkout errado (reconfirmar MCP); K02 resets de cooldown (operação seletiva); K03 input duplicado (um leitor); K04 rebind quebra slots/histórico (IDs estáveis); K05 proc por whiff/recursão (receipt+budget); K06 troca incompatível (preflight/rollback); K07 efeito cobre perigo (hierarquia/prova humana); K08 fixture altera resumo (isolamento e hashes); K09 dívida histórica confundida com regressão (filtro isolado+conjunto); K10 direção aprovada confundida com tuning (D01–14 com aceite explícito).

Exceção autorizada em C01.2b: parcela mínima de C01.4a/c foi antecipada — migração pura, Apply versionado no Settings existente, round-trip e prompts essenciais — para liberar WASD. Não declarar C01.4 concluída: remapeamento dos componentes de movimento, secundários, ação própria Interact, todos os contextos/UI e slots QERX ainda não foram entregues. Primeira regressão legada29/30 foi arquivada; caso isolado e grupo final passaram sem alterar tolerância, com causa de timing/ordem pendente.
