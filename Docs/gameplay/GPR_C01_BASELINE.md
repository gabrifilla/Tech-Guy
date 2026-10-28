# GPR-C01.1a — Baseline observada dos controles legados

26/10/2026, America/Sao_Paulo. Esta baseline não contém WASD nem RMB→Asura implementados. Aprovação humana recebida: GPR-00 documental, somente C01.1a/b; [revisão independente](../decisions/VALIDACAO_GPR00_TECH_GUY_20261026.md) e texto original em `../validation/artifacts/gameplay-rework/c01-baseline/autorizacao-recebida.txt`. A instrução atual do desenvolvedor supera propostas anteriores.

## Condições e integridade

MCP confirmou `D:/Dev/Personal/My_games/Tech-Guy`, Unity6000.5.10f1, instância Tech-Guy@fccafb2b47cd7671, branch gameplayOverhaul, HEAD3a3f0ee8ad82734104511b63877d38df512ea146 + alterações locais. Início/fim em NexusBlockout limpa, Edit Mode. Não havia run no Editor. C:~152GB/D:~33GB livres antes; seis capturas usaram cerca de144MB, incluindo tentativa descartada. Não foi executado batchmode nem aberto outro checkout.

1778 arquivos preexistentes próprios/config/pacotes/skills foram hasheados nesta coleta ampliada (inclui extensões além das1597 anteriores). As546 exclusões totais coincidem integralmente com o snapshot antigo. Destas,36 estão em Docs e510 fora de Docs: a diferença histórica era o universo contado, não prova de restauração. Preservar ambas as evidências com sua cobertura explícita.

Preferências consultadas: moedas2597, loadout0, unlocks1/2=1, tutorial=1; chave de unlock0 ausente. Bindings Q/W/E/F11 e F, PrimaryMouse0/MoveMouse1/DashSpace iguais antes/depois. RunSummary42387 caracteres, SHA2569751067523EABB7557FFEC504779DB853925D6665AE876DAF8C0C489D52189B3 preservado. Não houve restore manual de resumo antigo; nenhuma run foi criada/finalizada para obter estes PNGs. Resultados exibem o registro histórico real, não nova vitória.

## Cobertura das doze questões da autorização

| Questão | Evidência atual | Conclusão/limite |
|---|---|---|
|1. Click-to-move|Captura Nexus + entrada virtual Mouse1; LMB no chão também retornou ao ponto inicial|Ainda há dois caminhos de clique para destino; migrar exige desligar ambos, inclusive autoaproximação|
|2. Velocidade/aceleração/diagonal/colisão|Medição por API do controlador legado: mediana planar4,99857m/s cardinal e4,99989m/s diagonal, máximos~5 m/s; agente speed5/acc99999|Sem bônus diagonal nesse path legado. Não é prova do futuro WASD; aceleração subframe não mensurada. Altura/slope torna velocidade3D inadequada para comparar diagonais|
|3. Navegação e autoridade|Agente existente; API TryRequestGroundMovement, caminho ao treino com6 corners; testes de destino inválido/ilha/CC|Sem teleport/bake, todas amostras do percurso medido permaneceram no NavMesh. Cantos, avoidance e colisão do candidato Agent.Move ainda NÃO foram prototipados|
|4. Referência de câmera|TG_TopDown_Camera real,45°pitch/0°yaw na sessão; posição e orientação nos manifests|Base de câmera projetada é recomendação; mundo vs câmera e dash ainda exigem teste humano. Não alterar a câmera para decidir|
|5. Mira e alvo|Virtual Mouse.position e raycast de produção; alvo real TrainingDummy; testes target sem intenção/tap retido|Selecionar alvo não deve criar ataque persistente; futuro ataque independente não pode herdar perseguição|
|6. Tap/hold/combo|Tap Shift+LMB produziu1 básico na janela; hold legado produziu repetições no treino;20 básicos intercalados com skills, intervalo mediano de parede~0,295s|Hold existe e deve ser migrado explicitamente. Esse intervalo inclui interrupções/overhead e não é o tuning da arma0,2s nem benchmark|
|7. Asura|Meter chegou a100 por contatos normais no dummy; F11 recusado Busy durante Q; nova tentativa livre aceita1 vez, energia100→0 e mana1500→1353|Execução nativa atual com cooldown16s nominal; sem resource injection. Cadenciada/early finish cobertos por testes de fixture, não adquiridos naturalmente nesta sessão|
|8. Dash|Space virtual publicou AbilityUsed slot4, deslocou o player; paths e prioridades em testes|Dash tem movimento próprio; agent.velocity=0 durante dash não significa ausência de deslocamento. Direção atual mira cursor; proposta WASD ainda não aprovada|
|9. QWER|Q/W/E virtuais e F11 atual; leitura do holder e PNGs de HUD|index0 Avanço56mana/3s;1 Punhos77/5;2 Choque98/6;3 Asura147/16. Recarga nominal não inclui tempo Active nem redução da run|
|10. Finalização/interação|FinishSequenceF e Skill3/E no Nexus inspecionados, prompt E capturado; testes de finisher|F só finaliza Cadenciada/retorno elegível; não é Choque nem Asura inteira. Default Interact futuro permanece por decidir|
|11. UI/Nexus/resultados|Pausa no roteiro; resultados abertos por API real: BlocksWorldInput=true e TryDirectionalBasicAttack=false. Controles atuais exibidos sem Apply|Rebind/settings novo não implementado. Modal e página Controls abertos por API/reflexão, não navegação física humana; gate de soltura e conflitos têm testes/inspeção|
|12. Bindings/save|Leitura atual, screenshots F11,15 testes EditMode de binding/identidade e comparação de prefs|Persistência da configuração NOVA e piloto de overrides/migração ainda futuros; não tocar dados humanos para testar reset|

## Métodos e testes

- **Entrada humana física:** não realizada nesta atividade; nenhum aceite de feel/latência atribuído ao desenvolvedor.
- **Entrada virtual:** Keyboard/Mouse temporários do Input System, controles legados reais, outros dispositivos temporariamente desabilitados/restaurados. Scripts externos a Assets, sem alteração de bindings. Sem controle de FPS no modo current.
- **Comandos por API:** deslocamento preparatório/navegação numérica pelo controlador antigo, Pause/Resume e OpenManual/Close do painel real; abertura da página Controls por reflexão de estado/draft sem Save. Não equivalem a clique físico.
- **Unity Test Framework:**21/21 PlayMode (`6881b001422847d1974a22f9a82046da`) e15/15 EditMode (`b582bf9d07d14eac87006387f430ec9f`). Resultados detalhados/nomes nos JSONs; não são uma suíte global.

As fixtures PlayMode usam chão/atores de teste e algumas ajustam recurso/estado para exercitar fases da Cadenciada. Isso é distinto da energia natural usada no vídeo/PNGs do dummy real. Warnings de camera/dash/efeito/keys/Animator incompletos em fixtures foram preservados. Histórico Mutation/BurnTick não foi reexecutado.

## Capturas reais inspecionadas

Pipeline GameplayObservation existente, current,1280×720, PNGs e events.jsonl, condições/câmera/revisão nos manifests. Modo current não impõe seed/tempo; rótulo GAUNTLET_SMALL_GROUP não cria inimigos: aqui é Nexus/dummy, sem encounter hostil.

| Run | Conteúdo | Resultado |
|---|---|---|
|20261026T220046042-caf13bee|Primeira tentativa de diagnóstico: filtro de update impediu input|Captura ambiental completa, mas **descartada como prova de comandos**|
|20261026T220118194-1514b312|Movimento/tap/hold/dash/QWE/F11 indisponível|233 PNGs; complete/restored|
|20261026T220320789-7ecce255|Treino/ganho natural/Asura Busy durante Q|250 PNGs; complete/restored|
|20261026T220437791-c65a550e|Asura livre acionada por F11 virtual|85 PNGs; complete/restored|
|20261026T220632531-840f6b20|Resultados reais/ownership de modal|13 PNGs; complete/restored|
|20261026T220710899-cb654ca9|Controles legados na pausa, sem salvar|9 PNGs; complete/restored|

Oito PNGs selecionados foram abertos e copiados sem edição para [capturas](../validation/artifacts/gameplay-rework/c01-baseline/capturas); a captura adicionalframe180 da segunda run também foi aberta. Não foram inspecionados todos os frames. Os manifests e hashes dos events completos foram verificados. As imagens mostram HUD contextual ausente longe do treino, custo/recarga dos slots, geração de Asura, indicação EM USO/85% de proteção, dano no dummy, interação E, bindings reais e modal de resultados. Não certificam justiça contra boss, arte final, foot sliding em todas direções ou timing quadro a quadro.

## Incidentes/limitações

Execute_code/Roslyn falhou após reload por MissingMethodException de System.Reflection.Metadata; backend CodeDom executou os diagnósticos. Um delegate EditorApplication.update foi corrigido para CallbackFunction antes de executar; falhas eram compilações temporárias em memória, sem editar C# de produção. Primeira captura não recebeu input pelo filtro do diagnóstico, corrigido na segunda; não atribuir essa falha ao gameplay. Um clique diagonal do primeiro roteiro não produziu caminho; novo percurso API validou diagonal navegável, sem presumir falha do futuro motor.

Autoridade/riscos que limitam prontidão: Asura está acoplada ao index3/identidade R de snapshot/boons/analytics; extraction para especial não pode ser só binding duplicado. Bow ChargedShot depende de hold; RMB de Bow/Spear não existe como especial universal verificado. Nenhum protótipo novo de movimentação, gamepad, teste físico, reinício standalone ou baseline hostil foi executado. Esses limites entram no desenho e no gate seguinte.
