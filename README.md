# RobloxServerLauncher

O **RobloxPlayerLauncher.exe** é o launcher oficial do site **[RobloxServer](https://github.com/V0rtexLinux/RobloxServer)**,
no estilo do launcher do roblox.com de 2013: você clica em **Play** no site e o jogo abre.
Ele só joga os clientes **2012M** e **2013M**.

> Esta versão foi reescrita do zero e não usa mais o Novetus. Não existe mais Server Browser nem mapas locais:
> tudo vem do site.

## Como jogar

1. No site, clique em **Download ROBLOX** e rode o `RobloxPlayerLauncher.exe` uma vez. Ele se instala em
   `%LocalAppData%\RobloxServer` e registra o protocolo `robloxserver-player:` (sem pedir administrador).
2. Abra um jogo no site e clique em **Play**.
3. Na primeira vez, o launcher pergunta se você confia no site. Depois ele baixa o cliente do jogo (2012M ou 2013M)
   do próprio site, confere o SHA-256 e abre o jogo.

Para **hospedar** um servidor, clique em **Host Server** na página do jogo. A janela *ROBLOX Game Server* fica aberta
enquanto o servidor roda; fechar a janela desliga o servidor e o remove do site. Libere a porta **UDP 53640**
no roteador (ou no Raspberry Pi do RobloxServer) para jogadores de fora da sua rede.

## Como funciona

```
Site (Play)  ── GET /Game/GetAuthTicket.ashx ──►  ticket de uso único
     │
     └── robloxserver-player:1+launchmode:play+gameinfo:TICKET+placeid:ID+baseurl:URL
                                   │
RobloxPlayerLauncher.exe ──────────┘
  1. /Login/Negotiate.ashx?suggest=TICKET        → cookie .ROBLOSECURITY só do launcher
  2. /Game/PlaceLauncher.ashx?request=RequestGame → servidor, joinScriptUrl e cliente (2012M/2013M)
  3. /install/version.ashx?client=2012M           → versão + SHA-256; baixa /install/download.ashx se mudou
  4. /Game/Join.ashx?jobId=                       → script Lua assinado (--rbxsig) com um ticket novo
  5. inicia o cliente:  RobloxApp_client.exe -script "content\scripts\robloxserver_join_xxxx.lua"
```

Com **Host Server** o launcher registra o servidor em `/Game/Servers.ashx`, baixa o place, pega o script de
`/Game/GameServer.ashx`, abre o cliente como servidor e manda heartbeats até ele fechar. O script do servidor
confere o ticket de cada jogador em `/Game/ValidateTicket.ashx` e expulsa quem não tem ticket válido.

O launcher também se atualiza sozinho: se o site tiver um `RobloxPlayerLauncher.exe` diferente em
`App_Data/Launcher`, ele baixa, confere o SHA-256 e passa o link para a versão nova.

## Pacotes de cliente (para o dono do site)

O site serve um zip por cliente em `App_Data/Clients/2012M.zip` e `App_Data/Clients/2013M.zip`. A página *Admin*
mostra quais estão instalados. Trocar o zip faz todos os launchers baixarem a versão nova.

O zip tem a pasta do cliente (os `.exe` e a pasta `content`). Sem configuração, o launcher procura
`RobloxApp_client.exe`, `RobloxApp_server.exe`, `RobloxPlayerBeta.exe`, `RobloxPlayer.exe` ou `RobloxApp.exe` e usa:

| Modo | Argumentos padrão |
| --- | --- |
| Jogar | `-script "{script}"` |
| Servidor | `"{place}" -script "{script}"` |

Para outro layout, coloque um `RobloxServerClient.json` na raiz do zip:

```json
{
  "PlayerExe": "RobloxPlayerBeta.exe",
  "ServerExe": "RobloxApp_server.exe",
  "PlayerArgs": "-script \"{script}\"",
  "ServerArgs": "\"{place}\" -script \"{script}\"",
  "ServerLoadsPlace": false
}
```

Variáveis: `{script}` (caminho do Lua gerado), `{scriptasset}` (`rbxasset://scripts/...`), `{place}` (arquivo do
place, só no servidor), `{port}`, `{baseurl}`. Com `"ServerLoadsPlace": true` (ou sem `{place}` nos argumentos)
o servidor carrega o place pelo `game:Load` do script, em vez de receber o arquivo.

O cliente precisa aceitar scripts locais pelo `-script` (os clientes 2012M/2013M modificados para servidores
privados aceitam). Para a lista de jogadores, o chat e a mochila, inclua os core scripts em
`content\scripts\cores` (o script de entrada carrega `rbxasset://scripts/cores/StarterScript.lua`).

## Segurança

* Qualquer página pode abrir um link `robloxserver-player:`, por isso o launcher pergunta uma vez por site
  (`TrustedSite` em `%LocalAppData%\RobloxServer\Settings.ini`) antes de baixar e rodar qualquer coisa.
* Todas as requisições vão só para o site do link, sem seguir redirecionamentos para outros endereços.
* Os pacotes são conferidos por SHA-256, e entradas do zip que sairiam da pasta do cliente são recusadas.
* O ticket do botão Play vale uma vez só e expira em poucos minutos; o ticket do jogo é outro, validado pelo servidor.

## Configurações

`%LocalAppData%\RobloxServer\Settings.ini`:

```ini
HostPort=53640        ; porta UDP do servidor quando você hospeda
HostAddress=          ; endereço que os jogadores usam (vazio: o site decide)
TrustedSite=http://robloxserver.lan/
```

Desinstalar: `RobloxPlayerLauncher.exe --uninstall`. O log fica em `%LocalAppData%\RobloxServer\Logs\launcher.log`.

## Compilando

Abra `RobloxServerLauncher.sln` no Visual Studio 2015 ou mais novo (.NET Framework 4.6), ou use o Mono:

```
xbuild /p:Configuration=Release RobloxServerLauncher.sln
```

O GitHub Actions compila a cada push e publica o `RobloxPlayerLauncher.exe` em *Artifacts*. Copie-o para
`App_Data/Launcher/` no site para que o botão **Download ROBLOX** e a atualização automática usem essa versão.

## Legal

ROBLOX e os clientes ROBLOX foram feitos pela ROBLOX Corporation. Este projeto não é afiliado, patrocinado ou
endossado pela ROBLOX Corporation.
