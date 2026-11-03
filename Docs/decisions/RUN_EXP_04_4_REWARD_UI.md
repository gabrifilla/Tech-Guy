# RUN-EXP-04.4 — evolução de UX em preview, sem publicação

Escopo: comparar original/A/B e uma direção refinada na Unity, com ofertas/inventário reais, hierarquia limpa e escolhas legais. A/B continuam sem escolha humana; C é recomendação de revisão, não interface publicada. RunRewardUI.cs e cenas produtivas não receberam substituição.

## Diagnóstico e proposta

Inspecionados View/Model/Lab03, presenter nativo, RunBoons/ProtocolOfferPresentation/catálogos e bindingsGamePreferences/C01. A enfatiza três artefatos/efeitos simultâneos, boa comparação mas leitura distribuída; B enfatiza um efeito com trilho lateral, porém ainda lembra ficha técnica. C — Síntese mantém três nomes compactos numa faixa comparável e dá espaço a uma transformação selecionada, com símbolo, frase comportamental e contexto realmente adquirido. Seleção clara de alto contraste, fundo opaco e menos texto simultâneo. Não marca melhor escolha, não promoveTitanic e não altera pesos/RNG.

Hierarquia: nome/família, efeito selecionado, sinergia só quando possuída; restrições essenciais sempre no rodapé, números/escala nos detalhes. Glifos geométricos originais03 por família reaproveitados; não são ilustrações finais únicas por boon. Materiais/iluminação gameplay permanecem separados doUI. C merece revisão por reduzir a leitura inicial sem esconder nomes alternativos; não é aprovação autoral.

## Código e correções concretas

RunExp03RewardView tornou-separtial com layoutSynthesis em arquivo próprio. A/B mantidos. Um único fluxo compartilha revisão/IDs/Choose/confirm único/modal. RunExp04ReviewMenus abre laboratório existente, aplicaC temporariamente a uma run ou restaura presenter. Lab apenas ajustou espaço dos controles para não sobrepor os três botões. Nenhum novo sistema de recompensa/inventário.

RunExp03RewardModel corrige escopo legadoR: no controle direto atual é especialRMB/bindingGameControl.Move, nãoSkill4/F11. Q/W/E internos continuam traduzidos para os bindings reais Q/E/R ou personalizados. Labels em detalhes também traduzidos; Basic→AtaqueBásico e famíliaDuo localizada. Definições/IDs/executores não foram alterados. View reconstrói labels ao voltar da pausa, evitando binding antigo após Settings. Não consulta/regrava preferências para normalizá-las.

Atualizados testes de recompensa existentes para capturar todos os layouts com mesmas ofertas em cada comparação. Rank/contexto têm fixtures assistidas explícitas; lista comparativa não prova RNG ou elegibilidade natural. Caso visual de escopoQ é somente leitura/ineligível para commit, nunca ofertado por produção. Teste de pausa/rebindF9 verifica o texto efetivamente exibido ao retornar e restaura a chave original.

## Unity, evidência e interação

UnityMCP confirmou projetoD:/Dev/Personal/My_games/Tech-Guy6000.5.10f1. Job`e3c509a3e18d4e57b3a4a39d60d78cd9`:4/4 PlayMode,7,127s. Após labels de retorno/Basic, job`7ed0275de84c491f9cbbeed4b050002a`:4/4,5,542s. Sem vermelhoUI nesta fase. Capturas original/A/B/C, seleção/detalhes, sem parceiro, Titanic possuído e evoluçãoProtocol1→2 nas três armas,1160×600/1280×800. Imagens foram abertas/inspecionadas; layouts e estados distinguíveis sem prometer aprovação humana.

Mouse nativo pela skillcomputer-use, janelaUnity observada: mudar seleção/abrir detalhes/clicar sob modal manteveacquired0; confirmarGeada produziuacquired1 echoicesvazias. Logs`mouse-c-*.json`. Sem diretor/save no laboratório. Teclado/gamepad virtuaisC, pausa/modal, previewsemcommit, oferta vazia, duplicação e rebind foram exercitados noUTF. NavUI conserva convenções próprias de setas/dpad/Enter/sul/Tab/oeste; gameplay permanece rebindável, sem nova quarta skill.

Pacote portátil: `../validation/artifacts/run-exp-04/review/index.html` e`RUN_EXP_04_REVIEW.zip`. BuilderTools/RunExp04/build_review.py copiaPNGs reais, valida todoshref/src locais e incluiJSONs de testes. Primeira montagem45imagens/10jobs/0linksfaltantes; será regenerada após04.5. Não há links remotos nem assets de imagem ausentes. PreviewUnity:Tools→TechGuy→RUN-EXP-04→Revisão→Abrir laboratório, Play, Original/A/B/C. Em run real, confirmação consome ticket real; noLab descarta ao sair dePlay.

## Skills, integridade e limites

AplicadasTechGuyvision/boon-design/acceptance, game-ui-ux, create-game-assets, UnityMCP/C# ecomputer-use para mouse. Decisões: hierarquia progressiva, contraste de seleção, propriedade real da sinergia, um único commit e texto coerente comC01. Não houveFigma/imagemexterna/Blender alegado. Ícones03 e suas proveniências preservados; nenhum efeito novo deboon.

Console0 erros; referências/GUIDs de produçãoUI preservados.32preferências verificadas após isolamento04, conferência final em04.5; testes restauram rebind temporário. PerformanceUI não é aprovação de orçamento; renderers/gameplay8/32/64 medidos na próxima etapa. Detalhes ainda contêm terminologia legada comoChill/Heavy; localização integral, leitor de tela e ilustrações por boon ficam para direção aprovada. **Gate de pacote técnico cumprido; escolha visual humana pendente. Não publicarC automaticamente.**

## Fechamento da sequência04

Pacote final regenerado eZIP verificado:141 imagens incluídas,17 registros de jobs com intermediários preservados,0 links faltantes. Imagens adicionais cobrem progressão/Hookers e pares de preparação/ataque; o conjunto não implica141 estados distintos ou aprovação deC. Conteúdo completo abre peloHTML sem dependência de imagem externa.

Esta seção não altera os resultados intermediários acima. A integração final foi encerrada em04.5:15/15 Play integrados,1/1 matriz ampliada com12 cenários,33/33 regressões,10/10 efeitos de habilidades e28/28 Edit selecionados.1562/1574 hashes iguais e12 alterações previstas;32 preferências TechGuy atuais iguais, sem restaurar03. Fontes/cenas/metas antigas e UI publicada preservadas, Console0 erros; checker mantém dois diagnósticos históricos. Performance, GUIDs, jobs completos, limites de persistência e aprovação física estão no [relatório de validação](RUN_EXP_04_5_VALIDATION.md). [Entrega e pacote visual](RUN_EXP_04_RELATORIO.md). STOP04.5: arte final, direção de UI e julgamento físico permanecem pendentes.
