# HeroVerse

Aplicação Blazor WebAssembly para pesquisar personagens da SuperHeroAPI e consultar suas biografias, poderes, aparência e conexões.

## Recursos

- Busca de personagens por nome.
- Resultados em tabela com ficha de detalhes expansível.
- Exibição de biografia, aparência, habilidades e conexões.
- Integração com a SuperHeroAPI pelo backend, mantendo o token fora do navegador.
- Proxy local para imagens de personagens, com avatar alternativo quando uma imagem não está disponível.

## Arquitetura

| Projeto | Responsabilidade |
| --- | --- |
| `BlazorSuperHeroApi` | Interface Blazor WebAssembly |
| `BlazorSuperHeroApi.Application` | Contratos e modelos da aplicação |
| `BlazorSuperHeroApi.Infrastructure` | Integrações HTTP com serviços externos |
| `BlazorSuperHeroApi.Server` | API ASP.NET Core e hospedagem da aplicação |

## Requisitos

- .NET 10 SDK
- Token ativo da SuperHeroAPI

## Executar localmente

Configure o token usando User Secrets:

```powershell
dotnet user-secrets set "SuperHeroApi:AccessToken" "<seu-token>" --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
```

Inicie o servidor:

```powershell
dotnet run --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
```

Abra `https://localhost:7183`.

## Endpoints locais

| Método | Endpoint | Descrição |
| --- | --- | --- |
| `GET` | `/api/heroes/search?name=batman` | Pesquisa personagens pelo nome |
| `GET` | `/api/heroes/image?url={url-codificada}` | Busca uma imagem de retrato permitida pelo servidor |

O servidor consulta a SuperHeroAPI no formato `https://superheroapi.com/api/{token}/search/{nome}`.

## Configuração em produção

Defina `SuperHeroApi__AccessToken` como variável de ambiente no servidor. Não coloque o token em arquivos públicos, no projeto WebAssembly ou no repositório.

O carregamento das imagens depende de o servidor conseguir acessar `www.superherodb.com`. Redes institucionais ou restrições do provedor podem bloquear esse acesso; nesses casos, a aplicação exibe o avatar alternativo.
