using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace kirchnerd.StompNet.Internals.Middleware;

public class Pipeline<T>
{
    private readonly List<Func<T, Func<Task>, Task>> _middlewares = new();
    private Func<T, Task>? _handler;

    public Pipeline<T> Use(Func<T, Func<Task>, Task> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    public void Run(Func<T, Task> handler)
    {
        _handler = handler;
    }

    public async Task ExecuteAsync(T context)
    {
        if (_handler is null) return;

        if (_middlewares.Count > 0)
        {
            var rest = _middlewares.ToList();
            await NextMiddleware();

            async Task NextMiddleware()
            {
                if (rest.Count > 0)
                {
                    var middleware = rest.First();
                    rest = rest.Skip(1).ToList();
                    await middleware(context, NextMiddleware);
                }
                else
                {
                    await _handler(context);
                }
            }
        }
    }
}