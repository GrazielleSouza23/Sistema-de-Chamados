using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using System.Data;

public class Tecnico
{
    private Usuario _usuario;

    public Tecnico(Usuario usuario)
    {
        _usuario = usuario;
    }

    public void Menu()
    {
        bool running = true;
        while (running)
        {
            Console.Clear();
            Console.WriteLine($"==== MENU TÉCNICO - Bem-vindo, {_usuario.Nome} ====");
            Console.WriteLine("1. Registrar Chamado");
            Console.WriteLine("2. Acompanhar Status de Chamado");
            Console.WriteLine("3. Avaliar Atendimento");
            Console.WriteLine("4. Receber Notificações");
            Console.WriteLine("5. Verificar Solução da IA");
            Console.WriteLine("6. Visualizar Chamados Atribuídos (Priorizados)");
            Console.WriteLine("7. Acessar Histórico e Detalhes do Chamado");
            Console.WriteLine("8. Registrar Ações e Tempo Gasto");
            Console.WriteLine("9. Atualizar Status e Fechar Chamado");
            Console.WriteLine("10. Sair");
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
                    VisualizarChamadosAtribuidos();
                    break;
                case "7":
                    AcessarHistoricoDetalhesChamado();
                    break;
                case "8":
                    RegistrarAcoesTempoGasto();
                    break;
                case "9":
                    AtualizarStatusFecharChamado();
                    break;
                case "10":
                    running = false;
                    Console.WriteLine("Saindo do menu do técnico...");
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
        Console.WriteLine("==== VERIFICAR SOLUÇÃO DA IA (Técnico) ====");
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
                // A IA armazena a sugestão inicial no campo SolucaoAplicada ao criar o chamado.
                if (!string.IsNullOrWhiteSpace(chamado.SolucaoAplicada))
                {
                    Console.WriteLine($"ID do Chamado: {chamado.Id}");
                    Console.WriteLine($"Descrição do Problema: {chamado.Descricao}");
                    Console.WriteLine($"Categoria: {chamado.Categoria}");
                    Console.WriteLine($"---");
                    Console.WriteLine($"Sugestão da IA: {chamado.SolucaoAplicada}");
                    Console.WriteLine($"---");

                    // Adiciona a busca por outras soluções na base de conhecimento
                    Console.WriteLine("\n--- Outras Soluções da Base de Conhecimento (se houver) ---");
                    // Passa a descrição e categoria do chamado para buscar sugestões.
                    List<string> sugestoesAdicionais = Chamado.BuscarSugestoesSolucao(chamado.Descricao, chamado.Categoria);

                    // Filtra para não mostrar a mesma solução que já está em chamado.SolucaoAplicada
                    bool algumaAdicional = false;
                    foreach (var sugestao in sugestoesAdicionais)
                    {
                        // Verifica se a sugestão adicional é realmente diferente da principal já mostrada
                        // Esta verificação pode precisar ser mais robusta dependendo do formato exato das strings.
                        // Por exemplo, se 'chamado.SolucaoAplicada' for apenas a string da solução e 'sugestao' for "Título: X\nSolução: Y"
                        if (!sugestao.EndsWith(chamado.SolucaoAplicada.Substring(0, Math.Min(chamado.SolucaoAplicada.Length, 100)) + "..."))
                        {
                            Console.WriteLine(sugestao);
                            Console.WriteLine("---");
                            algumaAdicional = true;
                        }
                    }
                    if (!algumaAdicional)
                    {
                        Console.WriteLine("Nenhuma outra sugestão distinta encontrada na base de conhecimento para este chamado.");
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
        Console.WriteLine("==== REGISTRAR CHAMADO (Técnico) ====");
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
        Console.WriteLine("==== ACOMPANHAR STATUS DE CHAMADO (Técnico) ====");
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
        Console.WriteLine("==== AVALIAR ATENDIMENTO (Técnico) ====");
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
        Console.WriteLine("==== NOTIFICAÇÕES (Técnico) ====");
        Chamado.ReceberNotificacoes(_usuario.Id);
        Console.WriteLine("Pressione Enter para continuar."); 
        Console.ReadLine();
    }


    private void VisualizarChamadosAtribuidos()
    {
        Console.Clear();
        Console.WriteLine("==== CHAMADOS ATRIBUÍDOS A VOCÊ ====");

        List<Chamado> chamadosAtribuidos = Chamado.GetAssignedChamados(_usuario.Id);

        if (chamadosAtribuidos == null || chamadosAtribuidos.Count == 0)
        {
            Console.WriteLine("Nenhum chamado atribuído no momento.");
        }
        else
        {
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"| {"ID",-5} | {"Status",-15} | {"Urgência",-10} | {"Categoria",-15} | {"Descrição",-20} |");
            Console.WriteLine("--------------------------------------------------------------------------");
            foreach (var chamado in chamadosAtribuidos)
            {
                Console.WriteLine($"| {chamado.Id,-5} | {chamado.Status,-15} | {chamado.NivelUrgencia,-10} | {chamado.Categoria,-15} | {chamado.Descricao.Substring(0, Math.Min(chamado.Descricao.Length, 20)),-20} |");
            }
            Console.WriteLine("--------------------------------------------------------------------------");
        }

        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void AcessarHistoricoDetalhesChamado()
    {
        Console.Clear();
        Console.WriteLine("==== DETALHES E HISTÓRICO DO CHAMADO ====");
        Console.Write("Digite o ID do chamado: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            Chamado chamado = Chamado.GetChamadoDetails(idChamado);

            if (chamado != null)
            {
                Console.WriteLine("\n--- Detalhes do Chamado ---");
                Console.WriteLine($"ID: {chamado.Id}");
                Console.WriteLine($"Descrição: {chamado.Descricao}");
                Console.WriteLine($"Data de Abertura: {chamado.DataAbertura:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Data de Fechamento: {chamado.DataFechamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Aberto"}");
                Console.WriteLine($"Status: {chamado.Status}");
                Console.WriteLine($"Urgência: {chamado.NivelUrgencia}");
                Console.WriteLine($"Categoria: {chamado.Categoria}");
                Console.WriteLine($"Tempo Total de Atendimento: {chamado.TempoTotalAtendimento?.ToString("F2") ?? "N/A"} minutos");
                Console.WriteLine($"ID do Solicitante: {chamado.IdSolicitante}");
                Console.WriteLine($"ID do Técnico Responsável: {chamado.IdTecnicoResponsavel?.ToString() ?? "Não Atribuído"}");
                Console.WriteLine($"Solução Aplicada: {chamado.SolucaoAplicada ?? "N/A"}");

                List<string> historico = Chamado.GetHistoricoChamado(idChamado);
                Console.WriteLine("\n--- Histórico do Chamado ---");
                if (historico.Count == 0)
                {
                    Console.WriteLine("Nenhum histórico encontrado para este chamado.");
                }
                else
                {
                    foreach (var entrada in historico)
                    {
                        Console.WriteLine(entrada);
                    }
                }

                Console.WriteLine("\n--- Soluções Sugeridas ---");
                List<string> sugestoes = Chamado.BuscarSugestoesSolucao(chamado.Descricao, chamado.Categoria);
                if (sugestoes.Count == 0)
                {
                    Console.WriteLine("Nenhuma solução sugerida encontrada na Base de Conhecimento.");
                }
                else
                {
                    int count = 1;
                    foreach (var sugestao in sugestoes)
                    {
                        Console.WriteLine($"Sugestão {count}:");
                        Console.WriteLine(sugestao);
                        Console.WriteLine("---");
                        count++;
                    }
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


    private void RegistrarAcoesTempoGasto()
    {
        Console.Clear();
        Console.WriteLine("==== REGISTRAR AÇÕES E TEMPO GASTO ====");
        Console.Write("Digite o ID do chamado: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            Chamado chamado = Chamado.GetChamadoDetails(idChamado);
            if (chamado == null || chamado.IdTecnicoResponsavel != _usuario.Id)
            {
                Console.WriteLine($"Chamado com ID {idChamado} não encontrado ou não atribuído a você.");
                Console.WriteLine("Pressione Enter para continuar.");
                Console.ReadLine();
                return;
            }

            Console.Write("Descreva a ação realizada: ");
            string descricaoAcao = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(descricaoAcao))
            {
                Console.WriteLine("A descrição da ação não pode ser vazia.");
                Console.WriteLine("Pressione Enter para continuar.");
                Console.ReadLine();
                return;
            }


            Console.Write("Tempo gasto (em minutos): ");
            if (double.TryParse(Console.ReadLine(), out double tempoGasto))
            {
                if (tempoGasto <= 0)
                {
                    Console.WriteLine("O tempo gasto deve ser maior que zero.");
                    Console.WriteLine("Pressione Enter para continuar.");
                    Console.ReadLine();
                    return;
                }

                bool sucesso = Chamado.AdicionarRegistroTempo(idChamado, _usuario.Id, tempoGasto, descricaoAcao);

                Console.WriteLine(sucesso ? "Registro de tempo e ação salvo com sucesso!" : "Erro ao salvar registro de tempo e ação.");

            }
            else
            {
                Console.WriteLine("Tempo gasto inválido.");
            }
        }
        else
        {
            Console.WriteLine("ID de chamado inválido.");
        }

        Console.WriteLine("Pressione Enter para continuar.");
        Console.ReadLine();
    }

    private void AtualizarStatusFecharChamado()
    {
        Console.Clear();
        Console.WriteLine("==== ATUALIZAR STATUS / FECHAR CHAMADO ====");
        Console.Write("Digite o ID do chamado: ");
        if (int.TryParse(Console.ReadLine(), out int idChamado))
        {
            Chamado chamado = Chamado.GetChamadoDetails(idChamado);
            if (chamado == null || chamado.IdTecnicoResponsavel != _usuario.Id)
            {
                Console.WriteLine($"Chamado com ID {idChamado} não encontrado ou não atribuído a você.");
                Console.WriteLine("Pressione Enter para continuar.");
                Console.ReadLine();
                return;
            }


            Console.Write("Digite o novo status (ABERTO, EM_ANDAMENTO, RESOLVIDO, FECHADO): ");
            string statusInput = Console.ReadLine().ToUpper();
            StatusChamado novoStatus;

            if (Enum.TryParse(statusInput, out novoStatus))
            {
                string solucaoAplicada = null;
                DateTime? dataFechamento = null;

                if (novoStatus == StatusChamado.RESOLVIDO || novoStatus == StatusChamado.FECHADO)
                {
                    Console.Write("Descreva a solução aplicada (obrigatório para RESOLVIDO/FECHADO): ");
                    solucaoAplicada = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(solucaoAplicada))
                    {
                        Console.WriteLine("A solução aplicada é obrigatória para fechar ou resolver o chamado.");
                        Console.WriteLine("Pressione Enter para continuar.");
                        Console.ReadLine();
                        return;
                    }
                    dataFechamento = DateTime.Now;
                }

                bool sucesso = Chamado.AtualizarChamadoStatusSolucao(idChamado, novoStatus, solucaoAplicada, dataFechamento, _usuario.Id); // Pass the ID of the logged-in technician as the updating user

                Console.WriteLine(sucesso ? $"Status do chamado {idChamado} atualizado para {novoStatus}." : $"Erro ao atualizar o status do chamado {idChamado}.");
                if (sucesso && (novoStatus == StatusChamado.RESOLVIDO || novoStatus == StatusChamado.FECHADO))
                {
                    Console.WriteLine("Chamado fechado/resolvido com sucesso.");
                }

            }
            else
            {
                Console.WriteLine("Status inválido.");
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

