//
//  UserDailyCalendarViewModel.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 23/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI
import Combine

class UserDailyCalendarViewModel : Identifiable, ObservableObject {
    let id = UUID()
    let day : Date
    private let activities : [Activity]
    private var disposables = Set<AnyCancellable>()
    @Published var dataSource : [ActivityLogViewModel] = []
    @Published var total = 0.0
    @Published var isActive = true
    
    var dateString : String {
        
        let fmt = DateFormatter()
        fmt.dateFormat = "E d MMMM YYYY"
        return fmt.string(from: day)
    }
    
    func getLogs() -> [ActivityLog] {
        self.dataSource.map { ActivityLog(activityId: $0.activityId, time: $0.time, date: day)}
    }
    
    var remainingActivities : [Activity] {
        let activitiesSelected = dataSource.map { $0.activityId }
        return activities
            .filter { (!activitiesSelected.contains($0.id))}
    }
    
    func addActivity(activity: Activity) {
        let log = ActivityLog(activityId: activity.id, time: 0.0, date: day)
        let vm = ActivityLogViewModel(log: log, activityName: activity.name, total: {self.total})
        vm.$time
            .receive(on: RunLoop.main)
            .sink(receiveValue: { _ in
                self.refresh()
            })
            .store(in: &disposables)
        dataSource.append(vm)
        self.refresh()
    }
    
    func removeLog(log: ActivityLogViewModel) {
        guard let toRemove = self.dataSource.firstIndex(where: { $0.activityId == log.activityId }) else {
            return
        }
        
        self.dataSource.remove(at: toRemove)
        self.refresh()
    }
    
    func refresh() {
        self.total = self.dataSource.reduce(0.0, {acc,item in item.time + acc})
    }
    
    init(day:Date, logs:[ActivityLog], activities:[Activity]) {
        self.day = day
        self.activities = activities
        for log in logs {
            //TODO: include name where it should ! For now let it fail !
            let name = activities.first(where: {$0.id == log.activityId})!.name
            let vm = ActivityLogViewModel(log:log,activityName: name, total: { self.total } )
            vm.$time
                .receive(on: RunLoop.main)
                .sink(receiveValue: { _ in
                    self.refresh()
                })
                .store(in: &disposables)
            dataSource.append(vm)
        }
        self.refresh()
    }
}
