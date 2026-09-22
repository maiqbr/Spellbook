# Spellbook

**Spellbook** é um aplicativo Windows para gravar e reproduzir automações de teclado e mouse. As sequências são organizadas como *feitiços* e podem ser acionadas por atalhos globais, por exemplo `Ctrl+Alt+K`.

![Logo do Spellbook](Assets/spellbook-logo.png)

## Recursos

- Grava eventos de teclado, cliques, movimentos e rolagem do mouse.
- Cria sequências manualmente, com teclas, cliques, movimentos, rolagem e pausas configuráveis.
- Reproduz sequências com os intervalos originais entre as ações.
- Inclui autoclicker com botão esquerdo, direito ou do meio; clique simples, duplo ou triplo; posição dinâmica ou fixa; intervalo, quantidade e repetição contínua configuráveis.
- Permite repetir uma sequência e definir um atalho global para iniciar ou parar a automação.
- Mantém as automações no computador do usuário; não requer conta ou conexão com a internet para funcionar.
- Interface inspirada em fantasia sombria, com tons de café, ouro e violeta.

## Requisitos

- Windows 10 ou posterior.

Para executar a versão publicada, não é necessário instalar o .NET: o pacote de Release é autocontido.

## Uso

1. Abra o Spellbook e crie um novo feitiço.
2. Escolha **Gravado**, **Autoclicker** ou **Manual**.
3. Para autoclicker, defina o botão, velocidade, posição e quantidade desejada. Para Manual, adicione cada ação no construtor.
4. Opcionalmente, informe um atalho como `Ctrl+Alt+K` e use **Testar** para conferir a sequência.

## Desenvolvimento

É necessário ter o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.

```powershell
dotnet run
```

## Gerar um pacote para Windows

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
```

O executável gerado fica em `publish\win-x64\Spellbook.exe`. Para distribuir, compacte o conteúdo dessa pasta e anexe o `.zip` a uma [GitHub Release](https://docs.github.com/repositories/releasing-projects-on-github/managing-releases-in-a-repository).

## Uso responsável

Automação pode executar cliques e teclas em qualquer aplicativo ativo. Revise cada sequência antes de usá-la e respeite as regras dos serviços, jogos e plataformas envolvidos.

## Licença

Copyright © 2026 Maiq.

Este projeto é licenciado sob a [GNU General Public License v3.0 ou posterior](LICENSE) (GPL-3.0-or-later). Isso permite usar, estudar, modificar e redistribuir o Spellbook, inclusive comercialmente, desde que obras distribuídas derivadas também sejam disponibilizadas sob a mesma licença, com o código-fonte correspondente.
