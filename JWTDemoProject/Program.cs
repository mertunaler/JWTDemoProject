using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using TaskManagementSystem.Model;
using TaskManagementSystem.Model.DBContext;
using TaskManagementSystem.Service;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));


builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Issuer"],
        ValidAudience = builder.Configuration["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["SigningKey"]))
    };
});

builder.Services.AddSingleton<ITokenService>(new TokenService(builder));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
  {
    {
      new OpenApiSecurityScheme
      {
        Reference = new OpenApiReference
        {
          Type = ReferenceType.SecurityScheme,
          Id = "Bearer"
        },
        Scheme = "oauth2",
        Name = "Bearer",
        In = ParameterLocation.Header,
      },
      new List<string>()
    }});
});



var app = builder.Build();

app.UseAuthorization();
app.UseAuthentication();



if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1"));
}


app.MapGet("/", () => "This is a demo for learning JWT for ongoing project of mine :D. Type in /swagger to explore the available endpoints");

app.MapPost("/Login", async (AppDbContext _context, HttpContext http, ITokenService service, Login login) =>
{
    if (!string.IsNullOrEmpty(login.UserName) &&
      !string.IsNullOrEmpty(login.Password))
    {
        var userModel = await _context.Users.Where(u => u.Name == login.UserName && u.Password == login.Password).FirstOrDefaultAsync();
        if (userModel == null)
        {
            http.Response.StatusCode = 401;
            await http.Response.WriteAsJsonAsync(new { Message = "WHO ARE YOU!" });
            return;
        }
        var token = service.GetToken(userModel);
        await http.Response.WriteAsJsonAsync(new { token = token });
        return;
    }
});

app.MapPost("/AddUser", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (AppDbContext _context, User user) =>
{
    _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
    return new OkResult();
});

app.Run();
