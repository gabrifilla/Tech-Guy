# Validation — Sistema de Protocolos

## Status incremental

Tasks **1–12 concluídas**. Task 13 em implementação/validação; checkpoint, gate humano e encerramento (14–16) pendentes. Catálogo, inventário, aquisição, orçamento, produtores e motor dos sete efeitos implementados; cobertura incremental registrada abaixo. Performance e gameplay da Fase 5 não foram aprovados.

## Task 1 — auditoria e baseline

Contratos/consumidores/migrações registrados em `contract-audit.md`. Unity MCP conectado ao projeto Tech-Guy correto, Unity 6000.5.10f1, NexusLobby limpa. Sem falha de compilação; quatro avisos preexistentes de bindings em AbilityHolder, sem limpar Console.

Baseline PlayMode dirigido job `9181badced76444aae81298cfbb9b35d`: ThermalShockBurstMagnitudeTests e DetonationClassificationTests, **4/4 passed**, zero falhas/skips. Primeira tentativa perdeu sessão e não produziu resultado; repetição registrada. Baseline aprova somente comportamento anterior às alterações.

## Task 2.1 — autoria validada

Entregues `ProtocolCatalogDefinition`, `ProtocolCatalogValidator` e `ProtocolCatalogSnapshot`, com sete IDs, pré-requisitos, cap 3, coeficientes D2, parâmetros D6 e relógio escalado. Dados brutos inválidos são rejeitados antes de qualquer snapshot; dados de runtime são cópias isoladas. As fachadas de combate existentes não foram alteradas neste incremento.

Asset criado pelo Editor MCP: `Assets/_Project/ScriptableObjects/Protocols/DefaultProtocolCatalog.asset`, GUID `9754c0d8aad625149bb72e2d08bae82c`. Metadados de scripts/folders gerados pelo Unity. Inspector via SerializedObject confirmou sete entradas e root cap 32; salvar, descarregar e reimportar/recarregar preservou esses valores e Ignite DPS/rank 6. Isso verifica autoria, não binding de cena/runtime que pertence à próxima task.

Importação/compilação MCP concluídas, `EditorUtility.scriptCompilationFailed = false`; Console final sem erros. Uma solicitação de teste durante compilação retornou busy, sem resultado; execução realizada após disponibilidade. Não foi executada suíte completa.

| Job MCP EditMode | Seleção | Resultado |
| --- | --- | --- |
| `971ac08f1d764b1a88296003b38a1804` | ProtocolCatalogTests: defaults/snapshot isolado, NaN/±infinito/negativo, identidade/dependência, rank e coerência de caps/chance | 9/9 passed, 0 failed/skipped |
| `48b52a0ed3b7471c8ba98be79051a047` | Casos adicionados: entradas ausentes/duplicadas e relógio/intervalo inválidos | 2/2 passed, 0 failed/skipped |

São **11 casos distintos**, sem alegar reteste integral após acrescentar os dois últimos. Os arquivos de runtime não mudaram entre os jobs. Cena final NexusLobby limpa, fora de Play Mode; diff da NexusLobby vazio. Revisão dos novos scripts/testes não encontrou whitespace inválido; arquivos e meta relacionados presentes. Alterações preexistentes de outras specs/cenas/assets preservadas.

## Próxima ação

Integrar orçamento/runtime aos produtores (tasks 5.2/6), status e efeitos. Performance e aceitação humana continuam nas tasks próprias; não usar testes dos helpers como comprovação dessas etapas.

## Tasks 2.2–4 e 5.1 — continuação

ProtocolInventory local foi integrado a RunBoons: sessão/revisão, dependências/cap, bloqueio reentrante, fault e cleanup. O manual injeta asset explícito pela definição. Cenas/consumidores legados sem campo usam snapshot default validado e local, criado/destruído sem loader ou singleton; adaptação explícita para conservar essas cenas, não evidência de binding de asset em todos os legados.

RewardSteps conserva campos legados para fallback/topologia histórica e fornece categoria por ordinal e total de tickets. C1/C2/C6 agora Technique→Protocol; E2 dois Protocols individuais. Mutation/Repair/E1 preservados. Seleção D1 e chave de inventário usam ranks reais. Builder e UI de rota foram adaptados ao contrato, sem concluir ainda o gate completo de UI/feedback.

MCP salvou/descarregou/reimportou definição publicada: validação verdadeira, quatro paths com Protocol counts `[4,5,4,5]`, catálogo explícito e GUID preservado. Nenhuma geometria/cena/NavMesh foi alterada nessa migração.

Job `752753f5481a43798e77d27bd67608c4`: **24/24 EditMode**, zero falhas/skips, seleção ProtocolInventoryTests/ProtocolRewardSelectionTests/ProtocolMixedTicketTests/ManualRoutePolicyTests. Cobre pré-requisitos, cap, revisão, rejeição/reentrada/fault, sessão nova, sequências declaradas, ordinais mistos e regressão de policy.

Job `f8e62d17de234a2fa5a7389da0eb029e`: **2/2 PlayMode**, zero falhas/skips, ManualRunPlayerFeedbackTests. Fluxo real FirstSector obtém Technique+Protocol nas duas primeiras salas, respeita release entre tickets, mantém recompensa automática após impacto nativo letal e movimento por clique. Assert antigo de uma aquisição/sala foi migrado explicitamente para dois tickets conforme D1; assert de conclusão, feedback e movimento foi conservado.

Contexto/orçamento local: snapshots de ranks, produtor/execução, handles alvo/geração, seal/reservas, bounded queue/visitas/mortes, frame cap compartilhado e relógio escalado. Job inicial `986067defa3d41859e574fb8b1465537` não iniciou por timeout, sem assertions executadas. Repetição `52d9dde691584b56ab7c0786b1f15ef6`: **4/4 EditMode** ProtocolBudgetTests, zero falhas/skips. Runtime, métricas/queries/status/VFX e integração de todos os caps permanecem na 5.2 e tasks dependentes.

Compilações/imports MCP executados; sem erro de C# observado. Console durante a continuação apresentou erro isolado `[Package Manager Window] Operation cancelled`, além de warnings preexistentes de APIs obsoletas em testes; não foi limpo. Não atribuir cancelamento do Package Manager a compilação dos Protocolos nem esconder esse achado. Nenhuma medição de performance/gameplay executada neste trecho.

## Continuação — adaptador de dano e fronteira inicial

Implementados ProtocolRuntime (local ao jogador), snapshots antes do dano, fila limitada de impactos/emissões, aplicações de elementos por proprietário, ticks capturando ranks da aplicação, e escopos para impactos/áreas. PlayerActor usa o receipt real e mantém dano nativo quando a raiz derivada é recusada. GauntletContextRegistry inicia/fecha produtores e valida a reserva específica de cada filho. A amplificação de filhos ocorre antes do HP; impactos secundários não passam pelo canal nativo de energia/OnHit. Legacy PlayerOnHitEffects permanece para consumidores isolados sem ProtocolRuntime; contribuições próprias coexistem com status legados, sem removê-los no cleanup.

A resolução manual aguarda o lote confirmado de Protocolos e fecha aplicações/raízes ao concluir a sala. Ticks futuros não iniciam durante IsEncounterClosing. Frost bloqueia ataque na AI via estado próprio e restaura somente sua contribuição de velocidade; os contratos completos de resistência/proteção ainda precisam de testes dirigidos.

Job EditMode `37e1472024104c92b615927522eb792b`: **5/5 ProtocolBudgetTests**, incluindo reserva específica, sessão encerrada e ranks capturados pelo tick. Job PlayMode inicial `426efdb6606747eb89f848bb73ac59a8`: timeout de inicialização, nenhum teste executado. Repetição `465189e5337340e185ce0af94f68120c`: **3/3 ProtocolRuntimeDamageTests**. Após conectar PlayerActor: `fe912eb88e6d4404ac6ecfc024561344`, **5/5**, testes de dano e ManualRunPlayerFeedbackTests. Após ajustes de fronteira/Frost e dois casos adicionais: `5fb68848bfbe437a973566adb6345cac`, **7/7**, zero falhas/skips; cobre choque duplo sem duplicar dano, status anteriores à morte e Detonation por HP confirmado em vez de overkill.

A importação inicial do novo arquivo Scope foi incompleta com scope scripts e apresentou três erros de membros ausentes. Refresh all importou o arquivo e suas metas; recompilação posterior sem erros, testes acima executados nessa versão. Não foi limpa a Console. O diff global também contém whitespace preexistente em dois m_Name da FirstSector e mudanças de fases anteriores, preservadas.

**Ainda não concluir tasks 5.2/6–10:** faltam identidade compartilhada dos produtores de projéteis/salvas, ecos e zonas legadas; cobertura integral de caps, resistência, pausa/hitstop, pooling, cancelamento e fronteira; classificação/apresentação de Shatter e Combustion. O motor inicial passou os casos citados, não a aceitação completa dos sete efeitos. UI/VFX, integração publicada abrangente, performance e duas runs humanas também permanecem pendentes. Próxima ação: adaptar os produtores legados à mesma raiz/reservas e ampliar somente os testes pertinentes.

## Continuação — produtores, elementos, cascatas e apresentação

Implementados escopos de execução para ArsenalCombat, salvas/projéteis, ecos e filhos; ranks são capturados no lançamento, reservas persistem entre frames e não se renovam ao serem liberadas. IDs positivos são do Gauntlet; IDs negativos de produtores locais/legados, evitando colisão. Cada ID reservado só pode ser admitido uma vez por root. Tokens carregam revisão de lifecycle para impedir dano atrasado depois de fechamento/rebind, inclusive na mesma sessão/encontro numérico.

ProtocolElementStatus mantém contribuições owned, com Burn/Reactor, Frost por perfil, proteção de retomada, first tick, relógio único e cleanup separado do legado. Frost usa rank base, não rank temporariamente reduzido por vulnerabilidade. Snapshots guardam posição, HP, status, equipe e geração anteriores ao dano. Replays do receipt são recusados; morte por Overflow/choque/tick usa seu próprio receipt letal. Membership transitório é injetado pelo spawner manual para filtrar outros encontros.

Classificação de hooks: com ProtocolRuntime, OnHit/OnCrit/OnKill nativos só recebem Direct. Filhos e efeitos publicam OnProtocolImpact com sessão/root/geração/produtor/profundidade/dano/morte. Callbacks derivados herdam raiz e profundidade em vez de abrir nova execução. O canal legado sem runtime permanece. Energia continua pela política da Fase 3, incluindo a exceção causal do Guard Impact. A correção de Conductor em acerto letal segue D4/D5: mortes elegíveis também podem emitir cadeia; visitas de cadeia/explosão são combinadas.

Feedback local reutilizável: cadeia em zigue-zague, explosão/Combustion em anel, Shatter com 3–6 traços, Overflow em losango e choque em anel distinto. Sem colliders, leases ou callbacks de dano. Caps 24 ativos/16 novos por frame/TTL 0,6s escalado; clear idempotente. Cartões e inventário mostram números, cap/rank, dependências e a ausência de consumo no Overflow. Cache antigo sobrescrevia a descrição de Frost/Ignite e conservava dados do primeiro rank; foi corrigido para usar a apresentação atual dos Protocolos.

| Job MCP | Seleção | Resultado |
| --- | --- | --- |
| `05ce6b868e9c41c5999d703fa5a9affa` | Três regressões causais de energia, área Bow/Spear e cancelamento da Fase 3 | 3/3 passed |
| `0cd4aa2125374441a1cbb5c8e780b78f` | Dano Protocol + projéteis/relógios | 21/21 passed |
| `e5aa0d08438b4d13ba75f05aec1c7adb` | Dano + projéteis + primeiras salas/movimento | 23/23 passed |
| `e91c8efae1e34e7d8388a682f9ca41ca` | Dano + projéteis após clock/profile/query adjustments | 21/21 passed |
| `84f0b0599c0e479d99047907168f2f6a` | Catalog/Budget incluindo caps acima do contrato, namespaces, replay de reservas e cancelamento de batch | 24/24 passed |
| `f39b641d07674d03a984affc480da281` | Dano, projéteis e pool visual; paredes/equipe/colliders, morte Conductor, Shatter e profundidade 4 | 26/26 passed |
| `6761a332f9df456b8ca9df1c2a862045` | Cinco composições publicadas + projéteis/clock/hitstop/cleanup | 7/7 passed |

Os jobs não são casos adicionais a cada reteste. Resultados acima têm zero falhas/skips. O teste publicado adquire pelos tickets/RunBoons.Choose (sem injetar inventário), atravessa FirstSector, Mutation obrigatória, C1/C2 e Boss, comprova quatro aquisições normais/cinco Elite, segundo rank e sete IDs. Inimigos são fixture declarada: AI desativada, HP/reposicionamento controlados e jogador imune; isso não é run humana. Casos do pool verificam limites e reuso, não legibilidade percebida.

As primeiras tentativas publicadas (`10725310d2654b5380e8b9e82f05207d`, `5195a50b097f4a5ca737e03ac3609308`) falharam porque a fixture exigia propagação Conductor na sala de Boss sem vizinho; a verificação agora exige vizinhos para Conductor e continua exigindo Overflow no próprio alvo. Não foi alterado o dano para satisfazer essa expectativa incorreta. Um novo teste faltou importar UnityEngine.AI, produzindo CS0246; import corrigido. MCP ficou temporariamente sem resposta durante reload/compilação; chamadas sem resultado e timeout não contam como testes. Reconexão confirmou NexusLobby limpa, fora de Play Mode e scriptCompilationFailed=false.

Hardware observado via MCP: Ryzen 5 5600X (12 threads), 49065 MB RAM, RTX 3060 Ti; Unity 6000.5.10f1, backend Mono. Ainda não há medição Development nem aprovação das metas D8. CPU marker e contadores bounded foram acrescentados; validação dessas últimas alterações segue pendente até o job correspondente. Tasks não marcadas continuam exigindo sua cobertura, inspeção visual/publicada, performance e gate humano.

### Regressões e saturação em andamento

Job `329f013d92154ef3a22b0a9d23ef8d2b`: 6 testes causais, 4 passed/2 failed. Falhas dos testes de Echo/PressureField eram observação pelo canal OnHit antigo; conforme migração D4, agora observam filhos via OnProtocolImpact e preservam todas as assertions de contagem/dano/energia/postura/identidade/cancelamento/mana. Reteste `1d8f26a7e9434c4f99bdeb1663d9047b`: 3/3 passed, incluindo 33 receipts nativos e apenas 32 bursts Protocol, dano nativo preservado integralmente.

Job `d780180ff37f45c98f95519bc6fa5ac3`: 33 casos, 32 passed/1 failed. A saturação de 256 Burns revelou starvation: o cursor avançava em frames sem ticks, sobrepondo grupos servidos. Corrigido para avançar somente após serviço efetivo; reteste dirigido registrado a seguir. A expectativa de 256 aplicações atendidas foi mantida. Não considerar o grupo integral aprovado até o reteste da correção.

A revisão está preparando medição separada de GC próprio (GetAllocatedBytesForCurrentThread por escopo externo) e CPU (`TechGuy.Protocol.CPU`), sem contar duas vezes scopes aninhados. Hardware observado é superior à GPU/RAM de referência; metas absolutas não constituem prova no hardware inferior. O harness precisará registrar seed, build/backend, resolução, VFX, aquisições reais rank 1, injeção somente no stress e samples, sem substituir os tempos D8 por smoke.


Reteste `dec8259ed3ad40098e1b15158ff6d277`: 1/1 passed (256 Burns servidos round-robin e morte por tick detonando). Job `9c2644aa89124c33b86ab94f24bc47ac`: 33/33 passed, 0 failed/skipped, 113,987s, ProtocolRuntimeDamageTests + regressões isoladas DetonationReactorBurnScalingTests, DetonationIsolatedBurnTickTests, ThermalShockStatusPreservationTests, DetonationClassificationTests e GlobalCascadeBoundsTests. Buffer saturado de 129 vítimas ficou limitado a 32 impactos da raiz, sem raízes retidas; timers de bursts conservados e Resonance em ambos os choques verificadas em ranks 1–3.

Tasks 5.2, 6.1, 7.1 e 8 marcadas após esses checks. Tasks de Frost, propagação, fronteira, apresentação e integração mantêm verificações específicas restantes; harness/performance/aceite humano não foram confundidos com os testes acima.

### Continuação — Frost e propagação por rank (2026-10-04)

Unity MCP conectado a `Tech-Guy@fccafb2b47cd7671`, Unity 6000.5.10f1. Job `92578e4d14634466b1445818aa902244`: **42/42 PlayMode**, zero falhas/skips, 11,689s, grupos `ProtocolRuntimeDamageTests` e `ProtocolProjectileContextTests`. Inclui expiração de Frost/restauração da velocidade, política dos cinco perfis, pause/hitstop, proprietário externo, contexto reservado, saturação/round-robin de Burn, fórmulas de Conductor/Detonation ranks 1–3 e morte no primeiro choque. Tasks 7 e 9 concluídas com essa evidência e as regressões incrementais anteriores; performance e legibilidade humana permanecem separadas.

Harness Editor smoke v3 concluiu os três cenários (baseline, composição publicada rank 1, stress rank 3), com raw CSV e JSON em `Logs/ProtocolBenchmarks/editor-smoke-v3-20261004`. Console MCP: zero erros após a execução; Play Mode encerrado. As tentativas v1/v2 falharam durante aquisição: o predicate avançava o índice a cada comparação de cartão. Corrigido para avançar somente após `Choose` confirmado. Smoke usa warmup/coleta curtos e **não aprova D8**. Development Windows64 solicitado via MCP, job `build-b58acb0138`; resultado ainda pendente nesta anotação.

### Fronteira, autoria D1 e instrumentação — continuação

Job `a4660fd92e384f7088d8087d71f577c0`: 27/27 EditMode, 1,666s, validação da definição manual e tickets mistos. Catálogo publicado exige os tickets adicionais C1/C2/C6 e E2 duplo; dados legados sem catálogo preservam contrato anterior.

Job `a9b885ed052a49bd8fd33fb4c4872357`: 7 casos, 5 pass/2 fail. Teste de clique mantido perdeu o estado de dispositivo no Editor sem foco (configuração observada: ResetAndDisableNonBackgroundDevices/PointersAndKeyboardsRespectGameViewFocus); fixture passou a salvar/restaurar configurações e manter input sintético durante a execução. Sem alteração da lógica de gameplay para acomodar o teste. A expectativa de BasicHit após morte do jogador era inválida: a morte encerra a run e elimina notificações nativas posteriores. Corrigida a expectativa específica dessa fixture; crédito nativo de morte continua zero para Overflow. Job `23f62390219944bdbfeefdc85eddc9b4`: 4/4 cenários de fronteira, 4,863s, incluindo Overflow/tick/cascata e prioridade da morte concorrente. Callback reservado é invalidado e estado owned encerra antes do ticket. Reteste de input e nova sessão pendente.

Jobs `e3d578234aa44909b0b03bb695ab9b10` e `b09e28aef06a4cca80c82eb29d1eca26` perderam atualização após reload: nenhum resultado utilizável. Estado real Test Framework inativo confirmado via MCP antes de liberar job órfão. Job `d514ef00af1b439d98a141b75cde6a09` foi cancelado após erro de compilação da nova fixture (imunidade exige UnityEngine.Object, corrigida para director); não contar como sucesso.

Development Windows64 Mono: build MCP `build-b58acb0138`, succeeded, 71,614s, 196,24MB, zero erros/2 warnings. Player smoke v1 concluiu 3 cenários e encerrou automaticamente. Dados em `Logs/ProtocolBenchmarks/player-smoke-v1-20261004`; warnings de AbilityHolder/bindings já observados. **GC próprio inválido nesse backend:** probe MCP criou byte[8192] e GetAllocatedBytesForCurrentThread retornou zero. Zeros de GC dos smokes v1/v3 não são evidência de ausência de alocação. Harness atualizado para contador Unity Memory/GC Allocated In Frame (capacidade 1, soma por frame): GC total do frame como limite superior conservador, explicitamente distinto de custo isolado de Protocol. CPU/memória continuam via ProfilerRecorder. Medição D8 completa e atribuição de regressões ainda pendentes.
Reteste `9fed7f3e6e0d45579c074b9396592d9b`: 13/13 PlayMode, 24,837s, ManualPublishedRunPlayModeTests + ProtocolProjectileContextTests. Quatro percursos publicados até Boss/extração, Bow legado, mapa/clique mantido, invalidação na sessão seguinte, status/relógios/projéteis/cleanup. Configurações de input da fixture restauradas; nenhum contrato de release foi alterado. Task 10 concluída, combinando fronteira e cleanup com os jobs anteriores. Contadores próprios de GC inválidos removidos do runtime; nenhuma leitura zero restante apresentada como medida.

### UI, persistência e feedback (2026-10-05)

MCP runtime: mapa C1 mostra `1 Technique + 1 Protocol`; ticket real C1 após Technique mostra `escolha 2/2 · Protocol #1`, ranks e números de Frost/Ignite. Capturas inspecionadas em `Logs/ProtocolValidation/phase5-map.png`, `phase5-protocol-cards.png` (1047×589) e `phase5-protocol-cards-max.png` (1547×870); sem corte dos cartões observados. Game View maximizada temporariamente, depois restaurada. Nenhuma aceitação humana inferida dessas imagens.

Prefab temporário authoring: serializar `_combatAffiliation=Ally`, salvar, importar, LoadPrefabContents; leitura `Affiliation=Ally` confirmada via MCP. Fixture recém-criada e .meta removidas via AssetDatabase. Referências do catálogo/rota publicadas já verificadas nos incrementos anteriores. Cena NexusLobby restaurada, fora de Play.

Captura `phase5-feedback-shapes.png` revelou todos os acentos brancos: URP Unlit ignora a cor de vértice do LineRenderer. Correção usa MaterialPropertyBlock `_BaseColor` por stroke, mantendo material compartilhado; tinge somente strokes ativos e conserva translucência. Combustion tem dois anéis; choque térmico tem anel/cruz, Shatter estilhaços, Overflow losango e Conductor zigue-zague. Captura corrigida `phase5-feedback-tinted.png` inspecionada; tint/forma distintos, porém fixture cosmética no Nexus não comprova leitura em combate. Job `f71c649b629947a6b04220d5f8234f64`: 2/2 PlayMode, 0,161s, caps/pause/expiry/reuso e uniform de material/glifos. Task 11 técnica concluída; telegraphs/gameplay continuam gate 15.

Editor smoke v4 com GC Unity: 3 cenários concluídos, recorder GC válido e valores não-zero. Raw em `Logs/ProtocolBenchmarks/editor-smoke-v4-20261005`. GC inclui Editor/fixture e não aprova custo isolado nem D8. Nenhuma medição longa executada até esse registro. ProjectAuditor gerou automaticamente segundo documento de settings durante build; removido apenas esse bloco novo, preservando configuração anterior.
Job `0b8323218e88402b80a11665e3f6eb0a`: 5/5 PlayMode, 22,669s, cinco composições adquiridas por tickets reais, sete IDs ativados, quatro caminhos, input e modos publicados Bow/Spear (unlock de Spear da fixture restaurado). Job `55cb720bf43649439da5b4104c5ecfcc`: 7/7 regressões dirigidas Fase 3, 22,678s, energia/re-equip, Echo, PressureField, Guard, Hungry/Fist, Bow/Spear e confirmação/cancelamento. Task 12 concluída; injeção do harness continua somente fixture de stress/fronteira, sem substituir aquisição publicada. Task 13 ainda pendente; builds/smokes não representam cumprimento D8.
Revisão encontrou reentrada adicional: callback OnBurn recicla alvo e Frost ainda incrementa contador da aplicação removida. Job `816319b1a0f444d08721a7e736422f39`: 1 teste, failed, esperado zero aplicações Chill/obtido 1. Corrigidos guards após hooks tipados/elementos e antes de Frost; geração, root e registro owned revalidados. Task 10.2 reaberta até o reteste. Matriz Development full-v1 está executando o build anterior a esses guards; resultados representam essa versão. Repetir cenários afetados na versão final e registrar concorrência de Editor durante diagnóstico.
Reteste `8a5de0795cdb45ee9fee9569c7e283b9`: 48/48 PlayMode, 16,560s, dano/status, contexto de projéteis e fronteira; guard de reentrada aprovado sem alterar expectativas. Task 10.2 concluída novamente. Durante revisão da instrumentação identificou-se CPU de construção/liberação de scopes fora do marker. Coleta full-v1 interrompida de forma graciosa via WM_CLOSE exclusivamente no processo de benchmark (31348), preservando restauração de preferências; amostras parciais mantidas, sem aprovar D8. Marker ampliado a produtores, factories, amplificação e liberação de scopes; sem mudança de fórmula/limite. Recompilar/reexecutar performance nessa versão.
Instrumentação final recompilando: revision `20261005-reentry-scope-profiling` e Application.buildGUID serão registrados para identificar a versão medida. Script `analyze-performance.ps1` validado nos 3 cenários smoke e 7 baselines parciais; confere número de amostras, pares baseline, percentis, GC total e seis médias de memória. Nenhum approval automático inferido. Os baselines parciais de 8 alvos tiveram 3600 amostras por coleta de 60s; não substituem matriz completa e não têm cobertura final de CPU. MCP Console atual sem erros; diff/referências/meta revistos incrementalmente; TimeManager já modificado antes desta continuação permanece preservado.
