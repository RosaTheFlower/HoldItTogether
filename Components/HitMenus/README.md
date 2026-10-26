# Menus do HIT

Referência principal: [HIT-MENUS.md](../../HIT-MENUS.md). Pacotes externos futuros: [planejamento](../../Design/MENU-PACKAGES.md).

Cada menu é um componente Razor em Components/HitMenus, com nome
HitMenu{Nome}.razor. O HIT descobre os componentes compilados automaticamente,
ordena os menus comuns pelo nome do arquivo e cria as bolinhas de navegação.
Configurações ocupa sempre a última bolinha.

## Criar um menu

Crie Components/HitMenus/HitMenuAgenda.razor:

    @attribute [HoldItTogether.Services.HitMenus.HitMenuTitle("Minha agenda")]

    <section class="agenda-menu">
        <h2>Hoje</h2>
        <p>O conteúdo do seu menu.</p>
    </section>

O título é exibido pelo HIT na mesma linha de Fixar/Fechar, fora do conteúdo
da página. O atributo é opcional: sem ele, HitMenuMinhaAgenda recebe o título
"Minha Agenda". O título acompanha a troca por scroll ou pelas bolinhas.
Use h2/h3 para subtítulos; o cabeçalho comum já contém o h1 do menu.

Crie wwwroot/css/menus/HitMenuAgenda.css:

    :where(.hit-menu-panel[data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAgenda"]) .agenda-menu {
        width: 800px;
        min-height: 360px;
        color: var(--hit-menu-text, var(--hit-text));
    }

    :where(.hit-menu-panel[data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAgenda"]) h2 {
        margin: 0 0 16px;
        color: var(--hit-accent);
    }

O prefixo restringe o estilo à página. :where mantém a especificidade baixa,
para que os temas possam sobrescrever o estilo. Use os mesmos prefixos em
todas as regras do arquivo, inclusive dentro de @media.

Recompile/inicie o projeto: o componente e o arquivo CSS público serão
incluídos automaticamente. Não edite Program.cs, catálogo ou index.html.
Não crie .razor.css: o padrão é CSS público em wwwroot/css/menus.
Mantenha o namespace HoldItTogether.Components.HitMenus (ou um subnamespace);
nesse último caso, ajuste data-hit-menu para o nome completo do componente.

## Tamanho e layout

- O HIT mede o conteúdo real, inclusive mudanças posteriores e de tema.
- Uma largura de 800 px disponibiliza 800 px ao conteúdo, com espaço adicional
  para cabeçalho, margens e paginação.
- A animação, a ancoragem e os limites do monitor pertencem ao HIT.
- Use um elemento raiz com tamanho explícito ou conteúdo no fluxo normal.
- Evite 100vw/100vh: eles criam uma dependência circular com a janela.
- Conteúdo maior que a área física do monitor precisa de layout responsivo.
- Conteúdo livre fica na página; não reproduza os controles do cabeçalho.

## Navegação e estado

- Scroll para baixo: próximo menu; para cima: anterior. Não há retorno circular.
- As bolinhas aceitam clique e teclado. A primeira começa ativa.
- Um menu é montado na primeira visita e preserva seu estado até encerrar o HIT.
- Menus inativos ficam ocultos e não entram na medição.
- Textos e objetos não são selecionáveis, seguindo a regra geral do HIT.
- Controles interativos não iniciam o arraste da janela; use data-no-drag em
  outras regiões que precisem impedir o arraste.

Os componentes podem receber o CascadingParameter chamado HitMenu,
do tipo HitMenuContext, para consultar IsActive/IsVisible e chamar CloseRequested.
Configurações usa IsActive para pausar a leitura de RAM quando está oculto.

## Configurações e temas

As preferências são salvas em %LOCALAPPDATA%\HoldItTogether\settings.json.
Always on Top e System Tray são aplicados imediatamente. A RAM mostra
a memória residente privada do processo HIT (sem somar o WebView); em
sistemas antigos, usa a memória residente principal como alternativa.

Scroll speed permite controlar sensibilidade e cooldown, inclusive navegar
por vários menus com um único gesto. Remover animações atua apenas sobre CSS.
Themes importa/seleciona/exclui temas pessoais sem modificar o CSS original.

O padrão visual HIT Dark Theme, os temas embutidos HIT OLED e HIT Light Theme,
o contrato completo e exemplos estão em ../../Design/README.md.
Todos os menus devem seguir esse contrato e usar as variáveis públicas
sempre que possível.

A janela permanece fora da barra de tarefas. System Tray disponibiliza
Mostrar HIT, Ocultar HIT e Sair quando habilitado.
## Configurações próprias de cada menu

Um menu pode optar por ter configurações ao implementar IHitMenuSettings.
O host reconhece a interface e encaminha M2 no título para a instância do
menu selecionado. Também funciona com Shift+F10, tecla de contexto ou Enter
quando o título está focado. Menus sem a interface não recebem o atalho.
O menu geral de Configurações não implementa essa interface.

Exemplo mínimo:

    @implements HoldItTogether.Services.HitMenus.IHitMenuSettings

    @if (_settingsOpen) {
        <section data-no-drag>
            <button @onclick="CloseSettings">Voltar</button>
            <!-- Controles próprios do menu -->
        </section>
    } else {
        <p>Conteúdo normal</p>
    }

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
        private void CloseSettings() => _settingsOpen = false;
    }

A configuração é uma subtela da mesma instância: não cria outra bolinha nem
outro arquivo HitMenu dentro desta pasta. Coloque componentes auxiliares em
Components/{Recurso}, como Components/Audio/AudioSettingsView.razor.
O menu pode mudar o título comum usando HitMenuContext.TitleRequested.
M2 no título tem prioridade sobre o gesto de voltar de uma subtela.
IsSettingsOpen informa ao host que a configuração está aberta: enquanto true,
a troca de menus por scroll fica bloqueada. Ao voltar, retorne false.
Áudio também retorna true no painel de personalização da capa.
O host encaminha M2 no conteúdo das configurações para CloseSettingsAsync.
Esse retorno mantém a fixação, restaura o conteúdo e libera o scroll.
O gesto precisa começar com a configuração já aberta; o mesmo M2 que abre
um painel não pode fechá-lo. Objetos com configuração própria devem usar
@oncontextmenu:stopPropagation="true" para preservar seu atalho.

## Escala e controles após inatividade

HitMenuViewport oferece escala X/Y em porcentagem de 75 a 200 (100 é o padrão).
O componente mantém uma superfície natural e mede sua versão ampliada para o
host. Use o mesmo viewport no player e nas letras para preservar as dimensões.
A tela de configurações de Áudio permanece em 100% para facilitar os ajustes;
as dimensões escolhidas são aplicadas ao retornar ao player.

O controle compacto de tamanho ajusta X/Y juntos; a seta abre os eixos
individuais. Alterações são salvas ao soltar o slider ou concluir pelo teclado.

Para optar pela ocultação de ações secundárias, um menu visível pode declarar:

    <div data-hit-hide-controls="true" data-hit-view="player">
        <div data-hit-idle-controls>Botões secundários</div>
        <p>Conteúdo que continua visível e interativo.</p>
    </div>

O HIT oculta o cabeçalho e os elementos marcados depois de cinco segundos
sem interação, somente enquanto estiver aberto e fixado. Bloqueia foco/cliques nos elementos ocultos e restaura tudo com movimento do
mouse, entrada do ponteiro, teclado, foco, rolagem ou outros controles.
Atualizações de mídia não contam como interação. Eventos de hover na mesma
posição física do mouse, causados pelo redimensionamento, também não contam. Mudar de menu/subtela,
desfixar ou recolher restaura o cabeçalho. Use data-hit-hide-controls="false"
nas telas de configuração. data-hit-view identifica mudanças de subtela.

O CSS público correspondente está em wwwroot/css/hit-menus.css. A política
de inatividade fica em wwwroot/js/hit-menu-chrome.js; a medição de escala,
em wwwroot/js/hit-menu-viewport.js. Ambos são reutilizáveis por novos menus.

O estado data-hit-idle-view na esfera identifica player ou lyrics durante a
ocultação. No Áudio, o player remove a faixa de ações secundárias e o espaço
do cabeçalho para ficar menor. Letras mantém a altura da janela e recupera
esse espaço para leitura. Reprodução, seek e versos continuam interativos.
Os temas podem ajustar --hit-menu-padding-top (64px) e
--hit-menu-idle-padding-top (24px), mantendo a compensação da tela de letras.

O estado de foco é animado por --hit-focus-progress, em 1200ms por padrão.
Para uma região que deve contrair em qualquer subtela, use
.hit-idle-collapse > .hit-idle-collapse-content com data-hit-collapse="always".
As ações dentro dela continuam marcadas com data-hit-idle-controls.
Não esconda a região com display:none: o grid deve passar de 1fr a 0fr.
Áudio usa marcadores player/lyrics separados para preservar a medição da leitura.

Todo objeto com menu por M2 declara data-hit-menu-trigger="identificador".
Alt realça esses objetos por contorno tracejado e sublinhado pontilhado.
Ações com Ctrl usam data-ctrl-link. Ambos os estados são compartilhados
pelo shell em hit-modifiers.js; não implemente rastreamento de teclas por menu.
Esses marcadores só descrevem a interação; o componente implementa seu callback.

Relógio segue o mesmo catálogo e ocupa a segunda bolinha. O contexto agora
inclui IsPinned e LayoutRequested. Somente a página selecionada pode pedir
HitMenuLayout.Compact; o host oculta cabeçalho/paginação e mede o mostrador.
Peça Default ao abrir controles/configuração. O Relógio permite M1 no
mostrador para controles e M2 para preferências, com dica Alt.
Para permitir clique e arraste no mesmo controle, o compacto declara
data-hit-drag-handle. O shell só captura o pointer após seis pixels; M1 simples
continua abrindo o menu, e soltar um arraste não dispara esse clique.
Controles internos (botões/campos) continuam excluídos do arraste.

Listas roláveis devem preferir HitScrollRegion para preservar a rolagem nativa
sem acionar a navegação do HIT. O uso de Clock, fusos e prazos está em
[Services/Clock/README.md](../../Services/Clock/README.md); as classes públicas
estão em [CLOCK-THEMES.md](../../Design/CLOCK-THEMES.md).
