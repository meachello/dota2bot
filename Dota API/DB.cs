namespace Dota_API;
using MySql.Data.MySqlClient;

public class DB
{
    MySqlConnection connection = new MySqlConnection("server=dota2api.mysql.database.azure.com;port=3306;username=loh@dota2api;password=#Syst3m007;database=account_id");

    public void OpenConnection()
    {
        if (connection.State == System.Data.ConnectionState.Closed)
        {
            connection.Open();
        }
    }

    public void CloseConnection()
    {
        if (connection.State == System.Data.ConnectionState.Open)
        {
            connection.Close();
        }
    }

    public MySqlConnection getConnection()
    {
        return connection;
    }
}