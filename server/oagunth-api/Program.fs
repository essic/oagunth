// Learn more about F# at http://fsharp.org
namespace Oagunth.Api

open System
open System.Globalization
open System.Security.Authentication
open Giraffe
open OagunthCore.Core.OagunthErrors
open Saturn
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks.V2
open NodaTime
open NodaTime.Extensions
open MongoDB.Driver
module Server =
    open Oagunth.Core.Time
    open Oagunth.Core.Ports
    open Adapters
    open Drivers
    open Dto
    
    [<CLIMutable>]
    type CreateActivitiesRequest =
        {
            Activities : string array
        }
     
    let serviceConfig (serviceCollection:IServiceCollection) =
        serviceCollection
            .AddScoped<IManageUser,UserService>()
            .AddScoped<IHandleUserActivityTracking,UserTimeTrackingService>()
            .AddScoped<IReferenceActivities,ActivityReferenceService>()
            .AddScoped<IHandleUserActivitySubmission,UserActivitySubmission>()
            .AddScoped<IMongoClient>
                ( fun provider ->
                    let config = provider.GetService<IConfiguration>()
                    let connectionString = config.Item("mongoDbUrl") 
                    let settings =
                        let s = MongoClientSettings.FromUrl(MongoUrl(connectionString))
                        let ss = SslSettings()
                        ss.EnabledSslProtocols <- SslProtocols.Tls12
                        s.SslSettings <- ss
                        s
                    MongoClient(settings) :> IMongoClient )
         
    let configureAppConfiguration  (context: WebHostBuilderContext) (config: IConfigurationBuilder) =  
        config
            .AddJsonFile("appsettings.json",false,true)
            .AddJsonFile(sprintf "appsettings.%s.json" context.HostingEnvironment.EnvironmentName ,true)
            .AddEnvironmentVariables() |> ignore
        
        if context.HostingEnvironment.EnvironmentName = Environments.Development then
            config.AddUserSecrets<UserService>() |> ignore
       
    let greet name =
        json {| text = sprintf "Hello, %s" name |}
        
    let getUserCurrentTimeTracking username : HttpHandler =
        fun next (ctx:HttpContext) ->
            let userService = ctx.GetService<IManageUser>()
            let timeTrackingService = ctx.GetService<IHandleUserActivityTracking>()
            let activitySubmissionService = ctx.GetService<IHandleUserActivitySubmission>()
            let reply = getUserCalendar userService  timeTrackingService
                            activitySubmissionService username None
            match reply with 
            | Error msg -> Response.badRequest ctx msg
            | Ok r ->
                let reply =  UserCalendarTrackingDto(r)
                json reply next ctx

    let getUserTimeTracking (username,dateString) : HttpHandler =
        fun next ctx ->
            let userService = ctx.GetService<IManageUser>()
            let timeDataService = ctx.GetService<IHandleUserActivityTracking>()
            let activitySubmissionService = ctx.GetService<IHandleUserActivitySubmission>()
            let mutable dateParseResult = DateTime()
            if DateTime.TryParseExact(dateString,"dd-MM-yyyy",
                                      CultureInfo.InvariantCulture,DateTimeStyles.None ,&dateParseResult ) |> not
            then
                dateString |> sprintf "Invalid date ! [%s]" |> Response.badRequest ctx
            else
                let date = LocalDate.FromDateTime dateParseResult
                let result =
                    getUserCalendar userService timeDataService activitySubmissionService username (Some date)
                    |> Result.map UserCalendarTrackingDto
                    |> Result.map (fun reply -> json reply next ctx)
                    |> Result.mapError
                           (fun err ->
                                let msg = (err :> ITransformErrorToString).String
                                Response.internalError ctx msg)
                match result with
                | Ok toReturn | Error toReturn -> toReturn

    let saveActivities (username,month,year) : HttpHandler =
        fun _ ctx -> task {
            let userService = ctx.GetService<IManageUser>()
            let timeDataService = ctx.GetService<IHandleUserActivityTracking>()
            let activitySubmissionService = ctx.GetService<IHandleUserActivitySubmission>()
            let! payload = ctx.BindJsonAsync<AddRequest>()
            let data = AddRequest.transform payload
            return!
                match addUserActivities userService timeDataService
                          activitySubmissionService username month year data with
                | Error msg -> Response.badRequest ctx (msg :> ITransformErrorToString).String
                | _ -> Response.ok ctx String.Empty
        }

    let fakeLogin username : HttpHandler =
        fun next ctx ->
            let userService = ctx.GetService<IManageUser>()
            match userService.CreateUser username with
            | Ok user ->
                let dto = UserDto(user) 
                json dto next ctx
            | Error msg -> Response.internalError ctx (msg :> ITransformErrorToString).String
            
    let removeLogin username : HttpHandler =
        fun _ ctx ->
            let userService = ctx.GetService<IManageUser>()
            match userService.DeleteUser username with
            | Error msg -> Response.internalError ctx (msg :> ITransformErrorToString).String
            | _ -> Response.ok ctx String.Empty
            
    let createActivities  = 
        fun next (ctx:HttpContext) -> task {
            let activityRefService = ctx.GetService<IReferenceActivities>()
            let! payload = ctx.BindJsonAsync<CreateActivitiesRequest>()
            return!
                match createActivities activityRefService payload.Activities with
                | Error msg -> Response.badRequest ctx (msg :> ITransformErrorToString)
                | Ok r ->
                    let dto = r |> List.map ActivityDto
                    json dto next ctx
        }

    let removeActivity name =
        fun _ (ctx:HttpContext) ->
            let activityRefService = ctx.GetService<IReferenceActivities>()
            match activityRefService.RemoveActivityWithName name with
            | Error msg -> Response.badRequest ctx (msg :> ITransformErrorToString).String
            | _ -> Response.ok ctx String.Empty
    
    let getActivities =
        fun next (ctx:HttpContext) ->
            let activityRefService = ctx.GetService<IReferenceActivities>()
            match activityRefService.GetAllActivities() with
            | Error msg -> Response.internalError ctx (msg :> ITransformErrorToString).String
            | Ok activities ->
                let dto = activities |> Seq.map ActivityDto |> Seq.toArray
                json dto next ctx
    
    let submitActivities (username,month,year,week) =
        fun _ (ctx:HttpContext) ->
            let userService = ctx.GetService<IManageUser>()
            let activityTrackingService = ctx.GetService<IHandleUserActivityTracking>()
            let activitySubmissionService = ctx.GetService<IHandleUserActivitySubmission>()
            match userService.GetUser username with
            | Error msg -> Response.forbidden ctx msg
            | Ok user ->
                match MonthName.fromInt month with
                | Error msg -> Response.badRequest ctx (msg :> ITransformErrorToString).String
                | Ok month ->
                    let r = submitUserActivitiesForWeek
                                (activityTrackingService,
                                 activitySubmissionService,
                                 user,month,year,week)
                    match r with
                    | Error msg -> Response.badRequest ctx msg
                    | Ok _ -> Response.ok ctx String.Empty
    
    let apiRouter = router {
        getf "/api/say-hello/%s" greet

        postf "/api/login/%s" fakeLogin
        deletef "/api/login/%s" removeLogin
        
        post "/api/activities" createActivities
        get "/api/activities" getActivities
         
        deletef "/api/activity/%s" removeActivity
        
        getf "/api/monthly-tracking/%s/current" getUserCurrentTimeTracking
        getf "/api/monthly-tracking/%s/from/%s" getUserTimeTracking
        postf "/api/monthly-tracking/%s/month/%i/year/%i" saveActivities
        postf "/api/monthly-tracking/%s/month/%i/year/%i/week/%i/submit" submitActivities
    }

    let app = application {
        url "https://0.0.0.0:8080"
        use_router apiRouter
        service_config serviceConfig
        host_config (fun b -> b.ConfigureAppConfiguration(configureAppConfiguration))
        force_ssl
    }

    [<EntryPoint>]
    let main _ =
        run app
        0 // return an integer exit code