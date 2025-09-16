using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            ShowHelp();
            return 0;
        }

        var cmd = args[0].ToLowerInvariant();
        if (cmd == "create")
        {
            return HandleCreate(args.Skip(1).ToArray());
        }

        WriteError("Comando desconhecido. Use: cleanarch create <Name> [--presentation webapi|console|none] [--with-tests]");
        return 1;
    }

    static int HandleCreate(string[] args)
    {
        if (args.Length == 0)
        {
            WriteError("Nome do projeto é obrigatório.");
            return 1;
        }

        var name = args[0];
        var presentation = GetOptionValue(args, "--presentation", "-p") ?? "webapi";
        var withTests = args.Contains("--with-tests");

        WriteInfo($"Gerando solução '{name}' (presentation={presentation}, withTests={withTests})");

        try
        {
            CreateSolution(name);
            CreateProjects(name, presentation, withTests);
            SetupReferences(name, presentation, withTests);
            AddPlaceholders(name);

            if (presentation == "webapi")
                EnableSwagger(name);

            WriteSuccess("Geração concluída com sucesso. Boa codagem!");
            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"Erro: {ex.Message}");
            return 1;
        }
    }

    #region ---------- MÉTODOS PRINCIPAIS ----------

    static void CreateSolution(string name)
    {
        Run("dotnet", $"new sln -n {name}");
        Directory.CreateDirectory("src");
        Directory.CreateDirectory("tests");
    }

    static void CreateProjects(string name, string presentation, bool withTests)
    {
        Run("dotnet", $"new classlib -n {name}.Domain -o src/Domain");
        Run("dotnet", $"new classlib -n {name}.Application -o src/Application");
        Run("dotnet", $"new classlib -n {name}.Infrastructure -o src/Infrastructure");

        if (presentation == "webapi")
            Run("dotnet", $"new webapi -n {name}.WebUI -o src/WebUI");
        else if (presentation == "console")
            Run("dotnet", $"new console -n {name}.WebUI -o src/WebUI");
        else
            WriteWarning("Pulando criação do projeto WebUI.");

        if (withTests)
        {
            Run("dotnet", $"new xunit -n {name}.Domain.UnitTests -o tests/Domain.UnitTests");
            Run("dotnet", $"new xunit -n {name}.Application.UnitTests -o tests/Application.UnitTests");
            Run("dotnet", $"new xunit -n {name}.Application.IntegrationTests -o tests/Application.IntegrationTests");
        }
    }

    static void SetupReferences(string name, string presentation, bool withTests)
    {
        Run("dotnet", $"sln {name}.sln add src/Domain/{name}.Domain.csproj");
        Run("dotnet", $"sln {name}.sln add src/Application/{name}.Application.csproj");
        Run("dotnet", $"sln {name}.sln add src/Infrastructure/{name}.Infrastructure.csproj");

        if (presentation != "none")
            Run("dotnet", $"sln {name}.sln add src/WebUI/{name}.WebUI.csproj");

        if (withTests)
        {
            Run("dotnet", $"sln {name}.sln add tests/Domain.UnitTests/{name}.Domain.UnitTests.csproj");
            Run("dotnet", $"sln {name}.sln add tests/Application.UnitTests/{name}.Application.UnitTests.csproj");
            Run("dotnet", $"sln {name}.sln add tests/Application.IntegrationTests/{name}.Application.IntegrationTests.csproj");
        }

        Run("dotnet", $"add src/Application/{name}.Application.csproj reference src/Domain/{name}.Domain.csproj");
        Run("dotnet", $"add src/Infrastructure/{name}.Infrastructure.csproj reference src/Domain/{name}.Domain.csproj");
        Run("dotnet", $"add src/Infrastructure/{name}.Infrastructure.csproj reference src/Application/{name}.Application.csproj");

        if (presentation != "none")
        {
            Run("dotnet", $"add src/WebUI/{name}.WebUI.csproj reference src/Application/{name}.Application.csproj");
            Run("dotnet", $"add src/WebUI/{name}.WebUI.csproj reference src/Infrastructure/{name}.Infrastructure.csproj");
        }

        if (withTests)
        {
            Run("dotnet", $"add tests/Domain.UnitTests/{name}.Domain.UnitTests.csproj reference src/Domain/{name}.Domain.csproj");
            Run("dotnet", $"add tests/Application.UnitTests/{name}.Application.UnitTests.csproj reference src/Application/{name}.Application.csproj");
            Run("dotnet", $"add tests/Application.IntegrationTests/{name}.Application.IntegrationTests.csproj reference src/Application/{name}.Application.csproj");
        }
    }

    static void AddPlaceholders(string name)
    {
        Directory.CreateDirectory($"src/Domain/Entities");
        File.WriteAllText($"src/Domain/Entities/PlaceholderEntity.cs",
            $"namespace {name}.Domain.Entities {{ public class PlaceholderEntity {{ public int Id {{ get; set; }} }} }}");

        Directory.CreateDirectory($"src/Application/UseCases");
        File.WriteAllText($"src/Application/UseCases/PlaceholderUseCase.cs",
            $"namespace {name}.Application.UseCases {{ public class PlaceholderUseCase {{ }} }}");
    }

    static void EnableSwagger(string name)
    {
        var programPath = $"src/WebUI/Program.cs";
        if (File.Exists(programPath))
        {
            var programCode = File.ReadAllText(programPath);

            if (!programCode.Contains("UseSwagger"))
            {
                programCode = programCode.Replace("var app = builder.Build();",
                @$"builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                var app = builder.Build();
                app.UseSwagger();
                app.UseSwaggerUI();
                ");
                File.WriteAllText(programPath, programCode);
                WriteInfo("Swagger habilitado no projeto WebAPI.");
            }
        }
    }

    #endregion

    #region ---------- HELPERS ----------
    static string GetOptionValue(string[] args, string longOpt, string shortOpt)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith(longOpt + "=", StringComparison.OrdinalIgnoreCase))
                return args[i].Split('=', 2)[1];
            if (args[i].Equals(longOpt, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
            if (args[i].Equals(shortOpt, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return null;
    }

    static void Run(string file, string args)
    {
        WriteInfo($"[exec] {file} {args}");
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi);
        p.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        p.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        if (p.ExitCode != 0)
            throw new Exception($"Comando falhou: {file} {args}");
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("   cleanarch create <Name> [--presentation webapi|console|none] [--with-tests]");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("   cleanarch create MinhaApp --presentation webapi --with-tests");
    }
    #endregion

    #region ---------- COLORS ----------
    static void WriteSuccess(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    static void WriteError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(msg);
        Console.ResetColor();
    }

    static void WriteWarning(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    static void WriteInfo(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
    #endregion
}
