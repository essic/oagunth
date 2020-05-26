//
//  ActivityLogView.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 24/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI
import Combine

struct ActivityLogView: View {
    @ObservedObject var viewModel : ActivityLogViewModel
    
    init(viewModel: ActivityLogViewModel) {
        self.viewModel = viewModel
    }
    
    var body: some View {
        VStack {
            Divider()
            
            Text(viewModel.name)
                .font(.body)
                .fontWeight(.bold)
            
            HStack {
                Button(action: {self.viewModel.decrement()}) {
                    Text("-")
                        .font(.title)
                }
                
                Text("\(self.viewModel.time, specifier: "%.2f")")
                    .fontWeight(.light)
                
                Button(action: {self.viewModel.increment()}) {
                    Text("+")
                        .font(.title)
                }
            }
        }.padding()
    }
}

#if DEBUG
struct ActivityLogView_Previews: PreviewProvider {
    static let vm = ActivityLogViewModel(log: ActivityLog(activityId: UUID(), time: 1.0, date: Date()), activityName: "Some Company", total: { 1.0 })
    static var previews: some View {
        ActivityLogView(viewModel: vm)
    }
}
#endif
