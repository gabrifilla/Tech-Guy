# Checklist de playtest — build jogável

**Sessão:** ___ | **Commit:** ___ | **Arma:** ___ | **Build/Unity:** ___ | **Observador:** ___

## Antes

- [ ] Cena/build e resolução registradas, baseline e erros conhecidos separados.
- [ ] Seed/encontro e configuração reproduzível conhecidos.
- [ ] Sem boons (primeira sessão de arma) ou lista precisa de boons para sessão transformada.

## Durante

- [ ] Input executa o que jogador espera; ataques não saem sozinhos.
- [ ] Compromisso/cancel/evade tem janelas perceptíveis; nenhuma cobrança dupla.
- [ ] Hit tem peso e reação adequada a comum/elite/boss; stun/air juggle não indiscriminados.
- [ ] Telegraph é visível mesmo com efeitos, e reação possível sem adivinhação.
- [ ] Vida, recursos e cooldowns são reconhecíveis sob pressão.
- [ ] Boon alterou jogadas/risco/geometria e não apenas numerais.
- [ ] Não há leaks/stutter/desync óbvio ao concluir encontro/morrer.
- [ ] Fim da run exibe dados consistentes com os eventos capturados.

## Após

1. Melhor momento da sessão e motivo: ___
2. Problema de maior impacto (inclua timestamp): ___
3. Segundo problema prioritário: ___
4. Capturas/vídeo/logs: ___
5. Repetição confiável? ___
6. Resultado: `APROVADO`, `REFAZER`, `PENDENTE`, com justificativa: ___

**Nunca preencher automaticamente campos com 'passou' se ninguém executou a sessão.**

## GI1.9 — encontros mistos, sessão física de 8–12 minutos

**Estado: FEEDBACK HUMANO RECEBIDO; SEM ACEITE FINAL DO CONJUNTO.** Em25/10/2026, o desenvolvedor primeiro escolheu jogar depois e posteriormente respondeu às oito perguntas. Há avaliação humana direta, mas seed/nós/quantidade de encontros, dispositivos e boons específicos não foram informados; não houve coleta Codex dessa sessão. Não certificar automaticamente todos os cenários A–D ou três encontros consecutivos. O comentário positivo da GI1.5 sobre os golpes não aprova os inimigos após GI1.6–1.8.

**Entrada:** iniciar pelo Nexus atual, equipar Manopla se necessário e entrar em FirstSector pelo fluxo normal, usando mouse/teclado físicos e Game View focada. Conferir o HUD/Controles: leitura atual esquerdo=básico, direito=mover, Shift+básico parado, Q/W/E, Espaço=dash, **F11=Asura**, F=finalização contextual. Respeitar os bindings reais da sessão e disponibilidade de recursos.

**Percurso disponível:** início0 → C1/1 → C2/2 → escolha RP/3 ou C3/4 → C4/5; depois há outra escolha de ramo. Jogar pelo menos três encontros da rota que escolher, registrando os motivos das escolhas; boss/Elite não são obrigação deste roteiro. O snapshot da definição foi validado em leitura MCP, sem percorrer salas. FirstSector usa grupos/layout por seed: não tratar os spawns fixos do asset ou a captura seed1701 como composição garantida da próxima run.

| Cenário | Tentativa física | Registrar |
|---|---|---|
|A — pressão melee|No primeiro grupo com vários melees comuns, observe a aproximação e lute alternando básicos, deslocamento e dash.|Quais chegaram, quais atacaram, se mudaram sua posição e se retomaram a ofensiva após seus golpes. Distinguir Grunt comum de Heavy/Charger e outros papéis.|
|B — melee+ranged|Num grupo misto alcançado naturalmente, priorize alvos como preferir; se houver Sniper, tente sair lateralmente da linha anunciada.|Qual ameaça motivou a decisão, aviso visto antes do dano, eventual sobreposição/confusão, bloqueio ou oscilação. Sniper ausente: marcar NÃO ENCONTRADO.|
|C — sua pressão ofensiva|Durante A/B, use básicos repetidos, Q/W/E, dash e movimento após skills. Experimente modificadores somente se adquiridos normalmente.|Reações, retomada do inimigo, interrupção versus ataque comprometido, visibilidade do alvo, transições e controle. Não exigir que o jogador conheça o gate técnico antes de responder.|
|D — sequência prolongada|Complete ou tente três encontros ao longo da mesma run, passando por escolhas/recompensas normalmente.|Início/fim das lutas, ritmo, períodos sem ameaça, recuperação e utilidade dos espaços. Morte ou desistência são resultados válidos; registrar o trecho efetivamente alcançado.|

Se o grupo necessário não aparecer, marcar NÃO ENCONTRADO e prosseguir pelo percurso escolhido. Os cenários podem compartilhar encontros. Não conceder boon, forçar vitória ou preparar um grupo artificial para preencher os campos.

**Após cada cenário, responder com palavras próprias:**

1. Inimigo mais interessante e motivo: ___
2. Menos interessante/ameaçador e motivo: ___
3. Entendeu os ataques antes de sofrer dano? Qual exceção? ___
4. Conseguiu perceber um ataque chegando e escapar de propósito, andando ou usando dash? Ou os ataques acertaram/erraram sem você entender por quê? Exemplo: ___
5. O posicionamento dos inimigos criou uma decisão útil? ___
6. Algum inimigo pareceu incorreto ou ineficaz? O que fez? ___
7. A sala ajudou ou prejudicou a luta? Onde? ___
8. Momento mais frustrante: o que tentou, esperava e aconteceu? ___

**Registro mínimo por cenário/incidente:** A/B/C/D ___; seed/nó/sala ___; inimigos realmente presentes ___; boons adquiridos ___; ação→esperado→ocorrido ___; tempo relativo do vídeo/captura ___; repetiu? ___; avaliação humana ___; evidência técnica posterior ___; estado JOGADO / PARCIAL / NÃO ENCONTRADO / NÃO EXECUTADO ___. Não converter hipótese em causa confirmada nem preencher automaticamente as respostas.

**Coleta opcional:** no máximo dois trechos representativos de20 s, um de A/C e outro de B/C, usando o pipeline existente. O cenárioD continua sendo avaliado pela sequência jogada; esses clipes não comprovam sua duração completa. Com FirstSector já em Play, executar na raiz D:/Dev/Personal/My_games/Tech-Guy e voltar à Game View:

```powershell
python Tools/GameplayObservation/observe_gameplay.py run --request Tools/GameplayObservation/gi19-human-current.json
python Tools/GameplayObservation/observe_gameplay.py status
```

O preset current usa agenda vazia e não dirige comandos, altera seed/arma/stats ou impõe FPS/timeScale. Temporariamente configura a Game View em1280×720 e restaura sua configuração ao fechar o coletor; PNG a cada60 frames, JSONL por frame. O rótulo GAUNTLET_SMALL_GROUP não cria nem garante cinco inimigos: o manifesto registra seed/nó/roster reais. Fazer um trecho sem coletor para comparar eventual custo de captura; usar vídeo habitual se disponível, pois PNG espaçado não demonstra todo contato. Somente armar a coleta quando a pessoa estiver pronta. Cancelar o coletor com `observe_gameplay.py cancel` não significa encerrar a run.

**Leitura posterior:** Grunt: aproximação/distância → percepção de elegibilidade → windup → emissão → HP confirmado → recuperação. O flag de elegibilidade pode estar antigo enquanto a rotina está ocupada; não confundi-lo com novo aceite. Sniper: geometria do aviso/compromisso → direção configurada e deslocamento do projétil → HP ou esquiva; FirstObserved é amostragem, não callback universal. Reações/postura/controle e mortes/fases do encontro contextualizam a pressão; HP vem somente de ActorDamageReceived, sem somar hooks. Logs não substituem as oito respostas humanas.

**Preservação e decisão:** registrar os dados vigentes antes da sessão futura; o snapshot da preparação de hoje não deve sobrescrever progresso humano posterior. Preservar ganhos, escolhas e resultado da run. Encerrar o roteiro quando desejar, sem o observador interromper Play Mode/recompensas. Depois, escolher até três problemas pelo impacto relatado e pela evidência, separando origem confirmada de hipótese (IA/nav/ataque/apresentação/mapa/composição). Uma correção pequena exige reprodução e regressão; balanceamento, composição, mapas, armas e boons exigem decisão autoral. Resultado humano final: APROVADO / CORREÇÃO PONTUAL / REPETIR / PENDENTE ___; próxima prioridade e motivo ___ .

### Respostas humanas recebidas — GI1.9

| Pergunta | Relato do desenvolvedor |
|---|---|
|1 — mais interessante|Spawner: manter a luta pela produção de inimigos foi interessante. Propõe reviver mortos em vez de invocar do nada. Gostou também dos ranged, exceto alguns efeitos que surgem sem dano/efeito aparente.|
|2 — menos interessante|Inimigos básicos: preparação percebida como longa e alcance de atração insuficiente para fazê-los parecer ameaçadores.|
|3 — compreensão|A maioria dos ataques fez sentido. Exceção: ataque de ranged vindo de cima, sem dano aparente.|
|4 — resposta intencional|Após reformulação, confirmou que conseguia perceber e evitar intencionalmente a maioria dos golpes; algumas exceções não foram identificadas.|
|5 — posicionamento|Não percebeu decisões úteis; inimigos pareciam espalhados aleatoriamente. Disse que isso, por si só, não era ruim.|
|6 — comportamento incorreto|Remete às observações2/3: básicos pouco ameaçadores e efeito ranged sem consequência aparente.|
|7 — sala|Considerou precária e sem identidade; conexões entre salas grandes sem motivo percebido.|
|8 — frustração|Boons percebidos como pouco transformadores. Rolamento usado para viajar em vez de caminhar por parecer mais rápido e sem sentido no conjunto.|

**Esclarecimento posterior:** disse que todos os inimigos têm a mesma aparência e por isso não sabe de quem vinha o ataque aéreo. Afirmou permanecer dentro da área até o impacto sem perder vida. Isso é evidência humana de um incidente, ainda sem executor/seed/timecode ou reprodução técnica; não atribuir automaticamente ao Bomber, HazardCaster ou Sniper. A aparência pouco diferenciada impede reconhecer a origem/papel da ameaça.

**Interpretação:** conservar o interesse pelo spawner e pelos ranged compreensíveis, além da capacidade relatada de evitar a maioria dos ataques. Ressurreição é proposta de design, não funcionalidade atual nem mudança aprovada. Pressão dos básicos, consequência/identificação dos efeitos ranged e ritmo/identidade espacial são os três problemas de encontro apontados pelo relato; suas causas técnicas ainda não estão reproduzidas. O ataque aéreo merece investigação prioritária porque o jogador relata ficar na área sem dano. Boons e incentivo ao dash são frustrações transversais de prioridade alta, sem autorização automática para rework, nerf ou alteração de kits. Nenhuma mudança mecânica foi implementada a partir dessas respostas.

## GI1.5 — resposta da Manopla, sessão física de 8–12 minutos

**Estado: SESSÃO FÍSICA REALIZADA EM25/10; feedback geral recebido.** O desenvolvedor afirmou que o jogo e a sensação dos golpes estão legais; o incômodo principal está nos inimigos, na interação dos golpes com eles, no mapa e nos boons. Isso é avaliação humana positiva do conjunto de golpes, não confirmação individual das oito tarefas. Duas coletas passivas reais de30 s em FirstSector estão na [matriz](01_MATRIZ_DE_VALIDACAO.md); não houve vídeo contínuo ou comparação formal com/sem coletor. Asura Cadenciada foi encontrada adquirida naturalmente, mas sua ativação não foi registrada nesses clipes. Campos/tarefas não comprovados continuam em aberto. Data/jogador ___; cena/build ___; vídeo/timecode ___; seed/nó ___; arma/boons ___; resolução/FPS aproximado ___; sensação com/sem coletor ___.

**Preparação:** abrir o projeto correto, usar o Nexus atualmente aberto e entrar em FirstSector pelo fluxo normal com a Manopla. Começar sem boons, câmera atual, valores de produção; não usar seeded_combat, spawn/dano/energia injetados ou dispositivos virtuais para esta sessão. Conferir HUD/Controles: bindings atuais observados em25/10 são esquerdo=Primary, direito=Move, Shift+básico parado, Q/W/E, Espaço=dash; Asura está em **F11**, não R; F é finalização contextual. Preferências podem mudar: o HUD/Controles da sessão prevalece. Manter foco na Game View, aguardar mana/cooldown entre tentativas e não equiparar indisponibilidade a input perdido.

| Tentativa física, repetir até três vezes se houver dúvida | Registrar, sem orientar a resposta |
|---|---|
|1. Faça cinco básicos consecutivos: primeiro cinco cliques deliberados, depois segure o ataque parado com Shift. | Há resposta ao iniciar? Cada contato parece um soco? Consegue parar ao soltar? Os0,20 s parecem convincentes ou apressados? Se o alvo morrer, marcar série incompleta, sem aumentar HP. |
|2. Clique num inimigo que se move; aproxime, ataque e mude de posição. | Perseguição esperada, mudança de alvo, soco no vazio, distância/punho versus HP. Não interpretar todo whiff como perda de comando. |
|3. Faça básico→Q, básico→W e básico→E, em tentativas separadas. | A skill inicia, substitui o básico antes de contato ou sucede um hit já confirmado? Há pose sem efeito ou efeito sem pose? |
|4. Use Q/W/E; durante a recuperação, clique no chão e depois teste Espaço, separadamente. | Movimento que aguarda o fim, dash que dispara agora/depois ou é recusado; trajetória inesperada e retorno ao controle. |
|5. Durante o retorno dos braços de Q, pressione E uma única vez cedo e solte. Não repita imediatamente. | Mensagem de rejeição? E aparece sozinha depois? Confirmar a fase real no JSONL; não presumir que o gesto caiu em recovery. |
|6. Repita Q→E perto do fim da recuperação e solte; faça uma nova tentativa já em repouso. | Aceitação perto da janela e fora do cast; diferença perceptível da tentativa5. Comparar com mana/cooldown disponíveis. |
|7. Entre num grupo pequeno real e permita ameaças simultâneas antes de reagir com básicos, movimento e dash. | Distingue seu impacto da ameaça? Hitstop ajuda ou atrapalha o próximo comando? Reação/retomada dos inimigos legíveis? Não exige receber dano de propósito. |
|8. Se adquirir naturalmente uma transformação, registre seu ID/card e repita2–6 onde for seguro. | Qual ação/decisão mudou? Cadenciada só se realmente adquirida; se não surgir, marcar NÃO EXECUTADO, sem conceder boon. |

**Por tentativa:** `aceito imediato / rejeitado com motivo / aceito após espera / aparentemente ignorado / hit sem conexão visual / visual sem resultado / não executado`. Anotar esperado, ocorrido, timecode e necessidade de repetir. Primeiro avaliar espontaneamente; consultar os logs depois, para não substituir sensação por expectativa técnica.

**Coleta opcional reutilizando GameplayObservation:** já em Play Mode, em FirstSector com jogador vivo, executar na raiz do projeto e voltar à Game View:

```powershell
python Tools/GameplayObservation/observe_gameplay.py run --request Tools/GameplayObservation/gi15-human-current.json
python Tools/GameplayObservation/observe_gameplay.py status
```

O modo `current` tem agenda vazia: não dirige input, altera seed/arma, suspende a run ou encerra Play Mode. O rótulo do cenário não cria cinco inimigos; registrar o encontro real. O preset usa `observe_seconds=30`, trinta segundos reais sem impor FPS/timeScale; o limite por frames continua como padrão dos presets antigos. JSONL é amostrado a cada frame; PNG a cada60 para reduzir custo. Para motion, gravar vídeo com a ferramenta habitual e marcar o início da coleta pela primeira imagem/timecode. Os PNGs esparsos não demonstram todo contato. Repetir clipes curtos para tarefas diferentes. `python Tools/GameplayObservation/observe_gameplay.py cancel` encerra cedo, preservando evidência parcial identificada; não interromper a sessão inteira por isso.

Comparar pelo menos uma sequência com e sem coletor: readback/JSONL podem afetar desempenho e hitstop. Evitar pausa/menu durante o trecho principal; testar pausa separadamente. Sessão física usa progressão normal e pode gravar moedas/resultados: usar o perfil habitual conscientemente ou backup controlado antes de iniciar; não restaurar um snapshot antigo por cima de progresso humano novo.

**Leitura posterior:** correlacionar `absolute_frame` de InputEdgeObserved com AbilityUsed/AbilityRejected/BasicAttackPerformed, SkillSnapshot e PlayerSnapshot; HP vem exclusivamente de ActorDamageReceived/recibo em HpContactTiming. Callbacks podem anteceder a amostra de input no mesmo frame. Recovery começa/termina em SkillStateChanged e snapshots, com precisão de amostragem; elapsed/duration é progresso global, phase_progress é local à fase. NavigationSnapshot mostra o agente, não o aceite do clique. Não há recibo universal de rejeição de básico ou cancelamento: se faltam sinais, manter INCONCLUSIVO e usar vídeo/foco/estado, sem inventar causa.

**Contrato para interpretar5–6:** Q permite Skill em0,78–1, W em0,92–1 e E em0,82–1 do progresso global autorado; cooldown, mana, UI e dash ainda podem negar. Skill comum fora da janela é rejeitada e não fica numa fila geral; uma nova pressão é necessária. Dash tem buffer próprio e Cadenciada tem continuação própria. Movimento na recuperação também possui espera própria, distinta de skills.

**Incidente:** tarefa/timecode ___; tecla/binding/foco ___; esperado→ocorrido ___; input observado ___; aceitação/rejeição e fase ___; recibo de HP/execução ___; efeito visual ___; repetiu? ___; humano ou automação? ___. Resultado humano final: APROVADO / CORREÇÃO PONTUAL / REPETIR / PENDENTE ___; maior problema e motivo ___; próxima ação ___ .

## VS-03.3 — sessão humana do Nexus

**Estado: FEEDBACK AUTORAL RECEBIDO — experiência atual requer revisão.** O desenvolvedor relatou concentração excessiva de objetos/cards, armas grandes e provisórias, ausência de salas com funções distintas e pouca conexão com a busca pela Terra. Não há registro de execução das oito tarefas abaixo; seus campos continuam sem preenchimento automático. O checklist de combate acima não é condição para aprovar o hub.

**Data/jogador/observador:** ___ | **Primeira visita ou retorno:** ___ | **Build/cena/resolução:** ___
**Entrada:** teclado/mouse reais ___ | **Armas desbloqueadas:** ___ | **Perfil de teste/backup e responsável pela restauração:** ___

**Preparação do observador:** usar NexusLobby atual, câmera40°, sem modificar estações ou conceder recursos. Registrar os desbloqueios existentes; arma bloqueada não significa falha de interação. Usar perfil de teste com os dados necessários ou preservar/restaurar o perfil compartilhado. Não orientar localização/comandos antes da tentativa; registrar qualquer ajuda. Não exigir vitória/boss para avaliar retorno. Sem alterações de save ou configuração durante a tentativa, salvo as ações normais da sessão.

| Tarefa para a pessoa jogar | O que observar | Resultado / hesitação / ajuda |
|---|---|---|
|1. Explore e diga onde está e o que acha que pode fazer. Encontre onde trocar de arma. | Reconhecimento do hub/arsenal; primeira direção escolhida | ___ |
|2. Equipe Manopla, Arco e Lança disponíveis, em uma ordem escolhida por você. | Entendimento do prompt; ação inesperada; custo/desbloqueio distinguível | ___ |
|3. Afaste-se e mostre qual arma está equipada. | Que sinal usou: modelo, placa, guia ou card; leitura extensa necessária? | ___ |
|4. Encontre onde experimentar habilidades e faça um teste. | Descoberta do treino; HUD contextual e leitura do personagem | ___ |
|5. Abra uma consulta/painel e tente interagir com o mundo; feche e continue. | Ações ocultas, foco, soltura de teclas e movimento após fechar | ___ |
|6. Encontre como iniciar uma incursão; entre e encerre por abandono quando desejar. | Descoberta da saída; pausa/confirmação; retorno compreendido | ___ |
|7. Consulte o resultado, explique o que aconteceu, feche e encontre como consultá-lo de novo. | Clareza do resultado e acesso manual; sem confundir arma inicial com equipada agora | ___ |
|8. Prepare outra incursão: mova-se, troque de arma e localize novamente a saída. | Retenção dos controles e orientação na visita repetida | ___ |

**Por linha:** marcar sem ajuda / com ajuda / não concluiu / não tentou; anotar tempo/hesitação somente se observado, sem limite universal inventado.

### Registro de incidente (copiar apenas quando ocorrer)

**Tarefa e momento:** ___ | **Ação real / esperado / ocorrido:** ___
**Dificuldade ou fala literal da pessoa:** ___ | **Legibilidade/oclusão:** ___
**Origem da evidência:** humano ___ / teste técnico ___ | **Ajuda dada:** ___
**Reprodução no estado atual e evidência (se houver):** ___ | **Impacto:** bloqueia tarefa / recuperável / preferência visual

### Decisão após jogar

**Avaliação humana realizada por:** ___ | **Resultado:** encerrar VS-03 / abrir correção pontual / repetir tarefa inconclusiva
**Motivo e tarefas não concluídas:** ___ | **Próxima ação e responsável:** ___

Recomendar encerramento quando as tarefas essenciais forem compreendidas e concluídas sem bloqueio não resolvido. Abrir correção pontual somente para bug reproduzido ou pequeno problema demonstrado; câmera/layout maior exigem proposta comparável e decisão autoral. Acabamento artístico provisório, por si só, não mantém esta fase aberta. Testes PlayMode/input virtual não preenchem este aceite humano.

**Decisão atual, após o comentário do desenvolvedor em24/10:** não encerrar o aceite de experiência do Nexus. O relato envolve organização e sentido das interações, além de acabamento. Revisar a [proposta espacial/narrativa](../art/03_NEXUS_PROPOSTA_DE_EXPERIENCIA.md) antes de construir outro layout. Nenhuma dificuldade por tarefa, tempo ou ação específica foi inventada a partir desse comentário geral.
