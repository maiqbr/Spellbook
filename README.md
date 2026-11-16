# Spellbook

**Spellbook** é um utilitário Windows discreto na bandeja do sistema para duas tarefas simples: autoclick infinito e pequenos movimentos periódicos do mouse para manter o computador ativo.

![Logo do Spellbook](Assets/spellbook-logo.png)

## Recursos

- Não abre janela: fica somente no ícone da bandeja do sistema.
- Autoclicker infinito com botão esquerdo, direito ou do meio.
- Intervalo entre cliques por atalhos de menu ou valor personalizado, de 1 ms a 60 segundos.
- Modo **Manter ativo**, que move o mouse um pixel e retorna à posição original no intervalo escolhido.
- Todas as opções ficam no menu de clique com o botão direito do ícone.
- Opção para iniciar o Spellbook automaticamente junto com o Windows.
- Atalho global configurável para iniciar ou parar o autoclicker, mesmo com o menu fechado.
- Configurações salvas apenas neste computador; sem conta e sem conexão com a internet.

## Requisitos

- Windows 10 ou posterior.

Para executar a versão publicada, não é necessário instalar o .NET: o pacote de Release é autocontido.

## Instalação

Prefira baixar `Spellbook-Setup-vX.Y.Z-win-x64.exe` na página de Releases. O instalador cria o atalho no Menu Iniciar e pode criar um atalho na área de trabalho. O ZIP continua disponível como alternativa portátil.

## Uso

1. Abra o Spellbook. O ícone do grimório aparecerá na área de notificação do Windows; talvez seja necessário expandir a seta `^` ao lado do relógio.
2. Clique com o botão direito no ícone para abrir o menu.
3. Em **Autoclicker infinito**, escolha o botão, o intervalo e **Iniciar autoclicker infinito**. Ele continua clicando até você escolher a opção de parar.
4. Em **Manter ativo**, escolha a frequência e **Ativar micro movimentos**.
5. Marque **Iniciar com o Windows** se quiser que o Spellbook seja aberto automaticamente no próximo login.
6. Em **Atalho global do autoclicker**, clique no campo e pressione a combinação desejada; `F6` é o padrão.
7. Use as mesmas opções para interromper cada função ou escolha **Sair do Spellbook** para fechar o aplicativo.

## Desenvolvimento

É necessário ter o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.

```powershell
dotnet run
```

## Gerar um pacote para Windows

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
```

O executável gerado fica em `publish\win-x64\Spellbook.exe`. Para distribuir, compacte o executável em um `.zip` ou compile `Spellbook.iss` com o Inno Setup para gerar o instalador e anexe os arquivos a uma [GitHub Release](https://docs.github.com/repositories/releasing-projects-on-github/managing-releases-in-a-repository).

## Uso responsável

Use automação apenas onde ela for permitida. Alguns serviços, jogos e plataformas podem restringir ou detectar cliques automáticos e movimentos simulados.

## Licença

Copyright © 2026 Maiq.

Este projeto é licenciado sob a [GNU General Public License v3.0 ou posterior](LICENSE) (GPL-3.0-or-later). Isso permite usar, estudar, modificar e redistribuir o Spellbook, inclusive comercialmente, desde que obras distribuídas derivadas também sejam disponibilizadas sob a mesma licença, com o código-fonte correspondente.
