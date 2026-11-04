# RUN-EXP-07.0 — baseline real e fronteiras de evidência

03/11/2026. **DONE:** auditoria, capturas Unity e verificação focada do ambiente existente. **NOT DONE:** implementação 2.5D. Este relatório não aprova uma direção artística.

## Projeto correto e segurança

O chat iniciou no worktree C:/Users/gabri/.codex/worktrees/09c7/Tech-Guy, HEAD 67d8065a778c3dd91151718abb4dd3f9ca6e73f9, detached e limpo. Ele contém uma versão antiga, Unity 6000.3.3f1/URP 17.3.0, sem Docs nem .agents. Não serve de baseline para os marcos atuais.

Unity MCP confirmou uma única instância Tech-Guy@fccafb2b47cd7671 em D:/Dev/Personal/My_games/Tech-Guy, Unity 6000.5.10f1. Esse checkout é a fonte e destino desta entrega: branch gameplayOverhaul, HEAD 766afa66d93a58afc1b9de1f2eafa15ed8a5504b. Entrada: NexusBlockout limpa, Edit Mode, sem compilação pendente. Havia modificação em ProjectSettings/ProjectAuditorSettings.asset; nenhuma restauração Git foi executada. O estado posterior desse arquivo é conferido nos hashes, sem presumir autoria de mudanças do Editor.

Antes de testar: 2368 arquivos protegidos de Assets/_Project, ProjectSettings, Packages e SourceArt; 47 preferências TechGuy nos dois escopos de registro. Somente hashes, sem exportação de valores privados. Script Tools/RunExp07/safety_snapshot.py. Não se restaura snapshot antigo de save. Fontes de arte, cenas, prefabs, combate, boons, C05, input e economia são protegidos. Sem commit/push.

## Fontes lidas e aplicação

AGENTS raiz, README atual, skills locais e referências; Docs/00_VISAO, art/01_DIRECAO_VISUAL e 02_PIPELINE_DE_ASSETS; 04_ARQUITETURA_E_INTEGRIDADE; sistemas de inimigos/HUD; seções vigentes do GPR_TECHNICAL_DESIGN, masterplan, ledger e matriz; fluxo canônico de captura; relatórios 05.0/05 consolidado e 06.0/06.3/06 consolidado; MAP_ART_2_5D_FUTURE_PLAN, RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION e brief07 completos. Não foram encontradas instruções AGENTS aninhadas em Docs/.agents. Relatórios históricos não certificam a revisão atual.

## Owners encontrados na fonte e assets

| Sistema | Responsável real / evidência |
|---|---|
| Materialização e navegação | ManualStageEnvironment.BuildGenerated, NavMeshData e NavMeshModifier NotWalkable nas coberturas, antes do bake |
| Layout e composição | ManualRunRandomizer, FirstSectorEncounterLayout e FirstSectorContentDefinition; não foram alterados |
| Apresentação | FirstSectorRoomPresentation cria trilhas e bevel; FirstSectorEnvironmentArt troca mesh/material e combina racks visuais |
| Física | BoxCollider original de cada cobertura: 4,2 × 2,2 × 4,2 m. Centros locais (-3;1,1;-1,5) e (3;1,1;1,5) |
| LOS / tiro | EnemyAI e EnemyCombatActions nativos; fixture VerifyEmitterCover observa bloqueio e retomada de impactos |
| Câmera | TG_TopDown_Camera.HandleCamera e CameraOcclusionFader; follow em Start/Update, oclusão em LateUpdate/unscaledDeltaTime |
| UI | RunRewardUI e UI/Rewards; C05 permanece produtiva, sem edição |

FirstSectorEnvironmentArt mantém paredes visuais recuadas e racks combinados num sibling visual. A raiz combinada de racks não é automaticamente resolvida pelo collider da parede. A integração futura deve verificar essa associação por peça; não descombinar todas as paredes nesta etapa.

## Renderização Unity inspecionada

URP 17.5.0 (manifest atual); UniversalRenderPipelineAsset ativo, espaço Linear, qualidade Ultra. Renderer UniversalRenderPipelineAsset_Renderer: zero renderer features via recurso MCP. Asset: HDR ligado, render scale 1, MSAA 1, depth/opaque texture desligados; sombras da luz principal, mapa 2048, quatro cascatas, distância100, soft shadows; sombras de luzes adicionais desligadas. São configurações encontradas, não medições de GPU.

Inspeção aditiva temporária da FirstSector em Edit Mode, fechada sem salvar: luz Directional branca, intensidade1,6, sombras Soft, Euler(52;328;0). Câmera perspectiva FOV40, near0,2/far500. Transform serializado (0;10;-18), Euler(44,7121;0;0), **não confundir com follow em runtime**. Configuração follow16m/16m/ângulo0. Sem binding normal de órbita/zoom encontrado. Variações futuras não estão proibidas nem certificadas.

MCP confirmou URP/Lit em Ceramica grafite cobre (atlas BaseColor/Normal/MetallicSmoothness), Piso de arquivo (ArchiveFloor) e Estrutura de arquivo. Shader.Find confirmou Lit, Simple Lit e Unlit disponíveis; nenhum material novo foi criado. Meshes/fontes05 e prefabs permanecem originais.

## Capturas reais e condições

Teste existente Archive05ComparisonPreservesCoverRoutesLosMovementAndAbilities via Unity Test Framework/MCP, job0c4eed4e5e0042fba8845412ced7466e: **1/1 Passed**, 17,5907712s de seleção, sem skipped/falhas. Uma repetição para tentar coletar câmera: jobbb7e725898674b47a8a286c54feff5e6, **1/1 Passed**,14,8307034s. São duas execuções do mesmo caso, não dois testes distintos. O sampler temporário de Editor não sobreviveu ao reload; nenhum arquivo de telemetria de câmera foi produzido. Reconstrução posterior do planejador executada via CodeDom; tentativa Roslyn falhou por dependência MetadataReader, preservada como falha de ferramenta.

As imagens utilizadas são da primeira execução: pasta original run-exp-03/firstsector/20261103-214151-6130699, copiada integralmente para run-exp-07/review/baseline. Nomes05 pertencem ao helper legado; data, job e execução são atuais07. Segunda execução original:20261103-214738-2761800, preservada. ScreenCapture real,1160×599 pixels; não1920×1080. O helper congelou timeScale0 nas vistas gerais e comparou apresentação04/05; as referências principais07 usam **after**, arte05 produtiva.

| Referência | Arquivo no pacote | Condição |
|---|---|---|
| Arena coberta / base comum A/B/C | baseline/05-room-cover-after-normal-camera.png | Seed63,nó1, jogador no centro; mesma imagem de entrada para os três conceitos |
| Arena aberta | baseline/05-room-open-after-normal-camera.png | Seed0,nó1; referência separada, não A/B de geometria idêntica |
| Jogador junto à cobertura | baseline/05-room-player-circulating.png | Movimento nativo pela rota lateral |
| Fragmento / telegraph | baseline/natural-Fragmento-windup-node-1.png | Fase real, aviso vermelho ao lado do jogador |
| Emissor normal / LOS | baseline/natural-Emissor-idle-node-1.png e emissor-blocked/reacquired | Roster real; fixture posicional explícita |

Fixture: jogador imune, warps para entrada/LOS, AI/nav/dano inimigo ativos, remoção de CoinDrop em memória; preferência de arma/moeda/resumo e SessionState restaurados pelos scopes existentes. Não é gameplay humano sem assistência. Passou igualdade de contagem/bounds dos colliders entre apresentações, dois paths completos, raio de cobertura, movimento real e bloqueio/retomada de Emissor. Não prova método2.5D, pois nenhum foi implementado.

## Escala e câmera reconstruídas, não amostradas no PNG

Executado no Unity o planejador puro com asset FirstSectorManual e seed:0→centro(500;0;38), arena29×19m;63→centro(500;0;43), arena28×24m. Fonte do fixture posiciona jogador no centro antes da captura. Pela fórmula atual do follow: seed63→câmera(500;16;27), Euler(45;0;0); seed0→(500;16;22). São **reconstruções de configuração/código**, não transform medido no mesmo frame. O CSV poses-node-1 registra posições reais dos inimigos após a vista geral, não daquele frame exato. Essa limitação deve ser corrigida no manifesto do futuro A/B implementado.

Faixa observada: follow atual em sala coberta/aberta, FOV40; não há intervalo de zoom certificado. QA futura deve testar center/bordas e frente/trás das duas peças antes de aceitar uma faixa. Fonte de escala humana ~1,8m nas pranchas é referência ilustrativa, não medição do rig.

**NOT TESTED:** desempenho2.5D, outras resoluções nesta rodada, suporte de novas texturas/fader, gameplay dos conceitos. Console inicial0erros/aviso de Input Manager; final sem erros de gameplay observados e warnings existentes de keys/AbilityHolder. Não corrigir tuning/bindings para silenciar warning fora do escopo.

## Integridade final e QA do pacote

**DONE:**47/47 preferências TechGuy existentes idênticas,0adicionadas/removidas.2367/2368 arquivos protegidos idênticos;0adicionados/removidos nesses escopos. Única divergência: ProjectSettings/ProjectAuditorSettings.asset, modificado já na entrada e diferente no hash final; ao final coincide comHEAD. O agente não editou/restaurou esse arquivo. Origem da transição não confirmada; não atribuir causalidade nem restaurar conteúdo antigo por suposição. Não se declara integridade byte a byte desse ajuste doEditor. Evidência:evidence/integrity-comparison.json e snapshots antes/depois.

Editor final NexusBlockout limpa/Edit, timeScale1, assistênciaTitanicConductorfalse, seedoverride vazio e scope de resumo encerrado. Sem commit/push. Capturas e geração são isoladas emDocs/Tools. Revisão das seis artes feita nas imagens retornadas peloImageGen; baseline cover/windup aberta porview_image.

**BLOCKED:** préviaHTML no navegador da ferramenta, por política que recusa file:. Nenhum contorno do bloqueio foi tentado. **NOT TESTED:** renderização visual e interação doHTML em navegador real. Verificação estática deHTML/caminhos, assinatura/dimensões/CRC dePNG, proveniência/hashes e integridade doZIP são independentes e constam empackage-qa.json. Essa limitação permanece explícita em vez de alegar inspeção visual deHTML não executada.
