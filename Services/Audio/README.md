# Áudio do HIT

## Reprodução

WindowsMediaService usa GlobalSystemMediaTransportControlsSessionManager.
Prioriza a sessão atual se estiver tocando; caso contrário, outra sessão ativa,
ou a sessão atual/primeira disponível. Mostra capa, título, artista e origem.

Play/pause, anterior, próxima, seek, repetição e aleatório usam os comandos
da sessão e respeitam as capacidades anunciadas pelo aplicativo.
Comandos de uma geração antiga são rejeitados quando a sessão/faixa muda.
A timeline usa a posição e a data da última atualização do Windows.
Arrastar só mostra uma prévia; seek é enviado ao concluir a alteração.

Isso não identifica músicas pelo som. Apps/jogos que não publicam uma sessão
de mídia não fornecem metadados ou controles. O suporte a seek, loop e
aleatório depende do aplicativo, mesmo quando a faixa aparece no Windows.

## Letras

LyricsService busca conteúdo com timestamps reais, nesta ordem:

1. LRCLIB: consulta exata e, se necessário, busca por título/artista.
2. SyncLRC: endpoint público de letras sincronizadas.
3. NetEase Music: busca de faixa e obtenção do LRC correspondente.

As alternativas com busca validam título, artista e duração, para evitar a
letra de outra faixa/versão. Títulos podem remover sufixos como Official Video
e o prefixo redundante do artista.
Cada requisição tem limite de seis segundos e 1 MB. Cache de até 40 faixas:
acertos por uma hora; ausência por dois minutos. O uso do SyncLRC é limitado
localmente abaixo do teto público de 30 requisições por minuto.

Só letras temporizadas habilitam o botão: texto simples não recebe sincronização
inventada. O parser aceita vários timestamps por linha e offset global.
Uma busca antiga é cancelada ao trocar de faixa ou esconder a página.
A precisão depende da timeline do aplicativo e dos timestamps da fonte.

AudioLyricsView exibe todos os versos com destaque e scroll suave para o atual.
M2/Escape/Voltar retornam ao player. Clicar em um verso usa o timestamp e a
geração da faixa; se o aplicativo não oferecer seek, o botão fica sem ação.
Nenhuma letra é salva em disco.

Referências:
- https://lrclib.net/docs
- https://synclrc.dev/ (Developer API)
- https://music.163.com/
## Visualizador

AudioSpectrumService usa NAudio.Wasapi 2.2.1 com loopback da saída multimídia
padrão. Não abre microfone e não salva gravações.
AudioSpectrumAnalyzer mistura canais, aplica janela Hann e FFT de 2048 amostras
e gera 32 bandas logarítmicas, aproximadamente de 40 Hz a 16 kHz.
PCM16/24/32 e float são aceitos. Silêncio sem buffers limpa o espectro.

A interface recebe até 20 atualizações por segundo, sem recriar a árvore Razor
a cada quadro. As transições são CSS e respeitam Remover animações.
Captura para ao fechar a esfera, mudar de menu ou desligar o visualizador.
A saída padrão é verificada periodicamente para acompanhar troca de dispositivo.

As bandas ficam em uma camada de fundo sem alterar o tamanho do player e podem coexistir com Letras.
O espectro representa a mistura da saída, incluindo outros aplicativos;
não é um isolamento da faixa selecionada. Saídas exclusivas/protegidas ou
aplicativos roteados para outro dispositivo podem não aparecer nessa mistura.

## Plataforma e ciclo de vida

O projeto usa net8.0-windows10.0.19041.0 com mínimo declarado de Windows
10 1809, versão que introduziu as sessões globais de mídia.
Os serviços nativos ficam separados do Razor e o acesso de mídia é serializado.
IsVisible no contexto de menus suspende captura e busca de letras com o HIT recolhido.
O polling também para, exceto no modo de cores globais: nesse caso consulta
metadados a cada dois segundos para atualizar a paleta e a capa, sem captura.
CSS público: wwwroot/css/menus/HitMenuAudio.css.
## Apresentação e temas

O botão de capa ativa imagem de fundo, paleta dominante e layout compacto.
O visualizador é independente e pode ser ligado junto da capa.
Letras e personalização mantêm as dimensões do player atual.

M2 no botão de capa abre AudioAppearanceView, com blur de 0–40 px (padrão 8),
opção de capa na tela de letras, cores em todo o HIT e subopção de capa nos outros menus.
As preferências são salvas pelo HitSettingsService. O tema escolhido permanece
preservado; ao desligar o modo, volta a aparecer. Em Letras, a imagem fica
escondida por padrão e mantém apenas a cor predominante. Ativar “Capa na tela
de letras” exibe a imagem com o mesmo blur/intensidade do player, quando o modo
capa está ligado. A preferência audioArtworkLyricsCover é independente das
cores globais e da capa nos outros menus, e não altera o tamanho das Letras.

M2 no conteúdo das configurações (tanto do menu quanto da capa) usa o padrão
IHitMenuSettings.CloseSettingsAsync do host para voltar ao player. Título e
gatilhos de objetos preservam prioridade para abrir suas próprias opções.

O serviço e o botão de troca da saída de áudio foram removidos.
O loopback do visualizador continua acompanhando a saída padrão do Windows.

O CSS público e o contrato para criar temas específicos estão em
../../Design/AUDIO-THEMES.md. A extração de cores, o acompanhamento de letras e
o espectro ficam em wwwroot/js/audio-player.js; a apresentação fica nos CSS.

## Preferências do menu

HitMenuAudio implementa IHitMenuSettings. O host abre AudioSettingsView com
M2 no título atual. A subtela edita escala X/Y, ocultação de controles após
cinco segundos de inatividade enquanto fixado e exibição do aplicativo de
origem. O formulário fica em escala 100%; os valores são aplicados ao player
e às letras por HitMenuViewport. Preferências usam o HitSettingsService.

A política de inatividade é compartilhada em hit-menu-chrome.js. Ela observa
atividade real, estado aberto/fixado e a preferência do menu ativo. Polling,
metadados e transições dos versos não prolongam o temporizador. Cabeçalho
e ações secundárias ficam inert/aria-hidden;
reprodução e clique nos versos continuam disponíveis.

## Links e ativação do aplicativo de origem

SyncedLyrics guarda SourceUrl junto da fonte e dos versos no cache. LRCLIB
usa o registro por ID quando disponível; SyncLRC usa a consulta exata;
NetEase usa a página da faixa encontrada. Ctrl+clique na fonte aciona
IExternalLinkService, que só aceita URLs HTTP(S) e abre o navegador padrão.
Clique sem Ctrl não abre links.

Ctrl+clique no título, ou Ctrl+Enter quando focado, envia ActivateSource
pela mesma interface de comandos e verifica a geração da sessão.
MediaSourceWindow encontra janelas por AppUserModelID da janela/processo,
com alternativa por nome de processo para aplicativos desktop conhecidos.
Não seleciona programas por coincidência parcial do título. O título da faixa
apenas prioriza janelas que já correspondem ao aplicativo.
Restaura a janela minimizada e chama SetForegroundWindow; respeita a resposta
do Windows, sem executar outro aplicativo ou enviar teclas globais.

Referências nativas:
- https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetpropertystoreforwindow
- https://learn.microsoft.com/en-us/windows/win32/api/appmodel/nf-appmodel-getapplicationusermodelid
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindowasync
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow

O host bloqueia navegação por scroll enquanto IsSettingsOpen está true.
No foco por inatividade, o player encolhe e as letras recuperam espaço sem
mudar a altura da janela. A interação principal continua ativa.

A animação de foco usa os wrappers compartilhados, --hit-focus-progress e
hit-menu-chrome.js; serviços não alteram tamanho ou tempo de animação.
O estado Ctrl/Alt agora é do shell (hit-modifiers.js), sem listeners próprios
do Áudio. Alt destaca título e botão de capa que anunciam configuração por M2.
O contrato central de serviços/layout/configurações está em ../../HIT-MENUS.md.
