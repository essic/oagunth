//
//  ActivityLogViewModel.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 24/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import Foundation
import Combine

class ActivityLogViewModel : Identifiable, ObservableObject {
    let id = UUID()
    
    private var log : ActivityLog
    private let getTotal : () -> Double
    
    let name : String
    
    @Published var time = 0.0 {
        didSet{
            log.time = time
        }
    }
    
    var activityId : UUID {
        log.activityId
    }
    
    init(log:ActivityLog, activityName:String, total: @escaping () -> Double ) {
        self.log = log
        self.name = activityName
        self.time = log.time
        self.getTotal = total
    }
}

extension ActivityLogViewModel {
    func increment() {
        let total = getTotal()
        
        if total + 0.25 <= 1 {
            time = time + 0.25
        }
        
    }
    
    func decrement() {
        let newTime = time - 0.25
        
        if newTime >= 0 {
            time = newTime
        }
    }
}
