using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;

namespace MongoDBSharp
{
    public partial class Form1 : Form
    {
        public static MongoClient client = new MongoClient();
        public static IMongoDatabase db = client.GetDatabase("vkort");

        public Form1()
        {
            InitializeComponent();
            //ResetDB();
        }

        //Registration
        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text) &&
                !string.IsNullOrWhiteSpace(textBox2.Text) &&
                !string.IsNullOrEmpty(textBox3.Text))
            {
                User Usr = new User
                {
                    Email = textBox1.Text,
                    Name = textBox2.Text,
                    Surname = textBox3.Text
                };

                try { db.GetCollection<User>("Users").InsertOne(Usr); }
                catch (MongoWriteException ex)
                {
                    if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                    {
                        label7.Text = "Email is already in use";
                    }
                    else throw ex;
                    textBox1.Clear();
                    return;
                }

                UserSession.Logout();
                UserSession.Login(Usr);
                toForm2();
            }
            
        }
        
        //Login
        private void button2_Click(object sender, EventArgs e)
        {
            /*var filter = Builders<User>.Filter.Eq(u => u.Email, "asd") | Builders<User>.Filter.Eq(u => u.ID, "asd");
            var update = Builders<User>.Update.AddToSet(x => x.Cards, card);
            db.GetCollection<User>("asd").FindOneAndUpdate(u => u.Email == email, update);*/

            string Email = textBox4.Text;

            if (!string.IsNullOrWhiteSpace(textBox4.Text))
            {
                int count = db.GetCollection<User>("Users").Find(u => u.Email == Email.Trim()).ToList().Count;
                if (count == 0) label7.Text = "Enter valid email address";
                else
                {
                    UserSession.Logout();
                    User Usr = new User
                    {
                        Email = Email
                    };
                    UserSession.Login(Usr);
                    toForm2();
                }
            }
            else label7.Text = "Enter valid email address";

        }

        public void toForm2()
        {
            Form2 f = new Form2(db);
            Hide();
            f.ShowDialog();
            Close();
        }

        public void ResetDB()
        {
            db.DropCollection("Users");
            db.DropCollection("TickeTypes");

            db.CreateCollection("Users");
            db.CreateCollection("TickeTypes");
            

            var options = new CreateIndexOptions { Unique = true };
            var field = new StringFieldDefinition<User>("Email");
            var indexDefinition = new IndexKeysDefinitionBuilder<User>().Ascending(field);
#pragma warning disable CS0618 // Type or member is obsolete
            db.GetCollection<User>("Users").Indexes?.CreateOne(indexDefinition, options);
#pragma warning restore CS0618 // Type or member is obsolete

            var options1 = new CreateIndexOptions { Unique = true };
            var field1 = new StringFieldDefinition<User>("Cards.CardNum");
            var indexDefinition1 = new IndexKeysDefinitionBuilder<User>().Ascending(field1);
#pragma warning disable CS0618 // Type or member is obsolete
            db.GetCollection<User>("Users").Indexes?.CreateOne(indexDefinition1, options1);
#pragma warning restore CS0618 // Type or member is obsolete*/

            List<TickeTypes> tickets = new List<TickeTypes>();
            tickets.Add(new TickeTypes { TicketName = "30min", TicketPrice = 0.65f });
            tickets.Add(new TickeTypes { TicketName = "60min", TicketPrice = 0.9f });
            tickets.Add(new TickeTypes { TicketName = "1d", TicketPrice = 5 });
            tickets.Add(new TickeTypes { TicketName = "3d", TicketPrice = 8});
            tickets.Add(new TickeTypes { TicketName = "5d", TicketPrice = 12});
            tickets.Add(new TickeTypes { TicketName = "10d", TicketPrice = 15});
            tickets.Add(new TickeTypes { TicketName = "30d", TicketPrice = 29});
            tickets.Add(new TickeTypes { TicketName = "3m", TicketPrice = 81});
            tickets.Add(new TickeTypes { TicketName = "6m", TicketPrice = 157});
            tickets.Add(new TickeTypes { TicketName = "9m", TicketPrice = 235});
            tickets.Add(new TickeTypes { TicketName = "12m", TicketPrice = 310});

            db.GetCollection<TickeTypes>("TickeTypes").InsertMany(tickets);
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }



    }
}
