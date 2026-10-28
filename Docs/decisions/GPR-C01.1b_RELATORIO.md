# GPR-C01.1b — Design da migração e prontidão

- Data: 26/10/2026, America/Sao_Paulo.
- Fase/atividade: GPR-C01 / GPR-C01.1b.
- Status: **AWAITING_APPROVAL** para o próximo gate; elaboração do desenho concluída.
- Evidência: design PROPOSTO; testes técnicos limitados aos filtros registrados. Sem aceite de feel ou nova implementação.
- Projeto: D:/Dev/Personal/My_games/Tech-Guy, Unity 6000.5.10f1, MCP Tech-Guy@fccafb2b47cd7671; gameplayOverhaul/HEAD3a3f0ee + alterações locais. Autorização recebida limita a C01.1a/b, não à migração do player.

## Objetivo e realizado

Reconciliar a direção aprovada com a baseline e preparar passos mínimos, alternativas, regressões e decisões que faltam. Revisados os documentos canônicos do pacote, sem criar uma arquitetura concorrente: [controles/migração](../gameplay/GPR_CONTROL_MIGRATION.md), requisitos, design, tarefas e plano de validação; ADR-0006 recebeu seção vigente e preservou propostas antigas como histórico superado. GPR-00 agora tem aprovação documental condicional registrada, não aprovação dos parâmetros GPR-01.

O novo desenho especifica um clique LMB→um pedido, RMB→especial/Asura, quatro slots Q/E/R/X, um só leitor/driver e uma autoridade de movimento. Descreve adapter no holder para preservar Asura/recursos/cooldown/continuação/F/causa R; não chama executor direto ou duplica Asura em X. Detalha o acoplamento Array.IndexOf/index3 e o risco de resolver como Basic ao retirar a referência. X vazio inicial é recomendação, não decisão aplicada.

NavMeshAgent direto continua candidato condicionado a protótipo isolado; há critérios e alternativa CharacterController com autoridade única se falhar. Referencial e direção de dash continuam a comparar. Hold legado foi demonstrado e não virou padrão final; ChargedShot/Bow e especial de Bow/Spear têm gates próprios. Rebind possui plano de piloto EditMode em memória antes de backend, preview de custom F11, conflitos, versionamento, draft e soltura; nenhum Settings novo foi construído.

## Skills e arquivos

Aplicadas as mesmas quatro skills locais e nove instaladas da C01.1a, com ênfase em input-systems/unity-input-system (ações/edge/migração), unity-csharp-scripting/animation/navmesh (ownership/tempo/pose), game-feel/game-ui-ux (compromisso/foco), acceptance/MCP (evidência). Nenhuma selecionada ausente; referências Unity6000.3 adaptadas ao projeto6000.5.10f1 e pacotes atuais.

Modificados: `Docs/decisions/ADR-0006-GPR-DIRECAO-E-GATES.md`, `GAMEPLAY_REWORK_PROGRESS.md`, `GPR-00_PHASE_SUMMARY.md`; `Docs/gameplay/GPR_CONTROL_MIGRATION.md`, `GPR_REQUIREMENTS.md`, `GPR_TECHNICAL_DESIGN.md`, `GPR_TASKS.md`, `GPR_VALIDATION_PLAN.md`, `GPR_OWNERSHIP.md`, `README.md`; índices/prioridades/matriz canônicos recebem links/estado vigente. Criados este relatório, `GPR-C01.1_BASELINE_E_PRONTIDAO.md` e evidência de regressão Bow/Spear/QA final na pasta c01-baseline. Lista exata/hashes no manifesto final da atividade.

## Validação e limites

| Checagem | Resultado | Evidência/limite |
|---|---|---|
| Compatibilidade do design com baseline | PASS documental | Owners reais, acoplamentos e decisões pendentes explícitos; sem validar API futura por leitura |
| Baseline de C01.1a | 21/21 PlayMode +15/15 EditMode | Dois jobs separados; input virtual, API e imagens atuais, sem humano |
| Regressores adicionais Bow/Spear | **4/4 PASS** no JSON `testes-bow-spear-4.json` | Job d825ebde8d0943a09f3243c69e11fdd0; alcance/carga/movimento/retorno, não especiais novos |
| Rebind novo/motor WASD/RMB novo | NOT RUN | Sem implementação autorizada; piloto de migração e protótipo são futuros |
| Legibilidade/feel físico do novo esquema | NOT RUN | Nenhum comentário humano inventado |

Console, prefs/summary/hashes e estado final checados no consolidado; não limpar warnings nem restaurar progresso antigo. Não alterar inimigos/mapas, reações, timings ou conteúdo para obter teste verde. Os 4 testes adicionais exercitam fixtures existentes com recursos de teste, não provam uma run natural de Bow/Spear.

## Riscos e decisões ainda exigidas

D15 conteúdo inicial X e adapter do especial; D16 hold/ChargedShot; D17 especiais Bow/Spear; D18 dash; Interact/inspeção compacta e referência câmera/mundo; aprovação da implementação e UX de migração de preferências. Input Actions+facade e Agent.Move são recomendações condicionais, não aceites tácitos. GPR-01/D04–09 continuam sem aprovação de política, conteúdo ou números.

## Passagem

C01.1a concluída tecnicamente; C01.1b concluída como elaboração e aguardando aprovação. A fase GPR-C01 inteira **não** está concluída. Próxima tarefa recomendada: **C01.2a, protótipo isolado do motor**, somente após nova autorização. Sem integração no player publicado antes de seu gate; C01.3/4 exigem decisões adicionais e GPR-01 permanece fora. STOP aplicado conforme pedido do desenvolvedor.

Fechamento exato de arquivos e validação documental: [manifesto](../validation/artifacts/gameplay-rework/c01-baseline/manifesto-arquivos.json) / [QA](../validation/artifacts/gameplay-rework/c01-baseline/qa-final.json). Inclui atualização de Docs/systems/01_COMBATE.md para distinguir direção aprovada de runtime legado.1778 arquivos preexistentes iguais;546 exclusões mantidas;preferências/summary consultados iguais;consulta de erros vazia e Editor NexusBlockout limpa/EditMode. Nenhum novo controle implementado.
