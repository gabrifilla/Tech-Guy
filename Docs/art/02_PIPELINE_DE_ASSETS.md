# Pipeline de assets — consistência para arte gerada por IA

## Pipeline desejado

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
