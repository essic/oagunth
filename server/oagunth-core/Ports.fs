namespace Oagunth.Core
module Ports =
    open Time
    open Cra
    open NodaTime
    
    type IHandleUserActivitySubmission =
        abstract member Submit:
            user:User * month:MonthName * year:Year * week:WeekNumber ->Result<Unit,string>
        
        abstract member HasAlreadyActivitiesSubmittedOrValidated:
            user:User * month:MonthName * year:Year * week:WeekNumber -> Result<bool,string>
    
        abstract member GetActivitiesStatus:
            user:User * month:MonthName * year:Year * week:WeekNumber -> Result<ActivityTrackingStatus,string>
    
    type IHandleUserActivityTracking =
        abstract member FetchUserActivities:
            user:User * month:MonthName * year:Year -> Result<Map<LocalDate,ActivitiesTrackedForOneDay>,string>
            
        abstract member InsertOrUpdateActivities:
            user: User * month: MonthName * year: Year * data: Set<LocalDate * ActivitiesTrackedForOneDay> -> Result<Unit,string>
            
    type IManageUser =
        abstract member CreateUser :
            username:string -> Result<User,string>
        abstract member GetUser :
            username:string -> Result<User,string>
        abstract member DeleteUser :
            username:string -> Result<Unit,string>
            
    type IReferenceActivities =
        abstract member CreateActivities :
            activities: string seq -> Result<Activity list,string>
        abstract member RemoveActivityWithName :
            activityName : string -> Result<Unit,string>
        abstract member GetAllActivities :
            Unit -> Result<Set<Activity>,string> 