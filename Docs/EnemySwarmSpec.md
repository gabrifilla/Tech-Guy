# Tech-Guy — Spec de inimigos e composições de swarm

Status: proposta pronta para implementação incremental. Nenhuma tarefa abaixo está marcada como implementada.

Fonte: `Tech-Guy_Inimigos_e_Composicoes_de_Swarm.md`, fornecido pelo usuário. Esta spec transforma o catálogo em entregas executáveis. Regras que resolvem alternativas do catálogo são **decisões propostas**, ajustáveis após playtest. Valores numéricos são pontos de partida, não balanceamento aprovado.

## 1. Resultado esperado e escopo

Criar encontros em que tipos diferentes mudam a movimentação, a prioridade de alvos e as oportunidades de combo. Cada arquétipo deve ter função, preparação perceptível, ação característica e uma resposta possível do jogador.

Primeira entrega jogável: **8 Rush + 4 Grunts + 3 Shooters**, em uma sala de teste, preservando o ciclo de portas, recompensa e conclusão da sala. Depois ampliar para os 16 arquétipos iniciais sugeridos na fonte, validar suas composições e só então avançar para a segunda leva.

Inclui comportamento, integração com postura, feedback funcional, composição, ciclo de vida e desempenho. Arte final, lore, novos modelos, layout procedural e reformulação dos bosses ficam fora deste escopo. O boss atual deve continuar funcionando; o catálogo não define novos bosses. Animações, efeitos e sons mínimos para entender os ataques fazem parte de cada entrega.

## 2. Base existente e mudanças necessárias

Inspeção do projeto em 28/09/2026:

| Sistema existente | Reaproveitamento / lacuna |
|---|---|
| `EnemyAI` | Percepção, perseguição e interrupção; hoje todos convergem para a mesma lógica de aproximação. Separar políticas de posicionamento. |
| `EnemyAttackPatterns` | Atualmente escolhe golpes por Haste/Frost/Guard. Migrar a identidade do repertório para o arquétipo. |
| `EnemyCombatActions`, `EnemyAttackExecution` | Socos, pancada, investida, projétil e onda; reaproveitar execução cancelável e extrair operações conforme necessário. |
| `EnemyProfile`, `EnemyVariant`, `EnemyStatAffix` | Preservar raridade e modificadores; não usar raridade como identidade de comportamento. |
| `EnemyRank`, `CombatReactionController` | Preservar regras de stagger, quebra de postura, lançamento e controle. |
| `Actor` | Eventos `Died`, `DamageReceived`, `HealthChanged`; redução de dano por fonte. Cura parcial, escudo e ressurreição precisam de contratos próprios. |
| `FirstSectorDirector` | `Encounter.enemies` é um array fixo; conclusão consulta essa lista. Precisa de registro dinâmico antes de invocações. |
| `EncounterGates`, `RunBoons` | Preservar portas, escolha única de recompensa e progressão. |
| `EnemyCombatFeedback`, `CombatGroundRing`, `SkillAnimationPlayer` | Base de feedback e animação. Diferenciar intenções e suportes sem inundar a tela. |

Os prefabs atuais e alterações locais devem ser preservados. Migração deve ser explícita, idempotente e testada; builders não podem recriar cenas existentes para aplicar a mudança. Documentação antiga pode descrever versões anteriores: o código e os testes vigentes são a referência de integração.

## 3. Contratos de gameplay

### Identidade e execução

- **Arquétipo** determina objetivo tático, posicionamento e repertório. **Raridade** escala atributos/recompensas. **Afixo** adiciona ou modifica uma mecânica. **Rank** define resistência a controles. São eixos separados.
- Haste não transforma automaticamente Shooter em Charger; Frost não transforma todo melee em ranged. Manter um adaptador de compatibilidade para os prefabs antigos durante a migração.
- Fluxo: reposicionar → preparar → comprometer direção/alvo → executar → recuperar. Cada golpe declara quando deixa de acompanhar o jogador.
- Dano acompanha contato, trajetória ou tick da área. Telegraph e hitbox usam a mesma configuração. Cancelamento remove avisos e efeitos ainda não disparados.
- Morte/disable cancela preparação, canalização e vínculos. Projéteis já disparados podem concluir o voo; todos os efeitos são limpos ao encerrar a sala. Esse contrato precisa ser uniforme e testado na migração da execução atual.
- Pausa congela temporizadores. Reutilização por pool reinicia vida, postura, alvo, cooldown, buffs, efeitos e assinaturas de eventos.

### Justiça em grupos

- Proposta inicial: até **3 ataques melee em execução**, **2 especiais perigosos** e **1 controle forçado** ao mesmo tempo por sala. Configurável; inimigos sem autorização continuam se posicionando. Projéteis comuns têm limite próprio.
- Coordenador distribui oportunidades com espera máxima/fila justa; devolve vagas em conclusão, cancelamento, morte e disable. Uma ameaça não pode monopolizar a fila.
- Haste reduz cadência, mas preserva duração mínima de aviso dos especiais. Não comprimir tudo pelo mesmo multiplicador.
- Sniper, salto, gancho e teleport têm preparação mínima proposta de 0,8 s; controle forçado começa com aviso de 1 s. Ajustar em playtest com as armas atuais.
- Após root/puxão/empurrão, conceder 1 s de proteção contra novo controle forçado. Dano continua válido. Deslocamentos não atravessam paredes/portas nem retiram o player do NavMesh.
- Ataques perigosos exigem fonte visível ou aviso direcional fora da tela. Não criar dano instantâneo na posição atual do player.
- Spawns usam pontos válidos e aviso; proposta de distância mínima de 3 m do player. Se não houver ponto seguro, adiar com limite de tentativas; nunca usar posição inválida como fallback.
- Reservar rotas transitáveis: paredes/áreas não podem fechar todas as saídas da região onde o jogador está. Validar geometria, não apenas limitar quantidade.

### Buffs, mortes e recompensas

- Buffs registram fonte e duração. Remover uma fonte não apaga as demais nem deixa modificadores acumulados após pooling.
- Proposta: auras iguais usam a maior intensidade, não somam; escudo não acumula sem limite; cura não excede vida máxima nem revive.
- Invocados e ressuscitados não geram moedas/recompensas de sala adicionais. On-kill de combate pode funcionar uma vez por vida válida; identidades de vida impedem eventos duplicados.
- Registro da sala inclui inimigos vivos, spawns pendentes e canalizações de ressurreição. Não concluir durante a janela entre morte do Carrier e nascimento dos filhos.
- Sala termina quando não há inimigos obrigatórios vivos nem geração pendente. Limpeza/despawn administrativo não conta como kill. Recompensa e abertura de portas acontecem uma única vez.

## 4. Arquitetura proposta

Nomes abaixo são propostas de implementação, não classes já existentes.

| Peça | Responsabilidade |
|---|---|
| `EnemyArchetypeDefinition` (ScriptableObject) | ID estável, papel, atributos base, perfil de postura, distâncias e referências a ataques/feedback. Dados compartilhados imutáveis em runtime. |
| `EnemyAttackDefinition` | Alcance, aviso, compromisso de mira, execução, recuperação, cooldown, limite de acertos e categoria de ameaça. |
| `EnemyBrain` + políticas pequenas | Decisão/posicionamento melee, distância, flanco e suporte; estado por instância. MonoBehaviour coordena navegação/animação. |
| Executor de ações | Evolução incremental de `EnemyCombatActions`; operações reutilizáveis sem um switch crescente para cada inimigo. |
| `EncounterDefinition` | Grupos, quantidades, papéis de spawn, ondas, limites e referências a arquétipo/raridade/afixos. |
| `EncounterRuntime` | Registro local de unidades, reservas de spawn, conclusão, limpeza e métricas. Injetado pelo diretor; sem singleton novo. |
| `EncounterThreatCoordinator` | Vagas de ataque, justiça da fila, limites de controle e áreas; escopo por sala. |
| Serviços locais de efeitos/spawn | Pool de projéteis/avisos/unidades e ciclo de vida explícito, introduzidos conforme medição. |

Código em `Assets/_Project/Scripts/Characters/Enemy`, `Characters/Combat`, `Core`, `Effects` e `UI`, conforme responsabilidade. Assets em `Assets/_Project/ScriptableObjects/Enemies`, `ScriptableObjects/Encounters` e `Prefabs`. Ferramentas em `Scripts/Editor`; testes nas pastas existentes. Preservar `.meta`, GUIDs e referências; não modificar terceiros.

## 5. Catálogo inicial — 16 arquétipos

Cada linha vira uma entrega com prefab/definition, comportamento, animação/feedback, teste de contrato e playtest em composição. Diferenças de vida/dano são secundárias ao comportamento.

| Arquétipo | Contrato da primeira versão | Resposta do jogador / aceite específico |
|---|---|---|
| Rush | Aproxima rápido e desfere um golpe curto; baixa vida/postura. Ocupa posições ao redor do alvo. | Flanquear/interromper; não formar uma pilha com todos atacando juntos. |
| Grunt | Sequência curta: dois golpes, pausa legível, golpe forte; interrompível. | Reconhecer a pausa e punir; alcance real acompanha animação. |
| Heavy | Avanço lento, ataque frontal pesado e pancada próxima; resiste a stagger leve, não a toda quebra de postura. | Sair do frontal ou quebrar postura; recuperação abre janela real. |
| Charger | Busca distância, fixa direção e corre; termina no obstáculo ou limite. | Esquiva lateral; errar gera vulnerabilidade. Sem empurrar aliados na V1. |
| Shooter | Mantém faixa de distância, tiro simples com linha de visão, recuo limitado. | Aproximar reduz sua eficiência; não foge eternamente nem atira através de parede. |
| Spread Shooter | Leque de projéteis com direção comprometida e espaços entre trajetórias. | Usar distância/posição para atravessar lacunas; projéteis não nascem sobre o player. |
| Sniper | Mira longa, linha/som progressivos, trava mira antes de tiro rápido; cooldown longo. | Desviar após compromisso ou interromper; parede bloqueia disparo. |
| Bomber | Lança em arco para posição registrada; marcação antecede a explosão. | Abandonar a área; projétil não acompanha a esquiva depois de lançado. |
| Hazard Caster | Cria área persistente de dano após aviso; duração e quantidade limitadas. | Reposicionar; ticks não dependem da taxa de quadros. Slow/silêncio são extensões posteriores. |
| Hooker | Gancho reto antecipado; acerto puxa até destino seguro e libera controle. | Esquivar/usar obstáculo; sem puxão atravessando paredes ou cadeia infinita de controle. |
| Healer | Busca aliado ferido em alcance, canaliza cura parcial visível, tenta se afastar do player. | Priorizar/interromper; não cura mortos nem fora de alcance/linha de visão. |
| Shield Support | Canaliza escudo temporário em um aliado; vínculo mostra fonte vulnerável. | Matar/interromper suporte remove proteção concedida; sem escudo ilimitado ou autoescudo na V1. |
| Swarm | Unidade muito frágil, ataque simples e baixa ocupação; quantidade limitada pela sala. | AoE/on-kill eficientes; mortes registradas uma vez, sem tempestade de UI. |
| Spawner | Canaliza criação de Swarm; limite de filhos vivos, total por sala e frequência. | Interromper/eliminar para parar crescimento; não impede fim da sala por reserva abandonada. |
| Fragile | Perfil de postura baixo sobre base melee, com maior janela aérea. | Launch/juggle funcionam; morte no ar conclui corretamente e não deixa corpo bloqueando progressão. |
| Mirror | Defesa frontal reduz projéteis; costas, AoE e quebra de postura abrem defesa. | Flanquear/quebrar defesa. Reflexão de projéteis fica posterior à V1 para evitar ricochetes recursivos. |

## 6. Plano de execução e tasks

Todos os itens começam pendentes. Cada etapa só avança após seu aceite. Uma task deve caber em uma mudança revisável; se exigir muitos sistemas, dividir mantendo seu ID como referência.

### E0 — Baseline e arena de validação

Dependências: nenhuma. Entrega: reprodução segura do estado atual e cenário de comparação.

- [ ] **SW-001** Registrar testes existentes, prefabs, armas, parâmetros e comportamento atual do FirstSector/boss; registrar falhas preexistentes separadamente.
- [ ] **SW-002** Criar arena de teste isolada com obstáculos, portas, pontos de spawn e seleção de composição; não substituir FirstSector.
- [ ] **SW-003** Capturar baseline de CPU/GPU, GC, agentes, efeitos e tempo de sala com 15/30/60 unidades; registrar hardware, resolução e build de teste.

Aceite: entrar/sair da arena não altera assets compartilhados; baseline reproduzível; cenas atuais continuam acessíveis.

### E1 — Dados e migração compatível

Dependências: E0. Entrega: arquétipo independente de raridade/afixo.

- [ ] **SW-010** Implementar definitions de arquétipo/ataque, validação de referências e estado por instância.
- [ ] **SW-011** Separar escolha de ataque e posicionamento da execução; reaproveitar cancelamento, animação e colisões atuais.
- [ ] **SW-012** Criar adaptador para prefabs existentes; remover associação automática Haste→Charge/Frost→ranged somente nos migrados.
- [ ] **SW-013** Criar migração idempotente via Editor com relatório de objetos alterados e referências faltantes; manter boss no fluxo atual.

Aceite: dois inimigos compartilham definition sem compartilhar vida/cooldowns; reaplicar perfil não acumula atributos; variantes antigas e boss passam regressão.

### E2 — Primeira composição jogável

Dependências: E1. Entrega: Pressão básica, 15 unidades.

- [ ] **SW-020** Implementar Rush, ocupação melee e ataque curto com antecipação corporal.
- [ ] **SW-021** Implementar Grunt e sequência interrompível com pausa reconhecível.
- [ ] **SW-022** Implementar Shooter, linha de visão, faixa de distância e recuo com limite/fallback quando encurralado.
- [ ] **SW-023** Implementar coordenador local de vagas melee/especiais; testar cancelamento e ausência de starvation.
- [ ] **SW-024** Criar `EncounterDefinition` e executar 8 Rush + 4 Grunts + 3 Shooters na arena, com posições coerentes e seed reproduzível.
- [ ] **SW-025** Playtest com manopla, lança e arco; gravar confronto, ajustar leitura e medir comportamento com/sem Shooter vivo.

Aceite: matar Shooters reduz pressão à distância de forma perceptível; front line não vira fila imóvel; player consegue alcançar ranged; cada arma conclui a sala. Esta é a primeira parada para avaliar a direção antes de ampliar o catálogo.

### E3 — Burst, frontline e mobilidade

Dependências: E2. Entrega: Proteção ranged e um encontro de investidas.

- [ ] **SW-030** Implementar Heavy e integrar resistência com `CombatReactionController`.
- [ ] **SW-031** Migrar investida para Charger; testar colisão contínua, obstáculo, erro e recuperação.
- [ ] **SW-032** Implementar Spread Shooter com lacunas esquiváveis e limite de projéteis.
- [ ] **SW-033** Implementar Sniper com aviso audiovisual, trava de mira e linha de visão.
- [ ] **SW-034** Montar 4 Heavy + 4 Shooter + 2 Sniper; testar limite de especiais e janela de aproximação.

Aceite: Heavy protege espaço sem ser invulnerável; Charger não causa dano além da trajetória; dois Snipers não disparam inevitavelmente no mesmo instante.

### E4 — Áreas e disrupção

Dependências: E3. Entrega: pressão territorial com escape legível.

- [ ] **SW-040** Implementar Bomber, arco, ponto de impacto fixo e explosão única.
- [ ] **SW-041** Implementar Hazard Caster com ticks, limite de zonas e limpeza na conclusão da sala.
- [ ] **SW-042** Implementar serviço de deslocamento forçado seguro e proteção breve contra controles consecutivos.
- [ ] **SW-043** Implementar Hooker sobre esse serviço, com interrupção e bloqueio por cenário.
- [ ] **SW-044** Validar composição provisória 6 Rush + 2 Bomber + 2 Hazard Caster; documentar como versão sem Mine Layer da fonte.

Aceite: área avisada coincide com dano; há espaço/tempo para escapar; gancho não atravessa portas nem deixa input/agent travados após morte, pausa ou interrupção.

### E5 — Suporte e prioridade de alvo

Dependências: E4. Entrega: Proteção de suporte.

- [ ] **SW-050** Adicionar cura parcial e escudo com fonte, duração, eventos e limites; separar cura de ressurreição.
- [ ] **SW-051** Implementar Healer, seleção de feridos, recuo e canalização cancelável.
- [ ] **SW-052** Implementar Shield Support, escolha de aliado e vínculo visual removível.
- [ ] **SW-053** Implementar regras de sobreposição/remoção por fonte e feedback agrupado de buffs.
- [ ] **SW-054** Montar 8 Grunts + 2 Heavy + 1 Healer + 1 Shield Support; comparar eliminar suporte primeiro versus ignorá-lo.

Aceite: suporte é identificável sem tooltip; cura nunca ressuscita; matar a fonte remove somente seus efeitos; nenhum ciclo de proteção torna aliados invulneráveis.

### E6 — Swarm, geração e conclusão dinâmica

Dependências: E5. Entrega: invocações compatíveis com progressão/recompensas.

- [ ] **SW-060** Introduzir registro dinâmico por sala, reservas de spawn e contagem por eventos; adaptar `FirstSectorDirector` preservando fluxo antigo.
- [ ] **SW-061** Implementar Swarm com custo visual/CPU reduzido; introduzir pool com reset completo e testes de reutilização.
- [ ] **SW-062** Implementar Spawner, limite de filhos/total e cancelamento da geração pendente.
- [ ] **SW-063** Definir identidade de vida, política de moedas/on-kill e separar morte de despawn administrativo.
- [ ] **SW-064** Testar 2 Spawner + 10 Swarm como versão provisória de Infestação sem Carrier; matar Spawner durante canalização e último inimigo durante nascimento.

Aceite: sem conclusão precoce, sala eternamente presa, recompensa duplicada ou geração ilimitada; todos os inimigos/efeitos/buffs são limpos ao morrer, extrair ou reiniciar.

### E7 — Postura e defesa direcional

Dependências: E6. Entrega: conjunto inicial de 16 arquétipos completo.

- [ ] **SW-070** Implementar Fragile como perfil de postura, preservando launch, air juggle e morte aérea.
- [ ] **SW-071** Implementar Mirror com redução frontal, abertura por postura e exceção de AoE; testar classificação e direção do dano.
- [ ] **SW-072** Validar arena provisória 12 Fragile + 3 Grunt (sem Counterweight) e formação com Mirror/Heavy/Shooter.
- [ ] **SW-073** Validar todos os 16 arquétipos isolados e em pelo menos uma composição; registrar resposta esperada do jogador e vídeo curto por tipo.

Aceite: builds de combo têm alvos apropriados; Mirror não invalida permanentemente o arco; nenhum arquétipo depende apenas de mais vida/dano para se distinguir.

### E8 — Integração de campanha e desempenho

Dependências: E7. Entrega: primeira versão integrada do sistema.

- [ ] **SW-080** Selecionar composições para progressão do FirstSector, introduzindo funções gradualmente e preservando encontro do boss.
- [ ] **SW-081** Ajustar orçamento de ameaça e limites por tamanho/rotas da sala; recusar definitions inviáveis com diagnóstico claro.
- [ ] **SW-082** Medir 15/30/60 inimigos ativos com efeitos e on-kill; otimizar consultas, decisões escalonadas, HUD e pooling conforme os gargalos medidos.
- [ ] **SW-083** Validar vitória, morte, recompensa única, portas, extração, replay e compatibilidade das três armas/boss.
- [ ] **SW-084** Publicar tabela de parâmetros, vídeos de aceite, resultados de testes e pendências de balanceamento.

Meta provisória: 60 FPS no hardware de referência escolhido em SW-003, em build de desenvolvimento a 1080p, sem alocação gerenciada recorrente no loop estabilizado de IA. Medir p95 de frame/CPU e alocações por 60 s após aquecimento, incluindo cenário de explosões; Editor não é critério final. O limite suportado de população deve ser definido pela medição, não presumido como 60.

## 7. Segunda leva — backlog dependente do núcleo

Cada task inclui definition/prefab, feedback, testes específicos e inclusão em composição. Ordem interna pode mudar após E8; dependências técnicas devem permanecer.

| Task | Entrega | Depende de | Aceite específico |
|---|---|---|---|
| SW-090 | Leaper | E3/E4 | Ponto de queda fixado e avisado; pouso válido; interromper/morrer no salto restaura navegação. |
| SW-091 | Blink Striker | E3 | Aviso de origem/destino e atraso antes do golpe; não teleportar para parede ou sobre player. |
| SW-092 | Mine Layer | E4 | Minas armam com atraso, expiram e têm teto; proposta V1: destrutíveis, sem explosão ao despawn administrativo. |
| SW-093 | Wall Caster | E4/E6 | Parede temporária bloqueia ambos os lados e projéteis na V1; rejeita fechamento completo; NavMesh atualiza e restaura. |
| SW-094 | Repulsor | SW-042 | Onda antecipada, empurrão seguro e proteção contra cadeia de controles. |
| SW-095 | Anchor | SW-042/E5 | V1 usa slow via vínculo, sem reduzir dash; rompe por distância, morte ou quebra de postura; limpa modificador. |
| SW-096 | Haste Support | E5 | Aura respeita piso de aviso; maior buff prevalece; retirada não altera atributos permanentemente. |
| SW-097 | Reviver | E6 | Uma ressurreição por unidade elegível, vida parcial e canalização interrompível; não revive boss, suporte reviver ou invocados na V1; sem duplicar loot. |
| SW-098 | Splitter | E6 | Reserva filhos antes de finalizar morte; profundidade máxima 1 na V1; sem multiplicação recursiva. |
| SW-099 | Parasite | E5/E6 | Alvo selecionável independente; troca de hospedeiro limpa buff anterior; nunca fica invulnerável entre hospedeiros. |
| SW-100 | Absorber | E3/E5 | Acúmulo limitado e descarga antecipada; absorção não implica invulnerabilidade permanente. |
| SW-101 | Linker | E5/E6 | V1 divide dano entre até 3 aliados; metadado impede retransmissão recursiva; morte/saída de alcance desfaz vínculo. |

## 8. Ideias restantes da fonte — preservadas para depois

Não fazem parte dos 16 iniciais nem dos 12 da segunda leva. Não devem desaparecer do planejamento.

| Task | Ideia | Escopo inicial / dependência |
|---|---|---|
| SW-110 | Burst Shooter | Rajada de 3–5 tiros, carga interrompível e recuperação; E3. |
| SW-111 | Suppressor | Rajada sustentada em direção comprometida, duração/munição limitadas; E3 e orçamento de projéteis. |
| SW-112 | Flanker | Política lateral reutilizável, destinos válidos e fallback sem rodar indefinidamente; E2. |
| SW-113 | Trap Caster | Marca posição, arma e aplica root breve; E4 e proteção contra controles. |
| SW-114 | Silencer | Proposta V1: desacelerar recarga dentro da área, sem bloquear input; depende de auditoria do sistema de cooldown e E4. |
| SW-115 | Resistance Support | Redução por fonte com teto e vínculo legível; E5. |
| SW-116 | Stable | Perfil de postura resistente, não imunidade geral; E7. |
| SW-117 | Counterweight | Queda após launch gera impacto avisado; proposta: pode atingir aliados; E7 e atribuição segura de dano/on-kill. |
| SW-118 | Rage Breaker | Vulnerabilidade completa após stance break, seguida de agressividade temporária avisada; E7. |
| SW-119 | Carrier | Liberação de filhos na morte com reserva atômica; E6. |
| SW-120 | Mimic | Copiar categoria de habilidade via lista permitida, nunca executar diretamente o asset do player; auditar eventos de habilidades após E8. |

## 9. Elites e afixos

Introduzir afixos progressivamente após validar os arquétipos base. Eles não substituem tasks de comportamento. Limite proposto: um afixo por elite inicialmente; dois apenas após teste da combinação. Boss fica fora da aplicação automática.

- [ ] **SW-130** Revisar Acelerado, Resistente e Congelante existentes sob a separação de arquétipo/afixo. Respeitar piso de aviso, teto de redução e remoção por fonte.
- [ ] **SW-131** Explosivo e Flamejante: morte/exposição com aviso, limites de áreas e nenhuma explosão por limpeza de sala.
- [ ] **SW-132** Vampírico e Berserker: cura só por dano efetivo, limitada à vida máxima; aceleração por vida perdida respeita aviso mínimo.
- [ ] **SW-133** Teleportador, Multiplicador e Condutor: reutilizar destino seguro, orçamento de spawn e atribuição de dano sem recursão.
- [ ] **SW-134** Criar matriz de compatibilidade e teste de combinações. Bloquear inicialmente Reviver+Multiplicador, Spawner+Multiplicador e qualquer combinação que torne cura/proteção/controle ilimitados.

## 10. Composições e desbloqueio

Quantidades das dez composições básicas vêm da fonte. As avançadas não possuem contagens na fonte; devem receber orçamento/quantidades em task própria, sem tratá-las como configuração já pronta.

| Composição | Configuração da fonte | Disponível após |
|---|---|---|
| Pressão básica | 8 Rush, 4 Grunt, 3 Shooter | E2 |
| Proteção ranged | 4 Heavy, 4 Shooter, 2 Sniper | E3 |
| Arena fechando | 6 Rush, 2 Bomber, 2 Hazard Caster, 1 Mine Layer | SW-092 |
| Caos móvel | 4 Charger, 4 Leaper, 6 Rush, 2 Spread Shooter | SW-090 |
| Proteção de suporte | 8 Grunt, 2 Heavy, 1 Healer, 1 Shield Support | E5 |
| Fortaleza ranged | 2 Repulsor, 3 Shooter, 2 Sniper, 1 Haste Support | SW-094/096 |
| Infestação | 2 Spawner, 3 Carrier, 10 Swarm | SW-119 |
| Controle total | 2 Wall Caster, 2 Hazard Caster, 2 Hooker, 6 Grunt | SW-093 |
| High Priority | 10 Rush, 1 Healer, 1 Reviver, 1 Sniper | SW-097 |
| Juggle Playground | 12 Fragile, 3 Grunt, 2 Counterweight | SW-117 |
| Kill Box | Wall Caster, Bomber, Sniper, Heavy | SW-093 + teste de rotas |
| Conveyor | Hooker, Repulsor, Hazard Caster | SW-094 + teste de controles encadeados |
| Hive | Spawner, Parasite, Swarm, Healer | SW-099 + limites de população |
| Assault Squad | Charger, Leaper, Blink Striker, Haste Support | SW-090/091/096 |
| Bunker | Heavy, Mirror, Shield Support, Sniper, Shooter | E7 |

- [ ] **SW-140** Criar assets das dez composições completas quando seus tipos estiverem disponíveis; manter versões provisórias com nomes distintos.
- [ ] **SW-141** Definir quantidades/orçamento das cinco avançadas, validar rotas e limites de ameaça e registrar versões aprovadas por playtest.

## 11. Validação e definição de pronto

Uma task de gameplay só está concluída quando implementada, configurada em asset/prefab, testada e observada em Play Mode. Testes de geometria sozinhos não comprovam qualidade do golpe.

**EditMode:** escolha por arquétipo/distância, validação de definitions, regras de stacking, distribuição de vagas, budgets, geometria, identidades de vida e migração idempotente.

**PlayMode:** trajetória/colisão, momento de dano, invulnerabilidade de esquiva conforme sistema vigente, linha de visão, stagger/stance break, pausa, morte/disable, pooling, remoção de fonte, geração dinâmica, recompensa única e replay.

**Playtest:** testar com manopla/lança/arco, perto de paredes/portas e em multidão; observar se o jogador reconhece fonte, pode reagir e muda sua prioridade. Capturar vídeos de golpes e registrar problemas de leitura antes de aumentar quantidades.

**Regressão:** manter testes atuais de ataques, boss, clique próximo ao player e progressão. Toda entrega deve declarar testes executados, falhas preexistentes e validações não realizadas. Checar console, referências Unity e diff. Não alterar o visual global para resolver feedback de um inimigo.

## 12. Decisões para revisar durante os marcos

As propostas desta spec permitem começar sem bloquear em perguntas de detalhe. Rever no marco indicado:

| Momento | Decisão a revisar |
|---|---|
| E0 | Hardware alvo, teto de população e orçamento de frame. |
| E2 | Densidade melee, vagas simultâneas, distância e persistência de recuo ranged. |
| E4 | Piso de telegraph, duração de controle e janela de proteção entre controles. |
| E5 | Força de cura/escudo e regra de sobreposição de auras. |
| E6 | On-kill/loot de invocados, limites de geração e desempenho sob explosões. |
| E7/E8 | Aprovação dos 16 tipos, ordem de introdução no FirstSector e combinações frustrantes. |

Próximo trabalho recomendado: executar **E0**, depois **E1–E2**, e revisar a composição Pressão básica jogando antes de iniciar E3.
