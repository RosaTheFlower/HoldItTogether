# HIT — padrão de design e implementação

Contrato de menus/serviços/telas: [HIT-MENUS.md](../HIT-MENUS.md). Evolução para módulos externos: [MENU-PACKAGES.md](MENU-PACKAGES.md), ainda planejada.

Esta é a referência para criar telas, controles e temas. O padrão oficial é
**HIT Dark Theme** (anteriormente Obsidian Eyes), definido em:

- ../wwwroot/themes/HIT-OBSIDIAN-EYES.css — fonte canônica, distribuída e protegida.
- ../wwwroot/css/theme-base.css — estrutura compartilhada de cabeçalho, seções e diálogos.
- ../wwwroot/css/menus/ — estrutura pública de cada página.

O aplicativo distribui três temas protegidos, registrados em
Services/Themes/HitBundledThemes.cs:

| Nome na interface | Identificador persistido | CSS público |
| --- | --- | --- |
| HIT Dark Theme | default | HIT-OBSIDIAN-EYES.css |
| HIT OLED | bundled:HIT-OLED-OFF-WHITE.css | HIT-OLED-OFF-WHITE.css |
| HIT Light Theme | bundled:HIT-LIGHT.css | HIT-LIGHT.css |

Os arquivos ficam em wwwroot/themes e acompanham build/publicação. Os nomes
internos do Dark/OLED preservam compatibilidade. O catálogo é explícito:
arquivos antigos deixados na saída não viram opções automaticamente.
O botão Excluir fica desabilitado para os três, e o serviço também rejeita
a exclusão. Uma cópia importada continua sendo um tema pessoal independente.

Dark permanece como seleção inicial, base de temas parciais e fallback.
OLED usa preto #000000 e off-white #f2efe7. Light usa superfícies claras e
texto escuro, preservando olhos, layout, movimentos e interações.

### Prioridade da capa e paleta musical

Todos os temas devem preservar as opções de capa, blur e cores da música.
Não esconda .sphere-audio-backdrop/.audio-artwork-wash nem fixe as variáveis
--audio-art-* com !important. Não sobrescreva --audio-foreground/muted/accent/surface
em .audio-menu[data-audio-palette="true"] com a paleta fixa do tema.
No conteúdo, use --audio-* com fallback para --hit-menu-*/--hit-*.
O shell aplica as cores globais por data-audio-theme="global". O Relógio
herda texto/destaque via --hit-menu-text, --hit-menu-muted e --hit-accent.
Desligar a opção da música restaura o tema escolhido; escolher OLED não
desliga nem neutraliza a capa ou a paleta musical.

Há menus de Áudio e Relógio, além de Configurações. Menus e temas antigos não fazem parte do build. Não recrie variantes
distribuídas sem uma decisão explícita de produto.

## Direção visual

HIT é uma superfície pequena de desktop: informação curta, espaço tranquilo,
pouca decoração e controles que não disputam atenção com o conteúdo.

| Elemento | Padrão |
| --- | --- |
| Superfície aberta | Grafite #17191e |
| Texto principal | #e8ebf0 a #f1f2f4 |
| Texto secundário | #9299a5 |
| Separadores | Branco com 3–5% de opacidade |
| Destaque | Branco frio #d7dfec; sem cores saturadas por padrão |
| Tipografia | Segoe UI Variable / Segoe UI / sans-serif |
| Valores numéricos | Cascadia Mono / Consolas / monospace |
| Título de página | 14 px, peso 600, no cabeçalho comum |
| Rótulos de controles | 12 px, peso 500 |
| Informação auxiliar | 10–11 px |
| Cantos da janela aberta | 22 px |
| Botões e campos | Cantos de 7–9 px, bordas discretas |
| Movimento | Resposta rápida, desaceleração longa; nunca bloquear interação |

Use variáveis de cor e fonte. Evite subtítulos para rótulos autoexplicativos.
Não adicione brilhos fortes, cartões desnecessários ou animações contínuas
dentro das páginas.

## Responsabilidades

- DynamicSphere controla abrir/fechar, fixação, arraste e interação nativa.
- HitMenuHost descobre páginas, preserva estado, mostra bolinhas e troca por scroll.
- Cada HitMenu*.razor contém apenas o conteúdo daquela página.
- O título é fornecido ao cabeçalho comum pelo catálogo. Não replique Fixar/Fechar.
- HitDisclosure é o componente reutilizável de seção expansível.
- HitThemePicker concentra seleção/importação/exclusão e liberação de foco do seletor.
- HitThemeManager aplica o tema padrão e, depois, os estilos do tema pessoal.

O CSS de um tema não define regras de negócio nem muda o HTML do aplicativo.

## Seções expansíveis

A estrutura de cada seção é sempre:

**Rótulo → controle principal → seta**, todos na mesma linha.

Ela começa recolhida. A seta tem nome acessível, aria-expanded e aria-controls;
apenas o conteúdo adicional é ocultado. Os controles principais continuam
acessíveis. Use o componente compartilhado, em vez de duplicar a estrutura:

    <HitDisclosure Title="Exemplo" PanelId="example-options">
        <MainControl>
            <!-- Switch ou seletor principal -->
        </MainControl>
        <ChildContent>
            <!-- Opções avançadas -->
        </ChildContent>
    </HitDisclosure>

Use um PanelId único por seção. O tamanho da janela acompanha a expansão
pelo ResizeObserver, sem larguras dependentes da própria viewport.
Para coordenar uma lista de seções, passe Expanded e trate ExpandedChanged.
Sem Expanded, o componente mantém seu próprio estado, como antes.

### Configurações

A ordem é:

1. Always on Top.
2. System Tray.
3. Remover animações.
4. Mostrar tooltips.
5. Scroll speed: cooldown visível; sensibilidade e pausa dentro da expansão.
6. RAM em uso: uma linha de 10 px, sem subtítulo, atualizada só com a página ativa.
7. Temas: seletor visível; ações dentro da expansão.

Importar CSS, Abrir pasta e Excluir ficam **lado a lado numa única linha**,
com três colunas iguais ocupando toda a largura disponível.

Após escolher um tema, o seletor libera o foco e a roda volta a navegar
pelos menus. Não bloqueie wheel na seção de Temas.
Sliders podem interromper a propagação para permitir ajustes sem trocar de página.

## Exclusão de temas

Excluir inicia uma confirmação; nunca apaga no primeiro clique.

- Use um dialog modal nativo do HTML, com título e nome do tema.
- Cancelar recebe foco inicial. Escape cancela sem fechar o HIT.
- O navegador mantém o foco dentro do modal e o restaura ao fechar.
- A roda não troca de página enquanto o diálogo está aberto.
- Excluir exige um segundo clique, em um botão distinto.
- Se a seleção mudar ou a seção recolher, descarte a confirmação pendente.
- Vincule a confirmação ao identificador original, não apenas ao nome visível.
- Os três temas embutidos não podem ser apagados pela interface nem pelo serviço.
- Ao apagar o tema pessoal ativo, restaure HIT Dark Theme antes de remover o arquivo.

## Criar páginas

Crie Components/HitMenus/HitMenuAgenda.razor:

    @attribute [HoldItTogether.Services.HitMenus.HitMenuTitle("Agenda")]

    <section class="agenda-menu">
        <h2>Hoje</h2>
        <p>Seu conteúdo.</p>
    </section>

O título pode ser omitido: o nome do arquivo fornece um título automaticamente.
Crie o CSS público em wwwroot/css/menus/HitMenuAgenda.css:

    :where(.hit-menu-panel[data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAgenda"]) .agenda-menu {
        width: 440px;
        color: var(--hit-menu-text, var(--hit-text));
    }

Repita o prefixo nas regras da página; :where deixa a especificidade baixa.
Não use isolamento .razor.css. O build encontra esses arquivos públicos e
gera css/menus.css sem registros manuais. Veja também
../Components/HitMenus/README.md.

## Contrato de temas — versão 1

Um arquivo .css, de até 1 MB, contém uma ou ambas as áreas:

    @layer sphere {
        :root {
            --hit-open-background: #17191e;
            --hit-open-radius: 22px;
            --sphere-text: #f1f2f4;
        }
        .sphere-title { font-size: 14px; }
    }

    @layer menus {
        :root {
            --hit-menu-font: "Segoe UI", sans-serif;
            --hit-menu-text: #e8ebf0;
            --hit-menu-muted: #9299a5;
        }
        h2 { color: #e8ebf0; }
    }

- sphere atua sobre esfera, cabeçalho, ações e paginação.
- menus é automaticamente limitado ao conteúdo das páginas.
- :root representa a raiz de cada área, não o documento inteiro.
- Estilos pessoais são aplicados depois do padrão; a especificidade normal
  do CSS continua valendo. Use as classes específicas ao substituir uma regra
  mais específica do tema padrão.
- O tema padrão permanece carregado por baixo de temas pessoais, para que
  arquivos parciais herdem a identidade Obsidian.
- Se o arquivo ativo desaparecer, o HIT restaura o padrão e avisa.
- Um tema de formato inválido preserva o último estilo pessoal válido e informa o erro.
- Selecionar/importar/excluir nunca escreve nem apaga o CSS padrão.
- Arquivos pessoais ficam em %LOCALAPPDATA%\HoldItTogether\Themes.
- Após importar, edite a cópia nessa pasta; o HIT relê o tema a cada segundo.
- Não há necessidade de recompilar para alterar um tema pessoal.

São aceitos seletores, pseudoelementos, variáveis, @media, @supports,
@keyframes e @font-face dentro das áreas. Não use CSS nesting ou @import.
Fontes/imagens podem usar recursos instalados, URLs HTTPS absolutas ou data:
URLs. Caminhos relativos à pasta pessoal não são servidos pelo WebView.

## Os olhos do HIT

O tema padrão usa ::before e ::after como dois olhos brancos. Eles substituem
os reflexos/contorno anteriores e somem ao abrir o menu.

A camada sphere declara --hit-track-pointer: 1. O aplicativo fornece:

| Variável | Valor |
| --- | --- |
| --hit-pointer-x | Direção horizontal, -1 a 1 |
| --hit-pointer-y | Direção vertical, -1 a 1 |
| --hit-pointer-abs-x | Intensidade horizontal, 0 a 1 |
| --hit-pointer-abs-y | Intensidade vertical, 0 a 1 |

No Windows, a esfera recolhida acompanha o cursor também fora da janela.
O loop nativo existente fornece até 20 atualizações por segundo; só envia
direções alteradas, arredondadas, e o CSS suaviza os movimentos.
Com o menu aberto, o rastreamento nativo específico do tema é pausado.
Em outros sistemas, os eventos disponíveis são os do WebView.

Um tema pode declarar --hit-track-pointer: 0 para desligar a solicitação.
Não adicione JavaScript executável aos arquivos de tema.

Remover animações tem prioridade sobre os temas: elimina animações e
transições, incluindo piscadas. A posição dos olhos continua sendo atualizada.

## Checklist ao mudar a interface

- Títulos alinhados com Fixar/Fechar.
- Nenhum texto selecionável e nenhum scroll de página.
- Primeiro menu inicial e Configurações sempre na última bolinha.
- Seções recolhidas, teclado acessível e controles principais sempre visíveis.
- Scroll funcionando após usar o seletor de temas.
- Expansão/recolhimento sem loops de redimensionamento.
- Modal com Cancelar, Escape, foco e segundo clique de exclusão.
- Padrão protegido e aplicado novamente após remover um tema pessoal.
- Mesmos estados com e sem animações.
- CSS e temas presentes na saída compilada/publicada.
## Inicialização

index.html mostra uma versão estática de 56 × 56 px do HIT Obsidian, com dois
olhos brancos e 24 px de margem, ocupando os mesmos 104 × 104 px da janela
inicial. O placeholder não aceita cliques, foco, seleção ou animações.
Blazor substitui o conteúdo de app pela esfera funcional ao montar a aplicação.
O CSS dessa apresentação inicial fica em wwwroot/css/loading.css.

## Arquivos para versionar

Versione código-fonte, CSS público, os três temas embutidos, projeto/solução e documentação.
bin, obj, artifacts, preferências da IDE e o índice gerado css/menus.css são
ignorados. O build gera novamente esse índice e copia os recursos públicos
para a saída. Não mantenha cópias exportadas do tema: o arquivo canônico já pode
ser copiado/importado quando necessário.
## Mini-player de áudio

A página Áudio substitui Início e segue a mesma estrutura de título, medição e
paginação. Não adicione controles de janela dentro do componente do player.

- Capa de 64 px, título em 15 px e artista em 11 px; metadados longos são truncados.
- Timeline acima dos controles principais: aleatório, anterior, play/pause, próxima, loop.
- Ações secundárias: modo capa/cores/compacto à esquerda; letras e visualizador à direita.
- Letras têm tela própria, com retorno por M2/Escape/Voltar. Mantêm as dimensões do player; o espectro fica ao fundo.
- Controles indisponíveis no aplicativo de origem ficam desabilitados.
- As letras só são habilitadas quando o serviço encontra conteúdo sincronizado.
- O visualizador mostra dados reais. Não use números aleatórios ou animações decorativas.
- A captura para fora de vista. O polling só continua no modo de cores globais, a cada dois segundos.
- M2 no modo capa abre blur, cores globais e subopção de capa nos outros menus.
- O painel da capa também permite imagem no fundo das Letras; a opção é
  independente do alcance global e preserva blur, legibilidade e dimensões.
- Mostrar tooltips é uma preferência global dos controles HTML, preservando aria-label.
- Todo CSS permanece público em css/menus/HitMenuAudio.css, com escopo estável.

HitMenuContext.IsActive indica o menu selecionado; IsVisible também considera
a esfera aberta. Recursos de mídia devem consultar ambos para suspender trabalho.
O contrato específico de estilos, classes, paleta da capa e animações está em
[AUDIO-THEMES.md](AUDIO-THEMES.md). Ele deve acompanhar novas mudanças da página.
## Configurações específicas dos menus

O padrão opcional é IHitMenuSettings: M2 no título abre a configuração do
menu atual, inclusive de suas subtelas. Configurações gerais não adota o
atalho. O contrato e o exemplo para novos menus estão em
[Components/HitMenus/README.md](../Components/HitMenus/README.md).

Áudio oferece tamanho uniforme ou X/Y individualmente, ocultação de ações
após cinco segundos sem interação enquanto fixado e Mostrar fonte.
Mantenha o formulário compacto e a escala do formulário em 100%. A escala
escolhida vale para player e letras, que devem continuar com medidas iguais.
Ao ocultar ações no player, remova o espaço reservado e compacte a janela.
Em Letras, mantenha a janela e amplie a leitura para ocupar o espaço liberado.
O CSS dessas opções continua público e usa as cores do tema/paleta atual.

A interface IHitMenuSettings também expõe IsSettingsOpen; true bloqueia
scroll entre menus automaticamente. Áudio inclui nessa regra suas
configurações e a personalização da capa. Voltar libera a navegação.
CloseSettingsAsync completa o contrato: M2 no conteúdo de uma configuração
aberta retorna ao menu. O host preserva o gesto de abrir pelo título/objeto e
não desfaz a fixação. Gatilhos de objeto usam stopPropagation no contextmenu.

Interações com Ctrl: títulos de músicas e a fonte de letras usam
data-ctrl-link. O cursor vira uma mão e o texto ganha sublinhado enquanto
Ctrl estiver pressionado. Clique simples mantém o comportamento normal.

## Movimento de foco e indicação de configuração

O foco por inatividade leva 1200ms, com início rápido e desaceleração longa.
O layout contrai gradualmente as regiões secundárias e o espaço do cabeçalho;
Letras recupera esse espaço usando o mesmo progresso, sem aumentar a janela.
Ao voltar a interagir, a transição parte da posição atual, sem salto.
O fade de ações/título dura 450ms. As opções de movimento reduzido têm prioridade.

Estilize por --hit-focus-duration, --hit-focus-ease,
--hit-focus-fade-duration e pelas duas margens de cabeçalho documentadas.
O tamanho nativo acompanha a animação do conteúdo; não duplique uma transição
de height nesse intervalo. Não troque as regiões animadas por display:none.

Ctrl revela data-ctrl-link com sublinhado contínuo. Alt revela
data-hit-menu-trigger com sublinhado pontilhado e contorno tracejado.
Ícones precisam do contorno porque não possuem texto para sublinhar.
Use --hit-link-hint-color / --hit-menu-hint-color para ajustar as cores.
O título de Configurações gerais não anuncia um menu próprio.

Planeje tela principal, detalhes, configuração do menu e configuração de
objetos conforme [HIT-MENUS.md](../HIT-MENUS.md). Temas continuam controlando
apresentação; serviços/layout externos pertencem ao futuro formato de módulos.

## Menu Relógio e apresentação compacta

Relógio é a segunda bolinha; Configurações gerais continua sendo a última.
O conteúdo principal escolhe entre Sistema, Temporizador e Pomodoro.
M2 no título abre segundos, dois relógios, som e parâmetros do Pomodoro. M2 no mostrador do sistema abre fusos: nomes, adição, remoção e ordem. As duas telas bloqueiam navegação por scroll.

Enquanto fixado, o menu principal pede HitMenuLayout.Compact após três segundos sem interação ou ao clicar no mostrador.
O shell retira cabeçalho/paginação e usa margens compactas. O mostrador mantém
somente a função selecionada. M1 reabre controles, M2 configura e Alt revela
esse gesto. Hover não expande o compacto. O mostrador compacto permite arrastar:
M1 sem deslocamento abre, enquanto segurar e mover reposiciona sem abrir ao soltar.
O relógio secundário compacto exibe apenas os dígitos pequenos e muted,
centralizados abaixo do principal; o nome permanece disponível à acessibilidade.

A configuração do sistema usa linhas sem cartões, com nome, prévia e seta.
Cada relógio e a inclusão começam recolhidos, usando HitDisclosure; somente
uma edição fica aberta. Nome/fuso e ações de ordem/remoção aparecem na expansão.
Ao reordenar, a edição acompanha o relógio. Depois de adicionar, recolha o formulário.

O estilo do Relógio continua separado e público. Contrato de classes e
variáveis: [CLOCK-THEMES.md](CLOCK-THEMES.md).
