# HeroVerse

HeroVerse é uma aplicação ASP.NET Core + Blazor WebAssembly para pesquisar personagens e consultar dados detalhados fornecidos pela SuperHeroAPI. A interface é executada no navegador, enquanto o servidor é responsável por proteger credenciais e integrar fontes externas.

## Visão geral

O projeto apresenta:

- Busca de personagens por nome.
- Ficha com biografia, aparência, poderes, trabalho e conexões.
- Catálogo de imagens externo servido por uma rota intermediária do backend.
- Proteção do token da SuperHeroAPI, que nunca é distribuído ao WebAssembly.
- Validação das URLs recebidas de fontes externas antes do redirecionamento.
- Cache em memória para consultas de imagens.
- Tratamento de erros HTTP e respostas vazias ou não encontradas.

## Arquitetura

```text
Navegador / Blazor WebAssembly
            |
            | HTTP HTTPS para a aplicação hospedada
            v
ASP.NET Core Server
  |                         |
  | ISuperHeroService       | IHeroImageCatalog
  v                         v
SuperHeroAPI             Akabab no GitHub
                             |
                             | jsDelivr CDN
                             v
                        Imagem final
```

| Projeto | Responsabilidade |
| --- | --- |
| `BlazorSuperHeroApi` | Aplicação Blazor WebAssembly: interface, estado da tela e chamadas à API local. |
| `BlazorSuperHeroApi.Application` | Contratos de aplicação e modelos compartilhados pelos resultados. |
| `BlazorSuperHeroApi.Infrastructure` | Implementações HTTP, configuração dos `HttpClient`s e cache da aplicação. |
| `BlazorSuperHeroApi.Server` | Hospedagem da aplicação, endpoints mínimos HTTP e arquivos estáticos. |

## Fluxo de uma busca

Quando o usuário informa um nome e envia o formulário:

```text
1. O Blazor executa uma chamada HTTP local.
2. O servidor recebe GET /api/heroes/search?name=<nome>.
3. O servidor chama a SuperHeroAPI.
4. A SuperHeroAPI devolve os personagens encontrados.
5. O servidor transforma ou valida a resposta.
6. O JSON é devolvido ao navegador.
7. O Blazor atualiza os resultados e permite selecionar uma ficha.
```

### 1. Requisição do navegador

O componente principal faz uma chamada relativa à origem hospedada:

```http
GET /api/heroes/search?name=batman
```

O nome é codificado com `Uri.EscapeDataString` antes de entrar na URL. Como o `HttpClient` do WebAssembly usa o endereço base da própria aplicação, não é necessário configurar uma URL absoluta separada para o backend.

### 2. Endpoint da API local

O endpoint `GET /api/heroes/search` valida o parâmetro `name`. Uma busca vazia gera `400 Bad Request` no formato Problem Details.

O endpoint recebe a abstração `ISuperHeroService` por injeção de dependência e delega a consulta. Um erro de disponibilidade da SuperHeroAPI, JSON inválido ou timeout é convertido em `502 Bad Gateway`. Um token recusado pela fonte externa gera uma resposta específica indicando que o token configurado não foi aceito.

### 3. Requisição ao serviço externo

No servidor, o serviço cria o endereço completo da seguinte forma:

```text
https://superheroapi.com/api/{token}/search/{nome}
```

A `BaseAddress` configurada em `AddHttpClient<ISuperHeroService, SuperHeroService>` é:

```text
https://superheroapi.com/api/{token}/
```

Depois, o método chama o caminho relativo `search/{nome}`. Esse desenho mantém o token fora do projeto WebAssembly e fora do navegador.

### 4. Resposta recebida

A resposta HTTP é deserializada como `SearchResponse`. Quando `response` é `success`, a lista de resultados é devolvida ao Blazor. Se a API informar que não encontrou um personagem, o servidor devolve uma lista vazia. Outros erros da fonte são classificados como problemas de configuração, acesso ou disponibilidade.

As imagens originais fornecidas pela SuperHeroAPI não são usadas diretamente pela interface atual. A aplicação utiliza uma rota própria para procurar uma imagem no catálogo Akabab.

## Fluxo completo de carregamento de uma imagem

Cada personagem possui um `id`. A interface não monta diretamente uma URL externa no navegador. Em vez disso, gera uma URL da própria aplicação:

```text
/api/heroes/{id}/image
```

Exemplo:

```http
GET /api/heroes/70/image
```

### Por que existe uma rota intermediária

A rota `/api/heroes/{id}/image` oferece uma camada de controle entre o navegador e o catálogo externo:

- O frontend não precisa conhecer a estrutura interna do repositório Akabab.
- A URL recebida da fonte externa pode ser validada antes do uso.
- O servidor pode responder `404` quando o personagem não existe no catálogo.
- Apenas `HTTPS` é permitido.
- Somente o host `cdn.jsdelivr.net` na porta `443` é aceito.
- O caminho precisa corresponder ao padrão `/gh/akabab/superhero-api@0.3.0/api/images/md/{id}-{slug}.jpg`.
- Fragmentos, queries e informações de usuário na URL são rejeitados.
- URLs imprevistas não são repassadas ao navegador.
- Há um ponto único para aplicar timeout, tratamento de erro e cache.

### Cache das imagens

Existem duas formas de cache no fluxo:

| Nível | Onde ocorre | Duração | Responsabilidade |
| --- | --- | --- | --- |
| Metadados da imagem | `IMemoryCache` no servidor | 7 dias | Guarda o endereço encontrado no JSON `id/{id}.json`. |
| Resposta da rota local | Cabeçalho HTTP no navegador/proxy | 1 dia | `Cache-Control: public, max-age=86400`. |

O cache em memória é por processo. Reiniciar o servidor limpa esse cache. Não há cache persistente em Redis ou banco de dados.

Os dados do CDN propriamente ditos permanecem no CDN; o servidor não baixa e não hospeda o arquivo `.jpg`. Ele apenas resolve o endereço da imagem e emite o redirecionamento HTTP.

### Comportamento de erro das imagens

| Situação | Resposta |
| --- | --- |
| ID inválido ou catálogo sem personagem | `404 Not Found` |
| Timeout do catálogo | `502 Bad Gateway` |
| Erro HTTP ou JSON inválido | `502 Bad Gateway` |
| URL externa fora do padrão permitido | `502 Bad Gateway` |
| Falha no navegador ao carregar o retorno redirecionado | Interface mostra o avatar com a inicial do personagem |

## Endpoints HTTP

### Buscar personagens

```http
GET /api/heroes/search?name=batman
Accept: application/json
```

Resposta esperada em caso de sucesso:

```json
[
  {
    "id": "70",
    "name": "Batman",
    "powerstats": {
      "intelligence": "100",
      "strength": "26",
      "speed": "27",
      "durability": "50",
      "power": "47",
      "combat": "100"
    },
    "biography": {
      "full-name": "Bruce Wayne",
      "publisher": "DC Comics",
      "alignment": "good"
    }
  }
]
```

O modelo completo também pode conter aparência, trabalho, conexões e imagem, conforme os campos enviados pela fonte.

Respostas de erro usam os formatos Problem Details do ASP.NET Core:

- `400` — parâmetro `name` ausente ou vazio.
- `502` — falha ao consultar a SuperHeroAPI.
- `502` — token recusado pela fonte externa.

### Consultar e redirecionar para uma imagem

```http
GET /api/heroes/{id}/image
```

Respostas possíveis:

- `302 Found` com o cabeçalho `Location` apontando para o jsDelivr.
- `404 Not Found` quando o ID não corresponde a uma imagem conhecida.
- `502 Bad Gateway` quando o catálogo falha ou retorna um endereço inválido.

Exemplo:

```powershell
Invoke-WebResponse -Uri "https://localhost:7183/api/heroes/70/image" -MaximumRedirection 0
```

Em clientes que seguem redirecionamentos automaticamente, a resposta final será a imagem hospedada pelo CDN.

## Requisitos

- .NET 10 SDK
- Um token válido da SuperHeroAPI
- Conexão HTTPS com `superheroapi.com` e `cdn.jsdelivr.net`
- Certificado de desenvolvimento HTTPS local configurado

## Configuração local

### 1. Restaurar dependências

Na raiz do repositório:

```powershell
dotnet restore
```

### 2. Configurar o token da SuperHeroAPI

Configure o token com User Secrets no projeto do servidor:

```powershell
dotnet user-secrets set "SuperHeroApi:AccessToken" "<seu-token>" --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
```

O projeto valida na inicialização:

- `SuperHeroApi:BaseUrl` como URL absoluta HTTPS.
- `SuperHeroApi:AccessToken` como um valor não vazio.

A configuração pública da URL está em `appsettings.json`:

```json
{
  "SuperHeroApi": {
    "BaseUrl": "https://superheroapi.com/api"
  }
}
```

Não adicione o token ao `appsettings.json`, ao código-fonte, ao WebAssembly ou a arquivos versionados.

### 3. Executar a aplicação

```powershell
dotnet watch --project .\BlazorSuperHeroApi.Server\BlazorSuperHeroApi.Server.csproj
```

Abra:

- HTTPS: `https://localhost:7183`
- HTTP: `http://localhost:5149`

O redirecionamento HTTPS encaminha a versão HTTP para o endereço seguro.

### 4. Testar a rota de imagem

O arquivo [HeroImage.http](./BlazorSuperHeroApi.Server/HeroImage.http) contém uma requisição pronta para o personagem de ID `70`:

```http
GET https://localhost:7183/api/heroes/70/image
```

## Configuração em produção

Defina o token no ambiente do servidor:

```text
SuperHeroApi__AccessToken=<seu-token>
```

Em provedores de nuvem, use a variável em serviços de segredo gerenciado, variáveis de ambiente protegidas ou parâmetros seguros da plataforma.

Para a URL base da API, use uma variável no mesmo formato hierárquico:

```text
SuperHeroApi__BaseUrl=https://superheroapi.com/api
```

Os dois `HttpClient`s possuem limites próprios de timeout:

| Serviço | Timeout |
| --- | --- |
| SuperHeroAPI | 20 segundos |
| Catálogo Akabab no jsDelivr | 10 segundos |

O servidor deve permitir HTTPS de saída para `superheroapi.com` e `cdn.jsdelivr.net`. Se o ambiente usa proxy corporativo, firewall ou inspeção TLS, essas rotas precisam ser liberadas.

## Considerações sobre provedores externos

- A disponibilidade e o formato dos dados dependem da SuperHeroAPI.
- O catálogo Akabab é uma fonte pública mantida externamente.
- O conteúdo de imagem é distribuído pelo jsDelivr.
- Antes de publicar comercialmente ou em larga escala, revise os termos de uso, licenças e políticas das fontes externas.
- Os dados podem mudar sem aviso. Mudanças de schema podem exigir atualização dos modelos JSON em `HeroProfile.cs`.

## Segurança

- O token da SuperHeroAPI é configurado no servidor e não deve aparecer no bundle WebAssembly.
- A URL da SuperHeroAPI precisa usar HTTPS.
- As URLs vindas do catálogo de imagens são permitidas somente no host e no padrão de caminho fixos.
- O endereço da imagem não aceita fragmentos, query strings ou informações de usuário.
- A validação é feita antes de emitir `302 Redirect`.
- O acesso externo não é feito diretamente pelo componente Blazor para o catálogo Akabab.
