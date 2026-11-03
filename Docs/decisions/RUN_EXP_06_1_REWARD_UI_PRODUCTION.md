# RUN-EXP-06.1 — Síntese C05 no fluxo normal de recompensas

03/11/2026. O pedido06 aprova expressamente C05 e a apresentação01–05. A publicação usa a implementação compartilhada já existente, promovida para o domínio de recompensas; não há uma segunda cópia da interface nem dependência de laboratório para abrir tickets normais.

## Implementação e autoridade

`RunBoons.Awake → RunRewardUI.Bind → Update` é o caminho produtivo. O ownerRunRewardUI cria ou reutiliza uma única `RewardSelectionView`, seleciona Synthesis05 e desliga controles de revisão. O owner permanece habilitado para a página de build/pausa. A apresentação chama `RunBoons.Choose`; ProgressionDirector, sessão, política, catálogo e ticket continuam determinando elegibilidade, RNG, ordinal, resultado e aquisição. Nenhuma alteração de pool, peso, economia, boon ou seed publicada.

Cinco assets foram movidos por `AssetDatabase.MoveAsset`, preservando seus GUIDs, de UI/Review para UI/Rewards:

- RunExp03RewardView e dois partials → RewardSelectionView e partials Synthesis/Synthesis05.
- RunExp03RewardModel → RewardChoicePresentation.
- RunExp05RewardArt → RewardIllustrationCatalog.

ReferênciasC# dos testes/ferramentas foram atualizadas. Os caminhos Resources das ilustrações/glyphs foram preservados, pois são assets carregados em runtime; seu prefixo histórico não é uma dependência de laboratório. `presentation-moves.json`/`presentation-promotion.json` registram operações e GUIDs. ComparaçãoOriginal/A/B/C04 continua utilitário explícito; não há override instalado nas cenas normais. O mostruário assistido é somente leitura e não permite escapar para a confirmação legada.

## Semântica e interação

A view guarda a referência do ticket/request, sala, revisão e IDs atuais. Antes de escolher, confere a oferta atual e aceita uma confirmação por frame; a autoridade nativa continua responsável por resolver exatamente uma vez. Novo ticket/sala reinicia seleção, detalhes e scroll. A UI não mantém uma cópia mutável de inventário nem gera escolhas.

Três alternativas compactas quando existem três ofertas; menos alternativas quando o catálogo real oferece menos. Cabeçalho informa categoria anunciada, ordinal/total, quantidade real e alternativa por categoria esgotada. Isso não promete uma raridade inexistente no dado atual. Conteúdo imediato, sinergia já ativa, possibilidade futura e próximo rank continuam separados no modelo. Texto de bindings vem das preferências atuais; não há promessa de que outro boon aparecerá.

Mouse seleciona/abre detalhes/incorpora; teclado usa setas/1–3, Enter, Tab e Backspace; gamepad usa direcional, sul, oeste e leste. São controles de navegação de UI atuais, não uma nova tela de remapeamento de menus. Os bindings de habilidades permanecem remapeáveis e aparecem nos rótulos. Backspace/leste fecha detalhes primeiro ou mantém ticket pendente. Esc fecha detalhes antes da pausa; fora dos detalhes, a pausa e sua página de build continuam com PauseMenuUI. Reabrir preserva o ticket e seus IDs, sem gastar recompensa.

Perda de foco ignora input; retorno exige liberar controlesUI pressionados antes de aceitar nova entrada. Enter remapeado temporariamente para Skill1 não dispara habilidade ao confirmar, nem ao entrar no próximo encontro segurando a tecla. Soltar e pressionar novamente devolve a ação nativa. Inputs duplicados, detalhes e mostruário são ignorados sem anunciar que a oferta legal ficou indisponível. Ticket realmente obsoleto continua recusado pela validação.

Os títulos ajustam tamanho dentro do espaço e os detalhes têm scroll e título integral. Capturas reais do fluxo normal usam1160×600,1280×800 e1920×1080; a matrizUI também exercita as três armas/aspectos. A inspeção visual06 encontrou popups de dano cobrindo a tela: `GUI.depth` ordena o MonoBehaviour completo, portanto restaurá-lo ao fim anulava a profundidade de modal. A view agora mantém sua profundidade atribuída; não foi necessário alterar dano, popups ou relógios de combate. A mensagem temporária ao tentar confirmar em detalhes também foi corrigida. Evidência anterior permanece nos artefatos; o relatório06.4 indica a reexecução final.

## Cobertura integral do catálogo alcançável

Auditoria executa as mesmas ramificações nativas de armas, expansões habilitadas, mutaçõesGauntlet e quatro protocolos. Para cada candidato, a fixture isolada aplica apenas pré-requisitos aceitos pela elegibilidade nativa e verifica aquisição. Isso comprova alcançabilidade/compatibilidade; **não é uma afirmação de obter todos os boons por RNG numa única run**. A apresentação assistida das famílias é identificada como tal.

Resultado: **40 linhas por arma, 32 IDs únicos, todos com testemunho de elegibilidade; 8 IDs com arte dedicada, usando7 ilustrações; 24 IDs sem ilustração dedicada**. TemporalEcho/PunhoEcoante compartilham uma imagem coerente. A oitava imagem autorada, Piercing, corresponde a `weapon_Piercing`, fora do pool atual; foi preservada e não aplicada a uma mecânica diferente. Expansões desabilitadas ficam separadas no JSON, não foram ligadas para melhorar cobertura.

Mutation, Augment, Protocol, Technique e Resonance/Duo têm marcas coerentes de família. O fallback não representa falsamente um executor específico. Continuação por catálogo esgotado tem marca própria e texto **SEM AQUISIÇÃO / Continuar**, sem fingir recompensa nova. Não há campo de raridadeBoonDefinition atual a inventar.

Lista completa e pendências: [CSV](../validation/artifacts/run-exp-06/reward-catalogue.csv), [24 ilustrações faltantes](../validation/artifacts/run-exp-06/reward-illustrations-missing.md) e `catalogue/*/catalogue06.json`. O maior título alcançado foi Uppercut do Eclipse,19 caracteres; textos maiores de descrição/contexto são avaliados nos detalhes. Não afirmar suporte a qualquer tamanho arbitrário de conteúdo futuro.

## Evidência, skills e limites

Primeira UI após promoção: job `379af7b8185e4007b69b157603ca91c5`,7/7Play,19,432s. Primeiro conjunto produtivo+catálogo: `1d7235b8aef54cae930824552195cb79`,5/5,24,863s. Integração completa antes da correção visual: `5ed555724612428789c8e41cb2c440d5`,27/27,289,124s. Resultado final após a correção, inspeção de capturas e mouse nativo estão no [06.4](RUN_EXP_06_4_VALIDATION.md); estes números intermediários não substituem essa validação final nem somam testes únicos.

Aplicadas: skills locais de visão, boons, combate e aceitaçãoUnity/referências; UnityMCP/C#, game-ui-ux, input-systems/UnityInputSystem, create-game-assets e performance-optimization. A revisão visual usa as capturas reais; UTF não concede aceite artístico humano. O aceite01–05 foi dado pelo desenvolvedor no pedido06; não é gate ainda pendente. A aparência da integração06 permanece disponível para revisão posterior, sem fingir novo playtest humano.

Concluído: publicação no fluxo nativo, remoção de dependênciaLab, cobertura/fallback e interação no escopo documentado. Não concluído por escopo:24 ilustrações dedicadas, migração global de UI/input ou nova localização de conteúdo. Não testado: toda combinação cartesiana de boon/arma/ator, outros hardwares e custo isolado de cada ilustração/UI no GPU. Nenhuma dessas lacunas foi ocultada como arte final ou suíte global verde.
