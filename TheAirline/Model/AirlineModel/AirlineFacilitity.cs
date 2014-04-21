﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using TheAirline.Model.GeneralModel;

namespace TheAirline.Model.AirlineModel
{
    //the class for an airlines facilities
    [Serializable]
    public class AirlineFacility : ISerializable
    {

        
        public static string Section { get; set; }
        [Versioning("uid")]
        public string Uid { get; set; }
        [Versioning("price")]
        private double APrice;
        public double Price { get { return GeneralHelpers.GetInflationPrice(this.APrice); } set { this.APrice = value; } }
        [Versioning("monthlycost")]
        public double MonthlyCost { get; set; }
        [Versioning("luxury")]
        public int LuxuryLevel { get; set; } //for business customers
        [Versioning("service")]
        public int ServiceLevel { get; set; } //for repairing airliners 
        [Versioning("fromyear")]
        public int FromYear { get; set; }
        public AirlineFacility(string section, string uid, double price, double monthlyCost,int fromYear, int serviceLevel, int luxuryLevel)
        {
            AirlineFacility.Section = section;
            this.Uid = uid;
            this.FromYear = fromYear;
            this.MonthlyCost = monthlyCost;
            this.Price = price;
            this.LuxuryLevel = luxuryLevel;
            this.ServiceLevel = serviceLevel;
        }
        public string Name
        {
            get { return Translator.GetInstance().GetString(AirlineFacility.Section, this.Uid); }
        }

        public string Shortname
        {
            get { return Translator.GetInstance().GetString(AirlineFacility.Section, this.Uid, "shortname"); }
        }
        public AirlineFacility(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt16("version");

            var fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props = new List<PropertyInfo>(this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            var propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (SerializationEntry entry in info)
            {
                MemberInfo prop = propsAndFields.FirstOrDefault(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Name == entry.Name);

                if (prop != null)
                {
                    if (prop is FieldInfo)
                        ((FieldInfo)prop).SetValue(this, entry.Value);
                    else
                        ((PropertyInfo)prop).SetValue(this, entry.Value);
                }
            }

            var notSetProps = propsAndFields.Where(p => ((Versioning)p.GetCustomAttribute(typeof(Versioning))).Version > version);

            foreach (MemberInfo notSet in notSetProps)
            {
                Versioning ver = (Versioning)notSet.GetCustomAttribute(typeof(Versioning));

                if (ver.AutoGenerated)
                {
                    if (notSet is FieldInfo)
                        ((FieldInfo)notSet).SetValue(this, ver.DefaultValue);
                    else
                        ((PropertyInfo)notSet).SetValue(this, ver.DefaultValue);

                }

            }

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", 1);

            Type myType = this.GetType();

            var fields = myType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null);

            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.GetCustomAttribute(typeof(Versioning)) != null));

            var propsAndFields = props.Cast<MemberInfo>().Union(fields.Cast<MemberInfo>());

            foreach (MemberInfo member in propsAndFields)
            {
                object propValue;

                if (member is FieldInfo)
                    propValue = ((FieldInfo)member).GetValue(this);
                else
                    propValue = ((PropertyInfo)member).GetValue(this, null);

                Versioning att = (Versioning)member.GetCustomAttribute(typeof(Versioning));

                info.AddValue(att.Name, propValue);
            }



        }
    }
    //the collection of facilities
    public class AirlineFacilities
    {
        private static List<AirlineFacility> facilities = new List<AirlineFacility>();
         //clears the list
        public static void Clear()
        {
            facilities = new List<AirlineFacility>();
        }
        //adds a new facility to the collection
        public static void AddFacility(AirlineFacility facility)
        {
            facilities.Add(facility);
        }
        //returns a facility
        public static AirlineFacility GetFacility(string uid)
        {
            return facilities.Find(f => f.Uid == uid);
        }
        //returns the list of facilities
        public static List<AirlineFacility> GetFacilities()
        {
            return facilities;
        }
        public static List<AirlineFacility> GetFacilities(Predicate<AirlineFacility> match)
        {
            return facilities.FindAll(match);
        }
    }
}
