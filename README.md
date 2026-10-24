# Hold It Together (HIT)

Aplicativo desktop em .NET 8, Photino e Blazor, com uma esfera interativa,
menus descobertos automaticamente e temas CSS externos. O padrão visual é
HIT Obsidian Eyes.

## Executar

    dotnet restore
    dotnet run --project HoldItTogether.csproj

No Windows, o Photino utiliza o runtime WebView2. O aplicativo não aparece
na barra de tarefas; a bandeja do sistema pode ser habilitada em Configurações.

## Publicar

    dotnet publish HoldItTogether.csproj -c Release -o artifacts/publish

Distribua a saída inteira, incluindo wwwroot e as dependências nativas.

## Organização

- Components: esfera, controles compartilhados e menus Razor.
- Services: janela nativa, navegação, configurações, memória e temas.
- wwwroot: CSS público, JavaScript, página inicial e tema canônico.
- Design: padrão visual e contrato de temas.

Existe uma página de exemplo, Início, além do menu fixo Configurações.
Para adicionar menus, siga [Components/HitMenus/README.md](Components/HitMenus/README.md).
Para layout, estados, controles e temas, consulte [Design/README.md](Design/README.md).

As preferências e os temas importados ficam em
%LOCALAPPDATA%\HoldItTogether, fora dos arquivos do projeto.

## Versionamento

Os arquivos gerados, caches, saídas de publicação e preferências locais são
ignorados pelo .gitignore. O índice wwwroot/css/menus.css é gerado no build;
não precisa ser criado nem mantido manualmente.