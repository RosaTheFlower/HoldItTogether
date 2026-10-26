# Serviço do menu Relógio

A segunda página é `Components/HitMenus/HitMenuRelogio.razor`. Configurações gerais continua na última bolinha.

## Funções e configurações

- Sistema: horário e data do relógio principal; segundos opcionais e um segundo relógio discreto abaixo, inclusive no modo compacto.
- Temporizador: horas/minutos/segundos, iniciar, pausar, continuar e redefinir.
- Pomodoro: foco, pausa curta/longa, pausa/continuação, reinício e pular etapa. Padrão: 25/5/15 minutos e pausa longa a cada quatro focos.

M2 no **título do menu** abre segundos, exibição de dois relógios, som e parâmetros do Pomodoro.
M2 no **mostrador do sistema**, completo ou compacto, abre uma tela própria de relógios globais (`ClockSystemSettingsView`). É possível renomear, mudar o fuso, adicionar, remover e reordenar até 12 relógios. Pelo menos um permanece cadastrado.

O primeiro da lista é o principal; o segundo aparece quando `ShowTwoClocks` está ativo. Os demais ficam salvos para reorganização. O padrão contém Sistema e Nova York, com a exibição dupla inicialmente desativada. Remover um relógio promove o seguinte na ordem; se restar apenas um, só ele é mostrado.

`IsSettingsOpen` bloqueia a troca de menu por scroll nas duas telas de configuração. A lista de fusos tem rolagem própria via `HitScrollRegion` e `hit-local-scroll.js`.

A lista mostra linhas com nome, horário e posição (Principal/Secundário). `HitDisclosure` concentra nome/fuso e ações em uma expansão recolhida inicialmente; somente uma edição ou inclusão fica aberta. A edição acompanha o item ao reordenar. Adicionar recolhe e limpa o formulário após salvar com sucesso. Campos usam rótulos curtos e ícones SVG, conforme o padrão Obsidian.

## Fusos horários

`ClockTimeZones` usa as regras de `TimeZoneInfo` disponíveis no Windows, incluindo horário de verão na data apresentada. Não usa diferenças fixas de UTC nem altera o relógio do computador.

`local` acompanha o fuso do sistema. Outros itens usam um identificador de fuso; por exemplo, `E. South America Standard Time` para Brasília e `Eastern Standard Time` para Nova York. Nomes de exibição são editáveis e independentes do fuso.

A conversão parte do instante do snapshot. A data do mostrador principal também segue seu fuso, inclusive ao atravessar a meia-noite. A normalização descarta fusos inválidos, limita nomes a 40 caracteres e restaura um relógio local quando a lista fica inválida/vazia.

## Serviço, prazos e avisos

`HitClockService` é singleton e começa com o aplicativo. Seu worker processa conclusões a cada segundo, independentemente do menu, da esfera recolhida ou da bandeja. O componente mantém um ticker de apresentação apenas enquanto está visível.

Contagens em andamento guardam prazo UTC; pausadas guardam o restante. O restante é calculado pelo prazo, sem acumular erros de renderização. Uma etapa de foco concluída conta uma rodada; pular não conta. Depois de suspensão/atraso grande, o Pomodoro prepara a próxima etapa a partir do retorno. Novas durações preservam a etapa em andamento/pausada e passam a valer nas etapas seguintes.

`ClockNotifications` apresenta avisos também em outros menus. No compacto, uma marca discreta indica avisos pendentes sem cobrir os dígitos. `IClockSound` permite testes silenciosos; a implementação usa [MessageBeep do Windows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebeep), sujeito ao esquema/volume de sons do sistema.

Os avisos visuais ficam no HIT. O processo fechado não emite avisos; ao abrir, recupera prazos vencidos uma vez.

## Persistência e migração

Preferências ficam em `clock` de `settings.json`, via `HitSettingsService`: função, segundos, lista ordenada de relógios, exibição dupla, som e Pomodoro.

`%LOCALAPPDATA%\HoldItTogether\Clock\state.json` guarda temporizador, Pomodoro e até 20 avisos. A função Lembretes foi removida: perfis antigos descartam sua coleção e seus avisos ao carregar, preservando contagens e avisos de temporizador/Pomodoro. O antigo modo selecionado é normalizado para Sistema.

Gravações usam arquivo temporário e substituição; não há escrita por tick sem alterações. Falhas ao salvar conclusões mantêm o resultado em memória e tentam novamente sem repetir o som. Erros são apresentados na tela completa.

## Compactação e clique

Fixar mantém o menu completo. Após **3 segundos sem interação real**, ou M1 no mostrador completo enquanto fixado, o componente pede `LayoutRequested(Compact)`. O shell oculta cabeçalho e bolinhas e reduz as margens. A função selecionada continua ativa.

**Somente M1 no mostrador compacto reabre o menu**, sem desfixar. Hover não expande. Enter/Espaço também permitem abrir pelo teclado; M2 abre a configuração correspondente. Voltar de uma configuração mantém o menu completo e começa outro prazo de três segundos. Configurações nunca compactam por inatividade.

O mostrador completo usa `role="button"` e `data-no-drag`. O compacto usa `role="button"` e `data-hit-drag-handle`: permite segurar M1 e arrastar, inclusive sobre os dígitos, sem reabrir nem desfixar. A captura do pointer é adiada até deslocar seis pixels; um clique permanece no mostrador e reabre normalmente. Soltar um arraste não dispara abertura. M2 e teclado continuam disponíveis; hover não expande.

No compacto, o segundo relógio apresenta somente o horário pequeno, com `--clock-muted`, centralizado abaixo do principal. Seu nome continua na descrição acessível; a tela completa conserva o rótulo.

A raiz elegível anuncia `data-hit-idle-action="compact"` e `data-hit-idle-delay="3000"`. `hit-menu-chrome.js` observa interações reais e emite `hit-menu-idle`; `clock-menu.js` chama `CompactAfterInactivity`. Ticks não reiniciam o prazo. O padrão do Áudio permanece em cinco segundos.

## Estilos e validação

CSS público: `wwwroot/css/menus/HitMenuRelogio.css`; avisos: `wwwroot/css/clock-notifications.css`. Contrato: [CLOCK-THEMES.md](../../Design/CLOCK-THEMES.md); padrão geral: [HIT-MENUS.md](../../HIT-MENUS.md).

Validar tempo simulado, pausa/reinício, Pomodoro, suspensão, persistência, migração e avisos únicos. Na interface: M1 real com arraste conectado, M2 no título/mostrador, três segundos, hover sem expansão, scroll local/bloqueado, ordem e remoção de fusos, horário de verão, exibição dupla compacta/completa, segundos e temas. Testes usam dados em `obj`, sem alterar preferências do usuário.
