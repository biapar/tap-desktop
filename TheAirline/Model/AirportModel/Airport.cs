﻿namespace TheAirline.Model.AirportModel
{
    using System;
    using System.Collections.Generic;
    using System.Device.Location;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using TheAirline.Model.AirlineModel;
    using TheAirline.Model.AirlineModel.AirlineCooperationModel;
    using TheAirline.Model.AirlinerModel;
    using TheAirline.Model.AirlinerModel.RouteModel;
    using TheAirline.Model.GeneralModel;
    using TheAirline.Model.GeneralModel.Helpers;
    using TheAirline.Model.GeneralModel.WeatherModel;
    using TheAirline.Model.PassengerModel;

    [Serializable]
    //the class for an airport
    public class Airport : ISerializable
    {
        #region Fields

        [Versioning("facilities")]
        private List<AirlineAirportFacility> Facilities;

        [Versioning("contracts")]
        private List<AirportContract> _Contracts;

        private List<Hub> _Hubs;

        #endregion

        #region Constructors and Destructors

        public Airport(AirportProfile profile)
        {
            this.Profile = profile;
            this.Income = 0;
            this.DestinationPassengers = new List<DestinationDemand>();
            this.DestinationCargo = new List<DestinationDemand>();
            this.Facilities = new List<AirlineAirportFacility>();
            this.Cooperations = new List<Cooperation>();
            this.Statistics = new AirportStatistics();
            this.Weather = new List<Weather>();
            this.Terminals = new Terminals(this);
            this.Runways = new List<Runway>();
            this._Hubs = new List<Hub>();
            this.DestinationPassengerStatistics = new Dictionary<Airport, long>();
            this.DestinationCargoStatistics = new Dictionary<Airport, double>();
            this.LastExpansionDate = new DateTime(1900, 1, 1);
            this.Statics = new AirportStatics(this);
            this.AirlineContracts = new List<AirportContract>();
            this.Demand = new AirportDemand(this);
        }

        private Airport(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt16("version");

            IEnumerable<FieldInfo> fields =
                this.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    this.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            IEnumerable<MemberInfo> propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (SerializationEntry entry in info)
            {
                MemberInfo prop =
                    propsAndFields.FirstOrDefault(
                        p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Name == entry.Name);

                if (prop != null)
                {
                    if (prop is FieldInfo)
                    {
                        ((FieldInfo)prop).SetValue(this, entry.Value);
                    }
                    else
                    {
                        if (entry.Name != "weather")
                            ((PropertyInfo)prop).SetValue(this, entry.Value);
                    }
                }
            }

            IEnumerable<MemberInfo> notSetProps =
                propsAndFields.Where(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Version > version);

            foreach (MemberInfo notSet in notSetProps)
            {
                var ver = (Versioning)notSet.GetCustomAttribute(typeof(Versioning));

                if (ver.AutoGenerated)
                {
                    if (notSet is FieldInfo)
                    {
                        ((FieldInfo)notSet).SetValue(this, ver.DefaultValue);
                    }
                    else
                    {
                        ((PropertyInfo)notSet).SetValue(this, ver.DefaultValue);
                    }
                }
            }

            if (version < 3)
                this.LandingFee = AirportHelpers.GetStandardLandingFee(this);

            if (this.Weather == null)
            { 
                this.Weather = new List<Weather>();

                AirportHelpers.CreateAirportWeather(this);
            }

            if (this.Weather.Contains(null))
            {
                AirportHelpers.CreateAirportWeather(this);
            }

            this.Statics = new AirportStatics(this);
           
        }

        #endregion

        #region Public Properties

        public List<AirportContract> AirlineContracts
        {
            get
            {
                return this.getAirlineContracts();
            }
            set
            {
                this._Contracts = value;
            }
        }

        [Versioning("cooperations", Version = 2)]
        public List<Cooperation> Cooperations { get; set; }

        [Versioning("hubs")]
        public List<Hub> Hubs
        {
            private get
            {
                return this.getHubs();
            }
            set
            {
                this._Hubs = value;
            }
        }

        [Versioning("income")]
        public long Income { get; set; }

        public Boolean IsHub
        {
            get
            {
                return this.getHubs().Count > 0;
            }
            set
            {
                ;
            }
        }

        public AirportDemand Demand { get; set; }
        [Versioning("lastexpansiondate")]
        public DateTime LastExpansionDate { get; set; }

        [Versioning("profile")]
        public AirportProfile Profile { get; set; }

        [Versioning("runways")]
        public List<Runway> Runways { get; set; }

        public AirportStatics Statics { get; set; }

        [Versioning("statistics")]
        public AirportStatistics Statistics { get; set; }

        [Versioning("terminals")]
        public Terminals Terminals { get; set; }

        [Versioning("weather")]
        public List<Weather> Weather { get; set; }

        [Versioning("landingfee",Version=3)]
        public double LandingFee { get; set; }
        #endregion

        #region Properties

        [Versioning("destinationcargo")]
        private List<DestinationDemand> DestinationCargo { get; set; }

        [Versioning("cargostatistics")]
        private Dictionary<Airport, double> DestinationCargoStatistics { get; set; }

        [Versioning("passengerstatistics")]
        private Dictionary<Airport, long> DestinationPassengerStatistics { get; set; }

        [Versioning("destinationpassengers")]
        private List<DestinationDemand> DestinationPassengers { get; set; }

        #endregion

        #region Public Methods and Operators

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 3);

            Type myType = this.GetType();

            IEnumerable<FieldInfo> fields =
                myType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    myType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            IEnumerable<MemberInfo> propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (MemberInfo member in propsAndFields)
            {
                object propValue;

                if (member is FieldInfo)
                {
                    propValue = ((FieldInfo)member).GetValue(this);
                }
                else
                {
                    propValue = ((PropertyInfo)member).GetValue(this, null);
                }

                var att = (Versioning)member.GetCustomAttribute(typeof(Versioning));

                info.AddValue(att.Name, propValue);
            }
            
            
      
            
        }

        public void addAirlineContract(AirportContract contract)
        {
            lock (this._Contracts)
            {
                this._Contracts.Add(contract);
            }

            if (!contract.Airline.Airports.Contains(this))
            {
                contract.Airline.addAirport(this);
            }
        }

        public void addAirportFacility(Airline airline, AirportFacility facility, DateTime finishedDate)
        {
            //this.Facilities.RemoveAll(f => f.Airline == airline && f.Facility.Type == facility.Type);
            this.Facilities.Add(new AirlineAirportFacility(airline, this, facility, finishedDate));
        }

        //sets the facility for an airline
        public void addAirportFacility(AirlineAirportFacility facility)
        {
            //this.Facilities.RemoveAll(f => f.Airline == facility.Airline && f.Facility.Type == facility.Facility.Type);
            this.Facilities.Add(facility);
        }

        public void addCargoDestinationStatistics(Airport destination, double cargo)
        {
            lock (this.DestinationCargoStatistics)
            {
                if (!this.DestinationCargoStatistics.ContainsKey(destination))
                {
                    this.DestinationCargoStatistics.Add(destination, cargo);
                }
                else
                {
                    this.DestinationCargoStatistics[destination] += cargo;
                }
            }
        }

        public void addCooperation(Cooperation cooperation)
        {
            this.Cooperations.Add(cooperation);
        }

        public void addDestinationCargoRate(DestinationDemand cargo)
        {
            this.Statics.addCargoDemand(cargo);
        }

        public void addDestinationCargoRate(Airport destination, ushort rate)
        {
            lock (this.DestinationCargo)
            {
                DestinationDemand destinationCargo = this.getDestinationCargoObject(destination);

                if (destinationCargo != null)
                {
                    destinationCargo.Rate += rate;
                }
                else
                {
                    this.DestinationCargo.Add(new DestinationDemand(destination.Profile.IATACode, rate));
                }
            }
        }

        public void addDestinationPassengersRate(DestinationDemand passengers)
        {
            this.Statics.addPassengerDemand(passengers);
        }

        public void addDestinationPassengersRate(Airport destination, ushort rate)
        {
            lock (this.DestinationPassengers)
            {
                DestinationDemand destinationPassengers = this.getDestinationPassengersObject(destination);

                if (destinationPassengers != null)
                {
                    destinationPassengers.Rate += rate;
                }
                else
                {
                    this.DestinationPassengers.Add(new DestinationDemand(destination.Profile.IATACode, rate));
                }
            }
        }
      
        public void addHub(Hub hub)
        {
            this._Hubs.Add(hub);
        }

        //adds a major destination to the airport
        public void addMajorDestination(string destination, int pax)
        {
            if (this.Profile.MajorDestionations.ContainsKey(destination))
            {
                this.Profile.MajorDestionations[destination] += pax;
            }
            else
            {
                this.Profile.MajorDestionations.Add(destination, pax);
            }
        }

        public void addPassengerDestinationStatistics(Airport destination, long passengers)
        {
            lock (this.DestinationPassengerStatistics)
            {
                if (!this.DestinationPassengerStatistics.ContainsKey(destination))
                {
                    this.DestinationPassengerStatistics.Add(destination, passengers);
                }
                else
                {
                    this.DestinationPassengerStatistics[destination] += passengers;
                }
            }
        }

        public void addTerminal(Terminal terminal)
        {
            this.Terminals.addTerminal(terminal);
        }

        //returns a list of major destinations and pax

        //clears the list of airline contracts
        public void clearAirlineContracts()
        {
            lock (this._Contracts)
            {
                this.AirlineContracts.Clear();
            }
        }

        public void clearDestinationCargoStatistics()
        {
            this.DestinationCargoStatistics.Clear();
        }

        public void clearDestinationPassengerStatistics()
        {
            this.DestinationPassengerStatistics.Clear();
        }

        public void clearDestinationPassengers()
        {
            this.DestinationPassengers.Clear();
        }

        public void clearFacilities()
        {
            this.Facilities = new List<AirlineAirportFacility>();
        }

        //cleares the list of facilities for an airline
        public void clearFacilities(Airline airline)
        {
            this.Facilities.RemoveAll(f => f.Airline == airline);
        }
        //returns if an airline is building a facility
        public Boolean isBuildingFacility(Airline airline, AirportFacility.FacilityType type)
        {
             var facilities = new List<AirlineAirportFacility>(this.Facilities);
             lock (this.Facilities)
             {
                 return facilities.Exists(f => f.Airline == airline && f.Facility.Type == type && f.FinishedDate > GameObject.GetInstance().GameTime);
             }
       
        }
        public AirlineAirportFacility getAirlineAirportFacility(Airline airline, AirportFacility.FacilityType type)
        {
            var facilities = new List<AirlineAirportFacility>();
            lock (this.Facilities)
            {
                facilities = (from f in this.Facilities
                    where
                        f.Airline == airline && f.Facility.Type == type
                        && GameObject.GetInstance().GameTime >= f.FinishedDate
                    orderby f.FinishedDate descending
                    select f).ToList();

                if (facilities.Count() == 0)
                {
                    AirportFacility noneFacility = AirportFacilities.GetFacilities(type).Find(f => f.TypeLevel == 0);
                    this.addAirportFacility(airline, noneFacility, GameObject.GetInstance().GameTime);

                    facilities.Add(this.getAirlineAirportFacility(airline, type));
                }
            }
            return facilities.FirstOrDefault();
        }

        public AirportFacility getAirlineBuildingFacility(Airline airline, AirportFacility.FacilityType type)
        {
            AirlineAirportFacility facility =
                this.Facilities.FirstOrDefault(
                    f =>
                        f.Airline == airline && f.Facility.Type == type
                        && GameObject.GetInstance().GameTime < f.FinishedDate);

            if (facility == null)
            {
                return null;
            }
            return facility.Facility;
        }
      
        //adds an airline airport contract to the airport

        //returns the contracts for an airline
        public List<AirportContract> getAirlineContracts(Airline airline)
        {
            return this.AirlineContracts.FindAll(a => a.Airline == airline);
        }

        //returns if an airline has a contract of a specific type

        //return all airline contracts
        public List<AirportContract> getAirlineContracts()
        {
            List<AirportContract> contracts;
            lock (this._Contracts)
            {
                contracts = new List<AirportContract>(this._Contracts);
            }
            return contracts;
        }

        public double getAirlineReputation(Airline airline)
        {
            //The score could be airport facilities for the airline, routes, connecting routes, hotels, service level per route etc
            double score = 0;

            score += this.Cooperations.Where(c => c.Airline == airline).Sum(c => c.Type.ServiceLevel * 9);

            List<AirlineAirportFacility> facilities;
            
            lock (this.Facilities)
            {
                facilities = new List<AirlineAirportFacility>(this.Facilities);
            }
             score += facilities.Where(f => f.Airline == airline).Sum(f => f.Facility.ServiceLevel * 10);
            
            if (airline.Routes.Exists(r=>r==null))
            {
                string aName = airline.Profile.Name;
                Boolean isHuman = airline.IsHuman;
            }

            IEnumerable<Route> airportRoutes =
                airline.Routes.Where(r => r.Destination1 == this || r.Destination2 == this);
            score += 7 * airportRoutes.Count();
            score += 6
                     * airportRoutes.Where(r => r.Type == Route.RouteType.Passenger)
                         .Sum(r => ((PassengerRoute)r).getServiceLevel(AirlinerClass.ClassType.Economy_Class));

            score +=
                airline.Alliances.Sum(
                    a =>
                        5
                        * a.Members.SelectMany(m => m.Airline.Routes)
                            .Count(r => r.Destination1 == this || r.Destination2 == this));

            foreach (CodeshareAgreement codesharing in airline.Codeshares)
            {
                int codesharingRoutes =
                    (codesharing.Airline1 == airline ? codesharing.Airline2 : codesharing.Airline1).Routes.Count(
                        r => r.Destination2 == this || r.Destination1 == this);
                score += 4 * codesharingRoutes;
            }

            return score;
        }

        public List<AirlineAirportFacility> getAirportFacilities(Airline airline)
        {
            var fac = new List<AirlineAirportFacility>();

            lock (this.Facilities)
            {
                fac = this.Facilities.FindAll(f => f.Airline == airline);
            }

            return fac;
        }

        //returns all facilities
        public List<AirlineAirportFacility> getAirportFacilities()
        {
            return this.Facilities;
        }

        public AirportFacility getAirportFacility(
            Airline airline,
            AirportFacility.FacilityType type,
            Boolean useAirport = false)
        {
            if (!useAirport)
            {
                return this.getAirlineAirportFacility(airline, type).Facility;
            }
            AirportFacility airlineFacility = this.getCurrentAirportFacility(airline, type);
            AirportFacility airportFacility = this.getCurrentAirportFacility(null, type);

            return airportFacility == null || airlineFacility.TypeLevel > airportFacility.TypeLevel
                ? airlineFacility
                : airportFacility;
        }

        public List<AirportFacility> getCurrentAirportFacilities(Airline airline)
        {
            var fs = new List<AirportFacility>();
            foreach (AirportFacility.FacilityType type in Enum.GetValues(typeof(AirportFacility.FacilityType)))
            {
                fs.Add(this.getCurrentAirportFacility(airline, type));
            }

            return fs;
        }

        public AirportFacility getCurrentAirportFacility(Airline airline, AirportFacility.FacilityType type)
        {
            var facilities = new List<AirportFacility>();


            lock (this.Facilities)
            {
               var tFacilities = new List<AirlineAirportFacility>(this.Facilities);
         
                facilities = (from f in tFacilities
                    where
                        f.Airline == airline && f.Facility.Type == type
                        && f.FinishedDate <= GameObject.GetInstance().GameTime
                    orderby f.FinishedDate descending
                    select f.Facility).ToList();
                int numberOfFacilities = facilities.Count();

                if (numberOfFacilities == 0 && airline != null)
                {
                    AirportFacility noneFacility = AirportFacilities.GetFacilities(type).Find(f => f.TypeLevel == 0);
                    this.addAirportFacility(airline, noneFacility, GameObject.GetInstance().GameTime);

                    facilities.Add(noneFacility);
                }
            }
            return facilities.FirstOrDefault();
        }

        public DestinationDemand getDestinationCargoObject(Airport destination)
        {
            return this.DestinationCargo.Find(a => a.Destination == destination.Profile.IATACode);
        }

        //returns the maximum value for the run ways

        //returns the destination cargo for a specific destination
        public ushort getDestinationCargoRate(Airport destination)
        {
            DestinationDemand cargo = this.DestinationCargo.Find(a => a.Destination == destination.Profile.IATACode);

            if (cargo == null)
            {
                return this.Statics.getDestinationCargoRate(destination);
            }
            return (ushort)(cargo.Rate + this.Statics.getDestinationCargoRate(destination));
        }

        public double getDestinationCargoStatistics(Airport destination)
        {
            double cargo = 0;
            lock (this.DestinationCargoStatistics)
            {
                if (this.DestinationCargoStatistics.ContainsKey(destination))
                {
                    cargo = this.DestinationCargoStatistics[destination];
                }
            }
            return cargo;
        }

        //adds a passenger rate for a destination

        //returns all airports where the airport has demand
        public List<Airport> getDestinationDemands()
        {
            var destinations = new List<DestinationDemand>();

            destinations.AddRange(this.Statics.getDemands());
            destinations.AddRange(this.DestinationCargo);
            destinations.AddRange(this.DestinationPassengers);

            return destinations.Select(d => Airports.GetAirport(d.Destination)).Distinct().ToList();

       }

        public long getDestinationPassengerStatistics(Airport destination)
        {
            long passengers;
            lock (this.DestinationPassengerStatistics)
            {
                if (this.DestinationPassengerStatistics.ContainsKey(destination))
                {
                    passengers = this.DestinationPassengerStatistics[destination];
                }
                else
                {
                    passengers = 0;
                }
            }

            return passengers;
        }

        //returns if the destination has passengers rate

        //returns a destination passengers object
        public DestinationDemand getDestinationPassengersObject(Airport destination)
        {
            return this.DestinationPassengers.Find(a => a.Destination == destination.Profile.IATACode);
        }

        public ushort getDestinationPassengersRate(Airport destination, AirlinerClass.ClassType type)
        {
            DestinationDemand pax = this.DestinationPassengers.Find(a => a.Destination == destination.Profile.IATACode);

            Array values = Enum.GetValues(typeof(AirlinerClass.ClassType));

            int classFactor = 0;

            int i = 1;

            foreach (AirlinerClass.ClassType value in values)
            {
                if (value == type)
                {
                    classFactor = i;
                }
                i++;
            }

            if (pax == null)
            {
                return this.Statics.getDestinationPassengersRate(destination, type);
            }
            return
                (ushort)
                    (this.Statics.getDestinationPassengersRate(destination, type) + (ushort)(pax.Rate / classFactor));
        }

        //returns a destination cargo object

        //returns the sum of passenger demand
        public int getDestinationPassengersSum()
        {
            int sum;
            lock (this.DestinationPassengers)
            {
                sum = this.DestinationPassengers.Sum(d => d.Rate);
            }

            return sum + this.Statics.getDestinationPassengersSum();
        }

        public List<DestinationDemand> getDestinationsPassengers()
        {
            return this.DestinationPassengers;

          
        }
        public List<DestinationDemand> getDestinationPassengersDemands()
        {
            List<DestinationDemand> demands = new List<DestinationDemand>();

            demands.AddRange(this.Statics.getPassengersDemand());
            demands.AddRange(this.getDestinationsPassengers());
           
            return demands;
        }
        //adds a number of passengers to destination to the statistics

        //returns the price for a gate
        public long getGatePrice()
        {
            long sizeValue = 100 + 102 * ((int)this.Profile.Size + 1);
            return Convert.ToInt64(GeneralHelpers.GetInflationPrice(sizeValue));
        }

        public List<Hub> getHubs(HubType.TypeOfHub type)
        {
            List<Hub> hubs;
            lock (this._Hubs) hubs = new List<Hub>(this._Hubs);

            return hubs.FindAll(h => h.Type.Type == type);
        }

        //returns all hubs
        public List<Hub> getHubs()
        {
            List<Hub> hubs;
            lock (this._Hubs) hubs = new List<Hub>(this._Hubs);

            return hubs;
        }
        /*
        //returns the fee for landing at the airport
        public double getLandingFee()
        {
            long sizeValue = 151 * ((int)this.Profile.Size + 1);
            return GeneralHelpers.GetInflationPrice(sizeValue);
        }*/

        public Dictionary<Airport, int> getMajorDestinations()
        {
            var majorDestinations = new Dictionary<Airport, int>();

            foreach (var md in this.Profile.MajorDestionations)
            {
                majorDestinations.Add(Airports.GetAirport(md.Key), md.Value);
            }

            return majorDestinations;
        }

        public long getMaxRunwayLength()
        {
            if (this.Runways.Count == 0)
            {
                return 0;
            }

            IEnumerable<long> query = from r in this.Runways
                where r.BuiltDate <= GameObject.GetInstance().GameTime
                select r.Length;
            return query.Max();
        }

        public long getTerminalGatePrice()
        {
            long price = 75000 * ((int)this.Profile.Size + 1);
            return Convert.ToInt64(GeneralHelpers.GetInflationPrice(price));
        }

        public long getTerminalPrice()
        {
            long price = 500000 + 15000 * ((int)this.Profile.Size + 1);
            return Convert.ToInt64(GeneralHelpers.GetInflationPrice(price));
        }

        /*
        //adds a facility to an airline
        public void addAirportFacility(Airline airline, AirportFacility facility, DateTime finishedDate)
        {
            lock (this.Facilities)
            {
                this.Facilities.Add(new AirlineAirportFacility(airline, this, facility, finishedDate));
            }
        }*/
        //sets the facility for an airline

        //returns if the airport has a facility for any airline
        public Boolean hasAirlineFacility()
        {
            return this.Facilities.Exists(f => f.Airline != null && f.Facility.TypeLevel > 0);
        }

        public Boolean hasAsHomebase(Airline airline)
        {
            foreach (FleetAirliner airliner in airline.Fleet)
            {
                if (airliner.Homebase == this)
                {
                    return true;
                }
            }

            return false;
        }

        public Boolean hasContractType(Airline airline, AirportContract.ContractType type)
        {
            return this.AirlineContracts.Exists(c => c.Airline == airline && c.Type == type);
        }

        public Boolean hasDestinationCargStatistics(Airport destination)
        {
            return this.DestinationCargoStatistics.ContainsKey(destination);
        }

        public Boolean hasDestinationPassengerStatistics(Airport destination)
        {
            return this.DestinationPassengerStatistics.ContainsKey(destination);
        }

        public Boolean hasDestinationPassengersRate(Airport destination)
        {
            return this.Statics.hasDestinationPassengersRate(destination)
                   || this.DestinationPassengers.Exists(a => a.Destination == destination.Profile.IATACode);
        }

        //returns the facilities being build for an airline

        //returns if an airline has any facilities at the airport
        public Boolean hasFacilities(Airline airline)
        {
            Boolean hasFacilities = false;
            foreach (AirportFacility.FacilityType type in Enum.GetValues(typeof(AirportFacility.FacilityType)))
            {
                if (this.getAirportFacility(airline, type).TypeLevel > 0)
                {
                    hasFacilities = true;
                }
            }
            return hasFacilities;
        }

        //returns if an airline has any facilities besides a specific type
        public Boolean hasFacilities(Airline airline, AirportFacility.FacilityType ftype)
        {
            Boolean hasFacilities = false;
            foreach (AirportFacility.FacilityType type in Enum.GetValues(typeof(AirportFacility.FacilityType)))
            {
                if (type != ftype)
                {
                    if (this.getAirportFacility(airline, type).TypeLevel > 0)
                    {
                        hasFacilities = true;
                    }
                }
            }
            return hasFacilities;
        }

        public Boolean hasHub(Airline airline)
        {
            return this.Hubs.Exists(h => h.Airline == airline);
        }

        public void removeAirlineContract(AirportContract contract)
        {
            lock (this._Contracts)
            {
                this._Contracts.Remove(contract);
            }

            if (!this.AirlineContracts.Exists(c => c.Airline == contract.Airline))
            {
                contract.Airline.removeAirport(this);
            }
        }

        public void removeCooperation(Cooperation cooperation)
        {
            this.Cooperations.Remove(cooperation);
        }

        //returns if an airline has any airliners with the airport as home base

        //removes the facility for an airline
        public void removeFacility(Airline airline, AirportFacility facility)
        {
            this.Facilities.RemoveAll(f => f.Airline == airline && f.Facility.Type == facility.Type);
        }

        //clears the list of facilites

        //removes a hub
        public void removeHub(Hub hub)
        {
            this._Hubs.Remove(hub);
        }

        //returns if an airline have a hub

        //removes a terminal from the airport
        public void removeTerminal(Terminal terminal)
        {
            AirportContract terminalContract =
                this.AirlineContracts.Find(c => c.Terminal != null && c.Terminal == terminal);

            if (terminalContract != null)
            {
                this.removeAirlineContract(terminalContract);
            }

            this.Terminals.removeTerminal(terminal);
        }

        #endregion

        //adds a cooperation to the airport
    }

    //the collection of airports
    public class Airports
    {
        #region Static Fields

        public static Dictionary<GeneralHelpers.Size, int> CargoAirportsSizes =
            new Dictionary<GeneralHelpers.Size, int>();

        public static double LargeAirports;

        public static double LargestAirports;

        public static double MediumAirports,
            SmallAirports;

        public static double SmallestAirports;

        public static double VeryLargeAirports;

        public static double VerySmallAirports;

        private static List<Airport> airports = new List<Airport>();

        #endregion

        #region Public Methods and Operators

        public static void AddAirport(Airport airport)
        {
            airports.Add(airport);
        }

        //clears the list
        public static void Clear()
        {
            airports = new List<Airport>();
        }

        //returns if a specific airport is in the list
        public static Boolean Contains(Airport airport)
        {
            return airports.Contains(airport);
        }

        public static int Count()
        {
            return GetAllActiveAirports().Count;
        }

        public static double CountLarge()
        {
            return LargeAirports;
        }

        public static double CountLargest()
        {
            return LargestAirports;
        }

        public static double CountMedium()
        {
            return MediumAirports;
        }

        public static double CountSmall()
        {
            return SmallAirports;
        }

        public static double CountSmallest()
        {
            return SmallestAirports;
        }

        public static double CountVeryLarge()
        {
            return VeryLargeAirports;
        }

        public static double CountVerySmall()
        {
            return VerySmallAirports;
        }

        //adds an airport

        //returns an airport based on iata code
        public static Airport GetAirport(string iata)
        {
            List<Airport> tAirports = airports.FindAll(a => a.Profile.IATACode == iata);

            if (tAirports.Count == 1)
            {
                return tAirports[0];
            }

            Airport tAirport = tAirports.Find(a => GeneralHelpers.IsAirportActive(a));

            if (tAirport == null)
            {
                return null;
            }
            return tAirport;
        }

        //returns an airport based on match
        public static Airport GetAirport(Predicate<Airport> match)
        {
            return airports.Find(match);
        }

        //returns an airport based on id

        //returns a possible match for coordinates
        public static Airport GetAirport(GeoCoordinate coordinates)
        {
            return GetAllActiveAirports().Find(a => a.Profile.Coordinates.Equals(coordinates));
        }

        public static Airport GetAirportFromID(string id)
        {
            Airport airport = airports.Find(a => a.Profile.ID == id);

            if (airport != null)
            {
                return airport;
            }

            return airports.Find(a => a.Profile.ID.StartsWith(id.Substring(0, 8)));
                //airports.Find(a=>a.Profile.ID.StartsWith(id.Substring(0, id.LastIndexOf('-'))));
        }

        //returns all airports with a specific size
        public static List<Airport> GetAirports(GeneralHelpers.Size size)
        {
            return GetAirports(delegate(Airport airport) { return airport.Profile.Size == size; });
        }

        //returns all airports from a specific country
        public static List<Airport> GetAirports(Country country)
        {
            return GetAirports(delegate(Airport airport) { return airport.Profile.Country == country; });
        }

        //returns all airports from a specific region
        public static List<Airport> GetAirports(Region region)
        {
            return GetAirports(delegate(Airport airport) { return airport.Profile.Country.Region == region; });
        }

        //returns a list of airports
        public static List<Airport> GetAirports(Predicate<Airport> match)
        {
            return GetAllActiveAirports().FindAll(match);
        }

        public static List<Airport> GetAllActiveAirports()
        {
            return airports.FindAll(a => GeneralHelpers.IsAirportActive(a));
        }

        //returns all airports
        public static List<Airport> GetAllAirports(Predicate<Airport> match)
        {
            return airports.FindAll(match);
        }

        public static List<Airport> GetAllAirports()
        {
            return airports;
            ;
        }

        //returns the total number of airports

        //removes all airports with a specific match
        public static void RemoveAirports(Predicate<Airport> match)
        {
            airports.RemoveAll(match);
        }

        #endregion
    }
}