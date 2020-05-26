//
//  OagunthError.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 22/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import Foundation

enum OagunthError : Error {
    case parsing(desc: String)
    case network(desc: String)
}
