# Roadmap de qualidade e prioridade

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



> Sequência recomendada de trabalho, **não** autorização para implementação em lote; checar tarefas, branches e priorizações atuais com o usuário antes de começar mudanças.

## Atualização Gameplay Intelligence 1.9 — feedback humano, 25/10/2026

Após a preparação, o desenvolvedor respondeu ao [roteiro GI1.9](validation/04_CHECKLIST_PLAYTEST.md#respostas-humanas-recebidas--gi19). Spawner e boa parte dos ranged foram interessantes; básicos pareceram lentos/pouco ameaçadores, alguns efeitos ranged (um vindo de cima) não tiveram consequência aparente, e salas/conexões pareceram precárias e extensas sem propósito. Esclareceu ter ficado dentro da área aérea sem perder HP; não identifica o atacante porque considera a aparência de todos igual. Consegue evitar intencionalmente a maioria dos golpes. Distribuição aleatória, isoladamente, não foi considerada ruim. Boons pouco transformadores e dash como transporte dominante foram as frustrações transversais relatadas. Sem seed/nós/boons/captura dessa sessão, não atribuir causas técnicas ou aprovar o conjunto.

Próximo recorte recomendado: primeiro identificar/reproduzir o efeito ranged sem consequência (possível bug); depois avaliar aproximação/percepção/preparação dos básicos e o incentivo de mobilidade com exemplos medidos. O impacto subjetivo de boons deve orientar um recorte próprio com a build realmente adquirida; ligações entre salas exigem proposta espacial separada. Reviver mortos pelo spawner é proposta de design a decidir. Preservar GI1.6–1.8; nenhuma alteração de dano, velocidade, cooldown, postura, mapa, boon ou arma foi autorizada automaticamente por esse feedback.

## Atualização Gameplay Intelligence 1.8 — 25/10/2026

Sniper compromete origem/direção quando seu aviso se torna visível; projétil segue a mesma linha sem nova aquisição. Sem fase extra, atraso, alteração de dano/velocidade/cadência ou mudança dos outros ranged. Regressão90°→0° e comparação FirstSector de dois tiros20,983°/5,406°→0°/0°, com esquiva válida e emissões/timings preservados.11/11 focados,98/98 regressões PlayMode e25/25 EditMode aprovados nos filtros da [matriz](validation/01_MATRIZ_DE_VALIDACAO.md). GI1.6/GI1.7, dados e cenas preservados; sem aceite humano novo.

Recomendação GI1.9: sessão humana curta de pressão e legibilidade com os Grunts/Snipers atuais. Usar incidentes concretos para escolher o próximo ajuste ou arquétipo; não retunar automaticamente o Sniper nem iniciar rework de encontros, mapas, boons ou armas.

## Atualização Gameplay Intelligence 1.7 — 25/10/2026

Contrato de aproximação do Grunt comum corrigido e validado tecnicamente: sugestão de posição/separação/tolerância agora cabe no alcance melee existente, sem aumentar alcance, dano, velocidade ou mudar timings. Antes:12 s sem ataque; depois, no mesmo cenário, cinco contatos de12 HP, recuperação liberada e novo ataque. Há comparação parada e em movimento,11/11 casos focados,87/87 regressões PlayMode e25/25 EditMode; cobertura/assistência/limites na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md). Outros arquétipos/ranks mantêm a política anterior; não há novo aceite humano.

Próxima proposta: GI1.8 discutir o Sniper com tracking inicial, aim-lock explícito e coerência aviso→projétil após compromisso. Isso ainda exige decisão de design; a GI1.7 preservou targeting e reprodução do problema. Mapas, boons, Nexus e Bow2.0/Spear2.0 seguem fora deste recorte.

## Atualização Gameplay Intelligence 1.6 — 25/10/2026

Uma correção visual pequena validada: EnemyCombatActions conserva sua pose manual até o render. Sem alterações de IA, dano, resistência ou armas. O próximo recorte recomendado, GI1.7, é reconciliar aproximação/preferência do Grunt com seu alcance efetivo: no cenário real parado, ele navegou12 s sem atacar. O conflito de banda está documentado, mas mudança de prioridade/alcance exige decisão autoral. Depois, decidir se o Sniper deve atualizar o aviso ou manter o disparo na direção anunciada; houve90° de divergência em diagnóstico controlado. Evidências e alternativas em [inimigos](systems/03_INIMIGOS.md) e [matriz](validation/01_MATRIZ_DE_VALIDACAO.md). Bow2.0/Spear2.0 devem apoiar-se nesses contratos, sem mascarar problemas de ameaça com buffs de arma. Mapa, boons e Nexus não foram reformulados.

## Atualização Gameplay Intelligence 1.4 — 25/10/2026

**GI1.5 — sessão física realizada:** o desenvolvedor relatou sensação dos golpes legal; a prioridade percebida passou a ser inimigos/interação dos golpes com eles, mapa e boons. Dois trechos físicos coletados, sem aceite individual de todas as tarefas; ver [roteiro e registro](validation/04_CHECKLIST_PLAYTEST.md#gi15--resposta-da-manopla-sessão-física-de-812-minutos). Não alterar velocidade, animação, cancelamento ou buffer geral com esse feedback. Recomenda-se GI1.6 focada em resposta dos inimigos aos acertos, ameaça/retomada e leitura dos encontros: reproduzir um problema concreto antes de corrigir. Mapa e boons ficam registrados para recortes próprios, sem autorização de reformulação em lote.

Correção implementada do básico da Manopla: aceitação imediata, HP no contato autorado, apresentação ajustada à cadência existente conforme decisão explícita do desenvolvedor. Regressão vermelha→verde;16 casos focados e60 regressões PlayMode distintos aprovados, com uma fixture de clique atualizada para o novo contrato e repetida;22 EditMode aprovados. Comparação real antes/depois registrada, dados restaurados e Editor em NexusBlockout limpa. Cobertura, jobs e limites na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md). Próximo recorte recomendado, GI1.5: playtest humano curto de aproximação, socos consecutivos, esquiva e retomada contra ameaças móveis, antes de qualquer ajuste de números ou arte. Arco/Lança têm regressões focadas; falhas históricas de Mutations/BurnTick conservam validação separada.

## Atualização VS-02A — 21/10/2026

Coleta, atribuição e preservação aprovadas para esta tarefa e implementadas, **sem interface final**. Três resumos de runs assistidas reconciliados ao HP real, 20 novos testes aprovados e 47 EditMode; suíte conjunta PlayMode 111/112, burn saturado passa isolado e permanece investigação separada. Contrato de dano/ambiguidade em [telemetria](validation/03_TELEMETRIA_E_GAME_FEEL.md); evidências na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md#vs-02a--run-analytics-e-atribuição-causal-21102026).

Próximo recorte recomendado: desenhar/aprovar VS-02B para consumir `RunSummaryStore`, sem recalcular parcelas, prometer contribuição individual não demonstrada ou confundir visões com somas adicionais. A UI futura precisa tratar dados ausentes, dano de habilidade desconhecida, última sessão e outcome. Preservar a trilha das 11 falhas históricas e investigar separadamente a saturação sensível à execução. Não iniciar UI, conteúdo ou mudanças de gameplay sem nova autorização.

## Atualização VS-01.1 — 21/10/2026

**Gate de estabilidade investigado fechado tecnicamente:** a reprodução confirmou wrappers de projéteis destruídos mantidos no pool estático após descarregamento da cena. Correção no adaptador existente `ComponentPool`, regressão vermelha→verde e três runs publicadas sucessivas com tiros e zero erros. São 75 casos PlayMode aprovados nos filtros atuais; evidências e limites na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md).

A investigação recomendada na atualização VS-01 abaixo foi executada; não permanece como causa desconhecida. Não é autorização para ampliar gameplay. Próximo passo recomendado: validar o escopo da VS-02 e seus critérios de resultado/retorno, preservando o gate humano de combate e as 11 falhas históricas restantes. Execução assistida não substitui partida natural. Reservar espaço em disco antes de novas capturas: houve bloqueio real do Editor por D: abaixo de 1 GB.

## Atualização VS-01 — 20–21/10/2026

O plano de 20/10 abaixo permanece histórico/proposto, com este avanço autorizado: fixture Elite corrigida e percurso de produção com aquisição de Eclipse Uppercut→combate Elite→abandono/Nexus observados sob automação assistida. Não chamar esse percurso de partida natural. Testes e evidências na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md#vs-01--mutation-adquirida-e-combate-elite-2021102026).

- **Gate técnico da fixture Elite fechado:** ordinais vigentes, spawn/rank/perfil, duas aquisições e release verificados; `bulwark` permanece aposentado. Demais falhas herdadas seguem separadas e não foram ajustadas para obter verde.
- **Próxima investigação de estabilidade recomendada:** reproduzir segunda run na mesma sessão após Nexus, pois houve cinco exceções em `EnemyProjectile.Spawn:373`. Inspecionar pool e teardown antes de corrigir; a hipótese de ciclo de vida ainda não é causa confirmada. Afeta ameaças de combate e bloqueia declarar o ciclo repetido estável. Não há autorização implícita para alterar IA/resistências.
- **Gate humano pendente:** jogar o percurso com controles físicos, perceber Q transformada, avaliar aproximação/contato, legibilidade e justiça do Elite. A automação corrigiu sua própria mira; isso não aprova nem altera o targeting de produção.
- **Escopo futuro por aprovar:** resultado consultável no Nexus, breakdown por habilidade e recorte vitória/boss versus abandono. O teste terminou em `Aborted`, sem criar extração após Elite. Nenhuma nova Mutation, interface, habilidade ou mecânica foi implementada.

## Fase 0 — Higiene e verdade do projeto

- Inventariar árvores de cenas, scripts, SOs, tests, Docs existentes, executores e dependências.
- Confirmar conexão do MCP e estabelecer um smoke test de build/cena.
- Manter a estrutura atual de Docs como fonte oficial e conferir referências. Não restaurar documentação antiga excluída intencionalmente. Verificar exclusões não documentais conforme [05_MIGRACAO_E_LIMPEZA.md](05_MIGRACAO_E_LIMPEZA.md).
- **Gate:** mapa de arquitetura com fontes verificadas, duplicações suspeitas e pendências abertas.

## Fase 1 — Visibilidade da IA

- Reutilizar as rotinas de captura já existentes, inventariadas em [validation/02_CAPTURA_E_INSPECAO_VISUAL.md](validation/02_CAPTURA_E_INSPECAO_VISUAL.md); o ciclo integrado foi implementado e utilizado nas Gameplay Intelligence 1.0–1.3. A cobertura de cada cenário continua distinta.
- Cobrir HUD, swarm, reação manopla em comum/elite, arco, lança, boons e fim de run.
- Capturar vídeo + eventos para gameplay dinâmico; PNG para composição/legibilidade.
- **Gate:** baseline repetível na mesma câmera/seed/resolução e evidências identificadas por commit.

## Fase 2 — Confiabilidade do combate

- Auditar pipeline de ataque, dano, causal roots, stagger/stance/launch, custos/cooldown, cancel e cleanup.
- Checar regressões das três armas sem novas features.
- **Gate:** testes representativos Unity + playtest humano do kit base, pendências claras.

## Fase 3 — Boons e sinergias

- Usar o catálogo atual e a [matriz de validação](validation/01_MATRIZ_DE_VALIDACAO.md): há resultados Unity históricos de 18/10 para boons; completar os gates em aberto e repetir verificações quando a revisão/configuração mudar.
- Garantir ofertas reais, combinações legais, limites de proc e feedback audiovisual.
- **Gate:** seleção → aplicação → persistência → execução → finalização demonstrada em gameplay, com teto de runtime.

## Fase 4 — Aparência e UI

- Escolher direção visual a partir de estudos comparáveis; manter receita única de assets.
- Iterar HUD/telegraphs/VFX/ambiente com screenshots e máximo de 2 problemas prioritários por rodada.
- **Gate:** leitura do jogador/ameaça, consistência de materiais e performance na resolução alvo.

## Fase 5 — Polimento do loop

- Consertar fricções da run e fechamento de tela de estatísticas; expandir conteúdo só após qualidade da base.
- **Gate:** teste ponta a ponta com evidências e aprovação autoral de experience/feel.

## Política de priorização

Defeitos que inviabilizam jogar, corrupção de save/GUID, perda de input e duplicação de dano/custo têm prioridade sobre novos boons. Nada de expandir três frentes ao mesmo tempo sem razão explícita.

## Vertical Slice 1.0 — plano para aprovação (20/10/2026)

**PROPOSTO; nenhuma implementação autorizada por este plano.** A visão já é a Bible distribuída por fontes canônicas, sem novo arquivo mestre. Fantasia, filosofia, identidade das armas, boons e vetos estão claros. O que ainda falta decidir é o recorte demonstrável, o fechamento dos resultados e a avaliação humana de diversão/leitura. Estado por etapa em [estado e lacunas](02_ESTADO_E_LACUNAS.md); execução Unity e dívida herdada na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md).

### Menor percurso aproveitando a produção

Recomenda-se **Nexus → Manopla → 0/S → 1/C1 → 2/C2 → 3/reparo → 5/C4/Mutation → 7/Elite → recompensas → abandono → Nexus** como primeiro percurso de avaliação da build. São quatro encontros de combate, além do reparo; não um encontro seguido imediatamente de Mutation/Elite. O ramo 3 evita o combate adicional de 4, preservando a escolha existente. Recompensas do primeiro encontro são Technique + Protocol; a oportunidade de Mutation está em 5. O ramo 7 é opcional no jogo, mas necessário no roteiro de avaliação do slice.

Esse recorte **não é uma vitória**: o abandono mantém `Aborted`. Extração vitoriosa só existe após o boss 10; o caminho com Elite continua **7 → 8 → 9 → 10 → portal** (sete combates no total pelo ramo de reparo). Não criar extração no Elite, saltar gates ou mover recompensas para encurtar o percurso sem nova decisão. A randomização conserva grafo/ranks, mas altera composição/recompensas; seed de QA é evidência reproduzível, não mudança da produção.

### Decisões de escopo necessárias

| Decisão pendente do desenvolvedor | Recomendação / consequência |
|---|---|
| Slice exige vitória/extração ou aceita primeiro marco encerrado por abandono após Elite? | Aprovar o recorte curto como marco intermediário; manter boss/extração como gate separado antes de chamar o percurso vitorioso de entregue. Não transformar abandono em sucesso. |
| Qual Mutation/transformação existente demonstrará a build? | Comparar as ofertas vigentes em 5, como EclipseUppercut ou Asura Cadenciada, e escolher pelo playtest. Não fixar oferta, conceder energia nem ativar boons antigos. Se Cadenciada for escolhida, acumular Asura pelo combate real. |
| Qual resultado mínimo deve ser consultável no Nexus? | Proposta: outcome, encontros concluídos e boons adquiridos, a partir de fatos da sessão; dano por habilidade continua intenção aprovada e requer atribuição/reconciliação. Aprovar explicitamente se esse breakdown pode ficar para marco seguinte. Não substituir a decisão de produto por um resumo pobre por conveniência. |
| Qual nível visual é aceitável no slice? | Manter arte provisória coerente e corrigir bloqueios de leitura comprovados; não exigir paleta/shader novos para o primeiro percurso. Receita final continua dependente de estudos/aceite. Validar resolução-alvo e legibilidade dos pedestais. |

### Fatias verticais e dependências

Cada fatia termina em experiência verificável. Arquivos citados são **potencialmente afetados em tarefa futura**, não autorização de edição agora. A trilha de estabilidade abaixo pode investigar fixtures em paralelo ao planejamento, sem ampliar gameplay.

| Fatia / dependência | Objetivo percebido / componentes reutilizados e sistemas potencialmente afetados | Aceite técnico | Avaliação humana / evidências | Riscos |
|---|---|---|---|---|
| **VS-A — uma incursão curta completa**; primeira recomendada | Encontrar/equipar Manopla, entrar, vencer C1, escolher todas as recompensas e abandonar com retorno. Reutilizar `LobbyArsenal`, `LobbyInteraction`, `WeaponLoadout`, `ManualRunUI`, `ProgressionDirector`, `RunBoons`, tickets e observação. Potenciais ajustes só nesses pontos de integração e respectivos testes/ferramentas, após aprovação. | Roteiro reproduzível em cena publicada, HP/dano/postura de produção, movimento/ataques reais; duas aquisições de C1, uma por confirmação; nenhuma entrada/input residual; `Aborted` uma vez, novo Nexus sem boons/hooks da sessão anterior. Restaurar prefs/Editor no teardown. Migrar a fixture Elite para drenar ordinais reais sem alterar política. | Jogador identifica arsenal/incursão/recompensas e entende por que voltou. Capturar pedestal, entrada E, encontro com papéis identificados, último hit→reward, aquisição, abandono e retorno; PNG/linha temporal, vídeo humano se necessário. | Focus/input, gates, morte simultânea, Spawner/filhos na fronteira, confirmação duplicada, prefs e cleanup. Não utilizar mortes injetadas como aceite de gameplay. |
| **VS-B — a escolha muda o próximo combate**; VS-A | Percorrer 1→2→3→5, adquirir Mutation pela oferta real, experimentar no 7/Elite e sair pelo fluxo já existente. Reutilizar randomizador/grafo, `ManualRewardCatalog`, inventário, executores da Manopla e perfis de inimigos. Potenciais sistemas: apresentação/card/execução do boon escolhido e captura existente, somente se houver defeito comprovado. | Sem mudar catálogo/pesos, uma Mutation por slot; recibo aplicado uma vez; comparar base/transformada no mesmo contexto quando possível; registrar HP, mana, Asura, cooldown, impactos, reação/resistência Elite, pausa/cancelamento e teardown. Preservar contratos GI1.0–1.3; regressão Arco/Lança se houver toque compartilhado. | A pessoa descreve mudança de decisão/risco/geometria/input sem consultar logs; Elite exige resposta e não fica permanentemente controlado. Evidências antes/depois da skill, card, inventário, contato, recuperação e retomada do Elite. | Mutation não utilizada por energia/slot, card promete outro efeito, sobreposição de VFX, boon raro mascarando kit base, recursos insuficientes para chegar ao Elite. Balanceamento só após diagnóstico e autorização. |
| **VS-C — o percurso tem fechamento compreensível**; VS-A e aprovação dos resultados; VS-B para avaliar build | Consultar resultado após retorno ao Nexus, preservando morte/abandono/extração. Reutilizar `RunSessionState`, aquisições e lifecycle; UI de resultado/lobby e armazenamento/transporte mínimo a definir no desenho técnico, sem novo manager global por padrão. | Congelar fatos antes do teardown, transmitir uma vez, distinguir outcomes e sessão nova, sem reaplicar recompensas. Se houver dano por habilidade, total reconciliado com dano real e efeitos derivados, sem dupla contagem. Dados ausentes não viram zero inventado. | Jogador entende o desfecho, a build utilizada e o que aprendeu. Capturar resultado em saída e Nexus; confrontar campos com eventos/estado bruto. | Sessão destruída antes da coleta, resumo antigo exibido como novo, contagem duplicada, persistência/save sem contrato. Conteúdo mínimo depende de aprovação. |
| **VS-D — conclusão vitoriosa e repetição**; VS-B/C e aprovação do recorte | Prosseguir 7→8→9→10, vencer boss, extrair e iniciar outra incursão; morte/abandono como alternativas reais. Reutilizar boss/ranks, portal, loader, geração, prefs e loadout. Potenciais arquivos: lifecycle, encontros, navegação/retorno e testes afetados por defeito demonstrado. | Caminho natural completo, vitória somente após boss e portal; morte na mesma fronteira vence a resolução; retry não duplica resultado/recompensa; nova sessão limpa. Build smoke no alvo acordado, Console sem erros e regressão das três armas pertinente. | Dificuldade/tempo aceitáveis, ameaça legível e vontade de tentar outra build. Capturar Elite, boss, extração, resultado e segunda entrada; registrar feedback do desenvolvedor. | Duração ainda não medida, boss/budgets sob build real, desempenho/VFX/GC, fechamento em cena nova e portabilidade do pacote local Tripo3D. |

**Ordem recomendada:** aprovar recorte/resultados → VS-A → VS-B → VS-C → VS-D, podendo desenhar VS-C após a decisão de conteúdo enquanto VS-B é avaliada. Não declarar Vertical Slice 1.0 completo sem os oito critérios solicitados; o recorte curto é marco intermediário, não dispensa resultado no Nexus nem concede aceite humano.

### Trilha independente de estabilidade herdada

Não exigir verde artificial da suíte antiga. Os 12 casos e suas evidências continuam na matriz; não reativar `focus`, PressureField/Shockwave aposentados da oferta, mudar limites de Mutation ou enfraquecer asserts. Migrar fixtures mantendo o contrato que pretendem provar.

- **Gate de evidência da rota:** fixture Elite não drena todas as recompensas atuais; causa reproduzida na auditoria. Corrigir a cobertura em tarefa aprovada, sem presumir que o fluxo de produção falha: os quatro caminhos publicados passaram nesta auditoria, com limitações de combate descritas na matriz.
- **Gates do slice que precisam prova vigente:** clique stale/revisão de recompensa, esgotamento/fallback e aborto pela pausa indisponível. Fixtures antigas não chegam a esses asserts; o smoke de abandono pela UI normal não substitui o caso de pausa. Podem ser cobertos por testes atuais equivalentes, sem obrigação de preservar o setup obsoleto.
- **Risco condicionado à build:** falhas de FlurryEcho com presets PressureField/Shockwave exigem isolar o suporte retido em configuração vigente antes de incluir essa combinação como evidência do slice. Não excluir FlurryEcho da produção nem reativar presets para passar testes.
- **Dívida investigada sem bloqueio mecânico demonstrado:** `focus` fora da oferta; fixture de HUD com duas Mutations Q; query global de popups sensível a contexto; DemolishingAdvance e repetição de básico sensíveis à execução. Estes dois últimos não têm causa definitiva. Se aparecerem no percurso natural ou comprometerem leitura/dano/input, passam a bloqueantes pelo impacto real.

**Bloqueios efetivos atuais:** interface de resultado no Nexus ausente (backend disponível na VS-02A); percurso natural build→Elite e aceite humano ainda não demonstrados; escopo do desfecho/resultado por aprovar. Não há evidência de que todas as 12 falhas impeçam jogar. Preservar baseline/capturas GI1.0–1.3, dados de produção, alterações locais e as exclusões preexistentes.

## Gameplay Rework — gate GPR-00,26/10/2026

Pedido atual prioriza GPR-C01 (WASD/mira/mouse/QERX/remapeamento) antes de GPR-01 (mutações/skills). Apenas auditoria/desenho GPR-00 foi autorizado. [Consolidado e pendências](decisions/GPR-00_PHASE_SUMMARY.md), [ledger](decisions/GAMEPLAY_REWORK_PROGRESS.md), [decisões D01–14](decisions/ADR-0006-GPR-DIRECAO-E-GATES.md). STOP para aprovação; próxima tarefa recomendada C01.1a somente após autorização explícita. Baseline visual/física deve preceder qualquer mudança de runtime;19 testes focados atuais não aprovam feel.

### Gate atualizado após aprovação GPR-00

O desenvolvedor aprovou GPR-00 como auditoria/documentação com ajustes e autorizou somente C01.1a/b. Ambas foram executadas para baseline técnica/virtual e design; [prontidão](decisions/GPR-C01.1_BASELINE_E_PRONTIDAO.md). Direção vigente: um cliqueLMB pede um básico; RMB é especial/Asura, independente de quatro slotsQERX. Sem hold automático aprovado, Interact/motor/dash/X inicial ainda por decidir. Próxima proposta: C01.2a como protótipo isolado, sob nova aprovação; nenhuma integração no player nem GPR-01 iniciada.
