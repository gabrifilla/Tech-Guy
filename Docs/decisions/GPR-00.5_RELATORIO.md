# GPR-00.5 — Input, movimento e migração

- Data26/10/2026, America/Sao_Paulo; fase GPR-00/atividade00.5.
- Status COMPLETED (desenho); aprovação AWAITING_HUMAN_APPROVAL. Evidência NOT_APPLICABLE para proposta.
- Ambiente/revisão/autoridade: [00.1](GPR-00.1_RELATORIO.md), sem autorização runtime.

## Objetivo e aceite

Inspecionar controles reais, agente/câmera/rotação/dash/hold/contextos/UI/rebind; produzir matriz estável com conflitos, alternativas e tarefas testáveis. Critérios satisfeitos por [GPR_CONTROL_MIGRATION.md](../gameplay/GPR_CONTROL_MIGRATION.md), integrado às tarefas/design/QA da00.4. Dados ausentes e hipóteses estão identificados.

## Realizado e arquivos

Criados documento de migração e este relatório; ADR/pacote/ledger/matriz atualizados. Campo do agente e root motion lidos no Editor em EditMode; sem alterar propriedades. Contrato proposto preserva índices0–3, separa Interact/FinishSequence e move apenas defaults para QERX. Define RMB terminal mais largo/curto como proposta de tuning explícita, mantendo timing/HP/reação. Define alternativa de backend e exclusão de leitores duplicados, migração de F11/custom e UI com draft/conflitos/soltura.

Skills aplicadas: router (seleção já feita), unity-csharp-scripting, unity-input-system e referência rebinding, input-systems, unity-navmesh, unity-animation, game-feel, game-ui-ux, unity-mcp-orchestrator e quatro skills locais. Uso orientou ownership/gates; não foi aplicada receita de controlador nova em produção.

## Evidência e limitações

| Checagem | Resultado | Limite |
|---|---|---|
| Bindings reais | Q/W/E/F11 +F | Leitura atual do Editor; novo mapa ainda não existe |
| Movimento/Animator | speed5/acc99999/angular0/radius0,5/rootMotionfalse | Campos em NexusBlockout EditMode; não são métricas de gameplay |
| Fonte click/aim/dash/gates | PASS estático | Matriz cita owners e distingue vetores de destinos |
| 15 EditMode +4 PlayMode | PASS nos filtros registrados | Sem suíte global, sem input físico novo |
| WASD/combos/rebind propostos | NOT RUN | Sem implementação; testes e baseline físico/visual em C01.1 |

Sem screenshots/vídeos novos inspecionados ou playtest humano nesta fase. Nenhuma medição nova de latência/CPU/GPU/feel. Gamepad não certificado. Parameters de varredura são propostos, jamais apresentados como aprovados. Falhas históricas de Mutations/BurnTick/hold não foram reexecutadas.

Complementos de fechamento: `Docs/decisions/GPR-00_PHASE_SUMMARY.md`, `Docs/gameplay/README.md` e cópia integral do plano WASD; acréscimos em `Docs/README.md`, `Docs/03_PRIORIDADES.md` e matriz canônica. Manifesto exato dos arquivos/evidências e limites de cobertura no consolidado. QA final checou15 documentos/55 links e preferências consultadas iguais;1597 hashes de fontes/config/skills/metas sem mudança e510 exclusões Git preservadas. Editor NexusBlockout limpa/EditMode;0 erros na consulta final, warnings de fixtures mantidos. Conclusão técnica limitada aos filtros executados.

## Passagem e gate

Atividade documental concluída; consolidar relatório de fase e integridade final, então **STOP**. Próxima atividade recomendada somente após aprovação: GPR-C01.1a. Não iniciar C01 nem01 com este relatório. Aprovação necessária vem do pedido explícito de parar após GPR-00; ADRD01–14 reúne o resultado concreto a revisar.
