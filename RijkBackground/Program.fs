// Learn more about F# at http://fsharp.org
open System.Runtime.InteropServices
open System.IO
open System.Text.Json.Serialization
open System
open FSharp.Data

[<Literal>]
let key = "qZBfiLms"

[<Literal>]
let baseURL = "https://www.rijksmuseum.nl/api/en/collection?key=" + key + "&culture=en"

type Collection = JsonProvider<baseURL>

let getLimit (count: int): int =
    if count > 10 * 1000 then
        printfn "Warning: %d results for this query, but we'll only pick one of the 10.000st results" count
        10 * 1000
    else
        count

let callAPI (searchTerm: string): string =
    printf "Getting count..."
    let resultsPerPage = string 1
    // Can't get more than 10K pages
    let searchURL = baseURL + "&imgonly=True&type=painting&ps=" + resultsPerPage + "&q=" + searchTerm

    let result = Collection.Load(searchURL)
    let limit = getLimit (result.Count)

    let page = string (Random().Next(limit))

    printfn "Downloading picture %s out of %d..." page limit

    let result = Collection.Load(searchURL + "&p=" + page)

    if result.ArtObjects.Length > 1 then failwith "More than 1 art object"

    result.ArtObjects.[0].WebImage.Url

let saveImage(url: string): string =
    let result = Http.Request(url)
    match result.Body with
    | Text text ->
        failwith "The image URL didn't return any binary data"
    | Binary bytes ->
        printfn "Downloading %d KB" (bytes.Length / 1024)
        let extension =
            match result.Headers.Item("Content-Type") with
            | "image/jpeg" -> "jpg"
            | "image/png" -> "png"
            | mimeType -> failwith "Unexpected mime type " + mimeType

        let file = Path.Combine(Directory.GetCurrentDirectory(), "background." + extension)
        File.WriteAllBytes(file, bytes)
        file

module Bindings =
    [<DllImport("user32.dll")>]
    extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni)
    
let setAsBackground(file: string): int =
    printfn "Setting %s as wallpaper" file
    let SPI_SETDESKWALLPAPER = uint32 20
    let param = uint32 1
    let SPIF_UPDATEINIFILE = uint32 0x1;
    
    Bindings.SystemParametersInfo(SPI_SETDESKWALLPAPER, param, file, SPIF_UPDATEINIFILE)
    
    
    // Best
    //if (style == Style.Fit)
    //    key.SetValue(@"WallpaperStyle", 6.ToString());
    //    key.SetValue(@"TileWallpaper", 0.ToString())

    //if (style == Style.Fill)
    //{
    //    key.SetValue(@"WallpaperStyle", 10.ToString());
    //    key.SetValue(@"TileWallpaper", 0.ToString());
    //}
    
[<EntryPoint>]
let main argv =
    let term = "landscape"

    printfn "Hello Rijkmuseum API!"

    let url = callAPI (term)

    printfn "Downloading %s" url

    let file = saveImage(url)
    printfn "------ %d" (setAsBackground(file))

    0 // return an integer exit code
