using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using System.Text.RegularExpressions;

public class IA
{
    public class ResultadoIA
    {
        public NivelUrgencia DeterminarUrgencia { get; set; }
        public string SolucaoSugerida { get; set; }
    }

    public static ResultadoIA ProcessarChamado(string descricao, string categoria, OracleConnection conn, OracleTransaction transaction)
    {
        NivelUrgencia determinedUrgency = NivelUrgencia.BAIXO;
        string suggestedSolution = null;

        try
        {
            string sqlSla = @"
                SELECT NivelUrgencia
                FROM SLA
                WHERE CategoriaChamado = :categoria";

            List<NivelUrgencia> urgenciasPossiveis = new List<NivelUrgencia>();

            using (var cmdSla = new OracleCommand(sqlSla, conn))
            {
                cmdSla.Transaction = transaction;
                cmdSla.Parameters.Add(":categoria", OracleDbType.Varchar2).Value = categoria;

                using (var readerSla = cmdSla.ExecuteReader())
                {
                    while (readerSla.Read())
                    {
                        if (Enum.TryParse(readerSla["NivelUrgencia"].ToString(), out NivelUrgencia nivel))
                        {
                            urgenciasPossiveis.Add(nivel);
                        }
                    }
                }
            }

            if (urgenciasPossiveis.Count > 0)
            {
                Dictionary<NivelUrgencia, int> urgencyOrder = new Dictionary<NivelUrgencia, int>
                {
                    { NivelUrgencia.BAIXO, 1 },
                    { NivelUrgencia.MEDIO, 2 },
                    { NivelUrgencia.ALTO, 3 },
                    { NivelUrgencia.CRITICO, 4 }
                };

                determinedUrgency = urgenciasPossiveis.OrderByDescending(u => urgencyOrder[u]).First();
            }

            suggestedSolution = BuscarSolucaoBaseConhecimento(descricao, categoria, conn, transaction);

            return new ResultadoIA
            {
                DeterminarUrgencia = determinedUrgency,
                SolucaoSugerida = suggestedSolution
            };
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle durante o processamento da IA: " + ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado durante o processamento da IA: " + ex.Message);
            throw;
        }
    }

    private static string BuscarSolucaoBaseConhecimento(string descricaoProblema, string categoria, OracleConnection conn, OracleTransaction transaction)
    {
        try
        {
            // Extrai palavras-chave da descrição
            char[] delimiters = new char[] { ' ', '.', ',', ';', ':', '!', '?', '\n', '\r', '\t' };
            string[] palavras = descricaoProblema.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string[] stopwords = { "A", "O", "E", "DE", "DO", "DA", "UM", "UMA", "ESTÁ", "NÃO", "COM", "POR", "PARA" };

            List<string> palavrasChave = palavras
                .Select(p => p.ToUpper())
                .Where(p => p.Length >= 3 && !stopwords.Contains(p))
                .Distinct()
                .ToList();

            Console.WriteLine("Palavras-chave extraídas:");
            palavrasChave.ForEach(p => Console.WriteLine($" - {p}"));
            Console.WriteLine("Pressione Enter para continuar...");
            Console.ReadLine();

            // Monta SQL dinâmica
            string sql = @"
            SELECT Solucao
            FROM BaseConhecimento
            WHERE Categoria = :categoria";

            if (palavrasChave.Count > 0)
            {
                sql += " AND (";
                for (int i = 0; i < palavrasChave.Count; i++)
                {
                    if (i > 0) sql += " OR ";
                    sql += $"UPPER(Titulo) LIKE :palavra{i} OR UPPER(Descricao) LIKE :palavra{i}";
                }
                sql += ")";
            }

            sql += " FETCH FIRST 1 ROW ONLY";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Transaction = transaction;

                cmd.BindByName = true;

                // Parâmetro de categoria (sem ":" no nome do parâmetro)
                cmd.Parameters.Add("categoria", OracleDbType.Varchar2).Value = categoria;

                // Parâmetros das palavras-chave (com "%" incluído e nomes sem ":")
                for (int i = 0; i < palavrasChave.Count; i++)
                {
                    string nomeParametro = $"palavra{i}";
                    string valor = $"%{palavrasChave[i]}%";
                    cmd.Parameters.Add(nomeParametro, OracleDbType.Varchar2).Value = valor;
                }

                // Execução da consulta
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader["Solucao"]?.ToString();
                    }
                    else
                    {
                        Console.WriteLine("Nenhuma solução encontrada.");
                        return null;
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao buscar solução: " + ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao buscar solução: " + ex.Message);
            throw;
        }
    }



    public static List<string> GetCategoriasSLA()
    {
        List<string> categorias = new List<string>();
        try
        {
            using (var conn = OracleConnectionFactory.GetConnection())
            {
                conn.Open();
                string sql = "SELECT DISTINCT CategoriaChamado FROM SLA ORDER BY CategoriaChamado";
                using (var cmd = new OracleCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categorias.Add(reader["CategoriaChamado"].ToString());
                        }
                    }
                }
            }
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao buscar categorias de SLA: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao buscar categorias de SLA: " + ex.Message);
        }
        return categorias;
    }
}