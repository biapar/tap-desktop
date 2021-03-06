﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TheAirline.Model.AirportModel;
using TheAirline.Model.GeneralModel;
using TheAirline.Model.GeneralModel.StatisticsModel;

namespace TheAirline.Model.AirlinerModel.RouteModel
{
   [Serializable]
    //the class for a cargo route
    public class CargoRoute : Route
    {
       [Versioning("unitprice")]
       public double PricePerUnit { get; set; }
        public CargoRoute(string id, Airport destination1, Airport destination2,DateTime startDate, double pricePerUnit)
            : base(RouteType.Cargo, id, destination1, destination2,startDate)
        {
            this.PricePerUnit = pricePerUnit;
        }
        public override double getFillingDegree()
        {
            if (this.HasStopovers)
            {
                double fillingDegree = 0;

                var legs = this.Stopovers.SelectMany(s => s.Legs);
                foreach (CargoRoute leg in legs)
                {
                    fillingDegree += leg.getFillingDegree();
                }
                return fillingDegree / legs.Count();

            }
            else
            {
                double cargo = Convert.ToDouble(this.Statistics.getTotalValue(StatisticsTypes.GetStatisticsType("Cargo")));

                double cargoCapacity = Convert.ToDouble(this.Statistics.getTotalValue(StatisticsTypes.GetStatisticsType("Capacity")));

                if (cargo > cargoCapacity)
                    return 1;

                return cargo / cargoCapacity;
            }
        }
        private CargoRoute(SerializationInfo info, StreamingContext ctxt) : base(info,ctxt)
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

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
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

            base.GetObjectData(info, context);

        }
    }
}
