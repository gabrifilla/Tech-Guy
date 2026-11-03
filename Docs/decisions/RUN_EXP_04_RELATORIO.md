# RUN-EXP-04 — consolidação entregue para revisão

02/11/2026. Executadas04.0–04.5 no projeto real `D:/Dev/Personal/My_games/Tech-Guy`, Unity6000.5.10f1 viaMCP. **STOP04.5. UI publicada preservada; C é proposta. Arte é intermediária e a avaliação física continua com o desenvolvedor.**

## Resultado concreto

Encontros mantêm o roster/ranks/quantidade e os slots seguros existentes, mas organizam pressão, retaguarda lateral ou suporte protegido por stream independente. Fragmento/Emissor receberam cópias04 com encaixes, mandíbulas, apoios e carcaças mais estruturados, peso visual/preparação/reação/desagregação; mesmas cápsulas, dano, origem e timings nativos. Fontes03 preservadas. Salas abertas e cobertas receberam pisos/trilhas e coberturas chanfradas apenas visualmente, mantendo física, duas circulações eLOS.

UI original/A/B/C comparáveis naUnity. C reduz leitura simultânea com três alternativas compactas e efeito/contexto do selecionado; sinergia depende de posse real, detalhes mostram números/restrições. Corrigidos rótulos de bindings especiais/QER e atualização após Settings, somente no presenter de revisão. Probabilidades, tickets, inventário, economia e definição dos boons intactos. Não existe integração permanente deC.

## Entregáveis e estado de cada etapa

| Etapa | Estado | Relatório |
|---|---|---|
|04.0 baseline/owners|Concluída; isolamento de moeda corrigido antes da sequência|[Baseline](RUN_EXP_04_0_BASELINE.md)|
|04.1 encontros|Implementados e testados; pressão física ainda não revisada|[Encontros](RUN_EXP_04_1_ENCOUNTERS.md)|
|04.2 inimigos|Revisão intermediária integrada/testada; arte final não implementada/aprovada|[Arte](RUN_EXP_04_2_ENEMY_ART.md)|
|04.3 salas|Apresentação implementada/testada; coesão final não revisada|[Salas](RUN_EXP_04_3_ROOMS.md)|
|04.4 UI|ProtótipoC e comparação entregues; publicação não implementada, aprovação pendente|[Recompensas](RUN_EXP_04_4_REWARD_UI.md)|
|04.5 integração|Seleções técnicas concluídas; sem verde global|[Validação completa](RUN_EXP_04_5_VALIDATION.md)|

[Abrir comparação visual portátil](../validation/artifacts/run-exp-04/review/index.html) · [ZIP com imagens e dados](../validation/artifacts/run-exp-04/RUN_EXP_04_REVIEW.zip). Inclui GameView real original/A/B/C nas três armas e dois aspectos, seleção/detalhes/contexto/rank, antes/depois dos inimigos e salas e poses/LOS. Sem assets remotos necessários. Proveniência e hashes novos no asset-manifest; links verificados no review-check.

## Como experimentar sem editar código

1. NaUnity, projetoD: real, use `Tools → Tech Guy → RUN-EXP-04 → Revisão → Próxima incursão seed63`, fora dePlay. Entre pelo portal normal doNexus, com assistência de recompensas desligada. Primeiro combate reproduz cobertura e dupla no sorteio legítimo; inimigos antigos permanecem. Para arena aberta use o menu existente `Tools → Tech Guy → RUN-EXP-03 → Próxima incursão com seed 0`. A seed não garante toda variação em todas as runs.
2. Contorne as duas massas, quebreLOS doEmissor e retorne ao tiro. Observe pressãoFragmento e priorização da retaguarda/suporte. Teste movimento/dash/básico/especial/QER com seus bindings atuais. Menus `Salas antes/com polimento` alternam apenas a aparência emPlay.
3. Use `Abrir laboratório de recompensas`, entre emPlay, alterne Original/A/B/C e as três armas. Seleção/detalhes não concedem; confirmação adquire apenas no inventário temporário doLab. Sair dePlay descarta a sessão. Para ofertas reais de run, `Síntese C temporária na run atual`; confirmar nesse contexto consome ticket legítimo. `Restaurar recompensa publicada` remove a prévia.
4. Para comparação rápida, abra oHTML: os pares de inimigos e salas usam a mesma câmera/escala/luz por comparação. Capturas de arte/custo usam imunidade de fixture declarada; encontros de dano/Hooker usamAI/dano ativos sem imunidade. Nenhuma dessas fixtures equivale a aprovação humana.

## Evidências e limites

Finais:15/15Play integrados (`cd2586...`), matriz ampliada1/1 com12cenários (`e75f95...`),33/33regressões (`75f05f...`),10/10efeitos nativos de skills (`e6b956...`),28/28Edit (`4d746f...`). IDs completos e saídas em04.5. BaselineCC intermitente3/4→isolado1/1, repetido4/4 e finalpass não teve causa inventada. Primeiro observador de ranged foi corrigido para esperarHP após viagem do projétil. Falhas intermediárias preservadas. Não se alegou suite global353 verde; dívida histórica continua.

8/32/64 ativos: quadroEditorp95 8,24/12,44/18,39ms. AI/nav/dano/poses ligados;GC e drawcalls incluemEditor/testes, sem orçamento standalone. Custo/articulação e fontes brutas em04.5; nenhum ganho comparativo fabricado. Blender indisponível: autoriaC#Unity, sem rig/atlas/LOD final alegados.

1562/1574 hashes atuais iguais,12alterações previstas,0removidos no escopo. Cenas/metas e32preferênciasTechGuy iguais ao início04; não restaurado estado histórico03. Checker mantém erroPlayground e avisoSampler históricos; Console final0erros. EditorNexusBlockout limpo/Edit, assistênciafalse, sem overrides. Branch/HEAD e mudanças anteriores preservados. Não feitos commit, push ou desligamento nesta etapa.

Skills/owners/arquivos, confirmaçãoMCP, GUIDs, isolamentos de teste e limites de save/performance estão documentados em cada relatório e consolidados04.5. Quatro skills locaisTechGuy e especializadasUnity/arte/UI/encontros/validação orientaram as mudanças; nenhuma ferramentaDCC não conectada é apresentada como usada.

## Gates e decisões pendentes

| Gate | Resultado |
|---|---|
|A combate|Contratos e contrajogo técnico verificados; pressão/diversão aguardam teste humano|
|B identidade|Geometria/articulação diferenciam os dois candidatos; arte intermediária, revisão humana pendente|
|C salas|Cobertura/LOS/nav/duas rotas preservadas nos testes; revisão física pendente|
|D coesão|Comparação concreta entregue; decisão artística humana pendente|
|E recompensa|Original/A/B/C disponíveis; direção e publicação pendentes de aprovação explícita|
|F estabilidade|Nenhum novo bloqueio produtivo confirmado no recorte; intermitência/dívida histórica abertas|
|G integridade|Baseline atual protegido no escopo enumerado; sem garantia universal de todos os saves|

Decisões concretas: escolher a direçãoUI (manter atual, A, B ouC); avaliar se as proporções/articulação04 devem orientar a próxima autoriaDCC; avaliar pressão/recuperação e leitura de telegraphs nas duas salas. Nenhuma decisão é presumida pelo resultado automático. Não iniciarRUN-EXP-05, UI permanente, novosboons/bosses ou tuning geral sem próximo pedido.
