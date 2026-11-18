# HeroVerse

Aplicação Blazor WebAssembly para pesquisar personagens da SuperHeroAPI e consultar biografia, aparência, habilidades e conexões.

## Recursos

- Busca de personagens por nome.
- Resultados em tabela com fichas expansíveis.
- Integração da SuperHeroAPI pelo backend.
- Resolução de imagens por ID usando o catálogo Akabab e o CDN jsDelivr.
- Avatar com a inicial do personagem quando não há uma imagem disponível.

## Arquitetura

| Projeto | Responsabilidade |
| --- | --- |
| `BlazorSuperHeroApi` | Interface Blazor WebAssembly |
| `BlazorSuperHeroApi.Application` | Contratos e modelos da aplicação |
| `BlazorSuperHeroApi.Infrastructure` | Clientes HTTP e integrações externas |
| `BlazorSuperHeroApi.Server` | API ASP.NET Core e hospedagem da aplicação |

## Requisitos

- .NET 10 SDK
- Token ativo da SuperHeroAPI
- Acesso de rede do servidor a `superheroapi.com` e `cdn.jsdelivr.net`

## Executar localmente

Configure o token com User Secrets:

```powershell
dotnet user-secrets set "SuperHeroApi:AccessToken" "<seu-token>" --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
```

Inicie o servidor:

```powershell
dotnet run --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
```

Abra `https://localhost:7183`.

## API de dados: SuperHeroAPI

O servidor consulta a SuperHeroAPI usando o formato:

```text
https://superheroapi.com/api/{token}/search/{nome}
```

Por exemplo, uma pesquisa por Batman usa `search/Batman`. O serviço codifica o nome para URL antes de enviar a requisição. A resposta contém um indicador de sucesso e uma lista `results` com os personagens encontrados. Cada personagem inclui um `id`, usado também para resolver a imagem.

O token é adicionado pelo backend ao criar o cliente HTTP. Ele não é enviado pelo navegador nem incluído na URL da aplicação local.

### Endpoint local de busca

```http
GET /api/heroes/search?name=Batman
```

Retorna os personagens encontrados com seus dados de biografia, poderes, aparência, trabalho e conexões. Uma busca válida sem resultados retorna uma lista vazia.

## Catálogo de imagens e CDN

As imagens são obtidas do projeto [akabab/superhero-api](https://github.com/akabab/superhero-api), publicado no CDN [jsDelivr](https://www.jsdelivr.com/). O servidor usa a versão `0.3.0` do catálogo para manter os caminhos previsíveis.

### Localizar imagem pelo ID

Para o Batman, cujo ID é `70`, o servidor consulta:

```text
https://cdn.jsdelivr.net/gh/akabab/superhero-api@0.3.0/api/id/70.json
```

Esse JSON contém os dados do personagem e diferentes tamanhos de imagem:

```json
{
  "id": 70,
  "name": "Batman",
  "images": {
    "xs": "https://cdn.jsdelivr.net/gh/akabab/superhero-api@0.3.0/api/images/xs/70-batman.jpg",
    "sm": "https://cdn.jsdelivr.net/gh/akabab/superhero-api@0.3.0/api/images/sm/70-batman.jpg",
    "md": "https://cdn.jsdelivr.net/gh/akabab/superhero-api@0.3.0/api/images/md/70-batman.jpg",
    "lg": "https://cdn.jsdelivr.net/gh/akabab/superhero-api@0.3.0/api/images/lg/70-batman.jpg"
  }
}
```

O HeroVerse usa o tamanho `md`. O backend valida que a URL esteja no host e no caminho esperados e mantém a URL em cache em memória por sete dias. Respostas sem imagem não são armazenadas em cache.

### Fluxo de carregamento

1. A interface usa o ID do personagem para solicitar `GET /api/heroes/{id}/image` ao servidor local.
2. O servidor consulta ou recupera do cache a URL `images.md` do catálogo.
3. O endpoint responde com um redirecionamento HTTP `302` para a imagem no jsDelivr.
4. O navegador baixa a imagem diretamente do CDN. Os arquivos não são copiados para o projeto nem retransmitidos pelo servidor.
5. Se o catálogo não tiver imagem ou o carregamento falhar, a interface mostra a inicial do personagem.

O CDN jsDelivr permite uso cross-origin das imagens, evitando o bloqueio CORP observado com imagens hospedadas em `superherodb.com`.

### Endpoint local de imagem

```http
GET /api/heroes/70/image
```

Respostas esperadas:

| Status | Significado |
| --- | --- |
| `302 Found` | Redireciona para a imagem no CDN |
| `404 Not Found` | ID inválido ou sem imagem no catálogo |
| `502 Bad Gateway` | Catálogo indisponível ou resposta inválida |

## Endpoints locais

| Método | Endpoint | Descrição |
| --- | --- | --- |
| `GET` | `/api/heroes/search?name={nome}` | Pesquisa personagens na SuperHeroAPI |
| `GET` | `/api/heroes/{id}/image` | Redireciona para a imagem do catálogo Akabab no jsDelivr |

## Configuração em produção

Defina `SuperHeroApi__AccessToken` como variável de ambiente do servidor. Não coloque o token em arquivos públicos, no projeto WebAssembly ou no repositório. Se o token já foi exposto, revogue-o e configure um novo.

O repositório Akabab declara licença MIT para o projeto. As imagens podem ter direitos próprios dos respectivos titulares; verifique os termos aplicáveis e obtenha as permissões necessárias antes de publicar ou usar comercialmente.
