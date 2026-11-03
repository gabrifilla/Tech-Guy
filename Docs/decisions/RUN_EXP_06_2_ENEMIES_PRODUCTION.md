# RUN-EXP-06.2 — consolidação dos inimigos aprovados

Entrada02/11/2026; consolidação03/11/2026, America/Sao_Paulo. O pedido06 aceita explicitamente os modelos e identidades05 para o fluxo normal, sem declarar rig/pintura/clips finais de produção.

## Auditoria e decisão

O primeiroMCP06 reabriu aFirstSector e confirmou que `FirstSectorContent` já referenciava Fragmento05 (`af5d41ab4bac433419d3d4399f164eca`) e Emissor05 (`b47ff7d4f586a5b4abd96e57dbfaf617`). A configuração normal mantém16famílias e apenas duas entradas adicionais. Não faltava instalar um modelo emoutro registro: criar cópias06 ou registrar novos arquétipos duplicaria o conteúdo aprovado.

**DONE:** referências produtivas auditadas e preservadas. Nenhum arquivo de prefab, mesh, material, textura, cápsula, perfil, executor ouAI foi editado nesta consolidação. A publicação deUI06 usa os mesmos inimigos. Fontes03/04, `.blend`, FBX, atlas e scripts de autoria05 permanecem preservados.

| Ator | Geometria e material | Contratos preservados |
|---|---|---|
| Fragmento |18renderers rígidos,5772triângulos, UVatlas/normais/tangentesDCC, pivôs nativos | Cápsula0,85; silhueta baixa/quadrúpede, melee, contatos eIK nativos |
| Emissor |7renderers rígidos,2172triângulos, duas famílias de material, canal oco preservado | Cápsula0,8; conchas separadas, ranged/LOS, preparação e emissão nativas |

InspeçãoUnity: missingScripts0 emambos; referências de materiais válidas; sourceFBX não readable e meshes assados com UVs. Documentação de orientação/calibração, superfície, topologia e limites está em05.2. Não existe novo rig deformável nem rootmotion/retiming de ataque06. A revisão de peso/turnbank05 permanece somente visual.

## Evidência e verificação

Baseline atual06: job `0d8453e3ded9498eac248ed69c7a4313`,13/13PlayMode,85,144s. Inclui comparaçãoDCC real, fases/apoios/dano/despawn, rosters e sala comLOS; é evidência nova anterior à mudança deUI06. `references-baseline.json` registraGUIDs,renderers,triângulos,cápsulas e ausência de scripts faltantes. Capturas atuais foram copiadas para06/baseline com origem ehash; históricos05 não foram regravados.

Validação integrada final de ataques, reações, pausa/controle, mixed encounters, navegação, morte/cleanup e três armas está registrada em [06.4](RUN_EXP_06_4_VALIDATION.md). Não confundir baseline13 com uma suíte final inexistente nem reutilizar o verde05 como prova de06. O custo adicional de geometria já existente será observado novamente, sem otimização global ou remodelagem alheia.

Skills aplicadas: quatroTechGuy locais/referências, UnityMCP/C#/animation/NavMesh, create-game-assets/three-d/art-direction/provenance eperformance-optimization. Elas orientaram preservar owners, conferir importação/pivôs/escala e diferenciar fonteDCC, execução e aceite humano. Não foi necessária nova operaçãoBlender nesta etapa; sua disponibilidade não foi confundida com nova modelagem.

Alternativa rejeitada: mover/copiar todos os modelos para uma pasta06 apenas por nomenclatura. O caminhoRunExp05 indica proveniência do asset, não dependência de uma cena de laboratório. O trade-off é manter nomes históricos na autoria com vínculo produtivo explícito. A integração está aceita pelo pedido06; acabamento final, LOD dedicado e rigs/clipsDCC continuam **NOT DONE**, não foram dispensados como qualidade final.
