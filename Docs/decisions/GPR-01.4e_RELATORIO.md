# GPR-01.4e — validação integrada e fechamento

29/10/2026 · **IMPLEMENTAÇÃO E VALIDAÇÃO INTEGRADA CONCLUÍDAS COM RESSALVA LEGADA. STOP após01.4.** Nenhuma regressão bloqueadora nova confirmada; uma ocorrência de Asura permanece sem causa demonstrada. Não há aprovação global de regressões ou novo aceite físico.

## Escopo e entregas

Depois dos gates01.4a–d: comparadores nativos C/B+C, aquisição durante voo, tickets reais A+C, carga8/32/64 com dois tornados simultâneos, regressõesGI/Protocols/armas/Asura/input/movimento/rewards,90 EditMode e auditoria de integridade. Criados os seis relatórios solicitados e atualizados requisitos, desenho, tarefas, validação/matriz, fatia, ledger, ADR0006, boons e estado/lacunas. Nenhum01.5/Singularity, balanceamento, especial, skill replacement ou redesign.

Novos arquivos de testes: TitanicCompositionPlayModeTests.cs, TitanicElementalVisualPlayModeTests.cs e TitanicCompositionIntegrationTests.cs, com metas importadas. Testes existentes alterados: TitanicConductorPlayModeTests.cs, TitanicFistsPlayModeTests.cs, TitanicTornadoPlayModeTests.cs e TitanicRoomClearPlayModeTests.cs. Writers antigos agora respeitam SessionState/pasta da atividade; expectativas antigas de proibição de C foram atualizadas para o contrato autorizado, mantendo dano/recursos/dedupe exatos. Nenhuma assertion enfraquecida para esconder falhas.

Produção acumulada01.4: BreakerGauntletCombat.cs, GauntletExecutionSnapshot.cs, ProtocolBudget.cs, ProtocolImpactIdentity.cs, ProtocolRuntime.Scope.cs, ProtocolRuntime.Damage.cs, ProtocolRuntime.TitanicFists.cs e TitanicFistsVisual.cs. Alterações anteriores01.2c/01.3 permanecem no diff, separadas pela baseline inicial. Sem commit/push.

## Comandos e resultados reais

Todos os jobs abaixo são Unity Test Framework pelo Unity MCP real, projetoD:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, instânciaTech-Guy@fccafb2b47cd7671. Argumentos exatos: [stage-requests](../validation/artifacts/gameplay-rework/gpr-01-4/stage-requests.json), [focused.request](../validation/artifacts/gameplay-rework/gpr-01-4/focused.request.json), [regression.request](../validation/artifacts/gameplay-rework/gpr-01-4/regression.request.json), [edit.request](../validation/artifacts/gameplay-rework/gpr-01-4/edit.request.json). Chamar run_tests com o JSON e consultar get_test_job(job_id,include_details=true); início de job não éPASS.

| Seleção | Resultado | Job | Duração |
|---|---|---|---|
| Integração nova/categoryGPR014e |7/7 |34bca951bf75400c89724e735f9ce9bd |13,4459895s |
| Fundação/Titanic/Protocols/lifecycle/rewards focados |98/98 |26a2fbc8b2bb4677abcd61001804423c |171,0936248s |
| Regressão ampliadaGI/Asura/Bow/Spear/input/movimento/Protocols |199/203 |db79017cf3414c77b42ffc894bdefe5c |132,9846686s |
| Diagnóstico dos quatro casos anteriores |4/4 |68f8ccd73d6d4b45ab60b60dda14e515 |3,0922674s |
| EditMode: grafo, compatibilidade, ofertas, seeds, retidos, Asura |90/90 |90d987a03dea4974ad900c0d8b755311 |118,1830698s |

Zero ignorados.278 nomes PlayMode distintos e90 EditMode tiveram aprovação observada na união das seleções finais, **368 nomes, sem somar repetidos**. A contagem NÃO substitui o resultado199/203 da bateria ampliada nem prova correção da intermitência. Artefatos detalhados e cálculo em [test-summary-complete.json](../validation/artifacts/gameplay-rework/gpr-01-4/test-summary-complete.json).

### Falhas e diagnóstico preservados

- Três casos de regressão passaram pelas asserts de gameplay, mas falharam ao gravar combo-observation.json, integration-movement.json e wall-comparison.json porque a nova pasta regression-final não havia sido criada. Corrigida somente a preparação do destino; os mesmos três passaram no diagnóstico4/4. Originais históricos não foram sobrescritos.
- CadencedRContinuationDiscardsRecoveryMovementWithoutPublishingAnotherAbilityUse falhou em BasicAttackR1MovementFluidityTests.cs:258: HasPendingGroundMovement eraFalse no callback de recovery. A mesma fixture passou no diagnóstico isolado, sem modificar código/tempo/assert. Causa não comprovada; não chamar de corrigida nem inferir que01.4 a introduziu. Não houve repetição da bateria para apagar a ocorrência ou tuning de Asura.
- EditMode longo desconectou duas consultas MCP; o job90/90 foi recuperado completo após reconexão. Não reiniciamos Editor nem rodamos batchmode concorrente.
- Checker genérico Unity terminouexit1 por ValueError de pacoteTripo3D entreC: eD:. [Limitação observada](../validation/artifacts/gameplay-rework/gpr-01-4/checker-observed.json). Auditoria focal de GUIDs/importação/cena tem alcance próprio, sem certificação fictícia do checker.

## HP, aquisição e desempenho

Recibos confirmam nativos C eB+C sem Titanic, vento12→Conductor5 letal→Detonation3,75, contagem de mortes e parent único. Quatro ordensABC/ACB/BAC/CAB são assistidas explicitamente. A+B por tickets reais seed0 eA+C seed1, rota1→2→4→5→6, uma Mutation e um Protocol; sem concessão gratuita nesses dois testes. A rota normal exercitada contém só uma oportunidade Protocol, portanto não foi falsificada uma aquisição tripla natural ou alterada a economia. Modo anterior de apoioA+B continua o existente, não foi estendido para concederC.

| Alvos | Tornados simultâneos/pool | Alvos atingidos | Fila máxima | CPUProtocol mediana/p95 | Frame mediana/p95 | GC máximo/frame |
|---|---|---|---|---|---|---|
|8 |2/2 |8 |9 |0,0882/0,2822ms |2,9866/4,3974ms |535853B |
|32 |2/2 |32 |33 |0,106/0,717ms |4,4349/6,4633ms |1838960B |
|64 |2/2 |32 |64 |0,1065/1,117ms |4,2572/6,7704ms |750447B |

Medições da primeira rodada controlada emreceipts/crowd-*.json: ProfilerRecorder de TechGuy.Protocol.CPU eGC Allocated In Frame válidos. Overhead de Editor, testes, setup/pool inicial e gravação incluído; frame não é tempoCPU exclusivo. Não isola sweep/LOS/RaycastAll/ribbon/GPU, não certifica standalone nem ausência de allocations.64 negou64 tentativas derivadas no conjunto, cap32 preservado; zero roots e views ativas no teardown. Não houve rewrite de desempenho.

## Unity, integridade e limites

Editor final NexusBlockout limpa/EditMode, compilação concluída,0 missing scripts,0 ribbons de tornado remanescentes. Console final amostrada pelo MCP:0 erros/avisos retornados, sem Clear manual; isso não apaga falhas históricas. Modo de assistência prévioA+B observadoTrue, preservado. Campos de evidência SessionState só redirecionam testes; não escrevem PlayerPrefs.

Baseline2675 arquivos, hashes, diff preexistente e auditoria emgpr-01-4. Nenhum arquivo protegido de cena/prefab/asset/meta/config preexistente ou artefato histórico alterado; inventário final discrimina fontes/documentos intencionais e GUIDs. Saves/preferências/progresso não recebem novas escritas do runtime; scopes existentes das fixtures preservam as chaves que controlam. Não há prova universal de todos os saves/PlayerPrefs ou save/resume de run inteira.

Skills efetivamente aplicadas: router, unity-csharp-scripting, unity-mcp-orchestrator e quatro locais de visão, combate, boon-design e aceiteUnity, incluindo referências. Sem delegação ou instalação fictícia.

Limites remanescentes: ocorrência legada de recoveryAsura, revisão física das novas camadas/telegraphs, aquisição tripla natural na rota atual, benchmark isolado/standalone e save-resume integral. Recomenda-se revisão física e decisão sobre a intermitência antes de autorizar01.5. Não há nova atividade autorizada pelo encerramento deste relatório.
