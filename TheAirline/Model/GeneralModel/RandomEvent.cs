﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheAirline.Model.AirportModel;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.AirlineModel;
using TheAirline.Model.AirlinerModel.RouteModel;
using TheAirline.Model.GeneralModel;
using TheAirline.Model.GeneralModel.StatisticsModel;
using TheAirline.Model.GeneralModel.InvoicesModel;
using TheAirline.Model.AirlineModel.SubsidiaryModel;
using TheAirline.Model.PilotModel;

namespace TheAirline.Model.GeneralModel
{
    [Serializable]
    public class RandomEvent
    {
        public enum EventType { Safety, Security, Maintenance, Customer, Employee, Political }
        public EventType Type { get; set; }
        public Airline Airline { get; set; }
        public string EventName { get; set; }
        public string EventMessage { get; set; }
        public FleetAirliner Airliner { get; set; }
        public Airport Airport { get; set; }
        public Country Country { get; set; }
        public Route Route { get; set; }
        public bool CriticalEvent { get; set; }
        public DateTime DateOccurred { get; set; }
        public int CustomerHappinessEffect { get; set; } //0-100
        public int AircraftDamageEffect { get; set; } //0-100
        public int AirlineSecurityEffect { get; set; } //0-100
        public int AirlineSafetyEffect { get; set; } //0-100
        public int EmployeeHappinessEffect { get; set; } //0-100
        public int FinancialPenalty { get; set; } //dollar amount to be added or subtracted from airline cash
        public double PaxDemandEffect { get; set; } //0-2
        public double CargoDemandEffect { get; set; } //0-2
        public int EffectLength { get; set; } //should be defined in months
        public string EventID { get; set; }
        public int Frequency { get; set; } //frequency per 3 years
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public RandomEvent(EventType type, string name, string message, bool critical, int custHappiness, int aircraftDamage, int airlineSecurity, int airlineSafety, int empHappiness, int moneyEffect, double paxDemand, double cargoDemand, int length, string id, int frequency, DateTime stat, DateTime end)
        {

            this.DateOccurred = GameObject.GetInstance().GameTime;
            this.CustomerHappinessEffect = 0;
            this.AircraftDamageEffect = 0;
            this.AirlineSecurityEffect = 0;
            this.EmployeeHappinessEffect = 0;
            this.FinancialPenalty = 0;
            this.PaxDemandEffect = 1;
            this.CargoDemandEffect = 1;
            this.EffectLength = 1;
            this.CriticalEvent = false;
            this.EventName = "";
            this.EventMessage = "";
            this.Type = type;

         
            this.EventID = id;
        }

        //applies the effects of an event
        public void ExecuteEvents(Airline airline, DateTime time) 
        {
            Random rnd = new Random();
            foreach (RandomEvent rEvent in airline.EventLog.Values)
            {
                if (rEvent.DateOccurred.DayOfYear == time.DayOfYear)
                {
                    rEvent.Airliner.Airliner.Damaged += AircraftDamageEffect;
                    airline.Money += rEvent.FinancialPenalty;
                    airline.scoresCHR.Add(rEvent.CustomerHappinessEffect);
                    airline.scoresEHR.Add(rEvent.EmployeeHappinessEffect);
                    airline.scoresSafety.Add(rEvent.AirlineSafetyEffect);
                    airline.scoresSecurity.Add(rEvent.AirlineSecurityEffect);
                    PassengerHelpers.ChangePaxDemand(airline, (rEvent.PaxDemandEffect * rnd.Next(9, 11) / 10));
                }
            }
        }

        //returns a list of proportions of events based on current ratings
        public static List<double> GetEventProportions(Airline airline)
        {
            //chr 0 ehr 1 scr 2 sfr 3 total 4
            List<int> ratings = new List<int>();
            ratings.Add(100 - airline.CustomerHappinessRating);
            ratings.Add(100 - airline.EmployeeHappinessRating);
            ratings.Add(100 - airline.SecurityRating);
            ratings.Add(100 - airline.SafetyRating);
            ratings.Add(100 - airline.MaintenanceRating);
            ratings.Add(500 - ratings.Sum());
            double pCHR = ratings[0] / ratings[5];
            double pEHR = ratings[1] / ratings[5];
            double pSCR = ratings[2] / ratings[5];
            double pSFR = ratings[3] / ratings[5];
            double pMTR = ratings[4] / ratings[5];
            ratings.Clear();
            List<double> pRatings = new List<double>();
            pRatings.Add(pCHR);
            pRatings.Add(pEHR);
            pRatings.Add(pSCR);
            pRatings.Add(pSFR);
            pRatings.Add(pMTR);
            return pRatings;
            
            
        }

        //generates x number of events for each event type for the current year. Should be called only from OnNewYear
        public static void GenerateEvents(Airline airline)
        {
            Random rnd = new Random();
            Dictionary<RandomEvent.EventType, double> eventOccurences = new Dictionary<EventType, double>();
            int eFreq = 0;
            double secEvents;
            double safEvents;
            double polEvents;
            double maintEvents;
            double custEvents;
            double empEvents;

            //sets an overall event frequency based on an airlines total overall rating
            int totalRating = airline.CustomerHappinessRating + airline.EmployeeHappinessRating + airline.SafetyRating + airline.SecurityRating;
            if (totalRating < 300)
            {
                eFreq = (int)rnd.Next(1, 6);
            }
            else if (totalRating < 200)
            {
                eFreq = (int)rnd.Next(4, 10);
            }
            else if (totalRating < 100)
            {
                eFreq = (int)rnd.Next(8, 16);
            }
            else eFreq = (int)rnd.Next(0, 4);

            //gets the event proportions and multiplies them by total # events to get events per type
            List<double> probs = GetEventProportions(airline);
            custEvents = (int)eFreq * probs[0];
            empEvents = (int)eFreq * probs[1];
            secEvents = (int)eFreq * probs[2];
            safEvents = (int)eFreq * probs[3];
            maintEvents = (int)eFreq * probs[4];
            polEvents = eFreq - custEvents - empEvents - secEvents - maintEvents;
            eventOccurences.Add(RandomEvent.EventType.Customer, custEvents);
            eventOccurences.Add(RandomEvent.EventType.Employee, empEvents);
            eventOccurences.Add(RandomEvent.EventType.Maintenance, maintEvents);
            eventOccurences.Add(RandomEvent.EventType.Safety, safEvents);
            eventOccurences.Add(RandomEvent.EventType.Security, secEvents);
            eventOccurences.Add(RandomEvent.EventType.Political, polEvents);

            //generates the given number of events for each type
            foreach (KeyValuePair<RandomEvent.EventType, double> v in eventOccurences)
            {
                int k = (int)v.Value;
                List<RandomEvent> list = RandomEvents.GetEvents(v.Key, k, airline);
                foreach (RandomEvent e in list)
                {
                    airline.EventLog.Add(e.EventID, e);
                }
            }


        }

        /*public RandomEvent GenerateRandomEvent()
        {
            //code needed

        }*/

        //adds an event to an airline's event log
        public void AddEvent(Airline airline, RandomEvent rEvent)
        {
            airline.EventLog.Add(rEvent.EventID, rEvent);
        }


        //removes an event from the airlines event log
        public static void RemoveEvent(Airline airline, RandomEvent rEvent)
        {
            airline.EventLog.Remove(rEvent.EventID);
        }


        //checks if an event's effects are expired
        public static void CheckExpired(DateTime expDate)
        {
            foreach (Airline airline in Airlines.GetAllAirlines())
            {
                foreach (RandomEvent rEvent in airline.EventLog.Values)
                {
                    expDate = GameObject.GetInstance().GameTime.AddMonths(rEvent.EffectLength);
                    if (expDate < GameObject.GetInstance().GameTime)
                    {
                        PassengerHelpers.ChangePaxDemand(airline, (1 / rEvent.PaxDemandEffect));
                        RemoveEvent(airline, rEvent);
                    }  }  }
        }
    }

    public class RandomEvents
    {
        private static Dictionary<string, RandomEvent> events = new Dictionary<string, RandomEvent>();

        public static void Clear()
        {
            events = new Dictionary<string, RandomEvent>();
        }

        public static void AddEvent(RandomEvent rEvent)
        {
            events.Add(rEvent.EventName, rEvent);
        }

        //gets a single event by name
        public static RandomEvent GetEvent(string name)
        {
            return events[name];
        }


        //gets a list of all events
        public static List<RandomEvent> GetEvents()
        {
            return events.Values.ToList();
        }

        //gets all events of a given type
        public static List<RandomEvent> GetEvents(RandomEvent.EventType type)
        {
            return GetEvents().FindAll((delegate(RandomEvent rEvent) {return rEvent.Type ==type; }));
        }

        //gets x number of random events of a given type
        public static List<RandomEvent> GetEvents(RandomEvent.EventType type, int number, Airline airline)
        {
            Random rnd = new Random();
            Dictionary<int,RandomEvent> rEvents = new Dictionary<int,RandomEvent>();
            List<RandomEvent> tEvents = GetEvents(type);
            int i = 1;
            int j = 0;
            foreach (RandomEvent r in tEvents)
                if (r.Start <= GameObject.GetInstance().GameTime && r.End >= GameObject.GetInstance().GameTime)
                {
                    {
                        r.DateOccurred = MathHelpers.GetRandomDate(GameObject.GetInstance().GameTime, GameObject.GetInstance().GameTime.AddMonths(12));
                        r.Airline = airline;
                        r.Airliner = Helpers.AirlinerHelpers.GetRandomAirliner(airline);
                        r.Route = r.Airliner.Routes[rnd.Next(r.Airliner.Routes.Count())];
                        r.Country = r.Route.Destination1.Profile.Country;
                        r.Airport = r.Route.Destination1;
                        rEvents.Add(i, r);
                        i++;
                    }
                }

            tEvents.Clear();

            while (j < number)
            {
                int item = rnd.Next(rEvents.Count());
                tEvents.Add(rEvents[item]);
                j++;
            }

            return tEvents;
        }

    }


}