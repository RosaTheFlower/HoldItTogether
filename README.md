# Hold It Together (HIT)

O contrato principal de serviços, telas, configurações, interações e estilos está em [HIT-MENUS.md](HIT-MENUS.md). O formato futuro de módulos ZIP está planejado em [Design/MENU-PACKAGES.md](Design/MENU-PACKAGES.md); a importação de menus por ZIP ainda não está implementada.

Aplicativo desktop para Windows em .NET 8, Photino e Blazor, com uma esfera interativa,
menus descobertos automaticamente e temas CSS externos. O padrão visual é
HIT Obsidian Eyes.

## Executar

    dotnet restore
    dotnet run --project HoldItTogether.csproj

Requer Windows 10 1809 ou superior e o runtime WebView2. O aplicativo não aparece
na barra de tarefas; a bandeja do sistema pode ser habilitada em Configurações.

## Publicar

    dotnet publish HoldItTogether.csproj -c Release -o artifacts/publish

Distribua a saída inteira, incluindo wwwroot e as dependências nativas.

## Organização

- Components: esfera, controles compartilhados e menus Razor.
- Services: janela nativa, navegação, configurações, memória e temas.
- wwwroot: CSS público, JavaScript, página inicial e tema canônico.
- Design: padrão visual e contrato de temas.

Os menus são Áudio e Relógio, além do menu fixo Configurações na última posição.
Ele integra as sessões de mídia do Windows, com controles de reprodução,
letras sincronizadas com fontes alternativas, modo capa/cores e visualizador de fundo.
Consulte [Services/Audio/README.md](Services/Audio/README.md) para funcionamento e limites.
Relógio oferece relógios globais, temporizador e Pomodoro, com um
mostrador compacto da função selecionada após três segundos sem interação enquanto fixado ou ao clicar no relógio. Consulte
[Services/Clock/README.md](Services/Clock/README.md) e
[Design/CLOCK-THEMES.md](Design/CLOCK-THEMES.md) para serviço e estilos públicos.
Para adicionar menus, siga [Components/HitMenus/README.md](Components/HitMenus/README.md).
Para layout, estados, controles e temas, consulte [Design/README.md](Design/README.md).

As preferências e os temas importados ficam em
%LOCALAPPDATA%\HoldItTogether, fora dos arquivos do projeto.

## Versionamento

Os arquivos gerados, caches, saídas de publicação e preferências locais são
ignorados pelo .gitignore. O índice wwwroot/css/menus.css é gerado no build;
não precisa ser criado nem mantido manualmente.
O guia [Design/AUDIO-THEMES.md](Design/AUDIO-THEMES.md) detalha os seletores e
variáveis da página de Áudio e Letras. Ele também acompanha a saída compilada.
