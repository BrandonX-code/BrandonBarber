using Barber.Maui.API.Data;
using Barber.Maui.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
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
//builder.Services.AddHostedService<ReminderService>();

var app = builder.Build();

app.UseDeveloperExceptionPage();
builder.Configuration.AddEnvironmentVariables();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

// ✅ CONFIGURAR ARCHIVOS ESTÁTICOS PARA SERVIR APK
app.UseStaticFiles();

// ✅ Servir archivos desde wwwroot con configuración específica para APK
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/vnd.android.package-archive",
    ContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider
    {
        Mappings = { [".apk"] = "application/vnd.android.package-archive" }
    }
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorPages();
app.MapControllers();

app.Run();