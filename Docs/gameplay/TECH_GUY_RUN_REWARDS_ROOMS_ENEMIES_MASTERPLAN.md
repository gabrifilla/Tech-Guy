# Tech Guy — plano integrado de incursões, recompensas, salas e inimigos

**Frente RUN-EXP-01,30/10/2026. Plano para revisão; STOP após os protótipos da etapa2.** Identificador independente de GPR. Não autoriza rollout futuro, troca modular Q/E/R, armas/especiais/Singularity ou criação de bosses.

## Resultado desta frente

| Atividade | Entrega | Estado e limite |
|---|---|---|
| Etapa0 | [Diagnóstico, fixes e jobs](../decisions/RUN_STAGE_0_CRITICAL_FIXES.md) | Pull não é renovado indefinidamente; janela estreita de0,6s contra outro pull. Fogo com oito línguas geométricas animadas/núcleo.10/10 focados finais e câmera publicada; aceite humano e regressões globais continuam abertos. |
| Etapa1 | [Auditoria e especificação](../decisions/RUN_STAGE_1_AUDIT_AND_DESIGN.md) | Inventários de recompensas/grafo/prefabs, taxonomia espacial, cinco candidatos, reações/contrajogo, pipeline e prioridades. Design proposto, não conteúdo publicado. |
| Etapa2 | [Estudos e capturas](../decisions/RUN_STAGE_2_PROTOTYPES.md) | Cinco formas/gestos e duas arenas isoladas;1/1 teste final de isolamento/captura. Primitivas sem IA/rig final/nav gameplay. |

GPR-01 permanece tecnicamente demonstrada com suas limitações. A evidência histórica203/204 e o relato de fogo irreconhecível não foram apagados; esta frente também encontrou322/353 na seleção ampliada. Não somar reexecuções isoladas para chamar tudo de verde.

## Intenção de produto

O desenvolvedor preso no computador deve atravessar mundos com regras e habitantes próprios para voltar à Terra. Cada run oferece uma história construída por escolhas: pressionar postura com Manopla, escolher precisão/distância com Arco ou dominar linhas/retornos com Lança. Arma com personalidade é direção narrativa preservada; não foi criado diálogo novo. A variedade de mundos deve mudar percurso e decisão, não apenas paleta.

Prioridade: recuperar controle e ler ameaça; reconhecer o efeito adquirido; fazer uma escolha útil; experimentar a build por tempo suficiente; encontrar espaços que exijam respostas diferentes. O kit base funciona sem boon. A+B+C é exemplo alcançável, não objetivo probabilístico da economia.

## Diagnóstico consolidado

- Pull pertence a Hooker/HookLine; conexões reiniciavam o bloqueio. Correção localizada preserva duração do pull e demais CC. Sequência publicada mostrou0,6002→2,1030s de pull e janela até3,6071s; captura assistida e CSV no relatório0.
- Fogo existia no snapshot/pool, mas fitas internas perdiam leitura para vento branco. A forma foi alterada sem mudar alcance, velocidade, dano, raio ou TTL; sem burn/DoT.
- O modo de assistência A+B foi encontrado **ligado no Editor**. Ele prioriza aquisições e muda oportunidades só para playtest; estado preservado. Para testar descoberta natural, desligar explicitamente o menu de teste e registrar contexto. Não usar essa sessão como evidência de economia normal.
- Pools base elegíveis: Technique/Mutation/Protocol=4/3/4 na Manopla,1/4/4 no Arco,1/5/4 na Lança. Pools vazios/saturados e capacidades mudam os números. Até3 cards sem reposição não garante3 boas decisões.
-11 nós/12 conexões/quatro caminhos, com reparo ou combate e ramo Elite; Mutation5 precede boss10. Layout gerado varia dimensão/posições, mas mantém arenas retangulares vazias e grade de spawn. Mais salas iguais não resolve identidade.
-16 prefabs consultados usam as mesmas sete malhas/Avatar/controller. Diferenças de perfil, tamanho e cor não substituem forma. ReactionController é adicionado no spawn por ProgressionDirector; prefab cru e inimigo da run não são o mesmo estado.

## Recompensas e builds

Preservar tickets sessão/nó/ordinal, revisão, IDs, determinismo e compatibilidade. Primeiro melhorar explicação de efeito imediato, transformação do input/geometria, potencial de interação e restrições. Depois medir habilitadores versus intensificadores por terço de run e encontros/usos restantes. Timing de Mutation, novas alternativas e qualquer alteração de pool/frequência/peso exigem revisão própria; não implantar aqui.

Critério: o jogador pode buscar uma direção, adaptar-se ao que apareceu e escolher força presente versus potencial futuro. Não medir sucesso por completar Titanic+Conductor+Detonation; medir escolhas mortas, repetição semântica, estilo executado e oportunidades após transformação. A coleta precisa separar arma, seed, assistência, família, ranks, cards e sobrevivência. Duração natural/viabilidade final por arma ainda não foram medidas nesta frente.

## Espaço e progressão

Manter NexusBlockout e grafo publicado. Comparar primeiro arena aberta com arena de obstáculos baixos no mesmo conjunto de anchors. Taxonomia proposta: aberta (prioridade/esquiva), obstáculos (LOS/alinhamento), ondas (gestão de pressão), Elite (compromisso/punição), desafio opcional (risco anunciado), boss (regra local) e respiro (recompensa/transição segura). A tabela completa de propósito, layout, composição, spawn, câmera, duração hipotética, recompensa, variação e custo está na etapa1; não são sete categorias automaticamente autorizadas.

Não permitir spawn dentro do jogador, surpresa fora da tela ou objetivo que dependa de inimigo inalcançável. Teto de vivos inclui invocados e reforços pendentes. Aviso→entrada→ativo→conclusão→ticket único precisa ser explícito. Obstáculo exige NavMeshPath/LOS/dash/oclusão validados; recompensa acontece após causalidade/feedback limpos. Extração só após boss; nunca converter abandono em vitória.

## Bíblia visual e matriz de contrajogo

Hipótese Malha de Memória: infraestrutura de processos que preservam e bloqueiam uma conexão com a Terra. Placas, canais e encaixes; piso limpo, núcleos pequenos, sinais hostis prioritários. Paleta não aprovada. Formas abaixo devem ser reconhecidas sem cor/nome:

| Forma candidata | Função e ação | Preparação/recuperação | Melee/ranged e reações |
|---|---|---|---|
| Fragmento quadrúpede | Swarm aproxima em arco e bote curto comprometido | Corpo baixo antes do bote; recompõe patas após avanço | Interromper startup elegível/desviar perpendicular; comum respeita stagger/push/pull/launch do perfil |
| Emissor bifurcado suspenso | Ranged procura LOS e dispara linha comprometida | Metades abrem e travam; fecha durante recovery | Encurtar distância/quebrar LOS; sem tiro invisível ou tracking após commit |
| Carretel baixo de seis apoios | Puller com braço/cabo e linha clara | Arma gancho/recolhe cabo, pull limitado e janela0 | Flanquear/sair da faixa; quebra interrompe pela autoridade existente; múltiplos não renovam lock |
| Bastião largo de placas | Heavy controla uma região com martelo | Levanta massa e compromete slam; lateral exposta | Punir flanco/ângulo; resistência e fallback Heavy, não imunidade universal |
| Árbitro alto de tripé | Elite de linha/varredura candidata | Braços anunciam faixa; recovery legível e janela de postura | Cruzar espaço livre/punir núcleo; proteção de retomada preservada, sem perma-stun |

Distâncias, navegação, coordenação, limites em multidão e ataques secundários justificados estão detalhados na etapa1. Não variar apenas HP/velocidade/dano. Coordenador melee existente é reutilizado; coordenação adicional ranged/controle requer validação específica, sem rewrite global de IA.

Elite atual é rank/afixo sobre conteúdo existente. Boss atual tem charge/slam/um-dois/onda e follow-ups abaixo de50%HP. Próximo boss só após contrato/storyboard/layout aprovado; preservar imunidades e stagger. Não criar uma mecânica exclusiva no estudo e declará-la entregue.

## Pipeline e implementação futura

Reutilizar raiz Actor/EnemyVariant, IDs/gerações, NavMeshAgent, dano/recibos/causal root, EnemyAI/EnemyCombatActions e ReactionController. Autorar por arquétipo visualRoot, forma, rig/Avatar adequado, poses/clips, materiais, origem de arma/aviso e hurtbox. Quadrúpede/disco/tripé podem usar rig genérico próprio; reutilizar o humanoide de67bones não pode apagar anatomia.

Concept turnaround → geometria em metros → UV/material → rig e clips completos → export/import preset → prefab isolado → testes de dano/nav/reação/pool → câmera normal → revisão. Blender MCP desconectado nesta sessão; arte final depende de artista ou addon ativo e autoria real. Primitivas atuais são nossas e não exigem asset externo; não têm qualidade de produção, texturas/rig/LOD finais.

Pooling atual de projéteis não é pooling de inimigos: encontros usam Instantiate/Destroy. Se houver necessidade medida, criar reset verificável de HP/geração/postura/resume/nav/Animator/afixos/listeners/summons, sem introduzir manager global. Custo de skinning/renderers e simultaneidade de telegraphs precisam de CPU/GPU/player8/32/64 reais; fixture Protocol do relatório0 não certifica essa escala.

## Backlog com gates e dependências

| Ordem | Trabalho proposto | Dependência / complexidade | Gate de saída |
|---|---|---|---|
|0 | Revisão física pull/fogo/tripla | Build atual, assistência explicitamente registrada; baixa execução/avaliação humana | Recupera movimento/dash, reconhece fogo sem zoom e vê aviso inimigo |
|1 | Sanear fixtures dos contratos aprovados | Ledger de31 falhas e baseline causal; média | Testes refletem contato real e catálogo atual, sem garantir RNG nem esconder falhas |
|2 | Escolher par de candidatos e uma arena | Revisão do estudo2; decisão de arte; média | Papel reconhecido sem cor/nome e vantagem espacial descrita pelo jogador |
|3 | Integrar um par com conteúdo3D próprio | Rig/animation/anchors/reactions/nav; média/alta | Ataque/aviso/HP/recovery coerentes e cleanup/reuso sem vazamento |
|4 | Ajustar comunicação e experimento de descoberta | Inventários e telemetria real; baixa/média para UI, média/alta para economia | Alternativas úteis por arma e tempo com transformação; aprovação antes de frequência/pesos |
|5 | Sala/onda/desafio e depois boss | Gates anteriores, scheduler/budget/autoria; alta | Teto de ameaças, progressão/tickets/nav e8/32/64 medidos, aprovação humana |

Nenhum item2–5 foi implantado em produção. Estimativas são relativas, não cronograma contratado. Q/E/R modular continua outra autorização; deixar dados vinculados a capacidades/identidade da ação ajuda a migração futura.

## Evidência, limitações e revisão

Correções: jobs e todos os resultados em relatório0;10/10 finais,4/4 visuais por rodada,5/5 snapshot/pool/crowd, baseline31 e seleção353 preservados. Auditoria: JSON de assets/runtime. Protótipo:1/1 final e cinco Game View/poses. Console não foi limpo; FastScriptReload/PackageManager registraram ocorrências ambientais. Scanner global ainda aponta script legado ausente em Playground e warning do sampler de teste, sem erros nos novos assets. Não afirmar regressão integral ou aceite artístico.

Arquivos da frente: PlayerActor.cs; TitanicFistsVisual.cs; novos TitanicFlameSilhouette.cs, RunFrontCriticalPlayModeTests.cs, RunFrontPublishedHookTests.cs, RunFrontPrototypeReviewTests.cs; `partial` em Gpr015ProductionAuditTests; árvore Prototypes/RunExperience; três relatórios, checkpoint, este plano, ledger/matriz/estado e artefatos. Outros diffs anteriores permanecem e foram autorizados para o commit/push conjunto pelo usuário. Skills aplicadas estão nos relatórios por atividade.

Revisão solicitada ao desenvolvedor: reconhecer o fogo na câmera normal; avaliar janela do gancho; escolher ou rejeitar a direção geométrica e o par inicial; escolher aberta/linhas partidas para integração; aprovar uma futura atividade de comunicação/economia com métricas de descoberta. Esta revisão será a base da próxima autorização, não uma continuação automática.
