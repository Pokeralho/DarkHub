# DarkHub

O DarkHub é um aplicativo WPF poderoso e versátil projetado para maximizar o desempenho do seu PC e simplificar tarefas diárias. Combinando ferramentas de otimização,
automação e manipulação de mídia, o DarkHub oferece uma solução completa para usuários e desenvolvedores.

## Funcionalidades
- **Otimizador de PC**: Melhore o desempenho do sistema com limpeza de arquivos desnecessários, desativação de processos pesados e ativação do Windows para eficiência máxima.
- **Auto Clicker**: Automatize cliques repetitivos com intervalos configuráveis, ideal para jogos ou tarefas rotineiras.
- **Conversor de Arquivos**: Converta facilmente arquivos de texto, imagem, áudio e vídeo em formatos populares.
- **Editor de Metadados**: Organize suas coleções de mídia editando títulos, artistas, álbuns e mais diretamente nos arquivos.
- **Extrator de Texto de Imagens**: Utilize OCR para extrair texto de imagens, perfeito para digitalizar documentos ou capturas de tela.
- **Downloader de Vídeos do YouTube**: Baixe vídeos ou playlists do YouTube em formato MP4 com suporte a H.264 e AAC.
- **Transcrição de vídeos do YouTube**: Extraia e copie transcrições de vídeos do YouTube.
- **Editor de Texto com Interpretador Python**: Escreva, edite e execute scripts Python em tempo real, com suporte a depuração integrado.
- **Monitor de Recursos**: Informações e métricas em tempo real sobre hardware e software. Além de um Benchmark para avaliação de desempenho.
- **Gestor de palavras-passe**: Este gerenciador de senhas fornece uma forma segura e fácil de utilizar para armazenar, gerir e recuperar palavras-passe.
- **Segurança Avançada**: Ajuda o utilizador a identificar aplicações e websites maliciosos.

## Pré-requisitos
- Windows 10 ou superior.
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework) ou superior.

## Pré-requisitos para o uso do youtube downloader com o source code `Arquvios já inclusos na release`(coloque ambos na pasta `assets`):
- [ffmpeg.exe](https://www.gyan.dev/ffmpeg/builds/#release-builds)
- [yt-dlp.exe](https://github.com/yt-dlp/yt-dlp/releases/)

## Restaurar arquivos locais
Arquivos grandes, binários de terceiros e segredos continuam fora do Git. Para reconstruir o ambiente local depois de clonar:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Restore-LocalAssets.ps1
```

O script baixa `yt-dlp.exe` e tenta restaurar `ffmpeg.exe`. Para `CPU-Z.exe`, `GPU-Z.exe`, `HWiNFO64.exe`, `DDU.exe` e `assets\settings`, use uma pasta de backup ou uma release extraída:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Restore-LocalAssets.ps1 -ThirdPartyToolsDirectory "C:\caminho\para\backup"
```

Certificados, senhas e `.pfx` não devem ser versionados nem copiados para `assets`.

## Build e assinatura
Para gerar uma release local sem assinatura:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-SignedRelease.ps1 -SkipSigning
```

Para assinar o `DarkHub.exe`, instale o Windows SDK para ter `signtool.exe` e informe um certificado seu por variável de ambiente:

```powershell
$env:DARKHUB_SIGN_CERT_PATH = "C:\certs\DarkHub-release.pfx"
$env:DARKHUB_SIGN_CERT_PASSWORD = "senha-do-certificado"
powershell -ExecutionPolicy Bypass -File .\scripts\Build-SignedRelease.ps1
```

Também é possível usar um certificado instalado no repositório do Windows:

```powershell
$env:DARKHUB_SIGN_CERT_THUMBPRINT = "THUMBPRINT_DO_CERTIFICADO"
powershell -ExecutionPolicy Bypass -File .\scripts\Build-SignedRelease.ps1
```

## Contribuição
Contribuições são bem-vindas! Siga estes passos:
1. Faça um fork do repositório.
2. Crie uma branch para sua feature (`git checkout -b feature/nova-funcionalidade`).
3. Faça commit das suas mudanças (`git commit -m "Adiciona nova funcionalidade"`).
4. Envie para o repositório remoto (`git push origin feature/nova-funcionalidade`).
5. Abra um Pull Request.

## Licença
Este projeto está licenciado sob a [MIT License](LICENSE).


## Contato
- Autor: Pokeralho
- Email: luisfernandobtu80@gmail.com
- GitHub: [Pokeralho](https://github.com/Pokeralho)


## Créditos
- Agradecemos:
- [SpaceSniffer](https://github.com/redtrillix/SpaceSniffer) Repositório do executável usado como parte dos recursos de otimização do DarkHub.
- [CPU-Z](https://www.cpuid.com/softwares/cpu-z.html) Ferramenta para monitorar e fornecer informações detalhadas sobre a CPU e outros componentes do sistema.
- [GPU-Z](https://www.techpowerup.com/gpuz/) Utility para obter informações detalhadas sobre placas de vídeo.
- [HWiNFO](https://www.hwinfo.com/) Software avançado de monitoramento e diagnóstico de hardware.
- [DDU (Display Driver Uninstaller)](https://www.wagnardsoft.com/display-driver-uninstaller-ddu-) Ferramenta para remoção completa de drivers de vídeo.
