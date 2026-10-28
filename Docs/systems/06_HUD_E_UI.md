# HUD e UI — legibilidade antes de ornamento

Fonte documental principal da apresentação e interação de combate/run. As decisões de produto abaixo continuam vigentes; presença de widgets não prova legibilidade aprovada.

## Decisões

- **HUD de combate:** vida e mana/recurso visíveis durante o jogo, inspirado funcionalmente em Diablo; a energia Asura e outros recursos legítimos devem ser distinguíveis.
- **Linguagem de arte:** inspirada em Hades, mas sem importar sua estrutura de HUD.
- **Cards/boons:** mostrar claramente transformação, slot afetado, conflito/pré-requisito e consequências.
- **Fim da run:** dados detalhados por habilidade como referência de conteúdo em Megabonk, numa identidade visual própria.

## Hierarquia em combate

1. Jogador e inimigos ameaçadores.
2. Telegraphs, área perigosa, projéteis hostis.
3. Saúde/recurso/habilidades/cooldowns.
4. Efeitos de boons e textos numéricos.
5. Fundo/decoração.

**Jamais inverter essa ordem só por espetáculo.**

## Implementação inspecionada em 18/10/2026

Inspeção estática no workspace `gameplayOverhaul`, base `3a3f0ee` com alterações locais; sem observação visual no Editor nesta etapa.

- [`PlayerHUD`](../../Assets/_Project/Scripts/UI/PlayerHUD.cs) pertence à cena e recebe `PlayerActor` explicitamente. Requer vida, mana, painel Asura e cinco slots vinculados: Q/W/E/R + dash. `Start` desabilita o componente com erro se faltarem referências; não cria um segundo HUD por procura global.
- `HealthChanged`/`ManaChanged` atualizam barras e números; `AbilityUsed`/`AbilityRejected` alimentam feedback. Slots consultam `AbilityHolder` para habilidade ativa, recarga, bloqueio e binding atual de `GamePreferences`. `HudFormatter`/`UiValueCache` evitam reatribuições textuais quando o valor não muda. Isso é constatação do código, não medição de GC/FPS.
- Asura aparece com manopla equipada/ativa e jogador vivo; mostra energia e estado do R, incluindo espera da Cadenciada e comando de finalizar sequência. O HUD usa a política real de energia para feedback, sem instituir um recurso separado.
- O nome de uma Mutation substitui o nome da ação apresentada. Há estados específicos para carga do Juízo, quantidade de minas/hastes e retorno da Órbita; tooltips indicam custo/recarga e comandos pertinentes. Existência dessas strings não aprova sua clareza em tela.
- O prefab compartilhado [`PlayerHUD.prefab`](../../Assets/_Project/Prefabs/PlayerHUD.prefab) aparece nas cenas `FirstSector`, `PrologueTutorial` e `NexusLobby` por referência serializada. A integridade completa de importação/referências precisa do Editor; leitura de YAML não a substitui.
- [`ManualRunUI`](../../Assets/_Project/Scripts/UI/ManualRunUI.cs) e [`ManualRunUI.PauseMap`](../../Assets/_Project/Scripts/UI/ManualRunUI.PauseMap.cs) apresentam mapa/descoberta, passagem selada, recompensas, reparo, pausa e estado de retorno. A política de [run](05_RUN_E_PROGRESSAO.md) autoriza transições; a UI não deve conceder recompensa por conta própria.
- [`RunRewardUI`](../../Assets/_Project/Scripts/UI/RunRewardUI.cs) apresenta escolhas de `RunBoons`, com descrição e contexto de compatibilidade derivados dos catálogos. [Boons](04_BOONS.md) é a fonte das regras de elegibilidade, substituição e limites.

## Divergências e estado de aceite

**Implementado:** recursos, slots e feedback acima possuem consumidores reais. **Validado por testes:** apenas os cenários históricos identificados na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md); não equivalem a uma revisão visual completa atual. **Gameplay/aparência aprovadas:** não foi identificado aceite autoral que permita declarar o conjunto aprovado nesta consolidação.

**Resultados — atualização VS-02B.2:** a tela analítica uGUI/TMP foi implementada no Nexus, sobre RunSummaryStore, com direção visual provisoriamente aprovada pelo desenvolvedor. Implementação, evidências e limites na seção VS-02B.2 ao final; aceite humano da experiência Unity permanece separado.

## Casos de teste

- Vida baixa, Asura cheia/vazia, cooldowns simultâneos, stun/controle, jogador na borda, combate com swarm/VFX e resolução/crop real.
- Compare Game View/build em dimensões fixas e pós-processamento ligado. Verifique escala/tipografia/ícones e contraste perceptual.
- Tela de card com requisito não atendido, opções mutuamente exclusivas, texto longo e navegação por teclado/controle se houver suporte real.
- Tela final: valores legíveis, causa de morte/resultado e skill top coerente com telemetria.

## Critério de aceite

O jogador consegue identificar recurso restante e ameaça iminente sem procurar em elementos decorativos; testes técnicos sozinhos não provam boa leitura, incluir screenshots e avaliação humana.


## VS-02B.1 — proposta de resultados para aprovação (21/10/2026)

**Status: ESTUDO DE DESIGN / UX, não interface final nem direção visual aprovada.** A VS-02A continua sendo a fonte dos fatos. Não se alteraram o HUD de combate, fluxo de retorno, save, regras ou dados de produção. Protótipo navegável em [Tools/UX/RunResultsPrototype/index.html](../../Tools/UX/RunResultsPrototype/index.html), com `style.css`, `app.js`, snapshot `data.js` e verificações `verify.cjs` no mesmo diretório. Abre localmente em navegador; durante a revisão também foi servido somente em `127.0.0.1:18763/Tools/UX/RunResultsPrototype/`. A barra superior de cenários pertence à revisão e não ao produto.

### Skills efetivamente aplicadas

| Skill verificada no projeto | Aplicação à proposta |
|---|---|
| `tech-guy-game-vision` + `references/vision-and-constraints.md` | Nexus como lugar seguro de consulta, identidade digital, informação causal legível e HUD de gameplay preservado. Paleta/tipografia tratadas como estudo, sem declarar aprovação por código existente. |
| `tech-guy-boon-design` + `references/catalog-and-gates.md` | Separar aquisições, transformação e dano causado; manter famílias registradas, IDs estáveis, origem da habilidade e filhos. Não converter inventário em contribuição, quantidade adquirida em rank ou categoria legada em Augment presumido. |
| `tech-guy-unity-acceptance` + `references/editor-validation.md` | Instância/projeto/Inspector/Console e baseline visual reais; distinguir preview de navegador, render Unity, testes anteriores e aceite humano. |
| `unity-mcp-orchestrator`, global | Descoberta de instância/recursos, leitura de prefab e DTOs primitivos, captura no Editor correto e restauração a Edit Mode. |

Há quatro skills locais instaladas sob `.agents/skills/`; não foi encontrada uma skill local dedicada a UI/UX, tipografia ou hierarquia. Não se inventou instalação. A skill de combate não foi aplicada a alterações mecânicas porque não há alteração de combate nesta etapa. Tipografia, contraste, densidade e feedback da interface são propostas derivadas dos contratos e avaliadas no protótipo.

### Diagnóstico da base existente

Inspeção Unity MCP em `Tech-Guy@fccafb2b47cd7671`, Unity6000.5.10f1, projeto `D:/Dev/Personal/My_games/Tech-Guy`. NexusLobby limpa em Edit Mode, Console sem erros. `PlayerHUD.prefab`: uGUI ScreenSpaceOverlay, CanvasScaler ScaleWithScreenSize, referência1920×1080; LiberationSans SDF, marfim `#DED4BA`, números18 e captions12 na autoria. O Nexus carregado tinha um Canvas PlayerHUD com27 TMP_Text e nenhum EventSystem encontrado nessa inspeção. Isso não valida foco/interação de uma futura tela uGUI.

`ManualRunUI`, `PauseMenuUI`, `RunRewardUI` e `LobbyInteraction` usam IMGUI. Menus de run/pausa trabalham com enquadramento virtual1280×720, superfícies escuras, ciano, dourado, texto claro e navegação própria. `RunRewardUI` já tem scroll da build, agrupamento/metadados via `RunModifierPresentation` e seleção por mouse/teclado. A tela atual de retorno contém outcome/retry, sem detalhamento estatístico. Não há consumidor final de RunSummary na UI.

Baseline novo do Nexus em Play Mode: `Library/GameplayObservation/vs02b1/nexus-baseline.png`, **1280×739**, realmente aberto. Há sobreposição preexistente de textos ambientais do hub; não foi corrigida nesta tarefa. O baseline não é uma captura do novo resultado nem uma comparação de telas em resoluções equivalentes. A nova proposta foi renderizada no navegador em1280×720 e1920×1080. O Play Mode foi encerrado, sem navegar ou iniciar run.

### Conceito: Registro de Incursão

Uma superfície de registro digital com acabamento contido de ARPG: grafite/azul profundo, texto marfim, arestas e divisórias finas, dourado para hierarquia e ciano para efeitos/foco. O sigilo TG e os recortes angulares conectam a tela ao Nexus. Gradientes suaves sugerem metal; não se importa ornamento gótico, mitologia ou assets de outro jogo. Sem ilustração de fundo competindo com os números, chuva de partículas ou contadores animados que atrasem a leitura.

Interpretação das referências fornecidas pelo desenvolvedor: **Megabonk → profundidade e exploração das fontes; Diablo IV → enquadramento/material e hierarquia de ARPG; Hades → contraste e composição estilizada; Tech Guy → identidade digital e causalidade.** Não se reproduz uma tela literal. Páginas oficiais/de distribuição consultadas como referência: [Megabonk](https://store.steampowered.com/app/3405340/Megabonk/), [Hades](https://www.supergiantgames.com/games/hades/) e [press kit Diablo IV](https://blizzard.westus-v2.propressroom.com/en/Diablo-IV-gamescom-Press-Kit). Não foram extraídos assets; a página principal de DiabloIV retornou403 e não foi declarada visualmente inspecionada.

Receita **proposta, ainda por aprovar**: fundo `#080D16`, painel `#131C2A`, texto `#F1E9D7`, secundário `#B2BDC9`, dourado `#D4B476`, ciano `#82D8E6`, alerta `#F0C487`, morte `#E5A19A`. Fonte Liberation Sans já presente no projeto; nenhuma fonte nova. Em720p: título39px, números principais35px, corpo15–16px, rótulos secundários12–14px; em1080p título48 e números44, sem esticar o painel além de1680px. Meta: corpo principal legível e detalhes progressivos, não encolher tudo para caber.

### Hierarquia e interação

1. **Desfecho + arma inicial + tempo decorrido**: leitura antes dos números. ID abreviado, identidade completa e seed em detalhes.
2. **Três fatos principais**: dano efetivo causado, inimigos derrotados e dano recebido. Nada de score, DPS, bônus inventado ou comparação com run anterior.
3. **Visão geral**: barras horizontais por habilidade, ordenadas por HP efetivo, todas com denominador `EffectiveDamage`. Segmento sólido nativo + segmento hachurado secundário incluído; número e percentual sempre acompanhados de texto. Origem incerta recebe linha própria com `?`, rótulo explícito e hachura âmbar. Não usar gráfico de pizza ou barras em escala logarítmica.
4. **Destaque factual + aquisições**: “Maior dano registrado”, nunca “habilidade mais importante”. Três itens de build no resumo; restante em aba própria. Nenhuma porcentagem atribuída a Mutation/modificador por sua presença no inventário.
5. **Exploração sob demanda**: clicar/Enter na habilidade abre detalhe nativo/filhos/usos/acertos. Aba Fontes permite ler secundários, nativos ou todas as parcelas; seu subtotal **já está incluído** no total. Aba Build e percurso mostra famílias/aquisições, recompensas confirmadas e identidade adicional.
6. **Ação principal fixa**: “Continuar no Nexus”, fechando somente o painel quando a chegada ao Nexus já foi concluída. Não inicia nova run, reaplica recompensa ou chama um loader paralelo. No protótipo, apenas troca para um estado local de revisão e oferece reabrir o último resultado.

Abas por clique, Tab e setas/Home/End; Enter/Espaço acionam o controle em foco; PageDown rola conteúdo extenso; Escape fecha o detalhe e restaura foco. Rótulo/contorno/posição identificam seleção, não só cor. O detalhe usa dialog modal do navegador, sem interceptar teclado globalmente. Esses comportamentos foram verificados **no protótipo**, não certificados no Input System do jogo. A transposição deve respeitar bindings/gates reais e exigir soltura antes de voltar a comandos de mundo; as letras Q/W/E/R no gráfico são identificadores de slot, não promessa de bindings fixos.

### Mapeamento ao RunSummary existente

| Informação de UI | Campo/fonte | Regra de apresentação |
|---|---|---|
| Disponibilidade | `RunSummaryStore.TryRead(out summary, out diagnostic)` | Falha → estado sem dados; não construir resumo de zeros nem inferir outcome. Diagnóstico técnico não precisa ocupar o título do jogador. |
| Desfecho | `Outcome`, `Finalized` | Somente Victory/Death/Aborted finalizados. Elite derrotada não vira Victory. |
| Arma e identidade | `InitialWeapon`, `SessionId`, `Seed`, `StartedUtc` | Rotular arma **inicial**, não arma final presumida. ID/seed completos em detalhes. Não oferecer histórico, que o Store não mantém. |
| Duração principal | `WallSeconds` | mm:ss, inclui pausa, espera e percurso; não “tempo de combate”. `ScaledSeconds` apenas em detalhe como tempo simulado. Sem DPS. |
| Dano causado | `EffectiveDamage` | HP efetivo atribuído ao jogador; não somar `ObservedEnemyHpLoss`, overkill ou subtotal de efeitos. |
| Dano recebido | `DamageReceived` | HP perdido, sem nome de agressor ou causa de morte inventados. |
| Inimigos derrotados | `EnemiesDefeated` | Mortes observadas; `PlayerCreditedKills` é detalhe de crédito letal, não substituição silenciosa. |
| Barras por habilidade | `Damage` com `PlayerAttributed=true`, agrupado por `Weapon + Ability` | Mesma partição exclusiva da VS-02A; soma das linhas = total, incluindo filhos. `Abilities.EffectiveIncludingChildren` é visão equivalente, **não parcela adicional**. |
| Nativo/secundário | `Secondary`, `Producer`, `ProducerId`, `Effective` | Preenchimento sólido/hachurado e tipo textual. Subtotal na aba Fontes pertence ao mesmo total. Produtor nativo Persistent não deve virar proc pelo nome. |
| Origem ambígua dentro do total | `Ability=Unattributed`, `Attribution`, `PlayerAttributed=true` | “Origem de habilidade incerta”, sem divisão estimada entre pais. `mixed_burn_origins` é tradução de ambiguidade, não estimativa. |
| HP sem autoria do jogador | `UnattributedEnemyHpLoss`, parcelas `PlayerAttributed=false` | Aviso separado **fora do seu total**; não confundir com habilidade incerta do jogador. |
| Transformação/modificador | `MutationId`, `ModifierTags` | Tags do impacto no detalhe; não percentual exclusivo nem efeito adicional fictício. |
| Usos e acertos | `Abilities.Uses`, `NativeHpTransactions`, `SecondaryHpTransactions` | Usos comprometidos, não tentativas de input; acertos por alvo com perda positiva de HP, não precisão. Dado de habilidade ausente → traço. |
| Build | `Build[].Id/Family/Acquisitions` | “Adquiridos na run”; quantidade adquirida **não rank ou equipamento ativo**. Mutation/Augment/Protocol/etc. separados conforme família registrada; TechniqueOrLegacy em “Outros efeitos”. Não reclassificar uma família desconhecida por aparência. |
| Rótulos | IDs do DTO + metadados estáticos já verificados | “Mil punhos” conferido no catálogo para `weapon_FlurryEcho`. O snapshot não guarda nome histórico de cada skill; o estudo usa AçãoQ/W/E/R. Resolver nomes definitivos requer proposta explícita, não consultar a arma equipada hoje para reescrever o passado. |
| Recompensas/percurso | `Rewards[].Id/Kind/Alternative/ScaledSeconds`, identidade da run | Histórico de concessões confirmadas. Não transformar `Progress.Count` em número de encontros concluídos; sampling de fase não é medição completa da rota. |
| Destaques | HP agrupado; `LargestImpact`, `Overkill`, `OverkillComplete` em detalhes | Maior dano registrado, incluindo filhos. Overkill separado do total; se incompleto, rotular parcial. Não inferir eficácia tática, dano evitado, melhor sinergia ou contribuição marginal de cada boon. |
| Qualidade do registro | `ObserverErrors`, `Diagnostic`, listas opcionais, retorno do Store | Aviso de parcial quando há erro de coleta; origem incerta é outro eixo e pode existir num registro completo. Lista null → indisponível; lista válida vazia → nenhuma aquisição/recompensa. |

Limitação do schema1: seus escalares são primitivos sem flags individuais de disponibilidade. Um campo ausente num JSON pode desserializar como0; a UI não consegue distinguir esse caso de um zero registrado apenas pelo DTO. O estudo diferencia **Store ausente/inválido** de **resumo válido de valores0** e listas ausentes/vazias, sem prometer detecção de toda ausência individual. Qualquer ampliação do contrato de disponibilidade, nome histórico de skill, rank ativo ou slot final fica fora desta etapa e precisa aprovação.

Números arredondados a uma casa decimal só na apresentação; somas/percentuais usam os valores completos. Valor positivo menor que0,1 aparece como “<0,1”, nunca0; totais arredondados de linhas podem ter diferença visual de arredondamento e não devem ser recalculados somando strings. Percentuais ficam ausentes se total0. Não normalizar cada barra pela maior linha, porque ocultaria sua participação no total.

### Estados desenhados

| Estado | Texto/ação | Integridade |
|---|---|---|
| Victory | “Extração concluída” / “Run concluída por extração.”; dourado contido | Somente Outcome Victory; sem vitória por Elite ou por retorno qualquer. |
| Death | “Morte na incursão” / “Seu personagem foi derrotado.”; coral suave | Sem culpado/causa de morte ou penalidade não disponíveis. |
| Aborted | “Incursão abandonada” / “Você encerrou o percurso antes da extração.”; azul acinzentado | Sem troféu, texto de vitória ou confusão com morte. |
| Sem resumo / inválido | “Resultado indisponível”; métricas “—”; ação de continuar no Nexus | Não inventar outcome, percentuais ou números0. |
| Registro parcial | Aviso no cabeçalho e área de conteúdo, mantendo fatos registrados | Não declarar dados integrais só porque reconciliam. |
| Origem incerta | Linha e alerta com valor, dentro/fora do total explicitados | Não dividir dano proporcionalmente entre pais possíveis. |
| Resumo válido sem dano/kills/recompensas | Valores0, explicação e listas vazias | Sem skill vencedora, gráfico vazio com percentuais ou confete. |
| Consulta no Nexus | “Consulta do último resultado”; mesma identidade, só fechar/reabrir | Leitura não concede recompensas nem substitui resultado por fictício. Não há carrossel de histórico. |

### Protótipo e assets

O estado inicial usa exportação **real e congelada** de `RunSummaryStore` por Unity MCP, sessão `c7fa8269-0403-4043-b437-25c1b7fcab40`: Aborted,564HP,9 inimigos,47,999878HP recebidos e58,910534s reais. Não é conexão ao vivo nem uma nova run. Outros estados são **dados fictícios de estudo**, identificados na barra e com ID de exemplo próprio; build extensa é stress de layout, não demonstra aquisição, compatibilidade ou liberação de conteúdo. Nenhum preset grava no Store/PlayerPrefs.

Escolha de prototipação: HTML/CSS/JS local, fora de Assets e sem plugin adicional, permitindo revisar leitura/interação em duas resoluções sem alterar Unity. Reutiliza **por referência** a LiberationSans.ttf existente, dados reais do DTO e a família visual inspecionada; barras, divisórias e sigilo são geometria/CSS, sem imagem decorativa gerada. O servidor local, quando ativo, serve apenas a revisão; não é site publicado nem dependência do jogo. Para reproduzir os checks: disponibilizar Playwright no NODE_PATH e executar `node Tools/UX/RunResultsPrototype/verify.cjs` com Edge instalado (ou TG_BROWSER_CHANNEL configurado). Saída em `Library/GameplayObservation/vs02b1/`.

Para o produto, reaproveitar: `RunSummaryStore` como fonte, `RunModifierPresentation.For`/catálogos para metadados de apresentação estáticos, família de estilos e scroll de `RunRewardUI`/`ManualRunUI`/`PauseMenuUI`, Liberation Sans, e contratos de soltura de `RunSelectionReleaseGate`/controles. **Não** chamar `Count/Hint/Offered` de apresentação dependentes de `RunBoons` vivo para reconstruir build após teardown, nem reaplicar recompensas do histórico. `PlayerHUD` e seu Canvas permanecem intactos.

Renderer final **por decidir antes da VS-02B.2**: IMGUI é o caminho de menor mudança para acompanhar os menus existentes e seus gates; uGUI/TMP reutiliza a tecnologia de texto do HUD, mas um painel interativo precisará de EventSystem/InputSystemUIInputModule, foco, navegação e ownership verificados, sem duplicação. Nenhum dos dois foi implementado aqui; não há proposta de terceiro framework ou manager de estatísticas.

Assets: nenhum novo asset Unity é necessário para revisar este estudo. Depois do aceite, um painel9-slice simples e ícones próprios de arma/categoria podem ser aprovados **se** ajudarem reconhecimento/escala; evitar ícones genéricos decorativos, textura atrás de texto ou nova fonte sem necessidade. Letras/símbolos e materiais atuais bastam para uma primeira transposição funcional.

### Critérios de UX, riscos e decisões pendentes

Checks de navegador:58 combinações de estado/aba/resolução, sem overflow horizontal ou da página, ação principal visível, texto dos elementos avaliados sem corte horizontal; scroll interno para build longa, foco de abas, Escape/retorno de foco e reabertura por Enter. Pares de texto avaliados contra painel `#131C2A` tiveram contraste mínimo8,06:1. Não é certificação WCAG de todos os estados, leitor de tela, HDR, daltonismo ou Input System Unity. Inspeção visual e resultados estão na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md).

Proposta de aceitação humana: após combate, identificar outcome, duração e três números principais sem abrir aba; reconhecer que os efeitos já pertencem às habilidades; explicar a diferença entre item adquirido e dano confirmado; encontrar build/efeitos com mouse e teclado; distinguir0 de“—” e origem incerta de autoria não confirmada. Realizar com captura720p e1080p, textos expandidos, build extensa e distribuição muito desigual. **Não houve esse playtest humano nesta etapa**; nenhuma nota de satisfação, tempo de compreensão ou limiar de leitura foi inventado.

Aprovar antes de implementar VS-02B.2:

1. Direção visual, densidade, fonte reutilizada, barras sólidas/hachuradas e as três abas.
2. Nomenclatura dos três outcomes e “Maior dano registrado”, mantendo os limites de interpretação.
3. Abertura automática **somente após chegada confirmada ao Nexus**, mais entrada manual “Último resultado” no hub; proposta não muda o loader/retorno nem bloqueia sua recuperação. Enquanto retornar/falhar, permanece a autoridade do lifecycle atual. Abertura automática exige identificar sessão nova/evitar reapresentação indevida, não supor que um resumo velho é desta run.
4. Renderer da primeira implementação e tratamento de nomes históricos indisponíveis. Manter slots por enquanto não exige mudar save; enriquecer schema ou resolver snapshot de nomes exige escopo próprio.


## VS-02B — revisão e preparação da implementação (22/10/2026)

**Proposta para aprovação; não implementa VS-02B.2 nem concede aceite visual.** Mantém a receita e os dados do estudo acima. A investigação do Store está no [lifecycle de run](05_RUN_E_PROGRESSAO.md#investigação-do-último-resumo--vs-02b-22102026); não se muda a persistência para resolver apresentação.

### Skills acessíveis e aplicação

Inventário do projeto: as mesmas quatro skills em `.agents/skills/`. Foram relidas integralmente `tech-guy-game-vision`, `tech-guy-boon-design`, `tech-guy-unity-acceptance` e suas três referências: identidade digital e leitura causal; aquisições sem contribuição presumida; evidência técnica separada de aceite. `unity-mcp-orchestrator` global também foi lida integralmente, aplicada na descoberta e inspeção somente leitura. Nenhuma skill local específica de UI/UX/tipografia foi encontrada.

O ambiente pessoal contém também `tech-guy-world-identity`, `tech-guy-world-journey`, `tech-guy-level-layout`, `tech-guy-enemy-encounters`, `tech-guy-map-storytelling`. A primeira foi lida integralmente para avaliar pertinência, mas sua responsabilidade são universos/mapas: não foi usada para inventar uma direção de interface. As demais não são instruções de UI e não foram declaradas utilizadas. O cache acessível contém skills Figma (generate-design, generate-library, use, design-to-code e outras), visualize e templates de artefatos. A disponibilidade foi conferida; descrições/escopos de generate-design e visualize foram consultados para triagem, não aplicados como se fossem uma revisão estética geral. Não houve autoria em Figma ou visualização inline HTML nova, nem leitura integral/aplicação indevidamente atribuída a essas skills. Não se instalou uma skill nova. Contratos artísticos e HUD/UI, e não um nome de skill inexistente, fundamentam composição e tipografia desta revisão.

### Revisão visual e de interação

As sete imagens solicitadas foram abertas: overview, Victory, Death, Aborted, sources, build extensa e ausência de estatísticas; overview1080p também foi aberto. Todas são renders do protótipo HTML, não UI nova no Unity. Reexecução independente dos58 checks em1280×720/1920×1080 aprovada, em `Library/GameplayObservation/vs02b-review/ux-checks.json`. Testes de layout/foco não aprovam estética.

**Pontos fortes observados:** título e três números principais dominam a leitura; conteúdo secundário fica nas abas; ação de fechamento continua visível; legendas/textos complementam a cor; preenchimento nativo/secundário e subtotal tornam a causalidade explícita; ausência não vira zero. O sigilo angular e materiais discretos preservam o tema digital sem ornamentação atrás dos números. Liberation Sans mantém coerência com recursos existentes.

**Problemas e ajustes recomendados antes/durante a transposição, sujeitos à revisão visual:**

1. A barra de estudo ainda diz “Último resumo real”, embora seu `data.js` seja um snapshot arquivado `c7fa8269`. Não confundir com o Store atual `bfc32dfb`. Na revisão, rotular o arquivo como snapshot histórico; no produto, mostrar ID abreviado e início local da run, identidade completa nos detalhes. As capturas originais são preservadas, sem retoque.
2. A linguagem é limpa, porém ainda próxima de um painel analítico genérico. Identidade Tech Guy é uma hipótese visual, não uma conquista demonstrada. Validar enquadramento angular/sigilo com o desenvolvedor antes de encomendar arte; não adicionar fundo ruidoso para compensar.
3. Em720p há muitas notas explicativas; em1080p sobra área abaixo das cinco linhas. Recomenda-se reduzir repetição, aproximar o bloco de conteúdo do título e manter explicações causais nos detalhes, preservando o aviso de inclusão dos efeitos. Não aumentar fonte por auto-fit nem redistribuir barras por um denominador diferente.
4. Na build extensa, itens continuam abaixo da dobra. Scroll funciona, mas merece indicador visível de continuação e foco que acompanhe o item selecionado. Nomes técnicos de famílias/tickets como “Technique” precisam de rótulos de apresentação, preservando IDs/categorias reais. Não vender a lista adquirida como build ativa final.
5. “Ação Q/W/E/R” é honesto com o schema, mas pouco expressivo. Manter slots como fallback nesta fatia, sem buscar no equipamento atual um nome histórico falso. Descrições estáticas verificadas podem enriquecer detalhes; novos campos históricos exigem escopo próprio.
6. As barras têm escala correta em relação ao total e destacam mal parcelas muito pequenas; números/detalhes são essenciais. Não impor largura mínima que aumente artificialmente contribuição. Títulos/cores diferenciam os três outcomes sem depender só da cor; maior celebração de Victory é decisão estética pendente, não requisito técnico.
7. O protótipo permite Escape no detalhe, mas não implementa Escape para fechar a tela principal. O retorno por botão está validado no navegador. A implementação deverá acrescentar cancel/back da tela principal, prioridade sobre pausa e soltura; não declarar isso pronto pelo teste do modal.

### Arquitetura recomendada: uGUI + TextMeshPro

Recomendação para aprovação: Canvas próprio de resultados no Nexus, `ScreenSpaceOverlay`, `CanvasScaler` com referência1920×1080, layout responsivo verificado em720p/1080p, LiberationSans SDF reutilizada; `Button`, `ScrollRect`, máscaras e navegação explícita. `PlayerHUD.prefab` permanece intacto: é fonte de tecnologia/fonte, não um painel interativo pronto para reutilizar inteiro. MCP confirmou27 TMP_Text, scaler e ausência de GraphicRaycaster no prefab; Nexus carregado possui0 EventSystems. As assemblies uGUI, TMP e Input System estão carregadas; configuração estática `activeInputHandler: 2` (Both).

Autorizar na VS-02B.2 um EventSystem **único** no Nexus com `InputSystemUIInputModule` e ações UI explícitas (point/click/scroll/move/submit/cancel). Adicionar GraphicRaycaster ao Canvas novo, não ao HUD por efeito colateral. Detectar/reutilizar ownership existente caso cenas aditivas o tragam; não criar outro sistema a cada abertura. A [documentação oficial de UI Input Module](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/using-ui-input-module.md) descreve a ligação de ações ao EventSystem; conferir API/pacote instalado1.20.0 na implementação. Não migrar todos os menus IMGUI, instalar framework ou criar manager global de analytics.

Separação proposta: presenter de cena recebe snapshot uma vez, formata um modelo de apresentação sem UnityObjects, view somente desenha/navega. `RunSummaryStore` e `RunStatistics` continuam fontes existentes. Nenhuma recontagem de eventos de HUD, persistência da UI ou reatribuição de dano.

**Conflito real de input:** `AbilityHolder.BlocksWorldInput` considera inicialização, pausa, lobby e rewards; não conhece uma futura tela de resultados. `LobbyInteraction.Update` observa pausa, não EventSystem. Raycast/foco sozinhos não bloqueiam polling de teclado/movimento. A menor integração futura é dar ao dono existente de interação do Nexus a informação de painel modal aberto e encaminhá-la aos gates atuais (inclusive estação/pausa), com refs serializadas; observar mouse/teclado segurados e chamar `RequireAttackRelease` ao devolver controle. `RunSelectionReleaseGate` é padrão reutilizável de soltura, não API pronta de modal de resultado. Não mudar cancelamentos/timing do combate, bindings globais ou Time.timeScale só para abrir esta tela. Pausa existente mantém autoridade de clock/audio; cancel do resultado precisa consumir o evento antes da pausa. Testar isso no Unity, inclusive movimento já em curso.

### Fluxo proposto e estabilidade do snapshot

- **Fim de run:** produção finaliza/persiste e realiza teardown/retorno como hoje. Falha no loader mantém retry existente; resultado não deve fingir chegada concluída.
- **Chegada ao Nexus:** abrir uma vez após cena e dependências prontas, apenas para o SessionId que acabou de retornar. A integração de apresentação deve transportar uma intenção transitória de abertura com o ID pelo fluxo de retorno existente; isso ainda não existe. Não usar simplesmente “Store tem dado” como prova de nova run, nem alterar o loader mecânico/outcome. Se necessário um pequeno handoff de ID, mantê-lo na responsabilidade da apresentação/retorno, sem segundo session manager, novo save ou event bus.
- **Enquanto aberta:** `TryRead` uma vez na abertura; reter a cópia e o modelo local por toda a consulta, sem polling do Store e sem reaproveitar DTO mutável global. Uma escrita posterior não troca título/abas/números. Consultas independentes podem mostrar resultados diferentes legitimamente.
- **Fechar:** botão “Continuar no Nexus” ou cancel/back; detalhe fecha antes da tela principal. Devolver foco ao ponto de entrada manual quando existir e exigir soltura antes de acionar mundo/estação/habilidade.
- **Consulta manual:** entrada “Último resultado” no Nexus, acessível sem depender de estar perto da estação de saída. Cada nova abertura lê o Store novamente; não concede recompensa. Isso atualiza a consulta após uma nova run, sem trocar a consulta já aberta.
- **Reinício:** manter último resumo persistido para consulta manual. Recomenda-se não reabrir automaticamente um resultado velho a cada boot, sem criar chave de “já visto”; a intenção automática é transitória e vale para um retorno nesta execução. Não é save/resume de run.
- **Sem dados ou ID esperado divergente:** nunca apresentar registro antigo como resultado da run recém-encerrada. Para consulta manual, estado indisponível com “—” e fechar. Para retorno automático, aviso de resultado indisponível para a run esperada e possibilidade de consultar separadamente o último resultado salvo. Diagnóstico técnico fica fora do título.

### Plano VS-02B.2 para aprovação

| Fatia | Escopo e reutilização | Critério técnico / evidência |
|---|---|---|
| B2-0 — isolamento da validação | Auditar fixtures integradas que finalizam runs; preservar/restaurar chave de resumo no setup/teardown, sem modificar Store para mascarar testes. Já demonstrado que ManualPublishedRun não a preserva. | Comparar bytes/ausência da chave antes/depois de teste; todos outcomes podem ser validados sem substituir o resultado do desenvolvedor. Falha/interrupção de runner continua risco a documentar. |
| B2-1 — leitura e modelo | Presenter/modelo puros consumindo DTO existente; snapshot estável, estados, ID, formatação e metadados estáticos. | Testar leituraA/aberturaA/escritaB/telaA/fechar/reabrirB; totais reconciliados, desconhecido/zero/lista ausente e nenhuma escrita da view. |
| B2-2 — tela uGUI | Canvas separado, TMP, três abas, fontes, build/scroll, detalhe, seleção e cancelar; novo EventSystem apenas onde necessário. | Import/compilação/Inspector/GUIDs, foco e captura720p/1080p com sete estados identificados. Avaliação humana da direção antes de ampliar acabamento. |
| B2-3 — Nexus/retorno | Intenção de abertura com SessionId no fluxo existente; entry manual, gates de input e cleanup de callbacks. | Victory/Death/Aborted, retorno com retry, boot/consulta sem dado, fechar/segurar tecla/clique, pausa, reabertura e segunda/terceira run sem listener/eventSystem duplicado. |
| B2-4 — aceite | Comparação dos fatos na UI com o snapshot/eventos originais; fronteira explícita entre fictício e produção. | EditMode do modelo; PlayMode de presenter/input/retorno, reinício em build isolada, Console sem limpeza. Não escrever dados fictícios no save real para screenshots. Playtest humano de compreensão/legibilidade separado dos asserts. |

Arquivos/sistemas potencialmente afetados depois do aceite: scripts/prefab de resultados em UI, NexusLobby e referências de entrada, integração estreita em LobbyInteraction/PauseMenuUI/gates e retorno manual, fixtures que persistem. Nenhuma alteração prevista em dano, boons, progressão, HUD de combate ou contrato/schema do Store. Não implementar um histórico, nomes históricos, ranks finais ou nova persistência nesta fatia.

**Decisões pendentes:** aprovar/revisar receita e densidade visual; títulos dos outcomes e destaque factual; uGUI/TMP com EventSystem e integração modal; autoabertura no Nexus apenas para nova run, manual após boot e slots como fallback histórico. As sete capturas são a proposta concreta a revisar; nenhuma aprovação é inferida do silêncio ou dos58 checks.

## VS-02B.2 — resultados no Unity (22/10/2026)

**Implementado no Nexus; direção do protótipo aprovada provisoriamente pelo desenvolvedor no pedido desta etapa.** Sem reformulação estética, mudança de gameplay ou de persistência. As propostas B2 anteriores ficam como histórico; esta seção registra o comportamento entregue. Skills de visão, boon-design, unity-acceptance e unity-mcp-orchestrator/referências já lidas foram aplicadas à causalidade, recursos existentes, routing e aceite técnico separado de humano.

### Responsabilidades e componentes

- `RunResultsPresenter`: referências serializadas para jogador, lobby, fonte e EventSystem; leitura única de `RunSummaryStore.TryRead` por abertura, modal e soltura de input. Não consulta CurrentRunSummary vivo ou RunBoons destruído.
- `RunResultsModel`: cópia privada profunda do DTO e modelo de apresentação. Agrupa a partição `Damage` por arma+habilidade, mantendo nativo/secundário, origem incerta e autoria desconhecida fora do total. Não modifica o DTO recebido, grava dados ou estima contribuição marginal. Percentuais e somas usam números completos; arredondamento só no texto.
- `RunResultsView`: Canvas próprio ScreenSpaceOverlay/CanvasScaler1920×1080/GraphicRaycaster e widgets uGUI/TMP construídos uma vez pelo componente da cena. Reutiliza LiberationSans SDF existente, sem novos assets decorativos ou alteração de PlayerHUD/prefab. ScrollRect, scrollbar, três abas, foco explícito, detalhes e fechamento. Linhas textuais ajustam altura ao conteúdo; itens de habilidades mostram barras nativas/secundárias e valores.
- `RunResultsFocus`: contorno de foco uGUI, sem depender apenas de cor do outcome/hover.
- `RunResultsHandoff`: apenas ID transitório de sessão, resetado no início do runtime e consumido uma vez no Nexus. Não é novo coletor, event bus, session manager ou save.

NexusLobby contém o presenter/view com dependências serializadas e um EventSystem com InputSystemUIInputModule/ações UI. A view cria somente seus widgets; não cria EventSystem a cada abertura. Cena salva pelo MCP, recarregada pelas fixtures e por ciclos Play; GUIDs preexistentes preservados. O HUD e os outros menus continuam usando seus componentes anteriores.

### Fluxo e input entregues

`BeginManualReturn` preserva a ordem de teardown/persistência e acrescenta o ID esperado ao handoff antes do loader existente. O presenter só o consome no Start do Nexus efetivamente carregado. Falha/retry não regrava resultado nem fabrica Victory; o teste de retry também confere que a identidade esperada é mantida. Boot sem handoff não autoabre resumo antigo. Acesso manual “Último resultado” no canto superior direito, inicialmente focado para Enter, relê o Store a cada abertura. Divergência de ID apresenta indisponível para aquela consulta; fechar e consultar manualmente permite acessar separadamente o último salvo.

O modelo permanece congelado ao alternar abas/detalhes, mesmo se outro resumo for gravado. Fechar/reabrir lê o novo registro. Victory/Death/Aborted, arma inicial, duração com pausas, identidade/início, HP causado/recebido, inimigos, fontes, boons adquiridos, recompensas, maior impacto e fases registradas estão disponíveis. A lista adquirida não afirma rank/configuração final; o histórico de fases não vira número inferido de encontros. Sem resumo válido: traços/explicação e fechar; zero válido continua zero. Sem DPS, causa de morte ou contribuição exclusiva de Mutation inventados.

`LobbyInteraction.IsPanelOpen` incorpora o modal/soltura e mantém os gates existentes de AbilityHolder/CharControlScript; lobby/pausa não processam input através do painel. O presenter usa `TryStopEncounterOffense` existente no Nexus antes de abrir, respeitando sua recusa durante impacto comprometido e descartando buffers; não altera implementação/regra de cancelamento, custos ou cooldowns. NavMeshAgent mantém seu estado anterior de parada; cursor visível/destravado durante consulta e restaurado ao liberar ownership.

Tab/Shift+Tab, navegação do EventSystem, Enter, mouse, scroll, PageUp/Down e Escape funcionam. Detalhe desabilita seleção do conteúdo atrás dele; Escape fecha primeiro detalhe, depois painel. Um guard de frame impede que o Enter de abertura também submeta o botão Continuar recém-focado. Fechamento mantém bloqueio até um frame posterior com teclado/mouse liberados, chama RequireAttackRelease e devolve foco ao acesso manual. Desativação/destruição libera movimento/cursor e limpa estado; reativação com detalhe anterior não deixa botões desabilitados. Time.timeScale/audio não são alterados pelo modal.

### Validação e limites atuais

Resultados completos e jobs na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md#vs-02b2--implementação-dos-resultados-22102026). Capturas reais de Game View em `Library/GameplayObservation/vs02b2/results-overview-720.png` e `results-overview-1080.png`; fontes/build nas duas resoluções e imagens de origem incerta, subtotal, fatos e acesso no Nexus. Todas as10 capturas finais foram abertas. Três PNGs iniciais foram preservados como desenvolvimento, não confundidos com estado final.

Os renders finais consomem o resumo real arquivado/persistido `bfc32dfb`:4266HP,44 mortes observadas,341,999512HP recebidos (342 na UI),218,749395s (03:38), maior impacto66HP. As6 linhas por habilidade e as20 parcelas somam exatamente4266;229 de origem de habilidade incerta estão dentro desse total. `ui-consumed-summary.json`, `ui-visible-text.txt` e `ui-numeric-comparison.json` vinculam dados e tela. Origem histórica dessa Victory continua não atribuída; não é alegação de nova vitória natural. Nenhum dado fictício foi gravado para preparar screenshots.

Vitória/morte, vazio/inválido/zero,35 itens de build e escritaB durante consultaA foram testados em fixtures explicitamente sintéticas, com restauração do Store. Três runs de produção foram iniciadas/abandonadas por API na mesma sessão Play, sem injetar HP/dano/recursos; não são playtest humano nem vitória natural. As rotas publicadas ainda usam sua assistência documentada em testes. Persistência foi verificada entre ciclos Stop/Play com novo runtime e leitura do registro Windows; não houve reinício de processo standalone/build. Esse teste e avaliação humana de leitura/feel ficam pendentes para B3. Nomes históricos de habilidades e disponibilidade de cada escalar continuam limitados pelo schema1; fallback por slot preservado.

## VS-03.1 — Nexus: circulação e interação (22/10/2026)

Três melhorias funcionais, sem redefinir arte, topologia ou progressão. O Nexus continua espacial: chegada ao sul, arsenal a oeste, treino atrás dele, arquivo a leste e saída para FirstSector ao nordeste. Fragmento/Origem continuam terminais informativos sem destinos jogáveis.

- **Ownership de interação:** referências serializadas ligam LobbyInteraction e LobbyArsenal, que vivem em objetos distintos. O gate existente de AbilityHolder reserva o binding de Skill3 perto dos pedestais; o arsenal consulta o mesmo dono de modal/soltura usado pelos resultados. Assim, equipar não dispara skill e resultados não permitem uma troca oculta.
- **Sinalização:** nomes curtos e dimensões menores nos três pedestais, título do arsenal e treino; destino do portal nomeado First Sector. Nenhuma posição de estação, collider, material ou NavMesh alterado. Tamanhos originais dos cabeçalhos dos portais preservados.
- **Contexto do hub:** LobbyInteraction apresenta orientação, bindings atuais e família da arma realmente equipada. PlayerHUD recebe referências somente no Nexus e oculta seu Canvas fora do raio de 4,5 unidades do alvo de treino ou durante o modal. HUD oculto não bloqueia cliques. Ao treinar, recursos/skills continuam disponíveis; FirstSector conserva o HUD normal e o prefab compartilhado permanece intacto.

Sem novo manager, persistência, menu de equipamentos ou atividade. Os cards existentes e sua indicação EQUIPADA continuam sendo usados; não se declarou inexistente o feedback que já possuíam. Resultados/RunSummary, dano, custo, recompensas e desbloqueios não tiveram seus contratos alterados. As referências da cena foram salvas e recarregadas no Editor.

Evidências, cinco problemas priorizados, mapa funcional, métodos de input, regressões e pendências estão na [matriz VS-03.1](../validation/01_MATRIZ_DE_VALIDACAO.md#vs-031--nexus-usabilidade-e-interação-22102026). A experiência foi exercitada com input simulado, sem Warp; aprovação humana e acabamento artístico continuam pendentes.

## VS-03.2 — leitura espacial do Nexus (22/10/2026)

A organização das estações, câmera40°, geometria de circulação e NavMesh permanecem iguais. Três ajustes locais de apresentação foram implementados:

- **Cards de proximidade:** margem inferior esquerda, com alternativa à direita se o avatar ocupar essa faixa. A área central, o guia, o acesso aos resultados e os nomes dos três pedestais ficam livres nos percursos720p/1080p. Seleção e ação continuam pertencendo ao pedestal mais próximo; não foi criado menu de equipamentos.
- **Equipamento físico:** cada pedestal recebeu uma placa TextMesh EQUIPADA, clonada do recurso de sinalização existente. LobbyArsenal mostra somente a placa correspondente à arma realmente equipada. A projeção da placa respeita a faixa inferior reservada ao HUD de treino. Preços, desbloqueios e persistência continuam em WeaponLoadout/CurrencyWallet.
- **Orientação:** o rótulo TREINO saiu do chão sob o personagem e foi colocado ao lado do alvo. O guia conserva os bindings atuais e usa uma indicação direcional calculada para o portal jogável ciano; esta indicação cede lugar ao prompt existente quando perto da incursão. Não foram promovidos portais informativos a atividades.

Ownership VS-03.1, HUD contextual e implementação VS-02B.2 preservados. O card ainda apresenta o conteúdo de WeaponCardContent; somente seu posicionamento mudou. Não há tutorial obrigatório, nova atividade ou alteração de combate.

[Validação VS-03.2](../validation/01_MATRIZ_DE_VALIDACAO.md#vs-032--organização-espacial-do-nexus-22102026) reúne24 capturas pareadas, comparação de condições, roteiro assistido e regressões. Uma prévia temporária de câmera52° mostrou mais contexto, mas reduziu o personagem e continuou cortando a incursão; foi revertida e não integra a cena entregue.
