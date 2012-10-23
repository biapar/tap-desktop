﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheAirline.Model.AirportModel;

namespace TheAirline.Model.AirlineModel.SubsidiaryModel
{
    //the class for a future subsidiary airline for an airline
    public class FutureSubsidiaryAirline
    {
        public string Name { get; set; }
        public string IATA { get; set; }
        public Airline.AirlineFocus Market { get; set; }
        public Airline.AirlineMentality Mentality { get; set; }
        public Airport PreferedAirport { get; set; }
        public string Logo { get; set; }
        public FutureSubsidiaryAirline(string name, string iata,Airport airport, Airline.AirlineMentality mentality, Airline.AirlineFocus market, string logo)
        {
            this.Name = name;
            this.IATA = iata;
            this.PreferedAirport = airport;
            this.Mentality = mentality;
            this.Market = market;
            this.Logo = logo;
        }
    }
}
