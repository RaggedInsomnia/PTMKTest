using System.Globalization;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Npgsql;

namespace PTMKTest
{
    public class App
    {
        static readonly string connectionString =
            "Host=localhost;Username=postgres;Password=12345;Database=PTMKTest;";

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Use command line arguments to start program");
                return;
            }

            if (args.Length == 1)
            {
                int var = Convert.ToInt32(args[0]);
                switch (var)
                {
                    case 1:
                        using (NpgsqlConnection conn = new(connectionString))
                        {
                            string query =
                                "CREATE TABLE Employees (Id SERIAL PRIMARY KEY, FIO VARCHAR(100), BirthDate DATE, Sex VARCHAR(6))";
                            NpgsqlCommand command = new(query, conn);
                            try
                            {
                                conn.Open();
                                command.ExecuteNonQuery();
                                Console.WriteLine("Table was created successfully");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error creating database: {ex.Message}");
                            }
                        }

                        break;
                    case 3:
                        using (NpgsqlConnection conn = new(connectionString))
                        {
                            string query =
                                "SELECT DISTINCT ON (FIO, BirthDate) * FROM Employees ORDER BY FIO";
                            NpgsqlCommand command = new(query, conn);
                            try
                            {
                                conn.Open();
                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        Console.WriteLine(
                                            $"{reader["FIO"]} \t {DateTime.Parse(reader["BirthDate"].ToString()).ToString("yyyy-MM-dd")} \t {reader["Sex"]} \t Age:{Employee.CountYO(DateOnly.FromDateTime(DateTime.Parse(reader["BirthDate"].ToString())))}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error getting data: {ex.Message}");
                            }
                        }

                        break;
                    case 4:
                        List<Employee> employees = new();

                        for (int i = 0; i < 1_000_000; i++)
                        {
                            Employee employee = new()
                            {
                                FIO = GenerateFIO(),
                                BirthDate = DateOnly.FromDateTime(DateTime.Today.AddDays(new Random().Next(-18250, 0))),
                                Sex = new Random().Next(1, 3) == 1 ? "Male" : "Female"
                            };
                            employees.Add(employee);
                        }

                        Employee.SaveListToBD(employees, connectionString);

                        employees.RemoveRange(0, employees.Count);
                        for (int i = 0; i < 100; i++)
                        {
                            Employee employee = new()
                            {
                                FIO = GenerateFIO().Remove(0).Prepend('F').ToString(),
                                BirthDate = DateOnly.FromDateTime(
                                    DateTime.Today.AddDays(new Random().Next(-18250, 0))),
                                Sex = "Male"
                            };
                            employees.Add(employee);
                        }

                        Employee.SaveListToBD(employees, connectionString);

                        break;
                    case 5:
                        Stopwatch stopwatch = new();
                        stopwatch.Start();
                        using (NpgsqlConnection conn = new(connectionString))
                        {
                            string query =
                                "SELECT * FROM Employees WHERE Sex = 'Male' AND FIO LIKE 'F%'";
                            NpgsqlCommand command = new(query, conn);
                            try
                            {
                                conn.Open();
                                using (var reader = command.ExecuteReader())
                                {
                                    stopwatch.Stop();

                                    while (reader.Read())
                                    {
                                        Console.WriteLine(
                                            $"{reader["FIO"]} \t {DateTime.Parse(reader["BirthDate"].ToString()).ToString("yyyy-MM-dd")} \t {reader["Sex"]} \t Age:{Employee.CountYO(DateOnly.FromDateTime(DateTime.Parse(reader["BirthDate"].ToString())))}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error getting data: {ex.Message}");
                            }

                            Console.WriteLine(stopwatch.Elapsed);
                        }

                        break;
                    case 6:
                        using (NpgsqlConnection conn = new(connectionString))
                        {
                            string query =
                                "CREATE INDEX index_sex ON Employees(Sex);\nCREATE INDEX index_fio ON Employees(FIO);";
                            NpgsqlCommand command = new(query, conn);
                            try
                            {
                                command.ExecuteNonQuery();
                                Console.WriteLine("Indexes were created successfully");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error creating database: {ex.Message}");
                            }

                            stopwatch = new();
                            stopwatch.Start();
                            query =
                                "SELECT * FROM Employees WHERE Sex = 'Male' AND FIO LIKE 'F%'";
                            command = new(query, conn);
                            try
                            {
                                conn.Open();
                                using (var reader = command.ExecuteReader())
                                {
                                    stopwatch.Stop();

                                    while (reader.Read())
                                    {
                                        Console.WriteLine(
                                            $"{reader["FIO"]} \t {DateTime.Parse(reader["BirthDate"].ToString()).ToString("yyyy-MM-dd")} \t {reader["Sex"]} \t Age:{Employee.CountYO(DateOnly.FromDateTime(DateTime.Parse(reader["BirthDate"].ToString())))}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error getting data: {ex.Message}");
                            }

                            Console.WriteLine(stopwatch.Elapsed);
                        }

                        break;
                }
            }
            else if (args.Length == 4)
            {
                if (Convert.ToInt32(args[0]) == 2)
                {
                    Employee employee = new()
                    {
                        FIO = args[1],
                        BirthDate = DateOnly.Parse(args[2]),
                        Sex = args[3]
                    };
                    employee.SaveToBD(connectionString);
                }
                else
                    Console.WriteLine("Wrong operating mode");
            }
            else
                Console.WriteLine("Wrong amount of arguments");
        }

        private static int Menu() //вывод меню для выбора действия
        {
            int var = -1;
            Console.Clear();
            Console.WriteLine(
                "Choose option:\n1. Add Person\n2. Print unique objects\n3. Generate 1000000 records\n4. Generate 100 males\n5. Find males with 'F' starting name\n9. CLEAR\n0. Exit");
            while (!int.TryParse(Console.ReadLine(), out var))
                Console.WriteLine("Invalid format, try again: ");
            return var;
        }

        private static string GenerateFIO() //генерация имени
        {
            Random rand = new();
            char[] letters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            StringBuilder res = new();
            for (int i = 0; i < 3; i++)
            {
                int wordlength = rand.Next(3, 10); //генерация трех слов длиной от 3 до 10 символов
                for (int j = 0; j < wordlength; j++)
                {
                    res.Append(letters[rand.Next(letters.Length)]);
                }

                res.Append(' ');
            }

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(res.ToString()); //первая буква каждого слова становится заглавной
        }
    }
}