# RUN-EXP-07 — pacote de revisão visual até07.4

03/11/2026. **DONE:** auditoria da base correta, três direções, seis artes originais, proposta de fachada/cobertura e pacote HTML offline. **PENDING DEVELOPER APPROVAL:** escolha artística. **NOT DONE:**07.5/07.6, protótipo Unity e conversão de FirstSector. **NOT TESTED:** desempenho/leitura dos conceitos em movimento. A entrega distingue arte gerada, capturaUnity e proposta técnica.

## Abrir e decidir

[Revisão offline](../index.html) e ZIP portátil na pasta da entrega original. As imagens e relatórios estão incluídos; o HTML não usa rede, bibliotecas externas nem fontes remotas. Comparação individual e conjunto A/B/C, opção visual em cinza, pranchas de faces/materiais, baseline coberta/aberta, telegraph e LOS. Os PNGs completos são os originais raster do ImageGen. Não existem PSD/blend/fontes em camadas fornecidos pelo gerador.

**Recomendação: A — Arquivo Digital Fragmentado**, para reduzir variáveis na primeira comparação técnica e aproveitar continuidade com FirstSector05. B evidencia contraste gráfico urbano; C evidencia linguagem pictórica mineral. A escolha não apaga as alternativas nem estabelece lore. A recomendação não concede aprovação.

## Skills efetivamente aplicadas

| Skill lida | Técnica/regra aplicada | Decisão resultante |
|---|---|---|
| router | Detectar engine/versão real antes de roteiro | Recusar baseline do worktree antigo; usar Unity6000.5/URP17.5 conectados |
| tech-guy-game-vision + vision-and-constraints | Distinguir intenção, implementado, verificado e aceito | Mundos são propostas; preservar Nexus, personagem e armas |
| tech-guy-combat-unity + combat-matrix | Autoridades de dano/controle/LOS já existentes | Nenhuma mudança de combate, boons, input ou Burn |
| tech-guy-unity-acceptance + editor-validation | MCP, filtro proporcional, evidência atual e restauração | Teste existente focado, scopes atuais e hashes/prefs, sem verde global |
| unity-mcp-orchestrator | Resource-first, instância/câmera/pipeline antes de ação | Confirmar raizD:, cena limpa, renderer sem features, leitura aditiva sem salvar |
| unity-csharp-scripting | Versão fixada/propriedade serializada/lifecycle | Proposta usa owners existentes; nenhuma classe runtime nova |
| create-game-assets + art-direction/raster/three-d/provenance | Forma/valor/material, câmera comum, alvo em escala e fonte rastreada |6 imagens com prompts/hashes; piso quieto, pranchas não sãoUV/fontes fictícias |
| imagegen | Ferramenta integrada para gerar/editar, fontes copiadas ao projeto |6 chamadas reais; referências de baseline e família; sem fallbackCLI |
| tech-guy-world-identity + world-card | Tese e contraste sem depender de cor; invariantes |A estratos, B contorno/cidade, C pincel/curvas; sem mecânica/lore extra |
| camera-systems | Follow/depth/oclusão pertencem à câmera | Propor faixa e testar extremos sem billboarding da cobertura |
| level-design | Métricas/circulação antes de dressing | Duas massas e mesmas rotas, sem novo layout ou obstáculos |
| performance-optimization | Medir antes de otimizar, separar Editor/build | Especificar A/B e custos de textura/overdraw; nenhum ganhoFPS alegado |

Skills locais encontradas: quatro em .agents, incluindo boon-design, cujo SKILL.md não foi aplicado porque o catálogo está fora do escopo; seu mapa foi identificado na descoberta. Não há skill local separada de renderURP, pintura2.5D ou Blender. As skills instaladas oferecem câmera, assets, shaders e desempenho; shader-programming foi descoberto no catálogo mas não aplicado porque não há autoria de shader. Fontes técnicas oficiais e shader/assets reais sustentam a proposta, sem instalar skills/ferramentas adicionais. Não foi delegada tarefa a subagentes.

## Auditoria e estado atual

Fonte/entregaD:/Dev/Personal/My_games/Tech-Guy, gameplayOverhaul,HEAD766afa66. Worktree do chat09c7 ficou intacto, por ser anterior aos marcos aprovados. Cena inicial/finalNexusBlockout limpa/Edit. UnityMCP6000.5.10f1 eURP17.5 reais. Presentations atuais já separam arte/física. Fader suporta MeshRenderer e tem limites de hierarquia/alpha; racks combinados exigem associação local. C05, Fragmento eEmissor preservados. BlenderMCP **BLOCKED** por conexão recusada; consequência: nenhum .blend/modelo/UV novo, sem impedir conceitos raster. ImageGen integrado executou as seis artes. Ferramenta execute_code/Roslyn falhou numa leitura de reconstrução por MetadataReader; CodeDom executou a consulta equivalente. Nenhum erro de script de produção foi corrigido ou introduzido por essa leitura.

## Verificações realizadas e limites

Unity UTF: uma seleção focada executada duas vezes,1/1Passed cada; segunda tentou coletar metadata de câmera por callback, perdido no reload. Nenhuma declaração de telemetria não produzida. Reconstituição explícita das seeds emUnity registrada separadamente. Capturas atuais deGameView1160×599 com roster/telegraph/cover/LOS. O fixture tem imunidade/warps e restaura prefs correntes, não reproduz sobrevivência humana. Console inicial avisoInputManager; execução tem warningsAbilityHolder, sem alteração de binding. Não rodou suite global, benchmark2.5D, build novo, playtest humano ou conversão. Validação do pacote e hashes no manifest/QA/integridade.

## Riscos e dependências

Arte raster reinterpreta detalhes do HUD/personagens e enquadramentoB: são limitações de conceito, não mudança aprovada. A prancheta pode sugerir recorte que precisa ser contido no envelope físico. Antes deUnity, redesenhar mapas de face sem títulos/sombras duplicadas, acertarUV/cantos/pivot e validar noMaterialInspector. Custo artístico estimado não é medição. Alpha/fade pode revelar retângulos; plano perde volume sob follow; texturas/materiais/overdraw podem aumentar custo. Conservar3D como fallback por peça.

## Arquivos produzidos e modificados

Produzidos: RUN_EXP_07_0_BASELINE.md,07_1_MULTIVERSE_ART_DIRECTION.md,07_2_VISUAL_CONCEPTS.md,07_3_TECHNICAL_PROPOSAL.md, este relatório e07_SKILL_APPLICATION.md; artefatos emDocs/validation/artifacts/run-exp-07; Tools/RunExp07/safety_snapshot.py ebuild_review.py. Novas evidências dos helpers permanecem nos dois diretóriosrun-exp-03/firstsector datados214151/214738. Manifesto enumera imagens/prompts/fontes e hashes; inventário final lista o pacote.

Modificados intencionalmente somente três documentos mestres: TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md, GAMEPLAY_REWORK_PROGRESS.md e01_MATRIZ_DE_VALIDACAO.md, com bloco07 acima do histórico. Nenhum arquivoUnity de produção/art source foi editado pelo agente; comparação integral dos escopos protegidos acompanha esta afirmação. Se houver divergência de configuração doEditor no relatório de integridade, ela deve ser exposta e não revertida automaticamente.

## Gates e parada

| Gate | Status / limite |
|---|---|
| A skills/projeto | DONE com metadata de câmera reconstruída explicitamente, não amostrada no mesmoPNG |
| B diferenciação | DONE como demonstração artística; reconhecimento humano pendente |
| C leitura | DONE como análise visual por candidato; NOT TESTED em2.5D jogável |
| D artes/proveniência | DONE,6PNG originais/prompts; fonte emcamadas não disponível |
| E técnica | DONE como proposta de duas peças, NOT TESTED implementação |
| F integridade | Conferir evidence/integrity-comparison.json; nenhum commit/push ou restauração de save |
| G aprovação | PENDING DEVELOPER APPROVAL |

Decisões para o desenvolvedor: escolherA/B/C ou pedir revisão objetiva; aceitar a linguagem das duas peças ou indicar ajustes; autorizar expressamente07.5 somente após escolher imagens/recorte. Aprovação da imagem não é aceite da integração; implementação local não autoriza conversão global. **STOP após07.4.**

## Integridade final e QA do pacote

**DONE:**47/47 preferências TechGuy existentes idênticas,0adicionadas/removidas.2367/2368 arquivos protegidos idênticos;0adicionados/removidos nesses escopos. Única divergência: ProjectSettings/ProjectAuditorSettings.asset, modificado já na entrada e diferente no hash final; ao final coincide comHEAD. O agente não editou/restaurou esse arquivo. Origem da transição não confirmada; não atribuir causalidade nem restaurar conteúdo antigo por suposição. Não se declara integridade byte a byte desse ajuste doEditor. Evidência:evidence/integrity-comparison.json e snapshots antes/depois.

Editor final NexusBlockout limpa/Edit, timeScale1, assistênciaTitanicConductorfalse, seedoverride vazio e scope de resumo encerrado. Sem commit/push. Capturas e geração são isoladas emDocs/Tools. Revisão das seis artes feita nas imagens retornadas peloImageGen; baseline cover/windup aberta porview_image.

**BLOCKED:** préviaHTML no navegador da ferramenta, por política que recusa file:. Nenhum contorno do bloqueio foi tentado. **NOT TESTED:** renderização visual e interação doHTML em navegador real. Verificação estática deHTML/caminhos, assinatura/dimensões/CRC dePNG, proveniência/hashes e integridade doZIP são independentes e constam empackage-qa.json. Essa limitação permanece explícita em vez de alegar inspeção visual deHTML não executada.
