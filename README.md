# Spellbook

Aplicativo Windows para gravar e reproduzir automações de teclado e mouse, armazenadas localmente como **feitiços**. Cada feitiço pode ter um atalho global, como `Ctrl+Alt+K`.

## Executar durante o desenvolvimento

```powershell
dotnet run
```

## Gerar a versão para compartilhar

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
```

O executável estará em `publish\win-x64\Spellbook.exe`. Para publicar, compacte essa pasta em `.zip` e envie-a para uma Release do GitHub, Itch.io ou Google Drive. O app pode ser usado offline; os feitiços são gravados em `%AppData%\Spellbook\spells.json`.

> Atenção: automações podem acionar cliques e teclas em qualquer programa. Revise a sequência antes de usá-la em tarefas sensíveis e não a utilize para violar regras de serviços ou jogos.
