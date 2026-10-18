# SplitscreenFix para Super Mario Eclipse

**Windows x64 | Launcher cooperativo local | Projeto da comunidade**

O SplitscreenFix inicia o **Super Mario Eclipse v1.1.0** em duas instâncias locais (Mario como host e Luigi como client), organiza as janelas e permite trocar os controles durante a partida.

> **Importante:** Este é um projeto da comunidade, não uma versão oficial da Nintendo. Você precisa fornecer sua própria **ISO modificada do Super Mario Eclipse v1.1.0**. A ISO original do Super Mario Sunshine não é compatível. A ISO e os saves pessoais não estão incluídos.

## Instalação

1. Baixe `SplitscreenFix-Windows-x64.zip` na seção **Releases**.
2. Extraia **todo o ZIP** para uma pasta, por exemplo `Documentos\SplitscreenFix`. Não execute os arquivos de dentro do ZIP.
3. Dê dois cliques em **`Setup-Install.cmd`**. Ele chama o instalador, verifica o pacote e procura a ISO. Se não encontrar, selecione-a manualmente.
4. Abra o atalho **SplitscreenFix - Mario e Luigi** ou, após instalar, execute `Eclipse-2-Jogadores-Rebind.exe` na pasta extraída.
5. Conecte os controles, clique em **Atualizar** e atribua um controle a Mario e Luigi.

A versão já compilada não deve exigir administrador, MSYS2 ou compilador.

## Como jogar

- **Iniciar 1P - SOLO** abre apenas uma instância. Outra opção são os atalhos **Jogador 1 solo / Jogador 2 solo**, quando presentes na versão.
- **Iniciar 1P + 2P - COOP** abre as duas instâncias. Entre em uma fase com Mario antes de Luigi conectar; o launcher aguarda o host ficar pronto.
- Para trocar controles sem fechar o jogo, clique em **Atualizar**, selecione os controles e clique em **Aplicar controles**.
- **(Sem gamepad)** deixa o jogador sem controle físico atribuído. Teclado/mouse funcionam **na janela que está em foco**.
- **Atualizar layout** organiza as janelas; **Alternar borda** alterna as molduras.
- Para divisão superior/inferior sem deformar os personagens, use **Em cima / embaixo**, **Sem borda** e deixe **Esticar a imagem** desativado.
- O **LOG** (ou a linha de status em versões anteriores) mostra o diagnóstico dos dispositivos e da inicialização.

> As funções SOLO/COOP, LOG e Sem gamepad dependem da versão do launcher empacotada. Teste-as nas duas janelas antes de distribuir.

## Problemas com controles

- **Emparelhado** no Bluetooth do Windows não significa que o controle tenha **entrada de jogo** no SDL. Reconecte em modo gamepad e atualize a lista.
- Dois controles com o **mesmo VID:PID** não podem ser separados apenas pelo filtro VID/PID atual.
- Se o mesmo controle atuar nas duas janelas, atribua dispositivos diferentes ou escolha **Sem gamepad** em uma delas.
- Teclado/mouse respondem somente na janela em foco.
- Se Luigi não conectar, confira o `Client.stderr.log` mais recente. A porta multiplayer local padrão é **24002**; as duas instâncias devem usar a mesma porta.

## Arquivos e dados

- `Setup-Install.cmd`: entrada de instalação com nome compreensível em PT-BR e inglês, que chama os scripts originais.
- `Instalar.cmd`, `Instalar.ps1`: scripts originais mantidos por compatibilidade.
- `Eclipse-2-Jogadores-Rebind.exe`: nome do launcher esperado pelo instalador existente.
- `Runtime\sms-rebind.exe`: executável modificado do PC Port para Windows x64.
- `Source\`: fontes modificados e patches, quando presentes.
- `Assets\`, `Licenses\`: imagens e avisos de licença.
- `README-PT-BR.md` / `README-EN.md`: instruções nos dois idiomas.

Os caminhos locais ficam em `SplitscreenFix.config.json`; **não compartilhe esse arquivo**. Saves e logs ficam fora da pasta do programa. O instalador foi projetado para preservar saves existentes, inclusive ao migrar dados antigos de `%TEMP%\SMS-Eclipse-Multiplayer-Test`.

## Atualização

Extraia a nova versão em **outra pasta** e execute `Setup-Install.cmd` novamente. Mantenha a ISO e os saves pessoais fora do ZIP compartilhado.

## Direitos e compatibilidade

O SplitscreenFix é um projeto independente da comunidade. A Nintendo e os respectivos colaboradores do jogo/port mantêm seus direitos. Redistribua o executável modificado, bibliotecas, imagens e códigos apenas quando as licenças ou titulares permitirem. O repositório **não** inclui nenhuma ISO. Confira compatibilidade e licenciamento antes de cada lançamento público.
