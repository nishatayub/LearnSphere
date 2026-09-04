using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Repositories;
using LearnSphere.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearnSphere.Tests
{
    /// <summary>
    /// Spins up a real SQLite database (in-memory, one connection per test) so tests
    /// exercise actual EF Core behavior - unique indexes, FK restrictions, etc. -
    /// instead of the looser semantics of the EF Core InMemory provider.
    /// </summary>
    public sealed class TestFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _services;

        public ApplicationDbContext Context { get; }
        public IUnitOfWork UnitOfWork { get; }
        public UserManager<User> UserManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }

        public TestFixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new ApplicationDbContext(options);
            Context.Database.EnsureCreated();

            var services = new ServiceCollection();
            services.AddSingleton(Context);
            services.AddLogging();
            services.AddDataProtection();
            services.AddIdentityCore<User>(opts =>
                {
                    opts.Password.RequireDigit = false;
                    opts.Password.RequireUppercase = false;
                    opts.Password.RequireLowercase = false;
                    opts.Password.RequireNonAlphanumeric = false;
                    opts.Password.RequiredLength = 4;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            _services = services.BuildServiceProvider();
            UserManager = _services.GetRequiredService<UserManager<User>>();
            RoleManager = _services.GetRequiredService<RoleManager<IdentityRole>>();

            UnitOfWork = new UnitOfWork(Context);

            foreach (var role in new[] { "Student", "Instructor", "Admin" })
            {
                RoleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }
        }

        public User CreateUser(string email, string role, string firstName = "Test", string lastName = "User")
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = new DateTime(2000, 1, 1),
                EmailConfirmed = true
            };

            var result = UserManager.CreateAsync(user, "Password1").GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            UserManager.AddToRoleAsync(user, role).GetAwaiter().GetResult();
            return user;
        }

        public void Dispose()
        {
            _services.Dispose();
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
