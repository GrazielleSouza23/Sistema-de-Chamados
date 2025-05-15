using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using System;

public static class OracleConnectionFactory
{
    private static string connectionString = "User Id=grazielle;Password=root;Data Source=localhost:1521/XEPDB1;";

    public static OracleConnection GetConnection()
    {
        try
        {
            var conn = new OracleConnection(connectionString);
            return conn;
        }
        catch (OracleException ex)
        {
            Console.WriteLine("Erro Oracle ao obter conexão: " + ex.Message);
            throw new Exception("Não foi possível conectar ao banco de dados Oracle.", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado ao obter conexão: " + ex.Message);
            throw new Exception("Erro interno ao tentar obter conexão com o banco de dados.", ex);
        }
    }
}