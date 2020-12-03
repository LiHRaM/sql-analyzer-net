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
        public Dto() { }
    }
}
