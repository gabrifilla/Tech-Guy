# Boons — poder que modifica gameplay

## Estado vigente — auditoria GPR-01.5, 29/10/2026

**GPR-01.5 concluída como auditoria, com ressalva de estabilidade e aceite físico pendente. STOP após01.5.** O desenvolvedor aceita01.4 como implementada/demonstrada no foco documentado; não aceitou fisicamente as novas camadas fogo/eletricidade ou a tripla. Gameplay01.3 já foi aceito. O histórico199/203 +4/4 isolados de01.4 não foi reclassificado como203/203.

A+B+C foi obtida por tickets reais, sem grants de boons, nos quatro percursos publicados deFirstSector, com assistênciaA+B desligada/restaurada no scope dos testes. Combate/conclusão dos encontros foram automatizados, não humanos. A rota atual usa geração e tem umaMutation e2–7 oportunidadesProtocol na amostra; a antiga conclusão de uma única oportunidade limitava-se à fixture. Nenhum peso/frequência/gate/sala/economia foi alterado.

Modelo determinístico64 seeds×4 caminhos×3 políticas, sorteio/commit de produção:ABC253/256 priorizandoB,252/256 priorizandoC,42/256 escolhendo uniformemente entre cards sorteados. Não são probabilidades universais nem garantia de sobrevivência/diversão. Quatro comparadores reais na câmera normal, com vários inimigos/telegraphs, mostram vento/fogo/eletricidade e feedback causal separado; leitura física continua pendente.

Validação desta atividade:98/98 focados;3/3 EditMode de amostra;8/8 produção (4rotas+4visuais);5/5 correções de ambiente/probe;10/10 checks finais;90/90 EditMode final. Regressão ampliada **203/204**: caso de pausa/recovery falhou emIsCasting após0,4s reais de pausa, passando inalterado no diagnóstico posterior. Causa ainda não confirmada, risco temporal aberto. Não declarar a bateria totalmente verde ouGPR-01 encerrada.

Somente testes/documentação mudaram: diretórios de evidência criados pelos writers; helperAsura exige recovery ainda estacionária, porque a espera móvel também reportaRecovery/progress1 e era admitida antes. Probe reproduziu a premissa incorreta; não prova retrospectivamente toda falha histórica. Nenhum timer/TTL/buffer/cadência/animação ou owner de produção alterado. Medições8/32/64 mantêm dois tornados/pool2 e cap32, com limites de Editor/GC agregado.

Antes de encerrarGPR-01 ou autorizar01.6: diagnosticar pausa/recovery e obter revisão física das camadas/tripla/telegraphs. Auditoria futura dedicada de recompensas/ritmo, layouts, identidade visual de inimigos e bosses é apenas proposta. Singularity, tuning, economia nova, novasMutations/especiais/skills e redesign não foram executados.

[Relatório consolidado e evidências](../decisions/GPR-01.5_RELATORIO.md). Relatórios01.5a/b/c/d preservam comandos, jobs, falhas e limites.

## Histórico anterior — não sobrepõe GPR-01.5


## Estado vigente — GPR-01.4, 29/10/2026

O pedido direto aceita fisicamente01.2c/01.3 e autoriza sequencialmente01.4a–e, incluindo transformação visual obrigatória. **GPR-01.4 implementada, com validação integrada e ressalva legada; STOP após01.4.** GPR-01.5, Singularity, novos especiais, troca modular, tuning, inimigos e salas não foram executados. Todo estado anterior abaixo é histórico onde divergir deste registro.

A=mutation_gauntlet_titan_fists; B=conductor; C=detonation, mesmos IDs/catálogos/tickets. Base/B/C/B+C mantêm execução nativa e não criam tornado. A viaja como vento; A+B acrescenta identidade elétrica e uma oportunidade Conductor; A+C acrescenta fogo e uma oportunidade Detonation em morte confirmada; A+B+C compõe vento, eletricidade e fogo, permitindo morte pelo Conductor derivado autorizar Detonation. Fogo visual não aplica burn/DoT.

Raiz e budget32 compartilhados; uma emissão Conductor e uma Detonation por tornado. Sem Conductor→Conductor, Detonation→Detonation/Conductor ou tornado→tornado no ramo novo. Native B+C preservado, inclusive suas cascatas e dedupe: não aplicar a whitelist nova globalmente. Soco e vento podem conduzir separadamente quando existem alvos elegíveis e capacidade.

Ownership visual no GauntletExecutionSnapshot aceito; ranks mecânicos na raiz aceita. A reserva intersecta ownership/snapshot e inventário, e o visual recebe essa mesma política. Aquisição durante antecipação/voo não altera projétil anterior. Cosmetic Completed conserva identidade aceita, sem root, HP, arco externo ou explosão. Pool/leases limpam fogo e eletricidade em release/clear/swap/disable/run/cena/contexto. Geometria, velocidade3,5m/s, alcance5,5m, TTL2s, raio0,65m e dano0,60× preservados.

Validação real Unity6000.5.10f1/MCP no checkoutD:/Dev/Personal/My_games/Tech-Guy:98/98 focados,12/12 visual/lifecycle,7/7 integração e90/90 EditMode. Regressão ampliada199/203; três falhas de diretório corrigidas na preparação e4/4 no diagnóstico isolado. Uma ocorrência de Asura cadenciada permanece sem causa confirmada; diagnóstico verde não é correção ou bateria ampliada inteiramente verde.368 nomes distintos com resultado aprovado observado (278 PlayMode+90 EditMode), sem ignorados nas seleções finais; não suíte global nem novo aceite humano.

Tickets reais A+B seed0 e A+C seed1; quatro ordens triplas assistidas explicitamente. Rota normal exercitada oferece somente uma oportunidade Protocol: não foi ampliada para fabricar aquisição tripla natural. Capturas Game View, receipts separados, desempenho controlado8/32/64 e todas as tentativas no consolidado e artefatosGPR-01.4. Playtest físico de leitura/feel desta revisão continua pendente.

[Consolidado](../decisions/GPR-01.4_RELATORIO.md) · relatórios separados01.4a/b/c/d/e e evidências novas vinculadas nele.

## Histórico anterior — não sobrepõe o estado GPR-01.4


## Apoio temporário ao playtest — 29/10/2026

A pedido do desenvolvedor, existe um modo de teste no Editor: **Tools > Tech Guy > Testar Tornado + Conductor**. Quando ligado antes de iniciar uma incursão da Manopla, cada sala Combat/Elite recebe uma Mutação + um Protocolo. A primeira recompensa garante Punhos Titânicos enquanto não adquirida; o primeiro Protocolo garante Conductor enquanto ausente. Depois, ofertas compatíveis normais; conteúdo esgotado usa o fallback existente, sem duplicar Mutation. Boss/Repair/Start e Bow/Spear preservados.

Modo capturado no snapshot da run; toggle vale para novas incursões. SessionState, sem gravar PlayerPrefs, saves ou assets; desliga ao fechar o Editor e não é ativável em build. Economia normal fora desse modo permanece vigente. Assistência de teste não demonstra oferta natural/balanceamento. 7/7 PlayMode via MCP, job266ffdae522c4ec08129075f01e33776, incluindo FirstSector e tickets reais; zero ignorados, não suíte global. Evidência em validation/artifacts/gameplay-rework/gpr-01-3/reward-playtest-20261029.

## Estado vigente — Titanic Fists + Conductor, 29/10/2026

A/tornado01.2b aceita fisicamente.01.2c corrige somente a apresentação de terminais legítimos em Traversal de sala concluída, sem raiz/dano/recurso.01.3 permite uma emissão Conductor a partir do primeiro impacto efetivo do tornado, com ownershipA+B, ranks capturados, mesma raiz/parent e quota32. None permanece default; sem Detonation, recursão ou budget novo. Efeitos nativos deB e identidade/catálogo/IDs/recompensas preservados.

[Relatório01.2c](../decisions/GPR-01.2c_RELATORIO.md) / [relatório01.3](../decisions/GPR-01.3_RELATORIO.md). Composição exige revisão humana. Tuning/variedade visual, economia e Singularity estão somente no backlog de design. **STOP após01.3**. Seções anteriores abaixo são históricas onde divergirem.

## Histórico — GPR-01.2b, 28/10/2026

A onda instantânea anterior foi aceita tecnicamente pelo desenvolvedor após teste físico, mas rejeitada como gameplay final. Punhos Titânicos agora emite um **tornado móvel de vento** em cada contato terminal executado, inclusive whiff:5,5m/3,5m/s, dano0,60× pacote aceito não crítico, sweep/LOS e mesma raiz/caps. Sem novo grant de Conductor/Detonation (`ProtocolBasicEffects.None`), sem tuning do kit nativo ou mudança de controles. [Relatório da revisão, evidências e limitações](../decisions/GPR-01.2b_RELATORIO.md). **Aceite físico do tornado pendente; STOP antes de GPR-01.3.**

## Histórico da entrega GPR-01.2 anterior à revisão

### Registro histórico — GPR-01.2,28/10/2026

Punhos Titânicos (`mutation_gauntlet_titan_fists`) está registrada como Mutation Gauntlet/Basic no catálogo executável existente: uma onda frontal após HP nativo confirmado do terminal LMB, com reserva única na mesma raiz e `ProtocolBasicEffects.None`. Caixa1,5× e dano0,60× provisórios; sem novo recurso/postura/CC, grant de Protocol, ação ou tuning do básico. Sobrepeça e onda são reversíveis e locais à run. Oferta por ticket/seed foi exercitada sem grant do boon numa rota de fixture; economia de toda run e aceite físico continuam distintos. [Relatório e limitações](../decisions/GPR-01.2_RELATORIO.md). STOP antes de01.3.



## Histórico da fundação01.1

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](../gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Proposta histórica da fatia — A entregue acima; B/C ainda futuros

[Fatia para revisão](../gameplay/GPR_01_VERTICAL_SLICE.md): A/Punhos Titânicos é conteúdo novo proposto para Basic, B=conductor e C=detonation reutilizam IDs/executores atuais. Sem catálogo habilitado ou asset criado nesta rodada. A hipótese geométrica1,5×/dano0,60× e restrições por linhagem exigem aprovação.

Basic existe como GauntletActionSlot=-1, mas BoonSlot/BoonSlotMask e o grafo ainda configuram quatro identidades históricas. A extensão proposta não converte o especial index3 em modular e não expande Any silenciosamente. Preservar uma Mutation por alvo, dois Augments por habilidade e duas Resonances/run; Mythics continuam gated. Restauração/snapshots/ofertas atuais devem permanecer compatíveis.



**Objetivo:** boons transformam *como* lutar, não apenas o dano. Fonte documental principal deste sistema; os catálogos no projeto são a autoridade dos IDs e dados executáveis. Decisões de design abaixo continuam vigentes; implementação não equivale a aceite de gameplay.

## Taxonomia de design

- `Technique`: suporte ou modificação do estilo/skill segundo sistema existente.
- `Mutation`: substituição ou transformação perceptível de uma habilidade/slot.
- `Augment`: camada adicional que intensifica/especializa comportamento.
- `Protocol`: regra ou interação sistêmica, incluindo efeitos condicionais.
- `Resonance/Duo`: sinergia que requer pré-requisitos e cria efeito composto.
- `Mythic`: transformação rara de alto impacto; **não habilitar** sem passar gates de runtime, visual e métricas.

No código, [`BoonFamily`](../../Assets/_Project/Scripts/Core/Run/BoonDefinition.cs) contém `Mutation`, `Augment`, `Protocol`, `Resonance` e `Mythic`. `Technique` é uma categoria de recompensa em `RunRewardKind` e reúne suportes preservados, Augments e Duos compatíveis; não é uma sexta família serializada. `Duo` é apresentado como `Resonance`. Não criar tipos só para igualar os termos de design.

## Regras críticas

1. **ID estável:** não reutilizar IDs de efeitos antigos; respeitar saves/catálogos e distinção ativo/inativo.
2. **Uma autoridade de custo/cooldown:** boon não deve cobrar mana ou gerar Asura múltiplas vezes via reexecução.
3. **Compatibilidade e scoping:** conferir alvo/slot/origem/capabilities e efeito na skill real; nunca aplicar Ricochet a entidades que não são projéteis elegíveis.
4. **Causalidade:** distinguir native/derived/echo/status tick; respeitar orçamento de evento, origem da raiz causal e proibição de procs recursivos.
5. **Gates:** dados elegíveis + executor implementado + integração Unity + ofertas reais + feedback + persistência. Item no catálogo **não prova** executor pronto.
6. **Fallback por alvo:** se CC não se aplica ao boss, habilidade deve manter consequência útil e autorizada; não inventar conversão em DPS automática.
7. **Leitura:** VFX secundário não pode esconder telegraphs de hostis, efeitos da arma ou jogador.

## Implementação inspecionada em 18/10/2026

Inspeção estática do workspace `gameplayOverhaul`, base Git `3a3f0ee` com alterações locais; não é uma build desse commit puro. Nenhum teste Unity foi executado durante a consolidação. A [matriz de validação](../validation/01_MATRIZ_DE_VALIDACAO.md) concentra resultados históricos e seus limites.

| Responsabilidade | Fonte e comportamento encontrado |
|---|---|
| Autoria e inventário | [`BoonReworkCatalog.cs`](../../Assets/_Project/Scripts/Core/Run/BoonReworkCatalog.cs) e [`BoonReworkCatalog.asset`](../../Assets/_Project/ScriptableObjects/Run/BoonReworkCatalog.asset): 55 IDs distintos, 31 com `_enabledForOffers: 1`, 24 desligados. A contagem do asset não representa o pool elegível de uma arma/run. |
| Pool executável | [`BoonOfferCompatibility.ExecutableDefinitions`](../../Assets/_Project/Scripts/Core/Run/BoonOfferCompatibility.cs) compõe definições preservadas, Eclipse e expansão. O runtime não concede automaticamente todas as entradas do asset de autoria. `Filter` exige arma, alvo real e aquisição válida no grafo. |
| Catálogos por identidade | [`GauntletExpansionCatalog`](../../Assets/_Project/Scripts/Core/Run/GauntletExpansionCatalog.cs), [`EclipseBoonCatalog`](../../Assets/_Project/Scripts/Core/Run/EclipseBoonCatalog.cs), [`BowBoonCatalog`](../../Assets/_Project/Scripts/Core/Run/BowBoonCatalog.cs), [`SpearBoonCatalog`](../../Assets/_Project/Scripts/Core/Run/SpearBoonCatalog.cs). Títulos/descrições não substituem os executores `BreakerGauntletCombat`, `BowBoonCombat` e `SpearBoonCombat`. |
| Compatibilidade | [`BoonCapabilityGraph`](../../Assets/_Project/Scripts/Core/Run/BoonCapabilityGraph.cs) resolve primeiro a Mutation e depois suportes dependentes, por slot; não aceita requisito satisfeito pelo próprio boon ou por outro slot. Valida IDs, arma, rank, pré-requisitos e bloqueios. |
| Limites do grafo | Uma Mutation substituta e até dois Augments por habilidade; até duas Resonances por run. Suportes preservados (`IsRetainedSupport`) não consomem os novos limites de substituição/Augment. **Mythics são recusados explicitamente inclusive na restauração de aquisições.** |
| Aquisição | [`ManualRewardCatalog`](../../Assets/_Project/Scripts/Core/Run/ManualRewardCatalog.cs) gera elegíveis; [`BoonExpansionCatalog`](../../Assets/_Project/Scripts/Core/Run/BoonExpansionCatalog.cs) dá peso 2, em vez de 1, a complementos de Mutation já adquirida no alvo. [`BoonOfferDraw`](../../Assets/_Project/Scripts/Core/Run/BoonOfferDraw.cs) sorteia até três IDs distintos sem reposição; peso não garante oferta nem Duo. Contrato transacional em [run](05_RUN_E_PROGRESSAO.md). |
| Isolamento | [`RunBoons`](../../Assets/_Project/Scripts/Core/RunBoons.cs) clona arma e habilidades na run. [`RunBoons.Manual`](../../Assets/_Project/Scripts/Core/Run/RunBoons.Manual.cs) mantém inventário por família em trocas compatíveis e limpa estado/modificadores/hooks ao encerrar. Registro de aquisição serializável não comprova save/load de uma run inteira. |
| Causalidade | `ProtocolRuntime`, `ProtocolBudget` e identidades de impacto preservam sessão/encontro/raiz/produtor/profundidade. Impactos derivados não devem se passar por acertos nativos nem renovar energia/entidades sem autorização. [Telemetria](../validation/03_TELEMETRIA_E_GAME_FEEL.md) descreve os pontos reais de observação. |

Transformações encontradas: Manopla com Eclipse/Queda e expansão própria; Arco com Vínculo da Caça (Q, selo e prioridade com cone/alcance/linha de visão), Jardim de Minas (E, conjunto substituível) e Flecha do Juízo (R, carga interrompível); Lança com Corte do Horizonte (Q, linha detonável por E nativo), Cerco de Hastes (E, hastes sem bloqueio de navegação) e Órbita do Dragão (R, projeção com retorno antecipável pelo comando de finalizar sequência). Custos e recargas continuam pertencendo ao slot/execução; ecos não autorizam recriar linhas, minas ou conjuntos inteiros.

## Gates e divergências abertas

- **Implementado:** há caminhos executáveis para os lotes acima e filtros reais; catálogo habilitado não prova acessibilidade em toda rota.
- **Validado por testes históricos:** há resultados Unity posteriores à baseline de 17/10, incluindo contratos, PlayMode e round-trip do catálogo. Ver escopo e falhas intermediárias na matriz; a pendência técnica antiga não deve ser repetida integralmente como ainda aberta.
- **Gameplay aprovada:** não há aceite autoral identificado para o lote completo na evidência consultada. Leitura visual, diversão, balanceamento e desempenho continuam exigindo avaliação própria.
- **Propostas:** candidatos desligados e Mythics continuam sob gate; presença no inventário não é autorização para implementar/habilitar.
- **Oferta versus intenção:** a simulação V2 encontrou zero aquisição e zero pré-requisito completo de `duo_bow_eclipse_archer` em 600 runs simuladas do arco. A montagem perceptível de sinergias continua um objetivo de design; o dado registra uma lacuna de oportunidade, sem autorizar mudar pesos, recompensas ou pré-requisitos.

## Teste de uma transformação

- Antes: gravar habilidade base (input, timing, alcance, custo, HUD, dano/controle).
- Depois: gravar a transformação com ângulo/câmera comparáveis.
- Registrar o que mudou nas decisões do jogador, risco e condição de sinergia, não somente no combat log.
- Cobrir adquirido antes/depois de outro boon, incompatibilidades, slot, cancelamento, fim de encontro, save/load, reset run e pool/budget.
- Validar com manopla + regressão mínimo do arco/lança, ou inverso, conforme dependências.

## Propostas futuras — não aprovadas

Preservação de ideias de design, sem compromisso de implementação, habilitação ou valores. Os candidatos declarados abaixo estão desligados em `BoonReworkCatalog.CreateDefinitions`/asset; Choque Térmico permanece ideia de Duo, sem definição executável identificada desse contrato. Dados declarativos não tornam uma transformação pronta.

| Proposta | Distinção a preservar antes de qualquer tarefa |
|---|---|
| Orbe de Ruptura — W universal | Orbe estático com limiar de hits elegíveis para detonação; sem pull. Não confundir com Singularidade. |
| Contra-Impacto — E manopla | Postura de counter contra golpe bloqueável, com resposta condicionada; golpes não bloqueáveis mantêm o contrato. |
| Passagem Fantasma — Q arco | Recuo/reposicionamento + salva de origem ilusória, sem ator de proxy/aggro nem clones recursivos. A declaração atual inclui capability `Summon`; resolver esse contrato antes de executar, sem presumir que a capability autoriza criar outro ator. |
| Âncora do Dragão — Q lança | Projétil e segundo pressionamento distinto para avanço, respeitando sólidos; recolhimento é suporte futuro. |
| Choque Térmico — ideia de Duo | Frost consumido por Ignite, consumo idempotente e fallback útil autorizado contra boss. **Há um efeito `ThermalShock` em `ProtocolRuntime.Damage` hoje; isso não comprova esse Duo nem autoriza trocar a interação existente por consumo de Frost.** |
| Asura Espelhado / R Espelhado | Avatar repete somente hits confirmados elegíveis, sem replicar custo, energia ou outro avatar. |
| Tempestade de Caça — R arco | Arrastar/soltar a mira define uma linha de salvas, alterando geometria e input. |
| Duas Luas — R lança | Alternância de alcance/varredura que modifica entrada e espaçamento, sem haste permanente. |
| Singularidade Errante — W universal, opcional | Zona móvel com atração apenas em perfis autorizados e fallback contra resistentes; continua candidata, não requisito para concluir o lote atual. |

Qualquer detalhamento novo permanece `PROPOSTO` até decisão explícita. As regras de causalidade, economia, compatibilidade, resistência e leitura acima também valem para essas ideias.

## Não objetivos

Não adicionar infinidade de tiers ou novos sistemas de procs em paralelo. Não habilitar todo conteúdo de uma tabela apenas porque está especificado. Não refazer todos os boons para resolver um bug individual.
