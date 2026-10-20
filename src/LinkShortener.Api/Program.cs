using LinkShortener.Api;

var app = ApiApplication.Build(new WebApplicationOptions { Args = args });

app.Run();
