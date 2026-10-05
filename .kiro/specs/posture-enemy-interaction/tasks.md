# Implementation Plan: Posture & Enemy Interaction (Fase 2)

## Overview

Este plano converte [requirements.md](requirements.md) e [design.md](design.md) em incrementos de implementação da **Fase 2 — Posture & Enemy Interaction**, no padrão das specs existentes. Estende o sistema atual; não substitui a fundação da Fase 1 nem implementa Manopla 2.0, novos boons, reworks de Arco/Lança ou Pull de inimigo.

Ordem: reconciliação e auditoria → dados e lógica pura com testes → pipeline de reação e gates → movimentos/lifecycle → migração do básico e perfis → feedback agrupado → regressão e playtest. A execução começou em 02/10/2026. Os itens marcados possuem evidência registrada em `contract-audit.md`; itens restantes e checkpoints permanecem pendentes.

**Pré-requisito reconciliado na tarefa 1:** os contratos antigos de CC proporcional de Heavy/Fragile foram substituídos pelo CC binário da Fase 2. A tarefa 1 fecha esse contrato e as decisões necessárias antes das tarefas de código dependentes. O pedido de gerar o plano permite documentar a sequência agora; não autoriza ignorar conflitos nem tratar números iniciais como balanceamento aprovado.

Código de reação em `Assets/_Project/Scripts/Characters/Combat`; apresentação em `Effects`/`UI`; configuração em `EnemyProfile` e assets sob `Assets/_Project`. Ferramentas de migração ficam separadas do gameplay, em pasta Editor apropriada. Testes EditMode em `Assets/_Project/Scripts/Tests/EditMode/Editor`, PlayMode em `Assets/_Project/Scripts/Tests/PlayMode`. Unity gera `.meta` novos; preservar os GUIDs existentes.

Propriedades P1–P14 referem-se à seção **Correctness Properties** do design. Usar `PropertyCheck.ForAll(...)`, ≥100 casos determinísticos por propriedade, seed/contraexemplo e tags `// Feature: posture-enemy-interaction, Property N: {texto}` e `// Validates: Requirements X.Y`. Nenhum teste de contrato, persistência ou cenário de integração listado abaixo é opcional.

Unity MCP é obrigatório nas verificações dependentes do Editor: confirmar instância/projeto na execução, capturar Console inicial sem limpar, importar quando necessário, aguardar compilação e inspecionar Console após cada incremento relevante. Executar testes suportados e sair de PlayMode preservando a cena do usuário. Se indisponível, registrar tentativa e limitação; marcar checks pendentes, sem declarar checkpoint validado apenas por inspeção estática.

## Tasks

- [x] 1. Consolidar contratos e auditar consumidores antes de implementar
  - [x] 1.1 Reconciliar o contrato cruzado de Fragile e CC binário
    - Conferir `enemy-swarm-core-archetypes` R17.4, design/tasks associados e testes que exigem airtime proporcional a resistência.
    - Registrar substituição intencional: resistência <1 permite duração/altura/distância integral; resistência 1 impede o canal. Fragile continua lançável, mas a comparação antiga de airtime não é simultaneamente preservada.
    - Sincronizar documentos afetados e registrar quais expectativas de teste precisarão mudar, preservando cobertura de duração, imunidade, colisão e recuperação. Não alterar testes para ocultar defeitos nem implementar multiplicador oculto.
    - _Requisitos: R2.1, R2.6, R3.7, R6.8, R8.7, R8.9_
  - [x] 1.2 Auditar APIs, ordem de callbacks e entradas reais de reação
    - Inventariar fachadas do controlador, `PlayerActor.ApplyHitReactionTo`, construtor/membros de `HitReactionRequest`, valores dos enums e emissores/assinantes de `StanceBroken`, `OnStanceBreak`, `OnBasicHit`, `AttackHitsResolved`.
    - Registrar a ordem observável por pipeline básico/área/hitbox/boon, inclusive callbacks reentrantes; não supor ordenação idêntica.
    - Mapear `CharControlScript.BuildBasicAttackReaction`, `HitboxDamage`, `AreaHitStep`, `GauntletLoopSteps`, `BreakerGauntletCombat`, Arco/Lança e boons afetados.
    - Auditar consumidores de `IsControlLocked`/`IsStunned`: IA, boss, archetype behaviors, Mirror, Healer, preferred distance, pull do Hooker e HUD.
    - Produzir `.kiro/specs/posture-enemy-interaction/contract-audit.md` com fonte/método, contrato antigo/novo, justificativa e próximos checks.
    - _Requisitos: R5.1, R5.2, R8.1–R8.9_
  - [x] 1.3 Confirmar autoria necessária e baseline Unity
    - Confirmar Unity MCP no projeto correto, capacidades disponíveis, estado do Editor e Console preexistente; não reutilizar confirmação histórica como estado atual.
    - Inspecionar perfis/prefabs/cenas representativos e referências de Animator/visual/áudio/VFX; inventariar overrides legados do básico por arma e combo.
    - Registrar defaults iniciais do design e faixas de validação, inclusive durações de Knockdown, teto de SmallMob e watchdog. Definir como representam ausência de override e zero intencional.
    - Confirmar representante MediumMob (Heavy) sem trocar seu rank/stats e documentar clips reutilizados ou alternativa procedural.
    - _Requisitos: R2.4, R3.1, R3.6, R5.4, R5.5, R7.7, R8.11_

- [x] 2. Checkpoint — contratos reconciliados e baseline registrado
  - Dependência: tarefa 1. Confirmar que não há contratos antigos e novos contraditórios, especialmente Fragile, locks, retomada e eventos.
  - Não iniciar tarefas de código dependentes enquanto a reconciliação necessária estiver aberta. Registrar limitações reais e decisões que exijam esclarecimento do usuário.
  - _Requisitos: R8.7, R8.9_

- [x] 3. Implementar configuração de categoria, dados de alvo e validação
  - [x] 3.1 Adicionar categoria e configuração serializável compatível
    - Adicionar `ReactionCategory { Unspecified=0, SmallMob=1, MediumMob=2, Elite=3, Boss=4 }`, versão de autoria, categoria opcional, defaults de quebra, resistência Knockdown, fases/durações, políticas/tetos e exceções físicas explícitas de boss.
    - Preservar campos existentes e dados serializados; usar `FormerlySerializedAs` somente se renomeação for necessária. Manter `EnemyRank` separado de `EnemyRarity` e categoria.
    - Adicionar `StanceBreakEffect.Knockdown=4` após os quatro membros atuais, sem alterar seus valores; auditar switches e assets conforme tarefa 1.
    - _Requisitos: R2.4, R3.1, R3.6, R3.8, R8.2, R8.3_
  - [x] 3.2 Implementar resolução pura e validação de dados brutos
    - Resolver fallback Normal→SmallMob, Elite→Elite, Legendary→Elite com dureza Legendary, Boss→Boss; ausência de perfil produz configuração completa e aviso estável `RankReactionDefaults.MissingConfigId(rank)`.
    - Preservar monotonicidade dos defaults por rank. Validar NaN/infinito, negativos e limites de resistência, postura, multiplicador, imunidade, atraso, janela e efeitos antes de clamps/getters.
    - Runtime usa fallback seguro; `OnValidate` diagnostica campo/faixa sem mutar autoria nem executar gameplay.
    - _Requisitos: R3.1, R3.7–R3.9, R4.9, R8.6, R8.11_
  - [x] 3.3 Adicionar testes EditMode de configuração
    - **P3 — Identidade:** fallback determinístico, categoria explícita, Legendary, falta de perfil, rank real invariante sob EffectiveRank e valores serializados de enum preservados.
    - Casos exemplo para dados inválidos, limites superiores, zero intencional e fallback runtime sem alteração do asset; provar aviso sem spam por acerto.
    - _Requisitos: R3.1, R3.7–R3.9, R4.8, R4.9, R8.2, R8.11_

- [x] 4. Estender decisão pura de reação, resistência e interrupção
  - [x] 4.1 Centralizar elegibilidade e fallback de quebra
    - Reusar helpers existentes; resolver efeito explícito antes do default de categoria. MediumMob usa Knockdown; Knockback nunca é fallback genérico.
    - Proteger rank Boss ou categoria Boss contra interrupção comum e canais físicos proibidos, inclusive perfil Boss→SmallMob incoerente. Exceções físicas são explícitas por canal.
    - Estender `BreakEffectResistance`: Knockdown→Stun→None; preservar KnockUp→Stun→None, Stun→None e Knockback→None. Aplicar resistência binária conforme contrato reconciliado.
    - Revalidar permissões/resistências em fallback por obstrução; None resolvido termina a física, não reinicia busca de default.
    - _Requisitos: R1.4, R1.5, R2.1–R2.6, R3.2–R3.8, R4.2, R6.8, R6.9, R7.12_
  - [x] 4.2 Estender `RankInterruptStagger` sem bloquear postura
    - Usar rank real e limiar estritamente crescente por raridade para a mesma resistência; consumir um limiar por contribuição elegível e preservar excedente não negativo.
    - Adicionar decaimento/reset [0,5; 5] s desde a última contribuição positiva aceita. Valor recusado/zero não renova o prazo; ticks sem hit não disparam interrupções por excedente.
    - Categoria/estado/limiar precedem `InterruptWindowGate`; abaixo do limiar não cancelar, mas dano de postura continua independente quando elegível.
    - _Requisitos: R7.1–R7.8_
  - [x] 4.3 Testar propriedades e fronteiras
    - **P1:** independência de canais; **P4:** pedido/default/escadas; **P6:** limiar/excedente/decaimento; **P7:** cancelamento autorizado; **P12:** fallback autorizado e Boss incoerente.
    - Cobrir contribuição maior que dois limiares, resistência exatamente 1, ausência de windup, beat já resolvido, Launch obstruído com Stun proibido e pedido None versus resultado None.
    - _Requisitos: R1.1, R1.2, R1.6, R2.6, R3.8, R4.2, R7.1–R7.8, R7.12_

- [x] 5. Implementar estado puro de postura, efeitos, janela e retomada
  - [x] 5.1 Modelar tempo e propriedade dos bloqueios
    - Receber `now/delta` do relógio local elegível; modelar tokens por efeito, geração, janela, imunidade e deadlines sem dependência de cena.
    - Expiração libera só seu token. `IsControlLocked` agrega hard CC/abertura restritiva; `IsStunned` indica somente hard CC físico; Normal não recebe token restritivo por sua janela.
    - Modelar Knockdown derrubada→chão→recuperação e prioridade Knockdown>Launch>Knockback>Push, com política para mesma prioridade e invalidação de gerações antigas.
    - _Requisitos: R2.1–R2.4, R4.4, R6.1–R6.3, R6.9, R7.7_
  - [x] 5.2 Modelar quebra, regeneração e proteção de retomada
    - Detectar zero no passo de resolução; fixar resultado, janela positiva, reserva máxima, imunidade e atraso antes de notificar callbacks.
    - Regenerar após último dano de postura aplicado, sem exceder máximo; zero/dano ignorado não renova atraso.
    - Elite/Legendary/Boss ignoram postura/interrupção durante proteção, sem dano acumulado ou evento de quebra fictício; HP/feedback comum continuam.
    - Iniciar retomada após todos os bloqueios relevantes; terminar na conclusão da primeira execução válida/recovery, ausência de elegibilidade, cancelamento externo, lifecycle ou watchdog de até 10 s. Não rearmar automaticamente nem enfileirar controle recusado.
    - _Requisitos: R4.1, R4.4–R4.8, R6.4, R6.5, R7.7, R7.9, R7.11_
  - [x] 5.3 Escrever testes do estado
    - **P5 — Quebra única**, **P8 — Recuperação**, **P9 — Reaplicação limitada**, **P13 — Movimento exclusivo**, **P14 — Bloqueios observáveis**.
    - Verificar tokens sobrepostos, reaplicação/teto por categoria, transição Launch→Knockdown→Knockback, janela sem hard CC, retomada sem alvo/caminho/ação, watchdog e cleanup com callbacks antigos.
    - _Requisitos: R4.1, R4.4–R4.8, R6.1–R6.9, R7.7–R7.9, R7.11_

- [x] 6. Checkpoint — dados e lógica pura aprovados
  - Dependências: tarefas 3–5. Executar testes EditMode relevantes via Unity MCP, aguardar compilação e inspecionar Console sem limpar.
  - Confirmar P1/P3–P9/P12–P14 e os casos de fronteira; P2, P10 e P11 recebem cobertura nos incrementos seguintes. Não tratar decisão pura como prova de movimento ou gameplay.

- [x] 7. Integrar o pipeline único no `CombatReactionController`
  - [x] 7.1 Integrar os canais sem duplicar dano ou cancelamento
    - Preservar fachadas públicas; `ApplyReaction` e `ApplyInterruptStagger` usam as decisões anteriores e aplicam postura uma vez, inclusive None ou contribuição abaixo do limiar.
    - Fixar estado de quebra consistente antes de `StanceBroken`; preservar ordem auditada de callbacks ou registrar adaptação justificada. A quebra não duplica reação imediata/cancelamento.
    - Ponto de impacto finito e direção segura; fallback sem NaN/infinito. Não mudar HP ou pipeline de arma fora das reações planejadas.
    - _Requisitos: R1.1–R1.7, R4.1, R4.2, R5.1, R5.2, R7.2, R7.5, R7.6, R8.1–R8.5_
  - [x] 7.2 Integrar relógio de gameplay e configuração
    - Resolver configuração em `EnemyVariant` antes do primeiro hit e preservar chamadas `ConfigureRank/ConfigureStance`.
    - Avançar `now` local por delta escalado somente quando não pausado nem sob `HitStopRunner` ativo injetado; janela/acúmulo/efeitos/imunidade/watchdog compartilham origem.
    - Não criar segundo mecanismo de escala de tempo; resetar relógio/geração no lifecycle.
    - _Requisitos: R3.6, R3.9, R4.5–R4.9, R6.1, R6.3–R6.5, R7.8, R8.3_
  - [x] 7.3 Adicionar testes de integração do controlador
    - None+postura quebra no mesmo passo; None+zero não move; ausência de cosmético não altera mecânica; quebra resistida abre janela; chamada de interrupção abaixo do limiar reduz postura sem cancelar.
    - Verificar evento único, estado consistente durante callback reentrante e invariância do rank/categoria real.
    - _Requisitos: R1.1, R1.2, R1.6, R4.1–R4.8, R5.1, R7.2, R8.3_

- [x] 8. Integrar gates de IA, abertura e oportunidade de ação
  - [x] 8.1 Adaptar IA e consumidores de bloqueio
    - Integrar `EnemyAI`, `EnemyCombatActions`, `SectorBoss` e consumidores inventariados com semântica agregada de controle; sempre verificar lifecycle/agent além das propriedades.
    - Quebra cancela windup/beat pendente no mesmo passo; beat resolvido permanece. Abertura impede novas ofensivas/navegação; retomada escolhe nova execução sem reproduzir beat cancelado.
    - Mirror fica desprotegido durante abertura; Healer aborta canal corretamente; Hooker devolve controle ao jogador quando sua fonte fica travada; HUD não trata abertura isolada como stun.
    - _Requisitos: R3.4, R3.5, R4.4, R6.2, R6.9, R7.5, R7.6, R8.3, R8.7_
  - [x] 8.2 Conectar lifecycle da primeira ação à proteção
    - Reportar identificação, início, fim de recovery e cancelamento externo da execução ofensiva real; não liberar proteção apenas no beat.
    - Resolver saídas sem alvo/alcance/caminho/ação no passo de decisão; watchdog usa gameplay e diagnostica autoria excepcional sem rearmar proteção.
    - _Requisitos: R7.9, R7.11_
  - [x] 8.3 Testar abertura e retomada em PlayMode
    - Boss imune a todos os CC: abertura com controle travado/stun false, cancelamento pendente e retomada corretos.
    - Pressão comum contínua em Elite/Legendary/Boss, imunidade menor que janela, uma ação válida completa; HP/eventos de hit normais e nenhuma quebra fictícia durante proteção.
    - Boss real com categoria SmallMob; saídas sem alvo/caminho/ação e cancelamento externo; nenhum beat repetido.
    - _Requisitos: R3.5, R3.8, R4.4, R6.9, R7.9, R7.11, R7.12_

- [x] 9. Executar movimentos, Knockdown e cleanup com autoridade única
  - [x] 9.1 Separar microdeslocamento de hard CC
    - Push não chama interrupção nem pausa agente como efeito colateral; limitar deslocamento realizado a 0,5 m e rotação a 15°, inclusive caminho Rigidbody.
    - Reusar `ImmediateReactionClamp`, `DisplacementTier` e `ControlLockGate`; lock suprime Push extra e fim de todos os locks reabilita elegibilidade.
    - _Requisitos: R1.3–R1.7, R6.6, R6.7_
  - [x] 9.2 Implementar executores físicos e pose de Knockdown
    - Stun sem deslocamento, Launch com intenção aérea positiva e lock até pouso, Knockback explícito na direção correta e Knockdown completo com altura aérea zero.
    - Uma autoridade por trajetória/pose; substituição superior encerra geração anterior na posição segura atual, assenta Launch antes de Knockdown e rejeita Knockback inferior.
    - Obstrução passa novamente por permissões antes de fallback; não criar lock proibido e desfazê-lo depois. Não ligar ragdoll como escritor concorrente.
    - _Requisitos: R2.1–R2.4, R2.6, R6.1, R6.2, R6.8, R6.9_
  - [x] 9.3 Garantir cleanup e recuperação segura
    - Morte/disable/troca de sala/pool invalidam geração, cancelam timers/coroutines e restauram apenas offsets próprios, sem reviver ou reativar alvo.
    - Restaurar agente só vivo/ativo/habilitado/on-NavMesh/sem locks, preservando paradas externas. Efeitos antigos não retomam o transform após substituição.
    - _Requisitos: R6.2, R6.3_
  - [x] 9.4 Testar limites e lifecycle em Unity
    - **P2 — Limites imediatos** no modelo puro e medidas reais de deslocamento em PlayMode.
    - Push durante windup sem cancelamento; Launch bloqueado+Stun imune; Knockback contra geometria; ciclo de Knockdown; sequência de efeitos incompatíveis; locks sobrepostos e callback atrasado após pool.
    - _Requisitos: R1.3–R1.7, R2.1–R2.4, R6.1–R6.3, R6.6–R6.9_

- [x] 10. Checkpoint — postura, abertura e reações integradas
  - Dependências: tarefas 7–9. Executar EditMode/PlayMode relevantes via MCP e inspecionar Console.
  - Confirmar mecânica por chamadas reais do pipeline, recuperação sem reativação indevida e efeitos distintivos. A migração do input do básico e o feedback final ainda pertencem às próximas tarefas.

- [x] 11. Migrar autoria e entrada real do básico da Manopla
  - [x] 11.1 Transportar intenção/contribuição de stagger por arma e estágio
    - Adaptar `BuildBasicAttackReaction`, pipeline de área e caminho de hitbox com extensão aditiva de payload/contexto; manter assinatura/construtor existente.
    - Manopla autorada solicita Stagger e contribuição positiva; decisão final continua no alvo. Push isolado não contribui nem interrompe.
    - Usar dados por arma ou snapshot ao equipar, sem mudar globalmente o default compartilhado; preservar E em `GauntletLoopSteps` e APIs de Arco/Lança.
    - _Requisitos: R1.3, R1.4, R7.10, R7.12, R8.1, R8.2, R8.5, R8.6_
  - [x] 11.2 Criar migração Editor explícita e idempotente
    - Preview por asset/cena/prefab/override; versão legada→Fase 2 apenas para básico da Manopla. Preservar None/Push intencionais e registrar exceções para revisão.
    - Inventariar `comboReactions`, configuração ao equipar e instâncias de CombatStudy, Playground, NexusLobby, FirstSector e PrologueTutorial; não supor que a lista esgota todas as referências.
    - Autorar dados de alvo: Heavy MediumMob, Grunt/Fragile SmallMob, durações/defaults/resistência Knockdown/políticas e exceções explícitas, sem mudar rank real/stats fora do necessário.
    - Salvar/recarregar e inspecionar persistência via MCP; reaplicar migração não altera dados novamente.
    - _Requisitos: R2.4, R3.1, R3.3, R3.6–R3.9, R7.7, R7.12, R8.2, R8.11_
  - [x] 11.3 Testar ponta a ponta com input real e assets migrados
    - Básico direcionado e com alvo: pequeno interrompe em janela elegível, médio tem limitação por limiar e boss não cancela fora da abertura.
    - E abre juggle em comum elegível e respeita imunidades de boss; troca de arma não leva stagger da Manopla para Arco/Lança.
    - Verificar um dano/callback/proc por acerto, preservação de HP, timing e overrides; testar migração/persistência além de chamadas manuais ao controller.
    - _Requisitos: R3.2–R3.5, R7.10, R7.12, R8.4, R8.5, R8.8_

- [x] 12. Implementar composição pura e escopo de hit-stop
  - [x] 12.1 Modelar candidatos confirmados e fechamento por grupo
    - Resultado do impacto conserva contribuição global de dano válido inclusive letal; vida atual só governa efeitos locais. Contato sem dano não contribui por estar vivo.
    - Reusar `HitStop`/`HitStopGrouping` para máximo comum+quebra, sem soma; separar identidade `(ExecutionId, ImpactEvent.Index)` e lote do scheduler.
    - Implementar escopo local passado explicitamente, filhos reentrantes, profundidade e finalização idempotente em `finally`, usando somente resultados confirmados.
    - Impacto externo sem contexto cria grupo próprio; não agrupar uma execução inteira nem chamadas independentes por coincidência de frame. Não adicionar global/thread-static.
    - _Requisitos: R5.8, R5.9, R8.1, R8.4, R8.5_
  - [x] 12.2 Escrever testes de composição e fechamento
    - **P10 — Feedback separado:** máximo por grupo, contribuição letal preservada, sem candidato para contato sem dano e sem ênfase de quebra inexistente.
    - Cobrir filhos reentrantes, fechamento repetido, exception/retorno antecipado/cancelamento, impacto assíncrono em outro grupo e duração zero produzindo zero aplicações.
    - _Requisitos: R5.8–R5.10, R8.11_

- [x] 13. Integrar feedback audiovisual e aplicação única de hit-stop
  - [x] 13.1 Integrar o escopo nos executores e callbacks
    - Abrir lote antes dos impactos devidos; fechar após resolvedores, callbacks públicos e `AttackHitsResolved` síncronos.
    - Adaptar `BreakerGauntletCombat.EmitAuthoredImpact/ApplyHitStopForImpact`, área, hitbox e entradas externas para registrar candidatos; nenhum listener/apresentador aplica runner por alvo.
    - Aplicar `HitStopRunner` no máximo uma vez ao fechar; preservar contratos/ordem auditados e restauração de pausa/slow-motion. Nenhum mecanismo paralelo de hit-stop.
    - _Requisitos: R5.8, R5.9, R8.1, R8.4, R8.5, R8.8_
  - [x] 13.2 Implementar apresentação de quebra e sinais de janela
    - Evento de quebra único com ponto finito e ponte de HookBus existente; não duplicar hook no apresentador.
    - Pop/flash [0,20; 0,30] s reais com escala base restaurada inclusive em disable/reaplicação; animação/pose, SFX e VFX distintos de hit comum usando assets existentes.
    - Popup [1,0; 1,2] s reais e sinal de início/fim/ícone de janela quando `ShouldShowStanceBar` permite. Abertura isolada não usa indicação de stun.
    - Ausência de cosmético não altera postura/controle; cleanup e canais de escala/pose não competem com movimento.
    - _Requisitos: R1.2, R5.1–R5.7, R5.10, R6.3, R8.3_
  - [x] 13.3 Validar feedback em PlayMode
    - Único alvo morto pelo impacto mantém hit-stop global, sem navegação/controle local inválido; multi-alvo com múltiplas quebras e callbacks reentrantes produz uma aplicação por grupo.
    - Medir pop/popup em tempo real, restauração de escala, sinais de janela e nenhuma apresentação de quebra em hit não quebrador.
    - Pausa antes/durante hit-stop, slow-motion, overlap e teardown restauram modificadores corretamente; timers mecânicos permanecem congelados durante pausa/hit-stop.
    - _Requisitos: R5.1–R5.10, R6.1, R6.3, R7.8_

- [x] 14. Checkpoint — incremento completo da Fase 2 jogável
  - Dependências: tarefas 11–13. Validar via MCP cenas/prefabs/perfis migrados, compilação, Console e testes relevantes.
  - Confirmar básico real, E, quatro categorias, Heavy MediumMob, Knockdown e feedback agrupado. Registrar resultados; ainda não concluir gameplay acceptance sem tarefa 17.

- [x] 15. Executar regressão e garantir compatibilidade dos consumidores
  - [x] 15.1 Preservar e verificar contratos automatizados
    - **P11 — Compatibilidade:** assinaturas/valores serializados, payload, contagem e ordenação por pipeline auditado.
    - Executar `CompatibilityContractPlayModeTests`, `HookBusWiringExampleTests`, `BasicHitChannelPropertyTests`, `GuardBreakerPropertyTests`, `GuardBreakerKnockbackPlayModeTests` e demais testes de postura afetados.
    - Adaptar somente expectativas substituídas por contratos registrados na tarefa 1; preservar cobertura e justificar cada adaptação.
    - _Requisitos: R8.1–R8.4, R8.7, R8.9, R8.10_
  - [x] 15.2 Verificar armas, boons e comportamentos afetados
    - Manopla/boons: AsuraSurge, HungryComboTracker, ImpactGuardTracker/GuardBreaker, AdaptiveCadenceTracker, SeismicFist e E; sem procs/eventos duplicados ou Knockback genérico.
    - Arco/Lança: equipar, mover, atacar, habilidades/dash, firing-on-the-move, projéteis, timing e HP nos caminhos compartilhados.
    - Healer/Mirror/Hooker: canais cancelados, abertura legível, refletor e restauração de pull conforme contratos; Fragile conforme spec reconciliada.
    - Fase 1: fases, compromisso, cancelamento, buffer e dash permanecem fora das mudanças previstas.
    - _Requisitos: R2.5, R7.10, R8.4–R8.8, R8.10_

- [x] 16. Checkpoint — validação técnica e rastreabilidade finais
  - Dependência: tarefa 15. Executar suíte relevante EditMode/PlayMode por MCP e registrar resultados reais, testes não executados e limitações.
  - Conferir P1–P14 e critérios R1–R8 contra evidência; inspecionar Console sem apagar baseline; confirmar saída de PlayMode.
  - Revisar `git diff` por alterações involuntárias, GUIDs/referências, `.meta` órfãos e serialização inesperada. Inspecionar referências afetadas em Unity e persistência após reload.
  - Atualizar `contract-audit.md` com decisões/evidência técnica; falhas demonstradas dentro do escopo devem ser corrigidas antes de marcar este checkpoint.
  - _Requisitos: R4.9, R6.3, R8.7, R8.9–R8.11_

- [x] 17. Gate manual — aceitação de combate da Fase 2
  - Jogar com Manopla sem boons contra pequeno, médio, elite e boss, incluindo Heavy MediumMob e encontro com Legendary.
  - Verificar se categorias e quebra são reconhecíveis sem ler barra; Knockdown comunica queda/chão/recuperação; E abre juggle onde permitido; Elite/Boss recuperam ação real sob pressão comum contínua.
  - Registrar condições, configuração, observações, ajustes de balanceamento e defeitos. Compilação/testes aprovados não satisfazem este gate.
  - Se houver ajustes de dados/código, repetir apenas checks afetados antes de registrar aceitação. Pendência que depende de avaliação humana permanece explícita.
  - _Requisitos: R2.4, R3.2–R3.5, R4.4, R5.3–R5.7, R7.9, R7.10, R7.12_

- [x] 18. Consolidar handoff e encerramento da Fase 2
  - Dependências: tarefas 16–17. Produzir `.kiro/specs/posture-enemy-interaction/validation.md` com decisão/rationale, escopo, contratos adaptados, migrações aplicadas, evidências de compilação/testes/runtime/playtest separadas e próximas ações.
  - Atualizar status dos documentos sem apagar registros históricos; marcar tarefas concluídas somente com evidência, listar defeitos/validações pendentes e manter fases futuras separadas.
  - Não declarar a fase plenamente validada com checks Unity obrigatórios ou gate manual pendentes.
  - _Requisitos: R8.7, R8.10; AGENTS.md — Completion and Handoff_
  - Handoff técnico preparado em `validation.md`; o encerramento continua dependendo da aceitação humana da tarefa 17.
  - Atualização em 03/10/2026: usuário declarou **“Fase 2 está aceita. Pode seguir com as proximas tasks”**. Aceitação humana registrada no handoff, sem inventar duração, cena ou observações por cenário; tarefas 17–18 encerradas e dependência da Fase 3 liberada. As limitações técnicas preexistentes permanecem registradas.

## Dependencies

| Tarefa | Depende de |
| --- | --- |
| 1 | Documentos atuais e disponibilidade de baseline |
| 2 | 1 |
| 3 | 2 |
| 4 | 3 |
| 5 | 3, 4 |
| 6 | 3, 4, 5 |
| 7 | 6 |
| 8 | 7 |
| 9 | 7, 8 |
| 10 | 7, 8, 9 |
| 11 | 10 |
| 12 | 6 |
| 13 | 11, 12 |
| 14 | 11, 12, 13 |
| 15 | 14 |
| 16 | 15 |
| 17 | 16 |
| 18 | 16, 17 |

A tarefa 12 pode ser preparada após o checkpoint 6; sua integração depende da tarefa 11. Alterações no mesmo controlador/pipeline devem seguir a ordem dos incrementos para não criar duas autoridades temporárias de reação ou hit-stop. Este grafo expressa a ordem da implementação autorizada pelo usuário.

## Requirements Coverage Map

| Requisito | Tarefas principais |
| --- | --- |
| R1 — Canais independentes e limites | 4, 7, 9, 11, 13 |
| R2 — Taxonomia, Knockdown e fallback | 1, 3–5, 9, 15, 17 |
| R3 — Categoria, rank e dados | 3, 4, 7, 8, 11, 17 |
| R4 — Quebra e abertura | 4, 5, 7, 8 |
| R5 — Feedback e hit-stop | 1, 7, 12, 13, 17 |
| R6 — Tempo, recuperação e cleanup | 5, 7–9, 13, 16 |
| R7 — Acúmulo, retomada e básico | 4, 5, 8, 11, 15, 17 |
| R8 — Compatibilidade e validação | 1–3, 7, 11–13, 15, 16, 18 |

**Registro histórico — primeiro incremento em 02/10/2026:** configuração, decisões e estado puro implementados; 29 testes EditMode passaram via Unity MCP, incluindo persistência de asset temporário. Subtarefas 3.1, 4.1 e 5.2 concluídas. Restam casos adicionais de cobertura para 3.3/4.3/5.3, diagnóstico de ausência de perfil no novo fluxo e modelagem explícita das fases Knockdown; checkpoint 6 ainda pendente. Controlador/IA, executores físicos, migração dos assets de jogo, input real, hit-stop e PlayMode ainda não integrados/validados.


### Evidência incremental — continuidade

- Checkpoint 6: job MCP f9e5fde8328b4a2c9d58dbe8f0c3855e, 27/27 EditMode, fases/políticas/fallback/dados/estado/acúmulo.
- Controlador: job 2620d14ae35e49fa82e1e3a93236f884, 9/9 PlayMode (PostureReactionPlayModeTests e GuardBreakerKnockbackPlayModeTests), zero skips; estado reentrante, persistência de rank, Push Rigidbody, Knockdown/cleanup, boss imune, relógio, obstrução, aviso sem spam. Cobertura adicional de windup em validação.
- Tarefa 12: job bb0bf0f304db44c8add11d2fdc7ae19c, 6/6 EditMode (escopo e fronteiras), zero skips.
- Migração executada via MCP: 4 assets; segunda aplicação 0 alterações. Heavy MediumMob/220/HP2.2/movimento0.6 e Manopla Stagger/0.35 confirmados após save/reimport. Restante da tarefa 11 aguarda integração e checks.

### Integração validada — 02/10/2026

- Tarefas 8–14: controlador/IA/execução, retomada real Elite/Legendary/Boss, prioridade e lifecycle, básico migrado e E pela entrada AbilityHolder, feedback local e escopo de lote integrados.
- Job 69a16b497da74364a40316872a97f8cf: 47/47 PlayMode, zero falhas/skips, 19,0745406 s; inclui input Manopla, E comum/boss, múltiplas quebras e callback resolved reentrante, receipt de dano com cura/exceção, pop/popup em pausa e contratos de compatibilidade.
- Registro incremental desse checkpoint: regressão geral e inspeção final ainda estavam em andamento. Resultado final abaixo.

### Validação técnica concluída — 02/10/2026

- Tarefas 1–16 concluídas. EditMode final: job ff0e594693a7444ea2fde030de64d9d4, 450/450, zero falhas/skips, 38,4490683 s.
- Regressão PlayMode ampla: job 6f30a40ef89742ba8d636618c292906a, 171 executados, 169 aprovados, 2 falhas de fixture, zero skips, 1667,0197982 s. Summary consultado em memória via MCP porque get_test_job omite result em status failed.
- Duas fixtures corrigidas mantendo os casos e asserções: tempo determinístico do primeiro frame no teste homing; limpeza de referências do pool após destruir projéteis no teste SplitArrow/Overflow. Produção dos projéteis preservada.
- Reteste final: job 25b398a4c2ae4b47b0938febc9775c05, 23/23 PlayMode, zero falhas/skips, 40,1872393 s; inclui as duas falhas corrigidas e toda a integração de postura/feedback. As 171 verificações têm aprovação observada entre a execução ampla e o reteste; não houve uma segunda execução integral após as correções de fixture.
- MCP confirmou compilação sem erros, persistência dos quatro assets e repetição da migração com zero alterações. Sete cenas/49 prefabs inspecionados; script ausente legado localizado em Playground/Player e referências ausentes já existentes em HEAD registradas no handoff.
- git diff --check limpo; nenhum diff de cenas/prefabs/metas existentes/ProjectSettings; todos os scripts novos com meta e zero metas novos órfãos. Cenas temporárias do runner removidas pelo próprio runner. Editor em MainMenu, fora de PlayMode, dirty preservado; Console sem erros, warnings esperados das fixtures registrados no handoff.
- Gate 17 permanece pendente de avaliação humana; tarefa 18 tem handoff preparado, mas encerramento depende desse gate. `validation.md` contém evidência, limitações e roteiro concreto de aceitação.
