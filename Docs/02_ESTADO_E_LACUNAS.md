# Estado verificado, conflitos e lacunas

## RUN-EXP-01 —30/10/2026

Correções localizadas de pull encadeado e silhueta de fogo implementadas; auditoria de recompensas/salas/inimigos e protótipos isolados entregues. [Plano integrado e revisão](gameplay/TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md).10/10 focados finais,37/37 EditMode e1/1 estudo;322/353 ampliado mantém31 falhas e investigação documentada, sem encerramento fictício deGPR01. Reconhecimento humano do fogo/tripla e da direção geométrica continua pendente.

Confirmado no Unity:16 prefabs com mesmas sete malhas/controller; formas novas são apenas estudos geométricos fora da progressão. Assistência temporária A+B está ligada no Editor e foi preservada, podendo distorcer playtest de descoberta; desligar explicitamente para testar a economia normal. Não foram alterados RNG, tickets, economia, Nexus/FirstSector, modelos produtivos, kits ou QER modular. Próximas implementações dependem da revisão; STOP após2. Relatórios e matriz distinguem código, técnica, hipótese e aceite.

## Estado vigente — auditoria GPR-01.5, 29/10/2026

**GPR-01.5 concluída como auditoria, com ressalva de estabilidade e aceite físico pendente. STOP após01.5.** O desenvolvedor aceita01.4 como implementada/demonstrada no foco documentado; não aceitou fisicamente as novas camadas fogo/eletricidade ou a tripla. Gameplay01.3 já foi aceito. O histórico199/203 +4/4 isolados de01.4 não foi reclassificado como203/203.

A+B+C foi obtida por tickets reais, sem grants de boons, nos quatro percursos publicados deFirstSector, com assistênciaA+B desligada/restaurada no scope dos testes. Combate/conclusão dos encontros foram automatizados, não humanos. A rota atual usa geração e tem umaMutation e2–7 oportunidadesProtocol na amostra; a antiga conclusão de uma única oportunidade limitava-se à fixture. Nenhum peso/frequência/gate/sala/economia foi alterado.

Modelo determinístico64 seeds×4 caminhos×3 políticas, sorteio/commit de produção:ABC253/256 priorizandoB,252/256 priorizandoC,42/256 escolhendo uniformemente entre cards sorteados. Não são probabilidades universais nem garantia de sobrevivência/diversão. Quatro comparadores reais na câmera normal, com vários inimigos/telegraphs, mostram vento/fogo/eletricidade e feedback causal separado; leitura física continua pendente.

Validação desta atividade:98/98 focados;3/3 EditMode de amostra;8/8 produção (4rotas+4visuais);5/5 correções de ambiente/probe;10/10 checks finais;90/90 EditMode final. Regressão ampliada **203/204**: caso de pausa/recovery falhou emIsCasting após0,4s reais de pausa, passando inalterado no diagnóstico posterior. Causa ainda não confirmada, risco temporal aberto. Não declarar a bateria totalmente verde ouGPR-01 encerrada.

Somente testes/documentação mudaram: diretórios de evidência criados pelos writers; helperAsura exige recovery ainda estacionária, porque a espera móvel também reportaRecovery/progress1 e era admitida antes. Probe reproduziu a premissa incorreta; não prova retrospectivamente toda falha histórica. Nenhum timer/TTL/buffer/cadência/animação ou owner de produção alterado. Medições8/32/64 mantêm dois tornados/pool2 e cap32, com limites de Editor/GC agregado.

Antes de encerrarGPR-01 ou autorizar01.6: diagnosticar pausa/recovery e obter revisão física das camadas/tripla/telegraphs. Auditoria futura dedicada de recompensas/ritmo, layouts, identidade visual de inimigos e bosses é apenas proposta. Singularity, tuning, economia nova, novasMutations/especiais/skills e redesign não foram executados.

[Relatório consolidado e evidências](decisions/GPR-01.5_RELATORIO.md). Relatórios01.5a/b/c/d preservam comandos, jobs, falhas e limites.

## Histórico anterior — não sobrepõe GPR-01.5


## Estado vigente — GPR-01.4, 29/10/2026

O pedido direto aceita fisicamente01.2c/01.3 e autoriza sequencialmente01.4a–e, incluindo transformação visual obrigatória. **GPR-01.4 implementada, com validação integrada e ressalva legada; STOP após01.4.** GPR-01.5, Singularity, novos especiais, troca modular, tuning, inimigos e salas não foram executados. Todo estado anterior abaixo é histórico onde divergir deste registro.

A=mutation_gauntlet_titan_fists; B=conductor; C=detonation, mesmos IDs/catálogos/tickets. Base/B/C/B+C mantêm execução nativa e não criam tornado. A viaja como vento; A+B acrescenta identidade elétrica e uma oportunidade Conductor; A+C acrescenta fogo e uma oportunidade Detonation em morte confirmada; A+B+C compõe vento, eletricidade e fogo, permitindo morte pelo Conductor derivado autorizar Detonation. Fogo visual não aplica burn/DoT.

Raiz e budget32 compartilhados; uma emissão Conductor e uma Detonation por tornado. Sem Conductor→Conductor, Detonation→Detonation/Conductor ou tornado→tornado no ramo novo. Native B+C preservado, inclusive suas cascatas e dedupe: não aplicar a whitelist nova globalmente. Soco e vento podem conduzir separadamente quando existem alvos elegíveis e capacidade.

Ownership visual no GauntletExecutionSnapshot aceito; ranks mecânicos na raiz aceita. A reserva intersecta ownership/snapshot e inventário, e o visual recebe essa mesma política. Aquisição durante antecipação/voo não altera projétil anterior. Cosmetic Completed conserva identidade aceita, sem root, HP, arco externo ou explosão. Pool/leases limpam fogo e eletricidade em release/clear/swap/disable/run/cena/contexto. Geometria, velocidade3,5m/s, alcance5,5m, TTL2s, raio0,65m e dano0,60× preservados.

Validação real Unity6000.5.10f1/MCP no checkoutD:/Dev/Personal/My_games/Tech-Guy:98/98 focados,12/12 visual/lifecycle,7/7 integração e90/90 EditMode. Regressão ampliada199/203; três falhas de diretório corrigidas na preparação e4/4 no diagnóstico isolado. Uma ocorrência de Asura cadenciada permanece sem causa confirmada; diagnóstico verde não é correção ou bateria ampliada inteiramente verde.368 nomes distintos com resultado aprovado observado (278 PlayMode+90 EditMode), sem ignorados nas seleções finais; não suíte global nem novo aceite humano.

Tickets reais A+B seed0 e A+C seed1; quatro ordens triplas assistidas explicitamente. Rota normal exercitada oferece somente uma oportunidade Protocol: não foi ampliada para fabricar aquisição tripla natural. Capturas Game View, receipts separados, desempenho controlado8/32/64 e todas as tentativas no consolidado e artefatosGPR-01.4. Playtest físico de leitura/feel desta revisão continua pendente.

[Consolidado](decisions/GPR-01.4_RELATORIO.md) · relatórios separados01.4a/b/c/d/e e evidências novas vinculadas nele.

## Histórico anterior — não sobrepõe o estado GPR-01.4


## Atualização — 29/10/2026, GPR-01.2c /01.3

Tornado01.2b aceito fisicamente como funcional/divertido. Lifecycle após clear corrigido com vento cosmético fora de Encounter; composiçãoA+B implementada sob grantConductor específico, sem Detonation ou tuning. [Relatório01.2c](decisions/GPR-01.2c_RELATORIO.md) / [relatório01.3](decisions/GPR-01.3_RELATORIO.md) delimitam testes e evidências atuais; composição aguarda revisão humana. Basic cadence, moonwalk, tornado speed/width/aim, reward economy, stages/pacing, enemy mesh variety, elites/bosses e Singularity permanecem tarefas futuras sem autorização de implementação. STOP após01.3.

## Histórico — GPR-01.2b, 28/10/2026

A onda instantânea anterior foi aceita tecnicamente pelo desenvolvedor após teste físico, mas rejeitada como gameplay final. Punhos Titânicos agora emite um **tornado móvel de vento** em cada contato terminal executado, inclusive whiff:5,5m/3,5m/s, dano0,60× pacote aceito não crítico, sweep/LOS e mesma raiz/caps. Sem novo grant de Conductor/Detonation (`ProtocolBasicEffects.None`), sem tuning do kit nativo ou mudança de controles. [Relatório da revisão, evidências e limitações](decisions/GPR-01.2b_RELATORIO.md). **Aceite físico do tornado pendente; STOP antes de GPR-01.3.**

## Histórico da entrega GPR-01.2 anterior à revisão

### Registro histórico — GPR-01.2 — 28/10/2026

Primeira Mutation da Manopla implementada: Punhos Titânicos, com onda/visual reversíveis, recibo nativo de HP e grantsNone. [Relatório, testes, limitações e próximo gate](decisions/GPR-01.2_RELATORIO.md). Nenhum aceite físico/artístico de A foi recebido. GPR-01.3 permanece não autorizada. O estado01.1 abaixo é histórico.



## Histórico da fundação01.1

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Ambiente e fronteiras

- [ProjectVersion](../ProjectSettings/ProjectVersion.txt): Unity **6000.5.10f1**. [Packages](../Packages/manifest.json): URP **17.5.0**, Input System **1.20.0**, Test Framework **1.7.0**, Unity MCP declarado em **v10.3.0**. [GraphicsSettings](../ProjectSettings/GraphicsSettings.asset) aponta para o asset URP em `_Project/Settings/Rendering`.
- Fluxo atual (28/10/2026): `MainMenu → PrologueTutorial → NexusBlockout → FirstSector → NexusBlockout` (o prólogo depende do save). `NexusBlockout` é o Nexus com salas. `NexusLobby` permanece habilitada apenas para fixtures legadas e como fonte de ferramentas; `Playground` e `CombatStudy` permanecem desabilitadas, mas são fontes de geradores; `ProceduralStageAuthoring` é usada pela validação de rig fora do build. Ver [restauração e auditoria de cenas](decisions/NEXUS_RESTAURACAO_20261028.md).
- Código próprio, arte e cenas em `Assets/_Project`; terceiros em `Assets/_ThirdParty`; duas raízes Resources com chaves distintas. Ver [arquitetura](04_ARQUITETURA_E_INTEGRIDADE.md).
- Tripo3D usa caminho absoluto local no manifest de pacotes: portabilidade para outra máquina não comprovada. Pacotes não foram alterados nesta consolidação.
- Na consolidação inicial não havia MCP exposto. Na implementação de observação em 18/10/2026, a conexão ao vivo foi confirmada com `Tech-Guy@fccafb2b47cd7671`, projeto `D:/Dev/Personal/My_games/Tech-Guy`, inicialmente NexusLobby em Edit Mode. Captura e testes posteriores estão na matriz; reconfirmar a conexão em cada sessão.

## Implementação localizada e limite da evidência

| Área | Encontrado em arquivos atuais | Validação/aceite |
|---|---|---|
| Controle e combate | Input remapeável, QWER/dash/F, buffer/cancel por perfil, dano confirmado, mana e políticas Asura; [combate](systems/01_COMBATE.md) | Existência e encaminhamento inspecionados; responsividade e feel exigem execução e aceite humano |
| Armas | Kits de manopla/arco/lança, executores específicos e assets; custos efetivos podem diferir dos campos brutos; [armas](systems/02_ARMAS.md) | Não declarar tuning ou apresentação finais só pela leitura dos valores |
| Inimigos | IA, comportamentos por arquétipo, coordenação, resistência/postura e telegraphs; [inimigos](systems/03_INIMIGOS.md) | Justiça e legibilidade em swarm continuam gates de gameplay |
| Boons | Catálogo serializado, definições executáveis, compatibilidade, flags e bloqueio explícito de Mythics; [boons](systems/04_BOONS.md) | Há resultados Unity históricos posteriores a 17/10; escopo e limites na matriz, sem equivaler a aceite humano |
| Run | `FirstSector` seleciona o fluxo de rota com randomização, políticas de sessão e tickets de recompensa; [run](systems/05_RUN_E_PROGRESSAO.md) | Simulação de ofertas não comprova percurso jogado, sobrevivência, duração ou balanceamento |
| HUD e resultado | HUD de recursos/skills, cards, pausa/mapa e retorno; [HUD](systems/06_HUD_E_UI.md) | O fluxo de resultado localizado não entrega o breakdown de dano por habilidade da direção de produto |
| Arte | Pipeline URP e fontes do personagem existem; [pipeline](art/02_PIPELINE_DE_ASSETS.md) | Checkpoint de autoria não comprova integração Unity; paleta/shader final não certificados |
| Evidência | Resultados anteriores de EditMode/PlayMode, round-trip e simulações; [matriz](validation/01_MATRIZ_DE_VALIDACAO.md) | Resultados preservados e agregados, não uma nova execução da suíte completa sobre esta consolidação |

## Conflitos e lacunas em aberto

| ID | Intenção preservada / constatação | Tratamento |
|---|---|---|
| L01 — resultados | Backend VS-02A e tela uGUI/TMP no Nexus implementados na VS-02B.2, com snapshot e consulta manual | Validação técnica nos filtros da matriz; direção provisória, aceite humano e reinício standalone pendentes. Ver HUD/UI e matriz VS-02B.2 |
| L02 — aprovação de gameplay | Identidades, reações, boons transformadores e legibilidade são regras aceitas; testes técnicos não provam diversão/clareza | Aceite específico de gameplay continua humano. Esta consolidação não concede novos aceites |
| L03 — animação Q | A autoria tem ação completa Q; o player de animação localizado usa movimentos/contatos por golpe | Integração e revisão na câmera real pendentes; não ligar o vídeo de autoria ao runtime automaticamente |
| L04 — cobertura e exclusões | Há exclusões pendentes de fontes/testes, anteriores à consolidação | Riscos separados em [05_MIGRACAO_E_LIMPEZA.md](05_MIGRACAO_E_LIMPEZA.md); não reverter nem confirmar exclusões |
| L05 — ciclo visual | O workflow da [ADR-0005](decisions/ADR-0005-CICLO-DE-CAPTURA.md) foi autorizado e estende o preview existente; manopla capturada/comparada | Demais perfis têm cobertura distinta e limites em [captura](validation/02_CAPTURA_E_INSPECAO_VISUAL.md); game feel segue dependente de aprovação humana |
| L06 — identidade/contato das armas | A lança avalia sweet spot com ponta sintética; Flow não tem consumidor runtime localizado; marca/carga do arco não estão integralmente no kit base | Conflitos de integração/identidade em [armas](systems/02_ARMAS.md#conflitos-e-lacunas-conhecidos); preservar a direção e exigir tarefa de gameplay para reconciliar |
| L07 — oportunidade de sinergia | A simulação V2 do arco não completou requisitos nem ofertou/adquiriu `duo_bow_eclipse_archer` nas 600 runs consultadas | Lacuna de oportunidade, não prova de preferência/diversão humana; manter requisitos/pesos até decisão autorizada. Ver [boons](systems/04_BOONS.md) e matriz |

Descrições antigas de run linear, bloqueio universal durante cast e custos fixos foram substituídas por fontes atuais; isso corrige documentação, sem aprovar nem alterar o comportamento. Divergências futuras entre código e regra de produto devem ser registradas aqui com fonte e impacto antes de qualquer decisão de gameplay.

## Prontidão do Vertical Slice 1.0 — auditoria de 20/10/2026

Reinspeção na mesma branch/base, com alterações locais: leitura dos canônicos/código e Unity MCP real, instância `Tech-Guy@fccafb2b47cd7671`, Unity 6000.5.10f1, projeto `D:/Dev/Personal/My_games/Tech-Guy`. Não foram implementadas mecânicas. Método, resultados, capturas e limites na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md); roteiro/decisões na [prioridade do slice](03_PRIORIDADES.md#vertical-slice-10--plano-para-aprovação-20102026).

### Qualidade da Bible atual

| Tema | Avaliação e lacuna comprovada |
|---|---|
| Fantasia e sinopse | Claras na visão e README: desenvolvedor no universo digital, Nexus, armas expressivas e builds. Não foi localizado arquivo autônomo de sinopse na árvore atual; não restaurar o antigo nem criar duplicata. Diálogo das armas é intenção, não entrega comprovada. |
| Combate e armas | Pilares, controle, resistência/reação e três identidades bem definidos em fontes próprias. GI1.0–1.4 acrescentaram evidência técnica da Manopla, incluindo HP do básico no contato autorado; não aprovam diversão nem eliminam lacunas de identidade de Arco/Lança em L06. Ver matriz para cobertura e pendências de cada iteração. |
| Loop, boons e progressão | Contratos de rota/recompensa/Mutation e persistência existem. A visão não explicitava o ciclo completo Nexus→build→encerramento→Nexus; adicionada síntese com links, sem duplicar regras. Oportunidade de escolha e percepção da transformação continuam gates humanos. |
| Encontros e recompensas | Implementação tem composição por seed, papéis, Elite e boss, com ramos exclusivos. Ritmo/duração/dificuldade ideais e recorte do slice não estão aprovados nem medidos. Não inventar curva ou novos encontros. |
| Arte e legibilidade | Direção macro e hierarquia claras; paleta/shader/mundo finais permanecem por decidir. Captura atual do Nexus mostra sobreposição de rótulos dos pedestais a 1160×599, sem aprovação estética. |
| Consolidado, experimental e rejeitado | Vetos estão nos pilares/visão; ofertas vigentes, conteúdo desligado e propostas são separados em boons. Síntese acrescentada à visão; testes não promovem hipótese a design aprovado. Não tratar todo conteúdo desligado como definitivamente rejeitado. |

A documentação é suficiente para orientar trabalho coerente, mas não para anunciar um slice fechado: faltam escopo aprovado, prova do percurso natural e avaliação humana. Não há necessidade de outro documento chamado Game Design Bible.

### Estado das oito etapas solicitadas

“Funcional e validada” nesta tabela significa **o escopo técnico descrito**, nunca gameplay aprovada. Testes com mortes determinísticas/teleporte não são partidas naturais.

| Etapa | Classificação | Evidência e limite / dependência |
|---|---|---|
| 1. Iniciar no Nexus | **Funcional e validada** no Editor | Nexus real em Play Mode; arsenal, jogador e HUD ativos. Entrada publicada MainMenu/prólogo não reexecutada nesta auditoria. |
| 2. Escolher/equipar Manopla | **Funcional e validada** por API/loadout; interação completa sem validação integrada | Pedestais referenciados, `WeaponLoadout.Select(player,0)` e HUD da Manopla observados; testes de troca/persistência passaram. Smoke não repetiu a tecla E para trocar no pedestal: interação e descoberta ainda precisam playtest. |
| 3. Encontro com funções distintas | **Funcional e validada** na geração/materialização; sem aceite de composição | Rota publicada randomizada e testes de spawn; smoke registrou C1 com Spawner/Fragile/Grunt/Heavy/Shooter. Snapshot não significa que esse encontro foi vencido naturalmente. GI anteriores cobrem combate curto, não a run inteira. |
| 4. Concluir encontro/receber recompensa | **Funcional e validada** sob fixtures de integração | Fronteira, tickets, dois ordinais de C1, reabertura e liberação de input passaram. Um teste mata por dano injetado; outro baixa HP para 1 antes do básico. Sem prova nova de vitória com stats de produção. |
| 5. Boon perceptível | **Funcional, mas sem validação integrada** da experiência | Oferta/aquisição nas cenas publicadas e execução de Cadenciada em fixture passaram. Essa fixture concede recursos/energia e usa alvo artificial. Não prova que a pessoa obtenha/use/perceba a transformação no percurso real. |
| 6. Elite com build modificada | **Funcional, mas sem validação integrada** de combate | Nó 7 e rank Elite reais; teste de quatro caminhos passa por Mutation→Elite, mas mata deterministicamente. Teste isolado de autoria falha antes do Elite por ordinal pendente. Percepção/resistência/sobrevivência continuam pendentes. |
| 7. Concluir ou abandonar | **Funcional e validada** nos caminhos técnicos descritos | Extração e morte em teste de cenas publicadas; abandono via `ManualRunUI.TryAbortRun` e retorno reais no smoke. Pausa/Mutation indisponível continua com fixture herdada inválida. Não há extração após Elite: conclusão vitoriosa exige boss 10. |
| 8. Retornar e ver resultados no Nexus | **Parcialmente implementada** | Retorno real confirmado. Resultado básico existe durante `Returning` em FirstSector; não foi localizado transporte/painel da última run no Nexus e ele não apareceu no smoke. Breakdown de dano por habilidade **ausente**. Conteúdo mínimo e adiamento analítico **bloqueados por decisão de escopo**, não por falta de teste. |

Não foram encontradas razões para reimplementar hub, inventário, grafo, tickets ou saída. As dependências críticas são integração natural/observação da build, fechamento dos resultados e definição autoral do recorte. Nova implementação aguarda aprovação do plano.

## VS-01 — atualização de 20–21/10/2026

Esta atualização sucede a auditoria acima, sem reinterpretar seus resultados históricos. Mesma branch/base com alterações locais; Unity MCP confirmou o projeto e a instância reais. Evidências, testes e limitações detalhados na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md#vs-01--mutation-adquirida-e-combate-elite-2021102026).

| Critério | Estado atual e limite |
|---|---|
| Fixture isolada de Elite | Corrigida e aprovada: consome todos os ordinais prévios e usa duas ofertas vigentes, sem restaurar `bulwark`. Verifica 1 Elite + 4 comuns, perfil/postura, fronteira e release entre aquisições. Não é evidência de combate natural. |
| Encontros e recompensas com atributos de produção | Observados no percurso Nexus→0→1→2→3/reparo→5/Mutation→7/Elite. Navegação, mira, comandos e escolhas assistidos; dano, IA, custos, recompensas e resistências executados pelos componentes existentes. Sem injeção de HP/dano/postura/recursos ou Mutation. |
| Transformação→Elite | Eclipse Uppercut adquirido por ticket real em 5, usado contra Elite em 7: custo efetivo 56, impacto confirmado de 36 HP, sem launch da Elite. Encontro concluído com 940,12 HP do jogador; abandono posterior retorna ao Nexus com `Aborted`. Validação integrada assistida, sem aprovação de diversão. |
| Repetir run após retorno | Risco concreto separado: cinco exceções em `EnemyProjectile.Spawn:373` numa segunda run da mesma sessão Play. Relação com ciclo de vida do pool é hipótese; não corrigida. Uma execução posterior iniciada em Play novo terminou sem erros, o que não resolve a repetição. |
| Resultados no Nexus | Retorno confirmado novamente; painel consultável da última run e breakdown por habilidade continuam lacunas. Não implementados nesta tarefa. |

113 casos PlayMode e 77 EditMode distintos passaram nos filtros registrados, incluindo regressões de combate e três armas. Não foi reexecutada a suíte ampliada inteira. A falha histórica da fixture Elite foi resolvida; as outras 11 não foram declaradas corrigidas. Nenhuma mecânica de gameplay foi alterada: somente fixture, correção necessária da captura `current` no Editor e documentação. Restam playtest humano, avaliação da legibilidade da transformação e investigação da repetição de runs antes de considerar o slice estável.

## VS-01.1 — estabilidade entre runs, 21/10/2026

O risco de `EnemyProjectile.Spawn:373` registrado na VS-01 teve **causa confirmada e correção validada tecnicamente**. O pool estático conservava wrappers de projéteis destruídos ao descarregar FirstSector; a aquisição seguinte devolvia null. `ComponentPool` agora recria seu armazenamento quando detecta raiz destruída, mantendo reuso na cena e cleanup preguiçoso no próximo Acquire. Contrato principal em [inimigos](systems/03_INIMIGOS.md#projéteis-entre-cenas--vs-011-21102026), prova e limites na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md).

| Critério | Evidência atual |
|---|---|
| Reprodução anterior | Três runs/retornos na mesma sessão Play: 1, 50 e 0 emissões observadas; zero/uma/uma exceção respectivamente. Raiz destruída e wrappers inválidos na fila registrados antes da aquisição que falhou; não foi injeção de estado inválido. |
| Regressão controlada | Teste novo falhou com a exceção original antes da correção; cinco casos de lifecycle passaram depois, incluindo AimedShot/SniperShot em três ciclos de cena, dono/alvo/listeners, morte e cancelamentos. |
| Percurso publicado após correção | Três runs Nexus→C1→abandono/Nexus, **sem sair de Play**: 8, 54 e 5 emissões observadas, zero erros. Incluiu LobShot/SniperShot, depois SpreadShot/AimedShot, depois SniperShot. Na segunda run havia seis projéteis ativos antes de descarregar. Sem alterar HP, dano, recursos, IA ou composição. |
| Referências e regressões | Nas 67 emissões observadas, raiz válida e zero itens Unity-null na fila; alvos ativos conferem com o jogador da run. Registry permaneceu em quatro callbacks durante a sequência. Total de **75 PlayMode distintos aprovados**, com regressões das três armas e Combat Feel; não representa suíte completa nem nova suíte EditMode. |

**Prontidão:** o bloqueio investigado da VS-01.1 está fechado no escopo testado, permitindo discutir/iniciar a próxima fatia aprovada da VS-02. Isso não aprova game feel nem o Vertical Slice inteiro. Permanecem resultados consultáveis no Nexus, escopo de desfecho, playtest humano e a dívida histórica não tratada. As runs foram assistidas e encerradas com `Aborted`, sem vitória/extração do boss. O aviso real de espaço baixo em D: foi contornado arquivando apenas capturas desta tarefa em C: com junções nos caminhos originais; o disco de projeto ainda exige atenção operacional.

## Manutenção

Atualizar esta seção e o canônico do tema com **fonte + método + data + revisão/configuração + limite da evidência**. Definições de status ficam em [README](README.md); resultados detalhados ficam na matriz. Não depender de documentos antigos intencionalmente excluídos nem de arquivos locais em Library para compreender o estado oficial.

## Gameplay Intelligence 1.7 — aproximação do Grunt, 25/10/2026

O bloqueio reproduzido da GI1.6 foi isolado e corrigido no posicionamento: preferência2,20 m/tolerância0,50 m competiam com elegibilidade1,075 m. Ablação de sugestões preservando avoidance/root motion permitiu atacar; regressão com prefab/perfil/agente reais falhou antes e passou depois. A restrição atua somente em Grunt Normal/SmallMob, deriva o destino/tolerância do alcance atual e preserva geometria/dano/tempos/resistências. FirstSector seed1701 confirmou cinco contatos, recuperação e novos ataques; comparação em movimento também concluída.11/11 focados,87/87 regressões PlayMode e25/25 EditMode passaram nos filtros descritos na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md). Não é suíte global nem aceite humano. Sniper continua com divergência aviso/mira; GI1.8 é proposta de aim-lock por aprovar. Outros melees e pressão em swarms maiores continuam fora da cobertura desta correção restrita.

## Gameplay Intelligence 1.8 — trajetória confiável do Sniper, 25/10/2026

A divergência do Sniper registrada acima foi corrigida na tarefa autorizada GI1.8: aquisição única no início do aviso, mesmos origin/forward para linha e projétil, sem reacquisition na configuração do tiro comprometido. Os demais consumidores mantêm o default anterior. Prefab/perfil reais: regressão90° antes/0° depois; FirstSector reproduzível20,983° e5,406° antes/0° nos dois disparos depois, com esquiva válida e mesmo número/tempo de emissões.11/11 focados,98/98 regressões PlayMode e25/25 EditMode passaram; dados/cenas/GI1.6/GI1.7 preservados. Método, assistência e limites na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md). Resta avaliação humana da pressão e legibilidade do grupo, sem declarar suíte global ou game feel aprovados.


## VS-02A — coleta e preservação de resultados, 21/10/2026

Backend `RunSummary` implementado no ManualSlice, sem UI nova ou mudança mecânica. Recibo final de HP e origem primitiva reaproveitam Actor, Manopla, Protocols e lifecycle de rota; resumo final antes do teardown e último resultado versionado em PlayerPrefs, consultável no Nexus. [Contrato principal](validation/03_TELEMETRIA_E_GAME_FEEL.md#run-analytics--contrato-vs-02a-21102026) e [resultados reais](validation/01_MATRIZ_DE_VALIDACAO.md#vs-02a--run-analytics-e-atribuição-causal-21102026).

20 casos novos aprovados; bateria PlayMode 111/112, teste de burn saturado passou isolado, causa da sensibilidade ainda aberta; 47/47 EditMode nos filtros pertinentes. Três runs assistidas sucessivas na mesma sessão Play retornaram ao Nexus com 618/528/564 HP efetivos reconciliados, sem mistura de sessões ou erro do coletor/Console. PNGs abertos e metadados/eventos correlacionados, sem aprovação subjetiva. UI final, contribuições marginais por boon, partida humana e dívida histórica continuam pendentes.
