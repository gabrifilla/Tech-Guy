# Requirements Document

## Introduction

Esta especificação inicia a **Fase 5 — Sistema de Protocolos** do Tech-Guy, conforme `Docs/Tech-Guy_Gameplay_Rework.md`, especialmente §§18, 38, 42–43 e 48. Protocolos conectam sistemas da build: aplicação de elementos, impactos, mortes e efeitos secundários devem produzir combinações emergentes, mantendo clareza visual e custo de execução limitado.

**Status: requirements aprovados pelo parecer `Tech-Guy_Parecer_Requirements_Fase5.md`, sem ajustes obrigatórios; design aprovado pelo usuário e execução iniciada, com tasks 1–12 concluídas; 13 em implementação/validação, 14–16 pendentes em `tasks.md`.** O usuário autorizou o início da Fase 5 e posteriormente sua implementação. A aprovação documental não comprova aceitação de gameplay dos Protocolos; evidências incrementais em `validation.md`. O registro de duas runs humanas da Fase 4 continua separado em `../run-structure/validation.md`.

### Escopo e dependências

- Entregar/revisar os sete Protocolos de §48: **Ignite, Frost, Conductor, Detonation, Reactor, Resonance e Overflow**. A estimativa de 5–6 Protocolos do vertical slice não remove itens dessa lista explícita.
- Reutilizar efeitos existentes, aquisição por `RunBoons`, apresentação de recompensas e contratos de combate das Fases 2–3. Integrar o catálogo da rota manual da Fase 4.
- Manter Protocolos universais e distinguir sua função das Techniques e Mutations. Usar a Manopla como cenário principal de aceitação; preservar compatibilidade das armas existentes sem antecipar Arco 2.0 ou Lança 2.0.
- Revisar recursão, orçamento das cascatas, classificação dos impactos, aplicação de elementos, legibilidade e performance.
- Não incluir novos setores, geração procedural, meta progressão, novos arquétipos, Utility rewards ou todos os gatilhos exemplificados em §38. Expansão do número de salas ou de aquisições exige decisão explícita no design.

### Fundação observada — inspeção estática

| Sistema existente | Contrato ou decisão a resolver |
| --- | --- |
| `Characters/Combat/PlayerOnHitEffects.cs` | Aplica elementos, choque térmico e Overflow. Overflow atualmente causa dano adicional condicionado a elementos sem consumi-los; apresentação que promete consumo precisa ser reconciliada. |
| `Characters/Combat/RunSynergyEffects.cs` | Implementa Conductor, Detonation, Reactor e Resonance; possui limites de 32 impactos secundários e profundidade 4. O orçamento é reiniciado por resolução, o que não demonstra um limite compartilhado por ataque com vários alvos. |
| `Core/RunBoons.cs` | Já aplica os sete IDs; elegibilidade e pré-requisitos precisam corresponder ao catálogo e à progressão publicados. |
| `Core/Run/ManualRewardCatalog.cs` | A Fase 4 oferece Ignite, Frost, Conductor e Detonation. A rota normal tem uma aquisição de Protocol; o caminho Elite tem duas. Dependências de múltiplos Protocolos podem tornar combinações inalcançáveis nessa run. |
| `UI/RunModifierPresentation.cs` | Reutilizar títulos, categorias e detalhes; corrigir divergências entre descrição e efeito real. |
| Specs `gauntlet-2-rework`, `run-structure` e `modifier-synergies-theme17` | Preservar recursos/impactos da Manopla, tickets e conclusão de encontros. A spec histórica de sinergias é referência de compatibilidade, não substitui o escopo desta fase. |

Os caminhos são relativos a `Assets/_Project/Scripts`. Esta inspeção identifica decisões de contrato; não substitui compilação, testes ou observação em Unity.

## Glossary

- **Protocol**: modificador universal que conecta efeitos ou eventos de combate, adquirido para a sessão atual.
- **Rank**: nível de um Protocol, com regra explícita para aquisição repetida e limite.
- **Impacto_Direto**: impacto originado pela ação de arma; não inclui dano criado por Protocolos.
- **Impacto_Secundário**: dano produzido por cadeia, explosão, choque ou outro efeito derivado.
- **DoT**: dano periódico de status, distinto de impacto direto e secundário instantâneo.
- **Raiz_de_Cascata**: origem identificável de uma resolução, incluindo os alvos e efeitos derivados abrangidos pelo mesmo orçamento.
- **Orçamento_de_Cascata**: limites compartilhados de impactos, profundidade e demais recursos definidos no design.
- **Status_Elemental**: Burn ou Chill com duração, intensidade e regras de renovação explícitas.
- **Ticket_de_Recompensa**: oferta congelada e identificada por sessão, nó e ordinal, concluída uma única vez.
- **Combinação_Alcançável**: composição adquirível e funcional em um percurso publicado, sem depender de recompensas inexistentes ou de comandos de depuração.
- **Legibilidade**: capacidade de distinguir causa, alvos e consequências relevantes dos efeitos sem perder sinais de perigo do combate.

## Requirements

### Requirement 1: Catálogo e identidade dos Protocolos

**User Story:** Como jogador, quero Protocolos com funções reconhecíveis, para construir combinações e entender minhas aquisições.

#### Acceptance Criteria

1. THE Sistema_de_Protocolos SHALL disponibilizar Ignite, Frost, Conductor, Detonation, Reactor, Resonance e Overflow com identidade e descrição próprias.
2. THE Sistema_de_Protocolos SHALL preservar IDs existentes e referências serializadas, ou fornecer migração explícita quando uma alteração for necessária.
3. THE Sistema_de_Protocolos SHALL definir para cada Protocol seu gatilho, alvos válidos, efeito, pré-requisitos, regra de repetição e limite de rank.
4. WHEN uma aquisição repetida for permitida, THE Sistema_de_Protocolos SHALL aplicar a progressão de rank declarada sem duplicar inscrições no mesmo evento.
5. WHEN dados de autoria contiverem valores inválidos, NaN ou infinito, THE Sistema_de_Protocolos SHALL rejeitar os dados com diagnóstico antes de executar efeitos de combate.
6. THE Sistema_de_Protocolos SHALL manter separadas a validação dos dados brutos e a normalização dos valores utilizados em runtime.

### Requirement 2: Aquisição e combinações alcançáveis

**User Story:** Como jogador, quero que os Protocolos oferecidos funcionem na minha run, para tomar decisões úteis sobre a build.

#### Acceptance Criteria

1. THE Sistema_de_Recompensas SHALL integrar os sete Protocolos ao catálogo elegível, respeitando família, rank e pré-requisitos declarados.
2. WHEN uma oferta depender de outra aquisição, THE Sistema_de_Recompensas SHALL explicar a dependência e impedir uma escolha apresentada como funcional quando ela não puder funcionar na run atual.
3. THE Design_de_Protocolos SHALL demonstrar como cada Protocol e as combinações usadas na aceitação podem ser adquiridos em percursos publicados, considerando uma ou duas aquisições de Protocol na rota atual.
4. IF a solução exigir mudar oportunidades, pacotes de aquisição ou pré-requisitos, THEN THE Design_de_Protocolos SHALL registrar a alteração, sua justificativa e seus efeitos sobre os contratos da Fase 4 antes da implementação.
5. WHEN um ticket for fechado e reaberto, THE Sistema_de_Recompensas SHALL conservar os cartões congelados sem reroll ou aplicação antecipada.
6. WHEN uma escolha for confirmada, THE Sistema_de_Recompensas SHALL aplicar uma única aquisição e concluir somente o ticket da sessão correspondente.
7. THE Sistema_de_Recompensas SHALL preservar a aquisição obrigatória de Mutation antes do Boss e não substituí-la por um Protocol.

### Requirement 3: Ignite e Frost

**User Story:** Como jogador, quero aplicar elementos de forma previsível, para criar oportunidades de interação entre Protocolos.

#### Acceptance Criteria

1. WHEN um impacto elegível ativar Ignite, THE Sistema_de_Protocolos SHALL aplicar Burn com dano periódico, duração e regras de renovação declarados.
2. WHEN um impacto elegível ativar Frost, THE Sistema_de_Protocolos SHALL aplicar Chill com intensidade, duração e chance de aplicação declaradas.
3. WHEN um status for reaplicado, THE Sistema_de_Protocolos SHALL seguir a regra documentada de acúmulo ou renovação sem criar ticks duplicados involuntários.
4. WHEN Burn e Chill interagirem, THE Sistema_de_Protocolos SHALL resolver choque térmico segundo uma ordem explícita de leitura/aplicação de status e incluir seus efeitos no orçamento da raiz.
5. WHILE o jogo estiver pausado ou sob hitstop, THE Sistema_de_Protocolos SHALL usar os relógios definidos para ticks, duração e renovação, sem compensação que multiplique dano indevidamente ao retomar.
6. THE Sistema_de_Protocolos SHALL preservar resistências, imunidades e contratos de controle dos ranks inimigos, sem transformar Chill em quebra de postura ou interrupção não autorizada.
7. WHEN o alvo morrer ou o status expirar, THE Sistema_de_Protocolos SHALL encerrar os efeitos e inscrições pertencentes àquela aplicação.

### Requirement 4: Conductor e Detonation

**User Story:** Como jogador, quero propagar efeitos entre adversários, para transformar posicionamento e eliminações em vantagens de combate.

#### Acceptance Criteria

1. WHEN Conductor receber um gatilho elegível, THE Sistema_de_Protocolos SHALL selecionar alvos válidos dentro dos limites de quantidade, alcance e dano declarados.
2. THE Sistema_de_Protocolos SHALL definir uma regra verificável de seleção e revisita de alvos dentro da mesma raiz, impedindo ciclos ilimitados entre adversários.
3. WHEN uma morte elegível e confirmada ativar Detonation, THE Sistema_de_Protocolos SHALL gerar a explosão prevista uma única vez para aquela morte.
4. THE Sistema_de_Protocolos SHALL declarar quais origens de dano, incluindo DoT e impactos secundários, podem ativar cada efeito.
5. WHEN Detonation interagir com Burn, Chill ou ambos, THE Sistema_de_Protocolos SHALL aplicar variantes e precedência documentadas sem contar fragmentos visuais como impactos adicionais.
6. THE Sistema_de_Protocolos SHALL excluir jogador, aliados, objetos inválidos e alvos já mortos quando não forem destinatários autorizados do efeito.
7. WHEN uma propagação atingir seu orçamento, THE Sistema_de_Protocolos SHALL interromper novos efeitos derivados sem desfazer dano já confirmado.

### Requirement 5: Reactor, Resonance e Overflow

**User Story:** Como jogador, quero ampliar as interações da minha build, para escolher Protocolos por sua combinação com elementos e efeitos secundários.

#### Acceptance Criteria

1. WHEN Reactor estiver ativo sobre uma aplicação elegível de Burn, THE Sistema_de_Protocolos SHALL aplicar a amplificação declarada sem multiplicar o mesmo componente repetidamente por recursão.
2. WHEN Resonance receber um impacto secundário elegível, THE Sistema_de_Protocolos SHALL calcular a amplificação a partir dos status e ranks definidos no instante de avaliação.
3. WHEN Overflow receber seu gatilho elegível sobre um alvo elemental, THE Sistema_de_Protocolos SHALL produzir o dano adicional declarado, com origem identificada e sujeito ao orçamento compartilhado.
4. THE Sistema_de_Protocolos SHALL preservar o comportamento existente de Overflow sem consumo de status, salvo mudança explícita aprovada no design e acompanhada de migração e testes.
5. THE Design_de_Protocolos SHALL definir se cada efeito observa status anteriores ao impacto ou também status introduzidos pelo próprio impacto, e manter essa ordem na apresentação e nos testes.
6. WHEN um pré-requisito estiver ausente, THE Sistema_de_Protocolos SHALL seguir o comportamento declarado e informar a condição, sem anunciar uma amplificação inexistente.
7. THE Sistema_de_Protocolos SHALL garantir alcance real desses três Protocolos na progressão publicada conforme Requirement 2, sem depender de uma build montada apenas em teste.

### Requirement 6: Composição e contratos de impacto

**User Story:** Como jogador, quero que os Protocolos interajam com o combate, para obter combinações fortes sem quebrar as regras da arma ou da sala.

#### Acceptance Criteria

1. THE Sistema_de_Protocolos SHALL identificar a origem de cada efeito como impacto direto, secundário ou DoT e preservar autoria, alvo e sessão correspondentes.
2. WHEN uma ação atingir múltiplos adversários, THE Sistema_de_Protocolos SHALL aplicar o escopo de raiz definido no design sem reiniciar artificialmente o orçamento a cada efeito derivado.
3. WHEN um efeito gerar outro efeito, THE Sistema_de_Protocolos SHALL compartilhar contexto, profundidade e orçamento conforme o contrato da raiz.
4. THE Sistema_de_Protocolos SHALL impedir que dano secundário ou DoT seja contabilizado como impacto nativo direto para ganho de energia da Manopla.
5. WHEN ocorrer uma morte durante uma cascata, THE Sistema_de_Run SHALL aguardar os impactos confirmados e leases de feedback relevantes antes de abrir a recompensa ou concluir o encontro.
6. THE Sistema_de_Protocolos SHALL preservar categorias de impacto, epoch, hitstop e resultados já confirmados, sem reinicializar o combate para encerrar uma cascata.
7. WHEN ocorrer reentrada no processamento de efeitos, THE Sistema_de_Protocolos SHALL impedir resolução duplicada e recursão fora do contexto autorizado.
8. THE Sistema_de_Protocolos SHALL impedir dano inválido, NaN ou infinito de alcançar a resolução de vida/postura.

### Requirement 7: Limites de execução e performance

**User Story:** Como jogador, quero combinações intensas que mantenham o jogo responsivo, para continuar tomando decisões durante combates densos.

#### Acceptance Criteria

1. THE Design_de_Protocolos SHALL declarar limites numéricos de profundidade, impactos derivados e recursos de processamento, distinguindo limites por raiz e limites agregados relevantes.
2. THE Sistema_de_Protocolos SHALL aplicar limites a todos os caminhos derivados, incluindo choque térmico e Overflow, e não somente às cadeias de Conductor/Detonation.
3. WHEN uma cascata exceder um limite, THE Sistema_de_Protocolos SHALL encerrar o trabalho excedente de forma previsível sem criar uma nova raiz para contornar o limite.
4. THE Sistema_de_Protocolos SHALL limitar consultas de alvos, filas, coleções temporárias e efeitos visuais simultâneos, sem crescimento ilimitado em combates prolongados.
5. THE Sistema_de_Protocolos SHALL manter separadas as quantidades de efeitos visuais e impactos de gameplay.
6. THE Validação_de_Performance SHALL comparar uma referência sem a combinação e o cenário com a combinação, registrando hardware, quantidade de inimigos, duração, custos e limites de aceitação definidos no design.
7. WHEN testes técnicos respeitarem os limites mas o gameplay perder legibilidade ou responsividade, THE Processo_de_Aceitação SHALL registrar o problema como pendente, sem considerar apenas a ausência de exceções como aprovação.

### Requirement 8: Apresentação e clareza das decisões

**User Story:** Como jogador, quero entender o que cada Protocol faz e por que um efeito aconteceu, para escolher e reconhecer minhas combinações.

#### Acceptance Criteria

1. WHEN um Protocol for oferecido, THE UI_de_Recompensa SHALL mostrar nome, categoria, efeito principal, rank resultante e dependências relevantes à decisão.
2. WHEN uma aquisição depender de uma combinação, THE UI_de_Recompensa SHALL distinguir benefício imediato de benefício condicionado e informar a condição faltante.
3. THE UI_de_Recompensa SHALL descrever fielmente consumo, renovação, amplificação e origem de dano, sem contradizer o efeito executado.
4. THE Feedback_de_Combate SHALL distinguir fogo, frio, propagação e explosão sem ocultar sinais de ataque e perigos importantes dos inimigos.
5. THE Feedback_de_Combate SHALL respeitar o orçamento visual e evitar sobreposição ilimitada de números, partículas e indicadores.
6. THE UI_de_Rota SHALL manter visível a categoria Protocol e sua oportunidade de aquisição antes de comprometer o ramo, preservando os contratos de escolha e liberação de input da Fase 4.

### Requirement 9: Sessão, transições e compatibilidade

**User Story:** Como jogador, quero manter minha build durante a run e começar a seguinte sem resíduos, para que as escolhas pertençam à sessão correta.

#### Acceptance Criteria

1. WHEN o jogador mudar de sala, THE Sistema_de_Protocolos SHALL preservar aquisições, ranks e recursos existentes sem reaplicar ganhos ou limpar a build.
2. WHEN a run terminar por morte, aborto ou extração, THE Sistema_de_Protocolos SHALL remover inscrições, filas e efeitos temporários pertencentes à sessão encerrada.
3. WHEN uma nova run começar, THE Sistema_de_Protocolos SHALL iniciar sem ranks ou callbacks residuais da sessão anterior.
4. WHEN chegar um callback de sessão encerrada, THE Sistema_de_Protocolos SHALL impedir aplicação de dano ou recompensa na nova sessão.
5. THE Sistema_de_Protocolos SHALL preservar comportamento de Techniques, Mutations, arsenais e modos legados fora das mudanças explicitamente documentadas.
6. THE Sistema_de_Protocolos SHALL preservar referências de cenas, prefabs e assets e verificar a persistência dos dados de autoria após salvar e recarregar.
7. WHEN ocorrer pausa, hitstop, interrupção ou descarte do alvo, THE Sistema_de_Protocolos SHALL encerrar ou suspender trabalho segundo os relógios e regras de cleanup declarados, preservando resultados confirmados.

### Requirement 10: Validação e aceitação da fase

**User Story:** Como responsável pelo jogo, quero evidências de funcionamento e gameplay, para aceitar combinações emergentes sem regressões ou custo excessivo.

#### Acceptance Criteria

1. THE Processo_de_Validação SHALL executar pelo Unity MCP importação, compilação, inspeção do Console e testes EditMode/PlayMode relevantes às alterações, registrando limitações concretas quando indisponíveis.
2. THE Processo_de_Validação SHALL cobrir os sete Protocolos isoladamente e combinações representativas de elementos, propagação, explosão e amplificação.
3. THE Processo_de_Validação SHALL verificar múltiplos alvos, mortes simultâneas, reentrada, limites, dados inválidos, pause/hitstop e cleanup entre sessões conforme o comportamento alterado.
4. THE Processo_de_Validação SHALL demonstrar aquisição e ativação das combinações de aceitação em percursos publicados, além dos testes que montam builds diretamente.
5. THE Processo_de_Validação SHALL verificar que mortes por cascata não bloqueiam conclusão de sala nem duplicam recompensa, e que impactos secundários não geram energia nativa indevida.
6. THE Processo_de_Validação SHALL executar somente testes pertinentes às alterações e dependências afetadas, ampliando a seleção quando falhas ou mudanças adicionais justificarem.
7. THE Aceitação_de_Gameplay SHALL avaliar combinações capazes de produzir consequências emergentes, identificando quais decisões e interações diferem do efeito isolado.
8. THE Aceitação_de_Gameplay SHALL registrar separadamente legibilidade, responsividade e resultados medidos de performance; compilação e testes automatizados não substituem essa aceitação.
9. THE Handoff_da_Fase SHALL registrar decisões, migrações, evidências e pendências sem declarar concluída uma validação Unity ou humana que não tenha ocorrido.

## Cenários de aceitação a detalhar no design

| Cenário | Resultado a observar |
| --- | --- |
| Ignite + Conductor | Propagação e elementos seguem ordem definida, com alvos e orçamento verificáveis. |
| Ignite + Reactor + Detonation | Amplificação e morte elemental geram consequência reconhecível sem duplicar explosões. |
| Frost + Detonation | Variante de frio mantém sinais de perigo e respeita resistências. |
| Elementos + propagação + Resonance | Amplificação secundária corresponde aos status observados e é alcançável pela progressão definida. |
| Elemento + Overflow | Dano adicional e persistência dos status correspondem à descrição. |
| Ataque nativo com múltiplas mortes | Raiz respeita limites; energia, feedback final e recompensa mantêm os contratos da Manopla/run. |
| Encontro denso e prolongado | Custos e efeitos visuais permanecem dentro dos limites medidos. |
| Encerramento e nova run | Nenhum rank, efeito ou callback da sessão anterior interfere na seguinte. |

Essas combinações são cenários para demonstrar composição, não garantia de que a rota atual já permite adquirir todos os componentes. O design deve resolver sua alcançabilidade antes de usá-las como critérios de entrega.

## Decisões necessárias no design

- Resolver elegibilidade e pré-requisitos considerando a quantidade real de aquisições da rota; explicitar qualquer adaptação do contrato de recompensas.
- Definir gatilhos permitidos, ordem de status, rank máximo, repetição e fórmulas dos sete Protocolos.
- Definir fronteira da raiz para ataques com múltiplos alvos e todos os caminhos secundários, incluindo DoT, choque térmico e Overflow.
- Fixar orçamentos e critérios de performance a partir de uma referência reproduzível; os limites existentes são ponto de partida, não evidência de aprovação.
- Reconciliar textos de apresentação com semântica executada e definir sinais visuais compatíveis com o combate atual.

**Registro de elaboração:** documentação da Fase 5 baseada no roadmap e em inspeção estática. Nenhum código, asset ou teste de Protocolos foi alterado ou executado para elaborar estes requirements. As verificações das correções da Fase 4 são registradas separadamente em sua spec.

