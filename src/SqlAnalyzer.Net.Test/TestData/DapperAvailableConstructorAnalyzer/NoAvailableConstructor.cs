using Dapper;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SqlAnalyzer.Net.Test
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var sql = new SqlConnection();
            await sql.QueryAsync<Dto>(@"SELECT * FROM Dto");
        }
    }

    public class Dto
    {
        public int I { get; set; }
        public string Am { get; set; }
        public uint Inaccessible { get; set; }
        public Dto(int i) { I = i; }
    }
}
