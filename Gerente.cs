using System;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Globalization;

public class Gerente
{
    private Usuario _usuario;

    public Gerente(Usuario usuario)
    {
        _usuario = usuario;
    }

    public void Menu()
    {
        bool running = true;
        while (running)
        {
            Console.Clear();
            Console.WriteLine($"==== MENU GERENTE - Bem-vindo, {_usuario.Nome} ====");
            Console.WriteLine("1. Registrar Chamado");
            Console.WriteLine("2. Acompanhar Status de Chamado");
            Console.WriteLine("3. Avaliar Atendimento");
            Console.WriteLine("4. Receber Notificações");
            Console.WriteLine("5. Verificar Solução da IA");
            Console.WriteLine("6. Visualizar Relatórios e Indicadores");
            Console.WriteLine("7. Acompanhar Produtividade da Equipe");
            Console.WriteLine("8. Configurar Níveis de Escalonamento e SLA's");
            Console.WriteLine("9. Receber Notificações Críticas/Atrasadas");
            Console.WriteLine("10. Sair");
            Console.Write("Escolha uma opção: ");

            string opcao = Console.ReadLine();

            switch (opcao)
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
                    VisualizarRelatoriosIndicadores();
                    break;
                case "7":
                    AcompanharProdutividadeEquipe();
                    break;
                case "8":
                    ConfigurarEscalonamentoSLA();
                    break;
                case "9":
                    ReceberNotificacoesCriticasAtrasadas();
                    break;
                case "10":
                    running = false;
                    Console.WriteLine("Saindo do menu do gerente...");
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

    private void VerificarSolucaoIA()
    {
        Console.Clear();
        Console.WriteLine("==== VERIFICAR SOLUÇÃO DA IA (Gerente) ====");
        Console.Write("Digite o ID do chamado para verificar a sugestão da IA: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            Chamado chamado = Chamado.GetChamadoDetails(idChamado);

            if (chamado == null)
            {
                Console.WriteLine($"Chamado com ID {idChamado} não encontrado.");
            }
            else
            {
                Console.WriteLine("\n--- Sugestão da IA para o Chamado ---");
                if (!string.IsNullOrWhiteSpace(chamado.SolucaoAplicada)) // SolucaoAplicada é onde a IA armazena a sugestão inicial
                {
                    Console.WriteLine($"ID do Chamado: {chamado.Id}");
                    Console.WriteLine($"Descrição do Problema: {chamado.Descricao}");
                    Console.WriteLine($"Categoria: {chamado.Categoria}");
                    Console.WriteLine($"---");
                    Console.WriteLine($"Sugestão da IA: {chamado.SolucaoAplicada}");
                    Console.WriteLine($"---");
                    // Adiciona a busca por outras soluções na base de conhecimento
                    Console.WriteLine("\n--- Outras Soluções da Base de Conhecimento (se houver) ---");
                    List<string> sugestoesAdicionais = Chamado.BuscarSugestoesSolucao(chamado.Descricao, chamado.Categoria);
                    if (sugestoesAdicionais.Any() && sugestoesAdicionais.All(s => s != chamado.SolucaoAplicada)) // Evita repetir a sugestão principal
                    {
                        foreach (var sugestao in sugestoesAdicionais)
                        {
                            if (sugestao != $"Título: {chamado.Descricao}\nSolução: {chamado.SolucaoAplicada.Substring(0, Math.Min(chamado.SolucaoAplicada.Length, 100))}...") // Uma verificação mais robusta pode ser necessária
                            {
                                Console.WriteLine(sugestao);
                                Console.WriteLine("---");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Nenhuma outra sugestão encontrada na base de conhecimento para este chamado.");
                    }
                }
                else
                {
                    Console.WriteLine("Nenhuma solução inicial foi sugerida pela IA para este chamado ou o campo está vazio.");
                    // Mesmo sem sugestão inicial da IA, busca na base de conhecimento
                    Console.WriteLine("\n--- Buscando Soluções na Base de Conhecimento ---");
                    List<string> sugestoesBase = Chamado.BuscarSugestoesSolucao(chamado.Descricao, chamado.Categoria);
                    if (sugestoesBase.Any())
                    {
                        foreach (var sugestao in sugestoesBase)
                        {
                            Console.WriteLine(sugestao);
                            Console.WriteLine("---");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Nenhuma sugestão encontrada na base de conhecimento para este chamado.");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("ID de chamado inválido.");
        }
        Console.WriteLine("\nPressione Enter para continuar.");
        Console.ReadLine();
    }

    private void RegistrarChamado()
    {
        Console.Clear();
        Console.WriteLine("==== REGISTRAR CHAMADO (Gerente) ====");
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
        bool sucesso = Chamado.RegistrarChamado(descricao, categoria, _usuario.Id, out newChamadoId);

        Console.WriteLine(sucesso ? $"Chamado registrado com sucesso! ID: {newChamadoId}" : "Erro ao registrar chamado.");
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void AcompanharStatusChamado()
    {
        Console.Clear();
        Console.WriteLine("==== ACOMPANHAR STATUS DE CHAMADO (Gerente) ====");
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
        Console.WriteLine("==== AVALIAR ATENDIMENTO (Gerente) ====");
        Console.Write("Digite o ID do chamado para avaliar: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            Console.Write("Digite a nota (0-10): ");
            if (int.TryParse(Console.ReadLine(), out int notaAvaliacao))
            {
                if (notaAvaliacao < 0 || notaAvaliacao > 10)
                {
                    Console.WriteLine("A nota deve ser entre 0 e 10.");
                    Console.WriteLine("Pressione Enter para continuar.");
                    Console.ReadLine();
                    return;
                }

                Console.Write("Digite um comentário: ");
                string comentarioAvaliacao = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(comentarioAvaliacao))
                {
                    Console.WriteLine("O comentário da avaliação não pode ser vazio.");
                }

                Chamado.AvaliarAtendimento(idChamado, notaAvaliacao, comentarioAvaliacao, _usuario.Id);

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
        Console.WriteLine("==== NOTIFICAÇÕES (Gerente - Chamados Solicitados) ====");
        Chamado.ReceberNotificacoes(_usuario.Id);
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }


    private void VisualizarRelatoriosIndicadores()
    {
        Console.Clear();
        Console.WriteLine("==== VISUALIZAR RELATÓRIOS E INDICADORES ====");

        Console.WriteLine("Escolha o tipo de relatório:");
        Console.WriteLine("1. Chamados por Categoria");
        Console.WriteLine("2. Tempo Médio de Atendimento");
        Console.WriteLine("3. Chamados por Técnico");
        Console.WriteLine("4. Voltar");
        Console.Write("Opção: ");

        string opcaoRelatorio = Console.ReadLine();

        switch (opcaoRelatorio)
        {
            case "1":
                RelatorioChamadosPorCategoria();
                break;
            case "2":
                RelatorioTempoMedioAtendimento();
                break;
            case "3":
                RelatorioChamadosPorTecnico();
                break;
            case "4":
                return;
            default:
                Console.WriteLine("Opção inválida.");
                break;
        }

        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void RelatorioChamadosPorCategoria()
    {
        Console.WriteLine("\n--- Relatório: Chamados por Categoria ---");
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open(); 

                string sql = @"
                    SELECT Categoria, COUNT(*) AS TotalChamados,
                           AVG(DataFechamento - DataAbertura) * 24 * 60 AS TempoMedioMinutos -- Difference of dates in minutes
                    FROM Chamado
                    WHERE Status = 'FECHADO' OR Status = 'RESOLVIDO'
                    GROUP BY Categoria
                    ORDER BY COUNT(*) DESC";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("Nenhum dado de chamado fechado para este relatório.");
                        }
                        else
                        {
                            Console.WriteLine("--------------------------------------------------------------------------------------");
                            Console.WriteLine($"| {"Categoria",-20} | {"Total de Chamados",-15} | {"Tempo Médio (min)",-18} |");
                            Console.WriteLine("--------------------------------------------------------------------------------------");
                            while (reader.Read())
                            {
                                string categoria = reader["Categoria"].ToString();
                                int totalChamados = Convert.ToInt32(reader["TotalChamados"]);
                                object tempoMedioObj = reader["TempoMedioMinutos"];
                                double tempoMedioMinutos = tempoMedioObj != DBNull.Value ? Convert.ToDouble(tempoMedioObj) : 0;

                                Console.WriteLine($"| {categoria,-20} | {totalChamados,-15} | {tempoMedioMinutos:F2},-18 |");
                            }
                            Console.WriteLine("-------------------------------------------------------------------------------------");
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao gerar relatório de chamados por categoria: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao gerar relatório de chamados por categoria: " + ex.Message);
        }
    }

    private void RelatorioTempoMedioAtendimento()
    {
        Console.WriteLine("\n--- Relatório: Tempo Médio Geral de Atendimento ---");
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT AVG(DataFechamento - DataAbertura) * 24 * 60 AS TempoMedioGeralMinutos
                    FROM Chamado
                    WHERE Status = 'FECHADO' OR Status = 'RESOLVIDO'";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    object tempoMedioObj = cmd.ExecuteScalar();

                    if (tempoMedioObj != DBNull.Value && tempoMedioObj != null)
                    {
                        double tempoMedioMinutos = Convert.ToDouble(tempoMedioObj);
                        Console.WriteLine($"Tempo Médio Geral de Atendimento (Chamados Fechados/Resolvidos): {tempoMedioMinutos:F2} minutos");
                    }
                    else
                    {
                        Console.WriteLine("Dados insuficientes de chamados fechados/resolvidos para calcular o tempo médio.");
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao gerar relatório de tempo médio: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao gerar relatório de tempo médio: " + ex.Message);
        }
    }

    private void RelatorioChamadosPorTecnico()
    {
        Console.WriteLine("\n--- Relatório: Chamados por Técnico ---");
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT U.Nome AS NomeTecnico, COUNT(C.ID) AS TotalChamadosAtribuidos,
                           SUM(RT.TempoDedicado) AS TempoTotalDedicadoMinutos
                    FROM Usuario U
                    LEFT JOIN Chamado C ON U.ID = C.ID_TecnicoResponsavel
                    LEFT JOIN RegistroTempo RT ON C.ID = RT.ID_Chamado AND U.ID = RT.ID_Tecnico
                    WHERE U.TipoDeUsuario = 'TECNICO'
                    GROUP BY U.Nome
                    ORDER BY COUNT(C.ID) DESC";


                using (var cmd = new OracleCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("Nenhum técnico encontrado ou nenhum chamado atribuído/tempo registrado.");
                        }
                        else
                        {
                            Console.WriteLine("----------------------------------------------------------------------------------------------");
                            Console.WriteLine($"| {"Técnico",-25} | {"Chamados Atribuídos",-20} | {"Tempo Total Dedicado (min)",-25} |");
                            Console.WriteLine("----------------------------------------------------------------------------------------------");
                            while (reader.Read())
                            {
                                string nomeTecnico = reader["NomeTecnico"].ToString();
                                int totalChamados = Convert.ToInt32(reader["TotalChamadosAtribuidos"]);
                                object tempoDedicadoObj = reader["TempoTotalDedicadoMinutos"];
                                double tempoTotalDedicado = tempoDedicadoObj != DBNull.Value ? Convert.ToDouble(tempoDedicadoObj) : 0;

                                Console.WriteLine($"| {nomeTecnico,-25} | {totalChamados,-20} | {tempoTotalDedicado:F2},-25 |");
                            }
                            Console.WriteLine("----------------------------------------------------------------------------------------------");
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao gerar relatório de chamados por técnico: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao gerar relatório de chamados por técnico: " + ex.Message);
        }
    }


    private void AcompanharProdutividadeEquipe()
    {
        Console.Clear();
        Console.WriteLine("==== ACOMPANHAR PRODUTIVIDADE DA EQUIPE ====");
        Console.WriteLine("Funcionalidade similar ao relatório 'Chamados por Técnico', focando no desempenho individual.");
        RelatorioChamadosPorTecnico();

        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void ConfigurarEscalonamentoSLA()
    {
        Console.Clear();
        Console.WriteLine("==== CONFIGURAR ESCALONAMENTO E SLA's ====");

        Console.WriteLine("Escolha a configuração:");
        Console.WriteLine("1. Visualizar SLA's existentes");
        Console.WriteLine("2. Adicionar/Atualizar SLA");
        Console.WriteLine("3. Remover SLA");
        Console.WriteLine("4. Voltar");
        Console.Write("Opção: ");
        string opcaoConfig = Console.ReadLine();

        switch (opcaoConfig)
        {
            case "1":
                VisualizarSLAs();
                break;
            case "2":
                AdicionarAtualizarSLA();
                break;
            case "3":
                RemoverSLA();
                break;
            case "4":
                return;
            default:
                Console.WriteLine("Opção inválida.");
                break;
        }
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void VisualizarSLAs()
    {
        Console.WriteLine("\n--- SLA's Existentes ---");
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string sql = "SELECT CategoriaChamado, NivelUrgencia, TempoMaximoResolucaoHoras FROM SLA ORDER BY CategoriaChamado, NivelUrgencia";
                using (var cmd = new OracleCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("Nenhum SLA configurado.");
                        }
                        else
                        {
                            Console.WriteLine("---------------------------------------------------------------------");
                            Console.WriteLine($"| {"Categoria",-25} | {"Nível Urgência",-15} | {"Tempo Máx. (Horas)",-20} |");
                            Console.WriteLine("---------------------------------------------------------------------");
                            while (reader.Read())
                            {
                                Console.WriteLine($"| {reader["CategoriaChamado"],-25} | {reader["NivelUrgencia"],-15} | {reader["TempoMaximoResolucaoHoras"],-20} |");
                            }
                            Console.WriteLine("---------------------------------------------------------------------");
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao visualizar SLAs: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao visualizar SLAs: " + ex.Message);
        }
    }

    private void AdicionarAtualizarSLA()
    {
        Console.Write("Digite a Categoria do Chamado para o SLA: ");
        string categoria = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(categoria))
        {
            Console.WriteLine("Categoria não pode ser vazia."); return;
        }

        Console.Write("Digite o Nível de Urgência (BAIXO, MEDIO, ALTO, CRITICO): ");
        string urgenciaInput = Console.ReadLine().ToUpper();
        if (!Enum.TryParse(urgenciaInput, out NivelUrgencia urgencia))
        {
            Console.WriteLine("Nível de urgência inválido."); return;
        }

        Console.Write("Digite o Tempo Máximo de Resolução (em horas): ");
        if (!int.TryParse(Console.ReadLine(), out int tempoMaximo) || tempoMaximo <= 0)
        {
            Console.WriteLine("Tempo máximo inválido."); return;
        }

        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string checkSql = "SELECT COUNT(*) FROM SLA WHERE CategoriaChamado = :categoria AND NivelUrgencia = :urgencia";
                bool exists = false;
                using (var checkCmd = new OracleCommand(checkSql, conn))
                {
                    checkCmd.Parameters.Add(":categoria", OracleDbType.Varchar2).Value = categoria;
                    checkCmd.Parameters.Add(":urgencia", OracleDbType.Varchar2).Value = urgencia.ToString();
                    exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                }

                string sql;
                if (exists)
                {
                    sql = "UPDATE SLA SET TempoMaximoResolucaoHoras = :tempoMaximo WHERE CategoriaChamado = :categoria AND NivelUrgencia = :urgencia";
                    Console.WriteLine("Atualizando SLA existente...");
                }
                else
                {
                    sql = "INSERT INTO SLA (CategoriaChamado, NivelUrgencia, TempoMaximoResolucaoHoras) VALUES (:categoria, :urgencia, :tempoMaximo)";
                    Console.WriteLine("Adicionando novo SLA...");
                }

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(":categoria", OracleDbType.Varchar2).Value = categoria;
                    cmd.Parameters.Add(":urgencia", OracleDbType.Varchar2).Value = urgencia.ToString();
                    cmd.Parameters.Add(":tempoMaximo", OracleDbType.Int32).Value = tempoMaximo;
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine(rowsAffected > 0 ? "SLA salvo com sucesso!" : "Falha ao salvar SLA.");
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao salvar SLA: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao salvar SLA: " + ex.Message);
        }
    }

    private void RemoverSLA()
    {
        Console.Write("Digite a Categoria do Chamado do SLA a ser removido: ");
        string categoria = Console.ReadLine();
        Console.Write("Digite o Nível de Urgência do SLA a ser removido (BAIXO, MEDIO, ALTO, CRITICO): ");
        string urgenciaInput = Console.ReadLine().ToUpper();

        if (string.IsNullOrWhiteSpace(categoria) || !Enum.TryParse(urgenciaInput, out NivelUrgencia urgencia))
        {
            Console.WriteLine("Dados inválidos para remover SLA.");
            return;
        }

        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM SLA WHERE CategoriaChamado = :categoria AND NivelUrgencia = :urgencia";
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(":categoria", OracleDbType.Varchar2).Value = categoria;
                    cmd.Parameters.Add(":urgencia", OracleDbType.Varchar2).Value = urgencia.ToString();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine(rowsAffected > 0 ? "SLA removido com sucesso!" : "SLA não encontrado ou falha ao remover.");
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao remover SLA: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao remover SLA: " + ex.Message);
        }
    }


    private void ReceberNotificacoesCriticasAtrasadas()
    {
        Console.Clear();
        Console.WriteLine("==== NOTIFICAÇÕES CRÍTICAS E CHAMADOS ATRASADOS ====");

        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();

                // Consulta para chamados críticos (Urgência CRITICO ou ALTO) que estão ABERTOS ou EM_ANDAMENTO
                string sqlCriticos = @"
                    SELECT C.ID, C.Descricao, C.Status, C.NivelUrgencia, C.DataAbertura, U.Nome AS Solicitante
                    FROM Chamado C
                    JOIN Usuario U ON C.ID_Solicitante = U.ID
                    WHERE (C.NivelUrgencia = 'CRITICO' OR C.NivelUrgencia = 'ALTO')
                      AND (C.Status = 'ABERTO' OR C.Status = 'EM_ANDAMENTO')
                    ORDER BY C.DataAbertura DESC";

                Console.WriteLine("\n--- Chamados Críticos (ALTO/CRITICO) Atuais ---");
                using (var cmdCriticos = new OracleCommand(sqlCriticos, conn))
                {
                    using (var reader = cmdCriticos.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("Nenhum chamado crítico (ALTO/CRITICO) em aberto ou em andamento.");
                        }
                        else
                        {
                            Console.WriteLine($"| {"ID",-5} | {"Urgência",-10} | {"Status",-15} | {"Abertura",-20} | {"Solicitante",-15} | {"Descrição",-30} |");
                            Console.WriteLine(new string('-', 100));
                            while (reader.Read())
                            {
                                Console.WriteLine($"| {reader["ID"],-5} | {reader["NivelUrgencia"],-10} | {reader["Status"],-15} | {Convert.ToDateTime(reader["DataAbertura"]).ToString("yyyy-MM-dd HH:mm"),-20} | {reader["Solicitante"],-15} | {reader["Descricao"].ToString().Substring(0, Math.Min(reader["Descricao"].ToString().Length, 30)),-30} |");
                            }
                            Console.WriteLine(new string('-', 100));
                        }
                    }
                }

                // Consulta para chamados atrasados (DataAbertura + TempoMaximoResolucaoHoras < SYSDATE)
                // Esta consulta é mais complexa e depende da estrutura do SLA
                string sqlAtrasados = @"
                    SELECT C.ID, C.Descricao, C.Status, C.NivelUrgencia, C.DataAbertura, S.TempoMaximoResolucaoHoras, U.Nome AS Solicitante,
                           (C.DataAbertura + NUMTODSINTERVAL(S.TempoMaximoResolucaoHoras, 'HOUR')) AS PrazoFinal
                    FROM Chamado C
                    JOIN SLA S ON C.Categoria = S.CategoriaChamado AND C.NivelUrgencia = S.NivelUrgencia
                    JOIN Usuario U ON C.ID_Solicitante = U.ID
                    WHERE (C.Status = 'ABERTO' OR C.Status = 'EM_ANDAMENTO')
                      AND (C.DataAbertura + NUMTODSINTERVAL(S.TempoMaximoResolucaoHoras, 'HOUR')) < SYSDATE
                    ORDER BY PrazoFinal ASC";

                Console.WriteLine("\n--- Chamados Atrasados (Fora do SLA) ---");
                using (var cmdAtrasados = new OracleCommand(sqlAtrasados, conn))
                {
                    using (var reader = cmdAtrasados.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("Nenhum chamado atualmente atrasado (fora do SLA).");
                        }
                        else
                        {
                            Console.WriteLine($"| {"ID",-5} | {"Urgência",-10} | {"Status",-15} | {"Abertura",-20} | {"Prazo Final",-20} | {"Solicitante",-15} | {"Descrição",-30} |");
                            Console.WriteLine(new string('-', 120));
                            while (reader.Read())
                            {
                                Console.WriteLine($"| {reader["ID"],-5} | {reader["NivelUrgencia"],-10} | {reader["Status"],-15} | {Convert.ToDateTime(reader["DataAbertura"]).ToString("yyyy-MM-dd HH:mm"),-20} | {Convert.ToDateTime(reader["PrazoFinal"]).ToString("yyyy-MM-dd HH:mm"),-20} | {reader["Solicitante"],-15} | {reader["Descricao"].ToString().Substring(0, Math.Min(reader["Descricao"].ToString().Length, 30)),-30} |");
                            }
                            Console.WriteLine(new string('-', 120));
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao buscar notificações críticas/atrasadas: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao buscar notificações críticas/atrasadas: " + ex.Message);
        }

        Console.WriteLine("\nPressione Enter para continuar.");
        Console.ReadLine();
    }
}

