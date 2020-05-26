module Adapters
open Oagunth.Core
open Oagunth.Core.Cra
open Oagunth.Core.Time
open MongoDB.Bson
open MongoDB.Driver
open System.Linq
open NodaTime
open System

type UserActivitySubmission(client:IMongoClient) =
    let submittedStatus = "submitted"
    let validatedStatus = "validated"
    let submissions =
        client
            .GetDatabase("oagunth-db-dev")
            .GetCollection<BsonDocument>("UserActivitySubmitted")
    
    let makeId user month year week =
        let (Id userId) = user.UserId
        let month = MonthName.toInt month
        sprintf "%A/%i-%i/%i" userId month year week
        
    interface IHandleUserActivitySubmission with
    
        member _.GetActivitiesStatus(user:User,month:MonthName,year:Year,week:WeekNumber) =
            try
                let _id = makeId user month year week
                let _idField = FieldDefinition<BsonDocument,string>.op_Implicit("_id")
                let c = Builders<BsonDocument>.Filter.Eq(_idField,_id)
                match submissions.Find(c).ToList() |> Seq.tryHead with
                | None -> ActivityTrackingStatus.New |> Ok
                | Some document ->
                    match document.GetValue("status").AsString with
                    | status when status = submittedStatus -> Ok ActivityTrackingStatus.Submitted
                    | status when status = validatedStatus -> Ok ActivityTrackingStatus.Validated
                    | unknown -> unknown |> sprintf "Unknown status %s"  |> OagunthError.outsideSingle |> Error
            with
                ex -> ex |> OagunthError.outsideSingleExn |> Error
        member x.Submit(user:User,month:MonthName,year:Year,week:WeekNumber)  =
            try
                let _id = makeId user month year week
                let document =
                    BsonDocument()
                        .Add("_id",BsonString(_id))
                        .Add("status",BsonString(submittedStatus))
                        .Add("onDate",BsonDateTime(DateTime.UtcNow))
                    
                submissions.InsertOne(document)
                Ok ()
            with
             ex -> OagunthError.outsideSingleExn ex |> Error
        
        member x.HasAlreadyActivitiesSubmittedOrValidated(user:User,month:MonthName,year:Year,week:WeekNumber) =
            try
                let _id = makeId user month year week
                let _idField = FieldDefinition<BsonDocument,string>.op_Implicit("_id")
                let c = Builders<BsonDocument>.Filter.Eq(_idField,_id)
                let count = submissions.CountDocuments(c)
                (count = (int64 1)) |> Ok
            with
            ex -> ex |> OagunthError.outsideSingleExn |> Error
    
type UserService(client:IMongoClient) =
    let users =
        client
            .GetDatabase("oagunth-db-dev")
            .GetCollection<BsonDocument>("Users")
        
    interface IManageUser with
        member x.CreateUser username =
            let document = BsonDocument()
            match username |>Email.make with
            | Ok email ->
                let newUserId = Guid.NewGuid().ToString()
                document
                    .Add("_id",BsonString(username)) 
                    .Add("userId",BsonString(newUserId))
                |> ignore
                try
                    users.InsertOne(document)
                    Ok <| { UserId = Guid.Parse(newUserId) |> Id ; UserName = email }
                with
                 ex -> ex |> OagunthError.outsideSingleExn |> Error
            | Error msg -> msg |> Error
            
        member x.GetUser username =
            let id = FieldDefinition<BsonDocument,string>.op_Implicit("_id")
            let c = Builders<BsonDocument>.Filter.Eq(id,username)
            match users.Find(c).ToList() |> Seq.tryHead with
            | None -> username |> sprintf "Cannot find [%s]" |> OagunthError.outsideSingle |> Error
            | Some item ->
                let userId : UserId = item.GetValue("userId").AsString |> Guid.Parse |> Id
                item.GetValue("_id").AsString
                |> Email.make
                |> Result.map (fun email -> { UserId = userId ; UserName = email })
        
        member x.DeleteUser username =
            let id = FieldDefinition<BsonDocument,string>.op_Implicit("_id")
            let c = Builders<BsonDocument>.Filter.Eq(id,username)
            try
                let result = users.DeleteOne(c)
                if result.DeletedCount <> (int64 0)
                then () |> Ok
                else username |> sprintf "Cannot delete [%s]" |> OagunthError.outsideSingle |> Error
            with
                ex -> ex |> OagunthError.outsideSingleExn |> Error
        
type UserTimeTrackingService(client:IMongoClient) =
    let userActivities =
        client
            .GetDatabase("oagunth-db-dev")
            .GetCollection("UserActivities")
            
    let makeId user month year =
       let (Id userId) = user.UserId
       sprintf "%A/%i-%i" userId (MonthName.toInt month) year
    
    let activityTrackingToBson (s:ActivityTracking) =
        let time = float s.TimeLogged
        let (Id id) = s.Reference
        BsonDocument()
            .Add("activityReference",BsonString(id.ToString()))
            .Add("time",BsonDouble(time))
    
    let activityTrackingFromBson (s:BsonDocument) =
        { TimeLogged = s.GetValue("time").AsDouble * 1.<day>
          Reference = s.GetValue("activityReference").AsString |> Guid.Parse |> Id }
           
    let toBson (date:LocalDate,activities:ActivitiesTrackedForOneDay) =
        let activitiesDocument =
            activities.Activities
            |> Seq.map activityTrackingToBson
            |> Seq.toArray
            |> BsonArray
        
        let bsonDate = date.AtMidnight().ToDateTimeUnspecified() |> BsonDateTime
        BsonDocument()
            .Add("dateOfDay",bsonDate)
            .Add("activitiesOfDay",activitiesDocument)
    
    let fromBson(s:BsonDocument) =
        let activitiesForDay =
            s.GetValue("activitiesOfDay")
                .AsBsonArray
            |> Seq.map
                (fun bsonValue ->
                    activityTrackingFromBson bsonValue.AsBsonDocument)
            |> Seq.toList
        let date =
            s.GetValue("dateOfDay")
                .AsBsonDateTime
                .ToLocalTime()
            |> LocalDateTime.FromDateTime
        date.Date, { Activities = activitiesForDay }
        
    
    let makeCriterionId value =
        let idField = FieldDefinition<BsonDocument,string>.op_Implicit("_id")
        Builders<BsonDocument>.Filter.Eq(idField,value) 

    interface IHandleUserActivityTracking with
        member x.FetchUserActivities(user,month,year) =
            try
                let _id = makeId user month year
                let c = makeCriterionId _id
                match userActivities.Find(c).ToList() |> Seq.tryHead with
                | None -> [] |> Map.ofList
                | Some document ->
                    document.GetValue("allDays")
                        .AsBsonArray
                    |> Seq.map ( fun doc -> doc.AsBsonDocument |> fromBson)
                    |> Map.ofSeq
                |> Ok
            with
                ex -> ex |> OagunthError.outsideSingleExn |> Error
        
        member x.InsertOrUpdateActivities(user,month,year,data) =
            let idValue = makeId user month year
            let c = makeCriterionId idValue
            match userActivities.Find(c).ToList() |> Seq.tryHead with
            | None ->
                let activities =
                    data
                    |> Seq.map toBson
                    |> Seq.toArray
                    |> BsonArray
                let document =
                    BsonDocument()
                        .Add("_id",BsonString(idValue))
                        .Add("allDays",activities)
                userActivities.InsertOne(document)
                () |> Ok
            | Some _ ->
                let update =
                    let value =
                        data
                        |> Seq.map toBson
                        |> BsonArray
                    let allDaysField = FieldDefinition<BsonDocument,BsonArray>.op_Implicit("allDays")
                    Builders<BsonDocument>.Update.Set(allDaysField,value)
                userActivities.UpdateOne(c,update) |> ignore
                Ok ()
            
type ActivityReferenceService(client:IMongoClient) =
    let activities =
        client
            .GetDatabase("oagunth-db-dev")
            .GetCollection<BsonDocument>("Activities")
    
    let toBson (source:Activity) =
        let (Id id) = source.Reference
        BsonDocument()
            .Add("referenceId",BsonString(id.ToString()))
            .Add("activityName",BsonString(source.Name))

    let fromBson (source:BsonDocument) =
        let activityId : ActivityRef = source.GetValue("referenceId").AsString |> Guid.Parse |> Id
        let activityName = source.GetValue("activityName").AsString
        { Reference = activityId ; Name = activityName }
        
    let allActivities() =
        activities
            .AsQueryable()
            .ToArray()
            |> Array.map fromBson
            |> Set.ofSeq

    interface IReferenceActivities with
        member x.CreateActivities newActivities =
            try
               let newActivities =
                   newActivities
                   |> Seq.map (fun name -> {Name = name; Reference =  Guid.NewGuid() |> Id})
                   |> Seq.toList
               let documents =
                   newActivities |> List.map toBson
               activities.InsertMany(documents)
               newActivities |> Ok
            with
                ex -> ex |> OagunthError.outsideSingleExn |> Error
        
        member x.RemoveActivityWithName name =
            try
                let nameField = FieldDefinition<BsonDocument,string>.op_Implicit("activityName")
                let c = Builders<BsonDocument>.Filter.Eq(nameField, name)
                let result = activities.DeleteOne(c)
                if result.DeletedCount <> (int64 0)
                then () |> Ok
                else name |> sprintf "Cannot delete [%s]" |> OagunthError.outsideSingle |> Error
            with
                ex -> ex |> OagunthError.outsideSingleExn |> Error

        member x.GetAllActivities() =
            try allActivities() |> Set.ofSeq |> Ok with ex -> ex |> OagunthError.outsideSingleExn |> Error