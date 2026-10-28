# GPR-C01 — Design vigente de controles e migração

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



27/10/2026. **GPR-C01.3a concluída tecnicamente, aguardando revisão.** C01.2c aceita no escopo técnico. LMB direcional por clique implementado; hold do básico somente com ChargedShot por decisão explícita; Asura no RMB da Manopla incluída por autorização adicional.102/102 testes normais; aceite físico pendente. C01.3b/GPR-01 não iniciadas. [Relatório C01.3a](../decisions/GPR-C01.3a_RELATORIO.md).

## C01.3a vigente — autorização ampliada durante a execução

C01.2c foi aceita tecnicamente. C01.3a entregou LMB por clique/cursor sem target, e o usuário acrescentou Asura no RMB. Exceção aprovada: hold do básico preservado somente com ChargedShot. Sem o boon, Bow também é um pedido por clique. Ataques no vazio usam os mesmos executores/contato, sem seleção de inimigo.

Na Manopla, canal secundário/Mouse1 → especial Asura existente; F11 legado funciona como compatibilidade na mesma entrega/estado e nunca como Asura extra. Secundário+LMB prioriza especial; F+secundário prioriza finisher. Sem energia não cai para básico. HUD apresenta binding real, arrays/IDs/causaR intactos. O adapter reaproveita a binding de Move no perfil atual; rebind dedicado completo continua pendente.

Asura ainda é referenciada no index3 histórico para preservar boons/snapshots/timers. Não declarar quatro slots modulares separados ou X entregue. As propostas de extração abaixo continuam propostas onde não contempladas por esse adapter. Arco/Lança mantêm ações secundárias antigas, sem especial novo. Textos de fronteira anterior abaixo são históricos quando conflitam com este delta.

## C01.2c: ownership e decisões vigentes

Q/E/R continuam skills migradas; especial histórico index3/F11 e finisher configurado preservados. RMB/Shift+mouse mantêm comportamento legado; nenhum adapter Asura ou novo básico foi criado.

**Interact separado é design, sem binding definitiva.** Intenção com alvo contextual, sequência e geração do contexto; resolver ownership antes da entrega ao holder. Prioridade UI modal → interação válida → gameplay, um consumidor por press. Adapter temporário usa binding histórica de Skill3 no lobby, com reserva por proximidade e barreira no close; não escolher G/F nem renumerar GameControl/slots. Implementar ação/configuração só em etapa explicitamente autorizada.

Feedback físico incorporado: LMB deve aceitar ataque sem target (C01.3 a revisar); dash deve oferecer escolha mouse/direção WASD em Settings; animação caminhada/mira precisa refino. Nada disso foi implementado em C01.2c. O default atual de dash permanece cursor até decisão de implementação.

## Implementado agora versus alvo final

Motor protegido integrado em4 cenas, WASD relativo à câmera/mira independente, mouse sem destinos, dash cursor. Teclas de três skills herdadas Q/E/R; especial legadoF11 e finisherF no baseline. Perfil versionado no GamePreferences/Settings existente conserva as chaves antigas, corrige conflitos WASD e permite rollback. Os índices/causas históricos continuam intactos. Interação contextual acompanha Skill3/R até ser separada.

LMB/hold seguem comportamento legado; RMB não ganhou Asura. X sem conteúdo e sem alias do especial. WASD ainda fixas/reservadas; remap completo, secundários e contextos UI/reward/results ficam para os recortes seguintes. Na Nexus, diagonal contra borda parou; rigidez e percurso real exigem avaliação física.66/66 finais não equivalem a aprovação física. Abaixo permanece o desenho das etapas completas, não uma alegação de que já foram implementadas.

## Aprovado versus por decidir

Aprovado: WASD direto, mira independente no cursor, Space dash; um clique deliberadoLMB pede um básico; cliques avançam a sequência da arma; RMB aciona o especial da arma, Asura na Manopla; quatro slots modulares Q/E/R/X; preservar compromisso/cancel/impacto/reação/cleanup; Settings remapeável e input seguro. F contextual está provisoriamente preservado; Esc mantido. Nenhum novo binding foi aplicado.

Não aprovado: repetição automática por hold como política final, InteractG/F/V, conteúdo inicial de X após tirar Asura do loadout, semânticas RMB de Bow/Spear, direção de dash, referencial câmera/mundo, motor direto definitivo e implementação do backend de rebind. VarreduraRMB1,25×/0,8× e alias de básicoBow/Spear foram SUPERADOS, sem protótipo ou implementação.

## Fronteira atual comprovada

GamePreferences usa enum Primary0/Move1/Dash2/Skill1..4=3..6/FinishSequence7, KeyCode persistido e polling do Input System. Não há .inputactions de jogo localizado; activeInputHandler=Both. Sessão atual: Mouse0/Mouse1/Space/Q/W/E/F11/F. A mesma preferênciaSkill4 é global entre armas: F11 não pode ser migrado cegamente como se fosse universalmente Asura.

CharControlScript usa raycast/WorldClickResolver, destino de chão por LMB ouRMB, seleção/autoaproximação e BasicAttackDriver tap/hold. GroundMovementBuffer guarda destino de recovery, não vetor. AbilityHolder agrega gates de mundo/menu/reward; executores assumem agente/rotação durante casts. Camera TG_TopDown_Camera acompanha player com yaw configurável; não trocar por Cinemachine pelo pacote estar instalado. SkillAnimationPlayer toma e restaura pose/root motion.

Baseline: agente5 m/s, aceleração99999, root motionfalse no Nexus; navegação por API antiga~5 m/s planar cardinal/diagonal. Agent.Move futuro não foi exercitado. Dash autorado20m/s durante0,25s, sem custo nominal; deslocamento real limitado pelo mundo. Esses valores descrevem o atual, não aprovam rebalanceamento.

## Matriz de ações, identidades e bindings-alvo

| Ação | Atual/owner | Alvo aprovado ou proposta | Migração e limite |
|---|---|---|---|
| MoveVector | Clique/CharControlScript/NavMeshAgent | WASD aprovado; câmera relativa proposta | Eliminar destinos de ambos os botões e autoaproximação; motor ainda hipótese |
| Aim | Cursor/raycast/controlador e executores | Cursor independente aprovado | Sem cursor lock; manter direção capturada após commit |
| PrimaryBasic | Primary/BasicAttackDriver/BasicImpact | CliqueLMB =um pedido aprovado | Hold não autoriza repeats por padrão; preservar canal release/carga explicitamente |
| WeaponSpecial | Asura no holder/index3 e BreakerGauntletCombat | RMB aprovado para Asura | Não usar básico, quinto slot ou duplicar executor/estado |
| Skill1/index0 | Q/Avanço | Q aprovado | Mesma habilidade/custo/causa; swap só em GPR-01 autorizado |
| Skill2/index1 | W/Punhos | E aprovado | W passa a direção, sem consumir slot1 junto |
| Skill3/index2 | E/Choque e interação | R aprovado | Choque/Stagger preservado, Interact separado |
| Skill4/index3 modular | Atualmente contém Asura | X aprovado como slot modular | Conteúdo inicial pendenteD15; X não pode duplicar AsuraRMB |
| Dash | Space/Holder/DashScript, mira cursor | Space aprovado; direçãoD18 | Preservar disponibilidade, cancel, i-frames e duração |
| FinishSequence | F/holder/Cadenciada ou órbita | F provisório | Prioridade atual sobre continuação simultânea preservada |
| Interact | Skill3/E e Skill4 em arsenal compacto | Ação própria; tecla pendente | MostrarG e alternativaF/V para decisão, sem aplicar; inspeção compacta por UI/ação própria |
| Pause/Back | Esc/Pause/UI | Esc mantido | Reservado para recuperação/cancelar captura |
| UI Navigate/Submit/Cancel | Fluxos UI existentes | Map/contextoUI exclusivo | Um EventSystem, sem click-through ou duplicação com gameplay |

Ações novas devem ser anexadas/mapeadas por ID estável, nunca inseridas no meio de enum serializado. Binding físico não é identidade de habilidade nem bucket de dano. Q/W/E/R históricos permanecem origem de analytics/boons quando correspondem às ações antigas; o HUD mostra o binding real.

## Um clique, um pedido de básico

Usar um único leitor de PrimaryBasic (edge da ação), que enfileira no máximo uma intenção no BasicAttackDriver existente. Não registrar simultaneamente callbackperformed, polling eOnClick para o mesmo press. A cada novo clique legal, avançar somente um golpe da sequência da arma; nunca executar a sequência inteira. Clique durante busy/cadência segue capacidade/expiração do buffer atual: no máximo um pendente, sem acumular cliques infinitamente nem alargar janelas. Registrar aceito/retido/recusado para diagnóstico; um pedido não garante execução em estado inválido.

Capturar aim/contexto na fronteira já usada pelo commit. Cancelar/intenção inválida/morte/disable perdem pendentes pelo owner existente. Soltar botão não revoga HP comprometido; manter scheduler/ImpactEvent, pose no contato, recuperação, custos, hitstop e reação. Não autoaproximar nem selecionar outro alvo por vizinhança. Teclas mantidas/repeat do SO não produzem novoedge. Combos continuam arma-específicos e sem novo clock de ataque.

**Hold atual:** existe com alvo/Primary e com Shift direcional. No alvo, BasicAttackDriver recebe SetHold; a carga ChargedShot do arco compartilha o sinal mantido. Padrão clique aprovado não permite preservar repeats implicitamente. Recomenda-se um modo legado opt-in separado de clique, somente se aprovado, sem misturar listeners; até C01.3 nada muda. Para ChargedShot, decidir explicitamente entre compatibilidade opt-in ou nova semântica de carga por press/release com teste autoral. Não desabilitar boon/arma, mover sua carga paraRMB ou habilitar hold automaticamente para esconder o conflito.

## RMB → Asura existente: adaptação mínima e acoplamentos

Asura é hoje a própria Ability emWeaponScript.abilities[3]. Holder controla preflight, mana/Active/Cooldown, buffer de continuação e F. BreakerGauntletCombat usa Array.IndexOf para slot/snapshot/causa, GauntletEnergyPolicy consome100 e recebe confirmações deduplicadas. BeginAction/SourceFor/Mutations e UI tratam R como identidade antiga. Remover referência do array sem resolver identidade pode produzir-1/Basic; chamar TryActivate direto pula gates/cooldown do holder; copiar Asura para X e RMB duplica caminhos. Essas três abordagens são rejeitadas.

Proposta de compatibilidade **a aprovar emD15**, antes de C01.3:

1. O mesmo AbilityHolder resolve a única referência legadaAsura (AsuraBurst/index3) como especial da Manopla, sem criar outro executor. Separar a visão runtime dos quatro slots modulares do local autorado legado. Não mutar asset compartilhado; RunBoons continua dono de clones.
2. Mover ownership do estado/cooldown da Asura para um único registro do especial **dentro do holder**, reutilizando o preflight/tick e o mesmo executor/contexto. Não criar outro timer concorrente; a duração de execução continua no scheduler atual. Views de slots e especial não podem apontar para dois estados mutáveis da mesma execução.
3. Manter referência autorada e identidade históricaR enquanto necessário à compatibilidade de snapshot/boons; resolver ID da ação explicitamente, não pelo binding físico ou posição nova do HUD. Views modulares podem ter quatro entradas comX vazio como proposta inicial; preencherX depois requer referência/identidade/capabilities próprias e autorização de conteúdo, sem roubar Asura.
4. RMB edge passa pelo holder e ativa a Asura existente se pronta. Durante Cadenciada, novoedge RMB chama continuação pelo mesmo execution token, custo único; holdRMB não continua. Preservar capacidade/expiração do buffer existente, F antecipado e sua prioridade no mesmo frame, resistência/proteção e cancel. Novo input nunca renova energia ou paga cooldown duas vezes.
5. Atualizar GetGauntletRUnavailability/ActiveAbilities/timers/HUD/observação para usar a referência/estado correto; barraAsura existente mostraRMB e disponibilidade do especial, barQERX mantém quatro slots. Não criar quinto slot modular. R histórico na telemetria passa a ser apresentado comoAsura/Especial quando identificável, sem renomear saves ou inventar histórico ausente.
6. Boons ligados à Asura antiga continuam mirando sua identidade/capabilities, não o novoX apenas porque também tem índice3. Atualizar resolução/prompt de alvo quando necessário, sem habilitar conteúdo ou criar outro grafo. Asura continua pertencendo ao mesmo family/inventário/raiz; nenhum novo schema de RunSummary está autorizado.

A alternativa de RMB chamar o slot3 eX ser uma segunda tecla da mesma skill é incompatível com o pedido. Alternativa de preservar tudo até definir uma quarta habilidade mantém segurança, mas adia a migração da Manopla. Recomenda-seX vazio inicial para evitar inventar kit em C01; **essa proposta não foi aprovada**. O design final de quatro skills modulares continua objetivo posterior.

## Movimento, mira, facing e compromisso

Recomendação: prototipar primeiro NavMeshAgent como motor único, usando o mesmo mundo/radius/speed/stat/cast multiplier. A medição de pathfinding antigo não prova feel de movimento direto. Primeira tarefaC01.2a, se autorizada, é fixture isolada com cantos, corredor, colisão, borda, obstáculo/avoidance, desnível, root motion, dash, pull e attackroot. Não integrar no player publicado antes do gate.

Vetor2D deve anular opostos e normalizar diagonais. Referencial proposto é right/forward da câmera projetados noXZ; world-relative é alternativa a comparar fisicamente. Vetor degenerado/sem câmera retorna direção válida anterior ouzero seguro, sem NaN. Após aprovação, um só coordenador converte vetor em deslocamento limitado contra NavMesh e sólidos. SamplePosition sozinho não garante parede; validar segmento/sweep do corpo, impedir atravessar espessura/canto e não usar Warp de rotina. Não coexistirSetDestination de clique comMove direto ou com Rigidbody/CharacterController escrevendo posição.

Ordem de autoridade: morte/disable → deslocamento externo/CC → dash → cast com controle próprio → locomoção livre. Hitstop/pausa mantêm clocks existentes; melee rooted não ganha kiting. GroundMovementBuffer antigo não deve reproduzir um destino depois de soltarWASD. Retomar direção somente se ainda ativa e permitida; buffer ofensivo segue prioridade/cancel atuais. Animação usa deslocamento realizado, pois agent.velocity pode não refletirMove/dash; CurrentMoveDirection/KitingStep precisa a mesma fonte verdadeira, sem outro controlador.

Facing livre usa mira válida independentemente do andar; ação comprometida preserva direção/captura até janela atual permitir alteração. Executores continuam donos de rotação/root e restauram flags. Sem homing ou ajuste de cadência para mascarar dificuldade de input.

**Critério do motor:** não atravessar/teleportar/sair do NavMesh, não acelerar diagonal, sem jitter em contato/soltura, um writer de posição por fase, roots/i-frames/cancel intactos e avaliação humana positiva contra a baseline. Tolerâncias numéricas derivadas da resolução do fixture; desempenho/feel não inferidos de inspector. Se Agent.Move falhar: primeiro verificar projeção/colisão/arbitragem; se insuficiente, comparar CharacterController como owner único e agente apenas de consulta. Essa alternativa tem custo de migrar dash/pull/CC/animação e exige revisão; nunca sobrepor dois motores. Rigidbody é terceira opção se física for requisito demonstrado, não default do recorte.

DashSpace é aprovado; direção aindaD18. Recomenda-se inicialmente manter mira/cursor para reduzir mudança de contrato; alternativa usaWASD ativo→mira→facing. Comparar dois protótipos isolados idênticos em duração/i-frames/cancel/colisão e coletar preferência física; não aplicar a alternativa por conta própria.

## Bow e Spear

Preservar básicos, projéteis, geometria e skills existentes; regressão compartilhada obrigatória. Bow tem carga/marca/minas em boons e cast móvel nativo; Spear tem sweetspot, linhas e órbita/retorno condicionais. Essas capacidades não comprovam um especial universal já disponível. Não mover R/charge/órbita paraRMB nem repetir básico como alias. Definir o comportamento específico emD17, sem criar kit em C01.1. Conteúdo de RMB dessas armas pode ficar indisponível com feedback explícito apenas se o desenvolvedor aprovar uma transição; não anunciar o esquema de três armas como final sem esse gate. F conserva retorno elegível, sem virar interação.

## Settings e migração de preferências

Recomenda-se Input Actions/Gameplay+UI com GamePreferences como facade única e ControlRemappingPanel existente ampliado. Polling legado ou callbacks podem ser estratégia de consumo, mas somente um leitor gera comando. Não adicionar PlayerInput/EventSystem concorrentes. ActiveInputHandlingBoth continua até auditoria de fallbacks/terceiros; nenhum pacote atualizado.

Antes do backend: pilotoEditMode em memória, lendo cópia das preferências reais e defaults, gerando draft/actionIDs e testando round-trip de overrides, reset, dados ausentes/corrompidos e conflitos. Nenhuma gravação no PlayerPrefs humano durante o piloto. Esse piloto é tarefa futura de C01.4a, não Settings implementado em C01.1.

Esquema versionado somente de controles; manter chaves antigas/IDs para diagnóstico e rollback. Instalação nova recebe WASD/LMB/RMB/QERX/Space/Esc e auxiliares somente após decisão. Perfil existente recebe preview antes/depois e conflitos: W antigoSkill2 versusMoveUp; E/R antigos versusQERX; Mouse1Move versusSpecial; F especial contextual versusInteract; F11 antigoSkill4 agoraAsura naManopla mas skill4 normal emBow/Spear. Não inferir intenção global pelo equipamento atual ou copiarF11 simultaneamente para X e RMB. Aplicação exige escolha explícita de owner/alias compatível; sem escrita automática ou perda silenciosa. Até confirmação, preservar perfil/control route antiga ou impedir entrada na migração por gate explícito acordado, sem aplicar metade do esquema.

Painel: primário e alternativo opcional por ação, partesWASD remapeáveis individualmente, captura de Button elegível e cancelEsc. Não capturar posição/delta/scroll para atacar; validar mousebuttons/keylayout/reservadas/teclas impossíveis; preservar pelo menos um acesso a ações essenciais/menu. Duplicatas em contexto simultâneo são recusadas com nome da ação; compartilhamentoUI exclusivo é permitido. Não trocar outra ação silenciosamente. Draft/Apply/Cancel/Reset separados, overrides validados e labels atuais por evento; teste720p/1080p e teclado/mouse. Dispose da captura em cancel/disable/fechamento.

Menus/rewards/resultados/pedestais/typing/rebind/transição têm ownership antes de comandos. Abrir desativa input do mundo e zera vetor/hold não comprometido; fechar consome press de confirmação e exige soltura dos comandos mantidos antes de rearmar, incluindo movimentos. Perda de foco/device remove intenção presa. Um só EventSystem; nenhum cast/ataque/custo por clique emUI. Gamepad integral não foi comprovado e não é novo escopo; preservar consumidores já existentes.

## Gate e escopo mínimo seguinte

[Prontidão histórica](../decisions/GPR-C01.1_BASELINE_E_PRONTIDAO.md). C01.2a foi aceita para implementação e C01.2b entregue com autorização adicional das teclas necessárias. [Relatório vigente](../decisions/GPR-C01.2b_RELATORIO.md). Próxima proposta: avaliação física e C01.2c; Asura/X/especiais Bow-Spear, interação própria, LMB/hold e remap completo conservam seus gates. GPR-01 não iniciada. Binding mínimo foi integrado; nenhum slot modular, Mutation ou kit novo foi criado.
