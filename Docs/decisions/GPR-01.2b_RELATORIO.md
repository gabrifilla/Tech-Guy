# GPR-01.2b â€” Punhos TitÃ¢nicos: tornado de vento mÃ³vel

28/10/2026 Â· **RevisÃ£o de A; STOP antes de GPR-01.3.** ValidaÃ§Ã£o final descrita abaixo; aceite fÃ­sico do novo tornado pendente.

## Escopo e feedback humano

O desenvolvedor testou a GPR-01.2 e aceitou sua funcionalidade tÃ©cnica, mas considerou o gameplay pouco transformador. A onda instantÃ¢nea anterior nÃ£o foi aprovada como design final. Esta entrega atende Ã  [autorizaÃ§Ã£o direta](AUTORIZACAO_GPR_01_2B_20261028.md): tornado lento, progressivo e emitido tambÃ©m apÃ³s terminal em whiff. O [relatÃ³rio original](GPR-01.2_RELATORIO.md) e seus artefatos permanecem histÃ³ricos, sem reescrever a experiÃªncia anterior.

NÃ£o implementados grants de Conductor/Detonation, troca modular, nova skill, alteraÃ§Ã£o dos ataques/Asura, nem GPR-01.3. Controles, navegaÃ§Ã£o, animaÃ§Ãµes, cadÃªncia, janelas de cancelamento e dano nativo permanecem sob os executores existentes. O trabalho Ã© uma extensÃ£o do ProtocolRuntime e do contato bÃ¡sico atual.

## PreparaÃ§Ã£o e estado inicial

AGENTS.md e canÃ´nicos lidos; skills locais e instaladas efetivamente consultadas. Projeto confirmado novamente no MCP real: `D:/Dev/Personal/My_games/Tech-Guy`, Unity **6000.5.10f1**, instÃ¢ncia `Tech-Guy@fccafb2b47cd7671`. O cwd antigo do chat nÃ£o foi utilizado para editar. Branch `gameplayOverhaul`, HEAD inicial `37fa43fca5b09115c46704ccd6f01ddadb089360`; alteraÃ§Ãµes anteriores nÃ£o commitadas preservadas, inclusive o CSV recovery-normal jÃ¡ modificado. [Git inicial](../validation/artifacts/gameplay-rework/gpr-01-2b/git-initial.txt), [hashes iniciais](../validation/artifacts/gameplay-rework/gpr-01-2b/baseline-hashes.json).

NexusBlockout inicialmente limpa em Edit Mode; nenhuma sessÃ£o humana ativa observada. MCP via HTTP local existente, sem simular Editor. A sessÃ£o HTTP antiga retornou404 na retomada; reinicializada a sessÃ£o cliente e reconfirmado o projeto. Durante EditMode houve desconexÃ£o e timeout do plugin; o Editor recuperou a resposta sem ser encerrado. Resultados sÃ³ contabilizados apÃ³s retorno do job completo.

## EmissÃ£o e impacto: contratos distintos

Na aceitaÃ§Ã£o do terminal, o snapshot existente confirma A/BasicFinisher e captura o pacote elegÃ­vel. Somente o callback de contato real de ActionImpactScheduler, depois do batch nativo de HP/notificaÃ§Ãµes/feedback, chama `CommitTitanicTerminalContact`. NÃ£o se usa clique, evento cosmÃ©tico, pedido de ataque ou overlap para conceder emissÃ£o. Cancelamento antes desse contato nÃ£o emite; cancelamento legÃ­timo da recovery dentro de uma notificaÃ§Ã£o apÃ³s contato preserva a emissÃ£o jÃ¡ comprometida, desde que owner/arma/contexto continuem vÃ¡lidos.

`ReserveTitanicTerminalEmission` reserva filho especÃ­fico na **mesma raiz**, usando a quota compartilhada de filho bÃ¡sico. A marca de contato Ã© estreita: owner, execuÃ§Ã£o, Basic/Direct, snapshot de A, terminal e dano finito. `ReserveConfirmedBasicChild` e seu requisito de receipt positivo **nÃ£o foram relaxados**. A reserva do projÃ©til nÃ£o cria HP, estatÃ­sticas, energia, mana ou raiz. A raiz retida encerra quando o soco e o futuro terminam.

Cada passo resolve overlap inicial e capsule sweep contÃ­nuo, escolhe primeiro a distÃ¢ncia do sÃ³lido bloqueador e sÃ³ depois considera vÃ­timas antes dele. HÃ¡ LOS entre origem e spawn offset para nÃ£o saltar uma parede fina, e LOS de segmento para o alvo. Triggers de hurtbox sÃ£o elegÃ­veis; sÃ³lidos sem Actor bloqueiam. SaturaÃ§Ã£o dos arrays/negaÃ§Ã£o de query encerra com seguranÃ§a. Filtro de inimigo, layer, vida/atividade e membership de sessÃ£o/encontro preservado.

Impactos usam `QueueDamage`/`DrainDamage`, confirmaÃ§Ã£o de HP e estatÃ­sticas existentes, produtor Technique, profundidade1, linhagem Basic/A e **RequiredReservation** revalidada em cada commit. DeduplicaÃ§Ã£o por identidade+geraÃ§Ã£o; vÃ¡rios colliders nÃ£o duplicam HP. VÃ­timas nativas positivas do mesmo terminal continuam excluÃ­das, preservando a regra anterior. A mesma entidade reativada com nova geraÃ§Ã£o pode receber um impacto dessa geraÃ§Ã£o. Nenhum tÃºnel novo de dano nem criaÃ§Ã£o de outra raiz a cada vÃ­tima.

PolÃ­tica da linhagem: **`ProtocolBasicEffects.None`**, inclusive com Conductor/Detonation adquiridos. Nenhum grant novo de Protocol, postura/CC, recurso ou novo tornado causado por dano do tornado. Comportamentos nativos de Protocol fora desse ramo continuam intactos.

## Dados experimentais e dano exato

| ParÃ¢metro | Valor |
|---|---|
| ID / alvo / rank | `mutation_gauntlet_titan_fists` / Manopla Basic /1 |
| Deslocamento mÃ¡ximo |5,5m a partir do centro de spawn; origem de spawn avanÃ§a0,75m da pose do soco |
| Velocidade / vida mÃ¡xima |3,5m/s /2s escalados; normalmente alcance encerra em aproximadamente1,571s de movimento |
| Raio / altura |0,65m /1,8m, cÃ¡psula elevada0,05m do chÃ£o plano |
| Dano |0,60 Ã— pacote nÃ£o crÃ­tico capturado na aceitaÃ§Ã£o do terminal |
| DireÃ§Ã£o |Forward comprometido no contato; independente da rotaÃ§Ã£o/mouse posterior |
| Gates |Uma emissÃ£o por terminal; budgets,32 impactos e caps existentes; sem aumentar limites |

O pacote Ã© `Stats.CaptureAttackDamage(weapon.attackDamage, 1, 0, momentum.Modifier, momentum.BatchMultiplier(rank)).Amount`, com o mesmo tratamento elegÃ­vel de Momentum do caminho de dano existente. Captura stats e Momentum presentes **na aceitaÃ§Ã£o**; nÃ£o usa HP efetivo, alvo, receipt inexistente ou maior receipt. NÃ£o chama `RollAttackDamage` e nÃ£o sorteia crÃ­tico novo. O fator0,60 Ã© aplicado uma vez ao criar o trabalho; a fila recebe o valor jÃ¡ resolvido e nÃ£o reaplica modificadores do atacante. A vÃ­tima ainda aplica sua mitigaÃ§Ã£o/imunidade. O bÃ¡sico nativo conserva suas regras de crÃ­tico e timing. Uma alteraÃ§Ã£o posterior em Stats nÃ£o muda o projÃ©til aceito.

Todos os novos valores estÃ£o em TitanicFistsDefinition, sem tuning de dano/velocidade/dash/cadÃªncia nativos. A antiga constante GeometryMultiplier permanece sem uso; nÃ£o participa do tornado. Valores sÃ£o hipÃ³teses de protÃ³tipo, nÃ£o aprovaÃ§Ã£o de balanceamento.

## Lifecycle e apresentaÃ§Ã£o

O tornado tem posiÃ§Ã£o prÃ³pria, avanÃ§a a cada delta escalado e congela com pausa/hitstop. NÃ£o acompanha o transform do jogador apÃ³s lanÃ§ar. Troca de arma, revogaÃ§Ã£o de contexto/sessÃ£o/encontro, morte/disable, fim de run e cena cancelam futuros. TerminaÃ§Ã£o libera token de filho e lease visual. Vida prÃ³pria finita evita raiz retida indefinidamente.

SobrepeÃ§a angular/Ã¢mbar substituÃ­da por cuff de vento e vÃ³rtice claro de quatro LineRenderers, com espirais animadas pela idade escalada. Pool limitado pelo limite existente de raÃ­zes, geraÃ§Ã£o de lease evita que cleanup antigo desative instÃ¢ncia reutilizada. Objetos do projÃ©til sÃ£o world-space, sem collider/Rigidbody; apresentaÃ§Ã£o nÃ£o causa dano. OnDisable limpa e OnDestroy destrÃ³i objetos/material prÃ³prios. NÃ£o hÃ¡ mudanÃ§a permanente de prefab, rig ou cena.

AquisiÃ§Ã£o continua no catÃ¡logo/ticket existentes com mesmo ID/rank/polÃ­tica. O teste de oferta legÃ­tima usa rota de fixture, seed0/node5, sem grant da Mutation e com resoluÃ§Ã£o Ãºnica. Testes mecÃ¢nicos/visuais isolados usam aquisiÃ§Ã£o assistida explÃ­cita; nÃ£o representam frequÃªncia de oferta nem run fÃ­sica completa.

## Testes e evidÃªncias

| Seleção / evidência | Resultado real | Job |
|---|---|---|
| Red contra onda anterior |0/3, todos falharam pelo contrato novo ausente |4efbf72871cf4f7985742c5e34af3303 |
| Primeiros3 após implementação |3/3,4,340654s |0e70fd1fdec04e4087cb1119d7a90fe9 |
| Mecânica/catálogo/aquisição inicial |24/24,33,5163689s |434de621d83b4af48004c4a6d5df17ca |
| Captura independente |1/1,2,6244889s |bc2eff1080244693bec03d8868e8e7fa |
| EditMode grafo/catálogos/seed |90/90,114,6857402s |4b30bf2757f3424eb46e2d268dd40f82 |
| Focados finais, incluindo fixture do binding |29/29,41,7569338s;28 casos da Mutation e1 interação |a0ca7eec1c9f48668c0bcf4601e6b9ed |
| Regressão ampliada final |187/187,90,7447516s |02090688af2f415ea00dbd3b27cb386f |

**305 nomes distintos finais aprovados, zero ignorados**, com1 sobreposição (teste de interação) entre focados e regressão. Não é a suíte global. [Contagem verificável](../validation/artifacts/gameplay-rework/gpr-01-2b/test-summary.json). Zero ignorados nas seleções concluídas acima. [Focados finais](../validation/artifacts/gameplay-rework/gpr-01-2b/focused-final-pass.json), [EditMode](../validation/artifacts/gameplay-rework/gpr-01-2b/edit-final.json), [cenários reproduzíveis](../validation/artifacts/gameplay-rework/gpr-01-2b/REPRODUCAO.md). As tentativas intermediárias não são somadas ao total final.

O teste visual usa comandos automatizados pela API real de ataque; nÃ£o Ã© entrada fÃ­sica nem aprovaÃ§Ã£o humana. Capturas sÃ£o ScreenCapture do Game View real, sem ediÃ§Ã£o. CÃ¢mera/luz/alvo vermelho e montagem do rig pertencem Ã  fixture; cenÃ¡rio isolado, sem alterar a arena de produÃ§Ã£o. A cÃ¢mera facilita observar o corredor, mas nÃ£o certifica legibilidade sob todos os telegraphs de uma batalha real.

![Tornado em deslocamento](../validation/artifacts/gameplay-rework/gpr-01-2b/tornado-travel-a.png)

SequÃªncia: [equipado](../validation/artifacts/gameplay-rework/gpr-01-2b/tornado-equipped.png), [emissÃ£o apÃ³s whiff](../validation/artifacts/gameplay-rework/gpr-01-2b/tornado-emitted.png), [trajeto A](../validation/artifacts/gameplay-rework/gpr-01-2b/tornado-travel-a.png), [trajeto B](../validation/artifacts/gameplay-rework/gpr-01-2b/tornado-travel-b.png), [HP posterior](../validation/artifacts/gameplay-rework/gpr-01-2b/tornado-impact.png), [limpeza](../validation/artifacts/gameplay-rework/gpr-01-2b/tornado-cleanup.png). PosiÃ§Ã£o/tempo/HP/raÃ­zes em [travel.csv](../validation/artifacts/gameplay-rework/gpr-01-2b/travel.csv). PNG confirma aparÃªncia/posiÃ§Ã£o; HP Ã© comprovado pelo teste/CSV, nÃ£o por inferÃªncia da cÃ¡psula vermelha.

## RegressÃµes, limitaÃ§Ãµes e desempenho

A regressÃ£o inicial falhou **186/187**, especificamente `GPRSafetyContextualRHasOneOwnerAndCannotFallThroughOnClose`, linha594: painel nÃ£o abriu ao pressionar G. Reproduzida isoladamente **0/1**. DiagnÃ³stico direto encontrou binding atual de interaÃ§Ã£o **F**; a fixture presumiu G fixo. Corrigida somente a fixture de entrada: injeta binding configurado (teclado ou mouse) e mantÃ©m asserÃ§Ãµes, tempos e tolerÃ¢ncias; sem gravar/restaurar preferÃªncias Ã  forÃ§a nem alterar a produÃ§Ã£o. NÃ£o confundir essa correÃ§Ã£o demonstrada com os casos histÃ³ricos intermitentes de burn/hitstop/recovery, cujos clocks/asserÃ§Ãµes nÃ£o foram alterados.

Os trÃªs testes iniciais do novo contrato falharam contra a onda antiga, antes da implementaÃ§Ã£o; evidÃªncia red3/3 falhos preservada. Nova semÃ¢ntica substitui os testes incompatÃ­veis de onda/HP nativo, sem `Ignore` ou alteraÃ§Ã£o retroativa dos resultados histÃ³ricos. Texto integral dos fontes anteriores em previous-sources.json, alÃ©m dos resultados originais da fase anterior.

Duas queries NonAlloc por projÃ©til/passo, arrays e lista reutilizados; LOS ainda usa `Physics.RaycastAll` e pode alocar. Pool evita Instantiate/Destroy a cada emissÃ£o apÃ³s aquecer, mas o primeiro uso cria GameObjects/LineRenderers/material e cada trabalho ainda aloca um objeto. Quatro ribbons de48 pontos sÃ£o atualizados por frame visual. O teste de4FPS confirma sweep/distÃ¢ncia, nÃ£o um benchmark de carga. NÃ£o foi medido orÃ§amento de CPU/GPU/GC em8/32/64 inimigos: desempenho/legibilidade em swarms e rampas, ambiente complexo e aspecto final seguem para playtest/perfil. A trajetÃ³ria Ã© reta no plano comprometido, sem homing ou adaptaÃ§Ã£o ao relevo; piso inclinado pode bloquear a cÃ¡psula, sem atravessar sÃ³lidos. NÃ£o foi solicitado transformar isso em projÃ©til que segue NavMesh.

Os cenÃ¡rios automatizados cobrem contratos e fixtures reais, nÃ£o uma run humana completa. Acabamento artÃ­stico, percepÃ§Ã£o de alcance, impacto e sensaÃ§Ã£o transformadora continuam pendentes. O levantamento de seeds e a composiÃ§Ã£o com novos grants ficam fora desta fase.

## Arquivos e skills aplicadas

Fontes de produÃ§Ã£o revisados nesta atividade:

- Characters/Player/Actions/CharControlScript.BasicImpact.cs: captura nÃ£o crÃ­tica e contato que autoriza emissÃ£o.
- Characters/Combat/Protocols/ProtocolBudget.cs e ProtocolRuntime.Scope.cs: contrato estreito e reserva na raiz existente.
- Characters/Combat/Protocols/ProtocolRuntime.TitanicFists.cs: deslocamento, sweep, LOS, caps, damage queue e cleanup.
- Core/Run/TitanicFistsDefinition.cs: descriÃ§Ã£o, geometria/tempo e capability Projectile.
- Effects/TitanicFistsVisual.cs: cuff/vÃ³rtice/pool reversÃ­vel.

Caminhos acima relativos a `Assets/_Project/Scripts`. TitanicFistsPlayModeTests.cs atualizado preservando os trÃªs testes de aquisiÃ§Ã£o/catÃ¡logo aplicÃ¡veis; TitanicTornadoPlayModeTests.cs e meta novos. DirectMovementIntegrationPlayModeTests.cs teve apenas correÃ§Ã£o da fixture do binding configurado, preservando as mudanÃ§as anteriores. Novo cliente Tools/GameplayObservation/gpr_01_2b_mcp.py e evidÃªncias isoladas em gpr-01-2b. Atualizados requisitos, design tÃ©cnico, tasks, fatia, matriz, progresso e avisos de estado; este relatÃ³rio e autorizaÃ§Ã£o arquivada sÃ£o novos. Demais alteraÃ§Ãµes vistas no Git pertencem Ã  entrega anterior e foram preservadas.

Skills locais realmente lidas/aplicadas: **tech-guy-game-vision** (transformaÃ§Ã£o expressiva coerente com a identidade), **tech-guy-combat-unity** (contato/compromisso/cancel/GI), **tech-guy-boon-design** (ID/capabilities/recompensa/root/grants) e **tech-guy-unity-acceptance** (prova real versus aceite fÃ­sico), em `.agents/skills/`.

ColeÃ§Ã£o instalada: **router**, **unity-csharp-scripting**, **unity-scriptableobjects**, **game-feel**, **unity-mcp-orchestrator** reutilizadas das leituras desta conversa; **unity-physics** carregada nesta revisÃ£o para sweep/colisÃµes. ExemplosUnity6.3 adaptados ao Editor6000.5.10f1 real, sem atualizar packages. **computer-use** carregada apenas para diagnosticar janela sem resposta; nÃ£o houve playtest fÃ­sico por essa ferramenta. Nenhuma skill de outro engine foi apresentada como aplicada; nenhuma skill nova foi instalada. Skills genÃ©ricas nÃ£o substituem os contratos locais. Nenhuma delegaÃ§Ã£o/subagente usado.

## Integridade e encerramento

Inventário inicial de2459 arquivos: **2451 iguais, 8 fontes intencionalmente alterados, zero ausentes**. Nenhuma cena, prefab, asset, meta/configuração preexistente ou evidência histórica mudou em relação ao início desta atividade. 89 artefatos do experimentoGPR-01.2 incluídos nessa comparação e preservados. O CSV recovery-normal que já estava modificado também mantém seu hash inicial; não foi restaurado deHEAD. Novo fonte de teste e meta importados; zero GUID duplicado em Assets/_Project. [Integridade](../validation/artifacts/gameplay-rework/gpr-01-2b/integrity-final.json), [preservação histórica](../validation/artifacts/gameplay-rework/gpr-01-2b/historical-preservation.json), [GUIDs](../validation/artifacts/gameplay-rework/gpr-01-2b/guid-scan.json).

Editor final: NexusBlockout limpa/Edit Mode, compilação sem falhas, zero missing scripts, zero dispositivosGPR/Titanic e zero objetos de pool do tornado remanescentes. Tag temporáriaSessionState removida. Console nativa `GetCountsByType`: **0 erros,526 avisos,429 logs**, sem Clear manual. Avisos amostrados de fixtures com câmera/dash/click/Animator ausentes e keys legadas, sem confundir com assets publicados. `read_console` classificou como Exception um Debug.Log de ChargedShot; divergência do plugin preservada, não erro confirmado no contador nativo. Os erros de compilação intermediários foram corrigidos e continuam registrados em artefato, sem esconder a tentativa. [Estado final](../validation/artifacts/gameplay-rework/gpr-01-2b/editor-final.json), [filtro error do plugin](../validation/artifacts/gameplay-rework/gpr-01-2b/console-error.json), [avisos](../validation/artifacts/gameplay-rework/gpr-01-2b/console-warning.json).

Os três casos intermitentes históricos de burn/hitstop/recovery passaram na bateria atual, inclusive `MovementDuringRecoveryKeepsTheCast_UsesLatestValidOrderOnce`. Nenhuma causa temporal histórica foi demonstrada corrigida por esta entrega; não se declara encerrada a pendência apenas por esses PASS.

O runtime novo nÃ£o escreve PlayerPrefs, save ou progresso persistente. Fixtures existentes restauram chaves sob seu escopo; o bindingF encontrado foi preservado. NÃ£o houve baseline independente universal de todos os saves/PlayerPrefs nem teste de save/resume da run; isso permanece uma limitaÃ§Ã£o explÃ­cita.

## RecomendaÃ§Ã£o e prÃ³ximo gate

Recomendo **revisÃ£o fÃ­sica da GPR-01.2b pelo desenvolvedor**, verificando whiff terminal, direÃ§Ã£o travada, leitura do vento, corredor, parede, pausas e retorno apÃ³s troca. NÃ£o declarar final o design sÃ³ por ter passado testes tÃ©cnicos. GPR-01.3 sÃ³ deve comeÃ§ar apÃ³s essa avaliaÃ§Ã£o e nova autorizaÃ§Ã£o: investigar tornadoâ†’Conductor com quota/linhagem prÃ³pria, sem ativar Detonation ou relaxar polÃ­tica global. Nada dessa composiÃ§Ã£o foi implementado agora.

## Salvamento Git solicitado no encerramento

Após o pedido adicional do desenvolvedor nesta conversa, o encerramento inclui commit e push de **todas as alterações presentes**, inclusive a entrega01.2 anterior e o CSV preexistente, não apenas os arquivos desta revisão. O hash e a confirmação remota são informados na resposta final. Desligamento somente após confirmar o push. Não há início de01.3 ou alteração de escopo por esse salvamento.
