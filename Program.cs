using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;

class Program
{
    static void Main(string[] args)
    {

        bool executando = true;

        while (executando)
        {
            Console.Clear();
            Console.WriteLine("==== MENU PRINCIPAL ====");
            Console.WriteLine("1 - Cadastrar Usuário");
            Console.WriteLine("2 - Login");
            Console.WriteLine("3 - Sair");
            Console.Write("Escolha uma opção: ");

            string opcao = Console.ReadLine(); // Ler a opção uma vez

            switch (opcao)
            {
                case "1":
                    Cadastrar();
                    break;
                case "2":
                    Login();
                    break;
                case "3":
                    executando = false;
                    Console.WriteLine("Saindo...");
                    Console.WriteLine("Pressione Enter para continuar.");
                    Console.ReadLine();
                    break;
                default:
                    Console.WriteLine("Opção inválida. Pressione Enter para continuar.");
                    Console.ReadLine();
                    break;
            }
        }
    }

    static void Cadastrar()
    {
        Console.Clear();
        Console.WriteLine("==== CADASTRO DE USUÁRIO ====");

        Usuario u = new Usuario(); // Cria uma instância do usuário

        Console.Write("Nome: ");
        u.Nome = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(u.Nome))
        {
            Console.WriteLine("Nome não pode ser vazio.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }

        Console.Write("Email: ");
        u.Email = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(u.Email) || !IsValidEmail(u.Email)) // Adiciona validação de email
        {
            Console.WriteLine("Email inválido ou vazio.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }

        Console.Write("Data de nascimento (dd/mm/aaaa): ");
        DateTime dataNascimento;
        if (!DateTime.TryParseExact(Console.ReadLine(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dataNascimento))
        {
            Console.WriteLine("Formato de data inválido. Use dd/mm/aaaa.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }
        u.DataNascimento = dataNascimento;

        // --- Menu enumerado para Tipo de Usuário no Cadastro ---
        Console.WriteLine("\n==== SELECIONE O TIPO DE USUÁRIO ====");
        var tiposUsuario = Enum.GetValues(typeof(TipoUsuario));
        for (int i = 0; i < tiposUsuario.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {tiposUsuario.GetValue(i)}");
        }

        bool tipoValido = false;
        while (!tipoValido)
        {
            Console.Write($"Digite o NÚMERO do tipo de usuário (1 a {tiposUsuario.Length}): ");
            string tipoInput = Console.ReadLine();

            if (int.TryParse(tipoInput, out int indiceTipo) && indiceTipo > 0 && indiceTipo <= tiposUsuario.Length)
            {
                u.TipoDeUsuario = (TipoUsuario)tiposUsuario.GetValue(indiceTipo - 1);
                tipoValido = true;
            }
            else
            {
                Console.WriteLine("Seleção de tipo de usuário inválida. Por favor, digite o número correto.");
            }
        }

        // Lógica para o campo Departamento baseada no TipoDeUsuario
        if (u.TipoDeUsuario == TipoUsuario.COMUM)
        {
            // --- Lógica para selecionar Departamento de um menu ---
            Console.WriteLine("\n==== SELECIONE O DEPARTAMENTO ====");
            List<Departamento> departamentosDisponiveis = GetDepartamentosDisponiveis();

            if (departamentosDisponiveis.Count > 0)
            {
                Console.WriteLine("Departamentos disponíveis:");
                for (int i = 0; i < departamentosDisponiveis.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {departamentosDisponiveis[i].Nome}");
                }

                bool selecaoValida = false;
                while (!selecaoValida)
                {
                    Console.Write($"Digite o NÚMERO do departamento (1 a {departamentosDisponiveis.Count}): ");
                    string inputDepartamento = Console.ReadLine();

                    if (int.TryParse(inputDepartamento, out int indiceDepartamento) && indiceDepartamento > 0 && indiceDepartamento <= departamentosDisponiveis.Count)
                    {
                        // Atribui o NOME do departamento selecionado à propriedade Departamento do usuário
                        u.Departamento = departamentosDisponiveis[indiceDepartamento - 1].Nome;
                        selecaoValida = true;
                    }
                    else
                    {
                        Console.WriteLine("Seleção de departamento inválida. Por favor, digite o número correto.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Nenhum departamento disponível na base de dados para seleção.");
                Console.WriteLine("Não é possível cadastrar usuário COMUM sem departamentos disponíveis. Pressione Enter para continuar.");
                Console.ReadLine();
                return;
            }
        }
        else if (u.TipoDeUsuario == TipoUsuario.TECNICO || u.TipoDeUsuario == TipoUsuario.GERENTE)
        {
            u.Departamento = "TI"; // Define "TI" para Técnico e Gerente
            Console.WriteLine("Departamento definido automaticamente para: TI");
        }
        else if (u.TipoDeUsuario == TipoUsuario.RH)
        {
            u.Departamento = "RH"; // Define "RH" para RH
            Console.WriteLine("Departamento definido automaticamente para: RH");
        }


        Console.Write("Telefone: ");
        u.Telefone = Console.ReadLine();
        // Adicionar validação de formato de telefone, se necessário.

        // A DataCadastro e o Status (ATIVO) são definidos no construtor da classe Usuario

        Console.Write("Senha: ");
        string senhaTextoClaro = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(senhaTextoClaro))
        {
            Console.WriteLine("Senha não pode ser vazia.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }
        // A senha em texto claro é passada para o método de cadastro para ser hasheada

        // Não solicita mais o Status, pois o construtor define como ATIVO

        List<int> habilidadesSelecionadas = new List<int>();

        if (u.TipoDeUsuario == TipoUsuario.TECNICO)
        {
            Console.WriteLine("\n==== SELECIONE AS HABILIDADES (Técnico) ====");
            List<Habilidade> habilidadesDisponiveis = GetHabilidadesDisponiveis();

            if (habilidadesDisponiveis.Count > 0)
            {
                Console.WriteLine("Habilidades disponíveis:");
                for (int i = 0; i < habilidadesDisponiveis.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {habilidadesDisponiveis[i].Nome}");
                }
                Console.WriteLine("Digite o NÚMERO das habilidades separadas por vírgula (ex: 1,3,5) ou deixe em branco para nenhuma:");

                string inputHabilidades = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(inputHabilidades))
                {
                    string[] selecoes = inputHabilidades.Split(',');
                    foreach (string selecao in selecoes)
                    {
                        if (int.TryParse(selecao.Trim(), out int indice) && indice > 0 && indice <= habilidadesDisponiveis.Count)
                        {
                            // Adiciona o ID real da habilidade selecionada
                            habilidadesSelecionadas.Add(habilidadesDisponiveis[indice - 1].Id);
                        }
                        else
                        {
                            Console.WriteLine($"A seleção '{selecao.Trim()}' é inválida e será ignorada.");
                        }
                    }
                    // Opcional: Remover IDs duplicados caso o usuário digite o mesmo número mais de uma vez
                    habilidadesSelecionadas = habilidadesSelecionadas.Distinct().ToList();
                }
            }
            else
            {
                Console.WriteLine("Nenhuma habilidade disponível na base de dados para seleção.");
            }
        }


        // Passa a senha e a lista de habilidades (agora com a nova lógica) para o método de cadastro
        bool sucesso = u.CadastrarUsuario(senhaTextoClaro, habilidadesSelecionadas);

        Console.WriteLine(sucesso ? $"Usuário {u.Nome} cadastrado com sucesso! ID: {u.Id}" : "Erro ao cadastrar usuário. Verifique os dados (ex: email duplicado).");
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    // === MÉTODO: BUSCAR HABILIDADES DISPONÍVEIS NO BANCO ===
    static List<Habilidade> GetHabilidadesDisponiveis()
    {
        List<Habilidade> habilidades = new List<Habilidade>();
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string sql = "SELECT ID, Nome, Descricao FROM HABILIDADE ORDER BY NOME"; // Busca todas as habilidades
                using (var cmd = new OracleCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            habilidades.Add(new Habilidade
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Nome = reader["Nome"].ToString(),
                                Descricao = reader["Descricao"].ToString() // Pode ser útil para exibição futura
                            });
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao buscar habilidades: " + ex.Message);
            // Em um sistema real, você pode registrar este erro em um log
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao buscar habilidades: " + ex.Message);
            // Em um sistema real, você pode registrar este erro em um log
        }
        return habilidades;
    }

    // === MÉTODO: BUSCAR DEPARTAMENTOS DISPONÍVEIS NO BANCO ===
    static List<Departamento> GetDepartamentosDisponiveis()
    {
        List<Departamento> departamentos = new List<Departamento>();
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string sql = "SELECT ID, Nome, Descricao FROM DEPARTAMENTO ORDER BY Nome"; // Busca todos os departamentos
                using (var cmd = new OracleCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            departamentos.Add(new Departamento
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Nome = reader["Nome"].ToString(),
                                Descricao = reader["Descricao"].ToString() 
                            });
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao buscar departamentos: " + ex.Message);
            // Em um sistema real, você pode registrar este erro em um log
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao buscar departamentos: " + ex.Message);
            // Em um sistema real, você pode registrar este erro em um log
        }
        return departamentos;
    }

    // Classe auxiliar simples para representar Habilidade (pode ser movida para um arquivo separado)
    public class Habilidade
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
    }

    // Classe auxiliar simples para representar Departamento (pode ser movida para um arquivo separado)
    public class Departamento
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
    }


    // Método de Validação de Email (mantido)
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }


    static void Login()
    {
        Console.Clear();
        Console.WriteLine("==== LOGIN ====");

        Console.Write("ID: ");
        int id;
        if (!int.TryParse(Console.ReadLine(), out id))
        {
            Console.WriteLine("ID inválido.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("\n==== SELECIONE SEU TIPO DE USUÁRIO ====");
        var tiposUsuario = Enum.GetValues(typeof(TipoUsuario));
        for (int i = 0; i < tiposUsuario.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {tiposUsuario.GetValue(i)}");
        }

        bool tipoValidoLogin = false;
        TipoUsuario tipo = TipoUsuario.COMUM;
        while (!tipoValidoLogin)
        {
            Console.Write($"Digite o NÚMERO do seu tipo de usuário (1 a {tiposUsuario.Length}): ");
            string tipoInput = Console.ReadLine();
            if (int.TryParse(tipoInput, out int indiceTipo) && indiceTipo > 0 && indiceTipo <= tiposUsuario.Length)
            {
                tipo = (TipoUsuario)tiposUsuario.GetValue(indiceTipo - 1);
                tipoValidoLogin = true;
            }
            else
            {
                Console.WriteLine("Seleção de tipo de usuário inválida. Tente novamente.");
            }
        }

        string departamento = null;
        if (tipo == TipoUsuario.COMUM)
        {
            Console.WriteLine("\n==== SELECIONE SEU DEPARTAMENTO ====");
            List<Departamento> departamentosDisponiveis = GetDepartamentosDisponiveis();

            if (departamentosDisponiveis.Count > 0)
            {
                Console.WriteLine("Departamentos disponíveis:");
                for (int i = 0; i < departamentosDisponiveis.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {departamentosDisponiveis[i].Nome}");
                }

                bool selecaoValida = false;
                while (!selecaoValida)
                {
                    Console.Write($"Digite o NÚMERO do seu departamento (1 a {departamentosDisponiveis.Count}): ");
                    string inputDepartamento = Console.ReadLine();

                    if (int.TryParse(inputDepartamento, out int indiceDepartamento) && indiceDepartamento > 0 && indiceDepartamento <= departamentosDisponiveis.Count)
                    {
                        // Atribui o NOME do departamento selecionado
                        departamento = departamentosDisponiveis[indiceDepartamento - 1].Nome;
                        selecaoValida = true;
                    }
                    else
                    {
                        Console.WriteLine("Seleção de departamento inválida. Por favor, digite o número correto.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Nenhum departamento disponível na base de dados.");
                // Se não houver departamentos, talvez não seja possível logar como comum
                Console.WriteLine("Não é possível logar como usuário COMUM sem departamentos disponíveis. Pressione Enter para continuar.");
                Console.ReadLine();
                return; // Sai da função de login
            }
        }


        Console.Write("Senha: ");
        string senha = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(senha))
        {
            Console.WriteLine("Senha não pode ser vazia.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }


        Usuario authenticatedUser = Usuario.Login(id, tipo, departamento, senha); // Passa o departamento (será null para outros tipos)

        if (authenticatedUser != null)
        {
            Console.WriteLine("Login bem-sucedido!");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();

            // Direciona para o menu apropriado com base no tipo de usuário
            switch (authenticatedUser.TipoDeUsuario)
            {
                case TipoUsuario.COMUM:
                    authenticatedUser.Menu(); // Chamada para o menu do próprio objeto Usuario (Comum)
                    break;
                case TipoUsuario.TECNICO:
                    Tecnico tecnicoMenu = new Tecnico(authenticatedUser);
                    tecnicoMenu.Menu();
                    break;
                case TipoUsuario.GERENTE:
                    Gerente gerenteMenu = new Gerente(authenticatedUser);
                    gerenteMenu.Menu();
                    break;
                case TipoUsuario.RH:
                    Rh rhMenu = new Rh(authenticatedUser);
                    rhMenu.Menu();
                    break;
                default:
                    Console.WriteLine("Tipo de usuário desconhecido. Não é possível exibir o menu.");
                    Console.WriteLine("Pressione Enter para continuar.");
                    Console.ReadLine();
                    break;
            }
        }
        else
        {
            // A mensagem de erro específica (senha inválida, usuário não encontrado)
            // já foi exibida dentro do método Usuario.Login.
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
        }
    }
}