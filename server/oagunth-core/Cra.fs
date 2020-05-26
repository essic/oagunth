namespace Oagunth.Core

module Cra =
    open Time
    open NodaTime
    open System

    
    //This creates a unit of measure ...
    [<Measure>]
    type day

    //Now I do not have simple float to represent days but float<day> !
    let isValidForOneDay unitOfDay =
        if unitOfDay >= 0.<day> && unitOfDay % 0.25<day> = 0.<day> && unitOfDay <= 1.<day>
        then true
        else false

    type ActivityTrackingStatus =
        | New
        | Saved
        | Submitted
        | Validated

    type WeekAndStatus =
        { Id : WeekId
          Status : ActivityTrackingStatus }

    type Id<'a> = Id of Guid

    type Activity =
        { Reference: ActivityRef
          Name: String }
    and ActivityRef = Id<Activity>

    type Email =
        private E of string
        with
            member x.String =
                let (E value) = x
                value
                
    //Creating a module of the type, allows to functions under the type
    //A good way to avoid name collision, amongst other thing ...
    module Email =
        let make email =
            try
                System.Net.Mail.MailAddress(email) |> ignore
                email.Trim() |> E |> Ok
            with
                _ ->
                    email
                    |> sprintf "Invalid email address [%s]"
                    |> OagunthError.insideSingle
                    |> Error

    type User =
        { UserId: UserId
          UserName: Email }

    and UserId = Id<User>

    type ActivityTracking =
        { Reference: ActivityRef
          TimeLogged: float<day> }

    type ActivitiesTrackedForOneDay =
        { Activities: ActivityTracking list }
        with
            member x.GetTimeTracked() =
                x.Activities |> List.sumBy (fun a -> a.TimeLogged)

    module ActivitiesTrackedForOneDay =
        let mergeDuplicatedActivity (source:ActivitiesTrackedForOneDay) =
            let mergedActivities =
                source.Activities
                |> List.groupBy
                    ( fun item -> item.Reference )
                |> List.map
                    ( fun (ref,items) ->
                        {  Reference = ref
                           TimeLogged = items |> Seq.sumBy (fun item -> item.TimeLogged) } )
            { Activities = mergedActivities }
            
        let isValid (source:ActivitiesTrackedForOneDay) =
            source.Activities |> List.sumBy (fun item -> item.TimeLogged) |> isValidForOneDay

    type TotalTimeTrackedForOneDay =
        private
        | Invalid of LocalDate * float<day>
        | Valid of LocalDate * float<day>

    module TotalActivityTrackedForOneDay =
        let calculate (date: LocalDate, activities: ActivitiesTrackedForOneDay) =
            let total =
                activities.Activities |> List.sumBy (fun a -> a.TimeLogged)
            match isValidForOneDay total with
            | true -> Valid(date, total)
            | false -> Invalid(date, total)
            
        let popIt x =
            match x with
            | Invalid (date,day) | Valid (date,day) -> date,day

    type UserCalendarTracking' =
        { User: User
          MonthlyCalendar: MonthlyCalendar
          UserSelectedActivities: Map<LocalDate, ActivitiesTrackedForOneDay>
          WeeksStatus : WeekAndStatus list
          From : LocalDate }
    type UserCalendarTracking = private | Content of UserCalendarTracking'
        with
        member x.GetCalendar() =
            let (Content c) = x
            c.MonthlyCalendar
        member x.GetActivities() =
            let (Content c) = x
            c.UserSelectedActivities
        member x.GetUser() =
            let (Content c) = x
            c.User
        member x.GetFrom() =
            let (Content c) = x
            c.From
        member x.GetStatusInfo() =
            let (Content c) = x
            c.WeeksStatus

    type UserCalendarTrackingError =
        | Unknown of string
        //Behold ! Anonymous records !
        | DatesOutsideOfCalendarError of {| Month: MonthName; Msg : String |}
        | InvalidTimeTrackingError of {| Month : MonthName;  ActivitiesError : (LocalDate * float<day>) list |}
        | DuplicatedActivityTrackingDetected of {| Duplicates : (LocalDate*ActivityRef) list |}
        with
            //This is how we implement an interface !
            interface ITransformErrorToString with
                member x.String =
                    match x with
                    | DatesOutsideOfCalendarError err ->
                        let m = sprintf "Error while computing user calendar for %A" err.Month 
                        err.Msg
                        |> sprintf "%s -> [%s]" m
                    | Unknown msg -> msg
                    | DuplicatedActivityTrackingDetected err ->
                        let m = "Duplicated activities detected !"
                        err.Duplicates
                        |> sprintf "%s -> [%A]" m
                    | InvalidTimeTrackingError err ->
                        let m = sprintf "Irregularities on user activities detected !"
                        err.ActivitiesError
                        |> sprintf "%s -> [%A]" m
                    
    
    module UserCalendarTracking =
        let private checkDateIsInMonthBoundary
            (calendar: MonthlyCalendar)
            (activities: (LocalDate * ActivitiesTrackedForOneDay) list) =
            match activities with
            | [] -> None
            | _ ->
                let firstActivityDate,lastActivityDate =
                    activities |> List.head |> fst, (List.last >> fst) activities
                if calendar.IsMonth(firstActivityDate.Month) && calendar.IsMonth(lastActivityDate.Month)
                then None
                else
                    let (MonthlyCalendarMonth month) = calendar
                    {| Msg = sprintf "All activities must be within the month of %A" month
                       Month = month |} 
                    |> DatesOutsideOfCalendarError |> Some

        let private checkActivitiesTimeLoggedAreValid
            (calendar: MonthlyCalendar)
            (activities: (LocalDate * ActivitiesTrackedForOneDay) list) =
            let invalidActivities =
                activities
                |> List.map TotalActivityTrackedForOneDay.calculate
                |> List.filter (fun item ->
                    match item with
                    | Invalid (_, _) -> true
                    | _ -> false)
                |> List.map TotalActivityTrackedForOneDay.popIt
                
            if not <| invalidActivities.IsEmpty
            then
                let (MonthlyCalendarMonth month) = calendar
                {| Month = month; ActivitiesError = invalidActivities |}
                |> InvalidTimeTrackingError
                |> Some
            else None
        let private checkNoDuplicateActivityExistsOnDay (activities: (LocalDate * ActivitiesTrackedForOneDay) list) =
            let duplicatedActivitiesForOneDay =
                activities
                |> List.collect
                       ( fun (date,activities) ->
                            activities.Activities |> List.countBy (fun activity -> date,activity.Reference)
                       )
                |> List.filter
                    (fun (_,countOfActivity) -> countOfActivity > 1)
                |> List.map fst
            if not <| duplicatedActivitiesForOneDay.IsEmpty
            then Some <| DuplicatedActivityTrackingDetected {| Duplicates = duplicatedActivitiesForOneDay |}
            else None
     
        let make
            (user: User)
            (calendar: MonthlyCalendar)
            (activities: Map<LocalDate, ActivitiesTrackedForOneDay>)
            (weekStatus: WeekAndStatus list)
            (from: LocalDate ) =
            let activities' =
                activities |> Map.toList //This is ordered by LocalDate !

            let errors =
                [ checkActivitiesTimeLoggedAreValid calendar activities'
                  checkDateIsInMonthBoundary calendar activities'
                  checkNoDuplicateActivityExistsOnDay activities' ]
                |> List.choose (id)
                
            if errors.IsEmpty |> not
            then errors
                 |> Seq.map ( fun error -> (error :> ITransformErrorToString).String)
                 |> OagunthError.insideMany
                 |> Error
            else
                { User = user
                  MonthlyCalendar = calendar
                  UserSelectedActivities = activities
                  From = from
                  WeeksStatus = weekStatus }
                |> Content
                |> Ok
