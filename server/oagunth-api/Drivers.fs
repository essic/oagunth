module Drivers
open Oagunth.Core
open Oagunth.Core.Time
open Oagunth.Core.Cra
open NodaTime
open NodaTime.Extensions
open System

let rec private dirtySequence (x:Result<'a,'b> list) : Result<'a list, 'b> =
    match x with
    | (Error b)::_ -> Error b
    | [] -> Ok []
    | (Ok a)::xs -> dirtySequence xs |> Result.map (fun i -> a :: i )

let getUserCalendar
    (userService:IManageUser)
    (activityTrackingService:IHandleUserActivityTracking)
    (activitySubmissionPort: IHandleUserActivitySubmission)
    (username:string)
    (now: LocalDate option) =
        let now =
            match now with
            | Some x -> x
            | None -> SystemClock.Instance.InUtc().GetCurrentDate()
        let month = now |> MonthName.fromLocalDate
        let year = now.Year
        let calendar = MonthlyCalendar.from year month
        
        match userService.GetUser username with
        | Error msg -> Error msg
        | Ok user ->
            let rec getAllOrFail (weeks:WeeklyPeriod list) : Result<WeekAndStatus list,OagunthError>=
                match weeks with
                | [] -> [] |> Ok
                | item::xs ->
                    match activitySubmissionPort
                              .GetActivitiesStatus(user,month,year,item.Id.WeekNumber) with
                    | Error err -> Error err
                    | Ok status ->
                        getAllOrFail xs
                        |> Result.map
                               ( fun c ->
                                    { Id = item.Id
                                      Status = status } :: c )
                    
            getAllOrFail
                (calendar.GetWeeksInOrder()
                          |> Array.filter ( fun week -> week.BusinessDays.IsEmpty |> not )|> List.ofArray)
            |> Result.bind
                   (fun w ->
                        activityTrackingService.FetchUserActivities(user,month,year)
                        |> Result.map(fun x -> w,x) )
            |> Result.map (fun (w,r) -> r,user,w)
            |> Result.bind
                   ( fun (reply,user,w) ->
                        UserCalendarTracking.make user calendar reply w now )

let createActivities (activityManagerService:IReferenceActivities) (newActivities: string seq) =
    let newActivities = newActivities |> List.ofSeq
    match activityManagerService.GetAllActivities() with
    | Error msg -> Error msg
    | Ok allActivities ->
        let entries = newActivities |> Set.ofSeq
        let currentActivitiesName = allActivities |> Set.map (fun i -> i.Name)
        let commonActivities = Set.intersect entries currentActivitiesName
        
        if newActivities |> Seq.exists String.IsNullOrWhiteSpace
        then "Invalid activity name detected" |> OagunthError.outsideSingle |> Error
        elif entries.Count <> newActivities.Length
        then "Duplicates are forbidden !" |> OagunthError.outsideSingle |> Error
        elif commonActivities.IsEmpty |> not then
            commonActivities
            |> Set.toArray
            |> Array.reduce (fun state entry -> String.Join(";",state,entry))
            |> sprintf "Activities already exists [%s]"
            |> OagunthError.outsideSingle
            |> Error
        else
           activityManagerService.CreateActivities newActivities

let addUserActivities
    (userService:IManageUser)
    (activityTrackingService:IHandleUserActivityTracking)
    (activitySubmissionPort: IHandleUserActivitySubmission)
    (username:string)
    (month:Month)
    (year:Year)
    (userActivitiesToSave: (LocalDate * ActivitiesTrackedForOneDay) list) =
        let now = SystemClock.Instance.InUtc().GetCurrentDate()
        match month |> MonthName.fromInt, userService.GetUser(username) with
        | Error _, _ | _ , Error _ -> "Invalid input !" |> OagunthError.outsideSingle |> Error
        | Ok month, Ok user ->
            match activityTrackingService.FetchUserActivities(user,month,year) with
            | Error msg -> Error msg
            | Ok currentUserActivities ->
                let mutable newCurrentActivities = currentUserActivities
                userActivitiesToSave
                |> List.iter
                    ( fun (date,item) ->  newCurrentActivities <- Map.add date item newCurrentActivities)
                let calendar = MonthlyCalendar.from year month
                match UserCalendarTracking.make user calendar newCurrentActivities [] now with
                | Error error -> error |> sprintf "%A"  |> OagunthError.outsideSingle |> Error
                | Ok userCalendar ->
                    let weeksAndStatus =
                        userCalendar.GetCalendar().GetWeeksInOrder()
                        |> Seq.map
                               (fun item ->
                                    activitySubmissionPort
                                        .HasAlreadyActivitiesSubmittedOrValidated(user,month,year,item.Id.WeekNumber)
                                        |> Result.map (fun i -> item,i) ) |> Seq.toList       
                    match weeksAndStatus |> dirtySequence with
                    | Error e ->
                        (e :> ITransformErrorToString).String
                        |> sprintf "Can't control existing activities status (Network error ?) [%s]"
                        |> OagunthError.outsideSingle |> Error
                    | Ok weeksAndStatus ->
                        let userActivitiesToSave = userActivitiesToSave |> Map.ofList
                        let daysOfWeeksAndStatus =
                            weeksAndStatus
                            |> Seq.collect
                                ( fun (weeklyPeriod,isSubmittedOrValidated) ->
                                    weeklyPeriod.BusinessDays
                                    |> List.map (fun date -> date,isSubmittedOrValidated) )
                            |> Map.ofSeq

                        match
                            userActivitiesToSave
                            |> Seq.exists ( fun a -> daysOfWeeksAndStatus.[a.Key]) with
                        | true ->
                            "Cannot add activities on days already submitted or validated"
                            |> OagunthError.outsideSingle
                            |> Error
                        | false ->
                            activityTrackingService.InsertOrUpdateActivities
                                (user,month ,year, userCalendar.GetActivities() |> Map.toSeq |> Set.ofSeq)

let submitUserActivitiesForWeek
    (activityTrackingPort:IHandleUserActivityTracking,
     activitySubmissionPort: IHandleUserActivitySubmission,
     user:User, month:MonthName,year:Year,week:WeekNumber) =
        let now = SystemClock.Instance.InUtc().GetCurrentDate()
        match activitySubmissionPort.HasAlreadyActivitiesSubmittedOrValidated(user,month,year,week) with
        | Error msg -> Error msg
        | Ok true ->  "Activities already submitted or validated !" |> OagunthError.outsideSingle |> Error
        | Ok false ->
            let calendar = MonthlyCalendar.from year month
            match activityTrackingPort.FetchUserActivities(user,month,year) with
            | Error msg -> Error msg
            | Ok  currentUserActivities ->
            match UserCalendarTracking.make user calendar currentUserActivities [] now with
            | Error _ ->  "Could not retrieve user calendar!" |> OagunthError.outsideSingle |> Error
            | Ok userCalendar ->
            match userCalendar.GetCalendar().TryFindWeek(week) with
            | None ->  "Invalid week for given month !" |> OagunthError.outsideSingle |> Error
            | Some weeklyPeriod ->
                if weeklyPeriod.BusinessDays.IsEmpty
                then  "No working day exists for this week !" |> OagunthError.outsideSingle |> Error
                else
                    let allHasBeenTracked =
                        weeklyPeriod.BusinessDays
                        |> List.forall
                            ( fun date ->
                                match currentUserActivities.TryFind(date) with
                                | Some x when x.GetTimeTracked() = 1.<day> -> true
                                | _ -> false   )
                    if allHasBeenTracked then
                        activitySubmissionPort.Submit(user,month,year,week)
                    else "All has not been tracked !" |> OagunthError.outsideSingle |> Error