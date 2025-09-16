# 🧰 CleanArch CLI

Uma **ferramenta de linha de comando (CLI)** para gerar projetos baseados em **Clean Architecture** com .NET. Ela cria automaticamente a solução, os projetos, testes, referências e até habilita o **Swagger** quando configurado como WebAPI.

-----

## 📥 Instalação

### 1\. Clonar o repositório

```bash
git clone https://github.com/seu-usuario/CleanArch.Cli.git
cd CleanArch.Cli/CleanArch.Cli
```

### 2\. Gerar o pacote `.nupkg`

```bash
dotnet pack -c Release -o ./nupkg
```

Isso vai gerar um pacote no diretório `nupkg`, algo como:

```
nupkg/CleanArch.Cli.1.0.0.nupkg
```

### 3\. Instalar a ferramenta

#### 🔹 Instalação local (apenas no projeto atual)

```bash
dotnet tool install --add-source ./nupkg cleanarch --version 1.0.0
```

#### 🔹 Instalação global (disponível em qualquer lugar do sistema)

```bash
dotnet tool install --global --add-source ./nupkg cleanarch --version 1.0.0
```

Se já tiver instalado e quiser atualizar:

```bash
dotnet tool update --global --add-source ./nupkg cleanarch
```

-----

## 🏗️ Criando um novo projeto

Após instalar a CLI, basta rodar:

```bash
cleanarch create MinhaApp --presentation webapi --with-tests
```

-----

## ⚙️ Opções disponíveis

  * `--presentation webapi`: Cria o projeto de apresentação como WebAPI (com Swagger já configurado).
  * `--presentation console`: Cria o projeto de apresentação como Console App.
  * `--presentation none`: Não cria projeto de apresentação.
  * `--with-tests`: Gera também projetos de testes (xUnit).

-----

## 📂 Estrutura de pastas gerada

Exemplo com `--presentation webapi --with-tests`:

```
MinhaApp/
├── MinhaApp.sln
│
├── src/
│   ├── Domain/              # Entidades e regras de negócio
│   ├── Application/         # Casos de uso e lógica de aplicação
│   ├── Infrastructure/      # Persistência, serviços externos, etc.
│   └── WebUI/               # API Web (com Swagger habilitado)
│
└── tests/
    ├── Domain.UnitTests/
    ├── Application.UnitTests/
    └── Application.IntegrationTests/
```

-----

## ▶️ Executando a aplicação

Entre no diretório do projeto gerado e rode:

```bash
cd MinhaApp/src/WebUI
dotnet run
```

Se for WebAPI, a aplicação iniciará em `https://localhost:5001` (ou porta configurada).
O Swagger UI estará disponível em:

👉 `https://localhost:5001/swagger`

-----

## 📜 Licença

Este projeto está sob a licença MIT. Sinta-se livre para usar, modificar e contribuir.

-----

⚡ Agora você tem uma CLI pronta para acelerar a criação de projetos Clean Architecture em .NET\!

