using System;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Globalization;
using System.Data;

public class Rh
{
    private Usuario _usuario;

    public Rh(Usuario usuario)
    {
        _usuario = usuario;
    }

    public void Menu()
    {
        bool running = true;
        while (running)
        {
            Console.Clear();
            Console.WriteLine($"==== MENU RH - Bem-vindo, {_usuario.Nome} ====");
            Console.WriteLine("1. Registrar Chamado");
            Console.WriteLine("2. Acompanhar Status de Chamado");
            Console.WriteLine("3. Avaliar Atendimento");
            Console.WriteLine("4. Receber Notificações");
            Console.WriteLine("5. Verificar Solução da IA");
            Console.WriteLine("6. Gerar Relatórios para Auditoria e Banco de Horas");
            Console.WriteLine("7. (Funcionalidade de Integração com Folha de Pagamento - Representada por Relatórios)");
            Console.WriteLine("8. Sair");
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
                    GerarRelatoriosRH();
                    break;
                case "7":
                    Console.WriteLine("A integração direta com sistemas de folha de pagamento é complexa e geralmente feita via APIs ou arquivos.");
                    Console.WriteLine("A funcionalidade 'Gerar Relatórios para Auditoria e Banco de Horas' (Opção 6) representa a extração de dados para este fim.");
                    Console.WriteLine("Pressione Enter para continuar.");
                    Console.ReadLine();
                    break;
                case "8":
                    running = false;
                    Console.WriteLine("Saindo do menu de RH...");
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
        Console.WriteLine("==== VERIFICAR SOLUÇÃO DA IA (RH) ====");
        Console.Write("Digite o ID do chamado que você registrou: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            // Obter detalhes do chamado
            Chamado chamado = Chamado.GetChamadoDetails(idChamado);

            // Verificar se o chamado existe e se pertence ao usuário logado (RH neste caso)
            if (chamado == null)
            {
                Console.WriteLine($"Chamado com ID {idChamado} não encontrado.");
            }
            // Assegura que o usuário de RH só pode verificar soluções de chamados que ele mesmo abriu.
            else if (chamado.IdSolicitante != _usuario.Id)
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
                if (!string.IsNullOrWhiteSpace(chamado.SolucaoAplicada)) // A IA preenche SolucaoAplicada ao criar o chamado
                {
                    Console.WriteLine($"Solução: {chamado.SolucaoAplicada}");
                    Console.WriteLine("\nEsta solução resolveu o seu problema? (S/N)");
                    string resposta = Console.ReadLine().Trim().ToUpper();

                    if (resposta == "S")
                    {
                        // Usuário aceitou a solução, fechar o chamado
                        Console.WriteLine("Você aceitou a solução. Fechando o chamado...");
                        // O ID do usuário logado (_usuario.Id) é passado como quem está atualizando/fechando o chamado.
                        bool sucessoFechamento = Chamado.AtualizarChamadoStatusSolucao(chamado.Id, StatusChamado.RESOLVIDO, chamado.SolucaoAplicada, DateTime.Now, _usuario.Id);
                        Console.WriteLine(sucessoFechamento ? "Chamado fechado com sucesso como RESOLVIDO!" : "Erro ao fechar o chamado.");
                    }
                    else if (resposta == "N")
                    {
                        // Usuário não aceitou a solução, encaminhar para um técnico
                        Console.WriteLine("Você não aceitou a solução. Encaminhando para um técnico...");
                        // O ID do usuário logado (_usuario.Id) é passado como quem está solicitando a designação.
                        bool sucessoAtribuicao = Chamado.DesignarTecnico(chamado.Id, _usuario.Id);
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

    private void RegistrarChamado()
    {
        Console.Clear();
        Console.WriteLine("==== REGISTRAR CHAMADO (RH) ====");

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
        Console.WriteLine("==== ACOMPANHAR STATUS DE CHAMADO (RH) ====");
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
        Console.WriteLine("==== AVALIAR ATENDIMENTO (RH) ====");
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
        Console.WriteLine("==== NOTIFICAÇÕES (RH - Chamados Solicitados) ====");
        Chamado.ReceberNotificacoes(_usuario.Id);
        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }


    private void GerarRelatoriosRH()
    {
        Console.Clear();
        Console.WriteLine("==== GERAR RELATÓRIOS DE RH ====");

        Console.WriteLine("Escolha o tipo de relatório de RH:");
        Console.WriteLine("1. Horas Dedicadas por Técnico (para Folha de Pagamento/Banco de Horas)");
        Console.WriteLine("2. Relatório Geral de Desempenho de Atendimento");
        Console.WriteLine("3. Voltar");
        Console.Write("Opção: ");

        string opcaoRelatorio = Console.ReadLine();

        switch (opcaoRelatorio)
        {
            case "1":
                RelatorioHorasPorTecnico();
                break;
            case "2":
                RelatorioDesempenhoGeral();
                break;
            case "3":
                return;
            default:
                Console.WriteLine("Opção inválida.");
                break;
        }

        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void RelatorioHorasPorTecnico()
    {
        Console.WriteLine("\n--- Relatório: Horas Dedicadas por Técnico ---");
        Console.Write("Digite a Data de Início (dd/mm/yyyy) (deixe em branco para todos): ");
        string dataInicialInput = Console.ReadLine();
        DateTime? dataInicial = null;
        if (!string.IsNullOrEmpty(dataInicialInput))
        {
            if (!DateTime.TryParseExact(dataInicialInput, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateInitial))
            {
                Console.WriteLine("Formato de data de início inválido. Use dd/mm/yyyy. Gerando relatório para todos os períodos.");
            }
            else
            {
                dataInicial = parsedDateInitial;
            }
        }


        Console.Write("Digite a Data de Fim (dd/mm/yyyy) (deixe em branco para todos): ");
        string dataFinalInput = Console.ReadLine();
        DateTime? dataFinal = null;
        if (!string.IsNullOrEmpty(dataFinalInput))
        {
            if (!DateTime.TryParseExact(dataFinalInput, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateFinal))
            {
                Console.WriteLine("Formato de data de fim inválido. Use dd/mm/yyyy. Gerando relatório para todos os períodos.");
            }
            else
            {
                dataFinal = parsedDateFinal;
            }
        }


        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT U.Nome AS NomeTecnico, SUM(RT.TempoDedicado) AS HorasTotaisDedicadasMinutos
                    FROM Usuario U
                    JOIN RegistroTempo RT ON U.ID = RT.ID_Tecnico
                    WHERE U.TipoDeUsuario = 'TECNICO'";

                if (dataInicial.HasValue)
                {
                    sql += " AND RT.DataRegistro >= :dataInicial";
                }
                if (dataFinal.HasValue)
                {
                    sql += " AND RT.DataRegistro <= :dataFinal";
                }

                sql += " GROUP BY U.Nome ORDER BY U.Nome";


                using (var cmd = new OracleCommand(sql, conn))
                {
                    if (dataInicial.HasValue)
                    {
                        cmd.Parameters.Add(":dataInicial", dataInicial.Value);
                    }
                    if (dataFinal.HasValue)
                    {
                        cmd.Parameters.Add(":dataFinal", dataFinal.Value.Date.AddDays(1).AddSeconds(-1));
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("Nenhum registro de tempo encontrado para o período e técnicos.");
                        }
                        else
                        {
                            Console.WriteLine("--------------------------------------------------------------------");
                            Console.WriteLine($"| {"Técnico",-25} | {"Horas Dedicadas (min)",-20} |");
                            Console.WriteLine("-------------------------------------------------------------------");
                            while (reader.Read())
                            {
                                string nomeTecnico = reader["NomeTecnico"].ToString();
                                object horasDedicadasObj = reader["HorasTotaisDedicadasMinutos"];
                                double horasDedicadasMinutos = horasDedicadasObj != DBNull.Value ? Convert.ToDouble(horasDedicadasObj) : 0;

                                Console.WriteLine($"| {nomeTecnico,-25} | {horasDedicadasMinutos:F2},-20 |");
                            }
                            Console.WriteLine("-------------------------------------------------------------------");
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao gerar relatório de horas por técnico: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao gerar relatório de horas por técnico: " + ex.Message);
        }
    }

    private void RelatorioDesempenhoGeral()
    {
        Console.WriteLine("\n--- Relatório: Desempenho Geral de Atendimento ---");

        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT COUNT(*) AS TotalChamadosFechadosResolvidos,
                           AVG(DataFechamento - DataAbertura) * 24 * 60 AS TempoMedioGeralMinutos,
                           COUNT(CASE WHEN Status = 'RESOLVIDO' THEN 1 END) AS TotalResolvidos, -- Count resolved tickets
                           COUNT(CASE WHEN Status = 'FECHADO' THEN 1 END) AS TotalFechados -- Count closed tickets (can be different from resolved depending on flow)
                    FROM Chamado
                    WHERE Status = 'FECHADO' OR Status = 'RESOLVIDO'";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int totalChamados = Convert.ToInt32(reader["TotalChamadosFechadosResolvidos"]);
                            object tempoMedioObj = reader["TempoMedioGeralMinutos"];
                            double tempoMedioMinutos = tempoMedioObj != DBNull.Value ? Convert.ToDouble(tempoMedioObj) : 0;
                            int totalResolvidos = reader["TotalResolvidos"] != DBNull.Value ? Convert.ToInt32(reader["TotalResolvidos"]) : 0; // Added handling for DBNull
                            int totalFechados = reader["TotalFechados"] != DBNull.Value ? Convert.ToInt32(reader["TotalFechados"]) : 0; // Added handling for DBNull


                            Console.WriteLine($"Total de Chamados Fechados/Resolvidos: {totalChamados}");
                            Console.WriteLine($"Total de Chamados Resolvidos: {totalResolvidos}");
                            Console.WriteLine($"Total de Chamados Fechados: {totalFechados}");
                            Console.WriteLine($"Tempo Médio Geral de Atendimento (Fechados/Resolvidos): {tempoMedioMinutos:F2} minutos");

                            double taxaResolucao = totalChamados > 0 ? (double)totalResolvidos / totalChamados * 100 : 0;
                            Console.WriteLine($"Taxa de Resolução: {taxaResolucao:F2}%");

                        }
                        else
                        {
                            Console.WriteLine("Nenhum dado de chamado fechado/resolvido para este relatório.");
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao gerar relatório de desempenho geral: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao gerar relatório de desempenho geral: " + ex.Message);
        }
    }

}

