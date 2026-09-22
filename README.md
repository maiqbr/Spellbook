# Spellbook

**Spellbook** é um aplicativo Windows para gravar e reproduzir automações de teclado e mouse. As sequências são organizadas como *feitiços* e podem ser acionadas por atalhos globais, por exemplo `Ctrl+Alt+K`.

![Logo do Spellbook](Assets/spellbook-logo.png)

## Recursos

- Grava eventos de teclado, cliques, movimentos e rolagem do mouse.
- Reproduz sequências com os intervalos originais entre as ações.
- Permite repetir uma sequência e definir um atalho global por feitiço.
- Mantém as automações no computador do usuário; não requer conta ou conexão com a internet para funcionar.
- Interface inspirada em fantasia sombria, com tons de café, ouro e violeta.

## Requisitos

- Windows 10 ou posterior.

Para executar a versão publicada, não é necessário instalar o .NET: o pacote de Release é autocontido.

## Uso

1. Abra o Spellbook e crie um novo feitiço.
2. Clique em **Gravar**.
3. Execute a sequência desejada fora da janela do aplicativo.
4. Retorne ao Spellbook e clique em **Parar gravação**.
5. Opcionalmente, informe um atalho como `Ctrl+Alt+K` e use **Testar** para conferir a sequência.

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

Este projeto ainda não possui uma licença. Antes de receber contribuições ou redistribuições, adicione uma licença apropriada ao repositório.
