# Temas da página Relógio

O Relógio usa o contrato geral de [HIT-MENUS.md](../HIT-MENUS.md).
Todos os estilos ficam em arquivos CSS públicos, copiados para build/publish:

- wwwroot/css/menus/HitMenuRelogio.css: página, mostradores, controles e configurações.
- wwwroot/css/clock-notifications.css: aviso global dentro do shell.
- wwwroot/css/hit-menus.css: margens do layout compacto compartilhado.

## Escopo

A página é identificada por:

    .hit-menu-panel[data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuRelogio"]

Na camada menus de um tema importado, use classes relativas do Relógio.
O HIT acrescenta o escopo das páginas automaticamente. Não use seletores
globais de tags se quiser modificar somente esta página.

| Seletor | Finalidade |
| --- | --- |
| .clock-menu | Raiz, largura padrão de 360px |
| .clock-menu.is-compact | Somente o mostrador fixado |
| [data-clock-view="main"] | Conteúdo principal |
| [data-clock-view="settings"] | Configuração do menu |
| [data-clock-view="systemsettings"] | Configuração de relógios globais por M2 no mostrador |
| .clock-tabs / .clock-tab.is-current | Escolha de função |
| .clock-readout | Mostrador |
| .clock-readout[data-clock-mode="system"] | Relógio do sistema |
| .clock-readout[data-clock-mode="timer"] | Temporizador |
| .clock-readout[data-clock-mode="pomodoro"] | Pomodoro |
| .clock-readout.is-alert | Aviso pendente da função |
| .clock-digits | Dígitos com largura tabular |
| .clock-secondary / .clock-secondary-label / .clock-secondary-digits | Segundo relógio; no compacto, somente dígitos centralizados e muted |
| .clock-readout-label / .clock-readout-detail | Contexto da tela completa |
| .clock-compact-trigger | Abrir controles com M1 ou configurar com M2 |
| .clock-full-trigger | Fixado: clicar no mostrador para compactar; M2 configura |
| .clock-duration | Entrada de horas/minutos/segundos |
| .clock-progress / .clock-progress span | Progresso real da etapa Pomodoro |
| .clock-button / .clock-button.is-primary | Ações |
| .clock-settings / .clock-setting-row / .clock-disclosure | Preferências |
| .clock-switch / .clock-setting-number | Interruptores e valores |
| .clock-system-settings / .clock-world-list | Configuração de fusos e lista com scroll local |
| .clock-world-entry / .clock-world-preview / .clock-world-position | Linha recolhida: nome, horário e posição |
| .clock-world-entry .disclosure-content / .clock-world-actions | Edição, ordenação e remoção na expansão |
| .clock-world-icon / .clock-world-remove | Ações com ícones SVG |
| .clock-world-add / .clock-world-count / .clock-world-field | Inclusão recolhida, quantidade e campos de nome/fuso |
| .clock-error | Erros de validação/persistência |

## Variáveis

Defina em .clock-menu para um tema apenas do Relógio:

| Variável | Padrão/função |
| --- | --- |
| --clock-text | Texto herdado de --hit-menu-text / --hit-text |
| --clock-muted | Apoio herdado de --hit-menu-muted / --hit-muted |
| --clock-accent | Destaque herdado de --hit-accent |
| --clock-surface | Contraste herdado de --hit-open-background |
| --clock-digit-size | 48px; mostrador completo |
| --clock-compact-digit-size | 34px; mostrador fixado |
| --clock-secondary-digit-size | 20px; relógio secundário completo |
| --clock-compact-secondary-digit-size | 14px; relógio secundário compacto |
| --clock-progress | Porcentagem calculada na barra Pomodoro |

O shell oferece --hit-compact-padding-x (18px) e
--hit-compact-padding-y (14px). Defina essas margens na camada sphere.
Elas não são as margens do modo de foco do Áudio.

## Exemplo

    @layer menus {
        .clock-menu {
            --clock-accent: #a5d7ef;
            --clock-digit-size: 46px;
            --clock-compact-digit-size: 32px;
        }
        .clock-tabs { border-radius: 12px; }
        .clock-button { border-radius: 10px; }
    }

Para alterar só o relógio do sistema:

    @layer menus {
        .clock-readout[data-clock-mode="system"] .clock-digits {
            font-family: "Segoe UI Variable", "Segoe UI", sans-serif;
            font-weight: 300;
        }
    }

O CSS altera aparência, não altera fusos, contagens ou a função selecionada.
Os dígitos reais são atualizados pelo serviço/componente; não substitua texto
por pseudo-elementos com horário fictício.

## Compacto, configuração e avisos

Fixado, o Relógio pede layout Compact ao host após três segundos sem interação ou ao clicar no mostrador. .hit-menu-host.is-compact
ajusta margens; .dynamic-sphere.is-menu-compact identifica a janela nesse
estado. O título e as bolinhas ficam hidden/inert conforme necessário.
Não restaure os controles com CSS para tentar desfixar a janela.

Os avisos globais ficam fora de .hit-menu-panel. Personalize pela camada sphere:

    @layer sphere {
        .clock-notice {
            border-color: #a5d7ef44;
            border-radius: 14px;
        }
    }

A paleta global da música também pode colorir o Relógio quando ativada.
Remover animações e prefers-reduced-motion anulam transições, incluindo
a barra de progresso, sem parar os contadores. Os gestos Ctrl/Alt e a
acessibilidade seguem o contrato compartilhado.

A tela principal fixada anuncia data-hit-idle-action="compact" ao shell.
data-hit-idle-delay="3000" define os três segundos do Relógio, sem alterar o prazo do Áudio.
Esse atributo é retirado no modo compacto e nas duas telas de configuração.
Ele não se confunde com data-hit-hide-controls do Áudio: o evento solicita uma
mudança de layout do componente, sem ocultar controles por CSS.
M1 no compacto reabre o menu mantendo a fixação; hover não reabre sozinho.
O gatilho completo usa role="button" e data-no-drag. O compacto usa
data-hit-drag-handle: clique abre e arraste move sem abrir ao soltar.
O cursor grab/grabbing indica o gesto. A captura só começa após seis pixels,
preservando o destino do clique; não implemente arraste por CSS ou por tema.
M2 no mostrador do sistema abre relógios globais; M2 no título abre configurações gerais.
A exibição dupla vale também no compacto, sem mudar a hierarquia visual do relógio principal.
No compacto, não há nome visível do secundário: somente horário pequeno,
centralizado e com --clock-muted. O nome continua na descrição acessível.
As linhas de configuração usam HitDisclosure, sem cartões extras; títulos,
prévias e setas ficam alinhados. Edição e inclusão começam recolhidas.
