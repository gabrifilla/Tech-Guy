# RUN-EXP-05.2 — autoriaDCC e integração dos dois candidatos

02/11/2026, horário do cliente. **DONE**: pipelineDCC executado, fontes editáveis, meshes/materiais importados, cópias jogáveis e verificações descritas. **PENDING DEVELOPER APPROVAL**: qualidade artística e sensação. Não declarar o conceito ilustrado como modelo final, nem o passeEmissor como finalização equivalente ao alvo prioritário.

## Diagnóstico, alvo e alternativas

Fragmento04 preservava identidade funcional, mas tinha conexões simples, superfícies sem UV específico e leitura de peças facetadas independentes. Alvo05: cobertura dorsal mais contínua, cabeça/mandíbulas deliberadas, joelho/apoio legíveis, material cerâmico sobre estrutura grafite com encaixes cobre. Emissor mantém anatomia suspensa/conchas/canal, recebendo passe secundário de limpeza, bevel, UV e materiais coerentes. Não copiar um personagem ou adicionar adereços aleatórios para chamar de evolução.

Rejeitados: trocar a silhueta aceita, aumentar a quantidade de primitivasUnity, usar um modelo externo sem procedência, reconstruirAnimator ou retimar contato. Mantido o rig rígido/IK existente; uma armature humana não resolveria estes mecanismos. A fonteDCC é editável por mesh e guia montado; não há alegação de retopologia/skin/animclips finais.

## Ferramentas efetivamente usadas

BlenderMCP inicialmente não conectava; CLI instalado5.2.2LTS foi testado. Após o desenvolvedor informar porta9876, MCP conectou aoBlender5.2.2. Addon1.8/protocolo13 contra expectativa15; execute_code/get_scene_info/look funcionaram, sem atualizar addon ou alterar opções de privacidade. Bibliotecas/geradores externos estavam desligados; não usadosTripo, Meshy ou assets arbitrários.

Cena inicialBlender sem arquivo salvo:Cube/Camera/Light. Preservada integralmente; trabalho emnova cenaTechGuy_RUNEXP05, coleções próprias e cópias.blend foraAssets. Shadernodes consultados por tipo. OperaçõesMCP executaram o script rastreável `Tools/RunExp05/author_characters.py`, com revisão visual porlook. Fontes `Fragmento_ArtSource.blend`/`Emissor_ArtSource.blend` e contratosJSON emTools/RunExp05/Source; FBX reais emArt/RunExp05/Enemies, não uma alegação de exportação.

Calibração explícitaBlender→Unity identificou espelhamentoX no primeiro experimento; conversão finalUnity(x,y,z)→Blender(-x,-z,y) preservou os três eixos e escala1 no retorno. Evidênciasfbx-axis-calibration*.json. Guia montado foi corrigido para remover somente posição/rotação de cena do prefab, conservando escala nativa; isso não alterou o rig de jogo. Pivôs de cadaMesh foram mantidos no espaço local contratado.

## Autoria e importação

Oito meshes únicosFragmento, reutilizados em18 renderers nativos: corpo em perfis de superfície com quads/bevel, cabeça/face, coxas/joelhos/apoios, mandíbulas e três escudos dorsais num mesh. A primeira inspeção mostrou cobertura dorsal estreita deixando um grande volume exposto; uma revisão de cobertura alargou os três escudos existentes dentro do envelope lateral do torso. Nenhuma nova pata/renderer. Material e textura reforçam articulação, não apenas cor de um prefab antigo.

Emissor: sete meshes nativos limpos/soldados/chanfrados eUVs; canal oco e câmara preservados. O passe secundário conserva substancialmente sua geometria04, explicitamente. As duas criaturas compartilham a linguagem sem compartilhar anatomia. Não apresentar esse passe como remodelagem completa.

SmartUV com margem e remapeamento em atlas de trim deliberadamente compartilhado; faces materiais foram consolidadas num slot por mesh após UV, evitando multiplicar drawcalls por oito famílias. Texturas1024: cor, metálico/suavidade empacotados e normal de gravuras discretas. Mapas de cor sem iluminação pintada; não chamar isso de pintura manual final. Canalcor sRGB, mapas de dados/normal lineares, mipmaps e compressãoHQ, ReadWrite desligado paratexturas/FBX; meshes nativos derivados permanecem legíveis para validação/uso atual. ShaderURP/Lit existente, sem feature/outline/bloom/luz global novos.

ImportadorEditor `RunExp05EnemyArtAuthoring` consome FBX e cria meshes nativos/assets/prefabs copiados04. Hierarquia/localtransforms/colliders/components/arquétipos intactos. Fonte04 e seusGUIDs permanecem. Cópias05:FragmentoGUIDaf5d41ab4bac433419d3d4399f164eca;EmissorGUIDb47ff7d4f586a5b4abd96e57dbfaf617. Fragmento5772triângulos/18renderers/1material;Emissor2172/7/2. SemSkinnedMeshRenderer novo. Dados e fontes emenemy-art-import.json e manifestosDCC adjacentes aos.blend.

FirstSectorEnemyPose ganhou flag opcional_laminatedPresentation, false emrevisões antigas e true nas cópias05: bank visual de giro até3,5° no corpo ou40% disso nas conchas. Easing e reset porEnable, suspensão porpausa/hitstop, sem alterar tempo de ataque, emissões, dano, capsule, navegação ou alvo. ApoioIK, flinch, antecipação/impacto/recovery e desagregação permanecem dos mesmos owners. Materiais/novas superfícies tornam essas fases mais distintas; não foram criadas novas ações.

## Unity, testes e falhas de evidência

UnityMCP no projetoD:/Dev/Personal/My_games/Tech-Guy6000.5.10f1 confirmouimportação, assets persistidos e0missing nos candidatos. JobEdit `e920d69f1a4849aa9dbd758bacf158ce`:1/1,0,671s. Verifica dano/cadência/windup, arquétipo, nav/cápsula, cada pivô,18/7 renderers, slotúnico, UV/normal/tangentes/valores finitos evolume positivo; sem alterar a asserção04.

Play `5675aa08bf01495caf1f0793d538495a`:1/1,10,406s. Par05 controlado pelo diretor real; apresentação neutra comAI inicialmente pausada, mesma pose/câmera/luz nos pares04/05 eclose-up semHUD; depoisAI/dano/nav/fasesativos. Apoios<4cm de drift, preparação/ataque de ambos, hit real porHP, morte/despawn/cleanup passaram. Jogador imune somente nesta comparação. Roster controlado e morte automatizada não provam obtenção natural/sobrevivência humana.

Apesar do verde mecânico, primeiras imagens mostraram uma silhueta ciano temporária, inadequada como prova visual. Nova execução `d0b42145bf444eb3beea82e8ba76446a`:1/1,9,952s, mostrou materiais corretos **sem mudança neles**. Shaderdiagnostic não registrouerro. Comportamento compatível com primeiro aquecimento de variantes doEditor; origem exata inicial não foi retrospectivamente provada. A fixture passou a compilar/aguardar passes antes de capturar. Imagens iniciais/diagnóstico preservados e não apresentadas como arte final. A integração final repetirá o caso com essa prevenção.

PNG05-art-*-neutral-closeup,normal-camera,phase,hit e disassembly emdestinos únicos03 da execução, copiados ao pacote05 no fechamento. Poses/timing atuais compartilhados nos pares isolam geometria/material; não fingir reconstruir o movimento antigo. Capturas corretas foram abertas/inspecionadas; cabeça, quatro apoios e conchas permanecem distinguíveis. A aprovação visual final é humana.

## Skills, limites e gate

Aplicadas quatroTechGuy locais/refs (autoridade de combate/aceite e identidade), create-game-assets/three-d/art-direction/provenance (fonteDCC/UV/importação/direitos), unity-mcp/C#/animation/NavMesh (pivôs, eventos nativos e verificação), game-feel (peso semretiming), performance-optimization (contar custo antes deotimizar). Nenhuma skill dedicada derig/modelagem foi encontrada; isso foi documentado, não substituído por uma instalação inventada. BlenderMCPpassou a estar disponível, superando a limitação inicial05.0.

**NOT DONE**: skin deformável/rig final/clipsDCC, pintura exclusiva porparte com densidade final/atlas de produção completo eLOD dedicado. LOD pode ser útil após a medição; não o introduzir especulativamente na câmera fixa. Normais/gravuras e acabamento ainda são intermediários; conceito ilustrado tem complexidade maior que o modelo entregue. Parecer artístico/footcontact percebido e força da derrota não são aprovados por1/1.

Integração reversível somente nas duas referênciasFirstSectorContent, depois dos contratos e inspeção correta. Performance8/32/64 e preservação2058hashes/32prefs atuais serão encerradas05.4. Nenhum stat/economia, cena de produção, novo arquétipo ou efeito ofensivo foi criado. Gate técnico de pipeline e compatibilidade permite o recorte deambiente; gate artístico permanece **PENDING DEVELOPER APPROVAL**.
