using System;
using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;

namespace StepCue.TenantApp.Web.Data
{
    public class DataContextFactory
    {
        /// <summary>
        /// Creates a new instance of DataContext with an in-memory database
        /// Useful for testing scenarios
        /// </summary>
        /// <param name="databaseName">Optional name for the in-memory database</param>
        /// <returns>A configured instance of DataContext</returns>
        public static DataContext Create(string databaseName = "InMemoryDbForTesting")
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new DataContext(options);
        }
    }
}