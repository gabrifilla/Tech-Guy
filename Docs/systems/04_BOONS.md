# Boons — poder que modifica gameplay

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](../gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Proposta atual de transformação do básico — GPR-01

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
