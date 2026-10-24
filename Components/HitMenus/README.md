# Menus do HIT

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
do tipo HitMenuContext, para consultar IsActive e chamar CloseRequested.
Configurações usa IsActive para pausar a leitura de RAM quando está oculto.

## Configurações e temas

As preferências são salvas em %LOCALAPPDATA%\HoldItTogether\settings.json.
Always on Top e System Tray são aplicados imediatamente. A RAM mostra
a memória residente privada do processo HIT (sem somar o WebView); em
sistemas antigos, usa a memória residente principal como alternativa.

Scroll speed permite controlar sensibilidade e cooldown, inclusive navegar
por vários menus com um único gesto. Remover animações atua apenas sobre CSS.
Themes importa/seleciona/exclui temas pessoais sem modificar o CSS original.

O padrão visual Obsidian Eyes, o contrato completo e exemplos estão em ../../Design/README.md.
Todos os menus devem seguir esse contrato e usar as variáveis públicas
sempre que possível.

A janela permanece fora da barra de tarefas. System Tray disponibiliza
Mostrar HIT, Ocultar HIT e Sair quando habilitado.