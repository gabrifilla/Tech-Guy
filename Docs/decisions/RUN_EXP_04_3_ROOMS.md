# RUN-EXP-04.3 — apresentação espacial das salas atuais

Escopo: somente apresentação deCombat na autoridadeManualStageEnvironment.BuildGenerated. Não reconstruir gerador, fechar caminhos, trocar grafo ou adicionar obstáculos aleatórios. Implementado e testado; direção visual final ainda humana.

## Diagnóstico e alterações

Base03: piso/parede uniformes, duas massas deslocadas de4,2×2,2×4,2, chance45% nosCombat por stream independente. Distinção mecânica aprovada já existe; apresentação ainda de blockout. Arquivos inspecionados:ManualStageEnvironment.Procedural/FirstSectorContentDefinition/GeneratedBox/GeometryPlan/Navigation e capturas03. NavMesh usaCollectObjects.Children ePhysicsColliders, fato que orientou os detalhes sem colisão.

CriadosFirstSectorRoomPresentation.cs eRunExp04RoomAuthoring.cs, três materiaisURP/Lit próprios emArt/RunExp04/Rooms. OContent ganhou referências opcionais de piso aberto/coberto/trim. Uma chamada noBuildGenerated cria apresentação somente se todas as referências estiverem presentes; contexto sem assets mantém forma anterior. Nenhuma alteração noNexus ou salasBoss/Repair/Elite/Start.

Sala aberta: piso mais neutro, perímetro com divisões curtas, centro silencioso para telegraphs e alvo. Sala coberta: piso mais frio/escuro, duas trilhas longitudinais de circulação e marcas discretas nas bordas. Massas recebem mesh chanfrada visual dentro do mesmobounds;BoxCollider conserva o volume03. Pequeno recorte visual de canto<=0,105m mantém collider simplificado explícito. Não adicionar luzes, bloom, colisões, triggers, obstáculos deNavMesh ou efeitos vermelhos semelhantes a perigo.

Meshes geradas têmUV/normals e dono que as destrói no teardown; não são materiais instanciados por quadro. SetVisible permite comparar antes/depois no runtime sem rebake ou escrita em asset. Decor éMeshFilter/MeshRenderer puro, semCreatePrimitive/collider pendente antes do bake. Renderers extra: um de trilhas por sala; duas coberturas compartilham mesh chanfrada da sala. Floor materiais compartilhados. Orçamento final não alegado.

Não introduzida terceira configuração de colisão: a variedade atual de tamanhos/salas abertas/cobertas continua suficiente para este polimento limitado. Variar obstáculos aleatoriamente sem novo gate de navegação criaria risco desnecessário. Padrões visuais e proporções não são tema final de todo universo.

## Evidência, testes e segurança

MCP confirmouUnity6000.5.10f1/projeto real; autoria de materiais/refs salva emrooms-authoring.json. Console0 apóscompile. Job`854f15035d3a450d8f45a73c4770e43d`:2/2 PlayMode,27,777s. Comparação aberta0/coberta63 na câmera normal, mesma pose/iluminação para cada antes/depois. Colliders/bounds idênticos com apresentação ligada/desligada; duas circulações completas; massa bloqueia o raio geométrico; Emissor real interrompe/reobtémLOS. Básicos deManopla/Arco/Lança acertam os candidatos04 em aberto e não atravessam a cobertura.

Os rays geométricos consultam apenas os colliders estáticos da arena, evitando falso positivo de um inimigo cruzando a linha; o teste de tiro usa executor nativo separadamente. Jogador imune só na comparação visual; teste de contato usaAI congelada/colliders isolados explicitamente, não desempenho. Capturas04-room-* em`../validation/artifacts/run-exp-03/firstsector/20261102-201011-6400567/` foram abertas/inspecionadas: trilhas discretas, centro livre, telegraphs vermelhos legíveis; antes/depois entrarão no pacote04. Testes emrooms-play.json incluem saídas exatas. Nenhum vermelho desta etapa ou alteração de arma/custo/recompensa.

Skills aplicadas:TechGuyvision/combat/acceptance, level-layout/encounters, create-game-assets, UnityMCP/C#/NavMesh eperformance-optimization. Regras: detalhe periférico, física como autoridade, contraste sem neon indiscriminado, teardown e evidência real separados deaceite autoral. Sem textura externa/DCC ou palette final alegada.

Gate técnico04.3PASS no recorte; preservação32prefs e hashes/cenas/GUIDs será encerrada em04.5. Performance8/32/64 será repetida, sem supor custo gratuito. Próximo: evoluçãoUI somente empreview; decisão e publicação continuam separadas.

## Fechamento da sequência04

Esta seção não altera os resultados intermediários acima. A integração final foi encerrada em04.5:15/15 Play integrados,1/1 matriz ampliada com12 cenários,33/33 regressões,10/10 efeitos de habilidades e28/28 Edit selecionados.1562/1574 hashes iguais e12 alterações previstas;32 preferências TechGuy atuais iguais, sem restaurar03. Fontes/cenas/metas antigas e UI publicada preservadas, Console0 erros; checker mantém dois diagnósticos históricos. Performance, GUIDs, jobs completos, limites de persistência e aprovação física estão no [relatório de validação](RUN_EXP_04_5_VALIDATION.md). [Entrega e pacote visual](RUN_EXP_04_RELATORIO.md). STOP04.5: arte final, direção de UI e julgamento físico permanecem pendentes.
