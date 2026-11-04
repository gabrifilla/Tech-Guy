# Pipeline de assets — consistência para arte gerada por IA

<!-- RUNEXP08 CURRENT -->
## RUN-EXP-08 — protótipo pictórico isolado; STOP

03/11/2026. Direção C/pictórica escolhida explicitamente no pedido08. Cópia de cena com nó 1 e seus acessos: fachada fixa 2.5D, coberturas pintadas, piso quieto e pórticos revisados; física / roster/combate/grafo intactos. Revisão final31/31; seleção anterior33/33. Custo Editor ABBA: drawcalls366→418, sem GPU/standalone; tempos não isolam arte porque AI/população evoluem.2368 arquivos antigos e47 prefs idênticos;25 arquivos novosUnity. Beleza/fluxo percebido **PENDING DEVELOPER APPROVAL**; sem conversão produtiva ou conteúdo novo. [Relatório, imagens e limites](../decisions/RUN_EXP_08_RELATORIO.md).
<!-- /RUNEXP08 CURRENT -->

<!-- RUNEXP06 CURRENT -->
## RUN-EXP-06 — consolidar autoria; imagem antes da conversão futura

03/11/2026. FontesBlender/FBX/UV/materials05 preservadas e refs produtivas confirmadas. Sem nova modelagem06.32IDs/24 ilustrações faltantes auditados. Futuro07: imagem original revisada antes de aplicar emfachada/cobertura, depois revisãoUnity emmovimento. Sem renderer/stylemanager ou conversão atual; ganho de autoria/desempenho precisa ser medido.

Estado06 prevalece sobre gates pendentes/restrições de fases anteriores abaixo, preservados como histórico. [Evidência e limites](../decisions/RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md).
<!-- /RUNEXP06 CURRENT -->

<!-- RUNEXP05 CURRENT -->
## RUN-EXP-05 — pipelineDCC efetivamente disponível

02/11/2026. Blender5.2.2LTS viaMCP9876: fontes.blend separadas preservamScene original; malhas autoradas/limpas, UVtrimatlas1024/basecolor/metallicsmoothness/normal, calibração de eixo e exportFBX real. UnityMCP importou/inspecionou normais/tangentes/UVs/GUIDs e cópias de prefabs nativos. Fragmento5772tris/18renderers; Emissor2172/7. Rig rígido/IK atual, semArmature/clips finaisDCC ou produção artística final.8 ilustrações512 originais mapeiam9IDs; resto do catálogo usa fallback declarado. Fontes/proveniência emTools/RunExp05 easset-manifest05.

[Entrega e limites](../decisions/RUN_EXP_05_2_ENEMY_ART.md).
<!-- /RUNEXP05 CURRENT -->

## Pipeline desejado

Para a futura frente de mapas, incluir a variante **imagem aprovada → câmera/pivot/escala definidos → plano ou volume simples texturizado → física nativa independente → revisãoGameView → comparação de custo**. O [plano2.5D](../decisions/MAP_ART_2_5D_FUTURE_PLAN.md) serve como brief de profundidade/oclusão, sombras e faixa de câmera. É preparação documental, não pipeline já implementado.

`brief com referência → concept (com orientação consistente) → modelo/asset → limpeza/retopo/rig → import Unity → materiais compartilhados → animação → prefab em cena-teste → revisão na câmera real`

Ferramentas possíveis: Tripo ou Meshy para criação 3D, Blender para ajuste, Unity para execução e visual, Codex para scripts e validação. **Não assumir que uma integração ou ferramenta específica está instalada.**

## Implementação localizada em 18/10/2026

- Fontes do personagem ficam em [SourceArt/Characters/TechGuy](../../SourceArt/Characters/TechGuy); o [HANDOFF de autoria](../../SourceArt/Characters/TechGuy/HANDOFF.md) descreve os checkpoints do artefato. Ele não substitui esta direção de produto nem comprova importação de todo checkpoint em Unity.
- O checkpoint 19 contém uma ação Q completa de autoria; o runtime localizado em [SkillAnimationPlayer](../../Assets/_Project/Scripts/Abilities/Weapon/SkillAnimationPlayer.cs) e [BreakerGauntletCombat](../../Assets/_Project/Scripts/Abilities/Weapon/BreakerGauntletCombat.cs) ainda usa movimentos/contatos por golpe. Não atribuir integração ou aceite em Game view ao vídeo de Blender.
- O manifest de pacotes declara Tripo3D por caminho absoluto local. A existência dessa referência não comprova que ela estará disponível em outra máquina; não modificar pacotes ou instalar ferramentas como efeito colateral documental.
- Capturas novas seguem [o fluxo de validação](../validation/02_CAPTURA_E_INSPECAO_VISUAL.md). Não rodar builders de cena/combate para obter apenas uma imagem: alguns também alteram tuning, navegação e cenas.

## Campos obrigatórios no registro do asset

- identidade/uso/silhueta, prompt/seed ou fonte artística, direitos/licença, data/versão;
- unidade/escala/orientação, pivot, topologia/tris, UVs, texturas/resolução, LOD se aplicável;
- rig/esqueleto, clips, loop/root motion, avatar/retarget, compatibilidade com animação;
- import preset, material/shader, parâmetros visuais usados, colisões/performance;
- screenshot na cena padrão e resultado na câmera real.

## O que padronizar

Preset de importação e materiais por **categoria visual compatível**, parâmetros consistentes de cor/luz/outline, texel density e asset QA. **Não forçar um shader toon universal** se armaduras, VFX, chão e rostos exigirem tratamentos diferentes para legibilidade.

## Problemas conhecidos de geração 3D

Orientação de braços, assimetria de equipamentos, T-pose/A-pose, rig de quadrúpedes em esqueleto bípede, normals invertidas, escala incoerente e detalhes invisíveis na câmera de jogo. Para personagens complexos, preferir render/imagem de referência frontal/lateral/traseira consistente ou separar itens rígidos quando esse fluxo produzir melhor correspondência; sempre conferir o modelo no Unity, não só em render de showcase.

## Aceite

Asset deve ser reconhecível no tamanho real, coerente com lote, sem clipping ou UV/rig incompatível e sem impacto desproporcional de draw calls, memória ou GC. A aprovação estética continua humana.

## Fechamento RUN-EXP-09 — 03/11/2026

RUN-EXP-09 integrado: pátio, L, galeria com bolsões e anel são footprints físicos, usando materiais/meshes05 já existentes. Plantas derivadas de BoxColliders/NavMesh são documentação técnica, não concepts nem screenshots de produção. Capturas de comparação e produção são identificadas separadamente. Massa do anel usa material05, sem nova pintura global.

A cena painterly08, suas texturas, shader e captura histórica permanecem isolados/preservados. Beleza, leitura junto à massa e fluxo precisam de playtest e aceite do desenvolvedor; medição geométrica não equivale a aceite artístico. [Relatório09](../decisions/RUN_EXP_09_RELATORIO.md). STOP após09.
