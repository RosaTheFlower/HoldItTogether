# Padrão de Menus HIT

Referência principal para desenvolver menus, serviços, configurações e layouts.
Os exemplos funcionais são Áudio e Relógio. O padrão visual inicial é HIT Dark Theme.
HIT OLED e HIT Light Theme também são embutidos e protegidos contra exclusão.
Os três preservam capa, blur e paleta musical quando habilitados.

## Situação atual e direção futura

| Recurso | Situação |
| --- | --- |
| Descoberta de componentes HitMenu*.razor compilados no projeto | Implementado |
| Configurações do menu pelo M2 no título | Implementado e opcional |
| Configuração de um objeto pelo M2 e destaque com Alt | Implementado no botão de capa; contrato reutilizável |
| Ações com Ctrl e indicação visual | Implementado e compartilhado pelo HIT |
| Foco por inatividade, escala, temas CSS públicos | Implementado |
| Layout compacto solicitado pelo menu fixado | Implementado no Relógio |
| Instalar menus externos por ZIP sem recompilar o HIT | Planejado; não há instalador ou carregador hoje |

O planejamento dos pacotes está em [Design/MENU-PACKAGES.md](Design/MENU-PACKAGES.md).
Uma pasta com novos arquivos Razor não é compilada automaticamente pelo aplicativo publicado.

## Mapa de responsabilidades

    Components/
      DynamicSphere.razor             Janela, estados e cabeçalho comum
      HitMenuHost.razor               Instâncias, navegação e contexto
      HitMenuViewport.razor           Escala e medição do conteúdo
      HitScrollRegion.razor           Scroll local sem trocar de menu
      HitDisclosure.razor             Seções expansíveis
      HitMenus/
        HitMenuAudio.razor            Entrada do menu e coordenação das subtelas
        HitMenuRelogio.razor          Relógios globais, temporizador e Pomodoro
        HitMenuConfiguracoes.razor    Configurações globais; última bolinha
      Audio/
        AudioLyricsView.razor         Tela de leitura
        AudioSettingsView.razor       Configurações do menu
        AudioAppearanceView.razor     Configurações do objeto capa/cores
    Services/
      HitMenus/                      Descoberta, contexto, navegação e contratos
      Settings/                      Preferências persistidas
      Audio/                         Mídia do Windows, FFT, letras e ativação
      Clock/                         Fusos, prazos, ciclos e avisos persistidos
      Themes/                        Importação e proteção dos temas
      ExternalLinkService.cs         Navegador padrão para links HTTP(S)
    wwwroot/
      css/hit-menus.css               Contrato compartilhado de layout e dicas
      css/menus/HitMenuAudio.css      Estilo público específico da página
      js/dynamic-sphere.js            Medição e ponte com a janela nativa
      js/hit-menu-viewport.js         Medição da superfície natural
      js/hit-menu-chrome.js           Inatividade e transição de foco
      js/hit-modifiers.js             Estado de Ctrl/Alt para todos os menus
      js/audio-player.js             Espectro, paleta e acompanhamento dos versos

O fluxo normal é serviço → snapshot → componente → HTML/CSS.
Um comando percorre o sentido inverso: interação → callback → serviço.
JavaScript trata DOM, medição e animação; não substitui os serviços de negócio.

## Criar um menu hoje

1. Crie Components/HitMenus/HitMenuAgenda.razor.
2. Mantenha o namespace HoldItTogether.Components.HitMenus ou um subnamespace.
3. Defina um título e coloque somente o conteúdo da página no componente.
4. Crie wwwroot/css/menus/HitMenuAgenda.css.
5. Recompile e inicie. O catálogo e o índice de CSS reconhecem os arquivos.

Exemplo mínimo:

    @attribute [HoldItTogether.Services.HitMenus.HitMenuTitle("Agenda")]

    <section class="agenda-menu">
        <h2>Hoje</h2>
        <p>Conteúdo do menu.</p>
    </section>

O catálogo aceita componentes públicos, concretos, sem parâmetros genéricos,
cujo nome começa com HitMenu, no namespace de menus. A descoberta atual consulta
o assembly do próprio HIT. Os menus comuns são ordenados pelo nome do tipo;
Configurações fica sempre por último. Sem o atributo, o nome do tipo fornece o título.

Não crie entradas HitMenu separadas para cada subtela. Elas virariam novas bolinhas.
Coloque os componentes auxiliares em Components/Agenda, por exemplo.

## Serviços e dados

Serviços ficam em Services/{Recurso}. O Razor coordena apresentação e comandos;
acesso a Windows, HTTP, arquivos e captura pertence ao serviço correspondente.

- Defina interfaces quando existir fronteira de integração ou necessidade de substituir a implementação.
- Use snapshots/DTOs para representar dados, estado e capacidades disponíveis.
- Prefira operações assíncronas com CancellationToken, timeout e resultados explícitos.
- Rejeite comandos de dados antigos quando a origem mudar; Áudio usa Generation.
- Limite concorrência em recursos compartilhados e cancele consultas substituídas.
- Apresente indisponibilidade/erro no menu sem derrubar o restante do HIT.
- Cache deve ter limite e prazo. Não faça requisições ou gravações a cada render.
- Separe preferências persistentes de estado temporário da tela.
- Registre serviços em Program.cs. A descoberta de menus não registra serviços automaticamente.

Hoje os serviços compartilhados são registrados por DI no início do aplicativo.
Use singleton para um recurso realmente compartilhado e com estado protegido.
Não guarde um componente ou ElementReference num singleton.
O projeto usa HitSettingsService para configurações; não escreva em bin, no
tema distribuído ou no diretório do projeto para persistir preferências.

Um serviço nativo continua necessário depois de ocultar o menu? Documente o
motivo. No Áudio, captura e busca de letras param fora de vista; apenas a
consulta leve de metadados pode continuar para manter a paleta global.

## Contexto, ciclo de vida e navegação

O host mantém uma instância de cada menu visitado até encerrar o HIT.
Menus ocultos preservam estado, mas não entram na medição e ficam inert.

Receba o contexto quando necessário:

    [CascadingParameter(Name = "HitMenu")]
    public HoldItTogether.Services.HitMenus.HitMenuContext? MenuContext { get; set; }

| Membro | Uso |
| --- | --- |
| IsActive | Menu selecionado |
| IsVisible | Menu selecionado e esfera aberta |
| CloseRequested | Fechar a esfera |
| TitleRequested | Alterar o título comum na subtela |
| IsPinned | Fixação atual do HIT |
| LayoutRequested | Pedir Default ou Compact ao host, somente pelo menu ativo |

Pause trabalho de interface quando IsVisible for false. Ao descartar o componente,
cancele tarefas, remova eventos e desconecte observers/JS references.
Atualizações vindas de callbacks usam InvokeAsync antes de alterar a interface.

Scroll muda de menu, com sensibilidade/cooldown globais. Sliders e áreas de leitura
podem manter a roda local. Uma configuração específica aberta bloqueia a troca
por scroll; as bolinhas continuam permitindo navegação deliberada.

## Planejar telas e subtelas

Planeje cada recurso antes de acrescentar botões:

| Tela | Objetivo | Navegação/tamanho |
| --- | --- | --- |
| Principal | Informação e ações frequentes | Uma bolinha |
| Detalhe/leitura | Conteúdo ampliado, como Letras | Subtela; retorno ao conteúdo principal |
| Configuração do menu | Preferências do recurso | M2 no título; bloqueia scroll entre menus |
| Configuração de objeto | Ajustes de um elemento específico | M2 no objeto; não cria bolinha |
| Confirmação | Ação destrutiva identificada | Modal com Cancelar/Escape/foco |

Use estado explícito e telas mutuamente exclusivas. Para novos menus, prefira
um enum de tela a vários booleanos independentes. Determine o que permanece
montado para preservar posição, animação ou medição.

O cabeçalho comum contém título, Fixar e Fechar. Não replique esses controles
no conteúdo. Título de subtela é publicado por TitleRequested, com proteção
do host para ignorar mudanças de um menu que não está selecionado.

## Configurar o menu

Implemente IHitMenuSettings apenas quando o recurso tiver configurações:

    @implements HoldItTogether.Services.HitMenus.IHitMenuSettings

    @code {
        private bool _settingsOpen;
        public bool IsSettingsOpen => _settingsOpen;
        public Task OpenSettingsAsync() => InvokeAsync(() => {
            _settingsOpen = true;
            StateHasChanged();
        });
        public Task CloseSettingsAsync() => InvokeAsync(() => {
            _settingsOpen = false;
            StateHasChanged();
        });
        private void ReturnToContent() => _settingsOpen = false;
    }

O host chama OpenSettingsAsync por M2 no título. IsSettingsOpen bloqueia
scroll imediatamente, sem depender de um render posterior.
Inclua nesse estado outros painéis de configuração do menu, como o da capa.
M2 no conteúdo de uma configuração aberta chama CloseSettingsAsync e volta
ao conteúdo, sem fechar nem desfixar o HIT. O host verifica o estado no início
do gesto para não fechar imediatamente uma configuração que acabou de abrir.
M2 no título continua reservado a OpenSettingsAsync; gatilhos próprios
interrompem a propagação do evento de contexto.

O título também aceita tecla de contexto, Shift+F10 e Enter quando focado.
Configurações gerais não implementa a interface e não anuncia outro menu.

O formulário usa o mesmo padrão visual: controles essenciais na linha,
opções adicionais dentro de HitDisclosure. O Áudio deixa o formulário em
escala 100% e aplica os tamanhos escolhidos ao retornar ao conteúdo.

## Objetos com configuração própria

Um objeto pode oferecer ação simples no M1 e ajustes no M2. No Áudio:

- M1 no botão de capa ativa/desativa o modo.
- M2 no mesmo botão abre blur, alcance global das cores e imagem global.
- O painel é uma subtela da instância do Áudio.

Declare data-hit-menu-trigger="identificador" no objeto que realmente abre
um menu pelo M2. Implemente o evento de contexto e evite a ação nativa do navegador:

    <button type="button" data-hit-menu-trigger="appearance" data-no-drag
            @onclick="ToggleObject"
            @oncontextmenu="OpenObjectSettings" @oncontextmenu:preventDefault="true" @oncontextmenu:stopPropagation="true">
        Configurável
    </button>

O marcador só descreve a interação para o estilo compartilhado; não cria o
painel nem executa código. Use um identificador estável, acessibilidade e
retorno por Voltar/Escape. Ofereça também teclado de contexto quando necessário.

Se houver um gesto M2 para voltar da subtela, ele não pode interceptar o M2
do título nem de um objeto configurável. Não marque como configurável algo
que só volta ou não possui menu. Não transforme M2 em uma ação destrutiva.

## Contrato de Ctrl e Alt

| Gesto | Indicação | Ação |
| --- | --- | --- |
| Ctrl sobre data-ctrl-link | Sublinhado contínuo e cursor clicável | Ação explícita com Ctrl+M1 |
| Alt sobre data-hit-menu-trigger | Sublinhado pontilhado e contorno tracejado | Descobre o menu; M2 continua abrindo |
| M2 no título configurável | O título anuncia configuração | Configuração do menu |
| M2 num objeto configurável | O objeto anuncia configuração | Configuração daquele objeto |

Ctrl/Alt são lidos pelo shell em hit-modifiers.js. Os atributos
data-ctrl-pressed/data-alt-pressed ficam na esfera e sobrevivem a renders
que trocam classes. São limpos ao soltar teclas ou perder foco/visibilidade.
Alt não abre painéis por si só. Objetos desabilitados não recebem destaque.

No Áudio, Ctrl+clique na fonte abre a URL real da letra; no título, ativa a
janela do aplicativo da sessão de mídia. O handler valida o modificador;
o atributo visual sozinho não autoriza ações. Links externos passam pelo
IExternalLinkService. O título também oferece Ctrl+Enter.
O arraste ignora Ctrl+M1 em elementos data-ctrl-link.

## Tamanho, janela e foco por inatividade

Não dimensione o conteúdo em 100vw/100vh: a janela já depende do conteúdo.
A medição usa dimensões naturais e continua reagindo a mudanças de tema/tela.
O host acrescenta margem, cabeçalho e paginação. A janela mantém sua ancoragem
e limites físicos, que pertencem ao shell.

HitMenuViewport oferece escala X/Y de 75–200%. Seu elemento de superfície
mantém a dimensão natural; o invólucro informa a dimensão escalada ao host.
As variáveis medidas por JS ficam num elemento sem style controlado pelo Razor.

Um menu pode optar por foco automático com:

    <section data-hit-hide-controls="true" data-hit-view="principal">
        <div class="hit-idle-collapse" data-hit-collapse="always">
            <div class="hit-idle-collapse-content">
                <div data-hit-idle-controls>Ações secundárias</div>
            </div>
        </div>
        <p>Conteúdo principal continua interativo.</p>
    </section>

Após cinco segundos sem interação, apenas aberto e fixado, o HIT oculta o
cabeçalho e ações marcadas. Eles recebem inert/aria-hidden imediatamente.
Movimento real do mouse, teclado ou interação restauram os controles.
Polling e hover na mesma posição física, causado pelo resize, não contam.

Áudio tem duas regras de espaço:

- Player: contrai cabeçalho e faixa secundária; a janela fica menor.
- Letras: mantém a janela e amplia a área de leitura; a fonte permanece disponível.

A transição de foco dura 1200ms, com desaceleração suave; o fade dura 450ms.
Ela pode ser revertida a partir do ponto atual quando o usuário interage.
Os marcadores data-hit-controls-hidden, data-hit-idle-view e
data-hit-layout-animating pertencem ao shell.

A variável registrada --hit-focus-progress vai de 0 a 1 no elemento de medição
e é herdada pelo conteúdo. Header e leitura compensam o espaço usando o
mesmo progresso; a escala Y é considerada. Regiões colapsam com grid 1fr→0fr,
sem display:none abrupto. Durante essa transição a janela acompanha a altura
animada do layout, sem uma segunda animação de altura atrasada.

Remover animações e prefers-reduced-motion preservam os estados finais,
gestos e funcionalidade, com mudança imediata. Não imponha tempos no C#.

## Estilização e temas

CSS é público, inclusive após publicar. Não use Razor CSS isolado.
Os arquivos distribuídos são preservados pela escolha/importação/exclusão de
temas. Um tema pessoal acrescenta regras; não reescreve o padrão canônico.

Um tema usa as camadas sphere e menus:

    @layer sphere {
        :root {
            --hit-focus-duration: 1600ms;
            --hit-focus-fade-duration: 600ms;
            --hit-menu-hint-color: #d3d9ef;
        }
    }
    @layer menus {
        .agenda-menu { color: var(--hit-menu-text, var(--hit-text)); }
        .agenda-menu h2 { color: var(--hit-accent); }
    }

A camada sphere personaliza janela/mascote/cabeçalho. A camada menus é
automaticamente limitada às páginas. Use uma raiz própria para não afetar
outros recursos. CSS do projeto usa escopo de baixa especificidade:

    :where(.hit-menu-panel[data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAgenda"]) .agenda-menu {
        width: 400px;
        color: var(--hit-menu-text, var(--hit-text));
    }

| Variável pública | Padrão/finalidade |
| --- | --- |
| --hit-text / --hit-muted / --hit-accent | Texto, apoio e destaque |
| --hit-font / --hit-mono-font | Tipografia |
| --hit-open-background / --hit-open-radius | Superfície aberta |
| --hit-focus-duration | 1200ms; layout de foco |
| --hit-focus-ease | cubic-bezier(0.22,1,0.36,1) |
| --hit-focus-fade-duration | 450ms; título/ações |
| --hit-menu-padding-top / --hit-menu-idle-padding-top | 64px / 24px |
| --hit-link-hint-color | Cor opcional do destaque Ctrl |
| --hit-menu-hint-color | Cor opcional do destaque Alt |

Use duração simples em ms ou s, entre 0 e 10 segundos, no contrato de foco.
Não edite --hit-focus-progress para forçar estados: ele é controlado pelo shell.
Não dependa de classes acrescentadas via JS em elementos com class do Razor;
use os atributos/variáveis publicados. Temas não acrescentam serviços ou HTML.

Detalhes visuais: [Design/README.md](Design/README.md).
Seletores, paleta e camadas de Áudio: [Design/AUDIO-THEMES.md](Design/AUDIO-THEMES.md).

## Fluxo de trabalho para novos recursos

1. Defina a informação principal, comandos e capacidades da integração.
2. Separe serviço/DTO de apresentação; registre a dependência.
3. Desenhe tela principal, estados vazios/erros e subtelas.
4. Decida configurações do menu e de cada objeto; marque os alvos de M2.
5. Defina retorno, bloqueio de scroll, escala e política de foco.
6. Implemente CSS público com variáveis e escopo do menu.
7. Verifique ativo/oculto/recolhido, com/sem animação e tema claro de contraste suficiente.
8. Teste ações, cancelamento e configurações salvas; publique com CSS/JS/documentação.

Prefira testar comportamento: geração antiga rejeitada, scroll bloqueado e
liberado, ausência de trabalho fora de vista, retorno M2, interação dos versos,
destaques Ctrl/Alt, movimento intermediário e reversão da animação, escala sem
oscilar e arquivos públicos presentes no publish. Não gere movimento falso
para simular dados reais.

## Próximos contratos

O formato ZIP deve aproveitar essas separações sem fingir que CSS pode
implementar serviço. O desenho de módulos, manifesto, ciclo de instalação,
carregamento de código/layout e limites de customização está no documento
de pacotes planejados. Nenhum desses mecanismos é ativado apenas por criar
as pastas descritas.

## Layout compacto do menu fixado

Relógio demonstra o contrato de apresentação independente de configuração.
O contexto informa IsPinned. Um menu ativo pode solicitar:

    await MenuContext.LayoutRequested.InvokeAsync(HitMenuLayout.Compact);

Para voltar ao cabeçalho/paginação comuns, solicite HitMenuLayout.Default.
O shell aplica .is-menu-compact à esfera e .is-compact ao host, retirando os
controles comuns e usando --hit-compact-padding-x/y. Não duplique Fixar/Fechar
no widget nem altere a geometria nativa diretamente.

A solicitação é publicada ao mudar de estado, não em todos os renders.
O host ignora solicitações de páginas inativas e restaura a apresentação
da nova página ao navegar. Abrir configuração/formulário usa layout Default.

No Relógio, fixado, três segundos sem interação ou um clique no mostrador exibem apenas a função selecionada; M1 no compacto abre os controles,
M2 configura, e o conteúdo/serviço continua ativo. O worker de prazos continua
mesmo fora de vista; apenas o ticker da apresentação é suspenso.
Hover não expande o compacto. O mostrador completo usa role="button" e data-no-drag.
O compacto usa data-hit-drag-handle: adia a captura até seis pixels de movimento,
preservando o clique para abrir. Arrastar reposiciona sem abrir nem desfixar;
soltar o arraste suprime somente o clique correspondente.
M2 no mostrador do sistema abre fusos globais; M2 no título abre opções gerais.
O primeiro e o segundo fusos da lista definem a exibição dupla, quando ativada.
No compacto, o secundário mostra apenas o horário pequeno e muted, abaixo
e centralizado. Na configuração, use HitDisclosure com Expanded/ExpandedChanged
para coordenar uma edição por vez, mantendo nome, prévia e seta na mesma linha.

Use HitScrollRegion para listas de conteúdo com rolagem local. O componente
interrompe wheel no DOM antes da navegação Blazor, mantendo o scroll nativo,
e remove listeners quando é descartado.

Serviço/persistência: [Services/Clock/README.md](Services/Clock/README.md).
Estilização: [Design/CLOCK-THEMES.md](Design/CLOCK-THEMES.md).

O Relógio usa data-hit-idle-action="compact" na tela principal fixada.
O Relógio anuncia data-hit-idle-delay="3000"; o shell emite hit-menu-idle após três segundos de inatividade. O padrão compartilhado continua em cinco segundos. O menu valida
visibilidade/fixação/tela antes de solicitar Compact. Relógio atualizado,
polling e mudanças de texto não reiniciam esse prazo. Voltar ao menu o mantém
aberto até a próxima inatividade ou clique no mostrador.
