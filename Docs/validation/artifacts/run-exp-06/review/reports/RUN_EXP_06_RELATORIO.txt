# RUN-EXP-06 — integração produtiva concluída e próxima direção preparada

03/11/2026. C05 agora é a interface normal dos tickets de recompensa de FirstSector, sem laboratório, menu de desenvolvimento ou seed especial. Fragmento/Emissor e ambiente05 já estavam integrados; foram consolidados e verificados sem duplicar assets, spawn ou arquétipos. A aprovação01–05/C05 foi recebida expressamente no pedido06. O acabamento de arte ainda tem limitações descritas abaixo.

O planejamento2.5D está pronto em [06.5](RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION.md), com três linguagens originais exploratórias e contratos comuns de leitura/física. O [brief07](RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md) compara uma fachada e uma cobertura3D/híbrida na mesma câmera/sala, exige revisão das imagens antes da conversão e **não autoriza começar07**. Nenhum mapa foi convertido, renderer/worldmanager criado ou universo publicado06.

## Entrega para revisar

[Revisão visual portátilHTML](../validation/artifacts/run-exp-06/review/index.html) · [ZIP](../validation/artifacts/run-exp-06/RUN_EXP_06_REVIEW.zip) · [24 ilustrações dedicadas ainda faltantes](../validation/artifacts/run-exp-06/reward-illustrations-missing.md) · [Catálogo32 IDsCSV](../validation/artifacts/run-exp-06/reward-catalogue.csv).

O pacote distingue fluxo produtivo, comparadores assistidos e medições; contém capturas atuais nas três armas, fallback, detalhes, contextos/rank, salas/inimigos/LOS e aplicativo real. Arte aprovada não foi remodelada para fabricar uma reintegração. Cinco scripts de apresentação foram promovidos porAssetDatabase mantendoGUIDs; RunRewardUI continuaowner e RunBoons/ticket continuamautoridades. Detalhes/pausa/foco/cancelamento/reabertura/remapeamento e confirmação exata foram exercitados. A revisão visual detectou e corrigiu popups sobre a tela; não houve tuning de combate/RNG/economia.

| Seleção | Job | Resultado | Duração |
|---|---|---|---|
| FirstSector + UI produtiva e catálogo | `d775c4fd01864708999ab03077facbb4` | 27/27, falhas0, skipped0 | 286.705s |
| Regressões de combate/input/boons | `ae7e05f0965342e4b9cb3eb2a4dc703a` | 33/33, falhas0, skipped0 | 43.775s |
| HP/atribuição e efeitos de habilidades | `258b26f015304b1699935dc316556811` | 10/10, falhas0, skipped0 | 11.171s |
| Básicos/combos e câmera/oclusão | `8901f2dd89e743258749252a41d40513` | 29/29, falhas0, skipped0 | 3.373s |
| Conteúdo/geometria/encontros/arte | `111fc22c5f2845729644ab2edfe1c47f` | 29/29, falhas0, skipped0 | 1.598s |


São seleções reais desta execução, sem alegação de suíte global verde. [06.4](RUN_EXP_06_4_VALIDATION.md) registra jobs intermediários/falhas/correções, limites e números brutos. MouseWindows selecionou/incorporou uma oferta legal emFirstSector e preservou o próximo ticket ao fechar/reabrir. Os três percursosNexus→boss→Nexus são automatizados, com imunidade/warp/conclusãoletal declaradas; uma run sem assistência também usou seed aleatória. Não são um playthrough humano de sobrevivência.

## Como reproduzir com segurança

1. Abrir `D:/Dev/Personal/My_games/Tech-Guy` na Unity6000.5.10f1. Salvar trabalho próprio antes de trocar cena; não abrir uma segundaUnity no mesmo projeto. Cena normal inicialNexusBlockout.
2. Usar o arsenal e os loadouts já disponíveis na sua progressão. A cobertura automatizadaArco/Lança usa acesso temporário restaurado; não modifica seus desbloqueios para um playtest manual.
3. Manter assistênciaTitanicConductor desligada e limpar apenas um override de seed que você mesmo tenha configurado. Para reprodução determinística, usar o campo de seed existente com0Manopla/2Arco/1Lança e restaurá-lo ao final; não é requisito para C05 abrir.
4. Entrar emPlay, aproximar-se da estaçãoFirstSector noNexus e usar o binding de interação mostrado pelo jogo. Não esperar entrada automática por umScenePortal inexistente nessa estação.
5. Completar um encontro normal. No ticket real, testar selecionar com mouse/setas/direcional, abrir detalhes e voltar, fechar mantendo pendente e reabrir, conferir categoria/ordinal, incorporar e verificar a build pela pausa. Não usar mostruário de arte como prova de RNG. Seguir salas abertas/cobertas até boss, extração eNexus.
6. Emcobertura, contornar pelos dois lados, observarEmissor perder/readquirirLOS, Fragmento navegar e projéteis/dash respeitarem sólidos. Exercitar básicos, especial eQ/E/R com os prompts/bindings atuais. Preservar seus saves e sair dePlay ao terminar.
7. Para repetir a seleção automatizada, usar TestRunner com as classes/filtros de `validation-selections-complete.json`, ou `python Tools/RunExp06/run_validation.py` com a ponteMCP existente. Não executar junto de um playtest manual. As fixturesdeclaram suas manipulações temporárias e restauram os escopos atuais; o script não restaura saves históricos.

## Custo e integridade

8/32/64 ativos medidos noEditor contra baseline06 e emstandalone atualWindowsrelease1920×1080, AI/nav/dano/poses ativos. O harness05 foi reutilizado sem duplicação, emproduto de perfil isolado. CPU/GPU/memória e indisponibilidades estão no06.4; diferenças entre instantes de AI não demonstram otimização. Sem orçamento universal ou alegação de VRAM medida.

2739hashes comparados, cincoCS/metas movidos comGUIDs preservados; cenas/prefabs/arte/definições/input/settings/fontfallback/fontesDCC iguais.47 preferências existentes de gameplay iguais,0novas/alteradas/removidas nesses escopos. Nada foi restaurado de snapshots04/05. O checker mantém um erro/umaviso históricos fora das cenas produtivas; Console final0erros semlimpeza manual. EditorNexusBlockout limpa/Edit/Time1, sem assistência, seed ou scopes pendentes.

Limitações mantidas:24IDs sem ilustração dedicada, cobertos por marca de família; uma imagem autorada fora do pool é preservada. Rig/clipsDCC eLOD finais não foram feitos. Outras máquinas, custo isolado deVFX/modal e técnica2.5D no jogo não foram testados. Aceite05 não é declaração de arte finalAAA; integração06 fica disponível para sua revisão, sem inventar aceite humano novo.

## Documentos e encerramento

[06.0Baseline](RUN_EXP_06_0_BASELINE.md) · [06.1UI](RUN_EXP_06_1_REWARD_UI_PRODUCTION.md) · [06.2Inimigos](RUN_EXP_06_2_ENEMIES_PRODUCTION.md) · [06.3Ambiente](RUN_EXP_06_3_ENVIRONMENT_PRODUCTION.md) · [06.4Validação](RUN_EXP_06_4_VALIDATION.md) · [06.5Direção](RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION.md) · [Brief07](RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md).

Masterplan, design técnico, UI, arte/pipeline, matriz, ledger, estado, requisitos e tarefas receberam estado06 acima do histórico. A instrução posterior do usuário autoriza commit e push **de todas as alterações**, inclusive as anteriores; desligamento somente depois de push confirmado e trabalho salvo, semforçar perda de dados. Ver `RUN_EXP_06_DELIVERY.md` para o registroGit final. STOP06: implementação07 não iniciada.
