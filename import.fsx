#r "nuget: IdentityModel"
#r "nuget: FSharp.Data"
open System
open System.IO
open System.Net
open System.Net.Http
open IdentityModel.Client
open System.Text.Json
open FSharp.Data

// Set import file, uri's, secrets, mapped columns, etc here
type ImportFile = CsvProvider<"sample.csv">
let importFilePath = "sample.csv"
let importUri = ""
let identityServerUri = ""
let identityClientId = ""
let identityScope = ""
let identityClientSecret = ""

let mapToDomain (provider: ImportFile) =
    provider.Rows
    |> Seq.choose (fun row -> 
        match Option.ofNullable row.NumericId, row.Email with
        | None, _ | _, null | _, "" -> None
        | Some id, email -> 
            Some {|
                Id = id
                Email = email
            |})

let getIdentityServerToken uri clientId scopeName clientSecret =
    async {
        let! discovery = (new HttpClient()).GetDiscoveryDocumentAsync(uri) |> Async.AwaitTask
        if discovery.IsError then
            return discovery.Error |> Error
        else
            let! token =
                (new HttpClient()).RequestClientCredentialsTokenAsync(
                    new ClientCredentialsTokenRequest(
                        Address = discovery.TokenEndpoint,
                        ClientId = clientId,
                        ClientSecret = clientSecret,
                        Scope = scopeName)
                ) |> Async.AwaitTask
            return token.AccessToken |> Ok
    } 

let makeImportRequest url token request =
    Http.AsyncRequestString(
        url, 
        httpMethod = HttpMethod.Post,
        headers = [ 
            "Accept", HttpContentTypes.Json
            "content-type", HttpContentTypes.Json
            "Authorization", $"Bearer %s{token}"
        ],
        body = TextRequest request)
    |> Async.Catch

let toResponseError =
    function
    | Choice1Of2 _ -> None
    | Choice2Of2 e -> Some e

let ParallelThrottle throttle workflows = Async.Parallel(workflows, throttle)

let runImport fetchToken makeRequest filePath = 
    fetchToken
    |> Async.RunSynchronously
    |> function
    | Ok token -> 
        let importData = filePath |> Path.GetFullPath |> ImportFile.Load |> mapToDomain |> Seq.toList
        let errors: Exception array =
            importData
            |> List.map (JsonSerializer.Serialize >> makeRequest token)
            |> ParallelThrottle 10
            |> Async.RunSynchronously
            |> Array.choose toResponseError
        printfn $"Completed {importData.Length} requests. Errors: {errors.Length}"
        if not (Array.isEmpty errors) then
            let errorMessages = errors |> Array.map (fun e -> e.Message)
            printfn $"See ResponseErrors.txt for full errors."
            File.WriteAllLines("ResponseErrors.txt", errorMessages)
    | Error e -> printfn $"error fetching token {e}"

let fetchToken = getIdentityServerToken identityServerUri identityClientId identityScope identityClientSecret
let makeRequest = makeImportRequest importUri
runImport fetchToken makeRequest importFilePath