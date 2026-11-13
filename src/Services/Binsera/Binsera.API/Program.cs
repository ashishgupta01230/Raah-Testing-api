using Binsera.API.Applications.Extensions;
using Binsera.API.BackgroundServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using System.Text.RegularExpressions;
using Binsera.Application;
using Binsera.Application.Members;
using Binsera.Core;
using Binsera.Persistence;
using HautSphere.BuildingBlocks.Application.Extensions;
using HautSphere.BuildingBlocks.Configuration.Extensions;
using HautSphere.BuildingBlocks.Logger;
using HautSphere.BuildingBlocks.Neo4J.Configuration;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMvcWithFilters();
builder.Services.Configure<MvcOptions>(options => options.Conventions.Add(new RoutePrefixConvention("api")));
builder.Services.AddBinseraDbContext(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAutoRegisteredServices();
builder.Services.AddDomainEvents();
builder.Services.AddScoped<SessionManagerContext>();
CompanySetting.Default = builder.Configuration.GetSection("CompanySetting").Get<CompanySetting>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddNeo4j(options =>
{
    builder.Configuration.GetSection("Neo4jOptions").Bind(options);
});
builder.Services.AddScoped<ClientAwareSessionProvider>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Host.AddLogger();
var app = builder.Build();

// Serves the Angular apps merged into wwwroot: public-site at "/", admin
// under "/admin", member under "/member" (see haut-it-training-ui's
// scripts/production-deploy.js for how wwwroot is populated). Skipped in
// Development, where the UI runs on its own dev server (ng serve) instead.
//
// UseRouting() is called explicitly right after static files: without it,
// ASP.NET Core inserts routing/endpoint-matching at the very start of the
// pipeline instead of at this point, so requests for real static files
// (e.g. hashed JS chunks) were being matched by the catch-all SPA fallback
// route before UseStaticFiles ever got a chance to serve them.
// Angular's production build content-hashes bundle filenames
// (main-XXXXXXXX.js, chunk-XXXXXXXX.js, styles-XXXXXXXX.css), so those are
// safe to cache forever — a new deploy simply produces new filenames.
// Everything else (index.html, favicon, images, i18n json) keeps the SAME
// filename across deploys, so it must never be cached by the browser;
// otherwise users keep loading an old index.html that references bundle
// files which no longer exist, without a hard refresh.
var hashedBundlePattern = new Regex(@"-[0-9A-Za-z]{8,}\.(js|css)$", RegexOptions.Compiled);
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.GetTypedHeaders();
        if (hashedBundlePattern.IsMatch(ctx.File.Name))
        {
            var cacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(365),
            };
            cacheControl.Extensions.Add(new NameValueHeaderValue("immutable"));
            headers.CacheControl = cacheControl;
        }
        else
        {
            headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true,
            };
        }
    },
};

if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles(staticFileOptions);
}

app.UseRouting();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseCorsPolicy(app.Configuration);
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BinseraDbContext>();
    db.Database.Migrate();
}
app.MapControllers();

if (!app.Environment.IsDevelopment())
{
    // SPA client-side routing fallbacks, most specific mount first.
    app.MapFallbackToFile("/admin/{*path}", "admin/index.html", staticFileOptions);
    app.MapFallbackToFile("/member/{*path}", "member/index.html", staticFileOptions);
    app.MapFallbackToFile("{*path}", "index.html", staticFileOptions);
}

app.Run();
