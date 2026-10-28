# GPR-C01.3a — clique direcional de básico e Asura no RMB

27/10/2026 · America/Sao_Paulo · fase GPR-C01 · atividade **GPR-C01.3a**.

**CONCLUÍDA tecnicamente; aguardando revisão do desenvolvedor.** Foram implementados o novo básico primário e, por autorização adicional durante a atividade, Asura no RMB da Manopla. C01.3b não foi iniciada. Aceite humano final de input, movimento e apresentação continua pendente.

## Autorização e preparação

[Registro do pedido e ampliações](AUTORIZACAO_GPR_C01_3A_20261027.md). C01.2c foi aceita como conclusão técnica no escopo documentado. O pedido inicial restringia C01.3a a LMB; mensagem posterior autorizou Asura no RMB. O desenvolvedor escolheu expressamente preservar hold **somente com ChargedShot** para o básico primário do Arco. Também solicitou commit/push de tudo acumulado e desligamento depois do push, tratados no fechamento Git, não como autorização de outras features.

Lidos AGENTS.md, canônicos GPR/requisitos/design/tarefas/ADR-0006/ledger/matriz, contratos relevantes, skills e referências. Somente AGENTS.md da raiz foi encontrado como instrução de repositório. Projeto real **D:/Dev/Personal/My_games/Tech-Guy**, branch **gameplayOverhaul**, HEAD inicial **3a3f0ee8ad82734104511b63877d38df512ea146**. Havia extensa alteração anterior: o snapshot Git inicial foi preservado, sem reset ou descarte. O cwd antigo do chat em C: não corresponde ao projeto aberto.

Unity MCP nativo agora disponível e exercitado, complementado pelo cliente MCP HTTP para arquivar dados estruturados e reproduzir filtros. Instância **Tech-Guy@fccafb2b47cd7671**, Editor **6000.5.10f1**, Input System **1.20.0**, AI Navigation **2.0.14**, Test Framework **1.7.0**. Inicialmente Edit Mode, NexusBlockout limpa, sem compilação/teste ou sessão humana ativa observável. Nenhum batchmode paralelo aberto sobre o projeto. Baseline atual de combate **30/30 PASS**, job`9c2524071a774ac4978f425c93995481`, antes das mudanças. Console inicial: zero erros e quatro avisos existentes de keys incompletas.

## Comportamento entregue

### LMB / básico primário

No modo WASD publicado, Primary lê o edge da binding efetiva. Cada press deliberado pede um ataque direcional individual. A direção vem do raio do cursor contra o plano horizontal do personagem; não usa WorldClickResolver, busca de inimigos, assistência de clique, snap ou target lock para esse input. Clicar chão vazio pode iniciar antecipação/animação e tentativa de impacto que erra. Não exige collider sob o cursor. Mira inválida, direção degenerada ou câmera ausente não autoriza ataque.

A execução usa `TryDirectionalBasicAttack` → `PerformAttack` e os owners existentes. Manopla conserva `BeginGauntletBasicImpact`, scheduler/ImpactEvents, geometria, contexto causal, confirmação de HP, reação e hitstop existentes. Spear conserva o caminho de área e seu alcance autorado; Bow conserva projétil. Um clique não agenda a sequência inteira. `nextAttackTime`, duração busy, reset do combo, animações e perfis não foram retunados. Cliques sucessivos válidos avançam somente o próximo estágio. Não há navegação, aproximação automática ou seleção de alvo pelo novo LMB.

**Buffer:** reutilizado o `AbilityHolder._inputBuffer`, já single-slot, `CommandKind.Basic`/`CommandIntent`, TTL existente **120 ms**, clock unscaled excluindo pausa/hitstop. A direção é capturada no press, não recalculada quando a intenção libera. Último pedido válido substitui o pendente; não há fila ilimitada. Apenas impedimento temporal retém um clique. Cast de skill comprometido, dash/pull ou contexto inválido não ganham cancel irrestrito por básico. Skill/dash aceitos, cancel, troca de arma, UI/foco/disable descartam a intenção básica apropriada. Repetição de hold não alimenta esse buffer.

O desenho anterior mencionava o BasicAttackDriver como seam provável. A inspeção mostrou que seu tap legado guarda seleção/hold sem expiração direcional, enquanto o holder já possui o buffer genérico de comandos. Usou-se esse owner existente em vez de um segundo buffer, executor ou novo relógio. O driver legado continua para os contextos preservados.

### Exceção aprovada do Arco

Sem ChargedShot: um clique pede uma flecha, segurar não repete. Com rank de ChargedShot ativo: preservada a sequência legada de disparo inicial e disparos por cadência enquanto segurado; a carga acumula entre disparos e é consumida pela flecha. Soltar interrompe o hold e reinicia a carga; não fabrica disparo de release. Aim de cada novo disparo segue o cursor no início de sua execução.

A descrição do helper ChargedShot sugere disparo normal ao soltar antes do threshold, mas o controlador real não implementava uma regra universal de “carregar e disparar ao soltar”. Essa divergência foi auditada e a decisão do usuário preservou o comportamento real, sem inventar outra mecânica. O teste equipa um rank pela API de inventário existente: não habilita novos boons/ofertas do catálogo. Hold da habilidade de Arco index3/Judgment permanece no executor original, separado do básico.

### Asura no RMB — ampliação autorizada

O canal secundário efetivo é exposto semanticamente por `GamePreferences.WeaponSpecialBinding`, usando a binding existente de Move, **Mouse1 neste perfil**, sem reescrever preferências. No modo direto de Manopla com AsuraBurst, RMB edge chega ao mesmo `AbilityHolder.TryUseAbility(3)`. A referência, Active/Cooldown, energia, mana, executor e identidade histórica R continuam únicos. Não chama TryActivate diretamente e não duplica Asura numa nova habilidade ou em X.

Binding legada de Skill4/F11 foi preservada como compatibilidade: RMB ou F11 no mesmo frame resultam numa entrega única, não dois custos. HUD/GetAbilityKey(3) mostra a binding secundária. Labels mínimos do painel existente foram corrigidos; não foi criado rebind completo ou outro backend. Uma personalização anterior do canal secundário é respeitada, em vez de gravar Mouse1 por cima dela.

RMB da Manopla não alcança mais o caminho legado de básico/interação por mouse; sem energia não há fallback para soco. Em LMB+RMB simultâneos, a intenção especial tem prioridade, independentemente da ordem dos Updates. UI gráfica/contextos impedem o special e exigem soltura. Cadenciada usa um novo press por continuação, mesmo execution token e custo único; hold não continua. F/FinishSequence continua prioritário sobre RMB simultâneo. Arco/Lança mantêm suas ações secundárias legadas: nenhum especial novo foi inventado.

**Limite de migração:** Asura ainda possui referência/estado legado index3 por compatibilidade com snapshots, boons e estatísticas. Este adapter não entrega quatro slots modulares independentes nem conteúdo X. A extração completa do loadout e Settings continua pendente de design/etapa própria.

## Facing e animação

`HandleDirectMovement` controla facing livre pelo cursor e animação de locomoção por deslocamento real. Ao iniciar básico, `FaceDirection` captura a direção; busy/cast impede facing livre de sobrescrever esse compromisso. `SkillAnimationPlayer`/BasicImpact continuam donos da pose e do contato. O novo input não gira globalmente o corpo para o vetor WASD nem altera root motion.

No jogador real, controller **Animator Controller Character**, root motionfalse; clips observados: Idle, Walk_N, Run_N, PunchRightA, PunchLeftA, Quad PunchA, Death, RollForward e GetHit. A lista desse controller não oferece um conjunto completo de strafe/backpedal. O desconforto relatado ao caminhar olhando o mouse permanece registrado, sem substituir rig/clips/artwork nesta atividade.

Menor follow-up recomendado: medir velocidade efetiva no espaço local do facing (`dot` com forward/right), definir regra de strafe/backpedal e transições com compromisso de ataque, conferir clips disponíveis e desenhar blend direcional somente após aprovação. Não corrigir moonwalking girando o personagem para WASD durante um ataque. A opção de dash cursor/WASD também continua pendente; o dash atual e seus valores foram preservados.

## Validação final executada

**102/102 casos distintos PASS**, zero ignorados; não é a suíte global.

| Bateria | Resultado / duração | Job MCP |
|---|---|---|
| Novos LMB, Arco, Spear, UI/hitstop e RMB Asura |19/19 ·19,862436s |`74fc13a0f30a481ebe6e5a340439bb0c` |
| Integração/movimento C01.2 |23/23 ·8,754867s |`d8786721fb814feba945e93fff8820d1` |
| UI/results/reload/pausa |7/7 ·3,565856s |`2704c2c356d4436ca9aaa5e81948ad42` |
| GI/legado/Asura/Bow/Spear/recovery |30/30 ·23,741416s |`bc1d6be6defa460987ab35809f787aab` |
| Dash/cancel/imunidade/cleanup |8/8 ·1,852647s |`bf16eff1e41b43ce9980383195a0e245` |
| EditMode identidade/finalização/bindings |15/15 ·0,039555s |`043ed5373ff74f11bb99f4d7dcb70d24` |

Arquivos em [c01-3a](../validation/artifacts/gameplay-rework/c01-3a): `new-19-final.json`, `movement-regression-final.json`, `contexts-final.json`, `combat-regression-final.json`, `dash-final.json`, `edit-final.json`, seus `.request.json` e `.start.json`. Runner reproduzível: `python Tools/GameplayObservation/c01_2c_validation.py <request.json> <resultado.json>`, serviço MCP no Editor correto, um job por vez. SessionState `TechGuy.Tests.GprPhase=c01-3a` separa os outputs das regressões de evidência histórica; removido no fechamento.

Os 19 novos casos provam: hold comum não repete; vazio executa sem target/path; cliques avançam0/1/2 respeitando cadência; contato real acerta um ator lateral à posição do cursor e erra o lado oposto, uma confirmação; direção da pose comprometida; último pedido/expiração sem combo automático; WASD não vira destino de mouse; pausa/foco/disable e UI sem replay; swap e skill descartam pendentes; Arco normal vs ChargedShot; Spear e alcance; hitstop conserva um tap; RMB custa uma vez inclusive com F11/LMB simultâneos, não dá básico sem energia, não vaza de UI, continua Cadenciada por edges e respeita F.

### Iterações, problemas e correções

- Compilação inicial de teste tentou usar Newtonsoft sem referência no assembly de PlayMode; substituído por JsonUtility, sem alterar dependências de assembly. Nova propriedade Asura usou inicialmente nome errado de enum; corrigido para RunWeaponFamily. Logs de ambos arquivados. Uma tentativa de job15 não inicializou durante o erro: **não conta como teste executado/passado**; cancelado o runner próprio para liberar importação.
- Primeiras baterias LMB tiveram7/12 e1/12: os dispositivos físicos competiam com os virtuais e havia roteamento de foco do Editor. A fixture agora desabilita os dispositivos antigos apenas no Input System durante o scope, usa política de background de teste já empregada em testes existentes e restaura tudo no Dispose. Asserts de produção não foram afrouxadas. Diagnóstico2/2 e depois15/15 passaram.
- Ao acrescentar quatro casos,18/19: o helper do clique esperava25ms **escalados**, atravessando o hitstop antes de verificar a intenção. Passou a25ms **reais**, adequados à duração física de um clique; asserções de zero ataques durante stop e um após retomada preservadas. Final19/19.
- Cliente HTTP de evidência usava id JSON-RPC fixo em chamadas concorrentes da mesma sessão; recebeu IDs únicos para não perder respostas SSE. Sessão HTTP expirada foi renovada sem reiniciar servidor. MCP nativo também foi utilizado diretamente.
- Incidente histórico de recovery de C01.2c permanece investigado, sem declaração de correção definitiva: TTL/hitch não foi alterado em C01.3a. O grupo atual30 passou; isso não encerra a causa histórica.

## Game View e observações estruturadas

Jogador publicado **Player - Nexus**, NexusBlockout. Driver em `Tools/GameplayObservation/c01_3a_nexus_driver.cs.txt`, disparado via execute_code no Play Mode. **Input/foco virtuais; energia100 concedida somente em memória para exercitar Asura; nenhum playtest físico ou concessão persistida.**

**1372 amostras, cerca de5,15s:** três básicos com índices0/1/2, um AbilityUsed de Asura, direção capturada X≈1/Z≈0. X permaneceu6,5 e Z−6,8 durante toda a observação: nenhum click-to-move. Intervalo e busy observados0,2s, sem tuning. Mana inicial1500, mínimo1353 para custo147; regeneração normal permaneceu ativa e o valor final não deve ser usado como custo líquido. Custo único também foi validado em fixture sem regen. `nexus-proveniencia.json`, `nexus-input.csv`, `nexus-measurements.json`, `combo-observation.json` e `runtime-info.json` preservam detalhes.

Capturas reais **1160×600**, abertas e inspecionadas:

- [Primeiro clique](../validation/artifacts/gameplay-rework/c01-3a/nexus-basic-1.png).
- [Segundo clique](../validation/artifacts/gameplay-rework/c01-3a/nexus-basic-2.png).
- [Terceiro clique](../validation/artifacts/gameplay-rework/c01-3a/nexus-basic-3.png).
- [Asura no RMB](../validation/artifacts/gameplay-rework/c01-3a/nexus-asura-rmb.png).

São evidências do comportamento no jogador real, não aprovação humana de animação/arte/feel, nem prova de hits contra inimigos na Nexus vazia. A prova de contato/dedupe vem das fixtures reais de combate. Frame mediano observado3,506ms; máximo333,333ms na amostra de Editor com inicialização. Não é benchmark CPU/GC/build/crowd. UI raycast é consultado por evento relevante, reutilizando lista/PointerEventData; não há varredura de inimigos no novo LMB.

## Integridade, Console e arquivos

Console runtime/final consultada: **zero erros**, um aviso legado de keys incompletas. Compilações reais e testes Unity concluídos; nenhum comando Clear foi usado. Erros intermediários estão arquivados, sem serem apagados do relato. Checker genérico da skill executado: **FALHOU**, exit1, incompatibilidade `os.path.relpath` entre pacote externo em C: e projeto em D:. Não conta como PASS; usada validação focada de importação/compilação/referências/GUIDs, testes e hashes.

Inventário protegido inicial: **1795 arquivos**, **1789 iguais**, **6 alterações intencionais**, **zero ausências**. Dois novos GUIDs de scripts gerados/importados no Unity, sem duplicação no inventário. Nenhuma cena, prefab, perfil, clip, controller, asset, GUID preexistente ou valor autorado foi alterado nesta atividade. As alterações antigas de mapas/arte/combate existentes no Git são anteriores e serão incluídas no commit por solicitação expressa; não atribuí-las a C01.3a.

`snapshot-before.json` e `snapshot-after.json` iguais: bindings legados `[323,324,32,113,119,101,292,102]`, moedas2597, resumo42387 caracteres/hash`9751067523EABB7557FFEC504779DB853925D6665AE876DAF8C0C489D52189B3`. Isso verifica esses campos persistentes, não um dump de todas as preferências. Perfil runtime preservou Skill4F11; apresentação do especial mostrouMouse1. Nenhum Apply de Settings. Editor final em **Edit Mode, NexusBlockout limpa**; dispositivos virtuais removidos e políticas de Input System restauradas.

Arquivos de gameplay modificados nesta atividade:

- `Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.cs`: leitura primária direta, guard de direção, descarte de pendente no cancel e separação do hold legado.
- **Novo** `Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.DirectBasicInput.cs` e `.meta`: cursor/press/UI/exceção de carga/ownership secundário.
- `Assets/_Project/Scripts/Abilities/AbilityHolder.cs`: uso do buffer compartilhado e adapter único de entrada Asura.
- `Assets/_Project/Scripts/Core/GamePreferences.cs`: fachada semântica do canal especial, sem novos índices/prefs.
- `Assets/_Project/Scripts/UI/ControlRemappingPanel.cs`: labels correspondentes ao comportamento atual, sem rebind completo.

Testes: **novo** `DirectBasicInputPlayModeTests.cs` e `.meta`; modificados `DirectMovementIntegrationPlayModeTests.cs` (isolamento/restauração de input e artefatos) e `BasicAttackR1MovementFluidityTests.cs` (destino dos artefatos, sem mudar TTL/asserts).

Ferramentas: novos `c01_3a_nexus_driver.cs.txt`, `c01_3a_audit.py`, `c01_3a_measurements.py`; `c01_2b_mcp.py` recebeu IDs RPC únicos. Docs: este relatório, autorização, evidências c01-3a e atualização de ledger, matriz, ADR-0006, requisitos/design/tarefas/plano de validação/migração. `.gitattributes` e hooks locais Git LFS preparados para salvar os arquivos Blender acumulados acima de100MiB; sem reescrever histórico. O push solicitado será confirmado na mensagem final com a referência efetiva.

## Skills encontradas e aplicadas

Locais reais `.agents/skills/`: **tech-guy-game-vision**, **tech-guy-combat-unity**, **tech-guy-boon-design**, **tech-guy-unity-acceptance**, SKILL.md/referências consultados. Aplicados visão e limites; cadência/impacto/reação/causa; identidade de Asura/boons/carga sem alterar catálogo; evidência Unity e separação de aceites.

Instaladas em `C:/Users/gabri/.codex/skills/`: **router** (engine já identificado, sem rerotear a cada turno), **unity-csharp-scripting**, **unity-input-system**, **input-systems**, **unity-animation**, **game-feel**, **unity-mcp-orchestrator** (pasta unity-mcp-skill), **game-ui-ux**. Aplicadas às APIs/lifecycle, press vs hold, buffer único, pose/contato, preservação de feedback, orquestração e ownership de UI. Orientações Unity6.3 adaptadas à versão real6.5, sem atualizar pacotes ou adicionar PlayerInput só por exemplo genérico. Não foi encontrada skill standalone de testes Unity na seleção instalada; a customizada de aceitação e o MCP guiaram a validação. Checker genérico incompatível registrado acima, não disfarçado como aprovado.

## Limitações, pendências e próximo recorte

Não há aceitação física final de C01.3a. Refinamento de walking/strafe/backpedal, configuração da direção do dash, ação Interact independente, slotsQERX/extração completa de Asura, remapeamento completo, especiais Bow/Spear, mutações GPR-01 e reworks de mapas/inimigos permanecem fora desta entrega.

RMB/F11 são compatibilidade de uma única Asura histórica; o canal especial herda a binding secundária existente. Não há segunda Asura em X. Nos outros kits, secundário/Shift continuam legados até design próprio. Confirmar fisicamente se a exceção de ChargedShot preservada satisfaz o produto; não assumir um contrato universal de disparo ao soltar.

**Proposta C01.3b, somente após revisão:** sessão física de clique/whiff/combo/aim oposto, Cadenciada/finisher e troca entre armas; regressões complementares de simultaneidade/reentrância/cancel, binding secundária personalizada, inimigos Common/Heavy/Elite/Boss, UI completa e segundo ciclo de run; observar performance em combate/crowd e reconciliar eventos/HP/estatísticas. Definir o recorte de extração modular antes de ocupar X. Pré-requisitos: revisão deste relatório e aprovação explícita. Testes obrigatórios de3a não autorizam declarar3b executada.

**STOP após GPR-C01.3a.** O fechamento Git e o desligamento solicitados não iniciam outra atividade de gameplay.
