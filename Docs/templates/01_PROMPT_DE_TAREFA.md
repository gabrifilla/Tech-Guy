# Prompt reutilizável para tarefa — copie e preencha

```text
SISTEMA: [manopla | arco | lança | inimigos | boons | HUD | run | arte]
OBJETIVO: [uma mudança observável pelo jogador]
MOTIVAÇÃO: [problema visto no vídeo/playtest]
CENÁRIO REPRODUZÍVEL: [cena/arma/inimigo/seed/camera]
COMPORTAMENTO ATUAL: [o que acontece hoje; indicar se é observação ou suposição]
COMPORTAMENTO ESPERADO: [input → resultado e exceções]
NÃO OBJETIVOS: [o que NÃO mudar: arma/recursos/arquitetura/balance...]
REFERÊNCIA: [link/screenshot + exata propriedade a aproveitar, se houver]
CRITÉRIOS DE ACEITE: [testes, vídeo, console, números quando disponíveis]

Leia AGENTS.md e docs canônicos do sistema antes de qualquer alteração.
Fase 1: AUDITE sem editar; descreva classes/cenas/prefabs existentes, dependências,
risco de duplicação e plano de alteração mínima. Se achar conflito importante, pergunte.
Fase 2: implemente apenas a fatia acordada; não antecipe outros sistemas.
Fase 3: valide com Unity MCP conectado quando suportado, testes relevantes,
vídeo/screenshot e evidências. Distinguir teste automático e aprovação humana.
Fase 4: reporte diff, comportamento entregue, evidências, regressões e pendências.
Nunca declare DONE se os gates obrigatórios não foram verificados.
```
