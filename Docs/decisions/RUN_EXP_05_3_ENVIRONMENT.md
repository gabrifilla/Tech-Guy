# RUN-EXP-05.3 — ambiente de arquivo estratificado

02/11/2026. Objetivo: tornar a sala com coberturas reconhecível sem alterar seu jogo aprovado. A auditoria05.0 encontrou caixas funcionais, piso/trilhas04 e pouca construção ambiental. O alvo delegado foi uma câmara de arquivo: coberturas como cartuchos laminados e paredes como infraestrutura de serviço. Direção de trabalho **PENDING DEVELOPER APPROVAL**.

## Construção e limites

Fonte real emBlender5.2.2LTS pelo MCP conectado, `Tools/RunExp05/author_environment.py` e `Source/Archive_ArtSource.blend`. Dois módulos originais com bevel/UVs reaproveitam o atlas cerâmica/grafite/cobre dos inimigos: ArchiveCover3780 triângulos e ArchiveServiceRack2292. Fontes exportadas comoFBX, importadas naUnity e assadas emMeshassets com normais/tangentes/UVs.0 arestas não manifold nas duas fontes. Não são apenas novas cores ou faixas sobre a mesma caixa: há núcleo estrutural, cartuchos interligados, sockets rebaixados, moldura e racks de serviço. Ainda são um recorte modular intermediário, não toda a produção de ambientes.

`FirstSectorEnvironmentArt` é uma camada render opcional por arenaCombat. Troca apenas oMesh das duas coberturas e materiais; racks são combinados numMesh por arena, sem colliders. O miolo visual das paredes recua dentro do volume físico existente para expor os módulos. A borda visual fica até cerca de0,11m para dentro da colisão lateral da parede; não cria área caminhável nova. Altura/colisão real continuam4m. Piso quieto original em `author_floor.py`, baixa variação, juntas/buchas sem marcas de ameaça. URP/Lit e iluminação atuais, sem nova luz, shader global, bloom, animação de cenário ouVFX ofensivo.

Criados materiais/meshes/textura em `Art/RunExp05/Environment`; authoring `RunExp05EnvironmentAuthoring.BuildAndIntegrate()` preenche cinco referências opcionais noContent. `ManualStageEnvironment.Procedural` cria a camada depois da apresentação04, somenteCombat. Nenhum peso, seed, nó, spawn, chance de cobertura, BoxCollider, NavMeshModifier, bake PhysicsColliders ou porta mudou. O ambiente aberto recebe só a infraestrutura e piso correspondentes. Salas especiais/corredores não foram redesenhados.

Comparação04/05 usa `SetVisible(false/true)` nessa camada, guardando referências04 e liberando somente meshes de runtime que a própria camada criou. A apresentação04 continua disponível; não há destruição de assets emprestados.

## Alternativas, custo e verificação

Rejeitados props no centro e mudanças das massas: prejudicariam rotas/LOS e ampliariam escopo. Rejeitada identidade por neons/chão de alto contraste: concorreria com antecipações. A escolha acrescenta geometria estática de racks, compensada por combinação numa submissão de material por arena. Não chamar isso de ganho de desempenho; custo medido05.4. Texturas compartilhadas poupam materiais mas repetem detalhes; há trabalho artístico futuro de densidade, desgaste dirigido eLOD se a medição justificar.

UnityMCP confirmou importação, compilação eContent persistido GUID25d2c5770d69f034092d3478280990f8. Primeiro job `c7822075a0bc4dee9e8d8ae0f49a352d`: **1/1PlayMode,15,414s**, zero falhas. Duas seeds0/63: mesma pose/câmera/luz por comparação, contagem/bounds de colliders idênticos nos dois estados, raio de cobertura e duas rotasNavMesh completas. Com63, AI real, Emissor bloqueado/reaquisição deLOS e deslocamento real do jogador pela rota lateral. Imunidade apenas nessa fixture visual; não é sobrevivência humana. Nenhuma asserção enfraquecida.

Capturas `05-room-{open,cover}-{before,after}-normal-camera`, circulação do jogador, fases reais Fragmento/Emissor, bloqueio/reentradaLOS emdestinos03 únicos, reunidas no pacote05. Imagem de sala coberta após foi aberta: inimigos eHUD distinguíveis; coberturas mostram cartuchos, paredes mostram estrutura, piso permanece quieto. A primeira inspeçãoBlender enquadrou módulos sobrepostos ao guia do inimigo (ambos eram visíveis); não é apresentada como composição final do ambiente.

Skills aplicadas: quatroTechGuy locais/refs para identidade/autoridade/aceite, create-game-assets/three-d/art-direction/provenance paraDCC/importação, tech-guy-level-layout/level-design para circulação/leitura, unity-mcp/C#/NavMesh para física e verificação, performance-optimization para medir sem otimização global. Nenhuma instalação de skillDCC dedicada foi inventada.

Teste adicional executará Q/E/R, especial e dash das três armas nessa sala, com recursos explícitos de fixture eAI ativa; atribuiçãoHP/colisão por arma são verificadas em seleções próprias. Resultados finais, integridade de2058arquivos/32prefs e custo constam05.4. Aparência, telegraphs empressão e qualidade percebida continuam **PENDING DEVELOPER APPROVAL**; aprovação técnica não substitui esse gate.
