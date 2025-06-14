using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;

namespace StepCue.TenantApp.Core.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected DataContext Context { get; }

        protected TestBase()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Context = new DataContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}