# HIT — padrão de design e implementação

Esta é a referência para criar telas, controles e temas. O padrão oficial é
**HIT Obsidian Eyes**, definido em:

- ../wwwroot/themes/HIT-OBSIDIAN-EYES.css — fonte canônica, distribuída e protegida.
- ../wwwroot/css/theme-base.css — estrutura compartilhada de cabeçalho, seções e diálogos.
- ../wwwroot/css/menus/ — estrutura pública de cada página.

Há uma página de exemplo (Início), além de Configurações. Menus e temas antigos não fazem parte do build. Não recrie variantes
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

### Configurações

A ordem é:

1. Always on Top.
2. System Tray.
3. Remover animações.
4. Scroll speed: cooldown visível; sensibilidade e pausa dentro da expansão.
5. RAM em uso: uma linha de 10 px, sem subtítulo, atualizada só com a página ativa.
6. Temas: seletor visível; ações dentro da expansão.

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
- O tema padrão não pode ser apagado pela interface.
- Ao apagar o tema pessoal ativo, restaure Obsidian Eyes antes de remover o arquivo.

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

Versione código-fonte, CSS público, tema canônico, projeto/solução e documentação.
bin, obj, artifacts, preferências da IDE e o índice gerado css/menus.css são
ignorados. O build gera novamente esse índice e copia os recursos públicos
para a saída. Não mantenha cópias exportadas do tema: o arquivo canônico já pode
ser copiado/importado quando necessário.