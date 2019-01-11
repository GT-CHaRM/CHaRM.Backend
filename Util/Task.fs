module CHaRM.Util.Task

open System.Threading.Tasks

type 't task = Task<'t>

let withError<'err, 'a when 'err :> exn> (f: 'err -> unit) (input: 'a task) =
    input.ContinueWith((fun (t: 'a task) ->
        if t.Exception <> null then
            t.Exception.InnerExceptions
            |> Seq.filter (fun err -> err :? 'err)
            |> Seq.iter (fun err -> f (err :?> 'err))))
            |> ignore
    input

