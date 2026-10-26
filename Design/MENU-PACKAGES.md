# Pacotes externos de Menus HIT — planejamento

**Status: proposta de arquitetura. Não existe instalador ZIP, watcher de pacotes,
registro dinâmico ou compilação de Razor em runtime nesta versão.**

O padrão implementado está em [HIT-MENUS.md](../HIT-MENUS.md). Este documento
define como evoluir sem misturar serviço, layout, tema e preferências.

## Experiência desejada

O usuário coloca um ZIP na pasta interna de importação. O HIT detecta que a
cópia terminou, valida/descompacta em área temporária, registra o pacote e
acrescenta seus menus à navegação. Configurações gerais continua por último.

O pacote fornece serviços e layout. Instalar um menu não deve alterar o tema
canônico, os arquivos do HIT ou as preferências de outro módulo. CSS e assets
do pacote ficam editáveis. Desabilitar um módulo preserva suas preferências.

## Estrutura proposta do ZIP

    MeuMenu.zip
      manifest.json
      README.md
      Services/
        Example.Services.dll
        Example.Services.deps.json
      Layout/
        Example.Layout.dll
        css/menu.css
        assets/icon.svg
      Dependencies/
        dependências privadas, quando necessárias
      Source/
        fontes opcionais do autor

Services concentra integração, comandos e modelos. Layout contém apresentação,
subtelas, configurações e recursos públicos. Os nomes seguem a estrutura
Services do projeto; a interface pode chamá-los de Serviços e Layout.

Exemplo de manifesto **proposto, ainda não aceito pelo aplicativo**:

    {
      "formatVersion": 1,
      "id": "org.example.agenda",
      "name": "Agenda",
      "version": "1.0.0",
      "hitContractVersion": "1",
      "entryAssembly": "Services/Example.Services.dll",
      "entryType": "Example.Services.AgendaModule",
      "menus": [
        {
          "id": "principal",
          "title": "Agenda",
          "componentAssembly": "Layout/Example.Layout.dll",
          "componentType": "Example.Layout.HitMenuAgenda",
          "styles": ["Layout/css/menu.css"]
        }
      ]
    }

IDs são estáveis: o identificador efetivo será pacote + menu, por exemplo
org.example.agenda:principal. Nome traduzido e versão não alteram a identidade.
Conflitos de ID devem ser reportados; não substitua menus internos.

## O que pode executar

### Primeira proposta: módulos .NET compilados

A evolução mais próxima do código atual é distribuir serviços .NET e
componentes Razor compilados, junto do CSS/JS/assets públicos. O pacote é
compilado pelo autor, contra uma versão definida do contrato HIT e o framework
suportado. O usuário final não precisa recompilar o HIT para instalá-lo.

Arquivos .razor/.cs soltos não são componentes executáveis pelo host atual.
Se quiser editar código-fonte do pacote, será necessário um fluxo de build
do módulo. Source é material de autoria; não é uma instrução para compilar
código automaticamente durante a importação.

Um pacote .NET executa com os privilégios do processo do HIT.
AssemblyLoadContext separa dependências; não é uma sandbox de segurança.
Para uma fronteira real de execução, um backend externo precisará de processo
separado e mensagens/DTOs bem definidos.

### Alternativa futura: layout declarativo

Um formato HTML/CSS com ações vinculadas a serviços poderia permitir alterações
de layout sem compilar Razor. Precisará definir componentes/expressões aceitos,
contrato de comandos, estado e ciclo de vida. HTML/CSS por si só não cria uma
integração nativa, nem fornece callbacks Blazor arbitrários.

Compilação de Razor/C# em runtime seria outra funcionalidade, com toolchain,
referências, diagnóstico e versionamento próprios. Não faz parte da importação
descrita como primeira etapa. Não prometa que renomear um HTML para Razor é suficiente.

## Contratos a extrair do aplicativo

Antes de carregar módulos externos, criar uma biblioteca compartilhada de
contratos, versionada e separada do executável. A API proposta deve definir:

- Descritor de módulo: ID, versão, menus e dependências.
- Registro de serviços do módulo e escopo de resolução.
- Contexto do menu: visibilidade, título, fechar e capacidades do host.
- Configuração opcional: IsSettingsOpen/OpenSettingsAsync e retorno.
- Serviços permitidos do host: preferências, diretório de dados, links e integrações expostas.
- Estados/DTOs assíncronos, cancelamento, erro e descarte.
- Registro de CSS/assets e resolução de caminhos relativos.

Esses contratos ainda não são APIs públicas de plugins. Os tipos atuais
HitMenuContext/IHitMenuSettings são o ponto de partida a preservar ou adaptar.
Não fazer o plugin referenciar tipos internos da janela Photino por conveniência.

## Pastas internas propostas

    %LOCALAPPDATA%/HoldItTogether/Modules/
      Inbox/                         ZIPs recebidos
      Staging/{operacao}/             Extração isolada, antes de registrar
      Installed/{id}/{version}/       Código/layout/assets do pacote instalado
      State/{id}/                    Preferências e dados do usuário
      Logs/                          Diagnóstico de instalação/execução

O tema distribuído continua em wwwroot/themes. Temas pessoais mantêm a pasta
gerenciada pelo HitThemeService. Conteúdo de pacote não substitui esses arquivos.

## Instalação e atualização

1. Aguardar arquivo estável/liberado para leitura; não extrair enquanto é copiado.
2. Ler o manifesto e conferir formato, IDs, caminhos, versões e duplicidades.
3. Extrair somente em Staging. Normalizar cada caminho; rejeitar destinos fora
   da pasta, caminhos absolutos, links e colisões de nomes. Impor limites de
   quantidade/tamanho descompactado antes de aceitar o pacote.
4. Conferir assemblies, recursos e compatibilidade do contrato/runtime.
5. Preparar serviços e descritores sem expor um menu parcialmente instalado.
6. Mover a versão preparada para Installed e publicar o registro de uma vez.
7. Acrescentar menus comuns, mantendo Configurações gerais por último.
8. Em falha, remover a área temporária e manter a versão anterior funcionando.

Uma atualização é instalada em uma pasta de versão nova. Preferências ficam
em State e precisam de migração versionada. Não sobrescrever DLLs carregadas
ou apagar o tema padrão para aplicar o estilo do módulo.

Watchers podem gerar eventos repetidos; a operação precisa ser idempotente.
Falhas devem ser identificáveis sem impedir o uso dos menus internos.

## Registro, carregamento e ciclo de vida

O HitMenuCatalog atual é estático e consulta o assembly do HIT.
Será necessário um registro observável que reúna menus internos e módulos,
notifique o host e mantenha seleção por ID, em vez de depender só do índice.

Proposta de carregamento .NET:

- Um AssemblyLoadContext por módulo e AssemblyDependencyResolver para dependências.
- Mapear assemblies declarados e dependências privadas ao layout do pacote; o .deps.json acompanha o assembly de entrada. O resolver padrão não procura qualquer subpasta arbitrária por conta própria.
- Compartilhar a mesma instância da biblioteca de contratos e assemblies do host.
- Evitar duplicar os tipos IComponent/contratos em contextos incompatíveis.
- Instanciar serviços do módulo num escopo controlado e descartável.
- Renderizar os tipos de componente registrados através do host.
- Mapear CSS/assets a caminhos próprios do módulo, sem reescrever o wwwroot interno.

Carregar uma nova versão e descarregar a anterior são operações diferentes.
Remover exige desmontar componentes, cancelar tarefas, remover eventos/JS,
descartar serviços e soltar referências a Type/Assembly/snapshots do módulo.
AssemblyLoadContext.Unload é cooperativo; não encerra threads ou referências
pendentes à força.

A primeira entrega pode instalar e ativar no próximo início do HIT.
A meta seguinte é adicionar/remover menus durante a execução. Esse segundo
passo exige o registro observável e descarte completo; não basta um watcher.

## Layout e configuração do módulo

Cada módulo segue o mesmo padrão do HIT:

- Tela principal com título no cabeçalho comum, sem duplicar Fixar/Fechar.
- Subtelas de leitura/detalhes com retorno explícito.
- Configuração do menu pelo M2 no título, com scroll bloqueado.
- Objetos configuráveis marcados com data-hit-menu-trigger e destaque Alt.
- Ações especiais com data-ctrl-link e validação real do gesto.
- Política de visibilidade/inatividade sem trabalho de fundo acidental.
- Escala/medição independentes da janela e movimento reduzido.
- Placeholders/erros quando uma integração não estiver disponível.

Layouts podem mudar HTML/estrutura porque pertencem ao módulo; temas CSS
personalizam apresentação dentro da estrutura existente. São recursos diferentes.

## Estilos e customização

CSS e assets de Layout são arquivos públicos editáveis, também após instalar.
O host deve carregar/remover estilos de cada pacote pelo seu ID e escopo.
Uma falha de CSS não pode apagar o estilo base do HIT.

O estilo local de um módulo será limitado ao seu contêiner. Ajustes de tipografia,
cor, fundo, estados de hover, foco, animação e subtelas usam variáveis públicas.
Temas globais continuam podendo personalizar menus, dentro do contrato.

Modificar a esfera/cabeçalho exige o contrato de tema global, separado do CSS
local do menu. Um módulo não deve sobrescrever todos os botões de outros menus
com seletores globais por acidente. JS de layout tem inicialização/descarte
definidos; o formato atual de tema CSS não executa JavaScript.

CSS deve refletir estado; não persiste preferências nem implementa comandos.
Definir um switch visual no CSS não cria um serviço. Configuração do módulo
precisa de modelo, validação, salvamento e apresentação próprios.

## Entregas sugeridas

1. Contratos de módulo e exemplo compilável, sem instalador automático.
2. Registro que combina menus internos e externos; instalação manual versionada.
3. Importação ZIP em Staging, erros, atualização/rollback e ativação no início.
4. Watcher/arrastar para Inbox e registro dinâmico sem reiniciar.
5. Desabilitar/remover/recarregar módulos com descarte verificado.
6. Ferramentas de autoria, validação de pacotes e, se decidido, layout declarativo.

Testar pacote incompleto, versão incompatível, ID duplicado, caminho escapando
da área de extração, update com serviço ativo, dependência divergente,
CSS inválido, cancelamento e remoção enquanto um menu está selecionado.

## Referências técnicas

- [Plugins .NET e dependências](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support).
- [Descarregamento cooperativo](https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability).

Essas referências embasam a proposta técnica. Elas não indicam que o carregador
ou o formato de manifesto já estejam implementados no HIT.
