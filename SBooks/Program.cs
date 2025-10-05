using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using SBooks.Data;
using SBooks.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<SbooksContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        // Cấu hình thêm cho cookie
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SbooksAuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("RequireUser", policy =>
        policy.RequireRole("User"));

    options.AddPolicy("RequireAdminOrUser", policy =>
        policy.RequireRole("Admin", "User"));
});

builder.Services.AddRazorPages(options =>
{
    // Folder /Admin: chỉ Admin vào
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdmin");

    // Folder /User: chỉ User vào
    // options.Conventions.AuthorizeFolder("/User", "RequireAdminOrUser");

});

var app = builder.Build();


#region Seeding data

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SbooksContext>();

    // Seed Roles nếu chưa có
    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Role { RoleName = "Admin" },
            new Role { RoleName = "User" }
        );
        context.SaveChanges();
    }

    // Seed Admin user nếu chưa có
    if (!context.Users.Any(u => u.Username == "admin"))
    {
        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Email = "admin@sbooks.com",
            FullName = "System Admin",
            IsActive = true
        };

        var adminRole = context.Roles.First(r => r.RoleName == "Admin");
        admin.Roles.Add(adminRole);   // EF sẽ tự insert vào user_roles

        context.Users.Add(admin);
        context.SaveChanges();
    }

    // Seed User thường nếu chưa có
    if (!context.Users.Any(u => u.Username == "user1"))
    {
        var user1 = new User
        {
            Username = "user1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
            Email = "user1@sbooks.com",
            FullName = "Normal User 1",
            IsActive = true
        };

        var userRole = context.Roles.First(r => r.RoleName == "User");
        user1.Roles.Add(userRole);

        context.Users.Add(user1);
        context.SaveChanges();
    }
}
#endregion


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
