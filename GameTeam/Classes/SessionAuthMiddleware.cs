﻿namespace GameTeam.Classes
{
    public class SessionAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Пропускаем проверку для API auth endpoints и страниц входа/регистрации
            if (context.Request.Path.StartsWithSegments("/api/auth") ||
                context.Request.Path.StartsWithSegments("/Register") ||
                context.Request.Path.StartsWithSegments("/Login"))
            {
                context.Response.Headers.Append("X-Is-Authenticated", context.Session.GetString("IsAuthenticated"));
                Console.WriteLine(context.Session.GetString("IsAuthenticated"));
                await _next(context);
                return;
            }

            // Для всех остальных запросов проверяем аутентификацию
            var isAuthenticated = context.Session.GetString("IsAuthenticated");

            context.Response.Headers.Append("X-Is-Authenticated", context.Session.GetString("IsAuthenticated"));
            if (string.IsNullOrEmpty(isAuthenticated) || isAuthenticated != "true")
            {
                // Для API запросов возвращаем 401
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                // Для остальных перенаправляем на страницу входа
                else
                {
                    context.Response.Redirect("/Register");
                    return;
                }
            }

            // Если пользователь аутентифицирован, добавляем заголовки
            context.Response.Headers.Append("X-Is-Authenticated", "true");
            context.Response.Headers.Append("X-Username", context.Session.GetString("Username") ?? string.Empty);

            await _next(context);
        }
    }
}
