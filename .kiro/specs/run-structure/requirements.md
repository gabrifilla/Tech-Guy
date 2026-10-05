# Requirements Document

## Introduction

Esta especificação inicia a **Fase 4 — Run Structure** do Tech-Guy, conforme `Docs/Tech-Guy_Gameplay_Rework.md`, especialmente §§24–27, 42–43 e 47. O objetivo é transformar progressão entre encontros em decisões: escolher a recompensa perseguida, considerar HP restante, assumir risco de Elite e alcançar uma Mutation antes do Boss. O critério principal é que **duas runs consecutivas com a mesma arma produzam decisões diferentes**.

**Status: requirements adequados para elaboração do design conforme o parecer completo de 04/10/2026, `Tech-Guy_Parecer_Requirements_Fase4_Completo.md`; design revisado e plano incremental elaborados em `design.md` e `tasks.md`; implementação e validação técnica das tasks 1–14 concluídas em 04/10/2026, com evidências em `validation.md`; aceitação humana da task 15 e encerramento da task 16 pendentes.** O primeiro parecer avaliou um trecho até R4.5; a continuação R4.6–R10 já existia no projeto e foi conferida/complementada. O parecer completo não identifica ajustes obrigatórios antes do design; sua aprovação é documental, sem comprovar implementação, runtime ou balanceamento. Fases 2 e 3 aceitas expressamente pelo usuário; encerramento da Fase 3 em `../gauntlet-2-rework/validation.md`. Valores de cura, dificuldade e recompensas serão definidos no design e avaliados por playtest, não presumidos aprovados pelo roadmap.

### Escopo e dependências

- Entregar um setor de vertical slice centrado na Manopla: grafo manual, mapa de rota, categorias de recompensa, HP persistente, Repair, Elite, Mutation e Boss/extração.
- Usar §§42–43 como referência de tamanho: 6–8 encontros de combate no percurso completo, incluindo Elite quando escolhido e Boss; visitas sem combate não contam como encontros. Disponibilizar pelo menos uma bifurcação real, uma oportunidade de Repair, uma opção de Elite e uma aquisição garantida de Mutation no percurso até o Boss.
- Reutilizar combate/input/postura e conteúdo da Fase 3, `RunBoons`, apresentação de recompensa, portas, navegação, inimigos e fluxo Nexus/extração existentes. Preservar arsenal e configuração de arma entre runs.
- A Fase 5 cria/revisa o Protocol System. Nesta fase, a categoria Protocol pode selecionar conteúdo universal **já existente**, mediante catálogo explícito; não implementar novos Protocols ou reclassificar silenciosamente todo o catálogo.
- Excluir nova geração procedural, novos setores, meta progressão, Event rooms, expansão de inimigos/armas e reworks de Arco/Lança. Não remover o gerador procedural já entregue nem reorganizar assets não relacionados.
- Mana, cooldowns, Asura, buffs e aquisição mantêm seus contratos atuais salvo adaptações de transição documentadas no design. HP persistente não significa impedir curas explícitas de efeitos existentes.

### Fundação observada — inspeção estática

| Sistema | Reuso e conflito a resolver no design |
| --- | --- |
| `Stages/ProgressionDirector.cs` | Atualmente gera/materializa grafo, controla encontros, recompensa pendente, fragmentos de acesso e extração. A rota manual exige seleção explícita de modo/fonte de rota, sem dois diretores ativos. |
| `Stages/Model/RoomGraph.cs`, `RoomType.cs` | Modelo existente de salas/conexões; separar função do encontro e categoria de recompensa evita presumir que cada sala de recompensa exige outro combate. Preservar valores serializados existentes ao adaptar. |
| `Core/ProceduralStageEnvironment.cs`, `Core/EncounterGates.cs` | Geometria/navegação e portas existentes; mapa de decisão deve corresponder aos destinos realmente alcançáveis. |
| `Items/RewardTrophy.cs`, `Core/RunBoons.cs` | Troféu abre oferta genérica por sala; adaptar contexto/categoria e conclusão única. Manopla já possui Techniques e Mutations exclusivas por slot. |
| `Characters/Player/PlayerActor.cs`, `Core/RunBoons.cs` | Recursos atuais; Iron Heart aumenta vida máxima e cura completamente de forma explícita. Não confundir esse efeito com cura automática de transição. |
| Specs `procedural-stage-room-generation` / `playable-procedural-run` | Fluxo anterior procedural/fragmentos não representa automaticamente o novo contrato manual. Registrar adaptação local e conservar o modo legado quando aplicável. |

Os caminhos são relativos a `Assets/_Project/Scripts`. Essa leitura não comprova estado serializado, compilação ou runtime. `Docs/RunLoop.md` contém descrições históricas conflitantes sobre layout e recompensas; o design deverá reconciliar somente o fluxo afetado com código e inspeção Unity.

## Glossary

- **Run**: sessão iniciada no Nexus e encerrada por morte ou extração após o Boss; inventário temporário pertence à sessão.
- **Rota_Manual**: grafo de destinos e conexões autorados; não depende de gerar uma topologia procedural nova.
- **Nó**: destino identificável da rota, com função, encontro opcional, categoria de recompensa e conexões.
- **Bifurcação_Real**: escolha entre destinos diferentes cujo risco, benefício ou acesso muda o percurso, tornando uma alternativa indisponível ou impondo custo de oportunidade equivalente definido no design. Coletar todos os ramos indistintamente ou usar duas entradas para o mesmo resultado obrigatório não a satisfaz.
- **Categoria_de_Recompensa**: intenção visível antes da entrada, distinta de raridade, rank do inimigo e família da arma.
- **Technique / Mutation**: especialização ou transformação de arma da Fase 3; Mutation tem aquisição única por slot e incompatibilidades explícitas.
- **Protocol**: categoria de conteúdo universal existente nesta fase, sem antecipar os novos sistemas da Fase 5.
- **Repair**: recuperação explícita e limitada, disponível por oportunidade identificada, sem combate obrigatório para utilizar.
- **Elite**: encontro opcional de maior risco com vantagem de recompensa previamente declarada; não redefine resistência/rank da Fase 2.
- **HP_Persistente**: dano e cura acompanham o jogador entre nós; transição normal não restaura HP automaticamente.
- **Conclusão_de_Recompensa**: aquisição/uso/alternativa explicitamente resolvida uma única vez, após a qual a progressão pode continuar.
- **Identidade_de_Sessão**: identidade nova a cada run, usada para distinguir callbacks e estado temporário de sessões encerradas.
- **Garantia_de_Mutation**: pelo menos uma aquisição elegível de Mutation naquela run antes de entrar no Boss; oferta não aceita ou alternativa genérica não satisfaz essa garantia.

## Requirements

### Requirement 1: Rota manual e escolhas alcançáveis

**User Story:** Como jogador, quero escolher meu percurso, para perseguir benefícios diferentes com a mesma arma.

#### Acceptance Criteria

1. THE Sistema_de_Run SHALL executar uma rota manual com início, destinos identificáveis, conexões válidas e Boss alcançável.
2. THE Rota_Manual SHALL oferecer pelo menos uma Bifurcação_Real antes do Boss, com diferenças de categoria de recompensa ou risco visíveis antes da confirmação.
3. WHEN um destino é confirmado, THE Sistema_de_Run SHALL avançar apenas por conexão disponível, registrar a escolha e impedir entrada simultânea em outro encontro.
4. THE Rota_Manual SHALL permitir completar cada percurso válido com 6–8 encontros de combate, conforme definição do escopo, sem depender de visitas repetidas para cumprir esse tamanho.
5. THE Sistema_de_Run SHALL distinguir destinos disponíveis, escolhidos, concluídos e bloqueados; retornar a um nó concluído SHALL NOT reiniciar encontro ou conceder sua recompensa novamente.
6. IF a autoria contém IDs duplicados, referências ausentes, conexões inválidas ou percurso sem saída, THEN THE Sistema_de_Run SHALL diagnosticar e impedir iniciar a rota inválida sem alterar o estado de uma run vigente.
7. WHEN o jogador confirma um ramo de Bifurcação_Real, THE Sistema_de_Run SHALL aplicar a exclusão da alternativa ou o custo de oportunidade autorado e anunciado; portas, destinos e mapa SHALL refletir a mesma decisão.

### Requirement 2: Mapa e informação prévia

**User Story:** Como jogador, quero conhecer o objetivo de cada caminho antes de entrar, para decidir com intenção.

#### Acceptance Criteria

1. THE Mapa_de_Rota SHALL apresentar conexões, posição/progresso atual, destinos disponíveis, função, categoria de recompensa e indicação de Elite/Boss/Repair.
2. WHEN o jogador consulta um destino disponível, THE Mapa_de_Rota SHALL mostrar o benefício perseguido e os bloqueios aplicáveis, sem prometer uma categoria diferente da recompensa efetiva.
3. WHEN a confirmação não é permitida por combate, seleção de recompensa, pausa, morte ou transição vigente, THE Sistema_de_Run SHALL rejeitá-la sem consumir recursos ou trocar destino.
4. THE UI_da_Run SHALL permitir entender e confirmar a escolha pelos controles existentes, sem deixar input da UI iniciar ataques, skills ou movimento involuntários.
5. THE Mapa_de_Rota SHALL corresponder à progressão real; apresentação cosmética SHALL NOT destravar portas, resolver recompensas ou concluir encontros.

### Requirement 3: Categorias de recompensa e aquisição

**User Story:** Como jogador, quero receber o tipo de melhoria que escolhi perseguir, para construir um estilo deliberadamente.

#### Acceptance Criteria

1. THE Sistema_de_Recompensa SHALL separar função do nó de categoria de recompensa: Combat/Elite/Boss descrevem encontro ou destino; Technique/Protocol/Mutation descrevem conteúdo perseguido; Repair descreve visita de recuperação. THE Sistema_de_Run SHALL NOT exigir um combate adicional somente porque uma recompensa recebeu uma categoria.
2. WHEN uma recompensa Technique é resolvida na Manopla, THE Sistema_de_Recompensa SHALL ofertar apenas Techniques elegíveis da Fase 3, respeitando ranks máximos e inventário vigente.
3. WHEN uma recompensa Mutation é resolvida, THE Sistema_de_Recompensa SHALL usar Mutations elegíveis da arma e preservar exclusividade por slot, aliases e incompatibilidades existentes.
4. WHEN uma recompensa Protocol é resolvida, THE Sistema_de_Recompensa SHALL usar um subconjunto explicitamente definido do conteúdo universal existente; SHALL NOT implementar os Protocols novos da Fase 5 como dependência.
5. THE Sistema_de_Recompensa SHALL identificar categoria, efeito, rank e incompatibilidades antes da aquisição e não repetir opções equivalentes na mesma oferta.
6. IF uma categoria não possui opções elegíveis, THEN THE Sistema_de_Recompensa SHALL apresentar uma alternativa definida no design, identificada como alternativa, sem inventar rank extra, apagar aquisições ou bloquear permanentemente a rota.
7. WHEN a seleção é aceita, THE Sistema_de_Recompensa SHALL aplicar a aquisição uma única vez, preservar assets fonte e concluir a pendência; callbacks repetidos ou reentrantes SHALL NOT duplicar recompensa ou progressão.

### Requirement 4: HP persistente e consequências

**User Story:** Como jogador, quero que o dano sofrido tenha consequência na próxima escolha, para considerar recuperação e risco.

#### Acceptance Criteria

1. WHEN o jogador conclui combate, recebe recompensa ou entra em outro nó normal, THE Sistema_de_Run SHALL preservar HP atual, sem cura completa ou percentual automática por essa transição.
2. WHEN uma cura explícita de Repair, boon ou efeito existente é aplicada, THE Sistema_de_Run SHALL identificar sua origem e limitar HP ao máximo vigente, sem confundi-la com cura de transição.
3. WHEN a vida máxima muda por aquisição, THE Sistema_de_Run SHALL preservar o contrato documentado do efeito e não criar cura adicional não prevista.
4. THE Sistema_de_Run SHALL impedir duplicação de cura por reentrada, callback repetido, fechamento/reabertura de UI ou repetição de transição.
5. THE HUD SHALL apresentar HP atual/máximo consistente antes e depois de cada transição e recuperação.
6. THE Fluxo_de_Run SHALL manter mana, cooldowns e Asura conforme seus contratos definidos; adaptações necessárias de transição SHALL ser explicitadas no design antes da implementação.
7. WHEN uma mudança de sala reconstitui o personagem ou seus stats, THE Sistema_de_Run SHALL resolver a vida máxima final antes de restaurar o HP atual válido, sem clamp temporário que perca HP, simular nova aquisição ou repetir cura de aquisição como Iron Heart.

### Requirement 5: Repair como decisão limitada

**User Story:** Como jogador, quero escolher recuperação em troca de outra oportunidade, para administrar o risco do restante da run.

#### Acceptance Criteria

1. THE Rota_Manual SHALL oferecer pelo menos uma oportunidade alcançável de Repair antes do Boss e permitir reconhecer quando uma alternativa de rota a substitui.
2. THE Repair SHALL informar recursos restaurados, quantidade/regra, limite de uso e eventual custo antes da confirmação; valores SHALL ser autorados e definidos no design, sem exigir uma nova moeda.
3. WHEN o jogador usa Repair, THE Sistema_de_Run SHALL aplicar recuperação uma única vez por oportunidade e marcar seu consumo.
4. THE Repair SHALL NOT permitir cura ilimitada por revisita ou exigir combate na própria oportunidade de recuperação.
5. IF HP está completo ou a recuperação não pode produzir benefício, THEN THE Repair SHALL comunicar isso e oferecer saída sem softlock; eventual consumo/custo SHALL seguir regra explícita apresentada antes da confirmação.
6. THE Repair SHALL NOT reinicializar o inventário, trocar a arma ou repor Asura implicitamente.
7. IF uma tentativa de Repair é rejeitada, THEN THE Sistema_de_Run SHALL preservar recursos, custo e disponibilidade da oportunidade, sem cura, cobrança ou conclusão.
8. THE Repair SHALL permitir dispensar a recuperação e continuar por uma saída válida; efeito da dispensa sobre o uso restante SHALL ser definido no design e apresentado antes da confirmação.

### Requirement 6: Elite e risco compensado

**User Story:** Como jogador, quero poder aceitar um desafio maior por um benefício reconhecível, para tomar uma decisão de risco.

#### Acceptance Criteria

1. THE Rota_Manual SHALL oferecer pelo menos uma opção alcançável de Elite antes do Boss, com caminho alternativo que permita concluir a run sem escolhê-la.
2. THE UI_de_Rota SHALL identificar o maior risco e a vantagem da recompensa de Elite antes da entrada; o design SHALL definir uma vantagem verificável em relação ao encontro normal de referência.
3. THE Encontro_Elite SHALL reutilizar inimigos e regras de postura/resistência existentes, com dificuldade/composição autoradas, sem alterar silenciosamente os contratos da Fase 2.
4. WHEN o Elite é derrotado, THE Sistema_de_Run SHALL conceder apenas a recompensa anunciada e liberar a progressão após sua conclusão.
5. IF o jogador morre no Elite, THEN THE Sistema_de_Run SHALL encerrar a run pelo fluxo de morte, sem conceder a recompensa pendente.
6. IF as opções elegíveis da recompensa de Elite se esgotam, THEN THE Sistema_de_Recompensa SHALL aplicar uma alternativa explicitamente definida que conserve a vantagem anunciada e verificável; nome/cor de sala SHALL NOT ser sua única vantagem.
7. THE Encontro_Elite SHALL preservar abertura e retomada da Fase 2, sem controle paralelo; callbacks repetidos de conclusão/recompensa SHALL NOT conceder novamente sua vantagem.

### Requirement 7: Mutation garantida e power spike

**User Story:** Como jogador, quero alcançar uma transformação importante antes do Boss, para experimentar a evolução do meu build.

#### Acceptance Criteria

1. THE Rota_Manual SHALL garantir pelo menos uma aquisição elegível de Mutation naquela run antes da entrada no Boss em todo percurso válido de conclusão, inclusive os que evitam Elite, sem depender exclusivamente de roll de raridade.
2. WHEN a garantia ainda não foi satisfeita e existem Mutations elegíveis, THE Recompensa_Garantida SHALL permitir adquirir pelo menos uma delas pelo fluxo de seleção existente, respeitando família, exclusividade por slot e incompatibilidades da Fase 3. Oferta não aceita SHALL NOT ser contabilizada como aquisição.
3. WHEN a Mutation é adquirida, THE HUD_e_Recompensa SHALL comunicar a ação transformada e preservar custos, snapshots e lifecycle da Fase 3.
4. IF as Mutations elegíveis se esgotaram porque o jogador já as adquiriu naquela run, THEN THE Sistema_de_Run SHALL registrar a garantia satisfeita pela aquisição anterior e aplicar a alternativa definida para a oportunidade excedente, sem contar essa alternativa como nova Mutation.
5. THE Rota_Manual SHALL incluir combate utilizável após a oportunidade de Mutation, permitindo observar o comportamento transformado antes ou durante o Boss.
6. IF nenhuma Mutation foi adquirida e não há opção elegível por configuração/família/incompatibilidade, THEN THE Sistema_de_Run SHALL diagnosticar a impossibilidade de cumprir a garantia; alternativa genérica de R3.6 SHALL NOT satisfazê-la nem autorizar entrada no Boss. THE Autoria SHALL rejeitar configurações que inevitavelmente produzam esse estado e o design SHALL definir recuperação sem apagar aquisições ou prender o jogador em uma UI.

### Requirement 8: Boss, conclusão e replay

**User Story:** Como jogador, quero concluir o percurso e começar outra run limpa, para comparar decisões usando a mesma arma.

#### Acceptance Criteria

1. THE Rota_Manual SHALL possuir um Boss final e informar sua condição de acesso; essa condição SHALL ser satisfazível em todo percurso válido, incluindo o que evita Elite.
2. IF fragmentos de acesso legados forem reutilizados, THEN THE Sistema_de_Run SHALL adaptar sua obtenção ao novo percurso sem exigir nós indisponíveis ou recompensas duplicadas; a decisão SHALL ser registrada no design.
3. WHEN o Boss é derrotado validamente e sua recompensa final é resolvida, quando prevista, THE Sistema_de_Run SHALL disponibilizar extração pelo fluxo existente, sem carregar automaticamente outra cena antes de o jogador completar a extração.
4. WHEN ocorre morte ou extração, THE Sistema_de_Run SHALL concluir a sessão uma única vez, remover inventário/efeitos temporários, filas, handlers, portas e pendências pertencentes à run.
5. WHEN uma nova run começa, THE Sistema_de_Run SHALL reinicializar rota, HP/recursos iniciais conforme contrato, recompensas e oportunidades consumidas, preservando arma do arsenal e preferências persistentes.
6. THE Sistema_de_Run SHALL impedir callbacks de uma sessão anterior de afetar a seguinte.
7. THE Fluxo_Final SHALL definir no design o desempate de morte do jogador e vitória sobre o Boss na mesma resolução, sem produzir simultaneamente dois desfechos ou conceder recompensas incompatíveis com o resultado aceito.
8. THE Sistema_de_Run SHALL entregar recompensa final, concluir extração e retornar ao Nexus no máximo uma vez por sessão; callbacks repetidos SHALL NOT criar novo portal, recompensa ou retorno.
9. THE Sistema_de_Run SHALL manter portas, mapa e input coerentes com combate, recompensa final e transição, impedindo acesso precoce ao Boss/extração; exigências de fragmentos legados SHALL NOT tornar Elite obrigatório ou invalidar um percurso autorizado.

### Requirement 9: Autoria, integração e preservação

**User Story:** Como desenvolvedor, quero configurar uma rota pequena usando a infraestrutura existente, para validar o loop sem ampliar prematuramente o projeto.

#### Acceptance Criteria

1. THE Autoria_de_Rota SHALL utilizar dados serializáveis no Inspector para nós, conexões, categorias, encontros e recuperação, com persistência verificada após salvar/recarregar.
2. THE Autoria_de_Rota SHALL validar referências, IDs, enumerações e limites numéricos brutos, incluindo NaN/infinito/negativos relevantes, antes de normalizar ou iniciar gameplay.
3. THE Integração SHALL usar uma única autoridade de progressão e permitir distinguir explicitamente o fluxo manual do procedural existente; SHALL NOT executar os dois simultaneamente nem remover o gerador fora deste escopo.
4. THE Implementação SHALL preservar GUIDs, assets/Resources e contratos serializados legados, com migração explícita se necessária e referências verificadas via Unity MCP.
5. THE Fluxo_da_Run SHALL preservar controle, input buffer, pausa/hit-stop, HUD e combate das fases anteriores, removendo somente locks/efeitos pertencentes à transição em seu cleanup.
6. THE Design SHALL registrar os contratos deliberadamente substituídos das specs de run anteriores e os comportamentos mantidos, sem tratar documentação histórica como evidência runtime.
7. WHEN uma nova run começa, THE Sistema_de_Run SHALL criar uma Identidade_de_Sessão nova, mantendo estado de rota, nós concluídos, Repair consumido, aquisições e pendências exclusivos dessa sessão; arsenal permanente SHALL NOT ser confundido com Techniques/Mutations temporárias.
8. WHEN um callback de encontro, recompensa, cura ou transição pertence a uma sessão encerrada, THE Sistema_de_Run SHALL rejeitar sua contribuição mesmo após iniciar outra run ou reutilizar componentes.

### Requirement 10: Validação técnica e aceitação de gameplay

**User Story:** Como equipe, quero comprovar a progressão e avaliar decisões em duas runs, para encerrar a fase com evidências separadas.

#### Acceptance Criteria

1. THE Validação_Técnica SHALL cobrir todos os percursos autorizados, contagem de 6–8 combates, Boss alcançável, Elite opcional, Repair e aquisição garantida de Mutation, além de disponibilidade, categoria/elegibilidade, recompensa única, persistência de HP, morte/extração/replay e callbacks obsoletos.
2. THE Validação_Unity SHALL confirmar projeto/Editor correto e usar obrigatoriamente Unity MCP nas validações necessárias que ele suporta: importação/compilação/Console, autoria/referências/save-reload, testes relevantes e runtime pertinente. THE Validação SHALL preservar a cena do usuário, sair de PlayMode e registrar limitações/checks pendentes sem limpar Console para ocultar problemas.
3. THE Testes SHALL verificar fronteiras como HP cheio/baixo, catálogos esgotados, cancelamento de UI, callbacks repetidos, pausa/hit-stop e morte durante recompensa/transição, sem enfraquecer testes corretos de contratos mantidos.
4. THE Validação SHALL executar somente regressões pertinentes às alterações e dependências afetadas, registrando falhas, skips, retries e verificações pendentes sem exigir a suíte completa a cada tarefa.
5. THE Aceitação_Humana SHALL avaliar duas runs consecutivas com a mesma Manopla e registrar escolhas de rota/recompensa, HP nas bifurcações, uso ou recusa de Repair/Elite, Mutation adquirida e desfecho.
6. THE Aceitação_Humana SHALL verificar se as decisões diferem por motivos compreensíveis de risco/build/HP; diferença de seed, layout cosmético ou opções aleatórias sem impacto SHALL NOT satisfazer isoladamente o objetivo da fase.
7. THE Encerramento SHALL separar resultados técnicos de aceitação humana e registrar decisões, adaptações, evidências e próximas ações em handoff; SHALL NOT declarar a fase concluída com checks Unity necessários ou aceitação pendentes.
8. THE Validação_Runtime SHALL exercitar Nexus → rota/escolhas/encontros/recompensas → Boss → extração/Nexus, além de morte e nova run, incluindo reconstrução de HP sem cura repetida e isolamento da sessão anterior.

## Cobertura do parecer de requirements — 04/10/2026

| Ponto do parecer | Contrato conferido/complementado |
| --- | --- |
| Anexo parcial até R4.5 | Continuação R4.6–R10 já presente no projeto; preservada e revisada, sem inferir aprovação integral |
| Bifurcação e função versus recompensa | Glossário, R1.7 e R3.1; exclusão/custo de oportunidade e estados físicos/UI coerentes |
| Repair rejeitado/dispensa | R5.2/R5.7–R5.8; custo, uso e recuperação preservados em rejeição |
| Vantagem de Elite esgotado | R6.6–R6.7; vantagem real/fallback e contratos de abertura/retomada |
| Primeira Mutation efetivamente adquirida | R7.1–R7.6; fallback não substitui garantia, aquisição anterior pode satisfazê-la |
| HP reconstruído sem nova aquisição | R4.7; máximo final antes de HP atual e nenhuma repetição de cura |
| Boss, recompensa final e morte/vitória | R8.3/R8.7–R8.9; desempate definido no design e conclusão única |
| Identidade/reset e modo manual/legado | R8.4–R8.6/R9.3–R9.8; temporários isolados e callbacks antigos rejeitados |
| Validação e duas runs humanas | R10.1–R10.8; MCP obrigatório conforme suporte, percurso real e aceitação separada |

Esta revisão foi documental. Não houve alteração de código/assets, compilação, testes ou nova inspeção Unity. Taxas/preços, catálogos/fallbacks, topologia e desempates serão propostas explícitas no design; fases anteriores aceitas e escopo futuro permanecem preservados.

## Próxima etapa

**Parecer completo — 04/10/2026:** a revisão integral confirma cobertura R1–R10 e adequação para seguir ao design, sem outro ciclo obrigatório de requirements. As observações anteriores de seções ausentes referiam-se apenas ao envio parcial. Permanecem decisões do design: topologia/contagem por percurso, compromisso/reconexão dos ramos, cancelamento/recusa de Mutation sem satisfazer a garantia, prevenção de catálogo incompatível, HP/Repair, vantagem/fallback de Elite, acesso ao Boss, concorrência morte/vitória e seleção manual/legado. Os critérios de aquisição efetiva em R7.1–R7.6 permanecem, inclusive a distinção entre aquisição anterior e alternativa genérica. Nenhum check Unity ou playtest foi realizado nesta atualização documental.

O design inicial em `design.md` apresenta decisões D1–D8 para os pontos acima, quatro percursos de 6/7/7/8 combates e cobertura R1–R10. O plano incremental foi elaborado em `tasks.md`, no padrão das fases anteriores; próxima etapa de execução: auditoria e baseline da task 1. Este documento não implementa esses comportamentos nem altera cenas/assets/código.
