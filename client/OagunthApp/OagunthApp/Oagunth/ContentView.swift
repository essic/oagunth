//
//  ContentView.swift
//  OagunthApp
//
//  Created by Aly-Bocar Cisse on 22/05/2020.
//  Copyright © 2020 Aly-Bocar Cisse. All rights reserved.
//

import SwiftUI

struct ContentView: View {
    let userMonthlyCalendarViewModel : UserMonthlyCalendarViewModel
    
    var body: some View {
        NavigationView{
            VStack(alignment: .leading) {
                NavigationLink(destination: EmptyView()) {
                    Text("Current week")
                        .font(.largeTitle)
                }
                Spacer()
                NavigationLink(destination: UserMonthlyCalendarView(viewModel: userMonthlyCalendarViewModel)){
                    Text("Current month")
                        .font(.largeTitle)
                }
                Spacer()
                NavigationLink(destination: EmptyView()){
                    Text("Search ...")
                        .font(.largeTitle)
                }
                Spacer()
            }.padding()
        }.navigationBarTitle("Menu")
    }
    
    init() {
        self.userMonthlyCalendarViewModel =
            UserMonthlyCalendarViewModel(api: OagunthClient())
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
    }
}


