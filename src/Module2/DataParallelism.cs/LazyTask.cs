using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataParallelism
{
    class LazyTask
    {
        static string cmdText = null;
        static SqlConnection conn = null;

        // Lazy asynchronous operation to initialize the Person object
        Lazy<Task<Person>> person =
            new Lazy<Task<Person>>(async () =>
            {
                using (var cmd = new SqlCommand(cmdText, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string firstName = reader["first_name"].ToString();
                        string lastName = reader["last_name"].ToString();
                        return new Person(firstName, lastName);
                    }
                }
                throw new Exception("Failed to fetch Person");
            });

        async Task<Person> FetchPerson()
        {
            return await person.Value;
        }
    }

    public class Person
    {
        public readonly string FullName;
        public Person(string firstName, string lastName)
        {
            FullName = firstName + " " + lastName;
            Console.WriteLine(FullName);
        }
    }
}
