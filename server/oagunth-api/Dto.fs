module Dto
open System
open System.Globalization
open Oagunth.Core.Cra
open Oagunth.Core.Time
open NodaTime

//This is a class !
type ActivityDto(id,name) =
    member val Id : Guid = id with get,set
    member val Name : String = name with get,set
    
    new() =
       ActivityDto(Guid.Empty,String.Empty)
    new(source: Activity) =
        let (Id id) = source.Reference
        ActivityDto(id,source.Name)

type ActivityTrackedDto(time,date:LocalDate,activityId) =
    member val ActivityId : Guid = activityId with get,set
    member val Time : float = time with get,set
    member val Date : DateTime =
        date.AtMidnight().ToDateTimeUnspecified().ToUniversalTime()
        with get,set
    
    new() =
       ActivityTrackedDto(0.0,LocalDate(),Guid.Empty)
 
 type WeeklyCalendarDto(week:WeeklyPeriod) =
     member val WeekNumber = week.Id.WeekNumber with get
     member val WeekYear = week.Id.WeekYear with get
     member val Days =
         week.BusinessDays
         |> Seq.map (fun d -> d.AtMidnight().ToDateTimeUnspecified().ToUniversalTime())
         |> Seq.toArray with get
   
 type WeekAndStatusDto(x:WeekAndStatus) =
     member val WeekYear =
         x.Id.WeekYear with get
     member val WeekNumber =
         x.Id.WeekNumber with get
     member val Status =
         x.Status |> sprintf "%A" with get
    
 type MonthlyCalendarDto(calendar:MonthlyCalendar,date:LocalDate) =
     member val CurrentWeekYear =
        match calendar.GetCurrentWeek(date) with
        | Some x -> x.WeekYear
        | _ -> 0
        with get
     member val CurrentWeekNumber =
        match calendar.GetCurrentWeek(date) with
        | Some x -> x.WeekNumber
        | _ -> 0
        with get
     member val Weeks =
        calendar.GetWeeksInOrder() |> Array.map WeeklyCalendarDto with get        
 type UserDto(user:User) =
     member val Username : String = user.UserName.String with get
     member val UserId : String =
         let (Id userId) = user.UserId
         userId.ToString()
         with get
 type UserCalendarTrackingDto(data:UserCalendarTracking) =
     member val CurrentDate =
         data.GetFrom().ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
         with get
     member val Activities : ActivityTrackedDto[] =
         data.GetActivities()
         |> Map.toSeq
         |> Seq.collect
            ( fun (date,v) ->
                v.Activities
                |> List.map
                    (fun activity ->
                        let (Id activityId) = activity.Reference
                        ActivityTrackedDto(float activity.TimeLogged,date,activityId) )
            )
         |> Seq.toArray
         with get
     member val Calendar : MonthlyCalendarDto =
         MonthlyCalendarDto(data.GetCalendar(),data.GetFrom())
         with get
     
     member val AllWeeksStatus =
        data.GetStatusInfo() |> List.map WeekAndStatusDto with get

type [<CLIMutable>] AddRequest =
    { Days : ActivityForTheDayReq array }
and [<CLIMutable>] ActivityForTheDayReq =
    { Day : int
      Month : int
      Year : int
      Log : TimeTrackingReq }
    with
    member x.ToActivityTracking() =
        { ActivityTracking.Reference =  x.Log.ActivityRef |> Id
          ActivityTracking.TimeLogged = x.Log.Time * 1.<day> }
and [<CLIMutable>] TimeTrackingReq =
    { Time : float
      ActivityRef : Guid }
    
module AddRequest =
    let transform (req:AddRequest) =
        req.Days
        |> Array.groupBy ( fun item -> LocalDate(item.Year,item.Month,item.Day))
        |> Array.map
            ( fun (date,items) ->
                let forTheDay =
                    items
                    |> Seq.map ( fun item -> item.ToActivityTracking())
                    |> Seq.toList
                date, { ActivitiesTrackedForOneDay.Activities = forTheDay } )
        |> Array.toList