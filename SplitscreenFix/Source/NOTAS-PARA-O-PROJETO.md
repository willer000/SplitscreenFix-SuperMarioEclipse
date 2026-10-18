# Alteração para rebinding ao vivo

Base: commit `acbac9ba19d55a7b37f3bce9dc0097f75530d4b9` do sms-pc-port com multiplayer e Eclipse no Windows x64. O patch `sms-live-controller-rebind.patch` altera apenas `platform/gx/src/gx_platform.cpp` e `platform/pad/pad.cpp`.

Cada processo recebe `SMS_PAD_BIND_PATH`, apontando para um arquivo ASCII que contém `VID:PID`, por exemplo `054c:09cc`. O GX relê o arquivo periodicamente, filtra eventos SDL do controle selecionado, limpa botões/eixos do anterior, ajusta rumble e grava `<arquivo>.ack` com o ID aplicado. O launcher troca o arquivo com substituição atômica e espera o ACK. O controle novo precisa aparecer como SDL GameController. O filtro é por VID/PID; IDs duplicados ainda não são diferenciados.

O pacote contém binários para teste e o código alterado. Para integrar no projeto, aplique o patch ao commit base e compile com Eclipse habilitado. O executável incluído foi compilado com `SMS_ECLIPSE=ON`, `SMS_ARCH=64` e `CMAKE_BUILD_TYPE=RelWithDebInfo` usando MinGW64. A ISO e os dados de jogo são lidos em tempo de execução do disco do usuário.

O executável de distribuição em `Runtime` teve apenas as seções de depuração removidas com `strip --strip-debug`. A versão completa com símbolos está em `C:\SplitscreenFix\Debug\sms-rebind-com-simbolos.exe` na máquina onde o pacote foi montado; ela não acompanha o ZIP para reduzir o tamanho da publicação.

Teste local: o processo confirmou `054c:09cc`, recebeu a troca para `054c:05c4` sem encerrar, iniciou o host na porta 24002, e os atalhos solo abriram com ACK. O pacote também iniciou fora da pasta do código fonte. Não houve verificação automática de botões físicos nem da sequência completa de conexão host/client nesta rodada.
