using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TriUgla.WebApi.Services;

namespace TriUgla.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Core services
            builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();
            builder.Services.AddSingleton<IJobQueue>(_ => new ChannelJobQueue(capacity: 10_000));
            builder.Services.AddSingleton<IMeshingEngine, DummyMeshingEngine>();
            builder.Services.AddHostedService<MeshingWorker>();

            // JWT
            IConfigurationSection? jwt = builder.Configuration.GetSection("Jwt");
            string key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
