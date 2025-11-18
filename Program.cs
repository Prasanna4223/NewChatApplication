using SignalRChatApplication; // Make sure you reference the correct namespace for ChatHub
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// **Correct place to configure the SignalR Hub** (replace `_ =` with direct call)
//app.MapHub<ChatHub>("/chatHub"); // match JS
//app.MapHub<CallHub>("/callHub");
app.MapHub<CallHub>("/callHub");   // ? EXACTLY lowercase c, lowercase h, ub

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();