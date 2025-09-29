using MappedFolderServer.Data;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDatabaseContext>();
builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options =>
{
    options.LoginPath = "/password";  // where unauthenticated users are redirected
    options.AccessDeniedPath = "/forbidden";
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireClaim("AdminUserId", "admin");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var options = new RewriteOptions()
    .AddRewrite(@"^password\/?$", "password.html", skipRemainingRules: true)
    .AddRewrite(@"^slugs\/?$", "slugs.html", skipRemainingRules: true)
    .AddRewrite(@"^forbidden\/?$", "forbidden.html", skipRemainingRules: true);
app.UseRewriter(options);
app.UseStaticFiles();
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("Migrating Database");
using (var db = new AppDatabaseContext())
{
    db.Database.Migrate();
}
Console.WriteLine("Migrated Database");


app.Run();