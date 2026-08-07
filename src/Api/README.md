# Api (Host)

The single ASP.NET Core Web API host process. Wires up all modules, middleware,
authentication, Swagger/OpenAPI, rate limiting, health checks and Serilog.
This is the only project that references every module's `Api` layer.
