using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace CoolPlanner
{
    /// <summary>Направление деятельности (категория мероприятий).</summary>
    public class Direction
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>Мероприятие в плане.</summary>
    public class EventItem
    {
        public int Id { get; set; }
        public int DirectionId { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Place { get; set; }
        public string Responsible { get; set; }

        /// <summary>Название направления для отображения в таблице (не сохраняется).</summary>
        [XmlIgnore]
        public string DirectionName { get; set; }
    }

    /// <summary>Участник мероприятий.</summary>
    public class Participant
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Group { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    /// <summary>Контейнер всех данных системы, сохраняется в XML.</summary>
    public class DataStore
    {
        public List<Direction> Directions { get; set; }
        public List<EventItem> Events { get; set; }
        public List<Participant> Participants { get; set; }
        public int NextDirectionId { get; set; }
        public int NextEventId { get; set; }
        public int NextParticipantId { get; set; }

        public DataStore()
        {
            Directions = new List<Direction>();
            Events = new List<EventItem>();
            Participants = new List<Participant>();
            NextDirectionId = 1;
            NextEventId = 1;
            NextParticipantId = 1;
        }
    }

    public static class Storage
    {
        private static string FilePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coolplanner.xml");
            }
        }

        public static DataStore Load()
        {
            if (!File.Exists(FilePath))
            {
                return new DataStore();
            }

            try
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var serializer = new XmlSerializer(typeof(DataStore));
                    return (DataStore)serializer.Deserialize(stream);
                }
            }
            catch (Exception)
            {
                return new DataStore();
            }
        }

        public static void Save(DataStore data)
        {
            using (var stream = File.Create(FilePath))
            {
                var serializer = new XmlSerializer(typeof(DataStore));
                serializer.Serialize(stream, data);
            }
        }

        public static string FileLocation
        {
            get { return FilePath; }
        }
    }
}
