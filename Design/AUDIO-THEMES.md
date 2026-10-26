# Temas da página de Áudio

Todo o CSS permanece público após compilar:

- wwwroot/css/menus/HitMenuAudio.css: player, visualizador, letras e personalização.
- wwwroot/css/audio-shell.css: paleta e capa aplicadas ao restante do HIT.
- wwwroot/themes/HIT-OBSIDIAN-EYES.css: tema padrão protegido.

Um tema pessoal é um CSS importado em Configurações → Temas. A importação
acrescenta estilos e preserva os arquivos distribuídos. Este guia acompanha
a saída compilada em Design/AUDIO-THEMES.md.

## Estrutura e estados

A página é identificada por:

    .hit-menu-panel[data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAudio"]

Na camada menus, o HIT acrescenta o escopo dos menus automaticamente; use
as classes relativas abaixo. Para atingir só Áudio, inicie por .audio-menu.

| Seletor | Finalidade |
| --- | --- |
| .audio-menu | Raiz do player e das subtelas |
| .audio-menu.is-artwork | Modo capa/cores/compacto solicitado |
| .audio-menu[data-audio-palette="true"] | Capa válida com paleta calculada |
| .audio-menu.has-spectrum | Visualizador ligado, independente do modo capa |
| .audio-menu.is-lyrics | Tela de letras |
| .audio-menu[data-audio-lyrics-cover="true"] | Preferência para mostrar capa nas letras |
| .audio-menu.is-customizing | Personalização do modo capa |
| .audio-menu.is-menu-settings | Configurações de Áudio, abertas por M2 no título |
| .audio-menu.hide-source | Origem do áudio escondida pelo usuário |
| .audio-scene | Fundo cobrindo também cabeçalho e paginação |
| .audio-artwork-wash | Imagem de capa suavizada |
| .audio-spectrum / .spectrum-bar | Bandas reais do áudio |
| .audio-artwork-veil | Véu discreto para contraste |
| .audio-player-content | Player mantido no fluxo para medir o tamanho |
| .audio-player-content.is-obscured | Player invisível, sem interação ou foco |
| .audio-overlay-view | Subtela sobreposta, com as mesmas dimensões do player |
| .audio-track / .audio-metadata | Capa lateral e metadados |
| .audio-title / .audio-artist | Título e artista |
| .audio-timeline / .audio-times | Progresso e duração |
| .audio-toolbar / .audio-transport | Aleatório, anterior, play/pause, próxima e loop |
| .audio-tools / .audio-artwork-toggle | Ações secundárias |
| .audio-lyrics-view | Conteúdo da tela de letras |
| .lyrics-heading / .audio-view-back / .lyrics-provider | Voltar e fonte |
| .lyrics-viewport / .lyrics-lines | Área rolável e versos |
| .lyric-line.is-current | Verso atual |
| .lyric-line.is-past / .lyric-line.is-future | Versos anteriores/próximos |
| .audio-appearance-view | Personalização |
| .appearance-blur | Controle de blur e valor |
| .appearance-global / .appearance-suboption | Cores globais e capa global |
| .appearance-switch / .appearance-hint | Interruptores e explicação |

Letras, personalização e configurações são subtelas: não adicionam bolinhas. O título comum
muda para Letras, Tema da música ou Configurações de áudio. M2, Escape ou Voltar retornam ao player.

A altura das subtelas segue o player no modo atual, inclusive compacto.
O player permanece medindo o espaço, com visibility:hidden, inert e aria-hidden.
Não use display:none nele nem imponha uma altura própria à tela de letras.

## Camadas de fundo

A cena é isolada: imagem em z-index 0, espectro em 1 e véu em 2. Os controles
ficam acima da cena. Todos os fundos ignoram eventos do mouse. Ativar um recurso
não remove o outro nem altera o tamanho da janela.

As marcações de paleta usam atributos data, independentes das classes que o
Blazor atualiza ao ligar o visualizador ou fixar a esfera. Preserve esses
atributos ao criar estilos.

## Variáveis públicas

Defina em .audio-menu, dentro da camada menus:

| Variável | Padrão / função |
| --- | --- |
| --audio-foreground | Texto principal herdado do tema |
| --audio-muted | Texto secundário herdado do tema |
| --audio-accent | Cor dos controles herdada do tema |
| --audio-surface | Cor de contraste do play |
| --audio-spectrum-opacity | 0.11; espectro sem capa |
| --audio-spectrum-art-opacity | 0.10; espectro junto da capa |
| --audio-cover-opacity | 0.32; presença da imagem |
| --audio-cover-blur | 8px; valor controlado pela preferência do usuário |
| --audio-lyric-size | 20px; tamanho dos versos |
| --audio-lyric-gap | 18px; espaço entre versos |
| --audio-lyric-future-blur | 0.6px; desfoque dos versos futuros |
| --audio-scene-inset-top | 64px; espaço do cabeçalho |
| --audio-scene-inset-side | 24px; margens do host |
| --audio-scene-inset-bottom | 44px; paginação |

Os insets acompanham as margens do host se o tema alterar sua estrutura.
--lyrics-viewport-size é medido pelo JS para centralizar versos na altura real.
O antigo --audio-lyrics-height não é usado.

O blur selecionado é aplicado inline em --audio-cover-blur para que o controle
tenha prioridade sobre o padrão do CSS. Ajuste-o pelo painel M2. Para a capa
global, --audio-cover-opacity pode ser declarado na esfera pela camada sphere.

Exemplo de tema parcial:

    @layer menus {
        .audio-menu {
            --audio-accent: #b4d4ff;
            --audio-spectrum-opacity: 0.16;
            --audio-spectrum-art-opacity: 0.12;
            --audio-cover-opacity: 0.24;
            --audio-lyric-size: 22px;
            --audio-lyric-gap: 22px;
            --audio-lyric-future-blur: 0px;
        }
        .audio-title { font-weight: 600; }
        .audio-cover { border-radius: 16px; }
        .lyric-line { font-family: "Segoe UI", sans-serif; }
        .lyric-line.is-current { color: var(--audio-accent); }
    }

## Paleta, personalização e alcance

Clique no botão Modo capa e cores para ativar/desativar imagem, paleta e player
compacto. M2 no mesmo botão abre a personalização, mesmo com o modo desligado:

- Blur de 0 a 40 px, com prévia enquanto arrasta e salvamento ao soltar.
- Capa na tela de letras: desligada por padrão; permite imagem no fundo da
  leitura quando o modo capa está ligado. Usa o blur e a intensidade existentes.
- Usar cores em todo o HIT: mantém a paleta nos outros menus e na esfera recolhida.
- Capa nos outros menus: subopção ligada por padrão; pode ocultar só a imagem
  global e manter as cores. O blur também vale para essa imagem.

As preferências são persistidas em settings.json: audioArtworkMode,
audioArtworkBlur, audioArtworkGlobalAccent, audioArtworkGlobalCover e audioArtworkLyricsCover.

Em Letras fica apenas a cor predominante escurecida por padrão. Com a nova
opção ativa, .is-lyrics[data-audio-palette="true"][data-audio-lyrics-cover="true"]
também exibe .audio-artwork-wash. É independente da capa nos outros menus e das
cores globais; o fundo usa a mesma camada, sem duplicação no shell das Letras.
Com o modo desligado,
ou uma capa ausente/inválida, o tema escolhido volta a aparecer.
Com alcance local, as cores saem ao trocar de menu ou recolher o HIT.
Com alcance global, acompanham a música também quando Áudio está fora de vista.
Isso não modifica os arquivos do tema escolhido.

A análise usa uma miniatura de 36 × 36 px em memória, sem baixar imagens
adicionais ou salvar cópias. O fundo é escurecido para manter legibilidade.
Os resultados são --audio-art-background, --audio-art-surface,
--audio-art-accent, --audio-art-foreground e --audio-art-muted.
Prefira as variáveis públicas --audio-foreground/--audio-accent para sobrepor
as cores do player:

    @layer menus {
        .audio-menu[data-audio-palette="true"] { --audio-accent: #ffffff; }
    }

No cabeçalho/esfera, use a camada sphere:

    @layer sphere {
        .dynamic-sphere[data-audio-theme] .sphere-title { color: #fff; }
        .dynamic-sphere[data-audio-theme="global"] {
            --audio-cover-opacity: 0.24;
        }
    }

data-audio-theme vale local ou global. data-audio-cover="true" habilita a
imagem global no elemento .sphere-audio-backdrop, sem interceptar cliques,
mudar as medidas ou reutilizar os pseudo-elementos dos olhos.
A imagem global é escondida durante a tela de Letras.

## Letras e movimento

Os versos permanecem no DOM ao trocar o destaque; opacidade, blur e escala
têm transições CSS. O JS acompanha o verso com scroll suave e pausa após
rolagem manual. Clicar envia seu timestamp real para o player, com proteção
contra comandos de uma faixa anterior. Sem suporte a seek, os versos ficam
legíveis e sem ação.

Remover animações e a preferência do sistema por movimento reduzido anulam
transições e scroll suave. Sincronização, cliques e dados reais do visualizador
continuam funcionando.

## Limites

O tema controla apresentação. Letras, captura, reprodução, extração de cores
e retorno por M2 pertencem ao aplicativo; um tema não executa JavaScript.
Importar, selecionar ou excluir temas mantém o CSS distribuído preservado.

## Configurações de Áudio e tamanho

M2 no título comum do HIT abre AudioSettingsView. O atalho permanece
disponível quando o título mostra Letras ou Tema da música. M2 no botão de
capa continua abrindo apenas a personalização de capa/cores.

As novas preferências salvas são audioScaleX, audioScaleY (75–200%, padrão
100%), audioHideControls (padrão desligado) e audioShowSource (padrão ligado).
Mostrar fonte também vale para o player compacto no modo capa.

O slider compacto Tamanho altera os dois eixos igualmente. A seta abre largura
e altura individuais. A configuração permanece sem escala para manter os
controles legíveis; o tamanho escolhido aparece ao voltar ao player.
O mesmo viewport dimensiona player e letras, sem alterar a altura na troca.

Classes públicas da configuração: .audio-settings-view,
.audio-size-disclosure, .audio-size-uniform, .audio-size-axis e
.audio-setting-row. Os interruptores reutilizam .appearance-switch.
A expansão reutiliza HitDisclosure e suas classes .disclosure-*.

O invólucro reutilizável usa .hit-menu-scale, .hit-menu-viewport e
.hit-menu-surface. --hit-menu-scale-x/y são fatores aplicados pelo componente.
--hit-menu-natural-width/height são dimensões naturais medidas pelo JS em
um elemento separado, para não desaparecerem quando o Blazor atualiza a escala.
Mantenha a origem da transformação no canto superior esquerdo. O fundo
compensa os insets para continuar cobrindo cabeçalho e paginação.

Com Esconder botões ligado, cinco segundos sem interação e com o HIT fixado
ocultam o cabeçalho e as ações secundárias. Reprodução, timeline e versos
permanecem interativos. A regra também vale para Letras, incluindo seu botão
Voltar. O player remove o espaço dos botões e do cabeçalho, ficando menor.
Em Letras, o cabeçalho liberado aumenta a área de leitura, mantendo a janela.

O estado visual é .dynamic-sphere[data-hit-controls-hidden="true"].
Elementos com data-hit-idle-controls são ações secundárias. Eles recebem inert
e aria-hidden durante a ocultação; o CSS também remove o espaço quando necessário. Movimento/entrada do mouse ou interação
revelam os controles; consultas de mídia e avanço de versos não reiniciam o
temporizador. Configurações de Áudio e personalização não ocultam controles.
O gesto M2 no título é reservado às configurações, sem disparar Voltar.

## Layout de foco e links com Ctrl

Configurações de Áudio e personalização bloqueiam scroll entre menus enquanto
estiverem abertas. Voltar libera a navegação.

Durante a ocultação, a esfera recebe data-hit-idle-view="player" ou "lyrics".
O player contrai a região que contém .audio-tools. .hit-menu-measure reduz seu padding
superior de --hit-menu-padding-top (64px) para --hit-menu-idle-padding-top
(24px). A tela de letras compensa essa redução no seu viewport, dividindo
pela escala Y para manter a altura física. Seu botão Voltar some e a fonte
permanece no canto direito. Alterar essas variáveis pelo tema preserva a
compensação. Eventos de hover sem movimento físico não desfazem a ocultação.

A esfera .dynamic-sphere recebe data-ctrl-pressed="true" enquanto Ctrl está pressionado.
Elementos com data-ctrl-link ganham cursor clicável e sublinhado: .audio-title
e .lyrics-provider. O título também aceita Ctrl+Enter quando focado.
Os atributos são separados das classes reescritas pelo Blazor.

Ctrl + clique na fonte abre o navegador padrão no registro da letra.
SyncedLyrics.SourceUrl preserva a URL correspondente ao resultado encontrado.
LRCLIB e SyncLRC abrem o registro original da API (contém a letra e os dados
da consulta); NetEase abre a página da faixa. Nenhum link é inventado a partir
do nome da música.

Ctrl + clique no título traz para frente a janela existente do aplicativo
da sessão de mídia. Uma janela minimizada é restaurada; esse gesto não inicia
uma nova reprodução. O arraste da janela do HIT é ignorado nesse gesto com Ctrl.

## Movimento de foco e descoberta por Alt

O contrato principal está em [HIT-MENUS.md](../HIT-MENUS.md).
O foco agora usa --hit-focus-progress (0→1) animado por 1200ms, com curva
--hit-focus-ease. A mesma progressão reduz o padding do host e aumenta a
compensação de leitura, mantendo a janela de Letras estável. Os wrappers
.hit-idle-collapse / .hit-idle-collapse-content contraem suas linhas de grid
até zero, preservando altura intermediária e reversão suave.

Não aplique display:none em .audio-tools ou .lyrics-back para reproduzir o
foco: isso elimina a animação. Os marcadores data-hit-collapse="player" e
"lyrics" escolhem a região que contrai; o conteúdo usado para medir a tela
de Letras mantém seu tamanho natural. O fade usa --hit-focus-fade-duration,
450ms por padrão. Remover animações continua funcionando.

data-hit-layout-animating="true" indica que o shell acompanha as medidas
animadas. Nesse intervalo, o contorno não deve acrescentar uma segunda
transição de height atrasada. Não remova essa proteção para personalizar a
velocidade; ajuste --hit-focus-duration (ms ou s, até 10 segundos).

Alt ativa data-alt-pressed="true" na esfera. O título configurável anuncia
data-hit-menu-trigger="settings"; o botão de capa anuncia "appearance".
O CSS compartilhado usa sublinhado pontilhado e contorno tracejado, inclusive
nos ícones. Ctrl mantém sublinhado contínuo para data-ctrl-link.
Os dois estados são globais do shell e limpos ao perder foco.
--hit-link-hint-color e --hit-menu-hint-color permitem personalizar as dicas
pela camada sphere. Os gestos reais continuam sendo Ctrl+M1 e M2.
