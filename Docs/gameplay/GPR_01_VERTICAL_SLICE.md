# GPR-01 — primeira fatia da Manopla para revisão

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


## Estado vigente — A aceita; primeira composição A+B, 29/10/2026

O desenvolvedor aceitou fisicamente GPR-01.2b como funcional e divertida. A correção01.2c do feedback após concluir sala foi autorizada antes de01.3 e passou32/32 antes da composição. [Relatório01.2c](../decisions/GPR-01.2c_RELATORIO.md) / [relatório01.3](../decisions/GPR-01.3_RELATORIO.md).

| Build | Comportamento vigente |
|---|---|
| Base | Básico nativo, sem tornado novo. |
| A / Punhos Titânicos | Tornado móvel aceito, inclusive terminal em whiff; em Traversal após clear, somente vento cosmético. |
| B / Conductor | Protocol nativo preservado; não cria tornado. |
| A+B | Primeiro impacto efetivo de vento pode conduzir do inimigo atingido para até rankB vizinhos válidos. Uma emissão por tornado/raiz, quota32 existente, sem recursos ou recursão. |

A+B introduz interação verificável mesmo quando o soco terminal erra: o impacto tardio do vento produz condução onde B isolado não teria evento nativo. Whiff total, dano zero, imunidade, budget negado e vento cosmético não produzem arcos. Arcos exibidos correspondem a HP elétrico confirmado, com root/parent existentes. Nenhuma Detonation autorizada; grantsNone continuam default.

Velocidade/tamanho/mira e demais números de01.2b congelados nesta atividade. Backlog de cadência, animação direcional, rewards, fases/ritmo, meshes, elites/bosses e Singularity em [GPR_TASKS](GPR_TASKS.md), sem implementação autorizada.

**STOP após01.3 para revisão do desenvolvedor.** As seções antigas a seguir são registro histórico, inclusive gates anteriores e hipóteses da onda instantânea.

## Histórico — GPR-01.2b, 28/10/2026

O desenvolvedor testou fisicamente a onda instantânea anterior: funcionalidade técnica aceita, resultado de gameplay considerado pouco transformador e rejeitado como design final. A revisão autorizada substitui essa onda por um **tornado de vento móvel**, mantendo o ID da Mutation, aquisição, controles e combate nativo. A evidência de GPR-01.2 abaixo permanece histórica.

Cada terminal realmente executado no contato do scheduler, com A no snapshot aceito, autoriza no máximo um tornado **inclusive em whiff**. Pedido/cancelamento pré-contato e estágios intermediários não emitem. Contrato específico de emissão separado de `ReserveConfirmedBasicChild`, cujo requisito de HP confirmado permanece intacto. Emissão não produz HP/estatística/recurso nem nova raiz; impactos posteriores usam a fila e os receipts existentes, mesma raiz/parent e caps. Política do ramo: `ProtocolBasicEffects.None`.

Definição experimental única: deslocamento de 5,5 m a 3,5 m/s, vida máxima escalada de 2 s, raio 0,65 m, altura 1,8 m e offset inicial 0,75 m. Direção comprometida não acompanha mouse/rotação posterior. Dano = 0,60 × `CaptureAttackDamage` não crítico capturado na aceitação do terminal (dano authored + stats/Momentum elegíveis naquela aceitação); não depende de receipt, não sorteia crítico nem reaplica multiplicadores do atacante. Mitigação/HP seguem o alvo. Uma vítima por handle+generation, mantendo a exclusão das vítimas nativas já confirmadas.

Overlap inicial + capsule sweep por passo; sólidos e LOS bloqueiam. Reservas negadas/saturação encerram com segurança. Pausa/hitstop congelam; troca de arma/contexto, fim da run, disable/morte e cena invalidam/limpam reservas e apresentação. VFX reversível, pool limitado e leases por geração, sem Rigidbody/segundo executor/alteração permanente de prefab.

Implementação e validação desta revisão registradas no [relatório GPR-01.2b](../decisions/GPR-01.2b_RELATORIO.md). **Aceite físico do tornado novo pendente. STOP antes de GPR-01.3**: somente recomendar revisão da composição com Conductor após avaliação do desenvolvedor; nenhum grant novo de Conductor/Detonation, swap modular, tuning nativo ou quarta skill implementado.

### Fechamento técnico da GPR-01.2b

**CONCLUÍDA TECNICAMENTE NO ESCOPO TESTADO; HUMANO PENDENTE.**29/29 focados (28 da Mutation+1 interação),90/90 EditMode e187/187 regressões:305 nomes distintos finais, zero ignorados; não suíte global. Focados joba0ca7eec1c9f48668c0bcf4601e6b9ed; EditMode4b30bf2757f3424eb46e2d268dd40f82; regressão02090688af2f415ea00dbd3b27cb386f. Falhas intermediárias, correções de fixtures e intermitências históricas registradas no relatório, sem relaxar asserts/clocks.

- [x] Separar contato terminal/whiff de receipt positivo sem relaxar reserva confirmada global.
- [x] Tornado progressivo, direção capturada, pacote não crítico estável, sweep/LOS, caps/dedupe e cleanup.
- [x] Pool/VFX reversíveis; captura Game View em6 quadros eCSV; aquisição por ticket preservada.
- [x] Regressões de controles/GI/armas e integridade de assets/evidência histórica no alcance declarado.
- [ ] Revisão física do tornado pelo desenvolvedor; sensação/arte/balanceamento e carga em swarms pendentes.
- [ ] Autorizar separadamente GPR-01.3 antes de compor Conductor; nenhuma composição nova habilitada.

## Histórico — estado GPR-01.2 anterior à revisão

### Registro histórico — GPR-01.2, 28/10/2026

GPR-01.1 aceita tecnicamente pelo desenvolvedor; GPR-C01 continua encerrada. Autorizada somente a primeira Mutation jogável **Punhos Titânicos**. Implementados catálogo Basic da Manopla, onda na raiz nativa com HP confirmado e grants `ProtocolBasicEffects.None`, sobrepeça reversível e testes focados. Implementação concluída com ressalva de regressão:18/18 focados e90/90 EditMode;187/187 na última bateria ampliada.295 casos distintos finais, com ocorrências intermitentes anteriores de hitstop/burn registradas e não declaradas corrigidas. Zero ignorados; não confundir automação com aceite humano. [Relatório da atividade](../decisions/GPR-01.2_RELATORIO.md).

Parâmetros experimentais: caixa frontal de 1,5× o alcance/largura efetivos do soco, altura preservada; 0,60× o maior dano pré-mitigação entre receipts nativos positivos do terminal, coeficiente aplicado uma vez. Sem tuning do básico/Asura/QER, nova ação, recurso, postura ou grant Conductor/Detonation. Nova aquisição usa os tickets/catálogos existentes; nenhuma política de recompensa ampliada.

**STOP após GPR-01.2. GPR-01.3 e etapas seguintes não autorizadas.** Revisão humana da mutação e de seu visual permanece pendente; moonwalk/cadência e swap modular continuam diferidos. As seções anteriores preservadas abaixo são históricas onde divergirem deste estado.



## Proposta histórica antes da autorização01.2

28/10/2026 · **DIREÇÃO EXPERIMENTAL v1 ACEITA / GPR-01.1 AUTORIZADA / CONTEÚDO A NÃO IMPLEMENTADO.**
Pré-requisito cumprido: [GPR-C01 completa](../decisions/GPR-C01_PHASE_SUMMARY.md). A direção e os parâmetros iniciais de protótipo foram aceitos pelo desenvolvedor, sem aprovação final de balanceamento. Somente a fundação01.1 foi autorizada; conteúdo/visual A exige revisão e autorização01.2.

## Objetivo e alteração de escopo

Demonstrar base→A→A+B→A+B+C com uma única Mutation nova e dois Protocols existentes. LMB básico, RMB especial permanente, três modulares Q/E/R; nenhuma X. Kit sem boons e Asura permanecem como aprovados.

O pedido atual antecipa uma sinergia de três vias, antes prevista para GPR-02. Para minimizar o recorte, propomos **adiar o swap Avanço→Uppercut**, antes obrigatório em GPR-01: a integração solicitada será com Q/Avanço já existente. Essa redução foi explicitamente aprovada, sem remover modularidade da visão. RF-TROCA continua NÃO VALIDADO em GPR-03.1. Não ampliar simultaneamente o sistema de trocas, nem implementar ecos/múltiplas builds de GPR-02/03.

## Conteúdo e decisão nova

| Camada | Comportamento proposto | Decisão que muda |
|---|---|---|
| Base | Combo LMB e Q/E/R/RMB atuais | Aproximar, mirar e escolher a janela de compromisso |
| A — Punhos Titânicos | Só terminal do combo com HP nativo confirmado libera uma onda frontal curta; amplia geometria para vítimas fora do soco | Alinhar um alvo em contato e outro na extensão lateral/frontal; whiff não ativa |
| B — Bobina encadeada | Conductor existente, ID conductor; onda recebe permissão específica para uma emissão elétrica adicional por terminal | Escolher alvo da onda para alcançar um inimigo separado, mantendo distância/LOS reais do Protocol |
| C — Reator de sucata | Detonation existente, ID detonation; morte confirmada na nova ramificação produz uma explosão local limitada | Priorizar inimigo fraco perto do grupo para converter uma cadeia linear em área centrada na vítima morta |

C funciona sem A/B pelas regras já existentes do Protocol. A funciona sozinha. B+C também preservam seus comportamentos nativos. A+B+C não é apenas bônus percentual: muda a origem final de dano de frente do jogador para a posição da vítima morta. Sem morte, C não explode; contra boss isolado não inventa compensação de DPS/CC. Mistos com adds permitem decisão tática. Não garantir a mesma build em toda seed.

**Exemplo de prova:** Q nativo aproxima do grupo; terminal LMB confirma HP em T0; onda atinge T1 fora da caixa do soco; Conductor alcança T2; se T2 morre, Detonation alcança T3 próximo de T2, fora do soco/onda. Sem C, T3 não recebe essa explosão. Q também preserva os Protocols que já suporta, sem alterar seu executor ou custo. Esse percurso é cenário planejado, não observado. A prova deve controlar HP/posição e comparar também B+C sem A, isolando emissões nativas: T3 não pode já estar sendo alcançado por uma cascata nativa independente. Caso isso ocorra, reprojetar o cenário, sem apresentar o efeito existente como sinergia nova.

## Contrato de A — números de protótipo para aprovação

- ID candidato mutation_gauntlet_titan_fists; novo, ainda sem asset/registro/reserva. Uma Mutation no alvo Basic, rank1 nesta fatia.
- Usa o terminal real reconhecido por BasicFinisher, sem forçar sequência fixa ou novo botão. Snapshot no commit: build, dano, estágio, arma e dimensões; pose de emissão no contato válido, seguindo a direção comprometida existente.
- Uma caixa frontal instantânea, origem do impacto nativo; alcance e largura1,5× os valores efetivos do soco, altura inalterada. Não é projétil, dash ou zona persistente.
- Cada vítima nova recebe0,60× o dano básico capturado. Não repetir dano em vítima já atingida pelo contato nativo desse terminal. Valores são hipótese de conteúdo novo, não tuning do básico.
- Um disparo por execução terminal, mesmo com múltiplos alvos/colliders/notificações. Nenhum disparo por overlap sem receipt de HP, por postura isolada, whiff ou alvo imune.
- Sem mana/cooldown extra nem alteração de cadência, recovery, animação/contact fraction, cancel, hitstop ou recursos base.
- Onda sem contribuição de postura/CC adicional nesta fatia. Não herda Launch/push/stun do soco; reações de dano e resistência continuam sob autoridade do alvo. Soco nativo preserva todas as reações atuais.

## Causalidade e limites da ramificação nova

A geração parte de HP confirmado do terminal e reserva filho no contexto/raiz existente antes de query/dano. O terminal mantém seu ganho nativo de energia; onda, eletricidade e explosão novas não geram mana/Asura, não simulam skills nem criam raízes novas.

Na ramificação criada por A: no máximo uma onda, uma emissão de Conductor e uma de Detonation por execução terminal, além dos caps compartilhados já existentes. Transições permitidas: onda→Conductor; morte da onda ou de seu Conductor→primeira Detonation. Conductor não dispara outro Conductor; Detonation não realimenta nenhuma emissão. A primeira reserva válida de explosão consome a única quotaC desse ramo, em ordem determinística. Múltiplas mortes não repetem C. Reserva negada implica zero HP/postura/efeito mecânico; apresentação só acompanha emissão admitida.

**Divergência encontrada no código:** ProtocolEligibility.Conducts aceita também Conductor/Detonation; a execução nativa já admite cascatas limitadas. Não é verdade que o runtime atual proíba toda recursão elétrica. A proposta exige uma restrição de elegibilidade **somente na linhagem nova de A**, com propagação explícita dessa política aos descendentes, dentro de ProtocolRuntime/ProtocolRootContext. Não mudar a política global, reduzir o comportamento nativo de B/C ou usar Technique como permissão irrestrita para Ignite/Frost/Overflow.

O novo ramo autoriza só Conductor e Detonation; demais Protocols não são herdados automaticamente por A. Ranks/radius/fatores/dedupe/LOS de B/C continuam do catálogo efetivo. Budget e clocks não são aumentados. Emissão nativa de B/C no soco/Q continua independente e contabilizada como já existe; o limite “uma adicional” não deve apagar nem duplicar a emissão nativa.

Alvos por handle+generation, ordem determinística, dedupe por impacto/ramo, sessão/encontro vivos, LOS e sólidos. Reentrância de morte/troca/disable cancela o trabalho ainda não comprometido; HP já confirmado continua registrado uma vez. Sem forçar movimento ou bypass de imunidade.

## Diagnóstico histórico P1 — antes da fundação01.1

| Seam real | Reutilizar | Extensão necessária / risco |
|---|---|---|
| CharControlScript.BasicImpact / ActionImpactScheduler / ExecutionImpactLedger | Commit, terminal, impacto e cancel atuais | Snapshot de A e ligação a receipt; não usar BasicAttackPerformed/ComboNova como confirmação |
| PlayerActor.TryApplyGauntletAreaDamage / GauntletImpactTransaction | Resolução de HP, geração, LOS e notificações | Obter vítimas/HP confirmados para autorizar onda uma vez; evitar callback cosmético causando dano |
| BreakerGauntletCombat / GauntletImpactContext / GauntletEnergyPolicy | Owner de execução e recursos | Filho sem energia e com cleanup no owner atual; sem segundo executor |
| ProtocolRuntime.Scope / Damage / ProtocolBudget | Reserva de filho, queue, cap, impacto confirmado | Política limitada por linhagem de A; não chamar BeginExecution/BeginStandalone para renovar budget |
| BoonDefinition / BoonCapabilityGraph / BoonOfferCompatibility | Inventário e validação de capabilities | Grafo espera4 identidades Q/W/E/R; Basic=-1 existe no combate, não no BoonSlot atual |
| ManualRewardCatalog / RunBoons.Manual / tickets | RNG, oportunidade, revalidação, clones | Incluir A como Mutation Basic e em snapshots/keys/summary; não trocar oportunidade por concessão extra |
| RunBoons.CompleteOffer / AbilityHolder.RefreshLoadout | Owners atuais | RefreshLoadout global recria timers: aquisição de A/B/C sem troca de skill deve preservar timers/estados dos três slots e especial; delta limitado e testado, não implementar swap genérico |
| RunStatistics / RunDamageSource | Receipts de HP e identidade Basic | Diferenciar produtores sem contar total duas vezes ou renomear fontes pelo binding |
| Rig/WeaponLoadout/apresentação local | Ancoragem e ciclo de vida existentes | Uma peça visual própria da run, sem escalar Transform de gameplay |

**Desenho auditado e implementado na fundação01.1:** anexar BoonSlot.Basic=4 e BoonSlotMask.Basic=16, sem renumerar Q/W/E/R nem GauntletActionSlot.Basic=-1. Mapear os dois explicitamente; nunca castar -1 para array. Preservar Any=15 legado; autorização a Basic deve ser explícita, não expandir silenciosamente todos os boons universais. Grafo/adaptadores aceitam a configuração opcional Basic da Manopla e mantêm chamadas/snapshots antigos de4 alvos. Basic é alvo de modificação do LMB, não slot de cast no HUD. Uma Mutation por alvo, dois Augments por habilidade e duas Resonances por run permanecem; index3/R histórico continua especial e não é substituível por oferta modular. Provar restore antigo/novo e aquisições inválidas antes de publicar conteúdo.

A extensão coube sem migração de assets e foi validada com configurações antigas, restore atômico e save/unload/reload de catálogo temporário de teste. Não foi criado catálogo ou inventário paralelo. Não criar catálogo, inventário, scheduler ou pipeline de dano paralelo.

## Oferta, percurso e apresentação

A usa ticket Mutation compatível e deve contar para o gate da primeira Mutation. B e C usam tickets Protocol existentes, sem aumentar recompensas, mudar pesos ou forçar drops. Validar que uma seed/rota representativa oferece as três oportunidades antes do boss; se não existir, **parar e apresentar delta de oferta**, sem fabricar uma prova por grant de debug.

Provar todas as seis ordens de aquisição para A/B/C quando houver tickets legais, e inventário final equivalente. Sem suporte/slot inválido oferecido. A capacidade de modificar Basic deve constar do card como “Ataque básico — LMB”, não Q/R/X. Cards de B/C distinguem seus efeitos existentes e o limite específico da onda; C explica condição de morte e ausência de explosão em boss isolado.

Manifestação mínima: uma sobrepeça angular na Manopla, visivelmente maior na câmera de gameplay, criada como attachment visual local da run; não escalar ossos/colliders/hitbox. Onda curta corresponde à geometria; B usa assinatura elétrica existente, C explosão existente. Sem rig novo, arte final ou geração de imagens agora. Comparar base/A/A+B/A+B+C/reset; peça some ao trocar arma, morrer/disable/pooling, encerrar run e iniciar outra. Reconciliação idempotente, sem materiais/objetos órfãos. Perigos hostis e HUD continuam legíveis.

## Critérios propostos para implementação futura

1. Base sem boons conserva timings/cancel/custos/Asura e reações; comparar somente contratos afetados, sem pedir novo aceite completo de C01.
2. A altera geometria em alvo que base não alcança; whiff/immune/parede/reentrância/derivado não criam onda, duplicação ou recursos.
3. A+B prova alvo adicional; A+B+C prova explosão em posição nova e diferença em relação a A+B. Mortes múltiplas/overkill/cap negado respeitam limites.
4. Todas as ordens, restore/IDs/limites e aquisição sem reset de cooldown, sem writes em assets; tickets/first Mutation/gate boss e draw determinísticos.
5. Common/Heavy/Elite/Boss mantêm controle/reações válidos; sem stun-lock novo. Pausa/hitstop, troca/morte/cena/fim de encontro/novo spawn não deixam filhos.
6. HP efetivo reconcilia total/subcategorias; root/producer/parent não confundem derivados com hits nativos; energia do básico nativo permanece correta.
7. Duas runs/retornos NexusBlockout: transformação e reset observados; snapshots humanos protegidos. Assistência/debug explicitamente separada de percurso real.
8. Perfis8/32/64 alvos para cascatas e legibilidade, conforme relevância, sem meta de frame budget inventada; saturação não autoriza dano sem reserva.
9. Unity MCP: compilação/Console, dados serializados com save/reload, testes focados, Play Mode, imagens comparáveis; aceite humano **somente da nova mutação/sinergia/visual**, depois de pronta.

Esses critérios descrevem a futura fatia completa. A fundação01.1 exercitou contratos e regressões, sem criar A/onda/visual ou provar economia A/B/C e sem aceite humano da mutação. Comparadores futuros obrigatórios: base, A, B+C, A+B, A+C, A+B+C e sem morte; isolar cascatas nativas. Não reutilizar o aceite de C01 como aceite da nova build.

## Fundação01.1 e próximo escopo

ContratoBasic foi anexado aos enums/validators/grafo e adaptadores, preservando Any15 e quatro referências autoradas. Receipts de HP real alimentam uma reserva por terminal, após o batch nativo e fora de callbacks reentrantes; a reserva mantém raiz/parent e orçamento existentes. Novos filhos não recebem mana/Asura/hooks nativos. O snapshot captura a identidadeBasic da build sem alterar casts em andamento.

A política nova é **None por padrão**. Somente autorização explícita na reserva permite Conductor e/ou Detonation; as fixtures técnicas usam essa permissão, não o player de produção. Isso permite implementar A sozinha em01.2 sem antecipar01.3/4. Fora desse ramo, cascatas nativas permanecem iguais. Aquisição preserva timers de referências inalteradas; troca explícita/equip mantém seu caminho e nenhum swap genérico foi implementado.

O token reservado pode manter a raiz após fechamento normal do produtor, como contratos de filhos existentes; Dispose revoga esse trabalho e troca/morte/invalidação de encontro/disable/cena encerram o estado pelo owner atual. O consumidor futuro não pode resolver HP durante pausa/hitstop ou com reserva negada. Sem mudar cancel/commit do básico aprovado.

**Escopo exato proposto de GPR-01.2, após aprovação:** registrar uma única MutationBasic, mutation_gauntlet_titan_fists, em autoria/runtime existentes; ligar snapshot/receipt/reserva ao terminal; caixa frontal instantânea1,5× alcance/largura e0,60× dano capturado em vítimas novas, sem postura/CC adicional; card do LMB e uma sobrepeça visual reversível da Manopla; testar base/A, whiff/immune/LOS/gerações/cancel/clocks/cleanup e referências autoradas, com capturas comparáveis. Usar permissões None: composições derivadas B/C continuam sob gates01.3/4. Sem retuning base, rig novo, especial novo ou UI de swap. Números provisórios; qualquer necessidade de alterar geometria/visual para percepção deve ser apresentada com evidência, sem mudar o básico nativo.

Estado histórico antes desta autorização:01.P1 aceita;01.1 aguardava revisão;01.2 ainda não iniciada.01.3/4/5/6 exigem aprovações subsequentes; não há autorização em lote. Q/Avanço é compatibilidade de aproximação; uma sinergia mecânica skill↔básico exige hipótese e prova próprias futuras.

RF-TROCA permanece para GPR-03.1: aquisição/substituição de Q/E/R com compatibilidade, investimentos, timers/recursos, ticket e rollback; não substituir RMB/Asura. Aceite exige oferta real e troca seletiva observada, ainda NÃO VALIDADO.

**Gate histórico01.1 satisfeito pela autorização01.2. O STOP vigente passa a ser após01.2, para revisão antes de01.3.**
