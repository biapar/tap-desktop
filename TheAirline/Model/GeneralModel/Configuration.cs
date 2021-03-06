﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using TheAirline.Model.AirlinerModel;
using TheAirline.Model.AirlinerModel.RouteModel;

namespace TheAirline.Model.GeneralModel
{
    //the class for a configuration
    [Serializable]
    public abstract class Configuration : ISerializable
    {
        public enum ConfigurationType { Airliner, Routeclasses,AirlinerType }

        [Versioning("type")]
        public ConfigurationType Type { get; set; }

        [Versioning("standard")]
        public Boolean Standard { get; set; }

        [Versioning("name")]
        public string Name { get; set; }

        [Versioning("id")]
        public string ID { get; set; }
        public Configuration(ConfigurationType type, string name,Boolean standard)
        {
            this.Name = name;
            this.Type = type;
            this.Standard = standard;
            this.ID = name;
        }
             protected Configuration(SerializationInfo info, StreamingContext ctxt)
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
    //the collection of configurations
    public class Configurations
    {
        private static List<Configuration> configurations = new List<Configuration>();
        //adds a configuration to the list
        public static void AddConfiguration(Configuration configuration)
        {
            if (configurations.Find(c => c.Name == configuration.Name && !c.Standard) != null)
                configurations.RemoveAll(c => c.Name == configuration.Name);

            configurations.Add(configuration);
        }
        //returns a standard configuration with a specific name / or
        public static Configuration GetStandardConfiguration(string name)
        {
            return configurations.Find(c => (c.Name == name || c.ID == name) && c.Standard);
        }
        //returns the list of configuraitons
        public static List<Configuration> GetConfigurations()
        {
            return configurations;
        }
        //returns the list of configurations for a specific type
        public static List<Configuration> GetConfigurations(Configuration.ConfigurationType type)
        {
            return configurations.FindAll(c => c.Type == type);
        }
        //returns the list of configurations
        public static List<Configuration> GetConfigurations(Predicate<Configuration> match)
        {
            return configurations.FindAll(match);
        }
        //clears the list of configurations
        public static void Clear()
        {
            configurations = new List<Configuration>();
        }

    }
}
