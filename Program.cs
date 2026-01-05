using Rabbit.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<Rabbit.Services.RabbitConsumer>();


var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Rabbit}/{action=Index}/{id?}");

app.Run();
