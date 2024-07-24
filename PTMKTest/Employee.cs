using System.Text;
using Npgsql;

namespace PTMKTest
{
    internal class Employee
    {
        public int Id { get; set; }
        public string? FIO { get; set; }
        public DateOnly BirthDate { get; set; }
        public string Sex { get; set; }

        public void SaveToBD(string connectionString)
        {
            using (NpgsqlConnection conn = new(connectionString))
            {
                string query =
                    $"INSERT INTO Employees (FIO, BirthDate, Sex) VALUES ('{FIO}', '{BirthDate.ToString("yyyy-MM-dd")}', '{Sex}')";
                NpgsqlCommand command = new(query, conn);
                try
                {
                    conn.Open();
                    command.ExecuteNonQuery();
                    Console.WriteLine("Person was added successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding person to DB: {ex.Message}");
                }
            }
        }

        public static void SaveListToBD(List<Employee> employees, string connectionString)
        {
            int BatchSize = 20000;
            using (NpgsqlConnection conn = new(connectionString))
            {
                try
                {
                    conn.Open();
                    for (int i = 0; i < employees.Count; i += BatchSize)
                    {
                        var query = new StringBuilder();
                        var batch = employees.GetRange(i, Math.Min(BatchSize, employees.Count - i));
                        query.Append("INSERT INTO Employees (FIO, BirthDate, Sex) VALUES ");

                        for (int j = 0; j < batch.Count; j++)
                        {
                            query.Append($"(@FIO{i + j}, @BirthDate{i + j}, @Sex{i + j})");
                            if (j < batch.Count - 1)
                            {
                                query.Append(',');
                            }
                        }

                        using (NpgsqlCommand command = new(query.ToString(), conn))
                        {
                            for (int j = 0; j < batch.Count; j++)
                            {
                                command.Parameters.AddWithValue($"@FIO{i + j}", batch[j].FIO);
                                command.Parameters.AddWithValue($"@BirthDate{i + j}", batch[j].BirthDate);
                                command.Parameters.AddWithValue($"@Sex{i + j}", batch[j].Sex);
                            }

                            command.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine("List was successfully saved to BD");
                }

                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static int CountYO(DateOnly dateofBirth)
        {
            var now = DateTime.Now;
            if (now.Year <= dateofBirth.Year)
                return 0;
            int n = now.Year - dateofBirth.Year;
            if (dateofBirth.DayOfYear > now.DayOfYear)
                --n;
            return n;
        }
    }
}