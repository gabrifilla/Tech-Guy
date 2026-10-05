# Validação incremental — Run Structure

Execução iniciada em 04/10/2026. Fase ainda em implementação; aceitação humana da task 15 pendente. Fases 2–3 permanecem aceitas. Mudanças existentes no workspace foram preservadas.

## Tasks 1–2 — auditoria e checkpoint

Auditoria inicial corrigida conforme task-1-review.md; decisões, callers e evidências atuais em contract-audit.md. MCP Tech-Guy@fccafb2b47cd7671, Unity 6000.5.10f1, projeto correto, FirstSector limpa e PlayMode encerrado. Preview scenes fechadas sem salvar. Inspeção de referências principais, 16 arquétipos/prefabs, arma de quatro slots, HUD/câmera/player e NavMesh existente. Console não foi limpo.

PlayMode energia/recompensa 1/1 passou (01f2a04495d74a618bbc22e8c112a9bd); EditMode FirstSectorProceduralTests 9/9 passaram (e936e35c12584eb08eeab69775eeaac2). Timeout da primeira tentativa EditMode resolvido com init_timeout=120000. Avisos da fixture de combate não são novos defeitos de cena. Registro externo de 488 testes no checklist não foi executado por esta sessão e não é usado como evidência.

## Task 3 — dados e autoria

Modelos serializáveis por listas, bindings separados da definição, snapshot que copia números/listas/spawns/grupos/percursos, sem mudar RoomType nem scenes publicadas. Validador verifica D1 estruturalmente (IDs podem variar), quatro percursos e ordem de recompensas, grupos exclusivos completos, ciclos/referências/duplicatas, valores brutos, spawns/prefabs e quatro alvos reais de Mutation usando snapshots da Manopla. OnValidate apenas diagnostica.

Orçamentos defensivos de autoria: 11 nós/12 conexões/2 grupos do D1, até 32 spawns por encontro, arenas entre 4 e 128 unidades, centros X/Z entre -2000 e 2000, offsets Y entre 0 e 5. São limites de validação, não tuning de dificuldade; Repair deve ser finito em (0,1]. Zero bruto de Repair é recusado, não normalizado silenciosamente.

Primeira execução MCP ManualRunDefinitionTests: 21/21 passaram (10d20e9a2fc6453c9d0adfa0f106414b), incluindo save/unload/import/reload de asset temporário com referências reais e limpeza pelo AssetDatabase. Após adicionar OnValidate e modelos de bindings, uma execução pertinente adicional verifica a versão final. Compilação/resultado final registrados abaixo.

Versão final task 3: import/compilação concluídos, Console consultado sem erros; ManualRunDefinitionTests novamente 21/21 passaram (80d743fb8d6d476797bc10d72a858020). Task 3 marcada concluída antes de iniciar task 4. Definições, bindings e testes receberam metadata pelo import do Unity; nenhum GUID existente alterado.

## Pendências

Task 5 concluída em fixtures isoladas: único ProgressionDirector (partial com lógica manual separada), seleção da fonte persistida antes de gerar, outras famílias seguem legado; preflight definição/ambiente, volumes/gates, spawn com player injetado antes de Awake e morte registrando apenas candidata. ManualStageEnvironment não gera nem assa navegação runtime. FirstSector publicada permanece modo 0 com player/environment conectados e sem definição/ambiente manual, confirmado por SerializedObject via MCP. Entrada manual também revalida arma real após Awake; não troca família silenciosamente.

PlayMode 8/8 passaram (29eaff38b591441c8cbf411a45b94512): três testes do mundo manual e regressões StageRoomActivationTests/StageDoorSealingTests. Primeira execução teve uma falha da fixture de Arco, que reequipava a arma fictícia do harness; corrigida a preparação para equipar a seleção real, sem relaxar asserts. Fixtures de ambiente colocam volumes juntos deliberadamente para isolar coordenação; não validam layout, dificuldade ou navegação da rota real.

FirstSectorProceduralTests após integração 9/9 passaram (63dd594092c8438ea7c9ec319d617720). Tentativa anterior 79f96d3f7bff4cf392a7767dd58b9c41 retornou timeout de inicialização, sem resultado contabilizado. Console contém os erros esperados pelos testes de referências inválidas e da regressão de morte em GameObject inativo; distinguir de falhas não esperadas. Compilação/import e estado idle fora de PlayMode verificados. Não houve publicação manual nem save da cena do usuário.

Task 4 concluída: sessão com Guid local, phase/current/pending distintos, exclusões persistentes e ordinais, guard de estado/clocks e deduplicação. ManualRoutePolicyTests 15/15 passaram via MCP (6432b92cfcff4731814ae371ffaefee9), abrangendo os quatro percursos, entrada fora de ordem, callbacks antigos, rejeições sem mutação, primeira Mutation, dois ordinais e prioridade de morte/claim de extração. São fixtures de policy; não simulam aplicação real de boons, cura, pipeline de combate ou scene load. Import/compilação e Console sem erros nesta etapa. Nenhum novo singleton nem alteração no combate publicado.

Tasks seguintes: recompensas, HP/Repair, Elite/Mutation, fronteira mecânica, lifecycle e UI/integração. Runtime ponta a ponta e feeling ainda não validados. A cena publicada continua procedural até task 13.

## Task 6 — autoria física isolada

Definição D1, prefab de ambiente e NavMesh criados e salvos via Editor/MCP em pastas Run próprias. Onze arenas, doze corredores com barreiras/carving e anchors, entradas e pontos de reward; projeção RoomGraph usa geometria/anchors sem grid. Spawns usam prefabs existentes, E1 um Elite e quatro comuns, Boss rank explícito. Nenhuma publicação ou save de FirstSector/Nexus nesta etapa; roots manual e legado exclusivos.

Persistência/validação: 24/24 EditMode passaram (429c43285b464ff08bddf03e908089dd); versão final de autoria/projeção 2/2 (04911acc6dbe4f2a948f9f70914f6362). Uma tentativa perdeu conexão em reload e outra com namespace incorreto executou zero testes; nenhuma delas conta como aprovação.

PlayMode navegação/mundo/portas 5/5 (7929576907fb48ae851aed6e9ae890c4). Verificação posterior incluiu retorno pelos corredores concluídos: navegação e coordenação passaram; teste adicional de dash/Q inicialmente falhou por condições da fixture (distância superior ao avanço autorado, depois mana zero). Corrigidas as precondições sem alterar habilidades nem relaxar bloqueio de travessia; dash e Q reais passaram 1/1 (56ec8312f6374acc967dbcd6cec4bbbf). Sessões de policy simulam resolução de recompensas, não comprovam aplicação/combate ponta a ponta. Console consultado, PlayMode encerrado, cena original preservada. Aceitação de dificuldade/diversão continua pendente.

## Task 7 — recompensas tipadas

RunRewardRequest identifica sessão/nó/ordinal. Policy local congela até três IDs distintos, mantém revisão e claim antes do aplicador/evento; falha parcial recebe Faulted/diagnóstico, sem retry. RunBoons reutiliza o aplicador existente com evento contextual separado; OfferReward(int)/RewardChosen e roll Rewrite continuam legados. Catálogo manual contém dez Techniques/ranks vigentes, quatro Mutations/alvos validados/aliases e somente ignite/frost/conductor/detonation. Esgotamento usa +40 Armor real; primeira Mutation vazia permanece pendente.

EditMode policy inicialmente 10/10 (0dbde94108894a24ae09e359e7412581); versão final com alias e regressão Rewrite 12/12 (3301659fc82d4fa0bd841f2038e8ccac). PlayMode integração de rewards, legado Mutation e bind no mundo 4/4 (b2771d0c0530497798d14036c9724be1); versão final rewards/protocols 3/3 (431a9489103547608c8c28e398b0ed16). Armor +40 único, HP/mana/Asura preservados, reabertura/revisão/reentrância e aquisição real de Mutation observados. Cada Protocol aplicado sem Protocol anterior em C5/E2: fixture congela um card elegível para exercitar os quatro efeitos deterministicamente; composição/distribuição testada na policy. Resoluções anteriores são stand-ins explícitos, não playthrough real. Fonte da arma permanece intacta. Import/compilação MCP e Console sem erros após corrigir import inicial dos arquivos novos e API obsoleta GetInstanceID (API GetEntityId conferida por reflection MCP). Fronteira de encontro/UI final ainda nas tasks 11/13.

## Task 8 — recursos e Repair

Inicialização manual única após inventário/clones prontos; transições não fazem refill/reequip. Reconstrução explícita RefreshResourceStatsPreservingAmounts calcula máximos finais e preserva quantidades absolutas/clamp, sem mudar RefreshResourceStats(bool) legado. Repair usa fração autorada 0,35, déficit e claim antes dos callbacks de Heal; skip resolve a mesma oportunidade, HP cheio recusa Use, falhas parciais não permitem retry. UI final de Repair na task 13; HUD existente observado com prefab real.

EditMode Repair 16/16 (9eac940094644bd0a030c2d8dec71c82): baixo/cheio/max alterado, zero/NaN/infinito, reentrância, estado inválido, dispensa e morte durante callback. PlayMode 3/3 (c9cc6f021fe247d9aee5d28af3ebf635): cura real/HealthChanged/HUD, cooldown/mana/Asura preservados, HP absoluto/max/clamp, Iron Heart legado cura na aquisição mas não na reconstrução, reativação do diretor mantém sessão/HP. Compilação MCP confirmada por scriptCompilationFailed=false depois de corrigir tipo do source de modifier na fixture. Console sem erros novos; avisos de dependências/Animator das fixtures permanecem. Autoria RP persistida verificada na task 6. Progressão inicial de fixture é resolvida pela policy; integração completa continua pendente.

## Task 9 — Elite e dois ordinais

Diretor escuta somente eventos contextuais e abre o segundo ordinal com inventário atualizado, mantendo saídas seladas. Release gate impede aquisição na mesma frame e enquanto 1/2/3/Enter/clique estiverem segurados; Update observa soltura no Input System. E1 usa prefab Magic_Haste existente em rank Elite mais quatro Grunts; perfil/AI/reação atuais preservados, E2 normal adicional/Protocol. Vantagem técnica: duas aquisições, inclusive +80 Armor efetivo quando Techniques estão no limite. Tuning/risco percebido não foi aprovado por testes.

PlayMode composição autorada, tecla segurada/solta, +40/+40, duplicação, revisão e regressão de ataque protegido Elite/Legendary/Boss: 3/3 (63ff6c83fc904d9e9b0abde4381c6c66). EditMode release gate 1/1 (7be35656112f4bf58845c8b3315f106c). Morte entre ordinais 1/1 (6adb49e73adf4a06b875f17c3f43b521): jogador morto recusa segunda aplicação e teardown destrói inventário. A primeira versão da fixture verificava inventário após sua destruição; corrigido momento da observação, preservando requisito. Dois runs que não inicializaram por referências de assembly da fixture e um timeout EditMode registrados sem contabilizar resultado; corrigidos InputSystem asmdef e referência ao builder Editor em teste PlayMode. Console sem erros após correções; original FirstSector restaurada pelo runner. Nós anteriores/completion frontier são stand-ins explícitos; task 11 substitui esse ponto de integração, task 12 fecha resultado de morte.

## Task 10 — garantia e recuperação

C4 permanece pendente ao fechar UI; apenas aquisição contextual conta. Sem opções para primeira Mutation: diagnóstico único por ticket, Boss selado e API de aborto/Nexus (Aborted, nunca vitória). PlayerActor consulta preflight local do RunBoons antes de cancelar/equipar: outras famílias recusadas; troca compatível válida usa novas cópias isoladas, mantém inventário e segue reset Asura da troca real previsto na Fase 3. Nenhum slot é removido para fabricar elegibilidade.

PlayMode família/troca compatível passou na execução d6bbb6ea679d49dabe2f4b3463372092; a execução teve uma falha de fixture no combate Demolidor, que esperava dano na trajetória, explicitamente ausente no contrato Fase 3. Alvo corrigido para a zona do uppercut final, sem modificar combate. Versão final 2/2 (dcd38419b37e4b4ba3ede2c2563e0c92): quatro combinações de ramos com aquisição real C4, quatro Mutations executadas contra alvo com mana/snapshot/dano e fontes preservadas; ocupação prévia de todos slots não conta como aquisição, cancel/reabrir mantém ticket, aborto único. Cards individuais precongelados a partir do catálogo para testar deterministicamente cada ação; nós anteriores e composição do próximo combate são fixtures, não playthrough completo. Recuperação tentou retorno com destino deliberadamente ausente, validando resultado e diagnóstico sem carregar outra cena; carga real/cleanup/retry finais são consolidados nas tasks 12–13. Alias/import/elegibilidade pura cobertos nas tasks 3/7. Import/compilação e Console MCP sem erros não esperados.

## Task 11 — fronteira mecânica e cleanup

Última morte registra candidata/EncounterClosing, bloqueando novo input sem cancelar o lote atual. LateUpdate espera transação e leases de feedback fechados e pausa liberada; reclama uma conclusão, para somente cast/filhos/inputs futuros nos owners, mantém timers naturais e prepara ticket mesmo com hit-stop confirmado ativo. Não chama Configure(null), não muda energia/categoria/epoch nem remove hit-stop; confirmação de UI continua rejeitada durante hit-stop. Morte na mesma resolução impede publicação de reward. Boss fica em FinalResolution para task 12.

MCP PlayMode 4/4 (c4be1cda8b55463cae762229701104dd): básico real multi-alvo letal, feedback aberto entre frames, skill/produtor letal com Echo futuro, integração inicial e regressão Guard/letal. Último Echo, morte simultânea e regressões cancel-before-producer/ordinary-cancel 4/4 (99c505bf386847e7b57b44c8cce61c83). Após acrescentar guarda de input entre candidata e cleanup, versão final dos quatro cenários de fronteira 4/4 (b88ce3df2ab640f29f8e3d1919c42645). Native gain pago uma vez, HP/eventos fechados, filhos futuros zerados, hit-stop ativo preservado, categoria/epoch estáveis, ticket/publicação únicos e morto sem reward. Import/compilação MCP, Console sem erros, fora de PlayMode. Fixtures coordenam mundo e golpes reais, sem aprovação de dificuldade/diversão.

## Task 12 — Boss, extração e lifecycle

Boss é resolvido na fronteira mecânica, sem recompensa final de build: vivo disponibiliza um portal; morte na mesma resolução ou antes da reclamação vence. O portal recebe guard/callback local opcional, preservando polling/destino e comportamento legado. Extracted é reclamado antes de teardown/carga; contatos repetidos e callbacks antigos não alteram resultado. Aborted/Dead nunca criam vitória. Retorno usa loader local injetável, duas tentativas limitadas e retry explícito apenas após falha; nunca inicia nova operação enquanto a anterior está pendente. Atraso de morte permanece 0,5 s escalado, conforme legado. Inventário/efeitos/locks próprios são limpos uma vez; clones equipados permanecem válidos até unload e são destruídos pelo OnDestroy existente, evitando referência destruída durante retry. Arsenal persistente não é alterado.

Unity MCP: compilação confirmada sem falha. PlayMode 4/4 (7784dbc492704809ad57cc28254a6ed2): extração única/falha/retry, Boss e jogador mortos no mesmo lote, recuperação por aborto e regressão do último lote letal. PlayMode 3/3 (aa93597e67eb48dd8d3f264bfca3f806): morto em contato residual com portal, identidade nova rejeitando pedidos antigos e duas regressões de retorno por morte legado. EditMode policy 15/15 (6443eadbe95a4ccaa4bec87ef6373643): gates, ordinais, quatro rotas, morte/extração e sessões. Os dois erros de NexusLobby_NotInBuild no Console correspondem aos testes legados esperados; não foram apagados. Fixtures usam policy para nós anteriores e loader falso para verificar concorrência/falha; carga real de cenas e duas runs completas são validadas na task 13. FirstSector preservada, fora de PlayMode. Não aprova dificuldade/diversão.

## Task 13 — UI e cenas publicadas

FirstSector publicada via Unity MCP: opt-in ManualSlice, definição/prefab autorados conectados, player inicialmente inativo para ativar o NavMesh certo antes de seu Awake/OnEnable. Director ativa o player tanto manual como legado, sem trocar arma. Nexus exibe modo derivado de WeaponLoadout existente (Manopla manual, Bow/Spear procedural). ManualRunUI, local ao diretor, apresenta mapa/funções/categorias/portas, status, riscos/benefícios dos ramos, Repair efetivo, garantia de Mutation, aborto e retry. Só emite pedidos ao owner. UI de recompensa identifica categoria/Armor alternativa, mantém ticket ao fechar e explica impossibilidade; cache inclui revisão. Objetivo manual não anuncia fragmentos legados.

Locks de movimento/input pertencem à UI; closing espera release, não autoriza básico/skill/clique involuntário. Boot bloqueia input até inventário/resources prontos. Defeito demonstrado: TryDirectionalBasicAttack rejeitado pela UI ainda fazia ResetPath; guarda antecipada mantém rejeição sem efeito colateral. Fixture de Elite foi atualizada para aguardar a fronteira real de task 11, em vez de concluir a policy por fora. O teste de mouse isola e restaura o dispositivo físico: seus eventos reais interferiam na simulação de hold. Refresh scope scripts não importou algumas alterações; force/all e reflexão confirmaram assembly atual antes dos resultados finais.

MCP save/reload confirmou modo 1, player inativo, definição válida, UI/bindings/NavMesh persistidos. PlayMode cenas reais inicialmente 2/2 (b9dcc3c72c544c9a997a7175279a91e9); versão consolidada 5/5 (82bca46fe8014a8599acbe467d938355): Nexus → quatro percursos → C4 com Mutation real → Boss → portal/carga Nexus, runs com novas identidades/inventário/HP/Asura, morte/retorno e Bow procedural, mapa bloqueando input e exigindo soltar clique, Elite real com dois Armors e regressão de Q/W/E/básico em espera cadenciada. Testes usam Warp e kills determinísticos: não afirmam que o jogador navegou todo corredor nem aprovam dificuldade; navegação real/portas/dash/avanço já cobertos na task 6. Execuções intermediárias 4d4b3bd5d3e14c9faf1870ff37c88f3b, 4ca4a2c4fc4b4cf6bf200fe3ad187f16, c815d12355e4405da7f826bb30cab703, 14db4bbb3ac64be2b0f5afe23d51fa76 e c374f5663b28464da0d9b92382b8fcb9 tiveram falhas corrigidas de input/fixture; gate de mouse final 1/1 (eb6c9be8ae2744488ab1444f07961613). Nenhuma falha foi contabilizada como sucesso; filtro incorreto de nome de Elite em execução intermediária não é usado como evidência.

Inspeção visual MCP/ScreenCapture em PlayMode encontrou linhas fora do scroll; desenho ortogonal com clipping, contraste e ordem do painel corrigidos e nova captura revisada. Mapa inicial agora mostra S/C1/C2 e alternativas em ordem; itens posteriores acessíveis por scroll. Runtime observado em RouteChoice, player em (500,0.08,0), agente no NavMesh, UI aberta. Console sem erros; warnings conhecidos de keys vazio usam Q/W/E/R fallback. Screenshots locais temporários Temp/RunStructureMapFinal.png, não assets de jogo. PlayMode encerrado, FirstSector limpa, seleção persistente original restaurada. Aceitação humana permanece task 15.

## Task 14 — checkpoint técnico e handoff provisório

Versão final após tratamento de unload/cleanup idempotente e assertion de NavMesh exclusivo: MCP PlayMode 3/3 (8095ceabb15749b996fac1b0b3ad987d), percursos publicados/extração, morte/Nexus/Bow legado e básico direcional legado. A alteração posterior de desenho foi conferida por captura real de PlayMode, sem repetir testes de mecânica não alterada. Resultado de compilação final: scriptCompilationFailed=false. Console sem erros; avisos de Animator/câmera/dash/click ausentes pertencem à fixture do básico legado; keys vazio usa o fallback existente e também aparece nas cenas reais. Nenhum Console foi limpo.

MCP final: FirstSector e Nexus abertas/recarregadas, zero scripts ausentes, definição/bindings válidos (11 nós/12 conexões), NavMeshData correto, player inicial inativo, LobbyInteraction com player válido/quatro estações. Nexus fechada sem alterações; FirstSector limpa, fora de PlayMode. Assets novos com .meta; GUIDs preservados: FirstSector f62bb8b29264e014aa2e95e916388ed9, definição 4d17687f81ce0dc42b2035a3dec505e3, prefab bf6c2c11bb28e6a4dbb40743c37f5b61, navegação 1317bac79e11b6c41ad6e27ae80676ce. Diff da cena limitado a player inicialmente inativo, campos do diretor, UI e instância do prefab. Espaços finais em m_Name são saída normal do serializer Unity. Sem mudanças em Nexus, RoomType, Resources ou terceiros neste trabalho; alterações preexistentes em outras specs/combat/assets foram preservadas e não são atribuídas a esta fase. Não houve commit/reset/discard.

| Contrato / propriedades | Evidência incremental |
| --- | --- |
| R1, P1/P2 — topologia/compromisso | Tasks 3–6: quatro rotas/gates/volumes/nav; task 13: quatro percursos em cenas reais |
| R2, P9 — mapa/input/gates | Tasks 5–6: portas/dash/avanço; task 13: GUI real, input/release, estados físicos/lógicos |
| R3, P3/P4/P15 — rewards/tickets/Protocols | Tasks 7/9: frozen/revision/commit/reentrância/alternativa, quatro efeitos alcançáveis; task 13: aquisições publicadas |
| R4, P5/P13/P16 — HP/resources/frontier | Tasks 8/11: max final/HP absoluto, pausas/feedback/Asura/category/epoch; regressões dirigidas |
| R5, P6 — Repair | Task 8: cura limitada/única/dispensa/rejeições; task 13: UI e rota; tuning humano pendente |
| R6, P7 — Elite | Tasks 6/9: autoria/rank/composição, duas aquisições reais/release; task 13: caminho publicado |
| R7, P8 — Mutation | Tasks 3/7/10: validação/slots/import/família/garantia/aborto, quatro formas em combate; task 13: C4 real em quatro caminhos |
| R8, P10/P11 — Boss/sessão/retorno | Tasks 4/12: gates/morte concorrente/portal/retry/stale callbacks; task 13: cargas reais e runs novas |
| R9, P12/P14 — autoria/compatibilidade | Tasks 3/6/13: save-reload/snapshot/GUIDs/exclusividade de modo; task 14: diff/referências/metadata |
| R10.1–R10.4, R10.7–R10.8 — checks técnicos | Evidências MCP por task, limites/falhas/retries registrados e cena preservada |
| R10.5–R10.6 — gameplay humano | Task 15 pendente; kills determinísticos/Warp não aprovam decisões/dificuldade/diversão |

### Como experimentar e próxima ação

No Nexus, selecionar Manopla no arsenal e entrar no Primeiro Setor. Mapa abre em RouteChoice; escolher destino compromete o ramo e abre apenas sua passagem. [M] fecha/reabre o mapa, com movimento/básico/skills bloqueados enquanto aberto. Após encontro, escolher cartão; fechar mantém reward pendente para reabrir pelo mapa. Repair permite usar cura efetiva ou dispensar; Elite tem combate extra/duas Techniques. C4 exige Mutation real antes do Boss. Portal conclui extração; aborto/morte retornam sem vitória. Bow/Spear exibem e usam o modo legado. Falha de carga apresenta retry, preservando resultado.

Task 15 requer duas runs humanas consecutivas com a mesma Manopla. Registrar HP antes das decisões A (C2) e B (C4), ramo/motivo, aquisições/Mutation, uso/dispensa de Repair, benefício/risco do Elite e desfecho. Cura de 35%, Elite 1+4 e Armor +40 continuam hipóteses de tuning; não foram aprovados por testes automatizados.

| Run humana | HP A/B | Ramos e motivos | Aquisições / Mutation | Repair / Elite | Desfecho / clareza |
| --- | --- | --- | --- | --- | --- |
| 1 | Pendente | Pendente | Pendente | Pendente | Pendente |
| 2 | Pendente | Pendente | Pendente | Pendente | Pendente |

Entrega técnica pronta; task 15 permanece desmarcada e task 16 depende desse resultado. Este handoff é provisório, sem declarar a fase encerrada. Se gameplay demonstrar ajustes, aplicar no escopo atual e repetir somente checks afetados antes de registrar aceite. Nenhuma proposta de fase futura foi incorporada.

### Correções após feedback da primeira run — 04/10/2026

O usuário relatou movimento por clique indisponível, bloqueio após eliminar a segunda sala e dificuldade de leitura das escolhas. A inspeção Unity confirmou pisos publicados na layer Default enquanto o raycast de movimento usa Ground. Os 33 colliders de pisos de arenas/corredores foram corrigidos no prefab existente, preservando GUID e geometria; o authoring agora atribui Ground e valida sua disponibilidade.

A apresentação da recompensa tentava abrir uma única vez após a morte final. Hitstop/recuperação podiam bloquear essa tentativa sem apresentação posterior. O diretor agora tenta apresentar o ticket pendente quando as condições permitem; fechar voluntariamente mantém a mesma oferta fechada até reabertura explícita. Não há seleção automática nem reset de energia, epoch ou feedback confirmado.

A UI prioriza cartões de destino com categoria/quantidade de recompensa, adversários e custo de oportunidade; a rota completa fica disponível por botão. Inclui atalhos 1/2 com proteção de liberação, HP, situação da Mutation, inimigos restantes e orientação de movimento/reabertura. As categorias de recompensa já existentes são apresentadas com maior clareza; não foram criados Protocolos novos neste ajuste.

**Verificações efetivamente executadas pelo Unity MCP:** importação/compilação sem falha, Console sem erros, teste de movimento físico pelo raycast real e teste de duas salas com básico nativo letal, hitstop, apresentação automática e fechamento/reabertura. Job `e778f11eaca04817971ccc9fe8a28ae7`: **2/2 PlayMode aprovados**. Regressões de proteção de input e preservação de energia/hitstop/epoch no impacto final, job `20b440e9c4314faf831f507c48e22be3`: **2/2 aprovados**. A primeira tentativa destas regressões perdeu conexão sem produzir resultado; foi repetida após reconexão. Não foi executada a suíte completa.

Inspeção visual em Play Mode do cartão inicial publicada em `Temp/phase4-route-feedback.png`: conteúdo legível na resolução capturada, categoria e confirmação visíveis. Essa observação não comprova todas as resoluções nem substitui avaliação humana dos dois ramos. Play Mode encerrado após a inspeção, preservando a cena original NexusLobby.

O usuário declarou “Gostei de como ficou” e autorizou iniciar a Fase 5. Esse aceite da apresentação/avanço está registrado; não equivale a um relato de duas runs completas. O registro de gameplay exigido pela task 15.1 permanece pendente. A Fase 5 inicia em `../protocol-system/requirements.md`, sem implementação de Protocolos neste handoff.
