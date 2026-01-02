
using IptvMan.Clients;
using IptvMan.Services;
using LiteDB;

namespace IptvMan;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Ensure the database directory exists
        Directory.CreateDirectory(Configuration.AppDataDirectory);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddHttpClient();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IXtreamClient, XtreamClient>();
        builder.Services.AddSingleton<IAccountService, AccountService>();
        builder.Services.AddSingleton<IChannelMappingService, ChannelMappingService>();

        builder.Services.AddSingleton<ILiteDatabase>(new LiteDatabase(Configuration.DatabasePath));
        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
