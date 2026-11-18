# HeroVerse

Aplicação Blazor WebAssembly com uma API ASP.NET Core responsável por consultar a SuperHeroAPI. O navegador acessa somente o backend local; o token da SuperHeroAPI permanece na configuração do servidor.

## Configuração local

Revogue o token que foi compartilhado na conversa e gere um novo. Configure-o com User Secrets no projeto servidor:

```powershell
dotnet user-secrets set "SuperHeroApi:AccessToken" "<NOVO_TOKEN>" --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
dotnet run --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
```

Abra `https://localhost:7183`. O endpoint `GET /api/heroes/search?name=batman` pesquisa personagens e retorna seus dados de biografia, aparência, poderes, trabalho, conexões e imagem.
O servidor consulta a SuperHeroAPI no formato `https://superheroapi.com/api/{token}/search/{nome}`.

Se a busca retornar que a SuperHeroAPI recusou o token, confirme que o token ativo está salvo nos User Secrets do projeto `BlazorSuperHeroApi.Server`. A credencial não deve ser adicionada ao projeto WebAssembly.

Em ambientes publicados, configure `SuperHeroApi__AccessToken` como variável de ambiente do servidor. Não adicione tokens ao projeto WebAssembly, ao repositório ou a arquivos servidos ao navegador.

## Projetos

- `BlazorSuperHeroApi.Application`: contratos de busca e modelos de domínio.
- `BlazorSuperHeroApi.Infrastructure`: integração HTTP com a SuperHeroAPI e registro de dependências.
- `BlazorSuperHeroApi.Server`: endpoint backend, validação de configuração e hospedagem da aplicação WebAssembly.
- `BlazorSuperHeroApi`: interface Blazor WebAssembly.
