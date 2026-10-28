# Inimigos — resposta a hits, papéis e telegraphs

**Direção de design vigente:** composição legível de ameaças, com papéis distintos e respostas de impacto apropriadas a comum/heavy/elite/boss. **Base técnica:** inspeção estática em 18/10/2026, `gameplayOverhaul`, HEAD `3a3f0ee` com alterações locais. Runtime descrito como `IMPLEMENTADO_NAO_VALIDADO` nesta consolidação: não houve execução de Unity, medição de timing ou aceite humano aqui. Ver [matriz de validação](../validation/01_MATRIZ_DE_VALIDACAO.md) para evidências históricas.

## Papéis e autoria encontrados

O [ArchetypeId](../../Assets/_Project/Scripts/Characters/Enemy/ArchetypeId.cs) enumera 16 identidades, e há [prefabs](../../Assets/_Project/Prefabs/EnemyArchetypes/) e [assets de autoria](../../Assets/_Project/ScriptableObjects/Enemies/Archetypes/) correspondentes. Existência do catálogo não prova que todos apareçam numa seed da run.

| Grupo | Identidades presentes |
|---|---|
| Melee | Rush, Grunt, Heavy, Charger |
| Ranged | Shooter, SpreadShooter, Sniper, Bomber |
| Controle | HazardCaster, Hooker |
| Suporte | Healer, ShieldSupport |
| Swarm | Swarm, Spawner |
| Especiais | Fragile, Mirror |

[EnemyArchetype](../../Assets/_Project/Scripts/Characters/Enemy/EnemyArchetype.cs) associa ID estável, `EnemyProfile`, traits, papel principal e template opcional. Os pares de arquivos como `Heavy.asset` e `HeavyArchetype.asset` têm responsabilidades distintas: perfil numérico/de reação e identidade/comportamento, respectivamente. Não são duplicatas documentais ou técnicas por semelhança de nome.

[EnemyVariant](../../Assets/_Project/Scripts/Characters/Enemy/EnemyVariant.cs) resolve esse perfil e o aplica nos caminhos de HP, dano, velocidade, ataques, postura e afixos existentes. Ele configura reação cedo em `Awake` e evita resetar postura já atingida quando `Start` completa o restante. ID fora do enum ou perfil ausente passa por fallback seguro em vez de inventar uma nova categoria; a referência serializada individual não verifica duplicatas de um catálogo global. Healer, ShieldSupport, Spawner, HazardCaster e Mirror recebem componentes de comportamento próprios; prioritários recebem marcador.

Raridade visual (`Normal`, `Magic`, `Rare`) e rank/categoria de reação são eixos diferentes. [EnemyVisualStyle](../../Assets/_Project/Scripts/Characters/Enemy/EnemyVisualStyle.cs) usa branco/azul/amarelo e boss laranja, com escalas específicas; essa cor não substitui a configuração de resistência. `EnemyVariant` compõe afixos como gelo, aceleração e resistência pelo perfil/prefab.

## IA, telegraph e ataques atuais

[EnemyAI](../../Assets/_Project/Scripts/Characters/Enemy/EnemyAI.cs) coordena percepção, navegação, aproximação, retreat/standoff, jitter de cadência e ataque. A configuração é por inimigo; não afirmar que todos recuam ou usam a mesma distância. Controle de postura interrompe o caminho ofensivo apropriado, com retorno à navegação após liberação.

[EnemyCombatActions](../../Assets/_Project/Scripts/Characters/Enemy/EnemyCombatActions.cs) executa socos/slam, charge, frost bolt, shockwave, aimed/spread/sniper/lob shot, hook e telegraph de colocação de hazard. O aviso e a execução de impacto têm caminhos separados; `HookLine` desloca com dano zero no telegraph, e `HazardPlace` sinaliza a área antes do comportamento de hazard ativá-la. A leitura visual e correspondência com hitbox continuam dependendo de observação real.

[SwarmAttackCoordinator](../../Assets/_Project/Scripts/Characters/Enemy/SwarmAttackCoordinator.cs) mantém um pool de slots melee por encontro, independente da arma. IA solicita token antes do telegraph e o devolve no cancelamento/interrupção. Ranged não consome esses slots. A referência é opcional: sem coordenador atribuído/encontrado no pai, o inimigo ataca sem esse limite; não tratar o default de três slots como garantia global de toda cena.

O [SectorBoss](../../Assets/_Project/Scripts/Characters/Enemy/SectorBoss.cs) substitui a IA comum do Guardião do Núcleo: aproximação, charge, HeavySlam, DoublePunch e Shockwave. Abaixo de 50% de HP abre follow-ups condicionais e reduz intervalo entre ciclos. Interrupção passa pelo executor e cancela a sequência. Isso descreve o padrão codificado, sem atestar justiça, dificuldade ou legibilidade aprovadas.

## Contrato de resistência e postura

A intenção permanece separar reação visual, micro-stagger, stun, quebra, launch, knockdown, push e knockback; vocabulário e pipeline em [combate](01_COMBATE.md).

A implementação atual usa [CombatReactionController](../../Assets/_Project/Scripts/Characters/Combat/CombatReactionController.cs), [ReactionConfiguration](../../Assets/_Project/Scripts/Characters/Combat/ReactionConfiguration.cs) e [PostureReactionState](../../Assets/_Project/Scripts/Characters/Combat/PostureReactionState.cs):

- `EnemyProfile` define máximo/recuperação de postura e resistências; `ReactionCategory` distingue SmallMob, MediumMob, Elite e Boss, além do rank. Categoria não é deduzida só pelo tamanho visual.
- Dano de postura elegível reduz o medidor. Ao quebrar, o estado restaura postura, abre vulnerabilidade e estabelece imunidade antes de notificar callbacks, limitando reentrada.
- Defaults de quebra: SmallMob → KnockUp; MediumMob → Knockdown; Elite/Boss → Stun. São consequências da quebra, não autorização para todo básico lançar automaticamente. Overrides/perfis e resistências podem alterar o resultado.
- [BreakEffectResistance](../../Assets/_Project/Scripts/Characters/Combat/BreakEffectResistance.cs) degrada KnockUp/Knockdown imunes para Stun ou None; Knockback imune vira None. Boss exige autorização explícita para launch/knockdown/knockback. Não substituir fallback por deslocamento indiscriminado.
- Efeitos têm leases/tokens e limite contínuo de controle; política de reaplicação pode ignorar/substituir/estender dentro do limite. Relógio de postura pausa com pausa/hitstop consultados pelo controller.
- Abertura restritiva de elites/ranks especiais/boss separa lock de vulnerabilidade de stun físico. Há proteção de retomada até uma ação ofensiva elegível completar, com watchdog/fim por inelegibilidade, para evitar re-quebra permanente.
- Push/movimento físico, pose visual de launch/knockdown e parada do NavMesh têm donos/cleanup separados. Morte/disable devem liberar apenas o estado possuído.

## Direção a preservar e questões abertas

O fluxo-alvo é `AcquireTarget → Tell → Windup/Telegraph → Commit/Active → Recovery → Resume`, com aviso espacial/temporal coerente com animação e hitbox. O código tem executores e gates para esses componentes; aprovação temporal/visual continua pendente nesta revisão.

A matriz [DefaultWeaponEnemyMatrix](../../Assets/_Project/ScriptableObjects/Combat/DefaultWeaponEnemyMatrix.asset) e [WeaponEnemyResponseResolver](../../Assets/_Project/Scripts/Weapons/WeaponEnemyResponseResolver.cs) descrevem respostas por arma/papel e fallback quando falta célula. Isso não prova que a pessoa jogando perceba a relação nem que o alvo esteja vulnerável a qualquer efeito da skill. Preservar manopla como pressão/stance, lança como seleção de zona e arco como prioridade/distância.

Falhas a capturar em tarefa de gameplay: aviso divergente da hitbox; ataque fora de tela sem leitura; ataque durante controle; boss preso em CC; navegação perdida após push; alvo preso no ar; projétil/hazard órfão no fim do encontro. Ausência de gravação não permite declarar essas falhas presentes ou resolvidas.

## Evidência e aceite

### Projéteis entre cenas — VS-01.1, 21/10/2026

[EnemyProjectile](../../Assets/_Project/Scripts/Characters/Enemy/EnemyProjectile.cs) cria instâncias pela factory existente, com visual esférico reutilizado; não depende de um prefab serializado de projétil. AimedShot/SniperShot, SpreadShot e LobShot passam pelo mesmo adaptador [ComponentPool](../../Assets/_Project/Scripts/Core/Pooling/ComponentPool.cs). `ProjectileMotion` continua dono das decisões de trajetória/resolução; `EnemyProjectile` configura dono/alvo, assina morte do dono e devolve ao pool no término. Cancelar o ataque antes da emissão impede o tiro; cancelar depois não apaga o projétil já emitido.

O pool estático pode sobreviver à cena, mas sua raiz e seus filhos são objetos da cena. A reprodução confirmou referências Unity destruídas na fila após FirstSector→Nexus; o próximo Acquire devolvia null e Spawn falhava em `projectile.transform`. Não foi falha de prefab ou de referência de origem do ataque. `PoolResetRegistry` continua responsável pelo início de Play/domínio, não por cada troca de cena.

A correção fica no adaptador: se havia instâncias criadas e a raiz Unity foi destruída, o próximo `Acquire` recria somente o armazenamento daquela raiz antes de adquirir. Reuso dentro da cena permanece; a factory volta a criar raiz/instâncias na cena ativa. Contagens do adaptador referem-se à raiz vigente, não à soma histórica de cenas destruídas. O descarte é **preguiçoso no Acquire**: o snapshot no Nexus pode ainda mostrar a raiz/fila destruídas até a próxima aquisição. Não tornar o pool persistente nem adicionar controlador global de lifetime para contornar esse contrato.

Regressão nova em [EnemyProjectileLifecyclePlayModeTests](../../Assets/_Project/Scripts/Tests/PlayMode/EnemyProjectileLifecyclePlayModeTests.cs): reprodução vermelha sem a correção e cinco casos aprovados após ela, com descarregamento real de cenas, emissões AimedShot/SniperShot em três ciclos, tiros ativos/estacionados, morte, troca de dono/alvo, listeners e cancelamento. Fixture de lifecycle não equivale a partida natural; percurso publicado e resultados detalhados ficam na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md). Nenhuma mudança em dano, velocidade, cooldown, telegraphs ou decisão ofensiva.

Há testes fontes de [arquétipos](../../Assets/_Project/Scripts/Tests/PlayMode/ArchetypeIntegrationTests.cs), [variedade](../../Assets/_Project/Scripts/Tests/PlayMode/EnemyVarietyIntegrationTests.cs), [execução de ataques](../../Assets/_Project/Scripts/Tests/PlayMode/EnemyAttackExecutionTests.cs), [ataques físicos](../../Assets/_Project/Scripts/Tests/PlayMode/EnemyPhysicalAttackTests.cs) e [postura](../../Assets/_Project/Scripts/Tests/PlayMode/PostureReactionPlayModeTests.cs). Esta consolidação verificou a existência/leitura dos contratos, sem executar esses testes ou aprovar sua cobertura.

Aceite requer arena com entidades reais comum/heavy/elite/boss, timeline e vídeo; medir tell, active, interrupção, recuperação e retomada sob pressão. Teste de helper ou prefab presente não comprova distribuição na run, leitura em swarm nem aprovação pelo desenvolvedor.

## Interação e apresentação — GI1.6, 25/10/2026

Atualização da inspeção histórica acima: houve execução Unity e comparação real nesta iteração; resultados e limitações na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md). A sessão humana da GI1.5 foi preservada até o desenvolvedor encerrá-la por conta própria. Não houve novo aceite humano dos inimigos.

### Autoridades atuais

- EnemyVariant/EnemyProfile resolvem autoria, rank/categoria, resistências e valores de postura; raridade visual não substitui esses contratos.
- EnemyAI possui percepção, aproximação/posicionamento, facing, cadência e slot melee. EnemyCombatActions e EnemyAttackExecution possuem preparação, aviso, contato, projétil e recuperação. SectorBoss conserva sua decisão ofensiva própria.
- CombatReactionController/PostureReactionState possuem reação, postura, controle e proteção de retomada. Stagger elegível pede cancelamento pelo EnemyAI somente na janela existente; quebra usa o caminho de interrupção próprio. Um flinch GetHit não significa stun ou cancelamento.
- SkillAnimationPlayer possui o graph de apresentação. Desde GI1.6, EnemyCombatActions reaplica em LateUpdate somente sua amostra já selecionada enquanto `_busy`; não avança relógio, escolhe outra animação nem emite impacto. Cancel/complete continuam liberando graph/ownership. Isso corrige perda da pose após avaliação do AnimatorController, sem trocar prioridades de reação/ataque.

| Representante real | Ação e movimento | Reação/postura e continuidade |
|---|---|---|
|Grunt, Normal/SmallMob|Punch com windup autorado0,40 s, recuperação corporal0,30 s; EnemyAI acrescenta recuperação/cadência existentes. Prefab escala0,5: elegibilidade melee1,075 m, área do soco1,20 m; preferência autorada2,20 m.|Postura100 no cenário. Contribuição Stagger positiva pode interromper durante windup, sem hard CC automático. GetHit próprio reinicia e retorna à locomoção; interrupção libera ataque/slot. Na captura passiva não chegou a iniciar ofensiva em12 s, apesar de navegar.|
|Shooter, Normal/SmallMob|AimedShot: preparação0,25 s, tiro capturado no fim e recuperação0,30 s; standoff/retreat pertencem à IA. Testado com prefab real em fixture de apresentação.|Perfil próprio, postura80; mesmo gate de interrupção elegível. Não forçar GetHit sobre ataque/recuperação que mantém ownership. Após a correção, pose e um único contato de projétil passaram no teste.|
|Sniper, Normal/SmallMob no cenário|SniperShot: faixa longa, aviso de1 s, projétil e recuperação0,45 s. Observado disparar, recuperar e voltar a disparar na produção.|Postura60 observada. O aviso usa a direção inicial, mas o disparo mira de novo no fim: diagnóstico com alvo móvel mediu90° entre ambos. Política de mira exige decisão autoral.|

Elite/Boss foram cobertos por fixtures de proteção/retomada e quebra, sem nova captura natural desses ranks. Resistências, limiares, imunidade, cancelamento e proteção de uma ação ofensiva não foram alterados.

### Problemas e decisões pendentes

1. **Aproximação melee não vira ameaça no cenário parado.** Grunt manteve distância mínima1,227 m, acima da elegibilidade1,075 m, por12 s sem ataque/controle; houve navegação em718 amostras. Preferência2,20 m/tolerância0,50 m conflitam com a banda efetiva curta. Isso demonstra sintoma e conflito de autoria; influência de separação, root motion e rotação/nav ainda precisa ser isolada. Proposta preferida para próxima etapa: garantir entrada na banda melee antes de voltar ao posicionamento preferido, com margem/tolerância compatíveis. Alternativa: rever alcance/geometria do soco para a distância desejada. Ambas alteram comportamento/ameaça e dependem de aprovação; nenhuma foi implementada.
2. **Pose ofensiva não sobrevivia até o render — corrigido.** Grunt e Shooter perderam cerca de21° da pose autorada no braço em fixture real. A única correção de gameplay/apresentação desta tarefa é a reaplicação pelo owner existente, sem novo clip, controller ou efeito de controle.
3. **Sniper pode atirar fora da direção anunciada.** A linha é construída no início e permanece estática; Face(target) no fim redireciona o projétil. Alternativa A: aviso acompanhar a mira final; B: disparo seguir a direção anunciada, com regra explícita de travamento. A preserva a perseguição atual do alvo; B muda contrajogo/balanceamento. Não implementar uma política de mira implicitamente. O diagnóstico registra a geometria, não aprova nenhuma das alternativas.

Recomenda-se GI1.7 focada na aproximação/pressão do Grunt após decisão autoral, seguida da coerência aviso→tiro do Sniper. Para Bow2.0 e Spear2.0, preservar o mesmo contrato de HP/postura/resistência; resolver ameaça e leitura do inimigo antes de compensar com dano, alcance ou CC das armas. O ajuste visual é compartilhado pelas interações das três armas, sem redesign de kit. Mapa, boons e Nexus continuam fora deste recorte.

## Aproximação melee — GI1.7, 25/10/2026

A tarefa GI1.7 autorizou corrigir aproximação sem aumentar alcance ou alterar balanceamento. O achado anterior do Grunt foi isolado: o destino preferido de2,20 m, com tolerância0,50 m, pode autorizar recuo enquanto o ataque ainda exige distância entre centros de1,075 m. Ao entrar/sair da tolerância, a IA alterna sugestão preferida e chase com offset; navegação continua, mas não chega à faixa. Na reprodução de produção, houve323 sugestões de reposicionamento, todas a2,20 m, caminhos completos e stoppingDistance0; distância mínima1,227 m e nenhum ataque em12 s. Não era um gate de facing impedindo um ataque já elegível.

Ablação em fixture de60 Hz, com prefab/escala/perfil/agent reais e receptor sintético com agente: configuração original não emite; remover somente as sugestões permite três emissões em6 s, preservando avoidance, collider, root motion e offset. Remover também o offset continua permitindo três. Isso identifica o conflito de posicionamento como causa suficiente do bloqueio reproduzido; não significa que todos os efeitos de multidão/root motion foram universalmente resolvidos.

**Correção restrita:** EnemyAI fornece `PositioningAttackRange` somente para Grunt, rank Normal, categoria SmallMob. PreferredDistanceLayer limita a preferência efetiva a90% do orçamento planar do alcance existente (raiz de alcance²−diferença vertical²), reaplica esse limite após a separação e limita a tolerância à margem restante. No piso plano: alvo0,9675 m, tolerância máxima0,1075 m, elegibilidade preservada1,075 m e cone preservado1,20 m. São limites derivados em runtime; o profile2,20 m não foi regravado. A separação continua sugerindo ângulos, e colisão/avoidance/NavMesh continuam decidindo o movimento realizável. Não há escrita de Transform, novo controlador, aumento de hitbox, velocidade ou dano. EnemyAI continua validando a rota completa com agentType/areaMask/bounds antes de aplicar a sugestão.

Percepção combina distância planar da checagem de alcance com o gate melee3D; no cenário plano são iguais. A elegibilidade e o cone usam centros, sem somar raio do alvo; o raio do NavMeshAgent pertence à navegação, não é um bônus de alcance. A correção preserva essa convenção. Normal de outros arquétipos, Elite/Legendary/Boss e ranged mantêm a política anterior. Somente o destino de aproximação foi restringido; alcance e geometria do golpe não mudaram.

Na mesma FirstSector/nó1/seed1701, o Grunt agora inicia windup em0,867 s a0,936 m, confirma12 HP em1,267 s, termina apresentação em1,567 s e libera a rotina de recuperação em1,917 s; novo windup em3,083 s. Cinco contatos em12 s; a sexta preparação começa no final da janela. Dano12, windup0,40 s, recuperação corporal0,30 s, recuperação da IA0,35 s e cadência existente foram preservados. Comparação em movimento e limites na matriz. Capturas automáticas e fixtures não constituem aprovação humana.

**Próximo recorte proposto, GI1.8:** manter a reprodução do Sniper e discutir tracking inicial, instante explícito de aim-lock e aviso/projétil coerentes após compromisso. É proposta de design; targeting do Sniper não mudou na GI1.7. Outros arquétipos melee precisam de tarefa/observação própria antes de ampliar a política restrita ao Grunt.

## Compromisso do Sniper — GI1.8, 25/10/2026

A GI1.8 autoriza o contrato aquisição da mira → compromisso no aviso visível → windup existente → projétil na direção anunciada → recuperação. Não há fase nova de tracking, lock percentual ou atraso adicional. Movimento anterior à execução entra na aquisição inicial; depois de a linha aparecer, o Sniper não redireciona o tiro. O windup de1 s, recuperação corporal0,45 s, cadência, dano, range26 m, velocidade24 m/s e raio0,35 m permanecem iguais.

**Causa confirmada em duas etapas:** SniperShot construía a lane com Face(target)/MuzzleOrigin no começo e voltava a calcular ambos na emissão. Além disso, EnemyProjectile.SetupStraight recalculava a direção para o alvo, mesmo quando Spawn recebia uma direção. Só remover o segundo Face não seria suficiente. Regressão nova com prefab/perfil reais e receptor sintético mediu90° de divergência e um contato de21,6 HP após movimento durante windup; falhou antes da correção e passou depois com0°/zero contatos, uma esquiva legítima.

**Implementação:** SniperShot reutiliza os mesmos locals origin/forward da linha no Spawn e passa lockDirection:true. EnemyProjectile.Configure/Spawn aceitam essa opção com default false; SetupStraight usa o facing fornecido somente quando explicitamente solicitado. Não guarda um modo persistente entre aquisições do pool. AimedShot, SpreadShot, LobShot e demais consumidores mantêm seu encaminhamento anterior. EnemyAttackExecution continua sendo dono do aviso e da autorização; projétil continua dono de viagem, colisão, HP único e cleanup. Não há novo sistema de targeting/telegraph ou bus.

O predicate existente de LoS para o alvo atual continua podendo cancelar antes da emissão, junto com validade/controle. Isso não cria tracking: cancelar não autoriza trocar a trajetória anunciada. Cancelamento pré-emissão remove aviso e impede o tiro; após emissão legítima, cancelar a ação não apaga o projétil existente. Pausa/hitstop conservam a direção comprometida; morte/destruição do alvo encerra preparação sem projétil órfão. Reconfiguração do mesmo projétil e descarregamento/reentrada em cenas foram cobertos nos testes descritos na matriz.

Na comparação FirstSector/nó1/seed1701, mesmo movimento/câmera/posições/perfis, os dois Snipers emitiram nos mesmos frames166/232: divergências20,983° e5,406° antes,0° em ambos depois. Um contato ranged21,6 HP anterior tornou-se esquiva sem HP ranged; os dois contatos melee de12 HP permaneceram iguais. Alvo parado ainda recebe uma única transação com o dano autorado. Isso comprova coerência mecânica no recorte testado, não aprovação humana de pressão/legibilidade. Próximo passo recomendado: playtest curto de grupo Grunt/Sniper antes de ajustar timings ou ampliar o trabalho a outros arquétipos.

## Encontros mistos — GI1.9, 25/10/2026

O desenvolvedor optou por realizar a sessão física depois. [Roteiro A–D](../validation/04_CHECKLIST_PLAYTEST.md#gi19--encontros-mistos-sessão-física-de-812-minutos) cobre vários melees, composição melee/ranged, pressão da Manopla e três encontros da rota real. Não houve nova partida, captura ou correção nesta preparação. A definição possui caminhos válidos, mas FirstSector randomiza composição por seed; grupo autorado fixo ou seed de teste anterior não garante o roster humano futuro. Slot melee existente de capacidade3 não foi alterado nem tratado como prova de ameaça satisfatória.

Postura ofensiva, aproximação do Grunt e direção anunciada do Sniper são comportamentos tecnicamente verificados a preservar, não um aceite de diversão. Pressão coletiva, leitura simultânea e ritmo/uso do espaço são perguntas pendentes. Não há três novos defeitos de encontro comprovados nem origem atribuída a mapa/composição nesta etapa. Boons/mapas/Bow2.0/Spear2.0 devem receber incidentes e decisões reais dessa sessão antes de serem propostos como solução para um incômodo ainda não localizado.

**Feedback humano posterior:** o desenvolvedor destacou positivamente o spawner por prolongar a luta e os ranged em geral. Básicos pareceram pouco ameaçadores pela preparação longa e atração curta; alguns ranged produziram efeito sem dano aparente, incluindo um ataque vindo de cima. Esclareceu ter permanecido dentro da área sem perder HP e não identificar o executor porque todos parecem iguais. Bomber/LobShot ou HazardCaster são caminhos existentes a inspecionar, não identidades comprovadas desse incidente. A maioria dos ataques foi compreensível e evitável intencionalmente, sem aprovação específica do Sniper. Distribuição espalhada não foi considerada ruim por si só, embora não tenha gerado decisão útil percebida. Registro integral no checklist/matriz, sem nova captura ou regressão.

SpawnerBehavior atualmente produz novos Swarms com intervalo/teto de vivos; a sugestão humana de reviver mortos é uma alternativa de design, não comportamento implementado ou aprovado. A baixa ameaça percebida dos básicos não demonstra falha de GI1.7 nem justifica reduzir windup/aumentar sightRange sem proposta. Mapas/conexões, boons e mobilidade por dash receberam críticas humanas específicas e permanecem recortes separados. Priorizar a reprodução do efeito ranged sem consequência antes de alterar regras de encontro.
