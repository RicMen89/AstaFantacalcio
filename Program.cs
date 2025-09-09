using AstaFantacalcio.Models;
using AstaFantacalcio.Services;

namespace AstaFantacalcio
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton<ExcelImportService>();
            builder.Services.AddSingleton<AuctionService>();
            builder.Services.AddSingleton<AuctionOrchestrator>();
        
            builder.Services.AddSession();
            builder.Services.AddSignalR();

            // Configurazione delle impostazioni
            builder.Services.Configure<AuctionSettings>(
                builder.Configuration.GetSection("AuctionSettings"));
            builder.Services.Configure<AdminSettings>(
                builder.Configuration.GetSection("AdminSettings"));
            builder.Services.Configure<ParticipantSettings>(
                builder.Configuration.GetSection("ParticipantSettings"));

            //builder.Services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
            //});

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            app.UseSession();

            app.MapHub<Hubs.AuctionHub>("/auctionHub");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();

        }
    }
}
