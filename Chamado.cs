using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Linq;

public enum StatusChamado { ABERTO, EM_ANDAMENTO, RESOLVIDO, FECHADO }
public enum NivelUrgencia { BAIXO, MEDIO, ALTO, CRITICO }

public class Chamado
{
    public int Id { get; private set; }
    public string Descricao { get; set; }
    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
    public StatusChamado Status { get; set; }
    public NivelUrgencia NivelUrgencia { get; set; }
    public string Categoria { get; set; }
    public double? TempoTotalAtendimento { get; set; }
    public int IdSolicitante { get; set; }
    public int? IdTecnicoResponsavel { get; set; }
    public string SolucaoAplicada { get; set; }

    public Chamado()
    {
        DataAbertura = DateTime.Now;
        Status = StatusChamado.ABERTO; 
    }

    // Construtor para carregar um chamado do banco (com ID)
    public Chamado(int id, string descricao, DateTime dataAbertura, DateTime? dataFechamento, StatusChamado status, NivelUrgencia nivelUrgencia, string categoria, double? tempoTotalAtendimento, int idSolicitante, int? idTecnicoResponsavel, string solucaoAplicada)
    {
        Id = id;
        Descricao = descricao;
        DataAbertura = dataAbertura;
        DataFechamento = dataFechamento;
        Status = status;
        NivelUrgencia = nivelUrgencia;
        Categoria = categoria;
        TempoTotalAtendimento = tempoTotalAtendimento;
        IdSolicitante = idSolicitante;
        IdTecnicoResponsavel = idTecnicoResponsavel;
        SolucaoAplicada = solucaoAplicada;
    }


    public static bool RegistrarChamado(string descricao, string categoria, int idSolicitante, out int newChamadoId)
    {
        newChamadoId = 0;

        // Validação básica
        if (string.IsNullOrWhiteSpace(descricao) || string.IsNullOrWhiteSpace(categoria))
        {
            Console.WriteLine("Erro de validação: Descrição e Categoria do chamado são obrigatórios.");
            return false;
        }
        if (!Usuario.UsuarioExiste(idSolicitante))
        {
            Console.WriteLine($"Erro: Solicitante com ID {idSolicitante} não encontrado.");
            return false;
        }


        Chamado novoChamado = new Chamado
        {
            Descricao = descricao,
            Categoria = categoria,
            IdSolicitante = idSolicitante,
            Status = StatusChamado.ABERTO,
            DataAbertura = DateTime.Now 
        };


        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente no bloco using

                // Iniciar uma transação para garantir atomicidade
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Processar chamado com a IA para determinar urgência e sugerir solução
                        Console.WriteLine("Processando chamado com a IA...");
                        IA.ResultadoIA iaResult = IA.ProcessarChamado(novoChamado.Descricao, novoChamado.Categoria, conn, transaction); // Updated class name

                        // Set the properties based on IA result
                        novoChamado.NivelUrgencia = iaResult.DeterminarUrgencia;
                        novoChamado.SolucaoAplicada = iaResult.SolucaoSugerida;

                        // Obtém o próximo ID da sequence para o chamado (passa conexão para o método auxiliar)
                        novoChamado.Id = GetNextChamadoId(conn); // Ensure ID is generated within the transaction scope

                        string sqlInsertChamado = @"
                            INSERT INTO Chamado (
                                ID, Descricao, DataAbertura, Status, NivelUrgencia, Categoria,
                                ID_Solicitante, SolucaoAplicada
                            ) VALUES (
                                :id, :descricao, :dataAbertura, :status, :nivelUrgencia, :categoria,
                                :idSolicitante, :solucaoAplicada
                            )";

                        using (var cmd = new OracleCommand(sqlInsertChamado, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.Parameters.Add(":id", OracleDbType.Decimal).Value = novoChamado.Id;
                            cmd.Parameters.Add(":descricao", OracleDbType.Varchar2).Value = novoChamado.Descricao;
                            cmd.Parameters.Add(":dataAbertura", OracleDbType.Date).Value = novoChamado.DataAbertura;
                            cmd.Parameters.Add(":status", OracleDbType.Varchar2).Value = novoChamado.Status.ToString();
                            cmd.Parameters.Add(":nivelUrgencia", OracleDbType.Varchar2).Value = novoChamado.NivelUrgencia.ToString();
                            cmd.Parameters.Add(":categoria", OracleDbType.Varchar2).Value = novoChamado.Categoria;
                            cmd.Parameters.Add(":idSolicitante", OracleDbType.Decimal).Value = novoChamado.IdSolicitante;
                            cmd.Parameters.Add(":solucaoAplicada", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(novoChamado.SolucaoAplicada) ? DBNull.Value : (object)novoChamado.SolucaoAplicada;

                            int rowsInserted = cmd.ExecuteNonQuery();

                            if (rowsInserted > 0)
                            {
                                newChamadoId = novoChamado.Id;

                                AdicionarHistoricoChamado(novoChamado.Id, "Chamado aberto pelo solicitante e triado pela IA.", novoChamado.IdSolicitante, conn, transaction); // Pass conn and transaction

                                string notificationMessage = $"Chamado #{novoChamado.Id} aberto. Status: {novoChamado.Status}, Urgência: {novoChamado.NivelUrgencia}.";
                                if (!string.IsNullOrEmpty(novoChamado.SolucaoAplicada))
                                {
                                    notificationMessage += $" IA sugeriu uma solução. Verifique a solução no menu 'Verificar Solução da IA'."; // User will see the suggestion when viewing details
                                }
                                else
                                {
                                    notificationMessage += " IA não sugeriu uma solução automática. Aguarde, o chamado será revisado por um técnico se necessário.";
                                }
                                EnviarNotificacao(novoChamado.IdSolicitante, notificationMessage, novoChamado.Id, "ATUALIZACAO", conn, transaction);


                                transaction.Commit();
                                return true;
                            }
                            else
                            {
                                transaction.Rollback();
                                Console.WriteLine("Inserção do chamado no banco falhou.");
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("Erro durante o registro do chamado e processamento da IA: " + ex.Message);
                        return false;
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter conexão para registrar chamado: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter conexão para registrar chamado: " + ex.Message);
            return false;
        }
    }


    private static int GetNextChamadoId(OracleConnection conn)
    {
        string query = "SELECT chamado_seq.NEXTVAL FROM dual";
        using (var cmd = new OracleCommand(query, conn))
        {
            if (conn.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Conexão com o banco de dados não está aberta.");
            }
            object result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                throw new Exception("Falha em obter o próximo ID da sequência de chamado.");
            }
        }
    }


    public static void AcompanharStatusChamado(int idChamado)
    {
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente no bloco using

                // Obter detalhes do chamado
                string sqlChamado = @"
                    SELECT ID, Descricao, DataAbertura, DataFechamento, Status, NivelUrgencia, Categoria,
                           ID_Solicitante, ID_TecnicoResponsavel, SolucaoAplicada, TempoTotalAtendimento
                    FROM Chamado
                    WHERE ID = :idChamado";

                Chamado chamado = null;
                using (var cmdChamado = new OracleCommand(sqlChamado, conn))
                {
                    // ID é NUMBER no BD -> Decimal em C#
                    cmdChamado.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                    using (var readerChamado = cmdChamado.ExecuteReader())
                    {
                        if (readerChamado.Read())
                        {
                            chamado = new Chamado
                            {
                                Id = Convert.ToInt32(readerChamado["ID"]),
                                Descricao = readerChamado["Descricao"].ToString(),
                                DataAbertura = Convert.ToDateTime(readerChamado["DataAbertura"]),
                                DataFechamento = readerChamado["DataFechamento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(readerChamado["DataFechamento"]),
                                Status = (StatusChamado)Enum.Parse(typeof(StatusChamado), readerChamado["Status"].ToString()),
                                NivelUrgencia = (NivelUrgencia)Enum.Parse(typeof(NivelUrgencia), readerChamado["NivelUrgencia"].ToString()),
                                Categoria = readerChamado["Categoria"].ToString(),
                                TempoTotalAtendimento = readerChamado["TempoTotalAtendimento"] == DBNull.Value ? (double?)null : Convert.ToDouble(readerChamado["TempoTotalAtendimento"]),
                                IdSolicitante = Convert.ToInt32(readerChamado["ID_Solicitante"]),
                                IdTecnicoResponsavel = readerChamado["ID_TecnicoResponsavel"] == DBNull.Value ? (int?)null : Convert.ToInt32(readerChamado["ID_TecnicoResponsavel"]),
                                SolucaoAplicada = readerChamado["SolucaoAplicada"] == DBNull.Value ? null : readerChamado["SolucaoAplicada"].ToString()
                            };

                            Console.WriteLine("\n--- Detalhes do Chamado ---");
                            Console.WriteLine($"ID: {chamado.Id}");
                            Console.WriteLine($"Descrição: {chamado.Descricao}");
                            Console.WriteLine($"Data Abertura: {chamado.DataAbertura:yyyy-MM-dd HH:mm:ss}"); // Formata data
                            Console.WriteLine($"Data Fechamento: {chamado.DataFechamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Aberto"}"); // Formata data
                            Console.WriteLine($"Status: {chamado.Status}");
                            Console.WriteLine($"Urgência: {chamado.NivelUrgencia}");
                            Console.WriteLine($"Categoria: {chamado.Categoria}");
                            Console.WriteLine($"Tempo Total Atendimento: {chamado.TempoTotalAtendimento?.ToString("F2") ?? "N/A"} minutos"); // Formata tempo
                            Console.WriteLine($"Solicitante ID: {chamado.IdSolicitante}");
                            Console.WriteLine($"Técnico Responsável ID: {chamado.IdTecnicoResponsavel?.ToString() ?? "Não Atribuído"}");
                            Console.WriteLine($"Solução Aplicada: {chamado.SolucaoAplicada ?? "N/A"}");
                        }
                        else
                        {
                            Console.WriteLine($"Chamado com ID {idChamado} não encontrado.");
                            Console.WriteLine("Pressione Enter para continuar."); // Mantido para feedback
                            Console.ReadLine();
                            return;
                        }
                    }
                }

                // Obter histórico do chamado
                string sqlHistorico = @"
                    SELECT DataAtualizacao, DescricaoAtualizacao, ID_UsuarioAtualizou
                    FROM HistoricoChamado
                    WHERE ID_Chamado = :idChamado
                    ORDER BY DataAtualizacao ASC";

                using (var cmdHistorico = new OracleCommand(sqlHistorico, conn))
                {
                    // ID_Chamado é NUMBER no BD -> Decimal em C#
                    cmdHistorico.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                    using (var readerHistorico = cmdHistorico.ExecuteReader())
                    {
                        Console.WriteLine("\n--- Histórico do Chamado ---");
                        if (!readerHistorico.HasRows)
                        {
                            Console.WriteLine("Nenhum histórico encontrado para este chamado.");
                        }
                        else
                        {
                            while (readerHistorico.Read())
                            {
                                Console.WriteLine($"Data: {Convert.ToDateTime(readerHistorico["DataAtualizacao"]):yyyy-MM-dd HH:mm:ss} - Atualização: {readerHistorico["DescricaoAtualizacao"]} (Atualizado por Usuário ID: {readerHistorico["ID_UsuarioAtualizou"]})"); // Formata data
                            }
                        }
                    }
                }

                // Se o chamado estiver em um estado que permite avaliação (RESOLVIDO ou FECHADO)
                if (chamado != null && (chamado.Status == StatusChamado.RESOLVIDO || chamado.Status == StatusChamado.FECHADO))
                {
                    // Verificar se já existe avaliação para este chamado (passa conexão para o método auxiliar)
                    if (!AvaliacaoExiste(chamado.Id, conn))
                    {
                        Console.WriteLine("\nEste chamado está pronto para ser avaliado.");
                        // Em uma aplicação de console, você poderia perguntar ao usuário se ele quer avaliar agora.
                        // Aqui apenas indicamos que a avaliação é possível.
                    }
                    else
                    {
                        Console.WriteLine("\nEste chamado já foi avaliado.");
                        ExibirAvaliacaoExistente(chamado.Id, conn); // Opcional: mostrar a avaliação existente (passa conexão para o método auxiliar)
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao acompanhar status do chamado: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao acompanhar status do chamado: " + ex.Message);
        }
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    // Verificar se uma avaliação para o chamado já existe na tabela Avaliacao (Recebe a conexão)
    private static bool AvaliacaoExiste(int idChamado, OracleConnection conn)
    {
        try
        {
            string sql = "SELECT COUNT(*) FROM Avaliacao WHERE ID_Chamado = :idChamado";
            using (var cmd = new OracleCommand(sql, conn))
            {
                // ID_Chamado é NUMBER no BD -> Decimal em C#
                cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao verificar existência de avaliação: " + ex.Message);
            return false; // Assume que não existe em caso de erro
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao verificar existência de avaliação: " + ex.Message);
            return false; // Assume que não existe em caso de erro
        }
    }

    // Método opcional para exibir a avaliação existente (Recebe a conexão)
    private static void ExibirAvaliacaoExistente(int idChamado, OracleConnection conn)
    {
        try
        {
            string sql = "SELECT Nota, Comentario, DataAvaliacao, ID_Usuario FROM Avaliacao WHERE ID_Chamado = :idChamado";
            using (var cmd = new OracleCommand(sql, conn))
            {
                // ID_Chamado é NUMBER no BD -> Decimal em C#
                cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int nota = Convert.ToInt32(reader["Nota"]);
                        string comentario = reader["Comentario"].ToString();
                        DateTime dataAvaliacao = Convert.ToDateTime(reader["DataAvaliacao"]);
                        int idUsuarioAvaliou = Convert.ToInt32(reader["ID_Usuario"]);

                        Console.WriteLine("\n--- Avaliação ---");
                        Console.WriteLine($"Nota: {nota}/10");
                        Console.WriteLine($"Comentário: {comentario}");
                        Console.WriteLine($"Data da Avaliação: {dataAvaliacao:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Avaliado por Usuário ID: {idUsuarioAvaliou}");
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao exibir avaliação: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao exibir avaliação: " + ex.Message);
        }
    }


    // Method to rate a ticket (used by all user types) - Receives the ID of the user doing the rating
    public static void AvaliarAtendimento(int idChamado, int nota, string comentario, int idUsuarioAvaliador)
    {
        if (nota < 0 || nota > 10)
        {
            Console.WriteLine("Erro de validação: A nota deve ser entre 0 e 10.");
            return;
        }
        if (string.IsNullOrWhiteSpace(comentario))
        {
            // Console.WriteLine("Erro de validação: O comentário da avaliação não pode ser vazio."); // Decide if the comment is mandatory or not
        }
        // Check if the evaluating user exists (using the method in Usuario.cs)
        if (!Usuario.UsuarioExiste(idUsuarioAvaliador))
        {
            Console.WriteLine($"Erro: Usuário avaliador com ID {idUsuarioAvaliador} não encontrado.");
            return;
        }


        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                // Iniciar uma transação
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Check if the ticket is in a state that allows rating (RESOLVIDO or FECHADO) (pass connection)
                        string statusChamadoStr = GetStatusChamado(idChamado, conn);

                        if (statusChamadoStr != null && (statusChamadoStr == StatusChamado.RESOLVIDO.ToString() || statusChamadoStr == StatusChamado.FECHADO.ToString()))
                        {
                            // Check if a rating already exists for this ticket (pass connection)
                            if (AvaliacaoExiste(idChamado, conn))
                            {
                                Console.WriteLine($"O chamado {idChamado} já foi avaliado.");
                                transaction.Rollback(); // Cancel the transaction
                                return; // Exit the method if already rated
                            }

                            // Insert the rating into the Avaliacao table
                            string sql = @"
                                INSERT INTO Avaliacao (ID, ID_Chamado, ID_Usuario, Nota, Comentario, DataAvaliacao)
                                VALUES (:idAvaliacao, :idChamado, :idUsuario, :nota, :comentario, :dataAvaliacao)";

                            using (var cmd = new OracleCommand(sql, conn))
                            {
                                cmd.Transaction = transaction; // Associate with the transaction
                                // IDs are NUMBER in DB -> Decimal in C#. Nota is NUMBER/Int. Comentário is Varchar2. Data is Date.
                                cmd.Parameters.Add(":idAvaliacao", OracleDbType.Decimal).Value = GetNextAvaliacaoId(conn); // Get ID from sequence (within transaction)
                                cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                                cmd.Parameters.Add(":idUsuario", OracleDbType.Decimal).Value = idUsuarioAvaliador; // ID of the user who rated
                                cmd.Parameters.Add(":nota", OracleDbType.Int32).Value = nota;
                                cmd.Parameters.Add(":comentario", OracleDbType.Varchar2).Value = comentario;
                                cmd.Parameters.Add(":dataAvaliacao", OracleDbType.Date).Value = DateTime.Now;

                                int rowsInserted = cmd.ExecuteNonQuery();

                                if (rowsInserted > 0)
                                {
                                    Console.WriteLine($"Avaliação para o chamado {idChamado} registrada com sucesso!");
                                    // Add history of rating (Internal static method)
                                    AdicionarHistoricoChamado(idChamado, $"Chamado avaliado com nota {nota} pelo usuário ID {idUsuarioAvaliador}.", idUsuarioAvaliador, conn, transaction); // Pass connection and transaction
                                    transaction.Commit(); // Commit the transaction
                                }
                                else
                                {
                                    Console.WriteLine($"Erro ao registrar avaliação para o chamado {idChamado}.");
                                    transaction.Rollback(); // Rollback the transaction
                                }
                            }
                        }
                        else if (statusChamadoStr != null)
                        {
                            Console.WriteLine($"O chamado {idChamado} não está em um estado (RESOLVIDO ou FECHADO) que permita avaliação.");
                            transaction.Rollback(); // Cancel the transaction
                        }
                        else
                        {
                            Console.WriteLine($"Chamado com ID {idChamado} não encontrado.");
                            transaction.Rollback(); // Cancel the transaction
                        }
                    }
                    catch (OracleException ex)
                    {
                        transaction.Rollback(); // Rollback the transaction in case of Oracle error
                        Console.WriteLine("Erro Oracle ao registrar avaliação: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback the transaction in case of general error
                        Console.WriteLine("Erro inesperado ao registrar avaliação: " + ex.Message);
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter conexão para registrar avaliação: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter conexão para registrar avaliação: " + ex.Message);
        }
        // Removed the duplicate Console.ReadLine() here, as it is already in the calling method in Program.cs or menus.
    }

    // Method to get the next ID from the Avaliacao sequence (Receives connection)
    private static int GetNextAvaliacaoId(OracleConnection conn) // Static method
    {
        string query = "SELECT avaliacao_seq.NEXTVAL FROM dual"; // Assuming the sequence is named 'avaliacao_seq'
        using (var cmd = new OracleCommand(query, conn))
        {
            object result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                throw new Exception("Failed to get the next ID from the avaliacao sequence.");
            }
        }
    }


    // Get the current status of a ticket (Receives connection)
    private static string GetStatusChamado(int idChamado, OracleConnection conn)
    {
        try
        {
            string sql = "SELECT Status FROM Chamado WHERE ID = :idChamado";
            using (var cmd = new OracleCommand(sql, conn))
            {
                // ID is NUMBER in DB -> Decimal in C#
                cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                object status = cmd.ExecuteScalar();
                return status?.ToString();
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter status do chamado: " + ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter status do chamado: " + ex.Message);
            return null;
        }
    }

    // Get the requester ID of a ticket (Receives connection)
    public static int GetIdSolicitanteChamado(int idChamado, OracleConnection conn) // Static method
    {
        try
        {
            string sql = "SELECT ID_Solicitante FROM Chamado WHERE ID = :idChamado";
            using (var cmd = new OracleCommand(sql, conn))
            {
                // ID is NUMBER in DB -> Decimal in C#
                cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                object idSolicitante = cmd.ExecuteScalar();
                return idSolicitante != DBNull.Value ? Convert.ToInt32(idSolicitante) : 0; // Returns 0 or throws exception if not found/null
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter ID do solicitante do chamado: " + ex.Message);
            return 0; // Returns 0 in case of error
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter ID do solicitante do chamado: " + ex.Message);
            return 0; // Returns 0 in case of error
        }
    }


    // Method to retrieve notifications for a user (used by all user types)
    public static void ReceberNotificacoes(int idUsuario)
    {
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT ID, Mensagem, DataEnvio, Tipo, ID_ChamadoRelacionado
                    FROM Notificacao
                    WHERE ID_UsuarioDestinatario = :idUsuario AND Lida = 'N'
                    ORDER BY DataEnvio DESC";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(":idUsuario", OracleDbType.Decimal).Value = idUsuario;
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine($"\n--- Suas Notificações ---");
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("Nenhuma nova notificação.");
                        }
                        else
                        {
                            Console.WriteLine("---------------------------------------------------------------------------------------");
                            Console.WriteLine($"| {"ID",-5} | {"Tipo",-15} | {"Data Envio",-19} | {"ID Chamado",-10} | {"Mensagem",-40} |");
                            Console.WriteLine("---------------------------------------------------------------------------------------");
                            while (reader.Read())
                            {
                                int notifId = Convert.ToInt32(reader["ID"]);
                                string tipo = reader["Tipo"].ToString();
                                DateTime dataEnvio = Convert.ToDateTime(reader["DataEnvio"]);
                                object idChamadoObj = reader["ID_ChamadoRelacionado"];
                                string idChamado = idChamadoObj != DBNull.Value ? idChamadoObj.ToString() : "N/A";
                                string mensagem = reader["Mensagem"].ToString();


                                Console.WriteLine($"| {notifId,-5} | {tipo,-15} | {dataEnvio:yyyy-MM-dd HH:mm:ss},-19 | {idChamado,-10} | {mensagem.Substring(0, Math.Min(mensagem.Length, 40)),-40} |"); // Format date and limit message length
                            }
                            Console.WriteLine("---------------------------------------------------------------------------------------");

                            MarcarNotificacoesComoLidas(idUsuario, conn);
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao buscar notificações: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao buscar notificações: " + ex.Message);
        }
        // Removed duplicate Console.ReadLine() here.
    }

    // Add a record to the ticket history - Receives connection AND transaction. NOW INTERNAL STATIC.
    internal static void AdicionarHistoricoChamado(int idChamado, string descricao, int idUsuarioAtualizou, OracleConnection conn, OracleTransaction transaction)
    {
        try
        {
            // The connection and transaction are received as parameter
            string sql = @"
                     INSERT INTO HistoricoChamado (ID, ID_Chamado, DataAtualizacao, DescricaoAtualizacao, ID_UsuarioAtualizou)
                     VALUES (:idHistorico, :idChamado, :dataAtualizacao, :descricaoAtualizacao, :idUsuarioAtualizou)";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Transaction = transaction; // Associate with transaction
                // IDs are NUMBER in DB -> Decimal in C#. Descrição is Varchar2. Data is Date.
                cmd.Parameters.Add(":idHistorico", OracleDbType.Decimal).Value = GetNextHistoricoChamadoId(conn); // Get ID from sequence (within transaction)
                cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                cmd.Parameters.Add(":dataAtualizacao", OracleDbType.Date).Value = DateTime.Now;
                cmd.Parameters.Add(":descricaoAtualizacao", OracleDbType.Varchar2).Value = descricao;
                cmd.Parameters.Add(":idUsuarioAtualizou", OracleDbType.Decimal).Value = idUsuarioAtualizou;
                cmd.ExecuteNonQuery();
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao adicionar histórico do chamado: " + ex.Message);
            throw; // Re-throw to ensure transaction is handled by the caller
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao adicionar histórico do chamado: " + ex.Message);
            throw; // Re-throw to ensure transaction is handled by the caller
        }
    }

    // Method to get the next ID from the HistoricoChamado sequence (Receives connection)
    private static int GetNextHistoricoChamadoId(OracleConnection conn)
    {
        string query = "SELECT historicochamado_seq.NEXTVAL FROM dual"; // Assuming the sequence is named 'historicocamado_seq'
        using (var cmd = new OracleCommand(query, conn))
        {
            if (conn.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Banco de dados não está aberto.");
            }
            object result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                throw new Exception("Falha em obter o próximo ID da sequência do historicochamado.");
            }
        }
    }


    // Send a notification to a user - Receives connection AND transaction. NOW INTERNAL STATIC.
    internal static void EnviarNotificacao(int idUsuarioDestinatario, string mensagem, int idChamadoRelacionado, string tipo, OracleConnection conn, OracleTransaction transaction)
    {
        try
        {
            // The connection and transaction are received as parameter
            string sql = @"
                     INSERT INTO Notificacao (ID, Mensagem, DataEnvio, Lida, ID_UsuarioDestinatario, ID_ChamadoRelacionado, Tipo)
                     VALUES (:idNotificacao, :mensagem, :dataEnvio, :lida, :idUsuarioDestinatario, :idChamadoRelacionado, :tipo)";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Transaction = transaction; // Associate with transaction
                // IDs are NUMBER in DB -> Decimal in C#. Mensagem, Tipo are Varchar2. Data is Date. Lida is Varchar2.
                cmd.Parameters.Add(":idNotificacao", OracleDbType.Decimal).Value = GetNextNotificacaoId(conn); // Get ID from sequence (within transaction)
                cmd.Parameters.Add(":mensagem", OracleDbType.Varchar2).Value = mensagem;
                cmd.Parameters.Add(":dataEnvio", OracleDbType.Date).Value = DateTime.Now;
                cmd.Parameters.Add(":lida", OracleDbType.Varchar2).Value = "N"; // Notification not read by default
                cmd.Parameters.Add(":idUsuarioDestinatario", OracleDbType.Decimal).Value = idUsuarioDestinatario;
                cmd.Parameters.Add(":idChamadoRelacionado", OracleDbType.Decimal).Value = idChamadoRelacionado;
                cmd.Parameters.Add(":tipo", OracleDbType.Varchar2).Value = tipo;
                cmd.ExecuteNonQuery();
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao enviar notificação: " + ex.Message);
            throw; // Re-throw to ensure transaction is handled by the caller
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao enviar notificação: " + ex.Message);
            throw; // Re-throw to ensure transaction is handled by the caller
        }
    }

    // Method to get the next ID from the Notificacao sequence (Receives connection)
    private static int GetNextNotificacaoId(OracleConnection conn)
    {
        string query = "SELECT notificacao_seq.NEXTVAL FROM dual"; // Assuming the sequence is named 'notificacao_seq'
        using (var cmd = new OracleCommand(query, conn))
        {
            if (conn.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Database connection is not open.");
            }
            object result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                throw new Exception("Failed to get the next ID from the notificacao sequence.");
            }
        }
    }


    // Mark notifications as read for a user (Receives connection)
    private static void MarcarNotificacoesComoLidas(int idUsuario, OracleConnection conn)
    {
        try
        {
            // The connection is received as parameter
            string sql = "UPDATE Notificacao SET Lida = 'S' WHERE ID_UsuarioDestinatario = :idUsuario AND Lida = 'N'";
            using (var cmd = new OracleCommand(sql, conn))
            {
                // ID_UsuarioDestinatario is NUMBER in DB -> Decimal in C#
                cmd.Parameters.Add(":idUsuario", OracleDbType.Decimal).Value = idUsuario;
                cmd.ExecuteNonQuery();
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao marcar notificações como lidas: " + ex.Message);
            // Do not re-throw here, as it's a secondary operation after display
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao marcar notificações como lidas: " + ex.Message);
            // Do not re-throw here
        }
    }

    // Additional methods needed for Technician functionalities (implemented or adjusted to use sequence)

    // Method to get tickets assigned to a specific technician
    public static List<Chamado> GetAssignedChamados(int tecnicoId)
    {
        List<Chamado> chamados = new List<Chamado>();
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                // Order by urgency (CRITICO highest, then ALTO, etc.) and then by opening date
                string sql = @"
                    SELECT ID, Descricao, DataAbertura, DataFechamento, Status, NivelUrgencia, Categoria, TempoTotalAtendimento, ID_Solicitante, ID_TecnicoResponsavel, SolucaoAplicada
                    FROM Chamado
                    WHERE ID_TecnicoResponsavel = :idTecnico
                    ORDER BY
                        CASE NivelUrgencia
                            WHEN 'CRITICO' THEN 1
                            WHEN 'ALTO' THEN 2
                            WHEN 'MEDIO' THEN 3
                            WHEN 'BAIXO' THEN 4
                            ELSE 5
                        END,
                        DataAbertura ASC";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    // ID_TecnicoResponsavel is NUMBER in DB -> Decimal in C#
                    cmd.Parameters.Add(":idTecnico", OracleDbType.Decimal).Value = tecnicoId;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            chamados.Add(new Chamado
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                Descricao = reader["Descricao"].ToString(),
                                DataAbertura = Convert.ToDateTime(reader["DataAbertura"]),
                                DataFechamento = reader["DataFechamento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DataFechamento"]),
                                Status = (StatusChamado)Enum.Parse(typeof(StatusChamado), reader["Status"].ToString()),
                                NivelUrgencia = (NivelUrgencia)Enum.Parse(typeof(NivelUrgencia), reader["NivelUrgencia"].ToString()),
                                Categoria = reader["Categoria"].ToString(),
                                TempoTotalAtendimento = reader["TempoTotalAtendimento"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["TempoTotalAtendimento"]),
                                IdSolicitante = Convert.ToInt32(reader["ID_Solicitante"]),
                                IdTecnicoResponsavel = reader["ID_TecnicoResponsavel"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ID_TecnicoResponsavel"]),
                                SolucaoAplicada = reader["SolucaoAplicada"] == DBNull.Value ? null : reader["SolucaoAplicada"].ToString()
                            });
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter chamados atribuídos: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter chamados atribuídos: " + ex.Message);
        }
        return chamados;
    }

    // Method to get details of a specific ticket (used by technician and potentially others)
    public static Chamado GetChamadoDetails(int idChamado)
    {
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                string sqlChamado = @"
                        SELECT ID, Descricao, DataAbertura, DataFechamento, Status, NivelUrgencia, Categoria, TempoTotalAtendimento, ID_Solicitante, ID_TecnicoResponsavel, SolucaoAplicada
                        FROM Chamado
                        WHERE ID = :idChamado";

                using (var cmdChamado = new OracleCommand(sqlChamado, conn))
                {
                    // ID é NUMBER no BD -> Decimal em C#
                    cmdChamado.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                    using (var readerChamado = cmdChamado.ExecuteReader())
                    {
                        if (readerChamado.Read())
                        {
                            return new Chamado
                            {
                                Id = Convert.ToInt32(readerChamado["ID"]),
                                Descricao = readerChamado["Descricao"].ToString(),
                                DataAbertura = Convert.ToDateTime(readerChamado["DataAbertura"]),
                                DataFechamento = readerChamado["DataFechamento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(readerChamado["DataFechamento"]),
                                Status = (StatusChamado)Enum.Parse(typeof(StatusChamado), readerChamado["Status"].ToString()),
                                NivelUrgencia = (NivelUrgencia)Enum.Parse(typeof(NivelUrgencia), readerChamado["NivelUrgencia"].ToString()),
                                Categoria = readerChamado["Categoria"].ToString(),
                                TempoTotalAtendimento = readerChamado["TempoTotalAtendimento"] == DBNull.Value ? (double?)null : Convert.ToDouble(readerChamado["TempoTotalAtendimento"]),
                                IdSolicitante = Convert.ToInt32(readerChamado["ID_Solicitante"]),
                                IdTecnicoResponsavel = readerChamado["ID_TecnicoResponsavel"] == DBNull.Value ? (int?)null : Convert.ToInt32(readerChamado["ID_TecnicoResponsavel"]),
                                SolucaoAplicada = readerChamado["SolucaoAplicada"] == DBNull.Value ? null : readerChamado["SolucaoAplicada"].ToString()
                            };
                        }
                        else
                        {
                            // Console.WriteLine($"Chamado com ID {idChamado} não encontrado."); // Provide feedback even in static method - Let the caller handle UI feedback
                            return null;
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter detalhes do chamado: " + ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter detalhes do chamado: " + ex.Message);
            return null;
        }
    }


    public static bool AtualizarChamadoStatusSolucao(int idChamado, StatusChamado novoStatus, string solucao, DateTime? dataFechamento, int idUsuarioAtualizou)
    {
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                // Iniciar uma transação
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Update status, solution, closing date and calculate total time if closing/resolving
                        string sql = @"
                        UPDATE Chamado
                        SET Status = :novoStatus,
                        SolucaoAplicada = :solucao,
                        DataFechamento = :dataFechamento,
                        TempoTotalAtendimento = CASE WHEN :dataFechamentoCheck IS NOT NULL THEN 
                        (SELECT (SYSDATE - DataAbertura) * 24 * 60 FROM Chamado WHERE ID = :idChamadoSub) 
                        ELSE TempoTotalAtendimento 
                        END
                        WHERE ID = :idChamado";


                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction; // Associate with the transaction
                            // Add parameters with explicit Oracle type specification. ID is NUMBER in DB -> Decimal in C#
                            cmd.Parameters.Add(":novoStatus", OracleDbType.Varchar2).Value = novoStatus.ToString();
                            cmd.Parameters.Add(":solucao", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(solucao) ? DBNull.Value : (object)solucao;
                            cmd.Parameters.Add(":dataFechamento", OracleDbType.Date).Value = dataFechamento == null ? DBNull.Value : (object)dataFechamento;
                            cmd.Parameters.Add(":dataFechamentoCheck", OracleDbType.Date).Value = dataFechamento == null ? DBNull.Value : (object)dataFechamento;
                            // IDs and NUMBER columns in DB should be mapped to Decimal in C# for better precision
                            cmd.Parameters.Add(":idChamadoSub", OracleDbType.Decimal).Value = idChamado;
                            cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;


                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                // Add history of the update (Internal static method)
                                AdicionarHistoricoChamado(idChamado, $"Status atualizado para {novoStatus}. Solução: {(string.IsNullOrEmpty(solucao) ? "N/A" : solucao)}", idUsuarioAtualizou, conn, transaction); // Pass connection and transaction

                                // Send notification to the requester if status changed to RESOLVED or CLOSED
                                if (novoStatus == StatusChamado.RESOLVIDO || novoStatus == StatusChamado.FECHADO)
                                {
                                    int solicitanteId = GetIdSolicitanteChamado(idChamado, conn); // Get requester ID using the same connection
                                    if (solicitanteId > 0)
                                    {
                                        EnviarNotificacao(solicitanteId, $"Chamado #{idChamado} foi {novoStatus}.", idChamado, "RESOLUCAO", conn, transaction); // Pass connection and transaction
                                    }
                                }


                                transaction.Commit(); // Commit the transaction
                                return true;
                            }
                            else
                            {
                                transaction.Rollback(); // Rollback the transaction
                                // Console.WriteLine($"Chamado com ID {idChamado} não encontrado."); // Message if ticket doesn't exist - Let the caller handle UI feedback
                                return false;
                            }
                        }
                    }
                    catch (OracleException ex)
                    {
                        transaction.Rollback(); // Rollback the transaction in case of Oracle error
                        Console.WriteLine("Erro Oracle ao atualizar status/solução do chamado: " + ex.Message);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback the transaction in case of general error
                        Console.WriteLine("Erro inesperado ao atualizar status/solução do chamado: " + ex.Message);
                        return false;
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter conexão ou iniciar transação para atualizar status/solução: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter conexão ou iniciar transação para atualizar status/solução: " + ex.Message);
            return false;
        }
    }


    public static bool AdicionarRegistroTempo(int idChamado, int idTecnico, double tempoDedicado, string descricaoAtividade)
    {
        if (tempoDedicado <= 0)
        {
            Console.WriteLine("Erro de validação: O tempo dedicado deve ser maior que zero.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(descricaoAtividade))
        {
            Console.WriteLine("Erro de validação: A descrição da atividade não pode ser vazia.");
            return false;
        }
        // Check if technician and ticket exist BEFORE starting the transaction (Reuse method from Usuario.cs)
        if (!Usuario.UsuarioExiste(idTecnico))
        {
            Console.WriteLine($"Erro: Técnico com ID {idTecnico} não encontrado.");
            return false;
        }
        // Check if the ticket exists (Can be done before or within the transaction). We'll do it before.
        if (Chamado.GetChamadoDetails(idChamado) == null)
        {
            // Message is already displayed in the called method
            return false;
        }


        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                // Iniciar uma transação
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string sql = @"
                            INSERT INTO RegistroTempo (ID, ID_Chamado, ID_Tecnico, DataRegistro, TempoDedicado, DescricaoAtividade)
                            VALUES (:id, :idChamado, :idTecnico, :dataRegistro, :tempoDedicado, :descricaoAtividade)";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.Parameters.Add(":id", OracleDbType.Decimal).Value = ObterProximoRegistroTempoId(conn);
                            cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                            cmd.Parameters.Add(":idTecnico", OracleDbType.Decimal).Value = idTecnico;
                            cmd.Parameters.Add(":dataRegistro", OracleDbType.Date).Value = DateTime.Now;
                            cmd.Parameters.Add(":tempoDedicado", OracleDbType.Double).Value = tempoDedicado; // Using Double for time
                            cmd.Parameters.Add(":descricaoAtividade", OracleDbType.Varchar2).Value = descricaoAtividade;

                            int rowsInserted = cmd.ExecuteNonQuery();

                            if (rowsInserted > 0)
                            {
                                AtualizarChamadoTotalTime(idChamado, tempoDedicado, conn, transaction);
                                AdicionarHistoricoChamado(idChamado, $"Registrado {tempoDedicado:F2} minutos em '{descricaoAtividade}'.", idTecnico, conn, transaction); // Pass connection and transaction

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
                        transaction.Rollback(); // Rollback the transaction in case of Oracle error
                        Console.WriteLine("Erro Oracle ao adicionar registro de tempo: " + ex.Message);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback the transaction in case of general error
                        Console.WriteLine("Erro inesperado ao adicionar registro de tempo: " + ex.Message);
                        return false;
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter conexão ou iniciar transação para adicionar registro de tempo: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter conexão ou iniciar transação para adicionar registro de tempo: " + ex.Message);
            return false;
        }
    }

    private static int ObterProximoRegistroTempoId(OracleConnection conn)
    {
        string query = "SELECT registrotempo_seq.NEXTVAL FROM dual"; // Assuming the sequence is named 'registrotempo_seq'
        using (var cmd = new OracleCommand(query, conn))
        {
            object result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                throw new Exception("Não foi possível obter o próximo ID da sequence de registro de tempo.");
            }
        }
    }

    // Method to update the TempoTotalAtendimento in the Chamado table (used after adding time log) - Receives connection AND transaction
    private static void AtualizarChamadoTotalTime(int idChamado, double timeSpent, OracleConnection conn, OracleTransaction transaction)
    {
        try
        {
            // The connection and transaction are received as parameter
            string sql = @"
                     UPDATE Chamado
                     SET TempoTotalAtendimento = NVL(TempoTotalAtendimento, 0) + :timeSpent
                     WHERE ID = :idChamado";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Transaction = transaction; // Associate with the transaction
                // Time Dedicated is NUMBER -> Double/Decimal. ID is NUMBER -> Decimal.
                cmd.Parameters.Add(":timeSpent", OracleDbType.Double).Value = timeSpent; // Using Double
                cmd.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                cmd.ExecuteNonQuery();
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao atualizar tempo total do chamado: " + ex.Message);
            throw; // Re-throw to ensure transaction is handled by the caller
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao atualizar tempo total do chamado: " + ex.Message);
            throw; // Re-throw to ensure transaction is handled by the caller
        }
    }

    // Method to get history entries for a ticket (can be here or in a separate class)
    public static List<string> GetHistoricoChamado(int idChamado) // Static method
    {
        List<string> historico = new List<string>();
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                string sqlHistorico = @"
                    SELECT DataAtualizacao, DescricaoAtualizacao, ID_UsuarioAtualizou
                    FROM HistoricoChamado
                    WHERE ID_Chamado = :idChamado
                    ORDER BY DataAtualizacao ASC";

                using (var cmdHistorico = new OracleCommand(sqlHistorico, conn))
                {
                    // ID_Chamado is NUMBER in DB -> Decimal in C#
                    cmdHistorico.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;
                    using (var readerHistorico = cmdHistorico.ExecuteReader())
                    {
                        while (readerHistorico.Read())
                        {
                            historico.Add($"Data: {Convert.ToDateTime(readerHistorico["DataAtualizacao"]):yyyy-MM-dd HH:mm:ss} - Atualização: {readerHistorico["DescricaoAtualizacao"]} (Atualizado por Usuário ID: {readerHistorico["ID_UsuarioAtualizou"]})"); // Format date
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter histórico do chamado: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter histórico do chamado: " + ex.Message);
        }
        return historico;
    }

    public static List<string> BuscarSugestoesSolucao(string descricaoProblema, string categoria)
    {
        List<string> sugestoes = new List<string>();
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                string sql = @"
                    SELECT Titulo, Solucao
                    FROM BaseConhecimento
                    WHERE Categoria = :categoria
                    AND (:descricaoProblema LIKE '%' || Titulo || '%' OR :descricaoProblema LIKE '%' || Descricao || '%')
                    FETCH FIRST 3 ROWS ONLY"; // Fetch up to 3 suggestions

                using (var cmd = new OracleCommand(sql, conn))
                {
                    // Parameters are strings, Varchar2 is appropriate
                    cmd.Parameters.Add(":categoria", OracleDbType.Varchar2).Value = categoria;
                    cmd.Parameters.Add(":descricaoProblema", OracleDbType.Varchar2).Value = descricaoProblema;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string titulo = reader["Titulo"].ToString();
                            string solucao = reader["Solucao"].ToString();
                            sugestoes.Add($"Título: {titulo}\nSolução: {solucao.Substring(0, Math.Min(solucao.Length, 100))}..."); // Display portion of solution
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao buscar sugestões de solução: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao buscar sugestões de solução: " + ex.Message);
        }
        return sugestoes;
    }


    // Recebe o ID do chamado e o ID do usuário que solicitou a atribuição (neste caso, o usuário COMUM)
    public static bool DesignarTecnico(int idChamado, int idUsuarioSolicitou)
    {
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); // Abrir a conexão explicitamente

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Obter detalhes do chamado para saber a categoria
                        Chamado chamado = GetChamadoDetails(idChamado); // Reutiliza método existente

                        if (chamado == null)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        // Verificar se o chamado já está atribuído ou fechado/resolvido
                        if (chamado.IdTecnicoResponsavel.HasValue || chamado.Status == StatusChamado.RESOLVIDO || chamado.Status == StatusChamado.FECHADO)
                        {
                            Console.WriteLine($"O chamado {idChamado} já está atribuído a um técnico ou foi resolvido/fechado.");
                            transaction.Rollback();
                            return false;
                        }

                        string sqlFindTechnician = @"
                             SELECT DISTINCT U.ID, U.Nome
                             FROM Usuario U
                             LEFT JOIN Habilidade_Tecnico HT ON U.ID = HT.ID_Tecnico
                             LEFT JOIN Habilidade H ON HT.ID_Habilidade = H.ID
                             WHERE U.TipoDeUsuario = 'TECNICO'
                             AND (
                                -- Tenta encontrar técnicos com habilidades que correspondem à categoria
                                EXISTS (SELECT 1 FROM Habilidade WHERE ID = HT.ID_Habilidade AND UPPER(H.Nome) LIKE '%' || UPPER(:categoriaChamado) || '%')
                                -- OU considere todos os técnicos se a primeira busca não encontrar nada (para garantir atribuição)
                                OR NOT EXISTS (SELECT 1 FROM Habilidade_Tecnico WHERE ID_Tecnico IN (SELECT ID FROM Usuario WHERE TipoDeUsuario = 'TECNICO')) -- Se não houver NENHUM técnico com habilidade registrada
                             )
                             ORDER BY U.Nome";

                        int? idTecnicoAtribuir = null;
                        string nomeTecnicoAtribuir = "Não Encontrado";

                        using (var cmdFindTechnician = new OracleCommand(sqlFindTechnician, conn))
                        {
                            cmdFindTechnician.Transaction = transaction; // Associa com a transação
                            cmdFindTechnician.Parameters.Add(":categoriaChamado", OracleDbType.Varchar2).Value = chamado.Categoria; // Usa a categoria do chamado

                            using (var reader = cmdFindTechnician.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    idTecnicoAtribuir = Convert.ToInt32(reader["ID"]);
                                    nomeTecnicoAtribuir = reader["Nome"].ToString();
                                    Console.WriteLine($"Técnico encontrado para atribuição: {nomeTecnicoAtribuir} (ID: {idTecnicoAtribuir})");
                                    // Em um sistema real, aqui você escolheria o técnico mais disponível (menor carga de chamados abertos, etc.)
                                }
                                else
                                {
                                    Console.WriteLine("Nenhum técnico adequado encontrado para atribuir o chamado com base na categoria.");
                                    // Opcional: Atribuir a um grupo genérico ou deixar sem atribuição por enquanto.
                                    // Por enquanto, vamos retornar falso se nenhum técnico for encontrado.
                                    transaction.Rollback();
                                    return false;
                                }
                            }
                        }

                        // 3. Atribuir o chamado ao técnico encontrado e atualizar o status
                        string sqlUpdateChamado = @"
                             UPDATE Chamado
                             SET ID_TecnicoResponsavel = :idTecnico,
                                 Status = :novoStatus -- Pode mudar para EM_ANDAMENTO ou manter ABERTO, dependendo da regra
                             WHERE ID = :idChamado";

                        using (var cmdUpdateChamado = new OracleCommand(sqlUpdateChamado, conn))
                        {
                            cmdUpdateChamado.Transaction = transaction; // Associa com a transação
                            cmdUpdateChamado.Parameters.Add(":idTecnico", OracleDbType.Decimal).Value = idTecnicoAtribuir.Value; // Usa o ID do técnico encontrado
                            cmdUpdateChamado.Parameters.Add(":novoStatus", OracleDbType.Varchar2).Value = StatusChamado.EM_ANDAMENTO.ToString(); // Define como EM_ANDAMENTO
                            cmdUpdateChamado.Parameters.Add(":idChamado", OracleDbType.Decimal).Value = idChamado;

                            int rowsAffected = cmdUpdateChamado.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                // 4. Adicionar histórico e notificação
                                AdicionarHistoricoChamado(idChamado, $"Chamado atribuído ao técnico ID {idTecnicoAtribuir} ({nomeTecnicoAtribuir}) pelo usuário ID {idUsuarioSolicitou}.", idUsuarioSolicitou, conn, transaction);
                                EnviarNotificacao(idTecnicoAtribuir.Value, $"Um chamado foi atribuído a você: #{idChamado} ({chamado.Descricao}).", idChamado, "ATRIBUICAO", conn, transaction);
                                // Opcional: Notificar o solicitante que o chamado foi atribuído a um técnico.
                                EnviarNotificacao(chamado.IdSolicitante, $"Seu chamado #{idChamado} foi atribuído a um técnico e está agora {StatusChamado.EM_ANDAMENTO}.", idChamado, "ATUALIZACAO", conn, transaction);


                                transaction.Commit(); // Confirma a transação
                                return true;
                            }
                            else
                            {
                                transaction.Rollback(); // Reverte a transação
                                Console.WriteLine($"Erro interno ao atualizar o chamado {idChamado} com o técnico atribuído.");
                                return false;
                            }
                        }
                    }
                    catch (OracleException ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("Erro Oracle durante a atribuição do técnico ao chamado: " + ex.Message);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("Erro inesperado durante a atribuição do técnico ao chamado: " + ex.Message);
                        return false;
                    }
                } 
            } 
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter conexão para atribuição do técnico: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter conexão para atribuição do técnico: " + ex.Message);
            return false;
        }
    }

}