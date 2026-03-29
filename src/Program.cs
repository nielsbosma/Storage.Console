using Spectre.Console.Cli;
using Storage.Console.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<UploadCommand>("upload")
        .WithDescription("Upload a file or folder to Azure Blob Storage and get a read-only SAS URL");
});

return app.Run(args);
