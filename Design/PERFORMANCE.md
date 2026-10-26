# Memória e desempenho do HIT

Revisão de 26/10/2026. As funções de áudio, letras, visualizador, relógios,
Pomodoro, temas, fixação, M2 e bandeja permanecem disponíveis.

## Mudanças

- CSS: HitThemeService mantém somente a base e o tema ativo em cache. Verifica
  tamanho, última escrita e criação do arquivo antes de reler/gerar SHA-256.
  Edições normais continuam sendo percebidas pelo monitor de um segundo,
  incluindo alterações com o mesmo tamanho. Seleção padrão libera a cópia do
  tema pessoal anterior. Erros continuam preservando o último CSS válido no JS.
- Aplicação de temas: atualizações simultâneas são agrupadas. O seletor conserva
  sua lista enquanto os nomes não mudam, e só renderiza novamente quando há
  alteração de opções, confirmação ou erro. O monitor de RAM para com a janela
  fora de vista; a medição exibida continua sendo a memória residente privada
  do processo HIT, conforme o contrato existente.
- Letras: cache com até 40 resultados e orçamento estimado de 4 MiB, incluindo
  UTF-16, linhas, referências e metadados. Entradas vencidas são descartadas;
  resultados grandes continuam sendo retornados completos, sem obrigar sua
  retenção no cache. O limite de 3.000 linhas LRC/5.000 timestamps continua
  aplicado. A leitura JSON evita uma cópia completa do corpo da resposta.
- Renderização das letras: mudanças de posição dentro do mesmo verso não
  reconstroem centenas de botões e closures. Mudança de verso, faixa, estado de
  comandos, carregamento e erro continuam atualizando a interface. Fora de
  vista, a árvore de leitura e seus observers são desmontados, preservando o
  modelo da letra e o modo escolhido para retomada imediata.
- Capas: ajustes de blur/aparência reutilizam a imagem que já está no WebView,
  sem reenviar a mesma base64 pelo bridge. Fora de vista, sem paleta global,
  o componente, o serviço de mídia e o JS liberam suas referências à capa;
  a miniatura volta a ser solicitada ao player local quando necessária.
  A qualidade/resolução das imagens não é reduzida. Capa/paleta global continuam
  acompanhando a faixa, inclusive fora do Áudio, quando habilitadas.
- Recursos JS: importação de módulos assíncronos é compartilhada por instância;
  módulos/observers são desconectados no descarte. Imagens temporárias usadas
  para extrair cores são soltas após a análise. Tooltips examinam os elementos
  alterados/adicionados, sem pesquisar toda a interface em cada mutação.
- Fusos: catálogo completo carregado apenas quando um seletor é aberto.
  As seções recolhidas montam somente nome, horário e seta; os seletores são
  criados na edição. Há cache limitado para os fusos usados, sem carregar todos
  os objetos TimeZoneInfo na inicialização das preferências.
- Visualizador: mantém 32 bandas e 20 quadros/s, com um buffer reutilizado por
  sessão. A saída nativa é liberada também se a criação da captura falhar.

Não há GC forçado, esvaziamento artificial do working set, redução da qualidade
visual ou encerramento de processos do WebView2 como política de produção.
Prazos e avisos continuam sendo processados pelo serviço independente do relógio.

## Medições controladas

Build Release, .NET 8/Windows, mesma máquina e mesmos dados antes/depois.
Ensaios locais em obj/performance-tests, com arquivos/perfis descartáveis.

| Cenário | Antes | Depois |
| --- | ---: | ---: |
| Alocações em 1.000 consultas de base + OLED inalterados | 153,52 MiB | 0,60 MiB |
| Alocações em 100 consultas com CSS pessoal de 512 KiB | 256,73 MiB | 0,072 MiB |
| Alocações em 100 posições dentro do mesmo verso (300 versos) | 12,52 MiB | 0,051 MiB |
| Renderizações dessas 100 posições | 100 | 0 |
| Opções de fuso no HTML de dois relógios recolhidos | 435 | 0 |
| HTML dessas configurações | 40.820 bytes | 2.654 bytes |
| Heap retido pelo cache após 40 letras extensas sintéticas | 60,02 MiB | 3,97 MiB |

Alocações usam GC.GetAllocatedBytesForCurrentThread nos trechos síncronos e
GC.GetTotalAllocatedBytes nos testes de renderização. A retenção do cache foi
comparada com GC.GetTotalMemory após coleta, somente no ensaio. Cada letra
sintética tem 2.800 versos longos; esse caso mede a proteção contra crescimento,
sem representar o tamanho habitual de uma música.

Alocação acumulada e heap do cache não equivalem à RAM total residente do
aplicativo. O processo HIT não estava ativo no início para uma comparação de
working set em uso real. WebView2, imagens decodificadas, JIT e estado da faixa
contribuem para o total; compare a mesma versão Release, mesma faixa/capa,
mesmos menus/temas e mesmo tempo de aquecimento ao medir no Gerenciador.

## Validação

Foram exercitados reprodução, comandos, seek, letras sincronizadas, fontes
alternativas, retomada das letras, renderização apenas ao mudar verso, limites
do cache, descarte da capa oculta, reutilização de imagem em ajustes, FFT e
suspensão de captura. Também: persistência/aviso dos timers, fusos, compacto,
M1/arraste/M2, importação/edição ao vivo dos temas, exclusão protegida, tooltips,
paleta/capa nas letras e restauração dos três temas embutidos.
