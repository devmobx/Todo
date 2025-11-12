# .NET Cheat Sheet

## Templates

```bash
# Create Web API Template

# Setup template in already created project folder
dotnet new webapi --use-controllers -o .

# Create template and the project folder
dotnet new webapi --use-controllers -o <path>/<project-name>
```

## Run

```bash
# Use to trust the dev certificates for the http profile
dotnet dev-certs https --trust

# Launch the api on the http profile
dotnet run --launch-profile https

# Build the binaries
dotnet build
```

## Package Management

```bash
dotnet add package <package-name>
```

### Common packages

| Purpose                     | Package                                |
| --------------------------- | -------------------------------------- |
| In-memory databases         | Microsoft.EntityFrameworkCore.InMemory |
| Swagger / API documentation | NSwag.AspNetCore                       |
