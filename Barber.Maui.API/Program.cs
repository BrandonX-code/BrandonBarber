using Barber.Maui.API.Data;
using Barber.Maui.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://0.0.0.0:5286", "https://0.0.0.0:7283");

//builder.WebHost.ConfigureKestrel(options =>
//{
//    var port = Environment.GetEnvironmentVariable("PORT") ?? "5286";
//    options.ListenAnyIP(int.Parse(port));
//});
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT");

    if (port != null)
    {
        // Modo Render
        options.ListenAnyIP(int.Parse(port));
    }
    else
    {
        // Modo LOCAL
        options.ListenAnyIP(5286); // HTTP
        options.ListenAnyIP(7283, listen => listen.UseHttps()); // HTTPS
    }
});
builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();
// Add services to the container.
builder.Services.AddHttpClient(); // ✅ Esto resuelve el error
builder.Services.AddRazorPages(); // ✅ Para las Razor Pages
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    ));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
var app = builder.Build();
app.UseDeveloperExceptionPage();
builder.Configuration.AddEnvironmentVariables();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();
//}
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorPages(); // ✅ Mapear las Razor Pages
app.MapControllers();

app.Run();
