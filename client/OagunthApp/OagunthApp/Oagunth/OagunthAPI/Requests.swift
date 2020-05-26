//
//  Requests.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 25/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import Foundation

struct Log : Codable {
    var time : Double
    var activityRef : UUID
}

struct SaveActivity : Codable {
    var day : Int
    var month : Int
    var year : Int
    var log : Log
}

struct SaveActivities : Codable {
    var days : [SaveActivity]
}
