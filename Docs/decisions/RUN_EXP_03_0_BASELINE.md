# RUN-EXP-03.0 — baseline e arquitetura de integração

Complemento de encerramento documental02/11/2026: arquitetura abaixo implementada e validada naUnity30/10. Relatórios03.1–4 registram os jobs finais12/12+24/24 PlayMode e26/26 EditMode, os vermelhos intermediários e os limites.32 chaves TechGuy permaneceram iguais no checkpoint finalUnity; uma leitura posterior com Editor fechado encontrou quatro diferenças de gameplay e preservou esses valores atuais, sem atribuir sua origem ou restaurar um snapshot antigo. GateC/D continua humano. Não houve nova validaçãoUnity quando a conexão posterior foi recusada.

Pedido de30/10/2026: desenvolvedor reconheceu e aprovou fisicamente papéis/silhuetas de Fragmento/Emissor e contrajogo por cobertura. A UI de02 foi rejeitada; arte final não foi aprovada. A autorização atual permite integração limitada antes do primeiro boss, dois conceitos de UI isolados, refino e validação, STOP03.4.

## Baseline antes de alterações publicadas

Unity MCP confirmou projeto D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, instânciaTech-Guy@fccafb2b47cd7671. Cena inicial ArenaB deRUNEXP02, Edit Mode, limpa; assistênciaABC false. Branch gameplayOverhaul, HEAD314d28ec, alterações anteriores preservadas. Inventário de1500 arquivos, status e SHA256 em ../validation/artifacts/run-exp-03/baseline-files.json. Cópia restaurável dos arquivos existentes/GUI Ds em restorable-baseline.zip. Registro de preferências completo das chaves do produto, com valores hashados, em baseline-context.json; sem reset. Console inicial0 erros.

Baseline UTF job173d618819be47b3901d72f65c4ac2d4:5/5 PlayMode,13,379s. Casos exatos e saídas em baseline-tests.json: hooks de produção permitem movimento/dash; GPR015 rota0 usa tickets; composição tripla em câmera publicada; raiz de projétil Protocol; dois executoresRUNEXP02 navegam/causam dano/concluem. Capturas da composição são assistidas, não prova de obtenção natural. Relatórios históricos02/01 e falhas31 continuam preservados.

## Grafo de implementação, definido antes de modificar produção

FirstSector._firstSectorContent (novo asset opcional) → ManualRunRandomizer.Generate → mesmo ManualRouteSnapshot/grafo/recompensas, roster com dois prefabs adicionais e offsets seguros → ManualStageEnvironment.BuildGenerated com coberturas escolhidas por stream independente → NavMeshSurface/validação → ProgressionDirector.SpawnManualEncounter → Actor/EnemyVariant/EnemyAI/EnemyCombatActions/CombatReactionController/ProtocolEncounterMembership → mesma contagem de mortes, cleanup, tickets, boss e extração/Nexus.

Config nula preserva o algoritmo anterior. Lista produtiva dos16 arquétipos permanece; novos candidatos são cópias próprias (mesmos behaviors Rush/Sniper), sem IDs novos ou substituição de famílias. Frequência pretendida: cada prefab extra é uma entrada de mesmo peso no pool existente, mantendo os primeiros combatentes e limites de suporte do randomizador. Quantidades/HP/dano não aumentam. Cover:45% dos nós Combat por seed/nó em stream próprio; Elite/Boss/Repair/Start preservados. Não se garante aparição em toda rota nem sequência fixa. Distribuição real será medida. Spawns dentro da massa são realocados em pontos periféricos válidos; nunca sobre o topo, marcado Not Walkable. Mesmos materiais/portas/grafo e validação de caminhos.

Recompensa: RunRewardUI é IMGUI; duas apresentações novas usam a mesma tecnologia em cena isolada, sem ligar apresentação alternativa na FirstSector. Adaptador de leitura consome ofertas e inventário reais; escolher permanece no commit legal existente somente no teste interativo autorizado. Comparações nunca tratam uma cooferta como aquisição. Nenhuma mudança no RNG/economia. Gate visual humano antes de qualquer substituição publicada.

Arte: preservar integralmente Assets/_Project/Prototypes/RunExp02. Cópias integradas recebem refino incremental em assets novos, com comparação técnica/visual; refinamento ainda intermediário se o DCC não funcionar. Blender MCP tentou get_addon_status e não conectou. Nenhum gerador externo ou Blender executado.

## Riscos e gates

Topos navegáveis, spawns encobertos, agente com cápsula de tamanho incorreto, alvo preso, dano através de parede, seeds variáveis, efeito sobre RNG e alteração de saves são riscos explícitos. Áreas existentes não serão redefinidas globalmente. UI exige hierarquia nova com informação secundária sob ação deliberada, duas alternativas reais e comparação em dois aspectos. Erro bloqueador confirmado impede próximo estágio até correção localizada. Scripts/Assets antigos só mudam nos pontos descritos; Nexus, definições, controles, kits e estudos02 são protegidos.

Skills descobertas/read: quatro locais TechGuy e referências; router, unity-mcp-orchestrator, unity-csharp-scripting, unity-scriptableobjects, unity-animation, unity-navmesh, game-ui-ux, create-game-assets/three-d-pipeline, game-feel, performance-optimization, tech-guy-level-layout, tech-guy-enemy-encounters. Catálogo instalado inspecionado; nenhuma skill Blender/rig dedicada encontrada. Aplicação por etapa será registrada nos relatórios03.1–4. Não foram instaladas dependências.

Gate03.0: aprovado para a integração descrita, baseline5/5; revisão artística/UI permanece futura. Este relatório será complementado pelos jobs e alterações observadas, sem reescrever a arquitetura retrospectivamente.
