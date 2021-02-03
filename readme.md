# Fsharp 5.0 script import test
Example/Test of newer F# scripting features for data import task.

## Some covered newer F# features
- script downloads package dependencies (nuget)
- String interpolation
- anonymous records

## What it does
- **Reads file** using a typeprovider
    - CsvProvider can be swapped e.g. JsonProvider
- **Validates & maps** to a domain type
    - skipping invalid data
- **Fetches Token**
    - Identity Server in this case
- **Posts** data to API endpoint
    - in parallel Asynchronous batches
- **Results** summarised
- **Collates** any response errors
    - writes to file ```ResponseErrors.txt```

## Requirements
- [dotnet core > 5.0](https://dotnet.microsoft.com/download/dotnet-core)

## Run
- Set values for: import file, uri's, secrets, mapped columns, etc
- run ```dotnet fsi import.fsx```