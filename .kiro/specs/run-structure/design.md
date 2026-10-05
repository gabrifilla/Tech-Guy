# Design Document

## Overview

Este design especifica a **Fase 4 — Run Structure**, seguindo `requirements.md`, `Docs/Tech-Guy_Gameplay_Rework.md` (§§24–27, 42–43 e 47) e os pareceres de requirements de 04/10/2026. A run deve permitir perseguir recompensas, administrar HP e aceitar risco, produzindo decisões distintas em duas runs com a mesma Manopla.

**Status: design revisado em 04/10/2026 conforme `Tech-Guy_Parecer_Design_Fase4.md`; implementação e validação técnica das tasks 1–14 concluídas; aceitação humana da task 15 pendente, conforme `validation.md`.** Arquitetura preservada; pool manual de Protocols corrigido e fronteira de encerramento do encontro explicitada. A adequação documental não aprova automaticamente tuning ou gameplay. Fases 2–3 aceitas; seus contratos de combate, conteúdo e lifecycle permanecem. Plano incremental elaborado em `tasks.md`; tasks 1–14 concluídas, 15–16 pendentes.

Decisões D1–D8 abaixo fecham topologia, recompensas, Repair, Elite, garantia de Mutation, transições e término. Números novos são parâmetros iniciais ajustáveis por evidência. Não incluir Protocol System novo, geração procedural nova, meta progressão, setores adicionais ou rework de Arco/Lança.

## Architecture

### Autoridade única e modos

Evoluir `ProgressionDirector` como único coordenador de mundo. Introduzir `RunProgressionMode` separado: `ProceduralLegacy = 0`, `ManualSlice = 1`. Default zero preserva cenas existentes até migração explícita. Seleção ocorre antes de gerar/materializar qualquer estágio em Awake; manual não chama StageGenerator, loop de descoberta secreta ou progressão multistage legado. Nenhum segundo MonoBehaviour dirige a run.

Extrair decisões novas em helpers C# locais, sem ampliar o diretor com toda a lógica de rota/recompensa. O modo legado conserva seus caminhos, inclusive fragmentos e ofertas genéricas. `RoomType` não recebe reordenação nem reinterpretacão de valores: tipos existentes são uma projeção geométrica; função e recompensa manuais vivem em metadados próprios.

| Camada proposta | Responsabilidade |
| --- | --- |
| `ManualRunDefinition` (ScriptableObject) | Nós/conexões, grupos exclusivos, encontros, regras de recompensa/Repair e referências; autoria por listas serializáveis |
| `ManualRouteValidator` | Validação bruta e enumeração dos quatro percursos; nenhuma mutação de gameplay |
| `ManualRouteSnapshot` | Cópia validada e imutável da definição no começo da sessão |
| `RunSessionState` | Identidade, fase, nós, ramo escolhido, tickets, aquisições efetivas e resultado; estado local |
| `ManualRoutePolicy` | Disponibilidade, compromisso de ramo, autorização de entrada, conclusão e acesso ao Boss |
| `RunRewardPolicy` | Catálogos tipados, elegibilidade, ofertas congeladas, alternativas e tickets |
| `RunBoons` | Único aplicador dos efeitos/inventário existentes; API contextual nova, API legado preservada |
| `ManualStageEnvironment` | Adapter de geometria fixa, portas, volumes e anchors; não decide progressão |
| `RunRouteUI` / `RepairView` | Apresentação e pedidos ao coordenador; não atribuem HP/recompensa diretamente |

Novos scripts de progressão/helpers em `Assets/_Project/Scripts/Core/Run`; UI em `UI`; definições em `Assets/_Project/ScriptableObjects/Run`. Não mover os scripts antigos de `Stages` para adequar estrutura. Dependências serializadas/injetadas; nenhum singleton/global novo.

### Integração física

Reautorar **FirstSector** para o slice manual mediante modo explícito e referências da definição. Preservar GUID da cena, portal Nexus, câmera, PlayerHUD, prefabs/arquétipos e retorno. Manter a opção procedural no diretor e builder legado; a mudança do default serializado da cena é migração de autoria, não alteração global do gerador. Roots de ambiente manual e procedural são exclusivos: escolher modo antes de ativar geometria, navegação, triggers ou geradores; manter somente o ambiente do modo ativo, sem dois NavMeshes/colisões sobrepostos. A entrada do Nexus resolve o modo conforme família antes de carregar, e a cena valida essa escolha antes de inicializar o diretor.

Usar arenas fixas e corredores autorados, com NavMesh assado no Editor. O adapter projeta cada nó em RoomGraph: Start → Start, Combat/Elite → Combat, Boss → Boss, Repair → Treasure; a projeção não executa a regra de Treasure do modo legado. Conexões manuais são ligadas por anchors/portas explícitos, sem exigir que dois centros sejam adjacentes no grid do gerador. Assim, não alimentar silenciosamente o materializador procedural com um grafo que viola suas suposições geométricas.

Reutilizar `EncounterGates` para bloquear passagens, mas disponibilidade lógica vem da policy manual. Cada conexão tem passagem navegável e bloqueio físico nos anchors. O jogador confirma o destino no mapa; apenas a conexão confirmada abre. Entrada é reconhecida no volume do destino para armar o encontro, sem teleportar por mera confirmação. Revalidar sessão/conexão na entrada; acesso por geometria/dash a nó indisponível não inicia encontro/recompensa. Portas e sólidos devem impedir atravessamento normal; recusa lógica é defesa adicional, não substituto de colisão/NavMesh.

Layout autorado deve prover corredores separados nos dois ramos e reconexão após cada escolha. Nenhuma passagem conecta Repair a C3 ou C5 a E1/E2. Antes de salvar, verificar alcançabilidade dos quatro percursos e ausência de atalhos ao Boss em Unity.

## Components and Interfaces

APIs propostas, não existentes declaradas como implementadas:

| Interface | Contrato |
| --- | --- |
| `TryStartManualRun(definition, player)` | Valida definição/família antes de alterar sessão; cria snapshot, inventário e identidade novos |
| `TryCommitDestination(sessionId, nodeId)` | Preflight completo; confirma conexão/ramo uma vez e emite snapshot de UI |
| `TryEnterNode(sessionId, nodeId)` | Reconhece volume autorizado e transita para encontro/Repair |
| `TryResolveEncounter(sessionId, nodeId)` | Confirma mortes do encontro efetivo; não usa contagem de objetos globais |
| `OfferReward(RunRewardRequest)` | Rota tipada, tickets e número de escolhas; não passa pelo pool genérico/roll Rewrite |
| `Choose(int index)` | Fachada UI preservada; resolve opção do ticket vigente com preflight de elegibilidade |
| `TryUseRepair(sessionId, ticketId)` / `TrySkipRepair(...)` | Aplica/dispensa uma vez; recusa não altera recursos ou ticket |
| `TryRequestExtraction(sessionId)` | Exige Boss resolvido e fase de extração; reclama retorno uma vez |
| `TryEndRun(sessionId, outcome)` | Invalida contribuições tardias, cancela owned state e inicia retorno |

Manter `RunBoons.OfferReward(int)` e `RewardChosen` para consumidores legados. Modo manual usa notificação contextual com ticket; nunca avança nó apenas por `RewardChosen` sem identidade. Não chamar duas APIs de conclusão para o mesmo evento. `RewardTrophy` continua apresentando interação; se foi destruído após reclamar, fechar UI não perde o ticket: objetivo/ponto de interação permite reabrir a seleção pendente.

`ScenePortal` de extração deve aceitar callback/guard opcional do proprietário manual antes de carregar Nexus; portal legado mantém comportamento quando não configurado. Esse adapter é necessário para marcar resultado/teardown antes de scene load, sem segundo listener concorrente de extração.

## Data Models

### Autoria e snapshot

`ManualRunDefinition`: versão, ID da rota, família suportada (`Gauntlet` no slice), nó inicial/final, lista de nós/conexões, grupos exclusivos, encontro/Repair/reward rules. Cada nó: ID inteiro estável, função `Start/Combat/Elite/Repair/Boss`, reward kind `None/Technique/Protocol/Mutation`, quantidade de aquisições, encontro referenciado e anchors de cena vinculados pelo adapter. Não usar dicionários como fonte serializada.

Separar definição SO de bindings da cena: lista serializada `nodeId → arena/entrada/rewardAnchor` e `connectionId → gates/anchors`. Validator confirma ambos antes de iniciar. Conteúdo de encontro usa os arquétipos/prefabs já presentes, sem novos modelos inimigos. Perfil contém posições de spawn válidas, quantidade e referências; não inferir Elite apenas por nome/cor.

Snapshot copia listas e números; edição de Inspector durante run só vale na próxima sessão. IDs de nó não são Unity GUIDs. Sessão tem Guid novo local, independente da seed de ofertas. Dados runtime usam dicionários/sets internos sem se tornarem sistema de save. Seed só seleciona ofertas dentro da categoria; topologia não muda por seed.

`RunRewardTicket`: sessionId, nodeId, ordinal da aquisição, tipo anunciado/efetivo, estado `Pending/Committing/Resolved`, opções e resultado. Chave `(sessionId,nodeId,ordinal)` deduplica troféu/seleção/reabertura. Uma recompensa de Elite com duas aquisições possui ordinais 0 e 1, não duas sessões/recompensas de nó.

`RunSessionState`: phase, currentNode, pendingDestination, estados dos nós (`Locked/Available/Committed/InProgress/RewardPending/Completed/Excluded`), escolhas de grupos, tickets, número de Mutations efetivamente adquiridas e resultado (`None/Dead/Extracted/Aborted`). Não inferir nova aquisição pela presença de registro legacy preexistente sem origem da sessão.

### Validação bruta

Rejeitar IDs duplicados/referências ausentes, conexões inválidas, ciclos de progressão, enum inválido, spawn fora de arena e rota sem início/Boss único. Enumerar caminhos seguindo exclusividade/reconexões; exigir 6–8 combates, Repair/Elite ofertados, caminho sem Elite, Mutation garantida antes do Boss e nenhum caminho que contorne requisito. Verificar runtime eligibility das quatro Mutations com o loadout inicial, não apenas uma lista de IDs.

Floats finitos/não negativos; Repair fraction em [0,1], composição com pelo menos um inimigo em encontros, ordinais/quantidades positivos para rewards. Zero autorado de cura é distinto de ausência, mas uma configuração Repair que não pode restaurar nenhum recurso não é válida para este slice. Falha retorna diagnóstico sem criar portas, equipar clones ou destruir uma sessão válida. `OnValidate` somente diagnostica, sem bake/execução/mutação de outros assets.

## Design Decisions

### D1 — Grafo concreto e compromisso de ramo

```text
S → C1 → C2 → { RP ou C3 } → C4 → { C5 ou E1 → E2 } → C6 → B → X
```

Chaves: S início; RP Repair; C1–C6 combates normais; E1 Elite; E2 combate da rota Elite; B Boss; X extração, não outro encontro/nó com recompensa.

| Nó | Função | Benefício após conclusão | Grupo |
| --- | --- | --- | --- |
| S | Start | Sem recompensa; mapa disponível | — |
| C1 | Combat | Uma Technique | — |
| C2 | Combat | Uma Technique | — |
| RP | Repair | Uso/dispensa de Repair | A: alternativa a C3 |
| C3 | Combat | Uma Technique adicional | A: alternativa a RP |
| C4 | Combat | Uma Mutation garantida | Reconexão A |
| C5 | Combat | Um Protocol existente | B: alternativa a E1/E2 |
| E1 | Elite | Duas aquisições de Technique, sequenciais | B |
| E2 | Combat | Um Protocol existente | Exclusivo após E1 |
| C6 | Combat | Uma Technique | Reconexão B |
| B | Boss | Sem recompensa final de build no MVP | — |

| Percurso | Combates contados | Total |
| --- | --- | --- |
| RP + C5 | C1, C2, C4, C5, C6, B | 6 |
| C3 + C5 | C1, C2, C3, C4, C5, C6, B | 7 |
| RP + E1/E2 | C1, C2, C4, E1, E2, C6, B | 7 |
| C3 + E1/E2 | C1, C2, C3, C4, E1, E2, C6, B | 8 |

Confirmar RP exclui C3, e vice-versa; confirmar C5 exclui E1/E2, confirmar E1 exclui C5. Exclusão dura até fim da sessão. Reconexão não restaura alternativas. Backtracking a salas concluídas é permitido somente onde portas/colisão autorizam, sem farming/Repair novo; não fornece nova saída para ramo excluído. Mapear progressão como DAG, embora passagens físicas escolhidas possam permitir retorno.

Compromisso é aplicado somente após preflight de destino, sessão, estado, gates e loadout; callback de UI rejeitado não exclui nenhum ramo. Confirmação válida assume o custo de oportunidade anunciado mesmo que RP seja dispensado depois. Não existe reroll de rota neste MVP.

### D2 — Catálogos tipados e alternativas

Ofertas manuais: até três opções distintas sorteadas sem reposição, menos de três quando há menos elegíveis; nunca duplicar para preencher cards. Conjunto fica congelado enquanto pendente; reabrir não sorteia outra vez. Validar elegibilidade novamente na escolha; mudança legítima de inventário reconstrói opções elegíveis com diagnóstico, sem debitar/aplicar uma seleção inválida. Sem nova aquisição enquanto houver combate/cast ou outra seleção ativa.

| Categoria | Catálogo manual | Esgotamento |
| --- | --- | --- |
| Technique | Dez Techniques da Fase 3, IDs existentes e ranks 1–3 | Uma opção `bulwark` existente: +40 Armor por aquisição; card **ALTERNATIVA — DEFESA**, não Technique fictícia |
| Mutation | Avanço Demolidor, Campo de Pressão, Epicentro, Asura Cadenciada; elegibilidade existente | Alternativa `bulwark` somente se uma Mutation desta sessão já satisfaz a garantia |
| Protocol | IDs atuais `ignite`, `frost`, `conductor`, `detonation` | `bulwark` como alternativa explicitamente anunciada |
| Repair | Recuperação direta de D3 | Dispensa, não recompensa genérica |
| Boss | None no MVP | Nenhum card/recompensa final pendente |

Protocol reutiliza efeitos atuais sem nova fórmula/recursão. Cada percurso tem exatamente uma aquisição de Protocol em C5 ou E2, com inventário temporário novo. Por isso o pool manual contém apenas `ignite`, `frost`, `conductor` e `detonation`, conforme elegibilidade vigente: cada um deve ser alcançável nessa única oportunidade. Reactor e Resonance permanecem no catálogo/efeitos legados, mas não são candidatos manuais nem conteúdo contado como oferecível nesta topologia; seus pré-requisitos não são removidos e conteúdo de outra sessão não é importado para satisfazê-los. Overflow e novos efeitos também ficam fora desta distribuição. Alterar futuramente essa decisão exigirá oportunidades anteriores suficientes e revisão explícita da distribuição, sem aumentar automaticamente combates. Catálogo manual não oferece `vitality`, `focus` ou transform genérico; esses efeitos/IDs permanecem no pool legado, e reconstrução jamais reaplica suas curas/descontos.

`bulwark` já tem aplicação em RunBoons; adaptá-lo à apresentação tipada sem novo efeito. Não é cura, moeda ou Protocol novo. Se um limite vigente impedir sua aplicação, tratar como configuração inválida: não resolver ticket vazio fingindo benefício. No MVP não introduzir um novo cap que esgote essa alternativa.

Antes de escolher ramo, mapa informa regra de alternativas; ao entrar e emitir ticket, indicar a alternativa efetiva se esgotado. E1 informa **duas aquisições**, inclusive duas alternativas quando necessário; nenhuma vantagem depende da chance de roll Rewrite. API genérica `OfferReward(int)` conserva o roll e composição legados.

### D3 — Repair, HP e demais recursos

Repair inicial: **35% do HP máximo vigente**, limitado ao déficit; um uso, sem moeda, mana ou Asura. Valor autorado `healFraction = 0.35`; UI mostra HP atual/máximo, cura efetiva prevista e **uma utilização**. Dispensa encerra a oportunidade sem cura e sem uso futuro após revisita. HP cheio: botão de uso indisponível com motivo, dispensa disponível. Rejeição por sessão/estado/morte não cura, cobra ou resolve ticket.

HP não é salvo em preferência permanente: é estado da sessão. Manter o mesmo PlayerActor no setor fixo evita reinstanciação entre nós. Se uma operação exige reconstrução de stats/loadout, capturar HP atual, montar stats finais, depois restaurar `min(HP capturado, maxHP final)`, sem chamar handlers de pickup. Não reduzir HP por máximo intermediário antes de reinstalar modificadores. Apenas aquisição nova de Iron Heart no fluxo que a oferece continua curando completamente; reconstrução/importação não representa aquisição nova.

Transição normal manual não cura, restaura mana, concede Asura nem reinicia cooldown. Mana regen/efeitos existentes continuam; tempo de deslocamento permite regen natural. Recompensa aceita mantém a chamada existente de `AbilityHolder.RefreshLoadout`, inclusive reset de timers que ela faz: este é o contrato atual da aquisição, não um bônus por entrada/visita. Dispensa de RP e reabrir UI não chamam RefreshLoadout. Energia/categoria de Asura preservadas em progressão e seleção; não reequipar/configurar a Manopla para atualizar mapa ou abrir portas. Morte/teardown e troca de arma seguem a Fase 3.

Começo de nova sessão: recursos de entrada cheios, cooldowns iniciais prontos, Asura zero/categoria vazia e inventário temporário novo. Aplicar inicialização exatamente uma vez antes de S; não repetí-la ao reativar GameObject durante a sessão. Manter arma escolhida no arsenal; build temporária não atravessa extração/morte.

### D4 — Elite e vantagem observável

E1 propõe uma arena com **um inimigo rank Elite do catálogo atual e quatro comuns existentes**; usar perfil de resistência/abertura já autorado. Não promover um Grunt apenas alterando texto. Composição de referência normal: quatro comuns, ou composição atual equivalente definida no asset; C6 pode usar Heavy existente. Layout/spawns e dificuldade precisam ser validados; contagem não estabelece feeling aprovado.

Vantagem E1: duas escolhas de Technique após um encontro, cada uma podendo ser rank ou opção distinta elegível. Concluir ordinal 0 atualiza inventário antes de construir ordinal 1. Com catálogo esgotado, duas aplicações reais +40 Armor em vez de uma alternativa normal. E2 exige combate adicional e concede o mesmo Protocol de C5. Mapa mostra encontro Elite + encontro adicional e benefício de duas aquisições, sem chamá-lo de Mutation garantida ou raridade nova.

Morte cancela ordinais pendentes; escolhas já feitas não são reembolsadas, mas a build inteira é removida no teardown. Dois ordinais não podem ser confirmados no mesmo callback de tecla; exigir release/reentrada válida da UI, conforme input existente.

### D5 — Garantia de Mutation e cancelamento

C4 está em todo percurso, antes de C5/E1/C6/B. A conclusão de seu combate abre ticket Mutation; só aquisição efetiva resolve a primeira garantia. Oportunidade de seleção não basta. Fechar UI mantém reward pending, saídas seladas e interação para reabrir; não troca por Technique/Armor nem consome ticket. Não oferecer botão de dispensa da primeira Mutation.

Entrada manual exige Manopla com alvos válidos das quatro Mutations e sem conteúdo temporário herdado de sessão anterior. Arco/Lança selecionados no arsenal mantêm acesso ao modo legado; portal deve informar qual modo iniciará, sem trocar a arma silenciosamente. Troca de família pelo arsenal não existe dentro do setor manual; um pedido externo incompatível é recusado antes de trocar enquanto a sessão estiver ativa. Ranks/importação da Manopla preservam elegibilidade/contratos locais.

Se uma Mutation já foi adquirida por ticket válido desta sessão, ela satisfaz a garantia. Alias ShockRing conta somente quando recebido efetivamente como aquisição elegível da sessão, não por registrar um ID preexistente ou importar save desconhecido. Se opções estiverem esgotadas, alternativa pode resolver a oportunidade excedente, sem incrementar contador de Mutations.

Estado inesperado sem opções e sem aquisição: manter Boss bloqueado, registrar erro com sessão/loadout, oferecer **abortar run e retornar ao Nexus** (sem vitória) em vez de UI sem saída. Não fabricar Mutation, apagar slot ou quebrar exclusividade. Definição/loadout inválido deve ser recusado antes de começar; abortar é recuperação de defeito runtime, não um percurso de conclusão válido.

### D6 — Máquina de estados e atomicidade

| Fase | Disponibilidade e próximo estado |
| --- | --- |
| RouteChoice | Sem combate/reward/cast pendente; consulta/confirmar próximo destino |
| Traversal | Conexão confirmada; movimento permitido, mapa informativo; entrada autoriza Encounter ou RepairPending |
| Encounter | Portas seladas, input de combate normal; mapa não confirma destinos |
| RewardPending | Casts/filhos do encontro finalizados antes de interação; UI/interaction possui lock local, saídas seladas |
| RepairPending | Uso/dispensa únicos; sem combate, sem cast ofensivo durante confirmação |
| FinalResolution | Boss resolvido, confirmação de mortes/resultado; nenhuma extração ou recompensa duplicada |
| ExtractionAvailable | Movimento até portal permitido, sem inimigos/reward pendentes; mapa final informativo |
| Returning / Ended | Sessão fechada para contribuições; input/transição guardados e teardown |

Para pedido, validar sessionId/phase/node/ticket, PlayerActor vivo, pausa/hitstop, cast/dash e referências; preparar resultado antes de mutar. Claim de ticket `Committing` impede reentrância. Aplicar efeito pelo RunBoons, registrar aquisição/garantia, resolver ticket, atualizar nó/portas, publicar resultado contextual. Rejeição esperada conserva estado. Se exceção torna commit parcialmente aplicado, não retry cego: diagnosticar, manter progressão bloqueada e oferecer abortar; implementação deve estruturar efeitos para evitar falha depois da aplicação. Não simular rollback de efeitos de combate irreversíveis.

UI apenas emite pedido. Cancelamento visual libera lock próprio, mas mantém pendência. Retorno físico a sala concluída não desloca a fronteira lógica da progressão nem reabre disponibilidade de ramos.

A morte do último inimigo registra uma **conclusão candidata**, sem cleanup dentro desse primeiro callback. O coordenador confirma o encerramento somente após a resolução integral do lote de impacto, seus callbacks síncronos, confirmações de HP/postura/energia e fechamento do grupo de feedback. O fechamento inclui a decisão de hit-stop confirmada, mas não exige esperar duração de hit-stop, animações ou popups cosméticos para sempre. Um impacto descendente que elimina o último inimigo também conclui seu lote antes da transição; sua origem continua regendo elegibilidade de energia.

Depois dessa fronteira, revalidar sessão, jogador vivo, encontro vigente e ausência de inimigos restantes; reclamar encerramento uma única vez. Só então cancelar ações/filhos **futuros** pelos owners existentes e preparar o único ticket de recompensa/transição. Cleanup não invalida HP, ganho letal, quebra, eventos ou contribuição de hit-stop já confirmados; não reinicia energia, categoria ou epoch da Manopla por mudança de sala. Novo encontro não inicia enquanto cleanup ofensivo da sala anterior permanece pendente. Isso exige adapter de cleanup de execução que preserve recurso e inventário; não usar Configure(null) como atalho de transição. Callbacks tardios não reabrem o encontro nem geram segundo ticket.

Pausa/timeScale zero e HitStopRunner ativo rejeitam confirmações mecânicas; não acumulam intenção para executar depois. Animações UI usam unscaled, sem alterar sessão. Locks de UI/restores pertencem à operação; não remover stop/lock de cast/dash de outro proprietário. Release de ataque após UI impede que a seleção fabrique um básico.

### D7 — Boss, acesso e término concorrente

Manual: entrada B exige C6 concluído, todos os tickets do percurso resolvidos e pelo menos uma Mutation adquirida na sessão. Não exigir fragmentos legados; ocultar contador/selo de fragmentos neste modo. Legacy mantém condição de fragmentos e descoberta. Elite nunca é requisito indireto.

Boss usa prefab/composição existentes, sem novo comportamento. Vitória candidata exige todos os inimigos do encontro Boss concluídos e jogador vivo. Eventos de morte apenas registram fatos; coordenador finaliza no fim da resolução/frame após callbacks síncronos, antes de disponibilizar portal. **Morte tem prioridade sobre vitória se o jogador morrer na mesma resolução.** Não encerrar antecipadamente no primeiro callback do Boss e ignorar morte posterior desse lote.

No MVP B não concede recompensa final de build, pois não há combate posterior para usá-la. Para futura autoria com recompensa final, fase FinalResolution/RewardPending deve resolvê-la antes do portal; sem mudar o critério vigente. Não criar economia/meta reward nesta fase.

ExtractionAvailable não significa sessão encerrada: manter inventário/HP e observar morte até extração aceita. Pedido de portal revalida jogador vivo e Boss válido; reclama resultado `Extracted` e estado Returning antes de carregar. Morte antes dessa reclamação vence; depois do commit de extração, callbacks da sessão encerrada são descartados. Portal/carga/teardown com chave de sessão, exatamente uma vez. Player morto jamais extrai por contato residual.

Morte usa atraso de retorno legado, sem novo clock de gameplay. Se load Nexus falha, manter Returning e apresentar retry do carregamento já reclamado, sem aplicar novamente recompensa/resultado. Aborto tem resultado Aborted, limpa owned state e retorna sem simular vitória. Portas/portal temporários destruídos somente pelo proprietário da sessão; preservar a cena autorada.

### D8 — Adaptações locais versus modo legado

| Contrato existente | ManualSlice | ProceduralLegacy |
| --- | --- | --- |
| StageGenerator/RunSeedSource | Topologia snapshot fixa; seed somente para ofertas | Geração/seed e determinismo preservados |
| RoomType/directional gates | Metadados separados e bindings de corredor; nenhum enum reordenado | Projeção/materialização atuais |
| Proximidade começa qualquer sala elegível | Somente destino confirmado; exclusões e fase validadas | Loop atual |
| Oferta genérica após sala | Categoria/ticket; sem roll Rewrite para Mutation | `OfferReward(int)` atual |
| Fragmentos ao escolher reward | Não usados; gate C6 + tickets + Mutation | Condição atual preservada |
| Treasure/Secret | RP possui handler manual; nenhuma descoberta secreta | Comportamento atual |
| Boss marca IsRunComplete antes de portal | Estado de extração distinto de sessão encerrada; resultado no portal | Fachada/semântica atual preservada |
| Cura/pickup/reset de recursos | D3 distingue aquisição, transição e reconstrução | Não alterar efeitos/caminhos fora do slice |

Atualizar documentação legada somente onde necessário para explicar seleção do modo; preservar histórico/evidências, sem declarar specs antigas erradas globalmente. Builder manual não deve ser sobreposto depois pelo builder procedural sem escolha explícita. Nenhuma alteração de código de terceiros.

## Error Handling

Diagnósticos incluem definitionId, sessionId, nodeId/ticket e motivo; não logs por frame. Referência inválida: abortar início sem materialização parcial. Evento antigo/duplicado: recusar sem efeito; erro de configuração significativo é distinto de repetição normal. Card inválido não paga/conclui e oferece opções atuais sem reroll arbitrário. Mutation impossível: D5. Carregamento: D7.

Mapa não esconde condição de erro com falso destino disponível. Não limpar Console para apresentar validação limpa. Valores finitos extremos e quantidades além do orçamento autorado são recusados antes de spawn; não clamping silencioso de grafo/garantia. Bounds, navmesh e prefabs reais requerem inspeção Unity, não apenas validator puro.

## Correctness Properties

| Propriedade | Invariante observável |
| --- | --- |
| P1 — Percursos | Exatamente quatro percursos de conclusão, 6/7/7/8 combates, Boss alcançável sem Elite |
| P2 — Compromisso | Confirmação válida exclui alternativa; rejeição não muda ramo; reconexão/revisita não restaura benefícios |
| P3 — Categoria | Oferta tipada corresponde ao mapa; alternativa identificada e não conta como Mutation/Technique original |
| P4 — Aquisição única | Um commit por ticket/ordinal; reentrância/reabrir/tecla repetida não duplica nem resorteia |
| P5 — HP | Transição preserva HP; reconstrução resolve max final primeiro; cura somente por fonte explícita |
| P6 — Repair | Um uso limitado ao déficit ou dispensa; rejeição conserva ticket e recursos, HP cheio tem saída |
| P7 — Elite | Dois benefícios reais após E1, vantagem mantida no esgotamento; E2 e risco anunciados |
| P8 — Mutation | Todo acesso válido a B possui aquisição nesta sessão; fallback/oferta cancelada não a satisfaz |
| P9 — Estado físico/UI | Gates/mapa/volumes concordam com policy; ação UI não dispara combate nem abre destino indevido |
| P10 — Sessões | Callback de sessão encerrada não afeta nova; inventário temporário não vaza, arsenal persiste |
| P11 — Término | Morte/vitória simultânea decide morte; extração viva ocorre uma vez e só após Boss válido |
| P12 — Autoria | Save/reload preserva dados; inválidos rejeitados antes de afetar sessão; snapshot permanece imutável |
| P13 — Relógios/cleanup | Pausa/hitstop não aceita transição, cosmetic não progride; apenas locks/filhos próprios removidos |
| P14 — Compatibilidade | Modo legado conserva geração/enum/recompensas/fragmentos; combate/Manopla preservam contratos anteriores |
| P15 — Conteúdo alcançável | Cada Protocol do pool manual possui sequência válida de aquisição em C5/E2; Reactor/Resonance não são contados como ofertáveis |
| P16 — Fronteira de encontro | Último lote letal termina confirmações/eventos/grupo de feedback antes do cleanup futuro; um ticket, Asura preservada e sem reabertura tardia |

## Testing Strategy

### Incrementos técnicos

1. Baseline Unity MCP: descobrir capacidades, confirmar projeto/Editor, Console e cena/dirty state. Auditar imports/loaders/builders e estados reais de recursos; registrar conflitos encontrados antes de adaptar.
2. EditMode: validator e quatro percursos; exclusões, enums/números inválidos, Mutation impossível, session/tickets/reentrância, seed de oferta estável, catálogos/fallbacks, HP máximo reconstruído e morte/vitória. Usar testes de comportamento e harness de propriedades existente onde apropriado.
3. MCP autoria: FirstSector/definição/bindings, NavMesh/portas/spawns, save/reload, referências e GUIDs. Não marcar autoria concluída por ler YAML apenas.
4. PlayMode real: Nexus → cada ramo → C4 Mutation → C6 → Boss → portal/Nexus; morte normal/Elite/Boss, fechamento/reabertura de seleção, rejeição de Repair, HP cheio/baixo, dois ordinais, callbacks antigos e duas sessões. Driver pode completar inimigos deterministicamente para testar progressão, identificando esse fato; isso não comprova feeling/dificuldade.
5. Boundary/input: pausa/hitstop, cast ativo, UI click/1–3/release, reentrada, morte durante seleção/retorno, volumes fora de ordem e tentativa de alcançar excluído. Verificar preservação de energia/categoria em reward/cleanup de sala e reset somente no lifecycle previsto.
6. Regressões dirigidas: ProgressionDirector procedural/fragmentos/extração, RoomGraph/RoomType, RunBoons legado, Foco/Manopla HUD e qualquer caller de ScenePortal afetado. Rodar só dependências alteradas; resultados incrementais suficientes não precisam ser repetidos sem risco novo.
7. Alcançabilidade de conteúdo: demonstrar uma sequência válida para cada um dos quatro Protocols manuais nos percursos normal/Elite; verificar exclusão de Reactor/Resonance do pool manual e preservação de seus efeitos/ofertas legados.
8. Fronteira letal: básico/skill, lote multi-alvo e descendente eliminando o último inimigo. Inspecionar ordem real de confirmação de dano/energia/quebra, callbacks, fechamento de feedback/hit-stop, cleanup e ticket único. Verificar energia elegível uma vez, recurso/categoria/epoch preservados, sem hits futuros após cleanup ou reabertura por callback tardio. Incluir morte do jogador na mesma resolução, seguindo D7.

Na auditoria anterior ao código, verificar efeitos reais de RefreshLoadout sobre timers, energia/categoria, snapshots e consumidores; D3 não autoriza reset acidental de Asura. Concretizar seleção Nexus → FirstSector antes de Awake e inspecionar exclusão real de roots/NavMeshes nos dois modos, inclusive família incompatível. Confirmar aplicação/benefício real de `bulwark`. Reconstrução de ofertas por mudança legítima de inventário usa ordem estável de IDs e seed derivada da sessão/ticket/revisão de inventário, com diagnóstico; reabertura sem mudança mantém a mesma revisão/opções e não consome RNG. Retry de scene load mantém resultado já reclamado, sem nova sessão/recompensa.

Após C#/assets: refresh/import e compilação Unity, Console sem limpar, testes relevantes via MCP quando suportados. Falha/desconexão MCP mantém checks pendentes; CLI/diff não substitui Editor. Restaurar estado da cena e sair de PlayMode. Não salvar mudanças runtime sobre assets autorados. Registrar incidentes/retries e diferenciação entre compilação, teste, runtime e aprovação humana.

### Gate humano

Duas runs consecutivas com a mesma Manopla, sem trocar a topologia só para criar diferença cosmética. Registrar HP em A/B, ramo/razão, Techniques/Protocol/Mutation, uso/dispensa Repair, risco percebido Elite e desfecho. Avaliar se HP/build/benefício motivam decisões distintas; se ambas escolhem igual, investigar motivo sem forçar opções para declarar sucesso. Parâmetros 35% Repair/composição Elite são hipóteses sujeitas a esse resultado. Aprovação técnica não aceita gameplay.

## Requirements Coverage Map

| Requisito | Decisão/componentes e checks |
| --- | --- |
| R1 | D1, validator/policy, P1–P2, quatro percursos e navegação real |
| R2 | RouteUI/bindings, D1/D6, P3/P9, input e gates |
| R3 | D2, reward policy/RunBoons, P3–P4/P15, elegibilidade/fallback e catálogo alcançável |
| R4 | D3, P5/P13, reconstrução e fonte de cura |
| R5 | D3/D6, P6, uso/rejeição/dispensa/HP cheio |
| R6 | D4, P7/P14, composição/resistência e dois benefícios |
| R7 | D5, P8, todos os percursos/cancelamento/esgotamento |
| R8 | D6–D7, P10–P11, acesso/resultado/portal/replay |
| R9 | Architecture/Data Models/D6/D8, P10/P12–P14/P16, autoria/migração/compatibilidade e fronteira de cleanup |
| R10 | Testing Strategy, P1–P16, MCP e duas runs humanas |

## Cobertura do parecer de design — 04/10/2026

| Ponto | Ajuste e decisão |
| --- | --- |
| A — Protocols inalcançáveis | D2/P15: solução mínima, quatro IDs manuais; Reactor/Resonance preservados apenas no legado, sem novas oportunidades de Protocol |
| B — Cleanup dentro do callback letal | D6/P16: conclusão candidata, lote e grupo de feedback completos antes de cancelar futuro/preparar ticket |
| Atenções de execução | Testing Strategy: RefreshLoadout, seleção antes de Awake, navegação, ofertas/revisão determinística, retorno físico/lógico, Armor e retry de carregamento |

O parecer é revisão documental da arquitetura. Ajustes acima são contratos para implementação/testes futuros, não correções de defeitos runtime já reproduzidos. Topologia 6/7/7/8, componentes e escopo permanecem; não houve Unity MCP, compilação, teste ou playtest nesta revisão.

## Handoff e próxima etapa

Plano incremental em `tasks.md`: auditoria/baseline → dados/policy → autoria da rota → recompensa/Repair/HP → Elite/Mutation → fronteira de encontro/Boss/lifecycle → UI/integração → checkpoint técnico → gate humano → encerramento. Tasks 1–14 implementadas e validadas incrementalmente via Unity MCP, com evidências e limitações em `validation.md`. Próxima etapa: gate humano da task 15; task 16 depende dessa aceitação. Nenhuma execução MCP, teste, bake ou alteração de gameplay foi realizada para produzir este design; observações de sistemas existentes são inspeção estática. Propostas futuras ficam fora deste plano.
