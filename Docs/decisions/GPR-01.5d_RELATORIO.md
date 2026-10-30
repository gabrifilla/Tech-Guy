# GPR-01.5d — estabilidade, regressões e desempenho

29/10/2026 · **AUDITORIA CONCLUÍDA COM RESSALVA DE ESTABILIDADE.** Fixtures de diretório corrigidas e vulnerabilidade do helperAsura demonstrada/corrigida. A seleção ampliada final teve **203/204**, com uma ocorrência de pausa/recovery ainda sem causa confirmada. Não declarar regressão inteiramente verde nem encerramento técnico irrestrito deGPR-01.

## Separação das ocorrências históricas

GPR-01.4 continua historicamente199/203 e depois4/4 isolados, nunca203/203. As três falhas de gravação aconteciam após asserts de gameplay, porque o destino SessionState apontava para diretório ausente. Foi criado GameplayTestEvidence.Directory: resolve o destino e cria-o antes da gravação de combo-observation.json, integration-movement.json e wall-comparison.json. Alterações restritas aos writers de DirectBasicInputPlayModeTests.cs e DirectMovementIntegrationPlayModeTests.cs. Sem mudar asserts, input ou runtime.

Teste da correção: destino novo `gpr-01-5/d/fresh-fixture-path` foi consultado pelo MCP e não existia. Os três writers criaram-no sem preparação externa. Junto com a Asura/probe: **5/5**, job **06fe1522a3f5409c98c1ab09b8c75586**,4,7162157s, [resultado](../validation/artifacts/gameplay-rework/gpr-01-5/d-fixture-correction.json). Resultado válido, sem depender do mkdir manual que mascarou o problema na entrega anterior.

## Asura: evidência reproduzível e alcance da correção

Método original CadencedRContinuationDiscardsRecoveryMovementWithoutPublishingAnotherAbilityUse. Em01.4 falhou BasicAttackR1MovementFluidityTests.cs:258, HasPendingGroundMovement=False. [Histórico extraído](../validation/artifacts/gameplay-rework/gpr-01-5/asura-history.json) também encontra uma falha do mesmo método em c01-3b/correcao/combat-regression.json, antes deTitanic01.4, além de passes anteriores. Portanto não é correto atribuir a introdução ao novo tornado só pela primeira revisão recente.

Inspeção dos owners: a espera cadenciada conserva _elapsed no fim do pulso e continua reportando Recovery/progress1. AbilityHolder.MovementAllowedWhileCasting=True nesse estado. TryRequestGroundMovement aceita movimento imediato e descarta o buffer. O helper antigo verificava apenas CanQueueRecoveryMovement e progress>=0,85; admitia justamente essa espera e então exigia movimento pendente, uma premissa incompatível com o estado.

Novo teste GPR015AsuraMovableWaitMatchesHistoricalRecoveryFailureCondition espera a espera real do executor e demonstra: `waiting=True; progress=1; oldHelperAdmits=True; movementAllowed=True; pending=False`. Também continua a mesma execução, sem custo novo. **1/1**, job **e39daaeb0dc64dda992af435a8762e0b**,1,6788837s, [probe](../validation/artifacts/gameplay-rework/gpr-01-5/d-asura-boundary.json). Não foi atribuído cast state nem alongado TTL/timer.

Correção somente na fixture: o caso original pede requireStationary=True ao helper e só age em recovery que ainda não permite movimento. Callback, asserts de prioridade/energia/mana/execução, threshold0,85 e deadline3s preservados. Outros consumidores do helper conservam o default anterior. Não foi alterado AbilityHolder, controlador, Asura, animação, buffer, recovery ou relógio do jogo. O caso original passou no filtro5/5 e na bateria ampliada final.

**Limite causal:** o probe confirma o defeito de admissão do helper e reproduz o mesmo valorFalse. Como a falha antiga não registrava waiting/progress naquele instante, não se prova retrospectivamente que toda ocorrência histórica veio dessa fronteira. Não declarar cura universal de timing; sob frames que saltam toda a janela, o teste ainda pode reportar falta de observação válida em vez de fabricar input.

## Regressão final e novo risco de pausa

Unity Test Framework via MCP, argumentos em [d-regression.request.json](../validation/artifacts/gameplay-rework/gpr-01-5/d-regression.request.json): seletores ampliados históricos +probe,204 casos. Job **56c2a29c44d5439886a104eb349abb06**, **203/204**,131,8532721s, zero ignorados. [Resultado integral](../validation/artifacts/gameplay-rework/gpr-01-5/d-regression-final.json).

Falha: PauseDiscardsRecoveryMovementAndDoesNotReplayOnResume, BasicAttackR1MovementFluidityTests.cs:112, `holder.IsCasting` esperadoTrue após pausa/0,4s reais. Asserts anteriores de descarte/rejeição de movimento passaram. O cast já não estava ativo no ponto observado. Foram consultados owner/helper e25 resultados históricos desse método, todos aprovados; [histórico](../validation/artifacts/gameplay-rework/gpr-01-5/pause-history.json). Não há trace suficiente para distinguir avanço residual no frame da pausa, premissa temporal da fixture ou defeito de produção. Não presumir causa nem editar tempos para obterPASS.

O caso passou inalterado no diagnóstico junto com visual/snapshot/pool/cargas, job **40bd953aa78d4a91a955d62b76c0ca40**, **10/10**,22,25779s. [Seletores](../validation/artifacts/gameplay-rework/gpr-01-5/final-checks.request.json), [resultado](../validation/artifacts/gameplay-rework/gpr-01-5/final-checks.json). EssePASS não corrige a falha nem transforma203/204 em204/204. Não repetimos a bateria até ficar verde. Risco pendente: estabilidade de suspensão de casts junto à fronteira de recovery; requer diagnóstico específico antes de encerrarGPR-01.

EditMode final de grafo/compatibilidade/ofertas/seeds/retidos/Asura: **90/90**, job **8c62271c2a964aa596f571b2a7da472e**,88,1930016s, [requisição](../validation/artifacts/gameplay-rework/gpr-01-5/edit.request.json) / [resultado](../validation/artifacts/gameplay-rework/gpr-01-5/edit-final.json). Zero ignorados; seleções não são suíte global. Foram exercitados GI, HP/analytics, WASD/dash, LMB, QER, RMBAsura, Bow/Spear, rewards, foco/disable/morte/run/cena, clear e próximos encontros nos filtros01.5a/d.

## Desempenho controlado na revisão final

Mesmas fixtures8/32/64, dois tornados simultâneos, pool2. ProfilerRecorder válido de TechGuy.Protocol.CPU eGC Allocated In Frame. Fontes de produção intactas; nenhum budget/densidade foi elevado.

| Alvos | Atingidos | Fila máxima | Impactos negados no conjunto | CPUProtocol mediana/p95 | Frame mediana/p95 | GC máximo/frame |
|---|---|---|---|---|---|---|
|8 |8 |9 |0 |0,0643/0,2082ms |2,2688/3,5390ms |422909B |
|32 |32 |32 |0 |0,0620/0,4703ms |2,7591/4,3583ms |664432B |
|64 |32 |64 |64 |0,0609/0,7176ms |3,1877/4,6958ms |648803B |

Dados completos em `gpr-01-5/final-checks/receipts/crowd-*.json`; medição inicial também preservada em a/receipts.64 satura o cap32 e a fila64; zero raízes/views ativas após cleanup. Frame inclui Editor/testes/setup/pool e não equivale aCPU exclusiva. GC não é atribuído só ao tornado; não declarar zeroalloc. Sweep/LOS/RaycastAll e ribbons estão agregados no marker; a auditoria estática confirma RaycastAll ainda aloca. Não houve isolamento por subrotina, profilerGPU ou benchmarkrelease/standalone, logo não se decide gargaloCPUvsGPU ou otimização por esses números.

## Integridade, arquivos e limitações

Somente testes/helper/documentação mudaram em01.5. Novos: GameplayTestEvidence.cs/meta, Gpr015AsuraStabilityTests.cs/meta, Gpr015ProductionAuditTests.cs/meta, Gpr015RewardAccessibilityTests.cs/meta. Alterados os três testes legados de ambiente/recovery e os dois helpersTitanic de destino de evidência. Compilação/Console e cena inspecionadas no MCP real. Skills quatro locais, router, Unity C#/MCP e performance-optimization aplicadas; sem delegação.

Baseline e auditoria final emgpr-01-5: cenas/prefabs/assets/GUIDs, produção e evidência histórica protegidos.36 preferências comparadas:32 iguais; quatro alterações pertencem à sessão Unity/Unity Connect e à chavePT_Run do pacote com.unity.test-framework.performance, não à progressão. Loadout/moedas/bindings/resumo persistido mantiveram os valores. Dos5 arquivos no diretório persistente, só TestResults.xml foi atualizado pelo próprio UTF; é saída operacional do último teste, não alteração de save do jogo. Não reconstruímos o XML anterior nem tratamos essa saída como evidência histórica imutável; os artefatos históricos emDocs foram preservados.

AssistênciaA+B e dois destinosSessionState de evidência restaurados aos valores iniciais. NexusBlockout limpa/EditMode,0 scripts ausentes e0 ribbons remanescentes. Console final conservou aviso de websocketMCP; não foi limpa. Checker genérico foi executado novamente e continua falhando no pacote externoTripo3D entreC:/D:, sem alteração de terceiros ou alegação de aprovação do checker. GUIDs focais/importação têm alcance separado.

Recomendação: não encerrar estabilidade deGPR-01 como aprovada. Investigar especificamente pausa na fronteira de recovery e obter playtest físico de leitura/tripla. Nenhum01.6, retuning, redesign ou expansão executado. A auditoria01.5 termina com essa ressalva explícita.
