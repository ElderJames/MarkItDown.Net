using MarkItDown.Web.Components;
using MarkItDownSharp;
using MarkItDownSharp.Extensions.AliyunOCR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntDesign();
builder.Services.AddMarkItDown(options =>
{
    options.UseAliyunOcr(ocrOptions =>
    {
        ocrOptions.AccessKeyId = builder.Configuration["AliyunOcr:AccessKeyId"];
        ocrOptions.AccessKeySecret = builder.Configuration["AliyunOcr:AccessKeySecret"];
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
