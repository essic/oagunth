module OagunthCore.Core
open System

[<AutoOpen>]
module OagunthErrors =
    
    let private join (c:string) (values: string seq) =
        String.Join(c,values |> Seq.toArray)
    
    type ITransformErrorToString =
        abstract member String : string
    
    type ErrorContent =
        | SingleMsg of string
        | SingleExn of exn
        | ManyMsg of string seq
        | ManyExn of exn seq
    
    module ErrorContent =
        let stringIt (e:ErrorContent) =
            match e with
            | SingleMsg msg -> msg
            | SingleExn ex -> ex.Message
            | ManyExn allEx -> allEx |> Seq.map (fun ex -> ex.Message) |> join "\n"
            | ManyMsg allMsg -> allMsg |> join "\n"

    type OagunthError =
        | OutsideCore of ErrorContent
        | FromCore of ErrorContent
        with
            interface ITransformErrorToString with
                member x.String =
                    match x with
                    | OutsideCore oc -> oc |> ErrorContent.stringIt
                    | FromCore fc -> fc |> ErrorContent.stringIt
    
    
    module OagunthError =
        let insideSingleExn ex =
            SingleExn ex |> OagunthError.FromCore
            
        let insideManyExn ex =
            ManyExn ex |> OagunthError.FromCore

        let insideSingle msg =
            SingleMsg msg |> OagunthError.FromCore
        let insideMany msg =
            ManyMsg msg |> OagunthError.FromCore

        let outsideSingleExn ex =
            SingleExn ex |> OagunthError.OutsideCore
            
        let outsideManyExn ex =
            ManyExn ex |> OagunthError.OutsideCore

        let outsideSingle msg =
            SingleMsg msg |> OagunthError.OutsideCore
        let outsideMany msg =
            SingleMsg msg |> OagunthError.OutsideCore
        