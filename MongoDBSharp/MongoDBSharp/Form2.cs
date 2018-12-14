using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MongoDBSharp
{
    public partial class Form2 : Form
    {
        Form1 frm = new Form1();
        User Usr = UserSession.GetUser();
        IMongoDatabase db;

        public Form2(IMongoDatabase _db)
        {
            InitializeComponent();
            db = _db;
            var tickets = db.GetCollection<TickeTypes>("TickeTypes").Find(_ => true).ToList();
            for(int i = 0; i < 11; i++) comboBox3.Items.Add(tickets[i]);
            GetUserInfo();
            UpdateForm();
            var coll= db.GetCollection<User>("Users");
            var test = coll.Find(u => u.Cards.Any(c => true)).ToList();
            FieldDefinition<User> cardfield = "Cards";
            //.Group(new BsonDocument { { "_id", "$token" }, { "count", new BsonDocument("$sum", 1) } })
            
            

            
            Debug.WriteLine("breakpoint");
        }

        //Add new card
        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                Card card = new Card { CardNum = textBox1.Text };
                GetUserInfo();

                var update = Builders<User>.Update.AddToSet(x => x.Cards, card);

                try { db.GetCollection<User>("Users").FindOneAndUpdate(u => u.Email == UserSession.GetUser().Email, update); }
                catch (MongoCommandException ec)
                {
                    if (ec.Message == "Command findAndModify failed: Cannot apply $addToSet to non-array field. Field named 'Cards' has non-array type null.")
                    {
                        List<Card> Cards = new List<Card> { card };
                        var update1 = Builders<User>.Update.Set(x => x.Cards, Cards);
                        //db.GetCollection<User>("Users").FindOneAndUpdate(u => u.Email == UserSession.GetUser().Email, update);
                        try { db.GetCollection<User>("Users").UpdateOne(u => u.Email == UserSession.GetUser().Email, update1); }
                        catch (MongoWriteException ex)
                        {
                            if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                            {
                                label11.Text = "Card is already in use";
                                return;
                            }
                        }
                        catch (MongoBulkWriteException eb)
                        {
                            if (eb.Message.Contains("E11000"))
                            {
                                label11.Text = "Card is already in use";
                                return;
                            }
                        }
                        label11.Text = "New card added";
                        Usr.Cards = new List<Card>();
                        Usr.Cards.Add(card);
                        UpdateForm();
                        return;
                    }
                    if (ec.Message.Contains("E11000"))
                    {
                        label11.Text = "Card is already in use";
                        return;
                    }
                    else throw ec;
                }
                foreach (Card usrcard in Usr.Cards)
                {
                    if (usrcard.CardNum == textBox1.Text)
                    {
                        label11.Text = "This Card already exists";
                        UpdateForm();
                        return;
                    }
                }

                label11.Text = "New card added";
                Usr.Cards.Add(card);
                UpdateForm();
            }
        }

        //Get User info
        public void GetUserInfo()
        {
            var builder = Builders<User>.Filter;
            var query = builder.Eq(x => x.Email, Usr.Email);
            Usr = db.GetCollection<User>("Users").Find(query).Single();
        }

        public void UpdateForm()
        {
            if (Usr.Cards != null)
            {
                comboBox1.Items.Clear();
                comboBox2.Items.Clear();
                comboBox6.Items.Clear();
                comboBox5.Items.Clear();
                foreach (Card card in Usr.Cards)
                {

                    comboBox1.Items.Add(card.CardNum);
                    comboBox2.Items.Add(card.CardNum);
                    comboBox6.Items.Add(card.CardNum);
                    comboBox5.Items.Add(card.CardNum);
                }
            }
        }

        //Check if user already has this ticket
        public bool CheckTickets(Card card, string type, int index)
        {
            if(card.Tickets == null)
            {
                Usr.Cards[index].Tickets = new Dictionary<string, int>();
                return false;
            }
            else
            {
                foreach (KeyValuePair<string, int> entry in card.Tickets)
                {
                    if (entry.Key == type) return true;
                }
            }
            return false;
        }

        //Buy new ticket
        private void button2_Click(object sender, EventArgs e)
        {
            label11.Text = (comboBox3.SelectedItem as TickeTypes).TicketPrice.ToString();
            if (!string.IsNullOrWhiteSpace(textBox2.Text))
            {
                //var tickss = db.GetCollection<TickeTypes>("TickeTypes").Find(_ => true).ToList();
                string type = comboBox3.SelectedItem.ToString();
                int qnty;
                if (Int32.TryParse(textBox2.Text, out qnty)) { }
                else
                {
                    label11.Text = "String could not be parsed.";
                    return;
                }

                GetUserInfo();
                int index = 0;
                foreach (Card usrCard in Usr.Cards)
                {
                    if (usrCard.CardNum == comboBox1.SelectedItem.ToString())
                    {
                        index = Usr.Cards.IndexOf(usrCard);
                        break;
                    }
                }

                float fullprice = getFullPrice();
                if (Usr.Cards[index].Money < fullprice)
                {
                    label11.Text = "Not enough money";
                    return;
                }

                if (!CheckTickets(Usr.Cards[index], type, index)) Usr.Cards[index].Tickets.Add(type, qnty);
                else Usr.Cards[index].Tickets[type] += qnty;
                Usr.Cards[index].Money -= fullprice; 
                
                db.GetCollection<User>("Users").ReplaceOne(u => u.Email == Usr.Email, Usr);
                UpdateForm();
                label11.Text = "New ticket(s) added";
                //TODO: MAYBE ADD CARD TO USR ONLY WHEN QUERY IS COMPLETE
            }
        }

        //Activate ticket
        private void button3_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(comboBox2.SelectedItem.ToString()) &&
                !string.IsNullOrWhiteSpace(comboBox4.SelectedItem.ToString()))
            {
                GetUserInfo();
                int index = 0;
                foreach (Card usrCard in Usr.Cards)
                {
                    if (usrCard.CardNum == comboBox2.SelectedItem.ToString())
                    {
                        index = Usr.Cards.IndexOf(usrCard);
                        break;
                    }
                }
                if (Usr.Cards[index].Tickets[comboBox4.SelectedItem.ToString()] <= 0)
                {
                    label11.Text = "Not enough tickets";
                    return;
                }
                if (Usr.Cards[index].ActiveTicket != null)
                {
                    label11.Text = "Ticket is already active";
                    return;
                }
                var a = Usr.Cards[index].ActiveTicket;
                if (Usr.Cards[index].ActiveTicket == null)
                {
                    Usr.Cards[index].ActiveTicket = new ActiveTicket();
                }
                Usr.Cards[index].Tickets[comboBox4.SelectedItem.ToString()] -= 1;

                //Remove ticket if qnty 0
                if (Usr.Cards[index].Tickets[comboBox4.SelectedItem.ToString()] == 0)
                    Usr.Cards[index].Tickets.Remove(comboBox4.SelectedItem.ToString());

                Usr.Cards[index].ActiveTicket.Type = comboBox4.SelectedItem.ToString();
                db.GetCollection<User>("Users").ReplaceOne(u => u.Email == Usr.Email, Usr);
                comboBox4.Items.Clear();
                label11.Text = "Success";
            }
            
        }
      
        //Add money
        private void button6_Click(object sender, EventArgs e)
        {
            float qnty = float.Parse(textBox3.Text, CultureInfo.InvariantCulture.NumberFormat);

            GetUserInfo();
            int index = 0;
            foreach (Card usrCard in Usr.Cards)
            {
                if (usrCard.CardNum == comboBox6.SelectedItem.ToString())
                {
                    index = Usr.Cards.IndexOf(usrCard);
                    break;
                }
            }
            Usr.Cards[index].Money += qnty;

            db.GetCollection<User>("Users").ReplaceOne(u => u.Email == Usr.Email, Usr);
            UpdateForm();
            label11.Text = "Money added";
        }

        //Get bought tickets
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem.ToString() != "")
            {
                comboBox4.Items.Clear();
                foreach (Card usrCard in Usr.Cards)
                {
                    if (usrCard.CardNum == comboBox2.SelectedItem.ToString())
                    {
                        if (usrCard.Tickets == null) return;
                        foreach (KeyValuePair<string, int> ticket in usrCard.Tickets)
                            comboBox4.Items.Add(ticket.Key);
                        return;
                    }
                }
            }
        }

        //Update ammount of bought ticket
        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedItem.ToString() != "")
            {
                foreach (Card usrCard in Usr.Cards)
                {
                    if (usrCard.CardNum == comboBox2.SelectedItem.ToString())
                    {
                        if (usrCard.Tickets == null) return;
                        label12.Text = usrCard.Tickets[comboBox4.SelectedItem.ToString()].ToString();
                    }
                }
            };
        }
        
        public float getFullPrice()
        {
            float price = (comboBox3.SelectedItem as TickeTypes).TicketPrice;
            int ammount = int.Parse(textBox2.Text);
            return price * ammount;
        }
        
        //Show info on selected card
        private void button4_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(comboBox5.SelectedItem.ToString()))
            {
                GetUserInfo();
                label17.Text = comboBox5.SelectedItem.ToString();
                foreach (Card card in Usr.Cards)
                {
                    if (card.CardNum == comboBox5.SelectedItem.ToString())
                    {
                        label2.Text = card.Money.ToString();
                        if (card.ActiveTicket != null) label10.Text = card.ActiveTicket.Type;
                        else label10.Text = "Inactive";
                    }
                }
            }
        }
        
        //Set the full priice label
        private void button5_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox2.Text)) label19.Text = getFullPrice().ToString();
            else label19.Text = "enter ammount";
        }

        //Show all cards in collection
        private void button7_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            var coll = db.GetCollection<User>("Users");

            var test1 = coll.Aggregate()
                .Unwind("Cards")
                .Group(new BsonDocument("_id", "$Cards.CardNum")).ToList();

            foreach(var row in test1)
            {
                var listViewItem = new ListViewItem(row[0].ToString());
                listView1.Items.Add(listViewItem);
            }

        }

        //Show Money of each account(combining cards)
        private void button8_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            var coll = db.GetCollection<User>("Users");

            var text1 = coll.Aggregate()
                .Unwind("Cards")
                .Group(new BsonDocument { { "_id", "$Email" }, { "total", new BsonDocument("$sum", "$Cards.Money") } }).ToList();

            foreach (var row in text1)
            {
                ListViewItem listViewItem = new ListViewItem(new[] { row[0].ToString(), row[1].ToString() });
                listView1.Items.Add(listViewItem);
            }

        }

        //Map-Reduce show money of each account
        private void button9_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            var coll = db.GetCollection<User>("Users");

            string map = @"function() {
                for (var i = 0; i < this.Cards.length; i++)
                {
                    var value = this.Cards[i].Money;
                    emit(this.Email, value);
                }
            }";
            string reduce = @"function(Email, Money) {
                return Array.sum(Money);
            }";

            var options = new MapReduceOptions<User, BsonDocument>
            {
                OutputOptions = MapReduceOutputOptions.Inline
            };

            var results = coll.MapReduce(map, reduce, options).ToList();

            foreach (var row in results)
            {
                //var listViewItem = new ListViewItem(row[0].ToString() , row[1].ToString());
                ListViewItem listViewItem = new ListViewItem(new[] { row[0].ToString(), row[1].ToString() });
                listView1.Items.Add(listViewItem);

            }
                //Debug.WriteLine(result.ToJson());
        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }



        private void label18_Click(object sender, EventArgs e)
        {

        }
        


        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        
    }
}
