using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDBSharp
{
    public class User
    {
        [BsonId]
        public ObjectId ID { get; set; }

        [BsonElement("Email")]
        public string Email { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Surname")]
        public string Surname { get; set; }

        [BsonElement("Cards")]
        public List<Card> Cards { get; set; }


    }

    public class Card
    {
        [BsonElement("CardNum")]
        public string CardNum { get; set; }

        [BsonElement("Tickets")]
        public Dictionary<string, int> Tickets { get; set; }

        [BsonElement("ActiveTicket")]
        public ActiveTicket ActiveTicket { get; set; }

        [BsonElement("Money")]
        public float Money { get; set; }
    }

    public class ActiveTicket
    {
        [BsonId]
        public ObjectId ID { get; set; }
        [BsonElement("Type")]
        public string Type { get; set; }
        [BsonElement("Duedate")]
        public DateTime DueDate { get; set; }
    }

    public class TickeTypes
    {
        [BsonId]
        public ObjectId ID { get; set; }
        [BsonElement("TicketName")]
        public string TicketName { get; set; }
        [BsonElement("TicketPrice")]
        public float TicketPrice { get; set; }

        public override string ToString()
        {
            return TicketName;
        }
    }

    public class Item
    {
        public string Name;
        public int Value;
        public Item(string name, int value)
        {
            Name = name; Value = value;
        }
        public override string ToString()
        {
            // Generates the text shown in the combo box
            return Name;
        }
    }


    public class UserSession
    {
        private static volatile User currentUser;
        private static object syncRoot = new Object();

        private UserSession() { }

        public static bool CheckState()
        {
            if (currentUser == null) return false;
            else return true;
        }

        public static User GetUser()
        {
            if (currentUser == null) throw new Exception("Not logged in.");
            return currentUser;
        }

        public static void Login(User user)
        {
            if (currentUser != null) throw new Exception("Already logged in");
            lock (syncRoot)
            {
                currentUser = user;
            }
        }

        public static void Logout()
        {
            lock (syncRoot)
            {
                currentUser = null;
            }
        }
    }
}
