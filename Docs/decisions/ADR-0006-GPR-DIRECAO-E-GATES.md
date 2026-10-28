# ADR-0006 — Gameplay Rework: decisões propostas e divergências

## Atualização vigente — C01.3a,27/10/2026

[Autorização e ampliações](AUTORIZACAO_GPR_C01_3A_20261027.md) / [relatório](GPR-C01.3a_RELATORIO.md). C01.2c aceita tecnicamente;3a autorizada para LMB e depois ampliada diretamente para Asura no RMB.3a concluída tecnicamente102/102; revisão pendente, sem aceite físico final e sem iniciar3b.

| Decisão | Delta vigente |
|---|---|
| D11 — primário | Um press→um pedido direcional no plano, sem exigir target/snap; cliques avançam só o próximo básico; whiff usa executor/geometria existentes. Buffer genérico do holder120ms/single slot, clocks intactos. |
| D16 — hold | Desenvolvedor escolheu preservar hold somente com ChargedShot para o básico primário. Arco sem esse rank não repete; release não cria disparo. Não é uma nova regra universal release-to-fire. |
| D11 — RMB Asura | Aprovado e implementado por mensagem adicional: canal secundário efetivo (Mouse1) entrega no mesmo holder/estado de AsuraBurst; F11 compatível, OR de edges não duplica custo. Sem básico no RMB Manopla; RMB vence LMB simultâneo; F vence continuação. |
| D15 — slots/extração | Mantida referência/índice histórico3 para snapshots/boons/timers. Adapter de input entregue; quatro slots modulares independentes, X e extração completa continuam não implementados. Sem segunda Asura em X. |
| D17 — outros secundários | Arco/Lança permanecem com input secundário legado; nenhum kit novo inferido. |
| D12/D18 — animação/dash | Facing de ataque/impacto preservado. Walk_N/Run_N não formam repertório completo de strafe/backpedal; requer design separado. Dash cursor preservado, configuração mouse/WASD futura. |
| Gate | STOP após3a, revisar relatório antes de3b. Commit/push integral e desligamento autorizados não expandem gameplay. |

As seções antigas abaixo são históricas onde conflitam com este delta.

## Atualização vigente — C01.2c,27/10/2026

C01.2a/2b aceitas pelo desenvolvedor no escopo técnico testado; C01.2c foi autorizada especificamente e agora está concluída tecnicamente, aguardando revisão.83/83 normais, falha negativa de hitch500 preservada, histórico de recovery não encerrado como corrigido. [Relatório](GPR-C01.2c_RELATORIO.md). As propostas históricas abaixo são superadas apenas pelos deltas explícitos desta seção.

| Decisão/delta | Estado vigente |
|---|---|
| D12 — movimento | Manter NavMeshAgent + writer existente; sliding restrito à locomoção, avanço seguro e projeção do resíduo sem ganho de velocidade. Valores speed/dash preservados; feel físico desta revisão pendente. |
| D12 — dash/facing | Feedback humano pede opção entre cursor e direção WASD nas configurações; default atual para cursor preservado. Opção e refinamento de animação ainda não implementados. |
| D13 — interação | Interact deve ter identidade própria, ownership contextual e binding ainda aberta. Não adotar sugestões históricas G/F. Adapter legado de Skill3/R no lobby é temporário e continua reservado; close invalida comandos. |
| D13 — demais ações | Q/E/R migrados preservados; especial index3/F11 e finalização configurada intactos. Sem X ou RMB Asura nesta atividade. |
| TTL recovery sob hitch |0,35s preservados; hitch500 excede validade e o teste original falha nessa condição. Novo negativo verifica descarte. Investigar causa histórica/decidir semântica antes de chamar incidente definitivamente corrigido. |
| Gate seguinte | STOP após C01.2c; pedir revisão antes de C01.3. LMB sem target é feedback para esse recorte; Settings/dash e demais migrações exigem etapa autorizada. |

27/10/2026. Estado vigente: **C01.2a aceita para implementação; C01.2b e migração mínima das teclas de skills autorizadas e implementadas**. Validação técnica66/66 finais, sem aceite físico. Demais etapas não iniciadas. [Relatório C01.2b](GPR-C01.2b_RELATORIO.md), [C01.2a histórica](GPR-C01.2a_RELATORIO.md), [visão](../gameplay/TECH_GUY_GAMEPLAY_MUTACOES_SINERGIAS_E_SKILLS_MODULARES(1).md) e plano WASD v1.1. A decisão direta mais recente prevalece sobre fontes históricas.

### D12/D14 — adoção C01.2b e exceção de teclas autorizada

WASD relativo à câmera e mira independente integrados nas quatro cenas jogáveis habilitadas, com motor protegido por cápsula/raycast. Cursor dash preservado; sem adoção da alternativa WASD dash. Writer comum recebe os produtores existentes, pull prevalece sobre dash, body traversal publica a pose já resolvida para contato. Nenhum aceite físico é inferido do “perfeito” referente ao protótipo.

Confirmação adicional do desenvolvedor incluiu migração de teclas para liberar W: três skills herdadas Q/E/R, especial herdado na customizaçãoF11 e finisherF do baseline. X sem conteúdo e AsuraRMB não implementados; índices/IDs/boons/causas continuam históricos. Facada GamePreferences estendida com leitura migrada e Apply versionado em DirectBinding.v2, sem sobrescrever as chaves antigas. Settings de movimento e arquitetura modular completas continuam pendentes. Interact segue o contexto histórico Skill3, efetivamenteR neste perfil, até decisão de separação.

Próxima proposta é avaliação física e C01.2c. Ressalvas: diagonal contra borda pode parar inteira; falha transitória de buffer não reproduzida nos reruns continua registrada; nenhum GO global de controles ou GPR-01.

### Histórico D12 — evidência C01.2a e correção de dependências

Move cru rejeitado: atravessa collider não incluído no bake. Move com sweep de cápsula e limite por raycast navegável permanece candidato técnico, sem adoção no player. Camera-relative inicial e world-relative comparados a mesma velocidade; referencial e direção final do dash ainda sem aceite humano. Dash cursor preservado como padrão de compatibilidade da fixture; alternativa WASD só comparativa. CharacterController avaliado sozinho e sem controle simultâneo do agente: bloqueia parede, mas precisa solução própria para borda/solo/ownership. Não iniciar migração por esta evidência.

Extração de Asura, X inicial e especiais Bow/Spear seguem abertos, **sem bloquear o experimento isolado de motor**. Gates de conteúdo aplicam-se às etapas correspondentes, não retroativamente à C01.2a. A próxima C01.2b exige revisão deste relatório e autorização explícita; nenhuma arquitetura de input de produção foi migrada.

## Revisão de decisão recebida após GPR-00 — vigente

| ID | Estado atual | Decisão/limite |
|---|---|---|
| D01/D02 | PROPOSTO, não regra permanente | Preservar kit/ações na migração; proteção inicial e quantidade de slots preenchidos não definem o loadout modular final. Conteúdo inicial de X após separar Asura exige D15 |
| D04/D05 | PROPOSTO, não aprovado | Bloqueio por investimento e cooldown=max são hipóteses para a fatia futura; medir ofertas mortas. Sem autorização GPR-01 |
| D06–D09 | PROPOSTO | A+Conductor+Uppercut, procs e prova visual continuam candidatos; precisam rota/ordinal legais e validação. Aprovação documental não aprovou os parâmetros |
| D11 | APPROVED_EXISTING — nova direção | **LMB clique deliberado=um pedido de básico; cliques sucessivos avançam sequência. RMB =especial específico da arma, Asura na Manopla.** Não é outro básico/terminal/heavy, quinta modular nem substituto de QERX. Proposta antiga de varredura1,25×/0,8× e alias Bow/Spear está SUPERADA |
| D12 | PARCIALMENTE APROVADO / engenharia pendente | WASD direto, mira independente, Space dash, respeitar compromisso e uma autoridade. Agent.Move, referência câmera/mundo e direção de dash continuam hipóteses, não aceitas por testes de navegação legada |
| D13 | PARCIALMENTE APROVADO | Q/E/R/X são slots1–4; Space aprovado; Esc mantido; F contextual provisório. InteractG/F/V não aprovado. Asura não fica como skillX: passa ao especialRMB, preservando executor/continuações |
| D14 | DIREÇÃO APROVADA, desenho PROPOSTO | Ações, conflitos/reservadas/persistência/reset/labels/gates exigidos. Input Actions+facade recomendado; piloto de migração em memória/EditMode e arquitetura precisam gate antes do backend; Settings não implementado agora |
| D15 | NEEDS_DEVELOPER_DECISION | Asura hoje é index3; separar especial do loadout exige política de compatibilidade. Recomenda-se quatro slots modulares comX inicialmente vazio e Asura no especial, sem criar habilidade nova; alternativa aguardar autoria de uma quarta skill aprovada. **Não implantar X vazio sem decisão** |
| D16 | NEEDS_DEVELOPER_DECISION | Padrão novo sem repetição automática por hold. Recomendar modo legado opt-in apenas após aprovação, preservando opção de migração e analisando ChargedShot do arco. Não apagar nem aceitar hold silenciosamente |
| D17 | NEEDS_DEVELOPER_DECISION | RMB de Bow/Spear precisa mecânica de arma projetada. Marca/carga/órbita encontradas pertencem a habilidades/boons; não realocá-las ou inventar kits nesta atividade. Não usar alias de básico |
| D18 | NEEDS_DEVELOPER_DECISION | Dash sempre na mira (mais próximo do atual) versus vetorWASD com fallbackmira/facing. Comparar fisicamente antes de decidir; câmera relativa também precisa aceite |

**Acoplamento comprovado:** BreakerGauntletCombat.TryBeginCast usa Array.IndexOf(_weapon.abilities,ability) para snapshot/causa; AbilityHolder.TryUseAbility trata index3 como continuação Cadenciada; cooldown/buffer/RunStatistics e boons R também dependem da identidade histórica. Simplesmente remover Asura do array geraria índice-1 e pode confundir Basic; duplicar binding em RMB/X criaria dois caminhos para uma execução. Não usar nenhuma dessas simplificações.

Desenho de compatibilidade recomendado em [migração](../gameplay/GPR_CONTROL_MIGRATION.md): mesmo holder/executor, única referência e único estado de Asura, identidade histórica R preservada; visão modular de quatro slots independente da referência autorada legada. QuandoX receber conteúdo próprio, resolver identidade/capabilities explicitamente antes de ocupar o índice. Isso é proposta de engenharia, não implementação autorizada.

**Gate vigente após C01.2b:** implementação do motor e teclas mínimas autorizada/entregue; físico e demais recortes pendentes. C01.2c é próxima proposta; C01.3–5/GPR-01 não iniciadas. D15–18/interação/remap completo serão resolvidas nas etapas correspondentes. [Relatório vigente](GPR-C01.2b_RELATORIO.md) / [prontidão histórica](GPR-C01.1_BASELINE_E_PRONTIDAO.md).

## Histórico preservado da proposta GPR-00 — NÃO vigente onde conflita com a revisão acima

## Autoridade e divergências

| Tema | Visão/plano | Runtime atual | Reconciliação |
|---|---|---|---|
| Controle | Visão QWER; decisão posterior WASD/mouse/QERX | Clique esquerdo básico/mover, direito mover/interagir; Shift direcional; Q/W/E/F11 efetivos | APPROVED_EXISTING: direção nova substitui teclas antigas; implementação ainda ausente |
| Finisher E | Preservar finalização característica em E | index2 é Choque/Stagger; index3 Asura; F TryFinishSequence (Cadenciada/órbita), F11 é binding salvo de index3 | Preservar separadamente as três funções; não declarar E = F = Asura |
| Reação básica | Visão cita Push/flinch, sem interrupção sistemática | default Push + overrides/combos/contribuição de interrupção; canônico antigo admite micro-stagger elegível | Preservar perfis reais, testar Common/Heavy/Elite/Boss; não impor Stagger global nem remover exceções válidas |
| Slice | Visão pede duas builds/três efeitos; plano faseia dois itens primeiro | Já há Protocols/Mutations, sem composição visual/modularidade universal comprovadas | GPR-01 dois itens + um swap; GPR-02 três itens; GPR-03 duas builds. Critérios finais da visão continuam válidos nas fases posteriores |
| Kit inicial | Core + ≥1 ativa, loadout formado na run | Quatro skills autoradas por arma e gates de rota dependentes de Mutation | Manter quatro preenchidas na migração/fatia inicial; redução futura exige decisão própria |
| Arquitetura | Exemplos de novas Definitions/RunBuildState | Clones de RunBoons, WeaponRunModifiers, holder, scheduler, budget e analytics reais | Estender owners, não duplicá-los; nomes de exemplo não são APIs |
| Visual | Hierarquia original prioriza perigos críticos acima do espetáculo | Canônico HUD ordena jogador/inimigos e depois avisos | Critério operacional: nenhum efeito do jogador pode encobrir aviso crítico, silhueta ou recursos; demais nuances ficam sob revisão visual humana |

## Registro de decisões

APPROVED_EXISTING significa direção já expressa pelo desenvolvedor ou contrato a preservar, não aceite de nova implementação. PROPOSED é recomendação para aprovação; NEEDS_DEVELOPER_DECISION identifica alternativas materiais.

| ID / estado | Recomendação concreta | Alternativa e impacto | Owner |
|---|---|---|---|
| D01 NEEDS_DEVELOPER_DECISION — núcleo protegido | Preservar básico, dash, Choque(index2), Asura(index3) e FinishSequence; Q/Avanço permanece inicial, único candidato a troca controlada da fatia | Tornar E/Asura livremente trocáveis altera identidade/energia/finisher e amplia regressões; não recomendado agora | 00.3 |
| D02 PROPOSED — kit inicial | Quatro ativas preenchidas na migração e GPR-01 | Core+uma ativa exige redesenho de ofertas/rota; adiar GPR-03 | 00.3 |
| D03 PROPOSED — compatibilidade | Skills exclusivas de arma inicialmente; universais só com executor/capabilities reais | Universalizar logo expande combinações inválidas e custo de teste | 00.3 |
| D04 NEEDS_DEVELOPER_DECISION — investimentos | Na fatia, recusar troca que invalide Mutation/Augment específico; investimentos compatíveis permanecem; sem reembolso/moeda/conversão | Suspender exige estado e UI adicionais; reembolsar exige economia nova. Nenhuma perda silenciosa | 00.3 |
| D05 PROPOSED — recarga/charges/cast | Trocar só em reward segura sem cast/filho/feedback pendente; restante novo=max(restante antigo, recarga nominal efetiva nova), sem tocar outros slots; entrada com zero charges se skill futura tiver charges | Herdar percentual permite refrescar por habilidade curta; resetar pronto cria exploração. Fatia usa skill sem charges | 00.3 |
| D06 NEEDS_DEVELOPER_DECISION — conteúdo preciso | A: Punhos Titânicos (novo conteúdo do básico, contrato abaixo); B: Conductor já existente, ampliado somente para a onda autorizada. Skill: Avanço(index0)→Uppercut Eclipse existente por adaptador de variante e clones | Criar skill nova no slot1 aumenta autoria/tuning; reaproveitar ComboNova não é equivalente a finisher confirmado | 00.3 |
| D07 PROPOSED — oportunidade | Ofertar A em ordinal Mutation compatível existente; B via tickets Protocol existentes; swap usa a oportunidade já ocupada por Uppercut, sem ticket/ordinal extra. Validador garante pelo menos um caminho legal, sem garantir build rara em toda run | Reservar nova sala/forçar drops altera economia/rota. Se tipos atuais não comportarem a oferta, apresentar delta ao desenvolvedor antes de tocar assets | 00.3 |
| D08 PROPOSED — procs | Onda é filho de final de combo confirmado, pode acionar Conductor uma vez; elétricos não regeneram onda/eletricidade/energia/mana por esta regra nova | Permitir todos os derivados exigiria outra política de chains; GPR-02 apenas por whitelist | 00.3 |
| D09 NEEDS_DEVELOPER_DECISION — prova visual | Manoplas/acessório maior visível + onda correspondente; B acrescenta fissura/arco discreto. Comparar base/A/A+B/reset na câmera real e verificar aviso hostil/HP | Só recolorir não demonstra transformação; novo rig/modelo inteiro amplia escopo | 00.3 |
| D10 PROPOSED — defeitos externos | Incidente ranged GI1.9 fica separado; se invalidar prova de esquiva, bloquear esse cenário e solicitar recorte específico | Corrigir todos os inimigos/mapas como dependência amplia tarefa sem causa confirmada | 00.3 |
| D11 NEEDS_DEVELOPER_DECISION — RMB | Gauntlet: ramo terminal do combo após LMB preparatório; um press válido de RMB substitui o próximo terminal da sequência, com perfil/cadência existentes; fora da janela não executa/cobra. Bow/Spear usam política de canal secundário equivalente ao básico nativo inicialmente, sem diferencial anunciado | Heavy novo precisa perfil/tuning/animação; follow-up contextual pode conflitar com F. Diferencial de Bow/Spear fica GPR-05 | 00.5 |
| D12 NEEDS_DEVELOPER_DECISION — locomoção | WASD relativo à projeção da câmera; agente existente como único motor, sem pathfinding de perseguição; dash na direção WASD ativa, senão mira válida, senão facing | World-relative é simples, mas rompe orientação visual em câmeras giradas; CharacterController exige migrar toda a integração | 00.5 |
| D13 NEEDS_DEVELOPER_DECISION — teclas auxiliares | Skill1 Q, Skill2 E, Skill3 R, Skill4 X; Interact G; FinishSequence F; Dash Space; Pause Esc. Asura fica index3/X; sem quinta skill | F para interagir exige mover finalização para outra tecla; manter E interação continua ambiguidade. F11 customizado deve ser preservado até migração explícita do jogador | 00.5 |
| D14 NEEDS_DEVELOPER_DECISION — Settings | Um backend InputAction com fachada GamePreferences e painel existente ampliados; migração versionada de bindings, conflitos por contexto, primário+secundário opcional, Apply/Cancel/Reset | Ampliar apenas KeyCode é menor diff inicial, mas aumenta código próprio de composite/rebind/contexto. Não manter dois leitores simultâneos | 00.5 |

O plano sugere Interact=F, WeaponSpecial=C e Finisher=V como recomendações, não decisões. A auditoria não encontrou Asura independente do index3: criar C além de X duplicaria a ativação. Por isso D13 recomenda G/F e Asura no index3/X; alternativa F-interação/V-finalização continua disponível para escolha explícita. A política detalhada de RMB (incluindo geometria curta/larga proposta, sem mudar timing/HP) está em [GPR_CONTROL_MIGRATION](../gameplay/GPR_CONTROL_MIGRATION.md); ela completa D11 e não é tuning aprovado.

## Fatia mínima GPR-01 proposta

**A — Punhos Titânicos (identificador novo a registrar, não asset existente):** apenas o contato terminal do combo básico que confirmar HP pode reservar uma onda frontal curta na origem/direção capturadas. Só uma onda por raiz, dedupe por vítima/geração, sem homing. Onda estende geometria para um alvo lateral que o soco não alcançaria; alvo já atingido pelo soco não recebe HP adicional dessa onda nesta primeira fatia. Mantém HP por vítima pelo coeficiente do básico capturado (1,0 como hipótese de protótipo a aprovar), sem mudar cadência/cancel/perfil de reação. Alcance/largura derivam do perfil real e precisam ajuste visual aprovado antes da implementação, sem copiar 2,5m de ComboNova. Primary confirma→filho reservado→impacto; whiff não cria onda. Visual acompanha alcance verdadeiro.

**B — Conductor existente:** reaproveitar ranks/limites/dano do catálogo de Protocols atual. A onda adquire autorização explícita para uma emissão elétrica por raiz (mesmo se acerta vários alvos); ela não adquire permissão genérica para todos os Protocols. Vítima e alvos encadeados distintos, elegíveis e dentro da geometria/LOS atual do Conductor; não inverter ordem para multiplicar procs. A+B permite atingir um alvo por onda e encadear a um terceiro elegível, com lineage de Basic→onda→Conductor e total reconciliado. B antes de A e A antes de B devem gerar o mesmo comportamento com mesmo inventário/seed.

**Swap limitado — Avanço→Uppercut Eclipse:** candidato já existe (`mutation_gauntlet_eclipse_uppercut`, BoonReworkCatalog/BreakerGauntletCombat). Adaptador de escolha no index0 usa clones/Mutation do executor atual e as restrições existentes; não anunciar sistema modular universal concluído. Dash continua mobilidade mínima; Choque e Asura preservados. Antes de equipar, UI compara ação, custo/recarga, mobilidade perdida, launch por perfil e boons impedidos. Caso a API de Mutation atual não consiga preservar timers/rollback sem reescrita, manter desenho proposto e pedir revisão de escopo em vez de criar executor paralelo.

**C opcional GPR-02 — Ecos do Lutador:** um eco terminal autorizado, mesma ancestry/budget; nunca eco de eco. Não habilitar Punho Ecoante do slot W para Basic por simples renomeação. Onda+Conductor+eco precisa regra explícita de apenas uma condução por raiz e visual distinto; não faz parte de GPR-01.

Aceite GPR-01: base funcional; A muda geometria de verdade; A+B muda alcance tático/efeito e mantém dedupe/budget; swap seguro e explicável; visual mínimo reversível; segundo ciclo limpo; resumo confere HP e lineage; nenhuma regressão GI de timing/reação/cancel. Exige GPR-C01 aprovado tecnicamente e pelo desenvolvedor primeiro. Duas builds completas, três vias, meta e expansão de armas ficam nas fases posteriores.

## Ordem, conflitos e estado visual

Resolver arma/skill/slot estáveis → seleção exclusiva de forma → snapshot por execução de modificadores válidos → intenção/commit nos clocks existentes → contato/HP confirmado → whitelist de derivados/budget → reação/feedback existente → apresentação derivada/telemetria. Adições compatíveis por prioridade e ID estável; formas exclusivas são recusadas/mostradas como substituição antes de confirmar, nunca “última escrita vence”. Aquisição não pode alterar snapshot de ataque em andamento.

Visuais são reconciliados a partir do inventário autoritativo: assinatura permanente de run, efeitos temporários e feedback de impacto têm owners/teardown separados; transformação exclusiva pode ocultar outra peça, mas deve restaurá-la ao sair. MaterialPropertyBlock/attachments só se compatíveis com rig/renderers atuais; não acumular materiais novos por recompensa. Hit reaction, telegraph, silhueta e HUD prevalecem sobre espetáculo. Morte/disable/troca/pooling/nova run removem somente estado do owner.
