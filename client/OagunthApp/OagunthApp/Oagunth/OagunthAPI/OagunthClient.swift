//
//  OagunthClient.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 22/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import Foundation
import Combine

enum MyOwnUnit {
    case Unit
}

protocol OagunthFetchable {
    func fetchActivities() -> AnyPublisher<[Activity],OagunthError>
    func fetchUserCalendarForCurrentMonth() -> AnyPublisher<MonthlyCalendar,OagunthError>
    func saveActivities(day:Date,logs:SaveActivities) -> AnyPublisher<MyOwnUnit,OagunthError>
    func submitActivities(month:Int,year:Int,weekNumber:Int) -> AnyPublisher<MyOwnUnit,OagunthError>
}

class OagunthClient : OagunthFetchable {
    
    func submitActivities(month:Int,year:Int,weekNumber:Int) -> AnyPublisher<MyOwnUnit,OagunthError> {
        guard let url = URL(string: "https://localhost:8080/api/monthly-tracking/essic@cellenza.fr/month/\(month)/year/\(year)/week/\(weekNumber)/submit") else {
            let error = OagunthError.network(desc: "Invalid date !")
            return Fail(error: error).eraseToAnyPublisher()
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        
        return
            session.dataTaskPublisher(for: request)
                .mapError { OagunthError.network(desc: $0.localizedDescription)}
                .print()
                .map { _ in MyOwnUnit.Unit }
        .print()
                .eraseToAnyPublisher()
    }
    
    func saveActivities(day: Date,logs:SaveActivities) -> AnyPublisher<MyOwnUnit,OagunthError>{
        
        let calendar = Foundation.Calendar.current
        let monthComponent = Foundation.Calendar.Component.month
        let yearComponent = Foundation.Calendar.Component.year
        let components = calendar.dateComponents([monthComponent,yearComponent], from: day)
        
        guard let month = components.month, let year = components.year else {
            let error = OagunthError.network(desc: "Invalid date !")
            return Fail(error: error).eraseToAnyPublisher()
        }
        
        guard let url = URL(string: "https://localhost:8080/api/monthly-tracking/essic@cellenza.fr/month/\(month)/year/\(year)") else {
            let error = OagunthError.network(desc: "Couldn't create URL")
            return Fail(error: error).eraseToAnyPublisher()
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        do {
            request.httpBody =
                try JSONEncoder().encode(logs)
        } catch {
            let error = OagunthError.parsing(desc: error.localizedDescription)
            return Fail(error: error).eraseToAnyPublisher()
        }
        
        return
            session.dataTaskPublisher(for: request)
                .mapError { OagunthError.network(desc: $0.localizedDescription)}
                .map { _ in MyOwnUnit.Unit }
                .eraseToAnyPublisher()
    }
    
    
    private let session: URLSession
    
    init(session: URLSession = .shared) {
        self.session = session
    }
    
    func fetchActivities() -> AnyPublisher<[Activity], OagunthError> {
        guard let url = URL(string: "https://localhost:8080/api/activities") else {
            let error = OagunthError.network(desc: "Couldn't create URL")
            return Fail(error: error).eraseToAnyPublisher()
        }
        return
            session.dataTaskPublisher(for: url)
                .mapError { OagunthError.network(desc: $0.localizedDescription)}
                .map { $0.data }
                .decode(type: [Activity].self, decoder: JSONDecoder() )
                .mapError { OagunthError.parsing(desc: $0.localizedDescription) }
                .eraseToAnyPublisher()
    }
    
    func fetchUserCalendarForCurrentMonth() -> AnyPublisher<MonthlyCalendar, OagunthError> {
        guard let url = URL(string: "https://localhost:8080/api/monthly-tracking/essic@cellenza.fr/current") else {
            let error = OagunthError.network(desc: "Couldn't create URL")
            return Fail(error: error).eraseToAnyPublisher()
        }
        
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        
        return
            session.dataTaskPublisher(for: url)
                .mapError { OagunthError.network(desc: $0.localizedDescription)}
                .map { $0.data }
                .decode(type: MonthlyCalendar.self, decoder: decoder )
                .mapError { OagunthError.parsing(desc: $0.localizedDescription) }
                .eraseToAnyPublisher()
    }
}

struct FakeClient : OagunthFetchable {
    func saveActivities(day: Date, logs: SaveActivities) -> AnyPublisher<MyOwnUnit, OagunthError> {
        let error = OagunthError.network(desc: "Not implemented")
        return Fail(error: error).eraseToAnyPublisher()
    }
    
    func submitActivities(month:Int,year:Int,weekNumber:Int) -> AnyPublisher<MyOwnUnit,OagunthError> {
        let error = OagunthError.network(desc: "Not implemented")
        return Fail(error: error).eraseToAnyPublisher()
    }
    
    func fetchActivities() -> AnyPublisher<[Activity], OagunthError> {
        let activitiesData : [Activity] = load("ActivitiesData.json")
        return Just(activitiesData)
            .mapError { _ in OagunthError.network(desc: "Not possible ")}
            .eraseToAnyPublisher()
    }
    
    func fetchUserCalendarForCurrentMonth() -> AnyPublisher<MonthlyCalendar, OagunthError> {
        let monthlyCalendarData : MonthlyCalendar = load("MonthlyCalendarData.json")
        return Just(monthlyCalendarData)
            .mapError { _ in OagunthError.network(desc: "Not possible !")}
            .eraseToAnyPublisher()
    }
}

private func load<T: Decodable>(_ filename: String) -> T {
    let data: Data
    
    guard let file = Bundle.main.url(forResource: filename, withExtension: nil)
        else {
            fatalError("Couldn't find \(filename) in main bundle.")
    }
    
    do {
        data = try Data(contentsOf: file)
    } catch {
        fatalError("Couldn't load \(filename) from main bundle:\n\(error)")
    }
    
    do {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try decoder.decode(T.self, from: data)
    } catch {
        fatalError("Couldn't parse \(filename) as \(T.self):\n\(error)")
    }
}
