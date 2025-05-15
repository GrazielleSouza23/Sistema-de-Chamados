using System;
using Oracle.ManagedDataAccess.Client;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Collections.Generic;
using System.Linq;

public enum TipoUsuario { COMUM, TECNICO, GERENTE, RH }
public enum StatusUsuario { ATIVO, INATIVO }

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public DateTime DataNascimento { get; set; }
    public TipoUsuario TipoDeUsuario { get; set; }
    public string Departamento { get; set; }
    public string Telefone { get; set; }
    public DateTime DataCadastro { get; set; }
    public StatusUsuario Status { get; set; }
    private string Senha { get; set; }

    public Usuario()
    {
        DataCadastro = DateTime.Now;
        Status = StatusUsuario.ATIVO; 
    }

    public Usuario(int id, string nome, string email, DateTime dataNascimento, TipoUsuario tipoDeUsuario, string departamento, string telefone, DateTime dataCadastro, StatusUsuario status, string senha)
    {
        Id = id;
        Nome = nome;
        Email = email;
        DataNascimento = dataNascimento;
        TipoDeUsuario = tipoDeUsuario;
        Departamento = departamento;
        Telefone = telefone;
        DataCadastro = dataCadastro;
        Status = status;
        Senha = senha;
    }


    public bool CadastrarUsuario(string senha, List<int> habilidadesSelecionadas = null)
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(senha))
        {
            return false;
        }
        if (!IsValidEmail(Email))
        {
           return false;
        }

        this.Senha = senha;

        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        this.Id = GetNextUsuarioId(conn);

                        string query = @"
                            INSERT INTO Usuario (
                                ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento,
                                Telefone, DataCadastro, Status, Senha
                            )
                            VALUES (
                                :id, :nome, :email, :dataNascimento, :tipoDeUsuario, :departamento,
                                :telefone, :dataCadastro, :status, :senha
                            )";

                        using (var cmd = new OracleCommand(query, conn))
                        {
                            cmd.Transaction = transaction; 
                            cmd.Parameters.Add(":id", OracleDbType.Decimal).Value = this.Id;
                            cmd.Parameters.Add(":nome", OracleDbType.Varchar2).Value = this.Nome;
                            cmd.Parameters.Add(":email", OracleDbType.Varchar2).Value = this.Email;
                            cmd.Parameters.Add(":dataNascimento", OracleDbType.Date).Value = this.DataNascimento;
                            cmd.Parameters.Add(":tipoDeUsuario", OracleDbType.Varchar2).Value = this.TipoDeUsuario.ToString();
                            cmd.Parameters.Add(":departamento", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(this.Departamento) ? DBNull.Value : (object)this.Departamento;
                            cmd.Parameters.Add(":telefone", OracleDbType.Varchar2).Value = this.Telefone;
                            cmd.Parameters.Add(":dataCadastro", OracleDbType.Date).Value = this.DataCadastro;
                            cmd.Parameters.Add(":status", OracleDbType.Varchar2).Value = this.Status.ToString();
                            cmd.Parameters.Add(":senha", OracleDbType.Varchar2).Value = this.Senha;
                            int rowsInserted = cmd.ExecuteNonQuery();

                            if (rowsInserted > 0)
                            {
                                if (this.TipoDeUsuario == TipoUsuario.TECNICO && habilidadesSelecionadas != null && habilidadesSelecionadas.Count > 0)
                                {
                                    string insertHabilidadeTecnicoSql = @"
                                        INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade)
                                        VALUES (:idTecnico, :idHabilidade)";

                                    foreach (int habilidadeId in habilidadesSelecionadas)
                                    {
                                        using (var cmdHabilidade = new OracleCommand(insertHabilidadeTecnicoSql, conn))
                                        {
                                            cmdHabilidade.Transaction = transaction;
                                            cmdHabilidade.Parameters.Add(":idTecnico", OracleDbType.Decimal).Value = this.Id;
                                            cmdHabilidade.Parameters.Add(":idHabilidade", OracleDbType.Decimal).Value = habilidadeId;
                                            cmdHabilidade.ExecuteNonQuery();
                                        }
                                    }
                                }

                                transaction.Commit();
                                return true;
                            }
                            else
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }
                    }
                    catch (OracleException ex)
                    {
                        transaction.Rollback();
                        if (ex.Number == 1)
                        {
                            if (ex.Message.Contains("USUARIO_EMAIL_UK"))
                            {
                                Console.WriteLine("Erro ao registrar usuário: Email já registrado.");
                            }
                            else if (ex.Message.Contains("HABILIDADE_TECNICO_PK"))
                            {
                                Console.WriteLine("Erro ao registrar habilidades: Uma ou mais habilidades já foram associadas a este técnico."); // Although the UI selection by number should prevent this, database validation is important.
                            }
                            else
                            {
                                Console.WriteLine("Erro ao registrar: Violação de restrição única. Verifique os dados fornecidos.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Erro Oracle durante o registro: " + ex.Message);
                        }
                        return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("Erro inesperado durante o registro: " + ex.Message);
                        return false;
                    }
                } 
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter conexão para registro: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter conexão para registro: " + ex.Message);
            return false;
        }
    }


    private static int GetNextUsuarioId(OracleConnection conn)
    {
        string query = "SELECT usuario_seq.NEXTVAL FROM dual";
        using (var cmd = new OracleCommand(query, conn))
        {
            if (conn.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("A conexão com o banco de dados não está aberta.");
            }
            object result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                throw new Exception("Falha ao obter o próximo ID da sequência de usuário.");
            }
        }
    }

    public static Usuario Login(int id, TipoUsuario tipo, string departamento, string senhaTextoClaro)
    {
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();

                string query = @"
            SELECT ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha
            FROM Usuario
            WHERE ID = :id AND TipoDeUsuario = :tipoDeUsuario";

                if (tipo == TipoUsuario.COMUM)
                {
                    query += " AND Departamento = :departamento";
                }

                using (var cmd = new OracleCommand(query, conn))
                {
                    cmd.Parameters.Add(":id", OracleDbType.Decimal).Value = id;
                    cmd.Parameters.Add(":tipoDeUsuario", OracleDbType.Varchar2).Value = tipo.ToString();

                    if (tipo == TipoUsuario.COMUM)
                    {
                        cmd.Parameters.Add(":departamento", OracleDbType.Varchar2).Value = departamento;
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Usuario loggedInUser = new Usuario
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Nome = reader["Nome"].ToString(),
                                Email = reader["Email"].ToString(),
                                DataNascimento = Convert.ToDateTime(reader["DataNascimento"]),
                                TipoDeUsuario = (TipoUsuario)Enum.Parse(typeof(TipoUsuario), reader["TipoDeUsuario"].ToString()),
                                Departamento = reader["Departamento"] == DBNull.Value ? null : reader["Departamento"].ToString(),
                                Telefone = reader["Telefone"].ToString(),
                                DataCadastro = Convert.ToDateTime(reader["DataCadastro"]),
                                Status = (StatusUsuario)Enum.Parse(typeof(StatusUsuario), reader["Status"].ToString()),
                                Senha = reader["Senha"].ToString()
                            };

                            if (senhaTextoClaro == loggedInUser.Senha)
                            {
                                return loggedInUser;
                            }
                            else
                            {
                                Console.WriteLine("Senha inválida.");
                                return null;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Usuário não encontrado ou credenciais incorretas.");
                            return null;
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle durante o login: " + ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado durante o login: " + ex.Message);
            return null;
        }
    }



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

    public static bool UsuarioExiste(int idUsuario)
    {
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM Usuario WHERE ID = :idUsuario";
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(":idUsuario", OracleDbType.Decimal).Value = idUsuario;
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao verificar a existência do usuário por ID: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao verificar a existência do usuário por ID: " + ex.Message);
            return false;
        }
    }

    public void Menu()
    {
        if (this.TipoDeUsuario != TipoUsuario.COMUM)
        {
            Console.WriteLine("Erro: Este menu é apenas para usuários do tipo COMUM.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }

        bool running = true;
        while (running)
        {
            Console.Clear();
            Console.WriteLine($"==== MENU COMUM - Bem-vindo, {this.Nome} ====");
            Console.WriteLine("1. Registrar Chamado");
            Console.WriteLine("2. Acompanhar Status de Chamado");
            Console.WriteLine("3. Avaliar Atendimento");
            Console.WriteLine("4. Receber Notificações");
            Console.WriteLine("5. Verificar Solução da IA");
            Console.WriteLine("6. Sair");
            Console.Write("Escolha uma opção: ");

            switch (Console.ReadLine())
            {
                case "1":
                    RegistrarChamado();
                    break;
                case "2":
                    AcompanharStatusChamado();
                    break;
                case "3":
                    AvaliarAtendimento();
                    break;
                case "4":
                    ReceberNotificacoes();
                    break;
                case "5":
                    VerificarSolucaoIA();
                    break;
                case "6": 
                    running = false;
                    Console.WriteLine("Saindo do menu de usuário comum...");
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

    private void RegistrarChamado()
    {
        Console.Clear();
        Console.WriteLine("==== REGISTRAR CHAMADO ====");

        Console.Write("Descrição do Problema: ");
        string descricao = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(descricao))
        {
            Console.WriteLine("A descrição do problema não pode ser vazia.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("\n==== SELECIONE A CATEGORIA ====");
        List<string> categoriasDisponiveis = IA.GetCategoriasSLA();

        if (categoriasDisponiveis.Count == 0)
        {
            Console.WriteLine("Nenhuma categoria de chamado disponível no momento. Não é possível registrar o chamado.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
            return;
        }

        for (int i = 0; i < categoriasDisponiveis.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {categoriasDisponiveis[i]}");
        }

        string categoria = null;
        bool categoriaValida = false;
        while (!categoriaValida)
        {
            Console.Write($"Digite o NÚMERO da categoria (1 a {categoriasDisponiveis.Count}): ");
            string inputCategoria = Console.ReadLine();

            if (int.TryParse(inputCategoria, out int indiceCategoria) && indiceCategoria > 0 && indiceCategoria <= categoriasDisponiveis.Count)
            {
                categoria = categoriasDisponiveis[indiceCategoria - 1];
                categoriaValida = true;
            }
            else
            {
                Console.WriteLine("Seleção de categoria inválida. Por favor, digite o número correto.");
            }
        }


        int newChamadoId;
        bool sucesso = Chamado.RegistrarChamado(descricao, categoria, this.Id, out newChamadoId);

        Console.WriteLine(sucesso ? $"Chamado registrado com sucesso! ID: {newChamadoId}" : "Erro ao registrar chamado.");
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
        Console.Clear();
        Menu();
    }

    private void AcompanharStatusChamado()
    {
        Console.Clear();
        Console.WriteLine("==== ACOMPANHAR STATUS DE CHAMADO ====");
        Console.Write("Digite o ID do chamado para acompanhar o status: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            Chamado.AcompanharStatusChamado(idChamado);
        }
        else
        {
            Console.WriteLine("ID de chamado inválido.");
            Console.WriteLine("Pressione Enter para continuar.");
            Console.ReadLine();
        }
    }

    private void AvaliarAtendimento()
    {
        Console.Clear();
        Console.WriteLine("==== AVALIAR ATENDIMENTO ====");
        Console.Write("Digite o ID do chamado para avaliar: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            Console.Write("Digite a nota (0-10): ");
            if (int.TryParse(Console.ReadLine(), out int nota))
            {
                if (nota < 0 || nota > 10)
                {
                    Console.WriteLine("A nota deve ser entre 0 e 10.");
                    Console.WriteLine("Pressione Enter para continuar.");
                    Console.ReadLine();
                    return;
                }
                Console.Write("Digite um comentário: ");
                string comentario = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(comentario))
                {
                    Console.WriteLine("O comentário da avaliação não pode ser vazio.");
                }
                Chamado.AvaliarAtendimento(idChamado, nota, comentario, this.Id);

            }
            else
            {
                Console.WriteLine("Pontuação inválida.");
            }
        }
        else
        {
            Console.WriteLine("ID de chamado inválido.");
        }
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void ReceberNotificacoes()
    {
        Console.Clear();
        Console.WriteLine("==== NOTIFICAÇÕES ====");
        Chamado.ReceberNotificacoes(this.Id);
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }


    private void VerificarSolucaoIA()
    {
        Console.Clear();
        Console.WriteLine("==== VERIFICAR SOLUÇÃO DA IA ====");
        Console.Write("Digite o ID do chamado que você registrou: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            // Obter detalhes do chamado
            Chamado chamado = Chamado.GetChamadoDetails(idChamado);

            // Verificar se o chamado existe e se pertence ao usuário logado
            if (chamado == null)
            {
                Console.WriteLine($"Chamado com ID {idChamado} não encontrado.");
            }
            else if (chamado.IdSolicitante != this.Id)
            {
                Console.WriteLine($"O chamado com ID {idChamado} não foi registrado por você.");
            }
            else if (chamado.Status != StatusChamado.ABERTO)
            {
                Console.WriteLine($"O chamado {idChamado} não está no status ABERTO. Status atual: {chamado.Status}.");
                Console.WriteLine("A verificação da solução da IA só é possível para chamados ABERTOS.");
            }
            else
            {
                Console.WriteLine("\n--- Solução Sugerida pela IA ---");
                if (!string.IsNullOrWhiteSpace(chamado.SolucaoAplicada))
                {
                    Console.WriteLine($"Solução: {(chamado.SolucaoAplicada)}");
                    Console.WriteLine("\nEsta solução resolveu o seu problema? (S/N)");
                    string resposta = Console.ReadLine().Trim().ToUpper();

                    if (resposta == "S")
                    {
                        // Usuário aceitou a solução, fechar o chamado
                        Console.WriteLine("Você aceitou a solução. Fechando o chamado...");

                        bool sucessoFechamento = Chamado.AtualizarChamadoStatusSolucao(chamado.Id, StatusChamado.RESOLVIDO, chamado.SolucaoAplicada, DateTime.Now, this.Id);

                        Console.WriteLine(sucessoFechamento ? "Chamado fechado com sucesso como RESOLVIDO!" : "Erro ao fechar o chamado.");

                    }
                    else if (resposta == "N")
                    {
                        // Usuário não aceitou a solução, encaminhar para um técnico
                        Console.WriteLine("Você não aceitou a solução. Encaminhando para um técnico...");
                        // Chamar um novo método para atribuir a um técnico (vamos criar este método em Chamado.cs)
                        bool sucessoAtribuicao = Chamado.DesignarTecnico(chamado.Id, this.Id); // Passa o ID do usuário logado como quem solicitou a atribuição

                        Console.WriteLine(sucessoAtribuicao ? "Chamado atribuído a um técnico." : "Erro ao atribuir o chamado a um técnico.");

                    }
                    else
                    {
                        Console.WriteLine("Opção inválida. O chamado permanece ABERTO.");
                    }
                }
                else
                {
                    Console.WriteLine("Nenhuma solução sugerida pela IA para este chamado no momento.");
                    // O chamado já está ABERTO, então nenhuma ação adicional é necessária aqui
                    // exceto talvez informar ao usuário que ele pode aguardar um técnico.
                    Console.WriteLine("Aguarde, o chamado será revisado por um técnico se necessário.");
                }
            }
        }
        else
        {
            Console.WriteLine("ID de chamado inválido.");
        }
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }
   
}